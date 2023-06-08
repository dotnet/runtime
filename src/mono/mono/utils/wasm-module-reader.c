// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <config.h>

#include "mono/metadata/mono-endian.h"

#include "wasm-module-reader.h"

#define WASM_MODULE_SECTION(ident,str) str,
static const char *
wasm_module_section_names[] = {
#include "wasm-sections.def"
#undef WASM_MODULE_SECTION
};

static const char *
mono_wasm_module_section_get_name (int section)
{
	g_assert (section > 0 && section < MONO_WASM_MODULE_NUM_SECTIONS);
	return wasm_module_section_names[section];
}

static gboolean
bc_read8 (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint8_t *out)
{
	if (ptr < boundp) {
		*out = *ptr;
		*endp = ptr + 1;
		return TRUE;
	}
	return FALSE;
}

static gboolean
bc_read32 (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint32_t *out)
{
	if (ptr + 3 < boundp) {
		*out = read32 (ptr);
		*endp = ptr + 4;
		return TRUE;
	}
	return FALSE;
}

static gboolean
bc_read_uleb128 (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint32_t *out)
{
	uint32_t val = 0;
	unsigned int shift = 0;
	while (1) {
		uint8_t b;
		if (!bc_read8 (ptr, boundp, &ptr, &b))
			return FALSE;
		val |= (b & 0x7f) << shift;
		if ((b & 0x80) == 0) break;
		shift += 7;
		g_assertf (shift < 35, "expected uleb128 encoded u32, got extra bytes\n");
	}
	*out = val;
	*endp = ptr;

	return TRUE;
}

gboolean
mono_wasm_module_decode_uleb128 (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint32_t *out)
{
	return bc_read_uleb128 (ptr, boundp, endp, out);
}

static gboolean
visit_section (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, MonoWasmModuleVisitor *visitor, gpointer user_data, gboolean *should_stop)
{
	uint8_t code = 0;
	uint32_t sec_size = 0;
	if (!bc_read8 (ptr, boundp, &ptr, &code))
		return FALSE;
	if (!bc_read_uleb128 (ptr, boundp, &ptr, &sec_size))
		return FALSE;

	*should_stop = FALSE;
	gboolean success = visitor->section_visitor (code, ptr, sec_size, user_data, should_stop);
	*endp = ptr + sec_size; // advance past the section payload
	return success;
}

/*
 * return TRUE if successfully visited, FALSE if there was a problem
 */
gboolean
mono_wasm_module_visit (const uint8_t *ptr, const uint8_t *boundp, MonoWasmModuleVisitor *visitor, gpointer user_data)
{
	if (!mono_wasm_module_is_wasm (ptr, boundp))
		return FALSE;

	ptr += 4;

	uint32_t version = 0;
	if (!bc_read32 (ptr, boundp, &ptr, &version))
		return FALSE;
	if (version != 1)
		return FALSE;

	gboolean success = TRUE;

	gboolean stop = FALSE;
	while (success && !stop && ptr < boundp) {
		success = visit_section (ptr, boundp, &ptr, visitor, user_data, &stop);
	}

	return success;
}

gboolean
mono_wasm_module_is_wasm (const uint8_t *ptr, const uint8_t *boundp)
{
	const uint32_t wasm_magic = 0x6d736100u; // "\0asm"
	uint32_t magic = 0;
	if (!bc_read32 (ptr, boundp, &ptr, &magic))
		return FALSE;
	return magic == wasm_magic;
}

gboolean
mono_wasm_module_decode_passive_data_segment (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **endp, uint32_t *data_len, const uint8_t **data_start)
{
	uint8_t code = 0;
	if (!bc_read8 (ptr, boundp, &ptr, &code))
		return FALSE;
	if (code != 1)
		return FALSE; // not a passive segment
	uint32_t len = 0;
	if (!bc_read_uleb128 (ptr, boundp, &ptr, &len))
		return FALSE;
	*data_start = ptr;
	*data_len = len;
	*endp = ptr + len;
	return TRUE;
}
