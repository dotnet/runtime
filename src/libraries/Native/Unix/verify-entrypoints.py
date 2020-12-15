#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.


from glob import glob
import sys
import re
import subprocess
import argparse

if __name__ == "__main__":
    from sys import stdout, stderr

    parser = argparse.ArgumentParser(description="Check if exports from Arg1 match entries in Arg2.")
    parser.add_argument('dll', help="so/dylib binary")
    parser.add_argument('entries', help="entrypoints.c source")

    args = parser.parse_args()
    dllEntries = subprocess.check_output(['nm', args.dll])

    # match name in "000000000000ce50 T CryptoNative_X509StackAddMultiple"
    #            or "000000000000ce50 T _CryptoNative_X509StackAddMultiple"
    exportPatternStr = r'(?:\sT\s_*)(\S*)'
    exportPattern = re.compile(exportPatternStr)

    dllList = re.findall(exportPattern, dllEntries)

    # match name in "DllImportEntry(CryptoNative_BioRead)"
    entriesPatternStr = r'(?:DllImportEntry\()(\S*)\)'
    entriesPattern = re.compile(entriesPatternStr)

    with open(args.entries, 'r') as f:
        entriesList = re.findall(entriesPattern, f.read())

    dllSet = set(dllList)
    entriesSet = set(entriesList)

    # ignore well-known dll exports
    dllSet.discard('init')
    dllSet.discard('fini')

    diff = dllSet ^ entriesSet

    if len(diff) > 0:
        stderr.write("ERROR: entrypoints.c file did not match entries exported from " + args.dll + '\n')
        stderr.write("DIFFERENCES FOUND: ")
        stderr.write(', '.join(diff) + '\n')
        exit(1)
