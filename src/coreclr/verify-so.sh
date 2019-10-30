#!/usr/bin/env bash
# $1 contains full path to the .so to verify
# $2 contains message to print when the verification fails

OSName=$(uname -s)
case $OSName in
    Linux)
        source /etc/os-release
        # TODO: add support for verification on Alpine Linux
        if [ "$ID" != "alpine" ]; then
            ldd -r $1 | awk 'BEGIN {count=0} /undefined symbol:/ { if (count==0) {print "Undefined symbol(s) found:"} print " " $3; count++ } END {if (count>0) exit(1)}'
            if [ $? != 0 ]; then
                echo "$2"
                exit 1
            fi
        fi
        ;;
esac

# TODO: add support for verification on non-Linux Unixes (except of OSX)
