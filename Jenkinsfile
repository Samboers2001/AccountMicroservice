pipeline {
    agent any 
    
    environment {
        PATH = "/usr/local/bin:/Users/samboers/google-cloud-sdk/bin:$PATH"
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

        stage('Restore and Test') {
            steps {
                script {
                    sh 'ls -l AccountMicroservice.Tests'
                    sh '/usr/local/share/dotnet/dotnet restore AccountMicroservice.Tests/AccountMicroservice.Tests.csproj'
                    sh '/usr/local/share/dotnet/dotnet test AccountMicroservice.Tests/AccountMicroservice.Tests.csproj'
                }
            }
        }
        
        stage('Build docker image') {
            steps {
                script {
                    sh 'docker build -t samboers/accountmicroservice .'
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

        stage('Deploy Database to Kubernetes') {
            steps {
                script {
                    sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-secret.yaml'
                    sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-claim.yaml' 
                    sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-depl.yaml'
                }
            }
        }

        stage('Deploy AccountMicroservice to Kubernetes') {
            steps {
                script {
                    sh 'kubectl apply -f K8S/Local/account-service/account-depl.yaml'
                    sh 'kubectl apply -f K8S/Local/account-service/account-service-hpa.yaml'
                }
            }
        }

        stage('Rollout Restart') {
            steps {
                script {
                    sh 'kubectl rollout restart deployment account-depl'
                }
            }
        }
    }
}
