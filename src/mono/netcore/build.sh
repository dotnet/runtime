#!/usr/bin/env bash

usage()
{
  echo "Common settings:"
  echo "  --configuration <value>    Build configuration: 'Debug' or 'Release' (short: -c)"
  echo "  --help                     Print help and exit (short: -h)"
  echo ""

  echo "Actions:"
  echo "  --pack                     Package build outputs into NuGet packages"
  echo "  --test                     Run all unit tests in the solution (short: -t)"
  echo ""

  echo "Command line arguments starting with '/p:' are passed through to MSBuild."
  echo "Arguments can also be passed in with a single hyphen."
}

pack=false
configuration='Debug'
properties=''

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


cd ..
./autogen.sh --with-core=only
cd netcore

CPU_COUNT=$(getconf _NPROCESSORS_ONLN || echo 4)

# build mono runtime
make runtime -j$(CPU_COUNT)

# build System.Private.CoreLib (../mcs/class/System.Private.CoreLib)
make bcl CORLIB_BUILD_FLAGS="$properties"

# create a nupkg with runtime and System.Private.CoreLib
if [ "$pack" = "true" ]; then
    make nupkg
fi

# run all xunit tests
if [ "$test" = "true" ]; then
    make xtestall
fi