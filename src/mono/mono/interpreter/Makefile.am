INCLUDES = 				\
	-I$(top_srcdir)			\
	-I$(top_srcdir)/libffi/include	\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)			

bin_PROGRAMS = mint

mint_SOURCES =	\
	hacks.h		\
	interp.h	\
	interp.c	\
	icall.c

mint_LDADD =				\
	../metadata/libmetadata.a	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	../../libffi/libffi.a
	-lm
