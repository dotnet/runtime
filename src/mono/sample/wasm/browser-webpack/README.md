## Sample for packaging dotnet.js via WebPack

```
dotnet build /p:TargetOS=browser /p:TargetArchitecture=wasm -c Debug /t:RunSample
```