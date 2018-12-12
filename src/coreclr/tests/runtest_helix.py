#!/usr/bin/env python

# This script runs tests in helix. It defines a set of test scenarios
# that enable various combinations of runtime configurations to be set
# via the test process environment.

# This script calls "corerun xunit.console.dll xunitwrapper.dll",
# where the xunitwrapper.dll will run a .sh/.cmd script per test in a
# separate process. This process will have the scenario environment
# variables set, but the xunit process will not.

# TODO: Factor out logic common with runtest.py

import argparse
import subprocess
import os
import sys
import tempfile

test_scenarios = {
    "jitstress2": { "COMPlus_TieredCompilation": "0",
                    "COMPlus_JitStress": "2" },
}

if sys.platform.startswith('linux') or sys.platform == "darwin":
    platform_type = "unix"
elif sys.platform == "win32":
    platform_type = "windows"
else:
    print("unknown os: %s" % sys.platform)
    sys.exit(1)

def get_testenv_script(env_dict):
    if platform_type == "unix":
        return ''.join([ "export %s=%s%s" % (k, v, os.linesep) for k, v in env_dict.items() ])
    elif platform_type == "windows":
        return ''.join([ "set %s=%s%s" % (k, v, os.linesep) for k, v in env_dict.items() ])

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Parse arguments")
    parser.add_argument("-scenario", dest="scenario", default=None)
    parser.add_argument("-wrapper", dest="wrapper", default=None, required=True)
    args = parser.parse_args()
    scenario = args.scenario
    wrapper = args.wrapper

    if not "HELIX_CORRELATION_PAYLOAD" in os.environ:
        print("HELIX_CORRELATION_PAYLOAD must be defined in environment")
        sys.exit(1)

    if not "HELIX_WORKITEM_PAYLOAD" in os.environ:
        print("HELIX_WORKITEM_PAYLOAD must be defined in environment")
        sys.exit(1)

    core_root = os.environ["HELIX_CORRELATION_PAYLOAD"]

    if platform_type == "unix":
        corerun = os.path.join(core_root, "corerun")
    else:
        corerun = os.path.join(core_root, "corerun.exe")

    # Unlike the old test wrapper, this runs xunit.console.dll from
    # the correlation payload. This removes the need for redundant
    # copies of the console runner in each test directory.
    command = [corerun,
               os.path.join(os.environ["HELIX_CORRELATION_PAYLOAD"], "xunit.console.dll"),
               os.path.join(os.environ["HELIX_WORKITEM_PAYLOAD"], wrapper),
               "-noshadow",
               "-xml", "testResults.xml",
               "-notrait", "category=outerloop",
               "-notrait", "category=failing"]

    if scenario is None:
        print("CORE_ROOT=%s" % core_root)
        os.environ["CORE_ROOT"] = core_root

        print("BEGIN EXECUTION")
        print(' '.join(command))
        proc = subprocess.Popen(command)
        proc.communicate()
        print("Finished running tests. Exit code = %d" % proc.returncode)
        sys.exit(proc.returncode)
    else:
        print("scenario: %s" % scenario)
        with tempfile.NamedTemporaryFile(mode="w") as testenv:
            testenv.write(get_testenv_script(test_scenarios[scenario]))
            testenv.flush()

            print("__TestEnv=%s" % testenv.name)
            os.environ["__TestEnv"] = testenv.name

            with open(testenv.name) as testenv_written:
                contents = testenv_written.read()
                print(contents)

            print("CORE_ROOT=%s" % core_root)
            os.environ["CORE_ROOT"] = core_root

            print("BEGIN EXECUTION")
            print(' '.join(command))
            proc = subprocess.Popen(command)
            proc.communicate()
            print("Finished running tests. Exit code = %d" % proc.returncode)
            sys.exit(proc.returncode)
