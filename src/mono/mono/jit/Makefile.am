INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)			

bin_PROGRAMS = mono

mono_SOURCES =			\
	codegen.h		\
	codegen-x86.c		\
	emit-x86.c		\
	jit.h			\
	regset.h		\
	regset.c		\
	mempool.h		\
	mempool.c		\
	jit.c

mono_LDADD =				\
	../metadata/libmetadata.a	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

codegen-x86.c codegen.h: x86.brg
	../monoburg/monoburg x86.brg -d codegen.h > codegen-x86.c

BUILT_SOURCES = codegen-x86.c codegen.h

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = x86.brg

