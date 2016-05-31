#!/usr/bin/env bash

usage()
{
    echo "Builds the NuGet packages from the binaries that were built in the Build product binaries step."
    echo "No option parameters."
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
build_packages_log=$working_tree_root/build-packages.log
binclashlog=$working_tree_root/binclash.log
binclashloggerdll=$working_tree_root/Tools/Microsoft.DotNet.Build.Tasks.dll
RuntimeOS=ubuntu.14.04

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Darwin)
        # Darwin version can be three sets of digits (e.g. 10.10.3), we want just the first two
        DarwinVersion=$(sw_vers -productVersion | awk 'match($0, /[0-9]+\.[0-9]+/) { print substr($0, RSTART, RLENGTH) }')
        RuntimeOS=osx.$DarwinVersion
        ;;

    FreeBSD|NetBSD)
        # TODO this doesn't seem correct
        RuntimeOS=osx.10.10
        ;;

    Linux)
        if [ ! -e /etc/os-release ]; then
            echo "Cannot determine Linux distribution, assuming Ubuntu 14.04"
        else
            source /etc/os-release
            # for some distros we only need the version major number
            VersionMajor=$(echo $VERSION_ID | awk 'match($0, /[0-9]+/) { print substr($0, RSTART, RLENGTH) }')
            if [ "$ID" == "rhel" ]; then
                RuntimeOS=$ID.$VersionMajor
            else
                RuntimeOS=$ID.$VERSION_ID
            fi
        fi
        ;;

    *)
        echo "Unsupported OS '$OSName' detected. Configuring as if for Ubuntu."
        ;;
esac

options="/m /nologo /v:minimal /clp:Summary /flp:v=diagnostic;Append;LogFile=$build_packages_log /l:BinClashLogger,$binclashloggerdll;LogFile=$binclashlog /p:FilterToOSGroup=$RuntimeOS"
allargs="$@"

echo -e "Running build-packages.sh $allargs" > $build_packages_log

if [ "$allargs" == "-h" ] || [ "$allargs" == "--help" ]; then
    usage
fi

# Ensure that MSBuild is available
echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

echo -e "\n$working_tree_root/Tools/corerun $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/packages.builds $options $allargs" >> $build_packages_log
$working_tree_root/Tools/corerun $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/packages.builds $options $allargs


if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-packages.sh ." >> $build_packages_log
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

echo "Done building packages."
echo -e "\nDone building packages." >> $build_packages_log
exit 0
