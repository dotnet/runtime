#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               :lst_creator.py
#
# Script to create a working list file from the test overlay directory. This
# will be used by smarty to run tests.
#
################################################################################

import argparse
import datetime
import os
import re
import sys

from collections import defaultdict

################################################################################
# Argument Parser
################################################################################

DESCRIPTION = """Python script to help create/update the arm64 lstFile
              """

PARSER = argparse.ArgumentParser(description=DESCRIPTION)

PARSER.add_argument("--test", dest="testing", action="store_true", default=False)
PARSER.add_argument("-lst_file", dest="old_list_file", nargs='?', default=None)
PARSER.add_argument("-pri0_test_dir", dest="pri0_test_dir", nargs='?', default=None)
PARSER.add_argument("-pri1_test_dir", dest="pri1_test_dir", nargs='?', default=None)
PARSER.add_argument("-commit_hash", dest="commit_hash", nargs='?', default=None)
PARSER.add_argument("-failures_csv", dest="failures_csv", nargs='?', default=None)
PARSER.add_argument("--unset_new", dest="unset_new", action="store_true", default=False)
ARGS = PARSER.parse_args(sys.argv[1:])

################################################################################
# Helper Functions
################################################################################

def create_list_file(file_name, metadata):
    """ Create a lstFile given a set of metadata input

    Args:
        file_name (str): Location to write the lstFile
        metadata ({ str: { str: str } }): Dictionary mapping test name to
                                        : a tuple, the first tuple's value is
                                        : a dictionary of key/value attributes,
                                        : the second is test index.

    """

    current_time = datetime.datetime.now()
    current_time_str = current_time.strftime("%d-%b-%Y %H:%M:%S%z")

    metadata = [metadata[item] for item in metadata]
    metadata = sorted(metadata, key=lambda item: item[1])

    new_metadata = [item for item in metadata if item[1] == -1]
    old_metadata = [item for item in metadata if item[1] != -1]

    with open(file_name, "w") as file_handle:
        file_handle.write("## This list file has been produced automatically. Any changes\n")
        file_handle.write("## are subject to being overwritten when reproducing this file.\n")
        file_handle.write("## \n")
        file_handle.write("## Last Updated: %s\n" % current_time_str)
        file_handle.write("## Commit: %s\n" % ARGS.commit_hash)
        file_handle.write("## \n")

        order = ["RelativePath", "WorkingDir", "Expected",
                "MaxAllowedDurationSeconds", "Categories", "HostStyle"]

        def write_metadata(data, count=None):
            for item in data:
                test_name = item[0]["RelativePath"]
                if item[1] != -1:
                    count = item[1]

                item = item[0]

                # Get the test name.
                title = "[%s_%d]" % (test_name.split("\\")[-1], count)
                count += 1

                file_handle.write("%s\n" % title)

                attribute_str = ""
                for key in order:
                    attribute_str += "%s=%s\n" % (key, item[key])

                file_handle.write(attribute_str + "\n")

        write_metadata(old_metadata)

        old_number = 0
        try:
            old_number = old_metadata[-1][1] + 1

        except:
            # New lstFile
            pass

        write_metadata(new_metadata, old_number + 1)

def create_metadata(tests):
    """ Given a set of tests create the metadata around them

    Args:
        tests ({str : int}): List of tests for which to determine metadata
                           : int represents the priority

    Returns:
        test_metadata ({ str: { str: str } }): Dictionary mapping test name to
                                             : a dictionary of key/value
                                             : attributes.

    """
    test_metadata = defaultdict(lambda: None)

    failures_csv = ARGS.failures_csv

    failure_information = defaultdict(lambda: None)

    if failures_csv is not None:
        lines = []
        assert(os.path.isfile(failures_csv))

        with open(failures_csv, "r") as file_handle:
            lines = file_handle.readlines()

        try:
            for line in lines:
                split = line.split(",")
                relative_path = split[0].replace("/", "\\")
                category = split[1]

                failure_information[relative_path] = category.strip()
        except:
            raise Exception("Error. CSV format expects: relativepath,category")

    for test in tests:
        test_name = test
        priority = tests[test]

        working_directory = os.path.dirname(test_name).replace("/", "\\")

        # Make sure the tests use the windows \ separator.
        relative_path = test_name.replace("/", "\\")
        max_duration = "600"

        if priority == 0:
            categories = "EXPECTED_PASS"
        else:
            categories = "EXPECTED_PASS;Pri%d" % priority

        expected = "0"
        host_style = "0"

        metadata = defaultdict(lambda: None)
        metadata["RelativePath"] = relative_path
        metadata["WorkingDir"] = working_directory
        metadata["MaxAllowedDurationSeconds"] = max_duration
        metadata["HostStyle"] = host_style
        metadata["Expected"] = expected
        metadata["Categories"] = categories

        if failure_information[relative_path] is not None:
            metadata["Categories"] = failure_information[relative_path]

        test_metadata[relative_path] = metadata

    return test_metadata

def get_all_tests(base_dir):
    """ Find all of the tests in the enlistment

    Args:
        base_dir (str): Directory to start traversing from

    Returns:
        test_list ([str]): List of the tests. Note this is defined to be every
                         : cmd file under the base_dir.

    Note:
        To find the tests correctly you must build the tests correctly and
        pass that directory. This method will NOT check to make sure that
        this has been done correctly.

        This is a recursive method.

    """

    def get_all_tests_helper(working_dir):
        """ Helper function to recursively get all tests.
        """

        assert os.path.isdir(working_dir)

        items = os.listdir(working_dir)
        items = [os.path.join(working_dir, item) for item in items]
        dirs = [item for item in items if os.path.isdir(item)]
        tests = [item for item in items if ".cmd" in item]

        for item in dirs:
            tests += get_all_tests_helper(item)

        return tests

    # Recursively get all of the tests in the directory.
    tests = get_all_tests_helper(base_dir)

    # Find the correct base directory for the tests.
    common_prefix = os.path.commonprefix(tests)

    if common_prefix is not None:
        tests = [test.replace(common_prefix, "") for test in tests]

    return tests

def log(message):
    """ Log a debug message. This is to be used when the --test option is passed
    """

    if ARGS.testing is True:
        print message

def parse_lst_file(lst_file):
    """Parse a lstFile given.

    Args:
        lst_file(str): location of the lstFile

    Returns:
        test_metadata (defaultdict(lambda: None)): Key is test name.

    """

    assert os.path.isfile(lst_file)

    contents = None
    with open(lst_file) as file_handle:
        contents = file_handle.read()

    split = re.split("\[(.*?)\]", contents)

    unique_name = None
    test_metadata = defaultdict(lambda: None)
    for item in split:
        if len(item) == 0 or item[0] == "#":
            continue

        if unique_name is None:
            unique_name = item
        else:
            index = int(unique_name.split("_")[-1])
            metadata = defaultdict(lambda: None)

            attributes = item.split("\n")
            for attribute in attributes:
                # Skip the removed new lines.
                if len(attribute) == 0:
                    continue

                pair = attribute.split("=")
                key = pair[0].strip()
                value = pair[1].strip()

                metadata[key] = value

            # Relative path is unique, while the test name alone is not.
            unique_name = metadata["RelativePath"]
            test_metadata[unique_name] = (metadata, index)
            unique_name = None

    return test_metadata

################################################################################
# Main
################################################################################

def main(args):
    """ Main method
    Args:
        args ([str]): the arugments passed to the program.

    """

    # Assign all of the passed variables.
    pri0_test_dir = args.pri0_test_dir
    pri1_test_dir = args.pri1_test_dir
    old_list_file = args.old_list_file
    commit_hash = args.commit_hash
    unset_new = args.unset_new

    if commit_hash is None:
        print "Error please provide a commit hash."
        sys.exit(1)

    if pri0_test_dir is None or not os.path.isdir(pri0_test_dir):
        print "Error the Pri0 test directory passed is not a valid directory."
        sys.exit(1)

    if pri1_test_dir is None or not os.path.isdir(pri1_test_dir):
        print "Error the Pri1 test directory passed is not a valid directory."
        sys.exit(1)

    pri0_tests = get_all_tests(pri0_test_dir)
    print "Found %d tests in the pri0 test directory." % (len(pri0_tests))

    pri1_tests = get_all_tests(pri1_test_dir)
    print "Found %d tests in the pri1 test directory." % (len(pri1_tests))
    print

    priority_marked_tests = defaultdict(lambda: None)

    for test in pri1_tests:
        priority_marked_tests[test] = 1
    for test in pri0_tests:
        priority_marked_tests[test] = 0

    old_test_metadata = None
    # If we are updating an old lstFile. Get all of the tests from that
    # lstFile and their metadata.
    if old_list_file is not None:
        old_test_metadata = parse_lst_file(old_list_file)

        print "Found %d tests in the old lstFile." % (len(old_test_metadata))
        print

    test_metadata = create_metadata(priority_marked_tests)

    # Make sure the tuples are set up correctly.
    for item in test_metadata:
        test_metadata[item] = (test_metadata[item], -1)

    if old_test_metadata is not None:
        # If the new information has been changed, we will need to update
        # the lstFile.

        new_test_count = 0
        update_count = 0
        for test_name in test_metadata:
            new_metadata = test_metadata[test_name]
            old_metadata = old_test_metadata[test_name]

            attributes = None
            if old_test_metadata[test_name] is None:
                new_test_count += 1
                new_metadata[0]["Categories"] += ";NEW"
                old_test_metadata[test_name] = (new_metadata[0], -1)

            else:
                index = old_metadata[1]
                old_metadata = old_metadata[0]
                attributes = set(old_metadata.keys() + new_metadata[0].keys())

                # Make sure we go through all attributes of both sets.
                # If an attribute exists in one set but not the other it will
                # be None. If the new metadata has a new attribute, write this
                # into the old metadata. If the old metadata has an attribute
                # that does not exist in the new set. Do not remove it.

                overwritten = False

                for attribute in attributes:
                    if attribute == "MaxAllowedDurationSeconds":
                            continue
                    if attribute == "Categories":
                        new_split = new_metadata[0]["Categories"].split(";")
                        old_split = old_metadata["Categories"].split(";")

                        if unset_new:
                            if "NEW" in old_split:
                                old_split.remove("NEW")

                        # If an old test is marked as a failing test. Make
                        # sure that we carry that information along.
                        if "EXPECTED_PASS" in new_split and "EXPECTED_FAIL" in old_split:
                            new_split.remove("EXPECTED_PASS")

                        # If it used to be marked as pass but it is now failing. Make sure
                        # we remove the old category.
                        elif "EXPECTED_FAIL" in new_split and "EXPECTED_PASS" in old_split:
                            old_split.remove("EXPECTED_PASS")

                        joined_categories = set(old_split + new_split)

                        if (old_split != new_split):
                            overwritten = True
                        ordered_categories = []
                        for item in old_split:
                            if item in joined_categories:
                                ordered_categories.append(item)
                                joined_categories.remove(item)

                        ordered_categories = [item for item in ordered_categories if item != ""]

                        old_metadata[attribute] = ";".join(ordered_categories)
                        old_metadata[attribute] = old_metadata[attribute] + ";" + ";".join(joined_categories) if len(joined_categories) > 0 else old_metadata[attribute]
                        old_test_metadata[test_name] = (old_metadata, index)

                    elif new_metadata[0][attribute] != old_metadata[attribute]:
                            # If the old information is not the same as the new
                            # information, keep the new information. overwrite the old
                            # metadata.
                            if new_metadata[0][attribute] is not None:
                                overwritten = True
                                old_metadata[attribute] = new_metadata[0][attribute]

                                old_test_metadata[test_name] = (old_metadata, index)

                    if overwritten:
                        update_count += 1

        tests_removed = 0
        tests_to_remove = []
        for old_test_name in old_test_metadata:
            # Remove all old unreferenced tests
            if old_test_name not in test_metadata:
                tests_to_remove.append(old_test_name)
                tests_removed += 1

        for test_name in tests_to_remove:
            old_test_metadata.pop(test_name)

        print "Added %d tests." % new_test_count
        print "Removed %d tests." % tests_removed
        print "Finished join. %d tests updated." % update_count

        test_metadata = old_test_metadata

    # Overwrite the old file if provided, else use the generic name Tests.lst
    lst_file = "Tests.lst" if old_list_file is None else old_list_file

    # Write out the new lstFile
    create_list_file(lst_file, test_metadata)

################################################################################
################################################################################

if __name__ == "__main__":
    main(ARGS)
