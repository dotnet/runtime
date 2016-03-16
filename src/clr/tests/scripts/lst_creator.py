#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
#
##
# Title               :lst_creator.py
#
# Script to create a working list file from the test overlay directory. This 
# will be used by smarty to run tests.
#
################################################################################

import os
import os.path
import sys

################################################################################
# Globals
################################################################################

g_debug = False
g_name = ""
g_old_list_file = ""

def print_debug(str):
    if g_debug is True:
        print str

################################################################################
# Requires the OSS test overlay directory to be built and passed.
#
# find_tests
################################################################################

def find_tests(base_dir, cat = None, dir = None):
    # Begin walking this folder recursively.
    # Look for any file that ends with .cmd
    # we will be returning a list of these files.

    subdir_list = []
    cmd_list = []

    def traverse_dir(dir, cat, cat_dir, add_cat = False):
        if os.path.basename(dir) == cat_dir:
            add_cat = True

        dir = os.path.abspath(dir)

        for filename in os.listdir(dir):
            print_debug(filename)

            filename = os.path.join(dir, filename)
            print_debug("Full Path: " + filename)

            if os.path.isfile(filename) and filename.split(".")[-1] == "cmd":
                if add_cat is True:
                    cmd_list.append((filename, cat))
                else:
                    cmd_list.append((filename, g_name))

            elif os.path.isdir(filename):
                traverse_dir(filename, cat, cat_dir, add_cat)

    traverse_dir(base_dir, cat, dir)

    return cmd_list

################################################################################
# Main
################################################################################

if __name__ == "__main__":
    print "Starting lst_creator"
    print "- - - - - - - - - - - - - - - - - - - - - - - - - - - -"
    print

    if len(sys.argv) < 4:
        print "Error, incorrect number of arguments."
        print "Ex usage: python lst_creator <root_oss_test_dir> <lst file name> <optional cat name> <old list file location>"
        print "Tests must be built!"
        exit(1)

    if not os.path.isdir(sys.argv[1]):
        print "Error argument passed is not a valid directory."
        exit(1)

    g_name = sys.argv[3]

    cat, dirname = None, None
    
    if len(sys.argv) > 4:
        if sys.argv[4] == "-D":
            cat = sys.argv[5]
            dirname = sys.argv[6]

        elif sys.argv[4] != "-D":
            g_old_list_file = sys.argv[4]

            if not os.path.isfile(g_old_list_file):

                print "Error, old list file must be valid."
                exit(1)

    cmd_list = find_tests(sys.argv[1], cat, dirname)

    print "Found " + str(len(cmd_list)) + " tests to add."
    print

    if g_old_list_file is not "":
        print "Updating the old list file"

    else:
        print "Creating the lst file."

    unique_output = dict()
    largest_value = 0

    # If there was an old list file passed. Parse it for all the old tests.

    if g_old_list_file is not "":
        old_list_file_lines = []

        with open(sys.argv[4]) as lst_file_handle:

            old_list_file_lines = lst_file_handle.readlines()

        for line in old_list_file_lines:
            split_line = line.split("[")

            # We only need the test names
            # which come in as [ testname_number ]
            if len(split_line) == 1:
                continue

            # This is a test name, start splitting

            split_line = split_line[1].split("]")
            split_line = split_line[0].split("_")

            if largest_value < int(split_line[-1]):
                largest_value = int(split_line[-1])

            test_name = "_".join(split_line[:-1])

            if len(test_name.split("exe")) == 1:
                # Error, name is not an exe.
                print "Error"

                sys.exit(1)

            unique_output[test_name] = True

        print str(len(unique_output)) + " tests found in the old lstFile."

    output = []

    repeat_count = 0
    count = largest_value

    for line in cmd_list:
        path, cat = line[0], line[1]

        # get the relative path
        prefix = os.path.commonprefix([path, sys.argv[1]])
        rel_path = os.path.relpath(path, prefix)

        cmd_contents = None
        with open(path) as cmd_file_handle:
            cmd_contents = cmd_file_handle.readlines()

        expected_exit_code_line = None

        for cmd_line in cmd_contents:
            if cmd_line.find("CLRTestExpectedExitCode") != -1:
                expected_exit_code_line = cmd_line
                break

        if expected_exit_code_line is None:
            print "Error, cmd file missing contents. Skipping, however, the test suite was not built correctly."
            print path
            continue

        expected = expected_exit_code_line[expected_exit_code_line.find("CLRTestExpectedExitCode") + (len("CLRTestExpectedExitCode") + 1):].strip()
        max_allowed_duration = 600
        categories = cat
        build_type = "CoreSys"
        relative_path = rel_path[:rel_path.find("cmd")] + "exe"
        working_dir = os.path.dirname(rel_path)
        test_name = os.path.basename(relative_path)

        try:
            if unique_output[test_name] == True:
                repeat_count += 1

                continue

        except:
            output.append("[" + test_name + "_" + str(count) + "]" + "\n")

            count = count + 1

            output.append("RelativePath=" + os.path.relpath(relative_path) + "\n")
            output.append("WorkingDir=" + os.path.relpath(working_dir) + "\n")
            output.append("Expected=" + expected + "\n")
            output.append("MaxAllowedDurationSeconds=" + str(max_allowed_duration) + "\n")
            output.append("Categories=" + categories + "\n")
            output.append("HostStyle=Any")
            output.append("\n")

    print
    print "Writing out lst file."

    if repeat_count > 0:
        print "Found " + str(repeat_count) + " old tests."
    
        # If we found repeats then we open file to append not write.

        with open(g_old_list_file, 'a') as list_file_handle:
            list_file_handle.write("\n")

            for line in output:
                list_file_handle.write(line)


    else:
        with open(sys.argv[2], 'w') as list_file_handle:

            list_file_handle.write("##=== Test Definitions ===============================\n")

            for line in output:
                list_file_handle.write(line)
