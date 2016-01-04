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
      
// Innerloop build OS's
def osList = ['Ubuntu', 'Debian8.2', 'OSX', 'Windows_NT', 'FreeBSD', 'CentOS7.1', 'OpenSUSE13.2']

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

def static getBuildJobName(def configuration, def architecture, def os) {
    // If the architecture is x64, do not add that info into the build name.
    // Need to change around some systems and other builds to pick up the right builds
    // to do that.
    
    switch (architecture) {
        case 'x64':
            // For now we leave x64 off of the name for compatibility with other jobs
            return configuration.toLowerCase() + '_' + os.toLowerCase()
        case 'arm64':
        case 'arm':
            // These are cross builds
            return architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
        case 'x86':
            return architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + os.toLowerCase()
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }
}

// **************************
// Define the basic inner loop builds for PR and commit.  This is basically just the set
// of coreclr builds over linux/osx/freebsd/windows and debug/release.  In addition, the windows
// builds will do a couple extra steps.
// **************************

// Adds a trigger for the PR build if one is needed.  If isFlowJob is true, then this is the
// flow job that rolls up the build and test for non-windows OS's
def static addPRTrigger(def job, def architecture, def os, def configuration, isFlowJob) {
    def osGroup = getOSGroup(os)
    switch (architecture) {
        case 'x64':
            switch (os) {
                case 'OpenSUSE13.2':
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build", '(?i).*test\\W+suse.*')
                    break
                case 'Debian8.2':
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build", '(?i).*test\\W+debian.*')
                    break
                case 'Ubuntu':
                case 'OSX':
                    // Only add the trigger for the flow job and only for Release, since Debug is too slow
                    if (isFlowJob && configuration == 'Release') {
                        Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test")
                    }
                    break
                case 'CentOS7.1':
                case 'OpenSUSE13.2':
                    assert !isFlowJob
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build")
                    break
                case 'Windows_NT':
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test")
                    break
                case 'FreeBSD':
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build")
                    break
                default:
                    println("Unknown os: ${os}");
                    assert false
                    break
            }
            break
        case 'arm64':
        case 'arm':
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
            // For windows, x86 runs by default
            if (os == 'Windows_NT') {
                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build")
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

[true, false].each { buildPri1Tests ->
    [true, false].each { isPR ->
        ['arm', 'arm64', 'x64', 'x86'].each { architecture ->
            ['Debug', 'Release'].each { configuration ->
                osList.each { os ->
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

                    //Skip Pri-1 test build unless it's for Windows x64 Release 
                    if (buildPri1Tests && (architecture != 'x64' || configuration != 'Release' || os != 'Windows_NT'))
                    {
                        println("Skipping unneeded Priority 1 tests");
                        return
                    }
                
                    // Calculate names
                    def lowerConfiguration = configuration.toLowerCase()
                    if (buildPri1Tests) {
                        def jobName = getBuildJobName(configuration, architecture, os) + "_pri1"
                    }
                    else {
                        def jobName = getBuildJobName(configuration, architecture, os)
                    }
                    
                    // Create the new job
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {}

                    setMachineAffinity(newJob, os, architecture)
                    // Add all the standard options
                    Utilities.standardJobSetup(newJob, project, isPR)
                    if (buildPri1Tests){
                        Utilities.addPeriodicTrigger(newJob, '@daily')
                    }
                    else if (isPR) {
                        addPRTrigger(newJob, architecture, os, configuration, false)
                    }
                    else {
                        Utilities.addGithubPushTrigger(newJob)
                    }
                
                    def buildCommands = [];
                    def osGroup = getOSGroup(os)
                
                    // Calculate the build steps, archival, and xunit results
                    switch (os) {
                        case 'Windows_NT':
                            switch (architecture) {
                                case 'x64':
                                case 'x86':
                                    if (buildPri1Tests)
                                    {
                                        buildCommands += "build.cmd ${lowerConfiguration} ${architecture} Priority 1"
                                    } 
                                    else
                                    {
                                        buildCommands += "build.cmd ${lowerConfiguration} ${architecture}"
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
                                    if (architecture == 'x86' && !isPR) {
                                        Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml')
                                    }
                                    break
                                case 'arm64':
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
} // buildPri1Tests

// Create the Linux/OSX coreclr test leg for debug and release.
[true, false].each { buildPri1Tests ->
    [true, false].each { isPR ->
        // Architectures.  x64 only at this point
        ['x64'].each { architecture ->
            // Put the OS's supported for coreclr cross testing here
            ['Ubuntu', 'OSX'].each { os ->
                ['Debug', 'Release'].each { configuration ->
                    // Skip unnecessary Pri1 legs
                    if (buildPri1Tests && (isPR || configuration != 'Release'))
                    {
                        println("Skipping unneeded Pri1 build")
                        return
                    }
                    def lowerConfiguration = configuration.toLowerCase()
                    def osGroup = getOSGroup(os)
                    def jobName = getBuildJobName(configuration, architecture, os) + "_tst"
                    def inputCoreCLRBuildName = Utilities.getFolderName(project) + '/' + 
                        Utilities.getFullJobName(project, getBuildJobName(configuration, architecture, os), isPR)
                    def name_suffix = ""
                    if (buildPri1Tests)
                    {
                        name_suffix = "_pri1";
                    }
                    def inputWindowTestsBuildName = Utilities.getFolderName(project) + '/' + 
                        Utilities.getFullJobName(project, getBuildJobName(configuration, architecture, 'windows_nt') + name_suffix, isPR)
                
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR) + name_suffix) {
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
            --coreFxNativeBinDir=\"\${WORKSPACE}/bin/${osGroup}.${architecture}.Release\"""")
                        }
                    }
                
                    setMachineAffinity(newJob, os, architecture)
                    Utilities.standardJobSetup(newJob, project, isPR)
                    Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')
                
                    // Create a build flow to join together the build and tests required to run this
                    // test.
                    // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
                    // Linux CoreCLR test
                    def flowJobName = getBuildJobName(configuration, architecture, os) + "_flow"
                    def fullTestJobName = Utilities.getFolderName(project) + '/' + newJob.name
                    def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR) + name_suffix) {
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
                    if (buildPri1Tests) {
                        Utilities.addPeriodicTrigger(newFlowJob, '@daily')
                    }
                    else if (isPR) {
                        addPRTrigger(newFlowJob, architecture, os, configuration, true)
                    }
                    else {
                        Utilities.addGithubPushTrigger(newFlowJob)
                    }
                }
            }
        }
    }
}