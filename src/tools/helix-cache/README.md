# Helix result cache prototype

A C# NativeAOT CLI for a local, trusted filesystem cache of Linux x64 Helix
work-item results. An opt-in Azure Pipelines shadow lane exercises actual
submission and collection without replacing any test executions or results.

## Build and exercise

From the repository root, using its configured SDK and NuGet feeds:

```sh
dotnet publish src/tools/helix-cache/src/HelixCache.csproj -c Release -r linux-x64 \
  -o artifacts/tools/helix-cache
dotnet test src/tools/helix-cache/tests/HelixCache.Tests.csproj -c Release
```

NativeAOT publishing requires a supported native compiler/linker and development
libraries. For managed development, build with `-p:PublishAot=false` and run the
generated apphost with the SDK runtime available.

## Structure

`src/` contains the executable project; `tests/` is a sibling xUnit project.
Tests exercise the published NativeAOT executable and inject HTTP responses for
Helix collection. Neither project needs source-glob exclusions.

The command pipeline is:

1. `CommandLine` parses and validates arguments into command-specific records before any I/O.
2. `Submission` validates work items and payload declarations; `LinuxEnvironment` resolves the declared queue environment.
3. `Cache.Lookup` freezes shared inputs once per lookup, constructs execution identities, and checks completed entries.
4. `HelixClient` handles typed HTTP responses, completion polling, and result downloads.
5. `Results.Read` validates receipts and reports and produces a `TestRun` containing counts and comparable outcomes.
6. `ShadowValidation` compares a fresh run with any cached run before `Cache.Store` publishes a passing entry atomically.

JSON contracts use source-generated System.Text.Json serialization.
`HelixJsonContext` uses the service's PascalCase names for transport models.
`CacheJsonContext` uses camelCase for local models, including environment
fingerprints. Image/provisioning descriptions remain opaque JSON because their
open-ended fields must all participate in the key. Canonical key encoding is
separate from deserialization and domain validation. Prototype formats and keys
may change during development; existing entries are disposable.

## Lookup

```sh
helix-cache lookup submission.json --cache /path/to/cache --output lookup.json
```

Example `submission.json`:

```json
{
  "schemaVersion": 1,
  "workItems": [
    {
      "name": "Example.Tests",
      "queues": ["AzureLinux.3.Amd64.Open"],
      "payload": { "path": "Example.Tests.zip" },
      "correlationPayloads": [
        { "path": "test-runtime.zip", "destination": "" }
      ],
      "command": "./RunTests.sh",
      "preCommands": [],
      "postCommands": [],
      "environment": { "DOTNET_TieredCompilation": "1" },
      "timeoutSeconds": 900,
      "expectedExitCode": 0
    }
  ]
}
```

Input paths are relative to the submission manifest. A payload is either a ZIP
archive or a directory. ZIP snapshots are rewritten in ordinal entry order with
fixed timestamps and compression, preserving file contents, external attributes
(including Unix modes and symlinks), and empty directories. Duplicate paths,
traversal, and file/directory conflicts are rejected. Directory snapshots preserve
Unix modes and empty directories and normalize filesystem timestamps. Symlinks in
directory inputs/results are rejected; put linked payloads in an archive. Shared
inputs are captured once per invocation. Only frozen archives are rewritten;
original archives are never changed.

The tool resolves the actual host queue, requires an available Linux AMD64 queue,
and fingerprints its declared image, ordered provisioning recipe and workspace
path. Docker targets use Arcade's `(alias)host@image` syntax and must include a
lowercase `@sha256:` digest. Operational queue state is excluded. The full target
string remains part of the execution identity, including its alias.

For offline use or deterministic tests, supply `--queue-info queues.json`, where
the file is a trusted snapshot of the array returned by
`https://helix.dot.net/api/2019-06-17/info/queues`. Without that option the tool
fetches queue information once from that endpoint.

Each `lookup.json` entry contains `status` (`hit` or `miss`), an `execution`
record, and a `resultsPath` for hits. Hits and misses both exit zero; malformed
inputs, HTTP failures, unreadable files and corrupt entries exit nonzero.
Diagnostics go to stderr. The caller must retain every requested item when
combining cached and fresh results, including an all-hit submission.

**Submit the snapshot paths in the execution record, not the original input
paths.** Treat snapshots as immutable. If the sender needs writable directories
(the Helix SDK can write `.payload` markers), make separate submission copies
from these snapshots. Do not resolve inputs again after computing the key.

## Store completed results

Save one entry's `execution` object from `lookup.json` as `execution.json`, and
collect the original work item's results into a directory:

```sh
helix-cache store execution.json --results downloaded-results --cache /path/to/cache
```

The directory must include a `result.json` receipt constructed by the trusted
result collector from Helix's completed-work-item status:

```json
{
  "schemaVersion": 1,
  "completed": true,
  "infrastructureSucceeded": true,
  "exitCode": 0,
  "jobId": "00000000-0000-0000-0000-000000000001",
  "workItemName": "Example.Tests",
  "targetQueue": "AzureLinux.3.Amd64.Open",
  "resultFiles": ["testResults.xml"]
}
```

`infrastructureSucceeded` must be false for Helix infrastructure errors,
cancellations or timeouts. Submission success is not completion. This tool does
not independently authenticate the receipt with Helix.

The prototype accepts xUnit XML (`assemblies` / `assembly` / `test`) only. Each assembly must
contain tests whose results agree with its total/passed/failed/skipped counts;
failures, errors, empty or incomplete reports are rejected. Skips are retained.
All files in the results directory are copied and checksummed. The execution key
and input snapshots are checked before publishing an entry. Atomic directory
renames make concurrent stores safe; an existing valid entry wins.

Store uses the environment captured at lookup, never today's queue metadata.
Lookup checks artifact integrity and reports corruption as an error rather than
returning a cached pass. Original job provenance and reports are retained.
Cached results must be labeled as reused when published; no new Helix job is
invented. TRX/JUnit, cache eviction and replacing normal MSBuild test submissions
with cached results are outside this prototype.

## Azure Pipelines shadow validation

Queue `eng/pipelines/runtime.yml` with its `helixCacheShadow` boolean parameter
enabled. This draft temporarily defaults to **true** for validation; disable it
before merging. The additional steps run only in the public
project's Linux x64 `CoreCLR_AllSubsets` job, after its normal submissions.
They use the existing build's test archives for `System.Memory.Data.Tests`,
`System.Runtime.CompilerServices.Unsafe.Tests`, `System.IO.Hashing.Tests`, and
`System.Text.Encodings.Web.Tests`. All four work items are submitted in one
additional job to `AzureLinux.3.Amd64.Open`, sharing one frozen runtime archive.

`Cache@2` restores `$(Pipeline.Workspace)/.helix-cache/entries`, using a rolling
per-build snapshot key and a stable restore prefix, as the compiler caches do.
The cache task's build ID is **not** part of an execution digest. Only completed
entries are persisted; frozen input snapshots are recreated in each job.
Existing Azure Pipelines branch/PR cache scoping applies.
An Azure restore hit is not a native result hit. Check the post-job cache-save
log before concluding that a new snapshot persisted: the agent can print
`Failed to cache item.` followed by `Saved item.` while leaving the task green.

All orchestration runs in the C# NativeAOT executable. Its `prepare` command
records the full command, upload post-command, timeout, queue and SHA-256 digest
of the pinned Helix SDK package, runs native lookup, and exports frozen paths to
`submission.props`. Repeat `--payload` for each suite; work-item names come from
archive basenames and must be unique (case-insensitively), using ASCII letters,
digits, dots, underscores or hyphens, excluding empty names, `.` and `..`.
`--helix-sdk` must identify that installed SDK's `.nupkg`.
`eng/testing/helix-cache-shadow.proj` submits those exact archives through the
pinned Helix SDK without injecting extra reporters. The SDK generates its own
command wrapper, including chmod and exit-code handling; its package digest
participates in the key as `commandGeneratorDigest`, and submission checks the
installed package against that digest. The generated UUID script directory is
not stable and is not fingerprinted literally. This assumes those random wrapper
paths are irrelevant to the tests; it is not a byte-for-byte fingerprint of every
SDK-generated archive. Its default target only
validates inputs; submission requires **`-t:Submit`**, which waits for completion
and cancels the submitted job on task cancellation.

`collect` checks the public Helix API's job, submitted queue alias (or queue ID
when no alias is reported), work-item identity, terminal state, infrastructure
errors, failure reason and exit code, then downloads the uploaded
xUnit XML and console log. It constructs the receipt from that completion,
passes the report through native admission, and compares any hit with the fresh
test identities/outcomes/skip reasons. Ordering, worker directory prefixes and
execution timings are ignored. The resolved physical queue ID and submitted
alias are preserved in provenance alongside the original cached job.
Every suite's report and comparison must pass before any suite is stored.
Each entry is published atomically, not the entire batch. Every hit still
executes on Helix; no cached result is published as a new run.

Evidence is included in the existing build-log artifact under
`artifacts/log/<Configuration>/helix-cache-shadow/`, including `lookup.json`,
`summary.json` with per-suite keys, hit/miss status, test counts and comparisons,
and `work-items/<suite>/{execution.json,work-item.json,results/}`. Shadow
errors are visible but use `continueOnError`, so they do not replace or weaken
the normal test decision. Internal queues and authentication are not supported
by this first shadow lane.

For local preparation without submitting, after building the archives:

```sh
sdk="$(dotnet msbuild eng/testing/helix-cache-shadow.proj -nologo -getProperty:HelixCacheSdkPackage)"
artifacts/tools/helix-cache/helix-cache prepare \
  --cache artifacts/tmp/helix-cache \
  --output artifacts/log/Release/helix-cache-shadow \
  --payload artifacts/helix/tests/linux.AnyCPU.Release/System.Memory.Data.Tests.zip \
  --payload artifacts/helix/tests/linux.AnyCPU.Release/System.IO.Hashing.Tests.zip \
  --runtime artifacts/helix/runtime/test-runtime-net11.0-linux-Release-x64.zip \
  --queue AzureLinux.3.Amd64.Open --helix-sdk "$sdk"
dotnet msbuild eng/testing/helix-cache-shadow.proj -t:Validate \
  -p:HelixCacheShadowDirectory="$PWD/artifacts/log/Release/helix-cache-shadow"
```

The integration suite mocks public completion/file endpoints; live submission,
cross-build restoration and unchanged-rebuild hit rates require shadow validation.
Repacking unchanged ZIP members no longer changes the frozen bytes or key.
Changed binary contents, permissions, paths, and other execution inputs still
invalidate results, even when source changes appear unrelated to a test.

## Cache contract

Keys include the schema namespace, work-item name, target queue, declared Linux
environment, payload identities and destinations, ordered correlation overlays,
commands, an optional command-generator digest, environment overrides, timeout
and expected exit code. Callers that inject generated commands must fingerprint
their generator or provide its actual frozen output. Object
properties are sorted ordinally; command and overlay order is preserved. Local
absolute paths and result timestamps are not key material.

This is a fingerprint of the **declared Linux environment**, not an attestation of
the worker's installed packages, kernel or CPU features. Native queues can drift
without changing their published recipes; a pinned container identifies its
userspace but not its host. External services and randomized/stress trials may
also make result reuse inappropriate. Enable caching only where that equivalence
contract is acceptable. Build-specific command arguments remain in the key;
there is no heuristic removal of build IDs or reporting arguments.

The cache and manifests are trusted local inputs, not a shared-cache security
boundary. Input snapshots live under `inputs/v1`; completed results are under
`entries/v1/<prefix>/<key>`. Do not mutate entries or delete snapshots while
their executions are outstanding.
