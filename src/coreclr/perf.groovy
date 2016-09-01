// Import the utility functionality.

import jobs.generation.*;

def project = GithubProject
def branch = GithubBranchName
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

def static getOSGroup(def os) {
    def osGroupMap = ['Ubuntu':'Linux',
        'RHEL7.2': 'Linux',
        'Ubuntu16.04': 'Linux',
        'Debian8.4':'Linux',
        'Fedora23':'Linux',
        'OSX':'OSX',
        'Windows_NT':'Windows_NT',
        'FreeBSD':'FreeBSD',
        'CentOS7.1': 'Linux',
        'OpenSUSE13.2': 'Linux',
        'OpenSUSE42.1': 'Linux',
        'LinuxARMEmulator': 'Linux']
    def osGroup = osGroupMap.get(os, null) 
    assert osGroup != null : "Could not find os group for ${os}"
    return osGroupMap[os]
}

[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
        def newJob = job(Utilities.getFullJobName(project, "perf_${os}", isPR)) {
            // Set the label.
            label('performance')
            steps {
                    // Batch
                    batchFile("C:\\tools\\nuget install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory C:\\tools -Prerelease")
                    batchFile("python C:\\tools\\Microsoft.BenchView.JSONFormat.0.1.0-pre008\\tools\\machinedata.py")
                    batchFile("set __TestIntermediateDir=int&&build.cmd release x64")
                    batchFile("tests\\runtest.cmd release x64")
                    batchFile("tests\\scripts\\run-xunit-perf.cmd")
            }
        }

        // Save machinedata.json to /artifact/bin/ Jenkins dir
        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles('sandbox\\perf-*.xml')
        archiveSettings.addFiles('machinedata.json')
        Utilities.addArchival(newJob, archiveSettings)

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        if (isPR) {
            TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
            builder.setGithubContext("${os} Perf Tests")
            builder.triggerOnlyOnComment()
            builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+perf.*")
            builder.triggerForBranch(branch)
            builder.emitTrigger(newJob)
        }
        else {
            // Set a push trigger
            TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
            builder.emitTrigger(newJob)
        }
    }
}

// Create the Linux/OSX/CentOS coreclr test leg for debug and release and each scenario
[true, false].each { isPR ->
    ['Ubuntu'].each { os ->
        def osGroup = getOSGroup(os)
        def architecture = 'x64'
        def configuration = 'Checked'
        def newJob = job(Utilities.getFullJobName(project, "perf_${osGroup}", isPR)) {
            parameters {
                stringParam('CORECLR_WINDOWS_BUILD', '', 'Build number to copy CoreCLR windows test binaries from')
                stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
            }
                    
            steps {
                // Set up the copies
                // Coreclr build containing the tests and mscorlib
                copyArtifacts("dotnet_coreclr/master/x64_checked_windows_nt_pri1_bld") {
                    excludePatterns('**/testResults.xml', '**/*.ni.dll')
                    buildSelector {
                        buildNumber('${CORECLR_WINDOWS_BUILD}')
                    }
                }

                // Coreclr build we are trying to test
                copyArtifacts("dotnet_coreclr/master/checked_ubuntu") {
                    excludePatterns('**/testResults.xml', '**/*.ni.dll')
                    buildSelector {
                        buildNumber('${CORECLR_BUILD}')
                    }
                }

                def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' + 'master'
                        
                // Corefx components.  We now have full stack builds on all distros we test here, so we can copy straight from CoreFX jobs.
                def osJobName = (os == 'Ubuntu') ? 'ubuntu14.04' : os.toLowerCase()
                copyArtifacts("${corefxFolder}/${osJobName}_release") {
                    includePatterns('bin/build.tar.gz')
                    buildSelector {
                        latestSuccessful(true)
                    }
                }
                        
                // Unpack the corefx binaries
                shell("tar -xf ./bin/build.tar.gz")

                // Unzip the tests first.  Exit with 0
                shell("unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/Windows_NT.${architecture}.${configuration} || exit 0")
                            
                // Execute the tests                               
                shell('./init-tools.sh')

                shell("""sudo bash ./tests/scripts/run-xunit-perf.sh \\
                --testRootDir=\"\${WORKSPACE}/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreClrBinDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --mscorlibDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --coreFxBinDir=\"\${WORKSPACE}/bin/${osGroup}.AnyCPU.Release;\${WORKSPACE}/bin/Unix.AnyCPU.Release;\${WORKSPACE}/bin/AnyOS.AnyCPU.Release\" \\
                --coreFxNativeBinDir=\"\${WORKSPACE}/bin/${osGroup}.${architecture}.Release\"""")
            }
        }

        // Save machinedata.json to /artifact/bin/ Jenkins dir
        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles('perf-*.xml')
        Utilities.addArchival(newJob, archiveSettings)

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        if (isPR) {
            TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
            builder.setGithubContext("${os} Perf Tests")
            builder.triggerOnlyOnComment()
            builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+perf.*")
            builder.triggerForBranch(branch)
            builder.emitTrigger(newJob)
        }
        else {
            // Set a push trigger
            TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
            builder.emitTrigger(newJob)
        }
    } // os
} // isPR
