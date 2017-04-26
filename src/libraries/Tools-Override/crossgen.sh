#!/usr/bin/env bash

# Restores crossgen and runs it on all tools components.

__CoreClrVersion=2.0.0-preview1-25204-02
__SharedFxVersion=2.0.0-preview1-001913-00
__MyGetFeed=https://dotnet.myget.org/F/dotnet-core/api/v3/index.json

usage()
{
    echo "crossgen.sh <directory>"
    echo "    Restores crossgen and runs it on all assemblies in <directory>."
    exit 0
}

restore_crossgen()
{
    __crossgen=$__sharedFxDir/crossgen
    if [ -e $__crossgen ]; then
        return
    fi

    __pjDir=$__toolsDir/crossgen
    mkdir -p $__pjDir
    echo "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>netcoreapp2.0</TargetFramework><DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences><RuntimeIdentifiers>$__packageRid</RuntimeIdentifiers></PropertyGroup><ItemGroup><PackageReference Include=\"Microsoft.NETCore.Runtime.CoreCLR\" Version=\"$__CoreClrVersion\" /><PackageReference Include=\"Microsoft.NETCore.Platforms\" Version=\"1.1.0\" /></ItemGroup></Project>" > "$__pjDir/crossgen.csproj"
    $__dotnet restore $__pjDir/crossgen.csproj --packages $__packagesDir --source $__MyGetFeed
    __crossgenInPackage=$__packagesDir/runtime.$__packageRid.microsoft.netcore.runtime.coreclr/$__CoreClrVersion/tools/crossgen
    if [ ! -e $__crossgenInPackage ]; then
        echo "The crossgen executable could not be found at "$__crossgenInPackage". Aborting crossgen.sh."
        exit 1
    fi
    cp $__crossgenInPackage $__sharedFxDir
    __crossgen=$__sharedFxDir/crossgen
    # Executables restored with .NET Core 2.0 do not have executable permission flags. https://github.com/NuGet/Home/issues/4424
    chmod +x $__crossgen
}

crossgen_everything()
{
    echo "Running crossgen on all assemblies in $__targetDir."
    for file in $__targetDir/*.{dll,exe}
    do
        if [ $(basename $file) != "Microsoft.Build.Framework.dll" ]; then
            crossgen_single $file & pid=$!
            __pids+=" $pid"
        fi
    done

    trap "kill $__pids 2&> /dev/null" SIGINT
    wait $__pids
    echo "Crossgen finished."
}

crossgen_single()
{
    __file=$1
    if [[ $__file != *.ni.dll && $__file != *.ni.exe ]]; then
        $__crossgen /Platform_Assemblies_Paths $__sharedFxDir:$__toolsDir /nologo /MissingDependenciesOK /ReadyToRun $__file > /dev/null
        if [ $? -eq 0 ]; then
            __outname="${__file/.dll/.ni.dll}"
            __outname="${__outname/.exe/.ni.exe}"
            echo "$__file -> $__outname"
        else
            echo "Unable to successfully compile $__file"
        fi
    fi
}

if [ ! -z $BUILDTOOLS_SKIP_CROSSGEN ]; then
    echo "BUILDTOOLS_SKIP_CROSSGEN is set. Skipping crossgen step."
    exit 0
fi

if [[ -z "$1" || "$1" == "-?" || "$1" == "--help" || "$1" == "-h" ]]; then
    usage
fi

__targetDir=$1
__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__toolsDir=$__scriptpath/../Tools
__dotnet=$__toolsDir/dotnetcli/dotnet
__packagesDir=$__scriptpath/../packages
__sharedFxDir=$__toolsDir/dotnetcli/shared/Microsoft.NETCore.App/$__SharedFxVersion/

case $(uname -s) in
    Darwin)
        __packageRid=osx-x64
        ;;
    Linux)
        __packageRid=linux-x64
        ;;
    *)
        echo "Unsupported OS $(uname -s) detected. Skipping crossgen of the toolset."
        exit 0
        ;;
esac

restore_crossgen
crossgen_everything
exit 0
