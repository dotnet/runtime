# Running ARM32 tests on modern hardware

One of our supported targets is 32-bit ARM. It can be quite challenging to construct a realistic ARM32 environment where you can build or run tests in a reasonable amount of time. Thankfully, it's possible to configure an ARM64 linux environment so that you can cross-build from ARM64 to ARM32, and run tests there using native hardware support instead of software emulation. This is not possible on ARM64-based Windows (this functionality is not offered by the OS).

## Configuring your ARM64 environment to run ARM32 binaries

By default your ARM64 Linux install probably doesn't have support for ARM32 binaries enabled, which will cause running the binaries to fail with a cryptic error. So you'll need to add the architecture to dpkg and install some core userspace libraries that CoreCLR will need to actually run your tests, i.e.:

```bash
$ sudo dpkg --add-architecture armhf

$ sudo apt-get update
Reading package lists... Done

$ sudo apt-get install libc6:armhf libstdc++6:armhf libicu74:armhf
The following additional packages will be installed:
```

Note that when installing a package for another architecture, you need to suffix the package name with the architecture name. For me, the three packages above were sufficient to run an ARM32 JIT test.

## Cross-building for ARM32 on ARM64

Follow the steps from https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/cross-building.md#linux-cross-building as-is. You should end up with a `linux.arm` `Core_Root` and test artifacts.

## Running an ARM32 test in your ARM64 environment

We're finally ready to go, probably. Export an environment variable to point to your ARM32 core root, and then run your test, i.e.:

```bash
$ export CORE_ROOT=/home/kg/runtime/artifacts/tests/coreclr/linux.arm.Release/Tests/Core_Root/

$ bash artifacts/tests/coreclr/linux.arm.Release/JIT/Directed/StructABI/StructABI/StructABI.sh
BEGIN EXECUTION
/home/kg/runtime/artifacts/tests/coreclr/linux.arm.Release/Tests/Core_Root//corerun -p System.Reflection.Metadata.MetadataUpdater.IsSupported=false -p System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization=true StructABI.dll ''
Issue80393_HFA failed. Retval: f1=1 f3=3
```
