#!/usr/bin/env bash
# Runs the stress test using docker-compose

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

repo_root=$(git -C "$scriptroot" rev-parse --show-toplevel)
major_version=$(grep -oP '(?<=<MajorVersion>).*?(?=</MajorVersion>)' "$repo_root/eng/Versions.props")
minor_version=$(grep -oP '(?<=<MinorVersion>).*?(?=</MinorVersion>)' "$repo_root/eng/Versions.props")
version="$major_version.$minor_version"
imagename="dotnet-sdk-libs-current"
configuration="Release"
buildcurrentlibraries=0
buildonly=0
nobuild=0
clientstressargs=""
serverstressargs=""

projectdir=$1
shift 1
if [[ ! -d "$projectdir" ]]; then
    echo "First argument must be path to the stress project directory"
    exit 1
fi

dumpssharepath="$projectdir/dumps"

while [[ $# -gt 0 ]]; do
  opt="$(printf "%s" "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -dumpssharepath|-d)
      dumpssharepath=$2
      shift 2
      ;;
    -sdkimagename|-t)
      imagename=$2
      shift 2
      ;;
    -configuration|-c)
      configuration=$2
      shift 2
      ;;
    -buildcurrentlibraries|-b)
      buildcurrentlibraries=1
      shift 1
      ;;
    -buildonly|-o)
      buildonly=1
      shift 1
      ;;
    -nobuild|-n)
      nobuild=1
      shift 1
      ;;
    -clientstressargs)
      clientstressargs=$2
      shift 2
      ;;
    -serverstressargs)
      serverstressargs=$2
      shift 2
      ;;
    *)
      shift 1
      ;;
  esac
done

repo_root=$(git -C "$scriptroot" rev-parse --show-toplevel)

if [[ "$buildcurrentlibraries" -eq 1 ]]; then
    libraries_args=" -t $imagename -c $configuration"

    if ! "$repo_root"/eng/docker/build-docker-sdk.sh $libraries_args; then
        exit 1
    fi
fi

compose_file="$projectdir/docker-compose.yml"

if [[ "$nobuild" -eq 0 ]]; then
    build_args="--build-arg VERSION=$version --build-arg CONFIGURATION=$configuration"
    if [[ -n "$imagename" ]]; then
        build_args="$build_args --build-arg SDK_BASE_IMAGE=$imagename"
    fi

    if ! docker-compose --file "$compose_file" build $build_args; then
        exit $?
    fi
fi

if [[ "$buildonly" -eq 0 ]]; then
    if [[ -n "$dumpssharepath" ]]; then
        export DUMPS_SHARE="$dumpssharepath"
        export DUMPS_SHARE_MOUNT_ROOT="/dumps-share"
    fi

    export STRESS_CLIENT_ARGS=$clientstressargs
    export STRESS_SERVER_ARGS=$serverstressargs
    docker-compose --file "$compose_file" up --abort-on-container-exit
    exit $?
fi
