#include "config.h"

#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/class.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object.h>
#include <mono/utils/mono-hash.h>
#include <mono/metadata/gc-internal.h>

#ifdef DISABLE_PORTABILITY
int __mono_io_portability_helpers = PORTABILITY_NONE;

void 
mono_portability_helpers_init (void)
{
}

gchar *
mono_portability_find_file (const gchar *pathname, gboolean last_exists)
{
	g_assert_not_reached();
	return NULL;
}

#else

typedef struct 
{
	guint32 count;
	gchar *requestedName;
	gchar *actualName;
} MismatchedFilesStats;

static CRITICAL_SECTION mismatched_files_section;
static MonoGHashTable *mismatched_files_hash = NULL;

static inline gchar *mono_portability_find_file_internal (GString **report, gboolean *differs, const gchar *pathname, gboolean last_exists);
static inline void append_report (GString **report, const gchar *format, ...);
static inline void print_report (const gchar *report);
static inline guint32 calc_strings_hash (const gchar *str1, const gchar *str2);
static void print_mismatched_stats_at_exit (void);

#include <dirent.h>

int __mono_io_portability_helpers = PORTABILITY_UNKNOWN;

static void mismatched_stats_foreach_func (gpointer key, gpointer value, gpointer user_data)
{
	MismatchedFilesStats *stats = (MismatchedFilesStats*)value;

	fprintf (stdout,
		 "    Count: %u\n"
		 "Requested: %s\n"
		 "   Actual: %s\n\n",
		 stats->count, stats->requestedName, stats->actualName);
}

static void print_mismatched_stats_at_exit (void)
{
	if (!mismatched_files_hash || mono_g_hash_table_size (mismatched_files_hash) == 0)
		return;

	fprintf (stdout, "\n-=-=-=-=-=-=-= MONO_IOMAP Stats -=-=-=-=-=-=-=\n");
	mono_g_hash_table_foreach (mismatched_files_hash, mismatched_stats_foreach_func, NULL);
	fflush (stdout);
}

static guint mismatched_files_guint32_hash (gconstpointer key)
{
	if (!key)
		return 0;

	return *((guint32*)key);
}

static gboolean mismatched_files_guint32_equal (gconstpointer key1, gconstpointer key2)
{
	if (!key1 || !key2)
		return FALSE;

	return (gboolean)(*((guint32*)key1) == *((guint32*)key2));
}

void mono_portability_helpers_init (void)
{
        const gchar *env;

	if (__mono_io_portability_helpers != PORTABILITY_UNKNOWN)
		return;
	
        __mono_io_portability_helpers = PORTABILITY_NONE;
	
        env = g_getenv ("MONO_IOMAP");
        if (env != NULL) {
                /* parse the environment setting and set up some vars
                 * here
                 */
                gchar **options = g_strsplit (env, ":", 0);
                int i;
                
                if (options == NULL) {
                        /* This shouldn't happen */
                        return;
                }
                
                for (i = 0; options[i] != NULL; i++) {
#ifdef DEBUG
                        g_message ("%s: Setting option [%s]", __func__,
                                   options[i]);
#endif
                        if (!strncasecmp (options[i], "drive", 5)) {
                                __mono_io_portability_helpers |= PORTABILITY_DRIVE;
                        } else if (!strncasecmp (options[i], "case", 4)) {
                                __mono_io_portability_helpers |= PORTABILITY_CASE;
                        } else if (!strncasecmp (options[i], "all", 3)) {
                                __mono_io_portability_helpers |= (PORTABILITY_DRIVE | PORTABILITY_CASE);
			} else if (!strncasecmp (options[i], "report", 7)) {
				__mono_io_portability_helpers |= PORTABILITY_REPORT;
			}
                }
	}

	if (IS_PORTABILITY_REPORT) {
		InitializeCriticalSection (&mismatched_files_section);
		MONO_GC_REGISTER_ROOT (mismatched_files_hash);
		mismatched_files_hash = mono_g_hash_table_new (mismatched_files_guint32_hash, mismatched_files_guint32_equal);
		g_atexit (print_mismatched_stats_at_exit);
	}
}

/* Returns newly allocated string, or NULL on failure */
static gchar *find_in_dir (DIR *current, const gchar *name)
{
	struct dirent *entry;

#ifdef DEBUG
	g_message ("%s: looking for [%s]\n", __func__, name);
#endif
	
	while((entry = readdir (current)) != NULL) {
#ifdef DEBUGX
		g_message ("%s: found [%s]\n", __func__, entry->d_name);
#endif
		
		if (!g_ascii_strcasecmp (name, entry->d_name)) {
			char *ret;
			
#ifdef DEBUG
			g_message ("%s: matched [%s] to [%s]\n", __func__,
				   entry->d_name, name);
#endif

			ret = g_strdup (entry->d_name);
			closedir (current);
			return ret;
		}
	}
	
#ifdef DEBUG
	g_message ("%s: returning NULL\n", __func__);
#endif
	
	closedir (current);
	
	return(NULL);
}

static inline guint32 do_calc_string_hash (guint32 hash, const gchar *str)
{
	guint32 ret = hash;
	gchar *cc = (gchar*)str;
	gchar *end = (gchar*)(str + strlen (str) - 1);

	for (; cc < end; cc += 2) {
		ret = (ret << 5) - ret + *cc;
		ret = (ret << 5) - ret + cc [1];
	}
	end++;
	if (cc < end)
		ret = (ret << 5) - ret + *cc;

	return ret;
}

static inline guint32 calc_strings_hash (const gchar *str1, const gchar *str2)
{
	return do_calc_string_hash (do_calc_string_hash (0, str1), str2);
}

static inline void print_report (const gchar *report)
{
	MonoClass *klass;
	MonoProperty *prop;
	MonoString *str;
	char *stack_trace;

	fprintf (stdout, "-=-=-=-=-=-=- MONO_IOMAP REPORT -=-=-=-=-=-=-\n%s\n", report);
	klass = mono_class_from_name (mono_defaults.corlib, "System", "Environment");
	mono_class_init (klass);
	prop = mono_class_get_property_from_name (klass, "StackTrace");
	str = (MonoString*)mono_property_get_value (prop, NULL, NULL, NULL);
	stack_trace = mono_string_to_utf8 (str);

	fprintf (stdout, "-= Stack Trace =-\n%s\n\n", stack_trace);
	g_free (stack_trace);
	fflush (stdout);
}

static inline void append_report (GString **report, const gchar *format, ...)
{
	va_list ap;
	if (!*report)
		*report = g_string_new ("");

	va_start (ap, format);
	g_string_append_vprintf (*report, format, ap);
	va_end (ap);
}

gchar *mono_portability_find_file (const gchar *pathname, gboolean last_exists)
{
	GString *report = NULL;
	gboolean differs = FALSE;
	gchar *ret =  mono_portability_find_file_internal (&report, &differs, pathname, last_exists);
	if (report) {
		if (report->len && differs) {
			char *rep = g_string_free (report, FALSE);
			print_report (rep);
			g_free (rep);
		} else
			g_string_free (report, TRUE);
	}

	return ret;
}

/* Returns newly-allocated string or NULL on failure */
static inline gchar *mono_portability_find_file_internal (GString **report, gboolean *differs, const gchar *pathname, gboolean last_exists)
{
	gchar *new_pathname, **components, **new_components;
	int num_components = 0, component = 0;
	DIR *scanning = NULL;
	size_t len;
	gboolean do_report = IS_PORTABILITY_REPORT;
	gboolean drive_stripped = FALSE;

	if (IS_PORTABILITY_NONE) {
		return(NULL);
	}

	if (do_report)
		append_report (report, " - Requested file path: '%s'\n", pathname);
	new_pathname = g_strdup (pathname);
	
#ifdef DEBUG
	g_message ("%s: Finding [%s] last_exists: %s\n", __func__, pathname,
		   last_exists?"TRUE":"FALSE");
#endif
	
	if (last_exists &&
	    access (new_pathname, F_OK) == 0) {
#ifdef DEBUG
		g_message ("%s: Found it without doing anything\n", __func__);
#endif
		return(new_pathname);
	}
	
	/* First turn '\' into '/' and strip any drive letters */
	g_strdelimit (new_pathname, "\\", '/');

#ifdef DEBUG
	g_message ("%s: Fixed slashes, now have [%s]\n", __func__,
		   new_pathname);
#endif
	
	if (IS_PORTABILITY_DRIVE &&
	    g_ascii_isalpha (new_pathname[0]) &&
	    (new_pathname[1] == ':')) {
		int len = strlen (new_pathname);
		
		g_memmove (new_pathname, new_pathname+2, len - 2);
		new_pathname[len - 2] = '\0';

		if (do_report) {
			append_report (report, " - Stripped drive letter.\n");
			drive_stripped = TRUE;
		}
#ifdef DEBUG
		g_message ("%s: Stripped drive letter, now looking for [%s]\n",
			   __func__, new_pathname);
#endif
	}

	len = strlen (new_pathname);
	if (len > 1 && new_pathname [len - 1] == '/') {
		new_pathname [len - 1] = 0;
#ifdef DEBUG
		g_message ("%s: requested name had a trailing /, rewritten to '%s'\n",
			   __func__, new_pathname);
#endif
	}

	if (last_exists &&
	    access (new_pathname, F_OK) == 0) {
#ifdef DEBUG
		g_message ("%s: Found it\n", __func__);
#endif
		if (do_report && drive_stripped)
			*differs = TRUE;

		return(new_pathname);
	}

	/* OK, have to work harder.  Take each path component in turn
	 * and do a case-insensitive directory scan for it
	 */

	if (!(IS_PORTABILITY_CASE)) {
		g_free (new_pathname);
		return(NULL);
	}

	components = g_strsplit (new_pathname, "/", 0);
	if (components == NULL) {
		/* This shouldn't happen */
		g_free (new_pathname);
		return(NULL);
	}
	
	while(components[num_components] != NULL) {
		num_components++;
	}
	g_free (new_pathname);
	
	if (num_components == 0){
		return NULL;
	}
	

	new_components = (gchar **)g_new0 (gchar **, num_components + 1);

	if (num_components > 1) {
		if (strcmp (components[0], "") == 0) {
			/* first component blank, so start at / */
			scanning = opendir ("/");
			if (scanning == NULL) {
#ifdef DEBUG
				g_message ("%s: opendir 1 error: %s", __func__,
					   g_strerror (errno));
#endif
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			new_components[component++] = g_strdup ("");
		} else {
			DIR *current;
			gchar *entry;
		
			current = opendir (".");
			if (current == NULL) {
#ifdef DEBUG
				g_message ("%s: opendir 2 error: %s", __func__,
					   g_strerror (errno));
#endif
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			entry = find_in_dir (current, components[0]);
			if (entry == NULL) {
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			scanning = opendir (entry);
			if (scanning == NULL) {
#ifdef DEBUG
				g_message ("%s: opendir 3 error: %s", __func__,
					   g_strerror (errno));
#endif
				g_free (entry);
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		
			new_components[component++] = entry;
		}
	} else {
		if (last_exists) {
			if (strcmp (components[0], "") == 0) {
				/* First and only component blank */
				new_components[component++] = g_strdup ("");
			} else {
				DIR *current;
				gchar *entry;
				
				current = opendir (".");
				if (current == NULL) {
#ifdef DEBUG
					g_message ("%s: opendir 4 error: %s",
						   __func__,
						   g_strerror (errno));
#endif
					g_strfreev (new_components);
					g_strfreev (components);
					return(NULL);
				}
				
				entry = find_in_dir (current, components[0]);
				if (entry == NULL) {
					g_strfreev (new_components);
					g_strfreev (components);
					return(NULL);
				}
				
				new_components[component++] = entry;
			}
		} else {
				new_components[component++] = g_strdup (components[0]);
		}
	}

#ifdef DEBUG
	g_message ("%s: Got first entry: [%s]\n", __func__, new_components[0]);
#endif

	g_assert (component == 1);
	
	for(; component < num_components; component++) {
		gchar *entry;
		gchar *path_so_far;
		
		if (!last_exists &&
		    component == num_components -1) {
			entry = g_strdup (components[component]);
			closedir (scanning);
		} else {
			entry = find_in_dir (scanning, components[component]);
			if (entry == NULL) {
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		}
		
		new_components[component] = entry;
		
		if (component < num_components -1) {
			path_so_far = g_strjoinv ("/", new_components);

			scanning = opendir (path_so_far);
			g_free (path_so_far);
			if (scanning == NULL) {
				g_strfreev (new_components);
				g_strfreev (components);
				return(NULL);
			}
		}
	}
	
	g_strfreev (components);

	new_pathname = g_strjoinv ("/", new_components);

#ifdef DEBUG
	g_message ("%s: pathname [%s] became [%s]\n", __func__, pathname,
		   new_pathname);
#endif
	
	g_strfreev (new_components);

	if ((last_exists &&
	     access (new_pathname, F_OK) == 0) ||
	    (!last_exists)) {
		if (do_report && strcmp (pathname, new_pathname) != 0) {
			guint32 hash;
			MismatchedFilesStats *stats;

			EnterCriticalSection (&mismatched_files_section);
			hash = calc_strings_hash (pathname, new_pathname);
			stats = (MismatchedFilesStats*)mono_g_hash_table_lookup (mismatched_files_hash, &hash);
			if (stats == NULL) {
				guint32 *hashptr;

				stats = (MismatchedFilesStats*) g_malloc (sizeof (MismatchedFilesStats));
				stats->count = 1;
				stats->requestedName = g_strdup (pathname);
				stats->actualName = g_strdup (new_pathname);
				hashptr = (guint32*)g_malloc (sizeof (guint32));
				*hashptr = hash;
				mono_g_hash_table_insert (mismatched_files_hash, (gpointer)hashptr, stats);

				*differs = TRUE;
				append_report (report, " - Found file path: '%s'\n", new_pathname);
			} else {
				stats->count++;
				LeaveCriticalSection (&mismatched_files_section);
			}
		}
		
		return(new_pathname);
	}
	
	g_free (new_pathname);
	return(NULL);
}


#endif
