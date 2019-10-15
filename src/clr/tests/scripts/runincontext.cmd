@rem This script is a bridge that allows .cmd files of individual tests to run the respective test executables
@rem in an unloadable AssemblyLoadContext.
@rem
@rem To use this script, set the CLRCustomTestLauncher environment variable to the full path of this script.
@rem
@rem Additional command line arguments can be passed to the runincontext tool by setting the RunInContextExtraArgs
@rem environment variable
@rem
@rem The .cmd files of the individual tests will call this script to launch the test.
@rem This script gets the following arguments
@rem 1. Full path to the directory of the test binaries (the test .cmd file is in there)
@rem 2. Filename of the test executable
@rem 3. - n. Additional arguments that were passed to the test .cmd

set CORE_LIBRARIES=%1
%_DebuggerFullPath% "%CORE_ROOT%\corerun.exe" "%CORE_ROOT%\runincontext.dll" %RunInContextExtraArgs% /referencespath:%CORE_ROOT%\ %1%2 %3 %4 %5 %6 %7 %8 %9
