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

function exit_with_error {
    local errorCode=$1
    local errorMsg=$2

    if [ ! -z "$2" ]; then
        echo $2
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

# This script must be located in coreclr/tests.
scriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo "Running init-tools.sh"
"${scriptDir}"/../init-tools.sh

dotnet=$"${scriptDir}"/../.dotnet/dotnet
packageDir="${scriptDir}"/../.packages
csprojPath="${scriptDir}"/src/Common/stress_dependencies/stress_dependencies.csproj

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

# Use uname to determine what the OS is.
OSName=$(uname -s)
case $OSName in
    Linux)
        __BuildOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __BuildOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __BuildOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __BuildOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __BuildOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        __BuildOS=SunOS
        __HostOS=SunOS
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __BuildOS=Linux
        __HostOS=Linux
        ;;
esac

isPortable=0

source "${scriptDir}"/../init-distro-rid.sh
initDistroRidGlobal ${__BuildOS} x64 ${isPortable}

# Hack, replace the rid to ubuntu.14.04 which has a valid non-portable
# package.
#
# The CoreDisTools package is currently manually packaged and we only have
# 14.04 and 16.04 packages. Use the oldest package which will work on newer
# platforms.
if [[ ${__BuildOS} == "Linux" ]]; then
   __DistroRid=ubuntu.14.04
fi

# Query runtime Id
rid=${__DistroRid}

echo "Rid to be used: ${rid}"

if [ -z "$rid" ]; then
    exit_with_error 1 "Failed to query runtime Id"
fi    

# Download the package
echo Downloading CoreDisTools package
bash -c -x "$dotnet restore $csprojPath --source https://dotnet.myget.org/F/dotnet-core/ --packages $packageDir"
if [ $? -ne 0 ]
then
    exit_with_error 1 "Failed to restore the package"
fi

# Get library path
libPath=`find $packageDir | grep $rid | grep -m 1 libcoredistools`
echo "libPath to be used: ${libPath}"

if [ ! -e $libPath ] || [ -z "$libPath" ]; then
    exit_with_error 1 'Failed to locate the downloaded library'
fi

# Copy library to output directory
echo 'Copy library:' $libPath '-->' $libInstallDir/
cp -f $libPath $libInstallDir
if [ $? -ne 0 ]
then
    exit_with_error 1 "Failed to copy the library"
fi

# Return success
exit $EXIT_CODE_SUCCESS
