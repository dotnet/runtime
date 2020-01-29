
#include "utils/mono-compiler.h"

#if ENABLE_MONOTOUCH
#include "../../support/zlib-helper.c"
#elif ENABLE_MONODROID
#include "../../support/nl.c"
#include "../../support/zlib-helper.c"
#else
MONO_EMPTY_SOURCE_FILE(empty);
#endif
