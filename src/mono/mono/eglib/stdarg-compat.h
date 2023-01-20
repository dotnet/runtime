#ifndef _COMPAT_STDARG_H_COMPAT
#define _COMPAT_STDARG_H_COMPAT

#ifdef __cplusplus
extern "C" {
#endif

#define __va_copy(d,s) __builtin_va_copy(d,s)

#ifdef __cplusplus
}
#endif

#ifndef __STDARG_H
#define __STDARG_H

#ifndef _VA_LIST
typedef __builtin_va_list va_list;
#define _VA_LIST
#endif
#define va_start(ap, param) __builtin_va_start(ap, param)
#define va_end(ap)          __builtin_va_end(ap)
#define va_arg(ap, type)    __builtin_va_arg(ap, type)

/* GCC always defines __va_copy, but does not define va_copy unless in c99 mode
 * or -ansi is not specified, since it was not part of C90.
 */
#define __va_copy(d,s) __builtin_va_copy(d,s)

#if __STDC_VERSION__ >= 199901L || __cplusplus >= 201103L || !defined(__STRICT_ANSI__)
#define va_copy(dest, src)  __builtin_va_copy(dest, src)
#endif

#ifndef __GNUC_VA_LIST
#define __GNUC_VA_LIST 1
typedef __builtin_va_list __gnuc_va_list;
#endif

#endif /* __STDARG_H */

#ifndef _GLIBCXX_CSTDARG
#define _GLIBCXX_CSTDARG 1

// Adhere to section 17.4.1.2 clause 5 of ISO 14882:1998
#ifndef va_end
#define va_end(ap) va_end (ap)
#endif

#endif /* cstdarg */

#endif
