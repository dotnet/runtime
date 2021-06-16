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
