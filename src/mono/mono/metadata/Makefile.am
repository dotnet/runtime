if PLATFORM_WIN32
# Use -m here. This will use / as directory separator (C:/WINNT).
# The files that use MONO_ASSEMBLIES and/or MONO_CFG_DIR replace the
# / by \ if running under WIN32.
assembliesdir = `cygpath -m "${libdir}"`
confdir = `cygpath -m "${sysconfdir}"`
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
AM_CFLAGS = -O
else
assembliesdir = $(exec_prefix)/lib
confdir = $(sysconfdir)
endif

bin_PROGRAMS = pedump

#
# libtool is not capable of creating static/shared versions of the same
# convenience lib, so we have to do it ourselves
#
noinst_LTLIBRARIES = libmonoruntime.la libmonoruntime-static.la

INCLUDES = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CFLAGS) $(GLIB_CFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

#
# Make sure any prefix changes are updated in the binaries too.
#
#  assembly.c uses MONO_ASSEMBLIES
#  mono-config.c uses MONO_CFG_DIR
#
# This won't result in many more false positives than AC_DEFINEing them
# in configure.in.
#
assembly.lo mono-config.lo: Makefile

CLEANFILES = mono-bundle.stamp

libmonoruntime_static_la_LIBADD = $(bundle_obj) $(libmonoruntime_la_LIBADD)

libmonoruntime_la_SOURCES = \
	reflection.c	\
	object.c	\
	object-internals.h	\
	icall.c		\
	icall-def.h		\
	char-conversions.h \
	decimal.c	\
	decimal.h	\
	boehm-gc.c	\
	null-gc.c	\
	sgen-gc.c	\
	sgen-gc.h	\
	gc.c		\
	gc-internal.h	\
	method-builder.h \
	method-builder.c \
	marshal.c	\
	marshal.h	\
	mono-mlist.c	\
	mono-mlist.h	\
	monitor.c	\
	monitor.h	\
	tabledefs.h \
	threads.c	\
	threads-types.h	\
	threadpool.c	\
	threadpool.h	\
	threadpool-internals.h	\
	file-io.c	\
	file-io.h	\
	socket-io.c	\
	socket-io.h	\
	exception.c	\
	exception.h	\
	appdomain.c	\
	debug-mono-symfile.h	\
	debug-mono-symfile.c	\
	mono-debug.h		\
	mono-debug.c		\
	mono-debug-debugger.h	\
	mono-debug-debugger.c	\
	profiler.c	\
	profiler-private.h	\
	rand.h		\
	rand.c		\
	security.c	\
	security.h	\
	security-core-clr.c	\
	security-core-clr.h	\
	string-icalls.c \
	string-icalls.h \
	sysmath.h	\
	sysmath.c	\
	process.c	\
	process.h	\
	environment.c	\
	environment.h	\
	locales.c	\
	locales.h	\
	normalization-tables.h	\
	filewatcher.c	\
	filewatcher.h	\
	culture-info.h  \
	culture-info-tables.h	\
	security-manager.c	\
	security-manager.h	\
	console-io.c		\
	console-io.h	\
	assembly.c	\
	coree.c \
	coree.h \
	domain.c	\
	domain-internals.h	\
	opcodes.c	\
	image.c		\
	cil-coff.h	\
	metadata.c	\
	metadata-internals.h	\
	number-formatter.h	\
	verify.c	\
	verify-internals.h	\
	mono-endian.c	\
	mono-endian.h	\
	mono-config.c	\
	rawbuffer.c	\
	rawbuffer.h	\
	loader.c	\
	class.c		\
	class-internals.h		\
	wrapper-types.h		\
	mempool.c	\
	mono-perfcounters.c	\
	mono-perfcounters.h	\
	mono-perfcounters-def.h	\
	debug-helpers.c	\
	generic-sharing.c

libmonoruntime_static_la_SOURCES = $(libmonoruntime_la_SOURCES)

libmonoruntime_static_la_LDFLAGS = -static

libmonoruntimeincludedir = $(includedir)/mono-$(API_VER)/mono/metadata

libmonoruntimeinclude_HEADERS = \
	attrdefs.h	\
	appdomain.h	\
	mono-debug.h	\
	mono-gc.h	\
	debug-mono-symfile.h	\
	threads.h	\
	environment.h	\
	assembly.h	\
	opcodes.h	\
	blob.h		\
	image.h		\
	metadata.h	\
	verify.h	\
	reflection.h	\
	row-indexes.h	\
	tokentype.h	\
	loader.h	\
	class.h		\
	object.h	\
	exception.h	\
	profiler.h	\
	appdomain.h	\
	mono-config.h	\
	debug-helpers.h	\
	mempool.h

if DTRACE_G_REQUIRED

PEDUMP_DTRACE_OBJECT = pedump-dtrace.$(OBJEXT)

pedump-dtrace.$(OBJEXT): $(top_srcdir)/data/mono.d libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la
	DTRACE="$(DTRACE)" DTRACEFLAGS="$(DTRACEFLAGS)" AR="$(AR)" $(SHELL) $(top_srcdir)/dtrace-prelink.sh \
	--pic pedump-dtrace.$(OBJEXT) $(top_srcdir)/data/mono.d libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la

else
PEDUMP_DTRACE_OBJECT = 
endif

pedump_SOURCES =		\
	pedump.c

pedump_LDADD = libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(PEDUMP_DTRACE_OBJECT)

EXTRA_DIST = make-bundle.pl sample-bundle

