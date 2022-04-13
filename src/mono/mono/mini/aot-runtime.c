/**
 * \file
 * mono Ahead of Time compiler
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc.
 * Copyright 2011 Xamarin, Inc.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

#if HOST_WIN32
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

#include <mono/metadata/abi-details.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/mono-endian.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-digest.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/mono-tls-inline.h>
#include <mono/utils/options.h>

#include "mini.h"
#include "seq-points.h"
#include "aot-compiler.h"
#include "aot-runtime.h"
#include "jit-icalls.h"
#include "mini-runtime.h"
#include <mono/jit/mono-private-unstable.h>
#include "llvmonly-runtime.h"

#include <mono/metadata/components.h>

#ifndef DISABLE_AOT

#ifdef MONO_ARCH_CODE_EXEC_ONLY
extern guint8* mono_aot_arch_get_plt_entry_exec_only (gpointer amodule_info, host_mgreg_t *regs, guint8 *code, guint8 *plt);
extern guint32 mono_arch_get_plt_info_offset_exec_only (gpointer amodule_info, guint8 *plt_entry, host_mgreg_t *regs, guint8 *code, MonoAotResolvePltInfoOffset resolver, gpointer amodule);
extern void mono_arch_patch_plt_entry_exec_only (gpointer amodule_info, guint8 *code, gpointer *got, host_mgreg_t *regs, guint8 *addr);
#endif

#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))

#define JIT_INFO_MAP_BUCKET_SIZE 32

typedef struct _JitInfoMap JitInfoMap;
struct _JitInfoMap {
	MonoJitInfo *jinfo;
	JitInfoMap *next;
	int method_index;
};

#define GOT_INITIALIZING 1
#define GOT_INITIALIZED  2

struct MonoAotModule {
	char *aot_name;
	/* Pointer to the Global Offset Table */
	gpointer *got;
	gpointer *llvm_got;
	gpointer *shared_got;
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
	int got_initialized;
	guint8 *mem_begin;
	guint8 *mem_end;
	guint8 *jit_code_start;
	guint8 *jit_code_end;
	guint8 *llvm_code_start;
	guint8 *llvm_code_end;
	guint8 *plt;
	guint8 *plt_end;
	guint8 *blob;
	gpointer weak_field_indexes;
	guint8 *method_flags_table;
	/* Maps method indexes to their code */
	/* Raw pointer on arm64e */
	gpointer *methods;
	/* Sorted array of method addresses */
	gpointer *sorted_methods;
	/* Method indexes for each method in sorted_methods */
	int *sorted_method_indexes;
	/* The length of the two tables above */
	int sorted_methods_len;
	guint32 *method_info_offsets;
	guint32 *ex_info_offsets;
	guint32 *class_info_offsets;
	guint32 *got_info_offsets;
	guint32 *llvm_got_info_offsets;
	guint32 *methods_loaded;
	guint16 *class_name_table;
	guint32 *extra_method_table;
	guint32 *extra_method_info_offsets;
	guint32 *unbox_trampolines;
	guint32 *unbox_trampolines_end;
	guint32 *unbox_trampoline_addresses;
	guint8 *unwind_info;
	/* Maps method index -> unbox tramp */
	gpointer *unbox_tramp_per_method;

	/* Points to the mono EH data created by LLVM */
	guint8 *mono_eh_frame;

	/* Points to the data tables if MONO_AOT_FILE_FLAG_SEPARATE_DATA is set */
	gpointer tables [MONO_AOT_TABLE_NUM];
	/* Points to the trampolines */
	guint8 *trampolines [MONO_AOT_TRAMP_NUM];
	/* The first unused trampoline of each kind */
	guint32 trampoline_index [MONO_AOT_TRAMP_NUM];

	gboolean use_page_trampolines;

	MonoAotFileInfo info;

	gpointer *globals;
	MonoDl *sofile;

	JitInfoMap **async_jit_info_table;
	mono_mutex_t mutex;
};

typedef struct {
	void *next;
	unsigned char *trampolines;
	unsigned char *trampolines_end;
} TrampolinePage;

static GHashTable *aot_modules;
#define mono_aot_lock() mono_os_mutex_lock (&aot_mutex)
#define mono_aot_unlock() mono_os_mutex_unlock (&aot_mutex)
static mono_mutex_t aot_mutex;

/*
 * Maps assembly names to the mono_aot_module_<NAME>_info symbols in the
 * AOT modules registered by mono_aot_register_module ().
 */
static GHashTable *static_aot_modules;
/*
 * Same as above, but tracks module that must be loaded before others are
 * This allows us to have a "container" module which contains resources for
 * other modules. Since it doesn't provide methods for a managed assembly,
 * and it needs to be fully loaded by the time the other code needs it, it
 * must be eagerly loaded before other modules.
 */
static char *container_assm_name;
static MonoAotModule *container_amodule;
static GHashTable *loaded_static_aot_modules;

/*
 * Maps MonoJitInfo* to the aot module they belong to, this can be different
 * from ji->method->klass->image's aot module for generic instances.
 */
static GHashTable *ji_to_amodule;

/* Maps method addresses to MonoAotMethodFlags */
static GHashTable *code_to_method_flags;

/* For debugging */
static gint32 mono_last_aot_method = -1;

static gboolean make_unreadable = FALSE;
static guint32 name_table_accesses = 0;
static guint32 n_pagefaults = 0;

/* Used to speed-up find_aot_module () */
static gsize aot_code_low_addr = (gssize)-1;
static gsize aot_code_high_addr = 0;

/* Stats */
static gint32 async_jit_info_size;

#ifdef TARGET_APPLE_MOBILE
#define USE_PAGE_TRAMPOLINES (mscorlib_aot_module->use_page_trampolines)
#else
#define USE_PAGE_TRAMPOLINES 0
#endif

#define mono_aot_page_lock() mono_os_mutex_lock (&aot_page_mutex)
#define mono_aot_page_unlock() mono_os_mutex_unlock (&aot_page_mutex)
static mono_mutex_t aot_page_mutex;

static MonoAotModule *mscorlib_aot_module;

/* Embedding API hooks to load the AOT data for AOT images compiled with MONO_AOT_FILE_FLAG_SEPARATE_DATA */
static MonoLoadAotDataFunc aot_data_load_func;
static MonoFreeAotDataFunc aot_data_free_func;
static gpointer aot_data_func_user_data;

static void
init_plt (MonoAotModule *info);

static void
compute_llvm_code_range (MonoAotModule *amodule, guint8 **code_start, guint8 **code_end);

static gboolean
init_method (MonoAotModule *amodule, gpointer info, guint32 method_index, MonoMethod *method, MonoClass *init_class, MonoError *error);

static MonoJumpInfo*
decode_patches (MonoAotModule *amodule, MonoMemPool *mp, int n_patches, gboolean llvm, guint32 *got_offsets);

static MonoMethodSignature*
decode_signature (MonoAotModule *module, guint8 *buf, guint8 **endbuf);

static void
load_container_amodule (MonoAssemblyLoadContext *alc);

static void
sort_methods (MonoAotModule *amodule);

static void
amodule_lock (MonoAotModule *amodule)
{
	mono_os_mutex_lock (&amodule->mutex);
}

static void
amodule_unlock (MonoAotModule *amodule)
{
	mono_os_mutex_unlock (&amodule->mutex);
}

/*
 * load_image:
 *
 *   Load one of the images referenced by AMODULE. Returns NULL if the image is not
 * found, and sets @error for what happened
 */
static MonoImage *
load_image (MonoAotModule *amodule, int index, MonoError *error)
{
	MonoAssembly *assembly = NULL;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssemblyLoadContext *alc = mono_alc_get_ambient ();

	g_assert (index < amodule->image_table_len);

	error_init (error);

	if (amodule->image_table [index])
		return amodule->image_table [index];
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: module %s wants to load image %d: %s", amodule->aot_name, index, amodule->image_names[index].name);
	if (amodule->out_of_date) {
		mono_error_set_bad_image_by_name (error, amodule->aot_name, "Image out of date: %s", amodule->aot_name);
		return NULL;
	}

	/*
	 * LoadFile allows loading more than one assembly with the same name.
	 * That means that just calling mono_assembly_load is unlikely to find
	 * the correct assembly (it'll just return the first one loaded).  But
	 * we shouldn't hardcode the full assembly filepath into the AOT image,
	 * so it's not obvious that we can call mono_assembly_open_predicate.
	 *
	 * In the JIT, an assembly opened with LoadFile is supposed to only
	 * refer to already-loaded assemblies (or to GAC & MONO_PATH)
	 * assemblies - so nothing new should be loading.  And for the
	 * LoadFile'd assembly itself, we can check if the name and guid of the
	 * current AOT module matches the wanted name and guid and just return
	 * the AOT module's assembly.
	 */
	if (!strcmp (amodule->assembly->image->guid, amodule->image_guids [index])) {
		assembly = amodule->assembly;
	} else if (mono_get_corlib () && !strcmp (mono_get_corlib ()->guid, amodule->image_guids [index])) {
		/* This might be called before corlib is added to the root domain */
		assembly = mono_get_corlib ()->assembly;
	} else {
		MonoAssemblyByNameRequest req;
		mono_assembly_request_prepare_byname (&req, alc);
		req.basedir = amodule->assembly->basedir;
		assembly = mono_assembly_request_byname (&amodule->image_names [index], &req, &status);
	}
	if (!assembly) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: module %s is unusable because dependency %s is not found.", amodule->aot_name, amodule->image_names [index].name);
		mono_error_set_bad_image_by_name (error, amodule->aot_name, "module '%s' is unusable because dependency %s is not found (error %d).\n", amodule->aot_name, amodule->image_names [index].name, status);
		amodule->out_of_date = TRUE;
		return NULL;
	}

	if (strcmp (assembly->image->guid, amodule->image_guids [index])) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: module %s is unusable (GUID of dependent assembly %s doesn't match (expected '%s', got '%s')).", amodule->aot_name, amodule->image_names [index].name, amodule->image_guids [index], assembly->image->guid);
		mono_error_set_bad_image_by_name (error, amodule->aot_name, "module '%s' is unusable (GUID of dependent assembly %s doesn't match (expected '%s', got '%s')).", amodule->aot_name, amodule->image_names [index].name, amodule->image_guids [index], assembly->image->guid);
		amodule->out_of_date = TRUE;
		return NULL;
	}

	amodule->image_table [index] = assembly->image;
	return assembly->image;
}

static gint32
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

/*
 * mono_aot_get_offset:
 *
 *   Decode an offset table emitted by emit_offset_table (), returning the INDEXth
 * entry.
 */
static guint32
mono_aot_get_offset (guint32 *table, int index)
{
	int i, group, ngroups, index_entry_size;
	int start_offset, offset, group_size;
	guint8 *data_start, *p;
	guint32 *index32 = NULL;
	guint16 *index16 = NULL;

	/* noffsets = table [0]; */
	group_size = table [1];
	ngroups = table [2];
	index_entry_size = table [3];
	group = index / group_size;

	if (index_entry_size == 2) {
		index16 = (guint16*)&table [4];
		data_start = (guint8*)&index16 [ngroups];
		p = data_start + index16 [group];
	} else {
		index32 = (guint32*)&table [4];
		data_start = (guint8*)&index32 [ngroups];
		p = data_start + index32 [group];
	}

	/* offset will contain the value of offsets [group * group_size] */
	offset = start_offset = decode_value (p, &p);
	for (i = group * group_size + 1; i <= index; ++i) {
		offset += decode_value (p, &p);
	}

	//printf ("Offset lookup: %d -> %d, start=%d, p=%d\n", index, offset, start_offset, table [3 + group]);

	return offset;
}

static MonoMethod*
decode_resolve_method_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error);

static MonoClass*
decode_klass_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error);

static MonoType*
decode_type (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error);

static MonoGenericInst*
decode_generic_inst (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	int type_argc, i;
	MonoType **type_argv;
	MonoGenericInst *inst;
	guint8 *p = buf;

	error_init (error);
	type_argc = decode_value (p, &p);
	type_argv = g_new0 (MonoType*, type_argc);

	for (i = 0; i < type_argc; ++i) {
		MonoClass *pclass = decode_klass_ref (module, p, &p, error);
		if (!pclass) {
			g_free (type_argv);
			return NULL;
		}
		type_argv [i] = m_class_get_byval_arg (pclass);
	}

	inst = mono_metadata_get_generic_inst (type_argc, type_argv);
	g_free (type_argv);

	*endbuf = p;

	return inst;
}

static gboolean
decode_generic_context (MonoAotModule *amodule, MonoGenericContext *ctx, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	guint8 *p = buf;
	guint8 *p2;
	guint32 offset, flags;

	/* Either the class_inst or method_inst offset */
	flags = decode_value (p, &p);

	if (flags & 1) {
		offset = decode_value (p, &p);
		p2 = amodule->blob + offset;
		ctx->class_inst = decode_generic_inst (amodule, p2, &p2, error);
		if (!ctx->class_inst)
			return FALSE;
	}
	if (flags & 2) {
		offset = decode_value (p, &p);
		p2 = amodule->blob + offset;
		ctx->method_inst = decode_generic_inst (amodule, p2, &p2, error);
		if (!ctx->method_inst)
			return FALSE;
	}

	*endbuf = p;
	return TRUE;
}

static MonoClass*
decode_klass_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	MonoImage *image;
	MonoClass *klass = NULL, *eklass;
	guint32 token, rank, idx;
	guint8 *p = buf;
	int reftype;

	error_init (error);
	reftype = decode_value (p, &p);
	if (reftype == 0) {
		*endbuf = p;
		mono_error_set_bad_image_by_name (error, module->aot_name, "Decoding a null class ref: %s", module->aot_name);
		return NULL;
	}

	switch (reftype) {
	case MONO_AOT_TYPEREF_TYPEDEF_INDEX:
		idx = decode_value (p, &p);
		image = load_image (module, 0, error);
		if (!image)
			return NULL;
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF + idx, error);
		break;
	case MONO_AOT_TYPEREF_TYPEDEF_INDEX_IMAGE:
		idx = decode_value (p, &p);
		image = load_image (module, decode_value (p, &p), error);
		if (!image)
			return NULL;
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF + idx, error);
		break;
	case MONO_AOT_TYPEREF_TYPESPEC_TOKEN:
		token = decode_value (p, &p);
		image = module->assembly->image;
		if (!image) {
			mono_error_set_bad_image_by_name (error, module->aot_name, "No image associated with the aot module: %s", module->aot_name);
			return NULL;
		}
		klass = mono_class_get_checked (image, token, error);
		break;
	case MONO_AOT_TYPEREF_GINST: {
		MonoClass *gclass;
		MonoGenericContext ctx;
		MonoType *type;

		gclass = decode_klass_ref (module, p, &p, error);
		if (!gclass)
			return NULL;
		g_assert (mono_class_is_gtd (gclass));

		memset (&ctx, 0, sizeof (ctx));
		guint32 offset = decode_value (p, &p);
		guint8 *p2 = module->blob + offset;
		ctx.class_inst = decode_generic_inst (module, p2, &p2, error);
		if (!ctx.class_inst)
			return NULL;
		type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (gclass), &ctx, error);
		if (!type)
			return NULL;
		klass = mono_class_from_mono_type_internal (type);
		mono_metadata_free_type (type);
		break;
	}
	case MONO_AOT_TYPEREF_VAR: {
		MonoType *t = NULL;
		MonoGenericContainer *container = NULL;
		gboolean has_constraint = decode_value (p, &p);

		if (has_constraint) {
			MonoClass *par_klass;
			MonoType *gshared_constraint;

			gshared_constraint = decode_type (module, p, &p, error);
			if (!gshared_constraint)
				return NULL;

			par_klass = decode_klass_ref (module, p, &p, error);
			if (!par_klass)
				return NULL;

			t = mini_get_shared_gparam (m_class_get_byval_arg (par_klass), gshared_constraint);
			mono_metadata_free_type (gshared_constraint);
			klass = mono_class_from_mono_type_internal (t);
		} else {
			int type = decode_value (p, &p);
			int num = decode_value (p, &p);
			gboolean is_not_anonymous = decode_value (p, &p);

			if (is_not_anonymous) {
				gboolean is_method = decode_value (p, &p);

				if (is_method) {
					MonoMethod *method_def;
					g_assert (type == MONO_TYPE_MVAR);
					method_def = decode_resolve_method_ref (module, p, &p, error);
					if (!method_def)
						return NULL;

					container = mono_method_get_generic_container (method_def);
				} else {
					MonoClass *class_def;
					g_assert (type == MONO_TYPE_VAR);
					class_def = decode_klass_ref (module, p, &p, error);
					if (!class_def)
						return NULL;

					container = mono_class_try_get_generic_container (class_def); //FIXME is this a case for a try_get?
				}
			} else {
				// We didn't decode is_method, so we have to infer it from type enum.
				container = mono_get_anonymous_container_for_image (module->assembly->image, type == MONO_TYPE_MVAR);
			}

			t = g_new0 (MonoType, 1);
			t->type = (MonoTypeEnum)type;
			if (is_not_anonymous) {
				t->data.generic_param = mono_generic_container_get_param (container, num);
			} else {
				/* Anonymous */
				MonoGenericParam *par = mono_metadata_create_anon_gparam (module->assembly->image, num, type == MONO_TYPE_MVAR);
				t->data.generic_param = par;
				// FIXME: maybe do this for all anon gparams?
				((MonoGenericParamFull*)par)->info.name = mono_make_generic_name_string (module->assembly->image, num);
			}
			// FIXME: Maybe use types directly to avoid
			// the overhead of creating MonoClass-es
			klass = mono_class_from_mono_type_internal (t);

			g_free (t);
		}
		break;
	}
	case MONO_AOT_TYPEREF_ARRAY:
		/* Array */
		rank = decode_value (p, &p);
		eklass = decode_klass_ref (module, p, &p, error);
		if (!eklass)
			return NULL;
		klass = mono_class_create_array (eklass, rank);
		break;
	case MONO_AOT_TYPEREF_PTR: {
		MonoType *t;

		t = decode_type (module, p, &p, error);
		if (!t)
			return NULL;
		klass = mono_class_from_mono_type_internal (t);
		g_free (t);
		break;
	}
	case MONO_AOT_TYPEREF_BLOB_INDEX: {
		guint32 offset = decode_value (p, &p);
		guint8 *p2;

		p2 = module->blob + offset;
		klass = decode_klass_ref (module, p2, &p2, error);
		break;
	}
	default:
		mono_error_set_bad_image_by_name (error, module->aot_name, "Invalid klass reftype %d: %s", reftype, module->aot_name);
	}
	//g_assert (klass);
	//printf ("BLA: %s\n", mono_type_full_name (m_class_get_byval_arg (klass)));
	*endbuf = p;
	return klass;
}

static MonoClassField*
decode_field_info (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	ERROR_DECL (error);
	MonoClass *klass = decode_klass_ref (module, buf, &buf, error);
	guint32 token;
	guint8 *p = buf;

	if (!klass) {
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		return NULL;
	}

	token = MONO_TOKEN_FIELD_DEF + decode_value (p, &p);

	*endbuf = p;

	return mono_class_get_field (klass, token);
}

/*
 * Parse a MonoType encoded by encode_type () in aot-compiler.c. Return malloc-ed
 * memory.
 */
static MonoType*
decode_type (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	guint8 *p = buf;
	MonoType *t;

	if (*p == MONO_TYPE_CMOD_REQD) {
		++p;

		int count = decode_value (p, &p);

		/* TODO: encode aggregate cmods differently than simple cmods and make it possible to use the more compact encoding here. */
		t = (MonoType*)g_malloc0 (mono_sizeof_type_with_mods (count, TRUE));
		mono_type_with_mods_init (t, count, TRUE);

		/* Try not to blow up the stack. See comment on MONO_MAX_EXPECTED_CMODS */
		g_assert (count < MONO_MAX_EXPECTED_CMODS);
		MonoAggregateModContainer *cm = g_alloca (mono_sizeof_aggregate_modifiers (count));
		cm->count = count;
		for (int i = 0; i < count; ++i) {
			MonoSingleCustomMod *cmod = &cm->modifiers [i];
			cmod->required = decode_value (p, &p);
			cmod->type = decode_type (module, p, &p, error);
			goto_if_nok (error, fail);
		}

		mono_type_set_amods (t, mono_metadata_get_canonical_aggregate_modifiers (cm));
		for (int i = 0; i < count; ++i)
			mono_metadata_free_type (cm->modifiers [i].type);
	} else {
		t = (MonoType *) g_malloc0 (MONO_SIZEOF_TYPE);
	}

	while (TRUE) {
		if (*p == MONO_TYPE_PINNED) {
			t->pinned = TRUE;
			++p;
		} else if (*p == MONO_TYPE_BYREF) {
			t->byref__ = TRUE;
			++p;
		} else {
			break;
		}
	}

	t->type = (MonoTypeEnum)*p;
	++p;

	switch (t->type) {
	case MONO_TYPE_VOID:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		break;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		t->data.klass = decode_klass_ref (module, p, &p, error);
		if (!t->data.klass)
			goto fail;
		break;
	case MONO_TYPE_SZARRAY:
		t->data.klass = decode_klass_ref (module, p, &p, error);

		if (!t->data.klass)
			goto fail;
		break;
	case MONO_TYPE_PTR:
		t->data.type = decode_type (module, p, &p, error);
		if (!t->data.type)
			goto fail;
		break;
	case MONO_TYPE_FNPTR:
		t->data.method = decode_signature (module, p, &p);
		if (!t->data.method)
			goto fail;
		break;
	case MONO_TYPE_GENERICINST: {
		MonoClass *gclass;
		MonoGenericContext ctx;
		MonoType *type;
		MonoClass *klass;

		gclass = decode_klass_ref (module, p, &p, error);
		if (!gclass)
			goto fail;
		g_assert (mono_class_is_gtd (gclass));

		memset (&ctx, 0, sizeof (ctx));
		ctx.class_inst = decode_generic_inst (module, p, &p, error);
		if (!ctx.class_inst)
			goto fail;
		type = mono_class_inflate_generic_type_checked (m_class_get_byval_arg (gclass), &ctx, error);
		if (!type)
			goto fail;
		klass = mono_class_from_mono_type_internal (type);
		t->data.generic_class = mono_class_get_generic_class (klass);
		break;
	}
	case MONO_TYPE_ARRAY: {
		MonoArrayType *array;
		int i;

		// FIXME: memory management
		array = g_new0 (MonoArrayType, 1);
		array->eklass = decode_klass_ref (module, p, &p, error);
		if (!array->eklass)
			goto fail;
		array->rank = decode_value (p, &p);
		array->numsizes = decode_value (p, &p);

		if (array->numsizes)
			array->sizes = (int *)g_malloc0 (sizeof (int) * array->numsizes);
		for (i = 0; i < array->numsizes; ++i)
			array->sizes [i] = decode_value (p, &p);

		array->numlobounds = decode_value (p, &p);
		if (array->numlobounds)
			array->lobounds = (int *)g_malloc0 (sizeof (int) * array->numlobounds);
		for (i = 0; i < array->numlobounds; ++i)
			array->lobounds [i] = decode_value (p, &p);
		t->data.array = array;
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: {
		MonoClass *klass = decode_klass_ref (module, p, &p, error);
		if (!klass)
			goto fail;
		t->data.generic_param = m_class_get_byval_arg (klass)->data.generic_param;
		break;
	}
	default:
		mono_error_set_bad_image_by_name (error, module->aot_name, "Invalid encoded type %d: %s", t->type, module->aot_name);
		goto fail;
	}

	*endbuf = p;

	return t;
fail:
	g_free (t);
	return NULL;
}

// FIXME: Error handling, memory management

static MonoMethodSignature*
decode_signature_with_target (MonoAotModule *module, MonoMethodSignature *target, guint8 *buf, guint8 **endbuf)
{
	ERROR_DECL (error);
	MonoMethodSignature *sig;
	guint32 flags;
	int i, gen_param_count = 0, param_count, call_conv;
	guint8 *p = buf;
	gboolean hasthis, explicit_this, has_gen_params, pinvoke;

	flags = *p;
	p ++;
	has_gen_params = (flags & 0x10) != 0;
	hasthis = (flags & 0x20) != 0;
	explicit_this = (flags & 0x40) != 0;
	pinvoke = (flags & 0x80) != 0;
	call_conv = flags & 0x0F;

	if (has_gen_params)
		gen_param_count = decode_value (p, &p);
	param_count = decode_value (p, &p);
	if (target && param_count != target->param_count)
		return NULL;
	sig = (MonoMethodSignature *)g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + param_count * sizeof (MonoType *));
	sig->param_count = param_count;
	sig->sentinelpos = -1;
	sig->hasthis = hasthis;
	sig->explicit_this = explicit_this;
	sig->pinvoke = pinvoke;
	sig->call_convention = call_conv;
	sig->generic_param_count = gen_param_count;
	sig->ret = decode_type (module, p, &p, error);
	if (!sig->ret)
		goto fail;
	for (i = 0; i < param_count; ++i) {
		if (*p == MONO_TYPE_SENTINEL) {
			g_assert (sig->call_convention == MONO_CALL_VARARG);
			sig->sentinelpos = i;
			p ++;
		}
		sig->params [i] = decode_type (module, p, &p, error);
		if (!sig->params [i])
			goto fail;
	}

	if (sig->call_convention == MONO_CALL_VARARG && sig->sentinelpos == -1)
		sig->sentinelpos = sig->param_count;

	*endbuf = p;

	return sig;
fail:
	mono_error_cleanup (error); /* FIXME don't swallow the error */
	g_free (sig);
	return NULL;
}

static MonoMethodSignature*
decode_signature (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	return decode_signature_with_target (module, NULL, buf, endbuf);
}

static gboolean
sig_matches_target (MonoAotModule *module, MonoMethod *target, guint8 *buf, guint8 **endbuf)
{
	MonoMethodSignature *sig;
	gboolean res;
	guint8 *p = buf;

	sig = decode_signature_with_target (module, mono_method_signature_internal (target), p, &p);
	res = sig && mono_metadata_signature_equal (mono_method_signature_internal (target), sig);
	g_free (sig);
	*endbuf = p;
	return res;
}

/* Stores information returned by decode_method_ref () */
typedef struct {
	MonoImage *image;
	guint32 token;
	MonoMethod *method;
	gboolean no_aot_trampoline;
} MethodRef;

/*
 * decode_method_ref_with_target:
 *
 *   Decode a method reference, storing the image/token into a MethodRef structure.
 * This avoids loading metadata for the method if the caller does not need it. If the method has
 * no token, then it is loaded from metadata and ref->method is set to the method instance.
 * If TARGET is non-NULL, abort decoding if it can be determined that the decoded method
 *  couldn't resolve to TARGET, and return FALSE.
 * There are some kinds of method references which only support a non-null TARGET.
 * This means that its not possible to decode this into a method, only to check
 * that the method reference matches a given method. This is normally not a problem
 * as these wrappers only occur in the extra_methods table, where we already have
 * a method we want to lookup.
 *
 * If there was a decoding error, we return FALSE and set @error
 */
static gboolean
decode_method_ref_with_target (MonoAotModule *module, MethodRef *ref, MonoMethod *target, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	guint32 image_index, value;
	MonoImage *image = NULL;
	guint8 *p = buf;

	memset (ref, 0, sizeof (MethodRef));
	error_init (error);

	value = decode_value (p, &p);
	image_index = value >> 24;

	if (image_index == MONO_AOT_METHODREF_NO_AOT_TRAMPOLINE) {
		ref->no_aot_trampoline = TRUE;
		value = decode_value (p, &p);
		image_index = value >> 24;
	}

	if (image_index < MONO_AOT_METHODREF_MIN || image_index == MONO_AOT_METHODREF_METHODSPEC ||
		image_index == MONO_AOT_METHODREF_GINST || image_index == MONO_AOT_METHODREF_BLOB_INDEX) {
		if (target && target->wrapper_type) {
			return FALSE;
		}
	}

	if (image_index == MONO_AOT_METHODREF_WRAPPER) {
		WrapperInfo *info;
		guint32 wrapper_type;

		wrapper_type = decode_value (p, &p);

		if (target && target->wrapper_type != wrapper_type)
			return FALSE;

		/* Doesn't matter */
		image = mono_defaults.corlib;

		switch (wrapper_type) {
		case MONO_WRAPPER_ALLOC: {
			int atype = decode_value (p, &p);
			ManagedAllocatorVariant variant =
				mono_profiler_allocations_enabled () ?
				MANAGED_ALLOCATOR_PROFILER : MANAGED_ALLOCATOR_REGULAR;

			ref->method = mono_gc_get_managed_allocator_by_type (atype, variant);
			/* Try to fallback to the slow path version */
			if (!ref->method)
				ref->method = mono_gc_get_managed_allocator_by_type (atype, MANAGED_ALLOCATOR_SLOW_PATH);
			if (!ref->method) {
				mono_error_set_bad_image_by_name (error, module->aot_name, "Error: No managed allocator, but we need one for AOT.\nAre you using non-standard GC options?\n%s\n", module->aot_name);
				return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_WRITE_BARRIER: {
			ref->method = mono_gc_get_write_barrier ();
			break;
		}
		case MONO_WRAPPER_STELEMREF: {
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_NONE) {
				ref->method = mono_marshal_get_stelemref ();
			} else if (subtype == WRAPPER_SUBTYPE_VIRTUAL_STELEMREF) {
				int kind;

				kind = decode_value (p, &p);

				ref->method = mono_marshal_get_virtual_stelemref_wrapper ((MonoStelemrefKind)kind);
			} else {
				mono_error_set_bad_image_by_name (error, module->aot_name, "Invalid STELEMREF subtype %d: %s", subtype, module->aot_name);
				return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_SYNCHRONIZED: {
			MonoMethod *m = decode_resolve_method_ref (module, p, &p, error);
			if (!m)
				return FALSE;
			ref->method = mono_marshal_get_synchronized_wrapper (m);
			break;
		}
		case MONO_WRAPPER_OTHER: {
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_PTR_TO_STRUCTURE || subtype == WRAPPER_SUBTYPE_STRUCTURE_TO_PTR) {
				MonoClass *klass = decode_klass_ref (module, p, &p, error);
				if (!klass)
					return FALSE;

				if (!target)
					return FALSE;
				if (klass != target->klass)
					return FALSE;

				if (subtype == WRAPPER_SUBTYPE_PTR_TO_STRUCTURE) {
					if (strcmp (target->name, "PtrToStructure"))
						return FALSE;
					ref->method = mono_marshal_get_ptr_to_struct (klass);
				} else {
					if (strcmp (target->name, "StructureToPtr"))
						return FALSE;
					ref->method = mono_marshal_get_struct_to_ptr (klass);
				}
			} else if (subtype == WRAPPER_SUBTYPE_SYNCHRONIZED_INNER) {
				MonoMethod *m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_synchronized_inner_wrapper (m);
			} else if (subtype == WRAPPER_SUBTYPE_ARRAY_ACCESSOR) {
				MonoMethod *m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_array_accessor_wrapper (m);
			} else if (subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN) {
				ref->method = mono_marshal_get_gsharedvt_in_wrapper ();
			} else if (subtype == WRAPPER_SUBTYPE_GSHAREDVT_OUT) {
				ref->method = mono_marshal_get_gsharedvt_out_wrapper ();
			} else if (subtype == WRAPPER_SUBTYPE_INTERP_IN) {
				MonoMethodSignature *sig = decode_signature (module, p, &p);
				if (!sig)
					return FALSE;
				ref->method = mini_get_interp_in_wrapper (sig);
				g_free (sig);
			} else if (subtype == WRAPPER_SUBTYPE_INTERP_LMF) {
				MonoJitICallInfo *icall_info = mono_find_jit_icall_info ((MonoJitICallId)decode_value (p, &p));
				ref->method = mini_get_interp_lmf_wrapper (icall_info->name, (gpointer) icall_info->func);
			} else if (subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG) {
				MonoMethodSignature *sig = decode_signature (module, p, &p);
				if (!sig)
					return FALSE;
				ref->method = mini_get_gsharedvt_in_sig_wrapper (sig);
				g_free (sig);
			} else if (subtype == WRAPPER_SUBTYPE_GSHAREDVT_OUT_SIG) {
				MonoMethodSignature *sig = decode_signature (module, p, &p);
				if (!sig)
					return FALSE;
				ref->method = mini_get_gsharedvt_out_sig_wrapper (sig);
				g_free (sig);
			} else if (subtype == WRAPPER_SUBTYPE_AOT_INIT) {
				guint32 init_type = decode_value (p, &p);
				ref->method = mono_marshal_get_aot_init_wrapper ((MonoAotInitSubtype) init_type);
			} else if (subtype == WRAPPER_SUBTYPE_LLVM_FUNC) {
				guint32 init_type = decode_value (p, &p);
				ref->method = mono_marshal_get_llvm_func_wrapper ((MonoLLVMFuncWrapperSubtype) init_type);
			} else {
				mono_error_set_bad_image_by_name (error, module->aot_name, "Invalid UNKNOWN wrapper subtype %d: %s", subtype, module->aot_name);
				return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_MANAGED_TO_MANAGED: {
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_ELEMENT_ADDR) {
				int rank = decode_value (p, &p);
				int elem_size = decode_value (p, &p);

				ref->method = mono_marshal_get_array_address (rank, elem_size);
			} else if (subtype == WRAPPER_SUBTYPE_STRING_CTOR) {
				MonoMethod *m;

				m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;

				if (!target)
					return FALSE;
				g_assert (target->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED);

				info = mono_marshal_get_wrapper_info (target);
				if (info && info->subtype == subtype && info->d.string_ctor.method == m)
					ref->method = target;
				else
					return FALSE;
			} else if (subtype == WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER) {
				MonoClass *klass = decode_klass_ref (module, p, &p, error);
				if (!klass)
					return FALSE;
				MonoMethod *m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;
				int name_idx = decode_value (p, &p);
				const char *name = (const char*)module->blob + name_idx;
				ref->method = mono_marshal_get_generic_array_helper (klass, name, m);
			}
			break;
		}
		case MONO_WRAPPER_MANAGED_TO_NATIVE: {
			MonoMethod *m;
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER) {
				MonoJitICallInfo *icall_info = mono_find_jit_icall_info ((MonoJitICallId)decode_value (p, &p));
				ref->method = mono_icall_get_wrapper_method (icall_info);
			} else if (subtype == WRAPPER_SUBTYPE_NATIVE_FUNC_INDIRECT) {
				MonoClass *klass = decode_klass_ref (module, p, &p, error);
				if (!klass)
					return FALSE;
				MonoMethodSignature *sig = decode_signature (module, p, &p);
				if (!sig)
					return FALSE;
				ref->method = mono_marshal_get_native_func_wrapper_indirect (klass, sig, TRUE);
			} else {
				m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;

				/* This should only happen when looking for an extra method */
				if (!target)
					return FALSE;
				if (mono_marshal_method_from_wrapper (target) == m)
					ref->method = target;
				else
					return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_CASTCLASS: {
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_CASTCLASS_WITH_CACHE)
				ref->method = mono_marshal_get_castclass_with_cache ();
			else if (subtype == WRAPPER_SUBTYPE_ISINST_WITH_CACHE)
				ref->method = mono_marshal_get_isinst_with_cache ();
			else {
				mono_error_set_bad_image_by_name (error, module->aot_name, "Invalid CASTCLASS wrapper subtype %d: %s", subtype, module->aot_name);
				return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_RUNTIME_INVOKE: {
			int subtype = decode_value (p, &p);

			if (!target)
				return FALSE;

			if (subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_DYNAMIC) {
				if (strcmp (target->name, "runtime_invoke_dynamic") != 0)
					return FALSE;
				ref->method = target;
			} else if (subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT) {
				/* Direct wrapper */
				MonoMethod *m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_runtime_invoke (m, FALSE);
			} else if (subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL) {
				/* Virtual direct wrapper */
				MonoMethod *m = decode_resolve_method_ref (module, p, &p, error);
				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_runtime_invoke (m, TRUE);
			} else {
				MonoMethodSignature *sig;

				sig = decode_signature_with_target (module, NULL, p, &p);
				info = mono_marshal_get_wrapper_info (target);
				g_assert (info);

				if (info->subtype != subtype) {
					g_free (sig);
					return FALSE;
				}
				g_assert (info->d.runtime_invoke.sig);
				const gboolean same_sig = mono_metadata_signature_equal (sig, info->d.runtime_invoke.sig);
				g_free (sig);
				if (same_sig)
					ref->method = target;
				else
					return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_DELEGATE_INVOKE:
		case MONO_WRAPPER_DELEGATE_BEGIN_INVOKE:
		case MONO_WRAPPER_DELEGATE_END_INVOKE: {
			gboolean is_inflated = decode_value (p, &p);
			WrapperSubtype subtype;

			if (is_inflated) {
				MonoClass *klass;
				MonoMethod *invoke, *wrapper;

				klass = decode_klass_ref (module, p, &p, error);
				if (!klass)
					return FALSE;

				switch (wrapper_type) {
				case MONO_WRAPPER_DELEGATE_INVOKE:
					invoke = mono_get_delegate_invoke_internal (klass);
					wrapper = mono_marshal_get_delegate_invoke (invoke, NULL);
					break;
				case MONO_WRAPPER_DELEGATE_BEGIN_INVOKE:
					invoke = mono_get_delegate_begin_invoke_internal (klass);
					wrapper = mono_marshal_get_delegate_begin_invoke (invoke);
					break;
				case MONO_WRAPPER_DELEGATE_END_INVOKE:
					invoke = mono_get_delegate_end_invoke_internal (klass);
					wrapper = mono_marshal_get_delegate_end_invoke (invoke);
					break;
				default:
					g_assert_not_reached ();
					break;
				}
				if (target) {
					/*
					 * Due to the way mini_get_shared_method_full () works, we could end up with
					 * multiple copies of the same wrapper.
					 */
					if (wrapper->klass != target->klass)
						return FALSE;
					ref->method = target;
				} else {
					ref->method = wrapper;
				}
			} else {
				/*
				 * These wrappers are associated with a signature, not with a method.
				 * Since we can't decode them into methods, they need a target method.
				 */
				if (!target)
					return FALSE;

				if (wrapper_type == MONO_WRAPPER_DELEGATE_INVOKE) {
					subtype = (WrapperSubtype)decode_value (p, &p);
					info = mono_marshal_get_wrapper_info (target);
					if (info) {
						if (info->subtype != subtype)
							return FALSE;
					} else {
						if (subtype != WRAPPER_SUBTYPE_NONE)
							return FALSE;
					}
				}
				if (sig_matches_target (module, target, p, &p))
					ref->method = target;
				else
					return FALSE;
			}
			break;
		}
		case MONO_WRAPPER_NATIVE_TO_MANAGED: {
			MonoMethod *m;
			MonoClass *klass;

			m = decode_resolve_method_ref (module, p, &p, error);
			if (!m)
				return FALSE;
			gboolean has_class = decode_value (p, &p);
			if (has_class) {
				klass = decode_klass_ref (module, p, &p, error);
				if (!klass)
					return FALSE;
			} else
				klass = NULL;
			ref->method = mono_marshal_get_managed_wrapper (m, klass, 0, error);
			if (!is_ok (error))
				return FALSE;
			break;
		}
		default:
			g_assert_not_reached ();
		}
	} else if (image_index == MONO_AOT_METHODREF_METHODSPEC) {
		image_index = decode_value (p, &p);
		ref->token = decode_value (p, &p);

		image = load_image (module, image_index, error);
		if (!image)
			return FALSE;
	} else if (image_index == MONO_AOT_METHODREF_BLOB_INDEX) {
		guint32 offset = decode_value (p, &p);

		guint8 *p2;

		p2 = module->blob + offset;
		if (!decode_method_ref_with_target (module, ref, target, p2, &p2, error))
			return FALSE;
		image = ref->image;
		if (!image)
			return FALSE;
	} else if (image_index == MONO_AOT_METHODREF_GINST) {
		MonoClass *klass;
		MonoGenericContext ctx;
		guint32 token_index;

		/*
		 * These methods do not have a token which resolves them, so we
		 * resolve them immediately.
		 */
		klass = decode_klass_ref (module, p, &p, error);
		if (!klass)
			return FALSE;

		if (target && target->klass != klass)
			return FALSE;

		image_index = decode_value (p, &p);
		token_index = decode_value (p, &p);
		ref->token = mono_metadata_make_token (MONO_TABLE_METHOD, token_index);

		image = load_image (module, image_index, error);
		if (!image)
			return FALSE;

		ref->method = mono_get_method_checked (image, ref->token, NULL, NULL, error);
		if (!ref->method)
			return FALSE;

		memset (&ctx, 0, sizeof (ctx));

MONO_DISABLE_WARNING(4127) /* conditional expression is constant */
		if (FALSE && mono_class_is_ginst (klass)) {
			ctx.class_inst = mono_class_get_generic_class (klass)->context.class_inst;
			ctx.method_inst = NULL;

			ref->method = mono_class_inflate_generic_method_full_checked (ref->method, klass, &ctx, error);
			if (!ref->method)
				return FALSE;
		}
MONO_RESTORE_WARNING

		memset (&ctx, 0, sizeof (ctx));

		if (!decode_generic_context (module, &ctx, p, &p, error))
			return FALSE;

		ref->method = mono_class_inflate_generic_method_full_checked (ref->method, klass, &ctx, error);
		if (!ref->method)
			return FALSE;

	} else if (image_index == MONO_AOT_METHODREF_ARRAY) {
		MonoClass *klass;
		int method_type;

		klass = decode_klass_ref (module, p, &p, error);
		if (!klass)
			return FALSE;
		method_type = decode_value (p, &p);
		switch (method_type) {
		case 0:
			ref->method = mono_class_get_method_from_name_checked (klass, ".ctor", m_class_get_rank (klass), 0, error);
			return_val_if_nok (error, FALSE);
			break;
		case 1:
			ref->method = mono_class_get_method_from_name_checked (klass, ".ctor", m_class_get_rank (klass) * 2, 0, error);
			return_val_if_nok (error, FALSE);
			break;
		case 2:
			ref->method = mono_class_get_method_from_name_checked (klass, "Get", -1, 0, error);
			return_val_if_nok (error, FALSE);
			break;
		case 3:
			ref->method = mono_class_get_method_from_name_checked (klass, "Address", -1, 0, error);
			return_val_if_nok (error, FALSE);
			break;
		case 4:
			ref->method = mono_class_get_method_from_name_checked (klass, "Set", -1, 0, error);
			return_val_if_nok (error, FALSE);
			break;
		default:
			mono_error_set_bad_image_by_name (error, module->aot_name, "Invalid METHODREF_ARRAY method type %d: %s", method_type, module->aot_name);
			return FALSE;
		}
	} else {
		if (image_index == MONO_AOT_METHODREF_LARGE_IMAGE_INDEX) {
			image_index = decode_value (p, &p);
			value = decode_value (p, &p);
		}

		ref->token = MONO_TOKEN_METHOD_DEF | (value & 0xffffff);

		image = load_image (module, image_index, error);
		if (!image)
			return FALSE;
	}

	*endbuf = p;

	ref->image = image;

	return TRUE;
}

static gboolean
decode_method_ref (MonoAotModule *module, MethodRef *ref, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	return decode_method_ref_with_target (module, ref, NULL, buf, endbuf, error);
}

/*
 * decode_resolve_method_ref_with_target:
 *
 *   Similar to decode_method_ref, but resolve and return the method itself.
 */
static MonoMethod*
decode_resolve_method_ref_with_target (MonoAotModule *module, MonoMethod *target, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	MethodRef ref;

	error_init (error);

	if (!decode_method_ref_with_target (module, &ref, target, buf, endbuf, error))
		return NULL;
	if (ref.method)
		return ref.method;
	if (!ref.image) {
		mono_error_set_bad_image_by_name (error, module->aot_name, "No image found for methodref with target: %s", module->aot_name);
		return NULL;
	}
	return mono_get_method_checked (ref.image, ref.token, NULL, NULL, error);
}

static MonoMethod*
decode_resolve_method_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf, MonoError *error)
{
	return decode_resolve_method_ref_with_target (module, NULL, buf, endbuf, error);
}

static void
find_symbol (MonoDl *module, gpointer *globals, const char *name, gpointer *value)
{
	if (globals) {
		int global_index;
		guint16 *table, *entry;
		guint16 table_size;
		guint32 hash;
		char *symbol = (char*)name;

#ifdef TARGET_MACH
		symbol = g_strdup_printf ("_%s", name);
#endif

		/* The first entry points to the hash */
		table = (guint16 *)globals [0];
		globals ++;

		table_size = table [0];
		table ++;

		hash = mono_metadata_str_hash (symbol) % table_size;

		entry = &table [hash * 2];

		/* Search the hash for the index into the globals table */
		global_index = -1;
		while (entry [0] != 0) {
			guint32 index = entry [0] - 1;
			guint32 next = entry [1];

			//printf ("X: %s %s\n", (char*)globals [index * 2], name);

			if (!strcmp ((const char*)globals [index * 2], symbol)) {
				global_index = index;
				break;
			}

			if (next != 0) {
				entry = &table [next * 2];
			} else {
				break;
			}
		}

		if (global_index != -1)
			*value = globals [global_index * 2 + 1];
		else
			*value = NULL;

		if (symbol != name)
			g_free (symbol);
	} else {
		ERROR_DECL (symbol_error);
		*value = mono_dl_symbol (module, name, symbol_error);
		mono_error_cleanup (symbol_error);
	}
}

static void
find_amodule_symbol (MonoAotModule *amodule, const char *name, gpointer *value)
{
	g_assert (!(amodule->info.flags & MONO_AOT_FILE_FLAG_LLVM_ONLY));

	find_symbol (amodule->sofile, amodule->globals, name, value);
}

void
mono_install_load_aot_data_hook (MonoLoadAotDataFunc load_func, MonoFreeAotDataFunc free_func, gpointer user_data)
{
	aot_data_load_func = load_func;
	aot_data_free_func = free_func;
	aot_data_func_user_data = user_data;
}

/* Load the separate aot data file for ASSEMBLY */
static guint8*
open_aot_data (MonoAssembly *assembly, MonoAotFileInfo *info, void **ret_handle)
{
	MonoFileMap *map;
	char *filename;
	guint8 *data;

	if (aot_data_load_func) {
		data = aot_data_load_func (assembly, info->datafile_size, aot_data_func_user_data, ret_handle);
		g_assert (data);
		return data;
	}

	/*
	 * Use <assembly name>.aotdata as the default implementation if no callback is given
	 */
	filename = g_strdup_printf ("%s.aotdata", assembly->image->name);
	map = mono_file_map_open (filename);
	g_assert (map);
	data = (guint8*)mono_file_map (info->datafile_size, MONO_MMAP_READ, mono_file_map_fd (map), 0, ret_handle);
	g_assert (data);

	return data;
}

static gboolean
check_usable (MonoAssembly *assembly, MonoAotFileInfo *info, guint8 *blob, char **out_msg)
{
	char *build_info;
	char *msg = NULL;
	gboolean usable = TRUE;
	gboolean full_aot, interp, safepoints;
	guint32 excluded_cpu_optimizations;

	if (strcmp (assembly->image->guid, (const char*)info->assembly_guid)) {
		msg = g_strdup ("doesn't match assembly");
		usable = FALSE;
	}

	build_info = mono_get_runtime_build_info ();
	if (strlen ((const char *)info->runtime_version) > 0 && strcmp (info->runtime_version, build_info)) {
		msg = g_strdup_printf ("compiled against runtime version '%s' while this runtime has version '%s'", info->runtime_version, build_info);
		usable = FALSE;
	}
	g_free (build_info);

	full_aot = info->flags & MONO_AOT_FILE_FLAG_FULL_AOT;
	interp = info->flags & MONO_AOT_FILE_FLAG_INTERP;

	if (mono_aot_only && !full_aot) {
		if (!interp) {
			msg = g_strdup ("not compiled with --aot=full");
			usable = FALSE;
		}
	}
	if (!mono_aot_only && full_aot) {
		msg = g_strdup ("compiled with --aot=full");
		usable = FALSE;
	}
	if (mono_use_interpreter && !interp && !strcmp (assembly->aname.name, MONO_ASSEMBLY_CORLIB_NAME)) {
		/* mscorlib contains necessary interpreter trampolines */
		msg = g_strdup ("not compiled with --aot=interp");
		usable = FALSE;
	}
	if (mono_llvm_only && !(info->flags & MONO_AOT_FILE_FLAG_LLVM_ONLY)) {
		msg = g_strdup ("not compiled with --aot=llvmonly");
		usable = FALSE;
	}
	if (mono_use_llvm && !(info->flags & MONO_AOT_FILE_FLAG_WITH_LLVM)) {
		/* Prefer LLVM JITted code when using --llvm */
		msg = g_strdup ("not compiled with --aot=llvm");
		usable = FALSE;
	}
	if (mini_debug_options.mdb_optimizations && !(info->flags & MONO_AOT_FILE_FLAG_DEBUG) && !full_aot && !interp) {
		msg = g_strdup ("not compiled for debugging");
		usable = FALSE;
	}

	mono_arch_cpu_optimizations (&excluded_cpu_optimizations);
	if (info->opts & excluded_cpu_optimizations) {
		msg = g_strdup ("compiled with unsupported CPU optimizations");
		usable = FALSE;
	}

	if (info->gc_name_index != -1) {
		char *gc_name = (char*)&blob [info->gc_name_index];
		const char *current_gc_name = mono_gc_get_gc_name ();

		if (strcmp (current_gc_name, gc_name) != 0) {
			msg = g_strdup_printf ("compiled against GC %s, while the current runtime uses GC %s.\n", gc_name, current_gc_name);
			usable = FALSE;
		}
	}

	safepoints = info->flags & MONO_AOT_FILE_FLAG_SAFEPOINTS;

	if (!safepoints && mono_threads_are_safepoints_enabled ()) {
		msg = g_strdup ("not compiled with safepoints");
		usable = FALSE;
	}

#ifdef MONO_ARCH_CODE_EXEC_ONLY
	if (!(info->flags & MONO_AOT_FILE_FLAG_CODE_EXEC_ONLY)) {
		msg = g_strdup ("not compiled targeting a runtime configured as CODE_EXEC_ONLY");
		usable = FALSE;
	}
#else
	if (info->flags & MONO_AOT_FILE_FLAG_CODE_EXEC_ONLY) {
		msg = g_strdup ("compiled targeting a runtime configured as CODE_EXEC_ONLY");
		usable = FALSE;
	}
#endif

	*out_msg = msg;
	return usable;
}

/*
 * TABLE should point to a table of call instructions. Return the address called by the INDEXth entry.
 */
static void*
get_call_table_entry (void *table, int index, int entry_size)
{
#if defined(TARGET_ARM)
	guint32 *ins_addr;
	guint32 ins;
	gint32 offset;

	if (entry_size == 8) {
		ins_addr = (guint32 *)table + (index * 2);
		g_assert ((guint32) *ins_addr == (guint32 ) 0xe51ff004); // ldr pc, =<label>
		return *((char **) (ins_addr + 1));
	}

	g_assert (entry_size == 4);
	ins_addr = (guint32*)table + index;
	ins = *ins_addr;
	if ((ins >> ARMCOND_SHIFT) == ARMCOND_NV) {
		/* blx */
		offset = (((int)(((ins & 0xffffff) << 1) | ((ins >> 24) & 0x1))) << 7) >> 7;
		return (char*)ins_addr + (offset * 2) + 8 + 1;
	} else {
		g_assert ((ins >> ARMCOND_SHIFT) == ARMCOND_AL);
		/* bl */
		offset = (((int)ins & 0xffffff) << 8) >> 8;
		return (char*)ins_addr + (offset * 4) + 8;
	}
#elif defined(TARGET_ARM64)
	return mono_arch_get_call_target ((guint8*)table + (index * 4) + 4);
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
	/* The callee expects an ip which points after the call */
	return mono_arch_get_call_target ((guint8*)table + (index * 5) + 5);
#else
	g_assert_not_reached ();
	return NULL;
#endif
}

/*
 * init_amodule_got:
 *
 *   Initialize the shared got entries for AMODULE.
 */
static void
init_amodule_got (MonoAotModule *amodule, gboolean preinit)
{
	MonoJumpInfo *ji;
	MonoMemPool *mp;
	MonoJumpInfo *patches;
	guint32 got_offsets [128];
	ERROR_DECL (error);
	int i, npatches;

	/* These can't be initialized in load_aot_module () */
	if (amodule->got_initialized == GOT_INITIALIZED)
		return;

	mono_loader_lock ();

	/*
	 * If it is initialized some other thread did it in the meantime. If it is
	 * initializing it means the current thread is initializing it since we are
	 * holding the loader lock, skip it.
	 */
	if (amodule->got_initialized) {
		mono_loader_unlock ();
		return;
	}

	if (!preinit)
		amodule->got_initialized = GOT_INITIALIZING;

	mp = mono_mempool_new ();
	npatches = amodule->info.nshared_got_entries;
	for (i = 0; i < npatches; ++i)
		got_offsets [i] = i;
	if (amodule->got)
		patches = decode_patches (amodule, mp, npatches, FALSE, got_offsets);
	else
		patches = decode_patches (amodule, mp, npatches, TRUE, got_offsets);
	g_assert (patches);
	for (i = 0; i < npatches; ++i) {
		ji = &patches [i];

		if (amodule->shared_got [i]) {
		} else if (ji->type == MONO_PATCH_INFO_AOT_MODULE) {
			amodule->shared_got [i] = amodule;
		} else if (preinit) {
			/*
			 * This is called from init_amodule () during startup, so some things might not
			 * be setup. Initialize just the slots needed to make method initialization work.
			 */
			if (ji->type == MONO_PATCH_INFO_JIT_ICALL_ID) {
				if (ji->data.jit_icall_id == MONO_JIT_ICALL_mini_llvm_init_method)
					amodule->shared_got [i] = (gpointer)mini_llvm_init_method;
			}
		} else if (ji->type == MONO_PATCH_INFO_GC_CARD_TABLE_ADDR && !mono_gc_is_moving ()) {
			amodule->shared_got [i] = NULL;
		} else if (ji->type == MONO_PATCH_INFO_GC_NURSERY_START && !mono_gc_is_moving ()) {
			amodule->shared_got [i] = NULL;
		} else if (ji->type == MONO_PATCH_INFO_GC_NURSERY_BITS && !mono_gc_is_moving ()) {
			amodule->shared_got [i] = NULL;
		} else if (ji->type == MONO_PATCH_INFO_IMAGE) {
			amodule->shared_got [i] = amodule->assembly->image;
		} else if (ji->type == MONO_PATCH_INFO_MSCORLIB_GOT_ADDR) {
			if (mono_defaults.corlib) {
				MonoAotModule *mscorlib_amodule = mono_defaults.corlib->aot_module;

				if (mscorlib_amodule)
					amodule->shared_got [i] = mscorlib_amodule->got;
			} else {
				amodule->shared_got [i] = amodule->got;
			}
		} else if (ji->type == MONO_PATCH_INFO_AOT_MODULE) {
			amodule->shared_got [i] = amodule;
		} else if (ji->type == MONO_PATCH_INFO_NONE) {
		} else {
			amodule->shared_got [i] = mono_resolve_patch_target (NULL, NULL, ji, FALSE, error);
			mono_error_assert_ok (error);
		}
	}

	if (amodule->got) {
		for (i = 0; i < npatches; ++i)
			amodule->got [i] = amodule->shared_got [i];
	}
	if (amodule->info.flags & MONO_AOT_FILE_FLAG_WITH_LLVM) {
		void (*init_aotconst) (int, gpointer) = (void (*)(int, gpointer))amodule->info.llvm_init_aotconst;
		for (i = 0; i < npatches; ++i) {
			amodule->llvm_got [i] = amodule->shared_got [i];
			init_aotconst (i, amodule->llvm_got [i]);
		}
	}

	mono_mempool_destroy (mp);

	if (!preinit) {
		mono_memory_barrier ();
		amodule->got_initialized = GOT_INITIALIZED;
	}
	mono_loader_unlock ();
}

#ifdef TARGET_APPLE_MOBILE
// Follow branch islands on ARM iOS machines.
static inline guint8 *
method_address_resolve (guint8 *code_addr)
{
#if defined(TARGET_ARM) || defined(TARGET_ARM64)
#if defined(TARGET_ARM)
	// Skip branches to thumb destinations; the convention used is that the
	// lowest bit is set if the destination is thumb. See
	// get_call_table_entry.
	if (((uintptr_t) code_addr) & 0x1)
		return code_addr;
#endif
	for (;;) {
		// `mono_arch_get_call_target` takes the IP after the branch
		// instruction, not before. Add 4 bytes to compensate.
		guint8 *next = mono_arch_get_call_target (code_addr + 4);
		if (next == NULL) return code_addr;
		code_addr = next;
	}
#endif
	return code_addr;
}
#else
static inline guint8 *
method_address_resolve (guint8 *code_addr) {
	return code_addr;
}
#endif

#ifdef HOST_WASM
static void
register_methods_in_jinfo (MonoAotModule *amodule)
{
	MonoAssembly *assembly = amodule->assembly;
	int i;
	static MonoBitSet *registered;
	static int registered_len;

	/*
	 * Register the methods in AMODULE in the jit info table. There are 2 issues:
	 * - emscripten could reorder code so methods from different aot images are intermixed.
	 * - if linkonce linking is used, multiple aot images could refer to the same method.
	 */

	sort_methods (amodule);

	if (amodule->sorted_methods_len == 0)
		return;

	mono_aot_lock ();

	/* The 'registered' bitset contains whenever we have already registered a method in the jit info table */
	int max = -1;
	for (i = 0; i < amodule->sorted_methods_len; ++i)
		max = MAX (max, GPOINTER_TO_INT (amodule->sorted_methods [i]));
	g_assert (max != -1);
	if (registered == NULL) {
		registered = mono_bitset_new (max, 0);
		registered_len = max;
	} else if (max > registered_len) {
		MonoBitSet *new_registered = mono_bitset_clone (registered, max);
		mono_bitset_free (registered);
		registered = new_registered;
		registered_len = max;
	}

#if 0
	for (i = 0; i < amodule->sorted_methods_len; ++i) {
		printf ("%s %d\n", amodule->assembly->aname.name, amodule->sorted_methods [i]);
	}
#endif

	int start = 0;
	while (start < amodule->sorted_methods_len) {
		/* Find beginning of interval */
		int start_method = GPOINTER_TO_INT (amodule->sorted_methods [start]);
		if (mono_bitset_test_fast (registered, start_method)) {
			start ++;
			continue;
		}
		/* Find end of interval */
		int end = start + 1;
		while (end < amodule->sorted_methods_len && GPOINTER_TO_INT (amodule->sorted_methods [end]) == GPOINTER_TO_INT (amodule->sorted_methods [end - 1]) + 1 && !mono_bitset_test_fast (registered, GPOINTER_TO_INT (amodule->sorted_methods [end])))
			end ++;
		int end_method = GPOINTER_TO_INT (amodule->sorted_methods [end - 1]);
		//printf ("%s [%d %d]\n", amodule->assembly->aname.name, start_method, end_method);
		/* The 'end' parameter is exclusive */
		mono_jit_info_add_aot_module (assembly->image, GINT_TO_POINTER (start_method), GINT_TO_POINTER (end_method + 1));

		for (int j = start_method; j < end_method + 1; ++j)
			g_assert (!mono_bitset_test_fast (registered, j));
		start = end;
	}
	for (i = 0; i < amodule->sorted_methods_len; ++i)
		mono_bitset_set_fast (registered, GPOINTER_TO_INT (amodule->sorted_methods [i]));

	mono_aot_unlock ();
}
#endif

static void
load_aot_module (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, gpointer user_data, MonoError *error)
{
	char *aot_name, *found_aot_name;
	MonoAotModule *amodule;
	MonoDl *sofile;
	gboolean usable = TRUE;
	char *version_symbol = NULL;
	char *msg = NULL;
	gpointer *globals = NULL;
	MonoAotFileInfo *info = NULL;
	int version;
	int align_double, align_int64;
	guint8 *aot_data = NULL;

	if (mono_compile_aot)
		return;

	if (mono_aot_mode == MONO_AOT_MODE_NONE)
		return;

	if (assembly->image->aot_module)
		/*
		 * Already loaded. This can happen because the assembly loading code might invoke
		 * the assembly load hooks multiple times for the same assembly.
		 */
		return;

	if (image_is_dynamic (assembly->image))
		return;

	gboolean loaded = FALSE;

	mono_aot_lock ();

	if (static_aot_modules)
		info = (MonoAotFileInfo *)g_hash_table_lookup (static_aot_modules, assembly->aname.name);
	if (info) {
		if (!loaded_static_aot_modules)
			loaded_static_aot_modules = g_hash_table_new (NULL, NULL);
		if (g_hash_table_lookup (loaded_static_aot_modules, info))
			loaded = TRUE;
		else
			g_hash_table_insert (loaded_static_aot_modules, info, info);
	}

	mono_aot_unlock ();

	if (loaded)
		/*
		 * Already loaded by another assembly with the same name, or the same assembly loaded
		 * in another ALC.
		 */
		return;

	sofile = NULL;

	found_aot_name = NULL;

	if (info) {
		/* Statically linked AOT module */
		aot_name = g_strdup_printf ("%s", assembly->aname.name);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "Found statically linked AOT module '%s'.", aot_name);
		if (!(info->flags & MONO_AOT_FILE_FLAG_LLVM_ONLY)) {
			globals = (void **)info->globals;
			g_assert (globals);
		}
		found_aot_name = g_strdup (aot_name);
	} else {
		aot_name = g_strdup_printf ("%s%s", assembly->image->name, MONO_SOLIB_EXT);

		{
			ERROR_DECL (load_error);
			sofile = mono_dl_open (aot_name, MONO_DL_LAZY, load_error);
			if (sofile)
				found_aot_name = g_strdup (aot_name);
			else
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: image '%s' not found: %s", aot_name, mono_error_get_message_without_fields (load_error));
			mono_error_cleanup (load_error);
		}

		g_free (aot_name);
		if (!sofile) {
			GList *l;

			for (l = mono_aot_paths; l; l = l->next) {
				char *path = (char*)l->data;

				char *basename = g_path_get_basename (assembly->image->name);
				aot_name = g_strdup_printf ("%s/%s%s", path, basename, MONO_SOLIB_EXT);

				ERROR_DECL (load_error);
				sofile = mono_dl_open (aot_name, MONO_DL_LAZY, load_error);
				if (sofile)
					found_aot_name = g_strdup (aot_name);
				else
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: image '%s' not found: %s", aot_name, mono_error_get_message_without_fields (load_error));
				mono_error_cleanup (load_error);

				g_free (basename);
				g_free (aot_name);
				if (sofile)
					break;
			}
		}

		if (!sofile) {
			// Maybe do these on more platforms ?
#ifndef HOST_WASM
			if (mono_aot_only && !mono_use_interpreter && table_info_get_rows (&assembly->image->tables [MONO_TABLE_METHOD])) {
				aot_name = g_strdup_printf ("%s%s", assembly->image->name, MONO_SOLIB_EXT);
				g_error ("Failed to load AOT module '%s' ('%s') in aot-only mode.\n", aot_name, assembly->image->name);
				g_free (aot_name);
			}
#endif
			return;
		}
	}

	if (!info) {
		find_symbol (sofile, globals, "mono_aot_version", (gpointer *) &version_symbol);
		find_symbol (sofile, globals, "mono_aot_file_info", (gpointer*)&info);
	}

	// Copy aotid to MonoImage
	memcpy(&assembly->image->aotid, info->aotid, 16);

	if (version_symbol) {
		/* Old file format */
		version = atoi (version_symbol);
	} else {
		g_assert (info);
		version = info->version;
	}

	if (version != MONO_AOT_FILE_VERSION) {
		msg = g_strdup_printf ("wrong file format version (expected %d got %d)", MONO_AOT_FILE_VERSION, version);
		usable = FALSE;
	} else {
		guint8 *blob;
		void *handle;

		if (info->flags & MONO_AOT_FILE_FLAG_SEPARATE_DATA) {
			aot_data = open_aot_data (assembly, info, &handle);

			blob = aot_data + info->table_offsets [MONO_AOT_TABLE_BLOB];
		} else {
			blob = (guint8 *)info->blob;
		}

		usable = check_usable (assembly, info, blob, &msg);
	}

	if (!usable) {
		if (mono_aot_only) {
			g_error ("Failed to load AOT module '%s' while running in aot-only mode: %s.\n", found_aot_name, msg);
		} else {
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: module %s is unusable: %s.", found_aot_name, msg);
		}
		g_free (msg);
		g_free (found_aot_name);
		if (sofile) {
			ERROR_DECL (close_error);
			mono_dl_close (sofile, close_error);
			mono_error_cleanup (close_error);
		}
		assembly->image->aot_module = NULL;
		return;
	}

	/* Sanity check */
	align_double = MONO_ABI_ALIGNOF (double);
	align_int64 = MONO_ABI_ALIGNOF (gint64);
	int card_table_shift_bits = 0;
	gpointer card_table_mask = NULL;
	mono_gc_get_card_table (&card_table_shift_bits, &card_table_mask);

	g_assert (info->double_align == align_double);
	g_assert (info->long_align == align_int64);
	g_assert (info->generic_tramp_num == MONO_TRAMPOLINE_NUM);
	g_assert (info->card_table_shift_bits == card_table_shift_bits);
	g_assert (info->card_table_mask == GPOINTER_TO_UINT (card_table_mask));

	amodule = g_new0 (MonoAotModule, 1);
	amodule->aot_name = found_aot_name;
	amodule->assembly = assembly;

	memcpy (&amodule->info, info, sizeof (*info));

	amodule->got = (void **)amodule->info.jit_got;
	/*
	 * The llvm code keeps its data in separate scalar variables, so this just used by this module.
	 */
	amodule->llvm_got = g_malloc0 (sizeof (gpointer) * amodule->info.llvm_got_size);
	amodule->globals = globals;
	amodule->sofile = sofile;
	amodule->method_to_code = g_hash_table_new (mono_aligned_addr_hash, NULL);
	amodule->extra_methods = g_hash_table_new (NULL, NULL);
	amodule->shared_got = g_new0 (gpointer, info->nshared_got_entries);

	if (info->flags & MONO_AOT_FILE_FLAG_SEPARATE_DATA) {
		for (int i = 0; i < MONO_AOT_TABLE_NUM; ++i)
			amodule->tables [i] = aot_data + info->table_offsets [i];
	}

	mono_os_mutex_init_recursive (&amodule->mutex);

	/* Read image table */
	{
		guint32 table_len;
		char *table = NULL;

		if (info->flags & MONO_AOT_FILE_FLAG_SEPARATE_DATA)
			table = (char *)amodule->tables [MONO_AOT_TABLE_IMAGE_TABLE];
		else
			table = (char *)info->image_table;
		g_assert (table);

		table_len = *(guint32*)table;
		table += sizeof (guint32);
		amodule->image_table = g_new0 (MonoImage*, table_len);
		amodule->image_names = g_new0 (MonoAssemblyName, table_len);
		amodule->image_guids = g_new0 (char*, table_len);
		amodule->image_table_len = table_len;
		for (guint32 i = 0; i < table_len; ++i) {
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

			table = (char *)ALIGN_PTR_TO (table, 8);
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

	amodule->jit_code_start = (guint8 *)info->jit_code_start;
	amodule->jit_code_end = (guint8 *)info->jit_code_end;
	if (info->flags & MONO_AOT_FILE_FLAG_SEPARATE_DATA) {
		amodule->blob = (guint8*)amodule->tables [MONO_AOT_TABLE_BLOB];
		amodule->method_info_offsets = (guint32*)amodule->tables [MONO_AOT_TABLE_METHOD_INFO_OFFSETS];
		amodule->ex_info_offsets = (guint32*)amodule->tables [MONO_AOT_TABLE_EX_INFO_OFFSETS];
		amodule->class_info_offsets = (guint32*)amodule->tables [MONO_AOT_TABLE_CLASS_INFO_OFFSETS];
		amodule->class_name_table = (guint16*)amodule->tables [MONO_AOT_TABLE_CLASS_NAME];
		amodule->extra_method_table = (guint32*)amodule->tables [MONO_AOT_TABLE_EXTRA_METHOD_TABLE];
		amodule->extra_method_info_offsets = (guint32*)amodule->tables [MONO_AOT_TABLE_EXTRA_METHOD_INFO_OFFSETS];
		amodule->got_info_offsets = (guint32*)amodule->tables [MONO_AOT_TABLE_GOT_INFO_OFFSETS];
		amodule->llvm_got_info_offsets = (guint32*)amodule->tables [MONO_AOT_TABLE_LLVM_GOT_INFO_OFFSETS];
		amodule->weak_field_indexes = (guint32*)amodule->tables [MONO_AOT_TABLE_WEAK_FIELD_INDEXES];
		amodule->method_flags_table = (guint8*)amodule->tables [MONO_AOT_TABLE_METHOD_FLAGS_TABLE];
	} else {
		amodule->blob = (guint8*)info->blob;
		amodule->method_info_offsets = (guint32 *)info->method_info_offsets;
		amodule->ex_info_offsets = (guint32 *)info->ex_info_offsets;
		amodule->class_info_offsets = (guint32 *)info->class_info_offsets;
		amodule->class_name_table = (guint16 *)info->class_name_table;
		amodule->extra_method_table = (guint32 *)info->extra_method_table;
		amodule->extra_method_info_offsets = (guint32 *)info->extra_method_info_offsets;
		amodule->got_info_offsets = (guint32*)info->got_info_offsets;
		amodule->llvm_got_info_offsets = (guint32*)info->llvm_got_info_offsets;
		amodule->weak_field_indexes = (guint32*)info->weak_field_indexes;
		amodule->method_flags_table = (guint8*)info->method_flags_table;
	}
	amodule->unbox_trampolines = (guint32 *)info->unbox_trampolines;
	amodule->unbox_trampolines_end = (guint32 *)info->unbox_trampolines_end;
	amodule->unbox_trampoline_addresses = (guint32 *)info->unbox_trampoline_addresses;
	amodule->unwind_info = (guint8 *)info->unwind_info;
	amodule->mem_begin = (guint8*)amodule->jit_code_start;
	amodule->mem_end = (guint8 *)info->mem_end;
	amodule->plt = (guint8 *)info->plt;
	amodule->plt_end = (guint8 *)info->plt_end;
	amodule->mono_eh_frame = (guint8 *)info->mono_eh_frame;
	amodule->trampolines [MONO_AOT_TRAMP_SPECIFIC] = (guint8 *)info->specific_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_STATIC_RGCTX] = (guint8 *)info->static_rgctx_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_IMT] = (guint8 *)info->imt_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_GSHAREDVT_ARG] = (guint8 *)info->gsharedvt_arg_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_FTNPTR_ARG] = (guint8 *)info->ftnptr_arg_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_UNBOX_ARBITRARY] = (guint8 *)info->unbox_arbitrary_trampolines;

	if (mono_is_corlib_image (assembly->image) || !strcmp (assembly->aname.name, MONO_ASSEMBLY_CORLIB_NAME)) {
		g_assert (!mscorlib_aot_module);
		mscorlib_aot_module = amodule;
	}

	/* Compute method addresses */
	amodule->methods = (void **)g_malloc0 (amodule->info.nmethods * sizeof (gpointer));
	for (guint32 i = 0; i < amodule->info.nmethods; ++i) {
		void *addr = NULL;

		if (amodule->info.llvm_get_method) {
			gpointer (*get_method) (int) = (gpointer (*)(int))amodule->info.llvm_get_method;

			addr = get_method (i);
		}

		if (amodule->info.flags & MONO_AOT_FILE_FLAG_CODE_EXEC_ONLY) {
			addr = ((gpointer*)amodule->info.method_addresses) [i];
		} else {
			/* method_addresses () contains a table of branches, since the ios linker can update those correctly */
			if (!addr && amodule->info.method_addresses) {
				addr = get_call_table_entry (amodule->info.method_addresses, i, amodule->info.call_table_entry_size);
				g_assert (addr);
				if (addr == amodule->info.method_addresses)
					addr = NULL;
				else
					addr = method_address_resolve ((guint8 *) addr);
			}
		}
		if (addr == NULL)
			amodule->methods [i] = GINT_TO_POINTER (-1);
		else
			amodule->methods [i] = addr;
	}

	if (make_unreadable) {
#ifndef TARGET_WIN32
		guint8 *addr;
		guint8 *page_start, *page_end;
		int err, len;

		addr = amodule->mem_begin;
		g_assert (addr);
		len = amodule->mem_end - amodule->mem_begin;

		/* Round down in both directions to avoid modifying data which is not ours */
		page_start = (guint8 *) (((gssize) (addr)) & ~ (mono_pagesize () - 1)) + mono_pagesize ();
		page_end = (guint8 *) (((gssize) (addr + len)) & ~ (mono_pagesize () - 1));
		if (page_end > page_start) {
			err = mono_mprotect (page_start, (page_end - page_start), MONO_MMAP_NONE);
			g_assert (err == 0);
		}
#endif
	}

	/* Compute the boundaries of LLVM code */
	if (info->flags & MONO_AOT_FILE_FLAG_WITH_LLVM)
		compute_llvm_code_range (amodule, &amodule->llvm_code_start, &amodule->llvm_code_end);

	mono_aot_lock ();

	if (amodule->jit_code_start) {
		aot_code_low_addr = MIN (aot_code_low_addr, (gsize)amodule->jit_code_start);
		aot_code_high_addr = MAX (aot_code_high_addr, (gsize)amodule->jit_code_end);
	}
	if (amodule->llvm_code_start) {
		aot_code_low_addr = MIN (aot_code_low_addr, (gsize)amodule->llvm_code_start);
		aot_code_high_addr = MAX (aot_code_high_addr, (gsize)amodule->llvm_code_end);
	}

	g_hash_table_insert (aot_modules, assembly, amodule);
	mono_aot_unlock ();

	init_amodule_got (amodule, TRUE);

#ifdef HOST_WASM
	register_methods_in_jinfo (amodule);
#else
	if (amodule->jit_code_start)
		mono_jit_info_add_aot_module (assembly->image, amodule->jit_code_start, amodule->jit_code_end);
	if (amodule->llvm_code_start)
		mono_jit_info_add_aot_module (assembly->image, amodule->llvm_code_start, amodule->llvm_code_end);
#endif

	assembly->image->aot_module = amodule;

	if (mono_aot_only && !mono_llvm_only) {
		char *code;
		find_amodule_symbol (amodule, "specific_trampolines_page", (gpointer *)&code);
		amodule->use_page_trampolines = code != NULL;
		/*g_warning ("using page trampolines: %d", amodule->use_page_trampolines);*/
	}

	if (info->flags & MONO_AOT_FILE_FLAG_WITH_LLVM)
		/* Directly called methods might make calls through the PLT */
		init_plt (amodule);

	/*
	 * Register the plt region as a single trampoline so we can unwind from this code
	 */
	mono_aot_tramp_info_register (
		mono_tramp_info_create (
			NULL,
			amodule->plt,
			amodule->plt_end - amodule->plt,
			NULL,
			mono_unwind_get_cie_program ()
			),
		NULL
		);

	/*
	 * Since we store methoddef and classdef tokens when referring to methods/classes in
	 * referenced assemblies, we depend on the exact versions of the referenced assemblies.
	 * MS calls this 'hard binding'. This means we have to load all referenced assemblies
	 * non-lazily, since we can't handle out-of-date errors later.
	 * The cached class info also depends on the exact assemblies.
	 */
	if (!mono_opt_aot_lazy_assembly_load) {
		for (guint32 i = 0; i < amodule->image_table_len; ++i) {
			ERROR_DECL (load_error);
			load_image (amodule, i, load_error);
			mono_error_cleanup (load_error); /* FIXME don't swallow the error */
		}
	}

	if (amodule->out_of_date) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: Module %s is unusable because a dependency is out-of-date.", assembly->image->name);
		if (mono_aot_only && (mono_aot_mode != MONO_AOT_MODE_LLVMONLY_INTERP))
			g_error ("Failed to load AOT module '%s' while running in aot-only mode because a dependency cannot be found or it is out of date.\n", found_aot_name);
	} else {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: image '%s' found.", found_aot_name);
	}
}

/*
 * mono_aot_register_module:
 *
 * This should be called by embedding code to register normal AOT modules statically linked
 * into the executable.
 *
 * \param aot_info the value of the 'mono_aot_module_<ASSEMBLY_NAME>_info' global symbol from the AOT module.
 */
void
mono_aot_register_module (gpointer *aot_info)
{
	gpointer *globals;
	char *aname;
	MonoAotFileInfo *info = (MonoAotFileInfo *)aot_info;

	g_assert (info->version == MONO_AOT_FILE_VERSION);

	if (!(info->flags & MONO_AOT_FILE_FLAG_LLVM_ONLY)) {
		globals = (void **)info->globals;
		g_assert (globals);
	}

	aname = (char *)info->assembly_name;

	/* This could be called before startup */
	if (aot_modules)
		mono_aot_lock ();

	if (!static_aot_modules)
		static_aot_modules = g_hash_table_new (g_str_hash, g_str_equal);

	g_hash_table_insert (static_aot_modules, aname, info);

	if (info->flags & MONO_AOT_FILE_FLAG_EAGER_LOAD) {
		/*
		 * This assembly contains shared generic instances/wrappers, etc. It needs be be loaded
		 * before AOT code is loaded.
		 */
		g_assert (!container_assm_name);
		container_assm_name = aname;
	}

	if (aot_modules)
		mono_aot_unlock ();
}

void
mono_aot_init (void)
{
	mono_os_mutex_init_recursive (&aot_mutex);
	mono_os_mutex_init_recursive (&aot_page_mutex);
	aot_modules = g_hash_table_new (NULL, NULL);

	mono_install_assembly_load_hook_v2 (load_aot_module, NULL, FALSE);
	mono_counters_register ("Async JIT info size", MONO_COUNTER_INT|MONO_COUNTER_JIT, &async_jit_info_size);

	char *lastaot = g_getenv ("MONO_LASTAOT");
	if (lastaot) {
		mono_last_aot_method = atoi (lastaot);
		g_free (lastaot);
	}
}

/*
 * load_container_amodule:
 *
 *   Load the container assembly and its AOT image.
 */
static void
load_container_amodule (MonoAssemblyLoadContext *alc)
{
	ERROR_DECL (error);

	if (!container_assm_name || container_amodule)
		return;

	char *local_ref = container_assm_name;
	container_assm_name = NULL;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	MonoAssemblyOpenRequest req;
	gchar *dll = g_strdup_printf (		"%s.dll", local_ref);
	/*
	 * Don't fire managed assembly load events whose execution
	 * might require this module to be already loaded.
	 */
	mono_assembly_request_prepare_open (&req, alc);
	req.request.no_managed_load_event = TRUE;
	MonoAssembly *assm = mono_assembly_request_open (dll, &req, &status);
	if (!assm) {
		gchar *exe = g_strdup_printf ("%s.exe", local_ref);
		assm = mono_assembly_request_open (exe, &req, &status);
	}
	g_assert (assm);
	load_aot_module (alc, assm, NULL, error);
	container_amodule = assm->image->aot_module;
}

static gboolean
decode_cached_class_info (MonoAotModule *module, MonoCachedClassInfo *info, guint8 *buf, guint8 **endbuf)
{
	ERROR_DECL (error);
	guint32 flags;
	MethodRef ref;
	gboolean res;

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
	info->is_generic_container = (flags >> 8) & 0x1;
	info->has_weak_fields = (flags >> 9) & 0x1;

	if (info->has_cctor) {
		res = decode_method_ref (module, &ref, buf, &buf, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */
		if (!res)
			return FALSE;
		info->cctor_token = ref.token;
	}
	if (info->has_finalize) {
		res = decode_method_ref (module, &ref, buf, &buf, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */
		if (!res)
			return FALSE;
		info->finalize_image = ref.image;
		info->finalize_token = ref.token;
	}

	info->instance_size = decode_value (buf, &buf);
	info->class_size = decode_value (buf, &buf);
	info->packing_size = decode_value (buf, &buf);
	info->min_align = decode_value (buf, &buf);

	*endbuf = buf;

	return TRUE;
}

gpointer
mono_aot_get_method_from_vt_slot (MonoVTable *vtable, int slot, MonoError *error)
{
	int i;
	MonoClass *klass = vtable->klass;
	MonoAotModule *amodule = m_class_get_image (klass)->aot_module;
	guint8 *info, *p;
	MonoCachedClassInfo class_info;
	gboolean err;
	MethodRef ref;
	gboolean res;
	gpointer addr;
	ERROR_DECL (inner_error);

	error_init (error);

	if (MONO_CLASS_IS_INTERFACE_INTERNAL (klass) || m_class_get_rank (klass) || !amodule)
		return NULL;

	info = &amodule->blob [mono_aot_get_offset (amodule->class_info_offsets, mono_metadata_token_index (m_class_get_type_token (klass)) - 1)];
	p = info;

	err = decode_cached_class_info (amodule, &class_info, p, &p);
	if (!err)
		return NULL;

	for (i = 0; i < slot; ++i) {
		decode_method_ref (amodule, &ref, p, &p, inner_error);
		mono_error_cleanup (inner_error); /* FIXME don't swallow the error */
	}

	res = decode_method_ref (amodule, &ref, p, &p, inner_error);
	mono_error_cleanup (inner_error); /* FIXME don't swallow the error */
	if (!res)
		return NULL;
	if (ref.no_aot_trampoline)
		return NULL;

	if (mono_metadata_token_index (ref.token) == 0 || mono_metadata_token_table (ref.token) != MONO_TABLE_METHOD)
		return NULL;

	addr = mono_aot_get_method_from_token (ref.image, ref.token, error);
	return addr;
}

gboolean
mono_aot_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	MonoAotModule *amodule = m_class_get_image (klass)->aot_module;
	guint8 *p;
	gboolean err;

	if (m_class_get_rank (klass) || !m_class_get_type_token (klass) || !amodule)
		return FALSE;

	p = (guint8*)&amodule->blob [mono_aot_get_offset (amodule->class_info_offsets, mono_metadata_token_index (m_class_get_type_token (klass)) - 1)];

	err = decode_cached_class_info (amodule, res, p, &p);
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
	MonoAotModule *amodule = image->aot_module;
	guint16 *table, *entry;
	guint16 table_size;
	guint32 hash;
	char full_name_buf [1024];
	char *full_name;
	const char *name2, *name_space2;
	MonoTableInfo  *t;
	guint32 cols [MONO_TYPEDEF_SIZE];
	GHashTable *nspace_table;

	if (!amodule || !amodule->class_name_table)
		return FALSE;

	amodule_lock (amodule);

	*klass = NULL;

	/* First look in the cache */
	if (!amodule->name_cache)
		amodule->name_cache = g_hash_table_new (g_str_hash, g_str_equal);
	nspace_table = (GHashTable *)g_hash_table_lookup (amodule->name_cache, name_space);
	if (nspace_table) {
		*klass = (MonoClass *)g_hash_table_lookup (nspace_table, name);
		if (*klass) {
			amodule_unlock (amodule);
			return TRUE;
		}
	}

	table_size = amodule->class_name_table [0];
	table = amodule->class_name_table + 1;

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
	hash = mono_metadata_str_hash (full_name) % table_size;
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
				ERROR_DECL (error);
				amodule_unlock (amodule);
				*klass = mono_class_get_checked (image, token, error);
				if (!is_ok (error))
					mono_error_cleanup (error); /* FIXME don't swallow the error */

				/* Add to cache */
				if (*klass) {
					amodule_lock (amodule);
					nspace_table = (GHashTable *)g_hash_table_lookup (amodule->name_cache, name_space);
					if (!nspace_table) {
						nspace_table = g_hash_table_new (g_str_hash, g_str_equal);
						g_hash_table_insert (amodule->name_cache, (char*)name_space2, nspace_table);
					}
					g_hash_table_insert (nspace_table, (char*)name2, *klass);
					amodule_unlock (amodule);
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

	amodule_unlock (amodule);

	return TRUE;
}

GHashTable *
mono_aot_get_weak_field_indexes (MonoImage *image)
{
	MonoAotModule *amodule = image->aot_module;

	if (!amodule)
		return NULL;

#if ENABLE_WEAK_ATTR
	/* Initialize weak field indexes from the cached copy */
	guint32 *indexes = (guint32*)amodule->weak_field_indexes;
	int len  = indexes [0];
	GHashTable *indexes_hash = g_hash_table_new (NULL, NULL);
	for (int i = 0; i < len; ++i)
		g_hash_table_insert (indexes_hash, GUINT_TO_POINTER (indexes [i + 1]), GUINT_TO_POINTER (1));
	return indexes_hash;
#else
	g_assert_not_reached ();
#endif
}

/* Compute the boundaries of the LLVM code for AMODULE. */
static void
compute_llvm_code_range (MonoAotModule *amodule, guint8 **code_start, guint8 **code_end)
{
	guint8 *p;
	int version, fde_count;
	gint32 *table;

	if (amodule->info.llvm_get_method) {
		gpointer (*get_method) (int) = (gpointer (*)(int))amodule->info.llvm_get_method;

#ifdef HOST_WASM
		gsize min = 1 << 30, max = 0;
		//gsize prev = 0;

		// FIXME: This depends on emscripten allocating ftnptr ids sequentially
		for (guint32 i = 0; i < amodule->info.nmethods; ++i) {
			void *addr = NULL;

			addr = get_method ((int)i);
			gsize val = (gsize)addr;
			if (val) {
				//g_assert (val > prev);
				if (val < min)
					min = val;
				else if (val > max)
					max = val;
				//prev = val;
			}
		}
		if (max) {
			*code_start = (guint8*)min;
			*code_end = (guint8*)(max + 1);
		} else {
			*code_start = NULL;
			*code_end = NULL;
		}
#else
		*code_start = (guint8 *)get_method (-1);
		*code_end = (guint8 *)get_method (-2);

		g_assert (*code_end > *code_start);
#endif
		return;
	}

	g_assert (amodule->mono_eh_frame);

	p = amodule->mono_eh_frame;

	/* p points to data emitted by LLVM in DwarfException::EmitMonoEHFrame () */

	/* Header */
	version = *p;
	g_assert (version == 3);
	p ++;
	p ++;
	p = (guint8 *)ALIGN_PTR_TO (p, 4);

	fde_count = *(guint32*)p;
	p += 4;
	table = (gint32*)p;

	if (fde_count > 0) {
		*code_start = (guint8 *)amodule->methods [table [0]];
		*code_end = (guint8*)amodule->methods [table [(fde_count - 1) * 2]] + table [fde_count * 2];
	} else {
		*code_start = NULL;
		*code_end = NULL;
	}
}

static gboolean
is_llvm_code (MonoAotModule *amodule, guint8 *code)
{
#if HOST_WASM
	return TRUE;
#else
	if ((guint8*)code >= amodule->llvm_code_start && (guint8*)code < amodule->llvm_code_end)
		return TRUE;
	else
		return FALSE;
#endif
}

static gboolean
is_thumb_code (MonoAotModule *amodule, guint8 *code)
{
	if (is_llvm_code (amodule, code) && (amodule->info.flags & MONO_AOT_FILE_FLAG_LLVM_THUMB))
		return TRUE;
	else
		return FALSE;
}

/*
 * decode_llvm_mono_eh_frame:
 *
 *   Decode the EH information emitted by our modified LLVM compiler and construct a
 * MonoJitInfo structure from it.
 * If JINFO is NULL, set OUT_LLVM_CLAUSES to the number of llvm level clauses.
 * This function is async safe when called in async context.
 */
static void
decode_llvm_mono_eh_frame (MonoAotModule *amodule, MonoJitInfo *jinfo,
						   guint8 *code, guint32 code_len,
						   MonoJitExceptionInfo *clauses, int num_clauses,
						   GSList **nesting,
						   int *this_reg, int *this_offset, int *out_llvm_clauses)
{
	guint8 *p, *code1, *code2;
	guint8 *fde, *cie, *code_start, *code_end;
	int version, fde_count;
	gint32 *table;
	int i, pos, left, right;
	MonoJitExceptionInfo *ei;
	MonoMemoryManager *mem_manager;
	guint32 fde_len, ei_len, nested_len, nindex;
	gpointer *type_info;
	MonoLLVMFDEInfo info;
	guint8 *unw_info;
	gboolean async;

	mem_manager = m_image_get_mem_manager (amodule->assembly->image);

	async = mono_thread_info_is_async_context ();

	if (!amodule->mono_eh_frame) {
		if (!jinfo) {
			*out_llvm_clauses = num_clauses;
			return;
		}
		memcpy (jinfo->clauses, clauses, num_clauses * sizeof (MonoJitExceptionInfo));
		return;
	}

	g_assert (amodule->mono_eh_frame && code);

	p = amodule->mono_eh_frame;

	/* p points to data emitted by LLVM in DwarfMonoException::EmitMonoEHFrame () */

	/* Header */
	version = *p;
	g_assert (version == 3);
	p ++;
	/* func_encoding = *p; */
	p ++;
	p = (guint8 *)ALIGN_PTR_TO (p, 4);

	fde_count = *(guint32*)p;
	p += 4;
	table = (gint32*)p;

	/* There is +1 entry in the table */
	cie = p + ((fde_count + 1) * 8);

	/* Binary search in the table to find the entry for code */
	left = 0;
	right = fde_count;
	while (TRUE) {
		pos = (left + right) / 2;

		/* The table contains method index/fde offset pairs */
		g_assert (table [(pos * 2)] != -1);
		code1 = (guint8 *)amodule->methods [table [(pos * 2)]];
		if (pos + 1 == fde_count) {
			code2 = amodule->llvm_code_end;
		} else {
			g_assert (table [(pos + 1) * 2] != -1);
			code2 = (guint8 *)amodule->methods [table [(pos + 1) * 2]];
		}

		if (code < code1)
			right = pos;
		else if (code >= code2)
			left = pos + 1;
		else
			break;
	}

	code_start = (guint8 *)amodule->methods [table [(pos * 2)]];
	if (pos + 1 == fde_count) {
		/* The +1 entry in the table contains the length of the last method */
		int len = table [(pos + 1) * 2];
		code_end = code_start + len;
	} else {
		code_end = (guint8 *)amodule->methods [table [(pos + 1) * 2]];
	}
	if (!code_len)
		code_len = code_end - code_start;

	g_assert (code >= code_start && code < code_end);

	if (is_thumb_code (amodule, code_start))
		/* Clear thumb flag */
		code_start = (guint8*)(((gsize)code_start) & ~1);

	fde = amodule->mono_eh_frame + table [(pos * 2) + 1];
	/* This won't overflow because there is +1 entry in the table */
	fde_len = table [(pos * 2) + 2 + 1] - table [(pos * 2) + 1];

	/* Compute lengths */
	mono_unwind_decode_llvm_mono_fde (fde, fde_len, cie, code_start, &info, NULL, NULL, NULL);

	if (async) {
		/* These are leaked, but the leak is bounded */
		ei = mono_mem_manager_alloc0_lock_free (mem_manager, info.ex_info_len * sizeof (MonoJitExceptionInfo));
		type_info = mono_mem_manager_alloc0_lock_free (mem_manager, info.ex_info_len * sizeof (gpointer));
		unw_info = mono_mem_manager_alloc0_lock_free (mem_manager, info.unw_info_len);
	} else {
		ei = (MonoJitExceptionInfo *)g_malloc0 (info.ex_info_len * sizeof (MonoJitExceptionInfo));
		type_info = (gpointer *)g_malloc0 (info.ex_info_len * sizeof (gpointer));
		unw_info = (guint8*)g_malloc0 (info.unw_info_len);
	}
	mono_unwind_decode_llvm_mono_fde (fde, fde_len, cie, code_start, &info, ei, type_info, unw_info);

	ei_len = info.ex_info_len;
	*this_reg = info.this_reg;
	*this_offset = info.this_offset;

	/*
	 * LLVM might represent one IL region with multiple regions.
	 */

	/* Count number of nested clauses */
	nested_len = 0;
	for (i = 0; i < ei_len; ++i) {
		/* This might be unaligned */
		gint32 cindex1 = read32 (type_info [i]);
		GSList *l;

		for (l = nesting [cindex1]; l; l = l->next)
			nested_len ++;
	}

	if (!jinfo) {
		*out_llvm_clauses = ei_len + nested_len;
		return;
	}

	/* Store the unwind info addr/length in the MonoJitInfo structure itself so its async safe */
	MonoUnwindJitInfo *jinfo_unwind = mono_jit_info_get_unwind_info (jinfo);
	g_assert (jinfo_unwind);
	jinfo_unwind->unw_info = unw_info;
	jinfo_unwind->unw_info_len = info.unw_info_len;

	for (i = 0; i < ei_len; ++i) {
		/*
		 * clauses contains the original IL exception info saved by the AOT
		 * compiler, we have to combine that with the information produced by LLVM
		 */
		/* The type_info entries contain IL clause indexes */
		int clause_index = read32 (type_info [i]);
		MonoJitExceptionInfo *jei = &jinfo->clauses [i];
		MonoJitExceptionInfo *orig_jei = &clauses [clause_index];

		g_assert (clause_index < num_clauses);
		jei->flags = orig_jei->flags;
		jei->data.catch_class = orig_jei->data.catch_class;

		jei->try_start = ei [i].try_start;
		jei->try_end = ei [i].try_end;
		jei->handler_start = ei [i].handler_start;
		jei->clause_index = clause_index;

		if (is_thumb_code (amodule, (guint8 *)jei->try_start)) {
			jei->try_start = (void*)((gsize)jei->try_start & ~1);
			jei->try_end = (void*)((gsize)jei->try_end & ~1);
			/* Make sure we transition to thumb when a handler starts */
			jei->handler_start = (void*)((gsize)jei->handler_start + 1);
		}
	}

	/* See exception_cb () in mini-llvm.c as to why this is needed */
	nindex = ei_len;
	for (i = 0; i < ei_len; ++i) {
		gint32 cindex1 = read32 (type_info [i]);
		GSList *l;

		for (l = nesting [cindex1]; l; l = l->next) {
			gint32 nesting_cindex = GPOINTER_TO_INT (l->data);
			MonoJitExceptionInfo *nesting_ei;
			MonoJitExceptionInfo *nesting_clause = &clauses [nesting_cindex];

			nesting_ei = &jinfo->clauses [nindex];
			nindex ++;

			memcpy (nesting_ei, &jinfo->clauses [i], sizeof (MonoJitExceptionInfo));
			nesting_ei->flags = nesting_clause->flags;
			nesting_ei->data.catch_class = nesting_clause->data.catch_class;
			nesting_ei->clause_index = nesting_cindex;
		}
	}
	g_assert (nindex == ei_len + nested_len);
}

static gpointer
alloc0_jit_info_data (MonoMemoryManager *mem_manager, int size, gboolean async_context)

#define alloc0_jit_info_data(mem_manager, size, async_context) (g_cast (alloc0_jit_info_data ((mem_manager), (size), (async_context))))

{
	gpointer res;

	if (async_context) {
		res = mono_mem_manager_alloc0_lock_free (mem_manager, size);
		mono_atomic_fetch_add_i32 (&async_jit_info_size, size);
	} else {
		res = mono_mem_manager_alloc0 (mem_manager, size);
	}
	return res;
}

/*
 * In async context, this is async safe.
 */
static MonoJitInfo*
decode_exception_debug_info (MonoAotModule *amodule,
							 MonoMethod *method, guint8* ex_info,
							 guint8 *code, guint32 code_len)
{
	ERROR_DECL (error);
	int buf_len, num_clauses;
	MonoJitInfo *jinfo;
	MonoJitInfoFlags flags = JIT_INFO_NONE;
	guint unwind_info, eflags;
	gboolean has_generic_jit_info, has_dwarf_unwind_info, has_clauses, has_seq_points, has_try_block_holes, has_arch_eh_jit_info;
	gboolean from_llvm, has_gc_map;
	guint8 *p;
	int try_holes_info_size, num_holes;
	int this_reg = 0, this_offset = 0;
	MonoMemoryManager *mem_manager = m_image_get_mem_manager (amodule->assembly->image);
	gboolean async;

	code = (guint8*)MINI_FTNPTR_TO_ADDR (code);

	/* Load the method info from the AOT file */
	async = mono_thread_info_is_async_context ();

	p = ex_info;
	eflags = decode_value (p, &p);
	has_generic_jit_info = (eflags & 1) != 0;
	has_dwarf_unwind_info = (eflags & 2) != 0;
	has_clauses = (eflags & 4) != 0;
	has_seq_points = (eflags & 8) != 0;
	from_llvm = (eflags & 16) != 0;
	has_try_block_holes = (eflags & 32) != 0;
	has_gc_map = (eflags & 64) != 0;
	has_arch_eh_jit_info = (eflags & 128) != 0;

	if (has_dwarf_unwind_info) {
		unwind_info = decode_value (p, &p);
		g_assert (unwind_info < (1 << 30));
	} else {
		unwind_info = decode_value (p, &p);
	}
	if (has_generic_jit_info)
		flags |= JIT_INFO_HAS_GENERIC_JIT_INFO;

	if (has_try_block_holes) {
		num_holes = decode_value (p, &p);
		flags |= JIT_INFO_HAS_TRY_BLOCK_HOLES;
		try_holes_info_size = sizeof (MonoTryBlockHoleTableJitInfo) + num_holes * sizeof (MonoTryBlockHoleJitInfo);
	} else {
		num_holes = try_holes_info_size = 0;
	}

	if (has_arch_eh_jit_info) {
		flags |= JIT_INFO_HAS_ARCH_EH_INFO;
		/* Overwrite the original code_len which includes alignment padding */
		code_len = decode_value (p, &p);
	}

	/* Exception table */
	if (has_clauses)
		num_clauses = decode_value (p, &p);
	else
		num_clauses = 0;

	if (from_llvm) {
		int len;
		MonoJitExceptionInfo *clauses;
		GSList **nesting;

		/*
		 * Part of the info is encoded by the AOT compiler, the rest is in the .eh_frame
		 * section.
		 */
		if (async) {
			if (num_clauses < 16) {
				clauses = g_newa (MonoJitExceptionInfo, num_clauses);
				nesting = g_newa (GSList*, num_clauses);
			} else {
				clauses = alloc0_jit_info_data (mem_manager, sizeof (MonoJitExceptionInfo) * num_clauses, TRUE);
				nesting = alloc0_jit_info_data (mem_manager, sizeof (GSList*) * num_clauses, TRUE);
			}
			memset (clauses, 0, sizeof (MonoJitExceptionInfo) * num_clauses);
			memset (nesting, 0, sizeof (GSList*) * num_clauses);
		} else {
			clauses = g_new0 (MonoJitExceptionInfo, num_clauses);
			nesting = g_new0 (GSList*, num_clauses);
		}

		for (int i = 0; i < num_clauses; ++i) {
			MonoJitExceptionInfo *ei = &clauses [i];

			ei->flags = decode_value (p, &p);

			if (!(ei->flags == MONO_EXCEPTION_CLAUSE_FILTER || ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY)) {
				len = decode_value (p, &p);

				if (len > 0) {
					if (async) {
						p += len;
					} else {
						ei->data.catch_class = decode_klass_ref (amodule, p, &p, error);
						mono_error_cleanup (error); /* FIXME don't swallow the error */
					}
				}
			}

			ei->clause_index = i;

			ei->try_offset = decode_value (p, &p);
			ei->try_len = decode_value (p, &p);
			ei->handler_offset = decode_value (p, &p);
			ei->handler_len = decode_value (p, &p);

			/* Read the list of nesting clauses */
			while (TRUE) {
				int nesting_index = decode_value (p, &p);
				if (nesting_index == -1)
					break;
				// FIXME: async
				g_assert (!async);
				nesting [i] = g_slist_prepend (nesting [i], GINT_TO_POINTER (nesting_index));
			}
		}

		flags |= JIT_INFO_HAS_UNWIND_INFO;

		int num_llvm_clauses;
		/* Get the length first */
		decode_llvm_mono_eh_frame (amodule, NULL, code, code_len, clauses, num_clauses, nesting, &this_reg, &this_offset, &num_llvm_clauses);
		len = mono_jit_info_size (flags, num_llvm_clauses, num_holes);
		jinfo = (MonoJitInfo *)alloc0_jit_info_data (mem_manager, len, async);
		mono_jit_info_init (jinfo, method, code, code_len, flags, num_llvm_clauses, num_holes);

		decode_llvm_mono_eh_frame (amodule, jinfo, code, code_len, clauses, num_clauses, nesting, &this_reg, &this_offset, NULL);

		if (!async) {
			g_free (clauses);
			for (int i = 0; i < num_clauses; ++i)
				g_slist_free (nesting [i]);
			g_free (nesting);
		}
		jinfo->from_llvm = 1;
	} else {
		int len = mono_jit_info_size (flags, num_clauses, num_holes);
		jinfo = (MonoJitInfo *)alloc0_jit_info_data (mem_manager, len, async);
		/* The jit info table needs to sort addresses so it contains non-authenticated pointers on arm64e */
		mono_jit_info_init (jinfo, method, code, code_len, flags, num_clauses, num_holes);

		for (guint32 i = 0; i < jinfo->num_clauses; ++i) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];

			ei->flags = decode_value (p, &p);

#ifdef MONO_CONTEXT_SET_LLVM_EXC_REG
			/* Not used for catch clauses */
			if (ei->flags != MONO_EXCEPTION_CLAUSE_NONE)
				ei->exvar_offset = decode_value (p, &p);
#else
			ei->exvar_offset = decode_value (p, &p);
#endif

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER || ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
				ei->data.filter = code + decode_value (p, &p);
			} else {
				len = decode_value (p, &p);

				if (len > 0) {
					if (async) {
						p += len;
					} else {
						ei->data.catch_class = decode_klass_ref (amodule, p, &p, error);
						mono_error_cleanup (error); /* FIXME don't swallow the error */
					}
				}
			}

			ei->try_start = code + decode_value (p, &p);
			ei->try_end = code + decode_value (p, &p);
			ei->handler_start = code + decode_value (p, &p);

			/* Keep try_start/end non-authenticated, they are never branched to */
			//ei->try_start = MINI_ADDR_TO_FTNPTR (ei->try_start);
			//ei->try_end = MINI_ADDR_TO_FTNPTR (ei->try_end);
			ei->handler_start = MINI_ADDR_TO_FTNPTR (ei->handler_start);
			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER)
				ei->data.filter = MINI_ADDR_TO_FTNPTR (ei->data.filter);
			else if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY)
				ei->data.handler_end = MINI_ADDR_TO_FTNPTR (ei->data.handler_end);
		}

		jinfo->unwind_info = unwind_info;
		jinfo->from_aot = 1;
	}

	if (has_try_block_holes) {
		MonoTryBlockHoleTableJitInfo *table;

		g_assert (jinfo->has_try_block_holes);

		table = mono_jit_info_get_try_block_hole_table_info (jinfo);
		g_assert (table);

		table->num_holes = (guint16)num_holes;
		for (int i = 0; i < num_holes; ++i) {
			MonoTryBlockHoleJitInfo *hole = &table->holes [i];
			hole->clause = decode_value (p, &p);
			hole->length = decode_value (p, &p);
			hole->offset = decode_value (p, &p);
		}
	}

	if (has_arch_eh_jit_info) {
		MonoArchEHJitInfo *eh_info;

		g_assert (jinfo->has_arch_eh_info);

		eh_info = mono_jit_info_get_arch_eh_info (jinfo);
		eh_info->stack_size = decode_value (p, &p);
		eh_info->epilog_size = decode_value (p, &p);
	}

	if (async) {
		/* The rest is not needed in async mode */
		jinfo->async = TRUE;
		jinfo->d.aot_info = amodule;
		// FIXME: Cache
		return jinfo;
	}

	if (has_generic_jit_info) {
		MonoGenericJitInfo *gi;
		int len;

		g_assert (jinfo->has_generic_jit_info);

		gi = mono_jit_info_get_generic_jit_info (jinfo);
		g_assert (gi);

		gi->nlocs = decode_value (p, &p);
		if (gi->nlocs) {
			gi->locations = (MonoDwarfLocListEntry *)alloc0_jit_info_data (mem_manager, gi->nlocs * sizeof (MonoDwarfLocListEntry), async);
			for (int i = 0; i < gi->nlocs; ++i) {
				MonoDwarfLocListEntry *entry = &gi->locations [i];

				entry->is_reg = decode_value (p, &p);
				entry->reg = decode_value (p, &p);
				if (!entry->is_reg)
					entry->offset = decode_value (p, &p);
				if (i > 0)
					entry->from = decode_value (p, &p);
				entry->to = decode_value (p, &p);
			}
			gi->has_this = 1;
		} else {
			if (from_llvm) {
				gi->has_this = this_reg != -1;
				gi->this_reg = this_reg;
				gi->this_offset = this_offset;
			} else {
				gi->has_this = decode_value (p, &p);
				gi->this_reg = decode_value (p, &p);
				gi->this_offset = decode_value (p, &p);
			}
		}

		len = decode_value (p, &p);
		if (async) {
			p += len;
		} else {
			jinfo->d.method = decode_resolve_method_ref (amodule, p, &p, error);
			mono_error_cleanup (error); /* FIXME don't swallow the error */
		}

		gi->generic_sharing_context = alloc0_jit_info_data (mem_manager, sizeof (MonoGenericSharingContext), async);
		if (decode_value (p, &p)) {
			/* gsharedvt */
			MonoGenericSharingContext *gsctx = gi->generic_sharing_context;

			gsctx->is_gsharedvt = TRUE;
		}
	}

	if (method && has_seq_points) {
		MonoSeqPointInfo *seq_points;

		p += mono_seq_point_info_read (&seq_points, p, FALSE);

		if (!async) {
			// FIXME: Call a function in seq-points.c
			// FIXME:
			MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
			jit_mm_lock (jit_mm);
			/* This could be set already since this function can be called more than once for the same method */
			if (!g_hash_table_lookup (jit_mm->seq_points, method))
				g_hash_table_insert (jit_mm->seq_points, method, seq_points);
			else
				mono_seq_point_info_free (seq_points);
			jit_mm_unlock (jit_mm);
		}

		jinfo->seq_points = seq_points;
	}

	/* Load debug info */
	buf_len = decode_value (p, &p);
	if (!async)
		mono_debug_add_aot_method (method, code, p, buf_len);
	p += buf_len;

	if (has_gc_map) {
		int map_size = decode_value (p, &p);
		/* The GC map requires 4 bytes of alignment */
		while ((guint64)(gsize)p % 4)
			p ++;
		jinfo->gc_info = p;
		p += map_size;
	}

	if (amodule != m_class_get_image (jinfo->d.method->klass)->aot_module && !async) {
		mono_aot_lock ();
		if (!ji_to_amodule)
			ji_to_amodule = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (ji_to_amodule, jinfo, amodule);
		mono_aot_unlock ();
	}

	return jinfo;
}

static gboolean
amodule_contains_code_addr (MonoAotModule *amodule, guint8 *code)
{
	return (code >= amodule->jit_code_start && code <= amodule->jit_code_end) ||
		(code >= amodule->llvm_code_start && code <= amodule->llvm_code_end);
}

/*
 * mono_aot_get_unwind_info:
 *
 *   Return a pointer to the DWARF unwind info belonging to JI.
 */
guint8*
mono_aot_get_unwind_info (MonoJitInfo *ji, guint32 *unwind_info_len)
{
	MonoAotModule *amodule;
	guint8 *p;
	guint8 *code = (guint8 *)ji->code_start;

	if (ji->async)
		amodule = ji->d.aot_info;
	else
		amodule = m_class_get_image (jinfo_get_method (ji)->klass)->aot_module;
	g_assert (amodule);
	g_assert (ji->from_aot);

	if (!amodule_contains_code_addr (amodule, code)) {
		/* ji belongs to a different aot module than amodule */
		mono_aot_lock ();
		g_assert (ji_to_amodule);
		amodule = (MonoAotModule *)g_hash_table_lookup (ji_to_amodule, ji);
		g_assert (amodule);
		g_assert (amodule_contains_code_addr (amodule, code));
		mono_aot_unlock ();
	}

	p = amodule->unwind_info + ji->unwind_info;
	*unwind_info_len = decode_value (p, &p);
	return p;
}

static void
msort_method_addresses_internal (gpointer *array, int *indexes, int lo, int hi, gpointer *scratch, int *scratch_indexes)
{
	int mid = (lo + hi) / 2;
	int i, t_lo, t_hi;

	if (lo >= hi)
		return;

	if (hi - lo < 32) {
		for (i = lo; i < hi; ++i)
			if (array [i] > array [i + 1])
				break;
		if (i == hi)
			/* Already sorted */
			return;
	}

	msort_method_addresses_internal (array, indexes, lo, mid, scratch, scratch_indexes);
	msort_method_addresses_internal (array, indexes, mid + 1, hi, scratch, scratch_indexes);

	if (array [mid] < array [mid + 1])
		return;

	/* Merge */
	t_lo = lo;
	t_hi = mid + 1;
	for (i = lo; i <= hi; i ++) {
		if (t_lo <= mid && ((t_hi > hi) || array [t_lo] < array [t_hi])) {
			scratch [i] = array [t_lo];
			scratch_indexes [i] = indexes [t_lo];
			t_lo ++;
		} else {
			scratch [i] = array [t_hi];
			scratch_indexes [i] = indexes [t_hi];
			t_hi ++;
		}
	}
	for (i = lo; i <= hi; ++i) {
		array [i] = scratch [i];
		indexes [i] = scratch_indexes [i];
	}
}

static void
msort_method_addresses (gpointer *array, int *indexes, int len)
{
	gpointer *scratch;
	int *scratch_indexes;

	scratch = g_new (gpointer, len);
	scratch_indexes = g_new (int, len);
	msort_method_addresses_internal (array, indexes, 0, len - 1, scratch, scratch_indexes);
	g_free (scratch);
	g_free (scratch_indexes);
}

static void
sort_methods (MonoAotModule *amodule)
{
	int nmethods = amodule->info.nmethods;

	/* Compute a sorted table mapping code to method indexes. */
	if (amodule->sorted_methods)
		return;

	// FIXME: async
	gpointer *methods = g_new0 (gpointer, nmethods);
	int *method_indexes = g_new0 (int, nmethods);
	int methods_len = 0;

	for (int i = 0; i < nmethods; ++i) {
		/* Skip the -1 entries to speed up sorting */
		if (amodule->methods [i] == GINT_TO_POINTER (-1))
			continue;
		methods [methods_len] = amodule->methods [i];
		method_indexes [methods_len] = i;
		methods_len ++;
	}
	/* Use a merge sort as this is mostly sorted */
	msort_method_addresses (methods, method_indexes, methods_len);
	for (int i = 0; i < methods_len -1; ++i)
		g_assert (methods [i] <= methods [i + 1]);

	amodule->sorted_methods_len = methods_len;
	if (mono_atomic_cas_ptr ((gpointer*)&amodule->sorted_methods, methods, NULL) != NULL)
		/* Somebody got in before us */
		g_free (methods);
	if (mono_atomic_cas_ptr ((gpointer*)&amodule->sorted_method_indexes, method_indexes, NULL) != NULL)
		/* Somebody got in before us */
		g_free (method_indexes);
}

/*
 * mono_aot_find_jit_info:
 *
 *   In async context, the resulting MonoJitInfo will not have its method field set, and it will not be added
 * to the jit info tables.
 * FIXME: Large sizes in the lock free allocator
 */
MonoJitInfo *
mono_aot_find_jit_info (MonoImage *image, gpointer addr)
{
	ERROR_DECL (error);
	int pos, left, right, code_len;
	int method_index, table_len;
	guint32 token;
	MonoAotModule *amodule = image->aot_module;
	MonoMemoryManager *mem_manager = m_image_get_mem_manager (image);
	MonoMethod *method = NULL;
	MonoJitInfo *jinfo;
	guint8 *code, *ex_info, *p;
	guint32 *table;
	gpointer *methods;
	guint8 *code1, *code2;
	int methods_len;
	gboolean async;

	if (!amodule)
		return NULL;

	addr = MINI_FTNPTR_TO_ADDR (addr);

	if (!amodule_contains_code_addr (amodule, (guint8 *)addr))
		return NULL;

	async = mono_thread_info_is_async_context ();

	sort_methods (amodule);

	/* Binary search in the sorted_methods table */
	methods = amodule->sorted_methods;
	methods_len = amodule->sorted_methods_len;
	code = (guint8 *)addr;
	left = 0;
	right = methods_len;
	while (TRUE) {
		pos = (left + right) / 2;

		code1 = (guint8 *)methods [pos];
		if (pos + 1 == methods_len) {
#ifdef HOST_WASM
			code2 = code1 + 1;
#else
			if (code1 >= amodule->jit_code_start && code1 < amodule->jit_code_end)
				code2 = amodule->jit_code_end;
			else
				code2 = amodule->llvm_code_end;
#endif
		} else {
			code2 = (guint8 *)methods [pos + 1];
		}

		if (code < code1)
			right = pos;
		else if (code >= code2)
			left = pos + 1;
		else
			break;
	}

#ifdef HOST_WASM
	if (addr != methods [pos])
		return NULL;
#endif

	g_assert (addr >= methods [pos]);
	if (pos + 1 < methods_len)
		g_assert (addr < methods [pos + 1]);
	method_index = amodule->sorted_method_indexes [pos];

	/* In async mode, jinfo is not added to the normal jit info table, so have to cache it ourselves */
	if (async) {
		JitInfoMap **jinfo_table = amodule->async_jit_info_table;
		LOAD_ACQUIRE_FENCE;
		if (jinfo_table) {
			int buckets = (amodule->info.nmethods / JIT_INFO_MAP_BUCKET_SIZE) + 1;
			JitInfoMap *current_item = jinfo_table [method_index % buckets];
			LOAD_ACQUIRE_FENCE;
			while (current_item) {
				if (current_item->method_index == method_index)
					return current_item->jinfo;
				current_item = current_item->next;
				LOAD_ACQUIRE_FENCE;
			}
		}
	}

	code = (guint8 *)amodule->methods [method_index];
	ex_info = &amodule->blob [mono_aot_get_offset (amodule->ex_info_offsets, method_index)];

#ifdef HOST_WASM
	/* WASM methods have no length, can only look up the method address */
	code_len = 1;
#else
	if (pos == methods_len - 1) {
		if (code >= amodule->jit_code_start && code < amodule->jit_code_end)
			code_len = amodule->jit_code_end - code;
		else
			code_len = amodule->llvm_code_end - code;
	} else {
		guint8* code_end = (guint8*)methods [pos + 1];

		if (code >= amodule->jit_code_start && code < amodule->jit_code_end && code_end > amodule->jit_code_end) {
			code_end = amodule->jit_code_end;
		}

		if (code >= amodule->llvm_code_start && code < amodule->llvm_code_end && code_end > amodule->llvm_code_end) {
			code_end = amodule->llvm_code_end;
		}

		code_len = code_end - code;
	}
#endif

	g_assert ((guint8*)code <= (guint8*)addr && (guint8*)addr < (guint8*)code + code_len);

	/* Might be a wrapper/extra method */
	if (!async) {
		if (amodule->extra_methods) {
			amodule_lock (amodule);
			method = (MonoMethod *)g_hash_table_lookup (amodule->extra_methods, GUINT_TO_POINTER (method_index));
			amodule_unlock (amodule);
		} else {
			method = NULL;
		}

		if (!method) {
			if (method_index >= table_info_get_rows (&image->tables [MONO_TABLE_METHOD])) {
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

				p = amodule->blob + table [(pos * 2) + 1];
				method = decode_resolve_method_ref (amodule, p, &p, error);
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				if (!method)
					/* Happens when a random address is passed in which matches a not-yey called wrapper encoded using its name */
					return NULL;
			} else {
				token = mono_metadata_make_token (MONO_TABLE_METHOD, method_index + 1);
				method = mono_get_method_checked (image, token, NULL, NULL, error);
				if (!method)
					g_error ("AOT runtime could not load method due to %s", mono_error_get_message (error)); /* FIXME don't swallow the error */
				mono_error_cleanup (error);
			}
		}
		/* FIXME: */
		g_assert (method);
	}

	//printf ("F: %s\n", mono_method_full_name (method, TRUE));

	jinfo = decode_exception_debug_info (amodule, method, ex_info, code, code_len);

	g_assert ((guint8*)addr >= (guint8*)jinfo->code_start);

	if (async) {
		/* Add it to the async JitInfo tables */
		JitInfoMap **current_table, **new_table;
		JitInfoMap *current_item, *new_item;
		int buckets = (amodule->info.nmethods / JIT_INFO_MAP_BUCKET_SIZE) + 1;

		for (;;) {
			current_table = amodule->async_jit_info_table;
			LOAD_ACQUIRE_FENCE;
			if (current_table)
				break;

			new_table = alloc0_jit_info_data (mem_manager, buckets * sizeof (JitInfoMap*), async);
			STORE_RELEASE_FENCE;
			if (mono_atomic_cas_ptr ((volatile gpointer *)&amodule->async_jit_info_table, new_table, current_table) == current_table)
				break;
		}

		new_item = alloc0_jit_info_data (mem_manager, sizeof (JitInfoMap), async);
		new_item->method_index = method_index;
		new_item->jinfo = jinfo;

		for (;;) {
			current_item = amodule->async_jit_info_table [method_index % buckets];
			LOAD_ACQUIRE_FENCE;
			new_item->next = current_item;
			STORE_RELEASE_FENCE;
			if (mono_atomic_cas_ptr ((volatile gpointer *)&amodule->async_jit_info_table [method_index % buckets], new_item, current_item) == current_item)
				break;
		}
	} else {
		/* Add it to the normal JitInfo tables */
		mono_jit_info_table_add (jinfo);
	}

	if ((guint8*)addr >= (guint8*)jinfo->code_start + jinfo->code_size)
		/* addr is in the padding between methods, see the adjustment of code_size in decode_exception_debug_info () */
		return NULL;

	return jinfo;
}

static gboolean
decode_patch (MonoAotModule *aot_module, MonoMemPool *mp, MonoJumpInfo *ji, guint8 *buf, guint8 **endbuf)
{
	ERROR_DECL (error);
	guint8 *p = buf;
	gpointer *table;
	MonoImage *image;
	MonoMemoryManager *mem_manager = m_image_get_mem_manager (aot_module->assembly->image);

	switch (ji->type) {
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_METHOD_FTNDESC:
	case MONO_PATCH_INFO_ICALL_ADDR:
	case MONO_PATCH_INFO_ICALL_ADDR_CALL:
	case MONO_PATCH_INFO_METHOD_RGCTX:
	case MONO_PATCH_INFO_METHOD_CODE_SLOT:
	case MONO_PATCH_INFO_METHOD_PINVOKE_ADDR_CACHE:
	case MONO_PATCH_INFO_LLVMONLY_INTERP_ENTRY: {
		MethodRef ref;
		gboolean res;

		res = decode_method_ref (aot_module, &ref, p, &p, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */
		if (!res)
			goto cleanup;

		if (!ref.method && !mono_aot_only && !ref.no_aot_trampoline && (ji->type == MONO_PATCH_INFO_METHOD) && (mono_metadata_token_table (ref.token) == MONO_TABLE_METHOD)) {
			ji->data.target = mono_create_ftnptr (mono_create_jit_trampoline_from_token (ref.image, ref.token));
			ji->type = MONO_PATCH_INFO_ABS;
		}
		else {
			if (ref.method) {
				ji->data.method = ref.method;
			}else {
				ji->data.method = mono_get_method_checked (ref.image, ref.token, NULL, NULL, error);
				if (!ji->data.method)
					g_error ("AOT Runtime could not load method due to %s", mono_error_get_message (error)); /* FIXME don't swallow the error */
				mono_error_assert_ok (error);
			}
			g_assert (ji->data.method);
			mono_class_init_internal (ji->data.method->klass);
		}
		break;
	}
	case MONO_PATCH_INFO_LDSTR_LIT:
	{
		guint32 len = decode_value (p, &p);

		ji->data.name = (char*)p;
		p += len + 1;
		break;
	}
	case MONO_PATCH_INFO_METHODCONST:
		/* Shared */
		ji->data.method = decode_resolve_method_ref (aot_module, p, &p, error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (!ji->data.method)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
		/* Shared */
		ji->data.klass = decode_klass_ref (aot_module, p, &p, error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (!ji->data.klass)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		ji->data.del_tramp = (MonoDelegateClassMethodPair *)mono_mempool_alloc0 (mp, sizeof (MonoDelegateClassMethodPair));
		ji->data.del_tramp->klass = decode_klass_ref (aot_module, p, &p, error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (!ji->data.del_tramp->klass)
			goto cleanup;
		if (decode_value (p, &p)) {
			ji->data.del_tramp->method = decode_resolve_method_ref (aot_module, p, &p, error);
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			if (!ji->data.del_tramp->method)
				goto cleanup;
		}
		ji->data.del_tramp->is_virtual = decode_value (p, &p) ? TRUE : FALSE;
		break;
	case MONO_PATCH_INFO_IMAGE:
		ji->data.image = load_image (aot_module, decode_value (p, &p), error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
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
		ji->data.table = (MonoJumpInfoBBTable *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoBBTable));
		ji->data.table->table_size = decode_value (p, &p);
		table = (void **)mono_mem_manager_alloc (mem_manager, sizeof (gpointer) * ji->data.table->table_size);
		ji->data.table->table = (MonoBasicBlock**)table;
		for (int i = 0; i < ji->data.table->table_size; i++)
			table [i] = (gpointer)(gssize)decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_R4:
	case MONO_PATCH_INFO_R4_GOT: {
		guint32 val;

		ji->data.target = mono_mem_manager_alloc0 (mem_manager, sizeof (float));
		val = decode_value (p, &p);
		*(float*)ji->data.target = *(float*)&val;
		break;
	}
	case MONO_PATCH_INFO_R8:
	case MONO_PATCH_INFO_R8_GOT: {
		guint32 val [2];
		guint64 v;

		// FIXME: Align to 16 bytes ?
		ji->data.target = mono_mem_manager_alloc0 (mem_manager, sizeof (double));

		val [0] = decode_value (p, &p);
		val [1] = decode_value (p, &p);
		v = ((guint64)val [1] << 32) | ((guint64)val [0]);
		*(double*)ji->data.target = *(double*)&v;
		break;
	}
	case MONO_PATCH_INFO_LDSTR:
		image = load_image (aot_module, decode_value (p, &p), error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (!image)
			goto cleanup;
		ji->data.token = mono_jump_info_token_new (mp, image, MONO_TOKEN_STRING + decode_value (p, &p));
		break;
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_DECLSEC:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		/* Shared */
		image = load_image (aot_module, decode_value (p, &p), error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (!image)
			goto cleanup;
		ji->data.token = mono_jump_info_token_new (mp, image, decode_value (p, &p));

		ji->data.token->has_context = decode_value (p, &p);
		if (ji->data.token->has_context) {
			gboolean res = decode_generic_context (aot_module, &ji->data.token->context, p, &p, error);
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			if (!res)
				goto cleanup;
		}
		break;
	case MONO_PATCH_INFO_EXC_NAME:
		ji->data.klass = decode_klass_ref (aot_module, p, &p, error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (!ji->data.klass)
			goto cleanup;
		ji->data.name = m_class_get_name (ji->data.klass);
		break;
	case MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG:
	case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR:
	case MONO_PATCH_INFO_GC_NURSERY_START:
	case MONO_PATCH_INFO_GC_NURSERY_BITS:
	case MONO_PATCH_INFO_PROFILER_ALLOCATION_COUNT:
	case MONO_PATCH_INFO_PROFILER_CLAUSE_COUNT:
		break;
	case MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR:
		ji->data.uindex = decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_CASTCLASS_CACHE:
		ji->data.index = decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_JIT_ICALL_ID:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR_NOCALL:
		ji->data.jit_icall_id = (MonoJitICallId)decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_RGCTX_SLOT_INDEX: {
		gboolean res;
		MonoJumpInfoRgctxEntry *entry;
		guint32 offset, val;
		guint8 *p2;

		offset = decode_value (p, &p);
		val = decode_value (p, &p);

		entry = (MonoJumpInfoRgctxEntry *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoRgctxEntry));
		p2 = aot_module->blob + offset;
		entry->in_mrgctx = ((val & 1) > 0) ? TRUE : FALSE;
		if (entry->in_mrgctx)
			entry->d.method = decode_resolve_method_ref (aot_module, p2, &p2, error);
		else
			entry->d.klass = decode_klass_ref (aot_module, p2, &p2, error);
		entry->info_type = (MonoRgctxInfoType)((val >> 1) & 0xff);
		entry->data = (MonoJumpInfo *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));
		entry->data->type = (MonoJumpInfoType)((val >> 9) & 0xff);
		mono_error_cleanup (error); /* FIXME don't swallow the error */

		res = decode_patch (aot_module, mp, entry->data, p, &p);
		if (!res)
			goto cleanup;
		ji->data.rgctx_entry = entry;
		break;
	}
	case MONO_PATCH_INFO_SEQ_POINT_INFO:
	case MONO_PATCH_INFO_AOT_MODULE:
	case MONO_PATCH_INFO_MSCORLIB_GOT_ADDR:
		break;
	case MONO_PATCH_INFO_SIGNATURE:
	case MONO_PATCH_INFO_GSHAREDVT_IN_WRAPPER:
		ji->data.target = decode_signature (aot_module, p, &p);
		break;
	case MONO_PATCH_INFO_GSHAREDVT_CALL: {
		MonoJumpInfoGSharedVtCall *info = (MonoJumpInfoGSharedVtCall *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoGSharedVtCall));
		info->sig = decode_signature (aot_module, p, &p);
		g_assert (info->sig);
		info->method = decode_resolve_method_ref (aot_module, p, &p, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */

		ji->data.target = info;
		break;
	}
	case MONO_PATCH_INFO_GSHAREDVT_METHOD: {
		MonoGSharedVtMethodInfo *info = (MonoGSharedVtMethodInfo *)mono_mempool_alloc0 (mp, sizeof (MonoGSharedVtMethodInfo));

		info->method = decode_resolve_method_ref (aot_module, p, &p, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */

		info->num_entries = decode_value (p, &p);
		info->count_entries = info->num_entries;
		info->entries = (MonoRuntimeGenericContextInfoTemplate *)mono_mempool_alloc0 (mp, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->num_entries);
		for (int i = 0; i < info->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *template_ = &info->entries [i];

			template_->info_type = (MonoRgctxInfoType)decode_value (p, &p);
			switch (mini_rgctx_info_type_to_patch_info_type (template_->info_type)) {
			case MONO_PATCH_INFO_CLASS: {
				MonoClass *klass = decode_klass_ref (aot_module, p, &p, error);
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				if (!klass)
					goto cleanup;
				template_->data = m_class_get_byval_arg (klass);
				break;
			}
			case MONO_PATCH_INFO_FIELD:
				template_->data = decode_field_info (aot_module, p, &p);
				if (!template_->data)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_METHOD:
				template_->data = decode_resolve_method_ref (aot_module, p, &p, error);
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				if (!template_->data)
					goto cleanup;
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}
		ji->data.target = info;
		break;
	}
	case MONO_PATCH_INFO_GSHARED_METHOD_INFO: {
		MonoGSharedMethodInfo *info = (MonoGSharedMethodInfo *)mono_mempool_alloc0 (mp, sizeof (MonoGSharedMethodInfo));

		info->method = decode_resolve_method_ref (aot_module, p, &p, error);
		mono_error_assert_ok (error);

		info->num_entries = decode_value (p, &p);
		info->count_entries = info->num_entries;
		info->entries = (MonoRuntimeGenericContextInfoTemplate *)mono_mempool_alloc0 (mp, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->num_entries);
		for (int i = 0; i < info->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *entry = &info->entries [i];
			MonoJumpInfoType patch_type;

			entry->info_type = (MonoRgctxInfoType)decode_value (p, &p);
			patch_type = mini_rgctx_info_type_to_patch_info_type (entry->info_type);
			switch (patch_type) {
			case MONO_PATCH_INFO_CLASS: {
				MonoClass *klass = decode_klass_ref (aot_module, p, &p, error);
				mono_error_cleanup (error); /* FIXME don't swallow the error */
				if (!klass)
					goto cleanup;
				entry->data = m_class_get_byval_arg (klass);
				break;
			}
			case MONO_PATCH_INFO_FIELD:
				entry->data = decode_field_info (aot_module, p, &p);
				if (!entry->data)
					goto cleanup;
				break;
			case MONO_PATCH_INFO_METHOD:
				entry->data = decode_resolve_method_ref (aot_module, p, &p, error);
				mono_error_assert_ok (error);
				break;
			case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
			case MONO_PATCH_INFO_VIRT_METHOD:
			case MONO_PATCH_INFO_GSHAREDVT_METHOD:
			case MONO_PATCH_INFO_GSHAREDVT_CALL: {
				MonoJumpInfo tmp;
				tmp.type = patch_type;
				if (!decode_patch (aot_module, mp, &tmp, p, &p))
					goto cleanup;
				entry->data = (gpointer)tmp.data.target;
				break;
			}
			default:
				g_assert_not_reached ();
				break;
			}
		}
		ji->data.target = info;
		break;
	}
	case MONO_PATCH_INFO_VIRT_METHOD: {
		MonoJumpInfoVirtMethod *info = (MonoJumpInfoVirtMethod *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoVirtMethod));

		info->klass = decode_klass_ref (aot_module, p, &p, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */

		info->method = decode_resolve_method_ref (aot_module, p, &p, error);
		mono_error_assert_ok (error); /* FIXME don't swallow the error */

		ji->data.target = info;
		break;
	}
	case MONO_PATCH_INFO_GC_SAFE_POINT_FLAG:
	case MONO_PATCH_INFO_SPECIFIC_TRAMPOLINES:
	case MONO_PATCH_INFO_SPECIFIC_TRAMPOLINES_GOT_SLOTS_BASE:
		break;
	case MONO_PATCH_INFO_AOT_JIT_INFO:
		ji->data.index = decode_value (p, &p);
		break;
	default:
		g_error ("unhandled type %d", ji->type);
		break;
	}

	*endbuf = p;

	return TRUE;

 cleanup:
	return FALSE;
}

/*
 * decode_patches:
 *
 *    Decode a list of patches identified by the got offsets in GOT_OFFSETS. Return an array of
 * MonoJumpInfo structures allocated from MP. GOT entries already loaded have their
 * ji->type set to MONO_PATCH_INFO_NONE.
 */
static MonoJumpInfo*
decode_patches (MonoAotModule *amodule, MonoMemPool *mp, int n_patches, gboolean llvm, guint32 *got_offsets)
{
	MonoJumpInfo *patches;
	MonoJumpInfo *ji;
	gpointer *got;
	guint32 *got_info_offsets;
	int i;
	gboolean res;

	if (llvm) {
		got = amodule->llvm_got;
		got_info_offsets = (guint32 *)amodule->llvm_got_info_offsets;
	} else {
		got = amodule->got;
		got_info_offsets = (guint32 *)amodule->got_info_offsets;
	}

	patches = (MonoJumpInfo *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo) * n_patches);
	for (i = 0; i < n_patches; ++i) {
		guint8 *p = amodule->blob + mono_aot_get_offset (got_info_offsets, got_offsets [i]);

		ji = &patches [i];
		ji->type = (MonoJumpInfoType)decode_value (p, &p);

		/* See load_method () for SFLDA */
		if (got && got [got_offsets [i]] && ji->type != MONO_PATCH_INFO_SFLDA) {
			/* Already loaded */
			ji->type = MONO_PATCH_INFO_NONE;
		} else {
			res = decode_patch (amodule, mp, ji, p, &p);
			if (!res)
				return NULL;
		}
	}

	return patches;
}

static MonoJumpInfo*
load_patch_info (MonoAotModule *amodule, MonoMemPool *mp, int n_patches,
				 gboolean llvm, guint32 **got_slots,
				 guint8 *buf, guint8 **endbuf)
{
	MonoJumpInfo *patches;
	int pindex;
	guint8 *p;

	p = buf;

	*got_slots = (guint32 *)g_malloc (sizeof (guint32) * n_patches);
	for (pindex = 0; pindex < n_patches; ++pindex) {
		(*got_slots)[pindex] = decode_value (p, &p);
	}

	patches = decode_patches (amodule, mp, n_patches, llvm, *got_slots);
	if (!patches) {
		g_free (*got_slots);
		*got_slots = NULL;
		return NULL;
	}

	*endbuf = p;
	return patches;
}

static void
register_jump_target_got_slot (MonoMethod *method, gpointer *got_slot)
{
	/*
	 * Jump addresses cannot be patched by the trampoline code since it
	 * does not have access to the caller's address. Instead, we collect
	 * the addresses of the GOT slots pointing to a method, and patch
	 * them after the method has been compiled.
	 */
	GSList *list;
	MonoJitMemoryManager *jit_mm;
	MonoMethod *shared_method = mini_method_to_shared (method);
	method = shared_method ? shared_method : method;

	jit_mm = jit_mm_for_method (method);
	jit_mm_lock (jit_mm);
	if (!jit_mm->jump_target_got_slot_hash)
		jit_mm->jump_target_got_slot_hash = g_hash_table_new (NULL, NULL);
	list = (GSList *)g_hash_table_lookup (jit_mm->jump_target_got_slot_hash, method);
	list = g_slist_prepend (list, got_slot);
	g_hash_table_insert (jit_mm->jump_target_got_slot_hash, method, list);
	jit_mm_unlock (jit_mm);
}

/*
 * load_method:
 *
 *   Load the method identified by METHOD_INDEX from the AOT image. Return a
 * pointer to the native code of the method, or NULL if not found.
 * METHOD might not be set if the caller only has the image/token info.
 */
static gpointer
load_method (MonoAotModule *amodule, MonoImage *image, MonoMethod *method, guint32 token, int method_index,
			 MonoError *error)
{
	MonoJitInfo *jinfo = NULL;
	guint8 *code = NULL, *info;
	gboolean res;

	error_init (error);

	init_amodule_got (amodule, FALSE);

	if (amodule->out_of_date)
		return NULL;

	if (amodule->info.llvm_get_method) {
		/*
		 * Obtain the method address by calling a generated function in the LLVM module.
		 */
		gpointer (*get_method) (int) = (gpointer (*)(int))amodule->info.llvm_get_method;
		code = (guint8 *)get_method (method_index);
	}

	if (!code) {
		if (method_index < amodule->info.nmethods)
			code = (guint8*)MINI_ADDR_TO_FTNPTR ((guint8 *)amodule->methods [method_index]);
		else
			return NULL;

		/* JITted method */
		if (amodule->methods [method_index] == GINT_TO_POINTER (-1)) {
			if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
				char *full_name;

				if (!method) {
					method = mono_get_method_checked (image, token, NULL, NULL, error);
					if (!method)
						return NULL;
				}
				if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)) {
					full_name = mono_method_full_name (method, TRUE);
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: NOT FOUND: %s.", full_name);
					g_free (full_name);
				}
			}
			return NULL;
		}
	}

	info = &amodule->blob [mono_aot_get_offset (amodule->method_info_offsets, method_index)];

	if (!amodule->methods_loaded) {
		amodule_lock (amodule);
		if (!amodule->methods_loaded) {
			guint32 *loaded;

			loaded = g_new0 (guint32, amodule->info.nmethods / 32 + 1);
			mono_memory_barrier ();
			amodule->methods_loaded = loaded;
		}
		amodule_unlock (amodule);
	}

	if ((amodule->methods_loaded [method_index / 32] >> (method_index % 32)) & 0x1)
		return code;

	if (mini_debug_options.aot_skip_set && !(method && method->wrapper_type)) {
		gint32 methods_aot = mono_atomic_load_i32 (&mono_jit_stats.methods_aot);
		methods_aot += mono_atomic_load_i32 (&mono_jit_stats.methods_aot_llvm);
		if (methods_aot == mini_debug_options.aot_skip) {
			if (!method) {
				method = mono_get_method_checked (image, token, NULL, NULL, error);
				if (!method)
					return NULL;
			}
			if (method) {
				char *name = mono_method_full_name (method, TRUE);
				g_print ("NON AOT METHOD: %s.\n", name);
				g_free (name);
			} else {
				g_print ("NON AOT METHOD: %p %d\n", code, method_index);
			}
			mini_debug_options.aot_skip_set = FALSE;
			return NULL;
		}
	}

	if (mono_last_aot_method != -1) {
		gint32 methods_aot = mono_atomic_load_i32 (&mono_jit_stats.methods_aot);
		methods_aot += mono_atomic_load_i32 (&mono_jit_stats.methods_aot_llvm);
		if (methods_aot >= mono_last_aot_method)
			return NULL;
		else if (methods_aot == mono_last_aot_method - 1) {
			if (!method) {
				method = mono_get_method_checked (image, token, NULL, NULL, error);
				if (!method)
					return NULL;
			}
			if (method) {
				char *name = mono_method_full_name (method, TRUE);
				g_print ("LAST AOT METHOD: %s.\n", name);
				g_free (name);
			} else {
				g_print ("LAST AOT METHOD: %p %d\n", code, method_index);
			}
		}
	}

	if (!(is_llvm_code (amodule, code) && (amodule->info.flags & MONO_AOT_FILE_FLAG_LLVM_ONLY)) ||
		(mono_llvm_only && method && method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED)) {
		/* offset == 0 means its llvm code */
		if (mono_aot_get_offset (amodule->method_info_offsets, method_index) != 0) {
			res = init_method (amodule, NULL, method_index, method, NULL, error);
			if (!res)
				goto cleanup;
		}
	}

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
		char *full_name;

		if (!method) {
			method = mono_get_method_checked (image, token, NULL, NULL, error);
			if (!method)
				return NULL;
		}

		full_name = mono_method_full_name (method, TRUE);

		if (!jinfo)
			jinfo = mono_aot_find_jit_info (amodule->assembly->image, code);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: FOUND method %s [%p - %p %p]", full_name, code, code + jinfo->code_size, info);
		g_free (full_name);
	}

	if (mono_llvm_only) {
		guint8 flags = amodule->method_flags_table [method_index];
		/* The caller needs to looks this up, but its hard to do without constructing the full MonoJitInfo, so save it here */
		if (flags & (MONO_AOT_METHOD_FLAG_GSHAREDVT_VARIABLE | MONO_AOT_METHOD_FLAG_INTERP_ENTRY_ONLY)) {
			mono_aot_lock ();
			if (!code_to_method_flags)
				code_to_method_flags = g_hash_table_new (NULL, NULL);
			g_hash_table_insert (code_to_method_flags, code, GUINT_TO_POINTER (flags));
			mono_aot_unlock ();
		}
	}

	init_plt (amodule);

	amodule_lock (amodule);

	if (is_llvm_code (amodule, code))
		mono_atomic_inc_i32 (&mono_jit_stats.methods_aot_llvm);
	else
		mono_atomic_inc_i32 (&mono_jit_stats.methods_aot);

	if (method && method->wrapper_type)
		g_hash_table_insert (amodule->method_to_code, method, code);

	/* Commit changes since methods_loaded is accessed outside the lock */
	mono_memory_barrier ();

	amodule->methods_loaded [method_index / 32] |= 1 << (method_index % 32);

	amodule_unlock (amodule);

	if (MONO_PROFILER_ENABLED (jit_begin) || MONO_PROFILER_ENABLED (jit_done)) {
		if (!method) {
			method = mono_get_method_checked (amodule->assembly->image, token, NULL, NULL, error);
			if (!method)
				return NULL;
		}
		MONO_PROFILER_RAISE (jit_begin, (method));
		jinfo = mono_jit_info_table_find_internal (code, TRUE, FALSE);
		g_assert (jinfo);
		MONO_PROFILER_RAISE (jit_done, (method, jinfo));
		jinfo = NULL;
	}

	return code;

 cleanup:
	if (jinfo)
		g_free (jinfo);

	return NULL;
}

/** find_aot_method_in_amodule
	*
	* \param code_amodule The AOT module containing the code pointer
	* \param method The method to find the code index for
	* \param hash_full The hash for the method
	*/
static guint32
find_aot_method_in_amodule (MonoAotModule *code_amodule, MonoMethod *method, guint32 hash_full)
{
	ERROR_DECL (error);
	guint32 table_size, entry_size, hash;
	guint32 *table, *entry;
	guint32 index;
	static guint32 n_extra_decodes;

	// The AOT module containing the MonoMethod
	// The reference to the metadata amodule will differ among multiple dedup methods
	// which mangle to the same name but live in different assemblies. This leads to
	// the caching breaking. The solution seems to be to cache using the "metadata" amodule.
	MonoAotModule *metadata_amodule = m_class_get_image (method->klass)->aot_module;

	if (!metadata_amodule || metadata_amodule->out_of_date || !code_amodule || code_amodule->out_of_date)
		return 0xffffff;

	table_size = code_amodule->extra_method_table [0];
	hash = hash_full % table_size;
	table = code_amodule->extra_method_table + 1;
	entry_size = 3;

	entry = &table [hash * entry_size];

	if (entry [0] == 0)
		return 0xffffff;

	index = 0xffffff;
	while (TRUE) {
		guint32 key = entry [0];
		guint32 value = entry [1];
		guint32 next = entry [entry_size - 1];
		MonoMethod *m;
		guint8 *p, *orig_p;

		p = code_amodule->blob + key;
		orig_p = p;

		amodule_lock (metadata_amodule);
		if (!metadata_amodule->method_ref_to_method)
			metadata_amodule->method_ref_to_method = g_hash_table_new (NULL, NULL);
		m = (MonoMethod *)g_hash_table_lookup (metadata_amodule->method_ref_to_method, p);
		amodule_unlock (metadata_amodule);
		if (!m) {
			m = decode_resolve_method_ref_with_target (code_amodule, method, p, &p, error);
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			/*
			 * Can't catche runtime invoke wrappers since it would break
			 * the check in decode_method_ref_with_target ().
			 */
			if (m && m->wrapper_type != MONO_WRAPPER_RUNTIME_INVOKE) {
				amodule_lock (metadata_amodule);
				g_hash_table_insert (metadata_amodule->method_ref_to_method, orig_p, m);
				amodule_unlock (metadata_amodule);
			}
		}
		if (m == method) {
			index = value;
			break;
		}

		/* Methods decoded needlessly */
		if (m) {
			//printf ("%d %s %s %p\n", n_extra_decodes, mono_method_full_name (method, TRUE), mono_method_full_name (m, TRUE), orig_p);
			n_extra_decodes ++;
		}

		if (next != 0)
			entry = &table [next * entry_size];
		else
			break;
	}

	if (index != 0xffffff)
		g_assert (index < code_amodule->info.nmethods);

	return index;
}

static void
add_module_cb (gpointer key, gpointer value, gpointer user_data)
{
	g_ptr_array_add ((GPtrArray*)user_data, value);
}

static gboolean
inst_is_private (MonoGenericInst *inst)
{
	for (guint i = 0; i < inst->type_argc; ++i) {
		MonoType *t = inst->type_argv [i];
		if ((t->type == MONO_TYPE_CLASS || t->type == MONO_TYPE_VALUETYPE)) {
			int access_level = mono_class_get_flags (t->data.klass) & TYPE_ATTRIBUTE_VISIBILITY_MASK;
			if (access_level == TYPE_ATTRIBUTE_NESTED_PRIVATE || access_level == TYPE_ATTRIBUTE_NOT_PUBLIC)
				return TRUE;
		}
	}
	return FALSE;
}

gboolean
mono_aot_can_dedup (MonoMethod *method)
{
#ifdef TARGET_WASM
	/* Use a set of wrappers/instances which work and useful */
	switch (method->wrapper_type) {
	case MONO_WRAPPER_RUNTIME_INVOKE:
		return TRUE;
		break;
	case MONO_WRAPPER_OTHER: {
		WrapperInfo *info = mono_marshal_get_wrapper_info (method);

		if (info->subtype == WRAPPER_SUBTYPE_PTR_TO_STRUCTURE ||
			info->subtype == WRAPPER_SUBTYPE_STRUCTURE_TO_PTR ||
			info->subtype == WRAPPER_SUBTYPE_INTERP_LMF ||
			info->subtype == WRAPPER_SUBTYPE_AOT_INIT)
			return FALSE;
#if 0
		// See is_linkonce_method () in mini-llvm.c
		if (info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG || info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_OUT_SIG)
			/* Handled using linkonce */
			return FALSE;
#endif
		return TRUE;
	}
	default:
		break;
	}

	if (method->is_inflated && !mono_method_is_generic_sharable_full (method, TRUE, FALSE, FALSE) &&
		!mini_is_gsharedvt_signature (mono_method_signature_internal (method)) &&
		!mini_is_gsharedvt_klass (method->klass)) {
		MonoGenericContext *context = mono_method_get_context (method);
		if (context->method_inst && mini_is_gsharedvt_inst (context->method_inst))
			return FALSE;
		/* No point in dedup-ing private instances */
		if ((context->class_inst && inst_is_private (context->class_inst)) ||
			(context->method_inst && inst_is_private (context->method_inst)))
			return FALSE;
		return TRUE;
	}
	return FALSE;
#else
	gboolean not_normal_gshared = method->is_inflated && !mono_method_is_generic_sharable_full (method, TRUE, FALSE, FALSE);
	gboolean extra_method = (method->wrapper_type != MONO_WRAPPER_NONE) || not_normal_gshared;

	return extra_method;
#endif
}


/*
 * find_aot_method:
 *
 *   Try finding METHOD in the extra_method table in all AOT images.
 * Return its method index, or 0xffffff if not found. Set OUT_AMODULE to the AOT
 * module where the method was found.
 */
static guint32
find_aot_method (MonoMethod *method, MonoAotModule **out_amodule)
{
	guint32 index;
	GPtrArray *modules;
	int i;
	guint32 hash = mono_aot_method_hash (method);

	/* Try the place we expect to have moved the method only
	 * We don't probe, as that causes hard-to-debug issues when we fail
	 * to find the method */
	if (container_amodule && mono_aot_can_dedup (method)) {
		*out_amodule = container_amodule;
		index = find_aot_method_in_amodule (container_amodule, method, hash);
		return index;
	}

	/* Try the method's module first */
	*out_amodule = m_class_get_image (method->klass)->aot_module;
	index = find_aot_method_in_amodule (m_class_get_image (method->klass)->aot_module, method, hash);
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
		MonoAotModule *amodule = (MonoAotModule *)g_ptr_array_index (modules, i);

		if (amodule != m_class_get_image (method->klass)->aot_module)
			index = find_aot_method_in_amodule (amodule, method, hash);
		if (index != 0xffffff) {
			*out_amodule = amodule;
			break;
		}
	}

	g_ptr_array_free (modules, TRUE);

	return index;
}

guint32
mono_aot_find_method_index (MonoMethod *method)
{
	MonoAotModule *out_amodule;
	return find_aot_method (method, &out_amodule);
}

static gboolean
init_method (MonoAotModule *amodule, gpointer info, guint32 method_index, MonoMethod *method, MonoClass *init_class, MonoError *error)
{
	MonoMemPool *mp;
	MonoClass *klass_to_run_ctor = NULL;
	gboolean from_plt = method == NULL;
	int pindex, n_patches;
	guint8 *p;
	MonoJitInfo *jinfo = NULL;
	guint8 *code;
	MonoGenericContext *context;
	MonoGenericContext ctx;

	/* Might be needed if the method is externally called */
	init_plt (amodule);
	init_amodule_got (amodule, FALSE);

	memset (&ctx, 0, sizeof (ctx));

	error_init (error);

	if (!info)
		info = &amodule->blob [mono_aot_get_offset (amodule->method_info_offsets, method_index)];

	p = (guint8*)info;

	// FIXME: Is this aligned ?
	guint32 encoded_method_index = *(guint32*)p;
	if (method_index)
		g_assert (method_index == encoded_method_index);
	method_index = encoded_method_index;
	p += 4;

	code = (guint8 *)amodule->methods [method_index];
	guint8 flags = amodule->method_flags_table [method_index];

	if (flags & MONO_AOT_METHOD_FLAG_HAS_CCTOR)
		klass_to_run_ctor = decode_klass_ref (amodule, p, &p, error);
	if (!is_ok (error))
		return FALSE;

	//FIXME old code would use the class from @method if not null and ignore the one encoded. I don't know if we need to honor that -- @kumpera
	if (method)
		klass_to_run_ctor = method->klass;

	context = NULL;
	if (flags & MONO_AOT_METHOD_FLAG_HAS_CTX) {
		decode_generic_context (amodule, &ctx, p, &p, error);
		mono_error_assert_ok (error);
		context = &ctx;
	}

	if (flags & MONO_AOT_METHOD_FLAG_HAS_PATCHES)
		n_patches = decode_value (p, &p);
	else
		n_patches = 0;

	if (n_patches) {
		MonoJumpInfo *patches;
		guint32 *got_slots;
		gboolean llvm;
		gpointer *got;

		mp = mono_mempool_new ();

		if ((gpointer)code >= amodule->info.jit_code_start && (gpointer)code <= amodule->info.jit_code_end) {
			llvm = FALSE;
			got = amodule->got;
		} else {
			llvm = TRUE;
			got = amodule->llvm_got;
			g_assert (got);
		}

		patches = load_patch_info (amodule, mp, n_patches, llvm, &got_slots, p, &p);
		if (patches == NULL) {
			mono_mempool_destroy (mp);
			goto cleanup;
		}

		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];
			gpointer addr;

			/*
			 * For SFLDA, we need to call resolve_patch_target () since the GOT slot could have
			 * been initialized by load_method () for a static cctor before the cctor has
			 * finished executing (#23242).
			 */
			if (ji->type == MONO_PATCH_INFO_NONE) {
			} else if (!got [got_slots [pindex]] || ji->type == MONO_PATCH_INFO_SFLDA) {
				/* In llvm-only made, we might encounter shared methods */
				if (mono_llvm_only && ji->type == MONO_PATCH_INFO_METHOD && mono_method_check_context_used (ji->data.method)) {
					g_assert (context);
					ji->data.method = mono_class_inflate_generic_method_checked (ji->data.method, context, error);
					if (!is_ok (error)) {
						g_free (got_slots);
						mono_mempool_destroy (mp);
						return FALSE;
					}
				}
				/* This cannot be resolved in mono_resolve_patch_target () */
				if (ji->type == MONO_PATCH_INFO_AOT_JIT_INFO) {
					// FIXME: Lookup using the index
					jinfo = mono_aot_find_jit_info (amodule->assembly->image, code);
					ji->type = MONO_PATCH_INFO_ABS;
					ji->data.target = jinfo;
				}
				addr = mono_resolve_patch_target (method, code, ji, TRUE, error);
				if (!is_ok (error)) {
					g_free (got_slots);
					mono_mempool_destroy (mp);
					return FALSE;
				}
				if (ji->type == MONO_PATCH_INFO_METHOD_JUMP)
					addr = mono_create_ftnptr (addr);
				mono_memory_barrier ();
				got [got_slots [pindex]] = addr;
				if (ji->type == MONO_PATCH_INFO_METHOD_JUMP)
					register_jump_target_got_slot (ji->data.method, &(got [got_slots [pindex]]));

				if (llvm) {
					void (*init_aotconst) (int, gpointer) = (void (*)(int, gpointer))amodule->info.llvm_init_aotconst;
					init_aotconst (got_slots [pindex], addr);
				}
			}
			ji->type = MONO_PATCH_INFO_NONE;
		}

		g_free (got_slots);

		mono_mempool_destroy (mp);
	}

	if (mini_debug_options.load_aot_jit_info_eagerly)
		jinfo = mono_aot_find_jit_info (amodule->assembly->image, code);

	gboolean inited_ok;
	inited_ok = TRUE;
	if (init_class) {
		MonoVTable *vt = mono_class_vtable_checked (init_class, error);
		if (!is_ok (error))
			inited_ok = FALSE;
		else
			inited_ok = mono_runtime_class_init_full (vt, error);
	} else if (from_plt && klass_to_run_ctor && !mono_class_is_gtd (klass_to_run_ctor)) {
		MonoVTable *vt = mono_class_vtable_checked (klass_to_run_ctor, error);
		if (!is_ok (error))
			inited_ok = FALSE;
		else
			inited_ok = mono_runtime_class_init_full (vt, error);
	}
	if (!inited_ok)
		return FALSE;

	return TRUE;

 cleanup:
	if (jinfo)
		g_free (jinfo);

	return FALSE;
}

/*
 * mono_aot_init_llvm_method:
 *
 *   Initialize the LLVM method identified by METHOD_INFO.
 */
gboolean
mono_aot_init_llvm_method (gpointer aot_module, gpointer method_info, MonoClass *init_class, MonoError *error)
{
	MonoAotModule *amodule = (MonoAotModule*)aot_module;

	return init_method (amodule, method_info, 0, NULL, init_class, error);
}

/*
 * mono_aot_get_method:
 *
 *   Return a pointer to the AOTed native code for METHOD if it can be found,
 * NULL otherwise.
 * On platforms with function pointers, this doesn't return a function pointer.
 */
gpointer
mono_aot_get_method (MonoMethod *method, MonoError *error)
{
	MonoClass *klass = method->klass;
	MonoMethod *orig_method = method;
	guint32 method_index;
	MonoAotModule *amodule = m_class_get_image (klass)->aot_module;
	guint8 *code;
	gboolean cache_result = FALSE;
	ERROR_DECL (inner_error);

	error_init (error);

	if (!amodule)
		return NULL;

	if (amodule->out_of_date)
		return NULL;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return NULL;

	/* Load the dedup module lazily */
	load_container_amodule (mono_assembly_get_alc (amodule->assembly));

	g_assert (m_class_is_inited (klass));

	/* Find method index */
	method_index = 0xffffff;

	gboolean dedupable = mono_aot_can_dedup (method);

	if (method->is_inflated && !method->wrapper_type && mono_method_is_generic_sharable_full (method, TRUE, FALSE, FALSE) && !dedupable) {
		MonoMethod *generic_orig_method = method;
		/*
		 * For generic methods, we store the fully shared instance in place of the
		 * original method.
		 */
		method = mono_method_get_declaring_generic_method (method);
		method_index = mono_metadata_token_index (method->token) - 1;

		if (amodule->info.flags & MONO_AOT_FILE_FLAG_WITH_LLVM) {
			/* Needed by mini_llvm_init_gshared_method_this () */
			/* generic_orig_method is a random instance but it is enough to make init_method () work */
			amodule_lock (amodule);
			g_hash_table_insert (amodule->extra_methods, GUINT_TO_POINTER (method_index), generic_orig_method);
			amodule_unlock (amodule);
		}
	}

	if (method_index == 0xffffff && (method->is_inflated || !method->token)) {
		/* This hash table is used to avoid the slower search in the extra_method_table in the AOT image */
		amodule_lock (amodule);
		code = (guint8 *)g_hash_table_lookup (amodule->method_to_code, method);
		amodule_unlock (amodule);
		if (code)
			return code;

		cache_result = TRUE;
		if (method_index == 0xffffff)
			method_index = find_aot_method (method, &amodule);

		/*
		 * Special case the ICollection<T> wrappers for arrays, as they cannot
		 * be statically enumerated, and each wrapper ends up calling the same
		 * method in Array.
		 */
		if (method_index == 0xffffff && method->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED && m_class_get_rank (method->klass) && strstr (method->name, "System.Collections.Generic")) {
			MonoMethod *m = mono_aot_get_array_helper_from_wrapper (method);

			code = (guint8 *)mono_aot_get_method (m, inner_error);
			mono_error_cleanup (inner_error);
			if (code)
				return code;
		}

		/*
		 * Special case Array.GetGenericValue_icall which is a generic icall.
		 * Generic sharing currently can't handle it, but the icall returns data using
		 * an out parameter, so the managed-to-native wrappers can share the same code.
		 */
		if (method_index == 0xffffff && method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE && method->klass == mono_defaults.array_class && !strcmp (method->name, "GetGenericValue_icall")) {
			MonoMethod *m;
			MonoGenericContext ctx;

			if (mono_method_signature_internal (method)->params [1]->type == MONO_TYPE_OBJECT)
				/* Avoid recursion */
				return NULL;

			m = mono_class_get_method_from_name_checked (mono_defaults.array_class, "GetGenericValue_icall", 3, 0, error);
			mono_error_assert_ok (error);
			g_assert (m);

			memset (&ctx, 0, sizeof (ctx));
			MonoType *args [ ] = { m_class_get_byval_arg (mono_defaults.object_class) };
			ctx.method_inst = mono_metadata_get_generic_inst (1, args);

			m = mono_marshal_get_native_wrapper (mono_class_inflate_generic_method_checked (m, &ctx, error), TRUE, TRUE);
			if (!m)
				g_error ("AOT runtime could not load method due to %s", mono_error_get_message (error)); /* FIXME don't swallow the error */

			/*
			 * Get the code for the <object> instantiation which should be emitted into
			 * the mscorlib aot image by the AOT compiler.
			 */
			code = (guint8 *)mono_aot_get_method (m, inner_error);
			mono_error_cleanup (inner_error);
			if (code)
				return code;
		}

		/* For ARRAY_ACCESSOR wrappers with reference types, use the <object> instantiation saved in corlib */
		if (method_index == 0xffffff && method->wrapper_type == MONO_WRAPPER_OTHER) {
			WrapperInfo *info = mono_marshal_get_wrapper_info (method);

			if (info->subtype == WRAPPER_SUBTYPE_ARRAY_ACCESSOR) {
				MonoMethod *array_method = info->d.array_accessor.method;
				if (MONO_TYPE_IS_REFERENCE (m_class_get_byval_arg (m_class_get_element_class (array_method->klass)))) {
					int rank;

					if (!strcmp (array_method->name, "Set"))
						rank = mono_method_signature_internal (array_method)->param_count - 1;
					else if (!strcmp (array_method->name, "Get") || !strcmp (array_method->name, "Address"))
						rank = mono_method_signature_internal (array_method)->param_count;
					else
						g_assert_not_reached ();
					MonoClass *obj_array_class = mono_class_create_array (mono_defaults.object_class, rank);
					MonoMethod *m = mono_class_get_method_from_name_checked (obj_array_class, array_method->name, mono_method_signature_internal (array_method)->param_count, 0, error);
					mono_error_assert_ok (error);
					g_assert (m);

					m = mono_marshal_get_array_accessor_wrapper (m);
					if (m != method) {
						code = (guint8 *)mono_aot_get_method (m, inner_error);
						mono_error_cleanup (inner_error);
						if (code)
							return code;
					}
				}
			}
		}

		if (method_index == 0xffffff && method->is_inflated && mono_method_is_generic_sharable_full (method, FALSE, TRUE, FALSE)) {
			/* Partial sharing */
			MonoMethod *shared;

			shared = mini_get_shared_method_full (method, SHARE_MODE_NONE, error);
			return_val_if_nok (error, NULL);

			method_index = find_aot_method (shared, &amodule);
			if (method_index != 0xffffff)
				method = shared;
		}

		if (method_index == 0xffffff && method->is_inflated && mono_method_is_generic_sharable_full (method, FALSE, FALSE, TRUE)) {
			MonoMethod *shared;
			/* gsharedvt */
			/* Use the all-vt shared method since this is what was AOTed */
			shared = mini_get_shared_method_full (method, SHARE_MODE_GSHAREDVT, error);
			if (!shared)
				return NULL;

			method_index = find_aot_method (shared, &amodule);
			if (method_index != 0xffffff) {
				method = mini_get_shared_method_full (method, SHARE_MODE_GSHAREDVT, error);
				if (!method)
					return NULL;
			}
		}

		if (method_index == 0xffffff) {
			if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
				char *full_name;

				full_name = mono_method_full_name (method, TRUE);
				mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT NOT FOUND: %s.", full_name);
				g_free (full_name);
			}
			return NULL;
		}

		if (method_index == 0xffffff)
			return NULL;

		/* Needed by find_jit_info */
		amodule_lock (amodule);
		g_hash_table_insert (amodule->extra_methods, GUINT_TO_POINTER (method_index), method);
		amodule_unlock (amodule);
	} else {
		/* Common case */
		method_index = mono_metadata_token_index (method->token) - 1;

		if (!mono_llvm_only) {
			guint32 num_methods = amodule->info.nmethods - amodule->info.nextra_methods;
			if (method_index >= num_methods)
				/* method not available in AOT image */
				return NULL;
		}
	}

	code = (guint8 *)load_method (amodule, m_class_get_image (klass), method, method->token, method_index, error);
	if (!is_ok (error))
		return NULL;
	if (code && cache_result) {
		amodule_lock (amodule);
		g_hash_table_insert (amodule->method_to_code, orig_method, code);
		amodule_unlock (amodule);
	}
	return code;
}

/**
 * Same as mono_aot_get_method, but we try to avoid loading any metadata from the
 * method.
 */
gpointer
mono_aot_get_method_from_token (MonoImage *image, guint32 token, MonoError *error)
{
	MonoAotModule *aot_module = image->aot_module;
	int method_index;
	gpointer res;

	error_init (error);

	if (!aot_module)
		return NULL;

	method_index = mono_metadata_token_index (token) - 1;

	res = load_method (aot_module, image, NULL, token, method_index, error);
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

	if (amodule_contains_code_addr (aot_module, data->addr))
		data->module = aot_module;
}

static MonoAotModule*
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

#ifdef MONO_ARCH_CODE_EXEC_ONLY
static guint32
aot_resolve_plt_info_offset (gpointer amodule, guint32 plt_entry_index)
{
	MonoAotModule *module = (MonoAotModule*)amodule;
	return mono_aot_get_offset (module->got_info_offsets, module->info.plt_got_info_offset_base + plt_entry_index);
}
#endif

void
mono_aot_patch_plt_entry (gpointer aot_module, guint8 *code, guint8 *plt_entry, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
	MonoAotModule *amodule = (MonoAotModule *)aot_module;

	if (!amodule)
		amodule = find_aot_module (code);
#ifdef MONO_ARCH_CODE_EXEC_ONLY
	mono_arch_patch_plt_entry_exec_only (&amodule->info, plt_entry, amodule->got, regs, addr);
#else
	mono_arch_patch_plt_entry (plt_entry, amodule->got, regs, addr);
#endif
}

/*
 * mono_aot_plt_resolve:
 *
 *   This function is called by the entries in the PLT to resolve the actual method that
 * needs to be called. It returns a trampoline to the method and patches the PLT entry.
 * Returns NULL if the something cannot be loaded.
 */
gpointer
mono_aot_plt_resolve (gpointer aot_module, host_mgreg_t *regs, guint8 *code, MonoError *error)
{
#ifdef MONO_ARCH_AOT_SUPPORTED
	guint8 *p, *target, *plt_entry;
	guint32 plt_info_offset;
	MonoJumpInfo ji;
	MonoAotModule *module = (MonoAotModule*)aot_module;
	gboolean res, no_ftnptr = FALSE;
	MonoMemPool *mp;
	gboolean using_gsharedvt = FALSE;

	error_init (error);

	plt_entry = mono_aot_get_plt_entry (regs, code);
	g_assert (plt_entry);

	plt_info_offset = mono_aot_get_plt_info_offset (aot_module, plt_entry, regs, code);

	//printf ("DYN: %p %d\n", aot_module, plt_info_offset);

	p = &module->blob [plt_info_offset];

	ji.type = (MonoJumpInfoType)decode_value (p, &p);

	mp = mono_mempool_new ();
	res = decode_patch (module, mp, &ji, p, &p);

	if (!res) {
		mono_mempool_destroy (mp);
		return NULL;
	}

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED
	using_gsharedvt = TRUE;
#endif

	/*
	 * Avoid calling resolve_patch_target in the full-aot case if possible, since
	 * it would create a trampoline, and we don't need that.
	 * We could do this only if the method does not need the special handling
	 * in mono_magic_trampoline ().
	 */
	if (mono_aot_only && ji.type == MONO_PATCH_INFO_METHOD && !ji.data.method->is_generic && !mono_method_check_context_used (ji.data.method) && !(ji.data.method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) &&
		!mono_method_needs_static_rgctx_invoke (ji.data.method, FALSE) && !using_gsharedvt) {
		target = (guint8 *)mono_jit_compile_method (ji.data.method, error);
		if (!is_ok (error)) {
			mono_mempool_destroy (mp);
			return NULL;
		}
		no_ftnptr = TRUE;
	} else {
		target = (guint8 *)mono_resolve_patch_target (NULL, NULL, &ji, TRUE, error);
		if (!is_ok (error)) {
			mono_mempool_destroy (mp);
			return NULL;
		}
	}

	/*
	 * The trampoline expects us to return a function descriptor on platforms which use
	 * it, but resolve_patch_target returns a direct function pointer for some type of
	 * patches, so have to translate between the two.
	 * FIXME: Clean this up, but how ?
	 */
	if (ji.type == MONO_PATCH_INFO_ABS
		|| ji.type == MONO_PATCH_INFO_JIT_ICALL_ID
		|| ji.type == MONO_PATCH_INFO_ICALL_ADDR
		|| ji.type == MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR
		|| ji.type == MONO_PATCH_INFO_JIT_ICALL_ADDR
		|| ji.type == MONO_PATCH_INFO_RGCTX_FETCH) {
		/* These should already have a function descriptor */
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		/* Our function descriptors have a 0 environment, gcc created ones don't */
		if (ji.type != MONO_PATCH_INFO_JIT_ICALL_ID
				&& ji.type != MONO_PATCH_INFO_JIT_ICALL_ADDR
				&& ji.type != MONO_PATCH_INFO_ICALL_ADDR
				&& ji.type != MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR)
			g_assert (((gpointer*)target) [2] == 0);
#endif
		/* Empty */
	} else if (!no_ftnptr) {
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		g_assert (((gpointer*)target) [2] != 0);
#endif
		target = (guint8 *)mono_create_ftnptr (target);
	}

	mono_mempool_destroy (mp);

	/* Patch the PLT entry with target which might be the actual method not a trampoline */
	mono_aot_patch_plt_entry (aot_module, code, plt_entry, module->got, regs, target);

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
 */
static void
init_plt (MonoAotModule *amodule)
{
	int i;
	gpointer tramp;

	if (amodule->plt_inited)
		return;

	tramp = mono_create_specific_trampoline (get_default_mem_manager (), amodule, MONO_TRAMPOLINE_AOT_PLT, NULL);
	tramp = mono_create_ftnptr (tramp);

	amodule_lock (amodule);

	if (amodule->plt_inited) {
		amodule_unlock (amodule);
		return;
	}

	if (amodule->info.plt_size <= 1) {
		amodule->plt_inited = TRUE;
		amodule_unlock (amodule);
		return;
	}

	/*
	 * Initialize the PLT entries in the GOT to point to the default targets.
	 */
	for (i = 1; i < amodule->info.plt_size; ++i)
		/* All the default entries point to the AOT trampoline */
		((gpointer*)amodule->got)[amodule->info.plt_got_offset_base + i] = tramp;

	mono_memory_barrier ();

	amodule->plt_inited = TRUE;

	amodule_unlock (amodule);
}

/*
 * mono_aot_get_plt_entry:
 *
 *   Return the address of the PLT entry called by the code at CODE if exists.
 */
guint8*
mono_aot_get_plt_entry (host_mgreg_t *regs, guint8 *code)
{
	MonoAotModule *amodule = find_aot_module (code);
	guint8 *target = NULL;

	if (!amodule)
		return NULL;

#ifdef TARGET_ARM
	if (is_thumb_code (amodule, code - 4))
		return mono_arm_get_thumb_plt_entry (code);
#endif

#ifdef MONO_ARCH_AOT_SUPPORTED
#ifdef MONO_ARCH_CODE_EXEC_ONLY
	target = mono_aot_arch_get_plt_entry_exec_only (&amodule->info, regs, code, amodule->plt);
#else
	target = mono_arch_get_call_target (code);
#endif
#else
	g_assert_not_reached ();
#endif

#ifdef TARGET_APPLE_MOBILE
	while (target != NULL) {
		if ((target >= (guint8*)(amodule->plt)) && (target < (guint8*)(amodule->plt_end)))
			return target;

		// Add 4 since mono_arch_get_call_target assumes we're passing
		// the instruction after the actual branch instruction.
		target = mono_arch_get_call_target (target + 4);
	}

	return NULL;
#else
	if ((target >= (guint8*)(amodule->plt)) && (target < (guint8*)(amodule->plt_end)))
		return target;
	else
		return NULL;
#endif
}

/*
 * mono_aot_get_plt_info_offset:
 *
 *   Return the PLT info offset belonging to the plt entry called by CODE.
 */
guint32
mono_aot_get_plt_info_offset (gpointer aot_module, guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	if (!plt_entry) {
		plt_entry = mono_aot_get_plt_entry (regs, code);
		g_assert (plt_entry);
	}

	/* The offset is embedded inside the code after the plt entry */
#ifdef MONO_ARCH_AOT_SUPPORTED
#ifdef MONO_ARCH_CODE_EXEC_ONLY
	return mono_arch_get_plt_info_offset_exec_only (&((MonoAotModule*)aot_module)->info, plt_entry, regs, code, aot_resolve_plt_info_offset, aot_module);
#else
	return mono_arch_get_plt_info_offset (plt_entry, regs, code);
#endif
#else
	g_assert_not_reached ();
	return 0;
#endif
}

static gpointer
mono_create_ftnptr_malloc (guint8 *code)
{
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
	MonoPPCFunctionDescriptor *ftnptr = g_malloc0 (sizeof (MonoPPCFunctionDescriptor));

	ftnptr->code = code;
	ftnptr->toc = NULL;
	ftnptr->env = NULL;

	return ftnptr;
#else
	return code;
#endif
}

/*
 * load_function_full:
 *
 *   Load the function named NAME from the aot image.
 */
static gpointer
load_function_full (MonoAotModule *amodule, const char *name, MonoTrampInfo **out_tinfo)
{
	char *symbol;
	guint8 *p;
	int n_patches, pindex;
	MonoMemPool *mp;
	gpointer code;
	guint32 info_offset;

	/* Load the code */

	find_amodule_symbol (amodule, name, &code);
	g_assertf (code, "Symbol '%s' not found in AOT file '%s'.\n", name, amodule->aot_name);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: FOUND function '%s' in AOT file '%s'.", name, amodule->aot_name);

	/* Load info */

	symbol = g_strdup_printf ("%s_p", name);
	find_amodule_symbol (amodule, symbol, (gpointer *)&p);
	g_free (symbol);
	if (!p)
		/* Nothing to patch */
		return code;

	info_offset = *(guint32*)p;
	if (out_tinfo) {
		MonoTrampInfo *tinfo;
		guint32 code_size, uw_info_len, uw_offset;
		guint8 *uw_info;
		/* Construct a MonoTrampInfo from the data in the AOT image */

		p += sizeof (guint32);
		code_size = *(guint32*)p;
		p += sizeof (guint32);
		uw_offset = *(guint32*)p;
		uw_info = amodule->unwind_info + uw_offset;
		uw_info_len = decode_value (uw_info, &uw_info);

		tinfo = g_new0 (MonoTrampInfo, 1);
		tinfo->code = (guint8 *)code;
		tinfo->code_size = code_size;
		tinfo->uw_info_len = uw_info_len;
		if (uw_info_len)
			tinfo->uw_info = uw_info;

		*out_tinfo = tinfo;
	}

	p = amodule->blob + info_offset;

	/* Similar to mono_aot_load_method () */

	n_patches = decode_value (p, &p);

	if (n_patches) {
		MonoJumpInfo *patches;
		guint32 *got_slots;

		mp = mono_mempool_new ();

		patches = load_patch_info (amodule, mp, n_patches, FALSE, &got_slots, p, &p);
		g_assert (patches);

		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];
			ERROR_DECL (error);
			gpointer target;

			if (amodule->got [got_slots [pindex]])
				continue;

			/*
			 * When this code is executed, the runtime may not be initalized yet, so
			 * resolve the patch info by hand.
			 */
			if (ji->type == MONO_PATCH_INFO_SPECIFIC_TRAMPOLINE_LAZY_FETCH_ADDR) {
				target = mono_create_specific_trampoline (get_default_mem_manager (), GUINT_TO_POINTER (ji->data.uindex), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, NULL);
				target = mono_create_ftnptr_malloc ((guint8 *)target);
			} else if (ji->type == MONO_PATCH_INFO_SPECIFIC_TRAMPOLINES) {
				target = amodule->info.specific_trampolines;
				g_assert (target);
			} else if (ji->type == MONO_PATCH_INFO_SPECIFIC_TRAMPOLINES_GOT_SLOTS_BASE) {
				target = &amodule->got [amodule->info.trampoline_got_offset_base [MONO_AOT_TRAMP_SPECIFIC]];
			} else if (ji->type == MONO_PATCH_INFO_JIT_ICALL_ADDR) {
				const MonoJitICallId jit_icall_id = (MonoJitICallId)ji->data.jit_icall_id;
				switch (jit_icall_id) {

#undef MONO_AOT_ICALL
#define MONO_AOT_ICALL(x) case MONO_JIT_ICALL_ ## x: target = (gpointer)x; break;

				MONO_AOT_ICALL (mono_get_lmf_addr)
				MONO_AOT_ICALL (mono_thread_force_interruption_checkpoint_noraise)
				MONO_AOT_ICALL (mono_exception_from_token)

				case MONO_JIT_ICALL_mono_debugger_agent_single_step_from_context:
					target = (gpointer)mono_component_debugger ()->single_step_from_context;
					break;
				case MONO_JIT_ICALL_mono_debugger_agent_breakpoint_from_context:
					target = (gpointer)mono_component_debugger ()->breakpoint_from_context;
					break;
				case MONO_JIT_ICALL_mono_throw_exception:
					target = mono_get_throw_exception_addr ();
					break;
				case MONO_JIT_ICALL_mono_rethrow_preserve_exception:
					target = mono_get_rethrow_preserve_exception_addr ();
					break;

				case MONO_JIT_ICALL_generic_trampoline_jit:
				case MONO_JIT_ICALL_generic_trampoline_jump:
				case MONO_JIT_ICALL_generic_trampoline_rgctx_lazy_fetch:
				case MONO_JIT_ICALL_generic_trampoline_aot:
				case MONO_JIT_ICALL_generic_trampoline_aot_plt:
				case MONO_JIT_ICALL_generic_trampoline_delegate:
				case MONO_JIT_ICALL_generic_trampoline_vcall:
					target = (gpointer)mono_get_trampoline_func (mono_jit_icall_id_to_trampoline_type (jit_icall_id));
					break;
				default:
					target = mono_arch_load_function (jit_icall_id);
					g_assertf (target, "Unknown relocation '%p'\n", ji->data.target);
				}
			} else {
				/* Hopefully the code doesn't have patches which need method to be set.
				 */
				target = mono_resolve_patch_target (NULL, (guint8 *)code, ji, FALSE, error);
				mono_error_assert_ok (error);
				g_assert (target);
			}

			if (ji->type != MONO_PATCH_INFO_NONE)
				amodule->got [got_slots [pindex]] = target;
		}

		g_free (got_slots);

		mono_mempool_destroy (mp);
	}

	return code;
}

static gpointer
load_function (MonoAotModule *amodule, const char *name)
{
	return load_function_full (amodule, name, NULL);
}

static MonoAotModule*
get_mscorlib_aot_module (void)
{
	MonoImage *image;
	MonoAotModule *amodule;

	image = mono_defaults.corlib;
	if (image && image->aot_module)
		amodule = image->aot_module;
	else
		amodule = mscorlib_aot_module;
	g_assert (amodule);
	return amodule;
}

static void
mono_no_trampolines (void)
{
	g_assert_not_reached ();
}

/*
 * Return the trampoline identified by NAME from the mscorlib AOT file.
 * On ppc64, this returns a function descriptor.
 */
gpointer
mono_aot_get_trampoline_full (const char *name, MonoTrampInfo **out_tinfo)
{
	MonoAotModule *amodule = get_mscorlib_aot_module ();

	if (mono_llvm_only) {
		*out_tinfo = NULL;
		return (gpointer)mono_no_trampolines;
	}

	return mono_create_ftnptr_malloc ((guint8 *)load_function_full (amodule, name, out_tinfo));
}

gpointer
mono_aot_get_trampoline (const char *name)
{
	MonoTrampInfo *out_tinfo;
	gpointer code;

	code =  mono_aot_get_trampoline_full (name, &out_tinfo);
	mono_aot_tramp_info_register (out_tinfo, NULL);

	return code;
}

static gpointer
read_unwind_info (MonoAotModule *amodule, MonoTrampInfo *info, const char *symbol_name)
{
	gpointer symbol_addr;
	guint32 uw_offset, uw_info_len;
	guint8 *uw_info;

	find_amodule_symbol (amodule, symbol_name, &symbol_addr);

	if (!symbol_addr)
		return NULL;

	uw_offset = *(guint32*)symbol_addr;
	uw_info = amodule->unwind_info + uw_offset;
	uw_info_len = decode_value (uw_info, &uw_info);

	info->uw_info_len = uw_info_len;
	if (uw_info_len)
		info->uw_info = uw_info;
	else
		info->uw_info = NULL;

	/* If successful return the address of the following data */
	return (guint32*)symbol_addr + 1;
}

#ifdef TARGET_APPLE_MOBILE
#include <mach/mach.h>

static TrampolinePage* trampoline_pages [MONO_AOT_TRAMP_NUM];

static void
read_page_trampoline_uwinfo (MonoTrampInfo *info, int tramp_type, gboolean is_generic)
{
	char symbol_name [128];

	if (tramp_type == MONO_AOT_TRAMP_SPECIFIC)
		sprintf (symbol_name, "specific_trampolines_page_%s_p", is_generic ? "gen" : "sp");
	else if (tramp_type == MONO_AOT_TRAMP_STATIC_RGCTX)
		sprintf (symbol_name, "rgctx_trampolines_page_%s_p", is_generic ? "gen" : "sp");
	else if (tramp_type == MONO_AOT_TRAMP_IMT)
		sprintf (symbol_name, "imt_trampolines_page_%s_p", is_generic ? "gen" : "sp");
	else if (tramp_type == MONO_AOT_TRAMP_GSHAREDVT_ARG)
		sprintf (symbol_name, "gsharedvt_trampolines_page_%s_p", is_generic ? "gen" : "sp");
	else if (tramp_type == MONO_AOT_TRAMP_UNBOX_ARBITRARY)
		sprintf (symbol_name, "unbox_arbitrary_trampolines_page_%s_p", is_generic ? "gen" : "sp");
	else
		g_assert_not_reached ();

	read_unwind_info (mscorlib_aot_module, info, symbol_name);
}

static unsigned char*
get_new_trampoline_from_page (int tramp_type)
{
	TrampolinePage *page;
	int count;
	void *tpage;
	vm_address_t addr, taddr;
	kern_return_t ret;
	vm_prot_t prot, max_prot;
	int psize, specific_trampoline_size;
	unsigned char *code;

	specific_trampoline_size = 2 * sizeof (gpointer);

	mono_aot_page_lock ();
	page = trampoline_pages [tramp_type];
	if (page && page->trampolines < page->trampolines_end) {
		code = page->trampolines;
		page->trampolines += specific_trampoline_size;
		mono_aot_page_unlock ();
		return code;
	}
	mono_aot_page_unlock ();

	psize = MONO_AOT_TRAMP_PAGE_SIZE;

	/* the trampoline template page is in the mscorlib module */
	MonoAotModule *amodule = mscorlib_aot_module;
	g_assert (amodule);

	if (tramp_type == MONO_AOT_TRAMP_SPECIFIC)
		tpage = load_function (amodule, "specific_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_STATIC_RGCTX)
		tpage = load_function (amodule, "rgctx_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_IMT)
		tpage = load_function (amodule, "imt_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_GSHAREDVT_ARG)
		tpage = load_function (amodule, "gsharedvt_arg_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_UNBOX_ARBITRARY)
		tpage = load_function (amodule, "unbox_arbitrary_trampolines_page");
	else
		g_error ("Incorrect tramp type for trampolines page");
	g_assert (tpage);
	/*g_warning ("loaded trampolines page at %x", tpage);*/

	/* avoid the unlikely case of looping forever */
	count = 40;
	page = NULL;
	while (page == NULL && count-- > 0) {
		MonoTrampInfo *gen_info, *sp_info;

		addr = 0;
		/* allocate two contiguous pages of memory: the first page will contain the data (like a local constant pool)
		 * while the second will contain the trampolines.
		 */
		do {
			ret = vm_allocate (mach_task_self (), &addr, psize * 2, VM_FLAGS_ANYWHERE);
		} while (ret == KERN_ABORTED);
		if (ret != KERN_SUCCESS) {
			g_error ("Cannot allocate memory for trampolines: %d", ret);
			break;
		}
		/*g_warning ("allocated trampoline double page at %x", addr);*/
		/* replace the second page with a remapped trampoline page */
		taddr = addr + psize;
		vm_deallocate (mach_task_self (), taddr, psize);
		ret = vm_remap (mach_task_self (), &taddr, psize, 0, FALSE, mach_task_self(), (vm_address_t)tpage, FALSE, &prot, &max_prot, VM_INHERIT_SHARE);
		if (ret != KERN_SUCCESS) {
			/* someone else got the page, try again  */
			vm_deallocate (mach_task_self (), addr, psize);
			continue;
		}
		/*g_warning ("remapped trampoline page at %x", taddr);*/

		mono_aot_page_lock ();
		page = trampoline_pages [tramp_type];
		/* some other thread already allocated, so use that to avoid wasting memory */
		if (page && page->trampolines < page->trampolines_end) {
			code = page->trampolines;
			page->trampolines += specific_trampoline_size;
			mono_aot_page_unlock ();
			vm_deallocate (mach_task_self (), addr, psize);
			vm_deallocate (mach_task_self (), taddr, psize);
			return code;
		}
		page = (TrampolinePage*)addr;
		page->next = trampoline_pages [tramp_type];
		trampoline_pages [tramp_type] = page;
		page->trampolines = (guint8*)(taddr + amodule->info.tramp_page_code_offsets [tramp_type]);
		page->trampolines_end = (guint8*)(taddr + psize - 64);
		code = page->trampolines;
		page->trampolines += specific_trampoline_size;
		mono_aot_page_unlock ();

		/* Register the generic part at the beggining of the trampoline page */
		gen_info = mono_tramp_info_create (NULL, (guint8*)taddr, amodule->info.tramp_page_code_offsets [tramp_type], NULL, NULL);
		read_page_trampoline_uwinfo (gen_info, tramp_type, TRUE);
		mono_aot_tramp_info_register (gen_info, NULL);
		/*
		 * FIXME
		 * Registering each specific trampoline produces a lot of
		 * MonoJitInfo structures. Jump trampolines are also registered
		 * separately.
		 */
		if (tramp_type != MONO_AOT_TRAMP_SPECIFIC) {
			/* Register the rest of the page as a single trampoline */
			sp_info = mono_tramp_info_create (NULL, code, page->trampolines_end - code, NULL, NULL);
			read_page_trampoline_uwinfo (sp_info, tramp_type, FALSE);
			mono_aot_tramp_info_register (sp_info, NULL);
		}
		return code;
	}
	g_error ("Cannot allocate more trampoline pages: %d", ret);
	return NULL;
}

#else
static unsigned char*
get_new_trampoline_from_page (int tramp_type)
{
	g_error ("Page trampolines not supported.");
	return NULL;
}
#endif


static gpointer
get_new_specific_trampoline_from_page (gpointer tramp, gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_SPECIFIC);

	data = (gpointer*)((char*)code - MONO_AOT_TRAMP_PAGE_SIZE);
	data [0] = arg;
	data [1] = tramp;
	/*g_warning ("new trampoline at %p for data %p, tramp %p (stored at %p)", code, arg, tramp, data);*/
	return MINI_ADDR_TO_FTNPTR (code);
}

static gpointer
get_new_rgctx_trampoline_from_page (gpointer tramp, gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_STATIC_RGCTX);

	data = (gpointer*)((char*)code - MONO_AOT_TRAMP_PAGE_SIZE);
	data [0] = arg;
	data [1] = tramp;
	/*g_warning ("new rgctx trampoline at %p for data %p, tramp %p (stored at %p)", code, arg, tramp, data);*/
	return MINI_ADDR_TO_FTNPTR (code);
}

static gpointer
get_new_imt_trampoline_from_page (gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_IMT);

	data = (gpointer*)((char*)code - MONO_AOT_TRAMP_PAGE_SIZE);
	data [0] = arg;
	/*g_warning ("new imt trampoline at %p for data %p, (stored at %p)", code, arg, data);*/
	return MINI_ADDR_TO_FTNPTR (code);
}

static gpointer
get_new_gsharedvt_arg_trampoline_from_page (gpointer tramp, gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_GSHAREDVT_ARG);

	data = (gpointer*)((char*)code - MONO_AOT_TRAMP_PAGE_SIZE);
	data [0] = arg;
	data [1] = tramp;
	/*g_warning ("new rgctx trampoline at %p for data %p, tramp %p (stored at %p)", code, arg, tramp, data);*/
	return MINI_ADDR_TO_FTNPTR (code);
}

static gpointer
get_new_unbox_arbitrary_trampoline_frome_page (gpointer addr)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_UNBOX_ARBITRARY);

	data = (gpointer*)((char*)code - MONO_AOT_TRAMP_PAGE_SIZE);
	data [0] = addr;

	return MINI_ADDR_TO_FTNPTR (code);
}

/* Return a given kind of trampoline */
/* FIXME set unwind info for these trampolines */
static gpointer
get_numerous_trampoline (MonoAotTrampoline tramp_type, int n_got_slots, MonoAotModule **out_amodule, guint32 *got_offset, guint32 *out_tramp_size)
{
#ifndef DISABLE_ASSERT_MESSAGES
	MonoImage *image;
#endif
	MonoAotModule *amodule = get_mscorlib_aot_module ();
	int index, tramp_size;

#ifndef DISABLE_ASSERT_MESSAGES
	/* Currently, we keep all trampolines in the mscorlib AOT image */
	image = mono_defaults.corlib;
#endif

	*out_amodule = amodule;

	mono_aot_lock ();

	if (amodule->trampoline_index [tramp_type] == amodule->info.num_trampolines [tramp_type]) {
		g_error ("Ran out of trampolines of type %d in '%s' (limit %d)\n",
				 tramp_type, image ? image->name : MONO_ASSEMBLY_CORLIB_NAME, amodule->info.num_trampolines [tramp_type]);
	}
	index = amodule->trampoline_index [tramp_type] ++;

	mono_aot_unlock ();

	*got_offset = amodule->info.trampoline_got_offset_base [tramp_type] + (index * n_got_slots);

	tramp_size = amodule->info.trampoline_size [tramp_type];

	if (out_tramp_size)
		*out_tramp_size = tramp_size;

	return amodule->trampolines [tramp_type] + (index * tramp_size);
}

static void
no_specific_trampoline (void)
{
	g_assert_not_reached ();
}

/*
 * Return a specific trampoline from the AOT file.
 */
gpointer
mono_aot_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, guint32 *code_len)
{
	MonoAotModule *amodule;
	guint32 got_offset, tramp_size;
	guint8 *code, *tramp;
	static gpointer generic_trampolines [MONO_TRAMPOLINE_NUM];
	static gboolean inited;
	static guint32 num_trampolines;

	if (mono_llvm_only) {
		*code_len = 1;
		return (gpointer)no_specific_trampoline;
	}

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
		const char *symbol;

		symbol = mono_get_generic_trampoline_name (tramp_type);
		generic_trampolines [tramp_type] = mono_aot_get_trampoline (symbol);
	}

	tramp = (guint8 *)generic_trampolines [tramp_type];
	g_assert (tramp);

	if (USE_PAGE_TRAMPOLINES) {
		code = (guint8 *)get_new_specific_trampoline_from_page (tramp, arg1);
		tramp_size = 8;
	} else {
		code = (guint8 *)get_numerous_trampoline (MONO_AOT_TRAMP_SPECIFIC, 2, &amodule, &got_offset, &tramp_size);

		amodule->got [got_offset] = tramp;
		amodule->got [got_offset + 1] = arg1;
	}

	if (code_len)
		*code_len = tramp_size;

	return MINI_ADDR_TO_FTNPTR (code);
}

gpointer
mono_aot_get_static_rgctx_trampoline (gpointer ctx, gpointer addr)
{
	MonoAotModule *amodule;
	guint8 *code;
	guint32 got_offset;

	if (USE_PAGE_TRAMPOLINES) {
		code = (guint8 *)get_new_rgctx_trampoline_from_page (addr, ctx);
	} else {
		code = (guint8 *)get_numerous_trampoline (MONO_AOT_TRAMP_STATIC_RGCTX, 2, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = ctx;
		amodule->got [got_offset + 1] = addr;
	}

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (MINI_ADDR_TO_FTNPTR (code));
}

gpointer
mono_aot_get_unbox_arbitrary_trampoline (gpointer addr)
{
	MonoAotModule *amodule;
	guint8 *code;
	guint32 got_offset;

	if (USE_PAGE_TRAMPOLINES) {
		code = (guint8 *)get_new_unbox_arbitrary_trampoline_frome_page (addr);
	} else {
		code = (guint8 *)get_numerous_trampoline (MONO_AOT_TRAMP_UNBOX_ARBITRARY, 1, &amodule, &got_offset, NULL);
		amodule->got [got_offset] = addr;
	}

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (MINI_ADDR_TO_FTNPTR (code));
}

static int
i32_idx_comparer (const void *key, const void *member)
{
	gint32 idx1 = GPOINTER_TO_INT (key);
	gint32 idx2 = *(gint32*)member;
	return idx1 - idx2;
}

static int
ui16_idx_comparer (const void *key, const void *member)
{
	int idx1 = GPOINTER_TO_INT (key);
	int idx2 = *(guint16*)member;
	return idx1 - idx2;
}

static gboolean
aot_is_slim_amodule (MonoAotModule *amodule)
{
	if (!amodule)
		return FALSE;

	/* "slim" only applies to mscorlib.dll */
	if (strcmp (amodule->aot_name, MONO_ASSEMBLY_CORLIB_NAME))
		return FALSE;

	guint32 f = amodule->info.flags;
	return (f & MONO_AOT_FILE_FLAG_INTERP) && !(f & MONO_AOT_FILE_FLAG_FULL_AOT);
}

gpointer
mono_aot_get_unbox_trampoline (MonoMethod *method, gpointer addr)
{
	ERROR_DECL (error);
	guint32 method_index = mono_metadata_token_index (method->token) - 1;
	MonoAotModule *amodule;
	gpointer code;
	guint32 *ut, *ut_end, *entry;
	int low, high, entry_index = 0;
	MonoTrampInfo *tinfo;

	if (method->is_inflated && !mono_method_is_generic_sharable_full (method, FALSE, FALSE, FALSE)) {
		method_index = find_aot_method (method, &amodule);
		if (method_index == 0xffffff && mono_method_is_generic_sharable_full (method, FALSE, TRUE, FALSE)) {
			MonoMethod *shared = mini_get_shared_method_full (method, SHARE_MODE_NONE, error);
			mono_error_assert_ok (error);
			method_index = find_aot_method (shared, &amodule);
		}
		if (method_index == 0xffffff && mono_method_is_generic_sharable_full (method, FALSE, TRUE, TRUE)) {
			MonoMethod *shared = mini_get_shared_method_full (method, SHARE_MODE_GSHAREDVT, error);
			mono_error_assert_ok (error);

			method_index = find_aot_method (shared, &amodule);
		}
	} else
		amodule = m_class_get_image (method->klass)->aot_module;

	if (amodule == NULL || method_index == 0xffffff || aot_is_slim_amodule (amodule)) {
		/* couldn't find unbox trampoline specifically generated for that
		 * method. this should only happen when an unbox trampoline is needed
		 * for `fullAOT code -> native-to-interp -> interp` transition if
		 *   (1) it's a virtual call
		 *   (2) the receiver is a value type, thus needs unboxing */
		g_assert (mono_use_interpreter);
		return mono_aot_get_unbox_arbitrary_trampoline (addr);
	}

	if (!amodule->unbox_tramp_per_method) {
		gpointer arr = g_new0 (gpointer, amodule->info.nmethods);
		mono_memory_barrier ();
		gpointer old_arr = mono_atomic_cas_ptr ((volatile gpointer*)&amodule->unbox_tramp_per_method, arr, NULL);
		if (old_arr)
			g_free (arr);
	}
	if (amodule->unbox_tramp_per_method [method_index])
		return amodule->unbox_tramp_per_method [method_index];

	if (amodule->info.llvm_unbox_tramp_indexes) {
		int unbox_tramp_idx;

		/* Search the llvm_unbox_tramp_indexes table using a binary search */
		if (amodule->info.llvm_unbox_tramp_elemsize == sizeof (guint32)) {
			void *ptr = mono_binary_search (GINT_TO_POINTER (method_index), amodule->info.llvm_unbox_tramp_indexes, amodule->info.llvm_unbox_tramp_num, amodule->info.llvm_unbox_tramp_elemsize, i32_idx_comparer);
			g_assert (ptr);
			g_assert (*(int*)ptr == method_index);
			unbox_tramp_idx = (guint32*)ptr - (guint32*)amodule->info.llvm_unbox_tramp_indexes;
		} else {
			void *ptr = mono_binary_search (GINT_TO_POINTER (method_index), amodule->info.llvm_unbox_tramp_indexes, amodule->info.llvm_unbox_tramp_num, amodule->info.llvm_unbox_tramp_elemsize, ui16_idx_comparer);
			g_assert (ptr);
			g_assert (*(guint16*)ptr == method_index);
			unbox_tramp_idx = (guint16*)ptr - (guint16*)amodule->info.llvm_unbox_tramp_indexes;
		}
		g_assert (unbox_tramp_idx < amodule->info.llvm_unbox_tramp_num);
		code = ((gpointer*)(amodule->info.llvm_unbox_trampolines))[unbox_tramp_idx];
		g_assert (code);

		code = MINI_ADDR_TO_FTNPTR (code);

		mono_memory_barrier ();
		amodule->unbox_tramp_per_method [method_index] = code;

		return code;
	}

	if (amodule->info.llvm_get_unbox_tramp) {
		gpointer (*get_tramp) (int) = (gpointer (*)(int))amodule->info.llvm_get_unbox_tramp;
		code = get_tramp (method_index);

		if (code) {
			mono_memory_barrier ();
			amodule->unbox_tramp_per_method [method_index] = code;

			return code;
		}
	}

	ut = amodule->unbox_trampolines;
	ut_end = amodule->unbox_trampolines_end;

	/* Do a binary search in the sorted table */
	code = NULL;
	low = 0;
	high = (ut_end - ut);
	while (low < high) {
		entry_index = (low + high) / 2;
		entry = &ut [entry_index];
		if (entry [0] < method_index) {
			low = entry_index + 1;
		} else if (entry [0] > method_index) {
			high = entry_index;
		} else {
			break;
		}
	}

	if (amodule->info.flags & MONO_AOT_FILE_FLAG_CODE_EXEC_ONLY)
		code = ((gpointer*)amodule->unbox_trampoline_addresses) [entry_index];
	else
		code = get_call_table_entry (amodule->unbox_trampoline_addresses, entry_index, amodule->info.call_table_entry_size);

	g_assert (code);

	code = MINI_ADDR_TO_FTNPTR (code);

	tinfo = mono_tramp_info_create (NULL, (guint8 *)code, 0, NULL, NULL);

	gpointer const symbol_addr = read_unwind_info (amodule, tinfo, "unbox_trampoline_p");
	if (!symbol_addr) {
		mono_tramp_info_free (tinfo);
		return FALSE;
	}

	tinfo->method = method;
	tinfo->code_size = *(guint32*)symbol_addr;
	tinfo->unwind_ops = mono_arch_get_cie_program ();
	mono_aot_tramp_info_register (tinfo, NULL);

	mono_memory_barrier ();
	amodule->unbox_tramp_per_method [method_index] = code;

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (code);
}

gpointer
mono_aot_get_lazy_fetch_trampoline (guint32 slot)
{
	char *symbol;
	gpointer code;
	MonoAotModule *amodule = mscorlib_aot_module;
	guint32 index = MONO_RGCTX_SLOT_INDEX (slot);
	static int count = 0;

	count ++;
	if (index >= amodule->info.num_rgctx_fetch_trampolines) {
		static gpointer addr;
		gpointer *info;

		/*
		 * Use the general version of the rgctx fetch trampoline. It receives a pair of <slot, trampoline> in the rgctx arg reg.
		 */
		if (!addr)
			addr = load_function (amodule, "rgctx_fetch_trampoline_general");
		info = (void **)mono_mem_manager_alloc0 (get_default_mem_manager (), sizeof (gpointer) * 2);
		info [0] = GUINT_TO_POINTER (slot);
		info [1] = mono_create_specific_trampoline (get_default_mem_manager (), GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, NULL);
		code = mono_aot_get_static_rgctx_trampoline (info, addr);
		return mono_create_ftnptr (code);
	}

	symbol = mono_get_rgctx_fetch_trampoline_name (slot);
	code = load_function (amodule, symbol);
	g_free (symbol);
	/* The caller expects an ftnptr */
	return mono_create_ftnptr (code);
}

static void
no_imt_trampoline (void)
{
	g_assert_not_reached ();
}

gpointer
mono_aot_get_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	guint32 got_offset;
	gpointer code;
	gpointer *buf;
	int i, index, real_count;
	MonoAotModule *amodule;

	if (mono_llvm_only)
		return (gpointer)no_imt_trampoline;

	real_count = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (item->is_equals)
			real_count ++;
	}

	/* Save the entries into an array */
	buf = (void **)m_class_alloc0 (vtable->klass, (real_count + 1) * 2 * sizeof (gpointer));
	index = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (!item->is_equals)
			continue;

		g_assert (item->key);

		buf [(index * 2)] = item->key;
		if (item->has_target_code) {
			gpointer *p = (gpointer *)m_class_alloc0 (vtable->klass, sizeof (gpointer));
			*p = item->value.target_code;
			buf [(index * 2) + 1] = p;
		} else {
			buf [(index * 2) + 1] = &(vtable->vtable [item->value.vtable_slot]);
		}
		index ++;
	}
	buf [(index * 2)] = NULL;
	buf [(index * 2) + 1] = fail_tramp;

	if (USE_PAGE_TRAMPOLINES) {
		code = get_new_imt_trampoline_from_page (buf);
	} else {
		code = get_numerous_trampoline (MONO_AOT_TRAMP_IMT, 1, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = buf;
	}

	return MINI_ADDR_TO_FTNPTR (code);
}

gpointer
mono_aot_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	MonoAotModule *amodule;
	guint8 *code;
	guint32 got_offset;

	if (USE_PAGE_TRAMPOLINES) {
		code = (guint8 *)get_new_gsharedvt_arg_trampoline_from_page (addr, arg);
	} else {
		code = (guint8 *)get_numerous_trampoline (MONO_AOT_TRAMP_GSHAREDVT_ARG, 2, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = arg;
		amodule->got [got_offset + 1] = addr;
	}

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (MINI_ADDR_TO_FTNPTR (code));
}

#ifdef MONO_ARCH_HAVE_FTNPTR_ARG_TRAMPOLINE
gpointer
mono_aot_get_ftnptr_arg_trampoline (gpointer arg, gpointer addr)
{
	MonoAotModule *amodule;
	guint8 *code;
	guint32 got_offset;

	if (USE_PAGE_TRAMPOLINES) {
		g_error ("FIXME: ftnptr_arg page trampolines");
	} else {
		code = (guint8 *)get_numerous_trampoline (MONO_AOT_TRAMP_FTNPTR_ARG, 2, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = arg;
		amodule->got [got_offset + 1] = addr;
	}

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (MINI_ADDR_TO_FTNPTR (code));
}
#endif


/*
 * mono_aot_set_make_unreadable:
 *
 *   Set whenever to make all mmaped memory unreadable. In conjuction with a
 * SIGSEGV handler, this is useful to find out which pages the runtime tries to read.
 */
void
mono_aot_set_make_unreadable (gboolean unreadable)
{
	static int inited;

	make_unreadable = unreadable;

	if (make_unreadable && !inited) {
		mono_counters_register ("AOT: pagefaults", MONO_COUNTER_JIT | MONO_COUNTER_INT, &n_pagefaults);
	}
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

	/*
	 * Not signal safe, but SIGSEGV's are synchronous, and
	 * this is only turned on by a MONO_DEBUG option.
	 */
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
#ifndef HOST_WIN32
	guint8* start = (guint8*)ROUND_DOWN (((gssize)ptr), mono_pagesize ());
	int res;

	mono_aot_lock ();
	res = mono_mprotect (start, mono_pagesize (), MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC);
	g_assert (res == 0);

	n_pagefaults ++;
	mono_aot_unlock ();
#endif
}

MonoAotMethodFlags
mono_aot_get_method_flags (guint8 *code)
{
	guint32 flags;

	if (!code_to_method_flags)
		return MONO_AOT_METHOD_FLAG_NONE;
	mono_aot_lock ();
	/* Not found and no FLAG_NONE are the same, but its not a problem */
	flags = GPOINTER_TO_UINT (g_hash_table_lookup (code_to_method_flags, code));
	mono_aot_unlock ();
	return (MonoAotMethodFlags)flags;
}

#else
/* AOT disabled */

void
mono_aot_init (void)
{
}

guint32
mono_aot_find_method_index (MonoMethod *method)
{
	g_assert_not_reached ();
	return 0;
}

gboolean
mono_aot_init_llvm_method (gpointer aot_module, gpointer method_info, MonoClass *init_class, MonoError *error)
{
	g_assert_not_reached ();
	return FALSE;
}

gpointer
mono_aot_get_method (MonoMethod *method, MonoError *error)
{
	error_init (error);
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
mono_aot_find_jit_info (MonoImage *image, gpointer addr)
{
	return NULL;
}

gpointer
mono_aot_get_method_from_token (MonoImage *image, guint32 token, MonoError *error)
{
	error_init (error);
	return NULL;
}

guint8*
mono_aot_get_plt_entry (host_mgreg_t *regs, guint8 *code)
{
	return NULL;
}

gpointer
mono_aot_plt_resolve (gpointer aot_module, host_mgreg_t *regs, guint8 *code, MonoError *error)
{
	return NULL;
}

void
mono_aot_patch_plt_entry (gpointer aot_module, guint8 *code, guint8 *plt_entry, gpointer *got, host_mgreg_t *regs, guint8 *addr)
{
}

gpointer
mono_aot_get_method_from_vt_slot (MonoVTable *vtable, int slot, MonoError *error)
{
	error_init (error);

	return NULL;
}

guint32
mono_aot_get_plt_info_offset (gpointer aot_module, guint8 *plt_entry, host_mgreg_t *regs, guint8 *code)
{
	g_assert_not_reached ();

	return 0;
}

gpointer
mono_aot_create_specific_trampoline (gpointer arg1, MonoTrampolineType tramp_type, guint32 *code_len)
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
mono_aot_get_trampoline_full (const char *name, MonoTrampInfo **out_tinfo)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_trampoline (const char *name)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_unbox_arbitrary_trampoline (gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_unbox_trampoline (MonoMethod *method, gpointer addr)
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
mono_aot_get_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_aot_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

#ifdef MONO_ARCH_HAVE_FTNPTR_ARG_TRAMPOLINE
gpointer
mono_aot_get_ftnptr_arg_trampoline (gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}
#endif

void
mono_aot_set_make_unreadable (gboolean unreadable)
{
}

gboolean
mono_aot_is_pagefault (void *ptr)
{
	return FALSE;
}

void
mono_aot_handle_pagefault (void *ptr)
{
}

guint8*
mono_aot_get_unwind_info (MonoJitInfo *ji, guint32 *unwind_info_len)
{
	g_assert_not_reached ();
	return NULL;
}

GHashTable *
mono_aot_get_weak_field_indexes (MonoImage *image)
{
	return NULL;
}

MonoAotMethodFlags
mono_aot_get_method_flags (guint8 *code)
{
	return MONO_AOT_METHOD_FLAG_NONE;
}

#endif
