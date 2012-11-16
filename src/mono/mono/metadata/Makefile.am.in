if HOST_WIN32
win32_sources = \
	console-win32.c

platform_sources = $(win32_sources)

# Use -m here. This will use / as directory separator (C:/WINNT).
# The files that use MONO_ASSEMBLIES and/or MONO_CFG_DIR replace the
# / by \ if running under WIN32.
if CROSS_COMPILING
assembliesdir = ${libdir}
confdir = ${sysconfdir}
else
assembliesdir = `cygpath -m "${libdir}"`
confdir = `cygpath -m "${sysconfdir}"`
endif
export HOST_CC
# The mingw math.h has "extern inline" functions that dont appear in libs, so
# optimisation is required to actually inline them
AM_CFLAGS = -O
else

assembliesdir = $(exec_prefix)/lib
confdir = $(sysconfdir)
unix_sources = \
	console-unix.c

platform_sources = $(unix_sources)
endif

if SHARED_MONO
if SUPPORT_BOEHM
bin_PROGRAMS = pedump
endif
endif

#
# libtool is not capable of creating static/shared versions of the same
# convenience lib, so we have to do it ourselves
#
if SUPPORT_SGEN
if DISABLE_EXECUTABLES
shared_sgen_libraries = libmonoruntimesgen.la 
else
if SHARED_MONO
shared_sgen_libraries = libmonoruntimesgen.la 
endif
endif
sgen_libraries = $(shared_sgen_libraries) libmonoruntimesgen-static.la 
endif

if SUPPORT_BOEHM
if DISABLE_EXECUTABLES
shared_boehm_libraries = libmonoruntime.la
else
if SHARED_MONO
shared_boehm_libraries = libmonoruntime.la
endif
endif
boehm_libraries = $(shared_boehm_libraries) libmonoruntime-static.la
endif

if MOONLIGHT
moonlight_libraries = libmonoruntimemoon.la
moon-do-build: libmonoruntime-static.la libmonoruntimemoon.la
moon-do-clean:
	-test -z "libmonoruntime-static.la" || rm -f libmonoruntime-static.la
	-test -z "libmonoruntimemoon.la" || rm -f libmonoruntimemoon.la
endif

if DISABLE_EXECUTABLES
noinst_LTLIBRARIES = $(shared_sgen_libraries) $(shared_boehm_libraries)
else
noinst_LTLIBRARIES = $(boehm_libraries) $(sgen_libraries) $(moonlight_libraries)
endif

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\"

#
# Make sure any prefix changes are updated in the binaries too.
#
#  assembly.c uses MONO_ASSEMBLIES
#  mono-config.c uses MONO_CFG_DIR
#
# This won't result in many more false positives than AC_DEFINEing them
# in configure.in.
#
assembly.lo mono-config.lo: Makefile

CLEANFILES = mono-bundle.stamp

libmonoruntime_static_la_LIBADD = $(bundle_obj) $(libmonoruntime_la_LIBADD)

null_sources = \
	console-null.c

common_sources = \
	$(platform_sources)	\
	assembly.c		\
	attach.h		\
	attach.c		\
	char-conversions.h	\
	cil-coff.h		\
	class.c			\
	class-internals.h	\
	cominterop.c		\
	cominterop.h		\
	console-io.h		\
	coree.c 		\
	coree.h 		\
	culture-info.h  	\
	culture-info-tables.h	\
	debug-helpers.c		\
	debug-mono-symfile.h	\
	debug-mono-symfile.c	\
	decimal.c		\
	decimal.h		\
	domain-internals.h	\
	environment.c		\
	environment.h		\
	exception.c		\
	exception.h		\
	file-io.c		\
	file-io.h		\
	filewatcher.c		\
	filewatcher.h		\
	gc-internal.h		\
	icall.c			\
	icall-def.h		\
	image.c			\
	loader.c		\
	locales.c		\
	locales.h		\
	lock-tracer.c		\
	lock-tracer.h		\
	marshal.c		\
	marshal.h		\
	mempool.c		\
	mempool.h		\
	mempool-internals.h	\
	metadata.c		\
	metadata-verify.c	\
	metadata-internals.h	\
	method-builder.h 	\
	method-builder.c 	\
	mono-basic-block.c	\
	mono-basic-block.h	\
	mono-config.c		\
	mono-cq.c		\
	mono-cq.h		\
	mono-debug.h		\
	mono-debug.c		\
	mono-debug-debugger.h	\
	mono-debug-debugger.c	\
	mono-endian.c		\
	mono-endian.h		\
	mono-hash.h		\
	mono-mlist.c		\
	mono-mlist.h		\
	mono-perfcounters.c	\
	mono-perfcounters.h	\
	mono-perfcounters-def.h	\
	mono-ptr-array.h	\
	mono-wsq.c		\
	mono-wsq.h		\
	monitor.h		\
	nacl-stub.c		\
	normalization-tables.h	\
	null-gc.c		\
	number-formatter.h	\
	object-internals.h	\
	opcodes.c		\
	socket-io.c		\
	socket-io.h		\
	process.c		\
	process.h		\
	profiler.c		\
	profiler-private.h	\
	rand.h			\
	rand.c			\
	runtime.c		\
	security.c		\
	security.h		\
	security-core-clr.c	\
	security-core-clr.h	\
	security-manager.c	\
	security-manager.h	\
	string-icalls.c 	\
	string-icalls.h 	\
	sysmath.h		\
	sysmath.c		\
	tabledefs.h 		\
	threads.c		\
	threads-types.h		\
	threadpool.c		\
	threadpool.h		\
	threadpool-internals.h	\
	verify.c		\
	verify-internals.h	\
	wrapper-types.h

# These source files have compile time dependencies on GC code
gc_dependent_sources = \
	appdomain.c	\
	domain.c	\
	gc.c		\
	monitor.c	\
	mono-hash.c	\
	object.c	\
	reflection.c

boehm_sources = \
	boehm-gc.c

sgen_sources = \
	sgen-os-posix.c		\
	sgen-os-mach.c		\
	sgen-os-win32.c		\
	sgen-gc.c		\
	sgen-internal.c		\
	sgen-marksweep.c	\
	sgen-marksweep-fixed.c	\
	sgen-marksweep-par.c	\
	sgen-marksweep-fixed-par.c	\
	sgen-los.c		\
	sgen-protocol.c \
	sgen-bridge.c		\
	sgen-bridge.h		\
	sgen-toggleref.c		\
	sgen-toggleref.h		\
	sgen-gc.h		\
	sgen-conf.h		\
	sgen-archdep.h		\
	sgen-cardtable.c	\
	sgen-cardtable.h	\
	sgen-pinning.c	\
	sgen-pinning.h	\
	sgen-pinning-stats.c	\
	sgen-workers.c	\
	sgen-workers.h	\
	sgen-gray.c	\
	sgen-gray.h	\
	sgen-major-copy-object.h \
	sgen-minor-copy-object.h \
	sgen-copy-object.h \
	sgen-major-scan-object.h \
	sgen-minor-scan-object.h \
	sgen-protocol.h		\
	sgen-scan-object.h	\
	sgen-nursery-allocator.c	\
	sgen-hash-table.c	\
	sgen-hash-table.h	\
	sgen-descriptor.c		\
	sgen-descriptor.h		\
	sgen-alloc.c		\
	sgen-debug.c		\
	sgen-simple-nursery.c	\
	sgen-split-nursery.c	\
	sgen-memory-governor.c	\
	sgen-memory-governor.h	\
	sgen-stw.c				\
	sgen-fin-weak-hash.c	\
	sgen-layout-stats.c	\
	sgen-layout-stats.h

libmonoruntime_la_SOURCES = $(common_sources) $(gc_dependent_sources) $(boehm_sources)
libmonoruntime_la_CFLAGS = $(BOEHM_DEFINES)

if MOONLIGHT
libmonoruntimemoon_la_SOURCES = $(libmonoruntime_la_SOURCES)
if MOONLIGHT_BOEHM
libmonoruntimemoon_la_CFLAGS = $(MOONLIGHT_DEFINES) $(BOEHM_DEFINES)
else
libmonoruntimemoon_la_CFLAGS = $(MOONLIGHT_DEFINES) $(SGEN_DEFINES)
endif
endif

libmonoruntimesgen_la_SOURCES = $(common_sources) $(gc_dependent_sources) $(sgen_sources)
libmonoruntimesgen_la_CFLAGS = $(SGEN_DEFINES)

libmonoruntime_static_la_SOURCES = $(libmonoruntime_la_SOURCES)
libmonoruntime_static_la_LDFLAGS = -static
libmonoruntime_static_la_CFLAGS = $(BOEHM_DEFINES)

libmonoruntimesgen_static_la_SOURCES = $(libmonoruntimesgen_la_SOURCES)
libmonoruntimesgen_static_la_LDFLAGS = -static
libmonoruntimesgen_static_la_CFLAGS = $(SGEN_DEFINES)

libmonoruntimeincludedir = $(includedir)/mono-$(API_VER)/mono/metadata

libmonoruntimeinclude_HEADERS = \
	assembly.h		\
	attrdefs.h		\
	appdomain.h		\
	blob.h			\
	class.h			\
	debug-helpers.h		\
	debug-mono-symfile.h	\
	threads.h		\
	environment.h		\
	exception.h		\
	image.h			\
	loader.h		\
	metadata.h		\
	mono-config.h		\
	mono-debug.h		\
	mono-gc.h		\
	sgen-bridge.h		\
	object.h		\
	opcodes.h		\
	profiler.h		\
	reflection.h		\
	row-indexes.h		\
	tokentype.h		\
	verify.h		

if DTRACE_G_REQUIRED

PEDUMP_DTRACE_OBJECT = pedump-dtrace.$(OBJEXT)

pedump-dtrace.$(OBJEXT): $(top_srcdir)/data/mono.d libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la
	DTRACE="$(DTRACE)" DTRACEFLAGS="$(DTRACEFLAGS)" AR="$(AR)" $(SHELL) $(top_srcdir)/data/dtrace-prelink.sh \
	--pic pedump-dtrace.$(OBJEXT) $(top_srcdir)/data/mono.d libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la

else
PEDUMP_DTRACE_OBJECT = 
endif

if SHARED_MONO
if SUPPORT_BOEHM
pedump_SOURCES =		\
	pedump.c

pedump_LDADD = libmonoruntime.la ../io-layer/libwapi.la ../utils/libmonoutils.la \
	$(LIBGC_LIBS) $(GLIB_LIBS) -lm $(LIBICONV) $(PEDUMP_DTRACE_OBJECT)

if PLATFORM_DARWIN
pedump_LDFLAGS=-framework CoreFoundation
endif
endif
endif

EXTRA_DIST = make-bundle.pl sample-bundle $(win32_sources) $(unix_sources) $(null_sources) runtime.h \
		tpool-poll.c tpool-epoll.c tpool-kqueue.c Makefile.am.in

