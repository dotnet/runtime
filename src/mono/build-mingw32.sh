#!/bin/bash -e
CURDIR="`pwd`"
MINGW=i386-mingw32msvc
CROSS_DIR=/opt/cross/$MINGW
EXTRA_CROSS_DIR=
INSTALL_DESTDIR="$CURDIR/mono-win32"
PROFILES="default net_2_0 net_3_5 net_4_0 net_4_5 moonlight"
TEMPORARY_PKG_CONFIG_DIR=/tmp/$RANDOM-pkg-config-$RANDOM
ORIGINAL_PATH="$PATH"

export CPPFLAGS_FOR_EGLIB CFLAGS_FOR_EGLIB CPPFLAGS_FOR_LIBGC CFLAGS_FOR_LIBGC

function cleanup ()
{
    if [ -d "$TEMPORARY_PKG_CONFIG_DIR" ]; then
	rm -rf "$TEMPORARY_PKG_CONFIG_DIR"
    fi
}

function check_pkg_config_dir ()
{
    local DIR="$1"
    local DIR_PREFIX="$2"

    if [ ! -d "$DIR" ]; then
	return
    fi

    install -d -m 755 "$TEMPORARY_PKG_CONFIG_DIR"
    for pc in "$DIR"/*.pc; do
	if [ -f $pc ]; then
	    pcname="`basename $pc`"
	    sed -e "s;^prefix=.*;prefix=$DIR_PREFIX;g" < $pc > "$TEMPORARY_PKG_CONFIG_DIR"/$pcname
	fi;
    done

    if [ -z "$CROSS_PKG_CONFIG_DIR" ]; then
	CROSS_PKG_CONFIG_DIR="$TEMPORARY_PKG_CONFIG_DIR"
    fi
}

function show_build_info ()
{
    cat <<EOF
Installation prefix: $MONO_PREFIX
           CPPFLAGS: ${CPPFLAGS:=not set}
            LDFLAGS: ${LDFLAGS:=not set}
          MONO_PATH: ${MONO_PATH:=not set}
EOF
}

function setup ()
{
    local pcname

    CROSS_BIN_DIR="$CROSS_DIR/bin"
    CROSS_DLL_DIR="$CROSS_DIR/bin"
    PATH=$CROSS_BIN_DIR:$PATH

    MONO_VERSION=`grep AC_INIT configure.in | cut -d ',' -f 2|tr -d '\[ \]'`
    
    if [ -d ./.git ]; then
	MONO_GIT_COMMIT="`git log -1 --format=format:%t`"
	MONO_GIT_BRANCH="`git branch|grep '\*'|cut -d ' ' -f 2|tr -d '\)'|tr -d '\('`"
	MONO_RELEASE="$MONO_VERSION-$MONO_GIT_BRANCH-$MONO_GIT_COMMIT"
    else
	MONO_RELEASE="$MONO_VERSION"
    fi

    MONO_PREFIX="$MONO_PREFIX/mono-$MONO_RELEASE"

    NOCONFIGURE=yes
    export NOCONFIGURE

    check_pkg_config_dir "$CROSS_DIR/lib/pkgconfig" "$CROSS_DIR"

    if [ -n "$EXTRA_CROSS_DIR" -a -d "$EXTRA_CROSS_DIR" ]; then
	if [ -d "$EXTRA_CROSS_DIR/bin" ]; then
		PATH="$EXTRA_CROSS_DIR/bin":$PATH
	fi
	
	check_pkg_config_dir "$EXTRA_CROSS_DIR/lib/pkgconfig" "$EXTRA_CROSS_DIR"

	if [ -d "$EXTRA_CROSS_DIR/include" ]; then
	    if [ -z "$CPPFLAGS" ]; then
		CPPFLAGS="-I \"$EXTRA_CROSS_DIR/include\""
	    else
		CPPFLAGS="-I \"$EXTRA_CROSS_DIR/include\" $CFLAGS"
	    fi
	fi

	if [ -d "$EXTRA_CROSS_DIR/lib" ]; then
	    if [ -z "$LDFLAGS" ]; then
		LDFLAGS="-I \"$EXTRA_CROSS_DIR/lib\""
	    else
		LDFLAGS="-I \"$EXTRA_CROSS_DIR/lib\" $LDFLAGS"
	    fi
	fi

	if [ -d "$EXTRA_CROSS_DIR/share/aclocal" ]; then
	    if [ -z "$MONO_PATH" ]; then
		MONO_PATH="\"$EXTRA_CROSS_DIR\""
	    else
		MONO_PATH="\"$EXTRA_CROSS_DIR\":$MONO_PATH"
	    fi
	fi
    fi
    
    export PATH MONO_PATH CPPFLAGS
    show_build_info
}

function build ()
{
    if [ -f ./Makefile ]; then
	make distclean
    fi

    if [ -d ./autom4te.cache ]; then
	rm -rf ./autom4te.cache
    fi

    if [ -f ./config.status ]; then
	for f in `find -name config.status -type f`; do
	    rm $f
	done
    fi

    ./autogen.sh 

    BUILD="`./config.guess`"

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

    if test -d $CURDIR/mcs; then
      mcsdir=$CURDIR/mcs
    else
      mcsdir=$CURDIR/../mcs
    fi

    cd "$mcsdir/mcs"
    for p in $PROFILES; do
	make DESTDIR="$INSTALL_DESTDIR" PROFILE=$p install || echo "mcs profile $p installation failed"
    done

    cd "$mcsdir/class"
    for p in $PROFILES; do
	make DESTDIR="$INSTALL_DESTDIR" PROFILE=$p install || echo "class library profile $p installation failed"
    done

    cd "$mcsdir/tools"
    for p in $PROFILES; do
	make DESTDIR="$INSTALL_DESTDIR" PROFILE=$p install || echo "tools profile $p installation failed"
    done

    cd "$CURDIR/mono-win32"
    rm -f "$CURDIR/mono-win32-$MONO_RELEASE".zip
    zip -9r "$CURDIR/mono-win32-$MONO_RELEASE".zip .

}

function usage ()
{
    cat <<EOF
Usage: build-mingw32.sh [OPTIONS]

where OPTIONS are:

 -d DIR     Sets the location of directory where MINGW is installed [$CROSS_DIR]
 -e DIR     Sets the location of directory where additional cross develoment packages are installed [${EXTRA_CROSS_DIR:=none}]
 -m MINGW   Sets the MINGW target name to be passed to configure [$MINGW]
 -p PREFIX  Prefix at which Mono is to be installed. Build will append the 'mono-X.Y' string to that path
EOF

    exit 1
}

trap cleanup 0

pushd . > /dev/null

while getopts "d:m:e:p:" opt; do
    case "$opt" in
	d) CROSS_DIR="$OPTARG" ;;
	m) MINGW="$OPTARG" ;;
	e) EXTRA_CROSS_DIR="$OPTARG" ;;
	p) MONO_PREFIX="$OPTARG" ;;
	*) usage ;;
    esac
done

setup
build
doinstall
show_build_info

popd > /dev/null
