AUTOMAKE_OPTIONS = foreign

SUBDIRS = @libgc_dir@ mono doc docs runtime scripts man data samples

dnl EXTRA_SUBDIRS = libgc

EXTRA_DIST= mono.pc.in mono.spec.in

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA= mono.pc
DISTCLEANFILES= mono.pc

#
# The following target is used to compile and install
# mono and mcs without running into the out-of-sync
# problems by doing the compilation in the right order.
#

fullbuild: remove-binaries mcs-tree-safe-build xinstall-runtime mcs-rest install

mcs-tree-safe-build:
	(cd ../mcs/jay; $(MAKE))
	(cd ../mcs/mcs; $(MAKE) MCS=mcs)
	(cd ../mcs/class/corlib; $(MAKE) MCS=mcs)
	cp ../mcs/class/lib/corlib.dll runtime
	cp ../mcs/mcs/mcs.exe runtime

xinstall-runtime:
	if echo $(SUBDIRS) | grep "libgc " > /dev/null; then (cd libgc; $(MAKE) && $(MAKE) install); fi
	(cd mono; $(MAKE) && $(MAKE) install)

mcs-rest:
	(cd ../mcs/class; make)

remove-binaries:
	rm ../mcs/class/lib/corlib.dll ../mcs/mcs/mcs.exe runtime/*dll runtime/*.exe >& /dev/null; echo


win32getdeps:
	wget http://www.go-mono.com/archive/pkgconfig-0.11-20020310.zip
	wget http://www.go-mono.com/archive/glib-2.0.4-20020703.zip 
	wget http://www.go-mono.com/archive/glib-dev-2.0.4-20020703.zip 
	wget http://www.go-mono.com/archive/libiconv-1.7.zip 
	wget http://www.go-mono.com/archive/libiconv-dev-1.7.zip 
	wget http://www.go-mono.com/archive/libintl-0.10.40-20020101.zip
	wget http://www.jroith.de/nsis4cygwin.zip
	unzip -n -d / pkgconfig-0.11-20020310.zip
	unzip -n -d / glib-2.0.4-20020703.zip
	unzip -n -d / glib-dev-2.0.4-20020703.zip
	unzip -n -d / libiconv-1.7.zip
	unzip -n -d / libiconv-dev-1.7.zip
	unzip -n -d / libintl-0.10.40-20020101.zip
	unzip -n -d / nsis4cygwin.zip

win32setup:
	cp /usr/lib/libglib-2.0-0.dll @prefix@/lib
	cp /usr/lib/iconv.dll @prefix@/lib
	cp /usr/lib/libgmodule-2.0-0.dll @prefix@/lib
	cp /usr/lib/libgobject-2.0-0.dll @prefix@/lib
	cp /usr/lib/libgthread-2.0-0.dll @prefix@/lib
	cp /usr/lib/libintl-1.dll @prefix@/lib
	makensis /DMILESTONE=@VERSION@ /DSOURCE_INSTALL_DIR=`cygpath -a -w @prefix@`\\* monowiz.win32.nsi
