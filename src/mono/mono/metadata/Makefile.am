noinst_PROGRAMS = pedump

lib_LIBRARIES = libmetadata.a

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

libmetadata_a_SOURCES = \
	image.c		\
	metadata.c	\
	rawbuffer.c

libmetadataincludedir = $(includedir)/mono/metadata

libmetadatainclude_HEADERS = \
	blobsig.h	\
	cil-coff.h	\
	eltype.h	\
	endian.h	\
	fieldattr.h	\
	image.h		\
	metadata.h	\
	methodattr.h	\
	methodheader.h	\
	methodsem.h	\
	paramattr.h	\
	propertyattr.h	\
	rawbuffer.h	\
	tokentype.h	\
	typeattr.h

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) 
