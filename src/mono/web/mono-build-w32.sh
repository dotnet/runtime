#!/bin/bash

# Script to automate the building of mono and its dependencies on
# cygwin.  Relies on wget being installed (could make it fall back to
# using lynx, links, w3, curl etc), assumes that gcc, make, tar,
# automake, etc are already installed too (may be worth testing for
# all that right at the top and bailing out if missing/too old/too new
# etc).


# See where we are.  This will become the top level directory for the
# installation, unless we are given an alternative location
here=$1
test -z "$here" && here=`pwd`

echo "Building Mono and dependencies in $here, installing to $here/install"

PATH=$here/install/bin:$here/install/lib:$PATH
export C_INCLUDE_PATH=$here/install/include

# Make sure cygwin's libiconv is installed, or libtool blows its tiny mind
if [ ! -f /usr/lib/libiconv.la ]; then
    echo "You need to install the cygwin \"libiconv\" package!"
    exit -1
fi

# Check mono out first, so we can run aclocal from inside the mono dir (it
# needs to see which version of the real aclocal to run)
test -z "$CVSROOT" && CVSROOT=:pserver:anonymous@anoncvs.go-mono.com:/mono
export CVSROOT

echo "Updating mono"

# cvs checkout does the same as cvs update, except that it copes with
# new modules being added

# Older versions of cvs insist on a cvs login for :pserver: methods
# Make sure cvs is using ssh for :ext: methods

if [ ${CVSROOT:0:5} = ":ext:" ]; then
    CVS_RSH=ssh
    export CVS_RSH
elif [ ${CVSROOT:0:9} = ":pserver:" ]; then
    if ! grep $CVSROOT ~/.cvspass > /dev/null 2>&1 ; then
	echo "Logging into CVS server.  Anonymous CVS password is probably empty"
	cvs login || exit -1
    fi
fi

cvs checkout mono || exit -1

echo "Checking automake version"
automake_required="1.6.2"
automake_version=`automake --version | head -1 | awk '{print $4}' | tr -d '[a-zA-Z]' | sed 's/-.*$//g'`
echo "Found automake version $automake_version"
if expr $automake_version \< $automake_required > /dev/null; then
	echo "Your automake is too old!  You need version $automake_required or newer."
	exit -1
else
	echo "Automake version new enough."
fi

# This causes libgc-not-found problem
#
## Select the stable version anyway...
#if [ ! -z "${AUTO_STABLE}" -o -e /usr/autotool/stable ]; then
#    export AUTO_STABLE=${AUTO_STABLE:-/usr/autotool/stable}
#    export AUTO_DEVEL=${AUTO_STABLE}
#fi

# Need to install pkgconfig and set ACLOCAL_FLAGS if there is not a
# pkgconfig installed already.  Otherwise set PKG_CONFIG_PATH to the
# glib we're about to install in $here/install.


# --print-ac-dir was added in 1.2h according to the ChangeLog.  This
# should mean that any automake new enough for us has it.

# This sets ACLOCAL_FLAGS to point to the freshly installed pkgconfig
# if it doesnt already exist on the system (otherwise auto* breaks if
# it finds two copies of the m4 macros).  The GIMP for Windows
# pkgconfig sets its prefix based on the location of its binary, so we
# dont need PKG_CONFIG_PATH (the internal pkgconfig config file
# $prefix is handled similarly). For the cygwin pkgconfig we do need to
# set it, and we need to edit the mingw pc files too.

function aclocal_scan () {
    # Quietly ignore the rogue '-I' and other aclocal flags that
    # aren't actually directories...
    #
    # cd into mono/ so that the aclocal wrapper can work out which version
    # of aclocal to run, and add /usr/share/aclocal too cos aclocal looks there
    # too.
    for i in `(cd mono && aclocal --print-ac-dir)` /usr/share/aclocal $ACLOCAL_FLAGS
    do
	if [ -f $i/$1 ]; then
	    return 0
	fi
    done

    return 1
}

function install_icuconfig() {
    if [ ! -f $here/install/bin/icu-config ]; then
        wget http://www.go-mono.com/archive/icu-config
	mv icu-config $here/install/bin
        chmod 755 $here/install/bin/icu-config
    fi
}


function install_package() {
    zipfile=$1
    markerfile=$2
    name=$3

    echo "Installing $name..."
    if [ ! -f $here/$zipfile ]; then
	wget http://www.go-mono.com/archive/$zipfile
    fi

    # Assume that the package is installed correctly if the marker
    # file is there
    if [ ! -f $here/install/$markerfile ]; then
	(cd $here/install || exit -1; unzip -o $here/$zipfile || exit -1) || exit -1
    fi
}

# pkgconfig is only used during the build, so we can use the cygwin version
# if it exists
if aclocal_scan pkg.m4 ; then
    install_pkgconfig=no
else
    install_pkgconfig=yes
fi

# This causes libgc-not-found problem
#
## But we still need to use the mingw libs for glib & co
#ACLOCAL_FLAGS="-I $here/install/share/aclocal $ACLOCAL_FLAGS"

#export PATH
#export ACLOCAL_FLAGS

# Grab pkg-config, glib etc
if [ ! -d $here/install ]; then
    mkdir $here/install || exit -1
fi

# Fetch and install pkg-config, glib, iconv, intl

if [ $install_pkgconfig = "yes" ]; then
    install_package pkgconfig-0.11-20020310.zip bin/pkg-config.exe pkgconfig
else
    echo "Not installing pkgconfig, you already seem to have it installed"
fi
install_package glib-2.0.4-20020703.zip lib/libglib-2.0-0.dll glib
install_package glib-dev-2.0.4-20020703.zip lib/glib-2.0.lib glib-dev
install_package libiconv-1.7.zip lib/iconv.dll iconv
install_package libintl-0.10.40-20020101.zip lib/libintl-1.dll intl
install_package libgc-dev.zip lib/gc.dll gc-dev
install_package icu-2.6.1-Win32_msvc7.zip icu/bin/icuuc26.dll icu

install_icuconfig

if [ $install_pkgconfig = "no" ]; then
    echo "Fixing up the pkgconfig paths"
    for i in $here/install/lib/pkgconfig/*.pc
    do
	mv $i $i.orig
	sed -e "s@^prefix=/target\$@prefix=$here/install@" < $i.orig > $i
    done
    export PKG_CONFIG_PATH=$here/install/lib/pkgconfig
fi

# Needed to find the libgc bits
export CFLAGS="-I $here/install/include -I $here/install/icu/include"
export LDFLAGS="-L$here/install/lib -L$here/install/icu/lib"
export PATH="$here/install/icu/bin:$PATH"

# Make sure we build native w32, not cygwin
#CC="gcc -mno-cygwin"
#export CC

# --prefix is used to set the class library dir in mono, and it needs
# to be in windows-native form.  It also needs to have '\' turned into
# '/' to avoid quoting issues during the build.
prefix=`cygpath -w $here/install | sed -e 's@\\\\@/@g'`

# Build and install mono
echo "Building and installing mono"

(cd $here/mono; ./autogen.sh --prefix=$prefix || exit -1; make || exit -1; make install || exit -1) || exit -1


echo ""
echo ""
echo "All done."
echo "Add $here/install/bin and $here/install/lib to \$PATH"
echo "Don't forget to copy the class libraries to $here/install/lib"

