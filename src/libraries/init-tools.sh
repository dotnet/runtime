__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__PACKAGES_DIR=$__scriptpath/packages
__TOOLRUNTIME_DIR=$__scriptpath/Tools
__DOTNET_PATH=$__TOOLRUNTIME_DIR/dotnetcli
__DOTNET_CMD=$__DOTNET_PATH/bin/dotnet
if [ -z "$__BUILDTOOLS_SOURCE" ]; then __BUILDTOOLS_SOURCE=https://www.myget.org/F/dotnet-buildtools/; fi
__BUILD_TOOLS_PACKAGE_VERSION=$(cat BuildToolsVersion.txt)
__BUILD_TOOLS_PATH=$__PACKAGES_DIR/Microsoft.DotNet.BuildTools/$__BUILD_TOOLS_PACKAGE_VERSION/lib
__PROJECT_JSON_PATH=$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION
__PROJECT_JSON_FILE=$__PROJECT_JSON_PATH/project.json
__PROJECT_JSON_CONTENTS="{ \"dependencies\": { \"Microsoft.DotNet.BuildTools\": \"$__BUILD_TOOLS_PACKAGE_VERSION\" }, \"frameworks\": { \"dnxcore50\": { } } }"

if [ ! -e $__PROJECT_JSON_FILE ]; then
 if [ -e $__TOOLRUNTIME_DIR ]; then rm -rf -- $__TOOLRUNTIME_DIR; fi

 if [ ! -e $__DOTNET_PATH ]; then
    mkdir "$__DOTNET_PATH" -p
    wget -q -O $__DOTNET_PATH/dotnet.tar https://dotnetcli.blob.core.windows.net/dotnet/dev/Binaries/Latest/dotnet-ubuntu-x64.latest.tar.gz
    cd $__DOTNET_PATH
    tar -xvf $__DOTNET_PATH/dotnet.tar
    cd $__scriptpath
 fi

 mkdir "$__PROJECT_JSON_PATH"
 echo $__PROJECT_JSON_CONTENTS > "$__PROJECT_JSON_FILE"

 if [ ! -e $__BUILD_TOOLS_PATH ]; then
    $__DOTNET_CMD restore "$__PROJECT_JSON_FILE" --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE
 fi

 sh $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR
fi
