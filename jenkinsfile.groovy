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
        stage('Sync') [
            steps {
                def get = new URL("https://httpbin.org/get").openConnection();
                def getRC = get.getResponseCode();
                println(getRC);
                if(getRC.equals(200)) {
                    println(get.getInputStream().getText());
                }
            }
        ]
    }
}
}