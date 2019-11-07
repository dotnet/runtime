#!/bin/bash

# Run CoreCLR OSS tests on Linux or Mac
# Use the instructions here:
#    https://github.com/dotnet/coreclr/blob/master/Documentation/building/unix-test-instructions.md
#
# Summary:
# 1. On Linux/Mac, in coreclr, ./build.sh
# 2. On Linux/Mac, in corefx, ./build.sh
# 3. On Linux/Mac, in corefx, "build-native.sh"
# 4. On Windows, build Linux mscorlib: build.cmd linuxmscorlib
# 5. Mount Windows shares on Linux
# 6. Copy tests to Linux/Mac:
#       Linux: cp --recursive ~/brucefo1/ManagedCodeGen/bin/tests/Windows_NT.x64.Debug ~/test/Windows_NT.x64.Debug
#       Mac  : cp -R          ~/brucefo1/ManagedCodeGen/bin/tests/Windows_NT.x64.Debug ~/test/Windows_NT.x64.Debug
# 7. Run this script
#
# If you pass "--testDir=NONE" to runtest.sh, it will create the "test overlay" (i.e., core_root directory)
# and exit. This is a hack because runtest.sh doesn't have a separate argument to just do this.

TESTROOT=~/test
CORECLRROOT=~/src/coreclr
COREFXROOT=~/src/corefx
WINDOWSCORECLRROOT=~/WindowsMachine/coreclr
WINDOWSFLAVOR=Windows_NT.x64.Debug
UNIXANYFLAVOR=OSX.AnyCPU.Debug
UNIXARCHFLAVOR=OSX.x64.Debug

ARGS="\
--testRootDir=${TESTROOT}/${WINDOWSFLAVOR} \
--testNativeBinDir=${CORECLRROOT}/bin/obj/${UNIXARCHFLAVOR}/tests \
--coreClrBinDir=${CORECLRROOT}/bin/Product/${UNIXARCHFLAVOR} \
--mscorlibDir=${WINDOWSCORECLRROOT}/bin/Product/${UNIXARCHFLAVOR} \
--coreFxBinDir=${COREFXROOT}/bin/${UNIXANYFLAVOR};${COREFXROOT}/bin/Unix.AnyCPU.Debug;${COREFXROOT}/bin/AnyOS.AnyCPU.Debug \
--coreFxNativeBinDir=${COREFXROOT}/bin/${UNIXARCHFLAVOR}"

pushd ${CORECLRROOT}
echo ${CORECLRROOT}/tests/runtest.sh ${ARGS}
#${CORECLRROOT}/tests/runtest.sh ${ARGS} --testDir=NONE
${CORECLRROOT}/tests/runtest.sh ${ARGS}
popd
