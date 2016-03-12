Cross Compilation for ARM on Linux
==================================

Through cross compilation, on Linux it is possible to build CoreFX for arm or arm64.
It is very similar to the cross compilation procedure of CoreCLR. 

Requirements
------------

You need a Debian based host, and the following packages need to be installed:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install qemu qemu-user-static binfmt-support debootstrap

In addition, to cross compile CoreFX, the binutils for the target are required. So for arm you need:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install binutils-arm-linux-gnueabihf

and for arm64 you need:

    lgs@ubuntu ~/git/corefx/ $ sudo apt-get install binutils-aarch64-linux-gnu


Generating the rootfs
---------------------
The `cross\build-rootfs.sh` script can be used to download the files needed for cross compilation. It will generate an Ubuntu 14.04 rootfs as this is what CoreFX targets.

    Usage: build-rootfs.sh [BuildArch]
    BuildArch can be: arm, arm64

The `build-rootfs.sh` script must be run as root, as it has to make some symlinks to the system. It will, by default, generate the rootfs in `cross\rootfs\<BuildArch>` however this can be changed by setting the `ROOTFS_DIR` environment variable.

For example, to generate an arm rootfs:

    lgs@ubuntu ~/git/corefx/ $ sudo ./cross/build-rootfs.sh arm

and if you wanted to generate the rootfs elsewhere:

    lgs@ubuntu ~/git/corefx/ $ sudo ROOTFS_DIR=/home/lgs/corefx-cross/arm ./build-rootfs.sh arm


Cross compiling for native CoreFX
---------------------------------
Once the rootfs has been generated, it will be possible to cross compile CoreFX. If `ROOTFS_DIR` was set when generating the rootfs, then it must also be set when running `build.sh`.

So, without `ROOTFS_DIR`:

    lgs@ubuntu ~/git/corefx/ $ ./build.sh native arm debug verbose clean cross

And with:

    lgs@ubuntu ~/git/corefx/ $ ROOTFS_DIR=/home/lgs/corefx-cross/arm ./build.sh native arm debug verbose clean cross

As usual the generated binaries will be found in `bin/BuildOS.BuildArch.BuildType/Native` as following:

    lgs@ubuntu ~/git/corefx/ ls -al ./bin/Linux.arm.Debug/Native
    total 988
    drwxrwxr-x 2 lgs lgs   4096  3  6 18:33 .
    drwxrwxr-x 3 lgs lgs   4096  3  6 18:33 ..
    -rw-r--r-- 1 lgs lgs  19797  3  6 18:33 System.IO.Compression.Native.so
    -rw-r--r-- 1 lgs lgs 428232  3  6 18:33 System.Native.a
    -rw-r--r-- 1 lgs lgs 228279  3  6 18:33 System.Native.so
    -rw-r--r-- 1 lgs lgs  53089  3  6 18:33 System.Net.Http.Native.so
    -rw-r--r-- 1 lgs lgs 266720  3  6 18:33 System.Security.Cryptography.Native.so
    lgs@ubuntu ~/git/corefx/ file ./bin/Linux.arm.Debug/Native/System.Native.so 
    ./bin/Linux.arm.Debug/Native/System.Native.so: 
    ELF 32-bit LSB  shared object, ARM, EABI5 version 1 (SYSV), 
    dynamically linked, BuildID[sha1]=fac50f1bd657c1759f0ad6cf5951511ddf252e67, not stripped


Compiling for managed CoreFX
============================
The managed components of CoreFX are architecture-independent and thus do not require a special build for arm or arm64.

Many of the managed binaries are also OS-independent, e.g. System.Linq.dll, while some are OS-specific, e.g. System.IO.FileSystem.dll, with different builds for Windows and Linux.

    lgs@ubuntu ~/git/corefx/ $ ./build.sh managed debug clean verbose 

The output is at bin/<BuildOS>.AnyCPU.Debug.

    lgs@ubuntu ~/git/corefx/ $ ls -al ./bin
    drwxrwxr-x  17 lgs lgs  4096  3  6 21:00 .
    drwxrwxr-x  10 lgs lgs  4096  3 12 15:04 ..
    drwx------  98 lgs lgs  4096  3  6 21:36 AnyOS.AnyCPU.Debug
    drwx------  98 lgs lgs  4096  3  6 19:40 AnyOS.AnyCPU.Release
    drwx------ 284 lgs lgs 20480  3  6 22:30 Linux.AnyCPU.Debug
    drwx------ 284 lgs lgs 20480  3  6 20:45 Linux.AnyCPU.Release
    drwx------  33 lgs lgs  4096  3  6 21:33 Windows_NT.AnyCPU.Debug
    drwx------  33 lgs lgs  4096  3  6 19:36 Windows_NT.AnyCPU.Release


