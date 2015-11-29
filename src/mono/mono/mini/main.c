#include <config.h>
#include "mini.h"

#ifndef HOST_WIN32
#ifndef BUILDVER_INCLUDED
#include "buildver-boehm.h"
#endif
#endif

/*
 * If the MONO_ENV_OPTIONS environment variable is set, it uses this as a
 * source of command line arguments that are passed to Mono before the
 * command line arguments specified in the command line.
 */
static int
mono_main_with_options (int argc, char *argv [])
{
	mono_parse_env_options (&argc, &argv);

	return mono_main (argc, argv);
}

#ifdef HOST_WIN32

#include <shellapi.h>

int
main (void)
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
