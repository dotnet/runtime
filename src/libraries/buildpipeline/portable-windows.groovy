@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Config - Build configuration. Note that we don't using 'Configuration' since it's used
//          in the build scripts and this can cause problems.
// OuterLoop - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null

simpleNode('Windows_NT','latest') {
    stage ('Checkout source') {
        retry (10) {
            checkout scm
        }
    }

    def logFolder = getLogFolder()

    stage ('Clean') {
        bat '.\\clean.cmd -all'
    }
    stage ('Sync') {
        bat '.\\sync.cmd -p -- /p:ArchGroup=x64 /p:RuntimeOS=win10'
    }
    stage ('Generate Version Assets') {
        bat '.\\build-managed.cmd -GenerateVersion'
    }
    stage ('Build Product') {
        bat ".\\build.cmd -buildArch=x64 -${params.Config} -- /p:SignType=real /p:RuntimeOS=win10"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.OuterLoop) {
            additionalArgs = '-Outerloop'
        }
        bat ".\\build-tests.cmd -buildArch=x64 -${params.Config} -SkipTests ${additionalArgs} -- /p:RuntimeOS=win10 /p:ArchiveTests=true"
    }
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
            def targetHelixQueues = ['Windows.10.Amd64.Open',
                                     'Windows.10.Nano.Amd64.Open',
                                     'Windows.7.Amd64.Open',
                                     'Windows.81.Amd64.Open']

            bat "\"%VS140COMNTOOLS%\\VsDevCmd.bat\" && msbuild src\\upload-tests.proj /p:ArchGroup=x64 /p:ConfigurationGroup=${params.Config} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=Windows_NT /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=%CloudDropAccessToken% /p:CloudResultsAccessToken=%OutputCloudResultsAccessToken% /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=\"${targetHelixQueues.join(',')}\" /p:HelixLogFolder= /p:HelixLogFolder=${WORKSPACE}\\${logFolder}\\ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt"

            submittedHelixJson = readJSON file: "${logFolder}\\SubmittedHelixRuns.txt"
        }
    }
}

stage ('Execute Tests') {
    def contextBase
    if (params.OuterLoop) {
        contextBase = "Win x64 tests w/outer - ${params.Config}"
    }
    else {
        contextBase = "Win x64 tests - ${params.Config}"
    }
    waitForHelixRuns(submittedHelixJson, contextBase)
}