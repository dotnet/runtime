#!/usr/bin/env bash

build_test_wrappers()
{
    if [[ "$__BuildTestWrappers" -ne -0 ]]; then
        echo "${__MsgPrefix}Creating test wrappers..."

        __Exclude="$__RepoRootDir/src/tests/issues.targets"
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

        nextCommand="\"${__DotNetCli}\" msbuild \"$__RepoRootDir/src/tests/run.proj\" /nodereuse:false /p:BuildWrappers=true /p:TestBuildMode=$__TestBuildMode /p:TargetsWindows=${TestWrapperTargetsWindows} $__Logging /p:TargetOS=$__TargetOS /p:Configuration=$__BuildType /p:TargetArchitecture=$__BuildArch /p:RuntimeFlavor=$__RuntimeFlavor \"/bl:${__RepoRootDir}/artifacts/log/${__BuildType}/build_test_wrappers_${__RuntimeFlavor}.binlog\" ${__UnprocessedBuildArgs[@]}"
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

build_mono_aot()
{
    __RuntimeFlavor="mono"
    __Exclude="$__RepoRootDir/src/tests/issues.targets"
    __TestBinDir="$__TestWorkingDir"
    CORE_ROOT="$__TestBinDir"/Tests/Core_Root
    export __Exclude
    export CORE_ROOT
    build_MSBuild_projects "Tests_MonoAot" "$__RepoRootDir/src/tests/run.proj" "Mono AOT compile tests" "/t:MonoAotCompileTests" "/p:RuntimeFlavor=$__RuntimeFlavor" "/p:MonoBinDir=$__MonoBinDir"
}

build_ios_apps()
{
    __RuntimeFlavor="mono" \
    __Exclude="$__RepoRootDir/src/tests/issues.targets" \
    build_MSBuild_projects "Create_iOS_App" "$__RepoRootDir/src/tests/run.proj" "Create iOS Apps" "/t:BuildAlliOSApp"
}

generate_layout()
{
    echo "${__MsgPrefix}Creating test overlay..."

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

    build_MSBuild_projects "Restore_Packages" "$__RepoRootDir/src/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

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

    build_MSBuild_projects "Tests_Overlay_Managed" "$__RepoRootDir/src/tests/run.proj" "Creating test overlay" "/t:CreateTestOverlay"

    # Precompile framework assemblies with crossgen if required
    if [[ "$__DoCrossgen2" != 0 ]]; then
        if [[ "$__SkipCrossgenFramework" == 0 ]]; then
            precompile_coreroot_fx
        fi
    fi
}

precompile_coreroot_fx()
{
    # Get the number of processors available to the scheduler
    # Other techniques such as `nproc` only get the number of
    # processors available to a single process.
    local platform="$(uname)"
    if [[ "$platform" == "FreeBSD" ]]; then
        __NumProc=$(($(sysctl -n hw.ncpu)+1))
    elif [[ "$platform" == "NetBSD" || "$platform" == "SunOS" ]]; then
        __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
    elif [[ "$platform" == "Darwin" ]]; then
        __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
    else
        __NumProc=$(nproc --all)
    fi

    local outputDir="$__TestIntermediatesDir/crossgen.out"
    local crossgenCmd="\"$__DotNetCli\" \"$CORE_ROOT/R2RTest/R2RTest.dll\" compile-framework -cr \"$CORE_ROOT\" --output-directory \"$outputDir\" --release --nocleanup --target-arch $__BuildArch -dop $__NumProc  -m \"$CORE_ROOT/StandardOptimizationData.mibc\""

    if [[ "$__CompositeBuildMode" != 0 ]]; then
        crossgenCmd="$crossgenCmd --composite"
    else
        crossgenCmd="$crossgenCmd --crossgen2-parallelism 1"
    fi

    local crossgenDir="$__BinDir"
    if [[ "$__CrossBuild" == 1 ]]; then
        crossgenDir="$crossgenDir/$__HostArch"
    fi

    crossgenCmd="$crossgenCmd --verify-type-and-field-layout --crossgen2-path \"$crossgenDir/crossgen2/crossgen2.dll\""

    echo "Running $crossgenCmd"
    eval $crossgenCmd
    local exitCode="$?"

    if [[ "$exitCode" != 0 ]]; then
        echo "Failed to crossgen the framework"
        return 1
    fi

    mv "$outputDir"/*.dll "$CORE_ROOT"

    return 0
}

build_Tests()
{
    echo "${__MsgPrefix}Building Tests..."

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
        build_MSBuild_projects "Restore_Product" "$__RepoRootDir/src/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: package restoration failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [[ "$__SkipNative" != 1 && "$__TargetOS" != "Browser" && "$__TargetOS" != "Android" && "$__TargetOS" != "iOS" && "$__TargetOS" != "iOSSimulator" ]]; then
        build_native "$__TargetOS" "$__BuildArch" "$__TestDir" "$__NativeTestIntermediatesDir" "install" "CoreCLR test component"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: native test build failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [[ "$__SkipManaged" != 1 ]]; then
        echo "Starting the Managed Tests Build..."

        build_MSBuild_projects "Tests_Managed" "$__RepoRootDir/src/tests/build.proj" "Managed tests build (build tests)" "$__up" "/p:RuntimeFlavor=$__RuntimeFlavor"

        if [[ "$?" -ne 0 ]]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: managed test build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "Checking the Managed Tests Build..."

            build_MSBuild_projects "Check_Test_Build" "$__RepoRootDir/src/tests/run.proj" "Check Test Build" "/t:CheckTestBuild"

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

        build_MSBuild_projects "Tests_Managed" "$__RepoRootDir/src/tests/build.proj" "Managed tests build (build tests)" "/t:CopyAllNativeProjectReferenceBinaries" "/bl:${__RepoRootDir}/artifacts/log/${__BuildType}/copy_native_test_binaries${__RuntimeFlavor}.binlog"

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
            buildArgs+=("/bl:${__RepoRootDir}/artifacts/log/${__BuildType}/build_managed_tests_${testGroupToBuild}.binlog");

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

usage_list=()

usage_list+=("-skiprestorepackages: skip package restore.")
usage_list+=("-skipgeneratelayout: Do not generate the Core_Root layout.")
usage_list+=("-skiptestwrappers: Don't generate test wrappers.")

usage_list+=("-buildtestwrappersonly: only build the test wrappers.")
usage_list+=("-copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.")
usage_list+=("-generatelayoutonly: only pull down dependencies and build coreroot.")

usage_list+=("-crossgen2: Precompiles the framework managed assemblies in coreroot using the Crossgen2 compiler.")
usage_list+=("-priority1: include priority=1 tests in the build.")
usage_list+=("-allTargets: Build managed tests for all target platforms.")

usage_list+=("-rebuild: if tests have already been built - rebuild them.")
usage_list+=("-runtests: run tests after building them.")
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
            __SkipGenerateLayout=1
            __SkipCrossgenFramework=1
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

        generatelayoutonly|-generatelayoutonly)
            __GenerateLayoutOnly=1
            ;;

        priority1|-priority1)
            __priority1=1
            __UnprocessedBuildArgs+=("/p:CLRTestPriorityToBuild=1")
            ;;

        allTargets|-allTargets)
            __UnprocessedBuildArgs+=("/p:CLRTestBuildAllTargets=allTargets")
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

        mono_aot|-mono_aot)
            __Mono=1
            __MonoAot=1
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
__DoCrossgen2=0
__CompositeBuildMode=0
__TestBuildMode=
__DotNetCli="$__RepoRootDir/dotnet.sh"
__GenerateLayoutOnly=
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
__UseNinja=0
__VerboseBuild=0
__CMakeArgs=""
__priority1=
__Mono=0
__MonoAot=0
CORE_ROOT=

source $__RepoRootDir/src/coreclr/_build-commons.sh

if [[ "${__BuildArch}" != "${__HostArch}" ]]; then
    __CrossBuild=1
fi

if [[ "$__CrossBuild" == 1 && "$__TargetOS" != "Android" ]]; then
    __UnprocessedBuildArgs+=("/p:CrossBuild=true")
fi

if [[ $__Mono -eq 1 ]]; then
    __RuntimeFlavor="mono"
else
    __RuntimeFlavor="coreclr"
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__OSPlatformConfig="$__TargetOS.$__BuildArch.$__BuildType"
__BinDir="$__RootBinDir/bin/coreclr/$__OSPlatformConfig"
__PackagesBinDir="$__BinDir/.nuget"
__TestDir="$__RepoRootDir/src/tests"
__TestWorkingDir="$__RootBinDir/tests/coreclr/$__OSPlatformConfig"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__OSPlatformConfig"
__TestIntermediatesDir="$__RootBinDir/tests/coreclr/obj/$__OSPlatformConfig"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"
__MonoBinDir="$__RootBinDir/bin/mono/$__OSPlatformConfig"

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

if [[ (-z "$__GenerateLayoutOnly") && (-z "$__BuildTestWrappersOnly") && ("$__MonoAot" -eq 0) ]]; then
    build_Tests
elif [[ ! -z "$__BuildTestWrappersOnly" ]]; then
    build_test_wrappers
elif [[ "$__MonoAot" -eq 1 ]]; then
    build_mono_aot
else
    generate_layout
fi

if [[ "$?" -ne 0 ]]; then
    echo "Failed to build tests"
    exit 1
fi

echo "${__MsgPrefix}Test build successful."
echo "${__MsgPrefix}Test binaries are available at ${__TestBinDir}"

if [ "$__TargetOS" == "Android" ]; then
    build_MSBuild_projects "Create_Android_App" "$__RepoRootDir/src/tests/run.proj" "Create Android Apps" "/t:BuildAllAndroidApp" "/p:RunWithAndroid=true"
elif [ "$__TargetOS" == "iOS" ] || [ "$__TargetOS" == "iOSSimulator" ]; then
    build_ios_apps
fi

if [[ "$__RunTests" -ne 0 ]]; then

    echo "Run Tests..."

    nextCommand="$__TestDir/run.sh --testRootDir=$__TestBinDir"
    echo "$nextCommand"
    eval $nextCommand

    echo "Tests run successful."
else
    echo "To run all the tests use:"
    echo ""
    echo "    src/tests/run.sh $__BuildType"
    echo ""
    echo "To run a single test use:"
    echo ""
    echo "    bash ${__TestBinDir}/__TEST_PATH__/__TEST_NAME__.sh -coreroot=${CORE_ROOT}"
    echo ""
fi
