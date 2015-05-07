#!/bin/sh
# LLDB installation script for FreeBSD 10.1 STABLE
# LLDB is yet not available in FreeBSD ports/packages, so we have to fetch 
# it from the LLVM source tree and build it ouselves. 
# Prerequisite packages, which must be installed prior to running this script:
# git python cmake ninja swig13
# 
# You can intall the tools by running (as root):
# pkg install git python cmake ninja swig13
# 

GIT=/usr/local/bin/git
CMAKE=/usr/local/bin/cmake
NINJA=/usr/local/bin/ninja
PYTHON_PATH=/usr/local/include/python2.7

$GIT clone http://llvm.org/git/llvm.git
cd llvm/tools
$GIT clone http://llvm.org/git/clang.git
$GIT clone http://llvm.org/git/lldb.git

cd ..
mkdir build
cd build

$CMAKE ../ -G Ninja -DCMAKE_CXX_FLAGS="-std=c++11 -stdlib=libc++ -I$PYTHON_PATH -g"
$NINJA lldb
#$NINJA check-lldb # if you want to run tests

su <<EOSU
$NINJA lldb install
EOSU
