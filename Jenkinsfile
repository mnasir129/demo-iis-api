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
        booleanParam(name: 'DEMO_API', defaultValue: true, description: 'Build and deploy Demo IIS API')
    }

    stages {
        stage('Build Demo Artifacts') {
            when {
                expression { return params.DEMO_API }
            }

            agent {
                docker {
                    image '178.28.51.108:8081/docker-hosted/local-dotnet9:latest'
                    registryUrl 'http://178.28.51.108:8081'
                    registryCredentialsId 'nexus-creds'
                    reuseNode true
                    alwaysPull true
                    label 'linux-docker'

                    /*
                     * Use Jenkins UID/GID here, not root.
                     * Replace 989:989 with your actual values from:
                     * id jenkins
                     */
                    args '-u 972:969 -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e NUGET_PACKAGES=/tmp/.nuget/packages'
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

        stage('Deploy to Local IIS') {
            when {
                expression { return params.DEMO_API }
            }

            agent { label 'linux-docker' }

            steps {
                checkout scm

                sh '''
                    echo "Workspace before unstash:"
                    pwd
                    ls -la
                '''

                dir('artifacts') {
                    unstash 'DemoAPI'
                }

                sh '''
                    echo "Artifacts:"
                    ls -lh artifacts/
                    echo "Ansible files:"
                    find automation/ansible -type f
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

                        echo "Running Ansible deployment to IIS..."

                        ansible-playbook \\
                          -i automation/ansible/inventories/local/hosts.yml \\
                          automation/ansible/playbooks/deploy_demo_iis_api.yml \\
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
            echo "Pipeline failed. Check Docker build, stash/unstash, Ansible WinRM, or IIS deployment logs."
        }

        success {
            echo "Pipeline completed successfully. DemoIisApi should be deployed to IIS."
        }
    }
}
