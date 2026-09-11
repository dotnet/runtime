# JSON manifest reader tasks

`MonoTargetsTasks.dll` contains two compiled MSBuild tasks that read JSON manifests into fixed sets of item outputs:

- `MonoRuntimeComponentManifestReadTask` reads runtime component definitions.
- `ReadWasmProps` reads browser and WASI runtime-pack settings.

Both tasks take a required `JsonFilePath` parameter. The JSON document has an optional top-level `properties` object and an optional `items` object. Each key in `items` names an output, whose value is an array containing either strings or objects:

```json
{
  "items": {
    "WasmOptConfigurationFlags": [
      "--enable-simd",
      {
        "identity": "--enable-threads",
        "source": "runtime-pack"
      }
    ]
  }
}
```

A string is used as the item identity. An object must contain a non-empty `Identity` value; its other string values become item metadata. Property names, `Identity`, and metadata names are matched case-insensitively. Item-group names are case-sensitive. Trailing commas are accepted, but comments are not.

Register the compiled task directly:

```xml
<UsingTask TaskName="ReadWasmProps" AssemblyFile="$(MonoTargetsTasksAssemblyPath)" />
```
