#!/usr/bin/env bash

usage()
{
    echo "Builds the NuGet packages from the binaries that were built in the Build product binaries step."
    echo "No option parameters."
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
build_packages_log=$working_tree_root/build-packages.log
arguments="$@"

echo -e "Running build-packages.sh $arguments" > $build_packages_log

# Parse arguments
if [ "$arguments" == "-h" ] || [ "$arguments" == "--help" ]; then
    usage
fi

# Ensure that MSBuild is available
echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

echo -e "\n$working_tree_root/Tools/corerun $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/packages.builds $arguments /nologo /v:minimal /flp:v=detailed;Append;LogFile=$build_packages_log" >> $build_packages_log
$working_tree_root/Tools/corerun $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/packages.builds $arguments /nologo /v:minimal "/flp:v=detailed;Append;LogFile=$build_packages_log"


if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-packages.sh ." >> $build_packages_log
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

echo "Done building packages."
echo -e "\nDone building packages." >> $build_packages_log
exit 0
