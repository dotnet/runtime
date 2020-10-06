MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(GLIB_CFLAGS)

if HOST_DARWIN
test_ldflags = -framework CoreFoundation -framework Foundation
endif

sgen_libs = \
	$(top_builddir)/mono/metadata/libmonoruntimesgen.la	\
	$(top_builddir)/mono/sgen/libmonosgen.la	\
	$(top_builddir)/mono/utils/libmonoutils.la \
	$(top_builddir)/mono/eglib/libeglib.la

mini_libs = \
	$(top_builddir)/mono/mini/libmini.la

if !DISABLE_INTERPRETER
mini_libs += $(top_builddir)/mono/mini/libmono-ee-interp.la
endif

if !DISABLE_DEBUGGER_AGENT
mini_libs += $(top_builddir)/mono/mini/libmono-dbg.la
endif

eventpipe_libs = \
	$(top_builddir)/mono/eventpipe/libeventpipe.la

CFLAGS = $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)

if !HOST_WIN32
if ENABLE_PERFTRACING

test_eventpipe_SOURCES = \
ep-buffer-manager-tests.c	\
ep-buffer-tests.c	\
ep-fake-tests.c	\
ep-fastserializer-tests.c	\
ep-file-tests.c	\
ep-provider-callback-dataqueue-tests.c	\
ep-rt-tests.c	\
ep-session-tests.c	\
ep-setup-tests.c	\
ep-teardown-tests.c \
ep-tests.c	\
ep-tests.h	\
ep-test-runner.c	\
ep-test-driver.c	\
ep-thread-tests.c \
ep-tests-debug.h

test_eventpipe_CFLAGS = $(AM_CFLAGS) $(SGEN_DEFINES)
test_eventpipe_LDADD = $(mini_libs) $(sgen_libs) $(eventpipe_libs)
test_eventpipe_LDFLAGS = $(test_ldflags)

noinst_PROGRAMS = test-eventpipe

endif ENABLE_PERFTRACING
endif !HOST_WIN32
