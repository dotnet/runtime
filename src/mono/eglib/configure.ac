AC_INIT(README)
AC_CANONICAL_SYSTEM

AM_CONFIG_HEADER(config.h)
AM_INIT_AUTOMAKE(eglib,0.3)
AM_MAINTAINER_MODE

AC_PROG_CC
AM_PROG_LIBTOOL
CFLAGS="$CFLAGS -g -O0 -D_GNU_SOURCE"
GNUC_PRETTY=
GNUC_UNUSED=
BREAKPOINT="G_STMT_START { raise (SIGTRAP); } G_STMT_END"
if test x$GCC = xyes; then
   GNUC_UNUSED='__attribute__((__unused__))'
   GNUC_NORETURN='__attribute__((__noreturn__))'
   echo target_cpu=$target_cpu
   case $target_cpu in
     i*86) BREAKPOINT="G_STMT_START { __asm__ (\"int \$03\"); } G_STMT_END" ;;
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

AM_CONDITIONAL(CROSS_COMPILING, [test x$cross_compiling = xyes])
AC_C_BIGENDIAN([ORDER=G_BIG_ENDIAN],[ORDER=G_LITTLE_ENDIAN])

case $host in
*-*-msdos* | *-*-go32* | *-*-mingw32* | *-*-cygwin* | *-*-windows*)
    PATHSEP='\\'
    SEARCHSEP=';'
    OS="WIN32"
    ;;
*)
    PATHSEP='/'
    SEARCHSEP=':'
    OS="UNIX"
    ;;
esac

case $target in
arm*-darwin*)
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

AC_SUBST(ORDER)
AC_SUBST(CFLAGS)
AC_SUBST(PATHSEP)
AC_SUBST(SEARCHSEP)
AC_SUBST(OS)
AM_CONDITIONAL(HOST_WIN32, test x$OS = xWIN32)
AM_CONDITIONAL(TARGET_WIN32, test x$OS = xWIN32)

AC_CHECK_SIZEOF(int)
AC_CHECK_SIZEOF(void *)
AC_CHECK_FUNCS(strndup strlcpy getpwent_r strtok_r rewinddir vasprintf)
AC_CHECK_LIB(iconv, iconv_open, LIBS="$LIBS -liconv")
AC_CHECK_LIB(iconv, libiconv_open, LIBS="$LIBS -liconv")
AC_SEARCH_LIBS(sqrtf, m)
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

AC_CHECK_HEADERS(getopt.h sys/time.h sys/wait.h pwd.h langinfo.h iconv.h)

if test $ac_cv_sizeof_void_p != $ac_cv_sizeof_int; then
   GPOINTER_TO_INT="((gint)(long) (ptr))"
   GPOINTER_TO_UINT="((guint)(long) (ptr))"
   GINT_TO_POINTER="((gpointer)(glong) (v))"
   GUINT_TO_POINTER="((gpointer)(gulong) (v))"
   GSIZE="long"
   GSIZE_FORMAT='"lu"'
   G_GUINT64_FORMAT='"lu"'
else
   GPOINTER_TO_INT="((gint) (ptr))"
   GPOINTER_TO_UINT="((guint) (ptr))"
   GINT_TO_POINTER="((gpointer) (v))"
   GUINT_TO_POINTER="((gpointer) (v))"
   GSIZE="int"
   GSIZE_FORMAT='"u"'
   G_GUINT64_FORMAT='"llu"'
fi

AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
	AC_MSG_ERROR([You need to install pkg-config])
fi

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

dnl
dnl No, you can not replace the call below with a pkg.m4 macro
dnl
if $PKG_CONFIG --atleast-version=$GLIB_REQUIRED_VERSION glib-2.0; then
    have_glib=true;
else
    have_glib=false;
fi
AM_CONDITIONAL(HAVE_GLIB, $have_glib)

AC_SUBST(GPOINTER_TO_INT)
AC_SUBST(GPOINTER_TO_UINT)
AC_SUBST(GINT_TO_POINTER)
AC_SUBST(GUINT_TO_POINTER)
AC_SUBST(GSIZE)
AC_SUBST(GSIZE_FORMAT)
AC_SUBST(G_GUINT64_FORMAT)

AC_OUTPUT([
Makefile
src/Makefile
src/eglib-config.h
test/Makefile
])
