INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)			

if X86
bin_PROGRAMS = mono
endif

mono_SOURCES =			\
	codegen.h		\
	codegen-x86.c		\
	emit-x86.c		\
	jit.h			\
	regset.h		\
	regset.c		\
	debug.h			\
	debug-private.h		\
	debug.c			\
	debug-stabs.c		\
	debug-dwarf2.c		\
	jit.c

mono_LDADD =				\
	../metadata/libmetadata.a	\
	../io-layer/libwapi.a		\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

codegen-x86.c codegen.h: x86.brg
	../monoburg/monoburg $(srcdir)/x86.brg -d codegen.h > codegen-x86.c

BUILT_SOURCES = codegen-x86.c codegen.h

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = x86.brg

