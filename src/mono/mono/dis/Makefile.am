INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

bin_PROGRAMS = monodis

monodis_SOURCES =	\
	get.c		\
	get.h		\
	dis.h		\
	dump.c		\
	dump.h		\
	main.c		\
	meta.h		\
	util.c		\
	util.h

monodis_LDADD = 			\
	../metadata/libmetadata.a 	\
	$(GLIB_LIBS) 

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)