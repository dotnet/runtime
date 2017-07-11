@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

def submittedHelixJson = null

simpleNode('OSX10.12','latest') {
    stage ('Checkout source') {
        checkout scm
    }

    def logFolder = getLogFolder()

    stage ('Initialize tools') {
        // Workaround nuget issue https://github.com/NuGet/Home/issues/5085 were we need to set HOME
        // Init tools
        sh 'HOME=\$WORKSPACE/tempHome ./init-tools.sh'
    }
    stage ('Generate version assets') {
        // Generate the version assets.  Do we need to even do this for non-official builds?
        sh "./build-managed.sh -- /t:GenerateVersionSourceFile /p:GenerateVersionSourceFile=true"
    }
    stage ('Sync') {
        sh "HOME=\$WORKSPACE/tempHome ./sync.sh -p -- /p:ArchGroup=x64"
    }
    stage ('Build Product') {
        sh "HOME=\$WORKSPACE/tempHome ./build.sh -buildArch=x64 -${params.CGroup}"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.TestOuter) {
            additionalArgs = '-Outerloop'
        }
        sh "HOME=\$WORKSPACE/tempHome ./build-tests.sh -buildArch=x64 -${params.CGroup} ${additionalArgs} -- /p:ArchiveTests=true /p:EnableDumpling=true"
    }
}
