#!/usr/bin/env bash

# restore.sh will bootstrap the cli and ultimately call "dotnet
# restore". Dependencies of the linker will get restored as well.

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
$working_tree_root/../eng/dotnet.sh restore $working_tree_root/../illink.sln $@
