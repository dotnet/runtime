#!/bin/bash

function print_usage {
    echo ''
    echo 'Usage:'
    echo '[rt=<runtime_path>] [nc=<nuget_cache_path>] [cli=<cli_path>] where:'
    echo '  <runtime_path>: path to the runtime that you want to use for testing.'
    echo '  <nuget_cache_path>: path to the nuget cache.'
    echo '  <cli_path>: path to the cli tool.'
    echo ''
    echo ''
}

# Argument variables
__RuntimeRoot='$(TestRoot)\Runtimes\Coreclr1'
__NugetCacheDir='$(WorkingDir)\packages'
__CliPath=
__ConfigFileName='Debugger.Tests.Config.txt'
__TemplateFileName='ConfigTemplate.txt'

for i in "$@"
do
    case $i in
        -h|--help)
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
        rt=*)
            __RuntimeRoot=${i#*=}
            ;;
        nc=*)
            __NugetCacheDir=${i#*=}
            ;;
        cli=*)
            __CliPath=${i#*=}
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

if ! -e "$__TemplateFileName"
then
    echo '$__TemplateFileName does not exist'
    exit 1
fi

if -e "$__ConfigFileName"
then
    rm "$__ConfigFileName"
fi

cp "$__TemplateFileName" "$__ConfigFileName"

sed -i \
    's/##Insert_Runtime_Root##/$__RuntimeRoot/g;' \
    's/##Insert_Nuget_Cache_Root##/$__NugetCacheDir/g'\
    's/##Cli_Path##/$__CliPath/g'\
    's/corerun.exe/corerun/g'\
    "$__ConfigFileName"

exit 0