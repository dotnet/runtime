INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)			

if X86
bin_PROGRAMS = mono
lib_LIBRARIES = libmono.a
endif

libmono_a_SOURCES =			\
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
	debug-dwarf2-plus.c	\
	jit.c			\
	trampoline.c		\
	delegate.c		\
	exception.c		\
	invoke.c		\
	message.h		\
	message.c

mono_SOURCES = mono.c

mono_LDADD =				\
	libmono.a			\
	../metadata/libmetadata.a	\
	../io-layer/libwapi.a		\
	../utils/libmonoutils.a 	\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

codegen-x86.c codegen.h: x86.brg
	../monoburg/monoburg $(srcdir)/x86.brg -d codegen.h > codegen-x86.c

BUILT_SOURCES = codegen-x86.c codegen.h

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = x86.brg

