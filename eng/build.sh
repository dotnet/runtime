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

usage()
{
  echo "Common settings:"
  echo "  --subset                   Build a subset, print available subsets with -subset help (short: -s)"
  echo "  --os                       Build operating system: Windows_NT, Linux, FreeBSD, OSX, tvOS, iOS, Android or WebAssembly"
  echo "  --arch                     Build platform: x86, x64, arm, armel, arm64 or wasm"
  echo "  --configuration            Build configuration: Debug, Release or [CoreCLR]Checked (short: -c)"
  echo "  --runtimeConfiguration     Runtime build configuration: Debug, Release or [CoreCLR]Checked (short: -rc)"
  echo "  --librariesConfiguration   Libraries build configuration: Debug or Release (short: -lc)"
  echo "  --projects <value>         Project or solution file(s) to build"
  echo "  --verbosity                MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic] (short: -v)"
  echo "  --binaryLog                Output binary log (short: -bl)"
  echo "  --cross                    Optional argument to signify cross compilation"
  echo "  --help                     Print help and exit (short: -h)"
  echo ""

  echo "Actions (defaults to --restore --build):"
  echo "  --restore                  Restore dependencies (short: -r)"
  echo "  --build                    Build all source projects (short: -b)"
  echo "  --rebuild                  Rebuild all source projects"
  echo "  --test                     Build and run tests (short: -t)"
  echo "  --pack                     Package build outputs into NuGet packages"
  echo "  --sign                     Sign build outputs"
  echo "  --publish                  Publish artifacts (e.g. symbols)"
  echo "  --clean                    Clean the solution"
  echo ""

  echo "Libraries settings:"
  echo "  --framework                Build framework: netcoreapp5.0 or net472 (short: -f)"
  echo "  --coverage                 Collect code coverage when testing"
  echo "  --testscope                Test scope, allowed values: innerloop, outerloop, all"
  echo "  --testnobuild              Skip building tests when invoking -test"
  echo "  --allconfigurations        Build packages for all build configurations"
  echo ""

  echo "Native build settings:"
  echo "  --clang                    Optional argument to build using clang in PATH (default)"
  echo "  --clangx.y                 Optional argument to build using clang version x.y"
  echo "  --cmakeargs                User-settable additional arguments passed to CMake."
  echo "  --gcc                      Optional argument to build using gcc in PATH (default)"
  echo "  --gccx.y                   Optional argument to build using gcc version x.y"

  echo "Command line arguments starting with '/p:' are passed through to MSBuild."
  echo "Arguments can also be passed in with a single hyphen."
}

initDistroRid()
{
    source $scriptroot/native/init-distro-rid.sh

    local passedRootfsDir=""
    local targetOs="$1"
    local buildArch="$2"
    local isCrossBuild="$3"
    # For RID calculation purposes, say we are always a portable build
    # All of our packages that use the distro rid (CoreCLR packages) are portable.
    local isPortableBuild=1

    # Only pass ROOTFS_DIR if __DoCrossArchBuild is specified.
    if (( isCrossBuild == 1 )); then
        passedRootfsDir=${ROOTFS_DIR}
    fi
    initDistroRidGlobal ${targetOs} ${buildArch} ${isPortableBuild} ${passedRootfsDir}
}

arguments=''
cmakeargs=''
extraargs=''
crossBuild=0

source $scriptroot/native/init-os-and-arch.sh

# Check if an action is passed in
declare -a actions=("b" "build" "r" "restore" "rebuild" "testnobuild" "sign" "publish" "clean")
actInt=($(comm -12 <(printf '%s\n' "${actions[@]/#/-}" | sort) <(printf '%s\n' "${@/#--/-}" | sort)))
firstArgumentChecked=0

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"

  if [[ $firstArgumentChecked -eq 0 && $opt =~ ^[a-zA-Z.+]+$ ]]; then
    arguments="$arguments /p:Subset=$1"
    shift 1
    continue
  fi

  firstArgumentChecked=1

  case "$opt" in
     -help|-h)
      usage
      exit 0
      ;;
     -subset|-s)
      arguments="$arguments /p:Subset=$2"
      shift 2
      ;;
     -arch)
      arch=$2
      shift 2
      ;;
     -configuration|-c)
      val="$(tr '[:lower:]' '[:upper:]' <<< ${2:0:1})${2:1}"
      arguments="$arguments -configuration $val"
      shift 2
      ;;
     -framework|-f)
      val="$(echo "$2" | awk '{print tolower($0)}')"
      arguments="$arguments /p:BuildTargetFramework=$val"
      shift 2
      ;;
     -os)
      os=$2
      arguments="$arguments /p:TargetOS=$2"
      shift 2
      ;;
     -allconfigurations)
      arguments="$arguments /p:BuildAllConfigurations=true"
      shift 1
      ;;
     -testscope)
      arguments="$arguments /p:TestScope=$2"
      shift 2
      ;;
     -testnobuild)
      arguments="$arguments /p:TestNoBuild=$2"
      shift 2
      ;;
     -coverage)
      arguments="$arguments /p:Coverage=true"
      shift 1
      ;;
     -runtimeconfiguration|-rc)
      val="$(tr '[:lower:]' '[:upper:]' <<< ${2:0:1})${2:1}"
      arguments="$arguments /p:RuntimeConfiguration=$val"
      shift 2
      ;;
     -librariesconfiguration|-lc)
      arguments="$arguments /p:LibrariesConfiguration=$2"
      shift 2
      ;;
     -cross)
      crossBuild=1
      arguments="$arguments /p:CrossBuild=True"
      shift 1
      ;;
     -clang*)
      arguments="$arguments /p:Compiler=$opt"
      shift 1
      ;;
     -cmakeargs)
      cmakeargs="${cmakeargs} ${opt} $2"
      shift 2
      ;;
     -gcc*)
      arguments="$arguments /p:Compiler=$opt"
      shift 1
      ;;
      *)
      extraargs="$extraargs $1"
      shift 1
      ;;
  esac
done

if [ ${#actInt[@]} -eq 0 ]; then
    arguments="-restore -build $arguments"
fi

initDistroRid $os $arch $crossBuild

# URL-encode space (%20) to avoid quoting issues until the msbuild call in /eng/common/tools.sh.
# In *proj files (XML docs), URL-encoded string are rendered in their decoded form.
cmakeargs="${cmakeargs// /%20}"
arguments="$arguments /p:TargetArchitecture=$arch"
arguments="$arguments /p:CMakeArgs=\"$cmakeargs\" $extraargs"
"$scriptroot/common/build.sh" $arguments
