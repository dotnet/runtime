#include <config.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ep-rt-config.h>
#include <eventpipe/ep-types.h>
#include <eventpipe/ep-rt.h>
#include <eventpipe/ep.h>

#include <mono/metadata/profiler.h>
#include <mono/metadata/callspec.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/sgen/sgen-conf.h>
#include <mono/sgen/sgen-tagged-pointer.h>
#include <clretwallmain.h>

extern EVENTPIPE_TRACE_CONTEXT MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_DOTNET_Context;
#define RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT MICROSOFT_DOTNETRUNTIME_MONO_PROFILER_PROVIDER_DOTNET_Context

// Enable/Disable mono profiler provider.
#define DEFAULT_MONO_PROFILER_PROVIDER_ENABLED false
static bool _mono_profiler_provider_enabled = DEFAULT_MONO_PROFILER_PROVIDER_ENABLED;

// Mono profilers.
static MonoProfilerHandle _mono_profiler_provider = NULL;
static MonoProfilerHandle _mono_heap_dump_profiler_provider = NULL;

// Profiler callspec.
static MonoCallSpec _mono_profiler_provider_callspec = {0};

// Buffered GC event types.
typedef enum {
	BUFFERED_GC_EVENT = 1,
	BUFFERED_GC_EVENT_RESIZE = 2,
	BUFFERED_GC_EVENT_ROOTS = 3,
	BUFFERED_GC_EVENT_MOVES = 4,
	BUFFERED_GC_EVENT_OBJECT_REF = 5,
	BUFFERED_GC_EVENT_ROOT_REGISTER = 6,
	BUFFERED_GC_EVENT_ROOT_UNREGISTER = 7
} BufferedGCEventType;

typedef struct _BufferedGCEvent BufferedGCEvent;
struct _BufferedGCEvent {
	BufferedGCEventType type;
	uint32_t payload_size;
};

#define GC_HEAP_DUMP_DEFAULT_MEM_BLOCK_SIZE (mono_pagesize() * 16)
#define GC_HEAP_DUMP_MEM_BLOCK_SIZE_INC (mono_pagesize())

typedef struct _GCHeapDumpMemBlock GCHeapDumpMemBlock;
struct _GCHeapDumpMemBlock {
	GCHeapDumpMemBlock *next;
	GCHeapDumpMemBlock *prev;
	uint8_t *start;
	uint32_t alloc_size;
	uint32_t size;
	uint32_t offset;
	uint32_t last_used_offset;
};

static volatile GCHeapDumpMemBlock *_gc_heap_dump_mem_blocks = NULL;
static volatile GCHeapDumpMemBlock *_gc_heap_dump_current_mem_block = NULL;
static volatile uint32_t _gc_heap_dump_requests = 0;
static volatile uint32_t _gc_heap_dump_in_progress = 0;
static volatile uint64_t _gc_heap_dump_trigger_count = 0;

static GSList *_mono_profiler_provider_params = NULL;
static GQueue *_gc_heap_dump_request_params = NULL;

// Lightweight atomic "exclusive/shared" lock, prevents new fire events to happend while GC is in progress and gives GC ability to wait until all pending fire events are done
// before progressing. State uint32_t is split into two uint16_t, upper uint16_t represent gc in progress state, taken when GC starts, preventing new fire events to execute and lower
// uint16_t keeps number of fire events in flight, (gc_in_progress << 16) | (fire_event_count & 0xFFFF). Spin lock is only taken on slow path to queue up pending shared requests
// while GC is in progress and should very rarely be needed.
typedef uint32_t gc_state_t;
typedef uint16_t gc_state_count_t;

#define GC_STATE_GET_FIRE_EVENT_COUNT(x) ((gc_state_count_t)((x & 0xFFFF)))
#define GC_STATE_INC_FIRE_EVENT_COUNT(x) ((gc_state_t)((gc_state_t)(x & 0xFFFF0000) | (gc_state_t)(GC_STATE_GET_FIRE_EVENT_COUNT(x) + 1)))
#define GC_STATE_DEC_FIRE_EVENT_COUNT(x) ((gc_state_t)((gc_state_t)(x & 0xFFFF0000) | (gc_state_t)(GC_STATE_GET_FIRE_EVENT_COUNT(x) - 1)))
#define GC_STATE_IN_PROGRESS_START(x) ((gc_state_t)((gc_state_t)(0xFFFF << 16) | (gc_state_t)GC_STATE_GET_FIRE_EVENT_COUNT(x)))
#define GC_STATE_IS_IN_PROGRESS(x) (((x >> 16) & 0xFFFF) == 0xFFFF)
#define GC_STATE_IN_PROGRESS_STOP(x) ((gc_state_t)((gc_state_t)GC_STATE_GET_FIRE_EVENT_COUNT(x)))

static volatile gc_state_t _gc_state = 0;
static ep_rt_spin_lock_handle_t _gc_lock = {0};

static
void
fire_buffered_gc_events (
	GCHeapDumpMemBlock *block,
	GHashTable *cache);

static
MonoProfilerCallInstrumentationFlags
method_instrumentation_filter_callback (
	MonoProfiler *prof,
	MonoMethod *method);

static
void
gc_root_register_callback (
	MonoProfiler *prof,
	const mono_byte *start,
	uintptr_t size,
	MonoGCRootSource source,
	const void * key,
	const char * name);

static
void
gc_root_unregister_callback (
	MonoProfiler *prof,
	const mono_byte *start);

static
bool
is_keword_enabled (uint64_t enabled_keywords, uint64_t keyword)
{
	return (enabled_keywords & keyword) == keyword;
}

static
gc_state_t
gc_state_volatile_load (const volatile gc_state_t *ptr)
{
	return ep_rt_volatile_load_uint32_t ((const volatile uint32_t *)ptr);
}

static
gc_state_t
gc_state_atomic_cas (volatile gc_state_t *target, gc_state_t expected, gc_state_t value)
{
	return (gc_state_t)(mono_atomic_cas_i32 ((volatile gint32 *)(target), (gint32)(value), (gint32)(expected)));
}

static
void
gc_in_progress_start (void)
{
	gc_state_t old_state = 0;
	gc_state_t new_state = 0;

	// Make sure fire event calls will block and wait for GC completion.
	ep_rt_spin_lock_acquire (&_gc_lock);

	// Set gc in progress state, preventing new fire event requests.
	do {
		old_state = gc_state_volatile_load (&_gc_state);
		EP_ASSERT (!GC_STATE_IS_IN_PROGRESS (old_state));
		new_state = GC_STATE_IN_PROGRESS_START (old_state);
	} while (gc_state_atomic_cas (&_gc_state, old_state, new_state) != old_state);

	gc_state_count_t count = GC_STATE_GET_FIRE_EVENT_COUNT (new_state);

	// Wait for all fire events to complete before progressing with gc.
	// NOTE, should never be called recursivly. Default yield count used in SpinLock.cs.
	int yield_count = 40;
	while (count) {
		if (yield_count > 0) {
			ep_rt_mono_thread_yield ();
			yield_count--;
		} else {
			ep_rt_thread_sleep (200);
		}
		count = GC_STATE_GET_FIRE_EVENT_COUNT (gc_state_volatile_load (&_gc_state));
	}
}

static
void
gc_in_progress_stop (void)
{
	gc_state_t old_state = 0;
	gc_state_t new_state = 0;

	// Reset gc in progress state.
	do {
		old_state = gc_state_volatile_load (&_gc_state);
		EP_ASSERT (GC_STATE_IS_IN_PROGRESS (old_state));

		new_state = GC_STATE_IN_PROGRESS_STOP (old_state);
		EP_ASSERT (!GC_STATE_IS_IN_PROGRESS (new_state));
	} while (gc_state_atomic_cas (&_gc_state, old_state, new_state) != old_state);

	// Make sure fire events can continune to execute.
	ep_rt_spin_lock_release (&_gc_lock);
}

static
bool
gc_in_progress (void)
{
	return GC_STATE_IS_IN_PROGRESS (gc_state_volatile_load (&_gc_state));
}

static
void
fire_event_enter (void)
{
	gc_state_t old_state = 0;
	gc_state_t new_state = 0;

	// NOTE, should never be called recursivly.
	do {
		old_state = gc_state_volatile_load (&_gc_state);
		if (GC_STATE_IS_IN_PROGRESS (old_state)) {
			// GC in progress and thread tries to fire event (this should be an unlikely scenario). Wait until GC is done.
			ep_rt_spin_lock_acquire (&_gc_lock);
			ep_rt_spin_lock_release (&_gc_lock);
			old_state = gc_state_volatile_load (&_gc_state);
		}
		// Increase number of fire event calls.
		new_state = GC_STATE_INC_FIRE_EVENT_COUNT (old_state);
	} while (gc_state_atomic_cas (&_gc_state, old_state, new_state) != old_state);
}

static
void
fire_event_exit (void)
{
	gc_state_t old_state = 0;
	gc_state_t new_state = 0;

	do {
		old_state = gc_state_volatile_load (&_gc_state);
		new_state = GC_STATE_DEC_FIRE_EVENT_COUNT (old_state);
	} while (gc_state_atomic_cas (&_gc_state, old_state, new_state) != old_state);
}

static
const EventFilterDescriptor *
provider_params_add (const EventFilterDescriptor *key)
{
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);

	EventFilterDescriptor *param = NULL;
	if (key && key->ptr && key->size) {
		uint64_t param_ptr = (uint64_t)g_malloc (key->size);
		if (param_ptr) {
			param = ep_event_filter_desc_alloc (param_ptr, key->size, key->type);
			if (param) {
				memcpy ((uint8_t*)(uintptr_t)param->ptr,(const uint8_t*)(uintptr_t)key->ptr, key->size);
				_mono_profiler_provider_params = g_slist_append (_mono_profiler_provider_params, param);
			} else {
				g_free ((void *)(uintptr_t)param_ptr);
			}
		}
	}
	return param;
}

static
bool
provider_params_remove (const EventFilterDescriptor *key)
{
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);

	bool removed = false;
	if (_mono_profiler_provider_params && key && key->ptr && key->size) {
		GSList *list = _mono_profiler_provider_params;
		EventFilterDescriptor *param = NULL;
		while (list) {
			param = (EventFilterDescriptor *)(list->data);
			if (param && param->ptr && param->type == key->type && param->size == key->size &&
				memcmp ((const void *)(uintptr_t)param->ptr, (const void *)(uintptr_t)key->ptr, param->size) == 0) {
					g_free ((void *)(uintptr_t)param->ptr);
					ep_event_filter_desc_free (param);
					_mono_profiler_provider_params = g_slist_delete_link (_mono_profiler_provider_params, list);
					removed = true;
					break;
			}
			list = list->next;
		}
	}

	return removed;
}

static
void
provider_params_free (void)
{
	for (GSList *list = _mono_profiler_provider_params; list; list = list->next) {
		EventFilterDescriptor *param = (EventFilterDescriptor *)(list->data);
		if (param) {
			g_free ((void *)(uintptr_t)param->ptr);
			ep_event_filter_desc_free (param);
		}
	}
	g_slist_free (_mono_profiler_provider_params);
	_mono_profiler_provider_params = NULL;
}

static
bool
provider_params_get_value (
	const EventFilterDescriptor *param,
	const ep_char8_t *key,
	const ep_char8_t **value)
{
	if (!param || !param->ptr || !param->size || !key)
		return false;

	const ep_char8_t *current = (ep_char8_t *)(uintptr_t)param->ptr;
	const ep_char8_t *end = current + param->size;
	bool found_key = false;

	if (value)
		*value = "";

	if (!current [param->size - 1]) {
		while (current < end) {
			if (found_key) {
				if (value)
					*value = current;
				break;
			}

			if (!ep_rt_utf8_string_compare_ignore_case (current, key)) {
				found_key = true;
			}

			current = current + strlen (current) + 1;
		}
	}

	return found_key;
}

static
bool
provider_params_contains_heap_collect_ondemand (const EventFilterDescriptor *param)
{
	const ep_char8_t *value = NULL;
	bool found_heap_collect_ondemand_value = false;

	if (provider_params_get_value (param, "heapcollect", &value)) {
		if (strstr (value, "ondemand"))
			found_heap_collect_ondemand_value = true;
	}

	return found_heap_collect_ondemand_value;
}

static
const ep_char8_t *
provider_params_get_heap_collect_ondemand_value (void)
{
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);

	const ep_char8_t *value = NULL;
	if (_gc_heap_dump_request_params && !g_queue_is_empty (_gc_heap_dump_request_params)) {
		EventFilterDescriptor *param = (EventFilterDescriptor *)g_queue_pop_head (_gc_heap_dump_request_params);
		if (param)
			provider_params_get_value (param, "heapcollect", &value);
		g_queue_push_head (_gc_heap_dump_request_params , (gpointer)param);
	}
	return value ? value : "";
}

static
void
gc_heap_dump_request_params_push_value (const EventFilterDescriptor *param)
{
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);

	if (!_gc_heap_dump_request_params)
		_gc_heap_dump_request_params = g_queue_new ();
	if (_gc_heap_dump_request_params) {
		EventFilterDescriptor *desc = NULL;
		if (param) {
			uint8_t *value = g_malloc (param->size);
			memcpy (value, (uint8_t*)(uintptr_t)param->ptr, param->size);
			desc = ep_event_filter_desc_alloc ((uint64_t)(uintptr_t)value, param->size, param->type);
		}
		g_queue_push_tail (_gc_heap_dump_request_params, (gpointer)desc);
	}
}

static
void
gc_heap_dump_request_params_pop_value (void)
{
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);

	if (_gc_heap_dump_request_params && !g_queue_is_empty (_gc_heap_dump_request_params)) {
		EventFilterDescriptor *param = (EventFilterDescriptor *)g_queue_pop_head (_gc_heap_dump_request_params);
		if (param) {
			g_free ((uint8_t*)(uintptr_t)param->ptr);
			ep_event_filter_desc_free (param);
		}
	}
}

static
void
gc_heap_dump_request_params_free (void)
{
	if (_gc_heap_dump_request_params) {
		while (!g_queue_is_empty (_gc_heap_dump_request_params))
			gc_heap_dump_request_params_pop_value ();
		g_queue_free (_gc_heap_dump_request_params);
		_gc_heap_dump_request_params = NULL;
	}
}

static
void
gc_heap_dump_requests_inc (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());
	ep_rt_atomic_inc_uint32_t (&_gc_heap_dump_requests);
}

static
void
gc_heap_dump_requests_dec (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());
	ep_rt_atomic_dec_uint32_t (&_gc_heap_dump_requests);
}

static
bool
gc_heap_dump_requested (void)
{
	if (!ep_rt_mono_is_runtime_initialized ())
		return false;

	return ep_rt_volatile_load_uint32_t(&_gc_heap_dump_requests) != 0 ? true : false;
}

static
bool
gc_heap_dump_in_progress (void)
{
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);
	return ep_rt_volatile_load_uint32_t_without_barrier (&_gc_heap_dump_in_progress) != 0 ? true : false;
}

static
void
gc_heap_dump_in_progress_start (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());

	ep_rt_spin_lock_requires_lock_held (&_gc_lock);
	ep_rt_volatile_store_uint32_t_without_barrier (&_gc_heap_dump_in_progress, 1);
}

static
void
gc_heap_dump_in_progress_stop (void)
{
	EP_ASSERT (ep_rt_mono_is_runtime_initialized ());

	ep_rt_spin_lock_requires_lock_held (&_gc_lock);
	ep_rt_volatile_store_uint32_t_without_barrier (&_gc_heap_dump_in_progress, 0);
}

static
void
gc_heap_dump_trigger_callback (MonoProfiler *prof)
{
	if (gc_heap_dump_requested ()) {
		ep_rt_spin_lock_acquire (&_gc_lock);
			gc_heap_dump_requests_dec ();
			gc_heap_dump_in_progress_start ();
		ep_rt_spin_lock_release (&_gc_lock);

		mono_gc_collect (mono_gc_max_generation ());

		ep_rt_spin_lock_acquire (&_gc_lock);
			gc_heap_dump_request_params_pop_value  ();
			gc_heap_dump_in_progress_stop ();
		ep_rt_spin_lock_release (&_gc_lock);
	}
}

static
GCHeapDumpMemBlock *
gc_heap_dump_mem_block_alloc (uint32_t req_size)
{
	EP_ASSERT (gc_in_progress ());

	GCHeapDumpMemBlock *prev = NULL;

	uint32_t size = GC_HEAP_DUMP_DEFAULT_MEM_BLOCK_SIZE;
	while (size - sizeof(GCHeapDumpMemBlock) < req_size)
		size += GC_HEAP_DUMP_MEM_BLOCK_SIZE_INC;

	GCHeapDumpMemBlock *block = mono_valloc (NULL, size, MONO_MMAP_READ | MONO_MMAP_WRITE | MONO_MMAP_ANON | MONO_MMAP_PRIVATE, MONO_MEM_ACCOUNT_PROFILER);
	if (block) {
		block->alloc_size = size;
		block->start = (uint8_t *)ALIGN_PTR_TO ((uint8_t *)block + sizeof (GCHeapDumpMemBlock), 16);
		block->size = (uint32_t)(((uint8_t*)block + size) - (uint8_t*)block->start);
		block->offset = 0;
		block->last_used_offset = 0;

		while (true) {
			prev = (GCHeapDumpMemBlock *)ep_rt_volatile_load_ptr_without_barrier ((volatile void **)&_gc_heap_dump_mem_blocks);
			if (mono_atomic_cas_ptr ((volatile gpointer*)&_gc_heap_dump_mem_blocks, block, prev) == prev)
				break;
		}

		if (prev)
			prev->next = block;
		block->prev = prev;
	}

	return block;
}

static
uint8_t *
gc_heap_dump_mem_alloc (uint32_t req_size)
{
	EP_ASSERT (gc_in_progress ());

	GCHeapDumpMemBlock *current_block = (GCHeapDumpMemBlock *)ep_rt_volatile_load_ptr_without_barrier ((volatile void **)&_gc_heap_dump_current_mem_block);
	uint8_t *buffer = NULL;

	if (!current_block) {
		current_block = gc_heap_dump_mem_block_alloc (req_size);
		if (current_block) {
			mono_memory_barrier ();
			ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_current_mem_block, current_block);
		}
	}

	if (current_block) {
		uint32_t prev_offset = (uint32_t)mono_atomic_fetch_add_i32 ((volatile int32_t *)&current_block->offset, (int32_t)req_size);
		if (prev_offset + req_size > current_block->size) {
			if (prev_offset <= current_block->size)
				current_block->last_used_offset = prev_offset;
			current_block = gc_heap_dump_mem_block_alloc (req_size);
			if (current_block) {
				buffer = current_block->start;
				current_block->offset += req_size;
				mono_memory_barrier ();
				ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_current_mem_block, current_block);
			}
		} else {
			buffer = (uint8_t*)current_block->start + prev_offset;
		}
	}

	return buffer;
}

static
void
gc_heap_dump_mem_block_free_all (void)
{
	GCHeapDumpMemBlock *current_block = (GCHeapDumpMemBlock *)ep_rt_volatile_load_ptr ((volatile void **)&_gc_heap_dump_current_mem_block);

	ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_current_mem_block, NULL);
	ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_mem_blocks, NULL);

	mono_memory_barrier ();

	while (current_block) {
		GCHeapDumpMemBlock *prev_block = current_block->prev;
		mono_vfree ((uint8_t *)current_block, current_block->alloc_size, MONO_MEM_ACCOUNT_MEM_MANAGER);
		current_block = prev_block;
	}
}

static
void
gc_heap_dump_mem_block_free_all_but_current (void)
{
	EP_ASSERT (gc_in_progress ());

	GCHeapDumpMemBlock *block_to_keep = (GCHeapDumpMemBlock *)ep_rt_volatile_load_ptr ((volatile void **)&_gc_heap_dump_current_mem_block);
	GCHeapDumpMemBlock *current_block = block_to_keep;

	ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_current_mem_block, NULL);
	ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_mem_blocks, NULL);

	mono_memory_barrier ();

	if (current_block) {
		if (current_block->prev) {
			current_block = current_block->prev;
			while (current_block) {
				GCHeapDumpMemBlock *prev_block = current_block->prev;
				mono_vfree ((uint8_t *)current_block, current_block->alloc_size, MONO_MEM_ACCOUNT_MEM_MANAGER);
				current_block = prev_block;
			}
		}
	}

	if (block_to_keep) {
		block_to_keep->prev = NULL;
		block_to_keep->next = NULL;
		block_to_keep->offset = 0;
		block_to_keep->last_used_offset = 0;
	}

	mono_memory_barrier ();

	ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_current_mem_block, block_to_keep);
	ep_rt_volatile_store_ptr_without_barrier ((volatile void **)&_gc_heap_dump_mem_blocks, block_to_keep);
}

static
uint8_t *
buffered_gc_event_alloc (uint32_t req_size)
{
	EP_ASSERT (gc_in_progress ());
	return gc_heap_dump_mem_alloc (req_size + sizeof (BufferedGCEvent));
}

static
void
fire_buffered_gc_events_in_alloc_order (GHashTable *cache)
{
	EP_ASSERT (gc_in_progress ());

	GCHeapDumpMemBlock *first_block = (GCHeapDumpMemBlock *)ep_rt_volatile_load_ptr ((volatile void **)&_gc_heap_dump_current_mem_block);
	while (first_block && first_block->prev)
		first_block = first_block->prev;

	GCHeapDumpMemBlock *current_block = first_block;
	while (current_block) {
		GCHeapDumpMemBlock *next_block = current_block->next;
		fire_buffered_gc_events (current_block, cache);
		current_block = next_block;
	}

	gc_heap_dump_mem_block_free_all_but_current ();
}

static
void
get_generic_types (
	MonoGenericInst *generic_instance,
	uint32_t *generic_type_count,
	uint8_t **generic_types)
{
	if (generic_instance) {
		uint8_t *buffer = g_malloc (generic_instance->type_argc * (sizeof (uint8_t) + sizeof (uint64_t)));
		if (buffer) {
			*generic_types = buffer;
			*generic_type_count = generic_instance->type_argc;
			for (uint32_t i = 0; i < generic_instance->type_argc; ++i) {
				uint8_t type = generic_instance->type_argv [i]->type;
				ep_write_buffer_uint8_t (&buffer, type);

				uint64_t class_id = (uint64_t)mono_class_from_mono_type_internal (generic_instance->type_argv [i]);
				ep_write_buffer_uint64_t (&buffer, class_id);
			}
		}
	}
}

static
void
get_class_data (
	MonoClass *klass,
	uint64_t *class_id,
	uint64_t *module_id,
	ep_char8_t **class_name,
	uint32_t *class_generic_type_count,
	uint8_t **class_generic_types)
{
	*class_id = (uint64_t)klass;
	*module_id = 0;

	if (klass)
		*module_id = (uint64_t)m_class_get_image (klass);

	if (klass && class_name)
		*class_name = (ep_char8_t *)mono_type_get_name_full (m_class_get_byval_arg (klass), MONO_TYPE_NAME_FORMAT_IL);
	else if (class_name)
		*class_name = NULL;

	if (class_generic_type_count && class_generic_types) {
		if (mono_class_is_ginst (klass)) {
			MonoGenericContext *context = mono_class_get_context (klass);
			MonoGenericInst *class_instance = (context && context->class_inst) ? context->class_inst : NULL;
			get_generic_types (class_instance, class_generic_type_count, class_generic_types);
		}
	}
}

static
void
fire_gc_event_root_register (
	uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t root_id;
	uintptr_t root_size;
	uint8_t root_source;
	uintptr_t root_key;

	memcpy (&root_id, data, sizeof (root_id));
	data += sizeof (root_id);

	memcpy (&root_size, data, sizeof (root_size));
	data += sizeof (root_size);

	memcpy (&root_source, data, sizeof (root_source));
	data += sizeof (root_source);

	memcpy (&root_key, data, sizeof (root_key));
	data += sizeof (root_key);

	FireEtwMonoProfilerGCRootRegister (
		(const void *)root_id,
		(uint64_t)root_size,
		root_source,
		(uint64_t)root_key,
		(const ep_char8_t *)data,
		NULL,
		NULL);
}

static
void
buffer_gc_event_root_register_callback (
	MonoProfiler *prof,
	const mono_byte *start,
	uintptr_t size,
	MonoGCRootSource source,
	const void * key,
	const char * name)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t root_id = (uintptr_t)start;
	uintptr_t root_size = size;
	uint8_t root_source = (uint8_t)source;
	uintptr_t root_key = (uintptr_t)key;
	const char *root_name = (name ? name : "");
	size_t root_name_len = strlen (root_name) + 1;

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_ROOT_REGISTER;
	gc_event_data.payload_size = (uint32_t)
		(sizeof (root_id) +
		sizeof (root_size) +
		sizeof (root_source) +
		sizeof (root_key) +
		root_name_len);

	uint8_t * buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCEvent.RootID
		memcpy(buffer, &root_id, sizeof (root_id));
		buffer += sizeof (root_id);

		// GCEvent.RootSize
		memcpy(buffer, &root_size, sizeof (root_size));
		buffer += sizeof (root_size);

		// GCEvent.RootType
		memcpy(buffer, &root_source, sizeof (root_source));
		buffer += sizeof (root_source);

		// GCEvent.RootKeyID
		memcpy(buffer, &root_key, sizeof (root_key));
		buffer += sizeof (root_key);

		// GCEvent.RootKeyName
		memcpy(buffer, root_name, root_name_len);
	}
}

static
void
fire_gc_event_root_unregister (
	uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t root_id;

	memcpy (&root_id, data, sizeof (root_id));

	FireEtwMonoProfilerGCRootUnregister (
		(const void *)root_id,
		NULL,
		NULL);
}

static
void
buffer_gc_event_root_unregister_callback (
	MonoProfiler *prof,
	const mono_byte *start)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t root_id = (uintptr_t)start;

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_ROOT_UNREGISTER;
	gc_event_data.payload_size = sizeof (root_id);

	uint8_t * buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCEvent.RootID
		memcpy(buffer, &root_id, sizeof (root_id));
	}
}

static
void
fire_gc_event (
	uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (gc_in_progress ());

	uint8_t gc_event_type;
	uint32_t generation;

	memcpy (&gc_event_type, data, sizeof (gc_event_type));
	data += sizeof (gc_event_type);

	memcpy (&generation, data, sizeof (generation));

	FireEtwMonoProfilerGCEvent (
		gc_event_type,
		generation,
		NULL,
		NULL);
}

static
void
buffer_gc_event (
	uint8_t gc_event_type,
	uint32_t generation)
{
	EP_ASSERT (gc_in_progress ());

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT;
	gc_event_data.payload_size =
		sizeof (gc_event_type) +
		sizeof (generation);

	uint8_t * buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCEvent.GCEventType
		memcpy(buffer, &gc_event_type, sizeof (gc_event_type));
		buffer += sizeof (gc_event_type);

		// GCEvent.GCGeneration
		memcpy(buffer, &generation, sizeof (generation));
	}
}

static
void
fire_gc_event_resize (
	uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t size;

	memcpy (&size, data, sizeof (size));

	FireEtwMonoProfilerGCResize (
		(uint64_t)size,
		NULL,
		NULL);
}

static
void
buffer_gc_event_resize_callback (
	MonoProfiler *prof,
	uintptr_t size)
{
	EP_ASSERT (gc_in_progress ());

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_RESIZE;
	gc_event_data.payload_size = sizeof (size);

	uint8_t * buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCResize.NewSize
		memcpy(buffer, &size, sizeof (size));
	}
}

static
void
fire_gc_event_moves (
	uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (gc_in_progress ());

	uint64_t count;

	memcpy (&count, data, sizeof (count));
	data += sizeof (count);

	FireEtwMonoProfilerGCMoves (
		(uint32_t)count,
		sizeof (uintptr_t) + sizeof (uintptr_t),
		data,
		NULL,
		NULL);
}

static
void
buffer_gc_event_moves_callback (
	MonoProfiler *prof,
	MonoObject *const* objects,
	uint64_t count)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t object_id;
	uintptr_t address_id;

	// Serialized as object_id/address_id pairs.
	count = count / 2;

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_MOVES;
	gc_event_data.payload_size =
		(uint32_t)(sizeof (count) +
		(count * (sizeof (uintptr_t) + sizeof (uintptr_t))));

	uint8_t * buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCMoves.Count
		memcpy (buffer, &count, sizeof (count));
		buffer += sizeof (count);

		// Serialize directly as memory stream expected by FireEtwMonoProfilerGCMoves.
		for (uint64_t i = 0; i < count; i++) {
			// GCMoves.Values[].ObjectID.
			object_id = (uintptr_t)SGEN_POINTER_UNTAG_ALL (*objects);
			ep_write_buffer_uintptr_t (&buffer, object_id);
			objects++;

			// GCMoves.Values[].AddressID.
			address_id = (uintptr_t)*objects;
			ep_write_buffer_uintptr_t (&buffer, address_id);
			objects++;
		}
	}
}

static
void
fire_gc_event_roots (
	uint8_t *data,
	uint32_t payload_size)
{
	EP_ASSERT (gc_in_progress ());

	uint64_t count;

	memcpy (&count, data, sizeof (count));
	data += sizeof (count);

	FireEtwMonoProfilerGCRoots (
		(uint32_t)count,
		sizeof (uintptr_t) + sizeof (uintptr_t),
		data,
		NULL,
		NULL);
}

static
void
buffer_gc_event_roots_callback (
	MonoProfiler *prof,
	uint64_t count,
	const mono_byte *const * addresses,
	MonoObject *const * objects)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t object_id;
	uintptr_t address_id;

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_ROOTS;
	gc_event_data.payload_size =
		(uint32_t)(sizeof (count) +
		(count * (sizeof (uintptr_t) + sizeof (uintptr_t))));

	uint8_t * buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCRoots.Count
		memcpy (buffer, &count, sizeof (count));
		buffer += sizeof (count);

		// Serialize directly as memory stream expected by FireEtwMonoProfilerGCRoots.
		for (uint64_t i = 0; i < count; i++) {
			// GCRoots.Values[].ObjectID.
			object_id = (uintptr_t)SGEN_POINTER_UNTAG_ALL (*objects);
			ep_write_buffer_uintptr_t (&buffer, object_id);
			objects++;

			// GCRoots.Values[].AddressID.
			address_id = (uintptr_t)*addresses;
			ep_write_buffer_uintptr_t (&buffer, address_id);
			addresses++;
		}
	}
}

static
void
fire_gc_event_heap_dump_object_reference (
	uint8_t *data,
	uint32_t payload_size,
	GHashTable *cache)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t object_id;
	uintptr_t vtable_id;
	uintptr_t object_size;
	uint8_t object_gen;
	uintptr_t object_ref_count;

	memcpy (&object_id, data, sizeof (object_id));
	data += sizeof (object_id);

	memcpy (&vtable_id, data, sizeof (vtable_id));
	data += sizeof (vtable_id);

	memcpy (&object_size, data, sizeof (object_size));
	data += sizeof (object_size);

	memcpy (&object_gen, data, sizeof (object_gen));
	data += sizeof (object_gen);

	memcpy (&object_ref_count, data, sizeof (object_ref_count));
	data += sizeof (object_ref_count);

	FireEtwMonoProfilerGCHeapDumpObjectReference (
		(const void *)object_id,
		(uint64_t)vtable_id,
		(uint64_t)object_size,
		object_gen,
		(uint32_t)object_ref_count,
		sizeof (uint32_t) + sizeof (uintptr_t),
		data,
		NULL,
		NULL);

	if (cache)
		g_hash_table_insert (cache, (MonoVTable *)SGEN_POINTER_UNTAG_ALL (vtable_id), NULL);
}

static
int
buffer_gc_event_heap_dump_object_reference_callback (
	MonoObject *obj,
	MonoClass *klass,
	uintptr_t size,
	uintptr_t num,
	MonoObject **refs,
	uintptr_t *offsets,
	void *data)
{
	EP_ASSERT (gc_in_progress ());

	uintptr_t object_id;
	uintptr_t vtable_id;
	uint8_t object_gen;
	uintptr_t object_size = size;
	uintptr_t object_ref_count = num;
	uint32_t object_ref_offset;

	/* account for object alignment */
	object_size += 7;
	object_size &= ~7;

	size_t payload_size =
		sizeof (object_id) +
		sizeof (vtable_id) +
		sizeof (object_size) +
		sizeof (object_gen) +
		sizeof (object_ref_count) +
		(object_ref_count * (sizeof (uint32_t) + sizeof (uintptr_t)));

	BufferedGCEvent gc_event_data;
	gc_event_data.type = BUFFERED_GC_EVENT_OBJECT_REF;
	gc_event_data.payload_size = GSIZE_TO_UINT32 (payload_size);

	uint8_t *buffer = buffered_gc_event_alloc (gc_event_data.payload_size);
	if (buffer) {
		// Internal header
		memcpy (buffer, &gc_event_data, sizeof (gc_event_data));
		buffer += sizeof (gc_event_data);

		// GCEvent.ObjectID
		object_id = (uintptr_t)SGEN_POINTER_UNTAG_ALL (obj);
		memcpy (buffer, &object_id, sizeof (object_id));
		buffer += sizeof (object_id);

		// GCEvent.VTableID
		vtable_id = (uintptr_t)SGEN_POINTER_UNTAG_ALL (mono_object_get_vtable_internal (obj));
		memcpy (buffer, &vtable_id, sizeof (vtable_id));
		buffer += sizeof (vtable_id);

		// GCEvent.ObjectSize
		memcpy (buffer, &object_size, sizeof (object_size));
		buffer += sizeof (object_size);

		// GCEvent.ObjectGeneration
		object_gen = (uint8_t)mono_gc_get_generation (obj);
		memcpy (buffer, &object_gen, sizeof (object_gen));
		buffer += sizeof (object_gen);

		// GCEvent.Count
		memcpy (buffer, &object_ref_count, sizeof (object_ref_count));
		buffer += sizeof (object_ref_count);

		// Serialize directly as memory stream expected by FireEtwMonoProfilerGCHeapDumpObjectReference.
		uintptr_t last_offset = 0;
		for (uintptr_t i = 0; i < object_ref_count; i++) {
			// GCEvent.Values[].ReferencesOffset
			object_ref_offset = GUINTPTR_TO_UINT32 (offsets [i] - last_offset);
			ep_write_buffer_uint32_t (&buffer, object_ref_offset);

			// GCEvent.Values[].ObjectID
			object_id = (uintptr_t)SGEN_POINTER_UNTAG_ALL (refs[i]);
			ep_write_buffer_uintptr_t (&buffer, object_id);

			last_offset = offsets [i];
		}
	}

	return 0;
}

static
void
fire_buffered_gc_events (
	GCHeapDumpMemBlock *block,
	GHashTable *cache)
{
	EP_ASSERT (gc_in_progress ());

	if (block) {
		uint32_t current_offset = 0;
		uint32_t used_size = (block->offset < block->size) ? block->offset : block->last_used_offset;
		BufferedGCEvent gc_event;
		while ((current_offset + sizeof (gc_event)) <= used_size) {
			uint8_t *data = block->start + current_offset;
			memcpy (&gc_event, data, sizeof (gc_event));
			data += sizeof (gc_event);
			if ((current_offset + sizeof (gc_event) + gc_event.payload_size) <= used_size) {
				switch (gc_event.type) {
				case BUFFERED_GC_EVENT:
					fire_gc_event (data, gc_event.payload_size);
					break;
				case BUFFERED_GC_EVENT_RESIZE:
					fire_gc_event_resize (data, gc_event.payload_size);
					break;
				case BUFFERED_GC_EVENT_ROOTS:
					fire_gc_event_roots (data, gc_event.payload_size);
					break;
				case BUFFERED_GC_EVENT_MOVES:
					fire_gc_event_moves (data, gc_event.payload_size);
					break;
				case BUFFERED_GC_EVENT_OBJECT_REF:
					fire_gc_event_heap_dump_object_reference (data, gc_event.payload_size, cache);
					break;
				case BUFFERED_GC_EVENT_ROOT_REGISTER:
					fire_gc_event_root_register (data, gc_event.payload_size);
					break;
				case BUFFERED_GC_EVENT_ROOT_UNREGISTER:
					fire_gc_event_root_unregister (data, gc_event.payload_size);
					break;
				default:
					EP_ASSERT (!"Unknown buffered GC event type.");
				}

				current_offset += sizeof (gc_event) + gc_event.payload_size;
			} else {
				break;
			}
		}
	}
}

static
void
fire_cached_gc_events (GHashTable *cache)
{
	if (cache) {
		GHashTableIter iter;
		MonoVTable *object_vtable;
		g_hash_table_iter_init (&iter, cache);
		while (g_hash_table_iter_next (&iter, (void**)&object_vtable, NULL)) {
			if (object_vtable) {
				uint64_t vtable_id = (uint64_t)object_vtable;
				uint64_t class_id;
				uint64_t module_id;
				ep_char8_t *class_name;
				get_class_data (object_vtable->klass, &class_id, &module_id, &class_name, NULL, NULL);
				FireEtwMonoProfilerGCHeapDumpVTableClassReference (
					vtable_id,
					class_id,
					module_id,
					class_name,
					NULL,
					NULL);
				g_free (class_name);
			}
		}
	}
}

static
void
app_domain_loading_callback (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainLoading ())
		return;

	uint64_t domain_id = (uint64_t)domain;

	fire_event_enter ();

	FireEtwMonoProfilerAppDomainLoading (
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
app_domain_loaded_callback (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainLoaded ())
		return;

	uint64_t domain_id = (uint64_t)domain;

	fire_event_enter ();

	FireEtwMonoProfilerAppDomainLoaded (
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
app_domain_unloading_callback (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainUnloading ())
		return;

	uint64_t domain_id = (uint64_t)domain;

	fire_event_enter ();

	FireEtwMonoProfilerAppDomainUnloading (
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
app_domain_unloaded_callback (
	MonoProfiler *prof,
	MonoDomain *domain)
{
	if (!EventEnabledMonoProfilerAppDomainUnloaded ())
		return;

	uint64_t domain_id = (uint64_t)domain;

	fire_event_enter ();

	FireEtwMonoProfilerAppDomainUnloaded (
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
app_domain_name_callback (
	MonoProfiler *prof,
	MonoDomain *domain,
	const char *name)
{
	if (!EventEnabledMonoProfilerAppDomainName ())
		return;

	uint64_t domain_id = (uint64_t)domain;

	fire_event_enter ();

	FireEtwMonoProfilerAppDomainName (
		domain_id,
		(const ep_char8_t *)(name ? name : ""),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
get_jit_data (
	MonoMethod *method,
	uint64_t *method_id,
	uint64_t *module_id,
	uint32_t *method_token,
	uint32_t *method_generic_type_count,
	uint8_t **method_generic_types)
{
	*method_id = (uint64_t)method;
	*module_id = 0;
	*method_token = 0;

	if (method) {
		*method_token = method->token;
		if (method->klass)
			*module_id = (uint64_t)m_class_get_image (method->klass);

		if (method_generic_type_count && method_generic_types) {
			if (method->is_inflated) {
				MonoGenericContext *context = mono_method_get_context (method);
				MonoGenericInst *method_instance = (context && context->method_inst) ? context->method_inst : NULL;
				get_generic_types (method_instance, method_generic_type_count, method_generic_types);
			}
		}
	}
}

static
void
jit_begin_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerJitBegin ())
		return;

	uint64_t method_id;
	uint64_t module_id;
	uint32_t method_token;

	get_jit_data (method, &method_id, &module_id, &method_token, NULL, NULL);

	fire_event_enter ();

	FireEtwMonoProfilerJitBegin (
		method_id,
		module_id,
		method_token,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
jit_failed_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerJitFailed ())
		return;

	uint64_t method_id;
	uint64_t module_id;
	uint32_t method_token;

	get_jit_data (method, &method_id, &module_id, &method_token, NULL, NULL);

	fire_event_enter ();

	FireEtwMonoProfilerJitFailed (
		method_id,
		module_id,
		method_token,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
jit_done_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoJitInfo *ji)
{
	if (!EventEnabledMonoProfilerJitDone () && !EventEnabledMonoProfilerJitDone_V1 () && !EventEnabledMonoProfilerJitDoneVerbose ())
		return;

	bool verbose = (RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.Level >= (uint8_t)EP_EVENT_LEVEL_VERBOSE);

	uint64_t method_id;
	uint64_t module_id;
	uint32_t method_token;

	uint32_t method_generic_type_count = 0;
	uint8_t *method_generic_types = NULL;

	char *method_namespace = NULL;
	const char *method_name = NULL;
	char *method_signature = NULL;

	get_jit_data (method, &method_id, &module_id, &method_token, &method_generic_type_count, &method_generic_types);

	if (verbose) {
		//TODO: Optimize string formatting into functions accepting GString to reduce heap alloc.
		method_name = method->name;
		method_signature = mono_signature_full_name (mono_method_signature_internal (method));
		if (method->klass)
			method_namespace = mono_type_get_name_full (m_class_get_byval_arg (method->klass), MONO_TYPE_NAME_FORMAT_IL);
	}

	fire_event_enter ();

	FireEtwMonoProfilerJitDone_V1 (
		method_id,
		module_id,
		method_token,
		method_generic_type_count,
		sizeof (uint8_t) + sizeof (uint64_t),
		method_generic_types,
		NULL,
		NULL);

	if (verbose) {
		FireEtwMonoProfilerJitDoneVerbose (
			method_id,
			(const ep_char8_t *)method_namespace,
			(const ep_char8_t *)method_name,
			(const ep_char8_t *)method_signature,
			NULL,
			NULL);
	}

	fire_event_exit ();

	g_free (method_namespace);
	g_free (method_signature);
	g_free (method_generic_types);
}

static
void
jit_chunk_created_callback (
	MonoProfiler *prof,
	const mono_byte *chunk,
	uintptr_t size)
{
	if (!EventEnabledMonoProfilerJitChunkCreated ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerJitChunkCreated (
		chunk,
		(uint64_t)size,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
jit_chunk_destroyed_callback (
	MonoProfiler *prof,
	const mono_byte *chunk)
{
	if (!EventEnabledMonoProfilerJitChunkDestroyed ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerJitChunkDestroyed (
		chunk,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
jit_code_buffer_callback (
	MonoProfiler *prof,
	const mono_byte *buffer,
	uint64_t size,
	MonoProfilerCodeBufferType type,
	const void *data)
{
	if (!EventEnabledMonoProfilerJitCodeBuffer ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerJitCodeBuffer (
		buffer,
		size,
		(uint8_t)type,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
class_loading_callback (
	MonoProfiler *prof,
	MonoClass *klass)
{
	if (!EventEnabledMonoProfilerClassLoading ())
		return;

	uint64_t class_id;
	uint64_t module_id;

	get_class_data (klass, &class_id, &module_id, NULL, NULL, NULL);

	fire_event_enter ();

	FireEtwMonoProfilerClassLoading (
		class_id,
		module_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
class_failed_callback (
	MonoProfiler *prof,
	MonoClass *klass)
{
	if (!EventEnabledMonoProfilerClassFailed ())
		return;

	uint64_t class_id;
	uint64_t module_id;

	get_class_data (klass, &class_id, &module_id, NULL, NULL, NULL);

	fire_event_enter ();

	FireEtwMonoProfilerClassFailed (
		class_id,
		module_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
class_loaded_callback (
	MonoProfiler *prof,
	MonoClass *klass)
{
	if (!EventEnabledMonoProfilerClassLoaded () && !EventEnabledMonoProfilerClassLoaded_V1 ())
		return;

	uint64_t class_id;
	uint64_t module_id;
	ep_char8_t *class_name;

	uint32_t class_generic_type_count = 0;
	uint8_t *class_generic_types = NULL;

	get_class_data (klass, &class_id, &module_id, &class_name, &class_generic_type_count, &class_generic_types);

	fire_event_enter ();

	FireEtwMonoProfilerClassLoaded_V1 (
		class_id,
		module_id,
		class_name ? class_name : "",
		class_generic_type_count,
		sizeof (uint8_t) + sizeof (uint64_t),
		class_generic_types,
		NULL,
		NULL);

	fire_event_exit ();

	g_free (class_name);
	g_free (class_generic_types);
}

static
void
get_vtable_data (
	MonoVTable *vtable,
	uint64_t *vtable_id,
	uint64_t *class_id,
	uint64_t *domain_id)
{
	*vtable_id = (uint64_t)vtable;
	*class_id = 0;
	*domain_id = 0;

	if (vtable) {
		*class_id = (uint64_t)mono_vtable_class_internal (vtable);
		*domain_id = (uint64_t)mono_vtable_domain_internal (vtable);
	}
}

static
void
vtable_loading_callback (
	MonoProfiler *prof,
	MonoVTable *vtable)
{
	if (!EventEnabledMonoProfilerVTableLoading ())
		return;

	uint64_t vtable_id;
	uint64_t class_id;
	uint64_t domain_id;

	get_vtable_data (vtable, &vtable_id, &class_id, &domain_id);

	fire_event_enter ();

	FireEtwMonoProfilerVTableLoading (
		vtable_id,
		class_id,
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
vtable_failed_callback (
	MonoProfiler *prof,
	MonoVTable *vtable)
{
	if (!EventEnabledMonoProfilerVTableFailed ())
		return;

	uint64_t vtable_id;
	uint64_t class_id;
	uint64_t domain_id;

	get_vtable_data (vtable, &vtable_id, &class_id, &domain_id);

	fire_event_enter ();

	FireEtwMonoProfilerVTableFailed (
		vtable_id,
		class_id,
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
vtable_loaded_callback (
	MonoProfiler *prof,
	MonoVTable *vtable)
{
	if (!EventEnabledMonoProfilerVTableLoaded ())
		return;

	uint64_t vtable_id;
	uint64_t class_id;
	uint64_t domain_id;

	get_vtable_data (vtable, &vtable_id, &class_id, &domain_id);

	fire_event_enter ();

	FireEtwMonoProfilerVTableLoaded (
		vtable_id,
		class_id,
		domain_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
module_loading_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleLoading ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerModuleLoading (
		(uint64_t)image,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
module_failed_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleFailed ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerModuleFailed (
		(uint64_t)image,
		NULL,
		NULL);

	fire_event_exit ();
}

static
const char *
get_module_path (MonoImage *image)
{
	if (image && image->filename) {
		/* if there's a filename, use it */
		return image->filename;
	} else if (image && image->module_name) {
		/* otherwise, use the module name */
		return image->module_name;
	}

	return "";
}

static
void
module_loaded_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleLoaded ())
		return;

	uint64_t module_id = (uint64_t)image;
	const ep_char8_t *module_path = NULL;
	const ep_char8_t *module_guid = NULL;

	if (image) {
		module_path = get_module_path (image);
		module_guid = (const ep_char8_t *)mono_image_get_guid (image);
	}

	fire_event_enter ();

	FireEtwMonoProfilerModuleLoaded (
		module_id,
		module_path ? module_path : "",
		module_guid ? module_guid : "",
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
module_unloading_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleUnloading ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerModuleUnloading (
		(uint64_t)image,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
module_unloaded_callback (
	MonoProfiler *prof,
	MonoImage *image)
{
	if (!EventEnabledMonoProfilerModuleUnloaded ())
		return;

	uint64_t module_id = (uint64_t)image;
	const ep_char8_t *module_path = NULL;
	const ep_char8_t *module_guid = NULL;

	if (image) {
		module_path = get_module_path (image);
		module_guid = (const ep_char8_t *)mono_image_get_guid (image);
	}

	fire_event_enter ();

	FireEtwMonoProfilerModuleUnloaded (
		module_id,
		module_path ? module_path : "",
		module_guid ? module_guid : "",
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
get_assembly_data (
	MonoAssembly *assembly,
	uint64_t *assembly_id,
	uint64_t *module_id,
	ep_char8_t **assembly_name)
{
	*assembly_id = (uint64_t)assembly;
	*module_id = 0;

	if (assembly)
		*module_id = (uint64_t)mono_assembly_get_image_internal (assembly);

	if (assembly && assembly_name)
		*assembly_name = (ep_char8_t *)mono_stringify_assembly_name (&assembly->aname);
	else if (assembly_name)
		*assembly_name = NULL;
}

static
void
assembly_loading_callback (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyLoading ())
		return;

	uint64_t assembly_id;
	uint64_t module_id;

	get_assembly_data (assembly, &assembly_id, &module_id, NULL);

	fire_event_enter ();

	FireEtwMonoProfilerAssemblyLoading (
		assembly_id,
		module_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
assembly_loaded_callback (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyLoaded ())
		return;

	uint64_t assembly_id;
	uint64_t module_id;
	ep_char8_t *assembly_name;

	get_assembly_data (assembly, &assembly_id, &module_id, &assembly_name);

	fire_event_enter ();

	FireEtwMonoProfilerAssemblyLoaded (
		assembly_id,
		module_id,
		assembly_name ? assembly_name : "",
		NULL,
		NULL);

	fire_event_exit ();

	g_free (assembly_name);
}

static
void
assembly_unloading_callback (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyUnloading ())
		return;

	uint64_t assembly_id;
	uint64_t module_id;

	get_assembly_data (assembly, &assembly_id, &module_id, NULL);

	fire_event_enter ();

	FireEtwMonoProfilerAssemblyUnloading (
		assembly_id,
		module_id,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
assembly_unloaded_callback (
	MonoProfiler *prof,
	MonoAssembly *assembly)
{
	if (!EventEnabledMonoProfilerAssemblyUnloaded ())
		return;

	uint64_t assembly_id;
	uint64_t module_id;
	ep_char8_t *assembly_name;

	get_assembly_data (assembly, &assembly_id, &module_id, &assembly_name);

	fire_event_enter ();

	FireEtwMonoProfilerAssemblyUnloaded (
		assembly_id,
		module_id,
		assembly_name ? assembly_name : "",
		NULL,
		NULL);

	fire_event_exit ();

	g_free (assembly_name);
}

static
void
method_enter_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoProfilerCallContext *context)
{
	if (!EventEnabledMonoProfilerMethodEnter ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodEnter (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
method_leave_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoProfilerCallContext *context)
{
	if (!EventEnabledMonoProfilerMethodLeave ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodLeave (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
method_tail_call_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoMethod *target_method)
{
	if (!EventEnabledMonoProfilerMethodTailCall ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodTailCall (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
method_exception_leave_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	MonoObject *exc)
{
	if (!EventEnabledMonoProfilerMethodExceptionLeave ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodExceptionLeave (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
method_free_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerMethodFree ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodFree (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
method_begin_invoke_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerMethodBeginInvoke ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodBeginInvoke (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
method_end_invoke_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (!EventEnabledMonoProfilerMethodEndInvoke ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMethodEndInvoke (
		(uint64_t)method,
		NULL,
		NULL);

	fire_event_exit ();
}

static
MonoProfilerCallInstrumentationFlags
method_instrumentation_filter_callback (
	MonoProfiler *prof,
	MonoMethod *method)
{
	if (_mono_profiler_provider_callspec.len > 0 && !mono_callspec_eval (method, &_mono_profiler_provider_callspec))
		return MONO_PROFILER_CALL_INSTRUMENTATION_NONE;

	return MONO_PROFILER_CALL_INSTRUMENTATION_ENTER |
			MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE |
			MONO_PROFILER_CALL_INSTRUMENTATION_TAIL_CALL |
			MONO_PROFILER_CALL_INSTRUMENTATION_EXCEPTION_LEAVE;
}

static
void
exception_throw_callback (
	MonoProfiler *prof,
	MonoObject *exc)
{
	if (!EventEnabledMonoProfilerExceptionThrow ())
		return;

	uint64_t type_id = 0;

	if (exc && mono_object_class(exc))
		type_id = (uint64_t)m_class_get_byval_arg (mono_object_class(exc));

	fire_event_enter ();

	FireEtwMonoProfilerExceptionThrow (
		type_id,
		SGEN_POINTER_UNTAG_ALL (exc),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
exception_clause_callback (
	MonoProfiler *prof,
	MonoMethod *method,
	uint32_t clause_num,
	MonoExceptionEnum clause_type,
	MonoObject *exc)
{
	if (!EventEnabledMonoProfilerExceptionClause ())
		return;

	uint64_t type_id = 0;

	if (exc && mono_object_class(exc))
		type_id = (uint64_t)m_class_get_byval_arg (mono_object_class(exc));

	fire_event_enter ();

	FireEtwMonoProfilerExceptionClause (
		(uint8_t)clause_type,
		clause_num,
		(uint64_t)method,
		type_id,
		SGEN_POINTER_UNTAG_ALL (exc),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_event_callback (
	MonoProfiler *prof,
	MonoProfilerGCEvent gc_event,
	uint32_t generation,
	mono_bool serial)
{
	switch (gc_event) {
	case MONO_GC_EVENT_PRE_STOP_WORLD:
	case MONO_GC_EVENT_POST_START_WORLD_UNLOCKED:
	{
		FireEtwMonoProfilerGCEvent (
			(uint8_t)gc_event,
			generation,
			NULL,
			NULL);
		break;
	}
	case MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED:
	{
		FireEtwMonoProfilerGCEvent (
			(uint8_t)gc_event,
			generation,
			NULL,
			NULL);

		gc_in_progress_start ();

		if (gc_heap_dump_in_progress ()) {
			FireEtwMonoProfilerGCHeapDumpStart (
				provider_params_get_heap_collect_ondemand_value (),
				NULL,
				NULL);
		}

		break;
	}
	case MONO_GC_EVENT_POST_STOP_WORLD:
	{
		if (gc_in_progress ()) {
			uint64_t enabled_keywords = RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.EnabledKeywordsBitmask;

			if (is_keword_enabled (enabled_keywords, GC_ROOT_KEYWORD)) {
				mono_profiler_set_gc_root_register_callback (_mono_profiler_provider, NULL);
				mono_profiler_set_gc_root_unregister_callback (_mono_profiler_provider, NULL);
				mono_profiler_set_gc_root_register_callback (_mono_heap_dump_profiler_provider, buffer_gc_event_root_register_callback);
				mono_profiler_set_gc_root_unregister_callback (_mono_heap_dump_profiler_provider, buffer_gc_event_root_unregister_callback);
			}

			if (gc_heap_dump_in_progress ()) {
				if (is_keword_enabled (enabled_keywords, GC_ROOT_KEYWORD)) {
					mono_profiler_set_gc_roots_callback (_mono_heap_dump_profiler_provider, buffer_gc_event_roots_callback);
				}

				if (is_keword_enabled (enabled_keywords, GC_MOVES_KEYWORD)) {
					mono_profiler_set_gc_moves_callback (_mono_heap_dump_profiler_provider, buffer_gc_event_moves_callback);
				}

				if (is_keword_enabled (enabled_keywords, GC_RESIZE_KEYWORD)) {
					mono_profiler_set_gc_resize_callback (_mono_heap_dump_profiler_provider, buffer_gc_event_resize_callback);
				}
			}

			buffer_gc_event (
				(uint8_t)gc_event,
				generation);
		}
		break;
	}
	case MONO_GC_EVENT_START:
	case MONO_GC_EVENT_END:
	{
		if (gc_in_progress ()) {
			buffer_gc_event (
				(uint8_t)gc_event,
				generation);
		}
		break;
	}
	case MONO_GC_EVENT_PRE_START_WORLD:
	{
		if (gc_in_progress ()) {
			uint64_t enabled_keywords = RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.EnabledKeywordsBitmask;

			if (gc_heap_dump_in_progress () && is_keword_enabled (enabled_keywords, GC_HEAP_DUMP_KEYWORD))
				mono_gc_walk_heap (0, buffer_gc_event_heap_dump_object_reference_callback, NULL);

			mono_profiler_set_gc_root_register_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_root_unregister_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_roots_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_moves_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_resize_callback (_mono_heap_dump_profiler_provider, NULL);

			if (is_keword_enabled (enabled_keywords, GC_ROOT_KEYWORD)) {
				mono_profiler_set_gc_root_register_callback (_mono_profiler_provider, gc_root_register_callback);
				mono_profiler_set_gc_root_unregister_callback (_mono_profiler_provider, gc_root_unregister_callback);
			}

			buffer_gc_event (
				(uint8_t)gc_event,
				generation);
		}

		break;
	}
	case MONO_GC_EVENT_POST_START_WORLD:
	{
		if (gc_in_progress ()) {
			GHashTable *cache = NULL;
			uint64_t enabled_keywords = RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.EnabledKeywordsBitmask;

			if (gc_heap_dump_in_progress () && is_keword_enabled (enabled_keywords, GC_HEAP_DUMP_VTABLE_CLASS_REF_KEYWORD))
				cache = g_hash_table_new_full (NULL, NULL, NULL, NULL);

			fire_buffered_gc_events_in_alloc_order (cache);
			fire_cached_gc_events (cache);

			if (cache)
				g_hash_table_destroy (cache);

			if (gc_heap_dump_in_progress ()) {
				FireEtwMonoProfilerGCHeapDumpStop (
					NULL,
					NULL);
			}

			FireEtwMonoProfilerGCEvent (
				(uint8_t)gc_event,
				generation,
				NULL,
				NULL);

			if (!is_keword_enabled (enabled_keywords, GC_KEYWORD))
				mono_profiler_set_gc_event_callback (_mono_profiler_provider, NULL);

			gc_heap_dump_in_progress_stop ();
			gc_in_progress_stop ();
		}
		break;
	}
	default:
		break;
	}
}

static
void
gc_allocation_callback (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerGCAllocation ())
		return;

	uint64_t vtable_id = 0;
	uint64_t object_size = 0;

	if (object) {
		vtable_id = (uint64_t)mono_object_get_vtable_internal (object);
		object_size = (uint64_t)mono_object_get_size_internal (object);

		/* account for object alignment */
		object_size += 7;
		object_size &= ~7;
	}

	fire_event_enter ();

	FireEtwMonoProfilerGCAllocation (
		vtable_id,
		SGEN_POINTER_UNTAG_ALL (object),
		object_size,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_handle_created_callback (
	MonoProfiler *prof,
	uint32_t handle,
	MonoGCHandleType type,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerGCHandleCreated ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCHandleCreated (
		handle,
		(uint8_t)type,
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_handle_deleted_callback (
	MonoProfiler *prof,
	uint32_t handle,
	MonoGCHandleType type)
{
	if (!EventEnabledMonoProfilerGCHandleDeleted ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCHandleDeleted (
		handle,
		(uint8_t)type,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_finalizing_callback (MonoProfiler *prof)
{
	if (!EventEnabledMonoProfilerGCFinalizing ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCFinalizing (
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_finalized_callback (MonoProfiler *prof)
{
	if (!EventEnabledMonoProfilerGCFinalized ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCFinalized (
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_finalizing_object_callback (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerGCFinalizingObject ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCFinalizingObject (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_finalized_object_callback (
	MonoProfiler *prof,
	MonoObject * object)
{
	if (!EventEnabledMonoProfilerGCFinalizedObject ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCFinalizedObject (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_root_register_callback (
	MonoProfiler *prof,
	const mono_byte *start,
	uintptr_t size,
	MonoGCRootSource source,
	const void * key,
	const char * name)
{
	if (!EventEnabledMonoProfilerGCRootRegister ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCRootRegister (
		start,
		(uint64_t)size,
		(uint8_t) source,
		(uint64_t)key,
		(const ep_char8_t *)(name ? name : ""),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
gc_root_unregister_callback (
	MonoProfiler *prof,
	const mono_byte *start)
{
	if (!EventEnabledMonoProfilerGCRootUnregister ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerGCRootUnregister (
		start,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
monitor_contention_callback (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerMonitorContention ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMonitorContention (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
monitor_failed_callback (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerMonitorFailed ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMonitorFailed (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
monitor_acquired_callback (
	MonoProfiler *prof,
	MonoObject *object)
{
	if (!EventEnabledMonoProfilerMonitorAcquired ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerMonitorAcquired (
		SGEN_POINTER_UNTAG_ALL (object),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
thread_started_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadStarted ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerThreadStarted (
		(uint64_t)tid,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
thread_stopping_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadStopping ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerThreadStopping (
		(uint64_t)tid,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
thread_stopped_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadStopped ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerThreadStopped (
		(uint64_t)tid,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
thread_exited_callback (
	MonoProfiler *prof,
	uintptr_t tid)
{
	if (!EventEnabledMonoProfilerThreadExited ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerThreadExited (
		(uint64_t)tid,
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
thread_name_callback (
	MonoProfiler *prof,
	uintptr_t tid,
	const char *name)
{
	if (!EventEnabledMonoProfilerThreadName ())
		return;

	fire_event_enter ();

	FireEtwMonoProfilerThreadName (
		(uint64_t)tid,
		(ep_char8_t *)(name ? name : ""),
		NULL,
		NULL);

	fire_event_exit ();
}

static
void
calculate_live_keywords (
	uint64_t *live_keywords,
	bool *trigger_heap_dump)
{
	uint64_t keywords[] = { GC_HEAP_COLLECT_KEYWORD };
	uint64_t count[] = { 0 };

	ep_requires_lock_held ();

	EP_ASSERT (G_N_ELEMENTS (keywords) == G_N_ELEMENTS (count));
	*live_keywords = ep_rt_mono_session_calculate_and_count_all_keywords (
		"Microsoft-DotNETRuntimeMonoProfiler",
		keywords,
		count,
		G_N_ELEMENTS (count));

	*trigger_heap_dump = ep_rt_mono_is_runtime_initialized ();
	*trigger_heap_dump &= is_keword_enabled (*live_keywords, GC_KEYWORD);
	*trigger_heap_dump &= is_keword_enabled (*live_keywords, GC_HEAP_COLLECT_KEYWORD);
	*trigger_heap_dump &= count [0] > _gc_heap_dump_trigger_count;

	_gc_heap_dump_trigger_count = count [0];

	ep_requires_lock_held ();
}

static
void
eventpipe_provider_callback (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	EP_ASSERT (_mono_profiler_provider_enabled);

	ep_rt_config_requires_lock_not_held ();
	ep_rt_spin_lock_requires_lock_held (&_gc_lock);

	EP_ASSERT (_mono_profiler_provider != NULL);
	EP_ASSERT (_mono_heap_dump_profiler_provider != NULL);

	EP_LOCK_ENTER (section1)
		uint64_t live_keywords = 0;
		bool trigger_heap_dump = false;
		calculate_live_keywords (&live_keywords, &trigger_heap_dump);

		if (is_keword_enabled(live_keywords, LOADER_KEYWORD)) {
			mono_profiler_set_domain_loading_callback (_mono_profiler_provider, app_domain_loading_callback);
			mono_profiler_set_domain_loaded_callback (_mono_profiler_provider, app_domain_loaded_callback);
			mono_profiler_set_domain_unloading_callback (_mono_profiler_provider, app_domain_unloading_callback);
			mono_profiler_set_domain_unloaded_callback (_mono_profiler_provider, app_domain_unloaded_callback);
			mono_profiler_set_domain_name_callback (_mono_profiler_provider, app_domain_name_callback);
			mono_profiler_set_image_loading_callback (_mono_profiler_provider, module_loading_callback);
			mono_profiler_set_image_failed_callback (_mono_profiler_provider, module_failed_callback);
			mono_profiler_set_image_loaded_callback (_mono_profiler_provider, module_loaded_callback);
			mono_profiler_set_image_unloading_callback (_mono_profiler_provider, module_unloading_callback);
			mono_profiler_set_image_unloaded_callback (_mono_profiler_provider, module_unloaded_callback);
			mono_profiler_set_assembly_loading_callback (_mono_profiler_provider, assembly_loading_callback);
			mono_profiler_set_assembly_loaded_callback (_mono_profiler_provider, assembly_loaded_callback);
			mono_profiler_set_assembly_unloading_callback (_mono_profiler_provider, assembly_unloading_callback);
			mono_profiler_set_assembly_unloaded_callback (_mono_profiler_provider, assembly_unloaded_callback);
		} else {
			mono_profiler_set_domain_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_unloading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_unloaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_name_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_unloading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_unloaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_unloading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_unloaded_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, JIT_KEYWORD)) {
			mono_profiler_set_jit_begin_callback (_mono_profiler_provider, jit_begin_callback);
			mono_profiler_set_jit_failed_callback (_mono_profiler_provider, jit_failed_callback);
			mono_profiler_set_jit_done_callback (_mono_profiler_provider, jit_done_callback);
			mono_profiler_set_jit_chunk_created_callback (_mono_profiler_provider, jit_chunk_created_callback);
			mono_profiler_set_jit_chunk_destroyed_callback (_mono_profiler_provider, jit_chunk_destroyed_callback);
			mono_profiler_set_jit_code_buffer_callback (_mono_profiler_provider, jit_code_buffer_callback);
		} else {
			mono_profiler_set_jit_begin_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_done_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_chunk_created_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_chunk_destroyed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_code_buffer_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, TYPE_LOADING_KEYWORD)) {
			mono_profiler_set_class_loading_callback (_mono_profiler_provider, class_loading_callback);
			mono_profiler_set_class_failed_callback (_mono_profiler_provider, class_failed_callback);
			mono_profiler_set_class_loaded_callback (_mono_profiler_provider, class_loaded_callback);
			mono_profiler_set_vtable_loading_callback (_mono_profiler_provider, vtable_loading_callback);
			mono_profiler_set_vtable_failed_callback (_mono_profiler_provider, vtable_failed_callback);
			mono_profiler_set_vtable_loaded_callback (_mono_profiler_provider, vtable_loaded_callback);
		} else {
			mono_profiler_set_class_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_class_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_class_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_vtable_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_vtable_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_vtable_loaded_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, METHOD_TRACING_KEYWORD)) {
			mono_profiler_set_method_enter_callback (_mono_profiler_provider, method_enter_callback);
			mono_profiler_set_method_leave_callback (_mono_profiler_provider, method_leave_callback);
			mono_profiler_set_method_tail_call_callback (_mono_profiler_provider, method_tail_call_callback);
			mono_profiler_set_method_exception_leave_callback (_mono_profiler_provider, method_exception_leave_callback);
			mono_profiler_set_method_free_callback (_mono_profiler_provider, method_free_callback);
			mono_profiler_set_method_begin_invoke_callback (_mono_profiler_provider, method_begin_invoke_callback);
			mono_profiler_set_method_end_invoke_callback (_mono_profiler_provider, method_end_invoke_callback);
		} else {
			mono_profiler_set_method_enter_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_leave_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_tail_call_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_exception_leave_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_free_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_begin_invoke_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_end_invoke_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, EXCEPTION_KEYWORD)) {
			mono_profiler_set_exception_throw_callback (_mono_profiler_provider, exception_throw_callback);
			mono_profiler_set_exception_clause_callback (_mono_profiler_provider, exception_clause_callback);
		} else {
			mono_profiler_set_exception_throw_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_exception_clause_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, GC_KEYWORD)) {
			mono_profiler_set_gc_event_callback (_mono_profiler_provider, gc_event_callback);
		} else {
			// NOTE, disabled in mono_profiler_gc_event, MONO_GC_EVENT_POST_START_WORLD to make sure all
			// callbacks during GC fires.
		}

		if (is_keword_enabled(live_keywords, GC_ALLOCATION_KEYWORD)) {
			mono_profiler_set_gc_allocation_callback (_mono_profiler_provider, gc_allocation_callback);
		} else {
			mono_profiler_set_gc_allocation_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, GC_HANDLE_KEYWORD)) {
			mono_profiler_set_gc_handle_created_callback (_mono_profiler_provider, gc_handle_created_callback);
			mono_profiler_set_gc_handle_deleted_callback (_mono_profiler_provider, gc_handle_deleted_callback);
		} else {
			mono_profiler_set_gc_handle_created_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_handle_deleted_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, GC_FINALIZATION_KEYWORD)) {
			mono_profiler_set_gc_finalizing_callback (_mono_profiler_provider, gc_finalizing_callback);
			mono_profiler_set_gc_finalized_callback (_mono_profiler_provider, gc_finalized_callback);
			mono_profiler_set_gc_finalizing_object_callback (_mono_profiler_provider, gc_finalizing_object_callback);
			mono_profiler_set_gc_finalized_object_callback (_mono_profiler_provider, gc_finalized_object_callback);
		} else {
			mono_profiler_set_gc_finalizing_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalized_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalizing_object_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalized_object_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled(live_keywords, GC_ROOT_KEYWORD)) {
			mono_profiler_set_gc_root_register_callback (_mono_profiler_provider, gc_root_register_callback);
			mono_profiler_set_gc_root_unregister_callback (_mono_profiler_provider, gc_root_unregister_callback);
		} else {
			mono_profiler_set_gc_root_register_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_root_unregister_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled (live_keywords, GC_HEAP_COLLECT_KEYWORD)) {
			mono_profiler_set_gc_finalized_callback (_mono_heap_dump_profiler_provider, gc_heap_dump_trigger_callback);
		} else {
			mono_profiler_set_gc_finalized_callback (_mono_heap_dump_profiler_provider, NULL);
		}

		if (is_keword_enabled (live_keywords, MONITOR_KEYWORD) || is_keword_enabled (match_any_keywords, CONTENTION_KEYWORD)) {
			mono_profiler_set_monitor_contention_callback (_mono_profiler_provider, monitor_contention_callback);
		} else {
			mono_profiler_set_monitor_contention_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled (live_keywords, MONITOR_KEYWORD)) {
			mono_profiler_set_monitor_failed_callback (_mono_profiler_provider, monitor_failed_callback);
			mono_profiler_set_monitor_acquired_callback (_mono_profiler_provider, monitor_acquired_callback);
		} else {
			mono_profiler_set_monitor_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_monitor_acquired_callback (_mono_profiler_provider, NULL);
		}

		if (is_keword_enabled (live_keywords, THREADING_KEYWORD)) {
			mono_profiler_set_thread_started_callback (_mono_profiler_provider, thread_started_callback);
			mono_profiler_set_thread_stopping_callback (_mono_profiler_provider, thread_stopping_callback);
			mono_profiler_set_thread_stopped_callback (_mono_profiler_provider, thread_stopped_callback);
			mono_profiler_set_thread_exited_callback (_mono_profiler_provider, thread_exited_callback);
			mono_profiler_set_thread_name_callback (_mono_profiler_provider, thread_name_callback);
		} else {
			mono_profiler_set_thread_started_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_stopping_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_stopped_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_exited_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_name_callback (_mono_profiler_provider, NULL);
		}

		if (_mono_profiler_provider_callspec.enabled) {
			if (is_keword_enabled(live_keywords, METHOD_INSTRUMENTATION_KEYWORD)) {
				mono_profiler_set_call_instrumentation_filter_callback (_mono_profiler_provider, method_instrumentation_filter_callback);
			} else {
				mono_profiler_set_call_instrumentation_filter_callback (_mono_profiler_provider, NULL);
			}
		}

		if (trigger_heap_dump) {
			if (filter_data) {
				if (provider_params_contains_heap_collect_ondemand (filter_data) && !provider_params_remove (filter_data)) {
					provider_params_add (filter_data);
				}
			}

			gc_heap_dump_request_params_push_value (filter_data);
			gc_heap_dump_requests_inc ();
			mono_gc_finalize_notify ();
		} else {
			provider_params_free ();
		}

		RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.Level = level;
		RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.EnabledKeywordsBitmask = live_keywords;
		RUNTIME_MONO_PROFILER_PROVIDER_CONTEXT.IsEnabled = (live_keywords == 1 ? true : false);
	EP_LOCK_EXIT (section1)

ep_on_exit:
	ep_rt_config_requires_lock_not_held ();
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
EP_CALLBACK_CALLTYPE
EventPipeEtwCallbackDotNETRuntimeMonoProfiler (
	const uint8_t *source_id,
	unsigned long is_enabled,
	uint8_t level,
	uint64_t match_any_keywords,
	uint64_t match_all_keywords,
	EventFilterDescriptor *filter_data,
	void *callback_data)
{
	if (!_mono_profiler_provider_enabled) {
		mono_trace (
			G_LOG_LEVEL_WARNING,
			MONO_TRACE_DIAGNOSTICS,
			"Microsoft-DotNETRuntimeMonoProfiler disabled, "
			"set MONO_DIAGNOSTICS=--diagnostic-mono-profiler=enable "
			"environment variable to enable provider.");
		return;
	}

	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);

	EP_SPIN_LOCK_ENTER (&_gc_lock, section1);
		eventpipe_provider_callback (
			source_id,
			is_enabled,
			level,
			match_any_keywords,
			match_all_keywords,
			filter_data,
			callback_data);
	EP_SPIN_LOCK_EXIT (&_gc_lock, section1);

ep_on_exit:
	ep_rt_spin_lock_requires_lock_not_held (&_gc_lock);
	return;

ep_on_error:
	ep_exit_error_handler ();
}

void
ep_rt_mono_profiler_provider_component_init (void)
{
	if (_mono_profiler_provider_enabled) {
		_mono_profiler_provider = mono_profiler_create (NULL);
		_mono_heap_dump_profiler_provider = mono_profiler_create (NULL);
	}
}

void
ep_rt_mono_profiler_provider_init (void)
{
	if (_mono_profiler_provider_enabled) {
		EP_ASSERT (_mono_profiler_provider != NULL);
		EP_ASSERT (_mono_heap_dump_profiler_provider != NULL);

		ep_rt_spin_lock_alloc (&_gc_lock);
	}
}

void
ep_rt_mono_profiler_provider_fini (void)
{
	if (_mono_profiler_provider_enabled) {
		if (_mono_profiler_provider) {
			mono_profiler_set_gc_root_register_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_root_unregister_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_event_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_allocation_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_handle_created_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_handle_deleted_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalizing_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalized_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalizing_object_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_gc_finalized_object_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_domain_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_unloading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_unloaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_domain_name_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_image_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_unloading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_image_unloaded_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_assembly_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_loaded_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_unloading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_assembly_unloaded_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_jit_begin_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_done_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_chunk_created_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_chunk_destroyed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_jit_code_buffer_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_class_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_class_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_class_loaded_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_vtable_loading_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_vtable_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_vtable_loaded_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_method_enter_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_leave_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_tail_call_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_exception_leave_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_free_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_begin_invoke_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_method_end_invoke_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_exception_throw_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_exception_clause_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_monitor_contention_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_monitor_failed_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_monitor_acquired_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_thread_started_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_stopping_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_stopped_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_exited_callback (_mono_profiler_provider, NULL);
			mono_profiler_set_thread_name_callback (_mono_profiler_provider, NULL);

			mono_profiler_set_call_instrumentation_filter_callback (_mono_profiler_provider, NULL);
		}

		if (_mono_heap_dump_profiler_provider) {
			mono_profiler_set_gc_root_register_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_root_unregister_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_roots_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_moves_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_resize_callback (_mono_heap_dump_profiler_provider, NULL);
			mono_profiler_set_gc_finalized_callback (_mono_heap_dump_profiler_provider, NULL);
		}

		_gc_heap_dump_requests = 0;
		_gc_heap_dump_in_progress = 0;
		_gc_heap_dump_trigger_count = 0;
		_gc_state = 0;
		
		// We were cleaning up resources (mutexes, tls data, etc) here but it races with
		// other threads on shutdown. Skipping cleanup to prevent failures. If unloading
		// and not leaking these threads becomes a priority we will have to reimplement
		// cleanup here.
	}
}

static
bool
profiler_parse_options (const ep_char8_t *option)
{
	do {
		if (!*option)
			return false;

		if (!strncmp (option, "enable", 6)) {
			_mono_profiler_provider_enabled = true;
			option += 6;
		} else if (!strncmp (option, "disable", 7)) {
			_mono_profiler_provider_enabled = false;
			option += 7;
		} else if (!strncmp (option, "alloc", 5)) {
			_mono_profiler_provider_enabled = true;
			mono_profiler_enable_allocations ();
			option += 5;
		} else if (!strncmp (option, "exception", 9)) {
			_mono_profiler_provider_enabled = true;
			mono_profiler_enable_clauses ();
			option += 9;
		/*} else if (!strncmp (option, "sample", 6)) {
		*	_mono_profiler_provider_enabled = true;
			mono_profiler_enable_sampling (_mono_profiler_provider);
			option += 6;*/
		} else {
			return false;
		}

		if (*option == ',')
			option++;
	} while (*option);

	return true;
}

bool
ep_rt_mono_profiler_provider_parse_options (const char *options)
{
	if (!options)
		return false;

	if (strncmp (options, "--diagnostic-mono-profiler=", 27) == 0) {
		if (!profiler_parse_options (options + 27))
			mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed parsing MONO_DIAGNOSTICS environment variable option: %s", options);
		return true;
	} else if (strncmp (options, "--diagnostic-mono-profiler-callspec=", 36) == 0) {
		char *errstr = NULL;
		if (!mono_callspec_parse (options + 36, &_mono_profiler_provider_callspec, &errstr)) {
			mono_trace (G_LOG_LEVEL_ERROR, MONO_TRACE_DIAGNOSTICS, "Failed parsing '%s': %s", options, errstr);
			g_free (errstr);
			mono_callspec_cleanup (&_mono_profiler_provider_callspec);
		} else {
			mono_profiler_set_call_instrumentation_filter_callback (_mono_profiler_provider, method_instrumentation_filter_callback);
		}
		return true;
	} else {
		return false;
	}
}

#endif /* ENABLE_PERFTRACING */

MONO_EMPTY_SOURCE_FILE(eventpipe_rt_mono_profiler_provider);
