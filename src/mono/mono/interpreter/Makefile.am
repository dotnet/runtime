INCLUDES = 				\
	-I$(top_srcdir)			\
	-I$(top_srcdir)/libffi/include	\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)			

bin_PROGRAMS = mono-int

mono_int_SOURCES =	\
	hacks.h		\
	interp.h	\
	interp.c	\
	icall.c

mono_int_LDADD =			\
	../metadata/libmetadata.a	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	../../libffi/libffi.a
	-lm
