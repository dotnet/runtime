#!/usr/bin/env bash
# should be placed in src/mono/sample/HelloWorld/

REPO_ROOT=../../../..
LLVM_PATH=$REPO_ROOT/artifacts/bin/mono/OSX.arm64.Debug
MONO_SGEN=$REPO_ROOT/artifacts/obj/mono/OSX.arm64.Debug/mono/mini/mono-sgen
export MONO_PATH=$REPO_ROOT/artifacts/bin/HelloWorld/arm64/Debug/osx-arm64/publish

if [ "$1" != "build" ] && [ "$1" != "build-all" ] && [ "$1" != "run" ]; then
    echo "Pass 'build', 'build-all' or 'run' as the first parameter";
    exit 1
fi

if [ "$1" == "build" ] || [ "$1" == "build-all" ]; then
    if [ "$2" == "interp" ]; then
        export MONO_ENV_OPTIONS="--aot=full,interp,llvm,llvm-path=$LLVM_PATH,mattr=crc,mattr=crypto"
    else
        export MONO_ENV_OPTIONS="--aot=full"
        # export MONO_ENV_OPTIONS="--aot=full,llvm,llvm-path=$LLVM_PATH,mattr=crc,mattr=crypto"
    fi
    if [ "$1" == "build-all" ]; then 
        DLLS=$MONO_PATH/*.dll;
    else
        DLLS=$MONO_PATH/HelloWorld.dll;
    fi
    for dll in $DLLS; 
    do
        echo "> AOTing MONO_ENV_OPTIONS=$MONO_ENV_OPTIONS $dll";
        $MONO_SGEN $dll
        if [ $? -eq 1 ]; then
            echo "> AOTing MONO_ENV_OPTIONS=$MONO_ENV_OPTIONS $dll has failed.";
            exit 1
        fi
    done
else
    if [ "$2" == "interp" ]; then
        export MONO_ENV_OPTIONS="--full-aot-interp"
    else
        export MONO_ENV_OPTIONS="--full-aot"
    fi
    echo "Running HelloWorld with: MONO_ENV_OPTIONS=$MONO_ENV_OPTIONS $MONO_SGEN $MONO_PATH/HelloWorld.dll";
    $MONO_SGEN $MONO_PATH/HelloWorld.dll
fi
exit 0

