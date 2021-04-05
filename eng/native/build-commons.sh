#!/usr/bin/env bash

initTargetDistroRid()
{
    source "$__RepoRootDir/eng/native/init-distro-rid.sh"

    local passedRootfsDir=""

    # Only pass ROOTFS_DIR if cross is specified and the target platform is not Darwin that doesn't use rootfs
    if [[ "$__CrossBuild" == 1 && "$platform" != "Darwin" ]]; then
        passedRootfsDir="$ROOTFS_DIR"
    fi

    initDistroRidGlobal "$__TargetOS" "$__BuildArch" "$__PortableBuild" "$passedRootfsDir"
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

    if [[ "$__HostOS" == "OSX" ]]; then
        # Check presence of pkg-config on the path
        command -v pkg-config 2>/dev/null || { echo >&2 "Please install pkg-config before running this script, see https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/macos-requirements.md"; exit 1; }

        if ! pkg-config openssl ; then
            # We export the proper PKG_CONFIG_PATH where openssl was installed by Homebrew
            # It's important to _export_ it since build-commons.sh is sourced by other scripts such as build-native.sh
            export PKG_CONFIG_PATH=$(brew --prefix)/opt/openssl@1.1/lib/pkgconfig:$(brew --prefix)/opt/openssl/lib/pkgconfig
            # We try again with the PKG_CONFIG_PATH in place, if pkg-config still can't find OpenSSL, exit with an error, cmake won't find OpenSSL either
            pkg-config openssl || { echo >&2 "Please install openssl before running this script, see https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/macos-requirements.md"; exit 1; }
        fi
    fi

    if [[ "$__UseNinja" == 1 ]]; then
        command -v ninja 2>/dev/null || command -v ninja-build 2>/dev/null || { echo "Unable to locate ninja!"; exit 1; }
    fi
}

build_native()
{
    targetOS="$1"
    platformArch="$2"
    cmakeDir="$3"
    intermediatesDir="$4"
    target="$5"
    cmakeArgs="$6"
    message="$7"

    # All set to commence the build
    echo "Commencing build of \"$target\" target in \"$message\" for $__TargetOS.$__BuildArch.$__BuildType in $intermediatesDir"

    if [[ "$targetOS" == OSX || "$targetOS" == MacCatalyst ]]; then
        if [[ "$platformArch" == x64 ]]; then
            cmakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $cmakeArgs"
        elif [[ "$platformArch" == arm64 ]]; then
            cmakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $cmakeArgs"
        else
            echo "Error: Unknown OSX architecture $platformArch."
            exit 1
        fi
    fi

    if [[ "$targetOS" == MacCatalyst ]]; then
        cmakeArgs="-DCLR_CMAKE_TARGET_MACCATALYST=1 $cmakeArgs"
    fi

    if [[ "$__UseNinja" == 1 ]]; then
        generator="ninja"
        buildTool="$(command -v ninja || command -v ninja-build)"
    else
        buildTool="make"
    fi

    runtimeVersionHeaderFile="$intermediatesDir/../runtime_version.h"
    if [[ "$__SkipConfigure" == 0 ]]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [[ "$__IsMSBuildOnNETCoreSupported" == 0 ]]; then __SkipGenerateVersion=1; fi
        # Drop version.c file
        __versionSourceFile="$intermediatesDir/version.c"

        if [[ ! -z "${__LogsDir}" ]]; then
            __binlogArg="-bl:\"$__LogsDir/GenNativeVersion_$__TargetOS.$__BuildArch.$__BuildType.binlog\""
        fi

        if [[ "$__SkipGenerateVersion" == 0 ]]; then
            "$__RepoRootDir/eng/common/msbuild.sh" /clp:nosummary "$__ArcadeScriptArgs" "$__RepoRootDir"/eng/empty.csproj \
                                                   /p:NativeVersionFile="$__versionSourceFile" \
                                                   /p:RuntimeVersionFile="$runtimeVersionHeaderFile" \
                                                   /t:GenerateRuntimeVersionFile /restore \
                                                   $__CommonMSBuildArgs $__binlogArg $__UnprocessedBuildArgs
            local exit_code="$?"
            if [[ "$exit_code" != 0 ]]; then
                echo "${__ErrMsgPrefix}Failed to generate native version file."
                exit "$exit_code"
            fi
        else
            # Generate the dummy version.c and runtime_version.h, but only if they didn't exist to make sure we don't trigger unnecessary rebuild
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            if [[ -e "$__versionSourceFile" ]]; then
                read existingVersionSourceLine < "$__versionSourceFile"
            fi
            if [[ "$__versionSourceLine" != "$existingVersionSourceLine" ]]; then
                cat << EOF > $runtimeVersionHeaderFile
#define RuntimeAssemblyMajorVersion 0
#define RuntimeAssemblyMinorVersion 0
#define RuntimeFileMajorVersion 0
#define RuntimeFileMinorVersion 0
#define RuntimeFileBuildVersion 0
#define RuntimeFileRevisionVersion 0
#define RuntimeProductMajorVersion 0
#define RuntimeProductMinorVersion 0
#define RuntimeProductPatchVersion 0
#define RuntimeProductVersion
EOF
                echo "$__versionSourceLine" > "$__versionSourceFile"
            fi
        fi

        if [[ "$__StaticAnalyzer" == 1 ]]; then
            scan_build=scan-build
        fi

        nextCommand="\"$__RepoRootDir/eng/native/gen-buildsys.sh\" \"$cmakeDir\" \"$intermediatesDir\" $platformArch $__Compiler \"$__CompilerMajorVersion\" \"$__CompilerMinorVersion\" $__BuildType \"$generator\" $scan_build $cmakeArgs"
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

    SAVED_CFLAGS="${CFLAGS}"
    SAVED_CXXFLAGS="${CXXFLAGS}"
    SAVED_LDFLAGS="${LDFLAGS}"

    # Let users provide additional compiler/linker flags via EXTRA_CFLAGS/EXTRA_CXXFLAGS/EXTRA_LDFLAGS.
    # If users directly override CFLAG/CXXFLAGS/LDFLAGS, that may lead to some configure tests working incorrectly.
    # See https://github.com/dotnet/runtime/issues/35727 for more information.
    export CFLAGS="${CFLAGS} ${EXTRA_CFLAGS}"
    export CXXFLAGS="${CXXFLAGS} ${EXTRA_CXXFLAGS}"
    export LDFLAGS="${LDFLAGS} ${EXTRA_LDFLAGS}"

    local exit_code
    if [[ "$__StaticAnalyzer" == 1 ]]; then
        pushd "$intermediatesDir"

        buildTool="$SCAN_BUILD_COMMAND -o $__BinDir/scan-build-log $buildTool"
        echo "Executing $buildTool $target -j $__NumProc"
        "$buildTool" $target -j "$__NumProc"
        exit_code="$?"

        popd
    else
        cmake_command=cmake
        if [[ "$build_arch" == "wasm" ]]; then
            cmake_command="emcmake cmake"
            echo "Executing $cmake_command --build \"$intermediatesDir\" --target $target -- -j $__NumProc"
            $cmake_command --build "$intermediatesDir" --target $target -- -j "$__NumProc"
            exit_code="$?"
        else
            # For non-wasm Unix scenarios, we may have to use an old version of CMake that doesn't support
            # multiple targets. Instead, directly invoke the build tool to build multiple targets in one invocation.
            pushd "$intermediatesDir"

            echo "Executing $buildTool $target -j $__NumProc"
            "$buildTool" $target -j "$__NumProc"
            exit_code="$?"

            popd
        fi
    fi

    CFLAGS="${SAVED_CFLAGS}"
    CXXFLAGS="${SAVED_CXXFLAGS}"
    LDFLAGS="${SAVED_LDFLAGS}"

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
    echo "BuildArch can be: -arm, -armel, -arm64, x64, x86, -wasm"
    echo "BuildType can be: -debug, -checked, -release"
    echo "-os: target OS (defaults to running OS)"
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
    echo "-keepnativesymbols: keep native/unmanaged debug symbols."
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

source "$__RepoRootDir/eng/native/init-os-and-arch.sh"

__BuildArch=$arch
__HostArch=$arch
__TargetOS=$os
__HostOS=$os
__BuildOS=$os

__msbuildonunsupportedplatform=0

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
platform="$(uname)"
if [[ "$platform" == "FreeBSD" ]]; then
  __NumProc=$(($(sysctl -n hw.ncpu)+1))
elif [[ "$platform" == "NetBSD" || "$platform" == "SunOS" ]]; then
  __NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
elif [[ "$platform" == "Darwin" ]]; then
  __NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
else
  __NumProc=$(nproc --all)
fi

while :; do
    if [[ "$#" -le 0 ]]; then
        break
    fi

    lowerI="$(echo "$1" | tr "[:upper:]" "[:lower:]")"
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

        keepnativesymbols|-keepnativesymbols)
            __CMakeArgs="$__CMakeArgs -DCLR_CMAKE_KEEP_NATIVE_SYMBOLS=true"
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

        os|-os)
            if [[ -n "$2" ]]; then
                __TargetOS="$2"
                shift
            else
                echo "ERROR: 'os' requires a non-empty option argument"
                exit 1
            fi
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

__CommonMSBuildArgs="/p:TargetArchitecture=$__BuildArch /p:Configuration=$__BuildType /p:TargetOS=$__TargetOS /nodeReuse:false $__OfficialBuildIdArg $__SignTypeArg $__SkipRestoreArg"

# Configure environment if we are doing a verbose build
if [[ "$__VerboseBuild" == 1 ]]; then
    VERBOSE=1
    export VERBOSE
    __CommonMSBuildArgs="$__CommonMSBuildArgs /v:detailed"
fi

if [[ "$__PortableBuild" == 0 ]]; then
    __CommonMSBuildArgs="$__CommonMSBuildArgs /p:PortableBuild=false"
fi

if [[ "$__BuildArch" == wasm ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == iOS || "$__TargetOS" == iOSSimulator ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == tvOS || "$__TargetOS" == tvOSSimulator ]]; then
    # nothing to do here
    true
elif [[ "$__TargetOS" == Android ]]; then
    # nothing to do here
    true
else
    __CMakeArgs="-DFEATURE_DISTRO_AGNOSTIC_SSL=$__PortableBuild $__CMakeArgs"
fi

# Configure environment if we are doing a cross compile.
if [[ "$__CrossBuild" == 1 ]]; then
    CROSSCOMPILE=1
    export CROSSCOMPILE
    # Darwin that doesn't use rootfs
    if [[ ! -n "$ROOTFS_DIR" && "$platform" != "Darwin" ]]; then
        ROOTFS_DIR="$__RepoRootDir/.tools/rootfs/$__BuildArch"
        export ROOTFS_DIR
    fi
fi

# init the target distro name
initTargetDistroRid

# Init if MSBuild for .NET Core is supported for this platform
isMSBuildOnNETCoreSupported
