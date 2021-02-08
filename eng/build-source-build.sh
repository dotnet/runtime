#!/usr/bin/env bash
set -euo pipefail

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )/.."

capture="common"
commonArgs=
coreclrArgs=
librariesArgs=
installerArgs=
additionalArgs=

for arg do
  shift
  opt="$(echo "$arg" | awk '{print tolower($0)}')"
  case $opt in
    (--coreclr-args)
            capture="coreclr"
            arg=""
            ;;
    (--libraries-args)
            capture="libraries"
            arg=""
            ;;
    (--installer-args)
            capture="installer"
            arg=""
            ;;
    (--additional-args)
            capture="additional"
            arg=""
            ;;
       (*) ;;
  esac

  if [ "$arg" != "" ]; then
      case $capture in
        (common)
            commonArgs="$commonArgs $arg"
            ;;
        (coreclr)
            coreclrArgs="$coreclrArgs $arg"
            ;;
        (libraries)
            librariesArgs="$librariesArgs $arg"
            ;;
        (installer)
            installerArgs="$installerArgs $arg"
            ;;
        (additional)
            additionalArgs="$additionalArgs $arg"
            ;;
        (*) ;;
      esac
  fi
done

echo "commonArgs = [$commonArgs]"
echo "coreclrArgs = [$coreclrArgs]"
echo "librariesArgs = [$librariesArgs]"
echo "installerArgs = [$installerArgs]"
echo "additionalAgs = [$additionalArgs]"

set -x

$scriptroot/build.sh $commonArgs -subset clr.tools+clr.runtime+clr.corelib+clr.nativecorelib+clr.packages $coreclrArgs $additionalArgs
find $scriptroot/artifacts/ -type f -name sourcebuild.binlog -exec rename "sourcebuild.binlog" "coreclrBuild.binlog" * {} \;

ilasmPath=$(dirname $(find $scriptroot/artifacts/bin -name ilasm))
$scriptroot/build.sh $commonArgs -subset libs $librariesArgs /p:ILAsmToolPath=$ilasmPath $additionalArgs
find $scriptroot/artifacts/ -type f -name sourcebuild.binlog -exec rename "sourcebuild.binlog" "librariesBuild.binlog" * {} \;

$scriptroot/build.sh $commonArgs -subset Host.Native+Host.Tools+Packs.Product+Packs.Installers $installerArgs /p:ILAsmToolPath=$ilasmPath $additionalArgs
find $scriptroot/artifacts/ -type f -name sourcebuild.binlog -exec rename "sourcebuild.binlog" "installerBuild.binlog" * {} \;
