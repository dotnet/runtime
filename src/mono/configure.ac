# Process this file with autoconf to produce a configure script.
#AC_PREREQ([2.62])

AC_INIT(mono, [5.3.0],
        [http://bugzilla.xamarin.com/enter_bug.cgi?classification=Mono])

AC_CONFIG_SRCDIR([README.md])
AC_CONFIG_MACRO_DIR([m4])
AC_CANONICAL_SYSTEM
AC_CANONICAL_HOST

# Gross hack to enable 'make dist' on automake 1.9+tar 1.14.
# The extra brackets are to foil regex-based scans.
m4_ifdef([_A][M_PROG_TAR],[_A][M_SET_OPTION([tar-ustar])])

AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar no-dist-gzip foreign subdir-objects]
                 m4_esyscmd([case `automake --version | head -n 1` in    # parallel-tests is default in automake 1.13+, we need to explicitly enable it
                             *1.11*|*1.12*) echo parallel-tests;;        # for 1.11 and 1.12 but not below as those versions don't recognize the flag
                             esac]))                                     # TODO: remove this hack once we require automake 1.11+

AC_CONFIG_HEADERS([config.h])
AM_MAINTAINER_MODE

API_VER=2.0
AC_SUBST(API_VER)

AC_PROG_LN_S

m4_ifdef([AM_SILENT_RULES], [AM_SILENT_RULES([yes])])

MONO_VERSION_MAJOR=`echo $VERSION | cut -d . -f 1`
MONO_VERSION_MINOR=`echo $VERSION | cut -d . -f 2`
MONO_VERSION_BUILD=`echo $VERSION | cut -d . -f 3`

# This is the version number of the corlib-runtime interface. When
# making changes to this interface (by changing the layout
# of classes the runtime knows about, changing icall signature or
# semantics etc), increment this variable.
#
# This can be reset to 0 when Mono's version number is bumped
# since it's part of the corlib version (the prefix '1' in the full
# version number is to ensure the number isn't treated as octal in C)
MONO_CORLIB_COUNTER=1
MONO_CORLIB_VERSION=`printf "1%02d%02d%02d%03d" $MONO_VERSION_MAJOR $MONO_VERSION_MINOR $MONO_VERSION_BUILD $MONO_CORLIB_COUNTER`

AC_DEFINE_UNQUOTED(MONO_CORLIB_VERSION,$MONO_CORLIB_VERSION,[Version of the corlib-runtime interface])
AC_SUBST(MONO_CORLIB_VERSION)

case $host_os in
*cygwin* )
		 echo "Run configure using ./configure --host=i686-pc-mingw32"
		 exit 1
esac

# In case of cygwin, override LN_S, irrespective of what it determines.
# The build uses cygwin, but the actual runtime doesn't.
case $host_os in
*cygwin* ) LN_S='cp -p';;
esac

#
# libgc defaults
#
libgc_configure_args=

# These variables are the CPPFLAGS/CFLAGS passed to libgc's configure
# libgc should inherit the original CFLAGS/CPPFLAGS passed to configure, i.e. -O0
CPPFLAGS_FOR_LIBGC=$CPPFLAGS
CFLAGS_FOR_LIBGC=$CFLAGS
CPPFLAGS_FOR_EGLIB=$CPPFLAGS
CFLAGS_FOR_EGLIB=$CFLAGS
CPPFLAGS_FOR_BTLS=$CPPFLAGS
CFLAGS_FOR_BTLS=$CFLAGS

# libgc uses some deprecated APIs
CFLAGS_FOR_LIBGC="$CFLAGS -Wno-deprecated-declarations"

#
# These are the flags that need to be stored in the mono.pc file for 
# compiling code that will embed Mono
#
libmono_cflags=""
libmono_ldflags=""
AC_SUBST(libmono_cflags)
AC_SUBST(libmono_ldflags)

# Variable to have relocatable .pc files (lib, or lib64)
# realpath isn't always available, and requires that all but the tip of the provided
# path exists. Fall back to the old behaviour, but realpath allows depth >1
# e.g. Debian puts Mono in /usr/bin and libs in /usr/lib/x86_64-linux-gnu/ which is
# too deep for the old method to work
reloc_libdir=`realpath --relative-to=${prefix} ${libdir} 2> /dev/null || basename ${libdir}`
AC_SUBST(reloc_libdir)

# Set to yes if Unix sockets cannot be created in an anonymous namespace
need_link_unlink=no

#Set to extra linker flags to be passed to the runtime binaries (mono /mono-sgen)
extra_runtime_ldflags=""

# Thread configuration inspired by sleepycat's db
AC_MSG_CHECKING([host platform characteristics])
libgc_threads=no
has_dtrace=no
parallel_mark=yes
ikvm_native=yes

host_win32=no
target_win32=no
platform_android=no
host_darwin=no
case "$host" in
	*-mingw*|*-*-cygwin*)
		AC_DEFINE(DISABLE_PORTABILITY,1,[Disable the io-portability layer])
		AC_DEFINE(PLATFORM_NO_SYMLINKS,1,[This platform does not support symlinks])
		host_win32=yes
		mono_cv_clang=no
		if test "x$cross_compiling" = "xno"; then
			if test "x$host" = "x$build" -a "x$host" = "x$target"; then
				target_win32=yes
			fi
		else
			if test "x$host" = "x$target"; then
				target_win32=yes
			fi
		fi
		HOST_CC="gcc"
		# Windows Vista or later is required
		CPPFLAGS="$CPPFLAGS -DWINVER=0x0600 -D_WIN32_WINNT=0x0600 -D_WIN32_IE=0x0501 -D_UNICODE -DUNICODE -DWIN32_THREADS -DFD_SETSIZE=1024"
		LDFLAGS="$LDFLAGS -lmswsock -lws2_32 -lole32 -loleaut32 -lpsapi -lversion -ladvapi32 -lwinmm -lkernel32 -liphlpapi"
		libmono_cflags="-mms-bitfields -mwindows"
		libmono_ldflags="-mms-bitfields -mwindows"
		libdl=
		libgc_threads=win32
		with_sigaltstack=no
		with_tls=pthread
		with_sgen_default_concurrent=yes
		LN_S=cp
		# This forces libgc to use the DllMain based thread registration code on win32
		libgc_configure_args="$libgc_configure_args --enable-win32-dllmain=yes"
		;;
	*-*-*netbsd*)
		CPPFLAGS="$CPPFLAGS -D_REENTRANT -DGC_NETBSD_THREADS -D_GNU_SOURCE"
		libmono_cflags="-D_REENTRANT"
		LDFLAGS="$LDFLAGS -pthread"
		CPPFLAGS="$CPPFLAGS -DPLATFORM_BSD"
		libmono_ldflags="-pthread"
		need_link_unlink=yes
		libdl="-ldl"
		libgc_threads=pthreads
		with_sigaltstack=no
		use_sigposix=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-kfreebsd*-gnu)
		CPPFLAGS="$CPPFLAGS -DGC_FREEBSD_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP -DUSE_MUNMAP -DTHREAD_LOCAL_ALLOC -pthread"
		libmono_cflags="-D_REENTRANT -DTHREAD_LOCAL_ALLOC -pthread"
		libmono_ldflags="-lpthread -pthread"
		libdl="-ldl"
		libgc_threads=pthreads
		need_link_unlink=yes
		with_sigaltstack=no
		use_sigposix=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-*freebsd*)
		if test "x$PTHREAD_CFLAGS" = "x"; then
			CPPFLAGS="$CPPFLAGS -DGC_FREEBSD_THREADS"
			libmono_cflags=
		else
			CPPFLAGS="$CPPFLAGS $PTHREAD_CFLAGS -DGC_FREEBSD_THREADS"
			libmono_cflags="$PTHREAD_CFLAGS"
		fi
		if test "x$PTHREAD_LIBS" = "x"; then
			LDFLAGS="$LDFLAGS -pthread -L/usr/local/lib"
			libmono_ldflags="-pthread"
		else
			LDFLAGS="$LDFLAGS $PTHREAD_LIBS -L/usr/local/lib"
			libmono_ldflags="$PTHREAD_LIBS"
		fi
		CPPFLAGS="$CPPFLAGS -DPLATFORM_BSD"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID, 1, [pthread is a pointer])
		libdl=
		libgc_threads=pthreads
		use_sigposix=yes
		has_dtrace=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-*openbsd*)
		CPPFLAGS="$CPPFLAGS -D_THREAD_SAFE -DGC_OPENBSD_THREADS -DPLATFORM_BSD -D_REENTRANT -DUSE_MMAP"
		if test "x$disable_munmap" != "xyes"; then
		CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_THREAD_SAFE -D_REENTRANT"
		LDFLAGS="$LDFLAGS -pthread"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID)
		libdl=
		libgc_threads=pthreads
		with_sigaltstack=no
		use_sigposix=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-linux-android*)
		platform_android=yes
		AC_DEFINE(PLATFORM_ANDROID,1,[Targeting the Android platform])
		AC_DEFINE(TARGET_ANDROID,1,[Targeting the Android platform])

		CPPFLAGS="$CPPFLAGS -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP"
		if test "x$disable_munmap" != "xyes"; then
			CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_REENTRANT"
		libdl="-ldl"
		libgc_threads=pthreads
		use_sigposix=yes

		with_tls=pthread
		with_sigaltstack=no
		with_static_mono=no

		# Android doesn't support boehm, as it's missing <link.h>
		support_boehm=no
		with_gc=sgen

		# isinf(3) requires -lm; see isinf check below
		LDFLAGS="$LDFLAGS -lm"

		# Bionic's <pthread.h> sets PTHREAD_STACK_MIN=2*PAGE_SIZE; doesn't define
		# PAGE_SIZE; breaks mono/io-layer/collection.c
		# Bionic doesn't provide S_IWRITE; breaks io-layer/io.c
		CFLAGS="$CFLAGS -DPAGE_SIZE=4096 -DS_IWRITE=S_IWUSR"
		CXXFLAGS="$CXXFLAGS -DPAGE_SIZE=4096 -DS_IWRITE=S_IWUSR"

		# to bypass the underscore linker check, can't work when cross-compiling
		mono_cv_uscore=yes
		mono_cv_clang=no
		;;
	*-*-linux*)
		CPPFLAGS="$CPPFLAGS -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP"
		if test "x$disable_munmap" != "xyes"; then
			CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_REENTRANT"
		libdl="-ldl"
		libgc_threads=pthreads
		use_sigposix=yes
		if test "x$cross_compiling" != "xno"; then
                	# to bypass the underscore linker check, not
                	# available during cross-compilation
                	mono_cv_uscore=no
                fi
		case "$host" in
		aarch64-*)
			support_boehm=no
			with_gc=sgen
			;;
		powerpc*-*-linux*)
			# https://bugzilla.novell.com/show_bug.cgi?id=504411
			disable_munmap=yes
			;;
		esac
		with_sgen_default_concurrent=yes
		;;
	*-*-nacl*)
		CPPFLAGS="$CPPFLAGS -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP"
		if test "x$disable_munmap" != "xyes"; then
			CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_REENTRANT"
		libdl=
		libgc_threads=pthreads
		use_sigposix=yes
		ikvm_native=no
		AC_DEFINE(DISABLE_SOCKETS,1,[Disable sockets support])
		AC_DEFINE(DISABLE_ATTACH, 1, [Disable agent attach support])
		;;
	*-*-hpux*)
		CPPFLAGS="$CPPFLAGS -DGC_HPUX_THREADS -D_HPUX_SOURCE -D_XOPEN_SOURCE_EXTENDED -D_REENTRANT"
		# +ESdbgasm only valid on bundled cc on RISC
		# silently ignored for ia64
		if test $GCC != "yes"; then
			CFLAGS="$CFLAGS +ESdbgasm"
			# Arrange for run-time dereferencing of null
			# pointers to produce a SIGSEGV signal.
			LDFLAGS="$LDFLAGS -z"
		fi
		CFLAGS="$CFLAGS +ESdbgasm"
		LDFLAGS="$LDFLAGS -z"
		libmono_cflags="-D_REENTRANT"
		libmono_ldflags="-lpthread"
		libgc_threads=pthreads
		need_link_unlink=yes
		use_sigposix=yes
		;;
	*-*-solaris*)
		CPPFLAGS="$CPPFLAGS -DGC_SOLARIS_THREADS -DGC_SOLARIS_PTHREADS -D_REENTRANT -D_POSIX_PTHREAD_SEMANTICS -DUSE_MMAP -DUSE_MUNMAP -DPLATFORM_SOLARIS"
		need_link_unlink=yes
		libmono_cflags="-D_REENTRANT"
		libgc_threads=pthreads
		has_dtrace=yes
		use_sigposix=yes
		enable_solaris_tar_check=yes
		;;
	*-*-darwin*)
		parallel_mark="Disabled_Currently_Hangs_On_MacOSX"
		host_darwin=yes
		target_mach=yes
		CPPFLAGS="$CPPFLAGS -D_THREAD_SAFE -DGC_MACOSX_THREADS -DPLATFORM_MACOSX -DUSE_MMAP -DUSE_MUNMAP"
		libmono_cflags="-D_THREAD_SAFE"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID)
		AC_DEFINE(USE_MACH_SEMA, 1, [...])
		libdl=
		libgc_threads=pthreads
		has_dtrace=yes
		if test "x$cross_compiling" = "xyes"; then
			has_broken_apple_cpp=yes
		fi
		dnl Snow Leopard is horribly broken -- it reports itself as i386-apple-darwin*, but
		dnl its gcc defaults to 64-bit mode.  They have also deprecated the usage of ucontext
		dnl we need to set some flags to build our 32-bit binaries on 10.6 properly
		case "$host" in
			dnl Snow Leopard and newer config.guess reports as this
			i*86-*-darwin*)
				BROKEN_DARWIN_FLAGS="-arch i386 -D_XOPEN_SOURCE"
				BROKEN_DARWIN_CPPFLAGS="-D_XOPEN_SOURCE"
				CPPFLAGS="$CPPFLAGS $BROKEN_DARWIN_CPPFLAGS"
				CFLAGS="$CFLAGS $BROKEN_DARWIN_FLAGS"
				CXXFLAGS="$CXXFLAGS $BROKEN_DARWIN_FLAGS"
				CCASFLAGS="$CCASFLAGS $BROKEN_DARWIN_FLAGS"
				CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC $BROKEN_DARWIN_CPPFLAGS"
				CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC $BROKEN_DARWIN_FLAGS"
				CPPFLAGS_FOR_EGLIB="$CPPFLAGS_FOR_EGLIB $BROKEN_DARWIN_CPPFLAGS"
				CFLAGS_FOR_EGLIB="$CFLAGS_FOR_EGLIB $BROKEN_DARWIN_FLAGS"
				with_sgen_default_concurrent=yes
				;;
			x*64-*-darwin*)
				with_sgen_default_concurrent=yes
				;;
			arm*-darwin*)
				has_dtrace=no
				;;			
		esac
		;;
	*-*-haiku*)
		CPPFLAGS="$CPPFLAGS -D_REENTRANT -D_THREAD_SAFE"
		libmono_cflags="-D_REENTRANT -D_THREAD_SAFE"
		libdl=
		LIBS="$LIBS -lnetwork -ltextencoding"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID)
		dnl Haiku does not support static TLS with __thread
		with_tls=pthread
		dnl Boehm is too much work to backport Haiku support for
		support_boehm=no
		libgc_threads=pthreads
		use_sigposix=yes
		;;
	*)
		AC_MSG_WARN([*** Please add $host to configure.ac checks!])
		libdl="-ldl"
		;;
esac
AC_MSG_RESULT(ok)

if test x$need_link_unlink = xyes; then
   AC_DEFINE(NEED_LINK_UNLINK, 1, [Define if Unix sockets cannot be created in an anonymous namespace])
fi

if test x$host_win32 = xyes; then
   AC_DEFINE(HOST_WIN32, 1, [Host Platform is Win32])
fi

if test x$target_win32 = xyes; then
   AC_DEFINE(TARGET_WIN32, 1, [Target Platform is Win32])
fi

# Defined for all targets/platforms using classic Windows API support.
AC_DEFINE(HAVE_CLASSIC_WINAPI_SUPPORT, 1, [Use classic Windows API support])
AC_DEFINE(HAVE_UWP_WINAPI_SUPPORT, 0, [Don't use UWP Windows API support])

AC_SUBST(extra_runtime_ldflags)
AM_CONDITIONAL(HOST_WIN32, test x$host_win32 = xyes)
AM_CONDITIONAL(TARGET_WIN32, test x$target_win32 = xyes)
AM_CONDITIONAL(PLATFORM_LINUX, echo x$target_os | grep -q linux)
AM_CONDITIONAL(PLATFORM_DARWIN, test x$host_darwin = xyes)
AM_CONDITIONAL(PLATFORM_SIGPOSIX, test x$use_sigposix = xyes)
AM_CONDITIONAL(PLATFORM_ANDROID, test x$platform_android = xyes)

if test -z "$PLATFORM_DARWIN_TRUE"; then :
PLATFORM_AOT_SUFFIX=.dylib
fi

if test -z "$PLATFORM_LINUX_TRUE"; then :
PLATFORM_AOT_SUFFIX=.so
fi

if test -z "$HOST_WIN32_TRUE"; then :
PLATFORM_AOT_SUFFIX=.dll
fi

AC_SUBST(PLATFORM_AOT_SUFFIX)

## PLATFORM_AOT_SUFFIX not so simple for windows :-)

AC_CHECK_TOOL(CC, gcc, gcc)
AC_PROG_CC
AC_CHECK_TOOL(CXX, g++, g++)
AC_PROG_CXX
AM_PROG_AS
AC_PROG_INSTALL
AC_PROG_AWK
AM_PROG_CC_C_O
dnl We should use AM_PROG_AS, but it's not available on automake/aclocal 1.4
: ${CCAS='$(CC)'}
# Set ASFLAGS if not already set.
: ${CCASFLAGS='$(CFLAGS)'}
AC_SUBST(CCAS)
AC_SUBST(CCASFLAGS)

# AC_PROG_CXX helpfully sets CXX to g++ even if no c++ compiler is found so check
# GXX instead. See http://lists.gnu.org/archive/html/bug-autoconf/2002-04/msg00056.html
if test "x$CXX" = "xg++"; then
	if test "x$GXX" != "xyes"; then
		# automake/libtool is so broken, it requires g++ even if the c++ sources
		# are inside automake conditionals
		AC_MSG_ERROR([You need to install g++])
	fi
fi

dnl may require a specific autoconf version
dnl AC_PROG_CC_FOR_BUILD
dnl CC_FOR_BUILD not automatically detected
CC_FOR_BUILD=$CC
CFLAGS_FOR_BUILD=$CFLAGS
BUILD_EXEEXT=
if test "x$cross_compiling" = "xyes"; then
	CC_FOR_BUILD=cc
	CFLAGS_FOR_BUILD=
	BUILD_EXEEXT=""
fi
AC_SUBST(CC_FOR_BUILD)
AC_SUBST(CFLAGS_FOR_BUILD)
AC_SUBST(HOST_CC)
AC_SUBST(BUILD_EXEEXT)

AM_CONDITIONAL(CROSS_COMPILING, [test x$cross_compiling = xyes])
AM_CONDITIONAL(USE_BATCH_FILES, [test x$host_win32 = xyes -a x$cross_compiling = xyes])

# Set STDC_HEADERS
AC_HEADER_STDC
AC_LIBTOOL_WIN32_DLL
# This causes monodis to not link correctly
#AC_DISABLE_FAST_INSTALL

#lookup makedev() header
AC_HEADER_MAJOR

AM_PROG_LIBTOOL
# Use dolt (http://dolt.freedesktop.org/) instead of libtool for building.
DOLT

export_ldflags=`(./libtool --config; echo eval echo \\$export_dynamic_flag_spec) | sh`
AC_SUBST(export_ldflags)

# Test whenever ld supports -version-script
AC_PROG_LD
AC_PROG_LD_GNU

AM_ICONV()

AC_CHECK_HEADERS(sys/filio.h sys/sockio.h netdb.h utime.h sys/utime.h semaphore.h sys/un.h linux/rtc.h sys/syscall.h sys/mkdev.h sys/uio.h sys/param.h sys/sysctl.h libproc.h sys/prctl.h)
AC_CHECK_HEADERS(sys/param.h sys/socket.h sys/ipc.h sys/utsname.h alloca.h ucontext.h pwd.h sys/select.h netinet/tcp.h netinet/in.h unistd.h sys/types.h link.h asm/sigcontext.h sys/inotify.h arpa/inet.h complex.h unwind.h)
AC_CHECK_HEADERS([linux/netlink.h linux/rtnetlink.h],
                  [], [], [#include <stddef.h>
		  #include <sys/socket.h>
		  #include <linux/socket.h>])

AC_CHECK_HEADERS(sys/user.h, [], [],
[
#ifdef HAVE_SYS_PARAM_H
# include <sys/param.h>
#endif
])

AC_CHECK_HEADERS(linux/serial.h)

AC_CHECK_HEADER(zlib.h, [have_zlib=yes], [have_zlib=no])
if test x$have_zlib = xyes; then
   AC_TRY_COMPILE([#include <zlib.h>], [
   #if defined(ZLIB_VERNUM) && (ZLIB_VERNUM >= 0x1230)
   return 0;
   #else
   #error No good zlib found
   #endif
   ],[
	AC_MSG_RESULT(Using system zlib)
	zlib_msg="system zlib"
	AC_DEFINE(HAVE_SYS_ZLIB,1,[Have system zlib])
   ],[
	AC_MSG_RESULT(Using embedded zlib)
	have_zlib=no
	zlib_msg="bundled zlib"
   ])
fi

AM_CONDITIONAL(HAVE_ZLIB, test x$have_zlib = xyes)
AC_DEFINE(HAVE_ZLIB,1,[Have system zlib])

# for mono/metadata/debug-symfile.c
AC_CHECK_HEADERS(elf.h)

# for support
AC_CHECK_HEADERS(poll.h)
AC_CHECK_HEADERS(sys/poll.h)
AC_CHECK_HEADERS(sys/wait.h)
AC_CHECK_HEADERS(grp.h)
AC_CHECK_HEADERS(syslog.h)

# for mono/dis
AC_CHECK_HEADERS(wchar.h)
AC_CHECK_HEADERS(ieeefp.h)
AC_MSG_CHECKING(for isinf)
AC_TRY_LINK([#include <math.h>], [
	int f = isinf (1.0);
], [
	AC_MSG_RESULT(yes)
	AC_DEFINE(HAVE_ISINF, 1, [isinf available])
], [
	# We'll have to use signals
	AC_MSG_RESULT(no)
])
# mingw
AC_CHECK_FUNCS(_finite, , AC_MSG_CHECKING(for _finite in math.h)
	AC_TRY_LINK([#include <math.h>], 
	[ _finite(0.0); ], 
	AC_DEFINE(HAVE__FINITE, 1, [Have _finite in -lm]) AC_MSG_RESULT(yes),
	AC_MSG_RESULT(no)))

# for Linux statfs support
AC_CHECK_HEADERS(linux/magic.h)

# not 64 bit clean in cross-compile
AC_CHECK_SIZEOF(void *, 4)

AC_CACHE_CHECK([for clang],
	mono_cv_clang,[
	AC_TRY_COMPILE([], [
		#ifdef __clang__
		#else
		#error "FAILED"
		#endif
		return 0;
	],
	[mono_cv_clang=yes],
	[mono_cv_clang=no],
	[])
])

AC_ARG_ENABLE(visibility-hidden,
[  --disable-visibility-hidden    disable usage of -fvisiblity=hidden],
   disable_visibility_hidden=yes, disable_visibility_hidden=no)

WARN=''
if test x"$GCC" = xyes; then
        WARN='-Wall -Wunused -Wmissing-prototypes -Wmissing-declarations -Wstrict-prototypes  -Wmissing-prototypes -Wnested-externs -Wpointer-arith -Wno-cast-qual -Wwrite-strings -Wno-switch -Wno-switch-enum -Wno-unused-value -Wno-attributes -Wno-format-zero-length'

		# We require C99 with some GNU extensions, e.g. `linux` macro
		CFLAGS="$CFLAGS -std=gnu99"

		# The runtime code does not respect ANSI C strict aliasing rules
		CFLAGS="$CFLAGS -fno-strict-aliasing"

		# We rely on signed overflow to behave
		CFLAGS="$CFLAGS -fwrapv"

		CFLAGS="$CFLAGS -DMONO_DLL_EXPORT"
		if test x"$disable_visibility_hidden" = xno; then
		   # Don't export any symbols by default
		   SHARED_CFLAGS="-fvisibility=hidden"
		   CFLAGS_FOR_EGLIB="$CFLAGS_FOR_EGLIB -fvisibility=hidden"
		   CXXFLAGS="$CXXFLAGS -fvisibility=hidden"
		fi

		ORIG_CFLAGS=$CFLAGS
		# Check for the normal version, since gcc ignores unknown -Wno options
		CFLAGS="$CFLAGS -Wunused-but-set-variable -Werror"
		AC_MSG_CHECKING(for -Wno-unused-but-set-variable option to gcc)
		AC_TRY_COMPILE([],[
				return 0;
		], [
		   AC_MSG_RESULT(yes)
		   CFLAGS="$ORIG_CFLAGS -Wno-unused-but-set-variable"
		], [
		   AC_MSG_RESULT(no)
		   CFLAGS=$ORIG_CFLAGS
		])

		if test "x$mono_cv_clang" = "xyes"; then
	   	   # https://bugzilla.samba.org/show_bug.cgi?id=8118
	   	   WARN="$WARN -Qunused-arguments"
		   WARN="$WARN -Wno-unused-function -Wno-tautological-compare -Wno-parentheses-equality -Wno-self-assign -Wno-return-stack-address -Wno-constant-logical-operand"
		   # We rely on zero length arrays in structs
		   WARN="$WARN -Wno-zero-length-array"
		fi
else
	# The Sun Forte compiler complains about inline functions that access static variables
	# so disable all inlining.
	case "$host" in
	*-*-solaris*)
		CFLAGS="$CFLAGS -Dinline="
		;;
	esac
fi
CFLAGS="$CFLAGS -g $WARN"
CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -g"
CPPFLAGS="$CPPFLAGS -g $WARN"

# Where's the 'mcs' source tree?
if test -d $srcdir/mcs; then
  mcsdir=mcs
else
  mcsdir=../mcs
fi

AC_ARG_WITH(mcs-path, [  --with-mcs-path=/path/to/mcs      Specify an alternate mcs source tree],
	if test x$with_mcs_path != "x" -a -d $with_mcs_path ; then
		mcsdir=$with_mcs_path
	fi
)

AC_ARG_WITH(jumptables, [  --with-jumptables=yes,no      enable/disable support for jumptables (ARM-only for now) (defaults to no)],[],[with_jumptables=no])

#
# A sanity check to catch cases where the package was unpacked
# with an ancient tar program (Solaris)
#
AC_ARG_ENABLE(solaris-tar-check,
[  --disable-solaris-tar-check    disable solaris tar check],
   do_solaris_tar_check=no, do_solaris_tar_check=yes)

if test x"$do_solaris_tar_check" = xyes -a x"$enable_solaris_tar_check" = xyes; then
   	AC_MSG_CHECKING(integrity of package)
	if test -f $mcsdir/class/System.Runtime.Serialization.Formatters.Soap/System.Runtime.Serialization.Formatters.Soap/SoapTypeMapper.cs
   	then
		AC_MSG_RESULT(ok)
   	else
		errorm="Your mono distribution is incomplete;  if unpacking from a tar file, make sure you use GNU tar;  see http://www.mono-project.com/IncompletePackage for more details"
		AC_MSG_ERROR([$errorm])
	fi
fi

if test "x$with_mcs_path" != "x"; then
mcs_topdir=$(cd "$mcsdir" && pwd)
mcs_topdir_from_srcdir=$mcs_topdir
else
mcs_topdir=$(cd "$srcdir/$mcsdir" && pwd)
mcs_topdir_from_srcdir='$(top_builddir)'/$mcsdir
fi

# Convert mcs_topdir* paths to Windows syntax.
if test x$cross_compiling$host_win32 = xnoyes; then
  mcs_topdir=$(cygpath -m $mcs_topdir)
  case $mcs_topdir_from_srcdir in
    /cygdrive/*)
	mcs_topdir_from_srcdir=$(cygpath -m $mcs_topdir_from_srcdir)
	;;
  esac
fi

AC_SUBST([mcs_topdir])
AC_SUBST([mcs_topdir_from_srcdir])

# gettext: prepare the translation directories. 
# we do not configure the full gettext, as we consume it dynamically from C#
AM_PO_SUBDIRS

if test "x$USE_NLS" = "xyes"; then
   AC_CHECK_PROG(HAVE_MSGFMT, msgfmt,yes,no)

   if test "x$HAVE_MSGFMT" = "xno"; then
	  AC_MSG_ERROR([msgfmt not found. You need to install the 'gettext' package, or pass --enable-nls=no to configure.])
   fi
fi

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)

pkg_config_path=
AC_ARG_WITH(crosspkgdir, [  --with-crosspkgdir=/path/to/pkg-config/dir      Change pkg-config dir to custom dir],
	if test x$with_crosspkgdir = "x"; then
		if test -s $PKG_CONFIG_PATH; then
			pkg_config_path=$PKG_CONFIG_PATH
		fi
	else
		pkg_config_path=$with_crosspkgdir
		PKG_CONFIG_PATH=$pkg_config_path
		export PKG_CONFIG_PATH
	fi
)

AC_ARG_ENABLE(werror, [  --enable-werror Pass -Werror to the C compiler], werror_flag=$enableval, werror_flag=no)
if test x$werror_flag = xyes; then
	WERROR_CFLAGS="-Werror"
fi
AC_SUBST([WERROR_CFLAGS])
AC_SUBST([SHARED_CFLAGS])

ac_configure_args="$ac_configure_args \"CPPFLAGS_FOR_EGLIB=$EGLIB_CPPFLAGS\" \"CFLAGS_FOR_EGLIB=$CFLAGS_FOR_EGLIB\""
AC_CONFIG_SUBDIRS(eglib)

GLIB_CFLAGS='-I$(top_srcdir)/eglib/src -I$(top_builddir)/eglib/src'
GLIB_LIBS='-L$(top_builddir)/eglib/src -leglib -lm'
  
AC_SUBST(GLIB_CFLAGS)
AC_SUBST(GLIB_LIBS)

# Enable support for fast thread-local storage
# Some systems have broken support, so we allow to disable it.
AC_ARG_WITH(tls, [  --with-tls=__thread,pthread    select Thread Local Storage implementation (defaults to __thread)],[],[with_tls=__thread])

# Enable support for using sigaltstack for SIGSEGV and stack overflow handling
# This does not work on some platforms (bug #55253)
AC_ARG_WITH(sigaltstack, [  --with-sigaltstack=yes,no      enable/disable support for sigaltstack (defaults to yes)],[],[with_sigaltstack=yes])

AC_ARG_WITH(static_mono, [  --with-static_mono=yes,no      link mono statically to libmono (faster) (defaults to yes)],[],[with_static_mono=yes])
AC_ARG_WITH(shared_mono, [  --with-shared_mono=yes,no      build a shared libmono library (defaults to yes)],[],[with_shared_mono=yes])
# Same as --with-shared_mono=no
AC_ARG_ENABLE(libraries, [  --disable-libraries disable the build of libmono], enable_libraries=$enableval, enable_libraries=yes)

if test "x$enable_static" = "xno"; then
   with_static_mono=no
fi

if test "x$enable_shared" = "xno"; then
   with_shared_mono=no
fi

if test "x$enable_libraries" = "xno"; then
   with_shared_mono=no
fi

AM_CONDITIONAL(DISABLE_LIBRARIES, test x$enable_libraries = xno)

case $host in
*nacl* ) with_shared_mono=yes;;
esac

if test "x$host_win32" = "xyes"; then
   # Boehm GC requires the runtime to be in its own dll
   with_static_mono=no
fi

AM_CONDITIONAL(STATIC_MONO, test x$with_static_mono != xno)
AM_CONDITIONAL(SHARED_MONO, test x$with_shared_mono != xno)
AC_ARG_ENABLE(mcs-build, [  --disable-mcs-build disable the build of the mcs directory], try_mcs_build=$enableval, enable_mcs_build=yes)

AC_ARG_WITH(xen_opt,   [  --with-xen_opt=yes,no          Enable Xen-specific behaviour (defaults to yes)],[],[with_xen_opt=yes])
if test "x$with_xen_opt" = "xyes" -a "x$mono_cv_clang" = "xno"; then
	AC_DEFINE(MONO_XEN_OPT, 1, [Xen-specific behaviour])
	ORIG_CFLAGS=$CFLAGS
	CFLAGS="$CFLAGS -mno-tls-direct-seg-refs"
	AC_MSG_CHECKING(for -mno-tls-direct-seg-refs option to gcc)
	AC_TRY_COMPILE([], [
		return 0;
	], [
	   AC_MSG_RESULT(yes)
	   # Pass it to libgc as well
	   CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -mno-tls-direct-seg-refs"
	], [
	   AC_MSG_RESULT(no)
	   CFLAGS=$ORIG_CFLAGS
	])
fi

AC_ARG_ENABLE(small-config, [  --enable-small-config Enable tweaks to reduce requirements (and capabilities)], enable_small_config=$enableval, enable_small_config=no)

if test x$enable_small_config = xyes; then
	AC_DEFINE(MONO_SMALL_CONFIG,1,[Reduce runtime requirements (and capabilities)])
	CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DSMALL_CONFIG"
fi

AC_ARG_ENABLE(system-aot, [  --enable-system-aot  Enable the Ahead-Of-Time compilation of system assemblies during the build (on by default on some platforms)], enable_system_aot=$enableval, enable_system_aot=default)

DISABLED_FEATURES=none
csc_compiler=default
endian=unknown
AC_C_BIGENDIAN([endian=big],[endian=little],[endian=unknown])
AC_MSG_CHECKING([CSharp compiler to use])
AC_ARG_WITH(csc, [  --with-csc=mcs,roslyn,default      Configures the CSharp compiler to use],[
   if test x$withval = xmcs; then
       csc_compiler=mcs
   elif test x$withval = xroslyn; then
       csc_compiler=roslyn
   elif test x$withval = xdefault; then
       :
   else
       AC_MSG_ERROR([You must supply one of "mcs", "roslyn" or "default" to the --with-csc option])
   fi
],[csc_compiler=default])

if test $csc_compiler = default; then
   if test $endian = big; then
      csc_compiler=mcs
   elif test $endian = little; then
      csc_compiler=roslyn
   else
      csc_compiler=mcs
   fi
fi
AC_MSG_RESULT($csc_compiler)

#
# Set the build profiles and options before things which use them
#

AC_ARG_WITH(profile4_x,          [  --with-profile4_x=yes,no            If you want to install the 4.x FX (defaults to yes)],                       [], [with_profile4_x=default])
AC_ARG_WITH(monodroid,           [  --with-monodroid=yes,no             If you want to build the MonoDroid assemblies (defaults to no)],            [], [with_monodroid=default])
AC_ARG_WITH(monotouch,           [  --with-monotouch=yes,no             If you want to build the Xamarin.iOS assemblies (defaults to no)],          [], [with_monotouch=default])
AC_ARG_WITH(monotouch_watch,     [  --with-monotouch_watch=yes,no       If you want to build the Xamarin.WatchOS assemblies (defaults to no)],      [], [with_monotouch_watch=default])
AC_ARG_WITH(monotouch_tv,        [  --with-monotouch_tv=yes,no          If you want to build the Xamarin.TVOS assemblies (defaults to no)],         [], [with_monotouch_tv=default])
AC_ARG_WITH(bitcode,             [  --with-bitcode=yes,no               If bitcode is enabled (defaults to no)],                                    [], [with_bitcode=default])
AC_ARG_WITH(xammac,              [  --with-xammac=yes,no                If you want to build the Xamarin.Mac assemblies (defaults to no)],          [], [with_xammac=default])
AC_ARG_WITH(testing_aot_hybrid,  [  --with-testing_aot_hybrid=yes,no    If you want to build the testing_aot_hybrid assemblies (defaults to no)],   [], [with_testing_aot_hybrid=default])
AC_ARG_WITH(testing_aot_full,    [  --with-testing_aot_full=yes,no      If you want to build the testing_aot_full assemblies (defaults to no)],     [], [with_testing_aot_full=default])
AC_ARG_WITH(winaot,              [  --with-winaot=yes,no                If you want to build the Windows friendly AOT assemblies (defaults to no)], [], [with_winaot=default])
AC_ARG_WITH(orbis,              [  --with-orbis=yes,no                  If you want to build the Orbis assemblies (defaults to no)], [], [with_orbis=default])


AC_ARG_WITH(runtime_preset, [  --with-runtime_preset=net_4_x,all,aot,hybridaot,fullaot,bitcode   Which default profile to build (defaults to net_4_x)],  [], [with_runtime_preset=net_4_x])

dnl
dnl Profile defaults
dnl
TEST_PROFILE=default
enable_llvm_default=no

with_profile4_x_default=no
with_monodroid_default=no
with_monotouch_default=no
with_monotouch_watch_default=no
with_monotouch_tv_default=no
with_xammac_default=no
with_testing_aot_hybrid_default=no
with_testing_aot_full_default=no
with_winaot_default=no
with_orbis_default=no

with_bitcode_default=no
with_cooperative_gc_default=no

INVARIANT_AOT_OPTIONS=nimt-trampolines=2000,ntrampolines=8000,nrgctx-fetch-trampolines=256,ngsharedvt-trampolines=4000

if test x$cross_compiling = xyes -o x$enable_mcs_build = xno; then
   DISABLE_MCS_DOCS_default=yes
elif test x$with_runtime_preset = xnet_4_x; then
   with_profile4_x_default=yes
elif test x$with_runtime_preset = xall; then
   with_profile4_x_default=yes
   with_monodroid_default=yes
   with_monotouch_default=yes
   with_monotouch_watch_default=yes
   with_monotouch_tv_default=yes
   with_xammac_default=yes
   with_winaot_default=yes
   with_orbis_default=yes
elif test x$with_runtime_preset = xfullaot; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_full_default=yes
   TEST_PROFILE=testing_aot_full

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--runtime=mobile -O=gsharedvt --aot=full,$INVARIANT_AOT_OPTIONS"

   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
elif test x$with_runtime_preset = xbitcode; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_full_default=yes
   with_bitcode_default=yes
   with_cooperative_gc_default=yes
   TEST_PROFILE=testing_aot_full
   enable_llvm_default=yes

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--runtime=mobile --aot=llvmonly,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --llvmonly"
elif test x$with_runtime_preset = xhybridaot; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_hybrid_default=yes
   TEST_PROFILE=testing_aot_hybrid 

   AOT_BUILD_FLAGS="--runtime=mobile --aot=hybrid,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --hybrid-aot"
elif test x$with_runtime_preset = xaot; then
   with_profile4_x_default=yes

   AOT_BUILD_FLAGS="--aot=$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS=""

   DISABLE_MCS_DOCS_default=yes
elif test x$with_runtime_preset = xwinaot; then
   DISABLE_MCS_DOCS_default=yes
   with_winaot_default=yes
   TEST_PROFILE=winaot

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--runtime=mobile --aot=full,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
elif test x$with_runtime_preset = xorbis; then
   DISABLE_MCS_DOCS_default=yes
   with_orbis_default=yes
   TEST_PROFILE=orbis

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--runtime=mobile --aot=full,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
else
   with_profile4_x_default=yes
fi

if test "x$AOT_BUILD_FLAGS" != "x"; then :
   AC_SUBST(AOT_BUILD_FLAGS)
   AC_SUBST(AOT_RUN_FLAGS)
   # For llvmonlycheck + fullaotcheck
   AC_SUBST(INVARIANT_AOT_OPTIONS)
fi

AC_SUBST(TEST_PROFILE)

if test "x$with_profile4_x" = "xdefault"; then
   with_profile4_x=$with_profile4_x_default
fi
if test "x$with_monodroid" = "xdefault"; then
   with_monodroid=$with_monodroid_default
fi
if test "x$with_monotouch" = "xdefault"; then
   with_monotouch=$with_monotouch_default
fi
if test "x$with_monotouch_watch" = "xdefault"; then
   with_monotouch_watch=$with_monotouch_watch_default
fi
if test "x$with_monotouch_tv" = "xdefault"; then
   with_monotouch_tv=$with_monotouch_tv_default
fi
if test "x$with_bitcode" = "xdefault"; then
   with_bitcode=$with_bitcode_default
fi
if test "x$with_xammac" = "xdefault"; then
   with_xammac=$with_xammac_default
fi
if test "x$with_testing_aot_hybrid" = "xdefault"; then
   with_testing_aot_hybrid=$with_testing_aot_hybrid_default
fi
if test "x$with_testing_aot_full" = "xdefault"; then
   with_testing_aot_full=$with_testing_aot_full_default
fi
if test "x$with_winaot" = "xdefault"; then
   with_winaot=$with_winaot_default
fi
if test "x$with_orbis" = "xdefault"; then
   with_orbis=$with_orbis_default
fi


AM_CONDITIONAL(INSTALL_4_x, [test "x$with_profile4_x" = "xyes"])
AM_CONDITIONAL(INSTALL_MONODROID, [test "x$with_monodroid" != "xno"])
AM_CONDITIONAL(INSTALL_MONOTOUCH, [test "x$with_monotouch" != "xno"])
AM_CONDITIONAL(INSTALL_MONOTOUCH_WATCH, [test "x$with_monotouch_watch" != "xno"])
AM_CONDITIONAL(INSTALL_MONOTOUCH_TV, [test "x$with_monotouch_tv" != "xno"])
AM_CONDITIONAL(BITCODE, test "x$with_bitcode" = "xyes")
AM_CONDITIONAL(INSTALL_XAMMAC, [test "x$with_xammac" != "xno"])
AM_CONDITIONAL(INSTALL_TESTING_AOT_HYBRID, [test "x$with_testing_aot_hybrid" != "xno"])
AM_CONDITIONAL(INSTALL_TESTING_AOT_FULL, [test "x$with_testing_aot_full" != "xno"])
AM_CONDITIONAL(INSTALL_WINAOT, [test "x$with_winaot" != "xno"])
AM_CONDITIONAL(INSTALL_ORBIS, [test "x$with_orbis" != "xno"])
AM_CONDITIONAL(FULL_AOT_TESTS, [test "x$TEST_PROFILE" = "xtesting_aot_full"] || [test "x$TEST_PROFILE" = "xwinaot"] || [test "x$TEST_PROFILE" = "xorbis"])
AM_CONDITIONAL(HYBRID_AOT_TESTS, [test "x$TEST_PROFILE" = "xtesting_aot_hybrid"])

default_profile=net_4_x
if test -z "$INSTALL_MONODROID_TRUE"; then :
   default_profile=monodroid
fi
if test -z "$INSTALL_MONOTOUCH_TRUE"; then :
   default_profile=monotouch
fi
if test -z "$INSTALL_XAMMAC_TRUE"; then :
   default_profile=xammac
fi
if test -z "$INSTALL_TESTING_AOT_HYBRID_TRUE"; then :
   default_profile=testing_aot_hybrid
fi
if test -z "$INSTALL_TESTING_AOT_FULL_TRUE"; then :
   default_profile=testing_aot_full
fi
if test -z "$INSTALL_WINAOT_TRUE"; then :
   default_profile=winaot
fi
if test -z "$INSTALL_ORBIS_TRUE"; then :
   default_profile=orbis
fi
if test -z "$INSTALL_4_x_TRUE"; then :
   default_profile=net_4_x
fi
DEFAULT_PROFILE=$default_profile
AC_SUBST(DEFAULT_PROFILE)

#
# End build profile configuration
#

if test x$USE_NLS = xprofile_default; then

	AC_MSG_CHECKING([NLS used])

	# We make the default value for USE_NLS
	# "no" on OSX because it isn't available on most
	# default OSX installs. The most common configurations will
	# all disable it, so this saves us typing.
	if test x$host_darwin = xyes; then
		USE_NLS=no;
	else
		USE_NLS=yes;
	fi

	AC_SUBST([USE_NLS])
	AC_MSG_RESULT([$USE_NLS])
fi

AC_ARG_ENABLE(minimal, [  --enable-minimal=LIST      drop support for LIST subsystems.
     LIST is a comma-separated list from: aot, profiler, decimal, pinvoke, debug, appdomains, verifier, 
     reflection_emit, reflection_emit_save, large_code, logging, com, ssa, generics, attach, jit, simd, soft_debug, perfcounters, normalization, desktop_loader, shared_perfcounters, remoting,
	 security, lldb, mdb, sgen_remset, sgen_marksweep_par, sgen_marksweep_fixed, sgen_marksweep_fixed_par, sgen_copying.],
[
	for feature in `echo "$enable_minimal" | sed -e "s/,/ /g"`; do
		eval "mono_feature_disable_$feature='yes'"
	done
	DISABLED_FEATURES=$enable_minimal
	disabled="Disabled:      $enable_minimal"
],[])

AC_DEFINE_UNQUOTED(DISABLED_FEATURES, "$DISABLED_FEATURES", [String of disabled features])

if test "x$mono_feature_disable_aot" = "xyes"; then
	AC_DEFINE(DISABLE_AOT, 1, [Disable AOT Compiler])
	enable_system_aot=no
	AC_MSG_NOTICE([Disabled AOT compiler])
fi

if test "x$mono_feature_disable_profiler" = "xyes"; then
	AC_DEFINE(DISABLE_PROFILER, 1, [Disable default profiler support])
	AC_MSG_NOTICE([Disabled support for the profiler])
fi
AM_CONDITIONAL(DISABLE_PROFILER, test x$mono_feature_disable_profiler = xyes)

if test "x$mono_feature_disable_decimal" = "xyes"; then
	AC_DEFINE(DISABLE_DECIMAL, 1, [Disable System.Decimal support])
	AC_MSG_NOTICE([Disabled support for decimal])
fi

if test "x$mono_feature_disable_pinvoke" = "xyes"; then
	AC_DEFINE(DISABLE_PINVOKE, 1, [Disable P/Invoke support])
	AC_MSG_NOTICE([Disabled support for P/Invoke])
fi

if test "x$mono_feature_disable_debug" = "xyes"; then
	AC_DEFINE(DISABLE_DEBUG, 1, [Disable runtime debugging support])
	AC_MSG_NOTICE([Disabled support for runtime debugging])
fi

if test "x$mono_feature_disable_reflection_emit" = "xyes"; then
	AC_DEFINE(DISABLE_REFLECTION_EMIT, 1, [Disable reflection emit support])
	mono_feature_disable_reflection_emit_save=yes
	AC_MSG_NOTICE([Disabled support for Reflection.Emit])
fi

if test "x$mono_feature_disable_reflection_emit_save" = "xyes"; then
	AC_DEFINE(DISABLE_REFLECTION_EMIT_SAVE, 1, [Disable assembly saving support in reflection emit])
	AC_MSG_NOTICE([Disabled support for Reflection.Emit.Save])
fi

if test "x$mono_feature_disable_large_code" = "xyes"; then
	AC_DEFINE(DISABLE_LARGE_CODE, 1, [Disable support for huge assemblies])
	AC_MSG_NOTICE([Disabled support for large assemblies])
fi

if test "x$mono_feature_disable_logging" = "xyes"; then
	AC_DEFINE(DISABLE_LOGGING, 1, [Disable support debug logging])
	AC_MSG_NOTICE([Disabled support for logging])
fi

if test "x$mono_feature_disable_com" = "xyes"; then
	AC_DEFINE(DISABLE_COM, 1, [Disable COM support])
	AC_MSG_NOTICE([Disabled COM support])
fi

if test "x$mono_feature_disable_ssa" = "xyes"; then
	AC_DEFINE(DISABLE_SSA, 1, [Disable advanced SSA JIT optimizations])
	AC_MSG_NOTICE([Disabled SSA JIT optimizations])
fi

if test "x$mono_feature_disable_generics" = "xyes"; then
	AC_DEFINE(DISABLE_GENERICS, 1, [Disable generics support])
	AC_MSG_NOTICE([Disabled Generics Support])
fi

if test "x$mono_feature_disable_shadowcopy" = "xyes"; then
   	AC_DEFINE(DISABLE_SHADOW_COPY, 1, [Disable Shadow Copy for AppDomains])
	AC_MSG_NOTICE([Disabled Shadow copy for AppDomains])
fi

if test "x$mono_feature_disable_portability" = "xyes"; then
   	AC_DEFINE(DISABLE_PORTABILITY, 1, [Disables the IO portability layer])
	AC_MSG_NOTICE([Disabled IO Portability layer])
fi

if test "x$mono_feature_disable_attach" = "xyes"; then
	AC_DEFINE(DISABLE_ATTACH, 1, [Disable agent attach support])
	AC_MSG_NOTICE([Disabled agent attach])
fi

if test "x$mono_feature_disable_verifier" = "xyes"; then
	AC_DEFINE(DISABLE_VERIFIER, 1, [Disables the verifier])
	AC_MSG_NOTICE([Disabled the metadata and IL verifiers])
fi

if test "x$mono_feature_disable_jit" = "xyes"; then
	AC_DEFINE(DISABLE_JIT, 1, [Disable the JIT, only full-aot mode will be supported by the runtime.])
	AC_MSG_NOTICE([Disabled the JIT engine, only full AOT will be supported])
fi

AM_CONDITIONAL(DISABLE_JIT, test x$mono_feature_disable_jit = xyes)

if test "x$mono_feature_disable_simd" = "xyes"; then
	AC_DEFINE(DISABLE_SIMD, 1, [Disable SIMD intrinsics related optimizations.])
	AC_MSG_NOTICE([Disabled SIMD support])
fi

if test "x$mono_feature_disable_soft_debug" = "xyes"; then
	AC_DEFINE(DISABLE_SOFT_DEBUG, 1, [Disable Soft Debugger Agent.])
	AC_MSG_NOTICE([Disabled Soft Debugger.])
fi

if test "x$mono_feature_disable_perfcounters" = "xyes"; then
	AC_DEFINE(DISABLE_PERFCOUNTERS, 1, [Disable Performance Counters.])
	AC_MSG_NOTICE([Disabled Performance Counters.])
fi
if test "x$mono_feature_disable_normalization" = "xyes"; then
	AC_DEFINE(DISABLE_NORMALIZATION, 1, [Disable String normalization support.])
	AC_MSG_NOTICE([Disabled String normalization support.])
fi

#TODO: remove assembly_remapping feature name once everyone is using desktop_loader
if test "x$mono_feature_disable_assembly_remapping" = "xyes" || test "x$mono_feature_disable_desktop_loader" = "xyes"; then
	AC_DEFINE(DISABLE_DESKTOP_LOADER, 1, [Disable desktop assembly loader semantics.])
	AC_MSG_NOTICE([Disabled desktop assembly loader semantics.])
fi

if test "x$mono_feature_disable_shared_perfcounters" = "xyes"; then
	AC_DEFINE(DISABLE_SHARED_PERFCOUNTERS, 1, [Disable shared perfcounters.])
	AC_MSG_NOTICE([Disabled Shared perfcounters.])
fi

if test "x$mono_feature_disable_appdomains" = "xyes"; then
	AC_DEFINE(DISABLE_APPDOMAINS, 1, [Disable support for multiple appdomains.])
	AC_MSG_NOTICE([Disabled support for multiple appdomains.])
fi

if test "x$mono_feature_disable_remoting" = "xyes"; then
	AC_DEFINE(DISABLE_REMOTING, 1, [Disable remoting support (This disables type proxies and make com non-functional)])
	AC_MSG_NOTICE([Disabled remoting])
fi

if test "x$mono_feature_disable_security" = "xyes"; then
	AC_DEFINE(DISABLE_SECURITY, 1, [Disable CAS/CoreCLR security])
	AC_MSG_NOTICE([Disabled CAS/CoreCLR security manager (used e.g. for Moonlight)])
fi

if test "x$mono_feature_disable_lldb" = "xyes"; then
	AC_DEFINE(DISABLE_LLDB, 1, [Disable support code for the LLDB plugin.])
	AC_MSG_NOTICE([Disabled LLDB plugin support code.])
fi

if test "x$mono_feature_disable_mdb" = "xyes"; then
	AC_DEFINE(DISABLE_MDB, 1, [Disable support for .mdb symbol files.])
	AC_MSG_NOTICE([Disabled support for .mdb symbol files.])
fi

if test "x$mono_feature_disable_sgen_remset" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_REMSET, 1, [Disable wbarrier=remset support in SGEN.])
	AC_MSG_NOTICE([Disabled wbarrier=remset support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_marksweep_par" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_MAJOR_MARKSWEEP_PAR, 1, [Disable major=marksweep-par support in SGEN.])
	AC_MSG_NOTICE([Disabled major=marksweep-par support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_marksweep_fixed" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_MAJOR_MARKSWEEP_FIXED, 1, [Disable major=marksweep-fixed support in SGEN.])
	AC_MSG_NOTICE([Disabled major=marksweep-fixed support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_marksweep_fixed_par" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_MAJOR_MARKSWEEP_FIXED_PAR, 1, [Disable major=marksweep-fixed-par support in SGEN.])
	AC_MSG_NOTICE([Disabled major=marksweep-fixed-par support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_copying" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_MAJOR_COPYING, 1, [Disable major=copying support in SGEN.])
	AC_MSG_NOTICE([Disabled major=copying support in SGEN.])
fi

AC_ARG_ENABLE(executables, [  --disable-executables disable the build of the runtime executables], enable_executables=$enableval, enable_executables=yes)
AM_CONDITIONAL(DISABLE_EXECUTABLES, test x$enable_executables = xno)

has_extension_module=no
AC_ARG_ENABLE(extension-module, [  --enable-extension-module=LIST enable the core-extensions from LIST],
[
	for extension in `echo "$enable_extension_module" | sed -e "s/,/ /g"`; do
		if test x$extension = xdefault ; then
			has_extension_module=yes;
		fi
	done
	if test x$enable_extension_module = xyes; then
		has_extension_module=yes;
	fi
], [])

AM_CONDITIONAL([HAS_EXTENSION_MODULE], [test x$has_extension_module != xno])

if test x$has_extension_module != xno ; then
	AC_DEFINE([ENABLE_EXTENSION_MODULE], 1, [Extension module enabled])
	AC_MSG_NOTICE([Enabling mono extension module.])
fi

# Deprecated
AC_ARG_ENABLE(gsharedvt, [  --enable-gsharedvt Enable generic valuetype sharing (Deprecated)], enable_gsharedvt=$enableval, enable_gsharedvt=no)

AC_MSG_CHECKING(for visibility __attribute__)
AC_COMPILE_IFELSE([
	AC_LANG_SOURCE([[
		void __attribute__ ((visibility ("hidden"))) doit (void) {}
   		int main () { doit (); return 0; }
   	]])
], [
   have_visibility_hidden=yes
   AC_MSG_RESULT(yes)
], [
   have_visibility_hidden=no
   AC_MSG_RESULT(no)
])

dnl
dnl Boehm GC configuration
dnl

AC_ARG_WITH(libgc,   [  --with-libgc=included,none  Controls the Boehm GC config, default=included],[libgc=$with_gc],[libgc=included])

AC_ARG_ENABLE(boehm, [  --disable-boehm            Disable the Boehm GC.], support_boehm=$enableval,support_boehm=${support_boehm:-yes})
AM_CONDITIONAL(SUPPORT_BOEHM, test x$support_boehm = xyes)

if test "x$support_boehm" = "xyes"; then

	AC_ARG_ENABLE(parallel-mark, [  --enable-parallel-mark     Enables Boehm GC Parallel Marking], enable_parallel_mark=$enableval, enable_parallel_mark=$parallel_mark)
	if test x$enable_parallel_mark = xyes; then
		libgc_configure_args="$libgc_configure_args --enable-parallel-mark"
	fi

	gc_msg=""
	LIBGC_CPPFLAGS=
	LIBGC_LIBS=
	LIBGC_STATIC_LIBS=
	libgc_dir=
	case "x$libgc" in
		xincluded)
			if test "x$support_boehm" = "xyes"; then
				libgc_dir=libgc
			fi

			LIBGC_CPPFLAGS='-I$(top_srcdir)/libgc/include'
			LIBGC_LIBS='$(top_builddir)/libgc/libmonogc.la'
			LIBGC_STATIC_LIBS='$(top_builddir)/libgc/libmonogc-static.la'

			BOEHM_DEFINES="-DHAVE_BOEHM_GC"

			if test x$target_win32 = xyes; then
				BOEHM_DEFINES="$BOEHM_DEFINES -DGC_NOT_DLL"
				CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DGC_BUILD -DGC_NOT_DLL"
			fi

			gc_msg="Included Boehm GC with typed GC"
			if test x$enable_parallel_mark = xyes; then
				AC_DEFINE_UNQUOTED(DEFAULT_GC_NAME, "Included Boehm (with typed GC and Parallel Mark)", [GC description])
				gc_msg="$gc_msg and parallel mark"
			else
				AC_DEFINE_UNQUOTED(DEFAULT_GC_NAME, "Included Boehm (with typed GC)", [GC description])
			fi
			;;

		xboehm|xbohem|xyes)
			AC_MSG_WARN("External Boehm is no longer supported")
			;;

		xsgen)
			AC_MSG_WARN("Use --with-sgen instead, --with-libgc= controls Boehm configuration")
			;;

		xnone)
			AC_MSG_WARN("Compiling mono without GC.")
			AC_DEFINE_UNQUOTED(DEFAULT_GC_NAME, "none", [GC description])
			AC_DEFINE(HAVE_NULL_GC,1,[No GC support.])
			gc_msg="none"
			;;
		*)
			AC_MSG_ERROR([Invalid argument to --with-libgc.])
			;;
	esac

	AC_ARG_WITH(large-heap, [  --with-large-heap=yes,no       Enable support for GC heaps larger than 3GB (defaults to no)], [large_heap=$withval], [large_heap=no])
	if test "x$large_heap" = "xyes"; then
	   CPPFLAGS="$CPPFLAGS -DLARGE_CONFIG"
	fi

	AC_SUBST(LIBGC_CPPFLAGS)
	AC_SUBST(LIBGC_LIBS)
	AC_SUBST(LIBGC_STATIC_LIBS)
	AC_SUBST(libgc_dir)
	AC_SUBST(BOEHM_DEFINES)

fi

dnl
dnl End of Boehm GC Configuration
dnl

dnl *************************************
dnl *** Checks for zero length arrays ***
dnl *************************************
AC_MSG_CHECKING(whether $CC supports zero length arrays)
AC_TRY_COMPILE([
	struct s {
		int  length;
		char data [0];
	};
], [], [
	AC_MSG_RESULT(yes)
	AC_DEFINE_UNQUOTED(MONO_ZERO_LEN_ARRAY, 0, [Length of zero length arrays])
], [
	AC_MSG_RESULT(no)
	AC_DEFINE_UNQUOTED(MONO_ZERO_LEN_ARRAY, 1, [Length of zero length arrays])
])

AC_CHECK_HEADERS(nacl/nacl_dyncode.h)

dnl ***********************************
dnl *** Checks for signals
dnl ***********************************
AC_CHECK_HEADERS(signal.h)
AC_CHECK_FUNCS(sigaction)
AC_CHECK_FUNCS(kill)
AC_CHECK_FUNCS(signal)

# signal() is declared inline in Android headers
# so we need to workaround it by overriding the check.
if test x$platform_android = xyes; then
	AC_DEFINE(HAVE_SIGNAL,1)
fi

if test x$host_win32 = xno; then

	dnl hires monotonic clock support
	AC_SEARCH_LIBS(clock_gettime, rt)
	AC_CHECK_FUNCS(clock_nanosleep)

	dnl dynamic loader support
	AC_CHECK_FUNC(dlopen, DL_LIB="",
		AC_CHECK_LIB(dl, dlopen, DL_LIB="-ldl", dl_support=no)
	)
	if test x$dl_support = xno; then
		AC_MSG_WARN([No dynamic loading support available])
	else
		LIBS="$LIBS $DL_LIB"
		AC_DEFINE(HAVE_DL_LOADER,1,[dlopen-based dynamic loader available])
		dnl from glib's configure.ac
		AC_CACHE_CHECK([for preceeding underscore in symbols],
			mono_cv_uscore,[
			AC_TRY_RUN([#include <dlfcn.h>
			int mono_underscore_test (void) { return 42; }
			int main() {
			  void *f1 = (void*)0, *f2 = (void*)0, *handle;
			  handle = dlopen ((void*)0, 0);
			  if (handle) {
			    f1 = dlsym (handle, "mono_underscore_test");
			    f2 = dlsym (handle, "_mono_underscore_test");
			  } return (!f2 || f1);
			}],
				[mono_cv_uscore=yes],
				[mono_cv_uscore=no],
			[])
		])
		if test "x$mono_cv_uscore" = "xyes"; then
			MONO_DL_NEED_USCORE=1
		else
			MONO_DL_NEED_USCORE=0
		fi
		AC_SUBST(MONO_DL_NEED_USCORE)
		AC_CHECK_FUNC(dlerror)
	fi

	dnl ******************************************************************
	dnl *** Checks for the IKVM JNI interface library                  ***
	dnl ******************************************************************
	AC_ARG_WITH(ikvm-native, [  --with-ikvm-native=yes,no      build the IKVM JNI interface library (defaults to yes)],[with_ikvm_native=$withval],[with_ikvm_native=$ikvm_native])

	ikvm_native_dir=
	if test x$with_ikvm_native = xyes; then
		ikvm_native_dir=ikvm-native
		jdk_headers_found="IKVM Native"
	fi

	AC_SUBST(ikvm_native_dir)

	AC_CHECK_HEADERS(execinfo.h)

	AC_CHECK_HEADERS(sys/auxv.h sys/resource.h)

	AC_CHECK_FUNCS(getgrgid_r)
	AC_CHECK_FUNCS(getgrnam_r)
	AC_CHECK_FUNCS(getresuid)
	AC_CHECK_FUNCS(setresuid)
	AC_CHECK_FUNCS(kqueue)
	AC_CHECK_FUNCS(backtrace_symbols)
	AC_CHECK_FUNCS(mkstemp)
	AC_CHECK_FUNCS(mmap)
	AC_CHECK_FUNCS(madvise)
	AC_CHECK_FUNCS(getrusage)
	AC_CHECK_FUNCS(getpriority)
	AC_CHECK_FUNCS(setpriority)
	AC_CHECK_FUNCS(dl_iterate_phdr)
	AC_CHECK_FUNCS(dladdr)
	AC_CHECK_FUNCS(sysconf)
	AC_CHECK_FUNCS(getrlimit)
	AC_CHECK_FUNCS(prctl)

	AC_CHECK_FUNCS(sched_getaffinity)
	AC_CHECK_FUNCS(sched_setaffinity)
	AC_CHECK_FUNCS(sched_getcpu)

	if test x$platform_android != xyes; then
		AC_CHECK_FUNCS(getpwnam_r)
		AC_CHECK_FUNCS(getpwuid_r)
	fi

	dnl ****************************************************************
	dnl *** Check for sched_setaffinity from glibc versions before   ***
	dnl *** 2.3.4. The older versions of the function only take 2    ***
	dnl *** parameters, not 3.                                       ***
	dnl ***                                                          ***
	dnl *** Because the interface change was not made in a minor     ***
	dnl *** version rev, the __GLIBC__ and __GLIBC_MINOR__ macros    ***
	dnl *** won't always indicate the interface sched_affinity has.  ***
	dnl ****************************************************************
	AC_MSG_CHECKING(for sched_setaffinity from glibc < 2.3.4)
	AC_TRY_COMPILE([#include <sched.h>], [
            int mask = 1; 
            sched_setaffinity(0, &mask);
			return 0;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY, 1, [Have GLIBC_BEFORE_2_3_4_SCHED_SETAFFINITY])
	], [
		# We have the new, three-parameter version
		AC_MSG_RESULT(no)
	])
	AC_TRY_COMPILE([#include <sched.h>], [
	     CPU_COUNT((void *) 0);
	], [
		AC_MSG_RESULT(yes)
		AC_DEFINE(GLIBC_HAS_CPU_COUNT, 1, [GLIBC has CPU_COUNT macro in sched.h])
	], [
		# We have the new, three-parameter version
		AC_MSG_RESULT(no)
	])

	dnl ******************************************************************
	dnl *** Check for large file support                               ***
	dnl *** (If we were using autoconf 2.50 we'd use AC_SYS_LARGEFILE) ***
	dnl ******************************************************************
	
	# Check that off_t can represent 2**63 - 1 correctly, working around
	# potential compiler bugs.  Defines LARGE_FILE_SUPPORT, adds $1 to
	# CPPFLAGS and sets $large_offt to yes if the test succeeds
	large_offt=no
	AC_DEFUN([LARGE_FILES], [
		large_CPPFLAGS=$CPPFLAGS
		CPPFLAGS="$CPPFLAGS $1"
		AC_TRY_COMPILE([
			#include <sys/types.h>
			#include <limits.h>
		], [
			/* Lifted this compile time assert method from: http://www.jaggersoft.com/pubs/CVu11_3.html */
			#define COMPILE_TIME_ASSERT(pred) \
				switch(0){case 0:case pred:;}
			COMPILE_TIME_ASSERT(sizeof(off_t) * CHAR_BIT == 64);
		], [
			AC_MSG_RESULT(ok)
			AC_DEFINE(HAVE_LARGE_FILE_SUPPORT, 1, [Have large file support])
			large_CPPFLAGS="$large_CPPFLAGS $1"
			large_offt=yes
		], [
			AC_MSG_RESULT(no)
		])
		CPPFLAGS=$large_CPPFLAGS
	])

	AC_MSG_CHECKING(if off_t is 64 bits wide)
	LARGE_FILES("")
	if test $large_offt = no; then
		AC_MSG_CHECKING(if _FILE_OFFSET_BITS=64 gives 64 bit off_t)
		LARGE_FILES("-D_FILE_OFFSET_BITS=64")
	fi
	if test $large_offt = no; then
		AC_MSG_WARN([No 64 bit file size support available])
	fi
	
	dnl *****************************
	dnl *** Checks for libsocket  ***
	dnl *****************************
	AC_CHECK_LIB(socket, socket, LIBS="$LIBS -lsocket")

	case "$host" in
		*-*-*freebsd*)
			dnl *****************************
			dnl *** Checks for libinotify ***
			dnl *****************************
			AC_CHECK_LIB(inotify, inotify_init, LIBS="$LIBS -linotify")
	esac

	dnl *******************************
	dnl *** Checks for MSG_NOSIGNAL ***
	dnl *******************************
	AC_MSG_CHECKING(for MSG_NOSIGNAL)
	AC_TRY_COMPILE([#include <sys/socket.h>], [
		int f = MSG_NOSIGNAL;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_MSG_NOSIGNAL, 1, [Have MSG_NOSIGNAL])
	], [
		# We'll have to use signals
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for IPPROTO_IP ***
	dnl *****************************
	AC_MSG_CHECKING(for IPPROTO_IP)
	AC_TRY_COMPILE([#include <netinet/in.h>], [
		int level = IPPROTO_IP;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IPPROTO_IP, 1, [Have IPPROTO_IP])
	], [
		# We'll have to use getprotobyname
		AC_MSG_RESULT(no)
	])

	dnl *******************************
	dnl *** Checks for IPPROTO_IPV6 ***
	dnl *******************************
	AC_MSG_CHECKING(for IPPROTO_IPV6)
	AC_TRY_COMPILE([#include <netinet/in.h>], [
		int level = IPPROTO_IPV6;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IPPROTO_IPV6, 1, [Have IPPROTO_IPV6])
	], [
		# We'll have to use getprotobyname
		AC_MSG_RESULT(no)
	])

	dnl ******************************
	dnl *** Checks for IPPROTO_TCP ***
	dnl ******************************
	AC_MSG_CHECKING(for IPPROTO_TCP)
	AC_TRY_COMPILE([#include <netinet/in.h>], [
		int level = IPPROTO_TCP;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IPPROTO_TCP, 1, [Have IPPROTO_TCP])
	], [
		# We'll have to use getprotobyname
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for SOL_IP     ***
	dnl *****************************
	AC_MSG_CHECKING(for SOL_IP)
	AC_TRY_COMPILE([#include <netdb.h>], [
		int level = SOL_IP;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SOL_IP, 1, [Have SOL_IP])
	], [
		# We'll have to use getprotobyname
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for SOL_IPV6     ***
	dnl *****************************
	AC_MSG_CHECKING(for SOL_IPV6)
	AC_TRY_COMPILE([#include <netdb.h>], [
		int level = SOL_IPV6;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SOL_IPV6, 1, [Have SOL_IPV6])
	], [
		# We'll have to use getprotobyname
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for SOL_TCP    ***
	dnl *****************************
	AC_MSG_CHECKING(for SOL_TCP)
	AC_TRY_COMPILE([#include <netdb.h>], [
		int level = SOL_TCP;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SOL_TCP, 1, [Have SOL_TCP])
	], [
		# We'll have to use getprotobyname
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for IP_PKTINFO ***
	dnl *****************************
	AC_MSG_CHECKING(for IP_PKTINFO)
	AC_TRY_COMPILE([#include <linux/in.h>], [
		int level = IP_PKTINFO;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IP_PKTINFO, 1, [Have IP_PKTINFO])
	], [
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for IPV6_PKTINFO ***
	dnl *****************************
	AC_MSG_CHECKING(for IPV6_PKTINFO)
	AC_TRY_COMPILE([#include <netdb.h>], [
		int level = IPV6_PKTINFO;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IPV6_PKTINFO, 1, [Have IPV6_PKTINFO])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Checks for IP_DONTFRAG     ***
	dnl **********************************
	AC_MSG_CHECKING(for IP_DONTFRAG)
	AC_TRY_COMPILE([#include <netinet/in.h>], [
		int level = IP_DONTFRAG;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IP_DONTFRAG, 1, [Have IP_DONTFRAG])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Checks for IP_DONTFRAGMENT ***
	dnl **********************************
	AC_MSG_CHECKING(for IP_DONTFRAGMENT)
	AC_TRY_COMPILE([#include <Ws2ipdef.h>], [
		int level = IP_DONTFRAGMENT;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IP_DONTFRAGMENT, 1, [Have IP_DONTFRAGMENT])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Checks for IP_MTU_DISCOVER ***
	dnl **********************************
	AC_MSG_CHECKING(for IP_MTU_DISCOVER)
	AC_TRY_COMPILE([#include <linux/in.h>], [
		int level = IP_MTU_DISCOVER;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IP_MTU_DISCOVER, 1, [Have IP_MTU_DISCOVER])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Checks for  IP_PMTUDISC_DO ***
	dnl **********************************
	AC_MSG_CHECKING(for IP_PMTUDISC_DO)
	AC_TRY_COMPILE([#include <linux/in.h>], [
		int level = IP_PMTUDISC_DO;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IP_PMTUDISC_DO, 1, [Have IP_PMTUDISC_DO])
	], [
		AC_MSG_RESULT(no)
	])

 	dnl *********************************
	dnl *** Check for struct ip_mreqn ***
	dnl *********************************
	AC_MSG_CHECKING(for struct ip_mreqn)
	AC_TRY_COMPILE([#include <netinet/in.h>], [
		struct ip_mreqn mreq;
		mreq.imr_address.s_addr = 0;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_STRUCT_IP_MREQN, 1, [Have struct ip_mreqn])
	], [
		# We'll just have to try and use struct ip_mreq
		AC_MSG_RESULT(no)
		AC_MSG_CHECKING(for struct ip_mreq)
		AC_TRY_COMPILE([#include <netinet/in.h>], [
			struct ip_mreq mreq;
			mreq.imr_interface.s_addr = 0;
		], [
			# Yes, we have it...
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_STRUCT_IP_MREQ, 1, [Have struct ip_mreq])
		], [
			# No multicast support
			AC_MSG_RESULT(no)
		])
	])
	
	dnl **********************************
	dnl *** Check for getaddrinfo ***
	dnl **********************************
	AC_MSG_CHECKING(for getaddrinfo)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [
		getaddrinfo(NULL,NULL,NULL,NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETADDRINFO, 1, [Have getaddrinfo])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for gethostbyname2_r ***
	dnl **********************************
	AC_MSG_CHECKING(for gethostbyname2_r)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [

		gethostbyname2_r(NULL,0,NULL,NULL,0,NULL,NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETHOSTBYNAME2_R, 1, [Have gethostbyname2_r])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for gethostbyname2 ***
	dnl **********************************
	AC_MSG_CHECKING(for gethostbyname2)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [
		gethostbyname2(NULL,0);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETHOSTBYNAME2, 1, [Have gethostbyname2])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for gethostbyname ***
	dnl **********************************
	AC_MSG_CHECKING(for gethostbyname)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [
		gethostbyname(NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETHOSTBYNAME, 1, [Have gethostbyname])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for getprotobyname ***
	dnl **********************************
	AC_MSG_CHECKING(for getprotobyname)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [
		getprotobyname(NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETPROTOBYNAME, 1, [Have getprotobyname])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for getnameinfo ***
	dnl **********************************
	AC_MSG_CHECKING(for getnameinfo)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [
		getnameinfo (NULL, 0, NULL, 0, NULL, 0, 0);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETNAMEINFO, 1, [Have getnameinfo])
	], [
		AC_MSG_RESULT(no)
	])


	dnl **********************************
	dnl *** Check for inet_ntop ***
	dnl **********************************
	AC_MSG_CHECKING(for inet_ntop)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <arpa/inet.h>
	], [
		inet_ntop (0, NULL, NULL, 0);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_INET_NTOP, 1, [Have inet_ntop])
	], [
		AC_MSG_RESULT(no)
	])

	dnl *****************************
	dnl *** Checks for libnsl     ***
	dnl *****************************
	AC_CHECK_FUNC(gethostbyaddr, , AC_CHECK_LIB(nsl, gethostbyaddr, LIBS="$LIBS -lnsl"))

	AC_CHECK_FUNCS(inet_pton inet_aton)

	dnl *****************************
	dnl *** Checks for libxnet    ***
	dnl *****************************
	case "${host}" in
		*solaris* )
			AC_MSG_CHECKING(for Solaris XPG4 support)
			if test -f /usr/lib/libxnet.so; then
				CPPFLAGS="$CPPFLAGS -D_XOPEN_SOURCE=500"
	    			CPPFLAGS="$CPPFLAGS -D__EXTENSIONS__"
				CPPFLAGS="$CPPFLAGS -D_XOPEN_SOURCE_EXTENDED=1"
           			LIBS="$LIBS -lxnet"
  	    			AC_MSG_RESULT(yes)
			else
  	    			AC_MSG_RESULT(no)
			fi

			if test "$GCC" = "yes"; then
		    		CFLAGS="$CFLAGS -Wno-char-subscripts"
			fi
    		;;
	esac

	dnl *****************************
	dnl *** Checks for libpthread ***
	dnl *****************************
# on FreeBSD -STABLE, the pthreads functions all reside in libc_r
# and libpthread does not exist
#
	case "${host}" in
		*-*-*haiku*)
			dnl Haiku has pthread in libroot (libc equiv)
			AC_CHECK_LIB(pthread, main, LIBS="$LIBS")
		;;
		*-*-*freebsd*)
			AC_CHECK_LIB(pthread, main, LIBS="$LIBS -pthread")
		;;
		*-*-*openbsd*)
			AC_CHECK_LIB(pthread, main, LIBS="$LIBS -pthread")
		;;
		*)
			AC_CHECK_LIB(pthread, main, LIBS="$LIBS -lpthread")
		;;
	esac
	AC_CHECK_HEADERS(pthread.h)
	AC_CHECK_HEADERS(pthread_np.h)
	AC_CHECK_FUNCS(pthread_mutex_timedlock)
	AC_CHECK_FUNCS(pthread_getattr_np pthread_attr_get_np pthread_setname_np)
	AC_CHECK_FUNCS(pthread_kill)
	AC_MSG_CHECKING(for PTHREAD_MUTEX_RECURSIVE)
	AC_TRY_COMPILE([ #include <pthread.h>], [
		pthread_mutexattr_t attr;
		pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
	], [
		AC_MSG_RESULT(ok)
	], [
		AC_MSG_RESULT(no)
		AC_ERROR(Posix system lacks support for recursive mutexes)
	])
	AC_CHECK_FUNCS(pthread_attr_setstacksize)
	AC_CHECK_FUNCS(pthread_attr_getstack pthread_attr_getstacksize)
	AC_CHECK_FUNCS(pthread_get_stacksize_np pthread_get_stackaddr_np)

	dnl **********************************
	dnl *** Check for mincore ***
	dnl **********************************
	AC_MSG_CHECKING(for mincore)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <sys/types.h>
		#include <sys/mman.h>
	], [
		mincore(NULL, 0, NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_MINCORE, 1, [Have mincore])
	], [
		AC_MSG_RESULT(no)
	])

	dnl ***********************************
	dnl *** Checks for working __thread ***
	dnl ***********************************
	AC_MSG_CHECKING(for working __thread)
	if test "x$with_tls" != "x__thread"; then
		AC_MSG_RESULT(disabled)
	elif test "x$cross_compiling" = "xyes"; then
		AC_MSG_RESULT(cross compiling, assuming yes)
	else
		AC_TRY_RUN([
			#if defined(__APPLE__) && defined(__clang__)
			#error "__thread does not currently work with clang on Mac OS X"
			#endif
			
			#include <pthread.h>
			__thread int i;
			static int res1, res2;

			void thread_main (void *arg)
			{
				i = arg;
				sleep (1);
				if (arg == 1)
					res1 = (i == arg);
				else
					res2 = (i == arg);
			}

			int main () {
				pthread_t t1, t2;

				i = 5;

				pthread_create (&t1, NULL, thread_main, 1);
				pthread_create (&t2, NULL, thread_main, 2);

				pthread_join (t1, NULL);
				pthread_join (t2, NULL);

				return !(res1 + res2 == 2);
			}
		], [
				AC_MSG_RESULT(yes)
		], [
				AC_MSG_RESULT(no)
				with_tls=pthread
		])
	fi

	dnl **************************************
	dnl *** Checks for working sigaltstack ***
	dnl **************************************
	AC_MSG_CHECKING(for working sigaltstack)
	if test "x$with_sigaltstack" != "xyes"; then
		AC_MSG_RESULT(disabled)
	elif test "x$cross_compiling" = "xyes"; then
		AC_MSG_RESULT(cross compiling, assuming yes)
	else
		AC_TRY_RUN([
			#include <stdio.h>
			#include <stdlib.h>
			#include <unistd.h>
			#include <signal.h>
			#include <pthread.h>
			#include <sys/wait.h>
			#if defined(__FreeBSD__) || defined(__NetBSD__)
			#define SA_STACK SA_ONSTACK
			#endif
			static void
			sigsegv_signal_handler (int _dummy, siginfo_t *info, void *context)
			{
				exit (0);
			}

			volatile char*__ptr = NULL;
			static void *
			loop (void *ignored)
			{
				*__ptr = 0;
				return NULL;
			}

			static void
			child ()
			{
				struct sigaction sa;
			#ifdef __APPLE__
				stack_t sas;
			#else
				struct sigaltstack sas;
			#endif
				pthread_t id;
				pthread_attr_t attr;

				sa.sa_sigaction = sigsegv_signal_handler;
				sigemptyset (&sa.sa_mask);
				sa.sa_flags = SA_SIGINFO | SA_ONSTACK;
				if (sigaction (SIGSEGV, &sa, NULL) == -1) {
					perror ("sigaction");
					return;
				}

				/* x86 darwin deliver segfaults using SIGBUS */
				if (sigaction (SIGBUS, &sa, NULL) == -1) {
					perror ("sigaction");
					return;
				}
				sas.ss_sp = malloc (SIGSTKSZ);
				sas.ss_size = SIGSTKSZ;
				sas.ss_flags = 0;
				if (sigaltstack (&sas, NULL) == -1) {
					perror ("sigaltstack");
					return;
				}

				pthread_attr_init (&attr);
				if (pthread_create(&id, &attr, loop, &attr) != 0) {
					printf ("pthread_create\n");
					return;
				}

				sleep (100);
			}

			int
			main ()
			{
				pid_t son;
				int status;
				int i;

				son = fork ();
				if (son == -1) {
					return 1;
				}

				if (son == 0) {
					child ();
					return 0;
				}

				for (i = 0; i < 300; ++i) {
					waitpid (son, &status, WNOHANG);
					if (WIFEXITED (status) && WEXITSTATUS (status) == 0)
						return 0;
					usleep (10000);
				}

				kill (son, SIGKILL);
				return 1;
			}

		], [
				AC_MSG_RESULT(yes)
				AC_DEFINE(HAVE_WORKING_SIGALTSTACK, 1, [Have a working sigaltstack])
		], [
				with_sigaltstack=no
				AC_MSG_RESULT(no)
		])
	fi

	dnl ********************************
	dnl *** Checks for semaphore lib ***
	dnl ********************************
	# 'Real Time' functions on Solaris
	# posix4 on Solaris 2.6
	# pthread (first!) on Linux
	AC_SEARCH_LIBS(sem_init, pthread rt posix4) 

	AC_SEARCH_LIBS(shm_open, pthread rt posix4) 
	AC_CHECK_FUNCS(shm_open)

	dnl ********************************
	dnl *** Checks for timezone stuff **
	dnl ********************************
	AC_CACHE_CHECK(for tm_gmtoff in struct tm, ac_cv_struct_tm_gmtoff,
		AC_TRY_COMPILE([
			#include <time.h>
			], [
			struct tm tm;
			tm.tm_gmtoff = 1;
			], ac_cv_struct_tm_gmtoff=yes, ac_cv_struct_tm_gmtoff=no))
	if test $ac_cv_struct_tm_gmtoff = yes; then
		AC_DEFINE(HAVE_TM_GMTOFF, 1, [Have tm_gmtoff])
	else
		AC_CACHE_CHECK(for timezone variable, ac_cv_var_timezone,
			AC_TRY_COMPILE([
				#include <time.h>
			], [
				timezone = 1;
			], ac_cv_var_timezone=yes, ac_cv_var_timezone=no))
		if test $ac_cv_var_timezone = yes; then
			AC_DEFINE(HAVE_TIMEZONE, 1, [Have timezone variable])
		else
			AC_ERROR(unable to find a way to determine timezone)
		fi
	fi

	dnl *********************************
	dnl *** Checks for math functions ***
	dnl *********************************
	AC_SEARCH_LIBS(sqrtf, m)
	if test "x$has_broken_apple_cpp" != "xyes"; then
	        AC_CHECK_FUNCS(finite, , AC_MSG_CHECKING(for finite in math.h)
	                AC_TRY_LINK([#include <math.h>], 
	                [ finite(0.0); ], 
	                AC_DEFINE(HAVE_FINITE, 1, [Have finite in -lm]) AC_MSG_RESULT(yes),
	                AC_MSG_RESULT(no)))
	fi
        AC_CHECK_FUNCS(isfinite, , AC_MSG_CHECKING(for isfinite in math.h)
                AC_TRY_LINK([#include <math.h>], 
                [ isfinite(0.0); ], 
                AC_DEFINE(HAVE_ISFINITE, 1, [Have isfinite]) AC_MSG_RESULT(yes),
                AC_MSG_RESULT(no)))

	dnl ****************************************************************
	dnl *** Checks for working poll() (macosx defines it but doesn't ***
	dnl *** have it in the library (duh))                            ***
	dnl ****************************************************************
	AC_CHECK_FUNCS(poll)

	dnl *************************
	dnl *** Check for signbit ***
	dnl *************************
	AC_MSG_CHECKING(for signbit)
	AC_TRY_LINK([#include <math.h>], [
		int s = signbit(1.0);
	], [
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SIGNBIT, 1, [Have signbit])
	], [
		AC_MSG_RESULT(no)
	]) 

	dnl **********************************
	dnl *** epoll			   ***
	dnl **********************************
	if test "x$ac_cv_header_nacl_nacl_dyncode_h" = "xno"; then
		AC_CHECK_HEADERS(sys/epoll.h)
		haveepoll=no
		AC_CHECK_FUNCS(epoll_ctl, [haveepoll=yes], )
		if test "x$haveepoll" = "xyes" -a "x$ac_cv_header_sys_epoll_h" = "xyes"; then
			AC_DEFINE(HAVE_EPOLL, 1, [epoll supported])
		fi
	fi

	havekqueue=no

	AC_CHECK_HEADERS(sys/event.h)
	AC_CHECK_FUNCS(kqueue, [havekqueue=yes], )

	dnl **************************************
	dnl * Darwin has a race that prevents us from using reliably:
	dnl * http://lists.apple.com/archives/darwin-dev/2011/Jun/msg00016.html
	dnl * Since kqueue is mostly used for scaling large web servers, 
	dnl * and very few folks run Mono on large web servers on OSX, falling
	dnl * back 
	dnl **************************************
	if test "x$havekqueue" = "xyes" -a "x$ac_cv_header_sys_event_h" = "xyes"; then
		if test "x$host_darwin" = "xno"; then
			AC_DEFINE(USE_KQUEUE_FOR_THREADPOOL, 1, [Use kqueue for the threadpool])
		fi
	fi

	dnl ******************************
	dnl *** Checks for SIOCGIFCONF ***
	dnl ******************************
	AC_CHECK_HEADERS(sys/ioctl.h)
	AC_CHECK_HEADERS(net/if.h, [], [],
	   [
	   #ifdef HAVE_SYS_TYPES_H
	   # include <sys/types.h>
	   #endif
	   #ifdef HAVE_SYS_SOCKET_H
	   # include <sys/socket.h>
	   #endif
	   ])
	AC_MSG_CHECKING(for ifreq)
	AC_TRY_COMPILE([
		#include <stdio.h>
		#include <sys/ioctl.h>
		#include <net/if.h>
		], [
		struct ifconf ifc;
		struct ifreq *ifr;
		void *x;
		ifc.ifc_len = 0;
		ifc.ifc_buf = NULL;
		x = (void *) &ifr->ifr_addr;
		],[
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_SIOCGIFCONF, 1, [Can get interface list])
		], [
			AC_MSG_RESULT(no)
		])
	dnl **********************************
	dnl ***     Checks for sin_len     ***
	dnl **********************************
	AC_MSG_CHECKING(for sockaddr_in.sin_len)
	AC_TRY_COMPILE([
		#include <netinet/in.h>
		], [
		struct sockaddr_in saddr;
		saddr.sin_len = sizeof (saddr);
		],[
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_SOCKADDR_IN_SIN_LEN, 1, [sockaddr_in has sin_len])
		], [
			AC_MSG_RESULT(no)
		])	
	dnl **********************************
	dnl ***    Checks for sin6_len     ***
	dnl **********************************
	AC_MSG_CHECKING(for sockaddr_in6.sin6_len)
	AC_TRY_COMPILE([
		#include <netinet/in.h>
		], [
		struct sockaddr_in6 saddr6;
		saddr6.sin6_len = sizeof (saddr6);
		],[
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_SOCKADDR_IN6_SIN_LEN, 1, [sockaddr_in6 has sin6_len])
		], [
			AC_MSG_RESULT(no)
		])
	dnl **********************************
	dnl *** Check for getifaddrs       ***
	dnl **********************************
	AC_MSG_CHECKING(for getifaddrs)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <sys/types.h>
		#include <sys/socket.h>
		#include <ifaddrs.h>
	], [
		getifaddrs(NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETIFADDRS, 1, [Have getifaddrs])
	], [
		AC_MSG_RESULT(no)
	])
	dnl **********************************
	dnl *** Check for if_nametoindex   ***
	dnl **********************************
	AC_MSG_CHECKING(for if_nametoindex)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <sys/types.h>
		#include <sys/socket.h>
		#include <net/if.h>
	], [
		if_nametoindex(NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IF_NAMETOINDEX, 1, [Have if_nametoindex])
	], [
		AC_MSG_RESULT(no)
	])
			
	dnl **********************************
	dnl *** Checks for proclib         ***
	dnl **********************************
	AC_CHECK_HEADER(sys/errno.h, [AC_DEFINE(HAVE_SYS_ERRNO_H, 1, Define to 1 if you have the <sys/errno.h> header file.)])
	dnl **********************************
	dnl *** Checks for MonoPosixHelper ***
	dnl **********************************
	AC_CHECK_HEADERS(checklist.h)
	AC_CHECK_HEADERS(pathconf.h)
	AC_CHECK_HEADERS(fstab.h)
	AC_CHECK_HEADERS(attr/xattr.h)
	AC_CHECK_HEADERS(sys/extattr.h)
	AC_CHECK_HEADERS(sys/sendfile.h)
	AC_CHECK_HEADERS(sys/statvfs.h)
	AC_CHECK_HEADERS(sys/statfs.h)
	AC_CHECK_HEADERS(sys/vfstab.h)
	AC_CHECK_HEADERS(sys/xattr.h)
	AC_CHECK_HEADERS(sys/mman.h)
	AC_CHECK_HEADERS(sys/param.h)
	AC_CHECK_HEADERS(sys/mount.h, [], [],
		[
		#ifdef HAVE_SYS_PARAM_H
		# include <sys/param.h>
		#endif
		])
	AC_CHECK_HEADERS(sys/mount.h)
	AC_CHECK_FUNCS(confstr)
	AC_CHECK_FUNCS(seekdir telldir)
	AC_CHECK_FUNCS(getdomainname)
	AC_CHECK_FUNCS(setdomainname)
	AC_CHECK_FUNCS(endgrent getgrent fgetgrent setgrent)
	AC_CHECK_FUNCS(setgroups)
	AC_CHECK_FUNCS(endpwent getpwent fgetpwent setpwent)
	AC_CHECK_FUNCS(getfsstat)
	AC_CHECK_FUNCS(lutimes futimes)
	AC_CHECK_FUNCS(mremap)
	AC_CHECK_FUNCS(remap_file_pages)
	AC_CHECK_FUNCS(posix_fadvise)
	AC_CHECK_FUNCS(posix_fallocate)
	AC_CHECK_FUNCS(posix_madvise)
	AC_CHECK_FUNCS(vsnprintf)
	AC_CHECK_FUNCS(sendfile)
	AC_CHECK_FUNCS(gethostid sethostid)
	AC_CHECK_FUNCS(sethostname)
	AC_CHECK_FUNCS(statfs)
	AC_CHECK_FUNCS(fstatfs)
	AC_CHECK_FUNCS(statvfs)
	AC_CHECK_FUNCS(fstatvfs)
	AC_CHECK_FUNCS(stime)
	AC_CHECK_FUNCS(strerror_r)
	AC_CHECK_FUNCS(ttyname_r)
	AC_CHECK_FUNCS(psignal)
	AC_CHECK_FUNCS(getlogin_r)
	AC_CHECK_FUNCS(lockf)
	AC_CHECK_FUNCS(swab)
	AC_CHECK_FUNCS(setusershell endusershell)
	AC_CHECK_FUNCS(futimens utimensat)
	AC_CHECK_FUNCS(fstatat mknodat readlinkat)
	AC_CHECK_FUNCS(readv writev preadv pwritev)
	AC_CHECK_FUNCS(setpgid)
	AC_CHECK_FUNCS(system)
	AC_CHECK_FUNCS(fork execv execve)
	AC_CHECK_FUNCS(accept4)
	AC_CHECK_FUNCS(localtime_r)
	AC_CHECK_FUNCS(mkdtemp)
	AC_CHECK_SIZEOF(size_t)
	AC_CHECK_TYPES([blksize_t], [AC_DEFINE(HAVE_BLKSIZE_T)], , 
		[#include <sys/types.h>
		 #include <sys/stat.h>
		 #include <unistd.h>])
	AC_CHECK_TYPES([blkcnt_t], [AC_DEFINE(HAVE_BLKCNT_T)], ,
		[#include <sys/types.h>
		 #include <sys/stat.h>
		 #include <unistd.h>])
	AC_CHECK_TYPES([suseconds_t], [AC_DEFINE(HAVE_SUSECONDS_T)], ,
		[#include <sys/time.h>])
	AC_CHECK_TYPES([struct cmsghdr], [AC_DEFINE(HAVE_STRUCT_CMSGHDR)], ,
		[#include <sys/socket.h>])
	AC_CHECK_TYPES([struct flock], [AC_DEFINE(HAVE_STRUCT_FLOCK)], ,
		[#include <unistd.h>
		 #include <fcntl.h>])
	AC_CHECK_TYPES([struct iovec], [AC_DEFINE(HAVE_STRUCT_IOVEC)], ,
		[#include <sys/uio.h>])
	AC_CHECK_TYPES([struct linger], [AC_DEFINE(HAVE_STRUCT_LINGER)], ,
		[#include <sys/socket.h>])
	AC_CHECK_TYPES([struct pollfd], [AC_DEFINE(HAVE_STRUCT_POLLFD)], ,
		[#include <sys/poll.h>])
	AC_CHECK_TYPES([struct sockaddr], [AC_DEFINE(HAVE_STRUCT_SOCKADDR)], ,
		[#include <sys/socket.h>])
	AC_CHECK_TYPES([struct sockaddr_storage], [AC_DEFINE(HAVE_STRUCT_SOCKADDR_STORAGE)], ,
		[#include <sys/socket.h>])
	AC_CHECK_TYPES([struct sockaddr_in], [AC_DEFINE(HAVE_STRUCT_SOCKADDR_IN)], ,
		[#include <netinet/in.h>])
	AC_CHECK_TYPES([struct sockaddr_in6], [AC_DEFINE(HAVE_STRUCT_SOCKADDR_IN6)], ,
		[#include <netinet/in.h>])
	AC_CHECK_TYPES([struct sockaddr_un], [AC_DEFINE(HAVE_STRUCT_SOCKADDR_UN)], ,
		[#include <sys/un.h>])
	AC_CHECK_TYPES([struct stat], [AC_DEFINE(HAVE_STRUCT_STAT)], ,
		[#include <sys/types.h>
		 #include <sys/stat.h>
		 #include <unistd.h>])
	AC_CHECK_TYPES([struct timespec], [AC_DEFINE(HAVE_STRUCT_TIMESPEC)], ,
		[#include <time.h>])
	AC_CHECK_TYPES([struct timeval], [AC_DEFINE(HAVE_STRUCT_TIMEVAL)], ,
		[#include <sys/time.h>
		 #include <sys/types.h>
		 #include <utime.h>])
	AC_CHECK_TYPES([struct timezone], [AC_DEFINE(HAVE_STRUCT_TIMEZONE)], ,
		[#include <sys/time.h>])
	AC_CHECK_TYPES([struct utimbuf], [AC_DEFINE(HAVE_STRUCT_UTIMBUF)], ,
		[#include <sys/types.h>
		 #include <utime.h>])
	AC_CHECK_MEMBERS(
		[struct dirent.d_off, struct dirent.d_reclen, struct dirent.d_type],,, 
		[#include <sys/types.h>
		 #include <dirent.h>])
	AC_CHECK_MEMBERS(
		[struct passwd.pw_gecos],,, 
		[#include <sys/types.h>
		 #include <pwd.h>])
	AC_CHECK_MEMBERS(
		[struct statfs.f_flags],,, 
		[#include <sys/types.h>
		 #include <sys/vfs.h>])
	AC_CHECK_MEMBERS(
		[struct stat.st_atim, struct stat.st_mtim, struct stat.st_ctim],,, 
		[#include <sys/types.h>
		 #include <sys/stat.h>
		 #include <unistd.h>])

	dnl Favour xattr through glibc, but use libattr if we have to
	AC_CHECK_FUNC(lsetxattr, ,
		AC_CHECK_LIB(attr, lsetxattr, XATTR_LIB="-lattr",)
	)
	AC_SUBST(XATTR_LIB)

	dnl kinfo_proc.kp_proc works on darwin but fails on other simil-bsds
	AC_CHECK_MEMBERS(
		[struct kinfo_proc.kp_proc],,, 
		[#include <sys/types.h>
		 #include <sys/param.h>
		 #include <sys/sysctl.h>
		 #include <sys/proc.h>
		 ])

	dnl *********************************
	dnl *** Checks for Windows compilation ***
	dnl *********************************
	AC_CHECK_HEADERS(sys/time.h)
	AC_CHECK_HEADERS(sys/param.h)
	AC_CHECK_HEADERS(dirent.h)

	dnl ******************************************
	dnl *** Checks for OSX and iOS compilation ***
	dnl ******************************************
	AC_CHECK_HEADERS(CommonCrypto/CommonDigest.h)

	dnl *********************************
	dnl *** Check for Console 2.0 I/O ***
	dnl *********************************
	AC_CHECK_HEADERS([curses.h])
	AC_CHECK_HEADERS([term.h], [], [],
	[#if HAVE_CURSES_H
	 #include <curses.h>
	 #endif
	])
	AC_CHECK_HEADERS([termios.h])
else
	dnl *********************************
	dnl *** Checks for Windows compilation ***
	dnl *********************************
	AC_CHECK_HEADERS(winternl.h)

	jdk_headers_found=no
	AC_CHECK_LIB(ws2_32, main, LIBS="$LIBS -lws2_32", AC_ERROR(bad mingw install?))
	AC_CHECK_LIB(psapi, main, LIBS="$LIBS -lpsapi", AC_ERROR(bad mingw install?))
	AC_CHECK_LIB(ole32, main, LIBS="$LIBS -lole32", AC_ERROR(bad mingw install?))
	AC_CHECK_LIB(winmm, main, LIBS="$LIBS -lwinmm", AC_ERROR(bad mingw install?))
	AC_CHECK_LIB(oleaut32, main, LIBS="$LIBS -loleaut32", AC_ERROR(bad mingw install?))
	AC_CHECK_LIB(advapi32, main, LIBS="$LIBS -ladvapi32", AC_ERROR(bad mingw install?))
	AC_CHECK_LIB(version, main, LIBS="$LIBS -lversion", AC_ERROR(bad mingw install?))

	dnl *********************************
	dnl *** Check for struct ip_mreqn ***
	dnl *********************************
	AC_MSG_CHECKING(for struct ip_mreqn)
	AC_TRY_COMPILE([#include <ws2tcpip.h>], [
		struct ip_mreqn mreq;
		mreq.imr_address.s_addr = 0;
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_STRUCT_IP_MREQN)
	], [
		# We'll just have to try and use struct ip_mreq
		AC_MSG_RESULT(no)
		AC_MSG_CHECKING(for struct ip_mreq)
		AC_TRY_COMPILE([#include <ws2tcpip.h>], [
			struct ip_mreq mreq;
			mreq.imr_interface.s_addr = 0;
		], [
			# Yes, we have it...
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_STRUCT_IP_MREQ)
		], [
			# No multicast support
			AC_MSG_RESULT(no)
		])
	])

	dnl **********************************
	dnl *** Check for getaddrinfo ***
	dnl **********************************
	AC_MSG_CHECKING(for getaddrinfo)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <winsock2.h>
		#include <ws2tcpip.h>
	], [
		getaddrinfo(NULL,NULL,NULL,NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETADDRINFO, 1, [Have getaddrinfo])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for gethostbyname ***
	dnl **********************************
	AC_MSG_CHECKING(for gethostbyname)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <winsock2.h>
		#include <ws2tcpip.h>
	], [
		gethostbyname(NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETHOSTBYNAME, 1, [Have gethostbyname])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for getprotobyname ***
	dnl **********************************
	AC_MSG_CHECKING(for getprotobyname)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <winsock2.h>
		#include <ws2tcpip.h>
	], [
		getprotobyname(NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETPROTOBYNAME, 1, [Have getprotobyname])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for getnameinfo ***
	dnl **********************************
	AC_MSG_CHECKING(for getnameinfo)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <winsock2.h>
		#include <ws2tcpip.h>
	], [
		getnameinfo (NULL, 0, NULL, 0, NULL, 0, 0);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETNAMEINFO, 1, [Have getnameinfo])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for inet_ntop ***
	dnl **********************************
	AC_MSG_CHECKING(for inet_ntop)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <winsock2.h>
		#include <ws2tcpip.h>
	], [
		inet_ntop (0, NULL, NULL, 0);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_INET_NTOP, 1, [Have inet_ntop])
	], [
		AC_MSG_RESULT(no)
	])

	dnl **********************************
	dnl *** Check for inet_pton ***
	dnl **********************************
	AC_MSG_CHECKING(for inet_pton)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <winsock2.h>
		#include <ws2tcpip.h>
	], [
		#ifndef inet_pton
		(void) inet_pton;
		#endif
		inet_pton (0, NULL, NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_INET_PTON, 1, [Have inet_pton])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_DECLS(InterlockedExchange64, [], [], [[#include <windows.h>]])
	AC_CHECK_DECLS(InterlockedCompareExchange64, [], [], [[#include <windows.h>]])
	AC_CHECK_DECLS(InterlockedDecrement64, [], [], [[#include <windows.h>]])
	AC_CHECK_DECLS(InterlockedIncrement64, [], [], [[#include <windows.h>]])
	AC_CHECK_DECLS(InterlockedAdd, [], [], [[#include <windows.h>]])
	AC_CHECK_DECLS(InterlockedAdd64, [], [], [[#include <windows.h>]])
	AC_CHECK_DECLS(__readfsdword, [], [], [[#include <windows.h>]])
fi

dnl socklen_t check
AC_MSG_CHECKING(for socklen_t)
AC_TRY_COMPILE([
#include <sys/types.h>
#include <sys/socket.h>
],[
  socklen_t foo;
],[
ac_cv_c_socklen_t=yes
	AC_DEFINE(HAVE_SOCKLEN_T, 1, [Have socklen_t])
	AC_MSG_RESULT(yes)
],[
	AC_MSG_RESULT(no)
])

AC_MSG_CHECKING(for array element initializer support)
AC_TRY_COMPILE([#include <sys/socket.h>], [
	const int array[] = {[1] = 2,};
], [
	# Yes, we have it...
	AC_MSG_RESULT(yes)
	AC_DEFINE(HAVE_ARRAY_ELEM_INIT,1,[Supports C99 array initialization])
], [
	# We'll have to use signals
	AC_MSG_RESULT(no)
])

AC_CHECK_FUNCS(trunc, , AC_MSG_CHECKING(for trunc in math.h)
	# Simply calling trunc (0.0) is no good since gcc will optimize the call away
	AC_TRY_LINK([#include <math.h>], 
	[ static void *p = &trunc; ],
	[
		AC_DEFINE(HAVE_TRUNC) 
		AC_MSG_RESULT(yes)
		ac_cv_trunc=yes
	],
	AC_MSG_RESULT(no)))

if test "x$ac_cv_truncl" != "xyes"; then
   AC_CHECK_LIB(sunmath, aintl, [ AC_DEFINE(HAVE_AINTL, 1, [Has the 'aintl' function]) LIBS="$LIBS -lsunmath"])
fi

AC_CHECK_FUNCS(execvp)

dnl ****************************
dnl *** Look for /dev/random ***
dnl ****************************

AC_MSG_CHECKING([if usage of random device is requested])
AC_ARG_ENABLE(dev-random,
[  --disable-dev-random    disable the use of the random device (enabled by default)],
try_dev_random=$enableval, try_dev_random=yes)

AC_MSG_RESULT($try_dev_random)

case "{$build}" in
    *-openbsd*)
    NAME_DEV_RANDOM="/dev/srandom"
    ;;

dnl Win32 does not have /dev/random, they have their own method...

    *-mingw*|*-*-cygwin*)
    ac_cv_have_dev_random=no
    ;;

dnl Everywhere else, it's /dev/random

    *)
    NAME_DEV_RANDOM="/dev/random"
    ;;
esac

AC_DEFINE_UNQUOTED(NAME_DEV_RANDOM, "$NAME_DEV_RANDOM", [Name of /dev/random])

dnl Now check if the device actually exists

if test "x$try_dev_random" = "xyes"; then
    AC_CACHE_CHECK(for random device, ac_cv_have_dev_random,
    [if test -r "$NAME_DEV_RANDOM" ; then
        ac_cv_have_dev_random=yes; else ac_cv_have_dev_random=no; fi])
    if test "x$ac_cv_have_dev_random" = "xyes"; then
        AC_DEFINE(HAVE_CRYPT_RNG, 1, [Have /dev/random])
    fi
else
    AC_MSG_CHECKING(for random device)
    ac_cv_have_dev_random=no
    AC_MSG_RESULT(has been disabled)
fi

if test "x$host_win32" = "xyes"; then
    AC_DEFINE(HAVE_CRYPT_RNG)
fi

if test "x$ac_cv_have_dev_random" = "xno" \
    && test "x$host_win32" = "xno"; then
    AC_MSG_WARN([[
***
*** A system-provided entropy source was not found on this system.
*** Because of this, the System.Security.Cryptography random number generator
*** will throw a NotImplemented exception.
***
*** If you are seeing this message, and you know your system DOES have an
*** entropy collection in place, please contact <crichton@gimp.org> and
*** provide information about the system and how to access the random device.
***
*** Otherwise you can install either egd or prngd and set the environment
*** variable MONO_EGD_SOCKET to point to the daemon's socket to use that.
***]])
fi

AC_ARG_ENABLE(bcl-opt, [  --disable-bcl-opt	BCL is compiled with no optimizations (allows accurate BCL debugging)], test_bcl_opt=$enableval, test_bcl_opt=yes)

AC_MSG_CHECKING([if big-arrays are to be enabled])
AC_ARG_ENABLE(big-arrays,  [  --enable-big-arrays	Enable the allocation and indexing of arrays greater than Int32.MaxValue], enable_big_arrays=$enableval, enable_big_arrays=no)
if test "x$enable_big_arrays" = "xyes" ; then
    if  test "x$ac_cv_sizeof_void_p" = "x8"; then
	AC_DEFINE(MONO_BIG_ARRAYS,1,[Enable the allocation and indexing of arrays greater than Int32.MaxValue])
    else
        AC_MSG_ERROR([The allocation and indexing of arrays greater than Int32.MaxValue is not supported on this platform.])
    fi
fi
AC_MSG_RESULT($enable_big_arrays)

dnl **************
dnl *** DTRACE ***
dnl **************

AC_ARG_ENABLE(dtrace,[  --enable-dtrace	Enable DTrace probes], enable_dtrace=$enableval, enable_dtrace=$has_dtrace)

if test "x$enable_dtrace" = "xyes"; then
   if test "x$has_dtrace" = "xno"; then
   	  AC_MSG_ERROR([DTrace probes are not supported on this platform.])
   fi
   AC_PATH_PROG(DTRACE, [dtrace], [no], [$PATH:/usr/sbin])
   if test "x$DTRACE" = "xno"; then
   	  AC_MSG_RESULT([dtrace utility not found, dtrace support disabled.])
	  enable_dtrace=no
   elif ! $DTRACE -h -s $srcdir/data/mono.d > /dev/null 2>&1; then
	  AC_MSG_RESULT([dtrace doesn't support -h option, dtrace support disabled.])
	  enable_dtrace=no
   fi
fi

dtrace_g=no
if test "x$enable_dtrace" = "xyes"; then
	AC_DEFINE(ENABLE_DTRACE, 1, [Enable DTrace probes])
	DTRACEFLAGS=
	if test "x$ac_cv_sizeof_void_p" = "x8"; then
		case "$host" in
			powerpc-*-darwin*)
			DTRACEFLAGS="-arch ppc64"
			;;
			i*86-*-darwin*)
			DTRACEFLAGS="-arch x86_64"
			;;
			*)
			DTRACEFLAGS=-64
			;;
		esac
	else
		case "$host" in
			powerpc-*-darwin*)
			DTRACEFLAGS="-arch ppc"
			;;
			i*86-*-darwin*)
			DTRACEFLAGS="-arch i386"
			;;
			*)
			DTRACEFLAGS=-32
			;;
		esac
	fi
	AC_SUBST(DTRACEFLAGS)
	case "$host" in
		*-*-solaris*)
		dtrace_g=yes
		;;
	esac
	AC_CHECK_HEADERS([sys/sdt.h])
fi
AM_CONDITIONAL(ENABLE_DTRACE, [test x$enable_dtrace = xyes])
AM_CONDITIONAL(DTRACE_G_REQUIRED, [test x$dtrace_g = xyes])

dnl **************
dnl ***  NaCl  ***
dnl **************

AC_ARG_ENABLE(nacl_codegen, [  --enable-nacl-codegen      Enable Native Client code generation], enable_nacl_codegen=$enableval, enable_nacl_codegen=no)
AC_ARG_ENABLE(nacl_gc, [  --enable-nacl-gc           Enable Native Client garbage collection], enable_nacl_gc=$enableval, enable_nacl_gc=no)

AM_CONDITIONAL(NACL_CODEGEN, test x$enable_nacl_codegen != xno)

dnl
dnl Hack to use system mono for operations in build/install not allowed in NaCl.
dnl
nacl_self_host=""
if test "x$ac_cv_header_nacl_nacl_dyncode_h" = "xyes"; then
   nacl_self_host="nacl_self_host"
fi
AC_SUBST(nacl_self_host)

if test "x$enable_nacl_codegen" = "xyes"; then
   MONO_NACL_ALIGN_MASK_OFF=1
   AC_DEFINE(TARGET_NACL, 1, [...])
   AC_DEFINE(__native_client_codegen__, 1, [...])
fi
if test "x$enable_nacl_gc" = "xyes"; then
   if test "x$TARGET" = "xAMD64" -o "x$TARGET" = "xX86"; then
      INSTRUMENT_CFLAG="-finstrument-for-thread-suspension"
   else
      # Not yet implemented
      INSTRUMENT_CFLAG=""
   fi
   CPPFLAGS="$CPPFLAGS $INSTRUMENT_CFLAG -D__native_client_gc__"
fi
AC_SUBST(MONO_NACL_ALIGN_MASK_OFF)

dnl **************************
dnl *** AOT cross offsets  ***
dnl **************************

AC_ARG_WITH(cross-offsets, [  --with-cross-offsets=<offsets file path>    Explicit AOT cross offsets file],
    AC_DEFINE_UNQUOTED(MONO_OFFSETS_FILE, "$withval", [AOT cross offsets file]))

dnl **************
dnl ***  LLVM  ***
dnl **************

AC_ARG_ENABLE(llvm,[  --enable-llvm	Enable the LLVM back-end], enable_llvm=$enableval, enable_llvm=default)
AC_ARG_ENABLE(loadedllvm,[  --enable-loadedllvm	Load the LLVM back-end dynamically], enable_llvm=$enableval && enable_loadedllvm=$enableval, enable_loadedllvm=no)
AC_ARG_ENABLE(llvm-version-check,[  --enable-llvm-version-check Check that the LLVM matches the version expected by mono], enable_llvm_version_check=$enableval, enable_llvm_version_check=no)
AC_ARG_ENABLE(llvm-runtime,[  --enable-llvm-runtime	Enable runtime support for llvmonly code], enable_llvm_runtime=$enableval, enable_llvm_runtime=no)

AC_ARG_WITH(llvm, [  --with-llvm=<llvm prefix>    Enable the LLVM back-end], enable_llvm=yes,)

if test "x$enable_llvm" = "xdefault"; then
   enable_llvm=$enable_llvm_default
fi

if test "x$enable_llvm" = "xyes"; then
   if test "x$with_llvm" != "x"; then
   	  LLVM_CONFIG=$with_llvm/bin/llvm-config
   	  if test x$host_win32 = xyes; then
   	  	LLVM_CONFIG=$LLVM_CONFIG.exe
   	  fi
	  if test ! -x $LLVM_CONFIG; then
	  	 AC_MSG_ERROR([LLVM executable $LLVM_CONFIG not found.])
      fi
   else
      AC_PATH_PROG(LLVM_CONFIG, llvm-config, no)
      if test "x$LLVM_CONFIG" = "xno"; then
   	     AC_MSG_ERROR([llvm-config not found.])
      fi
   fi

   llvm_codegen="x86codegen"
   case "$target" in
   arm*)
		llvm_codegen="armcodegen"
		;;
   esac

   if test "x$host_win32" = "xno"; then

   # Should be something like '2.6' or '2.7svn'
   llvm_version=`$LLVM_CONFIG --version`
   llvm_api_version=`$LLVM_CONFIG --mono-api-version 2>/dev/null`
   AC_MSG_CHECKING(LLVM version)
   AC_MSG_RESULT($llvm_version $llvm_api_version)
   if echo $llvm_version | grep -q 'mono'; then
	  if test "x$enable_llvm_version_check" = "xyes"; then
	  	 if test "$llvm_version" != "$expected_llvm_version"; then
		 	AC_MSG_ERROR([Expected llvm version $expected_llvm_version, but llvm-config --version returned $llvm_version"])
	  	 fi
	  fi
   else
	  AC_MSG_ERROR([Compiling with stock LLVM is not supported, please use the Mono LLVM repo at https://github.com/mono/llvm, with the GIT branch which matches this version of mono, i.e. 'mono-2-10' for Mono 2.10.])
   fi

   # The output of --cflags seems to include optimizations flags too
   if test $llvm_api_version -gt 100; then
   	  # The --cflags argument includes all kinds of warnings -pendantic etc.
	  LLVM_CFLAGS="-I$with_llvm/include -D__STDC_CONSTANT_MACROS -D__STDC_FORMAT_MACROS -D__STDC_LIMIT_MACROS"
	  LLVM_CXXFLAGS="-I$with_llvm/include -std=c++11 -D__STDC_CONSTANT_MACROS -D__STDC_FORMAT_MACROS -D__STDC_LIMIT_MACROS"
   else
	  LLVM_CFLAGS=`$LLVM_CONFIG --cflags | sed -e 's/-O2//g' | sed -e 's/-O0//g' | sed -e 's/-fomit-frame-pointer//g' | sed -e 's/-fPIC//g'`
      LLVM_CXXFLAGS=`$LLVM_CONFIG --cxxflags | sed -e 's/-O2//g' | sed -e 's/-O0//g' | sed -e 's/-fomit-frame-pointer//g' | sed -e 's/-fPIC//g'`
   fi
   # LLVM is compiled with -fno-rtti, so we need this too, since our classes inherit
   # from LLVM classes.
   LLVM_CXXFLAGS="$LLVM_CXXFLAGS -fno-rtti -fexceptions"
   LLVM_LDFLAGS=`$LLVM_CONFIG --ldflags`
   # This might include empty lines
   LLVM_SYSTEM_LIBS=`$LLVM_CONFIG --system-libs 2>/dev/null | grep -- -`
   llvm_jit_supported=yes
   llvm_jit_libs="jit mcjit $llvm_codegen"
   if test "x$host" != "x$target"; then
      # No need for jit libs
	  llvm_jit_supported=no
      llvm_jit_libs=""
   elif test $llvm_api_version -gt 100; then
      llvm_jit_libs="orcjit $llvm_codegen"
   fi
   LLVM_LIBS=`$LLVM_CONFIG --libs analysis core bitwriter $llvm_jit_libs`
   if test "x$LLVM_LIBS" = "x"; then
   	  echo "$LLVM_CONFIG --libs failed."
	  exit 1
   fi
   LLVM_LIBS="$LLVM_LIBS $LLVM_LDFLAGS $LLVM_SYSTEM_LIBS"
   # The c++ standard library used by llvm doesn't show up in $LLVM_SYSTEM_LIBS so add it manually
   if echo $LLVM_CXXFLAGS | grep -q -- '-stdlib=libc++'; then
      LLVM_LIBS="$LLVM_LIBS -lc++"
   else
      LLVM_LIBS="$LLVM_LIBS -lstdc++"
   fi

   expected_llvm_version="3.4svn-mono-mono/e656cac"

   else
       LLVM_CFLAGS="-I$with_llvm/include -DNDEBUG -D__NO_CTYPE_INLINE -D_GNU_SOURCE -D__STDC_CONSTANT_MACROS -D__STDC_FORMAT_MACROS -D__STDC_LIMIT_MACROS"
       LLVM_CXXFLAGS="$LLVM_CFLAGS -std=gnu++11 -fvisibility-inlines-hidden -fno-rtti -Woverloaded-virtual -Wcast-qual"
       LLVM_LDFLAGS="-L$with_llvm/lib"
       LLVM_SYSTEM_LIBS="-lshell32 -lpsapi -limagehlp -ldbghelp -lm"
       LLVM_LIBS="-lLLVMLTO -lLLVMObjCARCOpts -lLLVMLinker -lLLVMipo -lLLVMVectorize -lLLVMBitWriter \
         -lLLVMARMDisassembler -lLLVMARMCodeGen -lLLVMARMAsmParser -lLLVMARMDesc -lLLVMARMInfo \
         -lLLVMARMAsmPrinter -lLLVMTableGen -lLLVMDebugInfo -lLLVMOption -lLLVMX86Disassembler \
         -lLLVMX86AsmParser -lLLVMX86CodeGen -lLLVMSelectionDAG -lLLVMAsmPrinter -lLLVMX86Desc \
         -lLLVMMCDisassembler -lLLVMX86Info -lLLVMX86AsmPrinter -lLLVMX86Utils -lLLVMJIT \
         -lLLVMAnalysis -lLLVMTarget \
         -lLLVMIRReader -lLLVMAsmParser -lLLVMLineEditor -lLLVMMCAnalysis -lLLVMInstrumentation \
         -lLLVMInterpreter -lLLVMCodeGen -lLLVMScalarOpts -lLLVMInstCombine -lLLVMTransformUtils \
         -lLLVMipa -lLLVMAnalysis -lLLVMProfileData -lLLVMMCJIT -lLLVMTarget -lLLVMRuntimeDyld \
         -lLLVMObject -lLLVMMCParser -lLLVMBitReader -lLLVMExecutionEngine -lLLVMMC -lLLVMCore \
         -lLLVMSupport -lstdc++"
       LLVM_LIBS="$LLVM_LIBS $LLVM_SYSTEM_LIBS"

       llvm_config_path=$with_llvm/include/llvm/Config/llvm-config.h
       llvm_api_version=`awk '/MONO_API_VERSION/ { print $3 }' $llvm_config_path`
   fi  

   if test "x$llvm_api_version" = "x"; then
	  LLVM_CFLAGS="$LLVM_CFLAGS -DLLVM_API_VERSION=0"
	  LLVM_CXXFLAGS="$LLVM_CXXFLAGS -DLLVM_API_VERSION=0"
   else
	  LLVM_CFLAGS="$LLVM_CFLAGS -DLLVM_API_VERSION=$llvm_api_version"
	  LLVM_CXXFLAGS="$LLVM_CXXFLAGS -DLLVM_API_VERSION=$llvm_api_version"
   fi   

   AC_DEFINE_UNQUOTED(LLVM_VERSION, "$llvm_version", [Full version of LLVM libraries])

   AC_SUBST(LLVM_CFLAGS)
   AC_SUBST(LLVM_CXXFLAGS)
   AC_SUBST(LLVM_LIBS)
   AC_SUBST(LLVM_LDFLAGS)
   AC_DEFINE(ENABLE_LLVM, 1, [Enable the LLVM back end])
fi

AM_CONDITIONAL(ENABLE_LLVM, [test x$enable_llvm = xyes])
if test "x$enable_loadedllvm" = "xyes"; then
   AC_DEFINE(MONO_LLVM_LOADED, 1, [The LLVM back end is dynamically loaded])
fi
AM_CONDITIONAL(LOADED_LLVM, [test x$enable_loadedllvm = xyes])

if test "x$enable_llvm" = "xyes"; then
   enable_llvm_runtime=yes
fi
if test "x$enable_llvm_runtime" = "xyes"; then
   AC_DEFINE(ENABLE_LLVM_RUNTIME, 1, [Runtime support code for llvm enabled])
fi
AM_CONDITIONAL(ENABLE_LLVM_RUNTIME, [test x$enable_llvm_runtime = xyes])

TARGET="unknown"
ACCESS_UNALIGNED="yes"

LIBC="libc.so.6"
INTL="libc.so.6"
SQLITE="libsqlite.so.0"
SQLITE3="libsqlite3.so.0"
X11="libX11.so"
GDKX11="libgdk-x11-2.0.so.0"
GTKX11="libgtk-x11-2.0.so.0"
XINERAMA="libXinerama.so.1"

sizeof_register="SIZEOF_VOID_P"

jit_wanted=true
boehm_supported=true
BTLS_SUPPORTED=no
BTLS_PLATFORM=
case "$host" in
	mips*)
		TARGET=MIPS;
		arch_target=mips;
		with_tls=pthread;
		ACCESS_UNALIGNED="no"

		AC_MSG_CHECKING(for mips n32)
		AC_TRY_COMPILE([],[
		#if _MIPS_SIM != _ABIN32
		#error Not mips n32
		#endif
		return 0;
		],[
		AC_MSG_RESULT(yes)
		sizeof_register=8
		],[
		AC_MSG_RESULT(no)
		])
		;;
	i*86-*-*)
		TARGET=X86;
		arch_target=x86;
		case $host_os in
		  solaris*)
			LIBC="libc.so"
			INTL="libintl.so"
			if test "x$ac_cv_sizeof_void_p" = "x8"; then
				TARGET=AMD64
				arch_target=amd64
			fi

			# On solaris 10 x86, gcc prints a warning saying 'visibility attribute not supported on this configuration; ignored', but linking fails. A test case:
			# int astruct __attribute__ ((visibility ("hidden")));
			# void foo ()
			# {
			#	void *p = &astruct;
			# }
			# gcc -fPIC --shared -o libfoo.so foo.c
			# yields:
			# foo.c:6: warning: visibility attribute not supported in this configuration; ignored
			# ld: fatal: relocation error: R_386_GOTOFF: file /var/tmp//ccxYR96k.o: symbol astruct: relocation must bind locally
			have_visibility_hidden=no
			;;
		  mingw*|cygwin*)
			have_visibility_hidden=no		  
			;;
		  haiku*)
			LIBC=libroot.so
			;;
		  linux*)
			AOT_SUPPORTED="yes"
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=i386
			AC_CHECK_HEADER(stdalign.h,[],[BTLS_SUPPORTED=no])
			;;
		  darwin*)
			AOT_SUPPORTED="yes"
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=i386
			;;
		  openbsd*|freebsd*|kfreebsd-gnu*)
		        ;;
		esac
		;;
	x86_64-*-* | amd64-*-*)
		TARGET=AMD64;
		arch_target=amd64;
		if test "x$ac_cv_sizeof_void_p" = "x4"; then
			AC_DEFINE(__mono_ilp32__, 1, [64 bit mode with 4 byte longs and pointers])
			sizeof_register=8
		fi
		case $host_os in
		  linux*)
			AOT_SUPPORTED="yes"
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=x86_64
			AC_CHECK_HEADER(stdalign.h,[],[BTLS_SUPPORTED=no])
			;;
		  darwin*)
			AOT_SUPPORTED="yes"
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=x86_64
			boehm_supported=false
			;;
		  openbsd*|freebsd*|kfreebsd-gnu*)
			;;
		  mingw*)
			;;
		esac
		case "$host" in
			x86_64-*-nacl*)
				AC_DEFINE(__mono_ilp32__, 1, [64 bit mode with 4 byte longs and pointers])
				sizeof_register=8
				;;
		esac
		;;
	sparc*-*-*)
		if test "x$ac_cv_sizeof_void_p" = "x8"; then
		   TARGET=SPARC64
		else
			TARGET=SPARC
		fi
		arch_target=sparc;
		ACCESS_UNALIGNED="no"
		case $host_os in
		  linux*) ;;
		  *)
			LIBC="libc.so"
			INTL="libintl.so"
		esac
		if test x"$GCC" = xyes; then
			# We don't support v8 cpus
			CFLAGS="$CFLAGS -Wno-cast-align -mcpu=v9"
		fi
		if test x"$AR" = xfalse; then
			AC_MSG_ERROR([The required utility 'ar' is not found in your PATH. Usually it can be found in /usr/ccs/bin.])
		fi
		;;
	*-mingw*|*-*-cygwin*)
		# When this is enabled, it leads to very strange crashes at runtime (gcc-3.4.4)
		have_visibility_hidden=no
		INTL="intl"
		;;
	macppc-*-openbsd* | powerpc*-*-linux* | powerpc-*-openbsd* | \
        powerpc-*-sysv* | powerpc-*-darwin* | powerpc-*-netbsd* | powerpc-*-freebsd* )
		if test "x$ac_cv_sizeof_void_p" = "x8"; then
			TARGET=POWERPC64;
			CPPFLAGS="$CPPFLAGS -D__mono_ppc__ -D__mono_ppc64__"
			CFLAGS="$CFLAGS -mminimal-toc"
		else
			TARGET=POWERPC;
			CPPFLAGS="$CPPFLAGS -D__mono_ppc__"
		fi
		arch_target=ppc;
		case $host_os in
		  linux*|darwin*)
			;;
		esac
		;;
	armv7k-*-darwin*)
		TARGET=ARM;
		TARGET_SYS=WATCHOS
		arch_target=arm;
		arm_fpu=VFP_HARD
		ACCESS_UNALIGNED="no"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		;;

	arm*-darwin*)
		TARGET=ARM;
		arch_target=arm;
		ACCESS_UNALIGNED="no"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		;;
	arm*-linux*)
		TARGET=ARM;
		arch_target=arm;
		ACCESS_UNALIGNED="no"
		AOT_SUPPORTED="yes"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=arm
		AC_CHECK_HEADER(stdalign.h,[],[BTLS_SUPPORTED=no])
		case "$target" in
		arm*-linux*-gnueabi)
			BTLS_PLATFORM=armsoft
			;;
		esac
		;;
	arm*-netbsd*-eabi*)
		TARGET=ARM;
		arch_target=arm;
		ACCESS_UNALIGNED="no"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		;;
# TODO: make proper support for NaCl host.
#        arm*-*nacl)
#		TARGET=ARM;
#		arch_target=arm;
#		ACCESS_UNALIGNED="no"
#		AOT_SUPPORTED="no"
#		;;
	aarch64-*)
		# https://lkml.org/lkml/2012/7/15/133
		TARGET=ARM64
		arch_target=arm64
		boehm_supported=false
		AOT_SUPPORTED="yes"
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=aarch64
		AC_CHECK_HEADER(stdalign.h,[],[BTLS_SUPPORTED=no])
		;;
	s390x-*-linux*)
		TARGET=S390X;
		arch_target=s390x;
		ACCESS_UNALIGNED="yes"
		CFLAGS="$CFLAGS -mbackchain -D__USE_STRING_INLINES"
		;;
esac

HOST=$TARGET

if test "x$host" != "x$target"; then
   AC_DEFINE(MONO_CROSS_COMPILE,1,[The runtime is compiled for cross-compiling mode])
   enable_mcs_build=no
   BTLS_SUPPORTED=no
   case "$target" in
   arm*-darwin*)
		TARGET=ARM;
		arch_target=arm;
		ACCESS_UNALIGNED="no"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
		with_tls=pthread
		case "$target" in
		armv7k-*)
			arm_fpu=VFP_HARD
			AC_DEFINE(TARGET_WATCHOS, 1, [...])
			;;
		esac		
		;;
   powerpc64-ps3-linux-gnu)
		TARGET=POWERPC64
		arch_target=powerpc64
		AC_DEFINE(TARGET_PS3, 1, [...])
		# It would be better to just use TARGET_POWERPC64, but lots of code already
		# uses this define
		AC_DEFINE(__mono_ppc64__, 1, [...])
		AC_DEFINE(__mono_ilp32__, 1, [64 bit mode with 4 byte longs and pointers])
		sizeof_register=8
		target_byte_order=G_BIG_ENDIAN
		;;
   powerpc64-xbox360-linux-gnu)
		TARGET=POWERPC64
		arch_target=powerpc64
		AC_DEFINE(TARGET_XBOX360, 1, [...])
		# It would be better to just use TARGET_POWERPC64, but lots of code already
		# uses this define
		sizeof_register=8
		target_byte_order=G_BIG_ENDIAN
		;;
   x86_64-*-nacl)
		TARGET=AMD64
		arch_target=amd64
		AC_DEFINE(TARGET_AMD64, 1, [...])
		AC_DEFINE(__mono_ilp32__, 1, [64 bit mode with 4 byte longs and pointers])
		sizeof_register=8
		;;
# TODO: make proper support for NaCl target.
#   arm*-*nacl)
#		TARGET=ARM
#		arch_target=arm
#		AC_DEFINE(TARGET_ARM, 1, [...])
#		ACCESS_UNALIGNED="no"
#		sizeof_register=4
#               CPPFLAGS="$CPPFLAGS \
#                    -D__ARM_EABI__ \
#                    -D__arm__ \
#                    -D__portable_native_client__ \
#                    -Dtimezone=_timezone \
#                    -DDISABLE_SOCKETS \
#                    -DDISABLE_ATTACH \
#                    -DUSE_NEWLIB"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
#		with_tls=pthread
#		;;
   i686-*-nacl)
		TARGET=X86
		arch_target=x86
		AC_DEFINE(TARGET_X86, 1, [...])
		sizeof_register=4
		;;
   arm*-linux-*)
		TARGET=ARM;
		arch_target=arm;
		AC_DEFINE(TARGET_ARM, 1, [...])
		ACCESS_UNALIGNED="no"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
		with_tls=pthread
		target_mach=no
		case "$target" in
		armv7l-unknown-linux-gnueabi*)
			# TEGRA
			CPPFLAGS="$CPPFLAGS"
			;;
   		armv5-*-linux-androideabi*)
			AC_DEFINE(TARGET_ANDROID, 1, [...])
			CPPFLAGS="$CPPFLAGS"
			;;
   		*-linux-androideabi*)
			AC_DEFINE(TARGET_ANDROID, 1, [...])
			CPPFLAGS="$CPPFLAGS"
			;;
		esac
		;;
   arm*-netbsd*-eabi*)
		TARGET=ARM;
		arch_target=arm;
		AC_DEFINE(TARGET_ARM, 1, [...])
		ACCESS_UNALIGNED="no"
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
		with_tls=pthread
		target_mach=no
		;;
   i686*-linux-*)
		TARGET=X86;
		arch_target=x86;
		AC_DEFINE(TARGET_X86, 1, [...])
		CPPFLAGS="$CPPFLAGS"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h		
		with_tls=pthread
		target_mach=no

		case "$target" in
		*-linux-android*)
			AC_DEFINE(TARGET_ANDROID, 1, [...])
			;;
		esac
		;;
   x86_64*-linux-*)
		TARGET=AMD64;
		arch_target=amd64;
		AC_DEFINE(TARGET_AMD64, 1, [...])
		CPPFLAGS="$CPPFLAGS"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
		with_tls=pthread
		target_mach=no

		case "$target" in
		*-linux-android*)
			AC_DEFINE(TARGET_ANDROID, 1, [...])
			;;
		esac
		;;
   x86_64-ps4-freebsd)
		TARGET=AMD64;
		arch_target=amd64;
		AC_DEFINE(TARGET_AMD64, 1, [...])
		AC_DEFINE(TARGET_PS4, 1, [...])
		AC_DEFINE(DISABLE_HW_TRAPS, 1, [...])
		CPPFLAGS="$CPPFLAGS"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
		with_tls=pthread
		target_mach=no
		target_win32=no
		;;
   aarch64*-linux-*)
		TARGET=ARM64;
		arch_target=arm64;
		AC_DEFINE(TARGET_ARM64, 1, [...])
		CPPFLAGS="$CPPFLAGS"
		# Can't use tls, since it depends on the runtime detection of tls offsets
		# in mono-compiler.h
		with_tls=pthread
		target_mach=no
		case "$target" in
		*-linux-android*)
			AC_DEFINE(TARGET_ANDROID, 1, [...])
			;;
		esac
		;;
	aarch64-*)
		TARGET=ARM64
		;;
	*)
		AC_MSG_ERROR([Cross compiling is not supported for target $target])
	esac
fi

case "$TARGET" in
X86)
	AC_DEFINE(TARGET_X86, 1, [...])
	;;
AMD64)
	AC_DEFINE(TARGET_AMD64, 1, [...])
	;;
ARM)
	AC_DEFINE(TARGET_ARM, 1, [...])
	;;
ARM64)
	AC_DEFINE(TARGET_ARM64, 1, [...])
	;;
POWERPC)
	AC_DEFINE(TARGET_POWERPC, 1, [...])
	;;
POWERPC64)
	AC_DEFINE(TARGET_POWERPC, 1, [...])
	AC_DEFINE(TARGET_POWERPC64, 1, [...])
	;;
S390X)
	AC_DEFINE(TARGET_S390X, 1, [...])
	;;
MIPS)
	AC_DEFINE(TARGET_MIPS, 1, [...])
	;;
SPARC)
	AC_DEFINE(TARGET_SPARC, 1, [...])
	;;
SPARC64)
	AC_DEFINE(TARGET_SPARC64, 1, [...])
	;;
esac

case "$HOST" in
X86)
	AC_DEFINE(HOST_X86, 1, [...])
	;;
AMD64)
	AC_DEFINE(HOST_AMD64, 1, [...])
	;;
ARM)
	AC_DEFINE(HOST_ARM, 1, [...])
	;;
ARM64)
	AC_DEFINE(HOST_ARM64, 1, [...])
	;;
POWERPC)
	AC_DEFINE(HOST_POWERPC, 1, [...])
	;;
POWERPC64)
	AC_DEFINE(HOST_POWERPC, 1, [...])
	AC_DEFINE(HOST_POWERPC64, 1, [...])
	;;
S390X)
	AC_DEFINE(HOST_S390X, 1, [...])
	;;
MIPS)
	AC_DEFINE(HOST_MIPS, 1, [...])
	;;
SPARC)
	AC_DEFINE(HOST_SPARC, 1, [...])
	;;
SPARC64)
	AC_DEFINE(HOST_SPARC64, 1, [...])
	;;
esac



dnl *************
dnl *** VTUNE ***
dnl *************

AC_ARG_ENABLE(vtune,[  --enable-vtune	Enable the VTUNE back-end], enable_vtune=$enableval, enable_vtune=no)
AC_ARG_WITH(vtune, [  --with-vtune=<vtune prefix>	Enable jit vtune profiling], enable_vtune=yes,)

AM_CONDITIONAL(HAVE_VTUNE, test x$enable_vtune = xyes)

if test "x$enable_vtune" = "xyes"; then
	if test "x$with_vtune" = "x"; then
		VTUNE_PATH=/opt/intel/vtune_amplifier_xe
	else
		VTUNE_PATH=$with_vtune
	fi
	VTUNE_INCLUDE=$VTUNE_PATH/include
	case "$TARGET" in
	X86)
		VTUNE_LIB=$VTUNE_PATH/lib32
		;;
	AMD64)
		VTUNE_LIB=$VTUNE_PATH/lib64
		;;
	*)
		AC_MSG_ERROR([Unsupported target $TARGET for VTUNE.])
		;;
	esac
	if test ! -f $VTUNE_INCLUDE/jitprofiling.h; then
		AC_MSG_ERROR([VTUNE $VTUNE_INCLUDE/jitprofiling.h not found.])
	fi
	if test ! -f $VTUNE_LIB/libjitprofiling.a; then
		AC_MSG_ERROR([VTUNE $VTUNE_LIB/libjitprofiling.a not found.])
	fi

	VTUNE_CFLAGS=-I$VTUNE_INCLUDE
	VTUNE_LIBS="-L/$VTUNE_LIB/ -ljitprofiling"

	AC_SUBST(VTUNE_LIBS)
	AC_SUBST(VTUNE_CFLAGS)
fi
dnl Use GCC atomic ops if they work on the target.
if test x$GCC = "xyes"; then
	case $TARGET in
	X86 | AMD64 | ARM | ARM64 | POWERPC | POWERPC64 | MIPS | S390X | SPARC | SPARC64)
		AC_DEFINE(USE_GCC_ATOMIC_OPS, 1, [...])
		;;
	esac
fi

if test "x$target_mach" = "xyes"; then

   if test "x$TARGET_SYS" = "xWATCHOS"; then
	  AC_DEFINE(TARGET_WATCHOS,1,[The JIT/AOT targets WatchOS])
	  CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DTARGET_WATCHOS"
	  CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DTARGET_WATCHOS"
	  BTLS_SUPPORTED=no
   elif test "x$TARGET" = "xARM" -o "x$TARGET" = "xARM64"; then
   	  AC_DEFINE(TARGET_IOS,1,[The JIT/AOT targets iOS])
	  CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DTARGET_IOS"
	  CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DTARGET_IOS"
	  BTLS_SUPPORTED=no
   else
       AC_TRY_COMPILE([#include "TargetConditionals.h"],[
       #if TARGET_IPHONE_SIMULATOR == 1 || TARGET_OS_IPHONE == 1
       #error fail this for ios
       #endif
       return 0;
       ], [
	   	  AC_DEFINE(TARGET_OSX,1,[The JIT/AOT targets OSX])
          CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DTARGET_OSX"
          CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DTARGET_OSX"
          target_osx=yes
       ], [
          AC_DEFINE(TARGET_IOS,1,[The JIT/AOT targets iOS])
          CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DTARGET_IOS"
          CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DTARGET_IOS"
          BTLS_SUPPORTED=no
       ])
	fi
   AC_DEFINE(TARGET_MACH,1,[The JIT/AOT targets Apple platforms])
fi

AM_CONDITIONAL(TARGET_OSX, test x$target_osx = xyes)

if test "x$sizeof_register" = "x4"; then
   AC_DEFINE(SIZEOF_REGISTER,4,[size of machine integer registers])
elif test "x$sizeof_register" = "x8"; then
   AC_DEFINE(SIZEOF_REGISTER,8,[size of machine integer registers])
else
   AC_DEFINE(SIZEOF_REGISTER,SIZEOF_VOID_P,[size of machine integer registers])
fi

if test "x$target_byte_order" = "xG_BIG_ENDIAN"; then
   AC_DEFINE(TARGET_BYTE_ORDER,G_BIG_ENDIAN,[byte order of target])
elif test "x$target_byte_order" = "xG_LITTLE_ENDIAN"; then
   AC_DEFINE(TARGET_BYTE_ORDER,G_LITTLE_ENDIAN,[byte order of target])
else
   AC_DEFINE(TARGET_BYTE_ORDER,G_BYTE_ORDER,[byte order of target])
fi

if test "x$have_visibility_hidden" = "xyes"; then
   AC_DEFINE(HAVE_VISIBILITY_HIDDEN, 1, [Support for the visibility ("hidden") attribute])
fi

if test "x$have_deprecated" = "xyes"; then
   AC_DEFINE(HAVE_DEPRECATED, 1, [Support for the deprecated attribute])
fi

AC_ARG_ENABLE(interpreter, [  --enable-interpreter       Enable Interpreter], enable_interpreter=$enableval, enable_interpreter=no)

if test "x$enable_interpreter" = "xyes"; then
	AC_DEFINE(ENABLE_INTERPRETER, 1, [Enable Interpreter])
fi

AM_CONDITIONAL(ENABLE_INTERPRETER, [test x$enable_interpreter = xyes])


dnl 
dnl Simple Generational checks (sgen)
dnl
SGEN_DEFINES=
AC_ARG_WITH(sgen,                    [  --with-sgen=yes,no             Extra Generational GC, default=yes],[buildsgen=$with_sgen],[buildsgen=yes])
AC_ARG_WITH(sgen-default-concurrent, [  --with-sgen-default-concurrent=yes,no             Use Concurrent GC, default=no],[],[with_sgen_default_concurrent=no])
if test x$buildsgen = xyes; then
   AC_DEFINE(HAVE_MOVING_COLLECTOR, 1, [Moving collector])
   SGEN_DEFINES="-DHAVE_SGEN_GC"

	conc_gc_msg=""
   if test x$with_sgen_default_concurrent != xno; then
       AC_DEFINE(HAVE_CONC_GC_AS_DEFAULT, 1, [Defaults to concurrent GC])
	   conc_gc_msg=" (concurrent by default)"
   fi

   if test "x$gc_msg" = "x"; then
      gc_msg="sgen$conc_gc_msg"
   else
      gc_msg="sgen$conc_gc_msg and $gc_msg"
   fi
fi
AC_SUBST(SGEN_DEFINES)
AM_CONDITIONAL(SUPPORT_SGEN, test x$buildsgen = xyes)

jit_status="Building and using the JIT"

libsuffix=".so"

case "$host" in
     *-*-darwin*)
	libsuffix=".dylib"
	LIBC="libc.dylib"
	INTL="libintl.dylib"
	SQLITE="libsqlite.0.dylib"
	SQLITE3="libsqlite3.0.dylib"
	X11="libX11.dylib"
	GDKX11="libgdk-x11-2.0.dylib"
	GTKX11="libgtk-x11-2.0.dylib"
	;;
     *-*-*netbsd*)
	LIBC="libc.so"
	INTL="libintl.so"
	SQLITE="libsqlite.so"
	SQLITE3="libsqlite3.so"
	;;
     *-*-kfreebsd*-gnu)
	LIBC="libc.so.0.1"
	INTL="libc.so.0.1"
	X11="libX11.so.6"
	;;
    *-*-*freebsd*)
    	LIBC="libc.so.7"
	INTL="libintl.so"
	SQLITE="libsqlite.so"
	SQLITE3="libsqlite3.so"
	;;
    *-*-*openbsd*)
    	LIBC="libc.so"
	INTL="libintl.so"
        SQLITE="libsqlite.so"
        SQLITE3="libsqlite3.so"
	;;
    *-*-*linux*)
	AC_PATH_X
	dlsearch_path=`(libtool --config ; echo eval echo \\$sys_lib_dlsearch_path_spec) | sh`
	AC_MSG_CHECKING(for the soname of libX11.so)
	for i in $x_libraries $dlsearch_path; do
		for r in 4 5 6; do
			if test -f $i/libX11.so.$r; then
				X11=libX11.so.$r
				AC_MSG_RESULT($X11)
			fi
		done
	done
	
	if test "x$X11" = "xlibX11.so"; then
		AC_MSG_WARN([Could not find libX11.so. Do you have X.org or XFree86 installed? Assuming libX11.so.6...]);
		X11=libX11.so.6
	fi
	;;
esac


AC_SUBST(libsuffix)

AC_ARG_WITH([libgdiplus],
	[  --with-libgdiplus=installed|sibling|<path>	Override the libgdiplus used for System.Drawing tests (defaults to installed)],
	[], [with_libgdiplus=installed])

# default install location
libgdiplus_install_loc=libgdiplus${libsuffix}
case "$host" in
    *-*-*linux*)
    libgdiplus_install_loc=libgdiplus${libsuffix}.0
    ;;
esac

case $with_libgdiplus in
    no|installed)
    libgdiplus_loc=
    ;;

    yes|sibling)
    libgdiplus_loc=`cd ../libgdiplus && pwd`/src/libgdiplus.la
    ;;

    /*) # absolute path, assume it is an install location
    libgdiplus_loc=$with_libgdiplus
    libgdiplus_install_loc=$with_libgdiplus
    ;;

    *)
    libgdiplus_loc=`pwd`/$with_libgdiplus
    ;;
esac
AC_SUBST([libgdiplus_loc])
AC_SUBST([libgdiplus_install_loc])

AC_ARG_ENABLE(icall-symbol-map,[  --enable-icall-symbol-map Generate tables which map icall functions to their C symbols], icall_symbol_map=$enableval, icall_symbol_map=no)
if test "x$icall_symbol_map" = "xyes"; then
   AC_DEFINE(ENABLE_ICALL_SYMBOL_MAP, 1, [Icall symbol map enabled])
fi

AC_ARG_ENABLE(icall-export,[  --enable-icall-export Export icall functions], icall_export=$enableval, icall_export=no)
if test "x$icall_export" = "xyes"; then
   AC_DEFINE(ENABLE_ICALL_EXPORT, 1, [Icall export enabled])
fi

AC_ARG_ENABLE(icall-tables,[  --disable-icall-tables Disable the runtime lookup of icalls], icall_tables=$enableval, icall_tables=yes)
if test "x$icall_tables" = "xno"; then
   AC_DEFINE(DISABLE_ICALL_TABLES, 1, [Icall tables disabled])
fi

if test "x$with_tls" = "x__thread"; then
	AC_DEFINE(HAVE_KW_THREAD, 1, [Have __thread keyword])
	# Pass the information to libgc
	CPPFLAGS="$CPPFLAGS -DUSE_COMPILER_TLS"
	AC_MSG_CHECKING(if the tls_model attribute is supported)
	AC_TRY_COMPILE([static __thread int foo __attribute__((tls_model("initial-exec")));], [
		], [
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_TLS_MODEL_ATTR, 1, [tls_model available])
		], [
			AC_MSG_RESULT(no)
	])
fi

if test ${TARGET} = ARM; then
	dnl ******************************************
	dnl *** Check to see what FPU is available ***
	dnl ******************************************
	AC_MSG_CHECKING(which FPU to use)

	#
	# This is a bit tricky:
	#
	# if (__ARM_PCS_VFP) {
	# 	/* mfloat-abi=hard == VFP with hard ABI */
	# } elif (!__SOFTFP__) {
	# 	/* mfloat-abi=softfp == VFP with soft ABI */
	# } else {
	# 	/* mfloat-abi=soft == no VFP */
	# }
	#
	# The exception is iOS (w/ GCC) where none of the above
	# are defined (but iOS always uses the 'softfp' ABI).
	#
	# No support for FPA.
	#

	fpu=NONE

	# iOS GCC always uses the 'softfp' ABI.
	if test x"$GCC" = xyes && test x$host_darwin = xyes; then
		fpu=VFP
	fi

	# Are we using the 'hard' ABI?
	if test x$fpu = xNONE; then
		AC_TRY_COMPILE([], [
			#ifndef __ARM_PCS_VFP
			#error "Float ABI is not 'hard'"
			#endif
			return 0;
		], [
			fpu=VFP_HARD
		], [
			fpu=NONE
		])
	fi

	# No 'hard' ABI. 'soft' or 'softfp'?
	if test x$fpu = xNONE; then
		AC_TRY_COMPILE([], [
			#ifdef __SOFTFP__
			#error "Float ABI is not 'softfp'"
			#endif
			return 0;
		], [
			fpu=VFP
		], [
			fpu=NONE
		])
	fi

	if test x$arm_fpu != x; then
	   fpu=$arm_fpu
	fi

	AC_MSG_RESULT($fpu)
	CPPFLAGS="$CPPFLAGS -DARM_FPU_$fpu=1"
	unset fpu

	dnl *********************************************
	dnl *** Check which ARM version(s) we can use ***
	dnl *********************************************
	AC_MSG_CHECKING(which ARM version to use)

	AC_TRY_COMPILE([], [
		#if !defined(__ARM_ARCH_5T__) && !defined(__ARM_ARCH_5TE__) && !defined(__ARM_ARCH_5TEJ__)
		#error Not on ARM v5.
		#endif
		return 0;
	], [
		arm_v5=yes

		arm_ver=ARMv5
	], [])

	AC_TRY_COMPILE([], [
		#if !defined(__ARM_ARCH_6J__) && !defined(__ARM_ARCH_6ZK__) && !defined(__ARM_ARCH_6K__) && !defined(__ARM_ARCH_6T2__) && !defined(__ARM_ARCH_6M__)
		#error Not on ARM v6.
		#endif
		return 0;
	], [
		arm_v5=yes
		arm_v6=yes

		arm_ver=ARMv6
	], [])

	AC_TRY_COMPILE([], [
		#if !defined(__ARM_ARCH_7A__) && !defined(__ARM_ARCH_7R__) && !defined(__ARM_ARCH_7EM__) && !defined(__ARM_ARCH_7M__) && !defined(__ARM_ARCH_7S__)
		#error Not on ARM v7.
		#endif
		return 0;
	], [
		arm_v5=yes
		arm_v6=yes
		arm_v7=yes

		arm_ver=ARMv7
	], [])

	AC_MSG_RESULT($arm_ver)

	if test x$arm_v5 = xyes; then
		AC_DEFINE(HAVE_ARMV5, 1, [ARM v5])
		CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DHAVE_ARMV5=1"
	fi

	if test x$arm_v6 = xyes; then
		AC_DEFINE(HAVE_ARMV6, 1, [ARM v6])
		CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DHAVE_ARMV6=1"
	fi

	if test x$arm_v7 = xyes; then
		AC_DEFINE(HAVE_ARMV7, 1, [ARM v7])
		CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DHAVE_ARMV7=1"
	fi
fi

if test ${TARGET} = unknown; then
	CPPFLAGS="$CPPFLAGS -DNO_PORT"
	AC_MSG_WARN("mono has not been ported to $host: some things may not work.")
fi

if test "x$platform_android" = "xyes"; then
	case "x${TARGET}" in
	xARM)
		case "x$arm_ver" in
		xARMv5)
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=android-armv5
			;;
		xARMv6)
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=android-armv6
			;;
		xARMv7)
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=android-armv7
			;;
		*)
			BTLS_SUPPORTED=no
			;;
		esac
		;;
	xARM64)
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=android-v8a
		;;
	xX86)
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=android-x86
		;;
	xAMD64)
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=android-x64
		;;
	*)
		BTLS_SUPPORTED=no
		;;
	esac
fi

if test ${ACCESS_UNALIGNED} = no; then
	CPPFLAGS="$CPPFLAGS -DNO_UNALIGNED_ACCESS"
fi

if test x$host_darwin = xyes; then
	AC_MSG_CHECKING([for ranlib that supports -no_warning_for_no_symbols option])
	AS_IF(
		[$RANLIB -no_warning_for_no_symbols 2>&1 | grep -q "unknown option"],
		[AC_MSG_RESULT([no])],
		[
			# avoid AR calling ranlib, libtool calls it anyway. suppress no symbols warning.
			AR_FLAGS="Scru"
			RANLIB="$RANLIB -no_warning_for_no_symbols"
			AC_MSG_RESULT([yes])
		]
	)
fi

case "x$libgc" in
	xincluded)
		# Pass CPPFLAGS to libgc configure
		# We should use a separate variable for this to avoid passing useless and
		# potentially problematic defines to libgc (like -D_FILE_OFFSET_BITS=64)
		# This should be executed late so we pick up the final version of CPPFLAGS
		# The problem with this approach, is that during a reconfigure, the main
		# configure scripts gets invoked with these arguments, so we use separate
		# variables understood by libgc's configure to pass CPPFLAGS and CFLAGS.
		TMP_CPPFLAGS="$CPPFLAGS $CPPFLAGS_FOR_LIBGC"
		if test x$TARGET = xSPARC -o x$TARGET = xSPARC64; then
			TMP_CPPFLAGS=`echo $TMP_CPPFLAGS | sed -e 's/-D_FILE_OFFSET_BITS=64//g'`
		fi
		# Don't pass -finstrument-for-thread-suspension in, 
		# if these are instrumented it will be very bad news 
		# (infinite recursion, undefined parking behavior, etc)
		TMP_CPPFLAGS=`echo $TMP_CPPFLAGS | sed -e 's/-finstrument-for-thread-suspension//g'`
		ac_configure_args="$ac_configure_args --disable-embed-check --with-libgc-threads=$libgc_threads $libgc_configure_args \"CPPFLAGS_FOR_LIBGC=$TMP_CPPFLAGS\" \"CFLAGS_FOR_LIBGC=$CFLAGS_FOR_LIBGC\""
		if test "x$support_boehm" = "xyes"; then
			AC_CONFIG_SUBDIRS(libgc)
		fi
		;;
esac

MALLOC_MEMPOOLS=no
AC_ARG_WITH(malloc_mempools,[  --with-malloc-mempools=yes,no  Use malloc for each single mempool allocation (only for runtime debugging, defaults to no)],[
	if test x$with_malloc_mempools = xyes; then
		MALLOC_MEMPOOLS=yes
		AC_DEFINE(USE_MALLOC_FOR_MEMPOOLS,1,[Use malloc for each single mempool allocation])
	fi
])


DISABLE_MCS_DOCS=default
AC_ARG_WITH(mcs_docs,[  --with-mcs-docs=yes,no         If you want to build the documentation under mcs (defaults to yes)],[
	if test x$with_mcs_docs != xyes; then
		DISABLE_MCS_DOCS=yes
	fi
])
if test -n "$INSTALL_4_x_TRUE"; then :
	DISABLE_MCS_DOCS=yes
fi
if test "x$DISABLE_MCS_DOCS" = "xdefault"; then
   DISABLE_MCS_DOCS=$DISABLE_MCS_DOCS_default
fi

AC_ARG_WITH(lazy_gc_thread_creation, [  --with-lazy-gc-thread-creation=yes|no      Enable lazy runtime thread creation, embedding host must do it explicitly (defaults to no)],[
	if test x$with_lazy_gc_thread_creation != xno ; then
		AC_DEFINE(LAZY_GC_THREAD_CREATION,1,[Enable lazy gc thread creation by the embedding host.])
	fi
], [with_lazy_gc_thread_creation=no])

AC_ARG_WITH(cooperative_gc, [  --with-cooperative-gc=yes|no      Enable cooperative stop-the-world garbage collection (sgen only) (defaults to no)], [], [with_cooperative_gc=default])

if test x$with_cooperative_gc = xdefault; then
	with_cooperative_gc=$with_cooperative_gc_default
fi

if test x$with_cooperative_gc != xno; then
	AC_DEFINE(USE_COOP_GC,1,[Enable cooperative stop-the-world garbage collection.])
fi

AM_CONDITIONAL([ENABLE_COOP], [test x$with_cooperative_gc != xno])

AC_ARG_ENABLE(checked_build, [  --enable-checked-build=LIST      To enable checked build (expensive asserts), configure with a comma-separated LIST of checked build modules and then include that same list in the environment variable MONO_CHECK_MODE at runtime. Recognized checked build modules: all, gc, metadata, thread],[

	if test x$enable_checked_build != x ; then
		AC_DEFINE(ENABLE_CHECKED_BUILD,1,[Enable checked build])
	fi
	for feature in `echo "$enable_checked_build" | sed -e "s/,/ /g"`; do
		eval "mono_checked_build_test_enable_$feature='yes'"
	done

	if test "x$mono_checked_build_test_enable_all" = "xyes"; then
		eval "mono_checked_build_test_enable_gc='yes'"
		eval "mono_checked_build_test_enable_metadata='yes'"
		eval "mono_checked_build_test_enable_thread='yes'"
	fi

	if test "x$mono_checked_build_test_enable_gc" = "xyes"; then
		AC_DEFINE(ENABLE_CHECKED_BUILD_GC, 1, [Enable GC checked build])
	fi

	if test "x$mono_checked_build_test_enable_metadata" = "xyes"; then
		AC_DEFINE(ENABLE_CHECKED_BUILD_METADATA, 1, [Enable metadata checked build])
	fi

	if test "x$mono_checked_build_test_enable_thread" = "xyes"; then
		AC_DEFINE(ENABLE_CHECKED_BUILD_THREAD, 1, [Enable thread checked build])
	fi

], [])

AC_CHECK_HEADER([malloc.h], 
		[AC_DEFINE([HAVE_USR_INCLUDE_MALLOC_H], [1], 
			[Define to 1 if you have /usr/include/malloc.h.])],,)

if test x"$GCC" = xyes; then
   	# Implicit function declarations are not 64 bit safe
	# Do this late, since this causes lots of configure tests to fail
	CFLAGS="$CFLAGS -Werror-implicit-function-declaration"
	# jay has a lot of implicit declarations
	JAY_CFLAGS="-Wno-implicit-function-declaration"
fi

# When --disable-shared is used, libtool transforms libmono-2.0.la into libmono-2.0.so
# instead of libmono-static.a
if test "x$enable_shared" = "xno" -a "x$enable_executables" = "xyes"; then
   LIBMONO_LA=libmini-static.la
else
   if test x$buildsgen = xyes; then
      LIBMONO_LA=libmonosgen-$API_VER.la
   else
      LIBMONO_LA=libmonoboehm-$API_VER.la
   fi
fi
AC_SUBST(LIBMONO_LA)

dnl **************
dnl ***  Btls  ***
dnl **************

AC_ARG_ENABLE(btls, [  --disable-btls             Disable the BoringTls provider], enable_btls=$enableval, enable_btls=$BTLS_SUPPORTED)
AC_ARG_WITH(btls_android_ndk, [  --with-btls-android-ndk        Android NDK for BoringTls])

AM_CONDITIONAL(BTLS, test x$enable_btls = xyes)

btls_android=no
if test "x$enable_btls" = "xyes"; then
	AC_PATH_PROG(CMAKE, [cmake], [no], [$PATH:/Applications/CMake.app/Contents/bin:/usr/local/bin])
	if test "x$CMAKE" = "xno"; then
		AC_MSG_ERROR("cmake not found")
	fi

	BTLS_ROOT=`cd $srcdir && pwd`/external/boringssl
	AC_SUBST(BTLS_ROOT)

	btls_arch=
	btls_cflags=
	BTLS_CMAKE_ARGS=

	case "$BTLS_PLATFORM" in
	i386)
		btls_arch=i386
		btls_cflags="-m32"
		case $host_os in
			darwin*)
				btls_cflags="$btls_cflags -arch i386"
		esac
		;;
	x86_64)
		btls_arch=x86_64
		;;
	arm)
		btls_arch=arm
		;;
	armsoft)
		btls_arch=arm
		btls_cflags="-DOPENSSL_NO_ASM=1"
		;;
	aarch64)
		btls_arch=aarch64
		;;
	android-armv5)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"armeabi\" -DANDROID_NATIVE_API_LEVEL=12"
		;;
	android-armv6)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"armeabi\" -DANDROID_NATIVE_API_LEVEL=12"
		;;
	android-armv7)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"armeabi-v7a\" -DANDROID_NATIVE_API_LEVEL=12"
		;;
	android-v8a)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"arm64-v8a\" -DANDROID_NATIVE_API_LEVEL=12"
		;;
	android-x86)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"x86\" -DANDROID_NATIVE_API_LEVEL=12"
		;;
	android-x64)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"x86_64\" -DANDROID_NATIVE_API_LEVEL=12"
		;;
	*)
		AC_MSG_ERROR(Invalid BTLS platform)
	esac

	if test "x$platform_android" = "xyes"; then
		btls_android=yes
		BTLS_CMAKE_ARGS="$BTLS_CMAKE_ARGS -DCMAKE_TOOLCHAIN_FILE=$BTLS_ROOT/util/android-cmake/android.toolchain.cmake"
		if test "x$with_btls_android_ndk" != "x"; then
			BTLS_CMAKE_ARGS="$BTLS_CMAKE_ARGS -DANDROID_NDK=\"$with_btls_android_ndk\""
		else
			AC_MSG_ERROR([Need to pass the --with-btls-android-ndk argument when building with BTLS support on Android.])
		fi
	fi

	if test "x$btls_arch" != "x"; then
		BTLS_CMAKE_ARGS="$BTLS_CMAKE_ARGS -DBTLS_ARCH=\"$btls_arch\""
	fi

	BTLS_CFLAGS="$CPPFLAGS_FOR_BTLS $btls_cflags"
	AC_SUBST(BTLS_ARCH)
	AC_SUBST(BTLS_CFLAGS)
	AC_SUBST(BTLS_PLATFORM)
	AC_SUBST(BTLS_CMAKE_ARGS)

	AC_DEFINE(HAVE_BTLS, 1, [BoringTls is supported])
else
	enable_btls=no
fi

AM_CONDITIONAL(BTLS_ANDROID, test x$btls_android = xyes)

if test x$DISABLE_MCS_DOCS = xyes; then
   docs_dir=""
else
   docs_dir=docs
fi
AC_SUBST(docs_dir)

## Maybe should also disable if mcsdir is invalid.  Let's punt the issue for now.
AM_CONDITIONAL(BUILD_MCS, [test x$cross_compiling = xno && test x$enable_mcs_build != xno])

libmono_ldflags="$libmono_ldflags $LIBS"

AM_CONDITIONAL(MIPS_GCC, test ${TARGET}${ac_cv_prog_gcc} = MIPSyes)
AM_CONDITIONAL(MIPS_SGI, test ${TARGET}${ac_cv_prog_gcc} = MIPSno)
AM_CONDITIONAL(SPARC, test x$TARGET = xSPARC)
AM_CONDITIONAL(SPARC64, test x$TARGET = xSPARC64)
AM_CONDITIONAL(X86, test x$TARGET = xX86)
AM_CONDITIONAL(AMD64, test x$TARGET = xAMD64)
AM_CONDITIONAL(MIPS, test x$TARGET = xMIPS)
AM_CONDITIONAL(POWERPC, test x$TARGET = xPOWERPC)
AM_CONDITIONAL(POWERPC64, test x$TARGET = xPOWERPC64)
AM_CONDITIONAL(ARM, test x$TARGET = xARM)
AM_CONDITIONAL(ARM64, test x$TARGET = xARM64)
AM_CONDITIONAL(S390X, test x$TARGET = xS390X)
AM_CONDITIONAL(HOST_X86, test x$HOST = xX86)
AM_CONDITIONAL(HOST_AMD64, test x$HOST = xAMD64)
AM_CONDITIONAL(HOST_ARM, test x$HOST = xARM)
AM_CONDITIONAL(HOST_ARM64, test x$HOST = xARM64)
AM_CONDITIONAL(CROSS_COMPILE, test "x$host" != "x$target")

AC_SUBST(LIBC)
AC_SUBST(INTL)
AC_SUBST(SQLITE)
AC_SUBST(SQLITE3)
AC_SUBST(X11)
AC_SUBST(GDKX11)
AC_SUBST(GTKX11)
AC_SUBST(XINERAMA)
AC_DEFINE_UNQUOTED(MONO_ARCHITECTURE,"$arch_target",[The architecture this is running on])
AC_SUBST(arch_target)
AC_SUBST(CFLAGS)
AC_SUBST(CPPFLAGS)
AC_SUBST(LDFLAGS)

#This must always be defined when building the runtime
AC_DEFINE(MONO_INSIDE_RUNTIME,1, [Disable banned functions from being used by the runtime])

mono_build_root=`pwd`
AC_SUBST(mono_build_root)

mono_runtime=mono/mini/mono
AC_SUBST(mono_runtime)

CSC_LOCATION=`cd $srcdir && pwd`/external/roslyn-binaries/Microsoft.Net.Compilers/Microsoft.Net.Compilers.2.3.0/tools/csc.exe

if test $csc_compiler = mcs; then
  CSC=$mcs_topdir/class/lib/build/mcs.exe
else
  CSC=$CSC_LOCATION
fi

mono_cfg_root=$mono_build_root/runtime
if test x$host_win32 = xyes; then
  if test "x$cross_compiling" = "xno"; then
    mono_cfg_dir=`cygpath -w -a $mono_cfg_root`\\etc
    CSC=`cygpath -m -a $CSC`
    CSC_LOCATION=`cygpath -m -a $CSC_LOCATION`
  else
    mono_cfg_dir=`echo $mono_cfg_root | tr '/' '\\'`\\etc
  fi
else
  mono_cfg_dir=$mono_cfg_root/etc
fi
AC_SUBST(mono_cfg_dir)

AC_SUBST(CSC)

AC_CONFIG_FILES([po/mcs/Makefile.in])

AC_CONFIG_FILES([runtime/mono-wrapper],[chmod +x runtime/mono-wrapper])
AC_CONFIG_FILES([runtime/monodis-wrapper],[chmod +x runtime/monodis-wrapper])

AC_CONFIG_COMMANDS([runtime/etc/mono/1.0/machine.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/1.0
    cd runtime/etc/mono/1.0
    rm -f machine.config
    $LN_S $reldir/data/net_1_1/machine.config machine.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/2.0/machine.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/2.0
    cd runtime/etc/mono/2.0
    rm -f machine.config
    $LN_S $reldir/data/net_2_0/machine.config machine.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/2.0/web.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/2.0
    cd runtime/etc/mono/2.0
    rm -f web.config
    $LN_S $reldir/data/net_2_0/web.config web.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/browscap.ini],
[   depth=../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/
    cd runtime/etc/mono/
    rm -f browscap.ini
    $LN_S $reldir/data/browscap.ini browscap.ini
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/2.0/Browsers/Compat.browser],
[   depth=../../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/2.0/Browsers/
    cd runtime/etc/mono/2.0/Browsers
    rm -f Compat.browser
    $LN_S $reldir/data/Browsers/Compat.browser Compat.browser
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/4.0/Browsers/Compat.browser],
[   depth=../../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/4.0/Browsers/
    cd runtime/etc/mono/4.0/Browsers
    rm -f Compat.browser
    $LN_S $reldir/data/Browsers/Compat.browser Compat.browser
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/4.5/Browsers/Compat.browser],
[   depth=../../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/4.5/Browsers/
    cd runtime/etc/mono/4.5/Browsers
    rm -f Compat.browser
    $LN_S $reldir/data/Browsers/Compat.browser Compat.browser
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/4.0/machine.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/4.0
    cd runtime/etc/mono/4.0
    rm -f machine.config
    $LN_S $reldir/data/net_4_0/machine.config machine.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/4.0/web.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/4.0
    cd runtime/etc/mono/4.0
    rm -f web.config
    $LN_S $reldir/data/net_4_0/web.config web.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/4.5/machine.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/4.5
    cd runtime/etc/mono/4.5
    rm -f machine.config
    $LN_S $reldir/data/net_4_5/machine.config machine.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([runtime/etc/mono/4.5/web.config],
[   depth=../../../..
    case $srcdir in
    [[\\/$]]* | ?:[[\\/]]* ) reldir=$srcdir ;;
    .) reldir=$depth ;;
    *) reldir=$depth/$srcdir ;;
    esac
    $ac_aux_dir/install-sh -d runtime/etc/mono/4.5
    cd runtime/etc/mono/4.5
    rm -f web.config
    $LN_S $reldir/data/net_4_5/web.config web.config
    cd $depth
],[LN_S='$LN_S'])

AC_CONFIG_COMMANDS([quiet-libtool], [sed -e 's/echo "copying selected/# "copying selected/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool; sed -e 's/$ECHO "copying selected/# "copying selected/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool])
AC_CONFIG_COMMANDS([nolock-libtool], [sed -e 's/lock_old_archive_extraction=yes/lock_old_archive_extraction=no/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool])

AC_OUTPUT([
Makefile
mono-uninstalled.pc
acceptance-tests/Makefile
llvm/Makefile
scripts/mono-find-provides
scripts/mono-find-requires
mono/Makefile
mono/btls/Makefile
mono/utils/Makefile
mono/metadata/Makefile
mono/dis/Makefile
mono/cil/Makefile
mono/arch/Makefile
mono/arch/x86/Makefile
mono/arch/amd64/Makefile
mono/arch/ppc/Makefile
mono/arch/sparc/Makefile
mono/arch/s390x/Makefile
mono/arch/arm/Makefile
mono/arch/arm64/Makefile
mono/arch/mips/Makefile
mono/sgen/Makefile
mono/tests/Makefile
mono/tests/tests-config
mono/tests/gc-descriptors/Makefile
mono/tests/testing_gac/Makefile
mono/unit-tests/Makefile
mono/benchmark/Makefile
mono/mini/Makefile
mono/profiler/Makefile
m4/Makefile
ikvm-native/Makefile
scripts/Makefile
man/Makefile
docs/Makefile
data/Makefile
data/net_2_0/Makefile
data/net_4_0/Makefile
data/net_4_5/Makefile
data/net_2_0/Browsers/Makefile
data/net_4_0/Browsers/Makefile
data/net_4_5/Browsers/Makefile
data/mint.pc
data/mono-2.pc
data/monosgen-2.pc
data/mono.pc
data/mono-cairo.pc
data/mono-nunit.pc
data/mono-options.pc
data/mono-lineeditor.pc
data/monodoc.pc
data/dotnet.pc
data/dotnet35.pc
data/wcf.pc
data/cecil.pc
data/system.web.extensions_1.0.pc
data/system.web.extensions.design_1.0.pc
data/system.web.mvc.pc
data/system.web.mvc2.pc
data/system.web.mvc3.pc
data/aspnetwebstack.pc
data/reactive.pc
samples/Makefile
support/Makefile
data/config
tools/Makefile
tools/locale-builder/Makefile
tools/sgen/Makefile
tools/monograph/Makefile
tools/pedump/Makefile
runtime/Makefile
msvc/Makefile
po/Makefile
])

# Update all submodules recursively to ensure everything is checked out
$srcdir/scripts/update_submodules.sh

if test x$host_win32 = xyes; then
   # Get rid of 'cyg' prefixes in library names
   sed -e "s/\/cyg\//\/\//" libtool > libtool.new; mv libtool.new libtool; chmod 755 libtool
   # libtool seems to inherit -mno-cygwin from our CFLAGS, and uses it to compile its executable
   # wrapper scripts which use exec(). gcc has no problem compiling+linking this, but the resulting
   # executable doesn't work...
   sed -e "s,-mno-cygwin,,g" libtool > libtool.new; mv libtool.new libtool; chmod 755 libtool
fi

if test x$host_darwin = xyes; then
   # This doesn't seem to be required and it slows down parallel builds
   sed -e 's,lock_old_archive_extraction=yes,lock_old_archive_extraction=no,g' < libtool > libtool.new && mv libtool.new libtool && chmod +x libtool
fi

(
  case $prefix in
  NONE) prefix=$ac_default_prefix ;;
  esac
  case $exec_prefix in
  NONE) exec_prefix='${prefix}' ;;
  esac

  #
  # If we are cross compiling, we don't build in the mcs/ tree.  Let us not clobber
  # any existing config.make.  This allows people to share the same source tree
  # with different build directories, one native and one cross
  #
  if test x$cross_compiling = xno && test x$enable_mcs_build != xno; then

    test -w $mcs_topdir/build || chmod +w $mcs_topdir/build

    echo "prefix=$prefix" > $mcs_topdir/build/config.make
    echo "exec_prefix=$exec_prefix" >> $mcs_topdir/build/config.make
    echo "sysconfdir=$sysconfdir" >> $mcs_topdir/build/config.make
    echo 'mono_libdir=${exec_prefix}/lib' >> $mcs_topdir/build/config.make
    echo "mono_build_root=$mono_build_root" >> $mcs_topdir/build/config.make
    echo 'IL_FLAGS = /debug' >> $mcs_topdir/build/config.make
    echo "RUNTIME = $mono_build_root/runtime/mono-wrapper" >> $mcs_topdir/build/config.make
    echo "ILDISASM = $mono_build_root/runtime/monodis-wrapper" >> $mcs_topdir/build/config.make
    echo "JAY_CFLAGS = $JAY_CFLAGS" >> $mcs_topdir/build/config.make

    case $INSTALL in
    [[\\/$]]* | ?:[[\\/]]* ) mcs_INSTALL=$INSTALL ;;
    *) mcs_INSTALL=$mono_build_root/$INSTALL ;;
    esac

    echo "INSTALL = $mcs_INSTALL" >> $mcs_topdir/build/config.make

    export VERSION
    [myver=$($AWK 'BEGIN {
      split (ENVIRON["VERSION"] ".0.0.0", vsplit, ".")
      if(length(vsplit [1]) > 4) {
        split (substr(ENVIRON["VERSION"], 0, 4) "." substr(ENVIRON["VERSION"], 5) ".0.0", vsplit, ".")
      }
      print vsplit [1] "." vsplit [2] "." vsplit [3] "." vsplit [4]
    }')]

    echo "MONO_VERSION = $myver" >> $mcs_topdir/build/config.make
    echo "MONO_CORLIB_VERSION = $MONO_CORLIB_VERSION" >> $mcs_topdir/build/config.make

    if test x$host_darwin = xyes; then
      echo "BUILD_PLATFORM = darwin" >> $mcs_topdir/build/config.make
    elif test x$host_win32 = xyes; then
      echo "BUILD_PLATFORM = win32" >> $mcs_topdir/build/config.make
    else
      echo "BUILD_PLATFORM = linux" >> $mcs_topdir/build/config.make
    fi

    if test x$host_darwin = xyes; then
      echo "HOST_PLATFORM ?= darwin" >> $mcs_topdir/build/config.make
    elif test x$host_win32 = xyes; then
      echo "HOST_PLATFORM ?= win32" >> $mcs_topdir/build/config.make
    else
      echo "HOST_PLATFORM ?= linux" >> $mcs_topdir/build/config.make
    fi

    if test "x$PLATFORM_AOT_SUFFIX" != "x"; then
      echo "PLATFORM_AOT_SUFFIX = $PLATFORM_AOT_SUFFIX" >> $mcs_topdir/build/config.make
    fi

	if test x$AOT_SUPPORTED = xyes -a x$enable_system_aot = xdefault; then
	   enable_system_aot=yes
	fi

    if test x$host_win32 = xno -a x$enable_system_aot = xyes; then
      echo "ENABLE_AOT = 1" >> $mcs_topdir/build/config.make
    fi

    if test x$DISABLE_MCS_DOCS = xyes; then
      echo "DISABLE_MCS_DOCS = yes" >> $mcs_topdir/build/config.make
    fi

    if test x$has_extension_module != xno; then
        echo "EXTENSION_MODULE = 1" >> $srcdir/$mcsdir/build/config.make
    fi
    
    echo "DEFAULT_PROFILE = $default_profile" >> $srcdir/$mcsdir/build/config.make
    
    if test "x$test_bcl_opt" = "xyes"; then    
      echo "BCL_OPTIMIZE = 1" >> $srcdir/$mcsdir/build/config.make
    fi

    echo "CSC_LOCATION = $CSC_LOCATION" >> $srcdir/$mcsdir/build/config.make

    if test $csc_compiler = mcs; then
      echo "MCS_MODE = 1" >> $srcdir/$mcsdir/build/config.make
    fi

    if test "x$AOT_BUILD_FLAGS" != "x" ; then
      echo "AOT_RUN_FLAGS=$AOT_RUN_FLAGS" >> $srcdir/$mcsdir/build/config.make
      echo "AOT_BUILD_FLAGS=$AOT_BUILD_FLAGS" >> $srcdir/$mcsdir/build/config.make
    fi

    if test "x$enable_btls" = "xyes"; then
      echo "HAVE_BTLS=1" >> $srcdir/$mcsdir/build/config.make
    fi

  fi

)

libgdiplus_msg=${libgdiplus_loc:-assumed to be installed}

btls_platform_string=
if test x$enable_btls = xyes; then
	if test x$btls_android = xyes; then
		btls_platform_string=" (android:$BTLS_PLATFORM)"
	else
		btls_platform_string=" ($BTLS_PLATFORM)"
	fi
fi

echo "
        mcs source:    $mcsdir
	C# Compiler:   $csc_compiler

   Engine:
	Host:	       $host
	Target:	       $target
	GC:	       $gc_msg 
	TLS:           $with_tls
	SIGALTSTACK:   $with_sigaltstack
	Engine:        $jit_status
	BigArrays:     $enable_big_arrays
	DTrace:        $enable_dtrace
	LLVM Back End: $enable_llvm (dynamically loaded: $enable_loadedllvm)
	Interpreter:   $enable_interpreter

   Libraries:
	.NET 4.x:        $with_profile4_x
	Xamarin.Android: $with_monodroid
	Xamarin.iOS:     $with_monotouch
	Xamarin.WatchOS: $with_monotouch_watch
	Xamarin.TVOS:    $with_monotouch_tv
	Xamarin.Mac:     $with_xammac
	Windows AOT:     $with_winaot
	Orbis:           $with_orbis
	Test profiles:   AOT Full ($with_testing_aot_full), AOT Hybrid ($with_testing_aot_hybrid)
	JNI support:     $jdk_headers_found
	libgdiplus:      $libgdiplus_msg
	zlib:            $zlib_msg
	BTLS:            $enable_btls$btls_platform_string
	$disabled
"
if test x$with_static_mono = xno -a "x$host_win32" != "xyes"; then
   AC_MSG_WARN(Turning off static Mono is a risk, you might run into unexpected bugs)
fi
