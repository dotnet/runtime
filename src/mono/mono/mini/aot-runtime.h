/**
 * \file
 * Copyright 2002-2003 Ximian Inc
 * Copyright 2003-2011 Novell Inc
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_AOT_RUNTIME_H__
#define __MONO_AOT_RUNTIME_H__

#include "mini.h"

/* Version number of the AOT file format */
#define MONO_AOT_FILE_VERSION 180

#define MONO_AOT_TRAMP_PAGE_SIZE 16384

/* Constants used to encode different types of methods in AOT */
enum {
	MONO_AOT_METHODREF_MIN = 240,
	/* Image index bigger than METHODREF_MIN */
	MONO_AOT_METHODREF_LARGE_IMAGE_INDEX = 249,
	/* Runtime provided methods on arrays */
	MONO_AOT_METHODREF_ARRAY = 250,
	MONO_AOT_METHODREF_NO_AOT_TRAMPOLINE = 251,
	/* Wrappers */
	MONO_AOT_METHODREF_WRAPPER = 252,
	/* Methods on generic instances */
	MONO_AOT_METHODREF_GINST = 253,
	/* Methods resolve using a METHODSPEC token */
	MONO_AOT_METHODREF_METHODSPEC = 254,
	/* Blob index of the method encoding */
	MONO_AOT_METHODREF_BLOB_INDEX = 255
};

/* Constants used to encode different types of types in AOT */
enum {
	/* typedef index */
	MONO_AOT_TYPEREF_TYPEDEF_INDEX = 1,
	/* typedef index + image index */
	MONO_AOT_TYPEREF_TYPEDEF_INDEX_IMAGE = 2,
	/* typespec token */
	MONO_AOT_TYPEREF_TYPESPEC_TOKEN = 3,
	/* generic inst */
	MONO_AOT_TYPEREF_GINST = 4,
	/* type/method variable */
	MONO_AOT_TYPEREF_VAR = 5,
	/* array */
	MONO_AOT_TYPEREF_ARRAY = 6,
	/* blob index of the type encoding */
	MONO_AOT_TYPEREF_BLOB_INDEX = 7,
	/* ptr */
	MONO_AOT_TYPEREF_PTR = 8
};

/* Trampolines which we have a lot of */
typedef enum {
	MONO_AOT_TRAMP_SPECIFIC = 0,
	MONO_AOT_TRAMP_STATIC_RGCTX = 1,
	MONO_AOT_TRAMP_IMT = 2,
	MONO_AOT_TRAMP_GSHAREDVT_ARG = 3,
	MONO_AOT_TRAMP_FTNPTR_ARG = 4,
	MONO_AOT_TRAMP_UNBOX_ARBITRARY = 5,
	MONO_AOT_TRAMP_NUM = 6
} MonoAotTrampoline;

typedef enum {
	MONO_AOT_FILE_FLAG_WITH_LLVM = 1,
	MONO_AOT_FILE_FLAG_FULL_AOT = 2,
	MONO_AOT_FILE_FLAG_DEBUG = 4,
	MONO_AOT_FILE_FLAG_LLVM_THUMB = 8,
	MONO_AOT_FILE_FLAG_LLVM_ONLY = 16,
	MONO_AOT_FILE_FLAG_SAFEPOINTS = 32,
	MONO_AOT_FILE_FLAG_SEPARATE_DATA = 64,
	MONO_AOT_FILE_FLAG_EAGER_LOAD = 128,
	MONO_AOT_FILE_FLAG_INTERP = 256,
	MONO_AOT_FILE_FLAG_CODE_EXEC_ONLY = 512
} MonoAotFileFlags;

typedef enum {
	MONO_AOT_METHOD_FLAG_NONE = 0,
	MONO_AOT_METHOD_FLAG_HAS_CCTOR = 1,
	MONO_AOT_METHOD_FLAG_GSHAREDVT_VARIABLE = 2,
	MONO_AOT_METHOD_FLAG_HAS_PATCHES = 4,
	MONO_AOT_METHOD_FLAG_HAS_CTX = 8
} MonoAotMethodFlags;

typedef enum {
	MONO_AOT_TABLE_BLOB,
	MONO_AOT_TABLE_CLASS_NAME,
	MONO_AOT_TABLE_CLASS_INFO_OFFSETS,
	MONO_AOT_TABLE_METHOD_INFO_OFFSETS,
	MONO_AOT_TABLE_EX_INFO_OFFSETS,
	MONO_AOT_TABLE_EXTRA_METHOD_INFO_OFFSETS,
	MONO_AOT_TABLE_EXTRA_METHOD_TABLE,
	MONO_AOT_TABLE_GOT_INFO_OFFSETS,
	MONO_AOT_TABLE_LLVM_GOT_INFO_OFFSETS,
	MONO_AOT_TABLE_IMAGE_TABLE,
	MONO_AOT_TABLE_WEAK_FIELD_INDEXES,
	MONO_AOT_TABLE_METHOD_FLAGS_TABLE,
	MONO_AOT_TABLE_NUM
} MonoAotFileTable;

/* This structure is stored in the AOT file */
typedef struct MonoAotFileInfo
{
	/* The version number of the AOT file format, should match MONO_AOT_FILE_VERSION */
	guint32 version;
	/* For alignment */
	guint32 dummy;

	/* All the pointers should be at the start to avoid alignment problems */
	/* Symbols */
#define MONO_AOT_FILE_INFO_FIRST_SYMBOL jit_got
	/* Global Offset Table for JITted code */
	gpointer jit_got;
	/* Mono EH Frame created by llc when using LLVM */
	gpointer mono_eh_frame;
	/* Points to the get_method () function in the LLVM image or NULL */
	gpointer llvm_get_method;
	/* Points to the get_unbox_tramp () function in the LLVM image or NULL */
	gpointer llvm_get_unbox_tramp;
	/* Points to the init_aotconst () function in the LLVM image or NULL */
	gpointer llvm_init_aotconst;
	gpointer jit_code_start;
	gpointer jit_code_end;
	gpointer method_addresses;
	gpointer llvm_unbox_tramp_indexes;
	gpointer llvm_unbox_trampolines;

	/*
	 * Data tables.
	 * One pointer for each entry in MonoAotFileTable.
	 */
	/* Data blob */
	gpointer blob;
	gpointer class_name_table;
	gpointer class_info_offsets;
	gpointer method_info_offsets;
	gpointer ex_info_offsets;
	gpointer extra_method_info_offsets;
	gpointer extra_method_table;
	gpointer got_info_offsets;
	gpointer llvm_got_info_offsets;
	gpointer image_table;
	/* Points to an array of weak field indexes */
	gpointer weak_field_indexes;
	guint8 *method_flags_table;

	gpointer mem_end;
	/* The GUID of the assembly which the AOT image was generated from */
	gpointer assembly_guid;
	/*
	 * The runtime version string for AOT images generated using 'bind-to-runtime-version',
	 * NULL otherwise.
	 */
	char *runtime_version;
	/* Blocks of various kinds of trampolines */
	gpointer specific_trampolines;
	gpointer static_rgctx_trampolines;
	gpointer imt_trampolines;
	gpointer gsharedvt_arg_trampolines;
	gpointer ftnptr_arg_trampolines;
	gpointer unbox_arbitrary_trampolines;
	/* In static mode, points to a table of global symbols for trampolines etc */
	gpointer globals;
	/* Points to a string containing the assembly name*/
	gpointer assembly_name;
	/* Start of Mono's Program Linkage Table */
	gpointer plt;
	/* End of Mono's Program Linkage Table */
	gpointer plt_end;
	gpointer unwind_info;
	/* Points to a table mapping methods to their unbox trampolines */
	gpointer unbox_trampolines;
	/* Points to the end of the previous table */
	gpointer unbox_trampolines_end;
	/* Points to a table of unbox trampoline addresses/offsets */
	gpointer unbox_trampoline_addresses;
#define MONO_AOT_FILE_INFO_LAST_SYMBOL unbox_trampoline_addresses

	/* Scalars */
	/* The index of the first GOT slot used by the PLT */
	guint32 plt_got_offset_base;
	/* The index of the first GOT info slot used by the PLT */
	guint32 plt_got_info_offset_base;
	/* Number of entries in the GOT */
	guint32 got_size;
	/* Number of entries in the LLVM GOT */
	guint32 llvm_got_size;
	/* Number of entries in the PLT */
	guint32 plt_size;
	/* Number of methods */
	guint32 nmethods;
	/* Number of extra methods */
	guint32 nextra_methods;
	/* A union of MonoAotFileFlags */
	guint32 flags;
	/* Optimization flags used to compile the module */
	guint32 opts;
	/* SIMD flags used to compile the module */
	guint32 simd_opts;
	/* Index of the blob entry holding the GC used by this module */
	gint32 gc_name_index;
	guint32 num_rgctx_fetch_trampolines;
	/* These are used for sanity checking when cross-compiling */
	guint32 double_align, long_align, generic_tramp_num, card_table_shift_bits, card_table_mask;
	/* The page size used by trampoline pages */
	guint32 tramp_page_size;
	/* Size of each entry stored at method_addresses */
	guint32 call_table_entry_size;
	/*
	 * The number of GOT entries which need to be preinitialized when the
	 * module is loaded.
	 */
	guint32 nshared_got_entries;
	/* The size of the data file, if MONO_AOT_FILE_FLAG_SEPARATE_DATA is set */
	guint32 datafile_size;
	/* Number of entries in llvm_unbox_tramp_indexes */
	guint32 llvm_unbox_tramp_num;
	/* Size of entries in llvm_unbox_tramp_indexes (2/4) */
	guint32 llvm_unbox_tramp_elemsize;

	/* Arrays */
	/* Offsets for tables inside the data file if MONO_AOT_FILE_FLAG_SEPARATE_DATA is set */
	// FIXME: Sync with AOT
	guint32 table_offsets [MONO_AOT_TABLE_NUM];
	/* Number of trampolines */
	guint32 num_trampolines [MONO_AOT_TRAMP_NUM];
	/* The indexes of the first GOT slots used by the trampolines */
	guint32 trampoline_got_offset_base [MONO_AOT_TRAMP_NUM];
	/* The size of one trampoline */
	guint32 trampoline_size [MONO_AOT_TRAMP_NUM];
	/* The offset where the trampolines begin on a trampoline page */
	guint32 tramp_page_code_offsets [MONO_AOT_TRAMP_NUM];
	/* GUID of aot compilation */
	guint8 aotid[16];
} MonoAotFileInfo;

/* Number of symbols in the MonoAotFileInfo structure */
#define MONO_AOT_FILE_INFO_NUM_SYMBOLS (((G_STRUCT_OFFSET (MonoAotFileInfo, MONO_AOT_FILE_INFO_LAST_SYMBOL) - G_STRUCT_OFFSET (MonoAotFileInfo, MONO_AOT_FILE_INFO_FIRST_SYMBOL)) / sizeof (gpointer)) + 1)

void      mono_aot_init                     (void);
void      mono_aot_cleanup                  (void);
gpointer  mono_aot_get_method               (MonoDomain *domain,
											 MonoMethod *method, MonoError *error);
gpointer  mono_aot_get_method_from_token    (MonoDomain *domain, MonoImage *image, guint32 token, MonoError *error);
gboolean  mono_aot_is_got_entry             (guint8 *code, guint8 *addr);
guint8*   mono_aot_get_plt_entry            (host_mgreg_t *regs, guint8 *code);
guint32   mono_aot_get_plt_info_offset      (gpointer aot_module, guint8 *plt_entry, host_mgreg_t *regs, guint8 *code);
gboolean  mono_aot_get_cached_class_info    (MonoClass *klass, MonoCachedClassInfo *res);
gboolean  mono_aot_get_class_from_name      (MonoImage *image, const char *name_space, const char *name, MonoClass **klass);
MonoJitInfo* mono_aot_find_jit_info         (MonoDomain *domain, MonoImage *image, gpointer addr);
gpointer mono_aot_plt_resolve               (gpointer aot_module, host_mgreg_t *regs, guint8 *code, MonoError *error);
void     mono_aot_patch_plt_entry           (gpointer aot_module, guint8 *code, guint8 *plt_entry, gpointer *got, host_mgreg_t *regs, guint8 *addr);
gpointer mono_aot_get_method_from_vt_slot   (MonoDomain *domain, MonoVTable *vtable, int slot, MonoError *error);
gpointer mono_aot_create_specific_trampoline   (gpointer arg1, MonoTrampolineType tramp_type, MonoDomain *domain, guint32 *code_len);
gpointer mono_aot_get_trampoline            (const char *name);
gpointer mono_aot_get_trampoline_full       (const char *name, MonoTrampInfo **out_tinfo);
gpointer mono_aot_get_unbox_trampoline      (MonoMethod *method, gpointer addr);
gpointer mono_aot_get_lazy_fetch_trampoline (guint32 slot);
gpointer mono_aot_get_static_rgctx_trampoline (gpointer ctx, gpointer addr);
gpointer mono_aot_get_imt_trampoline        (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp);
gpointer mono_aot_get_gsharedvt_arg_trampoline(gpointer arg, gpointer addr);
gpointer mono_aot_get_ftnptr_arg_trampoline (gpointer arg, gpointer addr);
gpointer mono_aot_get_unbox_arbitrary_trampoline (gpointer addr);
guint8*  mono_aot_get_unwind_info           (MonoJitInfo *ji, guint32 *unwind_info_len);
guint32  mono_aot_method_hash               (MonoMethod *method);
gboolean mono_aot_can_dedup                 (MonoMethod *method);
MonoMethod* mono_aot_get_array_helper_from_wrapper (MonoMethod *method);
void     mono_aot_set_make_unreadable       (gboolean unreadable);
gboolean mono_aot_is_pagefault              (void *ptr);
void     mono_aot_handle_pagefault          (void *ptr);

guint32  mono_aot_find_method_index         (MonoMethod *method);
gboolean mono_aot_init_llvm_method          (gpointer aot_module, gpointer method_info, MonoClass *init_class, MonoError *error);
GHashTable *mono_aot_get_weak_field_indexes (MonoImage *image);
MonoAotMethodFlags mono_aot_get_method_flags (guint8 *code);

#ifdef MONO_ARCH_CODE_EXEC_ONLY
typedef guint32 (*MonoAotResolvePltInfoOffset)(gpointer amodule, guint32 plt_entry_index);
#endif

#endif /* __MONO_AOT_RUNTIME_H__ */
