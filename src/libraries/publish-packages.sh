#!/usr/bin/env bash

usage()
{
    echo "Publishes the NuGet packages to the specified location."
    echo "For publishing to Azure the following properties are required."
    echo "  /p:CloudDropAccountName='account name'"
    echo "  /p:CloudDropAccessToken='access token'"
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
publish_packages_log=$working_tree_root/publish-packages.log
options="/nologo /flp:v=detailed;Append;LogFile=$publish_packages_log"
allargs="$@"

echo -e "Running publish-packages.sh $allargs" > $publish_packages_log

# Parse arguments
if [ "$allargs" == "-h" ] || [ "$allargs" == "--help" ]; then
    usage
fi

# Ensure that MSBuild is available
echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

echo -e "\n$working_tree_root/Tools/dotnetcli/dotnet $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/publish.proj $options $allargs" >> $publish_packages_log
$working_tree_root/Tools/dotnetcli/dotnet $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/publish.proj $options $allargs

if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting publish-packages.sh ." >> $publish_packages_log
    echo "ERROR: An error occurred while publishing packages, see $publish_packages_log for more details."
    exit 1
fi

echo "Done publishing packages."
echo -e "\nDone publishing packages." >> $publish_packages_log
exit 0
