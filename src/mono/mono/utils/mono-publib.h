#ifndef __MONO_PUBLIB_H__
#define __MONO_PUBLIB_H__

/* 
 * Minimal general purpose header for use in public mono header files.
 * We can't include config.h, so we use compiler-specific preprocessor
 * directives where needed.
 */

#ifdef  __cplusplus
#define MONO_BEGIN_DECLS  extern "C" {
#define MONO_END_DECLS    }
#else
#define MONO_BEGIN_DECLS
#define MONO_END_DECLS
#endif

MONO_BEGIN_DECLS

/* VS 2010 and later have stdint.h */
#if defined(_MSC_VER) && _MSC_VER < 1600

typedef __int8			int8_t;
typedef unsigned __int8		uint8_t;
typedef __int16			int16_t;
typedef unsigned __int16	uint16_t;
typedef __int32			int32_t;
typedef unsigned __int32	uint32_t;
typedef __int64			int64_t;
typedef unsigned __int64	uint64_t;

#else

#include <stdint.h>

#endif /* end of compiler-specific stuff */

typedef int32_t		mono_bool;
typedef uint8_t		mono_byte;
typedef uint16_t	mono_unichar2;

typedef void	(*MonoFunc)	(void* data, void* user_data);
typedef void	(*MonoHFunc)	(void* key, void* value, void* user_data);

void mono_free (void *);

#define MONO_CONST_RETURN const

MONO_END_DECLS

#endif /* __MONO_PUBLIB_H__ */

