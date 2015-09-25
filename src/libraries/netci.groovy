// Import the utility functionality.

import jobs.generation.Utilities;

def project = 'dotnet/corefx'

// **************************
// Define code coverage build
// **************************

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
    Utilities.addHtmlPublisher(newJob, 'bin/tests/coverage', 'Code Coverage Report', 'index.htm')
    Utilities.addArchival(newJob, '**/coverage/*,msbuild.log')
}

Utilities.addScm(rollingCCJob, project)
Utilities.addStandardOptions(rollingCCJob)
Utilities.addStandardNonPRParameters(rollingCCJob)
Utilities.addPeriodicTrigger(rollingCCJob, '@daily')
             
Utilities.addPRTestSCM(prCCJob, project)
Utilities.addStandardOptions(prCCJob)
Utilities.addStandardPRParameters(prCCJob, project)
Utilities.addGithubPRTrigger(prCCJob, 'Code Coverage Windows Debug', '@dotnet-bot test code coverage please')