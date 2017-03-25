#!/usr/bin/env bash

# build.sh will bootstrap the cli and ultimately call "dotnet build".
# If no configuration is specified, the default configuration will be
# set to netcore_Debug (see config.json).

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
$working_tree_root/dotnet.sh build ../linker/Mono.Linker.csproj -c netcore_Debug $@
exit $?
