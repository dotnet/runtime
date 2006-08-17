noinst_LTLIBRARIES = libeglib.la

libeglib_la_SOURCES = \
	ghashtable.c	\
	gmem.c		\
	gmodule.c	\
	goutput.c	\
	gstr.c		\
	gslist.c        \
	gstring.c

libeglib_la_CFLAGS = -Wall -Werror -D_FORTIFY_SOURCE=2

INCLUDES = -I$(srcdir)

libeglib_la_LIBADD = -lm

MAINTAINERCLEANFILES = Makefile.in

