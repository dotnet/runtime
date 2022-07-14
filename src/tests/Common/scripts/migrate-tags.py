#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               :migrate-tags.py
#
################################################################################
# Script to migrate the 'Categories' tags from an exiting smarty list file
# to a new smarty list, which typically doesn't have the 'Categories' tags.
################################################################################

import os
import os.path
import sys

################################################################################
# Globals
################################################################################

def fatal(str):
    print str
    exit(1)

################################################################################
# parse_list_file
################################################################################

def parse_list_file(listfile):

    print 'Parsing:', listfile

    # This function will build a list 'testdata'
    # that contains a 4-tuple for each test that we find
    #
    testdata = []

    test_name = None
    test_number = None
    test_fullpath = None

    # This could also be a dictionary, but we want to preserve the order
    # that we encounter the entries and we only have a small number of keys
    # and python supports casting a list into a dictionary wehn you need to
    # Also consider using class OrderDict
    test_properties = []

    with open(listfile) as file:

        expecting_metadata = False
        expecting_testname = True

        for line in file:

            if expecting_metadata:
                # We are expecting a series of key=value assignments
                # this is the metadata for the current testname

                if test_name is None:
                    fatal('logic error - test_name not set');

                if test_number is None:
                    fatal('logic error - test_number not set');

                line = line.rstrip('\n')

                split_line = line.split('=')

                # if we have a single '=' the len(split_line) will be 2
                if len(split_line) == 2:
                    # we record the key and the value strings
                    key = split_line[0]
                    value = split_line[1]

                    if key == 'Categories':
                        # 'Categories' values are always split using ';'
                        # first remove the trailing newline
                        # now split using semicolon
                        value = value.split(';')

                    if key == 'RelativePath':
                        # The 'RelativePath' value is used in the tuple,
                        # so we record it's value here.
                        if test_fullpath is not None:
                            fatal('logic error - fullpath is already set');
                        test_fullpath = value

                    tup = (key, value)
                    test_properties.append(tup)
                else:
                    # we didn't have a 'key=value' line
                    # we will switch to expecting_testname
                    # note that we have already read the next line
                    # so need to fall into the code below which finds
                    # the test name from the line that we just read

                    if test_fullpath is None:
                        fatal('format error - RelativePath entry is missing');

                    # we must record the current test information:
                    # create the four-tuple entry
                    entry = (test_name,     test_number,
                             test_fullpath, test_properties)

                    testdata.append(entry)

                    # reset the test state variables to empty for the next test
                    test_name = None
                    test_number = None
                    test_fullpath = None
                    test_properties = []

                    # change the state to expecting_testname
                    expecting_metadata = False
                    expecting_testname = True

            if expecting_testname:
                # We are expecting the next testname entry
                # We will skip lines until we find a test name line
                # which comes in as [ testname_number ]
                split_line = line.split('[')

                # if we don't have a '[' the len(split_line) will be 1
                # we will skip (ignore) this line
                if len(split_line) == 1:
                    continue

                if test_name is not None:
                    fatal('logic error - test_name is already set');

                if test_number is not None:
                    fatal('logic error - test_number is already set');

                # we now expect to match '[testname_number]'
                # only when len(split_line) is 2 did we match exactly one '['
                if len(split_line) != 2:
                    fatal('syntax error - multiple [');

                split_line = split_line[1].split(']')

                # only when len(split_line) is 2 did we match exactly one ']'
                if len(split_line) != 2:
                    fatal('syntax error - missing or multiple ]:' + line);

                # get the string enclosed by [ ... ]
                name_and_number = split_line[0]

                split_line = name_and_number.split('_')

                # Note that the testname portion may also contain '_' so we
                # have to get the testnumber from the end using [-1]
                if len(split_line) == 1:
                    fatal('syntax error - missing _' + line);

                # elements in split_line are numbered [ 0, 1, ... -2, -1 ]
                test_number_str = split_line[-1]
                if len(split_line) == 2:
                    test_name = split_line[0]
                else:
                    test_name = '_'.join(split_line[0:-1])

                if not test_number_str.isdigit():
                    fatal('syntax error - missing or illegal testnumber'+line);

                test_number = int(test_number_str)

                expecting_testname = False
                expecting_metadata = True

    if expecting_metadata:
        # We need to create and append the last four-tuple entry
        entry = (test_name, test_number, test_fullpath, test_properties)
        testdata.append(entry)

    print str(len(testdata)) + ' tests found in ' + listfile
    print
    return testdata

################################################################################
# write_list_file
################################################################################

def write_list_file(filename, testdata):

    if len(testdata) == 0:
        fatal('logic error - testdata is empty');

    print 'Writing:', filename

    with open(filename, 'w') as file:

        line = '##=== Test Definitions ===============================\n'
        file.write(line)

        for entry in testdata:
            # entry = (test_name, test_number, test_fullpath, test_properties)
            test_name = entry[0]
            test_number = entry[1]
            test_properties = entry[3]

            line = '[' + test_name + '_' + str(test_number) + ']\n'
            file.write(line)

            for tup in test_properties:
                # tup = (key, value)
                key = tup[0]
                values = tup[1]

                # most values are already strings
                value_str = values;

                # when key is 'Categories' the values is a list of strings
                # so construct the value_str using join
                if key == 'Categories':
                    # 'Categories' values were split using ';'
                    # so we need to use 'join' to reverse that
                    value_str = ';'.join(values)

                line = key + '=' + value_str
                file.write(line + '\n')

    print 'Wrote ' + filename + ' with ' + str(len(testdata)) + ' tests'

################################################################################
# migrate_tags
################################################################################

def migrate_tags(new_data, old_data):

    print 'Migrating the tags'

    new_count = 0
    old_dict = {}

    for old_entry in old_data:
        # entry = (test_name, test_number, test_fullpath, test_properties)
        test_fullpath = old_entry[2]
        map_key = test_fullpath
        old_dict[map_key] = old_entry

    for new_entry in new_data:
        # entry = (test_name, test_number, test_fullpath, test_properties)
        test_name = new_entry[0]
        test_number = new_entry[1]
        test_fullpath = new_entry[2]
        test_properties = new_entry[3]

        # use list comprehensions to build a list of matches
        new_matches = [item for item in test_properties
                                if item[0] == 'Categories']
        if len(new_matches) == 0:
            cat_tup = ('Categories', [])
        else:
            if len(new_matches) > 1:
                fatal('format error - duplicate Categories entries');
            cat_tup = new_matches[0]

        # 'new_tags' is the set of 'Categories' TAGS for this in new_data
        new_tags = cat_tup[1]

        map_key = test_fullpath
        old_entry = old_dict.get(map_key)
        if (old_entry == None):
            # We will add the 'NEW' tag to flag this
            # as a test that is being added
            new_count += 1

            # Check if the 'NEW' tag is already present in cat_tup[1]
            if not 'NEW' in new_tags:
                new_tags.append('NEW')
        else:
            # We have a matching old_entry, so we will build a
            # concatenation of the 'Categories' tags

            # We need to migrate the 'Category' tags from the old_data
            oldtest_properties = old_entry[3]

            # use list comprehensions to build a list of matches
            old_matches = [item for item in oldtest_properties
                                    if item[0] == 'Categories']
            # there should be exactly 1 match
            if len(old_matches) != 1:
                fatal('format error - missing or duplicate Categories entries');

            old_tup = old_matches[0]

            # 'old_tags' is the set of 'Categories' TAGS for this in old_data
            old_tags = old_tup[1]

            # extend in place the 'new_tags' list with the 'old_tags' list
            new_tags.extend(old_tags)

    print str(new_count) + ' new tests found and tagged as NEW'
    print

################################################################################
# Main
################################################################################

if __name__ == '__main__':
    print 'Starting migrate-tags: Last Updated - 10-Mar-16'
    print '- - - - - - - - - - - - - - - - - - - - - - - - - - - -'

    if len(sys.argv) < 3:
        print 'Error, incorrect number of arguments.'
        print 'Ex usage: python migrate-tags <new_listfile> <old_listfile>'
        print 'Note this completely overwrites the exisiting new_listfile!'
        exit(1)

    new_listfile = sys.argv[1]
    old_listfile = sys.argv[2]

    if not os.path.isfile(new_listfile):
        fatal('Error: new listfile must be valid.')

    if not os.path.isfile(old_listfile):
        fatal('Error: old listfile must be valid.')

    new_data = parse_list_file(new_listfile)
    old_data = parse_list_file(old_listfile)

    migrate_tags(new_data, old_data)

    # Warning this completely overwrites the exisiting new_listfile
    write_list_file(new_listfile, new_data)




