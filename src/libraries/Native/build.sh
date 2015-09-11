#!/usr/bin/env bash

__ScriptDir="$( dirname "${BASH_SOURCE[0]}" )"
__ScriptDir="$( cd $__ScriptDir && pwd )"
__ProjectRoot="$( cd $__ScriptDir && cd ../.. && pwd )"

"$__ProjectRoot/build.sh" native "$@"
