#ifndef _MONONET_METADATA_ASSEMBLY_H_ 
#define _MONONET_METADATA_ASSEMBLY_H_

#include <mono/metadata/image.h>

#define CORLIB_NAME "corlib.dll"

typedef char * (*MonoAssemblyResolverFn)(const char *name);

MonoAssembly *mono_assembly_open     (const char *fname,
				      MonoAssemblyResolverFn resolver,
				      enum MonoImageOpenStatus *status);
void          mono_assembly_close    (MonoAssembly *assembly);

GHashTable   *mono_get_assemblies    (void);
#endif
