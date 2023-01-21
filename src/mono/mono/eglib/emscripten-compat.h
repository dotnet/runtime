#ifndef __EMSCRIPTEN_COMPAT_H
#define __EMSCRIPTEN_COMPAT_H

#ifdef EMSCRIPTEN
	#include <stdlib.h>
	#include <math.h>
	#include <stdarg.h>
	#include <malloc.h>

	#ifndef size_t
		typedef __SIZE_TYPE__ size_t;
	#endif

	#if defined __GNUC__ && defined __GNUC_MINOR__
		#define __GNUC_PREREQ(maj, min) \
			((__GNUC__ << 16) + __GNUC_MINOR__ >= ((maj) << 16) + (min))
	#else
		#define __GNUC_PREREQ(maj, min) 0
	#endif

	#if (defined __has_attribute \
     		&& (!defined __clang_minor__ \
         	|| 3 < __clang_major__ + (5 <= __clang_minor__)))
		#define __glibc_has_attribute(attr) __has_attribute (attr)
	#else
		#define __glibc_has_attribute(attr) 0
	#endif

	#ifndef __attribute_nonnull__
		#if __GNUC_PREREQ (3,3) || __glibc_has_attribute (__nonnull__)
			#define __attribute_nonnull__(params) __attribute__ ((__nonnull__ params))
		#else
			#define __attribute_nonnull__(params)
		#endif
	#endif

	#ifndef __nonnull
		#define __nonnull(params) __attribute_nonnull__ (params)
	#endif

	#ifndef __compar_fn_t
		typedef int (*__compar_fn_t) (const void *, const void *);
	#endif

	// #ifndef qsort
	// 	extern void qsort (void *__base, size_t __nmemb, size_t __size,
	// 		__compar_fn_t __compar) __nonnull ((1, 4));
	// #endif
#else
	#include <stdarg.h>
	#include <stdlib.h>
#endif

#endif /* emscripten-compat.h */
