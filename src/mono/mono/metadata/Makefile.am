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
	rawbuffer.c

libmetadataincludedir = $(includedir)/mono/metadata

libmetadatainclude_HEADERS = \
	assembly.h	\
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
