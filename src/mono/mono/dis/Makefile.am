AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

if HOST_WIN32
export HOST_CC
endif

if SUPPORT_SGEN
metadata_lib=$(top_builddir)/mono/metadata/libmonoruntimesgen-static.la
gc_lib=$(top_builddir)/mono/sgen/libmonosgen-static.la
else
metadata_lib=$(top_builddir)/mono/metadata/libmonoruntime-static.la
gc_lib=$(LIBGC_STATIC_LIBS)
endif

runtime_lib=	\
	$(metadata_lib) \
	$(gc_lib)	\
	$(top_builddir)/mono/utils/libmonoutils.la \
	$(GLIB_LIBS) $(LIBICONV)

if DISABLE_EXECUTABLES
bin_PROGRAMS =
else
if DISABLE_LIBRARIES
bin_PROGRAMS =
else
bin_PROGRAMS = monodis
endif
endif

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
	$(LLVM_LDFLAGS)			\
	$(GLIB_LIBS)			\
	$(LIBICONV)

if HOST_DARWIN
monodis_LDFLAGS=-framework CoreFoundation -framework Foundation
endif

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)
