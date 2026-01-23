#!/bin/sh
# SPDX-License-Identifier: 0BSD

###############################################################################
#
# Author: Jia Tan
#
###############################################################################

# Optional argument:
# $1 = directory of the xz executable

# If xz was not built, skip this test. Autotools and CMake put
# the xz executable in a different location.
XZ=${1:-../src/xz}/xz
if test ! -x "$XZ"; then
	echo "xz was not built, skipping this test."
	exit 77
fi

# If compression or decompression support is missing, this test is skipped.
# This isn't perfect because it does not specifically check for LZMA1/2
# filters. Many of the other tests also assume LZMA1/2 support if encoders
# or decoders are enabled.
if test ! -f ../config.h ; then
	:
elif grep 'define HAVE_ENCODERS' ../config.h > /dev/null \
		&& grep 'define HAVE_DECODERS' ../config.h > /dev/null ; then
	:
else
	echo "Compression or decompression support is disabled, skipping this test."
	exit 77
fi

# Create temporary input file. The file contents are not important.
SUFFIX_INPUT="suffix_temp"
SUFFIX_INPUT_FILES="$SUFFIX_INPUT"_files
SUFFIX_INPUT_FILES0="$SUFFIX_INPUT"_files0

# Remove possible leftover temporary files
rm -f \
	"$SUFFIX_INPUT" \
	"$SUFFIX_INPUT.foo" \
	"$SUFFIX_INPUT_FILES" \
	"$SUFFIX_INPUT_FILES"

echo "foobar" > "$SUFFIX_INPUT"

# Test basic suffix when compressing with raw format.
if "$XZ" -zfk --suffix=".foo" -Fraw --lzma1=preset=0 "$SUFFIX_INPUT" ; then
	:
else
	echo "Failed to compress a file with a suffix set in raw format"
	exit 1
fi

# Test the output file is named properly.
if test -f "$SUFFIX_INPUT.foo" ; then
	:
else
	echo "Raw format compressed output file not named properly"
	exit 1
fi

# Expect an error when compressing with raw format without a suffix
if "$XZ" -zfk -Fraw --lzma1=preset=0 "$SUFFIX_INPUT" 2> /dev/null; then
	echo "Error not reported when compressing in raw format without a suffix"
	exit 1
fi

# Expect an error when decompressing with raw format without a suffix
if "$XZ" -df -Fraw --lzma1=preset=0 "$SUFFIX_INPUT.foo" 2> /dev/null; then
	echo "Error not reported when decompressing in raw format without a suffix"
	exit 1
fi

# Test basic decompression with raw format and a suffix. This will also
# delete $SUFFIX_INPUT.foo
if "$XZ" -df --suffix=".foo" -Fraw --lzma1=preset=0 "$SUFFIX_INPUT.foo"; then
	:
else
	echo "Failed to decompress a file with a suffix set in raw format"
	exit 1
fi

# Test basic compression with .xz format and a suffix
if "$XZ" -zfk --suffix=".foo" --lzma2=preset=0 "$SUFFIX_INPUT" ; then
	:
else
	echo "Failed to compress a file with a suffix set in .xz format"
	exit 1
fi

# Test the output file is named properly.
if test -f "$SUFFIX_INPUT.foo" ; then
	:
else
	echo ".xz format compressed output file named properly"
	exit 1
fi

# This will delete $SUFFIX_INPUT.foo
if "$XZ" -df --suffix=".foo" "$SUFFIX_INPUT.foo"; then
	:
else
	echo "Failed to decompress a file with a suffix set in .xz format"
	exit 1
fi

# Test reading from stdin in raw mode. This was broken in
# cc5aa9ab138beeecaee5a1e81197591893ee9ca0 and fixed in
# 837ea40b1c9d4998cac4500b55171bf33e0c31a6
if echo foo | "$XZ" -Fraw --lzma1=preset=0 > /dev/null ; then
	:
else
	echo "Implicit write to stdout not detected"
	exit 1
fi

# Create two temporary files to be used with --files and --files0.
printf '%s\n' "$SUFFIX_INPUT" > "$SUFFIX_INPUT_FILES"
printf '%s\0' "$SUFFIX_INPUT" > "$SUFFIX_INPUT_FILES0"

# Test proper handling of --files/--files0 when no suffix is set. This
# must result in an error because xz does not know how to rename the output
# file from the input files. This caused a segmentation fault due to a
# mistake in f481523baac946fa3bc13d79186ffaf0c0b818a7, which was fixed by
# 0a601ddc89fd7e1370807c6b58964f361dfcd34a.
if "$XZ" -Fraw --lzma1=preset=0 --files="$SUFFIX_INPUT_FILES" 2> /dev/null ; then
	echo "Failed to report error when compressing a file specified by --files in raw mode without a suffix"
	exit 1
fi

if "$XZ" -Fraw --lzma1=preset=0 --files0="$SUFFIX_INPUT_FILES0" 2> /dev/null ; then
	echo "Failed to report error when compressing a file specified by --files0 in raw mode without a suffix"
	exit 1
fi

# Test proper suffix usage in raw mode with --files and --files0.
if "$XZ" -zfk -Fraw --lzma1=preset=0 --suffix=.foo --files="$SUFFIX_INPUT_FILES" ; then
	:
else
	echo "Error compressing a file specified by --files in raw mode with a suffix set"
	exit 1
fi

if test -f "$SUFFIX_INPUT.foo" ; then
	:
else
	echo "Entry processed by --files not named properly"
	exit 1
fi

# Remove the artifact so we can be sure the next test executes properly.
rm "$SUFFIX_INPUT.foo"

if "$XZ" -zfk -Fraw --lzma1=preset=0 --suffix=.foo --files0="$SUFFIX_INPUT_FILES0" ; then
	:
else
	echo "Error compressing a file specified by --files0 in raw mode with a suffix set"
	exit 1
fi

if test -f "$SUFFIX_INPUT.foo" ; then
	:
else
	echo "Entry processed by --files0 not named properly"
	exit 1
fi

# When the file type cannot be determined by xz, it will copy the contents
# of the file only if -c,--stdout is used. This was broken by
# 837ea40b1c9d4998cac4500b55171bf33e0c31a6 and fixed by
# f481523baac946fa3bc13d79186ffaf0c0b818a7.
if echo foo | "$XZ" -df > /dev/null 2>&1; then
	echo "Failed to report error when decompressing unknown file type without -c,--stdout"
	exit 1
fi

if echo foo | "$XZ" -dfc > /dev/null; then
	:
else
	echo "Failed to copy input to standard out when decompressing unknown file type with -c,--stdout"
	exit 1
fi

# Remove remaining test artifacts
rm -f \
	"$SUFFIX_INPUT" \
	"$SUFFIX_INPUT.foo" \
	"$SUFFIX_INPUT_FILES" \
	"$SUFFIX_INPUT_FILES0"
