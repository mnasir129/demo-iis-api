@Library('local-common-lib') _

def demoAPIZip = "demo-api.tar.gz"

pipeline {
    agent { label 'linux-docker' }

    options {
        ansiColor('xterm')
        timestamps()
        timeout(time: 1, unit: 'HOURS')
        disableConcurrentBuilds()
        skipDefaultCheckout(true)
    }

    parameters {
        booleanParam(name: 'DEMO_API', defaultValue: true, description: 'Build Demo IIS API')
    }

    stages {
        stage('Build Demo Artifacts') {
            when {
                expression { return params.DEMO_API }
            }

            agent {
                docker {
                    image '172.28.51.76:8081/docker-hosted/local-dotnet9:latest'
                    registryUrl 'http://172.28.51.76:8081'
                    registryCredentialsId 'nexus-creds'
                    reuseNode true
                    alwaysPull true
                    label 'linux-docker'
                    args '-u 0:0'
                }
            }

            stages {
                stage('Checkout Demo Code') {
                    steps {
                        checkout scm
                        sh '''
                            echo "Current workspace:"
                            pwd
                            echo "Files in workspace:"
                            ls -la
                        '''
                    }
                }

                stage('Package Demo API') {
                    steps {
                        script {
                            library('local-common-lib').local.DockerDotnet.setupDotnetLocalRepositories(this)

                            library('local-common-lib').local.DockerDotnet.runDotnetShellScript(this, """
                                dotnet restore DemoIisApi.csproj
                                dotnet publish DemoIisApi.csproj --configuration Release -o deploy
                                tar -cvzf ${demoAPIZip} -C deploy --exclude='appsettings*.json' .
                                ls -lh ${demoAPIZip}
                            """)

                            stash name: "DemoAPI", includes: "${demoAPIZip}", allowEmpty: false
                        }
                    }
                }
            }

            post {
                always {
                    echo "Cleaning build workspace"
                    deleteDir()
                }
            }
        }
    }

    post {
        always {
            echo "Build-only pipeline finished"
            deleteDir()
        }

        failure {
            echo "Build-only pipeline failed. Check Docker pull, shared library loading, or dotnet publish logs."
        }
    }
}