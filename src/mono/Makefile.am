AUTOMAKE_OPTIONS = foreign
ACLOCAL_AMFLAGS = -I .

SUBDIRS = $(libgc_dir) mono $(ikvm_jni_dir) docs data . runtime scripts man samples support web 

# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = libgc mono ikvm-jni docs data runtime scripts man samples support web tools

EXTRA_DIST= mono.pc.in mono.spec.in mint.pc.in

# Distribute the 'mcs' tree too
dist-hook:
	test -d $(distdir)/mcs || mkdir $(distdir)/mcs
	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) distdir=$$d dist-recursive

pkgconfigdir = $(libdir)/pkgconfig
if JIT_SUPPORTED
pkgconfig_DATA= mono.pc mint.pc
else
pkgconfig_DATA= mint.pc
endif
DISTCLEANFILES= mono.pc mint.pc

if USE_JIT
mono_runtime = mono/mini/mono
else
mono_runtime = mono/interpreter/mint
endif

all-local: mcs-do-full-build
	$(MAKE) populate-runtime-subdir

.PHONY: get-monolite-latest tmpinst-dir mcs-do-full-build mcs-do-clean-profiles mcs-do-run-test populate-runtime-subdir

tmpinst = runtime/_tmpinst

if PLATFORM_WIN32

populate_profiles = default:net_1_1

else

populate_profiles = default:net_1_1 net_2_0:net_2_0

endif

clean-local:
	cd $(mcs_topdir) && $(MAKE) PROFILES="basic net_1_1_bootstrap default net_2_0_bootstrap net_2_0" clean-profiles
	-rm -fr $(tmpinst)

check-local: mcs-do-run-test-profiles mono-do-testjit

mono-do-testjit:
	cd mono/tests && $(MAKE) test

# building with monolite

monolitedir = $(mcs_topdir)/class/lib/basic
get-monolite-latest: tmpinst-dir
	wget -O $(srcdir)/../monolite-latest.tar.gz http://www.go-mono.com/daily/monolite-latest.tar.gz
	-rm -f $(monolitedir)/mcs.exe $(monolitedir)/*.dll
	-rm -fr $(tmpinst)/monolite-*
	srcdir=`cd $(srcdir) && pwd` && cd $(tmpinst) && ( gzip -d -c $$srcdir/../monolite-latest.tar.gz | tar xf - )
	mv -f $(tmpinst)/monolite-*/mcs.exe $(tmpinst)/monolite-*/*.dll $(monolitedir)
	rm -fr $(tmpinst)/monolite-*

compiler-tests:
	$(MAKE) mcs-do-clean-profiles
	$(MAKE)
	$(MAKE) mcs-do-compiler-tests

bootstrap-world: compiler-tests
	$(MAKE) install

# internal targets

mcs-do-full-build: tmpinst-dir
	tmpinst=`cd $(tmpinst) && pwd` ; \
	cd $(mcs_topdir) && $(MAKE) RUNTIME=$$tmpinst/bin/mono all-profiles

populate-runtime-subdir:
	test -n '$(populate_profiles)'
	@srcdir=`cd $(srcdir) && pwd`; tmpinst=`pwd`/$(tmpinst) ; \
	for profile in $(populate_profiles); do \
	  ( set fnord `echo $$profile | sed 's,:, ,g'` && \
	    echo "cd $(mcs_topdir) && $(MAKE) RUNTIME=$$tmpinst/bin/mono PROGRAM_INSTALL_DIR=$$srcdir/runtime PROFILE=$$2 LIBRARY_INSTALL_DIR=$$srcdir/runtime/$$3 prefix=$$tmpinst/prefix install" && \
	    cd $(mcs_topdir) && \
	    $(MAKE) RUNTIME=$$tmpinst/bin/mono PROGRAM_INSTALL_DIR=$$srcdir/runtime PROFILE=$$2 LIBRARY_INSTALL_DIR=$$srcdir/runtime/$$3 prefix=$$tmpinst/prefix install ) || exit 1 ; \
	done
	find $(tmpinst)/prefix -type f -name '*jay*' -exec rm -f '{}' ';'
	for i in `find $(tmpinst)/prefix -type d -name '*jay*' -print`; do rm -f $$i/*; done
	test -z "`find $(tmpinst)/prefix -type f -print`"
	rm -rf $(tmpinst)/prefix

tmpinst-dir: 
	mkdir -p $(tmpinst) $(tmpinst)/bin $(tmpinst)/etc/mono/1.0 $(tmpinst)/etc/mono/2.0
	$(MAKE) tmpinst-dir-contents

tmpinst-dir-contents: $(tmpinst)/bin/mono $(tmpinst)/etc/mono/1.0/machine.config $(tmpinst)/etc/mono/2.0/machine.config

if PLATFORM_WIN32

$(tmpinst)/bin/mono: $(srcdir)/Makefile.am
	echo '#! /bin/sh' > $@
	echo 'builddir="'`pwd`'"; win_builddir="'`cygpath -w -a .`'"' >> $@
	echo 'tmpinst="$$win_builddir\\$(tmpinst)"' >> $@
	echo 'MONO_CFG_DIR="$$tmpinst\\etc"' >> $@
	echo 'export MONO_CFG_DIR' >> $@
	echo 'exec "$$builddir/libtool" --mode=execute "$$builddir/$(mono_runtime).exe" "$$@"' >> $@
	chmod +x $@

else

$(tmpinst)/bin/mono: $(srcdir)/Makefile.am
	echo '#! /bin/sh' > $@
	echo 'builddir="'`pwd`'"; tmpinst=$$builddir/$(tmpinst)' >> $@
	echo 'MONO_CFG_DIR=$$tmpinst/etc' >> $@
	echo 'export MONO_CFG_DIR' >> $@
	echo 'exec "$$builddir/libtool" --mode=execute "$$builddir/$(mono_runtime)" --config "$$builddir/data/config" "$$@"' >> $@
	chmod +x $@

endif

$(tmpinst)/bin/pedump: $(srcdir)/Makefile.am
	(b=`pwd`; echo '#! /bin/sh'; echo 'exec "'"$$b/libtool"'" --mode=execute "'"$$b/mono/metadata/pedump"'" "$$@"') > $@
	chmod +x $@

TEST_SUPPORT_FILES = $(tmpinst)/bin/mcs $(tmpinst)/bin/mbas $(tmpinst)/bin/ilasm $(tmpinst)/bin/gmcs
mcs-do-run-test-profiles: tmpinst-dir
	$(MAKE) $(TEST_SUPPORT_FILES)
	tmpinst=`cd $(tmpinst) && pwd` ; ret=: ; \
	PATH=$$tmpinst/bin:$$PATH ; export PATH ; \
	( cd $(mcs_topdir) && $(MAKE) RUNTIME=$$tmpinst/bin/mono run-test-profiles ) || ret=false ; \
	rm -f $(TEST_SUPPORT_FILES) ; $$ret

mcs-do-compiler-tests:
	$(MAKE) TEST_SUBDIRS="tests errors" mcs-do-run-test-profiles

# Used only by 'check-local' -- so, can safely use runtime/.
$(TEST_SUPPORT_FILES): $(srcdir)/Makefile.am
	t=`cd $(tmpinst) && pwd`; r=`cd $(srcdir)/runtime && pwd`; \
	( echo '#! /bin/sh'; echo 'exec "'"$$t/bin/mono"'" "'"$$r/$(@F).exe"'" "$$@"' ) > $@
	chmod +x $@

$(tmpinst)/etc/mono/1.0/machine.config: $(srcdir)/data/net_1_1/machine.config
	rm -f $@
	srcdir=`cd $(srcdir) && pwd`; cd $(tmpinst)/etc/mono/1.0 && $(LN_S) $$srcdir/data/net_1_1/machine.config machine.config

$(tmpinst)/etc/mono/2.0/machine.config: $(srcdir)/data/net_2_0/machine.config
	rm -f $@
	srcdir=`cd $(srcdir) && pwd`; cd $(tmpinst)/etc/mono/2.0 && $(LN_S) $$srcdir/data/net_2_0/machine.config machine.config

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
