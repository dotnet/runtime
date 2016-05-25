#!/usr/bin/env bash

usage()
{
    echo "Builds the NuGet packages from the binaries that were built in the Build product binaries step."
    echo "Usage: build-packages [arch] [configuration]"
    echo "arch can be x64, x86, arm, arm64 (default is x64)"
    echo "configuration can be release, checked, debug (default is debug)"
    echo
    exit 1
}

__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
build_packages_log=$__ProjectRoot/build-packages.log
binclashlog=$__ProjectRoot/binclash.log
binclashloggerdll=$__ProjectRoot/Tools/Microsoft.DotNet.Build.Tasks.dll
RuntimeOS=ubuntu.$VERSION_ID

__MSBuildPath=$__ProjectRoot/Tools/MSBuild.exe

# Parse arguments
__BuildArch=x64
__BuildType=Debug

allargs="$@"

echo -e "Running build-packages.sh $allargs" > $build_packages_log

if [ "$allargs" == "-h" ] || [ "$allargs" == "--help" ]; then
    usage
fi

while :; do
    if [ $# -le 0 ]; then
        break
    fi

    lowerI="$(echo $1 | awk '{print tolower($0)}')"
    case $lowerI in
        -\?|-h|--help)
            usage
            exit 1
            ;;

        x86)
            __BuildArch=x86
            ;;

        x64)
            __BuildArch=x64
            ;;

        arm)
            __BuildArch=arm
            ;;

        arm64)
            __BuildArch=arm64
            ;;
        debug)
            __BuildType=Debug
            ;;
        release)
            __BuildType=Release
            ;;
        checked)
            __BuildType=Checked
    esac
    shift
done

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        ;;
esac

if [ "$__BuildOS" == "Linux" ]; then
    if [ ! -e /etc/os-release ]; then
        echo "WARNING: Can not determine runtime id for current distro."
        export __DistroRid=""
    else
        source /etc/os-release
        export __DistroRid="$ID.$VERSION_ID-$__BuildArch"
    fi
fi

__IntermediatesDir="$__ProjectRoot/bin/obj/$__BuildOS.$__BuildArch.$__BuildType"

# Ensure that MSBuild is available
echo "Running init-tools.sh"
$__ProjectRoot/init-tools.sh

    echo "Generating nuget packages for "$__BuildOS

    # Invoke MSBuild
    $__ProjectRoot/Tools/corerun "$__MSBuildPath" /nologo "$__ProjectRoot/src/.nuget/Microsoft.NETCore.Runtime.CoreCLR/Microsoft.NETCore.Runtime.CoreCLR.builds" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$binclashlog" /t:Build /p:__BuildOS=$__BuildOS /p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__IntermediatesDir=$__IntermediatesDir /p:BuildNugetPackage=false /p:UseSharedCompilation=false

if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-packages.sh ." >> $build_packages_log
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

    # Build the JIT packages
    $__ProjectRoot/Tools/corerun "$__MSBuildPath" /nologo "$__ProjectRoot/src/.nuget/Microsoft.NETCore.Jit/Microsoft.NETCore.Jit.builds" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$binclashlog" /t:Build /p:__BuildOS=$__BuildOS /p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__IntermediatesDir=$__IntermediatesDir /p:BuildNugetPackage=false /p:UseSharedCompilation=false

if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-packages.sh ." >> $build_packages_log
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

    # Build the ILAsm package
    $__ProjectRoot/Tools/corerun "$__MSBuildPath" /nologo "$__ProjectRoot/src/.nuget/Microsoft.NETCore.ILAsm/Microsoft.NETCore.ILAsm.builds" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$binclashlog" /t:Build /p:__BuildOS=$__BuildOS /p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__IntermediatesDir=$__IntermediatesDir /p:BuildNugetPackage=false /p:UseSharedCompilation=false

if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-packages.sh ." >> $build_packages_log
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

    # Build the ILDAsm package
    $__ProjectRoot/Tools/corerun "$__MSBuildPath" /nologo "$__ProjectRoot/src/.nuget/Microsoft.NETCore.ILDAsm/Microsoft.NETCore.ILDAsm.builds" /verbosity:minimal "/fileloggerparameters:Verbosity=normal;LogFile=$binclashlog" /t:Build /p:__BuildOS=$__BuildOS /p:__BuildArch=$__BuildArch /p:__BuildType=$__BuildType /p:__IntermediatesDir=$__IntermediatesDir /p:BuildNugetPackage=false /p:UseSharedCompilation=false

if [ $? -ne 0 ]; then
    echo -e "\nAn error occurred. Aborting build-packages.sh ." >> $build_packages_log
    echo "ERROR: An error occurred while building packages, see $build_packages_log for more details."
    exit 1
fi

echo "Done building packages."
echo -e "\nDone building packages." >> $build_packages_log
exit 0
