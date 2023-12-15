# Cross-Building for Different Architectures and Operating Systems

* [Windows Cross-Building](#windows-cross-building)
  * [Cross-Compiling for ARM64 on Windows](#cross-compiling-for-arm64-on-windows)
  * [Cross-Compiling for x86 on Windows](#cross-compiling-for-x86-on-windows)
* [macOS Cross-Building](#macos-cross-building)
* [Linux Cross-Building](#linux-cross-building)
  * [Generating the ROOTFS](#generating-the-rootfs)
    * [ROOTFS for FreeBSD](#rootfs-for-freebsd)
  * [Cross-Compiling CoreCLR](#cross-compiling-coreclr)
    * [CoreCLR for FreeBSD](#coreclr-for-freebsd)
    * [Cross-Compiling CoreCLR for other VFP Configurations](#cross-compiling-coreclr-for-other-vfp-configurations)
  * [Building the Cross-Targeting Tools](#building-the-cross-targeting-tools)
* [Cross-Building using Docker](#cross-building-using-docker)
  * [Cross-Compiling for ARM32 and ARM64 with Docker](#cross-compiling-for-arm32-and-arm64-with-docker)
  * [Cross-Compiling for FreeBSD with Docker](#cross-compiling-for-freebsd-with-docker)

This guide will go more in-depth on how to do cross-building across multiple operating systems and architectures. It's worth mentioning this is not an any-to-any scenario. Only the combinations explained here are possible/supported. If/When any other combinations get supported/discovered, this document will get updated accordingly.

## Windows Cross-Building

This section will go over cross-compiling on Windows. Currently, Windows allows you to cross-compile from x64 to basically any other architecture.

### Cross-Compiling for ARM64 on Windows

To do cross-compilation for ARM64 on Windows, first make sure you have the appropriate tools and Windows SDK installed. This is described in detail in the [Windows requirements doc](/docs/workflow/requirements/windows-requirements.md#visual-studio).

Once you have all the required dependencies, it is a straightforward process. Windows knows how to cross-build behind curtains, so all you have to do is specify which architecture you want to build for:

```cmd
.\build.cmd -s clr -c Release -arch arm64
```

### Cross-Compiling for x86 on Windows

Building for x86 doesn't require any additional software installed or configured, since all the x64 build tools also have the capability of building for x86. Just specify it when calling the build script:

```cmd
.\build.cmd -s clr -c Release -arch x86
```

## macOS Cross-Building

This section will go over cross-compiling on macOS. Currently, macOS allows you to cross-compile between x64 and ARM64.

Similarly to targeting Windows x86, the native tooling you installed back in the [macOS requirements doc](/docs/workflow/requirements/macos-requirements.md) has the capabilities to effectuate the cross-compilation. You have simply to pass the `-cross` flag, along with the designated architecture. For example, for an arm64 build on an Intel x64 Mac:

```bash
./build.sh -s clr -c Release --cross -a arm64
```

## Linux Cross-Building

This section will go over cross-compiling on Linux. Currently, Linux allows you to cross-compile from x64 to ARM32 and ARM64, as well as to other Unix-based operating systems, like FreeBSD and Alpine.

### Generating the ROOTFS

Before you can attempt to do any Linux cross-building, you will need to generate the _ROOTFS_ corresponding to the platform you want to target. The script located in `eng/common/cross/build-rootfs.sh` is in charge of effectuating this task. Note that this script must be run with `sudo`, as it needs to make some symlinks to the system that would not be allowed otherwise.

For example, let's try generating a _ROOTFS_ targeting Ubuntu 18 (Bionic) for ARM64:

```bash
sudo ./eng/common/cross/build-rootfs.sh arm64 bionic
```

The _rootfs_ binaries will be placed in `.tools/rootfs/<arch>`. So, for this example, it would be `.tools/rootfs/arm64`. Note that the Linux codename argument is optional, and if you omit it, the script will pick its default one.

It is also possible to have `build-rootfs.sh` generate its output elsewhere. For that, you have to set the environment variable `ROOTFS_DIR` to the path where you want your _rootfs_ binaries to be placed in.

#### ROOTFS for FreeBSD

Generating the _ROOTFS_ for FreeBSD cross-compiling is virtually the same as for other Linux distributions in other architectures. The only difference is you have to specify it so. For example, for an x64 cross-compilation for FreeBSD 13:

```bash
sudo ./eng/common/cross/build-rootfs.sh x64 freebsd13
```

### Cross-Compiling CoreCLR

Once you have your _ROOTFS_ generated, make sure to set the environment variable `ROOTFS_DIR` to where your binaries are located if you didn't do so in the previous step. Then, build normally and pass the `--cross` flag to the build script:

```bash
export ROOTFS_DIR=/path/to/runtime/.tools/rootfs/arm64
./build.sh --subset clr --configuration Release --arch arm64 --cross
```

Like with any other build, you'll find the built binaries at `artifacts/bin/coreclr/Linux.<arch>.<configuration>`. For our example, it would be `artifacts/bin/coreclr/Linux.arm64.Release`.

#### CoreCLR for FreeBSD

Very similarly to generating the _ROOTFS_, cross-building for FreeBSD follows the same process as for other architectures, which is described above. The only difference is that, in addition to the `--cross` flag, you also have to specify it is for FreeBSD by means of the `--os` flag:

```bash
export ROOTFS_DIR=/path/to/runtime/.tools/rootfs/x64
./build.sh --subset clr --configuration Release --cross --os freebsd
```

#### Cross-Compiling CoreCLR for other VFP Configurations

The default ARM compilation configuration for CoreCLR is armv7-a with thumb-2 instruction set, and VFPv3 floating point with 32 64-bit FPU registers.

CoreCLR JIT requires 16 64-bit or 32 32-bit FPU registers.

A set of FPU configuration options have been provided in the build scripts to accommodate different CPU types. These FPU configuration options are:

* _CLR\_ARM\_FPU\_TYPE_: Translates to a value given to the `-mfpu` compiler option. Please refer to your compiler documentation for possible options.
* _CLR\_ARM\_FPU\_CAPABILITY_: Used by the PAL code to decide which FPU registers should be saved and restored during context switches (the supported options are 0x3 and 0x7):
  * Bit 0 unused always set to 1.
  * Bit 1 corresponds to 16 64-bit FPU registers.
  * Bit 2 corresponds to 32 64-bit FPU registers.

For example, if you wanted to support armv7 CPU with VFPv3-d16, you'd use the following compile options:

```bash
./build.sh --subset clr --configuration Release --cross --arch arm --cmakeargs "-DCLR_ARM_FPU_CAPABILITY=0x3" --cmakeargs "-DCLR_ARM_FPU_TYPE=vfpv3-d16"
```

### Building the Cross-Targeting Tools

Certain parts of the build process need some native components that are built for the current machine architecture, regardless of whichever you are targeting. These tools are referred to as cross-targeting tools or "cross tools". There are two categories of these tools today:

* Crossgen2 JIT Tools
* Diagnostic Libraries

The Crossgen2 JIT tools are used to run Crossgen2 on libraries built during the current build, such as during the `clr.nativecorelib` stage. Under normal circumstances, you should have no need to worry about this, since these tools are automatically built when using the `.\build.cmd` or `./build.sh` scripts at the root of the repo to build any of the CoreCLR native files.

However, you might find yourself needing to (re)build them because either you made changes to them, or you built CoreCLR in a different way using `build-runtime.sh` instead of the usual default script at the root of the repo. To build these tools, you need to run the `src/coreclr/build-runtime.sh` script, and pass the `-hostarch` flag with the architecture of the host machine, alongside the `-component crosscomponents` flag to specify that you only want to build the cross-targeting tools. Retaking our previous example of building for ARM64 using an x64 Linux machine:

```bash
./src/coreclr/build-runtime.sh -arm64 -hostarch x64 -component crosscomponents -cmakeargs "-DCLR_CROSS_COMPONENTS_BUILD=1"
```

The output of running this command is placed in `artifacts/bin/coreclr/linux.<target_arch>.<configuration>/<host_arch>`. For our example, it would be `artifacts/bin/coreclr/linux.arm64.Release/x64`.

On Windows, you can build these cross-targeting diagnostic libraries with the `linuxdac` and `alpinedac` subsets from the root `build.cmd` script. That said, you can also use the `build-runtime.cmd` script, like with Linux. These builds also require you to pass the `-os` flag to specify the target OS. For example:

```cmd
.\src\coreclr\build-runtime.cmd -arm64 -hostarch x64 -os linux -component crosscomponents -cmakeargs "-DCLR_CROSS_COMPONENTS_BUILD=1"
```

If you're building the cross-components in powershell, you'll need to wrap `"-DCLR_CROSS_COMPONENTS_BUILD=1"` with single quotes (`'`) to ensure things are escaped correctly for CMD.

## Cross-Building using Docker

When it comes to building, Docker offers the most flexibility when it comes to targeting different Linux platforms and other similar Unix-based ones, like FreeBSD. This is thanks to the multiple existing Docker images already configured for doing such cross-platform building, and Docker's ease of use of running out of the box on Windows machines with [WSL](https://docs.microsoft.com/windows/wsl/about) enabled, installed, and up and running, as well as Linux machines.

### Cross-Compiling for ARM32 and ARM64 with Docker

As mentioned in the [Linux Cross-Building section](#linux-cross-building), the `ROOTFS_DIR` environment variable has to be set to the _crossrootfs_ location. The prereqs Docker images already have _crossrootfs_ built, so you only need to specify it when creating the Docker container by means of the `-e` flag. These locations are specified in the [Docker Images table](/docs/workflow/building/coreclr/linux-instructions.md#docker-images).

In addition, you also have to specify the `--cross` flag with the target architecture. For example, the following command would create a container to build CoreCLR for Linux ARM64:

```bash
docker run --rm \
  -v <RUNTIME_REPO_PATH>:/runtime \
  -w /runtime \
  -e ROOTFS_DIR=/crossrootfs/arm64 \
  mcr.microsoft.com/dotnet-buildtools/prereqs:cbl-mariner-2.0-cross-arm64 \
  ./build.sh --subset clr --cross --arch arm64
```

### Cross-Compiling for FreeBSD with Docker

Using Docker to cross-build for FreeBSD is very similar to any other Docker Linux build. You only need to use the appropriate image and pass `--os` as well to specify this is not an architecture(-only) build. For example, to make a FreeBSD x64 build:

```bash
docker run --rm \
  -v <RUNTIME_REPO_PATH>:/runtime \
  -w /runtime \
  -e ROOTFS_DIR=/crossrootfs/x64 \
  mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-cross-freebsd-12 \
  ./build.sh --subset clr --cross --os freebsd
```
