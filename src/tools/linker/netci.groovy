import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

[true, false].each { isPR ->
    ['Windows_NT'].each { os ->

        def newJob = job(Utilities.getFullJobName(project, os.toLowerCase(), isPR)) {}

        if (os == 'Windows_NT') {
            newJob.with {
                steps {
                    batchFile(".\build.cmd")
                }
            }
        } else if (os == 'Ubuntu') {
            newJob.with {
                steps {
                    shell("\build.sh")
                }
            }
        }
        
        Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} Build")
        } else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}
