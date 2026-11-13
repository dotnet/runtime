# ILLink task cache integration

ILLink opts eligible task invocations into the
[shared invocation cache](../../../../src/tasks/Common/InvocationCache/README.md). This document
covers ILLink's input discovery, output ownership, and deliberate omissions; the shared README owns
hashing, storage, restoration, and concurrency. The feature is experimental and may change or be
removed without notice.

## Configuration and task boundary

Cache complete invocations in the MSBuild task, not in the linker CLI. Enable with
`ILLINK_EXPERIMENTAL_CACHE=true` and optionally override storage with
`ILLINK_EXPERIMENTAL_CACHE_PATH`. These are process environment variables, not task parameters or
MSBuild properties. Set them before starting MSBuild and do not change them during parallel builds;
reused processes must receive the intended environment.

The task supplies display name `ILLink` to the shared configuration. The default is `illink` under the
platform cache location; a path alone never enables caching. See
[task usage](../../../tools/illink/illink-tasks.md#experimental-task-cache) for configuration and
maintenance commands.

`ILLink` derives from the shared `CachedToolTask`, supplying `CacheConfiguration`, `DescribeInvocation`,
and `ExecuteToolCore`. The description supplies input paths, effective
arguments/context, and one `InvocationOutputs.FromDirectory` declaration. Eligibility and metadata
inspection stay in ILLink; content hashing and cache operations stay in the shared layer.
The base class calls `InvocationCache.TryCreateFromEnvironment` before describing the invocation.
If a cache is available and the description is supported, it calls the cache's instance
`TryExecuteCached` method with that description and an execution callback. The cache owns output
validation, timestamping, hashing, restoration, and storage.
On a miss, the cache invokes the callback. The base class allocates stdout/stderr capture buffers,
runs the tool, and returns an `InvocationResult` containing its exit code and those same buffers.
The cache reads the returned buffers and declared output files to store successful execution.
On a hit, the cache returns a result only after restoration succeeds.
`CachedToolTask` replays the returned output on both hits and misses through its inherited `ToolTask`
logging methods and current stream policy; the cache has no stream logging-policy settings.
If the cache declines an invocation, it returns false without invoking the callback;
the base class calls `ExecuteToolCore` directly without capture.
Replay receives only the stdout/stderr sequences. The base class determines the final task exit code
from the execution result and logged errors, without modifying the cache result.

## Input discovery and eligibility

Hash all supplied assemblies, reference assemblies, and root descriptors. Include adjacent PDBs,
MDBs, configuration files, and satellite assemblies even when symbols are disabled. Follow assembly
metadata file entries for linked resources and additional modules. Adding or removing a sidecar
changes identity.

The inventory includes the task assembly, selected dotnet host executable, and entire linker
assembly. Hash the linker without loading it or relying on MVID, timestamps, or length; do not scan
its deployment directory or the host's installed runtime trees.

Bundled linker dependencies such as Cecil and deployment configuration are not fingerprinted.
Changes to those files require a cleared/isolated cache or disabled caching unless the linker binary
also changes.

The resolved runtime/CoreLib executing the child linker is deliberately not identified. MSBuild's
runtime identity does not establish the child's, and runtimeconfig roll-forward settings do not
identify the resolved CoreLib. Resolving it would require additional host information or another
process. Runtime installation changes alone therefore need not invalidate entries; callers
requiring that distinction must clear or isolate storage.

Inherited environment variables are not enumerated, hashed, or used as a bypass list. Platform,
architecture, and culture are part of the shared key, but callers must disable caching when inherited
settings introduce untracked dependencies, affect outputs, or require side effects such as startup
hooks or profiling.

Bypass arbitrary `ExtraArgs`, custom steps/data, dependency-dump options, and explicit
`ToolTask.EnvironmentVariables` overrides. These can introduce undeclared inputs, outputs, or side
effects.

For runtime library builds, support `--ignore-link-attributes true` followed by zero or more
`--link-attributes FILE`, `--substitutions FILE`, `--ignore-substitutions true|false`, or
`--feature NAME true|false` options. Require the XML files to be readable. Runtime library builds
supply selected reference files through `ReferenceAssemblyPaths`, with project references first.
Shared framework libraries use their selected transitive implementation closure, including CoreLib;
packable and cross-platform flavors retain reference assemblies. Unrelated assemblies beside those
files are not inputs. Custom arguments retain directory resolution in the runtime targets;
`-d DIRECTORY` and other options outside this narrow whitelist bypass caching without changing
linker CLI execution.

Uninspectable assemblies (including PE images without managed metadata) and
other input-read failures bypass caching with diagnostics. Missing declared files have explicit
absence markers; normal linking decides whether an invocation can succeed. Inputs must remain stable
throughout discovery, hashing, and linking.

## Output preparation and diagnostics

The task owns a dedicated `OutputDirectory`, including when caching is disabled. `ExecuteToolCore`
deletes and recreates it exactly once using ordinary filesystem operations, then executes ILLink.
Cleanup errors fail the task. Do not add cache-aware preparation callbacks or make normal execution
depend on cache eligibility. The linker CLI retains its existing nonempty-directory behavior.

On a hit, the shared layer validates the entry and replaces the owned directory. On a miss or failed
restore, normal execution prepares it before linking. ILLink writes directly to its ordinary
destination; successful results are copied into storage afterward, not compiled into cache staging.

For eligible cache-enabled invocations, the cache and output directories must neither equal nor
contain each other. Invalid overlap fails before cleanup rather than falling back to uncached
linking.

Copy the complete output tree without filename exceptions. The MSBuild target owns
`<output-directory>.semaphore` (normally `linked.semaphore`) and invalidates it before its incremental
skip decision when the directory is missing. Old in-directory `Link.semaphore` files receive no
special migration or exclusion. Assume outputs are not symbolic links.

`CachedToolTask` captures original stdout/stderr lines on eligible misses at `ToolTask.StartToolProcess`
instead of logging them live. Both hits and misses replay them through the ordinary task logging path,
preserving warning codes, text, locations, and subcategories. Current stream importance, stderr-as-error,
and MSBuild warning promotion, exceptions, and warning-as-message policy apply without changing the
key. Compiler-owned warning settings remain part of the arguments and key. A warning promoted by
MSBuild fails the task, but successful tool execution is still stored. Subsequent hits reapply current
logging policy to the captured output.

The base class allocates and fills capture lists only on eligible misses.
It exposes those lists to its process handlers while calling `ExecuteToolCore` and clears its references
afterward, including on exceptions. The base class replaces the ordinary process-output handlers during
capture, forwarding end-of-stream notifications to `ToolTask` so it can finish draining the readers. The cache stores
successful tool results before replay applies logging policy; no logging-based exit-code override is
needed during execution. Partial output is replayed even if execution throws. Storage and replay use
separate stdout and stderr sequences in that order, so callback scheduling does not affect the cached
transcript. Bypassed invocations retain `ToolTask`'s ordinary live logging and interleaving.
Task reuse, misses, and restore failures start with fresh capture state.
Normal tool execution retains `ToolTask` subprocess cancellation behavior; shared cache operations
do not observe cancellation.

## Maintenance and CI integration

Use the shared [maintenance CLI](../../../../src/tools/BuildCache/README.md) with `--cache illink`,
or select an explicit directory. Command parsing, cache-name mappings, packaging, and installation
are documented there; purge semantics are in the shared cache README.

The `CoreLib_Libraries_AnalyzersLinker` CI job opts in through `setup-build-caches.yml` on Windows and
Unix x64/arm64. Restore a job-private Azure cache, record build start, run the build and all users,
then purge against that timestamp before uploading the next snapshot. This job enables
`RunILLinkInBuild` and builds `tools.illink` to obtain the maintenance tool; a library project
reference to `ILLink.Tasks` does not build it.

Purge only after successful builds and after all users exit. Skip absent versioned storage and report
a missing maintenance tool. Preserve tool diagnostics without failing the build for maintenance
errors. Incremental skips and unreached work do not refresh usage. See the
[CI recipe and wiring](../../../tools/illink/illink-tasks.md#ci-purge-ordering).

The ILLink-specific follow-up is resolved child-runtime identity. Shared
maintenance coordination and restoration limitations belong in the shared README.
