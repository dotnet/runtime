// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>

#include <glib.h>

#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/webcil-loader.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/wasm-module-reader.h"

/* keep in sync with webcil-writer */
enum {
	MONO_WEBCIL_VERSION_MAJOR = 0,
	MONO_WEBCIL_VERSION_MINOR = 0,
};

typedef struct MonoWebCilHeader {
	uint8_t id[4]; // 'W' 'b' 'I' 'L'
	// 4 bytes
	uint16_t version_major; // 0
	uint16_t version_minor; // 0
	// 8 bytes
	uint16_t coff_sections;
	uint16_t reserved0; // 0
	// 12 bytes

	uint32_t pe_cli_header_rva;
	uint32_t pe_cli_header_size;
	// 20 bytes

	uint32_t pe_debug_rva;
	uint32_t pe_debug_size;
	// 28 bytes
} MonoWebCilHeader;

static gboolean
find_webcil_in_wasm (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **webcil_payload_start);

static gboolean
webcil_image_match (MonoImage *image)
{
	gboolean success = FALSE;
	if (image->raw_data_len >= sizeof (MonoWebCilHeader)) {
		success = image->raw_data[0] == 'W' && image->raw_data[1] == 'b' && image->raw_data[2] == 'I' && image->raw_data[3] == 'L';

		if (!success && mono_wasm_module_is_wasm ((const uint8_t*)image->raw_data, (const uint8_t*)image->raw_data + image->raw_data_len)) {
			/* if it's a WebAssembly module, assume it's webcil-in-wasm and
			 * optimistically return TRUE
			 */
			success = TRUE;
		}
	}
	return success;
}

/*
 * Fills the MonoDotNetHeader with data from the given raw_data+offset
 * by reading the webcil header.
 * most of MonoDotNetHeader is unused and left uninitialized (assumed zero);
 */
static int32_t
do_load_header (const char *raw_data, uint32_t raw_data_len, int32_t offset, MonoDotNetHeader *header, int32_t *raw_data_rva_map_wasm_bump)
{
	MonoWebCilHeader wcheader;
	const uint8_t *raw_data_bound = (const uint8_t*)raw_data + raw_data_len;
	*raw_data_rva_map_wasm_bump = 0;
	if (mono_wasm_module_is_wasm ((const uint8_t*)raw_data, raw_data_bound)) {
		/* assume it's webcil wrapped in wasm */
		const uint8_t *webcil_segment_start = NULL;
		if (!find_webcil_in_wasm ((const uint8_t*)raw_data, raw_data_bound, &webcil_segment_start))
			return -1;
		// HACK: adjust all the rva physical offsets by this amount
		int32_t offset_adjustment = (int32_t)(webcil_segment_start - (const uint8_t*)raw_data);
		*raw_data_rva_map_wasm_bump = offset_adjustment;
		// skip to the beginning of the webcil payload
		offset += offset_adjustment;
	}

	if (offset + sizeof (MonoWebCilHeader) > raw_data_len)
		return -1;
	memcpy (&wcheader, raw_data + offset, sizeof (wcheader));

	if (!(wcheader.id [0] == 'W' && wcheader.id [1] == 'b' && wcheader.id[2] == 'I' && wcheader.id[3] == 'L' &&
	      GUINT16_FROM_LE (wcheader.version_major) == MONO_WEBCIL_VERSION_MAJOR && GUINT16_FROM_LE (wcheader.version_minor) == MONO_WEBCIL_VERSION_MINOR))
		return -1;

	memset (header, 0, sizeof(MonoDotNetHeader));
	header->coff.coff_sections = GUINT16_FROM_LE (wcheader.coff_sections);
	header->datadir.pe_cli_header.rva = GUINT32_FROM_LE (wcheader.pe_cli_header_rva);
	header->datadir.pe_cli_header.size = GUINT32_FROM_LE (wcheader.pe_cli_header_size);
	header->datadir.pe_debug.rva = GUINT32_FROM_LE (wcheader.pe_debug_rva);
	header->datadir.pe_debug.size = GUINT32_FROM_LE (wcheader.pe_debug_size);

	offset += sizeof (wcheader);
	return offset;
}

int32_t
mono_webcil_load_section_table (const char *raw_data, uint32_t raw_data_len, int32_t offset, int32_t webcil_section_adjustment, MonoSectionTable *t)
{
	/* WebCIL section table entries are a subset of a PE section
	 * header. Initialize just the parts we have.
	 */
	uint32_t st [4];

	if (G_UNLIKELY (offset < 0))
		return offset;
	if ((uint32_t)offset > raw_data_len)
		return -1;
	memcpy (st, raw_data + offset, sizeof (st));
	t->st_virtual_size = GUINT32_FROM_LE (st [0]);
	t->st_virtual_address = GUINT32_FROM_LE (st [1]);
	t->st_raw_data_size = GUINT32_FROM_LE (st [2]);
	t->st_raw_data_ptr = GUINT32_FROM_LE (st [3]) + (uint32_t)webcil_section_adjustment;
	offset += sizeof(st);
	return offset;
}


static gboolean
webcil_image_load_pe_data (MonoImage *image)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	int32_t offset = 0;
	int32_t webcil_section_adjustment = 0;
	int top;

	iinfo = image->image_info;
	header = &iinfo->cli_header;

	offset = do_load_header (image->raw_data, image->raw_data_len, offset, header, &webcil_section_adjustment);
	if (offset == -1)
		goto invalid_image;
	/* HACK! RVAs and debug table entry pointers are from the beginning of the webcil payload. adjust MonoImage:raw_data to point to it */
	g_assert (image->ref_count == 1);
	// NOTE: image->storage->raw_data could be shared if we loaded this image multiple times (for different ALCs, for example)
	// Do not adjust image->storage->raw_data.
#ifdef ENABLE_WEBCIL
	int32_t old_adjustment;
	old_adjustment = mono_atomic_cas_i32 ((volatile gint32*)&image->storage->webcil_section_adjustment, webcil_section_adjustment, 0);
	g_assert (old_adjustment == 0 || old_adjustment == webcil_section_adjustment);
#endif
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Adjusting offset image %s [%p].", image->name, image);
	image->raw_data += webcil_section_adjustment;
	image->raw_data_len -= webcil_section_adjustment;
	offset -= webcil_section_adjustment;
	// parts of ecma-335 loading depend on 4-byte alignment of the image
	g_assertf (((intptr_t)image->raw_data) % 4 == 0, "webcil image %s [%p] raw data %p not 4 byte aligned\n", image->name, image, image->raw_data);

	top = iinfo->cli_header.coff.coff_sections;

	iinfo->cli_section_count = top;
	iinfo->cli_section_tables = g_new0 (MonoSectionTable, top);
	iinfo->cli_sections = g_new0 (void *, top);

	for (int i = 0; i < top; i++) {
		MonoSectionTable *t = &iinfo->cli_section_tables [i];
		offset = mono_webcil_load_section_table (image->raw_data, image->raw_data_len, offset, /*webcil_section_adjustment*/ 0, t);
		if (offset == -1)
			goto invalid_image;
	}

	return TRUE;

invalid_image:
	return FALSE;

}

static gboolean
webcil_image_load_cli_data (MonoImage *image)
{
	MonoCLIImageInfo *iinfo;

	iinfo = image->image_info;

	if (!mono_image_load_cli_header (image, iinfo))
		return FALSE;

	if (!mono_image_load_metadata (image, iinfo))
		return FALSE;

	return TRUE;
}

static gboolean
webcil_image_load_tables (MonoImage *image)
{
	return TRUE;
}

static const MonoImageLoader webcil_loader = {
	webcil_image_match,
	webcil_image_load_pe_data,
	webcil_image_load_cli_data,
	webcil_image_load_tables,
};

void
mono_webcil_loader_install (void)
{
	mono_install_image_loader (&webcil_loader);
}

int32_t
mono_webcil_load_cli_header (const char *raw_data, uint32_t raw_data_len, int32_t offset, MonoDotNetHeader *header, int32_t *webcil_section_adjustment)
{
	return do_load_header (raw_data, raw_data_len, offset, header, webcil_section_adjustment);
}

struct webcil_in_wasm_ud
{
	const uint8_t *data_segment_1_start;
};

static gboolean
webcil_in_wasm_section_visitor (uint8_t sec_code, const uint8_t *sec_content, uint32_t sec_length, gpointer user_data, gboolean *should_stop)
{
	*should_stop = FALSE;
	if (sec_code != MONO_WASM_MODULE_DATA_SECTION)
		return TRUE;
	struct webcil_in_wasm_ud *data = (struct webcil_in_wasm_ud *)user_data;

	*should_stop = TRUE; // we don't care about the sections after the data section
	const uint8_t *ptr = sec_content;
	const uint8_t *boundp = sec_content + sec_length;

	uint32_t num_segments = 0;
	if (!mono_wasm_module_decode_uleb128 (ptr, boundp, &ptr, &num_segments))
		return FALSE;

	if (num_segments != 2)
		return FALSE;

	// skip over data segment 0, it's the webcil payload length as a u32 plus padding - we don't care about it
	uint32_t passive_segment_len = 0;
	const uint8_t *passive_segment_start = NULL;
	if (!mono_wasm_module_decode_passive_data_segment (ptr, boundp, &ptr, &passive_segment_len, &passive_segment_start))
		return FALSE;
	// data segment 1 is the actual webcil payload.
	if (!mono_wasm_module_decode_passive_data_segment (ptr, boundp, &ptr, &passive_segment_len, &passive_segment_start))
		return FALSE;
	data->data_segment_1_start = passive_segment_start;
	return TRUE;
}

static gboolean
find_webcil_in_wasm (const uint8_t *ptr, const uint8_t *boundp, const uint8_t **webcil_payload_start)
{
	struct webcil_in_wasm_ud user_data = {0,};
	MonoWasmModuleVisitor visitor = {0,};
	visitor.section_visitor = &webcil_in_wasm_section_visitor;
	if (!mono_wasm_module_visit(ptr, boundp, &visitor, &user_data))
		return FALSE;
	*webcil_payload_start = user_data.data_segment_1_start;
	return TRUE;
}
