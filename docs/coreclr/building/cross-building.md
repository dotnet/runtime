Cross Compilation for ARM
=========================

Through cross compilation, on Linux it is possible to build CoreCLR for ARM or ARM64.

Generating the rootfs
---------------------
The `cross\build-rootfs.sh` script can be used to download the files needed for cross compilation. It will generate an Ubuntu 14.04 rootfs as this is what CoreCLR targets.

    Usage: build-rootfs.sh [BuildArch]
    BuildArch can be: arm, arm64

This script requires a Debian based host, and the following packages installed

    qemu
    qemu-user-static
    binfmt-support
    debootstrap

The `build-rootfs.sh` script must be run as root as it has to make some symlinks to the system, it will by default generate the rootfs in `cross\rootfs\<BuildArch>` however this can be changed by setting the `ROOTFS_DIR` environment variable.

For example, to generate an ARM rootfs:

    ben@ubuntu ~/git/coreclr/ $ sudo ./cross/build-rootfs.sh arm

and if you wanted to generate the rootfs elsewhere:

    ben@ubuntu ~/git/coreclr/ $ sudo ROOTFS_DIR=/home/ben/coreclr-cross/arm ./build-rootfs.sh arm

Cross compiling CoreCLR
-----------------------
Once the rootfs has been generated it will be possible to cross compile CoreCLR. To cross compile CoreCLR the binutils for the target are required, for ARM this is `binutils-arm-linux-gnueabihf` and for ARM64 this is `binutils-aarch64-linux-gnu`. If `ROOTFS_DIR` was set when generating the rootfs, then it must also be set when running `build.sh`.

So, without `ROOTFS_DIR`:

    ben@ubuntu ~/git/coreclr/ $ ./build.sh arm debug verbose clean cross

And with:

    ben@ubuntu ~/git/coreclr/ $ ROOTFS_DIR=/home/ben/coreclr-cross/arm ./build.sh arm debug verbose clean cross

As usual the resulting binaries will be found in `bin/Product/BuildOS.BuildArch.BuildType/`

Cross Compilation for Linux, FreeBSD, or OS X
=============================================

It is also possible to use a Windows machine to build the managed components of CoreCLR or CoreFX for Linux or OS X.  This can be useful when the build on the target platform fails, for example due to Mono issues.

Build mscorlib on Windows
-------------------------
The following instructions assume you are on a Windows machine with a clone of the CoreCLR repo that has a correctly configured [environment](https://github.com/dotnet/coreclr/wiki/Windows-instructions#environment).

To build mscorlib for Linux, run the following command:

```
D:\git\coreclr> build.cmd linuxmscorlib
```

The arguments `freebsdmscorlib` and `osxmscorlib` can be used instead to build mscorlib for FreeBSD or OS X.

The output is at bin\Product\<BuildOS>.x64.Debug\mscorlib.dll.

The CoreCLR native components need to be built on the target platform.  This can be done with the following command:

```
ellismg@linux:~/git/coreclr$ ./build.sh skipmscorlib
```

Build the Framework Managed Components on Windows
-------------------------------------------------
The following instructions assume you are on a Windows machine with a clone of the CoreFX repo.

To build the CoreFX managed components for Linux, run the following command:

```
D:\git\corefx> build.cmd /p:OSGroup=Linux /p:SkipTests=true
```

`FreeBSD` and `OSX` can be used instead for the `OSGroup`.

The output is at bin\<BuildOS>.AnyCPU.Debug.

The CoreFX native components need to be built on the target platform.  This can be done with the following command:

```
ellismg@linux:~/git/corefx$ ./build.sh native
```
