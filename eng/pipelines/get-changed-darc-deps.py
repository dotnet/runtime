#
# Emits a comma separated list of dependencies from `eng/Version.Details.xml`
# that changed as compared to another versions file
#
# - we don't really care which is old, and which is new
# - A dependency name is emitted as changed if:
#   1. version, or sha changed
#   2. it is missing from one of the xmls

import xml.etree.ElementTree as ET
import sys
from os.path import exists

def getDependencies(xmlfile):
    tree = ET.parse(xmlfile)
    root = tree.getroot()
    deps = {}
    for depElement in root.findall('.//Dependency'):
        dep = {}
        dep['Version'] = depElement.attrib['Version']
        dep['Sha'] = depElement.find('Sha').text

        deps[depElement.attrib['Name']] = dep

    return deps

def compare(dict1, dict2):
    if dict1 is None or dict2 is None:
        print('Nones')
        return False

    if (not isinstance(dict1, dict)) or (not isinstance(dict2, dict)):
        print('Not dict')
        return False

    changed_names = []
    all_keys = set(dict1.keys()) | set(dict2.keys())
    for key in all_keys:
        if key not in dict1 or key not in dict2:
            print(key)
            # changed_names.append(key)
        elif dict1[key] != dict2[key]:
            print(key)
            # changed_names.append(key)

    print(','.join(changed_names))

if len(sys.argv) != 3:
    print(f'Usage: {sys.argv[0]} <old Version.Details.xml> <new Version.Details.xml>')
    exit(1)

if not exists(sys.argv[1]):
    print(f'Cannot find {sys.argv[1]}')
    exit(1)
if not exists(sys.argv[2]):
    print(f'Cannot find {sys.argv[2]}')
    exit(1)

newDeps = getDependencies(sys.argv[1])
oldDeps = getDependencies(sys.argv[2])

compare(oldDeps, newDeps)
