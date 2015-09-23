// Import the utility functionality.

import jobs.generation.Utilities;

def project = 'dotnet/corefx'

// **************************
// Define code coverage build
// **************************

// TODO: Move this functionality into dotnet-ci once verified correct:

def static addArchival(def job, def filesToArchive, def filesToExclude = '',
    def doNotFailIfNothingArchived = false, def archiveOnlyIfSuccessful = true) {
    
    job.with {
        publishers {
            archiveArtifacts {
                pattern(filesToArchive)
                exclude(filesToExclude)
                onlyIfSuccessful(archiveOnlyIfSuccessful)
                allowEmpty(doNotFailIfNothingArchived)
            }
        }
    }
}

def static addHtmlPublisher(def job, def reportDir, def name, def reportHtml, def keepAllReports = true) {
    job.with {
        publishers {
            publishHtml {
                report(reportDir) {
                    reportName(name)
                    keepAll(keepAllReports)
                    reportFiles(reportHtml)
                }
            }
        }
    }
}

def static addPeriodicTrigger(def job, def cronString) {
    job.with {
        triggers {
            cron(cronString)
        }
    }
}

// Define build string
def codeCoverageBuildString = '''call "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat" && build.cmd /p:Coverage=true'''

// Generate a rolling (12 hr job) and a PR job that can be run on demand

def rollingCCJob = job(Utilities.getFullJobName(project, 'code_coverage_windows', false)) {
  label('windows')
  steps {
    batchFile(codeCoverageBuildString)
  }
}

def prCCJob = job(Utilities.getFullJobName(project, 'code_coverage_windows', true)) {
  label('windows')
  steps {
    batchFile(codeCoverageBuildString)
  }
}

// For both jobs, archive the coverage info and publish an HTML report
[rollingCCJob, prCCJob].each { newJob ->
    addHtmlPublisher(newJob, 'bin/tests/coverage', 'Code Coverage Report', 'index.html')
    addArchival(newJob, '**/coverage/*,msbuild.log')
}

Utilities.addScm(rollingCCJob, project)
Utilities.addStandardOptions(rollingCCJob)
Utilities.addStandardNonPRParameters(rollingCCJob)
addPeriodicTrigger(rollingCCJob, '@daily')
             
Utilities.addPRTestSCM(prCCJob, project)
Utilities.addStandardOptions(prCCJob)
Utilities.addStandardPRParameters(prCCJob, project)
Utilities.addGithubPRTrigger(prCCJob, 'Code Coverage Windows Debug', 'test code coverage please')