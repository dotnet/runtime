noinst_PROGRAMS = pedump

lib_LIBRARIES = libmetadata.a

#assembliesdir = $(libdir)/$(VERSION)/assemblies
assembliesdir = $(libdir)

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)	\
	-DMONO_ASSEMBLIES=\""$(assembliesdir)"\"

libmetadata_a_SOURCES = \
	assembly.c	\
	image.c		\
	metadata.c	\
	private.h	\
	rawbuffer.c

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
	tokentype.h

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) 
