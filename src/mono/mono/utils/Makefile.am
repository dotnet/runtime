noinst_LTLIBRARIES = libmonoutils.la

INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS) -I$(top_srcdir)	-I$(top_srcdir)/mono 

libmonoutils_la_SOURCES = \
	mono-hash.c	\
	monobitset.c	\
	strtod.h	\
	strtod.c

libmonoutilsincludedir = $(includedir)/mono/utils

libmonoutilsinclude_HEADERS = \
	monobitset.h	\
	mono-hash.h

EXTRA_DIST = ChangeLog

