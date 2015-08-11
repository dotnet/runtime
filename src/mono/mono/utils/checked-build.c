/*
 * checked-build.c: Expensive asserts used when mono is built with --with-checked-build=yes
 *
 * Author:
 *	Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2015 Xamarin
 */
#include <config.h>
#ifdef CHECKED_BUILD

#include <mono/utils/checked-build.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-tls.h>
#include <glib.h>

#define MAX_NATIVE_BT 6
#define MAX_NATIVE_BT_PROBE (MAX_NATIVE_BT + 5)
#define MAX_TRANSITIONS 3


#ifdef HAVE_BACKTRACE_SYMBOLS
#include <execinfo.h>

//XXX We should collect just the IPs and lazily symbolificate them.
static int
collect_backtrace (gpointer out_data[])
{
	return backtrace (out_data, MAX_NATIVE_BT_PROBE);
}

static char*
translate_backtrace (gpointer native_trace[], int size)
{
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

	free (names);
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


typedef struct {
	GPtrArray *transitions;
} CheckState;

typedef struct {
	const char *name;
	int from_state, next_state, suspend_count, suspend_count_delta, size;
	gpointer backtrace [MAX_NATIVE_BT_PROBE];
} ThreadTransition;

static MonoNativeTlsKey thread_status;

void
checked_build_init (void)
{
	mono_native_tls_alloc (&thread_status, NULL);
}

static CheckState*
get_state (void)
{
	CheckState *state = mono_native_tls_get_value (thread_status);
	if (!state) {
		state = g_new0 (CheckState, 1);
		state->transitions = g_ptr_array_new ();
		mono_native_tls_set_value (thread_status, state);
	}

	return state;
}

static void
free_transition (ThreadTransition *t)
{
	g_free (t);
}

void
checked_build_thread_transition (const char *transition, void *info, int from_state, int suspend_count, int next_state, int suspend_count_delta)
{
	MonoThreadInfo *cur = mono_thread_info_current_unchecked ();
	CheckState *state = get_state ();
	/* We currently don't record external changes as those are hard to reason about. */
	if (cur != info)
		return;

	if (state->transitions->len >= MAX_TRANSITIONS)
		free_transition (g_ptr_array_remove_index (state->transitions, 0));

	ThreadTransition *t = g_new0 (ThreadTransition, 1);
	t->name = transition;
	t->from_state = from_state;
	t->next_state = next_state;
	t->suspend_count = suspend_count;
	t->suspend_count_delta = suspend_count_delta;
	t->size = collect_backtrace (t->backtrace);
	g_ptr_array_add (state->transitions, t);
}

static void
assertion_fail (const char *msg, ...)
{
	int i;
	GString* err = g_string_sized_new (100);
	CheckState *state = get_state ();

	g_string_append_printf (err, "Assertion failure in thread %p due to: ", mono_native_thread_id_get ());

	va_list args;
	va_start (args, msg);
	g_string_append_vprintf (err, msg, args);
	va_end (args);

	g_string_append_printf (err, "\nLast %d state transitions: (most recent first)\n", state->transitions->len);

	for (i = state->transitions->len - 1; i >= 0; --i) {
		ThreadTransition *t = state->transitions->pdata [i];
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
	}

	g_error (err->str);
	g_string_free (err, TRUE);
}

void
assert_gc_safe_mode (void)
{
	MonoThreadInfo *cur = mono_thread_info_current ();
	int state;

	if (!cur)
		assertion_fail ("Expected GC Safe mode but thread is not attached");

	switch (state = mono_thread_info_current_state (cur)) {
	case STATE_BLOCKING:
	case STATE_BLOCKING_AND_SUSPENDED:
		break;
	default:
		assertion_fail ("Expected GC Safe mode but was in %s state", mono_thread_state_name (state));
	}
}

void
assert_gc_unsafe_mode (void)
{
	MonoThreadInfo *cur = mono_thread_info_current ();
	int state;

	if (!cur)
		assertion_fail ("Expected GC Unsafe mode but thread is not attached");

	switch (state = mono_thread_info_current_state (cur)) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_SELF_SUSPEND_REQUESTED:
		break;
	default:
		assertion_fail ("Expected GC Unsafe mode but was in %s state", mono_thread_state_name (state));
	}
}

void
assert_gc_neutral_mode (void)
{
	MonoThreadInfo *cur = mono_thread_info_current ();
	int state;

	if (!cur)
		assertion_fail ("Expected GC Neutral mode but thread is not attached");

	switch (state = mono_thread_info_current_state (cur)) {
	case STATE_RUNNING:
	case STATE_ASYNC_SUSPEND_REQUESTED:
	case STATE_SELF_SUSPEND_REQUESTED:
	case STATE_BLOCKING:
	case STATE_BLOCKING_AND_SUSPENDED:
		break;
	default:
		assertion_fail ("Expected GC Neutral mode but was in %s state", mono_thread_state_name (state));
	}
}

#endif /* CHECKED_BUILD */
