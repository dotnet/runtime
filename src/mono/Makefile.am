ACLOCAL_AMFLAGS = -I m4

MONOTOUCH_SUBDIRS = $(libgc_dir) eglib/src mono

if CROSS_COMPILING
SUBDIRS = po $(libgc_dir) eglib mono $(ikvm_native_dir) data runtime scripts man samples msvc $(docs_dir)
# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = m4 po libgc eglib mono ikvm-native data runtime scripts man samples tools msvc docs
else
if ONLY_MONOTOUCH
SUBDIRS = $(MONOTOUCH_SUBDIRS) runtime
else
SUBDIRS = po $(libgc_dir) eglib mono $(ikvm_native_dir) support data runtime scripts man samples msvc $(docs_dir)
# Keep in sync with SUBDIRS
## 'tools' is not normally built
DIST_SUBDIRS = m4 po libgc eglib mono ikvm-native support data runtime scripts man samples tools msvc docs
endif
endif

all: update_submodules

SUBMODULE_ERROR='Could not recursively update all git submodules. You may experience compilation problems if some submodules are out of date'
update_submodules:
	@$(srcdir)/scripts/update_submodules

.PHONY: update_submodules

EXTRA_DIST= \
	    LICENSE \
	    autogen.sh \
	    build-mingw32.sh \
	    mkinstalldirs \
	    mono-uninstalled.pc.in \
	    winconfig.h \
	    mono-core.spec \
	    external

DISTCHECK_CONFIGURE_FLAGS = EXTERNAL_MCS=false EXTERNAL_RUNTIME=false

# Distribute the 'mcs' tree too
GIT_DIR ?= $(srcdir)/.git
dist-hook:
	test -d $(distdir)/mcs || mkdir $(distdir)/mcs
	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) distdir=$$d dist-recursive
	rm -rf `find $(top_distdir)/external -path '*\.git' -and -type d`
# Disable this for now because it is very slow and causes wrench to timeout:
#	test ! -d $(GIT_DIR) || ./scripts/commits-to-changelog.py --root=$(distdir) last-commit-with-compulsory-changelog-entry

pkgconfigdir = $(libdir)/pkgconfig
noinst_DATA = mono-uninstalled.pc
DISTCLEANFILES= mono-uninstalled.pc

# building with monolite
mcslib = $(mcs_topdir)/class/lib
monolite = $(mcslib)/monolite
mono_corlib_version = $(shell sed -n "s/\#define MONO_CORLIB_VERSION //p" $(srcdir)/mono/metadata/appdomain.c)
monolite_url = http://storage.bos.xamarin.com/mono-dist-master/latest/monolite-$(mono_corlib_version)-latest.tar.gz
.PHONY: get-monolite-latest 
get-monolite-latest:
	-rm -fr $(mcslib)/monolite-*
	-mkdir -p $(mcslib)
	test ! -d $(monolite) || test ! -d $(monolite).old || rm -fr $(monolite).old
	test ! -d $(monolite) || mv -f $(monolite) $(monolite).old
	cd $(mcslib) && { (wget -O- $(monolite_url) || curl $(monolite_url)) | gzip -d | tar xf - ; }
	cd $(mcslib) && mv -f monolite-* monolite

.PHONY: validate do-build-mono-mcs mcs-do-clean mcs-do-tests
validate: do-build-mono-mcs
	$(MAKE) mcs-do-tests
do-build-mono-mcs: mcs-do-clean
	$(MAKE) all
mcs-do-clean:
	cd runtime && $(MAKE) clean-local
	cd mono/tests && $(MAKE) clean
mcs-do-tests:
	cd runtime && $(MAKE) check-local
	cd mono/tests && $(MAKE) check

.PHONY: compiler-tests mcs-do-compiler-tests
compiler-tests:
	$(MAKE) test_select='TEST_SUBDIRS="tests errors"' validate
mcs-do-compiler-tests:
	$(MAKE) test_select='TEST_SUBDIRS="tests errors"' mcs-do-tests

.PHONY: bootstrap-world
bootstrap-world: compiler-tests
	$(MAKE) install

if INSTALL_MONOTOUCH
monotouch-do-build: config.h
	@list='$(MONOTOUCH_SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmono ) target="monotouch-do-build";; \
		* ) target="all";; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
        done;
	(cd runtime && $(MAKE) $(AM_MAKEFLAGS) monotouch-do-build)

monotouch-do-clean:
	@list='$(MONOTOUCH_SUBDIRS)'; for subdir in $$list; do \
	  case "x$$subdir" in \
		xmono ) target="monotouch-do-clean";; \
		* ) target="clean";; \
	  esac; \
	  echo "Making $$target in $$subdir"; \
	  (cd $$subdir && $(MAKE) $(AM_MAKEFLAGS) $$target); \
        done;
	(cd runtime && $(MAKE) $(AM_MAKEFLAGS) monotouch-do-clean)

endif

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

update-csproj:
	-rm msvc/scripts/order 
	-rm msvc/scripts/order.xml
	-rm -rf msvc/scripts/inputs
	-mkdir msvc/scripts/inputs
	(cd runtime; make V=1 extra_targets=csproj-local)

package-inputs:
	echo '<?xml version="1.0" encoding="utf-8"?>' > msvc/scripts/order.xml
	echo '<root>' >> msvc/scripts/order.xml
	for i in `cat msvc/scripts/order`; do \
		set `echo $$i | sed -e 's/:/ /' -e 's/.input//'`; \
		cat msvc/scripts/inputs/$$2.input | sed -e 's/\\\\/\\/g' -e 's/\\/\\\\/g' | \
		(echo "    <project dir=\"$$1\" library=\"$$2\">"; \
		 read boot;   echo "      <boot>$$boot</boot>"; \
		 read flags;  echo "      <flags>$$flags</flags>"; \
		 read output; echo "      <output>$$output</output>"; \
		 read built;  echo "      <built_sources>`echo $$built | sed 's/\\\/\\\\/g'`</built_sources>"; \
		 read libou;  echo "      <library_output>$$libou</library_output>"; \
		 read fx_ver; echo "      <fx_version>$$fx_ver</fx_version>"; \
		 read profile; echo "      <profile>$$profile</profile>"; \
		 read resp;   echo "      <response>$$resp</response>"; \
		 echo "    </project>") >> msvc/scripts/order.xml; \
	done
	echo "</root>" >> msvc/scripts/order.xml

