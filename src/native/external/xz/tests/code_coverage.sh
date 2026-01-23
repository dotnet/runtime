#!/bin/sh
# SPDX-License-Identifier: 0BSD

###############################################################################
#
# This builds xz with special CFLAGS for measuring code coverage and
# uses lcov and genhtml to create coverage reports.
#
# The current directory is used as the build directory so out-of-tree
# builds are possible. The coverage reports are written to the directory
# "coverage" under the current directory.
#
# Any options passed to this script are passed to "make" so to get
# faster builds use, for example, "-j4" as an argument to this script.
#
# Author: Jia Tan
#
###############################################################################

set -e

COVERAGE_DIR="coverage"

# Test if lcov is installed
if ! command -v lcov > /dev/null
then
	echo "Error: lcov not installed"
	exit 1
fi

# Test if genhtml is installed
if ! command -v genhtml > /dev/null
then
	echo "Error: genhtml not installed"
	exit 1
fi

top_srcdir=$(cd -- "$(dirname -- "$0")" && cd .. && pwd)

# Run the autogen.sh script if the configure script has not been generated
if ! test -f "$top_srcdir/configure"
then
	( cd "$top_srcdir" && ./autogen.sh )
fi

# Execute the configure script if the Makefile is not present
if ! test -f "Makefile"
then
	"$top_srcdir/configure" \
		--disable-xzdec \
		--disable-lzmadec \
		--disable-lzmainfo \
		--disable-shared \
		--enable-silent-rules \
		CFLAGS="$CFLAGS --coverage --no-inline -O0"
fi

# Run the tests
make "$@" check

# Re-create the coverage directory
rm -rf "$COVERAGE_DIR"
mkdir -p "$COVERAGE_DIR/liblzma"
mkdir -p "$COVERAGE_DIR/xz"

# Run lcov with src/liblzma as the input directory and write the
# results out to the coverage directory
lcov -c -d "src/liblzma" -o "$COVERAGE_DIR/liblzma/liblzma.cov"
lcov -c -d "src/xz" -o "$COVERAGE_DIR/xz/xz.cov"

# Generate the reports
genhtml "$COVERAGE_DIR/liblzma/liblzma.cov" -o "$COVERAGE_DIR/liblzma"
genhtml "$COVERAGE_DIR/xz/xz.cov" -o "$COVERAGE_DIR/xz"

echo "Success! See:"
echo "file://$PWD/$COVERAGE_DIR/liblzma/index.html"
echo "file://$PWD/$COVERAGE_DIR/xz/index.html"
