#!/bin/sh
#
# Initializes the CMake compiler and toolchain environment.
#

if [ -z "$reporoot" ] || [ -z "$build_arch" ] || [ -z "$target_os" ]; then
    echo "Usage: reporoot=<REPO_ROOT> build_arch=<ARCH> target_os=<OS> . init-cmake-toolchain.sh"
    exit 1
fi

CMAKE_CONFIGURE_COMMAND_WRAPPER=
CMAKE_INITIAL_CACHE=
CMAKE_TOOLCHAIN_FILE=
CLR_CMAKE_TARGET_OS=

if [ "$build_arch" = "wasm" ]; then
    . "$reporoot/eng/wasm/wasm-tool-cache.sh"

    if [ "$target_os" = "browser" ]; then
        if [ -z "$EMSDK_PATH" ]; then
            if ! EMSDK_PATH="$(wasm_tool_cache_dir emscripten "$reporoot/src/mono/browser/emscripten-version.txt" "$reporoot")"; then
                echo "Error: You need to set the EMSDK_PATH environment variable pointing to the emscripten SDK root."
                exit 1
            fi
        fi

        EMSDK_QUIET=1
        export EMSDK_PATH EMSDK_QUIET
        if [ -n "${BASH_VERSION:-}" ]; then
            . "$EMSDK_PATH/emsdk_env.sh"
        else
            emcmake()
            {
                EMSDK_PATH="$EMSDK_PATH" EMSDK_QUIET=1 bash -c '. "$EMSDK_PATH/emsdk_env.sh" && exec emcmake "$@"' _ "$@"
            }

            cmake()
            {
                EMSDK_PATH="$EMSDK_PATH" EMSDK_QUIET=1 bash -c '. "$EMSDK_PATH/emsdk_env.sh" && exec cmake "$@"' _ "$@"
            }
        fi

        CMAKE_CONFIGURE_COMMAND_WRAPPER=emcmake
        CMAKE_INITIAL_CACHE="$reporoot/eng/native/tryrun.browser.cmake"
    elif [ "$target_os" = "wasi" ]; then
        if [ -z "$WASI_SDK_PATH" ]; then
            if ! WASI_SDK_PATH="$(wasm_tool_cache_dir wasi-sdk "$reporoot/eng/wasm/wasi-sdk-version.txt" "$reporoot")"; then
                echo "Error: You need to set the WASI_SDK_PATH environment variable pointing to the WASI SDK root."
                exit 1
            fi
        fi

        CMAKE_TOOLCHAIN_FILE="$WASI_SDK_PATH/share/cmake/wasi-sdk-p2.cmake"
        CLR_CMAKE_TARGET_OS=wasi
    else
        echo "Error: target_os must be browser or wasi when build_arch is wasm."
        exit 1
    fi
fi

if [ "${USE_SCCACHE:-}" = "true" ]; then
    CMAKE_C_COMPILER_LAUNCHER=sccache
    if [ "$target_os" = "osx" ] || [ "$target_os" = "maccatalyst" ]; then
        CMAKE_C_COMPILER_LAUNCHER="$reporoot/eng/native/sccache-xarch-wrapper.sh"
    fi
    CMAKE_CXX_COMPILER_LAUNCHER="$CMAKE_C_COMPILER_LAUNCHER"
fi

export CMAKE_CONFIGURE_COMMAND_WRAPPER CMAKE_INITIAL_CACHE CLR_CMAKE_TARGET_OS
export CMAKE_TOOLCHAIN_FILE CMAKE_C_COMPILER_LAUNCHER CMAKE_CXX_COMPILER_LAUNCHER
