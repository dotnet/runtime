AUTOMAKE_OPTIONS = foreign

SUBDIRS = @libgc_dir@ mono @ikvm_jni_dir@ docs runtime scripts man data samples support web 

# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = libgc mono ikvm-jni docs runtime scripts man data samples support web tools

EXTRA_DIST= mono.pc.in mono.spec.in mint.pc.in

pkgconfigdir = $(libdir)/pkgconfig
if JIT_SUPPORTED
pkgconfig_DATA= mono.pc mint.pc
else
pkgconfig_DATA= mint.pc
endif
DISTCLEANFILES= mono.pc mint.pc

#
# The following targets are used to compile and install
# mono and mcs without running into the out-of-sync
# problems by doing the compilation in the right order.
#

mcs_topdir=$(top_srcdir)/../mcs

.PHONY: bootstrap faststrap fasterstrap bootstrap-clean bootstrap-check get-monolite-latest monolite-bootstrap
.PHONY: tmpinst-dir tmpinst-mcs-basic-build tmpinst-mcs-short-build tmpinst-mcs-full-build tmpinst-monolite
.PHONY: mono-do-runtime-only mcs-do-basic-build mcs-do-short-build mcs-do-full-build mcs-do-clean-profiles mcs-do-run-test

if USE_JIT
mono_runtime = mono/mini/mono
else
mono_runtime = mono/interpreter/mint
endif

# Minimal set of directories to build, and files used to run the 'mcs' compiler
MCS_BASIC_DIRS = jay mcs class/corlib class/System
MCS_BASIC_FILES = \
	$(mcs_topdir)/mcs/mcs.exe \
	$(mcs_topdir)/class/lib/default/mscorlib.dll \
	$(mcs_topdir)/class/lib/default/System.dll

# Set of directories and files the 'mcs' compiler can use
## We build class/System twice to resolve the cyclic dependency of class/System.XML
MCS_DIRS = $(MCS_BASIC_DIRS) class/System.XML class/System class/Mono.CSharp.Debugger
MCS_FILES = $(MCS_BASIC_FILES) \
	$(mcs_topdir)/class/lib/default/System.Xml.dll \
	$(mcs_topdir)/class/lib/default/Mono.CSharp.Debugger.dll

bootstrap: tmpinst-mcs-basic-build mono-do-runtime-only
	$(MAKE) tmpinst-mcs-short-build
	$(MAKE) fasterstrap

faststrap: tmpinst-mcs-short-build
	$(MAKE) mono-do-runtime-only
	$(MAKE) fasterstrap

fasterstrap: tmpinst-mcs-full-build
	rm -f $(top_srcdir)/runtime/*/*.dll $(top_srcdir)/runtime/*.exe
	$(MAKE)

bootstrap-clean: clean mcs-do-clean-profiles
bootstrap-check: check mcs-do-run-test

# building with monolite

get-monolite-latest:
	wget -O $(srcdir)/../monolite-latest.tar.gz http://www.go-mono.com/daily/monolite-latest.tar.gz

monolite-bootstrap: tmpinst-monolite mono-do-runtime-only
	rm -f $(MCS_FILES)
	$(MAKE) tmpinst-mcs-short-build
	$(MAKE) fasterstrap

# internal targets

tmpinst-mcs-basic-build: mcs-do-basic-build tmpinst-dir
	mv -f $(MCS_BASIC_FILES) _tmpinst/lib

tmpinst-mcs-short-build: mcs-do-short-build
	mv -f $(MCS_FILES) _tmpinst/lib

tmpinst-mcs-full-build: mcs-do-full-build
	cp -f $(MCS_FILES) _tmpinst/lib

tmpinst-monolite: tmpinst-dir
	-rm -f _tmpinst/lib/mcs.exe _tmpinst/lib/*.dll
	-rm -fr _tmpinst/monolite-*
	srcdir=`cd $(srcdir) && pwd` && cd _tmpinst && ( gzip -d -c $$srcdir/../monolite-latest.tar.gz | tar xf - )
	mv -f _tmpinst/monolite-*/mcs.exe _tmpinst/monolite-*/*.dll _tmpinst/lib
	rm -fr _tmpinst/monolite-*

# Build enough of 'mono' to run the stage1 mcs.exe
mono-do-runtime-only: $(CONFIG_HEADER)
	if test -z "$(libgc_dir)"; then :; else cd libgc && $(MAKE) ; fi
	cd mono && $(MAKE)

mcs-do-basic-build:
	rm -f $(MCS_FILES)
	for dir in $(MCS_BASIC_DIRS); do \
	  (cd $(mcs_topdir)/$$dir && $(MAKE) PROFILE=default BOOTSTRAP_MCS="mcs -d:BOOTSTRAP_WITH_OLDLIB") || exit 1 ; \
	done

mcs-do-short-build:
	tmpinst=`cd _tmpinst && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH; export PATH ; \
	for dir in $(MCS_DIRS); do \
	  (cd $(mcs_topdir)/$$dir && $(MAKE) PROFILE=default) || exit 1 ; \
	done

mcs-do-full-build:
	tmpinst=`cd _tmpinst && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	cd $(mcs_topdir) && $(MAKE) all-profiles

mcs-do-clean-profiles:
	cd $(mcs_topdir) && $(MAKE) clean-profiles

mcs-do-run-test: tmpinst-dir
	tmpinst=`cd _tmpinst && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	cd $(mcs_topdir) && $(MAKE) PROFILE=default run-test

tmpinst-dir.stamp: Makefile.am
	set -e ; \
	rootme=`pwd`; tmpinst=$$rootme/_tmpinst ; \
	mkdir -p $$tmpinst $$tmpinst/bin $$tmpinst/lib ; \
	echo '#! /bin/sh' > $$tmpinst/bin/mono ; \
	echo 'if test -z "$$MONO_PATH" ; then MONO_PATH="'"$$tmpinst/lib"'"' >> $$tmpinst/bin/mono ; \
	echo 'else MONO_PATH=$${MONO_PATH}:"'"$$tmpinst/lib"'" ; fi' >> $$tmpinst/bin/mono ; \
	echo 'export MONO_PATH' >> $$tmpinst/bin/mono ; \
	echo 'exec "'"$$rootme/libtool"'" --mode=execute "'"$$rootme/$(mono_runtime)"'" "$$@"' >> $$tmpinst/bin/mono ; \
	echo '#! /bin/sh' > $$tmpinst/bin/mcs ; \
	echo 'exec "'"$$tmpinst/bin/mono"'" "'"$$tmpinst/lib/mcs.exe"'" "$$@"' >> $$tmpinst/bin/mcs ; \
	chmod +x $$tmpinst/bin/mono $$tmpinst/bin/mcs
	echo "stamp" > $@

tmpinst-dir: tmpinst-dir.stamp
	@if test -d _tmpinst; then :; else rm -f tmpinst-dir.stamp; $(MAKE) tmpinst-dir.stamp; fi

# Obsolete target -- don't use.
.PHONY: fullbuild
fullbuild: bootstrap
	$(MAKE) install
	echo "*** 'fullbuild' is an obsolete target. Use 'make bootstrap && make install'."
	exit 1

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
