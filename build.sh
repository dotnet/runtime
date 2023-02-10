#!/usr/bin/env bash

echo ---------------------GOGO0---------------------

source="${BASH_SOURCE[0]}"

function is_cygwin_or_mingw()
{
  case $(uname -s) in
    CYGWIN*)    return 0;;
    MINGW*)     return 0;;
    *)          return 1;;
  esac
}

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
  # pass control to powershell build script.
  scriptroot=$(cygpath -d "$scriptroot")
  powershell -c "$scriptroot\\build.cmd" $@
else
  export COREHOST_TRACE=1
  export COREHOST_TRACE_VERBOSITY=3
  "$scriptroot/eng/build.sh" $@ || true

  echo ---------------------GOGO1---------------------

  export COREHOST_TRACEFILE=~/fusion.txt

  echo ---------------------GOGO2---------------------
  "$scriptroot/eng/build.sh" -b $@ || true
  echo ---------------------GOGO3---------------------
  cat ~/fusion.txt
  echo ---------------------GOGO4---------------------
fi
