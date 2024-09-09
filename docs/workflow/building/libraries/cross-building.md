Cross compile native runtime libraries on Linux
==================================

It is possible to build libraries on Linux for arm, armel, arm64 or other architectures by cross compiling. It is very similar to the cross compilation procedure of CoreCLR.

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

and similar ones for other architectures.

Generate the rootfs
---------------------
The `eng/common/cross/build-rootfs.sh` script can be used to download the files needed for cross compilation. It can generate rootfs for different operating systems and architectures, see `eng/common/cross/build-rootfs.sh --help` for more details.

The `build-rootfs.sh` script might need to be launched as root, as it has to make some symlinks to the system. By default it generates the rootfs in `.tools/rootfs/<BuildArch>`, however this can be changed by setting the `ROOTFS_DIR` environment variable or by using `--rootfsdir`.

For example, to generate an arm Ubuntu 18.04 rootfs:

    $ ./eng/common/cross/build-rootfs.sh arm bionic

And to generate the rootfs elsewhere:

    $ ./build-rootfs.sh arm bionic --rootfsdir /mnt/rootfs/arm


Compile native runtime libraries
---------------------------------

To build native runtime libraries for arm:

    $ ROOTFS_DIR=`pwd`/.tools/rootfs/arm ./build.sh libs.native --cross --arch arm --librariesConfiguration Release

Build artifacts can be found in `artifacts/bin/native/net9.0-<TargetOS>-<BuildArch>-<BuildType>/`:

    $ ls artifacts/bin/native/net9.0-Linux-Release-arm/*
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Globalization.Native.a
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Globalization.Native.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Globalization.Native.so.dbg
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.IO.Compression.Native.a
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.IO.Compression.Native.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.IO.Compression.Native.so.dbg
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.IO.Ports.Native.a
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.IO.Ports.Native.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.IO.Ports.Native.so.dbg
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Native.a
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Native.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Native.so.dbg
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Net.Security.Native.a
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Net.Security.Native.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Net.Security.Native.so.dbg
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Security.Cryptography.Native.OpenSsl.a
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Security.Cryptography.Native.OpenSsl.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Security.Cryptography.Native.OpenSsl.so.dbg

    $ file artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Native.so
    artifacts/bin/native/net9.0-Linux-Release-arm/libSystem.Native.so: ELF 32-bit LSB shared object, ARM, EABI5 version 1 (SYSV), dynamically linked, BuildID[sha1]=5f6f6f9c4012dffed133624867adf32ac2af130d, stripped


Compile managed runtime libraries on Linux
============================
The managed components of libraries are architecture-independent and, thus, do not require a special build for arm, armel, arm64 or other architectures (this is true if ILLinker trimming is disabled with `/p:ILLinkTrimAssembly=false`).

Many of the managed binaries are also OS-independent, e.g. System.Linq.dll, while some are OS-specific, e.g. System.IO.FileSystem.dll, with different builds for Windows and Linux.

Build of managed runtime libraries requires presence of built native runtime libraries.

To build managed runtime libraries for arm (architecture-dependent, can't be used on other architectures):

    $ ./build.sh libs.sfx --arch arm --librariesConfiguration Release

Note that by default ILLinker trimming is enabled and libraries built above for arm can't be used on other arches. To build architecture-independent managed runtime libraries for arm:

    $ ./build.sh libs.sfx --arch arm --librariesConfiguration Release /p:ILLinkTrimAssembly=false

Build artifacts can be found in `artifacts/bin/microsoft.netcore.app.runtime.<TargetOS>-<BuildArch>/<BuildType>/runtimes/<TargetOS>-<BuildArch>/lib/net9.0/`. For more details on the build configurations see [project-guidelines](/docs/coding-guidelines/project-guidelines.md).

Both native and managed runtime libraries can be built at the same time with:

    $ ROOTFS_DIR=`pwd`/.tools/rootfs/arm ./build.sh --cross --arch arm --librariesConfiguration Release --subset libs.native+libs.sfx
