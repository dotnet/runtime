#include <config.h>

#define MONO 1
/* clock_gettime () is found by configure on Apple builds, but its only present from ios 10, macos 10.12, tvos 10 and watchos 3 */
#if defined (TARGET_WASM) || defined (TARGET_IOS) || defined (TARGET_OSX) || defined (TARGET_WATCHOS) || defined (TARGET_TVOS)
#undef HAVE_CLOCK_MONOTONIC
#undef HAVE_CLOCK_MONOTONIC_COARSE
#endif

#ifdef TARGET_ANDROID
/* arc4random_buf() is not available even when configure seems to find it */
#undef HAVE_ARC4RANDOM_BUF
#endif
