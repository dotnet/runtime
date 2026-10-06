# ILLink.Tasks

## Terminology

Trimming allows a developer to reduce the final size of the application by removing unused managed code (IL) and other assets. Any publicly visible MSBuild property or item group name should use the term "trim" to refer to this capability.

The trimming process exposed to MSBuild via the ILLink.Tasks assembly which calls into ILLink tool to perform the actual trimming. The ILLink tool produces the trimmed output and can issue its own [warnings and errors](error-codes.md). All of them use `ILLink` prefix and unique code for easier identification.

*Note: This is similar to Compiler/csc: the capability is "compilation" and the related MSBuild/SDK integration uses "compile" as the term. The actual tool is csc and the errors/warnings coming from it are prefixed with CSC/CS.*

## Usage

To enable ILLink set `PublishTrimmed` property to `true` in your project and publish your app as self-contained.

```
dotnet publish -r <rid> -c Release -p:PublishTrimmed=true
```

alternatively you can edit your .csproj file to include

```xml
  <PropertyGroup>
    <PublishTrimmed>true</PublishTrimmed>
  </PropertyGroup>
```

The output will include only necessary code to run your application. The framework libraries size will be reduced noticeably.

## Experimental task cache

> [!WARNING]
> This feature is experimental. Its configuration and behavior may change or be removed without notice.
> Neither the configuration nor the on-disk format is a supported compatibility surface.

Set `ILLINK_EXPERIMENTAL_CACHE=true` in the environment before starting MSBuild to enable caching.
The boolean value is case-insensitive; unset, empty, or `false` disables caching. Invalid boolean
values disable caching with a diagnostic. Set `ILLINK_EXPERIMENTAL_CACHE_PATH` to override the
cache directory. Setting a path alone does not enable caching, and path values are never
interpreted as booleans. These settings apply only to the ILLink task, not the linker CLI or ILC.

For example, in a POSIX shell:

```sh
ILLINK_EXPERIMENTAL_CACHE=true ILLINK_EXPERIMENTAL_CACHE_PATH=/path/to/cache \
  dotnet publish -r <rid> -c Release -p:PublishTrimmed=true
```

These are environment variables, not MSBuild properties or task parameters. The task reads
its process environment on each invocation; do not change it during parallel builds, and ensure
reused build processes receive the intended environment.

The cache helper resolves an unset or empty directory override to `$XDG_CACHE_HOME/illink` (or
`$HOME/.cache/illink`) on Linux/other XDG Unix, `$HOME/Library/Caches/illink` on macOS,
and `LocalApplicationData/illink` on Windows. Relative `XDG_CACHE_HOME` values are ignored;
explicit relative cache directories are resolved against the working directory.

Eligible invocations use a key covering arguments, input-file and sidecar contents, the task
assembly, the selected dotnet host executable, and the entire ILLink assembly.
File contents are hashed on every invocation. Linker changes invalidate the cache even when
the module version ID (MVID), file length, and timestamp are unchanged.
Bundled dependencies and configuration are not fingerprinted. Changes to those files require
a cleared/isolated cache or disabled caching unless the linker binary also changes.

The key does not cover the host's installed runtime directories or the version of the runtime
executing ILLink. Runtime installation changes alone are not guaranteed to invalidate entries;
clear or isolate the cache when that distinction is required.
Cache hits replace the output directory without running ILLink.
Directories are created in the cache only when storing a successful result. Hits do not replay warnings or other linker diagnostics.

Caching is bypassed with a diagnostic for non-whitespace `ExtraArgs`, custom steps/data,
dependency-dump options, and explicit task environment overrides. Options supplied through
`ExtraArgs` remain unsupported for caching; these invocations still run the linker normally.
Inherited environment variables are not tracked; disable caching
when they affect outputs or dependencies beyond the keyed inputs, or require tool-execution
side effects such as startup hooks or profiling. Unreadable inputs or linker files also
fall back to normal linking. See the
[cache design](../../design/tools/illink/task-cache.md) for identity and eligibility details.

### Cache maintenance

The experimental `dotnet-illink-cache` .NET tool can purge entries that were not used recently:

```sh
dotnet illink-cache purge --cache-directory /path/to/cache --before 2026-10-01T12:00:00Z
```

The directory and UTC cutoff are required; maintenance does not use the task's opt-in or
directory environment variables. Use the tool built from the same revision as the task.
See the [tool README](../../../src/tools/illink/src/ILLink.CacheTool/README.md) for local
packaging and installation.

Each entry has a `last-used` timestamp, initialized during publication and refreshed after
a successful cache hit. Only entries strictly older than the cutoff are deleted. Staging
directories, unrelated directories, and other layout versions are not purged. There is no
size limit or automatic eviction during linking.

**Run purge only when no build is using that cache.** Purge and manual deletion are not
coordinated with readers or writers. Missing, malformed, non-UTC, or unreadable markers use
the entry directory's creation time as a fallback; the tool reports this on stderr without
counting it as an error. Such entries may be deleted despite recent use, or retained if
restoring a cache snapshot reset directory creation times. The tool reports
deleted/kept/error counts and returns nonzero for argument or maintenance failures.
Usage-marker update failures in the task are logged without failing linking.

### CI purge ordering

For a CI cache snapshot, use the build-start timestamp as the cutoff to retain entries used
or created by that build:

1. Restore the cache into a job-private directory and set `ILLINK_EXPERIMENTAL_CACHE=true`
   and `ILLINK_EXPERIMENTAL_CACHE_PATH` to that explicit directory.
2. Record the UTC timestamp before starting any linking. In a POSIX shell:
   ```sh
   export ILLINK_CACHE_BUILD_START="$(date -u +'%Y-%m-%dT%H:%M:%SZ')"
   ```
3. Run the build and wait for all processes using the cache to finish.
4. Run the tool from the same revision, with maintenance failures reported but nonfatal:
   ```sh
   if ! dotnet illink-cache purge \
       --cache-directory "$ILLINK_EXPERIMENTAL_CACHE_PATH" \
       --before "$ILLINK_CACHE_BUILD_START"; then
     echo "Warning: ILLink cache purge failed; see the tool diagnostics above." >&2
   fi
   ```
5. Save/upload the resulting cache directory only after purge finishes. For Azure Pipelines
   `Cache@2`, the purge step must precede its post-job save.

If the build and purge run in different CI steps, persist `ILLINK_CACHE_BUILD_START` as a
pipeline variable; a shell export alone does not carry it across steps. This variable is
only for the CI recipe, not another task configuration setting.

A failed build can still be followed by purge once all linking has stopped, but entries for
work that was never reached will count as unused. Likewise, incremental builds that skip
linking do not refresh cache usage. Skip purge if the cache may still be in use, including
during cancellation. Do not share a live directory with another job while purging.

This is usage guidance, not automatic CI enablement. ILLink cache restore/upload wiring
is separate from runtime's Roslyn/csc cache maintenance.

## ILLink Task Properties

### ExtraArgs

Additional [options](illink-options.md) passed to ILLink. Use `OutputDirectory` to select the output location; its value takes precedence over `-o` or `-out` arguments supplied here.

### OutputDirectory

The dedicated directory in which to place processed assemblies. Before running ILLink, the task deletes this directory and all its contents, then recreates it. If cleanup fails, the task fails without running the tool.

Previously, the SDK targets deleted only candidate assembly and PDB outputs. Other files could survive from earlier runs. Callers must now keep all inputs and any files they want to preserve outside `OutputDirectory`, including inputs supplied through `ExtraArgs`. Concurrent task invocations must use different output directories.

This is a breaking change for task callers that share the output directory with other build steps or override it through `ExtraArgs`. Set `OutputDirectory` to a dedicated linker output directory, then copy or move its results into any shared destination after the task succeeds. The SDK publish integration already uses a dedicated directory.

This cleanup occurs only when the task executes; an up-to-date target leaves its outputs intact. Running the command-line linker directly still permits a nonempty output directory and does not perform this cleanup.

### ReferenceAssemblyPaths

Assembly files with paths to assemblies needed as references.

### RootAssemblyNames

The names of the assemblies to root. This should contain assembly names without an extension, not file names or
paths.

### RootDescriptorFiles

A list of XML [descriptors](data-formats.md#descriptor-format) files specifying trimmer roots at a granular level.

## ILLink Task Customization

The trimmer can be invoked as an MSBuild task, `ILLink`. We recommend not using the task directly, because the SDK has built-in logic that handles computing the right set of reference assemblies as inputs, incremental trimming, and similar logic. If you would like to use the [advanced options](illink-options.md), you can invoke the msbuild task directly and pass any extra arguments like this:

```xml
<ILLink AssemblyPaths="@(AssemblyFilesToLink)"
        RootAssemblyNames="@(LinkerRootAssemblies)"
        RootDescriptorFiles="@(LinkerRootDescriptors)"
        OutputDirectory="output"
        ExtraArgs="-t --trim-mode link" />
```

## Default Trimming Behavior

The default in the .NET Core SDK is to trim framework assemblies only, in a conservative assembly-level mode (`copyused` action). Third-party libraries and the app will be analyzed but not trimmed. Other SDKs may modify these defaults.

## Customizing Trimming Behavior

`TrimMode` can be used to set the trimming behavior for framework assemblies. Additional assemblies can be given
metadata `IsTrimmable` and they will also be trimmed using this mode, or they can have per-assembly `TrimMode` which
takes precedence over the global `TrimMode`.

## Reflection

Note: this section is out-of-date. New versions of the trimmer can understand some of these reflection patterns.

Applications or frameworks (including ASP<span />.NET Core and WPF) that use reflection or related dynamic features will often break when trimmed, because the trimmer does not know about this dynamic behavior, and can not determine in general which framework types will be required for reflection at runtime. To trim such apps, you will need to tell the trimmer about any types needed by reflection in your code, and in packages or frameworks that you depend on. Be sure to test your apps after trimming.

If your app or its dependencies use reflection, you may need to tell the trimmer to keep reflection targets explicitly. For example, dependency injection in ASP<span />.NET Core apps will activate
types depending on what is present at runtime, and therefore may fail
if the trimmer has removed assemblies that would otherwise be
present. Similarly, WPF apps may call into framework code depending on
the features used. If you know beforehand what your app will require
at runtime, you can tell the trimmer about this in a few ways.

For example, an app may reflect over `System.IO.File`:
```csharp
Type file = System.Type.GetType("System.IO.File,System.IO.FileSystem");
```

To ensure that this works:

- You can include a direct reference to the required type in your code
  somewhere, for example by using `typeof(System.IO.File)`.

- You can tell the trimmer to explicitly keep an assembly by adding it
  to your csproj (use the assembly name *without* extension):

  ```xml
  <ItemGroup>
    <TrimmerRootAssembly Include="System.IO.FileSystem" />
  </ItemGroup>
  ```

- You can give the trimmer a more specific list of types or members to
  include using an xml [descriptor](data-formats.md#descriptor-format) file

  `.csproj`:
  ```xml
  <ItemGroup>
    <TrimmerRootDescriptor Include="TrimmerRoots.xml" />
  </ItemGroup>
  ```

  `TrimmerRoots.xml`:
  ```xml
  <linker>
    <assembly fullname="System.IO.FileSystem">
      <type fullname="System.IO.File" />
    </assembly>
  </linker>
  ```

## Caveats

Sometimes an application may include multiple versions of the same
assembly. This may happen when portable apps include platform-specific
managed code, which gets placed in the `runtimes` directory of the
publish output. In such cases, the trimmer will pick one of the
duplicate assemblies to analyze. This means that dependencies of the
un-analyzed duplicates may not be included in the application, so you
may need to root such dependencies manually.
