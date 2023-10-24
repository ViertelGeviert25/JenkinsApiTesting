pipeline {
    agent any
    stages {
        stage('Default') {
            steps {
                script {
                    bat '"H:\\dev\\JenkinsApiTesting\\DoSomething\\bin\\Debug\\DoSomething.exe"'
                }
        }
    }
}
}