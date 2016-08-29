// Import the utility functionality.

import jobs.generation.*

// The input project name (e.g. dotnet/coreclr)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

// Create a folder for JIT stress jobs
folder('jitstress')
                       
def static getOSGroup(def os) {
    def osGroupMap = ['Ubuntu':'Linux',
        'RHEL7.2': 'Linux',
        'Ubuntu16.04': 'Linux',
        'Debian8.4':'Linux',
        'Fedora23':'Linux',
        'OSX':'OSX',
        'Windows_NT':'Windows_NT',
        'FreeBSD':'FreeBSD',
        'CentOS7.1': 'Linux',
        'OpenSUSE13.2': 'Linux',
        'OpenSUSE42.1': 'Linux',
        'LinuxARMEmulator': 'Linux']
    def osGroup = osGroupMap.get(os, null) 
    assert osGroup != null : "Could not find os group for ${os}"
    return osGroupMap[os]
}

// We use this class (vs variables) so that the static functions can access data here.
class Constants {
    // Innerloop build OS's
    // The Windows_NT_BuildOnly OS is a way to speed up the Non-NT builds temporarily by avoiding
    // test execution in the build flow runs.  It generates the exact same build
    // as Windows_NT but without the tests.
    def static osList = ['Ubuntu', 'Debian8.4', 'OSX', 'Windows_NT', 'Windows_NT_BuildOnly', 'FreeBSD', 'CentOS7.1', 'OpenSUSE13.2', 'OpenSUSE42.1', 'RHEL7.2', 'LinuxARMEmulator', 'Ubuntu16.04', 'Fedora23']
    def static crossList = ['Ubuntu', 'OSX', 'CentOS7.1', 'RHEL7.2', 'Debian8.4', 'OpenSUSE13.2']
    // This is a set of JIT stress modes combined with the set of variables that
    // need to be set to actually enable that stress mode.  The key of the map is the stress mode and
    // the values are the environment variables
    def static jitStressModeScenarios = ['minopts' : ['COMPlus_JITMinOpts' : '1'], 'forcerelocs' : ['COMPlus_ForceRelocs' : '1'],
               'jitstress1' : ['COMPlus_JitStress' : '1'], 'jitstress2' : ['COMPlus_JitStress' : '2'],
               'jitstressregs1' : ['COMPlus_JitStressRegs' : '1'], 'jitstressregs2' : ['COMPlus_JitStressRegs' : '2'],
               'jitstressregs3' : ['COMPlus_JitStressRegs' : '3'], 'jitstressregs4' : ['COMPlus_JitStressRegs' : '4'],
               'jitstressregs8' : ['COMPlus_JitStressRegs' : '8'], 'jitstressregs0x10' : ['COMPlus_JitStressRegs' : '0x10'],
               'jitstressregs0x80' : ['COMPlus_JitStressRegs' : '0x80'],
               'jitstress2_jitstressregs1'    : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '1'],
               'jitstress2_jitstressregs2'    : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '2'],
               'jitstress2_jitstressregs3'    : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '3'],
               'jitstress2_jitstressregs4'    : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '4'],
               'jitstress2_jitstressregs8'    : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '8'],
               'jitstress2_jitstressregs0x10' : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x10'],
               'jitstress2_jitstressregs0x80' : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x80'],
               'corefx_baseline' : [ : ], // corefx baseline
               'corefx_minopts' : ['COMPlus_JITMinOpts' : '1'],
               'corefx_jitstress1' : ['COMPlus_JitStress' : '1'], 
               'corefx_jitstress2' : ['COMPlus_JitStress' : '2'],
               'corefx_jitstressregs1' : ['COMPlus_JitStressRegs' : '1'], 'corefx_jitstressregs2' : ['COMPlus_JitStressRegs' : '2'],
               'corefx_jitstressregs3' : ['COMPlus_JitStressRegs' : '3'], 'corefx_jitstressregs4' : ['COMPlus_JitStressRegs' : '4'],
               'corefx_jitstressregs8' : ['COMPlus_JitStressRegs' : '8'], 'corefx_jitstressregs0x10' : ['COMPlus_JitStressRegs' : '0x10'],
               'corefx_jitstressregs0x80' : ['COMPlus_JitStressRegs' : '0x80'],
               'gcstress0x3' : ['COMPlus_GCStress'  : '0x3'], 'gcstress0xc' : ['COMPlus_GCStress'  : '0xC'],
               'zapdisable' : ['COMPlus_ZapDisable'  : '0xC'],
               'heapverify1' : ['COMPlus_HeapVerify'  : '1'],
               'gcstress0xc_zapdisable' : ['COMPlus_GCStress'  : '0xC', 'COMPlus_ZapDisable'  : '1'],
               'gcstress0xc_zapdisable_jitstress2' : ['COMPlus_GCStress'  : '0xC', 'COMPlus_ZapDisable'  : '1', 'COMPlus_JitStress'  : '2'],
               'gcstress0xc_zapdisable_heapverify1' : ['COMPlus_GCStress'  : '0xC', 'COMPlus_ZapDisable'  : '1', 'COMPlus_HeapVerify'  : '1'],
               'gcstress0xc_jitstress1' : ['COMPlus_GCStress'  : '0xC', 'COMPlus_JitStress'  : '1'],
               'gcstress0xc_jitstress2' : ['COMPlus_GCStress'  : '0xC', 'COMPlus_JitStress'  : '2'],
               'gcstress0xc_minopts_heapverify1' : ['COMPlus_GCStress'  : '0xC', 'COMPlus_JITMinOpts'  : '1', 'COMPlus_HeapVerify'  : '1']
               ]
    // This is a set of r2r jit stress scenarios
    def static r2rJitStressScenarios = ['r2r_jitstress1', 'r2r_jitstress2', 'r2r_jitstressregs1', 'r2r_jitstressregs2', 'r2r_jitstressregs3',
                                        'r2r_jitstressregs4', 'r2r_jitstressregs8', 'r2r_jitstressregsx10', 'r2r_jitstressregsx80',
                                        'r2r_jitminopts', 'r2r_jitforcerelocs']
    // This is the basic set of scenarios
    def static basicScenarios = ['default', 'pri1', 'ilrt', 'r2r', 'pri1r2r', 'gcstress15_pri1r2r', 'longgc', 'coverage', 'formatting', 'gcsimulator'] + r2rJitStressScenarios
    def static configurationList = ['Debug', 'Checked', 'Release']
    // This is the set of architectures
    def static architectureList = ['arm', 'arm64', 'x64', 'x86ryujit', 'x86lb']
}

def static setMachineAffinity(def job, def os, def architecture) {
    if (architecture == 'arm64' && os == 'Windows_NT') {
        // For cross compilation
        job.with {
            label('arm64')
        }
    } else if ((architecture == 'arm' || architecture == 'arm64') && os == 'Ubuntu') {
        Utilities.setMachineAffinity(job, os, 'arm-cross-latest');
    } else {
        Utilities.setMachineAffinity(job, os, 'latest-or-auto');
    }
}

def static isJITStressJob(def scenario) {
    return Constants.jitStressModeScenarios.containsKey(scenario) ||
           (Constants.r2rJitStressScenarios.indexOf(scenario) != -1)
}

def static isGCStressRelatedTesting(def scenario) {
    // The 'gcstress15_pri1r2r' scenario is a basic scenario.
    // Detect it and make it a GCStress related.
    if (scenario == 'gcstress15_pri1r2r')
    {
        return true;
    }

    def gcStressTestEnvVars = [ 'COMPlus_GCStress', 'COMPlus_ZapDisable', 'COMPlus_HeapVerify']
    def scenarioName = scenario.toLowerCase()
    def isGCStressTesting = false
    Constants.jitStressModeScenarios[scenario].each{ k, v -> 
        if (k in gcStressTestEnvVars) {
            isGCStressTesting = true;
        }
    }   
    return isGCStressTesting
}

def static isCorefxTesting(def scenario) {
    def corefx_prefix = 'corefx_'
    if (scenario.length() < corefx_prefix.length()) {
        return false
    }
    return scenario.substring(0,corefx_prefix.length()) == corefx_prefix
}

def static isR2R(def scenario) {
    return (scenario == 'r2r' || scenario == 'pri1r2r')
}

def static isCoverage(def scenario) {
    return (scenario == 'coverage')
}

def static isLongGc(def scenario) {
    return (scenario == 'longgc' || scenario == 'gcsimulator')
}

def static setTestJobTimeOut(newJob, scenario) {
    if (isGCStressRelatedTesting(scenario)) {
        Utilities.setJobTimeout(newJob, 4320)
    }
    else if (isCorefxTesting(scenario)) {
        Utilities.setJobTimeout(newJob, 360)
    }
    else if (Constants.jitStressModeScenarios.containsKey(scenario)) {
        Utilities.setJobTimeout(newJob, 240)
    }
    else if (isR2R(scenario)) {
        Utilities.setJobTimeout(newJob, 240)
    }
    else if (isCoverage(scenario)) {
        Utilities.setJobTimeout(newJob, 1440)  
    }
    else if (isLongGc(scenario)) {
        Utilities.setJobTimeout(newJob, 1440)
    }
    // Non-test jobs use the default timeout value.
}

def static getStressModeDisplayName(def scenario) {
    def displayStr = ''
    Constants.jitStressModeScenarios[scenario].each{ k, v -> 
        def prefixLength = 'COMPlus_'.length()
        if (k.length() >= prefixLength) {
            def modeName = k.substring(prefixLength, k.length())
            displayStr += ' ' + modeName + '=' + v
        }
    }   
    return displayStr
}

// Generates the string for creating a file that sets environment variables
// that makes it possible to run stress modes.  Writes the script to a file called
// SetStressModes.[sh/cmd]
def static genStressModeScriptStep(def os, def stressModeName, def stressModeVars, def stepScriptLocation) {
    def stepScript = ''
    if (os == 'Windows_NT') {
        stepScript += "echo Creating TestEnv Script for ${stressModeName}\r\n"
        stepScript += "del ${stepScriptLocation}\r\n"
         
        // Timeout in ms, default is 10 minutes. For stress
        // modes up this to 30 minutes
        def timeout = 1800000

        // Set the Timeout
        stepScript += "set __TestTimeout=${timeout}\r\n"
        stressModeVars.each{ k, v -> 
            // Write out what we are writing to the script file
            stepScript += "echo Setting ${k}=${v}\r\n"
            // Write out the set itself to the script file`
            stepScript += "echo set ${k}=${v} >> ${stepScriptLocation}\r\n"
        }
    }
    else {
        stepScript += "echo Setting variables for ${stressModeName}\n"
        stepScript += "echo \\#\\!/usr/bin/env bash > ${stepScriptLocation}\n"
        stressModeVars.each{ k, v -> 
            // Write out what we are writing to the script file
            stepScript += "echo Setting ${k}=${v}\n"
            // Write out the set itself to the script file`
            stepScript += "echo export ${k}=${v} >> ${stepScriptLocation}\n"
        }
        stepScript += "chmod +x ${stepScriptLocation}\n"
    }
    return stepScript
}

// Corefx doesn't have a support to pass stress mode environment variables. This function
// generates commands to set or export environment variables
def static getStressModeEnvSetCmd(def os, def stressModeName) {
    def envVars = Constants.jitStressModeScenarios[stressModeName]
    def setEnvVars = ''
    if (os == 'Windows_NT') {
        envVars.each{ VarName, Value   ->
            if (VarName != '') {
                setEnvVars += "set ${VarName}=${Value}\n"
            }
        }
    }
    else {
        envVars.each{ VarName, Value   ->
            if (VarName != '') {
                setEnvVars += "export ${VarName}=${Value}\n"
            }
        }
    }
    return setEnvVars
}

// Calculates the name of the build job based on some typical parameters.
//
def static getJobName(def configuration, def architecture, def os, def scenario, def isBuildOnly, def isLinuxEmulatorBuild = false) {
    // If the architecture is x64, do not add that info into the build name.
    // Need to change around some systems and other builds to pick up the right builds
    // to do that.
    
    def suffix = scenario != 'default' ? "_${scenario}" : '';
    if (isBuildOnly) {
        suffix += '_bld'
    }
    def baseName = ''
    switch (architecture) {
        case 'x64':
            if (scenario == 'default') {
                // For now we leave x64 off of the name for compatibility with other jobs
                baseName = configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            else if (scenario == 'formatting') {
                // we don't care about the configuration for the formatting job. It runs all configs
                baseName = architecture.toLowerCase() + '_' + os.toLowerCase()
            }
            else {
                baseName = architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            break
        case 'arm64':
        case 'arm':
            // These are cross builds
            if (isLinuxEmulatorBuild == false) {
                baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            else {
                baseName = architecture.toLowerCase() + '_emulator_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            break
        case 'x86ryujit':
            baseName = 'x86_ryujit_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'x86lb':
            baseName = 'x86_lb_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }
    
    return baseName + suffix
}

static void addEmailPublisher(def job, def recipient) {
    job.with {
        publishers {
            extendedEmail(recipient, '$DEFAULT_SUBJECT', '$DEFAULT_CONTENT') {
                trigger('Aborted', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, true, true, true, true)
                trigger('Failure', '$PROJECT_DEFAULT_SUBJECT', '$PROJECT_DEFAULT_CONTENT', null, true, true, true, true)
            }
        }
    }
}

// **************************
// Define the basic inner loop builds for PR and commit.  This is basically just the set
// of coreclr builds over linux/osx/freebsd/windows and debug/release/checked.  In addition, the windows
// builds will do a couple extra steps.
// **************************

// Adds a trigger for the PR build if one is needed.  If isFlowJob is true, then this is the
// flow job that rolls up the build and test for non-windows OS's.  // If the job is a windows build only job,
// it's just used for internal builds
// If you add a job with a trigger phrase, please add that phrase to coreclr/Documentation/project-docs/ci-trigger-phrases.md
def static addTriggers(def job, def branch, def isPR, def architecture, def os, def configuration, def scenario, def isFlowJob, def isWindowsBuildOnlyJob, def isLinuxEmulatorBuild) {
    if (isWindowsBuildOnlyJob) {
        return
    }
    
    def bidailyCrossList = ['RHEL7.2', 'Debian8.4', 'OpenSUSE13.2']
    // Non pull request builds.
    if (!isPR) {
        // Check scenario.
        switch (scenario) {
            case 'default':
                switch (architecture) {
                    case 'x64':
                    case 'x86ryujit':
                    case 'x86lb':
                        if (isFlowJob || os == 'Windows_NT' || !(os in Constants.crossList)) {
                            Utilities.addGithubPushTrigger(job)
                        }
                        break
                    case 'arm':
                        Utilities.addGithubPushTrigger(job)
                        break
                    case 'arm64':
                        if (os == 'Windows_NT') {
                            Utilities.addGithubPushTrigger(job)
                            addEmailPublisher(job, 'dotnetonarm64@microsoft.com')
                        }
                        break
                    default:
                        println("Unknown architecture: ${architecture}");
                        assert false
                        break
                }
                break
            case 'pri1':
                // Pri one gets a push trigger, and only for release
                if (architecture == 'x64') {
                    if (configuration == 'Release') {
                        // We expect release jobs to be Windows, or in the cross list
                        assert (os == 'Windows_NT') || (os in Constants.crossList)
                        if (!os in bidailyCrossList) {
                            if (isFlowJob || os == 'Windows_NT') {
                                Utilities.addGithubPushTrigger(job)
                            }
                        } 
                        else {
                            if (isFlowJob) {
                                Utilities.addPeriodicTrigger(job, 'H H/12 * * *')
                            }
                        }
                    }
                }
                break
            case 'r2r':
                //r2r jobs that aren't pri1 can only be triggered by phrase
                break
            case 'pri1r2r':
                assert !(os in bidailyCrossList)
                //pri1 r2r gets a push trigger for checked/release
                if (configuration == 'Checked' || configuration == 'Release') {
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (architecture == 'x64' && os != 'OSX') {
                        //Flow jobs should be Windows, Ubuntu, OSX, or CentOS
                        if (isFlowJob || os == 'Windows_NT') {
                            Utilities.addGithubPushTrigger(job)
                        }
                    // OSX pri1r2r jobs should only run every 12 hours, not daily.
                    } else if (architecture == 'x64' && os == 'OSX'){
                        if (isFlowJob) {
                            Utilities.addPeriodicTrigger(job, 'H H/12 * * *')
                        }
                    }
                    // For x86, only add per-commit jobs for Windows
                    else if (architecture == 'x86ryujit' || architecture == 'x86lb') {
                        if (os == 'Windows_NT') {
                            Utilities.addGithubPushTrigger(job)
                        }
                    }
                    // arm64 pri1r2r jobs should only run every 12 hours.
                    else if (architecture == 'arm64') {
                        if (os == 'Windows_NT') {
                            Utilities.addPeriodicTrigger(job, 'H H/12 * * *')
                            addEmailPublisher(job, 'dotnetonarm64@microsoft.com')
                        }
                    }
                }
                break
            case 'r2r_jitstress1':
            case 'r2r_jitstress2':
            case 'r2r_jitstressregs1':
            case 'r2r_jitstressregs2':
            case 'r2r_jitstressregs3':
            case 'r2r_jitstressregs4':
            case 'r2r_jitstressregs8':
            case 'r2r_jitstressregsx10':
            case 'r2r_jitstressregsx80':
            case 'r2r_jitminopts':
            case 'r2r_jitforcerelocs':
            case 'gcstress15_pri1r2r':
                assert !(os in bidailyCrossList)

                // GCStress=C is currently not supported on OS X
                if (os == 'OSX' && isGCStressRelatedTesting(scenario)) {
                    break
                }

                //GC Stress 15 pri1 r2r gets a push trigger for checked/release
                if (configuration == 'Checked' || configuration == 'Release') {
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (architecture == 'x64') {
                        //Flow jobs should be Windows, Ubuntu, OSX, or CentOS
                        if (isFlowJob || os == 'Windows_NT') {
                            // Add a weekly periodic trigger
                            Utilities.addPeriodicTrigger(job, 'H H * * 3,6') // some time every Wednesday and Saturday
                        }
                    }
                    // For x86, only add per-commit jobs for Windows
                    else if (architecture == 'x86ryujit' || architecture == 'x86lb') {
                        if (os == 'Windows_NT') {
                            Utilities.addPeriodicTrigger(job, 'H H * * 3,6') // some time every Wednesday and Saturday
                        }
                    }
                }
                break
            case 'longgc':
                assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX')
                assert configuration == 'Release'
                assert architecture == 'x64'
                Utilities.addPeriodicTrigger(job, '@daily')
                addEmailPublisher(job, 'dotnetgctests@microsoft.com')
                break
            case 'gcsimulator':
                assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX')
                assert configuration == 'Release'
                assert architecture == 'x64'
                Utilities.addPeriodicTrigger(job, 'H H * * 3,6') // some time every Wednesday and Saturday
                addEmailPublisher(job, 'dotnetgctests@microsoft.com')
                break
            case 'ilrt':
                assert !(os in bidailyCrossList)
                // ILASM/ILDASM roundtrip one gets a daily build, and only for release
                if (architecture == 'x64' && configuration == 'Release') {
                    // We don't expect to see a job generated except in these scenarios
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (isFlowJob || os == 'Windows_NT') {
                        Utilities.addPeriodicTrigger(job, '@daily')
                    }
                }
                break
            case 'coverage':
                assert (os == 'Ubuntu' || os == 'Windows_NT')
                assert configuration == 'Release'
                assert architecture == 'x64'
                Utilities.addPeriodicTrigger(job, '@weekly')
                break
            case 'formatting':
                assert (os == 'Windows_NT' || os == "Ubuntu")
                assert architecture == 'x64'
                Utilities.addGithubPushTrigger(job)
                break
            case 'jitstressregs1':
            case 'jitstressregs2':
            case 'jitstressregs3':
            case 'jitstressregs4':
            case 'jitstressregs8':
            case 'jitstressregs0x10':
            case 'jitstressregs0x80':
            case 'minopts':
            case 'forcerelocs':
            case 'jitstress1':
            case 'jitstress2':   
            case 'jitstress2_jitstressregs1':
            case 'jitstress2_jitstressregs2':
            case 'jitstress2_jitstressregs3':
            case 'jitstress2_jitstressregs4':
            case 'jitstress2_jitstressregs8':
            case 'jitstress2_jitstressregs0x10':
            case 'jitstress2_jitstressregs0x80':
            case 'corefx_baseline': 
            case 'corefx_minopts':
            case 'corefx_jitstress1':               
            case 'corefx_jitstress2':
            case 'corefx_jitstressregs1':
            case 'corefx_jitstressregs2':
            case 'corefx_jitstressregs3':
            case 'corefx_jitstressregs4':
            case 'corefx_jitstressregs8':
            case 'corefx_jitstressregs0x10':
            case 'corefx_jitstressregs0x80':
            case 'zapdisable':           
                if (os != 'CentOS7.1' && !(os in bidailyCrossList)) {
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    Utilities.addPeriodicTrigger(job, '@daily')
                }
                break            
            case 'heapverify1':
            case 'gcstress0x3':            
                if (os != 'CentOS7.1' && !(os in bidailyCrossList)) {
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (architecture == 'arm64') {
                        assert (os == 'Windows_NT')
                        Utilities.addPeriodicTrigger(job, '@daily')
                        addEmailPublisher(job, 'dotnetonarm64@microsoft.com')
                    }
                    else {
                        Utilities.addPeriodicTrigger(job, '@weekly')
                    }
                }
                break
            case 'gcstress0xc':
            case 'gcstress0xc_zapdisable':
            case 'gcstress0xc_zapdisable_jitstress2':
            case 'gcstress0xc_zapdisable_heapverify1':
            case 'gcstress0xc_jitstress1':
            case 'gcstress0xc_jitstress2':
            case 'gcstress0xc_minopts_heapverify1':
                // GCStress=C is currently not supported on OS X
                if (os != 'CentOS7.1' && os != 'OSX' && !(os in bidailyCrossList)) {
                    assert (os == 'Windows_NT') || (os in Constants.crossList)
                    if (architecture == 'arm64') {
                        assert (os == 'Windows_NT')
                        // TODO: Enable a periodic trigger after tests are updated.
                        // Utilities.addPeriodicTrigger(job, '@daily')
                        // addEmailPublisher(job, 'dotnetonarm64@microsoft.com')
                    }
                    else {
                        Utilities.addPeriodicTrigger(job, '@weekly')
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
            if (scenario == 'coverage') {
                assert configuration == 'Release'
                if (os == 'Ubuntu') {
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Coverage Build & Test", "(?i).*test\\W+coverage.*")
                }
                break
            }

            if (scenario == 'formatting') {
                assert configuration == 'Checked'
                if (os == 'Windows_NT' || os == 'Ubuntu') {
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} Formatting", "(?i).*test\\W+${os}\\W+formatting.*")
                }
                break
            }

            switch (os) {
                // OpenSUSE, Debian & RedHat get trigger phrases for pri 0 build, and pri 1 build & test
                case 'OpenSUSE13.2':
                case 'Debian8.4':
                case 'RHEL7.2':
                    if (scenario == 'default') {
                        assert !isFlowJob
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build", "(?i).*test\\W+${os}.*")
                    }
                    else if (scenario == 'pri1' && isFlowJob) {
                        assert (configuration == 'Release')
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Pri 1 Build & Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                    }
                    break
                case 'Fedora23':
                case 'Ubuntu16.04':
                case 'OpenSUSE42.1':
                    assert !isFlowJob
                    assert scenario == 'default'
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build", "(?i).*test\\W+${os}\\W+.*")
                    break                
                case 'Ubuntu':
                case 'OSX':
                    // Triggers on the non-flow jobs aren't necessary here
                    // Corefx testing uses non-flow jobs.
                    if (!isFlowJob && !isCorefxTesting(scenario)) {
                        break
                    }
                    switch (scenario) {
                        case 'default':
                            // Ubuntu uses checked for default PR tests
                            if (configuration == 'Checked') {
                                // Default trigger
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test")
                            }
                            break
                        case 'pri1':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Priority 1 Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'r2r':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} R2R pri0 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'pri1r2r':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'gcstress15_pri1r2r':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GCStress 15 R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstress1':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstress1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstress2':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstress2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs1':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs2':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs3':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs3 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs4':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs4 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs8':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs8 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregsx10':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregsx10 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregsx80':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregsx80 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitminopts':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} JITMinOpts R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitforcerelocs':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ForceRelocs R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'longgc':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Long-Running GC Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'gcsimulator':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Simulator", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'minopts':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - MinOpts)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                           
                        case 'forcerelocs':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ForceRelocs)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                        case 'jitstressregs1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                           
                        case 'jitstressregs2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                                                   
                        case 'jitstressregs3':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=3)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                                                   
                        case 'jitstressregs4':      
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=4)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                                                   
                        case 'jitstressregs8':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=8)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstressregs0x10':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x10)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstressregs0x80':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x80)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress2_jitstressregs1':
                        case 'jitstress2_jitstressregs2':
                        case 'jitstress2_jitstressregs3':
                        case 'jitstress2_jitstressregs4':
                        case 'jitstress2_jitstressregs8':
                        case 'jitstress2_jitstressregs0x10':
                        case 'jitstress2_jitstressregs0x80':
                        case 'gcstress0x3':
                        case 'gcstress0xc':
                        case 'zapdisable':
                        case 'heapverify1':
                        case 'gcstress0xc_zapdisable':
                        case 'gcstress0xc_zapdisable_jitstress2':
                        case 'gcstress0xc_zapdisable_heapverify1':
                        case 'gcstress0xc_jitstress1':
                        case 'gcstress0xc_jitstress2':
                        case 'gcstress0xc_minopts_heapverify1':                                 
                            def displayStr = getStressModeDisplayName(scenario)  
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayStr})",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'corefx_baseline':
                        case 'corefx_minopts':
                        case 'corefx_jitstress1':
                        case 'corefx_jitstress2':
                        case 'corefx_jitstressregs1':
                        case 'corefx_jitstressregs2':
                        case 'corefx_jitstressregs3':
                        case 'corefx_jitstressregs4':
                        case 'corefx_jitstressregs8':
                        case 'corefx_jitstressregs0x10':
                        case 'corefx_jitstressregs0x80':
                            def displayName = 'CoreFx' + getStressModeDisplayName(scenario)                                                    
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayName})",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                          
                        default:
                            println("Unknown scenario: ${scenario}");
                            assert false
                            break
                    }
                    break
                case 'CentOS7.1':
                    switch (scenario) {
                        case 'pri1':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Priority 1 Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'r2r':
                            if (configuration == 'Checked' || configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} R2R pri0 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'pri1r2r':
                            if (configuration == 'Checked' || configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'gcstress15_pri1r2r':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GCStress 15 R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstress1':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstress1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstress2':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstress2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs1':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs2':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs3':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs3 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs4':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs4 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs8':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs8 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregsx10':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregsx10 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregsx80':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregsx80 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitminopts':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} JITMinOpts R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitforcerelocs':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ForceRelocs R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        default:
                            break
                    }   
                case 'Windows_NT':
                    switch (scenario) {
                        case 'default':
                            // Default trigger
                            if (configuration == 'Debug') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test")
                            }
                            break
                        case 'pri1':
                            // Default trigger
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Priority 1 Build and Test")
                            }
                            break
                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'r2r':
                            if (configuration == 'Checked' || configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} R2R pri0 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'pri1r2r':
                            if (configuration == 'Checked' || configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'gcstress15_pri1r2r':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GCStress 15 R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstress1':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstress1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstress2':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstress2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs1':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs2':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs3':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs3 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs4':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs4 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregs8':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregs8 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregsx10':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregsx10 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitstressregsx80':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} jitstressregsx80 R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitminopts':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} JITMinOpts R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'r2r_jitforcerelocs':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ForceRelocs R2R Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")  
                            }
                            break
                        case 'longgc':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Long-Running GC Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'gcsimulator':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Simulator", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break
                        case 'minopts':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - MinOpts)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'forcerelocs':                         
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ForceRelocs)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                           
                        case 'jitstress1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstressregs1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        case 'jitstressregs2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        case 'jitstressregs3':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=3)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        case 'jitstressregs4':      
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=4)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        case 'jitstressregs8':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=8)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        case 'jitstressregs0x10':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x10)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        case 'jitstressregs0x80':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x80)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break       
                        case 'jitstress2_jitstressregs1':
                        case 'jitstress2_jitstressregs2':
                        case 'jitstress2_jitstressregs3':
                        case 'jitstress2_jitstressregs4':
                        case 'jitstress2_jitstressregs8':
                        case 'jitstress2_jitstressregs0x10':
                        case 'jitstress2_jitstressregs0x80':
                        case 'gcstress0x3': 
                        case 'gcstress0xc':
                        case 'zapdisable':
                        case 'heapverify1':
                        case 'gcstress0xc_zapdisable':
                        case 'gcstress0xc_zapdisable_jitstress2':
                        case 'gcstress0xc_zapdisable_heapverify1':
                        case 'gcstress0xc_jitstress1':
                        case 'gcstress0xc_jitstress2':
                        case 'gcstress0xc_minopts_heapverify1':                                 
                            def displayStr = getStressModeDisplayName(scenario)
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayStr})",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                                   
                        case 'corefx_baseline':
                        case 'corefx_minopts':
                        case 'corefx_jitstress1':
                        case 'corefx_jitstress2':
                        case 'corefx_jitstressregs1':
                        case 'corefx_jitstressregs2':
                        case 'corefx_jitstressregs3':
                        case 'corefx_jitstressregs4':
                        case 'corefx_jitstressregs8':
                        case 'corefx_jitstressregs0x10':
                        case 'corefx_jitstressregs0x80':
                            def displayName = 'CoreFx ' + getStressModeDisplayName(scenario)
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayName})",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break                       
                        default:
                            println("Unknown scenario: ${scenario}");
                            assert false
                            break
                    }
                    break
                case 'FreeBSD':
                    assert scenario == 'default'
                    if (configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build")
                    }
                    break
                default:
                    println("Unknown os: ${os}");
                    assert false
                    break
            }
            break
        case 'arm':
            assert scenario == 'default'
            switch (os) {
                case 'Ubuntu':
                    if (isLinuxEmulatorBuild == false) {
                        // Removing the regex will cause this to run on each PR.
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} Cross ${configuration} Build", "(?i).*test\\W+Linux\\W+arm\\W+cross\\W+${configuration}.*")
                    }
                    else {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "Linux ARM Emulator Cross ${configuration} Build")
                    }
                    break
                default:
                    println("NYI os: ${os}");
                    assert false
                    break
            }
            break
        case 'arm64':
            assert (scenario == 'default') || (scenario == 'pri1r2r') || (scenario == 'gcstress0x3') || (scenario == 'gcstress0xc')

            // Set up a private trigger
            def contextString = "${os} ${architecture} Cross ${configuration}"
            if (scenario != 'default')
                contextString += " ${scenario}"
            contextString += " Build"
            // Debug builds only.
            if (configuration != 'Debug') {
               contextString += " and Test"
            }

            def arm64Users = ['erozenfeld', 'kyulee1', 'pgavlin', 'russellhadley', 'swaroop-sridhar', 'JosephTremoulet', 'jashook', 'RussKeldorph', 'gkhanna79', 'briansull', 'cmckinsey', 'jkotas', 'ramarag', 'markwilkie', 'rahku', 'tzwlai', 'weshaggard']
            switch (os) {
                case 'Windows_NT':
                    switch (scenario) {
                        case 'default':
                            Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString,
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}.*", null, arm64Users)
                            break
                        case 'pri1r2r':
                        case 'gcstress0x3':
                        case 'gcstress0xc':
                            Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString,
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*", null, arm64Users)
                            break
                    }
                    break
                default:
                    println("NYI os: ${os}");
                    assert false
                    break
            }
            break
        case 'x86ryujit':
            def arch = 'x86'
            def jit = 'ryujit'
            switch (scenario) {
                case 'default':
                    // Default trigger
                    if (os == 'Windows_NT') {
                        if (configuration == 'Checked') {
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test")
                        }
                    }
                    else {
                        // default trigger
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build", "(?i).*test\\W+${arch}\\W+${osGroup}.\\W+${jit}.*")
                    }
                    break
                case 'pri1':
                    // Default trigger
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Priority 1 Build and Test")
                    }
                    break
                case 'ilrt':
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break
                case 'r2r':
                    if (configuration == 'Checked' || configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} R2R pri0 Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break
                case 'pri1r2r':
                    if (configuration == 'Checked' || configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break
                case 'gcstress15_pri1r2r':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} GCStress 15 R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstress1':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstress1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstress2':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstress2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregs1':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregs2':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregs3':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs3 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregs4':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs4 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregs8':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs8 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregsx10':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregsx10 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitstressregsx80':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregsx80 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitminopts':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} JITMinOpts R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'r2r_jitforcerelocs':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} ForceRelocs R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                    break
                case 'longgc':
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Long-Running GC Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break
                case 'gcsimulator':
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} GC Simulator", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break
                case 'minopts':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - MinOpts)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break
                case 'forcerelocs':                         
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - ForceRelocs)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                           
                case 'jitstress1':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStress=1)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break
                case 'jitstress2':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStress=2)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break
                case 'jitstressregs1':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=1)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                       
                case 'jitstressregs2':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=2)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                       
                case 'jitstressregs3':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=3)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                       
                case 'jitstressregs4':      
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=4)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                       
                case 'jitstressregs8':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=8)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                       
                case 'jitstressregs0x10':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=0x10)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                       
                case 'jitstressregs0x80':
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - JitStressRegs=0x80)",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break       
                case 'jitstress2_jitstressregs1':
                case 'jitstress2_jitstressregs2':
                case 'jitstress2_jitstressregs3':
                case 'jitstress2_jitstressregs4':
                case 'jitstress2_jitstressregs8':
                case 'jitstress2_jitstressregs0x10':
                case 'jitstress2_jitstressregs0x80':
                case 'gcstress0x3': 
                case 'gcstress0xc':
                case 'zapdisable':
                case 'heapverify1':
                case 'gcstress0xc_zapdisable':
                case 'gcstress0xc_zapdisable_jitstress2':
                case 'gcstress0xc_zapdisable_heapverify1':
                case 'gcstress0xc_jitstress1':
                case 'gcstress0xc_jitstress2':
                case 'gcstress0xc_minopts_heapverify1':                                 
                    def displayStr = getStressModeDisplayName(scenario)
                    assert (os == 'Windows_NT')
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test (Jit - ${displayStr})",
                       "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    break                                   
                default:
                    println("Unknown scenario: ${arch} ${jit} ${scenario}");
                    assert false
                    break
            }
            break
        case 'x86lb':
            assert (scenario == 'default' || scenario == 'r2r' || scenario == 'pri1r2r' || scenario == 'gcstress15_pri1r2r' || scenario == 'longgc' || scenario == 'gcsimulator' ||
                    Constants.r2rJitStressScenarios.indexOf(scenario) != -1)
            // For windows, x86 runs by default
            def arch = 'x86'
            def jit = 'ryujit'
            if (architecture == 'x86lb') {
                jit = 'legacy_backend'
            }
            
            if (scenario == 'default') {
                if (os == 'Windows_NT') {
                    if (configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build and Test")
                    }
                }
                else {
                    // default trigger
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Build", "(?i).*test\\W+${arch}\\W+${osGroup}.\\W+${jit}.*")
                }
            }
            else if (scenario == 'r2r') {
                if (os == 'Windows_NT') {
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} R2R pri0 Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                }
            }
            else if (scenario == 'pri1r2r') {
                if (os == 'Windows_NT') {
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                }
            }
            else if (scenario == 'gcstress15_pri1r2r'){
                if (os == 'Windows_NT'){
                    if (configuration == 'Release'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} GCStress 15 R2R pri1 Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstress1'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstress1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstress2'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstress2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregs1'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs1 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregs2'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs2 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregs3'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs3 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregs4'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs4 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregs8'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregs8 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregsx10'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregsx10 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitstressregsx80'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitstressregsx80 R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitminopts'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitminopts R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'r2r_jitforcerelocs'){
                if (configuration == 'Release' || configuration == 'Checked') {
                    if (os == 'Windows_NT'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} jitforcerelocs R2R Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'longgc') {
                if (os == 'Windows_NT'){
                    if (configuration == 'Release'){
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} Long-Running GC Build & Test", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")  
                    }
                }
            }
            else if (scenario == 'gcsimulator') {
                if (os == 'Windows_NT') {
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${arch} ${jit} ${configuration} GC Simulator", "(?i).*test\\W+${os}\\W+${arch}\\W+${jit}\\W+${configuration}\\W+${scenario}.*")
                    }
                }
            }
            break
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }
}

// Additional scenario which can alter behavior

def combinedScenarios = Constants.basicScenarios + Constants.jitStressModeScenarios.keySet()
combinedScenarios.each { scenario ->
    [true, false].each { isPR ->
        Constants.architectureList.each { architecture ->
            Constants.configurationList.each { configuration ->
                Constants.osList.each { os ->
                    // If the OS is Windows_NT_BuildOnly, set the isBuildOnly flag to true
                    // and reset the os to Windows_NT
                    def isBuildOnly = false
                    if (os == 'Windows_NT_BuildOnly') {
                        isBuildOnly = true
                        os = 'Windows_NT'
                    }

                    // If the OS is LinuxARMEmulator and arch is arm, set the isLinuxEmulatorBuild
                    // flag to true and reset the os to Ubuntu
                    // The isLinuxEmulatorBuild flag will be used at a later time to execute the right
                    // set of build commands
                    // The tuples (LinuxARMEmulator, other architectures) are not handled and control returns
                    def isLinuxEmulatorBuild = false
                    if (os == 'LinuxARMEmulator' && architecture == 'arm') {
                        // Cross Builds only in Debug and Release modes allowed
                        if ( configuration == 'Checked' ) {
                            return
                        }

                        isLinuxEmulatorBuild = true
                        os = 'Ubuntu'
                    } else if (os == 'LinuxARMEmulator') {
                        return
                    }

                    // Skip totally unimplemented (in CI) configurations.
                    switch (architecture) {
                        case 'arm64':
                            // Windows only
                            if (os != 'Windows_NT' || isBuildOnly) {
                                return
                            }
                            break
                        case 'arm':
                            // Only Ubuntu cross implemented
                            if (os != 'Ubuntu') {
                                return
                            }
                            break
                        case 'x86ryujit':
                        case 'x86lb':
                            // Skip non-windows
                            if (os != 'Windows_NT') {
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
                    // Skip scenarios (blanket skipping for jit stress modes, which are good most everywhere
                    // with checked builds
                    def enableCorefxTesting = false
                    if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                        if (configuration != 'Checked') {
                            return
                        }
                        
                        enableCorefxTesting = isCorefxTesting(scenario)
                        
                        // Since these are just execution time differences,
                        // skip platforms that don't execute the tests here (Windows_NT only)
                        def isEnabledOS = os == 'Windows_NT' || (os == 'Ubuntu' && enableCorefxTesting)
                        if (!isEnabledOS || isBuildOnly) {
                            return
                        }
                        
                        switch (architecture) {
                            case 'arm64':
                                if ((scenario != 'gcstress0x3') && (scenario != 'gcstress0xc')) {
                                    return
                                }
                                break
                            case 'x64':
                                // Everything implemented
                                break
                            case 'x86ryujit':
                                if (enableCorefxTesting) {
                                    return
                                }
                                break
                            default:
                                return
                        }
                    }
                    else {
                        // If this is a r2r jitstress, jitstressregs, jitminopts or forcerelocs scenario
                        // and configuration is not Checked, bail out.
                        if (configuration != 'Checked' && Constants.r2rJitStressScenarios.indexOf(scenario) != -1) {
                            return;
                        }

                        // Skip scenarios
                        switch (scenario) {
                            case 'pri1':
                                // The pri1 build isn't necessary except for Windows_NT.  Non-Windows NT uses
                                // the default scenario build
                                if (os != 'Windows_NT') {
                                    return
                                }
                                // Only x64 for now
                                if (architecture != 'x64') {
                                    return
                                }
                                break
                            case 'ilrt':
                                // The ilrt build isn't necessary except for Windows_NT.  Non-Windows NT uses
                                // the default scenario build
                                if (os != 'Windows_NT') {
                                    return
                                }
                                // Only x64 for now
                                if (architecture != 'x64') {
                                    return
                                }
                                // Release only
                                if (configuration != 'Release') {
                                    return
                                }
                                break
                            case 'r2r':
                                // The r2r build isn't necessary except for Windows_NT.  Non-Windows NT uses
                                // the default scenario build
                                if (os != 'Windows_NT') {
                                    return
                                }
                                if (architecture != 'x64') {
                                    return
                                }
                                break
                            case 'pri1r2r':
                                // The pri1r2r build isn't necessary except for Windows_NT.  Non-Windows NT uses
                                // the default scenario build
                                if (os != 'Windows_NT') {
                                    return
                                }
                                if (architecture != 'x64') {
                                    if (architecture != 'arm64' || configuration == 'Debug') {
                                        return
                                    }
                                }
                                break
                            case 'gcstress15_pri1r2r':
                            case 'r2r_jitstress1':
                            case 'r2r_jitstress2':
                            case 'r2r_jitstressregs1':
                            case 'r2r_jitstressregs2':
                            case 'r2r_jitstressregs3':
                            case 'r2r_jitstressregs4':
                            case 'r2r_jitstressregs8':
                            case 'r2r_jitstressregsx10':
                            case 'r2r_jitstressregsx80':
                            case 'r2r_jitminopts':
                            case 'r2r_jitforcerelocs':
                                // The above builds are not necessary except for Windows_NT.  Non-Windows NT uses
                                // the default scenario build
                                if (os != 'Windows_NT') {
                                    return
                                }
                                if (architecture != 'x64') {
                                    return
                                }
                                break
                            case 'longgc':
                            case 'gcsimulator':
                                if (os != 'Windows_NT' && os != 'Ubuntu' && os != 'OSX') {
                                    return
                                }
                                if (architecture != 'x64') {
                                    return
                                }
                                if (configuration != 'Release') {
                                    return
                                }
                                break
                            // We need Windows x64 Release bits for the code coverage build
                            case 'coverage':
                                if (os != 'Windows_NT') {
                                    return
                                }
                                if (architecture != 'x64') {
                                    return
                                }
                                if (configuration != 'Release') {
                                    return
                                }
                                break
                            // We only run Windows and Ubuntu x64 Checked for formatting right now
                            case 'formatting':
                                if (os != 'Windows_NT' && os != 'Ubuntu') {
                                    return
                                }
                                if (architecture != 'x64') {
                                    return
                                }
                                if (configuration != 'Checked') {
                                    return
                                }
                                if (isBuildOnly) {
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
                    }
                
                    // Calculate names
                    def lowerConfiguration = configuration.toLowerCase()
                    def jobName = getJobName(configuration, architecture, os, scenario, isBuildOnly, isLinuxEmulatorBuild)
                    def folderName = isJITStressJob(scenario) ? 'jitstress' : '';
                    
                    // Create the new job
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR, folderName)) {}
                    
                    setMachineAffinity(newJob, os, architecture)

                    // Add all the standard options
                    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                    addTriggers(newJob, branch, isPR, architecture, os, configuration, scenario, false, isBuildOnly, isLinuxEmulatorBuild)
                
                    def buildCommands = [];
                    def osGroup = getOSGroup(os)
                
                    // Calculate the build steps, archival, and xunit results
                    switch (os) {
                        case 'Windows_NT':
                            switch (architecture) {
                                case 'x64':
                                case 'x86ryujit':
                                case 'x86lb':
                                    def arch = architecture
                                    if (architecture == 'x86ryujit' || architecture == 'x86lb') {
                                        arch = 'x86'
                                    }
                                    
                                    if (Constants.jitStressModeScenarios.containsKey(scenario) ||
                                            scenario == 'default' ||
                                            scenario == 'r2r' ||
                                            Constants.r2rJitStressScenarios.indexOf(scenario) != -1) {
                                        buildOpts = enableCorefxTesting ? 'skiptests' : ''
                                        buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${arch} ${buildOpts}"
                                    }

                                    // For Pri 1 tests, we must shorten the output test binary path names.
                                    // if __TestIntermediateDir is already set, build-test.cmd will
                                    // output test binaries to that directory. If it is not set, the 
                                    // binaries are sent to a default directory whose name is about
                                    // 35 characters long.

                                    else if (scenario == 'pri1' || scenario == 'pri1r2r' || scenario == 'gcstress15_pri1r2r'|| scenario == 'coverage') {
                                        buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${arch} -priority=1"
                                    }
                                    else if (scenario == 'ilrt') {
                                        // First do the build with skiptests and then build the tests with ilasm roundtrip
                                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} skiptests"
                                        buildCommands += "set __TestIntermediateDir=int&&build-test.cmd ${lowerConfiguration} ${arch} -ilasmroundtrip"
                                    }
                                    else if (isLongGc(scenario)) {
                                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} skiptests"
                                        buildCommands += "set __TestIntermediateDir=int&&build-test.cmd ${lowerConfiguration} ${arch}"
                                    }
                                    else if (scenario == 'formatting') {
                                        buildCommands += "python tests\\scripts\\format.py -c %WORKSPACE% -o Windows_NT -a ${arch}"
                                        break
                                    }
                                    else {
                                        println("Unknown scenario: ${scenario}")
                                        assert false
                                    }
                                    
                                    // If we are running a stress mode, we should write out the set of key
                                    // value env pairs to a file at this point and then we'll pass that to runtest.cmd

                                    if (!isBuildOnly) {
                                        //If this is a crossgen build, pass 'crossgen' to runtest.cmd
                                        def crossgenStr = ''
                                        def runcrossgentestsStr = ''
                                        def runjitstressStr = ''
                                        def runjitstressregsStr = ''
                                        def runjitmioptsStr = ''
                                        def runjitforcerelocsStr = ''
                                        def gcstressStr = ''
                                        def runtestArguments = ''
                                        def gcTestArguments = ''

                                        if (scenario == 'r2r' ||
                                            scenario == 'pri1r2r' ||
                                            scenario == 'gcstress15_pri1r2r' ||
                                            Constants.r2rJitStressScenarios.indexOf(scenario) != -1) {
                                                crossgenStr = 'crossgen'
                                                runcrossgentestsStr = 'runcrossgentests'
                                            
                                                if (scenario == 'r2r_jitstress1'){
                                                    runjitstressStr = 'jitstress 1'
                                                }
                                                else if (scenario == 'r2r_jitstress2') {
                                                    runjitstressStr = 'jitstress 2'
                                                }
                                                else if (scenario == 'r2r_jitstressregs1'){
                                                    runjitstressregsStr = 'jitstressregs 1'
                                                }
                                                else if (scenario == 'r2r_jitstressregs2') {
                                                    runjitstressregsStr = 'jitstressregs 2'
                                                }
                                                else if (scenario == 'r2r_jitstressregs3') {
                                                    runjitstressregsStr = 'jitstressregs 3'
                                                }
                                                else if (scenario == 'r2r_jitstressregs4') {
                                                    runjitstressregsStr = 'jitstressregs 4'
                                                }
                                                else if (scenario == 'r2r_jitstressregs8') {
                                                    runjitstressregsStr = 'jitstressregs 8'
                                                }
                                                else if (scenario == 'r2r_jitstressregsx10') {
                                                    runjitstressregsStr = 'jitstressregs x10'
                                                }
                                                else if (scenario == 'r2r_jitstressregsx80') {
                                                    runjitstressregsStr = 'jitstressregs x80'
                                                }
                                                else if (scenario == 'r2r_jitminopts') {
                                                    runjitmioptsStr = 'jitminopts'
                                                }
                                                else if (scenario == 'r2r_jitforcerelocs') {
                                                    runjitforcerelocsStr = 'jitforcerelocs'
                                                }
                                        }
                                        if (scenario == 'gcstress15_pri1r2r')
                                        {
                                           gcstressStr = 'gcstresslevel 0xF'
                                        }

                                        if (isLongGc(scenario)) {
                                            gcTestArguments = "${scenario} sequential"
                                        }

                                        runtestArguments = "${lowerConfiguration} ${arch} ${gcstressStr} ${crossgenStr} ${runcrossgentestsStr} ${runjitstressStr} ${runjitstressregsStr} ${runjitmioptsStr} ${runjitforcerelocsStr} ${gcTestArguments}"
                                        if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                                            if (enableCorefxTesting) {
                                                // Sync to corefx repo
                                                // Move coreclr files to a subdirectory, %workspace%/clr. Otherwise, corefx build 
                                                // thinks that %workspace% is the project base directory.
                                                buildCommands += "powershell new-item clr -type directory -force"
                                                buildCommands += 'powershell foreach ($x in get-childitem -force) { if (\$x.name -ne \'clr\') { move-item $x clr }}'
                                                buildCommands += "git clone https://github.com/dotnet/corefx fx"
                                                
                                                buildCommands += getStressModeEnvSetCmd(os, scenario);
                                                
                                                // Run corefx build and testing
                                                buildCommands += "cd fx && call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && Build.cmd -Release -- /p:BUILDTOOLS_OVERRIDE_RUNTIME=%WORKSPACE%\\clr\\bin\\Product\\Windows_NT.x64.Checked "                                                                                              
                                            }
                                            else {
                                                def stepScriptLocation = "%WORKSPACE%\\bin\\tests\\SetStressModes.bat"
                                                
                                                if (architecture == 'x86ryujit'){
                                                    def x86Vars = ['COMPLUS_AltJit' : '*', 'COMPLUS_AltJitName' : 'protojit.dll', 'COMPLUS_NoGuiOnAssert' : '1']
                                                    buildCommands += genStressModeScriptStep(os, scenario, Constants.jitStressModeScenarios[scenario] + x86Vars, stepScriptLocation)
                                                }
                                                else {
                                                    buildCommands += genStressModeScriptStep(os, scenario, Constants.jitStressModeScenarios[scenario], stepScriptLocation)
                                                }
                                                
                                                // Run tests with the 
                                                buildCommands += "tests\\runtest.cmd ${runtestArguments} TestEnv ${stepScriptLocation}"
                                            }
                                        }
                                        else if (architecture == 'x64') {
                                            buildCommands += "tests\\runtest.cmd ${runtestArguments}"
                                        }                                        
                                        else if (architecture == 'x86ryujit') {
                                            def testEnvLocation = "%WORKSPACE%\\tests\\x86\\ryujit_x86_testenv.cmd"
                                            
                                            buildCommands += "tests\\runtest.cmd ${runtestArguments} TestEnv ${testEnvLocation}"
                                        }
                                        else if (architecture == 'x86lb') {
                                            buildCommands += "tests\\runtest.cmd ${runtestArguments} Exclude0 x86_legacy_backend_issues.targets"
                                        }
                                    }

                                    if (!enableCorefxTesting) {
                                        // Run the rest of the build    
                                        // Build the mscorlib for the other OS's
                                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} linuxmscorlib"
                                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} freebsdmscorlib"
                                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} osxmscorlib"
                                    
                                        // Zip up the tests directory so that we don't use so much space/time copying
                                        // 10s of thousands of files around.
                                        buildCommands += "powershell -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${arch}.${configuration}', '.\\bin\\tests\\tests.zip')\"";
                                        
                                        if (!Constants.jitStressModeScenarios.containsKey(scenario)) {
                                            // For windows, pull full test results and test drops for x86/x64.
                                            // No need to pull for stress mode scenarios (downstream builds use the default scenario)
                                            Utilities.addArchival(newJob, "bin/Product/**,bin/tests/tests.zip")
                                        }
                                        
                                        if (!isBuildOnly) {
                                            if (architecture == 'x64' || !isPR) {
                                                Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml')
                                            }
                                            setTestJobTimeOut(newJob, scenario)
                                        }
                                    }
                                    else {
                                        // Archive only result xml files since corefx/bin/tests is very large around 10 GB.                                        
                                        // For windows, pull full test results and test drops for x86/x64
                                        Utilities.addArchival(newJob, "fx/bin/tests/**/testResults.xml")
                                        
                                        // Set timeout 
                                        setTestJobTimeOut(newJob, scenario)
                                        
                                        if (architecture == 'x64' || !isPR) {
                                            Utilities.addXUnitDotNETResults(newJob, 'fx/bin/tests/**/testResults.xml')
                                        }
                                    }
                                    
                                    break
                                case 'arm64':
                                    assert (scenario == 'default') || (scenario == 'pri1r2r') || (scenario == 'gcstress0x3') || (scenario == 'gcstress0xc')
                                    // Set time out
                                    setTestJobTimeOut(newJob, scenario)

                                    // Debug runs take too long to run. So build job only.
                                    if (lowerConfiguration == "debug") {
                                       buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${architecture} toolset_dir C:\\ats2"
                                    }
                                    else {
                                       buildCommands += "set __TestIntermediateDir=int&&build.cmd skiptests ${lowerConfiguration} ${architecture} toolset_dir C:\\ats2"
                                       // Test build and run are launched together.
                                       buildCommands += "Z:\\arm64\\common\\scripts\\arm64PostLauncher.cmd %WORKSPACE% ${architecture} ${lowerConfiguration} ${scenario}"
                                       Utilities.addXUnitDotNETResults(newJob, 'bin/tests/testResults.xml')
                                    }

                                    // Add archival.
                                    Utilities.addArchival(newJob, "bin/Product/**")
                                    break
                                default:
                                    println("Unknown architecture: ${architecture}");
                                    assert false
                                    break
                            }
                            break
                        case 'Ubuntu':
                        case 'Ubuntu16.04':
                        case 'Debian8.4':
                        case 'OSX':
                        case 'FreeBSD':
                        case 'CentOS7.1':
                        case 'RHEL7.2':
                        case 'OpenSUSE13.2':
                        case 'OpenSUSE42.1':
                        case 'Fedora23':
                            switch (architecture) {
                                case 'x64':
                                case 'x86ryujit':
                                case 'x86lb':
                                    def arch = architecture
                                    if (architecture == 'x86ryujit' || architecture == 'x86lb') {
                                        arch = 'x86'
                                    }

                                    if (scenario == 'formatting') {
                                        buildCommands += "python tests/scripts/format.py -c \${WORKSPACE} -o Linux -a ${arch}"
                                        break
                                    }
                                
                                    if (!enableCorefxTesting) {
                                        // We run pal tests on all OS but generate mscorlib (and thus, nuget packages)
                                        // only on supported OS platforms.
                                        if ((os == 'FreeBSD') || (os == 'OpenSUSE13.2'))
                                        {
                                            buildCommands += "./build.sh skipmscorlib verbose ${lowerConfiguration} ${arch}"
                                        }
                                        else
                                        {
                                            def bootstrapRid = Utilities.getBoostrapPublishRid(os)
                                            def bootstrapRidEnv = bootstrapRid != null ? "__PUBLISH_RID=${bootstrapRid} " : ''
                                            buildCommands += "${bootstrapRidEnv}./build.sh verbose ${lowerConfiguration} ${arch}"
                                        }
                                        buildCommands += "src/pal/tests/palsuite/runpaltests.sh \${WORKSPACE}/bin/obj/${osGroup}.${arch}.${configuration} \${WORKSPACE}/bin/paltestout"
                                    
                                        // Set time out
                                        setTestJobTimeOut(newJob, scenario)
                                        // Basic archiving of the build
                                        Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**/*.dylib,bin/obj/*/tests/**/*.so")
                                        // And pal tests
                                        Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                                    }
                                    else {
                                        // Corefx stress testing                                        
                                        assert os == 'Ubuntu'
                                        assert architecture == 'x64'
                                        assert lowerConfiguration == 'checked'
                                        
                                        // Build coreclr and move it to clr directory
                                        buildCommands += "./build.sh verbose ${lowerConfiguration} ${architecture}"
                                        buildCommands += "rm -rf .clr; mkdir .clr; mv * .clr; mv .git .clr; mv .clr clr"
                                        
                                        // Get corefx
                                        buildCommands += "git clone https://github.com/dotnet/corefx fx"
                                        
                                        // Set environment variable
                                        def setEnvVar = getStressModeEnvSetCmd(os, scenario)

                                        // Build and text corefx
                                        buildCommands += "rm -rf \$WORKSPACE/fx_home; mkdir \$WORKSPACE/fx_home"
                                        buildCommands += setEnvVar
                                        buildCommands += "cd fx; export HOME=\$WORKSPACE/fx_home; ./build.sh -Release -Outerloop -TestWithLocalLibraries -- /p:BUILDTOOLS_OVERRIDE_RUNTIME=\$WORKSPACE/clr/bin/Product/Linux.x64.Checked"  

                                        // Archive and process test result
                                        Utilities.addArchival(newJob, "fx/bin/tests/**/testResults.xml")
                                        setTestJobTimeOut(newJob, scenario)
                                        Utilities.addXUnitDotNETResults(newJob, 'fx/bin/tests/**/testResults.xml')
                                    }
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
                                    // All builds for ARM architecture are run on Ubuntu currently
                                    assert os == 'Ubuntu'
                                    if (isLinuxEmulatorBuild == false) {
                                        buildCommands += """echo \"Using rootfs in /opt/arm-liux-genueabihf-root\"
                                            ROOTFS_DIR=/opt/arm-linux-genueabihf-root ./build.sh skipmscorlib arm cross verbose ${lowerConfiguration}"""
                                        
                                        // Basic archiving of the build, no pal tests
                                        Utilities.addArchival(newJob, "bin/Product/**")
                                        break
                                    }
                                    else {
                                        // Make sure the build configuration is either of debug or release
                                        assert ( lowerConfiguration == 'debug' ) || ( lowerConfiguration == 'release' )

                                        // Setup variables to hold emulator folder path and the rootfs mount path
                                        def armemul_path = '/opt/linux-arm-emulator'
                                        def armrootfs_mountpath = '/opt/linux-arm-emulator-root'

                                        // Unzip the Windows test binaries first. Exit with 0
                                        buildCommands += "unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/Windows_NT.x64.${configuration} || exit 0"

                                        // Unpack the corefx binaries
                                        buildCommands += "tar -xf ./bin/build.tar.gz"

                                        // Call the ARM emulator build script to cross build and test using the ARM emulator rootfs
                                        buildCommands += """./tests/scripts/arm32_ci_script.sh \\
                                        --emulatorPath=${armemul_path} \\
                                        --mountPath=${armrootfs_mountpath} \\
                                        --buildConfig=${lowerConfiguration} \\
                                        --testRootDir=./bin/tests/Windows_NT.x64.${configuration} \\
                                        --coreFxNativeBinDir=./bin/Linux.arm-softfp.${configuration} \\
                                        --coreFxBinDir=\"./bin/Linux.AnyCPU.${configuration};./bin/Unix.AnyCPU.${configuration};./bin/AnyOS.AnyCPU.${configuration}\" \\
                                        --testDirFile=./tests/testsRunningInsideARM.txt"""


                                        // Basic archiving of the build
                                        Utilities.addArchival(newJob, "bin/Product/**")
                                        break
                                    }
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
                                // Setup corefx and Windows test binaries for Linux ARM Emulator Build
                                if (isLinuxEmulatorBuild) {
                                    // Define the Windows Tests and Corefx build job names
                                    def WindowTestsName = projectFolder + '/' +
                                                          Utilities.getFullJobName(project,
                                                                                   getJobName(lowerConfiguration,
                                                                                              'x64' ,
                                                                                              'windows_nt',
                                                                                              'default',
                                                                                              true),
                                                                                   false)
                                    def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' +
                                                       Utilities.getFolderName(branch)

                                    // Copy the Windows test binaries and the Corefx build binaries
                                    copyArtifacts(WindowTestsName) {
                                        excludePatterns('**/testResults.xml', '**/*.ni.dll')
                                        buildSelector {
                                            latestSuccessful(true)
                                        }
                                    }
                                    copyArtifacts("${corefxFolder}/linuxarmemulator_cross_${lowerConfiguration}") {
                                        includePatterns('bin/build.tar.gz')
                                        buildSelector {
                                            latestSuccessful(true)
                                        }
                                    }
                                }

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


// Create the Linux/OSX/CentOS coreclr test leg for debug and release and each scenario
combinedScenarios.each { scenario ->
    [true, false].each { isPR ->
        // Architectures.  x64 only at this point
        ['x64'].each { architecture ->
            // Put the OS's supported for coreclr cross testing here
            Constants.crossList.each { os ->
                Constants.configurationList.each { configuration ->

                    if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                        if (configuration != 'Checked') {
                            return
                        }
                        if (isCorefxTesting(scenario)) {
                            return
                        }
                        //Skip stress modes for these scenarios
                        if (os == 'RHEL7.2' || os == 'Debian8.4' || os == 'OpenSUSE13.2') {
                            return
                        }
                    }
                    // If this is a r2r jitstress, jitstressregs, jitminopts or forcerelocs scenario
                    // and configuration is not Checked, bail out.
                    else if (configuration != 'Checked' && Constants.r2rJitStressScenarios.indexOf(scenario) != -1) {
                        return;
                    }
                    // For CentOS, we only want Checked/Release pri1 builds.
                    else if (os == 'CentOS7.1') {
                        if (scenario != 'pri1' &&
                            scenario != 'r2r' && 
                            scenario != 'pri1r2r' && 
                            scenario != 'gcstress15_pri1r2r' &&
                            Constants.r2rJitStressScenarios.indexOf(scenario) == -1) {
                            return
                        }
                        if (configuration != 'Checked' && configuration != 'Release') {
                            return
                        }
                    }
                    // For RedHat, Debian, and OpenSUSE, we only do Release pri1 builds.
                    else if (os == 'RHEL7.2' || os == 'Debian8.4' || os == 'OpenSUSE13.2') {
                        if (scenario != 'pri1') {
                            return
                        }
                        if (configuration != 'Release') {
                            return
                        }
                    }
                    else {
                        // Skip scenarios
                        switch (scenario) {
                            case 'pri1':
                                // Nothing skipped
                                break
                            case 'ilrt':
                                // Release only
                                if (configuration != 'Release') {
                                    return
                                }
                                break
                            case 'r2r':
                                //Skip configs that aren't Checked or Release (so just Debug, for now)
                                if (configuration != 'Checked' && configuration != 'Release') {
                                    return
                                }
                                break
                            case 'pri1r2r':
                                //Skip configs that aren't Checked or Release (so just Debug, for now)
                                if (configuration != 'Checked' && configuration != 'Release') {
                                    return
                                }
                                break
                            case 'gcstress15_pri1r2r':
                            case 'r2r_jitstress1':
                            case 'r2r_jitstress2':
                            case 'r2r_jitstressregs1':
                            case 'r2r_jitstressregs2':
                            case 'r2r_jitstressregs3':
                            case 'r2r_jitstressregs4':
                            case 'r2r_jitstressregs8':
                            case 'r2r_jitstressregsx10':
                            case 'r2r_jitstressregsx80':
                            case 'r2r_jitminopts':
                            case 'r2r_jitforcerelocs':
                                //Skip configs that aren't Checked or Release (so just Debug, for now)
                                if (configuration != 'Checked' && configuration != 'Release') {
                                    return
                                }
                                break
                            case 'longgc':
                            case 'gcsimulator':
                                // Long GC tests take a long time on non-Release builds
                                if (configuration != 'Release') {
                                    return
                                }
                                break
                            case 'coverage':
                                //We only want Ubuntu Release for coverage
                                if (os != 'Ubuntu') {
                                    return
                                }
                                if (configuration != 'Release') {
                                    return
                                }
                            case 'formatting':
                                return
                            case 'default':
                                // Nothing skipped
                                break
                            default:
                                println("Unknown scenario: ${scenario}")
                                assert false
                                break
                        }
                    }
                    
                    def lowerConfiguration = configuration.toLowerCase()
                    def osGroup = getOSGroup(os)
                    def jobName = getJobName(configuration, architecture, os, scenario, false) + "_tst"
                    
                    // Unless this is a coverage test run, we want to copy over the default build of coreclr.
                    def inputScenario = 'default'
                    if (scenario == 'coverage') {
                        inputScenario = 'coverage'
                    }
                    def inputCoreCLRBuildName = projectFolder + '/' + 
                        Utilities.getFullJobName(project, getJobName(configuration, architecture, os, inputScenario, false), isPR)
                    // If this is a stress scenario, there isn't any difference in the build job
                    // so we didn't create a build only job for windows_nt specific to that stress mode.  Just copy
                    // from the default scenario
                    def testBuildScenario = scenario
                    if (testBuildScenario == 'coverage' || testBuildScenario == 'pri1r2r'|| testBuildScenario == 'gcstress15_pri1r2r') {
                        testBuildScenario = 'pri1'
                    }
                    else if ( testBuildScenario == 'r2r' || isLongGc(testBuildScenario)) {
                        testBuildScenario = 'default'
                    }
                    def inputWindowTestsBuildName = ''
                    if (Constants.jitStressModeScenarios.containsKey(testBuildScenario)) {
                        inputWindowTestsBuildName = projectFolder + '/' + 
                            Utilities.getFullJobName(project, getJobName(configuration, architecture, 'windows_nt', 'default', true), isPR)
                    }
                    else {
                        inputWindowTestsBuildName = projectFolder + '/' + 
                            Utilities.getFullJobName(project, getJobName(configuration, architecture, 'windows_nt', testBuildScenario, true), isPR)
                    }
                    // Enable Server GC for Ubuntu PR builds
                    def serverGCString = ''
                    
                    // Whether or not this test run should be run sequentially instead
                    // of in parallel. Only used for long GC tests.
                    def sequentialString = ''
                    
                    // Whether or not this test run should run a specific playlist.
                    // Only used for long GC tests.

                    // A note - runtest.sh does have "--long-gc" and "--gcsimulator" options
                    // for running long GC and GCSimulator tests, respectively. We don't use them
                    // here because using a playlist file produces much more readable output on the CI machines
                    // and reduces running time.
                    def playlistString = ''
                     
                    if (os == 'Ubuntu' && isPR){
                        serverGCString = '--useServerGC'
                    }

                    // pass --crossgen to runtest.sh for crossgen builds
                    def crossgenStr = ''
                    def runcrossgentestsStr = ''
                    def runjitstressStr = ''
                    def runjitstressregsStr = ''
                    def runjitmioptsStr = ''
                    def runjitforcerelocsStr = ''
                    def gcstressStr = ''

                    if (scenario == 'r2r' ||
                        scenario == 'pri1r2r' ||
                        scenario == 'gcstress15_pri1r2r' ||
                        Constants.r2rJitStressScenarios.indexOf(scenario) != -1) {
                            crossgenStr = '--crossgen'
                            runcrossgentestsStr = '--runcrossgentests'
                                            
                            if (scenario == 'r2r_jitstress1'){
                                runjitstressStr = '--jitstress=1'
                            }
                            else if (scenario == 'r2r_jitstress2') {
                                runjitstressStr = '--jitstress=2'
                            }
                            else if (scenario == 'r2r_jitstressregs1'){
                                runjitstressregsStr = '--jitstressregs=1'
                            }
                            else if (scenario == 'r2r_jitstressregs2') {
                                runjitstressregsStr = '--jitstressregs=2'
                            }
                            else if (scenario == 'r2r_jitstressregs3') {
                                runjitstressregsStr = '--jitstressregs=3'
                            }
                            else if (scenario == 'r2r_jitstressregs4') {
                                runjitstressregsStr = '--jitstressregs=4'
                            }
                            else if (scenario == 'r2r_jitstressregs8') {
                                runjitstressregsStr = '--jitstressregs=8'
                            }
                            else if (scenario == 'r2r_jitstressregsx10') {
                                runjitstressregsStr = '--jitstressregs=x10'
                            }
                            else if (scenario == 'r2r_jitstressregsx80') {
                                runjitstressregsStr = '--jitstressregs=x80'
                            }
                            else if (scenario == 'r2r_jitminopts') {
                                runjitmioptsStr = '--jitminopts'
                            }
                            else if (scenario == 'r2r_jitforcerelocs') {
                                runjitforcerelocsStr = '--jitforcerelocs'
                            }
                    }
                    if  (scenario == 'gcstress15_pri1r2r')
                    {
                        gcstressStr = '--gcstresslevel=0xF'
                    }

                    if (isLongGc(scenario)) {
                        // Long GC tests behave very poorly when they are not
                        // the only test running (many of them allocate until OOM).
                        sequentialString = '--sequential'
                        
                        // The Long GC playlist contains all of the tests that are
                        // going to be run. The GCSimulator playlist contains all of
                        // the GC simulator tests.
                        if (scenario == 'longgc') {
                            playlistString = '--long-gc --playlist=./tests/longRunningGcTests.txt'
                        }
                        else if (scenario == 'gcsimulator') {
                            playlistString = '--gcsimulator --playlist=./tests/gcSimulatorTests.txt'
                        }
                    }
                    
                    def folder = isJITStressJob(scenario) ? 'jitstress' : ''
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR, folder)) {
                        // Add parameters for the inputs
                    
                        parameters {
                            stringParam('CORECLR_WINDOWS_BUILD', '', 'Build number to copy CoreCLR windows test binaries from')
                            stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
                        }
                    
                        steps {
                            // Set up the copies
                        
                            // Coreclr build containing the tests and mscorlib
                        
                            copyArtifacts(inputWindowTestsBuildName) {
                                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                                buildSelector {
                                    buildNumber('${CORECLR_WINDOWS_BUILD}')
                                }
                            }

                            if (scenario == 'coverage') {

                                // Move coreclr to clr directory
                                shell("rm -rf .clr; mkdir .clr; mv * .clr; mv .git .clr; mv .clr clr")

                                // Build coreclr
                                shell("./clr/build.sh coverage verbose ${lowerConfiguration} ${architecture}")

                                // Remove folders from obj that we don't expect to be covered. May update this later.
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/ToolBox")
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/debug")
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/ilasm")
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/ildasm")
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/dlls/dbgshim")
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/dlls/mscordac")
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/dlls/mscordbi")

                                // Run PAL tests
                                shell("./clr/src/pal/tests/palsuite/runpaltests.sh \$(pwd)/clr/bin/obj/${osGroup}.${architecture}.${configuration} \$(pwd)/clr/bin/paltestout")

                                // Remove obj files for PAL tests so they're not included in coverage results
                                shell("rm -rf ./clr/bin/obj/Linux.x64.Release/src/pal/tests")
                                
                                // Unzip the tests first.  Exit with 0
                                shell("unzip -q -o ./clr/bin/tests/tests.zip -d ./clr/bin/tests/Windows_NT.${architecture}.${configuration} || exit 0")

                                // Get corefx
                                shell("git clone https://github.com/dotnet/corefx fx")

                                // Build Linux corefx
                                shell("./fx/build-native.sh -release -buildArch=x64 -os=Linux")
                                shell("./fx/build-managed.sh -release -buildArch=x64 -osgroup=Linux -skiptests")

                                def testEnvOpt = ""
                                def scriptFileName = "\$WORKSPACE/set_stress_test_env.sh"
                                def createScriptCmds = genStressModeScriptStep(os, scenario, Constants.jitStressModeScenarios['heapverify1'], scriptFileName)
                                shell("${createScriptCmds}")
                                testEnvOpt = "--test-env=" + scriptFileName

                                // Run corefx tests
                                shell("""./fx/run-test.sh \\
                --coreclr-bins \$(pwd)/clr/bin/Product/${osGroup}.${architecture}.${configuration} \\
                --mscorlib-bins \$(pwd)/clr/bin/Product/${osGroup}.${architecture}.${configuration} \\
                --corefx-tests \$(pwd)/fx/bin/tests/${osGroup}.AnyCPU.${configuration} \\
                --corefx-native-bins \$(pwd)/fx/bin/${osGroup}.${architecture}.${configuration} \\
                --configurationGroup Release""")


                                // Run coreclr tests w/ workstation GC
                                shell("""./clr/tests/runtest.sh \\
                --testRootDir=\"\$(pwd)/clr/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\$(pwd)/clr/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreClrBinDir=\"\$(pwd)/clr/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --mscorlibDir=\"\$(pwd)/clr/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --coreFxBinDir=\"\$(pwd)/fx/bin/${osGroup}.AnyCPU.Release;\$(pwd)/fx/bin/Unix.AnyCPU.Release;\$(pwd)/fx/bin/AnyOS.AnyCPU.Release\" \\
                --coreFxNativeBinDir=\"\$(pwd)/fx/bin/${osGroup}.${architecture}.Release\" \\
                --crossgen --runcrossgentests""")

                                // Run coreclr tests w/ server GC & HeapVerify enabled
                                shell("""./clr/tests/runtest.sh \\
                --testRootDir=\"\$(pwd)/clr/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\$(pwd)/clr/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreOverlayDir=\"\$(pwd)/clr/bin/tests/Windows_NT.${architecture}.${configuration}/Tests/coreoverlay\" \\
                --useServerGC ${testEnvOpt}""")

                                 // Run long-running coreclr GC tests & produce coverage reports
                                shell("""./clr/tests/runtest.sh \\
                --testRootDir=\"\$(pwd)/clr/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\$(pwd)/clr/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreOverlayDir=\"\$(pwd)/clr/bin/tests/Windows_NT.${architecture}.${configuration}/Tests/coreoverlay\" \\
                --long-gc --playlist=\"\$(pwd)/clr/tests/longRunningGcTests.txt\" --coreclr-coverage\\
                --coreclr-objs=\"\$(pwd)/clr/bin/obj/${osGroup}.${architecture}.${configuration}\" \\
                --coreclr-src=\"\$(pwd)/clr/src\" \\
                --coverage-output-dir=\"\${WORKSPACE}/coverage\" """)

                            }
                            else {

                                // Coreclr build we are trying to test
                            
                                copyArtifacts(inputCoreCLRBuildName) {
                                    excludePatterns('**/testResults.xml', '**/*.ni.dll')
                                    buildSelector {
                                        buildNumber('${CORECLR_BUILD}')
                                    }
                                }

                                def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' + Utilities.getFolderName(branch)
                        
                                // Corefx components.  We now have full stack builds on all distros we test here, so we can copy straight from CoreFX jobs.
                                def osJobName = (os == 'Ubuntu') ? 'ubuntu14.04' : os.toLowerCase()
                                copyArtifacts("${corefxFolder}/${osJobName}_release") {
                                    includePatterns('bin/build.tar.gz')
                                    buildSelector {
                                        latestSuccessful(true)
                                    }
                                }
                        
                                // Unpack the corefx binaries
                                shell("tar -xf ./bin/build.tar.gz")

                                // Unzip the tests first.  Exit with 0
                                shell("unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/Windows_NT.${architecture}.${configuration} || exit 0")
                            
                                // Execute the tests
                                // If we are running a stress mode, we'll set those variables first
                                def testEnvOpt = ""
                                if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                                    def scriptFileName = "\$WORKSPACE/set_stress_test_env.sh"
                                    def createScriptCmds = genStressModeScriptStep(os, scenario, Constants.jitStressModeScenarios[scenario], scriptFileName)
                                    if (createScriptCmds != "") {
                                        shell("${createScriptCmds}")
                                        testEnvOpt = "--test-env=" + scriptFileName
                                    }
                                }
                                
                                if (isGCStressRelatedTesting(scenario)) {
                                    shell('./init-tools.sh')
                                }

                                shell("""./tests/runtest.sh \\
                --testRootDir=\"\${WORKSPACE}/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreClrBinDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --mscorlibDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --coreFxBinDir=\"\${WORKSPACE}/bin/${osGroup}.AnyCPU.Release;\${WORKSPACE}/bin/Unix.AnyCPU.Release;\${WORKSPACE}/bin/AnyOS.AnyCPU.Release\" \\
                --coreFxNativeBinDir=\"\${WORKSPACE}/bin/${osGroup}.${architecture}.Release\" \\
                --limitedDumpGeneration \\
                ${testEnvOpt} ${serverGCString} ${gcstressStr} ${crossgenStr} ${runcrossgentestsStr} ${runjitstressStr} ${runjitstressregsStr} ${runjitmioptsStr} ${runjitforcerelocsStr} ${sequentialString} ${playlistString}""")
                            }
                        }
                    }

                    if (scenario == 'coverage') {
                        // Publish coverage reports
                        Utilities.addHtmlPublisher(newJob, '${WORKSPACE}/coverage/Coverage/reports', 'Code Coverage Report', 'coreclr.html')
                        addEmailPublisher(newJob, 'clrcoverage@microsoft.com')
                    }

                    // Experimental: If on Ubuntu 14.04, then attempt to pull in crash dump links
                    if (os in ['Ubuntu']) {
                        SummaryBuilder summaries = new SummaryBuilder()
                        summaries.addLinksSummaryFromFile('Crash dumps from this run:', 'dumplings.txt')
                        summaries.emit(newJob)
                    }

                    setMachineAffinity(newJob, os, architecture)
                    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                    // Set timeouts to 240.
                    setTestJobTimeOut(newJob, scenario)
                    Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')
                
                    // Create a build flow to join together the build and tests required to run this
                    // test.
                    // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
                    // Linux CoreCLR test
                    def flowJobName = getJobName(configuration, architecture, os, scenario, false) + "_flow"
                    def fullTestJobName = projectFolder + '/' + newJob.name
                    // Add a reference to the input jobs for report purposes
                    JobReport.Report.addReference(inputCoreCLRBuildName)
                    JobReport.Report.addReference(inputWindowTestsBuildName)
                    JobReport.Report.addReference(fullTestJobName)
                    def newFlowJob;

                    // If this is a coverage job, we don't copy any input coreCLR build - instead, we build it as part of the flow job,
                    // so that coverage data can be preserved.
                    if (scenario == 'coverage') {
                        newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR, folder)) {
                        buildFlow("""
// Build the input Windows job
windowsBuildJob = build(params, '${inputWindowTestsBuildName}')

// And then build the test build
build(params + [CORECLR_WINDOWS_BUILD: windowsBuildJob.build.number], '${fullTestJobName}')    
""")
                        }
                    // Normal jobs copy a Windows build & a non-Windows build
                    } else {
                        newFlowJob = buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR, folder)) {
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
                        }
                    }

                    setMachineAffinity(newFlowJob, os, architecture)
                    Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")
                    addTriggers(newFlowJob, branch, isPR, architecture, os, configuration, scenario, true, false, false)
                } // configuration
            } // os
        } // architecture
    } // isPR
} // scenario

JobReport.Report.generateJobReport(out)
