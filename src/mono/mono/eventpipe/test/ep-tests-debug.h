#ifndef __EVENTPIPE_TESTS_DEBUG_H__
#define __EVENTPIPE_TESTS_DEBUG_H__

#define _CRTDBG_MAP_ALLOC
#include <stdlib.h>
#include <crtdbg.h>

// Private function only used by EventPipe tests.
extern void ep_rt_mono_thread_exited (void);

#endif /* __EVENTPIPE_TESTS_DEBUG_H__ */
