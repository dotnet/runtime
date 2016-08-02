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
- llvm-3.5 
- clang-3.5 
- lldb-3.6
- lldb-3.6-dev 
- libunwind8 
- libunwind8-dev
- gettext
- libicu-dev
- liblttng-ust-dev
- libcurl4-openssl-dev
- libssl-dev
- uuid-dev

In order to get lldb-3.6 on Ubuntu 14.04, we need to add an additional package source:

```
ellismg@linux:~$ echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | sudo tee /etc/apt/sources.list.d/llvm.list
ellismg@linux:~$ wget -O - http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
ellismg@linux:~$ sudo apt-get update
```

Then install the packages you need:

```
ellismg@linux:~$ sudo apt-get install cmake llvm-3.5 clang-3.5 lldb-3.6 lldb-3.6-dev libunwind8 libunwind8-dev gettext libicu-dev liblttng-ust-dev libcurl4-openssl-dev libssl-dev uuid-dev
```

You now have all the required components.

Git Setup
---------

This guide assumes that you've cloned the corefx and coreclr repositories into `~/git/corefx` and `~/git/coreclr` on your Linux machine. If your setup is different, you'll need to pay careful attention to the commands you run. In this guide, I'll always show what directory I'm in.

Set the maximum number of file-handles
--------------------------------------

To ensure that your system can allocate enough file-handles for the corefx build run `sysctl fs.file-max`. If it is less than 100000, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

Build the Runtime and Microsoft Core Library
=============================================

To build the runtime on Linux, run build.sh from the root of the coreclr repository:

```
ellismg@linux:~/git/coreclr$ ./build.sh
```

After the build is completed, there should some files placed in `bin/Product/Linux.x64.Debug`.  The ones we are interested in are:

* `corerun`: The command line host.  This program loads and starts the CoreCLR runtime and passes the managed program you want to run to it.
* `libcoreclr.so`: The CoreCLR runtime itself.
* `mscorlib.dll`: Microsoft Core Library.

Build the Framework
===================

```
ellismg@linux:~/git/corefx$ ./build.sh
```

After the build is complete you will be able to find the output in the `bin` folder.

Build for ARM/Linux
===================

Libunwind-arm requires fixes that are not included in Ubuntu 14.04, yet. The fix allows libunwind-arm not to break when it is ordered to access unaccessible memory locations.

First, import the patch from the libunwind upstream: http://git.savannah.gnu.org/gitweb/?p=libunwind.git;a=commit;h=770152268807e460184b4152e23aba9c86601090

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

How to enable -O3 optimization level for ARM/Linux
==================================================

Currently, we can build coreclr with -O1 flag of clang in release build mode for Linux/ARM without any bugfix of llvm-3.6. This instruction is to enable -O3 optimization level of clang on Linux/ARM by fixing the bug of llvm.

First, download latest version from the clang-3.6/llvm-3.6 upstream: 
```
lgs@ubuntu cd /work/dotnet/
lgs@ubuntu wget http://llvm.org/releases/3.6.2/llvm-3.6.2.src.tar.xz
lgs@ubuntu tar xJf llvm-3.6.2.src.tar.xz
lgs@ubuntu cd ./llvm-3.6.2.src/tools/
lgs@ubuntu wget http://llvm.org/releases/3.6.2/cfe-3.6.2.src.tar.xz
lgs@ubuntu tar xJf cfe-3.6.2.src.tar.xz
lgs@ubuntu mv cfe-3.6.2.src clang
```

Second, expand the coverage of the upstream patch by:
https://bugs.launchpad.net/ubuntu/+source/llvm-defaults/+bug/1584089

Third, build clang-3.6/llvm-3.6 source as following: 
```
lgs@ubuntu cmake -DCMAKE_BUILD_TYPE=Release -DLLVM_TARGETS_TO_BUILD="all" -DCMAKE_INSTALL_PREFIX=~/llvm-3.6.2 \
-DLLVM_BUILD_LLVM_DYLIB=1 -DLLDB_DISABLE_LIBEDIT=1 -DLLDB_DISABLE_CURSES=1 -DLLDB_DISABLE_PYTHON=1 \
-DLLVM_ENABLE_DOXYGEN=0 -DLLVM_ENABLE_TERMINFO=0 -DLLVM_INCLUDE_EXAMPLES=0 -DLLVM_BUILD_RUNTIME=0 \
-DLLVM_INCLUDE_TESTS=0 -DPYTHON_INCLUDE_DIR=/usr/include/python2.7 /work/dotnet/llvm-3.6.2.src
lgs@ubuntu  
lgs@ubuntu sudo ln -sf /usr/bin/ld /usr/bin/ld.gold
lgs@ubuntu time make -j8
lgs@ubuntu time make -j8 install 
lgs@ubuntu
lgs@ubuntu sudo apt-get remove clang-3.6 llvm-3.6
lgs@ubuntu  vi ~/.bashrc (or /etc/profile)
# Setting new clang/llvm version
export PATH=$HOME/llvm-3.6.2/bin/:$PATH
export LD_LIBRARY_PATH=$HOME/llvm-3.6.2/lib:$LD_LIBRARY_PATH
```

For Ubuntu 14.04 X64 users, they can easily install the fixed clang/llvm3.6 package with "apt-get" command from the "ppa:leemgs/dotnet" Ubuntu repository, without the need to execute the above 1st, 2nd, and 3rd step.
```
lgs@ubuntu sudo add-apt-repository ppa:leemgs/dotnet
lgs@ubuntu sudo apt-get update
lgs@ubuntu sudo apt-get install clang-3.6 llvm-3.6 lldb-3.6
```

Finally, let's build coreclr with updated clang/llvm. If you meet a lldb related error message at build-time, try to build coreclr with "skipgenerateversion" option. 
```
lgs@ubuntu time ROOTFS_DIR=/work/dotnet/rootfs-coreclr/arm ./build.sh arm release clean cross 
```

Additional optimization levels for ARM/Linux: -Oz and -Ofast
============================================================

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

