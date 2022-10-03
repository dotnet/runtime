# Triaging CI test failures

This document describes some things to consider when investigating a test failure in the dotnet/runtime continuous integration (CI) system.
The focus is on reproducing the test failure and identifying the proper owner for a failure. Specific guidance is given on how to handle
stress mode test configuration failures, such as failures in a JIT stress test run.

## Area ownership

One goal of failure investigation is to quickly route failures to the correct area owner. The ownership of various product areas
is detailed [here](../../area-owners.md). The GitHub auto-tagging bot uses the ownership information
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

You might need to set variables in addition to the `COMPlus_*` (equivalently, `DOTNET_*`) variables. For example, you might see:
```
set RunCrossGen2=1
```
which instructs the coreclr test wrapper script to do crossgen2 compilation of the test.

Similarly,
```
set RunningIlasmRoundTrip=1
```
triggers an ildasm/ilasm round-trip test (that is, the test assembly is disassembled, then re-assembled, then run).

And,
```
set DoLink=1
```
triggers ILLink testing.

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

Then use the `runfo get-helix-payload` command with the job name and workitem name. You can get these from the `Debug` tab of the
`Result Details` for a specific test of interest in the Azure DevOps `Tests` view. It looks something like:
```json
{
  "HelixJobId": "9d864c94-e6b6-4691-8f7f-36aa9d2053a3",
  "HelixWorkItemName": "JIT.1"
}
```

Note that if a test fails and produces a core (crash) dump, the Azure DevOps "Artifacts" page will include a link to
the crash dump. It will also include a `how-to-debug-dump.md` file that describes using `runfo` to download the assets and other
tools to do the debugging (but currently only for libraries test runs).
This file is built from the template [here](../../../eng/testing/debug-dump-template.md), which has some useful information.

## Determining the most general case of a failure

A single test may run on many platforms and in many configurations, as described above. It's important to understand if a
failure is specific to a single configuration or platform, or is common across many configurations and platforms. For example,
if a test fails only under JIT stress on Windows arm64, it's almost certainly a JIT bug, and knowing it fails only on
the Windows arm64 platform expedites the bug fix investigation. However, if a test fails on all platforms and under all
configurations, then it indicates a core, platform-independent problem, either or in the test itself or in the product.

There are two useful ways to determine the breadth of a failure:
1. Look at Azure DevOps pipelines test failure information for many test runs, typically using the Azure Data Explorer (Kusto) database
of test results, or
2. Manually reproduce the test failure, by downloading the product and test assets or building them, as described above.

### Kusto

[Kusto](https://dataexplorer.azure.com/clusters/engsrvprod/databases/engineeringdata) is a useful tool
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

Note that if a test failure is a recent regression, there may not be many results; you may need to manually reproduce the failure.

Using Kusto data is also useful to help determine if a failure is rare, and look for a pattern of failures over time.

Also note that this database is currently only accessible internal to Microsoft.

## Example: JIT stress failures

To extensively test the CLR JIT, there are many automated test runs using the coreclr and libraries tests
but setting additional configuration settings. Because tests are run in so many JIT stress configurations,
any flakiness in a test is much more likely to occur in a JIT stress run and not necessarily be due to the
JIT stress mode itself. This section describes how to determine if a failure is likely due to a JIT bug.

### Source of test configurations

Note that all you really need to know is the set of environment variables set, as described above.

However, here are some useful links if you need to dig deeper into what configuration settings are used, and how.
The mapping from Azure DevOps pipeline to configuration settings is set by the `scenarios` tags for coreclr
tests [here](../../../eng/pipelines/common/templates/runtimes/run-test-job.yml) and for libraries tests
[here](../../../eng/pipelines/libraries/run-test-job.yml).
These tags are converted to configuration variables [here](../../../src/tests/Common/testenvironment.proj).

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
this might require running the test in a loop, applying artificial load to the machine, running on the exact
architecture/OS/OS version/Docker container, etc. Note that this applies to reproducing intermittent failures with or without stress
modes set.

Once the problem can be reproduced, attempt to reproduce the problem without setting any of the JIT stress variables, e.g., do not set:
- `COMPlus_TieredCompilation`
- `COMPlus_JitStress`
- `COMPlus_JitStressRegs`

If the test reliably fails with the JIT stress modes, but passes without, consider it a JIT issue.

## Example: GC stress failures

Failures that occur only when the `COMPlus_GCStress` variable is set are called "GCStress failures". There are several general kinds
of failures:
- Timeouts: tests run under this stress mode run very slowly.
- A "GC hole": the JIT (or sometimes VM) doesn't properly report all GC object locations to the system.
- A bug in the GC stress infrastructure.
- A bug in the GC itself.

Note the value `COMPlus_GCStress` is set to is a bitmask. Failures with 0x1 or 0x2 (and thus 0x3) are typically VM failures.
Failures with 0x4 or 0x8 (and thus 0xC) are typically JIT failures. Ideally, a failure can be reduced to fail with only a single
bit set (that is, either 0x4 or 0x8, which is more specific than just 0xC). That is especially true for 0xF, where we don't know if
it's likely a VM or a JIT failure without reducing it.

A commonly seen assert indicating a "GC hole" is `!CREATE_CHECK_STRING(pMT && pMT->Validate())`.
