#include <config.h>
#include "mini.h"
#ifndef HOST_WIN32
#include "buildver.h"
#endif

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

	return mono_main (argc, argv);
}

#else

int
main (int argc, char* argv[])
{
	mono_build_date = build_date;

#if HAVE_BOEHM_GC
	{
		char *p = getenv ("MONO_VM_CONFIG");
		if (p != NULL && strstr (p, "sgen") != NULL){
			GString *path = g_string_new (argv [0]);
			g_string_append (path, "-sgen");
			execvp (path->str, argv);
		}
	}
#endif
	return mono_main (argc, argv);
}

#endif
