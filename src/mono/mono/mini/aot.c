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
	GHashTable *methods;
	/* Pointer to the Global Offset Table */
	gpointer *got;
	guint32 got_size;
	char **icall_table;
	MonoAssemblyName *image_names;
	char **image_guids;
	MonoImage **image_table;
	gboolean out_of_date;
	guint8 *code;
	guint32 *code_offsets;
	guint8 *method_infos;
	guint32 *method_info_offsets;
} MonoAotModule;

typedef struct MonoAotCompile {
	FILE *fp;
	GHashTable *ref_hash;
	GHashTable *icall_hash;
	GPtrArray *icall_table;
	GHashTable *image_hash;
	GPtrArray *image_table;
	guint32 got_offset;
	guint32 *method_got_offsets;
} MonoAotCompile;

typedef struct MonoAotOptions {
	char *outfile;
} MonoAotOptions;

static GHashTable *aot_modules;

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

static gboolean 
is_got_patch (MonoJumpInfoType patch_type)
{
#ifdef __x86_64__
	return TRUE;
#elif defined(__i386__)
	return TRUE;
#else
	return FALSE;
#endif
}

/*****************************************************/
/*                 AOT RUNTIME                       */
/*****************************************************/

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


static inline gint32
decode_value (char *_ptr, char **rptr)
{
	unsigned char *ptr = (unsigned char *) _ptr;
	unsigned char b = *ptr;
	gint32 len;
	
	if ((b & 0x80) == 0){
		len = b;
		++ptr;
	} else if ((b & 0x40) == 0){
		len = ((b & 0x3f) << 8 | ptr [1]);
		ptr += 2;
	} else if (b != 0xff) {
		len = ((b & 0x1f) << 24) |
			(ptr [1] << 16) |
			(ptr [2] << 8) |
			ptr [3];
		ptr += 4;
	}
	else {
		len = (ptr [1] << 24) | (ptr [2] << 16) | (ptr [3] << 8) | ptr [4];
		ptr += 5;
	}
	if (rptr)
		*rptr = ptr;

	//printf ("DECODE: %d.\n", len);
	return len;
}

static MonoClass*
decode_klass_info (MonoAotModule *module, char *buf, char **endbuf)
{
	MonoImage *image;
	MonoClass *klass;
	guint32 token, rank, image_index;

	image_index = decode_value (buf, &buf);
	image = load_image (module, image_index);
	if (!image)
		return NULL;
	token = decode_value (buf, &buf);
	if (mono_metadata_token_code (token) == 0) {
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF + token);
	} else {
		token = MONO_TOKEN_TYPE_DEF + decode_value (buf, &buf);
		rank = decode_value (buf, &buf);
		klass = mono_class_get (image, token);
		g_assert (klass);
		klass = mono_array_class_get (klass, rank);
	}
	g_assert (klass);
	mono_class_init (klass);

	*endbuf = buf;
	return klass;
}

static MonoClassField*
decode_field_info (MonoAotModule *module, char *buf, char **endbuf)
{
	MonoClass *klass = decode_klass_info (module, buf, &buf);
	guint32 token;

	if (!klass)
		return NULL;

	token = MONO_TOKEN_FIELD_DEF + decode_value (buf, &buf);

	*endbuf = buf;

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
	guint32 *got_size_ptr = NULL;
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

#ifdef MONO_ARCH_HAVE_PIC_AOT
	g_module_symbol (assembly->aot_module, "got_addr", (gpointer *)&got_addr);
	g_assert (got_addr);
	got = (gpointer*)*got_addr;
	g_assert (got);
	g_module_symbol (assembly->aot_module, "got_size", (gpointer *)&got_size_ptr);
	g_assert (got_size_ptr);
#endif

	info = g_new0 (MonoAotModule, 1);
	info->aot_name = aot_name;
	info->methods = g_hash_table_new (NULL, NULL);
#ifdef MONO_ARCH_HAVE_PIC_AOT
	info->got = got;
	info->got_size = *got_size_ptr;
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

	/* Read method and method_info tables */
	g_module_symbol (assembly->aot_module, "method_offsets", (gpointer*)&info->code_offsets);
	g_module_symbol (assembly->aot_module, "methods", (gpointer*)&info->code);
	g_module_symbol (assembly->aot_module, "method_info_offsets", (gpointer*)&info->method_info_offsets);
	g_module_symbol (assembly->aot_module, "method_infos", (gpointer*)&info->method_infos);

	EnterCriticalSection (&aot_mutex);
	g_hash_table_insert (aot_modules, assembly, info);
	LeaveCriticalSection (&aot_mutex);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT loaded AOT Module for %s.\n", assembly->image->name);
}

void
mono_aot_init (void)
{
	InitializeCriticalSection (&aot_mutex);
	aot_modules = g_hash_table_new (NULL, NULL);

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
	
	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, ass);

	g_assert (klass->inited);

	if ((domain != mono_get_root_domain ()) && (!(aot_module->opts & MONO_OPT_SHARED)))
		/* Non shared AOT code can't be used in other appdomains */
		return NULL;

	minfo = g_hash_table_lookup (aot_module->methods, method);
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

	if (aot_module->out_of_date)
		return NULL;

	if (aot_module->code_offsets [mono_metadata_token_index (method->token) - 1] == 0xffffffff) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT NOT FOUND: %s.\n", mono_method_full_name (method, TRUE));
		return NULL;
	}

	code = &aot_module->code [aot_module->code_offsets [mono_metadata_token_index (method->token) - 1]];
	info = &aot_module->method_infos [aot_module->method_info_offsets [mono_metadata_token_index (method->token) - 1]];

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
	gboolean non_got_patches;
	char *p;

	minfo = g_new0 (MonoAotMethod, 1);

	minfo->domain = domain;
	jinfo = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));

	p = (char*)info;
	code_len = decode_value (p, &p);
	used_int_regs = decode_value (p, &p);

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

		jinfo->exvar_offset = decode_value (p, &p);

		for (i = 0; i < header->num_clauses; ++i) {
			MonoExceptionClause *ec = &header->clauses [i];				
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];

			ei->flags = ec->flags;
			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				ei->data.filter = code + decode_value (p, &p);
			else
				ei->data.catch_class = ec->data.catch_class;

			ei->try_start = code + decode_value (p, &p);
			ei->try_end = code + decode_value (p, &p);
			ei->handler_start = code + decode_value (p, &p);
		}
	}

	if (aot_module->opts & MONO_OPT_SHARED)
		used_strings = decode_value (p, &p);
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint token = decode_value (p, &p);
		mono_ldstr (mono_get_root_domain (), klass->image, mono_metadata_token_index (token));
	}

#ifdef MONO_ARCH_HAVE_PIC_AOT
	got_index = decode_value (p, &p);
#endif

	if (*p) {
		MonoImage *image;
		gpointer *table;
		int i;
		guint32 last_offset, buf_len;

		if (aot_module->opts & MONO_OPT_SHARED)
			mp = mono_mempool_new ();
		else
			mp = domain->mp;

		/* First load the type + offset table */
		last_offset = 0;
		patches = g_ptr_array_new ();
		while (*p) {
			MonoJumpInfo *ji = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));

#if defined(MONO_ARCH_HAVE_PIC_AOT)
			ji->type = *p;
			p ++;
#else
			guint8 b1, b2;

			b1 = *(guint8*)p;
			b2 = *((guint8*)p + 1);
			p += 2;

			ji->type = b1 >> 2;

			if (((b1 & (1 + 2)) == 3) && (b2 == 255))
				ji->ip.i = decode_value (p, &p);
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

		/* Null terminated array */
		p ++;

		/* Then load the other data */
		for (pindex = 0; pindex < patches->len; ++pindex) {
			MonoJumpInfo *ji = g_ptr_array_index (patches, pindex);

			switch (ji->type) {
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IID:
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_CLASS_INIT:
				ji->data.klass = decode_klass_info (aot_module, p, &p);
				if (!ji->data.klass)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_IMAGE:
				ji->data.image = load_image (aot_module, decode_value (p, &p));
				if (!ji->data.image)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_METHOD_JUMP: {
				guint32 image_index, token, value;

				value = decode_value (p, &p);
				image_index = value >> 24;
				token = MONO_TOKEN_METHOD_DEF | (value & 0xffffff);

				image = load_image (aot_module, image_index);
				if (!image)
					goto cleanup;
				ji->data.method = mono_get_method (image, token, NULL);
				g_assert (ji->data.method);
				mono_class_init (ji->data.method->klass);

				break;
			}
			case MONO_PATCH_INFO_WRAPPER: {
				guint32 wrapper_type;

				wrapper_type = decode_value (p, &p);

				switch (wrapper_type) {
				case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: {
					guint32 image_index, token, value;

					value = decode_value (p, &p);
					image_index = value >> 24;
					token = MONO_TOKEN_METHOD_DEF | (value & 0xffffff);

					image = load_image (aot_module, image_index);
					if (!image)
						goto cleanup;
					ji->data.method = mono_get_method (image, token, NULL);
					g_assert (ji->data.method);
					mono_class_init (ji->data.method->klass);

					ji->type = MONO_PATCH_INFO_METHOD;
					ji->data.method = mono_marshal_get_remoting_invoke_with_check (ji->data.method);
					break;
				}
				case MONO_WRAPPER_PROXY_ISINST: {
					MonoClass *klass = decode_klass_info (aot_module, p, &p);
					if (!klass)
						goto cleanup;
					ji->type = MONO_PATCH_INFO_METHOD;
					ji->data.method = mono_marshal_get_proxy_cancast (klass);
					break;
				}
				case MONO_WRAPPER_LDFLD:
				case MONO_WRAPPER_STFLD:
				case MONO_WRAPPER_ISINST: {
					MonoClass *klass = decode_klass_info (aot_module, p, &p);
					if (!klass)
						goto cleanup;
					ji->type = MONO_PATCH_INFO_METHOD;
					if (wrapper_type == MONO_WRAPPER_LDFLD)
						ji->data.method = mono_marshal_get_ldfld_wrapper (&klass->byval_arg);
					else if (wrapper_type == MONO_WRAPPER_STFLD)
						ji->data.method = mono_marshal_get_stfld_wrapper (&klass->byval_arg);
					else
						ji->data.method = mono_marshal_get_isinst (klass);
					break;
				}
				case MONO_WRAPPER_STELEMREF:
					ji->type = MONO_PATCH_INFO_METHOD;
					ji->data.method = mono_marshal_get_stelemref ();
					break;
				default:
					g_assert_not_reached ();
				}
				break;
			}
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_SFLDA:
				ji->data.field = decode_field_info (aot_module, p, &p);
				if (!ji->data.field)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_INTERNAL_METHOD:
				ji->data.name = aot_module->icall_table [decode_value (p, &p)];
				g_assert (ji->data.name);
				//printf ("A: %s.\n", ji->data.name);
				break;
			case MONO_PATCH_INFO_SWITCH:
				ji->data.table = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoBBTable));
				ji->data.table->table_size = decode_value (p, &p);
				table = g_new (gpointer, ji->data.table->table_size);
				ji->data.table->table = (MonoBasicBlock**)table;
				for (i = 0; i < ji->data.table->table_size; i++)
					table [i] = (gpointer)(gssize)decode_value (p, &p);
				break;
			case MONO_PATCH_INFO_R4:
				ji->data.target = mono_mempool_alloc0 (mp, sizeof (float));
				guint32 val;

				val = decode_value (p, &p);
				*(float*)ji->data.target = *(float*)&val;
				break;
			case MONO_PATCH_INFO_R8: {
				ji->data.target = mono_mempool_alloc0 (mp, sizeof (double));
				guint32 val [2];

				val [0] = decode_value (p, &p);
				val [1] = decode_value (p, &p);
				*(double*)ji->data.target = *(double*)val;
				break;
			}
			case MONO_PATCH_INFO_LDSTR:
				image = load_image (aot_module, decode_value (p, &p));
				if (!image)
					goto cleanup;
				ji->data.token = mono_jump_info_token_new (mp, image, MONO_TOKEN_STRING + decode_value (p, &p));
				break;
			case MONO_PATCH_INFO_LDTOKEN:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
				image = load_image (aot_module, decode_value (p, &p));
				if (!image)
					goto cleanup;
				ji->data.token = mono_jump_info_token_new (mp, image, decode_value (p, &p));
				break;
			case MONO_PATCH_INFO_EXC_NAME:
				ji->data.klass = decode_klass_info (aot_module, p, &p);
				if (!ji->data.klass)
					goto cleanup;
				ji->data.name = ji->data.klass->name;
				break;
			case MONO_PATCH_INFO_METHOD_REL:
				ji->data.offset = decode_value (p, &p);
				break;
			default:
				g_warning ("unhandled type %d", ji->type);
				g_assert_not_reached ();
			}
		}

		buf_len = decode_value (p, &p);
		mono_debug_add_aot_method (domain, method, code, p, buf_len);

#if MONO_ARCH_HAVE_PIC_AOT
		mono_arch_flush_icache (code, code_len);

		if (non_got_patches)
			make_writable (code, code_len);

		/* Do this outside the lock to avoid deadlocks */
		LeaveCriticalSection (&aot_mutex);
		non_got_patches = FALSE;
		for (pindex = 0; pindex < patches->len; ++pindex) {
			MonoJumpInfo *ji = g_ptr_array_index (patches, pindex);

			if (is_got_patch (ji->type)) {
				aot_module->got [got_index] = mono_resolve_patch_target (method, domain, code, ji, TRUE);
				got_index ++;
				ji->type = MONO_PATCH_INFO_NONE;
			}
			else
				non_got_patches = TRUE;
		}
		if (non_got_patches) {
			make_writable (code, code_len);
			mono_arch_patch_code (method, domain, code, patch_info, TRUE);
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
		jinfo->domain_neutral = 0;
#else
		jinfo->domain_neutral = (aot_module->opts & MONO_OPT_SHARED) != 0;
#endif

		minfo->info = jinfo;
		g_hash_table_insert (aot_module->methods, method, minfo);

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

gboolean
mono_aot_is_got_entry (guint8 *code, guint8 *addr)
{
	MonoJitInfo *ji;
	MonoAssembly *ass;
	MonoAotModule *aot_module;

	ji = mono_jit_info_table_find (mono_domain_get (), code);
	if (!ji)
		return FALSE;

	ass = ji->method->klass->image->assembly;

	if (!aot_modules)
		return FALSE;
	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, ass);
	if (!aot_module || !aot_module->got)
		return FALSE;

	return ((addr >= (guint8*)(aot_module->got)) && (addr < (guint8*)(aot_module->got + aot_module->got_size)));
}

/*****************************************************/
/*                 AOT COMPILER                      */
/*****************************************************/

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
#elif defined(__x86_64__) || defined(__i386__)
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

static void
emit_string_symbol (FILE *fp, const char *name, const char *value)
{
	emit_section_change (fp, ".text", 1);
	emit_global(fp, name, FALSE);
	emit_label(fp, name);
	fprintf (fp, "\t%s \"%s\"\n", AS_STRING_DIRECTIVE, value);
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

static inline void
encode_value (gint32 value, char *buf, char **endbuf)
{
	char *p = buf;

	//printf ("ENCODE: %d 0x%x.\n", value, value);

	/* 
	 * Same encoding as the one used in the metadata, extended to handle values
	 * greater than 0x1fffffff.
	 */
	if ((value >= 0) && (value <= 127))
		*p++ = value;
	else if ((value >= 0) && (value <= 16384)) {
		p [0] = 0x80 | (value >> 8);
		p [1] = value & 0xff;
		p += 2;
	} else if ((value >= 0) && (value <= 0x1fffffff)) {
		p [0] = (value >> 24) | 0xc0;
		p [1] = (value >> 16) & 0xff;
		p [2] = (value >> 8) & 0xff;
		p [3] = value & 0xff;
		p += 4;
	}
	else {
		p [0] = 0xff;
		p [1] = (value >> 24) & 0xff;
		p [2] = (value >> 16) & 0xff;
		p [3] = (value >> 8) & 0xff;
		p [4] = value & 0xff;
		p += 5;
	}
	if (endbuf)
		*endbuf = p;
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
encode_klass_info (MonoAotCompile *cfg, MonoClass *klass, char *buf, char **endbuf)
{
	encode_value (get_image_index (cfg, klass->image), buf, &buf);
	if (!klass->type_token) {
		/* Array class */
		g_assert (klass->rank > 0);
		g_assert (klass->element_class->type_token);
		encode_value (MONO_TOKEN_TYPE_DEF, buf, &buf);
		g_assert (mono_metadata_token_code (klass->element_class->type_token) == MONO_TOKEN_TYPE_DEF);
		encode_value (klass->element_class->type_token - MONO_TOKEN_TYPE_DEF, buf, &buf);
		encode_value (klass->rank, buf, &buf);
	}
	else {
		g_assert (mono_metadata_token_code (klass->type_token) == MONO_TOKEN_TYPE_DEF);
		encode_value (klass->type_token - MONO_TOKEN_TYPE_DEF, buf, &buf);
	}
	*endbuf = buf;
}

static void
encode_field_info (MonoAotCompile *cfg, MonoClassField *field, char *buf, char **endbuf)
{
	guint32 token = mono_get_field_token (field);

	encode_klass_info (cfg, field->parent, buf, &buf);
	g_assert (mono_metadata_token_code (token) == MONO_TOKEN_FIELD_DEF);
	encode_value (token - MONO_TOKEN_FIELD_DEF, buf, &buf);
	*endbuf = buf;
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
emit_method_code (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	FILE *tmpfp;
	int i, j, pindex;
	guint8 *code, *mname, *mname_p;
	int func_alignment = 16;
	GPtrArray *patches;
	MonoJumpInfo *patch_info;
	MonoMethodHeader *header;
#ifdef MONO_ARCH_HAVE_PIC_AOT
	gboolean skip;
#endif

	tmpfp = acfg->fp;
	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	emit_section_change (tmpfp, ".text", 0);

	/* Make the labels local */
	mname = g_strdup_printf (".Lm_%x", mono_metadata_token_index (method->token));
	mname_p = g_strdup_printf ("%s_p", mname);

	emit_alignment(tmpfp, func_alignment);
	emit_label(tmpfp, mname);

	if (cfg->verbose_level > 0)
		g_print ("Emitted as %s\n", mname);

	/* Sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

#ifdef MONO_ARCH_HAVE_PIC_AOT
	acfg->method_got_offsets [mono_metadata_token_index (method->token)] = acfg->got_offset;
	for (i = 0; i < cfg->code_len; i++) {
		patch_info = NULL;
		for (pindex = 0; pindex < patches->len; ++pindex) {
			patch_info = g_ptr_array_index (patches, pindex);
			if (patch_info->ip.i == i)
				break;
		}

		skip = FALSE;
		if (patch_info && (pindex < patches->len)) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_LABEL:
			case MONO_PATCH_INFO_BB:
			case MONO_PATCH_INFO_NONE:
				break;
			case MONO_PATCH_INFO_GOT_OFFSET: {
				guint32 offset = mono_arch_get_patch_offset (code + i);
				for (j = 0; j < offset; ++j)
					fprintf (tmpfp, ".byte 0x%x\n", (unsigned int) code [i + j]);
				fprintf (tmpfp, ".int got - . + %d\n", offset);

				i += offset + 4 - 1;
				skip = TRUE;
				break;
			}
			default:
				if (!is_got_patch (patch_info->type))
					break;

				for (j = 0; j < mono_arch_get_patch_offset (code + i); ++j)
					fprintf (tmpfp, ".byte 0x%x\n", (unsigned int) code [i + j]);
#ifdef __x86_64__
				fprintf (tmpfp, ".int got - . + %d\n", (unsigned int) ((acfg->got_offset * sizeof (gpointer)) - 4));
#elif defined(__i386__)
				fprintf (tmpfp, ".int %d\n", (unsigned int) ((acfg->got_offset * sizeof (gpointer))));
#endif
				acfg->got_offset ++;

				i += mono_arch_get_patch_offset (code + i) + 4 - 1;
				skip = TRUE;
			}
		}

		if (!skip)
			fprintf (tmpfp, ".byte 0x%x\n", (unsigned int) code [i]);
	}
#else
	for (i = 0; i < cfg->code_len; i++) {
		fprintf (tmpfp, ".byte 0x%x\n", (unsigned int) code [i]);
	}
#endif
}

static void
emit_method_info (MonoAotCompile *acfg, MonoCompile *cfg)
{
	MonoMethod *method;
	GList *l;
	FILE *tmpfp;
	int i, j, k, pindex, buf_size;
	guint32 debug_info_size;
	guint8 *code, *mname, *mname_p;
	GPtrArray *patches;
	MonoJumpInfo *patch_info;
	MonoMethodHeader *header;
	guint32 last_offset;
	char *p, *buf;
	guint8 *debug_info;
#ifdef MONO_ARCH_HAVE_PIC_AOT
	guint32 first_got_offset;
#endif

	tmpfp = acfg->fp;
	method = cfg->method;
	code = cfg->native_code;
	header = mono_method_get_header (method);

	emit_section_change (tmpfp, ".text", 0);

	/* Make the labels local */
	mname = g_strdup_printf (".Lm_%x", mono_metadata_token_index (method->token));
	mname_p = g_strdup_printf ("%s_p", mname);

	/* Sort relocations */
	patches = g_ptr_array_new ();
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next)
		g_ptr_array_add (patches, patch_info);
	g_ptr_array_sort (patches, compare_patches);

#ifdef MONO_ARCH_HAVE_PIC_AOT
	first_got_offset = acfg->method_got_offsets [mono_metadata_token_index (cfg->method->token)];
#endif

	/**********************/
	/* Encode method info */
	/**********************/

	buf_size = 4096;
	p = buf = g_malloc (buf_size);

	encode_value (cfg->code_len, p, &p);
	encode_value (cfg->used_int_regs, p, &p);

	/* Exception table */
	if (header->num_clauses) {
		MonoJitInfo *jinfo = cfg->jit_info;

		encode_value (jinfo->exvar_offset, p, &p);

		for (k = 0; k < header->num_clauses; ++k) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [k];

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				encode_value ((gint)((guint8*)ei->data.filter - code), p, &p);

			encode_value ((gint)((guint8*)ei->try_start - code), p, &p);
			encode_value ((gint)((guint8*)ei->try_end - code), p, &p);
			encode_value ((gint)((guint8*)ei->handler_start - code), p, &p);
		}
	}

	/* String table */
	if (cfg->opt & MONO_OPT_SHARED) {
		encode_value (g_list_length (cfg->ldstr_list), p, &p);
		for (l = cfg->ldstr_list; l; l = l->next) {
			encode_value ((long)l->data, p, &p);
		}
	}
	else
		/* Used only in shared mode */
		g_assert (!cfg->ldstr_list);

#ifdef MONO_ARCH_HAVE_PIC_AOT
	encode_value (first_got_offset, p, &p);
#endif

	/* First emit the type+position table */
	last_offset = 0;
	j = 0;
	for (pindex = 0; pindex < patches->len; ++pindex) {
		guint32 offset;
		patch_info = g_ptr_array_index (patches, pindex);
		
		if ((patch_info->type == MONO_PATCH_INFO_LABEL) ||
			(patch_info->type == MONO_PATCH_INFO_BB) ||
			(patch_info->type == MONO_PATCH_INFO_GOT_OFFSET) ||
			(patch_info->type == MONO_PATCH_INFO_NONE))
			/* Nothing to do */
			continue;

		j ++;
		//printf ("T: %d O: %d.\n", patch_info->type, patch_info->ip.i);
		offset = patch_info->ip.i - last_offset;
		last_offset = patch_info->ip.i;

#if defined(MONO_ARCH_HAVE_PIC_AOT)
		/* Only the type is needed */
		*p = patch_info->type;
		p++;
#else
		/* Encode type+position compactly */
		g_assert (patch_info->type < 64);
		if (offset < 1024 - 1) {
			*p = (patch_info->type << 2) + (offset >> 8);
			p++;
			*p = offset & ((1 << 8) - 1);
			p ++;
		}
		else {
			*p = (patch_info->type << 2) + 3;
			p ++;
			*p = 255;
			p ++;
			encode_value (offset, p, &p);
		}
#endif
	}

	/*
	 * 0 is PATCH_INFO_BB, which can't be in the file.
	 */
	/* NULL terminated array */
	*p = 0;
	p ++;

	/* Then emit the other info */
	for (pindex = 0; pindex < patches->len; ++pindex) {
		patch_info = g_ptr_array_index (patches, pindex);

		switch (patch_info->type) {
		case MONO_PATCH_INFO_LABEL:
		case MONO_PATCH_INFO_BB:
		case MONO_PATCH_INFO_GOT_OFFSET:
		case MONO_PATCH_INFO_NONE:
			break;
		case MONO_PATCH_INFO_IMAGE:
			encode_value (get_image_index (acfg, patch_info->data.image), p, &p);
			break;
		case MONO_PATCH_INFO_METHOD_REL:
			encode_value ((gint)patch_info->data.offset, p, &p);
			break;
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table = (gpointer *)patch_info->data.table->table;
			int k;

			encode_value (patch_info->data.table->table_size, p, &p);
			for (k = 0; k < patch_info->data.table->table_size; k++)
				encode_value ((int)(gssize)table [k], p, &p);
			break;
		}
		case MONO_PATCH_INFO_METHODCONST:
		case MONO_PATCH_INFO_METHOD:
		case MONO_PATCH_INFO_METHOD_JUMP: {
			guint32 image_index = get_image_index (acfg, patch_info->data.method->klass->image);
			guint32 token = patch_info->data.method->token;
			g_assert (image_index < 256);
			g_assert (mono_metadata_token_table (token) == MONO_TABLE_METHOD);

			encode_value ((image_index << 24) + (mono_metadata_token_index (token)), p, &p);
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
			encode_value (icall_index - 1, p, &p);
			break;
		}
		case MONO_PATCH_INFO_LDSTR: {
			guint32 image_index = get_image_index (acfg, patch_info->data.token->image);
			guint32 token = patch_info->data.token->token;
			g_assert (mono_metadata_token_code (token) == MONO_TOKEN_STRING);
			/* 
			 * An optimization would be to emit shared code for ldstr 
			 * statements followed by a throw.
			 */
			encode_value (image_index, p, &p);
			encode_value (patch_info->data.token->token - MONO_TOKEN_STRING, p, &p);
			break;
		}
		case MONO_PATCH_INFO_LDTOKEN:
		case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
			encode_value (get_image_index (acfg, patch_info->data.token->image), p, &p);
			encode_value (patch_info->data.token->token, p, &p);
			break;
		case MONO_PATCH_INFO_EXC_NAME: {
			MonoClass *ex_class;

			ex_class =
				mono_class_from_name (mono_defaults.exception_class->image,
									  "System", patch_info->data.target);
			g_assert (ex_class);
			encode_klass_info (acfg, ex_class, p, &p);
			break;
		}
		case MONO_PATCH_INFO_R4:
			encode_value (*((guint32 *)patch_info->data.target), p, &p);
			break;
		case MONO_PATCH_INFO_R8:
			encode_value (*((guint32 *)patch_info->data.target), p, &p);
			encode_value (*(((guint32 *)patch_info->data.target) + 1), p, &p);
			break;
		case MONO_PATCH_INFO_VTABLE:
		case MONO_PATCH_INFO_CLASS_INIT:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IID:
			encode_klass_info (acfg, patch_info->data.klass, p, &p);
			break;
		case MONO_PATCH_INFO_FIELD:
		case MONO_PATCH_INFO_SFLDA:
			encode_field_info (acfg, patch_info->data.field, p, &p);
			break;
		case MONO_PATCH_INFO_WRAPPER: {
			encode_value (patch_info->data.method->wrapper_type, p, &p);

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

				encode_value ((image_index << 24) + (mono_metadata_token_index (token)), p, &p);
				break;
			}
			case MONO_WRAPPER_PROXY_ISINST:
			case MONO_WRAPPER_LDFLD:
			case MONO_WRAPPER_STFLD:
			case MONO_WRAPPER_ISINST: {
				MonoClass *proxy_class = (MonoClass*)mono_marshal_method_from_wrapper (patch_info->data.method);
				encode_klass_info (acfg, proxy_class, p, &p);
				break;
			}
			case MONO_WRAPPER_STELEMREF:
				break;
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

	mono_debug_serialize_debug_info (cfg, &debug_info, &debug_info_size);

	encode_value (debug_info_size, p, &p);
	if (debug_info_size) {
		memcpy (p, debug_info, debug_info_size);
		p += debug_info_size;
		g_free (debug_info);
	}

	/* Emit method info */

	emit_section_change (tmpfp, ".text", 1);
	emit_label (tmpfp, mname_p);

	g_assert (p - buf < buf_size);
	for (i = 0; i < p - buf; ++i)
		fprintf (tmpfp, ".byte %d\n", (unsigned int) buf [i]);	
	g_free (buf);

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
	MonoCompile **cfgs;
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

	emit_string_symbol (tmpfp, "mono_assembly_guid" , image->guid);

	emit_string_symbol (tmpfp, "mono_aot_version", MONO_AOT_FILE_VERSION);

	opts_str = g_strdup_printf ("%d", opts);
	emit_string_symbol (tmpfp, "mono_aot_opt_flags", opts_str);
	g_free (opts_str);

	symbol = g_strdup_printf ("methods");
	emit_section_change (tmpfp, ".text", 0);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label (tmpfp, symbol);

	symbol = g_strdup_printf ("method_infos");
	emit_section_change (tmpfp, ".text", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label (tmpfp, symbol);

	cfgs = g_new0 (MonoCompile*, image->tables [MONO_TABLE_METHOD].rows + 32);
	acfg->method_got_offsets = g_new0 (guint32, image->tables [MONO_TABLE_METHOD].rows + 32);

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

		/*
		 * Since these methods are the only ones which are compiled with
		 * AOT support, and they are not used by runtime startup/shutdown code,
		 * the runtime will not see AOT methods during AOT compilation,so it
		 * does not need to support them by creating a fake GOT etc.
		 */
		cfg = mini_method_compile (method, opts, mono_get_root_domain (), FALSE, TRUE, 0);
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
				case MONO_WRAPPER_STELEMREF:
				case MONO_WRAPPER_ISINST:
				case MONO_WRAPPER_PROXY_ISINST:
					patch_info->type = MONO_PATCH_INFO_WRAPPER;
					break;
				}
			}
		}

		skip = FALSE;
		for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_METHOD:
			case MONO_PATCH_INFO_METHODCONST:
				if (patch_info->data.method->wrapper_type) {
					/* unable to handle this */
					//printf ("Skip (wrapper call):   %s %d -> %s\n", mono_method_full_name (method, TRUE), patch_info->type, mono_method_full_name (patch_info->data.method, TRUE));
					skip = TRUE;
					break;
				}
				if (!patch_info->data.method->token)
					/*
					 * The method is part of a constructed type like Int[,].Set (). It doesn't
					 * have a token, and we can't make one, since the parent type is part of
					 * assembly which contains the element type, and not the assembly which
					 * referenced this type.
					 */
					skip = TRUE;
				break;
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_CLASS_INIT:
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IID:
				if (!patch_info->data.klass->type_token)
					if (!patch_info->data.klass->element_class->type_token)
						skip = TRUE;
				break;
			default:
				break;
			}
		}

		if (skip) {
			wrappercount++;
			mono_destroy_compile (cfg);
			continue;
		}

		//printf ("Compile:           %s\n", mono_method_full_name (method, TRUE));

		cfgs [i] = cfg;

		ccount++;
	}

	/* Emit code */
	for (i = 0; i < image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (cfgs [i])
			emit_method_code (acfg, cfgs [i]);
	}

	/* Emit method info */
	for (i = 0; i < image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (cfgs [i])
			emit_method_info (acfg, cfgs [i]);
	}

	/*
	 * The icall and image tables are small but referenced in a lot of places.
	 * So we emit them at once, and reference their elements by an index.
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

#ifdef MONO_ARCH_HAVE_PIC_AOT
	/* Emit GOT */

	/* Don't make GOT global so accesses to it don't need relocations */
	symbol = g_strdup_printf ("got");
#ifdef __x86_64__
	emit_section_change (tmpfp, ".bss", 1);
#else
	emit_section_change (tmpfp, ".data", 1);
#endif
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);
	if (acfg->got_offset > 0)
		fprintf (tmpfp, ".skip %d\n", (int)(acfg->got_offset * sizeof (gpointer)));

	symbol = g_strdup_printf ("got_addr");
	emit_section_change (tmpfp, ".data", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);
	emit_pointer (tmpfp, "got");

	symbol = g_strdup_printf ("got_size");
	emit_section_change (tmpfp, ".data", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);
	fprintf (tmpfp, ".long %d\n", acfg->got_offset * sizeof (gpointer));
#endif

	symbol = g_strdup_printf ("method_offsets");
	emit_section_change (tmpfp, ".text", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);

	for (i = 0; i < image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (cfgs [i]) {
			symbol = g_strdup_printf (".Lm_%x", i + 1);
			fprintf (tmpfp, ".long %s - methods\n", symbol);
		}
		else
			fprintf (tmpfp, ".long 0xffffffff\n");
	}

	symbol = g_strdup_printf ("method_info_offsets");
	emit_section_change (tmpfp, ".text", 1);
	emit_global (tmpfp, symbol, FALSE);
	emit_alignment (tmpfp, 8);
	emit_label(tmpfp, symbol);

	for (i = 0; i < image->tables [MONO_TABLE_METHOD].rows; ++i) {
		if (cfgs [i]) {
			symbol = g_strdup_printf (".Lm_%x_p", i + 1);
			fprintf (tmpfp, ".long %s - method_infos\n", symbol);
		}
		else
			fprintf (tmpfp, ".long 0\n");
	}

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

gboolean
mono_aot_is_got_entry (guint8 *code, guint8 *addr)
{
	return FALSE;
}

int
mono_compile_assembly (MonoAssembly *ass, guint32 opts, const char *aot_options)
{
	return 0;
}
#endif

