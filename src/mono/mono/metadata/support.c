
#include "utils/mono-compiler.h"

#if defined(ENABLE_MONOTOUCH) && !defined(ENABLE_NETCORE)
#include "../../support/zlib-helper.c"
#elif defined(ENABLE_MONODROID) && !defined(ENABLE_NETCORE)
#include "../../support/nl.c"
#include "../../support/zlib-helper.c"
#else
MONO_EMPTY_SOURCE_FILE(empty);
#endif
