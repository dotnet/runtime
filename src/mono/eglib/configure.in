AC_INIT(README)
AC_CANONICAL_SYSTEM

AM_CONFIG_HEADER(config.h)
AM_INIT_AUTOMAKE(eglib,0.1)
AM_MAINTAINER_MODE

AC_PROG_CC
AM_PROG_LIBTOOL
CFLAGS='-g -O0'
AC_SUBST(CFLAGS)

GNUC_PRETTY=
GNUC_UNUSED=
BREAKPOINT=
if test x$GCC = xyes; then
   GNUC_PRETTY=__PRETTY_FUNCTION__
   GNUC_UNUSED='__attribute__((__unused__))'
   GNUC_NORETURN='__attribute__((__noreturn__))'
   case $target in
     i[3456]86-*)
       BREAKPOINT="G_STMT_START { __asm__ ("int $03"); } G_STMT_END"
     ;;
   esac
fi
AC_SUBST(GNUC_PRETTY)
AC_SUBST(GNUC_UNUSED)
AC_SUBST(GNUC_NORETURN)
AC_SUBST(BREAKPOINT)

AC_C_BIGENDIAN([ORDER=G_BIG_ENDIAN],[ORDER=G_LITTLE_ENDIAN])
AC_SUBST(ORDER)

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

AC_SUBST(PATHSEP)
AC_SUBST(SEARCHSEP)
AC_SUBST(OS)

AC_CHECK_SIZEOF(int)
AC_CHECK_SIZEOF(void *)

if test $ac_cv_sizeof_void_p != $ac_cv_sizeof_int; then
   GPOINTER_TO_INT="((gint)(long) (ptr))"
   GPOINTER_TO_UINT="((guint)(long) (ptr))"
   GINT_TO_POINTER="((gpointer)(glong) (v))"
   GUINT_TO_POINTER="((gpointer)(gulong) (v))"
   GSIZE="long"
   GSIZE_FORMAT='"lu"'
else
   GPOINTER_TO_INT="((gint) (ptr))"
   GPOINTER_TO_UINT="((guint) (ptr))"
   GINT_TO_POINTER="((gpointer) (v))"
   GUINT_TO_POINTER="((gpointer) (v))"
   GSIZE="int"
   GSIZE_FORMAT='"u"'
fi
AC_SUBST(GPOINTER_TO_INT)
AC_SUBST(GPOINTER_TO_UINT)
AC_SUBST(GINT_TO_POINTER)
AC_SUBST(GUINT_TO_POINTER)
AC_SUBST(GSIZE)
AC_SUBST(GSIZE_FORMAT)

AC_OUTPUT([
Makefile
src/Makefile
src/eglib-config.h
test/Makefile
])