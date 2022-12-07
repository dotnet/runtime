// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <config.h>

#include <glib.h>

#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/webcil-loader.h"

/* keep in sync with webcil-writer */
enum {
	MONO_WEBCIL_VERSION = 0,
};

typedef struct MonoWebCilHeader {
	uint8_t id[2]; // 'W' 'C'
	uint8_t version;
	uint8_t reserved0; // 0
	// 4 bytes
	uint16_t coff_sections;
	uint16_t reserved1; // 0
	// 8 bytes

	uint32_t metadata_rva;
	uint32_t metadata_size;
	// 16 bytes

	uint32_t cli_flags;
	int32_t cli_entry_point;
	// 24 bytes

	uint32_t pe_cli_header_rva;
	uint32_t pe_cli_header_size;
	// 32 bytes
} MonoWebCilHeader;

static gboolean
webcil_image_match (MonoImage *image)
{
	if (image->raw_data_len >= sizeof (MonoWebCilHeader)) {
		return image->raw_data[0] == 'W' && image->raw_data[1] == 'C';
	}
	return FALSE;
}

static gboolean
webcil_image_load_pe_data (MonoImage *image)
{
	MonoCLIImageInfo *iinfo;
	MonoDotNetHeader *header;
	MonoWebCilHeader wcheader;
	gint32 offset = 0;
	int i, top;
	char d [16 * 8];
	char *p;

	iinfo = image->image_info;
	header = &iinfo->cli_header;

	if (offset + sizeof (MonoWebCilHeader) > image->raw_data_len)
		goto invalid_image;
	memcpy (&wcheader, image->raw_data + offset, sizeof (wcheader));
	
	if (!(wcheader.id [0] == 'W' && wcheader.id [1] == 'C' && wcheader.version == MONO_WEBCIL_VERSION))
		goto invalid_image;
	
	header->coff.coff_sections = GUINT16_FROM_LE (wcheader.coff_sections);
	header->datadir.pe_cli_header.rva = GUINT32_FROM_LE (wcheader.pe_cli_header_rva);
	header->datadir.pe_cli_header.size = GUINT32_FROM_LE (wcheader.pe_cli_header_size);

	top = iinfo->cli_header.coff.coff_sections;

	iinfo->cli_section_count = top;
	iinfo->cli_section_tables = g_new0 (MonoSectionTable, top);
	iinfo->cli_sections = g_new0 (void *, top);

	offset += sizeof (wcheader);
	g_assert (top < 8);
	p = d;
	memcpy (d, image->raw_data + offset, top * 16);
	for (i = 0; i < top; i++) {
		MonoSectionTable *t = &iinfo->cli_section_tables [i];
		guint32 st [4];

		memcpy (st, p, sizeof (st));
		t->st_virtual_size = GUINT32_FROM_LE (st [0]);
		t->st_virtual_address = GUINT32_FROM_LE (st [1]);
		t->st_raw_data_size = GUINT32_FROM_LE (st [2]);
		t->st_raw_data_ptr = GUINT32_FROM_LE (st [3]);
		p += 16;
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
