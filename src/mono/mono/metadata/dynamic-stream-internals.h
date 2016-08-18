/* 
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_DYNAMIC_STREAM_INTERNALS_H__
#define __MONO_METADATA_DYNAMIC_STREAM_INTERNALS_H__

#include <mono/metadata/object.h>
#include <mono/metadata/metadata-internals.h>

void
mono_dynstream_init (MonoDynamicStream *stream);

guint32
mono_dynstream_insert_string (MonoDynamicStream *sh, const char *str);

guint32
mono_dynstream_insert_mstring (MonoDynamicStream *sh, MonoString *str, MonoError *error);

guint32
mono_dynstream_add_data (MonoDynamicStream *stream, const char *data, guint32 len);

guint32
mono_dynstream_add_zero (MonoDynamicStream *stream, guint32 len);

void
mono_dynstream_data_align (MonoDynamicStream *stream);

#endif  /* __MONO_METADATA_DYNAMIC_STREAM_INTERNALS_H__ */

