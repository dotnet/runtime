INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

bin_PROGRAMS = monodis

monodis_SOURCES =	\
	main.c		\
	util.c		\
	util.h

monodis_LDADD = 			\
	../metadata/libmetadata.a 	\
	$(GLIB_LIBS) 

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)