#!/bin/sh
ASPEN_ROOT=/Developer/Platforms/iPhoneOS.platform/Developer
ASPEN_SDK=$ASPEN_ROOT/SDKs/iPhoneOS2.0.sdk/

ORIG_PATH=$PATH
GLIB_FLAGS=`pkg-config --cflags --libs glib-2.0`

if [ -z "$GLIB_FLAGS" ]; then
	echo "pkg-config could not locate glib-2.0 needed for the monoburg step"
	exit;
fi

setenv () {
	export PATH=$ASPEN_ROOT/usr/bin:$PATH

	export C_INCLUDE_PATH="$ASPEN_SDK/usr/lib/gcc/arm-apple-darwin9/4.0.1/include:$ASPEN_SDK/usr/include"
	export CPLUS_INCLUDE_PATH="$ASPEN_SDK/usr/lib/gcc/arm-apple-darwin9/4.0.1/include:$ASPEN_SDK/usr/include"
	export CC=arm-apple-darwin9-gcc-4.0.1
	export CXX=arm-apple-darwin9-g++-4.0.1
	export CPP="cpp-4.0 -nostdinc -U__powerpc__ -U__i386__ -D__arm__"
	export CXXPP="cpp-4.0 -nostdinc -U__powerpc__ -U__i386__ -D__arm__"
	export LD=$CC
	export LDFLAGS="-liconv -Wl,-syslibroot,$ASPEN_SDK"
}

unsetenv () {
	export PATH=$ORIG_PATH

	unset C_INCLUDE_PATH
	unset CPLUS_INCLUDE_PATH
	unset CC
	unset CXX
	unset CPP
	unset CXXPP
	unset LD
	unset LDFLAGS
}

export mono_cv_uscore=yes
export cv_mono_sizeof_sunpath=104
export ac_cv_func_posix_getpwuid_r=yes
export ac_cv_func_backtrace_symbols=no


setenv

pushd eglib 
./autogen.sh --host=arm-apple-darwin9
popd

./autogen.sh --disable-mcs-build --host=arm-apple-darwin9 --disable-shared-handles --with-tls=pthread --with-sigaltstack=no --with-glib=embedded $@
perl -pi -e 's/MONO_SIZEOF_SUNPATH 0/MONO_SIZEOF_SUNPATH 104/' config.h
perl -pi -e 's/#define HAVE_FINITE 1//' config.h
perl -pi -e 's/#define HAVE_MMAP 1//' config.h
make

unsetenv
pushd mono/monoburg
/usr/bin/gcc -o monoburg ./monoburg.c parser.c -I../.. -pthread -lm $GLIB_FLAGS
make
popd

setenv
make
