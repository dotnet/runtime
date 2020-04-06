/**
 * \file
 * 
 * Private unstable APIs.
 *
 * WARNING: The declarations and behavior of functions in this header are NOT STABLE and can be modified or removed at
 * any time.
 *
 */


#ifndef __MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__
#define __MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__

#include <mono/utils/mono-publib.h>

typedef MonoGCHandle MonoAssemblyLoadContextGCHandle;

MONO_API MONO_RT_EXTERNAL_ONLY
MonoAssembly *mono_assembly_load_full_alc (MonoAssemblyName *aname, const char *basedir, MonoImageOpenStatus *status, MonoAssemblyLoadContextGCHandle alc_gchandle);

#endif /*__MONO_METADATA_MONO_PRIVATE_UNSTABLE_H__*/
