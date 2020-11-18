#ifndef __EVENTPIPE_RT_CONFIG_H__
#define __EVENTPIPE_RT_CONFIG_H__

#include <config.h>
#ifdef MONO_CORLIB_VERSION
// EventPipe runtime implementation.
#define EP_RT_H "ep-rt-mono.h"
#define EP_RT_TYPES_H "ep-rt-types-mono.h"
#define EP_RT_CONFIG_H "ep-rt-config-mono.h"

// DiagnosticServer runtime implementation.
#define DS_RT_H "ds-rt-mono.h"
#define DS_RT_TYPES_H "ds-rt-types-mono.h"
#else
// EventPipe runtime implementation.
#define EP_RT_H "ep-rt-coreclr.h"
#define EP_RT_TYPES_H "ep-rt-types-coreclr.h"
#define EP_RT_CONFIG_H "ep-rt-config-coreclr.h"

// DiagnosticServer runtime implementation.
#define DS_RT_H "ds-rt-coreclr.h"
#define DS_RT_TYPES_H "ds-rt-types-coreclr.h"

#include "common.h"
#endif

#include EP_RT_CONFIG_H

#define EP_INLINE_GETTER_SETTER

#ifdef EP_INLINE_GETTER_SETTER
#define EP_INCLUDE_SOURCE_FILES
#endif

#endif /* __EVENTPIPE_RT_CONFIG_H__ */
