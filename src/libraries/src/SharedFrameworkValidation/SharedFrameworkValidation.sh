#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__repo_root=$__scriptpath/../../
$__repo_root/sync.sh
if [ "$?" != "0" ]; then
    echo "ERROR: An error occured when trying to initialize the tools. Please check '$__repo_root/init-tools.log' for more details."
    exit 1
fi
$__repo_root/build-managed.sh -Project:$__repo_root/src/SharedFrameworkValidation/SharedFrameworkValidation.proj  -- /t:CreateScriptToDownloadSharedFrameworkZip
if [ "$?" != "0" ]; then
    echo "ERROR: An error occured when trying to write the script to download the shared framework. Please check '$__repo_root/msbuild.log' for more details."
    exit 1
fi
chmod a+x $__repo_root/bin/DownloadSharedFramework.sh
$__repo_root/bin/DownloadSharedFramework.sh
$__repo_root/build-managed.sh -Project:$__repo_root/src/SharedFrameworkValidation/SharedFrameworkValidation.proj
if [ "$?" != "0" ]; then
    echo "ERROR: An error occured when trying to restore the shared framework. Please check '$__repo_root/msbuild.log' for more details."
    exit 1
fi
chmod a+x $__repo_root/bin/CloneAndRunTests.sh
$__repo_root/bin/CloneAndRunTests.sh
