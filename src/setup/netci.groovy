// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;
import jobs.generation.ArchivalSettings;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = ['Debian8.2:x64:Debug', 'PortableLinux:x64:Release', 'Ubuntu:x64:Release', 'Ubuntu16.04:x64:Release', 'Ubuntu16.10:x64:Release', 'Ubuntu:arm:Release', 'Ubuntu16.04:arm:Release', 'OSX10.12:x64:Release', 'Windows_NT:x64:Release', 'Windows_NT:x86:Debug', 'Windows_NT:arm:Debug', 'Fedora24:x64:Debug', 'OpenSUSE42.1:x64:Debug']

def static getBuildJobName(def configuration, def os, def architecture) {
    return configuration.toLowerCase() + '_' + os.toLowerCase() + '_' + architecture.toLowerCase()
}

platformList.each { platform ->
    // Calculate names
    def (os, architecture, configuration) = platform.tokenize(':')

    // Calculate the job name
    def jobName = getBuildJobName(configuration, os, architecture)
    def buildCommand = '';
    def osForGHTrigger = os
    def version = "latest-or-auto"
    def dockerRepository = "microsoft/dotnet-buildtools-prereqs"
    def dockerContainer = ''
    def dockerWorkingDirectory = "/src/core-setup"
    def dockerCommand = ''
    def portableArgs = ''

    // Calculate build command
    if (os == 'Windows_NT') {
        buildCommand = ".\\build.cmd -ConfigurationGroup=${configuration} -TargetArchitecture=${architecture}"
        if ((architecture == 'arm' || architecture == 'arm64')) {
            buildCommand += " -PortableBuild=true -SkipTests=true"
        }
    }
    else if ((os.startsWith("Ubuntu")) && 
             (architecture == 'arm' || architecture == 'armel')) {
        if (os == 'Ubuntu') {
            dockerContainer = "ubuntu-14.04-cross-0cd4667-20172211042239"
        }
        else if (os == 'Ubuntu16.04') {
            dockerContainer = "ubuntu-16.04-cross-ef0ac75-20175511035548"
        }
        portableArgs = " -portable cross skiptests disablecrossgen"
        dockerCommand = "docker run --name ${dockerContainer} --rm -v \${WORKSPACE}:${dockerWorkingDirectory} -w=${dockerWorkingDirectory} ${dockerRepository}:${dockerContainer}"
        buildCommand = "${dockerCommand} ./build.sh ${configuration} ${architecture}${portableArgs}"
    }
    else if (os == "Ubuntu") {
        dockerContainer = "ubuntu-14.04-debpkg-e5cf912-20175003025046"
        dockerCommand = "docker run --name ${dockerContainer} --rm -v \${WORKSPACE}:${dockerWorkingDirectory} -w=${dockerWorkingDirectory} ${dockerRepository}:${dockerContainer}"
        buildCommand = "${dockerCommand} ./build.sh ${configuration} ${architecture}${portableArgs}"
    }
    else if (os == "PortableLinux") {
        // Jenkins non-Ubuntu CI machines don't have docker
        buildCommand = "./build.sh ${configuration} ${architecture} -portable"
        
        // Trigger a portable Linux build that runs on RHEL7.2
        osForGHTrigger = "PortableLinux"
        os = "RHEL7.2"
    }
    else {
        // Jenkins non-Ubuntu CI machines don't have docker
        buildCommand = "./build.sh ${configuration} ${architecture}"
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

    Utilities.setMachineAffinity(newJob, os, version)
    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

    if (!(architecture == 'arm')) {
        Utilities.addMSTestResults(newJob, '**/*-testResults.trx')
    }

    if (os == 'Ubuntu16.04' && architecture == 'arm') {
        // Don't enable by default
        def contextString = "${osForGHTrigger} ${architecture} ${configuration}"
        Utilities.addGithubPRTriggerForBranch(newJob, branch, "${contextString} Build", "(?i).*test\\W+${contextString}.*", true /* trigger on comment phrase only */)
    }
    else {
        Utilities.addGithubPRTriggerForBranch(newJob, branch, "${osForGHTrigger} ${architecture} ${configuration} Build")
    }

    ArchivalSettings settings = new ArchivalSettings();
    def archiveString = ["tar.gz", "zip", "deb", "msi", "pkg", "exe", "nupkg"].collect { "Bin/*/packages/*.${it},Bin/*/corehost/*.${it}" }.join(",")
    settings.addFiles(archiveString)
    settings.setArchiveOnSuccess()
    settings.setFailIfNothingArchived()

    Utilities.addArchival(newJob, settings)
}

// **************************
// Define ARM64 building.
// **************************
//['Windows_NT'].each { os ->
//    ['Release'].each { configurationGroup ->
//        def newJobName = "${configurationGroup.toLowerCase()}_${os.toLowerCase()}_arm64"
//        def arm64Users = ['ianhays', 'kyulee1', 'gkhanna79', 'weshaggard', 'stephentoub', 'rahku', 'ramarag']
//        def newJob = job(Utilities.getFullJobName(project, newJobName, /* isPR */ false)) {
//            steps {
//                // build the world, but don't run the tests
//                batchFile("build.cmd -ConfigurationGroup ${configurationGroup} -Architecure x64 -TargetArch arm64 -ToolsetDir C:\\ats2 -Framework netcoreapp1.1")
//            }
//            label("arm64")
//
//            // Kick off the test run
//            publishers {
//                archiveArtifacts {
//                    pattern("artifacts/win10-arm64/packages/*.zip")
//                    pattern("artifacts/win10-arm64/corehost/*.nupkg")
//                    onlyIfSuccessful(true)
//                    allowEmpty(false)
//                }
//            }
//        }
//
//        // Set up standard options.
//        Utilities.standardJobSetup(newJob, project, /* isPR */ false, "*/${branch}")
//
//        // Set a daily trigger
//        Utilities.addPeriodicTrigger(newJob, '@daily')
//
//        // Set up a PR trigger that is only triggerable by certain members
//        Utilities.addPrivateGithubPRTriggerForBranch(newJob, branch, "Windows_NT ARM64 ${configurationGroup} Build", "(?i).*test\\W+ARM64\\W+${os}\\W+${configurationGroup}", null, arm64Users)
//
//        // Set up a per-push trigger
//        Utilities.addGithubPushTrigger(newJob)
//    }
//}

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
