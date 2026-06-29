#!/bin/sh
currentDirectory=$(realpath "$(dirname "$0")")
currentTest=$(basename "$0" .sh)
runtimeExeArg=${1:-}
runtimeExe=${2:-}
extraLibsArg=${3:-}
extraLibsDir=${4:-}
if [ $# -eq 0 ]; then
	echo "Usage: ${currentTest}.sh -r /path/to/dotnet [-l /path/to/preloadlibs]"
	exit 0
fi
if [ "x$runtimeExeArg" != "x" ]; then
	if [ "x$runtimeExeArg" != "x-r" ]; then
		echo "Must specify runtime with -r /path/to/dotnet"
		exit 1
	fi
	if [ ! -x "$runtimeExe" ]; then
		echo "$runtimeExe is not a valid dotnet executable"
		exit 1
	fi
else
	echo "Must specify runtime with -r /path/to/dotnet"
	exit 1
fi
if [ "x$extraLibsArg" != "x" ]; then
	if [ "x$extraLibsArg" != "x-l" ]; then
		echo "Must specify preload libs with -l /path/to/preloadlibs"
		exit 1
	fi
	if [ ! -d "$extraLibsDir" ]; then
		echo "$extraLibsDir is not a valid directory"
		exit 1
	fi
	export LD_LIBRARY_PATH="$extraLibsDir"
fi
_RuntimeDir="$(dirname "$runtimeExe")"
# Consume OpenSSL if we find it in the runtime folder
if [ -d "$_RuntimeDir/openssl" ]; then
	_thisArch=$(uname -m)
	case "$_thisArch" in
		x86_64)
			_thisArchAndroid=x86_64
		;;
		i*86)
			_thisArchAndroid=x86
		;;
		armv*)
			_thisArchAndroid=armeabi-v7a
		;;
		aarch*)
			_thisArchAndroid=arm64-v8a
		;;
		*)
		;;
	esac
	export LD_LIBRARY_PATH="$_RuntimeDir/openssl/prefab/modules/ssl/libs/android.$_thisArchAndroid:$_RuntimeDir/openssl/prefab/modules/crypto/libs/android.$_thisArchAndroid"
	# Since we're on Helix, we know we want to set the SSL cert dir
	export SSL_CERT_DIR=/system/etc/security/cacerts
fi
# Android sets an invalid value for HOME, which we bypass on "real" Android via
# some env var setup in the task. Since we aren't using the Android task system
# for Bionic, we need to override the value another way
export HOME="$currentDirectory"
cd "$currentDirectory" || exit 1
# Sometimes the depsfile doesn't exist, so only conditionally try to pass it
if [ -e "${currentTest}.deps.json" ]; then
	depsFileArg="--depsfile ${currentTest}.deps.json"
fi
$runtimeExe exec --runtimeconfig "${currentTest}".runtimeconfig.json ${depsFileArg} xunit.console.dll "${currentTest}".dll -xml testResults.xml -nologo -nocolor -notrait category=IgnoreForCI -notrait category=OuterLoop -notrait category=failing
