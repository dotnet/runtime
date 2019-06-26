include $(top_srcdir)/mk/common.mk

AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

if HOST_WIN32
export HOST_CC
endif

glib_libs = $(top_builddir)/mono/eglib/libeglib.la

if SUPPORT_SGEN
metadata_lib=$(top_builddir)/mono/metadata/libmonoruntimesgen.la
gc_lib=$(top_builddir)/mono/sgen/libmonosgen.la
else
metadata_lib=$(top_builddir)/mono/metadata/libmonoruntime.la
gc_lib=$(LIBGC_STATIC_LIBS)
endif

runtime_lib=	\
	$(metadata_lib) \
	$(gc_lib)	\
	$(top_builddir)/mono/utils/libmonoutils.la \
	$(glib_libs)

noinst_LIBRARIES = libmonodis.a

if DISABLE_EXECUTABLES
bin_PROGRAMS =
else
if DISABLE_LIBRARIES
bin_PROGRAMS =
else
bin_PROGRAMS = monodis
noinst_LIBRARIES += libmonodismain.a
endif
endif

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)
libmonodis_a_CFLAGS = $(CFLAGS) @CXX_ADD_CFLAGS@
libmonodismain_a_CFLAGS = $(CFLAGS) @CXX_ADD_CFLAGS@

libmonodis_a_SOURCES =	\
	get.c		\
	get.h		\
	dis-cil.c	\
	dis-cil.h	\
	util.c		\
	util.h

libmonodismain_a_SOURCES =	\
	dump.c		\
	dump.h		\
	main.c		\
	meta.h		\
	declsec.c	\
	declsec.h

monodis_SOURCES =

monodis_LDADD = 			\
	libmonodismain_a-dump.$(OBJEXT)	\
	libmonodismain_a-main.$(OBJEXT)	\
	libmonodismain_a-declsec.$(OBJEXT) \
	libmonodis.a			\
	$(runtime_lib)			\
	$(LLVM_LIBS)			\
	$(LLVM_LDFLAGS)			\
	$(glib_libs)

if HOST_DARWIN
monodis_LDFLAGS=-framework CoreFoundation -framework Foundation
endif

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)
