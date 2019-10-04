# Build Core-Setup with a Local CoreCLR or CoreFX

## Testing with private CoreCLR bits

Generally the Core-Setup build system gets the CoreCLR from a NuGet package which gets pulled down and correctly copied to the various output directories by building `src\pkg\projects\netcoreapp\src\netcoreapp.depproj` which gets built as part of `build.cmd/sh`. For folks that want to do builds and test runs in corefx with a local private build of coreclr you can follow these steps:

1. Build CoreCLR and note your output directory. Ex: `\coreclr\bin\Product\Windows_NT.x64.Release\` Note this will vary based on your OS/Architecture/Flavor and it is generally a good idea to use Release builds for CoreCLR when building Core-Setup and the OS and Architecture should match what you are building in Core-Setup.
2. Build Core-Setup either passing in the `CoreCLROverridePath` property or setting it as an environment variable:

```batch
build.cmd /p:CoreCLROverridePath=d:\git\coreclr\bin\Product\Windows_NT.x64.Release
```

By convention the project will look for PDBs in a directory under `$(CoreCLROverridePath)/PDB` and if found will also copy them. If not found no PDBs will be copied. If you want to explicitly set the PDB path then you can pass `CoreCLRPDBOverridePath` property to that PDB directory.
Once you have updated your CoreCLR you can run tests however you usually do and it should be using your copy of CoreCLR.
If you prefer, you can use a Debug build of System.Private.CoreLib, but if you do you must also use a Debug build of the native portions of the runtime, e.g. coreclr.dll. Tests with a Debug runtime will execute much more slowly than with Release runtime bits. This override is only enabled for building the NETCoreApp shared framework.

## Testing with private CoreFX bits

Similarly to how Core-Setup consumes CoreCLR, Core-Setup also consumes CoreFX from a NuGet package which gets pulled down and correctly copied to the various output directories by building `src\pkg\projects\netcoreapp\src\netcoreapp.depproj` which gets built as part of `build.cmd/sh`. To test with a local private build of CoreFX, you can follow these steps:

1. Build CoreFX and note your configuration (Ex. Debug, Checked, Release).
2. Build Core-Setup either passing in the `CoreFXOverridePath` property or setting it as an environment variable (assuming your CoreFX repo is located at `d:\git\corefx`):

```batch
build.cmd /p:CoreFXOverridePath=d:\git\corefx\artifacts\packages\Debug
```

The Core-Setup build will resolve the correct libraries to use to build the NETCoreApp shared framework based on the restored packages and will locate the files in the output directory based on the nuspec file of the shared-framework package in the local CoreFX build. This override does not enable using a local CoreFX build when creating the WindowsDesktop shared framework.
