---
name: aspnet-benchmark
description: Run the TechEmpower ASP.NET Core benchmarks (PlatformBenchmarks) against a locally-built dotnet/runtime, either locally with an SDK overlay or on external infrastructure using Crank, to validate the end-to-end impact of a runtime change (e.g. sockets, io_uring, GC, JIT) under realistic HTTP load. Use this when asked to benchmark, load-test, or profile an ASP.NET Core / Kestrel scenario, or to compare an env-var-gated feature (e.g. `DOTNET_USE_IO_URING`) end-to-end.
---

# Running the TechEmpower (PlatformBenchmarks) Benchmarks

Use the TechEmpower `PlatformBenchmarks` app from the [`aspnet/Benchmarks`](https://github.com/aspnet/Benchmarks) repository to validate runtime changes under realistic HTTP server load. Microbenchmarks can isolate an operation's cost, but end-to-end ASP.NET benchmarks show how a change affects throughput, latency, and CPU usage under concurrent request/response processing.

These benchmarks are useful for changes to the socket/IO stack, GC, JIT, and other components whose impact depends on application behavior. Both workflows below run the benchmark against a locally-built runtime: start with local measurements to validate ideas, then use external infrastructure for final validation when access is available.

## Common Prerequisites

Both workflows require a validated local dotnet/runtime build containing the change under test, targeting the benchmark machine's OS and architecture: the development machine for local runs, or the application agent for Crank runs. **Both the native runtime and managed libraries must be built in Release.**

Reuse existing validated artifacts when available; do not rebuild merely to stage binaries. Consult the `build-and-test` skill before building, and never build when the user requested documentation or explicitly prohibited builds. If a build is needed, build runtime and libraries in Release with `-c release`, not just `-rc release`: the latter only forces the runtime to Release and leaves libraries at their default (`Debug`), producing a mismatched testhost.

```bash
./build.sh clr+libs -c release
```

Confirm the artifacts correspond to the intended source revision. Switching Git branches does not update existing build outputs. Record source commits, any uncommitted changes, configuration, and binary hashes.

## Choosing a Benchmark

For both workflows, prefer `/json` for representative maximum-throughput comparisons. The standard TechEmpower `/plaintext` scenario uses HTTP pipelining (16 requests batched per connection), whereas `/json` sends one request per round trip and better represents normal request/response traffic. Their throughput numbers are not directly comparable.

Both `/json` and `/plaintext` can run locally without a database. Other benchmarks, such as fortunes, single/multiple queries, and updates, require setting up a database. These can also run locally, but database setup is outside the scope of this document.

## Benchmarking a Local Runtime Build Locally

**Always validate ideas locally first.** This workflow provides quick verification on the development machine and requires no VPN or access to external infrastructure. Drive the application with `wrk`, or [`bombardier`](https://github.com/codesenberg/bombardier) on Windows (because `wrk` is Linux/macOS-only).

Treat local results as an initial signal, not a definitive performance measurement: the application, load generator, and other processes share the same machine and compete for CPU, memory, and other resources. This contention can distort throughput and latency, so local results may not predict performance on dedicated benchmark machines.

### Prerequisites

- A load-generation tool: on Linux, `wrk` (`sudo apt install wrk` or build from [source](https://github.com/wg/wrk)). `wrk` doesn't build/run on Windows; use [`bombardier`](https://github.com/codesenberg/bombardier) instead (`go install github.com/codesenberg/bombardier@latest`, or download a prebuilt binary from its [releases page](https://github.com/codesenberg/bombardier/releases)) - the examples below use `wrk` syntax, with the `bombardier` equivalent noted alongside.
- The [`aspnet/Benchmarks`](https://github.com/aspnet/Benchmarks) repository, which contains the TechEmpower and other benchmark apps.

### Step 1: Clone the Benchmarks Repository

```bash
git clone https://github.com/aspnet/Benchmarks.git
```

The relevant app is `src/BenchmarksApps/TechEmpower/PlatformBenchmarks` — a raw Kestrel `HttpApplication` implementation of the TechEmpower benchmark suite (JSON serialization, plaintext, fortunes, single/multiple queries, updates). It targets the latest `net*.0` TFM.

### Step 2: Publish the App Against the Repo's Own SDK

Build/publish the app with the dotnet/runtime repo's own SDK (`<runtime-repo>/.dotnet/dotnet`), not a system-wide SDK, so the app targets the same TFM as your local build:

```bash
cd Benchmarks/src/BenchmarksApps/TechEmpower/PlatformBenchmarks
<runtime-repo>/.dotnet/dotnet build -c Release
```

This produces `bin/Release/<tfm>/PlatformBenchmarks.dll`.

### Step 3: Make the Repo's SDK Run Against Your Local Runtime Build

`PlatformBenchmarks` needs `Microsoft.AspNetCore.App`, which the freshly-built `artifacts/bin/testhost` does **not** contain (it only has `Microsoft.NETCore.App`) — running it directly via `testhost`'s `corerun`/`dotnet` fails with a "no framework found" error. The repo SDK's own `dotnet` (under `<runtime-repo>/.dotnet`) already has `Microsoft.AspNetCore.App`, so the simplest way to combine "the ASP.NET Core framework" with "your locally-built `Microsoft.NETCore.App`" is to temporarily overlay the SDK's shared `Microsoft.NETCore.App/<version>` folder with the testhost's freshly-built one:

```bash
SDK_VERSION='<version>'
SDK_FX="<runtime-repo>/.dotnet/shared/Microsoft.NETCore.App/$SDK_VERSION"
TESTHOST_FX="<runtime-repo>/artifacts/bin/testhost/<tfm>-<os>-Release-<arch>/shared/Microsoft.NETCore.App/$SDK_VERSION"

# ALWAYS back up the original SDK framework files first, so they can be restored exactly.
# Use a fresh, uniquely-named backup directory every time: a fixed, reused path is unsafe
# if a previous run was interrupted before restoring — `mkdir -p` would silently succeed
# and the overlay step below would overwrite the only pristine backup with mutated files.
BACKUP_DIR="/tmp/netcoreapp_sdk_backup.$$"
mkdir "$BACKUP_DIR"   # fails loudly (no -p) if this exact path somehow already exists
rsync -a --delete "$SDK_FX"/ "$BACKUP_DIR"/

# Record the pre-overlay checksum so Step 6's restore can be verified against it.
md5sum "$SDK_FX"/System.Private.CoreLib.dll

# Overlay with the local build.
rsync -a --delete "$TESTHOST_FX"/ "$SDK_FX"/
```

**This mutates the repo's own SDK shared framework in place.** Never skip the backup step, and always restore it when done (Step 6) — leaving it mutated will silently break every other use of that SDK on the machine. Keep `$BACKUP_DIR` and the recorded checksum around until Step 6 has verified the restore.

### Step 4: Run the App and Verify It's Serving Requests

```bash
cd Benchmarks/src/BenchmarksApps/TechEmpower/PlatformBenchmarks/bin/Release/<tfm>
<runtime-repo>/.dotnet/dotnet exec PlatformBenchmarks.dll --urls http://127.0.0.1:5000 &
sleep 5
curl -sS http://127.0.0.1:5000/json
```

This leaves the terminal's working directory inside `PlatformBenchmarks/bin/Release/<tfm>` for the rest of the session. Any later step that references a path relative to `<runtime-repo>` (such as copying `crossgen2` below) must either `cd` back to `<runtime-repo>` first or use an absolute/`<runtime-repo>`-anchored path — a bare relative path resolves under this benchmark output directory instead and the copy silently fails or copies nothing.

Set whatever env var/`AppContext` switch you're comparing (e.g. `DOTNET_USE_IO_URING=1`/`=0`) *before* starting the process — it's read once at startup.

### Step 5: Run `wrk` (or `bombardier` on Windows) Against It

```bash
wrk -t12 -c256 -d15s --latency http://127.0.0.1:5000/json
```

- `-t`: number of `wrk` threads — a good default is the machine's core count.
- `-c`: number of concurrent connections — 256 is a reasonable default for a many-concurrent-connections scenario.
- `-d15s`: run duration; 15s is normally enough to get a stable number.
- `--latency`: also reports the latency percentile breakdown, not just aggregate throughput.

On Windows, use `bombardier` instead, with equivalent options:

```powershell
bombardier -c 256 -d 15s -l http://127.0.0.1:5000/json
```

- `-c`: concurrent connections (same meaning as `wrk -c`).
- `-d`: run duration (same meaning as `wrk -d`).
- `-l`: print the latency percentile breakdown (equivalent to `wrk --latency`).
- `bombardier` has no direct equivalent to `wrk -t`; it manages its own worker goroutines internally.

Run **at least two** runs per configuration (there's meaningful run-to-run variance) and report both, along with p50/p99 latency, not just a single throughput number. Present the results in a table.

Repeat Steps 4-5 for each configuration being compared (e.g. once with the env var on, once with it off), reusing the same overlay — only the running process needs to be restarted between configurations, not the overlay.

### Step 6: Clean Up

1. Kill the server process(es) — find the actual `dotnet exec ... PlatformBenchmarks.dll` PID (not the shell that launched it) with `pgrep -af PlatformBenchmarks` and `kill <pid>`.
2. **Restore the SDK's shared framework from the backup** and verify it via checksum before considering the machine clean:

   ```bash
   rsync -a --delete "$BACKUP_DIR"/ "$SDK_FX"/
   md5sum "$SDK_FX"/System.Private.CoreLib.dll   # must match the checksum recorded in Step 3 before overlaying
   ```

3. Remove `$BACKUP_DIR` and any log files once restoration is verified.

### Profiling a Benchmark Run with `perfcollect`

If the load-test numbers show a difference (or don't, and you need to know why) and you need native + managed call stacks, use `perfcollect` rather than a raw `perf record` — it also captures LTTng CLR events and can resolve JIT-generated code, not just precompiled/R2R code. `perfcollect` itself is Linux-only (it relies on LTTng/perf), regardless of which platform you used to drive the load test.

- Tool and full instructions: <https://aka.ms/perfcollect>. Also see the repo's own [Linux Performance Tracing](../../../docs/project/linux-performance-tracing.md) doc.
- Basic flow (needs two terminals — one to drive `perfcollect`, one to run the server):

  ```bash
  curl -OL https://aka.ms/perfcollect
  chmod +x perfcollect
  sudo ./perfcollect install   # one-time, installs LTTng/perf prerequisites

  # [App terminal] enable perf maps for JIT-generated code before starting the process:
  export DOTNET_PerfMapEnabled=1
  export DOTNET_EnableEventLog=1
  <runtime-repo>/.dotnet/dotnet exec PlatformBenchmarks.dll --urls http://127.0.0.1:5000 &

  # [Trace terminal] start collection, then drive load with wrk from a third terminal:
  sudo ./perfcollect collect webapp_trace

  # Ctrl+C to stop once wrk has run long enough (a few seconds under load is enough for a CPU investigation).
  ```

  This produces `webapp_trace.trace.zip`, analyzable with PerfView (<https://aka.ms/perfview>) or `perf report`/`perf script` directly on the underlying `.perf.data`.

- Raising `kernel.perf_event_paranoid` may be required for an unprivileged `perf record`/`perfcollect` to work at all:

  ```bash
  sudo sysctl -w kernel.perf_event_paranoid=-1   # temporary; restore the original value afterward
  ```

  Always restore the original value once profiling is done — don't leave a relaxed `perf_event_paranoid` on a shared machine.

#### ⚠️ Symbol Resolution Warnings

- **Precompiled (R2R/crossgen) framework symbols are not resolved automatically.** `perfcollect` needs a `crossgen2` tool matching the exact runtime build to map native framework code back to method names; without it, framework frames show up unresolved/hex-only in the trace. When profiling a **locally-built** runtime you don't need to hunt one down — your own build already produced the exact matching binary at `artifacts/bin/crossgen2_publish/<arch>/<config>/crossgen2`. Copy (or symlink) it next to `libcoreclr.so` in the directory you're actually running from (e.g. the testhost/SDK-overlay shared framework folder from Step 3) before collecting:

  ```bash
  # Use a path anchored to <runtime-repo> (or `cd` back there first) — a bare relative
  # path resolves under the PlatformBenchmarks output directory left by Step 4 instead.
  cp <runtime-repo>/artifacts/bin/crossgen2_publish/x64/Release/crossgen2 "$SDK_FX"/
  ```

  For a runtime you didn't build yourself, see the "Resolving Framework Symbols" section of [linux-performance-tracing.md](../../../docs/project/linux-performance-tracing.md) instead (it walks through obtaining a matching `crossgen2` via a self-contained publish).
- **Native runtime frames (`libcoreclr.so`, `libclrjit.so`, etc.) resolve out of the box, as long as you don't move or discard the build's `.dbg` files.** A Release native build ships each `.so` *stripped*, but the matching, un-stripped `libXyz.so.dbg` is written right alongside it in the same output directory (e.g. `artifacts/bin/coreclr/<rid>.<config>/`), linked via its `.gnu_debuglink` section and a matching Build ID. `perf`/`perfcollect` follow that link automatically as long as the `.dbg` file stays next to its `.so` — don't clean up or selectively copy only the `.so` files into a deployment layout, or you'll silently lose native symbolication.
- **JIT-generated (Tier0/Tier1) symbols require `DOTNET_PerfMapEnabled=1` to be set *before* the process starts.** If you forget this and only realize partway through a run, the trace from that run cannot be fixed after the fact — kill the process, re-export the env var, and restart it before collecting again.
- Even after doing all of the above, expect *some* residual unresolved frames from components you didn't build locally (e.g. the OS's own libraries, or a `crossgen2`/`.dbg` mismatch if you mixed binaries from different build configurations) — this doesn't mean the whole trace is useless; managed app-level and JIT-emitted frames still resolve correctly via the perf map regardless.
- `DOTNET_PerfMapEnabled=1` has a real (if usually small) overhead of its own — don't leave it set for the throughput (`wrk`/`bombardier`) runs themselves, only for the dedicated profiling run.

### Common Pitfalls

- **Running via `artifacts/bin/testhost` directly fails** with "no framework found" — that layout only has `Microsoft.NETCore.App`, not `Microsoft.AspNetCore.App`. Use the SDK-overlay approach in Step 3 instead.
- **Forgetting to set the env var before starting the process** — most feature switches (like `DOTNET_USE_IO_URING`) are read once at startup, so changing it and re-`curl`-ing the same running process has no effect.
- **Comparing a single run per configuration** — throughput varies run to run; always do at least two runs per configuration.
- **Leaving the SDK shared framework overlaid** — always restore it (Step 6) and verify via checksum; a stale/mismatched overlay silently breaks unrelated work on the same machine later.

## Benchmarking a Local Runtime Build on External Infrastructure with Crank

**Use external infrastructure for final validation after testing locally.** The .NET benchmarking infrastructure described here requires VPN access and is unavailable to external contributors. Runs take substantially longer than local checks, and the infrastructure is not always available; do not depend on it for the initial iteration loop.

Use [Crank](https://github.com/dotnet/crank) to deploy `PlatformBenchmarks` with selected locally-built runtime binaries to a remote application agent; a separate load agent drives HTTP requests without competing for the application's machine resources. These access restrictions apply to the shared infrastructure, not to Crank itself: external contributors can use Crank with their own agents.

References:

- [Crank documentation](https://github.com/dotnet/crank/blob/main/docs/README.md) and [getting started](https://github.com/dotnet/crank/blob/main/docs/getting_started.md): controller installation, agents, scenarios, and profiles.
- [Selecting .NET versions](https://github.com/dotnet/crank/blob/main/docs/dotnet_versions.md): framework, SDK, runtime, and ASP.NET version overrides.
- [PlatformBenchmarks configuration](https://github.com/aspnet/Benchmarks/blob/main/scenarios/platform.benchmarks.yml): `json` scenario, `application` and `load` jobs, and load variables.
- [Linux performance tracing](../../../docs/project/linux-performance-tracing.md): native and managed symbol resolution.

### 1. Establish the Inputs

- Use an installed Crank controller and reachable application/load agents. Prefer separate machines for throughput measurements so load generation does not compete with the server for CPU.
- Select a PlatformBenchmarks configuration and an authorized machine profile. The public configuration includes profiles for particular infrastructure; choose or define one for the actual agents rather than assuming those machines are accessible. The getting-started guide shows how profiles assign `application` and `load` endpoints.
- Pin the benchmark source revision and exact SDK, runtime, and ASP.NET versions for comparisons. Do not allow `main`, `latest`, or `edge` to change dependencies between runs. Ensure the benchmark TFM and selected versions are compatible with the local runtime overlay.

Keep stock ASP.NET binaries unless the experiment explicitly includes a local ASP.NET build. A runtime-only comparison must not accidentally include a modified Kestrel transport.

### 2. Choose the Smallest Coherent Binary Subset

**Do not upload the entire testhost, SDK, or artifacts directory.** Start with the changed component, then add only dependencies required for its implementation, managed/native ABI, or runtime compatibility. This reduces upload time and makes the experiment attributable.

Inspect the source diff and relevant project outputs before choosing files:

| Change under test | Files to consider for a Linux overlay |
|---|---|
| Managed library only, such as `System.Net.Sockets` | The changed implementation `.dll` and matching `.pdb`; additional implementation dependencies only if needed. |
| Native PAL or interop contract | The affected native library, such as `libSystem.Native.so`, its matching `.so.dbg`, and any managed assembly changed with that ABI. |
| CoreLib, including ThreadPool | `System.Private.CoreLib.dll` and `.pdb`, with compatible VM/JIT binaries. When using a locally-built CoreLib against a different published runtime, use the matching `libcoreclr.so` and `libclrjit.so` plus their `.dbg` files rather than assuming internal contracts match. |
| JIT only | `libclrjit.so` and `.so.dbg` if compatible with the selected VM's JIT/EE interface; otherwise include the matching runtime components. |
| VM or GC | `libcoreclr.so` and `.so.dbg`, plus matching CoreLib/JIT when their internal contracts require it. |
| Added APIs or type forwarders | Any additional deployable implementation/forwarder assemblies needed by consumers, such as `System.Runtime.dll` or `System.Threading.ThreadPool.dll`, with their symbols. **Do not deploy reference assemblies from `ref/`.** |
| Explicitly requested local ASP.NET change | The affected ASP.NET implementation assemblies and symbols, for example `Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.dll` and `.pdb`, plus required dependencies. |

These are selection rules, not a universal file list. A Socket-only change may need just two files; an experiment spanning Socket, CoreLib, and native code needs a larger coherent set. Do not omit a required dependency merely to minimize file count.

The prepared shared-framework directory is a useful source of coherent runtime files:

```text
artifacts/bin/testhost/<tfm>-<os>-Release-<arch>/shared/Microsoft.NETCore.App/<version>/
```

Determine the actual path from the build; do not assume the directory version equals the benchmark TFM. For incremental component builds, verify that the testhost contains the updated files, or stage the verified component outputs directly.

#### Stage an immutable overlay

For a managed Socket-only experiment with no other changed dependencies:

```bash
SHARED='/absolute/path/to/the/validated/shared/Microsoft.NETCore.App/<version>'
OVERLAY=/absolute/path/to/runtime/artifacts/tmp/crank-sockets-candidate

# A new directory for each binary variant; fail if it already exists.
mkdir "$OVERLAY"
cp "$SHARED/System.Net.Sockets.dll" "$SHARED/System.Net.Sockets.pdb" "$OVERLAY/"

# Keep the manifest outside the upload directory.
(cd "$OVERLAY" && sha256sum *) > "$OVERLAY.sha256"
```

Create the parent directory first if necessary. Extend the explicit copy list according to the table; never replace it with a wildcard over the full testhost. Preserve each overlay and its symbols until the comparison and trace analysis are complete. Do not overwrite an overlay while a run uses it.

#### Include symbols from the exact same build

- Managed assemblies: retain their matching portable `.pdb` files.
- Linux native libraries: keep each matching `.so.dbg` alongside its `.so`, preserving names. A Release `.so` is normally stripped; the matching debug file supplies native symbols. Windows builds need the corresponding native PDBs instead.
- A PDB or `.dbg` from another build is not interchangeable, even if the assembly version or filename is identical. Preserve hashes and native build IDs for later analysis.
- ReadyToRun framework frames can require additional matching symbol-generation tooling. Follow the Linux tracing guide when frames remain unresolved; do not upload all build tools speculatively or assume managed PDBs alone resolve all native code.

### 3. Upload the Overlay and Run JSON

Crank's `--application.options.outputFiles` uploads local files into the application's published output (the `published/` folder produced by `dotnet publish` on the agent) *after* that build completes. It is different from uploading source files or build inputs. Leave the controller's SDK/shared frameworks untouched; no in-place SDK replacement is needed.

**`PlatformBenchmarks` publishes framework-dependent by default, and `outputFiles` alone does not override the shared framework for a framework-dependent app.** A framework-dependent publish's own `published/` output does not contain a private copy of `Microsoft.NETCore.App`/`Microsoft.AspNetCore.App` at all — the host resolves `System.Private.CoreLib.dll`, `libcoreclr.so`, `libclrjit.so`, and other framework assemblies (including `System.Net.Sockets.dll`) exclusively from the shared framework directory selected by `--application.runtimeVersion`/`--application.aspNetCoreVersion`, which Crank provisions separately and does not touch when applying `outputFiles`. Dropping same-named files into `published/` in this mode has no effect: the run silently benchmarks the stock shared-framework binaries instead of the uploaded ones, for every row in the table above except application-local, non-framework assemblies.

To make the overlay actually take effect, publish the job **self-contained** instead, so the local runtime bits become part of `published/` itself and the later `outputFiles` copy overwrites them in place:

```bash
CONFIG=/absolute/path/to/platform.benchmarks.yml
PROFILE='<configured-machine-profile>'
TFM='<benchmark-tfm>'
SDK_VERSION='<exact-sdk-version>'
RUNTIME_VERSION='<exact-runtime-version>'
ASPNET_VERSION='<exact-aspnet-version>'
RUN=/absolute/path/to/results/json-candidate-a

crank \
  --config "$CONFIG" --scenario json --profile "$PROFILE" \
  --application.framework "$TFM" \
  --application.selfContained true \
  --application.sdkVersion "$SDK_VERSION" \
  --application.runtimeVersion "$RUNTIME_VERSION" \
  --application.aspNetCoreVersion "$ASPNET_VERSION" \
  --application.options.outputFiles "$OVERLAY/*" \
  --application.environmentVariables DOTNET_EnableEventPipe=0 \
  --application.beforeScript '/bin/sh -c "sha256sum published/System.Net.Sockets.dll published/System.Net.Sockets.pdb"' \
  --application.options.downloadOutput true \
  --application.options.downloadOutputOutput "$RUN-output" \
  --application.options.downloadBuildLog true \
  --application.options.downloadBuildLogOutput "$RUN-build" \
  --load.variables.connections 512 \
  --load.variables.warmup 90 \
  --load.variables.duration 60 \
  --json "$RUN.json"
```

Create the results directory beforehand and use a unique run name for every launch. Replace angle-bracket placeholders before executing. Quote `"$OVERLAY/*"` so Crank, not the shell, expands the upload pattern. Extend the `sha256sum` list to cover the complete selected payload and compare the downloaded output with the local manifest.

`--application.selfContained true` still needs `--application.runtimeVersion`/`--application.aspNetCoreVersion` pinned to the versions matching your overlay's ABI, since the self-contained publish step is what brings those shared-framework files into `published/` in the first place — `outputFiles` only replaces specific files afterward, it doesn't provision the rest of the runtime. Confirm the `beforeScript`'s `sha256sum` output in the build log matches the overlay's own manifest to prove the substitution actually landed, and check the downloaded build log for the `--self-contained` publish flag.

The example's `published/` paths and `/bin/sh` command are for the standard Linux PlatformBenchmarks job; adapt them if the job uses a different layout or OS. Confirm the build logs show the intended framework versions and that the application uses the deployed replacements, not an incompatible or separately located shared framework. File presence alone is not proof that a module was loaded; use module paths/build IDs from a diagnostic trace when investigating binding.

#### Windows controller with Linux binaries in WSL

The payload must match the **agent**, not the controller OS. A Windows controller can upload Linux binaries directly from a WSL UNC path, without copying an entire build onto a Windows drive:

```powershell
crank.exe --config C:\benchmarks\platform.yml --scenario json --profile <profile> `
  --application.options.outputFiles '\\wsl.localhost\Ubuntu\home\user\runtime\artifacts\tmp\crank-sockets-candidate\*'
```

This illustrates only the path syntax; retain the version, load, environment, and result options from the full command. Use paths accessible to the controller for configurations, uploads, and downloaded traces. From WSL, `wslpath -w <path>` converts a path for `crank.exe`.

### 4. Set Environment Variables on the Application Job

Repeat `--application.environmentVariables NAME=VALUE` for each variable. Setting a variable only in the controller's shell does **not** configure the remote application:

```text
--application.environmentVariables DOTNET_USE_IO_URING=1
--application.environmentVariables DOTNET_IORING_THREAD_COUNT=8
--application.environmentVariables DOTNET_ThreadPool_ForceMinWorkerThreads=0x10
```

These are examples for an io_uring experiment, not required defaults for every benchmark. Set only the controls relevant to the change. Check their parsing semantics: `DOTNET_ThreadPool_ForceMinWorkerThreads=0x10` means **16 decimal**; `=16` is parsed as hexadecimal and means 22. A minimum is not a cap on active workers.

Restart the application for every configuration because runtime switches are generally read at startup. For a flag-only comparison, reuse identical binaries and change only that flag. Do not silently change ring counts, worker settings, ASP.NET binaries, or load parameters at the same time.

### 5. Collect a Linux Trace with Crank

Use a **separate profiling run**, retaining the same binary overlay and workload. Add these options to the complete benchmark command:

```text
--application.dotnetTrace true
--application.dotnetTraceCollectMode collect-linux
--application.options.traceOutput /absolute/controller/path/json-candidate.nettrace
```

Choose a new `RUN` name for its JSON and logs as well. Crank starts `dotnet-trace collect-linux` on the agent and downloads the resulting trace to the controller. This mode captures Linux CPU samples, including native and kernel frames, rather than only managed EventPipe sampling. Use Crank's built-in collection instead of adding a startup hook or custom collector to the application.

- Use controller/agent versions supporting `dotnetTraceCollectMode` and an agent with a supported `dotnet-trace` tool. The current Crank implementation requires Linux, effective UID 0, and kernel 6.4 or newer for this mode. Container perf permissions can still prevent collection. Consult the agent's error rather than silently substituting a different trace mode or changing shared-machine security settings.
- Keep `--application.environmentVariables DOTNET_EnableEventPipe=0` for this native CPU profiling run to avoid EventPipe noise. This is **not** appropriate for a separate EventPipe-only `collect` run, which needs EventPipe enabled.
- Upload and retain the matching PDB/`.dbg` files with the binaries. Keep the overlay/manifest beside the collected evidence for offline symbol lookup; do not assume the downloaded trace embeds every symbol.
- Allow collection to finalize. For large traces, increase the wait budgets if needed:

  ```text
  --application.dotnetTraceStopTimeoutSec 600
  --application.collectTimeout 00:12:00
  ```

- Check the downloaded trace exists, is nonempty, and opens with useful samples and resolved target frames in a compatible viewer such as PerfView. Review collector errors and finalization warnings in the logs. A successful benchmark or controller exit code alone does not prove trace collection succeeded; a partial trace may still have been downloaded.
- Do not use traced throughput as the headline performance result. Trace collection changes overhead, and its window may include warmup; select the steady-state interval when analyzing CPU costs.

### 6. Compare and Preserve Results

Run at least two independent application launches per configuration, preferably in reverse-order pairs such as baseline/candidate/candidate/baseline. Keep source, binaries, machine profile, versions, connection count, warmup, measurement duration, and unrelated environment variables fixed.

Report results in a table with throughput, relative change, CPU, p99 latency, errors, launch count, and run-to-run variation. Distinguish peak CPU from mean CPU over the measured window; an average of per-run p99 values is not a pooled percentile. Do not call a small single-run difference an established improvement.

Preserve the exact command/configuration, source provenance, binary/symbol hashes, Crank JSON, application/build logs, and any trace files. Verify zero bad responses/socket errors and investigate failures rather than accepting misleading throughput. Keep failed profiling evidence separate from successful untraced measurements.
