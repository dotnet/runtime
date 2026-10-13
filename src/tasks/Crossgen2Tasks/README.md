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
the default on either supported platform is `$XDG_CACHE_HOME/crossgen2` (fully
qualified XDG paths only), or `.cache/crossgen2` under the user's profile directory.
On Windows, configure it with PowerShell:

```powershell
$env:CROSSGEN2_EXPERIMENTAL_CACHE = 'true'
$env:CROSSGEN2_EXPERIMENTAL_CACHE_PATH = 'C:\crossgen2-cache' # optional
```

Set configuration before starting MSBuild; do not change
its process environment during parallel builds.

### Supported contract

This first version supports **native Linux-x64 or Windows-x64 crossgen2, targeting
the corresponding host OS and x64 architecture, with non-composite PE output**.
Optional task-supplied MIBC profiles, Linux version-1 perf maps, and Windows native
PDBs are supported. It requires fully qualified, non-wildcard input/output paths that can be
represented safely in the task's response file. It does not parse extra command
strings in general: an unrecognized argument (including anything with a path-like
shape, response files, or an unexpected value) bypasses caching. A small allowlist
of known, pathless boolean switches used by in-repo callers (currently
`--target-allows-runtime-code-generation`, `--embed-pgo-data`,
`--verify-type-and-field-layout`, and `--enable-cached-interface-dispatch-support`,
each optionally suffixed with `:true`/`:false`) is still included verbatim in the
cache key, so a different value still produces a different entry. The existing
argument order and compiler invocation are unchanged.

The task owns exactly the native image and its symbol file, never either
containing directory. The symbol directory comes from `OutputPDBImage` metadata
on each compilation entry and may differ from the image directory. On Linux the symbol file is
`<image-without-extension>.ni.r2rmap` and `PerfmapFormatVersion` must be `1` when
symbols are enabled. On Windows it is `<image-without-extension>.ni.pdb`, or
`<image-without-extension>.pdb` for an image already ending in `.ni.dll` or `.ni.exe`.
When symbols are enabled, `OutputPDBImage` must use that exact compiler-generated
filename in the declared directory. When symbols are disabled, the cache still
tracks that filename in the metadata's directory to remove stale symbols; without
symbol-path metadata it tracks the filename beside the image.
Before an eligible miss executes, it deletes these two files, including
symbols left by a previous symbol-enabled compilation.
A hit restores their recorded presence/absence. Unrelated outputs and target-owned
semaphores are untouched. Bypassed/disabled invocations retain existing behavior,
including existing cleanup behavior.

Legacy crossgen, crossgen2 version 5, separate PDB-generation tasks, composites,
legacy MVID-named maps, Mach-O/Wasm containers, unrecognized extra
arguments (including response files, maps, repro packages, and codegen options
outside the allowlist above), explicit `ToolTask.EnvironmentVariables`, command
processors, and managed-host/apphost deployments bypass caching. No target opts
these callers in or rewrites their arguments. The direct compiler CLI is unchanged.

### Identity and assumptions

Each eligible invocation hashes a versioned, length-delimited description using
full SHA-256 content hashes. It includes:

* The task assembly and actual invoked native compiler. The selected JIT is
  explicit `JitPath`, or adjacent `libclrjit_unix_x64_x64.so` on Linux /
  `clrjit_win_x64_x64.dll` on Windows. The adjacent support library is
  `libjitinterface_x64.so` on Linux / `jitinterface_x64.dll` on Windows. Windows
  symbol-enabled invocations also hash adjacent `Microsoft.DiaSymReader.Native.dll`.
  Only the selected JIT is hashed, not all cross-target JITs or the runtime hosting MSBuild.
* Input and ordered implementation references, MIBC files, and rooted and adjacent
  CodeView PDB candidates (including absence). Multi-file assemblies bypass.
  On Windows, every present input-PDB candidate must be a portable PDB whose
  identity matches the assembly. Non-portable, corrupt, and mismatched input PDBs
  bypass caching rather than invoking unmodeled COM/.NET Framework symbol readers.
  This restriction does not exclude native PDB outputs.
* Effective normalized command/response strings, diagnostic policy, absolute
  output/working paths, host OS/architecture, cultures, and `DOTNET_PROCESSOR_COUNT`.

`Crossgen2CacheInvocation` resolves platform-specific deployment dependencies and
symbol-output conventions; the key computation, storage, restoration, and
diagnostic replay remain shared.

The deployment must be the actual native compiler, not a wrapper. Linux requires
an ELF executable; Windows requires a native x64 PE executable without a CLR header.
A symbolic-link executable or adjacent managed DLL/deps/runtimeconfig bypasses.
Unreadable dependencies never become empty hashes. File timestamps, lengths,
MVIDs, and version strings are not input identities.
Windows path containment and input/output alias checks compare case-insensitively;
drive-relative, root-relative, device, alternate-stream, and trailing-dot/space
invocation paths bypass caching. Embedded root-relative CodeView paths are resolved
against the compiler's working-directory drive and hashed, including absence;
drive-relative CodeView paths bypass. Filesystem aliases remain unsupported.

Windows native PDB generation retains the compiler's filename restrictions:
the output assembly normally needs the original input filename in a separate
directory. Native PDB generation also creates a fresh debug timestamp/GUID in the
image on each compilation. A cache hit restores the original stored image/PDB
pair byte-for-byte, rather than producing a new debug identity. Compiler-created
temporary native-image copies are not replayed.

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

### Deferred Windows input-symbol support

The Windows input-PDB bypass is a temporary limitation, not complete Windows
symbol support. Non-portable input PDBs and portable PDBs rejected by the compiler's
identity check can fall back to `UnmanagedPdbSymbolReader`, which resolves COM
symbol-reader and metadata services through .NET Framework (`mscoree.dll`).
Those dependencies are outside the adjacent native compiler deployment and are
not covered by hashing `Microsoft.DiaSymReader.Native.dll`, the output PDB writer.

Completing this support requires identifying and hashing the actual external
reader/metadata-service dependencies and their resolution context, or changing
compiler deployment to make that resolution explicit and isolated. Add real
compiler tests for native input PDBs, portable-PDB fallback, and dependency
invalidation before removing the bypass. Native Windows PDB output generation
remains supported in the meantime.

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
cancellation. There is no eviction, size limit, purge command, CI cache enablement, or stable
on-disk format guarantee.

### SDK coordination and local validation

The hook belongs in the mirrored `RunReadyToRunCompiler`, with `Crossgen2Cache.cs`
and `Crossgen2CacheInvocation.cs` ported alongside it to
`dotnet/sdk/src/Tasks/Microsoft.NET.Build.Tasks`. Shipping
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
`CROSSGEN2_CACHE_TEST_TOOL` to an existing native Linux-x64 `crossgen2` or
Windows-x64 `crossgen2.exe` executable to also run the real compiler equivalence tests against the test host's
`System.Linq.dll` and runtime references. Those tests compare uncached, miss/store,
and restored images/symbol files and count actual compiler executions.
Symbol-enabled cases cover both adjacent and separate symbol directories. Independent
Windows symbol-enabled compilations are compared excluding only their native
debug timestamp/GUID; restoration must preserve every image/PDB byte.
On Windows:

```powershell
.\build.cmd tasks
$env:CROSSGEN2_CACHE_TEST_TOOL = 'C:\path\to\crossgen2.exe'
.\.dotnet\dotnet.exe test src\tasks\Crossgen2Tasks.Tests\Crossgen2Tasks.Tests.csproj
```

On shared hosts, apply the host's memory-limited build scope and concurrency flags
to these commands.