Testing with CoreFX
===================

It may be valuable to use CoreFX tests to validate your changes to CoreCLR or mscorlib.

## Building CoreFX against CoreCLR
**NOTE:** The `BUILDTOOLS_OVERRIDE_RUNTIME` property no longer works.

To run CoreFX tests with an updated System.Private.Corelib.dll, [use these instructions](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md#testing-with-private-coreclr-bits).

To build CoreFX against the updated System.Private.Corelib.dll - we need to update instructions.

**Replace runtime between build.[cmd|sh] and build-tests.[cmd|sh]**

Use the following instructions to test a change to the dotnet/coreclr repo using dotnet/corefx tests.  Refer to the [CoreFx Developer Guide](https://github.com/dotnet/corefx/blob/master/Documentation/project-docs/developer-guide.md) for information about CoreFx build scripts.

1. Build the CoreClr runtime you wish to test under `<coreclr_root>`
2. Build the CoreFx repo (`build.[cmd|sh]`) under `<corefx_root>`, but don't build tests yet
3. Copy the contents of the CoreCLR binary root you wish to test into the CoreFx runtime folder (`<flavor>` below) created in step #2.  For example:

  `copy <coreclr_root>\bin\Product\Windows_NT.<arch>.<build_type>\* <corefx_root>\bin\runtime\<flavor>`  
  -or-  
  `cp <coreclr_root>/bin/Product/<os>.<arch>.<build_type>/* <corefx_root>/bin/runtime/<flavor>`  
  
4. Run the CoreFx `build-tests.[cmd|sh]` script as described in the Developer Guide.

**CI Script**

[run-corefx-tests.py](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.py) will clone dotnet/corefx and run steps 2-4 above automatically.  It is primarily intended to be run by the dotnet/coreclr CI system, but it might provide a useful reference or shortcut for individuals running the tests locally.

## Using the built CoreCLR testhost 

Instead of copying CoreCLR binaries you can also test your changes with an existing CoreFX build or CoreCLR's CI assemblies

### Locally-built CoreFX 

Once you have finished steps 1, 2. and 4. above execute the following instructions to test your local CLR changes with the built-CoreFX changes.

1. From `<coreclr_root>` run
`build-test.cmd <arch> <build_type> buildtesthostonly` 

-or-

`build-test.sh <arch> <build_type> generatetesthostonly` 

to generate the test host.
2. Navigate to `<corefx_root>\bin\tests\` and then the test you would like to run
3. Run

```cmd
<coreclr_root>\bin\<os>.<arch>.<build_type>\testhost\dotnet.exe <corefx_root>\bin\tests\<testname>\xunit.console.netcore.exe <testname>.dll
```
-or-

```sh
<coreclr_root>/bin/<os>.<arch>.<build_type>/testhost/dotnet <corefx_root>/bin/tests/<testname>/xunit.console.netcore.exe <testname>.dll
```

followed by any extra command-line arguments.

For example to run .NET Core Windows tests from System.Collections.Tests with an x64 Release build of CoreCLR.
```
cd C:\corefx\bin\tests\System.Collections.Tests
C:\coreclr\bin\tests\Windows_NT.x64.Release\testhost\dotnet.exe .\xunit.console.netcore.exe .\System.Collections.Tests.dll -notrait category=nonnetcoretests -notrait category=nonwindowstests
```

-or-

```
cd ~/corefx/bin/tests/System.Collections.Tests
~/coreclr/bin/tests/Linux.x64.Release/testhost/dotnet .\xunit.console.netcore.exe .\System.Collections.Tests.dll -notrait category=nonnetcoretests -notrait category=nonlinuxtests
```

### CI Script
CoreCLR has an alternative way to run CoreFX tests, built for PR CI jobs. 

To run tests against pre-built binaries you can execute the following from the CoreCLR repo root:

#### Windows
1. `.\build.cmd <arch> <build_type> skiptests`
2. `.\build-test.cmd <arch> <build_type> buildtesthostonly` - generates the test host
3. `.\tests\runtest.cmd <arch> <build_type> corefxtests|corefxtestsall` - runs CoreFX tests

#### Linux and OSX
1. `./build.sh <arch> <build_type> skiptests`
2. `./build-test.sh <arch> <build_type>  generatetesthostonly`
3. `./tests/runtest.sh  --corefxtests|--corefxtestsall --testHostDir=<path_to_testhost> --coreclr-src<path_to_coreclr>`

CoreFXTests - runs all tests defined in TopN.Windows.CoreFX.issues.json or the test list specified with the argument `CoreFXTestList`
CoreFXTestsAll - runs all tests available in the test list found at the URL in `.\coreclr\tests\CoreFX\CoreFXTestListURL.txt`.
#### Linux - specific
&lt;path_to_testhost&gt; - path to the coreclr test host built in step 2.
&lt;path_to_coreclr&gt; - path to coreclr source 

### Helix Testing
To use Helix-built binaries, substitute the URL in `.\coreclr\tests\CoreFX\CoreFXTestListURL.txt` with one acquired from a Helix test run and run the commands above.

#### Workflow
The CoreFX tests CI jobs run against cached test binaries in blob storage. This means that tests might need to be disabled until the test binaries are refreshed as breaking changes are merged in both CoreCLR and CoreFX. If you suspect a test is not failing because of a functional regression, but rather because it's stale you can add it to either the [Windows](https://github.com/dotnet/coreclr/blob/master/tests/CoreFX/TopN.CoreFX.x64.Windows.issues.json) or [Unix](https://github.com/dotnet/coreclr/blob/master/tests/CoreFX/TopN.CoreFX.x64.Unix.issues.json) test exclusion lists.

#### Test List Format
The tests defined in TopN.Windows.CoreFX.issues.json or the test list specified with the argument `CoreFXTestList` should conform to the following format -
```json
    {
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
    }
```