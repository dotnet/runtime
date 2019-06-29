Testing with CoreFX
===================

You can use CoreFX tests to validate your changes to CoreCLR.
The coreclr repo Azure DevOps CI system runs CoreFX tests against
every pull request, and regularly runs the CoreFX tests under
many different configurations (e.g., platforms, stress modes).

There are two basic options:

1. Build the CoreFX product and tests with a build of CoreCLR, or
2. Use a published snapshot of the CoreFX test build with a build of CoreCLR.

Mechanism #1 is the easiest. Mechanism #2 is how the CI system runs tests,
as it is optimized for our distributed Helix test running system.

# Building CoreFX against CoreCLR

In all cases, first build the version of CoreCLR that you wish to test. You do not need to build the coreclr tests. Typically, this will be a Checked build (faster than Debug, but with asserts that Release builds do not have). In the examples here, coreclr is assumed to be cloned to `f:\git\coreclr`.

For example:
```
f:\git\coreclr> build.cmd x64 checked skiptests
```

Next, build CoreFX from a clone of the [CoreFX repo](https://github.com/dotnet/corefx).
For these examples, CoreFX is assumed to be cloned to `f:\git\corefx`. For general
information about how to build CoreFX, refer to the
[CoreFX Developer Guide](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md).

Normally when you build CoreFX it is built against a "last known good" Release version of CoreCLR.

There are two options here:
1. Build CoreFX against your just-built CoreCLR.
2. Build CoreFX normally, against the "last known good" version of CoreCLR, and then overwrite the "last known good" CoreCLR.

Option #1 might fail to build if CoreCLR has breaking changes that have not propagated to CoreFX yet.
Option #2 should always succeed the build, since the CoreFX CI has verified this build already.
Option #2 is generally recommended.

## Building CoreFX against just-built CoreCLR

To build CoreFX tests against a current, "live", version of CoreCLR, including with an updated System.Private.CoreLib.dll,
[use these instructions](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits).

For example:
```
f:\git\corefx> build.cmd -configuration Release -arch x64 -restore -build -buildtests -test /p:CoreCLROverridePath=f:\git\coreclr\bin\Product\Windows_NT.x64.Checked
```

Note that this will replace the coreclr used in the build, and because `-test` is passed, will also run the tests.

## Replace CoreCLR after building CoreFX normally

Do the following:

1. Build the CoreFX repo, but don't build tests yet.

```
f:\git\corefx> build.cmd -configuration Release -arch x64 -restore -build
```

This creates a "testhost" directory with a subdirectory that includes the coreclr bits, e.g., `f:\git\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\3.0.0`.

2. Copy the contents of the CoreCLR build you wish to test into the CoreFX runtime
folder created in step #1.

```
f:\git\corefx> copy f:\git\coreclr\bin\Product\Windows_NT.x64.Checked\* f:\git\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64\shared\Microsoft.NETCore.App\3.0.0
```

3. Optionally, create a script that contains any environment variables you want to set when running each CoreFX test. Disabling TieredCompilation or setting a JIT stress mode is a common case. E.g.,

```
f:\git\corefx> echo set COMPlus_TieredCompilation=0>f:\git\corefx\SetStressModes.bat
```

4. Build and run the CoreFX tests. Optionally, pass in a file that will be passed to xunit to provide extra xunit arguments. Typically, this is used to exclude known failing tests.

```
f:\git\corefx> build.cmd -configuration Release -arch x64 -buildtests -test /p:WithoutCategories=IgnoreForCI /p:PreExecutionTestScript=f:\git\corefx\SetStressModes.bat /p:TestRspFile=f:\git\coreclr\tests\CoreFX\CoreFX.issues.rsp
```

## Automating the CoreFX build and test run process

The script [tests\scripts\run-corefx-tests.py](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.py) clones the corefx repo, and then implements Option 2, described above. This simplifies the whole process to a single script invocation.

First, build CoreCLR as usual. Then, invoke the script. Specify the build architecture, the build type, where you want corefx to be put, optionally a script of environment variables to set before running the tests, and optionally a test exclusion file (as above). For example:

```
f:\git\coreclr> echo set COMPlus_TieredCompilation=0>f:\git\coreclr\SetStressModes.bat
f:\git\coreclr> python -u f:\git\coreclr\tests\scripts\run-corefx-tests.py -arch x64 -build_type Checked -fx_root f:\git\coreclr\_fx -env_script f:\git\coreclr\SetStressModes.bat -exclusion_rsp_file f:\git\coreclr\tests\CoreFX\CoreFX.issues.rsp
```

## Handling cross-compilation testing

The above instructions work fine if you are building and testing on the same machine,
but what if you are building on one machine and testing on another? This happens,
for example, when building for Windows arm32 on a Windows x64 machine,
or building for Linux arm64 on a Linux x64 machine (possibly in Docker).
In these cases, build all the tests, copy them to the target machine, and run tests
there.

To do that, remove `-test` from the command-line used to build CoreFX tests. Without `-test`,
the tests will be built but not run.

If using `run-corefx-tests.py`, pass the argument `-no_run_tests`.

After the tests are copied to the remote machine, you want to run them. Use one of the scripts
[tests\scripts\run-corefx-tests.bat](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.bat) or 
[tests\scripts\run-corefx-tests.sh](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.sh)
to run all the tests (consult the scripts for proper usage). Or, run a single test as described below.

## Other corefx build considerations

To build for Linux arm32, you need to make sure to build using clang 5 (the default is clang 3.9).
You might need to pass `/p:BuildNativeCompiler=--clang5.0` to the corefx build sjcripts.

## Running a single CoreFX test assembly

Once you've built the CoreFX tests (possibly with replaced CoreCLR bits), you can also run just a single test. E.g.,

```
f:\git\corefx> cd f:\git\corefx\artifacts\bin\System.Buffers.Tests\netcoreapp-Release
f:\git\corefx\artifacts\bin\System.Buffers.Tests\netcoreapp-Release> RunTests.cmd -r f:\git\corefx\artifacts\bin\testhost\netcoreapp-Windows_NT-Release-x64
```

Alternatively, you can run the tests from from the test source directory, as follows:

```
f:\git\corefx> cd f:\git\corefx\src\System.Buffers\tests
f:\git\corefx\src\System.Buffers\tests> dotnet msbuild /t:Test /p:ForceRunTests=true;ConfigurationGroup=Release
```

# Using a published snapshot of CoreFX tests

The corefx official build system publishes a set of corefx test packages for consumption
by the coreclr CI. You can use this set of published files, but it is complicated, especially
if you wish to run more than one or a few tests.

The process builds a "test host", which is a directory layout like the dotnet CLI, and uses that
when invoking the tests. CoreFX product packages, and packages needed to run CoreFX tests,
are restored, and the CoreCLR to test is copied in.

For Windows:

1. `.\build.cmd <arch> <build_type> skiptests` -- build the CoreCLR you want to test
2. `.\build-test.cmd <arch> <build_type> buildtesthostonly` -- this generates the test host

For Linux and macOS:

1. `./build.sh <arch> <build_type> skiptests`
2. `./build-test.sh <arch> <build_type> generatetesthostonly`

The published tests are summarized in a `corefx-test-assets.xml` file that lives here:

```
https://dotnetfeed.blob.core.windows.net/dotnet-core/corefx-tests/$(MicrosoftPrivateCoreFxNETCoreAppVersion)/$(__BuildOS).$(__BuildArch)/$(_TargetGroup)/corefx-test-assets.xml
```

where `MicrosoftPrivateCoreFxNETCoreAppVersion` is defined in `eng\Versions.props`. For example:

```
https://dotnetfeed.blob.core.windows.net/dotnet-core/corefx-tests/4.6.0-preview8.19326.15/Linux.arm64/netcoreapp/corefx-test-assets.xml       
```

This file lists all the published test assets. You can download each one, unpack it, and
then use the generated test host to run the test.

Here is an example test file:
```
https://dotnetfeed.blob.core.windows.net/dotnet-core/corefx-tests/4.6.0-preview8.19326.15/Linux.arm64/netcoreapp/tests/AnyOS.AnyCPU.Release/CoreFx.Private.TestUtilities.Tests.zip
```

=========================

TBD: The following describes some automation for running CoreFX tests from a similar, but older system.
These instructions currently do not work (but perhaps should be revived to work).

For Windows:

3. `.\tests\runtest.cmd <arch> <build_type> corefxtests|corefxtestsall` -- this runs the CoreFX tests

For Linux and macOS:

3. `./tests/runtest.sh --corefxtests|--corefxtestsall --testHostDir=<path_to_testhost> --coreclr-src=<path_to_coreclr_root>`

where:
+ `<path_to_testhost>` - path to the CoreCLR test host built in step 2.
+ `<path_to_coreclr_root>` - path to root of CoreCLR clone. Required to build the TestFileSetup tool for CoreFX testing.

The set of tests run are based on the `corefxtests` or `corefxtestsall` arguments, as follows:
+ CoreFXTests - runs all tests defined in the dotnet/coreclr repo in `tests\CoreFX\CoreFX.issues.json`, or the test list specified with the optional argument `CoreFXTestList`.
+ CoreFXTestsAll - runs all tests available, ignoring exclusions. The full list of tests is found at the URL in the dotnet/coreclr repo at `.\tests\CoreFX`: one of `CoreFXTestListURL.txt`, `CoreFXTestListURL_Linux.txt`, or `CoreFXTestListURL_OSX.txt`, based on platform.

=========================

# CoreFX test exclusions

The CoreCLR CI system runs CoreFX tests against a just-built CoreCLR. If tests need to be
disabled due to transitory breaking change, for instance, update the 
[test exclusion file](https://github.com/dotnet/coreclr/blob/master/tests/CoreFX/CoreFX.issues.rsp).
