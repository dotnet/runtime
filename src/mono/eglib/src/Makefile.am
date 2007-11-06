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
	gmodule.h	\
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
	gmarkup.c	\
	gutf8.c		\
	gunicode.c

libeglib_la_CFLAGS = -Wall -D_FORTIFY_SOURCE=2

INCLUDES = -I$(srcdir)

if PLATFORM_WIN32
libeglib_la_LIBADD = -lm -liconv -lpsapi
else
libeglib_la_LIBADD = -lm
endif

MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = eglib-config.h.in
