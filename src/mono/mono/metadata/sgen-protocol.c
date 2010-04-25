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
#ifdef BINARY_PROTOCOL

#include "sgen-protocol.h"

/* If not null, dump binary protocol to this file */
static FILE *binary_protocol_file = NULL;

#define BINARY_PROTOCOL_BUFFER_SIZE	65536

static unsigned char binary_protocol_buffer [BINARY_PROTOCOL_BUFFER_SIZE];
static int binary_protocol_buffer_index = 0;

static void
flush_binary_protocol_buffer (void)
{
	if (!binary_protocol_file)
		return;
	if (binary_protocol_buffer_index == 0)
		return;

	fwrite (binary_protocol_buffer, 1, binary_protocol_buffer_index, binary_protocol_file);
	fflush (binary_protocol_file);

	binary_protocol_buffer_index = 0;
}

static void
protocol_entry (unsigned char type, gpointer data, int size)
{
	if (!binary_protocol_file)
		return;
	if (binary_protocol_buffer_index + 1 + size > BINARY_PROTOCOL_BUFFER_SIZE)
		flush_binary_protocol_buffer ();

	binary_protocol_buffer [binary_protocol_buffer_index++] = type;
	memcpy (binary_protocol_buffer + binary_protocol_buffer_index, data, size);
	binary_protocol_buffer_index += size;

	g_assert (binary_protocol_buffer_index <= BINARY_PROTOCOL_BUFFER_SIZE);
}

static void
binary_protocol_collection (int generation)
{
	SGenProtocolCollection entry = { generation };
	flush_binary_protocol_buffer ();
	protocol_entry (SGEN_PROTOCOL_COLLECTION, &entry, sizeof (SGenProtocolCollection));
}

static void
binary_protocol_alloc (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolAlloc entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_ALLOC, &entry, sizeof (SGenProtocolAlloc));
}

static void
binary_protocol_copy (gpointer from, gpointer to, gpointer vtable, int size)
{
	SGenProtocolCopy entry = { from, to, vtable, size };
	protocol_entry (SGEN_PROTOCOL_COPY, &entry, sizeof (SGenProtocolCopy));
}

static void
binary_protocol_pin (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolPin entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_PIN, &entry, sizeof (SGenProtocolPin));
}

static void
binary_protocol_mark (gpointer obj, gpointer vtable, int size)
{
	SGenProtocolMark entry = { obj, vtable, size };
	protocol_entry (SGEN_PROTOCOL_MARK, &entry, sizeof (SGenProtocolMark));
}

static void
binary_protocol_wbarrier (gpointer ptr, gpointer value, gpointer value_vtable)
{
	SGenProtocolWBarrier entry = { ptr, value, value_vtable };
	protocol_entry (SGEN_PROTOCOL_WBARRIER, &entry, sizeof (SGenProtocolWBarrier));
}

static void
binary_protocol_global_remset (gpointer ptr, gpointer value, gpointer value_vtable)
{
	SGenProtocolGlobalRemset entry = { ptr, value, value_vtable };
	protocol_entry (SGEN_PROTOCOL_GLOBAL_REMSET, &entry, sizeof (SGenProtocolGlobalRemset));
}

static void
binary_protocol_ptr_update (gpointer ptr, gpointer old_value, gpointer new_value, gpointer vtable, int size)
{
	SGenProtocolPtrUpdate entry = { ptr, old_value, new_value, vtable, size };
	protocol_entry (SGEN_PROTOCOL_PTR_UPDATE, &entry, sizeof (SGenProtocolPtrUpdate));
}

static void
binary_protocol_cleanup (gpointer ptr, gpointer vtable, int size)
{
	SGenProtocolCleanup entry = { ptr, vtable, size };
	protocol_entry (SGEN_PROTOCOL_CLEANUP, &entry, sizeof (SGenProtocolCleanup));
}

static void
binary_protocol_empty (gpointer start, int size)
{
	SGenProtocolEmpty entry = { start, size };
	protocol_entry (SGEN_PROTOCOL_EMPTY, &entry, sizeof (SGenProtocolEmpty));
}

static void
binary_protocol_thread_restart (gpointer thread)
{
	SGenProtocolThreadRestart entry = { thread };
	protocol_entry (SGEN_PROTOCOL_THREAD_RESTART, &entry, sizeof (SGenProtocolThreadRestart));

}

static void
binary_protocol_thread_register (gpointer thread)
{
	SGenProtocolThreadRegister entry = { thread };
	protocol_entry (SGEN_PROTOCOL_THREAD_REGISTER, &entry, sizeof (SGenProtocolThreadRegister));

}

static void
binary_protocol_thread_unregister (gpointer thread)
{
	SGenProtocolThreadUnregister entry = { thread };
	protocol_entry (SGEN_PROTOCOL_THREAD_UNREGISTER, &entry, sizeof (SGenProtocolThreadUnregister));

}

static void
binary_protocol_missing_remset (gpointer obj, gpointer obj_vtable, int offset, gpointer value, gpointer value_vtable, int value_pinned)
{
	SGenProtocolMissingRemset entry = { obj, obj_vtable, offset, value, value_vtable, value_pinned };
	protocol_entry (SGEN_PROTOCOL_MISSING_REMSET, &entry, sizeof (SGenProtocolMissingRemset));

}

#else

#define binary_protocol_collection(generation)
#define binary_protocol_alloc(obj, vtable, size)
#define binary_protocol_copy(from, to, vtable, size)
#define binary_protocol_pin(obj, vtable, size)
#define binary_protocol_mark(obj, vtable, size)
#define binary_protocol_wbarrier(ptr, value, value_vtable)
#define binary_protocol_global_remset(ptr, value, value_vtable)
#define binary_protocol_ptr_update(ptr, old_value, new_value, vtable, size)
#define binary_protocol_cleanup(ptr, vtable, size)
#define binary_protocol_empty(start, size)
#define binary_protocol_thread_restart(thread)
#define binary_protocol_thread_register(thread)
#define binary_protocol_thread_unregister(thread)
#define binary_protocol_missing_remset(obj, obj_vtable, offset, value, value_vtable, value_pinned)

#endif
