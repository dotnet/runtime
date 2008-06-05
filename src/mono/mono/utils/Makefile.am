noinst_LTLIBRARIES = libmonoutils.la

INCLUDES = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CFLAGS) $(GLIB_CFLAGS)

if ENABLE_DTRACE

BUILT_SOURCES = mono-dtrace.h

mono-dtrace.h: $(top_srcdir)/data/mono.d
	$(DTRACE) $(DTRACEFLAGS) -h -s $(top_srcdir)/data/mono.d -o $@

endif

if EGLIB_BUILD 
hash_sources = mono-ehash.c
else
hash_sources = mono-hash.c
endif

libmonoutils_la_SOURCES = \
	$(hash_sources)	\
	mono-md5.c	\
	mono-sha1.c	\
	mono-logger.c	\
	mono-codeman.c	\
	dlmalloc.h      \
	dlmalloc.c      \
	mono-counters.c	\
	mono-compiler.h	\
	mono-dl.c	\
	mono-dl.h	\
	mono-internal-hash.c	\
	mono-internal-hash.h	\
	mono-io-portability.c 	\
	mono-io-portability.h	\
	monobitset.c	\
	mono-math.c  \
	mono-mmap.c  \
	mono-mmap.h  \
	mono-time.c  \
	mono-time.h  \
	strtod.h	\
	strtod.c	\
	strenc.h	\
	strenc.c	\
	mono-uri.c	\
	mono-poll.c	\
	mono-path.c	\
	mono-stdlib.c \
	mono-property-hash.h \
	mono-property-hash.c \
	mono-value-hash.h \
	mono-value-hash.c \
	dtrace.h

libmonoutilsincludedir = $(includedir)/mono-$(API_VER)/mono/utils

libmonoutilsinclude_HEADERS = \
	monobitset.h	\
	mono-codeman.h	\
	mono-counters.h	\
	mono-digest.h	\
	mono-logger.h	\
	mono-hash.h	\
	mono-math.h	\
	mono-membar.h	\
	mono-path.h	\
	mono-poll.h	\
	mono-uri.h	\
	mono-stdlib.h

EXTRA_DIST = ChangeLog mono-hash.c mono-hash.h mono-ehash.c
