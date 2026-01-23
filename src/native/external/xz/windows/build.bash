#!/bin/bash
# SPDX-License-Identifier: 0BSD

###############################################################################
#
# This build a XZ Utils binary package using the GNU Autotools build system
#
# NOTE: This requires files that are generated as part of "make mydist".
#       So if building from xz.git, create a distribution tarball first,
#       extract it, and run this script from there.
#
# These were tested and known to work:
#   - Cross-compilation with MinGW-w64 v10.0.0 and GCC 12.2.0 from
#     GNU/Linux ("make check" will be skipped)
#   - MSYS2 with MinGW-w64 and GCC
#   - MSYS 1.0.11 (from 2009) with MinGW-w64 v11.0.0 and GCC 13.1.0
#
# Optionally, 7-Zip is used to create the final .zip and .7z packages.
# If the 7z tool is in PATH or if you have installed it in the default
# directory on Windows, this script should find it automatically.
#
# Before running this script, copy COPYING.MinGW-w64-runtime.txt to
# the 'windows' directory.
#
# NOTE: MinGW-w64 includes getopt_long(). The GNU getopt_long() (LGPLv2.1)
#       included in XZ Utils isn't used when building with MinGW-w64.
#
###############################################################################
#
# Author: Lasse Collin
#
###############################################################################

# Abort immediately if something goes wrong.
set -e

# White spaces in directory names may break things so catch them immediately.
case $(pwd) in
	' ' | '	' | '
') echo "Error: White space in the directory name" >&2; exit 1 ;;
esac

# This script can be run either at the top-level directory of the package
# or in the same directory containing this script.
if [ ! -f windows/build.bash ]; then
	cd ..
	if [ ! -f windows/build.bash ]; then
		echo "ERROR: You are in a wrong directory. This script" >&2
		echo "can be run either at the top-level directory of" >&2
		echo "the package or in the same directory containing" >&2
		echo "this script." >&2
		exit 1
	fi
fi

# COPYING.MinGW-w64-runtime.txt needs to be manually copied from MinGW-w64.
if [ ! -f windows/COPYING.MinGW-w64-runtime.txt ]; then
	echo "ERROR: The file 'windows/COPYING.MinGW-w64-runtime.txt'" >&2
	echo "doesn't exists. Copy it from MinGW-w64 so that the" >&2
	echo "copyright and license notices of the MinGW-w64 runtime" >&2
	echo "can be included in the package." >&2
	echo "(Or create an empty file if only doing a test build.)" >&2
	exit 1
fi

# Number of jobs for "make":
MAKE_JOBS=$(nproc 2> /dev/null || echo 1)

# "make check" has to be skipped when cross-compiling.
if [ "x$(uname -o)" = xMsys ]; then
	IS_NATIVE_BUILD=true
else
	IS_NATIVE_BUILD=false
fi

# Run configure and copy the binaries to the given directory.
#
# The first argument is the directory where to copy the binaries.
# The rest of the arguments are passed to configure.
buildit()
{
	DESTDIR=$1
	TRIPLET=$2
	CFLAGS=$3

	# In the MinGW-w64 + GCC toolchains running natively on Windows,
	# $TRIPLET-windres and $TRIPLET-strip commands might not exist.
	# Only the short names "windres" and "strip" might be available.
	# If both i686 and x86_64 toolchains are in PATH, wrong windres.exe
	# will be used for one of the builds, making the build fail. The
	# workaround is to put the directory of $TRIPLET-gcc to the front
	# of PATH if $TRIPLET-windres or $TRIPLET-strip is missing.
	OLD_PATH=$PATH
	if type -P "$TRIPLET-windres" > /dev/null \
			&& type -P "$TRIPLET-strip" > /dev/null; then
		STRIP=$TRIPLET-strip
	else
		STRIP=strip
		GCC_DIR=$(type -P "$TRIPLET-gcc")
		PATH=${GCC_DIR%/*}:$PATH
	fi

	# Clean up if it was already configured.
	[ -f Makefile ] && make distclean

	# Build the size-optimized binaries. Providing size-optimized liblzma
	# could be considered but I don't know if it should only use -Os or
	# should it also use --enable-small and if it should support
	# threading. So I don't include a size-optimized liblzma for now.
	./configure \
		--prefix= \
		--enable-silent-rules \
		--disable-dependency-tracking \
		--disable-nls \
		--disable-scripts \
		--disable-threads \
		--disable-shared \
		--enable-small \
		--host="$TRIPLET" \
		CFLAGS="$CFLAGS -Os"
	make -j"$MAKE_JOBS"

	if "$IS_NATIVE_BUILD"; then
		make -j"$MAKE_JOBS" check
	fi

	mkdir -pv "$DESTDIR"
	cp -v src/xzdec/{xz,lzma}dec.exe src/lzmainfo/lzmainfo.exe "$DESTDIR"

	make distclean

	# Build the normal speed-optimized binaries. The type of threading
	# (win95 vs. vista) will be autodetect from the target architecture.
	./configure \
		--prefix= \
		--enable-silent-rules \
		--disable-dependency-tracking \
		--disable-nls \
		--disable-scripts \
		--host="$TRIPLET" \
		CFLAGS="$CFLAGS -O2"
	make -j"$MAKE_JOBS" -C src/liblzma
	make -j"$MAKE_JOBS" -C src/xz LDFLAGS=-static

	if "$IS_NATIVE_BUILD"; then
		make -j"$MAKE_JOBS" -C tests check
	fi

	cp -v src/xz/xz.exe "$DESTDIR"
	cp -v src/liblzma/.libs/liblzma-5.dll "$DESTDIR/liblzma.dll"
	"$STRIP" -v "$DESTDIR/"*.{exe,dll}

	PATH=$OLD_PATH
}

# Copy files and convert newlines from LF to CR+LF. Optionally add a suffix
# to the destination filename.
#
# The first argument is the destination directory. The second argument is
# the suffix to append to the filenames; use empty string if no extra suffix
# is wanted. The rest of the arguments are the actual filenames.
txtcp()
{
	DESTDIR=$1
	SUFFIX=$2
	shift 2
	for SRCFILE; do
		DESTFILE="$DESTDIR/${SRCFILE##*/}$SUFFIX"
		echo "Converting '$SRCFILE' -> '$DESTFILE'"
		sed s/\$/$'\r'/ < "$SRCFILE" > "$DESTFILE"
	done
}

if type -P i686-w64-mingw32-gcc > /dev/null; then
	# 32-bit x86, Win2k or later if using MSVCRT
	#
	# Uncomment if using MSVCRT and you want the binaries to be compatible
	# with old Windows versions on old computers.
	#buildit pkg/bin_i686 i686-w64-mingw32 '-march=i686 -mtune=generic'

	# 32-bit x86 with SSE2 (Win2k or later if using MSVCRT)
	buildit pkg/bin_i686-sse2 i686-w64-mingw32 \
			'-march=i686 -msse2 -mtune=generic'
else
	echo
	echo "i686-w64-mingw32-gcc is not in PATH, skipping 32-bit x86 builds"
	echo
fi

if type -P x86_64-w64-mingw32-gcc > /dev/null; then
	# x86-64, Windows Vista or later
	buildit pkg/bin_x86-64 x86_64-w64-mingw32 \
			'-march=x86-64 -mtune=generic'
else
	echo
	echo "x86_64-w64-mingw32-gcc is not in PATH, skipping x86-64 build"
	echo
fi

if type -P ps2pdf > /dev/null; then
	make pdf
fi

# Copy the headers, the .def file, and the docs.
# They are the same for all architectures and builds.
mkdir -pv pkg/{include/lzma,doc/{manuals,examples}}
txtcp pkg/include "" src/liblzma/api/lzma.h
txtcp pkg/include/lzma "" src/liblzma/api/lzma/*.h
txtcp pkg/doc "" src/liblzma/liblzma.def
txtcp pkg/doc .txt AUTHORS COPYING COPYING.0BSD NEWS README THANKS
txtcp pkg/doc "" doc/*-file-format.txt \
	windows/README-Windows.txt \
	windows/liblzma-crt-mixing.txt \
	windows/COPYING.MinGW-w64-runtime.txt
txtcp pkg/doc/manuals "" doc/man/txt/{xz,xzdec,lzmainfo}.txt
if [ -d doc/man/pdf-a4 ]; then
	cp -v doc/man/pdf-*/{xz,xzdec,lzmainfo}-*.pdf pkg/doc/manuals
fi
# cp -rv doc/api pkg/doc/api
txtcp pkg/doc/examples "" doc/examples/*

# Create the package. This requires 7z from 7-Zip.
# If it isn't found, this step is skipped.
for SEVENZ in "$(type -P 7z || true)" \
		"$PROGRAMW6432/7-Zip/7z.exe" "$PROGRAMFILES/7-Zip/7z.exe" \
		"/c/Program Files/7-Zip/7z.exe"
do
	[ -x "$SEVENZ" ] && break
done

VER=$(sh build-aux/version.sh)
if [ -x "$SEVENZ" ]; then
	cd pkg
	"$SEVENZ" a -tzip "../xz-$VER-windows.zip" *
	"$SEVENZ" a "../xz-$VER-windows.7z" *
else
	echo
	echo "NOTE: 7z was not found. xz-$VER-windows.zip"
	echo "      and xz-$VER-windows.7z were not created."
	echo "      You can create them yourself from the pkg directory."
fi

echo
echo "Build completed successfully."
echo
