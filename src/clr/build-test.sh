#!/usr/bin/env bash

initHostDistroRid()
{
    __HostDistroRid=""
    if [ "$__HostOS" == "Linux" ]; then
        if [ -e /etc/os-release ]; then
            source /etc/os-release
            if [[ $ID == "alpine" ]]; then
                # remove the last version digit
                VERSION_ID=${VERSION_ID%.*}
            fi
            __HostDistroRid="$ID.$VERSION_ID-$__HostArch"
        elif [ -e /etc/redhat-release ]; then
            local redhatRelease=$(</etc/redhat-release)
            if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
               __HostDistroRid="rhel.6-$__HostArch"
            fi
        fi
    fi
    if [ "$__HostOS" == "FreeBSD" ]; then
        __freebsd_version=`sysctl -n kern.osrelease | cut -f1 -d'.'`
        __HostDistroRid="freebsd.$__freebsd_version-$__HostArch"
    fi

    if [ "$__HostDistroRid" == "" ]; then
        echo "WARNING: Cannot determine runtime id for current distro."
    fi
}

initTargetDistroRid()
{
    if [ $__CrossBuild == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            if [ ! -e $ROOTFS_DIR/etc/os-release ]; then
                echo "WARNING: Can not determine runtime id for current distro."
                export __DistroRid=""
            else
                source $ROOTFS_DIR/etc/os-release
                export __DistroRid="$ID.$VERSION_ID-$__BuildArch"
            fi
        fi
    else
        export __DistroRid="$__HostDistroRid"
    fi

    if [ "$__BuildOS" == "OSX" ]; then
        __PortableBuild=1
    fi

    # Portable builds target the base RID
    if [ "$__PortableBuild" == 1 ]; then
        if [ "$__BuildOS" == "Linux" ]; then
            export __DistroRid="linux-$__BuildArch"
        elif [ "$__BuildOS" == "OSX" ]; then
            export __DistroRid="osx-$__BuildArch"
        fi
    fi

   if [ "$ID.$VERSION_ID" == "ubuntu.16.04" ]; then
     export __DistroRid="ubuntu.14.04-$__BuildArch"
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

    build_Tests_internal "Restore_Product" "${__ProjectDir}/tests/build.proj" " -BatchRestorePackages" "Restore product binaries (build tests)"

    build_Tests_internal "Tests_GenerateRuntimeLayout" "${__ProjectDir}/tests/runtest.proj" "-BinPlaceRef -BinPlaceProduct -CopyCrossgenToProduct" "Restore product binaries (run tests)"

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up=-updateinvalidpackageversion
    fi

    # Work hardcoded path around
    if [ ! -f "${__BuildToolsDir}/Microsoft.CSharp.Core.Targets" ]; then
        ln -s "${__BuildToolsDir}/Microsoft.CSharp.Core.targets" "${__BuildToolsDir}/Microsoft.CSharp.Core.Targets"
    fi
    if [ ! -f "${__BuildToolsDir}/Microsoft.CSharp.targets" ]; then
        ln -s "${__BuildToolsDir}/Microsoft.CSharp.Targets" "${__BuildToolsDir}/Microsoft.CSharp.targets"
    fi

    echo "Starting the Managed Tests Build..."

    __ManagedTestBuiltMarker=${__TestBinDir}/managed_test_build

    if [ ! -f $__ManagedTestBuiltMarker ]; then

	    build_Tests_internal "Tests_Managed" "$__ProjectDir/tests/build.proj" "$__up" "Managed tests build (build tests)"

        if [ $? -ne 0 ]; then
            echo "${__MsgPrefix}Error: build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "Tests have been built."
            echo "Create marker \"${__ManagedTestBuiltMarker}\""
            touch $__ManagedTestBuiltMarker
        fi
    else
        echo "Managed Tests had been built before."
    fi

    if [ $__BuildTestWrappers -ne -0 ]; then
        echo "${__MsgPrefix}Creating test wrappers..."

        __XUnitWrapperBuiltMarker=${__TestBinDir}/xunit_wrapper_build

        if [ ! -f $__XUnitWrapperBuiltMarker ]; then

            build_Tests_internal "Tests_XunitWrapper" "$__ProjectDir/tests/runtest.proj" "-BuildWrappers -MsBuildEventLogging=\" \" " "Test Xunit Wrapper"

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

    echo "${__MsgPrefix}Creating test overlay..."

    if [ -z "$XuintTestBinBase" ]; then
      XuintTestBinBase=$__TestWorkingDir
    fi

    export CORE_ROOT=$XuintTestBinBase/Tests/Core_Root

    if [ ! -f "${CORE_ROOT}" ]; then
      mkdir -p $CORE_ROOT
    else
      rm -rf $CORE_ROOT/*
    fi

    cp -r $__BinDir/* $CORE_ROOT/ > /dev/null

    build_Tests_internal "Tests_Overlay_Managed" "$__ProjectDir/tests/runtest.proj" "-testOverlay" "Creating test overlay"

    if [ $__ZipTests -ne 0 ]; then
        echo "${__MsgPrefix}ZIP tests packages..."
        build_Tests_internal "Helix_Prep" "$__ProjectDir/tests/helixprep.proj" " " "Prep test binaries for Helix publishing"
    fi
}

build_Tests_internal()
{
	subDirectoryName=$1
	projectName=$2
	extraBuildParameters=$3
	stepName="$4"

	# Set up directories and file names
	__BuildLogRootName=$subDirectoryName
    __BuildLog="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.log"
    __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.wrn"
    __BuildErr="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.err"
    __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog}\""
    __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn}\""
    __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr}\""

    # Generate build command
    buildCommand="$__ProjectRoot/run.sh build -Project=$projectName -MsBuildLog=${__msbuildLog} -MsBuildWrn=${__msbuildWrn} -MsBuildErr=${__msbuildErr} $extraBuildParameters $__RunArgs $__UnprocessedBuildArgs"

    echo "Building step '$stepName' via $buildCommand"

    # Invoke MSBuild
    eval $buildCommand

    # Invoke MSBuild
    # $__ProjectRoot/run.sh build -Project=$projectName -MsBuildLog="$__msbuildLog" -MsBuildWrn="$__msbuildWrn" -MsBuildErr="$__msbuildErr" $extraBuildParameters $__RunArgs $__UnprocessedBuildArgs

    # Make sure everything is OK
    if [ $? -ne 0 ]; then
        echo "${__MsgPrefix}Failed to build $stepName. See the build logs:"
        echo "    $__BuildLog"
        echo "    $__BuildWrn"
        echo "    $__BuildErr"
        exit 1
    fi
}

usage()
{
    echo "Usage: $0 [BuildArch] [BuildType] [verbose] [coverage] [cross] [clangx.y] [ninja] [runtests] [bindir]"
    echo "BuildArch can be: x64, x86, arm, armel, arm64"
    echo "BuildType can be: debug, checked, release"
    echo "coverage - optional argument to enable code coverage build (currently supported only for Linux and OSX)."
    echo "ninja - target ninja instead of GNU make"
    echo "clangx.y - optional argument to build using clang version x.y."
    echo "cross - optional argument to signify cross compilation,"
    echo "      - will use ROOTFS_DIR environment variable if set."
    echo "crosscomponent - optional argument to build cross-architecture component,"
    echo "               - will use CAC_ROOTFS_DIR environment variable if set."
    echo "portableLinux - build for Portable Linux Distribution"
    echo "verbose - optional argument to enable verbose build output."
    echo "rebuild - if tests have already been built - rebuild them"
    echo "runtests - run tests after building them"
    echo "ziptests - zips CoreCLR tests & Core_Root for a Helix run"
    echo "bindir - output directory (defaults to $__ProjectRoot/bin)"
    echo "msbuildonunsupportedplatform - build managed binaries even if distro is not officially supported."
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
__DistroRid=""
__cmakeargs=""
__PortableLinux=0
__msbuildonunsupportedplatform=0
__ZipTests=0
__NativeTestIntermediatesDir=
__RunTests=0
__RebuildTests=0
__BuildTestWrappers=0
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

        clang3.5)
            __ClangMajorVersion=3
            __ClangMinorVersion=5
            ;;

        clang3.6)
            __ClangMajorVersion=3
            __ClangMinorVersion=6
            ;;

        clang3.7)
            __ClangMajorVersion=3
            __ClangMinorVersion=7
            ;;

        clang3.8)
            __ClangMajorVersion=3
            __ClangMinorVersion=8
            ;;

        clang3.9)
            __ClangMajorVersion=3
            __ClangMinorVersion=9
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
        *)
            __UnprocessedBuildArgs="$__UnprocessedBuildArgs $1"
            ;;
    esac

    shift
done


__RunArgs="-BuildArch=$__BuildArch -BuildType=$__BuildType -BuildOS=$__BuildOS"

# Configure environment if we are doing a verbose build
if [ $__VerboseBuild == 1 ]; then
    export VERBOSE=1
	__RunArgs="$__RunArgs -verbose"
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

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to set.
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

if [ ! -d "$__BinDir" ] || [ ! -d "$__BinDir/bin" ]; then

    echo "Has not been found built CoreCLR instance"
    echo "Please build it before tests using './build.sh $__BuildArch $__BuildType'"
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

echo "Building Tests..."

build_Tests

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
    echo "To run single test use the following command:"
    echo "    bash ${__TestBinDir}/__TEST_PATH__/__TEST_NAME__.sh -coreroot=${CORE_ROOT}"
fi

