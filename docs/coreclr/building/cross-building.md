Cross Compilation for ARM on Linux
==================================

Through cross compilation, on Linux it is possible to build CoreCLR for arm or arm64.

Requirements
------------

You need a Debian based host and the following packages needs to be installed:

    ben@ubuntu ~/git/coreclr/ $ sudo apt-get install qemu qemu-user-static binfmt-support debootstrap

In addition, to cross compile CoreCLR the binutils for the target are required. So for arm you need:

    ben@ubuntu ~/git/coreclr/ $ sudo apt-get install binutils-arm-linux-gnueabihf

and conversely for arm64:

    ben@ubuntu ~/git/coreclr/ $ sudo apt-get install binutils-aarch64-linux-gnu


Generating the rootfs
---------------------
The `cross\build-rootfs.sh` script can be used to download the files needed for cross compilation. It will generate an Ubuntu 14.04 rootfs as this is what CoreCLR targets.

    Usage: build-rootfs.sh [BuildArch]
    BuildArch can be: arm, arm64

The `build-rootfs.sh` script must be run as root as it has to make some symlinks to the system, it will by default generate the rootfs in `cross\rootfs\<BuildArch>` however this can be changed by setting the `ROOTFS_DIR` environment variable.

For example, to generate an arm rootfs:

    ben@ubuntu ~/git/coreclr/ $ sudo ./cross/build-rootfs.sh arm

and if you wanted to generate the rootfs elsewhere:

    ben@ubuntu ~/git/coreclr/ $ sudo ROOTFS_DIR=/home/ben/coreclr-cross/arm ./build-rootfs.sh arm


Cross compiling CoreCLR
-----------------------
Once the rootfs has been generated, it will be possible to cross compile CoreCLR. If `ROOTFS_DIR` was set when generating the rootfs, then it must also be set when running `build.sh`.

So, without `ROOTFS_DIR`:

    ben@ubuntu ~/git/coreclr/ $ ./build.sh arm debug verbose clean cross

And with:

    ben@ubuntu ~/git/coreclr/ $ ROOTFS_DIR=/home/ben/coreclr-cross/arm ./build.sh arm debug verbose clean cross

As usual the resulting binaries will be found in `bin/Product/BuildOS.BuildArch.BuildType/`


Compiling mscorlib for ARM Linux
================================

It is also possible to use a Windows and a Linux machine to build the managed components of CoreCLR for ARM Linux.  This can be useful when the build on the target platform fails, for example due to Mono issues.

Build mscorlib on Windows
-------------------------
The following instructions assume you are on a Windows machine with a clone of the CoreCLR repo that has a correctly configured [environment](https://github.com/dotnet/coreclr/wiki/Windows-instructions#environment).

To build mscorlib for Linux, run the following command:

```
D:\git\coreclr> build.cmd linuxmscorlib arm
```

The arguments `freebsdmscorlib` and `osxmscorlib` can be used instead to build mscorlib for FreeBSD or OS X.

The output is at bin\Product\<BuildOS>.arm.Debug\mscorlib.dll.


Build mscorlib on Ubuntu
-------------------------
The following instructions assume you are on a Linux machine such as Ubuntu 14.04 x86 64bit. 

To build mscorlib for Linux, run the following command:

```
    lgs@ubuntu ~/git/coreclr/ $ build.sh arm debug clean verbose   
```

The output is at bin/Product/<BuildOS>.arm.Debug/mscorlib.dll.

```
    lgs@ubuntu ~/git/coreclr/ $ file ./bin/Product/Linux.arm.Debug/mscorlib.dll 
    ./bin/Product/Linux.arm.Debug/mscorlib.dll: PE32 executable (DLL) 
    (console) ARMv7 Thumb Mono/.Net assembly, for MS Windows
```


