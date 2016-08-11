// Import the utility functionality.

import jobs.generation.*;

def project = GithubProject
def branch = GithubBranchName
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)


[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
        def newJob = job(Utilities.getFullJobName(project, "perf_${os}", isPR)) {
            // Set the label.
            steps {
                    // Batch
                    batchFile("set __TestIntermediateDir=int&&build.cmd release x64")
                    batchFile("tests\\runtest.cmd release x64")
                    batchFile("tests\\scripts\\run-xunit-perf.cmd")
            }
        }

        Utilities.setMachineAffinity(newJob, os, 'latest-or-auto-elevated') // Just run against Windows_NT VM’s for now.
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
