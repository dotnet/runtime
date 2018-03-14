@Library('dotnet-ci') _

// Incoming parameters.  Access with "params.<param name>".
// Note that the parameters will be set as env variables so we cannot use names that conflict
// with the engineering system parameter names.

//--------------------- Windows Functions ----------------------------//

def windowsBuild(String arch, String config, String pgo, boolean isBaseline) {
    checkout scm

    String pgoBuildFlag = ((pgo == 'nopgo') ? '-nopgooptimize' : '-enforcepgo')
    String baselineString = ""

    // For baseline builds, checkout the merge's parent
    if (isBaseline) {
        baselineString = "-baseline"
        bat "git checkout HEAD^^1"
    }

    bat "set __TestIntermediateDir=int&&.\\build.cmd -${config} -${arch} -skipbuildpackages ${pgoBuildFlag}"
    bat "tests\\runtest.cmd ${config} ${arch} GenerateLayoutOnly"
    bat "rd /s /q bin\\obj"

    // Stash build artifacts. Stash tests in an additional stash to be used by Linux test runs
    stash name: "nt-${arch}-${pgo}${baselineString}-build-artifacts", includes: 'bin/**'
    stash name: "nt-${arch}-${pgo}${baselineString}-test-artifacts", includes: 'bin/tests/**'
}

def windowsPerf(String arch, String config, String uploadString, String runType, String opt_level, String jit, String pgo, String scenario, boolean isBaseline, boolean isProfileOn, int slice) {
    withCredentials([string(credentialsId: 'CoreCLR Perf BenchView Sas', variable: 'BV_UPLOAD_SAS_TOKEN')]) {
        checkout scm
        String baselineString = ""
        if (isBaseline) {
            baselineString = "-baseline"
        }
        dir ('.') {
            unstash "nt-${arch}-${pgo}${baselineString}-test-artifacts"
            unstash "benchview-tools"
            unstash "metadata"
        }

        String pgoTestFlag = ((pgo == 'nopgo') ? '-nopgo' : '')

        // We want to use the baseline metadata for baseline runs. We expect to find the submission metadata in
        // submission-metadata.py
        if (isBaseline) {
            bat "move /y submission-metadata-baseline.json submission-metadata.json"
        }

        String testEnv = ""

        String failedOutputLogFilename = "run-xunit-perf-scenario.log"

        bat "py \".\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\""
        bat ".\\init-tools.cmd"

        // We run run-xunit-perf differently for each of the different job types

        String profileArg = isProfileOn ? "BranchMispredictions+CacheMisses+InstructionRetired" : "stopwatch"

        String runXUnitCommonArgs = "-arch ${arch} -configuration ${config} -generateBenchviewData \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" ${uploadString} ${pgoTestFlag} -runtype ${runType} ${testEnv} -optLevel ${opt_level} -jitName ${jit} -outputdir \"%WORKSPACE%\\bin\\sandbox_logs\""
        if (scenario == 'perf') {
            String runXUnitPerfCommonArgs = "${runXUnitCommonArgs} -stabilityPrefix \"START \\\"CORECLR_PERF_RUN\\\" /B /WAIT /HIGH /AFFINITY 0x2\""
            if (slice == -1)
            {
                String runXUnitPerflabArgs = "${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${arch}.${config}\\performance\\perflab\\Perflab -library"

                profileArg = isProfileOn ? "default+${profileArg}+gcapi" : profileArg
                bat "py tests\\scripts\\run-xunit-perf.py ${runXUnitPerflabArgs} -collectionFlags ${profileArg}"

                String runXUnitCodeQualityArgs = "${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${arch}.${config}\\Jit\\Performance\\CodeQuality\\"
                bat "py tests\\scripts\\run-xunit-perf.py ${runXUnitCodeQualityArgs} -collectionFlags ${profileArg}"
            }

            else {
                String runXUnitCodeQualityArgs = "${runXUnitPerfCommonArgs} -slice ${slice} -sliceConfigFile \"%WORKSPACE%\\tests\\scripts\\perf-slices.json\" -testBinLoc bin\\tests\\${os}.${arch}.${config}"
                bat "py tests\\scripts\\run-xunit-perf.py ${runXUnitCodeQualityArgs} -collectionFlags ${profileArg}"
            }
        }
        else if (scenario == 'jitbench') {
            String runXUnitPerfCommonArgs = "${runXUnitCommonArgs} -stabilityPrefix \"START \"CORECLR_PERF_RUN\" /B /WAIT /HIGH\" -scenarioTest"
            runXUnitPerfCommonArgs = "${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${arch}.${config}\\performance\\Scenario\\JitBench -group CoreCLR-Scenarios"

            if (!(opt_level == 'min_opt' && isProfileOn)) {
                bat "py tests\\scripts\\run-xunit-perf.py ${runXUnitPerfCommonArgs} -collectionFlags ${profileArgs}"
            }
        }
        else if (scenario == 'illink') {
            String runXUnitPerfCommonArgs = "${runXUnitCommonArgs} -scenarioTest"
            bat "\"%VS140COMNTOOLS%\\..\\..\\VC\\vcvarsall.bat\" x86_amd64\n" +
                "py tests\\scripts\\run-xunit-perf.py ${runXUnitPerfCommonArgs} -testBinLoc bin\\tests\\${os}.${arch}.${config}\\performance\\linkbench\\linkbench -group ILLink -nowarmup"
        }
        archiveArtifacts allowEmptyArchive: false, artifacts:'bin/sandbox_logs/**,machinedata.json', onlyIfSuccessful: false
    }
}

def windowsThroughput(String arch, String os, String config, String runType, String optLevel, String jit, String pgo, boolean isBaseline) {
    withCredentials([string(credentialsId: 'CoreCLR Perf BenchView Sas', variable: 'BV_UPLOAD_SAS_TOKEN')]) {
        checkout scm

        String baselineString = ""
        if (isBaseline) {
            baselineString = "-baseline"
        }

        String pgoTestFlag = ((pgo == 'nopgo') ? '-nopgo' : '')

        dir ('.') {
            unstash "nt-${arch}-${pgo}${baselineString}-build-artifacts"
            unstash "benchview-tools"
            unstash "throughput-benchmarks-${arch}"
            unstash "metadata"
        }

        // We want to use the baseline metadata for baseline runs. We expect to find the submission metadata in
        // submission-metadata.py
        if (isBaseline) {
            bat "move /y submission-metadata-baseline.json submission-metadata.json"
        }

        bat "py \".\\Microsoft.BenchView.JSONFormat\\tools\\machinedata.py\""
        bat ".\\init-tools.cmd"
        bat "py -u tests\\scripts\\run-throughput-perf.py -arch ${arch} -os ${os} -configuration ${config} -opt_level ${optLevel} -jit_name ${jit} ${pgoTestFlag} -clr_root \"%WORKSPACE%\" -assembly_root \"%WORKSPACE%\\${arch}ThroughputBenchmarks\\lib\" -benchview_path \"%WORKSPACE%\\Microsoft.Benchview.JSONFormat\\tools\" -run_type ${runType}"
        archiveArtifacts allowEmptyArchive: false, artifacts:'throughput-*.csv,machinedata.json', onlyIfSuccessful: false
    }
}

//------------------------ Linux Functions ----------------------------//

def linuxBuild(String arch, String config, String pgo, boolean isBaseline) {
    checkout scm

    String pgoBuildFlag = ((pgo == 'nopgo') ? '-nopgooptimize' : '')
    String baselineString = ""

    // For baseline runs, checkout the merge's parent
    if (isBaseline) {
        baselineString = "-baseline"
        sh "git checkout HEAD^1"
    }

    sh "./build.sh -verbose -${config} -${arch} ${pgoBuildFlag}"
    stash name: "linux-${arch}-${pgo}${baselineString}-build-artifacts", includes: 'bin/**'
}

def linuxPerf(String arch, String os, String config, String uploadString, String runType, String optLevel, String pgo, boolean isBaseline) {
    withCredentials([string(credentialsId: 'CoreCLR Perf BenchView Sas', variable: 'BV_UPLOAD_SAS_TOKEN')]) {
        checkout scm

        String baselineString = ""
        if (isBaseline) {
            baselineString = "-baseline"
        }

        String pgoTestFlag = ((pgo == 'nopgo') ? '-nopgo' : '')

        dir ('.') {
            unstash "linux-${arch}-${pgo}${baselineString}-build-artifacts"
            unstash "nt-${arch}-${pgo}${baselineString}-test-artifacts"
            unstash "metadata"
        }
        dir ('./tests/scripts') {
            unstash "benchview-tools"
        }

        // We want to use the baseline metadata for baseline runs. We expect to find the submission metadata in
        // submission-metadata.py
        if (isBaseline) {
            sh "mv -f submission-metadata-baseline.json submission-metadata.json"
        }

        sh "./tests/scripts/perf-prep.sh --nocorefx"
        sh "./init-tools.sh"
        sh "./build-test.sh release $arch generatelayoutonly"

        String runXUnitCommonArgs = "-arch ${arch} -os Ubuntu16.04 -configuration ${config} -stabilityPrefix \"taskset 0x00000002 nice --adjustment=-10\" -generateBenchviewData \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools\" ${uploadString} ${pgoTestFlag} -runtype ${runType} -optLevel ${optLevel} -outputdir \"\${WORKSPACE}/bin/sandbox_logs\""

        sh "python3 ./tests/scripts/run-xunit-perf.py -testBinLoc bin/tests/Windows_NT.${arch}.${config}/JIT/Performance/CodeQuality ${runXUnitCommonArgs}"
        archiveArtifacts allowEmptyArchive: false, artifacts:'bin/toArchive/**,machinedata.json', onlyIfSuccessful: false
    }
}

def linuxThroughput(String arch, String os, String config, String uploadString, String runType, String optLevel, String pgo, boolean isBaseline) {
    withCredentials([string(credentialsId: 'CoreCLR Perf BenchView Sas', variable: 'BV_UPLOAD_SAS_TOKEN')]) {
        checkout scm

        String baselineString = ""
        if (isBaseline) {
            baselineString = "-baseline"
        }

        String pgoTestFlag = ((pgo == 'nopgo') ? '-nopgo' : '')

        dir ('.') {
            unstash "linux-${arch}-${pgo}${baselineString}-build-artifacts"
            unstash "throughput-benchmarks-${arch}"
            unstash "metadata"
        }
        dir ('./tests/scripts') {
            unstash "benchview-tools"
        }

        // We want to use the baseline metadata for baseline runs. We expect to find the submission metadata in
        // submission-metadata.py
        if (isBaseline) {
            sh "mv -f submission-metadata-baseline.json submission-metadata.json"
        }

        sh "./tests/scripts/perf-prep.sh --throughput"
        sh "./init-tools.sh"
        sh "python3 ./tests/scripts/run-throughput-perf.py -arch \"${arch}\" -os \"${os}\" -configuration \"${config}\" -opt_level ${optLevel} ${pgoTestFlag} -clr_root \"\${WORKSPACE}\" -assembly_root \"\${WORKSPACE}/${arch}ThroughputBenchmarks/lib\" -run_type \"${runType}\"  -benchview_path \"\${WORKSPACE}/tests/scripts/Microsoft.BenchView.JSONFormat/tools\""
        archiveArtifacts allowEmptyArchive: false, artifacts:'throughput-*.csv,machinedata.json', onlyIfSuccessful: false
    }
}

//-------------------------- Job Definitions --------------------------//

String config = "Release"
String runType = isPR() ? 'private' : 'rolling'

String uploadString = '-uploadToBenchview'

stage ('Get Metadata and download Throughput Benchmarks') {
    simpleNode('Windows_NT', '20170427-elevated') {
        checkout scm
        String commit = getCommit()
        def benchViewName = isPR() ? "coreclr private %ghprbPullTitle%" : "coreclr rolling %GIT_BRANCH_WITHOUT_ORIGIN% ${commit}"
        def benchViewUser = getUserEmail()
        bat "mkdir tools\n" +
            "powershell Invoke-WebRequest https://dist.nuget.org/win-x86-commandline/v4.1.0/nuget.exe -OutFile %WORKSPACE%\\tools\\nuget.exe"
        bat "%WORKSPACE%\\tools\\nuget.exe install Microsoft.BenchView.JSONFormat -Source http://benchviewtestfeed.azurewebsites.net/nuget -Prerelease -ExcludeVersion"
        bat "%WORKSPACE%\\tools\\nuget.exe install Microsoft.BenchView.ThroughputBenchmarks.x64.Windows_NT -Source https://dotnet.myget.org/F/dotnet-core -Prerelease -ExcludeVersion"
        bat "%WORKSPACE%\\tools\\nuget.exe install Microsoft.BenchView.ThroughputBenchmarks.x86.Windows_NT -Source https://dotnet.myget.org/F/dotnet-core -Prerelease -ExcludeVersion"
        bat "set \"GIT_BRANCH_WITHOUT_ORIGIN=%GitBranchOrCommit:*/=%\"\n" +
            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"${benchViewName}\" --user-email \"${benchViewUser}\"\n" +
            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\build.py\" git --branch %GIT_BRANCH_WITHOUT_ORIGIN% --type ${runType}\n" +
            "py \"%WORKSPACE%\\Microsoft.BenchView.JSONFormat\\tools\\submission-metadata.py\" --name \"${benchViewName}-baseline\" --user-email \"${benchViewUser}\" -o submission-metadata-baseline.json\n"

        // TODO: revisit these moves. Originally, stash could not find the directories as currently named
        bat "move Microsoft.BenchView.ThroughputBenchmarks.x64.Windows_NT x64ThroughputBenchmarks"
        bat "move Microsoft.BenchView.ThroughputBenchmarks.x86.Windows_NT x86ThroughputBenchmarks"

        stash includes: 'Microsoft.BenchView.JSONFormat/**/*', name: 'benchview-tools'
        stash name: "metadata", includes: "*.json"
        stash name: "throughput-benchmarks-x64", includes: "x64ThroughputBenchmarks/**/*"
        stash name: "throughput-benchmarks-x86", includes: "x86ThroughputBenchmarks/**/*"
    }
}

// TODO: use non-pgo builds for throughput?
def innerLoopBuilds = [
    "windows x64 pgo build": {
        simpleNode('Windows_NT','latest') {
            windowsBuild('x64', config, 'pgo', false)
        }
     },
    "windows x86 pgo build": {
        simpleNode('Windows_NT','latest') {
            windowsBuild('x86', config, 'pgo', false)
        }
    },
    "linux x64 pgo build": {
        simpleNode('RHEL7.2', 'latest-or-auto') {
            linuxBuild('x64', config, 'pgo', false)
        }
    }
]

// Only run non-pgo builds on offical builds
def outerLoopBuilds = [:]

if (!isPR()) {
    outerLoopBuilds = [
        "windows x64 nopgo build": {
            simpleNode('Windows_NT','latest') {
                windowsBuild('x64', config, 'nopgo', false)
            }
        },
        "windows x86 nopgo build": {
           simpleNode('Windows_NT','latest') {
               windowsBuild('x86', config, 'nopgo', false)
           }
        },
        "linux x64 nopgo build": {
           simpleNode('RHEL7.2', 'latest-or-auto') {
               linuxBuild('x64', config, 'nopgo', false)
           }
        }
    ]
}

/*def baselineBuilds = [:]

if (isPR()) {
   baselineBuilds = [
       "windows x64 pgo baseline build": {
           simpleNode('Windows_NT','latest') {
               windowsBuild('x64', config, 'pgo', true)
           }
       },
       "windows x86 pgo baseline build": {
           simpleNode('Windows_NT','latest') {
               windowsBuild('x86', config, 'pgo', true)
           }
       }
   ]
}*/

stage ('Build Product') {
    parallel innerLoopBuilds //+ outerLoopBuilds //+ baselineBuilds
}

// Pipeline builds don't allow outside scripts (ie ArrayList.Add) if running from a script from SCM, so manually list these for now.
// Run the main test mix on all runs (PR + official)

def innerLoopTests = [:]

['x64', 'x86'].each { arch ->
    ['full_opt'].each { opt_level ->
        [false].each { isBaseline ->
            [0,1,2,3,4,5].each { slice ->
                String baseline = ""
                if (isBaseline) {
                    baseline = " baseline"
                }
                if (isPR() || !isBaseline) {
                    innerLoopTests["windows ${arch} ryujit ${opt_level} pgo ${slice}${baseline} perf"] = {
                        simpleNode('windows_server_2016_clr_perf', 180) {
                            windowsPerf(arch, config, uploadString, runType, opt_level, 'ryujit', 'pgo', 'perf', isBaseline, true, slice)
                        }
                    }

                }
            }

            if (arch == 'x64') {
                innerLoopTests["linux ${arch} ryujit ${opt_level} pgo perf"] = {
                    simpleNode('ubuntu_1604_clr_perf', 180) {
                        linuxPerf(arch, 'Ubuntu16.04', config, uploadString, runType, opt_level, 'pgo', false)
                    }
                }
            }
        }
    }
}

// Run the full test mix only on commits, not PRs
def outerLoopTests = [:]

if (!isPR()) {
    ['x64', 'x86'].each { arch ->
        outerLoopTests["windows ${arch} ryujit full_opt pgo${baseline} jitbench"] = {
            simpleNode('windows_server_2016_clr_perf', 180) {
                windowsPerf(arch, config, uploadString, runType, 'full_opt', 'ryujit', 'pgo', 'jitbench', false, false, -1)
            }
        }

        outerLoopTests["windows ${arch} ryujit full_opt pgo illink"] = {
            simpleNode('Windows_NT', '20170427-elevated') {
                windowsPerf(arch, config, uploadString, runType, 'full_opt', 'ryujit', 'pgo', 'illink', false, false, -1)
            }
        }
    }

    ['x64', 'x86'].each { arch ->
        ['min_opt', 'full_opt'].each { opt_level ->
            ['ryujit'].each { jit ->
                ['pgo', 'nopgo'].each { pgo_enabled ->
                    [true, false].each { isProfileOn ->
                        outerLoopTests["windows ${arch} ${jit} ${opt_level} ${pgo_enabled} perf"] = {
                            simpleNode('windows_server_2016_clr_perf', 180) {
                                windowsPerf(arch, config, uploadString, runType, opt_level, jit, pgo_enabled, 'perf', false, isProfileOn, -1)
                            }
                        }

                        outerLoopTests["windows ${arch} ${jit} ${opt_level} ${pgo_enabled} throughput"] = {
                            simpleNode('windows_server_2016_clr_perf', 180) {
                                windowsThroughput(arch, 'Windows_NT', config, runType, opt_level, jit, pgo_enabled, false, isProfileOn)
                            }
                        }
                    }
                }
            }
        }
    }

    ['x64'].each { arch ->
        ['min_opt', 'full_opt'].each { opt_level ->
            ['pgo', 'nopgo'].each { pgo_enabled ->
                outerLoopTests["linux ${arch} ryujit ${opt_level} ${pgo_enabled} perf"] = {
                    simpleNode('ubuntu_1604_clr_perf', 180) {
                        linuxPerf(arch, 'Ubuntu16.04', config, uploadString, runType, opt_level, pgo_enabled, false)
                    }
                }

                outerLoopTests["linux ${arch} ryujit ${opt_level} ${pgo_enabled} throughput"] = {
                    simpleNode('ubuntu_1604_clr_perf', 180) {
                        linuxThroughput(arch, 'Ubuntu16.04', config, uploadString, runType, opt_level, pgo_enabled, false)
                    }
                }
            }
        }
    }
}

stage ('Run testing') {
    parallel innerLoopTests //+ outerLoopTests
}
