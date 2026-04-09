#!/usr/bin/env bash

REPO_ROOT=../../../..
LLVM_PATH=$REPO_ROOT/artifacts/obj/mono/osx.arm64.Debug/llvm/arm64/bin
MONO_SGEN=$REPO_ROOT/artifacts/obj/mono/osx.arm64.Debug/mono/mini/mono-sgen
export MONO_PATH=$REPO_ROOT/artifacts/bin/HelloWorld/arm64/Debug/osx-arm64/publish

if [ "$1" != "build" ] && [ "$1" != "build-all" ] && [ "$1" != "run" ] && [ "$1" != "llvm-dis" ]; then
    echo "Pass 'build', 'build-all' or 'run' as the first parameter"
    echo "If 'build' - pass the name of the assembly as second, pass 'save-temps' as third argument to save llvm template files"
    echo "If 'run' - pass 'log' as third for verbose logging"
    echo "If 'llvm-dis' - pass the llvm template file saved from build as second argument"
    exit 1
fi

# full,interp,llvm,llvm-path=$LLVM_PATH,
if [ "$1" == "build" ] || [ "$1" == "build-all" ]; then
    if [ "$3" == "save-temps" ]; then
        export MONO_ENV_OPTIONS="--aot=llvm,save-temps,llvm-path=$LLVM_PATH,mcpu=native"
    else
        export MONO_ENV_OPTIONS="--aot=llvm,llvm-path=$LLVM_PATH,mcpu=native"
    fi
    
    if [ "$1" == "build-all" ]; then 
        DLLS=$MONO_PATH/*.dll;
    else
        if [ -z "$2" ]; then
            echo "Please pass the name of the assembly ex: HelloWorld.dll"
            exit 1
        fi
        DLLS=$MONO_PATH/$2;
    fi
    for dll in $DLLS; 
    do
        echo "> AOTing MONO_ENV_OPTIONS=$MONO_ENV_OPTIONS $dll";
        if [ "$3" == "debug" ]; then
            lldb -- $MONO_SGEN $dll
        else
            $MONO_SGEN $dll
        fi
        if [ $? -eq 1 ]; then
            echo "> AOTing MONO_ENV_OPTIONS=$MONO_ENV_OPTIONS $dll has failed.";
            exit 1
        fi
    done
fi

if [ "$1" == "run" ]; then
    export MONO_ENV_OPTIONS="--full-aot"
    # MONO_ENV_OPTIONS="--full-aot-interp"

    if [ "$2" == "log" ]; then
        LOG_LEVEL=debug 
        LOG_MASK=aot
    fi
    
    echo "Running HelloWorld with: MONO_ENV_OPTIONS=$MONO_ENV_OPTIONS $MONO_SGEN $MONO_PATH/HelloWorld.dll";
    MONO_LOG_LEVEL=$LOG_LEVEL MONO_LOG_MASK=$LOG_MASK $MONO_SGEN $MONO_PATH/HelloWorld.dll
fi

if [ "$1" == "llvm-dis" ]; then
    $LLVM_PATH/llvm-dis $2
fi
exit 0
