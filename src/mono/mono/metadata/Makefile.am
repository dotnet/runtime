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
else
assembliesdir = $(libdir)
confdir = $(sysconfdir)
endif

bin_PROGRAMS = monosn pedump

noinst_LTLIBRARIES = libmetadata.la libmonoruntime.la


INCLUDES = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CFLAGS) $(GLIB_CFLAGS) $(GMODULE_CFLAGS) \
	-DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

if WITH_BUNDLE
bundle_srcs = mono-bundle.s mono-bundle.h
mono-bundle.s mono-bundle.h: $(BUNDLE_FILE) $(srcdir)/make-bundle.pl
	perl $(srcdir)/make-bundle.pl $(BUNDLE_FILE) mono-bundle.h mono-bundle.s
else
bundle_srcs =
endif

libmonoruntime_la_SOURCES = \
	reflection.c	\
	object.c	\
	icall.c		\
	decimal.c	\
	decimal.h	\
	gc.c		\
	gc-internal.h	\
	marshal.c	\
	marshal.h	\
	monitor.c	\
	monitor.h	\
	threads.c	\
	threadpool.c	\
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
	string-icalls.c \
	string-icalls.h \
	sysmath.h	\
	sysmath.c	\
	process.c	\
	process.h	\
	environment.c	\
	environment.h

libmetadata_la_SOURCES = \
	assembly.c	\
	domain.c	\
	opcodes.c	\
	image.c		\
	metadata.c	\
	verify.c	\
	mono-endian.c	\
	mono-config.c	\
	mono-config.h	\
	private.h	\
	rawbuffer.c	\
	loader.c	\
	class.c		\
	mempool.c	\
	$(bundle_srcs)	\
	debug-helpers.c

libmetadataincludedir = $(includedir)/mono/metadata
libmonoruntimeincludedir = $(includedir)/mono/metadata

libmonoruntimeinclude_HEADERS = \
	appdomain.h	\
	mono-debug.h	\
	mono-debug-debugger.h	\
	debug-mono-symfile.h	\
	threadpool.h	\
	threads-types.h	\
	threads.h	\
	environment.h	\
	monitor.h

libmetadatainclude_HEADERS = \
	assembly.h	\
	opcodes.h	\
	blob.h		\
	cil-coff.h	\
	mono-endian.h	\
	image.h		\
	metadata.h	\
	verify.h	\
	rawbuffer.h	\
	reflection.h	\
	row-indexes.h	\
	tabledefs.h	\
	tokentype.h	\
	loader.h	\
	class.h		\
	object.h	\
	exception.h	\
	profiler.h	\
	appdomain.h	\
	debug-helpers.h	\
	mempool.h	\
	rand.h

pedump_SOURCES =		\
	pedump.c

pedump_LDADD = libmetadata.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) $(GMODULE_LIBS) -lm


monosn_LDADD = \
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(LIBGC_LIBS)			\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

BUILT_SOURCES = $(bundle_srcs)

EXTRA_DIST = make-bundle.pl sample-bundle

