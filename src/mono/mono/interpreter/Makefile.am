INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

bin_PROGRAMS = mono-int

mono_int_SOURCES =	\
	interp.c	\
	interp.h

mono_int_LDADD =	\
	../metadata/libmetadata.a	\
	../cli/libmonocli.a		\
	../dis/libmonodis.a		\
	$(GLIB_LIBS)

