noinst_PROGRAMS = pedump

lib_LIBRARIES = libmetadata.a

#
# Keep in sync with mono/runtime/Makefile.am
#
assembliesdir = $(libdir)

INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS) -I$(top_srcdir)	\
	-DMONO_ASSEMBLIES=\""$(assembliesdir)"\"		

libmetadata_a_SOURCES = \
	assembly.c	\
	opcodes.c	\
	image.c		\
	metadata.c	\
	verify.c	\
	mono-endian.c	\
	private.h	\
	rawbuffer.c	\
	reflection.c	\
	loader.c	\
	class.c		\
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
	mempool.h	\
	mempool.c	\
	appdomain.h	\
	appdomain.c	\
	rand.h		\
	rand.c

libmetadataincludedir = $(includedir)/mono/metadata

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
	appdomain.h	\
	rand.h

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) $(GMODULE_LIBS) ../io-layer/libwapi.a


