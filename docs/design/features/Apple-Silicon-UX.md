# Apple Silicon User Experience

Apple has [announced plans to transition its Mac hardware line]( https://www.apple.com/newsroom/2020/06/apple-announces-mac-transition-to-apple-silicon/) to a new Arm64-based chip that they refer to as “Apple Silicon”.

The transition has a few impacts on the .NET user experience.

## Mixed Architecture Side By Side Installs

Support for a new processor architecture is a significant undertaking.  It is too large an undertaking to consider on our stable release branches.  New architecture development is occurring in .NET 6.

We do not plan to back-port Apple Silicon support to the current releases .NET 2.1, .NET 3.1, and .NET 5.0.

As part of the their transition plan, Apple created the Rosetta 2 x86_64 emulator to run x64 code on an Apple Silicon processor. This means that .NET 2.1 LTS, .NET 3.1 LTS and .NET 5 mostly just work on Apple Silicon.

However the `dotnet` command line architecture makes it difficult to support side-by-side mixed architecture installs. The dotnet tool is responsible for stating the `hostfxr` library to resolve the runtime version to use. Until the runtime version is selected it is not clear which architecture the runtime will require.

For instance, the `dotnet --info` command must resolve the currently selected SDK, by finding the `global.json` or the most recent SDK. Then it must find the best runtime as requested by that SDK. Once that is determined the runtime required architecture could be known.

### Other Observations

- This mixed architecture problem is transient in nature. Essentially it means it will only exist for the support lifetime of the previous releases (2.1, 3.1, 5.0).  We want to avoid making major architectural changes to support this temporary design problem.

- We want to keep the process parent-child relationships as consistent as possible.

- We want minimal changes to the earlier releases.

### Proposed Side-By-Side Design

For Apple Silicon the proposed plan is to:

- On Apple Silicon .NET runtime versions less than 6.0 will always require `x86_64` architecture. All other runtime versions will require `arm64` architecture on Apple Silicon.

- Make the .NET 6 `dotnet` & `libhostfxr.dylib` (`hostfxr`) universal binaries. This means both native and emulated processes can perform runtime resolution to identify the correct runtime to use.

- The `hostfxr` will be modified to enforce the architecture version  policy above. It will return an architecture mismatch error when the runtime (by version policy above) require a different architecture than the currently running process.

- `dotnet` will be modified to enable switching the architecture of the current process when it sees an architecture mismatch error returned by `hostfxr`. The current process will switch architecture as needed and run with the same arguments as the new process. The switching will use `posix_spawn` with changes to `posix_spawnattr_t` to set `POSIX_SPAWN_SETEXEC` and preferred architecture via `posix_spawnattr_setbinpref_np` (see Apple Silicon man page) and setting `sysctl` `kern.curproc_arch_affinity` property appropriately.

- The installer will be modified to enforce the .NET 6 architecture policy. It will prevent osx-x64 install on Apple Silicon

- The existing installer releases will need to be tested to make sure they do not clobber the new `dotnet` modifications.

#### Implications

- The proposed version policy will prohibit us from making a native port of earlier runtime releases. .NET 2.1 will be out of support before .NET 6.0 releases. .NET 5.0 will be out of support 3 month after release of .NET 6. .NET 3.1 is the only viable candidate for back porting Apple Silicon support.  However, given the size of the changes to the runtime and the JIT it is highly unlikely this is something we would actively pursue.

- The proposed version policy will prevent .NET 6 apps published with rid of osx-x64 from running on Apple Silicon even with Rosetta 2 emulation. This is desirable because long term we want to stop supporting Rosetta 2 emulation on Apple Silicon. IT support grows our effective platform support matrix and increases our support burden. We are dependent on Apple to fix Rosetta 2 issues, but they are dependent on us to root cause them. Performance is also expected to be better with native code.

#### Future optimizations

The current proposed design will perform the runtime search twice when an architecture mismatch is detected. The user can prevent this extra delay by calling dotnet with the correct architecture for the required runtime.

- The default architecture is selected based on the architecture of the current process (typically the shell or the IDE).
- The user or the IDE can explicitly select the architecture of the target process using the `arch` command.

The extra search cost could be minimized by passing the result of the previous search when restarting the process. Adding an `--fx-version` option would accomplish this at the cost of changing the users specified argument order.  Using a new environment variable would avoid this unexpected side-effect.

## Apple Silicon requires Signed Binaries

Apple Silicon with Big Sur adds a new security policy which requires signed binaries by default. Unsigned binaries are simply killed without explanation. The user can expect an inscrutable message like `zsh killed: helloWorld` This directly affects our users who expect published binaries to just work.

The developer would need to explicitly sign after publishing for Apple Silicon.  The sigining command is relatively simple `codesign -s <signature> <app-path>`.

### Proposed changes

This is largely TBD. The addition of a message during publish is the most likely design.

## Universal Binaries

As part of the architectural transition, Apple has switched the entire BigSur OS to use universal binaries. This means every file in the OS has both the `arm64` and the `x86_64` code on disk.

Given both architectures, universal binaries are relatively easy to create. The general syntax would be `lipo <binaryArchA> <binaryArchB> -create -output <binaryUniversal>`

### No planned universal binary .NET runtime

At least for .NET 6, general universal binary support is not planned.

General support would require:

- All native binaries and libraries were built as universal binaries.
- All managed libraries would contain IL which would work correctly on both platforms (no architecture specific `#if`)
- All managed libraries contain either no `crossgen` native code, or `crossgen` code for both architectures.
- The runtime would need to be taught how to select the correct architecture `crossgen` code.
- The managed runtime file format would need to be extended to support multiple `crossgen` architectures.

Given:

- Universal binaries would effectively double the size of the runtime.
- It requires significant investment

This did not make .NET 6 investment plan.

#### Shared dotnet host

The impact to the shared `dotnet` host is discussed at length above. `dotnet` will be a universal binary.

#### dotnet-sos and the SOS plugin

The `dotnet-sos` tool installs the `SOS` diagnostic plugin into `lldb`. Since `lldb` is a universal binary, it expects its plugins to also be universal binaries. The plan would be to make the Apple Silcon SOS be a universal binary to allow SOS to work on `x86_64` and `arm64` processes.

#### Apple Store requires universal binaries

Publishing a .NET runtime to the Apple store is not a common customer scenario. However it is worth noting that the Apple store is requiring universal binaries.

This in currently an unsupported scenario.

It may be possible for a developer to generate a universal binary .NET 6 app, if each architecture is published as a single file app. Each architecture could be merged into a universal binary. This may just work, but it may also invalidate assumptions made by the single file app.
