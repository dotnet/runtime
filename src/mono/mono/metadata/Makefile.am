if PLATFORM_WIN32
export HOST_CC
endif

bin_PROGRAMS = monosn

noinst_PROGRAMS = pedump

noinst_LTLIBRARIES = libmetadata.la libmonoruntime.la

#
# Keep in sync with mono/runtime/Makefile.am
#
assembliesdir = $(libdir)

INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS) -I$(top_srcdir)	-I$(top_srcdir)/mono \
	-DMONO_ASSEMBLIES=\""$(assembliesdir)"\" -DMONO_CFG_DIR=\""$(sysconfdir)"\"

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
	debug-helpers.c

libmetadataincludedir = $(includedir)/mono/metadata
libmonoruntimeincludedir = $(includedir)/mono/metadata

libmonoruntimeinclude_HEADERS = \
	appdomain.h	\
	debug-mono-symfile.h	\
	threadpool.h	\
	threads-types.h	\
	threads.h	\
	environment.h

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

pedump_LDADD = libmetadata.la ../io-layer/libwapi.la ../utils/libmonoutils.la $(GLIB_LIBS) $(GMODULE_LIBS) -lm


monosn_LDADD = \
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

