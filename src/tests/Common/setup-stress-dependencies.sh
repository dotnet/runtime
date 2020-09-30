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
    echo './setup-stress-dependencies.sh --arch=<TargetArch> --outputDir=<coredistools_lib_install_path>'
    echo ''
    echo 'Required arguments:'
    echo '  --arch=<TargetArch>        : Target arch for the build'
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
        --arch=*)
            __BuildArch=${i#*=}
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

if [ -z "$__BuildArch" ]; then
    echo "--arch is required."
    print_usage
    exit_with_error 1
fi

if [ -z "$libInstallDir" ]; then
    echo "--outputDir is required."
    print_usage
    exit_with_error 1
fi

if [ "$__BuildArch" = "arm64" ] || [ "$__BuildArch" = "arm" ]; then
    echo "No runtime dependencies for arm32/arm64"
    exit $EXIT_CODE_SUCCESS
fi

# This script must be located in coreclr/tests.
scriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

dotnet=$"${scriptDir}"/../../../dotnet.sh
csprojPath="${scriptDir}"/stress_dependencies/stress_dependencies.csproj

if [ ! -e $dotnetCmd ]; then
    exit_with_error 1 'dotnet commandline does not exist:'$dotnetCmd
fi

# make output directory
if [ ! -e $libInstallDir ]; then
    mkdir -p $libInstallDir
fi

# Use uname to determine what the OS is.
OSName=$(uname -s)
case "$OSName" in
    Linux)
        __TargetOS=Linux
        __HostOS=Linux
        ;;

    Darwin)
        __TargetOS=OSX
        __HostOS=OSX
        ;;

    FreeBSD)
        __TargetOS=FreeBSD
        __HostOS=FreeBSD
        ;;

    OpenBSD)
        __TargetOS=OpenBSD
        __HostOS=OpenBSD
        ;;

    NetBSD)
        __TargetOS=NetBSD
        __HostOS=NetBSD
        ;;

    SunOS)
        if uname -o 2>&1 | grep -q illumos; then
            __TargetOS=illumos
            __HostOS=illumos
        else
            __TargetOS=Solaris
            __HostOS=Solaris
        fi
        ;;

    *)
        echo "Unsupported OS $OSName detected, configuring as if for Linux"
        __TargetOS=Linux
        __HostOS=Linux
        ;;
esac

isPortable=0

source "${scriptDir}"/../../../eng/native/init-distro-rid.sh
initDistroRidGlobal "$__TargetOS" x64 "$isPortable"

# Hack, replace the rid to ubuntu.14.04 which has a valid non-portable
# package.
#
# The CoreDisTools package is currently manually packaged and we only have
# 14.04 and 16.04 packages. Use the oldest package which will work on newer
# platforms.
if [ "$__TargetOS" = "Linux" ]; then
    if [ "$__BuildArch" = "x64" ]; then
        __DistroRid=ubuntu.14.04-x64
    elif [ "$__BuildArch" = "x86" ]; then
        __DistroRid=ubuntu.14.04-x86
    fi
fi

# Query runtime Id
rid="$__DistroRid"

echo "Rid to be used: ${rid}"

if [ -z "$rid" ]; then
    exit_with_error 1 "Failed to query runtime Id"
fi

# Download the package
echo Downloading CoreDisTools package
bash -c -x "$dotnet restore $csprojPath"
if [ $? -ne 0 ]
then
    exit_with_error 1 "Failed to restore the package"
fi

CoreDisToolsPackagePathOutputFile="${scriptDir}/../../../artifacts/obj/coreclr/${__TargetOS}.x64/optdatapath.txt"

bash -c -x "$dotnet msbuild $csprojPath /t:DumpCoreDisToolsPackagePath /p:CoreDisToolsPackagePathOutputFile=\"$CoreDisToolsPackagePathOutputFile\" /p:RuntimeIdentifier=\"$rid\""
if [ $? -ne 0 ]
then
    exit_with_error 1 "Failed to find the path to CoreDisTools."
fi

packageDir=$(<"${CoreDisToolsPackagePathOutputFile}")

# Get library path
libPath="$(find "$packageDir" -path "*$rid*libcoredistools*" -print | head -n 1)"
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
