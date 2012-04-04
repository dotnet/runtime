/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "config.h"
#include "sgen-protocol.h"

#ifdef SGEN_BINARY_PROTOCOL

/* If not null, dump binary protocol to this file */
static FILE *binary_protocol_file = NULL;

static int binary_protocol_use_count = 0;

#define BINARY_PROTOCOL_BUFFER_SIZE	(65536 - 2 * 8)

typedef struct _BinaryProtocolBuffer BinaryProtocolBuffer;
struct _BinaryProtocolBuffer {
	BinaryProtocolBuffer *next;
	int index;
	unsigned char buffer [BINARY_PROTOCOL_BUFFER_SIZE];
};

static BinaryProtocolBuffer *binary_protocol_buffers = NULL;

void
binary_protocol_init (const char *filename)
{
	binary_protocol_file = fopen (filename, "w");
}

gboolean
binary_protocol_is_enabled (void)
{
	return binary_protocol_file != NULL;
}

static void
binary_protocol_flush_buffers_rec (BinaryProtocolBuffer *buffer)
{
	if (!buffer)
		return;

	binary_protocol_flush_buffers_rec (buffer->next);

	g_assert (buffer->index > 0);
	fwrite (buffer->buffer, 1, buffer->index, binary_protocol_file);

	sgen_free_os_memory (buffer, sizeof (BinaryProtocolBuffer));
}

void
binary_protocol_flush_buffers (gboolean force)
{
	if (!binary_protocol_file)
		return;

	if (!force && binary_protocol_use_count != 0)
		return;

	binary_protocol_flush_buffers_rec (binary_protocol_buffers);
	binary_protocol_buffers = NULL;

	fflush (binary_protocol_file);
}

static BinaryProtocolBuffer*
binary_protocol_get_buffer (int length)
{
	BinaryProtocolBuffer *buffer, *new_buffer;

 retry:
	buffer = binary_protocol_buffers;
	if (buffer && buffer->index + length <= BINARY_PROTOCOL_BUFFER_SIZE)
		return buffer;

	new_buffer = sgen_alloc_os_memory (sizeof (BinaryProtocolBuffer), TRUE);
	new_buffer->next = buffer;
	new_buffer->index = 0;

	if (InterlockedCompareExchangePointer ((void**)&binary_protocol_buffers, new_buffer, buffer) != buffer) {
		sgen_free_os_memory (new_buffer, sizeof (BinaryProtocolBuffer));
		goto retry;
	}

	return new_buffer;
}


static void
protocol_entry (unsigned char type, gpointer data, int size)
{
	int index;
	BinaryProtocolBuffer *buffer;
	int old_count;

	if (!binary_protocol_file)
		return;

	do {
		old_count = binary_protocol_use_count;
		g_assert (old_count >= 0);
	} while (InterlockedCompareExchange (&binary_protocol_use_count, old_count + 1, old_count) != old_count);

 retry:
	buffer = binary_protocol_get_buffer (size + 1);
 retry_same_buffer:
	index = buffer->index;
	if (index + 1 + size > BINARY_PROTOCOL_BUFFER_SIZE)
		goto retry;

	if (InterlockedCompareExchange (&buffer->index, index + 1 + size, index) != index)
		goto retry_same_buffer;

	/* FIXME: if we're interrupted at this point, we have a buffer
	   entry that contains random data. */

	buffer->buffer [index++] = type;
	memcpy (buffer->buffer + index, data, size);
	index += size;

	g_assert (index <= BINARY_PROTOCOL_BUFFER_SIZE);

	do {
		old_count = binary_protocol_use_count;
		g_assert (old_count > 0);
	} while (InterlockedCompareExchange (&binary_protocol_use_count, old_count - 1, old_count) != old_count);
}

void
binary_protocol_collection (int generation)
{
	SGenProtocolCollection entry = { generation };
	binary_protocol_flush_buffers (FALSE);
	protocol_entry (SGEN_PROTOCOL_COLLECTION, &entry, sizeof (SGenProtocolCollection));
}

void
binary_protocol_alloc (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolAlloc entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_ALLOC, &entry, sizeof (SGenProtocolAlloc));
}

void
binary_protocol_alloc_pinned (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolAlloc entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_ALLOC_PINNED, &entry, sizeof (SGenProtocolAlloc));
}

void
binary_protocol_alloc_degraded (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolAlloc entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_ALLOC_DEGRADED, &entry, sizeof (SGenProtocolAlloc));
}

void
binary_protocol_copy (gpointer from, gpointer to, gpointer vtable, int size)
{
	SGenProtocolCopy entry = { from, to, vtable, size };
	protocol_entry (SGEN_PROTOCOL_COPY, &entry, sizeof (SGenProtocolCopy));
}

void
binary_protocol_pin (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolPin entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_PIN, &entry, sizeof (SGenProtocolPin));
}

void
binary_protocol_mark (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolMark entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_MARK, &entry, sizeof (SGenProtocolMark));
}

void
binary_protocol_wbarrier (gpointer ptr, gpointer value, gpointer value_vtable)
{
	SGenProtocolWBarrier entry = { ptr, value, value_vtable };
	protocol_entry (SGEN_PROTOCOL_WBARRIER, &entry, sizeof (SGenProtocolWBarrier));
}

void
binary_protocol_global_remset (gpointer ptr, gpointer value, gpointer value_vtable)
{
	SGenProtocolGlobalRemset entry = { ptr, value, value_vtable };
	protocol_entry (SGEN_PROTOCOL_GLOBAL_REMSET, &entry, sizeof (SGenProtocolGlobalRemset));
}

void
binary_protocol_ptr_update (gpointer ptr, gpointer old_value, gpointer new_value, gpointer vtable, int size)
{
	SGenProtocolPtrUpdate entry = { ptr, old_value, new_value, vtable, size };
	protocol_entry (SGEN_PROTOCOL_PTR_UPDATE, &entry, sizeof (SGenProtocolPtrUpdate));
}

void
binary_protocol_cleanup (gpointer ptr, gpointer vtable, int size)
{
	SGenProtocolCleanup entry = { ptr, vtable, size };
	protocol_entry (SGEN_PROTOCOL_CLEANUP, &entry, sizeof (SGenProtocolCleanup));
}

void
binary_protocol_empty (gpointer start, int size)
{
	SGenProtocolEmpty entry = { start, size };
	protocol_entry (SGEN_PROTOCOL_EMPTY, &entry, sizeof (SGenProtocolEmpty));
}

void
binary_protocol_thread_restart (gpointer thread)
{
	SGenProtocolThreadRestart entry = { thread };
	protocol_entry (SGEN_PROTOCOL_THREAD_RESTART, &entry, sizeof (SGenProtocolThreadRestart));

}

void
binary_protocol_thread_register (gpointer thread)
{
	SGenProtocolThreadRegister entry = { thread };
	protocol_entry (SGEN_PROTOCOL_THREAD_REGISTER, &entry, sizeof (SGenProtocolThreadRegister));

}

void
binary_protocol_thread_unregister (gpointer thread)
{
	SGenProtocolThreadUnregister entry = { thread };
	protocol_entry (SGEN_PROTOCOL_THREAD_UNREGISTER, &entry, sizeof (SGenProtocolThreadUnregister));

}

void
binary_protocol_missing_remset (gpointer obj, gpointer obj_vtable, int offset, gpointer value, gpointer value_vtable, int value_pinned)
{
	SGenProtocolMissingRemset entry = { obj, obj_vtable, offset, value, value_vtable, value_pinned };
	protocol_entry (SGEN_PROTOCOL_MISSING_REMSET, &entry, sizeof (SGenProtocolMissingRemset));

}

#endif
