# Experimental support of CoreCLR on iOS/tvOS

This is the internal documentation which outlines experimental support of CoreCLR on iOS/tvOS platforms.

## Table of Contents

- [Building CoreCLR for iOS/tvOS](#building-coreclr-for-iostvos)
  - [macOS](#macos)
    - [Prerequisites](#prerequisites)
    - [Building the runtime, libraries and tools](#building-the-runtime-libraries-and-tools)
- [Building and running a sample app](#building-and-running-a-sample-app)
  - [Building HelloiOS sample](#building-helloios-sample)
  - [Running HelloiOS sample on a simulator](#running-helloios-sample-on-a-simulator)
- [Building and running tests on a simulator](#building-and-running-tests-on-a-simulator)
- [Debugging the runtime and the sample app](#debugging-the-runtime-and-the-sample-app)
  - [Steps](#steps)
- [See also](#see-also)
- [Troubleshooting](#troubleshooting)

## Building CoreCLR for iOS/tvOS

Supported host systems for building CoreCLR for iOS/tvOS:
- [macOS](#macos) ✔

Supported target platforms:
- iOS Simulator ✔
- tvOS Simulator ❌ (not yet supported)
- Mac Catalyst ✔
- iOS Device ✔
- tvOS Device ❌ (not yet supported)

Supported target architectures:
- x64 ✔
- arm64 ✔

### macOS

#### Prerequisites

- macOS with Xcode installed
- iPhone SDK enabled in Xcode installation
- Build requirements are the same as for building native CoreCLR on macOS
- Xcode command line tools installed:
  ```bash
  xcode-select --install
  ```

> [!NOTE]
> Make sure you have accepted the Xcode license agreement:
> ```bash
> sudo xcodebuild -license accept
> ```

#### Building the runtime, libraries and tools

To build CoreCLR runtime, libraries and tools, run the following command from `<repo-root>`:

```bash
./build.sh clr+clr.runtime+libs+packs -os <ios|iossimulator|maccatalyst> -arch arm64 -cross -c <Debug|Checked>
```

> [!NOTE]
> The runtime packages will be located at: `<repo-root>/artifacts/packages/<configuration>/Shipping/`

## Building and running a sample app

To demonstrate building and running an iOS application with CoreCLR, we will use the [HelloiOS sample app](../../../../src/mono/sample/iOS/Program.csproj).

A prerequisite for building and running samples locally is to have CoreCLR successfully built for the desired iOS platform.

### Building HelloiOS sample

To build `HelloiOS`, run the following command from `<repo-root>`:

```bash
./dotnet.sh build src/mono/sample/iOS/Program.csproj -c <Debug|Checked> /p:TargetOS=<ios|iossimulator|maccatalyst> /p:TargetArchitecture=arm64 /p:UseMonoRuntime=false /p:RunAOTCompilation=false /p:MonoForceInterpreter=false
```

On successful execution, the command will output the iOS app bundle.

### Running HelloiOS sample on a simulator

To run the sample on a simulator, run the following command from `<repo-root>`:

```bash
./dotnet.sh publish src/mono/sample/iOS/Program.csproj -c <Debug|Checked> /p:TargetOS=<ios|iossimulator|maccatalyst> /p:TargetArchitecture=arm64 /p:DeployAndRun=true /p:UseMonoRuntime=false /p:RunAOTCompilation=false /p:MonoForceInterpreter=false
```

The command also produces an Xcode project that can be opened for debugging:

```bash
open ./src/mono/sample/iOS/bin/<ios|iossimulator|maccatalyst>-arm64/Bundle/HelloiOS/HelloiOS.xcodeproj
```

> [!NOTE]
> Make sure you have a simulator available. You can list available simulators with:
> ```bash
> xcrun simctl list devices
> ```
>
> If no simulators are available, create one using Xcode's Device Manager or the command line:
> ```bash
> xcrun simctl create "My iPhone" "iPhone 15" "iOS 17.0"
> ```

## Building and running tests on a simulator

To build the runtime tests for iOS with CoreCLR, run the following command from `<repo-root>`:

```bash
./src/tests/build.sh -os <iossimulator|tvossimulator> <x64|arm64> <Debug|Release> -p:UseMonoRuntime=false
```

> [!NOTE]
> Running the tests is not fully implemented yet. It will likely need similar app bundle infrastructure as NativeAOT/iOS uses.

## Debugging the runtime and the sample app

Native debugging is supported through Xcode. You can debug both the managed portion of the sample app and the native CoreCLR runtime.

### Steps

1. Build the runtime and `HelloiOS` sample app in `Debug` configuration.
2. Open the generated Xcode project:
   ```bash
   open ./src/mono/sample/iOS/bin/<target>/Bundle/HelloiOS/HelloiOS.xcodeproj
   ```
3. In Xcode, set breakpoints in the native CoreCLR code or managed code as needed.
4. Run the app in the iOS Simulator from Xcode to start debugging.
5. Use Xcode's debugging tools to inspect variables, call stacks, and step through code.

> [!NOTE]
> For debugging native CoreCLR code, you may need to build with debug symbols.

## See also

- [Building CoreCLR on macOS](../macos.md)
