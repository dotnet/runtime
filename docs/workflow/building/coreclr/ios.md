# Cross Compilation for iOS Simulator on macOS

## Requirements

Build requirements are the same as for building native CoreCLR on macOS. iPhone SDK has to be enabled in Xcode installation.

## Cross compiling CoreCLR

Build the runtime pack and tools with

```
./build.sh clr+clr.runtime+libs+packs -os [iossimulator/maccatalyst] -arch [x64/arm64] -cross -c Release
```

## Running the sample iOS app

Build and run the sample app with

```
./dotnet.sh publish src/mono/sample/iOS/Program.csproj -c Release /p:TargetOS=iossimulator /p:TargetArchitecture=arm64 /p:DeployAndRun=true /p:UseMonoRuntime=false /p:RunAOTCompilation=false /p:MonoForceInterpreter=false
```

The command also produces an Xcode project that can be opened with `open ./src/mono/sample/iOS/bin/iossimulator-arm64/Bundle/HelloiOS/HelloiOS.xcodeproj` and debugged in Xcode.

## Running the runtime tests

Build the runtime tests with

```
./src/tests/build.sh -os iossimulator arm64 Release -p:UseMonoRuntime=false
```

Running the tests is not implemented yet. It will likely need similar app bundle infrastructure as NativeAOT/iOS uses.