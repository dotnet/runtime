/**
 * \file
 * MonoJitInfo table.
 * Copyright 2012 Xamarin Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_JIT_INFO_H__
#define __MONO_METADATA_JIT_INFO_H__

#include <mono/utils/mono-forward-internal.h>
#include <mono/metadata/object-forward.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/utils/mono-codeman.h>
#include <mono/metadata/mono-hash.h>
#include <mono/metadata/mono-conc-hash.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-internal-hash.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/handle-decl.h>
#include <mono/jit/mono-private-unstable.h>

G_BEGIN_DECLS

typedef struct _MonoJitInfoTable MonoJitInfoTable;
typedef struct _MonoJitInfoTableChunk MonoJitInfoTableChunk;

#define MONO_JIT_INFO_TABLE_CHUNK_SIZE		64

struct _MonoJitInfoTableChunk
{
	int		       refcount;
	volatile int           num_elements;
	volatile gint8        *last_code_end;
	MonoJitInfo *next_tombstone;
	MonoJitInfo * volatile data [MONO_JIT_INFO_TABLE_CHUNK_SIZE];
};

struct _MonoJitInfoTable
{
	MonoDomain	       *domain;
	int			num_chunks;
	int			num_valid;
	MonoJitInfoTableChunk  *chunks [MONO_ZERO_LEN_ARRAY];
};

#define MONO_SIZEOF_JIT_INFO_TABLE (sizeof (struct _MonoJitInfoTable) - MONO_ZERO_LEN_ARRAY * SIZEOF_VOID_P)

typedef GArray MonoAotModuleInfoTable;

typedef struct {
	guint32  flags;
	gint32   exvar_offset;
	gpointer try_start;
	gpointer try_end;
	gpointer handler_start;
	/*
	 * For LLVM compiled code, this is the index of the il clause
	 * associated with this handler.
	 */
	int clause_index;
	uint32_t try_offset;
	uint32_t try_len;
	uint32_t handler_offset;
	uint32_t handler_len;
	union {
		MonoClass *catch_class;
		gpointer filter;
		gpointer handler_end;
	} data;
} MonoJitExceptionInfo;

/*
 * Contains information about the type arguments for generic shared methods.
 */
typedef struct {
	gboolean is_gsharedvt;
} MonoGenericSharingContext;

/* Simplified DWARF location list entry */
typedef struct {
	/* Whenever the value is in a register */
	gboolean is_reg;
	/*
	 * If is_reg is TRUE, the register which contains the value. Otherwise
	 * the base register.
	 */
	int reg;
	/*
	 * If is_reg is FALSE, the offset of the stack location relative to 'reg'.
	 * Otherwise, 0.
	 */
	int offset;
	/*
	 * Offsets of the PC interval where the value is in this location.
	 */
	int from, to;
} MonoDwarfLocListEntry;

typedef struct
{
	MonoGenericSharingContext *generic_sharing_context;
	int nlocs;
	MonoDwarfLocListEntry *locations;
	gint32 this_offset;
	guint8 this_reg;
	gboolean has_this:1;
	gboolean this_in_reg:1;
} MonoGenericJitInfo;

/*
A try block hole is used to represent a non-contiguous part of
of a segment of native code protected by a given .try block.
Usually, a try block is defined as a contiguous segment of code.
But in some cases it's needed to have some parts of it to not be protected.
For example, given "try {} finally {}", the code in the .try block to call
the finally part looks like:

try {
    ...
	call finally_block
	adjust stack
	jump outside try block
	...
} finally {
	...
}

The instructions between the call and the jump should not be under the try block since they happen
after the finally block executes, which means if an async exceptions happens at that point we would
execute the finally clause twice. So, to avoid this, we introduce a hole in the try block to signal
that those instructions are not protected.
*/
typedef struct
{
	guint32 offset;
	guint16 clause;
	guint16 length;
} MonoTryBlockHoleJitInfo;

typedef struct
{
	guint16 num_holes;
	MonoTryBlockHoleJitInfo holes [MONO_ZERO_LEN_ARRAY];
} MonoTryBlockHoleTableJitInfo;

typedef struct
{
	guint32 stack_size;
	guint32 epilog_size;
} MonoArchEHJitInfo;

typedef struct {
	/* Relative to code_start */
	int thunks_offset;
	int thunks_size;
} MonoThunkJitInfo;

typedef struct {
	guint8 *unw_info;
	int unw_info_len;
} MonoUnwindJitInfo;

typedef enum {
	JIT_INFO_NONE = 0,
	JIT_INFO_HAS_GENERIC_JIT_INFO = (1 << 0),
	JIT_INFO_HAS_TRY_BLOCK_HOLES = (1 << 1),
	JIT_INFO_HAS_ARCH_EH_INFO = (1 << 2),
	JIT_INFO_HAS_THUNK_INFO = (1 << 3),
	/*
	 * If this is set, the unwind info is stored in the structure, instead of being pointed to by the
	 * 'unwind_info' field.
	 */
	JIT_INFO_HAS_UNWIND_INFO = (1 << 4)
} MonoJitInfoFlags;

G_ENUM_FUNCTIONS (MonoJitInfoFlags)

struct _MonoJitInfo {
	/* NOTE: These first two elements (method and
	   next_jit_code_hash) must be in the same order and at the
	   same offset as in RuntimeMethod, because of the jit_code_hash
	   internal hash table in MonoDomain. */
	union {
		MonoMethod *method;
		MonoImage *image;
		MonoAotModule *aot_info;
		MonoTrampInfo *tramp_info;
	} d;
	union {
		MonoJitInfo *next_jit_code_hash;
		MonoJitInfo *next_tombstone;
	} n;
	gpointer    code_start;
	guint32     unwind_info;
	int         code_size;
	guint32     num_clauses:15;
	gboolean    has_generic_jit_info:1;
	gboolean    has_try_block_holes:1;
	gboolean    has_arch_eh_info:1;
	gboolean    has_thunk_info:1;
	gboolean    has_unwind_info:1;
	gboolean    from_aot:1;
	gboolean    from_llvm:1;
	gboolean    dbg_attrs_inited:1;
	gboolean    dbg_hidden:1;
	/* Whenever this jit info was loaded in async context */
	gboolean    async:1;
	gboolean    dbg_step_through:1;
	gboolean    dbg_non_user_code:1;
	/*
	 * Whenever this jit info refers to a trampoline.
	 * d.tramp_info contains additional data in this case.
	 */
	gboolean    is_trampoline:1;
	/* Whenever this jit info refers to an interpreter method */
	gboolean    is_interp:1;

	/* FIXME: Embed this after the structure later*/
	gpointer    gc_info; /* Currently only used by SGen */

	gpointer    seq_points;

	MonoJitExceptionInfo clauses [MONO_ZERO_LEN_ARRAY];
	/* There is an optional MonoGenericJitInfo after the clauses */
	/* There is an optional MonoTryBlockHoleTableJitInfo after MonoGenericJitInfo clauses*/
	/* There is an optional MonoArchEHJitInfo after MonoTryBlockHoleTableJitInfo */
	/* There is an optional MonoThunkJitInfo after MonoArchEHJitInfo */
};

#define MONO_SIZEOF_JIT_INFO (offsetof (struct _MonoJitInfo, clauses))

void
mono_jit_info_tables_init (void);

int
mono_jit_info_size (MonoJitInfoFlags flags, int num_clauses, int num_holes);

void
mono_jit_info_init (MonoJitInfo *ji, MonoMethod *method, guint8 *code, int code_size,
					MonoJitInfoFlags flags, int num_clauses, int num_holes);

MonoJitInfoTable *
mono_jit_info_table_new (void);

void
mono_jit_info_table_free (MonoJitInfoTable *table);

void
mono_jit_info_table_add    (MonoJitInfo *ji);

void
mono_jit_info_table_remove (MonoJitInfo *ji);

void
mono_jit_info_add_aot_module (MonoImage *image, gpointer start, gpointer end);

MonoGenericJitInfo*
mono_jit_info_get_generic_jit_info (MonoJitInfo *ji);

MONO_COMPONENT_API MonoGenericSharingContext*
mono_jit_info_get_generic_sharing_context (MonoJitInfo *ji);

void
mono_jit_info_set_generic_sharing_context (MonoJitInfo *ji, MonoGenericSharingContext *gsctx);

MonoTryBlockHoleTableJitInfo*
mono_jit_info_get_try_block_hole_table_info (MonoJitInfo *ji);

MonoArchEHJitInfo*
mono_jit_info_get_arch_eh_info (MonoJitInfo *ji);

MonoThunkJitInfo*
mono_jit_info_get_thunk_info (MonoJitInfo *ji);

MonoUnwindJitInfo*
mono_jit_info_get_unwind_info (MonoJitInfo *ji);

MONO_PROFILER_API MonoJitInfo* mono_jit_info_table_find_internal (gpointer addr, gboolean try_aot, gboolean allow_trampolines);

typedef void (*MonoJitInfoFunc) (MonoJitInfo *ji, gpointer user_data);

MONO_COMPONENT_API void
mono_jit_info_table_foreach_internal (MonoJitInfoFunc func, gpointer user_data);

void
mono_jit_code_hash_init (MonoInternalHashTable *jit_code_hash);

G_END_DECLS

#endif /* __MONO_METADATA_JIT_INFO_H__ */
