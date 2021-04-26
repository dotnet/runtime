/**
 * \file
 */

#ifndef __MONO_WRITE_PROTECT_H__
#define __MONO_WRITE_PROTECT_H__

#include <mono/utils/mono-publib.h>

#if defined (HOST_MACCAT) && defined (__aarch64__)

void
mono_jit_write_protect (int enabled);

#endif /* defined (HOST_MACCAT) && defined (__aarch64__) */

#endif /* __MONO_WRITE_PROTECT_H__ */

