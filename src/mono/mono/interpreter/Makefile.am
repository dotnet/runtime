INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

bin_PROGRAMS = mono-vm

mono_vm_SOURCES =	\
	interp.c	\
	interp.h

mono_vm_LDADD =	\
	../metadata/libmetadata.a	\
	$(GLIB_LIBS)

