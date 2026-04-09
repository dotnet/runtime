/**
 * \file
 * The main entry point for the mono executable
 *
 * The main entry point does a few things:
 *
 *   * Parses the MONO_ENV_OPTIONS variable to treat the
 *     contents of the variable as command line arguments for
 *     the mono runtime
 *
 *   * Launches Mono, by calling mono_main.
 */
#include <config.h>
#include <mono/utils/mono-publib.h>
#include <mono/metadata/assembly.h>

#include "mini.h"
#include "mini-runtime.h"

#ifdef HAVE_UNISTD_H
#  include <unistd.h>
#endif

//#define TEST_ICALL_SYMBOL_MAP 1

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

#if TEST_ICALL_SYMBOL_MAP

const char*
mono_lookup_icall_symbol_internal (gpointer func);

ICALL_EXPORT int ves_icall_System_Environment_get_ProcessorCount ();

#endif

#ifdef HOST_WIN32

#include <shellapi.h>

#ifdef _WINDOWS
int APIENTRY
wWinMain (HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
#else
int
main (int _argc, char* _argv[])
#endif
{
	int argc;
	gunichar2** argvw;
	gchar** argv;
	int i;

	argvw = CommandLineToArgvW (GetCommandLineW (), &argc);
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
#if TEST_ICALL_SYMBOL_MAP
	const char *p  = mono_lookup_icall_symbol_internal (mono_lookup_icall_symbol_internal);
	printf ("%s\n", p ? p : "null");
	p  = mono_lookup_icall_symbol_internal (ves_icall_System_Environment_get_ProcessorCount);
	printf ("%s\n", p ? p : "null");
#endif

	return mono_main_with_options (argc, argv);
}

#endif
