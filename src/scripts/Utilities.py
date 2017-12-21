from filecmp import dircmp
import shutil
import os

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
