
XZ Utils for Windows
====================

Introduction
------------

    This package includes command line tools (xz.exe and a few
    others) and the liblzma compression library from XZ Utils.
    You can find the latest version and full source code from
    <https://tukaani.org/xz/>.

    The parts of the XZ Utils source code, that are relevant to this
    binary package, are under the BSD Zero Clause License (0BSD).
    XZ Utils have been built using GCC and MinGW-w64 and linked
    statically against the MinGW-w64 runtime libraries. See
    COPYING.MinGW-w64-runtime.txt for copyright and license
    information that applies to the MinGW-w64 runtime.

        IMPORTANT: You must include COPYING.MinGW-w64-runtime.txt
        when distributing these XZ Utils binaries to meet
        the license terms of the MinGW-w64 runtime!

    (The file COPYING mentions GNU getopt_long. It's *not* used when
    XZ Utils is built with MinGW-w64. Thus GNU LGPLv2.1 doesn't apply.)


Package contents
----------------

    All executables and libraries in this package require
    Universal CRT (UCRT). It is included in Windows 10 and later,
    and it's possible to install UCRT on Windows XP and later.

    There is a SSE2 optimization in the compression code but this
    version of XZ Utils doesn't include run-time processor detection.
    The binaries don't work on 32-bit processors without SSE2 support.

    There is one directory for each type of executable and library files:

        bin_i686-sse2   32-bit x86 (i686 with SSE2)
        bin_x86-64      64-bit x86-64

    Each of the above directories have the following files:

        *.exe         Command line tools. (It's useless to double-click
                      these; use the command prompt instead.) These have
                      been linked statically against liblzma, so they
                      don't require liblzma.dll. Thus, you can copy e.g.
                      xz.exe to a directory that is in PATH without
                      copying any other files from this package.

                      NOTE: xzdec.exe and lzmadec.exe are optimized for
                      size, single-threaded, and slower than xz.exe.
                      Use xz.exe unless program size is important.

        liblzma.dll   Shared version of the liblzma compression library.
                      This file is mostly useful to developers, although
                      some non-developers might use it to upgrade their
                      copy of liblzma.

    The rest of the directories contain architecture-independent files:

        doc           Basic documentation in the plain text (TXT)
                      format. COPYING.txt, COPYING.0BSD.txt, and
                      COPYING.MinGW-w64-runtime.txt contain
                      copyright and license information.
                      liblzma.def is in this directory too.

        doc/manuals   The manuals of the command line tools

        doc/examples  Example programs for basic liblzma usage.

        include       C header files for liblzma. These should be
                      compatible with most C and C++ compilers.


Creating an import library for MSVC / Visual Studio
---------------------------------------------------

    To link against liblzma.dll, you need to create an import library
    first. You need the "lib" command from MSVC and liblzma.def from
    the "doc" directory of this package. Here is the command that works
    on 32-bit x86:

        lib /def:liblzma.def /out:liblzma.lib /machine:ix86

    On x86-64, the /machine argument has to be changed:

        lib /def:liblzma.def /out:liblzma.lib /machine:x64

    IMPORTANT: See also the file liblzma-crt-mixing.txt if your
    application isn't using UCRT.


Reporting bugs
--------------

    Report bugs to <xz@tukaani.org>.

