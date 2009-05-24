AUTOMAKE_OPTIONS = foreign
ACLOCAL_AMFLAGS = -I .

SUBDIRS = po $(libgc_dir) $(eglib_dir) mono $(ikvm_native_dir) support data runtime scripts man samples web msvc $(docs_dir)

# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = po libgc $(eglib_dir) mono ikvm-native support data runtime scripts man samples web tools msvc docs

EXTRA_DIST= nls.m4 po.m4 progtest.m4 mono-uninstalled.pc.in build-mingw32.sh LICENSE mkinstalldirs

DISTCHECK_CONFIGURE_FLAGS = EXTERNAL_MCS=false EXTERNAL_RUNTIME=false

# Distribute the 'mcs' tree too
dist-hook:
	test -d $(distdir)/mcs || mkdir $(distdir)/mcs
	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) PROFILE=net_1_1 distdir=$$d dist-recursive
	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) PROFILE=net_2_0 distdir=$$d dist-recursive

pkgconfigdir = $(libdir)/pkgconfig
noinst_DATA = mono-uninstalled.pc
DISTCLEANFILES= mono-uninstalled.pc

.PHONY: get-monolite-latest mcs-do-compiler-tests compiler-tests bootstrap-world

# building with monolite
mcslib = $(mcs_topdir)/class/lib
monolite = $(mcslib)/monolite
monolite_url = http://mono.ximian.com/daily/monolite-latest.tar.gz
get-monolite-latest:
	-rm -fr $(mcslib)/monolite-*
	-mkdir -p $(mcslib)
	test ! -d $(monolite) || test ! -d $(monolite).old || rm -fr $(monolite).old
	test ! -d $(monolite) || mv -f $(monolite) $(monolite).old
	cd $(mcslib) && { (wget -O- $(monolite_url) || curl $(monolite_url)) | gzip -d | tar xf - ; }
	cd $(mcslib) && mv -f monolite-* monolite

compiler-tests: build-test-mono-mcs-moon

compiler-tests-net_2_0:
	-rm -f $(mcs_topdir)/build/common/Consts.cs.save
	-mv -f $(mcs_topdir)/build/common/Consts.cs $(mcs_topdir)/build/common/Consts.cs.save
	cd $(mcs_topdir) && $(MAKE) PROFILE=net_2_0_bootstrap clean
	cd $(mcs_topdir) && $(MAKE) PROFILE=net_2_0 clean
	-mv -f $(mcs_topdir)/build/common/Consts.cs.save $(mcs_topdir)/build/common/Consts.cs
	$(MAKE) all
	$(MAKE) test_profiles=net_2_0 mcs-do-compiler-tests

bootstrap-world: compiler-tests
	$(MAKE) install

bootstrap-world-net_2_0: compiler-tests-net_2_0
	$(MAKE) install

# internal targets
.PHONY: build-test-mono-mcs-moon
build-test-mono-mcs-moon: do-build-moon-maybe
	$(MAKE) mcs-do-compiler-tests

.PHONY: do-build-mono-mcs
do-build-mono-mcs: mcs-do-clean
	$(MAKE) all

.PHONY: do-build-moon-maybe
do-build-moon-maybe: do-build-mono-mcs
	cd runtime && $(MAKE) moon-do-build

mcs-do-clean:
	cd runtime && $(MAKE) clean-local
	cd mono/tests && $(MAKE) clean
mcs-do-compiler-tests:
	cd runtime && $(MAKE) test_select='TEST_SUBDIRS="tests errors"' check-local
	cd mono/tests && $(MAKE) check

win32getdeps:
	wget http://www.go-mono.com/archive/pkgconfig-0.11-20020310.zip
	wget http://www.go-mono.com/archive/glib-2.0.4-20020703.zip 
	wget http://www.go-mono.com/archive/glib-dev-2.0.4-20020703.zip 
	wget http://www.go-mono.com/archive/libiconv-1.7.zip 
	wget http://www.go-mono.com/archive/libiconv-dev-1.7.zip 
	wget http://www.go-mono.com/archive/libintl-0.10.40-20020101.zip
	unzip -n -d / pkgconfig-0.11-20020310.zip
	unzip -n -d / glib-2.0.4-20020703.zip
	unzip -n -d / glib-dev-2.0.4-20020703.zip
	unzip -n -d / libiconv-1.7.zip
	unzip -n -d / libiconv-dev-1.7.zip
	unzip -n -d / libintl-0.10.40-20020101.zip

win32setup:
	makensis /DMILESTONE=$(VERSION) /DSOURCE_INSTALL_DIR=$(SOURCE_INSTALL_DIR) /DBUILDNUM=$(BUILDNUM) monowiz.win32.nsi

bootstrap: all
	@echo "*** 'make bootstrap' is obsolete.  Just run 'make' to perform a combined mono+mcs build"
	exit 1

patch-quiet:
	find mono -name Makefile -exec scripts/patch-quiet.sh {} \;
	find libgc -name Makefile -exec scripts/patch-quiet.sh {} \;

update-csproj:
	-rm msvc/scripts/order 
	-mkdir msvc/scripts/inputs
	(cd runtime; make V=1 extra_targets=csproj-local)

package-inputs:
	echo '<?xml version="1.0" encoding="utf-8"?>' > msvc/scripts/order.xml
	echo '<root>' >> msvc/scripts/order.xml
	for i in `cat msvc/scripts/order`; do \
		set `echo $$i | sed -e 's/:/ /' -e 's/.input//'`; \
		cat msvc/scripts/inputs/$$2.input | \
		(echo "    <project dir=\"$$1\" library=\"$$2\">"; \
		 read boot;   echo "      <boot>$$boot</boot>"; \
		 read mcs;    echo "      <mcs>$$mcs</mcs>"; \
		 read flags;  echo "      <flags>$$flags</flags>"; \
		 read output; echo "      <output>$$output</output>"; \
		 read built;  echo "      <built_sources>$$built</built_sources>"; \
		 read libou;  echo "      <library_output>$$libou</library_output>"; \
		 read resp;   echo "      <response>$$resp</response>"; \
		echo "    </project>") >> msvc/scripts/order.xml; \
	done
	echo "</root>" >> msvc/scripts/order.xml
