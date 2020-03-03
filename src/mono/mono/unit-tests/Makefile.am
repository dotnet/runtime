MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono -I$(top_srcdir)/samples $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper --aot-path=$(mcs_topdir)/class/lib/build

MCS_NO_UNSAFE = $(TOOLS_RUNTIME) $(CSC) -debug:portable \
	-noconfig -nologo \
	-nowarn:0162 -nowarn:0168 -nowarn:0219 -nowarn:0414 -nowarn:0618 \
	-nowarn:0169 -nowarn:1690 -nowarn:0649 -nowarn:0612 -nowarn:3021 \
	-nowarn:0197 $(PROFILE_MCS_FLAGS)
MCS_NO_LIB = $(MCS_NO_UNSAFE) -unsafe

MCS = $(MCS_NO_LIB)

LLVMMONOF=$(LLVM_LIBS) $(LLVM_LDFLAGS)

glib_libs = $(top_builddir)/mono/eglib/libeglib.la

test_cflags = $(AM_CFLAGS) $(SGEN_DEFINES)
test_ldadd = libtestlib.la \
	$(LIBGC_LIBS) $(glib_libs) $(LLVMMONOF)

if HOST_DARWIN
test_ldflags = -framework CoreFoundation -framework Foundation
endif

monodir=$(top_builddir)
sgen_libs = \
	$(monodir)/mono/metadata/libmonoruntimesgen.la	\
	$(monodir)/mono/sgen/libmonosgen.la	\
	$(monodir)/mono/utils/libmonoutils.la \
	$(glib_libs)

mini_libs = \
	$(monodir)/mono/mini/libmini.la

if !DISABLE_INTERPRETER
mini_libs += $(monodir)/mono/mini/libmono-ee-interp.la
endif

if !DISABLE_DEBUGGER_AGENT
mini_libs += $(monodir)/mono/mini/libmono-dbg.la
endif

CFLAGS = $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)

if !CROSS_COMPILE
if !HOST_WIN32

noinst_LTLIBRARIES = libtestlib.la
libtestlib_la_CFLAGS = @CXX_ADD_CFLAGS@
libtestlib_la_SOURCES = \
	test-sgen-qsort.c \
	test-memfuncs.c \
	test-mono-linked-list-set.c \
	test-conc-hashtable.c \
	test-mono-handle.c \
	test-mono-callspec.c \
	test-mono-string.c \
	test-mono-embed.c \
	test-embed-invoke.c 
libtestlib_la_LIBADD = ../metadata/libmonoruntimesgen.la ../sgen/libmonosgen.la ../utils/libmonoutils.la

test_sgen_qsort_SOURCES = main.c
test_sgen_qsort_CFLAGS = $(test_cflags) -DMAIN=test_sgen_qsort_main
test_sgen_qsort_LDADD = $(test_ldadd) libtestlib_la-test-sgen-qsort.lo
test_sgen_qsort_LDFLAGS = $(test_ldflags)

test_memfuncs_SOURCES = main.c
test_memfuncs_CFLAGS = $(test_cflags) -DMAIN=test_memfuncs_main
test_memfuncs_LDADD = $(test_ldadd) libtestlib_la-test-memfuncs.lo
test_memfuncs_LDFLAGS = $(test_ldflags)

test_mono_linked_list_set_SOURCES = main.c
test_mono_linked_list_set_CFLAGS = $(test_cflags) -DMAIN=test_mono_linked_list_set_main
test_mono_linked_list_set_LDADD = $(test_ldadd) libtestlib_la-test-mono-linked-list-set.lo
test_mono_linked_list_set_LDFLAGS = $(test_ldflags)

test_conc_hashtable_SOURCES = main.c
test_conc_hashtable_CFLAGS = $(test_cflags) -DMAIN=test_conc_hashtable_main
test_conc_hashtable_LDADD = $(test_ldadd) libtestlib_la-test-conc-hashtable.lo
test_conc_hashtable_LDFLAGS = $(test_ldflags)

test_mono_handle_SOURCES = main.c
test_mono_handle_CFLAGS = $(test_cflags) -DMAIN=test_mono_handle_main
test_mono_handle_LDADD = $(test_ldadd) libtestlib_la-test-mono-handle.lo
test_mono_handle_LDFLAGS = $(test_ldflags)

test_mono_callspec_SOURCES = main.c
test_mono_callspec_CFLAGS = $(AM_CFLAGS) -DMAIN=test_mono_callspec_main
test_mono_callspec_LDADD = $(test_ldadd) $(mini_libs) $(sgen_libs) libtestlib_la-test-mono-callspec.lo
test_mono_callspec_LDFLAGS = $(test_ldflags)
test_mono_callspec_DEPENDENCIES = callspec.exe

test_mono_string_SOURCES = main.c
test_mono_string_CFLAGS = $(test_cflags) -DMAIN=test_mono_string_main
test_mono_string_LDADD = $(test_ldadd) $(mini_libs) $(sgen_libs) libtestlib_la-test-mono-string.lo
test_mono_string_LDFLAGS = $(test_ldflags)

test_mono_embed_SOURCES = main.c
test_mono_embed_CFLAGS = $(test_cflags) -DMAIN=test_mono_embed_main
test_mono_embed_LDADD = $(test_ldadd) $(mini_libs) $(sgen_libs) libtestlib_la-test-mono-embed.lo
test_mono_embed_LDFLAGS = $(test_ldflags)
test_mono_embed_DEPENDENCIES = test-embed.exe

test_path_SOURCES = test-path.c
test_path_CFLAGS = $(test-cflags)
test_path_LDADD = $(test_ldadd) $(mini_libs) $(sgen_libs)
test_path_LDFLAGS = $(test_ldflags)

test_embed_invoke_SOURCES = main.c
test_embed_invoke_CFLAGS = $(test_cflags) -DMAIN=test_mono_embed_invoke_main
test_embed_invoke_LDADD = $(test_ldadd) $(mini_libs) $(sgen_libs) libtestlib_la-test-embed-invoke.lo
test_embed_invoke_LDFLAGS = $(test_ldflags)
test_embed_invoke_DEPENDENCIES = test-embed-invoke-cs.exe


check_PROGRAMS = test-sgen-qsort test-memfuncs test-mono-linked-list-set test-conc-hashtable test-mono-handle	\
		 test-path \
		 test-mono-callspec test-mono-string test-mono-embed test-embed-invoke

TESTS = test-sgen-qsort test-memfuncs test-mono-linked-list-set test-conc-hashtable test-mono-handle \
	test-path \
	test-mono-callspec test-mono-string test-mono-embed test-embed-invoke

AM_TESTS_ENVIRONMENT = export MONO_PATH=$(mcs_topdir)/class/lib/build;

test-local: $(check_PROGRAMS)

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
	rm -f test-embed.exe test-embed.pdb
	rm -f test-embed-invoke-cs.exe test-embed-invoke-cs.pdb

%.exe: %.cs
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -out:$@ $<

test-embed.exe: $(top_srcdir)/samples/embed/test.cs
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -out:$@ $<

test-embed-invoke-cs.exe: $(top_srcdir)/samples/embed/invoke.cs
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll  -out:$@ $<

endif !HOST_WIN32
endif !CROSS_COMPILE
