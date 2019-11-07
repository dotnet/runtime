#!/usr/bin/env bash
set -e

# Installs the NuGet Credential Provider, then calls common/msbuild.sh with all arguments. This
# creates a build context that can restore from authenticated sources. This script is intended for
# use by the official Microsoft build inside a Docker container.

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

. "$scriptroot/install-nuget-credprovider.sh"

"$scriptroot/common/msbuild.sh" "$@"
