#!/bin/bash

export CORE_ROOT=`pwd`/bin/tests/Windows_NT.$1.$2/Tests/coreoverlay
FRAMEWORK_DIR=`pwd`/bin/tests/Windows_NT.$1.$2/GC/Stress/Framework/ReliabilityFramework
$CORE_ROOT/corerun $FRAMEWORK_DIR/ReliabilityFramework.exe $FRAMEWORK_DIR/testmix_gc.config | tee stdout.txt

