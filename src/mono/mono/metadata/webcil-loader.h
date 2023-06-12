// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef _MONO_METADATA_WEBCIL_LOADER_H
#define _MONO_METADATA_WEBCIL_LOADER_H

#define MONO_WEBCIL_IN_WASM_EXTENSION ".wasm"

void
mono_webcil_loader_install (void);

int32_t
mono_webcil_load_cli_header (const char *raw_data, uint32_t raw_data_len, int32_t offset, MonoDotNetHeader *header, int32_t *webcil_section_adjustment);

int32_t
mono_webcil_load_section_table (const char *raw_data, uint32_t raw_data_len, int32_t offset, int32_t webcil_section_adjustment, MonoSectionTable *t);

#endif /*_MONO_METADATA_WEBCIL_LOADER_H*/
