@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null

simpleDockerNode('microsoft/dotnet-buildtools-prereqs:centos-6-376e1a3-20174311014331') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    def logFolder = getLogFolder()

    stage ('Initialize tools') {
        // Init tools
        sh 'LD_LIBRARY_PATH=/usr/local/lib ./init-tools.sh'
    }
    stage ('Generate version assets') {
        // Generate the version assets.  Do we need to even do this for non-official builds?
        sh "LD_LIBRARY_PATH=/usr/local/lib ./build-managed.sh -runtimeos=rhel.6 -- /t:GenerateVersionSourceFile /p:GenerateVersionSourceFile=true /p:PortableBuild=false"
    }
    stage ('Sync') {
        sh "LD_LIBRARY_PATH=/usr/local/lib ./sync.sh -p -runtimeos=rhel.6 -- /p:ArchGroup=x64 /p:PortableBuild=false"
    }
    stage ('Build Product') {
        sh "LD_LIBRARY_PATH=/usr/local/lib ./build.sh -buildArch=x64 -runtimeos=rhel.6 -${params.CGroup} -- /p:PortableBuild=false"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.TestOuter) {
            additionalArgs = '-Outerloop'
        }
        sh "LD_LIBRARY_PATH=/usr/local/lib ./build-tests.sh -buildArch=x64 -${params.CGroup} -SkipTests ${additionalArgs} -- /p:ArchiveTests=true /p:EnableDumpling=true /p:PortableBuild=false"
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
            def targetHelixQueues = ['RedHat.69.Amd64.Open']
            
            sh "LD_LIBRARY_PATH=/usr/local/lib ./Tools/msbuild.sh src/upload-tests.proj /p:ArchGroup=x64 /p:ConfigurationGroup=${params.CGroup} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=Linux /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=\$CloudDropAccessToken /p:CloudResultsAccessToken=\$OutputCloudResultsAccessToken /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=${targetHelixQueues.join('+')} /p:HelixLogFolder=${WORKSPACE}/${logFolder}/ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt"

            submittedHelixJson = readJSON file: "${logFolder}/SubmittedHelixRuns.txt"
        }
    }
}

stage ('Execute Tests') {
    def contextBase
    if (params.TestOuter) {
        contextBase = "RHEL.6 x64 Tests w/outer - ${params.CGroup}"
    }
    else {
        contextBase = "RHEL.6 x64 Tests - ${params.CGroup}"
    }
    waitForHelixRuns(submittedHelixJson, contextBase)
}
