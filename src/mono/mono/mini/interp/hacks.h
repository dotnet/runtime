/**
 * \file
 */

/* we need some special math function */
#ifndef _ISOC99_SOURCE
#define _ISOC99_SOURCE
#endif
#include <math.h>

/* which are not defined on FreeBSD */
#ifdef __GNUC__

#ifndef isunordered
#   define isunordered(u, v)                              \
    (__extension__                                        \
     ({ __typeof__(u) __u = (u); __typeof__(v) __v = (v); \
        isnan(__u) || isnan(__v); }))
#endif

#ifndef islessgreater
#   define islessgreater(x, u)                                    \
    (__extension__                                                \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y);         \
        !isunordered (__x, __y) && (__x < __y) || (__y < __x); }))
#endif

#ifndef islessequal
#   define islessequal(x, y)                              \
    (__extension__                                        \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y); \
        !isunordered(__x, __y) && __x <= __y; })) 
#endif

#ifndef isless
#   define isless(x, y)                                   \
    (__extension__                                        \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y); \
        !isunordered(__x, __y) && __x < __y; })) 
#endif

#ifndef isgreater
#   define isgreater(x, y)                                \
    (__extension__                                        \
     ({ __typeof__(x) __x = (x); __typeof__(y) __y = (y); \
        !isunordered(__x, __y) && __x > __y; }))
#endif

#else

/*  isunordered seems to crash on HPUX when built 64 bits
    so use generic implementation.
*/
#if defined(__hpux) && SIZEOF_VOID_P == 8
#undef isunordered
#undef islessgreater
#undef islessequal
#undef isless
#undef isgreater
#endif

#ifndef isunordered
#   define isunordered(u, v) (isnan(u) || isnan(v))
#endif

#ifndef islessgreater
#   define islessgreater(x, u) (!isunordered (x, y) && (x < y) || (y < x))
#endif

#ifndef islessequal
#   define islessequal(x, y) (!isunordered(x, y) && x <= y)
#endif

#ifndef isless
#   define isless(x, y) (!isunordered(x, y) && x < y) 
#endif

#ifndef isgreater
#   define isgreater(x, y) (!isunordered(x, y) && x > y)
#endif

#endif
