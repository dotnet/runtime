# Experimental invocation cache

These sources cache successful MSBuild task invocations and are compiled into each consumer. The API
and storage format are experimental, without compatibility guarantees.

## Integrating a task

Call `InvocationCache.ExecuteCached` with:

- An `InvocationCacheConfiguration` naming the tool and its enable/path environment variables. Only
  `true` enables caching; a path alone does not. Without an override, storage uses the platform's user
  cache directory.
- A description callback returning ordered input paths, effective arguments/context, and an owned
  directory or file set through `InvocationDescription`. Return null for unsupported invocations.
- A normal-execution callback that prepares outputs and runs the tool independently of caching.

The task must inventory all relevant inputs and toolchain files, including its own assembly. The
shared layer hashes declared files with SHA-256; it does not interpret arguments or discover
dependencies. Inputs must remain stable through discovery, hashing, and execution. Inherited
environment settings and installed runtime trees are not automatically fingerprinted.

An `InvocationDescription` can specify an effective working directory and a path mapping that
assigns logical identities to input files, output locations, and that working directory. File
access, output validation, and restoration still use the physical absolute paths; only key
serialization uses the mapped identities, captured when the description is constructed rather than
recomputed while hashing. Without a mapping, identities remain absolute.
Consumers must preserve complete relative layouts and distinguish external paths rather than
mapping unrelated files to the same identity. ILLink supplies its root-aware mapping this way.

Prepare any execution-path normalization independently of the cache callbacks so disabling or
bypassing caching does not change the invocation.

## Behavior and limitations

Disabled caching skips discovery and cache I/O. Hits validate and restore outputs without running the
tool. Misses and recoverable cache failures run it normally; only exit code zero with no logged errors
is stored. Failures are diagnosed, and failed tool execution is never retried. Invalid output
declarations, including cache/output overlap, fail before cleanup rather than bypassing caching.

Normal execution owns output preparation, including removal of stale or partially restored files.
The cache owns reconciliation only on hits, including deleting declared files recorded as absent.
Restoration is not transactional. Empty directories are not preserved, and symbolic links are not
checked.

Cross-worktree reuse requires stable logical path identities and a matching execution contract;
mapping identities does not rewrite tool arguments or normalize embedded paths in file contents.
Hits do not replay diagnostics or non-file task outputs. Cache operations do not observe cancellation.
Consumers must bypass invocations whose dependencies or required side effects cannot be modeled.

Entries are staged locally and published atomically. Concurrent processes on one machine may share
storage when writing distinct destinations; same-destination execution and distributed writers are
unsupported.

There is no automatic eviction. Corrupt or incompatible entries are not repaired; obsolete entries
within the current `v1` layout remain purgable. Purge or manual deletion must not race active users.

See the [ILLink integration](../../../../docs/design/tools/illink/task-cache.md) for eligibility and
toolchain omissions, and the [maintenance tool README](../../../tools/BuildCache/README.md) for purge
behavior and commands.
