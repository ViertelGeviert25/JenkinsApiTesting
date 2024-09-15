//Map parallelStages = [:]
List parallelStagesGroup = []
semaphoreMaxStages  = 2

def countCharacter(input, character) {
    int count = 0
    input.each { ch ->
        if (ch == character) {
            count++
        }
    }
    return count
}

@NonCPS
def List sortList(input, sep) {
    input = input.sort { str1, str2 ->
        str1.tokenize(sep).size() <=> str2.tokenize(sep).size()
    }
    return input
}

def generateStage(taskId, pagent) {
    return {
        script {
            // limit concurrency
            waitUntil { 
                if (semaphoreMaxStages > 0) { 
                    semaphoreMaxStages--; 
                    return true 
                } 
                else { 
                    return false 
                }
            }
        }
        stage("Task ${taskId}") {
            agent {
                label "${pagent}"
            }
            script {
                catchError(stageResult: 'FAILURE') {
                    int maxRetries = 3
                    int retryCount = 1
                    boolean success = false

                    while (retryCount <= maxRetries && !success) {
                        int returnCode = bat(returnStatus: true, script: """
                            ""H:\\dev\\JenkinsApiTesting\\Runner\\bin\\Release\\net8.0\\Runner.exe"" --taskId=${taskId} --retry=${retryCount}
                            echo Error level is %errorlevel%
                        """)
                        semaphoreMaxStages++

                        if (returnCode == 11) {
                            sleep(time: 15, unit: 'SECONDS')
                            retryCount++
                            echo "Retry attempt ${retryCount} due to status code 11"
                        } else if (returnCode == 0) {
                            success = true
                            bat 'exit 0'
                        } else {
                            bat 'exit -1'
                        }
                    }

                    if (!success) {
                        currentBuild.result = 'FAILURE'
                        error "Stage failed after ${maxRetries} retries due to status code 11"
                    }
                }
            }
        }
    }
}

pipeline {
    agent {
        label 'jenkinsagent1'
    }
    stages {
        stage('Create list of stages') {
            steps {
                echo 'This stage will be executed first.'
                echo "NODE_NAME = ${env.NODE_NAME}"
                echo "Current job name: $JOB_NAME"
                script {
                    char idSep = ';'
                    char sepChar = '_'
                    int taskIdLength = -1
                    Map parallelStages = [:]
                    List agents = ['jenkinsagent1', 'jenkinsagent2'] //, 'jenkinsagent3', 'jenkinsagent4']
                    int agentCount = agents.size()

                    List stagesList  = params.TASKIDS.tokenize(idSep)
                    if (stagesList.size() < 1) {
                        error('TASKIDS must be a comma-delimited list of applications to build')
                    }
                    stagesList = sortList(stagesList, sepChar)
                    stagesList.eachWithIndex { stage, index ->
                        echo "task configured: ${stage}"
                        String agent = agents[index % agentCount] // distribute stages on agent

                        int order = countCharacter(stage, sepChar)
                        if (order > taskIdLength) {
                            parallelStagesGroup += [:]
                            taskIdLength = order
                        }
                        // parallelStages["${agent}-${stage}"] = generateStage(stage, agent)
                        parallelStagesGroup.last()["${agent}-${stage}"] = generateStage(stage, agent)
                    }
                }
            }
        }
        stage('Run stages in parallel') {
            steps {
                script {
                    //parallel parallelStages
                    parallelStagesGroup.eachWithIndex { stagesMap, index ->
                        parallel stagesMap
                    }
                }
            }
        }
        stage('Conclusion') {
            steps {
                script {
                    bat 'echo Done.'
                }
            }
        }
    }
}

// ------------------    FIRST IDEA ----------------

// List parallelStagesGroup = []


// Maximum number of stages running in parallel
// def parallelLimit = 4

// sh "echo outer stages count: ${parallelLimit}"

// // Grouping the stages
// for (int idx = 0; idx < stagesList .size(); idx++) {
//     String stageName = stagesList[idx]
//     sh "echo Current Stage name is ${stageName}"
//     if (idx % parallelLimit == 0) {
//         sh "echo reached limit!!"
//         parallelStagesGroup += [:]
//     }
//     int groupSize = parallelStagesGroup.size()
//     sh "echo group size: ${groupSize}"
//     parallelStagesGroup.last()[stageName] = generateStage(stageName)
// }

    //    stage('Run Stages in Parallel') {
    //        steps {
    //            script {
    //                 // parallelStagesGroup.each { stagesMap ->
    //                 //     // Directly pass the map of stages to `parallel`
    //                 //     parallel stagesMap
    //                 // }
    //            }
    //        }
    //    }

// ------------------------------------------------

// Map distributedStages = [:]
// def agentCount = agents.size()

// stagesList.eachWithIndex { stage, index ->
//     def agent = agents[index % agentCount] // distribute stages on agent
//     if (!distributedStages[agent]) {
//         distributedStages[agent] = []
//     }
//     distributedStages[agent].add(stage)
// }
