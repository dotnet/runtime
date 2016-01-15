// Import the utility functionality.

import jobs.generation.Utilities;

def project = GithubProject
                       
def static getOSGroup(def os) {
    def osGroupMap = ['Ubuntu':'Linux',
        'Debian8.2':'Linux',
        'OSX':'OSX',
        'Windows_NT':'Windows_NT',
        'FreeBSD':'FreeBSD',
        'CentOS7.1': 'Linux',
        'OpenSUSE13.2': 'Linux']
    def osGroup = osGroupMap.get(os, null) 
    assert osGroup != null : "Could not find os group for ${os}"
    return osGroupMap[os]
}

class Constants {
    // Innerloop build OS's
    def static osList = ['Ubuntu', 'Debian8.2', 'OSX', 'Windows_NT', 'FreeBSD', 'CentOS7.1', 'OpenSUSE13.2']
    def static crossList = ['Ubuntu', 'OSX']
}

def static setMachineAffinity(def job, def os, def architecture) {
    if (architecture == 'arm64' && os == 'Windows_NT') {
        // For cross compilation
        job.with {
            label('arm64')
        }
    } else {
        return Utilities.setMachineAffinity(job, os);
    }
}

// Calculates the name of the build job based on some typical parameters.
def static getBuildJobName(def configuration, def architecture, def os, def scenario) {
    // If the architecture is x64, do not add that info into the build name.
    // Need to change around some systems and other builds to pick up the right builds
    // to do that.
    
    def suffix = scenario != 'default' ? "_${scenario}" : '';
    def baseName = ''
    switch (architecture) {
        case 'x64':
            // For now we leave x64 off of the name for compatibility with other jobs
            baseName = configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'arm64':
        case 'arm':
            // These are cross builds
            baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'x86':
            baseName = architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }
    
    return baseName + suffix
}

// **************************
// Define the basic inner loop builds for PR and commit.  This is basically just the set
// of coreclr builds over linux/osx/freebsd/windows and debug/release/checked.  In addition, the windows
// builds will do a couple extra steps.
// **************************

// Adds a trigger for the PR build if one is needed.  If isFlowJob is true, then this is the
// flow job that rolls up the build and test for non-windows OS's
def static addTriggers(def job, def isPR, def architecture, def os, def configuration, def scenario, def isFlowJob) {
    // Non pull request builds.
    if (!isPR) {
        // Check scenario.
        switch (scenario) {
            case 'default':
                switch (architecture)
                {
                    case 'x64':
                    case 'x86':
                        if (isFlowJob || os == 'Windows_NT') {
                            // default gets a push trigger for everything
                            Utilities.addGithubPushTrigger(job)
                        }
                        break
                    case 'arm':
                    case 'arm64':
                        Utilities.addGithubPushTrigger(job)
                        break
                    default:
                        println("Unknown architecture: ${architecture}");
                        assert false
                        break
                }
                break
            case 'pri1':
                // Pri one gets a daily build, and only for release
                if (architecture == 'x64' && configuration == 'Release') {
                    // We don't expect to see a job generated except in these scenarios
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (isFlowJob || os == 'Windows_NT') {
                        Utilities.addPeriodicTrigger(job, '@daily')
                    }
                }
                break
            case 'ilrt':
                // ILASM/ILDASM roundtrip one gets a daily build, and only for release
                if (architecture == 'x64' && configuration == 'Release') {
                    // We don't expect to see a job generated except in these scenarios
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (isFlowJob || os == 'Windows_NT') {
                        Utilities.addPeriodicTrigger(job, '@daily')
                    }
                }
                break
            default:
                println("Unknown scenario: ${scenario}");
                assert false
                break
        }
        return
    }
    // Pull request builds.  Generally these fall into two categories: default triggers and on-demand triggers
    // We generally only have a distinct set of default triggers but a bunch of on-demand ones.
    def osGroup = getOSGroup(os)
    switch (architecture) {
        case 'x64':
            switch (os) {
                case 'OpenSUSE13.2':
                    assert !isFlowJob
                    assert scenario == 'default'
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build", '(?i).*test\\W+suse.*')
                    break
                case 'Debian8.2':
                    assert !isFlowJob
                    assert scenario == 'default'
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build", '(?i).*test\\W+debian.*')
                    break
                case 'Ubuntu':
                case 'OSX':
                    // Triggers on the non-flow jobs aren't necessary here
                    if (!isFlowJob) {
                        break
                    }
                    switch (scenario) {
                        case 'default':
                            if (configuration == 'Release') {
                                // Default trigger
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test")
                            }
                            break
                        case 'pri1':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Priority 1 Build and Test", "(?i).*test\\W+${os}\\W+pri1.*")
                            }
                            break
                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+ilrt.*")
                            }
                            break
                        default:
                            println("Unknown scenario: ${scenario}");
                            assert false
                            break
                    }
                    break
                case 'CentOS7.1':
                case 'OpenSUSE13.2':
                    assert !isFlowJob
                    assert scenario == 'default'
                    if (configuration != 'Checked') {
                        Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build")
                    }
                    break
                case 'Windows_NT':
                    switch (scenario) {
                        case 'default':
                            // Default trigger
                            if (configuration != 'Checked') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test")
                            }
                            break
                        case 'pri1':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Priority 1 Build and Test", "(?i).*test\\W+${os}\\W+pri1.*")
                            }
                            break
                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+ilrt.*")
                            }
                            break
                        default:
                            println("Unknown scenario: ${scenario}");
                            assert false
                            break
                    }
                    break
                case 'FreeBSD':
                    assert scenario == 'default'
                    if (configuration != 'Checked') {
                        Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build")
                    }
                    break
                default:
                    println("Unknown os: ${os}");
                    assert false
                    break
            }
            break
        case 'arm64':
        case 'arm':
            assert scenario == 'default'
            switch (os) {
                case 'Ubuntu':
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} Cross ${configuration} Build", "(?i).*test\\W+${os}\\W+${architecture}.*")
                    break
                case 'Windows_NT':
                    // Set up a private trigger
                    Utilities.addPrivateGithubPRTrigger(job, "${os} ${architecture} Cross ${configuration} Build",
                        "(?i).*test\\W+${architecture}\\W+${osGroup}.*", null, ['jashook', 'RussKeldorph', 'gkhanna79', 'briansul', 'cmckinsey', 'jkotas', 'ramarag', 'markwilkie', 'rahku', 'tzwlai', 'weshaggard'])
                    break
            }
            break
        case 'x86':
            assert scenario == 'default'
            // For windows, x86 runs by default
            if (os == 'Windows_NT') {
                if (configuration != 'Checked') {
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build")
                }
            }
            else {
                // default trigger
                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build", "(?i).*test\\W+${architecture}\\W+${osGroup}.*")
            }
            break
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }
}

// Additional scenario which can alter behavior
['default', 'pri1', 'ilrt'].each { scenario ->
    [true, false].each { isPR ->
        ['arm', 'arm64', 'x64', 'x86'].each { architecture ->
            ['Debug', 'Checked', 'Release'].each { configuration ->
                Constants.osList.each { os ->
                    // Skip totally unimplemented (in CI) configurations.
                    switch (architecture) {
                        case 'arm64':
                            // Windows or cross compiled Ubuntu
                            if (os != 'Windows_NT' && os != 'Ubuntu') {
                                println("Skipping ${os} ${architecture}")
                                return
                            }
                            break
                        case 'arm':
                            // Only Ubuntu cross implemented
                            if (os != 'Ubuntu') {
                                println("Skipping ${os} ${architecture}")
                                return
                            }
                            break
                        case 'x86':
                            // Skip non-windows
                            if (os != 'Windows_NT') {
                                println("Skipping ${os} ${architecture}")
                                return
                            }
                            break
                        case 'x64':
                            // Everything implemented
                            break
                        default:
                            println("Unknown architecture: ${architecture}")
                            assert false
                            break
                    }

                    // Skip scenarios
                    switch (scenario) {
                        case 'pri1':
                            // The pri1 build isn't necessary except for os's in the cross list or Windows_NT (native OS runs)
                            if (os != 'Windows_NT' && !(os in Constants.crossList)) {
                                return
                            }
                            // Only x64 for now
                            if (architecture != 'x64') {
                                return
                            }
                            break
                        case 'ilrt':
                            // The ilrt build isn't necessary except for os's in the cross list or Windows_NT (native OS runs)
                            if (os != 'Windows_NT' && !(os in Constants.crossList)) {
                                return
                            }
                            // Only x64 for now
                            if (architecture != 'x64') {
                                return
                            }
                            break
                        case 'default':
                            // Nothing skipped
                            break
                        default:
                            println("Unknown scenario: ${scenario}")
                            assert false
                            break
                    }
                
                    // Calculate names
                    def lowerConfiguration = configuration.toLowerCase()
                    def jobName = getBuildJobName(configuration, architecture, os, scenario)
                    
                    // Create the new job
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {}

                    setMachineAffinity(newJob, os, architecture)
                    // Add all the standard options
                    Utilities.standardJobSetup(newJob, project, isPR)
                    addTriggers(newJob, isPR, architecture, os, configuration, scenario, false)
                
                    def buildCommands = [];
                    def osGroup = getOSGroup(os)
                
                    // Calculate the build steps, archival, and xunit results
                    switch (os) {
                        case 'Windows_NT':
                            switch (architecture) {
                                case 'x64':
                                case 'x86':
                                    switch (scenario) {
                                        case 'default':
                                            buildCommands += "build.cmd ${lowerConfiguration} ${architecture}"
                                            break
                                        case 'pri1':
                                            buildCommands += "build.cmd ${lowerConfiguration} ${architecture} Priority 1"
                                            break
                                        case 'ilrt':
                                            // First do the build with skiptestbuild and then build the tests with ilasm roundtrip
                                            buildCommands += "build.cmd ${lowerConfiguration} ${architecture} skiptestbuild"
                                            buildCommands += "test\\buildtest.cmd ${lowerConfiguration} ${architecture} ilasmroundtrip"
                                            break
                                        default:
                                            println("Unknown scenario: ${scenario}")
                                            assert false
                                            break
                                    }
                                    
                                    // TEMPORARY. Don't run tests for PR jobs on x86
                                    if (architecture == 'x64' || !isPR) {
                                        buildCommands += "tests\\runtest.cmd ${lowerConfiguration} ${architecture}"
                                    }
                                
                                    // Run the rest of the build    
                                    // Build the mscorlib for the other OS's
                                    buildCommands += "build.cmd ${lowerConfiguration} ${architecture} linuxmscorlib"
                                    buildCommands += "build.cmd ${lowerConfiguration} ${architecture} freebsdmscorlib"
                                    buildCommands += "build.cmd ${lowerConfiguration} ${architecture} osxmscorlib"
                                
                                    // Zip up the tests directory so that we don't use so much space/time copying
                                    // 10s of thousands of files around.
                                    buildCommands += "powershell -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${architecture}.${configuration}', '.\\bin\\tests\\tests.zip')\"";
                                    
                                    // For windows, pull full test results and test drops for x86/x64
                                    Utilities.addArchival(newJob, "bin/Product/**,bin/tests/tests.zip")
                                    // TEMPORARY. Don't run tests for PR jobs on x86
                                    if (architecture == 'x64' || !isPR) {
                                        Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml')
                                    }
                                    break
                                case 'arm64':
                                    assert scenario == 'default'
                                    buildCommands += "build.cmd ${lowerConfiguration} ${architecture} skiptestbuild /toolset_dir C:\\ats"
                                    // Add archival.  No xunit results for x64 windows
                                    Utilities.addArchival(newJob, "bin/Product/**")
                                    break
                                default:
                                    println("Unknown architecture: ${architecture}");
                                    assert false
                                    break
                            }
                            break
                        case 'Ubuntu':
                        case 'Debian8.2':
                        case 'OSX':
                        case 'FreeBSD':
                        case 'CentOS7.1':
                        case 'OpenSUSE13.2':
                            switch (architecture) {
                                case 'x64':
                                case 'x86':
                                    // Build commands are the same regardless of scenario on non-Windows other OS's.
                                    
                                    // On other OS's we skipmscorlib but run the pal tests
                                    buildCommands += "./build.sh skipmscorlib verbose ${lowerConfiguration} ${architecture}"
                                    buildCommands += "src/pal/tests/palsuite/runpaltests.sh \${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration} \${WORKSPACE}/bin/paltestout"
                                
                                    // Basic archiving of the build
                                    Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**")
                                    // And pal tests
                                    Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                                    break
                                case 'arm64':
                                    // We don't run the cross build except on Ubuntu
                                    assert os == 'Ubuntu'
                                    
                                    buildCommands += """echo \"Using rootfs in /opt/aarch64-linux-gnu-root\"
                                        ROOTFS_DIR=/opt/aarch64-linux-gnu-root ./build.sh skipmscorlib arm64 cross verbose ${lowerConfiguration}"""
                                    
                                    // Basic archiving of the build, no pal tests
                                    Utilities.addArchival(newJob, "bin/Product/**")
                                    break
                                case 'arm':
                                    // We don't run the cross build except on Ubuntu
                                    assert os == 'Ubuntu'
                                    buildCommands += """echo \"Using rootfs in /opt/arm-liux-genueabihf-root\"
                                        ROOTFS_DIR=/opt/arm-linux-genueabihf-root ./build.sh skipmscorlib arm cross verbose ${lowerConfiguration}"""
                                        
                                    // Basic archiving of the build, no pal tests
                                    Utilities.addArchival(newJob, "bin/Product/**")
                                    break
                                default:
                                    println("Unknown architecture: ${architecture}");
                                    assert false
                                    break
                            }
                            break
                        default:
                            println("Unknown os: ${os}");
                            assert false
                            break
                    }
                
                    newJob.with {
                        steps {
                            if (os == 'Windows_NT') {
                                buildCommands.each { buildCommand ->
                                    batchFile(buildCommand)
                                }
                            }
                            else {
                                buildCommands.each { buildCommand ->
                                    shell(buildCommand)
                               }
                            }
                        }
                    }
                } // os
            } // configuration
        } // architecture
    } // isPR
} // scenario

// Create the Linux/OSX coreclr test leg for debug and release and each scenario
['default', 'pri1', 'ilrt'].each { scenario ->
    [true, false].each { isPR ->
        // Architectures.  x64 only at this point
        ['x64'].each { architecture ->
            // Put the OS's supported for coreclr cross testing here
            Constants.crossList.each { os ->
                ['Debug', 'Checked', 'Release'].each { configuration ->
                    
                    // Skip scenarios
                    switch (scenario) {
                        case 'pri1':
                            // Nothing skipped
                            break
                        case 'ilrt':
                            // Nothing skipped
                            break
                        case 'default':
                            // Nothing skipped
                            break
                        default:
                            println("Unknown scenario: ${scenario}")
                            assert false
                            break
                    }
                    
                    def lowerConfiguration = configuration.toLowerCase()
                    def osGroup = getOSGroup(os)
                    def jobName = getBuildJobName(configuration, architecture, os, scenario) + "_tst"
                    def inputCoreCLRBuildName = Utilities.getFolderName(project) + '/' + 
                        Utilities.getFullJobName(project, getBuildJobName(configuration, architecture, os, scenario), isPR)
                    def inputWindowTestsBuildName = Utilities.getFolderName(project) + '/' + 
                        Utilities.getFullJobName(project, getBuildJobName(configuration, architecture, 'windows_nt', scenario), isPR)
                    // Enable Server GC for Ubuntu PR builds
                    def serverGCString = ""
                    if (os == 'Ubuntu' && isPR){
                        serverGCString = "--useServerGC"
                    }

                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
                        // Add parameters for the inputs
                    
                        parameters {
                            stringParam('CORECLR_WINDOWS_BUILD', '', 'Build number to copy CoreCLR windows test binaries from')
                            stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
                        }
                    
                        steps {
                            // Set up the copies
                            
                            // Coreclr build we are trying to test
                            
                            copyArtifacts(inputCoreCLRBuildName) {
                                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                                buildSelector {
                                    buildNumber('${CORECLR_BUILD}')
                                }
                            }
                        
                            // Coreclr build containing the tests and mscorlib
                        
                            copyArtifacts(inputWindowTestsBuildName) {
                                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                                buildSelector {
                                    buildNumber('${CORECLR_WINDOWS_BUILD}')
                                }
                            }
                        
                            // Corefx native components
                            def corefxNativeCompBinaries = 
                            copyArtifacts("dotnet_corefx/nativecomp_${os.toLowerCase()}_release") {
                                includePatterns('bin/**')
                                buildSelector {
                                    latestSuccessful(true)
                                }
                            }
                        
                            // CoreFX Linux binaries
                            copyArtifacts("dotnet_corefx/${os.toLowerCase()}_release_bld") {
                                includePatterns('bin/build.pack')
                                buildSelector {
                                    latestSuccessful(true)
                                }
                            }
                        
                            // Unpack the corefx binaries
                            shell("unpacker ./bin/build.pack ./bin")
                        
                            // Unzip the tests first.  Exit with 0
                            shell("unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/Windows_NT.${architecture}.${configuration} || exit 0")
                        
                            // Execute the tests
                            shell("""
        ./tests/runtest.sh \\
            --testRootDir=\"\${WORKSPACE}/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
            --testNativeBinDir=\"\${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
            --coreClrBinDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
            --mscorlibDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
            --coreFxBinDir=\"\${WORKSPACE}/bin/${osGroup}.AnyCPU.Release\" \\
            --coreFxNativeBinDir=\"\${WORKSPACE}/bin/${osGroup}.${architecture}.Release\" \\
            \${serverGCString}""")
                        }
                    }
                
                    setMachineAffinity(newJob, os, architecture)
                    Utilities.standardJobSetup(newJob, project, isPR)
                    Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')
                
                    // Create a build flow to join together the build and tests required to run this
                    // test.
                    // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
                    // Linux CoreCLR test
                    def flowJobName = getBuildJobName(configuration, architecture, os, scenario) + "_flow"
                    def fullTestJobName = Utilities.getFolderName(project) + '/' + newJob.name
                    def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR)) {
                        buildFlow("""
// Build the input jobs in parallel
parallel (
    { coreclrBuildJob = build(params, '${inputCoreCLRBuildName}') },
    { windowsBuildJob = build(params, '${inputWindowTestsBuildName}') }
)
    
// And then build the test build
build(params + [CORECLR_BUILD: coreclrBuildJob.build.number, 
                CORECLR_WINDOWS_BUILD: windowsBuildJob.build.number], '${fullTestJobName}')    
""")
                        // Needs a workspace
                        configure {
                            def buildNeedsWorkspace = it / 'buildNeedsWorkspace'
                            buildNeedsWorkspace.setValue('true')
                        }
                    }

                    Utilities.standardJobSetup(newFlowJob, project, isPR)
                    addTriggers(newFlowJob, isPR, architecture, os, configuration, scenario, true)
                } // configuration
            } // os
        } // architecture
    } // isPR
} // scenario