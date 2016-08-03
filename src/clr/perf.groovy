[true, false].each { isPR ->
    [‘Windows_NT’].each { os ->
        def newJob = job(Utilities.getFullJobName(project, PERF JOB NAME, isPR)) {
            // Set the label.
            steps {
                    // Batch
                    batchFile("build-product.cmd debug x64")
                    batchFile("run-xunit-perf.cmd")
            }
        }

        Utilities.setMachineAffinity(newJob, os, 'latest-or-auto') // Just run against Windows_NT VM’s for now.
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
