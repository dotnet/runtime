#!/usr/bin/env bash

# *.cmd and *.sh files may be considered test entry points. If launched directly, consider it a pass.
if [ "$1" != "--runCustomTest" ]; then
  exit 0
fi

CLRTestExpectedExitCode=100

echo Collect profile without R2R, use profile without R2R
rm -f profile.mcj
"$CORE_ROOT/corerun" BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi
"$CORE_ROOT/corerun" BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi

echo Collect profile with R2R, use profile with R2R
rm -f profile.mcj
"$CORE_ROOT/corerun" r2r/BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi
"$CORE_ROOT/corerun" r2r/BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi

echo Collect profile without R2R, use profile with R2R
rm -f profile.mcj
"$CORE_ROOT/corerun" BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi
"$CORE_ROOT/corerun" r2r/BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi

echo Collect profile with R2R, use profile without R2R
rm -f profile.mcj
"$CORE_ROOT/corerun" r2r/BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi
"$CORE_ROOT/corerun" BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi

echo Collect profile with R2R disabled, use profile with R2R enabled
rm -f profile.mcj
export COMPlus_ReadyToRun=0
"$CORE_ROOT/corerun" r2r/BasicTestWithMcj.dll
CLRTestExitCode=$?
unset COMPlus_ReadyToRun
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi
"$CORE_ROOT/corerun" r2r/BasicTestWithMcj.dll
CLRTestExitCode=$?
if [ $CLRTestExitCode -ne $CLRTestExpectedExitCode ]; then
  exit $CLRTestExitCode
fi

rm -f profile.mcj
exit $CLRTestExpectedExitCode
