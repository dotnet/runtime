#include <config.h>

#ifdef ENABLE_PERFTRACING
#include "ds-rt-config.h"

// Option to include all internal source files into ds.c.
#ifdef DS_INCLUDE_SOURCE_FILES
#define DS_FORCE_INCLUDE_SOURCE_FILES
#include "ds-server.c"
#include "ds-eventpipe-protocol.c"
#include "ds-dump-protocol.c"
#include "ds-ipc.c"
#ifdef HOST_WIN32
#include "ds-ipc-win32.c"
#else
#include "ds-ipc-posix.c"
#endif
#include "ds-process-protocol.c"
#include "ds-profiler-protocol.c"
#include "ds-protocol.c"
#endif

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_diagnostics;
const char quiet_linker_empty_file_warning_diagnostics = 0;
