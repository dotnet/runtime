#!/usr/bin/env bash

usage_list+=("-coverage: optional argument to enable code coverage build (currently supported only for Linux and OSX).")

handle_arguments() {

    case "$1" in
        coverage|-coverage)
            __CodeCoverage=1
            ;;

        *)
            handle_arguments_local "$1" "$2"
            ;;
    esac
}

source "$__RepoRootDir"/eng/native/build-commons.sh
