DIST_SUBDIRS = x86 ppc sparc arm arm64 s390x amd64 mips

AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

if ARM
# arm needs to build some stuff even in JIT mode
SUBDIRS = $(arch_target)
endif


