#include <glib.h>
#include <pthread.h>

#include <mono/io-layer/mono-mutex.h>
#include <mono/metadata/class.h>
#include <mono/metadata/profiler.h>

#include "gchandle-profiler.h"

void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *prof;

	g_print ("*** Running with the GCHandle profiler ***\n");

	// The profiler uses gchandle alloc info and jit info
	prof = gchandle_profiler_new ();
	mono_profiler_install (prof, gchandle_profiler_shutdown);
	mono_profiler_install_gc_roots (gchandle_profiler_track_gchandle, NULL);
	mono_profiler_install_jit_end (gchandle_profiler_method_jitted);
	mono_profiler_set_events (MONO_PROFILE_GC_ROOTS | MONO_PROFILE_JIT_COMPILATION);
}

void gchandle_profiler_shutdown (MonoProfiler *prof)
{
	g_print ("Shutting down the profiler\n");
	gchandle_profiler_dump_jitted_methods (prof);
	gchandle_profiler_dump_gchandles (prof);
}

MonoProfiler *gchandle_profiler_new ()
{
	MonoProfiler *prof;

	prof = g_new0 (MonoProfiler, 1);
	prof->type_name = g_getenv ("GCHANDLES_FOR_TYPE");
	prof->gchandles = g_ptr_array_new ();
	prof->jitted_methods = g_hash_table_new (g_str_hash, g_str_equal);
	prof->stacktraces = g_ptr_array_new ();
	mono_mutex_init (&prof->mutex, NULL);

	if (prof->type_name)
		g_print ("*** Recording GCHandle allocation stacktraces for type '%s'\n",  prof->type_name);

	return prof;
}

void
gchandle_profiler_dump_jitted_methods (MonoProfiler *prof)
{
	g_hash_table_foreach (prof->jitted_methods, dump_jitted_methods, NULL);
}

void
gchandle_profiler_method_jitted (MonoProfiler *prof, MonoMethod *method, MonoJitInfo* jinfo, int result)
{
	// Whenever a method is jitted, store the method name and increment the count by 1.
	// Methods can be jitted multiple times if instance delegates are passed to native code
	int count;
	char *name;

	mono_mutex_lock (&prof->mutex);

	name = mono_method_full_name (method, 1);
	count = GPOINTER_TO_INT (g_hash_table_lookup (prof->jitted_methods, name));
	g_hash_table_insert (prof->jitted_methods, name, GINT_TO_POINTER (count + 1));

	mono_mutex_unlock (&prof->mutex);
}

void gchandle_profiler_track_gchandle (MonoProfiler *prof, int op, int type, uintptr_t handle, MonoObject *obj)
{
	int i;
	GPtrArray *gchandles;
	GPtrArray *stacktraces;
	
	// Ignore anything that isn't a strong GC handle
	if (type != 2)
		return;

	gchandles = prof->gchandles;
	stacktraces = prof->stacktraces;

	mono_mutex_lock (&prof->mutex);

	// Keep the two arrays in sync so that the gchandle at index i stores its stacktrace at index i in the
	// other gptrarray. 
	if (op == MONO_PROFILER_GC_HANDLE_CREATED) {
		g_ptr_array_add (gchandles, (gpointer) handle);
		if (prof->type_name && !strcmp (prof->type_name, mono_class_get_name (mono_object_get_class(obj))))
			g_ptr_array_add (stacktraces, get_stack_trace ());
		else
			g_ptr_array_add (stacktraces, NULL);
	} else if (op == MONO_PROFILER_GC_HANDLE_DESTROYED) {
		for (i = 0; i < (int)gchandles->len; i++) {
			if (g_ptr_array_index (gchandles, i) == (gpointer) handle) {
				g_free (g_ptr_array_index (stacktraces, i));
				g_ptr_array_remove_index_fast (gchandles, i);
				g_ptr_array_remove_index_fast (stacktraces, i);
				break;
			}
		}
	}

	mono_mutex_unlock (&prof->mutex);
}

void dump_jitted_methods (gpointer key, gpointer value, gpointer user_data)
{
	// We only care about methods which are jitted multiple times.
	int jit_count = GPOINTER_TO_INT (value);
	if (jit_count > 10)
		g_print ("%d:\t%s\n", jit_count, (char*)key);
}

char *get_stack_trace ()
{
	GString *str;
	char *trace;

	str = g_string_new ("");
	mono_stack_walk_no_il (stack_walk_fn, str);
	trace = str->str;

	g_string_free (str, FALSE);
	return trace;
}


gboolean stack_walk_fn (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data)
{
	GString *str;
	MonoClass *klass;

	if (managed) {
		str = (GString *) data;
		klass = mono_method_get_class (method);

		g_string_append_c (str, '\t');
		g_string_append (str, mono_class_get_namespace (klass));
		g_string_append_c (str, '.');
		g_string_append (str, mono_class_get_name (klass));
		g_string_append_c (str, '.');
		g_string_append (str, mono_method_get_name (method));
		g_string_append_c (str, '\n');
	}
    return FALSE;
}


void accumulate_gchandles_by_type (gpointer data, gpointer user_data)
{
	// For every GCHandle we get the class name and store it in a hashtable
	// along with the number of times we've seen that class name. This tells
	// us how many GCHandles we have allocated for each class type.
	int count;
	GHashTable *by_type;
	const char *name;
	
	by_type = (GHashTable*) user_data;
	name = class_name_from_gchandle (GPOINTER_TO_INT (data));

	if (name) {
		count = GPOINTER_TO_INT (g_hash_table_lookup (by_type, name)) + 1;
		g_hash_table_insert (by_type, (void*) name, GINT_TO_POINTER (count));
	}
}

const char *class_name_from_gchandle (gint32 gchandle)
{
	MonoObject *ob;
	const char *name;
	
	ob = mono_gchandle_get_target (gchandle);
	if (!ob)
		return NULL;

	// Add in specific support for Gtk.ToggleRef in gtk-sharp so that
	// the profiler can detect what objects the ToggleRef is keeping alive
	name = mono_class_get_name (mono_object_get_class(ob));
	if (name && !strcmp (name, "ToggleRef")) {
		MonoClassField *field = mono_class_get_field_from_name (mono_object_get_class(ob), "reference");
		if (field) {
			mono_field_get_value (ob, field, &ob);
			if (ob)
				name = mono_class_get_name (mono_object_get_class(ob));
		}
	}

	return name;
}

void gchandle_profiler_dump_gchandles (MonoProfiler *prof)
{
	int i;
	GHashTable *by_type;
	GPtrArray *top_n_by_type;

	by_type = g_hash_table_new (g_str_hash, g_str_equal);
	top_n_by_type = g_ptr_array_new ();
	
	// Generate a sorted/filtered list of results so that we can print the
	// number of gchandles allocated for each type in ascending order so types
	// with a lot of GChandles are printed last. 
	g_ptr_array_foreach (prof->gchandles, accumulate_gchandles_by_type, by_type);
	g_hash_table_foreach (by_type, add_hashtable_keys_to_ptr_array, top_n_by_type);
	g_ptr_array_sort_with_data (top_n_by_type, gchandle_instances_comparer, by_type);

	for (i = 0; i < (int) top_n_by_type->len; i++)
		g_print ("\t%d GCHandles referencing type '%s'\n", GPOINTER_TO_INT (g_hash_table_lookup (by_type, top_n_by_type->pdata [i])), (char *) top_n_by_type->pdata [i]);
	g_print ("\n");

	gchandle_profiler_dump_gchandle_traces (prof);
}

void gchandle_profiler_dump_gchandle_traces (MonoProfiler *prof)
{
	int i, j;
	gint32 gchandle;
	GPtrArray *gchandles;
	MonoObject *ob;
	const char *name;
	GPtrArray *stacktraces;

	if (!prof->type_name)
		return;

	gchandles = prof->gchandles;
	stacktraces = prof->stacktraces;

	// For all allocated handles, see if any of them are referencing objects of the type
	// we care about. If they are, print out the allocation trace of all handles targetting
	// that object. Note that multiple handles targetting the same object are grouped together
	for (i = 0; i < (int) gchandles->len; i ++) {
		gchandle = GPOINTER_TO_INT (g_ptr_array_index (gchandles, i));
		ob = mono_gchandle_get_target (gchandle);
		name = class_name_from_gchandle (gchandle);
		if (name && !strcmp (name, prof->type_name)) {
			g_print ("Strong GCHandles allocated for object %p:\n", ob);
			for (j = i; j < (int) gchandles->len; j++) {
				if (mono_gchandle_get_target (GPOINTER_TO_INT (g_ptr_array_index (gchandles, j))) == ob) {
					g_print ("%s\n", (char *) g_ptr_array_index (stacktraces, j));
					g_ptr_array_remove_index_fast (gchandles, j);
					g_ptr_array_remove_index_fast (stacktraces, j);
					j --;
				}
			}
			g_print ("\n");
			i --;
		}
	}
}

void add_hashtable_keys_to_ptr_array (gpointer key, gpointer value, gpointer user_data)
{
	GPtrArray *by_type = (GPtrArray*) user_data;
	g_ptr_array_add (by_type, key);
}

gint gchandle_instances_comparer (gconstpointer base1, gconstpointer base2, gpointer user_data)
{
	GHashTable *by_type = (GHashTable *) user_data;
	char *left = *((char **) base1);
	char *right = *((char **) base2);

	int iddiff =  GPOINTER_TO_INT (g_hash_table_lookup (by_type, left)) - GPOINTER_TO_INT (g_hash_table_lookup (by_type, right));

	if (iddiff == 0)
		return 0;
	else if (iddiff < 0)
		return -1;
	else
		return 1;
}

