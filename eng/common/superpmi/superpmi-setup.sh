#!/usr/bin/env bash

# Setups up machine to send payload to Helix machine to do superpmi collection
#
# 1. Copies CORE_ROOT in Payload/PmiAssembliesDirectory/Core_Root directory.
# 2. Copies Test binaries in Payload/PmiAssembliesDirectory/Tests directory.
# 3. Copies Sources/src/coreclr/scripts in Payload/superpmi directory.
# 4. Clones dotnet/jitutils repo in Payload/jitutils directory.
# 5. Build jitutils

echo "Inside superpmi-setup.sh"

source_directory=$BUILD_SOURCESDIRECTORY
core_root_directory=
managed_test_artifact_directory=
architecture=x64
framework=net5.0
tag=Linux.x64.checked

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
    --sourcedirectory)
      source_directory=$2
      shift 2
      ;;
    --corerootdirectory)
      core_root_directory=$2
      shift 2
      ;;
    --managedtestartifactdirectory)
      managed_test_artifact_directory=$2
      shift 2
      ;;
    --architecture)
      architecture=$2
      shift 2
      ;;
    --framework)
      framework=$2
      shift 2
      ;;
    --tag)
      tag=$2
      shift 2
      ;;
    *)
      echo "Common settings:"
      echo "  --corerootdirectory <value>               Directory where Core_Root exists"
      echo "  --managedtestartifactdirectory <value>    Directory where Test artifacts exists"
      echo "  --architecture <value>                    Architecture of the testing being run"
      echo "  --help                                    Print help and exit"
      echo ""
      echo "Advanced settings:"
      echo "  --tag <value>                  The tag to be used for .mch files"
      echo "  --framework <value>            The framework to run, if not running in master"
      echo "  --sourcedirectory <value>      The directory of the sources. Defaults to env:BUILD_SOURCESDIRECTORY"
      echo ""
      exit 0
      ;;
  esac
done

# WorkItem Directories
workitem_directory=$source_directory/workitem
pmi_assemblies_directory=$workitem_directory/pmiAssembliesDirectory

# CorrelationPayload Directories
correlation_payload_directory=$source_directory/Payload
superpmi_directory=$correlation_payload_directory/superpmi
jitutils_directory=$correlation_payload_directory/jitutils

queue="Ubuntu.1804.Amd64"

if [[ "$architecture" = "arm" ]]; then
  queue="(Ubuntu.1804.Arm32)Ubuntu.1804.Armarch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-helix-arm32v7-bfcd90a-20200121150440"
elif [[ "$architecture" = "arm64" ]]; then
  queue="(Ubuntu.1804.Arm64)Ubuntu.1804.ArmArch@mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-18.04-helix-arm64v8-a45aeeb-20190620155855"
fi

helix_source_prefix="official"
creator=

echo "Done setting queue"

# Prepare WorkItemDirectories (Specific to the job)
mkdir -p $pmi_assemblies_directory/Core_Root/binaries
echo rsync -avr --exclude='*.pdb' $core_root_directory/* $pmi_assemblies_directory/Core_Root/binaries
rsync -avr --exclude='*.pdb' $core_root_directory/* $pmi_assemblies_directory/Core_Root/binaries

# mkdir -p $pmi_assemblies_directory/Tests
# rsync -avr --exclude='*.pdb' $managed_test_artifact_directory/* $pmi_assemblies_directory/Tests

# Prepare CorrelationPayloadDirectories (Common to all the jobs)
mkdir -p $superpmi_directory
echo rsync -avr $source_directory/src/coreclr/scripts/* $superpmi_directory
rsync -avr $source_directory/src/coreclr/scripts/* $superpmi_directory

echo rsync -avr  $core_root_directory/* $superpmi_directory
rsync -avr  $core_root_directory/* $superpmi_directory

echo "Cloning and building JitUtilsDirectory"
git clone --branch master --depth 1 --quiet https://github.com/dotnet/jitutils $jitutils_directory
cd $jitutils_directory

export PATH="$source_directory/.dotnet:$PATH"
echo "dotnet PATH: $PATH"
./bootstrap.sh

cp $jitutils_directory/bin/pmi.dll $superpmi_directory
cd $source_directory
rm -r $jitutils_directory

echo "Printing files in $workitem_directory"
ls -R -1 $workitem_directory

ci=true

_script_dir=$(pwd)/eng/common
. "$_script_dir/pipeline-logging-functions.sh"

# Directories
Write-PipelineSetVariable -name "CorrelationPayloadDirectory" -value "$correlation_payload_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "SuperPMIDirectory" -value "$superpmi_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "PmiAssembliesDirectory" -value "$pmi_assemblies_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "WorkItemDirectory" -value "$workitem_directory" -is_multi_job_variable false
# Script arguments
Write-PipelineSetVariable -name "Python" -value "python3" -is_multi_job_variable false
Write-PipelineSetVariable -name "Architecture" -value "$architecture" -is_multi_job_variable false

# Helix Arguments
Write-PipelineSetVariable -name "Creator" -value "$creator" -is_multi_job_variable false
Write-PipelineSetVariable -name "Queue" -value "$queue" -is_multi_job_variable false
Write-PipelineSetVariable -name "HelixSourcePrefix" -value "$helix_source_prefix" -is_multi_job_variable false
Write-PipelineSetVariable -name "MchFileTag" -value "$tag" -is_multi_job_variable false