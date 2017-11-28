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

#ifdef HAVE_DLFCN_H
#include <dlfcn.h>
#endif
#include <fcntl.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif

#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/metadata-internals.h>

#include <mono/mini/jit.h>

#include <mono/utils/atomic.h>
#include <mono/utils/hazard-pointer.h>
#include <mono/utils/lock-free-queue.h>
#include <mono/utils/mono-conc-hashtable.h>
#include <mono/utils/mono-os-mutex.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-counters.h>

// Statistics for profiler events.
static gint32 coverage_methods_ctr,
              coverage_statements_ctr,
              coverage_classes_ctr,
              coverage_assemblies_ctr;

struct _MonoProfiler {
	MonoProfilerHandle handle;

	FILE* file;

	char *args;

	mono_mutex_t mutex;
	GPtrArray *data;

	GPtrArray *filters;
	MonoConcurrentHashTable *filtered_classes;
	MonoConcurrentHashTable *suppressed_assemblies;

	MonoConcurrentHashTable *methods;
	MonoConcurrentHashTable *assemblies;
	MonoConcurrentHashTable *classes;

	MonoConcurrentHashTable *image_to_methods;

	GHashTable *uncovered_methods;

	guint32 previous_offset;
};

typedef struct {
	//Where to compress the output file
	gboolean use_zip;

	//Name of the generated xml file
	const char *output_filename;

	//Filter files used by the code coverage mode
	GPtrArray *cov_filter_files;
} ProfilerConfig;

static ProfilerConfig coverage_config;
static struct _MonoProfiler coverage_profiler;

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
	g_assert (prof == &coverage_profiler);

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
					memcpy (new_name, "&lt;&gt;", 8);
					new_name += 8;
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

	coverage_profiler.previous_offset = 0;
	coverage_profiler.data = g_ptr_array_new ();

	mono_profiler_get_coverage_data (coverage_profiler.handle, method, obtain_coverage_for_method);

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);
	image_name = mono_image_get_name (image);

	method_signature = mono_signature_get_desc (mono_method_signature (method), TRUE);
	class_name = parse_generic_type_names (mono_type_get_name (mono_class_get_type (klass)));
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

	fprintf (coverage_profiler.file, "\t<method assembly=\"%s\" class=\"%s\" name=\"%s (%s)\" filename=\"%s\" token=\"%d\">\n",
		escaped_image_name, escaped_class_name, escaped_method_name, escaped_method_signature, escaped_method_filename, mono_method_get_token (method));

	g_free (escaped_image_name);
	g_free (escaped_class_name);
	g_free (escaped_method_name);
	g_free (escaped_method_signature);
	g_free (escaped_method_filename);

	for (i = 0; i < coverage_profiler.data->len; i++) {
		CoverageEntry *entry = (CoverageEntry *)coverage_profiler.data->pdata[i];

		fprintf (coverage_profiler.file, "\t\t<statement offset=\"%d\" counter=\"%d\" line=\"%d\" column=\"%d\"/>\n",
			entry->offset, entry->counter, entry->line, entry->column);
	}

	fprintf (coverage_profiler.file, "\t</method>\n");

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

	image = mono_class_get_image (klass);
	image_name = mono_image_get_name (image);

	if (!image_name || strcmp (image_name, mono_image_get_name (((MonoImage*) userdata))) != 0)
		return;

	class_name = mono_type_get_name (mono_class_get_type (klass));

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

	fprintf (coverage_profiler.file, "\t<class name=\"%s\" method-count=\"%d\" full=\"%d\" partial=\"%d\"/>\n",
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
	MonoImage *image = mono_assembly_get_image (assembly);
	const char *image_name, *image_guid, *image_filename;
	char *escaped_image_name, *escaped_image_filename;
	int number_of_methods = 0, partially_covered = 0;
	guint fully_covered = 0;

	image_name = mono_image_get_name (image);
	image_guid = mono_image_get_guid (image);
	image_filename = mono_image_get_filename (image);

	image_name = image_name ? image_name : "";
	image_guid = image_guid ? image_guid : "";
	image_filename = image_filename ? image_filename : "";

	get_coverage_for_image (image, &number_of_methods, &fully_covered, &partially_covered);

	escaped_image_name = escape_string_for_xml (image_name);
	escaped_image_filename = escape_string_for_xml (image_filename);

	fprintf (coverage_profiler.file, "\t<assembly name=\"%s\" guid=\"%s\" filename=\"%s\" method-count=\"%d\" full=\"%d\" partial=\"%d\"/>\n",
		escaped_image_name, image_guid, escaped_image_filename, number_of_methods, fully_covered, partially_covered);

	g_free (escaped_image_name);
	g_free (escaped_image_filename);

	mono_conc_hashtable_foreach (coverage_profiler.classes, dump_classes_for_image, image);
}

static void
dump_coverage (void)
{
	fprintf (coverage_profiler.file, "<?xml version=\"1.0\"?>\n");
	fprintf (coverage_profiler.file, "<coverage version=\"0.3\">\n");

	mono_os_mutex_lock (&coverage_profiler.mutex);
	mono_conc_hashtable_foreach (coverage_profiler.assemblies, dump_assembly, NULL);
	mono_conc_hashtable_foreach (coverage_profiler.methods, dump_method, NULL);
	g_hash_table_foreach (coverage_profiler.uncovered_methods, dump_method, NULL);
	mono_os_mutex_unlock (&coverage_profiler.mutex);

	fprintf (coverage_profiler.file, "</coverage>\n");
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
coverage_filter (MonoProfiler *prof, MonoMethod *method)
{
	MonoError error;
	MonoClass *klass;
	MonoImage *image;
	MonoAssembly *assembly;
	MonoMethodHeader *header;
	guint32 iflags, flags, code_size;
	char *fqn, *classname;
	gboolean has_positive, found;
	MonoLockFreeQueue *image_methods, *class_methods;
	MonoLockFreeQueueNode *node;

	g_assert (prof == &coverage_profiler);

	flags = mono_method_get_flags (method, &iflags);
	if ((iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		return FALSE;

	// Don't need to do anything else if we're already tracking this method
	if (mono_conc_hashtable_lookup (coverage_profiler.methods, method))
		return TRUE;

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);

	// Don't handle coverage for the core assemblies
	if (mono_conc_hashtable_lookup (coverage_profiler.suppressed_assemblies, (gpointer) mono_image_get_name (image)) != NULL)
		return FALSE;

	if (coverage_profiler.filters) {
		/* Check already filtered classes first */
		if (mono_conc_hashtable_lookup (coverage_profiler.filtered_classes, klass))
			return FALSE;

		classname = mono_type_get_name (mono_class_get_type (klass));

		fqn = g_strdup_printf ("[%s]%s", mono_image_get_name (image), classname);

		// Check positive filters first
		has_positive = FALSE;
		found = FALSE;
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

	header = mono_method_get_header_checked (method, &error);
	mono_error_cleanup (&error);

	mono_method_header_get_code (header, &code_size, NULL);

	assembly = mono_image_get_assembly (image);

	// Need to keep the assemblies around for as long as they are kept in the hashtable
	// Nunit, for example, has a habit of unloading them before the coverage statistics are
	// generated causing a crash. See https://bugzilla.xamarin.com/show_bug.cgi?id=39325
	mono_assembly_addref (assembly);

	mono_os_mutex_lock (&coverage_profiler.mutex);
	mono_conc_hashtable_insert (coverage_profiler.methods, method, method);
	mono_conc_hashtable_insert (coverage_profiler.assemblies, assembly, assembly);
	mono_os_mutex_unlock (&coverage_profiler.mutex);

	image_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (coverage_profiler.image_to_methods, image);

	if (image_methods == NULL) {
		image_methods = (MonoLockFreeQueue *) g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (image_methods);
		mono_os_mutex_lock (&coverage_profiler.mutex);
		mono_conc_hashtable_insert (coverage_profiler.image_to_methods, image, image_methods);
		mono_os_mutex_unlock (&coverage_profiler.mutex);
	}

	node = create_method_node (method);
	mono_lock_free_queue_enqueue (image_methods, node);

	class_methods = (MonoLockFreeQueue *)mono_conc_hashtable_lookup (coverage_profiler.classes, klass);

	if (class_methods == NULL) {
		class_methods = (MonoLockFreeQueue *) g_malloc (sizeof (MonoLockFreeQueue));
		mono_lock_free_queue_init (class_methods);
		mono_os_mutex_lock (&coverage_profiler.mutex);
		mono_conc_hashtable_insert (coverage_profiler.classes, klass, class_methods);
		mono_os_mutex_unlock (&coverage_profiler.mutex);
	}

	node = create_method_node (method);
	mono_lock_free_queue_enqueue (class_methods, node);

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
	g_assert (prof == &coverage_profiler);

	dump_coverage ();

	mono_os_mutex_lock (&coverage_profiler.mutex);
	mono_conc_hashtable_foreach (coverage_profiler.assemblies, unref_coverage_assemblies, NULL);
	mono_os_mutex_unlock (&coverage_profiler.mutex);

	mono_conc_hashtable_destroy (coverage_profiler.methods);
	mono_conc_hashtable_destroy (coverage_profiler.assemblies);
	mono_conc_hashtable_destroy (coverage_profiler.classes);
	mono_conc_hashtable_destroy (coverage_profiler.filtered_classes);

	mono_conc_hashtable_destroy (coverage_profiler.image_to_methods);
	mono_conc_hashtable_destroy (coverage_profiler.suppressed_assemblies);
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
	mono_counters_register ("Event: Coverage methods", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_methods_ctr);
	mono_counters_register ("Event: Coverage statements", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_statements_ctr);
	mono_counters_register ("Event: Coverage classes", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_classes_ctr);
	mono_counters_register ("Event: Coverage assemblies", MONO_COUNTER_UINT | MONO_COUNTER_PROFILER | MONO_COUNTER_MONOTONIC, &coverage_assemblies_ctr);
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
	char *buffer = malloc (strlen (desc));
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

	//If coverage_config.output_filename begin with +, append the pid at the end
	if (!coverage_config.output_filename)
		coverage_config.output_filename = "coverage.xml";
	else if (*coverage_config.output_filename == '+')
		coverage_config.output_filename = g_strdup_printf ("%s.%d", coverage_config.output_filename + 1, getpid ());

	if (*coverage_config.output_filename == '|')
		coverage_profiler.file = popen (coverage_config.output_filename + 1, "w");
	else if (*coverage_config.output_filename == '#')
		coverage_profiler.file = fdopen (strtol (coverage_config.output_filename + 1, NULL, 10), "a");
	else
		coverage_profiler.file = fopen (coverage_config.output_filename, "w");

	if (!coverage_profiler.file) {
		mono_profiler_printf_err ("Could not create coverage profiler output file '%s': %s", coverage_config.output_filename, g_strerror (errno));
		exit (1);
	}

	mono_os_mutex_init (&coverage_profiler.mutex);
	coverage_profiler.methods = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.assemblies = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.classes = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.filtered_classes = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.image_to_methods = mono_conc_hashtable_new (NULL, NULL);
	coverage_profiler.uncovered_methods = g_hash_table_new (NULL, NULL);
	init_suppressed_assemblies ();

	coverage_profiler.filters = filters;

	MonoProfilerHandle handle = coverage_profiler.handle = mono_profiler_create (&coverage_profiler);

	mono_profiler_set_runtime_shutdown_end_callback (handle, cov_shutdown);
	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized);

	mono_profiler_enable_coverage ();
	mono_profiler_set_coverage_filter_callback (handle, coverage_filter);
}
