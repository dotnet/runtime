#! /bin/bash

# This is a simple and currently crude script to gernetate an osx-arm64 Core_Root
# It takes a reference osx-x64 Core_Root and replaces the native bits and 
# System.Private.CoreLib.dll from thode built lovally.
# It then removes any remaining x64 native binaries

# ToDo
# Error checking!!!
# Options ... build config!!!

source="${BASH_SOURCE[0]}"

# resolve $SOURCE until the file is no longer a symlink
while [[ -h $source ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"

  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

RepoRoot="$( cd -P "$( dirname "$source" )/../.." && pwd )"

Reference_Core_Root=${RepoRoot}/artifacts/tests/coreclr/OSX.x64.Debug/Tests/Core_Root
Core_Root=${RepoRoot}/artifacts/tests/coreclr/OSX.arm64.Debug/Tests/Core_Root
Arm64_Native_Paths="${RepoRoot}/artifacts/bin/coreclr/OSX.arm64.Debug ${RepoRoot}/artifacts/bin/native/net5.0-OSX-Debug-arm64"
Arm64_SPC_Path=${RepoRoot}/artifacts/bin/coreclr/OSX.arm64.Release/IL

function x64NativeFiles()
{
    for i in $(find ${Core_Root} -type f)
    do
       (file $i | grep -q x86_64) && echo $i
    done
}

# Remove any existing Core Root
rm -rf ${Core_Root}

# Create empty directory path
mkdir -p ${Core_Root}

# Copy reference Core Root
cp -r ${Reference_Core_Root}/* ${Core_Root}

# Copy crossgened arm64 S.P.C.dll
cp ${Arm64_SPC_Path}/System.Private.CoreLib.dll ${Core_Root}

# Replace osx-x64 native files with their arm64 counterparts
for i in $(x64NativeFiles)
do 
  echo
  echo $i
  find ${Arm64_Native_Paths} -name $(basename $i) -exec md5 "{}" ';' -exec cp "{}" $i ';'
done 2>&1

echo

# Remove any residual osx-x64 native files
for i in $(x64NativeFiles)
do 
  echo Removing native x86_64 file $i
  rm $i
done 2>&1
