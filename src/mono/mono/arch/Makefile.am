MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

DIST_SUBDIRS = x86 ppc sparc arm arm64 s390x amd64 mips riscv

AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

if ARM
# arm needs to build some stuff even in JIT mode
SUBDIRS = arm
endif

if RISCV
SUBDIRS = riscv
endif
