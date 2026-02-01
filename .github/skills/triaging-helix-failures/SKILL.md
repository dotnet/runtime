---
name: triaging-helix-failures
description: Triage Helix test work item failures in dotnet/runtime CI. Use this when investigating test failures, reproducing issues, or routing failures to the correct area owner.
---

# Triaging Helix Test Failures

When investigating a test failure in the dotnet/runtime continuous integration (CI) system, follow this systematic approach to identify the root cause and route the failure to the proper owner.

## Step 1: Identify the Platform Configuration

First, determine the exact platform configuration where the test failed:

### Key Platform Details

1. **Processor architecture**: x86, x64, arm32, or arm64
2. **Operating system**: Windows, Linux, or macOS
3. **OS-specific variants**: For Linux, check if it's a "musl" distribution (e.g., Alpine) or standard glibc

### Finding Docker Container Information

For CI failures, the Docker container mapping is defined in:
- **CoreCLR tests**: [`eng/pipelines/coreclr/templates/helix-queues-setup.yml`](https://github.com/dotnet/runtime/blob/main/eng/pipelines/coreclr/templates/helix-queues-setup.yml)
- **Libraries tests**: [`eng/pipelines/libraries/helix-queues-setup.yml`](https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/helix-queues-setup.yml)

## Step 2: Understand the Test Configuration

Many test runs use non-default configurations to stress various aspects of the system. The job name often reveals the configuration.

### Example Job Name Breakdown

`net11.0-windows-Release-x86-CoreCLR_checked-jitstress1-Windows.10.Amd64.Open`

This indicates:
- **Framework**: net11.0
- **OS**: Windows
- **Libraries config**: Release x86
- **CoreCLR config**: Checked
- **Stress mode**: `DOTNET_JitStress=1`
- **Helix queue**: Windows.10.Amd64.Open

### Critical: Extract All Environment Variables

**ALWAYS check the test console log** for the complete set of environment variables. Don't rely solely on the job name.

#### Windows Example

```
C:\h\w\AE88094B\w\B1B409BF\e>set DOTNET
DOTNET_JitStress=1
DOTNET_TieredCompilation=0
```

You must set **ALL** listed variables when reproducing, not just `DOTNET_JitStress`.

#### Linux/macOS Example

```
+ printenv
+ grep DOTNET
DOTNET_TieredCompilation=1
DOTNET_JitStress=1
DOTNET_DbgMiniDumpName=/home/helixbot/dotnetbuild/dumps/coredump.%d.dmp
DOTNET_DbgEnableMiniDump=1
```

### Additional Configuration Variables

Beyond `DOTNET_*` variables, watch for:

- **`RunCrossGen2=1`**: Triggers crossgen2 compilation of the test
- **`RunningIlasmRoundTrip=1`**: Triggers ildasm/ilasm round-trip test

### Configuration Mapping Sources

The mapping from Azure DevOps pipeline to configuration settings is defined in:
- **CoreCLR tests**: [`eng/pipelines/common/templates/runtimes/run-test-job.yml`](https://github.com/dotnet/runtime/blob/main/eng/pipelines/common/templates/runtimes/run-test-job.yml) (see `scenarios` tags)
- **Libraries tests**: [`eng/pipelines/libraries/run-test-job.yml`](https://github.com/dotnet/runtime/blob/main/eng/pipelines/libraries/run-test-job.yml) (see `scenarios` tags)
- **Variable conversion**: [`src/tests/Common/testenvironment.proj`](https://github.com/dotnet/runtime/blob/main/src/tests/Common/testenvironment.proj)

## Step 3: Download Product and Test Assets

You can either build assets locally or download the exact ones used by CI.

### Using runfo to Download CI Assets

The `runfo` tool is the easiest way to get CI assets:

**Install runfo:**
```sh
dotnet tool install --global runfo
dotnet tool update --global runfo
```

**Get Helix payload:**

1. Go to the Azure DevOps **Tests** view for the failing test
2. Open the **Debug** tab in **Result Details**
3. Find the Helix job ID and work item name:
   ```json
   {
     "HelixJobId": "9d864c94-e6b6-4691-8f7f-36aa9d2053a3",
     "HelixWorkItemName": "JIT.1"
   }
   ```
4. Download the payload:
   ```sh
   runfo get-helix-payload <HelixJobId> <HelixWorkItemName>
   ```

**Resources:**
- [runfo Website](https://runfo.azurewebsites.net)
- [runfo Documentation](https://github.com/jaredpar/runfo/tree/master/runfo#runfo)

### Crash Dumps

If a test crashes and produces a core dump:
- Check the Azure DevOps **Artifacts** page for the crash dump link
- Look for `how-to-debug-dump.md` with debugging instructions (for libraries tests)
- Template source: [`eng/testing/debug-dump-template.md`](https://github.com/dotnet/runtime/blob/main/eng/testing/debug-dump-template.md)

### Building Your Own Assets

Building Debug flavors locally is often preferable for better debugging fidelity, since CI typically uses Checked or Release builds.

## Step 4: Determine the Scope of the Failure

Understanding whether a failure is configuration-specific or widespread is critical for routing.

### Questions to Answer

- Does the test fail only under JIT stress on Windows arm64? → **Likely JIT bug**
- Does the test fail on all platforms and configurations? → **Core platform-independent problem or test issue**

### Method 1: Query Kusto

[Kusto](https://dataexplorer.azure.com/clusters/engsrvprod/databases/engineeringdata) provides CI test execution history. **Note: Microsoft-internal only.**

**Sample query for test failure history:**

```kusto
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

**Usage tips:**
- Adjust `test_name` to match your failing test
- Look for patterns: Does it always fail on ARM? Only with JIT stress?
- Check if the failure is a recent regression or long-standing issue
- Use to determine if a failure is rare or frequent

### Method 2: Manual Reproduction

Download or build product/test assets and reproduce with different configurations.

## Step 5: Diagnose Specific Failure Types

### JIT Stress Failures

JIT stress modes extensively test the CLR JIT. Any test flakiness is more likely to occur in JIT stress runs, but may not be JIT-related.

#### Identifying JIT Bugs

**Clear JIT bugs:**
- **Assertion failures** with `src/coreclr/jit` in the file path:
  ```
  Assert failure(PID 61 [0x0000003d], Thread: 75 [0x004b]): Assertion failed 'node->DefinesLocalAddr(this, size, &lcl, nullptr) && lvaGetDesc(lcl)->lvHiddenBufferStructArg' in 'System.Reflection.TypeLoading.Ecma.EcmaEvent:ComputeEventRaiseMethod():System.Reflection.TypeLoading.RoMethod:this' during 'Morph - Global' (IL size 37; hash 0x941ee672; FullOpts)

  File: /__w/1/s/src/coreclr/jit/gentree.cpp Line: 17913
  ```

**Probable JIT bugs:**
1. First reproduce the failure **with** JIT stress variables set (e.g., `DOTNET_JitStress`, `DOTNET_TieredCompilation`, `DOTNET_JitStressRegs`)
2. Then attempt to reproduce **without** any JIT stress variables
3. If it fails reliably with JIT stress but passes without → **JIT issue**

**Important:** For intermittent failures, you may need to:
- Run the test in a loop
- Apply artificial load to the machine
- Use the exact architecture/OS/OS version/Docker container as CI

### GC Stress Failures

Failures occurring only when `DOTNET_GCStress` is set.

#### Types of GC Stress Failures

1. **Timeouts**: Tests run very slowly under GC stress
2. **GC hole**: JIT or VM doesn't properly report all GC object locations
3. **Bug in GC stress infrastructure**
4. **Bug in the GC itself**

#### Understanding GCStress Values

`DOTNET_GCStress` is a **bitmask**:

| Value | Likely Source |
|-------|---------------|
| `0x1` or `0x2` (thus `0x3`) | VM failures |
| `0x4` or `0x8` (thus `0xC`) | JIT failures |
| `0xF` | Unclear - needs reduction to single bit |

**Best practice:** Reduce failures to a single bit (e.g., `0x4` or `0x8`) for precise diagnosis.

#### Common GC Hole Assert

```
!CREATE_CHECK_STRING(pMT && pMT->Validate())
```

This indicates the JIT or VM isn't properly tracking GC object locations.

## Step 6: Route to the Correct Area Owner

Once you've identified the failure type and scope, route it to the appropriate team.

### Finding Area Ownership

- **Area owners**: [docs/area-owners.md](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md)
- **Auto-tagging policy**: [.github/policies](https://github.com/dotnet/runtime/tree/main/.github/policies)

### Routing Guidelines

| Failure Type | Route To |
|--------------|----------|
| JIT assertion (file path contains `src/coreclr/jit`) | JIT team |
| Fails only with JIT stress, passes without | JIT team |
| GC stress with `0x4` or `0x8` | JIT team |
| GC stress with `0x1` or `0x2` | VM team |
| Platform-specific (only on ARM64, only on Alpine) | Check area-owners.md |
| Test-specific (test code issue) | Test owner per area-owners.md |

## Quick Reference Checklist

When triaging a test failure:

- [ ] Identify platform: architecture, OS, OS version/variant
- [ ] Extract **ALL** environment variables from test console log
- [ ] Determine stress mode: JIT stress, GC stress, or other
- [ ] Check if configuration-specific or widespread (Kusto or manual repro)
- [ ] For JIT stress: Try to reproduce without stress variables
- [ ] For GC stress: Identify the bitmask value and reduce if `0xF`
- [ ] Look for assertion failures and note file paths
- [ ] Download CI assets with `runfo get-helix-payload` (if reproducing)
- [ ] Route to correct area owner based on failure type

## Additional Resources

- [Triaging Failures Documentation](https://github.com/dotnet/runtime/blob/main/docs/workflow/ci/triaging-failures.md)
- [Area Owners](https://github.com/dotnet/runtime/blob/main/docs/area-owners.md)
- [runfo Tool](https://github.com/jaredpar/runfo/tree/master/runfo#runfo)
- [Kusto Database](https://dataexplorer.azure.com/clusters/engsrvprod/databases/engineeringdata) (Microsoft-internal)
- [Crash Dump Debug Template](https://github.com/dotnet/runtime/blob/main/eng/testing/debug-dump-template.md)
