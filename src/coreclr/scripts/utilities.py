##
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
##
##  This file provides utility functions to the adjacent python scripts

from hashlib import sha256
from io import StringIO
import filecmp
import shutil
import sys
import os

class WrappedStringIO(StringIO):
    """A wrapper around StringIO to allow writing str objects"""
    def write(self, s):
        if sys.version_info < (3, 0, 0):
            if isinstance(s, str):
                s = unicode(s)
        super(WrappedStringIO, self).write(s)

class UpdateFileWriter:
    """A file-like context object which will only write to a file if the result would be different

    Attributes:
        filename (str): The name of the file to update
        stream (WrappedStringIO): The file-like stream provided upon context enter

    Args:
        filename (str): Sets the filename attribute
    """
    filemode = 'w'

    def __init__(self, filename):
        self.filename = filename
        self.stream = None

    def __enter__(self):
        self.stream = WrappedStringIO()
        return self.stream

    def __exit__(self, exc_type, exc_value, traceback):
        if exc_value is None:
            new_content = self.stream.getvalue()
            new_hash = sha256()
            cur_hash = sha256()

            try:
                with open(self.filename, 'r') as fstream:
                    cur_hash.update(fstream.read().encode('utf-8'))
                file_found = True
            except IOError:
                file_found = False

            if file_found:
                new_hash.update(new_content.encode('utf-8'))
                update = new_hash.digest() != cur_hash.digest()
            else:
                update = True

            if update:
                with open(self.filename, 'w') as fstream:
                    fstream.write(new_content)

        self.stream.close()

def open_for_update(filename):
    return UpdateFileWriter(filename)

def split_entries(entries, directory):
    """Given a list of entries in a directory, listing return a set of file and a set of dirs"""
    files = set([entry for entry in entries if os.path.isfile(os.path.join(directory, entry))])
    dirs = set([entry for entry in entries if os.path.isdir(os.path.join(directory, entry))])

    return files, dirs

def update_directory(srcpath, dstpath, recursive=True, destructive=True, shallow=False):
    """Updates dest directory with files from src directory

    Args:
        destpath (str): The destination path to sync with the source
        srcpath (str): The source path to sync to the destination
        recursive(boolean): If True, descend into and update subdirectories (default: True)
        destructive(boolean): If True, delete files in the destination which do not exist in the source (default: True)
        shallow(boolean): If True, only use os.stat to diff files. Do not examine contents (default: False)
    """
    srcfiles, srcdirs = split_entries(os.listdir(srcpath), srcpath)
    dstfiles, dstdirs = split_entries(os.listdir(dstpath), dstpath)


    # Update files in both src and destination which are different in destination
    commonfiles = srcfiles.intersection(dstfiles)
    _, mismatches, errors = filecmp.cmpfiles(srcpath, dstpath, commonfiles, shallow=shallow)

    if errors:
        raise RuntimeError("Comparison failed for the following files(s): {}".format(errors))

    for mismatch in mismatches:
        shutil.copyfile(os.path.join(srcpath, mismatch), os.path.join(dstpath, mismatch))

    # Copy over files from source which do not exist in the destination
    for missingfile in srcfiles.difference(dstfiles):
        shutil.copyfile(os.path.join(srcpath, missingfile), os.path.join(dstpath, missingfile))

    #If destructive, delete files in destination which do not exist in sourc
    if destructive:
        for deadfile in dstfiles.difference(srcfiles):
            print(deadfile)
            os.remove(os.path.join(dstpath, deadfile))

        for deaddir in dstdirs.difference(srcdirs):
            print(deaddir)
            shutil.rmtree(os.path.join(dstpath, deaddir))

    #If recursive, do this again for each source directory
    if recursive:
        for dirname in srcdirs:
            dstdir, srcdir = os.path.join(dstpath, dirname), os.path.join(srcpath, dirname)
            if not os.path.exists(dstdir):
                os.makedirs(dstdir)
            update_directory(srcdir, dstdir, recursive, destructive, shallow)

class EventExclusions:
    def __init__(self):
        self.nostack         = set()
        self.explicitstack   = set()
        self.noclrinstance   = set()

def parseExclusionList(exclusion_filename):

    exclusionInfo = EventExclusions()

    if not os.path.isfile(exclusion_filename):
        return exclusionInfo

    with open(exclusion_filename,'r') as ExclusionFile:

        for line in ExclusionFile:
            line = line.strip()

            #remove comments
            if not line or line.startswith('#'):
                continue

            tokens = line.split(':')
            #entries starting with nomac are ignored
            if "nomac" in tokens:
                continue

            if len(tokens) > 5:
                raise Exception("Invalid Entry " + line + "in "+ exclusion_filename)

            eventProvider = tokens[2]
            eventTask     = tokens[1]
            eventSymbol   = tokens[4]

            if eventProvider == '':
                eventProvider = "*"
            if eventTask     == '':
                eventTask     = "*"
            if eventSymbol   == '':
                eventSymbol   = "*"
            entry = eventProvider + ":" + eventTask + ":" + eventSymbol

            if tokens[0].lower() == "nostack":
                exclusionInfo.nostack.add(entry)
            if tokens[0].lower() == "stack":
                exclusionInfo.explicitstack.add(entry)
            if tokens[0].lower() == "noclrinstanceid":
                exclusionInfo.noclrinstance.add(entry)

    return exclusionInfo

def parseInclusionList(inclusion_filename):

    inclusion_list = {}

    if not os.path.isfile(inclusion_filename):
        return inclusion_list

    with open(inclusion_filename,'r') as InclusionFile:
        for line in InclusionFile:
            line = line.strip()

            #remove comments
            if not line or line.startswith('#'):
                continue

            tokens = line.split(':')

            if len(tokens) == 0:
                continue

            if len(tokens) > 2:
                raise Exception("Invalid Entry " + line + "in "+ inclusion_filename)

            providerName = ""
            eventName = ""

            if len(tokens) == 2:
                providerName = tokens[0]
                eventName = tokens[1]

            if len(tokens) == 1:
                providerName = "*"
                eventName = tokens[0]

            if providerName not in inclusion_list:
                inclusion_list[providerName] = set()

            inclusion_list[providerName].add(eventName)

    return inclusion_list
