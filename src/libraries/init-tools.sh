#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__init_tools_log=$__scriptpath/init-tools.log
__PACKAGES_DIR=$__scriptpath/packages
__TOOLRUNTIME_DIR=$__scriptpath/Tools
__DOTNET_PATH=$__TOOLRUNTIME_DIR/dotnetcli
__DOTNET_CMD=$__DOTNET_PATH/dotnet
if [ -z "$__BUILDTOOLS_SOURCE" ]; then __BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json; fi
__BUILD_TOOLS_PACKAGE_VERSION=$(cat $__scriptpath/BuildToolsVersion.txt)
__DOTNET_TOOLS_VERSION=$(cat $__scriptpath/DotnetCLIVersion.txt)
__BUILD_TOOLS_PATH=$__PACKAGES_DIR/Microsoft.DotNet.BuildTools/$__BUILD_TOOLS_PACKAGE_VERSION/lib
__PROJECT_JSON_PATH=$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION
__PROJECT_JSON_FILE=$__PROJECT_JSON_PATH/project.json
__PROJECT_JSON_CONTENTS="{ \"dependencies\": { \"Microsoft.DotNet.BuildTools\": \"$__BUILD_TOOLS_PACKAGE_VERSION\" }, \"frameworks\": { \"netcoreapp1.0\": { } } }"
__INIT_TOOLS_DONE_MARKER=$__PROJECT_JSON_PATH/done

# Extended version of platform detection logic from dotnet/cli/scripts/obtain/dotnet-install.sh 16692fc
get_current_linux_name() {
    # Detect Distro
    if [ "$(cat /etc/*-release | grep -cim1 ubuntu)" -eq 1 ]; then
        if [ "$(cat /etc/*-release | grep -cim1 16.04)" -eq 1 ]; then
            echo "ubuntu.16.04"
            return 0
        fi
        if [ "$(cat /etc/*-release | grep -cim1 16.10)" -eq 1 ]; then
            echo "ubuntu.16.10"
            return 0
        fi

        echo "ubuntu"
        return 0
    elif [ "$(cat /etc/*-release | grep -cim1 centos)" -eq 1 ]; then
        echo "centos"
        return 0
    elif [ "$(cat /etc/*-release | grep -cim1 rhel)" -eq 1 ]; then
        echo "rhel"
        return 0
    elif [ "$(cat /etc/*-release | grep -cim1 debian)" -eq 1 ]; then
        echo "debian"
        return 0
    elif [ "$(cat /etc/*-release | grep -cim1 alpine)" -eq 1 ]; then
        echo "alpine"
        return 0
    elif [ "$(cat /etc/*-release | grep -cim1 fedora)" -eq 1 ]; then
        if [ "$(cat /etc/*-release | grep -cim1 23)" -eq 1 ]; then
            echo "fedora.23"
            return 0
        fi
        if [ "$(cat /etc/*-release | grep -cim1 24)" -eq 1 ]; then
            echo "fedora.24"
            return 0
        fi
    elif [ "$(cat /etc/*-release | grep -cim1 opensuse)" -eq 1 ]; then
        if [ "$(cat /etc/*-release | grep -cim1 13.2)" -eq 1 ]; then
            echo "opensuse.13.2"
            return 0
        fi
        if [ "$(cat /etc/*-release | grep -cim1 42.1)" -eq 1 ]; then
            echo "opensuse.42.1"
            return 0
        fi
    fi

    # Cannot determine Linux distribution, assuming Ubuntu 14.04.
    echo "ubuntu"
    return 0
}

if [ -z "$__DOTNET_PKG" ]; then
OSName=$(uname -s)
    case $OSName in
        Darwin)
            OS=OSX
            __DOTNET_PKG=dotnet-dev-osx-x64
            ulimit -n 2048
            ;;

        Linux)
            __DOTNET_PKG="dotnet-dev-$(get_current_linux_name)-x64"
            OS=Linux
            ;;

        *)
            echo "Unsupported OS '$OSName' detected. Downloading ubuntu-x64 tools."
            OS=Linux
            __DOTNET_PKG=dotnet-dev-ubuntu-x64
            ;;
  esac
fi

if [ ! -e $__INIT_TOOLS_DONE_MARKER ]; then
    __PATCH_CLI_NUGET_FRAMEWORKS=0

    if [ -e $__TOOLRUNTIME_DIR ]; then rm -rf -- $__TOOLRUNTIME_DIR; fi
    echo "Running: $__scriptpath/init-tools.sh" > $__init_tools_log

    if [ ! -e $__DOTNET_PATH ]; then

        mkdir -p "$__DOTNET_PATH"

        if [ -n "$DOTNET_TOOLSET_DIR" ] && [ -d "$DOTNET_TOOLSET_DIR/$__DOTNET_TOOLS_VERSION" ]; then
            echo "Copying $DOTNET_TOOLSET_DIR/$__DOTNET_TOOLS_VERSION to $__DOTNET_PATH" >> $__init_tools_log
            cp -r $DOTNET_TOOLSET_DIR/$__DOTNET_TOOLS_VERSION/* $__DOTNET_PATH
        elif [ -n "$DOTNET_TOOL_DIR" ] && [ -d "$DOTNET_TOOL_DIR" ]; then
            echo "Copying $DOTNET_TOOL_DIR to $__DOTNET_PATH" >> $__init_tools_log
            cp -r $DOTNET_TOOL_DIR/* $__DOTNET_PATH
        else
            echo "Installing dotnet cli..."
            __DOTNET_LOCATION="https://dotnetcli.blob.core.windows.net/dotnet/Sdk/${__DOTNET_TOOLS_VERSION}/${__DOTNET_PKG}.${__DOTNET_TOOLS_VERSION}.tar.gz"
            # curl has HTTPS CA trust-issues less often than wget, so lets try that first.
            echo "Installing '${__DOTNET_LOCATION}' to '$__DOTNET_PATH/dotnet.tar'" >> $__init_tools_log
            which curl > /dev/null 2> /dev/null
            if [ $? -ne 0 ]; then
                wget -q -O $__DOTNET_PATH/dotnet.tar ${__DOTNET_LOCATION}
            else
                curl --retry 10 -sSL --create-dirs -o $__DOTNET_PATH/dotnet.tar ${__DOTNET_LOCATION}
            fi
            cd $__DOTNET_PATH
            tar -xf $__DOTNET_PATH/dotnet.tar

            cd $__scriptpath

            __PATCH_CLI_NUGET_FRAMEWORKS=1
        fi
    fi


    if [ -n "$BUILD_TOOLS_TOOLSET_DIR" ] && [ -d "$BUILD_TOOLS_TOOLSET_DIR/$__BUILD_TOOLS_PACKAGE_VERSION" ]; then
        echo "Copying $BUILD_TOOLS_TOOLSET_DIR/$__BUILD_TOOLS_PACKAGE_VERSION to $__TOOLRUNTIME_DIR" >> $__init_tools_log
        cp -r $BUILD_TOOLS_TOOLSET_DIR/$__BUILD_TOOLS_PACKAGE_VERSION/* $__TOOLRUNTIME_DIR
    elif [ -n "$BUILD_TOOLS_TOOL_DIR" ] && [ -d "$BUILD_TOOLS_TOOL_DIR" ]; then
        echo "Copying $BUILD_TOOLS_TOOL_DIR to $__TOOLRUNTIME_DIR" >> $__init_tools_log
        cp -r $BUILD_TOOLS_TOOL_DIR/* $__TOOLRUNTIME_DIR
    else
        if [ ! -d "$__PROJECT_JSON_PATH" ]; then mkdir "$__PROJECT_JSON_PATH"; fi
        echo $__PROJECT_JSON_CONTENTS > "$__PROJECT_JSON_FILE"

        if [ ! -e $__BUILD_TOOLS_PATH ]; then
            echo "Restoring BuildTools version $__BUILD_TOOLS_PACKAGE_VERSION..."
            echo "Running: $__DOTNET_CMD restore \"$__PROJECT_JSON_FILE\" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE" >> $__init_tools_log
            $__DOTNET_CMD restore "$__PROJECT_JSON_FILE" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE >> $__init_tools_log
            if [ ! -e "$__BUILD_TOOLS_PATH/init-tools.sh" ]; then echo "ERROR: Could not restore build tools correctly. See '$__init_tools_log' for more details."1>&2; fi
        fi

        echo "Initializing BuildTools..."
        echo "Running: $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR" >> $__init_tools_log
        $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR >> $__init_tools_log
        if [ "$?" != "0" ]; then
            echo "ERROR: An error occured when trying to initialize the tools. Please check '$__init_tools_log' for more details."1>&2
            exit 1
        fi
    fi

    if [ $__PATCH_CLI_NUGET_FRAMEWORKS -eq 1 ]; then
        echo "Updating CLI NuGet Frameworks map..."
        cp $__TOOLRUNTIME_DIR/NuGet.Frameworks.dll $__TOOLRUNTIME_DIR/dotnetcli/sdk/$__DOTNET_TOOLS_VERSION >> $__init_tools_log
        if [ "$?" != "0" ]; then
            echo "ERROR: An error occured when updating Nuget for CLI . Please check '$__init_tools_log' for more details."1>&2
            exit 1
        fi
    fi

    touch $__INIT_TOOLS_DONE_MARKER

    echo "Done initializing tools."
else
    echo "Tools are already initialized"
fi
