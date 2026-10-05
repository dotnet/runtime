The files in this directory are equivalent/identical to corresponding files in the sdk repo. They should be kept equivalent.

The files in the subdirectory CommoonFilesPulledFromSdkRepo do not need to be updated aggressively as they change.
The files in the subdirectory ShimFilesSimulatingLogicInSdkRepo are simply in place to make the functionality be close enough so that these tasks can easily be compiled.

## Experimental invocation cache

`RunReadyToRunCompiler` has a local, environment-only experiment:

```sh
export CROSSGEN2_EXPERIMENTAL_CACHE=true
export CROSSGEN2_EXPERIMENTAL_CACHE_PATH=/absolute/path/to/cache # optional
```

The path alone does not enable caching. Unset, empty, or `false` disables it before
any cache I/O or hashing. Invalid boolean settings and unsupported invocations
log a normal-importance bypass message and execute normally. Without an override,
the Linux default is `$XDG_CACHE_HOME/crossgen2` (absolute XDG paths only), or
`$HOME/.cache/crossgen2`. Set configuration before starting MSBuild; do not change
its process environment during parallel builds.

### Supported contract

This first version supports **native Linux-x64 crossgen2, targeting Linux x64,
non-composite PE output**, with optional task-supplied MIBC profiles and version-1
perf maps. It requires absolute, non-wildcard input/output paths that can be
represented safely in the task's response file. It does not parse extra command
strings: anything other than empty semicolon-separated arguments bypasses caching.
The existing argument order and compiler invocation are unchanged.

The task owns exactly the native image and its adjacent
`<image-without-extension>.ni.r2rmap`, never the containing directory. When symbols
are enabled, `OutputPDBImage` must name that exact compiler-generated map and
`PerfmapFormatVersion` must be `1`. Before an eligible miss executes, it deletes
these two files, including a map left by a previous symbol-enabled compilation.
A hit restores their recorded presence/absence. Unrelated outputs and target-owned
semaphores are untouched. Bypassed/disabled invocations retain existing behavior,
including existing cleanup behavior.

Legacy crossgen, crossgen2 version 5, separate PDB-generation tasks, composites,
Windows PDBs, legacy MVID-named maps, Mach-O/Wasm containers, extra arguments
(including response files, maps, repro packages, and codegen options), explicit
`ToolTask.EnvironmentVariables`, command processors, and managed-host/apphost
deployments bypass caching. This includes current runtime callers that supply
extra arguments such as `--embed-pgo-data`. No target opts these callers in or
rewrites their arguments. The direct compiler CLI is unchanged.

### Identity and assumptions

Each eligible invocation hashes a versioned, length-delimited description using
full SHA-256 content hashes. It includes:

* The task assembly, actual invoked native `crossgen2`, selected JIT (explicit
  `JitPath` or adjacent `libclrjit_unix_x64_x64.so`), and adjacent
  `libjitinterface_x64.so`. Only the selected JIT is hashed, not all cross-target
  JITs or the runtime hosting MSBuild.
* Input and ordered implementation references, MIBC files, and rooted and adjacent
  CodeView PDB candidates (including absence). Multi-file assemblies bypass.
* Effective normalized command/response strings, diagnostic policy, absolute
  output/working paths, host OS/architecture, cultures, and `DOTNET_PROCESSOR_COUNT`.

The deployment must be the actual native compiler, not a wrapper. A non-ELF tool,
symbolic-link executable, or adjacent managed DLL/deps/runtimeconfig bypasses.
Unreadable dependencies never become empty hashes. File timestamps, lengths,
MVIDs, and version strings are not input identities.

**Inputs, tool deployment, and environment must remain stable from hashing through
execution/restoration.** There is no input snapshot or persistent digest cache.
Cache/output paths must not alias inputs or each other through symbolic links,
hard links, or filesystem aliases; use ordinary, trusted local directories.
The cache is trusted build storage, not an authenticated artifact source.

The cache deliberately does not identify OS native libraries, kernel/CPU state,
or arbitrary inherited environment settings. Known loader overrides
`LD_LIBRARY_PATH`, `LD_PRELOAD`, and `LD_AUDIT` bypass. Other inherited settings
that introduce dependencies, alter native loading/codegen, or require execution
side effects require disabling or isolating the cache. NativeAOT's bundled runtime
is covered by the compiler hash; managed child-runtime resolution is not attempted.
Absolute paths remain in keys: there is no promise of cross-worktree reuse.

### Diagnostics, storage, and failures

Only exit-code-zero results without task-logged errors are stored, and the native
image must exist. Compiler text and message importance are stored and replayed
through the same task diagnostic handler on a hit, preserving warnings and
`WarningsDetected` rather than suppressing them. Replay is compiler output, not a
claim that the compiler ran; timing text, if any, describes the original run.
External tool side effects are not replayed. Cache diagnostics are additional
normal-importance messages. Tool failures retain normal exit/error reporting.

Entries live under `<cache>/v1/<SHA-256>/` with indexed output files, a binary
manifest containing output hashes and diagnostics, and a manifest checksum.
Writers copy into unique cache-local staging directories and publish complete
immutable entries with one directory rename. Racing publishers never overwrite
an existing entry. Readers validate the manifest and every output before copying.
Concurrent tasks may share storage while writing distinct outputs; concurrent
tasks writing the same owned file are unsupported.

Restoration is sequential, not transactional. An I/O/corruption failure falls
back to compilation after deleting both owned outputs. Cleanup failure fails the
task, never returns a false hit. Successful restores use a UTC timestamp captured
before lookup, not the original cache timestamp, for downstream incremental checks.
Cache store failures are visible but do not fail a successful compilation.
Corrupt existing entries are not repaired automatically. Cache operations are
synchronous and do not add cancellation polling; tool execution retains ToolTask
cancellation. There is no eviction, size limit, purge command, CI wiring, or stable
on-disk format guarantee.

### SDK coordination and local validation

The hook belongs in the mirrored `RunReadyToRunCompiler`, with `Crossgen2Cache.cs`
ported alongside it to `dotnet/sdk/src/Tasks/Microsoft.NET.Build.Tasks`. Shipping
SDK behavior does not change merely by building this repository. The SDK port also
needs its own framework builds/tests and SDK resource/localization integration.
`Microsoft.NET.CrossGen.targets`, compiler resolution, and output preparation
need no changes for this narrow shape.

`./build.sh tasks` builds the self-contained tasks; the affected project targets
`$(BundledNETCoreAppTargetFramework)`. Focused tests run with:

```sh
./.dotnet/dotnet test src/tasks/Crossgen2Tasks.Tests/Crossgen2Tasks.Tests.csproj
```

The tests use isolated child processes for environment configuration. Set
`CROSSGEN2_CACHE_TEST_TOOL` to an existing native Linux-x64 `crossgen2` executable
to also run the real compiler equivalence tests against the test host's
`System.Linq.dll` and runtime references. Those tests compare uncached, miss/store,
and restored images/maps and count actual compiler executions.
On shared hosts, apply the host's memory-limited build scope and concurrency flags
to these commands.