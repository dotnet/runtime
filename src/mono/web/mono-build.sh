#! /usr/bin/env bash

# Script to automate the building of mono and its dependencies.
# Relies on wget being installed (could make it fall back to using
# lynx, links, w3, curl etc), assumes that gcc, make, tar, automake,
# etc are already installed too (may be worth testing for all that
# right at the top and bailing out if missing/too old/too new etc).


# See where we are.  This will become the top level directory for the
# installation, unless we are given an alternative location
here=$1
test -z "$here" && here=`pwd`

echo "Building Mono and dependencies in $here, installing to $here/install"

PATH=$here/install/bin:$PATH
LD_LIBRARY_PATH=$here/install/lib:$LD_LIBRARY_PATH

# Find a tool to fetch files. It must take an HTTP URL on the command line and
# save the file in the current directory.  (It must also talk HTTP/1.1, which
# rules out BSD's ftp(1), at least on FreeBSD 4.4.)
viable_downloaders="wget fetch"
for i in $viable_downloaders
do
    if which $i > /dev/null; then
	downloader=`which $i`
	break
    fi
done

if [ -z "$downloader" ]; then
    echo "Can't find a commandline download tool (tried: $viable_downloaders)"
    exit -1
else
    echo "Using $downloader to fetch files"
fi 

# We need to prefer GNU make if there's a choice.  BSD make falls over in
# the glib build if gtk-doc is disabled.
viable_makers="gmake make"
for i in $viable_makers
do
    if which $i > /dev/null; then
	MAKE=$i
	break
    fi
done

if [ -z "$MAKE" ]; then
    echo "Can't find a make tool (tried: $viable_makers)"
    exit -1
else
    echo "Using $MAKE"
    export MAKE
fi

# Need to install pkgconfig and set ACLOCAL_FLAGS if there is not a
# pkgconfig installed already.  Otherwise set PKG_CONFIG_PATH to the
# glib we're about to install in $here/install.  This script could
# attempt to be clever and see if glib 2 is already installed, too.


# --print-ac-dir was added in 1.2h according to the ChangeLog.  This
# should mean that any automake new enough for us has it.

function aclocal_scan () {
    # Quietly ignore the rogue '-I' and other aclocal flags that
    # aren't actually directories...
    for i in `aclocal --print-ac-dir` $ACLOCAL_FLAGS
    do
	if [ -f $i/$1 ]; then
	    return 0
	fi
    done

    return 1
}

function pkgconfig_scan () {
    module=$1

    echo "Finding pkgconfig files for $module..."

    # Should we use locate? or just a list of well-known directories?
    # locate has the problem of false positives in src dirs
    for i in /usr/lib/pkgconfig /usr/local/lib/pkgconfig
    do
	echo "Looking in $i..."
	if [ -f $i/${module}.pc ]; then
	    echo $i
	    return
	fi
    done
}

function install_package() {
    tarfile=$1
    dirname=$2
    name=$3
    configure_options=$4

    echo "Installing $name..."
    if [ ! -f $here/$tarfile ]; then
	(cd $here && $downloader http://www.go-mono.com/archive/$tarfile)
    fi

    # Assume that the package built correctly if the dir is there
    if [ ! -d $here/$dirname ]; then
	# Build and install package
	(cd $here && tar xzf $tarfile) || exit -1
	(cd $here/$dirname; ./configure --prefix=$here/install $configure_options || exit -1; $MAKE || exit -1; $MAKE install || exit -1)
	success=$?
	if [ $success -ne 0 ]; then
	    echo "***** $name build failure. Run rm -rf $here/$dirname to have this script attempt to build $name again next time"
	    exit -1
	fi
    fi
}

if aclocal_scan pkg.m4 ; then
    install_pkgconfig=no
else
    install_pkgconfig=yes
fi

if aclocal_scan glib-2.0.m4 ; then
    install_glib=no
    if [ $install_pkgconfig = "yes" ]; then
	# We have to tell the newly-installed pkgconfig about the
	# system-installed glib
	PKG_CONFIG_PATH=`pkgconfig_scan glib-2.0`:$PKG_CONFIG_PATH
    fi
else
    install_glib=yes
    PKG_CONFIG_PATH="$here/install/lib/pkgconfig:$PKG_CONFIG_PATH"
fi

if [ -f /usr/include/gc/gc.h ]; then
    install_libgc=no
else
    install_libgc=yes
fi

if [ $install_pkgconfig = "yes" -o $install_glib = "yes" ]; then
    ACLOCAL_FLAGS="-I $here/install/share/aclocal $ACLOCAL_FLAGS"
fi

export PATH
export LD_LIBRARY_PATH
export ACLOCAL_FLAGS
export PKG_CONFIG_PATH

# Freebsd puts iconv in /usr/local, so see if we need to add
# /usr/local/include and /usr/local/lib to CPPFLAGS and LDFLAGS.  We could
# skip this if it would add /usr/include and /usr/lib, but leaving it
# shouldnt break anything.
#
# Actually, it does break stuff :-(  gcc 3.2 prints gratuitous warnings
# and configure fails to find header files because of this cpp output.

if [ ! -f /usr/include/iconv.h ]; then
	iconvdirs="/usr/local/include"
	for i in $iconvdirs
	do
		if [ -f $i/iconv.h ]; then
			iconvh_dir=$i
			break
		fi
	done

	if [ -z "$iconvh_dir" ]; then
	    echo "Can't find iconv headers (looked in $iconvdirs)"
	    exit -1
	fi

	iconvlib_dir=`echo $iconvh_dir | sed -e 's/include/lib/'`

	echo "Adding $iconvh_dir to CPPFLAGS"
	echo "Adding $iconvlib_dir to LDFLAGS"

	CPPFLAGS="$CPPFLAGS -I$here/install/include -I$iconvh_dir"
	LDFLAGS="$LDFLAGS -L$here/install/lib -L$iconvlib_dir"
else
	CPPFLAGS="$CPPFLAGS -I$here/install/include"
	LDFLAGS="$LDFLAGS -L$here/install/lib"
fi

export CPPFLAGS
export LDFLAGS

# Grab pkg-config, glib and libgc if necessary

if [ $install_pkgconfig = "yes" ]; then
    install_package pkgconfig-0.8.0.tar.gz pkgconfig-0.8.0 pkgconfig ""
else
    echo "Not installing pkgconfig, you already seem to have it installed"
fi

if [ $install_glib = "yes" ]; then
    install_package glib-2.0.6.tar.gz glib-2.0.6 glib ""
else
    echo "Not installing glib, you already seem to have it installed"
fi

if [ $install_libgc = "yes" ]; then
    install_package gc6.1alpha5.tar.gz gc6.1alpha5 libgc "--enable-threads=pthreads"
    # make install didnt do the headers!
    mkdir -p $here/install/include/gc
    cp -r $here/gc6.1alpha5/include/* $here/install/include/gc
else
    echo "Not installing libgc, you already seem to have it installed"
fi

# End of build dependencies, now get the latest mono checkout and build that

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
    # Chop off the trailing /mono because cvs 1.11 adds the port number
    # into the .cvspass line
    if ! grep ${CVSROOT%:/mono} ~/.cvspass > /dev/null 2>&1 ; then
	echo "Logging into CVS server.  Anonymous CVS password is probably empty"
	cvs login
    fi
fi

(cd $here && cvs checkout mono) || exit -1

# Build and install mono
echo "Building and installing mono"

(cd $here/mono; ./autogen.sh --prefix=$here/install || exit -1; $MAKE || exit -1; $MAKE install || exit -1) || exit -1


echo ""
echo ""
echo "All done."
echo "Add $here/install/bin to \$PATH"
echo "Add $here/install/lib to \$LD_LIBRARY_PATH"
echo "Don't forget to copy the class libraries to $here/install/lib"

