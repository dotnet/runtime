AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper --aot-path=$(mcs_topdir)/class/lib/build

MCS_NO_UNSAFE = $(TOOLS_RUNTIME) $(CSC) -debug:portable \
	-noconfig -nologo \
	-nowarn:0162 -nowarn:0168 -nowarn:0219 -nowarn:0414 -nowarn:0618 \
	-nowarn:0169 -nowarn:1690 -nowarn:0649 -nowarn:0612 -nowarn:3021 \
	-nowarn:0197 $(PROFILE_MCS_FLAGS)
MCS_NO_LIB = $(MCS_NO_UNSAFE) -unsafe

MCS = $(MCS_NO_LIB)


test_cflags = $(AM_CFLAGS) $(SGEN_DEFINES)
test_ldadd = libtestlib.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(LIBICONV)
if HOST_DARWIN
test_ldflags = -framework CoreFoundation -framework Foundation
endif

monodir=$(top_builddir)
sgen_libs = \
	$(monodir)/mono/metadata/libmonoruntimesgen.la	\
	$(monodir)/mono/sgen/libmonosgen.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV)

if !CROSS_COMPILE
if !HOST_WIN32
if SUPPORT_BOEHM
if SUPPORT_SGEN

noinst_LTLIBRARIES = libtestlib.la
libtestlib_la_SOURCES =
libtestlib_la_LIBADD = ../metadata/libmonoruntimesgen.la ../sgen/libmonosgen.la ../utils/libmonoutils.la

test_sgen_qsort_SOURCES = test-sgen-qsort.c
test_sgen_qsort_CFLAGS = $(test_cflags)
test_sgen_qsort_LDADD = $(test_ldadd)
test_sgen_qsort_LDFLAGS = $(test_ldflags)

test_memfuncs_SOURCES = test-memfuncs.c
test_memfuncs_CFLAGS = $(test_cflags)
test_memfuncs_LDADD = $(test_ldadd)
test_memfuncs_LDFLAGS = $(test_ldflags)

test_mono_linked_list_set_SOURCES = test-mono-linked-list-set.c
test_mono_linked_list_set_CFLAGS = $(test_cflags)
test_mono_linked_list_set_LDADD = $(test_ldadd)
test_mono_linked_list_set_LDFLAGS = $(test_ldflags)

test_conc_hashtable_SOURCES = test-conc-hashtable.c
test_conc_hashtable_CFLAGS = $(test_cflags)
test_conc_hashtable_LDADD = $(test_ldadd)
test_conc_hashtable_LDFLAGS = $(test_ldflags)

test_mono_handle_SOURCES = test-mono-handle.c
test_mono_handle_CFLAGS = $(test_cflags)
test_mono_handle_LDADD = $(test_ldadd)
test_mono_handle_LDFLAGS = $(test_ldflags)

test_mono_callspec_SOURCES = test-mono-callspec.c
test_mono_callspec_CFLAGS = $(AM_CFLAGS)
test_mono_callspec_LDADD = $(test_ldadd) ../mini/libmini.la $(sgen_libs)
test_mono_callspec_LDFLAGS = $(test_ldflags)
test_mono_callspec_DEPENDENCIES = callspec.exe

check_PROGRAMS = test-sgen-qsort test-memfuncs test-mono-linked-list-set test-conc-hashtable test-mono-handle	\
		 test-mono-callspec

TESTS = test-sgen-qsort test-memfuncs test-mono-linked-list-set test-conc-hashtable test-mono-handle \
	test-mono-callspec

AM_TESTS_ENVIRONMENT = export MONO_PATH=$(mcs_topdir)/class/lib/build;

.NOTPARALLEL:

check-local:
	if [ -e test-suite.log ]; then \
		if grep -q "# FAIL:  0\|tests passed" test-suite.log; then successbool=True && failures=0; else successbool=False && failures=1; fi; \
		echo "<?xml version='1.0' encoding='utf-8'?><test-results failures='$$failures' total='1' not-run='0' name='unit-tests.dummy' date='$$(date +%F)' time='$$(date +%T)'><test-suite name='MonoTests.unit-tests' success='$$successbool' time='0'><results><test-case name='MonoTests.unit-tests.100percentsuccess' executed='True' success='$$successbool' time='0'>" > TestResult-unit-tests.xml; \
		if [ $$failures -ne 0 ]; then echo "<failure><message>"'<![CDATA[' >> TestResult-unit-tests.xml && cat test-suite.log >> TestResult-unit-tests.xml && echo "]]></message><stack-trace></stack-trace></failure>" >> TestResult-unit-tests.xml; fi; \
		echo "</test-case></results></test-suite></test-results>" >> TestResult-unit-tests.xml; \
	fi;

clean-local:
	rm -f callspec.exe callspec.pdb

%.exe: %.cs
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -out:$@ $<

endif SUPPORT_SGEN
endif SUPPORT_BOEHM
endif !HOST_WIN32
endif !CROSS_COMPILE

