ACLOCAL_AMFLAGS = -I m4

AM_CFLAGS = $(WERROR_CFLAGS)

SUBDIRS = @MONO_SUBDIRS@
noinst_SUBDIRS = @MONO_NOINST_SUBDIRS@
DIST_SUBDIRS = $(SUBDIRS) m4 netcore

if !ENABLE_NETCORE

all: $(update_submodules)

update_submodules:
	@cd $(srcdir) && scripts/update_submodules.sh

.PHONY: update_submodules

EXTRA_DIST= \
	README.md 		\
	LICENSE 		\
	autogen.sh 		\
	mkinstalldirs 		\
	mono-uninstalled.pc.in 	\
	winconfig.h 		\
	code_of_conduct.md 	\
	external		\
	netcore/Makefile	\
	mcs/class/referencesource

DISTCHECK_CONFIGURE_FLAGS = EXTERNAL_RUNTIME=false

# Distribute the 'mcs' tree too
GIT_DIR ?= $(srcdir)/.git
dist-hook:
	test -d $(distdir)/mcs || mkdir $(distdir)/mcs
	d=`cd $(distdir)/mcs && pwd`; cd $(mcs_topdir) && $(MAKE) distdir=$$d dist-recursive
	rm -rf `find $(top_distdir)/external -path '*\.git'`
	rm -rf `find $(top_distdir)/external -path '*\.libs'`
	rm -rf `find $(top_distdir)/external -path '*\.deps'`
	rm -f `find $(top_distdir)/external -path '*\.o'`
	rm -f `find $(top_distdir)/external -path '*\.so'`
	rm -f `find $(top_distdir)/external -path '*\.lo'`
	rm -f `find $(top_distdir)/external -path '*\.Plo'`
	rm -f `find $(top_distdir)/external -name '\.dirstamp'`
	rm -f `find $(top_distdir)/external -path '*\.exe' -not -path '*/roslyn-binaries/*'`
	rm -f `find $(top_distdir)/external -path '*\.dll' -not -path '*/binary-reference-assemblies/*' -not -path '*/roslyn-binaries/*' -not -path '*/helix-binaries/*'`
	rm -rf "$(top_distdir)/external/linker/test"
	rm -rf "$(top_distdir)/external/llvm-project/lldb/test"
	rm -rf "$(top_distdir)/external/llvm-project/libcxx/test"
	rm -rf "$(top_distdir)/external/llvm-project/clang/test"

pkgconfigdir = $(libdir)/pkgconfig
noinst_DATA = mono-uninstalled.pc
DISTCLEANFILES= mono-uninstalled.pc

# building with monolite
.PHONY: get-monolite-latest 
get-monolite-latest:
	$(MAKE) -C $(mcs_topdir)/class get-monolite-latest

if BITCODE
BITCODE_CHECK=yes
endif

if DEFAULT_TESTS
CI_TEST_SCRIPT=$(srcdir)/scripts/ci/run-test-default.sh
else
CI_TEST_SCRIPT=$(srcdir)/scripts/ci/run-test-$(TEST_PROFILE).sh
endif

.PHONY: check-ci
check-ci:
	MONO_LLVMONLY=$(BITCODE_CHECK) $(CI_TEST_SCRIPT)

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

install:
	for mydir in $(filter-out $(noinst_SUBDIRS),$(SUBDIRS)); do \
	  (cd $${mydir} && ${MAKE} install) \
	done

update-csproj:
	-rm msvc/scripts/order 
	-rm msvc/scripts/order.xml
	-rm -rf msvc/scripts/inputs
	-mkdir msvc/scripts/inputs
	(cd runtime; $(MAKE) V=1 extra_targets=csproj-local)

package-inputs:
	echo '<?xml version="1.0" encoding="utf-8"?>' > msvc/scripts/order.xml
	echo '<root>' >> msvc/scripts/order.xml
	for i in `cat msvc/scripts/order`; do \
		set `echo $$i | sed -e 's/:/ /' -e 's/.input//'`; \
		cat msvc/scripts/inputs/$$2.input | sed -e 's/\\\\/\\/g' -e 's/\\/\\\\/g' | \
		(echo "    <project dir=\"$$1\" library=\"$$2\">"; \
		 read boot;   echo "      <boot>$$boot</boot>"; \
		 read flags;  echo "      <flags>$$flags</flags>"; \
		 read sources;echo "      <sources>$$sources</sources>"; \
		 read output; echo "      <output>$$output</output>"; \
		 read built;  echo "      <built_sources>`echo $$built | sed 's/\\\/\\\\/g'`</built_sources>"; \
		 read libou;  echo "      <library_output>$$libou</library_output>"; \
		 read fx_ver; echo "      <fx_version>$$fx_ver</fx_version>"; \
		 read profile; echo "      <profile>$$profile</profile>"; \
		 read resxt;  echo "      <resources>$$resxt</resources>"; \
		 read resp;   echo "      <response>$$resp</response>"; \
		 echo "    </project>") >> msvc/scripts/order.xml; \
	done
	echo "</root>" >> msvc/scripts/order.xml

# Update llvm version in configure.ac to the output of $LLVM_DIR/bin/llvm-config --version
update-llvm-version:
	if test "x$$LLVM_DIR" = "x"; then echo "Set the make variable LLVM_DIR to the directory containing the LLVM installation."; exit 1; fi
	REV=`$(LLVM_DIR)/bin/llvm-config --version` && sed -e "s,expected_llvm_version=.*,expected_llvm_version=\"$$REV\"," < configure.ac > tmp && mv tmp configure.ac && echo "Version set to $$REV."


update-solution-files:
	cd msvc/scripts && $(MAKE) genproj.exe || exit $$?;
	$(MAKE) update-csproj
	$(MAKE) package-inputs
	(cd msvc/scripts; mono --debug genproj.exe $(GENPROJ_ARGS))

update-solution-files-with-tests:
	$(MAKE) "GENPROJ_ARGS=2012 true true" update-solution-files

endif
