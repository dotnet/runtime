#!/usr/bin/env bash

init_distro_name()
{
    if [ ! -e /etc/os-release ]; then
        echo "WARNING: Can not determine runtime id for current distro."
        export __distro_rid=""
    else
        source /etc/os-release
        export __distro_rid="$ID.$VERSION_ID-x64"
    fi
}

set -e

# determine current directory
SOURCE="${BASH_SOURCE[0]}"
while [ -h "$SOURCE" ]; do # resolve $SOURCE until the file is no longer a symlink
  DIR="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
  SOURCE="$(readlink "$SOURCE")"
  [[ "$SOURCE" != /* ]] && SOURCE="$DIR/$SOURCE" # if $SOURCE was a relative symlink, we need to resolve it relative to the path where the symlink file was located
done

# initialize variables
__project_dir="$( cd -P "$( dirname "$SOURCE" )" && pwd )"
__distro_rid=

# setup msbuild
"$__project_dir/init-tools.sh"

# acquire dependencies
pushd "$__project_dir/deps"
"$__project_dir/Tools/dotnetcli/dotnet" restore --source "https://dotnet.myget.org/F/dotnet-core/api/v3/index.json" --disable-parallel --packages "$__project_dir/packages"
popd

# cleanup existing packages
rm -rf $__project_dir/bin

# build to produce nupkgs
__msbuild="$__project_dir/Tools/msbuild.sh"

__targets_param=
if [ "$(uname -s)" == "Darwin" ]; then
    __targets_param="TargetsOSX=true"
else
    __targets_param="TargetsLinux=true"
    init_distro_name
fi

__common_parameters="/p:$__targets_param /p:DistroRid=$__distro_rid /verbosity:minimal"

$__msbuild $__project_dir/projects/packages.builds $__common_parameters || exit 1

exit 0