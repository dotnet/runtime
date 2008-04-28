#!/bin/sh
ASPEN_ROOT=/Developer/Platforms/iPhoneOS.platform/Developer
ASPEN_SDK=$ASPEN_ROOT/SDKs/iPhoneOS2.0.sdk/

export PATH=$ASPEN_ROOT/usr/bin:$PATH

export C_INCLUDE_PATH="$ASPEN_SDK/usr/lib/gcc/arm-apple-darwin9/4.0.1/include:$ASPEN_SDK/usr/include"
export CPLUS_INCLUDE_PATH="$ASPEN_SDK/usr/lib/gcc/arm-apple-darwin9/4.0.1/include:$ASPEN_SDK/usr/include"
export CC=arm-apple-darwin9-gcc-4.0.1
export CXX=arm-apple-darwin9-g++-4.0.1
export CPP="cpp-4.0 -nostdinc -U__powerpc__ -U__i386__ -D__arm__"
export CXXPP="cpp-4.0 -nostdinc -U__powerpc__ -U__i386__ -D__arm__"
export LD=$CC
export LDFLAGS=-Wl,-syslibroot,$ASPEN_SDK

export mono_cv_uscore=yes
export cv_mono_sizeof_sunpath=104
export ac_cv_func_posix_getpwuid_r=yes
export ac_cv_func_backtrace_symbols=no

pushd eglib 
./autogen.sh --host=arm-apple-darwin9
popd

./autogen.sh --disable-mcs-build --host=arm-apple-darwin9 --disable-shared-handles --with-tls=pthread --with-sigaltstack=no --with-glib=embedded --with-gc=none $@

curl -o mono/mini/inssel.c http://primates.ximian.com/~gnorton/iphone/inssel.c
curl -o mono/mini/inssel.h http://primates.ximian.com/~gnorton/iphone/inssel.h

make

# We dont use monoburg since I nicely provided the inssel's pregenerated above.
pushd mono/monoburg
touch monoburg
touch sample.c
popd

make
