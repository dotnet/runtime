MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY

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

endif # !ENABLE_MSVC_ONLY

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)

if !ENABLE_MSVC_ONLY

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

BUNDLE_ZLIB_PATH=$(top_builddir)/mono/zlib/libz.la

if HAVE_SYS_ZLIB
Z_LIBS= -lz
else
if HAVE_STATIC_ZLIB
Z_LIBS=$(STATIC_ZLIB_PATH)
else
Z_LIBS=$(BUNDLE_ZLIB_PATH)
endif
endif

monodis_LDADD = 			\
	libmonodismain_a-dump.$(OBJEXT)	\
	libmonodismain_a-main.$(OBJEXT)	\
	libmonodismain_a-declsec.$(OBJEXT) \
	libmonodis.a			\
	$(runtime_lib)			\
	$(LLVM_LIBS)			\
	$(LLVM_LDFLAGS)			\
	$(glib_libs)			\
	$(Z_LIBS)

if HOST_DARWIN
monodis_LDFLAGS=-framework CoreFoundation -framework Foundation
endif

man_MANS = monodis.1

EXTRA_DIST = $(man_MANS)

endif # !ENABLE_MSVC_ONLY

if ENABLE_MSVC_ONLY

if SUPPORT_SGEN
mono_bin_suffix = sgen
else
mono_bin_suffix =
endif

all-local:

	make -C $(top_srcdir)/msvc monodis
	cp -f $(mono_msvc_build_bin_dir)/monodis-$(mono_bin_suffix)$(EXEEXT) ./monodis$(EXEEXT)

clean-local:

	make -C $(top_srcdir)/msvc clean-monodis
	rm ./monodis$(EXEEXT)

endif # ENABLE_MSVC_ONLY
