# Experimental build invocation cache maintenance

`dotnet-build-cache` is a .NET tool for maintaining experimental MSBuild invocation caches.
Its commands and the cache format may change without notice. Use the tool built from the same
revision as the task. It does not manage Roslyn/csc or native compiler caches.
The [shared cache README](../../tasks/Common/InvocationCache/README.md) describes the task contract,
storage format, and lifecycle; this document covers the maintenance CLI.

```sh
dotnet build-cache purge --cache illink --before 2026-10-01T12:00:00Z
dotnet build-cache purge --cache crossgen2 --before 2026-10-01T12:00:00Z
dotnet build-cache purge --cache-directory /path/to/cache --before 2026-10-01T12:00:00Z
```

Choose `--cache` for a named cache or `--cache-directory` to select a directory directly.
Named caches use the corresponding `ILLINK_EXPERIMENTAL_CACHE_PATH` or
`CROSSGEN2_EXPERIMENTAL_CACHE_PATH`, falling back to the task's platform default (`illink`
or `crossgen2` under the user cache directory) when the environment variable is unset or empty.
If both options are supplied, `--cache-directory` determines the target; the name is validated
and only affects diagnostic labels. It does not check that the directory belongs to that cache.
An unknown name or invalid selected directory is an error; the tool does not fall back from
an invalid path or create missing directories. Maintenance ignores the boolean cache-enablement
environment variables, so disabled caches can still be maintained.

`--before` is required and must be an ISO 8601 UTC timestamp, with `Z` or `+00:00`.
Entries last used strictly before the cutoff are deleted; entries at or after it are kept.
The tool prints deleted, kept, and error counts. Argument or maintenance errors return a
nonzero exit code. Missing, invalid, or unreadable usage markers use the entry directory's
creation time instead; this fallback is reported on stderr but does not count as an error.

**No build may use the cache during purging.** The tool does not coordinate with active
readers or writers. Staging directories and unrelated directories are left alone.
There is no migration of entries created before usage markers were introduced.
The tool maintains only the shared `v1` directory. It does not read manifests, so
obsolete or incompatible entries within `v1` can be purged using the same cutoff.
Other version directories are left untouched.

## Building locally

From the repository root:

```sh
./build.sh tools.buildcache -c Release
./dotnet.sh pack src/tools/BuildCache/src/BuildCache.csproj -c Release
```

The package is produced under `artifacts/packages/Release/NonShipping`. Install the exact
locally built version with `dotnet tool install --tool-path <tools-directory>
--add-source artifacts/packages/Release/NonShipping dotnet-build-cache --version <version>`,
then add `<tools-directory>` to `PATH` or invoke its `dotnet-build-cache` executable directly.
Alternatively, run the built `artifacts/bin/BuildCache/Release/net*/dotnet-build-cache.dll`
with `dotnet` directly. The `tools.buildcachetests` subset builds its tests; the existing
`tools.illink` and `tools.illinktests` subsets also include the tool and tests respectively.
