/*
 * proflog.c: mono log profiler
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 */

#include <config.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/debug-helpers.h>
#include <stdlib.h>
#include <string.h>
#if defined (HAVE_ZLIB)
#include <zlib.h>
#endif
#include <assert.h>
#ifdef HOST_WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif

#include "utils.c"
#include "proflog.h"

#define BUFFER_SIZE (4096 * 16)
static int nocalls = 0;
static int notraces = 0;
static int use_zip = 0;
static int do_report = 0;
static int do_heap_shot = 0;
static int max_call_depth = 100;
static int runtime_inited = 0;

/* For linux compile with:
 * gcc -fPIC -shared -o libmono-profiler-log.so proflog.c utils.c -Wall -g -lz `pkg-config --cflags --libs mono-2`
 * gcc -o mprof-report decode.c utils.c -Wall -g -lz -lrt -lpthread `pkg-config --cflags mono-2`
 *
 * For osx compile with:
 * gcc -m32 -Dmono_free=free shared -o libmono-profiler-log.dylib proflog.c utils.c -Wall -g -lz `pkg-config --cflags mono-2` -undefined suppress -flat_namespace
 * gcc -m32 -o mprof-report decode.c utils.c -Wall -g -lz -lrt -lpthread `pkg-config --cflags mono-2`
 *
 * Install with:
 * sudo cp mprof-report /usr/local/bin
 * sudo cp libmono-profiler-log.so /usr/local/lib
 * sudo ldconfig
 */

typedef struct _LogBuffer LogBuffer;

/*
 * file format:
 * [header] [buffer]*
 *
 * The file is composed by a header followed by 0 or more buffers.
 * Each buffer contains events that happened on a thread: for a given thread
 * buffers that appear later in the file are guaranteed to contain events
 * that happened later in time. Buffers from separate threads could be interleaved,
 * though.
 * Buffers are not required to be aligned.
 *
 * header format:
 * [id: 4 bytes] constant value: LOG_HEADER_ID
 * [major: 1 byte] [minor: 1 byte] major and minor version of the log profiler
 * [format: 1 byte] version of the data format for the rest of the file
 * [ptrsize: 1 byte] size in bytes of a pointer in the profiled program
 * [startup time: 8 bytes] time in milliseconds since the unix epoch when the program started
 * [timer overhead: 4 bytes] approximate overhead in nanoseconds of the timer
 * [flags: 4 bytes] file format flags, should be 0 for now
 * [pid: 4 bytes] pid of the profiled process
 * [sysid: 4 bytes] operating system and architecture identifier
 *
 * The multiple byte integers are in little-endian format.
 *
 * buffer format:
 * [buffer header] [event]*
 * Buffers have a fixed-size header followed by 0 or more bytes of event data.
 * Timing information and other values in the event data are usually stored
 * as uleb128 or sleb128 integers. To save space, as noted for each item below,
 * some data is represented as a difference between the actual value and
 * either the last value of the same type (like for timing information) or
 * as the difference from a value stored in a buffer header.
 *
 * For timing information the data is stored as uleb128, since timing
 * increases in a monotonic way in each thread: the value is the number of
 * nanoseconds to add to the last seen timing data in a buffer. The first value
 * in a buffer will be calculated from the time_base field in the buffer head.
 *
 * Object or heap sizes are stored as uleb128.
 * Pointer differences are stored as sleb128, instead.
 *
 * If an unexpected value is found, the rest of the buffer should be ignored,
 * as generally the later values need the former to be interpreted correctly.
 *
 * buffer header format:
 * [bufid: 4 bytes] constant value: BUF_ID
 * [len: 4 bytes] size of the data following the buffer header
 * [time_base: 8 bytes] time base in nanoseconds since an unspecified epoch
 * [ptr_base: 8 bytes] base value for pointers
 * [obj_base: 8 bytes] base value for object addresses
 * [thread id: 8 bytes] system-specific thread ID (pthread_t for example)
 * [method_base: 8 bytes] base value for MonoMethod pointers
 *
 * event format:
 * [extended info: upper 4 bits] [type: lower 4 bits] [data]*
 * The data that follows depends on type and the extended info.
 * Type is one of the enum values in proflog.h: TYPE_ALLOC, TYPE_GC,
 * TYPE_METADATA, TYPE_METHOD, TYPE_EXCEPTION, TYPE_MONITOR, TYPE_HEAP.
 * The extended info bits are interpreted based on type, see
 * each individual event description below.
 * strings are represented as a 0-terminated utf8 sequence.
 *
 * backtrace format:
 * [flags: uleb128] must be 0
 * [num: uleb128] number of frames following
 * [frame: sleb128]* num MonoMethod pointers as differences from ptr_base
 *
 * type alloc format:
 * type: TYPE_ALLOC
 * exinfo: flags: TYPE_ALLOC_BT
 * [time diff: uleb128] nanoseconds since last timing
 * [ptr: sleb128] class as a byte difference from ptr_base
 * [obj: sleb128] object address as a byte difference from obj_base
 * [size: uleb128] size of the object in the heap
 * If the TYPE_ALLOC_BT flag is set, a backtrace follows.
 *
 * type GC format:
 * type: TYPE_GC
 * exinfo: one of TYPE_GC_EVENT, TYPE_GC_RESIZE, TYPE_GC_MOVE
 * [time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_GC_RESIZE
 *	[heap_size: uleb128] new heap size
 * if exinfo == TYPE_GC_EVENT
 *	[event type: uleb128] GC event (MONO_GC_EVENT_* from profiler.h)
 *	[generation: uleb128] GC generation event refers to
 * if exinfo == TYPE_GC_MOVE
 *	[num_objects: uleb128] number of object moves that follow
 *	[objaddr: sleb128]+ num_objects object pointer differences from obj_base
 *	num is always an even number: the even items are the old
 *	addresses, the odd numbers are the respective new object addresses
 *
 * type metadata format:
 * type: TYPE_METADATA
 * exinfo: flags: TYPE_LOAD_ERR
 * [time diff: uleb128] nanoseconds since last timing
 * [mtype: byte] metadata type, one of: TYPE_CLASS, TYPE_IMAGE, TYPE_ASSEMBLY, TYPE_DOMAIN
 * [pointer: sleb128] pointer of the metadata type depending on mtype
 * if mtype == TYPE_CLASS
 *	[image: sleb128] MonoImage* as a pointer difference from ptr_base
 * 	[flags: uleb128] must be 0
 * 	[name: string] full class name
 * if mtype == TYPE_IMAGE
 * 	[flags: uleb128] must be 0
 * 	[name: string] image file name
 *
 * type method format:
 * type: TYPE_METHOD
 * exinfo: one of: TYPE_LEAVE, TYPE_ENTER, TYPE_EXC_LEAVE, TYPE_JIT
 * [time diff: uleb128] nanoseconds since last timing
 * [method: sleb128] MonoMethod* as a pointer difference from the last such
 * pointer or the buffer method_base
 * if exinfo == TYPE_JIT
 *	[code address: sleb128] pointer to the native code as a diff from ptr_base
 *	[code size: uleb128] size of the generated code
 *	[name: string] full method name
 *
 * type exception format:
 * type: TYPE_EXCEPTION
 * exinfo: TYPE_EXCEPTION_BT flag and one of: TYPE_THROW, TYPE_CLAUSE
 * [time diff: uleb128] nanoseconds since last timing
 * if exinfo.low3bits == TYPE_CLAUSE
 * 	[clause type: uleb128] finally/catch/fault/filter
 * 	[clause num: uleb128] the clause number in the method header
 * 	[method: sleb128] MonoMethod* as a pointer difference from the last such
 * 	pointer or the buffer method_base
 * if exinfo.low3bits == TYPE_THROW
 * 	[object: sleb128] the object that was thrown as a difference from obj_base
 *	If the TYPE_EXCEPTION_BT flag is set, a backtrace follows.
 *
 * type monitor format:
 * type: TYPE_MONITOR
 * exinfo: TYPE_MONITOR_BT flag and one of: MONO_PROFILER_MONITOR_(CONTENTION|FAIL|DONE)
 * [time diff: uleb128] nanoseconds since last timing
 * [object: sleb128] the lock object as a difference from obj_base
 * if exinfo.low3bits == MONO_PROFILER_MONITOR_CONTENTION
 *	If the TYPE_MONITOR_BT flag is set, a backtrace follows.
 *
 * type heap format
 * type: TYPE_HEAP
 * exinfo: one of TYPE_HEAP_START, TYPE_HEAP_END, TYPE_HEAP_OBJECT
 * if exinfo == TYPE_HEAP_START
 * 	[time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_HEAP_END
 * 	[time diff: uleb128] nanoseconds since last timing
 * if exinfo == TYPE_HEAP_OBJECT
 * 	[object: sleb128] the object as a difference from obj_base
 * 	[class: sleb128] the object MonoClass* as a difference from ptr_base
 * 	[size: uleb128] size of the object on the heap
 * 	[num_refs: uleb128] number of object references
 * 	[objrefs: sleb128]+ object referenced as a difference from obj_base
 *
 */
struct _LogBuffer {
	LogBuffer *next;
	uint64_t time_base;
	uint64_t last_time;
	uintptr_t ptr_base;
	uintptr_t method_base;
	uintptr_t last_method;
	uintptr_t obj_base;
	uintptr_t thread_id;
	unsigned char* data_end;
	unsigned char* data;
	int size;
	int call_depth;
	unsigned char buf [1];
};

struct _MonoProfiler {
	LogBuffer *buffers;
	FILE* file;
#if defined (HAVE_ZLIB)
	gzFile *gzfile;
#endif
	int pipe_output;
	int last_gc_gen_started;
};

#ifdef HOST_WIN32
#define TLS_SET(x,y) TlsSetValue(x, y)
#define TLS_GET(x) ((LogBuffer *) TlsGetValue(x))
#define TLS_INIT(x) x = TlsAlloc ()
static int tlsbuffer;
#elif HAVE_KW_THREAD
#define TLS_SET(x,y) x = y
#define TLS_GET(x) x
#define TLS_INIT(x)
static __thread LogBuffer* tlsbuffer = NULL;
#else
#define TLS_SET(x,y) pthread_setspecific(x, y)
#define TLS_GET(x) ((LogBuffer *) pthread_getspecific(x))
#define TLS_INIT(x) pthread_key_create(&x, NULL)
static pthread_key_t tlsbuffer;
#endif

static LogBuffer*
create_buffer (void)
{
	LogBuffer* buf = alloc_buffer (BUFFER_SIZE);
	buf->size = BUFFER_SIZE;
	buf->time_base = current_time ();
	buf->last_time = buf->time_base;
	buf->data_end = (unsigned char*)buf + buf->size;
	buf->data = buf->buf;
	return buf;
}

static void
init_thread (void)
{
	LogBuffer *logbuffer;
	if (TLS_GET (tlsbuffer))
		return;
	logbuffer = create_buffer ();
	TLS_SET (tlsbuffer, logbuffer);
	logbuffer->thread_id = thread_id ();
	//printf ("thread %p at time %llu\n", (void*)logbuffer->thread_id, logbuffer->time_base);
}

static LogBuffer*
ensure_logbuf (int bytes)
{
	LogBuffer *old = TLS_GET (tlsbuffer);
	if (old && old->data + bytes + 100 < old->data_end)
		return old;
	TLS_SET (tlsbuffer, NULL);
	init_thread ();
	TLS_GET (tlsbuffer)->next = old;
	if (old)
		TLS_GET (tlsbuffer)->call_depth = old->call_depth;
	//printf ("new logbuffer\n");
	return TLS_GET (tlsbuffer);
}

static void
emit_byte (LogBuffer *logbuffer, int value)
{
	logbuffer->data [0] = value;
	logbuffer->data++;
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_value (LogBuffer *logbuffer, int value)
{
	encode_uleb128 (value, logbuffer->data, &logbuffer->data);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_time (LogBuffer *logbuffer, uint64_t value)
{
	uint64_t tdiff = value - logbuffer->last_time;
	unsigned char *p;
	if (value < logbuffer->last_time)
		printf ("time went backwards\n");
	//if (tdiff > 1000000)
	//	printf ("large time offset: %llu\n", tdiff);
	p = logbuffer->data;
	encode_uleb128 (tdiff, logbuffer->data, &logbuffer->data);
	/*if (tdiff != decode_uleb128 (p, &p))
		printf ("incorrect encoding: %llu\n", tdiff);*/
	logbuffer->last_time = value;
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_svalue (LogBuffer *logbuffer, int64_t value)
{
	encode_sleb128 (value, logbuffer->data, &logbuffer->data);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_ptr (LogBuffer *logbuffer, void *ptr)
{
	if (!logbuffer->ptr_base)
		logbuffer->ptr_base = (uintptr_t)ptr;
	emit_svalue (logbuffer, (intptr_t)ptr - logbuffer->ptr_base);
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_method (LogBuffer *logbuffer, void *method)
{
	if (!logbuffer->method_base) {
		logbuffer->method_base = (intptr_t)method;
		logbuffer->last_method = (intptr_t)method;
	}
	encode_sleb128 ((intptr_t)((char*)method - (char*)logbuffer->last_method), logbuffer->data, &logbuffer->data);
	logbuffer->last_method = (intptr_t)method;
	assert (logbuffer->data <= logbuffer->data_end);
}

static void
emit_obj (LogBuffer *logbuffer, void *ptr)
{
	if (!logbuffer->obj_base)
		logbuffer->obj_base = (uintptr_t)ptr >> 3;
	emit_svalue (logbuffer, ((uintptr_t)ptr >> 3) - logbuffer->obj_base);
	assert (logbuffer->data <= logbuffer->data_end);
}

static char*
write_int32 (char *buf, int32_t value)
{
	int i;
	for (i = 0; i < 4; ++i) {
		buf [i] = value;
		value >>= 8;
	}
	return buf + 4;
}

static char*
write_int64 (char *buf, int64_t value)
{
	int i;
	for (i = 0; i < 8; ++i) {
		buf [i] = value;
		value >>= 8;
	}
	return buf + 8;
}

static void
dump_header (MonoProfiler *profiler)
{
	char hbuf [128];
	char *p = hbuf;
	p = write_int32 (p, LOG_HEADER_ID);
	*p++ = LOG_VERSION_MAJOR;
	*p++ = LOG_VERSION_MINOR;
	*p++ = LOG_DATA_VERSION;
	*p++ = sizeof (void*);
	p = write_int64 (p, 0); /* startup time */
	p = write_int32 (p, 0); /* timer overhead */
	p = write_int32 (p, 0); /* flags */
	p = write_int32 (p, 0); /* pid */
	p = write_int32 (p, 0); /* opsystem */
#if defined (HAVE_ZLIB)
	if (profiler->gzfile) {
		gzwrite (profiler->gzfile, hbuf, p - hbuf);
	} else {
		fwrite (hbuf, p - hbuf, 1, profiler->file);
	}
#else
	fwrite (hbuf, p - hbuf, 1, profiler->file);
#endif
}

static void
dump_buffer (MonoProfiler *profiler, LogBuffer *buf)
{
	char hbuf [128];
	char *p = hbuf;
	if (buf->next)
		dump_buffer (profiler, buf->next);
	p = write_int32 (p, BUF_ID);
	p = write_int32 (p, buf->data - buf->buf);
	p = write_int64 (p, buf->time_base);
	p = write_int64 (p, buf->ptr_base);
	p = write_int64 (p, buf->obj_base);
	p = write_int64 (p, buf->thread_id);
	p = write_int64 (p, buf->method_base);
#if defined (HAVE_ZLIB)
	if (profiler->gzfile) {
		gzwrite (profiler->gzfile, hbuf, p - hbuf);
		gzwrite (profiler->gzfile, buf->buf, buf->data - buf->buf);
	} else {
#endif
		fwrite (hbuf, p - hbuf, 1, profiler->file);
		fwrite (buf->buf, buf->data - buf->buf, 1, profiler->file);
#if defined (HAVE_ZLIB)
	}
#endif
	free_buffer (buf, buf->size);
}

static void
runtime_initialized (MonoProfiler *profiler)
{
	runtime_inited = 1;
}

/*
 * Can be called only at safe callback locations.
 */
static void
safe_dump (MonoProfiler *profiler, LogBuffer *logbuffer)
{
	int cd = logbuffer->call_depth;
	take_lock ();
	dump_buffer (profiler, TLS_GET (tlsbuffer));
	release_lock ();
	TLS_SET (tlsbuffer, NULL);
	init_thread ();
	TLS_GET (tlsbuffer)->call_depth = cd;
}

static int
gc_reference (MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num, MonoObject **refs, void *data)
{
	int i;
	//const char *name = mono_class_get_name (klass);
	LogBuffer *logbuffer = ensure_logbuf (20 + num * 8);
	emit_byte (logbuffer, TYPE_HEAP_OBJECT | TYPE_HEAP);
	emit_obj (logbuffer, obj);
	emit_ptr (logbuffer, klass);
	/* account for object alignment in the heap */
	size += 7;
	size &= ~7;
	emit_value (logbuffer, size);
	emit_value (logbuffer, num);
	for (i = 0; i < num; ++i)
		emit_obj (logbuffer, refs [i]);
	//if (num)
	//	printf ("obj: %p, klass: %s, refs: %d, size: %d\n", obj, name, (int)num, (int)size);
	return 0;
}

static unsigned int hs_mode_ms = 0;
static unsigned int hs_mode_gc = 0;
static unsigned int gc_count = 0;
static uint64_t last_hs_time = 0;

static void
heap_walk (MonoProfiler *profiler)
{
	int do_walk = 0;
	uint64_t now;
	LogBuffer *logbuffer;
	if (!do_heap_shot)
		return;
	logbuffer = ensure_logbuf (10);
	now = current_time ();
	if (hs_mode_ms && (now - last_hs_time)/1000000 >= hs_mode_ms)
		do_walk = 1;
	else if (hs_mode_gc && (gc_count % hs_mode_gc) == 0)
		do_walk = 1;
	else if (!hs_mode_ms && !hs_mode_gc && profiler->last_gc_gen_started == mono_gc_max_generation ())
		do_walk = 1;

	if (!do_walk)
		return;
	emit_byte (logbuffer, TYPE_HEAP_START | TYPE_HEAP);
	emit_time (logbuffer, now);
	mono_gc_walk_heap (0, gc_reference, NULL);
	logbuffer = ensure_logbuf (10);
	now = current_time ();
	emit_byte (logbuffer, TYPE_HEAP_END | TYPE_HEAP);
	emit_time (logbuffer, now);
}

static void
gc_event (MonoProfiler *profiler, MonoGCEvent ev, int generation) {
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (10);
	now = current_time ();
	emit_byte (logbuffer, TYPE_GC_EVENT | TYPE_GC);
	emit_time (logbuffer, now);
	emit_value (logbuffer, ev);
	emit_value (logbuffer, generation);
	/* to deal with nested gen1 after gen0 started */
	if (ev == MONO_GC_EVENT_START) {
		profiler->last_gc_gen_started = generation;
		gc_count++;
	}
	if (ev == MONO_GC_EVENT_PRE_START_WORLD)
		heap_walk (profiler);
	if (ev == MONO_GC_EVENT_POST_START_WORLD)
		safe_dump (profiler, logbuffer);
	//printf ("gc event %d for generation %d\n", ev, generation);
}

static void
gc_resize (MonoProfiler *profiler, int64_t new_size) {
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (10);
	now = current_time ();
	emit_byte (logbuffer, TYPE_GC_RESIZE | TYPE_GC);
	emit_time (logbuffer, now);
	emit_value (logbuffer, new_size);
	//printf ("gc resized to %lld\n", new_size);
}

#define MAX_FRAMES 16
typedef struct {
	int count;
	MonoMethod* methods [MAX_FRAMES];
} FrameData;
static int num_frames = MAX_FRAMES / 2;

static mono_bool
walk_stack (MonoMethod *method, int32_t native_offset, int32_t il_offset, mono_bool managed, void* data)
{
	FrameData *frame = data;
	if (method && frame->count < num_frames) {
		frame->methods [frame->count++] = method;
		//printf ("In %d %s\n", frame->count, mono_method_get_name (method));
	}
	return frame->count == num_frames;
}

/*
 * a note about stack walks: they can cause more profiler events to fire,
 * so we need to make sure they don't happen after we started emitting an
 * event, hence the collect_bt/emit_bt split.
 */
static void
collect_bt (FrameData *data)
{
	data->count = 0;
	mono_stack_walk_no_il (walk_stack, data);
}

static void
emit_bt (LogBuffer *logbuffer, FrameData *data)
{
	/* FIXME: this is actually tons of data and we should
	 * just output it the first time and use an id the next
	 */
	if (data->count > num_frames)
		printf ("bad num frames: %d\n", data->count);
	emit_value (logbuffer, 0); /* flags */
	emit_value (logbuffer, data->count);
	//if (*p != data.count) {
	//	printf ("bad num frames enc at %d: %d -> %d\n", count, data.count, *p); printf ("frames end: %p->%p\n", p, logbuffer->data); exit(0);}
	while (data->count) {
		emit_ptr (logbuffer, data->methods [--data->count]);
	}
}

static void
gc_alloc (MonoProfiler *prof, MonoObject *obj, MonoClass *klass)
{
	uint64_t now;
	uintptr_t len;
	int do_bt = (nocalls && runtime_inited && !notraces)? TYPE_ALLOC_BT: 0;
	FrameData data;
	LogBuffer *logbuffer;
	len = mono_object_get_size (obj);
	/* account for object alignment in the heap */
	len += 7;
	len &= ~7;
	if (do_bt)
		collect_bt (&data);
	logbuffer = ensure_logbuf (32 + MAX_FRAMES * 8);
	now = current_time ();
	emit_byte (logbuffer, do_bt | TYPE_ALLOC);
	emit_time (logbuffer, now);
	emit_ptr (logbuffer, klass);
	emit_obj (logbuffer, obj);
	emit_value (logbuffer, len);
	if (do_bt)
		emit_bt (logbuffer, &data);
	if (logbuffer->next)
		safe_dump (prof, logbuffer);
	//printf ("gc alloc %s at %p\n", mono_class_get_name (klass), obj);
}

static void
gc_moves (MonoProfiler *prof, void **objects, int num)
{
	int i;
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (10 + num * 8);
	now = current_time ();
	emit_byte (logbuffer, TYPE_GC_MOVE | TYPE_GC);
	emit_time (logbuffer, now);
	emit_value (logbuffer, num);
	for (i = 0; i < num; ++i)
		emit_obj (logbuffer, objects [i]);
	//printf ("gc moved %d objects\n", num/2);
}

static char*
push_nesting (char *p, MonoClass *klass)
{
	MonoClass *nesting;
	const char *name;
	const char *nspace;
	nesting = mono_class_get_nesting_type (klass);
	if (nesting) {
		p = push_nesting (p, nesting);
		*p++ = '/';
		*p = 0;
	}
	name = mono_class_get_name (klass);
	nspace = mono_class_get_namespace (klass);
	if (*nspace) {
		strcpy (p, nspace);
		p += strlen (nspace);
		*p++ = '.';
		*p = 0;
	}
	strcpy (p, name);
	p += strlen (name);
	return p;
}

static char*
type_name (MonoClass *klass)
{
	char buf [1024];
	char *p;
	push_nesting (buf, klass);
	p = malloc (strlen (buf) + 1);
	strcpy (p, buf);
	return p;
}

static void
image_loaded (MonoProfiler *prof, MonoImage *image, int result)
{
	uint64_t now;
	const char *name;
	int nlen;
	LogBuffer *logbuffer;
	if (result != MONO_PROFILE_OK)
		return;
	name = mono_image_get_filename (image);
	nlen = strlen (name) + 1;
	logbuffer = ensure_logbuf (16 + nlen);
	now = current_time ();
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_IMAGE);
	emit_ptr (logbuffer, image);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	//printf ("loaded image %p (%s)\n", image, name);
	if (logbuffer->next)
		safe_dump (prof, logbuffer);
}

static void
class_loaded (MonoProfiler *prof, MonoClass *klass, int result)
{
	uint64_t now;
	char *name;
	int nlen;
	MonoImage *image;
	LogBuffer *logbuffer;
	if (result != MONO_PROFILE_OK)
		return;
	if (runtime_inited)
		name = mono_type_get_name (mono_class_get_type (klass));
	else
		name = type_name (klass);
	nlen = strlen (name) + 1;
	image = mono_class_get_image (klass);
	logbuffer = ensure_logbuf (24 + nlen);
	now = current_time ();
	emit_byte (logbuffer, TYPE_END_LOAD | TYPE_METADATA);
	emit_time (logbuffer, now);
	emit_byte (logbuffer, TYPE_CLASS);
	emit_ptr (logbuffer, klass);
	emit_ptr (logbuffer, image);
	emit_value (logbuffer, 0); /* flags */
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	//printf ("loaded class %p (%s)\n", klass, name);
	if (runtime_inited)
		mono_free (name);
	else
		free (name);
	if (logbuffer->next)
		safe_dump (prof, logbuffer);
}

static void
method_enter (MonoProfiler *prof, MonoMethod *method)
{
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (16);
	if (logbuffer->call_depth++ > max_call_depth)
		return;
	now = current_time ();
	emit_byte (logbuffer, TYPE_ENTER | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (logbuffer, method);
}

static void
method_leave (MonoProfiler *prof, MonoMethod *method)
{
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (16);
	if (--logbuffer->call_depth > max_call_depth)
		return;
	now = current_time ();
	emit_byte (logbuffer, TYPE_LEAVE | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (logbuffer, method);
	if (logbuffer->next)
		safe_dump (prof, logbuffer);
}

static void
method_exc_leave (MonoProfiler *prof, MonoMethod *method)
{
	uint64_t now;
	LogBuffer *logbuffer;
	if (nocalls)
		return;
	logbuffer = ensure_logbuf (16);
	if (--logbuffer->call_depth > max_call_depth)
		return;
	now = current_time ();
	emit_byte (logbuffer, TYPE_EXC_LEAVE | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (logbuffer, method);
}

static void
method_jitted (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo, int result)
{
	uint64_t now;
	char *name;
	int nlen;
	LogBuffer *logbuffer;
	if (result != MONO_PROFILE_OK)
		return;
	name = mono_method_full_name (method, 1);
	nlen = strlen (name) + 1;
	logbuffer = ensure_logbuf (32 + nlen);
	now = current_time ();
	emit_byte (logbuffer, TYPE_JIT | TYPE_METHOD);
	emit_time (logbuffer, now);
	emit_method (logbuffer, method);
	emit_ptr (logbuffer, mono_jit_info_get_code_start (jinfo));
	emit_value (logbuffer, mono_jit_info_get_code_size (jinfo));
	memcpy (logbuffer->data, name, nlen);
	logbuffer->data += nlen;
	mono_free (name);
	if (logbuffer->next)
		safe_dump (prof, logbuffer);
}

static void
throw_exc (MonoProfiler *prof, MonoObject *object)
{
	int do_bt = (nocalls && runtime_inited && !notraces)? TYPE_EXCEPTION_BT: 0;
	uint64_t now;
	FrameData data;
	LogBuffer *logbuffer;
	if (do_bt)
		collect_bt (&data);
	logbuffer = ensure_logbuf (16 + MAX_FRAMES * 8);
	now = current_time ();
	emit_byte (logbuffer, do_bt | TYPE_EXCEPTION);
	emit_time (logbuffer, now);
	emit_obj (logbuffer, object);
	if (do_bt)
		emit_bt (logbuffer, &data);
}

static void
clause_exc (MonoProfiler *prof, MonoMethod *method, int clause_type, int clause_num)
{
	uint64_t now;
	LogBuffer *logbuffer = ensure_logbuf (16);
	now = current_time ();
	emit_byte (logbuffer, TYPE_EXCEPTION | TYPE_CLAUSE);
	emit_time (logbuffer, now);
	emit_value (logbuffer, clause_type);
	emit_value (logbuffer, clause_num);
	emit_method (logbuffer, method);
}

static void
monitor_event (MonoProfiler *profiler, MonoObject *object, MonoProfilerMonitorEvent event)
{
	int do_bt = (nocalls && runtime_inited && !notraces && event == MONO_PROFILER_MONITOR_CONTENTION)? TYPE_MONITOR_BT: 0;
	uint64_t now;
	FrameData data;
	LogBuffer *logbuffer;
	if (do_bt)
		collect_bt (&data);
	logbuffer = ensure_logbuf (16 + MAX_FRAMES * 8);
	now = current_time ();
	emit_byte (logbuffer, (event << 4) | do_bt | TYPE_MONITOR);
	emit_time (logbuffer, now);
	emit_obj (logbuffer, object);
	if (do_bt)
		emit_bt (logbuffer, &data);
}

static void
thread_start (MonoProfiler *prof, uintptr_t tid)
{
	//printf ("thread start %p\n", (void*)tid);
	init_thread ();
}

static void
thread_end (MonoProfiler *prof, uintptr_t tid)
{
	take_lock ();
	if (TLS_GET (tlsbuffer))
		dump_buffer (prof, TLS_GET (tlsbuffer));
	release_lock ();
	TLS_SET (tlsbuffer, NULL);
}

static void
log_shutdown (MonoProfiler *prof)
{
	take_lock ();
	if (TLS_GET (tlsbuffer))
		dump_buffer (prof, TLS_GET (tlsbuffer));
	TLS_SET (tlsbuffer, NULL);
	release_lock ();
#if defined (HAVE_ZLIB)
	if (prof->gzfile)
		gzclose (prof->gzfile);
#endif
	if (prof->pipe_output)
		pclose (prof->file);
	else
		fclose (prof->file);
	free (prof);
}

static MonoProfiler*
create_profiler (const char *filename)
{
	MonoProfiler *prof;
	prof = calloc (1, sizeof (MonoProfiler));
	if (do_report) /* FIXME: use filename as output */
		filename = "|mprof-report -";

	if (!filename)
		filename = "output.mlpd";
	if (*filename == '|') {
		prof->file = popen (filename + 1, "w");
		prof->pipe_output = 1;
	} else {
		prof->file = fopen (filename, "wb");
	}
	if (!prof->file) {
		printf ("Cannot create profiler output: %s\n", filename);
		exit (1);
	}
#if defined (HAVE_ZLIB)
	if (use_zip)
		prof->gzfile = gzdopen (fileno (prof->file), "wb");
#endif
	dump_header (prof);
	return prof;
}

static void
usage (int do_exit)
{
	printf ("Log profiler version %d.%d (format: %d)\n", LOG_VERSION_MAJOR, LOG_VERSION_MINOR, LOG_DATA_VERSION);
	printf ("Usage: mono --profile=log[:OPTION1[,OPTION2...]] program.exe\n");
	printf ("Options:\n");
	printf ("\thelp             show this usage info\n");
	printf ("\t[no]alloc        enable/disable recording allocation info\n");
	printf ("\t[no]calls        enable/disable recording enter/leave method events\n");
	printf ("\theapshot         record heap shot info (by default at each major collection)\n");
	printf ("\thsmode=MODE      heapshot mode: every XXms milliseconds or every YYgc collections\n");
	printf ("\ttime=fast        use a faster (but more inaccurate) timer\n");
	printf ("\tmaxframes=NUM    collect up to NUM stack frames\n");
	printf ("\tcalldepth=NUM    ignore method events for call chain depth bigger than NUM\n");
	printf ("\toutput=FILENAME  write the data to file FILENAME\n");
	printf ("\toutput=|PROGRAM  write the data to the stdin of PROGRAM\n");
	printf ("\treport           create a report instead of writing the raw data to a file\n");
	printf ("\tzip              compress the output data\n");
	if (do_exit)
		exit (1);
}

static const char*
match_option (const char* p, const char *opt, char **rval)
{
	int len = strlen (opt);
	if (strncmp (p, opt, len) == 0) {
		if (rval) {
			if (p [len] == '=' && p [len + 1]) {
				const char *opt = p + len + 1;
				const char *end = strchr (opt, ',');
				char *val;
				int l;
				if (end == NULL) {
					l = strlen (opt);
				} else {
					l = end - opt;
				}
				val = malloc (l + 1);
				memcpy (val, opt, l);
				val [l] = 0;
				*rval = val;
				return opt + l;
			}
			usage (1);
		} else {
			if (p [len] == 0)
				return p + len;
			if (p [len] == ',')
				return p + len + 1;
		}
	}
	return p;
}

/* 
 * declaration to silence the compiler: this is the entry point that
 * mono will load from the shared library and call.
 */
extern void
mono_profiler_startup (const char *desc);

void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *prof;
	char *filename = NULL;
	const char *p;
	const char *opt;
	int fast_time = 0;
	int calls_enabled = 0;
	int allocs_enabled = 0;
	int events = MONO_PROFILE_GC|MONO_PROFILE_ALLOCATIONS|
		MONO_PROFILE_GC_MOVES|MONO_PROFILE_CLASS_EVENTS|MONO_PROFILE_THREADS|
		MONO_PROFILE_ENTER_LEAVE|MONO_PROFILE_JIT_COMPILATION|MONO_PROFILE_EXCEPTIONS|
		MONO_PROFILE_MONITOR_EVENTS|MONO_PROFILE_MODULE_EVENTS;

	p = desc;
	if (strncmp (p, "log", 3))
		usage (1);
	p += 3;
	if (*p == ':')
		p++;
	for (; *p; p = opt) {
		char *val;
		if (*p == ',') {
			opt = p + 1;
			continue;
		}
		if ((opt = match_option (p, "help", NULL)) != p) {
			usage (0);
			continue;
		}
		if ((opt = match_option (p, "calls", NULL)) != p) {
			calls_enabled = 1;
			continue;
		}
		if ((opt = match_option (p, "nocalls", NULL)) != p) {
			events &= ~MONO_PROFILE_ENTER_LEAVE;
			nocalls = 1;
			continue;
		}
		if ((opt = match_option (p, "alloc", NULL)) != p) {
			allocs_enabled = 1;
			continue;
		}
		if ((opt = match_option (p, "noalloc", NULL)) != p) {
			events &= ~MONO_PROFILE_ALLOCATIONS;
			continue;
		}
		if ((opt = match_option (p, "time", &val)) != p) {
			if (strcmp (val, "fast") == 0)
				fast_time = 1;
			else if (strcmp (val, "null") == 0)
				fast_time = 2;
			else
				usage (1);
			free (val);
			continue;
		}
		if ((opt = match_option (p, "report", NULL)) != p) {
			do_report = 1;
			continue;
		}
		if ((opt = match_option (p, "heapshot", NULL)) != p) {
			events &= ~MONO_PROFILE_ALLOCATIONS;
			events &= ~MONO_PROFILE_ENTER_LEAVE;
			nocalls = 1;
			do_heap_shot = 1;
			continue;
		}
		if ((opt = match_option (p, "hsmode", &val)) != p) {
			char *end;
			unsigned int count = strtoul (val, &end, 10);
			if (val == end)
				usage (1);
			if (strcmp (end, "ms") == 0)
				hs_mode_ms = count;
			else if (strcmp (end, "gc") == 0)
				hs_mode_gc = count;
			else
				usage (1);
			free (val);
			continue;
		}
		if ((opt = match_option (p, "zip", NULL)) != p) {
			use_zip = 1;
			continue;
		}
		if ((opt = match_option (p, "output", &val)) != p) {
			filename = val;
			continue;
		}
		if ((opt = match_option (p, "maxframes", &val)) != p) {
			char *end;
			num_frames = strtoul (val, &end, 10);
			if (num_frames > MAX_FRAMES)
				num_frames = MAX_FRAMES;
			free (val);
			notraces = num_frames == 0;
			continue;
		}
		if ((opt = match_option (p, "calldepth", &val)) != p) {
			char *end;
			max_call_depth = strtoul (val, &end, 10);
			free (val);
			continue;
		}
		if (opt == p) {
			usage (0);
			exit (0);
		}
	}
	if (calls_enabled) {
		events |= MONO_PROFILE_ENTER_LEAVE;
		nocalls = 0;
	}
	if (allocs_enabled)
		events |= MONO_PROFILE_ALLOCATIONS;
	utils_init (fast_time);

	prof = create_profiler (filename);
	init_thread ();

	mono_profiler_install (prof, log_shutdown);
	mono_profiler_install_gc (gc_event, gc_resize);
	mono_profiler_install_allocation (gc_alloc);
	mono_profiler_install_gc_moves (gc_moves);
	mono_profiler_install_class (NULL, class_loaded, NULL, NULL);
	mono_profiler_install_module (NULL, image_loaded, NULL, NULL);
	mono_profiler_install_thread (thread_start, thread_end);
	mono_profiler_install_enter_leave (method_enter, method_leave);
	mono_profiler_install_jit_end (method_jitted);
	mono_profiler_install_exception (throw_exc, method_exc_leave, clause_exc);
	mono_profiler_install_monitor (monitor_event);
	mono_profiler_install_runtime_initialized (runtime_initialized);

	mono_profiler_set_events (events);

	TLS_INIT (tlsbuffer);
}

