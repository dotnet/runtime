#ifndef _ZBUILD_H
#define _ZBUILD_H

/* This has to be first include that defines any types */
#if defined(_MSC_VER)
#  if defined(_WIN64)
    typedef __int64 ssize_t;
#  else
    typedef long ssize_t;
#  endif
#endif

#if defined(ZLIB_COMPAT)
#  define PREFIX(x) x
#  define PREFIX2(x) ZLIB_ ## x
#  define PREFIX3(x) z_ ## x
#  define PREFIX4(x) x ## 64
#  define zVersion zlibVersion
#  define z_size_t unsigned long
#else
#  define PREFIX(x) zng_ ## x
#  define PREFIX2(x) ZLIBNG_ ## x
#  define PREFIX3(x) zng_ ## x
#  define PREFIX4(x) zng_ ## x
#  define zVersion zlibng_version
#  define z_size_t size_t
#endif

/* Minimum of a and b. */
#define MIN(a, b) ((a) > (b) ? (b) : (a))
/* Maximum of a and b. */
#define MAX(a, b) ((a) < (b) ? (b) : (a))
/* Ignore unused variable warning */
#define Z_UNUSED(var) (void)(var)

#endif
