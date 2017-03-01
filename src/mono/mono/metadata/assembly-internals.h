/*
 * Copyright 2015 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_ASSEMBLY_INTERNALS_H__
#define __MONO_METADATA_ASSEMBLY_INTERNALS_H__

#include <mono/metadata/assembly.h>

MONO_API MonoImage*    mono_assembly_load_module_checked (MonoAssembly *assembly, uint32_t idx, MonoError *error);

MonoAssembly * mono_assembly_open_a_lot (const char *filename, MonoImageOpenStatus *status, gboolean refonly, gboolean load_from_context);

#endif /* __MONO_METADATA_ASSEMBLY_INTERNALS_H__ */
