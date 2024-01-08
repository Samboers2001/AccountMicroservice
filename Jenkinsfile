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
                    // Change to the tests project directory
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
                    // Switch back to the main project directory for Docker build
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'docker build -t samboers/accountmicroservice .'
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

        stage('Deploy Database to Kubernetes') {
            steps {
                script {
                    // Change to the main project directory for Kubernetes commands if needed
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-secret.yaml'
                        sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-claim.yaml' 
                        sh 'kubectl apply -f K8S/Local/account-database/mariadb-account-depl.yaml'
                    }
                }
            }
        }

        stage('Deploy AccountMicroservice to Kubernetes') {
            steps {
                script {
                    // Change to the main project directory for Kubernetes commands if needed
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
                    // Change to the main project directory for Kubernetes commands if needed
                    dir('/Users/samboers/development/order_management_system/AccountMicroservice') {
                        sh 'kubectl rollout restart deployment account-depl'
                    }
                }
            }
        }
    }
}
