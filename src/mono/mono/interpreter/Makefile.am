INCLUDES = 				\
	-I$(top_srcdir)			\
	$(LIBGC_CFLAGS)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

if PLATFORM_WIN32
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
AM_CFLAGS = -O
endif

if INTERP_SUPPORTED
bin_PROGRAMS = mint

lib_LTLIBRARIES = libmint.la
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

libmintincludedir = $(includedir)/mono/interpreter

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
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	../os/libmonoos.la		\
	$(libgc_libs)			\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	$(ICU_LIBS)			\
	-lm

man_MANS = mint.1

EXTRA_DIST = $(man_MANS)
