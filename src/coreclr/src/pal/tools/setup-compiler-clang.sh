#!/usr/bin/env bash
#
# This file sets the environment to be used for building with clang.
#

if which clang-3.5 > /dev/null 2>&1 
    then  
        export CC="$(which clang-3.5)" 
        export CXX="$(which clang++-3.5)"    
elif which clang > /dev/null 2>&1 
    then 
        export CC="$(which clang)" 
        export CXX="$(which clang++)"   
else  
    echo "Unable to find Clang Compiler" 
    exit 1 
fi
