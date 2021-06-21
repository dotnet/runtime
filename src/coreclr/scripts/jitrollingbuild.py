#!/usr/bin/env python3
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
#
##
# Title               : jitrollingbuild.py
#
# Notes:
#
# Script to upload and manage rolling build clrjit and the rolling build storage location
#
################################################################################
################################################################################

import argparse
import locale
import os
import shutil
import sys
import tempfile
import urllib.request
import zipfile
import re

from coreclr_arguments import *

locale.setlocale(locale.LC_ALL, '')  # Use '' for auto, or force e.g. to 'en_US.UTF-8'

################################################################################
# Azure Storage information
################################################################################

az_account_name = "clrjit2"
az_container_name = "jitrollingbuild"
az_builds_root_folder = "builds"
az_blob_storage_account_uri = "https://" + az_account_name + ".blob.core.windows.net/"
az_blob_storage_container_uri = az_blob_storage_account_uri + az_container_name

################################################################################
# Argument Parser
################################################################################

description = """\
Script to upload and manage rolling build clrjit
"""

upload_description = """\
Upload clrjit to SuperPMI Azure storage.
"""

download_description = """\
Download clrjit from SuperPMI Azure storage.
"""

list_description = """\
List clrjit in SuperPMI Azure storage.
"""

host_os_help = "OS (windows, OSX, Linux). Default: current OS."

arch_help = "Architecture (x64, x86, arm, arm64). Default: current architecture."

build_type_help = "Build type (Debug, Checked, Release). Default: Checked."

git_hash_help = "git hash"

target_dir_help = "Directory to put the downloaded JIT."

skip_cleanup_help = "Skip intermediate file removal."

# Start of parser object creation.

parser = argparse.ArgumentParser(description=description)

subparsers = parser.add_subparsers(dest='mode', help="Command to invoke")

# Common parser for git_hash/arch/build_type/host_os arguments

common_parser = argparse.ArgumentParser(add_help=False)

common_parser.add_argument("-arch", help=arch_help)
common_parser.add_argument("-build_type", default="Checked", help=build_type_help)
common_parser.add_argument("-host_os", help=host_os_help)

# subparser for upload
upload_parser = subparsers.add_parser("upload", description=upload_description, parents=[common_parser])

upload_parser.add_argument("-git_hash", required=True, help=git_hash_help)
upload_parser.add_argument("-az_storage_key", help="Key for the clrjit Azure Storage location. Default: use the value of the CLRJIT_AZ_KEY environment variable.")
upload_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)

# subparser for download
download_parser = subparsers.add_parser("download", description=download_description, parents=[common_parser])

download_parser.add_argument("-git_hash", required=True, help=git_hash_help)
download_parser.add_argument("-target_dir", required=True, help=target_dir_help)
download_parser.add_argument("--skip_cleanup", action="store_true", help=skip_cleanup_help)

# subparser for list
list_parser = subparsers.add_parser("list", description=list_description, parents=[common_parser])

list_parser.add_argument("-git_hash", help=git_hash_help)
list_parser.add_argument("--all", action="store_true", help="Show all JITs, not just those for the specified (or default) git hash, OS, architecture, and flavor")

################################################################################
# Helper classes
################################################################################


class TempDir:
    """ Class to create a temporary working directory, or use one that is passed as an argument.

        Use with: "with TempDir() as temp_dir" to change to that directory and then automatically
        change back to the original working directory afterwards and remove the temporary
        directory and its contents (if args.skip_cleanup is False).
    """

    def __init__(self, path=None):
        self.mydir = tempfile.mkdtemp() if path is None else path
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.mydir)
        return self.mydir

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)
        # Note: we are using the global `args`, not coreclr_args. This works because
        # the `skip_cleanup` argument is not processed by CoreclrArguments, but is
        # just copied there.
        if not args.skip_cleanup:
            shutil.rmtree(self.mydir)


class ChangeDir:
    """ Class to temporarily change to a given directory. Use with "with".
    """

    def __init__(self, mydir):
        self.mydir = mydir
        self.cwd = None

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.mydir)

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)


def determine_jit_name(coreclr_args):
    """ Determine the jit based on the OS. If "-altjit" is specified, then use the specified altjit,
        or an appropriate altjit based on target.

    Args:
        coreclr_args (CoreclrArguments): parsed args

    Return:
        jit_name(str) : name of the jit for this os
    """

    jit_base_name = "clrjit"
    if coreclr_args.host_os == "OSX":
        return "lib" + jit_base_name + ".dylib"
    elif coreclr_args.host_os == "Linux":
        return "lib" + jit_base_name + ".so"
    elif coreclr_args.host_os == "windows":
        return jit_base_name + ".dll"
    else:
        raise RuntimeError("Unknown OS.")


def list_az_jits(filter_func=lambda unused: True, prefix_string = None):
    """ List the JITs in Azure Storage using REST api

    Args:
        filter_func (lambda: string -> bool): filter to apply to the list. The filter takes a URL and returns True if this URL is acceptable.
        prefix_string: Optional. Specifies a string prefix for the Azure Storage query.

    Returns:
        urls (list): set of URLs in Azure Storage that match the filter.

    Notes:
        This method does not require installing the Azure Storage python package.
    """

    # This URI will return *all* the blobs, for all git_hash/OS/architecture/build_type combinations.
    # pass "prefix=foo/bar/..." to only show a subset. Or, we can filter later using string search.
    #
    # Note that there is a maximum number of results returned in one query of 5000. So we might need to
    # iterate. In that case, the XML result contains a `<NextMarker>` element like:
    #
    # <NextMarker>2!184!MDAwMDkyIWJ1aWxkcy8wMTZlYzI5OTAzMzkwMmY2ZTY4Yzg0YWMwYTNlYzkxN2Y5MzA0OTQ2L0xpbnV4L3g2NC9DaGVja2VkL2xpYmNscmppdF93aW5fYXJtNjRfeDY0LnNvITAwMDAyOCE5OTk5LTEyLTMxVDIzOjU5OjU5Ljk5OTk5OTlaIQ--</NextMarker>
    #
    # which we need to pass to the REST API with `marker=...`.

    urls = []

    list_az_container_uri_root = az_blob_storage_container_uri + "?restype=container&comp=list&prefix=" + az_builds_root_folder + "/"
    if prefix_string:
        list_az_container_uri_root += prefix_string

    iter = 1
    marker = ""

    while True:
        list_az_container_uri = list_az_container_uri_root + marker

        try:
            contents = urllib.request.urlopen(list_az_container_uri).read().decode('utf-8')
        except Exception as exception:
            print("Didn't find any collections using {}".format(list_az_container_uri))
            print("  Error: {}".format(exception))
            return None

        # Contents is an XML file with contents like:
        # <EnumerationResults ContainerName="https://clrjit2.blob.core.windows.net/jitrollingbuild">
        #   <Prefix>builds/</Prefix>
        #   <Blobs>
        #     <Blob>
        #       <Name>builds/755f01659f03196487ec41225de8956911f8049b/Linux/x64/Checked/libclrjit.so</Name>
        #       <Url>https://clrjit2.blob.core.windows.net/jitrollingbuild/builds/755f01659f03196487ec41225de8956911f8049b/Linux/x64/Checked/libclrjit.so</Url>
        #       <Properties>
        #         ...
        #       </Properties>
        #     </Blob>
        #     <Blob>
        #       <Name>builds/755f01659f03196487ec41225de8956911f8049b/OSX/x64/Checked/libclrjit.dylib</Name>
        #       <Url>https://clrjit2.blob.core.windows.net/jitrollingbuild/builds/755f01659f03196487ec41225de8956911f8049b/OSX/x64/Checked/libclrjit.dylib</Url>
        #       <Properties>
        #         ...
        #       </Properties>
        #     </Blob>
        #     ... etc. ...
        #   </Blobs>
        # </EnumerationResults>
        #
        # We just want to extract the <Url> entries. We could probably use an XML parsing package, but we just
        # use regular expressions.

        urls_split = contents.split("<Url>")[1:]
        for item in urls_split:
            url = item.split("</Url>")[0].strip()
            if filter_func(url):
                urls.append(url)

        # Look for a continuation marker.
        re_match = re.match(r'.*<NextMarker>(.*)</NextMarker>.*', contents)
        if re_match:
            marker_text = re_match.group(1)
            marker = "&marker=" + marker_text
            iter += 1
        else:
            break

    return urls


def upload_command(coreclr_args):
    """ Upload the JIT

    Args:
        coreclr_args (CoreclrArguments): parsed args
    """

    print("JIT upload")

    def upload_blob(file, blob_name):
        blob_client = blob_service_client.get_blob_client(container=az_container_name, blob=blob_name)

        # Check if the blob already exists, and delete it if it does, before uploading / replacing it.
        try:
            blob_client.get_blob_properties()
            # If no exception, then the blob already exists. Delete it!
            print("Warning: replacing existing blob!")
            blob_client.delete_blob()
        except Exception:
            # Blob doesn't exist already; that's good
            pass

        with open(file, "rb") as data:
            blob_client.upload_blob(data)

    # 1. Find all the JIT builds in the product directory
    # 2. Upload them
    #
    # We could also upload debug info, but it's not clear it's needed for most purposes, and it is very big:
    # it increases the upload size from about 190MB to over 900MB for each roll.
    #
    # For reference, the JIT debug info is found:
    #    a. For Windows, in the PDB subdirectory, e.g. PDB\clrjit.pdb
    #    b. For Linux .dbg files, and Mac .dwarf files, in the same directory as the jit, e.g., libcoreclr.so.dbg

    # Target directory: <root>/git-hash/OS/architecture/build-flavor/
    # Note that build-flavor will probably always be Checked.

    files = []

    # First, find the primary JIT that we expect to find.
    jit_name = determine_jit_name(coreclr_args)
    jit_path = os.path.join(coreclr_args.product_location, jit_name)
    if not os.path.isfile(jit_path):
        print("Error: Couldn't find JIT at {}".format(jit_path))
        raise RuntimeError("Missing JIT")

    files.append(jit_path)

    # Next, look for any and all cross-compilation JITs. These are named, e.g.:
    #   clrjit_unix_x64_x64.dll
    #   clrjit_win_arm_x64.dll
    #   clrjit_win_arm64_x64.dll
    # and so on, and live in the same product directory as the primary JIT.
    #
    # Note that the expression below explicitly filters out the primary JIT since we added that above.
    # We handle the primary JIT specially so we can error if it is missing. For the cross-compilation
    # JITs, we don't bother trying to ensure that all the ones we might expect are actually there.
    #
    # We don't do a recursive walk because the JIT is also copied to the "sharedFramework" subdirectory,
    # so we don't want to pick that up.

    if coreclr_args.host_os == "OSX":
        allowed_extensions = [ ".dylib" ]
        # Add .dwarf for debug info
    elif coreclr_args.host_os == "Linux":
        allowed_extensions = [ ".so" ]
        # Add .dbg for debug info
    elif coreclr_args.host_os == "windows":
        allowed_extensions = [ ".dll" ]
    else:
        raise RuntimeError("Unknown OS.")

    cross_jit_paths = [os.path.join(coreclr_args.product_location, item)
                       for item in os.listdir(coreclr_args.product_location)
                       if re.match(r'.*clrjit.*', item) and item != jit_name and any(item.endswith(extension) for extension in allowed_extensions)]
    files += cross_jit_paths

    # On Windows, grab the PDB files from a sub-directory.
    # if coreclr_args.host_os == "windows":
    #    pdb_dir = os.path.join(coreclr_args.product_location, "PDB")
    #    if os.path.isdir(pdb_dir):
    #        pdb_paths = [os.path.join(pdb_dir, item) for item in os.listdir(pdb_dir) if re.match(r'.*clrjit.*', item)]
    #        files += pdb_paths

    print("Uploading:")
    for item in files:
        print("  {}".format(item))

    try:
        from azure.storage.blob import BlobServiceClient

    except:
        print("Please install:")
        print("  pip install azure-storage-blob")
        print("See also https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-python")
        raise RuntimeError("Missing azure storage package.")

    blob_service_client = BlobServiceClient(account_url=az_blob_storage_account_uri, credential=coreclr_args.az_storage_key)
    blob_folder_name = "{}/{}/{}/{}/{}".format(az_builds_root_folder, coreclr_args.git_hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type)

    total_bytes_uploaded = 0

    # Should we compress the JIT on upload? It would save space, but it makes it slightly more complicated to use
    # because you can't just "wget" or otherwise download the file and use it immediately -- you need to unzip first.
    # So for now, don't compress it.
    compress_jit = False

    with TempDir() as temp_location:
        for file in files:
            if compress_jit:
                # Zip compress the file we will upload
                zip_name = os.path.basename(file) + ".zip"
                zip_path = os.path.join(temp_location, zip_name)
                print("Compress {} -> {}".format(file, zip_path))
                with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED) as zip_file:
                    zip_file.write(file, os.path.basename(file))

                file_stat_result = os.stat(file)
                zip_stat_result = os.stat(zip_path)
                print("Compressed {:n} to {:n} bytes".format(file_stat_result.st_size, zip_stat_result.st_size))
                total_bytes_uploaded += zip_stat_result.st_size

                blob_name = "{}/{}".format(blob_folder_name, zip_name)
                print("Uploading: {} ({}) -> {}".format(file, zip_path, az_blob_storage_container_uri + "/" + blob_name))
                upload_blob(zip_path, blob_name)
            else:
                file_stat_result = os.stat(file)
                total_bytes_uploaded += file_stat_result.st_size
                file_name = os.path.basename(file)
                blob_name = "{}/{}".format(blob_folder_name, file_name)
                print("Uploading: {} -> {}".format(file, az_blob_storage_container_uri + "/" + blob_name))
                upload_blob(file, blob_name)

    print("Uploaded {:n} bytes".format(total_bytes_uploaded))
    print("Finished JIT upload")


def download_urls(urls, target_dir):
    """ Download a set of files, specified as URLs, to a target directory.
        If the URLs are to .ZIP files, then uncompress them and copy all contents
        to the target directory.

    Args:
        urls (list): the URLs to download
        target_dir (str): target directory where files are copied. Directory must exist

    Returns:
        list of local filenames of downloaded files
    """

    print("Downloading:")
    for url in urls:
        print("  {}".format(url))

    local_files = []

    # In case we'll need a temp directory for ZIP file processing, create it first.
    with TempDir() as temp_location:
        for url in urls:
            item_name = url.split("/")[-1]

            if url.lower().endswith(".zip"):
                # Delete everything in the temp_location (from previous iterations of this loop, so previous URL downloads).
                temp_location_items = [os.path.join(temp_location, item) for item in os.listdir(temp_location)]
                for item in temp_location_items:
                    if os.path.isdir(item):
                        shutil.rmtree(item)
                    else:
                        os.remove(item)

                download_path = os.path.join(temp_location, item_name)

                print("Download: {} -> {}".format(url, download_path))
                urllib.request.urlretrieve(url, download_path)

                print("Uncompress {}".format(download_path))
                with zipfile.ZipFile(download_path, "r") as file_handle:
                    file_handle.extractall(temp_location)

                # Copy everything that was extracted to the target directory.
                items = [ os.path.join(temp_location, item) for item in os.listdir(temp_location) if not item.endswith(".zip") ]
                for item in items:
                    target_path = os.path.join(target_dir, os.path.basename(item))
                    print("Copy {} -> {}".format(item, target_path))
                    shutil.copy2(item, target_dir)
                    local_files.append(target_path)
            else:
                # Not a zip file; download directory to target directory
                download_path = os.path.join(target_dir, item_name)

                print("Download: {} -> {}".format(url, download_path))
                urllib.request.urlretrieve(url, download_path)

                local_files.append(download_path)

    return local_files


def get_jit_urls(coreclr_args, find_all=False):
    """ Helper method: collect a list of URLs for all the JIT files to download or list.

    Args:
        coreclr_args (CoreclrArguments): parsed args
        find_all (bool): True to show all, or False to filter based on coreclr_args
    """

    blob_filter_string = "{}/{}/{}/{}".format(coreclr_args.git_hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type)
    blob_prefix_filter = "{}/{}/{}".format(az_blob_storage_container_uri, az_builds_root_folder, blob_filter_string).lower()

    # Determine if a URL in Azure Storage should be allowed. The URL looks like:
    #   https://clrjit.blob.core.windows.net/jitrollingbuild/builds/git_hash/Linux/x64/Checked/clrjit.dll
    # Filter to just the current git_hash, OS, architecture, and build_flavor.
    # If "find_all" is True, then no filtering happens: everything is returned.
    def filter_jits(url):
        url = url.lower()
        return find_all or url.startswith(blob_prefix_filter)

    return list_az_jits(filter_jits, None if find_all else blob_filter_string)


def download_command(coreclr_args):
    """ Download the JIT

    Args:
        coreclr_args (CoreclrArguments): parsed args
    """

    urls = get_jit_urls(coreclr_args, find_all=False)
    if urls is None:
        return

    download_urls(urls, coreclr_args.target_dir)


def list_command(coreclr_args):
    """ List the JITs in Azure Storage

    Args:
        coreclr_args (CoreclrArguments) : parsed args
    """

    urls = get_jit_urls(coreclr_args, find_all=coreclr_args.all)
    if urls is None:
        return

    count = len(urls)

    if coreclr_args.all:
        print("{} JIT files".format(count))
    else:
        blob_filter_string = "{}/{}/{}/{}".format(coreclr_args.git_hash, coreclr_args.host_os, coreclr_args.arch, coreclr_args.build_type)
        print("{} JIT files for {}".format(count, blob_filter_string))
    print("")

    for url in urls:
        print("{}".format(url))

    print("")


def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False, require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "mode",  # "mode" is from the `parser.add_subparsers(dest='mode')` call
                        lambda unused: True,
                        "Unable to set mode")

    if coreclr_args.mode == "upload":

        coreclr_args.verify(args,
                            "git_hash",
                            lambda unused: True,
                            "Unable to set git_hash")

        coreclr_args.verify(args,
                            "az_storage_key",
                            lambda item: item is not None,
                            "Specify az_storage_key or set environment variable CLRJIT_AZ_KEY to the key to use.",
                            modify_arg=lambda arg: os.environ["CLRJIT_AZ_KEY"] if arg is None and "CLRJIT_AZ_KEY" in os.environ else arg)

        coreclr_args.verify(args,
                            "skip_cleanup",
                            lambda unused: True,
                            "Unable to set skip_cleanup")

        if not os.path.isdir(coreclr_args.product_location):
            print("Built product location could not be determined")
            raise RuntimeError("Error")

    elif coreclr_args.mode == "download":

        coreclr_args.verify(args,
                            "git_hash",
                            lambda unused: True,
                            "Unable to set git_hash")

        coreclr_args.verify(args,
                            "target_dir",
                            lambda unused: True,
                            "Unable to set target_dir")

        coreclr_args.verify(args,
                            "skip_cleanup",
                            lambda unused: True,
                            "Unable to set skip_cleanup")

        if not os.path.isdir(coreclr_args.target_dir):
            print("--target_dir directory does not exist")
            raise RuntimeError("Error")

    elif coreclr_args.mode == "list":

        coreclr_args.verify(args,
                            "git_hash",
                            lambda unused: True,
                            "Unable to set git_hash")

        coreclr_args.verify(args,
                            "all",
                            lambda unused: True,
                            "Unable to set all")

    return coreclr_args

################################################################################
# main
################################################################################


def main(args):
    """ Main method
    """

    if sys.version_info.major < 3:
        print("Please install python 3 or greater")
        return 1

    coreclr_args = setup_args(args)
    success = True

    if coreclr_args.mode == "upload":
        upload_command(coreclr_args)

    elif coreclr_args.mode == "download":
        download_command(coreclr_args)

    elif coreclr_args.mode == "list":
        list_command(coreclr_args)

    else:
        raise NotImplementedError(coreclr_args.mode)

    return 0 if success else 1

################################################################################
# __main__
################################################################################


if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
