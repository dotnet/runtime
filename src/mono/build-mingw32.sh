#!/bin/bash -e
CURDIR="`pwd`"
MINGW=i386-mingw32msvc
CROSS_DIR=/opt/cross/$MINGW
COPY_DLLS="libgio*.dll libglib*.dll libgmodule*.dll libgthread*.dll libgobject*.dll"
INSTALL_DESTDIR="$CURDIR/mono-win32"
PROFILES="default net_2_0 net_3_5 net_4_0 moonlight"
TEMPORARY_PKG_CONFIG_DIR=/tmp/$RANDOM-pkg-config-$RANDOM
ORIGINAL_PATH="$PATH"

export CPPFLAGS_FOR_EGLIB CFLAGS_FOR_EGLIB CPPFLAGS_FOR_LIBGC CFLAGS_FOR_LIBGC

function cleanup ()
{
    if [ -d "$TEMPORARY_PKG_CONFIG_DIR" ]; then
	rm -rf "$TEMPORARY_PKG_CONFIG_DIR"
    fi
}

function setup ()
{
    local pcname

    CROSS_BIN_DIR="$CROSS_DIR/bin"
    CROSS_DLL_DIR="$CROSS_DIR/bin"
    CROSS_PKG_CONFIG_DIR=$CROSS_DIR/lib/pkgconfig
    PATH=$CROSS_BIN_DIR:$PATH

    export PATH
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

    if [ -d "$CROSS_PKG_CONFIG_DIR" ]; then
	install -d -m 755 "$TEMPORARY_PKG_CONFIG_DIR"
	for pc in "$CROSS_PKG_CONFIG_DIR"/*.pc; do
	    pcname="`basename $pc`"
	    sed -e "s;^prefix=.*;prefix=$CROSS_DIR;g" < $pc > "$TEMPORARY_PKG_CONFIG_DIR"/$pcname
	done
	CROSS_PKG_CONFIG_DIR="$TEMPORARY_PKG_CONFIG_DIR"
    fi

    echo Mono Win32 installation prefix: $MONO_PREFIX
}

function build ()
{
    ./autogen.sh 

    BUILD="`./config.guess`"

    if [ -f ./Makefile ]; then
	make distclean
	rm -rf autom4te.cache
    fi

    if [ ! -d "$CURDIR/build-cross-windows" ]; then
	mkdir "$CURDIR/build-cross-windows"
    fi

    cd "$CURDIR/build-cross-windows"
    rm -rf *
    ../configure --prefix=$MONO_PREFIX --with-crosspkgdir=$CROSS_PKG_CONFIG_DIR --build=$BUILD --target=$MINGW --host=$MINGW --enable-parallel-mark --program-transform-name="" --with-tls=none --disable-mcs-build --disable-embed-check --enable-win32-dllmain=yes --with-libgc-threads=win32 --with-profile4=yes
    make
    cd "$CURDIR"

    if [ ! -d "$CURDIR/build-cross-windows-mcs" ]; then
	mkdir "$CURDIR/build-cross-windows-mcs"
    fi

    rm -rf autom4te.cache
    unset PATH
    PATH="$ORIGINAL_PATH"
    export PATH
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

function usage ()
{
    cat <<EOF
Usage: build-mingw32.sh [OPTIONS]

where OPTIONS are:

 -d DIR     Sets the location of directory where MINGW is installed [$CROSS_DIR]
 -m MINGW   Sets the MINGW target name to be passed to configure [$MINGW]
EOF

    exit 1
}

trap cleanup 0

pushd . > /dev/null

while getopts "d:m:h" opt; do
    case "$opt" in
	d) CROSS_DIR="$OPTARG" ;;
	m) MINGW="$OPTARG" ;;
	*) usage ;;
    esac
done

setup
build
doinstall

popd > /dev/null
