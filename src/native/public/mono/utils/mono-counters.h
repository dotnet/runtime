/**
 * \file
 *
 * WARNING: The functions in this header are no-ops and provided for source compatibility with older versions of mono only.
 *
 */

#ifndef __MONO_COUNTERS_H__
#define __MONO_COUNTERS_H__

#include <mono/utils/details/mono-counters-types.h>

MONO_BEGIN_DECLS

#define MONO_API_FUNCTION(ret,name,args) MONO_API ret name args;
#include <mono/utils/details/mono-counters-functions.h>
#undef MONO_API_FUNCTION

MONO_END_DECLS

#endif /* __MONO_COUNTERS_H__ */

