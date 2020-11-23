pipeline {
    agent any
    stages {
        stage('Build') {
            steps {
                bat 'bundle exec rake build:app'
            }
        }
        stage('Upload') {
            steps {
                bat 'bundle exec rake build:upload_app'
            }
        }
    }
}
