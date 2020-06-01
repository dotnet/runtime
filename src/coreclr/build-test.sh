#!/usr/bin/env bash

build_test_wrappers()
{
    if [[ "$__BuildTestWrappers" -ne -0 ]]; then
        echo "${__MsgPrefix}Creating test wrappers..."

        if [[ $__Mono -eq 1 ]]; then
            __RuntimeFlavor="mono"
        else
            __RuntimeFlavor="coreclr"
        fi

        __Exclude="${__ProjectDir}/tests/issues.targets"
        __BuildLogRootName="Tests_XunitWrapper"

        export __Exclude __BuildLogRootName

        buildVerbosity="Summary"

        if [[ "$__VerboseBuild" == 1 ]]; then
            buildVerbosity="Diag"
        fi

        # Set up directories and file names
        __BuildLogRootName="$subDirectoryName"
        __BuildLog="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.log"
        __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.wrn"
        __BuildErr="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.err"
        __MsbuildLog="/fileloggerparameters:\"Verbosity=normal;LogFile=${__BuildLog}\""
        __MsbuildWrn="/fileloggerparameters1:\"WarningsOnly;LogFile=${__BuildWrn}\""
        __MsbuildErr="/fileloggerparameters2:\"ErrorsOnly;LogFile=${__BuildErr}\""
        __Logging="$__MsbuildLog $__MsbuildWrn $__MsbuildErr /consoleloggerparameters:$buildVerbosity"

        nextCommand="\"${__DotNetCli}\" msbuild \"${__ProjectDir}/tests/src/runtest.proj\" /nodereuse:false /p:BuildWrappers=true /p:TestBuildMode=$__TestBuildMode /p:TargetsWindows=false $__Logging /p:TargetOS=$__TargetOS /p:Configuration=$__BuildType /p:TargetArchitecture=$__BuildArch /p:RuntimeFlavor=$__RuntimeFlavor \"/bl:${__RepoRootDir}/artifacts/log/${__BuildType}/build_test_wrappers_${__RuntimeFlavor}.binlog\""
        eval $nextCommand

        local exitCode="$?"
        if [[ "$exitCode" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: XUnit wrapper build failed. Refer to the build log files for details (above)"
            exit "$exitCode"
        else
            echo "XUnit Wrappers have been built."
            echo { "\"build_os\"": "\"${__TargetOS}\"", "\"build_arch\"": "\"${__BuildArch}\"", "\"build_type\"": "\"${__BuildType}\"" } > "${__TestWorkingDir}/build_info.json"

        fi
    fi
}

generate_layout()
{
    echo "${__MsgPrefix}Creating test overlay..."

    __TestDir="$__ProjectDir"/tests
    __ProjectFilesDir="$__TestDir"
    __TestBinDir="$__TestWorkingDir"

    if [[ "$__RebuildTests" -ne 0 ]]; then
        if [[ -d "${__TestBinDir}" ]]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf "$__TestBinDir"
        fi
    fi

    __CMakeBinDir="${__TestBinDir}"

    if [[ -z "$__TestIntermediateDir" ]]; then
        __TestIntermediateDir="tests/obj/${__TargetOS}.${__BuildArch}.${__BuildType}"
    fi

    echo "__TargetOS: ${__TargetOS}"
    echo "__BuildArch: ${__BuildArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediateDir: ${__TestIntermediateDir}"

    if [[ ! -f "$__TestBinDir" ]]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p "$__TestBinDir"
    fi
    if [[ ! -f "$__LogsDir" ]]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p "$__LogsDir"
    fi
    if [[ ! -f "$__MsbuildDebugLogsDir" ]]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p "$__MsbuildDebugLogsDir"
    fi

    # Set up the directory for MSBuild debug logs.
    MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"
    export MSBUILDDEBUGPATH

    __BuildProperties="-p:TargetOS=${__TargetOS} -p:TargetArchitecture=${__BuildArch} -p:Configuration=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    build_MSBuild_projects "Restore_Packages" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

    if [[ -n "$__UpdateInvalidPackagesArg" ]]; then
        __up="/t:UpdateInvalidPackageVersions"
    fi

    echo "${__MsgPrefix}Creating test overlay..."

    if [[ -z "$xUnitTestBinBase" ]]; then
        xUnitTestBinBase="$__TestWorkingDir"
    fi

    CORE_ROOT="$xUnitTestBinBase"/Tests/Core_Root
    export CORE_ROOT

    if [[ -d "${CORE_ROOT}" ]]; then
        rm -rf "$CORE_ROOT"
    fi

    mkdir -p "$CORE_ROOT"

    chmod +x "$__BinDir"/corerun
    chmod +x "$__CrossgenExe"

    build_MSBuild_projects "Tests_Overlay_Managed" "${__ProjectDir}/tests/src/runtest.proj" "Creating test overlay" "/t:CreateTestOverlay"

    if [[ "$__TargetOS" != "OSX" ]]; then
        nextCommand="\"$__TestDir/setup-stress-dependencies.sh\" --arch=$__BuildArch --outputDir=$CORE_ROOT"
        echo "Resolve runtime dependences via $nextCommand"
        eval $nextCommand

        local exitCode="$?"
        if [[ "$exitCode" != 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: setup-stress-dependencies failed."
            exit "$exitCode"
        fi
    fi

    # Precompile framework assemblies with crossgen if required
    if [[ "$__DoCrossgen" != 0 || "$__DoCrossgen2" != 0 ]]; then
        if [[ "$__SkipCrossgenFramework" == 0 ]]; then
            precompile_coreroot_fx
        fi
    fi
}

precompile_coreroot_fx()
{
    local overlayDir="$CORE_ROOT"
    local compilerName=Crossgen

    # Read the exclusion file for this platform
    skipCrossGenFiles=($(grep -v '^#' "$(dirname "$0")/tests/skipCrossGenFiles.${__BuildArch}.txt" 2> /dev/null))
    skipCrossGenFiles+=('System.Runtime.WindowsRuntime.dll')

    # Temporary output folder for Crossgen2-compiled assemblies
    local outputDir="$overlayDir"/out

    # Delete previously crossgened assemblies
    rm "$overlayDir"/*.ni.dll

    # Collect reference assemblies for Crossgen2
    local crossgen2References=""

    if [[ "$__DoCrossgen2" != 0 ]]; then
        compilerName=Crossgen2

        mkdir "$outputDir"

        skipCrossGenFiles+=('Microsoft.CodeAnalysis.CSharp.dll')
        skipCrossGenFiles+=('Microsoft.CodeAnalysis.dll')
        skipCrossGenFiles+=('Microsoft.CodeAnalysis.VisualBasic.dll')

        for reference in "$overlayDir"/*.dll; do
            crossgen2References+=" -r:${reference}"
        done
    fi

    echo "${__MsgPrefix}Running ${compilerName} on framework assemblies in CORE_ROOT: '${CORE_ROOT}'"

    local totalPrecompiled=0
    local failedToPrecompile=0
    local compositeCommandLine="${__DotNetCli}"
    compositeCommandLine+=" $__BinDir/crossgen2/crossgen2.dll"
    compositeCommandLine+=" --composite"
    compositeCommandLine+=" -O"
    compositeCommandLine+=" --out:$outputDir/framework-r2r.dll"
    declare -a failedAssemblies

    filesToPrecompile=$(find -L "$overlayDir" -maxdepth 1 -iname Microsoft.\*.dll -o -iname System.\*.dll -o -iname netstandard.dll -o -iname mscorlib.dll -type f)
    for fileToPrecompile in ${filesToPrecompile}; do
        local filename="$fileToPrecompile"
        if is_skip_crossgen_test "$(basename $filename)"; then
            continue
        fi

        if [[ "$__CompositeBuildMode" != 0 ]]; then
            compositeCommandLine+=" $filename"
            continue
        fi

        local commandLine=""

        if [[ "$__DoCrossgen" != 0 ]]; then
            commandLine="$__CrossgenExe /Platform_Assemblies_Paths $overlayDir $filename"
        fi

        if [[ "$__DoCrossgen2" != 0 ]]; then
            commandLine="${__DotNetCli} $overlayDir/crossgen2/crossgen2.dll $crossgen2References -O --inputbubble --out $outputDir/$(basename $filename) $filename"
        fi

        echo Precompiling "$filename"
        $commandLine 1> "$filename".stdout 2> "$filename".stderr
        local exitCode="$?"
        if [[ "$exitCode" != 0 ]]; then
            if grep -q -e '0x80131018' "$filename".stderr; then
                printf "\n\t$filename is not a managed assembly.\n\n"
            else
                echo Unable to precompile "$filename", exit code is "$exitCode".
                echo Command-line: "$commandLine"
                cat "$filename".stdout
                cat "$filename".stderr
                failedAssemblies+=($(basename -- "$filename"))
                failedToPrecompile=$((failedToPrecompile+1))
            fi
        else
            rm "$filename".{stdout,stderr}
        fi

        totalPrecompiled=$((totalPrecompiled+1))
        echo "Processed: $totalPrecompiled, failed $failedToPrecompile"
    done

    if [[ "$__CompositeBuildMode" != 0 ]]; then
        # Compile the entire framework in composite build mode
        echo "Compiling composite R2R framework: $compositeCommandLine"
        $compositeCommandLine
        local exitCode="$?"
        if [[ "$exitCode" != 0 ]]; then
            echo Unable to precompile composite framework, exit code is "$exitCode".
            exit 1
        fi
    fi

    if [[ "$__DoCrossgen2" != 0 ]]; then
        # Copy the Crossgen-compiled assemblies back to CORE_ROOT
        mv -f "$outputDir"/* "$overlayDir"/
        rm -r "$outputDir"
    fi

    if [[ "$failedToPrecompile" != 0 ]]; then
        echo Failed assemblies:
        for assembly in "${failedAssemblies[@]}"; do
            echo "  $assembly"
        done

        exit 1
    fi
}

declare -a skipCrossGenFiles

function is_skip_crossgen_test {
    for skip in "${skipCrossGenFiles[@]}"; do
        if [[ "$1" == "$skip" ]]; then
            return 0
        fi
    done
    return 1
}

build_Tests()
{
    echo "${__MsgPrefix}Building Tests..."

    __TestDir="$__ProjectDir"/tests
    __ProjectFilesDir="$__TestDir"
    __TestBinDir="$__TestWorkingDir"

    if [[ -f  "${__TestWorkingDir}/build_info.json" ]]; then
        rm  "${__TestWorkingDir}/build_info.json"
    fi

    if [[ "$__RebuildTests" -ne 0 ]]; then
        if [[ -d "$__TestBinDir" ]]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf "$__TestBinDir"
        fi
    fi

    __CMakeBinDir="$__TestBinDir"
    export __CMakeBinDir

    if [[ ! -d "$__TestIntermediatesDir" ]]; then
        mkdir -p "$__TestIntermediatesDir"
    fi

    __NativeTestIntermediatesDir="${__TestIntermediatesDir}/Native"
    if [[  ! -d "${__NativeTestIntermediatesDir}" ]]; then
        mkdir -p "${__NativeTestIntermediatesDir}"
    fi

    __ManagedTestIntermediatesDir="${__TestIntermediatesDir}/Managed"
    if [[ ! -d "${__ManagedTestIntermediatesDir}" ]]; then
        mkdir -p "${__ManagedTestIntermediatesDir}"
    fi

    echo "__TargetOS: ${__TargetOS}"
    echo "__BuildArch: ${__BuildArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediatesDir: ${__TestIntermediatesDir}"
    echo "__NativeTestIntermediatesDir: ${__NativeTestIntermediatesDir}"
    echo "__ManagedTestIntermediatesDir: ${__ManagedTestIntermediatesDir}"

    if [[ ! -f "$__TestBinDir" ]]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p "$__TestBinDir"
    fi
    if [[ ! -f "$__LogsDir" ]]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p "$__LogsDir"
    fi
    if [[ ! -f "$__MsbuildDebugLogsDir" ]]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p "$__MsbuildDebugLogsDir"
    fi

    # Set up the directory for MSBuild debug logs.
    MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"
    export MSBUILDDEBUGPATH

    __BuildProperties="-p:TargetOS=${__TargetOS} -p:TargetArchitecture=${__BuildArch} -p:Configuration=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    if [[ "${__SkipRestorePackages}" != 1 ]]; then
        build_MSBuild_projects "Restore_Product" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: package restoration failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [[ "$__SkipNative" != 1 ]]; then
        build_native "$__BuildArch" "$__TestDir" "$__ProjectRoot" "$__NativeTestIntermediatesDir" "CoreCLR test component"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: native test build failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [[ "$__SkipManaged" != 1 ]]; then
        echo "Starting the Managed Tests Build..."

        build_MSBuild_projects "Tests_Managed" "$__ProjectDir/tests/build.proj" "Managed tests build (build tests)" "$__up"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: managed test build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "Checking the Managed Tests Build..."

            build_MSBuild_projects "Check_Test_Build" "${__ProjectDir}/tests/src/runtest.proj" "Check Test Build" "/t:CheckTestBuild"

            if [[ "$?" -ne 0 ]]; then
                echo "${__ErrMsgPrefix}${__MsgPrefix}Error: Check Test Build failed."
                exit 1
            fi
        fi

        echo "Managed tests build success!"

        build_test_wrappers
    fi

    if [[ "$__CopyNativeTestBinaries" == 1 ]]; then
        echo "Copying native test binaries to output..."

        build_MSBuild_projects "Tests_Managed" "$__ProjectDir/tests/build.proj" "Managed tests build (build tests)" "/t:CopyAllNativeProjectReferenceBinaries"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: copying native test binaries failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [[ -n "$__UpdateInvalidPackagesArg" ]]; then
        __up="/t:UpdateInvalidPackageVersions"
    fi

    if [[ "$__SkipGenerateLayout" != 1 ]]; then
        generate_layout
    fi
}

build_MSBuild_projects()
{
    subDirectoryName="$1"
    shift
    projectName="$1"
    shift
    stepName="$1"
    shift
    extraBuildParameters=("$@")

    # Set up directories and file names
    __BuildLogRootName="$subDirectoryName"
    __BuildLog="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.log"
    __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.wrn"
    __BuildErr="$__LogsDir/${__BuildLogRootName}.${__TargetOS}.${__BuildArch}.${__BuildType}.err"

    if [[ "$subDirectoryName" == "Tests_Managed" ]]; then
        # Execute msbuild managed test build in stages - workaround for excessive data retention in MSBuild ConfigCache
        # See https://github.com/Microsoft/msbuild/issues/2993

        # __SkipPackageRestore and __SkipTargetingPackBuild used  to control build by tests/src/dirs.proj
        __SkipPackageRestore=false
        __SkipTargetingPackBuild=false
        __NumberOfTestGroups=3

        __AppendToLog=false

        if [[ -n "$__priority1" ]]; then
            __NumberOfTestGroups=10
        fi

        export __SkipPackageRestore __SkipTargetingPackBuild __NumberOfTestGroups

        for (( testGroupToBuild=1 ; testGroupToBuild <= __NumberOfTestGroups; testGroupToBuild = testGroupToBuild + 1 ))
        do
            __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog};Append=${__AppendToLog}\""
            __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn};Append=${__AppendToLog}\""
            __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr};Append=${__AppendToLog}\""

            __TestGroupToBuild="$testGroupToBuild"
            export __TestGroupToBuild

            # Generate build command
            buildArgs=("$projectName")
            buildArgs+=("/p:RestoreDefaultOptimizationDataPackage=false" "/p:PortableBuild=true")
            buildArgs+=("/p:UsePartialNGENOptimization=false" "/maxcpucount")

            buildArgs+=("${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}")
            buildArgs+=("${extraBuildParameters[@]}")
            buildArgs+=("${__CommonMSBuildArgs}")
            buildArgs+=("${__UnprocessedBuildArgs[@]}")
            buildArgs+=("\"/p:CopyNativeProjectBinaries=${__CopyNativeProjectsAfterCombinedTestBuild}\"");
            buildArgs+=("/p:__SkipPackageRestore=true");

            # Disable warnAsError - coreclr issue 19922
            nextCommand="\"$__RepoRootDir/eng/common/msbuild.sh\" $__ArcadeScriptArgs --warnAsError false ${buildArgs[@]}"
            echo "Building step '$stepName' testGroupToBuild=$testGroupToBuild via $nextCommand"
            eval $nextCommand

            # Make sure everything is OK
            if [[ "$?" -ne 0 ]]; then
                echo "${__ErrMsgPrefix}${__MsgPrefix}Failed to build $stepName. See the build logs:"
                echo "    $__BuildLog"
                echo "    $__BuildWrn"
                echo "    $__BuildErr"
                exit 1
            fi

            __SkipPackageRestore=true
            __SkipTargetingPackBuild=true
            export __SkipPackageRestore __SkipTargetingPackBuild

            __AppendToLog=true
        done
    else
        __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog}\""
        __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn}\""
        __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr}\""

        # Generate build command
        buildArgs=("$projectName")
        buildArgs+=("/p:RestoreDefaultOptimizationDataPackage=false" "/p:PortableBuild=true")
        buildArgs+=("/p:UsePartialNGENOptimization=false" "/maxcpucount")

        buildArgs+=("${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}")
        buildArgs+=("${extraBuildParameters[@]}")
        buildArgs+=("${__CommonMSBuildArgs}")
        buildArgs+=("${__UnprocessedBuildArgs[@]}")

        # Disable warnAsError - coreclr issue 19922
        nextCommand="\"$__RepoRootDir/eng/common/msbuild.sh\" $__ArcadeScriptArgs --warnAsError false ${buildArgs[@]}"
        echo "Building step '$stepName' via $nextCommand"
        eval $nextCommand

        # Make sure everything is OK
        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Failed to build $stepName. See the build logs:"
            echo "    $__BuildLog"
            echo "    $__BuildWrn"
            echo "    $__BuildErr"
            exit 1
        fi
    fi
}

usage_list=("-buildtestwrappersonly: only build the test wrappers.")
usage_list+=("-skiptestwrappers: Don't generate test wrappers.")
usage_list+=("-copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.")
usage_list+=("-crossgen: Precompiles the framework managed assemblies in coreroot.")
usage_list+=("-crossgen2: Precompiles the framework managed assemblies in coreroot using the Crossgen2 compiler.")
usage_list+=("-generatetesthostonly: only generate the test host.")
usage_list+=("-generatelayoutonly: only pull down dependencies and build coreroot.")
usage_list+=("-priority1: include priority=1 tests in the build.")
usage_list+=("-targetGeneric: Only build tests which run on any target platform.")
usage_list+=("-targetSpecific: Only build tests which run on a specific target platform.")

usage_list+=("-rebuild: if tests have already been built - rebuild them.")
usage_list+=("-runtests: run tests after building them.")
usage_list+=("-skiprestorepackages: skip package restore.")
usage_list+=("-skipgeneratelayout: Do not generate the Core_Root layout.")
usage_list+=("-excludemonofailures: Mark the build as running on Mono runtime so that mono-specific issues are honored.")

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__ProjectRoot"/../..; pwd -P)"
__BuildArch=

handle_arguments_local() {
    case "$1" in
        buildtestwrappersonly|-buildtestwrappersonly)
            __BuildTestWrappersOnly=1
            ;;

        skiptestwrappers|-skiptestwrappers)
            __BuildTestWrappers=0
            ;;

        copynativeonly|-copynativeonly)
            __SkipNative=1
            __SkipManaged=1
            __CopyNativeTestBinaries=1
            __CopyNativeProjectsAfterCombinedTestBuild=true
            __SkipCrossgenFramework=1
            ;;

        crossgen|-crossgen)
            __DoCrossgen=1
            __TestBuildMode=crossgen
            ;;

        crossgen2|-crossgen2)
            __DoCrossgen2=1
            __TestBuildMode=crossgen2
            ;;

        composite|-composite)
            __CompositeBuildMode=1
            __DoCrossgen2=1
            __TestBuildMode=crossgen2
            ;;

        generatetesthostonly|-generatetesthostonly)
            __GenerateTestHostOnly=1
            ;;

        generatelayoutonly|-generatelayoutonly)
            __GenerateLayoutOnly=1
            ;;

        priority1|-priority1)
            __priority1=1
            __UnprocessedBuildArgs+=("/p:CLRTestPriorityToBuild=1")
            ;;

        targetGeneric|-targetGeneric)
            __UnprocessedBuildArgs+=("/p:CLRTestNeedTargetToBuild=targetGeneric")
            ;;

        targetSpecific|-targetSpecific)
            __UnprocessedBuildArgs+=("/p:CLRTestNeedTargetToBuild=targetSpecific")
            ;;

        rebuild|-rebuild)
            __RebuildTests=1
            ;;

        runtests|-runtests)
            __RunTests=1
            ;;

        skiprestorepackages|-skiprestorepackages)
            __SkipRestorePackages=1
            ;;

        skipgeneratelayout|-skipgeneratelayout)
            __SkipGenerateLayout=1
            ;;

        excludemonofailures|-excludemonofailures)
            __Mono=1
            ;;
        *)
            __UnprocessedBuildArgs+=("$1")
            ;;
    esac
}

__BuildType=Debug
__CodeCoverage=
__IncludeTests=INCLUDE_TESTS

# Set the various build properties here so that CMake and MSBuild can pick them up
__ProjectDir="$__ProjectRoot"
export __ProjectDir

__BuildTestWrappers=1
__BuildTestWrappersOnly=
__Compiler=clang
__CompilerMajorVersion=
__CompilerMinorVersion=
__CommonMSBuildArgs=
__ConfigureOnly=0
__CopyNativeProjectsAfterCombinedTestBuild=true
__CopyNativeTestBinaries=0
__CrossBuild=0
__DistroRid=""
__DoCrossgen=0
__DoCrossgen2=0
__CompositeBuildMode=0
__DotNetCli="$__RepoRootDir/dotnet.sh"
__GenerateLayoutOnly=
__GenerateTestHostOnly=
__IsMSBuildOnNETCoreSupported=0
__MSBCleanBuildArgs=
__NativeTestIntermediatesDir=
__PortableBuild=1
__RebuildTests=0
__RootBinDir="$__RepoRootDir/artifacts"
__RunTests=0
__SkipConfigure=0
__SkipGenerateLayout=0
__SkipGenerateVersion=0
__SkipManaged=0
__SkipNative=0
__SkipRestore=""
__SkipRestorePackages=0
__SkipCrossgenFramework=0
__SourceDir="$__ProjectDir/src"
__UnprocessedBuildArgs=
__LocalCoreFXConfig=${__BuildType}
__UseNinja=0
__VerboseBuild=0
__CMakeArgs=""
__priority1=
CORE_ROOT=

source "$__ProjectRoot"/_build-commons.sh

if [[ "${__BuildArch}" != "${__HostArch}" ]]; then
    __CrossBuild=1
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/bin/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__TestDir="$__ProjectDir/tests"
__TestWorkingDir="$__RootBinDir/tests/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__TargetOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/coreclr/obj/$__TargetOS.$__BuildArch.$__BuildType"
__CrossComponentBinDir="$__BinDir"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"

__CrossArch="$__HostArch"
if [[ "$__CrossBuild" == 1 ]]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossgenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__TargetOS.$BuildArch.$__BuildType.log"
__CrossgenExe="$__CrossComponentBinDir/crossgen"

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to it.
# This is needed by CLI to function.
if [[ -z "$HOME" ]]; then
    if [[ ! -d "$__ProjectDir/temp_home" ]]; then
        mkdir temp_home
    fi

    HOME="$__ProjectDir"/temp_home
    export HOME
    echo "HOME not defined; setting it to $HOME"
fi

if [[ (-z "$__GenerateLayoutOnly") && (-z "$__GenerateTestHostOnly") && (-z "$__BuildTestWrappersOnly") ]]; then
    build_Tests
elif [[ ! -z "$__BuildTestWrappersOnly" ]]; then
    build_test_wrappers
else
    generate_layout
fi

if [[ "$?" -ne 0 ]]; then
    echo "Failed to build tests"
    exit 1
fi

echo "${__MsgPrefix}Test build successful."
echo "${__MsgPrefix}Test binaries are available at ${__TestBinDir}"

__testNativeBinDir="$__IntermediatesDir"/tests

if [[ "$__RunTests" -ne 0 ]]; then

    echo "Run Tests..."

    nextCommand="$__TestDir/runtest.sh --testRootDir=$__TestBinDir --coreClrBinDir=$__BinDir --coreFxBinDir=$CORE_ROOT --testNativeBinDir=$__testNativeBinDir"
    echo "$nextCommand"
    eval $nextCommand

    echo "Tests run successful."
else
    echo "To run all tests use 'tests/runtests.sh' where:"
    echo "    testRootDir      = $__TestBinDir"
    echo "    coreClrBinDir    = $__BinDir"
    echo "    coreFxBinDir     = $CORE_ROOT"
    echo "    testNativeBinDir = $__testNativeBinDir"
    echo " -------------------------------------------------- "
    echo " Example runtest.sh command"
    echo ""
    echo " ./tests/runtest.sh --coreOverlayDir=$CORE_ROOT --testNativeBinDir=$__testNativeBinDir --testRootDir=$__TestBinDir --copyNativeTestBin $__BuildType"
    echo " -------------------------------------------------- "
    echo "To run single test use the following command:"
    echo "    bash ${__TestBinDir}/__TEST_PATH__/__TEST_NAME__.sh -coreroot=${CORE_ROOT}"
fi
