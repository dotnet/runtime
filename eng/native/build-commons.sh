#!/usr/bin/env bash

initTargetDistroRid()
{
    source "$__RepoRootDir/eng/native/init-distro-rid.sh"

    local passedRootfsDir=""

    # Only pass ROOTFS_DIR if cross is specified.
    if [[ "$__CrossBuild" == 1 ]]; then
        passedRootfsDir="$ROOTFS_DIR"
    fi

    initDistroRidGlobal "$__BuildOS" "$__BuildArch" "$__PortableBuild" "$passedRootfsDir"
}

isMSBuildOnNETCoreSupported()
{
    __IsMSBuildOnNETCoreSupported="$__msbuildonunsupportedplatform"

    if [[ "$__IsMSBuildOnNETCoreSupported" == 1 ]]; then
        return
    fi

    if [[ "$__SkipManaged" == 1 ]]; then
        __IsMSBuildOnNETCoreSupported=0
        return
    fi

    if [[ ( "$__HostOS" == "Linux" )  && ( "$__HostArch" == "x64" || "$__HostArch" == "arm" || "$__HostArch" == "armel" || "$__HostArch" == "arm64" ) ]]; then
        __IsMSBuildOnNETCoreSupported=1
    elif [[ ( "$__HostOS" == "OSX" || "$__HostOS" == "FreeBSD" ) && "$__HostArch" == "x64" ]]; then
        __IsMSBuildOnNETCoreSupported=1
    fi
}

setup_dirs()
{
    echo Setting up directories for build

    mkdir -p "$__RootBinDir"
    mkdir -p "$__BinDir"
    mkdir -p "$__IntermediatesDir"
}

# Check the system to ensure the right prereqs are in place
check_prereqs()
{
    echo "Checking prerequisites..."

    # Check presence of CMake on the path
    command -v cmake 2>/dev/null || { echo >&2 "Please install cmake before running this script"; exit 1; }

    function version { echo "$@" | awk -F. '{ printf("%d%02d%02d\n", $1,$2,$3); }'; }

    local cmake_version="$(cmake --version | grep -Eo "[0-9]+\.[0-9]+\.[0-9]+")"

    if [[ "$(version "$cmake_version")" -lt "$(version 3.14.2)" ]]; then
        echo "Please install CMake 3.14.2 or newer from http://www.cmake.org/download/ or https://apt.kitware.com and ensure it is on your path."; exit 1;
    fi

    if [[ "$__UseNinja" == 1 ]]; then
        command -v ninja 2>/dev/null || command -v ninja-build 2>/dev/null || { echo "Unable to locate ninja!"; exit 1; }
    fi
}

build_native()
{
    platformArch="$1"
    cmakeDir="$2"
    tryrunDir="$3"
    intermediatesDir="$4"
    message="$5"

    # All set to commence the build
    echo "Commencing build of \"$message\" for $__BuildOS.$__BuildArch.$__BuildType in $intermediatesDir"

    if [[ "$__UseNinja" == 1 ]]; then
        generator="ninja"
        buildTool="$(command -v ninja || command -v ninja-build)"
    else
        buildTool="make"
    fi

    if [[ "$__SkipConfigure" == 0 ]]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [[ "$__IsMSBuildOnNETCoreSupported" == 0 ]]; then __SkipGenerateVersion=1; fi
        # Drop version.c file
        __versionSourceFile="$intermediatesDir/version.c"
        if [[ "$__SkipGenerateVersion" == 0 ]]; then
            "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary "$__ArcadeScriptArgs" "$__RepoRootDir"/eng/empty.csproj \
                                                   /p:NativeVersionFile="$__versionSourceFile" \
                                                   /t:GenerateNativeVersionFile /restore \
                                                   $__CommonMSBuildArgs $__UnprocessedBuildArgs
            local exit_code="$?"
            if [[ "$exit_code" != 0 ]]; then
                echo "${__ErrMsgPrefix}Failed to generate native version file."
                exit "$exit_code"
            fi
        else
            # Generate the dummy version.c, but only if it didn't exist to make sure we don't trigger unnecessary rebuild
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            if [[ -e "$__versionSourceFile" ]]; then
                read existingVersionSourceLine < "$__versionSourceFile"
            fi
            if [[ "$__versionSourceLine" != "$existingVersionSourceLine" ]]; then
                echo "$__versionSourceLine" > "$__versionSourceFile"
            fi
        fi

        if [[ "$__StaticAnalyzer" == 1 ]]; then
            scan_build=scan-build
        fi

        engNativeDir="$__RepoRootDir/eng/native"
        __CMakeArgs="-DCLR_ENG_NATIVE_DIR=\"$engNativeDir\" $__CMakeArgs"
        nextCommand="\"$engNativeDir/gen-buildsys.sh\" \"$cmakeDir\" \"$tryrunDir\" \"$intermediatesDir\" $platformArch $__Compiler \"$__CompilerMajorVersion\" \"$__CompilerMinorVersion\" $__BuildType \"$generator\" $scan_build $__CMakeArgs"
        echo "Invoking $nextCommand"
        eval $nextCommand

        local exit_code="$?"
        if [[ "$exit_code" != 0  ]]; then
            echo "${__ErrMsgPrefix}Failed to generate \"$message\" build project!"
            exit "$exit_code"
        fi
    fi

    # Check that the makefiles were created.
    if [[ ! -f "$intermediatesDir/CMakeCache.txt" ]]; then
        echo "${__ErrMsgPrefix}Unable to find generated build files for \"$message\" project!"
        exit 1
    fi

    # Build
    if [[ "$__ConfigureOnly" == 1 ]]; then
        echo "Finish configuration & skipping \"$message\" build."
        return
    fi

    if [[ "$__StaticAnalyzer" == 1 ]]; then
        pushd "$intermediatesDir"

        buildTool="$SCAN_BUILD_COMMAND -o $__BinDir/scan-build-log $buildTool"
        echo "Executing $buildTool install -j $__NumProc"
        "$buildTool" install -j "$__NumProc"

        popd
    else
        echo "Executing cmake --build \"$intermediatesDir\" --target install -j $__NumProc"
        cmake --build "$intermediatesDir" --target install -j "$__NumProc"
    fi

    local exit_code="$?"
    if [[ "$exit_code" != 0 ]]; then
        echo "${__ErrMsgPrefix}Failed to build \"$message\"."
        exit "$exit_code"
    fi
}

usage()
{
    echo "Usage: $0 <options>"
    echo ""
    echo "Common Options:"
    echo ""
    echo "BuildArch can be: -arm, -armel, -arm64, -armel, x64, x86, -wasm"
    echo "BuildType can be: -debug, -checked, -release"
    echo "-bindir: output directory (defaults to $__ProjectRoot/artifacts)"
    echo "-ci: indicates if this is a CI build."
    echo "-clang: optional argument to build using clang in PATH (default)."
    echo "-clangx.y: optional argument to build using clang version x.y."
    echo "-cmakeargs: user-settable additional arguments passed to CMake."
    echo "-configureonly: do not perform any builds; just configure the build."
    echo "-cross: optional argument to signify cross compilation,"
    echo "        will use ROOTFS_DIR environment variable if set."
    echo "-gcc: optional argument to build using gcc in PATH."
    echo "-gccx.y: optional argument to build using gcc version x.y."
    echo "-msbuildonunsupportedplatform: build managed binaries even if distro is not officially supported."
    echo "-ninja: target ninja instead of GNU make"
    echo "-numproc: set the number of build processes."
    echo "-portablebuild: pass -portablebuild=false to force a non-portable build."
    echo "-skipconfigure: skip build configuration."
    echo "-skipgenerateversion: disable version generation even if MSBuild is supported."
    echo "-stripsymbols: skip native image generation."
    echo "-verbose: optional argument to enable verbose build output."
    echo ""
    echo "Additional Options:"
    echo ""
    for i in "${!usage_list[@]}"; do
        echo "${usage_list[${i}]}"
    done
    echo ""
    exit 1
}

# Use uname to determine what the CPU is.
CPUName=$(uname -p)

# Some Linux platforms report unknown for platform, but the arch for machine.
if [[ "$CPUName" == "unknown" ]]; then
    CPUName=$(uname -m)
fi

case "$CPUName" in
    aarch64)
        __BuildArch=arm64
        __HostArch=arm64
        ;;

    amd64)
        __BuildArch=x64
        __HostArch=x64
        ;;

    armv7l)
        if (NAME=""; . /etc/os-release; test "$NAME" = "Tizen"); then
            __BuildArch=armel
            __HostArch=armel
        else
            __BuildArch=arm
            __HostArch=arm
        fi
        ;;

    i686)
        echo "Unsupported CPU $CPUName detected, build might not succeed!"
        __BuildArch=x86
        __HostArch=x86
        ;;

    x86_64)
        __BuildArch=x64
        __HostArch=x64
        ;;

    *)
        echo "Unknown CPU $CPUName detected, configuring as if for x64"
        __BuildArch=x64
        __HostArch=x64
        ;;
esac

# Use uname to determine what the OS is.
OSName=$(uname -s)
case "$OSName" in
    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
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

__msbuildonunsupportedplatform=0

while :; do
    if [[ "$#" -le 0 ]]; then
        break
    fi

    lowerI="$(echo "$1" | awk '{print tolower($0)}')"
    case "$lowerI" in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        arm|-arm)
            __BuildArch=arm
            ;;

        arm64|-arm64)
            __BuildArch=arm64
            ;;

        armel|-armel)
            __BuildArch=armel
            ;;

        bindir|-bindir)
            if [[ -n "$2" ]]; then
                __RootBinDir="$2"
                if [[ ! -d "$__RootBinDir" ]]; then
                    mkdir "$__RootBinDir"
                fi
                __RootBinParent=$(dirname "$__RootBinDir")
                __RootBinName="${__RootBinDir##*/}"
                __RootBinDir="$(cd "$__RootBinParent" &>/dev/null && printf %s/%s "$PWD" "$__RootBinName")"
                shift
            else
                echo "ERROR: 'bindir' requires a non-empty option argument"
                exit 1
            fi
            ;;

        checked|-checked)
            __BuildType=Checked
            ;;

        ci|-ci)
            __ArcadeScriptArgs="--ci"
            __ErrMsgPrefix="##vso[task.logissue type=error]"
            ;;

        clang*|-clang*)
                __Compiler=clang
                # clangx.y or clang-x.y
                version="$(echo "$lowerI" | tr -d '[:alpha:]-=')"
                parts=(${version//./ })
                __CompilerMajorVersion="${parts[0]}"
                __CompilerMinorVersion="${parts[1]}"
                if [[ -z "$__CompilerMinorVersion" && "$__CompilerMajorVersion" -le 6 ]]; then
                    __CompilerMinorVersion=0;
                fi
            ;;

        cmakeargs|-cmakeargs)
            if [[ -n "$2" ]]; then
                __CMakeArgs="$2 $__CMakeArgs"
                shift
            else
                echo "ERROR: 'cmakeargs' requires a non-empty option argument"
                exit 1
            fi
            ;;

        configureonly|-configureonly)
            __ConfigureOnly=1
            __SkipMSCorLib=1
            __SkipNuget=1
            ;;

        cross|-cross)
            __CrossBuild=1
            ;;

        debug|-debug)
            __BuildType=Debug
            ;;

        gcc*|-gcc*)
                __Compiler=gcc
                # gccx.y or gcc-x.y
                version="$(echo "$lowerI" | tr -d '[:alpha:]-=')"
                parts=(${version//./ })
                __CompilerMajorVersion="${parts[0]}"
                __CompilerMinorVersion="${parts[1]}"
            ;;

        msbuildonunsupportedplatform|-msbuildonunsupportedplatform)
            __msbuildonunsupportedplatform=1
            ;;

        ninja|-ninja)
            __UseNinja=1
            ;;

        numproc|-numproc)
            if [[ -n "$2" ]]; then
              __NumProc="$2"
              shift
            else
              echo "ERROR: 'numproc' requires a non-empty option argument"
              exit 1
            fi
            ;;

        portablebuild=false|-portablebuild=false)
            __PortableBuild=0
            ;;

        release|-release)
            __BuildType=Release
            ;;

        skipconfigure|-skipconfigure)
            __SkipConfigure=1
            ;;

        skipgenerateversion|-skipgenerateversion)
            __SkipGenerateVersion=1
            ;;

        stripsymbols|-stripsymbols)
            __CMakeArgs="-DSTRIP_SYMBOLS=true $__CMakeArgs"
            ;;

        verbose|-verbose)
            __VerboseBuild=1
            ;;

        x86|-x86)
            __BuildArch=x86
            ;;

        x64|-x64)
            __BuildArch=x64
            ;;

        wasm|-wasm)
            __BuildArch=wasm
            ;;

        *)
            handle_arguments "$1" "$2"
            if [[ "$__ShiftArgs" == 1 ]]; then
                shift
                __ShiftArgs=0
            fi
            ;;
    esac

    shift
done

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
platform=$(uname)
if [[ "$platform" == "FreeBSD" ]]; then
  __NumProc=$(sysctl hw.ncpu | awk '{ print $2+1 }')
elif [[ "$platform" == "NetBSD" ]]; then
  __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
elif [[ "$platform" == "Darwin" ]]; then
  __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
else
  __NumProc=$(nproc --all)
fi

__CommonMSBuildArgs="/p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__BuildOS=$__BuildOS /nodeReuse:false $__OfficialBuildIdArg $__SignTypeArg $__SkipRestoreArg"

# Configure environment if we are doing a verbose build
if [[ "$__VerboseBuild" == 1 ]]; then
    VERBOSE=1
    export VERBOSE
    __CommonMSBuildArgs="$__CommonMSBuildArgs /v:detailed"
fi

if [[ "$__PortableBuild" == 0 ]]; then
    __CommonMSBuildArgs="$__CommonMSBuildArgs /p:PortableBuild=false"
fi

# Configure environment if we are doing a cross compile.
if [[ "$__CrossBuild" == 1 ]]; then
    CROSSCOMPILE=1
    export CROSSCOMPILE
    if [[ ! -n "$ROOTFS_DIR" ]]; then
        ROOTFS_DIR="$__RepoRootDir/.tools/rootfs/$__BuildArch"
        export ROOTFS_DIR
    fi
fi

# init the target distro name
initTargetDistroRid

# Init if MSBuild for .NET Core is supported for this platform
isMSBuildOnNETCoreSupported
