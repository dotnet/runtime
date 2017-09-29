// Import the utility functionality.

import jobs.generation.*;

def project = GithubProject
def branch = GithubBranchName
def projectName = Utilities.getFolderName(project)
def projectFolder = projectName + '/' + Utilities.getFolderName(branch)

def static getOSGroup(def os) {
    def osGroupMap = ['Ubuntu14.04':'Linux',
        'RHEL7.2': 'Linux',
        'Ubuntu16.04': 'Linux',
        'Debian8.4':'Linux',
        'Fedora24':'Linux',
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

// Setup perflab tests runs
[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
        ['x64', 'x86'].each { arch ->
            [true, false].each { isSmoketest ->
                ['ryujit', 'legacy_backend'].each { jit ->

                    if (arch == 'x64' && jit == 'legacy_backend') {
                        return
                    }

                    ['full_opt', 'min_opt'].each { opt_level ->
                        if (isSmoketest && opt_level == 'min_opt') {
                            return
                        }

                        def architecture = arch
                        def jobName = isSmoketest ? "perf_perflab_${os}_${arch}_${opt_level}_${jit}_smoketest" : "perf_perflab_${os}_${arch}_${opt_level}_${jit}"
                        def testEnv = ""

                        if (jit == 'legacy_backend') {
                            testEnv = '-testEnv %WORKSPACE%\\tests\\legacyjit_x86_testenv.cmd'
                        }

                        def newJob = job(Utilities.getFullJobName(project, jobName, isPR)) {
                            // Set the label.
                            label('windows_server_2016_clr_perf')
                            wrappers {
                                credentialsBinding {
                                    string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                                }
                            }

                            if (isPR) {
                                parameters {
                                    stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
                                }
                            }
                            if (isSmoketest) {
                                parameters {
                                    stringParam('XUNIT_PERFORMANCE_MAX_ITERATION', '2', 'Sets the number of iterations to two.  We want to do this so that we can run as fast as possible as this is just for smoke testing')
                                    stringParam('XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED', '2', 'Sets the number of iterations to two.  We want to do this so that we can run as fast as possible as this is just for smoke testing')
                                }
                            }
                            else {
                                parameters {
                                    stringParam('XUNIT_PERFORMANCE_MAX_ITERATION', '21', 'Sets the number of iterations to twenty one.  We are doing this to limit the amount of data that we upload as 20 iterations is enough to get a good sample')
                                    stringParam('XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED', '21', 'Sets the number of iterations to twenty one.  We are doing this to limit the amount of data that we upload as 20 iterations is enough to get a good sample')
                                }
                            }

                            def configuration = 'Release'
                            def runType = isPR ? 'private' : 'rolling'
                            def benchViewName = isPR ? 'coreclr private %BenchviewCommitName%' : 'coreclr rolling %GIT_BRANCH_WITHOUT_ORIGIN% %GIT_COMMIT%'
                            def uploadString = isSmoketest ? '' : '-uploadToBenchview'

                            steps {
                                // Batch

                                batchFile("powershell wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile \"%WORKSPACE%\\nuget.exe\"")
                                batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\"")
                                batchFile("\"%WORKSPACE%\\nuget.exe\" install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")
                                //Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                                //we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                                batchFile("if \"%GIT_BRANCH:~0,7%\" == \"origin/\" (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%\") else (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%\")\n" +
                                "set \"BENCHVIEWNAME=${benchViewName}\"\n" +
                                "set \"BENCHVIEWNAME=%BENCHVIEWNAME:\"=%\"\n" +
                                "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"%BENCHVIEWNAME%\" --user \"dotnet-bot@microsoft.com\"\n" +
                                "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type ${runType}")
                                batchFile("py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"")
                                batchFile("set __TestIntermediateDir=int&&build.cmd ${configuration} ${architecture}")

                                batchFile("tests\\runtest.cmd ${configuration} ${architecture} GenerateLayoutOnly")

                                def runXUnitPerfCommonArgs = "-arch ${arch} -configuration ${configuration} -generateBenchviewData \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" ${uploadString} -runtype ${runType} ${testEnv} -optLevel ${opt_level} -jitName ${jit} -stabilityPrefix \"START \"CORECLR_PERF_RUN\" /B /WAIT /HIGH /AFFINITY 0x2\""

                                // Run with just stopwatch: Profile=Off
                                batchFile("tests\\scripts\\run-xunit-perf.cmd ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${architecture}.${configuration}\\performance\\perflab\\Perflab -library")
                                batchFile("xcopy.exe /VYQK bin\\sandbox\\Logs\\Perf-*.* bin\\toArchive\\sandbox\\Logs\\Perflab\\Off\\")

                                batchFile("tests\\scripts\\run-xunit-perf.cmd ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${architecture}.${configuration}\\Jit\\Performance\\CodeQuality")
                                batchFile("xcopy.exe /VYQK bin\\sandbox\\Logs\\Perf-*.* bin\\toArchive\\sandbox\\Logs\\CodeQuality\\Off\\")

                                // Run with the full set of counters enabled: Profile=On
                                if (opt_level != 'min_opt') {
                                    batchFile("tests\\scripts\\run-xunit-perf.cmd ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${architecture}.${configuration}\\performance\\perflab\\Perflab -library -collectionFlags default+BranchMispredictions+CacheMisses+InstructionRetired+gcapi")
                                    batchFile("xcopy.exe /VYQK bin\\sandbox\\Logs\\Perf-*.* bin\\toArchive\\sandbox\\Logs\\Perflab\\On\\")

                                    batchFile("tests\\scripts\\run-xunit-perf.cmd ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${architecture}.${configuration}\\Jit\\Performance\\CodeQuality -collectionFlags default+BranchMispredictions+CacheMisses+InstructionRetired+gcapi")
                                    batchFile("xcopy.exe /VYQK bin\\sandbox\\Logs\\Perf-*.* bin\\toArchive\\sandbox\\Logs\\CodeQuality\\On\\")
                                }
                            }
                        }

                        if (isSmoketest) {
                            Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')
                        }

                        def archiveSettings = new ArchivalSettings()
                        archiveSettings.addFiles('bin/toArchive/**')
                        archiveSettings.addFiles('machinedata.json')

                        Utilities.addArchival(newJob, archiveSettings)
                        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

                        newJob.with {
                            logRotator {
                                artifactDaysToKeep(30)
                                daysToKeep(30)
                                artifactNumToKeep(200)
                                numToKeep(200)
                            }
                            wrappers {
                                timeout {
                                    absolute(240)
                                }
                            }
                        }

                        if (isPR) {
                            TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
                            if (isSmoketest) {
                                builder.setGithubContext("${os} ${arch} ${opt_level} ${jit} CoreCLR Perf Tests Correctness")
                            }
                            else {
                                builder.setGithubContext("${os} ${arch} ${opt_level} ${jit} CoreCLR Perf Tests")

                                def opts = ""
                                if (opt_level == 'min_opt') {
                                    opts = '\\W+min_opts'
                                }
                                def jitt = ""
                                if (jit != 'ryujit') {
                                    jitt = "\\W+${jit}"
                                }

                                builder.triggerOnlyOnComment()
                                builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+${arch}${opts}${jitt}\\W+perf.*")
                            }
                            builder.triggerForBranch(branch)
                            builder.emitTrigger(newJob)
                        }
                        else {
                            // Set a push trigger
                            TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
                            builder.emitTrigger(newJob)
                        }
                    }
                }
            }
        }
    }
}

// Setup throughput perflab tests runs
[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
        ['x64', 'x86'].each { arch ->
            ['ryujit', 'legacy_backend'].each { jit ->
                [true, false].each { pgo_optimized ->
                    if (arch == 'x64' && jit == 'legacy_backend') {
                        return
                    }

                    // pgo not supported for legacy_backend
                    if (pgo_optimized && jit == 'legacy_backend') {
                        return
                    }

                    ['full_opt', 'min_opt'].each { opt_level ->
                        def architecture = arch

                        pgo_build = ""
                        pgo_test = ""
                        pgo_string = "pgo"
                        if (!pgo_optimized) {
                            pgo_build = " -nopgooptimize"
                            pgo_test = " -nopgo"
                            pgo_string = "nopgo"
                        }

                        def newJob = job(Utilities.getFullJobName(project, "perf_throughput_perflab_${os}_${arch}_${opt_level}_${jit}_${pgo_string}", isPR)) {
                            // Set the label.
                            label('windows_server_2016_clr_perf')
                            wrappers {
                                credentialsBinding {
                                    string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                                }
                            }

                            if (isPR) {
                                parameters {
                                    stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that will be used to build the full title of a run in Benchview.')
                                }
                            }

                            def configuration = 'Release'
                            def runType = isPR ? 'private' : 'rolling'
                            def benchViewName = isPR ? 'coreclr-throughput private %BenchviewCommitName%' : 'coreclr-throughput rolling %GIT_BRANCH_WITHOUT_ORIGIN% %GIT_COMMIT%'

                            steps {
                                // Batch
                                batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\"")
                                batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.ThroughputBenchmarks.${architecture}.${os}\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.ThroughputBenchmarks.${architecture}.${os}\"")
                                batchFile("C:\\Tools\\nuget.exe install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")
                                batchFile("C:\\Tools\\nuget.exe install Microsoft.BenchView.ThroughputBenchmarks.${architecture}.${os} -Source https://dotnet.myget.org/F/dotnet-core -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")
                                //Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                                //we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                                batchFile("if \"%GIT_BRANCH:~0,7%\" == \"origin/\" (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%\") else (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%\")\n" +
                                "set \"BENCHVIEWNAME=${benchViewName}\"\n" +
                                "set \"BENCHVIEWNAME=%BENCHVIEWNAME:\"=%\"\n" +
                                "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"${benchViewName}\" --user \"dotnet-bot@microsoft.com\"\n" +
                                "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type ${runType}")
                                batchFile("py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"")
                                batchFile("set __TestIntermediateDir=int&&build.cmd ${configuration} ${architecture}${pgo_build} skiptests")
                                batchFile("tests\\runtest.cmd ${configuration} ${architecture} GenerateLayoutOnly")
                                batchFile("py -u tests\\scripts\\run-throughput-perf.py -arch ${arch} -os ${os} -configuration ${configuration} -opt_level ${opt_level} -jit_name ${jit}${pgo_test} -clr_root \"%WORKSPACE%\" -assembly_root \"%WORKSPACE%\\Microsoft.BenchView.ThroughputBenchmarks.${architecture}.${os}\\lib\" -benchview_path \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" -run_type ${runType}")
                            }
                        }

                        // Save machinedata.json to /artifact/bin/ Jenkins dir
                        def archiveSettings = new ArchivalSettings()
                        archiveSettings.addFiles('throughput-*.csv')
                        Utilities.addArchival(newJob, archiveSettings)

                        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

                        if (isPR) {
                            def opts = ""
                            if (opt_level == 'min_opt') {
                                opts = '\\W+min_opts'
                            }

                            def jitt = ""
                            if (jit != 'ryujit') {
                                jitt = "\\W+${jit}"
                            }

                            def pgo_trigger = ""
                            if (pgo_optimized) {
                                pgo_trigger = "\\W+nopgo"
                            }


                            TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
                            builder.setGithubContext("${os} ${arch} ${opt_level} ${jit} ${pgo_string} CoreCLR Throughput Perf Tests")
                            builder.triggerOnlyOnComment()
                            builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+${arch}${opts}${jitt}${pgo_trigger}\\W+throughput.*")
                            builder.triggerForBranch(branch)
                            builder.emitTrigger(newJob)
                        }
                        else {
                            // Set a push trigger
                            TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
                            builder.emitTrigger(newJob)
                        }
                    }
                }
            }
        }
    }
}

def static getFullPerfJobName(def project, def os, def isPR) {
    return Utilities.getFullJobName(project, "perf_${os}", isPR)
}

// Create the Linux/OSX/CentOS coreclr test leg for debug and release and each scenario
[true, false].each { isPR ->
    def fullBuildJobName = Utilities.getFullJobName(project, 'perf_linux_build', isPR)
    def architecture = 'x64'
    def configuration = 'Release'

    // Build has to happen on RHEL7.2 (that's where we produce the bits we ship)
    ['RHEL7.2'].each { os ->
        def newBuildJob = job(fullBuildJobName) {
            steps {
                shell("./build.sh verbose ${architecture} ${configuration}")
            }
        }
        Utilities.setMachineAffinity(newBuildJob, os, 'latest-or-auto')
        Utilities.standardJobSetup(newBuildJob, project, isPR, "*/${branch}")
        Utilities.addArchival(newBuildJob, "bin/Product/**,bin/obj/*/tests/**/*.dylib,bin/obj/*/tests/**/*.so", "bin/Product/**/.nuget/**")
    }


    // Actual perf testing on the following OSes
    def perfOSList = ['Ubuntu14.04']
    perfOSList.each { os ->
        def newJob = job(getFullPerfJobName(project, os, isPR)) {

            label('linux_clr_perf')
            wrappers {
                credentialsBinding {
                    string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                }
            }

            if (isPR) {
                parameters {
                    stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
                }
            }

            parameters {
                // Cap the maximum number of iterations to 21.
                stringParam('XUNIT_PERFORMANCE_MAX_ITERATION', '21', 'Sets the number of iterations to twenty one.  We are doing this to limit the amount of data that we upload as 20 iterations is enough to get a good sample')
                stringParam('XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED', '21', 'Sets the number of iterations to twenty one.  We are doing this to limit the amount of data that we upload as 20 iterations is enough to get a good sample')
                stringParam('PRODUCT_BUILD', '', 'Build number from which to copy down the CoreCLR Product binaries built for Linux')
            }

            def osGroup = getOSGroup(os)
            def runType = isPR ? 'private' : 'rolling'
            def benchViewName = isPR ? 'coreclr private \$BenchviewCommitName' : 'coreclr rolling \$GIT_BRANCH_WITHOUT_ORIGIN \$GIT_COMMIT'

            steps {
                shell("./tests/scripts/perf-prep.sh")
                shell("./init-tools.sh")
                copyArtifacts(fullBuildJobName) {
                    includePatterns("bin/**")
                    buildSelector {
                        buildNumber('\${PRODUCT_BUILD}')
                    }
                }
                shell("GIT_BRANCH_WITHOUT_ORIGIN=\$(echo \$GIT_BRANCH | sed \"s/[^/]*\\/\\(.*\\)/\\1 /\")\n" +
                "python3.5 \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools/submission-metadata.py\" --name \" ${benchViewName} \" --user \"dotnet-bot@microsoft.com\"\n" +
                "python3.5 \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools/build.py\" git --branch \$GIT_BRANCH_WITHOUT_ORIGIN --type ${runType}")
                shell("""./tests/scripts/run-xunit-perf.sh \\
                --testRootDir=\"\${WORKSPACE}/bin/tests/Windows_NT.${architecture}.${configuration}\" \\
                --testNativeBinDir=\"\${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
                --coreClrBinDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --mscorlibDir=\"\${WORKSPACE}/bin/Product/${osGroup}.${architecture}.${configuration}\" \\
                --coreFxBinDir=\"\${WORKSPACE}/corefx\" \\
                --runType=\"${runType}\" \\
                --benchViewOS=\"${os}\" \\
                --generatebenchviewdata=\"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools\" \\
                --stabilityPrefix=\"taskset 0x00000002 nice --adjustment=-10\" \\
                --uploadToBenchview""")
                shell("mkdir -p bin/toArchive/sandbox/Logs/")
                shell("rsync -a bin/sandbox/Logs/Perf-*.* bin/toArchive/sandbox/Logs/")
            }
        }

        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles('bin/toArchive/**')
        archiveSettings.addFiles('machinedata.json')

        Utilities.addArchival(newJob, archiveSettings)
        Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

        // For perf, we need to keep the run results longer
        newJob.with {
            // Enable the log rotator
            logRotator {
                artifactDaysToKeep(30)
                daysToKeep(30)
                artifactNumToKeep(200)
                numToKeep(200)
            }
            wrappers {
                timeout {
                    absolute(240)
                }
            }
        }
    } // os

    def flowJobPerfRunList = perfOSList.collect { os ->
        "{ build(params + [PRODUCT_BUILD: b.build.number], '${getFullPerfJobName(project, os, isPR)}') }"
    }
    def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, "perf_linux_flow", isPR, '')) {
        if (isPR) {
            parameters {
                stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
            }
        }
        buildFlow("""
// First, build the bits on RHEL7.2
b = build(params, '${fullBuildJobName}')

// Then, run the perf tests
parallel(
    ${flowJobPerfRunList.join(",\n    ")}
)
""")
    }

    Utilities.setMachineAffinity(newFlowJob, 'Windows_NT', 'latest-or-auto')
    Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")

    if (isPR) {
        TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
        builder.setGithubContext("Linux Perf Test Flow")
        builder.triggerOnlyOnComment()
        builder.setCustomTriggerPhrase("(?i).*test\\W+linux\\W+perf\\W+flow.*")
        builder.triggerForBranch(branch)
        builder.emitTrigger(newFlowJob)
    }
    else {
        // Set a push trigger
        TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
        builder.emitTrigger(newFlowJob)
    }

} // isPR

def static getFullThroughputJobName(def project, def os, def isPR) {
    return Utilities.getFullJobName(project, "perf_throughput_${os}", isPR)
}

// Create the Linux/OSX/CentOS coreclr test leg for debug and release and each scenario
[true, false].each { isPR ->
    def fullBuildJobName = Utilities.getFullJobName(project, 'perf_throughput_linux_build', isPR)
    def architecture = 'x64'
    def configuration = 'Release'

    // Build has to happen on RHEL7.2 (that's where we produce the bits we ship)
    ['RHEL7.2'].each { os ->
        def newBuildJob = job(fullBuildJobName) {
            steps {
                shell("./build.sh verbose ${architecture} ${configuration}")
            }
        }
        Utilities.setMachineAffinity(newBuildJob, os, 'latest-or-auto')
        Utilities.standardJobSetup(newBuildJob, project, isPR, "*/${branch}")
        Utilities.addArchival(newBuildJob, "bin/Product/**")
    }

    // Actual perf testing on the following OSes
    def throughputOSList = ['Ubuntu14.04']
    def throughputOptLevelList = ['full_opt', 'min_opt']

    def throughputOSOptLevelList = []

    throughputOSList.each { os ->
        throughputOptLevelList.each { opt_level ->
            throughputOSOptLevelList.add("${os}_${opt_level}")
        }
    }

    throughputOSList.each { os ->
        throughputOptLevelList.each { opt_level ->
            def newJob = job(getFullThroughputJobName(project, "${os}_${opt_level}", isPR)) {

                label('linux_clr_perf')
                    wrappers {
                        credentialsBinding {
                            string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                        }
                    }

                if (isPR) {
                    parameters {
                        stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that will be used to build the full title of a run in Benchview.')
                    }
                }

                parameters {
                    stringParam('PRODUCT_BUILD', '', 'Build number from which to copy down the CoreCLR Product binaries built for Linux')
                }

                def osGroup = getOSGroup(os)
                def runType = isPR ? 'private' : 'rolling'
                def benchViewName = isPR ? 'coreclr-throughput private \$BenchviewCommitName' : 'coreclr-throughput rolling \$GIT_BRANCH_WITHOUT_ORIGIN \$GIT_COMMIT'

                steps {
                    shell("bash ./tests/scripts/perf-prep.sh --throughput")
                    shell("./init-tools.sh")
                    copyArtifacts(fullBuildJobName) {
                        includePatterns("bin/Product/**")
                        buildSelector {
                            buildNumber('\${PRODUCT_BUILD}')
                        }
                    }
                    shell("GIT_BRANCH_WITHOUT_ORIGIN=\$(echo \$GIT_BRANCH | sed \"s/[^/]*\\/\\(.*\\)/\\1 /\")\n" +
                    "python3.5 \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools/submission-metadata.py\" --name \" ${benchViewName} \" --user \"dotnet-bot@microsoft.com\"\n" +
                    "python3.5 \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools/build.py\" git --branch \$GIT_BRANCH_WITHOUT_ORIGIN --type ${runType}")
                    shell("""python3.5 ./tests/scripts/run-throughput-perf.py \\
                    -arch \"${architecture}\" \\
                    -os \"${os}\" \\
                    -configuration \"${configuration}\" \\
                    -opt_level \"${opt_level}\" \\
                    -clr_root \"\${WORKSPACE}\" \\
                    -assembly_root \"\${WORKSPACE}/Microsoft.Benchview.ThroughputBenchmarks.${architecture}.Windows_NT/lib\" \\
                    -run_type \"${runType}\" \\
                    -benchview_path \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools\"""")
                }
            }

            // Save machinedata.json to /artifact/bin/ Jenkins dir
            def archiveSettings = new ArchivalSettings()
            archiveSettings.addFiles('throughput-*.csv')
            archiveSettings.addFiles('machinedata.json')
            Utilities.addArchival(newJob, archiveSettings)

            Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

            // For perf, we need to keep the run results longer
            newJob.with {
                // Enable the log rotator
                logRotator {
                    artifactDaysToKeep(7)
                    daysToKeep(300)
                    artifactNumToKeep(25)
                    numToKeep(1000)
                }
            }
        } // opt_level
    } // os

    def flowJobTPRunList = throughputOSOptLevelList.collect { os ->
        "{ build(params + [PRODUCT_BUILD: b.build.number], '${getFullThroughputJobName(project, os, isPR)}') }"
    }
    def newFlowJob = buildFlowJob(Utilities.getFullJobName(project, "perf_throughput_linux_flow", isPR, '')) {
        if (isPR) {
            parameters {
                stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
            }
        }
        buildFlow("""
// First, build the bits on RHEL7.2
b = build(params, '${fullBuildJobName}')

// Then, run the perf tests
parallel(
    ${flowJobTPRunList.join(",\n    ")}
)
""")
    }

    Utilities.setMachineAffinity(newFlowJob, 'Windows_NT', 'latest-or-auto')
    Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")

    if (isPR) {
        TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
        builder.setGithubContext("Linux Throughput Perf Test Flow")
        builder.triggerOnlyOnComment()
        builder.setCustomTriggerPhrase("(?i).*test\\W+linux\\W+throughput\\W+flow.*")
        builder.triggerForBranch(branch)
        builder.emitTrigger(newFlowJob)
    }
    else {
        // Set a push trigger
        TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
        builder.emitTrigger(newFlowJob)
    }

} // isPR

// Setup CoreCLR-Scenarios tests
[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
        ['x64', 'x86'].each { arch ->
            ['ryujit', 'legacy_backend'].each { jit ->

                if (arch == 'x64' && jit == 'legacy_backend') {
                    return
                }

                ['full_opt', 'min_opt'].each { opt_level ->
                    def architecture = arch
                    def newJob = job(Utilities.getFullJobName(project, "perf_scenarios_${os}_${arch}_${opt_level}_${jit}", isPR)) {

                        def testEnv = ""
                        if (jit == 'legacy_backend') {
                            testEnv = '-testEnv %WORKSPACE%\\tests\\legacyjit_x86_testenv.cmd'
                        }

                        // Set the label.
                        label('windows_server_2016_clr_perf')
                        wrappers {
                            credentialsBinding {
                                string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                            }
                        }

                        if (isPR) {
                            parameters {
                                stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
                            }
                        }

                        parameters {
                            stringParam('XUNIT_PERFORMANCE_MAX_ITERATION', '1', 'Size test, one iteration is sufficient')
                            stringParam('XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED', '1', 'Size test, one iteration is sufficient')
                        }

                        def configuration = 'Release'
                        def runType = isPR ? 'private' : 'rolling'
                        def benchViewName = isPR ? 'CoreCLR-Scenarios private %BenchviewCommitName%' : 'CoreCLR-Scenarios rolling %GIT_BRANCH_WITHOUT_ORIGIN% %GIT_COMMIT%'
                        def uploadString = '-uploadToBenchview'

                        steps {
                            // Batch
                            batchFile("powershell wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile \"%WORKSPACE%\\nuget.exe\"")
                            batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\"")
                            batchFile("\"%WORKSPACE%\\nuget.exe\" install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")

                            //Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                            //we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                            batchFile("if \"%GIT_BRANCH:~0,7%\" == \"origin/\" (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%\") else (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%\")\n" +
                            "set \"BENCHVIEWNAME=${benchViewName}\"\n" +
                            "set \"BENCHVIEWNAME=%BENCHVIEWNAME:\"=%\"\n" +
                            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"%BENCHVIEWNAME%\" --user \"dotnet-bot@microsoft.com\"\n" +
                            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type ${runType}")
                            batchFile("py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"")
                            batchFile("set __TestIntermediateDir=int&&build.cmd ${configuration} ${architecture}")

                            batchFile("tests\\runtest.cmd ${configuration} ${architecture} GenerateLayoutOnly")

                            def runXUnitPerfCommonArgs = "-arch ${arch} -configuration ${configuration} -generateBenchviewData \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" ${uploadString} -runtype ${runType} ${testEnv} -optLevel ${opt_level} -jitName ${jit} -scenarioTest"

                            // Scenario: JitBench
                            batchFile("tests\\scripts\\run-xunit-perf.cmd ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${architecture}.${configuration}\\performance\\Scenario\\JitBench -group CoreCLR-Scenarios")
                            batchFile("xcopy.exe /VYQK bin\\sandbox\\Perf-*.* bin\\toArchive\\sandbox\\Logs\\Scenario\\JitBench\\")
                        }
                    }

                    def archiveSettings = new ArchivalSettings()
                    archiveSettings.addFiles('bin/toArchive/**')
                    archiveSettings.addFiles('machinedata.json')

                    Utilities.addArchival(newJob, archiveSettings)
                    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

                    newJob.with {
                        logRotator {
                            artifactDaysToKeep(30)
                            daysToKeep(30)
                            artifactNumToKeep(200)
                            numToKeep(200)
                        }
                        wrappers {
                            timeout {
                                absolute(240)
                            }
                        }
                    }

                    if (isPR) {
                        def opts = ""
                        if (opt_level == 'min_opt') {
                            opts = '\\W+min_opts'
                        }
                        def jitt = ""
                        if (jit != 'ryujit') {
                            jitt = "\\W+${jit}"
                        }

                        TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
                        builder.setGithubContext("${os} ${arch} ${opt_level} ${jit} Performance Scenarios Tests")
                        builder.triggerOnlyOnComment()
                        builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+${arch}{$opts}${jitt}\\W+perf\\W+scenarios.*")
                        builder.triggerForBranch(branch)
                        builder.emitTrigger(newJob)
                    }
                    else {
                        // Set a push trigger
                        TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
                        builder.emitTrigger(newJob)
                    }
                }
            }
        }
    }
}

// Setup size-on-disk test
['Windows_NT'].each { os ->
    ['x64', 'x86'].each { arch ->
        def architecture = arch
        def newJob = job(Utilities.getFullJobName(project, "sizeondisk_${arch}", false)) {

            wrappers {
                credentialsBinding {
                    string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                }
            }

            def channel = 'master'
            def configuration = 'Release'
            def runType = 'rolling'
            def benchViewName = 'Dotnet Size on Disk %DATE% %TIME%'
            def testBin = "%WORKSPACE%\\bin\\tests\\${os}.${architecture}.${configuration}"
            def coreRoot = "${testBin}\\Tests\\Core_Root"
            def benchViewTools = "%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools"

            steps {
                // Install nuget and get BenchView tools
                batchFile("powershell wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile \"%WORKSPACE%\\nuget.exe\"")
                batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\"")
                batchFile("\"%WORKSPACE%\\nuget.exe\" install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")

                // Generate submission metadata for BenchView
                // Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                // we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                batchFile("if \"%GIT_BRANCH:~0,7%\" == \"origin/\" (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%\") else (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%\")\n" +
                "set \"BENCHVIEWNAME=${benchViewName}\"\n" +
                "set \"BENCHVIEWNAME=%BENCHVIEWNAME:\"=%\"\n" +
                "py \"${benchViewTools}\\submission-metadata.py\" --name \"%BENCHVIEWNAME%\" --user \"dotnet-bot@microsoft.com\"\n" +
                "py \"${benchViewTools}\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type ${runType}")

                // Generate machine data from BenchView
                batchFile("py \"${benchViewTools}\\machinedata.py\"")

                // Build CoreCLR and gnerate test layout
                batchFile("set __TestIntermediateDir=int&&build.cmd ${configuration} ${architecture}")
                batchFile("tests\\runtest.cmd ${configuration} ${architecture} GenerateLayoutOnly")

                // Run the size on disk benchmark
                batchFile("\"${coreRoot}\\CoreRun.exe\" \"${testBin}\\sizeondisk\\sodbench\\SoDBench\\SoDBench.exe\" -o \"%WORKSPACE%\\sodbench.csv\" --architecture ${arch} --channel ${channel}")

                // From sodbench.csv, create measurment.json, then submission.json
                batchFile("py \"${benchViewTools}\\measurement.py\" csv \"%WORKSPACE%\\sodbench.csv\" --metric \"Size on Disk\" --unit \"bytes\" --better \"desc\"")
                batchFile("py \"${benchViewTools}\\submission.py\" measurement.json --build build.json --machine-data machinedata.json --metadata submission-metadata.json --group \"Dotnet Size on Disk\" --type ${runType} --config-name ${configuration} --architecture ${arch} --machinepool VM --config Channel ${channel}")

                // If this is a PR, upload submission.json
                batchFile("py \"${benchViewTools}\\upload.py\" submission.json --container coreclr")
            }
        }

        Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')

        def archiveSettings = new ArchivalSettings()
        archiveSettings.addFiles('bin/toArchive/**')
        archiveSettings.addFiles('machinedata.json')

        Utilities.addArchival(newJob, archiveSettings)
        Utilities.standardJobSetup(newJob, project, false, "*/${branch}")

        // Set the cron job here.  We run nightly on each flavor, regardless of code changes
        Utilities.addPeriodicTrigger(newJob, "@daily", true /*always run*/)

        newJob.with {
            logRotator {
                artifactDaysToKeep(30)
                daysToKeep(30)
                artifactNumToKeep(200)
                numToKeep(200)
            }
            wrappers {
                timeout {
                    absolute(240)
                }
            }
        }
    }
}

// Setup IlLink tests
[true, false].each { isPR ->
    ['Windows_NT'].each { os ->
        ['x64'].each { arch ->
            ['ryujit'].each { jit ->
                ['full_opt'].each { opt_level ->
                    def architecture = arch
                    def newJob = job(Utilities.getFullJobName(project, "perf_illink_${os}_${arch}_${opt_level}_${jit}", isPR)) {

                        def testEnv = ""
                        wrappers {
                            credentialsBinding {
                                string('BV_UPLOAD_SAS_TOKEN', 'CoreCLR Perf BenchView Sas')
                            }
                        }

                        if (isPR) {
                            parameters {
                                stringParam('BenchviewCommitName', '\${ghprbPullTitle}', 'The name that you will be used to build the full title of a run in Benchview.  The final name will be of the form <branch> private BenchviewCommitName')
                            }
                        }

                        parameters {
                            stringParam('XUNIT_PERFORMANCE_MAX_ITERATION', '1', 'Size test, one iteration is sufficient')
                            stringParam('XUNIT_PERFORMANCE_MAX_ITERATION_INNER_SPECIFIED', '1', 'Size test, one iteration is sufficient')
                        }

                        def configuration = 'Release'
                        def runType = isPR ? 'private' : 'rolling'
                        def benchViewName = isPR ? 'CoreCLR-Scenarios private %BenchviewCommitName%' : 'CoreCLR-Scenarios rolling %GIT_BRANCH_WITHOUT_ORIGIN% %GIT_COMMIT%'
                        def uploadString = '-uploadToBenchview'

                        steps {
                            // Batch
                            batchFile("powershell wget https://dist.nuget.org/win-x86-commandline/latest/nuget.exe -OutFile \"%WORKSPACE%\\nuget.exe\"")
                            batchFile("if exist \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\" rmdir /s /q \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\"")
                            batchFile("\"%WORKSPACE%\\nuget.exe\" install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -OutputDirectory \"%WORKSPACE%\" -Prerelease -ExcludeVersion")

                            //Do this here to remove the origin but at the front of the branch name as this is a problem for BenchView
                            //we have to do it all as one statement because cmd is called each time and we lose the set environment variable
                            batchFile("if \"%GIT_BRANCH:~0,7%\" == \"origin/\" (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH:origin/=%\") else (set \"GIT_BRANCH_WITHOUT_ORIGIN=%GIT_BRANCH%\")\n" +
                            "set \"BENCHVIEWNAME=${benchViewName}\"\n" +
                            "set \"BENCHVIEWNAME=%BENCHVIEWNAME:\"=%\"\n" +
                            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"%BENCHVIEWNAME%\" --user \"dotnet-bot@microsoft.com\"\n" +
                            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type ${runType}")
                            batchFile("py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\"")
                            batchFile("set __TestIntermediateDir=int&&build.cmd ${configuration} ${architecture}")

                            batchFile("tests\\runtest.cmd ${configuration} ${architecture} GenerateLayoutOnly")

                            def runXUnitPerfCommonArgs = "-arch ${arch} -configuration ${configuration} -generateBenchviewData \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" ${uploadString} -runtype ${runType} ${testEnv} -optLevel ${opt_level} -jitName ${jit} -scenarioTest"

                            // Scenario: ILLink
                            batchFile("tests\\scripts\\run-xunit-perf.cmd ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${architecture}.${configuration}\\performance\\linkbench\\linkbench -group ILLink -nowarmup")
                            batchFile("xcopy.exe /VYQK bin\\sandbox\\Perf-*.* bin\\toArchive\\sandbox\\Logs\\Scenario\\LinkBench\\")
                        }
                    }

                    def archiveSettings = new ArchivalSettings()
                    archiveSettings.addFiles('bin/toArchive/**')
                    archiveSettings.addFiles('machinedata.json')

                    // Set the label (currently we are only measuring size, therefore we are running on VM).
                    Utilities.setMachineAffinity(newJob, "Windows_NT", '20170427-elevated')
                    Utilities.addArchival(newJob, archiveSettings)
                    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")

                    newJob.with {
                        logRotator {
                            artifactDaysToKeep(30)
                            daysToKeep(30)
                            artifactNumToKeep(200)
                            numToKeep(200)
                        }
                        wrappers {
                            timeout {
                                absolute(240)
                            }
                        }
                    }

                    if (isPR) {
                        TriggerBuilder builder = TriggerBuilder.triggerOnPullRequest()
                        builder.setGithubContext("${os} ${arch} ${opt_level} ${jit} IlLink Tests")
                        builder.triggerOnlyOnComment()
                        builder.setCustomTriggerPhrase("(?i).*test\\W+${os}\\W+${arch}\\W+illink.*")
                        builder.triggerForBranch(branch)
                        builder.emitTrigger(newJob)
                    }
                    else {
                        // Set a push trigger
                        TriggerBuilder builder = TriggerBuilder.triggerOnCommit()
                        builder.emitTrigger(newJob)
                    }
                }
            }
        }
    }
}

Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Perf help",
    "Have a nice day!")
