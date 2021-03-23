#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               :pgocheck.py
#
# A script to check whether or not a particular portable executable
# (e.g. EXE, DLL) was compiled using PGO technology
#
################################################################################

from glob import glob
import sys
import re
import subprocess
import argparse

# This pattern matches the line which specifies if PGO, LTCG, or similar techologies were used for compilation
# coffgrp matches the literal string. It uniquely identifies within the field in question
# (?:\s+[0-9A-F]+){4} matches 4 hex valued fields without capturing them
# \((\S*)\) captures the text identifier from the dump output, letting us know the technology
pgo_pattern_str = r'coffgrp(?:\s+[0-9A-F]+){4}\s+\((\S*)\)'
pgo_pattern = re.compile(pgo_pattern_str)

def was_compiled_with_pgo(filename):
    # When running on Python 3, check_output returns a bytes object, which we need to
    # decode to a string object.
    headers = subprocess.check_output(["link", "/dump", "/headers", filename]).decode('utf-8')

    match = pgo_pattern.search(headers)

    result = False
    tech = "UNKNOWN"
    if match:
        result = match.group(1) == 'PGU'
        tech = match.group(1)

    return result, tech

if __name__ == "__main__":
    from sys import stdout, stderr

    parser = argparse.ArgumentParser(description="Check if the given PE files were compiled with PGO. Fails if the files were not.")
    parser.add_argument('files', metavar='file', nargs='+', help="the files to check for PGO flags")
    parser.add_argument('--negative', action='store_true', help="fail on PGO flags found")
    parser.add_argument('--quiet', action='store_true', help="don't output; just return a code")

    args = parser.parse_args()
    # Divide up filenames which are separated by semicolons as well as the ones by spaces. Avoid duplicates
    filenames = set()
    for token in args.files:
        unexpanded_filenames = token.split(';')
        # Provide support for Unix-style filename expansion (i.e. with * and ?)
        for unexpanded_filename in unexpanded_filenames:
            expanded_filenames = glob(unexpanded_filename)
            if unexpanded_filename and not expanded_filenames:
                stderr.write("ERROR: Could not find file(s) {0}\n".format(unexpanded_filename))
                exit(2)
            filenames.update(expanded_filenames)

    success = True
    for filename in filenames:
        result, tech = was_compiled_with_pgo(filename)
        success = success and result

        if not args.quiet:
            status = "compiled with PGO" if result else "NOT compiled with PGO"
            sys.stdout.write("{0}: {1} ({2})\n".format(filename, status, tech))

    if not success:
        if not args.quiet:
            if not args.negative:
                stderr.write("ERROR: The files listed above must be compiled with PGO\n")
            else:
                stderr.write("ERROR: The files listed above must NOT be compiled with PGO\n")
        exit(1)