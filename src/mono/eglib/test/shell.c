#include <glib.h>
#include <string.h>
#include <stdio.h>
#include "test.h"

RESULT
test_shell_argv1 ()
{
	GError *error;
	gint argc;
	gchar **argv;
	gboolean ret;

	/* The next line prints a critical error and returns FALSE 
	ret = g_shell_parse_argv (NULL, NULL, NULL, NULL);
	*/
	ret = g_shell_parse_argv ("", NULL, NULL, NULL);
	if (ret)
		return FAILED ("1. It should return FALSE");

	ret = g_shell_parse_argv ("hola", NULL, NULL, NULL);
	if (!ret)
		return FAILED ("2. It should return TRUE");

	argc = 0;
	ret = g_shell_parse_argv ("hola", &argc, NULL, NULL);
	if (!ret)
		return FAILED ("3. It should return TRUE");
	if (argc != 1)
		return FAILED ("4. argc was %d", argc);

	argc = 0;
	ret = g_shell_parse_argv ("hola bola", &argc, NULL, NULL);
	if (!ret)
		return FAILED ("5. It should return TRUE");
	if (argc != 2)
		return FAILED ("6. argc was %d", argc);

	argc = 0;
	ret = g_shell_parse_argv ("hola bola", &argc, &argv, NULL);
	if (!ret)
		return FAILED ("7. It should return TRUE");
	if (argc != 2)
		return FAILED ("8. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("9. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "bola"))
		return FAILED ("10. argv[1] was %s", argv [1]);

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola      'bola'", &argc, &argv, &error);
	if (!ret)
		return FAILED ("11. It should return TRUE");
	if (argc != 2)
		return FAILED ("12. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("13. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "bola"))
		return FAILED ("14. argv[1] was %s", argv [1]);
	if (error != NULL)
		return FAILED ("15. error is not null");


	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola    ''  'bola'", &argc, &argv, &error);
	if (!ret)
		return FAILED ("16. It should return TRUE");
	if (argc != 3)
		return FAILED ("17. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("18. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], ""))
		return FAILED ("19. argv[2] was %s", argv [1]);
	if (strcmp (argv [2], "bola"))
		return FAILED ("19. argv[2] was %s", argv [2]);
	if (error != NULL)
		return FAILED ("20. error is not null");

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	return OK;
}

RESULT
test_shell_argv2 ()
{
	GError *error;
	gint argc;
	gchar **argv;
	gboolean ret;

	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola      \"bola\"", &argc, &argv, &error);
	if (!ret)
		return FAILED ("1. It should return TRUE");
	if (argc != 2)
		return FAILED ("2. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("3. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "bola"))
		return FAILED ("4. argv[1] was %s", argv [1]);
	if (error != NULL)
		return FAILED ("5. error is not null");

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola    \"\"  \"bola \"", &argc, &argv, &error);
	if (!ret)
		return FAILED ("6. It should return TRUE");
	if (argc != 3)
		return FAILED ("7. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("8. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], ""))
		return FAILED ("9. argv[2] was %s", argv [1]);
	if (strcmp (argv [2], "bola "))
		return FAILED ("10. argv[2] was %s", argv [2]);
	if (error != NULL)
		return FAILED ("11. error is not null");

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola\n\t    \"\t\"  \"bola \"", &argc, &argv, &error);
	if (!ret)
		return FAILED ("10. It should return TRUE");
	if (argc != 3)
		return FAILED ("11. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("12. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "\t"))
		return FAILED ("13. argv[2] was %s", argv [1]);
	if (strcmp (argv [2], "bola "))
		return FAILED ("14. argv[2] was %s", argv [2]);
	if (error != NULL)
		return FAILED ("15. error is not null");

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola\n\t  \\\n  \"\t\"  \"bola \"", &argc, &argv, &error);
	if (!ret)
		return FAILED ("16. It should return TRUE");
	if (argc != 3)
		return FAILED ("17. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("18. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "\t"))
		return FAILED ("19. argv[2] was %s", argv [1]);
	if (strcmp (argv [2], "bola "))
		return FAILED ("20. argv[2] was %s", argv [2]);
	if (error != NULL)
		return FAILED ("21. error is not null");

	g_strfreev (argv);
	return OK;
}

RESULT
test_shell_argv3 ()
{
	GError *error;
	gint argc;
	gchar **argv;
	gboolean ret;

	argv = NULL;
	argc = 0;
	error = NULL;
	ret = g_shell_parse_argv ("hola      \"bola", &argc, &argv, &error);
	if (ret)
		return FAILED ("1. It should return FALSE");
	if (argc != 0)
		return FAILED ("2. argc was %d", argc);
	if (argv != NULL)
		return FAILED ("3. argv[0] was %s", argv [0]);
	if (error == NULL)
		return FAILED ("4. error is null");

	/* Text ended before matching quote was found for ". (The text was 'hola      "bola') */
	g_error_free (error);
	error = NULL;
	ret = g_shell_parse_argv ("hola      \\\"bola", &argc, &argv, &error);
	if (!ret)
		return FAILED ("5. It should return TRUE");
	if (argc != 2)
		return FAILED ("6. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("18. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "\"bola"))
		return FAILED ("18. argv[1] was %s", argv [1]);
	if (error != NULL)
		return FAILED ("8. error is not null");

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	ret = g_shell_parse_argv ("hola      \"\n\\'bola\"", &argc, &argv, &error);
	if (!ret)
		return FAILED ("9. It should return TRUE. %s", error->message);
	if (argc != 2)
		return FAILED ("10. argc was %d", argc);
	if (strcmp (argv [0], "hola"))
		return FAILED ("11. argv[0] was %s", argv [0]);
	if (strcmp (argv [1], "\n\\'bola"))
		return FAILED ("12. argv[1] was %s", argv [1]);
	if (error != NULL)
		return FAILED ("13. error is not null");

	g_strfreev (argv);
	argv = NULL;
	argc = 0;
	return OK;
}

static Test shell_tests [] = {
	{"g_shell_parse_argv1", test_shell_argv1},
	{"g_shell_parse_argv2", test_shell_argv2},
	{"g_shell_parse_argv3", test_shell_argv3},
	{NULL, NULL}
};

DEFINE_TEST_GROUP_INIT(shell_tests_init, shell_tests)

