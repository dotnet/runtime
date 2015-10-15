// Import the utility functionality.

import jobs.generation.Utilities;

def project = 'dotnet/coreclr'

// Map of OS's to labels.  TODO: Maybe move this into the Utils

def machineLabelMap = ['Ubuntu':'ubuntu',
                       'OSX':'mac',
                       'Windows_NT':'windows',
                       'FreeBSD': 'freebsd',
                       'CentOS7.1': 'centos-71',
                       'OpenSUSE13.2': 'openSuSE-132']
                       
// Map of the build OS to the directory that will have the outputs
def osGroupMap = ['Ubuntu':'Linux',
                    'OSX':'OSX',
                    'Windows_NT':'Windows_NT',
                    'FreeBSD':'FreeBSD',
                    'CentOS7.1': 'Linux',
                    'OpenSUSE13.2': 'Linux']
      
// Innerloop build OS's
def osList = ['Ubuntu', 'OSX', 'Windows_NT', 'FreeBSD', 'CentOS7.1', 'OpenSUSE13.2']

def static getBuildJobName(def configuration, def os) {
    // Massage names a bit
    return configuration.toLowerCase() + '_' + os.toLowerCase()
}

// **************************
// Define the basic inner loop builds for PR and commit.  This is basically just the set
// of coreclr builds over linux/osx/freebsd/windows and debug/release.  In addition, the windows
// builds will do a couple extra steps.
// **************************

// Loop over the options and build up the innerloop build matrix

['Debug', 'Release'].each { configuration ->
    osList.each { os ->
        // Calculate names
        def lowerConfiguration = configuration.toLowerCase()
        
        // Calculate job name
        def jobName = getBuildJobName(configuration, os)
        def buildCommand = '';
        
        def osGroup = osGroupMap[os]
        
        // Calculate the build command
        if (os == 'Windows_NT') {
            // On Windows we build the mscorlibs too.
            buildCommand = "build.cmd ${lowerConfiguration} && tests\\runtest.cmd ${lowerConfiguration} && build.cmd ${lowerConfiguration} linuxmscorlib && build.cmd ${lowerConfiguration} freebsdmscorlib && build.cmd ${lowerConfiguration} osxmscorlib"
        }
        else {
            // On other OS's we skipmscorlib but run the pal tests
            buildCommand = "./build.sh skipmscorlib verbose ${lowerConfiguration} && src/pal/tests/palsuite/runpaltests.sh \${WORKSPACE}/bin/obj/${osGroup}.x64.${configuration} \${WORKSPACE}/bin/paltestout"
        }
        
        // Create the new job
        def newCommitJob = job(Utilities.getFullJobName(project, jobName, false)) {
            // Set the label.
            label(machineLabelMap[os])
            steps {
                if (os == 'Windows_NT') {
                    // Batch
                    batchFile(buildCommand)
                }
                else {
                    // Shell
                    shell(buildCommand)
                }
            }
        }

        // Add commit job options
        Utilities.addScm(newCommitJob, project)
        Utilities.addStandardNonPRParameters(newCommitJob)
        Utilities.addGithubPushTrigger(newCommitJob)
        
        // Create the new PR job
        
        def newPRJob = job(Utilities.getFullJobName(project, jobName, true)) {
            // Set the label.
            label(machineLabelMap[os])
            steps {
                if (os == 'Windows_NT') {
                    // Batch
                    batchFile(buildCommand)
                }
                else {
                    // Shell
                    shell(buildCommand)
                }
            }
        }
        
        // Add a PR trigger.  For some OS's, create an explicit trigger
        // PR's are run for everything except SuSE
        if (os != 'OpenSUSE13.2') {
            Utilities.addGithubPRTrigger(newPRJob, "${os} ${configuration} Build")
        }
        Utilities.addPRTestSCM(newPRJob, project)
        Utilities.addStandardPRParameters(newPRJob, project)
        
        // Add common options:
        
        [newPRJob, newCommitJob].each { newJob ->
            Utilities.addStandardOptions(newJob)
            
            if (osGroup == 'Windows_NT') {
                Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml')
                Utilities.addArchival(newJob, "bin/Product/**,bin/tests/**", "bin/tests/obj/**")
            } else {
                // Add .NET results for the 
                Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                Utilities.addArchival(newJob, "bin/Product/**")
                Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**")
            }
        }
    }
}

// Ubuntu cross compiled arm and arm64 builds
// Scheduled for nightly and on-demand PR for now
// Cross compiled OS names go here
['Ubuntu'].each { os ->
    [true, false].each { isPR ->
        ['Debug', 'Release'].each { configuration ->
            def lowerConfiguration = configuration.toLowerCase()
            
            // Create the new job
            def newArm64Job = job(Utilities.getFullJobName(project, "arm64_cross_${os.toLowerCase()}_${lowerConfiguration}", isPR)) {
                // Set the label.
                label(machineLabelMap[os])
                steps {
                    shell("""
    echo \"Using rootfs in /opt/aarch64-linux-gnu-root\"
    ROOTFS_DIR=/opt/aarch64-linux-gnu-root ./build.sh skipmscorlib arm64 cross verbose ${lowerConfiguration}""")
                }
            }
            
            if (!isPR) {
                // Add rolling job options
                Utilities.addScm(newArm64Job, project)
                Utilities.addStandardNonPRParameters(newArm64Job)
                Utilities.addPeriodicTrigger(newArm64Job, '@daily')
                Utilities.addArchival(newArm64Job, "bin/Product/**")
            }
            else {
                // Add PR job options
                Utilities.addPRTestSCM(newArm64Job, project)
                Utilities.addStandardPRParameters(newArm64Job, project)
                Utilities.addGithubPRTrigger(newArm64Job, "Arm64 ${os} cross ${configuration} Build", '@dotnet-bot test arm')
            }
            
            // Create the new job
            def newArmJob = job(Utilities.getFullJobName(project, "arm_cross_${os.toLowerCase()}_${lowerConfiguration}", isPR)) {
                // Set the label.
                label(machineLabelMap[os])
                steps {
                    shell("""
    echo \"Using rootfs in /opt/arm-liux-genueabihf-root\"
    ROOTFS_DIR=/opt/arm-liux-genueabihf-root ./build.sh skipmscorlib arm cross verbose ${lowerConfiguration}""")
                }
            }
            
            if (!isPR) {
                // Add rolling job options
                Utilities.addScm(newArmJob, project)
                Utilities.addStandardNonPRParameters(newArmJob)
                Utilities.addPeriodicTrigger(newArmJob, '@daily')
                Utilities.addArchival(newArmJob, "bin/Product/**")
            }
            else {
                // Add PR job options
                Utilities.addPRTestSCM(newArmJob, project)
                Utilities.addStandardPRParameters(newArmJob, project)
                Utilities.addGithubPRTrigger(newArmJob, "Arm ${os} cross ${configuration} Build", '@dotnet-bot test arm')
            }
            
            [newArmJob, newArm64Job].each { newJob ->
                Utilities.addStandardOptions(newJob)
            }
        }
    }
}

// Create the Linux coreclr test leg for debug and release.
// Put the OS's supported for coreclr cross testing here
['Ubuntu'].each { os ->
    [true, false].each { isPR ->
        ['Debug', 'Release'].each { configuration ->
            
            def lowerConfiguration = configuration.toLowerCase()
            def osGroup = osGroupMap[os]
            def jobName = getBuildJobName(configuration, os) + "_tst"
            def inputCoreCLRBuildName = Utilities.getFolderName(project) + '/' + getBuildJobName(configuration, os)
            def inputWindowTestsBuildName = Utilities.getFolderName(project) + '/' + getBuildJobName(configuration, 'windows_nt')
            
            def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
                // Set the label.
                label(machineLabelMap[os])
                
                // Add parameters for the inputs
                
                parameters {
                    stringParam('CORECLR_WINDOWS_BUILD', '', 'Build number to copy CoreCLR windows test binaries from')
                    stringParam('CORECLR_LINUX_BUILD', '', 'Build number to copy CoreCLR linux binaries from')
                }
                
                steps {
                    // Set up the copies
                    
                    // Coreclr build we are trying to test
                    
                    copyArtifacts(inputCoreCLRBuildName) {
                        excludePatterns('**/testResults.xml', '**/*.ni.dll')
                        buildSelector {
                            buildNumber('${CORECLR_LINUX_BUILD}')
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
                    copyArtifacts("dotnet_corefx_linux_nativecomp_debug") {
                        includePatterns('bin/**')
                        buildSelector {
                            latestSuccessful(true)
                        }
                    }
                    
                    // Corefx linux binaries
                    copyArtifacts("dotnet_corefx_linux_nativecomp_debug") {
                        includePatterns('bin/Linux*/**')
                        buildSelector {
                            latestSuccessful(true)
                        }
                    }
                    
                    // Execute the shell command
                    
                    shell("""
./tests/runtest.sh \\
    --testRootDir='\${WORKSPACE}/bin/tests/Windows_NT.x64.Debug' \\
    --testNativeBinDir='\${WORKSPACE}/bin/obj/Linux.x64.Debug/tests' \\
    --coreClrBinDir='\${WORKSPACE}/bin/Product/Linux.x64.Debug' \\
    --mscorlibDir='\${WORKSPACE}/bin/Product/Linux.x64.Debug' \\
    --coreFxBinDir='\${WORKSPACE}/bin/Linux.AnyCPU.Debug' \\
    --coreFxNativeBinDir='\${WORKSPACE}/bin/Linux.x64.Debug'""")
                }
                
                if (!isPR) {
                    // Add rolling job options
                    Utilities.addScm(newJob, project)
                    Utilities.addStandardNonPRParameters(newJob)
                }
                else {
                    // Add PR job options
                    Utilities.addPRTestSCM(newJob, project)
                    Utilities.addStandardPRParameters(newJob, project)
                }
                Utilties.addStandardOptions(newJob)
                Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')
            }
            
            // Create a build flow to join together the build and tests required to run this
            // test.
            // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
            // Linux CoreCLR test
            def flowJobName = getBuildJobName(configuration, os) + "_flow"
            def fullTestJobName = Utilities.getFolderName(project) + '/' + newJob.name
            def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR)) {
                buildFlow("""
// Build the input jobs in parallel
parallel (
    { linuxBuildJob = build(params, '${inputCoreCLRBuildName}') },
    { windowsBuildJob = build(params, '${inputWindowTestsBuildName}') }
)

// And then build the test build
build(params + [CORECLR_LINUX_BUILD: linuxBuildJob.build.number, 
                CORECLR_WINDOWS_BUILD: windowsBuildJob.build.number], 
      '${fullTestJobName}')    
""")

                // Needs a workspace
                configure {
                    def buildNeedsWorkspace = it / 'buildNeedsWorkspace'
                    buildNeedsWorkspace.setValue('true')
                }
            }
            
            if (isPR) {
                Utilities.addPRTestSCM(newFlowJob, project)
                Utilities.addStandardPRParameters(newFlowJob, project)
                Utilities.addGithubPRTrigger(newFlowJob, "Linux ${configuration} Build and Test", '@dotnet-bot test linux')
            }
            else {
                Utilities.addScm(newFlowJob, project)
                Utilities.addStandardNonPRParameters(newFlowJob)
                Utilities.addGithubPushTrigger(newFlowJob)
            }
            
            Utilities.addStandardOptions(newFlowJob)
        }
    }
}

