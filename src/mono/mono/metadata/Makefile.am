noinst_PROGRAMS = pedump

lib_LIBRARIES = libmetadata.a

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

libmetadata_a_SOURCES = \
	assembly.h	\
	assembly.c	\
	cil-coff.h	\
	eltype.h	\
	endian.h	\
	fieldattr.h	\
	metadata.c	\
	metadata.h	\
	rawbuffer.c	\
	rawbuffer.h	\
	typeattr.h

pedump_SOURCES =		\
	pedump.c $(libmetadata_a_SOURCES)

pedump_LDADD = $(GLIB_LIBS) 
