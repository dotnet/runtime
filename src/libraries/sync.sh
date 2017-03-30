#!/usr/bin/env bash

if [ $# == 0 ]; then
    __args=-p
fi

working_tree_root="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

$working_tree_root/run.sh sync $__args $*
exit $?
