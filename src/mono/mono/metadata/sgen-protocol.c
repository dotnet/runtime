/*
 * sgen-protocol.c: Binary protocol of internal activity, to aid
 * debugging.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2012 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 */

#ifdef HAVE_SGEN_GC

#include "config.h"
#include "sgen-gc.h"
#include "sgen-protocol.h"
#include "sgen-memory-governor.h"
#include "utils/mono-mmap.h"

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

	sgen_free_os_memory (buffer, sizeof (BinaryProtocolBuffer), SGEN_ALLOC_INTERNAL);
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

	new_buffer = sgen_alloc_os_memory (sizeof (BinaryProtocolBuffer), SGEN_ALLOC_INTERNAL | SGEN_ALLOC_ACTIVATE, "debugging memory");
	new_buffer->next = buffer;
	new_buffer->index = 0;

	if (InterlockedCompareExchangePointer ((void**)&binary_protocol_buffers, new_buffer, buffer) != buffer) {
		sgen_free_os_memory (new_buffer, sizeof (BinaryProtocolBuffer), SGEN_ALLOC_INTERNAL);
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
binary_protocol_collection_begin (int index, int generation)
{
	SGenProtocolCollection entry = { index, generation };
	binary_protocol_flush_buffers (FALSE);
	protocol_entry (SGEN_PROTOCOL_COLLECTION_BEGIN, &entry, sizeof (SGenProtocolCollection));
}

void
binary_protocol_collection_end (int index, int generation)
{
	SGenProtocolCollection entry = { index, generation };
	binary_protocol_flush_buffers (FALSE);
	protocol_entry (SGEN_PROTOCOL_COLLECTION_END, &entry, sizeof (SGenProtocolCollection));
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
binary_protocol_thread_suspend (gpointer thread, gpointer stopped_ip)
{
	SGenProtocolThreadSuspend entry = { thread, stopped_ip };
	protocol_entry (SGEN_PROTOCOL_THREAD_SUSPEND, &entry, sizeof (SGenProtocolThreadSuspend));
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

void
binary_protocol_card_scan (gpointer start, int size)
{
	SGenProtocolCardScan entry = { start, size };
	protocol_entry (SGEN_PROTOCOL_CARD_SCAN, &entry, sizeof (SGenProtocolCardScan));
}

void
binary_protocol_cement (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolCement entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_CEMENT, &entry, sizeof (SGenProtocolCement));
}

void
binary_protocol_cement_reset (void)
{
	protocol_entry (SGEN_PROTOCOL_CEMENT_RESET, NULL, 0);
}

void
binary_protocol_dislink_update (gpointer link, gpointer obj, int track)
{
	SGenProtocolDislinkUpdate entry = { link, obj, track };
	protocol_entry (SGEN_PROTOCOL_DISLINK_UPDATE, &entry, sizeof (SGenProtocolDislinkUpdate));
}

#endif

#endif /* HAVE_SGEN_GC */
