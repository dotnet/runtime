#include <stdlib.h>
#include <string.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/jit/codegen.h>
#include <mono/jit/debug.h>

#include "debug-private.h"

typedef struct {
	const char *name;
	const char *spec;
} BaseTypes;

/*
 * Not 64 bit clean.
 * Note: same order of MonoTypeEnum.
 */
static BaseTypes
base_types[] = {
	{"", NULL},
	{"Void", "(0,1)"},
	{"Boolean", ";0;255;"},
	{"Char", ";0;65535;"},
	{"SByte", ";-128;127;"},
	{"Byte", ";0;255;"},
	{"Int16", ";-32768;32767;"},
	{"UInt16", ";0;65535;"},
	{"Int32", ";0020000000000;0017777777777;"},
	{"UInt32", ";0000000000000;0037777777777;"},
	{"Int64", ";01000000000000000000000;0777777777777777777777;"},
	{"UInt64", ";0000000000000;01777777777777777777777;"},
	{"Single", "r(0,8);4;0;"},
	{"Double", "r(0,8);8;0;"},
	{"String", "(0,41)=*(0,42)=xsMonoString:"}, /*string*/
	{"", }, /*ptr*/
	{"", }, /*byref*/
	{"", }, /*valuetype*/
	{"Class", "(0,44)=*(0,45)=xsMonoObject:"}, /*class*/
	{"", }, /*unused*/
	{"Array", }, /*array*/
	{"", }, /*typedbyref*/
	{"", }, /*unused*/
	{"", }, /*unused*/
	{"IntPtr", ";0020000000000;0017777777777;"},
	{"UIntPtr", ";0000000000000;0037777777777;"},
	{"", }, /*unused*/
	{"FnPtr", "*(0,1)"}, /*fnptr*/
	{"Object", "(0,47)=*(0,48)=xsMonoObject:"}, /*object*/
	{"SzArray", "(0,50)=*(0,51))=xsMonoArray:"}, /*szarray*/
	{NULL, NULL}
};

void
mono_debug_open_assembly_stabs (AssemblyDebugInfo* info)
{
}

void
mono_debug_close_assembly_stabs (AssemblyDebugInfo* info)
{
}

static void
write_method_stabs (AssemblyDebugInfo *info, DebugMethodInfo *minfo)
{
	int i;
	MonoMethod *method = minfo->method_info.method;
	MonoClass *klass = method->klass;
	MonoMethodSignature *sig = method->signature;
	char **names = g_new (char*, sig->param_count);

	fprintf (info->f, ".stabs \"%s:F(0,%d)\",36,0,%d,%p\n", minfo->name, sig->ret->type,
		 minfo->start_line, minfo->method_info.code_start);

	/* params */
	mono_method_get_param_names (method, (const char **)names);
	if (sig->hasthis)
		fprintf (info->f, ".stabs \"this:p(0,%d)=(0,%d)\",160,0,%d,%d\n",
			 info->next_idx++, klass->byval_arg.type, minfo->start_line,
			 minfo->method_info.this_var->offset);
	for (i = 0; i < minfo->method_info.num_params; i++) {
		int stack_offset = minfo->method_info.params [i].offset;

		fprintf (info->f, ".stabs \"%s:p(0,%d)=(0,%d)\",160,0,%d,%d\n",
			 names [i], info->next_idx++, sig->params [i]->type,
			 minfo->start_line, stack_offset);
	}

	/* local vars */
	for (i = 0; i < minfo->method_info.num_locals; ++i) {
		MonoMethodHeader *header = ((MonoMethodNormal*)method)->header;
		int stack_offset = minfo->method_info.locals [i].offset;

		fprintf (info->f, ".stabs \"local_%d:(0,%d)=(0,%d)\",128,0,%d,%d\n",
			 i, info->next_idx++, header->locals [i]->type, minfo->start_line, stack_offset);
	}

	if (minfo->line_numbers) {
		fprintf (info->f, ".stabn 68,0,%d,%d\n", minfo->start_line, 0);
		fprintf (info->f, ".stabn 68,0,%d,%d\n", minfo->first_line, minfo->method_info.prologue_end);

		for (i = 1; i < minfo->line_numbers->len; i++) {
			DebugLineNumberInfo *lni = g_ptr_array_index (minfo->line_numbers, i);

			fprintf (info->f, ".stabn 68,0,%d,%d\n", lni->line,
				 (char *)lni->address - minfo->method_info.code_start);
		}

		fprintf (info->f, ".stabn 68,0,%d,%d\n", minfo->last_line, minfo->method_info.epilogue_begin);
	}

	/* end of function */
	fprintf (info->f, ".stabs \"\",36,0,0,%d\n", minfo->method_info.code_size);

	g_free (names);
	fflush (info->f);
}

static void
get_enumvalue (MonoClass *klass, int idx, char *buf)
{
	guint32 const_cols [MONO_CONSTANT_SIZE];
	const char *ptr;
	guint32 crow = mono_metadata_get_constant_index (klass->image, MONO_TOKEN_FIELD_DEF | (idx + 1));

	if (!crow) {
		buf [0] = '0';
		buf [1] = 0;
		return;
	}
	mono_metadata_decode_row (&klass->image->tables [MONO_TABLE_CONSTANT], crow-1, const_cols, MONO_CONSTANT_SIZE);
	ptr = mono_metadata_blob_heap (klass->image, const_cols [MONO_CONSTANT_VALUE]);
	switch (const_cols [MONO_CONSTANT_TYPE]) {
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		/* FIXME: add other types... */
	default:
		g_snprintf (buf, 64, "%d", *(gint32*)ptr);
	}
}

static void
write_method_func (gpointer key, gpointer value, gpointer user_data)
{
	write_method_stabs (user_data, value);
}

static void
write_class_stabs (AssemblyDebugInfo *info, MonoClass *klass, int idx)
{
	char *name;
	int i;
	char buf [64];

	/* output enums ...*/
	if (klass->enumtype) {
		name = g_strdup_printf ("%s%s%s", klass->name_space, klass->name_space [0]? "_": "", klass->name);
		fprintf (info->f, ".stabs \"%s:T%d=e", name, ++info->next_idx);
		g_free (name);
		for (i = 0; i < klass->field.count; ++i) {
			if (klass->fields [i].type->attrs & FIELD_ATTRIBUTE_LITERAL) {
				get_enumvalue (klass, klass->field.first + i, buf);
				fprintf (info->f, "%s_%s=%s,", klass->name, klass->fields [i].name, buf);
			}
		}
		fprintf (info->f, ";\",128,0,0,0\n");
	}
	fflush (info->f);
}

static void
write_class (gpointer key, gpointer value, gpointer user_data)
{
	write_class_stabs (user_data, key, GPOINTER_TO_INT (value));
}

void
mono_debug_write_assembly_stabs (AssemblyDebugInfo* info)
{
	char *buf;
	int i;

	if (!(info->f = fopen (info->filename, "w")))
		return;

	fprintf (info->f, ".stabs \"%s.il\",100,0,0,0\n", info->name);

	for (i = 0; base_types [i].name; ++i) {
		if (! base_types [i].spec)
			continue;
		fprintf (info->f, ".stabs \"%s:t(0,%d)=", base_types [i].name, i);
		if (base_types [i].spec [0] == ';') {
			fprintf (info->f, "r(0,%d)%s\"", i, base_types [i].spec);
		} else {
			fprintf (info->f, "%s\"", base_types [i].spec);
		}
		fprintf (info->f, ",128,0,0,0\n");
	}

	g_hash_table_foreach (info->methods, write_method_func, info);

	g_hash_table_foreach (info->type_hash, write_class, info);

	fclose (info->f);
	info->f = NULL;

	/* yes, it's completely unsafe */
	buf = g_strdup_printf ("as %s -o /tmp/%s.o", info->filename, info->name);
	system (buf);
	g_free (buf);
}
