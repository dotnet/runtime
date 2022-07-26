#include "debugger-protocol.h"

#ifdef DBI_COMPONENT_MONO
#include "debugger-coreclr-compat.h"
#else
#include "debugger-mono-compat.h"
#endif

static int32_t packet_id = 0;

/*
 * Functions to decode protocol data
 */
int
m_dbgprot_buffer_add_command_header (MdbgProtBuffer *data, int command_set, int command, MdbgProtBuffer *out)
{
	g_assert (command_set <= UINT8_MAX);
	g_assert (command <= UINT8_MAX);
	int id = dbg_rt_atomic_inc_int32_t ((volatile int32_t *)&packet_id);

	uint32_t len = (uint32_t)(data->p - data->buf + HEADER_LENGTH);
	m_dbgprot_buffer_init (out, len);
	m_dbgprot_buffer_add_int (out, len);
	m_dbgprot_buffer_add_int (out, id);
	m_dbgprot_buffer_add_byte (out, 0); /* flags */
	m_dbgprot_buffer_add_byte (out, (uint8_t)command_set);
	m_dbgprot_buffer_add_byte (out, (uint8_t)command);
	m_dbgprot_buffer_add_data (out, data->buf, (uint32_t) (data->p - data->buf));
	return id;
}

void
m_dbgprot_decode_command_header (MdbgProtBuffer *recvbuf, MdbgProtHeader *header)
{
	header->len = m_dbgprot_decode_int (recvbuf->p, &recvbuf->p, recvbuf->end);
	header->id = m_dbgprot_decode_int (recvbuf->p, &recvbuf->p, recvbuf->end);
	header->flags = m_dbgprot_decode_byte (recvbuf->p, &recvbuf->p, recvbuf->end);
	if (header->flags == REPLY_PACKET) {
		header->error = m_dbgprot_decode_byte (recvbuf->p, &recvbuf->p, recvbuf->end);
		header->error_2 = m_dbgprot_decode_byte (recvbuf->p, &recvbuf->p, recvbuf->end);
	}
	else {
		header->command_set = m_dbgprot_decode_byte (recvbuf->p, &recvbuf->p, recvbuf->end);
		header->command = m_dbgprot_decode_byte (recvbuf->p, &recvbuf->p, recvbuf->end);
	}
}

int
m_dbgprot_decode_byte (uint8_t *buf, uint8_t **endbuf, uint8_t *limit)
{
	*endbuf = buf + 1;
	g_assert (*endbuf <= limit);
	return buf [0];
}

int
m_dbgprot_decode_int (uint8_t *buf, uint8_t **endbuf, uint8_t *limit)
{
	*endbuf = buf + 4;
	g_assert (*endbuf <= limit);
	return (((int)buf [0]) << 24) | (((int)buf [1]) << 16) | (((int)buf [2]) << 8) | (((int)buf [3]) << 0);
}

int64_t
m_dbgprot_decode_long (uint8_t *buf, uint8_t **endbuf, uint8_t *limit)
{
	uint32_t high = m_dbgprot_decode_int (buf, &buf, limit);
	uint32_t low = m_dbgprot_decode_int (buf, &buf, limit);

	*endbuf = buf;

	return ((((uint64_t)high) << 32) | ((uint64_t)low));
}

int
m_dbgprot_decode_id (uint8_t *buf, uint8_t **endbuf, uint8_t *limit)
{
	return m_dbgprot_decode_int (buf, endbuf, limit);
}

char*
m_dbgprot_decode_string (uint8_t *buf, uint8_t **endbuf, uint8_t *limit)
{
	int len = m_dbgprot_decode_int (buf, &buf, limit);
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

char*
m_dbgprot_decode_string_with_len(uint8_t* buf, uint8_t** endbuf, uint8_t* limit, int *len)
{
	*len = m_dbgprot_decode_int(buf, &buf, limit);
	char* s;

	if (*len < 0) {
		*endbuf = buf;
		return NULL;
	}

	s = (char*)g_malloc(*len + 1);
	g_assert(s);

	memcpy(s, buf, *len);
	s[*len] = '\0';
	buf += *len;
	*endbuf = buf;

	return s;
}

uint8_t*
m_dbgprot_decode_byte_array (uint8_t *buf, uint8_t **endbuf, uint8_t *limit, int32_t *len)
{
	*len = m_dbgprot_decode_int (buf, &buf, limit);
	uint8_t* s;

	if (*len < 0) {
		*endbuf = buf;
		return NULL;
	}

	s = (uint8_t*)g_malloc (*len);
	g_assert (s);

	memcpy (s, buf, *len);
	buf += *len;
	*endbuf = buf;

	return s;
}

/*
 * Functions to encode protocol data
 */

void
m_dbgprot_buffer_init (MdbgProtBuffer *buf, uint32_t size)
{
	buf->buf = (uint8_t *)g_malloc (size);
	buf->p = buf->buf;
	buf->end = buf->buf + size;
}

uint32_t
m_dbgprot_buffer_len (MdbgProtBuffer *buf)
{
	return (uint32_t)(buf->p - buf->buf);
}

void
m_dbgprot_buffer_make_room (MdbgProtBuffer *buf, uint32_t size)
{
	if (((uint32_t)(buf->end - buf->p)) < size) {
		size_t new_size = buf->end - buf->buf + size + 32;
		uint8_t *p = (uint8_t *)g_realloc (buf->buf, new_size);
		size = (uint32_t) (buf->p - buf->buf);
		buf->buf = p;
		buf->p = p + size;
		buf->end = buf->buf + new_size;
	}
}

void
m_dbgprot_buffer_add_byte (MdbgProtBuffer *buf, uint8_t val)
{
	m_dbgprot_buffer_make_room (buf, 1);
	buf->p [0] = val;
	buf->p++;
}

void
m_dbgprot_buffer_add_short (MdbgProtBuffer *buf, uint32_t val)
{
	m_dbgprot_buffer_make_room (buf, 2);
	buf->p [0] = (val >> 8) & 0xff;
	buf->p [1] = (val >> 0) & 0xff;
	buf->p += 2;
}

void
m_dbgprot_buffer_add_int (MdbgProtBuffer *buf, uint32_t val)
{
	m_dbgprot_buffer_make_room (buf, 4);
	buf->p [0] = (val >> 24) & 0xff;
	buf->p [1] = (val >> 16) & 0xff;
	buf->p [2] = (val >> 8) & 0xff;
	buf->p [3] = (val >> 0) & 0xff;
	buf->p += 4;
}

void
m_dbgprot_buffer_add_long (MdbgProtBuffer *buf, uint64_t l)
{
	m_dbgprot_buffer_add_int (buf, (l >> 32) & 0xffffffff);
	m_dbgprot_buffer_add_int (buf, (l >> 0) & 0xffffffff);
}

void
m_dbgprot_buffer_add_id (MdbgProtBuffer *buf, uint32_t id)
{
	m_dbgprot_buffer_add_int (buf, id);
}

void
m_dbgprot_buffer_add_data (MdbgProtBuffer *buf, uint8_t *data, uint32_t len)
{
	m_dbgprot_buffer_make_room (buf, len);
	memcpy (buf->p, data, len);
	buf->p += len;
}

void
m_dbgprot_buffer_add_utf16 (MdbgProtBuffer *buf, uint8_t *data, uint32_t len)
{
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
	m_dbgprot_buffer_make_room (buf, len);
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
m_dbgprot_buffer_add_string (MdbgProtBuffer *buf, const char *str)
{
	uint32_t len;

	if (str == NULL) {
		m_dbgprot_buffer_add_int (buf, 0);
	} else {
		len = (uint32_t) strlen (str);
		m_dbgprot_buffer_add_int (buf, len);
		m_dbgprot_buffer_add_data (buf, (uint8_t*)str, len);
	}
}

void
m_dbgprot_buffer_add_byte_array (MdbgProtBuffer *buf, uint8_t *bytes, uint32_t arr_len)
{
    m_dbgprot_buffer_add_int (buf, arr_len);
    m_dbgprot_buffer_add_data (buf, bytes, arr_len);
}

void
m_dbgprot_buffer_add_buffer (MdbgProtBuffer *buf, MdbgProtBuffer *data)
{
	m_dbgprot_buffer_add_data (buf, data->buf, m_dbgprot_buffer_len (data));
}

void
m_dbgprot_buffer_free (MdbgProtBuffer *buf)
{
	g_free (buf->buf);
}

const char*
m_dbgprot_event_to_string (MdbgProtEventKind event)
{
	switch (event) {
	case MDBGPROT_EVENT_KIND_VM_START: return "VM_START";
	case MDBGPROT_EVENT_KIND_VM_DEATH: return "VM_DEATH";
	case MDBGPROT_EVENT_KIND_THREAD_START: return "THREAD_START";
	case MDBGPROT_EVENT_KIND_THREAD_DEATH: return "THREAD_DEATH";
	case MDBGPROT_EVENT_KIND_APPDOMAIN_CREATE: return "APPDOMAIN_CREATE";
	case MDBGPROT_EVENT_KIND_APPDOMAIN_UNLOAD: return "APPDOMAIN_UNLOAD";
	case MDBGPROT_EVENT_KIND_METHOD_ENTRY: return "METHOD_ENTRY";
	case MDBGPROT_EVENT_KIND_METHOD_EXIT: return "METHOD_EXIT";
	case MDBGPROT_EVENT_KIND_ASSEMBLY_LOAD: return "ASSEMBLY_LOAD";
	case MDBGPROT_EVENT_KIND_ASSEMBLY_UNLOAD: return "ASSEMBLY_UNLOAD";
	case MDBGPROT_EVENT_KIND_BREAKPOINT: return "BREAKPOINT";
	case MDBGPROT_EVENT_KIND_STEP: return "STEP";
	case MDBGPROT_EVENT_KIND_TYPE_LOAD: return "TYPE_LOAD";
	case MDBGPROT_EVENT_KIND_EXCEPTION: return "EXCEPTION";
	case MDBGPROT_EVENT_KIND_KEEPALIVE: return "KEEPALIVE";
	case MDBGPROT_EVENT_KIND_USER_BREAK: return "USER_BREAK";
	case MDBGPROT_EVENT_KIND_USER_LOG: return "USER_LOG";
	case MDBGPROT_EVENT_KIND_CRASH: return "CRASH";
	default:
		g_assert ( 1 );
		return "";
	}
}
