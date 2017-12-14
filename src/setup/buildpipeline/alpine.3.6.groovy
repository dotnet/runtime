@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.
// CGroup - Build configuration.

simpleDockerNode('microsoft/dotnet-buildtools-prereqs:alpine-3.6-3148f11-20171119021156') {
    stage ('Checkout source') {
        checkoutRepo()
    }

    stage ('Build Product') {
        sh "./build.sh -ConfigurationGroup=${params.CGroup} -TargetArchitecture=${params.AGroup} -PortableBuild=false -strip-symbols -SkipTests=false -- /p:OutputRid=alpine.3.6-${params.AGroup}"
    }
}
