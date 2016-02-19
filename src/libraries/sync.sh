#!/usr/bin/env bash

usage()
{
    echo "Usage: sync [-p] [-s]"
    echo "Repository syncing script."
    echo "  -s         Fetch source history from all configured remotes"
    echo "             (git fetch --all -p -v)"
    echo "  -p         Restore all NuGet packages for the repository"
    echo
    echo "If no option is specified, then \"sync.sh -p -s\" is implied."
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
sync_log=$working_tree_root/sync.log

# Parse arguments

echo "Running sync.sh $*" > $sync_log

if [ $# == 0 ]; then
    sync_packages=true
    sync_src=true
fi

while [[ $# > 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
        usage
        ;;
        -p)
        sync_packages=true
        ;;
        -s)
        sync_src=true
        ;;
        *)
        echo "Unrecognized argument '$opt'"
        echo "Use 'sync -h' for help."
        exit 1
        ;;
    esac
    shift
done

echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

if [ "$sync_src" == true ]; then
    echo "Fetching git database from remote repos..."
    git fetch --all -p -v &>> $sync_log
    if [ $? -ne 0 ]; then
        echo -e "\ngit fetch failed. Aborting sync." >> $sync_log
        echo "ERROR: An error occurred while fetching remote source code; see $sync_log for more details."
        exit 1
    fi
fi

if [ "$sync_packages" == true ]; then
    echo "Restoring all packages..."
    echo -e "\n$working_tree_root/Tools/corerun $working_tree_root/Tools/MSBuild.exe $working_tree_root/build.proj /t:BatchRestorePackages /nologo /v:minimal /p:RestoreDuringBuild=true /flp:v=detailed;Append;LogFile=$sync_log" >> $sync_log
    $working_tree_root/Tools/corerun $working_tree_root/Tools/MSBuild.exe $working_tree_root/build.proj /t:BatchRestorePackages /nologo /v:minimal /p:RestoreDuringBuild=true "/flp:v=detailed;Append;LogFile=$sync_log"
    if [ $? -ne 0 ]
    then
        echo -e "\nPackage restored failed. Aborting sync." >> $sync_log
        echo "ERROR: An error occurred while syncing packages; see $sync_log for more details. There may have been networking problems, so please try again in a few minutes."
        exit 1
    fi
fi

echo "Sync completed successfully."
echo -e "\nSync completed successfully." >> $sync_log
exit 0
