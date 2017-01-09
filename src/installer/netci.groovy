// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = ['Debian8.2:x64:Debug', 'Ubuntu:x64:Release', 'Ubuntu16.04:x64:Release', 'Ubuntu16.10:x64:Release', 'OSX:x64:Release', 'Windows_NT:x64:Release', 'Windows_NT:x86:Debug', 'Windows_NT:arm:Debug', 'RHEL7.2:x64:Release', 'CentOS7.1:x64:Debug', 'Fedora23:x64:Debug', 'OpenSUSE13.2:x64:Debug', 'OpenSUSE42.1:x64:Debug']

def static getBuildJobName(def configuration, def os, def architecture) {
    return configuration.toLowerCase() + '_' + os.toLowerCase() + '_' + architecture.toLowerCase()
}


platformList.each { platform ->
    // Calculate names
    def (os, architecture, configuration) = platform.tokenize(':')

    // Calculate the job name
    def jobName = getBuildJobName(configuration, os, architecture)
    def buildCommand = '';

    // Calculate build command
    if (os == 'Windows_NT') {
        if (architecture == 'arm') {
            buildCommand = ".\\build.cmd -Configuration ${configuration} -TargetArch ${architecture} -Targets Default"
        }
        else {
            buildCommand = ".\\build.cmd -Configuration ${configuration} -Architecture ${architecture} -Targets Default"
        }
        
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
    
    if (!(os == 'Windows_NT' && architecture == 'arm')) {
        Utilities.addXUnitDotNETResults(newJob, '**/*-testResults.xml')
    }
    
    Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} ${architecture} ${configuration} Build")
}

// **************************
// Define ARM64 building.
// **************************
['Windows_NT'].each { os ->
    ['Release'].each { configurationGroup ->
        def newJobName = "${configurationGroup.toLowerCase()}_${os.toLowerCase()}_arm64"
        def arm64Users = ['ianhays', 'kyulee1', 'gkhanna79', 'weshaggard', 'stephentoub', 'rahku', 'ramarag']
        def newJob = job(Utilities.getFullJobName(project, newJobName, /* isPR */ false)) {
            steps {
                // build the world, but don't run the tests
                batchFile("build.cmd -Configuration ${configurationGroup} -Targets Init,Compile,Package,Publish -Architecure x64 -TargetArch arm64 -ToolsetDir C:\\ats2 -Framework netcoreapp1.1")
            }
            label("arm64")
            
            // Kick off the test run
            publishers {
                archiveArtifacts {
                    pattern("artifacts/win10-arm64/packages/*.zip")
                    pattern("artifacts/win10-arm64/corehost/*.nupkg")
                    onlyIfSuccessful(true)
                    allowEmpty(false)
                }
            }
        }

        // Set up standard options.
        Utilities.standardJobSetup(newJob, project, /* isPR */ false, "*/${branch}")
        
        // Set a daily trigger
        Utilities.addPeriodicTrigger(newJob, '@daily')
        
        // Set up a PR trigger that is only triggerable by certain members
        Utilities.addPrivateGithubPRTriggerForBranch(newJob, branch, "Windows_NT ARM64 ${configurationGroup} Build", "(?i).*test\\W+ARM64\\W+${os}\\W+${configurationGroup}", null, arm64Users)

        // Set up a per-push trigger
        Utilities.addGithubPushTrigger(newJob)
    }
}
