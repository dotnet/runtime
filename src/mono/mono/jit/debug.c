#include <stdlib.h>
#include <string.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/jit/codegen.h>

typedef struct {
	FILE *f;
	char *filename;
	char *name;
	int *mlines;
	int nmethods;
	int next_idx;
} AssemblyDebugInfo;

struct _MonoDebugHandle {
	char *name;
	GList *info;
};

#include "debug.h"

typedef struct {
	char *name;
	char *spec;
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

static void
output_std_stuff (AssemblyDebugInfo* debug)
{
	int i;
	for (i = 0; base_types [i].name; ++i) {
		if (! base_types [i].spec)
			continue;
		fprintf (debug->f, ".stabs \"%s:t(0,%d)=", base_types [i].name, i);
		if (base_types [i].spec [0] == ';') {
			fprintf (debug->f, "r(0,%d)%s\"", i, base_types [i].spec);
		} else {
			fprintf (debug->f, "%s\"", base_types [i].spec);
		}
		fprintf (debug->f, ",128,0,0,0\n");
	}
}

MonoDebugHandle*
mono_debug_open_file (char *filename)
{
	MonoDebugHandle *debug;
	
	debug = g_new0 (MonoDebugHandle, 1);
	debug->name = g_strdup (filename);
	return debug;
}

static void
debug_load_method_lines (AssemblyDebugInfo* info)
{
	FILE *f;
	char buf [1024];
	int i, mnum;
	char *name = g_strdup_printf ("%s.il", info->name);

	/* use an env var with directories for searching. */
	if (!(f = fopen (name, "r"))) {
		g_warning ("cannot open IL assembly file %s", name);
		g_free (name);
		return;
	}
	g_free (name);
	i = 0;
	while (fgets (buf, sizeof (buf), f)) {
		i++;
		if (sscanf (buf, " // method line %d", &mnum) && mnum < info->nmethods) {
			while (fgets (buf, sizeof (buf), f)) {
				++i;
				if (strstr (buf, "}"))
					break; /* internalcall or runtime method */
				if (strstr (buf, "IL_0000:"))
					break;
			}
			/* g_print ("method %d found at %d\n", mnum, i); */
			info->mlines [mnum] = i;
		}
	}
	fclose (f);
}

static AssemblyDebugInfo*
mono_debug_open_ass (MonoDebugHandle* handle, MonoImage *image)
{
	GList *tmp;
	AssemblyDebugInfo* info;

	for (tmp = handle->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;
		if (strcmp (info->name, image->assembly_name) == 0)
			return info;
	}
	info = g_new0 (AssemblyDebugInfo, 1);
	info->filename = g_strdup_printf ("%s-stabs.s", image->assembly_name);
	if (!(info->f = fopen (info->filename, "w"))) {
		g_free (info->filename);
		g_free (info);
		return NULL;
	}
	info->name = g_strdup (image->assembly_name);

	fprintf (info->f, ".stabs \"%s.il\",100,0,0,0\n", image->assembly_name);
	output_std_stuff (info);
	info->next_idx = 100;
	handle->info = g_list_prepend (handle->info, info);

	info->nmethods = image->tables [MONO_TABLE_METHOD].rows + 1;
	info->mlines = g_new0 (int, info->nmethods);
	debug_load_method_lines (info);
	return info;
}

void
mono_debug_make_symbols (MonoDebugHandle* debug)
{
	GList *tmp;
	char *buf;
	AssemblyDebugInfo* info;

	for (tmp = debug->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;
		/* yes, it's completely unsafe */
		buf = g_strdup_printf ("as %s -o /tmp/%s.o", info->filename, info->name);
		fflush (info->f);
		system (buf);
		g_free (buf);
	}
}

static void
mono_debug_close_ass (AssemblyDebugInfo* debug)
{
	fclose (debug->f);
	g_free (debug->mlines);
	g_free (debug->name);
	g_free (debug->filename);
	g_free (debug);
}

void
mono_debug_close (MonoDebugHandle* debug)
{
	GList *tmp;
	AssemblyDebugInfo* info;

	for (tmp = debug->info; tmp; tmp = tmp->next) {
		info = (AssemblyDebugInfo*)tmp->data;
		mono_debug_close_ass (info);
	}
	g_free (debug->name);
	g_free (debug);
}

void
mono_debug_add_method (MonoDebugHandle* debug, MonoFlowGraph *cfg)
{
	char *name;
	int line = 0;
	int i;
	MonoMethod *method = cfg->method;
	MonoClass *klass = method->klass;
	MonoMethodSignature *sig = method->signature;
	char **names = g_new (char*, sig->param_count);
	AssemblyDebugInfo* info = mono_debug_open_ass (debug, klass->image);

	/* FIXME: we should mangle the name better */
	name = g_strdup_printf ("%s%s%s__%s_%p", klass->name_space, klass->name_space [0]? "_": "",
			klass->name, method->name, method);

	for (i = 0; name [i]; ++i)
		if (name [i] == '.') name [i] = '_';

	mono_class_init (klass);
	/*
	 * Find the method index in the image.
	 */
	for (i = 0; klass->methods && i < klass->method.count; ++i) {
		if (klass->methods [i] == method) {
			line = info->mlines [klass->method.first + i + 1];
			/*g_print ("method %d found at line %d\n", klass->method.first + i + 1, line);*/
			break;
		}
	}

	/*
	 * We need to output all the basic info, if we change filename...
	 * fprintf (info->f, ".stabs \"%s.il\",100,0,0,0\n", klass->image->assembly_name);
	 */
	fprintf (info->f, ".stabs \"%s:F(0,%d)\",36,0,%d,%p\n", name, sig->ret->type, line, cfg->start);

	/* params */
	mono_method_get_param_names (cfg->method, (const char **)names);
	if (sig->hasthis)
		fprintf (info->f, ".stabs \"this:p(0,%d)=(0,%d)\",160,0,%d,%d\n", info->next_idx++, klass->byval_arg.type, line, 8); /* FIXME */
	for (i = 0; i < sig->param_count; ++i) {
		int stack_offset = g_array_index (cfg->varinfo, MonoVarInfo, cfg->args_start_index + i + sig->hasthis).offset;
		fprintf (info->f, ".stabs \"%s:p(0,%d)=(0,%d)\",160,0,%d,%d\n", names [i], info->next_idx++, sig->params [i]->type, line, stack_offset);
	}
	/* local vars */
	if (!method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethodHeader *header = ((MonoMethodNormal*)method)->header;
		for (i = 0; i < header->num_locals; ++i) {
			int stack_offset = g_array_index (cfg->varinfo, MonoVarInfo, cfg->locals_start_index + i).offset;
			fprintf (info->f, ".stabs \"local_%d:(0,%d)=(0,%d)\",128,0,%d,%d\n", i, info->next_idx++, header->locals [i]->type, line, stack_offset);
		}
	}
	/* start lines of basic blocks */
	for (i = 0; i < cfg->block_count; ++i) {
		int j;
		for (j = 0; j < cfg->bblocks [i].forest->len; ++j) {
			MBTree *t = (MBTree *) g_ptr_array_index (cfg->bblocks [i].forest, j);
			fprintf (info->f, ".stabn 68,0,%d,%d\n", line + t->cli_addr, t->addr);
		}
	}

	/* end of function */
	fprintf (info->f, ".stabs \"\",36,0,0,%d\n", cfg->code - cfg->start);
	g_free (name);
	g_free (names);
	fflush (info->f);
}

static void
get_enumvalue (MonoClass *klass, int index, char *buf)
{
	guint32 const_cols [MONO_CONSTANT_SIZE];
	const char *ptr;
	guint32 crow = mono_metadata_get_constant_index (klass->image, MONO_TOKEN_FIELD_DEF | (index + 1));

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

void
mono_debug_add_type (MonoDebugHandle* debug, MonoClass *klass)
{
	char *name;
	int i;
	char buf [64];
	AssemblyDebugInfo* info = mono_debug_open_ass (debug, klass->image);

	mono_class_init (klass);

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

