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
any cache I/O or hashing. Invalid configuration logs a low-importance diagnostic;
unsupported invocations log a normal-importance bypass message. Both execute
normally. Without an override, the shared platform default is
`$XDG_CACHE_HOME/crossgen2` on Linux (absolute XDG paths only), falling back to
`$HOME/.cache/crossgen2`, and `LocalApplicationData/crossgen2` on Windows.
On Windows, configure it with PowerShell:

```powershell
$env:CROSSGEN2_EXPERIMENTAL_CACHE = 'true'
$env:CROSSGEN2_EXPERIMENTAL_CACHE_PATH = 'C:\crossgen2-cache' # optional
```

Set configuration before starting MSBuild; do not change
its process environment during parallel builds.

### Supported contract

This first version supports **directly executed Linux-x64 or Windows-x64 crossgen2, targeting
the corresponding host OS and x64 architecture, with non-composite PE output**.
NativeAOT executables and self-contained or framework-dependent managed single-file
bundles are supported, including the repository's self-contained in-build compiler.
Managed hosting-runtime identity is intentionally not resolved; see the assumptions below.
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
Before tool execution, the task deletes these two files, including
symbols left by a previous symbol-enabled compilation. Preparation belongs to
normal tool execution, not the cache, so it also runs when caching is disabled
or bypassed. This intentionally changes those paths to remove stale outputs.
A hit restores their recorded presence/absence. Unrelated outputs and target-owned
semaphores are untouched. PDB-only compilation preserves the native image it reads
and cleans only its symbol output. When legacy symbols are requested, preparation
uses the declared symbol path rather than the modern crossgen2 naming convention.

Legacy crossgen, crossgen2 version 5, separate PDB-generation tasks, composites,
legacy MVID-named maps, Mach-O/Wasm containers, unrecognized extra
arguments (including response files, maps, repro packages, and codegen options
outside the allowlist above), explicit `ToolTask.EnvironmentVariables`, command
processors, `DotNetHostPath` invocations, and unbundled managed/apphost deployments bypass caching. No target opts
these callers in or rewrites their arguments. The direct compiler CLI is unchanged.

### Identity and assumptions

Each eligible invocation hashes a versioned, length-delimited description using
full SHA-256 content hashes. It includes:

* The task assembly and entire invoked compiler executable or single-file bundle. The selected JIT is
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

Caching requires an on-disk task assembly. If it is embedded in a single-file
MSBuild host and has no assembly location, the invocation visibly bypasses caching.

`Crossgen2CacheInvocation` resolves platform-specific deployment dependencies and
symbol-output conventions. Task-specific description code discovers assembly and
PDB inputs and checks supported semantics; the shared layer hashes the resulting
description and owns cache storage/restoration.

The deployment must be the actual compiler executable or single-file bundle, not
a wrapper. Linux requires an ELF executable; Windows requires an x64 PE executable
without a CLR header. These executable checks intentionally accept both NativeAOT
and managed single-file deployments; they do not distinguish the two.
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

**Inputs, tool deployment, and environment must remain stable from invocation
description through hashing and execution/restoration.** There is no input snapshot
or persistent digest cache.
Cache/output paths must not alias inputs or each other through symbolic links,
hard links, or filesystem aliases; use ordinary, trusted local directories.
The cache is trusted build storage, not an authenticated artifact source.

The cache deliberately does not identify OS native libraries, kernel/CPU state,
or arbitrary inherited environment settings. Known loader overrides
`LD_LIBRARY_PATH`, `LD_PRELOAD`, and `LD_AUDIT` bypass. Other inherited settings
that introduce dependencies, alter native loading/codegen, or require execution
side effects require disabling or isolating the cache.
Absolute paths remain in keys: there is no promise of cross-worktree reuse.

### Experimental managed-hosting policy

Like the [experimental ILLink task cache](https://github.com/dotnet/runtime/blob/72f0f94d975ccff3730cb19c338bee6d5c7fac8d/docs/design/tools/illink/task-cache.md#input-identity-and-eligibility), this cache does not resolve or hash the
managed runtime that executes the child compiler. The executable hash covers
NativeAOT's compiled runtime and any managed assemblies, runtime components, and
configuration embedded in a single-file bundle. It does not cover externally
resolved managed-runtime components, separately deployed hosting dependencies,
or their resolution context. The explicit JIT, support-library, and symbol-writer
hashes above remain part of the key for either deployment.

The runtime hosting MSBuild is not a reliable identity for a managed child
compiler. A bundled runtimeconfig describes requested frameworks and roll-forward
policy, not necessarily the exact runtime that will execute the compiler.
No installed hostfxr or shared-runtime trees are scanned, and no runtime-resolution
process is launched for cache lookup.

Roslyn's [compiler-output cache](https://github.com/dotnet/roslyn/blob/f955706493c2ef5d90bb1c2d76c13a27436c2669/docs/compilers/Design/compiler-output-cache-experiment.md)
runs inside the compiler server and includes compiler and runtime informational
versions, framework description, and OS description in its deterministic key.
That runtime identity describes the process actually performing compilation.
An MSBuild-task cache cannot substitute MSBuild's runtime identity for the child
compiler's, so it retains ILLink's explicit runtime-stability assumption instead.

**Runtime or hosting-dependency changes not reflected in other keyed files may
reuse an entry.** Clear or isolate the cache, or disable caching, when those changes
must invalidate prior results. This is an intentional experimental limitation,
not a promise of complete managed-deployment identity.

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

Only exit-code-zero results without task-logged errors are stored.
Cache hits do not replay compiler warnings or other compiler
text, matching the no-replay policy of the ILLink cache. `WarningsDetected` is
stored and restored separately as a Boolean task output; it remains available
to callers without replaying the warning messages. Normal execution resets this
output before running the compiler, including after a failed restoration.
External tool side effects are not replayed. Shared cache status/failure messages
are low-importance diagnostics. Tool failures retain normal exit/error reporting.
Invalid output declarations, including overlap between the cache directory and an
owned output file, fail the task before output preparation rather than falling back
to compilation.

Entries live under `<cache>/v1/<lowercase-SHA-256>/` with indexed files in `outputs/`,
a binary manifest containing the format version, output-contract kind, file count,
storage names, presence flags, output lengths and hashes, and optional task output
metadata (crossgen2's `WarningsDetected`). A manifest checksum protects this entire record.
The shared storage implementation under `src/tasks/Common/InvocationCache` is
compiled into the task assembly; it captures and restores the declared file set,
including absence. Files are not classified as required or optional: a successful
execution's observed presence/content or absence is the result to reproduce.
ILLink uses the same storage sources with an
owned-directory contract and the same manifest encoding and checksum.
The shared `InvocationCache.ExecuteCached` helper also handles restore-or-execute decisions and
stores only successful executions without logged errors. The task retains
eligibility, invocation description, and task output serialization. Shared code
computes the key; the task prepares outputs before actual compiler execution.
Participating tasks use the same `ExecuteTool` convention: a single helper call
passing an `InvocationCacheConfiguration` and parameterless callbacks that capture
tool arguments and call the named `DescribeInvocation` and `ExecuteNormally`
methods. The shared helper does not impose ToolTask's argument shape. `DescribeInvocation`
returns an `InvocationDescription` with typed `InvocationInputs` and
`InvocationOutputs` declarations plus argument/context strings. Both declarations
use `FromFiles` for individual file paths; outputs also support `FromDirectory`
for directory ownership. Neither declaration contains cache operations. The shared adapter
returns directly to normal execution when no cache is available and calls
`DescribeInvocation` only when caching is configured. A null description or other
cache bypass also returns directly to normal execution. The timestamp is captured
after description collection/validation, immediately before hashing. Cache creation is built
in; the same platform-default and configuration-diagnostic policy applies to both
tasks. The default directory name is the invariant-lowercase tool name, so the
caller supplies `"Crossgen2"` only once in its static readonly configuration.
That configuration contains environment-variable names, not resolved values;
shared execution reads the current environment lazily. It is separate from the
cache-independent invocation description.
`ExecuteNormally` uses no cache types or helpers and does not branch on eligibility.
Named cache-only callbacks read or write task output metadata. There is no
task-specific cleanup callback, diagnostic capture/replay, invocation-start hook,
or post-restore hook.
Both ILLink's owned-directory preparation and crossgen's declared-file preparation
belong to normal execution, regardless of cache configuration. Expected preparation
I/O/access failures log an error and return failure without executing or storing.
The shared implementation selects the storage version, not each tool. Compatibility is not
guaranteed within this experimental version. The keyed task assembly changes with
the embedded storage implementation, invalidating entries without changing the
namespace.
Writers copy into unique cache-local staging directories and publish complete
immutable entries with one directory rename. Nonblocking per-entry writer mutexes
avoid duplicate publication work. Racing publishers never overwrite
an existing entry. Readers validate the manifest and every output before copying.
Successful stores and hits update a best-effort `last-used` marker.
Concurrent tasks may share storage while writing distinct outputs; concurrent
tasks writing the same owned file are unsupported.

Restoration is sequential, not transactional. An I/O/corruption failure falls
back to compilation after deleting both owned outputs. Cleanup failure fails the
task, never returns a false hit. Successful restores use a UTC timestamp captured
before lookup, not the original cache timestamp, for downstream incremental checks.
Cache store failures are visible but do not fail a successful compilation.
Corrupt existing entries are not repaired automatically. Cache operations are
synchronous and do not add cancellation polling; tool execution retains ToolTask
cancellation. There is no automatic eviction, size limit, CI cache enablement, or stable
on-disk format guarantee.

Maintain an idle cache with the shared experimental tool:

```sh
dotnet build-cache purge --cache crossgen2 --before 2026-10-01T12:00:00Z
```

The named cache uses `CROSSGEN2_EXPERIMENTAL_CACHE_PATH` or the same platform
default as the task, regardless of whether caching is enabled. Alternatively,
use `--cache-directory /path/to/cache` on its own to select a directory directly.
No build may use the selected cache while purging. See the
[tool README](../../tools/BuildCache/README.md) for cutoff semantics and local usage.

### SDK coordination and local validation

The hook belongs in the mirrored `RunReadyToRunCompiler`, with `Crossgen2Cache.cs`
and `Crossgen2CacheInvocation.cs` ported alongside it to
`dotnet/sdk/src/Tasks/Microsoft.NET.Build.Tasks`, together with the shared
`src/tasks/Common/InvocationCache` sources. Shipping
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
`CROSSGEN2_CACHE_TEST_TOOL` to an existing directly executed Linux-x64 `crossgen2` or
Windows-x64 `crossgen2.exe` executable or single-file bundle to also run the real compiler equivalence tests against the test host's
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