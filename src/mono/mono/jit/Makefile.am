INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

if X86
bin_PROGRAMS = mono
lib_LTLIBRARIES = libmono.la

libmono_la_LIBADD = 	\
	../metadata/libmonoruntime.la

endif

libmono_la_SOURCES =		\
	win32-exception.c	\
	win32-exception.h	\
	codegen.h		\
	codegen-x86.c		\
	emit-x86.c		\
	regset.c		\
	debug-private.h		\
	debug.c			\
	debug-stabs.c		\
	debug-dwarf2.c		\
	debug-dwarf2-plus.c	\
	jit.c			\
	trampoline.c		\
	exception.c		\
	message.c		\
	linear-scan.c		\
	helpers.h		\
	helpers.c

libmonoincludedir = $(includedir)/mono/jit
libmonoinclude_HEADERS = \
	jit.h			\
	debug.h			\
	regset.h

mono_SOURCES = mono.c

#mono_LDFLAGS=-static
mono_LDADD =				\
	libmono.la			\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

codegen-x86.c codegen.h: x86.brg
	../monoburg/monoburg $(srcdir)/x86.brg -d codegen.h -s codegen-x86.c

BUILT_SOURCES = codegen-x86.c codegen.h

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = x86.brg

