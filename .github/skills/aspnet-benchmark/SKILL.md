---
name: aspnet-benchmark
description: Run the TechEmpower ASP.NET Core benchmarks (PlatformBenchmarks) locally against a locally-built dotnet/runtime, to validate the end-to-end impact of a runtime change (e.g. sockets, io_uring, GC, JIT) under realistic HTTP load. Use this when asked to benchmark, load-test, or profile an ASP.NET Core / Kestrel scenario, or to compare an env-var-gated feature (e.g. `DOTNET_USE_IO_URING`) end-to-end.
---

# Running the TechEmpower (PlatformBenchmarks) Benchmark Locally

When a runtime change needs to be validated under realistic HTTP server load (not just a BenchmarkDotNet microbenchmark — see the `performance-benchmark` skill for that), use the TechEmpower `PlatformBenchmarks` app from the [`aspnet/Benchmarks`](https://github.com/aspnet/Benchmarks) repository, driven by `wrk` (or [`bombardier`](https://github.com/codesenberg/bombardier) on Windows, since `wrk` is Linux/macOS-only), against a locally-built runtime.

This is the right tool when the change affects the socket/IO stack, GC, JIT, or anything else only observable under many concurrent connections and real request/response processing.

## Prerequisites

- A locally-built dotnet/runtime with the change under test (see the `build-and-test` skill). Build **everything** (runtime and libraries) in Release with `-c release`, not just `-rc release` — the latter only forces the runtime to Release and leaves libraries at their default (`Debug`), producing a mismatched testhost:

  ```bash
  ./build.sh clr+libs -c release
  ```

- A load-generation tool: on Linux, `wrk` (`sudo apt install wrk` or build from source — <https://github.com/wg/wrk>). `wrk` doesn't build/run on Windows; use [`bombardier`](https://github.com/codesenberg/bombardier) instead (`go install github.com/codesenberg/bombardier@latest`, or download a prebuilt binary from its releases page) — the examples below use `wrk` syntax, with the `bombardier` equivalent noted alongside.
- The [`aspnet/Benchmarks`](https://github.com/aspnet/Benchmarks) repository, which contains the TechEmpower and other benchmark apps.

## Step 1: Clone the Benchmarks Repository

```bash
git clone https://github.com/aspnet/Benchmarks.git
```

The relevant app is `src/BenchmarksApps/TechEmpower/PlatformBenchmarks` — a raw Kestrel `HttpApplication` implementation of the TechEmpower benchmark suite (JSON serialization, plaintext, fortunes, single/multiple queries, updates). It targets the latest `net*.0` TFM and does **not** require a database for the `/json` and `/plaintext` endpoints, which is normally all you need.

## Step 2: Prefer the `/json` Endpoint, Not `/plaintext`

Use `/json` for a "realistic max throughput" measurement. `/plaintext` conventionally uses HTTP pipelining (16 requests batched per connection in the official TechEmpower harness/`wrk` scripts), which does not reflect normal request/response traffic and inflates throughput numbers in a way that isn't representative. `/json` sends one request per round trip and is the more realistic comparison point.

## Step 3: Publish the App Against the Repo's Own SDK

Build/publish the app with the dotnet/runtime repo's own SDK (`<runtime-repo>/.dotnet/dotnet`), not a system-wide SDK, so the app targets the same TFM as your local build:

```bash
cd Benchmarks/src/BenchmarksApps/TechEmpower/PlatformBenchmarks
<runtime-repo>/.dotnet/dotnet build -c Release
```

This produces `bin/Release/<tfm>/PlatformBenchmarks.dll`.

## Step 4: Make the Repo's SDK Run Against Your Local Runtime Build

`PlatformBenchmarks` needs `Microsoft.AspNetCore.App`, which the freshly-built `artifacts/bin/testhost` does **not** contain (it only has `Microsoft.NETCore.App`) — running it directly via `testhost`'s `corerun`/`dotnet` fails with a "no framework found" error. The repo SDK's own `dotnet` (under `<runtime-repo>/.dotnet`) already has `Microsoft.AspNetCore.App`, so the simplest way to combine "the ASP.NET Core framework" with "your locally-built `Microsoft.NETCore.App`" is to temporarily overlay the SDK's shared `Microsoft.NETCore.App/<version>` folder with the testhost's freshly-built one:

```bash
SDK_FX=$(find <runtime-repo>/.dotnet/shared/Microsoft.NETCore.App -maxdepth 1 -type d | tail -1)
TESTHOST_FX="<runtime-repo>/artifacts/bin/testhost/<tfm>-<os>-Release-<arch>/shared/Microsoft.NETCore.App/<version>"

# ALWAYS back up the original SDK framework files first, so they can be restored exactly.
mkdir -p /tmp/netcoreapp_sdk_backup
rsync -a --delete "$SDK_FX"/ /tmp/netcoreapp_sdk_backup/

# Overlay with the local build.
rsync -a --delete "$TESTHOST_FX"/ "$SDK_FX"/
```

**This mutates the repo's own SDK shared framework in place.** Never skip the backup step, and always restore it when done (Step 6) — leaving it mutated will silently break every other use of that SDK on the machine.

## Step 5: Run the App and Verify It's Serving Requests

```bash
cd Benchmarks/src/BenchmarksApps/TechEmpower/PlatformBenchmarks/bin/Release/<tfm>
<runtime-repo>/.dotnet/dotnet exec PlatformBenchmarks.dll --urls http://127.0.0.1:5000 &
sleep 5
curl -sS http://127.0.0.1:5000/json
```

Set whatever env var/`AppContext` switch you're comparing (e.g. `DOTNET_USE_IO_URING=1`/`=0`) *before* starting the process — it's read once at startup.

## Step 6: Run `wrk` (or `bombardier` on Windows) Against It

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

Repeat Steps 5-6 for each configuration being compared (e.g. once with the env var on, once with it off), reusing the same overlay — only the running process needs to be restarted between configurations, not the overlay.

## Step 7: Clean Up

1. Kill the server process(es) — find the actual `dotnet exec ... PlatformBenchmarks.dll` PID (not the shell that launched it) with `pgrep -af PlatformBenchmarks` and `kill <pid>`.
2. **Restore the SDK's shared framework from the backup** and verify it via checksum before considering the machine clean:

   ```bash
   rsync -a --delete /tmp/netcoreapp_sdk_backup/ "$SDK_FX"/
   md5sum "$SDK_FX"/System.Private.CoreLib.dll   # compare against the value recorded before overlaying
   ```

3. Remove the temporary backup and any log files once restoration is verified.

## Profiling a Benchmark Run with `perfcollect`

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

### ⚠️ Symbol Resolution Warnings

- **Precompiled (R2R/crossgen) framework symbols are not resolved automatically.** `perfcollect` needs a `crossgen2` tool matching the exact runtime build to map native framework code back to method names; without it, framework frames show up unresolved/hex-only in the trace. When profiling a **locally-built** runtime you don't need to hunt one down — your own build already produced the exact matching binary at `artifacts/bin/crossgen2_publish/<arch>/<config>/crossgen2`. Copy (or symlink) it next to `libcoreclr.so` in the directory you're actually running from (e.g. the testhost/SDK-overlay shared framework folder from Step 4) before collecting:

  ```bash
  cp artifacts/bin/crossgen2_publish/x64/Release/crossgen2 "$SDK_FX"/
  ```

  For a runtime you didn't build yourself, see the "Resolving Framework Symbols" section of [linux-performance-tracing.md](../../../docs/project/linux-performance-tracing.md) instead (it walks through obtaining a matching `crossgen2` via a self-contained publish).
- **Native runtime frames (`libcoreclr.so`, `libclrjit.so`, etc.) resolve out of the box, as long as you don't move or discard the build's `.dbg` files.** A Release native build ships each `.so` *stripped*, but the matching, un-stripped `libXyz.so.dbg` is written right alongside it in the same output directory (e.g. `artifacts/bin/coreclr/<rid>.<config>/`), linked via its `.gnu_debuglink` section and a matching Build ID. `perf`/`perfcollect` follow that link automatically as long as the `.dbg` file stays next to its `.so` — don't clean up or selectively copy only the `.so` files into a deployment layout, or you'll silently lose native symbolication.
- **JIT-generated (Tier0/Tier1) symbols require `DOTNET_PerfMapEnabled=1` to be set *before* the process starts.** If you forget this and only realize partway through a run, the trace from that run cannot be fixed after the fact — kill the process, re-export the env var, and restart it before collecting again.
- Even after doing all of the above, expect *some* residual unresolved frames from components you didn't build locally (e.g. the OS's own libraries, or a `crossgen2`/`.dbg` mismatch if you mixed binaries from different build configurations) — this doesn't mean the whole trace is useless; managed app-level and JIT-emitted frames still resolve correctly via the perf map regardless.
- `DOTNET_PerfMapEnabled=1` has a real (if usually small) overhead of its own — don't leave it set for the throughput (`wrk`/`bombardier`) runs themselves, only for the dedicated profiling run.

## Common Pitfalls

- **Running via `artifacts/bin/testhost` directly fails** with "no framework found" — that layout only has `Microsoft.NETCore.App`, not `Microsoft.AspNetCore.App`. Use the SDK-overlay approach in Step 4 instead.
- **Forgetting to set the env var before starting the process** — most feature switches (like `DOTNET_USE_IO_URING`) are read once at startup, so changing it and re-`curl`-ing the same running process has no effect.
- **Comparing a single run per configuration** — throughput varies run to run; always do at least two runs per configuration.
- **Leaving the SDK shared framework overlaid** — always restore it (Step 7) and verify via checksum; a stale/mismatched overlay silently breaks unrelated work on the same machine later.
