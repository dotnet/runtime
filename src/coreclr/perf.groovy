// Import the utility functionality.

import jobs.generation.*;

def project = GithubProject
def branch = GithubBranchName
def projectName = Utilities.getFolderName(project)
def projectFolder = projectName + '/' + Utilities.getFolderName(branch)

def static getOSGroup(def os) {
    def osGroupMap = ['Ubuntu14.04':'Linux',
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
// Setup perflab tests runs
[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
		['x64'].each { architecture ->
			def newJob = job(Utilities.getFullJobName(project, "perf_perflab_${os}", isPR)) {
				// Set the label.
				label('windows_clr_perf')
				wrappers {
					credentialsBinding {
						string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
					}
				}

			if (isPR)
			{
				parameters
				{
					stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
				}
			}
			def configuration = 'Release'
			def runType = isPR ? 'private' : 'rolling'
			def benchViewName = isPR ? 'coreclr private %BenchviewCommitName%' : 'coreclr rolling %GIT_BRANCH_WITHOUT_ORIGIN% %GIT_COMMIT%'
				
				steps {
					// Batch

					batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\"")
					batchFile("C:\\Tools\\nuget.exe install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")
					//Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
					//we have to do it all as one statement because cmd is called each time and we lose the set environment variable
					batchFile("if [%GIT_BRANCH:~0,7%] == [origin/] (set GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%) else (set GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%)\n" +
					"py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name " + "\"" + benchViewName + "\"" + " --user " + "\"dotnet-bot@microsoft.com\"\n" +
					"py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type " + runType)
					batchFile("py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"")
					batchFile("set __TestIntermediateDir=int&&build.cmd release ${architecture}")
					batchFile("tests\\runtest.cmd release ${architecture} GenerateLayoutOnly")
					batchFile("tests\\scripts\\run-xunit-perf.cmd -arch ${architecture} -configuration ${configuration} -testBinLoc bin\\tests\\Windows_NT.${architecture}.Release\\performance\\perflab\\Perflab -library -uploadToBenchview \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" -runtype " + runType)
					batchFile("tests\\scripts\\run-xunit-perf.cmd -arch ${architecture} -configuration ${configuration} -testBinLoc bin\\tests\\Windows_NT.${architecture}.Release\\Jit\\Performance\\CodeQuality -uploadToBenchview \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" -runtype " + runType)
				}
			}

			// Save machinedata.json to /artifact/bin/ Jenkins dir
			def archiveSettings = new ArchivalSettings()
			archiveSettings.addFiles('perf-*.xml')
			archiveSettings.addFiles('perf-*.etl')
			Utilities.addArchival(newJob, archiveSettings)

			Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

			if (isPR) {
				TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
				builder.setGithubContext("${os} CoreCLR Perf Tests")
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
}

// Create the Linux/OSX/CentOS coreclr test leg for debug and release and each scenario
[true, false].each { isPR ->
    ['Ubuntu14.04'].each { os ->
        def newJob = job(Utilities.getFullJobName(project, "perf_${os}", isPR)) {
			
			label('linux_clr_perf')
				wrappers {
					credentialsBinding {
						string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
					}
				}
			
			if (isPR)
			{
				parameters
				{
					stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
				}
			}
			def osGroup = getOSGroup(os)
			def architecture = 'x64'
			def configuration = 'Release'
			def runType = isPR ? 'private' : 'rolling'
			def benchViewName = isPR ? 'coreclr private \$BenchviewCommitName' : 'coreclr rolling \$GIT_BRANCH_WITHOUT_ORIGIN \$GIT_COMMIT'
			
            steps {
                shell("bash ./tests/scripts/perf-prep.sh")
                shell("./init-tools.sh")
				shell("./build.sh ${architecture} ${configuration}")
				shell("GIT_BRANCH_WITHOUT_ORIGIN=\$(echo \$GIT_BRANCH | sed \"s/[^/]*\\/\\(.*\\)/\\1 /\")\n" +
				"python3.5 \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools/submission-metadata.py\" --name " + "\"" + benchViewName + "\"" + " --user " + "\"dotnet-bot@microsoft.com\"\n" +
				"python3.5 \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools/build.py\" git --branch \$GIT_BRANCH_WITHOUT_ORIGIN --type " + runType)
                shell("""sudo -E bash ./tests/scripts/run-xunit-perf.sh \\
                --testRootDir=\"\${WORKSPACE}/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreClrBinDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --mscorlibDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --coreFxBinDir=\"\${WORKSPACE}/corefx\" \\
				--runType=\"${runType}\" \\
				--benchViewOS=\"${os}\" \\
				--uploadToBenchview""")
            }
        }

        // Save machinedata.json to /artifact/bin/ Jenkins dir
        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles('sandbox/perf-*.xml')
        archiveSettings.addFiles('machinedata.json')
        Utilities.addArchival(newJob, archiveSettings)

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

        // For perf, we need to keep the run results longer
        newJob.with {
            // Enable the log rotator
            logRotator {
                artifactDaysToKeep(7)
                daysToKeep(300)
                artifactNumToKeep(25)
                numToKeep(1000)
            }
        }
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
