Build CoreCLR on NetBSD
=======================

This guide will walk you through building CoreCLR on NetBSD.  We'll start by showing how to set up your environment from scratch.

Environment
===========

These instructions are written on NetBSD 7.x on the amd64 platform, since that's the release the team uses.

Older releases aren't supported because building CoreCLR requires the modern LLVM stack (Clang, libunwind, and LLDB) that is developed against the NetBSD-7.x branch.

Pull Requests are welcome to address other ports (like i386 or evbarm) as long as they don't break the ability to use NetBSD/amd64.

Minimum RAM required to build is 1GB.

The pkgsrc framework is required to build .NET projects on NetBSD. Minimal pkgsrc version required is 2016Q1.

pkgsrc setup
------------

Fetch pkgsrc and install to the system. By default it's done in the /usr directory as root:

```
ftp -o- ftp://ftp.netbsd.org/pub/pkgsrc/stable/pkgsrc.tar.gz | tar -zxpf- -C /usr
```

The .NET projects are tracked in pkgsrc-wip.

In order to use pkgsrc-wip, git must be installed:


```
cd /usr/pkgsrc/devel/git-base && make install
```

To access resources over SSL link, mozilla-rootcerts must be installed:

```
cd /usr/pkgsrc/security/mozilla-rootcerts && make install
```

And follow the MESSAGE commands to finish the installation.


Installing pkgsrc-wip
---------------------

Type the following command to fetch the pkgsrc-wip sources:


```
cd /usr/pkgsrc
git clone --depth 1 git://wip.pkgsrc.org/pkgsrc-wip.git wip
```

Then install the CoreCLR package you need:

```
cd /usr/pkgsrc/wip/coreclr-git
make install
```

CoreCLR is installed in `/usr/pkg/CoreCLR` subdirectory by default.


PAL tests
=========

To run PAL tests on NetBSD, use the `make test` in the coreclr-git package from pkgsrc-wip:

```
cd /usr/pkgsrc/wip/coreclr-git
make test
```

Build CoreFX
============

The CoreFX package is located in pkgsrc-wip as corefx-git. In order to build it you need to perform the following command:

```
cd /usr/pkgsrc/wip/corefx-git
make
```

At the moment there is no install or test target in the pkgsrc framework.

CoreFX tests
============

The steps to run CoreFX managed code tests:

Build CoreCLR (with pkgsrc-wip/coreclr-git) on NetBSD x64, Debug and install the Product dir to /usr/pkg/CoreCLR:

```
cd /usr/pkgsrc/wip/coreclr-git && make install
```

Build CoreFX native x64 Debug and the work (build) dir is in /usr/pkgsrc/wip/corefx-git/work/corefx:

```
cd /usr/pkgsrc/wip/corefx-git && make
```

Build CoreCLR Debug x64 on Linux and copy System.Private.CoreLib.dll from ./artifacts/Product/Linux.x64.Debug/System.Private.CoreLib.dll to NetBSD machine under /usr/pkg/CoreCLR:

```
./build.sh mscorlib Debug
```

Build CoreFX Debug x64 on Linux and copy artifacts/ to NetBSD machine under /public/bin:

```
./build-native.sh -os=NetBSD
./build-managed.sh NetBSD -SkipTests
```

Run ./run-test.sh:

```
$ pwd
/usr/pkgsrc/wip/corefx-git/work/corefx
$ ./eng/run-test.sh \
--coreclr-bins /usr/pkg/CoreCLR/ \
--mscorlib-bins /usr/pkg/CoreCLR/ \
--corefx-tests /public/artifacts/tests/NetBSD.AnyCPU.Debug/ \
--corefx-native-bins ./artifacts/NetBSD.x64.Debug/Native/
```
