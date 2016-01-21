#!/usr/bin/env bash

if [ $# -eq 0 ]; then
    echo "Script for enabling CLang sanitizers for debug builds."
    echo "*Only tested on Ubuntu x64."
    echo "*This script must be 'sourced' (via dot+space) so that changes to environment variables are preserved. Run like this:"    
    if [ "$(dirname $0)" = "." ]; then
        echo " . enablesanitizers.sh [options]"
    else
        echo " cd $(dirname $0);. enablesanitizers.sh [options]; cd -"
    fi
    echo "Usage: [asan] [ubsan] [lsan] [all] [off] [clangx.y]"
    echo " asan: optional argument to enable Address Sanitizer."
    echo " ubsan: optional argument to enable Undefined Behavior Sanitizer."
    echo " lsan - optional argument to enable memory Leak Sanitizer."
    echo " all - optional argument to enable asan, ubsan and lsan."
    echo " off - optional argument to turn off all sanitizers."
    echo " clangx.y - optional argument to specify clang version x.y. which is used to resolve stack traces. Default is 3.6"
else
    # default to clang 3.6 instead of 3.5 because it supports print_stacktrace (otherwise only one stack frame)
    __ClangMajorVersion=3
    __ClangMinorVersion=6

    __EnableASan=0
    __EnableUBSan=0
    __EnableLSan=0
    __TurnOff=0
    __Options=

    for i in "$@"
        do
            lowerI="$(echo $i | awk '{print tolower($0)}')"
            case $lowerI in
            asan)
                __EnableASan=1
                ;;
            ubsan)
                __EnableUBSan=1
                ;;
            lsan)
                __EnableASan=1
                __EnableLSan=1
                ;;
            all)
                __EnableASan=1
                __EnableUBSan=1
                __EnableLSan=1
                ;;
            off)
                __TurnOff=1
                ;;
            clang3.5)
                __ClangMajorVersion=3
                __ClangMinorVersion=5
                ;;
            clang3.6)
                __ClangMajorVersion=3
                __ClangMinorVersion=6
                ;;
            clang3.7)
                __ClangMajorVersion=3
                __ClangMinorVersion=7
                ;;
            *)
                echo "Unknown arg: $i"
                return 1
        esac
    done

    if [ $__TurnOff == 1 ]; then
        unset DEBUG_SANITIZERS
        echo "Setting DEBUG_SANITIZERS="
    else
        # for now, specify alloc_dealloc_mismatch=0 as there are too many error reports that are not an issue
        ASAN_OPTIONS="symbolize=1 alloc_dealloc_mismatch=0"
        # when Clang 3.8 available, add: suppressions=$(readlink -f sanitizersuppressions.txt)
        UBSAN_OPTIONS="print_stacktrace=1"

        if [ $__EnableASan == 1 ]; then
            __Options="$__Options asan"
        fi
        if [ $__EnableUBSan == 1 ]; then
            __Options="$__Options ubsan"
        fi
        if [ $__EnableLSan == 1 ]; then
            ASAN_OPTIONS="$ASAN_OPTIONS detect_leaks"
        fi

        # passed to build.sh
        DEBUG_SANITIZERS="$__Options"
        export DEBUG_SANITIZERS
        echo "Setting DEBUG_SANITIZERS=$DEBUG_SANITIZERS"

        # used by ASan at run-time
        ASAN_OPTIONS="\"$ASAN_OPTIONS\""
        export ASAN_OPTIONS
        echo "Setting ASAN_OPTIONS=$ASAN_OPTIONS"

        UBSAN_OPTIONS="\"$UBSAN_OPTIONS\""
        export UBSAN_OPTIONS
        echo "Setting UBSAN_OPTIONS=$UBSAN_OPTIONS"

        # used by ASan at run-time
        ASAN_SYMBOLIZER_PATH="/usr/bin/llvm-symbolizer-$__ClangMajorVersion.$__ClangMinorVersion"
        export ASAN_SYMBOLIZER_PATH
        echo "Setting ASAN_SYMBOLIZER_PATH=$ASAN_SYMBOLIZER_PATH"
        echo "Done. You can now run: build.sh Debug clang$__ClangMajorVersion.$__ClangMinorVersion"
    fi

    unset __ClangMajorVersion
    unset __ClangMinorVersion
    unset __EnableASan
    unset __EnableUBSan
    unset __EnableLSan
    unset __TurnOff
    unset __Options
fi