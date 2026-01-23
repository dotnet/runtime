#!/bin/sh
# SPDX-License-Identifier: 0BSD

###############################################################################
#
# Look for missing license info in xz.git
#
# The project doesn't conform to the FSFE REUSE specification for now.
# Instead, this script helps in finding files that lack license info.
# Pass -v as an argument to get license info from all files in xz.git or,
# when .git isn't available, from files extracted from a release tarball
# (in case of a release tarball, the tree must be clean of any extra files).
#
# NOTE: This relies on non-POSIX xargs -0. It's supported on GNU and *BSDs.
#
###############################################################################
#
# Author: Lasse Collin
#
###############################################################################

# Print good files too if -v is passed as an argument.
VERBOSE=false
case $1 in
	'')
		;;
	-v)
		VERBOSE=true
		;;
	*)
		echo "Usage: $0 [-v]"
		exit 1
		;;
esac


# Use the C locale so that sorting is always the same.
LC_ALL=C
export LC_ALL


# String to match the SPDX license identifier tag.
# Spell it here in a way that doesn't match regular grep patterns.
SPDX_LI='SPDX''-License-''Identifier'':'

# Pattern for files that don't contain SPDX tags but they are under
# a free license that isn't 0BSD.
PAT_UNTAGGED_MISC='^COPYING\.
^INSTALL\.generic$'

# Pattern for files that are 0BSD but don't contain SPDX tags.
# (The two file format specification files are public domain but
# they can be treated as 0BSD too.)
PAT_UNTAGGED_0BSD='^(.*/)?\.gitattributes$
^(.*/)?\.gitignore$
^\.github/SECURITY\.md$
^AUTHORS$
^COPYING$
^ChangeLog$
^INSTALL$
^NEWS$
^PACKAGERS$
^(.*/)?README$
^THANKS$
^TODO$
^(.*/)?[^/]+\.txt$
^doc/SHA256SUMS$
^po/LINGUAS$
^src/common/w32_application\.manifest$
^tests/xzgrep_expected_output$
^tests/files/[^/]+\.(lz|lzma|xz)$'

# Pattern for files that must be ignored when Git isn't available. This is
# useful when this script is run right after extracting a release tarball.
PAT_TARBALL_IGNORE='^(m4/)?[^/]*\.m4$
^(.*/)?Makefile\.in(\.in)?$
^(po|po4a)/.*[^.]..$
^ABOUT-NLS$
^build-aux/(config\..*|ltmain\.sh|[^.]*)$
^config\.h\.in$
^configure$'


# Go to the top source dir.
cd "$(dirname "$0")/.." || exit 1

# Get the list of files to check from git if possible.
# Otherwise list the whole source tree. This script should pass
# if it is run right after extracting a release tarball.
if test -d .git && type git > /dev/null 2>&1; then
	FILES=$(git ls-files) || exit 1
	IS_TARBALL=false
else
	FILES=$(find . -type f) || exit 1
	FILES=$(printf '%s\n' "$FILES" | sed 's,^\./,,')
	IS_TARBALL=true
fi

# Sort to keep the order consistent.
FILES=$(printf '%s\n' "$FILES" | sort)


# Find the tagged files.
TAGGED=$(printf '%s\n' "$FILES" \
	| tr '\n' '\000' | xargs -0r grep -l "$SPDX_LI" --)

# Find the tagged 0BSD files.
TAGGED_0BSD=$(printf '%s\n' "$TAGGED" \
	| tr '\n' '\000' | xargs -0r grep -l "$SPDX_LI 0BSD" --)

# Find the tagged non-0BSD files, that is, remove the 0BSD-tagged files
# from the list of tagged files.
TAGGED_MISC=$(printf '%s\n%s\n' "$TAGGED" "$TAGGED_0BSD" | sort | uniq -u)


# Remove the tagged files from the list.
FILES=$(printf '%s\n%s\n' "$FILES" "$TAGGED" | sort | uniq -u)

# Find the intentionally-untagged files.
UNTAGGED_0BSD=$(printf '%s\n' "$FILES" | grep -E "$PAT_UNTAGGED_0BSD")
UNTAGGED_MISC=$(printf '%s\n' "$FILES" | grep -E "$PAT_UNTAGGED_MISC")

# Remove the intentionally-untagged files from the list.
FILES=$(printf '%s\n' "$FILES" | grep -Ev \
	-e "$PAT_UNTAGGED_0BSD" -e "$PAT_UNTAGGED_MISC")


# FIXME: Allow untagged translations if they have a public domain notice.
# These are old translations that haven't been updated after 2024-02-14.
# Eventually these should go away.
PD_PO=$(printf '%s\n' "$FILES" | grep '\.po$' | tr '\n' '\000' \
	| xargs -0r grep -Fl '# This file is put in the public domain.' --)

if test -n "$PD_PO"; then
	# Remove the public domain .po files from the list.
	FILES=$(printf '%s\n%s\n' "$FILES" "$PD_PO" | sort | uniq -u)
fi


# Remove generated files from the list which don't have SPDX tags but which
# can be present in release tarballs. This step is skipped when the file list
# is from "git ls-files".
GENERATED=
if $IS_TARBALL; then
	GENERATED=$(printf '%s\n' "$FILES" | grep -E "$PAT_TARBALL_IGNORE")
	FILES=$(printf '%s\n' "$FILES" | grep -Ev "$PAT_TARBALL_IGNORE")
fi


if $VERBOSE; then
	printf '# Tagged 0BSD files:\n%s\n\n' "$TAGGED_0BSD"
	printf '# Intentionally untagged 0BSD:\n%s\n\n' "$UNTAGGED_0BSD"

	# FIXME: Remove when no longer needed.
	if test -n "$PD_PO"; then
		printf '# Old public domain translations:\n%s\n\n' "$PD_PO"
	fi

	printf '# Tagged non-0BSD files:\n%s\n\n' "$TAGGED_MISC"
	printf '# Intentionally untagged miscellaneous: \n%s\n\n' \
		"$UNTAGGED_MISC"

	if test -n "$GENERATED"; then
		printf '# Generated files whose license was NOT checked:\n%s\n\n' \
			"$GENERATED"
	fi
fi


# Look for files with an unknown license and set the exit status accordingly.
STATUS=0
if test -n "$FILES"; then
	printf '# ERROR: Licensing is unclear:\n%s\n' "$FILES"
	STATUS=1
fi

exit "$STATUS"
