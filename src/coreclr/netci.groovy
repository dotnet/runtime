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
                Utilities.addArchival(newJob, "bin/Product/**,bin/tests/**,bin/obj/**/BclRewriter/mscorlib.apiClosure.xml,bin/obj/**/BclRewriter/mscorlib.implClosure.xml,bin/obj/**/GeneratedAssemblyInfo.cs,bin/obj/**/GeneratedVersion.h,bin/obj/**/moduleName.rsp,bin/obj/**/mscorlib.csproj.FileListAbsolute.txt,bin/obj/**/mscorlib.dll,bin/obj/**/mscorlib.pdb,bin/obj/**/mscorlib.resources,bin/obj/**/NativeVersion.res", "bin/tests/obj/**")
            } else {
                // Add .NET results for the 
                Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                Utilities.addArchival(newJob, "bin/Product/**")
            }
        }
    }
}

// Ubuntu cross compiled arm and arm64 builds
// Scheduled for nightly and on-demand PR for now

def os = 'Ubuntu'
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