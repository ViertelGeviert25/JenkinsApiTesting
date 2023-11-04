pipeline {
    agent any
    stages {
        stage('Default') {
            steps {
                echo "Hello ${params.TASK_ID}"
                script {
                    bat '"H:\\dev\\JenkinsApiTesting\\DoSomething\\bin\\Debug\\DoSomething.exe"'
                }
        }
    }
}
}