INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

if HOST_WIN32
export HOST_CC
endif

if JIT_SUPPORTED
runtime_lib=../mini/libmono-$(API_VER).la
else
runtime_lib=../interpreter/libmint.la
endif

bin_PROGRAMS = monodis

noinst_LIBRARIES = libmonodis.a

libmonodis_a_SOURCES =	\
	get.c		\
	get.h		\
	dis-cil.c	\
	dis-cil.h	\
	util.c		\
	util.h

monodis_SOURCES =	\
	dump.c		\
	dump.h		\
	main.c		\
	meta.h		\
	declsec.c	\
	declsec.h

monodis_LDADD = 			\
	libmonodis.a			\
	$(runtime_lib)			\
	$(LLVM_LIBS)			\
	$(GLIB_LIBS) 

if PLATFORM_DARWIN
monodis_LDFLAGS=-framework CoreFoundation
endif

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)
