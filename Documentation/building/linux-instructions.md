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

To ensure that your system can allocate enough file-handles for the corefx build, add `fs.file-max = 100000` to `/etc/sysctl.conf`, and then run `sudo sysctl -p`.

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
