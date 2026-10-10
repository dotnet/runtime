# Experimental ILLink cache maintenance

`dotnet-illink-cache` is a .NET tool for maintaining the experimental ILLink MSBuild task cache.
Its commands and the cache format may change without notice. Use the tool built from the same
revision as the task. It does not manage Roslyn/csc or native compiler caches.

```sh
dotnet illink-cache purge --cache-directory /path/to/cache --before 2026-10-01T12:00:00Z
```

Both options are required. The cutoff must be an ISO 8601 UTC timestamp, with `Z` or `+00:00`.
Entries last used strictly before the cutoff are deleted; entries at or after it are kept.
The tool prints deleted, kept, and error counts. Argument or maintenance errors return a
nonzero exit code. Missing, invalid, or unreadable usage markers use the entry directory's
creation time instead; this fallback is reported on stderr but does not count as an error.

**No build may use the cache during purging.** The tool does not coordinate with active
readers or writers. Staging directories and unrelated directories are left alone.
There is no migration of entries created before usage markers were introduced.

## Building locally

From the repository root:

```sh
./dotnet.sh pack src/tools/illink/src/ILLink.CacheTool/ILLink.CacheTool.csproj -c Release
```

The package is produced under `artifacts/packages/Release/NonShipping`. Install the exact
locally built version with `dotnet tool install --tool-path <tools-directory>
--add-source artifacts/packages/Release/NonShipping dotnet-illink-cache --version <version>`,
then add `<tools-directory>` to `PATH` or invoke its `dotnet-illink-cache` executable directly.
Alternatively, run the built `dotnet-illink-cache.dll` with `dotnet` directly.
