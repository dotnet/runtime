# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

# Helper script used for pointing the current bash environment 
# to the testhost sdk built by the corefx build script.

# script needs to be sourced, detect if running standalone
if [[ $0 = $_ ]]; then
    echo "Script needs to be sourced"
    echo "USAGE: . $0 <args>"
    exit 1
fi

# find corefx root, assuming script lives in the git repo
SOURCE_DIR="$(cd -P "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COREFX_ROOT_DIR=$(cd -P "$SOURCE_DIR" && git rev-parse --show-toplevel)

usage()
{
    echo "Usage:"
    echo "    -f <moniker>  Target framework: defaults to netcoreapp"
    echo "    -c <config>   Build configuration: defaults to Debug"
    echo "    -a <arch>     Build architecture: defaults to netcoreapp"
    echo "    -o <os>       Operating system"
    echo "    -b            Copy AspNetCore bits from bootstrap SDK"
}

detect_os()
{
    case $(uname -s) in
        Linux*)     echo Linux;;
        Darwin*)    echo MacOS;;
        CYGWIN*)    echo Windows_NT;;
        MINGW*)     echo Windows_NT;;
        *)          echo Unix;;
    esac
}

# parse command line args
OS=$(detect_os)
ARCH=x64
FRAMEWORK=netcoreapp
CONFIGURATION=Debug
COPY_ASPNETCORE_BITS="false"

OPTIND=1
while getopts "hf:c:a:o:b" opt; do
    case $opt in
        f) FRAMEWORK=$OPTARG ;;
        c) CONFIGURATION=$OPTARG ;;
        a) ARCH=$OPTARG ;;
        o) OS=$OPTARG ;;
        b) COPY_ASPNETCORE_BITS="true" ;;
        h) usage ; return 0 ;;
        *) usage ; return 1 ;;
    esac
done

# the corefx testhost does not bundle AspNetCore runtime bits;
# fix up by copying from the bootstrap sdk 
copy_aspnetcore_bits()
{
    testhost_path=$1

    find_bootstrap_sdk()
    {
        if [ -d "$COREFX_ROOT_DIR/.dotnet" ]; then
            echo $COREFX_ROOT_DIR/.dotnet
        else
            echo $(dirname "$(readlink -f "$(which dotnet)")")
        fi
    }

    netfx_bits_folder="Microsoft.NETCore.App"
    aspnet_bits_folder="Microsoft.AspNetCore.App"

    if [ ! -d "$testhost_path/shared/$aspnet_bits_folder" ]; then

        bootstrap_sdk=$(find_bootstrap_sdk)
        netfx_runtime_version=$(ls "$testhost_path/shared/$netfx_bits_folder" | sort -V | tail -n1)
        aspnet_runtime_version=$(ls "$bootstrap_sdk/shared/$aspnet_bits_folder" | sort -V | tail -n1)

        # copy the bits
        mkdir -p "$testhost_path/shared/$aspnet_bits_folder/"
        cp -R "$bootstrap_sdk/shared/$aspnet_bits_folder/$aspnet_runtime_version" "$testhost_path/shared/$aspnet_bits_folder/$netfx_runtime_version"
        [ $? -ne 0 ] && return 1

        aspNetRuntimeConfig="$testhost_path/shared/$aspnet_bits_folder/$netfx_runtime_version/$aspnet_bits_folder.runtimeconfig.json"
        if [ -f "$aspNetRuntimeConfig" ]; then
            # point aspnetcore runtimeconfig.json to current netfx version
            # would prefer jq here but missing in many distros by default
            sed -i 's/"version"\s*:\s*"[^"]*"/"version":"'$netfx_runtime_version'"/g' "$aspNetRuntimeConfig"
        fi

        echo "Copied Microsoft.AspNetCore.App runtime bits from $bootstrap_sdk"
    fi
}

apply_to_environment()
{
    candidate_path="$COREFX_ROOT_DIR/artifacts/bin/testhost/$FRAMEWORK-$OS-$CONFIGURATION-$ARCH"

    if [ ! -d $candidate_path ]; then
        echo "Could not locate testhost sdk path $candidate_path" 
        return 1
    elif [ ! -f $candidate_path/dotnet -a ! -f $candidate_path/dotnet.exe ]; then
        echo "Could not find dotnet executable in testhost sdk path $candidate_path"
        return 1
    fi

    if [ $COPY_ASPNETCORE_BITS = "true" ]; then
        copy_aspnetcore_bits $candidate_path
    
        if [ $? -ne 0 ]; then
            echo "failed to copy aspnetcore bits"
            return 1
        fi
    fi

    export DOTNET_ROOT=$candidate_path
    export DOTNET_CLI_HOME=$candidate_path
    export DOTNET_MULTILEVEL_LOOKUP=0
    export DOTNET_ROLL_FORWARD_ON_NO_CANDIDATE_FX=2

    if which cygpath > /dev/null 2>&1; then
        # cygwin & mingw compat: PATH values must be unix style
        export PATH=$(cygpath -u $candidate_path):$PATH
    else
        export PATH=$candidate_path:$PATH
    fi
}

apply_to_environment
