#!/bin/sh
# SPDX-License-Identifier: 0BSD

#############################################################################
#
# While it's possible to use the Doxyfile as is to generate liblzma API
# documentation, it is recommended to use this script because this adds
# the XZ Utils version number to the generated HTML.
#
# Other features:
#  - Generate documentation of the XZ Utils internals.
#  - Set input and output paths for out-of-tree builds.
#
#############################################################################
#
# Authors: Jia Tan
#          Lasse Collin
#
#############################################################################

set -e

show_usage()
{
	echo "Usage: $0 <api|internal> [ABS_TOP_SRCDIR ABS_OUTDIR]"
	echo
	echo "Supported modes:"
	echo " - 'api' (default): liblzma API docs into doc/api"
	echo " - 'internal': internal docs into doc/internal"
	echo
	echo "Absolute source and output dirs may be set" \
		"to do an out-of-tree build."
	echo "The output directory must already exist."
	exit 1
}

case $1 in
	api|internal)
		;;
	*)
		show_usage
		;;
esac

if type doxygen > /dev/null 2>&1; then
	:
else
	echo "$0: 'doxygen' command not found" >&2
	exit 1
fi

case $# in
	1)
		# One argument: Building inside the source tree
		ABS_TOP_SRCDIR=`dirname "$0"`/..
		ABS_OUTDIR=$ABS_TOP_SRCDIR/doc
		;;
	3)
		# Three arguments: Possibly an out of tree build
		ABS_TOP_SRCDIR=$2
		ABS_OUTDIR=$3
		;;
	*)
		show_usage
		;;
esac

if test ! -f "$ABS_TOP_SRCDIR/doxygen/Doxyfile"; then
	echo "$0: Source dir '$ABS_TOP_SRCDIR/doxygen/Doxyfile' not found" >&2
	exit 1
fi
if test ! -d "$ABS_OUTDIR"; then
	echo "$0: Output dir '$ABS_OUTDIR' not found" >&2
	exit 1
fi

# Get the package version so that it can be included in the generated docs.
PACKAGE_VERSION=`cd "$ABS_TOP_SRCDIR" && sh build-aux/version.sh`

case $1 in
	api)
		# Remove old documentation before re-generating the new.
		rm -rf "$ABS_OUTDIR/api"

		# Generate the HTML documentation by preparing the Doxyfile
		# in stdin and piping the result to the doxygen command.
		# With Doxygen, the last assignment of a value to a tag will
		# override any earlier assignment. So, we can use this
		# feature to override the tags that need to change between
		# "api" and "internal" modes.
		ABS_SRCDIR=$ABS_TOP_SRCDIR/src/liblzma/api
		(
			cat "$ABS_TOP_SRCDIR/doxygen/Doxyfile"
			echo "PROJECT_NUMBER         = $PACKAGE_VERSION"
			echo "OUTPUT_DIRECTORY       = $ABS_OUTDIR"
			echo "STRIP_FROM_PATH        = $ABS_SRCDIR"
			echo "INPUT                  = $ABS_SRCDIR"
		) | doxygen -q -
		;;

	internal)
		rm -rf "$ABS_OUTDIR/internal"
		(
			cat "$ABS_TOP_SRCDIR/doxygen/Doxyfile"
			echo 'PROJECT_NAME           = "XZ Utils"'
			echo "PROJECT_NUMBER         = $PACKAGE_VERSION"
			echo "OUTPUT_DIRECTORY       = $ABS_OUTDIR"
			echo "STRIP_FROM_PATH        = $ABS_TOP_SRCDIR"
			echo "INPUT                  = $ABS_TOP_SRCDIR/src"
			echo 'HTML_OUTPUT            = internal'
			echo 'SEARCHENGINE           = YES'
		) | doxygen -q -
		;;
esac
