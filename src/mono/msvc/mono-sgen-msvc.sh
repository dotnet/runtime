#!/bin/bash

# Wrapper that can be used as MONO_EXECUTABLE_WRAPPER in mono-wrapper when running MSVC build
# mono-sgen.exe. Simplify the setup of VS and MSVC toolchain, primarly when running MSVC build
# mono-sgen.exe as AOT compiler, since it needs to locate libraries as well as ClangC2 and linker
# from VS MSVC for corresponding architecture.

# NOTE, MSVC build mono-sgen.exe AOT compiler currently support 64-bit AMD codegen. mono-sgen-msvc.bat will ony setup
# amd64 versions of VS MSVC build environment and corresponding ClangC2 compiler.

# Optimization, only run full build environment when running mono-sgen.exe as AOT compiler.
# If not, just run mono-sgen.exe with supplied arguments.

MONO_SGEN_MSVC_SCRIPT_PATH=$(cd "$(dirname "$0")"; pwd)

if [[ "$@" != *"--aot="* ]]; then
    "$MONO_SGEN_MSVC_SCRIPT_PATH/mono-sgen.exe" "$@"
else
    MONO_SGEN_MSVC_SCRIPT_PATH=$(cygpath -w "$MONO_SGEN_MSVC_SCRIPT_PATH/mono-sgen-msvc.bat")
    "$WINDIR/System32/cmd.exe" /c "$MONO_SGEN_MSVC_SCRIPT_PATH" "$@"
fi
