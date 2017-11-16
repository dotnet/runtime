@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// TGroup - The target framework to build.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null
def submitToHelix = (params.TGroup == 'netcoreapp' || params.TGroup == 'uap')

simpleNode('Windows_NT','latest') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    def logFolder = getLogFolder()
    def framework = ''
    if (params.TGroup == 'all') {
        framework = '-allConfigurations'
    }
    else {
        framework = "-framework:${params.TGroup}"
    }
    def buildTests = (params.TGroup != 'all')

    stage ('Initialize tools') {
        // Init tools
        bat '.\\init-tools.cmd'
    }
    stage ('Sync') {
        bat ".\\sync.cmd -p -- /p:ArchGroup=${params.AGroup} /p:RuntimeOS=win10"
    }
    stage ('Generate Version Assets') {
        bat '.\\build-managed.cmd -GenerateVersion'
    }
    stage ('Build Product') {
        bat ".\\build.cmd ${framework} -buildArch=${params.AGroup} -${params.CGroup} -- /p:RuntimeOS=win10"
    }
    if (buildTests) {
        stage ('Build Tests') {
            def additionalArgs = ''
            def archiveTests = 'false'
            if (params.TestOuter) {
                additionalArgs += ' -Outerloop'
            }
            if (submitToHelix) {
                archiveTests = 'true'
            }
            if (submitToHelix || params.TGroup == 'uapaot') {
                additionalArgs += ' -SkipTests'
            }
            bat ".\\build-tests.cmd ${framework} -buildArch=${params.AGroup} -${params.CGroup}${additionalArgs} -- /p:RuntimeOS=win10 /p:ArchiveTests=${archiveTests}"
        }
    }
    if (submitToHelix) {
        stage ('Submit To Helix For Testing') {
            // Bind the credentials
            withCredentials([string(credentialsId: 'CloudDropAccessToken', variable: 'CloudDropAccessToken'),
                             string(credentialsId: 'OutputCloudResultsAccessToken', variable: 'OutputCloudResultsAccessToken')]) {
                // Ask the CI SDK for a Helix source that makes sense.  This ensures that this pipeline works for both PR and non-PR cases
                def helixSource = getHelixSource()
                // Ask the CI SDK for a Build that makes sense.  We currently use the hash for the build
                def helixBuild = getCommit()
                // Get the user that should be associated with the submission
                def helixCreator = getUser()

                // Target queues
                def targetHelixQueues = []
                if (params.TGroup == 'netcoreapp')
                {
                    targetHelixQueues = ['Windows.10.Amd64.Open',
                                         'Windows.7.Amd64.Open',
                                         'Windows.81.Amd64.Open']
                    if (params.AGroup == 'x64') {
                        targetHelixQueues += ['Windows.10.Nano.Amd64.Open']
                    }
                } else if (params.TGroup == 'uap') {
                    targetHelixQueues = ['Windows.10.Amd64.ClientRS2.Open']
                }

                bat "\"%VS140COMNTOOLS%\\VsDevCmd.bat\" && msbuild src\\upload-tests.proj /p:TargetGroup=${params.TGroup} /p:ArchGroup=${params.AGroup} /p:ConfigurationGroup=${params.CGroup} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=Windows_NT /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=%CloudDropAccessToken% /p:CloudResultsAccessToken=%OutputCloudResultsAccessToken% /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=\"${targetHelixQueues.join(',')}\" /p:HelixLogFolder= /p:HelixLogFolder=${WORKSPACE}\\${logFolder}\\ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt"

                submittedHelixJson = readJSON file: "${logFolder}\\SubmittedHelixRuns.txt"
            }
        }
    }
}

if (submitToHelix) {
    stage ('Execute Tests') {
        def contextBase
        if (params.TestOuter) {
            contextBase = "Win tests w/outer - ${params.TGroup} ${params.AGroup} ${params.CGroup}"
        }
        else {
            contextBase = "Win tests - ${params.TGroup} ${params.AGroup} ${params.CGroup}"
        }
        waitForHelixRuns(submittedHelixJson, contextBase)
    }
}
