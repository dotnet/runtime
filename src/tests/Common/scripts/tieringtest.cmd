@rem This script is a bridge that allows .cmd files of individual tests to run repeatedly so methods
@rem can tier up.
@rem
@rem To use this script, set the CLRCustomTestLauncher environment variable to the full path of this script.

set CORE_LIBRARIES=%1
%_DebuggerFullPath% "%CORE_ROOT%\corerun.exe" "%CORE_ROOT%\tieringtest.dll" %1%2 %3 %4 %5 %6 %7 %8 %9

