/*
 * aot.c: mono Ahead of Time compiler
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "config.h"
#include <sys/types.h>
#include <unistd.h>
#include <fcntl.h>
#include <string.h>
#ifndef PLATFORM_WIN32
#include <sys/mman.h>
#else
#include <windows.h>
#endif

#include <limits.h>    /* for PAGESIZE */
#ifndef PAGESIZE
#define PAGESIZE 4096
#endif

#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/marshal.h>
#include <mono/os/gc_wrapper.h>

#include "mini.h"

#ifdef PLATFORM_WIN32
#define SHARED_EXT ".dll"
#else
#define SHARED_EXT ".so"
#endif

#if defined(sparc)
#define AS_STRING_DIRECTIVE ".asciz"
#else
/* GNU as */
#define AS_STRING_DIRECTIVE ".string"
#endif

typedef struct MonoAotMethod {
	MonoJitInfo *info;
	MonoJumpInfo *patch_info;
	MonoDomain *domain;
} MonoAotMethod;

typedef struct MonoAotModule {
	/* Optimization flags used to compile the module */
	guint32 opts;
	/* Maps MonoMethods to MonoAotMethodInfos */
	MonoGHashTable *methods;
	char **icall_table;
	MonoImage **image_table;
	guint32* methods_present_table;
} MonoAotModule;

typedef struct MonoAotCompile {
	FILE *fp;
	GHashTable *ref_hash;
	GHashTable *icall_hash;
	GPtrArray *icall_table;
	GHashTable *image_hash;
	GPtrArray *image_table;
} MonoAotCompile;

static MonoGHashTable *aot_modules;

static CRITICAL_SECTION aot_mutex;

static guint32 mono_aot_verbose = 1;

/*
 * Disabling this will make a copy of the loaded code and use the copy instead 
 * of the original. This will place the caller and the callee close to each 
 * other in memory, possibly improving cache behavior. Since the original
 * code is in copy-on-write memory, this will not increase the memory usage
 * of the runtime.
 */
static gboolean use_loaded_code = FALSE;

/* For debugging */
static gint32 mono_last_aot_method = -1;

static MonoClass * 
decode_class_info (MonoAotModule *module, gpointer *data)
{
	MonoImage *image;
	MonoClass *klass;
	
	image = module->image_table [(guint32)data [1]];
	g_assert (image);

	if (data [0]) {
		return mono_class_get (image, (guint32)data [0]);
	} else {
		klass = decode_class_info (module, data [3]);
		return mono_array_class_get (klass, (guint32)data [2]);
	}

	return NULL;
}

static void
load_aot_module (MonoAssembly *assembly, gpointer user_data)
{
	char *aot_name;
	MonoAotModule *info;
	gboolean usable = TRUE;
	char *saved_guid = NULL;
	char *aot_version = NULL;
	char *opt_flags = NULL;

	aot_name = g_strdup_printf ("%s.so", assembly->image->name);

	assembly->aot_module = g_module_open (aot_name, G_MODULE_BIND_LAZY);

	if (!assembly->aot_module)
		return;

	g_module_symbol (assembly->aot_module, "mono_assembly_guid", (gpointer *) &saved_guid);
	g_module_symbol (assembly->aot_module, "mono_aot_version", (gpointer *) &aot_version);
	g_module_symbol (assembly->aot_module, "mono_aot_opt_flags", (gpointer *)&opt_flags);

	if (!aot_version || strcmp (aot_version, MONO_AOT_FILE_VERSION)) {
		if (mono_aot_verbose > 0)
			printf ("AOT module %s has wrong file format version (expected %s got %s)\n", aot_name, MONO_AOT_FILE_VERSION, aot_version);
		usable = FALSE;
	}
	else
		if (!saved_guid || strcmp (assembly->image->guid, saved_guid)) {
			if (mono_aot_verbose > 0)
				printf ("AOT module %s is out of date.\n", aot_name);
			usable = FALSE;
		}

	if (!usable) {
		g_free (aot_name);
		g_module_close (assembly->aot_module);
		assembly->aot_module = NULL;
		return;
	}

	/*
	 * It seems that MonoGHashTables are in the GC heap, so structures
	 * containing them must be in the GC heap as well :(
	 */
#ifdef HAVE_BOEHM_GC
	info = GC_MALLOC (sizeof (MonoAotModule));
#else
	info = g_new0 (MonoAotModule, 1);
#endif
	info->methods = mono_g_hash_table_new (NULL, NULL);
	sscanf (opt_flags, "%d", &info->opts);

	/* Read image table */
	{
		guint32 table_len, i;
		char *table = NULL;

		g_module_symbol (assembly->aot_module, "mono_image_table", (gpointer *)&table);
		g_assert (table);

		table_len = *(guint32*)table;
		table += sizeof (guint32);
		info->image_table = g_new0 (MonoImage*, table_len);
		for (i = 0; i < table_len; ++i) {
			info->image_table [i] = mono_image_loaded_by_guid (table);
			if (!info->image_table [i]) {
				if (mono_aot_verbose > 0)
					printf ("AOT module %s is out of date.\n", aot_name);
				mono_g_hash_table_destroy (info->methods);
				g_free (info->image_table);
#ifndef HAVE_BOEHM_GC
				g_free (info);
#endif
				g_free (aot_name);
				g_module_close (assembly->aot_module);
				assembly->aot_module = NULL;
				return;
			}
			table += strlen (table) + 1;
		}
	}

	/* Read icall table */
	{
		guint32 table_len, i;
		char *table = NULL;

		g_module_symbol (assembly->aot_module, "mono_icall_table", (gpointer *)&table);
		g_assert (table);

		table_len = *(guint32*)table;
		table += sizeof (guint32);
		info->icall_table = g_new0 (char*, table_len);
		for (i = 0; i < table_len; ++i) {
			info->icall_table [i] = table;
			table += strlen (table) + 1;
		}
	}

	/* Read methods present table */
	g_module_symbol (assembly->aot_module, "mono_methods_present_table", (gpointer *)&info->methods_present_table);
	g_assert (info->methods_present_table);

	EnterCriticalSection (&aot_mutex);
	mono_g_hash_table_insert (aot_modules, assembly, info);
	LeaveCriticalSection (&aot_mutex);

	if (mono_aot_verbose > 0)
		printf ("Loaded AOT Module for %s.\n", assembly->image->name);
}

void
mono_aot_init (void)
{
	InitializeCriticalSection (&aot_mutex);

	aot_modules = mono_g_hash_table_new (NULL, NULL);

	mono_install_assembly_load_hook (load_aot_module, NULL);

	if (getenv ("MONO_LASTAOT"))
		mono_last_aot_method = atoi (getenv ("MONO_LASTAOT"));
}
 
static MonoJitInfo *
mono_aot_get_method_inner (MonoDomain *domain, MonoMethod *method)
{
	MonoClass *klass = method->klass;
	MonoAssembly *ass = klass->image->assembly;
	MonoJumpInfo *patch_info = NULL;
	GModule *module = ass->aot_module;
	char method_label [256];
	char info_label [256];
	guint8 *code = NULL;
	gpointer *info;
	guint code_len, used_int_regs, used_strings;
	MonoAotModule *aot_module;
	MonoAotMethod *minfo;
	MonoJitInfo *jinfo;
	MonoMethodHeader *header = ((MonoMethodNormal*)method)->header;
	int i;

	if (!module)
		return NULL;

	if (!method->token)
		return NULL;

	if (mono_profiler_get_events () & MONO_PROFILE_ENTER_LEAVE)
		return NULL;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return NULL;

	aot_module = (MonoAotModule*)mono_g_hash_table_lookup (aot_modules, ass);

	g_assert (klass->inited);

	minfo = mono_g_hash_table_lookup (aot_module->methods, method);
	/* Can't use code from non-root domains since they can be unloaded */
	if (minfo && (minfo->domain == mono_root_domain)) {
		/* This method was already loaded in another appdomain */

		/* Duplicate jinfo */
		jinfo = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));
		memcpy (jinfo, minfo->info, sizeof (MonoJitInfo));
		if (jinfo->clauses) {
			jinfo->clauses = 
				mono_mempool_alloc0 (domain->mp, sizeof (MonoJitExceptionInfo) * header->num_clauses);
			memcpy (jinfo->clauses, minfo->info->clauses, sizeof (MonoJitExceptionInfo) * header->num_clauses);
		}

		if (aot_module->opts & MONO_OPT_SHARED)
			/* Use the same method in the new appdomain */
			;
		else if (!minfo->patch_info)
			/* Use the same method in the new appdomain */
			;			
		else {
			/* Create a copy of the original method and apply relocations */

			code = mono_code_manager_reserve (domain->code_mp, minfo->info->code_size);
			memcpy (code, minfo->info->code_start, minfo->info->code_size);

			if (mono_aot_verbose > 1)
				printf ("REUSE METHOD: %s %p - %p.\n", mono_method_full_name (method, TRUE), code, (char*)code + minfo->info->code_size);

			/* Do this outside the lock to avoid deadlocks */
			LeaveCriticalSection (&aot_mutex);
			mono_arch_patch_code (method, domain, code, minfo->patch_info, TRUE);
			EnterCriticalSection (&aot_mutex);
			mono_arch_flush_icache (code, minfo->info->code_size);

			/* Relocate jinfo */
			jinfo->code_start = code;
			if (jinfo->clauses) {
				for (i = 0; i < header->num_clauses; ++i) {
					MonoJitExceptionInfo *ei = &jinfo->clauses [i];
					gint32 offset = code - (guint8*)minfo->info->code_start;

					if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
						ei->data.filter = (guint8*)ei->data.filter + offset;
					ei->try_start = (guint8*)ei->try_start + offset;
					ei->try_end = (guint8*)ei->try_end + offset;
					ei->handler_start = (guint8*)ei->handler_start + offset;
				}
			}
		}

		return jinfo;
	}

	/* Do a fast check to see whenever the method exists */
	{
		guint32 index = mono_metadata_token_index (method->token) - 1;
		guint32 w;
		w = aot_module->methods_present_table [index / 32];
		if (! (w & (1 << (index % 32)))) {
			if (mono_aot_verbose > 1)
				printf ("NOT FOUND: %s.\n", mono_method_full_name (method, TRUE));
			return NULL;
		}
	}

	sprintf (method_label, "m_%x", mono_metadata_token_index (method->token));

	if (!g_module_symbol (module, method_label, (gpointer *)&code))
		return NULL;

	sprintf (info_label, "%s_p", method_label);

	if (!g_module_symbol (module, info_label, (gpointer *)&info))
		return NULL;

	if (mono_last_aot_method != -1) {
		if (mono_jit_stats.methods_aot > mono_last_aot_method)
				return NULL;
		else
			if (mono_jit_stats.methods_aot == mono_last_aot_method)
				printf ("LAST AOT METHOD: %s.%s.%s.\n", klass->name_space, klass->name, method->name);
	}

#ifdef HAVE_BOEHM_GC
	minfo = GC_MALLOC (sizeof (MonoAotMethod));
#else
	minfo = g_new0 (MonoAotMethod, 1);
#endif

	minfo->domain = domain;
	jinfo = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));

	code_len = GPOINTER_TO_UINT (*((gpointer **)info));
	info++;
	used_int_regs = GPOINTER_TO_UINT (*((gpointer **)info));
	info++;

	if (!use_loaded_code) {
		guint8 *code2;
		code2 = mono_code_manager_reserve (domain->code_mp, code_len);
		memcpy (code2, code, code_len);
		mono_arch_flush_icache (code2, code_len);
		code = code2;
	}

	if (mono_aot_verbose > 1)
		printf ("FOUND AOT compiled code for %s %p - %p %p\n", mono_method_full_name (method, TRUE), code, code + code_len, info);

	/* Exception table */
	if (header->num_clauses) {
		jinfo->clauses = 
			mono_mempool_alloc0 (domain->mp, sizeof (MonoJitExceptionInfo) * header->num_clauses);
		jinfo->num_clauses = header->num_clauses;

		jinfo->exvar_offset = GPOINTER_TO_UINT (*((gpointer**)info));
		info ++;

		for (i = 0; i < header->num_clauses; ++i) {
			MonoExceptionClause *ec = &header->clauses [i];				
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];

			ei->flags = ec->flags;
			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				ei->data.filter = code + GPOINTER_TO_UINT (*((gpointer**)info));
			else
				ei->data.token = GPOINTER_TO_UINT (*((gpointer**)info));
			info ++;
			ei->try_start = code + GPOINTER_TO_UINT (*((gpointer**)info));
			info ++;
			ei->try_end = code + GPOINTER_TO_UINT (*((gpointer**)info));
			info ++;
			ei->handler_start = code + GPOINTER_TO_UINT (*((gpointer**)info));
			info ++;
		}
	}

	if (aot_module->opts & MONO_OPT_SHARED) {
		used_strings = GPOINTER_TO_UINT (*((gpointer **)info));
		info++;
	}
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint token =  GPOINTER_TO_UINT (*((gpointer **)info));
		info++;
		mono_ldstr (mono_root_domain, klass->image, mono_metadata_token_index (token));
	}

	if (*info) {
		MonoMemPool *mp;
		MonoImage *image;
		guint8 *page_start;
		gpointer *table;
		int pages;
		int i, err;
		guint32 last_offset, buf_len;

		if (aot_module->opts & MONO_OPT_SHARED)
			mp = mono_mempool_new ();
		else
			mp = domain->mp;

		last_offset = 0;
		while (*info) {
			MonoJumpInfo *ji = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));
			gpointer *data;

			guint8 b1, b2;

			b1 = *(guint8*)info;
			b2 = *((guint8*)info + 1);

			info = (gpointer*)((guint8*)info + 2);

#if defined(sparc)
			{
				guint32 ptr = (guint32)info;
				info = (gpointer)((ptr + 3) & ~3);
			}
#endif

			ji->type = b1 >> 2;

			if (((b1 & (1 + 2)) == 3) && (b2 == 255)) {
				ji->ip.i = GPOINTER_TO_UINT (*info);
				info ++;
			}
			else
				ji->ip.i = (((guint32)(b1 & (1 + 2))) << 8) + b2;

			ji->ip.i += last_offset;
			last_offset = ji->ip.i;
			//printf ("T: %d O: %d.\n", ji->type, ji->ip.i);

			data = *((gpointer **)info);

			switch (ji->type) {
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IID:
				ji->data.klass = decode_class_info (aot_module, data);
				g_assert (ji->data.klass);
				mono_class_init (ji->data.klass);
				break;
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_CLASS_INIT:
				ji->data.klass = decode_class_info (aot_module, data);
				g_assert (ji->data.klass);
				mono_class_init (ji->data.klass);
				break;
			case MONO_PATCH_INFO_IMAGE:
				ji->data.image = aot_module->image_table [(guint32)data];
				g_assert (ji->data.image);
				break;
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_METHOD_JUMP: {
				guint32 image_index, token;

				image_index = (guint32)data >> 24;
				token = MONO_TOKEN_METHOD_DEF | ((guint32)data & 0xffffff);

				image = aot_module->image_table [image_index];
				ji->data.method = mono_get_method (image, token, NULL);
				g_assert (ji->data.method);
				mono_class_init (ji->data.method->klass);

				break;
			}
			case MONO_PATCH_INFO_WRAPPER: {
				guint32 image_index, token;
				guint32 wrapper_type;

				wrapper_type = (guint32)data[0];
				image_index = (guint32)data[1] >> 24;
				token = MONO_TOKEN_METHOD_DEF | ((guint32)data[1] & 0xffffff);

				image = aot_module->image_table [image_index];
				ji->data.method = mono_get_method (image, token, NULL);
				g_assert (ji->data.method);
				mono_class_init (ji->data.method->klass);

				g_assert (wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK);
				ji->type = MONO_PATCH_INFO_METHOD;
				ji->data.method = mono_marshal_get_remoting_invoke_with_check (ji->data.method);
				break;
			}
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_SFLDA: {
				MonoClass *klass = decode_class_info (aot_module, data [1]);
				mono_class_init (klass);
				ji->data.field = mono_class_get_field (klass, (guint32)data [0]);
				break;
			}
			case MONO_PATCH_INFO_INTERNAL_METHOD:
				ji->data.name = aot_module->icall_table [(guint32)data];
				g_assert (ji->data.name);
				//printf ("A: %s.\n", ji->data.name);
				break;
			case MONO_PATCH_INFO_SWITCH:
				ji->table_size = (int)data [0];
				table = g_new (gpointer, ji->table_size);
				ji->data.target = table;
				for (i = 0; i < ji->table_size; i++) {
					table [i] = data [i + 1];
				}
				break;
			case MONO_PATCH_INFO_R4:
			case MONO_PATCH_INFO_R8:
				ji->data.target = data;
				break;
			case MONO_PATCH_INFO_LDSTR:
			case MONO_PATCH_INFO_LDTOKEN:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
				image = aot_module->image_table [(int)data [0]];
				ji->data.token = mono_jump_info_token_new (mp, image, (int)data [1]);
				break;
			case MONO_PATCH_INFO_EXC_NAME:
				ji->data.klass = decode_class_info (aot_module, data);
				g_assert (ji->data.klass);
				mono_class_init (ji->data.klass);
				ji->data.name = ji->data.klass->name;
				break;
			case MONO_PATCH_INFO_METHOD_REL:
				ji->data.offset = (int)data [0];
				break;
			default:
				g_warning ("unhandled type %d", ji->type);
				g_assert_not_reached ();
			}

			info++;
			ji->next = patch_info;
			patch_info = ji;
		}

		info = (gpointer)((guint8*)info + 4);
		buf_len = *(guint32*)info;
		info = (gpointer)((guint8*)info + 4);
		mono_debug_add_aot_method (domain, method, code, (guint8*)info, buf_len);

		if (use_loaded_code) {
		/* disable write protection */
#ifndef PLATFORM_WIN32
			page_start = (char *) (((int) (code)) & ~ (PAGESIZE - 1));
			pages = (code + code_len - page_start + PAGESIZE - 1) / PAGESIZE;
			err = mprotect (page_start, pages * PAGESIZE, PROT_READ | PROT_WRITE | PROT_EXEC);
			g_assert (err == 0);
#else
			{
				DWORD oldp;
				g_assert (VirtualProtect (code, code_len, PAGE_EXECUTE_READWRITE, &oldp) != 0);
			}
#endif
		}

		/* Do this outside the lock to avoid deadlocks */
		LeaveCriticalSection (&aot_mutex);
		mono_arch_patch_code (method, domain, code, patch_info, TRUE);
		EnterCriticalSection (&aot_mutex);

		if (aot_module->opts & MONO_OPT_SHARED)
			/* No need to cache patches */
			mono_mempool_destroy (mp);
		else
			minfo->patch_info = patch_info;
	}

	mono_jit_stats.methods_aot++;

	{
		jinfo->code_size = code_len;
		jinfo->used_regs = used_int_regs;
		jinfo->method = method;
		jinfo->code_start = code;
		jinfo->domain_neutral = (aot_module->opts & MONO_OPT_SHARED) != 0;

		minfo->info = jinfo;
		mono_g_hash_table_insert (aot_module->methods, method, minfo);

		return jinfo;
	}
}

MonoJitInfo*
mono_aot_get_method (MonoDomain *domain, MonoMethod *method)
{
	MonoJitInfo *info;

	EnterCriticalSection (&aot_mutex);
	info = mono_aot_get_method_inner (domain, method);
	LeaveCriticalSection (&aot_mutex);

	/* Do this outside the lock */
	if (info) {
		mono_jit_info_table_add (domain, info);
		return info;
	}
	else
		return NULL;
}

static void
emit_section_change (FILE *fp, const char *section_name, int subsection_index)
{
#if defined(sparc)
	/* For solaris as, GNU as should accept the same */
	fprintf (fp, ".section \"%s\"\n", section_name);
#else
	fprintf (fp, "%s %s\n", section_name, subsection_index);
#endif
}

#if 0
static void
write_data_symbol (FILE *fp, const char *name, guint8 *buf, int size, int align)
{
	int i;

	emit_section_change (fp, ".text", 1);

	fprintf (fp, ".globl %s\n", name);
	fprintf (fp, "\t.align %d\n", align);
	fprintf (fp, "\t.type %s,#object\n", name);
	fprintf (fp, "\t.size %s,%d\n", name, size);
	fprintf (fp, "%s:\n", name);
	for (i = 0; i < size; i++) { 
		fprintf (fp, ".byte %d\n", buf [i]);
	}
	
}
#endif

static void
write_string_symbol (FILE *fp, const char *name, const char *value)
{
	emit_section_change (fp, ".text", 1);
	fprintf (fp, ".globl %s\n", name);
	fprintf (fp, "%s:\n", name);
	fprintf (fp, "\t%s \"%s\"\n", AS_STRING_DIRECTIVE, value);
}

static guint32
mono_get_field_token (MonoClassField *field) 
{
	MonoClass *klass = field->parent;
	int i;

	for (i = 0; i < klass->field.count; ++i) {
		if (field == &klass->fields [i])
			return MONO_TOKEN_FIELD_DEF | (klass->field.first + 1 + i);
	}

	g_assert_not_reached ();
	return 0;
}

static guint32
get_image_index (MonoAotCompile *cfg, MonoImage *image)
{
	guint32 index;

	index = GPOINTER_TO_UINT (g_hash_table_lookup (cfg->image_hash, image));
	if (index)
		return index - 1;
	else {
		index = g_hash_table_size (cfg->image_hash);
		g_hash_table_insert (cfg->image_hash, image, GUINT_TO_POINTER (index + 1));
		g_ptr_array_add (cfg->image_table, image);
		return index;
	}
}

static void
emit_image_index (MonoAotCompile *cfg, MonoImage *image)
{
	guint32 image_index;

	image_index = get_image_index (cfg, image);

	fprintf (cfg->fp, "\t.long %d\n", image_index);
}

static char *
cond_emit_klass_label (MonoAotCompile *cfg, MonoClass *klass)
{
	char *l1, *el = NULL;

	if ((l1 = g_hash_table_lookup (cfg->ref_hash, klass))) 
		return l1;

	if (!klass->type_token) {
		g_assert (klass->rank > 0);
		el = cond_emit_klass_label (cfg, klass->element_class);
	}
	
	fprintf (cfg->fp, "\t.align %d\n", sizeof (gpointer));
	l1 = g_strdup_printf ("klass_p_%08x_%p", klass->type_token, klass);
	fprintf (cfg->fp, "%s:\n", l1);
	fprintf (cfg->fp, "\t.long 0x%08x\n", klass->type_token);
	emit_image_index (cfg, klass->image);

	if (el) {
		fprintf (cfg->fp, "\t.long %d\n", klass->rank);	
		fprintf (cfg->fp, "\t.long %s\n", el);
	}

	g_hash_table_insert (cfg->ref_hash, klass, l1);

	return l1;
}

static char *
cond_emit_field_label (MonoAotCompile *cfg, MonoJumpInfo *patch_info)
{
	MonoClassField *field = patch_info->data.field;
	char *l1, *l2;
	guint token;

	if ((l1 = g_hash_table_lookup (cfg->ref_hash, field))) 
		return l1;

	l2 = cond_emit_klass_label (cfg, field->parent);
	fprintf (cfg->fp, "\t.align %d\n", sizeof (gpointer));
	token = mono_get_field_token (field);
	g_assert (token);
	l1 = g_strdup_printf ("klass_p_%08x_%p", token, field);
	fprintf (cfg->fp, "%s:\n", l1);
	fprintf (cfg->fp, "\t.long 0x%08x\n", token);
	fprintf (cfg->fp, "\t.long %s\n", l2);
		
	g_hash_table_insert (cfg->ref_hash, field, l1);

	return l1;
}

static gint
compare_patches (gconstpointer a, gconstpointer b)
{
	int i, j;

	i = (*(MonoJumpInfo**)a)->ip.i;
	j = (*(MonoJumpInfo**)b)->ip.i;

	if (i < j)
		return -1;
	else
		if (i > j)
			return 1;
	else
		return 0;
}

static void
emit_method (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	GList *l;
	FILE *tmpfp;
	int i, j, k, pindex;
	guint8 *code, *mname;
	int func_alignment = 16;
	GPtrArray *patches;
	MonoJumpInfo *patch_info;
	MonoMethodHeader *header;

	tmpfp = acfg->fp;
	method = cfg->method;
	code = cfg->native_code;
	header = ((MonoMethodNormal*)method)->header;

	emit_section_change (tmpfp, ".text", 0);
	mname = g_strdup_printf ("m_%x", mono_metadata_token_index (method->token));
	fprintf (tmpfp, "\t.align %d\n", func_alignment);
	fprintf (tmpfp, ".globl %s\n", mname);
	fprintf (tmpfp, "\t.type %s,#function\n", mname);
	fprintf (tmpfp, "%s:\n", mname);

	for (i = 0; i < cfg->code_len; i++) 
		fprintf (tmpfp, ".byte %d\n", (unsigned int) code [i]);

	emit_section_change (tmpfp, ".text", 1);

	/* Sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

	j = 0;
	for (pindex = 0; pindex < patches->len; ++pindex) {
		patch_info = g_ptr_array_index (patches, pindex);
		switch (patch_info->type) {
		case MONO_PATCH_INFO_LABEL:
		case MONO_PATCH_INFO_BB:
			/* relative jumps are no problem, there is no need to handle then here */
			break;
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table = (gpointer *)patch_info->data.target;
			int k;

			fprintf (tmpfp, "\t.align %d\n", sizeof (gpointer));
			fprintf (tmpfp, "%s_p_%d:\n", mname, j);
			fprintf (tmpfp, "\t.long %d\n", patch_info->table_size);
			
			for (k = 0; k < patch_info->table_size; k++) {
				fprintf (tmpfp, "\t.long %d\n", (int)table [k]);
			}
			j++;
			break;
		}
		case MONO_PATCH_INFO_INTERNAL_METHOD: {
			guint32 icall_index;

			icall_index = (guint32)g_hash_table_lookup (acfg->icall_hash, patch_info->data.name);
			if (!icall_index) {
				icall_index = g_hash_table_size (acfg->icall_hash) + 1;
				g_hash_table_insert (acfg->icall_hash, (gpointer)patch_info->data.name,
									 GUINT_TO_POINTER (icall_index));
				g_ptr_array_add (acfg->icall_table, (gpointer)patch_info->data.name);
			}
			patch_info->data.name = g_strdup_printf ("%d", icall_index - 1);
			j++;
			break;
		}
		case MONO_PATCH_INFO_METHODCONST:
		case MONO_PATCH_INFO_METHOD:
		case MONO_PATCH_INFO_METHOD_JUMP: {
			/*
			 * The majority of patches are for methods, so we emit
			 * them inline instead of defining a label for them to
			 * decrease the number of relocations.
			 */
			guint32 image_index = get_image_index (acfg, patch_info->data.method->klass->image);
			guint32 token = patch_info->data.method->token;
			g_assert (image_index < 256);
			g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

			patch_info->data.name = 
				g_strdup_printf ("%d", (image_index << 24) + (mono_metadata_token_index (token)));
			j++;
			break;
		}
		case MONO_PATCH_INFO_WRAPPER: {
			MonoMethod *m;
			guint32 image_index;
			guint32 token;

			m = mono_marshal_method_from_wrapper (patch_info->data.method);
			image_index = get_image_index (acfg, m->klass->image);
			token = m->token;
			g_assert (image_index < 256);
			g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

			fprintf (tmpfp, "\t.align %d\n", sizeof (gpointer));
			fprintf (tmpfp, "%s_p_%d:\n", mname, j);
			fprintf (tmpfp, "\t.long %d\n", patch_info->data.method->wrapper_type);
			fprintf (tmpfp, "\t.long %d\n", (image_index << 24) + (mono_metadata_token_index (token)));
			j++;
			break;
		}
		case MONO_PATCH_INFO_FIELD:
			patch_info->data.name = cond_emit_field_label (acfg, patch_info);
			j++;
			break;
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IID:
			patch_info->data.name = cond_emit_klass_label (acfg, patch_info->data.klass);
			j++;
			break;
		case MONO_PATCH_INFO_IMAGE:
			patch_info->data.name = g_strdup_printf ("%d", get_image_index (acfg, patch_info->data.image));
			j++;
			break;
		case MONO_PATCH_INFO_EXC_NAME: {
			MonoClass *ex_class;
			
			ex_class =
				mono_class_from_name (mono_defaults.exception_class->image,
									  "System", patch_info->data.target);
			g_assert (ex_class);
			patch_info->data.name = cond_emit_klass_label (acfg, ex_class);
			j++;
			break;
		}
		case MONO_PATCH_INFO_R4:
			fprintf (tmpfp, "\t.align 8\n");
			fprintf (tmpfp, "%s_p_%d:\n", mname, j);
			fprintf (tmpfp, "\t.long 0x%08x\n", *((guint32 *)patch_info->data.target));	
			j++;
			break;
		case MONO_PATCH_INFO_R8:
			fprintf (tmpfp, "\t.align 8\n");
			fprintf (tmpfp, "%s_p_%d:\n", mname, j);
			fprintf (tmpfp, "\t.long 0x%08x\n", *((guint32 *)patch_info->data.target));
			fprintf (tmpfp, "\t.long 0x%08x\n", *((guint32 *)patch_info->data.target + 1));
			j++;
			break;
		case MONO_PATCH_INFO_METHOD_REL:
			fprintf (tmpfp, "\t.align %d\n", sizeof (gpointer));
			fprintf (tmpfp, "%s_p_%d:\n", mname, j);
			fprintf (tmpfp, "\t.long 0x%08x\n", patch_info->data.offset);
			j++;
			break;
		case MONO_PATCH_INFO_VTABLE:
		case MONO_PATCH_INFO_CLASS_INIT:
			patch_info->data.name = cond_emit_klass_label (acfg, patch_info->data.klass);
			j++;
			break;
		case MONO_PATCH_INFO_SFLDA:
			patch_info->data.name = cond_emit_field_label (acfg, patch_info);
			j++;
			break;
		case MONO_PATCH_INFO_LDSTR:
		case MONO_PATCH_INFO_LDTOKEN:
		case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
			fprintf (tmpfp, "\t.align 8\n");
			fprintf (tmpfp, "%s_p_%d:\n", mname, j);
			fprintf (tmpfp, "\t.long 0x%08x\n", get_image_index (acfg, patch_info->data.token->image));
			fprintf (tmpfp, "\t.long 0x%08x\n", patch_info->data.token->token);
			j++;
			break;
		default:
			g_warning ("unable to handle jump info %d", patch_info->type);
			g_assert_not_reached ();
		}
	}

	fprintf (tmpfp, ".globl %s_p\n", mname);
	fprintf (tmpfp, "\t.align %d\n", sizeof (gpointer));
	fprintf (tmpfp, "%s_p:\n", mname);

	fprintf (tmpfp, "\t.long %d\n", cfg->code_len);
	fprintf (tmpfp, "\t.long %d\n", cfg->used_int_regs);

	/* Exception table */
	if (header->num_clauses) {
		MonoJitInfo *jinfo = cfg->jit_info;

		fprintf (tmpfp, "\t.long %d\n", jinfo->exvar_offset);

		for (k = 0; k < header->num_clauses; ++k) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [k];

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				fprintf (tmpfp, "\t.long %d\n", (guint8*)ei->data.filter - code);
			else
				/* fixme: tokens are not global */
				fprintf (tmpfp, "\t.long %d\n", ei->data.token);

			fprintf (tmpfp, "\t.long %d\n", (guint8*)ei->try_start - code);
			fprintf (tmpfp, "\t.long %d\n", (guint8*)ei->try_end - code);
			fprintf (tmpfp, "\t.long %d\n", (guint8*)ei->handler_start - code);
		}
	}

	/* String table */
	if (cfg->opt & MONO_OPT_SHARED) {
		fprintf (tmpfp, "\t.long %d\n", g_list_length (cfg->ldstr_list));
		for (l = cfg->ldstr_list; l; l = l->next) {
			fprintf (tmpfp, "\t.long 0x%08lx\n", (long)l->data);
		}
	}
	else
		/* Used only in shared mode */
		g_assert (!cfg->ldstr_list);

	//printf ("M: %s (%s).\n", mono_method_full_name (method, TRUE), mname);

	if (j) {
		guint32 last_offset;
		last_offset = 0;

		j = 0;
		for (pindex = 0; pindex < patches->len; ++pindex) {
			guint32 offset;
			patch_info = g_ptr_array_index (patches, pindex);

			if ((patch_info->type == MONO_PATCH_INFO_LABEL) ||
				(patch_info->type == MONO_PATCH_INFO_BB))
				/* Nothing to do */
				continue;

			//printf ("T: %d O: %d.\n", patch_info->type, patch_info->ip.i);
			offset = patch_info->ip.i - last_offset;
			last_offset = patch_info->ip.i;

			/* Encode type+position compactly */
			g_assert (patch_info->type < 64);
			if (offset < 1024 - 1) {
				fprintf (tmpfp, "\t.byte %d\n", (patch_info->type << 2) + (offset >> 8));
				fprintf (tmpfp, "\t.byte %d\n", offset & ((1 << 8) - 1));
#if defined(sparc)
				fprintf (tmpfp, "\t.align 4\n");
#endif
			}
			else {
				fprintf (tmpfp, "\t.byte %d\n", (patch_info->type << 2) + 3);
				fprintf (tmpfp, "\t.byte %d\n", 255);
#if defined(sparc)
				fprintf (tmpfp, "\t.align 4\n");
#endif
				fprintf (tmpfp, "\t.long %d\n", offset);
			}

			switch (patch_info->type) {
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_METHOD_JUMP:
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IID:
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_INTERNAL_METHOD:
			case MONO_PATCH_INFO_IMAGE:
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_CLASS_INIT:
			case MONO_PATCH_INFO_SFLDA:
			case MONO_PATCH_INFO_EXC_NAME:
				fprintf (tmpfp, "\t.long %s\n", patch_info->data.name);
				j++;
				break;
			case MONO_PATCH_INFO_SWITCH:
			case MONO_PATCH_INFO_R4:
			case MONO_PATCH_INFO_R8:
			case MONO_PATCH_INFO_METHOD_REL:
			case MONO_PATCH_INFO_LDSTR:
			case MONO_PATCH_INFO_LDTOKEN:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
			case MONO_PATCH_INFO_WRAPPER:
				fprintf (tmpfp, "\t.long %s_p_%d\n", mname, j);
				j++;
				break;
			case MONO_PATCH_INFO_LABEL:
			case MONO_PATCH_INFO_BB:
				break;
			default:
				g_warning ("unable to handle jump info %d", patch_info->type);
				g_assert_not_reached ();
			}

		}
	}

	/*
	 * 0 is PATCH_INFO_BB, which can't be in the file.
	 */
	/* NULL terminated array */
	fprintf (tmpfp, "\t.long 0\n");

	{
		guint8 *buf;
		guint32 buf_len;

		mono_debug_serialize_debug_info (cfg, &buf, &buf_len);

		fprintf (tmpfp, "\t.long %d\n", buf_len);

		for (i = 0; i < buf_len; ++i)
			fprintf (tmpfp, ".byte %d\n", (unsigned int) buf [i]);

		if (buf_len > 0)
			g_free (buf);
	}

	/* fixme: save the rest of the required infos */

	g_free (mname);
}

int
mono_compile_assembly (MonoAssembly *ass, guint32 opts)
{
	MonoCompile *cfg;
	MonoImage *image = ass->image;
	MonoMethod *method;
	char *com, *tmpfname, *opts_str;
	FILE *tmpfp;
	int i;
	guint8 *symbol;
	int ccount = 0, mcount = 0, lmfcount = 0, abscount = 0, wrappercount = 0, ocount = 0;
	GHashTable *ref_hash;
	MonoAotCompile *acfg;
	gboolean *emitted;

	printf ("Mono Ahead of Time compiler - compiling assembly %s\n", image->name);

	i = g_file_open_tmp ("mono_aot_XXXXXX", &tmpfname, NULL);
	tmpfp = fdopen (i, "w+");
	g_assert (tmpfp);

	ref_hash = g_hash_table_new (NULL, NULL);

	acfg = g_new0 (MonoAotCompile, 1);
	acfg->fp = tmpfp;
	acfg->ref_hash = ref_hash;
	acfg->icall_hash = g_hash_table_new (NULL, NULL);
	acfg->icall_table = g_ptr_array_new ();
	acfg->image_hash = g_hash_table_new (NULL, NULL);
	acfg->image_table = g_ptr_array_new ();

	write_string_symbol (tmpfp, "mono_assembly_guid" , image->guid);

	write_string_symbol (tmpfp, "mono_aot_version", MONO_AOT_FILE_VERSION);

	opts_str = g_strdup_printf ("%d", opts);
	write_string_symbol (tmpfp, "mono_aot_opt_flags", opts_str);
	g_free (opts_str);

	emitted = g_new0 (gboolean, image->tables [MONO_TABLE_METHOD].rows);

	for (i = 0; i < image->tables [MONO_TABLE_METHOD].rows; ++i) {
		MonoJumpInfo *patch_info;
		gboolean skip;
		guint32 token = MONO_TOKEN_METHOD_DEF | (i + 1);
       	        method = mono_get_method (image, token, NULL);
		
		/* fixme: maybe we can also precompile wrapper methods */
		if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		    (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		    (method->flags & METHOD_ATTRIBUTE_ABSTRACT)) {
			//printf ("Skip (impossible): %s\n", mono_method_full_name (method, TRUE));
			continue;
		}

		mcount++;

		/* fixme: we need to patch the IP for the LMF in that case */
		if (method->save_lmf) {
			//printf ("Skip (needs lmf):  %s\n", mono_method_full_name (method, TRUE));
			lmfcount++;
			continue;
		}

		//printf ("START:           %s\n", mono_method_full_name (method, TRUE));
		//mono_compile_method (method);

		cfg = mini_method_compile (method, opts, mono_root_domain, FALSE, 0);
		g_assert (cfg);

		if (cfg->disable_aot) {
			printf ("Skip (other): %s\n", mono_method_full_name (method, TRUE));
			ocount++;
			continue;
		}

		skip = FALSE;
		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			if (patch_info->type == MONO_PATCH_INFO_ABS) {
				/* unable to handle this */
				//printf ("Skip (abs addr):   %s %d\n", mono_method_full_name (method, TRUE), patch_info->type);
				skip = TRUE;	
				break;
			}
		}

		if (skip) {
			abscount++;
			continue;
		}

		/* remoting-invoke-with-check wrappers are very common */
		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			if ((patch_info->type == MONO_PATCH_INFO_METHOD) &&
				((patch_info->data.method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)))
				patch_info->type = MONO_PATCH_INFO_WRAPPER;
		}

		skip = FALSE;
		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			if ((patch_info->type == MONO_PATCH_INFO_METHOD ||
			     patch_info->type == MONO_PATCH_INFO_METHODCONST) &&
			    patch_info->data.method->wrapper_type) {
				/* unable to handle this */
				//printf ("Skip (wrapper call):   %s %d -> %s\n", mono_method_full_name (method, TRUE), patch_info->type, mono_method_full_name (patch_info->data.method, TRUE));
				skip = TRUE;	
				break;
			}
		}

		if (skip) {
			wrappercount++;
			continue;
		}

		//printf ("Compile:           %s\n", mono_method_full_name (method, TRUE));

		emitted [i] = TRUE;
		emit_method (acfg, cfg);

		mono_destroy_compile (cfg);

		ccount++;
	}

	/*
	 * The icall and image tables are small but referenced in a lot of places.
	 * So we emit them at once, and reference their elements by an index
	 * instead of an assembly label to cut back on the number of relocations.
	 */

	/* Emit icall table */

	symbol = g_strdup_printf ("mono_icall_table");
	emit_section_change (tmpfp, ".text", 1);
	fprintf (tmpfp, ".globl %s\n", symbol);
	fprintf (tmpfp, "\t.align 8\n");
	fprintf (tmpfp, "%s:\n", symbol);
	fprintf (tmpfp, ".long %d\n", acfg->icall_table->len);
	for (i = 0; i < acfg->icall_table->len; i++)
		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, (char*)g_ptr_array_index (acfg->icall_table, i));

	/* Emit image table */

	symbol = g_strdup_printf ("mono_image_table");
	emit_section_change (tmpfp, ".text", 1);
	fprintf (tmpfp, ".globl %s\n", symbol);
	fprintf (tmpfp, "\t.align 8\n");
	fprintf (tmpfp, "%s:\n", symbol);
	fprintf (tmpfp, ".long %d\n", acfg->image_table->len);
	for (i = 0; i < acfg->image_table->len; i++)
		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, ((MonoImage*)g_ptr_array_index (acfg->image_table, i))->guid);

	/*
	 * g_module_symbol takes a lot of time for failed lookups, so we emit
	 * a table which contains one bit for each method. This bit specifies
	 * whenever the method is emitted or not.
	 */

	symbol = g_strdup_printf ("mono_methods_present_table");
	emit_section_change (tmpfp, ".text", 1);
	fprintf (tmpfp, ".globl %s\n", symbol);
	fprintf (tmpfp, "\t.align 8\n");
	fprintf (tmpfp, "%s:\n", symbol);
	{
		guint32 k, nrows;
		guint32 w;

		nrows = image->tables [MONO_TABLE_METHOD].rows;
		for (i = 0; i < nrows / 32 + 1; ++i) {
			w = 0;
			for (k = 0; k < 32; ++k) {
				if (emitted [(i * 32) + k])
					w += (1 << k);
			}
			//printf ("EMITTED [%d] = %d.\n", i, b);
			fprintf (tmpfp, "\t.long %d\n", w);
		}
	}

	fclose (tmpfp);

	com = g_strdup_printf ("as %s -o %s.o", tmpfname, tmpfname);
	printf ("Executing the native assembler: %s\n", com);
	system (com);
	g_free (com);
#if defined(sparc)
	com = g_strdup_printf ("ld -shared -G -o %s%s %s.o", image->name, SHARED_EXT, tmpfname);
#else
	com = g_strdup_printf ("ld -shared -o %s%s %s.o", image->name, SHARED_EXT, tmpfname);
#endif
	printf ("Executing the native linker: %s\n", com);
	system (com);
	g_free (com);
	com = g_strdup_printf ("%s.o", tmpfname);
	unlink (com);
	g_free (com);
	/*com = g_strdup_printf ("strip --strip-unneeded %s%s", image->name, SHARED_EXT);
	printf ("Stripping the binary: %s\n", com);
	system (com);
	g_free (com);*/

	printf ("Compiled %d out of %d methods (%d%%)\n", ccount, mcount, mcount ? (ccount*100)/mcount : 100);
	printf ("%d methods contain absolute addresses (%d%%)\n", abscount, mcount ? (abscount*100)/mcount : 100);
	printf ("%d methods contain wrapper references (%d%%)\n", wrappercount, mcount ? (wrappercount*100)/mcount : 100);
	printf ("%d methods contain lmf pointers (%d%%)\n", lmfcount, mcount ? (lmfcount*100)/mcount : 100);
	printf ("%d methods have other problems (%d%%)\n", ocount, mcount ? (ocount*100)/mcount : 100);
	printf ("Retained input file.\n");
	//unlink (tmpfname);

	return 0;
}


