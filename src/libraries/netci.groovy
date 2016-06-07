// Import the utility functionality.

import jobs.generation.Utilities;
import jobs.generation.JobReport;

// The input project name (e.g. dotnet/corefx)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName
// Folder that the project jobs reside in (project/branch)
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

// Globals

// Map of os -> osGroup.
def osGroupMap = ['Ubuntu14.04':'Linux',
                  'Ubuntu16.04':'Linux',
                  'Debian8.4':'Linux',
                  'Fedora23':'Linux',
                  'OSX':'OSX',
                  'Windows_NT':'Windows_NT',
                  'CentOS7.1': 'Linux',
                  'OpenSUSE13.2': 'Linux',
                  'RHEL7.2': 'Linux']

// Map of os -> nuget runtime
def targetNugetRuntimeMap = ['OSX' : 'osx.10.10-x64',
                             'Ubuntu14.04' : 'ubuntu.14.04-x64',
                             'Ubuntu16.04' : 'ubuntu.16.04-x64',
                             'Fedora23' : 'fedora.23-x64',
                             'Debian8.4' : 'debian.8-x64',
                             'CentOS7.1' : 'centos.7-x64',
                             'OpenSUSE13.2' : 'opensuse.13.2-x64',
                             'RHEL7.2': 'rhel.7-x64']

def osShortName = ['Windows 10': 'win10',
                   'Windows 7' : 'win7',
                   'Windows_NT' : 'windows_nt',
                   'Ubuntu14.04' : 'ubuntu14.04',
                   'OSX' : 'osx',
                   'Windows Nano 2016' : 'winnano16',
                   'Ubuntu16.04' : 'ubuntu16.04',
                   'CentOS7.1' : 'centos7.1',
                   'Debian8.4' : 'debian8.4',
                   'OpenSUSE13.2' : 'opensuse13.2',
                   'Fedora23' : 'fedora23',
                   'RHEL7.2' : 'rhel7.2']

// **************************
// Define code coverage build
// **************************

[true, false].each { isPR ->
    ['local', 'nonlocal'].each { localType ->
        def isLocal = (localType == 'local')

        def newJobName = 'code_coverage_windows'
        def batchCommand = 'call "C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\Common7\\Tools\\VsDevCmd.bat" && build.cmd /p:Coverage=true /p:Outerloop=true /p:WithoutCategories=IgnoreForCI'
        if (isLocal) {
            newJobName = "${newJobName}_local"
            batchCommand = "${batchCommand} /p:TestWithLocalLibraries=true"
        }
        def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
            steps {
                batchFile(batchCommand)
            }
        }

        // Set up standard options
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
        // Set the machine affinity to windows machines
        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')
        // Publish reports
        Utilities.addHtmlPublisher(newJob, 'bin/tests/coverage', 'Code Coverage Report', 'index.htm')
        // Archive results.
        Utilities.addArchival(newJob, '**/coverage/*,msbuild.log')
        // Timeout. Code coverage runs take longer, so we set the timeout to be longer.
        Utilities.setJobTimeout(newJob, 180)
        // Set triggers
        if (isPR) {
            if (!isLocal) {
                // Set PR trigger
                Utilities.addGithubPRTriggerForBranch(newJob, branch, 'Code Coverage Windows Debug', '(?i).*test\\W+code\\W+coverage.*')
            }
        }
        else {
            // Set a periodic trigger
            Utilities.addPeriodicTrigger(newJob, '@daily')
        }
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
    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
    // Set the machine affinity to Ubuntu14.04 machines
    Utilities.setMachineAffinity(newJob, 'Ubuntu14.04', 'latest-or-auto')
    if (isPR) {
        // Set PR trigger.  Only trigger when the phrase is said.
        Utilities.addGithubPRTriggerForBranch(newJob, branch, 'Code Formatter Check', '(?i).*test\\W+code\\W+formatter\\W+check.*', true)
    }
    else {
        // Set a push trigger
        Utilities.addGithubPushTrigger(newJob)
    }
}

// **************************
// Define outerloop windows Nano testing.  Run locally on each machine.
// **************************
[true, false].each { isPR ->
    ['Windows Nano 2016'].each { os ->
        ['Debug', 'Release'].each { configurationGroup ->

            def newJobName = "outerloop_${osShortName[os]}_${configurationGroup.toLowerCase()}"
            
            def newBuildJobName = "outerloop_${osShortName[os]}_${configurationGroup.toLowerCase()}_bld"

            def newBuildJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:OSGroup=Windows_NT /p:ConfigurationGroup=${configurationGroup} /p:SkipTests=true /p:Outerloop=true /p:WithoutCategories=IgnoreForCI")
                    // Package up the results.
                    batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack . bin packages")
                }
            }

            // Set the affinity.  All of these run on Windows currently.
            Utilities.setMachineAffinity(newBuildJob, 'Windows_NT', 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newBuildJob, project, isPR, "*/${branch}")
            // Archive the results
            Utilities.addArchival(newBuildJob, "bin/build.pack,run-test.cmd,msbuild.log")
            
            def fullCoreFXBuildJobName = projectFolder + '/' + newBuildJob.name
            def newTestJobName =  "outerloop_${osShortName[os]}_${configurationGroup.toLowerCase()}_tst"
            def newTestJob = job(Utilities.getFullJobName(project, newTestJobName, isPR)) {
                steps {
                    // The tests/corefx components
                    copyArtifacts(fullCoreFXBuildJobName) {
                        includePatterns('bin/build.pack')
                        includePatterns('run-test.cmd')
                        buildSelector {
                            buildNumber('\${COREFX_BUILD}')
                        }
                    }

                    // Unpack the build data
                    batchFile("PowerShell -command \"\"C:\\Packer\\unpacker.ps1 .\\bin\\build.pack . > .\\bin\\unpacker.log\"\"")
                    // Run the tests
                    batchFile("run-test.cmd .\\bin\\tests\\Windows_NT.AnyCPU.${configurationGroup} %WORKSPACE%\\packages")
                    // Run the tests
                    batchFile("run-test.cmd .\\bin\\tests\\AnyOS.AnyCPU.${configurationGroup} %WORKSPACE%\\packages")
                }

                parameters {
                    stringParam('COREFX_BUILD', '', 'Build number to use for copying binaries for nano server bld.')
                }
            }

            // Set the affinity.  All of these run on Windows Nano currently.
            Utilities.setMachineAffinity(newTestJob, os)
            // Set up standard options.
            Utilities.addStandardOptions(newTestJob, isPR)
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newTestJob, 'bin/tests/**/testResults.xml')

            def fullCoreFXTestJobName = projectFolder + '/' + newTestJob.name
            def newJob = buildFlowJob(Utilities.getFullJobName(project, newJobName, isPR)) {
                buildFlow("""
                    b = build(params, '${fullCoreFXBuildJobName}')
                    build(params +
                    [COREFX_BUILD: b.build.number], '${fullCoreFXTestJobName}')
                    """)
            }

            // Set the machine affinity to windows_nt, since git fails on Nano.
            Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

            // Set up appropriate triggers.  PR on demand, otherwise nightly
            if (isPR) {
                // Set PR trigger.
                // TODO: More elaborate regex trigger?
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "OuterLoop ${os} ${configurationGroup}", "(?i).*test\\W+outerloop\\W+${os}\\W+${configurationGroup}.*")
            }
            else {
                // Set a periodic trigger
                Utilities.addPeriodicTrigger(newJob, '@daily')
            }
        }
    }
}

// **************************
// Define outerloop testing for OSes that can build and run.  Run locally on each machine.
// **************************
[true, false].each { isPR ->
    ['Windows 10', 'Windows 7', 'Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04', 'CentOS7.1', 'OpenSUSE13.2', 'RHEL7.2', 'Fedora23', 'Debian8.4', 'OSX'].each { os ->
        ['Debug', 'Release'].each { configurationGroup ->

            def newJobName = "outerloop_${osShortName[os]}_${configurationGroup.toLowerCase()}"

            def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                steps {
                    if (os == 'Windows 10' || os == 'Windows 7' || os == 'Windows_NT') {
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:ConfigurationGroup=${configurationGroup} /p:Outerloop=true /p:WithoutCategories=IgnoreForCI")
                    }
                    else if (os == 'OSX') {
                        shell("HOME=\$WORKSPACE/tempHome ./build.sh ${configurationGroup.toLowerCase()} /p:ConfigurationGroup=${configurationGroup} /p:Outerloop=true /p:TestWithLocalLibraries=true /p:WithoutCategories=IgnoreForCI")
                    }
                    else {
                        shell("sudo HOME=\$WORKSPACE/tempHome ./build.sh ${configurationGroup.toLowerCase()} /p:TestNugetRuntimeId=${targetNugetRuntimeMap[os]} /p:ConfigurationGroup=${configurationGroup} /p:Outerloop=true /p:TestWithLocalLibraries=true /p:WithoutCategories=IgnoreForCI")
                    }
                }
            }

            // Set the affinity.  OS name matches the machine affinity.
            if (os == 'Windows_NT') {
                Utilities.setMachineAffinity(newJob, os, "latest-or-auto-elevated")
            }
            else if (osGroupMap[os] == 'Linux') {
                Utilities.setMachineAffinity(newJob, os, 'outer-latest-or-auto')
            } else {
                Utilities.setMachineAffinity(newJob, os, 'latest-or-auto');
            }

            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newJob, 'bin/tests/**/testResults.xml')

            // Set up appropriate triggers.  PR on demand, otherwise nightly
            if (isPR) {
                // Set PR trigger.
                // TODO: More elaborate regex trigger?
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "OuterLoop ${os} ${configurationGroup}", "(?i).*test\\W+outerloop\\W+${os}\\W+${configurationGroup}.*")
            }
            else {
                // Set a periodic trigger
                Utilities.addPeriodicTrigger(newJob, '@daily')
            }
        }
    }
}

// **************************
// Define perf testing.  Built locally and submitted to Helix.
// **************************

// builds with secrets should never be available for pull requests.
// right now perf tests are only run on Win10 (but can be built on any Windows)
['Windows 10'].each { os ->
    ['Debug', 'Release'].each { configurationGroup ->

        def newJobName = "perf_${osShortName[os]}_${configurationGroup.toLowerCase()}"

        def newJob = job(Utilities.getFullJobName(project, newJobName, /* isPR */ false)) {
            steps {
                helix("Build.cmd /p:Creator=dotnet-bot /p:ArchiveTests=true /p:ConfigurationGroup=${configurationGroup} /p:Configuration=Windows_${configurationGroup} /p:TestDisabled=true /p:EnableCloudTest=true /p:BuildMoniker={uniqueId} /p:TargetQueue=Windows.10.Amd64 /p:TestProduct=CoreFx /p:Branch=master /p:OSGroup=Windows_NT /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken={CloudDropAccessToken} /p:CloudResultsAccessToken={CloudResultsAccessToken} /p:BuildCompleteConnection={BuildCompleteConnection} /p:BuildIsOfficialConnection={BuildIsOfficialConnection} /p:DocumentDbKey={DocumentDbKey} /p:DocumentDbUri=https://hms.documents.azure.com:443/ /p:FuncTestsDisabled=true /p:Performance=true")
            }
            // perf tests can be built on any Windows
            label("windows10 || windows7 || windows")
        }

        // Set up standard options.
        Utilities.standardJobSetup(newJob, project, /* isPR */ false, "*/${branch}")
        
        // Set a periodic trigger
        Utilities.addPeriodicTrigger(newJob, '@daily')

        Utilities.addPrivatePermissions(newJob)
    }
}

// **************************
// Define innerloop testing.  These jobs run on every merge and a subset of them run on every PR, the ones
// that don't run per PR can be requested via a magic phrase.
// **************************
[true, false].each { isPR ->
    ['Debug', 'Release'].each { configurationGroup ->
        ['Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04', 'Debian8.4', 'CentOS7.1', 'OpenSUSE13.2', 'Fedora23', 'RHEL7.2', 'OSX'].each { os ->
            def osGroup = osGroupMap[os]
            def newJobName = "${os.toLowerCase()}_${configurationGroup.toLowerCase()}"

            def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                // On Windows we use the packer to put together everything. On *nix we use tar
                steps {
                    if (os == 'Windows 10' || os == 'Windows 7' || os == 'Windows_NT') {
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:ConfigurationGroup=${configurationGroup} /p:OSGroup=${osGroup} /p:WithoutCategories=IgnoreForCI")
                        batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack .\\bin")
                    }
                    else {
                        // Use Server GC for Ubuntu/OSX Debug PR build & test
                        def useServerGC = (configurationGroup == 'Release' && isPR) ? 'useServerGC' : ''
                        shell("HOME=\$WORKSPACE/tempHome ./build.sh ${useServerGC} ${configurationGroup.toLowerCase()} /p:TestNugetRuntimeId=${targetNugetRuntimeMap[os]} /p:ConfigurationGroup=${configurationGroup} /p:TestWithLocalLibraries=true /p:WithoutCategories=IgnoreForCI")
                        // Tar up the appropriate bits.  On OSX the tarring is a different syntax for exclusion.
                        if (os == 'OSX') {
                            shell("tar -czf bin/build.tar.gz --exclude *.Tests bin/*.${configurationGroup} bin/ref bin/packages")
                        }
                        else {
                            shell("tar -czf bin/build.tar.gz bin/*.${configurationGroup} bin/ref bin/packages --exclude=*.Tests")
                        }
                    }
                }
            }

            // Set the affinity.
            Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newJob, 'bin/tests/**/testResults.xml')
            def archiveContents = "msbuild.log"
            if (os.contains('Windows')) {
                // Packer.exe is a .NET Framework application. When we can use it from the tool-runtime, we can archive the ".pack" file here.
                archiveContents += ",bin/build.pack"
            }
            else {
                archiveContents += ",bin/build.tar.gz"
            }
            // Add archival for the built data.
            Utilities.addArchival(newJob, archiveContents)
            // Set up triggers
            if (isPR) {
                // Set PR trigger, we run Windows_NT, Ubuntu 14.04, CentOS 7.1 and OSX on every PR.
                if ( os == 'Windows_NT' || os == 'Ubuntu14.04' || os == 'CentOS7.1' || os == 'OSX' ) {
                    Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${os} ${configurationGroup} Build and Test")
                }
                else {
                    Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${os} ${configurationGroup} Build and Test", "(?i).*test\\W+innerloop\\W+${os}\\W+${configurationGroup}.*")
                }
            }
            else {
                // Set a push trigger
                Utilities.addGithubPushTrigger(newJob)
            }
        }
    }
}

JobReport.Report.generateJobReport(out)
