pipeline {
    agent any 
    
    environment {
        PATH = "/Users/samboers/.dotnet/tools:/usr/local/share/dotnet:/usr/local/bin:/Users/samboers/google-cloud-sdk/bin:$PATH"
    }

    stages {
        stage('Checkout') {
            steps {
                git 'https://github.com/Samboers2001/AccountMicroservice'
            }
        }

        stage('Checkout Test Project') {
            steps {
                git 'https://github.com/Samboers2001/AccountMicroservice.Tests'
            }
        }

        stage('SonarCloud Scan') {
            steps {
                script {
                    withCredentials([string(credentialsId: 'sonarcloud-token', variable: 'SONAR_TOKEN')]) {
                        dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                            sh 'dotnet sonarscanner begin /k:"Samboers2001_AccountMicroservice" /o:"samboers2001" /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="$SONAR_TOKEN"'
                            sh 'dotnet build'
                            sh 'dotnet sonarscanner end /d:sonar.login="$SONAR_TOKEN"'
                        }
                    }
                }
            }
        }        

        stage('Restore and Test') {
            steps {
                script {
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice.Tests') {
                        sh '/usr/local/share/dotnet/dotnet restore'
                        sh '/usr/local/share/dotnet/dotnet test'
                    }
                }
            }
        }
        
        stage('Build docker image') {
            steps {
                script {
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'docker build -t samboers/accountmicroservice .'
                    }
                }
            }
        }

        stage('Run Trivy Scan') {
            steps {
                script {
                    def trivyExitCode = sh(script: '/opt/homebrew/bin/trivy image --exit-code 1 --no-progress samboers/accountmicroservice:latest', returnStatus: true)
                    
                    if (trivyExitCode != 0) {
                        echo "Vulnerabilities were found but the pipeline will continue."
                    } else {
                        echo "No vulnerabilities found."
                    }
                }
            }
        }

        stage('Push to dockerhub') {
            steps {
                script {
                    withCredentials([string(credentialsId: 'dockerhubpasswordcorrect', variable: 'dockerhubpwd')]) {
                        sh 'docker login -u samboers -p ${dockerhubpwd}'
                        sh 'docker push samboers/accountmicroservice'
                    }
                }
            }
        }

        // stage('Deploy Database to Kubernetes') {
        //     steps {
        //         script {
        //             dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
        //                 sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-secret.yaml'
        //                 sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-claim.yaml' 
        //                 sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-depl.yaml'
        //             }
        //         }
        //     }
        // }

        stage('Deploy AccountMicroservice to Kubernetes') {
            steps {
                script {
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'kubectl apply -f K8S/Local/account-service/account-depl.yaml'
                        sh 'kubectl apply -f K8S/Local/account-service/account-service-hpa.yaml'
                    }
                }
            }
        }

        stage('Rollout Restart') {
            steps {
                script {
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'kubectl rollout restart deployment account-depl'
                    }
                }
            }
        }

        stage('Wait for Deployment to be Ready') {
            steps {
                script {
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'kubectl wait --for=condition=available --timeout=60s deployment/account-depl'
                    }
                }
            }
        }

        stage('Load Testing') {
            steps {
                script {
                    sh 'rm -f /Users/samboers/JMeter/results.csv'
                    sh 'rm -rf /Users/samboers/JMeter/htmlReport/*' 
                    sh 'mkdir -p /Users/samboers/JMeter/htmlReport'
                    sh '/opt/homebrew/bin/jmeter -n -t /Users/samboers/JMeter/LoginLoadTest.jmx -l /Users/samboers/JMeter/results.csv -e -o /Users/samboers/JMeter/htmlReport'
                }
            }
        }
    }
}
