AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

if !CROSS_COMPILING
if !HOST_WIN32
if SUPPORT_BOEHM

test_sgen_qsort_SOURCES = test-sgen-qsort.c
test_sgen_qsort_CFLAGS = $(SGEN_DEFINES)
test_sgen_qsort_LDADD = ../metadata/libmonoruntimesgen.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(LIBICONV)
if PLATFORM_DARWIN
test_sgen_qsort_LDFLAGS=-framework CoreFoundation -framework Foundation
endif

test_gc_memfuncs_SOURCES = test-gc-memfuncs.c
test_gc_memfuncs_CFLAGS = $(SGEN_DEFINES)
test_gc_memfuncs_LDADD = ../metadata/libmonoruntimesgen.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(LIBICONV)
if PLATFORM_DARWIN
test_gc_memfuncs_LDFLAGS=-framework CoreFoundation -framework Foundation
endif

test_mono_linked_list_set_SOURCES = test-mono-linked-list-set.c
test_mono_linked_list_set_CFLAGS = $(AM_CFLAGS)
test_mono_linked_list_set_LDADD = ../metadata/libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(LIBICONV) $(PEDUMP_DTRACE_OBJECT)

if PLATFORM_DARWIN
test_mono_linked_list_set_LDFLAGS=-framework CoreFoundation -framework Foundation
endif

noinst_PROGRAMS = test-sgen-qsort test-gc-memfuncs test-mono-linked-list-set

TESTS = test-sgen-qsort test-gc-memfuncs test-mono-linked-list-set

endif SUPPORT_BOEHM
endif !HOST_WIN32
endif !CROSS_COMPILING
