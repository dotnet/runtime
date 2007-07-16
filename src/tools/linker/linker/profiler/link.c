/*
 * link.c: a profiler to help the static linker
 *
 * Authors:
 *   Jb Evain (jbevain@novell.com)
 *
 * (C) 2007 Novell, Inc. http://www.novell.com
 *
 */
#include <glib.h>
#include <string.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class.h>
#include <mono/metadata/image.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/profiler.h>

struct _MonoProfiler {
	const char *output_file;
	GHashTable *images;
};

typedef struct _LinkedImage {
	MonoImage *image;
	GHashTable *types;
} LinkedImage;

typedef struct _LinkedType {
	MonoClass *klass;
	GHashTable *methods;
} LinkedType;

typedef struct _LinkedMethod {
	MonoMethod *method;
} LinkedMethod;

static void
link_append_class_name (GString *res, MonoClass *klass, gboolean include_namespace)
{
	if (!klass) {
		g_string_append (res, "**unknown**");
		return;
	}

	if (mono_class_get_nesting_type (klass)) {
		link_append_class_name (res, mono_class_get_nesting_type (klass), include_namespace);
		g_string_append_c (res, '/');
	}

	if (include_namespace && *(mono_class_get_namespace (klass)))
		g_string_sprintfa (res, "%s.", mono_class_get_namespace (klass));

	g_string_sprintfa (res, "%s", mono_class_get_name (klass));
}

static MonoType *
link_get_element_type (MonoType *type)
{
	return mono_class_get_type (mono_class_get_element_class (mono_class_from_mono_type (type)));
}

static void
link_type_get_desc (GString *res, MonoType *type, gboolean include_namespace) {
	switch (mono_type_get_type (type)) {
	case MONO_TYPE_VOID:
		g_string_append (res, "System.Void"); break;
	case MONO_TYPE_CHAR:
		g_string_append (res, "System.Char"); break;
	case MONO_TYPE_BOOLEAN:
		g_string_append (res, "System.Boolean"); break;
	case MONO_TYPE_U1:
		g_string_append (res, "System.Byte"); break;
	case MONO_TYPE_I1:
		g_string_append (res, "System.SByte"); break;
	case MONO_TYPE_U2:
		g_string_append (res, "System.UInt16"); break;
	case MONO_TYPE_I2:
		g_string_append (res, "System.Int16"); break;
	case MONO_TYPE_U4:
		g_string_append (res, "System.UInt32"); break;
	case MONO_TYPE_I4:
		g_string_append (res, "System.Int32"); break;
	case MONO_TYPE_U8:
		g_string_append (res, "System.UInt64"); break;
	case MONO_TYPE_I8:
		g_string_append (res, "System.Int64"); break;
	case MONO_TYPE_FNPTR:
		g_string_append (res, "*()"); break;
	case MONO_TYPE_U:
		g_string_append (res, "System.UIntPtr"); break;
	case MONO_TYPE_I:
		g_string_append (res, "System.IntPtr"); break;
	case MONO_TYPE_R4:
		g_string_append (res, "System.Single"); break;
	case MONO_TYPE_R8:
		g_string_append (res, "System.Double"); break;
	case MONO_TYPE_STRING:
		g_string_append (res, "System.String"); break;
	case MONO_TYPE_OBJECT:
		g_string_append (res, "System.Object"); break;
	case MONO_TYPE_PTR:
		link_type_get_desc (res, mono_type_get_ptr_type (type), include_namespace);
		g_string_append_c (res, '*');
		break;
	case MONO_TYPE_ARRAY: {
		MonoClass *eklass = mono_class_get_element_class (mono_class_from_mono_type (type));
		link_type_get_desc (res, mono_class_get_type (eklass), include_namespace);
		g_string_sprintfa (res, "[%d]", mono_class_get_rank (eklass));
		break;
	}
	case MONO_TYPE_SZARRAY:
		link_type_get_desc (res, link_get_element_type (type), include_namespace);
		g_string_append (res, "[]");
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		link_append_class_name (res, mono_type_get_class (type), include_namespace);
		break;
	case MONO_TYPE_GENERICINST:
		//link_type_get_desc (res, &type->data.generic_class->container_class->byval_arg, include_namespace);  /* ? */
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		//g_string_append (res, type->data.generic_param->name);  /* ? */
		break;
	default:
		break;
	}
	if (mono_type_is_byref (type))
		g_string_append (res, "&amp;");
}

static char *
link_type_full_name (MonoType *type)
{
	GString *str;
	char *res;

	str = g_string_new ("");
	link_type_get_desc (str, type, TRUE);

	res = g_strdup (str->str);
	g_string_free (str, TRUE);
	return res;
}

static char *
link_class_full_name (MonoClass *klass)
{
	return link_type_full_name (mono_class_get_type (klass));
}

static char *
link_signature_get_desc (MonoMethodSignature *sig, gboolean include_namespace)
{
	int i;
	char *result;
	GString *res = g_string_new ("");

	for (i = 0; i < sig->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		link_type_get_desc (res, sig->params [i], include_namespace);
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

static char *
link_method_signature (MonoMethod *method)
{
	MonoMethodSignature *sig;
	char *res;

	sig = mono_method_signature (method);
	char *tmpsig = link_signature_get_desc (sig, TRUE);
	res = g_strdup_printf ("%s %s(%s)",
		link_type_full_name (mono_signature_get_return_type (sig)),
		mono_method_get_name (method), tmpsig);
	g_free (tmpsig);

	return res;
}

static char *
link_image_fullname (MonoImage *image)
{
	MonoAssemblyName *name;
	char *res;

	name = g_new0 (MonoAssemblyName, 1);
	mono_assembly_fill_assembly_name (image, name);
	res = mono_stringify_assembly_name (name);
	g_free (name);
	return res;
}

static LinkedType *
link_get_linked_type (LinkedImage *limage, MonoClass *klass)
{
	LinkedType *ltype;

	ltype = (LinkedType *) g_hash_table_lookup (limage->types, klass);

	if (ltype)
		return ltype;

	ltype = g_new0 (LinkedType, 1);
	ltype->klass = klass;
	ltype->methods = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (limage->types, klass, ltype);
	return ltype;
}

static LinkedImage *
link_get_linked_image (MonoProfiler *prof, MonoImage *image)
{
	LinkedImage *limage;

	limage = (LinkedImage *) g_hash_table_lookup (prof->images, image);

	if (limage)
		return limage;

	limage = g_new0 (LinkedImage, 1);
	limage->image = image;
	limage->types = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (prof->images, image, limage);
	return limage;
}

static void
link_method_leave (MonoProfiler *prof, MonoMethod *method)
{
	MonoClass *klass;
	MonoImage *image;

	LinkedType *ltype;
	LinkedImage *limage;
	LinkedMethod *lmethod;

	klass = mono_method_get_class (method);
	image = mono_class_get_image (klass);

	limage = link_get_linked_image (prof, image);
	ltype = link_get_linked_type (limage, klass);

	lmethod = (LinkedMethod *) g_hash_table_lookup (ltype->methods, method);
	if (lmethod)
		return;

	lmethod = g_new0 (LinkedMethod, 1);
	lmethod->method = method;
	g_hash_table_insert (ltype->methods, method, lmethod);
}

static void
link_free_member (gpointer key, gpointer value, gpointer data)
{
	g_free (value);
}

static void
link_free_type (gpointer key, gpointer value, gpointer data)
{
	LinkedType *type = (LinkedType *) value;

	g_hash_table_foreach (type->methods, link_free_member, NULL);
	g_free (type);
}

static void
link_free_image (gpointer key, gpointer value, gpointer data)
{
	LinkedImage *image = (LinkedImage *) value;

	g_hash_table_foreach (image->types, link_free_type, NULL);
	g_free (image);
}

static void
link_print_method (gpointer key, gpointer value, gpointer data)
{
	LinkedMethod *lmethod = (LinkedMethod *) value;
	FILE *output = (FILE *) data;
	char *signature;

	signature = link_method_signature (lmethod->method);
	fprintf (output, "\t\t\t<method signature=\"%s\" />\n", signature);
	g_free (signature);
}

static void
link_print_type (gpointer key, gpointer value, gpointer data)
{
	LinkedType *ltype = (LinkedType *) value;
	FILE *output = (FILE *) data;
	char *fullname;

	fullname = link_class_full_name (ltype->klass);
	fprintf (output, "\t\t<type fullname=\"%s\">\n", fullname);
	g_free (fullname);

	g_hash_table_foreach (ltype->methods, link_print_method, output);
	fprintf (output, "\t\t</type>\n");
}

static void
link_print_image (gpointer key, gpointer value, gpointer data)
{
	LinkedImage *limage = (LinkedImage *) value;
	FILE *output = (FILE *) data;
	char *fullname;

	fullname = link_image_fullname (limage->image);
	fprintf (output, "\t<assembly fullname=\"%s\">\n", fullname);
	g_free (fullname);
	g_hash_table_foreach (limage->types, link_print_type, output);
	fprintf (output, "\t</assembly>\n");
}

static void
link_print_tree (MonoProfiler *prof)
{
	FILE *output;

	output = fopen (prof->output_file, "w");
	fprintf (output, "<linker>\n");
	g_hash_table_foreach (prof->images, link_print_image, output);
	fprintf (output, "</linker>\n");
	fclose (output);
}

static void
link_shutdown (MonoProfiler *prof)
{
	link_print_tree (prof);
	g_hash_table_foreach (prof->images, link_free_image, NULL);
	g_free (prof);
}

void
mono_profiler_startup (const char *desc)
{
	MonoProfiler *prof;

	prof = g_new0 (MonoProfiler, 1);

	if (strncmp ("link:", desc, 5) == 0 && desc [5])
		prof->output_file = g_strdup (desc + 5);
	else
		prof->output_file = "link.xml";

	prof->images = g_hash_table_new (NULL, NULL);

	mono_profiler_install (prof, link_shutdown);

	mono_profiler_install_enter_leave (NULL, link_method_leave);

	mono_profiler_set_events (MONO_PROFILE_ENTER_LEAVE);
}
