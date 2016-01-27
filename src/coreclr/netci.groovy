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

def static getFullBranchName(def branch) {
    def branchMap = ['master':'*/master',
        'rc2':'*/release/1.0.0-rc2',
        'pr':'*/master']
    def fullBranchName = branchMap.get(branch, null)
    assert fullBranchName != null : "Could not find a full branch name for ${branch}"
    return branchMap[branch]
}

// We use this class (vs variables) so that the static functions can access data here.
class Constants {
    // Innerloop build OS's
    // The Windows_NT_BuildOnly OS is a way to speed up the Non-NT builds temporarily by avoiding
    // test execution in the build flow runs.  It generates the exact same build
    // as Windows_NT but without the tests.
    def static osList = ['Ubuntu', 'Debian8.2', 'OSX', 'Windows_NT', 'Windows_NT_BuildOnly', 'FreeBSD', 'CentOS7.1', 'OpenSUSE13.2']
    def static crossList = ['Ubuntu', 'OSX']
    // This is a set of JIT stress modes combined with the set of variables that
    // need to be set to actually enable that stress mode.  The key of the map is the stress mode and
    // the values are the environment variables
    def static jitStressModeScenarios = ['minopts' : ['COMPlus_JitMinOpts' : '1'], 'forcerelocs' : ['COMPlus_ForceRelocs' : '1'],
	           'jitstress1' : ['COMPlus_JitStress' : '1'], 'jitstress2' : ['COMPlus_JitStress' : '2'],
			   'jitstressregs1' : ['COMPlus_JitStressRegs' : '1'], 'jitstressregs2' : ['COMPlus_JitStressRegs' : '2'],
			   'jitstressregs3' : ['COMPlus_JitStressRegs' : '3'], 'jitstressregs4' : ['COMPlus_JitStressRegs' : '4'],
			   'jitstressregs8' : ['COMPlus_JitStressRegs' : '8'], 'jitstressregs0x10' : ['COMPlus_JitStressRegs' : '0x10'],
			   'jitstressregs0x80' : ['COMPlus_JitStressRegs' : '0x80']]
    // This is the basic set of scenarios
    def static basicScenarios = ['default', 'pri1', 'ilrt']
    // This is the set of configurations
    def static configurationList = ['Debug', 'Checked', 'Release']
    // This is the set of architectures
    def static architectureList = ['arm', 'arm64', 'x64', 'x86']
    // This is the set of branches
    def static branchList = ['master', 'rc2', 'pr']
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

// Generates the string for creating a file that sets environment variables
// that makes it possible to run stress modes.  Writes the script to a file called
// SetStressModes.[sh/cmd]
def static genStressModeScriptStep(def os, def stressModeName, def stressModeVars, def stepScriptLocation) {
    def stepScript = ''
    if (os == 'Windows_NT') {
        stepScript += "echo Creating TestEnv Script for ${stressModeName}\r\n"
        stepScript += "del ${stepScriptLocation}\r\n"
        stressModeVars.each{ k, v -> 
            // Write out what we are writing to the script file
            stepScript += "echo Setting ${k}=${v}\r\n"
            // Write out the set itself to the script file`
            stepScript += "echo set ${k}=${v} >> ${stepScriptLocation}\r\n"
        }
    }
    else {
        // For these we don't use a script, we use directly
        stepScript += "echo Setting variables for ${stressModeName}\n"
        stressModeVars.each{ k, v -> 
            // Write out what we are writing to the script file
            stepScript += "echo Setting ${k}=${v}\n"
            // Write out the set itself to the script file`
            stepScript += "${k}=${v}\n"
        }
    }
    return stepScript
}

// Calculates the name of the build job based on some typical parameters.
//
def static getJobName(def configuration, def architecture, def os, def scenario, def isBuildOnly, def branchName) {
    // If the architecture is x64, do not add that info into the build name.
    // Need to change around some systems and other builds to pick up the right builds
    // to do that.
    
    def suffix = scenario != 'default' ? "_${scenario}" : '';
    if (isBuildOnly) {
        suffix += '_bld'
    }
    if (branchName == 'rc2'){
        suffix += '_rc2'
    }
    def baseName = ''
    switch (architecture) {
        case 'x64':
            if (scenario == 'default') {
                // For now we leave x64 off of the name for compatibility with other jobs
                baseName = configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            else {
                baseName = architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            break
        case 'arm64':
        case 'arm':
            // These are cross builds
            baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'x86':
            baseName = architecture.toLowerCase() + '_lb_' + configuration.toLowerCase() + '_' + os.toLowerCase()
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
                        if (isFlowJob || os == 'Windows_NT' || !(os in Constants.crossList)) {
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
                assert (os == 'Windows_NT') || (os in Constants.crossList)
                Utilities.addPeriodicTrigger(job, '@daily')
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
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Priority 1 Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'minopts':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - MinOpts)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break							
						case 'forcerelocs':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - ForceRelocs)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                        case 'jitstressregs1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break							
						case 'jitstressregs2':
							assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break													
						case 'jitstressregs3':
							assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=3)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break													
						case 'jitstressregs4':		
							assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=4)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break													
						case 'jitstressregs8':
							assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=8)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
						case 'jitstressregs0x10':
							assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x10)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
						case 'jitstressregs0x80':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x80)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
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
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Priority 1 Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break
                        case 'minopts':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - MinOpts)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
						case 'forcerelocs':							
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - ForceRelocs)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
							break							
                        case 'jitstress1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstress2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStress=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break
                        case 'jitstressregs1':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=1)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break						
						case 'jitstressregs2':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=2)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break						
						case 'jitstressregs3':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=3)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break						
						case 'jitstressregs4':		
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=4)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break						
						case 'jitstressregs8':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=8)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break						
						case 'jitstressregs0x10':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x10)",
                               "(?i).*test\\W+${os}\\W+${scenario}.*")
                            break						
						case 'jitstressregs0x80':
                            assert (os == 'Windows_NT') || (os in Constants.crossList)
                            Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Build and Test (Jit - JitStressRegs=0x80)",
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
                    Utilities.addGithubPRTrigger(job, "${os} ${architecture} ${configuration} Legacy Backend Build and Test")
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

def combinedScenarios = Constants.basicScenarios + Constants.jitStressModeScenarios.keySet()
combinedScenarios.each { scenario ->
    Constants.branchList.each { branchName ->
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
                    def isPR = (branchName == 'pr');
                    
                    // Skip totally unimplemented (in CI) configurations.
                    switch (architecture) {
                        case 'arm64':
                            // Windows or cross compiled Ubuntu
                            if (os != 'Windows_NT' && os != 'Ubuntu') {
                                return
                            }
                            break
                        case 'arm':
                            // Only Ubuntu cross implemented
                            if (os != 'Ubuntu') {
                                return
                            }
                            break
                        case 'x86':
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
                    if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                        if (configuration != 'Checked') {
                            return
                        }
                        
                        // Since these are just execution time differences,
                        // skip platforms that don't execute the tests here (Windows_NT only)
                        if (os != 'Windows_NT' || isBuildOnly) {
                            return
                        }
                        
                        // No stress modes except on x64 right now (mainly because of bad test state on x86)
                        if (architecture != 'x64') {
                            return
                        }
                    }
                    else {
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
                    }
                
                    // Calculate names
                    def lowerConfiguration = configuration.toLowerCase()
                    def jobName = getJobName(configuration, architecture, os, scenario, isBuildOnly, branchName)
                    
                    // Create the new job
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {}

                    setMachineAffinity(newJob, os, architecture)

                    // Add all the standard options
                    Utilities.standardJobSetup(newJob, project, isPR, getFullBranchName(branchName))
                    addTriggers(newJob, isPR, architecture, os, configuration, scenario, false)
                
                    def buildCommands = [];
                    def osGroup = getOSGroup(os)
                
                    // Calculate the build steps, archival, and xunit results
                    switch (os) {
                        case 'Windows_NT':
                            switch (architecture) {
                                case 'x64':
                                case 'x86':

                                    // This will shorten the output file path lengths
                                    // if __TestIntermediateDir is already set, buildtest.cmd will
                                    // output test binaries to that directory. If it is not set, the 
                                    // binaries are sent to a default directory whose name is about
                                    // 35 characters long.
                                    buildCommands += "set __TestIntermediateDir=int"
                                    
                                    if (scenario == 'default' || Constants.jitStressModeScenarios.containsKey(scenario)) {
                                        buildCommands += "build.cmd ${lowerConfiguration} ${architecture}"
                                    }
                                    else if (scenario == 'pri1') {
                                        buildCommands += "build.cmd ${lowerConfiguration} ${architecture} Priority 1"
                                    }
                                    else if (scenario == 'ilrt') {
                                        // First do the build with skiptestbuild and then build the tests with ilasm roundtrip
                                        buildCommands += "build.cmd ${lowerConfiguration} ${architecture} skiptestbuild"
                                        buildCommands += "tests\\buildtest.cmd ${lowerConfiguration} ${architecture} ilasmroundtrip"
                                    }
                                    else {
                                        println("Unknown scenario: ${scenario}")
                                        assert false
                                    }
                                    
                                    // If we are running a stress mode, we should write out the set of key
                                    // value env pairs to a file at this point and then we'll pass that to runtest.cmd
                                    
                                    if (!isBuildOnly) {
                                        if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                                            def stepScriptLocation = "%WORKSPACE%\\bin\\tests\\SetStressModes.bat"
                                            buildCommands += genStressModeScriptStep(os, scenario, Constants.jitStressModeScenarios[scenario], stepScriptLocation)
                                            
                                            // Run tests with the 
                                            
                                            buildCommands += "tests\\runtest.cmd ${lowerConfiguration} ${architecture} TestEnv ${stepScriptLocation}"
                                        }
                                        else if (architecture == 'x64') {
                                            buildCommands += "tests\\runtest.cmd ${lowerConfiguration} ${architecture}"
                                        }
                                        else if (architecture == 'x86') {
                                            buildCommands += "tests\\runtest.cmd ${lowerConfiguration} ${architecture} Exclude0 x86_legacy_backend_issues.targets"
                                        }
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
                                    
                                    if (!isBuildOnly) {
                                        if (architecture == 'x64' || !isPR) {
                                            Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml')
                                        }
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
combinedScenarios.each { scenario ->
    Constants.branchList.each { branchName ->
        // Architectures.  x64 only at this point
        ['x64'].each { architecture ->
            // Put the OS's supported for coreclr cross testing here
            Constants.crossList.each { os ->
                Constants.configurationList.each { configuration ->

                    def isPR = (branchName == 'pr');

                    if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                        if (configuration != 'Checked') {
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
                    }
                    
                    def lowerConfiguration = configuration.toLowerCase()
                    def osGroup = getOSGroup(os)
                    def jobName = getJobName(configuration, architecture, os, scenario, false, branchName) + "_tst"
                    def inputCoreCLRBuildName = Utilities.getFolderName(project) + '/' + 
                        Utilities.getFullJobName(project, getJobName(configuration, architecture, os, 'default', false, branchName), isPR)
                    // If this is a stress scenario, there isn't any difference in the build job
                    // so we didn't create a build only job for windows_nt specific to that stress mode.  Just copy
                    // from the default scenario
                    def inputWindowTestsBuildName = ''
                    if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                        inputWindowTestsBuildName = Utilities.getFolderName(project) + '/' + 
                            Utilities.getFullJobName(project, getJobName(configuration, architecture, 'windows_nt', 'default', true, branchName), isPR)
                    }
                    else {
                        inputWindowTestsBuildName = Utilities.getFolderName(project) + '/' + 
                            Utilities.getFullJobName(project, getJobName(configuration, architecture, 'windows_nt', scenario, true, branchName), isPR)
                    }
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
                            // If we are running a stress mode, we'll set those variables first
                            def stressModeString = ""
                            if (Constants.jitStressModeScenarios.containsKey(scenario)) {
                                stressModeString = genStressModeScriptStep(os, scenario, Constants.jitStressModeScenarios[scenario], null)
                            }
                            
                            shell("""${stressModeString}
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
                    Utilities.standardJobSetup(newJob, project, isPR, getFullBranchName(branchName))
                    //Pri 1 tests need longer timeout
                    if (scenario == 'pri1') {
                        Utilities.setJobTimeout(newJob, 240)
                    }
                    Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')
                
                    // Create a build flow to join together the build and tests required to run this
                    // test.
                    // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
                    // Linux CoreCLR test
                    def flowJobName = getJobName(configuration, architecture, os, scenario, false, branchName) + "_flow"
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

                    Utilities.standardJobSetup(newFlowJob, project, isPR, getFullBranchName(branchName))
                    //Pri 1 tests need longer timeout
                    if (scenario == 'pri1') {
                        Utilities.setJobTimeout(newFlowJob, 240)
                    }
                    addTriggers(newFlowJob, isPR, architecture, os, configuration, scenario, true)
                } // configuration
            } // os
        } // architecture
    } // isPR
} // scenario
