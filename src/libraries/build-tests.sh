#!/usr/bin/env bash

usage()
{
    echo "Builds the tests that are in the repository."
    echo "No option parameters."
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
build_tests_log=$working_tree_root/build-tests.log
binclashlog=$working_tree_root/binclash.log
binclashloggerdll=$working_tree_root/Tools/Microsoft.DotNet.Build.Tasks.dll

options="/m /nologo /v:minimal /clp:Summary /flp:v=detailed;Append;LogFile=$build_tests_log /l:BinClashLogger,$binclashloggerdll;LogFile=$binclashlog"
allargs="$@"

echo -e "Running build-tests.sh $allargs" > $build_tests_log

if [ "$allargs" == "-h" ] || [ "$allargs" == "--help" ]; then
    usage
fi

# Ensure that MSBuild is available
echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

echo -e "\n$working_tree_root/Tools/dotnetcli/dotnet $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/tests.builds $options $allargs" >> $build_tests_log
$working_tree_root/Tools/dotnetcli/dotnet $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/tests.builds $options $allargs


if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-tests.sh ." >> $build_tests_log
    echo "ERROR: An error occurred while building tests, see $build_tests_log for more details."
    exit 1
fi

echo "Done building tests."
echo -e "\nDone building tests." >> $build_tests_log
exit 0
