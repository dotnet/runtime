#
# Copyright 2012 Xamarin Inc
#
AC_INIT(eglib, [0.3],
        [http://bugzilla.xamarin.com/enter_bug.cgi?classification=Mono])

AC_CONFIG_SRCDIR([README])
AC_CONFIG_MACRO_DIR([m4])
AC_CANONICAL_SYSTEM
AC_CANONICAL_HOST

AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar no-dist-gzip foreign])
AC_CONFIG_HEADERS([config.h])
AM_MAINTAINER_MODE

m4_ifdef([AM_SILENT_RULES], [AM_SILENT_RULES([yes])])

AC_PROG_CC
AM_PROG_LIBTOOL
# Use dolt (http://dolt.freedesktop.org/) instead of libtool for building.
DOLT
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

# We use a separate variable to pass down CPPFLAGS and CFLAGS from the main mono 
# configure, because of autoconf brokeness
if test "x$CPPFLAGS_FOR_EGLIB" != "x"; then
   CPPFLAGS=$CPPFLAGS_FOR_EGLIB
fi
if test "x$CFLAGS_FOR_EGLIB" != "x"; then
   CFLAGS=$CFLAGS_FOR_EGLIB
fi

CFLAGS="$CFLAGS -g -D_GNU_SOURCE"

AM_CONDITIONAL(CROSS_COMPILING, [test x$cross_compiling = xyes])
AC_C_BIGENDIAN([ORDER=G_BIG_ENDIAN],[ORDER=G_LITTLE_ENDIAN])

platform_darwin=no
platform_android=no

case $host in
*-*-msdos* | *-*-go32* | *-*-mingw32* | *-*-cygwin* | *-*-windows*)
    PATHSEP='\\'
    SEARCHSEP=';'
    OS="WIN32"
    PIDTYPE='void *'
    ;;
*-*darwin*)
    platform_darwin=yes
    PATHSEP='/'
    SEARCHSEP=':'
    OS="UNIX"
    PIDTYPE='int'
    ;;
*-*-linux-android*)
    platform_android=yes
	AC_DEFINE(PLATFORM_ANDROID,1,[Targeting the Android platform])
    PATHSEP='/'
    SEARCHSEP=':'
    OS="UNIX"
    PIDTYPE='int'
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
arm*-darwin*|aarch64*-*)
    CFLAGS="$CFLAGS -U_FORTIFY_SOURCE"
    ;;
i*86-*-darwin*)
    ORDER=G_LITTLE_ENDIAN
    ;;
*-*-openbsd*)
    CFLAGS="$CFLAGS -pthread"
    LDFLAGS="$LDFLAGS -pthread"
    ;;
esac

target_osx=no
target_ios=no

if test "x$platform_darwin" = "xyes"; then
	AC_TRY_COMPILE([#include "TargetConditionals.h"],[
	#if TARGET_IPHONE_SIMULATOR == 1 || TARGET_OS_IPHONE == 1
	#error fail this for ios
	#endif
	return 0;
	], [
		AC_DEFINE(TARGET_OSX,1,[The JIT/AOT targets OSX])
		target_osx=yes
	], [
		AC_DEFINE(TARGET_IOS,1,[The JIT/AOT targets iOS])
		target_ios=yes
	])
   AC_DEFINE(TARGET_MACH,1,[The JIT/AOT targets Apple platforms])
fi

AC_SUBST(ORDER)
AC_SUBST(CFLAGS)
AC_SUBST(PATHSEP)
AC_SUBST(SEARCHSEP)
AC_SUBST(OS)
AC_SUBST(PIDTYPE)
AM_CONDITIONAL(HOST_WIN32, test x$OS = xWIN32)
AM_CONDITIONAL(TARGET_WIN32, test x$OS = xWIN32)
AM_CONDITIONAL(PLATFORM_DARWIN, test x$platform_darwin = xyes)
AM_CONDITIONAL(PLATFORM_ANDROID, test x$platform_android = xyes)

AC_CHECK_SIZEOF(int)
AC_CHECK_SIZEOF(void *)
AC_CHECK_SIZEOF(long)
AC_CHECK_SIZEOF(long long)
AC_CHECK_FUNCS(strlcpy stpcpy strtok_r rewinddir vasprintf)
AC_CHECK_FUNCS(getrlimit)

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

AM_CONDITIONAL(NEED_VASPRINTF, test x$have_vasprintf = x )
AM_ICONV()
AC_SEARCH_LIBS(sqrtf, m)

# nanosleep may not be part of libc, also search it in other libraries
AC_SEARCH_LIBS(nanosleep, rt)

AC_SEARCH_LIBS(dlopen, dl)
old_ldflags="${LDFLAGS}"
LDFLAGS="${LDFLAGS} -Wl,-export-dynamic"
AC_TRY_LINK(, [int i;], found_export_dynamic=yes, found_export_dynamic=no)
if test $found_export_dynamic = no; then
	LDFLAGS="${old_ldflags}"
fi

AC_MSG_CHECKING(for varargs macros)
AC_TRY_COMPILE([],[
int foo (int i, int j);
#define bar(...) foo (1, __VA_ARGS__)
void main () {
	 bar (2);
}
],have_iso_varargs=yes,have_iso_varargs=no)
AC_MSG_RESULT($have_iso_varargs)
G_HAVE_ISO_VARARGS=0
if test "x$have_iso_varargs" = "xyes"; then
   G_HAVE_ISO_VARARGS=1
fi
AC_SUBST(G_HAVE_ISO_VARARGS)

AC_CHECK_HEADERS(getopt.h sys/select.h sys/time.h sys/wait.h pwd.h langinfo.h iconv.h localcharset.h sys/types.h sys/resource.h)
AC_CHECK_HEADER(alloca.h, [HAVE_ALLOCA_H=1], [HAVE_ALLOCA_H=0])
AC_SUBST(HAVE_ALLOCA_H)

if test $ac_cv_sizeof_void_p = $ac_cv_sizeof_int; then
   GPOINTER_TO_INT="((gint) (ptr))"
   GPOINTER_TO_UINT="((guint) (ptr))"
   GINT_TO_POINTER="((gpointer) (v))"
   GUINT_TO_POINTER="((gpointer) (v))"
   GSIZE="int"
   GSIZE_FORMAT='"u"'
   G_GUINT64_FORMAT='"llu"'
   G_GINT64_FORMAT='"lld"'
   G_GUINT32_FORMAT='"lu"'
   G_GINT32_FORMAT='"ld"'
elif test $ac_cv_sizeof_void_p = $ac_cv_sizeof_long; then
   GPOINTER_TO_INT="((gint)(long) (ptr))"
   GPOINTER_TO_UINT="((guint)(long) (ptr))"
   GINT_TO_POINTER="((gpointer)(glong) (v))"
   GUINT_TO_POINTER="((gpointer)(gulong) (v))"
   GSIZE="long"
   GSIZE_FORMAT='"lu"'
   G_GUINT64_FORMAT='"lu"'
   G_GINT64_FORMAT='"ld"'
   G_GUINT32_FORMAT='"u"'
   G_GINT32_FORMAT='"d"'
elif test $ac_cv_sizeof_void_p = $ac_cv_sizeof_long_long; then
   GPOINTER_TO_INT="((gint)(long long) (ptr))"
   GPOINTER_TO_UINT="((guint)(unsigned long long) (ptr))"
   GINT_TO_POINTER="((gpointer)(long long) (v))"
   GUINT_TO_POINTER="((gpointer)(unsigned long long) (v))"
   GSIZE="long long"
   GSIZE_FORMAT='"I64u"'
   G_GUINT64_FORMAT='"I64u"'
   G_GINT64_FORMAT='"I64i"'
   G_GUINT32_FORMAT='"I32u"'
   G_GINT32_FORMAT='"I32i"'
else
   AC_MSG_ERROR([unsupported pointer size])
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

AC_SUBST(GPOINTER_TO_INT)
AC_SUBST(GPOINTER_TO_UINT)
AC_SUBST(GINT_TO_POINTER)
AC_SUBST(GUINT_TO_POINTER)
AC_SUBST(GSIZE)
AC_SUBST(GSIZE_FORMAT)
AC_SUBST(G_GUINT64_FORMAT)
AC_SUBST(G_GINT64_FORMAT)
AC_SUBST(G_GUINT32_FORMAT)
AC_SUBST(G_GINT32_FORMAT)

AC_OUTPUT([
Makefile
m4/Makefile
src/Makefile
src/eglib-config.h
test/Makefile
])
