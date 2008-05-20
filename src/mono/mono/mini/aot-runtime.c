/*
 * aot.c: mono Ahead of Time compiler
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include "config.h"
#include <sys/types.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <fcntl.h>
#include <string.h>
#ifndef PLATFORM_WIN32
#include <sys/mman.h>
#else
#include <winsock2.h>
#include <windows.h>
#endif

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

#include <errno.h>
#include <sys/stat.h>
#include <limits.h>    /* for PAGESIZE */
#ifndef PAGESIZE
#define PAGESIZE 4096
#endif

#ifdef HAVE_SYS_WAIT_H
#include <sys/wait.h>  /* for WIFEXITED, WEXITSTATUS */
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
#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-logger.h>
#include "mono/utils/mono-compiler.h"

#include "mini.h"
#include "version.h"

#ifndef DISABLE_AOT

#ifdef PLATFORM_WIN32
#define SHARED_EXT ".dll"
#elif (defined(__ppc__) || defined(__ppc64__)) && defined(__MACH__)
#define SHARED_EXT ".dylib"
#else
#define SHARED_EXT ".so"
#endif

#define ALIGN_PTR_TO(ptr,align) (gpointer)((((gssize)(ptr)) + (align - 1)) & (~(align - 1)))
#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))

typedef struct MonoAotModule {
	char *aot_name;
	/* Optimization flags used to compile the module */
	guint32 opts;
	/* Pointer to the Global Offset Table */
	gpointer *got;
	guint32 got_size;
	GHashTable *name_cache;
	MonoAssemblyName *image_names;
	char **image_guids;
	MonoImage **image_table;
	guint32 image_table_len;
	gboolean out_of_date;
	gboolean plt_inited;
	guint8 *mem_begin;
	guint8 *mem_end;
	guint8 *code;
	guint8 *code_end;
	guint8 *plt;
	guint8 *plt_end;
	guint8 *plt_info;
	guint8 *plt_jump_table;
	guint32 plt_jump_table_size;
	guint32 *code_offsets;
	guint8 *method_info;
	guint32 *method_info_offsets;
	guint8 *got_info;
	guint32 *got_info_offsets;
	guint8 *ex_info;
	guint32 *ex_info_offsets;
	guint32 *method_order;
	guint32 *method_order_end;
	guint8 *class_info;
	guint32 *class_info_offsets;
	guint32 *methods_loaded;
	guint16 *class_name_table;
} MonoAotModule;

static GHashTable *aot_modules;
#define mono_aot_lock() EnterCriticalSection (&aot_mutex)
#define mono_aot_unlock() LeaveCriticalSection (&aot_mutex)
static CRITICAL_SECTION aot_mutex;

/*
 * Disabling this will make a copy of the loaded code and use the copy instead 
 * of the original. This will place the caller and the callee close to each 
 * other in memory, possibly improving cache behavior. Since the original
 * code is in copy-on-write memory, this will not increase the memory usage
 * of the runtime.
 */
static gboolean use_loaded_code = TRUE;

/*
 * Whenever to AOT compile loaded assemblies on demand and store them in
 * a cache under $HOME/.mono/aot-cache.
 */
static gboolean use_aot_cache = FALSE;

/*
 * Whenever to spawn a new process to AOT a file or do it in-process. Only relevant if
 * use_aot_cache is TRUE.
 */
static gboolean spawn_compiler = TRUE;

/* For debugging */
static gint32 mono_last_aot_method = -1;

static gboolean make_unreadable = FALSE;
static guint32 n_pagefaults = 0;
static guint32 name_table_accesses = 0;

/* Used to speed-up find_aot_module () */
static gsize aot_code_low_addr = (gssize)-1;
static gsize aot_code_high_addr = 0;

static gpointer
mono_aot_load_method (MonoDomain *domain, MonoAotModule *aot_module, MonoMethod *method, guint8 *code, guint8 *info);

static void
init_plt (MonoAotModule *info);

static inline gboolean 
is_got_patch (MonoJumpInfoType patch_type)
{
	return TRUE;
}

/*****************************************************/
/*                 AOT RUNTIME                       */
/*****************************************************/

static MonoImage *
load_image (MonoAotModule *module, int index)
{
	MonoAssembly *assembly;
	MonoImageOpenStatus status;

	g_assert (index < module->image_table_len);

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
decode_value (guint8 *ptr, guint8 **rptr)
{
	guint8 b = *ptr;
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
decode_klass_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	MonoImage *image;
	MonoClass *klass, *eklass;
	guint32 token, rank;

	token = decode_value (buf, &buf);
	if (token == 0) {
		*endbuf = buf;
		return NULL;
	}
	if (mono_metadata_token_table (token) == 0) {
		image = load_image (module, decode_value (buf, &buf));
		if (!image)
			return NULL;
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF + token);
	} else if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
		if (token == MONO_TOKEN_TYPE_SPEC) {
			MonoClass *gclass;
			int i;
			MonoGenericContext ctx;
			MonoGenericInst inst;

			gclass = decode_klass_ref (module, buf, &buf);
			g_assert (gclass->generic_container);

			memset (&ctx, 0, sizeof (ctx));
			memset (&inst, 0, sizeof (inst));
			ctx.class_inst = &inst;
			inst.type_argc = decode_value (buf, &buf);
			inst.type_argv = g_new0 (MonoType*, inst.type_argc);
			for (i = 0; i < inst.type_argc; ++i) {
				MonoClass *pclass = decode_klass_ref (module, buf, &buf);
				if (!pclass) {
					g_free (inst.type_argv);
					return NULL;
				}
				inst.type_argv [i] = &pclass->byval_arg;
			}
			klass = mono_class_from_mono_type (mono_class_inflate_generic_type (&gclass->byval_arg, &ctx));
		} else {
			image = load_image (module, decode_value (buf, &buf));
			if (!image)
				return NULL;
			klass = mono_class_get (image, token);
		}
	} else if (token == MONO_TOKEN_TYPE_DEF) {
		/* Array */
		image = load_image (module, decode_value (buf, &buf));
		if (!image)
			return NULL;
		rank = decode_value (buf, &buf);
		eklass = decode_klass_ref (module, buf, &buf);
		klass = mono_array_class_get (eklass, rank);
	} else {
		g_assert_not_reached ();
	}
	g_assert (klass);
	mono_class_init (klass);

	*endbuf = buf;
	return klass;
}

static MonoClassField*
decode_field_info (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	MonoClass *klass = decode_klass_ref (module, buf, &buf);
	guint32 token;

	if (!klass)
		return NULL;

	token = MONO_TOKEN_FIELD_DEF + decode_value (buf, &buf);

	*endbuf = buf;

	return mono_class_get_field (klass, token);
}

static inline MonoImage*
decode_method_ref (MonoAotModule *module, guint32 *token, guint8 *buf, guint8 **endbuf)
{
	guint32 image_index, value;
	MonoImage *image;

	value = decode_value (buf, &buf);
	image_index = value >> 24;
	*token = MONO_TOKEN_METHOD_DEF | (value & 0xffffff);

	if (image_index == 255) {
		/* Methodspec */
		image_index = decode_value (buf, &buf);
		*token = decode_value (buf, &buf);
	}

	*endbuf = buf;

	image = load_image (module, image_index);
	if (!image)
		return NULL;
	else
		return image;
}

G_GNUC_UNUSED
static void
make_writable (guint8* addr, guint32 len)
{
#ifndef PLATFORM_WIN32
	guint8 *page_start;
	int pages, err;

	page_start = (guint8 *) (((gssize) (addr)) & ~ (PAGESIZE - 1));
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
 * - recompile the AOT module if one of its dependencies changes
 */
static MonoDl*
load_aot_module_from_cache (MonoAssembly *assembly, char **aot_name)
{
	char *fname, *cmd, *tmp2, *aot_options;
	const char *home;
	MonoDl *module;
	gboolean res;
	gchar *out, *err;
	gint exit_status;

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
	module = mono_dl_open (fname, MONO_DL_LAZY, NULL);

	if (!module) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT not found.");

		mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT precompiling assembly '%s'... ", assembly->image->name);

		aot_options = g_strdup_printf ("outfile=%s", fname);

		if (spawn_compiler) {
			/* FIXME: security */
			/* FIXME: Has to pass the assembly loading path to the child process */
			cmd = g_strdup_printf ("mono -O=all --aot=%s %s", aot_options, assembly->image->name);

			res = g_spawn_command_line_sync (cmd, &out, &err, &exit_status, NULL);

#if !defined(PLATFORM_WIN32) && !defined(__ppc__) && !defined(__ppc64__) && !defined(__powerpc__)
			if (res) {
				if (!WIFEXITED (exit_status) && (WEXITSTATUS (exit_status) == 0))
					mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT failed: %s.", err);
				else
					mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT succeeded.");
				g_free (out);
				g_free (err);
			}
#endif
			g_free (cmd);
		} else {
			res = mono_compile_assembly (assembly, mono_parse_default_optimizations (NULL), aot_options);
			if (!res) {
				mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT failed.");
			} else {
				mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT succeeded.");
			}
		}

		module = mono_dl_open (fname, MONO_DL_LAZY, NULL);

		g_free (aot_options);
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
	char *runtime_version;
	char *opt_flags = NULL;
	gpointer *plt_jump_table_addr = NULL;
	guint32 *plt_jump_table_size = NULL;
	gpointer *got_addr = NULL;
	gpointer *got = NULL;
	guint32 *got_size_ptr = NULL;
	int i;

	if (mono_compile_aot)
		return;

	if (assembly->aot_module)
		/* 
		 * Already loaded. This can happen because the assembly loading code might invoke
		 * the assembly load hooks multiple times for the same assembly.
		 */
		return;

	if (assembly->image->dynamic)
		return;

	if (use_aot_cache)
		assembly->aot_module = load_aot_module_from_cache (assembly, &aot_name);
	else {
		char *err;
		aot_name = g_strdup_printf ("%s%s", assembly->image->name, SHARED_EXT);

		assembly->aot_module = mono_dl_open (aot_name, MONO_DL_LAZY, &err);

		if (!assembly->aot_module) {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT failed to load AOT module %s: %s\n", aot_name, err);
			g_free (err);
		}
	}

	if (!assembly->aot_module) {
		g_free (aot_name);
		return;
	}

	mono_dl_symbol (assembly->aot_module, "mono_assembly_guid", (gpointer *) &saved_guid);
	mono_dl_symbol (assembly->aot_module, "mono_aot_version", (gpointer *) &aot_version);
	mono_dl_symbol (assembly->aot_module, "mono_aot_opt_flags", (gpointer *)&opt_flags);
	mono_dl_symbol (assembly->aot_module, "mono_runtime_version", (gpointer *)&runtime_version);

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

	if (!runtime_version || ((strlen (runtime_version) > 0 && strcmp (runtime_version, FULL_VERSION)))) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s is compiled against runtime version %s while this runtime has version %s.\n", aot_name, runtime_version, FULL_VERSION);
		usable = FALSE;
	}

	if (!usable) {
		g_free (aot_name);
		mono_dl_close (assembly->aot_module);
		assembly->aot_module = NULL;
		return;
	}

	mono_dl_symbol (assembly->aot_module, "got_addr", (gpointer *)&got_addr);
	g_assert (got_addr);
	got = (gpointer*)*got_addr;
	g_assert (got);
	mono_dl_symbol (assembly->aot_module, "got_size", (gpointer *)&got_size_ptr);
	g_assert (got_size_ptr);

	info = g_new0 (MonoAotModule, 1);
	info->aot_name = aot_name;
	info->got = got;
	info->got_size = *got_size_ptr;
	info->got [0] = assembly->image;

	sscanf (opt_flags, "%d", &info->opts);

	/* Read image table */
	{
		guint32 table_len, i;
		char *table = NULL;

		mono_dl_symbol (assembly->aot_module, "mono_image_table", (gpointer *)&table);
		g_assert (table);

		table_len = *(guint32*)table;
		table += sizeof (guint32);
		info->image_table = g_new0 (MonoImage*, table_len);
		info->image_names = g_new0 (MonoAssemblyName, table_len);
		info->image_guids = g_new0 (char*, table_len);
		info->image_table_len = table_len;
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

	/* Read method and method_info tables */
	mono_dl_symbol (assembly->aot_module, "method_offsets", (gpointer*)&info->code_offsets);
	mono_dl_symbol (assembly->aot_module, "methods", (gpointer*)&info->code);
	mono_dl_symbol (assembly->aot_module, "methods_end", (gpointer*)&info->code_end);
	mono_dl_symbol (assembly->aot_module, "method_info_offsets", (gpointer*)&info->method_info_offsets);
	mono_dl_symbol (assembly->aot_module, "method_info", (gpointer*)&info->method_info);
	mono_dl_symbol (assembly->aot_module, "ex_info_offsets", (gpointer*)&info->ex_info_offsets);
	mono_dl_symbol (assembly->aot_module, "ex_info", (gpointer*)&info->ex_info);
	mono_dl_symbol (assembly->aot_module, "method_order", (gpointer*)&info->method_order);
	mono_dl_symbol (assembly->aot_module, "method_order_end", (gpointer*)&info->method_order_end);
	mono_dl_symbol (assembly->aot_module, "class_info", (gpointer*)&info->class_info);
	mono_dl_symbol (assembly->aot_module, "class_info_offsets", (gpointer*)&info->class_info_offsets);
	mono_dl_symbol (assembly->aot_module, "class_name_table", (gpointer *)&info->class_name_table);
	mono_dl_symbol (assembly->aot_module, "got_info", (gpointer*)&info->got_info);
	mono_dl_symbol (assembly->aot_module, "got_info_offsets", (gpointer*)&info->got_info_offsets);
	mono_dl_symbol (assembly->aot_module, "mem_end", (gpointer*)&info->mem_end);

	info->mem_begin = info->code;

	mono_dl_symbol (assembly->aot_module, "plt", (gpointer*)&info->plt);
	mono_dl_symbol (assembly->aot_module, "plt_end", (gpointer*)&info->plt_end);
	mono_dl_symbol (assembly->aot_module, "plt_info", (gpointer*)&info->plt_info);

	mono_dl_symbol (assembly->aot_module, "plt_jump_table_addr", (gpointer *)&plt_jump_table_addr);
	g_assert (plt_jump_table_addr);
	info->plt_jump_table = (guint8*)*plt_jump_table_addr;
	g_assert (info->plt_jump_table);

	mono_dl_symbol (assembly->aot_module, "plt_jump_table_size", (gpointer *)&plt_jump_table_size);
	g_assert (plt_jump_table_size);
	info->plt_jump_table_size = *plt_jump_table_size;

	if (make_unreadable) {
#ifndef PLATFORM_WIN32
		guint8 *addr;
		guint8 *page_start;
		int pages, err, len;

		addr = info->mem_begin;
		len = info->mem_end - info->mem_begin;

		/* Round down in both directions to avoid modifying data which is not ours */
		page_start = (guint8 *) (((gssize) (addr)) & ~ (PAGESIZE - 1)) + PAGESIZE;
		pages = ((addr + len - page_start + PAGESIZE - 1) / PAGESIZE) - 1;
		err = mprotect (page_start, pages * PAGESIZE, 0);
		g_assert (err == 0);
#endif
	}

	mono_aot_lock ();

	aot_code_low_addr = MIN (aot_code_low_addr, (gsize)info->code);
	aot_code_high_addr = MAX (aot_code_high_addr, (gsize)info->code_end);

	g_hash_table_insert (aot_modules, assembly, info);
	mono_aot_unlock ();

	mono_jit_info_add_aot_module (assembly->image, info->code, info->code_end);

	/*
	 * Since we store methoddef and classdef tokens when referring to methods/classes in
	 * referenced assemblies, we depend on the exact versions of the referenced assemblies.
	 * MS calls this 'hard binding'. This means we have to load all referenced assemblies
	 * non-lazily, since we can't handle out-of-date errors later.
	 */
	for (i = 0; i < info->image_table_len; ++i)
		load_image (info, i);

	if (info->out_of_date)
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT Module %s is unusable because a dependency is out-of-date.\n", assembly->image->name);
	else
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

static gboolean
decode_cached_class_info (MonoAotModule *module, MonoCachedClassInfo *info, guint8 *buf, guint8 **endbuf)
{
	guint32 flags;

	info->vtable_size = decode_value (buf, &buf);
	if (info->vtable_size == -1)
		/* Generic type */
		return FALSE;
	flags = decode_value (buf, &buf);
	info->ghcimpl = (flags >> 0) & 0x1;
	info->has_finalize = (flags >> 1) & 0x1;
	info->has_cctor = (flags >> 2) & 0x1;
	info->has_nested_classes = (flags >> 3) & 0x1;
	info->blittable = (flags >> 4) & 0x1;
	info->has_references = (flags >> 5) & 0x1;
	info->has_static_refs = (flags >> 6) & 0x1;
	info->no_special_static_fields = (flags >> 7) & 0x1;

	if (info->has_cctor) {
		MonoImage *cctor_image = decode_method_ref (module, &info->cctor_token, buf, &buf);
		if (!cctor_image)
			return FALSE;
	}
	if (info->has_finalize) {
		info->finalize_image = decode_method_ref (module, &info->finalize_token, buf, &buf);
		if (!info->finalize_image)
			return FALSE;
	}

	info->instance_size = decode_value (buf, &buf);
	info->class_size = decode_value (buf, &buf);
	info->packing_size = decode_value (buf, &buf);
	info->min_align = decode_value (buf, &buf);

	*endbuf = buf;

	return TRUE;
}	

gpointer
mono_aot_get_method_from_vt_slot (MonoDomain *domain, MonoVTable *vtable, int slot)
{
	int i;
	MonoAotModule *aot_module;
	MonoClass *klass = vtable->klass;
	guint8 *info, *p;
	MonoCachedClassInfo class_info;
	gboolean err;
	guint32 token;
	MonoImage *image;

	if (MONO_CLASS_IS_INTERFACE (klass) || klass->rank || !klass->image->assembly->aot_module)
		return NULL;

	mono_aot_lock ();

	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, klass->image->assembly);
	if (!aot_module) {
		mono_aot_unlock ();
		return NULL;
	}

	info = &aot_module->class_info [aot_module->class_info_offsets [mono_metadata_token_index (klass->type_token) - 1]];
	p = info;

	err = decode_cached_class_info (aot_module, &class_info, p, &p);
	if (!err) {
		mono_aot_unlock ();
		return NULL;
	}

	for (i = 0; i < slot; ++i)
		decode_method_ref (aot_module, &token, p, &p);

	image = decode_method_ref (aot_module, &token, p, &p);
	if (!image) {
		mono_aot_unlock ();
		return NULL;
	}

	mono_aot_unlock ();

	return mono_aot_get_method_from_token (domain, image, token);
}

gboolean
mono_aot_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	MonoAotModule *aot_module;
	guint8 *p;
	gboolean err;

	if (klass->rank || !klass->image->assembly->aot_module)
		return FALSE;

	mono_aot_lock ();

	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, klass->image->assembly);
	if (!aot_module) {
		mono_aot_unlock ();
		return FALSE;
	}

	p = (guint8*)&aot_module->class_info [aot_module->class_info_offsets [mono_metadata_token_index (klass->type_token) - 1]];

	err = decode_cached_class_info (aot_module, res, p, &p);
	if (!err) {
		mono_aot_unlock ();
		return FALSE;
	}

	mono_aot_unlock ();

	return TRUE;
}

/**
 * mono_aot_get_class_from_name:
 *
 *  Obtains a MonoClass with a given namespace and a given name which is located in IMAGE,
 * using a cache stored in the AOT file.
 * Stores the resulting class in *KLASS if found, stores NULL otherwise.
 *
 * Returns: TRUE if the klass was found/not found in the cache, FALSE if no aot file was 
 * found.
 */
gboolean
mono_aot_get_class_from_name (MonoImage *image, const char *name_space, const char *name, MonoClass **klass)
{
	MonoAotModule *aot_module;
	guint16 *table, *entry;
	guint16 table_size;
	guint32 hash;
	char full_name_buf [1024];
	char *full_name;
	const char *name2, *name_space2;
	MonoTableInfo  *t;
	guint32 cols [MONO_TYPEDEF_SIZE];
	GHashTable *nspace_table;

	if (!aot_modules)
		return FALSE;

	mono_aot_lock ();

	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, image->assembly);
	if (!aot_module || !aot_module->class_name_table) {
		mono_aot_unlock ();
		return FALSE;
	}

	*klass = NULL;

	/* First look in the cache */
	if (!aot_module->name_cache)
		aot_module->name_cache = g_hash_table_new (g_str_hash, g_str_equal);
	nspace_table = g_hash_table_lookup (aot_module->name_cache, name_space);
	if (nspace_table) {
		*klass = g_hash_table_lookup (nspace_table, name);
		if (*klass) {
			mono_aot_unlock ();
			return TRUE;
		}
	}

	table_size = aot_module->class_name_table [0];
	table = aot_module->class_name_table + 1;

	if (name_space [0] == '\0')
		full_name = g_strdup_printf ("%s", name);
	else {
		if (strlen (name_space) + strlen (name) < 1000) {
			sprintf (full_name_buf, "%s.%s", name_space, name);
			full_name = full_name_buf;
		} else {
			full_name = g_strdup_printf ("%s.%s", name_space, name);
		}
	}
	hash = g_str_hash (full_name) % table_size;
	if (full_name != full_name_buf)
		g_free (full_name);

	entry = &table [hash * 2];

	if (entry [0] != 0) {
		t = &image->tables [MONO_TABLE_TYPEDEF];

		while (TRUE) {
			guint32 index = entry [0];
			guint32 next = entry [1];
			guint32 token = mono_metadata_make_token (MONO_TABLE_TYPEDEF, index);

			name_table_accesses ++;

			mono_metadata_decode_row (t, index - 1, cols, MONO_TYPEDEF_SIZE);

			name2 = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAME]);
			name_space2 = mono_metadata_string_heap (image, cols [MONO_TYPEDEF_NAMESPACE]);

			if (!strcmp (name, name2) && !strcmp (name_space, name_space2)) {
				mono_aot_unlock ();
				*klass = mono_class_get (image, token);

				/* Add to cache */
				if (*klass) {
					mono_aot_lock ();
					nspace_table = g_hash_table_lookup (aot_module->name_cache, name_space);
					if (!nspace_table) {
						nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
						g_hash_table_insert (aot_module->name_cache, (char*)name_space2, nspace_table);
					}
					g_hash_table_insert (nspace_table, (char*)name2, *klass);
					mono_aot_unlock ();
				}
				return TRUE;
			}

			if (next != 0) {
				entry = &table [next * 2];
			} else {
				break;
			}
		}
	}

	mono_aot_unlock ();
	
	return TRUE;
}

static MonoJitInfo*
decode_exception_debug_info (MonoAotModule *aot_module, MonoDomain *domain, 
							 MonoMethod *method, guint8* ex_info, guint8 *code)
{
	int i, buf_len;
	MonoJitInfo *jinfo;
	guint code_len, used_int_regs;
	guint8 *p;
	MonoMethodHeader *header;

	header = mono_method_get_header (method);

	/* Load the method info from the AOT file */

	p = ex_info;
	code_len = decode_value (p, &p);
	used_int_regs = decode_value (p, &p);

	/* Exception table */
	if (header->num_clauses) {
		jinfo = 
			mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo) + (sizeof (MonoJitExceptionInfo) * header->num_clauses));
		jinfo->num_clauses = header->num_clauses;

		for (i = 0; i < header->num_clauses; ++i) {
			MonoExceptionClause *ec = &header->clauses [i];				
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];

			ei->flags = ec->flags;
			ei->exvar_offset = decode_value (p, &p);

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				ei->data.filter = code + decode_value (p, &p);
			else
				ei->data.catch_class = ec->data.catch_class;

			ei->try_start = code + decode_value (p, &p);
			ei->try_end = code + decode_value (p, &p);
			ei->handler_start = code + decode_value (p, &p);
		}
	}
	else
		jinfo = mono_mempool_alloc0 (domain->mp, sizeof (MonoJitInfo));

	jinfo->code_size = code_len;
	jinfo->used_regs = used_int_regs;
	jinfo->method = method;
	jinfo->code_start = code;
	jinfo->domain_neutral = 0;

	/* Load debug info */
	buf_len = decode_value (p, &p);
	mono_debug_add_aot_method (domain, method, code, p, buf_len);
	
	return jinfo;
}

MonoJitInfo *
mono_aot_find_jit_info (MonoDomain *domain, MonoImage *image, gpointer addr)
{
	MonoAssembly *ass = image->assembly;
	MonoDl *module = ass->aot_module;
	int pos, left, right, offset, offset1, offset2, last_offset, new_offset, page_index, method_index, table_len;
	guint32 token;
	MonoAotModule *aot_module;
	MonoMethod *method;
	MonoJitInfo *jinfo;
	guint8 *code, *ex_info;
	guint32 *table, *ptr;
	gboolean found;

	if (!module)
		return NULL;

	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, ass);

	if (domain != mono_get_root_domain ())
		/* FIXME: */
		return NULL;

	offset = (guint8*)addr - aot_module->code;

	/* First search through the index */
	ptr = aot_module->method_order;
	last_offset = 0;
	page_index = 0;
	found = FALSE;

	if (*ptr == 0xffffff)
		return NULL;
	ptr ++;

	while (*ptr != 0xffffff) {
		guint32 method_index = ptr [0];
		new_offset = aot_module->code_offsets [method_index];

		if (offset >= last_offset && offset < new_offset) {
			found = TRUE;
			break;
		}

		ptr ++;
		last_offset = new_offset;
		page_index ++;
	}

	/* Skip rest of index */
	while (*ptr != 0xffffff)
		ptr ++;
	ptr ++;

	table = ptr;
	table_len = aot_module->method_order_end - table;

	g_assert (table <= aot_module->method_order_end);

	if (found) {
		left = (page_index * 1024);
		right = left + 1024;

		if (right > table_len)
			right = table_len;

		offset1 = aot_module->code_offsets [table [left]];
		g_assert (offset1 <= offset);

		//printf ("Found in index: 0x%x 0x%x 0x%x\n", offset, last_offset, new_offset);
	}
	else {
		//printf ("Not found in index: 0x%x\n", offset);
		left = 0;
		right = table_len;
	}

	/* Binary search inside the method_order table to find the method */
	while (TRUE) {
		pos = (left + right) / 2;

		g_assert (table + pos <= aot_module->method_order_end);

		//printf ("Pos: %5d < %5d < %5d Offset: 0x%05x < 0x%05x < 0x%05x\n", left, pos, right, aot_module->code_offsets [table [left]], offset, aot_module->code_offsets [table [right]]);

		offset1 = aot_module->code_offsets [table [pos]];
		if (table + pos + 1 >= aot_module->method_order_end)
			offset2 = aot_module->code_end - aot_module->code;
		else
			offset2 = aot_module->code_offsets [table [pos + 1]];

		if (offset < offset1)
			right = pos;
		else if (offset >= offset2)
			left = pos + 1;
		else
			break;
	}

	method_index = table [pos];

	token = mono_metadata_make_token (MONO_TABLE_METHOD, method_index + 1);
	method = mono_get_method (image, token, NULL);

	/* FIXME: */
	g_assert (method);

	//printf ("F: %s\n", mono_method_full_name (method, TRUE));

	code = &aot_module->code [aot_module->code_offsets [method_index]];
	ex_info = &aot_module->ex_info [aot_module->ex_info_offsets [method_index]];

	jinfo = decode_exception_debug_info (aot_module, domain, method, ex_info, code);

	g_assert ((guint8*)addr >= (guint8*)jinfo->code_start);
	g_assert ((guint8*)addr < (guint8*)jinfo->code_start + jinfo->code_size);

	/* Add it to the normal JitInfo tables */
	mono_jit_info_table_add (domain, jinfo);
	
	return jinfo;
}

static gboolean
decode_patch_info (MonoAotModule *aot_module, MonoMemPool *mp, MonoJumpInfo *ji, guint8 *buf, guint8 **endbuf, guint32 *got_offset)
{
	guint8 *p = buf;
	gpointer *table;
	MonoImage *image;
	int i;

	switch (ji->type) {
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_ICALL_ADDR: {
		guint32 token;

		image = decode_method_ref (aot_module, &token, p, &p);
		if (!image)
			goto cleanup;

#ifdef MONO_ARCH_HAVE_CREATE_TRAMPOLINE_FROM_TOKEN
		if ((ji->type == MONO_PATCH_INFO_METHOD) && (mono_metadata_token_table (token) == MONO_TABLE_METHOD)) {
			ji->data.target = mono_create_jit_trampoline_from_token (image, token);
			ji->type = MONO_PATCH_INFO_ABS;
		}
		else {
			ji->data.method = mono_get_method (image, token, NULL);
			g_assert (ji->data.method);
			mono_class_init (ji->data.method->klass);
		}
#else
		ji->data.method = mono_get_method (image, token, NULL);
		g_assert (ji->data.method);
		mono_class_init (ji->data.method->klass);
#endif

		break;
	}
	case MONO_PATCH_INFO_WRAPPER: {
		guint32 wrapper_type;

		wrapper_type = decode_value (p, &p);

		ji->type = MONO_PATCH_INFO_METHOD;

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

			ji->data.method = mono_marshal_get_remoting_invoke_with_check (ji->data.method);
			break;
		}
		case MONO_WRAPPER_PROXY_ISINST: {
			MonoClass *klass = decode_klass_ref (aot_module, p, &p);
			if (!klass)
				goto cleanup;
			ji->data.method = mono_marshal_get_proxy_cancast (klass);
			break;
		}
		case MONO_WRAPPER_LDFLD:
		case MONO_WRAPPER_LDFLDA:
		case MONO_WRAPPER_STFLD:
		case MONO_WRAPPER_ISINST: {
			MonoClass *klass = decode_klass_ref (aot_module, p, &p);
			if (!klass)
				goto cleanup;
			if (wrapper_type == MONO_WRAPPER_LDFLD)
				ji->data.method = mono_marshal_get_ldfld_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_LDFLDA)
				ji->data.method = mono_marshal_get_ldflda_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_STFLD)
				ji->data.method = mono_marshal_get_stfld_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_ISINST)
				ji->data.method = mono_marshal_get_isinst (klass);
			else
				g_assert_not_reached ();
			break;
		}
		case MONO_WRAPPER_LDFLD_REMOTE:
			ji->data.method = mono_marshal_get_ldfld_remote_wrapper (NULL);
			break;
		case MONO_WRAPPER_STFLD_REMOTE:
			ji->data.method = mono_marshal_get_stfld_remote_wrapper (NULL);
			break;
		case MONO_WRAPPER_ALLOC: {
			int atype = decode_value (p, &p);

			ji->data.method = mono_gc_get_managed_allocator_by_type (atype);
			break;
		}
		case MONO_WRAPPER_STELEMREF:
			ji->data.method = mono_marshal_get_stelemref ();
			break;
		default:
			g_assert_not_reached ();
		}
		break;
	}
	case MONO_PATCH_INFO_INTERNAL_METHOD: {
		guint32 len = decode_value (p, &p);

		ji->data.name = (char*)p;
		p += len + 1;
		break;
	}
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
		*got_offset = decode_value (p, &p);

		if (aot_module->got [*got_offset]) {
			/* Already loaded */
			//printf ("HIT!\n");
		} else {
			guint8 *tmp = aot_module->got_info + aot_module->got_info_offsets [*got_offset];
			ji->data.klass = decode_klass_ref (aot_module, tmp, &tmp);
			if (!ji->data.klass)
				goto cleanup;
		}
		break;
	case MONO_PATCH_INFO_CLASS_INIT:
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		ji->data.klass = decode_klass_ref (aot_module, p, &p);
		if (!ji->data.klass)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_IMAGE:
		ji->data.image = load_image (aot_module, decode_value (p, &p));
		if (!ji->data.image)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_SFLDA:
		*got_offset = decode_value (p, &p);

		if (aot_module->got [*got_offset]) {
			/* Already loaded */
			//printf ("HIT2!\n");
		} else {
			guint8 *tmp = aot_module->got_info + aot_module->got_info_offsets [*got_offset];
			ji->data.field = decode_field_info (aot_module, tmp, &tmp);
			if (!ji->data.field)
				goto cleanup;
		}
		break;
	case MONO_PATCH_INFO_SWITCH:
		ji->data.table = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoBBTable));
		ji->data.table->table_size = decode_value (p, &p);
		table = g_new (gpointer, ji->data.table->table_size);
		ji->data.table->table = (MonoBasicBlock**)table;
		for (i = 0; i < ji->data.table->table_size; i++)
			table [i] = (gpointer)(gssize)decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_R4: {
		guint32 val;
		
		ji->data.target = mono_mempool_alloc0 (mp, sizeof (float));
		val = decode_value (p, &p);
		*(float*)ji->data.target = *(float*)&val;
		break;
	}
	case MONO_PATCH_INFO_R8: {
		guint32 val [2];

		ji->data.target = mono_mempool_alloc0 (mp, sizeof (double));

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
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_DECLSEC:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		*got_offset = decode_value (p, &p);

		if (aot_module->got [*got_offset]) {
			/* Already loaded */
		} else {
			guint8 *tmp = aot_module->got_info + aot_module->got_info_offsets [*got_offset];
			image = load_image (aot_module, decode_value (tmp, &tmp));
			if (!image)
				goto cleanup;
			ji->data.token = mono_jump_info_token_new (mp, image, decode_value (tmp, &tmp));
		}
		break;
	case MONO_PATCH_INFO_EXC_NAME:
		ji->data.klass = decode_klass_ref (aot_module, p, &p);
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

	*endbuf = p;

	return TRUE;

 cleanup:
	return FALSE;
}

static MonoJumpInfo*
load_patch_info (MonoAotModule *aot_module, MonoMemPool *mp, int n_patches, 
				 guint32 got_index, guint32 **got_slots, 
				 guint8 *buf, guint8 **endbuf)
{
	MonoJumpInfo *patches;
	MonoJumpInfo *patch_info = NULL;
	int pindex;
	guint32 last_offset;
	guint8 *p;

	p = buf;

	/* First load the type + offset table */
	last_offset = 0;
	patches = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo) * n_patches);

	for (pindex = 0; pindex < n_patches; ++pindex) {		
		MonoJumpInfo *ji = &patches [pindex];

		ji->type = *p;
		p ++;

		//printf ("T: %d O: %d.\n", ji->type, ji->ip.i);
		ji->next = patch_info;
		patch_info = ji;
	}

	*got_slots = g_malloc (sizeof (guint32) * n_patches);
	memset (*got_slots, 0xff, sizeof (guint32) * n_patches);

	/* Then load the other data */
	for (pindex = 0; pindex < n_patches; ++pindex) {
		MonoJumpInfo *ji = &patches [pindex];

		if (!decode_patch_info (aot_module, mp, ji, p, &p, (*got_slots) + pindex))
			goto cleanup;

		if ((*got_slots) [pindex] == 0xffffffff)
			(*got_slots) [pindex] = got_index ++;
	}

	*endbuf = p;
	return patches;

 cleanup:
	g_free (*got_slots);
	*got_slots = NULL;

	return NULL;
}
 
static gpointer
mono_aot_get_method_inner (MonoDomain *domain, MonoMethod *method)
{
	MonoClass *klass = method->klass;
	MonoAssembly *ass = klass->image->assembly;
	MonoDl *module = ass->aot_module;
	guint32 method_index = mono_metadata_token_index (method->token) - 1;
	guint8 *code, *info;
	MonoAotModule *aot_module;

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
	
	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, ass);

	g_assert (klass->inited);

	if ((domain != mono_get_root_domain ()) && (!(aot_module->opts & MONO_OPT_SHARED)))
		/* Non shared AOT code can't be used in other appdomains */
		return NULL;

	if (aot_module->out_of_date)
		return NULL;

	if (method->is_inflated) {
		if (!mono_method_is_generic_sharable_impl (method))
			return NULL;
		method = mono_method_get_declaring_generic_method (method);
	}

	if (aot_module->code_offsets [method_index] == 0xffffffff) {
		if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
			char *full_name = mono_method_full_name (method, TRUE);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT NOT FOUND: %s.\n", full_name);
			g_free (full_name);
		}
		return NULL;
	}

	code = &aot_module->code [aot_module->code_offsets [method_index]];
	info = &aot_module->method_info [aot_module->method_info_offsets [method_index]];

	if (!aot_module->methods_loaded)
		aot_module->methods_loaded = g_new0 (guint32, klass->image->tables [MONO_TABLE_METHOD].rows + 1);

	if ((aot_module->methods_loaded [method_index / 32] >> (method_index % 32)) & 0x1)
		return code;

	if (mono_last_aot_method != -1) {
		if (mono_jit_stats.methods_aot > mono_last_aot_method)
				return NULL;
		else
			if (mono_jit_stats.methods_aot == mono_last_aot_method)
				printf ("LAST AOT METHOD: %s.%s.%s.\n", klass->name_space, klass->name, method->name);
	}

	return mono_aot_load_method (domain, aot_module, method, code, info);
}

static gpointer
mono_aot_load_method (MonoDomain *domain, MonoAotModule *aot_module, MonoMethod *method, guint8 *code, guint8 *info)
{
	MonoClass *klass = method->klass;
	MonoJumpInfo *patch_info = NULL;
	MonoMemPool *mp;
	int i, pindex, got_index = 0, n_patches, used_strings;
	gboolean non_got_patches, keep_patches = TRUE;
	guint32 method_index = mono_metadata_token_index (method->token) - 1;
	guint8 *p, *ex_info;
	MonoJitInfo *jinfo = NULL;

	p = info;
	decode_klass_ref (aot_module, p, &p);

	if (!use_loaded_code) {
		guint8 *code2;

		if (!jinfo) {
			ex_info = &aot_module->ex_info [aot_module->ex_info_offsets [mono_metadata_token_index (method->token) - 1]];
			jinfo = decode_exception_debug_info (aot_module, domain, method, ex_info, code);
		}

		code2 = mono_code_manager_reserve (domain->code_mp, jinfo->code_size);
		memcpy (code2, code, jinfo->code_size);
		mono_arch_flush_icache (code2, jinfo->code_size);
		code = code2;
	}

	if (aot_module->opts & MONO_OPT_SHARED)
		used_strings = decode_value (p, &p);
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint token = decode_value (p, &p);
		mono_ldstr (mono_get_root_domain (), klass->image, mono_metadata_token_index (token));
	}

	if (aot_module->opts & MONO_OPT_SHARED)	
		keep_patches = FALSE;

	n_patches = decode_value (p, &p);

	keep_patches = FALSE;

	if (n_patches) {
		MonoJumpInfo *patches;
		guint32 *got_slots;

		if (keep_patches)
			mp = domain->mp;
		else
			mp = mono_mempool_new ();

		got_index = decode_value (p, &p);

		patches = load_patch_info (aot_module, mp, n_patches, got_index, &got_slots, p, &p);
		if (patches == NULL)
			goto cleanup;

		/* Do this outside the lock to avoid deadlocks */
		mono_aot_unlock ();
		non_got_patches = FALSE;
		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];

			if (is_got_patch (ji->type)) {
				if (!aot_module->got [got_slots [pindex]])
					aot_module->got [got_slots [pindex]] = mono_resolve_patch_target (method, domain, code, ji, TRUE);
				ji->type = MONO_PATCH_INFO_NONE;
			}
			else
				non_got_patches = TRUE;
		}
		if (non_got_patches) {
			if (!jinfo) {
				ex_info = &aot_module->ex_info [aot_module->ex_info_offsets [mono_metadata_token_index (method->token) - 1]];
				jinfo = decode_exception_debug_info (aot_module, domain, method, ex_info, code);
			}

			mono_arch_flush_icache (code, jinfo->code_size);
			make_writable (code, jinfo->code_size);
			mono_arch_patch_code (method, domain, code, patch_info, TRUE);
		}
		mono_aot_lock ();

		g_free (got_slots);

		if (!keep_patches)
			mono_mempool_destroy (mp);
	}

	mono_jit_stats.methods_aot++;

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
		char *full_name = mono_method_full_name (method, TRUE);

		if (!jinfo) {
			ex_info = &aot_module->ex_info [aot_module->ex_info_offsets [mono_metadata_token_index (method->token) - 1]];
			jinfo = decode_exception_debug_info (aot_module, domain, method, ex_info, code);
		}

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT FOUND AOT compiled code for %s %p - %p %p\n", full_name, code, code + jinfo->code_size, info);
		g_free (full_name);
	}

	aot_module->methods_loaded [method_index / 32] |= 1 << (method_index % 32);

	init_plt (aot_module);

	return code;

 cleanup:
	/* FIXME: The space in domain->mp is wasted */	
	if (aot_module->opts & MONO_OPT_SHARED)
		/* No need to cache patches */
		mono_mempool_destroy (mp);

	if (jinfo)
		g_free (jinfo);

	return NULL;
}

gpointer
mono_aot_get_method (MonoDomain *domain, MonoMethod *method)
{
	gpointer code;

	mono_aot_lock ();
	code = mono_aot_get_method_inner (domain, method);
	mono_aot_unlock ();

	return code;
}

static gpointer
mono_aot_get_method_from_token_inner (MonoDomain *domain, MonoImage *image, guint32 token, MonoClass **klass)
{
	MonoAssembly *ass = image->assembly;
	MonoMemPool *mp;
	int i, method_index, pindex, got_index, n_patches, used_strings;
	gboolean keep_patches = TRUE;
	guint8 *p;
	MonoDl *module = ass->aot_module;
	guint8 *code = NULL;
	guint8 *info;
	MonoAotModule *aot_module;

	*klass = NULL;

	if (!module)
		return NULL;

	if (mono_profiler_get_events () & MONO_PROFILE_ENTER_LEAVE)
		return NULL;

	aot_module = (MonoAotModule*) g_hash_table_lookup (aot_modules, ass);

	if (domain != mono_get_root_domain ())
		return NULL;

	if (aot_module->out_of_date)
		return NULL;

	if (aot_module->code_offsets [mono_metadata_token_index (token) - 1] == 0xffffffff) {
		return NULL;
	}

	method_index = mono_metadata_token_index (token) - 1;
	code = &aot_module->code [aot_module->code_offsets [method_index]];
	info = &aot_module->method_info [aot_module->method_info_offsets [method_index]];

	if (!aot_module->methods_loaded)
		aot_module->methods_loaded = g_new0 (guint32, image->tables [MONO_TABLE_METHOD].rows + 1);

	if ((aot_module->methods_loaded [method_index / 32] >> (method_index % 32)) & 0x1)
		return code;

	if (mono_last_aot_method != -1) {
		if (mono_jit_stats.methods_aot > mono_last_aot_method)
				return NULL;
		else
			if (mono_jit_stats.methods_aot == mono_last_aot_method) {
				MonoMethod *method = mono_get_method (image, token, NULL);
				printf ("LAST AOT METHOD: %s.%s.%s.\n", method->klass->name_space, method->klass->name, method->name);
			}
	}

	p = info;
	*klass = decode_klass_ref (aot_module, p, &p);

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
		MonoMethod *method = mono_get_method (image, token, NULL);
		char *full_name = mono_method_full_name (method, TRUE);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT FOUND AOT compiled code for %s %p %p\n", full_name, code, info);
		g_free (full_name);
	}

	if (aot_module->opts & MONO_OPT_SHARED)
		used_strings = decode_value (p, &p);
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint string_token = decode_value (p, &p);
		mono_ldstr (mono_get_root_domain (), image, mono_metadata_token_index (string_token));
	}

	if (aot_module->opts & MONO_OPT_SHARED)	
		keep_patches = FALSE;

	keep_patches = FALSE;

	n_patches = decode_value (p, &p);

	if (n_patches) {
		MonoJumpInfo *patches;
		guint32 *got_slots;

		if (keep_patches)
			mp = domain->mp;
		else
			mp = mono_mempool_new ();

		got_index = decode_value (p, &p);

		patches = load_patch_info (aot_module, mp, n_patches, got_index, &got_slots, p, &p);
		if (patches == NULL)
			goto cleanup;

		/* Do this outside the lock to avoid deadlocks */
		mono_aot_unlock ();

		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];

			if (is_got_patch (ji->type)) {
				if (!aot_module->got [got_slots [pindex]])
					aot_module->got [got_slots [pindex]] = mono_resolve_patch_target (NULL, domain, code, ji, TRUE);
				ji->type = MONO_PATCH_INFO_NONE;
			}
		}

		mono_aot_lock ();

		g_free (got_slots);

		if (!keep_patches)
			mono_mempool_destroy (mp);
	}

	mono_jit_stats.methods_aot++;

	aot_module->methods_loaded [method_index / 32] |= 1 << (method_index % 32);

	init_plt (aot_module);

	return code;

 cleanup:
	/* FIXME: The space in domain->mp is wasted */	
	if (aot_module->opts & MONO_OPT_SHARED)
		/* No need to cache patches */
		mono_mempool_destroy (mp);

	return NULL;
}

/**
 * Same as mono_aot_get_method, but we try to avoid loading any metadata from the
 * method.
 */
gpointer
mono_aot_get_method_from_token (MonoDomain *domain, MonoImage *image, guint32 token)
{
	gpointer res;	
	MonoClass *klass;

	mono_aot_lock ();
	res = mono_aot_get_method_from_token_inner (domain, image, token, &klass);
	mono_aot_unlock ();

	if (!res)
		return NULL;

	if (klass)
		mono_runtime_class_init (mono_class_vtable (domain, klass));

	return res;
}

typedef struct {
	guint8 *addr;
	gboolean res;
} IsGotEntryUserData;

static void
check_is_got_entry (gpointer key, gpointer value, gpointer user_data)
{
	IsGotEntryUserData *data = (IsGotEntryUserData*)user_data;
	MonoAotModule *aot_module = (MonoAotModule*)value;

	if (aot_module->got && (data->addr >= (guint8*)(aot_module->got)) && (data->addr < (guint8*)(aot_module->got + aot_module->got_size)))
		data->res = TRUE;
}

gboolean
mono_aot_is_got_entry (guint8 *code, guint8 *addr)
{
	IsGotEntryUserData user_data;

	if (!aot_modules)
		return FALSE;

	user_data.addr = addr;
	user_data.res = FALSE;
	mono_aot_lock ();
	g_hash_table_foreach (aot_modules, check_is_got_entry, &user_data);
	mono_aot_unlock ();
	
	return user_data.res;
}

typedef struct {
	guint8 *addr;
	MonoAotModule *module;
} FindAotModuleUserData;

static void
find_aot_module_cb (gpointer key, gpointer value, gpointer user_data)
{
	FindAotModuleUserData *data = (FindAotModuleUserData*)user_data;
	MonoAotModule *aot_module = (MonoAotModule*)value;

	if ((data->addr >= (guint8*)(aot_module->code)) && (data->addr < (guint8*)(aot_module->code_end)))
		data->module = aot_module;
}

static inline MonoAotModule*
find_aot_module (guint8 *code)
{
	FindAotModuleUserData user_data;

	if (!aot_modules)
		return NULL;

	/* Reading these need no locking */
	if (((gsize)code < aot_code_low_addr) || ((gsize)code > aot_code_high_addr))
		return NULL;

	user_data.addr = code;
	user_data.module = NULL;
		
	mono_aot_lock ();
	g_hash_table_foreach (aot_modules, find_aot_module_cb, &user_data);
	mono_aot_unlock ();
	
	return user_data.module;
}

/*
 * mono_aot_set_make_unreadable:
 *
 *   Set whenever to make all mmaped memory unreadable. In conjuction with a
 * SIGSEGV handler, this is useful to find out which pages the runtime tries to read.
 */
void
mono_aot_set_make_unreadable (gboolean unreadable)
{
	make_unreadable = unreadable;
}

typedef struct {
	MonoAotModule *module;
	guint8 *ptr;
} FindMapUserData;

static void
find_map (gpointer key, gpointer value, gpointer user_data)
{
	MonoAotModule *module = (MonoAotModule*)value;
	FindMapUserData *data = (FindMapUserData*)user_data;

	if (!data->module)
		if ((data->ptr >= module->mem_begin) && (data->ptr < module->mem_end))
			data->module = module;
}

static MonoAotModule*
find_module_for_addr (void *ptr)
{
	FindMapUserData data;

	if (!make_unreadable)
		return NULL;

	data.module = NULL;
	data.ptr = (guint8*)ptr;

	mono_aot_lock ();
	g_hash_table_foreach (aot_modules, (GHFunc)find_map, &data);
	mono_aot_unlock ();

	return data.module;
}

/*
 * mono_aot_is_pagefault:
 *
 *   Should be called from a SIGSEGV signal handler to find out whenever @ptr is
 * within memory allocated by this module.
 */
gboolean
mono_aot_is_pagefault (void *ptr)
{
	if (!make_unreadable)
		return FALSE;

	return find_module_for_addr (ptr) != NULL;
}

/*
 * mono_aot_handle_pagefault:
 *
 *   Handle a pagefault caused by an unreadable page by making it readable again.
 */
void
mono_aot_handle_pagefault (void *ptr)
{
#ifndef PLATFORM_WIN32
	guint8* start = (guint8*)ROUND_DOWN (((gssize)ptr), PAGESIZE);
	int res;

	mono_aot_lock ();
	res = mprotect (start, PAGESIZE, PROT_READ|PROT_WRITE|PROT_EXEC);
	g_assert (res == 0);

	n_pagefaults ++;
	mono_aot_unlock ();

#if 0
 {
	void *array [256];
	char **names;
	int i, size;

	printf ("\nNative stacktrace:\n\n");

	size = backtrace (array, 256);
	names = backtrace_symbols (array, size);
	for (i =0; i < size; ++i) {
		printf ("\t%s\n", names [i]);
	}
	free (names);
 }
#endif

#endif
}

/*
 * mono_aot_plt_resolve:
 *
 *   This function is called by the entries in the PLT to resolve the actual method that
 * needs to be called. It returns a trampoline to the method and patches the PLT entry.
 */
gpointer
mono_aot_plt_resolve (gpointer aot_module, guint32 plt_info_offset, guint8 *code)
{
#ifdef MONO_ARCH_AOT_SUPPORTED
	guint8 *p, *target, *plt_entry;
	MonoJumpInfo ji;
	MonoAotModule *module = (MonoAotModule*)aot_module;
	gboolean res;

	//printf ("DYN: %p %d\n", aot_module, plt_info_offset);

	p = &module->plt_info [plt_info_offset];

	ji.type = decode_value (p, &p);

	res = decode_patch_info (module, NULL, &ji, p, &p, NULL);
	// FIXME: Error handling (how ?)
	g_assert (res);

	target = mono_resolve_patch_target (NULL, mono_domain_get (), NULL, &ji, TRUE);

	/* Patch the PLT entry with target which might be the actual method not a trampoline */
	plt_entry = mono_aot_get_plt_entry (code);
	g_assert (plt_entry);
	mono_arch_patch_plt_entry (plt_entry, target);

	return target;
#else
	g_assert_not_reached ();
	return NULL;
#endif
}

/**
 * init_plt:
 *
 *   Initialize the PLT table of the AOT module. Called lazily when the first AOT
 * method in the module is loaded to avoid committing memory by writing to it.
 * LOCKING: Assumes the AOT lock is held.
 */
static void
init_plt (MonoAotModule *info)
{
#ifdef MONO_ARCH_AOT_SUPPORTED
#ifdef __i386__
	guint8 *buf = info->plt;
#elif defined(__x86_64__)
	int i, n_entries;
#elif defined(__arm__)
	int i, n_entries;
#endif
	gpointer tramp;

	if (info->plt_inited)
		return;

	tramp = mono_arch_create_specific_trampoline (info, MONO_TRAMPOLINE_AOT_PLT, mono_get_root_domain (), NULL);

#ifdef __i386__
	/* Initialize the first PLT entry */
	make_writable (info->plt, info->plt_end - info->plt);
	x86_jump_code (buf, tramp);
#elif defined(__x86_64__)
	/*
	 * Initialize the entries in the plt_jump_table to point to the default targets.
	 */
	 n_entries = info->plt_jump_table_size / sizeof (gpointer);

	 /* The first entry points to the AOT trampoline */
	 ((gpointer*)info->plt_jump_table)[0] = tramp;
	 for (i = 1; i < n_entries; ++i)
		 /* Each PLT entry is 16 bytes long, the default entry begins at offset 6 */
		 ((gpointer*)info->plt_jump_table)[i] = info->plt + (i * 16) + 6;
#elif defined(__arm__)
	 /* Initialize the first PLT entry */
	 make_writable (info->plt, info->plt_end - info->plt);
	 ((guint32*)info->plt)[1] = (guint32)tramp;

	 n_entries = ((guint8*)info->plt_end - (guint8*)info->plt) / 8;

	 /* 
	  * Initialize the jump targets embedded inside the PLT entries to the default
	  * targets.
	  */
	 for (i = 1; i < n_entries; ++i)
		 /* Each PLT entry is 8 bytes long, the jump target is at offset 4 */
		 /* Each default PLT target is 12 bytes long */
		 ((guint32*)info->plt)[(i * 2) + 1] = (guint8*)info->plt_end + ((i - 1) * 12);
#else
	g_assert_not_reached ();
#endif

	info->plt_inited = TRUE;
#endif
}

/*
 * mono_aot_get_plt_entry:
 *
 *   Return the address of the PLT entry called by the code at CODE if exists.
 */
guint8*
mono_aot_get_plt_entry (guint8 *code)
{
	MonoAotModule *aot_module = find_aot_module (code);
#if defined(__arm__)
	guint32 ins;
#endif

	if (!aot_module)
		return NULL;

#if defined(__i386__) || defined(__x86_64__)
	if (code [-5] == 0xe8) {
		guint32 disp = *(guint32*)(code - 4);
		guint8 *target = code + disp;

		if ((target >= (guint8*)(aot_module->plt)) && (target < (guint8*)(aot_module->plt_end)))
			return target;
	}
#elif defined(__arm__)
	ins = ((guint32*)(gpointer)code) [-1];

	/* Should be a 'bl' */
	if ((((ins >> 25) & 0x7) == 0x5) && (((ins >> 24) & 0x1) == 0x1)) {
		gint32 disp = ((gint32)ins) & 0xffffff;
		guint8 *target = code - 4 + 8 + (disp * 4);

		if ((target >= (guint8*)(aot_module->plt)) && (target < (guint8*)(aot_module->plt_end)))
			return target;
	}		
#else
	g_assert_not_reached ();
#endif

	return NULL;
}

/*
 * mono_aot_get_n_pagefaults:
 *
 *   Return the number of times handle_pagefault is called.
 */
guint32
mono_aot_get_n_pagefaults (void)
{
	return n_pagefaults;
}

#else
/* AOT disabled */

void
mono_aot_init (void)
{
}

gpointer
mono_aot_get_method (MonoDomain *domain, MonoMethod *method)
{
	return NULL;
}

gboolean
mono_aot_is_got_entry (guint8 *code, guint8 *addr)
{
	return FALSE;
}

gboolean
mono_aot_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	return FALSE;
}

gboolean
mono_aot_get_class_from_name (MonoImage *image, const char *name_space, const char *name, MonoClass **klass)
{
	return FALSE;
}

MonoJitInfo *
mono_aot_find_jit_info (MonoDomain *domain, MonoImage *image, gpointer addr)
{
	return NULL;
}

gpointer
mono_aot_get_method_from_token (MonoDomain *domain, MonoImage *image, guint32 token)
{
	return NULL;
}

gboolean
mono_aot_is_pagefault (void *ptr)
{
	return FALSE;
}

void
mono_aot_set_make_unreadable (gboolean unreadable)
{
}

guint32
mono_aot_get_n_pagefaults (void)
{
	return 0;
}

void
mono_aot_handle_pagefault (void *ptr)
{
}

guint8*
mono_aot_get_plt_entry (guint8 *code)
{
	return NULL;
}

gpointer
mono_aot_plt_resolve (gpointer aot_module, guint32 plt_info_offset, guint8 *code)
{
	return NULL;
}

gpointer
mono_aot_get_method_from_vt_slot (MonoDomain *domain, MonoVTable *vtable, int slot)
{
	return NULL;
}
#endif
