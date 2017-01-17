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
The `cross\build-rootfs.sh` script can be used to download the files needed for cross compilation. It will generate an rootfs as this is what CoreCLR targets.

    Usage: ./cross/build-rootfs.sh [BuildArch] [LinuxCodeName] [lldbx.y] [--skipunmount]
    BuildArch can be: arm(default), armel, arm64, x86
    LinuxCodeName - optional, Code name for Linux, can be: trusty(default), vivid, wily, xenial. If BuildArch is armel, LinuxCodeName is jessie(default) or tizen.
    lldbx.y - optional, LLDB version, can be: lldb3.6(default), lldb3.8

The `build-rootfs.sh` script must be run as root as it has to make some symlinks to the system, it will by default generate the rootfs in `cross\rootfs\<BuildArch>` however this can be changed by setting the `ROOTFS_DIR` environment variable.

For example, to generate an arm rootfs:

    ben@ubuntu ~/git/coreclr/ $ sudo ./cross/build-rootfs.sh arm

You can choose Linux code name to match your target, give `vivid` for `Ubuntu 15.04`, `wily` for `Ubuntu 15.10`. Default is `trusty`, version `Ubuntu 14.04`.

    ben@ubuntu ~/git/coreclr/ $ sudo ./cross/build-rootfs.sh arm wily

and if you wanted to generate the rootfs elsewhere:

    ben@ubuntu ~/git/coreclr/ $ sudo ROOTFS_DIR=/home/ben/coreclr-cross/arm ./cross/build-rootfs.sh arm

For example, to generate an armel rootfs:

    hqu@ubuntu ~/git/coreclr/ $ sudo ./cross/build-rootfs.sh armel

You can choose code name to match your target, give `jessie` for `Debian`, `tizen` for `Tizen`. Default is `jessie`.

    hque@ubuntu ~/git/coreclr/ $ sudo ./cross/build-rootfs.sh armel tizen

and if you wanted to generate the rootfs elsewhere:

    hque@ubuntu ~/git/coreclr/ $ sudo ROOTFS_DIR=/home/ben/coreclr-cross/armel ./cross/build-rootfs.sh armel tizen


Cross compiling CoreCLR
-----------------------
Once the rootfs has been generated, it will be possible to cross compile CoreCLR. If `ROOTFS_DIR` was set when generating the rootfs, then it must also be set when running `build.sh`.

So, without `ROOTFS_DIR`:

    ben@ubuntu ~/git/coreclr/ $ ./build.sh arm debug verbose cross -rebuild

And with:

    ben@ubuntu ~/git/coreclr/ $ ROOTFS_DIR=/home/ben/coreclr-cross/arm ./build.sh arm debug verbose cross -rebuild

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
    lgs@ubuntu ~/git/coreclr/ $ build.sh arm debug verbose -rebuild
```

The output is at bin/Product/<BuildOS>.arm.Debug/mscorlib.dll.

```
    lgs@ubuntu ~/git/coreclr/ $ file ./bin/Product/Linux.arm.Debug/mscorlib.dll 
    ./bin/Product/Linux.arm.Debug/mscorlib.dll: PE32 executable (DLL) 
    (console) ARMv7 Thumb Mono/.Net assembly, for MS Windows
```

Building coreclr for Linux ARM Emulator
=======================================

It is possible to build coreclr binaries (native and mscorlib.dll) and run coreclr unit tests on the Linux ARM Emulator (latest version provided here: [#3805](https://github.com/dotnet/coreclr/issues/3805)).
The `tests/scripts/arm32_ci_script.sh` script does this.

The following instructions assume that:
* You have set up the extracted emulator at `/opt/linux-arm-emulator` (such that `/opt/linux-arm-emulator/platform/rootfs-t30.ext4` exists)  
The emulator rootfs is of 4GB size by default. But to enable testing of coreclr binaries on the emulator, you need to resize the rootfs (to atleast 7GB) using the instructions given in the `doc/RESIZE-IMAGE.txt` file of the extracted emulator.
* The mount path for the emulator rootfs is `/opt/linux-arm-emulator-root` (change this path if you have a working directory at this path).

All the following instructions are for the Release mode. Change the commands and files accordingly for the Debug mode.

To just build libcoreclr and mscorlib for the Linux ARM Emulator, run the following command:
```
prajwal@ubuntu ~/coreclr $ ./tests/scripts/arm32_ci_script.sh \
    --emulatorPath=/opt/linux-arm-emulator \
    --mountPath=/opt/linux-arm-emulator-root \
    --buildConfig=Release \
    --skipTests
```

The Linux ARM Emulator is based on soft floating point and thus the native binaries in coreclr are built for the armel architecture. The coreclr binaries generated by the above command (native and mscorlib) can be found at `~/coreclr/bin/Product/Linux.armel.Release`.

To build libcoreclr and mscorlib, and run selected coreclr unit tests on the emulator, do the following:
* Download the latest Coreclr unit test binaries (or build on Windows) from here: [Debug](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_windows_nt_bld/lastSuccessfulBuild/artifact/bin/tests/tests.zip) and [Release](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_windows_nt_bld/lastSuccessfulBuild/artifact/bin/tests/tests.zip).  
Setup the binaries at `~/coreclr/bin/tests/Windows_NT.x64.Release`.
* Build corefx binaries for the Emulator as given [here](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-building.md#building-corefx-for-linux-arm-emulator).  
Setup these binaries at `~/corefx/bin/Linux.arm-softfp.Release`, `~/corefx/bin/Linux.AnyCPU.Release`, `~/corefx/bin/Unix.AnyCPU.Release`, and `~/corefx/bin/AnyOS.AnyCPU.Release`.
* Run the following command (change value of `--testDirFile` argument to the file containing your selection of tests):
```
prajwal@ubuntu ~/coreclr $ ./tests/scripts/arm32_ci_script.sh \
    --emulatorPath=/opt/linux-arm-emulator \
    --mountPath=/opt/linux-arm-emulator-root \
    --buildConfig=Release \
    --testRootDir=~/coreclr/bin/tests/Windows_NT.x64.Release \
    --coreFxNativeBinDir=~/corefx/bin/Linux.arm-softfp.Release \
    --coreFxBinDir="~/corefx/bin/Linux.AnyCPU.Release;~/corefx/bin/Unix.AnyCPU.Release;~/corefx/bin/AnyOS.AnyCPU.Release" \
    --testDirFile=~/coreclr/tests/testsRunningInsideARM.txt
```
