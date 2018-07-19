#!/usr/bin/env bash

initHostDistroRid()
{
    __HostDistroRid=""

    # Some OS groups should default to use the portable packages
    if [ "$__BuildOS" == "OSX" ]; then
        __PortableBuild=1
    fi

    if [ "$__HostOS" == "Linux" ]; then
        if [ -e /etc/redhat-release ]; then
            local redhatRelease=$(</etc/redhat-release)
            if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
                __HostDistroRid="rhel.6-$__HostArch"
            else
                __PortableBuild=1
            fi
        elif [ -e /etc/os-release ]; then
            source /etc/os-release
            if [[ $ID == "alpine" ]]; then
                __HostDistroRid="linux-musl-$__HostArch"
            else
                __PortableBuild=1
                __HostDistroRid="$ID.$VERSION_ID-$__HostArch"
            fi
        fi
    elif [ "$__HostOS" == "FreeBSD" ]; then
        __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'.'`
        __HostDistroRid="freebsd.$__freebsd_version-$__HostArch"
    fi

    # Portable builds target the base RID
    if [ "$__PortableBuild" == 1 ]; then
        if [ "$__BuildOS" == "OSX" ]; then
            export __HostDistroRid="osx-$__BuildArch"
        elif [ "$__BuildOS" == "Linux" ]; then
            export __HostDistroRid="linux-$__BuildArch"
        fi
    fi

    if [ "$__HostDistroRid" == "" ]; then
        echo "WARNING: Cannot determine runtime id for current distro."
    fi

    echo "Setting __HostDistroRid to $__HostDistroRid"
}

initTargetDistroRid()
{
    if [ $__CrossBuild == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            if [ ! -e $ROOTFS_DIR/etc/os-release ]; then
                if [ -e $ROOTFS_DIR/android_platform ]; then
                    source $ROOTFS_DIR/android_platform
                    export __DistroRid="$RID"
                else
                    echo "WARNING: Cannot determine runtime id for current distro."
                    export __DistroRid=""
                fi
            else
                source $ROOTFS_DIR/etc/os-release
                export __DistroRid="$ID.$VERSION_ID-$__BuildArch"
            fi
        fi
    else
        export __DistroRid="$__HostDistroRid"
    fi

    if [ "$ID.$VERSION_ID" == "ubuntu.16.04" ]; then
     export __DistroRid="ubuntu.14.04-$__BuildArch"
    fi

    # Portable builds target the base RID
    if [ "$__PortableBuild" == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            export __DistroRid="linux-$__BuildArch"
            export __RuntimeId="linux-$__BuildArch"
        elif [ "$__BuildOS" == "OSX" ]; then
            export __DistroRid="osx-$__BuildArch"
            export __RuntimeId="osx-$__BuildArch"
        fi
    fi

    echo "__DistroRid: " $__DistroRid
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
                if [ "$__HostDistroRid" == "$UNSUPPORTED_RID" ]; then
                    __isMSBuildOnNETCoreSupported=0
                    break
                fi
            done
        elif [ "$__HostOS" == "OSX" ]; then
            __isMSBuildOnNETCoreSupported=1
        fi
    fi
}

generate_layout()
{
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

    __BuildProperties="-p:OSGroup=${__BuildOS} -p:BuildOS=${__BuildOS} -p:BuildArch=${__BuildArch} -p:BuildType=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    build_Tests_internal "Restore_Packages" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "-BatchRestorePackages"

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up=-updateinvalidpackageversion
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

    build_Tests_internal "Tests_Overlay_Managed" "${__ProjectDir}/tests/runtest.proj" "Creating test overlay" "-testOverlay" 

    chmod +x $__BinDir/corerun
    chmod +x $__BinDir/crossgen

    # Make sure to copy over the pulled down packages
    cp -r $__BinDir/* $CORE_ROOT/ > /dev/null

}

generate_testhost()
{
    export TEST_HOST=$xUnitTestBinBase/testhost

    if [ -d "${TEST_HOST}" ]; then
        rm -rf $TEST_HOST
    fi

    echo "${__MsgPrefix}Creating test overlay..."    
    mkdir -p $TEST_HOST

    build_Tests_internal "Tests_Generate_TestHost" "${__ProjectDir}/tests/runtest.proj" "Creating test host" "-testHost"
}


build_Tests()
{
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

    __BuildProperties="-p:OSGroup=${__BuildOS} -p:BuildOS=${__BuildOS} -p:BuildArch=${__BuildArch} -p:BuildType=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    build_Tests_internal "Restore_Product" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "-BatchRestorePackages"

    if [ -n "$__BuildAgainstPackagesArg" ]; then
        build_Tests_internal "Tests_GenerateRuntimeLayout" "${__ProjectDir}/tests/runtest.proj" "Restore product binaries (run tests)" "-BinPlaceRef" "-BinPlaceProduct" "-CopyCrossgenToProduct"
    fi

    echo "Starting the Managed Tests Build..."

    build_Tests_internal "Tests_Managed" "$__ProjectDir/tests/build.proj" "Managed tests build (build tests)" "$__up"

    if [ $? -ne 0 ]; then
        echo "${__MsgPrefix}Error: build failed. Refer to the build log files for details (above)"
        exit 1
    else
        echo "Managed tests build success!"
    fi

    if [ $__BuildTestWrappers -ne -0 ]; then
        echo "${__MsgPrefix}Creating test wrappers..."

        __XUnitWrapperBuiltMarker=${__TestBinDir}/xunit_wrapper_build

        if [ ! -f $__XUnitWrapperBuiltMarker ]; then

            build_Tests_internal "Tests_XunitWrapper" "$__ProjectDir/tests/runtest.proj" "Test Xunit Wrapper" "-BuildWrappers" "-MsBuildEventLogging= " "-TargetsWindows=false"

            if [ $? -ne 0 ]; then
                echo "${__MsgPrefix}Error: build failed. Refer to the build log files for details (above)"
                exit 1
            else
                echo "XUnit Wrappers have been built."
                echo "Create marker \"${__XUnitWrapperBuiltMarker}\""
                touch $__XUnitWrapperBuiltMarker
            fi
        else
            echo "XUnit Wrappers had been built before."
        fi
    fi

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up=-updateinvalidpackageversion
    fi

    echo "${__MsgPrefix}Creating test overlay..."

    generate_layout

    if [ $__ZipTests -ne 0 ]; then
        echo "${__MsgPrefix}ZIP tests packages..."
        build_Tests_internal "Helix_Prep" "$__ProjectDir/tests/helixprep.proj" "Prep test binaries for Helix publishing" " "
    fi
}

build_Tests_internal()
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
    if [[ "${extraBuildParameters[*]}" == *"-MsBuildEventLogging"* ]]; then
        msbuildEventLogging=""
    else
        msbuildEventLogging="-MsBuildEventLogging=\"/l:BinClashLogger,Tools/Microsoft.DotNet.Build.Tasks.dll;LogFile=binclash.log\""
    fi

    if [[ "$subDirectoryName" == "Tests_Managed" ]]; then
        # Execute msbuild managed test build in stages - workaround for excessive data retention in MSBuild ConfigCache
        # See https://github.com/Microsoft/msbuild/issues/2993

        # __SkipPackageRestore and __SkipTargetingPackBuild used  to control build by tests/src/dirs.proj
        export __SkipPackageRestore=false
        export __SkipTargetingPackBuild=false
        export __BuildLoopCount=2
        export __TestGroupToBuild=1
        __AppendToLog=false

        if [ -n __priority1 ]; then
            export __BuildLoopCount=16
            export __TestGroupToBuild=2
        fi

        for (( slice=1 ; slice <= __BuildLoopCount; slice = slice + 1 ))
        do
            __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog};Append=${__AppendToLog}\""
            __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn};Append=${__AppendToLog}\""
            __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr};Append=${__AppendToLog}\""

            export TestBuildSlice=$slice

            # Generate build command
            buildArgs=("-Project=$projectName" "-MsBuildLog=${__msbuildLog}" "-MsBuildWrn=${__msbuildWrn}" "-MsBuildErr=${__msbuildErr}")
            buildArgs+=("$msbuildEventLogging")
            buildArgs+=("${extraBuildParameters[@]}")
            buildArgs+=("${__RunArgs[@]}")
            buildArgs+=("${__UnprocessedBuildArgs[@]}")

            echo "Building step '$stepName' slice=$slice via $buildCommand"

            # Invoke MSBuild
            "$__ProjectRoot/run.sh" build "${buildArgs[@]}"

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
        buildArgs=("-Project=$projectName" "-MsBuildLog=${__msbuildLog}" "-MsBuildWrn=${__msbuildWrn}" "-MsBuildErr=${__msbuildErr}")
        buildArgs+=("$msbuildEventLogging")
        buildArgs+=("${extraBuildParameters[@]}")
        buildArgs+=("${__RunArgs[@]}")
        buildArgs+=("${__UnprocessedBuildArgs[@]}")

        echo "Building step '$stepName' via $buildCommand"

        # Invoke MSBuild
        "$__ProjectRoot/run.sh" build "${buildArgs[@]}"

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

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [verbose] [coverage] [cross] [clangx.y] [ninja] [runtests] [bindir]"
    echo "BuildArch can be: x64, x86, arm, armel, arm64"
    echo "BuildType can be: debug, checked, release"
    echo "coverage - optional argument to enable code coverage build (currently supported only for Linux and OSX)."
    echo "ninja - target ninja instead of GNU make"
    echo "clangx.y - optional argument to build using clang version x.y - supported version 3.5 - 6.0"
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "crosscomponent - optional argument to build cross-architecture component,"
    echo "               - will use CAC_ROOTFS_DIR environment variable if set."
    echo "portableLinux - build for Portable Linux Distribution"
    echo "portablebuild - Use portable build."
    echo "verbose - optional argument to enable verbose build output."
    echo "rebuild - if tests have already been built - rebuild them"
    echo "generatelayoutonly - only pull down dependencies and build coreroot"
    echo "generatetesthostonly - only pull down dependencies and build coreroot and the CoreFX testhost"
    echo "buildagainstpackages - pull down and build using packages."
    echo "runtests - run tests after building them"
    echo "ziptests - zips CoreCLR tests & Core_Root for a Helix run"
    echo "bindir - output directory (defaults to $__ProjectRoot/bin)"
    echo "msbuildonunsupportedplatform - build managed binaries even if distro is not officially supported."
    echo "priority1 - include priority=1 tests in the build"
    exit 1
}


# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# $__ProjectRoot/build.sh $1 $2

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
__PackagesDir="$__ProjectDir/packages"
__RootBinDir="$__ProjectDir/bin"
__BuildToolsDir="$__ProjectDir/Tools"
__UnprocessedBuildArgs=
__RunArgs=
__MSBCleanBuildArgs=
__UseNinja=0
__VerboseBuild=0
__SkipRestore=""
__CrossBuild=0
__ClangMajorVersion=0
__ClangMinorVersion=0
__NuGetPath="$__PackagesDir/NuGet.exe"
__HostDistroRid=""
__BuildAgainstPackagesArg=
__DistroRid=""
__cmakeargs=""
__PortableLinux=0
__msbuildonunsupportedplatform=0
__ZipTests=0
__NativeTestIntermediatesDir=
__RunTests=0
__RebuildTests=0
__BuildTestWrappers=1
__GenerateLayoutOnly=
__GenerateTestHostOnly=
__priority1=
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

        portableBuild)
            __PortableBuild=1
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

        ninja)
            __UseNinja=1
            ;;

        runtests)
            __RunTests=1
            ;;

        rebuild)
            __RebuildTests=1
            ;;

        ziptests)
            __ZipTests=1
            ;;

        generatelayoutonly)
            __GenerateLayoutOnly=1
            ;;
        generatetesthostonly)
            __GenerateTestHostOnly=1
            ;;
        buildagainstpackages)
            __BuildAgainstPackagesArg=1
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
            __UnprocessedBuildArgs+=("-priority=1")
            ;;
        *)
            __UnprocessedBuildArgs+=("$1")
            ;;
    esac

    shift
done


__RunArgs=("-BuildArch=$__BuildArch" "-BuildType=$__BuildType" "-BuildOS=$__BuildOS")

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
    __RunArgs+=("-verbose")
fi

# Set default clang version
if [[ $__ClangMajorVersion == 0 && $__ClangMinorVersion == 0 ]]; then
    if [ $__CrossBuild == 1 ]; then
        __ClangMajorVersion=3
        __ClangMinorVersion=6
    else
        __ClangMajorVersion=3
        __ClangMinorVersion=5
    fi
fi


# Set dependent variables
__LogsDir="$__RootBinDir/Logs"

# init the host distro name
initHostDistroRid

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/Product/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__ToolsDir="$__RootBinDir/tools"
__TestDir="$__ProjectDir/tests"
__TestWorkingDir="$__RootBinDir/tests/$__BuildOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/obj/$__BuildOS.$__BuildArch.$__BuildType"
__isMSBuildOnNETCoreSupported=0
__CrossComponentBinDir="$__BinDir"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"

__CrossArch="$__HostArch"
if [[ "$__HostArch" == "x64" && "$__BuildArch" == "arm" ]]; then
    __CrossArch="x86"
fi
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

# Specify path to be set for CMAKE_INSTALL_PREFIX.
# This is where all built CoreClr libraries will copied to.
export __CMakeBinDir="$__BinDir"
if [ [ ! -d "$__BinDir" ] || [ ! -d "$__BinDir/bin" ] ]; then
    if [ [ -z "$__GenerateLayoutOnly" ] && [ -z "$__GenerateTestHostOnly" ] ]; then

        echo "Cannot find build directory for the CoreCLR native tests."
        echo "Please make sure native tests are built before building managed tests."
        echo "Example use: './build.sh $__BuildArch $__BuildType' without -skiptests switch"
    else
        echo "Cannot find build directory for the CoreCLR Product."
        echo "Please make sure CoreCLR and native tests are built before building managed tests."
        echo "Example use: './build.sh $__BuildArch $__BuildType' "
        fi
    exit 1
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

# Override tool directory

__CoreClrVersion=1.1.0
__sharedFxDir=$__BuildToolsDir/dotnetcli/shared/Microsoft.NETCore.App/$__CoreClrVersion/


if [[ (-z "$__GenerateLayoutOnly") && (-z "$__GenerateTestHostOnly") ]]; then
    echo "Building Tests..."
    build_Tests
else
    echo "Generating test layout..."
    generate_layout
    if [ ! -z "$__GenerateTestHostOnly" ]; then
        echo "Generating test host..."        
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

    echo "${__TestDir}/runtest.sh --testRootDir=$__TestBinDir --coreClrBinDir=$__BinDir --coreFxBinDir=$__sharedFxDir --testNativeBinDir=$__testNativeBinDir"

    $__TestDir/runtest.sh --testRootDir=$__TestBinDir --coreClrBinDir=$__BinDir --coreFxBinDir=$CORE_ROOT --testNativeBinDir=$__testNativeBinDir

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

