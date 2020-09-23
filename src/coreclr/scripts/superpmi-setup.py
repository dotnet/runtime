import subprocess
import argparse

from os import listdir, path, walk
from os.path import isfile, join, getsize
from coreclr_arguments import *

# Start of parser object creation.

parser = argparse.ArgumentParser(description="description")

parser.add_argument("-src_directory", help="path to src")
parser.add_argument("-dst_directory", help="path to dst")
parser.add_argument("-dst_folder_name", dest='dst_folder_name', default="binaries", help="Folder under dst/N/")
parser.add_argument("-max_size", help="Max size of partition in MB")


def setup_args(args):
    """ Setup the args for SuperPMI to use.

    Args:
        args (ArgParse): args parsed by arg parser

    Returns:
        args (CoreclrArguments)

    """
    coreclr_args = CoreclrArguments(args, require_built_core_root=False, require_built_product_dir=False,
                                    require_built_test_dir=False, default_build_type="Checked")

    coreclr_args.verify(args,
                        "src_directory",
                        lambda src_directory: os.path.isdir(src_directory),
                        "src_directory doesn't exist")

    coreclr_args.verify(args,
                        "dst_directory",
                        lambda dst_directory: (not os.path.isdir(dst_directory)),
                        "dst_directory already exist")

    coreclr_args.verify(args,
                        "dst_folder_name",
                        lambda unused: True,
                        "Unable to set dst_folder_name")

    coreclr_args.verify(args,
                        "max_size",
                        lambda max_size: True,  # max_size.isnumeric() and int(max_size) > 0,
                        "Please enter valid positive numeric max_size",
                        modify_arg=lambda max_size: int(max_size) * 1000 * 1000 if max_size.isnumeric() else max_size
                        # Convert to MB
                        )
    return coreclr_args


def get_files_sorted_by_size(src_directory):
    def sorter_by_size(pair):
        pair.sort(key=lambda x: x[1], reverse=True)
        return pair

    filename_with_size = []
    for file_path, subdirs, files in walk(src_directory):
        for name in files:
            curr_file_path = path.join(file_path, name)
            if not isfile(curr_file_path) or not name.endswith(".dll"):
                continue
            size = getsize(curr_file_path)
            filename_with_size.append((curr_file_path, size))

    return sorter_by_size(filename_with_size)


def first_fit(sorted_by_size, max_size):
    end_result = {}
    cached_size = {}
    partition_index = 0
    for curr_file in sorted_by_size:
        file_name, file_size = curr_file

        # Find the right bucket
        found_bucket = False

        if file_size < max_size:
            for p_index in end_result:
                total_in_curr_par = sum(n for _, n in end_result[p_index])
                if (total_in_curr_par + file_size) < max_size:
                    end_result[p_index].append(curr_file)
                    # cached_size[p_index] = cached_size[p_index] + file_size
                    found_bucket = True
                    break

            if not found_bucket:
                end_result[len(end_result) - 1] = [curr_file]

    return end_result


def partition_files(coreclr_args):

    src_directory = coreclr_args.src_directory
    dst_directory = coreclr_args.dst_directory
    dst_folder_name = coreclr_args.dst_folder_name
    max_size = coreclr_args.max_size

    sorted_by_size = get_files_sorted_by_size(src_directory)
    partitions = first_fit(sorted_by_size, max_size)

    index = 0
    for p_index in partitions:
        file_names = list(set([path.basename(curr_file[0]) for curr_file in partitions[p_index]]))
        curr_dst_path = path.join(dst_directory, str(index), dst_folder_name)
        command = ["robocopy", src_directory, curr_dst_path] + file_names
        command += [
            "/S",  # copy from sub-directories
            "/R:2",  # no. of retries
            "/W:5",  # seconds before retry
            "/NS",  # don't log file sizes
            "/NC",  # don't log file classes
            "/NFL",  # don't log file names
            "/NDL",  # don't log directory names
            "/NJH"  # No Job Header.
        ]
        print(" ".join(command))
        with subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE) as proc:
            stdout, _ = proc.communicate()
            print(stdout.decode('utf-8'))
        index += 1

    total_partitions = str(len(partitions))
    print('Total partitions: %s' % total_partitions)
    set_pipeline_variable("SuperPmiJobCount", total_partitions)


def set_pipeline_variable(name, value):
    define_variable_format = "##vso[task.setvariable variable={0}]{1}"
    print(define_variable_format.format(name, value))


def main(args):
    coreclr_args = setup_args(args)
    partition_files(coreclr_args)


################################################################################
# __main__
################################################################################

if __name__ == "__main__":
    args = parser.parse_args()
    sys.exit(main(args))
