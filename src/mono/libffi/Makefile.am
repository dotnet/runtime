## Process this with automake to create Makefile.in

AUTOMAKE_OPTIONS = foreign no-installinfo

if TESTSUBDIR
SUBDIRS = include testsuite
else
SUBDIRS = include
endif

EXTRA_DIST = 		\
	LICENSE 	\
	README		\
	ChangeLog.v1 	\
	alpha/ffi.c 	\
	arm/ffi.c	\
	ia64/ffi.c 	\
	m68k/ffi.c 	\
	mips/ffi.c 	\
	powerpc/ffi.c 	\
	s390/ffi.c	\
	sparc/ffi.c 	\
	x86/ffi.c 	\
	alpha/osf.S 	\
	arm/sysv.S 	\
	ia64/unix.S	\
	m68k/sysv.S 	\
	mips/n32.S 	\
	mips/o32.S	\
	powerpc/sysv.S 	\
	s390/sysv.S 	\
	sparc/v8.S 	\
	sparc/v9.S 	\
	x86/win32.S	\
	x86/sysv.S

# Multilib support variables.
MULTISRCTOP =
MULTIBUILDTOP =
MULTIDIRS =
MULTISUBDIR =
MULTIDO = true
MULTICLEAN = true

## Install a library built with a cross compiler in tooldir, not
## libdir.
if USE_LIBDIR
toolexeclibdir = $(libdir)$(MULTISUBDIR)
else
toolexecdir = $(exec_prefix)/$(target_alias)
toolexeclibdir = $(toolexecdir)/lib$(MULTISUBDIR)
endif

toolexeclib_LIBRARIES = libffi.a

## Work around automake deficiency
nodist_libffi_a_SOURCES = t-ffi.c t-asm.S

libffi_a_SOURCES = 			\
	debug.c 			\
	prep_cif.c 			\
	types.c 			\
	raw_api.c 			\
	java_raw_api.c 			\
	$(nodist_libffi_a_SOURCES)

AM_CFLAGS = -fexceptions

INCLUDES = 			\
	-I$(top_srcdir)/include \
	-Iinclude 		\
	-I$(top_srcdir)/src
