#!/bin/bash

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

# Need to install pkgconfig and set ACLOCAL_FLAGS if there is not a
# pkgconfig installed already.  Otherwise set PKG_CONFIG_PATH to the
# glib we're about to install in $here/install.  This script could
# attempt to be clever and see if glib 2 is already installed, too.


# --print-ac-dir was added in 1.2h according to the ChangeLog.  This
# should mean that any automake new enough for us has it.
if [ -f `aclocal --print-ac-dir`/pkg.m4 ]; then
    install_pkgconfig=no
    PKG_CONFIG_PATH="$here/install/lib/pkgconfig"
else
    install_pkgconfig=yes
    ACLOCAL_FLAGS="-I $here/install/share/aclocal $ACLOCAL_FLAGS"
fi


export PATH
export LD_LIBRARY_PATH
export ACLOCAL_FLAGS
export PKG_CONFIG_PATH

# Grab pkg-config-0.8, glib-1.3.12 if necessary

# If any more dependencies are added, it would be worth encapsulating
# the configure; make; make install part in a shell function

if [ $install_pkgconfig = "yes" ]; then
    echo "Installing pkgconfig..."
    if [ ! -f $here/pkgconfig-0.8.0.tar.gz ]; then
	wget --timestamping http://www.go-mono.org/archive/pkgconfig-0.8.0.tar.gz
    fi

    # Assume that pkgconfig built correctly if the dir is there
    if [ ! -d $here/pkgconfig-0.8.0 ]; then
	# Build and install pkg-config
	tar xzf $here/pkgconfig-0.8.0.tar.gz || exit -1
	(cd $here/pkgconfig-0.8.0; ./configure --prefix=$here/install || exit -1; make || exit -1; make install || exit -1)
	success=$?
	if [ $success -ne 0 ]; then
	    echo "***** pkgconfig build failure. Run rm -rf $here/pkgconfig-0.8.0 to have this script attempt to build pkgconfig again next time"
	    exit -1
	fi
    fi
else
    echo "Not installing pkgconfig, you already seem to have it installed"
fi


echo "Installing glib..."
if [ ! -f $here/glib-1.3.13.tar.gz ]; then
    wget --timestamping http://www.go-mono.org/archive/glib-1.3.13.tar.gz
fi

# Assume that glib built correctly if the dir is there
if [ ! -d $here/glib-1.3.13 ]; then
    # Build and install glib
    tar xzf $here/glib-1.3.13.tar.gz || exit -1
    (cd $here/glib-1.3.13; ./configure --prefix=$here/install || exit -1; make || exit -1; make install || exit -1)
    success=$?
    if [ $success -ne 0 ]; then
	echo "***** glib build failure. Run rm -rf $here/glib-1.3.13 to have this script attempt to build glib again next time"
	exit -1
    fi
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
    if ! grep $CVSROOT ~/.cvspass > /dev/null 2>&1 ; then
	echo "Logging into CVS server.  Anonymous CVS password is probably empty"
	cvs login
    fi
fi

cvs checkout mono || exit -1

# Build and install mono
echo "Building and installing mono"

(cd $here/mono; ./autogen.sh --prefix=$here/install || exit -1; make || exit -1; make install || exit -1) || exit -1


echo ""
echo ""
echo "All done."
echo "Add $here/install/bin to \$PATH"
echo "Add $here/install/lib to \$LD_LIBRARY_PATH"
echo "Don't forget to copy the class libraries to $here/install/lib"

