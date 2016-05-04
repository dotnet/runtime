if HAVE_ZLIB
Z_LIBS= -lz
else
Z_LIBS=
endif

AM_CPPFLAGS = \
	-DSUPPRESSION_DIR=\""$(datadir)/mono-$(API_VER)/mono/profiler"\"        \
	-I$(top_srcdir) 	\
	$(GLIB_CFLAGS)

if !HOST_WIN32
if !DISABLE_LIBRARIES
if !DISABLE_PROFILER
bin_PROGRAMS = mprof-report

if !BITCODE
prof_shlibs = \
	libmono-profiler-aot.la \
	libmono-profiler-iomap.la \
	libmono-profiler-log.la
endif

if HAVE_VTUNE
vtune_stlibs = libmono-profiler-vtune-static.la
if !BITCODE
vtune_shlibs = libmono-profiler-vtune.la
endif
endif

lib_LTLIBRARIES = \
	$(prof_shlibs) \
	libmono-profiler-aot-static.la \
	libmono-profiler-iomap-static.la \
	libmono-profiler-log-static.la \
	$(vtune_shlibs) \
	$(vtune_stlibs)

if PLATFORM_DARWIN
prof_ldflags = -Wl,-undefined -Wl,suppress -Wl,-flat_namespace
endif

if PLATFORM_ANDROID
prof_ldflags = -avoid-version
endif

endif
endif
endif

# The log profiler uses eglib functions, so it needs to be linked against
# libeglib in shared mode, but not in static mode, since that would
# leads to duplicate symbols when it is linked into an app which
# also uses eglib (e.g. the runtime). Automake doesn't support this
# functionality, so create a separate static version of the library.

libmono_profiler_aot_la_SOURCES = mono-profiler-aot.c
libmono_profiler_aot_la_LIBADD = $(GLIB_LIBS) $(LIBICONV)
libmono_profiler_aot_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_aot_static_la_SOURCES = mono-profiler-aot.c
libmono_profiler_aot_static_la_LDFLAGS = -static

libmono_profiler_iomap_la_SOURCES = mono-profiler-iomap.c
libmono_profiler_iomap_la_LIBADD = $(GLIB_LIBS) $(LIBICONV)
libmono_profiler_iomap_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_iomap_static_la_SOURCES = mono-profiler-iomap.c
libmono_profiler_iomap_static_la_LDFLAGS = -static

libmono_profiler_log_la_SOURCES = proflog.c
libmono_profiler_log_la_LIBADD = $(GLIB_LIBS) $(Z_LIBS)
libmono_profiler_log_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_log_static_la_SOURCES = proflog.c
libmono_profiler_log_static_la_LDFLAGS = -static

if HAVE_VTUNE
libmono_profiler_vtune_la_SOURCES = mono-profiler-vtune.c
libmono_profiler_vtune_la_CFLAGS = $(VTUNE_CFLAGS)
libmono_profiler_vtune_la_LIBADD = $(VTUNE_LIBS) $(GLIB_LIBS) $(LIBICONV)
libmono_profiler_vtune_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_vtune_static_la_SOURCES = mono-profiler-vtune.c
libmono_profiler_vtune_static_la_LDFLAGS = -static
endif

mprof_report_SOURCES = decode.c
mprof_report_LDADD = $(Z_LIBS) $(GLIB_LIBS) $(LIBICONV)

PLOG_TESTS_SRC=test-alloc.cs test-busy.cs test-monitor.cs test-excleave.cs \
	test-heapshot.cs test-traces.cs
PLOG_TESTS=$(PLOG_TESTS_SRC:.cs=.exe)

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

with_mono_path = MONO_PATH=$(CLASS)

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
MCS = $(RUNTIME) $(mcs_topdir)/class/lib/build/mcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219 -debug

%.exe: %.cs
	$(MCS) -out:$@ $<

testlog: $(PLOG_TESTS)
	$(with_mono_path) perl $(srcdir)/ptestrunner.pl $(top_builddir)

if NACL_CODEGEN
check-local:
else
check-local: testlog
endif

suppressiondir = $(datadir)/mono-$(API_VER)/mono/profiler
suppression_DATA = mono-profiler-log.suppression

EXTRA_DIST=utils.c utils.h proflog.h \
	$(PLOG_TESTS_SRC) ptestrunner.pl \
	$(suppression_DATA)
