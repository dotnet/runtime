#include "config.h"

#ifdef ENABLE_EXTENSION_MODULE
#include "../../../mono-extensions/mono/mini/mini-cross-helpers.c"
#else

void mono_cross_helpers_run (void);

void
mono_cross_helpers_run (void)
{
}
#endif
