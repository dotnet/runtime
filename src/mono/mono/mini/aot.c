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

#include <errno.h>
#include <sys/stat.h>
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
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/utils/mono-logger.h>
#include <mono/os/gc_wrapper.h>

#include "mini.h"

#ifndef DISABLE_AOT

#ifdef PLATFORM_WIN32
#define SHARED_EXT ".dll"
#elif defined(__ppc__) && defined(__MACH__)
#define SHARED_EXT ".dylib"
#else
#define SHARED_EXT ".so"
#endif

#if defined(sparc) || defined(__ppc__)
#define AS_STRING_DIRECTIVE ".asciz"
#else
/* GNU as */
#define AS_STRING_DIRECTIVE ".string"
#endif

#define ALIGN_PTR_TO(ptr,align) (gpointer)((((gssize)(ptr)) + (align - 1)) & (~(align - 1)))

typedef struct MonoAotMethod {
	MonoJitInfo *info;
	MonoJumpInfo *patch_info;
	MonoDomain *domain;
} MonoAotMethod;

typedef struct MonoAotModule {
	char *aot_name;
	/* Optimization flags used to compile the module */
	guint32 opts;
	/* Maps MonoMethods to MonoAotMethodInfos */
	MonoGHashTable *methods;
	/* Pointer to the Global Offset Table */
	gpointer *got;
	char **icall_table;
	MonoAssemblyName *image_names;
	char **image_guids;
	MonoImage **image_table;
	guint32* methods_present_table;
	gboolean out_of_date;
} MonoAotModule;

typedef struct MonoAotCompile {
	FILE *fp;
	GHashTable *ref_hash;
	GHashTable *icall_hash;
	GPtrArray *icall_table;
	GHashTable *image_hash;
	GPtrArray *image_table;
	guint32 got_offset;
} MonoAotCompile;

typedef struct MonoAotOptions {
	char *outfile;
} MonoAotOptions;

static MonoGHashTable *aot_modules;

static CRITICAL_SECTION aot_mutex;

/*
 * Disabling this will make a copy of the loaded code and use the copy instead 
 * of the original. This will place the caller and the callee close to each 
 * other in memory, possibly improving cache behavior. Since the original
 * code is in copy-on-write memory, this will not increase the memory usage
 * of the runtime.
 */
#ifdef MONO_ARCH_HAVE_PIC_AOT
static gboolean use_loaded_code = TRUE;
#else
static gboolean use_loaded_code = FALSE;
#endif

/*
 * Whenever to AOT compile loaded assemblies on demand and store them in
 * a cache under $HOME/.mono/aot-cache.
 */
static gboolean use_aot_cache = FALSE;

/* For debugging */
static gint32 mono_last_aot_method = -1;

static MonoJitInfo*
mono_aot_load_method (MonoDomain *domain, MonoAotModule *aot_module, MonoMethod *method, guint8 *code, guint8 *info);

static MonoImage *
load_image (MonoAotModule *module, int index)
{
	MonoAssembly *assembly;
	MonoImageOpenStatus status;

	if (module->image_table [index])
		return module->image_table [index];
	if (module->out_of_date)
		return NULL;

	assembly = mono_assembly_load (&module->image_names [index], NULL, &status);
	if (!assembly) {
		module->out_of_date = TRUE;
		return NULL;
	}

	if (strcmp (assembly->image->guid, module->image_guids [index])) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s is out of date (Older than dependency %s).\n", module->aot_name, module->image_names [index].name);
		module->out_of_date = TRUE;
		return NULL;
	}

	module->image_table [index] = assembly->image;
	return assembly->image;
}

static MonoClass*
decode_klass_info (MonoAotModule *module, guint32 *info, guint32 **out_info)
{
	MonoImage *image;
	MonoClass *klass;
	guint32 token, rank;

	image = load_image (module, info [0]);
	if (!image)
		return NULL;
	token = info [1];
	info += 2;
	if (token) {
		klass = mono_class_get (image, token);
	} else {
		token = info [0];
		rank = info [1];
		info += 2;
		klass = mono_class_get (image, token);
		g_assert (klass);
		klass = mono_array_class_get (klass, rank);
	}
	g_assert (klass);
	mono_class_init (klass);

	*out_info = info;
	return klass;
}

static MonoClassField*
decode_field_info (MonoAotModule *module, guint32 *info, guint32 **out_info)
{
	MonoClass *klass = decode_klass_info (module, info, &info);
	guint32 token;

	if (!klass)
		return NULL;

	token = info [0];
	info ++;
	*out_info = info;

	return mono_class_get_field (klass, token);
}

G_GNUC_UNUSED
static void
make_writable (guint8* addr, guint32 len)
{
#ifndef PLATFORM_WIN32
	guint8 *page_start;
	int pages, err;

	page_start = (char *) (((gssize) (addr)) & ~ (PAGESIZE - 1));
	pages = (addr + len - page_start + PAGESIZE - 1) / PAGESIZE;
	err = mprotect (page_start, pages * PAGESIZE, PROT_READ | PROT_WRITE | PROT_EXEC);
	g_assert (err == 0);
#else
	{
		DWORD oldp;
		g_assert (VirtualProtect (addr, len, PAGE_EXECUTE_READWRITE, &oldp) != 0);
	}
#endif
}

static void
create_cache_structure (void)
{
	const char *home;
	char *tmp;
	int err;

	home = g_get_home_dir ();
	if (!home)
		return;

	tmp = g_build_filename (home, ".mono", NULL);
	if (!g_file_test (tmp, G_FILE_TEST_IS_DIR)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT creating directory %s", tmp);
#ifdef PLATFORM_WIN32
		err = mkdir (tmp);
#else
		err = mkdir (tmp, 0777);
#endif
		if (err) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT failed: %s", g_strerror (errno));
			g_free (tmp);
			return;
		}
	}
	g_free (tmp);
	tmp = g_build_filename (home, ".mono", "aot-cache", NULL);
	if (!g_file_test (tmp, G_FILE_TEST_IS_DIR)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT creating directory %s", tmp);
#ifdef PLATFORM_WIN32
		err = mkdir (tmp);
#else
		err = mkdir (tmp, 0777);
#endif
		if (err) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT failed: %s", g_strerror (errno));
			g_free (tmp);
			return;
		}
	}
	g_free (tmp);
}

/*
 * load_aot_module_from_cache:
 *
 *  Experimental code to AOT compile loaded assemblies on demand. 
 *
 * FIXME: 
 * - Add environment variable MONO_AOT_CACHE_OPTIONS
 * - Add options for controlling the cache size
 * - Handle full cache by deleting old assemblies lru style
 * - Add options for excluding assemblies during development
 * - Maybe add a threshold after an assembly is AOT compiled
 * - invoking a new mono process is a security risk
 */
static GModule*
load_aot_module_from_cache (MonoAssembly *assembly, char **aot_name)
{
	char *fname, *cmd, *tmp2;
	const char *home;
	GModule *module;
	gboolean res;
	gchar *out, *err;

	*aot_name = NULL;

	if (assembly->image->dynamic)
		return NULL;

	create_cache_structure ();

	home = g_get_home_dir ();

	tmp2 = g_strdup_printf ("%s-%s%s", assembly->image->assembly_name, assembly->image->guid, SHARED_EXT);
	fname = g_build_filename (home, ".mono", "aot-cache", tmp2, NULL);
	*aot_name = fname;
	g_free (tmp2);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT trying to load from cache: '%s'.", fname);
	module = g_module_open (fname, G_MODULE_BIND_LAZY);	

	if (!module) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT not found.");

		mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT precompiling assembly '%s'... ", assembly->image->name);

		/* FIXME: security */
		cmd = g_strdup_printf ("mono -O=all --aot=outfile=%s %s", fname, assembly->image->name);

		res = g_spawn_command_line_sync (cmd, &out, &err, NULL, NULL);
		g_free (cmd);
		if (!res) {
			mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT failed.");
			return NULL;
		}

		mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT succeeded.");

		module = g_module_open (fname, G_MODULE_BIND_LAZY);	
	}

	return module;
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

#ifdef MONO_ARCH_HAVE_PIC_AOT
	gpointer *got_addr = NULL;
	gpointer *got = NULL;
#endif

	if (mono_compile_aot)
		return;
							
	if (use_aot_cache)
		assembly->aot_module = load_aot_module_from_cache (assembly, &aot_name);
	else {
		aot_name = g_strdup_printf ("%s%s", assembly->image->name, SHARED_EXT);

		assembly->aot_module = g_module_open (aot_name, G_MODULE_BIND_LAZY);

		if (!assembly->aot_module) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT failed to load AOT module %s: %s\n", aot_name, g_module_error ());
		}
	}

	if (!assembly->aot_module) {
		g_free (aot_name);
		return;
	}

	g_module_symbol (assembly->aot_module, "mono_assembly_guid", (gpointer *) &saved_guid);
	g_module_symbol (assembly->aot_module, "mono_aot_version", (gpointer *) &aot_version);
	g_module_symbol (assembly->aot_module, "mono_aot_opt_flags", (gpointer *)&opt_flags);
#ifdef MONO_ARCH_HAVE_PIC_AOT
	g_module_symbol (assembly->aot_module, "got_addr", (gpointer *)&got_addr);
	g_assert (got_addr);
	got = (gpointer*)*got_addr;
	g_assert (got);
#endif

	if (!aot_version || strcmp (aot_version, MONO_AOT_FILE_VERSION)) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s has wrong file format version (expected %s got %s)\n", aot_name, MONO_AOT_FILE_VERSION, aot_version);
		usable = FALSE;
	}
	else {
		if (!saved_guid || strcmp (assembly->image->guid, saved_guid)) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s is out of date.\n", aot_name);
			usable = FALSE;
		}
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
	info->aot_name = aot_name;
	info->methods = mono_g_hash_table_new (NULL, NULL);
#ifdef MONO_ARCH_HAVE_PIC_AOT
	info->got = got;
#endif
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
		info->image_names = g_new0 (MonoAssemblyName, table_len);
		info->image_guids = g_new0 (char*, table_len);
		for (i = 0; i < table_len; ++i) {
			MonoAssemblyName *aname = &(info->image_names [i]);

			aname->name = g_strdup (table);
			table += strlen (table) + 1;
			info->image_guids [i] = g_strdup (table);
			table += strlen (table) + 1;
			if (table [0] != 0)
				aname->culture = g_strdup (table);
			table += strlen (table) + 1;
			memcpy (aname->public_key_token, table, strlen (table) + 1);
			table += strlen (table) + 1;			

			table = ALIGN_PTR_TO (table, 8);
			aname->flags = *(guint32*)table;
			table += 4;
			aname->major = *(guint32*)table;
			table += 4;
			aname->minor = *(guint32*)table;
			table += 4;
			aname->build = *(guint32*)table;
			table += 4;
			aname->revision = *(guint32*)table;
			table += 4;
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

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT loaded AOT Module for %s.\n", assembly->image->name);
}

void
mono_aot_init (void)
{
	InitializeCriticalSection (&aot_mutex);

	MONO_GC_REGISTER_ROOT (aot_modules);
	aot_modules = mono_g_hash_table_new (NULL, NULL);

	mono_install_assembly_load_hook (load_aot_module, NULL);

	if (getenv ("MONO_LASTAOT"))
		mono_last_aot_method = atoi (getenv ("MONO_LASTAOT"));
	if (getenv ("MONO_AOT_CACHE"))
		use_aot_cache = TRUE;
}
 
static MonoJitInfo *
mono_aot_get_method_inner (MonoDomain *domain, MonoMethod *method)
{
	MonoClass *klass = method->klass;
	MonoAssembly *ass = klass->image->assembly;
	GModule *module = ass->aot_module;
	char method_label [256];
	char info_label [256];
	guint8 *code = NULL;
	guint8 *info;
	MonoAotModule *aot_module;
	MonoAotMethod *minfo;
	MonoJitInfo *jinfo;
	MonoMethodHeader *header;

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
	
	header = mono_method_get_header (method);
	
	aot_module = (MonoAotModule*)mono_g_hash_table_lookup (aot_modules, ass);

	g_assert (klass->inited);

	if ((domain != mono_get_root_domain ()) && (!(aot_module->opts & MONO_OPT_SHARED)))
		/* Non shared AOT code can't be used in other appdomains */
		return NULL;

	minfo = mono_g_hash_table_lookup (aot_module->methods, method);
	/* Can't use code from non-root domains since they can be unloaded */
	if (minfo && (minfo->domain == mono_get_root_domain ())) {
		/* This method was already loaded in another appdomain */

		/* Duplicate jinfo */
		jinfo = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));
		memcpy (jinfo, minfo->info, sizeof (MonoJitInfo));
		if (jinfo->clauses) {
			jinfo->clauses = 
				mono_mempool_alloc0 (domain->mp, sizeof (MonoJitExceptionInfo) * header->num_clauses);
			memcpy (jinfo->clauses, minfo->info->clauses, sizeof (MonoJitExceptionInfo) * header->num_clauses);
		}

		return jinfo;
	}

	/* Do a fast check to see whenever the method exists */
	{
		guint32 index = mono_metadata_token_index (method->token) - 1;
		guint32 w;
		w = aot_module->methods_present_table [index / 32];
		if (! (w & (1 << (index % 32)))) {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT NOT FOUND: %s.\n", mono_method_full_name (method, TRUE));
			return NULL;
		}
	}

	if (aot_module->out_of_date)
		return NULL;

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

	return mono_aot_load_method (domain, aot_module, method, code, info);
}

static MonoJitInfo*
mono_aot_load_method (MonoDomain *domain, MonoAotModule *aot_module, MonoMethod *method, guint8 *code, guint8 *info)
{
	MonoClass *klass = method->klass;
	MonoJumpInfo *patch_info = NULL;
	guint code_len, used_int_regs, used_strings;
	MonoAotMethod *minfo;
	MonoJitInfo *jinfo;
	MonoMethodHeader *header = mono_method_get_header (method);
	MonoMemPool *mp;
	GPtrArray *patches;
	int i, pindex, got_index;

	minfo = g_new0 (MonoAotMethod, 1);

	minfo->domain = domain;
	jinfo = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));

	code_len = *(guint32*)info;
	info += 4;
	used_int_regs = *(guint32*)info;
	info += 4;

	if (!use_loaded_code) {
		guint8 *code2;
		code2 = mono_code_manager_reserve (domain->code_mp, code_len);
		memcpy (code2, code, code_len);
		mono_arch_flush_icache (code2, code_len);
		code = code2;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT FOUND AOT compiled code for %s %p - %p %p\n", mono_method_full_name (method, TRUE), code, code + code_len, info);

	/* Exception table */
	if (header->num_clauses) {
		jinfo->clauses = 
			mono_mempool_alloc0 (domain->mp, sizeof (MonoJitExceptionInfo) * header->num_clauses);
		jinfo->num_clauses = header->num_clauses;

		jinfo->exvar_offset = *(guint32*)info;
		info += 4;

		for (i = 0; i < header->num_clauses; ++i) {
			MonoExceptionClause *ec = &header->clauses [i];				
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];

			ei->flags = ec->flags;
			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				ei->data.filter = code + *(guint32*)info;
			else
				ei->data.catch_class = ec->data.catch_class;
			info += 4;
			ei->try_start = code + *(guint32*)info;
			info += 4;
			ei->try_end = code + *(guint32*)info;
			info += 4;
			ei->handler_start = code + *(guint32*)info;
			info += 4;
		}
	}

	if (aot_module->opts & MONO_OPT_SHARED) {
		used_strings = *(guint32*)info;
		info += 4;
	}
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint token =  *(guint32*)info;
		info += 4;
		mono_ldstr (mono_get_root_domain (), klass->image, mono_metadata_token_index (token));
	}

#ifdef MONO_ARCH_HAVE_PIC_AOT
	got_index = *(guint32*)info;
	info += 4;
#endif

	if (*info) {
		MonoImage *image;
		gpointer *table;
		int i;
		guint32 last_offset, buf_len;
		guint32 *info32;

		if (aot_module->opts & MONO_OPT_SHARED)
			mp = mono_mempool_new ();
		else
			mp = domain->mp;

		/* First load the type + offset table */
		last_offset = 0;
		patches = g_ptr_array_new ();
		while (*info) {
			MonoJumpInfo *ji = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));

#ifdef MONO_ARCH_HAVE_PIC_AOT
			ji->type = *(guint8*)info;
			info ++;
#else
			guint8 b1, b2;

			b1 = *(guint8*)info;
			b2 = *((guint8*)info + 1);
			
			info += 2;

			ji->type = b1 >> 2;

			if (((b1 & (1 + 2)) == 3) && (b2 == 255)) {
				info = ALIGN_PTR_TO (info, 4);
				ji->ip.i = *(guint32*)info;
				info += 4;
			}
			else
				ji->ip.i = (((guint32)(b1 & (1 + 2))) << 8) + b2;

			ji->ip.i += last_offset;
			last_offset = ji->ip.i;
#endif
			//printf ("T: %d O: %d.\n", ji->type, ji->ip.i);

			ji->next = patch_info;
			patch_info = ji;

			g_ptr_array_add (patches, ji);
		}
		info ++;

		info = ALIGN_PTR_TO (info, sizeof (gpointer));

		info32 = (guint32*)info;

		/* Then load the other data */
		for (pindex = 0; pindex < patches->len; ++pindex) {
			MonoJumpInfo *ji = g_ptr_array_index (patches, pindex);

			switch (ji->type) {
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IID:
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_CLASS_INIT:
				ji->data.klass = decode_klass_info (aot_module, info32, &info32);
				if (!ji->data.klass)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_IMAGE:
				ji->data.image = load_image (aot_module, info32 [0]);
				if (!ji->data.image)
					goto cleanup;
				g_assert (ji->data.image);
				info32 ++;
				break;
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_METHOD_JUMP: {
				guint32 image_index, token;

				image_index = info32 [0] >> 24;
				token = MONO_TOKEN_METHOD_DEF | (info32 [0] & 0xffffff);

				image = load_image (aot_module, image_index);
				if (!image)
					goto cleanup;
				ji->data.method = mono_get_method (image, token, NULL);
				g_assert (ji->data.method);
				mono_class_init (ji->data.method->klass);
				info32 ++;

				break;
			}
			case MONO_PATCH_INFO_WRAPPER: {
				guint32 wrapper_type;

				wrapper_type = info32 [0];
				info32 ++;

				switch (wrapper_type) {
				case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: {
					guint32 image_index, token;

					image_index = info32 [0] >> 24;
					token = MONO_TOKEN_METHOD_DEF | (info32 [0] & 0xffffff);

					image = load_image (aot_module, image_index);
					if (!image)
						goto cleanup;
					ji->data.method = mono_get_method (image, token, NULL);
					g_assert (ji->data.method);
					mono_class_init (ji->data.method->klass);

					ji->type = MONO_PATCH_INFO_METHOD;
					ji->data.method = mono_marshal_get_remoting_invoke_with_check (ji->data.method);
					info32 ++;
					break;
				}
				case MONO_WRAPPER_PROXY_ISINST: {
					MonoClass *klass = decode_klass_info (aot_module, info32, &info32);
					if (!klass)
						goto cleanup;
					ji->type = MONO_PATCH_INFO_METHODCONST;
					ji->data.method = mono_marshal_get_proxy_cancast (klass);
					break;
				}
				case MONO_WRAPPER_LDFLD:
				case MONO_WRAPPER_STFLD: {
					MonoClass *klass = decode_klass_info (aot_module, info32, &info32);
					if (!klass)
						goto cleanup;
					ji->type = MONO_PATCH_INFO_METHOD;
					if (wrapper_type == MONO_WRAPPER_LDFLD)
						ji->data.method = mono_marshal_get_ldfld_wrapper (&klass->byval_arg);
					else
						ji->data.method = mono_marshal_get_stfld_wrapper (&klass->byval_arg);
					break;
				}
				default:
					g_assert_not_reached ();
				}
				break;
			}
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_SFLDA:
				ji->data.field = decode_field_info (aot_module, info32, &info32);
				if (!ji->data.field)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_INTERNAL_METHOD:
				ji->data.name = aot_module->icall_table [info32 [0]];
				g_assert (ji->data.name);
				info32 ++;
				//printf ("A: %s.\n", ji->data.name);
				break;
			case MONO_PATCH_INFO_SWITCH:
				ji->table_size = info32 [0];
				table = g_new (gpointer, ji->table_size);
				ji->data.target = table;
				for (i = 0; i < ji->table_size; i++) {
					table [i] = (gpointer)(gssize)info32 [i + 1];
				}
				info32 += (ji->table_size + 1);
				break;
			case MONO_PATCH_INFO_R4:
				ji->data.target = info32;
				info32 ++;
				break;
			case MONO_PATCH_INFO_R8:
				info32 = ALIGN_PTR_TO (info32, 8);
				ji->data.target = info32;
				info32 += 2;
				break;
			case MONO_PATCH_INFO_LDSTR:
			case MONO_PATCH_INFO_LDTOKEN:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
				image = load_image (aot_module, info32 [0]);
				if (!image)
					goto cleanup;
				ji->data.token = mono_jump_info_token_new (mp, image, info32 [1]);
				info32 += 2;
				break;
			case MONO_PATCH_INFO_EXC_NAME:
				ji->data.klass = decode_klass_info (aot_module, info32, &info32);
				if (!ji->data.klass)
					goto cleanup;
				ji->data.name = ji->data.klass->name;
				break;
			case MONO_PATCH_INFO_METHOD_REL:
				ji->data.offset = info32 [0];
				info32 ++;
				break;
			default:
				g_warning ("unhandled type %d", ji->type);
				g_assert_not_reached ();
			}
		}

		info = (guint8*)info32;

		buf_len = *(guint32*)info;
		info += 4;
		mono_debug_add_aot_method (domain, method, code, info, buf_len);

#if MONO_ARCH_HAVE_PIC_AOT
		mono_arch_flush_icache (code, code_len);

		/* Do this outside the lock to avoid deadlocks */
		LeaveCriticalSection (&aot_mutex);
		for (pindex = 0; pindex < patches->len; ++pindex) {
			MonoJumpInfo *ji = g_ptr_array_index (patches, pindex);

			aot_module->got [got_index + pindex] = mono_resolve_patch_target (method, domain, code, ji, TRUE);
		}
		EnterCriticalSection (&aot_mutex);
#else
		if (use_loaded_code)
			/* disable write protection */
			make_writable (code, code_len);

		/* Do this outside the lock to avoid deadlocks */
		LeaveCriticalSection (&aot_mutex);
		mono_arch_patch_code (method, domain, code, patch_info, TRUE);
		EnterCriticalSection (&aot_mutex);
#endif

		g_ptr_array_free (patches, TRUE);

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
#ifdef MONO_ARCH_HAVE_PIC_AOT
		jinfo->domain_neutral = 1;
#else
		jinfo->domain_neutral = (aot_module->opts & MONO_OPT_SHARED) != 0;
#endif

		minfo->info = jinfo;
		mono_g_hash_table_insert (aot_module->methods, method, minfo);

		return jinfo;
	}

 cleanup:
	g_ptr_array_free (patches, TRUE);

	/* FIXME: The space in domain->mp is wasted */	
	if (aot_module->opts & MONO_OPT_SHARED)
		/* No need to cache patches */
		mono_mempool_destroy (mp);

	return NULL;
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
#elif defined(__ppc__) && defined(__MACH__)
	/* This needs to be made more precise on mach. */
	fprintf (fp, "%s\n", subsection_index == 0 ? ".text" : ".data");
#else
	fprintf (fp, "%s %d\n", section_name, subsection_index);
#endif
}

static void
emit_symbol_type (FILE *fp, const char *name, gboolean func)
{
	const char *stype;

	if (func)
		stype = "function";
	else
		stype = "object";

#if defined(sparc)
	fprintf (fp, "\t.type %s,#%s\n", name, stype);
#elif !(defined(__ppc__) && defined(__MACH__))
	fprintf (fp, "\t.type %s,@%s\n", name, stype);
#elif defined(__x86_64__)
	fprintf (fp, "\t.type %s,@%s\n", name, stype);
#endif
}

static void
emit_global (FILE *fp, const char *name, gboolean func)
{
#if defined(__ppc__) && defined(__MACH__)
    // mach-o always uses a '_' prefix.
	fprintf (fp, ".globl _%s\n", name);
#else
	fprintf (fp, ".globl %s\n", name);
#endif

	emit_symbol_type (fp, name, func);
}

static void
emit_label (FILE *fp, const char *name)
{
#if defined(__ppc__) && defined(__MACH__)
    // mach-o always uses a '_' prefix.
	fprintf (fp, "_%s:\n", name);
#else
	fprintf (fp, "%s:\n", name);
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
	emit_global(fp, name, FALSE);
	emit_label(fp, name);
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
emit_klass_info (MonoAotCompile *cfg, MonoClass *klass)
{
	fprintf (cfg->fp, "\t.long 0x%08x\n", get_image_index (cfg, klass->image));
	fprintf (cfg->fp, "\t.long 0x%08x\n", klass->type_token);
	if (!klass->type_token) {
		/* Array class */
		g_assert (klass->rank > 0);
		g_assert (klass->element_class->type_token);
		fprintf (cfg->fp, "\t.long 0x%08x\n", klass->element_class->type_token);
		fprintf (cfg->fp, "\t.long 0x%08x\n", klass->rank);
	}
}

static void
emit_field_info (MonoAotCompile *cfg, MonoClassField *field)
{
	emit_klass_info (cfg, field->parent);
	fprintf (cfg->fp, "\t.long 0x%08x\n", mono_get_field_token (field));
}

#if defined(__ppc__) && defined(__MACH__)
static int
ilog2(register int value)
{
    int count = -1;
    while (value & ~0xf) count += 4, value >>= 4;
    while (value) count++, value >>= 1;
    return count;
}
#endif

static void 
emit_alignment(FILE *fp, int size)
{
#if defined(__ppc__) && defined(__MACH__)
	// the mach-o assembler specifies alignments as powers of 2.
	fprintf (fp, "\t.align %d\t; ilog2\n", ilog2(size));
#elif defined(__powerpc__)
	/* ignore on linux/ppc */
#else
	fprintf (fp, "\t.align %d\n", size);
#endif
}

G_GNUC_UNUSED static void
emit_pointer (FILE *fp, const char *target)
{
	emit_alignment (fp, sizeof (gpointer));
#if defined(__x86_64__)
	fprintf (fp, "\t.quad %s\n", target);
#elif defined(sparc) && SIZEOF_VOID_P == 8
	fprintf (fp, "\t.xword %s\n", target);
#else
	fprintf (fp, "\t.long %s\n", target);
#endif
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
	guint8 *code, *mname, *mname_p;
	int func_alignment = 16;
	GPtrArray *patches;
	MonoJumpInfo *patch_info;
	MonoMethodHeader *header;
	guint32 last_offset;
#ifdef MONO_ARCH_HAVE_PIC_AOT
	guint32 first_got_offset;
#endif

	tmpfp = acfg->fp;
	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	emit_section_change (tmpfp, ".text", 0);
	mname = g_strdup_printf ("m_%x", mono_metadata_token_index (method->token));
	mname_p = g_strdup_printf ("%s_p", mname);
	emit_alignment(tmpfp, func_alignment);
	emit_global(tmpfp, mname, TRUE);
	emit_label(tmpfp, mname);

	/* Sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

#ifdef MONO_ARCH_HAVE_PIC_AOT
	first_got_offset = acfg->got_offset;
	for (i = 0; i < cfg->code_len; i++) {
		patch_info = NULL;
		for (pindex = 0; pindex < patches->len; ++pindex) {
			patch_info = g_ptr_array_index (patches, pindex);
			if (patch_info->ip.i == i)
				break;
		}

		if (patch_info && (pindex < patches->len)) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_LABEL:
			case MONO_PATCH_INFO_BB:
				fprintf (tmpfp, ".byte %d\n", (unsigned int) code [i]);
				break;
			default:
				/* FIXME: This is arch specific */
				for (j = 0; j < mono_arch_get_aot_patch_offset (); ++j)
					fprintf (tmpfp, ".byte %d\n", (unsigned int) code [i + j]);
				fprintf (tmpfp, ".int got - . + %d\n", (unsigned int) ((acfg->got_offset * sizeof (gpointer)) - 4));

				acfg->got_offset ++;

				i += mono_arch_get_aot_patch_offset () + 4 - 1;
			}
		}
		else {
			fprintf (tmpfp, ".byte %d\n", (unsigned int) code [i]);
		}
	}
#else
	for (i = 0; i < cfg->code_len; i++) {
		fprintf (tmpfp, ".byte %d\n", (unsigned int) code [i]);
	}
#endif

	emit_section_change (tmpfp, ".text", 1);

	emit_global (tmpfp, mname_p, FALSE);
	emit_alignment (tmpfp, sizeof (gpointer));
	emit_label (tmpfp, mname_p);

	fprintf (tmpfp, "\t.long %d\n", cfg->code_len);
	fprintf (tmpfp, "\t.long %ld\n", (long)cfg->used_int_regs);

	/* Exception table */
	if (header->num_clauses) {
		MonoJitInfo *jinfo = cfg->jit_info;

		fprintf (tmpfp, "\t.long %d\n", jinfo->exvar_offset);

		for (k = 0; k < header->num_clauses; ++k) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [k];

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				fprintf (tmpfp, "\t.long %d\n", (gint)((guint8*)ei->data.filter - code));
			else
				/* the class is loaded from the header: optimize away later */
				fprintf (tmpfp, "\t.long %d\n", 0);

			fprintf (tmpfp, "\t.long %d\n", (gint)((guint8*)ei->try_start - code));
			fprintf (tmpfp, "\t.long %d\n", (gint)((guint8*)ei->try_end - code));
			fprintf (tmpfp, "\t.long %d\n", (gint)((guint8*)ei->handler_start - code));
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

#ifdef MONO_ARCH_HAVE_PIC_AOT
	fprintf (tmpfp, "\t.long %d\n", first_got_offset);
#endif

	/* First emit the type+position table */
	last_offset = 0;
	j = 0;
	for (pindex = 0; pindex < patches->len; ++pindex) {
		guint32 offset;
		patch_info = g_ptr_array_index (patches, pindex);
		
		if ((patch_info->type == MONO_PATCH_INFO_LABEL) ||
			(patch_info->type == MONO_PATCH_INFO_BB))
			/* Nothing to do */
			continue;

		j ++;
		//printf ("T: %d O: %d.\n", patch_info->type, patch_info->ip.i);
		offset = patch_info->ip.i - last_offset;
		last_offset = patch_info->ip.i;

#ifdef MONO_ARCH_HAVE_PIC_AOT
		/* Only the type is needed */
		fprintf (tmpfp, "\t.byte %d\n", patch_info->type);
#else
		/* Encode type+position compactly */
		g_assert (patch_info->type < 64);
		if (offset < 1024 - 1) {
			fprintf (tmpfp, "\t.byte %d\n", (patch_info->type << 2) + (offset >> 8));
			fprintf (tmpfp, "\t.byte %d\n", offset & ((1 << 8) - 1));
		}
		else {
			fprintf (tmpfp, "\t.byte %d\n", (patch_info->type << 2) + 3);
			fprintf (tmpfp, "\t.byte %d\n", 255);
			emit_alignment(tmpfp, 4);
			fprintf (tmpfp, "\t.long %d\n", offset);
		}
#endif
	}

	if (j) {
		/*
		 * 0 is PATCH_INFO_BB, which can't be in the file.
		 */
		/* NULL terminated array */
		fprintf (tmpfp, "\t.byte 0\n");

		emit_alignment (tmpfp, sizeof (gpointer));

		/* Then emit the other info */
		for (pindex = 0; pindex < patches->len; ++pindex) {
			patch_info = g_ptr_array_index (patches, pindex);

			if ((patch_info->type == MONO_PATCH_INFO_LABEL) ||
				(patch_info->type == MONO_PATCH_INFO_BB))
				/* Nothing to do */
				continue;

			switch (patch_info->type) {
			case MONO_PATCH_INFO_LABEL:
			case MONO_PATCH_INFO_BB:
				break;
			case MONO_PATCH_INFO_IMAGE:
				fprintf (tmpfp, "\t.long 0x%08x\n", get_image_index (acfg, patch_info->data.image));
				break;
			case MONO_PATCH_INFO_METHOD_REL:
				fprintf (tmpfp, "\t.long 0x%08x\n", (gint)patch_info->data.offset);
				break;
			case MONO_PATCH_INFO_SWITCH: {
				gpointer *table = (gpointer *)patch_info->data.target;
				int k;

				fprintf (tmpfp, "\t.long %d\n", patch_info->table_size);
			
				for (k = 0; k < patch_info->table_size; k++) {
					fprintf (tmpfp, "\t.long %d\n", (int)(gssize)table [k]);
				}
				break;
			}
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_METHOD_JUMP: {
				guint32 image_index = get_image_index (acfg, patch_info->data.method->klass->image);
				guint32 token = patch_info->data.method->token;
				g_assert (image_index < 256);
				g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

				fprintf (tmpfp, "\t.long 0x%08x\n", (image_index << 24) + (mono_metadata_token_index (token)));
				break;
			}
			case MONO_PATCH_INFO_INTERNAL_METHOD: {
				guint32 icall_index;

				icall_index = GPOINTER_TO_UINT (g_hash_table_lookup (acfg->icall_hash, patch_info->data.name));
				if (!icall_index) {
					icall_index = g_hash_table_size (acfg->icall_hash) + 1;
					g_hash_table_insert (acfg->icall_hash, (gpointer)patch_info->data.name,
										 GUINT_TO_POINTER (icall_index));
					g_ptr_array_add (acfg->icall_table, (gpointer)patch_info->data.name);
				}
				fprintf (tmpfp, "\t.long 0x%08x\n", icall_index - 1);
				break;
			}
			case MONO_PATCH_INFO_LDSTR:
			case MONO_PATCH_INFO_LDTOKEN:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
				fprintf (tmpfp, "\t.long 0x%08x\n", get_image_index (acfg, patch_info->data.token->image));
				fprintf (tmpfp, "\t.long 0x%08x\n", patch_info->data.token->token);
				break;
			case MONO_PATCH_INFO_EXC_NAME: {
				MonoClass *ex_class;

				ex_class =
					mono_class_from_name (mono_defaults.exception_class->image,
										  "System", patch_info->data.target);
				g_assert (ex_class);
				emit_klass_info (acfg, ex_class);
				break;
			}
			case MONO_PATCH_INFO_R4:
				fprintf (tmpfp, "\t.long 0x%08x\n", *((guint32 *)patch_info->data.target));	
				break;
			case MONO_PATCH_INFO_R8:
				emit_alignment (tmpfp, 8);
				fprintf (tmpfp, "\t.long 0x%08x\n", *((guint32 *)patch_info->data.target));
				fprintf (tmpfp, "\t.long 0x%08x\n", *(((guint32 *)patch_info->data.target) + 1));
				break;
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_CLASS_INIT:
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IID:
				emit_klass_info (acfg, patch_info->data.klass);
				break;
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_SFLDA:
				emit_field_info (acfg, patch_info->data.field);
				break;
			case MONO_PATCH_INFO_WRAPPER: {
				fprintf (tmpfp, "\t.long %d\n", patch_info->data.method->wrapper_type);

				switch (patch_info->data.method->wrapper_type) {
				case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: {
					MonoMethod *m;
					guint32 image_index;
					guint32 token;

					m = mono_marshal_method_from_wrapper (patch_info->data.method);
					image_index = get_image_index (acfg, m->klass->image);
					token = m->token;
					g_assert (image_index < 256);
					g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

					fprintf (tmpfp, "\t.long %d\n", (image_index << 24) + (mono_metadata_token_index (token)));
					break;
				}
				case MONO_WRAPPER_PROXY_ISINST:
				case MONO_WRAPPER_LDFLD:
				case MONO_WRAPPER_STFLD: {
					MonoClass *proxy_class = (MonoClass*)mono_marshal_method_from_wrapper (patch_info->data.method);
					emit_klass_info (acfg, proxy_class);
					break;
				}
				default:
					g_assert_not_reached ();
				}
				break;
			}
			default:
				g_warning ("unable to handle jump info %d", patch_info->type);
				g_assert_not_reached ();
			}
		}
	}

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
	g_free (mname_p);
}

static gboolean
str_begins_with (const char *str1, const char *str2)
{
	int len = strlen (str2);
	return strncmp (str1, str2, len) == 0;
}

static void
mono_aot_parse_options (const char *aot_options, MonoAotOptions *opts)
{
	gchar **args, **ptr;

	memset (opts, 0, sizeof (*opts));

	args = g_strsplit (aot_options ? aot_options : "", ",", -1);
	for (ptr = args; ptr && *ptr; ptr ++) {
		const char *arg = *ptr;

		if (str_begins_with (arg, "outfile=")) {
			opts->outfile = g_strdup (arg + strlen ("outfile="));
		}
		else {
			fprintf (stderr, "AOT : Unknown argument '%s'.\n", arg);
			exit (1);
		}
	}
}

int
mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options)
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
	MonoAotOptions aot_opts;
	char *outfile_name, *tmp_outfile_name;

	printf ("Mono Ahead of Time compiler - compiling assembly %s\n", image->name);

	mono_aot_parse_options (aot_options, &aot_opts);

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

		cfg = mini_method_compile (method, opts, mono_get_root_domain (), FALSE, 0);
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

		/* some wrappers are very common */
		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			if (patch_info->type == MONO_PATCH_INFO_METHODCONST) {
				switch (patch_info->data.method->wrapper_type) {
				case MONO_WRAPPER_PROXY_ISINST:
					patch_info->type = MONO_PATCH_INFO_WRAPPER;
				}
			}

			if (patch_info->type == MONO_PATCH_INFO_METHOD) {
				switch (patch_info->data.method->wrapper_type) {
				case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK:
				case MONO_WRAPPER_STFLD:
				case MONO_WRAPPER_LDFLD:
					patch_info->type = MONO_PATCH_INFO_WRAPPER;
					break;
				}
			}
		}

		skip = FALSE;
		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			if ((patch_info->type == MONO_PATCH_INFO_METHOD ||
			     patch_info->type == MONO_PATCH_INFO_METHODCONST)) {
				if (patch_info->data.method->wrapper_type) {
					/* unable to handle this */
					//printf ("Skip (wrapper call):   %s %d -> %s\n", mono_method_full_name (method, TRUE), patch_info->type, mono_method_full_name (patch_info->data.method, TRUE));
					skip = TRUE;
					break;
				}
				if (!patch_info->data.method->token) {
					/*
					 * The method is part of a constructed type like Int[,].Set (). It doesn't
					 * have a token, and we can't make one, since the parent type is part of
					 * assembly which contains the element type, and not the assembly which
					 * referenced this type.
					 */
					skip = TRUE;
					break;
				}
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
	emit_global(tmpfp, symbol, FALSE);
	emit_alignment(tmpfp, 8);
	emit_label(tmpfp, symbol);
	fprintf (tmpfp, ".long %d\n", acfg->icall_table->len);
	for (i = 0; i < acfg->icall_table->len; i++)
		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, (char*)g_ptr_array_index (acfg->icall_table, i));

	/* Emit image table */

	symbol = g_strdup_printf ("mono_image_table");
	emit_section_change (tmpfp, ".text", 1);
	emit_global(tmpfp, symbol, FALSE);
	emit_alignment(tmpfp, 8);
	emit_label(tmpfp, symbol);
	fprintf (tmpfp, ".long %d\n", acfg->image_table->len);
	for (i = 0; i < acfg->image_table->len; i++) {
		MonoImage *image = (MonoImage*)g_ptr_array_index (acfg->image_table, i);
		MonoAssemblyName *aname = &image->assembly->aname;

		/* FIXME: Support multi-module assemblies */
		g_assert (image->assembly->image == image);

		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, image->assembly_name);
		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, image->guid);
		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, aname->culture ? aname->culture : "");
		fprintf (tmpfp, "%s \"%s\"\n", AS_STRING_DIRECTIVE, aname->public_key_token);

		emit_alignment (tmpfp, 8);
		fprintf (tmpfp, ".long %d\n", aname->flags);
		fprintf (tmpfp, ".long %d\n", aname->major);
		fprintf (tmpfp, ".long %d\n", aname->minor);
		fprintf (tmpfp, ".long %d\n", aname->build);
		fprintf (tmpfp, ".long %d\n", aname->revision);
	}

	/*
	 * g_module_symbol takes a lot of time for failed lookups, so we emit
	 * a table which contains one bit for each method. This bit specifies
	 * whenever the method is emitted or not.
	 */

	symbol = g_strdup_printf ("mono_methods_present_table");
	emit_section_change (tmpfp, ".text", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label (tmpfp, symbol);
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

#ifdef MONO_ARCH_HAVE_PIC_AOT
	/* Emit GOT */

	/* Don't make GOT global so accesses to it don't need relocations */
	symbol = g_strdup_printf ("got");
	emit_section_change (tmpfp, ".bss", 1);
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);
	fprintf (tmpfp, ".skip %d\n", (int)(acfg->got_offset * sizeof (gpointer)));

	symbol = g_strdup_printf ("got_addr");
	emit_section_change (tmpfp, ".data", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);
	emit_pointer (tmpfp, "got");
#endif

	fclose (tmpfp);

#if defined(__x86_64__)
	com = g_strdup_printf ("as --64 %s -o %s.o", tmpfname, tmpfname);
#elif defined(sparc) && SIZEOF_VOID_P == 8
	com = g_strdup_printf ("as -xarch=v9 %s -o %s.o", tmpfname, tmpfname);
#else
	com = g_strdup_printf ("as %s -o %s.o", tmpfname, tmpfname);
#endif
	printf ("Executing the native assembler: %s\n", com);
	if (system (com) != 0) {
		g_free (com);
		return 1;
	}

	g_free (com);

	if (aot_opts.outfile)
		outfile_name = g_strdup_printf ("%s", aot_opts.outfile);
	else
		outfile_name = g_strdup_printf ("%s%s", image->name, SHARED_EXT);

	tmp_outfile_name = g_strdup_printf ("%s.tmp", outfile_name);

#if defined(sparc)
	com = g_strdup_printf ("ld -shared -G -o %s %s.o", outfile_name, tmpfname);
#elif defined(__ppc__) && defined(__MACH__)
	com = g_strdup_printf ("gcc -dynamiclib -o %s %s.o", outfile_name, tmpfname);
#else
	com = g_strdup_printf ("ld -shared -o %s %s.o", outfile_name, tmpfname);
#endif
	printf ("Executing the native linker: %s\n", com);
	if (system (com) != 0) {
		g_free (tmp_outfile_name);
		g_free (outfile_name);
		g_free (com);
		return 1;
	}

	g_free (com);
	com = g_strdup_printf ("%s.o", tmpfname);
	unlink (com);
	g_free (com);
	/*com = g_strdup_printf ("strip --strip-unneeded %s%s", image->name, SHARED_EXT);
	printf ("Stripping the binary: %s\n", com);
	system (com);
	g_free (com);*/

	rename (tmp_outfile_name, outfile_name);

	g_free (tmp_outfile_name);
	g_free (outfile_name);

	printf ("Compiled %d out of %d methods (%d%%)\n", ccount, mcount, mcount ? (ccount*100)/mcount : 100);
	printf ("%d methods contain absolute addresses (%d%%)\n", abscount, mcount ? (abscount*100)/mcount : 100);
	printf ("%d methods contain wrapper references (%d%%)\n", wrappercount, mcount ? (wrappercount*100)/mcount : 100);
	printf ("%d methods contain lmf pointers (%d%%)\n", lmfcount, mcount ? (lmfcount*100)/mcount : 100);
	printf ("%d methods have other problems (%d%%)\n", ocount, mcount ? (ocount*100)/mcount : 100);
	//printf ("Retained input file.\n");
	unlink (tmpfname);

	return 0;
}

#else
/* AOT disabled */

void
mono_aot_init (void)
{
}

MonoJitInfo*
mono_aot_get_method (MonoDomain *domain, MonoMethod *method)
{
	return NULL;
}

int
mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options)
{
	return 0;
}
#endif

