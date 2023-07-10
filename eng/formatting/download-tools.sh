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
    echo "dotnet RID: ${targetPlatform}"

    # override common RIDs with compatible version so we don't need to upload binaries for each RID
    case $targetPlatform in
        osx.*-x64)
            targetPlatform=osx.10.15-x64
            ;;
        ubuntu.*-x64)
            targetPlatform=ubuntu.18.04-x64
            ;;
    esac

    toolUrl=https://clrjit.blob.core.windows.net/clang-tools/${targetPlatform}/$1
    toolOutput=$2/$1

    echo "Downloading $1 from ${toolUrl} to ${toolOutput}"

    if [[ ! -x "$toolOutput" ]]; then
        curl --silent --retry 5 --fail -o "${toolOutput}" "$toolUrl"
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
