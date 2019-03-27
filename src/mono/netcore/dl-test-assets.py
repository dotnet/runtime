#!/usr/bin/env python

import sys
import subprocess
import xml.etree.ElementTree as ET

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
    print elem.attrib ["Id"]
    res = subprocess.call (["wget", "-N", "-P", outdir, base_url + "/" + elem.attrib ["Id"]])
    if res != 0:
        print ("Download failed.")
        sys.exit (1)

