#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
$__scriptpath/sync.sh
if [ "$?" != "0" ]; then
    echo "ERROR: An error occured when trying to initialize the tools. Please check '$__scriptpath/init-tools.log' for more details."
    exit 1
fi
$__scriptpath/build-managed.sh -Project:$__scriptpath/src/SharedFrameworkValidation/SharedFrameworkValidation.proj  -- /t:CreateScriptToDownloadSharedFrameworkZip
if [ "$?" != "0" ]; then
    echo "ERROR: An error occured when trying to write the script to download the shared framework. Please check '$__scriptpath/msbuild.log' for more details."
    exit 1
fi
chmod a+x $__scriptpath/bin/DownloadSharedFramework.sh
$__scriptpath/bin/DownloadSharedFramework.sh
$__scriptpath/build-managed.sh -Project:$__scriptpath/src/SharedFrameworkValidation/SharedFrameworkValidation.proj
if [ "$?" != "0" ]; then
    echo "ERROR: An error occured when trying to restore the shared framework. Please check '$__scriptpath/msbuild.log' for more details."
    exit 1
fi
chmod a+x $__scriptpath/bin/CloneAndRunTests.sh
$__scriptpath/bin/CloneAndRunTests.sh
