// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Import the utility functionality.

import jobs.generation.Utilities;
import jobs.generation.ArchivalSettings;

def project = GithubProject
def branch = GithubBranchName
def isPR = true

def platformList = [
  'Linux:x64:Release',
  'Linux:arm:Release',
  'Linux:arm64:Release',
  'OSX:x64:Release',
  'Windows_NT:x64:Release',
  'Windows_NT:x86:Debug',
  'Windows_NT:arm64:Debug',
  'Windows_NT:arm:Debug',
  'Tizen:armel:Release'
]

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
    def crossbuildargs = ''
    def buildArgs = "-ConfigurationGroup=${configuration} -TargetArchitecture=${architecture}"

    if (os != 'Windows_NT' && configuration == 'Release') {
        buildArgs += " -strip-symbols"
    }

    // Calculate build command
    if (os == 'Windows_NT') {
        buildCommand = ".\\build.cmd ${buildArgs}"
        if ((architecture == 'arm' || architecture == 'arm64')) {
            buildCommand += " -SkipTests=true"
        }
    }
    else if (os == 'Tizen') {
        dockerRepository = "tizendotnet/dotnet-buildtools-prereqs"
        dockerContainer = "ubuntu-16.04-cross-e435274-20180426002255-tizen-rootfs-5.0m1"

        dockerCommand = "docker run -e ROOTFS_DIR=/crossrootfs/${architecture}.tizen.build --name ${dockerContainer} --rm -v \${WORKSPACE}:${dockerWorkingDirectory} -w=${dockerWorkingDirectory} ${dockerRepository}:${dockerContainer}"
        buildArgs += " -SkipTests=true -DisableCrossgen=true -PortableBuild=false -CrossBuild=true -- /p:OverridePackageSource=https:%2F%2Ftizen.myget.org/F/dotnet-core/api/v3/index.json /p:OutputRid=tizen.5.0.0-${architecture}"
        buildCommand = "${dockerCommand} ./build.sh ${buildArgs}"
    }
    else if (os == "Linux") {

        // Prep for Portable Linux builds take place on Ubuntu 14.04
        if (architecture == 'arm' || architecture == 'armel' || architecture == 'arm64') {
            if (architecture == 'arm64') {
                dockerContainer = "ubuntu-16.04-cross-arm64-a3ae44b-20180316023254"
            }
            else {
                dockerContainer = "ubuntu-14.04-cross-e435274-20180323032140"
            }
            dockerCommand = "docker run -e ROOTFS_DIR=/crossrootfs/${architecture} --name ${dockerContainer} --rm -v \${WORKSPACE}:${dockerWorkingDirectory} -w=${dockerWorkingDirectory} ${dockerRepository}:${dockerContainer}"
            buildArgs += " -SkipTests=true -CrossBuild=true"

            if (architecture == 'armel') {
                buildArgs += " -DisableCrossgen=true"
            }

            buildCommand = "${dockerCommand} ./build.sh ${buildArgs}"

            osForGHTrigger = "Linux"
            os = "Ubuntu"
        }
        else {
            // Jenkins non-Ubuntu CI machines don't have docker
            buildCommand = "./build.sh ${buildArgs}"
            
            // Trigger a portable Linux build that runs on RHEL7.2
            osForGHTrigger = "Linux"
            os = "RHEL7.2"
        }
    }
    else {
        // Jenkins non-Ubuntu CI machines don't have docker
        buildCommand = "./build.sh ${buildArgs}"
        os = "OSX10.12"
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

    if (!(architecture == 'arm' || architecture == 'armel' || architecture == 'arm64')) {
        Utilities.addMSTestResults(newJob, '**/*-testResults.trx')
    }

    Utilities.addGithubPRTriggerForBranch(newJob, branch, "${osForGHTrigger} ${architecture} ${configuration} Build")
    
    ArchivalSettings settings = new ArchivalSettings();
    def archiveString = ["tar.gz", "zip", "deb", "msi", "pkg", "exe", "nupkg"].collect { "bin/*/packages/*.${it},bin/*/corehost/*.${it}" }.join(",")
    settings.addFiles(archiveString)
    settings.setArchiveOnSuccess()
    settings.setFailIfNothingArchived()

    Utilities.addArchival(newJob, settings)
}

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.
