@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// TGroup - The target framework to build.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null
def submitToHelix = (params.TGroup == 'netcoreapp' || params.TGroup == 'netfx' || params.TGroup == 'uap')

simpleNode('windows.10.amd64.clientrs4.devex.open') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    def logFolder = getLogFolder()
    def framework = ''
    if (params.TGroup == 'all') {
        framework = '-allconfigurations'
    }
    else {
        framework = "-framework ${params.TGroup}"
    }

    def commonprops = "-ci ${framework} /p:ArchGroup=${params.AGroup} /p:ConfigurationGroup=${params.CGroup}"

    stage ('Build Product') {
        bat ".\\build.cmd ${commonprops} /p:RuntimeOS=win10"
    }
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
        if (params.TGroup != 'all') {
            bat ".\\build.cmd -test ${commonprops} /p:RuntimeOS=win10 /p:ArchiveTests=${archiveTests} /p:EnableDumpling=false${additionalArgs}"
        }
        else {
            bat ".\\build.cmd -test -ci /p:TargetGroup=netstandard /p:ArchGroup=${params.AGroup} /p:ConfigurationGroup=${params.CGroup} /p:SkipTests=true"
            bat ".\\build.cmd -test ${commonprops}${additionalArgs}"
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
                    targetHelixQueues = ['Windows.7.Amd64.Open',
                                         'Windows.81.Amd64.Open',
                                         'Windows.10.Amd64.ClientRS4.ES.Open',]
                    if (params.AGroup == 'x64') {
                        targetHelixQueues += ['Windows.10.Nano.Amd64.Open']
                    }
                } else if (params.TGroup == 'uap' || params.TGroup == 'netfx') {
                    targetHelixQueues = ['Windows.10.Amd64.ClientRS4.Open']
                }

                // We should just use .\eng\common\msbuild.ps1 here instead.
                bat "set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1& .\\.dotnet\\dotnet.exe msbuild /nologo .\\src\\upload-tests.proj /p:TargetGroup=${params.TGroup} /p:ArchGroup=${params.AGroup} /p:ConfigurationGroup=${params.CGroup} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=Windows_NT /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=%CloudDropAccessToken% /p:CloudResultsAccessToken=%OutputCloudResultsAccessToken% /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=\\\"${targetHelixQueues.join(',')}\\\" /p:HelixLogFolder=${WORKSPACE}\\${logFolder}\\ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt /p:MaxRetryCount=3"

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
