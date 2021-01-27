/**
 * \file Runtime options
 *
 * Copyright 2020 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <stdio.h>

#include "options.h"
#include "mono/utils/mono-error-internals.h"

typedef enum {
	MONO_OPTION_BOOL,
	MONO_OPTION_BOOL_READONLY,
	MONO_OPTION_INT,
	MONO_OPTION_STRING
} MonoOptionType;

/* Define flags */
#define DEFINE_OPTION_FULL(option_type, ctype, c_name, cmd_name, def_value, comment) \
	ctype mono_opt_##c_name = def_value;
#define DEFINE_OPTION_READONLY(option_type, ctype, c_name, cmd_name, def_value, comment)
#include "options-def.h"

/* Flag metadata */
typedef struct {
	MonoOptionType option_type;
	gpointer addr;
	const char *cmd_name;
	int cmd_name_len;
} OptionData;

static OptionData option_meta[] = {
#define DEFINE_OPTION_FULL(option_type, ctype, c_name, cmd_name, def_value, comment) \
	{ option_type, &mono_opt_##c_name, cmd_name, sizeof (cmd_name) - 1 },
#define DEFINE_OPTION_READONLY(option_type, ctype, c_name, cmd_name, def_value, comment) \
	{ option_type, NULL, cmd_name, sizeof (cmd_name) - 1 },
#include "options-def.h"
};

static const char*
option_type_to_str (MonoOptionType type)
{
	switch (type) {
	case MONO_OPTION_BOOL:
		return "bool";
	case MONO_OPTION_BOOL_READONLY:
		return "bool (read-only)";
	case MONO_OPTION_INT:
		return "int";
	case MONO_OPTION_STRING:
		return "string";
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

static char *
option_value_to_str (MonoOptionType type, gconstpointer addr)
{
	switch (type) {
	case MONO_OPTION_BOOL:
	case MONO_OPTION_BOOL_READONLY:
		return *(gboolean*)addr ? g_strdup ("true") : g_strdup ("false");
	case MONO_OPTION_INT:
		return g_strdup_printf ("%d", *(int*)addr);
	case MONO_OPTION_STRING:
		return *(char**)addr ? g_strdup_printf ("%s", *(char**)addr) : g_strdup ("\"\"");
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

void
mono_options_print_usage (void)
{
#define DEFINE_OPTION_FULL(option_type, ctype, c_name, cmd_name, def_value, comment) do { \
		char *val = option_value_to_str (option_type, &mono_opt_##c_name); \
		g_printf ("  --%s (%s)\n\ttype: %s  default: %s\n", cmd_name, comment, option_type_to_str (option_type), val); \
		g_free (val); \
	} while (0);
#include "options-def.h"
}

/*
 * mono_optiond_parse_options:
 *
 *   Set options based on the command line arguments in ARGV/ARGC.
 * Remove processed arguments from ARGV and set *OUT_ARGC to the
 * number of remaining arguments.
 *
 * NOTE: This only sets the variables, the caller might need to do
 * additional processing based on the new values of the variables.
 */
void
mono_options_parse_options (const char **argv, int argc, int *out_argc, MonoError *error)
{
	int aindex = 0;
	int i;
	GHashTable *option_hash = NULL;

	while (aindex < argc) {
		const char *arg = argv [aindex];

		if (!(arg [0] == '-' && arg [1] == '-')) {
			aindex ++;
			continue;
		}
		arg = arg + 2;

		if (option_hash == NULL) {
			/* Compute a hash to avoid n^2 behavior */
			option_hash = g_hash_table_new (g_str_hash, g_str_equal);
			for (i = 0; i < G_N_ELEMENTS (option_meta); ++i) {
				g_hash_table_insert (option_hash, (gpointer)option_meta [i].cmd_name, &option_meta [i]);
			}
		}

		/* Compute flag name */
		char *arg_copy = g_strdup (arg);
		char *optname = arg_copy;
		int len = strlen (arg);
		int equals_sign_index = -1;
		/* Handle no- prefix */
		if (optname [0] == 'n' && optname [1] == 'o' && optname [2] == '-') {
			optname += 3;
		} else {
			/* Handle option=value */
			for (int i = 0; i < len; ++i) {
				if (optname [i] == '=') {
					equals_sign_index = i;
					optname [i] = '\0';
					break;
				}
			}
		}

		OptionData *option = (OptionData*)g_hash_table_lookup (option_hash, optname);
		g_free (arg_copy);

		if (!option) {
			aindex ++;
			continue;
		}

		switch (option->option_type) {
		case MONO_OPTION_BOOL:
		case MONO_OPTION_BOOL_READONLY: {
			gboolean negate = FALSE;
			if (len == option->cmd_name_len) {
			} else if (arg [0] == 'n' && arg [1] == 'o' && arg [2] == '-' && len == option->cmd_name_len + 3) {
				negate = TRUE;
			} else {
				break;
			}
			if (option->option_type == MONO_OPTION_BOOL_READONLY) {
				mono_error_set_error (error, 1, "Unable to set option '%s' as it's read-only.\n", arg);
				break;
			}
			*(gboolean*)option->addr = negate ? FALSE : TRUE;
			argv [aindex] = NULL;
			break;
		}
		case MONO_OPTION_INT:
		case MONO_OPTION_STRING: {
			const char *value = NULL;

			if (len == option->cmd_name_len) {
				// --option value
				if (aindex + 1 == argc) {
					mono_error_set_error (error, 1, "Missing value for option '%s'.\n", option->cmd_name);
					break;
				}
				value = argv [aindex + 1];
				argv [aindex] = NULL;
				argv [aindex + 1] = NULL;
				aindex ++;
			} else if (equals_sign_index != -1) {
				// option=value
				value = arg + equals_sign_index + 1;
				argv [aindex] = NULL;
			} else {
				g_assert_not_reached ();
			}

			if (option->option_type == MONO_OPTION_STRING) {
				*(char**)option->addr = g_strdup (value);
			} else {
				char *endp;
				long v = strtol (value, &endp, 10);
				if (!value [0] || *endp) {
					mono_error_set_error (error, 1, "Invalid value for option '%s': '%s'.\n", option->cmd_name, value);
					break;
				}
				*(int*)option->addr = (int)v;
			}
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}

		if (!is_ok (error))
			break;
		aindex ++;
	}

	if (option_hash)
		g_hash_table_destroy (option_hash);
	if (!is_ok (error))
		return;

	/* Remove processed arguments */
	aindex = 0;
	for (i = 0; i < argc; ++i) {
		if (argv [i])
			argv [aindex ++] = argv [i];
	}
	*out_argc = aindex;
}
