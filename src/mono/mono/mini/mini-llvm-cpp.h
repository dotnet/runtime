/**
 * \file
 * LLVM backend
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2009 Novell, Inc.
 */

#ifndef __MONO_MINI_LLVM_CPP_H__
#define __MONO_MINI_LLVM_CPP_H__

#include <glib.h>

#include "llvm-c/Core.h"
#include "llvm-c/ExecutionEngine.h"

#include "llvm-intrinsics-types.h"

#ifdef HAVE_UNWIND_H
#include <unwind.h>
#endif

G_BEGIN_DECLS

/*
 * Keep in sync with the enum in utils/mono-memory-model.h.
 */
typedef enum {
	LLVM_BARRIER_NONE = 0,
	LLVM_BARRIER_ACQ = 1,
	LLVM_BARRIER_REL = 2,
	LLVM_BARRIER_SEQ = 3,
} BarrierKind;

typedef enum {
	LLVM_ATOMICRMW_OP_XCHG = 0,
	LLVM_ATOMICRMW_OP_ADD = 1,
	LLVM_ATOMICRMW_OP_AND = 2,
	LLVM_ATOMICRMW_OP_OR = 3,
} AtomicRMWOp;

typedef enum {
	LLVM_ATTR_NO_UNWIND,
	LLVM_ATTR_NO_INLINE,
	LLVM_ATTR_OPTIMIZE_FOR_SIZE,
	LLVM_ATTR_OPTIMIZE_NONE,
	LLVM_ATTR_IN_REG,
	LLVM_ATTR_STRUCT_RET,
	LLVM_ATTR_NO_ALIAS,
	LLVM_ATTR_BY_VAL,
	LLVM_ATTR_UW_TABLE
} AttrKind;

void
mono_llvm_dump_value (LLVMValueRef value);

void
mono_llvm_dump_module (LLVMModuleRef module);

void
mono_llvm_dump_type (LLVMTypeRef type);

LLVMValueRef
mono_llvm_build_alloca (LLVMBuilderRef builder, LLVMTypeRef Ty,
						LLVMValueRef ArraySize,
						int alignment, const char *Name);

LLVMValueRef
mono_llvm_build_atomic_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
							 const char *Name, gboolean is_volatile, int alignment, BarrierKind barrier);

LLVMValueRef
mono_llvm_build_aligned_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
							  const char *Name, gboolean is_volatile, int alignment);

LLVMValueRef
mono_llvm_build_aligned_store (LLVMBuilderRef builder, LLVMValueRef Val, LLVMValueRef PointerVal,
							   gboolean is_volatile, int alignment);

LLVMValueRef
mono_llvm_build_atomic_store (LLVMBuilderRef builder, LLVMValueRef Val, LLVMValueRef PointerVal,
							  BarrierKind barrier, int alignment);

LLVMValueRef
mono_llvm_build_atomic_rmw (LLVMBuilderRef builder, AtomicRMWOp op, LLVMValueRef ptr, LLVMValueRef val);

LLVMValueRef
mono_llvm_build_fence (LLVMBuilderRef builder, BarrierKind kind);

void
mono_llvm_replace_uses_of (LLVMValueRef var, LLVMValueRef v);

LLVMValueRef
mono_llvm_build_cmpxchg (LLVMBuilderRef builder, LLVMValueRef addr, LLVMValueRef comparand, LLVMValueRef value);

LLVMValueRef
mono_llvm_build_weighted_branch (LLVMBuilderRef builder, LLVMValueRef cond, LLVMBasicBlockRef t, LLVMBasicBlockRef f, uint32_t t_weight, uint32_t f_weight);

LLVMValueRef
mono_llvm_build_exact_ashr (LLVMBuilderRef builder, LLVMValueRef lhs, LLVMValueRef rhs);

void
mono_llvm_add_string_metadata (LLVMValueRef insref, const char* label, const char* text);

void
mono_llvm_set_implicit_branch (LLVMBuilderRef builder, LLVMValueRef branch);

void
mono_llvm_set_must_tailcall (LLVMValueRef call_ins);

LLVMValueRef
mono_llvm_create_constant_data_array (const uint8_t *data, int len);

void
mono_llvm_set_is_constant (LLVMValueRef global_var);

void
mono_llvm_set_call_nonnull_arg (LLVMValueRef calli, int argNo);

void
mono_llvm_set_call_nonnull_ret (LLVMValueRef calli);

void
mono_llvm_set_func_nonnull_arg (LLVMValueRef func, int argNo);

GSList *
mono_llvm_calls_using (LLVMValueRef wrapped_local);

LLVMValueRef *
mono_llvm_call_args (LLVMValueRef calli);

gboolean
mono_llvm_is_nonnull (LLVMValueRef val);

void
mono_llvm_set_call_notailcall (LLVMValueRef call);

void
mono_llvm_set_call_noalias_ret (LLVMValueRef wrapped_calli);

void
mono_llvm_set_alignment_ret (LLVMValueRef val, int alignment);

void
mono_llvm_add_func_attr (LLVMValueRef func, AttrKind kind);

void
mono_llvm_add_func_attr_nv (LLVMValueRef func, const char *attr_name, const char *attr_value);

void
mono_llvm_add_param_attr (LLVMValueRef param, AttrKind kind);

void
mono_llvm_add_param_byval_attr (LLVMValueRef param, LLVMTypeRef type);

void
mono_llvm_add_instr_attr (LLVMValueRef val, int index, AttrKind kind);

void
mono_llvm_add_instr_byval_attr (LLVMValueRef val, int index, LLVMTypeRef type);

#if defined(ENABLE_LLVM) && defined(HAVE_UNWIND_H)
G_EXTERN_C _Unwind_Reason_Code mono_debug_personality (int a, _Unwind_Action b,
	uint64_t c, struct _Unwind_Exception *d, struct _Unwind_Context *e);
#endif

void*
mono_llvm_create_di_builder (LLVMModuleRef module);

gboolean
mono_llvm_can_be_gep (LLVMValueRef base, LLVMValueRef* actual_base, LLVMValueRef* actual_offset);

void*
mono_llvm_di_create_function (void *di_builder, void *cu, LLVMValueRef func, const char *name, const char *mangled_name, const char *dir, const char *file, int line);

void*
mono_llvm_di_create_compile_unit (void *di_builder, const char *cu_name, const char *dir, const char *producer);

void*
mono_llvm_di_create_file (void *di_builder, const char *dir, const char *file);

void*
mono_llvm_di_create_location (void *di_builder, void *scope, int row, int column);

void
mono_llvm_di_builder_finalize (void *di_builder);

void
mono_llvm_set_fast_math (LLVMBuilderRef builder);

void
mono_llvm_di_set_location (LLVMBuilderRef builder, void *loc_md);

LLVMValueRef
mono_llvm_get_or_insert_gc_safepoint_poll (LLVMModuleRef module);

gboolean
mono_llvm_remove_gc_safepoint_poll (LLVMModuleRef module);

typedef struct {
	const char* alias;
	guint32 flag;
} CpuFeatureAliasFlag;

int
mono_llvm_check_cpu_features (const CpuFeatureAliasFlag *features, int length);

LLVMValueRef
mono_llvm_register_intrinsic (LLVMModuleRef module, IntrinsicId id);

LLVMValueRef
mono_llvm_register_overloaded_intrinsic (LLVMModuleRef module, IntrinsicId id, LLVMTypeRef *types, int ntypes);

unsigned int
mono_llvm_get_prim_size_bits (LLVMTypeRef type);

typedef enum {
	LLVM_ASM_SIDE_EFFECT = 1 << 0,
	LLVM_ASM_ALIGN_STACK = 1 << 1,
} MonoLLVMAsmFlags;

LLVMValueRef
mono_llvm_inline_asm (LLVMBuilderRef builder, LLVMTypeRef type,
	const char *asmstr, const char *constraints,
	MonoLLVMAsmFlags flags, LLVMValueRef *args, unsigned num_args,
	const char *name);

G_END_DECLS

#endif /* __MONO_MINI_LLVM_CPP_H__ */
