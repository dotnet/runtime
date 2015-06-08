AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

test_cflags = $(AM_CFLAGS) $(SGEN_DEFINES)
test_ldadd = libtestlib.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(LIBICONV)
if PLATFORM_DARWIN
test_ldflags = -framework CoreFoundation -framework Foundation
endif


if !CROSS_COMPILE
if !HOST_WIN32
if SUPPORT_BOEHM

noinst_LTLIBRARIES = libtestlib.la
libtestlib_la_SOURCES =
libtestlib_la_LIBADD = ../metadata/libmonoruntimesgen.la ../sgen/libmonosgen.la ../utils/libmonoutils.la ../io-layer/libwapi.la 

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

noinst_PROGRAMS = test-sgen-qsort test-memfuncs test-mono-linked-list-set test-conc-hashtable

TESTS = test-sgen-qsort test-memfuncs test-mono-linked-list-set test-conc-hashtable

endif SUPPORT_BOEHM
endif !HOST_WIN32
endif !CROSS_COMPILE

