#!/usr/bin/env bash

#
# This script should be located in coreclr/tests.
#

function print_usage {
    echo ''
    echo 'Download coredistool for GC stress testing'
    echo ''
    echo 'Command line:'
    echo ''
    echo './setup-gcstress.sh --outputDir=<coredistools_lib_install_path>'
    echo ''
    echo 'Required arguments:'
    echo '  --outputDir=<path>         : Directory to install libcoredistools.so'
    echo ''
}

# Argument variables
libInstallDir=

# Handle arguments
verbose=0
for i in "$@"
do
    case $i in
        -h|--help)
            exit $EXIT_CODE_SUCCESS
            ;;
        -v|--verbose)
            verbose=1
            ;;
        --outputDir=*)
            libInstallDir=${i#*=}
            ;;
        *)
            echo "Unknown switch: $i"
            print_usage
            exit $EXIT_CODE_SUCCESS
            ;;
    esac
done

if [ -z "$libInstallDir" ]; then
    echo "--libInstallDir is required."
    print_usage
    exit $EXIT_CODE_EXCEPTION
fi

# This script must be located in coreclr/tests.
scriptDir=$(cd "$(dirname "$0")"; pwd -P)
dotnetToolsDir=$scriptDir/../Tools
dotnetCmd=${dotnetToolsDir}/dotnetcli/bin/dotnet
packageDir=${scriptDir}/../packages
jsonFilePath=${scriptDir}/project.json

# Check tool directory
if [ ! -e $dotnetToolsDir ]; then
    echo 'Directory containing dotnet commandline does not exist:' $dotnetToolsDir 
    exit 1
fi
if [ ! -e $dotnetCmd ]; then
    echo 'donet commandline does not exist:' $dotnetCmd
    exit 1
fi

# make package directory
if [ ! -e $packageDir ]; then
    mkdir -p $packageDir
fi

# make output directory
if [ ! -e $libInstallDir ]; then
    mkdir -p $libInstallDir
fi

# Write dependency information to project.json
echo {  \
    \"dependencies\": { \
    \"Microsoft.NETCore.CoreDisTools\": \"1.0.0-prerelease-00001\" \
    }, \
    \"frameworks\": { \"dnxcore50\": { } } \
    } > $jsonFilePath

# Download the package
echo Downloading CoreDisTools package
bash -c -x "$dotnetCmd restore $jsonFilePath --source https://dotnet.myget.org/F/dotnet-core/ --packages $packageDir"

# Get library path
libPath=`find $packageDir | grep -m 1 libcoredistools`
if [ ! -e $libPath ]; then
    echo 'Failed to locate the downloaded library'
    exit 1
fi

# Copy library to output directory
echo 'Copy library:' $libPath '-->' $libInstallDir/
cp -f $libPath $libInstallDir

# Delete temporary files
rm -rf $jsonFilePath

# Return success
exit 0

