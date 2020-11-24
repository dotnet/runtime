#ifndef __DIAGNOSTICS_RT_CONFIG_H__
#define __DIAGNOSTICS_RT_CONFIG_H__

#include <config.h>
#include "ep-rt-config.h"

#ifdef EP_INLINE_GETTER_SETTER
#define DS_INLINE_GETTER_SETTER
#endif

#ifdef EP_INCLUDE_SOURCE_FILES
#define DS_INCLUDE_SOURCE_FILES
#endif

#endif /* __DIAGNOSTICS_RT_CONFIG_H__ */
