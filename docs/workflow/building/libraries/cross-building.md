Cross Compilation for ARM on Linux
==================================

It is possible to build libraries on Linux for arm, armel, or arm64 by cross compiling. It is very similar to the cross compilation procedure of CoreCLR.

Requirements
------------

You need a Debian based host, and the following packages need to be installed:

    $ sudo apt-get install qemu qemu-user-static binfmt-support debootstrap

In addition, to cross compile libraries, the binutils for the target are required. So for arm you need:

    $ sudo apt-get install binutils-arm-linux-gnueabihf

for armel:

    $ sudo apt-get install binutils-arm-linux-gnueabi

and for arm64 you need:

    $ sudo apt-get install binutils-aarch64-linux-gnu


Generate the rootfs
---------------------
The `eng/common/cross/build-rootfs.sh` script can be used to download the files needed for cross compilation. It can generate rootfs for different operating systems.

    Usage: ./eng/common/cross/build-rootfs.sh [BuildArch] [CodeName] [lldbx.y] [llvmx[.y]] [--skipunmount] --rootfsdir <directory>]
    BuildArch can be: arm(default), arm64, armel, armv6, ppc64le, riscv64, s390x, x64, x86
    CodeName - optional, Code name for Linux, can be: xenial(default), zesty, bionic, alpine, alpine3.13 or alpine3.14. If BuildArch is armel, LinuxCodeName is jessie(default) or tizen.
                                   for FreeBSD can be: freebsd12, freebsd13
                                   for illumos can be: illumos
                                   for Haiku can be: haiku

The `build-rootfs.sh` script must be run as root, as it has to make some symlinks to the system. By default it generates the rootfs in `.tools/rootfs/<BuildArch>`, however this can be changed by setting the `ROOTFS_DIR` environment variable or by using `--rootfsdir`.

For example, to generate an arm Ubuntu 18.04 rootfs:

    $ sudo ./eng/common/cross/build-rootfs.sh arm bionic

And to generate the rootfs elsewhere:

    $ sudo ./build-rootfs.sh arm bionic --rootfsdir /mnt/rootfs/arm


Compile native part of libraries
---------------------------------

To build native part of libraries for arm using subset:

    $ ROOTFS_DIR=`pwd`/.tools/rootfs/arm ./build.sh --cross --arch arm --librariesConfiguration Release --subset libs.native

To build native part of libraries for arm with dedicated script (without msbuild):

    $ ROOTFS_DIR=`pwd`/.tools/rootfs/arm ./src/native/libs/build-native.sh -release -arm -cross -outconfig net7.0-Linux-Release-arm

Build artifacts can be found in `artifacts/bin/native/net7.0-<TargetOS>-<BuildArch>-<BuildType>/`:

    $ ls artifacts/bin/native/net7.0-Linux-Release-arm/*
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Globalization.Native.a
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Globalization.Native.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Globalization.Native.so.dbg
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.IO.Compression.Native.a
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.IO.Compression.Native.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.IO.Compression.Native.so.dbg
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.IO.Ports.Native.a
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.IO.Ports.Native.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.IO.Ports.Native.so.dbg
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Native.a
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Native.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Native.so.dbg
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Net.Security.Native.a
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Net.Security.Native.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Net.Security.Native.so.dbg
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Security.Cryptography.Native.OpenSsl.a
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Security.Cryptography.Native.OpenSsl.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Security.Cryptography.Native.OpenSsl.so.dbg

    $ file artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Native.so
    artifacts/bin/native/net7.0-Linux-Release-arm/libSystem.Native.so: ELF 32-bit LSB shared object, ARM, EABI5 version 1 (SYSV), dynamically linked, BuildID[sha1]=5f6f6f9c4012dffed133624867adf32ac2af130d, stripped


Compile managed part of libraries
============================
The managed components of libraries are architecture-independent and, thus, do not require a special build for arm, armel or arm64 (this is true if ILLinker trimming is disabled with `/p:ILLinkTrimAssembly=false`).

Many of the managed binaries are also OS-independent, e.g. System.Linq.dll, while some are OS-specific, e.g. System.IO.FileSystem.dll, with different builds for Windows and Linux.

Build of managed part of libraries requires presence of built native part of libraries.

To build managed part of libraries for arm using subset (architecture-dependent, can't be used on other architectures):

    $ ./build.sh --arch arm --librariesConfiguration Release --subset libs.sfx

Note that by default ILLinker trimming is enabled and libraries built above for arm can't be used on other arches. To build architecture-independent managed part of libraries for arm using subset:

    $ ./build.sh --arch arm --librariesConfiguration Release --subset libs.sfx /p:ILLinkTrimAssembly=false

Build artifacts can be found in `artifacts/bin/microsoft.netcore.app.runtime.<TargetOS>-<BuildArch>/<BuildType>/runtimes/<TargetOS>-<BuildArch>/lib/net7.0/`. For more details on the build configurations see [project-guidelines](/docs/coding-guidelines/project-guidelines.md).

Both native and managed parts can be built at the same time with:

    $ ROOTFS_DIR=`pwd`/.tools/rootfs/arm ./build.sh --cross --arch arm --librariesConfiguration Release --subset libs.native+libs.sfx

Build libraries for a new architecture
===================================

When building for a new architecture you will need to build the native pieces separately from the managed pieces in order to correctly boot strap the native runtime.

Native part build for target architecture:

    $ ROOTFS_DIR=`pwd`/.tools/rootfs/<BuildArch> ./src/native/libs/build-native.sh -release -<BuildArch> -cross -outconfig net7.0-Linux-Release-<BuildArch>

Architecture-independent managed part build for x64:

    $ ./build.sh --arch x64 --librariesConfiguration Release --subset libs.sfx /p:ILLinkTrimAssembly=false

The reason you need to build the managed portion for x64 is because it depends on runtime packages for the new architecture which don't exist yet so we use another existing architecture such as x64 as a proxy for building the managed binaries.

Similar if you want to try and run tests you will have to copy the managed assemblies from the proxy directory (i.e. `net7.0-Linux-Release-x64`) to the new architecture directory (i.e `net7.0-Linux-Release-<BuildArch>`) and run code via another host such as corerun because dotnet is at a higher level and most likely doesn't exist for the new architecture yet.

Once all the necessary builds are setup and packages are published the splitting of the build and manual creation of the runtime should no longer be necessary.
