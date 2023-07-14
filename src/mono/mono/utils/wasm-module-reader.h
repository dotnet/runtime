// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __MONO_WASM_MODULE_READER_H__
#define __MONO_WASM_MODULE_READER_H__

#include <glib.h>

typedef struct MonoWasmModuleVisitor
{
	/* return TRUE for success, set *should_stop to stop visitation */
	gboolean (*section_visitor) (uint8_t sec_code, const uint8_t *sec_content, uint32_t sec_length, gpointer user_data, gboolean *should_stop);
} MonoWasmModuleVisitor;

#define WASM_MODULE_SECTION(ident,str) MONO_WASM_MODULE_ ## ident ## _SECTION,
enum {
#include "wasm-sections.def"
	MONO_WASM_MODULE_NUM_SECTIONS,
};
#undef WASM_MODULE_SECTION

gboolean
mono_wasm_module_decode_uleb128 (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint32_t *out);

gboolean
mono_wasm_module_is_wasm (const uint8_t *ptr, const uint8_t *boundp);

gboolean
mono_wasm_module_visit (const uint8_t *ptr, const uint8_t *boundp, MonoWasmModuleVisitor *visitor, gpointer user_data);

/* returns FALSE if the data segment is not passive */
gboolean
mono_wasm_module_decode_passive_data_segment (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint32_t *data_len, const uint8_t **data_start);

#endif /* __MONO_WASM_MODULE_READER_H__*/
