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
	image.c		\
	metadata.c	\
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
	threads.c	\
	threads.h	\
	threads-types.h	\
	#

libmetadataincludedir = $(includedir)/mono/metadata

libmetadatainclude_HEADERS = \
	assembly.h	\
	blob.h		\
	cil-coff.h	\
	mono-endian.h	\
	image.h		\
	metadata.h	\
	rawbuffer.h	\
	reflection.h	\
	row-indexes.h	\
	tabledefs.h	\
	tokentype.h	\
	loader.h	\
	class.h		\
	object.h	

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) $(GMODULE_LIBS) ../io-layer/libwapi.a


