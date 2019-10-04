#!/usr/bin/env bash

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

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
  git submodule update --init -- ../external/llvm
  autogen_params="$autogen_params --enable-llvm"
fi

# run .././autogen.sh only once or if "--rebuild" argument is provided
if [[ "$force_rebuild" == "true" || ! -f .configured ]]; then
  (cd .. && ./autogen.sh --with-core=only $autogen_params CFLAGS="$EXTRA_CFLAGS" CXXFLAGS="$EXTRA_CXXFLAGS")
  touch .configured
fi

# build mono runtime
if [ "$skipnative" = "false" ]; then
  make runtime -j$CPU_COUNT
fi

# build System.Private.CoreLib (../mcs/class/System.Private.CoreLib)
if [ "$skipmscorlib" = "false" ]; then
  make bcl CORLIB_BUILD_FLAGS="$properties"
fi

# create a nupkg with runtime and System.Private.CoreLib
if [ "$pack" = "true" ]; then
  make nupkg
fi

# run all xunit tests
if [ "$test" = "true" ]; then
  make update-tests-corefx
  for testdir in corefx/tests/extracted/*; do
    ../scripts/ci/./run-step.sh --label=$(basename $testdir) --timeout=15m make run-tests-corefx-$(basename $testdir)
  done
fi