// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>

#include <glib.h>

#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/webcil-loader.h"

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
webcil_image_match (MonoImage *image)
{
	if (image->raw_data_len >= sizeof (MonoWebCilHeader)) {
		return image->raw_data[0] == 'W' && image->raw_data[1] == 'b' && image->raw_data[2] == 'I' && image->raw_data[3] == 'L';
	}
	return FALSE;
}

/*
 * Fills the MonoDotNetHeader with data from the given raw_data+offset
 * by reading the webcil header.
 * most of MonoDotNetHeader is unused and left uninitialized (assumed zero);
 */
static int32_t
do_load_header (const char *raw_data, uint32_t raw_data_len, int32_t offset, MonoDotNetHeader *header)
{
	MonoWebCilHeader wcheader;
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
mono_webcil_load_section_table (const char *raw_data, uint32_t raw_data_len, int32_t offset, MonoSectionTable *t)
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
	t->st_raw_data_ptr = GUINT32_FROM_LE (st [3]);
	offset += sizeof(st);
	return offset;
}


static gboolean
webcil_image_load_pe_data (MonoImage *image)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	int32_t offset = 0;
	int top;

	iinfo = image->image_info;
	header = &iinfo->cli_header;

	offset = do_load_header (image->raw_data, image->raw_data_len, offset, header);
	if (offset == -1)
		goto invalid_image;

	top = iinfo->cli_header.coff.coff_sections;

	iinfo->cli_section_count = top;
	iinfo->cli_section_tables = g_new0 (MonoSectionTable, top);
	iinfo->cli_sections = g_new0 (void *, top);

	for (int i = 0; i < top; i++) {
		MonoSectionTable *t = &iinfo->cli_section_tables [i];
		offset = mono_webcil_load_section_table (image->raw_data, image->raw_data_len, offset, t);
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
mono_webcil_load_cli_header (const char *raw_data, uint32_t raw_data_len, int32_t offset, MonoDotNetHeader *header)
{
	return do_load_header (raw_data, raw_data_len, offset, header);
}
