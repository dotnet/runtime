#!/usr/bin/env bash
working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
$working_tree_root/run.sh build-managed -packages "$@"
exit $?
