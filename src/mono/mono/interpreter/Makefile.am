INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)			

bin_PROGRAMS = mint

mint_SOURCES =		\
	hacks.h		\
	interp.h	\
	interp.c	

mint_LDADD =				\
	../arch/libmonoarch.la	\
	../metadata/libmetadata.a	\
	../io-layer/libwapi.a	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

man_MANS = mint.1

EXTRA_DIST = $(man_MANS)
