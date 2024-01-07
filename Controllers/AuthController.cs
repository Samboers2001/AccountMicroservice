using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AccountMicroservice.AsyncDataServices.Interfaces;
using AccountMicroservice.Helpers;
using AccountMicroservice.MessageBusEvents;
using AccountMicroservice.Models;

namespace AccountMicroservice.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IMessageBusClient _messageBusClient;
        private readonly Lib _lib = new Lib();

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IMessageBusClient messageBusClient)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _messageBusClient = messageBusClient;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] Login loginModel)
        {
            var user = await _userManager.FindByNameAsync(loginModel.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginModel.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };
                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }
                var token = GetToken(authClaims);
                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo
                });
            }
            else
            {
                throw new AppException("Username or password is incorrect");
            }
        }

        [HttpGet]
        [Route("userdetails")]
        public async Task<IActionResult> GetUserDetails()
        {
            var username = User.Identity.Name;
            if (username == null)
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user); // get the roles of the user

            return Ok(new { user, roles });
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] Register model)
        {
            if (!_lib.UsernameIsEmpty(model.Username))
            {
                throw new AppException("Name is required");
            }
            else if (!_lib.EmailIsEmpty(model.Email))
            {
                throw new AppException("Email is required");
            }
            else if (!_lib.IsValidEmail(model.Email))
            {
                throw new AppException($"Email '{model.Email}' is not valid");
            }
            else if (_userManager.Users.Any(x => x.Email == model.Email))
            {
                throw new AppException($"Email '{model.Email}' is already taken");
            }
            else if (!_lib.PasswordIsEmpty(model.Password))
            {
                throw new AppException("Password is required");
            }
            else if (!_lib.IsValidPassword(model.Password))
            {
                throw new AppException("Password must contain a number, uppercase letter, lowercase letter, and must be minimal 5 characters long");
            }
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User already exists!" });
            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User creation failed! Please check user details and try again." });

            UserRegisteredEvent userRegisteredEvent = new UserRegisteredEvent
            {
                UserId = user.Id
            };

            // Publish the message and wait for acknowledgment
            var acknowledgmentReceived = await PublishAndWaitForAcknowledgment(userRegisteredEvent, "user.registered");

            if (acknowledgmentReceived)
            {
                return Ok(new Response { Status = "Success", Message = "User created successfully!", Test = "Still, Yeah, Still works as expected!" });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "Acknowledgment not received. User creation might not have been processed successfully." });
            }
        }

        [HttpPost]
        [Route("register-admin")]
        public async Task<IActionResult> RegisterAdmin([FromBody] Register model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User already exists!" });
            IdentityUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError, new Response { Status = "Error", Message = "User creation failed! Please check user details and try again." });
            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
            if (!await _roleManager.RoleExistsAsync(UserRoles.User))
                await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));
            if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                await _userManager.AddToRoleAsync(user, UserRoles.Admin);
            }
            if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                await _userManager.AddToRoleAsync(user, UserRoles.User);
            }
            return Ok(new Response { Status = "Success", Message = "User created successfully!" });
        }

        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWTAuth:SecretKey"]));
            var token = new JwtSecurityToken(
                issuer: _configuration["JWTAuth:ValidIssuerURL"],
                audience: _configuration["JWTAuth:ValidAudienceURL"],
                expires: DateTime.Now.AddHours(3),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );
            return token;
        }

        private async Task<bool> PublishAndWaitForAcknowledgment(object message, string routingKey)
        {
            _messageBusClient.PublishMessage(message, routingKey);

            // You can add additional logic for waiting for acknowledgment here
            // For simplicity, this example waits for a fixed duration
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Return true for demonstration purposes (replace with actual acknowledgment check)
            return true;
        }
    }
}
