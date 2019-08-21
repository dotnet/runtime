#!/usr/bin/env python3

import xml.etree.ElementTree as ET
import os
import glob
import ntpath
import sys

if len(sys.argv) < 1:
    print("Usage: xunit-summary.py <path to xunit results (*.xml)>")
    sys.exit(1)
test_dir = sys.argv [1]

class TestResults():
    def __init__(self, name, total, passed, failed, skipped, errors, time):
        self.name = name
        self.total = total
        self.passed = passed
        self.failed = failed + errors
        self.skipped = skipped
        self.time = time

print("")

tests = []
for testfile in glob.glob(test_dir + "/*-xunit.xml"):
    assemblies = ET.parse(testfile).getroot()
    for assembly in assemblies:
        test_name = assembly.attrib.get("name")
        if test_name is None:
            print("WARNING: %s has no tests!" % ntpath.basename(testfile))
            continue
        tests.append(TestResults(test_name, 
            int(assembly.attrib["total"]), 
            int(assembly.attrib["passed"]), 
            int(assembly.attrib["failed"]), 
            int(assembly.attrib["skipped"]), 
            int(assembly.attrib["errors"]), 
            float(assembly.attrib["time"])))

# sort by name
tests.sort(key=lambda item: item.name)

print("")
print("=" * 105)
for t in tests:
    #if t.failed > 0: # uncomment to list only test suits with failures
        print("{0:<60}  Total:{1:<6}  Failed:{2:<6}  Time:{3} sec".format(t.name, t.total, t.failed, round(t.time, 1)))
print("=" * 105)

print("")
print("Total test suites:    %d" % len(tests))
print("Total tests run:      %d" % sum(x.total for x in tests))
print("Total tests passed:   %d" % sum(x.passed for x in tests))
print("Total tests failed:   %d" % sum(x.failed for x in tests))
print("Total tests skipped:  %d" % sum(x.skipped for x in tests))
print("Total duration:       %d min" % (sum(x.time for x in tests) / 60))
print("")

