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
def osGroupMap = ['Ubuntu14.04':'Linux',
                  'Ubuntu16.04':'Linux',
                  'Ubuntu16.10':'Linux',
                  'Debian8.4':'Linux',
                  'Fedora24':'Linux',
                  'OSX10.12':'OSX',
                  'Windows_NT':'Windows_NT',
                  'CentOS7.1': 'Linux',
                  'OpenSUSE13.2': 'Linux',
                  'OpenSUSE42.1': 'Linux',
                  'RHEL7.2': 'Linux',
                  'Tizen': 'Linux',
                  'LinuxARMEmulator': 'Linux',
                  'PortableLinux': 'Linux']

def osShortName = ['Windows 10': 'win10',
                   'Windows 7' : 'win7',
                   'Windows_NT' : 'windows_nt',
                   'Ubuntu14.04' : 'ubuntu14.04',
                   'OSX10.12' : 'osx',
                   'Windows Nano 2016' : 'winnano16',
                   'Ubuntu16.04' : 'ubuntu16.04',
                   'Ubuntu16.10' : 'ubuntu16.10',
                   'CentOS7.1' : 'centos7.1',
                   'Debian8.4' : 'debian8.4',
                   'OpenSUSE13.2' : 'opensuse13.2',
                   'OpenSUSE42.1' : 'opensuse42.1',
                   'Fedora24' : 'fedora24',
                   'RHEL7.2' : 'rhel7.2',
                   'PortableLinux' : 'portablelinux']

def buildArchConfiguration = ['Debug': 'x86',
                              'Release': 'x64']

def targetGroupOsMap = ['netcoreapp': ['Windows 10', 'Windows 7', 'Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04', 'Ubuntu16.10', 'CentOS7.1', 'OpenSUSE13.2', 'OpenSUSE42.1', 
                                        'RHEL7.2', 'Fedora24', 'Debian8.4', 'OSX10.12', 'PortableLinux'],
                        'netfx': ['Windows_NT']]

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
// Define outerloop windows Nano testing.  Run locally on each machine.
// **************************
[true, false].each { isPR ->
    ['Windows Nano 2016'].each { osName ->
        ['Debug', 'Release'].each { configurationGroup ->

            def newJobName = "outerloop_${osShortName[osName]}_${configurationGroup.toLowerCase()}"

            def newBuildJobName = "outerloop_${osShortName[osName]}_${configurationGroup.toLowerCase()}_bld"

            def newBuildJob = job(Utilities.getFullJobName(project, newBuildJobName, isPR)) {
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd -os=Windows_NT -${configurationGroup} -skipTests -outerloop -- /p:IsCIBuild=true")
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
            def newTestJobName =  "outerloop_${osShortName[osName]}_${configurationGroup.toLowerCase()}_tst"
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
            Utilities.setMachineAffinity(newTestJob, osName)
            // Set up standard options.
            Utilities.addStandardOptions(newTestJob, isPR)
            // Add the unit test results
            Utilities.addXUnitDotNETResults(newTestJob, 'bin/**/testResults.xml')

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
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "OuterLoop ${osName} ${configurationGroup}", "(?i).*test\\W+outerloop\\W+${osName}\\W+${configurationGroup}.*")
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
    ['netcoreapp', 'netfx'].each { targetGroup ->
        (targetGroupOsMap[targetGroup]).each { osName ->
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
                        if (osName == 'Windows 10' || osName == 'Windows 7' || osName == 'Windows_NT') {
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
                            shell("tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-${archGroup}\" .")
                        }
                        else {
                            def portableLinux = (osName == 'PortableLinux') ? '-portable' : ''
                            shell("sudo HOME=\$WORKSPACE/tempHome ./build.sh -${configurationGroup.toLowerCase()} ${portableLinux}")
                            shell("sudo HOME=\$WORKSPACE/tempHome ./build-tests.sh -${configurationGroup.toLowerCase()} -outerloop -- /p:IsCIBuild=true")
                            // Tar up the appropriate bits.
                            shell("tar -czf bin/build.tar.gz --directory=\"bin/runtime/${targetGroup}-${osGroup}-${configurationGroup}-${archGroup}\" .")

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
// Define perf testing.  Built locally and submitted to Helix.
// **************************

// builds with secrets should never be available for pull requests.
// right now perf tests are only run on Win10 (but can be built on any Windows)
['Windows 10'].each { osName ->
    ['Debug', 'Release'].each { configurationGroup ->

        def newJobName = "perf_${osShortName[osName]}_${configurationGroup.toLowerCase()}"

        def newJob = job(Utilities.getFullJobName(project, newJobName, /* isPR */ false)) {
            steps {
                helix("Build.cmd -- /p:Creator=dotnet-bot /p:ArchiveTests=true /p:ConfigurationGroup=${configurationGroup} /p:Configuration=Windows_${configurationGroup} /p:TestDisabled=true /p:EnableCloudTest=true /p:BuildMoniker={uniqueId} /p:TargetQueue=Windows.10.Amd64 /p:TestProduct=CoreFx /p:Branch=master /p:OSGroup=Windows_NT /p:CloudDropAccountName=dotnetbuilddrops /p:CloudResultsAccountName=dotnetjobresults /p:CloudDropAccessToken={CloudDropAccessToken} /p:CloudResultsAccessToken={CloudResultsAccessToken} /p:BuildCompleteConnection={BuildCompleteConnection} /p:BuildIsOfficialConnection={BuildIsOfficialConnection} /p:DocumentDbKey={DocumentDbKey} /p:DocumentDbUri=https://hms.documents.azure.com:443/ /p:FuncTestsDisabled=true /p:Performance=true")
            }
        }

        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')

        Utilities.setMachineAffinity(newJob, 'Windows_NT', 'latest-or-auto')

        // Set up standard options.
        Utilities.standardJobSetup(newJob, project, /* isPR */ false, "*/${branch}")

        // Set a periodic trigger
        Utilities.addPeriodicTrigger(newJob, '@daily')

        Utilities.addPrivatePermissions(newJob)
    }
}

// **************************
// Define target group vertical builds that will run on every merge.
// **************************
[true, false].each { isPR ->
    ['uap', 'uapaot', 'netfx'].each { targetGroup ->
        ['Debug'].each { configurationGroup ->
            ['Windows_NT'].each { osName ->
                def osGroup = osGroupMap[osName]
                def osForMachineAffinity = osName

                def newJobName = "${targetGroup}_${configurationGroup.toLowerCase()}"

                def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                    // On Windows we use the packer to put together everything. On *nix we use tar
                    steps {
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd -${configurationGroup} -framework:${targetGroup}")
                        batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build-tests.cmd -${configurationGroup} -framework:${targetGroup} -SkipTests")
                    }
                }
                // Set the affinity.
                Utilities.setMachineAffinity(newJob, osForMachineAffinity, 'latest-or-auto')
                // Set up standard options.
                Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                // Set up triggers
                if (isPR) {
                    Utilities.addGithubPRTriggerForBranch(newJob, branch, "Vertical ${targetGroup} Build")
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
// Define AllConfigurations builds that will run on every merge.
// **************************
[true, false].each { isPR ->
    ['Debug'].each { configurationGroup ->
        ['Windows_NT'].each { osName ->
            def osGroup = osGroupMap[osName]
            def osForMachineAffinity = osName

            def newJobName = "AllConfigurations_${configurationGroup.toLowerCase()}"

            def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                // On Windows we use the packer to put together everything. On *nix we use tar
                steps {
                    batchFile("call \"C:\\Program Files (x86)\\Microsoft Visual Studio 14.0\\VC\\vcvarsall.bat\" x86 && build.cmd -${configurationGroup} -allConfigurations")
                }
            }
            // Set the affinity.
            Utilities.setMachineAffinity(newJob, osForMachineAffinity, 'latest-or-auto')
            // Set up standard options.
            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
            // Set up triggers
            if (isPR) {
                Utilities.addGithubPRTriggerForBranch(newJob, branch, "${osName} ${configurationGroup} AllConfigurations Build")
            }
            else {
                // Set a push trigger
                Utilities.addGithubPushTrigger(newJob)
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
        ['Debug', 'Release'].each { configurationGroup ->
            ['Windows_NT', 'Ubuntu14.04', 'Ubuntu16.04', 'Ubuntu16.10', 'Debian8.4', 'CentOS7.1', 'OpenSUSE13.2', 'OpenSUSE42.1', 'Fedora24', 'RHEL7.2', 'OSX10.12', 'PortableLinux'].each { osName ->
                def osGroup = osGroupMap[osName]
                def osForMachineAffinity = osName
                
                if (osForMachineAffinity == 'PortableLinux') {
                    // Portable Linux builds happen on RHEL7.2
                    osForMachineAffinity = "RHEL7.2"
                }
                def archGroup = "x64"
                if (osName == 'Windows 10' || osName == 'Windows 7' || osName == 'Windows_NT') {
                    // On Windows, use different architectures for Debug and Release.
                    archGroup = buildArchConfiguration[configurationGroup]
                }
                def newJobName = "${osName.toLowerCase()}_${configurationGroup.toLowerCase()}"

                def newJob = job(Utilities.getFullJobName(project, newJobName, isPR)) {
                    // On Windows we use the packer to put together everything. On *nix we use tar
                    steps {
                        if (osName == 'Windows 10' || osName == 'Windows 7' || osName == 'Windows_NT') {
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
                    // Set PR trigger, we run Windows_NT, Ubuntu 14.04, CentOS 7.1, PortableLinux and OSX on every PR.
                    if ( osName == 'Windows_NT' || osName == 'Ubuntu14.04' || osName == 'CentOS7.1' || osName == 'OSX10.12' || osName== 'PortableLinux') {
                        Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${osName} ${configurationGroup} ${archGroup} Build and Test")
                    }
                    else {
                        Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${osName} ${configurationGroup} ${archGroup} Build and Test", "(?i).*test\\W+innerloop\\W+${osName}\\W+${configurationGroup}.*")
                    }
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
            ['Ubuntu14.04', 'Ubuntu16.04', 'Tizen'].each { osName ->
                if (osName == "Ubuntu14.04") {
                    linuxCodeName="trusty"
                    abi = "arm"
                }
                else if (osName == "Ubuntu16.04") {
                    linuxCodeName="xenial"
                    abi = "arm"
                }
                else if (osName == "Tizen") {
                    linuxCodeName="tizen"
                    abi = "armel"
                }

                def osGroup = osGroupMap[osName]
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

                // Set up triggers
                if (isPR) {
                    // We run Tizen release/debug, Ubuntu 14.04 release and Ubuntu 16.04 debug for ARM on every PR.
                    if (osName == "Tizen" || (osName == "Ubuntu14.04" && configurationGroup == "Release") || (osName == "Ubuntu16.04" && configurationGroup == "Debug")) {
                        Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${osName} ${abi} ${configurationGroup} Cross Build")
                    }
                    else {
                        Utilities.addGithubPRTriggerForBranch(newJob, branch, "Innerloop ${osName} ${abi} ${configurationGroup} Cross Build", "(?i).*test\\W+innerloop\\W+${osName}\\W+${abi}\\W+${configurationGroup}.*")
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
