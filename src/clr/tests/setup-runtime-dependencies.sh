#!/usr/bin/env bash
# set -x

#
# Constants
#
readonly EXIT_CODE_SUCCESS=0

#
# This script should be located in coreclr/tests.
#

function print_usage {
    echo ''
    echo 'Download coredistools for GC stress testing'
    echo ''
    echo 'Command line:'
    echo ''
    echo './setup-gcstress.sh --outputDir=<coredistools_lib_install_path>'
    echo ''
    echo 'Required arguments:'
    echo '  --outputDir=<path>         : Directory to install libcoredistools.so'
    echo ''
}

# temorary directory 
tmpDirPath=

function exit_with_error {
    local errorCode=$1
    local errorMsg=$2

    if [ ! -z "$2" ]; then
        echo $2
    fi
    
    if [ -e $tmpDirPath ]; then
        rm -rf $tmpDirPath
    fi
    
    exit $errorCode
}

function handle_ctrl_c {
    exit_with_error 1 'Aborted by Ctrl+C'
 }

# Register the Ctrl-C handler
trap handle_ctrl_c INT

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
    exit_with_error 1
fi

# create temp directory
tmpDirPath=`mktemp -d`
if [ ! -e $tmpDirPath ]; then
    exit_with_error 1 "Cannot create a temporary directory"
fi

# This script must be located in coreclr/tests.
scriptDir=$(cd "$(dirname "$0")"; pwd -P)
dotnetToolsDir=$scriptDir/../Tools
dotnetCmd=${dotnetToolsDir}/dotnetcli/dotnet
packageDir=${scriptDir}/../packages
jsonFilePath=${tmpDirPath}/project.json

# Check tool directory
if [ ! -e $dotnetToolsDir ]; then
    exit_with_error 1 'Directory containing dotnet commandline does not exist:'$dotnetToolsDir 
fi
if [ ! -e $dotnetCmd ]; then
    exit_with_error 1 'dotnet commandline does not exist:'$dotnetCmd
fi

# make package directory
if [ ! -e $packageDir ]; then
    mkdir -p $packageDir
fi

# make output directory
if [ ! -e $libInstallDir ]; then
    mkdir -p $libInstallDir
fi

# Query runtime Id
rid=`$dotnetCmd --info | grep 'RID:' | sed 's/^ *RID: *//g'`  
if [ -z "$rid" ]; then
    exit_with_error 1 "Failed to query runtime Id"
fi    

# Write dependency information to project.json
packageName='runtime.'$rid'.Microsoft.NETCore.CoreDisTools'
echo {  \
    \"dependencies\": { \
    \"$packageName\": \"1.0.1-prerelease-*\" \
    }, \
    \"frameworks\": { \"dnxcore50\": { } } \
    } > $jsonFilePath

# Download the package
echo Downloading CoreDisTools package
bash -c -x "$dotnetCmd restore $jsonFilePath --source https://dotnet.myget.org/F/dotnet-core/ --packages $packageDir"
if [ $? -ne 0 ]
then
    exit_with_error 1 "Failed to restore the package"
fi

# Get library path
libPath=`find $packageDir | grep $rid | grep -m 1 libcoredistools`
if [ ! -e $libPath ]; then
    exit_with_error 1 'Failed to locate the downloaded library'
fi

# Copy library to output directory
echo 'Copy library:' $libPath '-->' $libInstallDir/
cp -f $libPath $libInstallDir
if [ $? -ne 0 ]
then
    exit_with_error 1 "Failed to copy the library"
fi

# Delete temporary files
rm -rf $tmpDirPath

# Return success
exit $EXIT_CODE_SUCCESS

