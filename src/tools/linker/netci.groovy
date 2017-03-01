import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

def static setRecursiveSubmoduleOption(def job) {
    job.with {
        configure {
            it / 'scm' / 'extensions' << 'hudson.plugins.git.extensions.impl.SubmoduleOption' {
                recursiveSubmodules(true)
            }
        }
    }
}

[true, false].each { isPR ->
    ['Windows_NT', 'Ubuntu'].each { os ->

        def newJob = job(Utilities.getFullJobName(project, os.toLowerCase(), isPR)) {}

        if (os == 'Windows_NT') {
            newJob.with {
                steps {
                    batchFile("cd corebuild && restore.cmd")
                    batchFile("cd corebuild && build.cmd")
                }
            }
        } else if (os == 'Ubuntu') {
            newJob.with {
                steps {
                    shell("cd corebuild && ./restore.sh")
                    shell("cd corebuild && ./build.sh")
                }
            }
        }
        
        Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        setRecursiveSubmoduleOption(newJob)

        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} Build")
        } else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}
