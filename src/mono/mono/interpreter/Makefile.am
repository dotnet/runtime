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

bin_PROGRAMS = mint

lib_LTLIBRARIES = libmint.la

mint_SOURCES =		\
	main.c

mint_LDADD =		\
	libmint.la

libmint_la_SOURCES =	\
	hacks.h		\
	interp.h	\
	interp.c	

libmintincludedir = $(includedir)/mono/interpreter

libmintinclude_HEADERS = 	\
	embed.h

libmint_la_LIBADD =				\
	../arch/libmonoarch.la		\
	../metadata/libmonoruntime.la	\
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	../os/libmonoos.la		\
	$(LIBGC_LIBS)			\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	$(ICU_LIBS)			\
	-lm

man_MANS = mint.1

EXTRA_DIST = $(man_MANS)
