AUTOMAKE_OPTIONS = foreign

SUBDIRS = @libgc_dir@ mono @ikvm_jni_dir@ docs runtime scripts man data samples support web tools

# Keep in sync with SUBDIRS
DIST_SUBDIRS = libgc mono ikvm-jni docs runtime scripts man data samples support web

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

.PHONY: tmpinst-dir tmpinst-runtime bootstrap stage1 stage2 stage3 stage1-mono stage1-mcs bootstrap-clean bootstrap-check

if USE_JIT
mono_runtime = mono/mini/mono
else
mono_runtime = mono/interpreter/mint
endif

bootstrap: stage3
	rm -f $(top_srcdir)/runtime/*/*.dll $(top_srcdir)/runtime/*.exe
	$(MAKE)

bootstrap-clean: clean mcs-do-clean-profiles
bootstrap-check: check mcs-do-run-test

.PHONY: mcs-do-clean-profiles mcs-do-run-test
mcs-do-clean-profiles:
	cd $(mcs_topdir) && $(MAKE) clean-profiles

mcs-do-run-test: tmpinst-dir
	tmpinst=`cd _tmpinst && pwd` ; mcs_topdir=`cd $(mcs_topdir) && pwd`; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	cd $(mcs_topdir) && $(MAKE) PROFILE=default TEST_RUNTIME="MONO_PATH=$$mcs_topdir/class/lib/default mono" run-test

# Build stripped down versions of 'mcs', 'mscorlib.dll' and 'System.dll' with installed 'mcs'
## remove System.Xml.dll since it's a soft-dependency of System.dll
stage1-mcs: 
	rm -f $(mcs_topdir)/mcs/mcs.exe $(mcs_topdir)/class/lib/default/mscorlib.dll
	rm -f $(mcs_topdir)/class/lib/default/System.dll $(mcs_topdir)/class/lib/default/System.Xml.dll
	cd $(mcs_topdir)/jay && $(MAKE)
	cd $(mcs_topdir)/mcs && $(MAKE) PROFILE=default BOOTSTRAP_MCS="mcs -d:BOOTSTRAP_WITH_OLDLIB"
	cd $(mcs_topdir)/class/corlib && $(MAKE) PROFILE=default BOOTSTRAP_MCS="mcs -d:BOOTSTRAP_WITH_OLDLIB"
	cd $(mcs_topdir)/class/System && $(MAKE) PROFILE=default BOOTSTRAP_MCS="mcs -d:BOOTSTRAP_WITH_OLDLIB" MCS="mcs -d:BOOTSTRAP_WITH_OLDLIB"

# Build enough of 'mono' to run the stage1 mcs.exe
stage1-mono: $(CONFIG_HEADER)
	if test -z "$(libgc_dir)"; then :; else cd libgc && $(MAKE) ; fi
	cd mono && $(MAKE)

# (takes advantage of 'make -j': independent subtasks are separate dependencies)
stage1: stage1-mcs stage1-mono tmpinst-dir
	$(MAKE) tmpinst-runtime

# Use stage1 'mcs', 'mscorlib.dll' and 'System.dll' to build fuller versions
# FIXME: Mono.CSharp.Debugger is built only on Linux in the MCS tree.
#        At least the SymbolWriter stuff seems portable enough.  So either
#	 fix the MCS tree, or fix it here
stage2: stage1
	set -e ; \
	tmpinst=`cd _tmpinst && pwd` ; mcs_topdir=`cd $(mcs_topdir) && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH; export PATH ; \
	cd $$mcs_topdir/mcs && $(MAKE) PROFILE=default ; \
	cd $$mcs_topdir/class/corlib && $(MAKE) PROFILE=default ; \
	cd $$mcs_topdir/class/System && $(MAKE) PROFILE=default ; \
	cd $$mcs_topdir/class/Mono.CSharp.Debugger && $(MAKE) PROFILE=default
	$(MAKE) tmpinst-runtime

# Use stage2 binaries to build the whole tree
stage3: stage2
	tmpinst=`cd _tmpinst && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	cd $(mcs_topdir) && $(MAKE) all-profiles

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

tmpinst-runtime:
	tmpinst=`cd _tmpinst && pwd` && \
	mv -f $(mcs_topdir)/mcs/mcs.exe $$tmpinst/lib && \
	mv -f $(mcs_topdir)/class/lib/default/mscorlib.dll $(mcs_topdir)/class/lib/default/System.dll $$tmpinst/lib && \
	if test -f $(mcs_topdir)/class/lib/default/Mono.CSharp.Debugger.dll; then \
	  mv -f $(mcs_topdir)/class/lib/default/Mono.CSharp.Debugger.dll $$tmpinst/lib ; else : ; fi

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
	cp /usr/lib/libglib-2.0-0.dll @prefix@/lib
	cp /usr/lib/iconv.dll @prefix@/lib
	cp /usr/lib/libgmodule-2.0-0.dll @prefix@/lib
	cp /usr/lib/libgobject-2.0-0.dll @prefix@/lib
	cp /usr/lib/libgthread-2.0-0.dll @prefix@/lib
	cp /usr/lib/libintl-1.dll @prefix@/lib
	makensis /DMILESTONE=@VERSION@ /DSOURCE_INSTALL_DIR=`cygpath -a -w @prefix@`\\* monowiz.win32.nsi
