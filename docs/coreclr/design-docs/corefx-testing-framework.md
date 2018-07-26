
# Design

## Test host

To be able to test CoreCLR components in a dotnet CLI context, the testing framework scripts creates a `testhost` folder. The test host contains a minimal dotnet CLI instance, which is used to run the `xunit.console.netcore` test runner supplied with the archived CoreFX test binaries.
The test host depends on `CORE_ROOT` being present, as all `CORE_ROOT` files are copied to the test host. Both `CORE_ROOT` and the test host are built when running `.\build-test.cmd <arch> <build_type> buildtesthostonly` and contain the last built CoreCLR components (found under `\bin\Product\<os>.<arch>.<build_type>\`). The dependency on specific CoreFX packages used to run test host are specified in `\tests\src\Common\CoreFX\CoreFX.depproj` and are versioned to the current CoreFX dependency in CoreCLR. The `dotnet` executable used to run the xunit console runner is copied from the dotnet CLI instance under the `Tools` folder (restored during every build).

## Testing pipeline

Once the necessary components of the test host have been restored or built, a set of pre-built CoreFX binaries is downloaded from the urls specified in TestList.json found in the URL defined in `tests\CoreFX\CoreFXTestListURL[_Linux|_OSX].txt`. Once downloaded the tests are unarchived into each test folders' `xunit.console.netcore` executable is run with the produced `dotnet` CLI instance in the test host. To be able to exclude failing tests from running, the testing pipeline parses the tests defined `TopN.CoreFX.x64.issues.json` and creates a `.rsp` file, the path to which is passed as a command-line argument to `xunit.console.netcore`.
The test scripts start and run multiple instances of the xunit runner in parallel. The test results are output under the `bin\Logs` folder and laterparsed by the CI machines.

## Test binaries

The binaries used to run the tests are stored in Azure Blob Storage.
The current layout of blob storage is as follows – the root of the blob contains three folders for each OS. Each contains archived test binaries (i.e. the output of running `build-test` in CoreFX) and a .json file with a list of tests and their respective URLs. The json schema follows Helix's test list definitions, so that the test runs are compatible with Helix URLs.  

To update them you need access to the secure connection string. Once you have the connection string available, you can use either Azure CLI, Azure Storage Explorer or the Blob Storage API.

# Limitations and Future Development

## Full architecture/OS matrix support

This is the big lacking feature at the moment.

## Exclusion list

Defining all exclusions in a single file should be fairly straightforward. The schema is meant to be easily extensible - adding fields for exclusions on specific architectures and platforms would require a small change in how we generate `.rsp` files for the xunit test runner.

## Blob Storage

### Test Binary Refresh

A pain point of using cached test binaries is the lack of an established regular mechanism to refresh the test binaries in blob storage. Currently the only way to refresh the tests is to establish a set of CoreFX test binaries (either via building them locally or by downloading them from Helix) and manually uploading them to blob storage. Any new test projects have to be manually added to the TestList.json at the root of every OS-specific directory.  

A straightfoward way to implement this would be with a bot similar to maestro-bot, i.e. every time we update the CoreFX reference in CoreCLR, maestro-bot can send a request to refresh binaries. The request would be sent to an agent, which would fetch the latest built test binaries from Helix and update them in blob storage. Subsequently helper scripts can be used to generate test exclusions from failed tests so that CI is green while bugs are triaged.

### Layout

The current layout of blob storage is inelegant and not easy to support. We can define two potential paths for improvement:

+ Remove the .txt files with test URLs. Add a simple request handler between the blob storage and the testing framework to resolve URLs automatically for every supported platform/OS combo. This is where the binary refresh logic can also live.

+ The blob storage features above would duplicate some Helix functionality. The test list in each blob storage folder copies Helix’s JSON format. This was done intentionally to be compatible with the produced test payloads. We can use a Helix API to integrate test binary refresh with something similar to dotnet-maestro-bot and copy test binaries from a Helix run on a regular interval.

## xUnit Console Runner

Currently we depend on xunit.netcore.console.exe (our own implementation of an xUnit console runner) being present in the test payload. With the efforts going on in [the Arcade transition project](https://github.com/dotnet/corefx/projects/3) changes to the CoreFX test build format might happen, in which case we would need to start maintaining our one CoreFX console runner. The reason for this is the lack of fine-grained test exclusion capabilities in earlier xUnit versions (before [test exclusions were added to core xUnit](https://github.com/xunit/xunit/pull/1734)). The testing framework implementation assumes that the console runner accepts exclusion parameters (e.g. -skipmethod; -skipclass ), which are the basis of being able to selectively disable individual test methods. If the binaries maintain backwards-compatibility (extremely likely) with the console runner, then we can simply call an instance of xunit.netcore.console.exe from the test host directory.

## Paralellization

The testing framework currently does not utilize Helix's parallelization capabilities - all tests are run on a single  machine. Being able to execute select tests on different machines would speed up time to completion for most CI jobs.

Currently there is no convenient way to split up the test list - the most straightforward implementation would involve simply specifying the total number of machines and the current machine index and passing them to the testing framework, which would then only execute the specified portion.

A more sophisticated approach would be to schedule the tests according to the average time and ensuring an even mix of long- and short-running test assemblies per machine.

## JIT Stress and OPT Scenarios

JIT Stress and Opt variations rely on environmental variables being set. This is not currently implemented. A big roadblock here is that GCStress would also stress the testing harness itself.

## Replacing [run-corefx-tests.py](https://github.com/dotnet/coreclr/blob/master/tests/scripts/run-corefx-tests.py)

Realistically if all of the above is implemented the testing scripts are robust enough to stably handle all of the testing scenarios which the python script handles. More user-friendly features would be useful for determining irregular causes of failures. For example if a test runner crashes an error is reported and the test suite fails, but a developer would have to read through the execution logs to determine which test has crashed.

## Random bits and pieces

A few minor quality of life improvements could be made. One is checking for the existence of already downloaded binaries (particularly assuming that they won’t be updated too often) when testing. The second is adding a download retry mechanism. Currently if blob storage times out, a failure is reported and execution stopped. Under heavier load this might cause issues. Additionally, repro-ing failures is currently a bit cumbersome - a developer would have to edit `TopN.CoreFX.x64.issues.json` to only include the test assembly name, which they want to run. They could for example  specify a name of a test on the command line and have only that test be downloaded and run.
