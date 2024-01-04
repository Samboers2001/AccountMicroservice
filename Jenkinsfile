pipeline {
    agent any
    
    environment {
        PATH = "/usr/local/bin:$PATH"
    }

    stages {
        stage('Checkout') {
            steps {
                git 'https://github.com/Samboers2001/AccountMicroservice'
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
    }
}
