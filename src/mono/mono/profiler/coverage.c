/*
 * coverage.c: mono coverage profiler
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Alex Rønne Petersen (alexrp@xamarin.com)
 *   Ludovic Henry (ludovic@xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

/*
 * The Coverage XML output schema
 * <coverage>
 *   <assembly/>
 *   <class/>
 *   <method>
 *     <statement/>
 *   </method>
 * </coverage>
 *
 * Elements:
 *   <coverage> - The root element of the documentation. It can contain any number of
 *                <assembly>, <class> or <method> elements.
 *                Attributes:
 *                   - version: The version number for the file format - (eg: "0.3")
 *   <assembly> - Contains data about assemblies. Has no child elements
 *                Attributes:
 *                   - name: The name of the assembly - (eg: "System.Xml")
 *                   - guid: The GUID of the assembly
 *                   - filename: The filename of the assembly
 *                   - method-count: The number of methods in the assembly
 *                   - full: The number of fully covered methods
 *                   - partial: The number of partially covered methods
 *   <class> - Contains data about classes. Has no child elements
 *             Attributes:
 *                - name: The name of the class
 *                - method-count: The number of methods in the class
 *                - full: The number of fully covered methods
 *                - partial: The number of partially covered methods
 *   <method> - Contains data about methods. Can contain any number of <statement> elements
 *              Attributes:
 *                 - assembly: The name of the parent assembly
 *                 - class: The name of the parent class
 *                 - name: The name of the method, with all it's parameters
 *                 - filename: The name of the source file containing this method
 *                 - token
 *   <statement> - Contains data about IL statements. Has no child elements
 *                 Attributes:
 *                    - offset: The offset of the statement in the IL code
 *                    - counter: 1 if the line was covered, 0 if it was not
 *                    - line: The line number in the parent method's file
 *                    - column: The column on the line
 */

#include <config.h>
#include <glib.h>

#include <stdio.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>

#include <mono/mini/jit.h>

#include <mono/utils/atomic.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/mono-conc-hashtable.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-publib.h>

#define VERSION_MAJOR 0
#define VERSION_MINOR 3

// Statistics for profiler events.
static gint32 coverage_methods_ctr,
              coverage_statements_ctr,
              coverage_classes_ctr,
              coverage_assemblies_ctr;

typedef struct ProfilerConfig ProfilerConfig;

struct _MonoProfiler {
	MonoProfilerHandle handle;

	FILE* file;

	char *args;

	volatile gint32 runtime_inited;
	volatile gint32 in_shutdown;

	guint32 previous_offset;
	GPtrArray *data;

	mono_mutex_t mutex;

	GPtrArray *filters;
	MonoConcurrentHashTable *filtered_classes;
	MonoConcurrentHashTable *suppressed_assemblies;

	MonoConcurrentHashTable *methods;
	MonoConcurrentHashTable *assemblies;
	GHashTable *deferred_assemblies;
	GHashTable *images;

	MonoConcurrentHashTable *class_to_methods;
	MonoConcurrentHashTable *image_to_methods;

	GHashTable *uncovered_methods;
	gboolean done, dumped;

	ProfilerConfig *config;
	GString *s;
};

struct ProfilerConfig {
	//Where to compress the output file
	gboolean use_zip;

	//Name of the generated xml file
	const char *output_filename;

	//Filter files used by the code coverage mode
	GPtrArray *cov_filter_files;

	MonoMethodDesc *write_at;
	MonoMethodDesc *send_to;
	char *send_to_arg;
	char *send_to_str;
};

static ProfilerConfig coverage_config;
static MonoProfiler coverage_profiler;

/* This is a very basic escape function that escapes < > and &
   Ideally we'd use g_markup_escape_string but that function isn't
	 available in Mono's eglib. This was written without looking at the
	 source of that function in glib. */
static char *
escape_string_for_xml (const char *string)
{
	GString *string_builder = g_string_new (NULL);
	const char *start, *p;

	start = p = string;
	while (*p) {
		while (*p && *p != '&' && *p != '<' && *p != '>')
			p++;

		g_string_append_len (string_builder, start, p - start);

		if (*p == '\0')
			break;

		switch (*p) {
		case '<':
			g_string_append (string_builder, "&lt;");
			break;

		case '>':
			g_string_append (string_builder, "&gt;");
			break;

		case '&':
			g_string_append (string_builder, "&amp;");
			break;

		default:
			break;
		}

		p++;
		start = p;
	}

	return g_string_free (string_builder, FALSE);
}

typedef struct {
	MonoLockFreeQueueNode node;
	MonoMethod *method;
} MethodNode;

typedef struct {
	int offset;
	int counter;
	char *filename;
	int line;
	int column;
} CoverageEntry;

static void
free_coverage_entry (gpointer data, gpointer userdata)
{
	CoverageEntry *entry = (CoverageEntry *)data;
	g_free (entry->filename);
	g_free (entry);
}

static void
obtain_coverage_for_method (MonoProfiler *prof, const MonoProfilerCoverageData *entry)
{
	CoverageEntry *e = g_new (CoverageEntry, 1);

	coverage_profiler.previous_offset = entry->il_offset;

	e->offset = entry->il_offset;
	e->counter = entry->counter;
	e->filename = g_strdup(entry->file_name ? entry->file_name : "");
	e->line = entry->line;
	e->column = entry->column;

	g_ptr_array_add (coverage_profiler.data, e);
}

static char *
parse_generic_type_names(char *name)
{
	char *new_name, *ret;
	int within_generic_declaration = 0, generic_members = 1;

	if (name == NULL || *name == '\0')
		return g_strdup ("");

	if (!(ret = new_name = (char *) g_calloc (strlen (name) * 4 + 1, sizeof (char))))
		return NULL;

	do {
		switch (*name) {
			case '<':
				within_generic_declaration ++;
				break;

			case '>':
				within_generic_declaration --;

				if (within_generic_declaration)
					break;

				if (*(name - 1) != '<') {
					*new_name++ = '`';
					*new_name++ = '0' + generic_members;
				} else {
					memcpy (new_name, "<>", 2);
					new_name += 2;
				}

				generic_members = 0;
				break;

			case ',':
				generic_members++;
				break;

			default:
				if (!within_generic_declaration)
					*new_name++ = *name;

				break;
		}
	} while (*name++);

	return ret;
}

static void
dump_method (gpointer key, gpointer value, gpointer userdata)
{
	MonoMethod *method = (MonoMethod *)value;
	MonoClass *klass;
	MonoImage *image;
	char *class_name, *escaped_image_name, *escaped_class_name, *escaped_method_name, *escaped_method_signature, *escaped_method_filename;
	const char *image_name, *method_name, *method_signature, *method_filename;
	guint i;
	GString *s = coverage_profiler.s;

	coverage_profiler.previous_offset = 0;
	coverage_profiler.data = g_ptr_array_new ();

	mono_profiler_get_coverage_data (coverage_profiler.handle, method, obtain_coverage_for_method);

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);
	image_name = mono_image_get_name (image);

	method_signature = mono_signature_get_desc (mono_method_signature_internal (method), TRUE);
	class_name = parse_generic_type_names (mono_type_get_name (m_class_get_byval_arg (klass)));
	method_name = mono_method_get_name (method);

	if (coverage_profiler.data->len != 0) {
		CoverageEntry *entry = (CoverageEntry *)coverage_profiler.data->pdata[0];
		method_filename = entry->filename ? entry->filename : "";
	} else
		method_filename = "";

	image_name = image_name ? image_name : "";
	method_signature = method_signature ? method_signature : "";
	method_name = method_name ? method_name : "";

	escaped_image_name = escape_string_for_xml (image_name);
	escaped_class_name = escape_string_for_xml (class_name);
	escaped_method_name = escape_string_for_xml (method_name);
	escaped_method_signature = escape_string_for_xml (method_signature);
	escaped_method_filename = escape_string_for_xml (method_filename);

	g_string_append_printf (s, "\t<method assembly=\"%s\" class=\"%s\" name=\"%s (%s)\" filename=\"%s\" token=\"%d\">\n",
		escaped_image_name, escaped_class_name, escaped_method_name, escaped_method_signature, escaped_method_filename, mono_method_get_token (method));

	g_free (escaped_image_name);
	g_free (escaped_class_name);
	g_free (escaped_method_name);
	g_free (escaped_method_signature);
	g_free (escaped_method_filename);

	for (i = 0; i < coverage_profiler.data->len; i++) {
		CoverageEntry *entry = (CoverageEntry *)coverage_profiler.data->pdata[i];

		g_string_append_printf (s, "\t\t<statement offset=\"%d\" counter=\"%d\" line=\"%d\" column=\"%d\"/>\n",
			entry->offset, entry->counter, entry->line, entry->column);
	}

	g_string_append_printf (s, "\t</method>\n");

	g_free (class_name);

	g_ptr_array_foreach (coverage_profiler.data, free_coverage_entry, NULL);
	g_ptr_array_free (coverage_profiler.data, TRUE);
}

/* This empties the queue */
static guint
count_queue (MonoLockFreeQueue *queue)
{
	MonoLockFreeQueueNode *node;
	guint count = 0;

	while ((node = mono_lock_free_queue_dequeue (queue))) {
		count++;
		mono_thread_hazardous_try_free (node, g_free);
	}

	return count;
}

static void
dump_classes_for_image (gpointer key, gpointer value, gpointer userdata)
{
	MonoClass *klass = (MonoClass *)key;
	MonoLockFreeQueue *class_methods = (MonoLockFreeQueue *)value;
	MonoImage *image;
	char *class_name, *escaped_class_name;
	const char *image_name;
	int number_of_methods, partially_covered;
	guint fully_covered;
	GString *s = coverage_profiler.s;

	image = mono_class_get_image (klass);
	image_name = mono_image_get_name (image);

	if (!image_name || strcmp (image_name, mono_image_get_name (((MonoImage*) userdata))) != 0)
		return;

	class_name = mono_type_get_name (m_class_get_byval_arg (klass));

	number_of_methods = mono_class_num_methods (klass);

	GHashTable *covered_methods = g_hash_table_new (NULL, NULL);
	int count = 0;
	{
		MonoLockFreeQueueNode *node;
		guint count = 0;

		while ((node = mono_lock_free_queue_dequeue (class_methods))) {
			MethodNode *mnode = (MethodNode*)node;
			g_hash_table_insert (covered_methods, mnode->method, mnode->method);
			count++;
			mono_thread_hazardous_try_free (node, g_free);
		}
	}
	fully_covered = count;

	gpointer iter = NULL;
	MonoMethod *method;
	while ((method = mono_class_get_methods (klass, &iter))) {
		if (!g_hash_table_lookup (covered_methods, method))
			g_hash_table_insert (coverage_profiler.uncovered_methods, method, method);
	}
	g_hash_table_destroy (covered_methods);

	/* We don't handle partial covered yet */
	partially_covered = 0;

	escaped_class_name = escape_string_for_xml (class_name);

	g_string_append_printf (s, "\t<class name=\"%s\" method-count=\"%d\" full=\"%d\" partial=\"%d\"/>\n",
					 escaped_class_name, number_of_methods, fully_covered, partially_covered);

	g_free (escaped_class_name);

	g_free (class_name);

}

static void
get_coverage_for_image (MonoImage *image, int *number_of_methods, guint *fully_covered, int *partially_covered)
{
	MonoLockFreeQueue *image_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (coverage_profiler.image_to_methods, image);

	*number_of_methods = mono_image_get_table_rows (image, MONO_TABLE_METHOD);
	if (image_methods)
		*fully_covered = count_queue (image_methods);
	else
		*fully_covered = 0;

	// FIXME: We don't handle partially covered yet.
	*partially_covered = 0;
}

static void
dump_assembly (gpointer key, gpointer value, gpointer userdata)
{
	MonoAssembly *assembly = (MonoAssembly *)value;
	MonoImage *image = mono_assembly_get_image_internal (assembly);
	const char *image_name, *image_guid, *image_filename;
	char *escaped_image_name, *escaped_image_filename;
	int number_of_methods = 0, partially_covered = 0;
	guint fully_covered = 0;
	GString *s = coverage_profiler.s;

	image_name = mono_image_get_name (image);
	image_guid = mono_image_get_guid (image);
	image_filename = mono_image_get_filename (image);

	image_name = image_name ? image_name : "";
	image_guid = image_guid ? image_guid : "";
	image_filename = image_filename ? image_filename : "";

	get_coverage_for_image (image, &number_of_methods, &fully_covered, &partially_covered);

	escaped_image_name = escape_string_for_xml (image_name);
	escaped_image_filename = escape_string_for_xml (image_filename);

	g_string_append_printf (s, "\t<assembly name=\"%s\" guid=\"%s\" filename=\"%s\" method-count=\"%d\" full=\"%d\" partial=\"%d\"/>\n",
		escaped_image_name, image_guid, escaped_image_filename, number_of_methods, fully_covered, partially_covered);

	g_free (escaped_image_name);
	g_free (escaped_image_filename);

	mono_conc_hashtable_foreach (coverage_profiler.class_to_methods, dump_classes_for_image, image);
}

static void
dump_coverage (MonoProfiler *prof)
{
	GString *s;

	if (prof->dumped)
		return;
	prof->dumped = TRUE;

	s = g_string_new ("");
	prof->s = s;
	g_string_append_printf (s, "<?xml version=\"1.0\"?>\n");
	g_string_append_printf (s, "<coverage version=\"%d.%d\">\n", VERSION_MAJOR, VERSION_MINOR);

	mono_os_mutex_lock (&coverage_profiler.mutex);
	mono_conc_hashtable_foreach (coverage_profiler.assemblies, dump_assembly, NULL);
	mono_conc_hashtable_foreach (coverage_profiler.methods, dump_method, NULL);
	g_hash_table_foreach (coverage_profiler.uncovered_methods, dump_method, NULL);
	mono_os_mutex_unlock (&coverage_profiler.mutex);

	g_string_append_printf (s, "</coverage>\n");

	if (prof->config->send_to) {
		GHashTableIter iter;
		gpointer id;
		MonoImage *image;
		MonoMethod *send_method = NULL;
		MonoMethodSignature *sig;
		ERROR_DECL (error);

		g_hash_table_iter_init (&iter, prof->images);
		while (g_hash_table_iter_next (&iter, (void**)&image, (void**)&id)) {
			send_method = mono_method_desc_search_in_image (prof->config->send_to, image);
			if (send_method)
				break;
		}
		if (!send_method) {
			mono_profiler_printf_err ("Cannot find method in loaded assemblies: '%s'.", prof->config->send_to_str);
			exit (1);
		}

		sig = mono_method_signature_checked (send_method, error);
		mono_error_assert_ok (error);
		if (sig->param_count != 2 || sig->params [0]->type != MONO_TYPE_STRING || sig->params [1]->type != MONO_TYPE_STRING) {
			mono_profiler_printf_err ("Method '%s' should have signature void (string,string).", prof->config->send_to_str);
			exit (1);
		}

		MonoString *extra_arg = NULL;
		if (prof->config->send_to_arg) {
			extra_arg = mono_string_new_checked (prof->config->send_to_arg, error);
			mono_error_assert_ok (error);
		}

		MonoString *data = mono_string_new_checked (s->str, error);
		mono_error_assert_ok (error);

		MonoObject *exc;
		gpointer args [3];
		args [0] = data;
		args [1] = extra_arg;

		printf ("coverage-profiler | Passing data to '%s': %s\n", mono_method_full_name (send_method, 1), prof->config->send_to_arg ? prof->config->send_to_arg : "(null)");
		mono_runtime_try_invoke (send_method, NULL, args, &exc, error);
		mono_error_assert_ok (error);
		g_assert (exc == NULL);
	} else {
		fprintf (coverage_profiler.file, "%s", s->str);
	}
	g_string_free (s, TRUE);
}

static MonoLockFreeQueueNode *
create_method_node (MonoMethod *method)
{
	MethodNode *node = (MethodNode *) g_malloc (sizeof (MethodNode));
	mono_lock_free_queue_node_init ((MonoLockFreeQueueNode *) node, FALSE);
	node->method = method;

	return (MonoLockFreeQueueNode *) node;
}

static gboolean
consider_image (MonoImage *image)
{
	// Don't handle coverage for the core assemblies
	if (mono_conc_hashtable_lookup (coverage_profiler.suppressed_assemblies, (gpointer) mono_image_get_name (image)) != NULL)
		return FALSE;

	return TRUE;
}

static gboolean
consider_class (MonoImage *image, MonoClass *klass)
{
	if (coverage_profiler.filters) {
		/* Check already filtered classes first */
		if (mono_conc_hashtable_lookup (coverage_profiler.filtered_classes, klass))
			return FALSE;

		char *classname = mono_type_get_name (m_class_get_byval_arg (klass));
		char *fqn = g_strdup_printf ("[%s]%s", mono_image_get_name (image), classname);

		// Check positive filters first
		gboolean has_positive = FALSE;
		gboolean found = FALSE;

		for (guint i = 0; i < coverage_profiler.filters->len; ++i) {
			char *filter = (char *)g_ptr_array_index (coverage_profiler.filters, i);

			if (filter [0] == '+') {
				filter = &filter [1];

				if (strstr (fqn, filter) != NULL)
					found = TRUE;

				has_positive = TRUE;
			}
		}

		if (has_positive && !found) {
			mono_os_mutex_lock (&coverage_profiler.mutex);
			mono_conc_hashtable_insert (coverage_profiler.filtered_classes, klass, klass);
			mono_os_mutex_unlock (&coverage_profiler.mutex);
			g_free (fqn);
			g_free (classname);

			return FALSE;
		}

		for (guint i = 0; i < coverage_profiler.filters->len; ++i) {
			// FIXME: Is substring search sufficient?
			char *filter = (char *)g_ptr_array_index (coverage_profiler.filters, i);
			if (filter [0] == '+' || filter [0] != '-')
				continue;

			// Skip '-'
			filter = &filter [1];

			if (strstr (fqn, filter) != NULL) {
				mono_os_mutex_lock (&coverage_profiler.mutex);
				mono_conc_hashtable_insert (coverage_profiler.filtered_classes, klass, klass);
				mono_os_mutex_unlock (&coverage_profiler.mutex);
				g_free (fqn);
				g_free (classname);

				return FALSE;
			}
		}

		g_free (fqn);
		g_free (classname);
	}

	return TRUE;
}

static MonoLockFreeQueue *
register_image (MonoImage *image)
{
	// First try the fast path...
	MonoLockFreeQueue *image_methods = (MonoLockFreeQueue *) mono_conc_hashtable_lookup (coverage_profiler.image_to_methods, image);

	if (image_methods)
		return image_methods;

	mono_os_mutex_lock (&coverage_profiler.mutex);

	/*
	 * Another thread might've inserted the image since the check above, so
	 * check again now that we're holding the lock.
	 */
	image_methods = (MonoLockFreeQueue *) mono_conc_hashtable_lookup (coverage_profiler.image_to_methods, image);

	if (image_methods == NULL) {
		image_methods = (MonoLockFreeQueue *) g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (image_methods);
		mono_conc_hashtable_insert (coverage_profiler.image_to_methods, image, image_methods);

		MonoAssembly *assembly = mono_image_get_assembly (image);

		// FIXME: Locking
		g_hash_table_add (coverage_profiler.images, image);

		/*
		 * We have to keep all the assemblies we reference metadata in alive,
		 * otherwise they might be gone by the time we dump coverage data
		 * during shutdown. This can happen with e.g. the corlib test suite.
		 */
		mono_assembly_addref (assembly);

		mono_conc_hashtable_insert (coverage_profiler.assemblies, assembly, assembly);
	}

	mono_os_mutex_unlock (&coverage_profiler.mutex);

	return image_methods;
}

static MonoLockFreeQueue *
register_class (MonoClass *klass)
{
	// First try the fast path...
	MonoLockFreeQueue *class_methods = (MonoLockFreeQueue *) mono_conc_hashtable_lookup (coverage_profiler.class_to_methods, klass);

	if (class_methods)
		return class_methods;

	mono_os_mutex_lock (&coverage_profiler.mutex);

	/*
	 * Another thread might've inserted the class since the check above, so
	 * check again now that we're holding the lock.
	 */
	class_methods = (MonoLockFreeQueue *) mono_conc_hashtable_lookup (coverage_profiler.class_to_methods, klass);

	if (class_methods == NULL) {
		class_methods = (MonoLockFreeQueue *) g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (class_methods);
		mono_conc_hashtable_insert (coverage_profiler.class_to_methods, klass, class_methods);
	}

	mono_os_mutex_unlock (&coverage_profiler.mutex);

	return class_methods;
}

/*
 * Note: It's important that we do all of this work in the assembly_loaded
 * callback rather than the (more intuitive) image_loaded callback. This is
 * because assembly loading has not finished by the time image_loaded is
 * raised, so if the mono_class_get_checked () call below ends up needing to
 * resolve typeref tokens that point to other assemblies (it almost always
 * does), we end up accessing a bunch of half-initialized state, leading to
 * crashes.
 */
static void
assembly_loaded (MonoProfiler *prof, MonoAssembly *assembly)
{
	/*
	 * When dumping coverage data at shutdown, we may end up loading some extra
	 * assemblies for reflection purposes (e.g. method signatures). We don't
	 * need to consider these assemblies for coverage since no code in them is
	 * ever actually executed. This avoids a deadlock where we would try to
	 * take the coverage profiler lock recursively.
	 */
	if (mono_atomic_load_i32 (&coverage_profiler.in_shutdown))
		return;

	if (!mono_atomic_load_i32 (&coverage_profiler.runtime_inited)) {
		/*
		 * Certain assemblies (i.e. corlib) can be problematic during startup.
		 * The assembly_loaded callback is raised before mono_defaults.corlib
		 * is set, which means when we try to load classes from it, things
		 * break in weird ways because various mono_is_corlib_image () special
		 * cases throughout the runtime are suddenly false.
		 *
		 * Work around this by remembering assemblies that are loaded before
		 * runtime initialization has finished and processing them when we get
		 * the runtime_initialized callback.
		 */
		mono_assembly_addref (assembly);
		g_hash_table_insert (coverage_profiler.deferred_assemblies, assembly, assembly);
		return;
	}

	MonoImage *image = mono_assembly_get_image_internal (assembly);

	if (!consider_image (image))
		return;

	register_image (image);

	int rows = mono_image_get_table_rows (image, MONO_TABLE_TYPEDEF);

	for (int i = 1; i <= rows; i++) {
		ERROR_DECL (error);

		MonoClass *klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF | i, error);

		/*
		 * Swallow the error. The program will get an exception later on anyway
		 * when trying to use the class. If it's an entirely unused class,
		 * there's not much we can do; in that case, we just won't write it as
		 * uncovered to the output file.
		 */
		mono_error_cleanup (error);

		if (!klass || !consider_class (image, klass))
			continue;

		register_class (klass);
	}
}

static void
process_deferred_assembly (gpointer key, gpointer value, gpointer userdata)
{
	MonoAssembly *assembly = key;

	assembly_loaded ((MonoProfiler *) userdata, assembly);
	mono_assembly_close (assembly);
}

static gboolean
coverage_filter (MonoProfiler *prof, MonoMethod *method)
{
	guint32 iflags, flags;

	if (prof->done)
		return FALSE;
	if (prof->config->write_at && mono_method_desc_match (prof->config->write_at, method)) {
		printf ("coverage-profiler | Writing data at: '%s'.\n", mono_method_full_name (method, 1));
		dump_coverage (prof);
		prof->done = TRUE;
		return FALSE;
	}

	if (method->wrapper_type)
		return FALSE;

	flags = mono_method_get_flags (method, &iflags);

	if ((iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return FALSE;

	// Don't need to do anything else if we're already tracking this method.
	if (mono_conc_hashtable_lookup (coverage_profiler.methods, method))
		return TRUE;

	MonoClass *klass = mono_method_get_class (method);
	MonoImage *image = mono_class_get_image (klass);

	if (!consider_image (image) || !consider_class (image, klass))
		return FALSE;

	MonoLockFreeQueue *image_methods = register_image (image);
	MonoLockFreeQueue *class_methods = register_class (klass);

	mono_os_mutex_lock (&coverage_profiler.mutex);

	/*
	 * Another thread might've inserted the method since the check above, so
	 * check again now that we're holding the lock.
	 */
	if (!mono_conc_hashtable_lookup (coverage_profiler.methods, method)) {
		mono_conc_hashtable_insert (coverage_profiler.methods, method, method);
		mono_os_mutex_unlock (&coverage_profiler.mutex);

		// Don't need to hold the lock for these.
		MonoLockFreeQueueNode *image_methods_node = create_method_node (method);
		mono_lock_free_queue_enqueue (image_methods, image_methods_node);

		MonoLockFreeQueueNode *class_methods_node = create_method_node (method);
		mono_lock_free_queue_enqueue (class_methods, class_methods_node);
	} else {
		mono_os_mutex_unlock (&coverage_profiler.mutex);
	}

	return TRUE;
}

#define LINE_BUFFER_SIZE 4096
/* Max file limit of 128KB */
#define MAX_FILE_SIZE 128 * 1024
static char *
get_file_content (const gchar *filename)
{
	char *buffer;
	ssize_t bytes_read;
	long filesize;
	int res, offset = 0;
	FILE *stream;

	stream = fopen (filename, "r");
	if (stream == NULL)
		return NULL;

	res = fseek (stream, 0, SEEK_END);
	if (res < 0) {
		fclose (stream);
		return NULL;
	}

	filesize = ftell (stream);
	if (filesize < 0) {
		fclose (stream);
		return NULL;
	}

	res = fseek (stream, 0, SEEK_SET);
	if (res < 0) {
		fclose (stream);
		return NULL;
	}

	if (filesize > MAX_FILE_SIZE) {
		fclose (stream);
		return NULL;
	}

	buffer = (char *) g_malloc ((filesize + 1) * sizeof (char));
	while ((bytes_read = fread (buffer + offset, 1, LINE_BUFFER_SIZE, stream)) > 0)
		offset += bytes_read;

	/* NULL terminate our buffer */
	buffer[filesize] = '\0';

	fclose (stream);
	return buffer;
}

static char *
get_next_line (char *contents, char **next_start)
{
	char *p = contents;

	if (p == NULL || *p == '\0') {
		*next_start = NULL;
		return NULL;
	}

	while (*p != '\n' && *p != '\0')
		p++;

	if (*p == '\n') {
		*p = '\0';
		*next_start = p + 1;
	} else
		*next_start = NULL;

	return contents;
}

static void
init_suppressed_assemblies (void)
{
	char *content;
	char *line;

	coverage_profiler.suppressed_assemblies = mono_conc_hashtable_new (g_str_hash, g_str_equal);

	/* Don't need to free content as it is referred to by the lines stored in @filters */
	content = get_file_content (SUPPRESSION_DIR "/mono-profiler-coverage.suppression");
	if (content == NULL)
		return;

	while ((line = get_next_line (content, &content))) {
		line = g_strchomp (g_strchug (line));
		/* No locking needed as we're doing initialization */
		mono_conc_hashtable_insert (coverage_profiler.suppressed_assemblies, line, line);
	}
}

static void
parse_cov_filter_file (GPtrArray *filters, const char *file)
{
	char *content;
	char *line;

	/* Don't need to free content as it is referred to by the lines stored in @filters */
	content = get_file_content (file);
	if (content == NULL) {
		mono_profiler_printf_err ("Could not open coverage filter file '%s'.", file);
		return;
	}

	while ((line = get_next_line (content, &content)))
		g_ptr_array_add (filters, g_strchug (g_strchomp (line)));
}

static void
unref_coverage_assemblies (gpointer key, gpointer value, gpointer userdata)
{
	MonoAssembly *assembly = (MonoAssembly *)value;
	mono_assembly_close (assembly);
}

static void
cov_shutdown (MonoProfiler *prof)
{
	mono_atomic_store_i32 (&coverage_profiler.in_shutdown, TRUE);

	dump_coverage (prof);

	mono_os_mutex_lock (&coverage_profiler.mutex);
	mono_conc_hashtable_foreach (coverage_profiler.assemblies, unref_coverage_assemblies, NULL);
	mono_os_mutex_unlock (&coverage_profiler.mutex);

	g_hash_table_destroy (coverage_profiler.uncovered_methods);

	mono_conc_hashtable_destroy (coverage_profiler.image_to_methods);
	mono_conc_hashtable_destroy (coverage_profiler.class_to_methods);

	g_hash_table_destroy (coverage_profiler.deferred_assemblies);
	mono_conc_hashtable_destroy (coverage_profiler.assemblies);
	mono_conc_hashtable_destroy (coverage_profiler.methods);

	mono_conc_hashtable_destroy (coverage_profiler.suppressed_assemblies);
	mono_conc_hashtable_destroy (coverage_profiler.filtered_classes);

	mono_os_mutex_destroy (&coverage_profiler.mutex);

	if (*coverage_config.output_filename == '|') {
		pclose (coverage_profiler.file);
	} else if (*coverage_config.output_filename == '#') {
		// do nothing
	} else {
		fclose (coverage_profiler.file);
	}

	g_free (coverage_profiler.args);
}

static void
runtime_initialized (MonoProfiler *profiler)
{
	mono_atomic_store_i32 (&coverage_profiler.runtime_inited, TRUE);

	mono_counters_register ("Event: Coverage methods", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_methods_ctr);
	mono_counters_register ("Event: Coverage statements", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_statements_ctr);
	mono_counters_register ("Event: Coverage classes", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_classes_ctr);
	mono_counters_register ("Event: Coverage assemblies", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_assemblies_ctr);

	// See the comment in assembly_loaded ().
	g_hash_table_foreach (coverage_profiler.deferred_assemblies, process_deferred_assembly, profiler);
}

static void usage (void);

static gboolean
match_option (const char *arg, const char *opt_name, const char **rval)
{
	if (rval) {
		const char *end = strchr (arg, '=');

		*rval = NULL;
		if (!end)
			return !strcmp (arg, opt_name);

		if (strncmp (arg, opt_name, strlen (opt_name)) || (end - arg) > strlen (opt_name) + 1)
			return FALSE;
		*rval = end + 1;
		return TRUE;
	} else {
		//FIXME how should we handle passing a value to an arg that doesn't expect it?
		return !strcmp (arg, opt_name);
	}
}

static void
parse_arg (const char *arg)
{
	const char *val;

	if (match_option (arg, "help", NULL)) {
		usage ();
	// } else if (match_option (arg, "zip", NULL)) {
	// 	coverage_config.use_zip = TRUE;
	} else if (match_option (arg, "output", &val)) {
		coverage_config.output_filename = g_strdup (val);
	// } else if (match_option (arg, "covfilter", &val)) {
	// 	g_error ("not supported");
	} else if (match_option (arg, "covfilter-file", &val)) {
		if (coverage_config.cov_filter_files == NULL)
			coverage_config.cov_filter_files = g_ptr_array_new ();
		g_ptr_array_add (coverage_config.cov_filter_files, g_strdup (val));
	} else if (match_option (arg, "write-at-method", &val)) {
		coverage_config.write_at = mono_method_desc_new (val, TRUE);
		if (!coverage_config.write_at) {
			mono_profiler_printf_err ("Could not parse method description: %s", val);
			exit (1);
		}
	} else if (match_option (arg, "send-to-method", &val)) {
		coverage_config.send_to = mono_method_desc_new (val, TRUE);
		if (!coverage_config.send_to) {
			mono_profiler_printf_err ("Could not parse method description: %s", val);
			exit (1);
		}
		coverage_config.send_to_str = strdup (val);
	} else if (match_option (arg, "send-to-arg", &val)) {
		coverage_config.send_to_arg = strdup (val);
	} else {
		mono_profiler_printf_err ("Could not parse argument: %s", arg);
	}
}

static void
parse_args (const char *desc)
{
	const char *p;
	gboolean in_quotes = FALSE;
	char quote_char = '\0';
	char *buffer = g_malloc (strlen (desc) + 1);
	int buffer_pos = 0;

	for (p = desc; *p; p++){
		switch (*p){
		case ',':
			if (!in_quotes) {
				if (buffer_pos != 0){
					buffer [buffer_pos] = 0;
					parse_arg (buffer);
					buffer_pos = 0;
				}
			} else {
				buffer [buffer_pos++] = *p;
			}
			break;

		case '\\':
			if (p [1]) {
				buffer [buffer_pos++] = p[1];
				p++;
			}
			break;
		case '\'':
		case '"':
			if (in_quotes) {
				if (quote_char == *p)
					in_quotes = FALSE;
				else
					buffer [buffer_pos++] = *p;
			} else {
				in_quotes = TRUE;
				quote_char = *p;
			}
			break;
		default:
			buffer [buffer_pos++] = *p;
			break;
		}
	}

	if (buffer_pos != 0) {
		buffer [buffer_pos] = 0;
		parse_arg (buffer);
	}

	g_free (buffer);
}

static void
usage (void)
{
	mono_profiler_printf ("Mono coverage profiler");
	mono_profiler_printf ("Usage: mono --profile=coverage[:OPTION1[,OPTION2...]] program.exe\n");
	mono_profiler_printf ("Options:");
	mono_profiler_printf ("\thelp                 show this usage info");

	// mono_profiler_printf ("\tcovfilter=ASSEMBLY   add ASSEMBLY to the code coverage filters");
	// mono_profiler_printf ("\t                     prefix a + to include the assembly or a - to exclude it");
	// mono_profiler_printf ("\t                     e.g. covfilter=-mscorlib");
	mono_profiler_printf ("\tcovfilter-file=FILE  use FILE to generate the list of assemblies to be filtered");
	mono_profiler_printf ("\toutput=FILENAME      write the data to file FILENAME (the file is always overwritten)");
	mono_profiler_printf ("\toutput=+FILENAME     write the data to file FILENAME.pid (the file is always overwritten)");
	mono_profiler_printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM");
	mono_profiler_printf ("\toutput=|PROGRAM      write the data to the stdin of PROGRAM");
	mono_profiler_printf ("\twrite-at-method=METHOD       write the data when METHOD is compiled.");
	mono_profiler_printf ("\tsend-to-method=METHOD       call METHOD with the collected data.");
	mono_profiler_printf ("\tsend-to-arg=STR      extra argument to pass to METHOD.");
	// mono_profiler_printf ("\tzip                  compress the output data");

	exit (0);
}

MONO_API void
mono_profiler_init_coverage (const char *desc);

void
mono_profiler_init_coverage (const char *desc)
{
	if (mono_jit_aot_compiling ()) {
		mono_profiler_printf_err ("The coverage profiler does not currently support instrumenting AOT code.");
		exit (1);
	}

	GPtrArray *filters = NULL;

	parse_args (desc [strlen("coverage")] == ':' ? desc + strlen ("coverage") + 1 : "");

	if (coverage_config.cov_filter_files) {
		filters = g_ptr_array_new ();
		int i;
		for (i = 0; i < coverage_config.cov_filter_files->len; ++i) {
			const char *name = coverage_config.cov_filter_files->pdata [i];
			parse_cov_filter_file (filters, name);
		}
	}

	coverage_profiler.args = g_strdup (desc);
	coverage_profiler.config = &coverage_config;

	//If coverage_config.output_filename begin with +, append the pid at the end
	if (!coverage_config.output_filename)
		coverage_config.output_filename = "coverage.xml";
	else if (*coverage_config.output_filename == '+')
		coverage_config.output_filename = g_strdup_printf ("%s.%d", coverage_config.output_filename + 1, getpid ());

	if (*coverage_config.output_filename == '|') {
#ifdef HAVE_POPEN
		coverage_profiler.file = popen (coverage_config.output_filename + 1, "w");
#else
		g_assert_not_reached ();
#endif
	} else if (*coverage_config.output_filename == '#') {
		coverage_profiler.file = fdopen (strtol (coverage_config.output_filename + 1, NULL, 10), "a");
	} else {
		coverage_profiler.file = fopen (coverage_config.output_filename, "w");
	}

	if (!coverage_profiler.file) {
		mono_profiler_printf_err ("Could not create coverage profiler output file '%s': %s", coverage_config.output_filename, g_strerror (errno));
		exit (1);
	}

	mono_os_mutex_init (&coverage_profiler.mutex);

	coverage_profiler.filters = filters;
	coverage_profiler.filtered_classes = mono_conc_hashtable_new (NULL, NULL);
	init_suppressed_assemblies ();

	coverage_profiler.images = g_hash_table_new (NULL, NULL);
	coverage_profiler.methods = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.assemblies = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.deferred_assemblies = g_hash_table_new (NULL, NULL);

	coverage_profiler.class_to_methods = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.image_to_methods = mono_conc_hashtable_new (NULL, NULL);

	coverage_profiler.uncovered_methods = g_hash_table_new (NULL, NULL);

	MonoProfilerHandle handle = coverage_profiler.handle = mono_profiler_create (&coverage_profiler);

	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized);
	mono_profiler_set_assembly_loaded_callback (handle, assembly_loaded);

	mono_profiler_enable_coverage ();
	mono_profiler_set_coverage_filter_callback (handle, coverage_filter);
}
