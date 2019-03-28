#!/usr/bin/env python

import sys
import os
import subprocess
import xml.etree.ElementTree as ET
import zipfile

if len(sys.argv) < 4:
    print("Usage: dl-test-assets.py <path to assets.xml> <base url> <output dir>")
    sys.exit(1)

infile_name = sys.argv [1]
base_url = sys.argv [2]
outdir = sys.argv [3];
tree = ET.parse(infile_name)
root = tree.getroot()

for elem in root:
    if elem.tag != "Blob":
        continue
    id = elem.attrib ["Id"]
    filename = os.path.basename(id)
    # System.IO.Compression.* have a lot of test files (>100mb)
    if "System.IO.Compression" in filename:
        continue
    print filename
    res = subprocess.call (["wget", "-N", "-P", outdir, base_url + "/" + id])
    if res != 0:
        print ("Download failed for " + id)
        sys.exit (1)
    with zipfile.ZipFile(outdir + "/" + filename) as zf:
        zf.extractall(outdir + "/extracted/" + filename[:-4])
