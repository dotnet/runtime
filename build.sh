#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

function is_cygwin_or_mingw()
{
  case $(uname -s) in
    CYGWIN*)    return 0;;
    MINGW*)     return 0;;
    *)          return 1;;
  esac
}

# switch to different Xcode version
if [[ "$OSTYPE" == "darwin"* ]]; then
  if [[ -d "/Applications/Xcode_26.2.app" ]]; then
    echo "Switching to Xcode 26.2 for build"
    sudo xcode-select -s "/Applications/Xcode_26.2.app"
  fi
fi

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

if is_cygwin_or_mingw; then
  # if bash shell running on Windows (not WSL),
  # pass control to batch build script.
  "$scriptroot/build.cmd" "$@"
else
  "$scriptroot/eng/build.sh" "$@"
fi
