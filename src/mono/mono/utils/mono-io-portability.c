/**
 * \file
 */

#include "config.h"

#include <string.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <errno.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_PORTABILITY

#include <dirent.h>

int mono_io_portability_helpers = PORTABILITY_UNKNOWN;

static inline gchar *mono_portability_find_file_internal (GString **report, const gchar *pathname, gboolean last_exists);

void mono_portability_helpers_init (void)
{
        gchar *env;

	if (mono_io_portability_helpers != PORTABILITY_UNKNOWN)
		return;
	
        mono_io_portability_helpers = PORTABILITY_NONE;
	
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
                                mono_io_portability_helpers |= PORTABILITY_DRIVE;
                        } else if (!strncasecmp (options[i], "case", 4)) {
                                mono_io_portability_helpers |= PORTABILITY_CASE;
                        } else if (!strncasecmp (options[i], "all", 3)) {
                                mono_io_portability_helpers |= (PORTABILITY_DRIVE | PORTABILITY_CASE);
			}
                }
		g_free (env);
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

static inline void append_report (GString **report, const gchar *format, ...)
{
	va_list ap;
	if (!*report)
		*report = g_string_new ("");

	va_start (ap, format);
	g_string_append_vprintf (*report, format, ap);
	va_end (ap);
}

static inline void do_mono_profiler_iomap (GString **report, const char *pathname, const char *new_pathname)
{
	char *rep = NULL;
	GString *tmp = report ? *report : NULL;

	if (tmp) {
		if (tmp->len > 0)
			rep = g_string_free (tmp, FALSE);
		else
			g_string_free (tmp, TRUE);
		*report = NULL;
	}

	MONO_PROFILER_RAISE (iomap_report, (rep, pathname, new_pathname));
	g_free (rep);
}

gchar *mono_portability_find_file (const gchar *pathname, gboolean last_exists)
{
	GString *report = NULL;
	gchar *ret;
	
	if (!pathname || !pathname [0])
		return NULL;
	ret = mono_portability_find_file_internal (&report, pathname, last_exists);

	if (report)
		g_string_free (report, TRUE);

	return ret;
}

/* Returns newly-allocated string or NULL on failure */
static inline gchar *mono_portability_find_file_internal (GString **report, const gchar *pathname, gboolean last_exists)
{
	gchar *new_pathname, **components, **new_components;
	int num_components = 0, component = 0;
	DIR *scanning = NULL;
	size_t len;
	gboolean drive_stripped = FALSE;
	gboolean do_report = MONO_PROFILER_ENABLED (iomap_report);

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
			do_mono_profiler_iomap (report, pathname, new_pathname);

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
		if (do_report && strcmp (pathname, new_pathname) != 0)
			do_mono_profiler_iomap (report, pathname, new_pathname);

		return(new_pathname);
	}

	g_free (new_pathname);
	return(NULL);
}

#else /* DISABLE_PORTABILITY */

MONO_EMPTY_SOURCE_FILE (mono_io_portability);

#endif /* DISABLE_PORTABILITY */
