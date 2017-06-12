@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Config - Build configuration. Note that we don't using 'Configuration' since it's used
//          in the build scripts and this can cause problems.
// OuterLoop - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null

simpleDockerNode('microsoft/dotnet-buildtools-prereqs:rhel7_prereqs_2') {
    stage ('Checkout source') {
        checkout scm
    }

    def logFolder = getLogFolder()

    stage ('Initialize tools') {
        try {
            // Init tools
            sh './init-tools.sh'
        }
        catch (err) {
            // On errors for build tools initializations, it's useful to echo the contents of the file
            // for easy diagnosis.  This could also be copied to the log directory
            sh 'cat init-tools.log'
            // Ensure the build result is still propagated.
            throw err
        }
    }
    stage ('Generate version assets') {
        // Generate the version assets.  Do we need to even do this for non-official builds?
        sh "./build-managed.sh -- /t:GenerateVersionSourceFile /p:GenerateVersionSourceFile=true"
    }
    stage ('Sync') {
        sh "./sync.sh -p -- /p:ArchGroup=x64"
    }
    stage ('Build Product') {
        sh "./build.sh -buildArch=x64 -${params.Config}"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.OuterLoop) {
            additionalArgs = '-Outerloop'
        }
        sh "./build-tests.sh -buildArch=x64 -${params.Config} -SkipTests ${additionalArgs} -- /p:ArchiveTests=true /p:EnableDumpling=true"
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
                                     'Ubuntu.1610.Amd64.Open',
                                     // 'Ubuntu.1704.Amd64.Open',
                                     'suse.422.amd64.Open',
                                     'fedora.25.amd64.Open',]
            if (params.OuterLoop) {
                targetHelixQueues += ['Debian.90.Amd64.Open',]
            }					 

            sh "./Tools/msbuild.sh src/upload-tests.proj /p:ArchGroup=x64 /p:ConfigurationGroup=${params.Config} /p:TestProduct=corefx /p:TimeoutInSeconds=1200 /p:TargetOS=Linux /p:HelixJobType=test/functional/cli/ /p:HelixSource=${helixSource} /p:BuildMoniker=${helixBuild} /p:HelixCreator=${helixCreator} /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken=\$CloudDropAccessToken /p:CloudResultsAccessToken=\$OutputCloudResultsAccessToken /p:HelixApiEndpoint=https://helix.dot.net/api/2017-04-14/jobs /p:TargetQueues=${targetHelixQueues.join('+')} /p:HelixLogFolder=${WORKSPACE}/${logFolder}/ /p:HelixCorrelationInfoFileName=SubmittedHelixRuns.txt"

            submittedHelixJson = readJSON file: "${logFolder}/SubmittedHelixRuns.txt"
        }
    }
}

stage ('Execute Tests') {
    def contextBase
    if (params.OuterLoop) {
        contextBase = "Linux x64 Tests w/outer - ${params.Config}"
    }
    else {
        contextBase = "Linux x64 Tests - ${params.Config}"
    }
    waitForHelixRuns(submittedHelixJson, contextBase)
}