noinst_LTLIBRARIES = libmonoutils.la

INCLUDES = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CFLAGS) $(GLIB_CFLAGS) $(GMODULE_CFLAGS)

libmonoutils_la_SOURCES = \
	mono-hash.c	\
	mono-md5.c	\
	mono-sha1.c	\
	mono-logger.c	\
	monobitset.c	\
	mono-math.c  \
	strtod.h	\
	strtod.c	\
	strenc.h	\
	strenc.c

libmonoutilsincludedir = $(includedir)/mono/utils

libmonoutilsinclude_HEADERS = \
	monobitset.h	\
	mono-digest.h	\
	mono-logger.h	\
	mono-hash.h		\
	mono-math.h

EXTRA_DIST = ChangeLog

