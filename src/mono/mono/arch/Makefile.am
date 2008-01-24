DIST_SUBDIRS = x86 ppc sparc arm s390 s390x alpha hppa amd64 ia64 mips

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

if INTERP_SUPPORTED
SUBDIRS = $(arch_target)
else
if ARM
# arm needs to build some stuff even in JIT mode
SUBDIRS = $(arch_target)
endif
endif

if INTERP_SUPPORTED

noinst_LTLIBRARIES = libmonoarch.la

libmonoarch_la_SOURCES = unknown.c

libmonoarch_la_LIBADD = $(arch_target)/libmonoarch-$(arch_target).la

endif

EXTRA_DIST = ChangeLog

