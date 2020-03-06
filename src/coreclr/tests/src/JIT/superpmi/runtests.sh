#!/bin/bash

# Run CoreCLR OSS tests on linux or Mac
# Use the instructions here:
#    https://github.com/dotnet/runtime/blob/master/docs/workflow/building/coreclr/unix-test-instructions.md
#
# Summary:
# 1. On linux/Mac, in coreclr, ./build.sh
# 2. On linux/Mac, in corefx, ./build.sh
# 3. On linux/Mac, in corefx, "build-native.sh"
# 4. On Windows, build linux mscorlib: build.cmd linuxmscorlib
# 5. Mount Windows shares on linux
# 6. Copy tests to linux/Mac:
#       linux: cp --recursive ~/brucefo1/ManagedCodeGen/artifacts/tests/win.x64.Debug ~/test/win.x64.Debug
#       Mac  : cp -R          ~/brucefo1/ManagedCodeGen/artifacts/tests/win.x64.Debug ~/test/win.x64.Debug
# 7. Run this script
#
# If you pass "--testDir=NONE" to runtest.sh, it will create the "test overlay" (i.e., core_root directory)
# and exit. This is a hack because runtest.sh doesn't have a separate argument to just do this.

TESTROOT=~/test
CORECLRROOT=~/src/coreclr
COREFXROOT=~/src/corefx
WINDOWSCORECLRROOT=~/WindowsMachine/coreclr
WINDOWSFLAVOR=win.x64.Debug
UNIXANYFLAVOR=osx.AnyCPU.Debug
UNIXARCHFLAVOR=osx.x64.Debug

ARGS="\
--testRootDir=${TESTROOT}/${WINDOWSFLAVOR} \
--testNativeBinDir=${CORECLRROOT}/artifacts/obj/${UNIXARCHFLAVOR}/tests \
--coreClrBinDir=${CORECLRROOT}/artifacts/bin/coreclr/${UNIXARCHFLAVOR} \
--mscorlibDir=${WINDOWSCORECLRROOT}/artifacts/bin/coreclr/${UNIXARCHFLAVOR} \
--coreFxBinDir=${COREFXROOT}/artifacts/${UNIXANYFLAVOR};${COREFXROOT}/artifacts/Unix.AnyCPU.Debug;${COREFXROOT}/artifacts/AnyOS.AnyCPU.Debug \
--coreFxNativeBinDir=${COREFXROOT}/artifacts/${UNIXARCHFLAVOR}"

pushd ${CORECLRROOT}
echo ${CORECLRROOT}/tests/runtest.sh ${ARGS}
#${CORECLRROOT}/tests/runtest.sh ${ARGS} --testDir=NONE
${CORECLRROOT}/tests/runtest.sh ${ARGS}
popd
