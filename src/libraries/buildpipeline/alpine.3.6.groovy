@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// CGroup - Build configuration.
// TestOuter - If true, runs outerloop, if false runs just innerloop

simpleDockerNode('microsoft/dotnet-buildtools-prereqs:alpine-3.6-3148f11-20171119021156') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    stage ('Initialize tools') {
        // Init tools
        sh './init-tools.sh'
    }
    stage ('Generate version assets') {
        // Generate the version assets.  Do we need to even do this for non-official builds?
        sh "./build-managed.sh -runtimeos=alpine.3.6 -- /t:GenerateVersionSourceFile /p:GenerateVersionSourceFile=true /p:PortableBuild=false"
    }
    stage ('Sync') {
        sh "./sync.sh -p -runtimeos=alpine.3.6 -- /p:ArchGroup=x64 /p:PortableBuild=false"
    }
    stage ('Build Product') {
        sh "./build.sh -buildArch=x64 -runtimeos=alpine.3.6 -${params.CGroup} -- /p:PortableBuild=false"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.TestOuter) {
            additionalArgs = '-Outerloop'
        }
        sh "./build-tests.sh -buildArch=x64 -${params.CGroup} -SkipTests ${additionalArgs} -- /p:ArchiveTests=true /p:EnableDumpling=true /p:PortableBuild=false"
    }

    // TODO: Add submission for Helix testing once we have queue for Alpine Linux working
}

// TODO: Add "Execute tests" stage once we have queue for Alpine Linux working
