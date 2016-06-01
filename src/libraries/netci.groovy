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
                             'Ubuntu16.04' : 'ubuntu.14.04-x64',
                             'Fedora23' : 'ubuntu.14.04-x64',
                             'Debian8.4' : 'debian.8-x64',
                             'CentOS7.1' : 'centos.7-x64',
                             'OpenSUSE13.2' : 'ubuntu.14.04-x64',
                             'RHEL7.2': 'rhel.7-x64']

def osShortName = ['Windows 10': 'win10',
                   'Windows 7' : 'win7',
                   'Windows_NT' : 'windows_nt',
                   'Ubuntu14.04' : 'ubuntu14.04',
                   'OSX' : 'osx',
                   'Windows Nano 2016' : 'winnano16',
                   'Ubuntu16.04' : 'ubuntu16.04',
                   'CentOS7.1' : 'centos7.1',
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
// Define outerloop testing for linux OSes that can't build.  Run locally on each machine.
// **************************
def outerloopLinuxOSes = ['Ubuntu16.04', 'CentOS7.1', 'OpenSUSE13.2', 'RHEL7.2', 'Fedora23', 'Debian8.4']
[true, false].each { isPR ->
    ['Debug', 'Release'].each { configurationGroup ->
        outerloopLinuxOSes.each { os ->
            def osGroup = osGroupMap[os]

            //
            // First define the nativecomp build
            //
            
            def newNativeCompBuildJobName = "outerloop_nativecomp_${os.toLowerCase()}_${configurationGroup.toLowerCase()}"
            
            def newNativeCompJob = job(Utilities.getFullJobName(project, newNativeCompBuildJobName, isPR)) {
                steps {
                    shell("./build.sh native x64 ${configurationGroup.toLowerCase()}")
                }
            }
            
            // Set the affinity.  Use the 'latest or auto' version to pick up
            // new auto images.
            Utilities.setMachineAffinity(newNativeCompJob, os, 'outer-latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newNativeCompJob, project, isPR, "*/${branch}")
            // Add archival for the built data.
            Utilities.addArchival(newNativeCompJob, "bin/**")
            
            //
            // First we set up a build job that builds the corefx repo on Windows
            //
            
            def newBuildJobName = "outerloop_${os.toLowerCase()}_${configurationGroup.toLowerCase()}_bld"

            def newBuildJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:ConfigurationGroup=${configurationGroup} /p:TargetOS=${osGroup} /p:OSGroup=${osGroup} /p:SkipTests=true /p:TestNugetRuntimeId=${targetNugetRuntimeMap[os]} /p:WithoutCategories=IgnoreForCI /p:TestWithoutNativeImages=true /p:Outerloop=true")
                    // Package up the results.
                    batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack . bin packages")
                }
            }

            // Set the affinity.  All of these run on Windows currently.
            Utilities.setMachineAffinity(newBuildJob, 'Windows_NT', 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newBuildJob, project, isPR, "*/${branch}")
            // Archive the results
            Utilities.addArchival(newBuildJob, "bin/build.pack,msbuild.log")

            //
            // Then we set up a job that runs the test on the target OS
            //
            
            def fullNativeCompBuildJobName = projectFolder + '/' + newNativeCompJob.name
            def fullCoreFXBuildJobName = projectFolder + '/' + newBuildJob.name
            
            def newTestJobName = "outerloop_${os.toLowerCase()}_${configurationGroup.toLowerCase()}_tst"
            
            def newTestJob = job(Utilities.getFullJobName(project, newTestJobName, isPR)) { }

            addCopyCoreClrAndRunTestSteps(newTestJob, branch, os, osGroup, fullNativeCompBuildJobName, fullCoreFXBuildJobName, configurationGroup, 'Release', true, false)
            
            // Set the affinity.  All of these run on the target
            Utilities.setMachineAffinity(newTestJob, os, 'outer-latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newTestJob, project, isPR, "*/${branch}")
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newTestJob, '**/testResults.xml')
            
            //
            // Then we set up a flow job that runs the build and the nativecomp build in parallel and then executes.
            // the test job
            //
            
            def fullCoreFXTestJobName = projectFolder + '/' + newTestJob.name
            def flowJobName = "outerloop_${os.toLowerCase()}_${configurationGroup.toLowerCase()}"
            def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR)) {
                buildFlow("""
                    parallel (
                        { nativeCompBuild = build(params, '${fullNativeCompBuildJobName}') },
                        { coreFXBuild = build(params, '${fullCoreFXBuildJobName}') }
                    )
                    
                    // Then run the test job
                    build(params + 
                        [COREFX_BUILD: coreFXBuild.build.number,
                         COREFX_NATIVECOMP_BUILD : nativeCompBuild.build.number], '${fullCoreFXTestJobName}')
                """)
                
                // Needs a workspace
                configure {
                    def buildNeedsWorkspace = it / 'buildNeedsWorkspace'
                    buildNeedsWorkspace.setValue('true')
                }
            }
            
            // Set the affinity.  All of these run on the target
            Utilities.setMachineAffinity(newFlowJob, os, 'outer-latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")
            // Set up triggers
            if (isPR) {
                // Set PR trigger.
                Utilities.addGithubPRTriggerForBranch(newFlowJob, branch, "OuterLoop ${os} ${configurationGroup}", "(?i).*test\\W+outerloop\\W+${os}\\W+${configurationGroup}.*")
            }
            else {
                // Set a push trigger
                Utilities.addPeriodicTrigger(newFlowJob, '@daily')
            }
        }
    }
}

// **************************
// Define outerloop testing for OSes that can build and run.  Run locally on each machine.
// **************************
[true, false].each { isPR ->
    ['Windows 10', 'Windows 7', 'Windows_NT', 'Ubuntu14.04', 'OSX'].each { os ->
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
                        shell("sudo HOME=\$WORKSPACE/tempHome ./build.sh ${configurationGroup.toLowerCase()} /p:ConfigurationGroup=${configurationGroup} /p:Outerloop=true /p:TestWithLocalLibraries=true /p:WithoutCategories=IgnoreForCI")
                    }
                }
            }

            // Set the affinity.  OS name matches the machine affinity.
            if (os == 'Ubuntu14.04') {
                Utilities.setMachineAffinity(newJob, os, "outer-latest-or-auto")    
            }
            else if (os == 'Windows_NT') {
                 Utilities.setMachineAffinity(newJob, os, "latest-or-auto-elevated")
            }
            else {
                Utilities.setMachineAffinity(newJob, os, 'latest-or-auto')
            }

            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newJob, 'bin/tests/**/testResults.xml')

            // Unix runs take more than 2 hours to run, so we set the timeout to be longer.
            if (os == 'Ubuntu14.04' || os == 'OSX') {
                Utilities.setJobTimeout(newJob, 240)
            }

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

// adds steps to a job to download coreclr artifacts and corefx artifacts and invoke run-test.sh.
def static addCopyCoreClrAndRunTestSteps(def job, def coreclrBranch, String os, String osGroup, String fullNativeCompBuildJobName, String fullCoreFXBuildJobName, String configurationGroup, String coreClrConfigurationGroup, boolean isOuterLoop, boolean useServerGC) {
    // Grab the folder name for the coreclr branch
    def coreclrFolder = Utilities.getFolderName(coreclrBranch)
    job.with {
        steps {
            // CoreCLR
            copyArtifacts("dotnet_coreclr/${coreclrFolder}/${coreClrConfigurationGroup.toLowerCase()}_${os.toLowerCase()}") {
                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                buildSelector {
                    latestSuccessful(true)
                }
            }

            // MSCorlib
            copyArtifacts("dotnet_coreclr/${coreclrFolder}/${coreClrConfigurationGroup.toLowerCase()}_windows_nt") {
                includePatterns("bin/Product/${osGroup}*/**")
                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                buildSelector {
                    latestSuccessful(true)
                }
            }

            // Native components
            copyArtifacts(fullNativeCompBuildJobName) {
                includePatterns("bin/**")
                buildSelector {
                    buildNumber('\${COREFX_NATIVECOMP_BUILD}')
                }
            }

            // The tests/corefx components
            copyArtifacts(fullCoreFXBuildJobName) {
                includePatterns('bin/build.pack')
                buildSelector {
                    buildNumber('\${COREFX_BUILD}')
                }
            }

            // Unpack the build data
            shell("unpacker ./bin/build.pack .")
            // Export the LTTNG environment variable and then run the tests
            shell("""export LTTNG_HOME=/home/dotnet-bot
            ${isOuterLoop ? 'sudo' : '' } ./run-test.sh \\
                --configurationGroup ${configurationGroup} \\
                --os ${osGroup} \\
                --corefx-tests \${WORKSPACE}/bin/tests/${osGroup}.AnyCPU.${configurationGroup}/ \\
                --coreclr-bins \${WORKSPACE}/bin/Product/${osGroup}.x64.${coreClrConfigurationGroup}/ \\
                --mscorlib-bins \${WORKSPACE}/bin/Product/${osGroup}.x64.${coreClrConfigurationGroup}/ \\
                --IgnoreForCI \\
                ${useServerGC ? '--serverGc' : ''} ${isOuterLoop ? '--outerloop' : ''}
            ${isOuterLoop ? 'sudo find . -name \"testResults.xml\" -exec chmod 777 {} \\;' : ''}
            """)
        }

        // Add parameters for the input jobs
        parameters {
            stringParam('COREFX_BUILD', '', 'Build number to copy CoreFX test binaries from')
            stringParam('COREFX_NATIVECOMP_BUILD', '', 'Build number to copy CoreFX native components from')
        }
    }
}

// Here are the OS's that needs separate builds and tests.
// We create a build for the native compilation, a build for the build of corefx itself (on Windows)
// and then a build for the test of corefx on the target platform.  Then we link them with a build
// flow job.

def innerLoopNonWindowsOSs = ['Ubuntu16.04', 'Debian8.4', 'CentOS7.1', 'OpenSUSE13.2', 'RHEL7.2', 'Fedora23']
[true, false].each { isPR ->
    ['Debug', 'Release'].each { configurationGroup ->
        innerLoopNonWindowsOSs.each { os ->
            def osGroup = osGroupMap[os]

            //
            // First define the nativecomp build
            //
            
            def newNativeCompBuildJobName = "nativecomp_${os.toLowerCase()}_${configurationGroup.toLowerCase()}"
            
            def newNativeCompJob = job(Utilities.getFullJobName(project, newNativeCompBuildJobName, isPR)) {
                steps {
                    shell("./build.sh native x64 ${configurationGroup.toLowerCase()}")
                }
            }
            
            // Set the affinity.  Use the 'latest or auto' version to pick up
            // new auto images.
            Utilities.setMachineAffinity(newNativeCompJob, os, 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newNativeCompJob, project, isPR, "*/${branch}")
            // Add archival for the built data.
            Utilities.addArchival(newNativeCompJob, "bin/**")
            
            //
            // First we set up a build job that builds the corefx repo on Windows
            //
            
            def newBuildJobName = "${os.toLowerCase()}_${configurationGroup.toLowerCase()}_bld"

            def newBuildJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:ConfigurationGroup=${configurationGroup} /p:TargetOS=${osGroup} /p:OSGroup=${osGroup} /p:SkipTests=true /p:TestNugetRuntimeId=${targetNugetRuntimeMap[os]} /p:WithoutCategories=IgnoreForCI /p:TestWithoutNativeImages=true")
                    // Package up the results.
                    batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack . bin packages")
                }
            }

            // Set the affinity.  All of these run on Windows currently.
            Utilities.setMachineAffinity(newBuildJob, 'Windows_NT', 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newBuildJob, project, isPR, "*/${branch}")
            // Archive the results
            Utilities.addArchival(newBuildJob, "bin/build.pack,msbuild.log")

            //
            // Then we set up a job that runs the test on the target OS
            //
            
            def fullNativeCompBuildJobName = projectFolder + '/' + newNativeCompJob.name
            def fullCoreFXBuildJobName = projectFolder + '/' + newBuildJob.name
            
            def newTestJobName = "${os.toLowerCase()}_${configurationGroup.toLowerCase()}_tst"
            
            def newTestJob = job(Utilities.getFullJobName(project, newTestJobName, isPR)) { }

            addCopyCoreClrAndRunTestSteps(newTestJob, branch, os, osGroup, fullNativeCompBuildJobName, fullCoreFXBuildJobName, configurationGroup, 'Release', false, false)
            
            // Set the affinity.  All of these run on the target
            Utilities.setMachineAffinity(newTestJob, os, 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newTestJob, project, isPR, "*/${branch}")
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newTestJob, '**/testResults.xml')
            
            //
            // Then we set up a flow job that runs the build and the nativecomp build in parallel and then executes.
            // the test job
            //
            
            def fullCoreFXTestJobName = projectFolder + '/' + newTestJob.name
            def flowJobName = "${os.toLowerCase()}_${configurationGroup.toLowerCase()}"
            def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR)) {
                buildFlow("""
                    parallel (
                        { nativeCompBuild = build(params, '${fullNativeCompBuildJobName}') },
                        { coreFXBuild = build(params, '${fullCoreFXBuildJobName}') }
                    )
                    
                    // Then run the test job
                    build(params + 
                        [COREFX_BUILD: coreFXBuild.build.number,
                         COREFX_NATIVECOMP_BUILD : nativeCompBuild.build.number], '${fullCoreFXTestJobName}')
                """)
                
                // Needs a workspace
                configure {
                    def buildNeedsWorkspace = it / 'buildNeedsWorkspace'
                    buildNeedsWorkspace.setValue('true')
                }
            }
            
            // Set the affinity.  All of these run on the target
            Utilities.setMachineAffinity(newFlowJob, os, 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")
            // Set up triggers
            if (isPR) {
                // Set PR trigger.
                // Set of OS's that work currently. 
                if (os in ['OpenSUSE13.2', 'CentOS7.1']) {
                    // TODO #6070: Temporarily disabled due to failing globalization tests on OpenSUSE.
                    if (os != 'OpenSUSE13.2') {
                        Utilities.addGithubPRTriggerForBranch(newFlowJob, branch, "Innerloop ${os} ${configurationGroup} Build and Test")
                    }
                }
                else {
                    Utilities.addGithubPRTriggerForBranch(newFlowJob, branch, "Innerloop ${os} ${configurationGroup} Build and Test", "(?i).*test\\W+${os}.*")
                }
            }
            else {
                // Set a push trigger
                Utilities.addGithubPushTrigger(newFlowJob)
            }
        }
    }
}

// Generate the build and test versions for Windows_NT, Ubuntu14.04 and OSX.  When full build/run is supported on a platform, those platforms
// could be removed from above and then added in below.
def supportedFullCyclePlatforms = ['Windows_NT', 'Ubuntu14.04', 'OSX']

[true, false].each { isPR ->
    ['Debug', 'Release'].each { configurationGroup ->
        supportedFullCyclePlatforms.each { os ->
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
                        shell("HOME=\$WORKSPACE/tempHome ./build.sh ${useServerGC} ${configurationGroup.toLowerCase()} /p:ConfigurationGroup=${configurationGroup} /p:TestWithLocalLibraries=true /p:WithoutCategories=IgnoreForCI")
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
                // Set PR trigger.
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${os} ${configurationGroup} Build and Test")
            }
            else {
                // Set a push trigger
                Utilities.addGithubPushTrigger(newJob)
            }
        }
    }
}

// **************************
// Do some cross platform runs with a debug CoreFX *and* a debug CoreCLR. This reuses some existing jobs we've defined above
// **************************
[true, false].each { isPR ->
    innerLoopNonWindowsOSs.each { os ->
        def osGroup = osGroupMap[os]
        def configurationGroup = 'Debug'

        def fullNativeCompBuildJobName = projectFolder + '/' + Utilities.getFullJobName(project, "nativecomp_${os.toLowerCase()}_${configurationGroup.toLowerCase()}", isPR)
        def fullCoreFXBuildJobName = projectFolder + '/' + Utilities.getFullJobName(project, "${os.toLowerCase()}_${configurationGroup.toLowerCase()}_bld", isPR)

        def newTestJobName = "outerloop_${os.toLowerCase()}_${configurationGroup.toLowerCase()}_checked_coreclr_tst"

        def newTestJob = job(Utilities.getFullJobName(project, newTestJobName, isPR)) { }

        addCopyCoreClrAndRunTestSteps(newTestJob, branch, os, osGroup, fullNativeCompBuildJobName, fullCoreFXBuildJobName, configurationGroup, 'Checked', true, false)

        // Set the affinity.  All of these run on the target
        Utilities.setMachineAffinity(newTestJob, os, 'latest-or-auto')
        // Set up standard options.
        Utilities.standardJobSetup(newTestJob, project, isPR, "*/${branch}")
        // Add the unit test results
        Utilities.addXUnitDotNETResults(newTestJob, '**/testResults.xml')

        //
        // Then we set up a flow job that runs the build and the nativecomp build in parallel and then executes.
        // the test job
        //
        def fullCoreFXTestJobName = projectFolder + '/' + newTestJob.name
        def flowJobName = "outerloop_${os.toLowerCase()}_${configurationGroup.toLowerCase()}_checked_coreclr"
        def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR)) {
            buildFlow("""
                parallel (
                    { nativeCompBuild = build(params, '${fullNativeCompBuildJobName}') },
                    { coreFXBuild = build(params, '${fullCoreFXBuildJobName}') }
                )

                // Then run the test job
                build(params +
                    [COREFX_BUILD: coreFXBuild.build.number,
                     COREFX_NATIVECOMP_BUILD : nativeCompBuild.build.number], '${fullCoreFXTestJobName}')
            """)

            // Needs a workspace
            configure {
                def buildNeedsWorkspace = it / 'buildNeedsWorkspace'
                buildNeedsWorkspace.setValue('true')
            }
        }

        // Set the affinity.  All of these run on the target
        Utilities.setMachineAffinity(newFlowJob, os, 'latest-or-auto')
        // Set up standard options.
        Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")

        if (isPR) {
            // Set PR trigger.
            Utilities.addGithubPRTriggerForBranch(newFlowJob, branch, "OuterLoop ${os} ${configurationGroup}", "(?i).*test\\W+checked\\W+coreclr\\W+outerloop\\W+${os}.*")
        }
        else {
            // Set a periodic trigger
            Utilities.addPeriodicTrigger(newFlowJob, '@daily')
        }
    }
}

JobReport.Report.generateJobReport(out)
