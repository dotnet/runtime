#!/usr/bin/env bash

set -ue

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

engFolder="$(cd -P "$( dirname "$scriptroot" )" && pwd )"
downloadPathFolder="$(cd -P "$( dirname "$engFolder" )" && pwd )/artifacts/tools"

mkdir -p "$downloadPathFolder"

. "$scriptroot/../common/tools.sh"

InitializeDotNetCli true

targetPlatform=$(dotnet --info |grep RID:)
targetPlatform=${targetPlatform##*RID:* }

clangFormatUrl=https://clrjit.blob.core.windows.net/clang-tools/${targetPlatform}/clang-format
clangFormatOutput=${downloadPathFolder}/clang-format

if [[ ! -x "$downloadPathFolder/clang-format" ]]; then
    curl --retry 5 -o "${clangFormatOutput}" "$clangFormatUrl"
    chmod 751 $clangFormatOutput
fi

export PATH=$downloadPathFolder:$PATH
