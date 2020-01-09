# Build

To build just the libraries, use the `--subsetCategory` flag to the `build.sh` (or `build.cmd`) at the repo root:

```
./build.sh --subsetCategory libraries
```
or on Windows,
```
build.cmd --subsetCategory libraries
```

The libraries build has two logical components, the native build which produces the "shims" (which provide a stable interface between the OS and managed code) and the managed build which produces the MSIL code and NuGet packages that make up CoreFX. The commands above will build both.

The build configurations are generally defaulted based on where you are building (i.e. which OS or which architecture) but we have a few shortcuts for the individual properties that can be passed to the build scripts:

- `-framework|-f` identifies the target framework for the build. It defaults to `netcoreapp` but possible values include `netcoreapp` or `netfx`. (msbuild property `TargetGroup`)
- `-os` identifies the OS for the build. It defaults to the OS you are running on but possible values include `Windows_NT`, `Unix`, `Linux`, or `OSX`. (msbuild property `OSGroup`)
- `-configuration|-c Debug|Release` controls the optimization level the compilers use for the build. It defaults to `Debug`. (msbuild property `ConfigurationGroup`)
- `-arch` identifies the architecture for the build. It defaults to `x64` but possible values include `x64`, `x86`, `arm`, or `arm64`. (msbuild property `ArchGroup`)

For more details on the build configurations see [project-guidelines](../../../coding-guidelines/project-guidelines.md#build-pivots).

**Note**: In order to work on individual library projects or run library tests it is necessary to have built CoreCLR (aka, the "runtime"). To do this you must run `build.sh` (or `build.cmd`) from the root once before beginning that work.

It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch. If you invoke the build script without any actions, the default action chain `-restore -build` is executed.

You can chain multiple actions together (e.g., `-restore -build -buildtests`) and they will execute in the appropriate order. Note that if you specify actions like `-build` explicitly, you likely need to explicitly add `-restore` as well.

The most common workflow for developers is to first build once from the root, then iterate on an individual library and its tests.

By default build only builds the product libraries and none of the tests. If you want to build the tests you can add the flag `-buildtests`. If you want to run the tests you can add the flag `-test`. To build and run the tests combine both arguments: `-buildtests -test`. To specify just the libraries, use `-subcategory libraries`.

**Examples**
- Building in release mode for platform x64 (restore and build are implicit here as no actions are passed in)
```
./build.sh -subsetCategory libraries -c Release -arch x64
```

- Building the src assemblies and build and run tests (running all tests takes a considerable amount of time!)
```
./build.sh -subsetCategory libraries -restore -build -buildtests -test
```

- Building for different target frameworks (restore and build are implicit again as no action is passed in)
```
./build.sh -subsetCategory libraries  -framework netcoreapp
./build.sh -subsetCategory libraries  -framework netfx
```

- Build only managed components and skip the native build
```
./build.sh -subsetCategory libraries /p:BuildNative=false
```

- Clean the entire solution
```
./build.sh -subsetCategory libraries -clean
```

For Windows, replace `./build.sh` with `build.cmd`.

### Build Native

The libraries build contains some native code. This includes shims over libc, openssl, gssapi, and zlib. The build system uses CMake to generate Makefiles using clang. The build also uses git for generating some version information.

**Examples**

- Building in debug mode for platform x64
```
./src/libraries/Native/build-native.sh debug x64
```

- The following example shows how you would do an arm cross-compile build.
```
./src/libraries/Native/build-native.sh debug arm cross verbose
```

For Windows, replace `build-native.sh` with `build-native.cmd`.

### Building individual libraries

**Note**: Recall that in order to work on individual library projects or run library tests it is necessary to have built CoreCLR (aka, the "runtime"). To do this you must run `build.sh` (or `build.cmd`) from the root once before beginning that work.

Similar to building the entire repo with `build.cmd` or `build.sh` in the root you can build projects based on our directory structure by passing in the directory. We also support shortcuts for libraries so you can omit the root src folder from the path. When given a directory we will build all projects that we find recursively under that directory. Some examples may help here.

**Examples**

- Build all projects for a given library (e.g.: System.Collections) including running the tests
```
./build.sh -subsetCategory libraries System.Collections
```
or
```
 ./build.sh -subsetCategory libraries src\libraries\System.Collections
```
or
```
cd src/libraries/System.Collections
../../build.sh .
```

- Build just the tests for a library project.
```
 ./build.sh -subsetCategory libraries src\libraries\System.Collections\tests
```

- All the options listed above like framework and configuration are also supported (note they must be after the directory)
```
 ./build.sh -subsetCategory libraries System.Collections -f netfx -c Release
```

As an alternative, once you are iterating on specific libraries, you can either use `dotnet msbuild` (not `dotnet build` -- that will run a restore as well) or `msbuild`, depending on which is in your path. As `dotnet msbuild` works on both Unix and Windows we will use it throughout this guide.

Under the src directory is a set of directories, each of which represents a particular assembly in CoreFX. See Library Project Guidelines section under [project-guidelines](../../../coding-guidelines/project-guidelines.md) for more details about the structure.

For example the src\libraries\System.Diagnostics.DiagnosticSource directory holds the source code for the System.Diagnostics.DiagnosticSource.dll assembly.

You can build the DLL for System.Diagnostics.DiagnosticSource.dll by going to the `src\libraries\System.Diagnostics.DiagnosticsSource\src` directory and typing `dotnet msbuild`. The DLL ends up in `artifacts\bin\AnyOS.AnyCPU.Debug\System.Diagnostics.DiagnosticSource` as well as `artifacts\bin\runtime\[BuildConfiguration]`.

You can build the tests for System.Diagnostics.DiagnosticSource.dll by going to
`src\libraries\System.Diagnostics.DiagnosticSource\tests` and typing `dotnet msbuild`.

Some libraries might also have a ref and/or a pkg directory and you can build them in a similar way by typing `dotnet msbuild` in that directory.

For libraries that have multiple build configurations the configurations will be listed in the `<BuildConfigurations>` property group, commonly found in a configurations.props file next to the csproj. When building the csproj for a configuration the most compatible one in the list will be chosen and set for the build. For more information about `BuildConfigurations` see [project-guidelines](../../../coding-guidelines/project-guidelines.md).

**Examples**

- Build project for Linux for netcoreapp
```
dotnet msbuild System.Net.NetworkInformation.csproj /p:OSGroup=Linux
```

- Build release version of library
```
dotnet msbuild System.Net.NetworkInformation.csproj /p:ConfigurationGroup=Release
```

To build for all supported configurations you can use the `BuildAll` and `RebuildAll` tasks:

```
dotnet msbuild System.Net.NetworkInformation.csproj /t:RebuildAll
```

### Building all for other OSes

By default, building from the root will only build the libraries for the OS you are running on. One can
build for another OS by specifying `./build.sh -subsetCategory libraries -os [value]`.

Note that you cannot generally build native components for another OS but you can for managed components so if you need to do that you can do it at the individual project level or build all via passing `/p:BuildNative=false`.

### Building in Release or Debug

By default, building from the root or within a project will build the libraries in Debug mode.
One can build in Debug or Release mode from the root by doing `./build.sh -subsetCategory libraries  -c Release` or `./build.sh -subsetCategory libraries -c Debug` or when building a project by specifying `/p:ConfigurationGroup=[Debug|Release]` after the `dotnet msbuild` command.

### Building other Architectures

One can build 32- or 64-bit binaries or for any architecture by specifying in the root `./build.sh -subsetCategory libraries -arch [value]` or in a project `/p:ArchGroup=[value]` after the `dotnet msbuild` command.

## Working in Visual Studio

If you are working on Windows, and use Visual Studio, you can open individual libraries projects into it. From within Visual Studio you can then build, debug, and run tests.

// TODO - add more information here: or likely, in another document. We also need documentation on Visual Studio Code.