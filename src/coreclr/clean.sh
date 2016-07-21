#!/usr/bin/env bash

usage()
{
    echo "Usage: clean [-b] [-t] [-p]"
    echo "Repository cleaning script."
    echo "  -b         Clean bin directory"
    echo "  -t         Clean tools directory"
    echo "  -p         Clean packages directory"
    echo "  -all       Clean everything"
    echo
    echo "If no option is specified, then \"clean.sh -b -t -p\" is implied."
    exit 1
}

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo Cleaning previous output for the selected configuration

# Parse arguments
if [ $# == 0 ]; then
    clean_bin=true
    clean_tools=true
    clean_packages=true
fi

while [[ $# -gt 0 ]]
do
    opt="$1"
    case $opt in
        -h|--help)
        usage
        ;;
        -b)
        clean_bin=true
        ;;
        -t)
        clean_tools=true
        ;;
        -p)
        clean_packages=true
        ;;
        -all)
    	clean_bin=true
    	clean_tools=true
    	clean_packages=true
    	;;
        *)
    esac
    shift
done

if [ "$clean_bin" == true ]; then
	echo "Deleting bin directory"
	rm -rf "$__ProjectRoot/bin"
	if [ $? -ne 0 ]; then
        echo "Error while deleting bin directory - error code was $?"
        exit 1
    fi
fi

if [ "$clean_tools" == true ]; then
	echo "Deleting tools directory"
	rm -rf "$__ProjectRoot/Tools"
	if [ $? -ne 0 ]; then
        echo "Error while deleting tools directory - error code was $?"
        exit 1
    fi
fi

if [ "$clean_packages" == true ]; then
	echo "Deleting packages directory"
	rm -rf "$__ProjectRoot/packages"
	if [ $? -ne 0 ]; then
        echo "Error while deleting packages directory - error code was $?"
        exit 1
    fi
fi

echo "Clean was successful"

exit 0