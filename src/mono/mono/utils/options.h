/**
 * \file Runtime options
 *
 * Copyright 2020 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_UTILS_FLAGS_H__
#define __MONO_UTILS_FLAGS_H__

#include <config.h>
#include <glib.h>

#include "mono/utils/mono-error.h"

/* Declare list of options */
/* Each option will declare an exported C variable named mono_opt_... */
MONO_BEGIN_DECLS
#define DEFINE_OPTION_FULL(flag_type, ctype, c_name, cmd_name, def_value, comment) \
	MONO_API_DATA ctype mono_opt_##c_name;
#define DEFINE_OPTION_READONLY(flag_type, ctype, c_name, cmd_name, def_value, comment) \
	static const ctype mono_opt_##c_name = def_value;
#include "options-def.h"
MONO_END_DECLS

void mono_options_print_usage (void);

void mono_options_parse_options (const char **args, int argc, int *out_argc, MonoError *error);

#endif
