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
	MonoMetadata *m = &image->metadata;
	MonoTableInfo *tt = m->tables [MONO_TABLE_TYPEDEF];
	int next_is_valid;
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	guint last;
	
	g_assert (typedef_token < tt->rows);
	
	mono_metadata_decode_row (tt, typedef_idx, &ret->cols, CSIZE (ret->cols));

	/*
	 * Get the field and method range
	 */
	ret->field.first = ret->cols [MONO_TYPEREF_FIELD_LIST] - 1;
	ret->method.first = ret->cols [MONO_TYPEDEF_METHOD_LIST] - 1;
	
	if (tt->rows > typedef_idx + 1){
		mono_metadata_decode_row (tt, typedef_idx + 1, cols_next, CSIZE (cols_next));
		ret->field.last = cols_next [MONO_TYPEREF_FIELD_LIST] - 1;
		ret->method.last = cols_next [MONO_TYPEREF_FIELD_LIST] - 1;
	} else {
		ret->field.last = m->tables [MONO_TABLE_FIELD].rows;
		ret->field.method = m->tables [MONO_TABLE_METHOD].rows;
	}

	/*
	 * Get the method range
	 */
	ref
	
}


