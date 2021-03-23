/**
 * \file
 * Expensive asserts used when mono is built with --with-checked-build=yes
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */
#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifdef ENABLE_CHECKED_BUILD

#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-tls.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/image-internals.h>
#include <mono/metadata/loaded-images-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/reflection-internals.h>
#include <glib.h>

#ifdef HAVE_BACKTRACE_SYMBOLS
#include <execinfo.h>
#endif

// Selective-enable support

// Returns true for check modes which are allowed by both the current DISABLE_ macros and the MONO_CHECK_MODE env var.
// Argument may be a bitmask; if so, result is true if at least one specified mode is enabled.
mono_bool
mono_check_mode_enabled (MonoCheckMode query)
{
	static MonoCheckMode check_mode = MONO_CHECK_MODE_UNKNOWN;
	if (G_UNLIKELY (check_mode == MONO_CHECK_MODE_UNKNOWN))
	{
		MonoCheckMode env_check_mode = MONO_CHECK_MODE_NONE;
		gchar *env_string = g_getenv ("MONO_CHECK_MODE");

		if (env_string)
		{
			gchar **env_split = g_strsplit (env_string, ",", 0);
			for (gchar **env_component = env_split; *env_component; env_component++)
			{
				mono_bool G_GNUC_UNUSED check_all = g_str_equal (*env_component, "all");
#ifdef ENABLE_CHECKED_BUILD_GC
				if (check_all || g_str_equal (*env_component, "gc"))
					env_check_mode |= MONO_CHECK_MODE_GC;
#endif
#ifdef ENABLE_CHECKED_BUILD_METADATA
				if (check_all || g_str_equal (*env_component, "metadata"))
					env_check_mode |= MONO_CHECK_MODE_METADATA;
#endif
#ifdef ENABLE_CHECKED_BUILD_THREAD
				if (check_all || g_str_equal (*env_component, "thread"))
					env_check_mode |= MONO_CHECK_MODE_THREAD;
#endif
			}
			g_strfreev (env_split);
			g_free (env_string);
		}

		check_mode = env_check_mode;
	}
	return check_mode & query;
}

static int
mono_check_transition_limit (void)
{
	static int transition_limit = -1;
	if (transition_limit < 0) {
		gchar *env_string = g_getenv ("MONO_CHECK_THREAD_TRANSITION_HISTORY");
		if (env_string) {
			transition_limit = atoi (env_string);
			g_free (env_string);
		} else {
			transition_limit = 3;
		}
	}
	return transition_limit;
}

#define MAX_NATIVE_BT 6
#define MAX_NATIVE_BT_PROBE (MAX_NATIVE_BT + 5)
#define MAX_TRANSITIONS (mono_check_transition_limit ())

typedef struct {
	const char *name;
	int from_state, next_state, suspend_count, suspend_count_delta, size;
	gpointer backtrace [MAX_NATIVE_BT_PROBE];
} ThreadTransition;

typedef struct {
	guint32 in_gc_critical_region;
	// ring buffer of transitions, indexed by two guint16 indices:
	// push at buf_end, iterate from buf_start. valid range is
	// buf_start ... buf_end - 1 mod MAX_TRANSITIONS
	gint32 ringbuf;
	ThreadTransition transitions [MONO_ZERO_LEN_ARRAY];
} CheckState;

static MonoNativeTlsKey thread_status;

static mono_mutex_t backtrace_mutex;


void
checked_build_init (void)
{
	// Init state for get_state, which can be called either by gc or thread mode
	if (mono_check_mode_enabled (MONO_CHECK_MODE_GC | MONO_CHECK_MODE_THREAD))
		mono_native_tls_alloc (&thread_status, NULL);
#if HAVE_BACKTRACE_SYMBOLS
	mono_os_mutex_init (&backtrace_mutex);
#endif
}

static gboolean
backtrace_mutex_trylock (void)
{
	return mono_os_mutex_trylock (&backtrace_mutex) == 0;
}

static void
backtrace_mutex_unlock (void)
{
	mono_os_mutex_unlock (&backtrace_mutex);
}

static CheckState*
get_state (void)
{
	CheckState *state = (CheckState*)mono_native_tls_get_value (thread_status);
	if (!state) {
		state = (CheckState*) g_malloc0 (sizeof (CheckState) + sizeof(ThreadTransition) * MAX_TRANSITIONS);
		mono_native_tls_set_value (thread_status, state);
	}

	return state;
}

static void
ringbuf_unpack (gint32 ringbuf, guint16 *buf_start, guint16 *buf_end)
{
	*buf_start = (guint16) (ringbuf >> 16);
	*buf_end = (guint16) (ringbuf & 0x00FF);
}

static gint32
ringbuf_pack (guint16 buf_start, guint16 buf_end)
{
	return ((((gint32)buf_start) << 16) | ((gint32)buf_end));
}

static int
ringbuf_size (guint32 ringbuf, int n)
{
	guint16 buf_start, buf_end;
	ringbuf_unpack (ringbuf, &buf_start, &buf_end);
	if (buf_end > buf_start)
		return buf_end - buf_start;
	else
		return n - (buf_start - buf_end);
}

static guint16
ringbuf_push (gint32 *ringbuf, int n)
{
	gint32 ringbuf_old, ringbuf_new;
	guint16 buf_start, buf_end;
	guint16 cur;
retry:
	ringbuf_old = *ringbuf;
	ringbuf_unpack (ringbuf_old, &buf_start, &buf_end);
	cur = buf_end++;
	if (buf_end == n)
		buf_end = 0;
	if (buf_end == buf_start) {
		if (++buf_start == n)
			buf_start = 0;
	}
	ringbuf_new = ringbuf_pack (buf_start, buf_end);
	if (mono_atomic_cas_i32 (ringbuf, ringbuf_new, ringbuf_old) != ringbuf_old)
		goto retry;
	return cur;
}

#ifdef ENABLE_CHECKED_BUILD_THREAD

#ifdef HAVE_BACKTRACE_SYMBOLS

//XXX We should collect just the IPs and lazily symbolificate them.
static int
collect_backtrace (gpointer out_data[])
{
#if defined (__GNUC__) && !defined (__clang__)
	/* GNU libc backtrace calls _Unwind_Backtrace in libgcc, which internally may take a lock. */
	/* Suppose we're using hybrid suspend and T1 is in GC Unsafe and T2 is
	 * GC Safe.  T1 will be coop suspended, and T2 will be async suspended.
	 * Suppose T1 is in RUNNING, and T2 just changed from RUNNING to
	 * BLOCKING and it is in trace_state_change to record this fact.
	 *
	 * suspend initiator: switches T1 to ASYNC_SUSPEND_REQUESTED
	 * suspend initiator: switches T2 to BLOCKING_SUSPEND_REQUESTED and sends a suspend signal
	 * T1: calls mono_threads_transition_state_poll (),
	 * T1: switches to SELF_SUSPENDED and starts trace_state_change ()
	 * T2: is still in checked_build_thread_transition for the RUNNING->BLOCKING transition and calls backtrace ()
	 * T2: suspend signal lands while T2 is in backtrace() holding a lock; T2 switches to BLOCKING_ASYNC_SUSPENDED () and waits for resume
	 * T1: calls backtrace (), waits for the lock ()
	 * suspend initiator: waiting for T1 to suspend.
	 *
	 * At this point we're deadlocked.
	 *
	 * So what we'll do is try to take a lock before calling backtrace and
	 * only collect a backtrace if there is no contention.
	 */
	int i;
	for (i = 0; i < 2; i++ ) {
		if (backtrace_mutex_trylock ()) {
			int sz = backtrace (out_data, MAX_NATIVE_BT_PROBE);
			backtrace_mutex_unlock ();
			return sz;
		} else {
			mono_thread_info_yield ();
		}
	}
	/* didn't get a backtrace, oh well. */
	return 0;
#else
	return backtrace (out_data, MAX_NATIVE_BT_PROBE);
#endif
}

static char*
translate_backtrace (gpointer native_trace[], int size)
{
	if (size == 0)
		return g_strdup ("");
	char **names = backtrace_symbols (native_trace, size);
	GString* bt = g_string_sized_new (100);

	int i, j = -1;

	//Figure out the cut point of useless backtraces
	//We'll skip up to the caller of checked_build_thread_transition
	for (i = 0; i < size; ++i) {
		if (strstr (names [i], "checked_build_thread_transition")) {
			j = i + 1;
			break;
		}
	}

	if (j == -1)
		j = 0;
	for (i = j; i < size; ++i) {
		if (i - j <= MAX_NATIVE_BT)
			g_string_append_printf (bt, "\tat %s\n", names [i]);
	}

	g_free (names);
	return g_string_free (bt, FALSE);
}

#else

static int
collect_backtrace (gpointer out_data[])
{
	return 0;
}

static char*
translate_backtrace (gpointer native_trace[], int size)
{
	return g_strdup ("\tno backtrace available\n");
}

#endif

void
checked_build_thread_transition (const char *transition, void *info, int from_state, int suspend_count, int next_state, int suspend_count_delta, gboolean capture_backtrace)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_THREAD))
		return;

	/* We currently don't record external changes as those are hard to reason about. */
	if (!mono_thread_info_is_current ((THREAD_INFO_TYPE*)info))
		return;

	CheckState *state = get_state ();

	guint16 cur = ringbuf_push (&state->ringbuf, MAX_TRANSITIONS);

	ThreadTransition *t = &state->transitions[cur];
	t->name = transition;
	t->from_state = from_state;
	t->next_state = next_state;
	t->suspend_count = suspend_count;
	t->suspend_count_delta = suspend_count_delta;
	if (capture_backtrace)
		t->size = collect_backtrace (t->backtrace);
	else
		t->size = 0;
}

void
mono_fatal_with_history (const char * volatile msg, ...)
{
	GString* err = g_string_sized_new (100);

	g_string_append_printf (err, "Assertion failure in thread %p due to: ", mono_native_thread_id_get ());

	va_list args;
	va_start (args, msg);
	g_string_append_vprintf (err, msg, args);
	va_end (args);

	if (mono_check_mode_enabled (MONO_CHECK_MODE_THREAD))
	{
		CheckState *state = get_state ();
		guint16 cur, end;
		int len = ringbuf_size (state->ringbuf, MAX_TRANSITIONS);

		g_string_append_printf (err, "\nLast %d state transitions: (most recent first)\n", len);

		ringbuf_unpack (state->ringbuf, &cur, &end);
		while (cur != end) {
			ThreadTransition *t = &state->transitions[cur];
			char *bt = translate_backtrace (t->backtrace, t->size);
			g_string_append_printf (err, "[%s] %s -> %s (%d) %s%d at:\n%s",
				t->name,
				mono_thread_state_name (t->from_state),
				mono_thread_state_name (t->next_state),
				t->suspend_count,
				t->suspend_count_delta > 0 ? "+" : "", //I'd like to see this sort of values: -1, 0, +1
				t->suspend_count_delta,
				bt);
			g_free (bt);
			if (++cur == MAX_TRANSITIONS)
				cur = 0;
		}
	}

	g_error (err->str);
	g_string_free (err, TRUE);
}

#endif /* defined(ENABLE_CHECKED_BUILD_THREAD) */

#ifdef ENABLE_CHECKED_BUILD_GC

void
assert_gc_safe_mode (const char *file, int lineno)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return;

	MonoThreadInfo *cur = mono_thread_info_current ();

	if (!cur)
		mono_fatal_with_history ("%s:%d: Expected GC Safe mode but thread is not attached", file, lineno);

	int state = mono_thread_info_current_state (cur);
	switch (state) {
	case STATE_BLOCKING:
	case STATE_BLOCKING_SELF_SUSPENDED:
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		break;
	default:
		mono_fatal_with_history ("%s:%d: Expected GC Safe mode but was in %s state", file, lineno, mono_thread_state_name (state));
	}
}

void
assert_gc_unsafe_mode (const char *file, int lineno)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return;

	MonoThreadInfo *cur = mono_thread_info_current ();

	if (!cur)
		mono_fatal_with_history ("%s:%d: Expected GC Unsafe mode but thread is not attached", file, lineno);

	int state = mono_thread_info_current_state (cur);
	switch (state) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
		break;
	default:
		mono_fatal_with_history ("%s:%d: Expected GC Unsafe mode but was in %s state", file, lineno, mono_thread_state_name (state));
	}
}

void
assert_gc_neutral_mode (const char *file, int lineno)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return;

	MonoThreadInfo *cur = mono_thread_info_current ();

	if (!cur)
		mono_fatal_with_history ("%s:%d: Expected GC Neutral mode but thread is not attached", file, lineno);

	int state = mono_thread_info_current_state (cur);
	switch (state) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_BLOCKING:
	case STATE_BLOCKING_SELF_SUSPENDED:
	case STATE_BLOCKING_SUSPEND_REQUESTED:
		break;
	default:
		mono_fatal_with_history ("%s:%d: Expected GC Neutral mode but was in %s state", file, lineno, mono_thread_state_name (state));
	}
}

void *
critical_gc_region_begin(void)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return NULL;

	CheckState *state = get_state ();
	state->in_gc_critical_region++;
	return state;
}


void
critical_gc_region_end(void* token)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return;

	CheckState *state = get_state();
	g_assert (state == token);
	state->in_gc_critical_region--;
}

void
assert_not_in_gc_critical_region(void)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return;

	CheckState *state = get_state();
	if (state->in_gc_critical_region > 0) {
		mono_fatal_with_history("Expected GC Unsafe mode, but was in %s state", mono_thread_state_name (mono_thread_info_current_state (mono_thread_info_current ())));
	}
}

void
assert_in_gc_critical_region (void)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_GC))
		return;

	CheckState *state = get_state();
	if (state->in_gc_critical_region == 0)
		mono_fatal_with_history("Expected GC critical region");
}

#endif /* defined(ENABLE_CHECKED_BUILD_GC) */

#ifdef ENABLE_CHECKED_BUILD_METADATA

// check_metadata_store et al: The goal of these functions is to verify that if there is a pointer from one mempool into
// another, that the pointed-to memory is protected by the reference count mechanism for MonoImages.
//
// Note: The code below catches only some kinds of failures. Failures outside its scope notably incode:
// * Code below absolutely assumes that no mempool is ever held as "mempool" member by more than one Image or ImageSet at once
// * Code below assumes reference counts never underflow (ie: if we have a pointer to something, it won't be deallocated while we're looking at it)
// Locking strategy is a little slapdash overall.

// Reference audit support
#define check_mempool_assert_message(...) \
	g_assertion_message("Mempool reference violation: " __VA_ARGS__)

typedef struct
{
	MonoImage *image;
	MonoImageSet *image_set;
} MonoMemPoolOwner;

static MonoMemPoolOwner mono_mempool_no_owner = {NULL,NULL};

static gboolean
check_mempool_owner_eq (MonoMemPoolOwner a, MonoMemPoolOwner b)
{
	return a.image == b.image && a.image_set == b.image_set;
}

// Say image X "references" image Y if X either contains Y in its modules field, or X’s "references" field contains an
// assembly whose image is Y.
// Say image X transitively references image Y if there is any chain of images-referencing-images which leads from X to Y.
// Once the mempools for two pointers have been looked up, there are four possibilities:

// Case 1. Image FROM points to Image TO: Legal if FROM transitively references TO

// We'll do a simple BFS graph search on images. For each image we visit:
static void
check_image_search (GHashTable *visited, GPtrArray *next, MonoImage *candidate, MonoImage *goal, gboolean *success)
{
	// Image hasn't even been loaded-- ignore it
	if (!candidate)
		return;

	// Image has already been visited-- ignore it
	if (g_hash_table_lookup_extended (visited, candidate, NULL, NULL))
		return;

	// Image is the target-- mark success
	if (candidate == goal)
	{
		*success = TRUE;
		return;
	}

	// Unvisited image, queue it to have its children visited
	g_hash_table_insert (visited, candidate, NULL);
	g_ptr_array_add (next, candidate);
	return;
}

static gboolean
check_image_may_reference_image(MonoImage *from, MonoImage *to)
{
	if (to == from) // Shortcut
		return TRUE;

	// Corlib is never unloaded, and all images implicitly reference it.
	// Some images avoid explicitly referencing it as an optimization, so special-case it here.
	if (to == mono_defaults.corlib)
		return TRUE;

	// Non-dynamic images may NEVER reference dynamic images
	if (to->dynamic && !from->dynamic)
		return FALSE;

	// FIXME: We currently give a dynamic images a pass on the reference rules.
	// Dynamic images may ALWAYS reference non-dynamic images.
	// We allow this because the dynamic image code is known "messy", and in theory it is already
	// protected because dynamic images can only reference classes their assembly has retained.
	// However, long term, we should make this rigorous.
	if (from->dynamic && !to->dynamic)
		return TRUE;

	gboolean success = FALSE;

	// Images to inspect on this pass, images to inspect on the next pass
	GPtrArray *current = g_ptr_array_sized_new (1), *next = g_ptr_array_new ();

	// Because in practice the image graph contains cycles, we must track which images we've visited
	GHashTable *visited = g_hash_table_new (NULL, NULL);

	#define CHECK_IMAGE_VISIT(i) check_image_search (visited, next, (i), to, &success)

	CHECK_IMAGE_VISIT (from); // Initially "next" contains only from node

	// For each pass exhaust the "to check" queue while filling up the "check next" queue
	while (!success && next->len > 0) // Halt on success or when out of nodes to process
	{
		// Swap "current" and "next" and clear next
		GPtrArray *temp = current;
		current = next;
		next = temp;
		g_ptr_array_set_size (next, 0);

		int current_idx;
		for(current_idx = 0; current_idx < current->len; current_idx++)
		{
			MonoImage *checking = (MonoImage*)g_ptr_array_index (current, current_idx);

			mono_image_lock (checking);

			// For each queued image visit all directly referenced images
			int inner_idx;

			// 'files' and 'modules' semantically contain the same items but because of lazy loading we must check both
			for (inner_idx = 0; !success && inner_idx < checking->file_count; inner_idx++)
			{
				CHECK_IMAGE_VISIT (checking->files[inner_idx]);
			}

			for (inner_idx = 0; !success && inner_idx < checking->module_count; inner_idx++)
			{
				CHECK_IMAGE_VISIT (checking->modules[inner_idx]);
			}

			for (inner_idx = 0; !success && inner_idx < checking->nreferences; inner_idx++)
			{
				// Assembly references are lazy-loaded and thus allowed to be NULL.
				// If they are NULL, we don't care about them for this search, because their images haven't impacted ref_count yet.
				if (checking->references[inner_idx])
				{
					CHECK_IMAGE_VISIT (checking->references[inner_idx]->image);
				}
			}

			mono_image_unlock (checking);
		}
	}

	g_ptr_array_free (current, TRUE); g_ptr_array_free (next, TRUE); g_hash_table_destroy (visited);

	return success;
}

// Case 2. ImageSet FROM points to Image TO: One of FROM's "images" either is, or transitively references, TO.
static gboolean
check_image_set_may_reference_image (MonoImageSet *from, MonoImage *to)
{
	// See above-- All images implicitly reference corlib
	if (to == mono_defaults.corlib)
		return TRUE;

	int idx;
	gboolean success = FALSE;
	mono_image_set_lock (from);
	for (idx = 0; !success && idx < from->nimages; idx++)
	{
		if (check_image_may_reference_image (from->images[idx], to))
			success = TRUE;
	}
	mono_image_set_unlock (from);

	return success; // No satisfying image found in from->images
}

// Case 3. ImageSet FROM points to ImageSet TO: The images in TO are a strict subset of FROM (no transitive relationship is important here)
static gboolean
check_image_set_may_reference_image_set (MonoImageSet *from, MonoImageSet *to)
{
	if (to == from)
		return TRUE;

	gboolean valid = TRUE; // Until proven otherwise

	mono_image_set_lock (from); mono_image_set_lock (to);

	int to_idx, from_idx;
	for (to_idx = 0; valid && to_idx < to->nimages; to_idx++)
	{
		gboolean seen = FALSE;

		// If TO set includes corlib, the FROM set may
		// implicitly reference corlib, even if it's not
		// present in the set explicitly.
		if (to->images[to_idx] == mono_defaults.corlib)
			seen = TRUE;

		// For each item in to->images, scan over from->images seeking a path to it.
		for (from_idx = 0; !seen && from_idx < from->nimages; from_idx++)
		{
			if (check_image_may_reference_image (from->images[from_idx], to->images[to_idx]))
				seen = TRUE;
		}

		// If the to->images item is not found in from->images, the subset check has failed
		if (!seen)
			valid = FALSE;
	}

	mono_image_set_unlock (from); mono_image_set_unlock (to);

	return valid; // All items in "to" were found in "from"
}

// Case 4. Image FROM points to ImageSet TO: FROM transitively references *ALL* of the “images” listed in TO
static gboolean
check_image_may_reference_image_set (MonoImage *from, MonoImageSet *to)
{
	if (to->nimages == 0) // Malformed image_set
		return FALSE;

	gboolean valid = TRUE;

	mono_image_set_lock (to);
	int idx;
	for (idx = 0; valid && idx < to->nimages; idx++)
	{
		if (!check_image_may_reference_image (from, to->images[idx]))
			valid = FALSE;
	}
	mono_image_set_unlock (to);

	return valid; // All images in to->images checked out
}

// Small helper-- get a descriptive string for a MonoMemPoolOwner
// Callers are obligated to free buffer with g_free after use
static const char *
check_mempool_owner_name (MonoMemPoolOwner owner)
{
	GString *result = g_string_new (NULL);
	if (owner.image)
	{
		if (owner.image->dynamic)
			g_string_append (result, "(Dynamic)");
		g_string_append (result, owner.image->name);
	}
	else if (owner.image_set)
	{
		char *temp = mono_image_set_description (owner.image_set);
		g_string_append (result, "(Image set)");
		g_string_append (result, temp);
		g_free (temp);
	}
	else
	{
		g_string_append (result, "(Non-image memory)");
	}
	return g_string_free (result, FALSE);
}

// Helper -- surf various image-locating functions looking for the owner of this pointer
static MonoMemPoolOwner
mono_find_mempool_owner (void *ptr)
{
	MonoMemPoolOwner owner = mono_mempool_no_owner;

	owner.image = mono_find_image_owner (ptr);
	if (!check_mempool_owner_eq (owner, mono_mempool_no_owner))
		return owner;

	owner.image_set = mono_find_image_set_owner (ptr);
	if (!check_mempool_owner_eq (owner, mono_mempool_no_owner))
		return owner;

	owner.image = mono_find_dynamic_image_owner (ptr);

	return owner;
}

// Actually perform reference audit
static void
check_mempool_may_reference_mempool (void *from_ptr, void *to_ptr, gboolean require_local)
{
	if (!mono_check_mode_enabled (MONO_CHECK_MODE_METADATA))
		return;

	// Null pointers are OK
	if (!to_ptr)
		return;

	MonoMemPoolOwner from = mono_find_mempool_owner (from_ptr), to = mono_find_mempool_owner (to_ptr);

	if (require_local)
	{
		if (!check_mempool_owner_eq (from,to))
			check_mempool_assert_message ("Pointer in image %s should have been internal, but instead pointed to image %s", check_mempool_owner_name (from), check_mempool_owner_name (to));
	}

	// Writing into unknown mempool
	else if (check_mempool_owner_eq (from, mono_mempool_no_owner))
	{
		check_mempool_assert_message ("Non-image memory attempting to write pointer to image %s", check_mempool_owner_name (to));
	}

	// Reading from unknown mempool
	else if (check_mempool_owner_eq (to, mono_mempool_no_owner))
	{
		check_mempool_assert_message ("Attempting to write pointer from image %s to non-image memory", check_mempool_owner_name (from));
	}

	// Split out the four cases described above:
	else if (from.image && to.image)
	{
		if (!check_image_may_reference_image (from.image, to.image))
			check_mempool_assert_message ("Image %s tried to point to image %s, but does not retain a reference", check_mempool_owner_name (from), check_mempool_owner_name (to));
	}

	else if (from.image && to.image_set)
	{
		if (!check_image_may_reference_image_set (from.image, to.image_set))
			check_mempool_assert_message ("Image %s tried to point to image set %s, but does not retain a reference", check_mempool_owner_name (from), check_mempool_owner_name (to));
	}

	else if (from.image_set && to.image_set)
	{
		if (!check_image_set_may_reference_image_set (from.image_set, to.image_set))
			check_mempool_assert_message ("Image set %s tried to point to image set %s, but does not retain a reference", check_mempool_owner_name (from), check_mempool_owner_name (to));
	}

	else if (from.image_set && to.image)
	{
		if (!check_image_set_may_reference_image (from.image_set, to.image))
			check_mempool_assert_message ("Image set %s tried to point to image %s, but does not retain a reference", check_mempool_owner_name (from), check_mempool_owner_name (to));
	}

	else
	{
		check_mempool_assert_message ("Internal logic error: Unreachable code");
	}
}

void
check_metadata_store (void *from, void *to)
{
    check_mempool_may_reference_mempool (from, to, FALSE);
}

void
check_metadata_store_local (void *from, void *to)
{
    check_mempool_may_reference_mempool (from, to, TRUE);
}

#endif /* defined(ENABLE_CHECKED_BUILD_METADATA) */
#else /* ENABLE_CHECKED_BUILD */

MONO_EMPTY_SOURCE_FILE (checked_build);
#endif /* ENABLE_CHECKED_BUILD */
