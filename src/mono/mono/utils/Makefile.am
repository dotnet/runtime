noinst_LTLIBRARIES = libmonoutils.la

INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS) $(LIBGC_CFLAGS) -I$(top_srcdir)	-I$(top_srcdir)/mono

libmonoutils_la_SOURCES = \
	mono-hash.c	\
	mono-md5.c	\
	mono-sha1.c	\
	mono-logger.c	\
	monobitset.c	\
	strtod.h	\
	strtod.c

libmonoutilsincludedir = $(includedir)/mono/utils

libmonoutils_la_LIBADD = $(LIBGC_LIBS)

libmonoutilsinclude_HEADERS = \
	monobitset.h	\
	mono-digest.h	\
	mono-logger.h	\
	mono-hash.h

EXTRA_DIST = ChangeLog

