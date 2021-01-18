#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef HAVE_SYS_TYPES_H
#include <sys/types.h>
#endif
#ifdef HAVE_SYS_SELECT_H
#include <sys/select.h>
#endif
#ifdef HAVE_SYS_SOCKET_H
#include <sys/socket.h>
#endif
#ifdef HAVE_NETINET_TCP_H
#include <netinet/tcp.h>
#endif
#ifdef HAVE_NETINET_IN_H
#include <netinet/in.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <glib.h>

#ifdef HAVE_PTHREAD_H
#include <pthread.h>
#endif

#ifdef HOST_WIN32
#ifdef _MSC_VER
#include <winsock2.h>
#include <process.h>
#endif
#include <ws2tcpip.h>
#include <windows.h>
#endif

#include <mono/utils/mono-time.h>
#include <mono/utils/w32api.h>

#include "debugger-protocol.h"
#include <mono/utils/atomic.h>

static int packet_id = 0;

/*
 * Functions to decode protocol data
 */
int  
buffer_add_command_header(Buffer * data, int command_set, int command, Buffer *out)
{
	int id = mono_atomic_inc_i32(&packet_id);

	int len = data->p - data->buf + HEADER_LEN;
	buffer_init(out, len);
	buffer_add_int(out, len);
	buffer_add_int(out, id);
	buffer_add_byte(out, 0); /* flags */
	buffer_add_byte(out, command_set);
	buffer_add_byte(out, command);
	buffer_add_data(out, data->buf, data->p - data->buf);
	return id;
}

void 
decode_command_header(Buffer *recvbuf, Header *header)
{
	header->len = decode_int(recvbuf->buf, &recvbuf->buf, recvbuf->end);
	header->id = decode_int(recvbuf->buf, &recvbuf->buf, recvbuf->end);
	header->flags = decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
	if (header->flags == REPLY_PACKET) {
		header->error = decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
		header->error_2 = decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
	}
	else {
		header->command_set = decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
		header->command = decode_byte(recvbuf->buf, &recvbuf->buf, recvbuf->end);
	}
}

int
decode_byte (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	*endbuf = buf + 1;
	g_assert (*endbuf <= limit);
	return buf [0];
}

int
decode_int (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	*endbuf = buf + 4;
	g_assert (*endbuf <= limit);

	return (((int)buf [0]) << 24) | (((int)buf [1]) << 16) | (((int)buf [2]) << 8) | (((int)buf [3]) << 0);
}

gint64
decode_long (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	guint32 high = decode_int (buf, &buf, limit);
	guint32 low = decode_int (buf, &buf, limit);

	*endbuf = buf;

	return ((((guint64)high) << 32) | ((guint64)low));
}

int
decode_id (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	return decode_int (buf, endbuf, limit);
}

char*
decode_string (guint8 *buf, guint8 **endbuf, guint8 *limit)
{
	int len = decode_int (buf, &buf, limit);
	char *s;

	if (len < 0) {
		*endbuf = buf;
		return NULL;
	}

	s = (char *)g_malloc (len + 1);
	g_assert (s);

	memcpy (s, buf, len);
	s [len] = '\0';
	buf += len;
	*endbuf = buf;

	return s;
}

guint8*
decode_byte_array(guint8* buf, guint8** endbuf, guint8* limit, guint32* len)
{
	*len = decode_int(buf, &buf, limit);
	guint8* s;

	if (len < 0) {
		*endbuf = buf;
		return NULL;
	}

	s = (guint8*)g_malloc(*len);
	g_assert(s);

	memcpy(s, buf, *len);
	buf += *len;
	*endbuf = buf;

	return s;
}

/*
 * Functions to encode protocol data
 */

void
buffer_init (Buffer *buf, int size)
{
	buf->buf = (guint8 *)g_malloc (size);
	buf->p = buf->buf;
	buf->end = buf->buf + size;
}

int
buffer_len (Buffer *buf)
{
	return buf->p - buf->buf;
}

void
buffer_make_room (Buffer *buf, int size)
{
	if (buf->end - buf->p < size) {
		int new_size = buf->end - buf->buf + size + 32;
		guint8 *p = (guint8 *)g_realloc (buf->buf, new_size);
		size = buf->p - buf->buf;
		buf->buf = p;
		buf->p = p + size;
		buf->end = buf->buf + new_size;
	}
}

void
buffer_add_byte (Buffer *buf, guint8 val)
{
	buffer_make_room (buf, 1);
	buf->p [0] = val;
	buf->p++;
}

void
buffer_add_short (Buffer *buf, guint32 val)
{
	buffer_make_room (buf, 2);
	buf->p [0] = (val >> 8) & 0xff;
	buf->p [1] = (val >> 0) & 0xff;
	buf->p += 2;
}

void
buffer_add_int (Buffer *buf, guint32 val)
{
	buffer_make_room (buf, 4);
	buf->p [0] = (val >> 24) & 0xff;
	buf->p [1] = (val >> 16) & 0xff;
	buf->p [2] = (val >> 8) & 0xff;
	buf->p [3] = (val >> 0) & 0xff;
	buf->p += 4;
}

void
buffer_add_long (Buffer *buf, guint64 l)
{
	buffer_add_int (buf, (l >> 32) & 0xffffffff);
	buffer_add_int (buf, (l >> 0) & 0xffffffff);
}

void
buffer_add_id (Buffer *buf, int id)
{
	buffer_add_int (buf, (guint64)id);
}

void
buffer_add_data (Buffer *buf, guint8 *data, int len)
{
	buffer_make_room (buf, len);
	memcpy (buf->p, data, len);
	buf->p += len;
}

void
buffer_add_utf16 (Buffer *buf, guint8 *data, int len)
{
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	buffer_make_room (buf, len);
	memcpy (buf->p, data, len);
#else
	for (int i=0; i<len; i +=2) {
		buf->p[i] = data[i+1];
		buf->p[i+1] = data[i];
	}
#endif
	buf->p += len;
}

void
buffer_add_string (Buffer *buf, const char *str)
{
	int len;

	if (str == NULL) {
		buffer_add_int (buf, 0);
	} else {
		len = strlen (str);
		buffer_add_int (buf, len);
		buffer_add_data (buf, (guint8*)str, len);
	}
}

void
buffer_add_byte_array (Buffer *buf, guint8 *bytes, guint32 arr_len)
{
    buffer_add_int (buf, arr_len);
    buffer_add_data (buf, bytes, arr_len);
}

void
buffer_add_buffer (Buffer *buf, Buffer *data)
{
	buffer_add_data (buf, data->buf, buffer_len (data));
}

void
buffer_free (Buffer *buf)
{
	g_free (buf->buf);
}

const char*
event_to_string (EventKind event)
{
	switch (event) {
	case EVENT_KIND_VM_START: return "VM_START";
	case EVENT_KIND_VM_DEATH: return "VM_DEATH";
	case EVENT_KIND_THREAD_START: return "THREAD_START";
	case EVENT_KIND_THREAD_DEATH: return "THREAD_DEATH";
	case EVENT_KIND_APPDOMAIN_CREATE: return "APPDOMAIN_CREATE";
	case EVENT_KIND_APPDOMAIN_UNLOAD: return "APPDOMAIN_UNLOAD";
	case EVENT_KIND_METHOD_ENTRY: return "METHOD_ENTRY";
	case EVENT_KIND_METHOD_EXIT: return "METHOD_EXIT";
	case EVENT_KIND_ASSEMBLY_LOAD: return "ASSEMBLY_LOAD";
	case EVENT_KIND_ASSEMBLY_UNLOAD: return "ASSEMBLY_UNLOAD";
	case EVENT_KIND_BREAKPOINT: return "BREAKPOINT";
	case EVENT_KIND_STEP: return "STEP";
	case EVENT_KIND_TYPE_LOAD: return "TYPE_LOAD";
	case EVENT_KIND_EXCEPTION: return "EXCEPTION";
	case EVENT_KIND_KEEPALIVE: return "KEEPALIVE";
	case EVENT_KIND_USER_BREAK: return "USER_BREAK";
	case EVENT_KIND_USER_LOG: return "USER_LOG";
	case EVENT_KIND_CRASH: return "CRASH";
	default:
		g_assert_not_reached ();
		return "";
	}
}
