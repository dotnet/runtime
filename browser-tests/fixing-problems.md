## Rebuild After Changes

```bash
# Rebuild just libraries after test attribute changes
./build.sh -os browser -subset clr+libs+host -c Debug
```

## WASM calli missing for key

when you see error like `WASM calli missing for key: fii`

1) Edit `src/tasks/WasmAppBuilder/coreclr/ManagedToNativeGenerator.cs` and add the value into `missingCookies` collection. Keep the collection sorted.
2) Delete folder `runtime/artifacts/bin/<TestSuiteName>/Debug/net11.0-browser/browser-wasm/wwwroot/_framework/` recursively
3) run the generator with TestSuite binary assets
`.\dotnet.cmd build -bl /t:RunGenerator /p:RuntimeFlavor=CoreCLR /p:GeneratorOutputPath="runtime/src/coreclr/vm/wasm/" /p:AssembliesScanPath="runtime/artifacts/bin/<TestSuiteName>/Debug/net11.0-browser/browser-wasm/" src/tasks/WasmAppBuilder/WasmAppBuilder.csproj`
4) rebuild the runtime, see above
