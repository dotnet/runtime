#!/usr/bin/env bash
working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
#The Run Command Tool is calling src/Native/build-native.sh
$working_tree_root/run.sh build-native "$@"
exit $?
