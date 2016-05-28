#!/usr/bin/env bash

usage()
{
   echo "Usage: ${BASH_SOURCE[0]} --arch x64/x86/arm --hostbindir path-to-binaries" --hostver --fxrver --policyver --build --vertag
   exit 1
}

init_distro_name()
{
    if [ ! -e /etc/os-release ]; then
        echo "WARNING: Can not determine runtime id for current distro."
        export __distro_rid=""
    else
        source /etc/os-release
        export __distro_rid="$ID.$VERSION_ID-$__build_arch"
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
__build_arch=
__dotnet_host_bin_dir=
__distro_rid=
__host_ver=
__fxr_ver=
__policy_ver=
__build_major=
__build_minor=
__version_tag=

# parse arguments
while [ "$1" != "" ]; do
        lowerI="$(echo $1 | awk '{print tolower($0)}')"
        case $lowerI in
        -h|--help)
            usage
            exit 1
            ;;
        --arch)
            shift
            __build_arch=$1
            ;;
        --hostbindir)
            shift
            __dotnet_host_bin_dir=$1
            ;;
        --hostver)
            shift
            __host_ver=$1
            ;;
        --fxrver)
            shift
            __fxr_ver=$1
            ;;
        --policyver)
            shift
            __policy_ver=$1
            ;;
        --build-major)
            shift
            __build_major=$1
            ;;
        --build-minor)
            shift
            __build_minor=$1
            ;;
        --vertag)
            shift
            __version_tag=$1
            ;;
        *)
        echo "Unknown argument to pack.sh $1"; exit 1
    esac
    shift
done

# validate args
if [ -z $__dotnet_host_bin_dir ]; then
    usage
fi
if [ -z $__build_arch ]; then
    usage
fi

# setup msbuild
"$__project_dir/init-tools.sh"

# acquire dependencies
pushd "$__project_dir/deps"
"$__project_dir/Tools/dotnetcli/dotnet" restore --source "https://dotnet.myget.org/F/dotnet-core" --packages "$__project_dir/packages"
popd

# cleanup existing packages
rm -rf $__project_dir/bin

# build to produce nupkgs
__corerun="$__project_dir/Tools/corerun"
__msbuild="$__project_dir/Tools/MSBuild.exe"

__targets_param=
if [ "$(uname -s)" == "Darwin" ]; then
    __targets_param="TargetsOSX=true"
else
    __targets_param="TargetsLinux=true"
    init_distro_name
fi

__common_parameters="/p:Platform=$__build_arch /p:DotNetHostBinDir=$__dotnet_host_bin_dir /p:$__targets_param /p:DistroRid=$__distro_rid /p:HostVersion=$__host_ver /p:HostResolverVersion=$__fxr_ver /p:HostPolicyVersion=$__policy_ver /p:BuildNumberMajor=$__build_major /p:BuildNumberMinor=$__build_minor /p:PreReleaseLabel=$__version_tag /p:CLIBuildVersion=$__build_major /verbosity:minimal"

$__corerun $__msbuild $__project_dir/projects/packages.builds $__common_parameters || exit 1

cp -rf "$__project_dir/bin/packages" "$__dotnet_host_bin_dir"

exit 0
