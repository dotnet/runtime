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

function DownloadClangTool {
    targetPlatform=$(dotnet --info |grep RID:)
    targetPlatform=${targetPlatform##*RID:* }

    toolUrl=https://clrjit.blob.core.windows.net/clang-tools/${targetPlatform}/$1
    toolOutput=$2/$1

    if [[ ! -x "$toolOutput" ]]; then
        curl --retry 5 -o "${toolOutput}" "$toolUrl"
        chmod 751 $toolOutput
    fi

    if [[ ! -x "$toolOutput" ]]; then
        echo "Failed to download $1"
        exit 1
    fi
}


engFolder="$(cd -P "$( dirname "$scriptroot" )" && pwd )"
downloadPathFolder="$(cd -P "$( dirname "$engFolder" )" && pwd )/artifacts/tools"

mkdir -p "$downloadPathFolder"

. "$scriptroot/../common/tools.sh"

InitializeDotNetCli true

DownloadClangTool "clang-format" "$downloadPathFolder"
DownloadClangTool "clang-tidy" "$downloadPathFolder"

export PATH=$downloadPathFolder:$PATH
