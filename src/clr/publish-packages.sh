#!/usr/bin/env bash

usage()
{
    echo "Publishes the NuGet packages to the specified location."
    echo "For publishing to Azure the following properties are required."
    echo "   -AzureAccount=\"account name\""
    echo "   -AzureToken=\"access token\""
    echo "   -BuildType=\"Configuration\""
    echo "   -BuildArch=\"Architecture\""
    echo "For publishing to Azure, one of the following properties is required."
    echo "   -PublishPackages        Pass this switch to publish product packages" 
    echo "   -PublishSymbols         Pass this switch to publish symbol packages"
    echo "To specify the name of the container to publish into, use the following property:"
    echo "   -Container=\"container name\""
    echo "To specify the OS you're building for, use the following property:"
    echo "   -DistroRiD=\"RID\""	
    echo "Configuration can be Release, Checked, or Debug"
    echo "Architecture can be x64, x86, arm, or arm64"
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

$working_tree_root/run.sh publish-packages -BuildOS $*
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while publishing packages; see $working_tree_root/publish-packages.log for more details. There may have been networking problems, so please try again in a few minutes."
    exit 1
fi

echo "Publish completed successfully."
exit 0