#!/usr/bin/env bash

usage()
{
    echo "Usage: clean [options]"
    echo "Cleans the local dev environment."
    echo
    echo "  -b         Delete the binary output directory"
    echo "  -p         Delete the repo-local NuGet package directory"
    echo "  -c         Delete the user-local NuGet package caches"
    echo "  -t         Delete the tools directory"
    echo "  -s         Remove all untracked files under the src directory"
    echo "  -a, --all  Clean all of the above"
    echo
    echo "If no option is specified, then \"clean.sh -b\" is implied."
    exit 1
}

check_exit_status()
{
    ExitStatus=$?
    if [ $ExitStatus -ne 0 ]
    then
        echo "Command exited with exit status $ExitStatus" >> $CleanLog
        CleanSuccessful=false
    fi
}

WorkingTreeRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
CleanLog=$WorkingTreeRoot/clean.log

options="/nologo /v:minimal /clp:Summary /flp:v=detailed;Append;LogFile=$CleanLog"
unprocessedBuildArgs=
CleanSuccessful=true
CleanTargets=

echo "Running clean.sh $*" > $CleanLog

# Parse arguments
if [ $# == 0 ]
then
    CleanTargets="Clean;"
fi

while [[ $# > 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
        usage
        ;;
        -b)
        CleanTargets="Clean;$CleanTargets"
        ;;
        -p)
        CleanTargets="CleanPackages;$CleanTargets"
        ;;
        -c)
        CleanTargets="CleanPackagesCache;$CleanTargets"
        ;;
        -t)
        CleanToolsDir=true
        ;;
        -s)
        CleanSrc=true
        ;;
        -a|--all)
        CleanWorkingTree=true
        CleanTargets="Clean;CleanPackages;CleanPackagesCache;"
        ;;
        *)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
    esac
    shift
done

if [ -n "$CleanTargets" ]
then
    # Ensure that MSBuild is available
    echo "Running init-tools.sh"
    $WorkingTreeRoot/init-tools.sh

    # Trim the trailing semicolon from the targets string
    CleanTargetsTrimmed=${CleanTargets:0:${#CleanTargets}-1}

    echo "Running MSBuild target(s): $CleanTargetsTrimmed"
    echo -e "\n$WorkingTreeRoot/Tools/dotnetcli/dotnet $WorkingTreeRoot/Tools/MSBuild.exe $WorkingTreeRoot/build.proj /t:$CleanTargetsTrimmed $options $unprocessedBuildArgs" >> $CleanLog
    $WorkingTreeRoot/Tools/dotnetcli/dotnet $WorkingTreeRoot/Tools/MSBuild.exe $WorkingTreeRoot/build.proj /t:$CleanTargetsTrimmed $options $unprocessedBuildArgs
    check_exit_status
fi

if [ "$CleanToolsDir" == true ] && [ "$CleanWorkingTree" != true ]
then
    echo "Removing Tools directory"
    # This directory cannot be removed in a build target because MSBuild is in the Tools directory
    echo -e "\nrm -rf $WorkingTreeRoot/Tools" >> $CleanLog
    rm -rf $WorkingTreeRoot/Tools >> $CleanLog
    check_exit_status
fi

if [ "$CleanSrc" == true ] && [ "$CleanWorkingTree" != true ]
then
    echo "Removing all untracked files in the src directory"
    echo -e "\ngit clean -xdf $WorkingTreeRoot/src" >> $CleanLog
    git clean -xdf $WorkingTreeRoot/src >> $CleanLog
    check_exit_status
fi

if [ "$CleanWorkingTree" == true ]
then
    echo "Removing all untracked files in the working tree"
    echo -e "\ngit clean -xdf -e clean.log $WorkingTreeRoot" >> $CleanLog
    git clean -xdf -e clean.log $WorkingTreeRoot >> $CleanLog
    check_exit_status
fi

if [ "$CleanSuccessful" == true ]
then
    echo "Clean completed successfully."
    echo -e "\nClean completed successfully." >> $CleanLog
    exit 0
else
    echo "An error occured while cleaning; see $CleanLog for more details."
    echo -e "\nClean completed with errors." >> $CleanLog
    exit 1
fi
