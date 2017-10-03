@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.

//--------------------- Windows Functions ----------------------------//

stage ('Basic') {
    simpleNode('Windows_NT', '20170427-elevated') {
        checkout scm
    }
}

