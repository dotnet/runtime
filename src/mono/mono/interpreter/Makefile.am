INCLUDES = 				\
	-I$(top_srcdir)			\
	-I$(top_srcdir)/libffi/include	\
	$(GLIB_CFLAGS)			\
	$(GMODULE_CFLAGS)

bin_PROGRAMS = mono-int

mono_int_SOURCES =	\
	hacks.h		\
	interp.h	\
	interp.c

mono_int_LDADD =			\
	../metadata/libmetadata.a	\
	../cli/libmonocli.a		\
	../../libffi/.libs/libffi.a	\
	$(GMODULE_LIBS)			\
	$(GLIB_LIBS)			\
	-lm
