AUTOMAKE_OPTIONS = foreign

#if INCLUDED_LIBGC
#libgc_SUBDIRS = libgc
#else
libgc_SUBDIRS =
#endif

SUBDIRS = $(libgc_SUBDIRS) mono doc docs runtime scripts man data

#EXTRA_SUBDIRS = libgc

EXTRA_DIST= mono.pc.in mono.spec.in

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA= mono.pc
DISTCLEANFILES= mono.pc
