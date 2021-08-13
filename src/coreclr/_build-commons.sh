#!/usr/bin/env bash

usage_list+=("-coverage: optional argument to enable code coverage build (currently supported only for Linux and OSX).")
usage_list+=("-skipmanaged: do not build managed components.")
usage_list+=("-skipnative: do not build native components.")

handle_arguments() {

    case "$1" in
        coverage|-coverage)
            __CodeCoverage=1
            ;;

        skipmanaged|-skipmanaged)
            __SkipManaged=1
            __BuildTestWrappers=0
            ;;

        skipnative|-skipnative)
            __SkipNative=1
            __SkipCoreCLR=1
            __CopyNativeProjectsAfterCombinedTestBuild=false
            ;;

        *)
            handle_arguments_local "$1" "$2"
            ;;
    esac
}

source "$__RepoRootDir"/eng/native/build-commons.sh
