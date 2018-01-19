@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null

simpleDockerNode('microsoft/dotnet-buildtools-prereqs:rhel7_prereqs_2') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    def logFolder = getLogFolder()

    stage ('Initialize tools') {
        // Init tools
        sh './init-tools.sh'
    }
    stage ('Generate version assets') {
        // Generate the version assets.  Do we need to even do this for non-official builds?
        sh "./build-managed.sh -- /t:GenerateVersionSourceFile /p:GenerateVersionSourceFile=true"
    }
    stage ('Sync') {
        sh "./sync.sh -p -- /p:ArchGroup=x64"
    }
    stage ('Build Product') {
        sh "./build.sh -buildArch=x64 -${params.CGroup}"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.TestOuter) {
            additionalArgs = '-Outerloop'
        }
        sh "./build-tests.sh -buildArch=x64 -${params.CGroup} -SkipTests ${additionalArgs} -- /p:ArchiveTests=true /p:EnableDumpling=true"
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
            def targetHelixQueues = ['Centos.73.Amd64.Open',
                                     'RedHat.73.Amd64.Open',
                                     'Debian.87.Amd64.Open',
                                     'Ubuntu.1404.Amd64.Open',
                                     'Ubuntu.1604.Amd64.Open',
                                     'opensuse.422.amd64.open',
                                     'fedora.25.amd64.Open',]
            if (params.TestOuter) {
                targetHelixQueues += ['Debian.90.Amd64.Open',
                                      'Fedora.26.Amd64.Open',
                                      'SLES.12.Amd64.Open',]
            }

            sh "./Tools/msbuild.sh src/upload-tests.proj /p:ArchGroup=x64 /p:ConfigurationGroup=${params.CGroup} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=Linux /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=\$CloudDropAccessToken /p:CloudResultsAccessToken=\$OutputCloudResultsAccessToken /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=${targetHelixQueues.join('+')} /p:HelixLogFolder=${WORKSPACE}/${logFolder}/ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt"

            submittedHelixJson = readJSON file: "${logFolder}/SubmittedHelixRuns.txt"
        }
    }
}

stage ('Execute Tests') {
    def contextBase
    if (params.TestOuter) {
        contextBase = "Linux x64 Tests w/outer - ${params.CGroup}"
    }
    else {
        contextBase = "Linux x64 Tests - ${params.CGroup}"
    }
    waitForHelixRuns(submittedHelixJson, contextBase)
}
