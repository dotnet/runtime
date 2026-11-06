# Wasm app build

This usually consists of taking the built assemblies, and related files, and generating an app bundle.

Browser builds default the trimming discovery root `TypeMapEntryAssembly` to the
application's assembly name. The JSExport provider linker descriptor preserves
the native `BindAssemblyExports` entry point, which makes the type-map lookup
reachable even when JavaScript is the only caller of the application's exports.

Runtime discovery reads only the requested assembly's local type map, including
its ReadyToRun map when available. It does not follow assembly-target edges or
require unrelated lazy assemblies to have been downloaded. Assembly-target
attributes remain available for build-time trimming.

One generated provider per assembly uses the assembly name as its type-map key.
Its `ReadOnlySpan<string> GetExportNames()` catalog supplies fully qualified, hash-qualified names without
enumerating the type map. Discovery activates the provider and publishes deferred
functions without binding methods. The generated catalog method registers signature
metadata through an internal unsafe-accessor bridge. JavaScript copies that metadata
before the temporary native buffers are freed and creates marshalers on first use.
Managed discovery forwards the catalog strings
unchanged to JavaScript, which derives the export paths, hashes, method IDs and aliases
using the same parser for both runtimes. The first overload in catalog order supplies
the plain-name alias. Each pending invocation handle identifies the provider and
its catalog index. Both runtimes invoke that provider's `Bind(string methodId)`
inside the first actual managed invocation, preserving
synchronous return values and stable function identities without JavaScript proxies.
Method IDs are invariant decimal catalog indexes, local to the generated assembly
build rather than stable names or signature hashes. A generated string switch binds
only the selected method. No managed preflight call or binder delegate is required.
Async exports use their normal thread dispatch, including on the Mono UI thread.
Legacy assemblies retain their existing registration path.
Private and protected nested exports use managed unsafe accessors to their generated
wrappers. Generic and file-local containing types retain the diagnostic and reflection fallback.
Mono's implementation is limited to an internal unconditional, assembly-local
lookup for this discovery path. Its public application-wide type-map APIs remain
unsupported.

Wasm app build can run in two scenarios:

1. After build, eg. when running the app in VS, or `dotnet build foo.csproj`
2. For Publish, eg, when publishing the app to a folder, or Azure

A dotnet wasm app has some native wasm files (`dotnet.wasm`, and `dotnet.js`). How these files are obtained, or generated:

1. Build
    - a. with no native libraries referenced (AOT setting is ignored here)
        - files from the runtime pack are used as-is
    - b. with native libraries referenced
        - dotnet.wasm is relinked with the native libraries
2. Publish
    - dotnet.wasm is relinked with the native libraries, and updated pinvoke/icalls from the trimmed assemblies
    - if `RunAOTCompilation=true`, then the relinking includes AOT'ed assemblies

## `Build`

Implementation:

- Target `WasmBuildApp`
- runs after `Build` by default
    - which can be disabled by `$(DisableAutoWasmBuildApp)`
    - or the run-after target can be set via `$(WasmBuildAppAfterThisTarget)`

- To run a custom target
    - *before* any of the wasm build targets, use `$(WasmBuildAppDependsOn)`, and prepend your target name to that
    - *after* any of the wasm build targets, use `AfterTargets="WasmBuildApp"` on that target
- Avoid depending on this target, because it is available only when the workload is installed. Use `$(WasmNativeWorkload)` to check if it is installed.

## `Publish`

Implementation:

- This part runs as a nested build using a `MSBuild` task, which means that the project gets reevaluated. So, if there were any changes made to items/properties in targets before this, then they won't be visible in the nested build.
- By default `WasmTriggerPublishApp` runs after the `Publish` target, and that triggers the nested build
    - The nested build runs `WasmNestedPublishApp`, which causes `Build`, and `Publish` targets to be run
    - Because this causes `Build` to be run again, if you have any targets that get triggered by that, then they will be running twice.
        - But the original *build* run, and this *publish* run can be differentiated using `$(WasmBuildingForNestedPublish)`

- `WasmTriggerPublishApp` essentially just invokes the nested publish
    - This runs after `Publish`
        - which can be disabled by `$(DisableAutoWasmPublishApp)`
        - or the run-after target can be set via `$(WasmTriggerPublishAppAfterThisTarget)`

    - To influence the wasm build for publish, use `WasmNestedPublishApp`
        - To run a custom target before it, use `$(WasmNestedPublishAppDependsOn)`
        - to run a custom target *after* it, use `AfterTargets="WasmNestedPublishApp"`

    - If you want to *dependsOn* on this, then use `DependsOnTargets="WasmTriggerPublishApp"`

# `WasmApp.{props,targets}`, and `WasmApp.InTree.{props,targets}`

- Any project that wants to use this, can import the props+targets, and set up the
various properties before the target `WasmBuildApp` gets executed.
  - the recommended way to do this is to prepend the target to `$(WasmAppBuildDependsOn)`

- Any wasm projects within the `dotnet/runtime` repo should use the `WasmApp.InTree.{props,targets}` files
  - These files have relevant properties/targets to work with a local build
- Generally, the props file can be imported at the top, and the targets file at the bottom of a project file.

- `WasmBuildApp` target is not run by default. The importing project will have
to do that.

- By default, the `WasmLoadAssembliesAndReferences` task is not run, and
the specified `@(WasmAssembliesToBundle)` are directly passed to
`WasmAppBuilder`.
	- If the project needs assembly dependencies to be resolved, then
	set `$(WasmResolveAssembliesBeforeBuild) == true`.
  - Should you need to run the AOT toolset, ensure `$(RunAOTCompilation) == true`
  and set `$(WasmAOTDir)` to the directory that you want to AOT. Make sure that both
  `@(WasmAssembliesToBundle)` and `$(WasmAOTDir)` are absolute paths.

- Assemblies to be bundled with the app are set via
`@(WasmAssembliesToBundle)` (which optionally will have dependencies
resolved)

The various task inputs correspond to properties as:

```
  AssemblySearchPaths               : @(WasmAssemblySearchPaths)

  Assemblies                        : @(WasmAssembliesToBundle)

  AppDir                            : $(WasmAppDir)
  MainAssembly                      : $(WasmMainAssemblyPath)
  SatelliteAssemblies               : @(WasmSatelliteAssemblies)
  FilesToIncludeInFileSystem        : @(WasmFilesToIncludeInFileSystem)
  DebugLevel                        : $(WasmDebugLevel)
  ExtraFilesToDeploy                : @(WasmExtraFilesToDeploy)

  MicrosoftNetCoreAppRuntimePackDir : $(MicrosoftNetCoreAppRuntimePackRidDir)
```

- `run-v8.sh` script is emitted to `$(WasmRunV8ScriptPath)` which defaults to `$(WasmAppDir)`.
    - To control it's generation use `$(WasmGenerateRunV8Script)` (false by default)

This should be a step towards eventually having this build as a sdk.

Refer to `WasmApp.targets` for more information about the properties/items used as inputs to the process.

## Updating dependencies needed for building wasm apps

For example, if the wasm targets are using a new task, then references to that
need to be updated in a few places. Essentially, look for existing references
to `MonoAOTCompiler`, or `WasmAppBuilder` in the relevant files, and duplicate
them for the new task assembly.

1. The task assembly dir, and its path need to be in two properties:
    ```xml
    <MonoTargetsTasksDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'MonoTargetsTasks', 'Debug', 'net'))</MonoTargetsTasksDir>
    <MonoTargetsTasksAssemblyPath>$([MSBuild]::NormalizePath('$(MonoTargetsTasksDir)', 'MonoTargetsTasks.dll'))</MonoTargetsTasksAssemblyPath>
    ```

    And this needs to be set in:
    - `Directory.Build.props`
    - `src/mono/wasm/build/WasmApp.LocalBuild.props`
    - `src/mono/wasm/build/WasmApp.LocalBuild.targets`
    - `src/tests/Common/wasm-test-runner/WasmTestRunner.proj`

2. The new dependency (eg. task assembly) needs to be sent to helix as a payload, see `src/libraries/sendtohelixhelp.proj`. Use `MonoAOTCompiler` as an example.

3. Make changes similar to the one for existing dependent tasks in
   - `eng/testing/linker/trimmingTests.targets`,
   - `src/tests/Common/wasm-test-runner/WasmTestRunner.proj`
   - `src/tests/Directory.Build.targets`

## Profiling build performance

If encountering build performance issues, you can use the rollup `--perf` option and the typescript compiler `--generateCpuProfile` option to get build profile data, like so:

```../emsdk/node/14.18.2_64bit/bin/npm run rollup --perf -- --perf --environment Configuration:Release,NativeBinDir:./rollup-test-data,ProductVersion:12.3.4```

```node node_modules/typescript/lib/tsc.js --generateCpuProfile dotnet-tsc.cpuprofile -p tsconfig.json ```

The .cpuprofile file generated by node can be opened in the Performance tab of Chrome or Edge's devtools.

## Blazor

You can test local build of runtime in blazor by overriding location where blazor resolves runtime pack from

*BlazorWasm.csproj*
```xml
<Target Name="UpdateRuntimePack" AfterTargets="ResolveFrameworkReferences">
  <ItemGroup>
    <ResolvedRuntimePack PackageDirectory="{RUNTIME_REPO_ROOT}\artifacts\bin\microsoft.netcore.app.runtime.browser-wasm\Debug" Condition="'%(ResolvedRuntimePack.FrameworkName)' == 'Microsoft.NETCore.App'" />
  </ItemGroup>
</Target>
```