Cross Compilation for ARM on Windows
==================================

Building ARM for Windows can be done using cross compilation.

Requirements
------------

Install the ARM tools and Windows SDK, as described [here](https://github.com/dotnet/runtime/blob/main/docs/workflow/requirements/windows-requirements.md).

Cross compiling CoreCLR
-----------------------

Build using "arm" as the architecture. For example:

    C:\runtime> build.cmd -subset clr.runtime -arch arm -c debug

-or-

    C:\runtime> src\coreclr\build-runtime.cmd -arm -debug


Cross Compilation for ARM, ARM64 or x86 on Linux
================================================

Through cross compilation, on Linux it is possible to build CoreCLR for arm or arm64. Note that this documentation exists to explain using `runtime/eng/common/build-rootfs.sh`. This will build a rootfs and then use it to cross build. Newer documentation [linux-instructions.md](linux-instructions.md) exists which leverages docker to use a prebuilt environment to cross build.

Requirements for targeting Debian based distros
------------------------------------------------

You need a Debian based host and the following packages need to be installed:

    ~/runtime/ $ sudo apt-get install qemu qemu-user-static binfmt-support debootstrap

In addition, to cross compile CoreCLR the binutils for the target are required. So for arm you need:

    ~/runtime/ $ sudo apt-get install binutils-arm-linux-gnueabihf

and conversely for arm64:

    ~/runtime/ $ sudo apt-get install binutils-aarch64-linux-gnu

and for armel (ARM softfp):

    ~/runtime/ $ sudo apt-get install binutils-arm-linux-gnueabi


Requirements for targeting ARM or ARM64 Alpine Linux
-----------------------------------------------------

You can use any Linux distro as a host. The qemu, qemu-user-static and binfmt-support packages need to be installed (the names may be different for some distros).

In addition, to cross compile CoreCLR, the binutils for Alpine need to be built from the https://github.com/richfelker/musl-cross-make repo, since they are not available as packages.

To build them, use the following steps:
* Clone the repo
* Create a new config.mak file in the root directory of the repo and add the following lines into it:
  * `TARGET = armv6-alpine-linux-musleabihf` for ARM or `TARGET = aarch64-alpine-linux-musl` for ARM64
  * `OUTPUT = /usr`
  * `BINUTILS_CONFIG=--enable-gold=yes`
* Run `make` with current directory set to the root of the repo
* Run `sudo make install`

Generating the rootfs
---------------------
The `eng/common/cross/build-rootfs.sh` script can be used to download the files needed for cross compilation. It will generate a rootfs as this is what CoreCLR targets.

    Usage: ./eng/common/cross/build-rootfs.sh [BuildArch] [LinuxCodeName] [lldbx.y] [--skipunmount]
    BuildArch can be: arm(default), armel, arm64, x86
    LinuxCodeName - optional, Code name for Linux, can be: trusty(default), vivid, wily, xenial or alpine. If BuildArch is armel, LinuxCodeName is jessie(default) or tizen.
    lldbx.y - optional, LLDB version, can be: lldb3.6(default), lldb3.8. This is ignored when building rootfs for Alpine Linux.

The `build-rootfs.sh` script must be run as root as it has to make some symlinks to the system, it will by default generate the rootfs in `eng/common/cross/rootfs/<BuildArch>` however this can be changed by setting the `ROOTFS_DIR` environment variable.

For example, to generate an arm rootfs:

    ~/runtime/ $ sudo ./eng/common/cross/build-rootfs.sh arm

You can choose Linux code name to match your target, give `vivid` for `Ubuntu 15.04`, `wily` for `Ubuntu 15.10`. The default is `trusty`, version `Ubuntu 14.04`.

    ~/runtime/ $ sudo ./eng/common/cross/build-rootfs.sh arm wily

and if you wanted to generate the rootfs elsewhere:

    ~/runtime/ $ sudo ROOTFS_DIR=/home/cross/arm ./eng/common/cross/build-rootfs.sh arm

For example, to generate an armel rootfs:

    ~/runtime/ $ sudo ./eng/common/cross/build-rootfs.sh armel

You can choose code name to match your target, give `jessie` for `Debian`, `tizen` for `Tizen`. The default is `jessie`.

    ~/runtime/ $ sudo ./eng/common/cross/build-rootfs.sh armel tizen

and if you wanted to generate the rootfs elsewhere:

    ~/runtime/ $ sudo ROOTFS_DIR=/home/armel ./eng/common/cross/build-rootfs.sh armel tizen


Cross compiling CoreCLR
-----------------------
`ROOTFS_DIR` must be set when running `build-runtime.sh`.

    ~/runtime/ $ ROOTFS_DIR=/home/arm ./build.sh --subset clr.runtime --arch arm -c debug -v verbose --cross

-or-

    ~/runtime/ $ ROOTFS_DIR=/home/arm ./src/coreclr/build-runtime.sh -arm -debug -verbose -cross

As usual, the resulting binaries will be found in `artifacts/bin/coreclr/TargetOS.BuildArch.BuildType/`

Cross compiling CoreCLR for Other VFP configurations
----------------------------------------------------------
The default arm compilation configuration for CoreCLR is armv7-a with thumb-2 instruction set and
VFPv3 floating point with 32 64-bit FPU registers.

CoreCLR JIT requires 16 64-bit or 32 32-bit FPU registers.

A set of FPU configuration options have been provided via build-runtime.sh to accommodate different CPU types.
These FPU configuration options are: CLR_ARM_FPU_CAPABILITY and CLR_ARM_FPU_TYPE.

CLR_ARM_FPU_TYPE translates to a value given to -mfpu compiler option. Please refer to
your compiler documentation for possible options.

CLR_ARM_FPU_CAPABILITY is used by the PAL code to decide which FPU registers should be saved and
restored during context switches.

Bit 0 unused always set to 1.
Bit 1 corresponds to 16 64-bit FPU registers.
Bit 2 corresponds to 32 64-bit FPU registers.

Supported options are 0x3 and 0x7.

If you wanted to support armv7 CPU with VFPv3-d16, you'd use the following compile options:

```
./src/coreclr/build-runtime.sh -cross -arm -cmakeargs -DCLR_ARM_FPU_CAPABILITY=0x3 -cmakeargs -DCLR_ARM_FPU_TYPE=vfpv3-d16
```

Build System.Private.CoreLib on Ubuntu
--------------------------------------
The following instructions assume you are on a Linux machine such as Ubuntu 14.04 x86 64bit.

To build System.Private.CoreLib for Linux, run the following command:

```
    lgs@ubuntu ~/git/runtime/ $ ./build.sh --subset clr.corelib+clr.nativecorelib --arch arm -c debug -v verbose
```

The output is at `artifacts/bin/coreclr/<TargetOS>.arm.Debug/IL/System.Private.CoreLib.dll`.

```
    lgs@ubuntu ~/git/runtime/ $ file ./artifacts/bin/coreclr/Linux.arm.Debug/IL/System.Private.CoreLib.dll
    ./artifacts/bin/coreclr/Linux.arm.Debug/IL/System.Private.CoreLib.dll: PE32 executable (DLL)
    (console) ARMv7 Thumb Mono/.NET assembly, for MS Windows
```
