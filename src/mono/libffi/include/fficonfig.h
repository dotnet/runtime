/* fficonfig.h.  Generated automatically by configure.  */
/* fficonfig.h.in.  Generated automatically from configure.in by autoheader.  */

/* Define if using alloca.c.  */
/* #undef C_ALLOCA */

/* Define to one of _getb67, GETB67, getb67 for Cray-2 and Cray-YMP systems.
   This function is required for alloca.c support on those systems.  */
/* #undef CRAY_STACKSEG_END */

/* Define if you have alloca, as a function or macro.  */
#define HAVE_ALLOCA 1

/* Define if you have <alloca.h> and it should be used (not on Ultrix).  */
#define HAVE_ALLOCA_H 1

/* If using the C implementation of alloca, define if you know the
   direction of stack growth for your system; otherwise it will be
   automatically deduced at run-time.
 STACK_DIRECTION > 0 => grows toward higher addresses
 STACK_DIRECTION < 0 => grows toward lower addresses
 STACK_DIRECTION = 0 => direction of growth unknown
 */
/* #undef STACK_DIRECTION */

/* Define if you have the ANSI C header files.  */
#define STDC_HEADERS 1

/* Define this if you want extra debugging */
/* #undef FFI_DEBUG */

/* Define this if you are using Purify and want to suppress 
   spurious messages. */
/* #undef USING_PURIFY */

/* Define this is you do not want support for aggregate types.  */
/* #undef FFI_NO_STRUCTS */

/* Define this is you do not want support for the raw API.  */
/* #undef FFI_NO_RAW_API */

/* Define if you have the memcpy function.  */
#define HAVE_MEMCPY 1

/* The number of bytes in type short */
#define SIZEOF_SHORT 2

/* The number of bytes in type int */
#define SIZEOF_INT 4

/* The number of bytes in type long */
#define SIZEOF_LONG 4

/* The number of bytes in type long long */
#define SIZEOF_LONG_LONG 8

/* The number of bytes in type float */
#define SIZEOF_FLOAT 4

/* The number of bytes in type double */
#define SIZEOF_DOUBLE 8

/* The number of bytes in type long double */
#define SIZEOF_LONG_DOUBLE 12

/* The number of bytes in type void * */
#define SIZEOF_VOID_P 4

/* whether byteorder is bigendian */
/* #undef WORDS_BIGENDIAN */

/* 1234 = LIL_ENDIAN, 4321 = BIGENDIAN */
#define BYTEORDER 1234

