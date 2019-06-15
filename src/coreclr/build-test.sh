#!/usr/bin/env bash

__PortableBuild=1

initTargetDistroRid()
{
    source init-distro-rid.sh

    # Only pass ROOTFS_DIR if cross is specified.
    if (( ${__CrossBuild} == 1 )); then
        passedRootfsDir=${ROOTFS_DIR}
    fi

    initDistroRidGlobal ${__BuildOS} ${__BuildArch} ${__PortableBuild} ${passedRootfsDir}
}

isMSBuildOnNETCoreSupported()
{
    __isMSBuildOnNETCoreSupported=$__msbuildonunsupportedplatform

    if [ $__isMSBuildOnNETCoreSupported == 1 ]; then
        return
    fi

    if [ "$__HostArch" == "x64" ]; then
        if [ "$__HostOS" == "Linux" ]; then
            __isMSBuildOnNETCoreSupported=1
            UNSUPPORTED_RIDS=("debian.9-x64" "ubuntu.17.04-x64")
            for UNSUPPORTED_RID in "${UNSUPPORTED_RIDS[@]}"
            do
                if [ "${__DistroRid}" == "$UNSUPPORTED_RID" ]; then
                    __isMSBuildOnNETCoreSupported=0
                    break
                fi
            done
        elif [ "$__HostOS" == "OSX" ]; then
            __isMSBuildOnNETCoreSupported=1
        fi
    fi
}

build_test_wrappers()
{
    if [ $__BuildTestWrappers -ne -0 ]; then
        echo "${__MsgPrefix}Creating test wrappers..."

        export __Exclude="${__ProjectDir}/tests/issues.targets"
        export __BuildLogRootName="Tests_XunitWrapper"

        buildVerbosity="Summary"

        if [ $__VerboseBuild == 1 ]; then
            buildVerbosity="Diag"
        fi

        # Set up directories and file names
        __BuildLogRootName=$subDirectoryName
        __BuildLog="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.log"
        __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.wrn"
        __BuildErr="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.err"
        __MsbuildLog="/fileloggerparameters:\"Verbosity=normal;LogFile=${__BuildLog}\""
        __MsbuildWrn="/fileloggerparameters1:\"WarningsOnly;LogFile=${__BuildWrn}\""
        __MsbuildErr="/fileloggerparameters2:\"ErrorsOnly;LogFile=${__BuildErr}\""
        __Logging="$__MsbuildLog $__MsbuildWrn $__MsbuildErr /consoleloggerparameters:$buildVerbosity"

        nextCommand="\"${__DotNetCli}\" msbuild \"${__ProjectDir}/tests/runtest.proj\" /p:RestoreAdditionalProjectSources=https://dotnet.myget.org/F/dotnet-core/ /p:BuildWrappers=true /p:TargetsWindows=false $__Logging /p:__BuildOS=$__BuildOS /p:__BuildType=$__BuildType /p:__BuildArch=$__BuildArch"
        eval $nextCommand

        if [ $? -ne 0 ]; then
            echo "${__MsgPrefix}Error: build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "XUnit Wrappers have been built."
            echo { "\"build_os\"": "\"${__BuildOS}\"", "\"build_arch\"": "\"${__BuildArch}\"", "\"build_type\"": "\"${__BuildType}\"" } > "${__TestWorkingDir}/build_info.json"

        fi
    fi
}

generate_layout()
{
    echo "${__MsgPrefix}Creating test overlay..."

    __TestDir=$__ProjectDir/tests
    __ProjectFilesDir=$__TestDir
    __TestBinDir=$__TestWorkingDir

    if [ $__RebuildTests -ne 0 ]; then
        if [ -d "${__TestBinDir}" ]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf $__TestBinDir
        fi
    fi

    __CMakeBinDir="${__TestBinDir}"

    if [ -z "$__TestIntermediateDir" ]; then
        __TestIntermediateDir="tests/obj/${__BuildOS}.${__BuildArch}.${__BuildType}"
    fi

    echo "__BuildOS: ${__BuildOS}"
    echo "__BuildArch: ${__BuildArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediateDir: ${__TestIntermediateDir}"

    if [ ! -f "$__TestBinDir" ]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p $__TestBinDir
    fi
    if [ ! -f "$__LogsDir" ]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p $__LogsDir
    fi
    if [ ! -f "$__MsbuildDebugLogsDir" ]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p $__MsbuildDebugLogsDir
    fi

    # Set up the directory for MSBuild debug logs.
    export MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"

    __BuildProperties="-p:OSGroup=${__BuildOS} -p:BuildOS=${__BuildOS} -p:BuildArch=${__BuildArch} -p:BuildType=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    build_MSBuild_projects "Restore_Packages" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up="/t:UpdateInvalidPackageVersions"
    fi

    echo "${__MsgPrefix}Creating test overlay..."

    if [ -z "$xUnitTestBinBase" ]; then
        xUnitTestBinBase=$__TestWorkingDir
    fi

    export CORE_ROOT=$xUnitTestBinBase/Tests/Core_Root

    if [ -d "${CORE_ROOT}" ]; then
        rm -rf $CORE_ROOT
    fi

    mkdir -p $CORE_ROOT

    build_MSBuild_projects "Tests_Overlay_Managed" "${__ProjectDir}/tests/runtest.proj" "Creating test overlay" "/t:CreateTestOverlay"

    chmod +x $__BinDir/corerun
    chmod +x $__CrossgenExe

    # Make sure to copy over the pulled down packages
    cp -r $__BinDir/* $CORE_ROOT/ > /dev/null

    if [ "$__BuildOS" != "OSX" ]; then
        nextCommand="\"$__TestDir/setup-stress-dependencies.sh\" --arch=$__BuildArch --outputDir=$CORE_ROOT"
        echo "Resolve runtime dependences via $nextCommand"
        eval $nextCommand
        if [ $? != 0 ]; then
            echo "${__MsgPrefix}Error: setup-stress-dependencies failed."
            exit 1
        fi
    fi

    # Precompile framework assemblies with crossgen if required
    if [ $__DoCrossgen -ne 0 ]; then
        precompile_coreroot_fx
    fi
}

precompile_coreroot_fx()
{
    echo "${__MsgPrefix}Running crossgen on framework assemblies in CORE_ROOT: '${CORE_ROOT}'"

    # Read the exclusion file for this platform
    skipCrossGenFiles=($(read_array "$(dirname "$0")/tests/skipCrossGenFiles.${__BuildArch}.txt"))

    local overlayDir=$CORE_ROOT

    filesToPrecompile=$(find -L $overlayDir -iname \*.dll -not -iname \*.ni.dll -not -iname \*-ms-win-\* -not -iname xunit.\* -type f)
    for fileToPrecompile in ${filesToPrecompile}
    do
        local filename=${fileToPrecompile}
        if is_skip_crossgen_test "$(basename $filename)"; then
                continue
        fi
        echo Precompiling $filename
        $__CrossgenExe /Platform_Assemblies_Paths $overlayDir $filename 1> $filename.stdout 2>$filename.stderr
        local exitCode=$?
        if [[ $exitCode != 0 ]]; then
            if grep -q -e '0x80131018' $filename.stderr; then
                printf "\n\t$filename is not a managed assembly.\n\n"
            else
                echo Unable to precompile $filename.
                cat $filename.stdout
                cat $filename.stderr
                exit $exitCode
            fi
        else
            rm $filename.{stdout,stderr}
        fi
    done
}

declare -a skipCrossGenFiles

function is_skip_crossgen_test {
    for skip in "${skipCrossGenFiles[@]}"; do
        if [ "$1" == "$skip" ]; then
            return 0
        fi
    done
    return 1
}

# Get an array of items by reading the specified file line by line.
function read_array {
    local theArray=()

    if [ ! -f "$1" ]; then
        return
    fi

    # bash in Mac OS X doesn't support 'readarray', so using alternate way instead.
    # readarray -t theArray < "$1"
    # Any line that starts with '#' is ignored.
    while IFS='' read -r line || [ -n "$line" ]; do
        if [[ $line != "#"* ]]; then
            theArray[${#theArray[@]}]=$line
        fi
    done < "$1"
    echo ${theArray[@]}
}

generate_testhost()
{
    echo "${__MsgPrefix}Generating test host..."

    export TEST_HOST=$xUnitTestBinBase/testhost

    if [ -d "${TEST_HOST}" ]; then
        rm -rf $TEST_HOST
    fi

    mkdir -p $TEST_HOST

    build_MSBuild_projects "Tests_Generate_TestHost" "${__ProjectDir}/tests/runtest.proj" "Creating test host" "/t:CreateTestHost"
}


build_Tests()
{
    echo "${__MsgPrefix}Building Tests..."

    __TestDir=$__ProjectDir/tests
    __ProjectFilesDir=$__TestDir
    __TestBinDir=$__TestWorkingDir

    if [ -f  "${__TestWorkingDir}/build_info.json" ]; then
        rm  "${__TestWorkingDir}/build_info.json"
    fi

    if [ $__RebuildTests -ne 0 ]; then
        if [ -d "${__TestBinDir}" ]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf $__TestBinDir
        fi
    fi

    export __CMakeBinDir="${__TestBinDir}"
    if [ ! -d "${__TestIntermediatesDir}" ]; then
        mkdir -p ${__TestIntermediatesDir}
    fi

    __NativeTestIntermediatesDir="${__TestIntermediatesDir}/Native"
    if [  ! -d "${__NativeTestIntermediatesDir}" ]; then
        mkdir -p ${__NativeTestIntermediatesDir}
    fi

    __ManagedTestIntermediatesDir="${__TestIntermediatesDir}/Managed"
    if [ ! -d "${__ManagedTestIntermediatesDir}" ]; then
        mkdir -p ${__ManagedTestIntermediatesDir}
    fi

    echo "__BuildOS: ${__BuildOS}"
    echo "__BuildArch: ${__BuildArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediatesDir: ${__TestIntermediatesDir}"
    echo "__NativeTestIntermediatesDir: ${__NativeTestIntermediatesDir}"
    echo "__ManagedTestIntermediatesDir: ${__ManagedTestIntermediatesDir}"

    if [ ! -f "$__TestBinDir" ]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p $__TestBinDir
    fi
    if [ ! -f "$__LogsDir" ]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p $__LogsDir
    fi
    if [ ! -f "$__MsbuildDebugLogsDir" ]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p $__MsbuildDebugLogsDir
    fi

    # Set up the directory for MSBuild debug logs.
    export MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"

    __BuildProperties="-p:OSGroup=${__BuildOS} -p:BuildOS=${__BuildOS} -p:BuildArch=${__BuildArch} -p:BuildType=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    if [ ${__SkipRestorePackages} != 1 ]; then
        build_MSBuild_projects "Restore_Product" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"
    fi

    if [ $__SkipNative != 1 ]; then
        build_native_projects "$__BuildArch" "${__NativeTestIntermediatesDir}"

        if [ $? -ne 0 ]; then
            echo "${__MsgPrefix}Error: build failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [ $__SkipManaged != 1 ]; then
        echo "Starting the Managed Tests Build..."

        build_MSBuild_projects "Tests_Managed" "$__ProjectDir/tests/build.proj" "Managed tests build (build tests)" "$__up"

        if [ $? -ne 0 ]; then
            echo "${__MsgPrefix}Error: build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "Checking the Managed Tests Build..."

            build_MSBuild_projects "Check_Test_Build" "${__ProjectDir}/tests/runtest.proj" "Check Test Build" "/t:CheckTestBuild"

            if [ $? -ne 0 ]; then
                echo "${__MsgPrefix}Error: Check Test Build failed."
                exit 1
            fi
        fi

        echo "Managed tests build success!"
    fi

    build_test_wrappers

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up="/t:UpdateInvalidPackageVersions"
    fi

    generate_layout
}

build_MSBuild_projects()
{
    subDirectoryName=$1
    shift
    projectName=$1
    shift
    stepName="$1"
    shift
    extraBuildParameters=("$@")

    # Set up directories and file names
    __BuildLogRootName=$subDirectoryName
    __BuildLog="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.log"
    __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.wrn"
    __BuildErr="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.err"

    # Use binclashlogger by default if no other logger is specified
    if [[ "${extraBuildParameters[*]}" == *"/l:"* ]]; then
        __msbuildEventLogging=
    else
        __msbuildEventLogging="/l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll\;LogFile=binclash.log"
    fi

    if [[ "$subDirectoryName" == "Tests_Managed" ]]; then
        # Execute msbuild managed test build in stages - workaround for excessive data retention in MSBuild ConfigCache
        # See https://github.com/Microsoft/msbuild/issues/2993

        # __SkipPackageRestore and __SkipTargetingPackBuild used  to control build by tests/src/dirs.proj
        export __SkipPackageRestore=false
        export __SkipTargetingPackBuild=false
        export __NumberOfTestGroups=3

        __AppendToLog=false

        if [ -n "$__priority1" ]; then
            export __NumberOfTestGroups=10
        fi

        for (( testGroupToBuild=1 ; testGroupToBuild <= __NumberOfTestGroups; testGroupToBuild = testGroupToBuild + 1 ))
        do
            __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog};Append=${__AppendToLog}\""
            __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn};Append=${__AppendToLog}\""
            __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr};Append=${__AppendToLog}\""

            export __TestGroupToBuild=$testGroupToBuild

            # Generate build command
            buildArgs=("/nologo" "/verbosity:minimal" "/clp:Summary")
            buildArgs+=("/p:RestoreDefaultOptimizationDataPackage=false" "/p:PortableBuild=true")
            buildArgs+=("/p:UsePartialNGENOptimization=false" "/maxcpucount")

            buildArgs+=("$projectName" "${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}")
            buildArgs+=("$__msbuildEventLogging")
            buildArgs+=("${extraBuildParameters[@]}")
            buildArgs+=("${__CommonMSBuildArgs[@]}")
            buildArgs+=("${__UnprocessedBuildArgs[@]}")

            nextCommand="\"$__ProjectRoot/dotnet.sh\" msbuild ${buildArgs[@]}"
            echo "Building step '$stepName' testGroupToBuild=$testGroupToBuild via $nextCommand"
            eval $nextCommand

            # Make sure everything is OK
            if [ $? -ne 0 ]; then
                echo "${__MsgPrefix}Failed to build $stepName. See the build logs:"
                echo "    $__BuildLog"
                echo "    $__BuildWrn"
                echo "    $__BuildErr"
                exit 1
            fi
            export __SkipPackageRestore=true
            export __SkipTargetingPackBuild=true
            __AppendToLog=true
        done
    else
        __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog}\""
        __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn}\""
        __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr}\""

        # Generate build command
        buildArgs=("/nologo" "/verbosity:minimal" "/clp:Summary")
        buildArgs+=("/p:RestoreDefaultOptimizationDataPackage=false" "/p:PortableBuild=true")
        buildArgs+=("/p:UsePartialNGENOptimization=false" "/maxcpucount")

        buildArgs+=("$projectName" "${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}")
        buildArgs+=("$__msbuildEventLogging")
        buildArgs+=("${extraBuildParameters[@]}")
        buildArgs+=("${__CommonMSBuildArgs[@]}")
        buildArgs+=("${__UnprocessedBuildArgs[@]}")

        nextCommand="\"$__ProjectRoot/dotnet.sh\" msbuild ${buildArgs[@]}"
        echo "Building step '$stepName' via $nextCommand"
        eval $nextCommand

        # Make sure everything is OK
        if [ $? -ne 0 ]; then
            echo "${__MsgPrefix}Failed to build $stepName. See the build logs:"
            echo "    $__BuildLog"
            echo "    $__BuildWrn"
            echo "    $__BuildErr"
            exit 1
        fi
    fi
}

build_native_projects()
{
    platformArch="$1"
    intermediatesForBuild="$2"

    extraCmakeArguments="-DCLR_CMAKE_TARGET_OS=${__BuildOS} -DCLR_CMAKE_HOST_ARCH=${platformArch}"
    message="native tests assets"

    # All set to commence the build
    echo "Commencing build of $message for $__BuildOS.$__BuildArch.$__BuildType in $intermediatesForBuild"

    generator=""
    buildFile="Makefile"
    buildTool="make"
    if [ $__UseNinja == 1 ]; then
        generator="ninja"
        buildFile="build.ninja"
        if ! buildTool=$(command -v ninja || command -v ninja-build); then
           echo "Unable to locate ninja!" 1>&2
           exit 1
        fi
    fi

    if [ $__SkipConfigure == 0 ]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [ $__isMSBuildOnNETCoreSupported == 0 ]; then __SkipGenerateVersion=1; fi
        # Drop version.c file
        __versionSourceFile="$intermediatesForBuild/version.c"
        if [ $__SkipGenerateVersion == 0 ]; then
            pwd
            $__ProjectRoot/eng/common/msbuild.sh $__ProjectRoot/eng/empty.csproj \
                                                 /p:NativeVersionFile=$__versionSourceFile \
                                                 /p:ArcadeBuild=true /t:GenerateNativeVersionFile /restore \
                                                 $__CommonMSBuildArgs $__UnprocessedBuildArgs
            if [ $? -ne 0 ]; then
                echo "Failed to generate native version file."
                exit $?
            fi
        else
            # Generate the dummy version.c, but only if it didn't exist to make sure we don't trigger unnecessary rebuild
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            if [ -e $__versionSourceFile ]; then
                read existingVersionSourceLine < $__versionSourceFile
            fi
            if [ "$__versionSourceLine" != "$existingVersionSourceLine" ]; then
                echo $__versionSourceLine > $__versionSourceFile
            fi
        fi

        pushd "$intermediatesForBuild"
        # Regenerate the CMake solution
        # Force cross dir to point to project root cross dir, in case there is a cross build.
        scriptDir="$__ProjectRoot/src/pal/tools"
        if [[ $__GccBuild == 0 ]]; then
            nextCommand="CONFIG_DIR=\"$__ProjectRoot/cross\" \"$scriptDir/gen-buildsys-clang.sh\" \"$__TestDir\" $__ClangMajorVersion $__ClangMinorVersion $platformArch $scriptDir $__BuildType $__CodeCoverage $generator $extraCmakeArguments $__cmakeargs"
        else
            nextCommand="CONFIG_DIR=\"$__ProjectRoot/cross\" \"$scriptDir/gen-buildsys-gcc.sh\" \"$__TestDir\" \"$__GccMajorVersion\" \"$__GccMinorVersion\" $platformArch $scriptDir $__BuildType $__CodeCoverage $generator $extraCmakeArguments $__cmakeargs"
        fi
        echo "Invoking $nextCommand"
        eval $nextCommand
        popd
    fi

    if [ ! -f "$intermediatesForBuild/$buildFile" ]; then
        echo "Failed to generate $message build project!"
        exit 1
    fi

    # Build
    if [ $__ConfigureOnly == 1 ]; then
        echo "Finish configuration & skipping $message build."
        return
    fi

    pushd "$intermediatesForBuild"

    echo "Executing $buildTool install -j $__NumProc"

    $buildTool install -j $__NumProc
    if [ $? != 0 ]; then
        echo "Failed to build $message."
        exit 1
    fi

    popd
    echo "Native tests build success!"
}

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [verbose] [coverage] [cross] [clangx.y] [ninja] [runtests] [bindir]"
    echo "BuildArch can be: x64, x86, arm, armel, arm64"
    echo "BuildType can be: debug, checked, release"
    echo "coverage - optional argument to enable code coverage build (currently supported only for Linux and OSX)."
    echo "ninja - target ninja instead of GNU make"
    echo "clangx.y - optional argument to build using clang version x.y - supported version 3.5 - 6.0"
    echo "gccx.y - optional argument to build using gcc version x.y."
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "portableLinux - build for Portable Linux Distribution"
    echo "portablebuild - Use portable build."
    echo "verbose - optional argument to enable verbose build output."
    echo "rebuild - if tests have already been built - rebuild them"
    echo "skipnative: skip the native tests build"
    echo "skipmanaged: skip the managed section of the test build"
    echo "buildtestwrappersonly - only build the test wrappers"
    echo "generatelayoutonly - only pull down dependencies and build coreroot"
    echo "generatetesthostonly - only pull down dependencies and build coreroot and the CoreFX testhost"
    echo "skiprestorepackages - skip package restore"
    echo "crossgen - Precompiles the framework managed assemblies in coreroot"
    echo "runtests - run tests after building them"
    echo "bindir - output directory (defaults to $__ProjectRoot/bin)"
    echo "msbuildonunsupportedplatform - build managed binaries even if distro is not officially supported."
    echo "priority1 - include priority=1 tests in the build"
    exit 1
}


# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Use uname to determine what the CPU is.
CPUName=$(uname -p)

# Some Linux platforms report unknown for platform, but the arch for machine.
if [ "$CPUName" == "unknown" ]; then
    CPUName=$(uname -m)
fi

case $CPUName in
    i686)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=x86
        __HostArch=x86
        ;;

    x86_64)
        __BuildArch=x64
        __HostArch=x64
        ;;

    armv7l)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=arm
        __HostArch=arm
        ;;

    aarch64)
        __BuildArch=arm64
        __HostArch=arm64
        ;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __BuildArch=x64
        __HostArch=x64
        ;;
esac

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        __HostOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        __HostOS=Linux
        ;;
esac

__BuildType=Debug
__CodeCoverage=
__IncludeTests=INCLUDE_TESTS

# Set the various build properties here so that CMake and MSBuild can pick them up
export __ProjectDir="$__ProjectRoot"
__SourceDir="$__ProjectDir/src"
__PackagesDir="$__ProjectDir/.packages"
__RootBinDir="$__ProjectDir/bin"
__BuildToolsDir="$__ProjectDir/Tools"
__DotNetCli="$__ProjectDir/dotnet.sh"
__UnprocessedBuildArgs=
__CommonMSBuildArgs=
__MSBCleanBuildArgs=
__UseNinja=0
__VerboseBuild=0
__SkipRestore=""
__SkipNative=0
__SkipManaged=0
__SkipConfigure=0
__SkipGenerateVersion=0
__ConfigureOnly=0
__CrossBuild=0
__ClangMajorVersion=0
__ClangMinorVersion=0
__GccBuild=0
__GccMajorVersion=0
__GccMinorVersion=0
__NuGetPath="$__PackagesDir/NuGet.exe"
__SkipRestorePackages=0
__DistroRid=""
__cmakeargs=""
__PortableLinux=0
__msbuildonunsupportedplatform=0
__NativeTestIntermediatesDir=
__RunTests=0
__RebuildTests=0
__BuildTestWrappers=1
__GenerateLayoutOnly=
__GenerateTestHostOnly=
__priority1=
__BuildTestWrappersOnly=
__DoCrossgen=0
CORE_ROOT=

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        x86)
            __BuildArch=x86
            ;;

        x64)
            __BuildArch=x64
            ;;

        arm)
            __BuildArch=arm
            ;;

        armel)
            __BuildArch=armel
            ;;

        arm64)
            __BuildArch=arm64
            ;;

        debug)
            __BuildType=Debug
            ;;

        checked)
            __BuildType=Checked
            ;;

        release)
            __BuildType=Release
            ;;

        coverage)
            __CodeCoverage=Coverage
            ;;

        cross)
            __CrossBuild=1
            ;;

        portablebuild=false)
            __PortableBuild=0
            ;;

        portablelinux)
            if [ "$__BuildOS" == "Linux" ]; then
                __PortableLinux=1
            else
                echo "ERROR: portableLinux not supported for non-Linux platforms."
                exit 1
            fi
            ;;

        verbose)
            __VerboseBuild=1
            ;;

        clang3.5|-clang3.5)
            __ClangMajorVersion=3
            __ClangMinorVersion=5
            ;;

        clang3.6|-clang3.6)
            __ClangMajorVersion=3
            __ClangMinorVersion=6
            ;;

        clang3.7|-clang3.7)
            __ClangMajorVersion=3
            __ClangMinorVersion=7
            ;;

        clang3.8|-clang3.8)
            __ClangMajorVersion=3
            __ClangMinorVersion=8
            ;;

        clang3.9|-clang3.9)
            __ClangMajorVersion=3
            __ClangMinorVersion=9
            ;;

        clang4.0|-clang4.0)
            __ClangMajorVersion=4
            __ClangMinorVersion=0
            ;;

        clang5.0|-clang5.0)
            __ClangMajorVersion=5
            __ClangMinorVersion=0
            ;;

        clang6.0|-clang6.0)
            __ClangMajorVersion=6
            __ClangMinorVersion=0
            ;;

        gcc5|-gcc5)
            __GccMajorVersion=5
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc6|-gcc6)
            __GccMajorVersion=6
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc7|-gcc7)
            __GccMajorVersion=7
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc8|-gcc8)
            __GccMajorVersion=8
            __GccMinorVersion=
            __GccBuild=1
            ;;

        gcc|-gcc)
            __GccMajorVersion=
            __GccMinorVersion=
            __GccBuild=1
            ;;

        ninja)
            __UseNinja=1
            ;;

        runtests)
            __RunTests=1
            ;;

        rebuild)
            __RebuildTests=1
            ;;

        skipnative|-skipnative)
            __SkipNative=1
            ;;

        skipmanaged|-skipmanaged)
            __SkipManaged=1
            __BuildTestWrappers=0
            ;;

        buildtestwrappersonly)
            __BuildTestWrappersOnly=1
            ;;

        generatelayoutonly)
            __GenerateLayoutOnly=1
            ;;

        generatetesthostonly)
            __GenerateTestHostOnly=1
            ;;

        skiprestorepackages)
            __SkipRestorePackages=1
            ;;

        crossgen)
            __DoCrossgen=1
            ;;

        bindir)
            if [ -n "$2" ]; then
                __RootBinDir="$2"
                if [ ! -d $__RootBinDir ]; then
                    mkdir $__RootBinDir
                fi
                __RootBinParent=$(dirname $__RootBinDir)
                __RootBinName=${__RootBinDir##*/}
                __RootBinDir="$(cd $__RootBinParent &>/dev/null && printf %s/%s $PWD $__RootBinName)"
                shift
            else
                echo "ERROR: 'bindir' requires a non-empty option argument"
                exit 1
            fi
            ;;

        msbuildonunsupportedplatform)
            __msbuildonunsupportedplatform=1
            ;;

        priority1)
            __priority1=1
            __UnprocessedBuildArgs+=("/p:CLRTestPriorityToBuild=1")
            ;;

        *)
            __UnprocessedBuildArgs+=("$1")
            ;;
    esac

    shift
done

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
if [ `uname` = "FreeBSD" ]; then
  __NumProc=`sysctl hw.ncpu | awk '{ print $2+1 }'`
elif [ `uname` = "NetBSD" ]; then
  __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
elif [ `uname` = "Darwin" ]; then
  __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
else
  __NumProc=$(nproc --all)
fi

__CommonMSBuildArgs=("/p:__BuildArch=$__BuildArch" "/p:__BuildType=$__BuildType" "/p:__BuildOS=$__BuildOS" "/nodeReuse:false")

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
    __CommonMSBuildArgs+=("/v:detailed")
fi

# Set default clang version
if [[ $__ClangMajorVersion == 0 && $__ClangMinorVersion == 0 ]]; then
    if [[ "$__BuildArch" == "arm" || "$__BuildArch" == "armel" ]]; then
        __ClangMajorVersion=5
        __ClangMinorVersion=0
    else
        __ClangMajorVersion=3
        __ClangMinorVersion=9
    fi
fi

# Set dependent variables
__LogsDir="$__RootBinDir/Logs"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__TestDir="$__ProjectDir/tests"
__TestWorkingDir="$__RootBinDir/tests/$__BuildOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/obj/$__BuildOS.$__BuildArch.$__BuildType"
__isMSBuildOnNETCoreSupported=0
__CrossComponentBinDir="$__BinDir"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"

__CrossArch="$__HostArch"
if [ $__CrossBuild == 1 ]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossgenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__BuildOS.$BuildArch.$__BuildType.log"
__CrossgenExe="$__CrossComponentBinDir/crossgen"

isMSBuildOnNETCoreSupported

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to it.
# This is needed by CLI to function.
if [ -z "$HOME" ]; then
    if [ ! -d "$__ProjectDir/temp_home" ]; then
        mkdir temp_home
    fi
    export HOME=$__ProjectDir/temp_home
    echo "HOME not defined; setting it to $HOME"
fi

# Configure environment if we are doing a cross compile.
if [ $__CrossBuild == 1 ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__ProjectRoot/cross/rootfs/$__BuildArch"
    fi
fi

# init the target distro name
initTargetDistroRid

if [ $__PortableBuild == 0 ]; then
    __CommonMSBuildArgs="$__CommonMSBuildArgs /p:PortableBuild=false"
fi

# Restore Build Tools
source $__ProjectRoot/init-tools.sh

if [[ (-z "$__GenerateLayoutOnly") && (-z "$__GenerateTestHostOnly") && (-z "$__BuildTestWrappersOnly") ]]; then
    build_Tests
elif [ ! -z "$__BuildTestWrappersOnly" ]; then
    build_test_wrappers
else
    generate_layout
    if [ ! -z "$__GenerateTestHostOnly" ]; then
        generate_testhost
    fi
fi

if [ $? -ne 0 ]; then
    echo "Failed to build tests"
    exit 1
fi

echo "${__MsgPrefix}Test build successful."
echo "${__MsgPrefix}Test binaries are available at ${__TestBinDir}"

__testNativeBinDir=$__IntermediatesDir/tests

if [ $__RunTests -ne 0 ]; then

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
    echo " ./tests/runtest.sh --coreOverlayDir=$CORE_ROOT --testNativeBinDir=$__testNativeBinDir --testRootDir=$__TestBinDir --copyNativeTestBin"
    echo " -------------------------------------------------- "
    echo "To run single test use the following command:"
    echo "    bash ${__TestBinDir}/__TEST_PATH__/__TEST_NAME__.sh -coreroot=${CORE_ROOT}"
fi

