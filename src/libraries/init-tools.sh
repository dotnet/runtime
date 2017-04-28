#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__init_tools_log=$__scriptpath/init-tools.log
__PACKAGES_DIR=$__scriptpath/packages
__TOOLRUNTIME_DIR=$__scriptpath/Tools
__DOTNET_PATH=$__TOOLRUNTIME_DIR/dotnetcli
__DOTNET_CMD=$__DOTNET_PATH/dotnet
if [ -z "$__BUILDTOOLS_SOURCE" ]; then __BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json; fi
export __BUILDTOOLS_USE_CSPROJ=true
__BUILD_TOOLS_PACKAGE_VERSION=$(cat $__scriptpath/BuildToolsVersion.txt)
__DOTNET_TOOLS_VERSION=$(cat $__scriptpath/DotnetCLIVersion.txt)
__BUILD_TOOLS_PATH=$__PACKAGES_DIR/microsoft.dotnet.buildtools/$__BUILD_TOOLS_PACKAGE_VERSION/lib
__INIT_TOOLS_RESTORE_PROJECT=$__scriptpath/init-tools.msbuild
__INIT_TOOLS_DONE_MARKER=$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION/done

if [ -z "$__DOTNET_PKG" ]; then
    if [ "$(uname -m | grep "i[3456]86")" = "i686" ]; then
        echo "Warning: build not supported on 32 bit Unix"
    fi
OSName=$(uname -s)
    case $OSName in
        Darwin)
            OS=OSX
            __DOTNET_PKG=dotnet-dev-osx-x64
            ulimit -n 2048
            ;;

        Linux)
            __DOTNET_PKG=dotnet-dev-linux-x64
            OS=Linux
            ;;

        *)
            echo "Unsupported OS '$OSName' detected. Downloading linux-x64 tools."
            OS=Linux
            __DOTNET_PKG=dotnet-dev-linux-x64
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
        if [ ! -e $__BUILD_TOOLS_PATH ]; then
            echo "Restoring BuildTools version $__BUILD_TOOLS_PACKAGE_VERSION..."
            echo "Running: $__DOTNET_CMD restore \"$__INIT_TOOLS_RESTORE_PROJECT\" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE /p:BuildToolsPackageVersion=$__BUILD_TOOLS_PACKAGE_VERSION" >> $__init_tools_log
            $__DOTNET_CMD restore "$__INIT_TOOLS_RESTORE_PROJECT" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE /p:BuildToolsPackageVersion=$__BUILD_TOOLS_PACKAGE_VERSION >> $__init_tools_log
            if [ ! -e "$__BUILD_TOOLS_PATH/init-tools.sh" ]; then echo "ERROR: Could not restore build tools correctly. See '$__init_tools_log' for more details."1>&2; fi
        fi

        echo "Initializing BuildTools..."
        echo "Running: $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR" >> $__init_tools_log

        # Executables restored with .NET Core 2.0 do not have executable permission flags. https://github.com/NuGet/Home/issues/4424
        chmod +x $__BUILD_TOOLS_PATH/init-tools.sh
        $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR >> $__init_tools_log
        if [ "$?" != "0" ]; then
            echo "ERROR: An error occured when trying to initialize the tools. Please check '$__init_tools_log' for more details."1>&2
            exit 1
        fi
    fi

    echo "Making all .sh files executable under Tools."
    # Executables restored with .NET Core 2.0 do not have executable permission flags. https://github.com/NuGet/Home/issues/4424
    ls $__scriptpath/Tools/*.sh | xargs chmod +x
    ls $__scriptpath/Tools/scripts/docker/*.sh | xargs chmod +x

    Tools/crossgen.sh $__scriptpath/Tools

    touch $__INIT_TOOLS_DONE_MARKER

    echo "Done initializing tools."
else
    echo "Tools are already initialized"
fi
