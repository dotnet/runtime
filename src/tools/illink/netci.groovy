import jobs.generation.Utilities;

def project = GithubProject
def branch = GithubBranchName

def static setRecursiveSubmoduleOption(def job) {
    job.with {
        scm {
            git {
                configure { gitscm ->
                    gitscm / 'extensions' << 'hudson.plugins.git.extensions.impl.SubmoduleOption' {
                        recursiveSubmodules(true)
                    }
                }
            }
        }
    }
}

[true, false].each { isPR ->
    ['Windows_NT'].each { os ->

        def newJob = job(Utilities.getFullJobName(project, os.toLowerCase(), isPR)) {}

        if (os == 'Windows_NT') {
            newJob.with {
                steps {
                    batchFile("build.cmd")
                }
            }
        } else if (os == 'Ubuntu') {
            newJob.with {
                steps {
                    shell("build.sh")
                }
            }
        }
        
        Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')

        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        setRecursiveSubmoduleOption(job)

        if (isPR) {
            Utilities.addGithubPRTriggerForBranch(newJob, branch, "${os} Build")
        } else {
            Utilities.addGithubPushTrigger(newJob)
        }
    }
}
