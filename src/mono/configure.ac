# Process this file with autoconf to produce a configure script.
#AC_PREREQ([2.62])

# This line is parsed by tools besides autoconf, such as msvc/mono.winconfig.targets.
# It should remain in the format they expect.
#
AC_INIT(mono, [6.13.0],
        [https://github.com/mono/mono/issues/new])

AC_CONFIG_SRCDIR([Makefile.am])
AC_CONFIG_MACRO_DIR([m4])

AC_CANONICAL_SYSTEM
AC_CANONICAL_HOST

# Gross hack to enable 'make dist' on automake 1.9+tar 1.14.
# The extra brackets are to foil regex-based scans.
m4_ifdef([_A][M_PROG_TAR],[_A][M_SET_OPTION([tar-ustar])])

AM_INIT_AUTOMAKE([1.9 dist-xz tar-ustar no-dist-gzip foreign subdir-objects]
                 m4_esyscmd([case `automake --version | head -n 1` in    # parallel-tests is default in automake 1.13+, we need to explicitly enable it
                             *1.11*|*1.12*) echo parallel-tests;;        # for 1.11 and 1.12 but not below as those versions don't recognize the flag
                             esac]))                                     # TODO: remove this hack once we require automake 1.11+

AC_CONFIG_HEADERS([config.h])
AM_MAINTAINER_MODE
m4_ifdef([AM_EXTRA_RECURSIVE_TARGETS], AM_EXTRA_RECURSIVE_TARGETS([test]))
m4_ifdef([AM_EXTRA_RECURSIVE_TARGETS], AM_EXTRA_RECURSIVE_TARGETS([test-bundle]))

API_VER=2.0
AC_SUBST(API_VER)

AC_PROG_LN_S

m4_ifdef([AM_SILENT_RULES], [AM_SILENT_RULES([yes])])

MONO_VERSION_MAJOR=`echo $VERSION | cut -d . -f 1`
MONO_VERSION_MINOR=`echo $VERSION | cut -d . -f 2`
MONO_VERSION_BUILD=`echo $VERSION | cut -d . -f 3`

#
# This is the version of the corlib-runtime interface. When
# making changes to this interface (by changing the layout
# of classes the runtime knows about, changing icall signature or
# semantics etc), change this variable.
#
# This must be unique relative to corlib interface and semantics.
#
# If you change corlib such that a runtime change is required, or
# vice versa, change this string. Examples include removing icalls,
# adding icalls, changing icall signatures, and changing type layouts
# that both sides know.
#
# It is an arbitrary string and should be parsed as such.
# A guid works and is encouraged.
#
# Generate it with uuidgen. For example:
# perl -pi.bak -e "s/^MONO_CORLIB_VERSION=\S+/MONO_CORLIB_VERSION=`uuidgen`/" configure.ac
#
# There is no ordering of corlib versions, no old or new,
# an exact match is required between corlib and runtime.
#
# This line is parsed by tools besides autoconf, such as msvc/mono.winconfig.targets.
# It should remain in the format they expect.
#
MONO_CORLIB_VERSION=1A5E0066-58DC-428A-B21C-0AD6CDAE2789

#
# Put a quoted #define in config.h.
# Substitute @MONO_CORLIB_VERSION@ unquoted.
#
AC_DEFINE_UNQUOTED(MONO_CORLIB_VERSION,"$MONO_CORLIB_VERSION",[Version of the corlib-runtime interface])
AC_SUBST(MONO_CORLIB_VERSION)

case $host_os in
*cygwin* )
		 echo "Run configure using ./configure --host=i686-w64-mingw32 or --host=x86_64-w64-mingw32"
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
target_wasm=no
platform_android=no
platform_tizen=no
platform_ios=no
host_darwin=no
host_linux=no
build_darwin=no
host_sunos=no

case "$host" in
	wasm32*)
		CFLAGS="$CFLAGS -D_REENTRANT -D_GNU_SOURCE -s WASM=1"
		CPPFLAGS="$CPPFLAGS -D_REENTRANT -DUSE_MMAP -s WASM=1"
		libgc_threads=pthreads
		host_wasm=yes
		# FIXME:
		RID="osx-x64"
		COREARCH="wasm32"
		CORETARGETS="-p:TargetsUnix=true -p:TargetsOSX=true"
		build_darwin=yes
		;;
	*-mingw*|*-*-cygwin*)
		AC_DEFINE(DISABLE_PORTABILITY,1,[Disable the io-portability layer])
		AC_DEFINE(HOST_NO_SYMLINKS,1,[This platform does not support symlinks])
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
		RID="win-x86"
		CORETARGETS="-p:TargetsWindows=true"
		COREARCH="x86"
		# Boehm not supported on 64-bit Windows.
		case "$host" in
		x86_64-*-* | amd64-*-*)
			with_gc=sgen
			RID="win-x64"
			COREARCH="x64"
			;;
		esac

		# Windows 7 or later is required
		WIN32_CPPFLAGS="-DWINVER=0x0601 -D_WIN32_WINNT=0x0601 -D_WIN32_IE=0x0501 -D_UNICODE -DUNICODE -DWIN32_THREADS -DFD_SETSIZE=1024"
		CPPFLAGS="$CPPFLAGS $WIN32_CPPFLAGS"
		WIN32_LDFLAGS="-lbcrypt -lmswsock -lws2_32 -lole32 -loleaut32 -lpsapi -lversion -ladvapi32 -lwinmm -lkernel32 -liphlpapi -static-libgcc"
		LDFLAGS="$LDFLAGS $WIN32_LDFLAGS"
		libmono_cflags="-mms-bitfields -mwindows"
		libmono_ldflags="-mms-bitfields -mwindows"
		libgc_threads=win32
		with_sigaltstack=no
		with_tls=pthread
		with_sgen_default_concurrent=yes
		LN_S=cp
		;;
	*-*-*netbsd*)
		CPPFLAGS="$CPPFLAGS -D_REENTRANT -DGC_NETBSD_THREADS -D_GNU_SOURCE"
		libmono_cflags="-D_REENTRANT"
		LDFLAGS="$LDFLAGS -pthread"
		CPPFLAGS="$CPPFLAGS -DHOST_BSD"
		libmono_ldflags="-pthread"
		need_link_unlink=yes
		libgc_threads=pthreads
		with_sigaltstack=no
		use_sigposix=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-kfreebsd*-gnu)
		CPPFLAGS="$CPPFLAGS -DGC_FREEBSD_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP -DUSE_MUNMAP -DTHREAD_LOCAL_ALLOC -pthread"
		libmono_cflags="-D_REENTRANT -DTHREAD_LOCAL_ALLOC -pthread"
		libmono_ldflags="-lpthread -pthread"
		libgc_threads=pthreads
		need_link_unlink=yes
		with_sigaltstack=no
		use_sigposix=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-*freebsd*)
		dnl For close_my_fds
		LDFLAGS="$LDFLAGS -lutil"
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
		CPPFLAGS="$CPPFLAGS -DHOST_BSD -D_WITH_GETLINE"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID, 1, [pthread is a pointer])
		libgc_threads=pthreads
		use_sigposix=yes
		has_dtrace=yes
		case "$host" in
		aarch64-*)
			support_boehm=no
			with_gc=sgen
			;;
		riscv*)
			support_boehm=no
			with_gc=sgen
			;;
		esac
		with_sgen_default_concurrent=yes
		;;
	*-*-*openbsd*)
		CPPFLAGS="$CPPFLAGS -D_THREAD_SAFE -DGC_OPENBSD_THREADS -DHOST_BSD -D_REENTRANT -DUSE_MMAP"
		LDFLAGS="${LDFLAGS} -Wl,-zwxneeded"
		if test "x$disable_munmap" != "xyes"; then
		CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_THREAD_SAFE -D_REENTRANT"
		LDFLAGS="$LDFLAGS -pthread"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID)
		libgc_threads=pthreads
		with_tls=pthread
		with_sigaltstack=no
		use_sigposix=yes
		with_sgen_default_concurrent=yes
		;;
	*-*-linux-android*)
		platform_android=yes
		AC_DEFINE(HOST_ANDROID,1,[Targeting the Android platform])
		AC_DEFINE(TARGET_ANDROID,1,[Targeting the Android platform])

		CPPFLAGS="$CPPFLAGS -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP"
		if test "x$disable_munmap" != "xyes"; then
			CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_REENTRANT"
		libgc_threads=pthreads
		use_sigposix=yes

		with_tls=pthread
		with_sigaltstack=no
		with_static_mono=no

		# Android doesn't support boehm, as it's missing <link.h>
		support_boehm=no
		with_gc=sgen

		# isinf(3) requires -lm
		LDFLAGS="$LDFLAGS -lm"

		# Bionic's <pthread.h> sets PTHREAD_STACK_MIN=2*PAGE_SIZE; doesn't define
		# PAGE_SIZE; breaks mono/io-layer/collection.c
		# Bionic doesn't provide S_IWRITE; breaks io-layer/io.c
		CFLAGS="$CFLAGS -DPAGE_SIZE=4096 -DS_IWRITE=S_IWUSR"
		CXXFLAGS="$CXXFLAGS -DPAGE_SIZE=4096 -DS_IWRITE=S_IWUSR"

		# FIXME? Only if __ANDROID_API__ < 24?
		# FILE32API means "long", which is stuck at 32bits for 32bit ABI (64bits for 64bit ABI).
		# ABI 24 introduces the "o" for "file offset" variations, which can be widened to 64bits for 32bit ABI (and still 64bits for 64bit).
		# Android itself uses FILE32API, and mono should use system zlib on Android anyway.
		ZLIB_CFLAGS="$ZLIB_CFLAGS -DUSE_FILE32API"

		# to bypass the underscore linker check, can't work when cross-compiling
		mono_cv_uscore=yes
		mono_cv_clang=no
		;;
	*-*-linux*)
		host_linux=yes
		CPPFLAGS="$CPPFLAGS -DGC_LINUX_THREADS -D_GNU_SOURCE -D_REENTRANT -DUSE_MMAP"
		if test "x$disable_munmap" != "xyes"; then
			CPPFLAGS="$CPPFLAGS -DUSE_MUNMAP"
		fi
		libmono_cflags="-D_REENTRANT"
		libgc_threads=pthreads
		CORETARGETS="-p:TargetsUnix=true"
		use_sigposix=yes
		if test "x$cross_compiling" != "xno"; then
                	# to bypass the underscore linker check, not
                	# available during cross-compilation
                	mono_cv_uscore=no
                fi
                case "$host" in
                *-musl)
                        AC_DEFINE(MUSL, 1, [musl libc])
                        ;;
                esac
		case "$host" in
		*-tizen-linux-*)
			platform_tizen=yes
			;;
		esac
		case "$host" in
		ppc64-*)
			RID="linux-ppc64"
			;;
		ppc64le-*)
			RID="linux-ppc64el"
			;;
		mipsel-*)
			RID="linux-mipsel"
			;;
		x86-*)
			RID="linux-x86"
			COREARCH="x86"
			;;
		x86_64-*)
			RID="linux-x64"
			COREARCH="x64"
			;;
		arm-*)
			# deal with this in the FPU detection section, since
			# we cannot determine FPU from triplet and don't want
			# to duplicate the logic
			;;
		aarch64-*)
			support_boehm=no
			with_gc=sgen
			RID="linux-arm64"
			COREARCH="arm64"
			;;
		powerpc*-*-linux*)
			# https://bugzilla.novell.com/show_bug.cgi?id=504411
			disable_munmap=yes
			;;
		powerpc*-*-freebsd*)
			# https://bugzilla.novell.com/show_bug.cgi?id=504411
			disable_munmap=yes
			;;
		riscv*)
			support_boehm=no
			with_gc=sgen
			;;
		esac
		with_sgen_default_concurrent=yes
		;;
	*-*-nacl*)
		echo "nacl no longer supported."
		exit 1
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
		CPPFLAGS="$CPPFLAGS -DGC_SOLARIS_THREADS -DGC_SOLARIS_PTHREADS -D_REENTRANT -D_POSIX_PTHREAD_SEMANTICS -DUSE_MMAP -DUSE_MUNMAP -DHOST_SOLARIS -D__EXTENSIONS__ -D_XPG4_2"
		need_link_unlink=yes
		libmono_cflags="-D_REENTRANT"
		libgc_threads=pthreads
		has_dtrace=yes
		use_sigposix=yes
		enable_solaris_tar_check=yes
		host_sunos=yes
		;;
	*-*-darwin*)
		parallel_mark="Disabled_Currently_Hangs_On_MacOSX"
		host_darwin=yes
		target_mach=yes
		CPPFLAGS="$CPPFLAGS -D_THREAD_SAFE -DGC_MACOSX_THREADS -DUSE_MMAP -DUSE_MUNMAP"
		libmono_cflags="-D_THREAD_SAFE"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID)
		AC_DEFINE(USE_MACH_SEMA, 1, [...])
		CORETARGETS="-p:TargetsUnix=true -p:TargetsOSX=true"
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
				BROKEN_DARWIN_FLAGS="-arch i386"
				BROKEN_DARWIN_CPPFLAGS=""
				CPPFLAGS="$CPPFLAGS $BROKEN_DARWIN_CPPFLAGS"
				CFLAGS="$CFLAGS $BROKEN_DARWIN_FLAGS"
				CXXFLAGS="$CXXFLAGS $BROKEN_DARWIN_FLAGS"
				CCASFLAGS="$CCASFLAGS $BROKEN_DARWIN_FLAGS"
				CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC $BROKEN_DARWIN_CPPFLAGS"
				CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC $BROKEN_DARWIN_FLAGS"
				with_sgen_default_concurrent=yes
				RID="osx-x86"
				COREARCH="x86"
				;;
			x*64-*-darwin*)
				with_sgen_default_concurrent=yes
				RID="osx-x64"
				COREARCH="x64"
				;;
			arm*-darwin*)
				platform_ios=yes
				has_dtrace=no
				;;
			aarch64*-darwin*)
				platform_ios=yes
				;;
		esac
		;;
	*-*-haiku*)
		dnl BSD_SOURCE is for getifaddrs
		CPPFLAGS="$CPPFLAGS -D_BSD_SOURCE -D_REENTRANT -D_THREAD_SAFE"
		libmono_cflags="-D_REENTRANT -D_THREAD_SAFE"
		LIBS="$LIBS -lnetwork -ltextencoding"
		need_link_unlink=yes
		AC_DEFINE(PTHREAD_POINTER_ID)
		dnl Haiku does not support static TLS with __thread
		with_tls=pthread
		libgc_threads=pthreads
		use_sigposix=yes
		with_sigaltstack=no
		;;
	*-*-fuchsia*)
		AC_DEFINE(DISABLE_PORTABILITY,1,[Disable the io-portability layer])
		AC_DEFINE(HOST_FUCHSIA,1,[Targeting the Fuchsia platform])
		fuchsia=true
		with_tls=pthread
		with_sigaltstack=yes
		with_static_mono=no
		support_boehm=no
		with_gc=sgen
		mono_cv_uscore=yes
		mono_cv_clang=no
		;;
	*-*-aix*|*-*-os400*)
		AC_DEFINE(HOST_AIX,1,[Targeting the AIX and PASE platforms])
		host_aix=yes
		dnl IMPORTANT: For svr4 sonames on AIX, you should set
		dnl `OBJECT_MODE=64` when configuring.
		dnl libtool cannot generate functioning svr4 sonames on
		dnl 64-bit without it.
		dnl Unfortunately, everything is complicated by the fact that
		dnl gcc doesn't respect this variable. (otherwise we could set
		dnl it for build time for configure and make)
		dnl On IBM i PASE using IBM's packages, GCC *does* respect this
		dnl variable, and builds are 64-bit by default. svr4 sonames
		dnl must still be specified when configuring on i, however.
		dnl As such, because Mono doesn't support 32-bit AIX or PASE,
		dnl set up a 64-bit build (assming GCC; XLC not supported)
		dnl regardless of what variable is used.
		case $host_os in
			aix*)
			if test "x$OBJECT_MODE" = "x64" && test "x$CC" = "x" && test "x$CXX" = "x"; then
				dnl HACK: Set -maix64 at the GCC invocation
				dnl level explicitly to work around the fact
				dnl GCC in default maix32 mode explodes when
				dnl binutils respects OBJECT_MODE.
				dnl When that check occurs, flags are not
				dnl passed to to the compiler, so GCC has no
				dnl chance to change its mode.
				dnl Otherwise, it may enter a state where it
				dnl runs, but uses the libtool "compile"
				dnl wrapper, which subtly breaks other things.
				dnl This should propagate to all uses of CC.
				dnl This is only set if not manually set.
				CC="gcc -maix64"
				CXX="g++ -maix64"
				echo
			fi
			;;
			dnl Not needed on i because it defaults to 64-bit and
			dnl has a GCC smart enough to respect OBJECT_MODE.
		esac
		dnl We still set this for *FLAGS, however, because we may not
		dnl be setting OBJECT_MODE.
		LDFLAGS="$LDFLAGS -maix64"
		CPPFLAGS="$CPPFLAGS -maix64 -DGC_AIX_THREADS -D_ALL_SOURCE -pthread -D_THREAD_SAFE -D_LARGE_FILES -D_REENTRANT"
		libmono_cflags="-pthread -D_THREAD_SAFE -D_REENTRANT"
		dnl Would you believe GNU nm doesn't know how to process AIX libraries?
		dnl Hardcode IBM binutils in case GNU ones end up on our path. Also
		dnl specifiy 64-bit mode for tools. (OBJECT_MODE is finicky with cmake.)
		dnl XXX: We should stop the hardcoding madness
		AR="/usr/bin/ar -X64"
		NM="/usr/bin/nm -X64"
		STRIP="/usr/bin/strip -X64"
		RANLIB="/usr/bin/ranlib -X64"
		dnl SGen is the future (changes to Boehm support code would be
		dnl required if you wish to re-enable Boehm)
		support_boehm=no
		with_gc=sgen
		need_link_unlink=yes
		use_sigposix=yes
		dnl Similar limitation to macOS about the first thread and the
		dnl guard page, except sometimes the runtime hangs. Disable for
		dnl now until cause can be determined or it seems OK enough.
		with_sigaltstack=no
		dnl use pthread TLS, __thread has issues with the compiler flags we use
		with_tls=pthread
		dnl ppc Linux is the same? test further
		disable_munmap=yes
		RID="aix-ppc64"
		CORETARGETS="-p:TargetsUnix=true"
		;;
	*)
		AC_MSG_WARN([*** Please add $host to configure.ac checks!])
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

if test x$host_darwin = xyes; then
   AC_DEFINE(HOST_DARWIN, 1, [Host Platform is Darwin])
fi

# Defined for all targets/platforms using classic Windows API support.
AC_DEFINE(HAVE_CLASSIC_WINAPI_SUPPORT, 1, [Use classic Windows API support])
AC_DEFINE(HAVE_UWP_WINAPI_SUPPORT, 0, [Don't use UWP Windows API support])

AC_SUBST(extra_runtime_ldflags)
AM_CONDITIONAL(HOST_WIN32, test x$host_win32 = xyes)
AM_CONDITIONAL(TARGET_WIN32, test x$target_win32 = xyes)
AM_CONDITIONAL(HOST_LINUX, echo x$target_os | grep -q linux)
AM_CONDITIONAL(HOST_DARWIN, test x$host_darwin = xyes)
AM_CONDITIONAL(HOST_SIGPOSIX, test x$use_sigposix = xyes)
AM_CONDITIONAL(HOST_ANDROID, test x$platform_android = xyes)
AM_CONDITIONAL(HOST_TIZEN, test x$platform_tizen = xyes)
AM_CONDITIONAL(HOST_IOS, test x$platform_ios = xyes)
AM_CONDITIONAL(HOST_WASM, test x$host_wasm = xyes)
AM_CONDITIONAL(HOST_AIX, test x$host_aix = xyes)

if test -z "$HOST_DARWIN_TRUE"; then :
PLATFORM_AOT_SUFFIX=.dylib
PLATFORM_AOT_PREFIX=lib
fi

if test -z "$HOST_LINUX_TRUE"; then :
PLATFORM_AOT_SUFFIX=.so
PLATFORM_AOT_PREFIX=lib
fi

if test -z "$HOST_WIN32_TRUE"; then :
PLATFORM_AOT_SUFFIX=.dll
PLATFORM_AOT_PREFIX=
fi

AC_SUBST(PLATFORM_AOT_SUFFIX)
AC_SUBST(PLATFORM_AOT_PREFIX)

if test -z "$HOST_WASM_TRUE"; then :
AC_DEFINE(HAVE_UTIME)
AC_DEFINE(HAVE_UTIMES)
fi

## PLATFORM_AOT_SUFFIX not so simple for windows :-)

AC_CHECK_TOOL(CC, gcc, gcc)
AC_PROG_CC
AC_CHECK_TOOL(CXX, g++, g++)
AC_PROG_CXX
AM_PATH_PYTHON([3.2])
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

if test "x$CXX" = "xno"; then
	AC_MSG_ERROR([No c++ compiler found. You need to install a c++ compiler])
fi

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

AC_CHECK_HEADERS(sys/filio.h sys/sockio.h netdb.h utime.h sys/utime.h semaphore.h sys/un.h linux/rtc.h sys/syscall.h sys/mkdev.h sys/uio.h sys/param.h sys/sysctl.h libproc.h sys/prctl.h copyfile.h gnu/lib-names.h)
AC_CHECK_HEADERS(sys/param.h sys/socket.h sys/ipc.h sys/utsname.h alloca.h ucontext.h pwd.h sys/select.h netinet/tcp.h netinet/in.h unistd.h sys/types.h link.h asm/sigcontext.h sys/inotify.h arpa/inet.h complex.h unwind.h)

# zlib/configure checks for unistd.h existence and defines HAVE_UNISTD_H on the compiler
# command line (not config.h) if it is present.
AC_CHECK_HEADER(unistd.h,
    [HAVE_UNISTD_H=1
     ZLIB_CFLAGS="$ZLIB_CFLAGS -DHAVE_UNISTD_H"],
    [HAVE_UNISTD_H=0])

AC_SUBST(HAVE_UNISTD_H)

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

case "$host" in
	# Use bundled zlib on Windows to be sure it is static.
	*-mingw*|*-cygwin*)
		have_sys_zlib=no
		zlib_msg="bundled zlib"
		;;
	# Use bundled zlib on RISC-V for now to make cross-compiling easier.
	riscv*)
		have_sys_zlib=no
		zlib_msg="bundled zlib"
		;;
	*)
		AC_CHECK_HEADER(zlib.h, [have_sys_zlib=yes], [have_sys_zlib=no])
		if test x$have_sys_zlib = xyes; then
			AC_TRY_COMPILE([#include <zlib.h>], [
				#if defined(ZLIB_VERNUM) && (ZLIB_VERNUM >= 0x1230)
				#else
				#error No good zlib found
				#endif
			],[
				AC_MSG_RESULT(Using system zlib)
				zlib_msg="system zlib"
				have_sys_zlib=yes
			],[
				AC_MSG_RESULT(zlib too old, using embedded zlib)
				have_sys_zlib=no
				zlib_msg="bundled zlib"
			])
		else
			AC_MSG_RESULT(zlib not found, using embedded zlib)
			have_sys_zlib=no
			zlib_msg="bundled zlib"
		fi
		;;
esac

AC_ARG_WITH(static-zlib, [  --with-static-zlib=PATH    use the specified static zlib instead of -lz],[STATIC_ZLIB_PATH=$with_static_zlib],[STATIC_ZLIB_PATH=])
if test "x$STATIC_ZLIB_PATH" != "x"; then
	have_static_zlib=yes
	have_sys_zlib=no
	zlib_msg="static zlib from $STATIC_ZLIB_PATH"
	AC_SUBST(STATIC_ZLIB_PATH)
fi

AM_CONDITIONAL(HAVE_STATIC_ZLIB, test x$have_static_zlib = xyes)
AM_CONDITIONAL(HAVE_SYS_ZLIB, test x$have_sys_zlib = xyes)
if test x$have_static_zlib = xyes; then
	AC_DEFINE(HAVE_STATIC_ZLIB, 1, [Use static zlib])
fi
if test x$have_sys_zlib = xyes; then
	AC_DEFINE(HAVE_SYS_ZLIB, 1, [Use OS-provided zlib])
fi
# for mono/metadata/debug-symfile.c
AC_CHECK_HEADERS(elf.h)

# for perf jit dump support
AC_CHECK_HEADERS(sys/mman.h)
if test "x$host_linux" = "xyes" -a x$ac_cv_header_sys_mman_h = xyes -a  x$ac_cv_header_elf_h = xyes -a x$ac_cv_header_sys_syscall_h = xyes; then
	AC_DEFINE(ENABLE_JIT_DUMP, 1, [Enable jit dump support on Linux])
fi

# for support
AC_CHECK_HEADERS(poll.h)
AC_CHECK_HEADERS(sys/poll.h)
AC_CHECK_HEADERS(sys/wait.h)
AC_CHECK_HEADERS(grp.h)
AC_CHECK_HEADERS(syslog.h)
AC_CHECK_FUNCS(vsyslog)

# for mono/dis
AC_CHECK_HEADERS(wchar.h)

# for Linux statfs support
AC_CHECK_HEADERS(linux/magic.h)

# For Android NDK unified headers
if test x$platform_android = xyes; then
	AC_CHECK_HEADERS(machine/endian.h sys/endian.h)
	AC_CHECK_HEADERS(android/legacy_signal_inlines.h, [have_android_signal_inlines=yes], [have_android_signal_inlines=no])
	AC_CHECK_HEADERS(android/ndk-version.h)

	# Make sure SIGRT{MIN,MAX} work - they will fail to work with unified headers if building for
	# API level < 21 *and* android/legacy_signal_inlines.h doesn't declare (and define) the required
	# libc APIs to obtain values for SIGRT{MIN,MAX}. We perform the check only if android/legacy_signal_inlines.h
	# is found because in other cases the macros will either work (for NDK < 14) or fail if the legacy header
	# doesn't contain the required definitions (NDK 14)
	if test x$have_android_signal_inlines = xyes; then
		AC_MSG_CHECKING([Whether Android SIGRTMIN/SGRTMAX macros are valid])
		AC_COMPILE_IFELSE([
			AC_LANG_PROGRAM([#include <signal.h>
				#include <android/legacy_signal_inlines.h>],[
				int i;
				for (i = SIGRTMIN + 1; i < SIGRTMAX; ++i) {
				}
			])],[
				AC_MSG_RESULT(yes)
				android_sigrtminmax_work=yes
			],[
				AC_MSG_RESULT(no)
				android_sigrtminmax_work=no
			]
		)

		if test x$android_sigrtminmax_work = xno; then
			AC_MSG_ERROR([Android SIGRTMIN/SIGRTMAX macros don't work in this NDK])
		fi
	fi

	# Attempt to detect whether we're using Android NDK unified headers
	AC_CHECK_HEADERS(android/api-level.h, [have_android_api_level=yes], [have_android_api_level=no])
	AC_CHECK_HEADERS(android/versioning.h, [have_android_versioning=yes], [have_android_versioning=no])

	android_unified_headers=no
	if test x$have_android_api_level = xyes; then
		if test x$have_android_versioning = xyes; then
			AC_MSG_CHECKING([whether using Android NDK unified headers])

			# Both macros are defined only in the NDK unified headers
			AC_COMPILE_IFELSE([
				AC_LANG_PROGRAM([
					#include <android/api-level.h>
					#include <android/versioning.h>
				],[
					#if __ANDROID_API_O__ == 26 && defined(__INTRODUCED_IN)
					#else
						#error __ANDROID_API_O__ != 26 or the __INTRODUCED_IN macro not defined
					#endif
				])],[
					AC_MSG_RESULT(yes)
					android_unified_headers=yes
				],[
					AC_MSG_RESULT(no)
					android_unified_headers=no
				]
			)
		fi
	fi

	if test x$android_unified_headers = xyes; then
		AC_DEFINE(ANDROID_UNIFIED_HEADERS, 1, [Whether Android NDK unified headers are used])
	fi
fi # Android

# not 64 bit clean in cross-compile
AC_CHECK_SIZEOF(void *)
AC_CHECK_SIZEOF(long)

AC_CHECK_SIZEOF(int)
AC_CHECK_SIZEOF(long long)

AC_CACHE_CHECK([for clang],
	mono_cv_clang,[
	AC_TRY_COMPILE([], [
		#ifdef __clang__
		#else
		#error "FAILED"
		#endif
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
		WARN='-Wall -Wunused -Wmissing-declarations -Wpointer-arith -Wno-cast-qual -Wwrite-strings -Wno-switch -Wno-switch-enum -Wno-unused-value -Wno-attributes'
		CFLAGS="$CFLAGS -Wmissing-prototypes -Wstrict-prototypes -Wnested-externs -Wno-format-zero-length"

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
		   CXXFLAGS="$CXXFLAGS -fvisibility=hidden"
		fi

		ORIG_CFLAGS=$CFLAGS
		# Check for the normal version, since gcc ignores unknown -Wno options
		CFLAGS="$CFLAGS -Wunused-but-set-variable -Werror"
		AC_MSG_CHECKING(for -Wno-unused-but-set-variable option to gcc)
		AC_TRY_COMPILE([],[
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
CFLAGS="$WARN $CFLAGS -g"
CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -g"
CPPFLAGS="$WARN $CPPFLAGS -g"

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

AC_ARG_WITH(core,       [  --with-core=only       controls whether to build Mono as a .NET Core runtime (defaults to no)],[],[with_core=no])
if test x$with_core = xonly; then
    AC_DEFINE(ENABLE_NETCORE,1,[Enables the support for .NET Core Features in the MonoVM])
    with_runtime_preset=netcore
fi
AM_CONDITIONAL(ENABLE_NETCORE, test x$with_core = xonly)

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

AC_ARG_ENABLE(ninja,[  --enable-ninja    Enable using ninja where available], enable_ninja=$enableval)
AC_CHECK_PROG(HAVE_NINJA, ninja, yes, no)
AM_CONDITIONAL(NINJA, test x$enable_ninja = xyes -a x$HAVE_NINJA != xno)

AC_ARG_ENABLE(werror, [  --enable-werror Pass -Werror to the C compiler], werror_flag=$enableval, werror_flag=no)
if test x$werror_flag = xyes; then
	WERROR_CFLAGS="-Werror"
fi
AC_SUBST([WERROR_CFLAGS])
AC_SUBST([SHARED_CFLAGS])

GLIB_CFLAGS='-I$(top_srcdir)/mono/eglib -I$(top_builddir)/mono/eglib'
  
AC_SUBST(GLIB_CFLAGS)

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

if test "x$host_win32" = "xyes"; then
   # Boehm GC requires the runtime to be in its own dll
   with_static_mono=no
fi

AM_CONDITIONAL(STATIC_MONO, test x$with_static_mono != xno)
AM_CONDITIONAL(SHARED_MONO, test x$with_shared_mono != xno)
AC_ARG_ENABLE(mcs-build, [  --disable-mcs-build disable the build of the mcs directory], try_mcs_build=$enableval, enable_mcs_build=yes)
AC_ARG_ENABLE(support-build, [  --disable-support-build disable the build of the support directory], try_support_build=$enableval, enable_support_build=yes)

AC_ARG_WITH(xen_opt,   [  --with-xen_opt=yes,no          Enable Xen-specific behaviour (defaults to yes)],[],[with_xen_opt=yes])
if test "x$with_xen_opt" = "xyes" -a "x$mono_cv_clang" = "xno"; then
	AC_DEFINE(MONO_XEN_OPT, 1, [Xen-specific behaviour])
	ORIG_CFLAGS=$CFLAGS
	CFLAGS="$CFLAGS -mno-tls-direct-seg-refs"
	AC_MSG_CHECKING(for -mno-tls-direct-seg-refs option to gcc)
	AC_TRY_COMPILE([], [
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
      case "$host" in
        s390x*) csc_compiler=roslyn ;;
        *) csc_compiler=mcs
      esac
   elif test $endian = little; then
      case "$host" in
        powerpc*) csc_compiler=mcs ;;
        *) csc_compiler=roslyn ;;
      esac
   else
      csc_compiler=mcs
   fi
fi
AC_MSG_RESULT($csc_compiler)

AC_ARG_WITH(jemalloc,             [  --with-jemalloc=yes,no               If jemalloc is enabled (defaults to no)],                                     [], [with_jemalloc=no])
AC_ARG_WITH(jemalloc-always,      [  --with-jemalloc_always=yes,no        If jemalloc is enabled and always used (defaults to yes)],                    [], [with_jemalloc_always=no])
AC_ARG_WITH(jemalloc-assert,      [  --with-jemalloc_assert=yes,no        If jemalloc performs runtime checks for memory correctness (defaults to no)], [], [with_jemalloc_assert=no])


if test x$target_win32 = xyes; then
with_jemalloc=no
with_jemalloc_assert=no
with_jemalloc_always=no
fi

AM_CONDITIONAL(MONO_JEMALLOC_ASSERT, [test "x$with_jemalloc_assert" = "xyes"])
if test "x$with_jemalloc_assert" = "xyes"; then
JEMALLOC_CFLAGS+=" -DMONO_JEMALLOC_ASSERT"
AC_DEFINE(MONO_JEMALLOC_ASSERT, 1, [Make jemalloc assert for mono])
AC_SUBST(MONO_JEMALLOC_ASSERT, 1)
fi

AM_CONDITIONAL(MONO_JEMALLOC_DEFAULT, [test "x$with_jemalloc_always" = "xyes"])
if test "x$with_jemalloc_always" = "xyes"; then
with_jemalloc=yes
JEMALLOC_CFLAGS+=" -DMONO_JEMALLOC_DEFAULT"
AC_DEFINE(MONO_JEMALLOC_DEFAULT, 1, [Make jemalloc default for mono])
AC_SUBST(MONO_JEMALLOC_DEFAULT, 1)
fi

AM_CONDITIONAL(MONO_JEMALLOC_ENABLED, [test "x$with_jemalloc" = "xyes"])
if test "x$with_jemalloc" = "xyes"; then
JEMALLOC_LDFLAGS="-L`pwd`/mono/utils/jemalloc/jemalloc/lib -ljemalloc_pic"
JEMALLOC_CFLAGS+=" -DMONO_JEMALLOC_ENABLED -I `pwd`/mono/utils/jemalloc/jemalloc/include"
JEMALLOC_AUTOCONF_FLAGS=" --build=$target --host=$host"

if test "x$target_mach" = "xyes"; then
	JEMALLOC_CPPFLAGS=" -stdlib=libc++ "
fi

AC_DEFINE(MONO_JEMALLOC_ENABLED, 1, [Enable jemalloc usage for mono])
AC_SUBST(MONO_JEMALLOC_ENABLED, 1)

AC_SUBST(JEMALLOC_CFLAGS)
AC_SUBST(JEMALLOC_CPPFLAGS)
AC_SUBST(JEMALLOC_LDFLAGS)
AC_SUBST(JEMALLOC_AUTOCONF_FLAGS)
fi

# Terminology:
# Crash privacy - Attempts to not send identifying information in the crash dump / to protect the obscurity of the program control flow
# MERP - The MS telemetry profile. Not for general use.
# Structured crashes - crashes at runtime that trigger a stack walk by mono that happens cooperatively

# Structured crashes are not merp crashes. Structured crashes are json dumps made by mono during crashes. Merp upload is going to use the dumping code is a very specific way, and is enabled at runtime with an icall.

#--with-crash_privacy=yes --with-structured_crashes=no means we don't wanna dump in non-merp-enabled builds, and we want to not send symbol strings. This is going to be the default pair of settings for VS4Mac.
#--with-crash_privacy=yes --with-structured_crashes=yes means you want to see crashes on your console, and you want to not see unmanaged symbol names. This is an option for proprietary apps that have manual bugs filed.
#--with-crash_privacy=no --with-structured_crashes=no means you want to see no crash dumps on failure and you don't care about privacy. This is how you'd set a "don't want it, don't care" configuration.
#--with-crash_privacy=no --with-structured_crashes=yes means you want full crashes and you want to see them in the terminal, not on telemetry. This is going to be how we build for CI.

AC_ARG_WITH(crash-privacy,       [  --with-crash_privacy=yes,no         If you want your crashes to not include names of symbols present in the binary. ], [], [with_crash_privacy=yes])
AC_ARG_WITH(structured-crashes,  [  --with-structured_crashes=yes,no    If you want your unmanaged crashes to result in a small crash dump file. ],        [], [with_structured_crashes=yes])
AC_ARG_ENABLE(crash-reporting,  [  --disable-crash-reporting            Enable or Disable crash reporting subsystem],        [crash_reporting=$enableval], [crash_reporting=yes])

if test "x$with_crash_privacy" = "xyes"; then
AC_DEFINE(MONO_PRIVATE_CRASHES,1,[Do not include names of unmanaged functions in the crash dump])
fi

if test "x$with_structured_crashes" = "xno"; then
AC_DEFINE(DISABLE_STRUCTURED_CRASH,1,[Do not create structured crash files during unmanaged crashes])
fi

case "$host" in
	*-mingw*|*-*-cygwin*)
		crash_reporting=no
		;;
esac

if test "x$crash_reporting" != "xyes"; then
CFLAGS="$CFLAGS -DDISABLE_CRASH_REPORTING=1"
CXXFLAGS="$CXXFLAGS -DDISABLE_CRASH_REPORTING=1"
fi

AC_ARG_ENABLE(monodroid, [ --enable-monodroid Enable runtime support for Monodroid (Xamarin.Android)], enable_monodroid=$enableval, enable_monodroid=no)
AM_CONDITIONAL(ENABLE_MONODROID, test x$enable_monodroid = xyes)

AC_ARG_ENABLE(monotouch, [ --enable-monotouch Enable runtime support for Monotouch (Xamarin.iOS and Xamarin.Mac)], enable_monotouch=$enableval, enable_monotouch=no)
AM_CONDITIONAL(ENABLE_MONOTOUCH, test x$enable_monotouch = xyes)

if test x$enable_monodroid = xyes; then
	AC_DEFINE(ENABLE_MONODROID, 1, [Enable runtime support for Monodroid (Xamarin.Android)])
fi

if test x$enable_monotouch = xyes; then
	AC_DEFINE(ENABLE_MONOTOUCH, 1, [Enable runtime support for Monotouch (Xamarin.iOS and Xamarin.Mac)])
fi

AC_ARG_ENABLE(cxx, [  --enable-cxx   compile some code as C++])
AM_CONDITIONAL(ENABLE_CXX, test x$enable_cxx = xyes)

# mono/corefx/native has a lot of invalid C++98 in its headers
# dotnet/corefx/native looks a lot better, i.e. 44e5bdafb8d989a220c9cf1b94f31a64a6e4f052
# use these C99 print macros in the command line, since glib may not win first inttypes include
#CXXFLAGS_COMMON=' -std=gnu++98 -fno-exceptions -fno-rtti '
CXXFLAGS_COMMON=' -std=gnu++0x -fno-exceptions -fno-rtti '
CXXFLAGS_COMMON="$CXXFLAGS_COMMON -D__STDC_LIMIT_MACROS -D__STDC_CONSTANT_MACROS -D__STDC_FORMAT_MACROS"
# "c++0x" instead of C++11, for compat with Centos6/gcc4.4

# -stdlib=libc++ is needed by clang for iOS 6.0 (-miphoneos-version-min=6.0)
# to support C++11 headers but it does not seem to harm elsewhere, so over-approximate
# and add it whenever we're running clang on Darwin.
if test "x$mono_cv_clang" = xyes -a x$host_darwin = xyes; then
	CXXFLAGS_COMMON="$CXXFLAGS_COMMON -stdlib=libc++"
fi

AC_SUBST(CXXFLAGS_COMMON)

if test "x$enable_cxx" = "xyes"; then

	CXX_ADD_CFLAGS=" -xc++ $CXXFLAGS_COMMON "

	# -std=gnu99 -xc++ is not allowed and errors.
	CXX_REMOVE_CFLAGS=-std=gnu99
	# These give warnings and should be removed. They are C-only.
	# i.e. C++ never allows these, they are always errors and their warningness is not controllable.
	CXX_REMOVE_CFLAGS="$CXX_REMOVE_CFLAGS -Wmissing-prototypes -Wstrict-prototypes -Wnested-externs"
	# Likewise with CentOS 6 gcc 4.4.
	CXX_REMOVE_CFLAGS="$CXX_REMOVE_CFLAGS -Werror-implicit-function-declaration"

	# The C-only-ness of -Wno-format-zero-length varies with gcc version.
	# It is C-only prior to 4.7. Using it with C++ prior to 4.7
	# generates a warning every time we run gcc which is very unsightly.
	# The warning is for e.g. sprintf(foo, "") which can just be
	# foo[0] = 0 but Mono's use is more elaborate, not as easy to "fix",
	# and completely legal and valid.
	# We have to switch to C++ and not just use -xc++ because of -std=gnu99 (error when combined with -xc++).
	# Alternatively, just run $CXX -xc++ -c /dev/null.
	AC_LANG_PUSH(C++)
	ORIG_CXXFLAGS=$CXXFLAGS
	CXXFLAGS="$CXXFLAGS -Werror -Wno-format-zero-length -xc++ "
	AC_MSG_CHECKING(or C-only-ness of -Wno-format-zero-length)
	AC_TRY_COMPILE([ ], [ ], [ AC_MSG_RESULT(yes) ],
				 [ AC_MSG_RESULT(no)
				   CXX_REMOVE_CFLAGS="$CXX_REMOVE_CFLAGS -Wno-format-zero-length" ])
	CXXFLAGS=$ORIG_CXXFLAGS
	AC_LANG_POP(C++)
fi
AC_SUBST(CXX_ADD_CFLAGS)
AC_SUBST(CXX_REMOVE_CFLAGS)

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
AC_ARG_WITH(testing_aot_full_interp,  [  --with-testing_aot_full_interp=yes,no    If you want to build the testing_aot_full_interp assemblies (defaults to no)],   [], [with_testing_aot_full_interp=default])
AC_ARG_WITH(testing_aot_hybrid,  [  --with-testing_aot_hybrid=yes,no    If you want to build the testing_aot_hybrid assemblies (defaults to no)],   [], [with_testing_aot_hybrid=default])
AC_ARG_WITH(testing_aot_full,    [  --with-testing_aot_full=yes,no      If you want to build the testing_aot_full assemblies (defaults to no)],     [], [with_testing_aot_full=default])
AC_ARG_WITH(winaot,              [  --with-winaot=yes,no                If you want to build the Windows friendly AOT assemblies (defaults to no)], [], [with_winaot=default])
AC_ARG_WITH(testing_winaot_interp,    [  --with-testing_winaot_interp=yes,no      If you want to build the Windows friendly AOT + Interpreter testing assemblies (defaults to no)], [], [with_testing_winaot_interp=default])
AC_ARG_WITH(orbis,               [  --with-orbis=yes,no                 If you want to build the Orbis assemblies (defaults to no)], [], [with_orbis=default])
AC_ARG_WITH(unreal,              [  --with-unreal=yes,no                If you want to build the Unreal assemblies (defaults to no)], [], [with_unreal=default])
AC_ARG_WITH(wasm,                [  --with-wasm=yes,no                  If you want to build the WebAssembly (defaults to no)], [], [with_wasm=default])


AC_ARG_WITH(runtime-preset, [  --with-runtime-preset=net_4_x,all,aot,aot_llvm,hybridaot,hybridaot_llvm,fullaot,fullaot_llvm,winaot,winaotinterp,winaot_llvm,winaotinterp_llvm,bitcode,bitcodeinterp,unreal,fullaotinterp,fullaotinterp_llvm  Which default profile to build (defaults to net_4_x)],  [], [with_runtime_preset=net_4_x])
AC_ARG_WITH(spectre-mitigation,             [  --with-spectre-mitigation=yes,no   If you want to build the runtime with compiler flags that enable Spectre mitigation (defaults to no)], [], [with_spectre_mitigation=default])
AC_ARG_WITH(spectre-indirect-branch-choice,   [  --with-spectre-indirect-branch-choice=keep,thunk,inline,extern   Convert indirect branches to the specified kind of thunk (defaults to inline)], [], [with_spectre_indirect_branch_choice=inline])
AC_ARG_WITH(spectre-function-return-choice, [  --with-spectre-function-return-choice=keep,thunk,inline,extern   Convert function return instructions to the specified kind of thunk (defaults to inline)], [], [with_spectre_function_return_choice=inline])

AC_ARG_WITH(static_icu, [  --with-static-icu=yes|no      Integrate ICU statically into the runtime (defaults to no)],[
	if test x$with_static_icu = xyes ; then
		AC_DEFINE(STATIC_ICU,1,[Integrate ICU statically into the runtime.])
	fi
], [with_static_icu=no])

dnl
dnl Spectre compiler mitigation flag checks
dnl
if test "x$with_spectre_mitigation" = "xyes"; then
   AC_MSG_NOTICE([Compiler Spectre mitigation support checks])
   SPECTRE_CFLAGS=
   SPECTRE_INDIRECT_BRANCH_KIND=
   case "x$with_spectre_indirect_branch_choice" in
       xkeep) SPECTRE_INDIRECT_BRANCH_KIND=keep ;;
       xthunk) SPECTRE_INDIRECT_BRANCH_KIND=thunk ;;
       xinline) SPECTRE_INDIRECT_BRANCH_KIND=thunk-inline ;;
       xextern) SPECTRE_INDIRECT_BRANCH_KIND=thunk-extern ;;
       *) AC_MSG_ERROR([Invalid indirect jump thunk kind ($with_spectre_indirect_branch_choice)]) ;;
   esac

   SPECTRE_FUNCTION_RETURN_KIND=""
   case "x$with_spectre_function_return_choice" in
       xkeep) SPECTRE_FUNCTION_RETURN_KIND=keep ;;
       xthunk) SPECTRE_FUNCTION_RETURN_KIND=thunk ;;
       xinline) SPECTRE_FUNCTION_RETURN_KIND=thunk-inline ;;
       xextern) SPECTRE_FUNCTION_RETURN_KIND=thunk-extern ;;
       *) AC_MSG_ERROR([Invalid function return thunk kind ($with_spectre_function_return_choice)]) ;;
   esac

   AX_CHECK_COMPILE_FLAG(
	[ -mindirect-branch=$SPECTRE_INDIRECT_BRANCH_KIND ],
	[ SPECTRE_CFLAGS="$SPECTRE_CFLAGS -mindirect-branch=$SPECTRE_INDIRECT_BRANCH_KIND" ]
   )

   AX_CHECK_COMPILE_FLAG(
	[ -mfunction-return=$SPECTRE_FUNCTION_RETURN_KIND ],
	[ SPECTRE_CFLAGS="$SPECTRE_CFLAGS -mfunction-return=$SPECTRE_FUNCTION_RETURN_KIND" ]
   )

   if test "x$SPECTRE_CFLAGS" != "x" ; then
        CFLAGS="$CFLAGS $SPECTRE_CFLAGS"
	CXXFLAGS="$CXXFLAGS $SPECTRE_CFLAGS"
	spectre_mitigation_status="mitigation enabled"
   fi
else
	spectre_mitigation_status="no mitigation"
fi

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
with_testing_aot_full_interp_default=no
with_testing_aot_hybrid_default=no
with_testing_aot_full_default=no
with_winaot_default=no
with_testing_winaot_interp_default=no
with_orbis_default=no
with_unreal_default=no
with_wasm_default=no

with_bitcode_default=no
enable_cooperative_suspend_default=no
enable_hybrid_suspend_default=no

# For the sake of clearer error messages, these numbers should all be different from each other.
INVARIANT_AOT_OPTIONS=nimt-trampolines=2000,ntrampolines=10000,nrgctx-fetch-trampolines=256,ngsharedvt-trampolines=4400,nftnptr-arg-trampolines=4000,nrgctx-trampolines=21000

AOT_BUILD_ATTRS=$INVARIANT_AOT_OPTIONS

MONO_LLVM_PATH_OPTION=llvm-path="`pwd`/llvm/usr/bin"

if test x$cross_compiling = xyes -o x$enable_mcs_build = xno; then
   DISABLE_MCS_DOCS_default=yes
fi

if test x$with_runtime_preset = xnetcore; then
   # Keep in sync with winconfig.h netcore configuration.
   DISABLE_MCS_DOCS_default=yes
   BTLS_SUPPORTED=no
   enable_mcs_build=no
   enable_support_build=no

   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_appdomains='yes'
   mono_feature_disable_cleanup='yes'
   mono_feature_disable_assembly_remapping='yes'
   mono_feature_disable_security='yes'
   mono_feature_disable_mdb='yes'
   mono_feature_disable_com='yes'
   mono_feature_disable_gac='yes'
   mono_feature_disable_perfcounters='yes'
   mono_feature_disable_attach='yes'
   mono_feature_disable_cfgdir_config='yes'
   if test "x$enable_monodroid" = "x" -a "x$enable_monotouch" = "x"; then
     mono_feature_disable_dllmap='yes' # FIXME: the mobile products use this
   fi
   disable_mono_native=yes
   support_boehm=no
elif test x$with_runtime_preset = xnet_4_x; then
   with_profile4_x_default=yes
   TEST_PROFILE=net_4_x
elif test x$with_runtime_preset = xall; then
   with_profile4_x_default=yes
   with_monodroid_default=yes
   with_monotouch_default=yes
   with_monotouch_watch_default=yes
   with_monotouch_tv_default=yes
   with_xammac_default=yes
   with_winaot_default=yes
   with_testing_winaot_interp_default=yes
   with_orbis_default=yes
   with_unreal_default=yes
   with_wasm_default=yes
   with_testing_aot_full_interp_default=yes
   with_testing_aot_hybrid_default=yes
   with_testing_aot_full_default=yes
   TEST_PROFILE=net_4_x
elif test x$with_runtime_preset = xbitcode; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_full_default=yes
   with_bitcode_default=yes
   enable_cooperative_suspend_default=yes
   # FIXME: use TEST_PROFILE=testing_aot_full
   TEST_PROFILE=testing_aot_bitcode
   PLATFORM_TEST_HARNESS_EXCLUDES=BitcodeNotSupported
   enable_llvm_default=yes

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   # We need at least clang++-6.0 to read the LLVM IR produced by the runtime
   AOT_BUILD_FLAGS="--runtime=mobile --aot=llvmonly,clangxx=clang++-6.0,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --llvmonly"
   AOT_MODE="llvmonly"
elif test x$with_runtime_preset = xbitcodeinterp; then
   with_testing_aot_full_interp_default=yes
   with_bitcode_default=yes
   enable_cooperative_suspend_default=yes
   # FIXME: use TEST_PROFILE=testing_aot_full_interp
   TEST_PROFILE=testing_aot_bitcode_interp
   PLATFORM_TEST_HARNESS_EXCLUDES=BitcodeNotSupported
   enable_llvm_default=yes

   # mscorlib.dll aot compilation crashes
   mono_feature_disable_com='yes'

   # We need at least clang++-6.0 to read the LLVM IR produced by the runtime
   AOT_BUILD_FLAGS="--aot=llvmonly,interp,clangxx=clang++-6.0,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--llvmonly-interp"
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
   AOT_MODE="full"
elif test x$with_runtime_preset = xfullaot_llvm; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_full_default=yes
   TEST_PROFILE=testing_aot_full

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--runtime=mobile -O=gsharedvt --llvm --aot=full,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
   AOT_MODE="full"
elif test x$with_runtime_preset = xhybridaot; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_hybrid_default=yes
   TEST_PROFILE=testing_aot_hybrid 

   AOT_BUILD_FLAGS="--runtime=mobile --aot=hybrid,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --hybrid-aot"
elif test x$with_runtime_preset = xhybridaot_llvm; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_aot_hybrid_default=yes
   TEST_PROFILE=testing_aot_hybrid

   AOT_BUILD_FLAGS="--runtime=mobile --llvm --aot=hybrid,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --hybrid-aot"
elif test x$with_runtime_preset = xfullaotinterp; then
   with_testing_aot_full_interp_default=yes
   TEST_PROFILE=testing_aot_full_interp

   # mscorlib.dll aot compilation crashes
   mono_feature_disable_com='yes'

   AOT_BUILD_FLAGS="--aot=full,interp,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--full-aot-interp"
elif test x$with_runtime_preset = xfullaotinterp_llvm; then
   with_testing_aot_full_interp_default=yes
   TEST_PROFILE=testing_aot_full_interp

   # mscorlib.dll aot compilation crashes
   mono_feature_disable_com='yes'

   AOT_BUILD_FLAGS="-O=gsharedvt --llvm --aot=full,interp,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--full-aot-interp"
elif test x$with_runtime_preset = xaot; then
   with_profile4_x_default=yes

   AOT_BUILD_FLAGS="--aot=$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS=""

   DISABLE_MCS_DOCS_default=yes
elif test x$with_runtime_preset = xaot_llvm; then
   with_profile4_x_default=yes

   AOT_BUILD_FLAGS="--llvm --aot=$INVARIANT_AOT_OPTIONS"
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

   AOT_BUILD_FLAGS="--runtime=mobile -O=gsharedvt --aot=full,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
   AOT_MODE="full"
elif test x$with_runtime_preset = xwinaotinterp; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_winaot_interp_default=yes
   TEST_PROFILE=testing_winaot_interp

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--aot=full,interp,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--full-aot-interp"
elif test x$with_runtime_preset = xwinaotinterp_llvm; then
   DISABLE_MCS_DOCS_default=yes
   with_testing_winaot_interp_default=yes
   TEST_PROFILE=testing_winaot_interp

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_appdomains='yes'

   INVARIANT_AOT_OPTIONS="llvmllc=-mattr=sse4.1,$INVARIANT_AOT_OPTIONS"
   AOT_BUILD_FLAGS="-O=gsharedvt --llvm --aot=full,interp,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--full-aot-interp"
elif test x$with_runtime_preset = xwinaot_llvm; then
   DISABLE_MCS_DOCS_default=yes
   with_winaot_default=yes
   TEST_PROFILE=winaot

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   INVARIANT_AOT_OPTIONS="llvmllc=-mattr=sse4.1,$INVARIANT_AOT_OPTIONS"
   AOT_BUILD_FLAGS="--runtime=mobile -O=gsharedvt --llvm --aot=full,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
   AOT_MODE="full"
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
   AOT_MODE="full"
elif test x$with_runtime_preset = xunreal; then
   DISABLE_MCS_DOCS_default=yes
   with_unreal_default=yes
   TEST_PROFILE=unreal

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_appdomains='no'

   AOT_BUILD_FLAGS="--runtime=mobile --aot=hybrid,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --hybrid-aot"
elif test x$with_runtime_preset = xwasm; then
   DISABLE_MCS_DOCS_default=yes
   with_wasm_default=yes
   TEST_PROFILE=wasm

   mono_feature_disable_com='yes'
   mono_feature_disable_remoting='yes'
   mono_feature_disable_reflection_emit_save='yes'
   mono_feature_disable_reflection_emit='yes'
   mono_feature_disable_appdomains='yes'

   AOT_BUILD_FLAGS="--runtime=mobile --aot=full,$INVARIANT_AOT_OPTIONS"
   AOT_RUN_FLAGS="--runtime=mobile --full-aot"
else
   TEST_PROFILE=net_4_x
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
if test "x$with_testing_aot_full_interp" = "xdefault"; then
   with_testing_aot_full_interp=$with_testing_aot_full_interp_default
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
if test "x$with_testing_winaot_interp" = "xdefault"; then
   with_testing_winaot_interp=$with_testing_winaot_interp_default
fi
if test "x$with_orbis" = "xdefault"; then
   with_orbis=$with_orbis_default
fi
if test "x$with_unreal" = "xdefault"; then
   with_unreal=$with_unreal_default
fi
if test "x$with_wasm" = "xdefault"; then
   with_wasm=$with_wasm_default
fi


AM_CONDITIONAL(INSTALL_4_x, [test "x$with_profile4_x" = "xyes"])
AM_CONDITIONAL(INSTALL_MONODROID, [test "x$with_monodroid" != "xno"])
AM_CONDITIONAL(INSTALL_MONOTOUCH, [test "x$with_monotouch" != "xno"])
AM_CONDITIONAL(INSTALL_MONOTOUCH_WATCH, [test "x$with_monotouch_watch" != "xno"])
AM_CONDITIONAL(INSTALL_MONOTOUCH_TV, [test "x$with_monotouch_tv" != "xno"])
AM_CONDITIONAL(BITCODE, test "x$with_bitcode" = "xyes")
AM_CONDITIONAL(STATIC_ICU, test "x$with_static_icu" = "xyes")
AM_CONDITIONAL(INSTALL_XAMMAC, [test "x$with_xammac" != "xno"])
AM_CONDITIONAL(INSTALL_TESTING_AOT_FULL_INTERP, [test "x$with_testing_aot_full_interp" != "xno"])
AM_CONDITIONAL(INSTALL_TESTING_AOT_HYBRID, [test "x$with_testing_aot_hybrid" != "xno"])
AM_CONDITIONAL(INSTALL_TESTING_AOT_FULL, [test "x$with_testing_aot_full" != "xno"])
AM_CONDITIONAL(INSTALL_WINAOT, [test "x$with_winaot" != "xno"])
AM_CONDITIONAL(INSTALL_TESTING_WINAOT_INTERP, [test "x$with_testing_winaot_interp" != "xno"])
AM_CONDITIONAL(INSTALL_ORBIS, [test "x$with_orbis" != "xno"])
AM_CONDITIONAL(INSTALL_UNREAL, [test "x$with_unreal" != "xno"])
AM_CONDITIONAL(INSTALL_WASM, [test "x$with_wasm" != "xno"])
AM_CONDITIONAL(INSTALL_NETCORE, [test "x$with_core" != "xno"])

AM_CONDITIONAL(HYBRID_AOT_TESTS, [test "x$TEST_PROFILE" = "xtesting_aot_hybrid"] || [test "x$TEST_PROFILE" = "xunreal"])
AM_CONDITIONAL(FULL_AOT_TESTS, [test "x$TEST_PROFILE" = "xtesting_aot_full"] || [test "x$TEST_PROFILE" = "xtesting_aot_bitcode"] || [test "x$TEST_PROFILE" = "xwinaot"] || [test "x$TEST_PROFILE" = "xorbis"] || [test "x$TEST_PROFILE" = "xwasm"])
AM_CONDITIONAL(FULL_AOT_INTERP_TESTS, [test "x$TEST_PROFILE" = "xtesting_aot_full_interp"] || [test "x$TEST_PROFILE" = "xtesting_aot_bitcode_interp"] || [test "x$TEST_PROFILE" = "xtesting_winaot_interp"])
AM_CONDITIONAL(DEFAULT_TESTS, [test "x$TEST_PROFILE" = "xnet_4_x"])

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
if test -z "$INSTALL_TESTING_AOT_FULL_INTERP_TRUE"; then :
   default_profile=testing_aot_full_interp
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
if test -z "$INSTALL_TESTING_WINAOT_INTERP_TRUE"; then :
   default_profile=testing_winaot_interp
fi
if test -z "$INSTALL_ORBIS_TRUE"; then :
   default_profile=orbis
fi
if test -z "$INSTALL_UNREAL_TRUE"; then :
   default_profile=unreal
fi
if test -z "$INSTALL_WASM_TRUE"; then :
   default_profile=wasm
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

AC_ARG_ENABLE(static-gcc-libs, [  --enable-static-gcc-libs      Statically link GCC support libs (for MinGW32)])
if test "x$enable_static_gcc_libs" = "xyes"; then
	# llvm/build.mk doesn't use automake, so make a regular make variable for it.
	AC_SUBST(STATIC_GCC_LIBS,1)
fi
AM_CONDITIONAL(ENABLE_STATIC_GCC_LIBS, test "x$enable_static_gcc_libs" = "xyes")

AC_ARG_ENABLE(minimal, [  --enable-minimal=LIST      drop support for LIST subsystems.
     LIST is a comma-separated list from: aot, profiler, decimal, pinvoke, debug, appdomains, verifier, dllmap, reflection_emit, reflection_emit_save, large_code, logging, com, ssa, generics, attach, jit, interpreter, simd, soft_debug, perfcounters, normalization, desktop_loader, shared_perfcounters, remoting, security, lldb, mdb, assert_messages, config, cfgdir_config, cleanup, sgen_marksweep_conc, sgen_split_nursery, sgen_gc_bridge, sgen_toggleref, sgen_debug_helpers, sgen_binary_protocol, sockets, gac, threads, processes, eventpipe.],
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
	AC_DEFINE(DISABLE_JIT, 1, [Disable the JIT, only full-aot mode or interpreter will be supported by the runtime.])
	AC_MSG_NOTICE([Disabled the JIT engine, only full AOT or interpreter will be supported])
fi

AM_CONDITIONAL(DISABLE_JIT, test x$mono_feature_disable_jit = xyes)

if test "x$mono_feature_disable_interpreter" = "xyes"; then
	AC_DEFINE(DISABLE_INTERPRETER, 1, [Disable the interpreter.])
	AC_MSG_NOTICE([Disabled the interpreter])
fi

AM_CONDITIONAL(DISABLE_INTERPRETER, test x$mono_feature_disable_interpreter = xyes)

if test "x$mono_feature_disable_interpreter" != "xyes" -o "x$mono_feature_disable_jit" != "xyes"; then
	AC_DEFINE(ENABLE_ILGEN, 1, [Some VES is available at runtime])
fi

AM_CONDITIONAL(ENABLE_ILGEN, test x$mono_feature_disable_interpreter != xyes -o x$mono_feature_disable_jit != xyes)

if test "x$mono_feature_disable_simd" = "xyes"; then
	AC_DEFINE(DISABLE_SIMD, 1, [Disable SIMD intrinsics related optimizations.])
	AC_MSG_NOTICE([Disabled SIMD support])
fi

if test "x$mono_feature_disable_soft_debug" = "xyes"; then
	AC_DEFINE(DISABLE_DEBUGGER_AGENT, 1, [Disable Soft Debugger Agent.])
	AC_MSG_NOTICE([Disabled Soft Debugger.])
fi

AM_CONDITIONAL(DISABLE_DEBUGGER_AGENT, test x$mono_feature_disable_debugger_agent = xyes)

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

if test "x$mono_feature_disable_assert_messages" = "xyes"; then
	AC_DEFINE(DISABLE_ASSERT_MESSAGES, 1, [Disable assertion messages.])
	AC_MSG_NOTICE([Disabled assertion messages.])
fi

if test "x$mono_feature_disable_cfgdir_config" = "xyes"; then
	AC_DEFINE(DISABLE_CFGDIR_CONFIG, 1, [Disable config directories.])
	AC_MSG_NOTICE([Disabled config directories.])
fi

if test "x$mono_feature_disable_config" = "xyes"; then
	AC_DEFINE(DISABLE_CONFIG, 1, [Disable .config file support.])
	AC_MSG_NOTICE([Disabled .config file support.])
fi

if test "x$mono_feature_disable_cleanup" = "xyes"; then
	AC_DEFINE(DISABLE_CLEANUP, 1, [Disable runtime cleanup.])
	AC_MSG_NOTICE([Disabled runtime cleanup.])
fi

if test "x$mono_feature_disable_sgen_marksweep_conc" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_MAJOR_MARKSWEEP_CONC, 1, [Disable concurrent gc support in SGEN.])
	AC_MSG_NOTICE([Disabled concurrent gc support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_split_nursery" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_SPLIT_NURSERY, 1, [Disable minor=split support in SGEN.])
	AC_MSG_NOTICE([Disabled minor=split support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_gc_bridge" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_GC_BRIDGE, 1, [Disable gc bridge support in SGEN.])
	AC_MSG_NOTICE([Disabled gc bridge support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_toggleref" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_TOGGLEREF, 1, [Disable toggleref support in SGEN.])
	AC_MSG_NOTICE([Disabled toggleref support in SGEN.])
fi

if test "x$mono_feature_disable_sgen_debug_helpers" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_DEBUG_HELPERS, 1, [Disable debug helpers in SGEN.])
	AC_MSG_NOTICE([Disabled debug helpers in SGEN.])
fi

if test "x$mono_feature_disable_sgen_binary_protocol" = "xyes"; then
	AC_DEFINE(DISABLE_SGEN_BINARY_PROTOCOL, 1, [Disable binary protocol logging in SGEN.])
	AC_MSG_NOTICE([Disabled binary protocol logging in SGEN.])
fi

if test "x$mono_feature_disable_sockets" = "xyes"; then
	AC_DEFINE(DISABLE_SOCKETS, 1, [Disable sockets])
	AC_MSG_NOTICE([Disabled sockets])
fi

if test "x$mono_feature_disable_gac" = "xyes"; then
	AC_DEFINE(DISABLE_GAC, 1, [Disable GAC])
	AC_MSG_NOTICE([Disabled GAC support])
fi

if test "x$mono_feature_disable_dllmap" = "xyes"; then
    AC_DEFINE(DISABLE_DLLMAP,1,[Disables use of DllMaps in MonoVM])
    AC_MSG_NOTICE([Disabled DllMap in the loader.])
fi

if test "x$mono_feature_disable_threads" = "xyes"; then
	AC_DEFINE(DISABLE_THREADS, 1, [Disable Threads])
	AC_MSG_NOTICE([Disabled threading support])
fi

if test "x$mono_feature_disable_processes" = "xyes"; then
	AC_DEFINE(DISABLE_PROCESSES, 1, [Disable process support])
	AC_MSG_NOTICE([Disabled process support])
fi

if test "x$mono_feature_disable_eventpipe" = "xyes"; then
	AC_DEFINE(DISABLE_EVENTPIPE, 1, [Disable EventPipe support])
	AC_MSG_NOTICE([Disabled EventPipe support])
	enable_perftracing=no
fi

if test x$enable_perftracing = x -a x$with_core = xonly; then
	if test -f $srcdir/mono/eventpipe/ep.h; then
		enable_perftracing=yes
	fi
fi
if test x$enable_perftracing = xyes; then
	AC_DEFINE(ENABLE_PERFTRACING,1,[Enables support for eventpipe library])
fi
AM_CONDITIONAL(ENABLE_PERFTRACING, test x$enable_perftracing = xyes)

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

AC_ARG_WITH(libgc,   [  --with-libgc=included,none  Controls the Boehm GC config, default=included],[libgc=$with_libgc],[libgc=included])

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
	case "x$libgc" in
		xincluded)
			LIBGC_CPPFLAGS='-I$(top_srcdir)/external/bdwgc/include -I$(top_srcdir)/external/bdwgc/libatomic_ops/src'
			LIBGC_LIBS='$(top_builddir)/external/bdwgc/libgc.la'
			LIBGC_STATIC_LIBS='$(top_builddir)/external/bdwgc/libgc-static.la'

			BOEHM_DEFINES="-DHAVE_BOEHM_GC -DGC_THREADS"

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
			AC_MSG_ERROR([Invalid argument $libgc to --with-libgc.])
			;;
	esac

	AC_ARG_WITH(large-heap, [  --with-large-heap=yes,no       Enable support for GC heaps larger than 3GB (defaults to no)], [large_heap=$withval], [large_heap=no])
	if test "x$large_heap" = "xyes"; then
	   CPPFLAGS="$CPPFLAGS -DLARGE_CONFIG"
	fi

	AC_SUBST(LIBGC_CPPFLAGS)
	AC_SUBST(LIBGC_LIBS)
	AC_SUBST(LIBGC_STATIC_LIBS)
	AC_SUBST(BOEHM_DEFINES)

fi
AM_CONDITIONAL(SUPPORT_NULLGC, test "x$libgc" = "xnone")

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

# `target_ios=yes` does not detect watch devices and fails when cross-compiling
AC_MONO_APPLE_TARGET(TARGET_OS_IPHONE, [mono_native_platform_ios=yes])

if test x$host_win32 = xno; then
	dnl *************************************
	dnl *** Checks for time capabilities ***
	dnl *************************************

	AC_MSG_CHECKING(for CLOCK_MONOTONIC)
	AC_TRY_COMPILE([#include <time.h>], [
		const int foo = CLOCK_MONOTONIC;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_CLOCK_MONOTONIC, 1, [CLOCK_MONOTONIC])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for CLOCK_MONOTONIC_COARSE)
	AC_TRY_COMPILE([#include <time.h>], [
		const int foo = CLOCK_MONOTONIC_COARSE;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_CLOCK_MONOTONIC_COARSE, 1, [CLOCK_MONOTONIC_COARSE])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for CLOCK_REALTIME)
	AC_TRY_COMPILE([#include <time.h>], [
		const int foo = CLOCK_REALTIME;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_CLOCK_REALTIME, 1, [CLOCK_REALTIME])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_FUNC(mach_absolute_time, [AC_DEFINE(HAVE_MACH_ABSOLUTE_TIME, 1, [mach_absolute_time])])
	AC_CHECK_FUNC(gethrtime, [AC_DEFINE(HAVE_GETHRTIME, 1, [gethrtime])])
	AC_CHECK_FUNC(read_real_time, [AC_DEFINE(HAVE_READ_REAL_TIME, 1, [read_real_time])])
	AC_CHECK_FUNC(mach_timebase_info, [AC_DEFINE(HAVE_MACH_TIMEBASE_INFO, 1, [mach_timebase_info])])
	AC_CHECK_FUNC(futimes,            [AC_DEFINE(HAVE_FUTIMES, 1, [futimes])])
	AC_CHECK_FUNC(futimens,           [AC_DEFINE(HAVE_FUTIMENS, 1, [futimens])])

	AC_CHECK_TYPES([clockid_t], [AC_DEFINE(HAVE_CLOCKID_T)], [],
		[#include <sys/types.h>])

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
		dnl from glib's configure.ac
		if test "x$cross_compiling" = "xyes"; then
		   AC_MSG_RESULT(cross compiling, assuming no)
		   mono_cv_uscore=no
		else
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
		fi
		if test "x$mono_cv_uscore" = "xyes"; then
			AC_DEFINE(MONO_DL_NEED_USCORE, 1, [Does dlsym require leading underscore.])
		fi
		AC_CHECK_FUNC(dlerror)
	fi

	dnl ******************************************************************
	dnl *** Checks for the IKVM JNI interface library                  ***
	dnl ******************************************************************
	AC_ARG_WITH(ikvm-native, [  --with-ikvm-native=yes,no      build the IKVM JNI interface library (defaults to yes)],[with_ikvm_native=$withval],[with_ikvm_native=$ikvm_native])

	if test x$with_ikvm_native = xyes; then
		jdk_headers_found="IKVM Native"
	fi

	AC_CHECK_HEADERS(execinfo.h)

	AC_CHECK_HEADERS(sys/auxv.h sys/resource.h)

	AC_CHECK_FUNCS(getgrgid_r)
	AC_CHECK_FUNCS(getgrnam_r)
	AC_CHECK_FUNCS(getresuid)
	AC_CHECK_FUNCS(setresuid)
	AC_CHECK_FUNCS(kqueue)
	# IBM provides a compatibility library for offering this function.
	# BSDs and others, have execinfo in base or packages.
	AC_SEARCH_LIBS(backtrace_symbols, execinfo util)
	# Two-step so it sets it in config.h
	AC_CHECK_FUNCS(backtrace_symbols)
	AC_CHECK_FUNCS(mkstemp)
	AC_CHECK_FUNCS(mmap)
	AC_CHECK_FUNCS(getrusage)
	AC_CHECK_FUNCS(getpriority)
	AC_CHECK_FUNCS(setpriority)
	AC_CHECK_FUNCS(dl_iterate_phdr)
	AC_CHECK_FUNCS(dladdr)
	AC_CHECK_FUNCS(sysconf)
	AC_CHECK_FUNCS(getrlimit)
	AC_CHECK_FUNCS(prctl)
	AC_CHECK_FUNCS(arc4random)
	AC_CHECK_FUNCS(nl_langinfo)

	AC_CHECK_FUNCS(sched_getaffinity)
	AC_CHECK_FUNCS(sched_setaffinity)
	AC_CHECK_FUNCS(sched_getcpu)

	if test x$platform_android != xyes; then
		AC_CHECK_FUNCS(getpwnam_r)
		AC_CHECK_FUNCS(getpwuid_r)
	fi

	AC_CHECK_FUNCS(readlink)
	AC_CHECK_FUNCS(chmod)
	AC_CHECK_FUNCS(lstat)
	AC_CHECK_FUNCS(getdtablesize)

	AC_CHECK_FUNCS(ftruncate)
	AC_CHECK_FUNCS(msync)

	AC_CHECK_FUNCS(gethostname getpeername)
	AC_CHECK_FUNCS(utime utimes)

	AC_CHECK_FUNCS(openlog closelog)
	AC_CHECK_FUNCS(atexit)
	AC_CHECK_FUNCS(popen)

	AC_FUNC_STRERROR_R()

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
	dnl *** Check for getprotobyname_r ***
	dnl **********************************
	AC_MSG_CHECKING(for getprotobyname_r)
		AC_TRY_LINK([
		#include <stdio.h>
		#include <netdb.h>
	], [
		getprotobyname_r(NULL, NULL, NULL, 0, NULL);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETPROTOBYNAME_R, 1, [Have getprotobyname_r])
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
		*solaris*)
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
	AC_CHECK_FUNCS(pthread_getattr_np pthread_attr_get_np pthread_getname_np pthread_setname_np pthread_cond_timedwait_relative_np)
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
	dnl check that pthread_mutexattr_setprotocol is declared
	AC_CHECK_DECLS([pthread_mutexattr_setprotocol], [], [], [[#include <pthread.h>]])
	AC_CHECK_FUNCS(mincore mlock munlock)

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
			
			#include <unistd.h>
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
		AC_MSG_RESULT(cross compiling, assuming no)
		with_sigaltstack=no
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
				stack_t sas;
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
	dnl *** Checks for sys_signame ***
	dnl ********************************
	AC_MSG_CHECKING(for sys_signame)
		AC_TRY_LINK([
		#include <signal.h>
	], [
		const char *signame = sys_signame[0];
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SYSSIGNAME, 1, [Have sys_signame])
	], [
		AC_MSG_RESULT(no)
	])


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

	dnl ****************************************************************
	dnl *** Checks for working poll() (macosx defines it but doesn't ***
	dnl *** have it in the library (duh))                            ***
	dnl ****************************************************************
	AC_CHECK_FUNCS(poll)

	dnl **********************************
	dnl *** epoll			   ***
	dnl **********************************
	AC_CHECK_HEADERS(sys/epoll.h)
	haveepoll=no
	AC_CHECK_FUNCS(epoll_ctl, [haveepoll=yes], )
	if test "x$haveepoll" = "xyes" -a "x$ac_cv_header_sys_epoll_h" = "xyes"; then
		AC_DEFINE(HAVE_EPOLL, 1, [epoll supported])
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
		#ifdef HAVE_NET_IF_H
		#include <net/if.h>
		#endif
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
	dnl *** Check for access ***
	dnl **********************************
	AC_MSG_CHECKING(for access)
		AC_TRY_LINK([
		#include <unistd.h>
	], [
		access(NULL,0);
	], [
		# Yes, we have it...
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_ACCESS, 1, [Have access])
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
	AC_CHECK_FUNCS(vsnprintf)
	AC_CHECK_FUNCS(sendfile)
	AC_CHECK_FUNCS(gethostid sethostid)
	AC_CHECK_FUNCS(sethostname)
	AC_CHECK_FUNCS(statfs)
	AC_CHECK_FUNCS(fstatfs)
	AC_CHECK_FUNCS(statvfs)
	AC_CHECK_FUNCS(fstatvfs)
	AC_CHECK_FUNCS(stime)
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
	AC_CHECK_FUNCS(waitpid)
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
		[struct stat.st_atim, struct stat.st_mtim, struct stat.st_atimespec, struct stat.st_ctim],,, 
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

	dnl *********************************
	dnl *** Checks for random         ***
	dnl *********************************
	if test x$host_darwin = xno; then
		AC_CHECK_HEADERS(sys/random.h)
		AC_CHECK_FUNCS(getrandom getentropy)
	fi

	dnl *********************************
	dnl *** Checks for Mono.Native    ***
	dnl *********************************

	# Translated from CMake in external/corefx/src/Native/Unix/configure.cmake, keep in sync!
	# Note: check_c_source_compiles in CMake is AC_TRY_LINK in autoconf

	AC_MSG_CHECKING(for linux/in.h)
	AC_TRY_COMPILE([
		#include <sys/socket.h>
		#include <linux/in.h>
	], [
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_LINUX_IN_H, 1, [linux/in.h])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_HEADERS([linux/if_packet.h])

	AC_MSG_CHECKING(for struct in_pktinfo)
	AC_TRY_COMPILE([
		#ifdef HAVE_LINUX_IN_H
		#include <sys/socket.h>
		#include <linux/in.h>
		#else
		#include <netinet/in.h>
		#endif
	], [
		struct in_pktinfo pktinfo;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IN_PKTINFO, 1, [struct in_pktinfo])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for struct ip_mreqn)
	AC_TRY_COMPILE([
		#if HAVE_LINUX_IN_H
		#include <sys/socket.h>
		#include <linux/in.h>
		#else
		#include <netinet/in.h>
		#endif
	], [
		struct ip_mreqn mreqn;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_IP_MREQN, 1, struct ip_mreqn)
	], [
		AC_MSG_RESULT(no)
	])

	if test x$host_darwin = xno; then
		AC_CHECK_TYPES([struct flock64], [AC_DEFINE(HAVE_FLOCK64, 1, struct flock64)], , [#include <fcntl.h>])
	fi

	AC_CHECK_DECL(O_CLOEXEC,         [AC_DEFINE(HAVE_O_CLOEXEC, 1, [O_CLOEXEC])], [], [[#include <fcntl.h>]])
	AC_CHECK_DECL(F_DUPFD_CLOEXEC,   [AC_DEFINE(HAVE_F_DUPFD_CLOEXEC, 1, [F_DUPFD_CLOEXEC])], [], [[#include <fcntl.h>]])

	# AC_CHECK_FUNC(getifaddrs,           [AC_DEFINE(HAVE_GETIFADDRS, 1, [getifaddrs])]) # already done above
	AC_CHECK_FUNC(Qp2getifaddrs,     [AC_DEFINE(HAVE_QP2GETIFADDRS, 1, [Qp2getifaddrs])])

	AC_CHECK_FUNC(lseek64,           [AC_DEFINE(HAVE_LSEEK64, 1, [lseek64])])
	AC_CHECK_FUNC(mmap64,            [AC_DEFINE(HAVE_MMAP64, 1, [mmap64])])
	AC_CHECK_FUNC(ftruncate64,       [AC_DEFINE(HAVE_FTRUNCATE64, 1, [ftruncate64])])
	AC_CHECK_FUNC(posix_fadvise64,   [AC_DEFINE(HAVE_POSIX_FADVISE64, 1, [posix_fadvise64])])

	if test "x$mono_native_platform_ios" = "xno"; then
		# On iOS, `stat64()` is deprecated and there is no `struct stat64` and `stat()`
		# is either 32-bit or 64-bit based on the device / simulator that you're running on.
		AC_CHECK_FUNC(stat64,            [AC_DEFINE(HAVE_STAT64, 1, [stat64])])
	fi

	AC_CHECK_DECL(pipe2,             [AC_DEFINE(HAVE_PIPE2, 1, [pipe2])])
	AC_CHECK_FUNC(getmntinfo,        [AC_DEFINE(HAVE_GETMNTINFO, 1, [getmntinfo])], [], [[#include <unistd.h>]])
	AC_CHECK_FUNC(strcpy_s,          [AC_DEFINE(HAVE_STRCPY_S, 1, [strcpy_s])])
	AC_CHECK_FUNC(strlcpy,           [AC_DEFINE(HAVE_STRLCPY, 1, [strlcpy])])
	AC_CHECK_FUNC(posix_fadvise,     [AC_DEFINE(HAVE_POSIX_ADVISE, 1, [posix_fadvise])]) # the define is called HAVE_POSIX_ADVISE in corefx, not a typo
	AC_CHECK_FUNC(ioctl,             [AC_DEFINE(HAVE_IOCTL, 1, [ioctl])])
	AC_CHECK_FUNC(sched_getaffinity, [AC_DEFINE(HAVE_SCHED_GETAFFINITY, 1, [sched_getaffinity])])
	AC_CHECK_FUNC(sched_setaffinity, [AC_DEFINE(HAVE_SCHED_SETAFFINITY, 1, [sched_setaffinity])])

	if test "x$platform_android" != "xyes"; then
		AC_CHECK_FUNC(arc4random_buf,    [AC_DEFINE(HAVE_ARC4RANDOM_BUF, 1, [arc4random_buf])])
	fi

	AC_CHECK_DECL(TIOCGWINSZ,        [AC_DEFINE(HAVE_TIOCGWINSZ, 1, [TIOCGWINSZ])], [], [[#include <sys/ioctl.h>]])
	AC_CHECK_FUNC(tcgetattr,         [AC_DEFINE(HAVE_TCGETATTR, 1, [tcgetattr])])
	AC_CHECK_FUNC(tcsetattr,         [AC_DEFINE(HAVE_TCSETATTR, 1, [tcsetattr])])
	AC_CHECK_DECL(ECHO,              [AC_DEFINE(HAVE_ECHO, 1, [ECHO])], [], [[#include <termios.h>]])
	AC_CHECK_DECL(ICANON,            [AC_DEFINE(HAVE_ICANON, 1, [ICANON])], [], [[#include <termios.h>]])
	AC_CHECK_DECL(TCSANOW,           [AC_DEFINE(HAVE_TCSANOW, 1, [TCSANOW])], [], [[#include <termios.h>]])

	AC_CHECK_DECL(lchflags,          [AC_DEFINE(HAVE_LCHFLAGS, 1, [lchflags])], [], 
		[[#include <sys/types.h> 
		#include <sys/stat.h>]])
	AC_CHECK_MEMBER(struct stat.st_flags,
		[AC_DEFINE(HAVE_STAT_FLAGS, 1, [struct stat.st_flags])],
		[],
		[#include <sys/types.h>
		#include <sys/stat.h>])

	AC_CHECK_MEMBER(struct stat.st_birthtimespec,
		[AC_DEFINE(HAVE_STAT_BIRTHTIME, 1, [struct stat.st_birthtimespec])],
		[],
		[#include <sys/types.h>
		#include <sys/stat.h>])
	AC_CHECK_MEMBER(struct stat.st_atimespec,
		[AC_DEFINE(HAVE_STAT_TIMESPEC, 1, [struct stat.st_atimespec])],
		[],
		[#include <sys/types.h>
		#include <sys/stat.h>])
	AC_CHECK_MEMBER(struct stat.st_atim,
		[AC_DEFINE(HAVE_STAT_TIM, 1, [struct stat.st_atim])],
		[],
		[#include <sys/types.h>
		#include <sys/stat.h>])
	AC_CHECK_MEMBER(struct stat.st_atimensec,
		[AC_DEFINE(HAVE_STAT_NSEC, 1, [struct stat.st_atimensec])],
		[],
		[#include <sys/types.h>
		#include <sys/stat.h>])
	AC_CHECK_MEMBER(struct dirent.d_namlen,       [AC_DEFINE(HAVE_DIRENT_NAME_LEN, 1, [struct dirent.d_namlen])], [], [#include <dirent.h>])
	AC_CHECK_MEMBER(struct statfs.f_fstypename,   [AC_DEFINE(HAVE_STATFS_FSTYPENAME, 1, [struct statfs.f_fstypename])], [], [#include <sys/mount.h>])
	AC_CHECK_MEMBER(struct statvfs.f_fstypename,  [AC_DEFINE(HAVE_STATVFS_FSTYPENAME, 1, [struct statvfs.f_fstypename])], [], [#include <sys/mount.h>])

	AC_MSG_CHECKING(for struct statfs)
	AC_TRY_COMPILE([
		#if defined(HAVE_STATFS_FSTYPENAME) || defined(HAVE_STATVFS_FSTYPENAME)
		#include <sys/mount.h>
		#else
		#include <sys/statfs.h>
		#endif
	], [
		struct statfs;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_STATFS, 1, [struct statfs])
	], [
		AC_MSG_RESULT(no)
	])

	if test "x$ac_cv_func_strerror_r_char_p" = "xyes" ; then
		AC_DEFINE(HAVE_GNU_STRERROR_R, 1, [char* strerror(int errnum, char *buf, size_t buflen)])
	fi

	AC_MSG_CHECKING(for readdir_r)
	AC_TRY_LINK([
		#include <dirent.h>
	], [
		DIR* dir;
		struct dirent* entry;
		struct dirent* result;
		readdir_r(dir, entry, &result);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_READDIR_R, 1, [readdir_r])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for kevent with void *data)
	AC_TRY_LINK([
		#include <sys/types.h>
		#include <sys/event.h>
	], [
		struct kevent event;
		void* data;
		EV_SET(&event, 0, EVFILT_READ, 0, 0, 0, data);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(KEVENT_HAS_VOID_UDATA, 1, [kevent with void *data])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_MEMBER(struct fd_set.fds_bits,   [AC_DEFINE(HAVE_FDS_BITS, 1, [struct fd_set.fds_bits])], [], [[#include <sys/select.h>]])
	AC_CHECK_MEMBER(struct fd_set.__fds_bits, [AC_DEFINE(HAVE_PRIVATE_FDS_BITS, 1, [struct fd_set.__fds_bits])], [], [[#include <sys/select.h>]])

	AC_MSG_CHECKING(for sendfile with 4 arguments)
	AC_TRY_LINK([
		#include <sys/sendfile.h>
	], [
		#if defined(TARGET_ANDROID)
		#if !defined(__ANDROID_API__)
		#error No definition for __ANDROID_API__ even though we're targeting TARGET_ANDROID
		#elif __ANDROID_API__ < 21
		#error sendfile is not supported on this Android API level
		#endif
		#endif

		int result = sendfile(0, 0, 0, 0);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SENDFILE_4, 1, [sendfile with 4 arguments])
	], [
		AC_MSG_RESULT(no)
	])

	ORIG_CFLAGS="$CFLAGS"
	CFLAGS="$CFLAGS -Werror-implicit-function-declaration"

	AC_MSG_CHECKING(for sendfile with 6 arguments)
	AC_TRY_LINK([
		#include <stdlib.h>
		#include <sys/types.h>
		#include <sys/socket.h>
		#include <sys/uio.h>
	], [
		int result = sendfile(0, 0, 0, NULL, NULL, 0);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_SENDFILE_6, 1, [sendfile with 6 arguments])
	], [
		AC_MSG_RESULT(no)
	])

	CFLAGS="$ORIG_CFLAGS"

	AC_CHECK_FUNC(fcopyfile,     [AC_DEFINE(HAVE_FCOPYFILE, 1, [fcopyfile])])
	AC_CHECK_FUNC(epoll_create1, [AC_DEFINE(HAVE_EPOLL, 1, [epoll_create1])])
	AC_CHECK_FUNC(accept4,       [AC_DEFINE(HAVE_ACCEPT4, 1, [accept4])])
	AC_CHECK_FUNC(kqueue,        [AC_DEFINE(HAVE_KQUEUE, 1, [kqueue])])

	ORIG_CFLAGS="$CFLAGS"
	CFLAGS="$CFLAGS -Werror=sign-conversion"

	AC_MSG_CHECKING(for getnameinfo with signed flags)
	AC_TRY_LINK([
		#include <sys/types.h>
		#include <netdb.h>
	], [
		const struct sockaddr *addr;
		socklen_t addrlen;
		char *host;
		socklen_t hostlen;
		char *serv;
		socklen_t servlen;
		int flags;
		int result = getnameinfo(addr, addrlen, host, hostlen, serv, servlen, flags);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETNAMEINFO_SIGNED_FLAGS, 1, [getnameinfo with signed flags])
	], [
		AC_MSG_RESULT(no)
	])

	CFLAGS="$ORIG_CFLAGS"

	if test "x$host_linux" = "xyes"; then
		AC_DEFINE(HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO, 1, [HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO])
	else
		AC_DEFINE(HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO, 0, [HAVE_SUPPORT_FOR_DUAL_MODE_IPV4_PACKET_INFO])
	fi

	# HAVE_CLOCK_MONOTONIC check already done above
	# HAVE_CLOCK_REALTIME check already done above
	# HAVE_MACH_ABSOLUTE_TIME check already done above
	# HAVE_MACH_TIMEBASE_INFO check already done above
	# HAVE_FUTIMES check already done above
	# HAVE_FUTIMENS check already done above

	ORIG_CFLAGS="$CFLAGS"
	CFLAGS="$CFLAGS -Werror=sign-conversion"

	AC_MSG_CHECKING(for bind with unsigned addrlen)
	AC_TRY_LINK([
		#include <sys/socket.h>
	], [
		int fd;
		struct sockaddr* addr;
		socklen_t addrLen;
		bind(fd, addr, addrLen);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(BIND_ADDRLEN_UNSIGNED, 1, [bind with unsigned addrlen])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for struct ipv6_mreq with unsigned ipv6mr_interface)
	AC_TRY_LINK([
		#include <netinet/in.h>
		#include <netinet/tcp.h>
	], [
		struct ipv6_mreq opt;
		unsigned int index = 0;
		opt.ipv6mr_interface = index;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(IPV6MR_INTERFACE_UNSIGNED, 1, [struct ipv6_mreq with unsigned ipv6mr_interface])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for inotify_rm_watch with unsigned wd)
	AC_TRY_LINK([
		#include <sys/inotify.h>
	], [
		intptr_t fd;
		uint32_t wd;
		int result = inotify_rm_watch(fd, wd);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(INOTIFY_RM_WATCH_WD_UNSIGNED, 1, [inotify_rm_watch with unsigned wd])
	], [
		AC_MSG_RESULT(no)
	])

	case "$host" in
		*-*-*freebsd*)
			dnl *****************************
			dnl *** Checks for libinotify ***
			dnl *****************************
			AC_CHECK_LIB(inotify, inotify_init, LIBS="$LIBS -linotify")
			if test "x$ac_cv_lib_inotify_inotify_init" = "xyes" ; then
				AC_DEFINE(HAVE_LIBINOTIFY, 1, [FreeBSD libinotify kqueue shim])
				dnl Needs to be done this way to avoid collision with various 
				dnl ports including glib and llvm*
				METADATA_CFLAGS="-I/usr/local/include"
				AC_SUBST(METADATA_CFLAGS)
			fi
			dnl Workaround due to inotify_rm_watch check failing without -I
			AC_MSG_CHECKING(for inotify_rm_watch with unsigned wd in libinotify)
			AC_TRY_LINK([
				#include </usr/local/include/sys/inotify.h>
			], [
				intptr_t fd;
				uint32_t wd;
				int result = inotify_rm_watch(fd, wd);
			],[
			   AC_MSG_RESULT(yes)
			   AC_DEFINE(INOTIFY_RM_WATCH_WD_UNSIGNED, 1, [inotify_rm_watch with unsigned wd])
			], [
				AC_MSG_RESULT(no)
			])
	esac

	CFLAGS="$ORIG_CFLAGS"

	AC_MSG_CHECKING(for shm_open that works well enough with mmap)
	if test "x$ac_cv_func_shm_open" = "xno" -o "x$ac_cv_func_shm_open_working_with_mmap" = "xno" ; then
		AC_MSG_RESULT(no)
	elif test "x$cross_compiling" = "xyes"; then
		AC_MSG_RESULT(cross compiling, assuming yes)
		AC_DEFINE(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP, 1, [shm_open that works well enough with mmap])
	else
		AC_TRY_RUN([
			#include <sys/mman.h>
			#include <fcntl.h>
			#include <unistd.h>

			int main ()
			{
			#ifdef __PASE__
				/* IBM i doesn't implement this and returns SIGILL */
				return -1;
			#endif
				int fd = shm_open("/mono_configure_shm_open", O_CREAT | O_RDWR, 0777);
				if (fd == -1)
					return -1;

				shm_unlink("/mono_configure_shm_open");

				// NOTE: PROT_EXEC and MAP_PRIVATE don't work well with shm_open
				//       on at least the current version of Mac OS X

				if (mmap(NULL, 1, PROT_EXEC, MAP_PRIVATE, fd, 0) == MAP_FAILED)
					return -1;

				return 0;
			}
		],[
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_SHM_OPEN_THAT_WORKS_WELL_ENOUGH_WITH_MMAP, 1, [shm_open that works well enough with mmap])
		], [
			AC_MSG_RESULT(no)
		])
	fi

	AC_MSG_CHECKING(for getpriority with int who)
	AC_TRY_LINK([
		#include <sys/resource.h>
	], [
		int which;
		int who;
		int result = getpriority(which, who);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(PRIORITY_REQUIRES_INT_WHO, 1, [getpriority with int who])
	], [
		AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for kevent with int parameters)
	AC_TRY_LINK([
		#include <sys/types.h>
		#include <sys/event.h>
	], [
		int kg;
		const struct kevent* chagelist;
		int nchanges;
		struct kevent* eventlist;
		int nevents;
		const struct timespec* timeout;
		int result = kevent(kg, chagelist, nchanges, eventlist, nevents, timeout);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(KEVENT_REQUIRES_INT_PARAMS, 1, [kevent with int parameters])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_FUNCS(mkstemps)
	# AC_CHECK_FUNCS(mkstemp) # already done above

	if test "x$ac_cv_func_mkstemps" != "xyes" -a "x$ac_cv_func_mkstemp" != "xyes"; then
		AC_MSG_ERROR([Cannot find mkstemps or mkstemp on this platform])
	fi

	AC_MSG_CHECKING(for tcp/var.h)
	AC_TRY_LINK([
		#include <sys/types.h>
		#include <sys/socketvar.h>
		#include <netinet/ip.h>
		#include <netinet/tcp.h>
		#include <netinet/tcp_var.h>
	], [
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_TCP_VAR_H, 1, [tcp/var.h])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_HEADERS([sys/cdefs.h])

	AC_MSG_CHECKING(for TCPSTATE enum in netinet/tcp.h)
	AC_TRY_LINK([
		#ifdef HAVE_SYS_CDEFS_H
		#include <sys/cdefs.h>
		#endif
		#include <netinet/tcp.h>
	], [
		int result = TCP_ESTABLISHED;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_TCP_H_TCPSTATE_ENUM, 1, [TCPSTATE enum in netinet/tcp.h])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_DECL(TCPS_ESTABLISHED, [AC_DEFINE(HAVE_TCP_FSM_H, 1, [HAVE_TCP_FSM_H])], [], [[#include <netinet/tcp_fsm.h>]])

	AC_MSG_CHECKING(for struct rt_msghdr)
	AC_TRY_COMPILE([
		#include <sys/types.h>
		#include <net/route.h>
	], [
		struct rt_msghdr;
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_RT_MSGHDR, 1, [struct rt_msghdr])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_HEADERS([sys/sysctl.h])
	AC_CHECK_HEADERS([linux/rtnetlink.h])

	AC_CHECK_FUNC(getpeereid,    [AC_DEFINE(HAVE_GETPEEREID, 1, [getpeereid])])
	#AC_CHECK_FUNC(getdomainname, [AC_DEFINE(HAVE_GETDOMAINNAME, 1, [getdomainname])]) # already done above
	AC_CHECK_FUNC(uname,         [AC_DEFINE(HAVE_UNAME, 1, [uname])])

	ORIG_CFLAGS="$CFLAGS"
	CFLAGS="$CFLAGS -Werror=shorten-64-to-32"

	AC_MSG_CHECKING(for getdomainname with size_t namelen)
	AC_TRY_LINK([
		#include <unistd.h>
	], [
		size_t namelen = 20;
		char name[20];
		int result = getdomainname(name, namelen);
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_GETDOMAINNAME_SIZET, 1, [getdomainname with size_t namelen])
	], [
		AC_MSG_RESULT(no)
	])

	CFLAGS="$ORIG_CFLAGS"

	AC_CHECK_FUNC(inotify_init, [AC_DEFINE(HAVE_INOTIFY_INIT, 1, [inotify_init])])
	AC_CHECK_FUNC(inotify_add_watch, [AC_DEFINE(HAVE_INOTIFY_ADD_WATCH, 1, [inotify_add_watch])])
	AC_CHECK_FUNC(inotify_rm_watch, [AC_DEFINE(HAVE_INOTIFY_RM_WATCH, 1, [inotify_rm_watch])])

	if test "x$ac_cv_func_inotify_init" = "xyes" -a "x$ac_cv_func_inotify_add_watch" = "xyes" -a "x$ac_cv_func_inotify_rm_watch" = "xyes"; then
		AC_DEFINE(HAVE_INOTIFY, 1, [HAVE_INOTIFY])
	elif test "x$host_linux" = "xyes"; then
		AC_MSG_ERROR([Cannot find inotify functions on a Linux platform.])
	fi

	# HAVE_CURLM_ADDED_ALREADY check skipped because we don't use libcurl in mono
	# HAVE_CURL_HTTP_VERSION_2TLS check skipped because we don't use libcurl in mono
	# HAVE_CURLPIPE_MULTIPLEX check skipped because we don't use libcurl in mono
	# HAVE_CURL_SSLVERSION_TLSv1_012 check skipped because we don't use libcurl in mono

	enable_gss=no;
	AC_MSG_CHECKING(for GSS/GSS.h)
	AC_TRY_COMPILE([
			#include <GSS/GSS.h>
	], [
	],[
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_GSSFW_HEADERS, 1, [GSS/GSS.h])
			AC_DEFINE(HAVE_GSS_SPNEGO_MECHANISM, 1, [GSS_SPNEGO_MECHANISM])
			enable_gss=yes
			AC_DEFINE(ENABLE_GSS, 1, [ENABLE_GSS])
	], [
			AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for gssapi/gssapi_ext.h)
	AC_TRY_COMPILE([
			#include <gssapi/gssapi_ext.h>
	], [
	],[
			AC_MSG_RESULT(yes)
			enable_gss=yes
			AC_DEFINE(ENABLE_GSS, 1, [ENABLE_GSS])
	], [
			AC_MSG_RESULT(no)
	])

	AC_MSG_CHECKING(for GSS_SPNEGO_MECHANISM)
	AC_TRY_COMPILE([
			#include <gssapi/gssapi_ext.h>
			#include <gssapi/gssapi_krb5.h>
			gss_OID_set_desc gss_mech_spnego_OID_set_desc = {.count = 1, .elements = GSS_SPNEGO_MECHANISM};
	], [
	],[
			AC_MSG_RESULT(yes)
			AC_DEFINE(HAVE_GSS_SPNEGO_MECHANISM, 1, [GSS_SPNEGO_MECHANISM])
	], [
			AC_MSG_RESULT(no)
	])

	AM_CONDITIONAL(ENABLE_GSS, test x$enable_gss = xyes)

	AC_CHECK_HEADERS([crt_externs.h])

	AC_MSG_CHECKING(for _NSGetEnviron)
	AC_TRY_LINK([
		#include <crt_externs.h>
	], [
		char **result = *(_NSGetEnviron());
	],[
		AC_MSG_RESULT(yes)
		AC_DEFINE(HAVE_NSGETENVIRON, 1, [_NSGetEnviron])
	], [
		AC_MSG_RESULT(no)
	])

	AC_CHECK_DECL(IN_EXCL_UNLINK, [AC_DEFINE(HAVE_IN_EXCL_UNLINK, 1, [IN_EXCL_UNLINK])], [], [[#include <sys/inotify.h>]])

	if test x$host_sunos = xyes; then
		# set -Werror=strict-prototypes, to match the flags used during the compilation.
		ORIG_CFLAGS="$CFLAGS"
		CFLAGS="$CFLAGS -Werror=strict-prototypes"
	fi

	AC_CHECK_FUNCS(madvise)
	AC_CHECK_FUNCS(posix_madvise)

	if test x$host_sunos = xyes; then
		CFLAGS="$ORIG_CFLAGS"
	fi

	# *** End of Mono.Native checks ***
else
	AM_CONDITIONAL(ENABLE_GSS, false)

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

	AC_CHECK_TYPES([struct sockaddr_in6], [AC_DEFINE(HAVE_STRUCT_SOCKADDR_IN6)], , [#include <ws2tcpip.h>])

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

dnl IBM i does not have /dev/random, use unblocking only

    *-*-os400*)
    NAME_DEV_RANDOM="/dev/urandom"
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
*** entropy collection in place, please report an issue on GitHub and
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
AC_ARG_ENABLE(llvm-asserts,[  --enable-llvm-asserts Enable llvm asserts (option to LLVM in CMake)], enable_llvm_asserts=$enableval, enable_llvm_asserts=no)

AC_ARG_WITH(llvm, [  --with-llvm=<llvm prefix>    Enable the LLVM back-end], enable_llvm=yes,)

if test "x$enable_llvm" = "xdefault"; then
   enable_llvm=$enable_llvm_default
fi

if test "x$enable_loadedllvm" = "xyes"; then
   AC_MSG_WARN("Loadable LLVM is no longer supported. Enabling default LLVM support instead.")
   enable_llvm=yes
fi

enable_llvm_msvc_only="no"
if test "x$enable_llvm" = "xyes"; then
	if test "x$host_win32" = "xyes"; then
		if test "x$cross_compiling" = "xno"; then
			case "$target" in
			x86_64*)
				AC_MSG_WARN("LLVM for host=Windows and target=Windows is only supported on x64 MSVC builds. Disabling LLVM for GCC build.")
				enable_llvm_msvc_only="yes"
				;;
			i686*)
				AC_MSG_ERROR("LLVM for host=Windows and target=Windows is only supported for x64 builds.")
				;;
			esac
		fi
	fi
fi

internal_llvm="no"
if test "x$enable_llvm" = "xyes"; then
	if test "x$with_llvm" != "x"; then
		EXTERNAL_LLVM_CONFIG=$with_llvm/bin/llvm-config
		if test x$host_win32 = xyes; then
			EXTERNAL_LLVM_CONFIG=$EXTERNAL_LLVM_CONFIG.exe
		fi
		if test ! -x $EXTERNAL_LLVM_CONFIG; then
			AC_MSG_ERROR([LLVM executable $EXTERNAL_LLVM_CONFIG not found.])
		fi
	else
		internal_llvm=yes
	fi

	LLVM_CODEGEN_LIBS="x86codegen"
	case "$target" in
	arm*)
		LLVM_CODEGEN_LIBS="armcodegen"
		;;
	aarch64*)
		LLVM_CODEGEN_LIBS="aarch64codegen"
		;;
	powerpc*)
		LLVM_CODEGEN_LIBS="powerpccodegen"
		;;
	esac
	if test "x$host" != "x$target"; then
		# No need for jit libs
		LLVM_CODEGEN_LIBS=
	fi

	AC_SUBST(LLVM_CODEGEN_LIBS)
	AC_SUBST(EXTERNAL_LLVM_CONFIG)

	if test "x$host_win32" = "xyes" && test "x$cross_compiling" = "xno" &&  test "x$internal_llvm" = "xno"; then
		EXTERNAL_LLVM_CONFIG_WIN32=$(cygpath -m $EXTERNAL_LLVM_CONFIG)
		AC_SUBST(EXTERNAL_LLVM_CONFIG_WIN32)
	fi
	if test "x$enable_llvm_msvc_only" != "xyes"; then
		AC_DEFINE(ENABLE_LLVM, 1, [Enable the LLVM back end])
	else
		AC_DEFINE(ENABLE_LLVM_MSVC_ONLY, 1, [Enable the LLVM back end])
	fi
fi # ENABLE_LLVM

# AC_DEFINE necessary for correct restore behavior on Linux
AM_CONDITIONAL(INTERNAL_LLVM, [test "x$internal_llvm" != "xno" && test "x$enable_llvm_msvc_only" != "xyes"])
if test "x$internal_llvm" != "xno"; then
	if test "x$enable_llvm_msvc_only" != "xyes"; then
		AC_DEFINE(INTERNAL_LLVM, 1, [LLVM used is being build during mono build])
	else
		AC_DEFINE(INTERNAL_LLVM_MSVC_ONLY, 1, [LLVM used is being build during mono build])
	fi
fi

AM_CONDITIONAL(INTERNAL_LLVM_ASSERTS, [test "x$enable_llvm_asserts" != "xno" && test "x$enable_llvm_msvc_only" != "xyes"])
if test "x$enable_llvm_asserts" != "xno"; then
	if test "x$enable_llvm_msvc_only" != "xyes"; then
		AC_DEFINE(INTERNAL_LLVM_ASSERTS, 1, [Build LLVM with assertions])
	else
		AC_DEFINE(INTERNAL_LLVM_ASSERTS_MSVC_ONLY, 1, [Build LLVM with assertions])
	fi
fi

AM_CONDITIONAL(ENABLE_LLVM, [test x$enable_llvm = xyes && test x$enable_llvm_msvc_only != xyes])

if test "x$enable_llvm" = "xyes"; then
	enable_llvm_runtime=yes
fi

AM_CONDITIONAL(ENABLE_LLVM_RUNTIME, [test x$enable_llvm_runtime = xyes && test x$enable_llvm_msvc_only != xyes])
if test "x$enable_llvm_runtime" = "xyes"; then
	if test "x$enable_llvm_msvc_only" != "xyes"; then
		AC_DEFINE(ENABLE_LLVM_RUNTIME, 1, [Runtime support code for llvm enabled])
	else
		AC_DEFINE(ENABLE_LLVM_RUNTIME_MSVC_ONLY, 1, [Runtime support code for llvm enabled])
	fi
fi

TARGET="unknown"
ACCESS_UNALIGNED="yes"

LIBC="libc.so.6"
INTL="libc.so.6"
SQLITE="libsqlite.so.0"
SQLITE3="libsqlite3.so.0"
ODBC="libodbc.so.2"
X11="libX11.so"
GDKX11="libgdk-x11-2.0.so.0"
GTKX11="libgtk-x11-2.0.so.0"
XINERAMA="libXinerama.so.1"

sizeof_register="SIZEOF_VOID_P"

jit_wanted=true
boehm_supported=true
BTLS_SUPPORTED=no
BTLS_PLATFORM=
INTERP_CFLAGS=""

case "$host" in
	wasm32*)
		TARGET=WASM
		arch_target=wasm
		BTLS_SUPPORTED=no
		ACCESS_UNALIGNED="no"
		with_tls=pthread
		target_wasm=yes
		if test "x$mono_cv_clang" = "xyes"; then
		   INTERP_CFLAGS="-mllvm -join-liveintervals=false"
		   AC_SUBST(INTERP_CFLAGS)
		fi
		;;
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
			BTLS_SUPPORTED=no
			BTLS_PLATFORM=i386
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
			AOT_SUPPORTED="yes"
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=i386
		        ;;
		esac
		;;
	x86_64-*-* | amd64-*-*)
		TARGET=AMD64;
		arch_target=amd64;
		if test "x$ac_cv_sizeof_void_p" = "x4"; then
			AC_DEFINE(MONO_ARCH_ILP32, 1, [64 bit mode with 4 byte longs and pointers])
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
			AOT_SUPPORTED="yes"
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=x86_64
			;;
		  mingw*|cygwin*)
			BTLS_SUPPORTED=no
			BTLS_PLATFORM=x86_64
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
		case "$host" in
			x86_64*mingw*)
				# Old Boehm fails to compile for x86_64-mingw.
				# It is trivial to fix, but just silently drop it.
				boehm_supported=false
				;;
		esac
		;;
	macppc-*-openbsd* | powerpc*-*-linux* | powerpc-*-openbsd* | \
        powerpc-*-sysv* | powerpc-*-darwin* | powerpc-*-netbsd* | \
        powerpc*-*-freebsd* | powerpc*-*-aix* | powerpc*-*-os400* )
		if test "x$ac_cv_sizeof_void_p" = "x8"; then
			TARGET=POWERPC64;
			CPPFLAGS="$CPPFLAGS -D__mono_ppc__ -D__mono_ppc64__"
			# mono#18554 - be more robust in testing for -mminimal-toc
			AC_MSG_NOTICE([Checking for PowerPC ISA -mminimal-toc support])
			AX_CHECK_COMPILE_FLAG(
				[-mminimal-toc],
				[CFLAGS="$CFLAGS -mminimal-toc"],
				[CFLAGS="$CFLAGS"]
			)
			case "$host" in
				powerpc*-*-freebsd*)
					# We need to be aware if we are ELFv1 or v2 here
					AC_MSG_NOTICE([Checking FreeBSD ELF version])
					if ! ( echo | cc -dM -E - | awk '/_CALL_ELF/ {print $NF}'); then
						AC_DEFINE([POWERPC_ELF], 1, [PowerPC ELFv1])
					else
						AC_DEFINE([POWERPC_ELFV2], 1, [PowerPC ELFv2])
					fi
					;;
			esac
		else
			TARGET=POWERPC;
			CPPFLAGS="$CPPFLAGS -D__mono_ppc__"
		fi
		arch_target=ppc;
		case $host_os in
		  aix*|os400*)
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=powerpc
			;;
		  linux*)
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=powerpc
			;;
		  freebsd*)
			BTLS_SUPPORTED=yes
			BTLS_PLATFORM=powerpc
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
	aarch64-*darwin*ilp32)
		TARGET=ARM6432
		arch_target=arm64
		AC_DEFINE(MONO_ARCH_ILP32, 1, [64 bit mode with 4 byte longs and pointers])
		sizeof_register=8
		# assuming no other target other than watchOS is using aarch64*darwin triple
		TARGET_SYS=WATCHOS
		;;
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
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=s390x
		CFLAGS="$CFLAGS -mbackchain -D__USE_STRING_INLINES"
		;;
	riscv32-*)
		TARGET=RISCV32
		ACCESS_UNALIGNED=no
		AOT_SUPPORTED=no
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=riscv32
		arch_target=riscv32
		boehm_supported=false
		;;
	riscv64*)
		TARGET=RISCV64
		ACCESS_UNALIGNED=no
		AOT_SUPPORTED=no
		BTLS_SUPPORTED=yes
		BTLS_PLATFORM=riscv64
		arch_target=riscv64
		boehm_supported=false
		;;
esac

HOST=$TARGET

if test "x$host" != "x$target"; then
   AC_DEFINE(MONO_CROSS_COMPILE,1,[The runtime is compiled for cross-compiling mode])
   enable_mcs_build=no
   enable_support_build=no
   BTLS_SUPPORTED=no
   # Can't use tls, since it depends on the runtime detection of tls offsets
   # in mono-compiler.h
   with_tls=pthread
   target_mach=no
   case "$target" in
   wasm32*)
		TARGET=WASM
		arch_target=wasm
		AC_DEFINE(TARGET_WASM, 1, [Target wasm])
		;;
   arm*-darwin*)
		TARGET=ARM;
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
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
		AC_DEFINE(MONO_ARCH_ILP32, 1, [64 bit mode with 4 byte longs and pointers])
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
   arm*-linux-*)
		TARGET=ARM;
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		;;
   arm*-netbsd*-eabi*)
		TARGET=ARM;
		CPPFLAGS="$CPPFLAGS -D__ARM_EABI__"
		;;
   i686*-linux-*)
		TARGET=X86;
		;;
   i*86-apple-*)
		TARGET=X86;
		;;
   x86_64*-linux-*)
		TARGET=AMD64;
		;;
   x86_64-ps4-freebsd)
		TARGET=AMD64;
		AC_DEFINE(TARGET_PS4, 1, [...])
		AC_DEFINE(DISABLE_HW_TRAPS, 1, [...])
		CPPFLAGS="$CPPFLAGS"
		target_win32=no
		;;
	aarch64*darwin*_ilp32)
		TARGET=ARM6432;
		AC_DEFINE(MONO_ARCH_ILP32, 1, [64 bit mode with 4 byte longs and pointers])
		sizeof_register=8
		AC_DEFINE(TARGET_WATCHOS, 1, [...])
		;;
	aarch64-*)
		TARGET=ARM64
		;;
	riscv32*)
		TARGET=RISCV32
		AC_DEFINE([TARGET_RISCV], [1], [Target is RISC-V])
		AC_DEFINE([TARGET_RISCV32], [1], [Target is 32-bit RISC-V])
		arch_target=riscv32
		target_mach=no
		with_tls=pthread
		;;
	riscv64*)
		TARGET=RISCV64
		AC_DEFINE([TARGET_RISCV], [1], [Target is RISC-V])
		AC_DEFINE([TARGET_RISCV64], [1], [Target is 64-bit RISC-V])
		arch_target=riscv64
		target_mach=no
		with_tls=pthread
		;;
	x86_64-google-fuchsia)
		TARGET=AMD64
		target_win32=nop
		AC_DEFINE(HOST_FUCHSIA,1,[Targeting the Fuchsia platform])
		;;
	*)
		AC_MSG_ERROR([Cross compiling is not supported for target $target])
	esac

	case "$target" in
	*-darwin*)
		target_mach=yes
		;;
	*-linux-android*)
		AC_DEFINE(TARGET_ANDROID, 1, [...])
		;;
	esac
fi

case "$TARGET" in
WASM)
	AC_DEFINE(TARGET_WASM, 1, [...])
	arch_target=wasm
	;;
X86)
	AC_DEFINE(TARGET_X86, 1, [...])
	arch_target=x86
	;;
AMD64)
	AC_DEFINE(TARGET_AMD64, 1, [...])
	arch_target=amd64
	;;
ARM)
	AC_DEFINE(TARGET_ARM, 1, [...])
	arch_target=arm
	ACCESS_UNALIGNED="no"
	;;
ARM64|ARM6432)
	AC_DEFINE(TARGET_ARM64, 1, [...])
	arch_target=arm64
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
RISCV32)
	AC_DEFINE([TARGET_RISCV], [1], [Target is RISC-V])
	AC_DEFINE([TARGET_RISCV32], [1], [Target is 32-bit RISC-V])
	;;
RISCV64)
	AC_DEFINE([TARGET_RISCV], [1], [Target is RISC-V])
	AC_DEFINE([TARGET_RISCV64], [1], [Target is 64-bit RISC-V])
	;;
esac

case "$TARGET" in
*32* | ARM | X86 | POWERPC | MIPS | SPARC | WASM | ARM6432)
    target_sizeof_void_p=4
    ;;
*64* | S390X)
    target_sizeof_void_p=8
    ;;
*)
    AC_MSG_ERROR([unknown target])
    ;;
esac

case "$HOST" in
WASM)
	AC_DEFINE(HOST_WASM, 1, [...])
	;;
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
RISCV32)
	AC_DEFINE([HOST_RISCV], [1], [Host is RISC-V])
	AC_DEFINE([HOST_RISCV32], [1], [Host is 32-bit RISC-V])
	;;
RISCV64)
	AC_DEFINE([HOST_RISCV], [1], [Host is RISC-V])
	AC_DEFINE([HOST_RISCV64], [1], [Host is 64-bit RISC-V])
	;;
esac

MONO_ARCH_GSHAREDVT_SUPPORTED=0
case "$HOST" in
X86 | AMD64 | ARM | ARM64)
	MONO_ARCH_GSHAREDVT_SUPPORTED=1 # keep in sync with mini-{x86,amd64,arm,arm64}.h
	;;
esac

AM_CONDITIONAL(MONO_ARCH_GSHAREDVT_SUPPORTED, test $MONO_ARCH_GSHAREDVT_SUPPORTED = 1)
AM_CONDITIONAL(TARGET_WASM, test $arch_target = wasm)

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
	X86 | AMD64 | ARM | ARM64 | ARM6432 | POWERPC | POWERPC64 | MIPS | S390X | SPARC | SPARC64 | RISCV32 | RISCV64)
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
   elif test "x$TARGET" = "xARM" -o "x$TARGET" = "xARM64" -o "x$TARGET" = "xARM6432"; then
   	  AC_DEFINE(TARGET_IOS,1,[The JIT/AOT targets iOS])
	  CPPFLAGS_FOR_LIBGC="$CPPFLAGS_FOR_LIBGC -DTARGET_IOS"
	  CFLAGS_FOR_LIBGC="$CFLAGS_FOR_LIBGC -DTARGET_IOS"
	  BTLS_SUPPORTED=no
	  target_ios=yes
   else
       AC_TRY_COMPILE([#include "TargetConditionals.h"],[
       #if TARGET_IPHONE_SIMULATOR == 1 || TARGET_OS_IPHONE == 1
       #error fail this for ios
       #endif
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
		  target_ios=yes
       ])
	fi
   AC_DEFINE(TARGET_MACH,1,[The JIT/AOT targets Apple platforms])
fi

AM_CONDITIONAL(TARGET_OSX, test x$target_osx = xyes)

AC_SUBST(SIZEOF_VOID_P,[$ac_cv_sizeof_void_p])

if test "x$target_byte_order" = "xG_BIG_ENDIAN"; then
   AC_DEFINE(TARGET_BYTE_ORDER,G_BIG_ENDIAN,[byte order of target])
elif test "x$target_byte_order" = "xG_LITTLE_ENDIAN"; then
   AC_DEFINE(TARGET_BYTE_ORDER,G_LITTLE_ENDIAN,[byte order of target])
else
   AC_DEFINE(TARGET_BYTE_ORDER,G_BYTE_ORDER,[byte order of target])
fi

AC_DEFINE_UNQUOTED(TARGET_SIZEOF_VOID_P,${target_sizeof_void_p},[wordsize of target])

if test "x$sizeof_register" = "x4"; then
  AC_DEFINE(SIZEOF_REGISTER,4,[size of target machine integer registers])
elif test "x$sizeof_register" = "x8"; then
  AC_DEFINE(SIZEOF_REGISTER,8,[size of target machine integer registers])
else
  AC_DEFINE_UNQUOTED(SIZEOF_REGISTER,${target_sizeof_void_p},[size of target machine integer registers])
fi

# zlib/configure detects visibility support similarly and defines HAVE_HIDDEN
# on the compiler command line (not config.h).
if test "x$have_visibility_hidden" = "xyes"; then
   AC_DEFINE(HAVE_VISIBILITY_HIDDEN, 1, [Support for the visibility ("hidden") attribute])
   ZLIB_CFLAGS="$ZLIB_CFLAGS -DHAVE_HIDDEN"
fi

if test "x$have_deprecated" = "xyes"; then
   AC_DEFINE(HAVE_DEPRECATED, 1, [Support for the deprecated attribute])
fi

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
     *-*-aix*)
	dnl on AIX/PASE, shared libraries can be and usually are inside archives
	dnl so, we specify them by libfoo.a(libfoo.so.0) for libtool's conventions,
	dnl or libfoo.a(shr[_64].o) for the AIX system convention,
	dnl or lib.so[.0](shr[_64].o) for libtool's hybrid convention
	dnl we may hardcode 64-bit names at times, but we don't do 32-bit AIX
	LIBC="libc.a(shr_64.o)"
	# Thanks, I hate it! This is crumbly, especially the one-liner.
	AC_MSG_CHECKING([how to dlopen libintl])
	AC_LINK_IFELSE([AC_LANG_SOURCE([
		/* XXX: Check for libintl header/gettext func better? */
		#include <libintl.h>

		int main (void) {
			gettext("Dummy for autoconf");
			return 1;
		}
	])],
	[
	INTL="$(dump -X64 -Hp conftest$EXEEXT | grep libintl | cut -c 38- | tr -s " " | awk '{print $1"("$2")"}')"
	AC_MSG_RESULT([yes, $INTL])
	],
	[
	INTL="libintl.a(libintl.so.8)"
	AC_MSG_RESULT([no, falling back to $INTL])
	])
	SQLITE3="libsqlite3.a(libsqlite3.so.0)"
	# it's very tempting to set a libsuffix, but it depends on the soname value
	;;
     *-*-os400*)
	dnl However, IBM's packages for i try to be more "normal" than the AIX world, naming wise.
	dnl The library archives has "normal" .so names, but they're members with predictable "shr" names for fat libraries.
	LIBC="libc.a(shr_64.o)"
	INTL="libintl.so(shr_64.o)"
	SQLITE3="libsqlite3.so(shr_64.o)"
	# likewise, it's safer to assume ".so(shr_64.o)" due to official IBM packages being built this way, but as with AIX, others may not be guaranteed
	# it may be worth revisiting this in the future
	;;
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
	if test "x$with_core" != "xonly"; then
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
	fi
	;;
esac

AC_SUBST(libsuffix)

######################################
# EGLIB CHECKS
######################################

GNUC_PRETTY=
GNUC_UNUSED=
BREAKPOINT="G_STMT_START { raise (SIGTRAP); } G_STMT_END"
if test x$GCC = xyes; then
   GNUC_UNUSED='__attribute__((__unused__))'
   GNUC_NORETURN='__attribute__((__noreturn__))'
   case $host_cpu in
     i*86|x86_64) BREAKPOINT="G_STMT_START { __asm__ (\"int \$03\"); } G_STMT_END" ;;
   esac
fi
AC_SUBST(GNUC_PRETTY)
AC_SUBST(GNUC_UNUSED)
AC_SUBST(GNUC_NORETURN)
AC_SUBST(BREAKPOINT)

AC_C_BIGENDIAN([ORDER=G_BIG_ENDIAN],[ORDER=G_LITTLE_ENDIAN])

case $host in
*-*-msdos* | *-*-go32* | *-*-mingw32* | *-*-cygwin* | *-*-windows*)
    PATHSEP='\\'
    SEARCHSEP=';'
    OS="WIN32"
    PIDTYPE='void *'
    ;;
*)
    PATHSEP='/'
    SEARCHSEP=':'
    OS="UNIX"
    PIDTYPE='int'
    ;;
esac

case $host in
	*-*-solaris*)
	CFLAGS="$CFLAGS -D_POSIX_PTHREAD_SEMANTICS"
	;;
esac

case $target in
arm*-darwin*|aarch64*-*|riscv*)
    CFLAGS="$CFLAGS -U_FORTIFY_SOURCE"
    ;;
i*86-*-darwin*)
    ORDER=G_LITTLE_ENDIAN
    ;;
*-*-haiku*)
    LDFLAGS="$LDFLAGS -ltextencoding"
    ;;
*-*-openbsd*)
    CFLAGS="$CFLAGS -pthread"
    LDFLAGS="$LDFLAGS -pthread"
    ;;
esac

AC_SUBST(ORDER)
AC_SUBST(PATHSEP)
AC_SUBST(SEARCHSEP)
AC_SUBST(OS)
AC_SUBST(PIDTYPE)

# Defined for all targets/platforms using classic Windows API support.
AC_DEFINE(HAVE_CLASSIC_WINAPI_SUPPORT, 1, [Use classic Windows API support])
AC_DEFINE(HAVE_UWP_WINAPI_SUPPORT, 0, [Don't use UWP Windows API support])

AC_CHECK_FUNCS(strlcpy stpcpy strtok_r rewinddir vasprintf)
AC_CHECK_FUNCS(getrlimit)
AC_CHECK_FUNCS(fork execv execve)
AC_CHECK_FUNCS(waitpid)

AC_ARG_WITH([overridable-allocators], [  --with-overridable-allocators	allow g_*alloc/g_free to call custom allocators set via g_mem_set_vtable])

if test x$with_overridable_allocators = xyes; then
	AC_DEFINE(ENABLE_OVERRIDABLE_ALLOCATORS,1,[Overridable allocator support enabled])
  AC_MSG_NOTICE([Overridable allocator support enabled])
else
  AC_MSG_NOTICE([Overridable allocator support disabled])
fi

#
# Mono currently supports 10.6, but strndup is not available prior to 10.7; avoiding
# the detection of strndup on OS X so Mono built on 10.7+ still runs on 10.6. This can be
# removed once support for 10.6 is dropped.
#
# iOS detection of strndup and getpwuid_r is faulty for some reason so let's simply avoid it
#
if test x$target_osx = xyes; then
AC_CHECK_FUNCS(getpwuid_r)
elif test x$target_ios = xno; then
AC_CHECK_FUNCS(strndup getpwuid_r)
fi

AC_SEARCH_LIBS(sqrtf, m)

# nanosleep may not be part of libc, also search it in other libraries
AC_SEARCH_LIBS(nanosleep, rt)

AC_SEARCH_LIBS(dlopen, dl)

# AIX/OS400 -bsymbolic
# Apple -bind_at_load
# The rest GNU.
#
export_link_flags=--export-dynamic
case $host_os in
mingw*|cygwin*)
	export_link_flags=--export-all-symbols
	;;
*-*-darwin*)
	export_link_flags=--export_dynamic
	;;
esac
AX_APPEND_LINK_FLAGS([-Wl,$export_link_flags -Wl,-Bsymbolic -bsymbolic -Wl,-z,now -Wl,-bind_at_load])

AC_CHECK_HEADERS(getopt.h sys/select.h sys/time.h sys/wait.h pwd.h iconv.h sys/types.h sys/resource.h)
dnl giconv.c will check on HAVE_ICONV_H but we need this for link time
AC_CHECK_LIB(iconv, iconv_open)
AC_CHECK_HEADER(alloca.h, [HAVE_ALLOCA_H=1], [HAVE_ALLOCA_H=0])
AC_SUBST(HAVE_ALLOCA_H)

# Get the exact type of size_t, not just its size.
# This is so we can find an exact printf format among u, lu, llu, I64u.
# To avoid warnings about slight mismatches.
# C99 runtime "zu" dependency is being avoided here.
#
# We have to compile as C++ because C is too lenient
# and lets the wrong thing compile, with warnings.
#
# Note: "zu" or ifdef <something> have the advantage
# of producing installable "biarch" headers. i.e. one Mac header
# for Mac/x86 and Mac/amd64.

AC_LANG_PUSH(C++)

# Check long before int because it is the overwhelming Unix answer,
# across 32bit and 64bit systems -- fewer compiler invocations in autoconf.
#
# long ahead of int also tends to produce biarch-compatible headers except Windows.
#
AC_MSG_CHECKING(if size_t is unsigned long)
AC_COMPILE_IFELSE([AC_LANG_SOURCE([
	#include <stddef.h>
	unsigned long *a = (size_t*)0;
])], [
	AC_MSG_RESULT(yes)
	AC_SUBST(GSIZE_FORMAT, '"lu"')
], [
	AC_MSG_RESULT(no)
	AC_MSG_CHECKING(if size_t is unsigned int)
	AC_COMPILE_IFELSE([AC_LANG_SOURCE([
		#include <stddef.h>
		unsigned *a = (size_t*)0;
	])], [
		AC_MSG_RESULT(yes)
		AC_SUBST(GSIZE_FORMAT, '"u"')
	], [
# At this point the majority of systems have their answer,
# and we descend into non-standard or new-standard territory.
#
# Check __int64 first because I64 on some systems predates ll, enabling
# new compiler/old runtime interop, and the types always have the same size.
		AC_MSG_RESULT(no)
		AC_MSG_CHECKING(if size_t is unsigned __int64)
		AC_COMPILE_IFELSE([AC_LANG_SOURCE([
			#include <stddef.h>
			unsigned __int64 *a = (size_t*)0;
		])], [
			AC_MSG_RESULT(yes)
			AC_SUBST(GSIZE_FORMAT, '"I64u"')
		], [
			AC_MSG_RESULT(no)
			AC_MSG_CHECKING(if size_t is unsigned long long)
			AC_COMPILE_IFELSE([AC_LANG_SOURCE([
				#include <stddef.h>
				unsigned long long *a = (size_t*)0;
			])], [
				AC_MSG_RESULT(yes)
				AC_SUBST(GSIZE_FORMAT, '"llu"')
			], [
				AC_MSG_RESULT(no)
				AC_MSG_ERROR(Unable to determine size_t among unsigned long, int, __int64, long long)
			] )
		] )
	] )
] )

AC_LANG_POP

# If size_t/ptrdiff_t is correct, use it. Otherwise C99 [u]intptr_t.
# This provides for an exact match with functions that
# take size_t like malloc and pthread_attr_getstacksize, avoiding warnings.
# uintptr_t is not necessarily the same.
#
# ptrdiff_t is preferred over ssize_t as it is C89 vs. new Posix.
# ssize_t looks nicer but ptrdiff_t is wrapped up as gssize anyway.

if test $ac_cv_sizeof_void_p = $ac_cv_sizeof_size_t; then
   GSIZE="size_t"
   GSSIZE="ptrdiff_t"
else
   GSIZE="uintptr_t"
   GSSIZE="intptr_t"
fi

AC_SUBST(GSIZE)
AC_SUBST(GSSIZE)
AC_SUBST(GSIZE_FORMAT)

#
# END OF EGLIB CHECKS
#

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
    no)
    libgdiplus_loc=
    ;;

    installed)
    libgdiplus_loc=
    if test x$cross_compiling = xno; then
        if test x$host_darwin = xyes; then
	    a=/Library/Frameworks/Mono.framework/Versions/Current/lib/$libgdiplus_install_loc
	    if test -x $a; then
	      libgdiplus_install_loc=$a
	      libgdiplus_loc=$a
	    fi
	fi
    fi
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
AC_SUBST(libgdiplus_loc)
AC_SUBST(libgdiplus_install_loc)

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

AM_CONDITIONAL(DISABLE_ICALL_TABLES, test x$icall_tables = xno)

if test "x$with_tls" = "x__thread"; then
	AC_DEFINE(MONO_KEYWORD_THREAD, __thread, [Have __thread keyword])
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
	], [
		arm_v5=yes

		arm_ver=ARMv5
	], [])

	AC_TRY_COMPILE([], [
		#if !defined(__ARM_ARCH_6J__) && !defined(__ARM_ARCH_6ZK__) && !defined(__ARM_ARCH_6K__) && !defined(__ARM_ARCH_6T2__) && !defined(__ARM_ARCH_6M__)
		#error Not on ARM v6.
		#endif
	], [
		arm_v5=yes
		arm_v6=yes

		arm_ver=ARMv6
	], [])

	AC_TRY_COMPILE([], [
		#if !defined(__ARM_ARCH_7A__) && !defined(__ARM_ARCH_7R__) && !defined(__ARM_ARCH_7EM__) && !defined(__ARM_ARCH_7M__) && !defined(__ARM_ARCH_7S__)
		#error Not on ARM v7.
		#endif
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

	if test x$host_linux = xyes; then
		RID="linux-arm"
		COREARCH="arm"
		if test x$fpu = xNONE; then
			RID="linux-armel"
			COREARCH="armel"
		fi
	fi
fi

AC_SUBST(RID)
AC_SUBST(COREARCH)
AC_SUBST(CORETARGETS)

if test ${TARGET} = RISCV32 -o ${TARGET} = RISCV64; then
	AC_ARG_WITH([riscv-fpabi], [  --with-riscv-fpabi=auto,double,soft   Select RISC-V floating point ABI (auto)], [fpabi=$withval], [fpabi=double])

	AC_MSG_CHECKING([which RISC-V floating point ABI to use])

	if test x$fpabi = xauto; then
		AC_TRY_COMPILE([], [
			#ifdef __riscv_float_abi_double
			#error "double"
			#endif
		], [
			fpabi=auto
		], [
			fpabi=double
		])
	fi

	if test x$fpabi = xauto; then
		AC_TRY_COMPILE([], [
			#ifdef __riscv_float_abi_single
			#error "single"
			#endif
		], [
			fpabi=auto
		], [
			fpabi=single
		])
	fi

	if test x$fpabi = xauto; then
		AC_TRY_COMPILE([], [
			#ifdef __riscv_float_abi_soft
			#error "soft"
			#endif
		], [
			fpabi=auto
		], [
			fpabi=soft
		])
	fi

	case $fpabi in
	double)
		AC_DEFINE([RISCV_FPABI_DOUBLE], [1], [RISC-V FPABI is double-precision])
		AC_MSG_RESULT([double-precision])
		;;
	single)
		AC_DEFINE([RISCV_FPABI_SINGLE], [1], [RISC-V FPABI is single-precision])
		AC_MSG_ERROR([single-precision, not supported])
		;;
	soft)
		AC_DEFINE([RISCV_FPABI_SOFT], [1], [RISC-V FPABI is soft float])
		AC_MSG_RESULT([soft float])
		;;
	auto)
		AC_MSG_ERROR([unknown])
		;;
	*)
		AC_MSG_ERROR([invalid option: $fpabi])
		;;
	esac
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
		ac_configure_args="$ac_configure_args --enable-single-obj-compilation --disable-embed-check --with-libgc-threads=$libgc_threads $libgc_configure_args \"CPPFLAGS_FOR_LIBGC=$TMP_CPPFLAGS\" \"CFLAGS_FOR_LIBGC=$CFLAGS_FOR_LIBGC\""
		if test "x$support_boehm" = "xyes"; then
			AC_CONFIG_SUBDIRS(external/bdwgc)
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

dnl *****************************
dnl *** Thread suspend policy ***
dnl *****************************

dnl Set a default hybrid or cooperative suspend on some platforms

dnl Coop default is set by the bitcode preset.

dnl If coop isn't on by default, maybe hybrid should be?
if test x$enable_cooperative_suspend_default != xyes; then
	case $HOST in
	X86 | AMD64)
		dnl Some host/target confusion, there's no host_osx (and
		dnl host_darwin would be true on iOS not just macOS).
		if test x$target_osx = xyes; then
			enable_hybrid_suspend_default=yes
		elif test x$host_linux = xyes -o x$host_win32 = xyes; then
			enable_hybrid_suspend_default=yes
		fi
		;;
	esac
fi

dnl Now check if there were flags overriding the defaults

AC_ARG_WITH(cooperative_gc,        [  --with-cooperative-gc=yes|no        Enable cooperative stop-the-world garbage collection (sgen only) (defaults to no)], [AC_MSG_WARN([--with-cooperative-gc is deprecated, use --enable-cooperative-suspend instead])], [with_cooperative_gc=default])
AC_ARG_ENABLE(cooperative_suspend, [  --enable-cooperative-suspend      Enable cooperative stop-the-world garbage collection (sgen only) (defaults to no)], [], [enable_cooperative_suspend=default])

if test x$enable_cooperative_suspend = xdefault -a x$with_cooperative_gc != xdefault; then
	enable_cooperative_suspend=$with_cooperative_gc
fi

if test x$enable_cooperative_suspend = xdefault; then
	enable_cooperative_suspend=$enable_cooperative_suspend_default
fi

if test x$enable_cooperative_suspend != xno; then
	AC_DEFINE(ENABLE_COOP_SUSPEND,1,[Enable cooperative stop-the-world garbage collection.])
	enable_hybrid_suspend_default=no
fi

AM_CONDITIONAL([ENABLE_COOP_SUSPEND], [test x$enable_cooperative_suspend != xno])

AC_ARG_ENABLE(hybrid_suspend, [ --enable-hybrid-suspend     Enable hybrid stop-the-world garbage collection (sgen only) - cooperative suspend for threads running managed and runtime code, and preemptive suspend for threads running native and P/Invoke code (defaults to no)], [], [enable_hybrid_suspend=default])

if test x$enable_hybrid_suspend = xdefault; then
   enable_hybrid_suspend=$enable_hybrid_suspend_default
fi

if test x$enable_hybrid_suspend != xno -a x$enable_cooperative_suspend != xno ; then
	AC_MSG_ERROR([Hybrid suspend and Cooperative suspend cannot be both enabled.])
fi

if test x$enable_hybrid_suspend != xno ; then
	AC_DEFINE(ENABLE_HYBRID_SUSPEND,1,[Enable hybrid suspend for GC stop-the-world])
fi

AM_CONDITIONAL([ENABLE_HYBRID_SUSPEND], [test x$enable_hybrid_suspend != xno])

dnl End of thread suspend policy

dnl ***************************
dnl *** feature experiments ***
dnl ***************************

dnl When adding experiments, also add to mono/utils/mono-experiments.def
AC_ARG_ENABLE(experiment, [ --enable-experiment=LIST       Enable experimental fatures from the comma-separate LIST.  Available experiments: null,tiered],[

	if test x$enable_experiment != x ; then
		AC_DEFINE(ENABLE_EXPERIMENTS,1,[Enable feature experiments])
	fi
	for feature in `echo "$enable_experiment" | sed -e "s/,/ /g"`; do
		eval "mono_experiment_test_enable_$feature='yes'"
	done

	if test "x$mono_experiment_test_enable_all" = "xyes"; then
		eval "mono_experiment_test_enable_null='yes'"
		eval "mono_experiment_test_enable_tiered='yes'"
		true
	fi

	if test "x$mono_experiment_test_enable_null" = "xyes"; then
		AC_DEFINE(ENABLE_EXPERIMENT_null, 1, [Enable experiment 'null'])
	fi
	if test "x$mono_experiment_test_enable_tiered" = "xyes"; then
		AC_DEFINE(ENABLE_EXPERIMENT_TIERED, 1, [Enable experiment 'Tiered Compilation'])
	fi
],[])

AM_CONDITIONAL(ENABLE_EXPERIMENT_TIERED, test x$mono_experiment_test_enable_tiered = xyes)

dnl **********************
dnl *** checked builds ***
dnl **********************

AC_ARG_ENABLE(checked_build, [  --enable-checked-build=LIST      To enable checked build (expensive asserts), configure with a comma-separated LIST of checked build modules and then include that same list in the environment variable MONO_CHECK_MODE at runtime. Recognized checked build modules: all, gc, metadata, thread, private_types, crash_reporting],[

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
		eval "mono_checked_build_test_enable_private_types='yes'"
		eval "mono_checked_build_test_enable_crash_reporting='yes'"
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

	if test "x$mono_checked_build_test_enable_private_types" = "xyes"; then
		AC_DEFINE(ENABLE_CHECKED_BUILD_PRIVATE_TYPES, 1, [Enable private types checked build])
	fi

	if test "x$mono_checked_build_test_enable_crash_reporting" = "xyes"; then
		# Required
		with_overridable_allocators=yes
		AC_DEFINE(ENABLE_OVERRIDABLE_ALLOCATORS,1,[Overridable allocator support enabled])

		AC_DEFINE(ENABLE_CHECKED_BUILD_CRASH_REPORTING, 1, [Enable private types checked build])
	fi
], [])

dnl End of checked builds

AM_CONDITIONAL(NEED_VASPRINTF, test x$ac_cv_func_vasprintf = xno || test x$with_overridable_allocators = xyes)

AC_CHECK_HEADER([malloc.h], 
		[AC_DEFINE([HAVE_USR_INCLUDE_MALLOC_H], [1], 
			[Define to 1 if you have /usr/include/malloc.h.])],,)

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
AC_ARG_ENABLE(btls-lib, [  --disable-btls-lib             Disable building the BTLS native library], enable_btls_lib=$enableval, enable_btls_lib=$BTLS_SUPPORTED)
AC_ARG_WITH(btls_android_ndk, [  --with-btls-android-ndk        Android NDK for BoringTls])
AC_ARG_WITH(btls_android_api, [  --with-btls-android-api        Android NDK API level for BTLS build])
AC_ARG_WITH(btls_android_cmake_toolchain, [  --with-btls-android-cmake-toolchain        Path to the cmake toolchain file for Android])
AC_ARG_WITH(btls_android_ndk_asm_workaround, [  --with-btls-android-ndk-asm-workaround        Work around a problem with NDK r18+ clang compiler when compiling some AES assembly for ARM])

if test "x$with_btls_android_api" = "x" ; then
	case "$BTLS_PLATFORM" in
		android-armv5)
			with_btls_android_api=16" ;;
		android-armv6)
			with_btls_android_api=16" ;;
		android-armv7)
			with_btls_android_api=16 ;;
		android-v8a)
			with_btls_android_api=21 ;;
		android-x86)
			with_btls_android_api=16 ;;
		android-x64)
			with_btls_android_api=21 ;;
	esac
fi

if test "x$enable_btls" = "xno"; then
   enable_btls_lib=no
fi

AM_CONDITIONAL(BTLS, test x$enable_btls_lib = xyes)

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

	if test "x$host_win32" = "xyes"; then
		AC_CHECK_PROG(HAVE_YASM, yasm, yes, no)
	fi

	case "$BTLS_PLATFORM" in
	i386)
		btls_arch=i386
		btls_cflags="-m32"
		case $host_os in
			darwin*)
				btls_cflags="$btls_cflags -arch i386"
				;;
			mingw*|cygwin*)
				btls_cflags="-DNOCRYPT $WIN32_CPPFLAGS"
				if test "x$HAVE_YASM" != "xyes"; then
					BTLS_CMAKE_ARGS="-DOPENSSL_NO_ASM=1"
				fi
				;;
		esac
		;;
	x86_64)
		btls_arch=x86_64
		case $host_os in
			mingw*|cygwin*)
				btls_cflags="-DNOCRYPT $WIN32_CPPFLAGS"
				if test "x$HAVE_YASM" != "xyes"; then
					BTLS_CMAKE_ARGS="-DOPENSSL_NO_ASM=1"
				fi
				;;
		esac
		;;
	arm)
		btls_arch=arm
		btls_cflags="-DOPENSSL_NO_ASM=1"
		;;
	armsoft)
		btls_arch=arm
		btls_cflags="-DOPENSSL_NO_ASM=1"
		;;
	aarch64)
		btls_arch=aarch64
		btls_cflags="-march=armv8-a+crypto"
		;;
	s390x)
		btls_arch=s390x
		;;
	powerpc)
		btls_arch=powerpc
		case $host_os in
			aix*|os400*)
				btls_cflags="$btls_cflags -maix64 -mminimal-toc -pthread -D_ALL_SOURCE -D_THREAD_SAFE -D_REENTRANT"
				BTLS_CMAKE_ARGS="$BTLS_CMAKE_ARGS -DCMAKE_AR=/usr/bin/ar -DCMAKE_C_ARCHIVE_CREATE=\"<CMAKE_AR> -X64 cr <TARGET> <LINK_FLAGS> <OBJECTS>\""
		esac
		;;
	android-armv5)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"armeabi\" -DANDROID_NATIVE_API_LEVEL=$with_btls_android_api"
		;;
	android-armv6)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"armeabi\" -DANDROID_NATIVE_API_LEVEL=$with_btls_android_api"
		;;
	android-armv7)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"armeabi-v7a\" -DANDROID_NATIVE_API_LEVEL=$with_btls_android_api"
		;;
	android-v8a)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"arm64-v8a\" -DANDROID_NATIVE_API_LEVEL=$with_btls_android_api"
		;;
	android-x86)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"x86\" -DANDROID_NATIVE_API_LEVEL=$with_btls_android_api"
		;;
	android-x64)
		BTLS_CMAKE_ARGS="-DANDROID_ABI=\"x86_64\" -DANDROID_NATIVE_API_LEVEL=$with_btls_android_api"
		;;
	riscv32)
		btls_arch=riscv32
		;;
	riscv64)
		btls_arch=riscv64
		;;
	*)
		AC_MSG_ERROR(Invalid BTLS platform)
	esac

	if test "x$platform_android" = "xyes"; then
	   	if test "x$with_btls_android_cmake_toolchain" = "x"; then
		   with_btls_android_cmake_toolchain="$BTLS_ROOT/util/android-cmake/android.toolchain.cmake"
		fi

		if test "x$with_btls_android_ndk_asm_workaround" != "x"; then
		   BTLS_CMAKE_ARGS="$BTLS_CMAKE_ARGS -DENABLE_NDK_ARM_WORKAROUND=ON"
		fi

		btls_android=yes
		BTLS_CMAKE_ARGS="$BTLS_CMAKE_ARGS -DCMAKE_TOOLCHAIN_FILE=$with_btls_android_cmake_toolchain"
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

## Maybe should also disable if mcsdir is invalid.  Let's punt the issue for now.
AM_CONDITIONAL(BUILD_MCS, [test x$cross_compiling = xno && test x$enable_mcs_build != xno])
AM_CONDITIONAL(BUILD_SUPPORT, [test x$enable_support_build != xno])

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
AM_CONDITIONAL(ARM64, test x$TARGET = xARM64 -o x$TARGET = xARM6432)
AM_CONDITIONAL(S390X, test x$TARGET = xS390X)
AM_CONDITIONAL([RISCV], [test x$TARGET = xRISCV32 -o x$TARGET = xRISCV64])
AM_CONDITIONAL([RISCV32], [test x$TARGET = xRISCV32])
AM_CONDITIONAL([RISCV64], [test x$TARGET = xRISCV64])
AM_CONDITIONAL(WASM, test x$TARGET = xWASM)
AM_CONDITIONAL(HOST_X86, test x$HOST = xX86)
AM_CONDITIONAL(HOST_AMD64, test x$HOST = xAMD64)
AM_CONDITIONAL(HOST_ARM, test x$HOST = xARM)
AM_CONDITIONAL(HOST_ARM64, test x$HOST = xARM64)
AM_CONDITIONAL([HOST_RISCV], [test x$HOST = xRISCV32 -o x$HOST = xRISCV64])
AM_CONDITIONAL([HOST_RISCV32], [test x$HOST = xRISCV32])
AM_CONDITIONAL([HOST_RISCV64], [test x$HOST = xRISCV64])
AM_CONDITIONAL(HOST_WASM, test x$HOST = xWASM)

AM_CONDITIONAL(CROSS_COMPILE, test "x$host" != "x$target")

AC_SUBST(LIBC)
AC_SUBST(INTL)
AC_SUBST(SQLITE)
AC_SUBST(SQLITE3)
AC_SUBST(ODBC)
AC_SUBST(X11)
AC_SUBST(GDKX11)
AC_SUBST(GTKX11)
AC_SUBST(XINERAMA)
AC_DEFINE_UNQUOTED(MONO_ARCHITECTURE,"$arch_target",[The architecture this is running on])
AC_SUBST(arch_target)

#This must always be defined when building the runtime
AC_DEFINE(MONO_INSIDE_RUNTIME,1, [Disable banned functions from being used by the runtime])

mono_build_root=`pwd`
AC_SUBST(mono_build_root)

AC_ARG_ENABLE(msvc_only,[  --enable-msvc-only    Build only with Visual Studio (msvc) (defaults to no). Disable gcc build.], enable_msvc_only=$enableval, enable_msvc_only=no)
if test x$host_win32 = xno; then
	if test x$enable_msvc_only = xyes; then
		echo "Error, --enable-msvc-only only supported for Windows builds."
		exit 1
	fi
fi
AM_CONDITIONAL(ENABLE_MSVC_ONLY, test x$enable_msvc_only = xyes)

AC_ARG_ENABLE(msvc,[  --enable-msvc    Build with Visual Studio (msvc) (defaults to no)], enable_msvc=$enableval, enable_msvc=no)
if test x$host_win32 = xno; then
	if test x$enable_msvc = xyes; then
		echo "Error, --enable-msvc only supported for Windows builds."
		exit 1
	fi
fi
if test x$enable_msvc_only = xyes; then
	enable_msvc=yes
fi
AM_CONDITIONAL(ENABLE_MSVC, test x$enable_msvc = xyes)

if test "x$enable_msvc" = "xyes"; then
	mono_msvc_build_dir='$(top_srcdir)'/msvc/build/sgen
	if test "x$host_cpu" = "xi686"; then
		mono_msvc_build_lib_dir='$(mono_msvc_build_dir)'/Win32/lib/Release
		mono_msvc_build_bin_dir='$(mono_msvc_build_dir)'/Win32/bin/Release
		mono_runtime=msvc/build/sgen/Win32/bin/Release/mono-sgen.exe
		mono_runtime_wrapper=$mono_runtime
	else
		mono_msvc_build_lib_dir='$(mono_msvc_build_dir)'/x64/lib/Release
		mono_msvc_build_bin_dir='$(mono_msvc_build_dir)'/x64/bin/Release
		mono_runtime=msvc/build/sgen/x64/bin/Release/mono-sgen.exe
		mono_runtime_wrapper=msvc/build/sgen/x64/bin/Release/mono-sgen-msvc.sh
	fi
	AC_SUBST(mono_msvc_build_dir)
	AC_SUBST(mono_msvc_build_lib_dir)
	AC_SUBST(mono_msvc_build_bin_dir)
else
	mono_runtime=mono/mini/mono
	mono_runtime_wrapper=$mono_runtime
fi

AC_SUBST(mono_runtime)
AC_SUBST(mono_runtime_wrapper)

CSC_LOCATION=`cd $srcdir && pwd`/external/roslyn-binaries/Microsoft.Net.Compilers/3.7.0/csc.exe
VBCS_LOCATION=`cd $srcdir && pwd`/external/roslyn-binaries/Microsoft.Net.Compilers/3.7.0/VBCSCompiler.exe

if test $csc_compiler = mcs; then
  CSC=$mcs_topdir/class/lib/build/mcs.exe
else
  CSC=$CSC_LOCATION
fi

AM_CONDITIONAL([CSC_IS_ROSLYN], [test x$csc_compiler != xmcs])

mono_cfg_root=$mono_build_root/runtime
if test x$host_win32 = xyes; then
  if test "x$cross_compiling" = "xno"; then
    mono_cfg_dir=`cygpath -w -a $mono_cfg_root`\\etc
    CSC=`cygpath -m -a $CSC`
    CSC_LOCATION=`cygpath -m -a $CSC_LOCATION`
    VBCS_LOCATION=`cygpath -m -a $VBCS_LOCATION`
  else
    mono_cfg_dir=`echo $mono_cfg_root | tr '/' '\\'`\\etc
  fi
else
  mono_cfg_dir=$mono_cfg_root/etc
fi

if test "x$platform_android" = "xyes"; then
  mono_cfg_dir=
fi

AC_SUBST(mono_cfg_dir)

AC_SUBST(CSC)

if test "x$with_core" != "xonly"; then

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
fi

# Top level Makefile.am subdirs
if test "x$with_core" = "xonly"; then
	MONO_SUBDIRS="llvm mono"
else
	if test "x$support_boehm" = "xyes" -a "x$libgc" = "xincluded"; then
		mono_subdirs_libgc=external/bdwgc
	fi

	if test x$enable_support_build != xno; then
		mono_subdirs_support=support
	fi

	if test x$cross_compiling = xno; then
		# Some tools might not build when cross-compiling
		mono_subdirs_tools=tools
	fi

	if test x$DISABLE_MCS_DOCS != xyes; then
		mono_subdirs_docs=docs
	fi

	if test x$with_ikvm_native = xyes; then
		mono_subdirs_ikvm_native=ikvm-native
	fi

	MONO_SUBDIRS="po $mono_subdirs_libgc llvm mono $mono_subdirs_ikvm_native $mono_subdirs_support data runtime scripts man samples $mono_subdirs_tools $mono_subdirs_docs msvc acceptance-tests"
	MONO_NOINST_SUBDIRS="$mono_subdirs_libgc"
fi

AC_SUBST(MONO_SUBDIRS)
AC_SUBST(MONO_NOINST_SUBDIRS)

AC_CONFIG_COMMANDS([quiet-libtool], [sed -e 's/echo "copying selected/# "copying selected/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool; sed -e 's/$ECHO "copying selected/# "copying selected/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool])
AC_CONFIG_COMMANDS([nolock-libtool], [sed -e 's/lock_old_archive_extraction=yes/lock_old_archive_extraction=no/g' < libtool > libtool.tmp && mv libtool.tmp libtool && chmod a+x libtool])
AC_CONFIG_COMMANDS([clean-llvm], [rm -f llvm/llvm_config.mk])

#
# Mono.Native Support
# -------------------
# Mono.Native is the new name for both System.Native and System.Security.Cryptography.Apple.
# It is built as a stand-alone shared library and not bundled with the runtime because we
# may need to build two different versions of it.
#
# Starting with macOS 10.12+ and iOS 10+, Apple introduced a new Unified API for some of the
# crypto primitives that we're using as part of System.Security.Cryptography.Apple.
#
# On Desktop, we can check at runtime whether the OS version is recent enough and switch
# implementation accordingly.  We build a single `libmono-native` shared library.
#
# However, on Mobile we cannot have any undefined symbols as this would break Bitcode.
#
# During the mobile build, we are called with `CFLAGS` containing an explicit minium version flag,
# which is eiter `-mmacosx-version-min=`, `-mios-simulator-version-min=` or `-miphoneos-version-min=`
# depending on platform.
#
# We build two versions of the shared library:
# - `libmono-native-compat` is built with whichever minimum version is passed to us via `CFLAGS`.
# - `libmono-native-unifed` is built with the minimum version set to macOS 10.12+ / iOS 10+.
#
# For testing purpuses, there is a function called `mono_native_get_platform_type ()`
# (see mono/native/mono-native-platform.c), which returns a `MonoNativePlatformType` enum value.
# There is also `Mono.MonoNativePlatform.GetPlatformType ()` (see mcs/class/corlib/Test/Mono/MonoNativePlatform.cs).
#
# This can be called by automated tests both to ensure that the library has been correctly installed and also
# to verify that it's the correct version of it.
#

AC_MSG_CHECKING([Mono.Native support])
sed_version_pattern='[[0-9]]\{1,2\}\(\.[[0-9]]\{1,2\}\)'
if test x$disable_mono_native = xyes; then
	mono_native=no
elif test x$target_osx = xyes; then
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CCASFLAGS=$CCASFLAGS
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS=$LDFLAGS

	mono_native=yes
	MONO_NATIVE_PLATFORM=macos
	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_MACOS"

	if test x$enable_gss = xyes; then
		MONO_NATIVE_LDFLAGS="$MONO_NATIVE_LDFLAGS -framework GSS"
	fi

	AC_MONO_APPLE_AVAILABLE(mono_native_compat, [whether we need the compatibility layer],
		[!(MAC_OS_X_VERSION_MIN_REQUIRED >= MAC_OS_X_VERSION_10_12)])

	if test x$mono_native_compat = xyes; then
		mono_native_compat=yes
		mono_native_text="Mac OSX (compat + unified v10.12)"
		MONO_NATIVE_COMPAT_CCASFLAGS=$MONO_NATIVE_CCASFLAGS
		MONO_NATIVE_COMPAT_CPPFLAGS=$MONO_NATIVE_CPPFLAGS
		MONO_NATIVE_COMPAT_CXXFLAGS=$MONO_NATIVE_CXXFLAGS
		MONO_NATIVE_COMPAT_CFLAGS=$MONO_NATIVE_CFLAGS
		MONO_NATIVE_COMPAT_LDFLAGS=$MONO_NATIVE_LDFLAGS

		sed_remove_mac_version_pattern="s/-mmacosx-version-min=$sed_version_pattern//g"
		MONO_NATIVE_UNIFIED_CCASFLAGS="`echo $CCASFLAGS | sed -e $sed_remove_mac_version_pattern` -mmacosx-version-min=10.12"
		MONO_NATIVE_UNIFIED_CPPFLAGS="`echo $CPPFLAGS | sed -e $sed_remove_mac_version_pattern` -mmacosx-version-min=10.12"
		MONO_NATIVE_UNIFIED_CXXFLAGS="`echo $CXXFLAGS | sed -e $sed_remove_mac_version_pattern` -mmacosx-version-min=10.12"
		MONO_NATIVE_UNIFIED_CFLAGS="`echo $CFLAGS | sed -e $sed_remove_mac_version_pattern` -mmacosx-version-min=10.12"
		MONO_NATIVE_UNIFIED_LDFLAGS="`echo $LDFLAGS | sed -e $sed_remove_mac_version_pattern` -mmacosx-version-min=10.12"
	else
		mono_native_compat=no
		mono_native_text="Mac OSX"
	fi
elif test x$mono_native_platform_ios = xyes; then
	AC_MONO_APPLE_TARGET(TARGET_OS_SIMULATOR, [mono_native_ios_sim=yes], [mono_native_ios_sim=no])

	AC_MONO_APPLE_TARGET(TARGET_OS_IOS, [
		if test x$mono_native_ios_sim = xyes; then
			mono_native_ios_target=ios-simulator
			mono_native_text="iOS Simulator"
			MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_IOS | MONO_NATIVE_PLATFORM_TYPE_IPHONE | MONO_NATIVE_PLATFORM_TYPE_SIMULATOR"
		else
			mono_native_ios_target=iphoneos
			mono_native_text="iOS Device"
			MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_IOS | MONO_NATIVE_PLATFORM_TYPE_IPHONE | MONO_NATIVE_PLATFORM_TYPE_DEVICE"
		fi
		mono_native_unified_version="10.0"
		mono_native_compat_check="__IPHONE_OS_VERSION_MIN_REQUIRED >= __IPHONE_10_0"
	], [
		AC_MONO_APPLE_TARGET(TARGET_OS_TV, [
			if test x$mono_native_ios_sim = xyes; then
				mono_native_ios_target=tvos-simulator
				mono_native_text="AppleTV Simulator"
				MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_IOS | MONO_NATIVE_PLATFORM_TYPE_TV | MONO_NATIVE_PLATFORM_TYPE_SIMULATOR"
			else
				mono_native_ios_target=tvos
				mono_native_text="AppleTV Device"
				MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_IOS | MONO_NATIVE_PLATFORM_TYPE_TV | MONO_NATIVE_PLATFORM_TYPE_DEVICE"
			fi
			mono_native_unified_version="10.0"
			mono_native_compat_check="__TV_OS_VERSION_MIN_REQUIRED >= __TVOS_10_0"
		], [
			AC_MONO_APPLE_TARGET(TARGET_OS_WATCH, [
				if test x$mono_native_ios_sim = xyes; then
					mono_native_ios_target=watchos-simulator
					mono_native_text="Apple Watch Simulator"
					MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_IOS | MONO_NATIVE_PLATFORM_TYPE_WATCH | MONO_NATIVE_PLATFORM_TYPE_SIMULATOR"
				else
					mono_native_ios_target=watchos
					mono_native_text="Apple Watch Device"
					MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_IOS | MONO_NATIVE_PLATFORM_TYPE_WATCH | MONO_NATIVE_PLATFORM_TYPE_DEVICE"
				fi
				mono_native_unified_version="5.0"
				mono_native_compat_check="__WATCH_OS_VERSION_MIN_REQUIRED >= __WATCHOS_5_0"
			], [
				AC_MSG_ERROR([Unknown iOS Target])
			])
		])
	])

	AC_MONO_APPLE_AVAILABLE(mono_native_compat, [whether we need the compatibility layer], [!($mono_native_compat_check)])

	if test x$with_bitcode = xyes; then
		mono_native_ldflags="-framework CoreFoundation -framework Foundation -no-undefined -fatal_warnings"
		MONO_NATIVE_LIBADD="../mini/$LIBMONO_LA"
	else
		mono_native_ldflags="-framework CoreFoundation -framework Foundation -fatal_warnings"
	fi

	if test x$enable_gss = xyes; then
		mono_native_ldflags="$mono_native_ldflags -framework GSS"
	fi

	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CCASFLAGS=$CCASFLAGS
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS="$LDFLAGS $mono_native_ldflags"

	if test x$mono_native_compat = xyes; then
		mono_native_text="$mono_native_text (compat + unified v$mono_native_unified_version)"
		MONO_NATIVE_COMPAT_CCASFLAGS=$MONO_NATIVE_CCASFLAGS
		MONO_NATIVE_COMPAT_CPPFLAGS=$MONO_NATIVE_CPPFLAGS
		MONO_NATIVE_COMPAT_CXXFLAGS=$MONO_NATIVE_CXXFLAGS
		MONO_NATIVE_COMPAT_CFLAGS=$MONO_NATIVE_CFLAGS
		MONO_NATIVE_COMPAT_LDFLAGS=$MONO_NATIVE_LDFLAGS

		sed_remove_ios_version_pattern="s/-m\(.*\)-version-min=$sed_version_pattern//g"
		MONO_NATIVE_UNIFIED_CCASFLAGS="`echo $CCASFLAGS | sed -e $sed_remove_ios_version_pattern` -m$mono_native_ios_target-version-min=$mono_native_unified_version"
		MONO_NATIVE_UNIFIED_CPPFLAGS="`echo $CPPFLAGS | sed -e $sed_remove_ios_version_pattern` -m$mono_native_ios_target-version-min=$mono_native_unified_version"
		MONO_NATIVE_UNIFIED_CXXFLAGS="`echo $CXXFLAGS | sed -e $sed_remove_ios_version_pattern` -m$mono_native_ios_target-version-min=$mono_native_unified_version"
		MONO_NATIVE_UNIFIED_CFLAGS="`echo $CFLAGS | sed -e $sed_remove_ios_version_pattern` -m$mono_native_ios_target-version-min=$mono_native_unified_version"
		MONO_NATIVE_UNIFIED_LDFLAGS="`echo $LDFLAGS $mono_native_ldflags | sed -e $sed_remove_ios_version_pattern` -m$mono_native_ios_target-version-min=$mono_native_unified_version"
	fi

	mono_native=yes
	MONO_NATIVE_PLATFORM=ios
elif test x$host_linux = xyes; then
	mono_native_text="Linux"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CCASFLAGS=$CCASFLAGS
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS=$LDFLAGS
	if test x$enable_gss = xyes; then
		MONO_NATIVE_LDFLAGS="$MONO_NATIVE_LDFLAGS -lgssapi_krb5"
	else
		AC_MSG_WARN([GSS is not enabled, it requires libkrb5-dev package])
	fi
	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=linux

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_LINUX"
elif test x$target_wasm = xyes; then
	mono_native_text="wasm"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CCASFLAGS=$CCASFLAGS
	# The system.native code uses _WASM_ to check for wasm
	MONO_NATIVE_CPPFLAGS="$CPPFLAGS -D_WASM_"
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS=$LDFLAGS

	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=linux

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_LINUX"
elif case $host_os in aix*|os400*) true;; *) false;; esac; then
	mono_native_text="AIX"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CCASFLAGS=$CCASFLAGS
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	dnl nosymbolic- is a hack in case -G for linking is used, as -G seems
	dnl to change the way unresolved symbols work on library load in an
	dnl incompatible manner. (as references to runtime functions are
	dnl resolved later) Default params otherwise are that, so seems OK.
	dnl Likewise, we don't seem to need to cover the entire runtime with
	dnl it either, on both AIX and PASE. -brtl from -G does seem to spew
	dnl Big Scary TOC Warnings (tm) from the linker, but it doesn't seem
	dnl problematic with gcc's -mminimal-toc.
	dnl ----
	dnl flock in AIX exists in libbsd (not the same as freedesktop.org
	dnl libbsd) which Mono.Native needs.
	dnl Because of the way that the library is built by default, unresolved
	dnl references are kept and resolved at runtime. Often, the dependency
	dnl chain means libbsd is loaded anyways, but not necessarily. It's
	dnl better to explicitly link it, even though it has it shadows libc's
	dnl ioctl with its own. (As for the other unresolved imports, those
	dnl should be provided by the Mono runtime loaded.)
	MONO_NATIVE_LDFLAGS="$LDFLAGS -lbsd -Wl,-bnosymbolic-"

	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=aix

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_AIX"
elif case $host_os in freebsd*) true;; *) false;; esac; then
	mono_native_text="FreeBSD"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS="$CFLAGS -I/usr/local/include"
	MONO_NATIVE_LDFLAGS=$LDFLAGS

	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=freebsd
	AC_MSG_CHECKING([Mono.Native support])
	AC_MSG_RESULT(freebsd)

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_FREEBSD"
elif case $host_os in netbsd*) true;; *) false;; esac; then
	mono_native_text="NetBSD"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS=$LDFLAGS

	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=netbsd
	AC_MSG_CHECKING([Mono.Native support])
	AC_MSG_RESULT(netbsd)

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_NETBSD"
elif case $host_os in haiku*) true;; *) false;; esac; then
	mono_native_text="Haiku"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS=$LDFLAGS

	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=haiku
	AC_MSG_CHECKING([Mono.Native support])
	AC_MSG_RESULT(haiku)

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_HAIKU"
elif test x$platform_android = xyes; then
	mono_native_text="Android"
	MONO_NATIVE_CC=$CC
	MONO_NATIVE_CXX=$CXX
	MONO_NATIVE_CPPFLAGS=$CPPFLAGS
	MONO_NATIVE_CXXFLAGS=$CXXFLAGS
	MONO_NATIVE_CFLAGS=$CFLAGS
	MONO_NATIVE_LDFLAGS=$LDFLAGS
	MONO_NATIVE_LIBADD="../mini/$LIBMONO_LA"

	mono_native=yes
	mono_native_compat=no
	MONO_NATIVE_PLATFORM=android

	MONO_NATIVE_PLATFORM_TYPE="MONO_NATIVE_PLATFORM_TYPE_ANDROID"
else
	mono_native=no
	mono_native_text="no"
fi

AC_MSG_RESULT($mono_native_text)

if test x$mono_native_compat = xyes; then
	MONO_NATIVE_LIBRARY_NAME=libmono-native-compat
else
	MONO_NATIVE_LIBRARY_NAME=libmono-native
fi

AC_SUBST(MONO_NATIVE_PLATFORM)
AC_SUBST(MONO_NATIVE_CC)
AC_SUBST(MONO_NATIVE_CXX)
AC_SUBST(MONO_NATIVE_CCASFLAGS)
AC_SUBST(MONO_NATIVE_COMPAT_CCASFLAGS)
AC_SUBST(MONO_NATIVE_UNIFIED_CCASFLAGS)
AC_SUBST(MONO_NATIVE_CPPFLAGS)
AC_SUBST(MONO_NATIVE_COMPAT_CPPFLAGS)
AC_SUBST(MONO_NATIVE_UNIFIED_CPPFLAGS)
AC_SUBST(MONO_NATIVE_CXXFLAGS)
AC_SUBST(MONO_NATIVE_COMPAT_CXXFLAGS)
AC_SUBST(MONO_NATIVE_UNIFIED_CXXFLAGS)
AC_SUBST(MONO_NATIVE_CFLAGS)
AC_SUBST(MONO_NATIVE_COMPAT_CFLAGS)
AC_SUBST(MONO_NATIVE_UNIFIED_CFLAGS)
AC_SUBST(MONO_NATIVE_LDFLAGS)
AC_SUBST(MONO_NATIVE_COMPAT_LDFLAGS)
AC_SUBST(MONO_NATIVE_UNIFIED_LDFLAGS)
AC_SUBST(MONO_NATIVE_LIBRARY_NAME)
AC_SUBST(MONO_NATIVE_LIBADD)

AM_CONDITIONAL(MONO_NATIVE, test x$mono_native = xyes)
AM_CONDITIONAL(MONO_NATIVE_COMPAT, test x$mono_native_compat = xyes)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_MACOS, test x$MONO_NATIVE_PLATFORM = xmacos)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_IOS, test x$MONO_NATIVE_PLATFORM = xios)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_LINUX, test x$MONO_NATIVE_PLATFORM = xlinux)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_AIX, test x$MONO_NATIVE_PLATFORM = xaix)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_ANDROID, test x$MONO_NATIVE_PLATFORM = xandroid)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_FREEBSD, test x$MONO_NATIVE_PLATFORM = xfreebsd)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_NETBSD, test x$MONO_NATIVE_PLATFORM = xnetbsd)
AM_CONDITIONAL(MONO_NATIVE_PLATFORM_HAIKU, test x$MONO_NATIVE_PLATFORM = xhaiku)

MONO_NATIVE_PLATFORM_TYPE_COMPAT="$MONO_NATIVE_PLATFORM_TYPE | MONO_NATIVE_PLATFORM_TYPE_COMPAT"
MONO_NATIVE_PLATFORM_TYPE_UNIFIED="$MONO_NATIVE_PLATFORM_TYPE | MONO_NATIVE_PLATFORM_TYPE_UNIFIED"
AC_SUBST(MONO_NATIVE_PLATFORM_TYPE)
AC_SUBST(MONO_NATIVE_PLATFORM_TYPE_COMPAT)
AC_SUBST(MONO_NATIVE_PLATFORM_TYPE_UNIFIED)

### C++ Linker selection
#
# We have to lie to autotools, sometimes.
#
# If we're building with llvm compiled into Mono, set MONO_CXXLD to CXX,
# otherwise to CC.  We want C linking even if we're compiling with C++ enabled
# because we do not want to link inthe C++ runtime library (-lstdc++ or -lc++),
# except if we're linking with LLVM which relies on it.
#
# Explanation of the hack:
#
# According to
# https://www.gnu.org/software/automake/manual/html_node/How-the-Linker-is-Chosen.html,
# automake chooses the linker that it used to compile a target based on the
# _SOURCES for that target.  If the target is comprised of only C code, it
# chooses the C linker (on Linux and OSX this is just gcc or clang called with
# linker arguments and a -o).  If the target _SOURCES include at least one C++ file, 
# then automake chooses the C++ linker (usually g++ or clang++ called with linker arguments and -o).
#
# The problem for Mono is that we don't want the C++ runtime library (libstdc++
# or libc++) linked in - even when Mono is written in C++, we take care not to
# require the runtime library.  As a result, we don't want g++ or clang++ to do
# the linking.  On the other hand if LLVM is enabled, then we _must_ use the
# C++ linker - LLVM doesn't have the same restrictions.
#
# So the trick is we set MONO_CXXLD here to $CXX or $CC and in
#  mono/mini/Makefile.am.in we have CCLD=$MONO_CXXLD and CXXLD=$MONO_CXXLD which bypasses
#  automake's autodetection and lets us use the linker that we want.
#
if test "x$enable_llvm_runtime" = "xyes"; then
	AC_SUBST(MONO_CXXLD, [$CXX])
	AC_SUBST(MONO_LIBTOOL_TAG, '--tag=CXX')
else
	AC_SUBST(MONO_CXXLD, [$CC])
	AC_SUBST(MONO_LIBTOOL_TAG, '')
fi

### Set -Werror options
#
# Anything involving -Werror must be done late because autoconf depends on compiling with warnings to be success.
#
if test x"$GCC" = xyes; then

	if test "x$with_jemalloc" != "xyes"; then

		# incompatible-pointer-types requires gcc circa 5.x

		ORIG_CFLAGS=$CFLAGS
		CFLAGS="$CFLAGS -Wincompatible-pointer-types -Werror"
		AC_MSG_CHECKING(for -Wincompatible-pointer-types option to gcc)
		AC_TRY_COMPILE([],[
		], [
			AC_MSG_RESULT(yes)
			CFLAGS="$ORIG_CFLAGS -Werror=incompatible-pointer-types"
		], [
			AC_MSG_RESULT(no)
			CFLAGS=$ORIG_CFLAGS
		])

		CFLAGS="$CFLAGS -Werror=return-type"
	fi

	# Implicit function declarations are not 64 bit safe
	# Do this late, since this causes lots of configure tests to fail
	CFLAGS="$CFLAGS -Werror-implicit-function-declaration"
	# jay has a lot of implicit declarations
	JAY_CFLAGS="-Wno-implicit-function-declaration"

	ORIG_CFLAGS=$CFLAGS
	CFLAGS="$CFLAGS -Wsometimes-uninitialized -Werror"
	AC_MSG_CHECKING(for -Wsometimes-uninitialized option to gcc)
	AC_TRY_COMPILE([],[
	], [
		AC_MSG_RESULT(yes)
		CFLAGS="$ORIG_CFLAGS -Werror=sometimes-uninitialized"
	], [
		AC_MSG_RESULT(no)
		CFLAGS=$ORIG_CFLAGS
	])
fi

# zlib/configure checks if this program compiles and if so
# defines _LARGEFILE64_SOURCE on the compiler command line (not config.h).
#
AC_MSG_CHECKING([for large file support])
AC_COMPILE_IFELSE(
	[AC_LANG_SOURCE([
		#include <sys/types.h>
		off64_t dummy = 0;
	])], [
		AC_MSG_RESULT(yes)
		# FIXME The ramifications of _LARGEFILE64_SOURCE are not understood.
		# ZLIB_CFLAGS="$ZLIB_CFLAGS -D_LARGEFILE64_SOURCE=1"
	], [
		AC_MSG_RESULT(no)
	])

# for icu shim
ICU_SHIM_PATH=.
if test x$with_core = xonly; then
	if test x$cross_compiling = xno; then
		AC_CHECK_FILE($srcdir/../libraries/Native/Unix/System.Globalization.Native/pal_icushim.h, [have_shim_globalization=yes], [have_shim_globalization=no])
	fi
	if test x$have_shim_globalization = xyes || test x$cross_compiling = xyes; then
		ICU_SHIM_PATH=../../../libraries/Native/Unix/System.Globalization.Native
		if test x$target_wasm = xyes && test x$with_static_icu = xyes; then
			ICU_CFLAGS="-DTARGET_UNIX -DU_DISABLE_RENAMING"
			have_sys_icu=yes
		elif test x$target_osx = xyes; then
			ORIG_CPPFLAGS=$CPPFLAGS
			# adding icu path to pkg_config_path
			PKG_CONFIG_PATH=$PKG_CONFIG_PATH:/usr/local/lib/pkgconfig:/usr/local/opt/icu4c/lib/pkgconfig
			export PKG_CONFIG_PATH
			CPPFLAGS="`pkg-config --cflags-only-I icu-uc`"
			AC_CHECK_LIB(icucore, ucol_open, LIBS=$LIBS,
				[AC_MSG_ERROR([Cannot find libicucore, skipping build for System.Globalization.Native. .NET globalization is not expected to function.])])
			AC_CHECK_HEADER(unicode/utypes.h, [have_sys_icu=yes], [have_sys_icu=no])
			if test x$have_sys_icu = xyes; then
				ICU_CFLAGS="$CPPFLAGS -DPALEXPORT="" -DOSX_ICU_LIBRARY_PATH=AS_ESCAPE(\"/usr/lib/libicucore.dylib\", '\"') -DTARGET_UNIX -DU_DISABLE_RENAMING -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option -Wno-deprecated-declarations"
			fi
			CPPFLAGS=$ORIG_CPPFLAGS
		elif test x$platform_android = xyes; then
			ICU_CFLAGS="-DPALEXPORT="" -DHAVE_UDAT_STANDALONE_SHORTER_WEEKDAYS -DHAVE_SET_MAX_VARIABLE -DTARGET_UNIX -DTARGET_ANDROID -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option"
			have_sys_icu=yes
		elif test x$host_linux = xyes; then
			AC_CHECK_LIB(icuuc, main, LIBS=$LIBS,
				[AC_MSG_ERROR([Cannot find libicuuc, try installing libicu-dev (or the appropriate package for your platform).])])
			AC_CHECK_LIB(icui18n, main, LIBS=$LIBS,
				[AC_MSG_ERROR([Cannot find libicui18n, try installing libicu-dev (or the appropriate package for your platform).])])
			AC_CHECK_HEADER(unicode/utypes.h, [have_sys_icu=yes], [have_sys_icu=no])
			if test x$have_sys_icu = xyes; then
				ICU_CFLAGS="-DPALEXPORT="" -DTARGET_UNIX -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option"
			fi
		elif test x$host_sunos = xyes; then
			ICU_CFLAGS="-DPALEXPORT="" -DTARGET_UNIX -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option"
			have_sys_icu=yes
		elif test x$host_wasm = xyes; then
			ICU_CFLAGS="-DPALEXPORT="" -DTARGET_UNIX -Wno-reserved-id-macro -Wno-documentation -Wno-documentation-unknown-command -Wno-switch-enum -Wno-covered-switch-default -Wno-covered-switch-default -Wno-extra-semi-stmt -Wno-unknown-warning-option"
			have_sys_icu=yes
		else
			GLOBALIZATION_SHIM_DEFINES="-DNO_GLOBALIZATION_SHIM"
		fi
		AC_SUBST(ICU_CFLAGS)
	else
		GLOBALIZATION_SHIM_DEFINES="-DNO_GLOBALIZATION_SHIM"
	fi
else
	GLOBALIZATION_SHIM_DEFINES="-DNO_GLOBALIZATION_SHIM"
fi

AC_SUBST(GLOBALIZATION_SHIM_DEFINES)
AC_SUBST(ICU_SHIM_PATH)
AM_CONDITIONAL(HAVE_SYS_ICU, test x$have_sys_icu = xyes)

AC_SUBST(CFLAGS)
AC_SUBST(CPPFLAGS)
AC_SUBST(LDFLAGS)
AC_SUBST(CCLDFLAGS)
AC_SUBST(ZLIB_CFLAGS)

# Update all submodules recursively to ensure everything is checked out
if test "x$with_core" != "xonly"; then
	(cd $srcdir && scripts/update_submodules.sh)
fi

AC_MONO_OUTPUT()

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
    echo "RUNTIME = $mono_build_root/runtime/mono-wrapper" >> $mcs_topdir/build/config.make
    echo "JAY_CFLAGS = $JAY_CFLAGS" >> $mcs_topdir/build/config.make
    echo "VERSION = $VERSION" >> $mcs_topdir/build/config.make

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
      echo "BUILD_PLATFORM = macos" >> $mcs_topdir/build/config.make
      echo "HOST_PLATFORM ?= macos" >> $mcs_topdir/build/config.make
      echo "XTEST_PLATFORM ?= osx" >> $mcs_topdir/build/config.make
    elif test x$host_win32 = xyes; then
      echo "BUILD_PLATFORM = win32" >> $mcs_topdir/build/config.make
      echo "HOST_PLATFORM ?= win32" >> $mcs_topdir/build/config.make
      echo "XTEST_PLATFORM ?= windows" >> $mcs_topdir/build/config.make
    elif test x$host_linux = xyes; then
      echo "BUILD_PLATFORM = linux" >> $mcs_topdir/build/config.make
      echo "HOST_PLATFORM ?= linux" >> $mcs_topdir/build/config.make
      echo "XTEST_PLATFORM ?= linux" >> $mcs_topdir/build/config.make
    else
      echo "BUILD_PLATFORM = unix" >> $mcs_topdir/build/config.make
      echo "HOST_PLATFORM ?= unix" >> $mcs_topdir/build/config.make
      echo "XTEST_PLATFORM ?= unix" >> $mcs_topdir/build/config.make
    fi

    echo "PLATFORM_AOT_PREFIX = $PLATFORM_AOT_PREFIX" >> $mcs_topdir/build/config.make
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

    echo "STANDALONE_CSC_LOCATION=$CSC_LOCATION" >> $srcdir/$mcsdir/build/config.make
    echo "SERVER_CSC_LOCATION?=$CSC_LOCATION" >> $srcdir/$mcsdir/build/config.make
    echo "VBCS_LOCATION?=$VBCS_LOCATION" >> $srcdir/$mcsdir/build/config.make

    if test $csc_compiler = mcs; then
      echo "MCS_MODE = 1" >> $srcdir/$mcsdir/build/config.make
    fi

    if test "x$AOT_BUILD_FLAGS" != "x" ; then
      echo "AOT_RUN_FLAGS=$AOT_RUN_FLAGS" >> $srcdir/$mcsdir/build/config.make
      echo "AOT_BUILD_ATTRS=$AOT_BUILD_ATTRS" >> $srcdir/$mcsdir/build/config.make

      if test "x$internal_llvm" != "xno" && test "x$enable_llvm_msvc_only" = "xno"; then
        echo "AOT_BUILD_FLAGS=$AOT_BUILD_FLAGS,$MONO_LLVM_PATH_OPTION" >> $srcdir/$mcsdir/build/config.make
      else
        echo "AOT_BUILD_FLAGS=$AOT_BUILD_FLAGS" >> $srcdir/$mcsdir/build/config.make
      fi
    fi

    if test "x$AOT_MODE" != "x" ; then
      echo "AOT_MODE=$AOT_MODE" >> $srcdir/$mcsdir/build/config.make
    fi

    if test "x$enable_btls" = "xyes"; then
      echo "HAVE_BTLS=1" >> $srcdir/$mcsdir/build/config.make
    fi

    if test "x$mono_native" = "xyes"; then
      echo "MONO_NATIVE_SUPPORTED=true" >> $srcdir/$mcsdir/build/config.make
    else
      echo "MONO_NATIVE_SUPPORTED=false" >> $srcdir/$mcsdir/build/config.make
    fi

    if test x$mono_native_compat = xyes; then
      echo "MONO_NATIVE_USING_COMPAT=true" >> $srcdir/$mcsdir/build/config.make
    else
      echo "MONO_NATIVE_USING_COMPAT=false" >> $srcdir/$mcsdir/build/config.make
    fi

    force_enable_compiler_server=no;

    AC_MSG_CHECKING([compiler server])
    AC_ARG_WITH(compiler-server, [  --with-compiler-server=yes,no,default,force      Enables or disables compiler server],[
       if test x$withval = xyes; then
           enable_compiler_server=yes;
       # force will bypass any automatic disables to allow you to test the compiler server
       #  in scenarios where it is disabled
       elif test x$withval = xforce; then
           enable_compiler_server=yes;
           force_enable_compiler_server=yes;
       elif test x$withval = xno; then
           enable_compiler_server=no;
       elif test x$withval = xdefault; then
           enable_compiler_server=yes;
       else
           AC_MSG_ERROR([You must supply one of "yes", "no", "default" or "force" to the --with-compiler-server option])
       fi
    ],[enable_compiler_server=yes])

    AC_MSG_RESULT($enable_compiler_server)

    if test x$enable_compiler_server = xyes; then
       if test x$force_enable_compiler_server = xyes; then
           AC_MSG_WARN([forcing compiler server to stay enabled])
       elif test x$host_darwin = xyes || test x$host_win32 = xyes; then
           AC_MSG_WARN([compiler server temporarily disabled on darwin and win32])
           enable_compiler_server=no;
       elif test x$csc_compiler = xmcs; then
           AC_MSG_WARN([mcs does not support the compiler server])
           enable_compiler_server=no;
       fi
    fi

    echo "MONO_NATIVE_PLATFORM_TYPE=$MONO_NATIVE_PLATFORM_TYPE" >> $srcdir/$mcsdir/build/config.make
    echo "MONO_NATIVE_PLATFORM=$MONO_NATIVE_PLATFORM" >> $srcdir/$mcsdir/build/config.make
    if test x$enable_compiler_server = xyes; then
      echo "ENABLE_COMPILER_SERVER?=1" >> $srcdir/$mcsdir/build/config.make
    else
      echo "ENABLE_COMPILER_SERVER?=0" >> $srcdir/$mcsdir/build/config.make
    fi
    echo "COMPILER_SERVER_PIPENAME?=monomake" >> $srcdir/$mcsdir/build/config.make

	if test x$enable_msvc_only = xyes; then
		echo "ENABLE_MSVC_ONLY=1" >> $srcdir/$mcsdir/build/config.make
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

thread_suspend_msg=
if test x$buildsgen = xyes; then
	if test x$enable_cooperative_suspend != xno; then
		thread_suspend_msg="Suspend:       Cooperative"
	elif test x$enable_hybrid_suspend != xno; then
		thread_suspend_msg="Suspend:       Hybrid"
	else
		thread_suspend_msg="Suspend:       Preemptive"
	fi
fi

if test "x$with_core" = "xonly"; then
  if test ! -e netcore/Makefile; then
    # Out of tree builds
    mkdir -p netcore
    echo "all:" > netcore/Makefile
  fi
  echo "VERSION = $VERSION" > netcore/config.make
  echo "RID = $RID" >> netcore/config.make
  echo "COREARCH = $COREARCH" >> netcore/config.make
  echo "CORETARGETS = $CORETARGETS" >> netcore/config.make
  echo "MONO_CORLIB_VERSION = $MONO_CORLIB_VERSION" >> netcore/config.make

  if test x$build_darwin = xyes; then
    echo "HOST_PLATFORM ?= macos" >> netcore/config.make
  elif test x$host_darwin = xyes; then
    echo "HOST_PLATFORM ?= macos" >> netcore/config.make
  elif test x$host_win32 = xyes; then
    echo "HOST_PLATFORM ?= win32" >> netcore/config.make
  elif test x$host_linux = xyes; then
    echo "HOST_PLATFORM ?= linux" >> netcore/config.make
  else
    echo "HOST_PLATFORM ?= unix" >> netcore/config.make
  fi
fi

	
echo "
	mcs source:    $mcsdir
	C# Compiler:   $csc_compiler
	CompilerServer:$enable_compiler_server

   Engine:
	Host:	       $host
	Target:	       $target
	GC:	       $gc_msg 
	$thread_suspend_msg
	TLS:           $with_tls
	SIGALTSTACK:   $with_sigaltstack
	Engine:        $jit_status
	BigArrays:     $enable_big_arrays
	DTrace:        $enable_dtrace
	LLVM Back End: $enable_llvm (built in-tree: $internal_llvm, assertions: $enable_llvm_asserts, msvc only: $enable_llvm_msvc_only)
	Spectre:       $spectre_mitigation_status
	Mono.Native:   $mono_native_text

   Libraries:
	.NET 4.x:        $with_profile4_x
	Xamarin.Android: $with_monodroid
	Xamarin.iOS:     $with_monotouch
	Xamarin.WatchOS: $with_monotouch_watch
	Xamarin.TVOS:    $with_monotouch_tv
	Xamarin.Mac:     $with_xammac
	Windows AOT:     $with_winaot
	Orbis:           $with_orbis
	Unreal:          $with_unreal
	WebAssembly:     $with_wasm
	Test profiles:   AOT Full ($with_testing_aot_full), AOT Hybrid ($with_testing_aot_hybrid), AOT Full Interp ($with_testing_aot_full_interp), Windows Full AOT Interp ($with_testing_winaot_interp)
	JNI support:     $jdk_headers_found
	libgdiplus:      $libgdiplus_msg
	zlib:            $zlib_msg
	BTLS:            $enable_btls$btls_platform_string
	jemalloc:        $with_jemalloc (always use: $with_jemalloc_always)
	crash reporting: $crash_reporting (private crashes: $with_crash_privacy)
	.NET Core:       $with_core
	$disabled
"
if test x$with_static_mono = xno -a "x$host_win32" != "xyes"; then
   AC_MSG_WARN([Turning off static Mono is a risk, you might run into unexpected bugs])
fi
