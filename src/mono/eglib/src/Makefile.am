noinst_LTLIBRARIES = libeglib.la

libeglib_la_SOURCES = \
	eglib-config.h	\
	glib.h	     	\	
	ghashtable.c 	\
	gmem.c       	\
	gmodule.c    	\
	goutput.c    	\
	gstr.c       	\
	gslist.c     	\
	gstring.c    	\
	gptrarray.c

libeglib_la_CFLAGS = -Wall -Werror -D_FORTIFY_SOURCE=2

INCLUDES = -I$(srcdir)

libeglib_la_LIBADD = -lm

MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = eglib-config.h.in