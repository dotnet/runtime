#!/usr/bin/env bash

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

arguments=''
extraargs=''
checkedPossibleDirectoryToBuild=false
defaultargs="--build --restore --warnaserror false"

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
     -buildarch)
      arguments="$arguments /p:ArchGroup=$2"
      shift 2
      ;;
     -release)
      arguments="$arguments /p:ConfigurationGroup=Release --configuration Release"
      shift 1
      ;;
     -debug)
      arguments="$arguments /p:ConfigurationGroup=Debug --configuration Debug"
      shift 1
      ;;
     -framework)
      arguments="$arguments /p:TargetGroup=$2"
      shift 2
      ;;
     -os)
      arguments="$arguments /p:OSGroup=$2"
      shift 2
      ;;
     -allconfigurations)
      arguments="$arguments /p:BuildAllConfigurations=true"
      shift 1
      ;;
     -test)
      defaultargs="--test --warnaserror false"
      shift 1
      ;;
     -includetests|-buildtests)
      arguments="$arguments /p:BuildTests=true"
      shift 1
      ;;
     -outerloop)
      arguments="$arguments /p:OuterLoop=true"
      shift 1
      ;;
     -skiptests)
      arguments="$arguments /p:SkipTests=true"
      shift 1
      ;;
     -coverage)
      arguments="$arguments /p:Coverage=true"
      shift 1
      ;;
     -stripsymbols)
      arguments="$arguments /p:BuildNativeStripSymbols=true"
      shift 1
      ;;
     -runtimeos)
      arguments="$arguments /p:RuntimeOS=$2"
      shift 2
      ;;
     -sync|-restore)
      defaultargs="--restore"
      shift 1
      ;;
      *)
      ea=$1

      if [[ $checkedPossibleDirectoryToBuild == false ]]; then
        checkedPossibleDirectoryToBuild=true

        if [[ -d "$1" ]]; then
          ea="/p:DirectoryToBuild=$1"
        elif [[ -d "$scriptroot/../src/$1" ]]; then
          ea="/p:DirectoryToBuild=$scriptroot/../src/$1"
        fi
      fi

      extraargs="$extraargs $ea"
      shift 1
      ;;
  esac
done

arguments="$defaultargs $arguments $extraargs"

"$scriptroot/common/build.sh" $arguments
exit $?