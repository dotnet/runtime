#include <config.h>
#include "mini.h"
#ifndef HOST_WIN32
#include "buildver.h"
#endif

/*
 * If the MONO_ENV_OPTIONS environment variable is set, it uses this as a
 * source of command line arguments that are passed to Mono before the
 * command line arguments specified in the command line.
 */
static int
mono_main_with_options (int argc, char *argv [])
{
	const char *env_options = getenv ("MONO_ENV_OPTIONS");
	if (env_options != NULL){
		GPtrArray *array = g_ptr_array_new ();
		GString *buffer = g_string_new ("");
		const char *p;
		int i;

		for (p = env_options; *p; p++){
			switch (*p){
			case ' ': case '\t':
				if (buffer->len != 0){
					g_ptr_array_add (array, g_strdup (buffer->str));
					g_string_truncate (buffer, 0);
				}
				break;
			case '\\':
				if (p [1]){
					g_string_append_c (buffer, p [1]);
					p++;
				}
				break;
			default:
				g_string_append_c (buffer, *p);
				break;
			}
		}
		if (buffer->len != 0)
			g_ptr_array_add (array, g_strdup (buffer->str));
		g_string_free (buffer, TRUE);

		if (array->len > 0){
			int new_argc = array->len + argc;
			char **new_argv = g_new (char *, new_argc + 1);
			int j;

			new_argv [0] = argv [0];
			
			/* First the environment variable settings, to allow the command line options to override */
			for (i = 0; i < array->len; i++)
				new_argv [i+1] = g_ptr_array_index (array, i);
			i++;
			for (j = 1; j < argc; j++)
				new_argv [i++] = argv [j];
			new_argv [i] = NULL;

			argc = new_argc;
			argv = new_argv;
		}
		g_ptr_array_free (array, TRUE);
	}

	return mono_main (argc, argv);
}

#ifdef HOST_WIN32

int
main ()
{
	int argc;
	gunichar2** argvw;
	gchar** argv;
	int i;

	argvw = CommandLineToArgvW (GetCommandLine (), &argc);
	argv = g_new0 (gchar*, argc + 1);
	for (i = 0; i < argc; i++)
		argv [i] = g_utf16_to_utf8 (argvw [i], -1, NULL, NULL, NULL);
	argv [argc] = NULL;

	LocalFree (argvw);

	return mono_main_with_options  (argc, argv);
}

#else

int
main (int argc, char* argv[])
{
	mono_build_date = build_date;
	
	return mono_main_with_options (argc, argv);
}

#endif
