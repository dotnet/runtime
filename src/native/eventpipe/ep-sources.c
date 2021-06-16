#include "ep-rt-config.h"

#ifdef ENABLE_PERFTRACING

// Option to include all internal source files into ep-sources.c.
#ifdef EP_INCLUDE_SOURCE_FILES
#ifndef EP_FORCE_INCLUDE_SOURCE_FILES
#define EP_FORCE_INCLUDE_SOURCE_FILES
#endif
#include "ep.c"
#include "ep-block.c"
#include "ep-buffer.c"
#include "ep-buffer-manager.c"
#include "ep-config.c"
#include "ep-event.c"
#include "ep-event-instance.c"
#include "ep-event-payload.c"
#include "ep-event-source.c"
#include "ep-file.c"
#include "ep-json-file.c"
#include "ep-metadata-generator.c"
#include "ep-provider.c"
#include "ep-sample-profiler.c"
#include "ep-session.c"
#include "ep-session-provider.c"
#include "ep-stack-contents.c"
#include "ep-stream.c"
#include "ep-thread.c"
#endif

#endif /* ENABLE_PERFTRACING */

extern const char quiet_linker_empty_file_warning_eventpipe_sources;
const char quiet_linker_empty_file_warning_eventpipe_sources = 0;
