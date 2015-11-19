// Import the utility functionality.

import jobs.generation.Utilities;

def project = 'dotnet/corefx'

// **************************
// Define code coverage build
// **************************

[true, false].each { isPR -> 
    def newJob = job(Utilities.getFullJobName(project, 'code_coverage_windows', isPR)) {
        steps {
            batchFile('call "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat" && build.cmd /p:Coverage=true')
        }
    }
    
    // Set up standard options
    Utilities.standardJobSetup(newJob, project, isPR)
    // Set the machine affinity to windows machines
    Utilities.setMachineAffinity(newJob, 'Windows_NT')
    // Publish reports
    Utilities.addHtmlPublisher(newJob, 'bin/tests/coverage', 'Code Coverage Report', 'index.htm')
    // Archive results.
    Utilities.addArchival(newJob, '**/coverage/*,msbuild.log')
    // Set triggers
    if (isPR) {
        // Set PR trigger
        Utilities.addGithubPRTrigger(newJob, 'Code Coverage Windows Debug', 'test code coverage')
    }
    else {
        // Set a periodic trigger
        Utilities.addPeriodicTrigger(newJob, '@daily')
    }
}

// **************************
// Define code formatter check build
// **************************

[true, false].each { isPR -> 
    def newJob = job(Utilities.getFullJobName(project, 'native_code_format_check', isPR)) {
        steps {
            shell('python src/Native/format-code.py checkonly')
        }
    }
    
    // Set up standard options.
    Utilities.standardJobSetup(newJob, project, isPR)
    // Set the machine affinity to Ubuntu machines
    Utilities.setMachineAffinity(newJob, 'Ubuntu')
    if (isPR) {
        // Set PR trigger.  Only trigger when the phrase is said.
        Utilities.addGithubPRTrigger(newJob, 'Code Formatter Check', '(?i).*test\\W+code\\W+formatter\\W+check.*', true)
    }
    else {
        // Set a push trigger
        Utilities.addGithubPushTrigger(newJob)
    }
}

// **************************
// Define outerloop windows testing.  Run locally on each machine.
// **************************

def osShortName = ['Windows 10': 'win10', 'Windows 7' : 'win7', 'Windows_NT' : 'windows_nt']
[true, false].each { isPR ->
    ['Windows 10', 'Windows 7', 'Windows_NT'].each { os ->
        ['Debug', 'Release'].each { configuration ->

            def newJobName = "outerloop_${osShortName[os]}_${configuration.toLowerCase()}"

            def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat\" && Build.cmd /p:Configuration=${configuration} /p:WithCategories=\"InnerLoop;OuterLoop\" /p:TestWithLocalLibraries=true")
                }
            }

            // Set the affinity.  OS name matches the machine affinity.
            Utilities.setMachineAffinity(newJob, os)
            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR)
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newJob, 'bin/tests/**/testResults.xml')

            // Set up appropriate triggers.  PR on demand, otherwise nightly
            if (isPR) {
                // Set PR trigger.
                // TODO: More elaborate regex trigger?
                Utilities.addGithubPRTrigger(newJob, "OuterLoop ${os} ${configuration}", "(?i).*test\\W+outerloop.*")
            }
            else {
                // Set a periodic trigger
                Utilities.addPeriodicTrigger(newJob, '@daily')
            }
        }
    }
}
