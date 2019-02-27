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

usage()
{
  echo "Default if no actions are passed in: --restore --build"
  echo ""
  echo "CoreFX specific settings:"
  echo "  --buildtests             Build test projects"
  echo "  --framework              The target group assemblies are built for (short: -f)"
  echo "  --os                     The operating system assemblies are built for"
  echo "  --allconfigurations      Build packages for all build configurations"
  echo "  --coverage               Collect code coverage when testing"
  echo "  --outerloop              Include tests which are marked as OuterLoop"
  echo "  --arch                   The architecture group"
  echo ""
}

arguments=''
extraargs=''
checkedPossibleDirectoryToBuild=false

# Check if an action is passed in
declare -a actions=("-r" "--restore" "-b" "--build" "--rebuild" "--deploy" "--deployDeps" "--test" "--integrationTest" "--performanceTest" "--sign" "--publish" "--buildtests")
actInt=($(comm -12 <(printf '%s\n' "${actions[@]}" | sort) <(printf '%s\n' "$@" | sort)))
if [ ${#actInt[@]} -eq 0 ]; then
    arguments="--restore --build"
fi

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
     --help|-h)
      usage
      "$scriptroot/common/build.sh" --help
      exit 0
      ;;
     --arch)
      arguments="$arguments /p:ArchGroup=$2"
      shift 2
      ;;
     --configuration|-c)
      arguments="$arguments /p:ConfigurationGroup=$2 --configuration $2"
      shift 2
      ;;
     --framework|-f)
      val="$(echo "$2" | awk '{print tolower($0)}')"
      arguments="$arguments /p:TargetGroup=$val"
      shift 2
      ;;
     --os)
      arguments="$arguments /p:OSGroup=$2"
      shift 2
      ;;
     --allconfigurations)
      arguments="$arguments /p:BuildAllConfigurations=true"
      shift 1
      ;;
     --buildtests)
      arguments="$arguments /p:BuildTests=true"
      shift 1
      ;;
     --outerloop)
      arguments="$arguments /p:OuterLoop=true"
      shift 1
      ;;
     --coverage)
      arguments="$arguments /p:Coverage=true"
      shift 1
      ;;
     --stripsymbols)
      arguments="$arguments /p:BuildNativeStripSymbols=true"
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

arguments="$arguments $extraargs"

"$scriptroot/common/build.sh" $arguments
exit $?