noinst_LTLIBRARIES = libmonoutils.la

INCLUDES = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CFLAGS) $(GLIB_CFLAGS) $(GMODULE_CFLAGS)

if EGLIB_BUILD 
	hash_sources = 
else
	hash_sources = mono-hash.c
endif

libmonoutils_la_SOURCES = \
	$(hash_sources)	\
	mono-md5.c	\
	mono-sha1.c	\
	mono-logger.c	\
	mono-codeman.c	\
	mono-counters.c	\
	mono-compiler.h	\
	monobitset.c	\
	mono-math.c  \
	strtod.h	\
	strtod.c	\
	strenc.h	\
	strenc.c	\
	mono-uri.c	\
	mono-poll.c	\
	mono-path.c	\
	mono-stdlib.c

libmonoutilsincludedir = $(includedir)/mono/utils

libmonoutilsinclude_HEADERS = \
	monobitset.h	\
	mono-codeman.h	\
	mono-counters.h	\
	mono-digest.h	\
	mono-logger.h	\
	mono-hash.h	\
	mono-math.h	\
	mono-path.h	\
	mono-poll.h	\
	mono-uri.h	\
	mono-stdlib.h

EXTRA_DIST = ChangeLog mono-hash.c mono-hash.h