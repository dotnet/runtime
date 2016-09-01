#!/usr/bin/env bash

initDistroName()
{
    if [ "$1" == "Linux" ]; then
        if [ ! -e /etc/os-release ]; then
            echo "WARNING: Can not determine runtime id for current distro."
            export __DistroRid=""
        else
            source /etc/os-release
            export __DistroRid="$ID.$VERSION_ID-x64"
        fi
    fi
}

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to set.
# This is needed by CLI to function.
if [ -z "$HOME" ]; then
    if [ ! -d "$__scriptpath/temp_home" ]; then
        mkdir temp_home
    fi
    export HOME=$__scriptpath/temp_home
    echo "HOME not defined; setting it to $HOME"
fi

__PACKAGES_DIR=$__scriptpath/packages
__TOOLRUNTIME_DIR=$__scriptpath/Tools
__DOTNET_PATH=$__TOOLRUNTIME_DIR/dotnetcli
__DOTNET_CMD=$__DOTNET_PATH/dotnet
if [ -z "$__BUILDTOOLS_SOURCE" ]; then __BUILDTOOLS_SOURCE=https://www.myget.org/F/dotnet-buildtools/; fi
__BUILD_TOOLS_PACKAGE_VERSION=$(cat $__scriptpath/BuildToolsVersion.txt)
__DOTNET_TOOLS_VERSION=$(cat $__scriptpath/DotnetCLIVersion.txt)
__BUILD_TOOLS_PATH=$__PACKAGES_DIR/Microsoft.DotNet.BuildTools/$__BUILD_TOOLS_PACKAGE_VERSION/lib
__PROJECT_JSON_PATH=$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION
__PROJECT_JSON_FILE=$__PROJECT_JSON_PATH/project.json
__PROJECT_JSON_CONTENTS="{ \"dependencies\": { \"Microsoft.DotNet.BuildTools\": \"$__BUILD_TOOLS_PACKAGE_VERSION\" }, \"frameworks\": { \"dnxcore50\": { } } }"
__DistroRid=""

OSName=$(uname -s)
case $OSName in
    Darwin)
        OS=OSX
        __DOTNET_PKG=dotnet-dev-osx-x64
        ;;

    Linux)
        OS=Linux

        if [ ! -e /etc/os-release ]; then
            echo "Cannot determine Linux distribution, asuming Ubuntu 14.04."
            __DOTNET_PKG=dotnet-dev-ubuntu.14.04-x64
        else
            source /etc/os-release
            __DOTNET_PKG="dotnet-dev-$ID.$VERSION_ID-x64"
        fi
        ;;

    *)
        echo "Unsupported OS $OSName detected. Downloading ubuntu-x64 tools"
        OS=Linux
        __DOTNET_PKG=dotnet-dev-ubuntu.14.04-x64
        ;;
esac

# Initialize Linux Distribution name and .NET CLI package name.

initDistroName $OS

# Work around mac build issue 
if [ "$OS" == "OSX" ]; then
  echo Raising file open limit - otherwise Mac build may fail
  echo ulimit -n 2048
  ulimit -n 2048
fi

__CLIDownloadURL=https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/${__DOTNET_TOOLS_VERSION}/${__DOTNET_PKG}.${__DOTNET_TOOLS_VERSION}.tar.gz
echo ".NET CLI will be downloaded from $__CLIDownloadURL"
echo "Locating $__PROJECT_JSON_FILE to see if we already downloaded .NET CLI tools..." 

if [ ! -e $__PROJECT_JSON_FILE ]; then
    echo "$__PROJECT_JSON_FILE not found. Proceeding to download .NET CLI tools. " 
    if [ -e $__TOOLRUNTIME_DIR ]; then rm -rf -- $__TOOLRUNTIME_DIR; fi

    if [ ! -e $__DOTNET_PATH ]; then
        # curl has HTTPS CA trust-issues less often than wget, so lets try that first.
        which curl > /dev/null 2> /dev/null
        if [ $? -ne 0 ]; then
          mkdir -p "$__DOTNET_PATH"
          wget -q -O $__DOTNET_PATH/dotnet.tar $__CLIDownloadURL
          echo "wget -q -O $__DOTNET_PATH/dotnet.tar $__CLIDownloadURL"
        else
          curl --retry 10 -sSL --create-dirs -o $__DOTNET_PATH/dotnet.tar $__CLIDownloadURL
          echo "curl --retry 10 -sSL --create-dirs -o $__DOTNET_PATH/dotnet.tar $__CLIDownloadURL"
        fi
        cd $__DOTNET_PATH
        tar -xf $__DOTNET_PATH/dotnet.tar
        if [ -n "$BUILDTOOLS_OVERRIDE_RUNTIME" ]; then
            find $__DOTNET_PATH -name *.ni.* | xargs rm 2>/dev/null
            cp -R $BUILDTOOLS_OVERRIDE_RUNTIME/* $__DOTNET_PATH/bin
            cp -R $BUILDTOOLS_OVERRIDE_RUNTIME/* $__DOTNET_PATH/bin/dnx
            cp -R $BUILDTOOLS_OVERRIDE_RUNTIME/* $__DOTNET_PATH/runtime/coreclr
        fi

        cd $__scriptpath
    fi

    mkdir "$__PROJECT_JSON_PATH"
    echo $__PROJECT_JSON_CONTENTS > "$__PROJECT_JSON_FILE"

    if [ ! -e $__BUILD_TOOLS_PATH ]; then
        $__DOTNET_CMD restore "$__PROJECT_JSON_FILE" --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE
    fi

    # On ubuntu 14.04, /bin/sh (symbolic link) calls /bin/dash by default.
    $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR

else
    echo "$__PROJECT_JSON_FILE found. Skipping .NET CLI installation."   
fi
