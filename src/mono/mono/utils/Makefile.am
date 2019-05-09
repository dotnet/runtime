include $(top_srcdir)/mk/common.mk

noinst_LTLIBRARIES = \
	libmonomath.la \
	libmonoutils.la

if ENABLE_LLVM
	if INTERNAL_LLVM 
		llvm_config=$(monodir)/llvm/usr/bin/llvm-config
	else
		llvm_config=$(EXTERNAL_LLVM_CONFIG)
	endif

	llvm_api_version_from_config=$(shell $(llvm_config) --mono-api-version 2>/dev/null)
	llvm_api_version=$(shell if [  -z "$(llvm_api_version_from_config)" ]; then echo "0"; else echo "$(llvm_api_version_from_config)"; fi)
	LLVM_CFLAGS=-DLLVM_API_VERSION=$(llvm_api_version)
endif

if ENABLE_LLVM
if INTERNAL_LLVM
llvm_config=$(monodir)/llvm/usr/bin/llvm-config
else
llvm_config=$(EXTERNAL_LLVM_CONFIG)
endif

llvm_api_version_from_config=$(shell $(llvm_config) --mono-api-version 2>/dev/null)
llvm_api_version=$(shell if [  -z "$(llvm_api_version_from_config)" ]; then echo "0"; else echo "$(llvm_api_version_from_config)"; fi)
LLVM_CFLAGS=-DLLVM_API_VERSION=$(llvm_api_version)
endif

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS) $(LLVM_CFLAGS)

if ENABLE_DTRACE

BUILT_SOURCES = mono-dtrace.h

mono-dtrace.h: $(top_srcdir)/data/mono.d
	$(DTRACE) $(DTRACEFLAGS) -h -s $(top_srcdir)/data/mono.d -o $@ || > $@

endif

if HOST_WIN32
win32_sources = \
	os-event-win32.c \
	mono-os-wait-win32.c \
	mono-os-semaphore-win32.c

platform_sources = $(win32_sources)
else
unix_sources = \
	os-event-unix.c

platform_sources = $(unix_sources)
endif

monoutils_sources = \
	$(platform_sources)	\
	mono-md5.c		\
	mono-sha1.c		\
	mono-logger.c		\
	mono-logger-internals.h	\
	mono-codeman.c		\
	dlmalloc.h      	\
	dlmalloc.c      	\
	mono-counters.c		\
	mono-compiler.h		\
	mono-complex.h		\
	mono-dl.c		\
	mono-dl-windows.c	\
	mono-dl-darwin.c	\
	mono-dl-posix.c		\
	mono-dl-wasm.c	\
	mono-dl.h		\
	mono-dl-windows-internals.h	\
	mono-experiments.h	\
	mono-experiments.c	\
	mono-log-windows.c	\
	mono-log-common.c	\
	mono-log-posix.c	\
	mono-log-android.c \
	mono-log-darwin.c \
	mono-log-flight-recorder.c \
	mono-merp.c	\
	mono-merp.h	\
	mono-state.h	\
	mono-state.c	\
	mono-internal-hash.c	\
	mono-internal-hash.h	\
	mono-io-portability.c 	\
	mono-io-portability.h	\
	monobitset.c		\
	mono-filemap.c		\
	mono-mmap.c  		\
	mono-mmap-windows.c  		\
	mono-mmap-wasm.c  		\
	mono-mmap.h  		\
	mono-mmap-internals.h	\
	mono-mmap-windows-internals.h	\
	mono-os-mutex.h		\
	mono-os-mutex.c		\
	mono-flight-recorder.h		\
	mono-flight-recorder.c		\
	mono-os-wait.h \
	mono-coop-mutex.h		\
	mono-once.h		\
	mono-lazy-init.h		\
	mono-networkinterfaces.c		\
	mono-networkinterfaces.h		\
	mono-proclib.c		\
	mono-proclib-windows.c		\
	mono-proclib.h		\
	mono-proclib-windows-internals.h		\
	mono-publib.c		\
	mono-jemalloc.c		\
	mono-string.h		\
	mono-time.c  		\
	mono-time.h  		\
	strenc.h		\
	strenc.c		\
	mono-uri.c		\
	mono-poll.c		\
	mono-path.c		\
	mono-os-semaphore.h	\
	mono-coop-semaphore.h		\
	mono-sigcontext.h	\
	mono-stdlib.c 		\
	mono-property-hash.h 	\
	mono-property-hash.c 	\
	mono-value-hash.h 	\
	mono-value-hash.c 	\
	freebsd-elf_common.h 	\
	freebsd-elf32.h		\
	freebsd-elf64.h		\
	freebsd-dwarf.h 	\
	dtrace.h			\
	gc_wrapper.h		\
	mono-error.c	\
	mono-error-internals.h	\
	monobitset.h	\
	mono-codeman.h	\
	mono-counters.h	\
	mono-digest.h	\
	mono-error.h	\
	mono-forward-internal.h	\
	mono-machine.h	\
	mono-math.h	\
	mono-membar.h	\
	mono-path.h	\
	mono-poll.h	\
	mono-uri.h	\
	mono-stdlib.h	\
	valgrind.h	\
	mach-support.h  \
	memcheck.h	\
	mono-context.c	\
	mono-context.h	\
	mono-stack-unwinding.h	\
	hazard-pointer.c	\
	hazard-pointer.h	\
	lock-free-queue.c	\
	lock-free-queue.h	\
	lock-free-alloc.c	\
	lock-free-alloc.h	\
	lock-free-array-queue.c	\
	lock-free-array-queue.h	\
	mono-linked-list-set.c	\
	mono-linked-list-set.h	\
	mono-threads.c	\
	mono-threads-state-machine.c	\
	mono-threads-posix.c	\
	mono-threads-posix-signals.c	\
	mono-threads-mach.c	\
	mono-threads-mach-helper.c	\
	mono-threads-windows.c	\
	mono-threads-linux.c	\
	mono-threads-freebsd.c	\
	mono-threads-netbsd.c	\
	mono-threads-openbsd.c	\
	mono-threads-android.c	\
	mono-threads-haiku.c	\
	mono-threads-aix.c	\
	mono-threads-wasm.c	\
	mono-threads.h	\
	mono-threads-debug.h	\
	mono-threads-api.h	\
	mono-threads-coop.c	\
	mono-threads-coop.h	\
	mono-utility-thread.c	\
	mono-utility-thread.h	\
	mono-tls.h	\
	mono-tls.c	\
	mono-utils-debug.c \
	mono-utils-debug.h \
	linux_magic.h	\
	mono-memory-model.h	\
	atomic.h	\
	atomic.c	\
	mono-hwcap.h	\
	mono-hwcap.c	\
	mono-hwcap-vars.h	\
	bsearch.h	\
	bsearch.c	\
	mono-signal-handler.h	\
	mono-conc-hashtable.h	\
	mono-conc-hashtable.c	\
	json.h	\
	json.c	\
	networking.c	\
	networking-posix.c	\
	networking-fallback.c	\
	networking-missing.c	\
	networking-windows.c	\
	networking.h	\
	mono-rand.c	\
	mono-rand-windows.c	\
	mono-rand.h \
	memfuncs.c \
	memfuncs.h \
	parse.c \
	parse.h	\
	checked-build.c \
	checked-build.h \
	os-event.h \
	refcount.h	\
	w32api.h	\
	unlocked.h	\
	ward.h

arch_sources = 

if !CROSS_COMPILE

if X86
arch_sources += mach-support-x86.c
endif

if AMD64
arch_sources += mach-support-amd64.c
endif

if ARM
arch_sources += mach-support-arm.c
endif

if ARM64
arch_sources += mach-support-arm64.c
endif

else

arch_sources += mach-support-unknown.c

endif

if !CROSS_COMPILE

if X86
arch_sources += mono-hwcap-x86.c
endif

if AMD64
arch_sources += mono-hwcap-x86.c
endif

if ARM
arch_sources += mono-hwcap-arm.c
endif

if ARM64
arch_sources += mono-hwcap-arm64.c
endif

if MIPS
arch_sources += mono-hwcap-mips.c
endif

if POWERPC
arch_sources += mono-hwcap-ppc.c
endif

if POWERPC64
arch_sources += mono-hwcap-ppc.c
endif

if SPARC
arch_sources += mono-hwcap-sparc.c
endif

if SPARC64
arch_sources += mono-hwcap-sparc.c
endif

if S390X
arch_sources += mono-hwcap-s390x.c
endif

if RISCV
arch_sources += mono-hwcap-riscv.c
endif

if HOST_WASM
arch_sources += mono-hwcap-wasm.c
endif

else

arch_sources += mono-hwcap-cross.c

endif

libmonoutils_la_SOURCES = $(monoutils_sources) $(arch_sources)
libmonoutils_la_CFLAGS = $(JEMALLOC_CFLAGS) @CXX_ADD_CFLAGS@
libmonoutils_la_LDFLAGS = $(JEMALLOC_LDFLAGS)
libmonoutilsincludedir = $(includedir)/mono-$(API_VER)/mono/utils
libmonoutils_la_LIBADD = mono-math-c.lo

libmonomath_la_SOURCES = mono-math-c.c
libmonomath_la_CFLAGS = @CXX_REMOVE_CFLAGS@

# These are public headers.
# They should not use glib.h, G_BEGIN_DECLS, guint, etc.
# They should be wrapped in MONO_BEGIN_DECLS / MONO_END_DECLS.
libmonoutilsinclude_HEADERS = 	\
	mono-logger.h		\
	mono-error.h		\
	mono-forward.h	\
	mono-publib.h		\
	mono-jemalloc.h		\
	mono-dl-fallback.h	\
	mono-counters.h

EXTRA_DIST = mono-errno.h mono-embed.h mono-embed.c ../../support/libm/complex.c mono-experiments.def

DIST_SUBDIRS = jemalloc

if MONO_JEMALLOC_ENABLED
SUBDIRS = jemalloc
endif

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)
