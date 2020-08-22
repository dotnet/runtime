#! /bin/bash

# This is a simple and currently crude script to generate an osx-arm64 Core_Root
# It works around issues with native files being built and/or included with the wrong arch

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

Core_Root=${RepoRoot}/artifacts/tests/coreclr/OSX.arm64.Debug/Tests/Core_Root
Arm64_Native_Paths="${RepoRoot}/artifacts/bin/coreclr/OSX.arm64.Debug ${RepoRoot}/artifacts/bin/native/OSX-arm64-Debug"
Arm64_SPC_Path=${RepoRoot}/artifacts/bin/coreclr/OSX.arm64.Debug/IL

function x64NativeFiles()
{
    for i in $(find ${Core_Root} -type f)
    do
       (file $i | grep -q x86_64) && echo $i
    done
}

# Remove any arm64 native files which were built with x86_64 architecture
for n in $(find ${Arm64_Native_Paths} -type f)
do
  (file $n | grep -q x86_64) && rm $n
done

# Rebuild native arm64 files
${RepoRoot}/src/libraries/Native/build-native.sh -arm64
${RepoRoot}/src/coreclr/build-runtime.sh -arm64

${RepoRoot}/src/coreclr/build-test.sh arm64 generatelayoutonly /p:LibrariesConfiguration=Debug

# Copy arm64 IL S.P.C.dll
cp ${Arm64_SPC_Path}/System.Private.CoreLib.dll ${Core_Root}

# Replace osx-x64 native files with their arm64 counterparts
for i in $(x64NativeFiles)
do 
  echo
  echo $i
  for n in $(find ${Arm64_Native_Paths} -name $(basename $i))
  do
    (file $n | grep -q x86_64) || (md5 $n; cp $n $i)
  done
done 2>&1

echo

# Remove any residual osx-x64 native files
for i in $(x64NativeFiles)
do 
  echo Removing native x86_64 file $i
  rm $i
done 2>&1
