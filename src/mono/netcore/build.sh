#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

# Handle being in the "wrong" directory
cd "${BASH_SOURCE%/*}/"

# Include VSTS logging helpers
. ../eng/common/pipeline-logging-functions.sh

usage()
{
  echo "Common settings:"
  echo "  --configuration <value>    Build configuration: 'Debug' or 'Release' (short: -c)"
  echo "  --help                     Print help and exit (short: -h)"
  echo ""

  echo "Actions:"
  echo "  --pack                     Package build outputs into NuGet packages"
  echo "  --test                     Run all unit tests in the solution (short: -t)"
  echo "  --rebuild                  Run ../.autogen.sh"
  echo "  --llvm                     Enable LLVM support"
  echo "  --skipnative               Do not build runtime"
  echo "  --skipmscorlib             Do not build System.Private.CoreLib"
  echo "  --ci                       Enable Azure DevOps telemetry decoration"
  echo ""

  echo "Command line arguments starting with '/p:' are passed through to MSBuild."
  echo "Arguments can also be passed in with a single hyphen."
}

pack=false
configuration='Debug'
properties=''
force_rebuild=false
test=false
skipmscorlib=false
skipnative=false
llvm=false
autogen_params=''
ci=false

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"
  case "$opt" in
    -help|-h)
      usage
      exit 0
      ;;
    -configuration|-c)
      properties="$properties $1 $2"
      configuration=$2
      shift
      ;;
    -pack)
      pack=true
      ;;
    -test|-t)
      test=true
      ;;
    -rebuild)
      force_rebuild=true
      ;;
    -skipmscorlib)
      skipmscorlib=true
      ;;
    -skipnative)
      skipnative=true
      ;;
    -llvm)
      llvm=true
      ;;
    -ci)
      ci=true
      ;;
    -p:*|/p:*)
      properties="$properties $1"
      ;;
    -m:*|/m:*)
      properties="$properties $1"
      ;;
    -bl:*|/bl:*)
      properties="$properties $1"
      ;;
    -dl:*|/dl:*)
      properties="$properties $1"
      ;;
    *)
      echo "Invalid argument: $1"
      usage
      exit 1
      ;;
  esac

  shift
done

CPU_COUNT=$(getconf _NPROCESSORS_ONLN || echo 4)

if [[ "$configuration" == "Debug" ]]; then
  EXTRA_CFLAGS="-O0 -ggdb3 -fno-omit-frame-pointer"
  EXTRA_CXXFLAGS="-O0 -ggdb3 -fno-omit-frame-pointer"
elif [[ "$configuration" == "Release" ]]; then
  EXTRA_CFLAGS="-O2 -g"
  EXTRA_CXXFLAGS="-O2 -g"
fi

if [ "$llvm" = "true" ]; then
  git submodule update --init -- ../external/llvm-project || (Write-PipelineTelemetryError -c "git" -e 1 "Error fetching LLVM submodule" && exit 1)
  autogen_params="$autogen_params --enable-llvm"
fi

# run .././autogen.sh only once or if "--rebuild" argument is provided
if [[ "$force_rebuild" == "true" || ! -f .configured ]]; then
  (cd .. && ./autogen.sh --with-core=only $autogen_params CFLAGS="$EXTRA_CFLAGS" CXXFLAGS="$EXTRA_CXXFLAGS") || (Write-PipelineTelemetryError -c "configure" -e 1 "Error running autogen" && exit 1)
  touch .configured
fi

# build mono runtime
if [ "$skipnative" = "false" ]; then
  make runtime -j$CPU_COUNT || (Write-PipelineTelemetryError -c "runtime" -e 1 "Error building unmanaged runtime" && exit 1)
fi

# build System.Private.CoreLib (../mcs/class/System.Private.CoreLib)
if [ "$skipmscorlib" = "false" ]; then
  make bcl CORLIB_BUILD_FLAGS="$properties" || (Write-PipelineTelemetryError -c "bcl" -e 1 "Error building System.Private.CoreLib" && exit 1)
fi

# create a nupkg with runtime and System.Private.CoreLib
if [ "$pack" = "true" ]; then
  if [ "$llvm" = "true" ]; then
    make nupkg-llvm || (Write-PipelineTelemetryError -c "nupkg" -e 1 "Error packing NuGet package" && exit 1)
  else
    make nupkg || (Write-PipelineTelemetryError -c "nupkg" -e 1 "Error packing NuGet package" && exit 1)
  fi
fi

# run all xunit tests
if [ "$test" = "true" ]; then
  make update-tests-corefx || (Write-PipelineTelemetryError -c "tests-download" -e 1 "Error downloading tests" && exit 1)
  if [ "$ci" = "true" ]; then
    make run-tests-corefx USE_TIMEOUT=1 || (Write-PipelineTelemetryError -c "tests" -e 1 "Error running tests" && exit 1)
  else
    make run-tests-corefx
  fi
fi
