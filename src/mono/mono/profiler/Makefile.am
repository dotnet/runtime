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

if HAVE_VTUNE
vtune_libs = libmono-profiler-vtune.la libmono-profiler-vtune-static.la
endif

lib_LTLIBRARIES = \
	libmono-profiler-aot.la \
	libmono-profiler-aot-static.la \
	libmono-profiler-iomap.la \
	libmono-profiler-iomap-static.la \
	libmono-profiler-log.la \
	libmono-profiler-log-static.la \
	$(vtune_libs)

suppressiondir = $(datadir)/mono-$(API_VER)/mono/profiler
suppression_DATA = mono-profiler-log.suppression

if PLATFORM_DARWIN
if BITCODE
prof_ldflags = -no-undefined
else
prof_ldflags = -Wl,-undefined -Wl,suppress -Wl,-flat_namespace
endif
endif

if TARGET_OSX
libmono_dep =
else
libmono_dep = $(monodir)/mono/mini/$(LIBMONO_LA)
endif

if PLATFORM_ANDROID
prof_ldflags = -avoid-version
endif

# FIXME fix the profiler tests to work with coop.
if !ENABLE_COOP
check_targets = testlog
endif

endif
endif
endif

monodir=$(top_builddir)

# The log profiler uses eglib functions, so it needs to be linked against
# libeglib in shared mode, but not in static mode, since that would
# leads to duplicate symbols when it is linked into an app which
# also uses eglib (e.g. the runtime). Automake doesn't support this
# functionality, so create a separate static version of the library.

libmono_profiler_aot_la_SOURCES = mono-profiler-aot.c
libmono_profiler_aot_la_LIBADD =  $(libmono_dep) $(GLIB_LIBS) $(LIBICONV)
libmono_profiler_aot_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_aot_static_la_SOURCES = mono-profiler-aot.c
libmono_profiler_aot_static_la_LDFLAGS = -static

libmono_profiler_iomap_la_SOURCES = mono-profiler-iomap.c
libmono_profiler_iomap_la_LIBADD = $(libmono_dep) $(GLIB_LIBS) $(LIBICONV)
libmono_profiler_iomap_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_iomap_static_la_SOURCES = mono-profiler-iomap.c
libmono_profiler_iomap_static_la_LDFLAGS = -static

libmono_profiler_log_la_SOURCES = mono-profiler-log.c
libmono_profiler_log_la_LIBADD = $(libmono_dep) $(GLIB_LIBS) $(Z_LIBS)
libmono_profiler_log_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_log_static_la_SOURCES = mono-profiler-log.c
libmono_profiler_log_static_la_LDFLAGS = -static

if HAVE_VTUNE
libmono_profiler_vtune_la_SOURCES = mono-profiler-vtune.c
libmono_profiler_vtune_la_CFLAGS = $(VTUNE_CFLAGS)
libmono_profiler_vtune_la_LIBADD = $(VTUNE_LIBS) $(LIBMONO) $(GLIB_LIBS) $(LIBICONV)
libmono_profiler_vtune_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_vtune_static_la_SOURCES = mono-profiler-vtune.c
libmono_profiler_vtune_static_la_LDFLAGS = -static
libmono_profiler_vtune_static_la_CFLAGS = $(VTUNE_CFLAGS)
libmono_profiler_vtune_static_la_LIBADD = $(VTUNE_LIBS)
endif

mprof_report_SOURCES = mprof-report.c
mprof_report_LDADD = $(Z_LIBS) $(GLIB_LIBS) $(LIBICONV)

PLOG_TESTS_SRC=test-alloc.cs test-busy.cs test-monitor.cs test-excleave.cs \
	test-heapshot.cs test-traces.cs
PLOG_TESTS=$(PLOG_TESTS_SRC:.cs=.exe)

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper
MCS = $(TOOLS_RUNTIME) $(CSC) -lib:$(CLASS) -unsafe -nologo -noconfig -nowarn:0162 -nowarn:0168 -nowarn:0219 -debug:portable

%.exe: %.cs
	$(MCS) -out:$@ $<

testlog: $(PLOG_TESTS)
	MONO_PATH=$(CLASS) perl $(srcdir)/ptestrunner.pl $(top_builddir)

check-local: $(check_targets)

EXTRA_DIST=mono-profiler-log.h \
	mono-profiler-aot.h \
	$(PLOG_TESTS_SRC) \
	ptestrunner.pl \
	$(suppression_DATA)
