Build CoreCLR on Linux
======================

This guide will walk you through building CoreCLR on Linux.  We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions are written assuming the Ubuntu 14.04 LTS, since that's the distro the team uses. Pull Requests are welcome to address other environments as long as they don't break the ability to use Ubuntu 14.04 LTS.

There have been reports of issues when using other distros or versions of Ubuntu (e.g. [Issue 95](https://github.com/dotnet/coreclr/issues/95)). If you're on another distribution, consider using docker's `ubuntu:14.04` image.

Minimum RAM required to build is 1GB. The build is known to fail on 512 MB VMs ([Issue 536](https://github.com/dotnet/coreclr/issues/536)).

Toolchain Setup
---------------

Install the following packages for the toolchain: 

- cmake 
- llvm-3.9
- clang-3.9
- lldb-3.9
- liblldb-3.9-dev
- libunwind8 
- libunwind8-dev
- gettext
- libicu-dev
- liblttng-ust-dev
- libcurl4-openssl-dev
- libssl-dev
- libkrb5-dev
- libnuma-dev (optional, enables numa support)

In order to get clang-3.9, llvm-3.9 and lldb-3.9 on Ubuntu 14.04, we need to add an additional package source:

    ~$ echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.9 main" | sudo tee /etc/apt/sources.list.d/llvm.list
    ~$ wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
    ~$ sudo apt-get update

Note: ARM clang has a known issue with CompareExchange
([#15074](https://github.com/dotnet/coreclr/issues/15074)), so for ARM you must
use clang-4.0 or higher.  Moreover, when building with clang-5.0, the
following errors occur:

```
coreclr/src/debug/inc/arm/primitives.h:66:1: error: __declspec attribute 'selectany' is
      not supported [-Werror,-Wignored-attributes]
```

This is fixed in clang-5.0.2, which can be installed from the apt
repository listed below.

For other version of Debian/Ubuntu, please visit http://apt.llvm.org/.

Then install the packages you need:

    ~$ sudo apt-get install cmake llvm-3.9 clang-3.9 lldb-3.9 liblldb-3.9-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev libnuma-dev libkrb5-dev

The lldb 3.9 package needs a lib file symbolic link fixed:

    cd /usr/lib/llvm-3.9/lib
    sudo ln -s ../../x86_64-linux-gnu/liblldb-3.9.so.1 liblldb-3.9.so.1

You now have all the required components.

If you are using Fedora, then you will need to install the following packages:

    ~$ sudo dnf install llvm cmake clang lldb-devel libunwind-devel lttng-ust-devel libicu-devel numactl-devel

Git Setup
---------

This guide assumes that you've cloned the corefx and coreclr repositories into `~/git/corefx` and `~/git/coreclr` on your Linux machine. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in.

Set the maximum number of file-handles
--------------------------------------

To ensure that your system can allocate enough file-handles for the corefx build run `sysctl fs.file-max`. If it is less than 100000, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

On Fedora:

`$ sudo dnf install mono-devel`

Build the Runtime and Microsoft Core Library
=============================================

To build the runtime on Linux, run build.sh from the root of the coreclr repository:

```
ellismg@linux:~/git/coreclr$ ./build.sh
```

After the build is completed, there should some files placed in `bin/Product/Linux.x64.Debug`.  The ones we are interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `System.Private.CoreLib.dll`: Microsoft Core Library.

Build the Framework
===================

```
ellismg@linux:~/git/corefx$ ./build.sh
```

After the build is complete you will be able to find the output in the `bin` folder.

Build for ARM/Linux
===================

The CI system and official builds use Docker to build ARM for Linux (for example, see the latest build [here](https://ci.dot.net/job/dotnet_coreclr/job/master/job/arm_cross_checked_ubuntu/lastSuccessfulBuild/consoleText)). The Docker container has pre-built rootfs directories containing the required tools. To build this way, do the following:

* Install Docker, probably Community Edition, on Windows, Mac, or Linux, from https://www.docker.com/. Some useful post-install setup is:
  * Linux: add your user to the docker group, this will avoid running docker with `sudo`:

    `sudo usermod -a <your account name> -G docker`

  * Windows: switch to Linux containers. This can be done by right clicking on the Docker icon in the lower right corner and clicking "Switch to Linux containers".
* Build using the Docker container (this is a `bash` script, for simplicity):

```
ROOT=/Users/me/git/coreclr
DOCKER_ARGS="run -i --rm -v ${ROOT}:/mnt/coreclr -w /mnt/coreclr -e ROOTFS_DIR=/crossrootfs/arm microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-cross-e435274-20180426002420"
docker ${DOCKER_ARGS} /mnt/coreclr/build.sh arm checked cross
docker ${DOCKER_ARGS} /mnt/coreclr/build-test.sh arm checked cross generatelayoutonly
```

Make sure you update the `ROOT` environment to point to your git clone of the coreclr repo.

The current Docker tag being used by the CI can be found in the `getDockerImageName` function in the [netci.groovy](https://github.com/dotnet/coreclr/blob/master/netci.groovy) file.

Libunwind issue
---------------
ARM libunwind versions before 1.3 require a fix. The fix allows libunwind not to break when it is ordered to access inaccessible memory locations. See [this](https://github.com/dotnet/coreclr/pull/3923) issue for history.

If required, first import the patch from the libunwind upstream: http://git.savannah.gnu.org/gitweb/?p=libunwind.git;a=commit;h=770152268807e460184b4152e23aba9c86601090.

Then, expand the coverage of the upstream patch by:

```
diff --git a/src/arm/Ginit.c b/src/arm/Ginit.c
index 1ed3dbf..c643032 100644
--- a/src/arm/Ginit.c
+++ b/src/arm/Ginit.c
@@ -128,6 +128,11 @@ access_mem (unw_addr_space_t as, unw_word_t addr, unw_word_t *val, int write,
 {
   if (write)
     {
+      /* validate address */
+      const struct cursor *c = (const struct cursor *) arg;
+      if (c && validate_mem(addr))
+        return -1;
+
       Debug (16, "mem[%x] <- %x\n", addr, *val);
       *(unw_word_t *) addr = *val;
     }
```

Additional optimization levels for ARM/Linux: -Oz and -Ofast
------------------------------------------------------------

This instruction is to enable additional optimization levels such as -Oz and -Ofast on ARM/Linux. The below table shows what we have to enable for the code optimization of the CoreCLR run-time either the size or speed on embedded devices. 

| **Content** | **Build Mode** | **Clang/LLVM (Linux)** |
| --- | --- | --- |
| -O0 | Debug | Disable optimization to generate the most debuggable code |
| -O1 | - | Optimize for code size and execution time |
| -O2 | Checked | Optimize more for code size and execution time |
| -O3 | Release | Optimize more for code size and execution time to make program run faster |
| -Oz | - | Optimize more to reduce code size further |
| -Ofast | - | Enable all the optimizations from O3 along with other aggressive optimizations |

If you want to focus on the size reduction for low-end devices, you have to modify clang-compile-override.txt to enable -Oz flag in the release build as following: 

```
--- a/src/pal/tools/clang-compiler-override.txt
+++ b/src/pal/tools/clang-compiler-override.txt
@@ -3,13 +3,13 @@ SET (CMAKE_C_FLAGS_DEBUG_INIT          "-g -O0")
 SET (CLR_C_FLAGS_CHECKED_INIT          "-g -O2")
 # Refer to the below instruction to support __thread with -O2/-O3 on Linux/ARM
 # https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md
-SET (CMAKE_C_FLAGS_RELEASE_INIT        "-g -O3")
+SET (CMAKE_C_FLAGS_RELEASE_INIT        "-g -Oz")
 SET (CMAKE_C_FLAGS_RELWITHDEBINFO_INIT "-g -O2")

 SET (CMAKE_CXX_FLAGS_INIT                "-Wall -Wno-null-conversion -std=c++11")
 SET (CMAKE_CXX_FLAGS_DEBUG_INIT          "-g -O0")
 SET (CLR_CXX_FLAGS_CHECKED_INIT          "-g -O2")
-SET (CMAKE_CXX_FLAGS_RELEASE_INIT        "-g -O3")
+SET (CMAKE_CXX_FLAGS_RELEASE_INIT        "-g -Oz")
 SET (CMAKE_CXX_FLAGS_RELWITHDEBINFO_INIT "-g -O2")

 SET (CLR_DEFINES_DEBUG_INIT              DEBUG _DEBUG _DBG URTBLDENV_FRIENDLY=Checked BUILDENV_
```


If you want to focus on the speed optimization for high-end devices, you have to modify clang-compile-override.txt to enable -Ofast flag in the release build as following: 
```
--- a/src/pal/tools/clang-compiler-override.txt
+++ b/src/pal/tools/clang-compiler-override.txt
@@ -3,13 +3,13 @@ SET (CMAKE_C_FLAGS_DEBUG_INIT          "-g -O0")
 SET (CLR_C_FLAGS_CHECKED_INIT          "-g -O2")
 # Refer to the below instruction to support __thread with -O2/-O3 on Linux/ARM
 # https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md
-SET (CMAKE_C_FLAGS_RELEASE_INIT        "-g -O3")
+SET (CMAKE_C_FLAGS_RELEASE_INIT        "-g -Ofast")
 SET (CMAKE_C_FLAGS_RELWITHDEBINFO_INIT "-g -O2")

 SET (CMAKE_CXX_FLAGS_INIT                "-Wall -Wno-null-conversion -std=c++11")
 SET (CMAKE_CXX_FLAGS_DEBUG_INIT          "-g -O0")
 SET (CLR_CXX_FLAGS_CHECKED_INIT          "-g -O2")
-SET (CMAKE_CXX_FLAGS_RELEASE_INIT        "-g -O3")
+SET (CMAKE_CXX_FLAGS_RELEASE_INIT        "-g -Ofast")
 SET (CMAKE_CXX_FLAGS_RELWITHDEBINFO_INIT "-g -O2")

 SET (CLR_DEFINES_DEBUG_INIT              DEBUG _DEBUG _DBG URTBLDENV_FRIENDLY=Checked BUILDENV_
```
