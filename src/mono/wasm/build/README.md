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
  MainJS                            : $(WasmMainJSPath)
  InvariantGlobalization            : $(WasmInvariantGlobalization)
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
    <JsonToItemsTaskFactoryDir>$([MSBuild]::NormalizeDirectory('$(ArtifactsBinDir)', 'JsonToItemsTaskFactory', 'Debug', '$(NetCoreAppToolCurrent)'))</JsonToItemsTaskFactoryDir>
    <JsonToItemsTaskFactoryTasksAssemblyPath>$([MSBuild]::NormalizePath('$(JsonToItemsTaskFactoryDir)', 'JsonToItemsTaskFactory.dll'))</JsonToItemsTaskFactoryTasksAssemblyPath>
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