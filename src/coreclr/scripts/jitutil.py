#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
# Title               : jitutil.py
#
# Notes:
#
# Utility functions used by Python scripts used by the CLR JIT team.
#
################################################################################
################################################################################

import os
import shutil
import subprocess
import sys
import tempfile
import logging
import time
import urllib
import urllib.request
import zipfile

################################################################################
##
## Helper classes
##
################################################################################

class TempDir:
    """ Class to create a temporary working directory, or use one that is passed as an argument.

        Use with: "with TempDir() as temp_dir" to change to that directory and then automatically
        change back to the original working directory afterwards and remove the temporary
        directory and its contents (if skip_cleanup is False).
    """

    def __init__(self, path=None, skip_cleanup=False):
        self.mydir = tempfile.mkdtemp() if path is None else path
        self.cwd = None
        self._skip_cleanup = skip_cleanup

    def __enter__(self):
        self.cwd = os.getcwd()
        os.chdir(self.mydir)
        return self.mydir

    def __exit__(self, exc_type, exc_val, exc_tb):
        os.chdir(self.cwd)
        if not self._skip_cleanup:
            try:
                shutil.rmtree(self.mydir)
            except Exception as ex:
                logging.warning("Warning: failed to remove directory \"%s\": %s", self.mydir, ex)
                # Print out all the remaining files and directories, in case that provides useful information
                # for diagnosing the failure. If there is an exception doing this, ignore it.
                try:
                    for dirpath, dirnames, filenames in os.walk(self.mydir):
                        for dir_name in dirnames:
                            logging.warning("  Remaining directory: \"%s\"", os.path.join(dirpath, dir_name))
                        for file_name in filenames:
                            logging.warning("  Remaining file: \"%s\"", os.path.join(dirpath, file_name))
                except Exception:
                    pass

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


################################################################################
##
## Azure DevOps pipelines helper functions
##
################################################################################

def set_pipeline_variable(name, value):
    """ This method sets pipeline variable.

    Args:
        name (string): Name of the variable.
        value (string): Value of the variable.
    """
    define_variable_format = "##vso[task.setvariable variable={0}]{1}"
    print("{0} -> {1}".format(name, value))  # logging
    print(define_variable_format.format(name, value))  # set variable



################################################################################
##
## Helper functions
##
################################################################################

def decode_and_print(str_to_decode):
    """Decode a UTF-8 encoded bytes to string.

    Args:
        str_to_decode (byte stream): Byte stream to decode

    Returns:
        String output. If there any encoding/decoding errors, it will not print anything
        and return an empty string.
    """
    output = ''
    try:
        output = str_to_decode.decode("utf-8", errors='replace')
        print(output)
    finally:
        return output

def run_command(command_to_run, _cwd=None, _exit_on_fail=False, _output_file=None, _env=None):
    """ Runs the command.

    Args:
        command_to_run ([string]): Command to run along with arguments.
        _cwd (string): Current working directory.
        _exit_on_fail (bool): If it should exit on failure.
        _output_file (): 
        _env: environment for sub-process, passed to subprocess.Popen()
    Returns:
        (string, string, int): Returns a tuple of stdout, stderr, and command return code if _output_file= None
        Otherwise stdout, stderr are empty.
    """
    print("Running: " + " ".join(command_to_run))
    command_stdout = ""
    command_stderr = ""

    if _env:
        print("  with environment:")
        for name, value in _env.items():
            print("    {0}={1}".format(name,value))

    return_code = 1

    output_type = subprocess.STDOUT if _output_file else subprocess.PIPE
    with subprocess.Popen(command_to_run, env=_env, stdout=subprocess.PIPE, stderr=output_type, cwd=_cwd) as proc:

        # For long running command, continuously print the output
        if _output_file:
            while True:
                with open(_output_file, 'a') as of:
                    output = proc.stdout.readline()
                    if proc.poll() is not None:
                        break
                    if output:
                        output_str = decode_and_print(output.strip())
                        of.write(output_str + "\n")
        else:
            command_stdout, command_stderr = proc.communicate()
            if len(command_stdout) > 0:
                decode_and_print(command_stdout)
            if len(command_stderr) > 0:
                decode_and_print(command_stderr)

        return_code = proc.returncode
        if _exit_on_fail and return_code != 0:
            print("Command failed. Exiting.")
            sys.exit(1)
    return command_stdout, command_stderr, return_code


def copy_directory(src_path, dst_path, verbose_output=False, verbose_copy=False, verbose_skip=False, match_func=lambda path: True):
    """Copies directory in 'src_path' to 'dst_path' maintaining the directory
    structure. https://docs.python.org/3.5/library/shutil.html#shutil.copytree can't
    be used in this case because it expects the destination directory should not
    exist, however we do call copy_directory() to copy files to same destination directory.

    Args:
        src_path (string): Path of source directory that need to be copied.
        dst_path (string): Path where directory should be copied.
        verbose_output (bool): True to print every copied or skipped file or error.
        verbose_copy (bool): True to print every copied file
        verbose_skip (bool): True to print every skipped file.
        match_func (str -> bool) : Criteria function determining if a file is copied.
    """
    display_copy = verbose_output or verbose_copy
    display_skip = verbose_output or verbose_skip
    for item in os.listdir(src_path):
        src_item = os.path.join(src_path, item)
        dst_item = os.path.join(dst_path, item)
        if os.path.isdir(src_item):
            copy_directory(src_item, dst_item, verbose_output, verbose_copy, verbose_skip, match_func)
        else:
            try:
                if match_func(src_item):
                    if display_copy:
                        print("> copy {0} => {1}".format(src_item, dst_item))
                    try:
                        if not os.path.exists(dst_path):
                            os.makedirs(dst_path)
                        shutil.copy2(src_item, dst_item)
                    except PermissionError as pe_error:
                        print('Ignoring PermissionError: {0}'.format(pe_error))
                else:
                    if display_skip:
                        print("> skipping {0}".format(src_item))
            except UnicodeEncodeError:
                # Should this always be displayed? Or is it too verbose somehow?
                if verbose_output:
                    print("> Got UnicodeEncodeError")


def copy_files(src_path, dst_path, file_names):
    """Copy files from 'file_names' list from 'src_path' to 'dst_path'.
    It retains the original directory structure of src_path.

    Args:
        src_path (string): Source directory from where files are copied.
        dst_path (string): Destination directory where files to be copied.
        file_names ([string]): List of full path file names to be copied.
    """

    print('### Copying below files from {0} to {1}:'.format(src_path, dst_path))
    print('')
    print(os.linesep.join(file_names))
    for f in file_names:
        # Create same structure in dst so we don't clobber same files names present in different directories
        dst_path_of_file = f.replace(src_path, dst_path)

        dst_directory = os.path.dirname(dst_path_of_file)
        if not os.path.exists(dst_directory):
            os.makedirs(dst_directory)
        try:
            shutil.copy2(f, dst_path_of_file)
        except PermissionError as pe_error:
            print('Ignoring PermissionError: {0}'.format(pe_error))


def remove_prefix(text, prefix):
    """ Helper function to remove a prefix `prefix` from a string `text`
    """
    if text.startswith(prefix):
        return text[len(prefix):]
    return text


def is_zero_length_file(fpath):
    """ Determine if a file system path refers to an existing file that is zero length

    Args:
        fpath (str) : file system path to test

    Returns:
        bool : true if the path is an existing file that is zero length
    """
    return os.path.isfile(fpath) and os.stat(fpath).st_size == 0


def is_nonzero_length_file(fpath):
    """ Determine if a file system path refers to an existing file that is non-zero length

    Args:
        fpath (str) : file system path to test

    Returns:
        bool : true if the path is an existing file that is non-zero length
    """
    return os.path.isfile(fpath) and os.stat(fpath).st_size != 0


def make_safe_filename(s):
    """ Turn a string into a string usable as a single file name component; replace illegal characters with underscores.
        Also, limit the length of the file name to avoid creating illegally long file names. This is done by taking a
        suffix of the name no longer than the maximum allowed file name length.

    Args:
        s (str) : string to convert to a file name

    Returns:
        (str) : The converted string
    """
    def safe_char(c):
        if c.isalnum():
            return c
        else:
            return "_"
    # Typically, a max filename length is 256, but let's limit it far below that, because callers typically add additional
    # strings to this.
    max_allowed_file_name_length = 150
    s = "".join(safe_char(c) for c in s)
    s = s[-max_allowed_file_name_length:]
    return s


def find_in_path(name, pathlist, match_func=os.path.isfile):
    """ Find a name (e.g., directory name or file name) in the file system by searching the directories
        in a `pathlist` (e.g., PATH environment variable that has been semi-colon
        split into a list).

    Args:
        name (str)               : name to search for
        pathlist (list)          : list of directory names to search
        match_func (str -> bool) : determines if the name is a match

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    for dirname in pathlist:
        candidate = os.path.join(dirname, name)
        if match_func(candidate):
            return candidate
    return None


def find_file(filename, pathlist):
    """ Find a filename in the file system by searching the directories
        in a `pathlist` (e.g., PATH environment variable that has been semi-colon
        split into a list).

    Args:
        filename (str)          : name to search for
        pathlist (list)         : list of directory names to search

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    return find_in_path(filename, pathlist)


def find_dir(dirname, pathlist):
    """ Find a directory name in the file system by searching the directories
        in a `pathlist` (e.g., PATH environment variable that has been semi-colon
        split into a list).

    Args:
        dirname (str)           : name to search for
        pathlist (list)         : list of directory names to search

    Returns:
        (str) The pathname of the object, or None if not found.
    """
    return find_in_path(dirname, pathlist, match_func=os.path.isdir)


def create_unique_directory_name(root_directory, base_name):
    """ Create a unique directory name by joining `root_directory` and `base_name`.
        If this name already exists, append ".1", ".2", ".3", etc., to the final
        name component until the full directory name is not found.

    Args:
        root_directory (str)     : root directory in which a new directory will be created
        base_name (str)          : the base name of the new directory name component to be added

    Returns:
        (str) The full absolute path of the new directory. The directory has been created.
    """
    root_directory = os.path.abspath(root_directory)
    full_path = os.path.join(root_directory, base_name)

    count = 1
    while os.path.isdir(full_path):
        new_full_path = os.path.join(root_directory, base_name + "." + str(count))
        count += 1
        full_path = new_full_path

    os.makedirs(full_path)
    return full_path


def create_unique_file_name(directory, base_name, extension):
    """ Create a unique file name in the specified directory by joining `base_name` and `extension`.
        If this name already exists, append ".1", ".2", ".3", etc., to the `base_name`
        name component until the full file name is not found.

    Args:
        directory (str)  : directory in which a new file will be created
        base_name (str)  : the base name of the new filename to be added
        extension (str)  : the filename extension of the new filename to be added

    Returns:
        (str) The full absolute path of the new filename.
    """
    directory = os.path.abspath(directory)
    if not os.path.isdir(directory):
        try:
            os.makedirs(directory)
        except Exception as exception:
            logging.critical(exception)
            raise exception

    full_path = os.path.join(directory, base_name + "." + extension)

    count = 1
    while os.path.isfile(full_path):
        new_full_path = os.path.join(directory, base_name + "." + str(count) + "." + extension)
        count += 1
        full_path = new_full_path

    return full_path


def get_files_from_path(path, match_func=lambda path: True):
    """ Return all files in a directory tree matching a criteria.

    Args:
        path (str)               : Either a single file to include, or a directory to traverse looking for matching
                                   files.
        match_func (str -> bool) : Criteria function determining if a file is added to the list

    Returns:
        Array of absolute paths of matching files
    """
    if not(os.path.isdir(path) or os.path.isfile(path)):
        logging.warning("Warning: \"%s\" is not a file or directory", path)
        return []

    path = os.path.abspath(path)

    files = []

    if os.path.isdir(path):
        for item in os.listdir(path):
            files += get_files_from_path(os.path.join(path, item), match_func)
    else:
        if match_func(path):
            files.append(path)

    return files


def is_url(path):
    """ Return True if this looks like a URL

    Args:
        path (str) : name to check

    Returns:
        True it it looks like an URL, False otherwise.
    """
    # Probably could use urllib.parse to be more precise.
    # If it doesn't look like an URL, treat it like a file, possibly a UNC file.
    return path.lower().startswith("http:") or path.lower().startswith("https:")

################################################################################
##
## Azure Storage functions
##
################################################################################

# Decide if we're going to download and enumerate Azure Storage using anonymous
# read access and urllib functions (False), or Azure APIs including authentication (True).
authenticate_using_azure = False

# Have we checked whether we have the Azure Storage libraries yet?
azure_storage_libraries_check = False


def require_azure_storage_libraries(need_azure_storage_blob=True, need_azure_identity=True):
    """ Check for and import the Azure libraries.
        We do this lazily, only when we decide we're actually going to need them.
        Once we've done it once, we don't do it again.

        For this to work for cross-module usage, after you call this function, you need to add a line like:
            from jitutil import BlobClient, AzureCliCredential
        naming all the types you want to use.

        The full set of types this function loads:
            BlobServiceClient, BlobClient, ContainerClient, AzureCliCredential
    """
    global azure_storage_libraries_check, BlobServiceClient, BlobClient, ContainerClient, AzureCliCredential

    if azure_storage_libraries_check:
        return

    azure_storage_libraries_check = True

    azure_storage_blob_import_ok = True
    if need_azure_storage_blob:
        try:
            from azure.storage.blob import BlobServiceClient, BlobClient, ContainerClient
        except:
            azure_storage_blob_import_ok = False

    azure_identity_import_ok = True
    if need_azure_identity:
        try:
            from azure.identity import AzureCliCredential
        except:
            azure_identity_import_ok = False

    if not azure_storage_blob_import_ok or not azure_identity_import_ok:
        logging.error("One or more required Azure Storage packages is missing.")
        logging.error("")
        logging.error("Please install:")
        logging.error("  pip install azure-storage-blob azure-identity")
        logging.error("or (Windows):")
        logging.error("  py -3 -m pip install azure-storage-blob azure-identity")
        logging.error("See also https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-python")
        raise RuntimeError("Missing Azure Storage package.")

    # The Azure packages spam all kinds of output to the logging channels.
    # Restrict this to only ERROR and CRITICAL.
    for name in logging.Logger.manager.loggerDict.keys():
        if 'azure' in name:
            logging.getLogger(name).setLevel(logging.ERROR)


def report_azure_error():
    """ Report an Azure error
    """
    logging.error("A problem occurred accessing Azure. Are you properly authenticated using the Azure CLI?")
    logging.error("Install the Azure CLI from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli.")
    logging.error("Then log in to Azure using `az login`.")


def download_with_azure(uri, target_location, fail_if_not_found=True):
    """ Do an URI download using Azure blob storage API. Compared to urlretrieve,
        there is no progress hook. Maybe if converted to use the async APIs we
        could have some kind of progress?

    Args:
        uri (string)              : URI to download
        target_location (string)  : local path to put the downloaded object
        fail_if_not_found (bool)  : if True, fail if a download fails due to file not found (HTTP error 404).
                                    Otherwise, ignore the failure.

    Returns True if successful, False on failure
    """

    require_azure_storage_libraries()

    logging.info("Download: %s -> %s", uri, target_location)

    ok = True
    az_credential = AzureCliCredential()
    blob = BlobClient.from_blob_url(uri, credential=az_credential)
    with open(target_location, "wb") as my_blob:
        try:
            download_stream = blob.download_blob(retry_total=0)
            try:
                my_blob.write(download_stream.readall())
            except Exception as ex1:
                logging.error("Error writing data to %s", target_location)
                report_azure_error()
                ok = False
        except Exception as ex2:
            logging.error("Azure error downloading %s", uri)
            report_azure_error()
            ok = False

    if not ok and fail_if_not_found:
        raise RuntimeError("Azure failed to download")
    return ok

################################################################################
##
## File downloading functions
##
################################################################################


def download_progress_hook(count, block_size, total_size):
    """ A hook for urlretrieve to report download progress

    Args:
        count (int)               : current block index
        block_size (int)          : size of a block
        total_size (int)          : total size of a payload
    """
    sys.stdout.write("\rDownloading {0:.1f}/{1:.1f} MB...".format(min(count * block_size, total_size) / 1024 / 1024, total_size / 1024 / 1024))
    sys.stdout.flush()


def download_with_progress_urlretrieve(uri, target_location, fail_if_not_found=True, display_progress=True):
    """ Do an URI download using urllib.request.urlretrieve with a progress hook.
        Retries the download up to 5 times unless the URL returns 404.

        Outputs messages using the `logging` package.

    Args:
        uri (string)              : URI to download
        target_location (string)  : local path to put the downloaded object
        fail_if_not_found (bool)  : if True, fail if a download fails due to file not found (HTTP error 404).
                                    Otherwise, ignore the failure.
        display_progress (bool)   : if True, display download progress (for URL downloads). Otherwise, do not.

    Returns True if successful, False on failure
    """
    logging.info("Download: %s -> %s", uri, target_location)

    ok = False
    num_tries = 5
    for try_num in range(num_tries):
        try:
            progress_display_method = download_progress_hook if display_progress else None
            urllib.request.urlretrieve(uri, target_location, reporthook=progress_display_method)
            ok = True
            break
        except Exception as ex:
            if try_num == num_tries - 1:
                raise ex

            if isinstance(ex, urllib.error.HTTPError) and ex.code == 404:
                if fail_if_not_found:
                    logging.error("HTTP 404 error")
                    raise ex
                # Do not retry; assume we won't progress
                break

            if display_progress:
                sys.stdout.write("\n")

            logging.error("Try {}/{} got error: {}".format(try_num + 1, num_tries, ex))
            sleep_time = (try_num + 1) * 2.0
            logging.info("Sleeping for {} seconds before next try".format(sleep_time))
            time.sleep(sleep_time)

    if display_progress:
        sys.stdout.write("\n") # Add newline after progress hook

    return ok


def download_one_url(uri, target_location, fail_if_not_found=True, is_azure_storage=False, display_progress=True):
    """ Do an URI download using urllib.request.urlretrieve or Azure Storage APIs.

    Args:
        uri (string)              : URI to download
        target_location (string)  : local path to put the downloaded object
        fail_if_not_found (bool)  : if True, fail if a download fails due to file not found (HTTP error 404).
                                    Otherwise, ignore the failure.
        display_progress (bool)   : if True, display download progress (for URL downloads). Otherwise, do not.

    Returns True if successful, False on failure
    """
    if is_azure_storage and authenticate_using_azure:
        return download_with_azure(uri, target_location, fail_if_not_found)
    else:
        return download_with_progress_urlretrieve(uri, target_location, fail_if_not_found, display_progress)


def download_files(paths, target_dir, verbose=True, fail_if_not_found=True, is_azure_storage=False, display_progress=True):
    """ Download a set of files, specified as URLs or paths (such as Windows UNC paths),
        to a target directory. If a file is a .ZIP file, then uncompress the file and
        copy all its contents to the target directory.

    Args:
        paths (list): the URLs and paths to download
        target_dir (str): target directory where files are copied. It will be created if it doesn't already exist.
        verbose (bool): if True, do verbose logging.
        fail_if_not_found (bool): if True, fail if a download fails due to file not found (HTTP error 404).
                                  Otherwise, ignore the failure.
        is_azure_storage (bool): if True, treat any URL as an Azure Storage URL
        display_progress (bool): if True, display download progress (for URL downloads). Otherwise, do not.

    Returns:
        list of full paths of local filenames of downloaded files in the target directory
    """

    if len(paths) == 0:
        logging.warning("No files specified to download")
        return None

    if verbose:
        logging.info("Downloading:")
        for item_path in paths:
            logging.info("  %s", item_path)

    # Create the target directory now, if it doesn't already exist.
    target_dir = os.path.abspath(target_dir)
    if not os.path.isdir(target_dir):
        os.makedirs(target_dir)

    local_paths = []

    # In case we'll need a temp directory for ZIP file processing, create it first.
    with TempDir() as temp_location:
        for item_path in paths:
            is_item_url = is_url(item_path)
            item_name = item_path.split("/")[-1] if is_item_url else os.path.basename(item_path)

            if item_path.lower().endswith(".zip"):
                # Delete everything in the temp_location (from previous iterations of this loop, so previous URL downloads).
                temp_location_items = [os.path.join(temp_location, item) for item in os.listdir(temp_location)]
                for item in temp_location_items:
                    if os.path.isdir(item):
                        shutil.rmtree(item)
                    else:
                        os.remove(item)

                download_path = os.path.join(temp_location, item_name)
                if is_item_url:
                    ok = download_one_url(item_path, download_path, fail_if_not_found=fail_if_not_found, is_azure_storage=is_azure_storage, display_progress=display_progress)
                    if not ok:
                        continue
                else:
                    if fail_if_not_found or os.path.isfile(item_path):
                        if verbose:
                            logging.info("Download: %s -> %s", item_path, download_path)
                        shutil.copy2(item_path, download_path)

                if verbose:
                    logging.info("Uncompress %s", download_path)
                with zipfile.ZipFile(download_path, "r") as file_handle:
                    file_handle.extractall(temp_location)

                # Copy everything that was extracted to the target directory.
                copy_directory(temp_location, target_dir, verbose_copy=verbose, match_func=lambda path: not path.endswith(".zip"))

                # The caller wants to know where all the files ended up, so compute that.
                for dirpath, _, files in os.walk(temp_location, topdown=True):
                    for file_name in files:
                        if not file_name.endswith(".zip"):
                            full_file_path = os.path.join(dirpath, file_name)
                            target_path = full_file_path.replace(temp_location, target_dir)
                            local_paths.append(target_path)
            else:
                # Not a zip file; download directory to target directory
                download_path = os.path.join(target_dir, item_name)
                if is_item_url:
                    ok = download_one_url(item_path, download_path, fail_if_not_found=fail_if_not_found, is_azure_storage=is_azure_storage, display_progress=display_progress)
                    if not ok:
                        continue
                else:
                    if fail_if_not_found or os.path.isfile(item_path):
                        if verbose:
                            logging.info("Download: %s -> %s", item_path, download_path)
                        shutil.copy2(item_path, download_path)
                local_paths.append(download_path)

    return local_paths
