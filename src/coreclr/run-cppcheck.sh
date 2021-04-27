#!/usr/bin/env bash

# This script automatically runs the basic cppcheck and sloccount tools to generate a static analysis report.

usage()
{
    echo "Usage: run-cppcheck.sh [options] [files]"
    echo "Option:                   Description"
    echo "  --no-sloccount          Don't run sloccount"
    echo "  --no-cppcheck           Don't run cppcheck"
    echo "  --cppcheck-out <file>   Output file for cppcheck step.  Default cppcheck.xml"
    echo "  --sloccount-out <file>  Output file for sloccount step.  Default sloccount.sc"
    echo "Files:"
    echo "  files                   Files to run cppcheck through.  Default src/**"
}

check_dependencies()
{
    # Check presence of cppcheck on the path
    if [ "$RunCppCheck" = "true" ]
    then
        hash cppcheck 2>/dev/null || { echo >&2 "Please install cppcheck before running this script"; exit 1; }
    fi

    # Check presence of sloccount on the path
    if [ "$RunSlocCount" = "true" ]
    then
        hash sloccount 2>/dev/null || { echo >&2 "Please install sloccount before running this script"; exit 1; }
    fi
}

ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
RunSlocCount=true
RunCppCheck=true
Files="$ProjectRoot/src/**"
FilesFromArgs=""
CppCheckOutput="cppcheck.xml"
SloccountOutput="sloccount.sc"

# Get the number of processors available to the scheduler
# Other techniques such as `nproc` only get the number of
# processors available to a single process.
platform="$(uname)"
if [ "$platform" = "FreeBSD" ]; then
  NumProc=$(($(sysctl -n hw.ncpu)+1))
elif [ "$platform" = "NetBSD" || "$platform" = "SunOS" ]; then
  NumProc=$(($(getconf NPROCESSORS_ONLN)+1))
else
  NumProc=$(($(getconf _NPROCESSORS_ONLN)+1))
fi

while [[ $# > 0 ]]
do
    opt="$1"
    shift
    case $opt in
        -?|-h|--help)
        usage
        exit 1
        ;;
        --no-sloccount)
        RunSlocCount=false
        ;;
        --no-cppcheck)
        RunCppCheck=false
        ;;
        --cppcheck-out)
        CppCheckOutput=$1
        shift
        ;;
        --sloccount-out)
        SloccountOutput=$1
        shift
        ;;
        --*)
        echo "Unrecognized option: $opt"
        usage
        exit 1
        ;;
        *)
        FilesFromArgs="$FilesFromArgs $opt"
    esac
done

if [ -n "$FilesFromArgs" ];
then
    Files=$FilesFromArgs
fi

if [ -z "$CppCheckOutput" ];
then
    echo "Expected: file for cppcheck output"
    usage
    exit 1
fi

if [ -z "$SloccountOutput" ];
then
    echo "Expected: file for sloccount output"
    usage
    exit 1
fi

check_dependencies

if [ "$RunCppCheck" = "true" ]
then
    echo "Running cppcheck for files: $Files"
    cppcheck --enable=all -j $NumProc --xml --xml-version=2 --force $Files 2> $CppCheckOutput
    CppCheckOutputs="$CppCheckOutput (cppcheck)"
fi

if [ "$RunSlocCount" = "true" ]
then
    echo "Running sloccount for files: $Files"
    sloccount --wide --details $Files > $SloccountOutput
    SlocCountOutputs="$SloccountOutput (sloccount)"
fi

echo Check finished.  Results can be found in: $CppCheckOutputs $SlocCountOutputs
exit 0
