AUTOMAKE_OPTIONS = foreign
ACLOCAL_AMFLAGS = -I .

SUBDIRS = $(libgc_dir) mono $(ikvm_jni_dir) docs data runtime scripts man samples support web 

# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = libgc mono ikvm-jni docs data runtime scripts man samples support web tools

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
.PHONY: populate-runtime-subdir

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
	$(mcs_topdir)/class/lib/default/Mono.CompilerServices.SymbolWriter.dll

tmpinst = _tmpinst

if PLATFORM_WIN32

populate_profiles = default:net_1_1

# bootstraps using the .NET C# compiler
bootstrap: tmpinst-dir mono-do-runtime-only
	$(MAKE) fasterstrap

else

populate_profiles = default:net_1_1 net_2_0:net_2_0

bootstrap: tmpinst-mcs-basic-build mono-do-runtime-only
	$(MAKE) tmpinst-mcs-short-build
	$(MAKE) fasterstrap

endif

faststrap: tmpinst-mcs-short-build
	$(MAKE) mono-do-runtime-only
	$(MAKE) fasterstrap

fasterstrap: tmpinst-mcs-full-build
	$(MAKE) populate-runtime-subdir
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
	mv -f $(MCS_BASIC_FILES) $(tmpinst)/lib

tmpinst-mcs-short-build: mcs-do-short-build tmpinst-dir
	mv -f $(MCS_FILES) $(tmpinst)/lib

tmpinst-mcs-full-build: mcs-do-full-build tmpinst-dir
	cp -f $(MCS_FILES) $(tmpinst)/lib

tmpinst-monolite: tmpinst-dir
	-rm -f $(tmpinst)/lib/mcs.exe $(tmpinst)/lib/*.dll
	-rm -fr $(tmpinst)/monolite-*
	srcdir=`cd $(srcdir) && pwd` && cd $(tmpinst) && ( gzip -d -c $$srcdir/../monolite-latest.tar.gz | tar xf - )
	mv -f $(tmpinst)/monolite-*/mcs.exe $(tmpinst)/monolite-*/*.dll $(tmpinst)/lib
	rm -fr $(tmpinst)/monolite-*

# Build enough of 'mono' to run the stage1 mcs.exe
mono-do-runtime-only: $(CONFIG_HEADER)
	for dir in $(libgc_dir) mono data; do \
	  (cd $$dir && $(MAKE)) || exit 1 ; \
	done

mcs-do-basic-build:
	rm -f $(MCS_FILES)
	for dir in $(MCS_BASIC_DIRS); do \
	  (cd $(mcs_topdir)/$$dir && $(MAKE) PROFILE=default NO_SIGN_ASSEMBLY=yes USE_BOOT_COMPILE=yes BOOTSTRAP_MCS="mcs -d:BOOTSTRAP_WITH_OLDLIB") || exit 1 ; \
	done

mcs-do-short-build:
	tmpinst=`cd $(tmpinst) && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH; export PATH ; \
	for dir in $(MCS_DIRS); do \
	  (cd $(mcs_topdir)/$$dir && $(MAKE) PROFILE=default NO_SIGN_ASSEMBLY=yes) || exit 1 ; \
	done

mcs-do-full-build:
	tmpinst=`cd $(tmpinst) && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	cd $(mcs_topdir) && $(MAKE) all-profiles

mcs-do-clean-profiles:
	cd $(mcs_topdir) && $(MAKE) clean-profiles

mcs-do-run-test: tmpinst-dir $(tmpinst)/bin/mbas
	tmpinst=`cd $(tmpinst) && pwd` ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	cd $(mcs_topdir) && $(MAKE) PROFILE=default run-test

populate-runtime-subdir:
	test -n '$(populate_profiles)'
	@srcdir=`cd $(srcdir) && pwd`; tmpinst=`pwd`/$(tmpinst) ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	for profile in $(populate_profiles); do \
	  ( set fnord `echo $$profile | sed 's,:, ,g'` && \
	    echo "cd $(mcs_topdir) && $(MAKE) PROGRAM_INSTALL_DIR=$$srcdir/runtime PROFILE=$$2 LIBRARY_INSTALL_DIR=$$srcdir/runtime/$$3 prefix=$$tmpinst/prefix install" && \
	    cd $(mcs_topdir) && \
	    $(MAKE) PROGRAM_INSTALL_DIR=$$srcdir/runtime PROFILE=$$2 LIBRARY_INSTALL_DIR=$$srcdir/runtime/$$3 prefix=$$tmpinst/prefix install ) || exit 1 ; \
	done
	find $(tmpinst)/prefix -type f -path '*/jay*' -exec rm -f '{}' ';'
	test -z "`find $(tmpinst)/prefix -type f -print`"
	rm -rf $(tmpinst)/prefix

tmpinst-dir: 
	mkdir -p $(tmpinst) $(tmpinst)/bin $(tmpinst)/lib $(tmpinst)/etc/mono/1.0
	$(MAKE) tmpinst-dir-contents

tmpinst-dir-contents: $(tmpinst)/bin/mono $(tmpinst)/bin/mcs $(tmpinst)/etc/mono/1.0/machine.config

if PLATFORM_WIN32

$(tmpinst)/bin/mono: $(srcdir)/Makefile.am
	echo '#! /bin/sh' > $@
	echo 'builddir="'`pwd`'"; win_builddir="'`cygpath -w -a .`'"' >> $@
	echo 'tmpinst="$$win_builddir\\$(tmpinst)"' >> $@
	echo 'if test -z "$$MONO_PATH" ; then MONO_PATH="$$tmpinst\\lib"; else MONO_PATH="$${MONO_PATH};$$tmpinst\\lib" ; fi' >> $@
	echo 'MONO_CFG_DIR="$$tmpinst\\etc"' >> $@
	echo 'export MONO_PATH MONO_CFG_DIR' >> $@
	echo 'exec "$$builddir/libtool" --mode=execute "$$builddir/$(mono_runtime).exe" "$$@"' >> $@
	chmod +x $@

else

$(tmpinst)/bin/mono: $(srcdir)/Makefile.am
	echo '#! /bin/sh' > $@
	echo 'builddir="'`pwd`'"; tmpinst=$$builddir/$(tmpinst)' >> $@
	echo 'if test -z "$$MONO_PATH" ; then MONO_PATH=$$tmpinst/lib; else MONO_PATH=$${MONO_PATH}:$$tmpinst/lib ; fi' >> $@
	echo 'MONO_CFG_DIR=$$tmpinst/etc' >> $@
	echo 'export MONO_PATH MONO_CFG_DIR' >> $@
	echo 'exec "$$builddir/libtool" --mode=execute "$$builddir/$(mono_runtime)" --config "$$builddir/data/config" "$$@"' >> $@
	chmod +x $@

endif

$(tmpinst)/bin/mcs: $(srcdir)/Makefile.am
	(t=`cd $(tmpinst) && pwd`; echo '#! /bin/sh'; echo 'exec "'"$$t/bin/mono"'" "'"$$t/lib/mcs.exe"'" "$$@"') > $@
	chmod +x $@

# Used only by 'bootstrap-check' -- so, can safely use runtime/mbas.exe.
$(tmpinst)/bin/mbas: $(srcdir)/Makefile.am
	t=`cd $(tmpinst) && pwd`; r=`cd $(srcdir)/runtime && pwd`; \
	( echo '#! /bin/sh'; echo 'exec "'"$$t/bin/mono"'" "'"$$r/mbas.exe"'" "$$@"' ) > $@
	chmod +x $@

$(tmpinst)/etc/mono/1.0/machine.config: $(srcdir)/data/net_1_1/machine.config
	rm -f $@
	srcdir=`cd $(srcdir) && pwd`; cd $(tmpinst)/etc/mono/1.0 && $(LN_S) $$srcdir/data/net_1_1/machine.config machine.config

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
