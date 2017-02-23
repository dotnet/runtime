Cross Compilation for ARM on Linux
==================================

It is possible to build CoreFx on Linux for arm, armel, or arm64 by cross compiling.
It is very similar to the cross compilation procedure of CoreCLR.

Requirements
------------

You need a Debian based host, and the following packages need to be installed:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install qemu qemu-user-static binfmt-support debootstrap

In addition, to cross compile CoreFX, the binutils for the target are required. So for arm you need:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install binutils-arm-linux-gnueabihf

for armel:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install binutils-arm-linux-gnueabi

and for arm64 you need:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install binutils-aarch64-linux-gnu


Generating the rootfs
---------------------
The `cross\build-rootfs.sh` script can be used to download the files needed for cross compilation. It will generate an Ubuntu 14.04 rootfs as this is what CoreFX targets.

    Usage: ./cross/build-rootfs.sh [BuildArch] [UbuntuCodeName]
    BuildArch can be: arm, armel, arm64, x86
    UbuntuCodeName - optional, Code name for Ubuntu, can be: trusty(default), vivid, wily, xenial. If BuildArch is armel, jessie(default) or tizen.

The `build-rootfs.sh` script must be run as root, as it has to make some symlinks to the system. It will, by default, generate the rootfs in `cross\rootfs\<BuildArch>` however this can be changed by setting the `ROOTFS_DIR` environment variable.

For example, to generate an arm rootfs:

    lgs@ubuntu ~/git/corefx/ $ sudo ./cross/build-rootfs.sh arm

and if you wanted to generate the rootfs elsewhere:

    lgs@ubuntu ~/git/corefx/ $ sudo ROOTFS_DIR=/home/lgs/corefx-cross/arm ./build-rootfs.sh arm


Cross compiling for native CoreFX
---------------------------------
Once the rootfs has been generated, it will be possible to cross compile CoreFX. If `ROOTFS_DIR` was set when generating the rootfs, then it must also be set when running `build.sh`.

So, without `ROOTFS_DIR`:

    lgs@ubuntu ~/git/corefx/ $ ./build-native.sh -debug -buildArch=arm -- verbose cross

And with:

    lgs@ubuntu ~/git/corefx/ $ ROOTFS_DIR=/home/lgs/corefx-cross/arm ./build-native.sh -debug -buildArch=arm -- verbose cross

As usual the generated binaries will be found in `bin/BuildOS.BuildArch.BuildType/native` as following:

    lgs@ubuntu ~/git/corefx/ ls -al ./bin/Linux.arm.Debug/native
    total 988
    drwxrwxr-x 2 lgs lgs   4096  3  6 18:33 .
    drwxrwxr-x 3 lgs lgs   4096  3  6 18:33 ..
    -rw-r--r-- 1 lgs lgs  19797  3  6 18:33 System.IO.Compression.Native.so
    -rw-r--r-- 1 lgs lgs 428232  3  6 18:33 System.Native.a
    -rw-r--r-- 1 lgs lgs 228279  3  6 18:33 System.Native.so
    -rw-r--r-- 1 lgs lgs  53089  3  6 18:33 System.Net.Http.Native.so
    -rw-r--r-- 1 lgs lgs 266720  3  6 18:33 System.Security.Cryptography.Native.so
    lgs@ubuntu ~/git/corefx/ file ./bin/Linux.arm.Debug/native/System.Native.so
    ./bin/Linux.arm.Debug/native/System.Native.so:
    ELF 32-bit LSB  shared object, ARM, EABI5 version 1 (SYSV),
    dynamically linked, BuildID[sha1]=fac50f1bd657c1759f0ad6cf5951511ddf252e67, not stripped


Compiling for managed CoreFX
============================
The managed components of CoreFX are architecture-independent and thus do not require a special build for arm, armel or arm64.

Many of the managed binaries are also OS-independent, e.g. System.Linq.dll, while some are OS-specific, e.g. System.IO.FileSystem.dll, with different builds for Windows and Linux.

    lgs@ubuntu ~/git/corefx/ $ ./build-managed.sh -debug -verbose

The output is at `bin/[BuildConfiguration]` where `BuildConfiguration` looks something like `netcoreapp-<OSGroup>-Debug-<Architecture>`. Ex: `bin/netcoreapp-Linux-Debug-x64`. For more details on the build configurations see [project-guidelines](../coding-guidelines/project-guidelines.md)

Building corefx for Linux ARM Emulator
=======================================

It is possible to build corefx binaries (native and managed) for the Linux ARM Emulator (latest version provided here: [#3805](https://github.com/dotnet/coreclr/issues/3805)).
The `scripts/arm32_ci_script.sh` script does this.

The following instructions assume that:
* You have set up the extracted emulator at `/opt/linux-arm-emulator` (such that `/opt/linux-arm-emulator/platform/rootfs-t30.ext4` exists)
* The mount path for the emulator rootfs is `/opt/linux-arm-emulator-root` (change this path if you have a working directory at this path).

All the following instructions are for the Release mode. Change the commands and files accordingly for the Debug mode.

To just build the native and managed corefx binaries for the Linux ARM Emulator, run the following command:
```
prajwal@ubuntu ~/corefx $ ./scripts/arm32_ci_script.sh \
    --emulatorPath=/opt/linux-arm-emulator \
    --mountPath=/opt/linux-arm-emulator-root \
    --buildConfig=Release
```

The Linux ARM Emulator is based on the soft floating point and thus the native binaries are generated for the armel architecture. The corefx binaries generated by the above command can be found at `~/corefx/bin/Linux.armel.Release`, `~/corefx/bin/Linux.AnyCPU.Release`, `~/corefx/bin/Unix.AnyCPU.Release`, and `~/corefx/bin/AnyOS.AnyCPU.Release`.


Build corefx for a new architecture
===================================

When building for a new architecture you will need to build the native pieces separate from the managed pieces in order to correctly boot strap the native runtime. Instead of calling build.sh directly you should instead split the calls like such:

Example building for armel
```
build-native.sh -buildArch=armel
--> Output goes to bin/runtime/netcoreapp-Linux-Debug-armel

build-managed.sh -buildArch=x64
--> Output goes to bin/runtime/netcoreapp-Linux-Debug-x64
```

The reason you need to build the managed portion for x64 is because it depends on runtime packages for the new architecture which don't exist yet so we use another existing architecture such as x64 as a proxy for building the managed binaries.

Similar if you want to try and run tests you will have to copy the managed assemblies from the proxy directory (i.e. `netcoreapp-Linux-Debug-x64`) to the new architecture directory (i.e `netcoreapp-Linux-Debug-armel`) and run code via another host such as corerun because dotnet is at a higher level and most likely doesn't exist for the new architecture yet.

Once all the necessary builds are setup and packages are published the spliting of the build and manual creation of the runtime should no longer be necessary.