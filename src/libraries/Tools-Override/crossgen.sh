#!/usr/bin/env bash

# Restores crossgen and runs it on all tools components.

__CoreClrVersion=1.1.0

usage()
{
    echo "crossgen.sh <directory>"
    echo "    Restores crossgen and runs it on all assemblies in <directory>."
    exit 0
}

restore_crossgen()
{
    __pjDir=$__toolsDir/crossgen
    mkdir -p $__pjDir
    echo "{\"frameworks\":{\"netcoreapp1.1\":{\"dependencies\":{\"Microsoft.NETCore.Runtime.CoreCLR\":\"$__CoreClrVersion\"}}},\"runtimes\":{\"$__rid\":{}}}" > "$__pjDir/project.json"
    $__dotnet restore $__pjDir/project.json --packages $__packagesDir
    cp $__packagesDir/runtime.$__rid.Microsoft.NETCore.Runtime.CoreCLR/$__CoreClrVersion/tools/crossgen $__sharedFxDir
    __crossgen=$__sharedFxDir/crossgen
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
    $__crossgen /Platform_Assemblies_Paths $__toolsDir:$__sharedFxDir /nologo /MissingDependenciesOK $__file > /dev/null
    if [ $? -eq 0 ]; then
      __outname="${__file/.dll/.ni.dll}"
      __outname="${__outname/.exe/.ni.exe}"
      echo "$__file -> $__outname"
    else
      echo "Unable to successfully compile $__file"
    fi
}

if [ -z "$1" ]; then
    usage
fi

__targetDir=$1
__scriptpath=$(cd "$(dirname "$0")"; pwd -P)
__toolsDir=$__scriptpath/../Tools
__dotnet=$__toolsDir/dotnetcli/dotnet
__packagesDir=$__scriptpath/../packages
__sharedFxDir=$__toolsDir/dotnetcli/shared/Microsoft.NETCore.App/$__CoreClrVersion/
__rid=$($__dotnet --info | sed -n -e 's/^.*RID:[[:space:]]*//p')

restore_crossgen
crossgen_everything
exit 0
