/**
 * \file
 */

#include "config.h"

#include "mono-compiler.h"
#include "write-protect.h"

#if defined (HOST_MACCAT) && defined (__aarch64__)

/* our own declaration of pthread_jit_write_protect_np so that we don't see the __API_UNAVAILABLE__ header */
void
pthread_jit_write_protect_np (int enabled);


void
mono_jit_write_protect (int enabled)
{
        pthread_jit_write_protect_np (enabled);
}

#else

MONO_EMPTY_SOURCE_FILE (write_protect);

#endif
