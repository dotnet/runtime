# Build

The libraries build has two logical components, the native build which produces the "shims" (which provide a stable interface between the OS and managed code) and
the managed build which produces the MSIL code and NuGet packages that make up CoreFX.

Calling the script `libraries` attempts to build both the native and managed code.

The build configurations are generally defaulted based on where you are building (i.e. which OS or which architecture) but we have a few shortcuts for the individual properties that can be passed to the build scripts:

- `-framework|-f` identifies the target framework for the build. It defaults to `netcoreapp` but possible values include `netcoreapp` or `netfx`. (msbuild property `TargetGroup`)
- `-os` identifies the OS for the build. It defaults to the OS you are running on but possible values include `Windows_NT`, `Unix`, `Linux`, or `OSX`. (msbuild property `OSGroup`)
- `-configuration|-c Debug|Release` controls the optimization level the compilers use for the build. It defaults to `Debug`. (msbuild property `ConfigurationGroup`)
- `-arch` identifies the architecture for the build. It defaults to `x64` but possible values include `x64`, `x86`, `arm`, or `arm64`. (msbuild property `ArchGroup`)

For more details on the build configurations see [project-guidelines](../../../coding-guidelines/project-guidelines.md#build-pivots).

**Note**: Before working on individual projects or test projects you **must** run `build` from the root once before beginning that work. It is also a good idea to run `build` whenever you pull a large set of unknown changes into your branch. If you invoke the build script without any actions, the default action chain `-restore -build` is executed. This means that restore and build are not implicit when invoking other actions!

**Note:** You can chain multiple actions together but the order of execution is fixed and does not relate to the position of the argument in the command.

The most common workflow for developers is to call `libraries` from the root once and then go and work on the individual library that you are trying to make changes for.

By default build only builds the product libraries and none of the tests. If you want to build the tests you can call `libraries -buildtests`. If you want to run the tests you can call `build -test`. To build and run the tests combine both arguments: `libraries -buildtests -test`. To build both the product libraries and the test libraries pass `libraries -build -buildtests` to the command line.

If you invoke the build script without any argument the default arguments will be executed `-restore -build`. Note that -restore and -build are only implicit if no actions are passed in.

**Examples**
- Building in release mode for platform x64 (restore and build are implicit here as no actions are passed in)
```
libraries -c Release -arch x64
```

- Building the src assemblies and build and run tests (running all tests takes a considerable amount of time!)
```
libraries -restore -build -buildtests -test
```

- Building for different target frameworks (restore and build are implicit again as no action is passed in)
```
libraries -framework netcoreapp
libraries -framework netfx
```

- Build only managed components and skip the native build
```
libraries /p:BuildNative=false
```

- Clean the entire solution
```
libraries -clean
```
### Build Native
The native build produces shims over libc, openssl, gssapi, and libz.
The build system uses CMake to generate Makefiles using clang.
The build also uses git for generating some version information.

The native component should be buildable on any system.

**Examples**

- Building in debug mode for platform x64
```
./src/libraries/Native/build-native debug x64
```

- The following example shows how you would do an arm cross-compile build.
```
./src/libraries/Native/build-native debug arm cross verbose
```

For more information about extra parameters take a look at the scripts `build-native` under src/Native.

### Building individual libraries

**Note**: Before working on individual projects or test projects you **must** run `libraries` from the root once before beginning that work. It is also a good idea to run `libraries` whenever you pull a large set of unknown changes into your branch.

Similar to building the entire repo with build.cmd/sh in the root you can build projects based on our directory structure by passing in the directory. We also support
shortcuts for libraries so you can omit the root src folder from the path. When given a directory we will build all projects that we find recursively under that directory.

**Examples**

- Build all projects for a given library (ex: System.Collections) including running the tests
```
libraries System.Collections
```
or
```
libraries src\libraries\System.Collections
```
or
```
cd src\libraries\System.Collections
..\..\libraries .
```

- Build just the tests for a library project.
```
libraries src\libraries\System.Collections\tests
```

- All the options listed above like framework and configuration are also supported (note they must be after the directory)
```
libraries System.Collections -f netfx -c Release
```

### Building individual projects

You can either use `dotnet msbuild` or `msbuild`, depending on which is in your path. As `dotnet msbuild` works on all supported environments (i.e. Unix) we will use it throughout this guide.

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
build for another OS by specifying `libraries -os [value]`.

Note that you cannot generally build native components for another OS but you can for managed components so if you need to do that you can do it at the individual project level or build all via passing `/p:BuildNative=false`.

### Building in Release or Debug

By default, building from the root or within a project will build the libraries in Debug mode.
One can build in Debug or Release mode from the root by doing `libraries -c Release` or `libraries -c Debug` or when building a project by specifying `/p:ConfigurationGroup=[Debug|Release]` after the `dotnet msbuild` command.

### Building other Architectures

One can build 32- or 64-bit binaries or for any architecture by specifying in the root `libraries -arch [value]` or in a project `/p:ArchGroup=[value]` after the `dotnet msbuild` command.
