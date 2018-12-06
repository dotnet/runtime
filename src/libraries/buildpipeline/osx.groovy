@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null

simpleNode('OSX10.12','latest') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    def logFolder = getLogFolder()
    def commonprops = "--ci /p:ArchGroup=x64 /p:ConfigurationGroup=${params.CGroup}"

    stage ('Build Product') {
        sh "HOME=\$WORKSPACE/tempHome ./build.sh ${commonprops}"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.TestOuter) {
            additionalArgs = ' /p:Outerloop=true'
        }
        sh "HOME=\$WORKSPACE/tempHome ./build.sh -test ${commonprops} /p:SkipTests=true /p:ArchiveTests=true /p:EnableDumpling=false${additionalArgs}"
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
            def targetHelixQueues = ['OSX.1012.Amd64.Open',
                                     'OSX.1013.Amd64.Open',]

            sh "HOME=\$WORKSPACE/tempHome ./eng/common/msbuild.sh --warnaserror false src/upload-tests.proj /p:ArchGroup=x64 /p:ConfigurationGroup=${params.CGroup} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=OSX /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=\$CloudDropAccessToken /p:CloudResultsAccessToken=\$OutputCloudResultsAccessToken /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=${targetHelixQueues.join('+')} /p:HelixLogFolder=${WORKSPACE}/${logFolder}/ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt"

            submittedHelixJson = readJSON file: "${logFolder}/SubmittedHelixRuns.txt"
        }
    }
}

stage ('Execute Tests') {
    def contextBase
    if (params.TestOuter) {
        contextBase = "OSX x64 Tests w/outer - ${params.CGroup}"
    }
    else {
        contextBase = "OSX x64 Tests - ${params.CGroup}"
    }
    waitForHelixRuns(submittedHelixJson, contextBase)
}
