/*
 * typedef.c: Handling of TypeDefs. 
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <mono/metadata/typedef.h>

/**
 * mono_typedef_decode:
 * @image: image to decode from
 * @tidx: typedef number
 *
 * Decodes the TypeDef whose index is @tidx in @image
 */
void
mono_typedef_decode (MonoImage *image, guint32 tidx, MonoTypedef *ret)
{
	metadata_t *m = &image->metadata;
	metadata_tableinfo_t *tt = m->tables [META_TABLE_TYPEDEF];
	int next_is_valid;
	guint32 cols_next [6];
	guint last;
	
	g_assert (typedef_token < tt->rows);
	
	mono_metadata_decode_row (tt, typedef_idx, &ret->cols, CSIZE (ret->cols));

	/*
	 * Get the field and method range
	 */
	ret->field.first = ret->cols [4] - 1;
	ret->method.first = ret->cols [5] - 1;
	
	if (tt->rows > typedef_idx + 1){
		mono_metadata_decode_row (tt, typedef_idx + 1, cols_next, CSIZE (cols_next));
		ret->field.last = cols_next [4] - 1;
		ret->method.last = cols_next [5] - 1;
	} else {
		ret->field.last = m->tables [META_TABLE_FIELD].rows;
		ret->field.method = m->tables [META_TABLE_METHOD].rows;
	}

	/*
	 * Get the method range
	 */
	ref
	
}


