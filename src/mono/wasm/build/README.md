# `WasmApp.targets`

- Any project that wants to use this, can import the file, and set up the
various properties before the target `WasmBuildApp` gets executed.

- `WasmBuildApp` target is not run by default. The importing project will have
to do that.

- By default, the `WasmLoadAssembliesAndReferences` task is not run, and
the specified `@(WasmAssembliesToBundle)` are directly passed to
`WasmAppBuilder`.
	- If the project needs assembly dependencies to be resolved, then
	set `$(WasmResolveAssembliesBeforeBuild) == true`.

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

  MicrosoftNetCoreAppRuntimePackDir : $(MicrosoftNetCoreAppRuntimePackRidDir)
```

- `run-v8.sh` script is emitted to `$(WasmRunV8ScriptPath)` which defaults to `$(WasmAppDir)`.
    - To control it's generation use `$(WasmGenerateRunV8Script)` (false by default)

This should be a step towards eventually having this build as a sdk.
