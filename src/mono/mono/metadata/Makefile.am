noinst_PROGRAMS = pedump

lib_LIBRARIES = libmetadata.a

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

libmetadata_a_SOURCES = \
	assembly.c	\
	metadata.c	\
	rawbuffer.c

libmetadataincludedir = $(includedir)/mono/metadata

libmetadata_a_HEADERS = \
	assembly.h	\
	cil-coff.h	\
	eltype.h	\
	endian.h	\
	fieldattr.h	\
	metadata.h	\
	methodattr.h	\
	methodsem.h	\
	paramattr.h	\
	propertyattr.h	\
	rawbuffer.h	\
	tokentype.h	\
	typeattr.h

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) 
