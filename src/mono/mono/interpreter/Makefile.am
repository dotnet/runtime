INCLUDES = 				\
	-I$(top_srcdir)			\
	$(LIBGC_CPPFLAGS)		\
	$(GLIB_CFLAGS)

if HOST_WIN32
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
AM_CFLAGS = -O
endif

if INTERP_SUPPORTED
bin_PROGRAMS = mint

lib_LTLIBRARIES = libmint.la

man_MANS = mint.1
endif

mint_SOURCES =		\
	main.c

mint_LDADD =		\
	libmint.la

libmint_la_SOURCES =	\
	hacks.h		\
	interp.h	\
	interp.c	\
	mintops.h	\
	mintops.def	\
	mintops.c	\
	transform.c

libmintincludedir = $(includedir)/mono-$(API_VER)/mono/interpreter

libmintinclude_HEADERS = 	\
	embed.h

# This is needed for automake dependency generation
if INCLUDED_LIBGC
libgc_libs=../../libgc/libmonogc.la
else
libgc_libs=$(LIBGC_LIBS)
endif

libmint_la_LIBADD =				\
	../arch/libmonoarch.la		\
	../metadata/libmonoruntime.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(libgc_libs)			\
	$(GLIB_LIBS)			\
	$(LIBICONV)			\
	$(ICU_LIBS)			\
	-lm

EXTRA_DIST = $(man_MANS)
