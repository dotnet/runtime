#!/usr/bin/env bash

usage()
{
    echo "Usage: sync [-p]"
    echo "Repository syncing script."
    echo "  -p         Restore all NuGet packages for the repository"
    echo "If no option is specified, then \"sync.sh -p\" is implied."
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
unprocessedBuildArgs=

# Parse arguments
# Assume the default '-p' argument if the only arguments specified are specified after double dash.
# Only position parameters can be specified after the double dash.
if [ $# == 0 ] || [ $1 == '--' ]; then
    buildArgs="-p"
fi

while [[ $# -gt 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
        usage
        ;;
        -p)
        buildArgs="-p"
        ;;
        *)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
    esac
    shift
done

$working_tree_root/run.sh sync $buildArgs $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while syncing packages; See $working_tree_root/sync.log for more details. There may have been networking problems, so please try again in a few minutes."
    exit 1
fi

echo "Sync completed successfully."
exit 0
