#!/usr/bin/env bash

__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__init_tools_log="$__scriptpath/init-tools.log"
__PACKAGES_DIR="$__scriptpath/packages"
__TOOLRUNTIME_DIR="$__scriptpath/Tools"
__DOTNET_PATH="$__TOOLRUNTIME_DIR/dotnetcli"
__DOTNET_CMD="$__DOTNET_PATH/dotnet"
if [ -z "${__BUILDTOOLS_SOURCE:-}" ]; then __BUILDTOOLS_SOURCE=https://dotnet.myget.org/F/dotnet-buildtools/api/v3/index.json; fi
export __BUILDTOOLS_USE_CSPROJ=true
__BUILD_TOOLS_PACKAGE_VERSION=$(cat "$__scriptpath/BuildToolsVersion.txt" | sed 's/\r$//') # remove CR if mounted repo on Windows drive
__DOTNET_TOOLS_VERSION=$(cat "$__scriptpath/DotnetCLIVersion.txt" | sed 's/\r$//') # remove CR if mounted repo on Windows drive
__ILASM_VERSION=$(cat "$__scriptpath/tools-local/ILAsmVersion.txt" | sed 's/\r$//') # remove CR if mounted repo on Windows drive
__BUILD_TOOLS_PATH="$__PACKAGES_DIR/microsoft.dotnet.buildtools/$__BUILD_TOOLS_PACKAGE_VERSION/lib"
__INIT_TOOLS_RESTORE_PROJECT="$__scriptpath/init-tools.msbuild"
__BUILD_TOOLS_SEMAPHORE="$__TOOLRUNTIME_DIR/$__BUILD_TOOLS_PACKAGE_VERSION/init-tools.complete"

if [ -e "$__BUILD_TOOLS_SEMAPHORE" ]; then
    echo "Tools are already initialized"
    return #return instead of exit because this script is inlined in other scripts which we don't want to exit
fi

if [ -e "$__TOOLRUNTIME_DIR" ]; then rm -rf -- "$__TOOLRUNTIME_DIR"; fi

if [ -d "${DotNetBuildToolsDir:-}" ]; then
    echo "Using tools from '$DotNetBuildToolsDir'."
    ln -s "$DotNetBuildToolsDir" "$__TOOLRUNTIME_DIR"

    if [ ! -e "$__DOTNET_CMD" ]; then
        echo "ERROR: Ensure that $DotNetBuildToolsDir contains the .NET Core SDK at $__DOTNET_PATH"
        exit 1
    fi

    echo "Done initializing tools."
    mkdir -p "$(dirname "$__BUILD_TOOLS_SEMAPHORE")" && touch "$__BUILD_TOOLS_SEMAPHORE"
    return #return instead of exit because this script is inlined in other scripts which we don't want to exit
fi

echo "Running: $__scriptpath/init-tools.sh" > "$__init_tools_log"

display_error_message()
{
    echo "Please check the detailed log that follows." 1>&2
    cat "$__init_tools_log" 1>&2
}

# Executes a command and retries if it fails.
execute_with_retry() {
    local count=0
    local retries=${retries:-5}
    local waitFactor=${waitFactor:-6}
    until "$@"; do
        local exit=$?
        count=$(( $count + 1 ))
        if [ $count -lt $retries ]; then
            local wait=$(( waitFactor ** (( count - 1 )) ))
            echo "Retry $count/$retries exited $exit, retrying in $wait seconds..."
            sleep $wait
        else
            say_err "Retry $count/$retries exited $exit, no more retries left."
            return $exit
        fi
    done

    return 0
}

if [ ! -e "$__DOTNET_PATH" ]; then
    if [ -z "${__DOTNET_PKG:-}" ]; then
        if [ "$(uname -m | grep "i[3456]86")" = "i686" ]; then
            echo "Warning: build not supported on 32 bit Unix"
        fi

        __PKG_ARCH=x64

        OSName=$(uname -s)
        case $OSName in
            Darwin)
                OS=OSX
                __PKG_RID=osx
                ulimit -n 2048
                # Format x.y.z as single integer with three digits for each part
                VERSION=`sw_vers -productVersion| sed -e 's/\./ /g' | xargs printf "%03d%03d%03d"`
                if [ "$VERSION" -lt 010012000 ]; then
                    echo error: macOS version `sw_vers -productVersion` is too old. 10.12 is needed as minimum.
                    exit 1
                fi
                ;;

            Linux)
                __PKG_RID=linux
                OS=Linux

                if [ -e /etc/os-release ]; then
                    source /etc/os-release
                    if [[ $ID == "alpine" ]]; then
                        __PKG_RID=linux-musl
                    fi
                elif [ -e /etc/redhat-release ]; then
                    redhatRelease=$(</etc/redhat-release)
                    if [[ $redhatRelease == "CentOS release 6."* || $redhatRelease == "Red Hat Enterprise Linux Server release 6."* ]]; then
                        __PKG_RID=rhel.6
                    fi
                fi

                ;;

            *)
            echo "Unsupported OS '$OSName' detected. Downloading linux-$__PKG_ARCH tools."
                OS=Linux
                __PKG_RID=linux
                ;;
        esac

        __DOTNET_PKG=dotnet-sdk-${__DOTNET_TOOLS_VERSION}-$__PKG_RID-$__PKG_ARCH
    fi
    mkdir -p "$__DOTNET_PATH"

    echo "Installing dotnet cli..."
    __DOTNET_LOCATION="https://dotnetcli.azureedge.net/dotnet/Sdk/${__DOTNET_TOOLS_VERSION}/${__DOTNET_PKG}.tar.gz"

    install_dotnet_cli() {
        if [[ -z "${DotNetBootstrapCliTarPath-}" ]]; then
            echo "Installing '${__DOTNET_LOCATION}' to '$__DOTNET_PATH/dotnet.tar'"
            rm -rf -- "$__DOTNET_PATH/*"
            # curl has HTTPS CA trust-issues less often than wget, so lets try that first.
            if command -v curl > /dev/null; then
                curl --retry 10 -sSL --create-dirs -o $__DOTNET_PATH/dotnet.tar ${__DOTNET_LOCATION}
            else
                wget -q -O $__DOTNET_PATH/dotnet.tar ${__DOTNET_LOCATION}
            fi
        else
            echo "Copying '$DotNetBootstrapCliTarPath' to '$__DOTNET_PATH/dotnet.tar'"
            cp $DotNetBootstrapCliTarPath $__DOTNET_PATH/dotnet.tar
        fi
        cd "$__DOTNET_PATH"
        tar -xf "$__DOTNET_PATH/dotnet.tar"
    }
    execute_with_retry install_dotnet_cli >> "$__init_tools_log" 2>&1

    cd "$__scriptpath"
fi

if [ ! -e "$__BUILD_TOOLS_PATH" ]; then
    echo "Restoring BuildTools version $__BUILD_TOOLS_PACKAGE_VERSION..."
    echo "Running: $__DOTNET_CMD restore \"$__INIT_TOOLS_RESTORE_PROJECT\" --no-cache --packages $__PACKAGES_DIR --source $__BUILDTOOLS_SOURCE /p:BuildToolsPackageVersion=$__BUILD_TOOLS_PACKAGE_VERSION /p:ToolsDir=$__TOOLRUNTIME_DIR" >> "$__init_tools_log"
    "$__DOTNET_CMD" restore "$__INIT_TOOLS_RESTORE_PROJECT" --no-cache --packages "$__PACKAGES_DIR" --source "$__BUILDTOOLS_SOURCE" /p:BuildToolsPackageVersion=$__BUILD_TOOLS_PACKAGE_VERSION /p:ToolsDir="$__TOOLRUNTIME_DIR" >> "$__init_tools_log"
    if [ ! -e "$__BUILD_TOOLS_PATH/init-tools.sh" ]; then
        echo "ERROR: Could not restore build tools correctly." 1>&2
        display_error_message
    fi
fi

if [ -z "${__ILASM_RID-}" ]; then
    __ILASM_RID=$__PKG_RID-$__PKG_ARCH
fi

echo "Using RID $__ILASM_RID for BuildTools native tools"

export ILASMCOMPILER_VERSION=$__ILASM_VERSION
export NATIVE_TOOLS_RID=$__ILASM_RID

if [ -n "${DotNetBootstrapCliTarPath-}" ]; then
    # Assume ilasm is not in nuget yet when bootstrapping...
    unset ILASMCOMPILER_VERSION
fi

echo "Initializing BuildTools..."
echo "Running: $__BUILD_TOOLS_PATH/init-tools.sh $__scriptpath $__DOTNET_CMD $__TOOLRUNTIME_DIR $__PACKAGES_DIR" >> "$__init_tools_log"

# Executables restored with .NET Core 2.0 do not have executable permission flags. https://github.com/NuGet/Home/issues/4424
chmod +x "$__BUILD_TOOLS_PATH/init-tools.sh"
"$__BUILD_TOOLS_PATH/init-tools.sh" "$__scriptpath" "$__DOTNET_CMD" "$__TOOLRUNTIME_DIR" "$__PACKAGES_DIR" >> "$__init_tools_log"
if [ "$?" != "0" ]; then
    echo "ERROR: An error occurred when trying to initialize the tools." 1>&2
    display_error_message
    exit 1
fi

echo "Making all .sh files executable under Tools."
# Executables restored with .NET Core 2.0 do not have executable permission flags. https://github.com/NuGet/Home/issues/4424
ls "$__scriptpath/Tools/"*.sh | xargs chmod +x
ls "$__scriptpath/Tools/scripts/docker/"*.sh | xargs chmod +x

"$__scriptpath/Tools/crossgen.sh" "$__scriptpath/Tools"

mkdir -p "$(dirname "$__BUILD_TOOLS_SEMAPHORE")" && touch "$__BUILD_TOOLS_SEMAPHORE"

echo "Done initializing tools."

