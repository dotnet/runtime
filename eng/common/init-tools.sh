#!/usr/bin/env bash

ci=${ci:-false}
configuration=${configuration:-'Debug'}
nodereuse=${nodereuse:-true}
prepare_machine=${prepare_machine:-false}
restore=${restore:-true}
verbosity=${verbosity:-'minimal'}
warnaserror=${warnaserror:-true}

repo_root="$scriptroot/../.."
eng_root="$scriptroot/.."
artifacts_dir="$repo_root/artifacts"
toolset_dir="$artifacts_dir/toolset"
log_dir="$artifacts_dir/log/$configuration"
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

toolset_version=`ReadGlobalVersion "Microsoft.DotNet.Arcade.Sdk"`

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

  local toolset_restore_log="$log_dir/ToolsetRestore.binlog"
  local proj="$toolset_dir/restore.proj"

  echo '<Project Sdk="Microsoft.DotNet.Arcade.Sdk"/>' > $proj

  MSBuild "$proj /t:__WriteToolsetLocation /clp:None /bl:$toolset_restore_log /p:__ToolsetLocationOutputFile=$toolset_location_file"
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

function InitializeTools {
  InitializeDotNetCli
  InitializeToolset
  InitializeCustomToolset
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

function MSBuild {
  local msbuildArgs="msbuild /m /nologo /clp:Summary /v:$verbosity"
  local extraArgs="$@"

  if [[ $warnaserror == true ]]; then
    msbuildArgs="$msbuildArgs /warnaserror"
  fi

  msbuildArgs="$msbuildArgs /nr:$nodereuse"

  #echo "$build_driver $msbuildArgs $extraArgs"
  "$build_driver" $msbuildArgs $extraArgs

  return $?
}

function InstallDarcCli {
  local darc_cli_package_name="microsoft.dotnet.darc"
  local uninstall_command=`$DOTNET_INSTALL_DIR/dotnet tool uninstall $darc_cli_package_name -g`
  local tool_list=$($DOTNET_INSTALL_DIR/dotnet tool list -g)
  if [[ $tool_list = *$darc_cli_package_name* ]]; then
    echo $($DOTNET_INSTALL_DIR/dotnet tool uninstall $darc_cli_package_name -g)
  fi

  echo "Installing Darc CLI version $toolset_version..."
  echo "You may need to restart your command shell if this is the first dotnet tool you have installed."
  echo $($DOTNET_INSTALL_DIR/dotnet tool install $darc_cli_package_name --version $toolset_version -v $verbosity -g)
}

# HOME may not be defined in some scenarios, but it is required by NuGet
if [[ -z $HOME ]]; then
  export HOME="$repo_root/artifacts/.home/"
  mkdir -p "$HOME"
fi

if [[ -z $NUGET_PACKAGES ]]; then
  if [[ $ci == true ]]; then
    export NUGET_PACKAGES="$repo_root/.packages"
  else
    export NUGET_PACKAGES="$HOME/.nuget/packages"
  fi
fi

mkdir -p "$toolset_dir"
mkdir -p "$log_dir"

if [[ $ci == true ]]; then
  mkdir -p "$temp_dir"
  export TEMP="$temp_dir"
  export TMP="$temp_dir"
fi

InitializeTools