noinst_LTLIBRARIES = libmonoutils.la

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS)

if ENABLE_DTRACE

BUILT_SOURCES = mono-dtrace.h

mono-dtrace.h: $(top_srcdir)/data/mono.d
	$(DTRACE) $(DTRACEFLAGS) -h -s $(top_srcdir)/data/mono.d -o $@ || > $@

endif

monoutils_sources = \
	mono-md5.c		\
	mono-sha1.c		\
	mono-logger.c		\
	mono-logger-internal.h	\
	mono-codeman.c		\
	dlmalloc.h      	\
	dlmalloc.c      	\
	mono-counters.c		\
	mono-compiler.h		\
	mono-dl.c		\
	mono-dl.h		\
	mono-internal-hash.c	\
	mono-internal-hash.h	\
	mono-io-portability.c 	\
	mono-io-portability.h	\
	monobitset.c		\
	mono-filemap.c		\
	mono-math.c  		\
	mono-mmap.c  		\
	mono-mmap.h  		\
	mono-mutex.c  		\
	mono-mutex.h  		\
	mono-networkinterfaces.c		\
	mono-networkinterfaces.h		\
	mono-proclib.c		\
	mono-proclib.h		\
	mono-publib.c		\
	mono-string.h		\
	mono-time.c  		\
	mono-time.h  		\
	strtod.h		\
	strtod.c		\
	strenc.h		\
	strenc.c		\
	mono-uri.c		\
	mono-poll.c		\
	mono-path.c		\
	mono-semaphore.c	\
	mono-semaphore.h	\
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
	mono-machine.h	\
	mono-math.h	\
	mono-membar.h	\
	mono-path.h	\
	mono-poll.h	\
	mono-uri.h	\
	mono-stdlib.h	\
	valgrind.h	\
	mach-support.c	\
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
	mono-threads-posix.c	\
	mono-threads-mach.c	\
	mono-threads-windows.c	\
	mono-threads.h	\
	mono-tls.h	\
	linux_magic.h	\
	mono-memory-model.h	\
	atomic.h	\
	atomic.c

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

else

arch_sources += mach-support-unknown.c

endif

libmonoutils_la_SOURCES = $(monoutils_sources) $(arch_sources)
libmonoutilsincludedir = $(includedir)/mono-$(API_VER)/mono/utils

libmonoutilsinclude_HEADERS = 	\
	mono-logger.h		\
	mono-error.h		\
	mono-publib.h		\
	mono-dl-fallback.h

EXTRA_DIST = ChangeLog mono-embed.h mono-embed.c
