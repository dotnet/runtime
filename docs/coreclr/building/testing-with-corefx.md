Testing with CoreFX
===================

You can use CoreFX tests to validate your changes to CoreCLR. There are two basic options:

1. Build the CoreFX product and tests against a build of CoreCLR, or
2. Use a snapshot of the CoreFX test build with a build of CoreCLR.

Both mechanisms are exposed to certain types of breaking changes which can cause test failures.
However, we have a test exclusion mechanism for option #2, with exclusions specified in the
CoreCLR tree, not the CoreFX tree. This can make it possible to exclude tests that fail for
transient breaking change reasons, as well as for more long-lasting reasons.

Mechanism #2 is used to run CoreFX tests in the CI against every CoreCLR pull request.

# Building CoreFX against CoreCLR

In general, refer to the
[CoreFX Developer Guide](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md)
for information about CoreFX build scripts.

Normally when you build CoreFX it is built against a "last known good" version of CoreCLR. 
To run CoreFX tests against a current, "live", version of CoreCLR (for example, a CoreCLR
you have built yourself), including with an updated System.Private.CoreLib.dll,
[use these instructions](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits).

## Replace runtime between building CoreFX product and tests

A variation on the above is to build CoreFX normally, then overwrite the "last known good" CoreCLR
it used with your build of CoreCLR.

Do the following:

1. Build the CoreCLR you wish to test under `<coreclr_root>`.
2. Build the CoreFX repo (using `build.[cmd|sh]`) under `<corefx_root>`, but don't build tests yet.
3. Copy the contents of the CoreCLR binary root you wish to test into the CoreFX runtime
folder created in step #2.

For example:

`copy <coreclr_root>\bin\Product\Windows_NT.<arch>.<build_type>\* <corefx_root>\artifacts\bin\testhost\netcoreapp-Windows_NT-<build_type>-<arch>\shared\Microsoft.NETCore.App\9.9.9`

-or-  

`cp <coreclr_root>/bin/Product/<os>.<arch>.<build_type>/* <corefx_root>/artifacts/bin/testhost/netcoreapp-<os>-<build_type>-<arch>/shared/Microsoft.NETCore.App/9.9.9`
  
4. Build and run the CoreFX tests using `build.[cmd|sh] -test` as described in the Developer Guide.

### CI Script

[run-corefx-tests.py](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.py)
will clone dotnet/corefx and run steps 2-4 above automatically. It is primarily intended
to be run by the dotnet/coreclr CI system, but it might provide a useful reference or
shortcut for individuals running the tests locally.

# Using the built CoreCLR test host

Here is an alternative method to the one described above. You can test your changes with
an existing CoreFX build or CoreCLR's cached CoreFX test build assemblies.

The "test host" is a dotnet CLI layout that includes both the CoreCLR and the CoreFX you want to test.

## Locally-built CoreFX 

First, build CoreCLR (building the tests is not required) and CoreFX (including the tests),
as described above:

1. Build the CoreCLR you wish to test under `<coreclr_root>`.
2. Build the CoreFX repo under `<corefx_root>`.
3. Build the CoreFX tests using `build.[cmd|sh] -test`.

Once these are built, execute the following commands to test your local CoreCLR changes
with the built CoreFX changes.

1. From `<coreclr_root>` run:

For Windows:
```
build-test.cmd <arch> <build_type> buildtesthostonly
```

For Linux:
```
build-test.sh <arch> <build_type> generatetesthostonly
```

to generate the test host.

2. Navigate to `<corefx_root>\bin\tests\` and then into the directory for the test
you would like to run.

3. Run:

For Windows:
```cmd
<coreclr_root>\bin\<os>.<arch>.<build_type>\testhost\dotnet.exe .\xunit.console.netcore.exe <testname>.dll
```

For Linux:
```sh
<coreclr_root>/bin/<os>.<arch>.<build_type>/testhost/dotnet ./xunit.console.netcore.exe <testname>.dll
```

followed by any extra command-line arguments for xunit.

For example to run .NET Core Windows tests from System.Collections.Tests with an x64 Release build of CoreCLR:

For Windows:
```
cd C:\corefx\artifacts\bin\tests\System.Collections.Tests
C:\coreclr\bin\tests\Windows_NT.x64.Release\testhost\dotnet.exe .\xunit.console.netcore.exe .\System.Collections.Tests.dll -notrait category=nonnetcoretests -notrait category=nonwindowstests
```

For Linux:
```
cd ~/corefx/bin/tests/System.Collections.Tests
~/coreclr/artifacts/bin/tests/Linux.x64.Release/testhost/dotnet ./xunit.console.netcore.exe ./System.Collections.Tests.dll -notrait category=nonnetcoretests -notrait category=nonlinuxtests
```

## Running against a cached copy of the CoreFX tests

CoreCLR has an alternative way to run CoreFX tests, built for CI jobs.

To run tests against pre-built binaries you can execute the following from the CoreCLR repo root:

For Windows:

1. `.\build.cmd <arch> <build_type> skiptests`
2. `.\build-test.cmd <arch> <build_type> buildtesthostonly` -- this generates the test host
3. `.\tests\runtest.cmd <arch> <build_type> corefxtests|corefxtestsall` -- this runs the CoreFX tests

For Linux and macOS:

1. `./build.sh <arch> <build_type> skiptests`
2. `./build-test.sh <arch> <build_type> generatetesthostonly`
3. `./tests/runtest.sh --corefxtests|--corefxtestsall --testHostDir=<path_to_testhost> --coreclr-src=<path_to_coreclr_root>`

where:
+ `<path_to_testhost>` - path to the CoreCLR test host built in step 2.
+ `<path_to_coreclr_root>` - path to root of CoreCLR clone. Required to build the TestFileSetup tool for CoreFX testing.

The set of tests run are based on the `corefxtests` or `corefxtestsall` arguments, as follows:
+ CoreFXTests - runs all tests defined in the dotnet/coreclr repo in `tests\CoreFX\CoreFX.issues.json`, or the test list specified with the optional argument `CoreFXTestList`.
+ CoreFXTestsAll - runs all tests available, ignoring exclusions. The full list of tests is found at the URL in the dotnet/coreclr repo at `.\tests\CoreFX`: one of `CoreFXTestListURL.txt`, `CoreFXTestListURL_Linux.txt`, or `CoreFXTestListURL_OSX.txt`, based on platform.

## Helix Testing

To use Helix-built binaries, substitute the URL in `.\tests\CoreFX\CoreFXTestListURL.txt`
with one acquired from a Helix test run and run the commands above.

## Workflow

The CoreFX tests CI jobs run against cached test binaries in blob storage. This means that
tests might need to be disabled until the test binaries are refreshed as breaking changes
are merged in both CoreCLR and CoreFX. If you suspect a test is not failing because of a
functional regression, but rather because it's stale you can add it to the
[test exclusion file](https://github.com/dotnet/coreclr/blob/master/tests/CoreFX/CoreFX.issues.json).

## Test List Format

The tests defined in CoreFX.issues.json or the test list specified with the argument
`CoreFXTestList` should conform to the following format.
```js
[   // array of assemblies
    {   // one per assembly
        "name": "<Fully Qualified Assembly Name>", //e.g. System.Collections.Concurrent.Tests
        "enabled": true|false, // Defines whether a test assembly should be run. If set to false any tests with the same name will not be run even if corefxtestsall is specified
        "exclusions": {
            "namespaces": // Can be null
            [
                {
                    "name": "System.Collections.Concurrent.Tests", // All test methods under this namespace will be skipped
                    "reason": "<Reason for exclusion>" // This should be a link to the GitHub issue describing the problem
                }
            ]
            "classes": // Can be null
            [
                {
                    "name": "System.Collections.Concurrent.Tests.ConcurrentDictionaryTests", // All test methods in this class will be skipped
                    "reason": "<Reason for exclusion>"
                }
            ]
            "methods": // Can be null
            [
                {
                    "name": "System.Collections.Concurrent.Tests.ConcurrentDictionaryTests.TestAddNullValue_IDictionary_ReferenceType_null",
                    "reason": "<Reason for exclusion>"
                },
                {
                    "name": "System.Collections.Concurrent.Tests.ConcurrentDictionaryTests.TestAddNullValue_IDictionary_ValueType_null_add",
                    "reason": "<Reason for exclusion>"
                }
            ]
        }
    },
    { // next assembly
        ...
    }
]
```
    