INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

liboldmono_la_LDFLAGS=-Wl,-version-script=$(srcdir)/ldscript

if X86
if PLATFORM_WIN32
export HOST_CC
endif

bin_PROGRAMS = oldmono
lib_LTLIBRARIES = liboldmono.la

liboldmono_la_LIBADD = 	\
	../metadata/libmonoruntime.la	\
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	../os/libmonoos.la

endif

jitsources =		\
	win32-exception.c	\
	win32-exception.h	\
	codegen.h		\
	codegen-x86.c		\
	emit-x86.c		\
	regset.c		\
	debug-jit.h		\
	debug-jit.c		\
	jit.c			\
	trampoline.c		\
	exception.c		\
	linear-scan.c		\
	helpers.h		\
	helpers.c

liboldmono_la_SOURCES = $(jitsources)

liboldmonoincludedir = $(includedir)/mono/jit
liboldmonoinclude_HEADERS = \
	jit.h			\
	regset.h

oldmono_SOURCES = mono.c $(jitsources)

#mono_LDFLAGS=-static
oldmono_LDADD =				\
	../metadata/libmonoruntime.la	\
	../metadata/libmetadata.la	\
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	../os/libmonoos.la		\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

oldmono_CFLAGS = $(AM_CFLAGS)

codegen-x86.c codegen.h: x86.brg
	../monoburg/monoburg $(srcdir)/x86.brg -d codegen.h -s codegen-x86.c

BUILT_SOURCES = codegen-x86.c codegen.h

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = x86.brg ldscript

