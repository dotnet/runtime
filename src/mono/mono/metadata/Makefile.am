MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY

if HOST_WIN32
win32_sources = \
	console-win32.c \
	console-win32-internals.h \
	cominterop-win32-internals.h \
	w32file-win32.c \
	w32file-win32-internals.h \
	icall-windows.c \
	icall-windows-internals.h \
	marshal-windows.c \
	marshal-windows-internals.h \
	mono-security-windows.c \
	mono-security-windows-internals.h \
	w32mutex-win32.c \
	w32semaphore-win32.c \
	w32event-win32.c \
	w32process-win32.c \
	w32socket-win32.c \
	w32error-win32.c \
	w32subset.h

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
	console-unix.c \
	w32mutex-unix.c \
	w32semaphore-unix.c \
	w32event-unix.c \
	w32process-unix.c \
	w32process-unix-internals.h \
	w32process-unix-osx.c \
	w32process-unix-bsd.c \
	w32process-unix-haiku.c \
	w32process-unix-default.c \
	w32socket-unix.c \
	w32file-unix.c \
	w32file-unix-glob.c \
	w32file-unix-glob.h \
	w32error-unix.c

platform_sources = $(unix_sources)
endif

glib_libs = $(top_builddir)/mono/eglib/libeglib.la

if HOST_WASM
platform_sources += threadpool-worker-wasm.c
else
platform_sources += threadpool-worker-default.c
endif

if BITCODE
libmonoldflags = -no-undefined
endif

if SUPPORT_SGEN
sgen_libraries = libmonoruntimesgen.la
endif

if SUPPORT_BOEHM
boehm_libraries = libmonoruntime.la
endif

if DISABLE_ICALL_TABLES
icall_table_libraries = libmono-icall-table.la
endif

if !ENABLE_ILGEN
ilgen_libraries = libmono-ilgen.la
endif

if ENABLE_PERFTRACING
eventpipe_libs = $(top_builddir)/mono/eventpipe/libeventpipe.la
endif

BUNDLE_ZLIB_PATH=$(top_builddir)/mono/zlib/libz.la

if HAVE_STATIC_ZLIB
Z_LIBS=$(STATIC_ZLIB_PATH)
else
if HAVE_SYS_ZLIB
Z_LIBS=-lz
else
Z_LIBS=$(BUNDLE_ZLIB_PATH)
endif
endif

noinst_LTLIBRARIES = libmonoruntime-config.la $(support_libraries) $(boehm_libraries) $(sgen_libraries) $(shim_libraries)

lib_LTLIBRARIES = $(icall_table_libraries) $(ilgen_libraries)

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS) $(METADATA_CFLAGS)

#
# Make sure any prefix changes are updated in the binaries too.
#
# This won't result in many more false positives than AC_DEFINEing them
# in configure.ac.
#
mono-config-dirs.lo: Makefile

#
# This library is used to localize the usage of MONO_BINDIR etc. to just one source file, thus enabling
# ccache to work even if the value of these defines change. We need to use a convenience library since automake
# doesn't support per file cflags.
#
libmonoruntime_config_la_SOURCES = \
	mono-config-dirs.h		\
	mono-config-dirs.c
libmonoruntime_config_la_CPPFLAGS = $(AM_CPPFLAGS) -DMONO_BINDIR=\"$(bindir)/\" -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\" -DMONO_RELOC_LIBDIR=\"../$(reloc_libdir)\" @CXX_ADD_CFLAGS@

#
# Support is a separate library because it is not valid C++.
#
support_libraries = libmonoruntime-support.la

libmonoruntime_support_la_SOURCES = support.c
libmonoruntime_support_la_LDFLAGS = $(Z_LIBS)
libmonoruntime_support_la_CFLAGS = $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @ZLIB_CFLAGS@

if ENABLE_NETCORE
if HAVE_SYS_ICU
shim_libraries = libmonoruntime-shimglobalization.la

nodist_libmonoruntime_shimglobalization_la_SOURCES = \
									@ICU_SHIM_PATH@/pal_calendarData.c \
									@ICU_SHIM_PATH@/pal_casing.c \
									@ICU_SHIM_PATH@/pal_collation.c \
									@ICU_SHIM_PATH@/pal_idna.c \
									@ICU_SHIM_PATH@/pal_locale.c \
									@ICU_SHIM_PATH@/pal_localeNumberData.c \
									@ICU_SHIM_PATH@/pal_localeStringData.c \
									@ICU_SHIM_PATH@/pal_normalization.c \
									@ICU_SHIM_PATH@/pal_timeZoneInfo.c \
									@ICU_SHIM_PATH@/entrypoints.c

libmonoruntime_shimglobalization_la_CFLAGS = @ICU_CFLAGS@ -I$(top_srcdir)/../libraries/Native/Unix/System.Globalization.Native/ -I$(top_srcdir)/../libraries/Native/Unix/Common/

if STATIC_ICU
nodist_libmonoruntime_shimglobalization_la_SOURCES += @ICU_SHIM_PATH@/pal_icushim_static.c
else
nodist_libmonoruntime_shimglobalization_la_SOURCES += @ICU_SHIM_PATH@/pal_icushim.c
endif # STATIC_ICU

endif # HAVE_SYS_ICU
endif # ENABLE_NETCORE

if !ENABLE_NETCORE
culture_libraries = ../culture/libmono-culture.la
endif

#
# This library contains the icall tables if the runtime was configured with --disable-icall-tables
#
if DISABLE_ICALL_TABLES
libmono_icall_table_la_SOURCES = \
	icall-table.c
# Add CXX_ADD_CFLAGS per-library until/unless https://github.com/dotnet/corefx/pull/31342.
libmono_icall_table_la_CFLAGS = $(SGEN_DEFINES) @CXX_ADD_CFLAGS@
libmono_icall_table_la_LDFLAGS = $(libmonoldflags)
if BITCODE
if WASM
libmono_icall_table_la_LIBADD = # empty to avoid duplicate symbols when enabling dynamic linking
else
libmono_icall_table_la_LIBADD = $(glib_libs) ../utils/libmonoutils.la ../sgen/libmonosgen.la $(eventpipe_libs) libmonoruntimesgen.la
endif
endif
endif

#
# This library contains code to generate IL at runtime
#
if !ENABLE_ILGEN
libmono_ilgen_la_SOURCES = \
	method-builder-ilgen.c \
	method-builder-ilgen.h \
	method-builder-ilgen-internals.h \
	marshal-ilgen.c \
	marshal-ilgen.h	\
	sgen-mono-ilgen.c \
	sgen-mono-ilgen.h
# Add CXX_ADD_CFLAGS per-library until/unless https://github.com/dotnet/corefx/pull/31342.
libmono_ilgen_la_CFLAGS = $(SGEN_DEFINES) @CXX_ADD_CFLAGS@
libmono_ilgen_la_LDFLAGS = $(libmonoldflags)
if BITCODE
if WASM
libmono_ilgen_la_LIBADD =  # empty to avoid duplicate symbols when enabling dynamic linking
else
libmono_ilgen_la_LIBADD = $(glib_libs) ../utils/libmonoutils.la ../sgen/libmonosgen.la $(eventpipe_libs) libmonoruntimesgen.la
endif
endif
endif


CLEANFILES = mono-bundle.stamp

null_sources = \
	console-null.c

null_gc_sources = \
	null-gc.c \
	null-gc-handles.h \
	null-gc-handles.c

if !DISABLE_ICALL_TABLES
icall_tables_sources = \
	icall-table.c
endif

if ENABLE_ILGEN
ilgen_sources = \
	method-builder-ilgen.c \
	method-builder-ilgen.h \
	method-builder-ilgen-internals.h \
	marshal-ilgen.c \
	marshal-ilgen.h	\
	sgen-mono-ilgen.c \
	sgen-mono-ilgen.h
endif

common_sources = \
	$(platform_sources)	\
	appdomain.c	\
	assembly-load-context.c	\
	domain.c	\
	appdomain-icalls.h	\
	assembly.c		\
	assembly-internals.h	\
	attach.h		\
	attach.c		\
	cil-coff.h		\
	class.c			\
	class-getters.h		\
	class-init.h		\
	class-init.c		\
	class-init-internals.h	\
	class-internals.h	\
	class-inlines.h		\
	class-private-definition.h	\
	class-setup-vtable.c	\
	class-accessors.c	\
	cominterop.c		\
	cominterop.h		\
	console-io.h		\
	coree.c 		\
	coree.h 		\
	coree-internals.h \
	debug-helpers.c		\
	debug-mono-symfile.h	\
	debug-mono-symfile.c	\
	debug-mono-ppdb.h	\
	debug-mono-ppdb.c	\
	domain-internals.h	\
	environment.c		\
	environment.h		\
	icall-eventpipe.c	\
	exception.c		\
	exception.h		\
	exception-internals.h	\
	external-only.h		\
	w32file.c		\
	w32file.h		\
	w32file-internals.h \
	filewatcher.c		\
	filewatcher.h		\
	gc-internals.h		\
	icall.c			\
	icalls.h		\
	icall-decl.h		\
	icall-def.h		\
	icall-internals.h \
	icall-signatures.h \
	icall-table.h	\
	image.c			\
	image-internals.h	\
	jit-icall-reg.h		\
	jit-info.c		\
	loader.c		\
	loader-internals.h	\
	loaded-images-internals.h		\
	loaded-images.c		\
	loaded-images-global.c	\
	loaded-images-netcore.c	\
	locales.h		\
	lock-tracer.c		\
	lock-tracer.h		\
	marshal.c		\
	marshal.h		\
	marshal-internals.h \
	mempool.c		\
	mempool.h		\
	mempool-internals.h	\
	metadata.c		\
	metadata-verify.c	\
	metadata-internals.h	\
	method-builder.h 	\
	method-builder-internals.h 	\
	method-builder.c 	\
	mono-basic-block.c	\
	mono-basic-block.h	\
	mono-config-internals.h	\
	mono-config.c		\
	mono-debug.h		\
	mono-debug.c		\
	debug-internals.h	\
	mono-endian.c		\
	mono-endian.h		\
	mono-hash-internals.h	\
	mono-hash.c		\
	mono-hash.h		\
	mono-conc-hash.c		\
	mono-conc-hash.h		\
	mono-mlist.c		\
	mono-mlist.h		\
	mono-perfcounters.c	\
	mono-perfcounters.h	\
	mono-perfcounters-def.h	\
	mono-ptr-array.h	\
	mono-route.c		\
	monitor.h		\
	normalization-tables.h	\
	number-formatter.h	\
	number-ms.h             \
	object.c	\
	object-forward.h	\
	object-internals.h	\
	opcodes.c		\
	property-bag.h	\
	property-bag.c	\
	w32socket.c		\
	w32socket.h		\
	w32socket-internals.h		\
	w32process.c		\
	w32process.h		\
	w32process-internals.h		\
	profiler.c		\
	profiler-private.h	\
	profiler-legacy.h	\
	rand.h			\
	rand.c			\
	remoting.h		\
	remoting.c		\
	runtime.c		\
	runtime.h		\
	mono-security.c		\
	security.h		\
	security-core-clr.c	\
	security-core-clr.h	\
	security-manager.c	\
	security-manager.h	\
	string-icalls.c 	\
	string-icalls.h 	\
	sysmath.c		\
	tabledefs.h 		\
	threads.c		\
	threads-types.h		\
	threadpool.c	\
	threadpool.h	\
	threadpool-worker.h	\
	threadpool-io.c	\
	threadpool-io.h	\
	verify.c		\
	verify-internals.h	\
	wrapper-types.h	\
	dynamic-image-internals.h	\
	dynamic-stream.c	\
	dynamic-stream-internals.h	\
	reflection-cache.h	\
	custom-attrs-internals.h	\
	sre-internals.h	\
	reflection-internals.h	\
	file-mmap-posix.c	\
	file-mmap-windows.c	\
	file-mmap.h	\
	object-offsets.h	\
	abi-details.h	\
	class-abi-details.h	\
	metadata-cross-helpers.c	\
	seq-points-data.h	\
	seq-points-data.c	\
	handle.c	\
	handle-decl.h	\
	handle.h	\
	w32mutex.h	\
	w32semaphore.h	\
	w32event.h	\
	w32handle-namespace.h	\
	w32handle-namespace.c	\
	w32handle.h	\
	w32handle.c	\
	w32error.h	\
	reflection.c	\
	dynamic-image.c	\
	sre.c	\
	sre-encode.c	\
	sre-save.c	\
	custom-attrs.c	\
	fdhandle.h	\
	fdhandle.c	\
	callspec.h	\
	callspec.c	\
	abi.c	\
	native-library.c \
	native-library.h \
	native-library-qcall.c \
	qcall-def.h 

# These source files have compile time dependencies on GC code
gc_dependent_sources = \
	gc-stats.c	\
	gc.c		\
	monitor.c

boehm_sources = \
	boehm-gc.c

sgen_sources = \
	sgen-bridge.c		\
	sgen-bridge.h		\
	sgen-bridge-internals.h	\
	sgen-dynarray.h		\
	sgen-old-bridge.c		\
	sgen-new-bridge.c		\
	sgen-tarjan-bridge.c		\
	sgen-toggleref.c		\
	sgen-toggleref.h		\
	sgen-stw.c				\
	sgen-mono.c		\
	sgen-mono.h		\
	sgen-client-mono.h

endif # !ENABLE_MSVC_ONLY

# Per-library to workaround CoreFX/native until/unless https://github.com/dotnet/corefx/pull/31342.
#CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @CXX_ADD_CFLAGS@
CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)

if !ENABLE_MSVC_ONLY

libmonoruntime_la_SOURCES = $(common_sources) $(icall_tables_sources) $(ilgen_sources) $(gc_dependent_sources) $(null_gc_sources) $(boehm_sources)
# Add CXX_ADD_CFLAGS per-library until/unless https://github.com/dotnet/corefx/pull/31342.
libmonoruntime_la_CFLAGS = $(BOEHM_DEFINES) $(GLOBALIZATION_SHIM_DEFINES) @CXX_ADD_CFLAGS@
libmonoruntime_la_LIBADD = libmonoruntime-config.la $(culture_libraries) $(support_libraries) $(shim_libraries)

libmonoruntimesgen_la_SOURCES = $(common_sources) $(icall_tables_sources) $(ilgen_sources) $(gc_dependent_sources) $(sgen_sources)
# Add CXX_ADD_CFLAGS per-library until/unless https://github.com/dotnet/corefx/pull/31342.
libmonoruntimesgen_la_CFLAGS = $(SGEN_DEFINES) $(GLOBALIZATION_SHIM_DEFINES) @CXX_ADD_CFLAGS@
libmonoruntimesgen_la_LIBADD = libmonoruntime-config.la $(culture_libraries) $(support_libraries) $(shim_libraries)

endif # !ENABLE_MSVC_ONLY

libmonoruntimeincludedir = $(includedir)/mono-$(API_VER)/mono/metadata

# This list is sorted for easier searching.
# These are public headers.
# They may not use G_BEGIN_DECLS, guint, glib.h, etc.
# debug-mono-symfile.h is an exception. It is only semi-public.
libmonoruntimeinclude_HEADERS = \
	appdomain.h		\
	assembly.h		\
	attrdefs.h		\
	blob.h			\
	class.h			\
	debug-helpers.h		\
	debug-mono-symfile.h	\
	environment.h		\
	exception.h		\
	image.h			\
	loader.h		\
	metadata.h		\
	mono-config.h		\
	mono-debug.h		\
	mono-gc.h		\
	mono-private-unstable.h	\
	object.h		\
	object-forward.h	\
	opcodes.h		\
	profiler.h		\
	profiler-events.h	\
	reflection.h		\
	row-indexes.h		\
	sgen-bridge.h		\
	threads.h		\
	tokentype.h		\
	verify.h

if !ENABLE_MSVC_ONLY

EXTRA_DIST = $(null_sources) \
		external-only.c \
		threadpool-io-poll.c threadpool-io-epoll.c threadpool-io-kqueue.c

endif # !ENABLE_MSVC_ONLY

if ENABLE_MSVC_ONLY

all-local:

	make -C $(top_srcdir)/msvc libmonoruntime

clean-local:

	make -C $(top_srcdir)/msvc clean-libmonoruntime

endif # ENABLE_MSVC_ONLY
