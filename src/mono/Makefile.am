AUTOMAKE_OPTIONS = foreign

SUBDIRS = @libgc_dir@ mono @ikvm_jni_dir@ docs runtime scripts man data samples support web

# Keep in sync with SUBDIRS
DIST_SUBDIRS = libgc mono ikvm-jni docs runtime scripts man data samples support web

# EXTRA_SUBDIRS = libgc

EXTRA_DIST= mono.pc.in mono.spec.in mint.pc.in

pkgconfigdir = $(libdir)/pkgconfig
if JIT_SUPPORTED
pkgconfig_DATA= mono.pc mint.pc
else
pkgconfig_DATA= mint.pc
endif
DISTCLEANFILES= mono.pc mint.pc

#
# The following target is used to compile and install
# mono and mcs without running into the out-of-sync
# problems by doing the compilation in the right order.
#

mcs_topdir=$(top_srcdir)/../mcs

.PHONY: fullbuild remove-binaries mcs-tree-safe-build xinstall-runtime mcs-rest

fullbuild: mcs-rest
	$(MAKE) install

mcs-tree-safe-build: remove-binaries
	cd $(mcs_topdir)/jay; $(MAKE)
	cd $(mcs_topdir)/mcs; $(MAKE) MCS=mcs BOOTSTRAP_MCS=mcs
	cd $(mcs_topdir)/class/corlib; $(MAKE) MCS=mcs BOOTSTRAP_MCS=mcs
	cp $(mcs_topdir)/class/lib/default/mscorlib.dll $(top_srcdir)/runtime
	cp $(mcs_topdir)/mcs/mcs.exe $(top_srcdir)/runtime

xinstall-runtime: mcs-tree-safe-build
	if test -z "$(libgc_dir)"; then :; else cd libgc; $(MAKE) && $(MAKE) install ; fi
	cd mono; $(MAKE) && $(MAKE) install
	cd runtime; $(MAKE) install assemblies_DATA=mscorlib.dll monobins_DATA=mcs.exe

mcs-rest: xinstall-runtime
	cd $(mcs_topdir)/class; $(MAKE)

remove-binaries:
	rm -f $(mcs_topdir)/class/lib/default/mscorlib.dll $(mcs_topdir)/mcs/mcs.exe $(top_srcdir)/runtime/*.dll $(top_srcdir)/runtime/*.exe


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
