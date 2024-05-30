# Building and Running CoreCLR Tests

* [Requirements](#requirements)
* [Overview](#overview)
* [Building the Core_Root](#building-the-core_root)
* [Building the Tests](#building-the-tests)
  * [Building an Individual Test](#building-an-individual-test)
  * [Building a Test Directory](#building-a-test-directory)
  * [Building a Test Subtree](#building-a-test-subtree)
  * [Test Executors](#test-executors)
    * [The Standalone Test Runner and Build Time Test Filtering](#the-standalone-test-runner-and-build-time-test-filtering)
    * [Building all tests with the Standalone Runner](#building-all-tests-with-the-standalone-runner)
  * [Building C++/CLI Native Test Components Against the Live Ref Assemblies](#building-ccli-native-test-components-against-the-live-ref-assemblies)
  * [Test Priorities](#test-priorities)
* [Running the Tests](#running-the-tests)
  * [Running Individual Tests](#running-individual-tests)
  * [PAL Tests (macOS and Linux only)](#pal-tests-macos-and-linux-only)
    * [Building PAL Tests](#building-pal-tests)
    * [Running PAL Tests](#running-pal-tests)
* [Modifying Tests](#modifying-tests)
* [Investigating Test Failures](#investigating-test-failures)

This guide will walk you through building and running the CoreCLR tests. These are located within the `src/tests` subtree of the runtime repo.

## Requirements

In order to build CoreCLR tests, you will need to have built the runtime and the libraries (that is, _clr_ and _libs_ subsets). You can find more detailed instructions per platform in their dedicated docs:

* [Windows](/docs/workflow/building/coreclr/windows-instructions.md)
* [macOS](/docs/workflow/building/coreclr/macos-instructions.md)
* [Linux](/docs/workflow/building/coreclr/linux-instructions.md)

For CoreCLR testing purposes, it is more than enough to simply build the _libs_ subset, as far as it concerns the libraries. If you want to know more in-depth about them, they have their own [libraries dedicated docs section](/docs/workflow/building/libraries/README.md).

## Overview

As mentioned in the introduction, all test-building work is done from the `src/tests` folder, so we will consider that our starting point for the rest of this guide.

Building the tests can be as simple as calling the build script without any arguments. This will by default look for a _Debug_ built runtime and _Release_ built libraries. However, by passing the appropriate flags, you can define which configurations you want to use. For example, let's suppose you have a _Checked_ runtime with _Debug_ libraries:

```bash
./src/tests/build.sh checked /p:LibrariesConfiguration=Debug
```

Note that for the libraries configuration, we are passing the argument directly to MSBuild instead of the build script, hence the `/p:LibrariesConfiguration` flag. Also, make sure you use the correct syntax depending on our platform. The _cmd_ script takes the arguments by placing, while the _sh_ script requires them to be with a hyphen.

**NOTE**: Building the whole test suite is a very lengthy process, so it is highly recommended you build individual tests, and/or test subtrees as you need them, to make your workflow more efficient. This is explained in detail later on in this doc.

## Building the Core_Root

The Core_Root folder is some sort of "dev-easy-to-use full build" of the product. It contains the built runtime binaries, as well as the library packages required to run tests. It is required that you build the libraries subset (`--subset libs`) before this command can be run.

Note that, as mentioned in the section above, running the tests build script by default searches the libraries in _Release_ mode, regardless of the runtime configuration you specify. If you built your libraries in another configuration, then you have to pass down the appropriate flag `/p:LibrariesConfiguration=<your_config>`.

The simplest command to generate the _Core\_Root_ from the repository's root path is the following:

```cmd
.\src\tests\build.cmd generatelayoutonly
```

This example assumes you built CoreCLR on _Debug_ mode and the Libraries on _Release_ mode, hence no additional flags are needed. After the build is complete, you will be able to find the output in the `artifacts/tests/coreclr/<OS>.<arch>.<configuration>/Tests/Core_Root` folder.

## Building the Tests

The following subsections will explain how to segment the test suite according to your needs. There are three main scopes of building tests:

* Individual Test Runner
* Full Directory
* Entire Subtree

When no set is specified, almost the whole test suite (`src/tests`) is built.

One of the most important attributes tests have is their **priority**. By default, only those tests with _Priority 0_ are built, regardless of whether it's an individual one, a directory, or a subtree, unless otherwise specified with the `-priority` flag. More info on this in [its dedicated section](#test-priorities). Regardless of which subset you build, all the outputs will be placed in `artifacts/tests/coreclr/<OS>.<arch>.<configuration>`.

**NOTE**: Some tests have native components to them. It is highly recommended you build all of those prior to attempting to build any managed test in the following sections, as it's not a very costly or lengthy process:

```cmd
.\src\tests\build.cmd skipmanaged
```

### Building an Individual Test

To build an individual test, you have to pass the `-test` flag along with the path to the test's `csproj` file to the build script. You can select more than one by repeating the `-test` flag. For example, let's try building a couple JIT tests:

On Windows:

```cmd
.\src\tests\build.cmd test JIT\Methodical\Methodical_d1.csproj test JIT\JIT_ro.csproj
```

On macOS and Linux:

```bash
./src/tests/build.sh -test:JIT/Methodical/Methodical_d1.csproj -test:JIT/JIT_ro.csproj
```

Alternatively, you can call _build_ directly using the `dotnet.cmd/dotnet.sh` script at the root of the repo and pass all arguments directly yourself:

```bash
./dotnet.sh build -c <Your Configuration> src/tests/path/to/test.csproj
```

### Building a Test Directory

To build all the tests contained in an individual directory, you have to pass the `-dir` flag along with the directory's path to the build script. Just like with individual tests, you can select more than one by repeating the `-dir` flag. For example, let's try a couple of folders in the JIT subtree:

On Windows:

```cmd
.\src\tests\build.cmd dir JIT dir Loader
```

On macOS and Linux:

```bash
./src/tests/build.sh -dir:JIT -dir:Loader
```

### Building a Test Subtree

To build a whole subtree, you have to pass the path to the root of the subtree you want with the `-tree` flag. Just like with any other subset, you can select more than one by repeating the `-tree` flag. For example, let's try building all the base services exceptions, and methodical JIT tests:

On Windows:

```cmd
.\src\tests\build.cmd tree baseservices\exceptions tree JIT\Methodical
```

On macOS and Linux:

```bash
./src/tests/build.sh -tree:baseservices/exceptions -tree:JIT/Methodical
```

### Test Executors

We have multiple different mechanisms of executing tests.

Our test entrypoints are generally what we call "merged test runners", as they provide an executable runner project for multiple different test assemblies. These projects can be identified by the `<Import Project="$(TestSourceDir)MergedTestRunner.targets" />` line in their .csproj file. These projects provide a simple experience for running tests. When executing a merged runner project, it will run each test sequentially and record if it passes or fails in an xunit results file. The merged test runner support runtime test filtering. If specified, the first argument to the test runner is treated as a `dotnet test --filter` argument following the xUnit rules in their documentation. Today, the runner only supports the simple form, a substring of a test's fully-qualified name, in the format `Namespace.ContainingTypeName.TypeName.Method`. If support for further filtering options is desired, please open an issue requesting it.

Some tests need to be run in their own process as they interact with global process state, they have a custom test entrypoint, or they interact poorly with other tests in the same process. These tests are generally marked with `<RequiresProcessIsolation>true</RequiresProcessIsolation>` in their project files. These tests can be run directly, but they can also be invoked through their corresponding merged test runner. The merged test runner will invoke them as a subprocess in the same manner as if they were run individually.

#### The Standalone Test Runner and Build Time Test Filtering

Sometimes you may want to run a test with the least amount of code before actually executing the test. In addition to the merged test runner, we have another runner mode known as the "Standalone" runner. This runner is used by default in tests that require process isolation. This runner consists of a simple `try-catch` around executing each test sequentially, with no test results file or runtime test filtering.

To filter tests on a merged test runner built as standalone, you can set the `TestFilter` property, like so: `./dotnet.sh build -c Checked src/tests/path/to/test.csproj -p:TestFilter=SubstringOfFullyQualifiedTestName`. This mechanism supports the same filtering as the runtime test filtering. Using this mechanism will allow you to skip individual test cases at build time instead of at runtime.

#### Building all tests with the Standalone Runner

If you wish to use the Standalone runner described in the [previous section](#the-standalone-test-runner-and-build-time-test-filtering), you can set the `BuildAllTestsAsStandalone` environment variable to `true` when invoking the `./src/tests/build.sh` or `./src/tests/build.cmd` scripts (for example, `export BuildAllTestsAsStandalone=true` or `set BuildAllTestsAsStandalone=true`). This will build all tests that are not directly in a merged test runner's project as separate executable tests and build only the tests that are compiled into the runner directly. If a runner has no tests that are built directly into the runner, then it will be excluded.

### Building C++/CLI Native Test Components Against the Live Ref Assemblies

By default, the _C++/CLI_ native test components build against the _ref pack_ from the SDK specified in the `global.json` file in the root of the repository. To build these components against the _ref assemblies_ produced in the build, pass the `-cmakeargs -DCPP_CLI_LIVE_REF_ASSEMBLIES=1` parameters to the test build. For example:

```bash
./src/tests/build.sh skipmanaged -cmakeargs -DCPP_CLI_LIVE_REF_ASSEMBLIES=1
```

### Test Priorities

As mentioned earlier in this guide, each test has a priority number assigned to them, and only tests with _Priority 0_ are built by default.

Now, here is where things get a little complicated. Test priority filtering is orthogonal to specifying test subsets. This means that even if when specifying tests, directories, and/or subtrees, you have to explicitly provide the priority if the test(s) of interest are not priority 0. Otherwise, the build will skip them.

Another very important thing to keep in mind, is that priorities are accumulative. This means that if for example, you pass `-priority=1` to the build script, all priority 0 _AND_ priority 1 tests get built.

Let's take one of the examples used in the previous subsections. Assume you want to build all _JIT Methodical Div_ tests, including both _pri0_ and _pri1_. This is how the command-line would look:

On Windows:

```cmd
.\src\tests\build.cmd dir JIT\Methodical\divrem\div -priority=1
```

On macOS and Linux:

```bash
./src/tests/build.sh -dir:JIT/Methodical/divrem/div -priority1
```

**NOTE**: Yes, you're seeing it right. The `priority` flag is a bit different between the Windows and macOS/Linux scripts.

## Running the Tests

The simplest way to run in-bundle the tests you've built is by using the `run.cmd/run.sh` script after you've worked with `build.cmd/build.sh`. The running script takes flags very similarly to the build one. Let's suppose you have a _Checked_ runtime you want to test on an _x64_ machine. You'd run all your built tests with the following command-line:

```cmd
.\src\tests\run.cmd x64 checked
```

The `run.cmd/run.sh` scripts also have a number of flags you can pass to set specific conditions and environment variables, such as _JIT Stress_, _GC Stress_, and so on. Run it with only any one of the help flags for more details.

Once your tests are done running, a report will be generated with all the results under the `artifacts/log` folder, and will be named `TestRun_<Arch>_<Configuration>.html`. The tests that failed will be listed in a file called `TestRunResults_<OS>_<Arch>_<Configuration>.err`.

For individual test results and outputs, those are written in a `Reports` folder within the test root's directory (`artifacts/tests/coreclr/<OS>.<Arch>.<Configuration>`). For example, the results for the _JIT Intrinsics Math Round Double_ test shown in [Building an Individual Test](#building-an-individual-test), would be placed in `artifacts/tests/coreclr/<OS>.<Arch>.<Configuration>/Reports/JIT/Intrinsics/MathRoundDouble_ro`.

### Running Individual Tests

After you've built one (or more) tests, the way to run them is by calling the generated `cmd/sh` script alongside them. These scripts take three optional arguments:

* `-debug`: Receives the path of a debugger to run the test under in.
* `-env`: Path to a _.env_ file to specify environment variables to be set for the test. More info about _dotenv_ can be found in [their repo](https://github.com/motdotla/dotenv).
* -coreroot: The path to the Core_Root you wish to use. Note that this flag is mandatory unless you have the `CORE_ROOT` environment variable set. Then, you can omit it and the script will use that one.

If this list of parameters feels familiar, it's because it's virtually the same as the arguments that `corerun` receives. More info on `corerun` in its [how-to-use doc](/docs/workflow/testing/using-corerun-and-coreroot.md).

These scripts have a couple more hidden functionalities, which can be activated by setting their environment variables prior to running the script:

* Run with Crossgen2: `RunCrossGen2=1`
* Build and run as composite: `CompositeBuildMode=1`. Note that this one depends on `RunCrossGen2` being set.

Let's run one of the Intrinsics tests as an example:

On Windows Command Prompt:

```cmd
set CORE_ROOT=<repo_root>\artifacts\tests\coreclr\windows.<Arch>.<Configuration>\Tests\Core_Root
cd path\to\JIT\Intrinsics\MathRoundDouble_ro
.\MathRoundDouble_ro.cmd
```

On macOS/Linux:

```bash
export CORE_ROOT=<repo_root>/artifacts/tests/coreclr/<OS>.<Arch>.<Configuration>/Tests/Core_Root
cd path/to/JIT/Intrinsics/MathRoundDouble_ro
./MathRoundDouble_ro.sh
```

On Powershell:

```powershell
$Env:CORE_ROOT = '<repo_root>\artifacts\tests\coreclr\windows.<Arch>.<Configuration>\Tests\Core_Root'
cd path\to\JIT\Intrinsics\MathRoundDouble_ro
.\MathRoundDouble_ro.cmd
```

Alternatively, instead of setting the _CORE\_ROOT_ environment variable, you can specify it directly to the test's script via the `-coreroot` flag, as mentioned at the beginning of this section:

On Windows:

```cmd
cd path\to\JIT\Intrinsics\MathRoundDouble_ro
.\MathRoundDouble_ro.cmd -coreroot <repo_root>\artifacts\tests\coreclr\windows.<Arch>.<Configuration>\Tests\Core_Root
```

On macOS/Linux:

```bash
cd path/to/JIT/Intrinsics/MathRoundDouble_ro
./MathRoundDouble_ro.sh -coreroot=<repo_root>/artifacts/tests/coreclr/<OS>.<Arch>.<Configuration>/Tests/Core_Root
```

If you want to run an individual test from a test runner, use the filtering capabilities described in the [Test Executors section](#test-executors).

### PAL Tests (macOS and Linux only)

The PAL layer tests are exclusive to Unix-based operating systems. This section will go on how to work with them.

#### Building PAL Tests

Firstly, build them by passing the `paltests` subset to the main build script of the repo:

```bash
./build.sh -s clr.paltests
```

#### Running PAL Tests

Once you're done building them, you can run them all either including or excluding the disabled tests:

_Including Disabled Tests:_

```bash
./src/coreclr/pal/tests/palsuite/runpaltests.sh artifacts/bin/coreclr/<OS>.<Arch>.<Configuration>/paltests
```

_Excluding Disabled Tests:_

```bash
cd artifacts/bin/coreclr/<OS>.<Arch>.<Configuration>
./paltests/runpaltests.sh paltests
```

In order to run only specific tests, edit `paltestlist.txt` in under `src/coreclr/pal/tests/palsuite`, and adjust it to your needs (don't check in those changes though). The test(s) results will be output to `/tmp/PalTestOutput/default/pal_tests.xml`.

To disable tests in the CI, edit `src/coreclr/pal/tests/palsuite/issues.targets` accordingly.

## Modifying Tests

If you need to edit any given test's source code, simply make your changes and rebuild the test project. Then, you can re-run it as needed following the instructions detailed in the sections above.

## Investigating Test Failures

Upon completing a test run with `run.sh/run.cmd`, you may find one or more tests failing. If this is the case, there will be additional files detailing the failures in each test's `Reports` folder (see [Running the Tests](#running-the-tests) for more info regarding reports). There are 2 main files of interest:

* `<Test>.output.txt`: Contains all the information logged by the test.
* `<Test>.error.txt`: Contains all the information reported by _CoreRun_ when the test process crashed.

The test's report will also contain the test command exactly as it run it, so you can investigate it further, either by running the app in the way you see fit and/or as the [Running Individual Tests](#running-individual-tests) details.
