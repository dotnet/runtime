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
	private.h	\
	rawbuffer.c	\
	loader.c	\
	class.c		\
	object.c	\
	icall.c		\
	threads.h	\
	threads-pthread.c	\
	threads-pthread.h	\
	#

libmetadataincludedir = $(includedir)/mono/metadata

libmetadatainclude_HEADERS = \
	assembly.h	\
	blob.h		\
	cil-coff.h	\
	endian.h	\
	image.h		\
	metadata.h	\
	rawbuffer.h	\
	row-indexes.h	\
	tabledefs.h	\
	tokentype.h	\
	loader.h	\
	class.h		\
	object.h	

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) $(GMODULE_LIBS) $(PTHREAD_LIBS)


