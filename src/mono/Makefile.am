AUTOMAKE_OPTIONS = foreign
ACLOCAL_AMFLAGS = -I .

SUBDIRS = $(libgc_dir) mono $(ikvm_jni_dir) docs data runtime scripts man samples support web 

# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = libgc mono ikvm-jni docs data runtime scripts man samples support web tools

EXTRA_DIST= mono.pc.in mono.spec.in mint.pc.in

# Distribute the 'mcs' tree too
dist-hook:
	test -d $(distdir)/mcs || mkdir $(distdir)/mcs
	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) PROFILE=default distdir=$$d dist-recursive

pkgconfigdir = $(libdir)/pkgconfig
if JIT_SUPPORTED
pkgconfig_DATA= mono.pc mint.pc
else
pkgconfig_DATA= mint.pc
endif
DISTCLEANFILES= mono.pc mint.pc

.PHONY: get-monolite-latest mcs-do-compiler-tests compiler-tests bootstrap-world

# building with monolite
mcslib = $(mcs_topdir)/class/lib
monolite = $(mcslib)/monolite
get-monolite-latest:
	-rm -fr $(mcslib)/monolite-*
	test ! -d $(monolite) || test ! -d $(monolite).old || rm -fr $(monolite).old
	test ! -d $(monolite) || mv -f $(monolite) $(monolite).old
	cd $(mcslib) && { wget -O- http://www.go-mono.com/daily/monolite-latest.tar.gz | gzip -d | tar xf - ; }
	cd $(mcslib) && mv -f monolite-* monolite

compiler-tests:
	cd runtime && $(MAKE) clean-local
	$(MAKE)
	$(MAKE) mcs-do-compiler-tests

bootstrap-world: compiler-tests
	$(MAKE) install

# internal targets
mcs-do-compiler-tests:
	cd runtime && $(MAKE) test_select='TEST_SUBDIRS="tests errors"' check-local

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
	makensis /DMILESTONE=$(VERSION) /DSOURCE_INSTALL_DIR=$(SOURCE_INSTALL_DIR) /DBUILDNUM=$(BUILDNUM) monowiz.win32.nsi

bootstrap: all
	@echo "*** 'make bootstrap' is obsolete.  Just run 'make' to perform a combined mono+mcs build"
	exit 1
