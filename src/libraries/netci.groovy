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

// Map of osName -> osGroup.
def osGroupMap = ['Windows 7':'Windows_NT',
                  'Windows_NT':'Windows_NT',
                  'Ubuntu14.04':'Linux',
                  'Ubuntu16.04':'Linux',
                  'Ubuntu16.10':'Linux',
                  'Debian8.4':'Linux',
                  'Fedora24':'Linux',
                  'OSX10.12':'OSX',
                  'CentOS7.1': 'Linux',
                  'RHEL7.2': 'Linux',
                  'PortableLinux': 'Linux']

def osShortName = ['Windows 7' : 'win7',
                   'Windows_NT' : 'windows_nt',
                   'Ubuntu14.04' : 'ubuntu14.04',
                   'Ubuntu16.04' : 'ubuntu16.04',
                   'Ubuntu16.10' : 'ubuntu16.10',
                   'Debian8.4' : 'debian8.4',
                   'Fedora24' : 'fedora24',
                   'OSX10.12' : 'osx',
                   'CentOS7.1' : 'centos7.1',
                   'RHEL7.2' : 'rhel7.2',
                   'PortableLinux' : 'portablelinux']

def buildArchConfiguration = ['Debug': 'x86',
                              'Release': 'x64']

def targetGroupOsMapOuterloop = ['netcoreapp': ['Windows 7', 'Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04', 'Ubuntu16.10', 'CentOS7.1',
                                        'RHEL7.2', 'Fedora24', 'Debian8.4', 'OSX10.12', 'PortableLinux']]

def targetGroupOsMapInnerloop = ['netcoreapp': ['Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04', 'Ubuntu16.10', 'CentOS7.1',
                                        'RHEL7.2', 'Fedora24', 'Debian8.4', 'OSX10.12', 'PortableLinux']]

// **************************
// Define code coverage build
// **************************

[true, false].each { isPR ->
    ['local', 'nonlocal'].each { localType ->
        def isLocal = (localType == 'local')

        def newJobName = 'code_coverage_windows'
        def batchCommand = 'call build.cmd && call build-tests.cmd -coverage -outerloop -- /p:IsCIBuild=true'
        if (isLocal) {
            newJobName = "${newJobName}_local"
            batchCommand = "${batchCommand}"
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
            shell('python src/Native/Unix/format-code.py checkonly')
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
// Define outerloop testing for OSes that can build and run.  Run locally on each machine.
// **************************
[true, false].each { isPR ->
    ['netcoreapp'].each { targetGroup ->
        (targetGroupOsMapOuterloop[targetGroup]).each { osName ->
            ['Debug', 'Release'].each { configurationGroup ->

                def osForMachineAffinity = osName
                if (osForMachineAffinity == 'PortableLinux') {
                    // Portable Linux builds happen on RHEL7.2
                    osForMachineAffinity = "RHEL7.2"
                }

                def osGroup = osGroupMap[osName]
                def archGroup = "x64"
                def newJobName = "outerloop_${targetGroup}_${osShortName[osName]}_${configurationGroup.toLowerCase()}"

                def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                    steps {
                        if (osName == 'Windows 7' || osName == 'Windows_NT') {
                            batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd -framework:${targetGroup} -${configurationGroup}")
                            batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build-tests.cmd -framework:${targetGroup} -${configurationGroup} -outerloop -- /p:IsCIBuild=true")
                            batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack .\\bin\\runtime\\${targetGroup}-${osGroup}-${configurationGroup}-${archGroup}")
                        }
                        else if (osName == 'OSX10.12') {
                            shell("HOME=\$WORKSPACE/tempHome ./build.sh -${configurationGroup.toLowerCase()}")
                            shell("HOME=\$WORKSPACE/tempHome ./build-tests.sh -${configurationGroup.toLowerCase()} -outerloop -- /p:IsCIBuild=true")
                            shell("tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-${archGroup}\" .")
                        }
                        else if (osName == 'CentOS7.1') {
                            // On Centos7.1, the cmake toolset is currently installed in /usr/local/bin (it was built manually).  When
                            // running sudo, that will be typically eliminated from the PATH, so let's add it back in.
                            shell("sudo PATH=\$PATH:/usr/local/bin HOME=\$WORKSPACE/tempHome ./build.sh -${configurationGroup.toLowerCase()}")
                            shell("sudo PATH=\$PATH:/usr/local/bin HOME=\$WORKSPACE/tempHome ./build-tests.sh -${configurationGroup.toLowerCase()} -outerloop -- /p:IsCIBuild=true")
                            shell("sudo tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-${archGroup}\" .")
                        }
                        else {
                            def portableLinux = (osName == 'PortableLinux') ? '-portable' : ''
                            shell("sudo HOME=\$WORKSPACE/tempHome ./build.sh -${configurationGroup.toLowerCase()} ${portableLinux}")
                            shell("sudo HOME=\$WORKSPACE/tempHome ./build-tests.sh -${configurationGroup.toLowerCase()} -outerloop -- /p:IsCIBuild=true")
                            // Tar up the appropriate bits.
                            shell("sudo tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-${archGroup}\" .")

                        }
                    }
                }

                // Set the affinity.  OS name matches the machine affinity.
                if (osName == 'Windows_NT' || osName == 'OSX10.12') {
                    Utilities.setMachineAffinity(newJob, osForMachineAffinity, "latest-or-auto-elevated")
                }
                else if (osGroup == 'Linux') {
                    Utilities.setMachineAffinity(newJob, osForMachineAffinity, 'outer-latest-or-auto')
                } else {
                    Utilities.setMachineAffinity(newJob, osForMachineAffinity, 'latest-or-auto');
                }

                // Set up standard options.
                Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                // Add the unit test results
                Utilities.addXUnitDotNETResults(newJob, 'bin/**/testResults.xml')
                def archiveContents = "msbuild.log"
                if (osName.contains('Windows')) {
                    // Packer.exe is a .NET Framework application. When we can use it from the tool-runtime, we can archive the ".pack" file here.
                    archiveContents += ",bin/build.pack"
                }
                else {
                    archiveContents += ",bin/build.tar.gz"
                }
                // Add archival for the built data.
                Utilities.addArchival(newJob, archiveContents, '', doNotFailIfNothingArchived=true, archiveOnlyIfSuccessful=false)
                // Set up appropriate triggers.  PR on demand, otherwise nightly
                if (isPR) {
                    // Set PR trigger.
                    // TODO: More elaborate regex trigger?
                    Utilities.addGithubPRTriggerForBranch(newJob, branch, "OuterLoop ${targetGroup} ${osName} ${configurationGroup} ${archGroup}", "(?i).*test\\W+outerloop\\W+${targetGroup} ${osName}\\W+${configurationGroup}.*")
                }
                else {
                    // Set a periodic trigger
                    Utilities.addPeriodicTrigger(newJob, '@daily')
                }
            }
        }
    }
}


// **************************
// Define innerloop testing.  These jobs run on every merge and a subset of them run on every PR, the ones
// that don't run per PR can be requested via a magic phrase.
// **************************
[true, false].each { isPR ->
    ['netcoreapp'].each { targetGroup ->
        (targetGroupOsMapInnerloop[targetGroup]).each { osName ->
            ['Debug', 'Release'].each { configurationGroup ->
                def osGroup = osGroupMap[osName]
                def osForMachineAffinity = osName

                if (osForMachineAffinity == 'PortableLinux') {
                    // Portable Linux builds happen on RHEL7.2
                    osForMachineAffinity = "RHEL7.2"
                }
                def archGroup = "x64"
                if (osName == 'Windows 7' || osName == 'Windows_NT') {
                    // On Windows, use different architectures for Debug and Release.
                    archGroup = buildArchConfiguration[configurationGroup]
                }
                def targetGroupString = targetGroup != 'netcoreapp' ? "${targetGroup}_" : '';
                def newJobName = "${targetGroupString}${osName.toLowerCase()}_${configurationGroup.toLowerCase()}"

                def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                    // On Windows we use the packer to put together everything. On *nix we use tar
                    steps {
                        if (osName == 'Windows 7' || osName == 'Windows_NT') {
                            batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd -${configurationGroup} -os:${osGroup} -buildArch:${archGroup} -framework:${targetGroup}")
                            batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build-tests.cmd -${configurationGroup} -os:${osGroup} -buildArch:${archGroup} -framework:${targetGroup} -- /p:IsCIBuild=true")
                            batchFile("C:\\Packer\\Packer.exe .\\bin\\build.pack .\\bin")
                        }
                        else {
                            // Use Server GC for Ubuntu/OSX Debug PR build & test
                            def useServerGC = (configurationGroup == 'Release' && isPR) ? 'useServerGC' : ''
                            def portableLinux = (osName == 'PortableLinux') ? '-portable' : ''
                            shell("HOME=\$WORKSPACE/tempHome ./build.sh -${configurationGroup.toLowerCase()} -framework:${targetGroup} -os:${osGroup} ${portableLinux} -buildArch:${archGroup}")
                            shell("HOME=\$WORKSPACE/tempHome ./build-tests.sh -${configurationGroup.toLowerCase()} -framework:${targetGroup} -os:${osGroup} -buildArch:${archGroup} -- ${useServerGC} /p:IsCIBuild=true")
                            // Tar up the appropriate bits.
                            shell("tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-x64\" .")
                        }
                    }
                }

                // Set the affinity.
                Utilities.setMachineAffinity(newJob, osForMachineAffinity, 'latest-or-auto')
                // Set up standard options.
                Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                // Add the unit test results
                Utilities.addXUnitDotNETResults(newJob, 'bin/**/testResults.xml')
                def archiveContents = "msbuild.log"
                if (osName.contains('Windows')) {
                    // Packer.exe is a .NET Framework application. When we can use it from the tool-runtime, we can archive the ".pack" file here.
                    archiveContents += ",bin/build.pack"
                }
                else {
                    archiveContents += ",bin/build.tar.gz"
                }
                // Add archival for the built data.
                Utilities.addArchival(newJob, archiveContents, '', doNotFailIfNothingArchived=true, archiveOnlyIfSuccessful=false)
                // Set up triggers
                if (isPR) {
                    targetGroupString = targetGroupString.replaceAll('_', ' ');
                    Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${targetGroupString}${osName} ${configurationGroup} ${archGroup} Build and Test", "(?i).*test\\W+innerloop\\W+${targetGroupString}${osName}\\W+${configurationGroup}.*")
                }
                else {
                    // Set a push trigger
                    Utilities.addGithubPushTrigger(newJob)
                }
            }
        }
    }
}

// **************************
// Define Linux ARM builds. These jobs run on every merge.
// Some jobs run on every PR. The ones that don't run per PR can be requested via a phrase.
// **************************
[true, false].each { isPR ->
    ['netcoreapp'].each { targetGroup ->
        ['Debug', 'Release'].each { configurationGroup ->
            ['Linux', 'Tizen'].each { osName ->
                if (osName == "Linux") {
                    linuxCodeName="xenial"
                    abi = "arm"
                }
                else if (osName == "Tizen") {
                    linuxCodeName="tizen"
                    abi = "armel"
                }

                def osGroup = "Linux"
                def newJobName = "${osName.toLowerCase()}_${abi.toLowerCase()}_cross_${configurationGroup.toLowerCase()}"

                def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                    steps {
                        // Call the arm32_ci_script.sh script to perform the cross build of native corefx
                        def script = "./cross/arm32_ci_script.sh --buildConfig=${configurationGroup.toLowerCase()} --${abi} --linuxCodeName=${linuxCodeName} --verbose"
                        shell(script)

                        // Tar up the appropriate bits.
                        shell("tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-${abi}\" .")
                    }
                }

                // The cross build jobs run on Ubuntu. The arm-cross-latest version
                // contains the packages needed for cross building corefx
                Utilities.setMachineAffinity(newJob, 'Ubuntu14.04', 'arm-cross-latest')

                // Set up standard options.
                Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

                // Add archival for the built binaries
                def archiveContents = "bin/build.tar.gz"
                Utilities.addArchival(newJob, archiveContents)
                
                newJob.with {
                    publishers {
                        azureVMAgentPostBuildAction {
                            agentPostBuildAction('Delete agent after build execution (when idle).')
                        }
                    }
                }

                // Set up triggers
                if (isPR) {
                    // We run Tizen Debug and Linux Release as default PR builds
                    if ((osName == "Tizen" && configurationGroup == "Debug") || (osName == "Linux" && configurationGroup == "Release")) {
                        Utilities.addGithubPRTriggerForBranch(newJob, branch, "${osName} ${abi} ${configurationGroup} Build")
                    }
                    else {
                        Utilities.addGithubPRTriggerForBranch(newJob, branch, "${osName} ${abi} ${configurationGroup} Build", "(?i).*test\\W+${osName}\\W+${abi}\\W+${configurationGroup}.*")
                    }
                }
                else {
                    // Set a push trigger
                    Utilities.addGithubPushTrigger(newJob)
                }
            } // osName
        } // configurationGroup
    } // targetGroup
} // isPR

// **************************
// Define Linux x86 builds. These jobs run daily and results will be used for CoreCLR test
// TODO: innerloop & outerloop testing & merge to general job generation routine
// **************************
['Debug', 'Release'].each { configurationGroup ->
    def osName = 'Ubuntu16.04'
    def archGroup = 'x86'
    def newJobName = "${osName.toLowerCase()}_${archGroup}_${configurationGroup.toLowerCase()}"

    def newJob = job(Utilities.getFullJobName(project, newJobName, false)) {
        steps {
            // Call x86_ci_script.sh script to perform the cross build of native corefx
            def script = "./cross/x86_ci_script.sh --buildConfig=${configurationGroup.toLowerCase()}"
            shell(script)

            // Tar up the appropriate bits
            shell("tar -czf bin/build.tar.gz --directory=\"bin/Linux.${archGroup}.${configurationGroup}/native\" .")
        }
    }

    // The cross build jobs run on Ubuntu 14.04 in spite of the target is Ubuntu 16.04.
    // The ubuntu 14.04 arm-cross-latest version contains the packages needed for cross building corefx
    Utilities.setMachineAffinity(newJob, 'Ubuntu14.04', 'arm-cross-latest')

    // Set up standard options.
    Utilities.standardJobSetup(newJob, project, false, "*/${branch}")

    // Add archival for the built binaries
    def archiveContents = "bin/build.tar.gz"
    Utilities.addArchival(newJob, archiveContents)

    // Set a push trigger as a daily work
    Utilities.addPeriodicTrigger(newJob, '@daily')
}

JobReport.Report.generateJobReport(out)

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.

Utilities.addCROSSCheck(this, project, branch)
