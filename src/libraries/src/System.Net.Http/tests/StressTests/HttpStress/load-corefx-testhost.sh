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
COREFX_ROOT_DIR=$(git -C "$SOURCE_DIR" rev-parse --show-toplevel)

usage()
{
    echo "Usage:"
    echo "    -f <moniker>  Target framework: defaults to netcoreapp"
    echo "    -c <config>   Build configuration: defaults to Debug"
    echo "    -a <arch>     Build architecture: defaults to netcoreapp"
    echo "    -o <os>       Operating system"
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

OPTIND=1
while getopts "hf:c:a:o:" opt; do
    case $opt in
        f) FRAMEWORK=$OPTARG ;;
        c) CONFIGURATION=$OPTARG ;;
        a) ARCH=$OPTARG ;;
        o) OS=$OPTARG ;;
        h) usage ; return 0 ;;
        *) usage ; return 1 ;;
    esac
done

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
