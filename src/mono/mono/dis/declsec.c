/*
 * declsec.h:  Support for the new declarative security attribute
 *	metadata format (2.0)
 *
 * Author:
 *	Sebastien Pouliot  <sebastien@ximian.com>
 *
 * Copyright (C) 2005 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <glib.h>
#include <math.h>

#include "mono/metadata/blob.h"
#include "mono/metadata/metadata.h"
#include "mono/metadata/mono-endian.h"
#include "mono/utils/mono-compiler.h"

#include "declsec.h"
#include "util.h"

static char*
declsec_20_get_classname (const char* p, const char **rptr)
{
	int apos, cpos = 0;
	char *c;
	char *a;
	char *result;
	GString *res = g_string_new ("");
	int len = mono_metadata_decode_value (p, &p);

	c = (char *) p;
	while ((*c++ != ',') && (cpos++ < len));
	c++;

	apos = cpos;
	a = c;
	while ((*a++ != ',') && (apos++ < len));

	if (apos - cpos > 1) {
		g_string_append_printf (res, "[%.*s]%.*s", apos - cpos, c, cpos, p);
	} else {
		/* in-assembly type aren't fully qualified (no comma) */
		g_string_append_printf (res, "%.*s", cpos - 1, p);
	}

	p += len;
	if (rptr)
		*rptr = p;

	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

static gboolean
declsec_20_write_type (GString *str, char type)
{
	switch (type) {
	case MONO_TYPE_BOOLEAN:
		g_string_append (str, "bool");
		break;
	case MONO_TYPE_SZARRAY:
		g_string_append (str, "[]");
		break;
	case MONO_TYPE_SYSTEM_TYPE:
		g_string_append (str, "type");
		break;
	case MONO_TYPE_STRING:
		g_string_append (str, "string");
		break;
	default:
		g_warning ("TODO type %d - please fill a bug report on this!", type);
		return FALSE;
	}
	return TRUE;
}

static const char*
declsec_20_write_value (GString *str, char type, const char *value)
{
	switch (type) {
	case MONO_TYPE_U1:
		g_string_append_printf (str, "%d", (unsigned char)*value);
		return value + 1;
	case MONO_TYPE_I1:
		g_string_append_printf (str, "%d", *value);
		return value + 1;
	case MONO_TYPE_BOOLEAN:
		g_string_append_printf (str, "%s", *value ? "true" : "false");
		return value + 1;
	case MONO_TYPE_CHAR:
		g_string_append_printf (str, "0x%04X", read16 (value));
		return value + 2;
	case MONO_TYPE_U2:
		g_string_append_printf (str, "%d", read16 (value));
		return value + 2;
	case MONO_TYPE_I2:
		g_string_append_printf (str, "%d", (gint16)read16 (value));
		return value + 2;
	case MONO_TYPE_U4:
		g_string_append_printf (str, "%d", read32 (value));
		return value + 4;
	case MONO_TYPE_I4:
		g_string_append_printf (str, "%d", (gint32)read32 (value));
		return value + 4;
	case MONO_TYPE_U8:
		g_string_append_printf (str, "%lld", (long long)read64 (value));
		return value + 8;
	case MONO_TYPE_I8:
		g_string_append_printf (str, "%lld", (long long)read64 (value));
		return value + 8;
	case MONO_TYPE_R4: {
		float val;
		int inf;
		readr4 (value, &val);
		inf = dis_isinf (val);
		if (inf == -1) 
			g_string_append_printf (str, "0xFF800000"); /* negative infinity */
		else if (inf == 1)
			g_string_append_printf (str, "0x7F800000"); /* positive infinity */
		else if (dis_isnan (val))
			g_string_append_printf (str, "0xFFC00000"); /* NaN */
		else
			g_string_append_printf (str, "%.8g", val);
		return value + 4;
	}
	case MONO_TYPE_R8: {
		double val;
		int inf;
		readr8 (value, &val);
		inf = dis_isinf (val);
		if (inf == -1) 
			g_string_append_printf (str, "0xFFF00000000000000"); /* negative infinity */
		else if (inf == 1)
			g_string_append_printf (str, "0x7FFF0000000000000"); /* positive infinity */
		else if (isnan (val))
			g_string_append_printf (str, "0xFFF80000000000000"); /* NaN */
		else
			g_string_append_printf (str, "%.17g", val);
		return value + 8;
	}
	case MONO_TYPE_STRING:
		if (*value == (char)0xff) {
			g_string_append (str, "nullref");
			return value + 1;
		} else {
			int len = mono_metadata_decode_value (value, &value);
			g_string_append_printf (str, "'%.*s'", len, value);
			return value + len;
		}
	case MONO_TYPE_SYSTEM_TYPE: {
		char *cname = declsec_20_get_classname (value, NULL);
		int len = mono_metadata_decode_value (value, &value);
		g_string_append (str, cname);
		g_free (cname);
		return value + len;
	}
	}
	return 0;
}

char*
dump_declsec_entry20 (MonoImage *m, const char* p, const char *indent)
{
	int i, num;
	char *result;
	GString *res = g_string_new ("");

	if (*p++ != MONO_DECLSEC_FORMAT_20)
		return NULL;

	g_string_append (res, "{");

	/* number of encoded permission attributes */
	num = mono_metadata_decode_value (p, &p);

	for (i = 0; i < num; i++) {
		int len, j, pos = 0, param_len;
		char *param_start;
		char *s = declsec_20_get_classname (p, &p);
		g_string_append_printf (res, "%s = {", s);
		g_free (s);

		/* optional parameters length */
		param_len = mono_metadata_decode_value (p, &p);
		param_start = (char *) p;

		/* number of parameters */
		pos = mono_metadata_decode_value (p, &p);
		for (j = 0; j < pos; j++) {
			int k, elem;
			int type = *p++;

			switch (type) {
			case MONO_DECLSEC_FIELD:
				/* not sure if/how we can get this in a declarative security attribute... */
				g_string_append (res, "field ");
				break;
			case MONO_DECLSEC_PROPERTY:
				g_string_append (res, "property ");
				break;
			default:
				g_warning ("TODO %d - please fill a bug report on this!", type);
				break;
			}

			type = *p++;

			if (type == MONO_DECLSEC_ENUM) {
				s = declsec_20_get_classname (p, &p);
				len = mono_metadata_decode_value (p, &p);
				g_string_append_printf (res, "enum %s '%.*s' = ", s, len, p);
				g_free (s);
				p += len;
				/* TODO: we must detect the size of the enum element (from the type ? length ?)
				 * note: ildasm v2 has some problem decoding them too and doesn't
				 * seems to rely on the type (as the other assembly isn't loaded) */
				g_string_append_printf (res, "int32(%d)", read32 (p));
				p += 4;
			} else {
				int arraytype = 0;
				if (type == MONO_TYPE_SZARRAY) {
					arraytype = *p++;
					declsec_20_write_type (res, arraytype);
				}
				declsec_20_write_type (res, type);

				len = mono_metadata_decode_value (p, &p);
				g_string_append_printf (res, " '%.*s' = ", len, p);
				p += len;

				if (type == MONO_TYPE_SZARRAY) {
					type = arraytype;
					declsec_20_write_type (res, type);
					elem = read32 (p);
					p += 4;
					g_string_append_printf (res, "[%d]", elem);
				} else {
					declsec_20_write_type (res, type);
					elem = 1;
				}
				g_string_append (res, "(");

				/* write value - or each element in the array */
				for (k = 0; k < elem; k++) {
					p = declsec_20_write_value (res, type, p);
					/* separate array elements */
					if (k < elem - 1)
						g_string_append (res, " ");
				}
				
				if (j < pos - 1)
					g_string_append_printf (res, ")\n%s", indent);
				else
					g_string_append (res, ")");
			}

		}

		if (i < num - 1)
			g_string_append_printf (res, "},\n%s", indent);
		else
			g_string_append (res, "}");

		if (param_len > 0)
			p = param_start + param_len;
	}
	g_string_append (res, "}");

	result = res->str;
	g_string_free (res, FALSE);
	return result;
}
