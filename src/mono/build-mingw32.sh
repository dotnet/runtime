#!/bin/bash -e
CURDIR="`pwd`"
CROSS_DIR=${1:-/opt/cross/}
MINGW=${1:-i386-mingw32msvc}
CROSS_BIN_DIR="$CROSS_DIR/bin"
CROSS_DLL_DIR="$CROSS_DIR/$MINGW/bin"
CROSS_PKG_CONFIG_DIR=$CROSS_DIR/$MINGW/lib/pkgconfig
COPY_DLLS="libgio*.dll libglib*.dll libgmodule*.dll libgthread*.dll libgobject*.dll"
PATH=$CROSS_BIN_DIR:$PATH
INSTALL_DESTDIR="$CURDIR/mono-win32"
PROFILES="default net_2_0 net_2_1 net_3_5"

export PATH

function setup ()
{
    if [ -d ./.git/svn ]; then
	SVN_INFO='git svn info'
    elif [ -d ./.svn ]; then
	SVN_INFO='svn info'
    else
	SVN_INFO=""
    fi

    if [ -n "$SVN_INFO" ]; then
	MONO_SVN_REVISION=`$SVN_INFO | grep Revision | sed 's/.*: //'`
	MONO_BRANCH=`$SVN_INFO | grep URL | sed -e 's;.*source/;;g' -e 's;/mono;;g'`
    else
	MONO_SVN_REVISION="rUNKNOWN"
	MONO_BRANCH="tarball"
    fi

    MONO_VERSION=`grep AM_INIT_AUTOMAKE configure.in | cut -d ',' -f 2|tr -d '\)'`
    MONO_RELEASE="$MONO_VERSION-$MONO_BRANCH-r$MONO_SVN_REVISION"
    MONO_PREFIX="/mono-$MONO_RELEASE"

    NOCONFIGURE=yes
    export NOCONFIGURE

    echo Mono Win32 installation prefix: $MONO_PREFIX
}

function build ()
{
    ./autogen.sh 

    if [ -f ./Makefile ]; then
	make distclean
    fi

    if [ ! -d "$CURDIR/build-cross-windows" ]; then
	mkdir "$CURDIR/build-cross-windows"
    fi

    cd "$CURDIR/build-cross-windows"
    rm -rf *
    ../configure --prefix=$MONO_PREFIX --with-crosspkgdir=$CROSS_PKG_CONFIG_DIR --target=$MINGW --host=$MINGW --enable-parallel-mark --program-transform-name=""
    make
    cd "$CURDIR"

    if [ ! -d "$CURDIR/build-cross-windows-mcs" ]; then
	mkdir "$CURDIR/build-cross-windows-mcs"
    fi
    cd "$CURDIR/build-cross-windows-mcs"
    rm -rf *
    ../configure --prefix=$MONO_PREFIX --enable-parallel-mark
    make
}

function doinstall ()
{
    if [ -d "$INSTALL_DIR" ]; then
	rm -rf "$INSTALL_DIR"
    fi
    cd "$CURDIR/build-cross-windows"
    make DESTDIR="$INSTALL_DESTDIR" USE_BATCH_FILES=yes install

    cd "$CURDIR/../mcs/mcs"

    for p in $PROFILES; do
	make DESTDIR="$INSTALL_DESTDIR" PROFILE=$p install || echo "mcs profile $p installation failed"
    done

    cd "$CURDIR/../mcs/class"
    for p in $PROFILES; do
	make DESTDIR="$INSTALL_DESTDIR" PROFILE=$p install || echo "class library profile $p installation failed"
    done

    cd "$CURDIR/../mcs/tools"
    for p in $PROFILES; do
	make DESTDIR="$INSTALL_DESTDIR" PROFILE=$p install || echo "tools profile $p installation failed"
    done

    cd "$CURDIR/mono-win32"
    for dll in $COPY_DLLS; do
	cp -ap "$CROSS_DLL_DIR"/$dll "$INSTALL_DESTDIR/$MONO_PREFIX/bin"
    done

    rm -f "$CURDIR/mono-win32-$MONO_RELEASE".zip
    zip -9r "$CURDIR/mono-win32-$MONO_RELEASE".zip .

}

pushd . > /dev/null

setup
build
doinstall

popd > /dev/null
