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

# Runs a subset build and interprets exit code.
# $1: Arbitrary name for the subset. Names the log files.
# $@: Remaining args are passed along.
#
# In Arcade 6, in CI mode, the build will always exit 0. This is intentional: https://github.com/dotnet/arcade/pull/6635
# To work around this (because we always build in CI mode for consistency), scan logs for the
# CI-style error reporting line so source-build can return the correct exit code.
subBuild() {
  name=$1
  shift

  mkdir "$scriptroot/artifacts" || :
  logFile="$scriptroot/artifacts/$name.log"

  # Exit NZEC if build command has NZEC *or* if the log file contains an error log command.
  (
    set -x
    "$scriptroot/build.sh" $commonArgs "$@" $additionalArgs | tee "$logFile"
  ) && (
    # Grep exits 0 if there is a match. Negate, so we exit 0 if there is no match.
    ! grep -F '##vso[task.complete result=Failed' "$logFile"
  )

  # Copy each subset binlog to its own file, rather than overwriting.
  find $scriptroot/artifacts/ -type f -name sourcebuild.binlog -exec rename "sourcebuild.binlog" "${name}Build.binlog" * {} \;
}

subBuild coreclr -subset clr.tools+clr.runtime+clr.corelib+clr.nativecorelib+clr.packages $coreclrArgs

ilasmPath=$(dirname $(find $scriptroot/artifacts/bin -name ilasm))
subBuild libraries -subset libs $librariesArgs /p:ILAsmToolPath=$ilasmPath

subBuild installer -subset Host.Native+Host.Tools+Packs.Product+Packs.Installers $installerArgs /p:ILAsmToolPath=$ilasmPath
