// Import the utility functionality.

import jobs.generation.*;

def project = GithubProject
def branch = GithubBranchName
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

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
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} Perf Tests") // Add a PR trigger.
        }
        else {
            // Set a push trigger
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}
