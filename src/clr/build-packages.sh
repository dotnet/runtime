#!/usr/bin/env bash

usage()
{
    echo "Builds the NuGet packages from the binaries that were built in the Build product binaries step."
    echo "Usage: build-packages -BuildArch -BuildType"
    echo "arch can be x64, x86, arm, arm64 (default is x64)"
    echo "configuration can be release, checked, debug (default is debug)"
    echo
    exit 1
}

__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

unprocessedBuildArgs=

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    case "$1" in
        -\?|-h|--help)
        usage
        exit 1
        ;;
        -BuildArch=*)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
        __Arch=$(echo $1| cut -d'=' -f 2)
        ;;
        *)
        unprocessedBuildArgs="$unprocessedBuildArgs $1"
    esac
    shift
done

$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/Microsoft.NETCore.Runtime.CoreCLR/Microsoft.NETCore.Runtime.CoreCLR.builds -DistroRid=\${OSRid}-$__Arch -UseSharedCompilation=false -BuildNugetPackage=false $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while building packages; See build-packages.log for more details."
    exit 1
fi

$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/Microsoft.NETCore.Jit/Microsoft.NETCore.Jit.builds -DistroRid=\${OSRid}-$__Arch -UseSharedCompilation=false -BuildNugetPackage=false $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while building packages; See build-packages.log for more details."
    exit 1
fi

$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/Microsoft.NETCore.ILAsm/Microsoft.NETCore.ILAsm.builds -DistroRid=\${OSRid}-$__Arch -UseSharedCompilation=false -BuildNugetPackage=false $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while building packages; See build-packages.log for more details."
    exit 1
fi

$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/Microsoft.NETCore.ILDAsm/Microsoft.NETCore.ILDAsm.builds -DistroRid=\${OSRid}-$__Arch -UseSharedCompilation=false -BuildNugetPackage=false $unprocessedBuildArgs
if [ $? -ne 0 ]
then
    echo "ERROR: An error occurred while building packages; See build-packages.log for more details."
    exit 1
fi

# Build the TestHost package
$__ProjectRoot/run.sh build-packages -Project=$__ProjectRoot/src/.nuget/Microsoft.NETCore.TestHost/Microsoft.NETCore.TestHost.builds -DistroRid=\${OSRid}-$__Arch -UseSharedCompilation=false -BuildNugetPackage=false $unprocessedBuildArgs
if [ $? -ne 0 ]; then
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

echo "Done building packages."
exit 0
