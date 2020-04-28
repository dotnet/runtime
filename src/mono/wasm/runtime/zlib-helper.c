/*
 * Used by System.IO.Compression.DeflateStream
 *
 * Author:
 *   Gonzalo Paniagua Javier (gonzalo@novell.com)
 *
 * (c) Copyright 2009 Novell, Inc.
 */
#include <string.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/jit/jit.h>

#include "zlib.h"

#ifndef MONO_API
#define MONO_API
#endif

#ifndef TRUE
#define FALSE 0
#define TRUE 1
#endif

/*
 * Basic data types
 */
typedef int            gint;
typedef unsigned int   guint;
typedef short          gshort;
typedef unsigned short gushort;
typedef long           glong;
typedef unsigned long  gulong;
typedef void *         gpointer;
typedef const void *   gconstpointer;
typedef char           gchar;
typedef unsigned char  guchar;

/* Types defined in terms of the stdint.h */
typedef int8_t         gint8;
typedef uint8_t        guint8;
typedef int16_t        gint16;
typedef uint16_t       guint16;
typedef int32_t        gint32;
typedef uint32_t       guint32;
typedef int64_t        gint64;
typedef uint64_t       guint64;
typedef float          gfloat;
typedef double         gdouble;
typedef int32_t        gboolean;

#define BUFFER_SIZE 4096
#define ARGUMENT_ERROR -10
#define IO_ERROR -11
#define MONO_EXCEPTION -12

#define z_malloc(size)          ((gpointer) malloc(size))
#define z_malloc0(size)         ((gpointer) calloc(1,size))
#define z_new(type,size)        ((type *) z_malloc (sizeof (type) * (size)))
#define z_new0(type,size)       ((type *) z_malloc0 (sizeof (type)* (size)))

typedef gint (*read_write_func) (guchar *buffer, gint length, void *gchandle);
struct _ZStream {
	z_stream *stream;
	guchar *buffer;
	read_write_func func;
	void *gchandle;
	guchar compress;
	guchar eof;
	guint32 total_in;
};
typedef struct _ZStream ZStream;

MONO_API ZStream *CreateZStream (gint compress, guchar gzip, read_write_func func, void *gchandle);
MONO_API gint CloseZStream (ZStream *zstream);
MONO_API gint Flush (ZStream *stream);
MONO_API gint ReadZStream (ZStream *stream, guchar *buffer, gint length);
MONO_API gint WriteZStream (ZStream *stream, guchar *buffer, gint length);
static gint flush_internal (ZStream *stream, gboolean is_final);
MonoMethod* GetReadOrWriteCallback (gint compress, void *gchandle);

static void *
z_alloc (void *opaque, unsigned int nitems, unsigned int item_size)
{
	return z_malloc0 (nitems * item_size);
}

static void
z_free (void *opaque, void *ptr)
{
	free (ptr);
}

ZStream *
CreateZStream (gint compress, guchar gzip, read_write_func func, void *gchandle)
{
	z_stream *z;
	gint retval;
	ZStream *result;

	if (func == NULL)
		return NULL;

#if !defined(ZLIB_VERNUM) || (ZLIB_VERNUM < 0x1204)
	/* Older versions of zlib do not support raw deflate or gzip */
	return NULL;
#endif

	z = z_new0 (z_stream, 1);
	if (compress) {
		retval = deflateInit2 (z, Z_DEFAULT_COMPRESSION, Z_DEFLATED, gzip ? 31 : -15, 8, Z_DEFAULT_STRATEGY);
	} else {
		retval = inflateInit2 (z, gzip ? 31 : -15);
	}

	if (retval != Z_OK) {
		free (z);
		return NULL;
	}
	z->zalloc = z_alloc;
	z->zfree = z_free;
	result = z_new0 (ZStream, 1);
	result->stream = z;
	result->func = func;
	result->gchandle = gchandle;
	result->compress = compress;
	result->buffer = z_new (guchar, BUFFER_SIZE);
	result->stream->next_out = result->buffer;
	result->stream->avail_out = BUFFER_SIZE;
	result->stream->total_in = 0;
	return result;
}

gint
CloseZStream (ZStream *zstream)
{
	gint status;
	gint flush_status;

	if (zstream == NULL)
		return ARGUMENT_ERROR;

	status = 0;
	if (zstream->compress) {
		if (zstream->stream->total_in > 0) {
			do {
				status = deflate (zstream->stream, Z_FINISH);
				flush_status = flush_internal (zstream, TRUE);
				if (flush_status == MONO_EXCEPTION) {
					status = flush_status;
					break;
				}
			} while (status == Z_OK); /* We want Z_STREAM_END or error here here */
			if (status == Z_STREAM_END)
				status = flush_status;
		}
		deflateEnd (zstream->stream);
	} else {
		inflateEnd (zstream->stream);
	}
	free (zstream->buffer);
	free (zstream->stream);
	memset (zstream, 0, sizeof (ZStream));
	free (zstream);
	return status;
}

static gint
write_to_managed (ZStream *stream)
{
	gint n;
	z_stream *zs;
	MonoObject *result, *exception;
	MonoMethod *unmanagedWrite = GetReadOrWriteCallback(stream->compress, stream->gchandle);
	void* args[3];

	zs = stream->stream;
	if (zs->avail_out != BUFFER_SIZE) {
		
		//n = stream->func (stream->buffer, BUFFER_SIZE - zs->avail_out, stream->gchandle);
		args[0] = &stream->buffer;
		int length = BUFFER_SIZE - zs->avail_out;
		args[1] = &length;
		args[2] = &stream->gchandle;
		exception = NULL;

		result = mono_runtime_invoke(unmanagedWrite, NULL, args, &exception);
		if (exception) {
			MonoClass* eklass = mono_object_get_class((MonoObject*)exception);
			MonoProperty* prop = mono_class_get_property_from_name(eklass, "Message");
			MonoString *message = (MonoString*)mono_property_get_value(prop, exception, NULL, NULL);
			char *p = mono_string_to_utf8 (message);
			fprintf(stderr,"An exception was thrown in zlib-helper.c UnmanageWrite (IntPtr, int, IntPtr) - %s\n", p);
			mono_free (p);
			return MONO_EXCEPTION;
		}	

		n = *(int*)mono_object_unbox (result);
		zs->next_out = stream->buffer;
		zs->avail_out = BUFFER_SIZE;
		if (n == MONO_EXCEPTION)
			return n;
		if (n < 0)
			return IO_ERROR;
	}
	return 0;
}

static gint
flush_internal (ZStream *stream, gboolean is_final)
{
	gint status;

	if (!stream->compress)
		return 0;

	if (!is_final && stream->stream->avail_in != 0) {
		status = deflate (stream->stream, Z_PARTIAL_FLUSH);
		if (status != Z_OK && status != Z_STREAM_END)
			return status;
	}
	return write_to_managed (stream);
}

gint
Flush (ZStream *stream)
{
	return flush_internal (stream, FALSE);
}

gint
ReadZStream (ZStream *stream, guchar *buffer, gint length)
{
	gint n;
	gint status;
	z_stream *zs;
	MonoObject *result, *exception;
	MonoMethod *unmanagedRead = GetReadOrWriteCallback(stream->compress, stream->gchandle);
	void* args[3];

	if (stream == NULL || buffer == NULL || length < 0)
		return ARGUMENT_ERROR;

	if (stream->eof)
		return 0;

	zs = stream->stream;
	zs->next_out = buffer;
	zs->avail_out = length;
	while (zs->avail_out > 0) {
		if (zs->avail_in == 0) {
			//n = stream->func (stream->buffer, BUFFER_SIZE, stream->gchandle);
			args[0] = &stream->buffer;
			int bufferLength = BUFFER_SIZE;
			args[1] = &bufferLength;
			args[2] = &stream->gchandle;
			exception = NULL;

			result = mono_runtime_invoke(unmanagedRead, NULL, args, &exception);
			if (exception) {
				MonoClass* eklass = mono_object_get_class((MonoObject*)exception);
				MonoProperty* prop = mono_class_get_property_from_name(eklass, "Message");
				MonoString *message = (MonoString*)mono_property_get_value(prop, exception, NULL, NULL);
				char *p = mono_string_to_utf8 (message);
				fprintf(stderr,"An exception was thrown in zlib-helper.c UnmanageRead (IntPtr, int, IntPtr) - %s\n", p);
				mono_free (p);
				return MONO_EXCEPTION;
			}	
			n = *(int*)mono_object_unbox (result);
			n = n < 0 ? 0 : n;
			stream->total_in += n;
			zs->next_in = stream->buffer;
			zs->avail_in = n;
		}

		if (zs->avail_in == 0 && zs->total_in == 0)
			return 0;

		status = inflate (stream->stream, Z_SYNC_FLUSH);
		if (status == Z_STREAM_END) {
			stream->eof = TRUE;
			break;
		} else if (status == Z_BUF_ERROR && stream->total_in == zs->total_in) {
			if (zs->avail_in != 0) {
				stream->eof = TRUE;
			}
			break;
		} else if (status != Z_OK) {
			return status;
		}
	}
	return length - zs->avail_out;
}

gint
WriteZStream (ZStream *stream, guchar *buffer, gint length)
{
	gint n;
	gint status;
	z_stream *zs;

	if (stream == NULL || buffer == NULL || length < 0)
		return ARGUMENT_ERROR;

	if (stream->eof)
		return IO_ERROR;

	zs = stream->stream;
	zs->next_in = buffer;
	zs->avail_in = length;
	while (zs->avail_in > 0) {
		if (zs->avail_out == 0) {
			zs->next_out = stream->buffer;
			zs->avail_out = BUFFER_SIZE;
		}
		status = deflate (stream->stream, Z_NO_FLUSH);
		if (status != Z_OK && status != Z_STREAM_END)
			return status;

		if (zs->avail_out == 0) {
			n = write_to_managed (stream);
			if (n < 0)
				return n;
		}
	}
	return length;
}

MonoMethod* 
GetReadOrWriteCallback (gint compress, void *gchandle)
{
	MonoObject* callback = mono_gchandle_get_target ((guint32)gchandle);
	if (!callback) {
		fprintf(stderr, "Error: zlib-helper - Callback target not found.\n");
		return NULL;
	}	

	MonoClass* klass = mono_object_get_class(callback);
	if (!klass) {
		fprintf(stderr, "Error: zlib-helper - Callback class not found.\n");
		return NULL;
	}

	MonoMethod* read_write_method = NULL;
	MonoMethodDesc* desc = NULL;
	if (compress == 1) {  // compress stream
		desc = mono_method_desc_new(":UnmanagedWrite", FALSE);
		if (!desc) {
			fprintf(stderr, "Error: zlib-helper - MethodDesc for UnmanagedWrite not found.\n");
			return NULL;
		}
		read_write_method = mono_method_desc_search_in_class (desc, klass);
		if (!read_write_method) {
			fprintf(stderr, "Error: zlib-helper - Method UnmanagedWrite not found.\n");
			return NULL;
		}
	}
	else {
		desc = mono_method_desc_new(":UnmanagedRead", FALSE);
		if (!desc) {
			fprintf(stderr, "Error: zlib-helper - MethodDesc for UnmanagedRead not found.\n");
			return NULL;
		}
		read_write_method = mono_method_desc_search_in_class (desc, klass);
		if (!read_write_method) {
			fprintf(stderr, "Error: zlib-helper - Method UnmanagedRead not found.\n");
			return NULL;
		}
	}
	mono_method_desc_free (desc);
	return read_write_method;
}

