#!/usr/bin/env bash

initTargetDistroRid()
{
    source "$__RepoRootDir/eng/native/init-distro-rid.sh"

    local passedRootfsDir=""

    # Only pass ROOTFS_DIR if cross is specified and the target platform is not Darwin that doesn't use rootfs
    if [[ "$__CrossBuild" == 1 && "$platform" != "Darwin" ]]; then
        passedRootfsDir="$ROOTFS_DIR"
    fi

    initDistroRidGlobal "$__TargetOS" "$__TargetArch" "$__PortableBuild" "$passedRootfsDir"
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
            export PKG_CONFIG_PATH=$(brew --prefix)/opt/openssl@3/lib/pkgconfig:$(brew --prefix)/opt/openssl@1.1/lib/pkgconfig:$(brew --prefix)/opt/openssl/lib/pkgconfig
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
    if [[ ! -e "$__RepoRootDir/artifacts/obj/_version.c" ]]; then
        eval "$__RepoRootDir/eng/native/version/copy_version_files.sh"
    fi

    targetOS="$1"
    hostArch="$2"
    cmakeDir="$3"
    intermediatesDir="$4"
    target="$5"
    cmakeArgs="$6"
    message="$7"

    # All set to commence the build
    echo "Commencing build of \"$target\" target in \"$message\" for $__TargetOS.$__TargetArch.$__BuildType in $intermediatesDir"

    if [[ "$targetOS" == OSX || "$targetOS" == MacCatalyst ]]; then
        if [[ "$hostArch" == x64 ]]; then
            cmakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"x86_64\" $cmakeArgs"
        elif [[ "$hostArch" == arm64 ]]; then
            cmakeArgs="-DCMAKE_OSX_ARCHITECTURES=\"arm64\" $cmakeArgs"
        else
            echo "Error: Unknown OSX architecture $hostArch."
            exit 1
        fi
    fi

    if [[ "$targetOS" == MacCatalyst ]]; then
        cmakeArgs="-DCMAKE_SYSTEM_VARIANT=MacCatalyst $cmakeArgs"
    fi

    if [[ ( "$targetOS" == Android || "$targetOS" == linux-bionic ) && -z "$ROOTFS_DIR" ]]; then
        if [[ -z "$ANDROID_NDK_ROOT" ]]; then
            echo "Error: You need to set the ANDROID_NDK_ROOT environment variable pointing to the Android NDK root."
            exit 1
        fi

        # keep ANDROID_PLATFORM in sync with src/mono/Directory.Build.props
        cmakeArgs="-DCMAKE_TOOLCHAIN_FILE=$ANDROID_NDK_ROOT/build/cmake/android.toolchain.cmake -DANDROID_PLATFORM=android-21 $cmakeArgs"

        # Don't try to set CC/CXX in init-compiler.sh - it's handled in android.toolchain.cmake already
        __Compiler="default"

        if [[ "$hostArch" == x64 ]]; then
            cmakeArgs="-DANDROID_ABI=x86_64 $cmakeArgs"
        elif [[ "$hostArch" == x86 ]]; then
            cmakeArgs="-DANDROID_ABI=x86 $cmakeArgs"
        elif [[ "$hostArch" == arm64 ]]; then
            cmakeArgs="-DANDROID_ABI=arm64-v8a $cmakeArgs"
        elif [[ "$hostArch" == arm ]]; then
            cmakeArgs="-DANDROID_ABI=armeabi-v7a $cmakeArgs"
        else
            echo "Error: Unknown Android architecture $hostArch."
            exit 1
        fi
    fi

    if [[ "$__UseNinja" == 1 ]]; then
        generator="ninja"
        buildTool="$(command -v ninja || command -v ninja-build)"
    else
        buildTool="make"
    fi

    if [[ "$__SkipConfigure" == 0 ]]; then

        if [[ "$__StaticAnalyzer" == 1 ]]; then
            scan_build=scan-build
        fi

        nextCommand="\"$__RepoRootDir/eng/native/gen-buildsys.sh\" \"$cmakeDir\" \"$intermediatesDir\" $hostArch $__Compiler $__BuildType \"$generator\" $scan_build $cmakeArgs"
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
    echo "BuildArch can be: -arm, -armv6, -armel, -arm64, -loongarch64, -riscv64, -s390x, -ppc64le, x64, x86, -wasm"
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
    echo "-ninja: target ninja instead of GNU make"
    echo "-numproc: set the number of build processes."
    echo "-portablebuild: pass -portablebuild=false to force a non-portable build."
    echo "-skipconfigure: skip build configuration."
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

__TargetArch=$arch
__TargetOS=$os
__HostOS=$os
__BuildOS=$os

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
  if command -v nproc > /dev/null 2>&1; then
    __NumProc=$(nproc --all)
  elif (NAME=""; . /etc/os-release; test "$NAME" = "Tizen"); then
    __NumProc=$(getconf _NPROCESSORS_ONLN)
  else
    __NumProc=1
  fi
fi

while :; do
    if [[ "$#" -le 0 ]]; then
        break
    fi

    lowerI="$(echo "${1/--/-}" | tr "[:upper:]" "[:lower:]")"
    case "$lowerI" in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        arm|-arm)
            __TargetArch=arm
            ;;

        armv6|-armv6)
            __TargetArch=armv6
            ;;

        arm64|-arm64)
            __TargetArch=arm64
            ;;

        armel|-armel)
            __TargetArch=armel
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
            __Compiler="$lowerI"
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
            __Compiler="$lowerI"
            ;;

        keepnativesymbols|-keepnativesymbols)
            __CMakeArgs="$__CMakeArgs -DCLR_CMAKE_KEEP_NATIVE_SYMBOLS=true"
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

        verbose|-verbose)
            __VerboseBuild=1
            ;;

        x86|-x86)
            __TargetArch=x86
            ;;

        x64|-x64)
            __TargetArch=x64
            ;;

        loongarch64|-loongarch64)
            __TargetArch=loongarch64
            ;;

        riscv64|-riscv64)
            __TargetArch=riscv64
            ;;

        s390x|-s390x)
            __TargetArch=s390x
            ;;

        wasm|-wasm)
            __TargetArch=wasm
            ;;

        ppc64le|-ppc64le)
            __TargetArch=ppc64le
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

        hostarch|-hostarch)
            if [[ -n "$2" ]]; then
                __HostArch="$2"
                shift
            else
                echo "ERROR: 'hostarch' requires a non-empty option argument"
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

if [[ -z "$__HostArch" ]]; then
    __HostArch=$__TargetArch
fi

__CommonMSBuildArgs="/p:TargetArchitecture=$__TargetArch /p:Configuration=$__BuildType /p:TargetOS=$__TargetOS /nodeReuse:false $__OfficialBuildIdArg $__SignTypeArg $__SkipRestoreArg"

# Configure environment if we are doing a verbose build
if [[ "$__VerboseBuild" == 1 ]]; then
    VERBOSE=1
    export VERBOSE
    __CommonMSBuildArgs="$__CommonMSBuildArgs /v:detailed"
fi

if [[ "$__PortableBuild" == 0 ]]; then
    __CommonMSBuildArgs="$__CommonMSBuildArgs /p:PortableBuild=false"
fi

if [[ "$__TargetArch" == wasm ]]; then
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
    if [[ -z "$ROOTFS_DIR" && "$platform" != "Darwin" ]]; then
        ROOTFS_DIR="$__RepoRootDir/.tools/rootfs/$__TargetArch"
        export ROOTFS_DIR
    fi
fi

# init the target distro name
initTargetDistroRid
