#!/bin/bash

OSName=$(uname -s)
case $OSName in
    Darwin)
        OS=OSX
        ;;

    FreeBSD)
        OS=FreeBSD
        ;;

    Linux)
        OS=Linux
        ;;

    NetBSD)
        OS=NetBSD
        ;;

    SunOS)
        OS=SunOS
        ;;
    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        OS=Linux
        ;;
esac

export CORE_ROOT=`pwd`/artifacts/tests/$OSName.$1.$2/Tests/Core_Root
FRAMEWORK_DIR=`pwd`/artifacts/tests/$OSName.$1.$2/GC/Stress/Framework/ReliabilityFramework
$CORE_ROOT/corerun $FRAMEWORK_DIR/ReliabilityFramework.exe $FRAMEWORK_DIR/testmix_gc.config
EXIT_CODE=$?
if [ $EXIT_CODE -eq 100 ]
then
    echo "ReliabilityFramework successful"
    exit 0
fi

if [ $EXIT_CODE -eq 99 ]
then
    echo "ReliabilityFramework test failed, some tests failed"
    exit 1
fi

echo "ReliabilityFramework returned a strange exit code $EXIT_CODE, perhaps some config is wrong?"
exit 1
