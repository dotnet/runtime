noinst_LTLIBRARIES = libeglib.la

libeglib_la_SOURCES = \
	eglib-config.h	\
	sort.frag.h	\
	glib.h	     	\
	garray.c	\
	gerror.c	\
	ghashtable.c 	\
	gmem.c       	\
	gmodule.c    	\
	goutput.c    	\
	gstr.c       	\
	gslist.c     	\
	gstring.c    	\
	gptrarray.c     \
	glist.c		\
	gmisc.c		\
	gqueue.c	\
	gpath.c		\
	gshell.c	\
	gspawn.c	\
	gtimer.c	\
	gdate.c		\
	gfile.c		\
	gpattern.c	\
	gdir.c		\
	gutf8.c

libeglib_la_CFLAGS = -Wall -Werror -D_FORTIFY_SOURCE=2

INCLUDES = -I$(srcdir)

libeglib_la_LIBADD = -lm

MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = eglib-config.h.in
