INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

bin_PROGRAMS = monodis

lib_LIBRARIES = libmonodis.a

libmonodis_a_SOURCES =	\
	get.c		\
	get.h		\
	dis-cil.c	\
	dis-cil.h	\
	util.c		\
	util.h

monodis_SOURCES =	\
	dump.c		\
	dump.h		\
	main.c		\
	meta.h

monodis_LDADD = 			\
	../metadata/libmetadata.a 	\
	libmonodis.a			\
	../../libffi/libffi.a		\
	$(GMODULE_LIBS) 		\
	$(GLIB_LIBS) 

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)