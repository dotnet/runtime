#ifndef __EVENTPIPE_RT_CONFIG_H__
#define __EVENTPIPE_RT_CONFIG_H__

#include "ep-shared-config.h"

#ifndef FEATURE_CORECLR

#include <config.h>

// EventPipe runtime implementation.
#define EP_RT_H <mono/eventpipe/ep-rt-mono.h>
#define EP_RT_TYPES_H <mono/eventpipe/ep-rt-types-mono.h>
#define EP_RT_CONFIG_H <mono/eventpipe/ep-rt-config-mono.h>

// DiagnosticServer runtime implementation.
#define DS_RT_H <mono/eventpipe/ds-rt-mono.h>
#define DS_RT_TYPES_H <mono/eventpipe/ds-rt-types-mono.h>

#else /* !FEATURE_CORECLR */

#ifndef EP_NO_RT_DEPENDENCY
#include "common.h"
#endif

#if defined(FEATURE_PERFTRACING)
#define ENABLE_PERFTRACING
#endif

#ifdef TARGET_WINDOWS
#define HOST_WIN32
#endif

#if defined(FEATURE_PERFTRACING) && defined(FEATURE_PROFAPI_ATTACH_DETACH) && defined(DACCESS_COMPILE)
#undef FEATURE_PROFAPI_ATTACH_DETACH
#endif

// EventPipe runtime implementation.
#define EP_RT_H "ep-rt-coreclr.h"
#define EP_RT_TYPES_H "ep-rt-types-coreclr.h"
#define EP_RT_CONFIG_H "ep-rt-config-coreclr.h"

// DiagnosticServer runtime implementation.
#define DS_RT_H "ds-rt-coreclr.h"
#define DS_RT_TYPES_H "ds-rt-types-coreclr.h"

#endif

#ifndef EP_NO_RT_DEPENDENCY
#include EP_RT_CONFIG_H
#endif

#define EP_INLINE_GETTER_SETTER

#ifdef EP_INLINE_GETTER_SETTER
#define EP_INCLUDE_SOURCE_FILES
#endif

#endif /* __EVENTPIPE_RT_CONFIG_H__ */
