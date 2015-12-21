// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject

// Globals

// Map of os -> osGroup.
def osGroupMap = ['Ubuntu':'Linux',
                  'Debian8.2':'Linux',
                  'OSX':'OSX',
                  'Windows_NT':'Windows_NT',
                  'FreeBSD':'FreeBSD',
                  'CentOS7.1': 'Linux',
                  'OpenSUSE13.2': 'Linux']
// Map of os -> nuget runtime
def targetNugetRuntimeMap = ['OSX' : 'osx.10.10-x64',
                             'Ubuntu' : 'ubuntu.14.04-x64',
                             'Debian8.2' : 'ubuntu.14.04-x64',
                             'FreeBSD' : 'ubuntu.14.04-x64',
                             'CentOS7.1' : 'ubuntu.14.04-x64',
                             'OpenSUSE13.2' : 'ubuntu.14.04-x64']
                    
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
        Utilities.addGithubPRTrigger(newJob, 'Code Coverage Windows Debug', '(?i).*test\\W+code\\W+coverage.*')
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
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && Build.cmd /p:Configuration=${configuration} /p:WithCategories=\"InnerLoop;OuterLoop\" /p:TestWithLocalLibraries=true")
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

// Here are the OS's that needs separate builds and tests.
// We create a build for the native compilation, a build for the build of corefx itself (on Windows)
// and then a build for the test of corefx on the target platform.  Then we link them with a build
// flow job.

def innerLoopNonWindowsOSs = ['Ubuntu', 'Debian8.2', 'OSX', 'FreeBSD', 'CentOS7.1', 'OpenSUSE13.2']
[true, false].each { isPR ->
    ['Debug', 'Release'].each { configuration ->
        innerLoopNonWindowsOSs.each { os ->
            def osGroup = osGroupMap[os]
            
            //
            // First define the nativecomp build
            //
            
            def newNativeCompBuildJobName = "nativecomp_${os.toLowerCase()}_${configuration.toLowerCase()}"
            
            def newNativeCompJob = job(Utilities.getFullJobName(project, newNativeCompBuildJobName, isPR)) {
                steps {
                    shell("./build.sh native x64 ${configuration.toLowerCase()}")
                }
            }
            
            // Set the affinity.  All of these run on Windows currently.
            Utilities.setMachineAffinity(newNativeCompJob, os)
            // Set up standard options.
            Utilities.standardJobSetup(newNativeCompJob, project, isPR)
            // Add archival for the built data.
            Utilities.addArchival(newNativeCompJob, "bin/**")
            
            //
            // First we set up a build job that builds the corefx repo on Windows
            //
            
            def newBuildJobName = "${os.toLowerCase()}_${configuration.toLowerCase()}_bld"

            def newBuildJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:Configuration=${configuration} /p:OSGroup=${osGroup} /p:SkipTests=true /p:TestNugetRuntimeId=${targetNugetRuntimeMap[os]}")
                    // Package up the results.
                    batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack .\\bin")
                }
            }

            // Set the affinity.  All of these run on Windows currently.
            Utilities.setMachineAffinity(newBuildJob, 'Windows_NT')
            // Set up standard options.
            Utilities.standardJobSetup(newBuildJob, project, isPR)
            // Archive the results
            Utilities.addArchival(newBuildJob, "bin/build.pack,bin/osGroup.AnyCPU.${configuration}/**,bin/ref/**,bin/packages/**,msbuild.log")
            
            //
            // Then we set up a job that runs the test on the target OS
            //
            
            def fullNativeCompBuildJobName = Utilities.getFolderName(project) + '/' + newNativeCompJob.name
            def fullCoreFXBuildJobName = Utilities.getFolderName(project) + '/' + newBuildJob.name
            
            def newTestJobName = "${os.toLowerCase()}_${configuration.toLowerCase()}_tst"
            
            def newTestJob = job(Utilities.getFullJobName(project, newTestJobName, isPR)) {
                steps {
                    // Copy data from other builds.
                    // TODO: Add a new job or allow for copying coreclr from debug build
                    
                    // CoreCLR
                    copyArtifacts("dotnet_coreclr/release_${os.toLowerCase()}") {
                        excludePatterns('**/testResults.xml', '**/*.ni.dll')
                        buildSelector {
                            latestSuccessful(true)
                        }
                    }
                    
                    // MSCorlib
                    copyArtifacts("dotnet_coreclr/release_windows_nt") {
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
                    shell("unpacker ./bin/build.pack ./bin")
                    // Export the LTTNG environment variable and then run the tests
                    shell("""export LTTNG_HOME=/home/dotnet-bot
                    ./run-test.sh \\
                        --configuration ${configuration} \\
                        --os ${osGroup} \\
                        --corefx-tests \${WORKSPACE}/bin/tests/${osGroup}.AnyCPU.${configuration} \\
                        --coreclr-bins \${WORKSPACE}/bin/Product/${osGroup}.x64.Release/ \\
                        --mscorlib-bins \${WORKSPACE}/bin/Product/${osGroup}.x64.Release/
                    """)
                }
                
                // Add parameters for the input jobs
                parameters {
                    stringParam('COREFX_BUILD', '', 'Build number to copy CoreFX test binaries from')
                    stringParam('COREFX_NATIVECOMP_BUILD', '', 'Build number to copy CoreFX native components from')
                }
            }
            
            // Set the affinity.  All of these run on the target
            Utilities.setMachineAffinity(newTestJob, os)
            // Set up standard options.
            Utilities.standardJobSetup(newTestJob, project, isPR)
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newTestJob, '**/testResults.xml')
            
            //
            // Then we set up a flow job that runs the build and the nativecomp build in parallel and then executes.
            // the test job
            //
            
            def fullCoreFXTestJobName = Utilities.getFolderName(project) + '/' + newTestJob.name
            def flowJobName = "${os.toLowerCase()}_${configuration.toLowerCase()}"
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
            Utilities.setMachineAffinity(newFlowJob, os)
            // Set up standard options.
            Utilities.standardJobSetup(newFlowJob, project, isPR)
            // Set up triggers
            if (isPR) {
                // Set PR trigger.
                // Set of OS's that work currently. 
                if (os in ['Ubuntu', 'CentOS7.1']) {
                    Utilities.addGithubPRTrigger(newFlowJob, "Innerloop ${os} ${configuration} Build and Test")
                }
            }
            else {
                // Set a push trigger
                Utilities.addGithubPushTrigger(newFlowJob)
            }
        }
    }
}

// Generate the build and test versions for Windows_NT.  When full build/run is supported on a platform, those platforms
// could be removed from above and then added in below.
def supportedFullCyclePlatforms = ['Windows_NT']

[true, false].each { isPR ->
    ['Debug', 'Release'].each { configuration ->
        supportedFullCyclePlatforms.each { osGroup ->
            def newJobName = "${osGroup.toLowerCase()}_${configuration.toLowerCase()}"

            def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd /p:Configuration=${configuration} /p:OSGroup=${osGroup}")
                    batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack .\\bin")
                }
            }

            // Set the affinity.  All of these run on Windows currently.
            Utilities.setMachineAffinity(newJob, osGroup)
            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR)
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newJob, 'bin/tests/**/testResults.xml')
            // Add archival for the built data.
            Utilities.addArchival(newJob, "bin/build.pack,bin/${osGroup}.AnyCPU.Debug/**,bin/ref/**,bin/packages/**,msbuild.log")
            // Set up triggers
            if (isPR) {
                // Set PR trigger.
                Utilities.addGithubPRTrigger(newJob, "Innerloop ${osGroup} ${configuration} Build and Test")
            }
            else {
                // Set a push trigger
                Utilities.addGithubPushTrigger(newJob)
            }
        }
    }
}