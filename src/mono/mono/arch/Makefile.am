SUBDIRS = $(arch_target)
DIST_SUBDIRS = x86

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

noinst_LTLIBRARIES = libmonoarch.la

libmonoarch_la_SOURCES = unknown.c

if X86
libmonoarch_la_LIBADD = $(arch_target)/libmonoarch-$(arch_target).la
endif

EXTRA_DIST = ChangeLog

