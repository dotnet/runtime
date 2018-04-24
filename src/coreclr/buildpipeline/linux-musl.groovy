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
    stage ('Sync') {
        sh "./sync.sh"
    }
    stage ('Build Product') {
        sh "./build.sh -x64 -${params.CGroup} -skiprestore -stripSymbols -portablebuild=false"
    }
}
