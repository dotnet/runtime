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
    def commonprops = "--ci /p:ArchGroup=${params.AGroup} /p:ConfigurationGroup=${params.CGroup}"

    stage ('Build Product') {
        sh "./build.sh ${commonprops} /p:RuntimeOs=linux-musl /p:PortableBuild=false"
    }
    stage ('Build Tests') {
        def additionalArgs = ''
        if (params.TestOuter) {
            additionalArgs = ' /p:OuterLoop=true'
        }
        sh "./build.sh -test ${commonprops} /p:SkipTests=true /p:ArchiveTests=true /p:EnableDumpling=false /p:PortableBuild=false${additionalArgs}"
    }

    // TODO: Add submission for Helix testing once we have queue for Alpine Linux working
}

// TODO: Add "Execute tests" stage once we have queue for Alpine Linux working
