##
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
##
##  This file provides utility functions to the adjacent python scripts

from filecmp import dircmp
from hashlib import sha256
from io import StringIO
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

def walk_recursively_and_update(dcmp):
    #for different Files Copy from right to left
    for name in dcmp.diff_files:
        srcpath = dcmp.right + "/" + name
        destpath = dcmp.left + "/" + name
        print("Updating %s" % (destpath))
        if  os.path.isfile(srcpath):
            shutil.copyfile(srcpath, destpath)
        else :
            raise Exception("path: " + srcpath + "is neither a file or folder")

    #copy right only files
    for name in dcmp.right_only:
        srcpath = dcmp.right + "/" + name
        destpath = dcmp.left + "/" + name
        print("Updating %s" % (destpath))
        if  os.path.isfile(srcpath):
            shutil.copyfile(srcpath, destpath)
        elif  os.path.isdir(srcpath):
            shutil.copytree(srcpath, destpath)
        else :
            raise Exception("path: " + srcpath + "is neither a file or folder")

    #delete left only files
    for name in dcmp.left_only:
        path = dcmp.left + "/" + name
        print("Deleting %s" % (path))
        if  os.path.isfile(path):
            os.remove(path)
        elif  os.path.isdir(path):
            shutil.rmtree(path)
        else :
            raise Exception("path: " + path + "is neither a file or folder")

    #call recursively
    for sub_dcmp in dcmp.subdirs.values():
        walk_recursively_and_update(sub_dcmp)

def UpdateDirectory(destpath,srcpath):

    print("Updating %s with %s" % (destpath,srcpath))
    if not os.path.exists(destpath):
        os.makedirs(destpath)
    dcmp = dircmp(destpath,srcpath)
    walk_recursively_and_update(dcmp)
