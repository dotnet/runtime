Building and running tests on Windows
=====================================

**Building Tests**        

In a clean command prompt, issue the following command: 

    C:\git\coreclr>tests\buildtest.cmd clean

**buildtest /?** will list supported parameters.

**Note:** The above command (or building from the repo_root) must be done once, at the least, to ensure that all test dependencies are initialized correctly. 

In Visual Studio, open `<repo_root>\tests\src\AllTestProjects.sln`, build all the test projects or the one required.

**Running Tests**

In a clean command prompt: `<repo_root>\tests\runtest.cmd`

**runtest /?** will list supported parameters.

This will generate the report named as `TestRun_<arch>_<flavor>.html` (e.g. `TestRun_x64__release.html`) in the current folder. It will also copy all the test dependencies to the folder passed at the command line.

**Investigating Test Failures**

Upon completing a test run, you may find one or more tests have failed.

The output of the Test will be available in Test reports directory, but the default the directory would be something like is `<repo_root>\binaries\tests\x64\debug\Reports\Exceptions\Finalization`.

There are 2 files of interest: 

- `Finalizer.output.txt` - Contains all the information logged by the test.
- `Finalizer.error.txt`  - Contains the information reported by CoreRun.exe (which executed the test) when the test process crashes.

**Rerunning a failed test**

If you wish to re-run a failed test, please follow the following steps:

1. Set an environment variable, `CORE_ROOT`, pointing to the path to product binaries that was passed to runtest.cmd. The command to set this environment variable is also specified in the test report for a failed test.
2. Next, run the failed test, the command to which is also present in the test report for a failed test. It will be something like `<repo_root>\binaries\tests\x64\debug\Exceptions\Finalization\Finalizer.cmd`.

If you wish to run the test under a debugger (e.g. [WinDbg](http://msdn.microsoft.com/en-us/library/windows/hardware/ff551063(v=vs.85).aspx)), append `-debug <debuggerFullPath>` to the test command. For example:


     <repo_root>\binaries\tests\x64\debug\Exceptions\Finalization\Finalizer.cmd -debug <debuggerFullPath>
    
**Modifying a test**

If test changes are needed, make the change and build the test project. This will binplace the binaries in test binaries folder (e.g. `<repo_root>\binaries\tests\x64\debug\Exceptions\Finalization`). At this point, follow the steps to re-run a failed test to re-run the modified test.

**Authoring Tests (in VS)**

1. Use an existing test such as `<repo_root>\tests\src\Exceptions\Finalization\Finalizer.csproj` as a template and copy it to a new folder under `<repo_root>\tests\src`.
2. Add the project of the new test to `<repo_root>\tests\src\AllTestProjects.sln` in VS
3. Add source files to this newly added project.
4. Indicate the success of the test by returning `100`.
5. Add the .NET CoreFX contract references, as required, via the Nuget Package Manager in Visual Studio. *Make sure this does not change the csproj. If it does, then undo the change in the csproj.*
6. Add any other projects as a dependency, if needed.
7. Build the test.
8. Follow the steps to re-run a failed test to validate the new test.
