SUBDIRS = $(arch_target)
DIST_SUBDIRS = x86 ppc sparc arm s390 alpha hppa

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

noinst_LTLIBRARIES = libmonoarch.la

libmonoarch_la_SOURCES = unknown.c

libmonoarch_la_LIBADD = $(arch_target)/libmonoarch-$(arch_target).la

EXTRA_DIST = ChangeLog

