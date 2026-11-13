# ILLink task cache integration

ILLink opts eligible task invocations into the
[shared invocation cache](../../../../src/tasks/Common/InvocationCache/README.md). This document
covers ILLink's input discovery, root-aware identity, output ownership, and deliberate omissions;
the shared README owns storage, restoration, and concurrency. The feature is experimental and may change or be
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
and `ExecuteToolCore`. The base class checks whether caching is enabled before describing the invocation.
Argument construction records input roles and one structured argument list in a single walk over
the task inputs. Registering a path resolves it once against the captured caller's working
directory. Argument records and input/output inventories share that physical full path.
Known path arguments are passed to ILLink as full paths, without changing its working directory.
The command-line and response-file hooks reuse that construction;
execution does not reconstruct or compare arguments.
The description callback expands the recorded inputs through `DescribeInvocation`, normalizes
newly discovered dependencies at registration, supplies the effective working directory, and declares
one owned output directory; it runs only when caching is enabled.
Description construction retains prepared physical paths and a source-root map. Only key construction
maps paths to portable identities, including known paths in the structured arguments. It maps each
distinct physical path once per key and orders the inventory by mapped identity. File reads, output
validation, and restoration use the stored physical paths directly. Normal execution uses
the already-generated arguments regardless of cache eligibility. ILLink owns metadata inspection
and path-independence checks; the shared layer owns key serialization, full-content hashing,
publication, restoration, and writer coordination.

For an eligible invocation, the base class calls the cache's instance `TryExecuteCached` method.
On a miss, the base class captures stdout/stderr during `ExecuteToolCore` and returns an
`InvocationResult` with the exit code and those buffers for storage. On a hit, the cache returns
the restored result. `CachedToolTask` replays output on both hits and misses through ordinary
`ToolTask` logging with the current stream policy, then determines the final task exit code from
the execution result and logged errors. Bypassed invocations execute directly without capture.

## Root identity and execution

Share eligible results across clones/worktrees with equivalent relative layouts. Accept physical
`SourceRoots` with stable logical `MappedPath` metadata (`/_/`, `/_1/`, etc.). Map known paths to
tagged logical-root/relative-path identities during key construction; retain absolute external paths.
Require distinct roots and consistent nested mappings. Missing or invalid mappings run uncached.
Independent roots, such as a checkout and a NuGet package directory, need not retain their physical
placement relative to each other.

Resolve original relative inputs against the caller's cwd and pass known paths as full paths,
whether caching is enabled, disabled, or bypassed. Do not select an execution root, change the
working directory, or mutate task inputs. Source-root directories need not exist unless the invocation
actually reads files from them. Unsupported opaque extra arguments retain their original spelling.

Use the shared SHA-256 key builder over a length-delimited description with ILLink-specific context. Include
mapped working directory, generated arguments with mapped path tokens, logical output identity,
platform, architecture, culture, and full file hashes sorted by logical input identity. Do not
replace substrings in raw arguments. Preserve filenames, relative layout, reference precedence,
sidecar associations, and ordered search directories. The model describes the task's built-in
argument generation and working directory; it does not detect or protect against subclass overrides.

Runtime per-library, shared-framework, out-of-band, and SDK publish targets forward the compiler's
existing `@(SourceRoot)` items after `InitializeSourceRootMappedPaths`. Aggregate runtime projects
import the managed compiler's mapping target explicitly because their SDK does not compile sources.
Initialization uses the existing `DeterministicSourcePaths` setting: deterministic builds normally
receive stable logical prefixes; other builds receive physical mappings and run uncached.
Root forwarding does not normalize upstream assemblies or
enable symbol preservation or compiler path mapping.

## Input discovery and eligibility

Hash effective assemblies, reference assemblies, and root descriptors recorded during argument
construction. Include adjacent PDBs,
configuration files, and satellite assemblies even when symbols are disabled. Adding or removing a
sidecar changes identity. Leave assembly filename/name mismatches and unsupported additional modules
to ILLink rather than preflight-rejecting them. Reject path-shaped metadata assembly references.
Leave root and XML assembly-name validation to ILLink, which treats path-shaped names as unresolved
rather than probing outside the inventory.

Linked resources with stored hashes do not introduce an external file read during rewriting.
Dependency tracking for the unusual missing-hash case, where Cecil can read the resource file to
compute an output hash, is deferred; such inputs are outside the cache's supported dependency model.

Inspect CodeView paths, portable-PDB document paths, and embedded portable PDBs.
Accept relative non-escaping paths and conventional synthetic `/_N/` paths; reject physical symbol
paths, native PDBs, MDBs, malformed symbols, and unsupported debug-directory kinds. External portable
PDB emission requires explicit `PreserveSymbolPaths=true` with already-normalized inputs. Copy
actions also require shareable input bytes. Explicit skipped references are metadata-inspected and
fully hashed, without output-symbol restrictions because their symbols are not emitted.

Do not scan input contents for physical source-root prefixes. Embedded checkout paths, including
`CallerFilePath` strings and custom-debug blobs, participate in full-content hashes: changing them
changes the key, while identical bytes can share an entry. Compiler and symbol defaults are unchanged.

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
architecture, and culture are part of the ILLink key, but callers must disable caching when inherited
settings introduce untracked dependencies, affect outputs, or require side effects such as startup
hooks or profiling.

Bypass arbitrary `ExtraArgs`, custom steps/data, dependency-dump options, and explicit
`ToolTask.EnvironmentVariables` overrides. These can introduce undeclared inputs, outputs, or side
effects.

For runtime library builds, support `--ignore-link-attributes true` followed by zero or more
`--link-attributes FILE`, `--substitutions FILE`, or `-d DIRECTORY` pairs. Require the XML files to be
readable. Inventory every top-level `.dll`, `.exe`, and `.winmd` candidate in each search directory,
including supported sidecars. Preserve directory order and leave linker resolution
unchanged; adding, changing, or removing even a shadowed candidate invalidates identity. Other SDK
options supplied through `ExtraArgs` remain unsupported.

Bypass `ExtraArgs` containing backslashes immediately before quotes, including even-length runs.
The supported tokenizer does not model the linker's escape rules; leave those invocations
unchanged rather than reconstruct different argument boundaries.

Unreadable directories, uninspectable assemblies (including PE images without managed metadata), and
other input-read failures bypass caching with diagnostics. Required files must be readable; normal
linking decides whether an unsupported invocation can succeed. Inputs must remain stable
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
