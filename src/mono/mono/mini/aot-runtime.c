/*
 * aot-runtime.c: mono Ahead of Time compiler
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
#ifdef HAVE_SYS_MMAN_H
#include <sys/mman.h>
#endif

#if PLATFORM_WIN32
#include <winsock2.h>
#include <windows.h>
#endif

#ifdef HAVE_EXECINFO_H
#include <execinfo.h>
#endif

#include <errno.h>
#include <sys/stat.h>

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
#include <mono/metadata/monitor.h>
#include <mono/metadata/threads-types.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-mmap.h>
#include "mono/utils/mono-compiler.h"
#include <mono/utils/mono-counters.h>

#include "mini.h"
#include "version.h"

#ifndef DISABLE_AOT

#ifdef PLATFORM_WIN32
#define SHARED_EXT ".dll"
#elif (defined(__ppc__) || defined(__powerpc__) || defined(__ppc64__)) || defined(__MACH__)
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
	GHashTable *name_cache;
	GHashTable *extra_methods;
	/* Maps methods to their code */
	GHashTable *method_to_code;
	/* Maps pointers into the method info to the methods themselves */
	GHashTable *method_ref_to_method;
	MonoAssemblyName *image_names;
	char **image_guids;
	MonoAssembly *assembly;
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
	guint32 *extra_method_table;
	guint32 *extra_method_info_offsets;
	guint8 *extra_method_info;
	guint8 *unwind_info;

	/* Points to the trampolines */
	guint8 *trampolines [MONO_AOT_TRAMP_NUM];
	/* The first unused trampoline of each kind */
	guint32 trampoline_index [MONO_AOT_TRAMP_NUM];

	MonoAotFileInfo info;

	gpointer *globals;
	MonoDl *sofile;
} MonoAotModule;

static GHashTable *aot_modules;
#define mono_aot_lock() EnterCriticalSection (&aot_mutex)
#define mono_aot_unlock() LeaveCriticalSection (&aot_mutex)
static CRITICAL_SECTION aot_mutex;

/* 
 * Maps assembly names to the mono_aot_module_<NAME>_info symbols in the
 * AOT modules registered by mono_aot_register_module ().
 */
static GHashTable *static_aot_modules;

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
static guint32 name_table_accesses = 0;

/* Used to speed-up find_aot_module () */
static gsize aot_code_low_addr = (gssize)-1;
static gsize aot_code_high_addr = 0;

static void
init_plt (MonoAotModule *info);

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
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s is unusable because dependency %s is not found.\n", module->aot_name, module->image_names [index].name);
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

static MonoMethod*
decode_method_ref_2 (MonoAotModule *module, guint8 *buf, guint8 **endbuf);

static MonoClass*
decode_klass_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf);

static MonoGenericInst*
decode_generic_inst (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	int type_argc, i;
	MonoType **type_argv;
	MonoGenericInst *inst;
	guint8 *p = buf;

	type_argc = decode_value (p, &p);
	type_argv = g_new0 (MonoType*, type_argc);

	for (i = 0; i < type_argc; ++i) {
		MonoClass *pclass = decode_klass_ref (module, p, &p);
		if (!pclass) {
			g_free (type_argv);
			return NULL;
		}
		type_argv [i] = &pclass->byval_arg;
	}

	inst = mono_metadata_get_generic_inst (type_argc, type_argv);
	g_free (type_argv);

	*endbuf = p;

	return inst;
}

static gboolean
decode_generic_context (MonoAotModule *module, MonoGenericContext *ctx, guint8 *buf, guint8 **endbuf)
{
	gboolean has_class_inst, has_method_inst;
	guint8 *p = buf;

	has_class_inst = decode_value (p, &p);
	if (has_class_inst) {
		ctx->class_inst = decode_generic_inst (module, p, &p);
		if (!ctx->class_inst)
			return FALSE;
	}
	has_method_inst = decode_value (p, &p);
	if (has_method_inst) {
		ctx->method_inst = decode_generic_inst (module, p, &p);
		if (!ctx->method_inst)
			return FALSE;
	}

	*endbuf = p;
	return TRUE;
}

static MonoClass*
decode_klass_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	MonoImage *image;
	MonoClass *klass, *eklass;
	guint32 token, rank;
	guint8 *p = buf;

	token = decode_value (p, &p);
	if (token == 0) {
		*endbuf = p;
		return NULL;
	}
	if (mono_metadata_token_table (token) == 0) {
		image = load_image (module, decode_value (p, &p));
		if (!image)
			return NULL;
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF + token);
	} else if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
		if (token == MONO_TOKEN_TYPE_SPEC) {
			MonoTypeEnum type = decode_value (p, &p);

			if (type == MONO_TYPE_GENERICINST) {
				MonoClass *gclass;
				MonoGenericContext ctx;
				MonoType *type;

				gclass = decode_klass_ref (module, p, &p);
				g_assert (gclass->generic_container);

				memset (&ctx, 0, sizeof (ctx));
				ctx.class_inst = decode_generic_inst (module, p, &p);
				if (!ctx.class_inst)
					return NULL;
				type = mono_class_inflate_generic_type (&gclass->byval_arg, &ctx);
				klass = mono_class_from_mono_type (type);
				mono_metadata_free_type (type);
			} else if ((type == MONO_TYPE_VAR) || (type == MONO_TYPE_MVAR)) {
				MonoType *t;
				MonoGenericContainer *container;

				int num = decode_value (p, &p);
				gboolean is_method = decode_value (p, &p);

				if (is_method) {
					MonoMethod *method_def;
					g_assert (type == MONO_TYPE_MVAR);
					method_def = decode_method_ref_2 (module, p, &p);
					if (!method_def)
						return NULL;

					container = mono_method_get_generic_container (method_def);
				} else {
					MonoClass *class_def;
					g_assert (type == MONO_TYPE_VAR);
					class_def = decode_klass_ref (module, p, &p);
					if (!class_def)
						return NULL;

					container = class_def->generic_container;
				}

				g_assert (container);

				// FIXME: Memory management
				t = g_new0 (MonoType, 1);
				t->type = type;
				t->data.generic_param = mono_generic_container_get_param (container, num);

				// FIXME: Maybe use types directly to avoid
				// the overhead of creating MonoClass-es
				klass = mono_class_from_mono_type (t);

				g_free (t);
			} else {
				g_assert_not_reached ();
			}
		} else {
			image = load_image (module, decode_value (p, &p));
			if (!image)
				return NULL;
			klass = mono_class_get (image, token);
		}
	} else if (token == MONO_TOKEN_TYPE_DEF) {
		/* Array */
		image = load_image (module, decode_value (p, &p));
		if (!image)
			return NULL;
		rank = decode_value (p, &p);
		eklass = decode_klass_ref (module, p, &p);
		klass = mono_array_class_get (eklass, rank);
	} else {
		g_assert_not_reached ();
	}
	g_assert (klass);
	mono_class_init (klass);

	*endbuf = p;
	return klass;
}

static MonoClassField*
decode_field_info (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	MonoClass *klass = decode_klass_ref (module, buf, &buf);
	guint32 token;
	guint8 *p = buf;

	if (!klass)
		return NULL;

	token = MONO_TOKEN_FIELD_DEF + decode_value (p, &p);

	*endbuf = p;

	return mono_class_get_field (klass, token);
}

/*
 * can_method_ref_match_method:
 *
 *   Determine if calling decode_method_ref_2 on P could return the same method as 
 * METHOD. This is an optimization to avoid calling decode_method_ref_2 () which
 * would create MonoMethods which are not needed etc.
 */
static gboolean
can_method_ref_match_method (MonoAotModule *module, guint8 *buf, MonoMethod *method)
{
	guint8 *p = buf;
	guint32 image_index, value;

	/* Keep this in sync with decode_method_ref () */
	value = decode_value (p, &p);
	image_index = value >> 24;

	if (image_index == MONO_AOT_METHODREF_WRAPPER) {
		guint32 wrapper_type;

		if (!method->wrapper_type)
			return FALSE;

		wrapper_type = decode_value (p, &p);

		if (method->wrapper_type != wrapper_type)
			return FALSE;
	} else if (image_index < MONO_AOT_METHODREF_MIN || image_index == MONO_AOT_METHODREF_METHODSPEC || image_index == MONO_AOT_METHODREF_GINST) {
		if (method->wrapper_type)
			return FALSE;
	}

	return TRUE;
}

/*
 * decode_method_ref:
 *
 *   Decode a method reference, and return its image and token. This avoids loading
 * metadata for the method if the caller does not need it. If the method has no token,
 * then it is loaded from metadata and METHOD is set to the method instance.
 */
static MonoImage*
decode_method_ref (MonoAotModule *module, guint32 *token, MonoMethod **method, gboolean *no_aot_trampoline, guint8 *buf, guint8 **endbuf)
{
	guint32 image_index, value;
	MonoImage *image = NULL;
	guint8 *p = buf;

	if (method)
		*method = NULL;
	if (no_aot_trampoline)
		*no_aot_trampoline = FALSE;

	value = decode_value (p, &p);
	image_index = value >> 24;

	if (image_index == MONO_AOT_METHODREF_NO_AOT_TRAMPOLINE) {
		if (no_aot_trampoline)
			*no_aot_trampoline = TRUE;
		value = decode_value (p, &p);
		image_index = value >> 24;
	}

	if (image_index == MONO_AOT_METHODREF_WRAPPER) {
		guint32 wrapper_type;

		wrapper_type = decode_value (p, &p);

		/* Doesn't matter */
		image = mono_defaults.corlib;

		switch (wrapper_type) {
		case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: {
			MonoMethod *m = decode_method_ref_2 (module, p, &p);

			if (!m)
				return NULL;
			mono_class_init (m->klass);
			*method = mono_marshal_get_remoting_invoke_with_check (m);
			break;
		}
		case MONO_WRAPPER_PROXY_ISINST: {
			MonoClass *klass = decode_klass_ref (module, p, &p);
			if (!klass)
				return NULL;
			*method = mono_marshal_get_proxy_cancast (klass);
			break;
		}
		case MONO_WRAPPER_LDFLD:
		case MONO_WRAPPER_LDFLDA:
		case MONO_WRAPPER_STFLD:
		case MONO_WRAPPER_ISINST: {
			MonoClass *klass = decode_klass_ref (module, p, &p);
			if (!klass)
				return NULL;
			if (wrapper_type == MONO_WRAPPER_LDFLD)
				*method = mono_marshal_get_ldfld_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_LDFLDA)
				*method = mono_marshal_get_ldflda_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_STFLD)
				*method = mono_marshal_get_stfld_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_ISINST)
				*method = mono_marshal_get_isinst (klass);
			else
				g_assert_not_reached ();
			break;
		}
		case MONO_WRAPPER_LDFLD_REMOTE:
			*method = mono_marshal_get_ldfld_remote_wrapper (NULL);
			break;
		case MONO_WRAPPER_STFLD_REMOTE:
			*method = mono_marshal_get_stfld_remote_wrapper (NULL);
			break;
		case MONO_WRAPPER_ALLOC: {
			int atype = decode_value (p, &p);

			*method = mono_gc_get_managed_allocator_by_type (atype);
			break;
		}
		case MONO_WRAPPER_STELEMREF:
			*method = mono_marshal_get_stelemref ();
			break;
		case MONO_WRAPPER_STATIC_RGCTX_INVOKE: {
			MonoMethod *m = decode_method_ref_2 (module, p, &p);

			if (!m)
				return NULL;
			*method = mono_marshal_get_static_rgctx_invoke (m);
			break;
		}
		case MONO_WRAPPER_SYNCHRONIZED: {
			MonoMethod *m = decode_method_ref_2 (module, p, &p);

			if (!m)
				return NULL;
			*method = mono_marshal_get_synchronized_wrapper (m);
			break;
		}
		case MONO_WRAPPER_UNKNOWN: {
			MonoMethodDesc *desc;
			MonoMethod *orig_method;
			int subtype = decode_value (p, &p);

			if (subtype == MONO_AOT_WRAPPER_MONO_ENTER)
				desc = mono_method_desc_new ("Monitor:Enter", FALSE);
			else if (subtype == MONO_AOT_WRAPPER_MONO_EXIT)
				desc = mono_method_desc_new ("Monitor:Exit", FALSE);
			else
				g_assert_not_reached ();
			orig_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
			g_assert (orig_method);
			mono_method_desc_free (desc);
			*method = mono_monitor_get_fast_path (orig_method);
			break;
		}
		default:
			g_assert_not_reached ();
		}
	} else if (image_index == MONO_AOT_METHODREF_WRAPPER_NAME) {
		/* Can't decode these */
		g_assert_not_reached ();
	} else if (image_index == MONO_AOT_METHODREF_METHODSPEC) {
		image_index = decode_value (p, &p);
		*token = decode_value (p, &p);

		image = load_image (module, image_index);
		if (!image)
			return NULL;
	} else if (image_index == MONO_AOT_METHODREF_GINST) {
		MonoClass *klass;
		MonoGenericContext ctx;

		/* 
		 * These methods do not have a token which resolves them, so we 
		 * resolve them immediately.
		 */
		klass = decode_klass_ref (module, p, &p);
		if (!klass)
			return NULL;

		image_index = decode_value (p, &p);
		*token = decode_value (p, &p);

		image = load_image (module, image_index);
		if (!image)
			return NULL;

		*method = mono_get_method_full (image, *token, NULL, NULL);
		if (!(*method))
			return NULL;

		memset (&ctx, 0, sizeof (ctx));

		if (FALSE && klass->generic_class) {
			ctx.class_inst = klass->generic_class->context.class_inst;
			ctx.method_inst = NULL;
 
			*method = mono_class_inflate_generic_method_full (*method, klass, &ctx);
		}			

		memset (&ctx, 0, sizeof (ctx));

		if (!decode_generic_context (module, &ctx, p, &p))
			return NULL;

		*method = mono_class_inflate_generic_method_full (*method, klass, &ctx);
	} else if (image_index == MONO_AOT_METHODREF_ARRAY) {
		MonoClass *klass;
		int method_type;

		klass = decode_klass_ref (module, p, &p);
		if (!klass)
			return NULL;
		method_type = decode_value (p, &p);
		*token = 0;
		switch (method_type) {
		case 0:
			*method = mono_class_get_method_from_name (klass, ".ctor", klass->rank);
			break;
		case 1:
			*method = mono_class_get_method_from_name (klass, ".ctor", klass->rank * 2);
			break;
		case 2:
			*method = mono_class_get_method_from_name (klass, "Get", -1);
			break;
		case 3:
			*method = mono_class_get_method_from_name (klass, "Address", -1);
			break;
		case 4:
			*method = mono_class_get_method_from_name (klass, "Set", -1);
			break;
		default:
			g_assert_not_reached ();
		}
	} else {
		g_assert (image_index < MONO_AOT_METHODREF_MIN);
		*token = MONO_TOKEN_METHOD_DEF | (value & 0xffffff);

		image = load_image (module, image_index);
		if (!image)
			return NULL;
	}

	*endbuf = p;

	return image;
}

/*
 * decode_method_ref_2:
 *
 *   Similar to decode_method_ref, but resolve and return the method itself.
 */
static MonoMethod*
decode_method_ref_2 (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	MonoMethod *method;
	guint32 token;
	MonoImage *image = decode_method_ref (module, &token, &method, NULL, buf, endbuf);

	if (method)
		return method;
	if (!image)
		return NULL;
	method = mono_get_method (image, token, NULL);
	return method;
}

G_GNUC_UNUSED
static void
make_writable (guint8* addr, guint32 len)
{
	guint8 *page_start;
	int pages, err;

	if (mono_aot_only)
		g_error ("Attempt to make AOT memory writable while running in aot-only mode.\n");

	page_start = (guint8 *) (((gssize) (addr)) & ~ (mono_pagesize () - 1));
	pages = (addr + len - page_start + mono_pagesize () - 1) / mono_pagesize ();

	err = mono_mprotect (page_start, pages * mono_pagesize (), MONO_MMAP_READ | MONO_MMAP_WRITE | MONO_MMAP_EXEC);
	g_assert (err == 0);
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
find_symbol (MonoDl *module, gpointer *globals, const char *name, gpointer *value)
{
	if (globals) {
		int i = 0;

		*value = NULL;
		for (i = 0; globals [i]; i+= 2) {
			if (strcmp (globals [i], name) == 0) {
				*value = globals [i + 1];
				break;
			}
		}
	} else {
		mono_dl_symbol (module, name, value);
	}
}

static void
load_aot_module (MonoAssembly *assembly, gpointer user_data)
{
	char *aot_name;
	MonoAotModule *amodule;
	MonoDl *sofile;
	gboolean usable = TRUE;
	char *saved_guid = NULL;
	char *aot_version = NULL;
	char *runtime_version, *build_info;
	char *opt_flags = NULL;
	gpointer *globals;
	gboolean full_aot = FALSE;
	MonoAotFileInfo *file_info = NULL;
	int i;
	gpointer *got_addr;

	if (mono_compile_aot)
		return;

	if (assembly->image->aot_module)
		/* 
		 * Already loaded. This can happen because the assembly loading code might invoke
		 * the assembly load hooks multiple times for the same assembly.
		 */
		return;

	if (assembly->image->dynamic)
		return;

	if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS)
		return;

	mono_aot_lock ();
	if (static_aot_modules)
		globals = g_hash_table_lookup (static_aot_modules, assembly->aname.name);
	else
		globals = NULL;
	mono_aot_unlock ();

	if (globals) {
		/* Statically linked AOT module */
		sofile = NULL;
		aot_name = g_strdup_printf ("%s", assembly->aname.name);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "Found statically linked AOT module '%s'.\n", aot_name);
	} else {
		if (use_aot_cache)
			sofile = load_aot_module_from_cache (assembly, &aot_name);
		else {
			char *err;
			aot_name = g_strdup_printf ("%s%s", assembly->image->name, SHARED_EXT);

			sofile = mono_dl_open (aot_name, MONO_DL_LAZY, &err);

			if (!sofile) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT failed to load AOT module %s: %s\n", aot_name, err);
				g_free (err);
			}
		}
	}

	if (!sofile && !globals) {
		if (mono_aot_only) {
			fprintf (stderr, "Failed to load AOT module '%s' in aot-only mode.\n", aot_name);
			exit (1);
		}
		g_free (aot_name);
		return;
	}

	find_symbol (sofile, globals, "mono_assembly_guid", (gpointer *) &saved_guid);
	find_symbol (sofile, globals, "mono_aot_version", (gpointer *) &aot_version);
	find_symbol (sofile, globals, "mono_aot_opt_flags", (gpointer *)&opt_flags);
	find_symbol (sofile, globals, "mono_runtime_version", (gpointer *)&runtime_version);
	find_symbol (sofile, globals, "mono_aot_got_addr", (gpointer *)&got_addr);

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

	build_info = mono_get_runtime_build_info ();
	if (!runtime_version || ((strlen (runtime_version) > 0 && strcmp (runtime_version, build_info)))) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s is compiled against runtime version '%s' while this runtime has version '%s'.\n", aot_name, runtime_version, build_info);
		usable = FALSE;
	}
	g_free (build_info);

	{
		char *full_aot_str;

		find_symbol (sofile, globals, "mono_aot_full_aot", (gpointer *)&full_aot_str);

		if (full_aot_str && !strcmp (full_aot_str, "TRUE"))
			full_aot = TRUE;
	}

	if (mono_aot_only && !full_aot) {
		fprintf (stderr, "Can't use AOT image '%s' in aot-only mode because it is not compiled with --aot=full.\n", aot_name);
		exit (1);
	}
	if (!mono_aot_only && full_aot) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module %s is compiled with --aot=full.\n", aot_name);
		usable = FALSE;
	}

	if (!usable) {
		if (mono_aot_only) {
			fprintf (stderr, "Failed to load AOT module '%s' while running in aot-only mode.\n", aot_name);
			exit (1);
		}
		g_free (aot_name);
		if (sofile)
			mono_dl_close (sofile);
		assembly->image->aot_module = NULL;
		return;
	}

	find_symbol (sofile, globals, "mono_aot_file_info", (gpointer*)&file_info);
	g_assert (file_info);

	amodule = g_new0 (MonoAotModule, 1);
	amodule->aot_name = aot_name;
	amodule->assembly = assembly;

	memcpy (&amodule->info, file_info, sizeof (*file_info));

	amodule->got = *got_addr;
	amodule->got [0] = assembly->image;
	amodule->globals = globals;
	amodule->sofile = sofile;
	amodule->method_to_code = g_hash_table_new (mono_aligned_addr_hash, NULL);

	sscanf (opt_flags, "%d", &amodule->opts);		

	/* Read image table */
	{
		guint32 table_len, i;
		char *table = NULL;

		find_symbol (sofile, globals, "mono_image_table", (gpointer *)&table);
		g_assert (table);

		table_len = *(guint32*)table;
		table += sizeof (guint32);
		amodule->image_table = g_new0 (MonoImage*, table_len);
		amodule->image_names = g_new0 (MonoAssemblyName, table_len);
		amodule->image_guids = g_new0 (char*, table_len);
		amodule->image_table_len = table_len;
		for (i = 0; i < table_len; ++i) {
			MonoAssemblyName *aname = &(amodule->image_names [i]);

			aname->name = g_strdup (table);
			table += strlen (table) + 1;
			amodule->image_guids [i] = g_strdup (table);
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
	find_symbol (sofile, globals, "method_offsets", (gpointer*)&amodule->code_offsets);
	find_symbol (sofile, globals, "methods", (gpointer*)&amodule->code);
	find_symbol (sofile, globals, "methods_end", (gpointer*)&amodule->code_end);
	find_symbol (sofile, globals, "method_info_offsets", (gpointer*)&amodule->method_info_offsets);
	find_symbol (sofile, globals, "method_info", (gpointer*)&amodule->method_info);
	find_symbol (sofile, globals, "ex_info_offsets", (gpointer*)&amodule->ex_info_offsets);
	find_symbol (sofile, globals, "ex_info", (gpointer*)&amodule->ex_info);
	find_symbol (sofile, globals, "method_order", (gpointer*)&amodule->method_order);
	find_symbol (sofile, globals, "method_order_end", (gpointer*)&amodule->method_order_end);
	find_symbol (sofile, globals, "class_info", (gpointer*)&amodule->class_info);
	find_symbol (sofile, globals, "class_info_offsets", (gpointer*)&amodule->class_info_offsets);
	find_symbol (sofile, globals, "class_name_table", (gpointer *)&amodule->class_name_table);
	find_symbol (sofile, globals, "extra_method_table", (gpointer *)&amodule->extra_method_table);
	find_symbol (sofile, globals, "extra_method_info", (gpointer *)&amodule->extra_method_info);
	find_symbol (sofile, globals, "extra_method_info_offsets", (gpointer *)&amodule->extra_method_info_offsets);
	find_symbol (sofile, globals, "got_info", (gpointer*)&amodule->got_info);
	find_symbol (sofile, globals, "got_info_offsets", (gpointer*)&amodule->got_info_offsets);
	find_symbol (sofile, globals, "specific_trampolines", (gpointer*)&(amodule->trampolines [MONO_AOT_TRAMP_SPECIFIC]));
	find_symbol (sofile, globals, "static_rgctx_trampolines", (gpointer*)&(amodule->trampolines [MONO_AOT_TRAMP_STATIC_RGCTX]));
	find_symbol (sofile, globals, "imt_thunks", (gpointer*)&(amodule->trampolines [MONO_AOT_TRAMP_IMT_THUNK]));
	find_symbol (sofile, globals, "unwind_info", (gpointer)&amodule->unwind_info);
	find_symbol (sofile, globals, "mem_end", (gpointer*)&amodule->mem_end);

	amodule->mem_begin = amodule->code;

	find_symbol (sofile, globals, "plt", (gpointer*)&amodule->plt);
	find_symbol (sofile, globals, "plt_end", (gpointer*)&amodule->plt_end);

	if (make_unreadable) {
#ifndef PLATFORM_WIN32
		guint8 *addr;
		guint8 *page_start;
		int pages, err, len;

		addr = amodule->mem_begin;
		len = amodule->mem_end - amodule->mem_begin;

		/* Round down in both directions to avoid modifying data which is not ours */
		page_start = (guint8 *) (((gssize) (addr)) & ~ (mono_pagesize () - 1)) + mono_pagesize ();
		pages = ((addr + len - page_start + mono_pagesize () - 1) / mono_pagesize ()) - 1;
		err = mono_mprotect (page_start, pages * mono_pagesize (), MONO_MMAP_NONE);
		g_assert (err == 0);
#endif
	}

	mono_aot_lock ();

	aot_code_low_addr = MIN (aot_code_low_addr, (gsize)amodule->code);
	aot_code_high_addr = MAX (aot_code_high_addr, (gsize)amodule->code_end);

	g_hash_table_insert (aot_modules, assembly, amodule);
	mono_aot_unlock ();

	mono_jit_info_add_aot_module (assembly->image, amodule->code, amodule->code_end);

	assembly->image->aot_module = amodule;

	/*
	 * Since we store methoddef and classdef tokens when referring to methods/classes in
	 * referenced assemblies, we depend on the exact versions of the referenced assemblies.
	 * MS calls this 'hard binding'. This means we have to load all referenced assemblies
	 * non-lazily, since we can't handle out-of-date errors later.
	 */
	for (i = 0; i < amodule->image_table_len; ++i)
		load_image (amodule, i);

	if (amodule->out_of_date) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT Module %s is unusable because a dependency is out-of-date.\n", assembly->image->name);
		if (mono_aot_only) {
			fprintf (stderr, "Failed to load AOT module '%s' while running in aot-only mode because a dependency cannot be found or it is out of date.\n", aot_name);
			exit (1);
		}
	}
	else
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT loaded AOT Module for %s.\n", assembly->image->name);
}

/*
 * mono_aot_register_globals:
 *
 *   This is called by the ctor function in AOT images compiled with the
 * 'no-dlsym' option.
 */
void
mono_aot_register_globals (gpointer *globals)
{
	g_assert_not_reached ();
}

/*
 * mono_aot_register_module:
 *
 *   This should be called by embedding code to register AOT modules statically linked
 * into the executable. AOT_INFO should be the value of the 
 * 'mono_aot_module_<ASSEMBLY_NAME>_info' global symbol from the AOT module.
 */
void
mono_aot_register_module (gpointer *aot_info)
{
	gpointer *globals;
	char *aname;

	globals = aot_info;
	g_assert (globals);

	/* Determine the assembly name */
	find_symbol (NULL, globals, "mono_aot_assembly_name", (gpointer*)&aname);
	g_assert (aname);

	/* This could be called before startup */
	if (aot_modules)
		mono_aot_lock ();

	if (!static_aot_modules)
		static_aot_modules = g_hash_table_new (g_str_hash, g_str_equal);

	g_hash_table_insert (static_aot_modules, aname, globals);

	if (aot_modules)
		mono_aot_unlock ();
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
		MonoImage *cctor_image = decode_method_ref (module, &info->cctor_token, NULL, NULL, buf, &buf);
		if (!cctor_image)
			return FALSE;
	}
	if (info->has_finalize) {
		info->finalize_image = decode_method_ref (module, &info->finalize_token, NULL, NULL, buf, &buf);
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
	MonoClass *klass = vtable->klass;
	MonoAotModule *aot_module = klass->image->aot_module;
	guint8 *info, *p;
	MonoCachedClassInfo class_info;
	gboolean err;
	guint32 token;
	MonoImage *image;
	gboolean no_aot_trampoline;

	if (MONO_CLASS_IS_INTERFACE (klass) || klass->rank || !aot_module)
		return NULL;

	info = &aot_module->class_info [aot_module->class_info_offsets [mono_metadata_token_index (klass->type_token) - 1]];
	p = info;

	err = decode_cached_class_info (aot_module, &class_info, p, &p);
	if (!err)
		return NULL;

	for (i = 0; i < slot; ++i)
		decode_method_ref (aot_module, &token, NULL, NULL, p, &p);

	image = decode_method_ref (aot_module, &token, NULL, &no_aot_trampoline, p, &p);
	if (!image)
		return NULL;
	if (no_aot_trampoline)
		return NULL;

	if (mono_metadata_token_index (token) == 0)
		return NULL;

	return mono_aot_get_method_from_token (domain, image, token);
}

gboolean
mono_aot_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	MonoAotModule *aot_module = klass->image->aot_module;
	guint8 *p;
	gboolean err;

	if (klass->rank || !aot_module)
		return FALSE;

	p = (guint8*)&aot_module->class_info [aot_module->class_info_offsets [mono_metadata_token_index (klass->type_token) - 1]];

	err = decode_cached_class_info (aot_module, res, p, &p);
	if (!err)
		return FALSE;

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
	MonoAotModule *aot_module = image->aot_module;
	guint16 *table, *entry;
	guint16 table_size;
	guint32 hash;
	char full_name_buf [1024];
	char *full_name;
	const char *name2, *name_space2;
	MonoTableInfo  *t;
	guint32 cols [MONO_TYPEDEF_SIZE];
	GHashTable *nspace_table;

	if (!aot_module || !aot_module->class_name_table)
		return FALSE;

	mono_aot_lock ();

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
	hash = mono_aot_str_hash (full_name) % table_size;
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

/*
 * LOCKING: Acquires the domain lock.
 */
static MonoJitInfo*
decode_exception_debug_info (MonoAotModule *aot_module, MonoDomain *domain, 
							 MonoMethod *method, guint8* ex_info, guint8 *code)
{
	int i, buf_len;
	MonoJitInfo *jinfo;
	guint code_len, used_int_regs, flags;
	gboolean has_generic_jit_info, has_dwarf_unwind_info;
	guint8 *p;
	MonoMethodHeader *header;
	int generic_info_size;

	header = mono_method_get_header (method);

	/* Load the method info from the AOT file */

	p = ex_info;
	code_len = decode_value (p, &p);
	flags = decode_value (p, &p);
	has_generic_jit_info = (flags & 1) != 0;
	has_dwarf_unwind_info = (flags & 2) != 0;
	if (has_dwarf_unwind_info) {
		guint32 offset;

		offset = decode_value (p, &p);
		g_assert (offset < (1 << 30));
		used_int_regs = offset;
	} else {
		used_int_regs = decode_value (p, &p);
	}
	if (has_generic_jit_info)
		generic_info_size = sizeof (MonoGenericJitInfo);
	else
		generic_info_size = 0;

	/* Exception table */
	if (header && header->num_clauses) {
		jinfo = 
			mono_domain_alloc0 (domain, sizeof (MonoJitInfo) + (sizeof (MonoJitExceptionInfo) * header->num_clauses) + generic_info_size);
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
	else {
		jinfo = mono_domain_alloc0 (domain, sizeof (MonoJitInfo) + generic_info_size);
	}

	jinfo->code_size = code_len;
	jinfo->used_regs = used_int_regs;
	jinfo->method = method;
	jinfo->code_start = code;
	jinfo->domain_neutral = 0;
	jinfo->from_aot = 1;

	if (has_generic_jit_info) {
		MonoGenericJitInfo *gi;

		jinfo->has_generic_jit_info = 1;

		gi = mono_jit_info_get_generic_jit_info (jinfo);
		g_assert (gi);

		gi->has_this = decode_value (p, &p);
		gi->this_reg = decode_value (p, &p);
		gi->this_offset = decode_value (p, &p);

		/* This currently contains no data */
		gi->generic_sharing_context = g_new0 (MonoGenericSharingContext, 1);

		jinfo->method = decode_method_ref_2 (aot_module, p, &p);
	}

	/* Load debug info */
	buf_len = decode_value (p, &p);
	mono_debug_add_aot_method (domain, method, code, p, buf_len);
	
	return jinfo;
}

/*
 * mono_aot_get_unwind_info:
 *
 *   Return a pointer to the DWARF unwind info belonging to JI.
 */
guint8*
mono_aot_get_unwind_info (MonoJitInfo *ji, guint32 *unwind_info_len)
{
	MonoAotModule *amodule = ji->method->klass->image->aot_module;
	guint8 *p;

	g_assert (amodule);
	g_assert (ji->from_aot);

	p = amodule->unwind_info + ji->used_regs;
	*unwind_info_len = decode_value (p, &p);
	return p;
}

MonoJitInfo *
mono_aot_find_jit_info (MonoDomain *domain, MonoImage *image, gpointer addr)
{
	int pos, left, right, offset, offset1, offset2, last_offset, new_offset;
	int page_index, method_index, table_len, is_wrapper;
	guint32 token;
	MonoAotModule *amodule = image->aot_module;
	MonoMethod *method;
	MonoJitInfo *jinfo;
	guint8 *code, *ex_info, *p;
	guint32 *table, *ptr;
	gboolean found;

	if (!amodule)
		return NULL;

	if (domain != mono_get_root_domain ())
		/* FIXME: */
		return NULL;

	offset = (guint8*)addr - amodule->code;

	/* First search through the index */
	ptr = amodule->method_order;
	last_offset = 0;
	page_index = 0;
	found = FALSE;

	if (*ptr == 0xffffff)
		return NULL;
	ptr ++;

	while (*ptr != 0xffffff) {
		guint32 method_index = ptr [0];
		new_offset = amodule->code_offsets [method_index];

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
	table_len = amodule->method_order_end - table;

	g_assert (table <= amodule->method_order_end);

	if (found) {
		left = (page_index * 1024);
		right = left + 1024;

		if (right > table_len)
			right = table_len;

		offset1 = amodule->code_offsets [table [left]];
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

		g_assert (table + pos <= amodule->method_order_end);

		//printf ("Pos: %5d < %5d < %5d Offset: 0x%05x < 0x%05x < 0x%05x\n", left, pos, right, amodule->code_offsets [table [left]], offset, amodule->code_offsets [table [right]]);

		offset1 = amodule->code_offsets [table [pos]];
		if (table + pos + 1 >= amodule->method_order_end)
			offset2 = amodule->code_end - amodule->code;
		else
			offset2 = amodule->code_offsets [table [pos + 1]];

		if (offset < offset1)
			right = pos;
		else if (offset >= offset2)
			left = pos + 1;
		else
			break;
	}

	method_index = table [pos];

	/* Might be a wrapper/extra method */
	if (amodule->extra_methods) {
		mono_aot_lock ();
		method = g_hash_table_lookup (amodule->extra_methods, GUINT_TO_POINTER (method_index));
		mono_aot_unlock ();
	} else {
		method = NULL;
	}

	if (!method) {
		if (method_index >= image->tables [MONO_TABLE_METHOD].rows) {
			/* 
			 * This is hit for extra methods which are called directly, so they are
			 * not in amodule->extra_methods.
			 */
			table_len = amodule->extra_method_info_offsets [0];
			table = amodule->extra_method_info_offsets + 1;
			left = 0;
			right = table_len;
			pos = 0;

			/* Binary search */
			while (TRUE) {
				pos = ((left + right) / 2);

				g_assert (pos < table_len);

				if (table [pos * 2] < method_index)
					left = pos + 1;
				else if (table [pos * 2] > method_index)
					right = pos;
				else
					break;
			}

			p = amodule->extra_method_info + table [(pos * 2) + 1];
			is_wrapper = decode_value (p, &p);
			g_assert (!is_wrapper);
			method = decode_method_ref_2 (amodule, p, &p);
			g_assert (method);
		} else {
			token = mono_metadata_make_token (MONO_TABLE_METHOD, method_index + 1);
			method = mono_get_method (image, token, NULL);
		}
	}

	/* FIXME: */
	g_assert (method);

	//printf ("F: %s\n", mono_method_full_name (method, TRUE));

	code = &amodule->code [amodule->code_offsets [method_index]];
	ex_info = &amodule->ex_info [amodule->ex_info_offsets [method_index]];

	jinfo = decode_exception_debug_info (amodule, domain, method, ex_info, code);

	g_assert ((guint8*)addr >= (guint8*)jinfo->code_start);
	g_assert ((guint8*)addr < (guint8*)jinfo->code_start + jinfo->code_size);

	/* Add it to the normal JitInfo tables */
	mono_jit_info_table_add (domain, jinfo);
	
	return jinfo;
}

static gboolean
decode_patch (MonoAotModule *aot_module, MonoMemPool *mp, MonoJumpInfo *ji, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;
	gpointer *table;
	MonoImage *image;
	int i;

	switch (ji->type) {
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_ICALL_ADDR:
	case MONO_PATCH_INFO_METHOD_RGCTX: {
		guint32 token;
		MonoMethod *method;
		gboolean no_aot_trampoline;

		image = decode_method_ref (aot_module, &token, &method, &no_aot_trampoline, p, &p);
		if (!image)
			goto cleanup;

		if (!method && !mono_aot_only && !no_aot_trampoline && (ji->type == MONO_PATCH_INFO_METHOD) && (mono_metadata_token_table (token) == MONO_TABLE_METHOD)) {
			ji->data.target = mono_create_jit_trampoline_from_token (image, token);
			ji->type = MONO_PATCH_INFO_ABS;
		}
		else {
			if (method)
				ji->data.method = method;
			else
				ji->data.method = mono_get_method (image, token, NULL);
			g_assert (ji->data.method);
			mono_class_init (ji->data.method->klass);
		}
		break;
	}
	case MONO_PATCH_INFO_INTERNAL_METHOD:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR: {
		guint32 len = decode_value (p, &p);

		ji->data.name = (char*)p;
		p += len + 1;
		break;
	}
	case MONO_PATCH_INFO_METHODCONST:
		/* Shared */
		ji->data.method = decode_method_ref_2 (aot_module, p, &p);
		if (!ji->data.method)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
		/* Shared */
		ji->data.klass = decode_klass_ref (aot_module, p, &p);
		if (!ji->data.klass)
			goto cleanup;
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
		/* Shared */
		ji->data.field = decode_field_info (aot_module, p, &p);
		if (!ji->data.field)
			goto cleanup;
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
		/* Shared */
		image = load_image (aot_module, decode_value (p, &p));
		if (!image)
			goto cleanup;
		ji->data.token = mono_jump_info_token_new (mp, image, decode_value (p, &p));

		ji->data.token->has_context = decode_value (p, &p);
		if (ji->data.token->has_context) {
			gboolean res = decode_generic_context (aot_module, &ji->data.token->context, p, &p);
			if (!res)
				goto cleanup;
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
	case MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG:
	case MONO_PATCH_INFO_GENERIC_CLASS_INIT:
	case MONO_PATCH_INFO_MONITOR_ENTER:
	case MONO_PATCH_INFO_MONITOR_EXIT:
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH: {
		gboolean res;
		MonoJumpInfoRgctxEntry *entry;

		entry = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoRgctxEntry));
		entry->method = decode_method_ref_2 (aot_module, p, &p);
		entry->in_mrgctx = decode_value (p, &p);
		entry->info_type = decode_value (p, &p);
		entry->data = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));
		entry->data->type = decode_value (p, &p);
		
		res = decode_patch (aot_module, mp, entry->data, p, &p);
		if (!res)
			goto cleanup;
		ji->data.rgctx_entry = entry;
		break;
	}
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
	int pindex;
	guint8 *p;

	p = buf;

	patches = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo) * n_patches);

	*got_slots = g_malloc (sizeof (guint32) * n_patches);

	for (pindex = 0; pindex < n_patches; ++pindex) {
		MonoJumpInfo *ji = &patches [pindex];
		guint8 *shared_p;
		gboolean res;
		guint32 got_offset;

		ji->type = decode_value (p, &p);

		if (mono_aot_is_shared_got_patch (ji)) {
			got_offset = decode_value (p, &p);

			if (aot_module->got [got_offset]) {
				/* Already loaded */
				//printf ("HIT!\n");
			} else {
				shared_p = aot_module->got_info + aot_module->got_info_offsets [got_offset];

				res = decode_patch (aot_module, mp, ji, shared_p, &shared_p);
				if (!res)
					goto cleanup;
			}
		} else {
			res = decode_patch (aot_module, mp, ji, p, &p);
			if (!res)
				goto cleanup;

			got_offset = got_index ++;
		}

		(*got_slots) [pindex] = got_offset;
	}

	*endbuf = p;
	return patches;

 cleanup:
	g_free (*got_slots);
	*got_slots = NULL;

	return NULL;
}

static void
register_jump_target_got_slot (MonoDomain *domain, MonoMethod *method, gpointer *got_slot)
{
	/*
	 * Jump addresses cannot be patched by the trampoline code since it
	 * does not have access to the caller's address. Instead, we collect
	 * the addresses of the GOT slots pointing to a method, and patch
	 * them after the method has been compiled.
	 */
	MonoJitDomainInfo *info = domain_jit_info (domain);
	GSList *list;
		
	mono_domain_lock (domain);
	if (!info->jump_target_got_slot_hash)
		info->jump_target_got_slot_hash = g_hash_table_new (NULL, NULL);
	list = g_hash_table_lookup (info->jump_target_got_slot_hash, method);
	list = g_slist_prepend (list, got_slot);
	g_hash_table_insert (info->jump_target_got_slot_hash, method, list);
	mono_domain_unlock (domain);
}

/*
 * load_method:
 *
 *   Load the method identified by METHOD_INDEX from the AOT image. Return a
 * pointer to the native code of the method, or NULL if not found.
 * METHOD might not be set if the caller only has the image/token info.
 */
static gpointer
load_method (MonoDomain *domain, MonoAotModule *aot_module, MonoImage *image, MonoMethod *method, guint32 token, int method_index)
{
	MonoClass *klass;
	gboolean from_plt = method == NULL;
	MonoMemPool *mp;
	int i, pindex, got_index = 0, n_patches, used_strings;
	gboolean keep_patches = TRUE;
	guint8 *p, *ex_info;
	MonoJitInfo *jinfo = NULL;
	guint8 *code, *info;

	if (mono_profiler_get_events () & MONO_PROFILE_ENTER_LEAVE)
		return NULL;

	if ((domain != mono_get_root_domain ()) && (!(aot_module->opts & MONO_OPT_SHARED)))
		/* Non shared AOT code can't be used in other appdomains */
		return NULL;

	if (aot_module->out_of_date)
		return NULL;

	if (aot_module->code_offsets [method_index] == 0xffffffff) {
		if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
			char *full_name;

			if (!method)
				method = mono_get_method (image, token, NULL);
			full_name = mono_method_full_name (method, TRUE);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT NOT FOUND: %s.\n", full_name);
			g_free (full_name);
		}
		return NULL;
	}

	code = &aot_module->code [aot_module->code_offsets [method_index]];
	info = &aot_module->method_info [aot_module->method_info_offsets [method_index]];

	mono_aot_lock ();
	if (!aot_module->methods_loaded)
		aot_module->methods_loaded = g_new0 (guint32, image->tables [MONO_TABLE_METHOD].rows + 1);
	mono_aot_unlock ();

	if ((aot_module->methods_loaded [method_index / 32] >> (method_index % 32)) & 0x1)
		return code;

	if (mono_last_aot_method != -1) {
		if (mono_jit_stats.methods_aot > mono_last_aot_method)
				return NULL;
		else
			if (method && mono_jit_stats.methods_aot == mono_last_aot_method)
				printf ("LAST AOT METHOD: %s.%s.%s.\n", method->klass->name_space, method->klass->name, method->name);
	}

	p = info;

	if (method) {
		klass = method->klass;
		decode_klass_ref (aot_module, p, &p);
	} else {
		klass = decode_klass_ref (aot_module, p, &p);
	}

	if (aot_module->opts & MONO_OPT_SHARED)
		used_strings = decode_value (p, &p);
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint token = decode_value (p, &p);
		mono_ldstr (mono_get_root_domain (), image, mono_metadata_token_index (token));
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

		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];

			if (!aot_module->got [got_slots [pindex]]) {
				aot_module->got [got_slots [pindex]] = mono_resolve_patch_target (method, domain, code, ji, TRUE);
				if (ji->type == MONO_PATCH_INFO_METHOD_JUMP)
					register_jump_target_got_slot (domain, ji->data.method, &(aot_module->got [got_slots [pindex]]));
			}
			ji->type = MONO_PATCH_INFO_NONE;
		}

		g_free (got_slots);

		if (!keep_patches)
			mono_mempool_destroy (mp);
	}

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
		char *full_name;

		if (!method)
			method = mono_get_method (image, token, NULL);

		full_name = mono_method_full_name (method, TRUE);

		if (!jinfo) {
			ex_info = &aot_module->ex_info [aot_module->ex_info_offsets [method_index]];
			jinfo = decode_exception_debug_info (aot_module, domain, method, ex_info, code);
		}

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT FOUND AOT compiled code for %s %p - %p %p\n", full_name, code, code + jinfo->code_size, info);
		g_free (full_name);
	}

	mono_aot_lock ();

	mono_jit_stats.methods_aot++;

	aot_module->methods_loaded [method_index / 32] |= 1 << (method_index % 32);

	init_plt (aot_module);

	if (method && method->wrapper_type)
		g_hash_table_insert (aot_module->method_to_code, method, code);

	mono_aot_unlock ();

	if (from_plt && klass && !klass->generic_container)
		mono_runtime_class_init (mono_class_vtable (domain, klass));

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

static guint32
find_extra_method_in_amodule (MonoAotModule *amodule, MonoMethod *method)
{
	guint32 table_size, entry_size, hash;
	guint32 *table, *entry;
	char *name = NULL;
	guint32 index;
	static guint32 n_extra_decodes;

	if (!amodule)
		return 0xffffff;

	table_size = amodule->extra_method_table [0];
	table = amodule->extra_method_table + 1;
	entry_size = 3;

	if (method->wrapper_type) {
		name = mono_aot_wrapper_name (method);
	}

	hash = mono_aot_method_hash (method) % table_size;

	entry = &table [hash * entry_size];

	if (entry [0] == 0)
		return 0xffffff;

	index = 0xffffff;
	while (TRUE) {
		guint32 key = entry [0];
		guint32 value = entry [1];
		guint32 next = entry [entry_size - 1];
		MonoMethod *m;
		guint8 *p;
		int is_wrapper_name;

		p = amodule->extra_method_info + key;
		is_wrapper_name = decode_value (p, &p);
		if (is_wrapper_name) {
			int wrapper_type = decode_value (p, &p);
			if (wrapper_type == method->wrapper_type && !strcmp (name, (char*)p)) {
				index = value;
				break;
			}
		} else if (can_method_ref_match_method (amodule, p, method)) {
			mono_aot_lock ();
			if (!amodule->method_ref_to_method)
				amodule->method_ref_to_method = g_hash_table_new (NULL, NULL);
			m = g_hash_table_lookup (amodule->method_ref_to_method, p);
			mono_aot_unlock ();
			if (!m) {
				guint8 *orig_p = p;
				m = decode_method_ref_2 (amodule, p, &p);
				if (m) {
					mono_aot_lock ();
					g_hash_table_insert (amodule->method_ref_to_method, orig_p, m);
					mono_aot_unlock ();
				}
			}
			if (m == method) {
				index = value;
				break;
			}

			/* Special case: wrappers of shared generic methods */
			if (m && method->wrapper_type && m->wrapper_type == m->wrapper_type &&
				method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED) {
				MonoMethod *w1 = mono_marshal_method_from_wrapper (method);
				MonoMethod *w2 = mono_marshal_method_from_wrapper (m);

				if (w1->is_inflated && ((MonoMethodInflated *)w1)->declaring == w2) {
					index = value;
					break;
				}
			}

			/* Methods decoded needlessly */
			/*
			if (m)
				printf ("%d %s %s\n", n_extra_decodes, mono_method_full_name (method, TRUE), mono_method_full_name (m, TRUE));
			*/
			n_extra_decodes ++;
		}

		if (next != 0)
			entry = &table [next * entry_size];
		else
			break;
	}

	g_free (name);
	return index;
}

static void
add_module_cb (gpointer key, gpointer value, gpointer user_data)
{
	g_ptr_array_add ((GPtrArray*)user_data, value);
}

/*
 * find_extra_method:
 *
 *   Try finding METHOD in the extra_method table in all AOT images.
 * Return its method index, or 0xffffff if not found. Set OUT_AMODULE to the AOT
 * module where the method was found.
 */
static guint32
find_extra_method (MonoMethod *method, MonoAotModule **out_amodule)
{
	guint32 index;
	GPtrArray *modules;
	int i;

	/* Try the method's module first */
	*out_amodule = method->klass->image->aot_module;
	index = find_extra_method_in_amodule (method->klass->image->aot_module, method);
	if (index != 0xffffff)
		return index;

	/* 
	 * Try all other modules.
	 * This is needed because generic instances klass->image points to the image
	 * containing the generic definition, but the native code is generated to the
	 * AOT image which contains the reference.
	 */

	/* Make a copy to avoid doing the search inside the aot lock */
	modules = g_ptr_array_new ();
	mono_aot_lock ();
	g_hash_table_foreach (aot_modules, add_module_cb, modules);
	mono_aot_unlock ();

	index = 0xffffff;
	for (i = 0; i < modules->len; ++i) {
		MonoAotModule *amodule = g_ptr_array_index (modules, i);

		if (amodule != method->klass->image->aot_module)
			index = find_extra_method_in_amodule (amodule, method);
		if (index != 0xffffff) {
			*out_amodule = amodule;
			break;
		}
	}
	
	g_ptr_array_free (modules, TRUE);

	return index;
}

gpointer
mono_aot_get_method (MonoDomain *domain, MonoMethod *method)
{
	MonoClass *klass = method->klass;
	guint32 method_index;
	MonoAotModule *amodule = klass->image->aot_module;
	guint8 *code;

	if (!amodule)
		return NULL;

	if (amodule->out_of_date)
		return NULL;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return NULL;

	g_assert (klass->inited);

	/* Find method index */
	if (method->is_inflated && mono_method_is_generic_sharable_impl (method, FALSE)) {
		method = mono_method_get_declaring_generic_method (method);
		method_index = mono_metadata_token_index (method->token) - 1;
	} else if (method->is_inflated || !method->token) {
		/* This hash table is used to avoid the slower search in the extra_method_table in the AOT image */
		mono_aot_lock ();
		code = g_hash_table_lookup (amodule->method_to_code, method);
		mono_aot_unlock ();
		if (code)
			return code;

		method_index = find_extra_method (method, &amodule);
		if (method_index == 0xffffff) {
			if (mono_aot_only && mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
				char *full_name;

				full_name = mono_method_full_name (method, TRUE);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT NOT FOUND: %s.\n", full_name);
				g_free (full_name);
			}
			return NULL;
		}

		if (method_index == 0xffffff)
			return NULL;

		/* Needed by find_jit_info */
		mono_aot_lock ();
		if (!amodule->extra_methods)
			amodule->extra_methods = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (amodule->extra_methods, GUINT_TO_POINTER (method_index), method);
		mono_aot_unlock ();
	} else {
		/* Common case */
		method_index = mono_metadata_token_index (method->token) - 1;
	}

	return load_method (domain, amodule, klass->image, method, method->token, method_index);
}

/**
 * Same as mono_aot_get_method, but we try to avoid loading any metadata from the
 * method.
 */
gpointer
mono_aot_get_method_from_token (MonoDomain *domain, MonoImage *image, guint32 token)
{
	MonoAotModule *aot_module = image->aot_module;
	int method_index;

	if (!aot_module)
		return NULL;

	method_index = mono_metadata_token_index (token) - 1;

	return load_method (domain, aot_module, image, NULL, token, method_index);
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

	if (aot_module->got && (data->addr >= (guint8*)(aot_module->got)) && (data->addr < (guint8*)(aot_module->got + aot_module->info.got_size)))
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
	MonoMemPool *mp;

	//printf ("DYN: %p %d\n", aot_module, plt_info_offset);

	p = &module->got_info [plt_info_offset];

	ji.type = decode_value (p, &p);

	mp = mono_mempool_new_size (512);
	res = decode_patch (module, mp, &ji, p, &p);
	// FIXME: Error handling (how ?)
	g_assert (res);

	target = mono_resolve_patch_target (NULL, mono_domain_get (), NULL, &ji, TRUE);

	mono_mempool_destroy (mp);

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
init_plt (MonoAotModule *amodule)
{
#ifdef MONO_ARCH_AOT_SUPPORTED
#ifdef __i386__
	guint8 *buf = amodule->plt;
#elif defined(__x86_64__) || defined(__arm__)
	int i;
#endif
	gpointer tramp;

	if (amodule->plt_inited)
		return;

	tramp = mono_create_specific_trampoline (amodule, MONO_TRAMPOLINE_AOT_PLT, mono_get_root_domain (), NULL);

#ifdef __i386__
	/* Initialize the first PLT entry */
	make_writable (amodule->plt, amodule->plt_end - amodule->plt);
	x86_jump_code (buf, tramp);
#elif defined(__x86_64__) || defined(__arm__)
	/*
	 * Initialize the PLT entries in the GOT to point to the default targets.
	 */

	 /* The first entry points to the AOT trampoline */
	 ((gpointer*)amodule->got)[amodule->info.plt_got_offset_base] = tramp;
	 for (i = 1; i < amodule->info.plt_size; ++i)
		 /* All the default entries point to the first entry */
		 ((gpointer*)amodule->got)[amodule->info.plt_got_offset_base + i] = amodule->plt;
#else
	g_assert_not_reached ();
#endif

	amodule->plt_inited = TRUE;
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
 * mono_aot_get_plt_info_offset:
 *
 *   Return the PLT info offset belonging to the plt entry called by CODE.
 */
guint32
mono_aot_get_plt_info_offset (gssize *regs, guint8 *code)
{
	guint8 *plt_entry = mono_aot_get_plt_entry (code);

	g_assert (plt_entry);

	/* The offset is embedded inside the code after the plt entry */
#if defined(__i386__)
	return *(guint32*)(plt_entry + 5);
#elif defined(__x86_64__)
	return *(guint32*)(plt_entry + 6);
#elif defined(__arm__)
	/* The offset is stored as the 5th word of the plt entry */
	return ((guint32*)plt_entry) [4];
#else
	g_assert_not_reached ();
	return 0;
#endif
}

/*
 * load_function:
 *
 *   Load the function named NAME from the aot image. 
 */
static gpointer
load_function (MonoAotModule *amodule, const char *name)
{
	char *symbol;
	guint8 *p;
	int n_patches, got_index, pindex;
	MonoMemPool *mp;
	gpointer code;

	/* Load the code */

	symbol = g_strdup_printf ("%s", name);
	find_symbol (amodule->sofile, amodule->globals, symbol, (gpointer *)&code);
	g_free (symbol);
	if (!code)
		g_error ("Symbol '%s' not found in AOT file '%s'.\n", name, amodule->aot_name);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT FOUND function '%s' in AOT file '%s'.\n", name, amodule->aot_name);

	/* Load info */

	symbol = g_strdup_printf ("%s_p", name);
	find_symbol (amodule->sofile, amodule->globals, symbol, (gpointer *)&p);
	g_free (symbol);
	if (!p)
		/* Nothing to patch */
		return code;

	/* Similar to mono_aot_load_method () */

	n_patches = decode_value (p, &p);

	if (n_patches) {
		MonoJumpInfo *patches;
		guint32 *got_slots;

		mp = mono_mempool_new ();

		got_index = decode_value (p, &p);

		patches = load_patch_info (amodule, mp, n_patches, got_index, &got_slots, p, &p);
		g_assert (patches);

		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];
			gpointer target;

			if (amodule->got [got_slots [pindex]])
				continue;

			/*
			 * When this code is executed, the runtime may not be initalized yet, so
			 * resolve the patch info by hand.
			 */
			if (ji->type == MONO_PATCH_INFO_JIT_ICALL_ADDR) {
				if (!strcmp (ji->data.name, "mono_get_lmf_addr")) {
					target = mono_get_lmf_addr;
				} else if (!strcmp (ji->data.name, "mono_thread_force_interruption_checkpoint")) {
					target = mono_thread_force_interruption_checkpoint;
				} else if (!strcmp (ji->data.name, "mono_exception_from_token")) {
					target = mono_exception_from_token;
				} else if (!strcmp (ji->data.name, "mono_throw_exception")) {
					target = mono_get_throw_exception ();
#ifdef __x86_64__
				} else if (!strcmp (ji->data.name, "mono_amd64_throw_exception")) {
					target = mono_amd64_throw_exception;
#endif
#ifdef __x86_64__
				} else if (!strcmp (ji->data.name, "mono_amd64_get_original_ip")) {
					target = mono_amd64_get_original_ip;
#endif
#ifdef __arm__
				} else if (!strcmp (ji->data.name, "mono_arm_throw_exception")) {
					target = mono_arm_throw_exception;
				} else if (!strcmp (ji->data.name, "mono_arm_throw_exception_by_token")) {
					target = mono_arm_throw_exception_by_token;
#endif
				} else if (strstr (ji->data.name, "trampoline_func_") == ji->data.name) {
					int tramp_type2 = atoi (ji->data.name + strlen ("trampoline_func_"));
					target = (gpointer)mono_get_trampoline_func (tramp_type2);
				} else if (strstr (ji->data.name, "specific_trampoline_lazy_fetch_") == ji->data.name) {
					/* atoll is needed because the the offset is unsigned */
					guint32 slot;
					int res;

					res = sscanf (ji->data.name, "specific_trampoline_lazy_fetch_%u", &slot);
					g_assert (res == 1);
					target = mono_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);
				} else if (!strcmp (ji->data.name, "specific_trampoline_monitor_enter")) {
					target = mono_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_ENTER, mono_get_root_domain (), NULL);
				} else if (!strcmp (ji->data.name, "specific_trampoline_monitor_exit")) {
					target = mono_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_EXIT, mono_get_root_domain (), NULL);
				} else if (!strcmp (ji->data.name, "specific_trampoline_generic_class_init")) {
					target = mono_create_specific_trampoline (NULL, MONO_TRAMPOLINE_GENERIC_CLASS_INIT, mono_get_root_domain (), NULL);
				} else if (!strcmp (ji->data.name, "mono_thread_get_and_clear_pending_exception")) {
					target = mono_thread_get_and_clear_pending_exception;
				} else {
					fprintf (stderr, "Unknown relocation '%s'\n", ji->data.name);
					g_assert_not_reached ();
					target = NULL;
				}
			} else {
				/* Hopefully the code doesn't have patches which need method or 
				 * domain to be set.
				 */
				target = mono_resolve_patch_target (NULL, NULL, code, ji, FALSE);
				g_assert (target);
			}

			amodule->got [got_slots [pindex]] = target;
		}

		g_free (got_slots);

		mono_mempool_destroy (mp);
	}

	return code;
}

/*
 * Return the piece of code identified by NAME from the mscorlib AOT file.
 */
gpointer
mono_aot_get_named_code (const char *name)
{
	MonoImage *image;
	MonoAotModule *amodule;

	image = mono_defaults.corlib;
	g_assert (image);

	amodule = image->aot_module;
	g_assert (amodule);

	return load_function (amodule, name);
}

/* Return a given kind of trampoline */
static gpointer
get_numerous_trampoline (MonoAotTrampoline tramp_type, int n_got_slots, MonoAotModule **out_amodule, guint32 *got_offset, guint32 *out_tramp_size)
{
	MonoAotModule *amodule;
	int index, tramp_size;
	MonoImage *image;

	/* Currently, we keep all trampolines in the mscorlib AOT image */
	image = mono_defaults.corlib;
	g_assert (image);

	mono_aot_lock ();

	amodule = image->aot_module;
	g_assert (amodule);

	*out_amodule = amodule;

	if (amodule->trampoline_index [tramp_type] == amodule->info.num_trampolines [tramp_type])
		g_error ("Ran out of trampolines of type %d in '%s' (%d)\n", tramp_type, image->name, amodule->info.num_trampolines [tramp_type]);

	index = amodule->trampoline_index [tramp_type] ++;

	mono_aot_unlock ();

	*got_offset = amodule->info.trampoline_got_offset_base [tramp_type] + (index * n_got_slots);

	tramp_size = amodule->info.trampoline_size [tramp_type];

	if (out_tramp_size)
		*out_tramp_size = tramp_size;

	return amodule->trampolines [tramp_type] + (index * tramp_size);
}

/*
 * Return a specific trampoline from the AOT file.
 */
gpointer
mono_aot_create_specific_trampoline (MonoImage *image, gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	MonoAotModule *amodule;
	guint32 got_offset, tramp_size;
	guint8 *code, *tramp;
	static gpointer generic_trampolines [MONO_TRAMPOLINE_NUM];
	static gboolean inited;
	static guint32 num_trampolines;

	if (!inited) {
		mono_aot_lock ();

		if (!inited) {
			mono_counters_register ("Specific trampolines", MONO_COUNTER_JIT | MONO_COUNTER_INT, &num_trampolines);
			inited = TRUE;
		}

		mono_aot_unlock ();
	}

	num_trampolines ++;

	if (!generic_trampolines [tramp_type]) {
		char *symbol;

		symbol = g_strdup_printf ("generic_trampoline_%d", tramp_type);
		generic_trampolines [tramp_type] = mono_aot_get_named_code (symbol);
		g_free (symbol);
	}

	tramp = generic_trampolines [tramp_type];
	g_assert (tramp);

	code = get_numerous_trampoline (MONO_AOT_TRAMP_SPECIFIC, 2, &amodule, &got_offset, &tramp_size);

	amodule->got [got_offset] = tramp;
	amodule->got [got_offset + 1] = arg1;

	if (code_len)
		*code_len = tramp_size;

	return code;
}

gpointer
mono_aot_get_static_rgctx_trampoline (gpointer ctx, gpointer addr)
{
	MonoAotModule *amodule;
	guint8 *code;
	guint32 got_offset;

	code = get_numerous_trampoline (MONO_AOT_TRAMP_STATIC_RGCTX, 2, &amodule, &got_offset, NULL);

	amodule->got [got_offset] = ctx;
	amodule->got [got_offset + 1] = addr; 

	return code;
}

gpointer
mono_aot_get_unbox_trampoline (MonoMethod *method)
{
	guint32 method_index = mono_metadata_token_index (method->token) - 1;
	MonoAotModule *amodule;
	char *symbol;
	gpointer code;

	if (method->is_inflated) {
		guint32 index = find_extra_method (method, &amodule);

		g_assert (index != 0xffffff);
		
		symbol = g_strdup_printf ("ut_e_%d", index);
	} else {
		amodule = method->klass->image->aot_module;
		g_assert (amodule);

		symbol = g_strdup_printf ("ut_%d", method_index);
	}
	code = load_function (amodule, symbol);
	g_free (symbol);
	return code;
}

gpointer
mono_aot_get_lazy_fetch_trampoline (guint32 slot)
{
	char *symbol;
	gpointer code;

	symbol = g_strdup_printf ("rgctx_fetch_trampoline_%u", slot);
	code = load_function (mono_defaults.corlib->aot_module, symbol);
	g_free (symbol);
	return code;
}

gpointer
mono_aot_get_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	guint32 got_offset;
	gpointer code;
	gpointer *buf;
	int i;
	MonoAotModule *amodule;

	code = get_numerous_trampoline (MONO_AOT_TRAMP_IMT_THUNK, 1, &amodule, &got_offset, NULL);

	/* Save the entries into an array */
	buf = mono_domain_alloc (domain, (count + 1) * 2 * sizeof (gpointer));
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];		

		g_assert (item->key);
		/* FIXME: */
		g_assert (!item->has_target_code);

		buf [(i * 2)] = item->key;
		buf [(i * 2) + 1] = &(vtable->vtable [item->value.vtable_slot]);
	}
	buf [(count * 2)] = NULL;
	buf [(count * 2) + 1] = fail_tramp;
	
	amodule->got [got_offset] = buf;

	return code;
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

guint32
mono_aot_get_plt_info_offset (gssize *regs, guint8 *code)
{
	g_assert_not_reached ();

	return 0;
}

gpointer
mono_aot_create_specific_trampoline (MonoImage *image, gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_static_rgctx_trampoline (gpointer ctx, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_named_code (const char *name)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_unbox_trampoline (MonoMethod *method)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_lazy_fetch_trampoline (guint32 slot)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	g_assert_not_reached ();
	return NULL;
}	

guint8*
mono_aot_get_unwind_info (MonoJitInfo *ji, guint32 *unwind_info_len)
{
	g_assert_not_reached ();
	return NULL;
}

#endif
