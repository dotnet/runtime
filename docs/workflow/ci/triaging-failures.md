# Triaging CI test failures

This document describes some things to consider when investigating a test failure in the dotnet/runtime continuous integration (CI) system.
The focus is on reproducing the test failure and identifying the proper owner for a failure. Specific guidance is given on how to handle
stress mode test configuration failures, such as failures in a JIT stress test run.

## Area ownership

The ownership of various product areas is detailed [here](../../area-owners.md). The GitHub auto-tagging bot uses the ownership information
in the file [fabricbot.json](../../../.github/fabricbot.json).

## Platform configuration

First, identify the platform the test was run on:
1. The processor architecture (x86, x64, arm32, arm64)
2. The operating system (Windows, Linux, macOS). In some cases, you might need to reproduce a failure under a specific operating system
version, such as a "musl" version of Linux (e.g., Alpine). You might need to reproduce using the same environment used by the CI system.
For Docker environments, the Docker container mapping for coreclr runs is defined [here](../../../eng/pipelines/coreclr/templates/helix-queues-setup.yml)
and for libraries runs is defined [here](../../../eng/pipelines/libraries/helix-queues-setup.yml).

## Test configuration

Many test runs use a non-default product configuration, to allow re-using existing test assets to stress various aspects of the system.
Determine the precise test configuration under which the test has failed. This might be evident from the test job name. For example,
`net7.0-windows-Release-x86-CoreCLR_checked-jitstress1-Windows.10.Amd64.Open` is a libraries test run on Windows with a Release x86 libraries
build, Checked coreclr build, and setting the `COMPlus_JitStress=1` configuration setting, in the `Windows.10.Amd64.Open` Helix queue.

You need to be careful when reproducing failures to set all the correct environment variables. In the above example, if you look at the
test failure console log, you find:

```
C:\h\w\AE88094B\w\B1B409BF\e>set COMPlus 
COMPlus_JitStress=1
COMPlus_TieredCompilation=0
```

Thus, you can see that you also need to set `COMPlus_TieredCompilation=0` when attempting to reproduce the failure.

On non-Windows platforms, you'll see a similar output for the test configuration. E.g.,

```
+ printenv
+ grep COMPlus
COMPlus_TieredCompilation=1
COMPlus_JitStress=1
COMPlus_DbgMiniDumpName=/home/helixbot/dotnetbuild/dumps/coredump.%d.dmp
COMPlus_DbgEnableMiniDump=1
```

## Product and test assets

To reproduce and/or debug a test failure, you'll need the product and test assets. You can either build these using the normal build processes,
or you can download the ones used by the CI system. Building your own is often preferable so you can build a Debug flavor for better
debugging fidelity: the CI typically runs with Checked and sometimes Release components.

If downloading assets from the CI, it's often easiest to use the `runfo` tool:
- [runfo Website](https://runfo.azurewebsites.net)
- [runfo Documentation](https://github.com/jaredpar/runfo/tree/master/runfo#runfo)

To install `runfo` as a .NET CLI global tool:

```sh
dotnet tool install --global runfo
dotnet tool update --global runfo
```

Then use the `runfo get-helix-payload` command.

Note that if a test fails and produces a core (crash) dump, the Azure DevOps "Artifacts" page will include a link to
the crash dump as well as a `how-to-debug-dump.md` file that describes using `runfo` to download the assets and other
tools to do the debugging. This file is built from the template [here](../../../eng/testing/debug-dump-template.md).

## Determining if a failure is due to JIT stress

To extensively test the CLR JIT, there are many automated test runs using the coreclr and libraries tests
but setting additional configuration settings. Because tests are run in so many JIT stress configurations,
any flakiness in a test is much more likely to occur in a JIT stress run and not necessarily be due to the
JIT stress mode itself. This section describes how to determine if a failure is likely due to a JIT bug.

### Source of test configurations

The mapping from Azure DevOps pipeline to configuration settings is set by the `scenarios` tags for coreclr
tests [here](../../../eng/pipelines/common/templates/runtimes/run-test-job.yml) and for libraries tests
[here](../../../eng/pipelines/libraries/run-test-job.yml).
These tags are converted to configuration variables [here](../../../src/tests/Common/testenvironment.proj).

However, all you really need to know is the set of environment variables set, as described above.

### Asserts

Of course, if the failure is due to a JIT assertion failure, the problem is obviously a bug in the JIT.

An example:

```
Assert failure(PID 61 [0x0000003d], Thread: 75 [0x004b]): Assertion failed 'node->DefinesLocalAddr(this, size, &lcl, nullptr) && lvaGetDesc(lcl)->lvHiddenBufferStructArg' in 'System.Reflection.TypeLoading.Ecma.EcmaEvent:ComputeEventRaiseMethod():System.Reflection.TypeLoading.RoMethod:this' during 'Morph - Global' (IL size 37; hash 0x941ee672; FullOpts)

    File: /__w/1/s/src/coreclr/jit/gentree.cpp Line: 17913
    Image: /root/helix/work/correlation/dotnet
```

Note, in particular, the `File:` line includes the path `src/coreclr/jit`.

### Repro without JIT stress modes

For other failures, first attempt to reproduce as it failed in the CI, with the same configuration settings. For intermittent failures,
this might require running the test in a loop, applying artificial load to the machine, running on the exact architecture/OS/OS version/Docker container, etc.

Once the problem can be reproduced, attempt to reproduce the problem without setting any of the JIT stress variables, e.g., do not set:
- `COMPlus_TieredCompilation`
- `COMPlus_JitStress`
- `COMPlus_JitStressRegs`

If the test reliably fails with the JIT stress modes, but passes without, consider it a JIT issue.

### Intermittent test failures

If it is difficult to reproduce an issue, a useful tool is to use [Kusto](https://dataexplorer.azure.com/clusters/engsrvprod/databases/engineeringdata)
to mine the CI test execution history, looking for clues as to how frequently a test fails, and in which configurations.

A sample query to find this information is:

```
//------------------------------
// History of a test failure
//------------------------------
let test_name = "stackoverflowtester";
Jobs
| join WorkItems on JobId
| join TestResults on WorkItemId
| where Type1 contains test_name
  and Status <> "Pass" and (Method == "cmd" or Method == "sh")
| project Queued, Pipeline = parse_json(Properties).DefinitionName, Pipeline_Configuration = parse_json(Properties).configuration,
  OS = QueueName, Arch = parse_json(Properties).architecture, Test = Type1, Result, Duration, Console_log = Message, WorkItemFriendlyName, Method 
| order  by Queued desc
| limit 100
```
