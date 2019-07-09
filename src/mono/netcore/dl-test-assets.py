#!/usr/bin/env python3

import sys
import os
import subprocess
import xml.etree.ElementTree as ET
import zipfile
import urllib.request, urllib.parse, urllib.error
import multiprocessing

if len(sys.argv) < 4:
    print("Usage: dl-test-assets.py <path to assets.xml> <base url> <output dir>")
    sys.exit(1)

infile_name = sys.argv [1]
base_url = sys.argv [2]
outdir = sys.argv [3]
tree = ET.parse(infile_name)
root = tree.getroot()

if not os.path.exists(outdir):
    os.makedirs(outdir)

def downloadAsset(elem):
    if elem.tag != "Blob":
        return
    id = elem.attrib ["Id"]
    filename = os.path.basename(id)
    print("Downloading " + filename)
    try:
        name, hdrs = urllib.request.urlretrieve(base_url + "/" + id, outdir + "/" + filename)
    except IOError as e:
        print("Download failed for " + id)
        sys.exit (1)
    print("Extracting " + filename)
    with zipfile.ZipFile(outdir + "/" + filename) as zf:
        zf.extractall(outdir + "/extracted/" + filename[:-4])

pool = multiprocessing.Pool(multiprocessing.cpu_count())
results = pool.map(downloadAsset, root)
pool.close()
pool.join()
