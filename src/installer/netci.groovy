// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = ['Debian8.2:x64:Debug', 'Ubuntu:x64:Release', 'Ubuntu16.04:x64:Release', 'OSX:x64:Release', 'Windows_NT:x64:Release', 'Windows_NT:x86:Debug', 'RHEL7.2:x64:Release', 'CentOS7.1:x64:Debug', 'Fedora23:x64:Debug', 'OpenSUSE13.2:x64:Debug']

def static getBuildJobName(def configuration, def os, def architecture) {
    return configuration.toLowerCase() + '_' + os.toLowerCase() + '_' + architecture.toLowerCase()
}


platformList.each { platform ->
    // Calculate names
    def (os, architecture, configuration) = platform.tokenize(':')

    // Calculate job name
    def jobName = getBuildJobName(configuration, os, architecture)
    def buildCommand = '';

    // Calculate the build command
    if (os == 'Windows_NT') {
        buildCommand = ".\\build.cmd -Configuration ${configuration} -Architecture ${architecture} -Targets Default"
    }
    else if (os == 'Windows_2016') {
        buildCommand = ".\\build.cmd -Configuration ${configuration} -Architecture ${architecture} -RunInstallerTestsInDocker -Targets Default"
    }
    else if (os == 'Ubuntu') {
        buildCommand = "./build.sh --skip-prereqs --configuration ${configuration} --docker ubuntu.14.04 --targets Default"
    }
    else {
        // Jenkins non-Ubuntu CI machines don't have docker
        buildCommand = "./build.sh --skip-prereqs --configuration ${configuration} --targets Default"
    }

    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
        // Set the label.
        steps {
            if (os == 'Windows_NT' || os == 'Windows_2016') {
                // Batch
                batchFile(buildCommand)
            }
            else {
                // Shell
                shell(buildCommand)
            }
        }
    }

    Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')
    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
    
    Utilities.addXUnitDotNETResults(newJob, '**/*-testResults.xml')
    
    Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${architecture} ${configuration} Build")
}


