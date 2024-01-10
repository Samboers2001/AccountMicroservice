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
