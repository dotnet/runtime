noinst_PROGRAMS = pedump

lib_LIBRARIES = libmetadata.a libmonoruntime.a

#
# Keep in sync with mono/runtime/Makefile.am
#
assembliesdir = $(libdir)

INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS) -I$(top_srcdir)	-I$(top_srcdir)/mono \
	-DMONO_ASSEMBLIES=\""$(assembliesdir)"\"		

libmonoruntime_a_SOURCES = \
	reflection.c	\
	object.c	\
	icall.c		\
	decimal.c	\
	decimal.h	\
	marshal.c	\
	marshal.h	\
	threads.c	\
	threads.h	\
	threads-types.h	\
	file-io.c	\
	file-io.h	\
	socket-io.c	\
	socket-io.h	\
	exception.c	\
	exception.h	\
	unicode.c	\
	unicode.h	\
	appdomain.h	\
	appdomain.c	\
	debug-helpers.h	\
	debug-helpers.c	\
	debug-symfile.h	\
	debug-symfile.c	\
	profiler.c	\
	profiler-private.h	\
	rand.h		\
	rand.c		\
	string-icalls.c \
	string-icalls.h \
	sysmath.h	\
	sysmath.c

libmetadata_a_SOURCES = \
	assembly.c	\
	domain.c	\
	opcodes.c	\
	image.c		\
	metadata.c	\
	verify.c	\
	mono-endian.c	\
	private.h	\
	rawbuffer.c	\
	loader.c	\
	class.c		\
	mempool.h	\
	mempool.c	\
	debug-helpers.h	\
	debug-helpers.c

libmetadataincludedir = $(includedir)/mono/metadata
libmonoruntimeincludedir = $(includedir)/mono/metadata

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
	rand.h

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = ../io-layer/libwapi.a ../utils/libmonoutils.a $(GLIB_LIBS) $(GMODULE_LIBS) -lm


