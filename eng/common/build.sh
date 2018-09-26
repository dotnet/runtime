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

help=false
restore=false
build=false
rebuild=false
test=false
pack=false
integration_test=false
performance_test=false
sign=false
public=false
ci=false

projects=''
configuration='Debug'
prepare_machine=false
verbosity='minimal'
properties=''

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
    --build)
      build=true
      shift 1
      ;;
    --ci)
      ci=true
      shift 1
      ;;
    --configuration)
      configuration=$2
      shift 2
      ;;
    --help)
      echo "Common settings:"
      echo "  --configuration <value>  Build configuration Debug, Release"
      echo "  --verbosity <value>      Msbuild verbosity (q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic])"
      echo "  --help                   Print help and exit"
      echo ""
      echo "Actions:"
      echo "  --restore                Restore dependencies"
      echo "  --build                  Build solution"
      echo "  --rebuild                Rebuild solution"
      echo "  --test                   Run all unit tests in the solution"
      echo "  --sign                   Sign build outputs"
      echo "  --pack                   Package build outputs into NuGet packages and Willow components"
      echo ""
      echo "Advanced settings:"
      echo "  --solution <value>       Path to solution to build"
      echo "  --ci                     Set when running on CI server"
      echo "  --prepareMachine         Prepare machine for CI run"
      echo ""
      echo "Command line arguments not listed above are passed through to MSBuild."
      exit 0
      ;;
    --pack)
      pack=true
      shift 1
      ;;
    --preparemachine)
      prepare_machine=true
      shift 1
      ;;
    --rebuild)
      rebuild=true
      shift 1
      ;;
    --restore)
      restore=true
      shift 1
      ;;
    --sign)
      sign=true
      shift 1
      ;;
    --solution)
      solution=$2
      shift 2
      ;;
    --test)
      test=true
      shift 1
      ;;
    --integrationtest)
      integration_test=true
      shift 1
      ;;
    --performancetest)
      performance_test=true
      shift 1
      ;;
    --publish)
      publish=true
      shift 1
      ;;
    --verbosity)
      verbosity=$2
      shift 2
      ;;
    *)
      properties="$properties $1"
      shift 1
      ;;
  esac
done

repo_root="$scriptroot/../.."
eng_root="$scriptroot/.."
artifacts_dir="$repo_root/artifacts"
toolset_dir="$artifacts_dir/toolset"
log_dir="$artifacts_dir/log/$configuration"
build_log="$log_dir/Build.binlog"
toolset_restore_log="$log_dir/ToolsetRestore.binlog"
temp_dir="$artifacts_dir/tmp/$configuration"

global_json_file="$repo_root/global.json"
build_driver=""
toolset_build_proj=""

# ReadVersionFromJson [json key]
function ReadGlobalVersion {
  local key=$1

  local unamestr="$(uname)"
  local sedextended='-r'
  if [[ "$unamestr" == 'Darwin' ]]; then
    sedextended='-E'
  fi;

  local version="$(grep -m 1 "\"$key\"" $global_json_file | sed $sedextended 's/^ *//;s/.*: *"//;s/",?//')"
  if [[ ! "$version" ]]; then
    echo "Error: Cannot find \"$key\" in $global_json_file" >&2;
    ExitWithExitCode 1
  fi;

  # return value
  echo "$version"
}

function InitializeDotNetCli {
  # Disable first run since we want to control all package sources
  export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

  # Don't resolve runtime, shared framework, or SDK from other locations to ensure build determinism
  export DOTNET_MULTILEVEL_LOOKUP=0

  # Source Build uses DotNetCoreSdkDir variable
  if [[ -n "$DotNetCoreSdkDir" ]]; then
    export DOTNET_INSTALL_DIR="$DotNetCoreSdkDir"
  fi

  
  local dotnet_sdk_version=`ReadGlobalVersion "dotnet"`
  local dotnet_root=""

  # Use dotnet installation specified in DOTNET_INSTALL_DIR if it contains the required SDK version, 
  # otherwise install the dotnet CLI and SDK to repo local .dotnet directory to avoid potential permission issues.
  if [[ -d "$DOTNET_INSTALL_DIR/sdk/$dotnet_sdk_version" ]]; then
    dotnet_root="$DOTNET_INSTALL_DIR"
  else
    dotnet_root="$repo_root/.dotnet"
    export DOTNET_INSTALL_DIR="$dotnet_root"

    if [[ "$restore" == true ]]; then
      InstallDotNetSdk $dotnet_root $dotnet_sdk_version
    fi
  fi

  build_driver="$dotnet_root/dotnet"
}

function InstallDotNetSdk {
  local root=$1
  local version=$2

  local install_script=`GetDotNetInstallScript $root`

  bash "$install_script" --version $version --install-dir $root
  local lastexitcode=$?

  if [[ $lastexitcode != 0 ]]; then
    echo "Failed to install dotnet SDK (exit code '$lastexitcode')."
    ExitWithExitCode $lastexitcode
  fi
}

function GetDotNetInstallScript {
  local root=$1
  local install_script="$root/dotnet-install.sh"

  if [[ ! -a "$install_script" ]]; then
    mkdir -p "$root"

    # Use curl if available, otherwise use wget
    if command -v curl > /dev/null; then
      curl "https://dot.net/v1/dotnet-install.sh" -sSL --retry 10 --create-dirs -o "$install_script"
    else
      wget -q -O "$install_script" "https://dot.net/v1/dotnet-install.sh"
    fi
  fi

  # return value
  echo "$install_script"
}

function InitializeToolset {
  local toolset_version=`ReadGlobalVersion "Microsoft.DotNet.Arcade.Sdk"`
  local toolset_location_file="$toolset_dir/$toolset_version.txt"

  if [[ -a "$toolset_location_file" ]]; then
    local path=`cat $toolset_location_file`
    if [[ -a "$path" ]]; then
      toolset_build_proj=$path
      return
    fi
  fi  

  if [[ "$restore" != true ]]; then
    echo "Toolset version $toolsetVersion has not been restored."
    ExitWithExitCode 2
  fi
  
  local proj="$toolset_dir/restore.proj"

  echo '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' > $proj
  "$build_driver" msbuild $proj /t:__WriteToolsetLocation /m /nologo /clp:None /warnaserror /bl:$toolset_restore_log /v:$verbosity /p:__ToolsetLocationOutputFile=$toolset_location_file 
  local lastexitcode=$?

  if [[ $lastexitcode != 0 ]]; then
    echo "Failed to restore toolset (exit code '$lastexitcode'). See log: $toolset_restore_log"
    ExitWithExitCode $lastexitcode
  fi

  toolset_build_proj=`cat $toolset_location_file`

  if [[ ! -a "$toolset_build_proj" ]]; then
    echo "Invalid toolset path: $toolset_build_proj"
    ExitWithExitCode 3
  fi
}

function InitializeCustomToolset {
  local script="$eng_root/RestoreToolset.sh"

  if [[ -a "$script" ]]; then
    . "$script"
  fi
}

function Build {
  "$build_driver" msbuild $toolset_build_proj \
    /m /nologo /clp:Summary /warnaserror \
    /v:$verbosity \
    /bl:$build_log \
    /p:Configuration=$configuration \
    /p:Projects=$projects \
    /p:RepoRoot="$repo_root" \
    /p:Restore=$restore \
    /p:Build=$build \
    /p:Rebuild=$rebuild \
    /p:Deploy=$deploy \
    /p:Test=$test \
    /p:Pack=$pack \
    /p:IntegrationTest=$integration_test \
    /p:PerformanceTest=$performance_test \
    /p:Sign=$sign \
    /p:Publish=$publish \
    /p:ContinuousIntegrationBuild=$ci \
    /p:CIBuild=$ci \
    $properties
  local lastexitcode=$?

  if [[ $lastexitcode != 0 ]]; then
    echo "Failed to build $toolset_build_proj"
    ExitWithExitCode $lastexitcode
  fi
}

function ExitWithExitCode {
  if [[ "$ci" == true && "$prepare_machine" == true ]]; then
    StopProcesses
  fi
  exit $1
}

function StopProcesses {
  echo "Killing running build processes..."
  pkill -9 "dotnet"
  pkill -9 "vbcscompiler"
}

function Main {
  # HOME may not be defined in some scenarios, but it is required by NuGet
  if [[ -z $HOME ]]; then
    export HOME="$repo_root/artifacts/.home/"
    mkdir -p "$HOME"
  fi

  if [[ -z $projects ]]; then
    projects="$repo_root/*.sln"
  fi

  if [[ -z $NUGET_PACKAGES ]]; then
    if [[ $ci ]]; then
      export NUGET_PACKAGES="$repo_root/.packages"
    else
      export NUGET_PACKAGES="$HOME/.nuget/packages"
    fi
  fi

  mkdir -p "$toolset_dir"
  mkdir -p "$log_dir"
  
  if [[ $ci ]]; then
    mkdir -p "$temp_dir"
    export TEMP="$temp_dir"
    export TMP="$temp_dir"
  fi

  InitializeDotNetCli
  InitializeToolset
  InitializeCustomToolset

  Build
  ExitWithExitCode $?
}

Main
