#!/usr/bin/env bash

usage()
{
    echo "Publishes the NuGet packages to the specified location."
    echo "For publishing to Azure the following properties are required."
    echo "   /p:CloudDropAccountName=\"account name\""
    echo "   /p:CloudDropAccessToken=\"access token\""
    echo "   /p:__BuildType=\"Configuration\""
    echo "   /p:__BuildArch=\"Architecture\""
    echo "Configuration can be Release, Checked, or Debug"
    echo "Architecture can be x64, x86, arm, or arm64"
    exit 1
}

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
publish_log=$working_tree_root/publish.log

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        ;;
esac

options="/nologo /v:minimal /flp:v=detailed;Append;LogFile=$publish_log"

echo "Running publish-packages.sh $*" > $publish_log

echo "Running init-tools.sh"
$working_tree_root/init-tools.sh

echo "Publishing packages..."
echo -e "\n$working_tree_root/Tools/dotnetcli/dotnet $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/publish.proj $options $*" /p:__BuildOS=$__BuildOS >> $publish_log
$working_tree_root/Tools/dotnetcli/dotnet $working_tree_root/Tools/MSBuild.exe $working_tree_root/src/publish.proj $options $* /p:__BuildOS=$__BuildOS
if [ $? -ne 0 ]
then
    echo -e "\nPackage publishing failed. Aborting." >> $publish_log
    echo "ERROR: An error occurred while publishing packages; see $publish_log for more details. There may have been networking problems, so please try again in a few minutes."
    exit 1
fi

echo "Publish completed successfully."
echo -e "\nPublish completed successfully." >> $publish_log
exit 0