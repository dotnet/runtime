/**
 * \file
 */

#include "config.h"

#include "mono-compiler.h"
#include "write-protect.h"

#if (defined(HOST_IOS) || defined(HOST_TVOS)) && defined (HOST_DARWIN_SIMULATOR) && defined (HOST_ARM64)

/* our own declaration of pthread_jit_write_protect_np so that we don't see the __API_UNAVAILABLE__ header */
void
pthread_jit_write_protect_np (int enabled);


void
mono_jit_write_protect (int enabled)
{
        pthread_jit_write_protect_np (enabled);
}

#else /* (defined(HOST_IOS) || defined(HOST_TVOS)) && defined (HOST_DARWIN_SIMULATOR) && defined (HOST_ARM64) */

MONO_EMPTY_SOURCE_FILE (write_protect);

#endif
