if PLATFORM_WIN32
export HOST_CC
# Use -m here. This will use / as directory separator (C:/WINNT).
# The files that use MONO_ASSEMBLIES and/or MONO_CFG_DIR replace the
# / by \ if running under WIN32.
assembliesdir = `cygpath -m "${libdir}"`
confdir = `cygpath -m "${sysconfdir}"`
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
AM_CFLAGS = -O
PLATFORM_LIB = ../os/libmonoos.la
else
assembliesdir = $(libdir)
confdir = $(sysconfdir)
endif

bin_PROGRAMS = pedump

#
# libtool is not capable of creating static/shared versions of the same
# convenience lib, so we have to do it ourselves
#
noinst_LTLIBRARIES = libmetadata.la libmonoruntime.la libmetadata-static.la libmonoruntime-static.la


INCLUDES = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CFLAGS) $(GLIB_CFLAGS) $(GMODULE_CFLAGS) $(ICU_CFLAGS) \
	-DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"


CLEANFILES = mono-bundle.stamp

libmetadata_la_LIBADD = $(bundle_obj) $(PLATFORM_LIB)

libmonoruntime_static_la_LIBADD = $(libmonoruntime_la_LIBADD)
libmetadata_static_la_LIBADD = $(libmetadata_la_LIBADD)

libmonoruntime_la_SOURCES = \
	reflection.c	\
	object.c	\
	object-internals.h	\
	icall.c		\
	char-conversions.h \
	decimal.c	\
	decimal.h	\
	gc.c		\
	gc-internal.h	\
	marshal.c	\
	marshal.h	\
	monitor.c	\
	monitor.h	\
	threads.c	\
	threads-types.h	\
	threadpool.c	\
	threadpool.h	\
	file-io.c	\
	file-io.h	\
	socket-io.c	\
	socket-io.h	\
	exception.c	\
	exception.h	\
	unicode.c	\
	unicode.h	\
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
	filewatcher.c	\
	filewatcher.h	\
	culture-info.h  \
	culture-info-tables.h

libmetadata_la_SOURCES = \
	assembly.c	\
	domain.c	\
	domain-internals.h	\
	opcodes.c	\
	image.c		\
	cil-coff.h	\
	tabledefs.h	\
	metadata.c	\
	metadata-internals.h	\
	verify.c	\
	mono-endian.c	\
	mono-endian.h	\
	mono-config.c	\
	mono-config.h	\
	private.h	\
	rawbuffer.c	\
	rawbuffer.h	\
	loader.c	\
	class.c		\
	class-internals.h		\
	mempool.c	\
	debug-helpers.c

libmetadata_static_la_SOURCES = $(libmetadata_la_SOURCES)
libmonoruntime_static_la_SOURCES = $(libmonoruntime_la_SOURCES)

libmetadata_static_la_LDFLAGS = -static
libmonoruntime_static_la_LDFLAGS = -static

libmetadataincludedir = $(includedir)/mono/metadata
libmonoruntimeincludedir = $(includedir)/mono/metadata

libmonoruntimeinclude_HEADERS = \
	appdomain.h	\
	mono-debug.h	\
	mono-debug-debugger.h	\
	debug-mono-symfile.h	\
	threads.h	\
	environment.h

libmetadatainclude_HEADERS = \
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
	debug-helpers.h	\
	mempool.h

pedump_SOURCES =		\
	pedump.c

pedump_LDADD = libmetadata.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) $(GMODULE_LIBS) -lm

EXTRA_DIST = make-bundle.pl sample-bundle

