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

function x64NativeFiles()
{
    for i in $(find ${Core_Root} -type f)
    do
       (file $i | grep -q x86_64) && echo $i
    done
}

${RepoRoot}/src/coreclr/build-test.sh arm64 generatelayoutonly /p:LibrariesConfiguration=Debug

# Warn for any residual osx-x64 native files
for i in $(x64NativeFiles)
do 
  echo Warning native x86_64 file $i
done 2>&1
