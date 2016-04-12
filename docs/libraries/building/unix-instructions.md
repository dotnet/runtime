Building CoreFX on FreeBSD, Linux and OS X
==========================================

Building CoreFX is pretty straightforward. Clone the repo and run the build script.

```bash
git clone https://github.com/dotnet/corefx.git
cd corefx
./build.sh
```

### `build.sh` Usage
 `./build.sh [managed] [native] [BuildArch] [BuildType] [clean] [verbose] [clangx.y] [platform] [cross] [skiptests] [cmakeargs]`

**Example:**

`./build.sh native x64 verbose clang3.9`

**Options:**

```bash
managed            # optional argument to build the managed code
native             # optional argument to build the native code

# The following arguments affect native builds only:

BuildArch          # build architecture (x64, x86, arm, arm64)
BuildType          # build configuration type (Debug, Release)
clean              # optional argument to force a clean build
verbose            # optional argument to enable verbose build output
clangx.y           # optional argument to build using clang version x.y
platform           # OS to compile for (FreeBSD, Linux, NetBSD, OSX, Windows)
cross              # optional argument to signify cross compilation, uses ROOTFS_DIR environment variable if set
skiptests          # skip the tests in the './bin/*/*Tests/' subdirectory
cmakeargs          # user-settable additional arguments passed to CMake
```

### Prerequisites

* bash
* curl (devel)
* icu
* clang
* llvm
* lldb
* cmake

> Note: These instructions have been validated on:
* Ubuntu 15.04, 14.04, and 12.04
* Fedora 22
* OS X 10.10 (Yosemite)
* FreeBSD 10.2
* NetBSD 7.0
* Alpine Linux 3.3

### Known Issues
If you see errors along the lines of `SendFailure (Error writing headers)` you may need to import trusted root certificates:

```sh
mozroots --import --sync
```
