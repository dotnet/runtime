INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS) -I$(top_srcdir)

bin_PROGRAMS = mono-int

mono_int_SOURCES =	\
	interp.c	\
	hacks.h	\
	interp.h

mono_int_LDADD =	\
	../metadata/libmetadata.a	\
	../cli/libmonocli.a		\
	$(GMODULE_LIBS)			\
	$(GLIB_LIBS)			\
	-lffi

