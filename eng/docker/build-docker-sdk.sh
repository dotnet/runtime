#!/usr/bin/env bash
# Builds libraries and produces a dotnet sdk docker image
# that contains the current bits in its shared framework folder.

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

imagename="dotnet-sdk-libs-current"
configuration="Release"
repo_root=$(git -C "$scriptroot" rev-parse --show-toplevel)
major_version=$(grep -oP '(?<=<MajorVersion>).*?(?=</MajorVersion>)' "$repo_root/eng/Versions.props")
minor_version=$(grep -oP '(?<=<MinorVersion>).*?(?=</MinorVersion>)' "$repo_root/eng/Versions.props")
version="$major_version.$minor_version"

while [[ $# -gt 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -imagename|-t)
      imagename=$2
      shift 2
      ;;
    -configuration|-c)
      configuration=$2
      shift 2
      ;;
    *)
      shift 1
      ;;
  esac
done

docker_file="$scriptroot/libraries-sdk.linux.Dockerfile"

docker build --tag $imagename \
    --build-arg CONFIGURATION=$configuration \
    --build-arg VERSION=$version \
    --file $docker_file \
    $repo_root

exit $?
