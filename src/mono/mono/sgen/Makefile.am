AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS)

if SUPPORT_SGEN
if DISABLE_EXECUTABLES
shared_libraries = libmonosgen.la
else
if SHARED_MONO
shared_libraries = libmonosgen.la
endif
endif
libraries = $(shared_libraries) libmonosgen-static.la
endif

if DISABLE_EXECUTABLES
noinst_LTLIBRARIES = $(shared_libraries)
else
noinst_LTLIBRARIES = $(libraries)
endif

monosgen_sources = \
	gc-internal-agnostic.h \
	sgen-alloc.c \
	sgen-archdep.h \
	sgen-cardtable.c \
	sgen-cardtable.h \
	sgen-client.h \
	sgen-conf.h \
	sgen-copy-object.h \
	sgen-debug.c \
	sgen-descriptor.c \
	sgen-descriptor.h \
	sgen-fin-weak-hash.c \
	sgen-gc.c \
	sgen-gc.h \
	sgen-gchandles.c \
	sgen-gray.c \
	sgen-gray.h \
	sgen-hash-table.c \
	sgen-hash-table.h \
	sgen-internal.c \
	sgen-layout-stats.c \
	sgen-layout-stats.h \
	sgen-los.c \
	sgen-major-copy-object.h \
	sgen-marksweep-drain-gray-stack.h \
	sgen-marksweep.c \
	sgen-memory-governor.c \
	sgen-memory-governor.h \
	sgen-minor-copy-object.h \
	sgen-minor-scan-object.h \
	sgen-nursery-allocator.c \
	sgen-pinning-stats.c \
	sgen-pinning.c \
	sgen-pinning.h \
	sgen-pointer-queue.c \
	sgen-pointer-queue.h \
	sgen-array-list.h \
	sgen-array-list.c \
	sgen-protocol-def.h \
	sgen-protocol.c \
	sgen-protocol.h \
	sgen-qsort.c \
	sgen-qsort.h \
	sgen-scan-object.h \
	sgen-simple-nursery.c \
	sgen-split-nursery.c \
	sgen-tagged-pointer.h \
	sgen-thread-pool.c \
	sgen-thread-pool.h \
	sgen-workers.c \
	sgen-workers.h

libmonosgen_la_SOURCES = $(monosgen_sources)
libmonosgen_la_CFLAGS = $(SGEN_DEFINES)

libmonosgen_static_la_SOURCES = $(libmonosgen_la_SOURCES)
libmonosgen_static_la_CFLAGS = $(SGEN_DEFINES)
libmonosgen_static_la_LDFLAGS = -static
