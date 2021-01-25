MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY

AM_CPPFLAGS = \
	-DSUPPRESSION_DIR=\""$(datadir)/mono-$(API_VER)/mono/profiler"\" \
	-I$(top_srcdir) \
	$(GLIB_CFLAGS) \
	$(SHARED_CFLAGS)

glib_libs = $(top_builddir)/mono/eglib/libeglib.la

prof_ldflags = $(CCLDFLAGS)

if !HOST_WIN32
if !DISABLE_LIBRARIES
if !DISABLE_PROFILER

if !DISABLE_EXECUTABLES
bin_PROGRAMS = mprof-report
endif

if HAVE_VTUNE
vtune_libs = \
	libmono-profiler-vtune.la \
	libmono-profiler-vtune-static.la
endif

lib_LTLIBRARIES = \
	libmono-profiler-aot.la \
	libmono-profiler-aot-static.la \
	libmono-profiler-log.la \
	libmono-profiler-log-static.la \
	libmono-profiler-coverage.la \
	libmono-profiler-coverage-static.la \
	$(vtune_libs)

suppressiondir = $(datadir)/mono-$(API_VER)/mono/profiler
suppression_DATA = mono-profiler-coverage.suppression

# FIXME: The profiler tests currently don't work with coop because the
# sampling infrastructure depends on signals being available.
#
# See: https://bugzilla.xamarin.com/show_bug.cgi?id=57011
if !ENABLE_COOP_SUSPEND
check_targets = run-test
endif

endif
endif
endif

# On Apple hosts, we want to allow undefined symbols when building the
# profiler modules as, just like on Linux, we don't link them to libmono,
# but rather let the dynamic linker sort things out.
#
# Bitcode, specifically, doesn't allow undefined symbols at all, even for
# shared libraries, so we want errors if the profiler modules contain any.
if HOST_DARWIN
if BITCODE
prof_ldflags += -no-undefined
else
prof_ldflags += -Wl,-undefined -Wl,suppress -Wl,-flat_namespace
endif
endif

# Versioned libraries are problematic on Android. In particular, we're not
# allowed to place versioned libraries inside APKs, so if libmono and the
# profiler modules contain versioned sonames, things will fail to resolve
# at runtime.
if HOST_ANDROID
prof_ldflags += -avoid-version
endif

# Linking to libmono on desktop can cause problems when loading a profiler
# module into a statically-linked mono executable, e.g. running library
# constructors multiple times, which can easily break LLVM libraries. So,
# we don't want to link to libmono on desktop. On Android, libmono is
# always loaded dynamically, so we do need to link to libmono there or the
# dynamic linker will have trouble resolving everything. On iOS, the story
# is similar to Android when running in the simulator.
if HOST_ANDROID
libmono_dep = $(top_builddir)/mono/mini/$(LIBMONO_LA)
else
if HOST_IOS
# Note that BITCODE implies HOST_IOS, so this case ensures that we link to
# libmono when building a bitcode runtime.
libmono_dep = $(top_builddir)/mono/mini/$(LIBMONO_LA)
else
if HOST_TIZEN
libmono_dep = $(top_builddir)/mono/mini/$(LIBMONO_LA)
else
# BITCODE => -no-undefined
if HOST_DARWIN
if BITCODE
libmono_dep = $(top_builddir)/mono/mini/$(LIBMONO_LA)
else
libmono_dep =
endif
else
libmono_dep =
endif
endif
endif
endif

if HAVE_STATIC_ZLIB
zlib_dep = $(STATIC_ZLIB_PATH)
else
if HAVE_SYS_ZLIB
# The log profiler uses zlib for output compression when available.
zlib_dep = -lz
else
zlib_dep =
endif
endif

# We build a separate, static version of each profiler for use on targets
# which do not support dynamic linking (e.g. iOS). We still want to build
# the shared version on those targets since it's useful for e.g. simulator
# builds.

libmono_profiler_aot_la_SOURCES = aot.c helper.c
libmono_profiler_aot_la_LIBADD =  $(libmono_dep) $(glib_libs)
libmono_profiler_aot_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_aot_static_la_SOURCES = aot.c helper.c
libmono_profiler_aot_static_la_LDFLAGS = -static

libmono_profiler_log_la_SOURCES = log.c log-args.c helper.c
libmono_profiler_log_la_LIBADD = $(libmono_dep) $(glib_libs) $(zlib_dep)
libmono_profiler_log_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_log_static_la_SOURCES = log.c log-args.c helper.c
libmono_profiler_log_static_la_LDFLAGS = -static

libmono_profiler_coverage_la_SOURCES = coverage.c
libmono_profiler_coverage_la_LIBADD = $(libmono_dep) $(glib_libs)
libmono_profiler_coverage_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_coverage_static_la_SOURCES = coverage.c
libmono_profiler_coverage_static_la_LDFLAGS = -static

if HAVE_VTUNE
libmono_profiler_vtune_la_SOURCES = vtune.c
libmono_profiler_vtune_la_CFLAGS = $(VTUNE_CFLAGS)
libmono_profiler_vtune_la_LIBADD = $(VTUNE_LIBS) $(libmono_dep) $(glib_libs)
libmono_profiler_vtune_la_LDFLAGS = $(prof_ldflags)
libmono_profiler_vtune_static_la_SOURCES = vtune.c
libmono_profiler_vtune_static_la_LDFLAGS = -static
libmono_profiler_vtune_static_la_CFLAGS = $(VTUNE_CFLAGS)
libmono_profiler_vtune_static_la_LIBADD = $(VTUNE_LIBS)
endif

mprof_report_SOURCES = mprof-report.c
mprof_report_LDADD = $(glib_libs) $(zlib_dep)
mprof_report_LDFLAGS = -no-undefined

noinst_LTLIBRARIES = libproftest_pinvokes.la

libproftest_pinvokes_la_SOURCES = proftest-pinvokes.c
if HOST_WIN32
# (borrowed from mono/tests/Makefile.am libtest_la_LDFLAGS)
# the exported names created by gcc for stdcall functions are missing the leading _, so MS.NET
# can't find them. So we use --kill-at to remove the @ suffix as well.
libproftest_pinvokes_la_LDFLAGS = -no-undefined -rpath `pwd` $(CCLDFLAGS) -Wl,--kill-at
else
libproftest_pinvokes_la_LDFLAGS = -no-undefined -rpath `pwd` $(CCLDFLAGS)
endif


PLOG_TESTS_SRC = \
	test-alloc.cs \
	test-busy.cs \
	test-monitor.cs \
	test-excleave.cs \
	test-heapshot.cs \
	test-pinvokes.cs \
	test-traces.cs

PLOG_TESTS = $(PLOG_TESTS_SRC:.cs=.exe)

CLASS = $(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)
TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper
MCS = $(TOOLS_RUNTIME) $(CSC) -lib:$(CLASS) -unsafe -nologo -noconfig -nowarn:0162 -nowarn:0168 -nowarn:0219 -debug:portable

%.exe: %.cs
	$(MCS) -out:$@ $<

test-pinvokes.exe: libproftest_pinvokes.la

test-local: $(PLOG_TESTS)

test-bundle-local:
	mkdir -p $(TEST_BUNDLE_PATH)/tests/profiler/
	cp -L .libs/libmono-profiler-log$(libsuffix) $(TEST_BUNDLE_PATH)/
	cp -L $(PLOG_TESTS) $(TEST_BUNDLE_PATH)/tests/profiler/
	cp -L .libs/libproftest_pinvokes$(libsuffix) $(TEST_BUNDLE_PATH)/tests/profiler/
	cp -L ptestrunner.pl $(TEST_BUNDLE_PATH)/tests/profiler/
	cp -L mprof-report $(TEST_BUNDLE_PATH)/
	chmod +x $(TEST_BUNDLE_PATH)/mprof-report

run-test: test
	MONO_PATH=$(CLASS) perl $(srcdir)/ptestrunner.pl $(top_builddir) nunit TestResult-profiler.xml

check-local: $(check_targets)

EXTRA_DIST = \
	log.h \
	aot.h \
	helper.h \
	$(PLOG_TESTS_SRC) \
	ptestrunner.pl \
	$(suppression_DATA)

endif # !ENABLE_MSVC_ONLY

if ENABLE_MSVC_ONLY

all-local:

	$(MAKE) -C $(top_srcdir)/msvc mprof-report
	cp -f $(mono_msvc_build_bin_dir)/mprof-report$(EXEEXT) ./mprof-report$(EXEEXT)

clean-local:

	$(MAKE) -C $(top_srcdir)/msvc clean-mprof-report
	rm ./mprof-report$(EXEEXT)

endif # ENABLE_MSVC_ONLY
