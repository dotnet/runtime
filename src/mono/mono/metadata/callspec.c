/**
 * \file
 * Call specification facilities for the Mono Runtime.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full
 * license information.
 */
#include <config.h>
#include "metadata.h"
#include "callspec.h"
#include "assembly.h"
#include "assembly-internals.h"
#include "class-internals.h"
#include "debug-helpers.h"

static MonoAssembly *prog_assembly;

gboolean
mono_callspec_eval_exception (MonoClass *klass, MonoCallSpec *spec)
{
	int include = 0;
	int i;

	if (!klass)
		return FALSE;

	for (i = 0; i < spec->len; i++) {
		MonoTraceOperation *op = &spec->ops [i];
		int inc = 0;

		switch (op->op) {
		case MONO_TRACEOP_EXCEPTION:
			if (strcmp ("", (const char*)op->data) == 0 &&
			    strcmp ("all", (const char*)op->data2) == 0)
				inc = 1;
			else if (strcmp ("", (const char*)op->data) == 0 ||
				 strcmp (m_class_get_name_space (klass), (const char*)op->data) == 0)
				if (strcmp (m_class_get_name (klass), (const char*)op->data2) == 0)
					inc = 1;
			break;
		default:
			break;
		}
		if (op->exclude) {
			if (inc)
				include = 0;
		} else if (inc)
			include = 1;
	}

	return include;
}

gboolean mono_callspec_eval (MonoMethod *method, const MonoCallSpec *spec)
{
	int include = 0;
	int i;

	for (i = 0; i < spec->len; i++) {
		MonoTraceOperation *op = &spec->ops[i];
		MonoMethodDesc *mdesc;
		gboolean is_full;
		int inc = 0;

		switch (op->op) {
		case MONO_TRACEOP_ALL:
			inc = 1;
			break;
		case MONO_TRACEOP_PROGRAM:
			if (prog_assembly &&
			    (m_class_get_image (method->klass) ==
			     mono_assembly_get_image_internal (prog_assembly)))
				inc = 1;
			break;
		case MONO_TRACEOP_WRAPPER:
			if ((method->wrapper_type ==
			     MONO_WRAPPER_NATIVE_TO_MANAGED) ||
			    (method->wrapper_type ==
			     MONO_WRAPPER_MANAGED_TO_NATIVE))
				inc = 1;
			break;
		case MONO_TRACEOP_METHOD:
			mdesc = (MonoMethodDesc*)op->data;
			is_full = mono_method_desc_is_full (mdesc);
			if (is_full &&
			    mono_method_desc_full_match (mdesc, method))
				inc = 1;
			if (!is_full && mono_method_desc_match (mdesc, method))
				inc = 1;
			break;
		case MONO_TRACEOP_CLASS:
			if (strcmp (m_class_get_name_space (method->klass), (const char*)op->data) == 0)
				if (strcmp (m_class_get_name (method->klass), (const char*)op->data2) ==
				    0)
					inc = 1;
			break;
		case MONO_TRACEOP_ASSEMBLY:
			if (strcmp (mono_image_get_name (m_class_get_image (method->klass)),
				    (const char*)op->data) == 0)
				inc = 1;
			break;
		case MONO_TRACEOP_NAMESPACE:
			if (strcmp (m_class_get_name_space (method->klass), (const char*)op->data) == 0)
				inc = 1;
			break;
		case MONO_TRACEOP_EXCEPTION:
			break;
		}
		if (op->exclude) {
			if (inc)
				include = 0;
		} else if (inc) {
			include = 1;
		}
	}
	return include;
}

static int is_filenamechar (char p)
{
	if (p >= 'A' && p <= 'Z')
		return TRUE;
	if (p >= 'a' && p <= 'z')
		return TRUE;
	if (p >= '0' && p <= '9')
		return TRUE;
	if (p == '.' || p == ':' || p == '_' || p == '-' || p == '`')
		return TRUE;
	return FALSE;
}

static char *get_string (char **in)
{
	char *start = *in;
	char *p = *in;
	while (is_filenamechar (*p)) {
		p++;
	}
	size_t len = p - start;
	char *ret = (char *)g_malloc (len + 1);
	memcpy (ret, start, len);
	ret [len] = 0;
	*in = p;
	return ret;
}

enum Token {
	TOKEN_METHOD,
	TOKEN_CLASS,
	TOKEN_ALL,
	TOKEN_PROGRAM,
	TOKEN_EXCEPTION,
	TOKEN_NAMESPACE,
	TOKEN_WRAPPER,
	TOKEN_STRING,
	TOKEN_EXCLUDE,
	TOKEN_DISABLED,
	TOKEN_SEPARATOR,
	TOKEN_END,
	TOKEN_ERROR
};

static int get_token (char **in, char **extra, char **errstr)
{
	char *p = *in;
	while (p[0] == '+')
		p++;

	*extra = NULL;

	if (p[0] == '\0') {
		*in = p;
		return TOKEN_END;
	}
	if (p[0] == 'M' && p[1] == ':') {
		p += 2;
		*extra = get_string (&p);
		*in = p;
		return TOKEN_METHOD;
	}
	if (p[0] == 'N' && p[1] == ':') {
		p += 2;
		*extra = get_string (&p);
		*in = p;
		return TOKEN_NAMESPACE;
	}
	if (p[0] == 'T' && p[1] == ':') {
		p += 2;
		*extra = get_string (&p);
		*in = p;
		return TOKEN_CLASS;
	}
	if (p[0] == 'E' && p[1] == ':') {
		p += 2;
		*extra = get_string (&p);
		*in = p;
		return TOKEN_EXCEPTION;
	}
	if (*p == '-') {
		p++;
		*in = p;
		return TOKEN_EXCLUDE;
	}
	if (is_filenamechar (*p)) {
		*extra = get_string (&p);
		*in = p;
		if (strcmp (*extra, "all") == 0)
			return TOKEN_ALL;
		if (strcmp (*extra, "program") == 0)
			return TOKEN_PROGRAM;
		if (strcmp (*extra, "wrapper") == 0)
			return TOKEN_WRAPPER;
		if (strcmp (*extra, "disabled") == 0)
			return TOKEN_DISABLED;
		return TOKEN_STRING;
	}
	if (*p == ',') {
		p++;
		*in = p;
		return TOKEN_SEPARATOR;
	}

	*errstr = g_strdup_printf ("Syntax error at or around '%s'", p);
	return TOKEN_ERROR;
}

static int get_spec (char **in, MonoCallSpec *spec, char **errstr)
{
	int n = spec->len;
	char *extra = NULL;

	int token = get_token (in, &extra, errstr);
	gboolean exclude = FALSE;
	if (token == TOKEN_EXCLUDE) {
		exclude = TRUE;
		token = get_token (in, &extra, errstr);
		if (token == TOKEN_EXCLUDE || token == TOKEN_DISABLED) {
			*errstr = g_strdup_printf ("Expecting an expression");
			token = TOKEN_ERROR;
			goto out;
		}
	}
	if (token == TOKEN_END || token == TOKEN_SEPARATOR ||
	    token == TOKEN_ERROR)
		goto out;

	if (token == TOKEN_DISABLED) {
		spec->enabled = FALSE;
		goto out;
	}

	if (token == TOKEN_METHOD) {
		MonoMethodDesc *desc = mono_method_desc_new (extra, TRUE);
		if (desc == NULL) {
			*errstr =
			    g_strdup_printf ("Invalid method name: %s", extra);
			token = TOKEN_ERROR;
			goto out;
		}
		spec->ops[n].op = MONO_TRACEOP_METHOD;
		spec->ops[n].data = desc;
	} else if (token == TOKEN_ALL)
		spec->ops[n].op = MONO_TRACEOP_ALL;
	else if (token == TOKEN_PROGRAM)
		spec->ops[n].op = MONO_TRACEOP_PROGRAM;
	else if (token == TOKEN_WRAPPER)
		spec->ops[n].op = MONO_TRACEOP_WRAPPER;
	else if (token == TOKEN_NAMESPACE) {
		spec->ops[n].op = MONO_TRACEOP_NAMESPACE;
		spec->ops[n].data = g_strdup (extra);
	} else if (token == TOKEN_CLASS || token == TOKEN_EXCEPTION) {
		char *p = strrchr (extra, '.');
		if (p) {
			*p++ = 0;
			spec->ops[n].data = g_strdup (extra);
			spec->ops[n].data2 = g_strdup (p);
		} else {
			spec->ops[n].data = g_strdup ("");
			spec->ops[n].data2 = g_strdup (extra);
		}
		spec->ops[n].op = token == TOKEN_CLASS ? MONO_TRACEOP_CLASS
						       : MONO_TRACEOP_EXCEPTION;
	} else if (token == TOKEN_STRING) {
		spec->ops[n].op = MONO_TRACEOP_ASSEMBLY;
		spec->ops[n].data = g_strdup (extra);
	} else {
		*errstr =
		    g_strdup_printf ("Syntax error in method specification");
		token = TOKEN_ERROR;
		goto out;
	}

	if (exclude)
		spec->ops[n].exclude = 1;

	spec->len = n + 1;
	token = TOKEN_SEPARATOR;
out:
	if (extra != NULL) {
		g_free (extra);
	}
	return token;
}

gboolean
mono_callspec_parse (const char *options, MonoCallSpec *spec, char **errstr)
{
	char *p = (char *)options;
	int size = 1;
	int token;

	memset (spec, 0, sizeof (*spec));
	*errstr = NULL;

	spec->enabled = TRUE;
	if (*p == 0) {
		spec->len = 1;
		spec->ops = g_new0 (MonoTraceOperation, 1);
		spec->ops[0].op = MONO_TRACEOP_ALL;
		return TRUE;
	}

	for (p = (char *)options; *p != 0; p++)
		if (*p == ',')
			size++;

	spec->ops = g_new0 (MonoTraceOperation, size);

	p = (char *)options;

	while ((token = (get_spec (&p, spec, errstr))) != TOKEN_END) {
		if (token == TOKEN_ERROR)
			return FALSE;
	}
	return TRUE;
}

void mono_callspec_cleanup (MonoCallSpec *spec)
{
	g_free (spec->ops);
	memset (spec, 0, sizeof (*spec));
}

void
mono_callspec_set_assembly (MonoAssembly *assembly)
{
	prog_assembly = assembly;
}
