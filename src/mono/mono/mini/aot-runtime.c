/*
 * aot-runtime.c: mono Ahead of Time compiler
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2002 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc.
 * Copyright 2011 Xamarin, Inc.
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
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/mono-endian.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/utils/mono-mmap.h>
#include "mono/utils/mono-compiler.h"
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-digest.h>

#include "mini.h"
#include "version.h"

#ifndef DISABLE_AOT

#ifdef TARGET_OSX
#define ENABLE_AOT_CACHE
#endif

#ifdef TARGET_WIN32
#define SHARED_EXT ".dll"
#elif ((defined(__ppc__) || defined(__powerpc__) || defined(__ppc64__)) || defined(__MACH__)) && !defined(__linux__)
#define SHARED_EXT ".dylib"
#elif defined(__APPLE__) && defined(TARGET_X86) && !defined(__native_client_codegen__)
#define SHARED_EXT ".dylib"
#else
#define SHARED_EXT ".so"
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))
#define ALIGN_PTR_TO(ptr,align) (gpointer)((((gssize)(ptr)) + (align - 1)) & (~(align - 1)))
#define ROUND_DOWN(VALUE,SIZE)	((VALUE) & ~((SIZE) - 1))

typedef struct {
	int method_index;
	MonoJitInfo *jinfo;
} JitInfoMap;

typedef struct MonoAotModule {
	char *aot_name;
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
	guint8 *blob;
	gint32 *code_offsets;
	gpointer *method_addresses;
	/* This contains <offset, index> pairs sorted by offset */
	/* This is needed because LLVM emitted methods can be in any order */
	gint32 *sorted_code_offsets;
	gint32 sorted_code_offsets_len;
	guint32 *method_info_offsets;
	guint32 *got_info_offsets;
	guint32 *ex_info_offsets;
	guint32 *class_info_offsets;
	guint32 *methods_loaded;
	guint16 *class_name_table;
	guint32 *extra_method_table;
	guint32 *extra_method_info_offsets;
	guint32 *unbox_trampolines;
	guint32 *unbox_trampolines_end;
	guint8 *unwind_info;
	guint8 *thumb_end;

	/* Points to the mono EH data created by LLVM */
	guint8 *mono_eh_frame;

	/* Points to the trampolines */
	guint8 *trampolines [MONO_AOT_TRAMP_NUM];
	/* The first unused trampoline of each kind */
	guint32 trampoline_index [MONO_AOT_TRAMP_NUM];

	gboolean use_page_trampolines;

	MonoAotFileInfo info;

	gpointer *globals;
	MonoDl *sofile;

	JitInfoMap *async_jit_info_table;
	mono_mutex_t mutex;
} MonoAotModule;

typedef struct {
	void *next;
	unsigned char *trampolines;
	unsigned char *trampolines_end;
} TrampolinePage;

static GHashTable *aot_modules;
#define mono_aot_lock() mono_mutex_lock (&aot_mutex)
#define mono_aot_unlock() mono_mutex_unlock (&aot_mutex)
static mono_mutex_t aot_mutex;

/* 
 * Maps assembly names to the mono_aot_module_<NAME>_info symbols in the
 * AOT modules registered by mono_aot_register_module ().
 */
static GHashTable *static_aot_modules;

/*
 * Maps MonoJitInfo* to the aot module they belong to, this can be different
 * from ji->method->klass->image's aot module for generic instances.
 */
static GHashTable *ji_to_amodule;

/*
 * Whenever to AOT compile loaded assemblies on demand and store them in
 * a cache.
 */
static gboolean enable_aot_cache = FALSE;

static gboolean mscorlib_aot_loaded;

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

static GHashTable *aot_jit_icall_hash;

#ifdef MONOTOUCH
#define USE_PAGE_TRAMPOLINES ((MonoAotModule*)mono_defaults.corlib->aot_module)->use_page_trampolines
#else
#define USE_PAGE_TRAMPOLINES 0
#endif

#define mono_aot_page_lock() mono_mutex_lock (&aot_page_mutex)
#define mono_aot_page_unlock() mono_mutex_unlock (&aot_page_mutex)
static mono_mutex_t aot_page_mutex;

static void
init_plt (MonoAotModule *info);

/*****************************************************/
/*                 AOT RUNTIME                       */
/*****************************************************/

static inline void
amodule_lock (MonoAotModule *amodule)
{
	mono_mutex_lock (&amodule->mutex);
}

static inline void
amodule_unlock (MonoAotModule *amodule)
{
	mono_mutex_unlock (&amodule->mutex);
}

/*
 * load_image:
 *
 *   Load one of the images referenced by AMODULE. Returns NULL if the image is not
 * found, and sets the loader error if SET_ERROR is TRUE.
 */
static MonoImage *
load_image (MonoAotModule *amodule, int index, gboolean set_error)
{
	MonoAssembly *assembly;
	MonoImageOpenStatus status;

	g_assert (index < amodule->image_table_len);

	if (amodule->image_table [index])
		return amodule->image_table [index];
	if (amodule->out_of_date)
		return NULL;

	assembly = mono_assembly_load (&amodule->image_names [index], amodule->assembly->basedir, &status);
	if (!assembly) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: module %s is unusable because dependency %s is not found.\n", amodule->aot_name, amodule->image_names [index].name);
		amodule->out_of_date = TRUE;

		if (set_error) {
			char *full_name = mono_stringify_assembly_name (&amodule->image_names [index]);
			mono_loader_set_error_assembly_load (full_name, FALSE);
			g_free (full_name);
		}
		return NULL;
	}

	if (strcmp (assembly->image->guid, amodule->image_guids [index])) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: module %s is unusable (GUID of dependent assembly %s doesn't match (expected '%s', got '%s').\n", amodule->aot_name, amodule->image_names [index].name, amodule->image_guids [index], assembly->image->guid);
		amodule->out_of_date = TRUE;
		return NULL;
	}

	amodule->image_table [index] = assembly->image;
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
	int start_offset, offset, noffsets, group_size;
	guint8 *data_start, *p;
	guint32 *index32 = NULL;
	guint16 *index16 = NULL;
	
	noffsets = table [0];
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
decode_resolve_method_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf);

static MonoClass*
decode_klass_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf);

static MonoType*
decode_type (MonoAotModule *module, guint8 *buf, guint8 **endbuf);

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
	guint8 *p = buf;
	guint8 *p2;
	int argc;

	p2 = p;
	argc = decode_value (p, &p);
	if (argc) {
		p = p2;
		ctx->class_inst = decode_generic_inst (module, p, &p);
		if (!ctx->class_inst)
			return FALSE;
	}
	p2 = p;
	argc = decode_value (p, &p);
	if (argc) {
		p = p2;
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
	MonoError error;
	MonoImage *image;
	MonoClass *klass = NULL, *eklass;
	guint32 token, rank, idx;
	guint8 *p = buf;
	int reftype;

	reftype = decode_value (p, &p);
	if (reftype == 0) {
		*endbuf = p;
		return NULL;
	}

	switch (reftype) {
	case MONO_AOT_TYPEREF_TYPEDEF_INDEX:
		idx = decode_value (p, &p);
		image = load_image (module, 0, TRUE);
		if (!image)
			return NULL;
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF + idx, &error);
		g_assert (mono_error_ok (&error));
		break;
	case MONO_AOT_TYPEREF_TYPEDEF_INDEX_IMAGE:
		idx = decode_value (p, &p);
		image = load_image (module, decode_value (p, &p), TRUE);
		if (!image)
			return NULL;
		klass = mono_class_get_checked (image, MONO_TOKEN_TYPE_DEF + idx, &error);
		g_assert (mono_error_ok (&error));
		break;
	case MONO_AOT_TYPEREF_TYPESPEC_TOKEN:
		token = decode_value (p, &p);
		image = module->assembly->image;
		if (!image)
			return NULL;
		klass = mono_class_get_checked (image, token, &error);
		g_assert (mono_error_ok (&error));
		break;
	case MONO_AOT_TYPEREF_GINST: {
		MonoClass *gclass;
		MonoGenericContext ctx;
		MonoType *type;

		gclass = decode_klass_ref (module, p, &p);
		if (!gclass)
			return NULL;
		g_assert (gclass->generic_container);

		memset (&ctx, 0, sizeof (ctx));
		ctx.class_inst = decode_generic_inst (module, p, &p);
		if (!ctx.class_inst)
			return NULL;
		type = mono_class_inflate_generic_type (&gclass->byval_arg, &ctx);
		klass = mono_class_from_mono_type (type);
		mono_metadata_free_type (type);
		break;
	}
	case MONO_AOT_TYPEREF_VAR: {
		MonoType *t;
		MonoGenericContainer *container = NULL;
		int type = decode_value (p, &p);
		int num = decode_value (p, &p);
		gboolean has_container = decode_value (p, &p);
		int serial = 0;

		if (has_container) {
			gboolean is_method = decode_value (p, &p);
			
			if (is_method) {
				MonoMethod *method_def;
				g_assert (type == MONO_TYPE_MVAR);
				method_def = decode_resolve_method_ref (module, p, &p);
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
		} else {
			serial = decode_value (p, &p);
		}

		t = g_new0 (MonoType, 1);
		t->type = type;
		if (container) {
			t->data.generic_param = mono_generic_container_get_param (container, num);
			g_assert (serial == 0);
		} else {
			/* Anonymous */
			MonoGenericParam *par = (MonoGenericParam*)mono_image_alloc0 (module->assembly->image, sizeof (MonoGenericParamFull));
			par->num = num;
			par->serial = serial;
			// FIXME:
			par->image = mono_defaults.corlib;
			t->data.generic_param = par;
		}

		// FIXME: Maybe use types directly to avoid
		// the overhead of creating MonoClass-es
		klass = mono_class_from_mono_type (t);

		g_free (t);
		break;
	}
	case MONO_AOT_TYPEREF_ARRAY:
		/* Array */
		rank = decode_value (p, &p);
		eklass = decode_klass_ref (module, p, &p);
		klass = mono_array_class_get (eklass, rank);
		break;
	case MONO_AOT_TYPEREF_PTR: {
		MonoType *t;

		t = decode_type (module, p, &p);
		if (!t)
			return NULL;
		klass = mono_class_from_mono_type (t);
		g_free (t);
		break;
	}
	case MONO_AOT_TYPEREF_BLOB_INDEX: {
		guint32 offset = decode_value (p, &p);
		guint8 *p2;

		p2 = module->blob + offset;
		klass = decode_klass_ref (module, p2, &p2);
		break;
	}
	default:
		g_assert_not_reached ();
	}
	g_assert (klass);
	//printf ("BLA: %s\n", mono_type_full_name (&klass->byval_arg));
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
 * Parse a MonoType encoded by encode_type () in aot-compiler.c. Return malloc-ed
 * memory.
 */
static MonoType*
decode_type (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	guint8 *p = buf;
	MonoType *t;

	t = g_malloc0 (sizeof (MonoType));

	while (TRUE) {
		if (*p == MONO_TYPE_PINNED) {
			t->pinned = TRUE;
			++p;
		} else if (*p == MONO_TYPE_BYREF) {
			t->byref = TRUE;
			++p;
		} else {
			break;
		}
	}

	t->type = *p;
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
		t->data.klass = decode_klass_ref (module, p, &p);
		break;
	case MONO_TYPE_SZARRAY:
		t->data.klass = decode_klass_ref (module, p, &p);

		if (!t->data.klass)
			return NULL;
		break;
	case MONO_TYPE_PTR:
		t->data.type = decode_type (module, p, &p);
		break;
	case MONO_TYPE_GENERICINST: {
		MonoClass *gclass;
		MonoGenericContext ctx;
		MonoType *type;
		MonoClass *klass;

		gclass = decode_klass_ref (module, p, &p);
		if (!gclass)
			return NULL;
		g_assert (gclass->generic_container);

		memset (&ctx, 0, sizeof (ctx));
		ctx.class_inst = decode_generic_inst (module, p, &p);
		if (!ctx.class_inst)
			return NULL;
		type = mono_class_inflate_generic_type (&gclass->byval_arg, &ctx);
		klass = mono_class_from_mono_type (type);
		t->data.generic_class = klass->generic_class;
		break;
	}
	case MONO_TYPE_ARRAY: {
		MonoArrayType *array;
		int i;

		// FIXME: memory management
		array = g_new0 (MonoArrayType, 1);
		array->eklass = decode_klass_ref (module, p, &p);
		if (!array->eklass)
			return NULL;
		array->rank = decode_value (p, &p);
		array->numsizes = decode_value (p, &p);

		if (array->numsizes)
			array->sizes = g_malloc0 (sizeof (int) * array->numsizes);
		for (i = 0; i < array->numsizes; ++i)
			array->sizes [i] = decode_value (p, &p);

		array->numlobounds = decode_value (p, &p);
		if (array->numlobounds)
			array->lobounds = g_malloc0 (sizeof (int) * array->numlobounds);
		for (i = 0; i < array->numlobounds; ++i)
			array->lobounds [i] = decode_value (p, &p);
		t->data.array = array;
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: {
		MonoClass *klass = decode_klass_ref (module, p, &p);
		if (!klass)
			return NULL;
		t->data.generic_param = klass->byval_arg.data.generic_param;
		break;
	}
	default:
		g_assert_not_reached ();
	}

	*endbuf = p;

	return t;
}

// FIXME: Error handling, memory management

static MonoMethodSignature*
decode_signature_with_target (MonoAotModule *module, MonoMethodSignature *target, guint8 *buf, guint8 **endbuf)
{
	MonoMethodSignature *sig;
	guint32 flags;
	int i, param_count, call_conv, gen_param_count = 0;
	guint8 *p = buf;
	gboolean hasthis, explicit_this, has_gen_params;

	flags = *p;
	p ++;
	has_gen_params = (flags & 0x10) != 0;
	hasthis = (flags & 0x20) != 0;
	explicit_this = (flags & 0x40) != 0;
	call_conv = flags & 0x0F;

	if (has_gen_params)
		gen_param_count = decode_value (p, &p);
	param_count = decode_value (p, &p);
	if (target && param_count != target->param_count)
		return NULL;
	sig = g_malloc0 (MONO_SIZEOF_METHOD_SIGNATURE + param_count * sizeof (MonoType *));
	sig->param_count = param_count;
	sig->sentinelpos = -1;
	sig->hasthis = hasthis;
	sig->explicit_this = explicit_this;
	sig->call_convention = call_conv;
	sig->param_count = param_count;
	sig->ret = decode_type (module, p, &p);
	for (i = 0; i < param_count; ++i) {
		if (*p == MONO_TYPE_SENTINEL) {
			g_assert (sig->call_convention == MONO_CALL_VARARG);
			sig->sentinelpos = i;
			p ++;
		}
		sig->params [i] = decode_type (module, p, &p);
	}

	if (sig->call_convention == MONO_CALL_VARARG && sig->sentinelpos == -1)
		sig->sentinelpos = sig->param_count;

	*endbuf = p;

	return sig;
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
	
	sig = decode_signature_with_target (module, mono_method_signature (target), p, &p);
	res = sig && mono_metadata_signature_equal (mono_method_signature (target), sig);
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
 */
static gboolean
decode_method_ref_with_target (MonoAotModule *module, MethodRef *ref, MonoMethod *target, guint8 *buf, guint8 **endbuf)
{
	guint32 image_index, value;
	MonoImage *image = NULL;
	guint8 *p = buf;

	memset (ref, 0, sizeof (MethodRef));

	value = decode_value (p, &p);
	image_index = value >> 24;

	if (image_index == MONO_AOT_METHODREF_NO_AOT_TRAMPOLINE) {
		ref->no_aot_trampoline = TRUE;
		value = decode_value (p, &p);
		image_index = value >> 24;
	}

	if (image_index < MONO_AOT_METHODREF_MIN || image_index == MONO_AOT_METHODREF_METHODSPEC || image_index == MONO_AOT_METHODREF_GINST) {
		if (target && target->wrapper_type)
			return FALSE;
	}

	if (image_index == MONO_AOT_METHODREF_WRAPPER) {
		guint32 wrapper_type;

		wrapper_type = decode_value (p, &p);

		if (target && target->wrapper_type != wrapper_type)
			return FALSE;

		/* Doesn't matter */
		image = mono_defaults.corlib;

		switch (wrapper_type) {
#ifndef DISABLE_REMOTING
		case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: {
			MonoMethod *m = decode_resolve_method_ref (module, p, &p);

			if (!m)
				return FALSE;
			mono_class_init (m->klass);
			ref->method = mono_marshal_get_remoting_invoke_with_check (m);
			break;
		}
		case MONO_WRAPPER_PROXY_ISINST: {
			MonoClass *klass = decode_klass_ref (module, p, &p);
			if (!klass)
				return FALSE;
			ref->method = mono_marshal_get_proxy_cancast (klass);
			break;
		}
		case MONO_WRAPPER_LDFLD:
		case MONO_WRAPPER_LDFLDA:
		case MONO_WRAPPER_STFLD:
		case MONO_WRAPPER_ISINST: {
			MonoClass *klass = decode_klass_ref (module, p, &p);
			if (!klass)
				return FALSE;
			if (wrapper_type == MONO_WRAPPER_LDFLD)
				ref->method = mono_marshal_get_ldfld_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_LDFLDA)
				ref->method = mono_marshal_get_ldflda_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_STFLD)
				ref->method = mono_marshal_get_stfld_wrapper (&klass->byval_arg);
			else if (wrapper_type == MONO_WRAPPER_ISINST)
				ref->method = mono_marshal_get_isinst (klass);
			else
				g_assert_not_reached ();
			break;
		}
		case MONO_WRAPPER_LDFLD_REMOTE:
			ref->method = mono_marshal_get_ldfld_remote_wrapper (NULL);
			break;
		case MONO_WRAPPER_STFLD_REMOTE:
			ref->method = mono_marshal_get_stfld_remote_wrapper (NULL);
			break;
#endif
		case MONO_WRAPPER_ALLOC: {
			int atype = decode_value (p, &p);

			ref->method = mono_gc_get_managed_allocator_by_type (atype);
			g_assert (ref->method);
			break;
		}
		case MONO_WRAPPER_WRITE_BARRIER:
			ref->method = mono_gc_get_write_barrier ();
			break;
		case MONO_WRAPPER_STELEMREF: {
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_NONE) {
				ref->method = mono_marshal_get_stelemref ();
			} else if (subtype == WRAPPER_SUBTYPE_VIRTUAL_STELEMREF) {
				int kind;
				WrapperInfo *info;
				
				kind = decode_value (p, &p);

				/* Can't decode this */
				if (!target)
					return FALSE;
				if (target->wrapper_type == MONO_WRAPPER_STELEMREF) {
					info = mono_marshal_get_wrapper_info (target);

					g_assert (info);
					if (info->subtype == subtype && info->d.virtual_stelemref.kind == kind)
						ref->method = target;
					else
						return FALSE;
				} else {
					return FALSE;
				}
			} else {
				g_assert_not_reached ();
			}
			break;
		}
		case MONO_WRAPPER_SYNCHRONIZED: {
			MonoMethod *m = decode_resolve_method_ref (module, p, &p);

			if (!m)
				return FALSE;
			ref->method = mono_marshal_get_synchronized_wrapper (m);
			break;
		}
		case MONO_WRAPPER_UNKNOWN: {
			MonoMethodDesc *desc;
			MonoMethod *orig_method;
			int subtype = decode_value (p, &p);

			if (subtype == WRAPPER_SUBTYPE_PTR_TO_STRUCTURE || subtype == WRAPPER_SUBTYPE_STRUCTURE_TO_PTR) {
				MonoClass *klass = decode_klass_ref (module, p, &p);
				
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
				MonoMethod *m = decode_resolve_method_ref (module, p, &p);

				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_synchronized_inner_wrapper (m);
			} else if (subtype == WRAPPER_SUBTYPE_ARRAY_ACCESSOR) {
				MonoMethod *m = decode_resolve_method_ref (module, p, &p);

				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_array_accessor_wrapper (m);
			} else if (subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN) {
				ref->method = mono_marshal_get_gsharedvt_in_wrapper ();
			} else if (subtype == WRAPPER_SUBTYPE_GSHAREDVT_OUT) {
				ref->method = mono_marshal_get_gsharedvt_out_wrapper ();
			} else {
				if (subtype == WRAPPER_SUBTYPE_FAST_MONITOR_ENTER)
					desc = mono_method_desc_new ("Monitor:Enter", FALSE);
				else if (subtype == WRAPPER_SUBTYPE_FAST_MONITOR_EXIT)
					desc = mono_method_desc_new ("Monitor:Exit", FALSE);
				else if (subtype == WRAPPER_SUBTYPE_FAST_MONITOR_ENTER_V4)
					desc = mono_method_desc_new ("Monitor:Enter(object,bool&)", FALSE);
				else
					g_assert_not_reached ();
				orig_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
				g_assert (orig_method);
				mono_method_desc_free (desc);
				ref->method = mono_monitor_get_fast_path (orig_method);
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
				WrapperInfo *info;
				MonoMethod *m;

				m = decode_resolve_method_ref (module, p, &p);
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
			}
			break;
		}
		case MONO_WRAPPER_MANAGED_TO_NATIVE: {
			MonoMethod *m;
			int subtype = decode_value (p, &p);
			char *name;

			if (subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER) {
				if (!target)
					return FALSE;

				name = (char*)p;
				if (strcmp (target->name, name) != 0)
					return FALSE;
				ref->method = target;
			} else {
				m = decode_resolve_method_ref (module, p, &p);

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
			else
				g_assert_not_reached ();
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
				MonoMethod *m = decode_resolve_method_ref (module, p, &p);

				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_runtime_invoke (m, FALSE);
			} else if (subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL) {
				/* Virtual direct wrapper */
				MonoMethod *m = decode_resolve_method_ref (module, p, &p);

				if (!m)
					return FALSE;
				ref->method = mono_marshal_get_runtime_invoke (m, TRUE);
			} else {
				MonoMethodSignature *sig;
				WrapperInfo *info;

				sig = decode_signature_with_target (module, NULL, p, &p);
				info = mono_marshal_get_wrapper_info (target);
				g_assert (info);

				if (info->subtype != subtype)
					return FALSE;
				g_assert (info->d.runtime_invoke.sig);
				if (mono_metadata_signature_equal (sig, info->d.runtime_invoke.sig))
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

				klass = decode_klass_ref (module, p, &p);
				if (!klass)
					return FALSE;

				switch (wrapper_type) {
				case MONO_WRAPPER_DELEGATE_INVOKE:
					invoke = mono_get_delegate_invoke (klass);
					wrapper = mono_marshal_get_delegate_invoke (invoke, NULL);
					break;
				case MONO_WRAPPER_DELEGATE_BEGIN_INVOKE:
					invoke = mono_get_delegate_begin_invoke (klass);
					wrapper = mono_marshal_get_delegate_begin_invoke (invoke);
					break;
				case MONO_WRAPPER_DELEGATE_END_INVOKE:
					invoke = mono_get_delegate_end_invoke (klass);
					wrapper = mono_marshal_get_delegate_end_invoke (invoke);
					break;
				default:
					g_assert_not_reached ();
					break;
				}
				if (target && wrapper != target)
					return FALSE;
				ref->method = wrapper;
			} else {
				/*
				 * These wrappers are associated with a signature, not with a method.
				 * Since we can't decode them into methods, they need a target method.
				 */
				if (!target)
					return FALSE;

				if (wrapper_type == MONO_WRAPPER_DELEGATE_INVOKE) {
					WrapperInfo *info;

					subtype = decode_value (p, &p);
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

			m = decode_resolve_method_ref (module, p, &p);
			if (!m)
				return FALSE;
			klass = decode_klass_ref (module, p, &p);
			if (!klass)
				return FALSE;
			ref->method = mono_marshal_get_managed_wrapper (m, klass, 0);
			break;
		}
		default:
			g_assert_not_reached ();
		}
	} else if (image_index == MONO_AOT_METHODREF_METHODSPEC) {
		image_index = decode_value (p, &p);
		ref->token = decode_value (p, &p);

		image = load_image (module, image_index, TRUE);
		if (!image)
			return FALSE;
	} else if (image_index == MONO_AOT_METHODREF_GINST) {
		MonoClass *klass;
		MonoGenericContext ctx;

		/* 
		 * These methods do not have a token which resolves them, so we 
		 * resolve them immediately.
		 */
		klass = decode_klass_ref (module, p, &p);
		if (!klass)
			return FALSE;

		if (target && target->klass != klass)
			return FALSE;

		image_index = decode_value (p, &p);
		ref->token = decode_value (p, &p);

		image = load_image (module, image_index, TRUE);
		if (!image)
			return FALSE;

		ref->method = mono_get_method_full (image, ref->token, NULL, NULL);
		if (!ref->method)
			return FALSE;

		memset (&ctx, 0, sizeof (ctx));

		if (FALSE && klass->generic_class) {
			ctx.class_inst = klass->generic_class->context.class_inst;
			ctx.method_inst = NULL;
 
			ref->method = mono_class_inflate_generic_method_full (ref->method, klass, &ctx);
		}			

		memset (&ctx, 0, sizeof (ctx));

		if (!decode_generic_context (module, &ctx, p, &p))
			return FALSE;

		ref->method = mono_class_inflate_generic_method_full (ref->method, klass, &ctx);
	} else if (image_index == MONO_AOT_METHODREF_ARRAY) {
		MonoClass *klass;
		int method_type;

		klass = decode_klass_ref (module, p, &p);
		if (!klass)
			return FALSE;
		method_type = decode_value (p, &p);
		switch (method_type) {
		case 0:
			ref->method = mono_class_get_method_from_name (klass, ".ctor", klass->rank);
			break;
		case 1:
			ref->method = mono_class_get_method_from_name (klass, ".ctor", klass->rank * 2);
			break;
		case 2:
			ref->method = mono_class_get_method_from_name (klass, "Get", -1);
			break;
		case 3:
			ref->method = mono_class_get_method_from_name (klass, "Address", -1);
			break;
		case 4:
			ref->method = mono_class_get_method_from_name (klass, "Set", -1);
			break;
		default:
			g_assert_not_reached ();
		}
	} else {
		if (image_index == MONO_AOT_METHODREF_LARGE_IMAGE_INDEX) {
			image_index = decode_value (p, &p);
			value = decode_value (p, &p);
		}

		ref->token = MONO_TOKEN_METHOD_DEF | (value & 0xffffff);

		image = load_image (module, image_index, TRUE);
		if (!image)
			return FALSE;
	}

	*endbuf = p;

	ref->image = image;

	return TRUE;
}

static gboolean
decode_method_ref (MonoAotModule *module, MethodRef *ref, guint8 *buf, guint8 **endbuf)
{
	return decode_method_ref_with_target (module, ref, NULL, buf, endbuf);
}

/*
 * decode_resolve_method_ref_with_target:
 *
 *   Similar to decode_method_ref, but resolve and return the method itself.
 */
static MonoMethod*
decode_resolve_method_ref_with_target (MonoAotModule *module, MonoMethod *target, guint8 *buf, guint8 **endbuf)
{
	MethodRef ref;
	gboolean res;

	res = decode_method_ref_with_target (module, &ref, target, buf, endbuf);
	if (!res)
		return NULL;
	if (ref.method)
		return ref.method;
	if (!ref.image)
		return NULL;
	return mono_get_method (ref.image, ref.token, NULL);
}

static MonoMethod*
decode_resolve_method_ref (MonoAotModule *module, guint8 *buf, guint8 **endbuf)
{
	return decode_resolve_method_ref_with_target (module, NULL, buf, endbuf);
}

#ifdef ENABLE_AOT_CACHE

/* AOT CACHE */

/*
 * FIXME:
 * - Add options for controlling the cache size
 * - Handle full cache by deleting old assemblies lru style
 * - Maybe add a threshold after an assembly is AOT compiled
 * - Add options for enabling this for specific main assemblies
 */

/* The cache directory */
static char *cache_dir;

/* The number of assemblies AOTed in this run */
static int cache_count;

/* Whenever to AOT in-process */
static gboolean in_process;

static void
collect_assemblies (gpointer data, gpointer user_data)
{
	MonoAssembly *ass = data;
	GSList **l = user_data;

	*l = g_slist_prepend (*l, ass);
}

#define SHA1_DIGEST_LENGTH 20

/*
 * get_aot_config_hash:
 *
 *   Return a hash for all the version information an AOT module depends on.
 */
static G_GNUC_UNUSED char*
get_aot_config_hash (MonoAssembly *assembly)
{
	char *build_info;
	GSList *l, *assembly_list = NULL;
	GString *s;
	int i;
	guint8 digest [SHA1_DIGEST_LENGTH];
	char *digest_str;

	build_info = mono_get_runtime_build_info ();

	s = g_string_new (build_info);

	mono_assembly_foreach (collect_assemblies, &assembly_list);

	/*
	 * The assembly list includes the current assembly as well, no need
	 * to add it.
	 */
	for (l = assembly_list; l; l = l->next) {
		MonoAssembly *ass = l->data;

		g_string_append (s, "_");
		g_string_append (s, ass->aname.name);
		g_string_append (s, "_");
		g_string_append (s, ass->image->guid);
	}

	for (i = 0; i < s->len; ++i) {
		if (!isalnum (s->str [i]) && s->str [i] != '-')
			s->str [i] = '_';
	}

	mono_sha1_get_digest ((guint8*)s->str, s->len, digest);

	digest_str = g_malloc0 ((SHA1_DIGEST_LENGTH * 2) + 1);
	for (i = 0; i < SHA1_DIGEST_LENGTH; ++i)
		sprintf (digest_str + (i * 2), "%02x", digest [i]);

	mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: file dependencies: %s, hash %s", s->str, digest_str);

	g_string_free (s, TRUE);

	return digest_str;
}

static void
aot_cache_init (void)
{
	if (mono_aot_only)
		return;
	enable_aot_cache = TRUE;
	in_process = TRUE;
}

/*
 * aot_cache_load_module:
 *
 *   Load the AOT image corresponding to ASSEMBLY from the aot cache, AOTing it if neccessary.
 */
static MonoDl*
aot_cache_load_module (MonoAssembly *assembly, char **aot_name)
{
	MonoAotCacheConfig *config;
	GSList *l;
	char *fname, *tmp2, *aot_options, *failure_fname;
	const char *home;
	MonoDl *module;
	gboolean res;
	gint exit_status;
	char *hash;
	int pid;
	gboolean enabled;
	FILE *failure_file;

	*aot_name = NULL;

	if (image_is_dynamic (assembly->image))
		return NULL;

	/* Check in the list of assemblies enabled for aot caching */
	config = mono_get_aot_cache_config ();

	enabled = FALSE;
	if (config->apps) {
		MonoDomain *domain = mono_domain_get ();
		MonoAssembly *entry_assembly = domain->entry_assembly;

		// FIXME: This cannot be used for mscorlib during startup, since entry_assembly is not set yet
		for (l = config->apps; l; l = l->next) {
			char *n = l->data;

			if ((entry_assembly && !strcmp (entry_assembly->aname.name, n)) || (!entry_assembly && !strcmp (assembly->aname.name, n)))
				break;
		}
		if (l)
			enabled = TRUE;
	}

	if (!enabled) {
		for (l = config->assemblies; l; l = l->next) {
			char *n = l->data;

			if (!strcmp (assembly->aname.name, n))
				break;
		}
		if (l)
			enabled = TRUE;
	}
	if (!enabled)
		return NULL;

	if (!cache_dir) {
		home = g_get_home_dir ();
		if (!home)
			return NULL;
		cache_dir = g_strdup_printf ("%s/Library/Caches/mono/aot-cache", home);
		if (!g_file_test (cache_dir, G_FILE_TEST_EXISTS|G_FILE_TEST_IS_DIR))
			g_mkdir_with_parents (cache_dir, 0777);
	}

	/*
	 * The same assembly can be used in multiple configurations, i.e. multiple
     * versions of the runtime, with multiple versions of dependent assemblies etc.
	 * To handle this, we compute a version string containing all this information, hash it,
	 * and use the hash as a filename suffix.
	 */
	hash = get_aot_config_hash (assembly);

	tmp2 = g_strdup_printf ("%s-%s%s", assembly->image->assembly_name, hash, SHARED_EXT);
	fname = g_build_filename (cache_dir, tmp2, NULL);
	*aot_name = fname;
	g_free (tmp2);

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: loading from cache: '%s'.", fname);
	module = mono_dl_open (fname, MONO_DL_LAZY, NULL);

	if (module) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: found in cache: '%s'.", fname);
		return module;
	}

	if (!strcmp (assembly->aname.name, "mscorlib") && !mscorlib_aot_loaded)
		/*
		 * Can't AOT this during startup, so we AOT it when called later from
		 * mono_aot_get_method ().
		 */
		return NULL;

	mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: not found.");

	/* Only AOT one assembly per run to avoid slowing down execution too much */
	if (cache_count > 0)
		return NULL;
	cache_count ++;

	/* Check for previous failure */
	failure_fname = g_strdup_printf ("%s.failure", fname);
	failure_file = fopen (failure_fname, "r");
	if (failure_file) {
		mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: assembly '%s' previously failed to compile '%s' ('%s')... ", assembly->image->name, fname, failure_fname);
		g_free (failure_fname);
		return NULL;
	} else {
		g_free (failure_fname);
		fclose (failure_file);
	}

	mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: compiling assembly '%s', logfile: '%s.log'... ", assembly->image->name, fname);

	/*
	 * We need to invoke the AOT compiler here. There are multiple approaches:
	 * - spawn a new runtime process. This can be hard when running with mkbundle, and
	 * its hard to make the new process load the same set of assemblies.
	 * - doing it in-process. This exposes the current process to bugs/leaks/side effects of
	 * the AOT compiler.
	 * - fork a new process and do the work there.
	 */
	if (in_process) {
		aot_options = g_strdup_printf ("outfile=%s,internal-logfile=%s.log%s%s", fname, fname, config->aot_options ? "," : "", config->aot_options ? config->aot_options : "");
		/* Maybe due this in another thread ? */
		res = mono_compile_assembly (assembly, mono_parse_default_optimizations (NULL), aot_options);
		if (res) {
			mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: compilation failed.");
			failure_fname = g_strdup_printf ("%s.failure", fname);
			failure_file = fopen (failure_fname, "a+");
			fclose (failure_file);
			g_free (failure_fname);
		} else {
			mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: compilation succeeded.");
		}
	} else {
		/*
		 * - Avoid waiting for the aot process to finish ?
		 *   (less overhead, but multiple processes could aot the same assembly at the same time)
		 */
		pid = fork ();
		if (pid == 0) {
			FILE *logfile;
			char *logfile_name;

			/* Child */

			logfile_name = g_strdup_printf ("%s/aot.log", cache_dir);
			logfile = fopen (logfile_name, "a+");
			g_free (logfile_name);

			dup2 (fileno (logfile), 1);
			dup2 (fileno (logfile), 2);

			aot_options = g_strdup_printf ("outfile=%s", fname);
			res = mono_compile_assembly (assembly, mono_parse_default_optimizations (NULL), aot_options);
			if (!res) {
				exit (1);
			} else {
				exit (0);
			}
		} else {
			/* Parent */
			waitpid (pid, &exit_status, 0);
			if (!WIFEXITED (exit_status) && (WEXITSTATUS (exit_status) == 0))
				mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: failed.");
			else
				mono_trace (G_LOG_LEVEL_MESSAGE, MONO_TRACE_AOT, "AOT: succeeded.");
		}
	}

	module = mono_dl_open (fname, MONO_DL_LAZY, NULL);

	return module;
}

#else

static void
aot_cache_init (void)
{
}

static MonoDl*
aot_cache_load_module (MonoAssembly *assembly, char **aot_name)
{
	return NULL;
}

#endif

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
		table = globals [0];
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

			if (!strcmp (globals [index * 2], symbol)) {
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
		char *err = mono_dl_symbol (module, name, value);

		if (err)
			g_free (err);
	}
}

static gboolean
check_usable (MonoAssembly *assembly, MonoAotFileInfo *info, char **out_msg)
{
	char *build_info;
	char *msg = NULL;
	gboolean usable = TRUE;
	gboolean full_aot;
	guint8 *blob;
	guint32 excluded_cpu_optimizations;

	if (strcmp (assembly->image->guid, info->assembly_guid)) {
		msg = g_strdup_printf ("doesn't match assembly");
		usable = FALSE;
	}

	build_info = mono_get_runtime_build_info ();
	if (strlen (info->runtime_version) > 0 && strcmp (info->runtime_version, build_info)) {
		msg = g_strdup_printf ("compiled against runtime version '%s' while this runtime has version '%s'", info->runtime_version, build_info);
		usable = FALSE;
	}
	g_free (build_info);

	full_aot = info->flags & MONO_AOT_FILE_FLAG_FULL_AOT;

	if (mono_aot_only && !full_aot) {
		msg = g_strdup_printf ("not compiled with --aot=full");
		usable = FALSE;
	}
	if (!mono_aot_only && full_aot) {
		msg = g_strdup_printf ("compiled with --aot=full");
		usable = FALSE;
	}
#ifdef TARGET_ARM
	/* mono_arch_find_imt_method () requires this */
	if ((info->flags & MONO_AOT_FILE_FLAG_WITH_LLVM) && !mono_use_llvm) {
		msg = g_strdup_printf ("compiled against LLVM");
		usable = FALSE;
	}
	if (!(info->flags & MONO_AOT_FILE_FLAG_WITH_LLVM) && mono_use_llvm) {
		msg = g_strdup_printf ("not compiled against LLVM");
		usable = FALSE;
	}
#endif
	if (mini_get_debug_options ()->mdb_optimizations && !(info->flags & MONO_AOT_FILE_FLAG_DEBUG) && !full_aot) {
		msg = g_strdup_printf ("not compiled for debugging");
		usable = FALSE;
	}

	mono_arch_cpu_optimizations (&excluded_cpu_optimizations);
	if (info->opts & excluded_cpu_optimizations) {
		msg = g_strdup_printf ("compiled with unsupported CPU optimizations");
		usable = FALSE;
	}

	if (!mono_aot_only && (info->simd_opts & ~mono_arch_cpu_enumerate_simd_versions ())) {
		msg = g_strdup_printf ("compiled with unsupported SIMD extensions");
		usable = FALSE;
	}

	blob = info->blob;

	if (info->gc_name_index != -1) {
		char *gc_name = (char*)&blob [info->gc_name_index];
		const char *current_gc_name = mono_gc_get_gc_name ();

		if (strcmp (current_gc_name, gc_name) != 0) {
			msg = g_strdup_printf ("compiled against GC %s, while the current runtime uses GC %s.\n", gc_name, current_gc_name);
			usable = FALSE;
		}
	}

	*out_msg = msg;
	return usable;
}

/* This returns an interop address */
static void*
get_arm_bl_target (guint32 *ins_addr)
{
#ifdef TARGET_ARM
	guint32 ins = *ins_addr;
	gint32 offset;

	if ((ins >> ARMCOND_SHIFT) == ARMCOND_NV) {
		/* blx */
		offset = (((int)(((ins & 0xffffff) << 1) | ((ins >> 24) & 0x1))) << 7) >> 7;
		return (char*)ins_addr + (offset * 2) + 8 + 1;
	} else {
		offset = (((int)ins & 0xffffff) << 8) >> 8;
		return (char*)ins_addr + (offset * 4) + 8;
	}
#elif defined(TARGET_ARM64)
	return mono_arch_get_call_target (((guint8*)ins_addr) + 4);
#else
	g_assert_not_reached ();
	return NULL;
#endif
}

static void
load_aot_module (MonoAssembly *assembly, gpointer user_data)
{
	char *aot_name;
	MonoAotModule *amodule;
	MonoDl *sofile;
	gboolean usable = TRUE;
	char *version_symbol = NULL;
	char *msg = NULL;
	gpointer *globals = NULL;
	MonoAotFileInfo *info = NULL;
	int i, version;
	guint8 *blob;
	gboolean do_load_image = TRUE;
	int align_double, align_int64;

	if (mono_compile_aot)
		return;

	if (assembly->image->aot_module)
		/* 
		 * Already loaded. This can happen because the assembly loading code might invoke
		 * the assembly load hooks multiple times for the same assembly.
		 */
		return;

	if (image_is_dynamic (assembly->image) || assembly->ref_only)
		return;

	if (mono_security_cas_enabled ())
		return;

	mono_aot_lock ();
	if (static_aot_modules)
		info = g_hash_table_lookup (static_aot_modules, assembly->aname.name);
	else
		info = NULL;
	mono_aot_unlock ();

	sofile = NULL;

	if (info) {
		/* Statically linked AOT module */
		aot_name = g_strdup_printf ("%s", assembly->aname.name);
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "Found statically linked AOT module '%s'.\n", aot_name);
		globals = info->globals;
	} else {
		if (enable_aot_cache)
			sofile = aot_cache_load_module (assembly, &aot_name);
		if (!sofile) {
			char *err;
			aot_name = g_strdup_printf ("%s%s", assembly->image->name, SHARED_EXT);

			sofile = mono_dl_open (aot_name, MONO_DL_LAZY, &err);

			if (!sofile) {
				mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT module '%s' not found: %s\n", aot_name, err);
				g_free (err);
			}
		}
	}

	if (!sofile && !globals) {
		if (mono_aot_only && assembly->image->tables [MONO_TABLE_METHOD].rows) {
			fprintf (stderr, "Failed to load AOT module '%s' in aot-only mode.\n", aot_name);
			exit (1);
		}
		g_free (aot_name);
		return;
	}

	if (!info) {
		find_symbol (sofile, globals, "mono_aot_version", (gpointer *) &version_symbol);
		find_symbol (sofile, globals, "mono_aot_file_info", (gpointer*)&info);
	}

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
		usable = check_usable (assembly, info, &msg);
	}

	if (!usable) {
		if (mono_aot_only) {
			fprintf (stderr, "Failed to load AOT module '%s' while running in aot-only mode: %s.\n", aot_name, msg);
			exit (1);
		} else {
			mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: module %s is unusable: %s.\n", aot_name, msg);
		}
		g_free (msg);
		g_free (aot_name);
		if (sofile)
			mono_dl_close (sofile);
		assembly->image->aot_module = NULL;
		return;
	}

#if defined (TARGET_ARM) && defined (TARGET_MACH)
	{
		MonoType t;
		int align = 0;

		memset (&t, 0, sizeof (MonoType));
		t.type = MONO_TYPE_R8;
		mono_type_size (&t, &align);
		align_double = align;

		memset (&t, 0, sizeof (MonoType));
		t.type = MONO_TYPE_I8;
		align_int64 = align;
	}
#else
	align_double = MONO_ABI_ALIGNOF (double);
	align_int64 = MONO_ABI_ALIGNOF (gint64);
#endif

	/* Sanity check */
	g_assert (info->double_align == align_double);
	g_assert (info->long_align == align_int64);
	g_assert (info->generic_tramp_num == MONO_TRAMPOLINE_NUM);

	blob = info->blob;

	amodule = g_new0 (MonoAotModule, 1);
	amodule->aot_name = aot_name;
	amodule->assembly = assembly;

	memcpy (&amodule->info, info, sizeof (*info));

	amodule->got = amodule->info.got;
	amodule->got [0] = assembly->image;
	amodule->globals = globals;
	amodule->sofile = sofile;
	amodule->method_to_code = g_hash_table_new (mono_aligned_addr_hash, NULL);
	amodule->blob = blob;

	mono_mutex_init_recursive (&amodule->mutex);

	/* Read image table */
	{
		guint32 table_len, i;
		char *table = NULL;

		table = info->image_table;
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

	amodule->code_offsets = info->code_offsets;
	amodule->method_addresses = info->method_addresses;
	amodule->code = info->methods;
#ifdef TARGET_ARM
	/* Mask out thumb interop bit */
	amodule->code = (void*)((mgreg_t)amodule->code & ~1);
#endif
	amodule->code_end = info->methods_end;
	amodule->method_info_offsets = info->method_info_offsets;
	amodule->ex_info_offsets = info->ex_info_offsets;
	amodule->class_info_offsets = info->class_info_offsets;
	amodule->class_name_table = info->class_name_table;
	amodule->extra_method_table = info->extra_method_table;
	amodule->extra_method_info_offsets = info->extra_method_info_offsets;
	amodule->unbox_trampolines = info->unbox_trampolines;
	amodule->unbox_trampolines_end = info->unbox_trampolines_end;
	amodule->got_info_offsets = info->got_info_offsets;
	amodule->unwind_info = info->unwind_info;
	amodule->mem_end = info->mem_end;
	amodule->mem_begin = amodule->code;
	amodule->plt = info->plt;
	amodule->plt_end = info->plt_end;
	amodule->mono_eh_frame = info->mono_eh_frame;
	amodule->trampolines [MONO_AOT_TRAMP_SPECIFIC] = info->specific_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_STATIC_RGCTX] = info->static_rgctx_trampolines;
	amodule->trampolines [MONO_AOT_TRAMP_IMT_THUNK] = info->imt_thunks;
	amodule->trampolines [MONO_AOT_TRAMP_GSHAREDVT_ARG] = info->gsharedvt_arg_trampolines;
	amodule->thumb_end = info->thumb_end;

	if (info->flags & MONO_AOT_FILE_FLAG_DIRECT_METHOD_ADDRESSES) {
		/* Compute code_offsets from the method addresses */
		amodule->code_offsets = g_malloc0 (amodule->info.nmethods * sizeof (gint32));
		for (i = 0; i < amodule->info.nmethods; ++i) {
			/* method_addresses () contains a table of branches, since the ios linker can update those correctly */
			void *addr = NULL;

#if defined(TARGET_ARM) || defined(TARGET_ARM64)
			addr = get_arm_bl_target ((guint32*)amodule->method_addresses + i);
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
			addr = mono_arch_get_call_target ((guint8*)amodule->method_addresses + (i * 5) + 5);
#else
			g_assert_not_reached ();
#endif
			g_assert (addr);
			if (addr == amodule->method_addresses)
				amodule->code_offsets [i] = 0xffffffff;
			else
				amodule->code_offsets [i] = (char*)addr - (char*)amodule->code;
		}
	}

	if (make_unreadable) {
#ifndef TARGET_WIN32
		guint8 *addr;
		guint8 *page_start, *page_end;
		int err, len;

		addr = amodule->mem_begin;
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

	mono_aot_lock ();

	aot_code_low_addr = MIN (aot_code_low_addr, (gsize)amodule->code);
	aot_code_high_addr = MAX (aot_code_high_addr, (gsize)amodule->code_end);

	g_hash_table_insert (aot_modules, assembly, amodule);
	mono_aot_unlock ();

	mono_jit_info_add_aot_module (assembly->image, amodule->code, amodule->code_end);

	assembly->image->aot_module = amodule;

	if (mono_aot_only) {
		char *code;
		find_symbol (amodule->sofile, amodule->globals, "specific_trampolines_page", (gpointer *)&code);
		amodule->use_page_trampolines = code != NULL;
		/*g_warning ("using page trampolines: %d", amodule->use_page_trampolines);*/
		if (mono_defaults.corlib) {
			/* The second got slot contains the mscorlib got addr */
			MonoAotModule *mscorlib_amodule = mono_defaults.corlib->aot_module;

			amodule->got [1] = mscorlib_amodule->got;
		} else {
			amodule->got [1] = amodule->got;
		}
	}

	if (mono_gc_is_moving ()) {
		MonoJumpInfo ji;

		memset (&ji, 0, sizeof (ji));
		ji.type = MONO_PATCH_INFO_GC_CARD_TABLE_ADDR;

		amodule->got [2] = mono_resolve_patch_target (NULL, mono_get_root_domain (), NULL, &ji, FALSE);
	}

	/*
	 * Since we store methoddef and classdef tokens when referring to methods/classes in
	 * referenced assemblies, we depend on the exact versions of the referenced assemblies.
	 * MS calls this 'hard binding'. This means we have to load all referenced assemblies
	 * non-lazily, since we can't handle out-of-date errors later.
	 * The cached class info also depends on the exact assemblies.
	 */
#if defined(__native_client__)
	/* TODO: Don't 'load_image' on mscorlib due to a */
	/* recursive loading problem.  This should be    */
	/* removed if mscorlib is loaded from disk.      */
	if (strncmp(assembly->aname.name, "mscorlib", 8)) {
		do_load_image = TRUE;
	} else {
		do_load_image = FALSE;
	}
#endif
	if (do_load_image) {
		for (i = 0; i < amodule->image_table_len; ++i)
			load_image (amodule, i, FALSE);
	}

	if (amodule->out_of_date) {
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: Module %s is unusable because a dependency is out-of-date.\n", assembly->image->name);
		if (mono_aot_only) {
			fprintf (stderr, "Failed to load AOT module '%s' while running in aot-only mode because a dependency cannot be found or it is out of date.\n", aot_name);
			exit (1);
		}
	}
	else
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_AOT, "AOT: loaded AOT Module for %s.\n", assembly->image->name);
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
	MonoAotFileInfo *info = (gpointer)aot_info;

	g_assert (info->version == MONO_AOT_FILE_VERSION);

	globals = info->globals;
	g_assert (globals);

	aname = info->assembly_name;

	/* This could be called before startup */
	if (aot_modules)
		mono_aot_lock ();

	if (!static_aot_modules)
		static_aot_modules = g_hash_table_new (g_str_hash, g_str_equal);

	g_hash_table_insert (static_aot_modules, aname, info);

	if (aot_modules)
		mono_aot_unlock ();
}

void
mono_aot_init (void)
{
	mono_mutex_init_recursive (&aot_mutex);
	mono_mutex_init_recursive (&aot_page_mutex);
	aot_modules = g_hash_table_new (NULL, NULL);

#ifndef __native_client__
	mono_install_assembly_load_hook (load_aot_module, NULL);
#endif
	mono_counters_register ("Async JIT info size", MONO_COUNTER_INT|MONO_COUNTER_JIT, &async_jit_info_size);

	if (g_getenv ("MONO_LASTAOT"))
		mono_last_aot_method = atoi (g_getenv ("MONO_LASTAOT"));
	aot_cache_init ();
}

void
mono_aot_cleanup (void)
{
	if (aot_jit_icall_hash)
		g_hash_table_destroy (aot_jit_icall_hash);
	if (aot_modules)
		g_hash_table_destroy (aot_modules);
}

static gboolean
decode_cached_class_info (MonoAotModule *module, MonoCachedClassInfo *info, guint8 *buf, guint8 **endbuf)
{
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

	if (info->has_cctor) {
		res = decode_method_ref (module, &ref, buf, &buf);
		if (!res)
			return FALSE;
		info->cctor_token = ref.token;
	}
	if (info->has_finalize) {
		res = decode_method_ref (module, &ref, buf, &buf);
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
mono_aot_get_method_from_vt_slot (MonoDomain *domain, MonoVTable *vtable, int slot)
{
	int i;
	MonoClass *klass = vtable->klass;
	MonoAotModule *amodule = klass->image->aot_module;
	guint8 *info, *p;
	MonoCachedClassInfo class_info;
	gboolean err;
	MethodRef ref;
	gboolean res;

	if (MONO_CLASS_IS_INTERFACE (klass) || klass->rank || !amodule)
		return NULL;

	info = &amodule->blob [mono_aot_get_offset (amodule->class_info_offsets, mono_metadata_token_index (klass->type_token) - 1)];
	p = info;

	err = decode_cached_class_info (amodule, &class_info, p, &p);
	if (!err)
		return NULL;

	for (i = 0; i < slot; ++i)
		decode_method_ref (amodule, &ref, p, &p);

	res = decode_method_ref (amodule, &ref, p, &p);
	if (!res)
		return NULL;
	if (ref.no_aot_trampoline)
		return NULL;

	if (mono_metadata_token_index (ref.token) == 0 || mono_metadata_token_table (ref.token) != MONO_TABLE_METHOD)
		return NULL;

	return mono_aot_get_method_from_token (domain, ref.image, ref.token);
}

gboolean
mono_aot_get_cached_class_info (MonoClass *klass, MonoCachedClassInfo *res)
{
	MonoAotModule *amodule = klass->image->aot_module;
	guint8 *p;
	gboolean err;

	if (klass->rank || !amodule)
		return FALSE;

	p = (guint8*)&amodule->blob [mono_aot_get_offset (amodule->class_info_offsets, mono_metadata_token_index (klass->type_token) - 1)];

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
	nspace_table = g_hash_table_lookup (amodule->name_cache, name_space);
	if (nspace_table) {
		*klass = g_hash_table_lookup (nspace_table, name);
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
				MonoError error;
				amodule_unlock (amodule);
				*klass = mono_class_get_checked (image, token, &error);
				if (!mono_error_ok (&error))
					mono_error_cleanup (&error); /* FIXME don't swallow the error */

				/* Add to cache */
				if (*klass) {
					amodule_lock (amodule);
					nspace_table = g_hash_table_lookup (amodule->name_cache, name_space);
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

/*
 * decode_mono_eh_frame:
 *
 *   Decode the EH information emitted by our modified LLVM compiler and construct a
 * MonoJitInfo structure from it.
 * LOCKING: Acquires the domain lock.
 */
static MonoJitInfo*
decode_llvm_mono_eh_frame (MonoAotModule *amodule, MonoDomain *domain, 
						   MonoMethod *method, guint8 *code, 
						   MonoJitExceptionInfo *clauses, int num_clauses,
						   MonoJitInfoFlags flags,
						   GSList **nesting,
						   int *this_reg, int *this_offset)
{
	guint8 *p;
	guint8 *fde, *cie, *code_start, *code_end;
	int version, fde_count;
	gint32 *table;
	int i, j, pos, left, right, offset, offset1, offset2, code_len, func_encoding;
	MonoJitExceptionInfo *ei;
	guint32 fde_len, ei_len, nested_len, nindex;
	gpointer *type_info;
	MonoJitInfo *jinfo;
	MonoLLVMFDEInfo info;

	g_assert (amodule->mono_eh_frame);

	p = amodule->mono_eh_frame;

	/* p points to data emitted by LLVM in DwarfException::EmitMonoEHFrame () */

	/* Header */
	version = *p;
	g_assert (version == 3);
	p ++;
	func_encoding = *p;
	p ++;
	p = ALIGN_PTR_TO (p, 4);

	fde_count = *(guint32*)p;
	p += 4;
	table = (gint32*)p;

	/* There is +1 entry in the table */
	cie = p + ((fde_count + 1) * 8);

	/* Binary search in the table to find the entry for code */
	offset = code - amodule->code;
	left = 0;
	right = fde_count;
	while (TRUE) {
		pos = (left + right) / 2;

		/* The table contains method index/fde offset pairs */
		g_assert (table [(pos * 2)] != -1);
		offset1 = amodule->code_offsets [table [(pos * 2)]];
		if (pos + 1 == fde_count) {
			offset2 = amodule->code_end - amodule->code;
		} else {
			g_assert (table [(pos + 1) * 2] != -1);
			offset2 = amodule->code_offsets [table [(pos + 1) * 2]];
		}

		if (offset < offset1)
			right = pos;
		else if (offset >= offset2)
			left = pos + 1;
		else
			break;
	}

	code_start = amodule->code + amodule->code_offsets [table [(pos * 2)]];
	if (pos + 1 == fde_count) {
		/* The +1 entry in the table contains the length of the last method */
		int len = table [(pos + 1) * 2];
		code_end = code_start + len;
	} else {
		code_end = amodule->code + amodule->code_offsets [table [(pos + 1) * 2]];
	}
	code_len = code_end - code_start;

	g_assert (code >= code_start && code < code_end);

	if (amodule->thumb_end && (guint8*)code_start < amodule->thumb_end)
		/* Clear thumb flag */
		code_start = (guint8*)(((mgreg_t)code_start) & ~1);

	fde = amodule->mono_eh_frame + table [(pos * 2) + 1];	
	/* This won't overflow because there is +1 entry in the table */
	fde_len = table [(pos * 2) + 2 + 1] - table [(pos * 2) + 1];

	mono_unwind_decode_llvm_mono_fde (fde, fde_len, cie, code_start, &info);
	ei = info.ex_info;
	ei_len = info.ex_info_len;
	type_info = info.type_info;
	*this_reg = info.this_reg;
	*this_offset = info.this_offset;

	/* Count number of nested clauses */
	nested_len = 0;
	for (i = 0; i < ei_len; ++i) {
		/* This might be unaligned */
		gint32 cindex1 = read32 (type_info [i]);
		GSList *l;

		for (l = nesting [cindex1]; l; l = l->next) {
			gint32 nesting_cindex = GPOINTER_TO_INT (l->data);

			for (j = 0; j < ei_len; ++j) {
				gint32 cindex2 = read32 (type_info [j]);

				if (cindex2 == nesting_cindex)
					nested_len ++;
			}
		}
	}

	/*
	 * LLVM might represent one IL region with multiple regions, so have to
	 * allocate a new JI.
	 */
	jinfo = 
		mono_domain_alloc0_lock_free (domain, mono_jit_info_size (flags, ei_len + nested_len, 0));
	mono_jit_info_init (jinfo, method, code, code_len, flags, ei_len + nested_len, 0);

	jinfo->unwind_info = mono_cache_unwind_info (info.unw_info, info.unw_info_len);
	/* This signals that unwind_info points to a normal cached unwind info */
	jinfo->from_aot = 0;
	jinfo->from_llvm = 1;

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

		/* Make sure we transition to thumb when a handler starts */
		if (amodule->thumb_end && (guint8*)jei->handler_start < amodule->thumb_end)
			jei->handler_start = (void*)((mgreg_t)jei->handler_start + 1);
	}

	/* See exception_cb () in mini-llvm.c as to why this is needed */
	nindex = ei_len;
	for (i = 0; i < ei_len; ++i) {
		gint32 cindex1 = read32 (type_info [i]);
		GSList *l;

		for (l = nesting [cindex1]; l; l = l->next) {
			gint32 nesting_cindex = GPOINTER_TO_INT (l->data);

			for (j = 0; j < ei_len; ++j) {
				gint32 cindex2 = read32 (type_info [j]);

				if (cindex2 == nesting_cindex) {
					/* 
					 * The try interval comes from the nested clause, everything else from the
					 * nesting clause.
					 */
					memcpy (&jinfo->clauses [nindex], &jinfo->clauses [j], sizeof (MonoJitExceptionInfo));
					jinfo->clauses [nindex].try_start = jinfo->clauses [i].try_start;
					jinfo->clauses [nindex].try_end = jinfo->clauses [i].try_end;
					nindex ++;
				}
			}
		}
	}
	g_assert (nindex == ei_len + nested_len);

	return jinfo;
}

static gpointer
alloc0_jit_info_data (MonoDomain *domain, int size, gboolean async_context)
{
	gpointer res;

	if (async_context) {
		res = mono_domain_alloc0_lock_free (domain, size);
		InterlockedExchangeAdd (&async_jit_info_size, size);
	} else {
		res = mono_domain_alloc0 (domain, size);
	}
	return res;
}

/*
 * LOCKING: Acquires the domain lock.
 * In async context, this is async safe.
 */
static MonoJitInfo*
decode_exception_debug_info (MonoAotModule *amodule, MonoDomain *domain, 
							 MonoMethod *method, guint8* ex_info, guint8 *addr,
							 guint8 *code, guint32 code_len)
{
	int i, buf_len, num_clauses, len;
	MonoJitInfo *jinfo;
	MonoJitInfoFlags flags = JIT_INFO_NONE;
	guint unwind_info, eflags;
	gboolean has_generic_jit_info, has_dwarf_unwind_info, has_clauses, has_seq_points, has_try_block_holes, has_arch_eh_jit_info;
	gboolean from_llvm, has_gc_map;
	guint8 *p;
	int generic_info_size, try_holes_info_size, num_holes, arch_eh_jit_info_size;
	int this_reg = 0, this_offset = 0;
	gboolean async;

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
	if (has_generic_jit_info) {
		flags |= JIT_INFO_HAS_GENERIC_JIT_INFO;
		generic_info_size = sizeof (MonoGenericJitInfo);
	} else {
		generic_info_size = 0;
	}

	if (has_try_block_holes) {
		num_holes = decode_value (p, &p);
		flags |= JIT_INFO_HAS_TRY_BLOCK_HOLES;
		try_holes_info_size = sizeof (MonoTryBlockHoleTableJitInfo) + num_holes * sizeof (MonoTryBlockHoleJitInfo);
	} else {
		num_holes = try_holes_info_size = 0;
	}

	if (has_arch_eh_jit_info) {
		flags |= JIT_INFO_HAS_ARCH_EH_INFO;
		arch_eh_jit_info_size = sizeof (MonoArchEHJitInfo);
		/* Overwrite the original code_len which includes alignment padding */
		code_len = decode_value (p, &p);
	} else {
		arch_eh_jit_info_size = 0;
	}

	/* Exception table */
	if (has_clauses)
		num_clauses = decode_value (p, &p);
	else
		num_clauses = 0;

	if (from_llvm) {
		MonoJitExceptionInfo *clauses;
		GSList **nesting;

		// FIXME: async
		g_assert (!async);

		/*
		 * Part of the info is encoded by the AOT compiler, the rest is in the .eh_frame
		 * section.
		 */
		clauses = g_new0 (MonoJitExceptionInfo, num_clauses);
		nesting = g_new0 (GSList*, num_clauses);

		for (i = 0; i < num_clauses; ++i) {
			MonoJitExceptionInfo *ei = &clauses [i];

			ei->flags = decode_value (p, &p);

			if (decode_value (p, &p))
				ei->data.catch_class = decode_klass_ref (amodule, p, &p);

			/* Read the list of nesting clauses */
			while (TRUE) {
				int nesting_index = decode_value (p, &p);
				if (nesting_index == -1)
					break;
				nesting [i] = g_slist_prepend (nesting [i], GINT_TO_POINTER (nesting_index));
			}
		}

		jinfo = decode_llvm_mono_eh_frame (amodule, domain, method, code, clauses, num_clauses, flags, nesting, &this_reg, &this_offset);

		g_free (clauses);
		for (i = 0; i < num_clauses; ++i)
			g_slist_free (nesting [i]);
		g_free (nesting);
	} else {
		len = mono_jit_info_size (flags, num_clauses, num_holes);
		jinfo = alloc0_jit_info_data (domain, len, async);
		mono_jit_info_init (jinfo, method, code, code_len, flags, num_clauses, num_holes);

		for (i = 0; i < jinfo->num_clauses; ++i) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];

			ei->flags = decode_value (p, &p);

			ei->exvar_offset = decode_value (p, &p);

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER || ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY)
				ei->data.filter = code + decode_value (p, &p);
			else {
				int len = decode_value (p, &p);

				if (len > 0) {
					if (async)
						p += len;
					else
						ei->data.catch_class = decode_klass_ref (amodule, p, &p);
				}
			}

			ei->try_start = code + decode_value (p, &p);
			ei->try_end = code + decode_value (p, &p);
			ei->handler_start = code + decode_value (p, &p);
		}

		jinfo->unwind_info = unwind_info;
		jinfo->domain_neutral = 0;
		jinfo->from_aot = 1;
	}

	if (has_try_block_holes) {
		MonoTryBlockHoleTableJitInfo *table;

		g_assert (jinfo->has_try_block_holes);

		table = mono_jit_info_get_try_block_hole_table_info (jinfo);
		g_assert (table);

		table->num_holes = (guint16)num_holes;
		for (i = 0; i < num_holes; ++i) {
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
			gi->locations = alloc0_jit_info_data (domain, gi->nlocs * sizeof (MonoDwarfLocListEntry), async);
			for (i = 0; i < gi->nlocs; ++i) {
				MonoDwarfLocListEntry *entry = &gi->locations [i];

				entry->is_reg = decode_value (p, &p);
				entry->reg = decode_value (p, &p);
				if (!entry->is_reg)
					entry->offset = decode_value (p, &p);
				if (i > 0)
					entry->from = decode_value (p, &p);
				entry->to = decode_value (p, &p);
			}
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
		if (async)
			p += len;
		else
			jinfo->d.method = decode_resolve_method_ref (amodule, p, &p);

		gi->generic_sharing_context = g_new0 (MonoGenericSharingContext, 1);
		if (decode_value (p, &p)) {
			/* gsharedvt */
			int i, n;
			MonoGenericSharingContext *gsctx = gi->generic_sharing_context;

			n = decode_value (p, &p);
			if (n) {
				gsctx->var_is_vt = alloc0_jit_info_data (domain, sizeof (gboolean) * n, async);
				for (i = 0; i < n; ++i)
					gsctx->var_is_vt [i] = decode_value (p, &p);
			}
			n = decode_value (p, &p);
			if (n) {
				gsctx->mvar_is_vt = alloc0_jit_info_data (domain, sizeof (gboolean) * n, async);
				for (i = 0; i < n; ++i)
					gsctx->mvar_is_vt [i] = decode_value (p, &p);
			}
		}
	}

	if (method && has_seq_points) {
		MonoSeqPointInfo *seq_points;
		int il_offset, native_offset, last_il_offset, last_native_offset, j;

		int len = decode_value (p, &p);

		seq_points = g_malloc0 (sizeof (MonoSeqPointInfo) + (len - MONO_ZERO_LEN_ARRAY) * sizeof (SeqPoint));
		seq_points->len = len;
		last_il_offset = last_native_offset = 0;
		for (i = 0; i < len; ++i) {
			SeqPoint *sp = &seq_points->seq_points [i];
			il_offset = last_il_offset + decode_value (p, &p);
			native_offset = last_native_offset + decode_value (p, &p);

			sp->il_offset = il_offset;
			sp->native_offset = native_offset;
			
			sp->flags = decode_value (p, &p);
			sp->next_len = decode_value (p, &p);
			sp->next = g_new (int, sp->next_len);
			for (j = 0; j < sp->next_len; ++j)
				sp->next [j] = decode_value (p, &p);

			last_il_offset = il_offset;
			last_native_offset = native_offset;
		}

		mono_domain_lock (domain);
		g_hash_table_insert (domain_jit_info (domain)->seq_points, method, seq_points);
		mono_domain_unlock (domain);
	}

	/* Load debug info */
	buf_len = decode_value (p, &p);
	if (!async)
		mono_debug_add_aot_method (domain, method, code, p, buf_len);
	p += buf_len;

	if (has_gc_map) {
		int map_size = decode_value (p, &p);
		/* The GC map requires 4 bytes of alignment */
		while ((guint64)(gsize)p % 4)
			p ++;		
		jinfo->gc_info = p;
		p += map_size;
	}

	if (amodule != jinfo->d.method->klass->image->aot_module) {
		mono_aot_lock ();
		if (!ji_to_amodule)
			ji_to_amodule = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (ji_to_amodule, jinfo, amodule);
		mono_aot_unlock ();		
	}

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
	MonoAotModule *amodule;
	guint8 *p;
	guint8 *code = ji->code_start;

	if (ji->async)
		amodule = ji->d.aot_info;
	else
		amodule = jinfo_get_method (ji)->klass->image->aot_module;
	g_assert (amodule);
	g_assert (ji->from_aot);

	if (!(code >= amodule->code && code <= amodule->code_end)) {
		/* ji belongs to a different aot module than amodule */
		mono_aot_lock ();
		g_assert (ji_to_amodule);
		amodule = g_hash_table_lookup (ji_to_amodule, ji);
		g_assert (amodule);
		g_assert (code >= amodule->code && code <= amodule->code_end);
		mono_aot_unlock ();
	}

	p = amodule->unwind_info + ji->unwind_info;
	*unwind_info_len = decode_value (p, &p);
	return p;
}

static G_GNUC_UNUSED int
compare_ints (const void *a, const void *b)
{
	return *(gint32*)a - *(gint32*)b;
}

static void
msort_code_offsets_internal (gint32 *array, int lo, int hi, gint32 *scratch)
{
	int mid = (lo + hi) / 2;
	int i, t_lo, t_hi;

	if (lo >= hi)
		return;

	if (hi - lo < 32) {
		for (i = lo; i < hi; ++i)
			if (array [(i * 2)] > array [(i * 2) + 2])
				break;
		if (i == hi)
			/* Already sorted */
			return;
	}

	msort_code_offsets_internal (array, lo, mid, scratch);
	msort_code_offsets_internal (array, mid + 1, hi, scratch);

	if (array [mid * 2] < array [(mid + 1) * 2])
		return;

	/* Merge */
	t_lo = lo;
	t_hi = mid + 1;
	for (i = lo; i <= hi; i ++) {
		if (t_lo <= mid && ((t_hi > hi) || array [t_lo * 2] < array [t_hi * 2])) {
			scratch [(i * 2)] = array [t_lo * 2];
			scratch [(i * 2) + 1] = array [(t_lo *2) + 1];
			t_lo ++;
		} else {
			scratch [(i * 2)] = array [t_hi * 2];
			scratch [(i * 2) + 1] = array [(t_hi *2) + 1];
			t_hi ++;
		}
	}
	for (i = lo; i <= hi; ++i) {
		array [(i * 2)] = scratch [i * 2];
		array [(i * 2) + 1] = scratch [(i * 2) + 1];
	}
}

static void
msort_code_offsets (gint32 *array, int len)
{
	gint32 *scratch;

	scratch = g_new (gint32, len * 2);
	msort_code_offsets_internal (array, 0, len - 1, scratch);
	g_free (scratch);
}

/*
 * mono_aot_find_jit_info:
 *
 *   In async context, the resulting MonoJitInfo will not have its method field set, and it will not be added
 * to the jit info tables.
 * FIXME: Large sizes in the lock free allocator
 */
MonoJitInfo *
mono_aot_find_jit_info (MonoDomain *domain, MonoImage *image, gpointer addr)
{
	int pos, left, right, offset, offset1, offset2, code_len;
	int method_index, table_len;
	guint32 token;
	MonoAotModule *amodule = image->aot_module;
	MonoMethod *method = NULL;
	MonoJitInfo *jinfo;
	guint8 *code, *ex_info, *p;
	guint32 *table;
	int nmethods;
	gint32 *code_offsets;
	int offsets_len, i;
	gboolean async;

	if (!amodule)
		return NULL;

	nmethods = amodule->info.nmethods;

	if (domain != mono_get_root_domain ())
		/* FIXME: */
		return NULL;

	async = mono_thread_info_is_async_context ();

	offset = (guint8*)addr - amodule->code;

	/* Compute a sorted table mapping code offsets to method indexes. */
	if (!amodule->sorted_code_offsets) {
		// FIXME: async
		code_offsets = g_new0 (gint32, nmethods * 2);
		offsets_len = 0;
		for (i = 0; i < nmethods; ++i) {
			/* Skip the -1 entries to speed up sorting */
			if (amodule->code_offsets [i] == 0xffffffff)
				continue;
			code_offsets [(offsets_len * 2)] = amodule->code_offsets [i];
			code_offsets [(offsets_len *2) + 1] = i;
			offsets_len ++;
		}
		/* Use a merge sort as this is mostly sorted */
		msort_code_offsets (code_offsets, offsets_len);
		//qsort (code_offsets, offsets_len, sizeof (gint32) * 2, compare_ints);
		for (i = 0; i < offsets_len -1; ++i)
			g_assert (code_offsets [(i * 2)] <= code_offsets [(i + 1) * 2]);

		amodule->sorted_code_offsets_len = offsets_len;
		mono_memory_barrier ();
		if (InterlockedCompareExchangePointer ((gpointer*)&amodule->sorted_code_offsets, code_offsets, NULL) != NULL)
			/* Somebody got in before us */
			g_free (code_offsets);
	}

	code_offsets = amodule->sorted_code_offsets;
	offsets_len = amodule->sorted_code_offsets_len;

	if (offsets_len > 0 && (offset < code_offsets [0] || offset >= (amodule->code_end - amodule->code)))
		return NULL;

	/* Binary search in the sorted_code_offsets table */
	left = 0;
	right = offsets_len;
	while (TRUE) {
		pos = (left + right) / 2;

		offset1 = code_offsets [(pos * 2)];
		if (pos + 1 == offsets_len)
			offset2 = amodule->code_end - amodule->code;
		else
			offset2 = code_offsets [(pos + 1) * 2];

		if (offset < offset1)
			right = pos;
		else if (offset >= offset2)
			left = pos + 1;
		else
			break;
	}

	g_assert (offset >= code_offsets [(pos * 2)]);
	if (pos + 1 < offsets_len)
		g_assert (offset < code_offsets [((pos + 1) * 2)]);
	method_index = code_offsets [(pos * 2) + 1];

	/* In async mode, jinfo is not added to the normal jit info table, so have to cache it ourselves */
	if (async) {
		JitInfoMap *table = amodule->async_jit_info_table;
		int len;

		if (table) {
			len = table [0].method_index;
			for (i = 1; i < len; ++i) {
				if (table [i].method_index == method_index)
					return table [i].jinfo;
			}
		}
	}

	code = &amodule->code [amodule->code_offsets [method_index]];
	ex_info = &amodule->blob [mono_aot_get_offset (amodule->ex_info_offsets, method_index)];

	if (pos == offsets_len - 1)
		code_len = amodule->code_end - code;
	else
		code_len = code_offsets [(pos + 1) * 2] - code_offsets [pos * 2];

	g_assert ((guint8*)code <= (guint8*)addr && (guint8*)addr < (guint8*)code + code_len);

	/* Might be a wrapper/extra method */
	if (!async) {
		if (amodule->extra_methods) {
			amodule_lock (amodule);
			method = g_hash_table_lookup (amodule->extra_methods, GUINT_TO_POINTER (method_index));
			amodule_unlock (amodule);
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

				p = amodule->blob + table [(pos * 2) + 1];
				method = decode_resolve_method_ref (amodule, p, &p);
				if (!method)
					/* Happens when a random address is passed in which matches a not-yey called wrapper encoded using its name */
					return NULL;
			} else {
				token = mono_metadata_make_token (MONO_TABLE_METHOD, method_index + 1);
				method = mono_get_method (image, token, NULL);
			}
		}
		/* FIXME: */
		g_assert (method);
	}

	//printf ("F: %s\n", mono_method_full_name (method, TRUE));
	
	jinfo = decode_exception_debug_info (amodule, domain, method, ex_info, addr, code, code_len);

	g_assert ((guint8*)addr >= (guint8*)jinfo->code_start);
	g_assert ((guint8*)addr < (guint8*)jinfo->code_start + jinfo->code_size);

	/* Add it to the normal JitInfo tables */
	if (async) {
		JitInfoMap *old_table, *new_table;
		int len;

		/*
		 * Use a simple inmutable table with linear search to cache async jit info entries.
		 * This assumes that the number of entries is small.
		 */
		while (TRUE) {
			/* Copy the table, adding a new entry at the end */
			old_table = amodule->async_jit_info_table;
			if (old_table)
				len = old_table[0].method_index;
			else
				len = 1;
			new_table = alloc0_jit_info_data (domain, (len + 1) * sizeof (JitInfoMap), async);
			if (old_table)
				memcpy (new_table, old_table, len * sizeof (JitInfoMap));
			new_table [0].method_index = len + 1;
			new_table [len].method_index = method_index;
			new_table [len].jinfo = jinfo;
			/* Publish it */
			mono_memory_barrier ();
			if (InterlockedCompareExchangePointer ((gpointer)&amodule->async_jit_info_table, new_table, old_table) == old_table)
				break;
		}
	} else {
		mono_jit_info_table_add (domain, jinfo);
	}
	
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
	case MONO_PATCH_INFO_METHOD_RGCTX:
	case MONO_PATCH_INFO_METHOD_CODE_SLOT: {
		MethodRef ref;
		gboolean res;

		res = decode_method_ref (aot_module, &ref, p, &p);
		if (!res)
			goto cleanup;

		if (!ref.method && !mono_aot_only && !ref.no_aot_trampoline && (ji->type == MONO_PATCH_INFO_METHOD) && (mono_metadata_token_table (ref.token) == MONO_TABLE_METHOD)) {
			ji->data.target = mono_create_ftnptr (mono_domain_get (), mono_create_jit_trampoline_from_token (ref.image, ref.token));
			ji->type = MONO_PATCH_INFO_ABS;
		}
		else {
			if (ref.method)
				ji->data.method = ref.method;
			else
				ji->data.method = mono_get_method (ref.image, ref.token, NULL);
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
		ji->data.method = decode_resolve_method_ref (aot_module, p, &p);
		if (!ji->data.method)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
	case MONO_PATCH_INFO_CLASS_INIT:
		/* Shared */
		ji->data.klass = decode_klass_ref (aot_module, p, &p);
		if (!ji->data.klass)
			goto cleanup;
		break;
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		ji->data.del_tramp = mono_mempool_alloc0 (mp, sizeof (MonoDelegateClassMethodPair));
		ji->data.del_tramp->klass = decode_klass_ref (aot_module, p, &p);
		if (!ji->data.del_tramp->klass)
			goto cleanup;
		if (decode_value (p, &p)) {
			ji->data.del_tramp->method = decode_resolve_method_ref (aot_module, p, &p);
			if (!ji->data.del_tramp->method)
				goto cleanup;
		}
		ji->data.del_tramp->virtual = decode_value (p, &p) ? TRUE : FALSE;
		break;
	case MONO_PATCH_INFO_IMAGE:
		ji->data.image = load_image (aot_module, decode_value (p, &p), TRUE);
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
		table = mono_domain_alloc (mono_domain_get (), sizeof (gpointer) * ji->data.table->table_size);
		ji->data.table->table = (MonoBasicBlock**)table;
		for (i = 0; i < ji->data.table->table_size; i++)
			table [i] = (gpointer)(gssize)decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_R4: {
		guint32 val;
		
		ji->data.target = mono_domain_alloc0 (mono_domain_get (), sizeof (float));
		val = decode_value (p, &p);
		*(float*)ji->data.target = *(float*)&val;
		break;
	}
	case MONO_PATCH_INFO_R8: {
		guint32 val [2];
		guint64 v;

		ji->data.target = mono_domain_alloc0 (mono_domain_get (), sizeof (double));

		val [0] = decode_value (p, &p);
		val [1] = decode_value (p, &p);
		v = ((guint64)val [1] << 32) | ((guint64)val [0]);
		*(double*)ji->data.target = *(double*)&v;
		break;
	}
	case MONO_PATCH_INFO_LDSTR:
		image = load_image (aot_module, decode_value (p, &p), TRUE);
		if (!image)
			goto cleanup;
		ji->data.token = mono_jump_info_token_new (mp, image, MONO_TOKEN_STRING + decode_value (p, &p));
		break;
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_DECLSEC:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		/* Shared */
		image = load_image (aot_module, decode_value (p, &p), TRUE);
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
	case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR:
	case MONO_PATCH_INFO_JIT_TLS_ID:
		break;
	case MONO_PATCH_INFO_CASTCLASS_CACHE:
		ji->data.index = decode_value (p, &p);
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH: {
		gboolean res;
		MonoJumpInfoRgctxEntry *entry;
		guint32 offset, val;
		guint8 *p2;

		offset = decode_value (p, &p);
		val = decode_value (p, &p);

		entry = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoRgctxEntry));
		p2 = aot_module->blob + offset;
		entry->method = decode_resolve_method_ref (aot_module, p2, &p2);
		entry->in_mrgctx = ((val & 1) > 0) ? TRUE : FALSE;
		entry->info_type = (val >> 1) & 0xff;
		entry->data = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfo));
		entry->data->type = (val >> 9) & 0xff;
		
		res = decode_patch (aot_module, mp, entry->data, p, &p);
		if (!res)
			goto cleanup;
		ji->data.rgctx_entry = entry;
		break;
	}
	case MONO_PATCH_INFO_SEQ_POINT_INFO:
		break;
	case MONO_PATCH_INFO_LLVM_IMT_TRAMPOLINE: {
		MonoJumpInfoImtTramp *imt_tramp = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoImtTramp));

		imt_tramp->method = decode_resolve_method_ref (aot_module, p, &p);
		imt_tramp->vt_offset = decode_value (p, &p);
		
		ji->data.imt_tramp = imt_tramp;
		break;
	}
	case MONO_PATCH_INFO_SIGNATURE:
		ji->data.target = decode_signature (aot_module, p, &p);
		break;
	case MONO_PATCH_INFO_TLS_OFFSET:
		ji->data.target = GINT_TO_POINTER (decode_value (p, &p));
		break;
	case MONO_PATCH_INFO_GSHAREDVT_CALL: {
		MonoJumpInfoGSharedVtCall *info = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoGSharedVtCall));
		info->sig = decode_signature (aot_module, p, &p);
		g_assert (info->sig);
		info->method = decode_resolve_method_ref (aot_module, p, &p);
		g_assert (info->method);

		ji->data.target = info;
		break;
	}
	case MONO_PATCH_INFO_GSHAREDVT_METHOD: {
		MonoGSharedVtMethodInfo *info = mono_mempool_alloc0 (mp, sizeof (MonoGSharedVtMethodInfo));
		int i;
		
		info->method = decode_resolve_method_ref (aot_module, p, &p);
		g_assert (info->method);
		info->num_entries = decode_value (p, &p);
		info->count_entries = info->num_entries;
		info->entries = mono_mempool_alloc0 (mp, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->num_entries);
		for (i = 0; i < info->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *template = &info->entries [i];

			template->info_type = decode_value (p, &p);
			switch (mini_rgctx_info_type_to_patch_info_type (template->info_type)) {
			case MONO_PATCH_INFO_CLASS: {
				MonoClass *klass = decode_klass_ref (aot_module, p, &p);
				if (!klass)
					goto cleanup;
				template->data = &klass->byval_arg;
				break;
			}
			case MONO_PATCH_INFO_FIELD:
				template->data = decode_field_info (aot_module, p, &p);
				if (!template->data)
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
				 guint32 **got_slots, 
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

		got_offset = decode_value (p, &p);

		shared_p = aot_module->blob + mono_aot_get_offset (aot_module->got_info_offsets, got_offset);

		ji->type = decode_value (shared_p, &shared_p);

		/* See load_method () for SFLDA */
		if (aot_module->got [got_offset] && ji->type != MONO_PATCH_INFO_SFLDA) {
			/* Already loaded */
		} else {
			res = decode_patch (aot_module, mp, ji, shared_p, &shared_p);
			if (!res)
				goto cleanup;
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
load_method (MonoDomain *domain, MonoAotModule *amodule, MonoImage *image, MonoMethod *method, guint32 token, int method_index)
{
	MonoClass *klass;
	gboolean from_plt = method == NULL;
	MonoMemPool *mp;
	int i, pindex, n_patches, used_strings;
	gboolean keep_patches = TRUE;
	guint8 *p;
	MonoJitInfo *jinfo = NULL;
	guint8 *code, *info;

	if (mono_profiler_get_events () & MONO_PROFILE_ENTER_LEAVE) {
		if (mono_aot_only)
			/* The caller cannot handle this */
			g_assert_not_reached ();
		return NULL;
	}

	if ((domain != mono_get_root_domain ()) && (!(amodule->info.opts & MONO_OPT_SHARED)))
		/* Non shared AOT code can't be used in other appdomains */
		return NULL;

	if (amodule->out_of_date)
		return NULL;

	if (amodule->code_offsets [method_index] == 0xffffffff) {
		if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
			char *full_name;

			if (!method)
				method = mono_get_method (image, token, NULL);
			full_name = mono_method_full_name (method, TRUE);
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: NOT FOUND: %s.", full_name);
			g_free (full_name);
		}
		return NULL;
	}

	code = &amodule->code [amodule->code_offsets [method_index]];

	info = &amodule->blob [mono_aot_get_offset (amodule->method_info_offsets, method_index)];

	if (amodule->thumb_end && code < amodule->thumb_end && ((amodule->info.flags & MONO_AOT_FILE_FLAG_DIRECT_METHOD_ADDRESSES) == 0)) {
		/* Convert this into a thumb address */
		g_assert ((amodule->code_offsets [method_index] & 0x1) == 0);
		code = &amodule->code [amodule->code_offsets [method_index] + 1];
	}

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

	if (mono_last_aot_method != -1) {
		if (mono_jit_stats.methods_aot >= mono_last_aot_method)
				return NULL;
		else if (mono_jit_stats.methods_aot == mono_last_aot_method - 1) {
			if (!method)
				method = mono_get_method (image, token, NULL);
			if (method) {
				char *name = mono_method_full_name (method, TRUE);
				g_print ("LAST AOT METHOD: %s.\n", name);
				g_free (name);
			} else {
				g_print ("LAST AOT METHOD: %p %d\n", code, method_index);
			}
		}
	}

	p = info;

	if (method) {
		klass = method->klass;
		decode_klass_ref (amodule, p, &p);
	} else {
		klass = decode_klass_ref (amodule, p, &p);
	}

	if (amodule->info.opts & MONO_OPT_SHARED)
		used_strings = decode_value (p, &p);
	else
		used_strings = 0;

	for (i = 0; i < used_strings; i++) {
		guint token = decode_value (p, &p);
		mono_ldstr (mono_get_root_domain (), image, mono_metadata_token_index (token));
	}

	if (amodule->info.opts & MONO_OPT_SHARED)	
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

		patches = load_patch_info (amodule, mp, n_patches, &got_slots, p, &p);
		if (patches == NULL)
			goto cleanup;

		for (pindex = 0; pindex < n_patches; ++pindex) {
			MonoJumpInfo *ji = &patches [pindex];
			gpointer addr;

			/*
			 * For SFLDA, we need to call resolve_patch_target () since the GOT slot could have
			 * been initialized by load_method () for a static cctor before the cctor has
			 * finished executing (#23242).
			 */
			if (!amodule->got [got_slots [pindex]] || ji->type == MONO_PATCH_INFO_SFLDA) {
				addr = mono_resolve_patch_target (method, domain, code, ji, TRUE);
				if (ji->type == MONO_PATCH_INFO_METHOD_JUMP)
					addr = mono_create_ftnptr (domain, addr);
				mono_memory_barrier ();
				amodule->got [got_slots [pindex]] = addr;
				if (ji->type == MONO_PATCH_INFO_METHOD_JUMP)
					register_jump_target_got_slot (domain, ji->data.method, &(amodule->got [got_slots [pindex]]));
			}
			ji->type = MONO_PATCH_INFO_NONE;
		}

		g_free (got_slots);

		if (!keep_patches)
			mono_mempool_destroy (mp);
	}

	if (mini_get_debug_options ()->load_aot_jit_info_eagerly)
		jinfo = mono_aot_find_jit_info (domain, amodule->assembly->image, code);

	if (mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
		char *full_name;

		if (!method)
			method = mono_get_method (image, token, NULL);

		full_name = mono_method_full_name (method, TRUE);

		if (!jinfo)
			jinfo = mono_aot_find_jit_info (domain, amodule->assembly->image, code);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: FOUND method %s [%p - %p %p]", full_name, code, code + jinfo->code_size, info);
		g_free (full_name);
	}

	amodule_lock (amodule);

	InterlockedIncrement (&mono_jit_stats.methods_aot);

	amodule->methods_loaded [method_index / 32] |= 1 << (method_index % 32);

	init_plt (amodule);

	if (method && method->wrapper_type)
		g_hash_table_insert (amodule->method_to_code, method, code);

	amodule_unlock (amodule);

	if (mono_profiler_get_events () & MONO_PROFILE_JIT_COMPILATION) {
		MonoJitInfo *jinfo;

		if (!method) {
			method = mono_get_method (image, token, NULL);
			g_assert (method);
		}
		mono_profiler_method_jit (method);
		jinfo = mono_jit_info_table_find (domain, (char*)code);
		g_assert (jinfo);
		mono_profiler_method_end_jit (method, jinfo, MONO_PROFILE_OK);
	}

	if (from_plt && klass && !klass->generic_container)
		mono_runtime_class_init (mono_class_vtable (domain, klass));

	return code;

 cleanup:
	/* FIXME: The space in domain->mp is wasted */	
	if (amodule->info.opts & MONO_OPT_SHARED)
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
	guint32 index;
	static guint32 n_extra_decodes;

	if (!amodule || amodule->out_of_date)
		return 0xffffff;

	table_size = amodule->extra_method_table [0];
	table = amodule->extra_method_table + 1;
	entry_size = 3;

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
		guint8 *p, *orig_p;

		p = amodule->blob + key;
		orig_p = p;

		amodule_lock (amodule);
		if (!amodule->method_ref_to_method)
			amodule->method_ref_to_method = g_hash_table_new (NULL, NULL);
		m = g_hash_table_lookup (amodule->method_ref_to_method, p);
		amodule_unlock (amodule);
		if (!m) {
			m = decode_resolve_method_ref_with_target (amodule, method, p, &p);
			if (m) {
				amodule_lock (amodule);
				g_hash_table_insert (amodule->method_ref_to_method, orig_p, m);
				amodule_unlock (amodule);
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
		if (m) {
			//printf ("%d %s %s %p\n", n_extra_decodes, mono_method_full_name (method, TRUE), mono_method_full_name (m, TRUE), orig_p);
			n_extra_decodes ++;
		}

		if (next != 0)
			entry = &table [next * entry_size];
		else
			break;
	}

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

/*
 * mono_aot_get_method:
 *
 *   Return a pointer to the AOTed native code for METHOD if it can be found,
 * NULL otherwise.
 * On platforms with function pointers, this doesn't return a function pointer.
 */
gpointer
mono_aot_get_method (MonoDomain *domain, MonoMethod *method)
{
	MonoClass *klass = method->klass;
	guint32 method_index;
	MonoAotModule *amodule = klass->image->aot_module;
	guint8 *code;

	if (enable_aot_cache && !amodule && domain->entry_assembly && klass->image == mono_defaults.corlib) {
		/* This cannot be AOTed during startup, so do it now */
		if (!mscorlib_aot_loaded) {
			mscorlib_aot_loaded = TRUE;
			load_aot_module (klass->image->assembly, NULL);
			amodule = klass->image->aot_module;
		}
	}

	if (!amodule)
		return NULL;

	if (amodule->out_of_date)
		return NULL;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
		(method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
		(method->flags & METHOD_ATTRIBUTE_ABSTRACT))
		return NULL;

	/*
	 * Use the original method instead of its invoke-with-check wrapper.
	 * This is not a problem when using full-aot, since it doesn't support
	 * remoting.
	 */
	if (mono_aot_only && method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)
		return mono_aot_get_method (domain, mono_marshal_method_from_wrapper (method));

	g_assert (klass->inited);

	/* Find method index */
	method_index = 0xffffff;
	if (method->is_inflated && !method->wrapper_type && mono_method_is_generic_sharable_full (method, FALSE, FALSE, FALSE)) {
		/* 
		 * For generic methods, we store the fully shared instance in place of the
		 * original method.
		 */
		method = mono_method_get_declaring_generic_method (method);
		method_index = mono_metadata_token_index (method->token) - 1;
	} else if (method->is_inflated || !method->token) {
		/* This hash table is used to avoid the slower search in the extra_method_table in the AOT image */
		amodule_lock (amodule);
		code = g_hash_table_lookup (amodule->method_to_code, method);
		amodule_unlock (amodule);
		if (code)
			return code;

		method_index = find_extra_method (method, &amodule);
		/*
		 * Special case the ICollection<T> wrappers for arrays, as they cannot
		 * be statically enumerated, and each wrapper ends up calling the same
		 * method in Array.
		 */
		if (method_index == 0xffffff && method->wrapper_type == MONO_WRAPPER_MANAGED_TO_MANAGED && method->klass->rank && strstr (method->name, "System.Collections.Generic")) {
			MonoMethod *m = mono_aot_get_array_helper_from_wrapper (method);

			code = mono_aot_get_method (domain, m);
			if (code)
				return code;
		}

		/*
		 * Special case Array.GetGenericValueImpl which is a generic icall.
		 * Generic sharing currently can't handle it, but the icall returns data using
		 * an out parameter, so the managed-to-native wrappers can share the same code.
		 */
		if (method_index == 0xffffff && method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE && method->klass == mono_defaults.array_class && !strcmp (method->name, "GetGenericValueImpl")) {
			MonoMethod *m;
			MonoGenericContext ctx;
			MonoType *args [16];

			if (mono_method_signature (method)->params [1]->type == MONO_TYPE_OBJECT)
				/* Avoid recursion */
				return NULL;

			m = mono_class_get_method_from_name (mono_defaults.array_class, "GetGenericValueImpl", 2);
			g_assert (m);

			memset (&ctx, 0, sizeof (ctx));
			args [0] = &mono_defaults.object_class->byval_arg;
			ctx.method_inst = mono_metadata_get_generic_inst (1, args);

			m = mono_marshal_get_native_wrapper (mono_class_inflate_generic_method (m, &ctx), TRUE, TRUE);

			/* 
			 * Get the code for the <object> instantiation which should be emitted into
			 * the mscorlib aot image by the AOT compiler.
			 */
			code = mono_aot_get_method (domain, m);
			if (code)
				return code;
		}

		/* Same for CompareExchange<T> and Exchange<T> */
		/* Same for Volatile.Read<T>/Write<T> */
		if (method_index == 0xffffff && method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE && method->klass->image == mono_defaults.corlib && 
			((!strcmp (method->klass->name_space, "System.Threading") && !strcmp (method->klass->name, "Interlocked") && (!strcmp (method->name, "CompareExchange") || !strcmp (method->name, "Exchange")) && MONO_TYPE_IS_REFERENCE (mono_method_signature (method)->params [1])) ||
			 (!strcmp (method->klass->name_space, "System.Threading") && !strcmp (method->klass->name, "Volatile") && (!strcmp (method->name, "Read") && MONO_TYPE_IS_REFERENCE (mono_method_signature (method)->ret))) ||
			 (!strcmp (method->klass->name_space, "System.Threading") && !strcmp (method->klass->name, "Volatile") && (!strcmp (method->name, "Write") && MONO_TYPE_IS_REFERENCE (mono_method_signature (method)->params [1]))))) {
			MonoMethod *m;
			MonoGenericContext ctx;
			MonoType *args [16];
			gpointer iter = NULL;

			while ((m = mono_class_get_methods (method->klass, &iter))) {
				if (mono_method_signature (m)->generic_param_count && !strcmp (m->name, method->name))
					break;
			}
			g_assert (m);

			memset (&ctx, 0, sizeof (ctx));
			args [0] = &mono_defaults.object_class->byval_arg;
			ctx.method_inst = mono_metadata_get_generic_inst (1, args);

			m = mono_marshal_get_native_wrapper (mono_class_inflate_generic_method (m, &ctx), TRUE, TRUE);

			/* Avoid recursion */
			if (method == m)
				return NULL;

			/* 
			 * Get the code for the <object> instantiation which should be emitted into
			 * the mscorlib aot image by the AOT compiler.
			 */
			code = mono_aot_get_method (domain, m);
			if (code)
				return code;
		}

		if (method_index == 0xffffff && method->is_inflated && mono_method_is_generic_sharable_full (method, FALSE, TRUE, FALSE)) {
			/* Partial sharing */
			MonoMethod *shared;

			shared = mini_get_shared_method (method);
			method_index = find_extra_method (shared, &amodule);
			if (method_index != 0xffffff)
				method = shared;
		}

		if (method_index == 0xffffff && method->is_inflated && mono_method_is_generic_sharable_full (method, FALSE, FALSE, TRUE)) {
			/* gsharedvt */
			/* Use the all-vt shared method since this is what was AOTed */
			method_index = find_extra_method (mini_get_shared_method_full (method, TRUE, TRUE), &amodule);
			if (method_index != 0xffffff)
				method = mini_get_shared_method_full (method, TRUE, FALSE);
		}

		if (method_index == 0xffffff) {
			if (mono_aot_only && mono_trace_is_traced (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT)) {
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
		if (!amodule->extra_methods)
			amodule->extra_methods = g_hash_table_new (NULL, NULL);
		g_hash_table_insert (amodule->extra_methods, GUINT_TO_POINTER (method_index), method);
		amodule_unlock (amodule);
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

void
mono_aot_patch_plt_entry (guint8 *code, guint8 *plt_entry, gpointer *got, mgreg_t *regs, guint8 *addr)
{
	MonoAotModule *amodule;

	/*
	 * Since AOT code is only used in the root domain, 
	 * mono_domain_get () != mono_get_root_domain () means the calling method
	 * is AppDomain:InvokeInDomain, so this is the same check as in 
	 * mono_method_same_domain () but without loading the metadata for the method.
	 */
	if (mono_domain_get () == mono_get_root_domain ()) {
		if (!got) {
			amodule = find_aot_module (code);
			if (amodule)
				got = amodule->got;
		}
		mono_arch_patch_plt_entry (plt_entry, got, regs, addr);
	}
}

/*
 * mono_aot_plt_resolve:
 *
 *   This function is called by the entries in the PLT to resolve the actual method that
 * needs to be called. It returns a trampoline to the method and patches the PLT entry.
 * Returns NULL if the something cannot be loaded.
 */
gpointer
mono_aot_plt_resolve (gpointer aot_module, guint32 plt_info_offset, guint8 *code)
{
#ifdef MONO_ARCH_AOT_SUPPORTED
	guint8 *p, *target, *plt_entry;
	MonoJumpInfo ji;
	MonoAotModule *module = (MonoAotModule*)aot_module;
	gboolean res, no_ftnptr = FALSE;
	MonoMemPool *mp;
	gboolean using_gsharedvt = FALSE;

	//printf ("DYN: %p %d\n", aot_module, plt_info_offset);

	p = &module->blob [plt_info_offset];

	ji.type = decode_value (p, &p);

	mp = mono_mempool_new_size (512);
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
		target = mono_jit_compile_method (ji.data.method);
		no_ftnptr = TRUE;
	} else {
		target = mono_resolve_patch_target (NULL, mono_domain_get (), NULL, &ji, TRUE);
	}

	/*
	 * The trampoline expects us to return a function descriptor on platforms which use
	 * it, but resolve_patch_target returns a direct function pointer for some type of
	 * patches, so have to translate between the two.
	 * FIXME: Clean this up, but how ?
	 */
	if (ji.type == MONO_PATCH_INFO_ABS || ji.type == MONO_PATCH_INFO_INTERNAL_METHOD || ji.type == MONO_PATCH_INFO_CLASS_INIT || ji.type == MONO_PATCH_INFO_ICALL_ADDR || ji.type == MONO_PATCH_INFO_JIT_ICALL_ADDR || ji.type == MONO_PATCH_INFO_RGCTX_FETCH) {
		/* These should already have a function descriptor */
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		/* Our function descriptors have a 0 environment, gcc created ones don't */
		if (ji.type != MONO_PATCH_INFO_INTERNAL_METHOD && ji.type != MONO_PATCH_INFO_JIT_ICALL_ADDR && ji.type != MONO_PATCH_INFO_ICALL_ADDR)
			g_assert (((gpointer*)target) [2] == 0);
#endif
		/* Empty */
	} else if (!no_ftnptr) {
#ifdef PPC_USES_FUNCTION_DESCRIPTOR
		g_assert (((gpointer*)target) [2] != 0);
#endif
		target = mono_create_ftnptr (mono_domain_get (), target);
	}

	mono_mempool_destroy (mp);

	/* Patch the PLT entry with target which might be the actual method not a trampoline */
	plt_entry = mono_aot_get_plt_entry (code);
	g_assert (plt_entry);
	mono_aot_patch_plt_entry (code, plt_entry, module->got, NULL, target);

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
 * LOCKING: Assumes the AMODULE lock is held.
 */
static void
init_plt (MonoAotModule *amodule)
{
	int i;
	gpointer tramp;

	if (amodule->plt_inited)
		return;

	tramp = mono_create_specific_trampoline (amodule, MONO_TRAMPOLINE_AOT_PLT, mono_get_root_domain (), NULL);

	/*
	 * Initialize the PLT entries in the GOT to point to the default targets.
	 */

	tramp = mono_create_ftnptr (mono_domain_get (), tramp);
	 for (i = 1; i < amodule->info.plt_size; ++i)
		 /* All the default entries point to the AOT trampoline */
		 ((gpointer*)amodule->got)[amodule->info.plt_got_offset_base + i] = tramp;

	amodule->plt_inited = TRUE;
}

/*
 * mono_aot_get_plt_entry:
 *
 *   Return the address of the PLT entry called by the code at CODE if exists.
 */
guint8*
mono_aot_get_plt_entry (guint8 *code)
{
	MonoAotModule *amodule = find_aot_module (code);
	guint8 *target = NULL;

	if (!amodule)
		return NULL;

#ifdef TARGET_ARM
	if (amodule->thumb_end && code < amodule->thumb_end) {
		return mono_arm_get_thumb_plt_entry (code);
	}
#endif

#ifdef MONO_ARCH_AOT_SUPPORTED
	target = mono_arch_get_call_target (code);
#else
	g_assert_not_reached ();
#endif

#ifdef MONOTOUCH
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
mono_aot_get_plt_info_offset (mgreg_t *regs, guint8 *code)
{
	guint8 *plt_entry = mono_aot_get_plt_entry (code);

	g_assert (plt_entry);

	/* The offset is embedded inside the code after the plt entry */
#ifdef MONO_ARCH_AOT_SUPPORTED
	return mono_arch_get_plt_info_offset (plt_entry, regs, code);
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
 * mono_aot_register_jit_icall:
 *
 *   Register a JIT icall which is called by trampolines in full-aot mode. This should
 * be called from mono_arch_init () during startup.
 */
void
mono_aot_register_jit_icall (const char *name, gpointer addr)
{
	/* No need for locking */
	if (!aot_jit_icall_hash)
		aot_jit_icall_hash = g_hash_table_new (g_str_hash, g_str_equal);
	g_hash_table_insert (aot_jit_icall_hash, (char*)name, addr);
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

	symbol = g_strdup_printf ("%s", name);
	find_symbol (amodule->sofile, amodule->globals, symbol, (gpointer *)&code);
	g_free (symbol);
	if (!code)
		g_error ("Symbol '%s' not found in AOT file '%s'.\n", name, amodule->aot_name);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_AOT, "AOT: FOUND function '%s' in AOT file '%s'.", name, amodule->aot_name);

	/* Load info */

	symbol = g_strdup_printf ("%s_p", name);
	find_symbol (amodule->sofile, amodule->globals, symbol, (gpointer *)&p);
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
		tinfo->code = code;
		tinfo->code_size = code_size;
		tinfo->uw_info = uw_info;
		tinfo->uw_info_len = uw_info_len;

		*out_tinfo = tinfo;
	}

	p = amodule->blob + info_offset;

	/* Similar to mono_aot_load_method () */

	n_patches = decode_value (p, &p);

	if (n_patches) {
		MonoJumpInfo *patches;
		guint32 *got_slots;

		mp = mono_mempool_new ();

		patches = load_patch_info (amodule, mp, n_patches, &got_slots, p, &p);
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
					target = mono_create_ftnptr_malloc (target);
				} else if (!strcmp (ji->data.name, "specific_trampoline_monitor_enter")) {
					target = mono_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_ENTER, mono_get_root_domain (), NULL);
					target = mono_create_ftnptr_malloc (target);
				} else if (!strcmp (ji->data.name, "specific_trampoline_monitor_exit")) {
					target = mono_create_specific_trampoline (NULL, MONO_TRAMPOLINE_MONITOR_EXIT, mono_get_root_domain (), NULL);
					target = mono_create_ftnptr_malloc (target);
				} else if (!strcmp (ji->data.name, "specific_trampoline_generic_class_init")) {
					target = mono_create_specific_trampoline (NULL, MONO_TRAMPOLINE_GENERIC_CLASS_INIT, mono_get_root_domain (), NULL);
					target = mono_create_ftnptr_malloc (target);
				} else if (!strcmp (ji->data.name, "mono_thread_get_and_clear_pending_exception")) {
					target = mono_thread_get_and_clear_pending_exception;
				} else if (strstr (ji->data.name, "generic_trampoline_")) {
					target = mono_aot_get_trampoline (ji->data.name);
				} else if (aot_jit_icall_hash && g_hash_table_lookup (aot_jit_icall_hash, ji->data.name)) {
					/* Registered by mono_arch_init () */
					target = g_hash_table_lookup (aot_jit_icall_hash, ji->data.name);
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

static gpointer
load_function (MonoAotModule *amodule, const char *name)
{
	return load_function_full (amodule, name, NULL);
}

/*
 * Return the trampoline identified by NAME from the mscorlib AOT file.
 * On ppc64, this returns a function descriptor.
 */
gpointer
mono_aot_get_trampoline_full (const char *name, MonoTrampInfo **out_tinfo)
{
	MonoImage *image;
	MonoAotModule *amodule;

	image = mono_defaults.corlib;
	g_assert (image);

	amodule = image->aot_module;
	g_assert (amodule);

	return mono_create_ftnptr_malloc (load_function_full (amodule, name, out_tinfo));
}

gpointer
mono_aot_get_trampoline (const char *name)
{
	return mono_aot_get_trampoline_full (name, NULL);
}

#ifdef MONOTOUCH
#include <mach/mach.h>

static TrampolinePage* trampoline_pages [MONO_AOT_TRAMP_NUM];

static unsigned char*
get_new_trampoline_from_page (int tramp_type)
{
	MonoAotModule *amodule;
	MonoImage *image;
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
	psize = mono_pagesize ();
	/* the trampoline template page is in the mscorlib module */
	image = mono_defaults.corlib;
	g_assert (image);

	amodule = image->aot_module;
	g_assert (amodule);

	g_assert (amodule->info.tramp_page_size == psize);

	if (tramp_type == MONO_AOT_TRAMP_SPECIFIC)
		tpage = load_function (amodule, "specific_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_STATIC_RGCTX)
		tpage = load_function (amodule, "rgctx_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_IMT_THUNK)
		tpage = load_function (amodule, "imt_trampolines_page");
	else if (tramp_type == MONO_AOT_TRAMP_GSHAREDVT_ARG)
		tpage = load_function (amodule, "gsharedvt_arg_trampolines_page");
	else
		g_error ("Incorrect tramp type for trampolines page");
	g_assert (tpage);
	/*g_warning ("loaded trampolines page at %x", tpage);*/

	/* avoid the unlikely case of looping forever */
	count = 40;
	page = NULL;
	while (page == NULL && count-- > 0) {
		addr = 0;
		/* allocate two contiguous pages of memory: the first page will contain the data (like a local constant pool)
		 * while the second will contain the trampolines.
		 */
		ret = vm_allocate (mach_task_self (), &addr, psize * 2, VM_FLAGS_ANYWHERE);
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
		page->trampolines = (void*)(taddr + amodule->info.tramp_page_code_offsets [tramp_type]);
		page->trampolines_end = (void*)(taddr + psize - 64);
		code = page->trampolines;
		page->trampolines += specific_trampoline_size;
		mono_aot_page_unlock ();
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

	data = (gpointer*)((char*)code - mono_pagesize ());
	data [0] = arg;
	data [1] = tramp;
	/*g_warning ("new trampoline at %p for data %p, tramp %p (stored at %p)", code, arg, tramp, data);*/
	return code;

}

static gpointer
get_new_rgctx_trampoline_from_page (gpointer tramp, gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_STATIC_RGCTX);

	data = (gpointer*)((char*)code - mono_pagesize ());
	data [0] = arg;
	data [1] = tramp;
	/*g_warning ("new rgctx trampoline at %p for data %p, tramp %p (stored at %p)", code, arg, tramp, data);*/
	return code;

}

static gpointer
get_new_imt_trampoline_from_page (gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_IMT_THUNK);

	data = (gpointer*)((char*)code - mono_pagesize ());
	data [0] = arg;
	/*g_warning ("new imt trampoline at %p for data %p, (stored at %p)", code, arg, data);*/
	return code;

}

static gpointer
get_new_gsharedvt_arg_trampoline_from_page (gpointer tramp, gpointer arg)
{
	void *code;
	gpointer *data;

	code = get_new_trampoline_from_page (MONO_AOT_TRAMP_GSHAREDVT_ARG);

	data = (gpointer*)((char*)code - mono_pagesize ());
	data [0] = arg;
	data [1] = tramp;
	/*g_warning ("new rgctx trampoline at %p for data %p, tramp %p (stored at %p)", code, arg, tramp, data);*/
	return code;
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

#ifdef MONOTOUCH
#define	MONOTOUCH_TRAMPOLINES_ERROR ". See http://docs.xamarin.com/ios/troubleshooting for instructions on how to fix this condition."
#else
#define	MONOTOUCH_TRAMPOLINES_ERROR ""
#endif
	if (amodule->trampoline_index [tramp_type] == amodule->info.num_trampolines [tramp_type]) {
		g_error ("Ran out of trampolines of type %d in '%s' (%d)%s\n", 
				 tramp_type, image->name, amodule->info.num_trampolines [tramp_type], MONOTOUCH_TRAMPOLINES_ERROR);
	}
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

		symbol = mono_get_generic_trampoline_name (tramp_type);
		generic_trampolines [tramp_type] = mono_aot_get_trampoline (symbol);
		g_free (symbol);
	}

	tramp = generic_trampolines [tramp_type];
	g_assert (tramp);

	if (USE_PAGE_TRAMPOLINES) {
		code = get_new_specific_trampoline_from_page (tramp, arg1);
		tramp_size = 8;
	} else {
		code = get_numerous_trampoline (MONO_AOT_TRAMP_SPECIFIC, 2, &amodule, &got_offset, &tramp_size);

		amodule->got [got_offset] = tramp;
		amodule->got [got_offset + 1] = arg1;
	}

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

	if (USE_PAGE_TRAMPOLINES) {
		code = get_new_rgctx_trampoline_from_page (addr, ctx);
	} else {
		code = get_numerous_trampoline (MONO_AOT_TRAMP_STATIC_RGCTX, 2, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = ctx;
		amodule->got [got_offset + 1] = addr; 
	}

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (mono_domain_get (), code);
}

gpointer
mono_aot_get_unbox_trampoline (MonoMethod *method)
{
	guint32 method_index = mono_metadata_token_index (method->token) - 1;
	MonoAotModule *amodule;
	gpointer code;
	guint32 *ut, *ut_end, *entry;
	int low, high, entry_index;

	if (method->is_inflated && !mono_method_is_generic_sharable_full (method, FALSE, FALSE, FALSE)) {
		method_index = find_extra_method (method, &amodule);
		if (method_index == 0xffffff && mono_method_is_generic_sharable_full (method, FALSE, FALSE, TRUE)) {
			MonoMethod *shared = mini_get_shared_method_full (method, TRUE, TRUE);
			method_index = find_extra_method (shared, &amodule);
		}
		g_assert (method_index != 0xffffff);
	} else {
		amodule = method->klass->image->aot_module;
		g_assert (amodule);
	}

	ut = amodule->unbox_trampolines;
	ut_end = amodule->unbox_trampolines_end;

	/* Do a binary search in the sorted table */
	code = NULL;
	low = 0;
	high = (ut_end - ut) / 2;
	while (low < high) {
		entry_index = (low + high) / 2;
		entry = &ut [(entry_index * 2)];
		if (entry [0] < method_index) {
			low = entry_index + 1;
		} else if (entry [0] > method_index) {
			high = entry_index;
		} else {
			if (amodule->info.flags & MONO_AOT_FILE_FLAG_DIRECT_METHOD_ADDRESSES)
				code = get_arm_bl_target (entry + 1);
			else
				code = amodule->code + entry [1];
			break;
		}
	}
	g_assert (code);

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (mono_domain_get (), code);
}

gpointer
mono_aot_get_lazy_fetch_trampoline (guint32 slot)
{
	char *symbol;
	gpointer code;
	MonoAotModule *amodule = mono_defaults.corlib->aot_module;
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
		info = mono_domain_alloc0 (mono_get_root_domain (), sizeof (gpointer) * 2);
		info [0] = GUINT_TO_POINTER (slot);
		info [1] = mono_create_specific_trampoline (GUINT_TO_POINTER (slot), MONO_TRAMPOLINE_RGCTX_LAZY_FETCH, mono_get_root_domain (), NULL);
		code = mono_aot_get_static_rgctx_trampoline (info, addr);
		return mono_create_ftnptr (mono_domain_get (), code);
	}

	symbol = mono_get_rgctx_fetch_trampoline_name (slot);
	code = load_function (mono_defaults.corlib->aot_module, symbol);
	g_free (symbol);
	/* The caller expects an ftnptr */
	return mono_create_ftnptr (mono_domain_get (), code);
}

gpointer
mono_aot_get_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	guint32 got_offset;
	gpointer code;
	gpointer *buf;
	int i, index, real_count;
	MonoAotModule *amodule;

	real_count = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (item->is_equals)
			real_count ++;
	}

	/* Save the entries into an array */
	buf = mono_domain_alloc (domain, (real_count + 1) * 2 * sizeof (gpointer));
	index = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];		

		if (!item->is_equals)
			continue;

		g_assert (item->key);

		buf [(index * 2)] = item->key;
		if (item->has_target_code) {
			gpointer *p = mono_domain_alloc (domain, sizeof (gpointer));
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
		code = get_numerous_trampoline (MONO_AOT_TRAMP_IMT_THUNK, 1, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = buf;
	}

	return code;
}

gpointer
mono_aot_get_gsharedvt_arg_trampoline (gpointer arg, gpointer addr)
{
	MonoAotModule *amodule;
	guint8 *code;
	guint32 got_offset;

	if (USE_PAGE_TRAMPOLINES) {
		code = get_new_gsharedvt_arg_trampoline_from_page (addr, arg);
	} else {
		code = get_numerous_trampoline (MONO_AOT_TRAMP_GSHAREDVT_ARG, 2, &amodule, &got_offset, NULL);

		amodule->got [got_offset] = arg;
		amodule->got [got_offset + 1] = addr; 
	}

	/* The caller expects an ftnptr */
	return mono_create_ftnptr (mono_domain_get (), code);
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
#ifndef PLATFORM_WIN32
	guint8* start = (guint8*)ROUND_DOWN (((gssize)ptr), mono_pagesize ());
	int res;

	mono_aot_lock ();
	res = mono_mprotect (start, mono_pagesize (), MONO_MMAP_READ|MONO_MMAP_WRITE|MONO_MMAP_EXEC);
	g_assert (res == 0);

	n_pagefaults ++;
	mono_aot_unlock ();
#endif
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

void
mono_aot_patch_plt_entry (guint8 *code, guint8 *plt_entry, gpointer *got, mgreg_t *regs, guint8 *addr)
{
}

gpointer
mono_aot_get_method_from_vt_slot (MonoDomain *domain, MonoVTable *vtable, int slot)
{
	return NULL;
}

guint32
mono_aot_get_plt_info_offset (mgreg_t *regs, guint8 *code)
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
mono_aot_get_trampoline (const char *name)
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

void
mono_aot_register_jit_icall (const char *name, gpointer addr)
{
}

#endif
