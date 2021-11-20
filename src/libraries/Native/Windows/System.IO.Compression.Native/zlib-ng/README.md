## zlib-ng
*zlib data compression library for the next generation systems*

Maintained by Hans Kristian Rosbach
          aka Dead2 (zlib-ng àt circlestorm dót org)

|CI|Status|
|:-|-|
|GitHub Actions|[![Master Branch Status](https://github.com/zlib-ng/zlib-ng/workflows/CI%20CMake/badge.svg)](https://github.com/zlib-ng/zlib-ng/actions) [![Master Branch Status](https://github.com/zlib-ng/zlib-ng/workflows/CI%20Configure/badge.svg)](https://github.com/zlib-ng/zlib-ng/actions) [![Master Branch Status](https://github.com/zlib-ng/zlib-ng/workflows/CI%20NMake/badge.svg)](https://github.com/zlib-ng/zlib-ng/actions)|
|Buildkite|[![Build status](https://badge.buildkite.com/7bb1ef84356d3baee26202706cc053ee1de871c0c712b65d26.svg?branch=develop)](https://buildkite.com/circlestorm-productions/zlib-ng)|
|CodeFactor|[![CodeFactor](https://www.codefactor.io/repository/github/zlib-ng/zlib-ng/badge)](https://www.codefactor.io/repository/github/zlib-ng/zlib-ng)|
|OSS-Fuzz|[![Fuzzing Status](https://oss-fuzz-build-logs.storage.googleapis.com/badges/zlib-ng.svg)](https://bugs.chromium.org/p/oss-fuzz/issues/list?sort=-opened&can=1&q=proj:zlib-ng)
|Codecov|[![codecov.io](https://codecov.io/github/zlib-ng/zlib-ng/coverage.svg?branch=develop)](https://codecov.io/github/zlib-ng/zlib-ng/)|


Features
--------

* Zlib compatible API with support for dual-linking
* Modernized native API based on zlib API for ease of porting
* Modern C99 syntax and a clean code layout
* Deflate medium and quick algorithms based on Intels zlib fork
* Support for CPU intrinsics when available
  * Adler32 implementation using SSSE3, AVX2, Neon & VSX
  * CRC32-B implementation using PCLMULQDQ & ACLE
  * Hash table implementation using CRC32-C intrinsics on x86 and ARM
  * Slide hash implementations using SSE2, AVX2, Neon & VSX
  * Compare256/258 implementations using SSE4.2 & AVX2
  * Inflate chunk copying using SSE2, AVX2 & Neon
  * Support for hardware-accelerated deflate using IBM Z DFLTCC
* Unaligned memory read/writes and large bit buffer improvements
* Includes improvements from Cloudflare and Intel forks
* Configure, CMake, and NMake build system support
* Comprehensive set of CMake unit tests
* Code sanitizers, fuzzing, and coverage
* GitHub Actions continuous integration on Windows, macOS, and Linux
  * Emulated CI for ARM, AARCH64, PPC, PPC64, SPARC64, S390x using qemu


History
-------

The motivation for this fork came after seeing several 3rd party
contributions containing new optimizations not getting implemented
into the official zlib repository.

Mark Adler has been maintaining zlib for a very long time, and he has
done a great job and hopefully he will continue for a long time yet.
The idea of zlib-ng is not to replace zlib, but to co-exist as a
drop-in replacement with a lower threshold for code change.

zlib has a long history and is incredibly portable, even supporting
lots of systems that predate the Internet. This is great, but it does
complicate further development and maintainability.
The zlib code has numerous workarounds for old compilers that do not
understand ANSI-C or to accommodate systems with limitations such as
operating in a 16-bit environment.

Many of these workarounds are only maintenance burdens, some of them
are pretty huge code-wise. For example, the [v]s[n]printf workaround
code has a whopping 8 different implementations just to cater to
various old compilers. With this many workarounds cluttered throughout
the code, new programmers with an idea/interest for zlib will need
to take some time to figure out why all of these seemingly strange
things are used, and how to work within those confines.

So I decided to make a fork, merge all the Intel optimizations, merge
the Cloudflare optimizations that did not conflict, plus a couple
of other smaller patches. Then I started cleaning out workarounds,
various dead code, all contrib and example code as there is little
point in having those in this fork for various reasons.

A lot of improvements have gone into zlib-ng since its start, and
numerous people and companies have contributed both small and big
improvements, or valuable testing.

Please read LICENSE.md, it is very simple and very liberal.


Build
-----

There are two ways to build zlib-ng:

### Cmake

To build zlib-ng using the cross-platform makefile generator cmake.

```
cmake .
cmake --build . --config Release
ctest --verbose -C Release
```

Alternatively, you can use the cmake configuration GUI tool ccmake:

```
ccmake .
```

### Configure

To build zlib-ng using the bash configure script:

```
./configure
make
make test
```

Build Options
-------------

| CMake                    | configure                | Description                                                                           | Default |
|:-------------------------|:-------------------------|:--------------------------------------------------------------------------------------|---------|
| ZLIB_COMPAT              | --zlib-compat            | Compile with zlib compatible API                                                      | OFF     |
| ZLIB_ENABLE_TESTS        |                          | Build test binaries                                                                   | ON      |
| WITH_GZFILEOP            | --without-gzfileops      | Compile with support for gzFile related functions                                     | ON      |
| WITH_OPTIM               | --without-optimizations  | Build with optimisations                                                              | ON      |
| WITH_NEW_STRATEGIES      | --without-new-strategies | Use new strategies                                                                    | ON      |
| WITH_NATIVE_INSTRUCTIONS | --native                 | Compiles with full instruction set supported on this host (gcc/clang -march=native)   | OFF     |
| WITH_SANITIZER           | --with-sanitizer         | Build with sanitizer (memory, address, undefined)                                     | OFF     |
| WITH_FUZZERS             | --with-fuzzers           | Build test/fuzz                                                                       | OFF     |
| WITH_MAINTAINER_WARNINGS |                          | Build with project maintainer warnings                                                | OFF     |
| WITH_CODE_COVERAGE       |                          | Enable code coverage reporting                                                        | OFF     |


Install
-------

WARNING: We do not recommend manually installing unless you really
know what you are doing, because this can potentially override the system
default zlib library, and any incompatibility or wrong configuration of
zlib-ng can make the whole system unusable, requiring recovery or reinstall.
If you still want a manual install, we recommend using the /opt/ path prefix.

For Linux distros, an alternative way to use zlib-ng (if compiled in
zlib-compat mode) instead of zlib, is through the use of the
_LD_PRELOAD_ environment variable. If the program is dynamically linked
with zlib, then zlib-ng will temporarily be used instead by the program,
without risking system-wide instability.

```
LD_PRELOAD=/opt/zlib-ng/libz.so.1.2.11.zlib-ng /usr/bin/program
```

### Cmake

To install zlib-ng system-wide using cmake:

```
cmake --build . --target install
```

### Configure

To install zlib-ng system-wide using the configure script:

```
make install
```

Contributing
------------

Zlib-ng is a aiming to be open to contributions, and we would be delighted to
receive pull requests on github.
Just remember that any code you submit must be your own and it must be zlib licensed.
Help with testing and reviewing of pull requests etc is also very much appreciated.

If you are interested in contributing, please consider joining our
IRC channel #zlib-ng on the Freenode IRC network.


Acknowledgments
----------------

Thanks to Servebolt.com for sponsoring my maintainership of zlib-ng.

Thanks go out to all the people and companies who have taken the time to contribute
code reviews, testing and/or patches. Zlib-ng would not have been nearly as good without you.

The deflate format used by zlib was defined by Phil Katz.
The deflate and zlib specifications were written by L. Peter Deutsch.

zlib was originally created by Jean-loup Gailly (compression)
and Mark Adler (decompression).


Advanced Build Options
----------------------

| CMake                           | configure             | Description                                                         | Default                |
|:--------------------------------|:----------------------|:--------------------------------------------------------------------|------------------------|
| ZLIB_DUAL_LINK                  |                       | Dual link tests with system zlib                                    | OFF                    |
| UNALIGNED_OK                    |                       | Allow unaligned reads                                               | ON (x86, arm)          |
|                                 | --force-sse2          | Skip runtime check for SSE2 instructions (Always on for x86_64)     | OFF (x86)              |
| WITH_AVX2                       |                       | Build with AVX2 intrinsics                                          | ON                     |
| WITH_SSE2                       |                       | Build with SSE2 intrinsics                                          | ON                     |
| WITH_SSE4                       |                       | Build with SSE4 intrinsics                                          | ON                     |
| WITH_PCLMULQDQ                  |                       | Build with PCLMULQDQ intrinsics                                     | ON                     |
| WITH_ACLE                       | --without-acle        | Build with ACLE intrinsics                                          | ON                     |
| WITH_NEON                       | --without-neon        | Build with NEON intrinsics                                          | ON                     |
| WITH_POWER8                     |                       | Build with POWER8 optimisations                                     | ON                     |
| WITH_DFLTCC_DEFLATE             | --with-dfltcc-deflate | Build with DFLTCC intrinsics for compression on IBM Z               | OFF                    |
| WITH_DFLTCC_INFLATE             | --with-dfltcc-inflate | Build with DFLTCC intrinsics for decompression on IBM Z             | OFF                    |
| WITH_UNALIGNED                  |                       | Allow optimizations that use unaligned reads if safe on current arch| ON                     |
| WITH_INFLATE_STRICT             |                       | Build with strict inflate distance checking                         | OFF                    |
| WITH_INFLATE_ALLOW_INVALID_DIST |                       | Build with zero fill for inflate invalid distances                  | OFF                    |
| INSTALL_UTILS                   |                       | Copy minigzip and minideflate during install                        | OFF                    |


Related Projects
----------------

* Fork of the popular minigzip                  https://github.com/zlib-ng/minizip-ng
* Python tool to benchmark minigzip/minideflate https://github.com/zlib-ng/deflatebench
* Python tool to benchmark pigz                 https://github.com/zlib-ng/pigzbench
* 3rd party patches for zlib-ng compatibility   https://github.com/zlib-ng/patches

