@Library('local-common-lib') _

def demoAPIZip = "demo-api.tar.gz"

def nexusRegistry = "172.28.51.108:8081"
def dotnetBuildImage = "${nexusRegistry}/docker-hosted/local-dotnet9:latest"

// Update this with your actual automation repo URL
def automationRepoUrl = "https://github.com/mnasir129/demo-iis-automation.git"

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
        booleanParam(name: 'DEMO_API', defaultValue: true, description: 'Build and deploy Demo IIS API')
    }

    stages {
        stage('Build Demo Artifacts') {
            when {
                expression { return params.DEMO_API }
            }

            agent {
                docker {
                    image "${dotnetBuildImage}"
                    registryUrl "http://${nexusRegistry}"
                    registryCredentialsId 'nexus-creds'
                    reuseNode true
                    alwaysPull true
                    label 'linux-docker'

                    /*
                     * Run container as Jenkins user to avoid root-owned workspace files.
                     * Current Jenkins UID/GID: 972:969
                     */
                    args '-u 972:969 -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e NUGET_PACKAGES=/tmp/.nuget/packages'
                }
            }

            stages {
                stage('Checkout Demo App Code') {
                    steps {
                        checkout scm

                        sh '''
                            echo "Current app workspace:"
                            pwd
                            echo "App repo files:"
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

        stage('Deploy to Local IIS') {
            when {
                expression { return params.DEMO_API }
            }

            agent { label 'linux-docker' }

            steps {
                sh '''
                    echo "Deploy workspace before automation checkout:"
                    pwd
                    ls -la || true
                '''

                dir('automation-repo') {
                    git branch: 'main',
                        url: "${automationRepoUrl}"
                }

                dir('artifacts') {
                    unstash 'DemoAPI'
                }

                sh '''
                    echo "Workspace after automation checkout and artifact unstash:"
                    pwd

                    echo "Automation repo files:"
                    find automation-repo -type f

                    echo "Artifact files:"
                    ls -lh artifacts/
                '''

                withCredentials([
                    usernamePassword(
                        credentialsId: 'windows-creds',
                        usernameVariable: 'ANSIBLE_REMOTE_USER',
                        passwordVariable: 'ANSIBLE_REMOTE_PASSWORD'
                    )
                ]) {
                    sh """
                        set -e

                        echo "Running Ansible deployment to IIS using separate automation repo..."

                        ansible-playbook \\
                          -i automation-repo/ansible/inventories/local/hosts.yml \\
                          automation-repo/ansible/playbooks/deploy_demo_iis_api.yml \\
                          --extra-vars "demo_api_code_tarball=$WORKSPACE/artifacts/${demoAPIZip}"
                    """
                }
            }

            post {
                always {
                    echo "Cleaning deploy workspace"
                    deleteDir()
                }
            }
        }
    }

    post {
        always {
            echo "Full CI/CD pipeline finished"
            deleteDir()
        }

        failure {
            echo "Pipeline failed. Check Docker build, stash/unstash, automation repo checkout, Ansible WinRM, or IIS deployment logs."
        }

        success {
            echo "Pipeline completed successfully. DemoIisApi should be deployed to IIS."
        }
    }
}