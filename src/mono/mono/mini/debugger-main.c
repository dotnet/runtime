#include "mini.h"

int
main (int argc, char **argv, char **envp)
{
	MonoDomain *domain;
	const char *file;
	int retval;

	g_assert (argc >= 3);
	file = argv [2];

	g_set_prgname (file);

	mini_parse_default_optimizations (argv [1]);

	domain = mini_init (argv [0]);

	mono_config_parse (NULL);
	//mono_set_rootdir ();

	retval = mono_debugger_main (domain, file, argc, argv, envp);

	mini_cleanup (domain);

 	return retval;
}

