AC_DEFUN([AC_COMPILE_CHECK_SIZEOF],
[changequote(<<, >>)dnl
dnl The name to #define.
define(<<AC_TYPE_NAME>>, translit(sizeof_$1, [a-z *], [A-Z_P]))dnl
dnl The cache variable name.
define(<<AC_CV_NAME>>, translit(ac_cv_sizeof_$1, [ *], [_p]))dnl
changequote([, ])dnl
AC_MSG_CHECKING(size of $1)
AC_CACHE_VAL(AC_CV_NAME,
[for ac_size in 4 8 1 2 16 12 $2 ; do # List sizes in rough order of prevalence.
  AC_TRY_COMPILE([#include "confdefs.h"
#include <sys/types.h>
$2
], [switch (0) case 0: case (sizeof ($1) == $ac_size):;], AC_CV_NAME=$ac_size)
  if test x$AC_CV_NAME != x ; then break; fi
done
])
if test x$AC_CV_NAME = x ; then
  AC_MSG_ERROR([cannot determine a size for $1])
fi
AC_MSG_RESULT($AC_CV_NAME)
AC_DEFINE_UNQUOTED(AC_TYPE_NAME, $AC_CV_NAME, [The number of bytes in type $1])
undefine([AC_TYPE_NAME])dnl
undefine([AC_CV_NAME])dnl
])


AC_DEFUN([AC_C_BIGENDIAN_CROSS],
[AC_CACHE_CHECK(whether byte ordering is bigendian, ac_cv_c_bigendian,
[ac_cv_c_bigendian=unknown
# See if sys/param.h defines the BYTE_ORDER macro.
AC_TRY_COMPILE([#include <sys/types.h>
#include <sys/param.h>], [
#if !BYTE_ORDER || !BIG_ENDIAN || !LITTLE_ENDIAN
 bogus endian macros
#endif], [# It does; now see whether it defined to BIG_ENDIAN or not.
AC_TRY_COMPILE([#include <sys/types.h>
#include <sys/param.h>], [
#if BYTE_ORDER != BIG_ENDIAN
 not big endian
#endif], ac_cv_c_bigendian=yes, ac_cv_c_bigendian=no)])
if test $ac_cv_c_bigendian = unknown; then
AC_TRY_RUN([main () {
  /* Are we little or big endian?  From Harbison&Steele.  */
  union
  {
    long l;
    char c[sizeof (long)];
  } u;
  u.l = 1;
  exit (u.c[sizeof (long) - 1] == 1);
}], ac_cv_c_bigendian=no, ac_cv_c_bigendian=yes,
[ echo $ac_n "cross-compiling... " 2>&AC_FD_MSG ])
fi])
if test $ac_cv_c_bigendian = unknown; then
AC_MSG_CHECKING(to probe for byte ordering)
[
cat >conftest.c <<EOF
short ascii_mm[] = { 0x4249, 0x4765, 0x6E44, 0x6961, 0x6E53, 0x7953, 0 };
short ascii_ii[] = { 0x694C, 0x5454, 0x656C, 0x6E45, 0x6944, 0x6E61, 0 };
void _ascii() { char* s = (char*) ascii_mm; s = (char*) ascii_ii; }
short ebcdic_ii[] = { 0x89D3, 0xE3E3, 0x8593, 0x95C5, 0x89C4, 0x9581, 0 };
short ebcdic_mm[] = { 0xC2C9, 0xC785, 0x95C4, 0x8981, 0x95E2, 0xA8E2, 0 };
void _ebcdic() { char* s = (char*) ebcdic_mm; s = (char*) ebcdic_ii; }
int main() { _ascii (); _ebcdic (); return 0; }
EOF
] if test -f conftest.c ; then
     if ${CC-cc} ${CFLAGS} conftest.c -o conftest.o && test -f conftest.o ; then
        if test `grep -l BIGenDianSyS conftest.o` ; then
           echo $ac_n ' big endian probe OK, ' 1>&AC_FD_MSG
           ac_cv_c_bigendian=yes
        fi
        if test `grep -l LiTTleEnDian conftest.o` ; then
           echo $ac_n ' little endian probe OK, ' 1>&AC_FD_MSG
           if test $ac_cv_c_bigendian = yes ; then
            ac_cv_c_bigendian=unknown;
           else
            ac_cv_c_bigendian=no
           fi
        fi
        echo $ac_n 'guessing bigendian ...  ' >&AC_FD_MSG
     fi
  fi
AC_MSG_RESULT($ac_cv_c_bigendian)
fi
if test $ac_cv_c_bigendian = yes; then
  AC_DEFINE(WORDS_BIGENDIAN, 1, [whether byteorder is bigendian])
  BYTEORDER=4321
else
  BYTEORDER=1234
fi
AC_DEFINE_UNQUOTED(BYTEORDER, $BYTEORDER, [1234 = LIL_ENDIAN, 4321 = BIGENDIAN])
if test $ac_cv_c_bigendian = unknown; then
  AC_MSG_ERROR(unknown endianess - sorry, please pre-set ac_cv_c_bigendian)
fi
])

