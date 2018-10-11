# Running unit tests within Visual Studio

Sometimes it is convenient to run individual unit tests within the Visual Studio IDE

There are several environment variables which must be set to get this to work correctly.  To make things easier there is a convenience script `visual-studio-devenv.cmd` provided.  This script will set the necessary environment variables.

## Steps

1. `build.cmd`
2. `visual-studio-devenv.cmd`
3. Open the test explorer window within the Visual Studio IDE
4. Select tests and run and/or debug.

## Limitations

* The script is not yet designed to be a robust solution.  As test configurations change this file could get out of date.  The required configuration can be discovered by carefully examining the logs generated when running the tests with `build.cmd -MsBuildLogging=/bl`.  The script can then be updated.
* The script is hardcoded to use the x64 debug build


