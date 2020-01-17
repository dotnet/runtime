#!/usr/bin/env bash

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

# Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
export DOTNET_MULTILEVEL_LOOKUP=0

# Disable first run since we want to control all package sources
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

source $scriptroot/eng/common/tools.sh

InitializeDotNetCli true # Install
__dotnetDir=${_InitializeDotNetCli}

dotnetPath=${__dotnetDir}/dotnet
${dotnetPath} "$@"
