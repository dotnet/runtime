Building CoreFX on FreeBSD, Linux and OS X
==========================================

The CoreFX build has two logical components, the native build which produces the
"shims" (which provide a stable interface between the OS and managed code) and
the managed build which produces the MSIL code and nuget packages that make up
CoreFX.

The native component should be buildable on any system, but the managed
components require a version of the .NET Core CLI (which the build will
download) so managed components can only be built on a subset of distros.

### Prerequsites (native build)

The native build produces shims over libc, openssl, gssapi, libcurl and
libz. The build system uses CMake (2.8.12 or higher) to generate Makefiles using
clang (3.5 or higher).  The build also uses git for generating some version
information.

For Ubuntu 14.04, the following packages should be installed to build the native
components

* git
* clang-3.5
* cmake
* make
* libc6-dev
* libssl-dev
* libkrb5-dev
* libcurl4-openssl-dev
* zlib1g-dev

`sudo apt-get install git clang-3.5 cmake make libc6-dev libssl-dev libkrb5-dev
libcurl4-openssl-dev zlib1g-dev`

On OS X, all the needed libraries are provided by XCode, with the exception of
openssl. To install openssl, we recommend that you use
[Homebrew](http://brew.sh) and do the following:

```
brew install openssl
brew link --force openssl
```

Once installed, the native components can be built by running:

```bash
./build.sh native
```

from the root of the repository

### Prerequsites (managed build)

Since the managed build uses the .NET Core CLI, there are some additional
pre-requesties from the CLI which need to be installed. Both libicu and
libunwind are used by CoreCLR to execute managed code, so they must be
installed. Since CoreFX does not actually link against these packages, runtime
versions are sufficent.  We also require curl to be present, which we use to
download the .NET Core CLI.

For Ubuntu 14.04, install the following packages:

* libunwind8
* libicu52
* curl

`sudo apt-get install libunwind8 libicu52 curl`

In addition to the above pacakges, the runtime versions of the packages listed
in the native section should also be installed (this happens automatically on
most systems when you install the development packages).

On OS X, we also require that openssl has been installed via Homebrew.

Once installed, the managed components can be built by running:

```bash
./build.sh managed
```

### `build.sh` Usage
When run without any arguments, `build.sh` will attempt to build both the native
and managed code.

There many flags that can be passed to `build.sh` to control its behavior.

`./build.sh [managed] [native] [BuildArch] [BuildType] [clean] [verbose] [clangx.y] [platform] [cross] [skiptests] [cmakeargs]`

**Example:**

`./build.sh native x64 verbose clang3.9`

**Options:**

```bash
managed            # optional argument to build the managed code
native             # optional argument to build the native code
platform           # OS to compile for (FreeBSD, Linux, NetBSD, OSX, Windows)
skiptests          # build, but do not run, the managed unit tests
BuildType          # build configuration type (Debug, Release)

# The following arguments affect native builds only:

BuildArch          # build architecture (x64, x86, arm, arm64)
clean              # optional argument to force a clean build
verbose            # optional argument to enable verbose build output
clangx.y           # optional argument to build using clang version x.y
cross              # optional argument to signify cross compilation, uses ROOTFS_DIR environment variable if set
cmakeargs          # user-settable additional arguments passed to CMake

```
### Known Issues
If you see errors along the lines of `SendFailure (Error writing headers)` you may need to import trusted root certificates:

```sh
mozroots --import --sync
```

## Steps to build on Ubuntu 14.04 LTS


1. Install the prerequisites
 * `sudo apt-get install git clang-3.5 cmake make libc6-dev libssl-dev
   libkrb5-dev libcurl4-openssl-dev zlib1g-dev libunwind8 libicu52 curl`
2. Clone the corefx repo `git clone https://github.com/dotnet/corefx.git`
3. Navigate to the `corefx` directory
4. Run the build script `./build.sh`
