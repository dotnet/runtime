SUBDIRS=atomic_ops

AM_CFLAGS=@PICFLAG@

include_HEADERS=atomic_ops.h atomic_ops_stack.h atomic_ops_malloc.h
lib_LIBRARIES = libatomic_ops.a libatomic_ops_gpl.a
if NEED_ASM
libatomic_ops_a_SOURCES = atomic_ops.c atomic_ops_sysdeps.S
else
libatomic_ops_a_SOURCES = atomic_ops.c
endif

libatomic_ops_gpl_a_SOURCES = atomic_ops_stack.c atomic_ops_malloc.c

EXTRA_DIST=Makefile.msft

