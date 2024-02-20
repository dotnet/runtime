/**
 * \file
 * transform CIL into different opcodes for more
 * efficient interpretation
 *
 * Written by Bernie Solomon (bernard@ugsolutions.com)
 * Copyright (c) 2004.
 */

#include "config.h"
#include <string.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/metadata-update.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-basic-block.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/utils/unlocked.h>
#include <mono/utils/mono-memory-model.h>

#include <mono/mini/mini.h>
#include <mono/mini/mini-runtime.h>
#include <mono/mini/aot-runtime.h>

#include "mintops.h"
#include "interp-internals.h"
#include "interp.h"
#include "transform.h"
#include "tiering.h"
#include "interp-pgo.h"

#if HOST_BROWSER
#include "jiterpreter.h"
#endif

MonoInterpStats mono_interp_stats;

#define DEBUG 0

static const char *stack_type_string [] = { "I4", "I8", "R4", "R8", "O ", "VT", "MP", "F " };

static int stack_type [] = {
	STACK_TYPE_I4, /*I1*/
	STACK_TYPE_I4, /*U1*/
	STACK_TYPE_I4, /*I2*/
	STACK_TYPE_I4, /*U2*/
	STACK_TYPE_I4, /*I4*/
	STACK_TYPE_I8, /*I8*/
	STACK_TYPE_R4, /*R4*/
	STACK_TYPE_R8, /*R8*/
	STACK_TYPE_O,  /*O*/
	STACK_TYPE_VT
};

static GENERATE_TRY_GET_CLASS_WITH_CACHE (intrinsic_klass, "System.Runtime.CompilerServices", "IntrinsicAttribute")
static GENERATE_TRY_GET_CLASS_WITH_CACHE (doesnotreturn_klass, "System.Diagnostics.CodeAnalysis", "DoesNotReturnAttribute")

static gboolean generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error);

static gboolean
has_intrinsic_attribute (MonoMethod *method)
{
	gboolean result = FALSE;
	ERROR_DECL (aerror);
	MonoClass *intrinsic_klass = mono_class_try_get_intrinsic_klass_class ();
	MonoCustomAttrInfo *ainfo = mono_custom_attrs_from_method_checked (method, aerror);
	mono_error_cleanup (aerror); /* FIXME don't swallow the error? */
	if (ainfo) {
		result = intrinsic_klass && mono_custom_attrs_has_attr (ainfo, intrinsic_klass);
		mono_custom_attrs_free (ainfo);
	}
	return result;
}

static gboolean
has_doesnotreturn_attribute (MonoMethod *method)
{
	gboolean result = FALSE;
	ERROR_DECL (aerror);
	MonoClass *doesnotreturn_klass = mono_class_try_get_doesnotreturn_klass_class ();
	MonoCustomAttrInfo *ainfo = mono_custom_attrs_from_method_checked (method, aerror);
	mono_error_cleanup (aerror); /* FIXME don't swallow the error? */
	if (ainfo) {
		result = doesnotreturn_klass && mono_custom_attrs_has_attr (ainfo, doesnotreturn_klass);
		mono_custom_attrs_free (ainfo);
	}
	return result;
}

InterpInst*
interp_new_ins (TransformData *td, int opcode, int len)
{
	InterpInst *new_inst;
	// Size of data region of instruction is length of instruction minus 1 (the opcode slot)
	new_inst = (InterpInst*)mono_mempool_alloc0 (td->mempool, sizeof (InterpInst) + sizeof (guint16) * ((len > 0) ? (len - 1) : 0));
	new_inst->opcode = GINT_TO_OPCODE (opcode);
	new_inst->il_offset = td->current_il_offset;
	return new_inst;
}

// This version need to be used with switch opcode, which doesn't have constant length
static InterpInst*
interp_add_ins_explicit (TransformData *td, int opcode, int len)
{
	InterpInst *new_inst = interp_new_ins (td, opcode, len);
	new_inst->prev = td->cbb->last_ins;
	if (td->cbb->last_ins)
		td->cbb->last_ins->next = new_inst;
	else
		td->cbb->first_ins = new_inst;
	td->cbb->last_ins = new_inst;
	// We should delete this, but is currently used widely to set the args of an instruction
	td->last_ins = new_inst;
	return new_inst;
}

static InterpInst*
interp_add_ins (TransformData *td, int opcode)
{
	return interp_add_ins_explicit (td, opcode, mono_interp_oplen [opcode]);
}

InterpInst*
interp_insert_ins_bb (TransformData *td, InterpBasicBlock *bb, InterpInst *prev_ins, int opcode)
{
	InterpInst *new_inst = interp_new_ins (td, opcode, mono_interp_oplen [opcode]);

	new_inst->prev = prev_ins;

	if (prev_ins) {
		new_inst->next = prev_ins->next;
		prev_ins->next = new_inst;
	} else {
		new_inst->next = bb->first_ins;
		bb->first_ins = new_inst;
	}

	if (new_inst->next == NULL)
		bb->last_ins = new_inst;
	else
		new_inst->next->prev = new_inst;

	new_inst->il_offset = -1;
	return new_inst;
}

/* Inserts a new instruction after prev_ins. prev_ins must be in cbb */
InterpInst*
interp_insert_ins (TransformData *td, InterpInst *prev_ins, int opcode)
{
	return interp_insert_ins_bb (td, td->cbb, prev_ins, opcode);
}

void
interp_clear_ins (InterpInst *ins)
{
	// Clearing instead of removing from the list makes everything easier.
	// We don't change structure of the instruction list, we don't need
	// to worry about updating the il_offset, or whether this instruction
	// was at the start of a basic block etc.
	ins->opcode = MINT_NOP;
}

gboolean
interp_ins_is_nop (InterpInst *ins)
{
	return ins->opcode == MINT_NOP || ins->opcode == MINT_IL_SEQ_POINT;
}

InterpInst*
interp_prev_ins (InterpInst *ins)
{
	ins = ins->prev;
	while (ins && interp_ins_is_nop (ins))
		ins = ins->prev;
	return ins;
}

InterpInst*
interp_next_ins (InterpInst *ins)
{
	ins = ins->next;
	while (ins && interp_ins_is_nop (ins))
		ins = ins->next;
	return ins;
}

static gboolean
check_stack_helper (TransformData *td, int n)
{
	int stack_size = GPTRDIFF_TO_INT (td->sp - td->stack);
	if (stack_size < n) {
		td->has_invalid_code = TRUE;
		return FALSE;
	}
	return TRUE;
}

#define CHECK_STACK(td, n) \
	do { \
		if (!check_stack_helper (td, n)) \
			goto exit; \
	} while (0)

#define CHECK_STACK_RET_VOID(td, n) \
	do { \
		if (!check_stack_helper (td, n)) \
			return; \
	} while (0)

#define CHECK_STACK_RET(td, n, ret) \
	do { \
		if (!check_stack_helper (td, n)) \
			return ret; \
	} while (0)

// We want to allow any block of stack slots to get moved in order for them to be aligned to MINT_STACK_ALIGNMENT
#define ENSURE_STACK_SIZE(td, size) \
	do { \
		if ((size) >= td->max_stack_size) \
			td->max_stack_size = ALIGN_TO (size + MINT_STACK_ALIGNMENT - MINT_STACK_SLOT_SIZE, MINT_STACK_ALIGNMENT); \
	} while (0)

#define ENSURE_I4(td, sp_off) \
	do { \
		if ((td)->sp [-(sp_off)].type == STACK_TYPE_I8) { \
			/* Same representation in memory, nothing to do */ \
			(td)->sp [-(sp_off)].type = STACK_TYPE_I4; \
		} \
	} while (0)

#define CHECK_TYPELOAD(klass) \
	do { \
		if (!(klass) || mono_class_has_failure (klass)) { \
			mono_error_set_for_class_failure (error, klass); \
			goto exit; \
		} \
	} while (0)

#define CHECK_FALLTHRU() \
	do {		 \
		if (G_UNLIKELY (td->ip >= end)) {		\
			interp_generate_ipe_bad_fallthru (td);	\
		}						\
	} while (0)

static void
realloc_stack (TransformData *td)
{
	ptrdiff_t sppos = td->sp - td->stack;

	td->stack_capacity *= 2;
	td->stack = (StackInfo*) g_realloc (td->stack, td->stack_capacity * sizeof (td->stack [0]));
	td->sp = td->stack + sppos;
}

static int
get_stack_size (TransformData *td, StackInfo *sp, int count)
{
	int result = 0;
	for (int i = 0; i < count; i++) {
		result += sp [i].size;
		if (td->vars [sp [i].var].simd)
			result = ALIGN_TO (result, MINT_SIMD_ALIGNMENT);
	}
	return result;
}

static MonoType*
get_type_from_stack (int type, MonoClass *klass)
{
	switch (type) {
		case STACK_TYPE_I4: return m_class_get_byval_arg (mono_defaults.int32_class);
		case STACK_TYPE_I8: return m_class_get_byval_arg (mono_defaults.int64_class);
		case STACK_TYPE_R4: return m_class_get_byval_arg (mono_defaults.single_class);
		case STACK_TYPE_R8: return m_class_get_byval_arg (mono_defaults.double_class);
		case STACK_TYPE_O: return (klass && !m_class_is_valuetype (klass)) ? m_class_get_byval_arg (klass) : m_class_get_byval_arg (mono_defaults.object_class);
		case STACK_TYPE_VT: return m_class_get_byval_arg (klass);
		case STACK_TYPE_MP:
		case STACK_TYPE_F:
			return m_class_get_byval_arg (mono_defaults.int_class);
		default:
			g_assert_not_reached ();
	}
}

int
mono_mint_type (MonoType *type)
{
	if (m_type_is_byref (type))
		return MINT_TYPE_I;
enum_type:
	switch (type->type) {
	case MONO_TYPE_I1:
		return MINT_TYPE_I1;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return MINT_TYPE_U1;
	case MONO_TYPE_I2:
		return MINT_TYPE_I2;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return MINT_TYPE_U2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return MINT_TYPE_I4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return MINT_TYPE_I;
	case MONO_TYPE_R4:
		return MINT_TYPE_R4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MINT_TYPE_I8;
	case MONO_TYPE_R8:
		return MINT_TYPE_R8;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		return MINT_TYPE_O;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			goto enum_type;
		} else
			return MINT_TYPE_VT;
	case MONO_TYPE_TYPEDBYREF:
		return MINT_TYPE_VT;
	case MONO_TYPE_GENERICINST:
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto enum_type;
	case MONO_TYPE_VOID:
		return MINT_TYPE_VOID;
	default:
		g_warning ("got type 0x%02x", type->type);
		g_assert_not_reached ();
	}
	return -1;
}

// This marks the var as renamable, allocating space for additional data.
// The original var data (InterpVar) will have an index that points to this
// additional data.
int
interp_make_var_renamable (TransformData *td, int var)
{
	// Check if already allocated
	if (td->vars [var].ext_index != -1)
		return td->vars [var].ext_index;

	if (td->renamable_vars_size == td->renamable_vars_capacity) {
		td->renamable_vars_capacity *= 2;
		if (td->renamable_vars_capacity == 0)
			td->renamable_vars_capacity = 2;
		td->renamable_vars = (InterpRenamableVar*) g_realloc (td->renamable_vars, td->renamable_vars_capacity * sizeof (InterpRenamableVar));
	}

	int ext_index = td->renamable_vars_size;
	InterpRenamableVar *ext = &td->renamable_vars [ext_index];
	memset (ext, 0, sizeof (InterpRenamableVar));
	ext->var_index = var;

	td->vars [var].ext_index = ext_index;

	td->renamable_vars_size++;

	return ext_index;
}

// This doesn't allocate a new var, rather additional information for fixed renamed vars
int
interp_create_renamed_fixed_var (TransformData *td, int var_index, int renamable_var_index)
{
	g_assert (td->vars [renamable_var_index].ext_index != -1);
	g_assert (td->vars [var_index].ext_index == -1);
	g_assert (td->vars [var_index].renamed_ssa_fixed);

	if (td->renamed_fixed_vars_size == td->renamed_fixed_vars_capacity) {
		td->renamed_fixed_vars_capacity *= 2;
		if (td->renamed_fixed_vars_capacity == 0)
			td->renamed_fixed_vars_capacity = 2;
		td->renamed_fixed_vars = (InterpRenamedFixedVar*) g_realloc (td->renamed_fixed_vars, td->renamed_fixed_vars_capacity * sizeof (InterpRenamedFixedVar));
	}

	int ext_index = td->renamed_fixed_vars_size;
	InterpRenamedFixedVar *ext = &td->renamed_fixed_vars [ext_index];

	ext->var_index = var_index;
	ext->renamable_var_ext_index = td->vars [renamable_var_index].ext_index;
	ext->live_out_bblocks = NULL;
	ext->live_limit_bblocks = NULL;

	td->vars [var_index].ext_index = ext_index;

	td->renamed_fixed_vars_size++;

	return ext_index;
}

/*
 * These are additional locals that can be allocated as we transform the code.
 * They are allocated past the method locals so they are accessed in the same
 * way, with an offset relative to the frame->locals.
 */
static int
interp_create_var_explicit (TransformData *td, MonoType *type, int size)
{
	if (td->vars_size == td->vars_capacity) {
		td->vars_capacity *= 2;
		if (td->vars_capacity == 0)
			td->vars_capacity = 2;
		td->vars = (InterpVar*) g_realloc (td->vars, td->vars_capacity * sizeof (InterpVar));
	}
	int mt = mono_mint_type (type);
	InterpVar *local = &td->vars [td->vars_size];
	memset (local, 0, sizeof (InterpVar));

	local->type = type;
	local->mt = mt;
	if (mt == MINT_TYPE_VT && m_class_is_simd_type (mono_class_from_mono_type_internal (type)))
		local->simd = TRUE;
	local->indirects = 0;
	local->offset = -1;
	local->size = size;
	local->live_start = -1;
	local->bb_index = -1;
	local->ext_index = -1;

	td->vars_size++;
	return td->vars_size - 1;

}

static void
interp_create_dummy_var (TransformData *td)
{
	g_assert (td->dummy_var < 0);
	td->dummy_var = interp_create_var_explicit (td, m_class_get_byval_arg (mono_defaults.void_class), 8);
	td->vars [td->dummy_var].offset = 0;
	td->vars [td->dummy_var].global = TRUE;
}

static int
get_tos_offset (TransformData *td)
{
	if (td->sp == td->stack)
		return 0;
	else
		return td->sp [-1].offset + td->sp [-1].size;
}

// Create a local for sp
static void
interp_create_stack_var (TransformData *td, StackInfo *sp, int type_size)
{
	int local = interp_create_var_explicit (td, get_type_from_stack (sp->type, sp->klass), type_size);

	td->vars [local].execution_stack = TRUE;
	sp->var = local;
}

static void
ensure_stack (TransformData *td, int additional)
{
	guint current_height = GPTRDIFF_TO_UINT (td->sp - td->stack);
	guint new_height = current_height + additional;
	if (new_height > td->stack_capacity)
		realloc_stack (td);
	if (new_height > td->max_stack_height)
		td->max_stack_height = new_height;
}

static void
push_type_explicit (TransformData *td, int type, MonoClass *k, int type_size)
{
	ensure_stack (td, 1);
	StackInfo *sp = td->sp;
	sp->type = GINT_TO_UINT8 (type);
	sp->klass = k;
	sp->flags = 0;
	sp->size = ALIGN_TO (type_size, MINT_STACK_SLOT_SIZE);
	interp_create_stack_var (td, sp, type_size);
	if (!td->optimized) {
		sp->offset = get_tos_offset (td);
		if (td->vars [sp->var].simd)
			sp->offset = ALIGN_TO (sp->offset, MINT_SIMD_ALIGNMENT);
		td->vars [sp->var].stack_offset = sp->offset;
		// Additional space that is allocated for the frame, when we don't run the var offset allocator
		ENSURE_STACK_SIZE(td, sp->offset + sp->size);
	}
	td->sp++;
}

static void
push_var (TransformData *td, int var_index)
{
	InterpVar *var = &td->vars [var_index];
	ensure_stack (td, 1);
	StackInfo *sp = td->sp;
	sp->type = GINT_TO_UINT8 (stack_type [var->mt]);
	sp->klass = mono_class_from_mono_type_internal (var->type);
	sp->flags = 0;
	sp->var = var_index;
	sp->size = ALIGN_TO (var->size, MINT_STACK_SLOT_SIZE);
	td->sp++;
}

// This does not handle the size/offset of the entry. For those cases
// we need to manually pop the top of the stack and push a new entry.
#define SET_SIMPLE_TYPE(s, ty) \
	do { \
		g_assert (ty != STACK_TYPE_VT); \
		g_assert ((s)->type != STACK_TYPE_VT); \
		(s)->type = (ty); \
		(s)->flags = 0; \
		(s)->klass = NULL; \
	} while (0)

#define SET_TYPE(s, ty, k) \
	do { \
		g_assert (ty != STACK_TYPE_VT); \
		g_assert ((s)->type != STACK_TYPE_VT); \
		(s)->type = GINT_TO_UINT8 ((ty)); \
		(s)->flags = 0; \
		(s)->klass = k; \
	} while (0)

static void
set_type_and_var (TransformData *td, StackInfo *sp, int type, MonoClass *klass)
{
	SET_TYPE (sp, type, klass);
	interp_create_stack_var (td, sp, MINT_STACK_SLOT_SIZE);
	if (!td->optimized)
		td->vars [sp->var].stack_offset = sp->offset;
}

static void
set_simple_type_and_var (TransformData *td, StackInfo *sp, int type)
{
	set_type_and_var (td, sp, type, NULL);
}

static void
push_type (TransformData *td, int type, MonoClass *k)
{
	// We don't really care about the exact size for non-valuetypes
	push_type_explicit (td, type, k, MINT_STACK_SLOT_SIZE);
}

static void
push_simple_type (TransformData *td, int type)
{
	push_type (td, type, NULL);
}

static void
push_type_vt (TransformData *td, MonoClass *k, int size)
{
	push_type_explicit (td, STACK_TYPE_VT, k, size);
}

static void
push_types (TransformData *td, StackInfo *types, int count)
{
	for (int i = 0; i < count; i++)
		push_type_explicit (td, types [i].type, types [i].klass, types [i].size);
}

int
interp_get_mov_for_type (int mt, gboolean needs_sext)
{
	switch (mt) {
	case MINT_TYPE_I1:
	case MINT_TYPE_U1:
	case MINT_TYPE_I2:
	case MINT_TYPE_U2:
		if (needs_sext)
			return MINT_MOV_I4_I1 + mt;
		else
			return MINT_MOV_4;
	case MINT_TYPE_I4:
	case MINT_TYPE_R4:
		return MINT_MOV_4;
	case MINT_TYPE_I8:
	case MINT_TYPE_R8:
		return MINT_MOV_8;
	case MINT_TYPE_O:
#if SIZEOF_VOID_P == 8
		return MINT_MOV_8;
#else
		return MINT_MOV_4;
#endif
	case MINT_TYPE_VT:
		return MINT_MOV_VT;
	}
	g_assert_not_reached ();
}

static guint16
get_mint_type_size (int mt)
{
	switch (mt) {
	case MINT_TYPE_I1:
	case MINT_TYPE_U1:
		return 1;
	case MINT_TYPE_I2:
	case MINT_TYPE_U2:
		return 2;
	case MINT_TYPE_I4:
	case MINT_TYPE_R4:
		return 4;
	case MINT_TYPE_I8:
	case MINT_TYPE_R8:
		return 8;
	case MINT_TYPE_O:
#if SIZEOF_VOID_P == 8
		return 8;
#else
		return 4;
#endif
	}
	g_assert_not_reached ();
}


// Should be called when td->cbb branches to newbb and newbb can have a stack state
static void
fixup_newbb_stack_locals (TransformData *td, InterpBasicBlock *newbb)
{
	// If not optimized, it is enough for vars to have same offset on the stack. It is not
	// mandatory for sregs and dregs to match.
	if (!td->optimized)
		return;
	if (newbb->stack_height <= 0)
		return;

	for (int i = 0; i < newbb->stack_height; i++) {
		int sloc = td->stack [i].var;
		int dloc = newbb->stack_state [i].var;
		if (sloc != dloc) {
			int mt = td->vars [sloc].mt;
			int mov_op = interp_get_mov_for_type (mt, FALSE);

			// FIXME can be hit in some IL cases. Should we merge the stack states ? (b41002.il)
			// g_assert (mov_op == interp_get_mov_for_type (td->vars [dloc].mt, FALSE));

			interp_add_ins (td, mov_op);
			interp_ins_set_sreg (td->last_ins, td->stack [i].var);
			interp_ins_set_dreg (td->last_ins, newbb->stack_state [i].var);

			if (mt == MINT_TYPE_VT) {
				g_assert (td->vars [sloc].size == td->vars [dloc].size);
				td->last_ins->data [0] = GINT_TO_UINT16 (td->vars [sloc].size);
			}
		}
	}
}

static void
merge_stack_type_information (StackInfo *state1, StackInfo *state2, int len)
{
	// Discard type information if we have type conflicts for stack contents
	for (int i = 0; i < len; i++) {
		if (state1 [i].klass != state2 [i].klass) {
			state1 [i].klass = NULL;
			state2 [i].klass = NULL;
		}
	}
}

// Initializes stack state at entry to bb, based on the current stack state
static void
init_bb_stack_state (TransformData *td, InterpBasicBlock *bb)
{
	// Check if already initialized
	if (bb->stack_height >= 0) {
		merge_stack_type_information (td->stack, bb->stack_state, bb->stack_height);
	} else {
		bb->stack_height = GPTRDIFF_TO_INT (td->sp - td->stack);
		if (bb->stack_height > 0) {
			int size = bb->stack_height * sizeof (td->stack [0]);
			bb->stack_state = (StackInfo*)mono_mempool_alloc (td->mempool, size);
			memcpy (bb->stack_state, td->stack, size);
		}
	}
}

static void
handle_branch (TransformData *td, int long_op, int offset)
{
	int target = GPTRDIFF_TO_INT (td->ip + offset - td->il_code);
	if (target < 0 || target >= td->code_size)
		g_assert_not_reached ();
	/* Add exception checkpoint or safepoint for backward branches */
	if (offset < 0) {
		if (mono_threads_are_safepoints_enabled ())
			interp_add_ins (td, MINT_SAFEPOINT);
	}

	InterpBasicBlock *target_bb = td->offset_to_bb [target];
	g_assert (target_bb);

	if (offset < 0)
		target_bb->backwards_branch_target = TRUE;

	if (offset < 0 && td->sp == td->stack && !td->inlined_method) {
		// Backwards branch inside unoptimized method where the IL stack is empty
		// This is candidate for a patchpoint
		target_bb->patchpoint_bb = TRUE;
		if (mono_interp_tiering_enabled () && !target_bb->patchpoint_data && td->optimized) {
			// The optimized imethod will store mapping from bb index to native offset so it
			// can resume execution in the optimized method, once we tier up in patchpoint
			td->patchpoint_data_n++;
			target_bb->patchpoint_data = TRUE;
		}
	}

	fixup_newbb_stack_locals (td, target_bb);
	if (offset > 0)
		init_bb_stack_state (td, target_bb);

	if (long_op != MINT_CALL_HANDLER) {
		if (td->cbb->no_inlining)
			target_bb->jump_targets--;
		// We don't link finally blocks into the cfg (or other handler blocks for that matter)
		interp_link_bblocks (td, td->cbb, target_bb);
	}

	interp_add_ins (td, long_op);
	td->last_ins->info.target_bb = target_bb;
}

static int
try_fold_one_arg_branch (TransformData *td, int mint_op)
{
	if (td->last_ins && MINT_IS_LDC_I4 (td->last_ins->opcode) && td->last_ins->dreg == td->sp [0].var) {
		gint32 val = interp_get_const_from_ldc_i4 (td->last_ins);
		interp_clear_ins (td->last_ins);

		switch (mint_op) {
			case MINT_BRFALSE_I4: return !val;
			case MINT_BRTRUE_I4: return !!val;
			default:
				g_assert_not_reached ();
		}
	}

	return -1;
}

static gboolean
one_arg_branch(TransformData *td, int mint_op, int offset, int inst_size)
{
	CHECK_STACK_RET (td, 1, TRUE);
	int type = td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP ? STACK_TYPE_I : td->sp [-1].type;
	int long_op = mint_op + type - STACK_TYPE_I4;
	--td->sp;
	if (offset) {
		int cond_result = try_fold_one_arg_branch (td, mint_op);
		if (cond_result != -1) {
			if (cond_result) {
				handle_branch (td, MINT_BR, offset + inst_size);
				return FALSE;
			} else {
				// branch condition always false, it is a NOP
				int target = GPTRDIFF_TO_INT (td->ip + offset + inst_size - td->il_code);
				td->offset_to_bb [target]->jump_targets--;
				return TRUE;
			}
		} else {
			handle_branch (td, long_op, offset + inst_size);
			interp_ins_set_sreg (td->last_ins, td->sp->var);
			return TRUE;
		}
	} else {
		interp_add_ins (td, MINT_NOP);
		return TRUE;
	}
}

static void
interp_add_conv (TransformData *td, StackInfo *sp, InterpInst *prev_ins, int type, int conv_op)
{
	InterpInst *new_inst;
	if (prev_ins)
		new_inst = interp_insert_ins (td, prev_ins, conv_op);
	else
		new_inst = interp_add_ins (td, conv_op);

	interp_ins_set_sreg (new_inst, sp->var);
	set_simple_type_and_var (td, sp, type);
	interp_ins_set_dreg (new_inst, sp->var);
}

static int
try_fold_two_arg_branch (TransformData *td, int mint_op)
{
	InterpInst *src2 = td->last_ins;
	if (!src2 || !MINT_IS_LDC_I4 (src2->opcode) || src2->dreg != td->sp [1].var)
		return -1;
	InterpInst *src1 = interp_prev_ins (src2);
	if (!src1 || !MINT_IS_LDC_I4 (src1->opcode) || src1->dreg != td->sp [0].var)
		return -1;

	gint32 val1 = interp_get_const_from_ldc_i4 (src1);
	gint32 val2 = interp_get_const_from_ldc_i4 (src2);

	int result = -1;
	switch (mint_op) {
		case MINT_BEQ_I4: result = val1 == val2; break;
		case MINT_BGE_I4: result = val1 >= val2; break;
		case MINT_BGT_I4: result = val1 > val2; break;
		case MINT_BLT_I4: result = val1 < val2; break;
		case MINT_BLE_I4: result = val1 <= val2; break;

		case MINT_BNE_UN_I4: result = val1 != val2; break;
		case MINT_BGE_UN_I4: result = (guint32)val1 >= (guint32)val2; break;
		case MINT_BGT_UN_I4: result = (guint32)val1 > (guint32)val2; break;
		case MINT_BLE_UN_I4: result = (guint32)val1 <= (guint32)val2; break;
		case MINT_BLT_UN_I4: result = (guint32)val1 < (guint32)val2; break;
		default:
			return -1;
	}
	interp_clear_ins (src1);
	interp_clear_ins (src2);

	return result;
}

static gboolean
two_arg_branch(TransformData *td, int mint_op, int offset, int inst_size)
{
	CHECK_STACK_RET (td, 2, TRUE);
	int type1 = td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP ? STACK_TYPE_I : td->sp [-1].type;
	int type2 = td->sp [-2].type == STACK_TYPE_O || td->sp [-2].type == STACK_TYPE_MP ? STACK_TYPE_I : td->sp [-2].type;

	if (type1 == STACK_TYPE_I4 && type2 == STACK_TYPE_I8) {
		// The il instruction starts with the actual branch, and not with the conversion opcodes
		interp_add_conv (td, td->sp - 1, td->last_ins, STACK_TYPE_I8, MINT_CONV_I8_I4);
		type1 = STACK_TYPE_I8;
	} else if (type1 == STACK_TYPE_I8 && type2 == STACK_TYPE_I4) {
		interp_add_conv (td, td->sp - 2, td->last_ins, STACK_TYPE_I8, MINT_CONV_I8_I4);
	} else if (type1 == STACK_TYPE_R4 && type2 == STACK_TYPE_R8) {
		interp_add_conv (td, td->sp - 1, td->last_ins, STACK_TYPE_R8, MINT_CONV_R8_R4);
		type1 = STACK_TYPE_R8;
	} else if (type1 == STACK_TYPE_R8 && type2 == STACK_TYPE_R4) {
		interp_add_conv (td, td->sp - 2, td->last_ins, STACK_TYPE_R8, MINT_CONV_R8_R4);
	} else if (type1 != type2) {
		g_warning("%s.%s: branch type mismatch %d %d",
			m_class_get_name (td->method->klass), td->method->name,
			td->sp [-1].type, td->sp [-2].type);
	}

	int long_op = mint_op + type1 - STACK_TYPE_I4;
	td->sp -= 2;
	if (offset) {
		int cond_result = try_fold_two_arg_branch (td, mint_op);
		if (cond_result != -1) {
			if (cond_result) {
				handle_branch (td, MINT_BR, offset + inst_size);
				return FALSE;
			} else {
				// branch condition always false, it is a NOP
				int target = GPTRDIFF_TO_INT (td->ip + offset + inst_size - td->il_code);
				td->offset_to_bb [target]->jump_targets--;
				return TRUE;
			}
		} else {
			handle_branch (td, long_op, offset + inst_size);
			interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			return TRUE;
		}
	} else {
		interp_add_ins (td, MINT_NOP);
		return TRUE;
	}
}

static void
unary_arith_op(TransformData *td, int mint_op)
{
	CHECK_STACK_RET_VOID(td, 1);
	int op = mint_op + td->sp [-1].type - STACK_TYPE_I4;
	td->sp--;
	interp_add_ins (td, op);
	interp_ins_set_sreg (td->last_ins, td->sp [0].var);
	push_simple_type (td, td->sp [0].type);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
}

static void
binary_arith_op(TransformData *td, int mint_op)
{
	CHECK_STACK_RET_VOID(td, 2);
	int type1 = td->sp [-2].type;
	int type2 = td->sp [-1].type;
	int op;
#if SIZEOF_VOID_P == 8
	if ((type1 == STACK_TYPE_MP || type1 == STACK_TYPE_I8) && type2 == STACK_TYPE_I4) {
		interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_I4);
		type2 = STACK_TYPE_I8;
	}
	if (type1 == STACK_TYPE_I4 && (type2 == STACK_TYPE_MP || type2 == STACK_TYPE_I8)) {
		interp_add_conv (td, td->sp - 2, NULL, STACK_TYPE_I8, MINT_CONV_I8_I4);
		type1 = STACK_TYPE_I8;
	}
#endif
	if (type1 == STACK_TYPE_R8 && type2 == STACK_TYPE_R4) {
		interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
		type2 = STACK_TYPE_R8;
	}
	if (type1 == STACK_TYPE_R4 && type2 == STACK_TYPE_R8) {
		interp_add_conv (td, td->sp - 2, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
		type1 = STACK_TYPE_R8;
	}
	if (type1 == STACK_TYPE_MP)
		type1 = STACK_TYPE_I;
	if (type2 == STACK_TYPE_MP)
		type2 = STACK_TYPE_I;
	if (type1 != type2) {
		g_warning("%s.%s: %04x arith type mismatch %s %d %d",
			m_class_get_name (td->method->klass), td->method->name,
			td->ip - td->il_code, mono_interp_opname (mint_op), type1, type2);
	}
	op = mint_op + type1 - STACK_TYPE_I4;
	td->sp -= 2;
	interp_add_ins (td, op);
	interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
	push_simple_type (td, type1);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
}

static void
shift_op(TransformData *td, int mint_op)
{
	CHECK_STACK_RET_VOID(td, 2);
	int op = mint_op + td->sp [-2].type - STACK_TYPE_I4;
	if (td->sp [-1].type != STACK_TYPE_I4) {
		g_warning("%s.%s: shift type mismatch %d",
			m_class_get_name (td->method->klass), td->method->name,
			td->sp [-2].type);
	}
	td->sp -= 2;
	interp_add_ins (td, op);
	interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
	push_simple_type (td, td->sp [0].type);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
}

static int
can_store (int st_value, int vt_value)
{
	if (st_value == STACK_TYPE_O || st_value == STACK_TYPE_MP || st_value == STACK_TYPE_F)
		st_value = STACK_TYPE_I;
	if (vt_value == STACK_TYPE_O || vt_value == STACK_TYPE_MP || vt_value == STACK_TYPE_F)
		vt_value = STACK_TYPE_I;
	return st_value == vt_value;
}

static MonoType*
get_arg_type_exact (TransformData *td, int n, int *mt)
{
	MonoType *type;
	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;

	if (hasthis && n == 0)
		type = m_class_get_byval_arg (td->method->klass);
	else
		type = mono_method_signature_internal (td->method)->params [n - !!hasthis];

	if (mt)
		*mt = mono_mint_type (type);

	return type;
}

static void
load_arg(TransformData *td, int n)
{
	gint32 size = 0;
	int mt;
	MonoClass *klass = NULL;
	MonoType *type;
	gboolean hasthis = mono_method_signature_internal (td->method)->hasthis;

	type = get_arg_type_exact (td, n, &mt);

	if (mt == MINT_TYPE_VT) {
		klass = mono_class_from_mono_type_internal (type);
		if (mono_method_signature_internal (td->method)->pinvoke && !mono_method_signature_internal (td->method)->marshalling_disabled)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);

		if (hasthis && n == 0) {
			mt = MINT_TYPE_I;
			klass = NULL;
			push_type (td, stack_type [mt], klass);
		} else {
			g_assert (size < G_MAXUINT16);
			push_type_vt (td, klass, size);
		}
	} else {
		if ((hasthis || mt == MINT_TYPE_I) && n == 0) {
			// Special case loading of the first ptr sized argument
			if (mt != MINT_TYPE_O)
				mt = MINT_TYPE_I;
		} else {
			if (mt == MINT_TYPE_O)
				klass = mono_class_from_mono_type_internal (type);
		}
		push_type (td, stack_type [mt], klass);
	}
	interp_add_ins (td, interp_get_mov_for_type (mt, TRUE));
	interp_ins_set_sreg (td->last_ins, n);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	if (mt == MINT_TYPE_VT)
		td->last_ins->data [0] = GINT32_TO_UINT16 (size);
}

static void
store_arg(TransformData *td, int n)
{
	gint32 size = 0;
	int mt;
	CHECK_STACK_RET_VOID (td, 1);
	MonoType *type;

	type = get_arg_type_exact (td, n, &mt);

	if (mt == MINT_TYPE_VT) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (mono_method_signature_internal (td->method)->pinvoke && !mono_method_signature_internal (td->method)->marshalling_disabled)
			size = mono_class_native_size (klass, NULL);
		else
			size = mono_class_value_size (klass, NULL);
		g_assert (size < G_MAXUINT16);
	}
	--td->sp;
	interp_add_ins (td, interp_get_mov_for_type (mt, FALSE));
	interp_ins_set_sreg (td->last_ins, td->sp [0].var);
	interp_ins_set_dreg (td->last_ins, n);
	if (mt == MINT_TYPE_VT)
		td->last_ins->data [0] = GINT32_TO_UINT16 (size);
}

static void
load_local (TransformData *td, int local)
{
	int mt = td->vars [local].mt;
	gint32 size = td->vars [local].size;
	MonoType *type = td->vars [local].type;

	if (mt == MINT_TYPE_VT) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		push_type_vt (td, klass, size);
	} else {
		MonoClass *klass = NULL;
		if (mt == MINT_TYPE_O)
			klass = mono_class_from_mono_type_internal (type);
		push_type (td, stack_type [mt], klass);
	}
	interp_add_ins (td, interp_get_mov_for_type (mt, TRUE));
	interp_ins_set_sreg (td->last_ins, local);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	if (mt == MINT_TYPE_VT)
		td->last_ins->data [0] = GINT32_TO_UINT16 (size);
}

static void
store_local (TransformData *td, int local)
{
	int mt = td->vars [local].mt;
	CHECK_STACK_RET_VOID (td, 1);

#if SIZEOF_VOID_P == 8
	// nint and int32 can be used interchangeably. Add implicit conversions.
	if (td->sp [-1].type == STACK_TYPE_I4 && stack_type [mt] == STACK_TYPE_I8)
		interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_I4);
	else if (td->sp [-1].type == STACK_TYPE_I8 && stack_type [mt] == STACK_TYPE_I4)
		interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_MOV_8);
#endif
	if (td->sp [-1].type == STACK_TYPE_R4 && stack_type [mt] == STACK_TYPE_R8)
		interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
	else if (td->sp [-1].type == STACK_TYPE_R8 && stack_type [mt] == STACK_TYPE_R4)
		interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R4, MINT_CONV_R4_R8);

	if (!can_store(td->sp [-1].type, stack_type [mt])) {
		g_error ("%s.%s: Store local stack type mismatch %d %d",
			m_class_get_name (td->method->klass), td->method->name,
			stack_type [mt], td->sp [-1].type);
	}
	--td->sp;
	interp_add_ins (td, interp_get_mov_for_type (mt, FALSE));
	interp_ins_set_sreg (td->last_ins, td->sp [0].var);
	interp_ins_set_dreg (td->last_ins, local);
	if (mt == MINT_TYPE_VT)
		td->last_ins->data [0] = GINT32_TO_UINT16 (td->vars [local].size);
}

static void
init_last_ins_call (TransformData *td)
{
	td->last_ins->flags |= INTERP_INST_FLAG_CALL;
	td->last_ins->info.call_info = (InterpCallInfo*)mono_mempool_alloc (td->mempool, sizeof (InterpCallInfo));
	td->last_ins->info.call_info->call_args = NULL;
}

static guint32
get_data_item_wide_index (TransformData *td, void *ptr, gboolean *new_slot)
{
	gpointer p = g_hash_table_lookup (td->data_hash, ptr);
	guint32 index;
	if (p != NULL) {
		if (new_slot)
			*new_slot = FALSE;
		return GPOINTER_TO_UINT (p) - 1;
	}
	if (td->max_data_items == td->n_data_items) {
		td->max_data_items = td->n_data_items == 0 ? 16 : 2 * td->max_data_items;
		td->data_items = (gpointer*)g_realloc (td->data_items, td->max_data_items * sizeof(td->data_items [0]));
	}
	index = td->n_data_items;
	td->data_items [index] = ptr;
	++td->n_data_items;
	g_hash_table_insert (td->data_hash, ptr, GUINT_TO_POINTER (index + 1));
	if (new_slot)
		*new_slot = TRUE;
	return index;
}

static guint16
get_data_item_index (TransformData *td, void *ptr)
{
	guint32 index = get_data_item_wide_index (td, ptr, NULL);
	g_assertf (index <= G_MAXUINT16, "Interpreter data item index 0x%x for method '%s' overflows", index, td->method->name);
	return (guint16)index;
}

static guint16
get_data_item_index_imethod (TransformData *td, InterpMethod *imethod)
{
	gboolean new_slot;
	guint32 index = get_data_item_wide_index (td, imethod, &new_slot);
	g_assertf (index <= G_MAXUINT16, "Interpreter data item index 0x%x for method '%s' overflows", index, td->method->name);
	if (new_slot && imethod && !imethod->optimized)
		td->imethod_items = g_slist_prepend (td->imethod_items, (gpointer)(gsize)index);
	return GUINT32_TO_UINT16 (index);
}

static gboolean
is_data_item_wide_index (guint32 data_item_index)
{
	return data_item_index > G_MAXUINT16;
}

static guint16
get_data_item_index_nonshared (TransformData *td, void *ptr)
{
	guint index;
	if (td->max_data_items == td->n_data_items) {
		td->max_data_items = td->n_data_items == 0 ? 16 : 2 * td->max_data_items;
		td->data_items = (gpointer*)g_realloc (td->data_items, td->max_data_items * sizeof(td->data_items [0]));
	}
	index = td->n_data_items;
	td->data_items [index] = ptr;
	++td->n_data_items;
	return GUINT_TO_UINT16 (index);
}

gboolean
mono_interp_jit_call_supported (MonoMethod *method, MonoMethodSignature *sig)
{
	GSList *l;

	if (!mono_jit_call_can_be_supported_by_interp (method, sig, mono_llvm_only))
		return FALSE;

	if (mono_aot_only && m_class_get_image (method->klass)->aot_module && !(method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)) {
		ERROR_DECL (error);
		mono_class_init_internal (method->klass);
		gpointer addr = mono_aot_get_method (method, error);
		if (addr && is_ok (error)) {
			MonoAotMethodFlags flags = mono_aot_get_method_flags (addr);
			if (!(flags & MONO_AOT_METHOD_FLAG_INTERP_ENTRY_ONLY))
				return TRUE;
		}
	}

	for (l = mono_interp_jit_classes; l; l = l->next) {
		const char *class_name = (const char*)l->data;
		// FIXME: Namespaces
		if (!strcmp (m_class_get_name (method->klass), class_name))
			return TRUE;
	}

	//return TRUE;
	return FALSE;
}

#ifdef ENABLE_EXPERIMENT_TIERED
static gboolean
jit_call2_supported (MonoMethod *method, MonoMethodSignature *sig)
{
	if (sig->param_count > 6)
		return FALSE;
	if (sig->pinvoke)
		return FALSE;
	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
		return FALSE;
	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		return FALSE;
	if (method->is_inflated)
		return FALSE;
	if (method->string_ctor)
		return FALSE;

	return TRUE;
}
#endif

static void
emit_ldptr (TransformData *td, gpointer data)
{
	interp_add_ins (td, MINT_LDPTR);
	push_simple_type (td, STACK_TYPE_I);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	td->last_ins->data [0] = get_data_item_index (td, data);
}

static MintICallSig
interp_get_icall_sig (MonoMethodSignature *sig);

static gpointer
imethod_alloc0 (TransformData *td, size_t size)
{
	if (td->rtm->method->dynamic)
		return mono_dyn_method_alloc0 (td->rtm->method, (guint)size);
	else
		return mono_mem_manager_alloc0 (td->mem_manager, (guint)size);
}

static void
interp_generate_icall_throw (TransformData *td, MonoJitICallInfo *icall_info, gpointer arg1, gpointer arg2)
{
	int num_args = icall_info->sig->param_count;
	if (num_args > 0)
		emit_ldptr (td, arg1);
	if (num_args > 1)
		emit_ldptr (td, arg2);

	td->sp -= num_args;

	interp_add_ins (td, MINT_ICALL);
	interp_ins_set_dummy_dreg (td->last_ins, td);
	interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
	td->last_ins->data [0] = interp_get_icall_sig (icall_info->sig);
	td->last_ins->data [1] = get_data_item_index (td, (gpointer)icall_info->func);
	init_last_ins_call (td);
	if (td->optimized) {
		if (num_args) {
			int *call_args = (int*)mono_mempool_alloc (td->mempool, (num_args + 1) * sizeof (int));
			for (int i = 0; i < num_args; i++)
				call_args [i] = td->sp [i].var;
			call_args [num_args] = -1;
			td->last_ins->info.call_info->call_args = call_args;
		}
	} else {
		td->last_ins->info.call_info->call_offset = get_tos_offset (td);
	}
}

static void
interp_generate_mae_throw (TransformData *td, MonoMethod *method, MonoMethod *target_method)
{
	MonoJitICallInfo *info = &mono_get_jit_icall_info ()->mono_throw_method_access;
	interp_generate_icall_throw (td, info, method, target_method);
}

static void
interp_generate_void_throw (TransformData *td, MonoJitICallId icall_id)
{
	MonoJitICallInfo *info = mono_find_jit_icall_info (icall_id);
	interp_generate_icall_throw (td, info, NULL, NULL);
}

static void
interp_generate_ipe_throw_with_msg (TransformData *td, MonoError *error_msg)
{
	MonoJitICallInfo *info = &mono_get_jit_icall_info ()->mono_throw_invalid_program;
	char *msg = mono_mem_manager_strdup (td->mem_manager, mono_error_get_message (error_msg));
	interp_generate_icall_throw (td, info, msg, NULL);
}

static void
interp_generate_ipe_bad_fallthru (TransformData *td)
{
	ERROR_DECL (bad_fallthru_error);
	char *method_code = mono_disasm_code_one (NULL, td->method, td->ip, NULL);
	mono_error_set_invalid_program (bad_fallthru_error, "Invalid IL (conditional fallthru past end of method) due to: %s", method_code);
	interp_generate_ipe_throw_with_msg (td, bad_fallthru_error);
	g_free (method_code);
	mono_error_cleanup (bad_fallthru_error);
}


int
interp_create_var (TransformData *td, MonoType *type)
{
	int size, align;

	size = mono_type_size (type, &align);
	g_assert (align <= MINT_STACK_SLOT_SIZE);

	return interp_create_var_explicit (td, type, size);
}

/*
 * ins_offset is the associated offset of this instruction
 * if ins is null, it means the data belongs to an instruction that was
 * emitted in the final code
 * ip is the address where the arguments of the instruction are located
 */
static char*
interp_dump_ins_data (InterpInst *ins, gint32 ins_offset, const guint16 *data, int opcode, gpointer *data_items)
{
	GString *str = g_string_new ("");
	int target;

	switch (mono_interp_opargtype [opcode]) {
	case MintOpNoArgs:
		break;
	case MintOpUShortInt:
		g_string_append_printf (str, " %u", *(guint16*)data);
		break;
	case MintOpTwoShorts:
		g_string_append_printf (str, " %d,%d", *(gint16*)data, *(gint16 *)(data + 1));
		break;
	case MintOpTwoInts:
		g_string_append_printf (str, " %u,%u", (guint32)READ32(data), (guint32)READ32(data + 2));
		break;
	case MintOpShortAndInt:
		g_string_append_printf (str, " %u,%u", *(guint16*)data, (guint32)READ32(data + 1));
		break;
	case MintOpShortInt:
		g_string_append_printf (str, " %d", *(gint16*)data);
		break;
	case MintOpClassToken: {
		MonoClass *klass = (MonoClass*)data_items [*(guint16*)data];
		g_string_append_printf (str, " %s.%s", m_class_get_name_space (klass), m_class_get_name (klass));
		break;
	}
	case MintOpVTableToken: {
		MonoVTable *vtable = (MonoVTable*)data_items [*(guint16*)data];
		g_string_append_printf (str, " %s.%s", m_class_get_name_space (vtable->klass), m_class_get_name (vtable->klass));
		break;
	}
	case MintOpMethodToken: {
		InterpMethod *imethod = (InterpMethod*)data_items [*(guint16*)data];
		char *name = mono_method_full_name (imethod->method, TRUE);
		g_string_append_printf (str, " %s", name);
		g_free (name);
		break;
	}
	case MintOpInt:
		g_string_append_printf (str, " %d", (gint32)READ32 (data));
		break;
	case MintOpLongInt:
		g_string_append_printf (str, " %" PRId64, (gint64)READ64 (data));
		break;
	case MintOpFloat: {
		gint32 tmp = READ32 (data);
		g_string_append_printf (str, " %g", * (float *)&tmp);
		break;
	}
	case MintOpDouble: {
		gint64 tmp = READ64 (data);
		g_string_append_printf (str, " %g", * (double *)&tmp);
		break;
	}
	case MintOpShortBranch:
		if (ins) {
			/* the target IL is already embedded in the instruction */
			g_string_append_printf (str, " BB%d", ins->info.target_bb->index);
		} else {
			target = ins_offset + *(gint16*)data;
			g_string_append_printf (str, " IR_%04x", target);
		}
		break;
	case MintOpBranch:
		if (ins) {
			g_string_append_printf (str, " BB%d", ins->info.target_bb->index);
		} else {
			target = ins_offset + (gint32)READ32 (data);
			g_string_append_printf (str, " IR_%04x", target);
		}
		break;
	case MintOpSwitch: {
		int sval = (gint32)READ32 (data);
		int i;
		g_string_append_printf (str, "(");
		gint32 p = 2;
		for (i = 0; i < sval; ++i) {
			if (i > 0)
				g_string_append_printf (str, ", ");
			if (ins) {
				g_string_append_printf (str, "BB%d", ins->info.target_bb_table [i]->index);
			} else {
				g_string_append_printf (str, "IR_%04x", (gint32)READ32 (data + p));
			}
			p += 2;
		}
		g_string_append_printf (str, ")");
		break;
	}
	case MintOpShortAndShortBranch:
		if (ins) {
			/* the target IL is already embedded in the instruction */
			g_string_append_printf (str, " %u, BB%d", *(guint16*)data, ins->info.target_bb->index);
		} else {
			target = ins_offset + *(gint16*)(data + 1);
			g_string_append_printf (str, " %u, IR_%04x", *(guint16*)data, target);
		}
		break;
	case MintOpPair2:
		g_string_append_printf (str, " %u <- %u, %u <- %u", data [0], data [1], data [2], data [3]);
		break;
	case MintOpPair3:
		g_string_append_printf (str, " %u <- %u, %u <- %u, %u <- %u", data [0], data [1], data [2], data [3], data [4], data [5]);
		break;
	case MintOpPair4:
		g_string_append_printf (str, " %u <- %u, %u <- %u, %u <- %u, %u <- %u", data [0], data [1], data [2], data [3], data [4], data [5], data [6], data [7]);
		break;
	default:
		g_string_append_printf (str, "unknown arg type\n");
	}

	return g_string_free (str, FALSE);
}

static void
interp_dump_compacted_ins (const guint16 *ip, const guint16 *start, gpointer *data_items)
{
	int opcode = *ip;
	int ins_offset = GPTRDIFF_TO_INT (ip - start);
	GString *str = g_string_new ("");

	g_string_append_printf (str, "IR_%04x: %-14s", ins_offset, mono_interp_opname (opcode));
	ip++;

        if (mono_interp_op_dregs [opcode] > 0)
                g_string_append_printf (str, " [%d <-", *ip++);
        else
                g_string_append_printf (str, " [nil <-");

        if (mono_interp_op_sregs [opcode] > 0) {
                for (int i = 0; i < mono_interp_op_sregs [opcode]; i++)
                        g_string_append_printf (str, " %d", *ip++);
                g_string_append_printf (str, "],");
        } else {
                g_string_append_printf (str, " nil],");
        }
	char *ins_data = interp_dump_ins_data (NULL, ins_offset, ip, opcode, data_items);
	g_print ("%s%s\n", str->str, ins_data);
	g_string_free (str, TRUE);
	g_free (ins_data);
}

static void
interp_dump_code (const guint16 *start, const guint16* end, gpointer *data_items)
{
	const guint16 *p = start;
	while (p < end) {
		interp_dump_compacted_ins (p, start, data_items);
		p = mono_interp_dis_mintop_len (p);
	}
}

void
interp_dump_ins (InterpInst *ins, gpointer *data_items)
{
	int opcode = ins->opcode;
	GString *str = g_string_new ("");
	if (ins->il_offset == -1)
		g_string_append_printf (str, "IL_----: %-14s", mono_interp_opname (opcode));
	else
		g_string_append_printf (str, "IL_%04x: %-14s", ins->il_offset, mono_interp_opname (opcode));

	if (mono_interp_op_dregs [opcode] > 0)
		g_string_append_printf (str, " [%d <-", ins->dreg);
	else
		g_string_append_printf (str, " [nil <-");

	if (opcode == MINT_PHI) {
		int *args = ins->info.args;
		while (*args != -1) {
			g_string_append_printf (str, " %d", *args);
			args++;
		}
		g_string_append_printf (str, "],");
	} else if (mono_interp_op_sregs [opcode] > 0) {
		for (int i = 0; i < mono_interp_op_sregs [opcode]; i++) {
			if (ins->sregs [i] == MINT_CALL_ARGS_SREG) {
				g_string_append_printf (str, " c:");
				if (ins->info.call_info && ins->info.call_info->call_args) {
					int *call_args = ins->info.call_info->call_args;
					while (*call_args != -1) {
						g_string_append_printf (str, " %d", *call_args);
						call_args++;
					}
				}
			} else {
				g_string_append_printf (str, " %d", ins->sregs [i]);
			}
		}
		g_string_append_printf (str, "],");
	} else {
		g_string_append_printf (str, " nil],");
	}

	if (opcode == MINT_LDLOCA_S) {
		// LDLOCA has special semantics, it has data in sregs [0], but it doesn't have any sregs
		g_string_append_printf (str, " %d", ins->sregs [0]);
	} else {
		char *descr = interp_dump_ins_data (ins, ins->il_offset, &ins->data [0], ins->opcode, data_items);
		g_string_append_printf (str, "%s", descr);
		g_free (descr);
	}
	g_print ("%s\n", str->str);
	g_string_free (str, TRUE);
}

static void
interp_dump_bb (InterpBasicBlock *bb, gpointer *data_items)
{
	g_print ("BB%d:\n", bb->index);
	for (InterpInst *ins = bb->first_ins; ins != NULL; ins = ins->next) {
		// Avoid some noise
		if (ins->opcode != MINT_NOP && ins->opcode != MINT_IL_SEQ_POINT)
			interp_dump_ins (ins, data_items);
	}
}


/* For debug use */
void
mono_interp_print_code (InterpMethod *imethod)
{
	MonoJitInfo *jinfo = imethod->jinfo;
	const guint8 *start;

	if (!jinfo)
		return;

	char *name = mono_method_full_name (imethod->method, 1);
	g_print ("Method : %s\n", name);
	g_free (name);

	start = (guint8*) jinfo->code_start;
	interp_dump_code ((const guint16*)start, (const guint16*)(start + jinfo->code_size), imethod->data_items);
}

/* For debug use */
void
mono_interp_print_td_code (TransformData *td)
{
	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb)
		interp_dump_bb (bb, td->data_items);
}


static MonoMethodHeader*
interp_method_get_header (MonoMethod* method, MonoError *error)
{
	/* An explanation: mono_method_get_header_internal returns an error if
	 * called on a method with no body (e.g. an abstract method, or an
	 * icall).  We don't want that.
	 */
	if (mono_method_has_no_body (method))
		return NULL;
	else
		return mono_method_get_header_internal (method, error);
}

static gboolean
interp_ip_in_cbb (TransformData *td, int il_offset)
{
	InterpBasicBlock *bb = td->offset_to_bb [il_offset];

	return bb == NULL || bb == td->cbb;
}

static gboolean
interp_ins_is_ldc (InterpInst *ins)
{
	return ins->opcode >= MINT_LDC_I4_M1 && ins->opcode <= MINT_LDC_I8;
}

gint32
interp_get_const_from_ldc_i4 (InterpInst *ins)
{
	switch (ins->opcode) {
	case MINT_LDC_I4_M1: return -1;
	case MINT_LDC_I4_0: return 0;
	case MINT_LDC_I4_1: return 1;
	case MINT_LDC_I4_2: return 2;
	case MINT_LDC_I4_3: return 3;
	case MINT_LDC_I4_4: return 4;
	case MINT_LDC_I4_5: return 5;
	case MINT_LDC_I4_6: return 6;
	case MINT_LDC_I4_7: return 7;
	case MINT_LDC_I4_8: return 8;
	case MINT_LDC_I4_S: return (gint32)(gint8)ins->data [0];
	case MINT_LDC_I4: return READ32 (&ins->data [0]);
	default:
		g_assert_not_reached ();
	}
}

/* If ins is not null, it will replace it with the ldc */
InterpInst*
interp_get_ldc_i4_from_const (TransformData *td, InterpInst *ins, gint32 ct, int dreg)
{
	guint16 opcode;
	switch (ct) {
	case -1: opcode = MINT_LDC_I4_M1; break;
	case 0: opcode = MINT_LDC_I4_0; break;
	case 1: opcode = MINT_LDC_I4_1; break;
	case 2: opcode = MINT_LDC_I4_2; break;
	case 3: opcode = MINT_LDC_I4_3; break;
	case 4: opcode = MINT_LDC_I4_4; break;
	case 5: opcode = MINT_LDC_I4_5; break;
	case 6: opcode = MINT_LDC_I4_6; break;
	case 7: opcode = MINT_LDC_I4_7; break;
	case 8: opcode = MINT_LDC_I4_8; break;
	default:
		if (ct >= -128 && ct <= 127)
			opcode = MINT_LDC_I4_S;
		else
			opcode = MINT_LDC_I4;
		break;
	}

	int new_size = mono_interp_oplen [opcode];

	if (ins == NULL)
		ins = interp_add_ins (td, opcode);

	int ins_size = mono_interp_oplen [ins->opcode];
	if (ins_size < new_size) {
		// We can't replace the passed instruction, discard it and emit a new one
		ins = interp_insert_ins (td, ins, opcode);
		interp_clear_ins (ins->prev);
	} else {
		ins->opcode = opcode;
	}
	interp_ins_set_dreg (ins, dreg);

	if (new_size == 3)
		ins->data [0] = (gint8)ct;
	else if (new_size == 4)
		WRITE32_INS (ins, 0, &ct);

	return ins;
}

static int
interp_get_ldind_for_mt (int mt)
{
	switch (mt) {
		case MINT_TYPE_I1: return MINT_LDIND_I1;
		case MINT_TYPE_U1: return MINT_LDIND_U1;
		case MINT_TYPE_I2: return MINT_LDIND_I2;
		case MINT_TYPE_U2: return MINT_LDIND_U2;
		case MINT_TYPE_I4: return MINT_LDIND_I4;
		case MINT_TYPE_I8: return MINT_LDIND_I8;
		case MINT_TYPE_R4: return MINT_LDIND_R4;
		case MINT_TYPE_R8: return MINT_LDIND_R8;
		case MINT_TYPE_O: return MINT_LDIND_I;
		default:
			g_assert_not_reached ();
	}
	return -1;
}

static int
interp_get_stind_for_mt (int mt)
{
	switch (mt) {
		case MINT_TYPE_I1:
		case MINT_TYPE_U1:
			return MINT_STIND_I1;
		case MINT_TYPE_I2:
		case MINT_TYPE_U2:
			return MINT_STIND_I2;
		case MINT_TYPE_I4:
			return MINT_STIND_I4;
		case MINT_TYPE_I8:
			return MINT_STIND_I8;
		case MINT_TYPE_R4:
			return MINT_STIND_R4;
		case MINT_TYPE_R8:
			return MINT_STIND_R8;
		case MINT_TYPE_O:
			return MINT_STIND_REF;
		default:
			g_assert_not_reached ();
	}
	return -1;
}

static void
interp_emit_ldobj (TransformData *td, MonoClass *klass)
{
	int mt = mono_mint_type (m_class_get_byval_arg (klass));
	gint32 size = 0;
	td->sp--;

	if (mt == MINT_TYPE_VT) {
		interp_add_ins (td, MINT_LDOBJ_VT);
		size = mono_class_value_size (klass, NULL);
		g_assert (size < G_MAXUINT16);
		interp_ins_set_sreg (td->last_ins, td->sp [0].var);
		push_type_vt (td, klass, size);
	} else {
		int opcode = interp_get_ldind_for_mt (mt);
		interp_add_ins (td, opcode);
		interp_ins_set_sreg (td->last_ins, td->sp [0].var);
		push_type (td, stack_type [mt], klass);
	}
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	if (mt == MINT_TYPE_VT)
		td->last_ins->data [0] = GINT32_TO_UINT16 (size);
}

static void
interp_emit_stobj (TransformData *td, MonoClass *klass, gboolean reverse_order)
{
	int mt = mono_mint_type (m_class_get_byval_arg (klass));

	if (mt == MINT_TYPE_VT) {
		if (m_class_has_references (klass)) {
			interp_add_ins (td, MINT_STOBJ_VT);
			td->last_ins->data [0] = get_data_item_index (td, klass);
		} else {
			interp_add_ins (td, MINT_STOBJ_VT_NOREF);
			td->last_ins->data [0] = GINT32_TO_UINT16 (mono_class_value_size (klass, NULL));
		}
	} else {
		int opcode = interp_get_stind_for_mt (mt);
		interp_add_ins (td, opcode);
	}
	td->sp -= 2;
	if (reverse_order)
		interp_ins_set_sregs2 (td->last_ins, td->sp [1].var, td->sp [0].var);
	else
		interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
}

static void
interp_emit_ldelema (TransformData *td, MonoClass *array_class, MonoClass *check_class)
{
	MonoClass *element_class = m_class_get_element_class (array_class);
	int rank = m_class_get_rank (array_class);
	int size = mono_class_array_element_size (element_class);

	gboolean bounded = m_class_get_byval_arg (array_class) ? m_class_get_byval_arg (array_class)->type == MONO_TYPE_ARRAY : FALSE;

	td->sp -= rank + 1;
	// We only need type checks when writing to array of references
	if (!check_class || m_class_is_valuetype (element_class)) {
		if (rank == 1 && !bounded) {
			interp_add_ins (td, MINT_LDELEMA1);
			interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			g_assert (size < G_MAXUINT16);
			td->last_ins->data [0] = GINT_TO_UINT16 (size);
		} else {
			interp_add_ins (td, MINT_LDELEMA);
			interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
			int *call_args = (int*)mono_mempool_alloc (td->mempool, (rank + 2) * sizeof (int));
			for (int i = 0; i < rank + 1; i++) {
				call_args [i] = td->sp [i].var;
			}
			call_args [rank + 1] = -1;
			g_assert (rank < G_MAXUINT16 && size < G_MAXUINT16);
			td->last_ins->data [0] = GINT_TO_UINT16 (rank);
			td->last_ins->data [1] = GINT_TO_UINT16 (size);
			init_last_ins_call (td);
			td->last_ins->info.call_info->call_args = call_args;
			if (!td->optimized)
				td->last_ins->info.call_info->call_offset = get_tos_offset (td);
		}
	} else {
		interp_add_ins (td, MINT_LDELEMA_TC);
		interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
		int *call_args = (int*)mono_mempool_alloc (td->mempool, (rank + 2) * sizeof (int));
		for (int i = 0; i < rank + 1; i++) {
			call_args [i] = td->sp [i].var;
		}
		call_args [rank + 1] = -1;
		td->last_ins->data [0] = get_data_item_index (td, check_class);
		init_last_ins_call (td);
		td->last_ins->info.call_info->call_args = call_args;
		if (!td->optimized)
			td->last_ins->info.call_info->call_offset = get_tos_offset (td);
	}

	push_simple_type (td, STACK_TYPE_MP);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
}

static void
interp_emit_metadata_update_ldflda (TransformData *td, MonoClassField *field, MonoError *error)
{
	g_assert (m_field_is_from_update (field));
	MonoType *field_type = field->type;
	g_assert (!m_type_is_byref (field_type));
	MonoClass *field_klass = mono_class_from_mono_type_internal (field_type);
	/* get a heap-allocated version of the field type */
	field_type = m_class_get_byval_arg (field_klass);
	guint32 field_token = mono_metadata_make_token (MONO_TABLE_FIELD, mono_metadata_update_get_field_idx (field));

	interp_add_ins (td, MINT_METADATA_UPDATE_LDFLDA);
	td->sp--;
	interp_ins_set_sreg (td->last_ins, td->sp [0].var);
	push_simple_type (td, STACK_TYPE_MP);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	td->last_ins->data [0] = get_data_item_index (td, field_type);
	td->last_ins->data [1] = get_data_item_index (td, GUINT_TO_POINTER (field_token));
}

static guint16
get_type_comparison_op (TransformData *td, gboolean equality)
{
	guint16 op;
	InterpInst *src1, *src2;
	src2 = td->last_ins;
	src1 = src2 ? interp_prev_ins (src2) : NULL;

	if (src1 && src2 && src1->opcode == MINT_LDPTR && src2->opcode == MINT_LDPTR &&
			td->sp [-2].var == src1->dreg && td->sp [-1].var == src2->dreg) {
		// Resolve the comparision immediately
		if (src1->data [0] == src2->data [0])
			op = equality ? MINT_LDC_I4_1 : MINT_LDC_I4_0;
		else
			op = equality ? MINT_LDC_I4_0 : MINT_LDC_I4_1;
		interp_clear_ins (src1);
		interp_clear_ins (src2);
	} else {
		op = equality ? MINT_CEQ_P : MINT_CNE_P;
	}

	return op;
}

/* Return TRUE if call transformation is finished */
static gboolean
interp_handle_intrinsics (TransformData *td, MonoMethod *target_method, MonoClass *constrained_class, MonoMethodSignature *csignature, gboolean readonly, int *op)
{
	const char *tm = target_method->name;
	gboolean in_corlib = m_class_get_image (target_method->klass) == mono_defaults.corlib;
	const char *klass_name_space;
	if (m_class_get_nested_in (target_method->klass))
		klass_name_space = m_class_get_name_space (m_class_get_nested_in (target_method->klass));
	else
		klass_name_space = m_class_get_name_space (target_method->klass);
	const char *klass_name = m_class_get_name (target_method->klass);

#ifdef INTERP_ENABLE_SIMD
	if ((mono_interp_opt & INTERP_OPT_SIMD) && interp_emit_simd_intrinsics (td, target_method, csignature, FALSE))
		return TRUE;
#endif

	if (target_method->klass == mono_defaults.string_class) {
		if (tm [0] == 'g') {
			if (strcmp (tm, "get_Chars") == 0)
				*op = MINT_GETCHR;
			else if (strcmp (tm, "get_Length") == 0)
				*op = MINT_STRLEN;
		} else if (tm [0] == 'F') {
			if (strcmp (tm, "FastAllocateString") == 0)
				*op = MINT_NEWSTR;
		}
	} else if (mono_class_is_subclass_of_internal (target_method->klass, mono_defaults.array_class, FALSE)) {
		if (!strcmp (tm, "get_Rank")) {
			*op = MINT_ARRAY_RANK;
		} else if (!strcmp (tm, "get_Length")) {
			*op = MINT_LDLEN;
		} else if (!strcmp (tm, "GetElementSize")) {
			*op = MINT_ARRAY_ELEMENT_SIZE;
		} else if (!strcmp (tm, "Address")) {
			MonoClass *check_class = readonly ? NULL : m_class_get_element_class (target_method->klass);
			interp_emit_ldelema (td, target_method->klass, check_class);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "Get")) {
			interp_emit_ldelema (td, target_method->klass, NULL);
			interp_emit_ldobj (td, m_class_get_element_class (target_method->klass));
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "Set")) {
			MonoClass *element_class = m_class_get_element_class (target_method->klass);
			MonoType *local_type = m_class_get_byval_arg (element_class);
			MonoClass *value_class = td->sp [-1].klass;
			// If value_class is NULL it means the top of stack is a simple type (valuetype)
			// which doesn't require type checks, or that we have no type information because
			// the code is unsafe (like in some wrappers). In that case we assume the type
			// of the array and don't do any checks.

			int local = interp_create_var (td, local_type);

			store_local (td, local);
			interp_emit_ldelema (td, target_method->klass, value_class);
			load_local (td, local);
			interp_emit_stobj (td, element_class, FALSE);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "UnsafeStore")) {
			g_error ("TODO ArrayClass::UnsafeStore");
		}
	} else if (in_corlib &&
			!strcmp (klass_name_space, "System.Diagnostics") &&
			!strcmp (klass_name, "Debugger")) {
		if (!strcmp (tm, "Break") && csignature->param_count == 0) {
			if (mini_should_insert_breakpoint (td->method))
				*op = MINT_BREAK;
		}
	} else if (in_corlib &&
			!strcmp (klass_name_space, "System") &&
			!strcmp (klass_name, "SpanHelpers") &&
			!strcmp (tm, "ClearWithReferences")) {
		*op = MINT_INTRINS_CLEAR_WITH_REFERENCES;
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "Marvin")) {
		if (!strcmp (tm, "Block")) {
			InterpInst *ldloca2 = td->last_ins;
			if (ldloca2 != NULL && ldloca2->opcode == MINT_LDLOCA_S) {
				InterpInst *ldloca1 = interp_prev_ins (ldloca2);
				if (ldloca1 != NULL && ldloca1->opcode == MINT_LDLOCA_S) {
					int var1 = ldloca1->sregs [0];
					int var2 = ldloca2->sregs [0];
					if (!td->optimized) {
						interp_add_ins (td, MINT_INTRINS_MARVIN_BLOCK);
						td->last_ins->sregs [0] = var1;
						td->last_ins->sregs [1] = var2;
						td->last_ins->data [0] = GINT_TO_UINT16 (var1);
						td->last_ins->data [1] = GINT_TO_UINT16 (var2);
					} else {
						// Convert this instruction to SSA form by splitting it into 2 different
						// single dreg instructions. When we generate final code, we will couple them
						// together.
						int result1 = interp_create_var (td, m_class_get_byval_arg (mono_defaults.uint32_class));
						int result2 = interp_create_var (td, m_class_get_byval_arg (mono_defaults.uint32_class));
						interp_add_ins (td, MINT_INTRINS_MARVIN_BLOCK_SSA1);
						td->last_ins->sregs [0] = var1;
						td->last_ins->sregs [1] = var2;
						td->last_ins->dreg = result1;

						interp_add_ins (td, MINT_INTRINS_MARVIN_BLOCK_SSA2);
						td->last_ins->sregs [0] = var1;
						td->last_ins->sregs [1] = var2;
						td->last_ins->dreg = result2;

						interp_add_ins (td, MINT_MOV_4);
						td->last_ins->sregs [0] = result1;
						td->last_ins->dreg = var1;
						interp_add_ins (td, MINT_MOV_4);
						td->last_ins->sregs [0] = result2;
						td->last_ins->dreg = var2;
					}

					// Remove the ldlocas
					td->vars [var1].indirects--;
					td->vars [var2].indirects--;
					interp_clear_ins (ldloca1);
					interp_clear_ins (ldloca2);
					td->sp -= 2;
					td->ip += 5;
					return TRUE;
				}
			}
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System.Runtime.InteropServices") && !strcmp (klass_name, "MemoryMarshal")) {
		if (!strcmp (tm, "GetArrayDataReference"))
			*op = MINT_INTRINS_MEMORYMARSHAL_GETARRAYDATAREF; // valid for both SZARRAY and MDARRAY
	} else if (in_corlib && !strcmp (klass_name_space, "System.Text.Unicode") && !strcmp (klass_name, "Utf16Utility")) {
		if (!strcmp (tm, "ConvertAllAsciiCharsInUInt32ToUppercase"))
			*op = MINT_INTRINS_ASCII_CHARS_TO_UPPERCASE;
		else if (!strcmp (tm, "UInt32OrdinalIgnoreCaseAscii"))
			*op = MINT_INTRINS_ORDINAL_IGNORE_CASE_ASCII;
		else if (!strcmp (tm, "UInt64OrdinalIgnoreCaseAscii"))
			*op = MINT_INTRINS_64ORDINAL_IGNORE_CASE_ASCII;
	} else if (in_corlib && !strcmp (klass_name_space, "System.Text") && !strcmp (klass_name, "ASCIIUtility")) {
		if (!strcmp (tm, "WidenAsciiToUtf16"))
			*op = MINT_INTRINS_WIDEN_ASCII_TO_UTF16;
	} else if (in_corlib && !strcmp (klass_name_space, "System") &&
			(!strcmp (klass_name, "Math") || !strcmp (klass_name, "MathF"))) {
		gboolean is_float = strcmp (klass_name, "MathF") == 0;
		int param_type = is_float ? MONO_TYPE_R4 : MONO_TYPE_R8;
		// FIXME add also intrinsic for Round
		if (csignature->param_count == 1 && csignature->params [0]->type == param_type) {
			// unops
			if (tm [0] == 'A') {
				if (strcmp (tm, "Asin") == 0){
					*op = MINT_ASIN;
				} else if (strcmp (tm, "Asinh") == 0){
					*op = MINT_ASINH;
				} else if (strcmp (tm, "Acos") == 0){
					*op = MINT_ACOS;
				} else if (strcmp (tm, "Acosh") == 0){
					*op = MINT_ACOSH;
				} else if (strcmp (tm, "Atan") == 0){
					*op = MINT_ATAN;
				} else if (strcmp (tm, "Atanh") == 0){
					*op = MINT_ATANH;
				} else if (strcmp (tm, "Abs") == 0) {
					*op = MINT_ABS;
				}
			} else if (tm [0] == 'C') {
				if (strcmp (tm, "Ceiling") == 0) {
					*op = MINT_CEILING;
				} else if (strcmp (tm, "Cos") == 0) {
					*op = MINT_COS;
				} else if (strcmp (tm, "Cbrt") == 0){
					*op = MINT_CBRT;
				} else if (strcmp (tm, "Cosh") == 0){
					*op = MINT_COSH;
				}
			} else if (strcmp (tm, "Exp") == 0) {
				*op = MINT_EXP;
			} else if (strcmp (tm, "Floor") == 0) {
				*op = MINT_FLOOR;
			} else if (tm [0] == 'L') {
				if (strcmp (tm, "Log") == 0) {
					*op = MINT_LOG;
				} else if (strcmp (tm, "Log2") == 0) {
					*op = MINT_LOG2;
				} else if (strcmp (tm, "Log10") == 0) {
					*op = MINT_LOG10;
				}
			} else if (tm [0] == 'S') {
				if (strcmp (tm, "Sin") == 0) {
					*op = MINT_SIN;
				} else if (strcmp (tm, "Sqrt") == 0) {
					*op = MINT_SQRT;
				} else if (strcmp (tm, "Sinh") == 0){
					*op = MINT_SINH;
				}
			} else if (tm [0] == 'T') {
				if (strcmp (tm, "Tan") == 0) {
					*op = MINT_TAN;
				} else if (strcmp (tm, "Tanh") == 0){
					*op = MINT_TANH;
				}
			}
		} else if (csignature->param_count == 2 && csignature->params [0]->type == param_type && csignature->params [1]->type == param_type) {
			if (strcmp (tm, "Atan2") == 0)
				*op = MINT_ATAN2;
			else if (strcmp (tm, "Pow") == 0)
				*op = MINT_POW;
			else if (strcmp (tm, "Min") == 0)
				*op = MINT_MIN;
			else if (strcmp (tm, "Max") == 0)
				*op = MINT_MAX;
		} else if (csignature->param_count == 3 && csignature->params [0]->type == param_type && csignature->params [1]->type == param_type && csignature->params [2]->type == param_type) {
			if (strcmp (tm, "FusedMultiplyAdd") == 0)
				*op = MINT_FMA;
		} else if (csignature->param_count == 2 && csignature->params [0]->type == param_type && csignature->params [1]->type == MONO_TYPE_I4 && strcmp (tm, "ScaleB") == 0) {
			*op = MINT_SCALEB;
		}

		if (*op != -1 && is_float) {
			*op = *op + (MINT_ASINF - MINT_ASIN);
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System") && (!strcmp (klass_name, "Span`1") || !strcmp (klass_name, "ReadOnlySpan`1"))) {
		if (!strcmp (tm, "get_Item")) {
			MonoGenericClass *gclass = mono_class_get_generic_class (target_method->klass);
			MonoClass *param_class = mono_class_from_mono_type_internal (gclass->context.class_inst->type_argv [0]);

			if (!mini_is_gsharedvt_variable_klass (param_class)) {
				MonoClassField *length_field = mono_class_get_field_from_name_full (target_method->klass, "_length", NULL);
				g_assert (length_field);
				int offset_length = m_field_get_offset (length_field) - sizeof (MonoObject);
				g_assert (offset_length == TARGET_SIZEOF_VOID_P);

				MonoClassField *ptr_field = mono_class_get_field_from_name_full (target_method->klass, "_reference", NULL);
				g_assert (ptr_field);
				int offset_pointer = m_field_get_offset (ptr_field) - sizeof (MonoObject);
				g_assert (offset_pointer == 0);

				int size = mono_class_array_element_size (param_class);
				interp_add_ins (td, MINT_GETITEM_SPAN);
				td->last_ins->data [0] = GINT_TO_UINT16 (size);

				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 5;
				return TRUE;
			}
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System.Runtime.CompilerServices") && !strcmp (klass_name, "Unsafe")) {
		if (!strcmp (tm, "AddByteOffset"))
#if SIZEOF_VOID_P == 4
			*op = MINT_ADD_I4;
#else
			*op = MINT_ADD_I8;
#endif

		else if (!strcmp (tm, "As") || !strcmp (tm, "AsRef"))
			*op = MINT_MOV_P;
		else if (!strcmp (tm, "AsPointer")) {
			/* NOP */
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_MP);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "AreSame")) {
			*op = MINT_CEQ_P;
		} else if (!strcmp (tm, "ByteOffset")) {
#if SIZEOF_VOID_P == 4
			interp_add_ins (td, MINT_SUB_I4);
#else
			interp_add_ins (td, MINT_SUB_I8);
#endif
			td->sp -= 2;
			interp_ins_set_sregs2 (td->last_ins, td->sp [1].var, td->sp [0].var);
			push_simple_type (td, STACK_TYPE_I);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "Unbox")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);

			MonoType *type = ctx->method_inst->type_argv [0];
			MonoClass *klass = mono_class_from_mono_type_internal (type);

			interp_add_ins (td, MINT_UNBOX);
			td->sp--;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_simple_type (td, STACK_TYPE_MP);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->last_ins->data [0] = get_data_item_index (td, klass);

			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "Copy")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);

			MonoType *type = ctx->method_inst->type_argv [0];
			MonoClass *klass = mono_class_from_mono_type_internal (type);

			interp_emit_ldobj (td, klass);
			interp_emit_stobj (td, klass, FALSE);

			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "CopyBlockUnaligned") || !strcmp (tm, "CopyBlock")) {
			*op = MINT_CPBLK;
		} else if (!strcmp (tm, "IsAddressLessThan")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);

			MonoClass *k = mono_defaults.boolean_class;
			interp_add_ins (td, MINT_CLT_UN_P);
			td->sp -= 2;
			interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			push_type (td, stack_type [mono_mint_type (m_class_get_byval_arg (k))], k);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "IsAddressGreaterThan")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);

			interp_add_ins (td, MINT_CGT_UN_P);
			td->sp -= 2;
			interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "SizeOf")) {
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *t = ctx->method_inst->type_argv [0];
			int align;
			int esize = mono_type_size (t, &align);
			interp_add_ins (td, MINT_LDC_I4);
			WRITE32_INS (td->last_ins, 0, &esize);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "SkipInit")) {
			*op = MINT_NOP;
		} else if (!strcmp (tm, "SubtractByteOffset")) {
#if SIZEOF_VOID_P == 4
			*op = MINT_SUB_I4;
#else
			*op = MINT_SUB_I8;
#endif
		} else if (!strcmp (tm, "InitBlockUnaligned") || !strcmp (tm, "InitBlock")) {
			*op = MINT_INITBLK;
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System.Runtime.CompilerServices") && !strcmp (klass_name, "RuntimeHelpers")) {
		if (!strcmp (tm, "get_OffsetToStringData")) {
			g_assert (csignature->param_count == 0);
			int offset = MONO_STRUCT_OFFSET (MonoString, chars);
			interp_add_ins (td, MINT_LDC_I4);
			WRITE32_INS (td->last_ins, 0, &offset);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "GetHashCode") || !strcmp (tm, "InternalGetHashCode")) {
			*op = MINT_INTRINS_GET_HASHCODE;
		} else if (!strcmp (tm, "TryGetHashCode")) {
			*op = MINT_INTRINS_TRY_GET_HASHCODE;
		} else if (!strcmp (tm, "GetRawData")) {
			interp_add_ins (td, MINT_LDFLDA_UNSAFE);
			td->last_ins->data [0] = (gint16) MONO_ABI_SIZEOF (MonoObject);

			td->sp--;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_simple_type (td, STACK_TYPE_MP);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

			td->ip += 5;
			return TRUE;
		} else if (!strcmp (tm, "IsBitwiseEquatable")) {
			g_assert (csignature->param_count == 0);
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *t = mini_get_underlying_type (ctx->method_inst->type_argv [0]);

			if (MONO_TYPE_IS_PRIMITIVE (t) && t->type != MONO_TYPE_R4 && t->type != MONO_TYPE_R8)
				*op = MINT_LDC_I4_1;
			else
				*op = MINT_LDC_I4_0;
		} else if (!strcmp (tm, "ObjectHasComponentSize")) {
			*op = MINT_INTRINS_RUNTIMEHELPERS_OBJECT_HAS_COMPONENT_SIZE;
		} else if (!strcmp (tm, "IsReferenceOrContainsReferences")) {
			g_assert (csignature->param_count == 0);
			MonoGenericContext *ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *t = mini_get_underlying_type (ctx->method_inst->type_argv [0]);

			gboolean has_refs;

			MonoClass *klass = mono_class_from_mono_type_internal (t);
			mono_class_init_internal (klass);
			if (MONO_TYPE_IS_REFERENCE (t))
				has_refs = TRUE;
			else if (MONO_TYPE_IS_PRIMITIVE (t))
				has_refs = FALSE;
			else
				has_refs = m_class_has_references (klass);

			*op = has_refs ? MINT_LDC_I4_1 : MINT_LDC_I4_0;
		} else if (!strcmp (tm, "CreateSpan") && csignature->param_count == 1 &&
				td->last_ins->opcode == MINT_LDPTR && td->last_ins->dreg == td->sp [-1].var) {
			MonoGenericContext* ctx = mono_method_get_context (target_method);
			g_assert (ctx);
			g_assert (ctx->method_inst);
			g_assert (ctx->method_inst->type_argc == 1);
			MonoType *span_arg_type = mini_get_underlying_type (ctx->method_inst->type_argv [0]);

			MonoClassField *field = (MonoClassField*)td->data_items [td->last_ins->data [0]];
			int alignment = 0;
			int element_size = mono_type_size (span_arg_type, &alignment);
			int num_elements = mono_type_size (field->type, &alignment) / element_size;
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
			const int swizzle = 1;
#else
			const int swizzle = element_size;
#endif
			gpointer data_ptr = (gpointer)mono_field_get_rva (field, swizzle);
			// instead of the field, we push directly the associated data
			td->last_ins->data [0] = get_data_item_index (td, data_ptr);

			// push the length of this span
			push_simple_type (td, STACK_TYPE_I4);
			interp_get_ldc_i4_from_const (td, NULL, num_elements, td->sp [-1].var);

			// create span
			interp_add_ins (td, MINT_INTRINS_SPAN_CTOR);
			td->sp -= 2;
			interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);

			MonoClass *ret_class = mono_class_from_mono_type_internal (csignature->ret);
			push_type_vt (td, ret_class, mono_class_value_size (ret_class, NULL));
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

			td->ip += 5;
			return TRUE;
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System") && !strcmp (klass_name, "RuntimeMethodHandle") && !strcmp (tm, "GetFunctionPointer") && csignature->param_count == 1) {
		// We must intrinsify this method on interp so we don't return a pointer to native code entering interpreter
		*op = MINT_LDFTN_DYNAMIC;
	} else if (in_corlib && target_method->klass == mono_defaults.systemtype_class && !strcmp (target_method->name, "op_Equality") &&
			td->sp [-1].klass == mono_defaults.runtimetype_class && td->sp [-2].klass == mono_defaults.runtimetype_class) {
		// We do a reference comparison only if we know both operands are runtime type
		// (they originate from object.GetType or ldftn + GetTypeFromHandle)
		*op = get_type_comparison_op (td, TRUE);
	} else if (in_corlib && target_method->klass == mono_defaults.systemtype_class && !strcmp (target_method->name, "op_Inequality") &&
			td->sp [-1].klass == mono_defaults.runtimetype_class && td->sp [-2].klass == mono_defaults.runtimetype_class) {
		*op = get_type_comparison_op (td, FALSE);
	} else if (in_corlib && target_method->klass == mono_defaults.object_class) {
		if (!strcmp (tm, "GetType")) {
			if (constrained_class && m_class_is_valuetype (constrained_class) && !mono_class_is_nullable (constrained_class)) {
				// If constrained_class is valuetype we already know its type.
				// Resolve GetType to a constant so we can fold type comparisons
				ERROR_DECL(error);
				gpointer systype = mono_type_get_object_checked (m_class_get_byval_arg (constrained_class), error);
				return_val_if_nok (error, FALSE);

				td->sp--;
				interp_add_ins (td, MINT_LDPTR);
				push_type (td, STACK_TYPE_O, mono_defaults.runtimetype_class);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, systype);

				td->ip += 5;
				return TRUE;
			} else {
				if (constrained_class) {
					if (mono_class_is_nullable (constrained_class)) {
						// We can't determine the behavior here statically because we don't know if the
						// nullable vt has a value or not. If it has a value, the result type is
						// m_class_get_cast_class (constrained_class), otherwise GetType should throw NRE.
						interp_add_ins (td, MINT_BOX_NULLABLE_PTR);
						td->last_ins->data [0] = get_data_item_index (td, constrained_class);
					} else {
						// deref the managed pointer to get the object
						interp_add_ins (td, MINT_LDIND_I);
					}
					td->sp--;
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					push_simple_type (td, STACK_TYPE_O);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				}
				interp_add_ins (td, MINT_INTRINS_GET_TYPE);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_type (td, STACK_TYPE_O, mono_defaults.runtimetype_class);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

				mono_class_init_internal (target_method->klass);

				td->ip += 5;
				return TRUE;
			}
		}
	} else if (in_corlib && target_method->klass == mono_defaults.enum_class && !strcmp (tm, "HasFlag")) {
		gboolean intrinsify = FALSE;
		MonoClass *base_klass = NULL;
		InterpInst *prev_ins = interp_prev_ins (td->last_ins);
		InterpInst *prev_prev_ins = prev_ins ? interp_prev_ins (prev_ins) : NULL;
		if (td->last_ins && td->last_ins->opcode == MINT_BOX &&
				prev_ins && interp_ins_is_ldc (prev_ins) &&
				prev_prev_ins && prev_prev_ins->opcode == MINT_BOX &&
				td->sp [-2].klass == td->sp [-1].klass &&
				interp_ip_in_cbb (td, GPTRDIFF_TO_INT (td->ip - td->il_code))) {
			// csc pattern : box, ldc, box, call HasFlag
			g_assert (m_class_is_enumtype (td->sp [-2].klass));
			MonoType *base_type = mono_type_get_underlying_type (m_class_get_byval_arg (td->sp [-2].klass));
			base_klass = mono_class_from_mono_type_internal (base_type);

			// Remove the boxing of valuetypes, by replacing them with moves
			prev_prev_ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mono_mint_type (base_type), FALSE));
			td->last_ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mono_mint_type (base_type), FALSE));

			intrinsify = TRUE;
		} else if (td->last_ins && td->last_ins->opcode == MINT_BOX &&
				prev_ins && interp_ins_is_ldc (prev_ins) && prev_prev_ins &&
				constrained_class && td->sp [-1].klass == constrained_class &&
				interp_ip_in_cbb (td, GPTRDIFF_TO_INT (td->ip - td->il_code))) {
			// mcs pattern : ldc, box, constrained Enum, call HasFlag
			g_assert (m_class_is_enumtype (constrained_class));
			MonoType *base_type = mono_type_get_underlying_type (m_class_get_byval_arg (constrained_class));
			base_klass = mono_class_from_mono_type_internal (base_type);
			int mt = mono_mint_type (m_class_get_byval_arg (base_klass));

			// Remove boxing and load the value of this
			td->last_ins->opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mt, FALSE));
			InterpInst *ins = interp_insert_ins (td, prev_prev_ins, interp_get_ldind_for_mt (mt));
			interp_ins_set_sreg (ins, td->sp [-2].var);
			interp_ins_set_dreg (ins, td->sp [-2].var);
			intrinsify = TRUE;
		}
		if (intrinsify) {
			interp_add_ins (td, MINT_INTRINS_ENUM_HASFLAG);
			td->last_ins->data [0] = get_data_item_index (td, base_klass);
			td->sp -= 2;
			interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			return TRUE;
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System.Threading") && !strcmp (klass_name, "Interlocked")) {
		if (!strcmp (tm, "MemoryBarrier") && csignature->param_count == 0)
			*op = MINT_MONO_MEMORY_BARRIER;
		else if (!strcmp (tm, "Exchange") && csignature->param_count == 2 && csignature->params [0]->type == MONO_TYPE_I8 && csignature->params [1]->type == MONO_TYPE_I8)
			*op = MINT_MONO_EXCHANGE_I8;
		else if (!strcmp (tm, "CompareExchange") && csignature->param_count == 3 &&
			 (csignature->params[1]->type == MONO_TYPE_I4 ||
			  csignature->params[1]->type == MONO_TYPE_I8)) {
			if (csignature->params[1]->type == MONO_TYPE_I4)
				*op = MINT_MONO_CMPXCHG_I4;
			else
				*op = MINT_MONO_CMPXCHG_I8;
		}
	} else if (in_corlib && !strcmp (klass_name_space, "System.Threading") && !strcmp (klass_name, "Thread")) {
		if (!strcmp (tm, "MemoryBarrier") && csignature->param_count == 0)
			*op = MINT_MONO_MEMORY_BARRIER;
	} else if (in_corlib &&
			!strcmp (klass_name_space, "System.Runtime.CompilerServices") &&
			!strcmp (klass_name, "JitHelpers") &&
			(!strcmp (tm, "EnumEquals") || !strcmp (tm, "EnumCompareTo"))) {
		MonoGenericContext *ctx = mono_method_get_context (target_method);
		g_assert (ctx);
		g_assert (ctx->method_inst);
		g_assert (ctx->method_inst->type_argc == 1);
		g_assert (csignature->param_count == 2);

		MonoType *t = ctx->method_inst->type_argv [0];
		t = mini_get_underlying_type (t);

		if (t->type == MONO_TYPE_R4 || t->type == MONO_TYPE_R8)
			return FALSE;

		gboolean is_i8 = (t->type == MONO_TYPE_I8 || t->type == MONO_TYPE_U8 || (TARGET_SIZEOF_VOID_P == 8 && (t->type == MONO_TYPE_I || t->type == MONO_TYPE_U)));
		gboolean is_unsigned = (t->type == MONO_TYPE_U1 || t->type == MONO_TYPE_U2 || t->type == MONO_TYPE_U4 || t->type == MONO_TYPE_U8 || t->type == MONO_TYPE_U);

		gboolean is_compareto = strcmp (tm, "EnumCompareTo") == 0;
		if (is_compareto) {
			int locala, localb;
			locala = interp_create_var (td, t);
			localb = interp_create_var (td, t);

			// Save arguments
			store_local (td, localb);
			store_local (td, locala);
			load_local (td, locala);
			load_local (td, localb);

			if (t->type >= MONO_TYPE_BOOLEAN && t->type <= MONO_TYPE_U2)
			{
				interp_add_ins (td, MINT_SUB_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			}
			else
			{
				// (a > b)
				if (is_unsigned)
					interp_add_ins (td, is_i8 ? MINT_CGT_UN_I8 : MINT_CGT_UN_I4);
				else
					interp_add_ins (td, is_i8 ? MINT_CGT_I8 : MINT_CGT_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				// (a < b)
				load_local (td, locala);
				load_local (td, localb);
				if (is_unsigned)
					interp_add_ins (td, is_i8 ? MINT_CLT_UN_I8 : MINT_CLT_UN_I4);
				else
					interp_add_ins (td, is_i8 ? MINT_CLT_I8 : MINT_CLT_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				// (a > b) - (a < b)
				interp_add_ins (td, MINT_SUB_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			}
			td->ip += 5;
			return TRUE;
		} else {
			if (is_i8) {
				*op = MINT_CEQ_I8;
			} else {
				*op = MINT_CEQ_I4;
			}
		}
	}
	else if (in_corlib &&
			   !strcmp ("System.Runtime.CompilerServices", klass_name_space) &&
			   !strcmp ("RuntimeFeature", klass_name)) {
		// NOTE: on the interpreter, use the C# code in System.Private.CoreLib for IsDynamicCodeSupported
		// and always return false for IsDynamicCodeCompiled
		if (!strcmp (tm, "get_IsDynamicCodeCompiled"))
			*op = MINT_LDC_I4_0;
#if defined(TARGET_WASM)
	} else if (in_corlib &&
			!strncmp ("System.Runtime.Intrinsics.Wasm", klass_name_space, 30) &&
			!strcmp (klass_name, "WasmBase")) {
		if (!strcmp (tm, "get_IsSupported")) {
			*op = MINT_LDC_I4_1;
		} else if (!strcmp (tm, "LeadingZeroCount")) {
			if (csignature->params [0]->type == MONO_TYPE_U4 || csignature->params [0]->type == MONO_TYPE_I4)
				*op = MINT_CLZ_I4;
			else if (csignature->params [0]->type == MONO_TYPE_U8 || csignature->params [0]->type == MONO_TYPE_I8)
				*op = MINT_CLZ_I8;
		} else if (!strcmp (tm, "TrailingZeroCount")) {
			if (csignature->params [0]->type == MONO_TYPE_U4 || csignature->params [0]->type == MONO_TYPE_I4)
				*op = MINT_CTZ_I4;
			else if (csignature->params [0]->type == MONO_TYPE_U8 || csignature->params [0]->type == MONO_TYPE_I8)
				*op = MINT_CTZ_I8;
		}
#endif
	} else if (in_corlib &&
			(!strncmp ("System.Runtime.Intrinsics.Arm", klass_name_space, 29) ||
			!strncmp ("System.Runtime.Intrinsics.PackedSimd", klass_name_space, 36) ||
			!strncmp ("System.Runtime.Intrinsics.X86", klass_name_space, 29) ||
			!strncmp ("System.Runtime.Intrinsics.Wasm", klass_name_space, 30)) &&
			!strcmp (tm, "get_IsSupported")) {
		*op = MINT_LDC_I4_0;
	} else if (in_corlib &&
		(!strncmp ("System.Runtime.Intrinsics.Arm", klass_name_space, 29) ||
		!strncmp ("System.Runtime.Intrinsics.X86", klass_name_space, 29))) {
		interp_generate_void_throw (td, MONO_JIT_ICALL_mono_throw_platform_not_supported);
	} else if (in_corlib && !strncmp ("System.Numerics", klass_name_space, 15)) {
		if (!strcmp ("Vector", klass_name) &&
				!strcmp (tm, "get_IsHardwareAccelerated")) {
			*op = MINT_LDC_I4_0;
		} else if (!strcmp ("BitOperations", klass_name)) {
			int arg_type = (csignature->param_count > 0) ? csignature->params [0]->type : MONO_TYPE_VOID;
			if ((!strcmp (tm, "RotateLeft") || !strcmp (tm, "RotateRight")) && MINT_IS_LDC_I4 (td->last_ins->opcode)) {
				gboolean left = !strcmp (tm, "RotateLeft");
				int ct = interp_get_const_from_ldc_i4 (td->last_ins);
				int opcode = -1;
				gboolean is_i4 = (arg_type == MONO_TYPE_U4) || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 4);
				gboolean is_i8 = (arg_type == MONO_TYPE_U8) || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 8);
				if (!is_i4 && !is_i8)
					return FALSE;
				if (is_i4)
					opcode = left ? MINT_ROL_I4_IMM : MINT_ROR_I4_IMM;
				else
					opcode = left ? MINT_ROL_I8_IMM : MINT_ROR_I8_IMM;

				interp_add_ins (td, opcode);
				td->last_ins->data [0] = ct & (is_i4 ? 31 : 63);
				td->sp -= 2;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, is_i4 ? STACK_TYPE_I4 : STACK_TYPE_I8);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

				td->ip += 5;
				return TRUE;
			} else if (!strcmp (tm, "LeadingZeroCount")) {
				if (arg_type == MONO_TYPE_U4 || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 4))
					*op = MINT_CLZ_I4;
				else if (arg_type == MONO_TYPE_U8 || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 8))
					*op = MINT_CLZ_I8;
			} else if (!strcmp (tm, "TrailingZeroCount")) {
				if (arg_type == MONO_TYPE_U4 || arg_type == MONO_TYPE_I4 ||
						((arg_type == MONO_TYPE_U || arg_type == MONO_TYPE_I) && SIZEOF_VOID_P == 4))
					*op = MINT_CTZ_I4;
				else if (arg_type == MONO_TYPE_U8 || arg_type == MONO_TYPE_I8 ||
						((arg_type == MONO_TYPE_U || arg_type == MONO_TYPE_I) && SIZEOF_VOID_P == 8))
					*op = MINT_CTZ_I8;
			} else if (!strcmp (tm, "PopCount")) {
				if (arg_type == MONO_TYPE_U4 || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 4))
					*op = MINT_POPCNT_I4;
				else if (arg_type == MONO_TYPE_U8 || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 8))
					*op = MINT_POPCNT_I8;
			} else if (!strcmp (tm, "Log2")) {
				if (arg_type == MONO_TYPE_U4 || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 4))
					*op = MINT_LOG2_I4;
				else if (arg_type == MONO_TYPE_U8 || (arg_type == MONO_TYPE_U && SIZEOF_VOID_P == 8))
					*op = MINT_LOG2_I8;
			}
		}
	} else if (in_corlib &&
			   (!strncmp ("System.Runtime.Intrinsics", klass_name_space, 25) &&
				!strncmp ("Vector", klass_name, 6) &&
				!strcmp (tm, "get_IsHardwareAccelerated"))) {
		*op = MINT_LDC_I4_0;
	}

	return FALSE;
}

static MonoMethod*
interp_transform_internal_calls (MonoMethod *method, MonoMethod *target_method, MonoMethodSignature *csignature, gboolean is_virtual)
{
	if (((method->wrapper_type == MONO_WRAPPER_NONE) || (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)) && target_method != NULL) {
		if (target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
			target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
		if (!is_virtual && target_method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
			target_method = mono_marshal_get_synchronized_wrapper (target_method);

		if (target_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL && !is_virtual && m_class_get_rank (target_method->klass) == 0)
			target_method = mono_marshal_get_native_wrapper (target_method, FALSE, FALSE);
	}
	return target_method;
}

static gboolean
interp_type_as_ptr (MonoType *tp);

/* Return whenever TYPE represents a vtype with only one scalar member */
static gboolean
is_scalar_vtype (MonoType *type)
{
	MonoClass *klass;
	MonoClassField *field;
	gpointer iter;

	if (!MONO_TYPE_ISSTRUCT (type))
		return FALSE;
	klass = mono_class_from_mono_type_internal (type);
	mono_class_init_internal (klass);

	int size = mono_class_value_size (klass, NULL);
	if (size == 0 || size > SIZEOF_VOID_P)
		return FALSE;

	iter = NULL;
	int nfields = 0;
	field = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		nfields ++;
		if (nfields > 1)
			return FALSE;
		MonoType *t = mini_get_underlying_type (field->type);
		if (!interp_type_as_ptr (t))
			return FALSE;
	}

	return TRUE;
}

static gboolean
interp_type_as_ptr (MonoType *tp)
{
	if (MONO_TYPE_IS_POINTER (tp))
		return TRUE;
	if (MONO_TYPE_IS_REFERENCE (tp))
		return TRUE;
	if ((tp)->type == MONO_TYPE_I4)
		return TRUE;
#if SIZEOF_VOID_P == 8
	if ((tp)->type == MONO_TYPE_I8 || (tp)->type == MONO_TYPE_U8)
		return TRUE;
#endif
	if ((tp)->type == MONO_TYPE_BOOLEAN)
		return TRUE;
	if ((tp)->type == MONO_TYPE_CHAR)
		return TRUE;
	if ((tp)->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (tp->data.klass))
		return TRUE;
	if (is_scalar_vtype (tp))
		return TRUE;
	return FALSE;
}

#define INTERP_TYPE_AS_PTR(tp) interp_type_as_ptr (tp)

static MintICallSig
interp_get_icall_sig (MonoMethodSignature *sig)
{
	MintICallSig op = MINT_ICALLSIG_MAX;
	switch (sig->param_count) {
	case 0:
		if (MONO_TYPE_IS_VOID (sig->ret))
			op = MINT_ICALLSIG_V_V;
		else if (INTERP_TYPE_AS_PTR (sig->ret))
			op = MINT_ICALLSIG_V_P;
		break;
	case 1:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]))
				op = MINT_ICALLSIG_P_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]))
				op = MINT_ICALLSIG_P_P;
		}
		break;
	case 2:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]))
				op = MINT_ICALLSIG_PP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]))
				op = MINT_ICALLSIG_PP_P;
		}
		break;
	case 3:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]))
				op = MINT_ICALLSIG_PPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]))
				op = MINT_ICALLSIG_PPP_P;
		}
		break;
	case 4:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]))
				op = MINT_ICALLSIG_PPPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]))
				op = MINT_ICALLSIG_PPPP_P;
		}
		break;
	case 5:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]))
				op = MINT_ICALLSIG_PPPPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]))
				op = MINT_ICALLSIG_PPPPP_P;
		}
		break;
	case 6:
		if (MONO_TYPE_IS_VOID (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]) &&
					INTERP_TYPE_AS_PTR (sig->params [5]))
				op = MINT_ICALLSIG_PPPPPP_V;
		} else if (INTERP_TYPE_AS_PTR (sig->ret)) {
			if (INTERP_TYPE_AS_PTR (sig->params [0]) &&
					INTERP_TYPE_AS_PTR (sig->params [1]) &&
					INTERP_TYPE_AS_PTR (sig->params [2]) &&
					INTERP_TYPE_AS_PTR (sig->params [3]) &&
					INTERP_TYPE_AS_PTR (sig->params [4]) &&
					INTERP_TYPE_AS_PTR (sig->params [5]))
				op = MINT_ICALLSIG_PPPPPP_P;
		}
		break;
	}
	return op;
}

/* larger than mono jit; chosen to ensure that List<T>.get_Item can be inlined */
#define INLINE_LENGTH_LIMIT 30
#define INLINE_DEPTH_LIMIT 10

static gboolean
is_metadata_update_disabled (void)
{
	static gboolean disabled = FALSE;
	if (disabled)
		return disabled;
	disabled = !mono_metadata_update_enabled (NULL);
	return disabled;
}

static gboolean
interp_method_check_inlining (TransformData *td, MonoMethod *method, MonoMethodSignature *csignature)
{
	MonoMethodHeaderSummary header;

	if (td->disable_inlining)
		return FALSE;

	if (td->cbb->no_inlining)
		return FALSE;

	// Exception handlers are always uncommon, with the exception of finally.
	int inner_clause = td->clause_indexes [td->current_il_offset];
	if (inner_clause != -1 && td->header->clauses [inner_clause].flags != MONO_EXCEPTION_CLAUSE_FINALLY)
		return FALSE;

	if (method->flags & METHOD_ATTRIBUTE_REQSECOBJ)
		/* Used to mark methods containing StackCrawlMark locals */
		return FALSE;

	if (csignature->call_convention == MONO_CALL_VARARG)
		return FALSE;

	if (!mono_method_get_header_summary (method, &header))
		return FALSE;

	/*runtime, icall and pinvoke are checked by summary call*/
	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) ||
	    header.has_clauses)
		return FALSE;

	if (td->inline_depth > INLINE_DEPTH_LIMIT)
		return FALSE;

	if (header.code_size >= INLINE_LENGTH_LIMIT) {
		gboolean aggressive_inlining = (method->iflags & METHOD_IMPL_ATTRIBUTE_AGGRESSIVE_INLINING);
		if (!aggressive_inlining)
			aggressive_inlining = has_intrinsic_attribute(method);
		if (!aggressive_inlining)
			return FALSE;
	}

	if (mono_class_needs_cctor_run (method->klass, NULL)) {
		MonoVTable *vtable;
		ERROR_DECL (error);
		if (!m_class_get_runtime_vtable (method->klass))
			/* No vtable created yet */
			return FALSE;
		vtable = mono_class_vtable_checked (method->klass, error);
		if (!is_ok (error)) {
			mono_interp_error_cleanup (error);
			return FALSE;
		}
		if (!vtable->initialized)
			return FALSE;
	}

	/* We currently access at runtime the wrapper data */
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

	if (td->prof_coverage)
		return FALSE;

	if (!is_metadata_update_disabled () && mono_metadata_update_no_inline (td->method, method))
		return FALSE;

	if (g_list_find (td->dont_inline, method))
		return FALSE;

	return TRUE;
}

static gboolean
interp_inline_method (TransformData *td, MonoMethod *target_method, MonoMethodHeader *header, MonoError *error)
{
	const unsigned char *prev_ip, *prev_il_code, *prev_in_start;
	int *prev_in_offsets;
	gboolean ret;
	unsigned int prev_max_stack_height, prev_locals_size;
	int prev_n_data_items;
	int i;
	int prev_sp_offset;
	int prev_aggressive_inlining;
	int prev_has_inlined_one_call;
	MonoGenericContext *generic_context = NULL;
	StackInfo *prev_param_area;
	InterpBasicBlock **prev_offset_to_bb;
	InterpBasicBlock *prev_cbb, *prev_entry_bb;
	MonoMethod *prev_inlined_method;
	GSList *prev_imethod_items;
	MonoMethodSignature *csignature = mono_method_signature_internal (target_method);
	int nargs = csignature->param_count + !!csignature->hasthis;
	InterpInst *prev_last_ins;

	if (header->code_size == 0)
		/* IL stripped */
		return FALSE;

	if (csignature->is_inflated)
		generic_context = mono_method_get_context (target_method);
	else {
		MonoGenericContainer *generic_container = mono_method_get_generic_container (target_method);
		if (generic_container)
			generic_context = &generic_container->context;
	}

	prev_ip = td->ip;
	prev_il_code = td->il_code;
	prev_in_start = td->in_start;
	prev_sp_offset = GPTRDIFF_TO_INT (td->sp - td->stack);
	prev_inlined_method = td->inlined_method;
	prev_last_ins = td->last_ins;
	prev_offset_to_bb = td->offset_to_bb;
	prev_cbb = td->cbb;
	prev_entry_bb = td->entry_bb;
	prev_aggressive_inlining = td->aggressive_inlining;
	prev_imethod_items = td->imethod_items;
	prev_has_inlined_one_call = td->has_inlined_one_call;
	td->has_inlined_one_call = FALSE;
	td->inlined_method = target_method;

	prev_max_stack_height = td->max_stack_height;
	prev_locals_size = td->vars_size;

	prev_n_data_items = td->n_data_items;
	prev_in_offsets = td->in_offsets;
	td->in_offsets = (int*)g_malloc0((header->code_size + 1) * sizeof(int));

	/* Inlining pops the arguments, restore the stack */
	prev_param_area = (StackInfo*)g_malloc (nargs * sizeof (StackInfo));
	memcpy (prev_param_area, &td->sp [-nargs], nargs * sizeof (StackInfo));

	int const prev_code_size = td->code_size;
	td->code_size = header->code_size;
	td->aggressive_inlining = !!(target_method->iflags & METHOD_IMPL_ATTRIBUTE_AGGRESSIVE_INLINING);
	if (!td->aggressive_inlining) {
		if (has_intrinsic_attribute(target_method))
			td->aggressive_inlining = TRUE;
	}
	if (td->verbose_level)
		g_print ("Inline start method %s.%s\n", m_class_get_name (target_method->klass), target_method->name);

	td->inline_depth++;
	ret = generate_code (td, target_method, header, generic_context, error);
	td->inline_depth--;

	if (!ret) {
		if (!is_ok (error))
			mono_interp_error_cleanup (error);

		if (td->verbose_level)
			g_print ("Inline aborted method %s.%s\n", m_class_get_name (target_method->klass), target_method->name);
		td->max_stack_height = prev_max_stack_height;
		td->vars_size = prev_locals_size;

		/* Remove any newly added items */
		for (i = prev_n_data_items; i < td->n_data_items; i++) {
			g_hash_table_remove (td->data_hash, td->data_items [i]);
		}
		td->n_data_items = prev_n_data_items;
		/* Also remove any added indexes from the imethod list */
		while (td->imethod_items != prev_imethod_items) {
			GSList *to_free = td->imethod_items;
			td->imethod_items = td->imethod_items->next;
			g_slist_free_1 (to_free);
		}

		td->sp = td->stack + prev_sp_offset;
		memcpy (&td->sp [-nargs], prev_param_area, nargs * sizeof (StackInfo));
		td->last_ins = prev_last_ins;
		td->cbb = prev_cbb;
		if (td->last_ins)
			td->last_ins->next = NULL;
		UnlockedIncrement (&mono_interp_stats.inline_failures);
	} else {
		MONO_PROFILER_RAISE (inline_method, (td->rtm->method, target_method));
		if (td->verbose_level)
			g_print ("Inline end method %s.%s\n", m_class_get_name (target_method->klass), target_method->name);
		UnlockedIncrement (&mono_interp_stats.inlined_methods);

		interp_link_bblocks (td, prev_cbb, td->entry_bb);
		prev_cbb->next_bb = td->entry_bb;

		// Make sure all bblocks that were added will now be offset from the original method that
		// is being transformed.
		InterpBasicBlock *tmp_bb = td->entry_bb;
		while (tmp_bb != NULL) {
			tmp_bb->il_offset = GPTRDIFF_TO_INT (prev_ip - prev_il_code);
			tmp_bb = tmp_bb->next_bb;
		}
	}

	td->ip = prev_ip;
	td->in_start = prev_in_start;
	td->il_code = prev_il_code;
	td->inlined_method = prev_inlined_method;
	td->offset_to_bb = prev_offset_to_bb;
	td->code_size = prev_code_size;
	td->entry_bb = prev_entry_bb;
	td->aggressive_inlining = prev_aggressive_inlining;
	td->has_inlined_one_call = prev_has_inlined_one_call;

	g_free (td->in_offsets);
	td->in_offsets = prev_in_offsets;

	g_free (prev_param_area);
	return ret;
}

static gboolean
interp_inline_newobj (TransformData *td, MonoMethod *target_method, MonoMethodSignature *csignature, int ret_mt, StackInfo *sp_params, gboolean is_protected)
{
	ERROR_DECL(error);
	InterpInst *newobj_fast, *prev_last_ins;
	int dreg, this_reg = -1;
	int prev_sp_offset;
	MonoClass *klass = target_method->klass;
	MonoMethodHeader *mheader = NULL;

	if (!(mono_interp_opt & INTERP_OPT_INLINE) ||
			!interp_method_check_inlining (td, target_method, csignature))
		return FALSE;

	if (mono_class_has_finalizer (klass) ||
			m_class_has_weak_fields (klass))
		return FALSE;

	prev_last_ins = td->cbb->last_ins;
	prev_sp_offset = GPTRDIFF_TO_INT (td->sp - td->stack);

	// Allocate var holding the newobj result. We do it here, because the var has to be alive
	// before the call, since newobj writes to it before executing the call.
	gboolean is_vt = m_class_is_valuetype (klass);
	int vtsize = 0;
	if (is_vt) {
		if (ret_mt == MINT_TYPE_VT)
			vtsize = mono_class_value_size (klass, NULL);
		else
			vtsize = MINT_STACK_SLOT_SIZE;

		dreg = interp_create_var (td, get_type_from_stack (stack_type [ret_mt], klass));

		interp_add_ins (td, MINT_INITLOCAL);
		interp_ins_set_dreg (td->last_ins, dreg);
		td->last_ins->data [0] = GINT_TO_UINT16 (vtsize);
	} else {
		dreg = interp_create_var (td, get_type_from_stack (stack_type [ret_mt], klass));
	}

	// Allocate `this` pointer
	if (is_vt) {
		push_simple_type (td, STACK_TYPE_I);
		this_reg = td->sp [-1].var;
	} else {
		push_var (td, dreg);
	}

	// Push back the params to top of stack. The original vars are maintained.
	ensure_stack (td, csignature->param_count);
	memcpy (td->sp, sp_params, sizeof (StackInfo) * csignature->param_count);
	td->sp += csignature->param_count;

	if (is_vt) {
		newobj_fast = interp_add_ins (td, MINT_LDLOCA_S);
		interp_ins_set_dreg (newobj_fast, this_reg);
		interp_ins_set_sreg (newobj_fast, dreg);
		td->vars [dreg].indirects++;
	} else {
		MonoVTable *vtable = mono_class_vtable_checked (klass, error);
		goto_if_nok (error, fail);
		newobj_fast = interp_add_ins (td, MINT_NEWOBJ_INLINED);
		interp_ins_set_dreg (newobj_fast, dreg);
		newobj_fast->data [0] = get_data_item_index (td, vtable);
	}

	if (is_protected)
		newobj_fast->flags |= INTERP_INST_FLAG_PROTECTED_NEWOBJ;

	mheader = interp_method_get_header (target_method, error);
	goto_if_nok (error, fail);

	if (!interp_inline_method (td, target_method, mheader, error))
		goto fail;

	push_var (td, dreg);
	return TRUE;
fail:
	mono_metadata_free_mh (mheader);
	// Restore the state
	td->sp = td->stack + prev_sp_offset;
	td->last_ins = prev_last_ins;
	td->cbb->last_ins = prev_last_ins;
	if (td->last_ins)
		td->last_ins->next = NULL;

	return FALSE;
}

static void
interp_constrained_box (TransformData *td, MonoClass *constrained_class, MonoMethodSignature *csignature, MonoError *error)
{
	int mt = mono_mint_type (m_class_get_byval_arg (constrained_class));
	StackInfo *sp = td->sp - 1 - csignature->param_count;
	if (mono_class_is_nullable (constrained_class)) {
		g_assert (mt == MINT_TYPE_VT);
		interp_add_ins (td, MINT_BOX_NULLABLE_PTR);
		td->last_ins->data [0] = get_data_item_index (td, constrained_class);
	} else {
		MonoVTable *vtable = mono_class_vtable_checked (constrained_class, error);
		return_if_nok (error);

		interp_add_ins (td, MINT_BOX_PTR);
		td->last_ins->data [0] = get_data_item_index (td, vtable);
	}
	interp_ins_set_sreg (td->last_ins, sp->var);
	set_simple_type_and_var (td, sp, STACK_TYPE_O);
	interp_ins_set_dreg (td->last_ins, sp->var);
}

static MonoMethod*
interp_get_method (MonoMethod *method, guint32 token, MonoImage *image, MonoGenericContext *generic_context, MonoError *error)
{
	if (method->wrapper_type == MONO_WRAPPER_NONE) {
		return mono_get_method_checked (image, token, NULL, generic_context, error);
	} else {
		MonoMethod *target_method = mono_method_get_wrapper_data (method, token);
		if (generic_context)
			target_method = mono_class_inflate_generic_method_checked (target_method, generic_context, error);
		return target_method;
	}
}

/*
 * emit_convert:
 *
 *   Emit some implicit conversions which are not part of the .net spec, but are allowed by MS.NET.
 */
static void
emit_convert (TransformData *td, StackInfo *sp, MonoType *target_type)
{
	int stype = sp->type;
	target_type = mini_get_underlying_type (target_type);

	// FIXME: Add more
	switch (target_type->type) {
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_I8: {
		switch (stype) {
		case STACK_TYPE_I4:
			interp_add_conv (td, sp, NULL, STACK_TYPE_I8, MINT_CONV_I8_I4);
			break;
		default:
			break;
		}
		break;
	}
	case MONO_TYPE_R4: {
		switch (stype) {
		case STACK_TYPE_R8:
			interp_add_conv (td, sp, NULL, STACK_TYPE_R4, MINT_CONV_R4_R8);
			break;
		default:
			break;
		}
		break;
	}
	case MONO_TYPE_R8: {
		switch (stype) {
		case STACK_TYPE_R4:
			interp_add_conv (td, sp, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
			break;
		default:
			break;
		}
		break;
	}
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U: {
		switch (stype) {
		case STACK_TYPE_I4:
			interp_add_conv (td, sp, NULL, STACK_TYPE_I8, MINT_CONV_I8_U4);
			break;
		default:
			break;
		}
	}
#endif
	default:
		break;
	}
}

static void
interp_emit_arg_conv (TransformData *td, MonoMethodSignature *csignature, int arg_start_offset)
{
	StackInfo *arg_start = td->sp - arg_start_offset - csignature->param_count;

	for (int i = 0; i < csignature->param_count; i++)
		emit_convert (td, &arg_start [i], csignature->params [i]);
}

static gint16
get_virt_method_slot (MonoMethod *method)
{
	if (mono_class_is_interface (method->klass))
		return (gint16)(-2 * MONO_IMT_SIZE + mono_method_get_imt_slot (method));
	else
		return (gint16)mono_method_get_vtable_slot (method);
}

static int*
create_call_args (TransformData *td, int num_args)
{
	// We don't need to know the sregs for calls in unoptimized code
	if (!td->optimized)
		return NULL;
	int *call_args = (int*) mono_mempool_alloc (td->mempool, (num_args + 1) * sizeof (int));
	for (int i = 0; i < num_args; i++)
		call_args [i] = td->sp [i].var;
	call_args [num_args] = -1;
	return call_args;
}

static void
interp_realign_simd_params (TransformData *td, StackInfo *sp_params, int num_args, int prev_offset)
{
	for (int i = 1; i < num_args; i++) {
		if (td->vars [sp_params [i].var].simd) {
			gint16 offset_amount;
			// If the simd struct comes immediately after the previous argument we do upper align
			// otherwise we should lower align to preserve call convention
			if ((sp_params [i - 1].offset + sp_params [i - 1].size) == sp_params [i].offset)
				offset_amount = (gint16)MINT_STACK_SLOT_SIZE;
			else
				offset_amount = -(gint16)MINT_STACK_SLOT_SIZE;

			interp_add_ins (td, MINT_MOV_STACK_UNOPT);
			// After previous alignment, this arg will be offset by MINT_STACK_SLOT_SIZE
			td->last_ins->data [0] = GINT_TO_UINT16 (sp_params [i].offset + prev_offset);
			td->last_ins->data [1] = offset_amount;
			td->last_ins->data [2] = GINT_TO_UINT16 (get_stack_size (td, sp_params + i, num_args - i));
		}
	}
}

static MonoMethod*
interp_try_devirt (MonoClass *this_klass, MonoMethod *target_method)
{
	ERROR_DECL(error);
	// No relevant information about the type
	if (!this_klass || this_klass == mono_defaults.object_class)
		return NULL;

	if (mono_class_is_interface (this_klass))
		return NULL;

	// Make sure first it is valid to lookup method in the vtable
	gboolean assignable;
	mono_class_is_assignable_from_checked (target_method->klass, this_klass, &assignable, error);
	if (!is_ok (error) || !assignable)
		return NULL;

	MonoMethod *new_target_method = mono_class_get_virtual_method (this_klass, target_method, error);
	if (!is_ok (error) || !new_target_method)
		return NULL;

	// TODO We would need to emit unboxing in order to devirtualize call to valuetype method
	if (m_class_is_valuetype (new_target_method->klass))
		return NULL;

	// final methods can still be overriden with explicit overrides
	if (m_class_is_sealed (this_klass))
		return new_target_method;

	return NULL;
}

/* Return FALSE if error, including inline failure */
static gboolean
interp_transform_call (TransformData *td, MonoMethod *method, MonoMethod *target_method, MonoGenericContext *generic_context, MonoClass *constrained_class, gboolean readonly, MonoError *error, gboolean check_visibility, gboolean save_last_error, gboolean tailcall)
{
	MonoImage *image = m_class_get_image (method->klass);
	MonoMethodSignature *csignature;
	int is_virtual = *td->ip == CEE_CALLVIRT;
	int calli = *td->ip == CEE_CALLI || *td->ip == CEE_MONO_CALLI_EXTRA_ARG;
	int op = -1;
	int native = 0;
	int need_null_check = is_virtual;
	int fp_sreg = -1, first_sreg = -1, dreg = -1;
	gboolean is_delegate_invoke = FALSE;

	guint32 token = read32 (td->ip + 1);

	if (target_method == NULL) {
		if (calli) {
			CHECK_STACK_RET(td, 1, FALSE);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				csignature = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
			else {
				csignature = mono_metadata_parse_signature_checked (image, token, error);
				return_val_if_nok (error, FALSE);
			}

			if (generic_context) {
				csignature = mono_inflate_generic_signature (csignature, generic_context, error);
				return_val_if_nok (error, FALSE);
			}

			/*
			 * The compiled interp entry wrapper is passed to runtime_invoke instead of
			 * the InterpMethod pointer. FIXME
			 */
			native = csignature->pinvoke || method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE;
			if (!method->dynamic && !method->wrapper_type && csignature->pinvoke && !mono_method_signature_has_ext_callconv (csignature, MONO_EXT_CALLCONV_SUPPRESS_GC_TRANSITION)) {
				// native calli needs a wrapper
				target_method = mono_marshal_get_native_func_wrapper_indirect (method->klass, csignature, FALSE);
				calli = FALSE;
				native = FALSE;
				// The function pointer is passed last, but the wrapper expects it as first argument
				// Switch the arguments.
				// When the var offset allocator is not used, in unoptimized code, we have to manually
				// push the values into the correct order. In optimized code, we just need to know what
				// local is the execution stack position during compilation, so we can just do a memmove
				// of the StackInfo
				if (td->optimized) {
					StackInfo sp_fp = td->sp [-1];
					StackInfo *start = &td->sp [-csignature->param_count - 1];
					memmove (start + 1, start, csignature->param_count * sizeof (StackInfo));
					*start = sp_fp;
				} else {
					int *arg_locals = mono_mempool_alloc0 (td->mempool, sizeof (int) * csignature->param_count);
					int fp_local = interp_create_var (td, m_class_get_byval_arg (mono_defaults.int_class));
					// Pop everything into locals. Push after into correct order
					store_local (td, fp_local);
					for (int i = csignature->param_count - 1; i >= 0; i--) {
						arg_locals [i] = interp_create_var (td, csignature->params [i]);
						store_local (td, arg_locals [i]);
					}
					load_local (td, fp_local);
					for (int i = 0; i < csignature->param_count; i++)
						load_local (td, arg_locals [i]);
				}

				// The method we are calling has a different signature
				csignature = mono_method_signature_internal (target_method);
			}
		} else {
			target_method = interp_get_method (method, token, image, generic_context, error);
			return_val_if_nok (error, FALSE);
			csignature = mono_method_signature_internal (target_method);
		}
	} else {
		csignature = mono_method_signature_internal (target_method);
	}

	if (calli && csignature->param_count == 0 && csignature->call_convention == MONO_CALL_THISCALL) {
		mono_error_set_generic_error (error, "System", "InvalidProgramException", "thiscall with 0 arguments");
		return FALSE;
	}

	if (check_visibility && target_method && !mono_method_can_access_method (method, target_method))
		interp_generate_mae_throw (td, method, target_method);

	if (target_method && target_method->string_ctor) {
		/* Create the real signature */
		MonoMethodSignature *ctor_sig = mono_metadata_signature_dup_mempool (td->mempool, csignature);
		ctor_sig->ret = m_class_get_byval_arg (mono_defaults.string_class);

		csignature = ctor_sig;
	}

	/* Intrinsics */
	if (target_method && interp_handle_intrinsics (td, target_method, constrained_class, csignature, readonly, &op)) {
		MONO_PROFILER_RAISE (inline_method, (td->rtm->method, target_method));
		return TRUE;
	}

	if (constrained_class) {
		if (m_class_is_enumtype (constrained_class) && !strcmp (target_method->name, "GetHashCode")) {
			/* Use the corresponding method from the base type to avoid boxing */
			MonoType *base_type = mono_class_enum_basetype_internal (constrained_class);
			g_assert (base_type);
			constrained_class = mono_class_from_mono_type_internal (base_type);
			target_method = mono_class_get_method_from_name_checked (constrained_class, target_method->name, 0, 0, error);
			mono_error_assert_ok (error);
			g_assert (target_method);
		}
	}

	if (constrained_class) {
		mono_class_setup_vtable (constrained_class);
		if (mono_class_has_failure (constrained_class)) {
			mono_error_set_for_class_failure (error, constrained_class);
			return FALSE;
		}
#if DEBUG_INTERP
		g_print ("CONSTRAINED.CALLVIRT: %s::%s.  %s (%p) ->\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		MonoMethod *virt_method = target_method;
		target_method = mono_get_method_constrained_with_method (image, target_method, constrained_class, generic_context, error);
#if DEBUG_INTERP
		g_print ("                    : %s::%s.  %s (%p)\n", target_method->klass->name, target_method->name, mono_signature_full_name (target_method->signature), target_method);
#endif
		/* Intrinsics: Try again, it could be that `mono_get_method_constrained_with_method` resolves to a method that we can substitute */
		if (target_method && interp_handle_intrinsics (td, target_method, constrained_class, csignature, readonly, &op)) {
			MONO_PROFILER_RAISE (inline_method, (td->rtm->method, target_method));
			return TRUE;
		}

		return_val_if_nok (error, FALSE);
		if (mono_class_has_dim_conflicts (constrained_class) && mono_class_is_method_ambiguous (constrained_class, virt_method))
			interp_generate_void_throw (td, MONO_JIT_ICALL_mono_throw_ambiguous_implementation);
		mono_class_setup_vtable (target_method->klass);

		// Follow the rules for constrained calls from ECMA spec
		if (m_method_is_static (target_method)) {
			is_virtual = FALSE;
		} else if (!m_class_is_valuetype (constrained_class)) {
			StackInfo *sp = td->sp - 1 - csignature->param_count;
			/* managed pointer on the stack, we need to deref that puppy */
			interp_add_ins (td, MINT_LDIND_I);
			interp_ins_set_sreg (td->last_ins, sp->var);
			set_simple_type_and_var (td, sp, STACK_TYPE_I);
			interp_ins_set_dreg (td->last_ins, sp->var);
		} else if (target_method->klass != constrained_class) {
			/*
			 * The type parameter is instantiated as a valuetype,
			 * but that type doesn't override the method we're
			 * calling, so we need to box `this'.
			 */
			int this_type = (td->sp - csignature->param_count - 1)->type;
			g_assert (this_type == STACK_TYPE_I || this_type == STACK_TYPE_MP);
			interp_constrained_box (td, constrained_class, csignature, error);
			return_val_if_nok (error, FALSE);
		} else {
			is_virtual = FALSE;
		}
	}

	if (target_method)
		mono_class_init_internal (target_method->klass);

	if (!is_virtual && target_method && (target_method->flags & METHOD_ATTRIBUTE_ABSTRACT) && !m_method_is_static (target_method)) {
		if (!mono_class_is_interface (method->klass))
			interp_generate_void_throw (td, MONO_JIT_ICALL_mono_throw_bad_image);
		else
			is_virtual = TRUE;
	}

	if (is_virtual && target_method && (!(target_method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
										(MONO_METHOD_IS_FINAL (target_method)))) {
		/* Not really virtual, just needs a null check */
		is_virtual = FALSE;
		need_null_check = TRUE;
	}

	CHECK_STACK_RET (td, csignature->param_count + csignature->hasthis, FALSE);

	gboolean skip_tailcall = FALSE;
	if (tailcall && !is_virtual && target_method != NULL) {
		MonoMethodHeader *mh = interp_method_get_header (target_method, error);
		if (mh != NULL && mh->code_size == 0)
			skip_tailcall = TRUE;
		mono_metadata_free_mh (mh);
	}

	if (tailcall && !td->gen_sdb_seq_points && !calli && op == -1 &&
		(target_method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) == 0 &&
		(target_method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) == 0 &&
		!(target_method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING) &&
		!skip_tailcall) {
		(void)mono_class_vtable_checked (target_method->klass, error);
		return_val_if_nok (error, FALSE);

		if (*(td->ip + 5) == CEE_RET) {
			if (td->inlined_method)
				return FALSE;

			if (td->verbose_level)
				g_print ("Optimize tail call of %s.%s\n", m_class_get_name (target_method->klass), target_method->name);

			int num_args = csignature->param_count + !!csignature->hasthis;
			td->sp -= num_args;
			guint32 params_stack_size = get_stack_size (td, td->sp, num_args);

			if (is_virtual) {
				interp_add_ins (td, MINT_CKNULL);
				interp_ins_set_sreg (td->last_ins, td->sp->var);
				set_simple_type_and_var (td, td->sp, td->sp->type);
				interp_ins_set_dreg (td->last_ins, td->sp->var);

				interp_add_ins (td, MINT_TAILCALL_VIRT);
				td->last_ins->data [2] = get_virt_method_slot (target_method);
			} else {
				interp_add_ins (td, MINT_TAILCALL);
			}
			interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
			td->last_ins->data [0] = get_data_item_index_imethod (td, mono_interp_get_imethod (target_method));
			td->last_ins->data [1] = GUINT32_TO_UINT16 (params_stack_size);
			init_last_ins_call (td);

			if (td->optimized) {
				int *call_args = create_call_args (td, num_args);
				td->last_ins->info.call_info->call_args = call_args;
			} else {
				// Execution stack is empty once the args are pop'ed
				td->last_ins->info.call_info->call_offset = 0;
			}

			int in_offset = GPTRDIFF_TO_INT (td->ip - td->il_code);
			if (interp_ip_in_cbb (td, in_offset + 5))
				++td->ip; /* gobble the CEE_RET if it isn't branched to */
			td->ip += 5;
			return TRUE;
		}
	}

	// Attempt to devirtualize the call
	if (is_virtual) {
		MonoClass *this_klass = (td->sp - 1 - csignature->param_count)->klass;
		MonoMethod *new_target_method = interp_try_devirt (this_klass, target_method);

		if (new_target_method) {
			if (td->verbose_level)
				g_print ("DEVIRTUALIZE %s.%s to %s.%s\n", m_class_get_name (target_method->klass), target_method->name, m_class_get_name (new_target_method->klass), new_target_method->name);
			target_method = new_target_method;
			is_virtual = FALSE;
		}
	}

	if (op == -1)
		target_method = interp_transform_internal_calls (method, target_method, csignature, is_virtual);

	if (csignature->call_convention == MONO_CALL_VARARG)
		csignature = mono_method_get_signature_checked (target_method, image, token, generic_context, error);

	if (need_null_check) {
		StackInfo *sp = td->sp - 1 - csignature->param_count;
		interp_add_ins (td, MINT_CKNULL);
		interp_ins_set_sreg (td->last_ins, sp->var);
		set_type_and_var (td, sp, sp->type, sp->klass);
		interp_ins_set_dreg (td->last_ins, sp->var);
	}

	/* Offset the function pointer when emitting convert instructions */
	int arg_start_offset = calli ? 1 : 0;
	interp_emit_arg_conv (td, csignature, arg_start_offset);

	g_assert (csignature->call_convention != MONO_CALL_FASTCALL);
	if ((mono_interp_opt & INTERP_OPT_INLINE) && op == -1 && !is_virtual && target_method && interp_method_check_inlining (td, target_method, csignature)) {
		MonoMethodHeader *mheader = interp_method_get_header (target_method, error);
		return_val_if_nok (error, FALSE);

		if (interp_inline_method (td, target_method, mheader, error)) {
			td->ip += 5;
			goto done;
		}
		mono_metadata_free_mh (mheader);
	}

	/*
	 * When inlining a method, only allow it to perform one call.
	 * We previously prohibited calls entirely, this is a conservative rule that allows
	 * Things like ThrowHelper.ThrowIfNull to theoretically inline, along with a hypothetical
	 * X.get_Item that just calls Y.get_Item (profitable to inline)
	 */
	if (op == -1 && td->inlined_method && !td->aggressive_inlining) {
		if (!target_method) {
			if (td->verbose_level > 1)
				g_print("Disabling inlining because we have no target_method for call in %s\n", td->method->name);
			return FALSE;
		} else if (td->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE) {
			// This scenario causes https://github.com/dotnet/runtime/issues/83792
			return FALSE;
		} else if (has_doesnotreturn_attribute(target_method)) {
			/*
			 * Since the method does not return, it's probably a throw helper and will not be called.
			 * As such we don't want it to prevent inlining of the method that calls it.
			 */
			if (td->verbose_level > 2)
				g_print("Overlooking inlined doesnotreturn call in %s (target %s)\n", td->method->name, target_method->name);
		} else if (td->has_inlined_one_call) {
			if (td->verbose_level > 1)
				g_print("Prohibiting second inlined call in %s (target %s)\n", td->method->name, target_method->name);
			return FALSE;
		} else {
			if (td->verbose_level > 2)
				g_print("Allowing single inlined call in %s (target %s)\n", td->method->name, target_method->name);
			td->has_inlined_one_call = TRUE;
		}
	}

	/* We need to convert delegate invoke to a indirect call on the interp_invoke_impl field */
	if (target_method && m_class_get_parent (target_method->klass) == mono_defaults.multicastdelegate_class) {
		const char *name = target_method->name;
		if (*name == 'I' && (strcmp (name, "Invoke") == 0))
			is_delegate_invoke = TRUE;
	}

	/* Pop the function pointer */
	if (calli) {
		--td->sp;
		fp_sreg = td->sp [0].var;
	}

	int param_end_offset = 0;
	if (!td->optimized && op == -1)
		param_end_offset = get_tos_offset (td);

	int num_args = csignature->param_count + !!csignature->hasthis;
	td->sp -= num_args;
	guint32 params_stack_size = get_stack_size (td, td->sp, num_args);

	int call_offset = -1;

	StackInfo *sp_args = td->sp;

	if (!td->optimized && op == -1) {
		int param_offset;
		if (num_args)
			param_offset = td->sp [0].offset;
		else
			param_offset = param_end_offset = get_tos_offset (td);

		if ((param_offset % MINT_STACK_ALIGNMENT) == 0) {
			call_offset = param_offset;
		} else if (params_stack_size) {
			int new_param_offset = ALIGN_TO (param_offset, MINT_STACK_ALIGNMENT);
			call_offset = new_param_offset;

			// Mov all params to the new_param_offset
			interp_add_ins (td, MINT_MOV_STACK_UNOPT);
			td->last_ins->data [0] = GINT_TO_UINT16 (param_offset);
			td->last_ins->data [1] = MINT_STACK_SLOT_SIZE;
			td->last_ins->data [2] = GINT_TO_UINT16 (param_end_offset - param_offset);

			// If we have any simd arguments, we broke their alignment. We need to find the first simd arg and realign it
			// together with the following params. First argument can't be simd type otherwise we would have been aligned
			// already.
			interp_realign_simd_params (td, td->sp, num_args, MINT_STACK_SLOT_SIZE);

			if (calli) {
				// fp_sreg is at the top of the stack, make sure it is not overwritten by MINT_CALL_ALIGN_STACK
				int offset = new_param_offset - param_offset;
				td->vars [fp_sreg].stack_offset += offset;
			}
		} else {
			call_offset = ALIGN_TO (param_offset, MINT_STACK_ALIGNMENT);
		}
	}

	int *call_args = create_call_args (td, num_args);

#ifndef MONO_ARCH_HAVE_SWIFTCALL
	if (mono_method_signature_has_ext_callconv (csignature, MONO_EXT_CALLCONV_SWIFTCALL)) {
		mono_error_set_not_supported (error, "CallConvSwift is not supported on this platform.");
		return FALSE;
	}
#endif

	// We overwrite it with the return local, save it for future use
	if (csignature->param_count || csignature->hasthis)
		first_sreg = td->sp [0].var;

	/* need to handle typedbyref ... */
	if (csignature->ret->type != MONO_TYPE_VOID) {
		int mt = mono_mint_type(csignature->ret);
		MonoClass *klass = mono_class_from_mono_type_internal (csignature->ret);

		if (mt == MINT_TYPE_VT) {
			guint32 res_size;
			if (csignature->pinvoke && !csignature->marshalling_disabled && method->wrapper_type != MONO_WRAPPER_NONE)
				res_size = mono_class_native_size (klass, NULL);
			else
				res_size = mono_class_value_size (klass, NULL);
			push_type_vt (td, klass, res_size);
			if (mono_class_has_failure (klass)) {
				mono_error_set_for_class_failure (error, klass);
				return FALSE;
			}
		} else {
			push_type (td, stack_type[mt], klass);
		}
		dreg = td->sp [-1].var;
	} else {
		// Create a new dummy local to serve as the dreg of the call
		// FIXME Consider adding special dreg type (ex -1), that is
		// resolved to null offset. The opcode shouldn't really write to it
		push_simple_type (td, STACK_TYPE_I4);
		td->sp--;
		dreg = td->sp [0].var;
	}

	if (op >= 0) {
		interp_add_ins (td, op);

		int has_dreg = mono_interp_op_dregs [op];
		int num_sregs = mono_interp_op_sregs [op];
		if (has_dreg)
			interp_ins_set_dreg (td->last_ins, dreg);
		if (num_sregs > 0) {
			if (num_sregs == 1)
				interp_ins_set_sreg (td->last_ins, first_sreg);
			else if (num_sregs == 2)
				interp_ins_set_sregs2 (td->last_ins, first_sreg, sp_args [1].var);
			else if (num_sregs == 3)
				interp_ins_set_sregs3 (td->last_ins, first_sreg, sp_args [1].var, sp_args [2].var);
			else
				g_error ("Unsupported opcode");
		}

		if (op == MINT_LDLEN) {
			SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
		}
	} else if (!calli && !is_delegate_invoke && !is_virtual && mono_interp_jit_call_supported (target_method, csignature)) {
		interp_add_ins (td, MINT_JIT_CALL);
		interp_ins_set_dreg (td->last_ins, dreg);
		interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
		init_last_ins_call (td);
		td->last_ins->data [0] = get_data_item_index_imethod (td, mono_interp_get_imethod (target_method));
	} else {
		if (is_delegate_invoke) {
			interp_add_ins (td, MINT_CALL_DELEGATE);
			interp_ins_set_dreg (td->last_ins, dreg);
			interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
			td->last_ins->data [0] = GUINT32_TO_UINT16 (params_stack_size);
			td->last_ins->data [1] = GUINT32_TO_UINT16 (csignature->param_count);
			if (csignature->param_count) {
				// Check if the first arg (after the delegate pointer) is simd
				// In case the delegate represents static method with no target, the instruction
				// needs to be able to access the actual arguments to continue with the call so it
				// needs to know whether there is an empty stack slot between the delegate ptr and the
				// rest of the args
				gboolean first_arg_is_simd = td->vars [sp_args [1].var].simd;
				td->last_ins->data [2] = first_arg_is_simd ? MINT_SIMD_ALIGNMENT : MINT_STACK_SLOT_SIZE;
			}
		} else if (calli) {
			MintICallSig icall_sig = MINT_ICALLSIG_MAX;
#ifndef MONO_ARCH_HAS_NO_PROPER_MONOCTX
			/* Try using fast icall path for simple signatures */
			if (native && !method->dynamic)
				icall_sig = interp_get_icall_sig (csignature);
#endif
			gboolean default_cconv = TRUE;
#ifdef TARGET_X86
#ifdef TARGET_WIN32
			// Platform that we don't actively support ?
			default_cconv = csignature->call_convention == MONO_CALL_C;
#else
			default_cconv = csignature->call_convention == MONO_CALL_DEFAULT || csignature->call_convention == MONO_CALL_C;
#endif
#endif
			// When using the Swift calling convention, emit MINT_CALLI_NAT opcode to manage context registers.
			default_cconv = default_cconv && !mono_method_signature_has_ext_callconv (csignature, MONO_EXT_CALLCONV_SWIFTCALL);

			// FIXME calli receives both the args offset and sometimes another arg for the frame pointer,
			// therefore some args are in the param area, while the fp is not. We should differentiate for
			// this, probably once we will have an explicit param area where we copy arguments.
			if (icall_sig != MINT_ICALLSIG_MAX && default_cconv) {
				interp_add_ins (td, MINT_CALLI_NAT_FAST);
				interp_ins_set_dreg (td->last_ins, dreg);
				interp_ins_set_sregs2 (td->last_ins, fp_sreg, MINT_CALL_ARGS_SREG);
				td->last_ins->data [0] = GINT_TO_UINT16 (icall_sig);
				td->last_ins->data [1] = get_data_item_index (td, (void *)csignature);
				td->last_ins->data [2] = !!save_last_error;
			} else if (native && method->dynamic && csignature->pinvoke) {
				interp_add_ins (td, MINT_CALLI_NAT_DYNAMIC);
				interp_ins_set_dreg (td->last_ins, dreg);
				interp_ins_set_sregs2 (td->last_ins, fp_sreg, MINT_CALL_ARGS_SREG);
				td->last_ins->data [0] = get_data_item_index (td, (void *)csignature);
			} else if (native) {
				interp_add_ins (td, MINT_CALLI_NAT);

				InterpMethod *imethod = NULL;
				/*
				 * We can have pinvoke calls outside M2N wrappers, in xdomain calls, where we can't easily get the called imethod.
				 * Those calls will be slower since we will not cache the arg offsets on the imethod, and have to compute them
				 * every time based on the signature.
				 */
				if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
					MonoMethod *pinvoke_method = mono_marshal_method_from_wrapper (method);
					if (pinvoke_method)
						imethod = mono_interp_get_imethod (pinvoke_method);
				}

				interp_ins_set_dreg (td->last_ins, dreg);
				interp_ins_set_sregs2 (td->last_ins, fp_sreg, MINT_CALL_ARGS_SREG);
				td->last_ins->data [0] = get_data_item_index (td, csignature);
				td->last_ins->data [1] = get_data_item_index_imethod (td, imethod);
				td->last_ins->data [2] = !!save_last_error;
				/* Cache slot */
				td->last_ins->data [3] = get_data_item_index_nonshared (td, NULL);
			} else {
				interp_add_ins (td, MINT_CALLI);
				interp_ins_set_dreg (td->last_ins, dreg);
				interp_ins_set_sregs2 (td->last_ins, fp_sreg, MINT_CALL_ARGS_SREG);
			}
		} else {
			InterpMethod *imethod = mono_interp_get_imethod (target_method);

			if (csignature->call_convention == MONO_CALL_VARARG) {
				interp_add_ins (td, MINT_CALL_VARARG);
				td->last_ins->data [1] = get_data_item_index (td, (void *)csignature);
				td->last_ins->data [2] = GUINT32_TO_UINT16 (params_stack_size);
			} else if (is_virtual) {
				interp_add_ins (td, MINT_CALLVIRT_FAST);
				td->last_ins->data [1] = get_virt_method_slot (target_method);
			} else {
				interp_add_ins (td, MINT_CALL);
			}
			interp_ins_set_dreg (td->last_ins, dreg);
			interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
			td->last_ins->data [0] = get_data_item_index_imethod (td, imethod);

#ifdef ENABLE_EXPERIMENT_TIERED
			if (MINT_IS_PATCHABLE_CALL (td->last_ins->opcode)) {
				g_assert (!calli && !is_virtual);
				td->last_ins->flags |= INTERP_INST_FLAG_RECORD_CALL_PATCH;
				g_hash_table_insert (td->patchsite_hash, td->last_ins, target_method);
			}
#endif
		}
		init_last_ins_call (td);
	}
	td->ip += 5;
	if (td->last_ins->flags & INTERP_INST_FLAG_CALL) {
		td->last_ins->info.call_info->call_args = call_args;
		if (!td->optimized) {
			g_assert (call_offset != -1);
			td->last_ins->info.call_info->call_offset = call_offset;
		}
	} else if (!td->optimized) {
		g_assert (call_offset == -1);
	}


done:
	if (csignature->ret->type != MONO_TYPE_VOID && target_method) {
		MonoClass *ret_klass = mini_handle_call_res_devirt (target_method);
		if (ret_klass)
			td->sp [-1].klass = ret_klass;
	}
	return TRUE;
}

static MonoClassField *
interp_field_from_token (MonoMethod *method, guint32 token, MonoClass **klass, MonoGenericContext *generic_context, MonoError *error)
{
	MonoClassField *field = NULL;
	if (method->wrapper_type != MONO_WRAPPER_NONE) {
		field = (MonoClassField *) mono_method_get_wrapper_data (method, token);
		*klass = m_field_get_parent (field);

		mono_class_setup_fields (m_field_get_parent (field));
	} else {
		field = mono_field_from_token_checked (m_class_get_image (method->klass), token, klass, generic_context, error);
		return_val_if_nok (error, NULL);
	}

	if (!method->skip_visibility && !mono_method_can_access_field (method, field)) {
		char *method_fname = mono_method_full_name (method, TRUE);
		char *field_fname = mono_field_full_name (field);
		mono_error_set_generic_error (error, "System", "FieldAccessException", "Field `%s' is inaccessible from method `%s'\n", field_fname, method_fname);
		g_free (method_fname);
		g_free (field_fname);
		return NULL;
	}

	return field;
}

InterpBasicBlock*
interp_alloc_bb (TransformData *td)
{
	InterpBasicBlock *bb = (InterpBasicBlock*)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock));
	bb->il_offset = -1;
	bb->native_offset = -1;
	bb->stack_height = -1;
	bb->index = td->bb_count++;

	return bb;
}

static InterpBasicBlock*
get_bb (TransformData *td, unsigned char *ip, gboolean make_list)
{
	int offset = GPTRDIFF_TO_INT (ip - td->il_code);
	InterpBasicBlock *bb = td->offset_to_bb [offset];

	if (!bb) {
		bb = interp_alloc_bb (td);

		bb->il_offset = offset;
		td->offset_to_bb [offset] = bb;
                /* Add the blocks in reverse order */
                if (make_list)
                        td->basic_blocks = g_list_prepend_mempool (td->mempool, td->basic_blocks, bb);
	}

	return bb;
}

/*
 * get_basic_blocks:
 *
 *   Compute the set of IL level basic blocks.
 */
static gboolean
get_basic_blocks (TransformData *td, MonoMethodHeader *header, gboolean make_list, MonoBitSet *il_targets)
{
	guint8 *start = (guint8*)td->il_code;
	guint8 *end = (guint8*)td->il_code + td->code_size;
	guint8 *ip = start;
	unsigned char *target;
	ptrdiff_t cli_addr;
	const MonoOpcode *opcode;
	InterpBasicBlock *bb;

	td->offset_to_bb = (InterpBasicBlock**)mono_mempool_alloc0 (td->mempool, (unsigned int)(sizeof (InterpBasicBlock*) * (end - start + 1)));
	get_bb (td, start, make_list);

	for (guint i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = header->clauses + i;
		if (start + c->try_offset > end || start + c->try_offset + c->try_len > end)
			return FALSE;
		bb = get_bb (td, start + c->try_offset, make_list);
		bb->jump_targets++;
		mono_bitset_set (il_targets, c->try_offset);
		mono_bitset_set (il_targets, c->try_offset + c->try_len);
		if (start + c->handler_offset > end || start + c->handler_offset + c->handler_len > end)
			return FALSE;
		bb = get_bb (td, start + c->handler_offset, make_list);
		bb->jump_targets++;
		mono_bitset_set (il_targets, c->handler_offset);
		mono_bitset_set (il_targets, c->handler_offset + c->handler_len);
		if (c->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			if (start + c->data.filter_offset > end)
				return FALSE;
			bb = get_bb (td, start + c->data.filter_offset, make_list);
			bb->jump_targets++;
			mono_bitset_set (il_targets, c->data.filter_offset);
		}
	}
	while (ip < end) {
		cli_addr = ip - start;
		int i = mono_opcode_value ((const guint8 **)&ip, end);
		opcode = &mono_opcodes [i];
		switch (opcode->argument) {
		case MonoInlineNone:
			ip++;
			break;
		case MonoInlineString:
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineMethod:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
			target = start + cli_addr + 2 + (signed char)ip [1];
			if (target > end)
				return FALSE;
			bb = get_bb (td, target, make_list);
			bb->jump_targets++;
			ip += 2;
			get_bb (td, ip, make_list);
			mono_bitset_set (il_targets, GPTRDIFF_TO_UINT32 (target - start));
			break;
		case MonoInlineBrTarget:
			target = start + cli_addr + 5 + (gint32)read32 (ip + 1);
			if (target > end)
				return FALSE;
			bb = get_bb (td, target, make_list);
			bb->jump_targets++;
			ip += 5;
			get_bb (td, ip, make_list);
			mono_bitset_set (il_targets, GPTRDIFF_TO_UINT32 (target - start));
			break;
		case MonoInlineSwitch: {
			guint32 n = read32 (ip + 1);
			guint32 j;
			ip += 5;
			cli_addr += 5 + 4 * n;
			target = start + cli_addr;
			if (target > end)
				return FALSE;
			bb = get_bb (td, target, make_list);
			bb->jump_targets++;
			mono_bitset_set (il_targets, GPTRDIFF_TO_UINT32 (target - start));
			for (j = 0; j < n; ++j) {
				target = start + cli_addr + (gint32)read32 (ip);
				if (target > end)
					return FALSE;
				bb = get_bb (td, target, make_list);
				bb->jump_targets++;
				ip += 4;
				mono_bitset_set (il_targets, GPTRDIFF_TO_UINT32 (target - start));
			}
			get_bb (td, ip, make_list);
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}

		if (i == CEE_THROW || i == CEE_ENDFINALLY || i == CEE_RETHROW)
			get_bb (td, ip, make_list);
	}

	/* get_bb added blocks in reverse order, unreverse now */
	if (make_list)
		td->basic_blocks = g_list_reverse (td->basic_blocks);

	return TRUE;
}

static void
interp_save_debug_info (InterpMethod *rtm, MonoMethodHeader *header, TransformData *td, GArray *line_numbers)
{
	MonoDebugMethodJitInfo *dinfo;

	if (!mono_debug_enabled ())
		return;

	/*
	 * We save the debug info in the same way the JIT does it, treating the interpreter IR as the native code.
	 */

	dinfo = g_new0 (MonoDebugMethodJitInfo, 1);
	dinfo->num_params = rtm->param_count;
	dinfo->params = g_new0 (MonoDebugVarInfo, dinfo->num_params);
	dinfo->num_locals = header->num_locals;
	dinfo->locals = g_new0 (MonoDebugVarInfo, header->num_locals);
	dinfo->code_start = (guint8*)rtm->code;
	dinfo->code_size = GPTRDIFF_TO_UINT32 (td->new_code_end - td->new_code);
	dinfo->epilogue_begin = 0;
	dinfo->has_var_info = TRUE;
	dinfo->num_line_numbers = line_numbers->len;
	dinfo->line_numbers = g_new0 (MonoDebugLineNumberEntry, dinfo->num_line_numbers);

	for (guint32 i = 0; i < dinfo->num_params; i++) {
		MonoDebugVarInfo *var = &dinfo->params [i];
		var->type = rtm->param_types [i];
	}
	for (guint32 i = 0; i < dinfo->num_locals; i++) {
		MonoDebugVarInfo *var = &dinfo->locals [i];
		var->type = mono_metadata_type_dup (NULL, header->locals [i]);
	}

	for (guint32 i = 0; i < dinfo->num_line_numbers; i++)
		dinfo->line_numbers [i] = g_array_index (line_numbers, MonoDebugLineNumberEntry, i);
	mono_debug_add_method (rtm->method, dinfo, NULL);

	mono_debug_free_method_jit_info (dinfo);
}

/* Same as the code in seq-points.c */
static void
insert_pred_seq_point (SeqPoint *last_sp, SeqPoint *sp, GSList **next)
{
	GSList *l;
	int src_index = last_sp->next_offset;
	int dst_index = sp->next_offset;

	/* bb->in_bb might contain duplicates */
	for (l = next [src_index]; l; l = l->next)
		if (GPOINTER_TO_UINT (l->data) == dst_index)
			break;
	if (!l)
		next [src_index] = g_slist_append (next [src_index], GUINT_TO_POINTER (dst_index));
}

static void
recursively_make_pred_seq_points (TransformData *td, InterpBasicBlock *bb)
{
	SeqPoint ** const MONO_SEQ_SEEN_LOOP = (SeqPoint**)GINT_TO_POINTER(-1);

	GArray *predecessors = g_array_new (FALSE, TRUE, sizeof (gpointer));
	GHashTable *seen = g_hash_table_new_full (g_direct_hash, NULL, NULL, NULL);

	// Insert/remove sentinel into the memoize table to detect loops containing bb
	bb->pred_seq_points = MONO_SEQ_SEEN_LOOP;

	for (int i = 0; i < bb->in_count; ++i) {
		InterpBasicBlock *in_bb = bb->in_bb [i];

		// This bb has the last seq point, append it and continue
		if (in_bb->last_seq_point != NULL) {
			predecessors = g_array_append_val (predecessors, in_bb->last_seq_point);
			continue;
		}

		// We've looped or handled this before, exit early.
		// No last sequence points to find.
		if (in_bb->pred_seq_points == MONO_SEQ_SEEN_LOOP)
			continue;

		// Take sequence points from incoming basic blocks

		if (in_bb == td->entry_bb)
			continue;

		if (in_bb->pred_seq_points == NULL)
			recursively_make_pred_seq_points (td, in_bb);

		// Union sequence points with incoming bb's
		for (guint j = 0; j < in_bb->num_pred_seq_points; j++) {
			if (!g_hash_table_lookup (seen, in_bb->pred_seq_points [j])) {
				g_array_append_val (predecessors, in_bb->pred_seq_points [j]);
				g_hash_table_insert (seen, in_bb->pred_seq_points [j], (gpointer)&MONO_SEQ_SEEN_LOOP);
			}
		}
		// predecessors = g_array_append_vals (predecessors, in_bb->pred_seq_points, in_bb->num_pred_seq_points);
	}

	g_hash_table_destroy (seen);

	if (predecessors->len != 0) {
		bb->pred_seq_points = (SeqPoint**)mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint *) * predecessors->len);
		bb->num_pred_seq_points = predecessors->len;

		for (guint newer = 0; newer < bb->num_pred_seq_points; newer++) {
			bb->pred_seq_points [newer] = (SeqPoint*)g_array_index (predecessors, gpointer, newer);
		}
	}

	g_array_free (predecessors, TRUE);
}

static void
collect_pred_seq_points (TransformData *td, InterpBasicBlock *bb, SeqPoint *seqp, GSList **next)
{
	// Doesn't have a last sequence point, must find from incoming basic blocks
	if (bb->pred_seq_points == NULL && bb != td->entry_bb)
		recursively_make_pred_seq_points (td, bb);

	for (guint i = 0; i < bb->num_pred_seq_points; i++)
		insert_pred_seq_point (bb->pred_seq_points [i], seqp, next);

	return;
}

static void
save_seq_points (TransformData *td, MonoJitInfo *jinfo)
{
	GByteArray *array;
	int seq_info_size;
	MonoSeqPointInfo *info;
	GSList **next = NULL;
	GList *bblist;

	if (!td->gen_seq_points)
		return;

	/*
	 * For each sequence point, compute the list of sequence points immediately
	 * following it, this is needed to implement 'step over' in the debugger agent.
	 * Similar to the code in mono_save_seq_point_info ().
	 */
	for (guint i = 0; i < td->seq_points->len; ++i) {
		SeqPoint *sp = (SeqPoint*)g_ptr_array_index (td->seq_points, i);

		/* Store the seq point index here temporarily */
		sp->next_offset = i;
	}
	next = (GSList**)mono_mempool_alloc0 (td->mempool, sizeof (GList*) * td->seq_points->len);
	for (bblist = td->basic_blocks; bblist; bblist = bblist->next) {
		InterpBasicBlock *bb = (InterpBasicBlock*)bblist->data;

		GSList *bb_seq_points = g_slist_reverse (bb->seq_points);
		SeqPoint *last = NULL;
		for (GSList *l = bb_seq_points; l; l = l->next) {
			SeqPoint *sp = (SeqPoint*)l->data;

			if (sp->il_offset == METHOD_ENTRY_IL_OFFSET || sp->il_offset == METHOD_EXIT_IL_OFFSET)
				/* Used to implement method entry/exit events */
				continue;

			if (last != NULL) {
				/* Link with the previous seq point in the same bb */
				next [last->next_offset] = g_slist_append_mempool (td->mempool, next [last->next_offset], GINT_TO_POINTER (sp->next_offset));
			} else {
				/* Link with the last bb in the previous bblocks */
				collect_pred_seq_points (td, bb, sp, next);
			}
			last = sp;
		}
	}

	/* Serialize the seq points into a byte array */
	array = g_byte_array_new ();
	SeqPoint zero_seq_point = {0};
	SeqPoint* last_seq_point = &zero_seq_point;
	for (guint i = 0; i < td->seq_points->len; ++i) {
		SeqPoint *sp = (SeqPoint*)g_ptr_array_index (td->seq_points, i);

		sp->next_offset = 0;
		if (mono_seq_point_info_add_seq_point (array, sp, last_seq_point, next [i], TRUE))
			last_seq_point = sp;
	}

	if (td->verbose_level) {
		g_print ("\nSEQ POINT MAP FOR %s: \n", td->method->name);

		for (guint i = 0; i < td->seq_points->len; ++i) {
			SeqPoint *sp = (SeqPoint*)g_ptr_array_index (td->seq_points, i);
			GSList *l;

			if (!next [i])
				continue;

			g_print ("\tIL0x%x[0x%0x] ->", sp->il_offset, sp->native_offset);
			for (l = next [i]; l; l = l->next) {
				int next_index = GPOINTER_TO_UINT (l->data);
				g_print (" IL0x%x", ((SeqPoint*)g_ptr_array_index (td->seq_points, next_index))->il_offset);
			}
			g_print ("\n");
		}
	}

	info = mono_seq_point_info_new (array->len, TRUE, array->data, TRUE, &seq_info_size);
	mono_atomic_fetch_add_i32 (&mono_jit_stats.allocated_seq_points_size, seq_info_size);

	g_byte_array_free (array, TRUE);

	jinfo->seq_points = info;
}

static void
interp_emit_memory_barrier (TransformData *td, int kind)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
	if (kind == MONO_MEMORY_BARRIER_SEQ)
		interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
#else
	interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
#endif
}

#define BARRIER_IF_VOLATILE(td, kind) \
	do { \
		if (volatile_) { \
			interp_emit_memory_barrier (td, kind); \
			volatile_ = FALSE; \
		} \
	} while (0)

#define INLINE_FAILURE \
	do { \
		if (inlining) \
			goto exit; \
	} while (0)

int
mono_interp_type_size (MonoType *type, int mt, int *align_p)
{
	int size, align;
	if (mt == MINT_TYPE_VT) {
		size = mono_type_size (type, &align);
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (m_class_is_simd_type (klass)) // mono_type_size should report the alignment
			align = MINT_SIMD_ALIGNMENT;
		else
			align = MINT_STACK_SLOT_SIZE;
		g_assert (align <= MINT_STACK_ALIGNMENT);
	} else {
		size = MINT_STACK_SLOT_SIZE; // not really
		align = MINT_STACK_SLOT_SIZE;
	}
	*align_p = align;
	return size;
}

static void
interp_method_compute_offsets (TransformData *td, InterpMethod *imethod, MonoMethodSignature *sig, MonoMethodHeader *header, MonoError *error)
{
	int offset, size, align;
	int num_args = sig->hasthis + sig->param_count;
	int num_il_locals = header->num_locals;
	int num_locals = num_args + num_il_locals;

	imethod->local_offsets = (guint32*)g_malloc (num_il_locals * sizeof(guint32));
	td->vars = (InterpVar*)g_malloc0 (num_locals * sizeof (InterpVar));
	td->vars_size = num_locals;
	td->vars_capacity = td->vars_size;

	td->renamable_vars = (InterpRenamableVar*)g_malloc (num_locals * sizeof (InterpRenamableVar));
	td->renamable_vars_size = 0;
	td->renamable_vars_capacity = num_locals;
	offset = 0;

	/*
	 * We will load arguments as if they are locals. Unlike normal locals, every argument
	 * is stored in a stackval sized slot and valuetypes have special semantics since we
	 * receive a pointer to the valuetype data rather than the data itself.
	 */
	for (int i = 0; i < num_args; i++) {
		MonoType *type;
		if (sig->hasthis && i == 0)
			type = m_class_is_valuetype (td->method->klass) ? m_class_get_this_arg (td->method->klass) : m_class_get_byval_arg (td->method->klass);
		else
			type = mono_method_signature_internal (td->method)->params [i - sig->hasthis];
		int mt = mono_mint_type (type);
		td->vars [i].type = type;
		td->vars [i].global = TRUE;
		td->vars [i].il_global = TRUE;
		td->vars [i].indirects = 0;
		td->vars [i].mt = mt;
		td->vars [i].ext_index = -1;
		size = mono_interp_type_size (type, mt, &align);
		td->vars [i].size = size;
		offset = ALIGN_TO (offset, align);
		td->vars [i].offset = offset;
		offset += size;
	}
	offset = ALIGN_TO (offset, MINT_STACK_ALIGNMENT);

	td->il_locals_offset = offset;
	for (int i = 0; i < num_il_locals; ++i) {
		int index = num_args + i;
		int mt = mono_mint_type (header->locals [i]);
		size = mono_interp_type_size (header->locals [i], mt, &align);
		if (header->locals [i]->type == MONO_TYPE_VALUETYPE) {
			if (mono_class_has_failure (header->locals [i]->data.klass)) {
				mono_error_set_for_class_failure (error, header->locals [i]->data.klass);
				return;
			}
		}
		offset = ALIGN_TO (offset, align);
		imethod->local_offsets [i] = offset;
		td->vars [index].type = header->locals [i];
		td->vars [index].offset = offset;
		td->vars [index].global = TRUE;
		td->vars [index].il_global = TRUE;
		td->vars [index].indirects = 0;
		td->vars [index].mt = mono_mint_type (header->locals [i]);
		td->vars [index].ext_index = -1;
		td->vars [index].size = size;
		// Every local takes a MINT_STACK_SLOT_SIZE so IL locals have same behavior as execution locals
		offset += size;
	}
	offset = ALIGN_TO (offset, MINT_STACK_ALIGNMENT);

	td->il_locals_size = offset - td->il_locals_offset;
	td->total_locals_size = offset;

	imethod->clause_data_offsets = (guint32*)g_malloc (header->num_clauses * sizeof (guint32));
	td->clause_vars = (int*)mono_mempool_alloc (td->mempool, sizeof (int) * header->num_clauses);
	for (guint i = 0; i < header->num_clauses; i++) {
		int var = interp_create_var (td, mono_get_object_type ());
		td->vars [var].global = TRUE;
		interp_alloc_global_var_offset (td, var);
		imethod->clause_data_offsets [i] = td->vars [var].offset;
		td->clause_vars [i] = var;
	}
}

void
mono_test_interp_method_compute_offsets (TransformData *td, InterpMethod *imethod, MonoMethodSignature *signature, MonoMethodHeader *header)
{
	ERROR_DECL (error);
	interp_method_compute_offsets (td, imethod, signature, header, error);
}

static gboolean
type_has_references (MonoType *type)
{
	if (MONO_TYPE_IS_REFERENCE (type))
		return TRUE;
	if (MONO_TYPE_ISSTRUCT (type)) {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		if (!m_class_is_inited (klass))
			mono_class_init_internal (klass);
		return m_class_has_references (klass);
	}
	return FALSE;
}

#ifdef NO_UNALIGNED_ACCESS
static int
get_unaligned_opcode (int opcode)
{
	switch (opcode) {
		case MINT_LDFLD_I8:
			return MINT_LDFLD_I8_UNALIGNED;
		case MINT_LDFLD_R8:
			return MINT_LDFLD_R8_UNALIGNED;
		case MINT_STFLD_I8:
			return MINT_STFLD_I8_UNALIGNED;
		case MINT_STFLD_R8:
			return MINT_STFLD_R8_UNALIGNED;
		default:
			g_assert_not_reached ();
	}
	return -1;
}
#endif

static void
interp_handle_isinst (TransformData *td, MonoClass *klass, gboolean isinst_instr)
{
	/* Follow the logic from jit's handle_isinst */
	if (!mono_class_has_variant_generic_params (klass)) {
		if (mono_class_is_interface (klass))
			interp_add_ins (td, isinst_instr ? MINT_ISINST_INTERFACE : MINT_CASTCLASS_INTERFACE);
		else if (m_class_get_rank (klass) == 0 && !mono_class_is_nullable (klass))
			interp_add_ins (td, isinst_instr ? MINT_ISINST_COMMON : MINT_CASTCLASS_COMMON);
		else
			interp_add_ins (td, isinst_instr ? MINT_ISINST : MINT_CASTCLASS);
	} else {
		interp_add_ins (td, isinst_instr ? MINT_ISINST : MINT_CASTCLASS);
	}
	td->sp--;
	interp_ins_set_sreg (td->last_ins, td->sp [0].var);
	if (isinst_instr)
		push_type (td, td->sp [0].type, td->sp [0].klass);
	else
		push_type (td, STACK_TYPE_O, klass);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	td->last_ins->data [0] = get_data_item_index (td, klass);

	td->ip += 5;
}

static void
interp_emit_ldsflda (TransformData *td, MonoClassField *field, MonoError *error)
{
	// Initialize the offset for the field
	MonoVTable *vtable = mono_class_vtable_checked (m_field_get_parent (field), error);
	return_if_nok (error);

	push_simple_type (td, STACK_TYPE_MP);
	if (mono_class_field_is_special_static (field)) {
		guint32 offset = GPOINTER_TO_UINT (mono_special_static_field_get_offset (field, error));
		mono_error_assert_ok (error);
		g_assert (offset);

		interp_add_ins (td, MINT_LDTSFLDA);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		WRITE32_INS(td->last_ins, 0, &offset);
	} else {
		interp_add_ins (td, MINT_LDSFLDA);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		td->last_ins->data [0] = get_data_item_index (td, vtable);
		td->last_ins->data [1] = get_data_item_index (td, mono_static_field_get_addr (vtable, field));
	}
}

static gboolean
interp_emit_load_const (TransformData *td, gpointer field_addr, int mt)
{
	if (mt == MINT_TYPE_VT)
		return FALSE;

	push_simple_type (td, stack_type [mt]);
	if ((mt >= MINT_TYPE_I1 && mt <= MINT_TYPE_I4)) {
		gint32 val;
		switch (mt) {
		case MINT_TYPE_I1:
			val = *(gint8*)field_addr;
			break;
		case MINT_TYPE_U1:
			val = *(guint8*)field_addr;
			break;
		case MINT_TYPE_I2:
			val = *(gint16*)field_addr;
			break;
		case MINT_TYPE_U2:
			val = *(guint16*)field_addr;
			break;
		default:
			val = *(gint32*)field_addr;
		}
		interp_get_ldc_i4_from_const (td, NULL, val, td->sp [-1].var);
	} else if (mt == MINT_TYPE_I8) {
		gint64 val = *(gint64*)field_addr;
		interp_add_ins (td, MINT_LDC_I8);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		WRITE64_INS (td->last_ins, 0, &val);
	} else if (mt == MINT_TYPE_R4) {
		float val = *(float*)field_addr;
		interp_add_ins (td, MINT_LDC_R4);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		WRITE32_INS (td->last_ins, 0, &val);
	} else if (mt == MINT_TYPE_R8) {
		double val = *(double*)field_addr;
		interp_add_ins (td, MINT_LDC_R8);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		WRITE64_INS (td->last_ins, 0, &val);
	} else {
		// Revert stack
		td->sp--;
		return FALSE;
	}
	return TRUE;
}

static void
interp_emit_sfld_access (TransformData *td, MonoClassField *field, MonoClass *field_class, int mt, gboolean is_load, MonoError *error)
{
	// Initialize the offset for the field
	MonoVTable *vtable = mono_class_vtable_checked (m_field_get_parent (field), error);
	return_if_nok (error);

	MonoType *ftype = mono_field_get_type_internal (field);
	if (ftype->attrs & FIELD_ATTRIBUTE_LITERAL) {
		mono_error_set_generic_error (error, "System", "MissingFieldException", "Using static instructions with literal field");
		return;
	}

	if (mono_class_field_is_special_static (field)) {
		guint32 offset = GPOINTER_TO_UINT (mono_special_static_field_get_offset (field, error));
		mono_error_assert_ok (error);
		g_assert (offset && (offset & 0x80000000) == 0);

		// Load address of thread static field
		push_simple_type (td, STACK_TYPE_MP);
		interp_add_ins (td, MINT_LDTSFLDA);
		interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		WRITE32_INS (td->last_ins, 0, &offset);

		// Do a load/store to this address
		if (is_load) {
			if (mt == MINT_TYPE_VT) {
				int field_size = mono_class_value_size (field_class, NULL);
				interp_add_ins (td, MINT_LDOBJ_VT);
				interp_ins_set_sreg (td->last_ins, td->sp [-1].var);
				td->sp--;
				push_type_vt (td, field_class, field_size);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = GINT_TO_UINT16 (field_size);
			} else {
				interp_add_ins (td, interp_get_ldind_for_mt (mt));
				interp_ins_set_sreg (td->last_ins, td->sp [-1].var);
				td->sp--;
				push_type (td, stack_type [mt], field_class);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			}
		} else {
			interp_emit_stobj (td, field_class, TRUE);
		}
	} else {
		gpointer field_addr = mono_static_field_get_addr (vtable, field);
		int size = 0;
		if (mt == MINT_TYPE_VT)
			size = mono_class_value_size (field_class, NULL);
		if (is_load) {
			if (ftype->attrs & FIELD_ATTRIBUTE_INIT_ONLY && vtable->initialized) {
				if (interp_emit_load_const (td, field_addr, mt))
					return;
			}
		}
		guint32 vtable_index = get_data_item_wide_index (td, vtable, NULL);
		guint32 addr_index = get_data_item_wide_index (td, (char*)field_addr, NULL);
		gboolean wide_data = is_data_item_wide_index (vtable_index) || is_data_item_wide_index (addr_index);
		guint32 klass_index = !wide_data ? 0 : get_data_item_wide_index (td, field_class, NULL);
		if (is_load) {
			if (G_UNLIKELY (wide_data)) {
				interp_add_ins (td, MINT_LDSFLD_W);
				if (mt == MINT_TYPE_VT) {
					push_type_vt (td, field_class, size);
				} else {
					push_type (td, stack_type [mt], field_class);
				}
			} else if (mt == MINT_TYPE_VT) {
				interp_add_ins (td, MINT_LDSFLD_VT);
				push_type_vt (td, field_class, size);
			} else {
				interp_add_ins (td, MINT_LDSFLD_I1 + mt - MINT_TYPE_I1);
				push_type (td, stack_type [mt], field_class);
			}
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
		} else {
			if (G_LIKELY (!wide_data))
				interp_add_ins (td, (mt == MINT_TYPE_VT) ? MINT_STSFLD_VT : (MINT_STSFLD_I1 + mt - MINT_TYPE_I1));
			else
				interp_add_ins (td, MINT_STSFLD_W);
			td->sp--;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
		}

		if (G_LIKELY (!wide_data)) {
			td->last_ins->data [0] = (guint16) vtable_index;
			td->last_ins->data [1] = (guint16) addr_index;
			if (mt == MINT_TYPE_VT)
				td->last_ins->data [2] = GINT_TO_UINT16 (size);
		} else {
			WRITE32_INS (td->last_ins, 0, &vtable_index);
			WRITE32_INS (td->last_ins, 2, &addr_index);
			WRITE32_INS (td->last_ins, 4, &klass_index);
		}

	}
}

static void
initialize_clause_bblocks (TransformData *td)
{
	MonoMethodHeader *header = td->header;

	for (guint32 i = 0; i < header->code_size; i++)
		td->clause_indexes [i] = -1;

	for (guint i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = header->clauses + i;
		InterpBasicBlock *try_bb, *bb;

		for (uint32_t j = c->handler_offset; j < c->handler_offset + c->handler_len; j++) {
			if (td->clause_indexes [j] == -1)
				td->clause_indexes [j] = i;
		}

		try_bb = td->offset_to_bb [c->try_offset];
		g_assert (try_bb);
		try_bb->preserve = TRUE;

		/* We never inline methods with clauses, so we can hard code stack heights */
		bb = td->offset_to_bb [c->handler_offset];
		g_assert (bb);
		bb->preserve = TRUE;
		bb->try_bblock = try_bb;

		if (c->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
			bb->stack_height = 0;
		} else {
			bb->stack_height = 1;
			bb->stack_state = (StackInfo*) mono_mempool_alloc0 (td->mempool, sizeof (StackInfo));
			bb->stack_state [0].type = STACK_TYPE_O;
			bb->stack_state [0].klass = NULL; /*FIX*/
			bb->stack_state [0].size = MINT_STACK_SLOT_SIZE;
			bb->stack_state [0].var = td->clause_vars [i];
		}

		if (c->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			bb = td->offset_to_bb [c->data.filter_offset];
			g_assert (bb);
			bb->preserve = TRUE;
			bb->try_bblock = try_bb;

			bb->stack_height = 1;
			bb->stack_state = (StackInfo*) mono_mempool_alloc0 (td->mempool, sizeof (StackInfo));
			bb->stack_state [0].type = STACK_TYPE_O;
			bb->stack_state [0].klass = NULL; /*FIX*/
			bb->stack_state [0].size = MINT_STACK_SLOT_SIZE;
			bb->stack_state [0].var = td->clause_vars [i];
		} else if (c->flags == MONO_EXCEPTION_CLAUSE_NONE) {
			/*
			 * JIT doesn't emit sdb seq intr point at the start of catch clause, probably
			 * by accident. Mimic the same behavior with the interpreter for now. Because
			 * this bb is not empty, we won't emit a MINT_SDB_INTR_LOC when generating the code
			 */
			interp_insert_ins_bb (td, bb, NULL, MINT_NOP);
		}
	}

}

static void
handle_ldind (TransformData *td, int op, int type, gboolean *volatile_)
{
	CHECK_STACK_RET_VOID (td, 1);
	interp_add_ins (td, op);
	td->sp--;
	interp_ins_set_sreg (td->last_ins, td->sp [0].var);
	push_simple_type (td, type);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

	if (*volatile_) {
		interp_emit_memory_barrier (td, MONO_MEMORY_BARRIER_ACQ);
		*volatile_ = FALSE;
	}
	++td->ip;
}

static void
handle_stind (TransformData *td, int op, gboolean *volatile_)
{
	CHECK_STACK_RET_VOID (td, 2);
	if (*volatile_) {
		interp_emit_memory_barrier (td, MONO_MEMORY_BARRIER_REL);
		*volatile_ = FALSE;
	}
	interp_add_ins (td, op);
	td->sp -= 2;
	interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);

	++td->ip;
}

static void
handle_ldelem (TransformData *td, int op, int type)
{
	CHECK_STACK_RET_VOID (td, 2);
	ENSURE_I4 (td, 1);
	interp_add_ins (td, op);
	td->sp -= 2;
	interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
	push_simple_type (td, type);
	interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
	++td->ip;
}

static void
handle_stelem (TransformData *td, int op)
{
	CHECK_STACK_RET_VOID (td, 3);
	ENSURE_I4 (td, 2);
	interp_add_ins (td, op);
	td->sp -= 3;
	interp_ins_set_sregs3 (td->last_ins, td->sp [0].var, td->sp [1].var, td->sp [2].var);
	++td->ip;
}

static gboolean
is_ip_protected (MonoMethodHeader *header, int offset)
{
	for (unsigned int i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *clause = &header->clauses [i];
		if (clause->try_offset <= GINT_TO_UINT32(offset) && GINT_TO_UINT32(offset) < (clause->try_offset + clause->try_len))
			return TRUE;
	}
	return FALSE;
}

static gboolean
should_insert_seq_point (TransformData *td)
{
	//following the CoreCLR's algorithm for adding the sequence points
	if ((*td->ip == CEE_NOP) ||
		(*td->ip == CEE_CALLVIRT) ||
		(*td->ip == CEE_CALLI) ||
		(*td->ip == CEE_CALL) ||
		(GPTRDIFF_TO_INT (td->sp - td->stack) == 0))
		return TRUE;
	return FALSE;
}

static gboolean
generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error)
{
	int mt, i32;
	guint32 token;
	int in_offset;
	const unsigned char *end;
	MonoSimpleBasicBlock *bb = NULL, *original_bb = NULL;
	gboolean sym_seq_points = FALSE;
	MonoBitSet *seq_point_locs = NULL;
	MonoBitSet *il_targets = NULL;
	gboolean readonly = FALSE;
	gboolean volatile_ = FALSE;
	gboolean tailcall = FALSE;
	MonoClass *constrained_class = NULL;
	MonoClass *klass;
	MonoClassField *field;
	MonoImage *image = m_class_get_image (method->klass);
	InterpMethod *rtm = td->rtm;
	MonoMethodSignature *signature = mono_method_signature_internal (method);
	int num_args = signature->hasthis + signature->param_count;
	int arglist_local = -1;
	int call_handler_count = 0;
	gboolean ret = TRUE;
	gboolean emitted_funccall_seq_point = FALSE;
	guint32 *arg_locals = NULL;
	guint32 *local_locals = NULL;
	InterpInst *last_seq_point = NULL;
	gboolean save_last_error = FALSE;
	gboolean link_bblocks = TRUE;
	gboolean inlining = td->method != method;
	gboolean generate_enc_seq_points_without_debug_info = FALSE;
	InterpBasicBlock *exit_bb = NULL;

	original_bb = bb = mono_basic_block_split (method, error, header);
	goto_if_nok (error, exit);
	g_assert (bb);

	td->il_code = header->code;
	td->in_start = td->ip = header->code;
	end = td->ip + header->code_size;

	td->cbb = td->entry_bb = interp_alloc_bb (td);
	if (td->gen_sdb_seq_points)
		td->basic_blocks = g_list_prepend_mempool (td->mempool, td->basic_blocks, td->cbb);

	td->cbb->stack_height = GPTRDIFF_TO_INT (td->sp - td->stack);

	if (inlining)
		exit_bb = interp_alloc_bb (td);
	else
		td->entry_bb->il_offset = 0;

	il_targets = mono_bitset_mem_new (
		mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (header->code_size, 0)),
		header->code_size, 0);
	if (!get_basic_blocks (td, header, td->gen_sdb_seq_points, il_targets)) {
		td->has_invalid_code = TRUE;
		goto exit;
	}

	if (!inlining)
		initialize_clause_bblocks (td);

	if (td->gen_sdb_seq_points && !inlining) {
		MonoDebugMethodInfo *minfo;

		minfo = mono_debug_lookup_method (method);

		if (minfo) {
			MonoSymSeqPoint *sps;
			int n_il_offsets;

			mono_debug_get_seq_points (minfo, NULL, NULL, NULL, &sps, &n_il_offsets);
			if (n_il_offsets == 0)
				generate_enc_seq_points_without_debug_info = mono_debug_generate_enc_seq_points_without_debug_info (minfo);
			// FIXME: Free
			seq_point_locs = mono_bitset_mem_new (mono_mempool_alloc0 (td->mempool, mono_bitset_alloc_size (header->code_size, 0)), header->code_size, 0);
			sym_seq_points = TRUE;

			for (int i = 0; i < n_il_offsets; ++i) {
				if (GINT_TO_UINT32(sps [i].il_offset) < header->code_size)
					mono_bitset_set_fast (seq_point_locs, sps [i].il_offset);
			}
			g_free (sps);

			MonoDebugMethodAsyncInfo* asyncMethod = mono_debug_lookup_method_async_debug_info (method);
			if (asyncMethod) {
				for (int i = 0; asyncMethod != NULL && i < asyncMethod->num_awaits; i++) {
					mono_bitset_set_fast (seq_point_locs, asyncMethod->resume_offsets [i]);
					mono_bitset_set_fast (seq_point_locs, asyncMethod->yield_offsets [i]);
				}
				mono_debug_free_method_async_debug_info (asyncMethod);
			}
		} else if (!method->wrapper_type && !method->dynamic && mono_debug_image_has_debug_info (m_class_get_image (method->klass))) {
			/* Methods without line number info like auto-generated property accessors */
			seq_point_locs = mono_bitset_new (header->code_size, 0);
			sym_seq_points = TRUE;
		}
	}

	if (sym_seq_points) {
		last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
		last_seq_point->flags |= INTERP_INST_FLAG_SEQ_POINT_METHOD_ENTRY;
	}

	if (!td->optimized)
		interp_add_ins (td, MINT_TIER_ENTER_METHOD);

	if (mono_debugger_method_has_breakpoint (method)) {
		interp_add_ins (td, MINT_BREAKPOINT);
	}

	if (!inlining) {
		if (td->verbose_level) {
			char *tmp = mono_disasm_code (NULL, method, td->ip, end);
			char *name = mono_method_full_name (method, TRUE);
			g_print ("Method %s, optimized %d, original code:\n", name, td->optimized);
			g_print ("%s\n", tmp);
			g_free (tmp);
			g_free (name);
		}

		if (td->optimized && !td->disable_ssa) {
			// Add arg defining instructions for SSA machinery
			for (int i = 0; i < num_args; i++) {
				interp_add_ins (td, MINT_DEF_ARG);
				interp_ins_set_dreg (td->last_ins, i);
			}
		}

		if (rtm->vararg) {
			// vararg calls are identical to normal calls on the call site. However, the
			// first instruction in a vararg method needs to copy the variable arguments
			// into a special region so they can be accessed by MINT_ARGLIST. This region
			// is localloc'ed so we have compile time static offsets for all locals/stack.
			arglist_local = interp_create_var (td, m_class_get_byval_arg (mono_defaults.int_class));
			interp_add_ins (td, MINT_INIT_ARGLIST);
			interp_ins_set_dreg (td->last_ins, arglist_local);
			// This is the offset where the variable args are on stack. After this instruction
			// which copies them to localloc'ed memory, this space will be overwritten by normal
			// locals
			td->last_ins->data [0] = GUINT_TO_UINT16 (td->il_locals_offset);
			td->has_localloc = TRUE;
		}

		guint16 enter_profiling = 0;
		if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
			enter_profiling |= TRACING_FLAG;
		if (rtm->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_ENTER)
			enter_profiling |= PROFILING_FLAG;
		if (enter_profiling) {
			interp_add_ins (td, MINT_PROF_ENTER);
			td->last_ins->data [0] = enter_profiling;
		}

		/*
		 * If safepoints are required by default, always check for polling,
		 * without emitting new instructions. This optimizes method entry in
		 * the common scenario, which is coop.
		 */
#if !defined(ENABLE_HYBRID_SUSPEND) && !defined(ENABLE_COOP_SUSPEND)
		/* safepoint is required on method entry */
		if (mono_threads_are_safepoints_enabled ())
			interp_add_ins (td, MINT_SAFEPOINT);
#endif
	} else {
		int local;
		arg_locals = (guint32*) g_malloc ((!!signature->hasthis + signature->param_count) * sizeof (guint32));
		/* Allocate locals to store inlined method args from stack */
		for (int i = signature->param_count - 1; i >= 0; i--) {
			MonoType *type = td->vars [td->sp [-1].var].type;
			local = interp_create_var (td, type);
			arg_locals [i + !!signature->hasthis] = local;
			store_local (td, local);
		}

		if (signature->hasthis) {
			/*
			 * If this is value type, it is passed by address and not by value.
			 * Valuetype this local gets integer type MINT_TYPE_I.
			 */
			MonoType *type = td->vars [td->sp [-1].var].type;
			local = interp_create_var (td, type);
			arg_locals [0] = local;
			store_local (td, local);
		}

		local_locals = (guint32*) g_malloc (header->num_locals * sizeof (guint32));
		for (int i = 0; i < header->num_locals; i++)
			local_locals [i] = interp_create_var (td, header->locals [i]);
	}

	/*
	 * We initialize the locals regardless of the presence of the init_locals
	 * flag. Locals holding references need to be zeroed so we don't risk
	 * crashing the GC if they end up being stored in an object.
	 */
	if (header->num_locals) {
		if (td->optimized) {
			// Add individual initlocal for each IL local. These should
			// all be optimized out by SSA cprop/deadce optimizations.
			for (int i = 0; i < header->num_locals; i++) {
				interp_add_ins (td, MINT_INITLOCAL);
				int local_var = inlining ? local_locals [i] : (num_args + i);
				td->last_ins->dreg = local_var;
				td->last_ins->data [0] = GINT_TO_UINT16 (td->vars [local_var].size);
			}
		} else {
			interp_add_ins (td, MINT_INITLOCALS);
			td->last_ins->data [0] = GUINT_TO_UINT16 (td->il_locals_offset);
			td->last_ins->data [1] = GUINT_TO_UINT16 (td->il_locals_size);
		}
	}

	td->dont_inline = g_list_prepend (td->dont_inline, method);
	while (td->ip < end) {
		// Check here for every opcode to avoid code bloat
		if (td->has_invalid_code)
			goto exit;
		in_offset = GPTRDIFF_TO_INT (td->ip - header->code);
		if (!inlining)
			td->current_il_offset = in_offset;

		InterpBasicBlock *new_bb = td->offset_to_bb [in_offset];
		if (new_bb != NULL && td->cbb != new_bb) {
			/* We are starting a new basic block. Change cbb and link them together */
			if (link_bblocks) {
				if (!new_bb->jump_targets && td->cbb->no_inlining) {
					// This is a bblock that is not branched to and falls through from
					// a dead predecessor. It means it is dead.
					new_bb->no_inlining = TRUE;
					if (td->verbose_level)
						g_print ("Disable inlining in BB%d\n", new_bb->index);
				}
				/*
				 * By default we link cbb with the new starting bblock, unless the previous
				 * instruction is an unconditional branch (BR, LEAVE, ENDFINALLY)
				 */
				interp_link_bblocks (td, td->cbb, new_bb);
				fixup_newbb_stack_locals (td, new_bb);
			} else if (!new_bb->jump_targets) {
				// This is a bblock that is not branched to and it is not linked to the
				// predecessor. It means it is dead.
				new_bb->no_inlining = TRUE;
				if (td->verbose_level)
					g_print ("Disable inlining in BB%d\n", new_bb->index);
			} else {
				g_assert (new_bb->jump_targets > 0);
			}
			td->cbb->next_bb = new_bb;
			td->cbb = new_bb;

			if (new_bb->stack_height >= 0) {
				if (new_bb->stack_height > 0) {
					if (link_bblocks)
						merge_stack_type_information (td->stack, new_bb->stack_state, new_bb->stack_height);
					// This is relevant only for copying the vars associated with the values on the stack
					memcpy (td->stack, new_bb->stack_state, new_bb->stack_height * sizeof(td->stack [0]));
				}
				td->sp = td->stack + new_bb->stack_height;
			} else if (link_bblocks) {
				/* This bblock is not branched to. Initialize its stack state */
				init_bb_stack_state (td, new_bb);
			}
			link_bblocks = TRUE;
			// Unoptimized code cannot access exception object directly from the exvar, we need
			// to push it explicitly on the execution stack
			if (!td->optimized) {
                                int index = td->clause_indexes [in_offset];
                                if (index != -1 && new_bb->stack_height == 1 && header->clauses [index].handler_offset == in_offset) {
					int exvar = td->clause_vars [index];
					g_assert (td->stack [0].var == exvar);
					td->sp--;
					push_simple_type (td, STACK_TYPE_O);
					interp_add_ins (td, MINT_MOV_P);
					interp_ins_set_sreg (td->last_ins, exvar);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
                                }
                        }
		}
		td->offset_to_bb [in_offset] = td->cbb;
		td->in_start = td->ip;

		if (in_offset == bb->end)
			bb = bb->next;

		/* Checks that a jump target isn't in the middle of opcode offset */
		int op_size = mono_opcode_size (td->ip, end);
		for (int i = 1; i < op_size; i++) {
			if (mono_bitset_test(il_targets, in_offset + i)) {
				td->has_invalid_code = TRUE;
				goto exit;
			}
		}
		if (bb->dead || td->cbb->dead) {
			g_assert (op_size > 0); /* The BB formation pass must catch all bad ops */

			if (td->verbose_level > 1)
				g_print ("SKIPPING DEAD OP at %x\n", in_offset);
			link_bblocks = FALSE;
			td->ip += op_size;
			continue;
		}

		if (td->verbose_level > 1) {
			g_print ("IL_%04lx %-10s, sp %ld, %s %-12s\n",
				td->ip - td->il_code,
				mono_opcode_name (*td->ip), td->sp - td->stack,
				td->sp > td->stack ? stack_type_string [td->sp [-1].type] : "  ",
				(td->sp > td->stack && (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_VT)) ? (td->sp [-1].klass == NULL ? "?" : m_class_get_name (td->sp [-1].klass)) : "");
		}

		if (td->gen_seq_points && ((!sym_seq_points && td->stack == td->sp) || (sym_seq_points && mono_bitset_test_fast (seq_point_locs, td->ip - header->code)))) {
			if (td->gen_sdb_seq_points) {
				if (in_offset == 0 || (header->num_clauses && !td->cbb->last_ins))
					interp_add_ins (td, MINT_SDB_INTR_LOC);
				last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
			} else {
				last_seq_point = interp_add_ins (td, MINT_IL_SEQ_POINT);
			}
		}

		if (td->prof_coverage) {
			ptrdiff_t cil_offset = td->ip - header->code;
			gpointer counter = &td->coverage_info->data [cil_offset].count;
			td->coverage_info->data [cil_offset].cil_code = (unsigned char*)td->ip;

			interp_add_ins (td, MINT_PROF_COVERAGE_STORE);
			WRITE64_INS (td->last_ins, 0, &counter);
		}

		if (G_UNLIKELY (generate_enc_seq_points_without_debug_info) && should_insert_seq_point (td))
			last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);

		switch (*td->ip) {
		case CEE_NOP:
			/* lose it */
			emitted_funccall_seq_point = FALSE;
			++td->ip;
			break;
		case CEE_BREAK:
			interp_add_ins (td, MINT_BREAK);
			++td->ip;
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int arg_n = *td->ip - CEE_LDARG_0;
			if (!inlining)
				load_arg (td, arg_n);
			else
				load_local (td, arg_locals [arg_n]);
			++td->ip;
			break;
		}
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int loc_n = *td->ip - CEE_LDLOC_0;
			if (!inlining)
				load_local (td, num_args + loc_n);
			else
				load_local (td, local_locals [loc_n]);
			++td->ip;
			break;
		}
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: {
			int loc_n = *td->ip - CEE_STLOC_0;
			if (!inlining)
				store_local (td, num_args + loc_n);
			else
				store_local (td, local_locals [loc_n]);
			++td->ip;
			break;
		}
		case CEE_LDARG_S: {
			int arg_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				load_arg (td, arg_n);
			else
				load_local (td, arg_locals [arg_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDARGA_S: {
			/* NOTE: n includes this */
			int n = ((guint8 *) td->ip) [1];

			if (!inlining) {
				interp_add_ins (td, MINT_LDLOCA_S);
				interp_ins_set_sreg (td->last_ins, n);
				td->vars [n].indirects++;
			} else {
				int loc_n = arg_locals [n];
				interp_add_ins (td, MINT_LDLOCA_S);
				interp_ins_set_sreg (td->last_ins, loc_n);
				td->vars [loc_n].indirects++;
			}
			push_simple_type (td, STACK_TYPE_MP);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 2;
			break;
		}
		case CEE_STARG_S: {
			int arg_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				store_arg (td, arg_n);
			else
				store_local (td, arg_locals [arg_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDLOC_S: {
			int loc_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				load_local (td, num_args + loc_n);
			else
				load_local (td, local_locals [loc_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDLOCA_S: {
			int loc_n = ((guint8 *)td->ip)[1];
			interp_add_ins (td, MINT_LDLOCA_S);
			if (!inlining)
				loc_n += num_args;
			else
				loc_n = local_locals [loc_n];
			interp_ins_set_sreg (td->last_ins, loc_n);
			td->vars [loc_n].indirects++;
			push_simple_type (td, STACK_TYPE_MP);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 2;
			break;
		}
		case CEE_STLOC_S: {
			int loc_n = ((guint8 *)td->ip)[1];
			if (!inlining)
				store_local (td, num_args + loc_n);
			else
				store_local (td, local_locals [loc_n]);
			td->ip += 2;
			break;
		}
		case CEE_LDNULL:
			interp_add_ins (td, MINT_LDNULL);
			push_type (td, STACK_TYPE_O, NULL);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			++td->ip;
			break;
		case CEE_LDC_I4_M1:
			interp_add_ins (td, MINT_LDC_I4_M1);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			++td->ip;
			break;
		case CEE_LDC_I4_0:
			if (in_offset + 2 < td->code_size && interp_ip_in_cbb (td, in_offset + 1) && td->ip [1] == 0xfe && td->ip [2] == CEE_CEQ &&
				td->sp > td->stack && td->sp [-1].type == STACK_TYPE_I4) {
				interp_add_ins (td, MINT_CEQ0_I4);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 3;
			} else {
				interp_add_ins (td, MINT_LDC_I4_0);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
			}
			break;
		case CEE_LDC_I4_1:
			if (in_offset + 1 < td->code_size && interp_ip_in_cbb (td, in_offset + 1) &&
				(td->ip [1] == CEE_ADD || td->ip [1] == CEE_SUB) && td->sp [-1].type == STACK_TYPE_I4) {
				interp_add_ins (td, td->ip [1] == CEE_ADD ? MINT_ADD1_I4 : MINT_SUB1_I4);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 2;
			} else {
				interp_add_ins (td, MINT_LDC_I4_1);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
			}
			break;
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			interp_add_ins (td, (*td->ip - CEE_LDC_I4_0) + MINT_LDC_I4_0);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			++td->ip;
			break;
		case CEE_LDC_I4_S:
			interp_add_ins (td, MINT_LDC_I4_S);
			td->last_ins->data [0] = ((gint8 *) td->ip) [1];
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 2;
			break;
		case CEE_LDC_I4:
			i32 = read32 (td->ip + 1);
			interp_add_ins (td, MINT_LDC_I4);
			WRITE32_INS (td->last_ins, 0, &i32);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			break;
		case CEE_LDC_I8: {
			gint64 val = read64 (td->ip + 1);
			interp_add_ins (td, MINT_LDC_I8);
			WRITE64_INS (td->last_ins, 0, &val);
			push_simple_type (td, STACK_TYPE_I8);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 9;
			break;
		}
		case CEE_LDC_R4: {
			float val;
			readr4 (td->ip + 1, &val);
			interp_add_ins (td, MINT_LDC_R4);
			WRITE32_INS (td->last_ins, 0, &val);
			push_simple_type (td, STACK_TYPE_R4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 5;
			break;
		}
		case CEE_LDC_R8: {
			double val;
			readr8 (td->ip + 1, &val);
			interp_add_ins (td, MINT_LDC_R8);
			WRITE64_INS (td->last_ins, 0, &val);
			push_simple_type (td, STACK_TYPE_R8);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->ip += 9;
			break;
		}
		case CEE_DUP: {
			int type = td->sp [-1].type;
			klass = td->sp [-1].klass;
			mt = td->vars [td->sp [-1].var].mt;
			if (mt == MINT_TYPE_VT) {
				gint32 size = mono_class_value_size (klass, NULL);
				g_assert (size < G_MAXUINT16);

				interp_add_ins (td, MINT_MOV_VT);
				interp_ins_set_sreg (td->last_ins, td->sp [-1].var);
				push_type_vt (td, klass, size);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = GINT32_TO_UINT16 (size);
			} else  {
				interp_add_ins (td, interp_get_mov_for_type (mt, FALSE));
				interp_ins_set_sreg (td->last_ins, td->sp [-1].var);
				push_type (td, type, klass);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			}
			td->ip++;
			break;
		}
		case CEE_POP:
			CHECK_STACK(td, 1);
			interp_add_ins (td, MINT_NOP);
			--td->sp;
			++td->ip;
			break;
		case CEE_JMP: {
			MonoMethod *m;
			INLINE_FAILURE;
			if (td->sp > td->stack)
				g_warning ("CEE_JMP: stack must be empty");
			token = read32 (td->ip + 1);
			m = mono_get_method_checked (image, token, NULL, generic_context, error);
			goto_if_nok (error, exit);
			interp_add_ins (td, MINT_JMP);
			td->last_ins->data [0] = get_data_item_index_imethod (td, mono_interp_get_imethod (m));
			td->ip += 5;
			break;
		}
		case CEE_CALLVIRT: /* Fall through */
		case CEE_CALLI:    /* Fall through */
		case CEE_CALL: {
			gboolean need_seq_point = FALSE;

			td->cbb->contains_call_instruction = TRUE;

			if (sym_seq_points && !mono_bitset_test_fast (seq_point_locs, td->ip + 5 - header->code))
				need_seq_point = TRUE;

			if (!interp_transform_call (td, method, NULL, generic_context, constrained_class, readonly, error, TRUE, save_last_error, tailcall))
				goto exit;

			if (need_seq_point && !generate_enc_seq_points_without_debug_info) {
				// check if it is a nested call and remove the MONO_INST_NONEMPTY_STACK of the last breakpoint, only for non native methods
				if (!(method->flags & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
					if (emitted_funccall_seq_point)	{
						if (last_seq_point)
							last_seq_point->flags |= INTERP_INST_FLAG_SEQ_POINT_NESTED_CALL;
					}
					else
						emitted_funccall_seq_point = TRUE;
				}
				last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
				// This seq point is actually associated with the instruction following the call
				last_seq_point->il_offset = GPTRDIFF_TO_INT (td->ip - header->code);
				last_seq_point->flags = INTERP_INST_FLAG_SEQ_POINT_NONEMPTY_STACK;
			}

			if (td->last_ins->opcode == MINT_TAILCALL || td->last_ins->opcode == MINT_TAILCALL_VIRT) {
				// Execution does not follow through
				link_bblocks = FALSE;
			}

			constrained_class = NULL;
			readonly = FALSE;
			save_last_error = FALSE;
			tailcall = FALSE;
			break;
		}
		case CEE_RET: {
			link_bblocks = FALSE;
			MonoType *ult = mini_type_get_underlying_type (signature->ret);
			mt = mono_mint_type (ult);
			if (mt != MINT_TYPE_VOID) {
				// Convert stack contents to return type if necessary
				CHECK_STACK (td, 1);
				emit_convert (td, td->sp - 1, ult);
			}
			/* Return from inlined method, return value is on top of stack */
			if (inlining) {
				td->ip++;
				fixup_newbb_stack_locals (td, exit_bb);
				interp_add_ins (td, MINT_BR);
				td->last_ins->info.target_bb = exit_bb;
				init_bb_stack_state (td, exit_bb);
				interp_link_bblocks (td, td->cbb, exit_bb);
				// If the next bblock didn't have its stack state yet initialized, we need to make
				// sure we properly keep track of the stack height, even after ret.
				if (ult->type != MONO_TYPE_VOID)
					--td->sp;
				break;
			}

			int vt_size = 0;
			if (mt != MINT_TYPE_VOID) {
				--td->sp;
				if (mt == MINT_TYPE_VT)
					vt_size = mono_class_value_size (mono_class_from_mono_type_internal (ult), NULL);
			}
			if (td->sp > td->stack) {
				mono_error_set_generic_error (error, "System", "InvalidProgramException", "");
				goto exit;
			}

			if (sym_seq_points) {
				last_seq_point = interp_add_ins (td, MINT_SDB_SEQ_POINT);
				td->last_ins->flags |= INTERP_INST_FLAG_SEQ_POINT_METHOD_EXIT;
			}

			guint16 exit_profiling = 0;
			if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
				exit_profiling |= TRACING_FLAG;
			if (rtm->prof_flags & MONO_PROFILER_CALL_INSTRUMENTATION_LEAVE)
				exit_profiling |= PROFILING_FLAG;
			if (exit_profiling) {
				/* This does the return as well */
				gboolean is_void = mt == MINT_TYPE_VOID;
				interp_add_ins (td, is_void ? MINT_PROF_EXIT_VOID : MINT_PROF_EXIT);
				td->last_ins->data [0] = exit_profiling;
				if (!is_void) {
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					WRITE32_INS (td->last_ins, 1, &vt_size);
				}
			} else {
				if (vt_size == 0) {
					if (mt == MINT_TYPE_VOID) {
						interp_add_ins (td, MINT_RET_VOID);
					} else {
						if (mt == MINT_TYPE_I1)
							interp_add_ins (td, MINT_RET_I1);
						else if (mt == MINT_TYPE_U1)
							interp_add_ins (td, MINT_RET_U1);
						else if (mt == MINT_TYPE_I2)
							interp_add_ins (td, MINT_RET_I2);
						else if (mt == MINT_TYPE_U2)
							interp_add_ins (td, MINT_RET_U2);
						else
							interp_add_ins (td, MINT_RET);
						interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					}
				} else {
					interp_add_ins (td, MINT_RET_VT);
					g_assert (vt_size < G_MAXUINT16);
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					td->last_ins->data [0] = GINT_TO_UINT16 (vt_size);
				}
			}
			++td->ip;
			break;
		}
		case CEE_BR: {
			int offset = read32 (td->ip + 1);
			if (offset) {
				handle_branch (td, MINT_BR, 5 + offset);
				link_bblocks = FALSE;
			}
			td->ip += 5;
			break;
		}
		case CEE_BR_S: {
			int offset = (gint8)td->ip [1];
			if (offset) {
				handle_branch (td, MINT_BR, 2 + (gint8)td->ip [1]);
				link_bblocks = FALSE;
			}
			td->ip += 2;
			break;
		}
		case CEE_BRFALSE:
			link_bblocks = one_arg_branch (td, MINT_BRFALSE_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BRFALSE_S:
			link_bblocks = one_arg_branch (td, MINT_BRFALSE_I4, (gint8)td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BRTRUE:
			link_bblocks = one_arg_branch (td, MINT_BRTRUE_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BRTRUE_S:
			link_bblocks = one_arg_branch (td, MINT_BRTRUE_I4, (gint8)td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BEQ:
			link_bblocks = two_arg_branch (td, MINT_BEQ_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BEQ_S:
			link_bblocks = two_arg_branch (td, MINT_BEQ_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGE:
			link_bblocks = two_arg_branch (td, MINT_BGE_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGE_S:
			link_bblocks = two_arg_branch (td, MINT_BGE_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGT:
			link_bblocks = two_arg_branch (td, MINT_BGT_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGT_S:
			link_bblocks = two_arg_branch (td, MINT_BGT_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLT:
			link_bblocks = two_arg_branch (td, MINT_BLT_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLT_S:
			link_bblocks = two_arg_branch (td, MINT_BLT_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLE:
			link_bblocks = two_arg_branch (td, MINT_BLE_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLE_S:
			link_bblocks = two_arg_branch (td, MINT_BLE_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BNE_UN:
			link_bblocks = two_arg_branch (td, MINT_BNE_UN_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BNE_UN_S:
			link_bblocks = two_arg_branch (td, MINT_BNE_UN_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGE_UN:
			link_bblocks = two_arg_branch (td, MINT_BGE_UN_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGE_UN_S:
			link_bblocks = two_arg_branch (td, MINT_BGE_UN_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGT_UN:
			link_bblocks = two_arg_branch (td, MINT_BGT_UN_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BGT_UN_S:
			link_bblocks = two_arg_branch (td, MINT_BGT_UN_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLE_UN:
			link_bblocks = two_arg_branch (td, MINT_BLE_UN_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLE_UN_S:
			link_bblocks = two_arg_branch (td, MINT_BLE_UN_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLT_UN:
			link_bblocks = two_arg_branch (td, MINT_BLT_UN_I4, read32 (td->ip + 1), 5);
			td->ip += 5;
			CHECK_FALLTHRU ();
			break;
		case CEE_BLT_UN_S:
			link_bblocks = two_arg_branch (td, MINT_BLT_UN_I4, (gint8) td->ip [1], 2);
			td->ip += 2;
			CHECK_FALLTHRU ();
			break;
		case CEE_SWITCH: {
			guint32 n;
			const unsigned char *next_ip;
			++td->ip;
			n = read32 (td->ip);
			interp_add_ins_explicit (td, MINT_SWITCH, MINT_SWITCH_LEN (n));
			WRITE32_INS (td->last_ins, 0, &n);
			td->ip += 4;
			next_ip = td->ip + n * 4;
			--td->sp;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			InterpBasicBlock **target_bb_table = (InterpBasicBlock**)mono_mempool_alloc0 (td->mempool, sizeof (InterpBasicBlock*) * n);
			for (guint32 i = 0; i < n; i++) {
				int offset = read32 (td->ip);
				ptrdiff_t target = next_ip - td->il_code + offset;
				InterpBasicBlock *target_bb = td->offset_to_bb [target];
				g_assert (target_bb);
				if (offset < 0) {
#if DEBUG_INTERP
					if (stack_height > 0 && stack_height != target_bb->stack_height)
						g_warning ("SWITCH with back branch and non-empty stack");
#endif
				} else {
					init_bb_stack_state (td, target_bb);
				}
				target_bb_table [i] = target_bb;
				interp_link_bblocks (td, td->cbb, target_bb);
				td->ip += 4;
			}
			td->last_ins->info.target_bb_table = target_bb_table;
			break;
		}
		case CEE_LDIND_I1:
			handle_ldind (td, MINT_LDIND_I1, STACK_TYPE_I4, &volatile_);
			break;
		case CEE_LDIND_U1:
			handle_ldind (td, MINT_LDIND_U1, STACK_TYPE_I4, &volatile_);
			break;
		case CEE_LDIND_I2:
			handle_ldind (td, MINT_LDIND_I2, STACK_TYPE_I4, &volatile_);
			break;
		case CEE_LDIND_U2:
			handle_ldind (td, MINT_LDIND_U2, STACK_TYPE_I4, &volatile_);
			break;
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
			handle_ldind (td, MINT_LDIND_I4, STACK_TYPE_I4, &volatile_);
			break;
		case CEE_LDIND_I8:
			handle_ldind (td, MINT_LDIND_I8, STACK_TYPE_I8, &volatile_);
			break;
		case CEE_LDIND_I:
			handle_ldind (td, MINT_LDIND_I, STACK_TYPE_I, &volatile_);
			break;
		case CEE_LDIND_R4:
			handle_ldind (td, MINT_LDIND_R4, STACK_TYPE_R4, &volatile_);
			break;
		case CEE_LDIND_R8:
			handle_ldind (td, MINT_LDIND_R8, STACK_TYPE_R8, &volatile_);
			break;
		case CEE_LDIND_REF:
			handle_ldind (td, MINT_LDIND_I, STACK_TYPE_O, &volatile_);
			break;
		case CEE_STIND_REF:
			handle_stind (td, MINT_STIND_REF, &volatile_);
			break;
		case CEE_STIND_I1:
			handle_stind (td, MINT_STIND_I1, &volatile_);
			break;
		case CEE_STIND_I2:
			handle_stind (td, MINT_STIND_I2, &volatile_);
			break;
		case CEE_STIND_I4:
			handle_stind (td, MINT_STIND_I4, &volatile_);
			break;
		case CEE_STIND_I:
			handle_stind (td, MINT_STIND_I, &volatile_);
			break;
		case CEE_STIND_I8:
			handle_stind (td, MINT_STIND_I8, &volatile_);
			break;
		case CEE_STIND_R4:
			handle_stind (td, MINT_STIND_R4, &volatile_);
			break;
		case CEE_STIND_R8:
			handle_stind (td, MINT_STIND_R8, &volatile_);
			break;
		case CEE_ADD:
			binary_arith_op(td, MINT_ADD_I4);
			++td->ip;
			break;
		case CEE_SUB:
			binary_arith_op(td, MINT_SUB_I4);
			++td->ip;
			break;
		case CEE_MUL:
			binary_arith_op(td, MINT_MUL_I4);
			++td->ip;
			break;
		case CEE_DIV:
			binary_arith_op(td, MINT_DIV_I4);
			++td->ip;
			break;
		case CEE_DIV_UN:
			binary_arith_op(td, MINT_DIV_UN_I4);
			++td->ip;
			break;
		case CEE_REM:
			binary_arith_op (td, MINT_REM_I4);
			++td->ip;
			break;
		case CEE_REM_UN:
			binary_arith_op (td, MINT_REM_UN_I4);
			++td->ip;
			break;
		case CEE_AND:
			binary_arith_op (td, MINT_AND_I4);
			++td->ip;
			break;
		case CEE_OR:
			binary_arith_op (td, MINT_OR_I4);
			++td->ip;
			break;
		case CEE_XOR:
			binary_arith_op (td, MINT_XOR_I4);
			++td->ip;
			break;
		case CEE_SHL:
			shift_op (td, MINT_SHL_I4);
			++td->ip;
			break;
		case CEE_SHR:
			shift_op (td, MINT_SHR_I4);
			++td->ip;
			break;
		case CEE_SHR_UN:
			shift_op (td, MINT_SHR_UN_I4);
			++td->ip;
			break;
		case CEE_NEG:
			unary_arith_op (td, MINT_NEG_I4);
			++td->ip;
			break;
		case CEE_NOT:
			unary_arith_op (td, MINT_NOT_I4);
			++td->ip;
			break;
		case CEE_CONV_U1:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_I1:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_U2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_I2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_U:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
#if SIZEOF_VOID_P == 4
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_U4_R8);
#else
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_U8_R8);
#endif
				break;
			case STACK_TYPE_R4:
#if SIZEOF_VOID_P == 4
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_U4_R4);
#else
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_U8_R4);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_I8_U4);
#endif
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_MOV_8);
#endif
				break;
			case STACK_TYPE_MP:
			case STACK_TYPE_O:
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_I:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
#if SIZEOF_VOID_P == 8
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_I8_R8);
#else
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_I4_R8);
#endif
				break;
			case STACK_TYPE_R4:
#if SIZEOF_VOID_P == 8
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_I8_R4);
#else
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_I4_R4);
#endif
				break;
			case STACK_TYPE_I4:
#if SIZEOF_VOID_P == 8
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_CONV_I8_I4);
#endif
				break;
			case STACK_TYPE_O:
			case STACK_TYPE_MP:
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I);
				break;
			case STACK_TYPE_I8:
#if SIZEOF_VOID_P == 4
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I, MINT_MOV_8);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_U4:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_U4_R8);
				break;
			case STACK_TYPE_I4:
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_MOV_8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 8
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_MOV_8);
#else
				SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_I4:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_I4_R8);
				break;
			case STACK_TYPE_I4:
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_MOV_8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 8
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_MOV_8);
#else
				SET_SIMPLE_TYPE (td->sp - 1, STACK_TYPE_I4);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_I8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_R8);
				break;
			case STACK_TYPE_I4: {
				if (interp_ins_is_ldc (td->last_ins) && td->last_ins == td->cbb->last_ins) {
					gint64 ct = interp_get_const_from_ldc_i4 (td->last_ins);
					interp_clear_ins (td->last_ins);

					interp_add_ins (td, MINT_LDC_I8);
					td->sp--;
					push_simple_type (td, STACK_TYPE_I8);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					WRITE64_INS (td->last_ins, 0, &ct);
				} else {
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_I4);
				}
				break;
			}
			case STACK_TYPE_I8:
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 4
				interp_add_ins (td, MINT_CONV_I8_I4);
#else
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_R4:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R4, MINT_CONV_R4_R8);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R4, MINT_CONV_R4_I8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R4, MINT_CONV_R4_I4);
				break;
			case STACK_TYPE_R4:
				/* no-op */
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_R8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_I8);
				break;
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
				break;
			case STACK_TYPE_R8:
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_U8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_I4:
				if (interp_ins_is_ldc (td->last_ins) && td->last_ins == td->cbb->last_ins) {
					gint64 ct = (guint32)interp_get_const_from_ldc_i4 (td->last_ins);
					interp_clear_ins (td->last_ins);

					interp_add_ins (td, MINT_LDC_I8);
					td->sp--;
					push_simple_type (td, STACK_TYPE_I8);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					WRITE64_INS (td->last_ins, 0, &ct);
				} else {
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_U4);
				}
				break;
			case STACK_TYPE_I8:
				break;
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_U8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_U8_R8);
				break;
			case STACK_TYPE_MP:
#if SIZEOF_VOID_P == 4
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_U4);
#else
				SET_SIMPLE_TYPE(td->sp - 1, STACK_TYPE_I8);
#endif
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CPOBJ: {
			CHECK_STACK (td, 2);

			token = read32 (td->ip + 1);
			klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
			goto_if_nok (error, exit);

			if (m_class_is_valuetype (klass)) {
				mt = mono_mint_type (m_class_get_byval_arg (klass));
				td->sp -= 2;
				if (mt == MINT_TYPE_VT && !m_class_has_references (klass)) {
					interp_add_ins (td, MINT_CPOBJ_VT_NOREF);
					td->last_ins->data [0] = GINT32_TO_UINT16 (mono_class_value_size (klass, NULL));
				} else {
					interp_add_ins (td, (mt == MINT_TYPE_VT) ? MINT_CPOBJ_VT : MINT_CPOBJ);
					td->last_ins->data [0] = get_data_item_index (td, klass);
				}
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			} else {
				td->sp--;
				interp_add_ins (td, MINT_LDIND_I);
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, STACK_TYPE_I);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

				td->sp -= 2;
				interp_add_ins (td, MINT_STIND_REF);
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
			}
			td->ip += 5;
			break;
		}
		case CEE_LDOBJ: {
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else {
				klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
				goto_if_nok (error, exit);
			}

			interp_emit_ldobj (td, klass);

			td->ip += 5;
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		}
		case CEE_LDSTR: {
			token = mono_metadata_token_index (read32 (td->ip + 1));
			push_type (td, STACK_TYPE_O, mono_defaults.string_class);
			if (method->wrapper_type == MONO_WRAPPER_NONE) {
				MonoString *s = mono_ldstr_checked (image, token, error);
				goto_if_nok (error, exit);
				/* GC won't scan code stream, but reference is held by metadata
				 * machinery so we are good here */
				interp_add_ins (td, MINT_LDSTR);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, s);
			} else if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				/* token is an index into the MonoDynamicMethod:method.method_data
				 * which comes from ReflectionMethodBuilder:refs.  See
				 * reflection_methodbuilder_to_mono_method.
				 *
				 * the actual data item is a managed MonoString from the managed DynamicMethod
				 */
				interp_add_ins (td, MINT_LDSTR_DYNAMIC);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, GUINT_TO_POINTER (token));
			} else {
				/* the token is an index into MonoWrapperMethod:method_data that
				 * stores a global or malloc'ed C string. defer MonoString
				 * allocation to execution-time */
				interp_add_ins (td, MINT_LDSTR_CSTR);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				const char *cstr = (const char*)mono_method_get_wrapper_data (method, token);
				td->last_ins->data [0] = get_data_item_index (td, (void*)cstr);
			}
			td->ip += 5;
			break;
		}
		case CEE_NEWOBJ: {
			MonoMethod *m;
			MonoMethodSignature *csignature;
			gboolean is_protected = is_ip_protected (header, GPTRDIFF_TO_INT (td->ip - header->code));

			td->ip++;
			token = read32 (td->ip);
			td->ip += 4;

			m = interp_get_method (method, token, image, generic_context, error);
			goto_if_nok (error, exit);

			csignature = mono_method_signature_internal (m);
			klass = m->klass;

			if (!mono_class_init_internal (klass)) {
				mono_error_set_for_class_failure (error, klass);
				goto_if_nok (error, exit);
			}
			MonoVTable *vtable = mono_class_vtable_checked (klass, error);
			goto_if_nok (error, exit);

			if (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_ABSTRACT) {
				char* full_name = mono_type_get_full_name (klass);
				mono_error_set_member_access (error, "Cannot create an abstract class: %s", full_name);
				g_free (full_name);
				goto_if_nok (error, exit);
			}

			int ret_mt = mono_mint_type (m_class_get_byval_arg (klass));
			if (klass == mono_defaults.int_class && csignature->param_count == 1) {
#if SIZEOF_VOID_P == 8
				if (td->sp [-1].type == STACK_TYPE_I4)
					interp_add_conv (td, td->sp - 1, NULL, stack_type [ret_mt], MINT_CONV_I8_I4);
#else
				if (td->sp [-1].type == STACK_TYPE_I8)
					interp_add_conv (td, td->sp - 1, NULL, stack_type [ret_mt], MINT_CONV_OVF_I4_I8);
#endif
			} else if (m_class_get_parent (klass) == mono_defaults.array_class) {
				int *call_args = (int*)mono_mempool_alloc (td->mempool, (csignature->param_count + 1) * sizeof (int));
				td->sp -= csignature->param_count;
				for (int i = 0; i < csignature->param_count; i++) {
					call_args [i] = td->sp [i].var;
				}
				call_args [csignature->param_count] = -1;

				interp_add_ins (td, MINT_NEWOBJ_ARRAY);
				td->last_ins->data [0] = get_data_item_index (td, m->klass);
				td->last_ins->data [1] = csignature->param_count;
				init_last_ins_call (td);
				if (!td->optimized)
					td->last_ins->info.call_info->call_offset = get_tos_offset (td);
				push_type (td, stack_type [ret_mt], klass);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
				td->last_ins->info.call_info->call_args = call_args;
			} else if (klass == mono_defaults.string_class) {
				if (!td->optimized) {
					int tos_offset = get_tos_offset (td);
					td->sp -= csignature->param_count;
					guint32 params_stack_size = tos_offset - get_tos_offset (td);

					td->cbb->contains_call_instruction = TRUE;
					interp_add_ins (td, MINT_NEWOBJ_STRING_UNOPT);
					td->last_ins->data [0] = get_data_item_index (td, mono_interp_get_imethod (m));
					td->last_ins->data [1] = GUINT32_TO_UINT16 (params_stack_size);
					push_type (td, stack_type [ret_mt], klass);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				} else {
					int *call_args = (int*)mono_mempool_alloc (td->mempool, (csignature->param_count + 2) * sizeof (int));
					td->sp -= csignature->param_count;

					// First arg is dummy var, it is null when passed to the ctor
					call_args [0] = interp_create_var (td, get_type_from_stack (stack_type [ret_mt], NULL));
					if (!td->disable_ssa) {
						// Make sure this arg is defined for SSA optimizations
						interp_add_ins (td, MINT_DEF);
					}
					td->last_ins->dreg = call_args [0];
					for (int i = 0; i < csignature->param_count; i++) {
						call_args [i + 1] = td->sp [i].var;
					}
					call_args [csignature->param_count + 1] = -1;

					td->cbb->contains_call_instruction = TRUE;
					interp_add_ins (td, MINT_NEWOBJ_STRING);
					td->last_ins->data [0] = get_data_item_index_imethod (td, mono_interp_get_imethod (m));
					push_type (td, stack_type [ret_mt], klass);

					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
					init_last_ins_call (td);
					td->last_ins->info.call_info->call_args = call_args;
				}
			} else if (m_class_get_image (klass) == mono_defaults.corlib &&
					(!strcmp (m_class_get_name (m->klass), "Span`1") ||
					!strcmp (m_class_get_name (m->klass), "ReadOnlySpan`1")) &&
					csignature->param_count == 2 &&
					csignature->params [0]->type == MONO_TYPE_PTR &&
					!type_has_references (mono_method_get_context (m)->class_inst->type_argv [0])) {
				/* ctor frequently used with ReadOnlySpan over static arrays */
				MONO_PROFILER_RAISE (inline_method, (td->rtm->method, m));
				interp_add_ins (td, MINT_INTRINS_SPAN_CTOR);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_type_vt (td, klass, mono_class_value_size (klass, NULL));
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			} else if (!td->optimized) {
				int tos = get_tos_offset (td);
				int param_offset, param_size;
				int param_count = csignature->param_count;
				if (param_count) {
					td->sp -= param_count;
					param_offset = td->sp [0].offset;
					param_size = tos - param_offset;
				} else {
					param_offset = tos;
					param_size = 0;
				}

				// Move params types in temporary buffer
				StackInfo *sp_params = (StackInfo*) mono_mempool_alloc (td->mempool, sizeof (StackInfo) * param_count);
				memcpy (sp_params, td->sp, sizeof (StackInfo) * param_count);

				gboolean is_vt = m_class_is_valuetype (klass);
				int vtsize = 0;
				// We conservatively ensure stack size with an additional 2 * MINT_STACK_SLOT_SIZE since we can do
				// at most 2 alignments
				const int padding = 2 * MINT_STACK_SLOT_SIZE;
				if (is_vt) {
					vtsize = mono_class_value_size (klass, NULL);
					vtsize = ALIGN_TO (vtsize, MINT_STACK_SLOT_SIZE);
					ENSURE_STACK_SIZE(td, (int)(tos + vtsize + MINT_STACK_SLOT_SIZE + padding));
					if (ret_mt == MINT_TYPE_VT)
						push_type_vt (td, klass, vtsize);
					else
						push_type (td, stack_type [ret_mt], klass);
				} else {
					ENSURE_STACK_SIZE(td, (int)(tos + 2 * MINT_STACK_SLOT_SIZE + padding));
					push_type (td, stack_type [ret_mt], klass);
				}

				int dreg = td->sp [-1].var;

				int call_offset = get_tos_offset (td);
				call_offset = ALIGN_TO (call_offset, MINT_STACK_ALIGNMENT);

				if (param_count) {
					if (td->vars [sp_params [0].var].simd) {
						// if first arg is simd, move all args at next aligned offset after this ptr
						interp_add_ins (td, MINT_MOV_STACK_UNOPT);
						td->last_ins->data [0] = GINT_TO_UINT16 (param_offset);
						td->last_ins->data [1] = GINT_TO_UINT16 (call_offset + MINT_SIMD_ALIGNMENT - param_offset);
						td->last_ins->data [2] = GINT_TO_UINT16 (param_size);
					} else {
						int realign_offset = call_offset + MINT_STACK_SLOT_SIZE - param_offset;
						// otherwise we move all args immediately after this ptr
						interp_add_ins (td, MINT_MOV_STACK_UNOPT);
						td->last_ins->data [0] = GINT_TO_UINT16 (param_offset);
						td->last_ins->data [1] = GINT_TO_UINT16 (realign_offset);
						td->last_ins->data [2] = GINT_TO_UINT16 (param_size);

						if ((realign_offset % MINT_SIMD_ALIGNMENT) != 0) {
							// the argument move broke the alignment of any potential simd arguments, realign
							interp_realign_simd_params (td, sp_params, param_count, realign_offset);
						}
					}
				}

				if (!mono_class_has_finalizer (klass) &&
					!m_class_has_weak_fields (klass)) {
					if (is_vt) {
						interp_add_ins (td, MINT_NEWOBJ_VT);
						td->last_ins->data [1] = GUINTPTR_TO_UINT16 (ALIGN_TO (vtsize, MINT_STACK_SLOT_SIZE));
					} else {
						interp_add_ins (td, MINT_NEWOBJ);
						td->last_ins->data [1] = get_data_item_index (td, vtable);
					}
				} else {
					interp_add_ins (td, MINT_NEWOBJ_SLOW);
					g_assert (!m_class_is_valuetype (klass));
				}
				td->last_ins->data [0] = get_data_item_index_imethod (td, mono_interp_get_imethod (m));
				interp_ins_set_dreg (td->last_ins, dreg);
				interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
				init_last_ins_call (td);
				td->last_ins->info.call_info->call_offset = call_offset;
			} else {
#ifdef INTERP_ENABLE_SIMD
				if ((mono_interp_opt & INTERP_OPT_SIMD) && interp_emit_simd_intrinsics (td, m, csignature, TRUE))
					break;
#endif
				td->sp -= csignature->param_count;

				// Move params types in temporary buffer
				StackInfo *sp_params = (StackInfo*) mono_mempool_alloc (td->mempool, sizeof (StackInfo) * csignature->param_count);
				memcpy (sp_params, td->sp, sizeof (StackInfo) * csignature->param_count);

				if (interp_inline_newobj (td, m, csignature, ret_mt, sp_params, is_protected))
					break;
				/* The constructor was not inlined, abort inlining of current method */
				if (!td->aggressive_inlining)
					INLINE_FAILURE;
				td->cbb->contains_call_instruction = TRUE;

				// Push the return value and `this` argument to the ctor
				gboolean is_vt = m_class_is_valuetype (klass);
				int vtsize = 0;
				if (is_vt) {
					vtsize = mono_class_value_size (klass, NULL);
					if (ret_mt == MINT_TYPE_VT)
						push_type_vt (td, klass, vtsize);
					else
						push_type (td, stack_type [ret_mt], klass);
					push_simple_type (td, STACK_TYPE_I);
				} else {
					push_type (td, stack_type [ret_mt], klass);
					push_type (td, stack_type [ret_mt], klass);
				}
				// Make sure this arg is defined for SSA optimizations
				interp_add_ins (td, MINT_DEF);
				td->last_ins->dreg = td->sp [-1].var;
				int dreg = td->sp [-2].var;

				// Push back the params to top of stack. The original vars are maintained.
				ensure_stack (td, csignature->param_count);
				memcpy (td->sp, sp_params, sizeof (StackInfo) * csignature->param_count);
				td->sp += csignature->param_count;

				if (!mono_class_has_finalizer (klass) &&
						!m_class_has_weak_fields (klass)) {
					if (is_vt) {
						interp_add_ins (td, MINT_NEWOBJ_VT);
						td->last_ins->data [1] = GUINTPTR_TO_UINT16 (ALIGN_TO (vtsize, MINT_STACK_SLOT_SIZE));
					} else {
						interp_add_ins (td, MINT_NEWOBJ);
						td->last_ins->data [1] = get_data_item_index (td, vtable);
					}
				} else {
					interp_add_ins (td, MINT_NEWOBJ_SLOW);
					g_assert (!m_class_is_valuetype (klass));
				}
				td->last_ins->data [0] = get_data_item_index_imethod (td, mono_interp_get_imethod (m));
				interp_ins_set_dreg (td->last_ins, dreg);
				interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);

				init_last_ins_call (td);
				if (is_protected)
					td->last_ins->flags |= INTERP_INST_FLAG_PROTECTED_NEWOBJ;
				// Parameters and this pointer are popped of the stack. The return value remains
				td->sp -= csignature->param_count + 1;
				 // Save the arguments for the call
				int *call_args = (int*) mono_mempool_alloc (td->mempool, (csignature->param_count + 2) * sizeof (int));
				for (int i = 0; i < csignature->param_count + 1; i++)
					call_args [i] = td->sp [i].var;
				call_args [csignature->param_count + 1] = -1;
				td->last_ins->info.call_info->call_args = call_args;
			}
			break;
		}
		case CEE_CASTCLASS:
		case CEE_ISINST: {
			gboolean isinst_instr = *td->ip == CEE_ISINST;
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			interp_handle_isinst (td, klass, isinst_instr);
			break;
		}
		case CEE_CONV_R_UN:
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
				break;
			case STACK_TYPE_R8:
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R_UN_I8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R_UN_I4);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_UNBOX:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else {
				klass = mono_class_get_and_inflate_typespec_checked (image, token, generic_context, error);
				goto_if_nok (error, exit);
			}

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method;
				if (m_class_is_enumtype (mono_class_get_nullable_param_internal (klass)))
					target_method = mono_class_get_method_from_name_checked (klass, "UnboxExact", 1, 0, error);
				else
					target_method = mono_class_get_method_from_name_checked (klass, "Unbox", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, target_method, generic_context, NULL, FALSE, error, FALSE, FALSE, FALSE))
					goto exit;
				/*
				 * CEE_UNBOX needs to push address of vtype while Nullable.Unbox returns the value type
				 * We create a local variable in the frame so that we can fetch its address.
				 */
				int local = interp_create_var (td, m_class_get_byval_arg (klass));
				store_local (td, local);

				interp_add_ins (td, MINT_LDLOCA_S);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				interp_ins_set_sreg (td->last_ins, local);
				td->vars [local].indirects++;
			} else {
				interp_add_ins (td, MINT_UNBOX);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, klass);
				td->ip += 5;
			}
			break;
		case CEE_UNBOX_ANY:
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			// Common in generic code:
			// box T + unbox.any T -> nop
			if ((td->last_ins->opcode == MINT_BOX || td->last_ins->opcode == MINT_BOX_VT) &&
					(td->sp - 1)->klass == klass && td->last_ins == td->cbb->last_ins) {
				interp_clear_ins (td->last_ins);
				mt = mono_mint_type (m_class_get_byval_arg (klass));
				td->sp--;
				// Push back the original value that was boxed. We should handle this in CEE_BOX instead
				if (mt == MINT_TYPE_VT)
					push_type_vt (td, klass, mono_class_value_size (klass, NULL));
				else
					push_type (td, stack_type [mt], klass);
				// FIXME do this somewhere else, maybe in super instruction pass, where we would check
				// instruction patterns
				// Restore the local that is on top of the stack
				td->sp [-1].var = td->last_ins->sregs [0];
				td->ip += 5;
				break;
			}

			if (mini_type_is_reference (m_class_get_byval_arg (klass))) {
				interp_handle_isinst (td, klass, FALSE);
			} else if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method;
				if (m_class_is_enumtype (mono_class_get_nullable_param_internal (klass)))
					target_method = mono_class_get_method_from_name_checked (klass, "UnboxExact", 1, 0, error);
				else
					target_method = mono_class_get_method_from_name_checked (klass, "Unbox", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, target_method, generic_context, NULL, FALSE, error, FALSE, FALSE, FALSE))
					goto exit;
			} else {
				interp_add_ins (td, MINT_UNBOX);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, klass);

				interp_emit_ldobj (td, klass);

				td->ip += 5;
			}

			break;
		case CEE_THROW:
			if (!td->aggressive_inlining)
				INLINE_FAILURE;
			if (!inlining) {
				guint32 il_offset = GINT_TO_UINT32(td->current_il_offset);
				for (unsigned int i = 0; i < td->header->num_clauses; i++) {
					MonoExceptionClause *clause = &td->header->clauses [i];
					// If we throw during try and then catch we don't have the bblocks
					// properly linked, just disable ssa for now
					if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE && (clause->try_offset <= il_offset) && (il_offset < (clause->try_offset + clause->try_len)))
						td->disable_ssa = TRUE;
				}
			}
			CHECK_STACK (td, 1);
			interp_add_ins (td, MINT_THROW);
			interp_ins_set_sreg (td->last_ins, td->sp [-1].var);
			link_bblocks = FALSE;
			td->sp = td->stack;
			++td->ip;
			break;
		case CEE_LDFLDA: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init_internal (klass);
			mono_class_setup_fields (klass);
			{
				if (is_static) {
					td->sp--;
					interp_emit_ldsflda (td, field, error);
					goto_if_nok (error, exit);
				} else {
					if (G_UNLIKELY (m_field_is_from_update (field))) {
						/* metadata-update: can't add byref fields */
						g_assert (!m_type_is_byref (ftype));
						interp_emit_metadata_update_ldflda (td, field, error);
						goto_if_nok (error, exit);
						td->ip += 5;
						break;
					}
					td->sp--;
					int foffset = m_class_is_valuetype (klass) ? m_field_get_offset (field) - MONO_ABI_SIZEOF (MonoObject) : m_field_get_offset (field);
					if (td->sp->type == STACK_TYPE_O || td->sp->type == STACK_TYPE_I) {
						interp_add_ins (td, MINT_LDFLDA);
						td->last_ins->data [0] = GINT_TO_UINT16 (foffset);
					} else {
						int sp_type = td->sp->type;
						g_assert (sp_type == STACK_TYPE_MP);
						if (foffset) {
							interp_add_ins (td, MINT_LDFLDA_UNSAFE);
							td->last_ins->data [0] = GINT_TO_UINT16 (foffset);
						} else {
							interp_add_ins (td, MINT_MOV_P);
						}
					}
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					push_simple_type (td, STACK_TYPE_MP);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				}
				td->ip += 5;
			}
			break;
		}
		case CEE_LDFLD: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			mono_class_init_internal (klass);
			mono_class_setup_fields (klass);

			MonoClass *field_klass = mono_class_from_mono_type_internal (ftype);
			mt = mono_mint_type (ftype);
			int field_size = mono_class_value_size (field_klass, NULL);

			{
				if (is_static) {
					td->sp--;
					interp_emit_sfld_access (td, field, field_klass, mt, TRUE, error);
					goto_if_nok (error, exit);
				} else if (td->sp [-1].type == STACK_TYPE_VT) {
					/* metadata-update: can't add fields to structs */
					g_assert (!m_field_is_from_update (field));
					int size = 0;
					/* First we pop the vt object from the stack. Then we push the field */
#ifdef NO_UNALIGNED_ACCESS
					if (m_field_get_offset (field) % SIZEOF_VOID_P != 0) {
						if (mt == MINT_TYPE_I8 || mt == MINT_TYPE_R8)
							size = 8;
					}
#endif
					interp_add_ins (td, MINT_MOV_SRC_OFF);
					g_assert (m_class_is_valuetype (klass));
					td->sp--;
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					td->last_ins->data [0] = GINT_TO_UINT16 (m_field_get_offset (field) - MONO_ABI_SIZEOF (MonoObject));
					td->last_ins->data [1] = GINT_TO_UINT16 (mt);
					if (mt == MINT_TYPE_VT)
						size = field_size;
					td->last_ins->data [2] = GINT_TO_UINT16 (size);

					if (mt == MINT_TYPE_VT)
						push_type_vt (td, field_klass, field_size);
					else
						push_type (td, stack_type [mt], field_klass);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				} else {
					if (G_UNLIKELY (m_field_is_from_update (field))) {
						g_assert (!m_type_is_byref (ftype));
						MonoClass *field_class = mono_class_from_mono_type_internal (ftype);
						interp_emit_metadata_update_ldflda (td, field, error);
						goto_if_nok (error, exit);
						interp_emit_ldobj (td, field_class);
						td->ip += 5;
						BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
						break;
					}
					int opcode = MINT_LDFLD_I1 + mt - MINT_TYPE_I1;
#ifdef NO_UNALIGNED_ACCESS
					if ((mt == MINT_TYPE_I8 || mt == MINT_TYPE_R8) && field->offset % SIZEOF_VOID_P != 0)
						opcode = get_unaligned_opcode (opcode);
#endif
					interp_add_ins (td, opcode);
					td->sp--;
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					td->last_ins->data [0] = GINT_TO_UINT16 (m_class_is_valuetype (klass) ? m_field_get_offset (field) - MONO_ABI_SIZEOF (MonoObject) : m_field_get_offset (field));
					if (mt == MINT_TYPE_VT) {
						int size = mono_class_value_size (field_klass, NULL);
						g_assert (size < G_MAXUINT16);
						td->last_ins->data [1] = GINT_TO_UINT16 (size);
					}
					if (mt == MINT_TYPE_VT)
						push_type_vt (td, field_klass, field_size);
					else
						push_type (td, stack_type [mt], field_klass);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				}
			}
			td->ip += 5;
			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_ACQ);
			break;
		}
		case CEE_STFLD: {
			CHECK_STACK (td, 2);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			gboolean is_static = !!(ftype->attrs & FIELD_ATTRIBUTE_STATIC);
			MonoClass *field_klass = mono_class_from_mono_type_internal (ftype);
			mono_class_init_internal (klass);
			mono_class_setup_fields (klass);
			mt = mono_mint_type (ftype);

			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);

			{
				if (is_static) {
					interp_emit_sfld_access (td, field, field_klass, mt, FALSE, error);
					goto_if_nok (error, exit);

					/* pop the unused object reference */
					td->sp--;

					/* the vtable of the field might not be initialized at this point */
					mono_class_vtable_checked (field_klass, error);
					goto_if_nok (error, exit);
				} else {
					if (G_UNLIKELY (m_field_is_from_update (field))) {
						// metadata-update: Can't add byref fields
						g_assert (!m_type_is_byref (ftype));
						MonoClass *field_class = mono_class_from_mono_type_internal (ftype);
						MonoType *local_type = m_class_get_byval_arg (field_class);
						int local = interp_create_var (td, local_type);
						store_local (td, local);
						interp_emit_metadata_update_ldflda (td, field, error);
						goto_if_nok (error, exit);
						load_local (td, local);
						interp_emit_stobj (td, field_class, FALSE);
						td->ip += 5;
						break;
					}
					int opcode = MINT_STFLD_I1 + mt - MINT_TYPE_I1;
#ifdef NO_UNALIGNED_ACCESS
					if ((mt == MINT_TYPE_I8 || mt == MINT_TYPE_R8) && field->offset % SIZEOF_VOID_P != 0)
						opcode = get_unaligned_opcode (opcode);
#endif
					interp_add_ins (td, opcode);
					td->sp -= 2;
					interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
					td->last_ins->data [0] = GINT_TO_UINT16 (m_class_is_valuetype (klass) ? m_field_get_offset (field) - MONO_ABI_SIZEOF (MonoObject) : m_field_get_offset (field));
					if (mt == MINT_TYPE_VT) {
						/* the vtable of the field might not be initialized at this point */
						mono_class_vtable_checked (field_klass, error);
						goto_if_nok (error, exit);
						if (m_class_has_references (field_klass)) {
							td->last_ins->data [1] = get_data_item_index (td, field_klass);
						} else {
							td->last_ins->opcode = MINT_STFLD_VT_NOREF;
							td->last_ins->data [1] = GINT32_TO_UINT16 (mono_class_value_size (field_klass, NULL));
						}
					}
				}
			}
			td->ip += 5;
			break;
		}
		case CEE_LDSFLDA: {
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			interp_emit_ldsflda (td, field, error);
			goto_if_nok (error, exit);
			td->ip += 5;
			break;
		}
		case CEE_LDSFLD: {
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			mt = mono_mint_type (ftype);
			klass = mono_class_from_mono_type_internal (ftype);
			gboolean in_corlib = m_class_get_image (m_field_get_parent (field)) == mono_defaults.corlib;

			if (in_corlib && !strcmp (field->name, "IsLittleEndian") &&
				!strcmp (m_class_get_name (m_field_get_parent (field)), "BitConverter") &&
				!strcmp (m_class_get_name_space (m_field_get_parent (field)), "System"))
			{
				interp_add_ins (td, (TARGET_BYTE_ORDER == G_LITTLE_ENDIAN) ? MINT_LDC_I4_1 : MINT_LDC_I4_0);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 5;
				break;
			}

			interp_emit_sfld_access (td, field, klass, mt, TRUE, error);
			goto_if_nok (error, exit);

			td->ip += 5;
			break;
		}
		case CEE_STSFLD: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			field = interp_field_from_token (method, token, &klass, generic_context, error);
			goto_if_nok (error, exit);
			MonoType *ftype = mono_field_get_type_internal (field);
			mt = mono_mint_type (ftype);

			emit_convert (td, td->sp - 1, ftype);

			/* the vtable of the field might not be initialized at this point */
			MonoClass *fld_klass = mono_class_from_mono_type_internal (ftype);
			mono_class_vtable_checked (fld_klass, error);
			goto_if_nok (error, exit);

			interp_emit_sfld_access (td, field, fld_klass, mt, FALSE, error);
			goto_if_nok (error, exit);

			td->ip += 5;
			break;
		}
		case CEE_STOBJ: {
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);

			interp_emit_stobj (td, klass, FALSE);

			td->ip += 5;
			break;
		}
#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_I_UN:
#endif
		case CEE_CONV_OVF_I8_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_I8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_I8_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_U4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_I8_U8);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			++td->ip;
			break;
#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_U_UN:
#endif
		case CEE_CONV_OVF_U8_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_U8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_U8_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_U4);
				break;
			case STACK_TYPE_I8:
				break;
			default:
				g_assert_not_reached ();
				break;
			}
			++td->ip;
			break;
		case CEE_BOX: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (mono_class_is_nullable (klass)) {
				MonoMethod *target_method = mono_class_get_method_from_name_checked (klass, "Box", 1, 0, error);
				goto_if_nok (error, exit);
				/* td->ip is incremented by interp_transform_call */
				if (!interp_transform_call (td, method, target_method, generic_context, NULL, FALSE, error, FALSE, FALSE, FALSE))
					goto exit;
			} else if (!m_class_is_valuetype (klass)) {
				/* already boxed, do nothing. */
				td->ip += 5;
			} else {
				if (G_UNLIKELY (m_class_is_byreflike (klass))) {
					mono_error_set_bad_image (error, image, "Cannot box IsByRefLike type '%s.%s'", m_class_get_name_space (klass), m_class_get_name (klass));
					goto exit;
				}

				const gboolean vt = mono_mint_type (m_class_get_byval_arg (klass)) == MINT_TYPE_VT;

				if (td->sp [-1].type == STACK_TYPE_R8 && m_class_get_byval_arg (klass)->type == MONO_TYPE_R4)
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R4, MINT_CONV_R4_R8);
				MonoVTable *vtable = mono_class_vtable_checked (klass, error);
				goto_if_nok (error, exit);

				td->sp--;
				interp_add_ins (td, vt ? MINT_BOX_VT : MINT_BOX);
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				td->last_ins->data [0] = get_data_item_index (td, vtable);
				push_type (td, STACK_TYPE_O, klass);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 5;
			}

			break;
		}
		case CEE_NEWARR: {
			CHECK_STACK (td, 1);
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			MonoClass *array_class = mono_class_create_array (klass, 1);
			MonoVTable *vtable = mono_class_vtable_checked (array_class, error);
			goto_if_nok (error, exit);

			unsigned char lentype = (td->sp - 1)->type;
			if (lentype == STACK_TYPE_I8) {
				/* mimic mini behaviour */
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_I8);
			} else {
				g_assert (lentype == STACK_TYPE_I4);
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_I4);
			}
			td->sp--;
			interp_add_ins (td, MINT_NEWARR);
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_type (td, STACK_TYPE_O, array_class);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->last_ins->data [0] = get_data_item_index (td, vtable);
			td->ip += 5;
			break;
		}
		case CEE_LDLEN:
			CHECK_STACK (td, 1);
			td->sp--;
			interp_add_ins (td, MINT_LDLEN);
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_simple_type (td, STACK_TYPE_I4);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			++td->ip;
			break;
		case CEE_LDELEMA: {
			gint32 size;
			CHECK_STACK (td, 2);
			ENSURE_I4 (td, 1);
			token = read32 (td->ip + 1);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *) mono_method_get_wrapper_data (method, token);
			else
				klass = mini_get_class (method, token, generic_context);

			CHECK_TYPELOAD (klass);

			if (!m_class_is_valuetype (klass) && method->wrapper_type == MONO_WRAPPER_NONE && !readonly) {
				/*
				 * Check the class for failures before the type check, which can
				 * throw other exceptions.
				 */
				mono_class_setup_vtable (klass);
				CHECK_TYPELOAD (klass);
				interp_add_ins (td, MINT_LDELEMA_TC);
				td->sp -= 2;
				int *call_args = (int*)mono_mempool_alloc (td->mempool, 3 * sizeof (int));
				call_args [0] = td->sp [0].var;
				call_args [1] = td->sp [1].var;
				call_args [2] = -1;
				init_last_ins_call (td);
				if (!td->optimized)
					td->last_ins->info.call_info->call_offset = get_tos_offset (td);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, klass);
				td->last_ins->info.call_info->call_args = call_args;
				interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
			} else {
				interp_add_ins (td, MINT_LDELEMA1);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				mono_class_init_internal (klass);
				size = mono_class_array_element_size (klass);
				td->last_ins->data [0] = GINT32_TO_UINT16 (size);
			}

			readonly = FALSE;

			td->ip += 5;
			break;
		}
		case CEE_LDELEM_I1:
			handle_ldelem (td, MINT_LDELEM_I1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_U1:
			handle_ldelem (td, MINT_LDELEM_U1, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_I2:
			handle_ldelem (td, MINT_LDELEM_I2, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_U2:
			handle_ldelem (td, MINT_LDELEM_U2, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_I4:
			handle_ldelem (td, MINT_LDELEM_I4, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_U4:
			handle_ldelem (td, MINT_LDELEM_U4, STACK_TYPE_I4);
			break;
		case CEE_LDELEM_I8:
			handle_ldelem (td, MINT_LDELEM_I8, STACK_TYPE_I8);
			break;
		case CEE_LDELEM_I:
			handle_ldelem (td, MINT_LDELEM_I, STACK_TYPE_I);
			break;
		case CEE_LDELEM_R4:
			handle_ldelem (td, MINT_LDELEM_R4, STACK_TYPE_R4);
			break;
		case CEE_LDELEM_R8:
			handle_ldelem (td, MINT_LDELEM_R8, STACK_TYPE_R8);
			break;
		case CEE_LDELEM_REF:
			handle_ldelem (td, MINT_LDELEM_REF, STACK_TYPE_O);
			break;
		case CEE_LDELEM:
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			switch (mono_mint_type (m_class_get_byval_arg (klass))) {
				case MINT_TYPE_I1:
					handle_ldelem (td, MINT_LDELEM_I1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_U1:
					handle_ldelem (td, MINT_LDELEM_U1, STACK_TYPE_I4);
					break;
				case MINT_TYPE_U2:
					handle_ldelem (td, MINT_LDELEM_U2, STACK_TYPE_I4);
					break;
				case MINT_TYPE_I2:
					handle_ldelem (td, MINT_LDELEM_I2, STACK_TYPE_I4);
					break;
				case MINT_TYPE_I4:
					handle_ldelem (td, MINT_LDELEM_I4, STACK_TYPE_I4);
					break;
				case MINT_TYPE_I8:
					handle_ldelem (td, MINT_LDELEM_I8, STACK_TYPE_I8);
					break;
				case MINT_TYPE_R4:
					handle_ldelem (td, MINT_LDELEM_R4, STACK_TYPE_R4);
					break;
				case MINT_TYPE_R8:
					handle_ldelem (td, MINT_LDELEM_R8, STACK_TYPE_R8);
					break;
				case MINT_TYPE_O:
					handle_ldelem (td, MINT_LDELEM_REF, STACK_TYPE_O);
					break;
				case MINT_TYPE_VT: {
					int size = mono_class_value_size (klass, NULL);
					g_assert (size < G_MAXUINT16);

					CHECK_STACK (td, 2);
					ENSURE_I4 (td, 1);
					interp_add_ins (td, MINT_LDELEM_VT);
					td->sp -= 2;
					interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
					push_type_vt (td, klass, size);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					td->last_ins->data [0] = GINT_TO_UINT16 (size);
					++td->ip;
					break;
				}
				default: {
					GString *res = g_string_new ("");
					mono_type_get_desc (res, m_class_get_byval_arg (klass), TRUE);
					g_print ("LDELEM: %s -> %d (%s)\n", m_class_get_name (klass), mono_mint_type (m_class_get_byval_arg (klass)), res->str);
					g_string_free (res, TRUE);
					g_assert (0);
					break;
				}
			}
			td->ip += 4;
			break;
		case CEE_STELEM_I:
			handle_stelem (td, MINT_STELEM_I);
			break;
		case CEE_STELEM_I1:
			handle_stelem (td, MINT_STELEM_I1);
			break;
		case CEE_STELEM_I2:
			handle_stelem (td, MINT_STELEM_I2);
			break;
		case CEE_STELEM_I4:
			handle_stelem (td, MINT_STELEM_I4);
			break;
		case CEE_STELEM_I8:
			handle_stelem (td, MINT_STELEM_I8);
			break;
		case CEE_STELEM_R4:
			handle_stelem (td, MINT_STELEM_R4);
			break;
		case CEE_STELEM_R8:
			handle_stelem (td, MINT_STELEM_R8);
			break;
		case CEE_STELEM_REF:
			handle_stelem (td, MINT_STELEM_REF);
			break;
		case CEE_STELEM:
			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			switch (mono_mint_type (m_class_get_byval_arg (klass))) {
				case MINT_TYPE_I1:
					handle_stelem (td, MINT_STELEM_I1);
					break;
				case MINT_TYPE_U1:
					handle_stelem (td, MINT_STELEM_U1);
					break;
				case MINT_TYPE_I2:
					handle_stelem (td, MINT_STELEM_I2);
					break;
				case MINT_TYPE_U2:
					handle_stelem (td, MINT_STELEM_U2);
					break;
				case MINT_TYPE_I4:
					handle_stelem (td, MINT_STELEM_I4);
					break;
				case MINT_TYPE_I8:
					handle_stelem (td, MINT_STELEM_I8);
					break;
				case MINT_TYPE_R4:
					handle_stelem (td, MINT_STELEM_R4);
					break;
				case MINT_TYPE_R8:
					handle_stelem (td, MINT_STELEM_R8);
					break;
				case MINT_TYPE_O:
					handle_stelem (td, MINT_STELEM_REF);
					break;
				case MINT_TYPE_VT: {
					int size = mono_class_value_size (klass, NULL);
					g_assert (size < G_MAXUINT16);

					handle_stelem (td, m_class_has_references (klass) ? MINT_STELEM_VT : MINT_STELEM_VT_NOREF);
					td->last_ins->data [0] = get_data_item_index (td, klass);
					td->last_ins->data [1] = GINT_TO_UINT16 (size);
					break;
				}
				default: {
					GString *res = g_string_new ("");
					mono_type_get_desc (res, m_class_get_byval_arg (klass), TRUE);
					g_print ("STELEM: %s -> %d (%s)\n", m_class_get_name (klass), mono_mint_type (m_class_get_byval_arg (klass)), res->str);
					g_string_free (res, TRUE);
					g_assert (0);
					break;
				}
			}
			td->ip += 4;
			break;
		case CEE_CKFINITE: {
			CHECK_STACK (td, 1);
			int ckfinite_stack_type = td->sp [-1].type;
			switch (ckfinite_stack_type) {
				case STACK_TYPE_R4: interp_add_ins (td, MINT_CKFINITE_R4); break;
				case STACK_TYPE_R8: interp_add_ins (td, MINT_CKFINITE_R8); break;
				default:
					g_error ("Invalid stack type");
			}
			td->sp--;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_simple_type (td, ckfinite_stack_type);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			++td->ip;
			break;
		}
		case CEE_MKREFANY:
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			interp_add_ins (td, MINT_MKREFANY);
			td->sp--;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_type_vt (td, mono_defaults.typed_reference_class, sizeof (MonoTypedRef));
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->last_ins->data [0] = get_data_item_index (td, klass);

			td->ip += 5;
			break;
		case CEE_REFANYVAL: {
			CHECK_STACK (td, 1);

			token = read32 (td->ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			interp_add_ins (td, MINT_REFANYVAL);
			td->sp--;
			interp_ins_set_sreg (td->last_ins, td->sp [0].var);
			push_simple_type (td, STACK_TYPE_MP);
			interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
			td->last_ins->data [0] = get_data_item_index (td, klass);

			td->ip += 5;
			break;
		}
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_I1_UN: {
			gboolean is_un = *td->ip == CEE_CONV_OVF_I1_UN;
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, is_un ? MINT_CONV_OVF_I1_U4 : MINT_CONV_OVF_I1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, is_un ? MINT_CONV_OVF_I1_U8 : MINT_CONV_OVF_I1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		}
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_U1_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U1_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U1_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U1_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U1_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_I2_UN: {
			gboolean is_un = *td->ip == CEE_CONV_OVF_I2_UN;
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, is_un ? MINT_CONV_OVF_I2_U4 : MINT_CONV_OVF_I2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, is_un ? MINT_CONV_OVF_I2_U8 : MINT_CONV_OVF_I2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		}
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U2:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U2_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U2_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U2_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U2_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
#if SIZEOF_VOID_P == 4
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_I_UN:
#endif
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_I4_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I4_R8);
				break;
			case STACK_TYPE_I4:
				if (*td->ip == CEE_CONV_OVF_I4_UN || *td->ip == CEE_CONV_OVF_I_UN)
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I4_U4);
				break;
			case STACK_TYPE_I8:
				if (*td->ip == CEE_CONV_OVF_I4_UN || *td->ip == CEE_CONV_OVF_I_UN)
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I4_U8);
				else
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_I4_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
#if SIZEOF_VOID_P == 4
		case CEE_CONV_OVF_U:
		case CEE_CONV_OVF_U_UN:
#endif
		case CEE_CONV_OVF_U4:
		case CEE_CONV_OVF_U4_UN:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_R8);
				break;
			case STACK_TYPE_I4:
				if (*td->ip == CEE_CONV_OVF_U4 || *td->ip == CEE_CONV_OVF_U)
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_I8);
				break;
			case STACK_TYPE_MP:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_CONV_OVF_U4_P);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_I:
#endif
		case CEE_CONV_OVF_I8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_I8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_I8_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_I8_I4);
				break;
			case STACK_TYPE_I8:
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
#if SIZEOF_VOID_P == 8
		case CEE_CONV_OVF_U:
#endif
		case CEE_CONV_OVF_U8:
			CHECK_STACK (td, 1);
			switch (td->sp [-1].type) {
			case STACK_TYPE_R4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_U8_R4);
				break;
			case STACK_TYPE_R8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_U8_R8);
				break;
			case STACK_TYPE_I4:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_U8_I4);
				break;
			case STACK_TYPE_I8:
				interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I8, MINT_CONV_OVF_U8_I8);
				break;
			default:
				g_assert_not_reached ();
			}
			++td->ip;
			break;
		case CEE_LDTOKEN: {
			int size;
			gpointer handle;
			token = read32 (td->ip + 1);
			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD || method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED) {
				handle = mono_method_get_wrapper_data (method, token);
				klass = (MonoClass *) mono_method_get_wrapper_data (method, token + 1);
				if (klass == mono_defaults.typehandle_class)
					handle = m_class_get_byval_arg ((MonoClass *) handle);

				if (generic_context) {
					handle = mono_class_inflate_generic_type_checked ((MonoType*)handle, generic_context, error);
					goto_if_nok (error, exit);
				}
			} else {
				handle = mono_ldtoken_checked (image, token, &klass, generic_context, error);
				goto_if_nok (error, exit);
			}
			mono_class_init_internal (klass);
			mt = mono_mint_type (m_class_get_byval_arg (klass));
			g_assert (mt == MINT_TYPE_VT);
			size = mono_class_value_size (klass, NULL);
			g_assert (size == sizeof(gpointer));

			const unsigned char *next_ip = td->ip + 5;
			MonoMethod *cmethod;
			if (next_ip < end &&
					interp_ip_in_cbb (td, GPTRDIFF_TO_INT (next_ip - td->il_code)) &&
					(*next_ip == CEE_CALL || *next_ip == CEE_CALLVIRT) &&
					(cmethod = interp_get_method (method, read32 (next_ip + 1), image, generic_context, error)) &&
					(cmethod->klass == mono_defaults.systemtype_class) &&
					(strcmp (cmethod->name, "GetTypeFromHandle") == 0)) {
				const unsigned char *next_next_ip = next_ip + 5;
				MonoMethod *next_cmethod;
				MonoClass *tclass = mono_class_from_mono_type_internal ((MonoType *)handle);
				// Optimize to true/false if next instruction is `call instance bool Type::get_IsValueType()`
				if (next_next_ip < end &&
						interp_ip_in_cbb (td, GPTRDIFF_TO_INT (next_next_ip - td->il_code)) &&
						(*next_next_ip == CEE_CALL || *next_next_ip == CEE_CALLVIRT) &&
						(next_cmethod = interp_get_method (method, read32 (next_next_ip + 1), image, generic_context, error)) &&
						(next_cmethod->klass == mono_defaults.systemtype_class) &&
						!strcmp (next_cmethod->name, "get_IsValueType")) {
					g_assert (!mono_class_is_open_constructed_type (m_class_get_byval_arg (tclass)));
					if (m_class_is_valuetype (tclass))
						interp_add_ins (td, MINT_LDC_I4_1);
					else
						interp_add_ins (td, MINT_LDC_I4_0);
					push_simple_type (td, STACK_TYPE_I4);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					td->ip = next_next_ip + 5;
					break;
				}

				interp_add_ins (td, MINT_LDPTR);
				gpointer systype = mono_type_get_object_checked ((MonoType*)handle, error);
				goto_if_nok (error, exit);
				push_type (td, STACK_TYPE_O, mono_defaults.runtimetype_class);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, systype);
				td->ip = next_ip + 5;
			} else {
				interp_add_ins (td, MINT_LDPTR);
				push_type_vt (td, klass, sizeof (gpointer));
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, handle);
				td->ip += 5;
			}

			break;
		}
		case CEE_ADD_OVF:
			binary_arith_op(td, MINT_ADD_OVF_I4);
			++td->ip;
			break;
		case CEE_ADD_OVF_UN:
			binary_arith_op(td, MINT_ADD_OVF_UN_I4);
			++td->ip;
			break;
		case CEE_MUL_OVF:
			binary_arith_op(td, MINT_MUL_OVF_I4);
			++td->ip;
			break;
		case CEE_MUL_OVF_UN:
			binary_arith_op(td, MINT_MUL_OVF_UN_I4);
			++td->ip;
			break;
		case CEE_SUB_OVF:
			binary_arith_op(td, MINT_SUB_OVF_I4);
			++td->ip;
			break;
		case CEE_SUB_OVF_UN:
			binary_arith_op(td, MINT_SUB_OVF_UN_I4);
			++td->ip;
			break;
		case CEE_ENDFINALLY: {
			int clause_index = td->clause_indexes [in_offset];
			MonoExceptionClause *clause = (clause_index != -1) ? (header->clauses + clause_index) : NULL;
			if (!clause || (clause->flags != MONO_EXCEPTION_CLAUSE_FINALLY && clause->flags != MONO_EXCEPTION_CLAUSE_FAULT)) {
				mono_error_set_generic_error (error, "System", "InvalidProgramException", "");
				goto exit;
			}
			td->sp = td->stack;
			interp_add_ins (td, MINT_ENDFINALLY);
			td->last_ins->data [0] = GINT_TO_UINT16 (clause_index);
			link_bblocks = FALSE;
			++td->ip;
			break;
		}
		case CEE_LEAVE:
		case CEE_LEAVE_S: {
			int target_offset;

			if (*td->ip == CEE_LEAVE)
				target_offset = 5 + read32 (td->ip + 1);
			else
				target_offset = 2 + (gint8)td->ip [1];

			td->sp = td->stack;

			g_assert (header->num_clauses < G_MAXUINT16);
			for (guint16 i = 0; i < header->num_clauses; ++i) {
				MonoExceptionClause *clause = &header->clauses [i];
				if (clause->flags != MONO_EXCEPTION_CLAUSE_FINALLY)
					continue;
				if (MONO_OFFSET_IN_CLAUSE (clause, GPTRDIFF_TO_UINT32(td->ip - header->code)) &&
						(!MONO_OFFSET_IN_CLAUSE (clause, GINT_TO_UINT32(target_offset + in_offset)))) {
					handle_branch (td, MINT_CALL_HANDLER, clause->handler_offset - in_offset);
					td->last_ins->data [2] = i;

					if (mono_interp_tiering_enabled ()) {
						// In the optimized method we will remember the native_offset of this bb_index
						// In the unoptimized method we will have to maintain the mapping between the
						// native offset of this bb and its bb_index
						td->patchpoint_data_n++;
						interp_add_ins (td, MINT_TIER_PATCHPOINT_DATA);
						call_handler_count++;
						td->last_ins->data [0] = GINT_TO_UINT16 (call_handler_count);
						g_assert (call_handler_count < G_MAXUINT16);
					}
				}
			}

			if (td->clause_indexes [in_offset] != -1) {
				/* LEAVE instructions in catch clauses need to check for abort exceptions */
				handle_branch (td, MINT_LEAVE_CHECK, target_offset);
			} else {
				handle_branch (td, MINT_BR, target_offset);
			}
			td->last_ins->info.target_bb->preserve = TRUE;

			if (*td->ip == CEE_LEAVE)
				td->ip += 5;
			else
				td->ip += 2;
			link_bblocks = FALSE;
			break;
		}
		case MONO_CUSTOM_PREFIX:
			++td->ip;
		        switch (*td->ip) {
				case CEE_MONO_RETHROW:
					CHECK_STACK (td, 1);
					interp_add_ins (td, MINT_MONO_RETHROW);
					td->sp--;
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					td->sp = td->stack;
					++td->ip;
					link_bblocks = FALSE;
					break;

				case CEE_MONO_LD_DELEGATE_METHOD_PTR:
					--td->sp;
					td->ip += 1;
					interp_add_ins (td, MINT_LD_DELEGATE_METHOD_PTR);
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					push_simple_type (td, STACK_TYPE_I);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					break;
				case CEE_MONO_CALLI_EXTRA_ARG: {
					int saved_local = td->sp [-1].var;
					/* Same as CEE_CALLI, except that we drop the extra arg required for llvm specific behaviour */
					td->sp -= 2;
					StackInfo tos = td->sp [1];

					// Push back to top of stack and fixup the local offset
					push_types (td, &tos, 1);
					td->sp [-1].var = saved_local;

					if (!interp_transform_call (td, method, NULL, generic_context, NULL, FALSE, error, FALSE, FALSE, FALSE))
						goto exit;
					break;
				}
				case CEE_MONO_JIT_ICALL_ADDR: {
					const guint32 icall_id_token = read32 (td->ip + 1);
					td->ip += 5;
					const gconstpointer func = mono_find_jit_icall_info ((MonoJitICallId)icall_id_token)->func;

					interp_add_ins (td, MINT_LDFTN_ADDR);
					push_simple_type (td, STACK_TYPE_I);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					td->last_ins->data [0] = get_data_item_index (td, (gpointer)func);
					break;
				}
				case CEE_MONO_ICALL: {
					int dreg = -1;
					MonoJitICallId const jit_icall_id = (MonoJitICallId)read32 (td->ip + 1);
					MonoJitICallInfo const * const info = mono_find_jit_icall_info (jit_icall_id);
					td->ip += 5;

					CHECK_STACK (td, info->sig->param_count);
					td->sp -= info->sig->param_count;
					int *call_args = (int*)mono_mempool_alloc (td->mempool, (info->sig->param_count + 1) * sizeof (int));
					for (int i = 0; i < info->sig->param_count; i++)
						call_args [i] = td->sp [i].var;
					call_args [info->sig->param_count] = -1;
					int param_offset = get_tos_offset (td);

					if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
						mt = mono_mint_type (info->sig->ret);
						push_simple_type (td, stack_type [mt]);
						dreg = td->sp [-1].var;
					} else {
						// dummy dreg
						push_simple_type (td, STACK_TYPE_I4);
						td->sp--;
						dreg = td->sp [0].var;
					}

					if (jit_icall_id == MONO_JIT_ICALL_mono_threads_attach_coop) {
						rtm->needs_thread_attach = 1;
						// Add dummy return value
						interp_add_ins (td, MINT_LDNULL);
						interp_ins_set_dreg (td->last_ins, dreg);
					} else if (jit_icall_id == MONO_JIT_ICALL_mono_threads_detach_coop) {
						g_assert (rtm->needs_thread_attach);
					} else {
						MintICallSig icall_sig = interp_get_icall_sig (info->sig);
						g_assert (icall_sig != MINT_ICALLSIG_MAX);

						interp_add_ins (td, MINT_ICALL);
						interp_ins_set_dreg (td->last_ins, dreg);
						interp_ins_set_sreg (td->last_ins, MINT_CALL_ARGS_SREG);
						td->last_ins->data [0] = icall_sig;
						td->last_ins->data [1] = get_data_item_index (td, (gpointer)info->func);
						init_last_ins_call (td);
						td->last_ins->info.call_info->call_args = call_args;
						if (!td->optimized)
							td->last_ins->info.call_info->call_offset = param_offset;
					}
					break;
				}
			case CEE_MONO_VTADDR: {
				int size;
				CHECK_STACK (td, 1);
				klass = td->sp [-1].klass;
				if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE && !signature->marshalling_disabled)
					size = mono_class_native_size (klass, NULL);
				else
					size = mono_class_value_size (klass, NULL);

				int local = interp_create_var_explicit (td, m_class_get_byval_arg (klass), size);
				interp_add_ins (td, MINT_MOV_VT);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				interp_ins_set_dreg (td->last_ins, local);
				td->last_ins->data [0] = GINT_TO_UINT16 (size);

				interp_add_ins (td, MINT_LDLOCA_S);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				interp_ins_set_sreg (td->last_ins, local);
				td->vars [local].indirects++;

				++td->ip;
				break;
			}
			case CEE_MONO_LDPTR:
			case CEE_MONO_CLASSCONST:
			case CEE_MONO_METHODCONST:
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_LDPTR);
				push_simple_type (td, STACK_TYPE_I);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, mono_method_get_wrapper_data (method, token));
				break;
			case CEE_MONO_PINVOKE_ADDR_CACHE: {
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_LDPTR);
				g_assert (method->wrapper_type != MONO_WRAPPER_NONE);
				push_simple_type (td, STACK_TYPE_I);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				/* This is a memory slot used by the wrapper */
				gpointer addr = imethod_alloc0 (td, sizeof (gpointer));
				td->last_ins->data [0] = get_data_item_index (td, addr);
				break;
			}
			case CEE_MONO_OBJADDR:
				CHECK_STACK (td, 1);
				++td->ip;
				td->sp[-1].type = STACK_TYPE_MP;
				/* do nothing? */
				break;
			case CEE_MONO_NEWOBJ:
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_MONO_NEWOBJ);
				push_simple_type (td, STACK_TYPE_O);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, mono_method_get_wrapper_data (method, token));
				break;
			case CEE_MONO_RETOBJ:
				CHECK_STACK (td, 1);
				token = read32 (td->ip + 1);
				td->ip += 5;
				interp_add_ins (td, MINT_MONO_RETOBJ);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);

				/*stackval_from_data (signature->ret, frame->retval, sp->data.vt, signature->pinvoke);*/

				if (td->sp > td->stack)
					g_warning ("CEE_MONO_RETOBJ: more values on stack: %d", td->sp-td->stack);
				break;
			case CEE_MONO_LDNATIVEOBJ: {
				token = read32 (td->ip + 1);
				td->ip += 5;
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				g_assert (m_class_is_valuetype (klass));
				td->sp--;

				int size = mono_class_native_size (klass, NULL);
				interp_add_ins (td, MINT_LDOBJ_VT);
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_type_vt (td, klass, size);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = GINT_TO_UINT16 (size);
				break;
			}
			case CEE_MONO_TLS: {
				g_error ("We shouldn't use managed allocator with interpreter");
				break;
			}
			case CEE_MONO_ATOMIC_STORE_I4:
				g_error ("We shouldn't use managed allocator with interpreter");
				break;
			case CEE_MONO_SAVE_LMF:
			case CEE_MONO_RESTORE_LMF:
			case CEE_MONO_NOT_TAKEN:
				++td->ip;
				break;
			case CEE_MONO_LDPTR_INT_REQ_FLAG:
				interp_add_ins (td, MINT_LDPTR);
				push_type (td, STACK_TYPE_MP, NULL);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->last_ins->data [0] = get_data_item_index (td, &mono_thread_interruption_request_flag);
				++td->ip;
				break;
			case CEE_MONO_MEMORY_BARRIER:
				interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
				++td->ip;
				break;
			case CEE_MONO_LDDOMAIN:
				interp_add_ins (td, MINT_MONO_LDDOMAIN);
				push_simple_type (td, STACK_TYPE_I);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			case CEE_MONO_SAVE_LAST_ERROR:
				save_last_error = TRUE;
				++td->ip;
				break;
			case CEE_MONO_GET_SP: {
				++td->ip;
				g_assert (*td->ip == MONO_CUSTOM_PREFIX);
				++td->ip;
				g_assert (*td->ip == CEE_MONO_ICALL);
				// in coop gc transitions we use mono.get.sp + calli to implement enter/exit
				// on interpreter we do these transitions explicitly when entering/exiting the
				// interpreter so we can ignore them here in the wrappers.
				MonoJitICallId const jit_icall_id = (MonoJitICallId)read32 (td->ip + 1);
				MonoJitICallInfo const * const info = mono_find_jit_icall_info (jit_icall_id);

				if (info->sig->ret->type != MONO_TYPE_VOID) {
					g_assert_checked (jit_icall_id == MONO_JIT_ICALL_mono_threads_enter_gc_safe_region_unbalanced);
					// Push a dummy coop gc var
					interp_add_ins (td, MINT_LDNULL);
					push_simple_type (td, STACK_TYPE_I);
					td->last_ins->dreg = td->sp [-1].var;
					interp_add_ins (td, MINT_MONO_ENABLE_GCTRANS);
				} else {
					g_assert_checked (jit_icall_id == MONO_JIT_ICALL_mono_threads_exit_gc_safe_region_unbalanced);
					// Pop the unused gc var
					td->sp--;
				}
				td->ip += 5;
				break;
			}
			default:
				g_error ("transform.c: Unimplemented opcode: 0xF0 %02x at 0x%x\n", *td->ip, td->ip-header->code);
			}
			break;
#if 0
		case CEE_PREFIX7:
		case CEE_PREFIX6:
		case CEE_PREFIX5:
		case CEE_PREFIX4:
		case CEE_PREFIX3:
		case CEE_PREFIX2:
		case CEE_PREFIXREF: ves_abort(); break;
#endif
		/*
		 * Note: Exceptions thrown when executing a prefixed opcode need
		 * to take into account the number of prefix bytes (usually the
		 * throw point is just (ip - n_prefix_bytes).
		 */
		case CEE_PREFIX1:
			++td->ip;
			switch (*td->ip) {
			case CEE_ARGLIST:
				load_local (td, arglist_local);
				++td->ip;
				break;
			case CEE_CEQ:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP) {
					interp_add_ins (td, MINT_CEQ_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				} else {
					if (td->sp [-1].type == STACK_TYPE_R4 && td->sp [-2].type == STACK_TYPE_R8)
						interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
					if (td->sp [-1].type == STACK_TYPE_R8 && td->sp [-2].type == STACK_TYPE_R4)
						interp_add_conv (td, td->sp - 2, NULL, STACK_TYPE_R8, MINT_CONV_R8_R4);
					interp_add_ins (td, MINT_CEQ_I4 + td->sp [-1].type - STACK_TYPE_I4);
				}
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			case CEE_CGT:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CGT_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CGT_I4 + td->sp [-1].type - STACK_TYPE_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			case CEE_CGT_UN:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CGT_UN_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CGT_UN_I4 + td->sp [-1].type - STACK_TYPE_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			case CEE_CLT:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CLT_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CLT_I4 + td->sp [-1].type - STACK_TYPE_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			case CEE_CLT_UN:
				CHECK_STACK(td, 2);
				if (td->sp [-1].type == STACK_TYPE_O || td->sp [-1].type == STACK_TYPE_MP)
					interp_add_ins (td, MINT_CLT_UN_I4 + STACK_TYPE_I - STACK_TYPE_I4);
				else
					interp_add_ins (td, MINT_CLT_UN_I4 + td->sp [-1].type - STACK_TYPE_I4);
				td->sp -= 2;
				interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			case CEE_LDVIRTFTN: /* fallthrough */
			case CEE_LDFTN: {
				MonoMethod *m;
				token = read32 (td->ip + 1);
				m = interp_get_method (method, token, image, generic_context, error);
				goto_if_nok (error, exit);

				if (!mono_method_can_access_method (method, m))
					interp_generate_mae_throw (td, method, m);

				if (method->wrapper_type == MONO_WRAPPER_NONE && m->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
					m = mono_marshal_get_synchronized_wrapper (m);

				if (constrained_class) {
					MonoMethod *virt_method = m;
					m = mono_get_method_constrained_with_method (image, m, constrained_class, generic_context, error);
					goto_if_nok (error, exit);
					if (mono_class_has_dim_conflicts (constrained_class) && mono_class_is_method_ambiguous (constrained_class, virt_method))
						interp_generate_void_throw (td, MONO_JIT_ICALL_mono_throw_ambiguous_implementation);
					constrained_class = NULL;
				}

				if (G_UNLIKELY (*td->ip == CEE_LDFTN &&
						m->wrapper_type == MONO_WRAPPER_NONE &&
						mono_method_has_unmanaged_callers_only_attribute (m))) {

					if (m->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
						interp_generate_void_throw (td, MONO_JIT_ICALL_mono_throw_not_supported);
						interp_add_ins (td, MINT_LDNULL);
						push_simple_type (td, STACK_TYPE_MP);
						interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
						td->ip += 5;
						break;
					}

					MonoMethod *ctor_method;

					const unsigned char *next_ip = td->ip + 5;
					/* check for
					 *    ldftn method_sig
					 *    newobj Delegate::.ctor
					 */
					if (next_ip < end &&
					    *next_ip == CEE_NEWOBJ &&
					    ((ctor_method = interp_get_method (method, read32 (next_ip + 1), image, generic_context, error))) &&
					    is_ok (error) &&
					    m_class_get_parent (ctor_method->klass) == mono_defaults.multicastdelegate_class &&
					    !strcmp (ctor_method->name, ".ctor")) {
						mono_error_set_not_supported (error, "Cannot create delegate from method with UnmanagedCallersOnlyAttribute");
						goto exit;
					}

					MonoClass *delegate_klass = NULL;
					MonoGCHandle target_handle = 0;
					ERROR_DECL (wrapper_error);
					m = mono_marshal_get_managed_wrapper (m, delegate_klass, target_handle, wrapper_error);
					if (!is_ok (wrapper_error)) {
						/* Generate a call that will throw an exception if the
						 * UnmanagedCallersOnly attribute is used incorrectly */
						interp_generate_ipe_throw_with_msg (td, wrapper_error);
						mono_interp_error_cleanup (wrapper_error);
						interp_add_ins (td, MINT_LDNULL);
						push_simple_type (td, STACK_TYPE_MP);
						interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					} else {
						/* push a pointer to a trampoline that calls m */
						gpointer entry = mini_get_interp_callbacks ()->create_method_pointer (m, TRUE, error);
#if SIZEOF_VOID_P == 8
						interp_add_ins (td, MINT_LDC_I8);
						WRITE64_INS (td->last_ins, 0, &entry);
#else
						interp_add_ins (td, MINT_LDC_I4);
						WRITE32_INS (td->last_ins, 0, &entry);
#endif
						push_simple_type (td, STACK_TYPE_MP);
						interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
					}
					td->ip += 5;
					break;
				}

				guint16 index = get_data_item_index_imethod (td, mono_interp_get_imethod (m));
				goto_if_nok (error, exit);
				if (*td->ip == CEE_LDVIRTFTN) {
					CHECK_STACK (td, 1);
					--td->sp;
					interp_add_ins (td, MINT_LDVIRTFTN);
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					td->last_ins->data [0] = index;
				} else {
					interp_add_ins (td, MINT_LDFTN);
					td->last_ins->data [0] = index;
				}
				push_simple_type (td, STACK_TYPE_F);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

				td->ip += 5;
				break;
			}
			case CEE_LDARG: {
				int arg_n = read16 (td->ip + 1);
				if (!inlining)
					load_arg (td, arg_n);
				else
					load_local (td, arg_locals [arg_n]);
				td->ip += 3;
				break;
			}
			case CEE_LDARGA: {
				int n = read16 (td->ip + 1);

				if (!inlining) {
					interp_add_ins (td, MINT_LDLOCA_S);
					interp_ins_set_sreg (td->last_ins, n);
					td->vars [n].indirects++;
				} else {
					int loc_n = arg_locals [n];
					interp_add_ins (td, MINT_LDLOCA_S);
					interp_ins_set_sreg (td->last_ins, loc_n);
					td->vars [loc_n].indirects++;
				}
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 3;
				break;
			}
			case CEE_STARG: {
				int arg_n = read16 (td->ip + 1);
				if (!inlining)
					store_arg (td, arg_n);
				else
					store_local (td, arg_locals [arg_n]);
				td->ip += 3;
				break;
			}
			case CEE_LDLOC: {
				int loc_n = read16 (td->ip + 1);
				if (!inlining)
					load_local (td, num_args + loc_n);
				else
					load_local (td, local_locals [loc_n]);
				td->ip += 3;
				break;
			}
			case CEE_LDLOCA: {
				int loc_n = read16 (td->ip + 1);
				interp_add_ins (td, MINT_LDLOCA_S);
				if (!inlining)
					loc_n += num_args;
				else
					loc_n = local_locals [loc_n];
				interp_ins_set_sreg (td->last_ins, loc_n);
				td->vars [loc_n].indirects++;
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->ip += 3;
				break;
			}
			case CEE_STLOC: {
				int loc_n = read16 (td->ip + 1);
				if (!inlining)
					store_local (td, num_args + loc_n);
				else
					store_local (td, local_locals [loc_n]);
				td->ip += 3;
				break;
			}
			case CEE_LOCALLOC:
				INLINE_FAILURE;
				CHECK_STACK (td, 1);
#if SIZEOF_VOID_P == 8
				if (td->sp [-1].type == STACK_TYPE_I8)
					interp_add_conv (td, td->sp - 1, NULL, STACK_TYPE_I4, MINT_MOV_8);
#endif
				interp_add_ins (td, MINT_LOCALLOC);
				td->sp--;
				if (td->sp != td->stack) {
					mono_error_set_generic_error (error, "System", "InvalidProgramException", "");
					goto exit;
				}
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_simple_type (td, STACK_TYPE_MP);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				td->has_localloc = TRUE;
				++td->ip;
				break;
#if 0
			case CEE_UNUSED57: ves_abort(); break;
#endif
			case CEE_ENDFILTER:
				td->sp--;
				if (td->sp != td->stack || td->sp [0].type != STACK_TYPE_I4) {
					mono_error_set_generic_error (error, "System", "InvalidProgramException", "");
					goto exit;
				}

				interp_add_ins (td, MINT_ENDFILTER);
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				++td->ip;
				link_bblocks = FALSE;
				break;
			case CEE_UNALIGNED_:
				td->ip += 2;
				break;
			case CEE_VOLATILE_:
				++td->ip;
				volatile_ = TRUE;
				break;
			case CEE_TAIL_:
				++td->ip;
				tailcall = TRUE;
				// TODO: This should raise a method_tail_call profiler event.
				break;
			case CEE_INITOBJ:
				CHECK_STACK(td, 1);
				token = read32 (td->ip + 1);
				klass = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (klass);
				if (m_class_is_valuetype (klass)) {
					--td->sp;
					interp_add_ins (td, MINT_INITOBJ);
					interp_ins_set_sreg (td->last_ins, td->sp [0].var);
					i32 = mono_class_value_size (klass, NULL);
					g_assert (i32 < G_MAXUINT16);
					td->last_ins->data [0] = GINT_TO_UINT16 (i32);
				} else {
					interp_add_ins (td, MINT_LDNULL);
					push_type (td, STACK_TYPE_O, NULL);
					interp_ins_set_dreg (td->last_ins, td->sp [-1].var);

					interp_add_ins (td, MINT_STIND_REF);
					td->sp -= 2;
					interp_ins_set_sregs2 (td->last_ins, td->sp [0].var, td->sp [1].var);
				}
				td->ip += 5;
				break;
			case CEE_CPBLK:
				CHECK_STACK(td, 3);
				/* FIX? convert length to I8? */
				if (volatile_)
					interp_add_ins (td, MINT_MONO_MEMORY_BARRIER);
				interp_add_ins (td, MINT_CPBLK);
				td->sp -= 3;
				interp_ins_set_sregs3 (td->last_ins, td->sp [0].var, td->sp [1].var, td->sp [2].var);
				BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_SEQ);
				++td->ip;
				break;
			case CEE_READONLY_:
				readonly = TRUE;
				td->ip += 1;
				break;
			case CEE_CONSTRAINED_:
				token = read32 (td->ip + 1);
				constrained_class = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (constrained_class);
				td->ip += 5;
				break;
			case CEE_INITBLK:
				CHECK_STACK(td, 3);
				BARRIER_IF_VOLATILE (td, MONO_MEMORY_BARRIER_REL);
				interp_add_ins (td, MINT_INITBLK);
				td->sp -= 3;
				interp_ins_set_sregs3 (td->last_ins, td->sp [0].var, td->sp [1].var, td->sp [2].var);
				td->ip += 1;
				break;
			case CEE_NO_:
				/* FIXME: implement */
				td->ip += 2;
				break;
			case CEE_RETHROW: {
				int clause_index = td->clause_indexes [in_offset];
				g_assert (clause_index != -1);
				interp_add_ins (td, MINT_RETHROW);
				td->last_ins->data [0] = GUINT32_TO_UINT16 (rtm->clause_data_offsets [clause_index]);
				td->sp = td->stack;
				link_bblocks = FALSE;
				++td->ip;
				break;
			}
			case CEE_SIZEOF: {
				gint32 size;
				token = read32 (td->ip + 1);
				td->ip += 5;
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC && !image_is_dynamic (m_class_get_image (method->klass)) && !generic_context) {
					int align;
					MonoType *type = mono_type_create_from_typespec_checked (image, token, error);
					goto_if_nok (error, exit);
					size = mono_type_size (type, &align);
				} else {
					int align;
					MonoClass *szclass = mini_get_class (method, token, generic_context);
					CHECK_TYPELOAD (szclass);
#if 0
					if (!szclass->valuetype)
						THROW_EX (mono_exception_from_name (mono_defaults.corlib, "System", "InvalidProgramException"), ip - 5);
#endif
					size = mono_type_size (m_class_get_byval_arg (szclass), &align);
				}
				interp_add_ins (td, MINT_LDC_I4);
				WRITE32_INS (td->last_ins, 0, &size);
				push_simple_type (td, STACK_TYPE_I4);
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				break;
			}
			case CEE_REFANYTYPE:
				interp_add_ins (td, MINT_REFANYTYPE);
				td->sp--;
				interp_ins_set_sreg (td->last_ins, td->sp [0].var);
				push_type_vt (td, mono_defaults.typehandle_class, mono_class_value_size (mono_defaults.typehandle_class, NULL));
				interp_ins_set_dreg (td->last_ins, td->sp [-1].var);
				++td->ip;
				break;
			default:
				g_error ("transform.c: Unimplemented opcode: 0xFE %02x (%s) at 0x%x\n", *td->ip, mono_opcode_name (256 + *td->ip), td->ip-header->code);
			}
			break;
		default: {
			mono_error_set_generic_error (error, "System", "InvalidProgramException", "opcode 0x%02x not handled", *td->ip);
			goto exit;
		}
		}
		// No IR instructions were added as part of a bb_start IL instruction. Add a MINT_NOP
		// so we always have an instruction associated with a bb_start. This is simple and avoids
		// any complications associated with il_offset tracking.
		if (!td->cbb->last_ins)
			interp_add_ins (td, MINT_NOP);
	}

	g_assert (td->ip == end);

	if (inlining) {
		// When inlining, all return points branch to this bblock. Code generation inside the caller
		// method continues in this bblock. exit_bb is not necessarily an out bb for cbb. We need to
		// restore stack state so future codegen can work.
		td->cbb->next_bb = exit_bb;
		td->cbb = exit_bb;
		if (exit_bb->stack_height >= 0) {
			if (exit_bb->stack_height > 0)
				memcpy (td->stack, exit_bb->stack_state, exit_bb->stack_height * sizeof(td->stack [0]));
			td->sp = td->stack + exit_bb->stack_height;
		}
		// If exit_bb is not reached by any other bb in this method, just mark it as dead so the
		// method that does the inlining no longer generates code for the following IL opcodes.
		if (exit_bb->in_count == 0)
			exit_bb->dead = TRUE;
	}

	if (sym_seq_points) {
		for (InterpBasicBlock *interp_bb = td->entry_bb->next_bb; interp_bb != NULL; interp_bb = interp_bb->next_bb) {
			if (interp_bb->first_ins && interp_bb->in_count > 1 && interp_bb->first_ins->opcode == MINT_SDB_SEQ_POINT)
				interp_insert_ins_bb (td, interp_bb, NULL, MINT_SDB_INTR_LOC);
		}
	}

	if (td->optimized && !inlining)
		mono_interp_pgo_method_was_tiered (method);

exit_ret:
	g_free (arg_locals);
	g_free (local_locals);
	mono_bitset_free (il_targets);
	mono_basic_block_free (original_bb);
	td->dont_inline = g_list_remove (td->dont_inline, method);

	return ret;
exit:
	ret = FALSE;
	if (td->has_invalid_code)
		mono_error_set_generic_error (error, "System", "InvalidProgramException", "");
	goto exit_ret;
}

static void
handle_relocations (TransformData *td)
{
	// Handle relocations
	for (guint i = 0; i < td->relocs->len; ++i) {
		Reloc *reloc = (Reloc*)g_ptr_array_index (td->relocs, i);
		int offset = reloc->target_bb->native_offset - reloc->offset;

		switch (reloc->type) {
		case RELOC_SHORT_BRANCH:
			g_assert (td->new_code [reloc->offset + reloc->skip + 1] == 0xdead);
			td->new_code [reloc->offset + reloc->skip + 1] = GINT_TO_UINT16 (offset);
			break;
		case RELOC_LONG_BRANCH: {
			guint16 *v = (guint16 *)&offset;
			g_assert (td->new_code [reloc->offset + reloc->skip + 1] == 0xdead);
			g_assert (td->new_code [reloc->offset + reloc->skip + 2] == 0xbeef);
			td->new_code [reloc->offset + reloc->skip + 1] = *(guint16 *)v;
			td->new_code [reloc->offset + reloc->skip + 2] = *(guint16 *)(v + 1);
			break;
		}
		case RELOC_SWITCH: {
			guint16 *v = (guint16 *)&offset;
			g_assert (td->new_code [reloc->offset] == 0xdead);
			g_assert (td->new_code [reloc->offset + 1] == 0xbeef);
			td->new_code [reloc->offset] = *(guint16 *)v;
			td->new_code [reloc->offset + 1] = *(guint16 *)(v + 1);
			break;
		}
		default:
			g_assert_not_reached ();
			break;
		}
	}
}

static void
alloc_unopt_global_local (TransformData *td, int *plocal, gpointer data)
{
	int local = *plocal;
	// Execution stack locals are resolved when we emit the instruction in the code stream,
	// once all global locals have their offset resolved
	if (td->vars [local].execution_stack)
		return;
	// Check if already resolved
	if (td->vars [local].offset != -1)
		return;

	int offset = td->total_locals_size;
	int size = td->vars [local].size;
	td->vars [local].offset = offset;
	td->total_locals_size = ALIGN_TO (offset + size, MINT_STACK_SLOT_SIZE);
}

int
interp_get_ins_length (InterpInst *ins)
{
	if (ins->opcode == MINT_SWITCH)
		return MINT_SWITCH_LEN (READ32 (&ins->data [0]));
#ifdef ENABLE_EXPERIMENT_TIERED
	else if (MINT_IS_PATCHABLE_CALL (ins->opcode))
		return MAX (mono_interp_oplen [MINT_JIT_CALL2], mono_interp_oplen [ins->opcode]);
#endif
	else
		return mono_interp_oplen [ins->opcode];
}

void
interp_foreach_ins_svar (TransformData *td, InterpInst *ins, gpointer data, void (*callback)(TransformData*, int*, gpointer))
{
	int opcode = ins->opcode;
	if (mono_interp_op_sregs [opcode]) {
		for (int i = 0; i < mono_interp_op_sregs [opcode]; i++) {
			if (ins->sregs [i] == MINT_CALL_ARGS_SREG) {
				if (ins->info.call_info && ins->info.call_info->call_args) {
					int *call_args = ins->info.call_info->call_args;
					while (*call_args != -1) {
						callback (td, call_args, data);
						call_args++;
					}
				}
			} else {
				callback (td, &ins->sregs [i], data);
			}
		}
	}
}

void
interp_foreach_ins_var (TransformData *td, InterpInst *ins, gpointer data, void (*callback)(TransformData*, int*, gpointer))
{
	interp_foreach_ins_svar (td, ins, data, callback);

	int opcode = ins->opcode;
	if (mono_interp_op_dregs [opcode])
		callback (td, &ins->dreg, data);
}

int
interp_compute_native_offset_estimates (TransformData *td)
{
	InterpBasicBlock *bb;
	int noe = 0;
	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins;
		bb->native_offset_estimate = noe;
		if (!td->optimized && bb->patchpoint_bb)
			noe += 2;

		for (ins = bb->first_ins; ins != NULL; ins = ins->next) {
			int opcode = ins->opcode;
			// Skip dummy opcodes for more precise offset computation
			if (MINT_IS_EMIT_NOP (opcode))
				continue;
			noe += interp_get_ins_length (ins);
			if (!td->optimized)
				interp_foreach_ins_var (td, ins, NULL, alloc_unopt_global_local);
		}
	}

	if (!td->optimized) {
		td->total_locals_size = ALIGN_TO (td->total_locals_size, MINT_STACK_ALIGNMENT);
		td->param_area_offset = td->total_locals_size;
	}
	return noe;
}

gboolean
interp_is_short_offset (int src_offset, int dest_offset)
{
	int diff = dest_offset - src_offset;
	if (diff >= G_MININT16 && diff <= G_MAXINT16)
		return TRUE;
	return FALSE;
}

static int
get_short_brop (int opcode)
{
	if (MINT_IS_UNCONDITIONAL_BRANCH (opcode)) {
		if (opcode == MINT_BR)
			return MINT_BR_S;
		else if (opcode == MINT_LEAVE_CHECK)
			return MINT_LEAVE_S_CHECK;
		else if (opcode == MINT_CALL_HANDLER)
			return MINT_CALL_HANDLER_S;
		else
			return opcode;
	}

	if (opcode >= MINT_BRFALSE_I4 && opcode <= MINT_BRTRUE_I8)
		return opcode + MINT_BRFALSE_I4_S - MINT_BRFALSE_I4;

	if (opcode >= MINT_BEQ_I4 && opcode <= MINT_BLT_UN_R8)
		return opcode + MINT_BEQ_I4_S - MINT_BEQ_I4;

	// Already short branch
	return opcode;
}

static int
get_var_offset (TransformData *td, int var)
{
	if (td->vars [var].offset != -1)
		return td->vars [var].offset;

	// FIXME Some vars might end up with unitialized offset because they are not declared at all in the code.
	// This can happen if the bblock declaring the var gets removed, while other unreachable bblocks, that access
	// the var are also not removed. This limitation is due to bblock removal using IN count for removing a bblock,
	// which doesn't account for cycles.
	if (td->optimized)
		return -1;

	// If we use the optimized offset allocator, all locals should have had their offsets already allocated
	g_assert (!td->optimized);
	// The only remaining locals to allocate are the ones from the execution stack
	g_assert (td->vars [var].execution_stack);

	td->vars [var].offset = td->total_locals_size + td->vars [var].stack_offset;
	return td->vars [var].offset;
}

static guint16*
emit_compacted_instruction (TransformData *td, guint16* start_ip, InterpInst *ins)
{
	guint16 opcode = ins->opcode;
	guint16 *ip = start_ip;

	// We know what IL offset this instruction was created for. We can now map the IL offset
	// to the IR offset. We use this array to resolve the relocations, which reference the IL.
	if (ins->il_offset != -1 && !td->in_offsets [ins->il_offset]) {
		g_assert (ins->il_offset >= 0 && GINT_TO_UINT32(ins->il_offset) < td->header->code_size);
		td->in_offsets [ins->il_offset] = GPTRDIFF_TO_INT (start_ip - td->new_code + 1);

		MonoDebugLineNumberEntry lne;
		lne.native_offset = GPTRDIFF_TO_UINT32 ((guint8*)start_ip - (guint8*)td->new_code);
		lne.il_offset = ins->il_offset;
		g_array_append_val (td->line_numbers, lne);
	}

	if (MINT_IS_EMIT_NOP (opcode))
		return ip;

	*ip++ = opcode;
	if (opcode == MINT_SWITCH) {
		int labels = READ32 (&ins->data [0]);
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->sregs [0]));
		// Write number of switch labels
		*ip++ = ins->data [0];
		*ip++ = ins->data [1];
		// Add relocation for each label
		for (int i = 0; i < labels; i++) {
			Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
			reloc->type = RELOC_SWITCH;
			reloc->offset = GPTRDIFF_TO_INT (ip - td->new_code);
			reloc->target_bb = ins->info.target_bb_table [i];
			g_ptr_array_add (td->relocs, reloc);
			*ip++ = 0xdead;
			*ip++ = 0xbeef;
		}
	} else if (MINT_IS_UNCONDITIONAL_BRANCH (opcode) || MINT_IS_CONDITIONAL_BRANCH (opcode) || MINT_IS_SUPER_BRANCH (opcode)) {
		if (opcode == MINT_BR) {
			InterpInst *target_first_ins = interp_first_ins (ins->info.target_bb);
			if (target_first_ins && MINT_IS_RETURN (target_first_ins->opcode)) {
				// Emit the return directly instead of branching
				ins = target_first_ins;
				opcode = ins->opcode;
				ip [-1] = opcode;
				goto opcode_emit;
			}
		}
		const int br_offset = GPTRDIFF_TO_INT (start_ip - td->new_code);
		gboolean has_imm = opcode >= MINT_BEQ_I4_IMM_SP && opcode <= MINT_BLT_UN_I8_IMM_SP;
		for (int i = 0; i < mono_interp_op_sregs [opcode]; i++)
			*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->sregs [i]));
		if (has_imm)
			*ip++ = ins->data [0];

		if (ins->info.target_bb->native_offset >= 0) {
			int offset = ins->info.target_bb->native_offset - br_offset;
			// Backwards branch. We can already patch it.
			if (interp_is_short_offset (br_offset, ins->info.target_bb->native_offset)) {
				// Replace the long opcode we added at the start
				*start_ip = GINT_TO_OPCODE (get_short_brop (opcode));
				*ip++ = GINT_TO_UINT16 (ins->info.target_bb->native_offset - br_offset);
			} else {
				WRITE32 (ip, &offset);
			}
		} else if (opcode == MINT_BR && ins->info.target_bb == td->cbb->next_bb) {
			// Ignore branch to the next basic block. Revert the added MINT_BR.
			ip--;
		} else {
			// If the estimate offset is short, then surely the real offset is short
			// otherwise we conservatively have to use long branch opcodes
			int cur_estimation_error = td->cbb->native_offset_estimate - td->cbb->native_offset;
			int target_bb_estimated_offset = ins->info.target_bb->native_offset_estimate - cur_estimation_error;
			gboolean is_short = interp_is_short_offset (br_offset, target_bb_estimated_offset);
			if (is_short)
				*start_ip = GINT_TO_OPCODE (get_short_brop (opcode));
			else
				g_assert (!MINT_IS_SUPER_BRANCH (opcode)); // FIXME missing handling for long branch

			// We don't know the in_offset of the target, add a reloc
			Reloc *reloc = (Reloc*)mono_mempool_alloc0 (td->mempool, sizeof (Reloc));
			reloc->type = is_short ? RELOC_SHORT_BRANCH : RELOC_LONG_BRANCH;
			reloc->skip = mono_interp_op_sregs [opcode] + has_imm;
			reloc->offset = br_offset;
			reloc->target_bb = ins->info.target_bb;
			g_ptr_array_add (td->relocs, reloc);
			*ip++ = 0xdead;
			if (!is_short)
				*ip++ = 0xbeef;
		}
		if (opcode == MINT_CALL_HANDLER)
			*ip++ = ins->data [2];

	} else if (opcode == MINT_SDB_SEQ_POINT || opcode == MINT_IL_SEQ_POINT) {
		SeqPoint *seqp = (SeqPoint*)mono_mempool_alloc0 (td->mempool, sizeof (SeqPoint));
		InterpBasicBlock *cbb;

		if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_METHOD_ENTRY) {
			seqp->il_offset = METHOD_ENTRY_IL_OFFSET;
			cbb = td->offset_to_bb [0];
		} else {
			if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_METHOD_EXIT)
				seqp->il_offset = METHOD_EXIT_IL_OFFSET;
			else
				seqp->il_offset = ins->il_offset;
			cbb = td->offset_to_bb [ins->il_offset];
		}
		seqp->native_offset = GPTRDIFF_TO_INT ((guint8*)start_ip - (guint8*)td->new_code);
		if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_NONEMPTY_STACK)
			seqp->flags |= MONO_SEQ_POINT_FLAG_NONEMPTY_STACK;
		if (ins->flags & INTERP_INST_FLAG_SEQ_POINT_NESTED_CALL)
			seqp->flags |= MONO_SEQ_POINT_FLAG_NESTED_CALL;
		g_ptr_array_add (td->seq_points, seqp);

		cbb->seq_points = g_slist_prepend_mempool (td->mempool, cbb->seq_points, seqp);
		cbb->last_seq_point = seqp;
		// IL_SEQ_POINT shouldn't exist in the emitted code, we undo the ip position
		if (opcode == MINT_IL_SEQ_POINT)
			return ip - 1;
	} else if (opcode == MINT_MOV_SRC_OFF || opcode == MINT_MOV_DST_OFF) {
		guint16 foff = ins->data [0];
		guint16 mt = ins->data [1];
		guint16 fsize = ins->data [2];
		ip--;

		if (opcode == MINT_MOV_DST_OFF && get_var_offset (td, ins->dreg) != get_var_offset (td, ins->sregs [1])) {
			// We are no longer storing a field into the same valuetype. Copy also the whole vt.
			*ip++ = MINT_MOV_VT;
			*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->dreg));
			*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->sregs [1]));
			*ip++ = GINT_TO_UINT16 (td->vars [ins->dreg].size);
		}

		int dest_off = get_var_offset (td, ins->dreg);
		int src_off = get_var_offset (td, ins->sregs [0]);
		if (opcode == MINT_MOV_SRC_OFF)
			src_off += foff;
		else
			dest_off += foff;
		if (mt == MINT_TYPE_VT || fsize) {
			// For valuetypes or unaligned access we just use memcpy
			opcode = MINT_MOV_VT;
		} else {
			if (opcode == MINT_MOV_SRC_OFF) {
				// Loading from field, always load full i4
				opcode = GINT_TO_OPCODE (interp_get_mov_for_type (mt, TRUE));
			} else {
				// Storing into field, copy exact size
				fsize = get_mint_type_size (mt);
				switch (fsize) {
					case 1: opcode = MINT_MOV_1; break;
					case 2: opcode = MINT_MOV_2; break;
					case 4: opcode = MINT_MOV_4; break;
					case 8: opcode = MINT_MOV_8; break;
					default: g_assert_not_reached ();
				}
			}
		}
		*ip++ = opcode;
		*ip++ = GINT_TO_UINT16 (dest_off);
		*ip++ = GINT_TO_UINT16 (src_off);
		if (opcode == MINT_MOV_VT)
			*ip++ = fsize;
#ifdef ENABLE_EXPERIMENT_TIERED
	} else if (ins->flags & INTERP_INST_FLAG_RECORD_CALL_PATCH) {
		g_assert (MINT_IS_PATCHABLE_CALL (opcode));

		/* TODO: could `ins` be removed by any interp optimization? */
		MonoMethod *target_method = (MonoMethod *) g_hash_table_lookup (td->patchsite_hash, ins);
		g_assert (target_method);
		g_hash_table_remove (td->patchsite_hash, ins);

		mini_tiered_record_callsite (start_ip, target_method, TIERED_PATCH_KIND_INTERP);

		int size = mono_interp_oplen [ins->opcode];
		int jit_call2_size = mono_interp_oplen [MINT_JIT_CALL2];

		g_assert (size < jit_call2_size);

		// Emit the rest of the data
		for (int i = 0; i < size - 1; i++)
			*ip++ = ins->data [i];

		/* intentional padding so we can patch a MINT_JIT_CALL2 here */
		for (int i = size - 1; i < (jit_call2_size - 1); i++)
			*ip++ = MINT_NIY;
#endif
	} else if (opcode >= MINT_MOV_8_2 && opcode <= MINT_MOV_8_4) {
		// This instruction is not marked as operating on any vars, all instruction slots are
		// actually vars. Resolve their offset
		int num_vars = mono_interp_oplen [opcode] - 1;
		for (int i = 0; i < num_vars; i++)
			*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->data [i]));
	} else if (opcode == MINT_MOV_STACK_UNOPT) {
		g_assert (!td->optimized);
		// ins->data [0] represents the stack offset of the call args (within the execution stack)
		*ip++ = GINT_TO_UINT16 (td->param_area_offset + ins->data [0]);
		*ip++ = GINT_TO_UINT16 (ins->data [1]);
		*ip++ = GINT_TO_UINT16 (ins->data [2]);
	} else if (opcode == MINT_INTRINS_MARVIN_BLOCK) {
		// Generated only in unoptimized code
		int var0 = ins->sregs [0];
		int var1 = ins->sregs [1];
		g_assert (var0 == ins->data [0]);
		g_assert (var1 == ins->data [1]);

		*ip++ = GINT_TO_UINT16 (get_var_offset (td, var0));
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, var1));
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, var0));
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, var1));
	} else if (opcode == MINT_INTRINS_MARVIN_BLOCK_SSA1) {
		int var0 = ins->sregs [0];
		int var1 = ins->sregs [1];
		g_assert (ins->next->opcode == MINT_INTRINS_MARVIN_BLOCK_SSA2);
		g_assert (var0 == ins->next->sregs [0]);
		g_assert (var1 == ins->next->sregs [1]);
		int dvar0 = ins->dreg;
		int dvar1 = ins->next->dreg;
		ip [-1] = MINT_INTRINS_MARVIN_BLOCK;
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, var0));
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, var1));
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, dvar0));
		*ip++ = GINT_TO_UINT16 (get_var_offset (td, dvar1));

		ins->next->opcode = MINT_NOP;
		InterpInst *next = interp_next_ins (ins);
		// We ensure that next->sregs [0] is not used again, it will no longer be set by intrinsic
		if (next->opcode == MINT_MOV_4 && td->var_values && td->var_values [next->sregs [0]].ref_count == 1) {
			if (next->sregs [0] == dvar0) {
				ip [-2] = GINT_TO_UINT16 (get_var_offset (td, next->dreg));
				next->opcode = MINT_NOP;
			} else if (next->sregs [0] == dvar1) {
				ip [-1] = GINT_TO_UINT16 (get_var_offset (td, next->dreg));
				next->opcode = MINT_NOP;
			}
		}
	} else {
opcode_emit:
		if (mono_interp_op_dregs [opcode])
			*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->dreg));

		if (mono_interp_op_sregs [opcode]) {
			for (int i = 0; i < mono_interp_op_sregs [opcode]; i++) {
				if (ins->sregs [i] == MINT_CALL_ARGS_SREG) {
					int offset = td->param_area_offset + ins->info.call_info->call_offset;
					*ip++ = GINT_TO_UINT16 (offset);
				} else {
					*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->sregs [i]));
				}
			}
		} else if (opcode == MINT_LDLOCA_S) {
			// This opcode receives a local but it is not viewed as a sreg since we don't load the value
			*ip++ = GINT_TO_UINT16 (get_var_offset (td, ins->sregs [0]));
		}

		int left = interp_get_ins_length (ins) - GPTRDIFF_TO_INT(ip - start_ip);
		// Emit the rest of the data
		for (int i = 0; i < left; i++)
			*ip++ = ins->data [i];
	}
	mono_interp_stats.emitted_instructions++;
	return ip;
}

static int
add_patchpoint_data (TransformData *td, int patchpoint_data_index, int native_offset, int key)
{
	if (td->optimized) {
		td->patchpoint_data [patchpoint_data_index++] = key;
		td->patchpoint_data [patchpoint_data_index++] = native_offset;
	} else {
		td->patchpoint_data [patchpoint_data_index++] = native_offset;
		td->patchpoint_data [patchpoint_data_index++] = key;
	}
	return patchpoint_data_index;
}

// Generates the final code, after we are done with all the passes
static void
generate_compacted_code (InterpMethod *rtm, TransformData *td)
{
	guint16 *ip;
	int size;
	int patchpoint_data_index = 0;
	td->relocs = g_ptr_array_new ();
	InterpBasicBlock *bb;

	// This iteration could be avoided at the cost of less precise size result, following
	// super instruction pass
	size = interp_compute_native_offset_estimates (td);

	// Generate the compacted stream of instructions
	td->new_code = ip = (guint16*)imethod_alloc0 (td, size * sizeof (guint16));

	if (td->patchpoint_data_n) {
		g_assert (mono_interp_tiering_enabled ());
		td->patchpoint_data = (int*)imethod_alloc0 (td, (td->patchpoint_data_n * 2 + 1) * sizeof (int));
		td->patchpoint_data [td->patchpoint_data_n * 2] = G_MAXINT32;
	}

	for (bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins = bb->first_ins;
		bb->native_offset = GPTRDIFF_TO_INT (ip - td->new_code);
		g_assert (bb->native_offset <= bb->native_offset_estimate);
		td->cbb = bb;

		if (bb->patchpoint_data)
			patchpoint_data_index = add_patchpoint_data (td, patchpoint_data_index, bb->native_offset, bb->index);
		if (!td->optimized && bb->patchpoint_bb) {
			// Add patchpoint in unoptimized method
			*ip++ = MINT_TIER_PATCHPOINT;
			*ip++ = (guint16)bb->index;
		}

		while (ins) {
			if (ins->opcode == MINT_TIER_PATCHPOINT_DATA) {
				int native_offset = (int)(ip - td->new_code);
				patchpoint_data_index = add_patchpoint_data (td, patchpoint_data_index, native_offset, -ins->data [0]);
			} else {
				ip = emit_compacted_instruction (td, ip, ins);
			}
			ins = ins->next;
		}
	}
	td->new_code_end = ip;
	td->in_offsets [td->header->code_size] = GPTRDIFF_TO_INT (td->new_code_end - td->new_code);

	// Patch all branches. This might be useless since we iterate once anyway to compute the size
	// of the generated code. We could compute the native offset of each basic block then.
	handle_relocations (td);

	g_ptr_array_free (td->relocs, TRUE);
}

/*
 * Very few methods have localloc. Handle it separately to not impact performance
 * of other methods. We replace the normal return opcodes with opcodes that also
 * reset the localloc stack.
 */
static void
interp_fix_localloc_ret (TransformData *td)
{
	g_assert (td->has_localloc);
	for (InterpBasicBlock *bb = td->entry_bb; bb != NULL; bb = bb->next_bb) {
		InterpInst *ins = bb->first_ins;
		while (ins) {
			if (ins->opcode >= MINT_RET && ins->opcode <= MINT_RET_VT) {
				ins->opcode += MINT_RET_LOCALLOC - MINT_RET;
			} else if (ins->opcode >= MINT_RET_I1 && ins->opcode <= MINT_RET_U2) {
				int mt = ins->opcode - MINT_RET_I1;
				int var = ins->sregs [0];
				int opcode;
				switch (mt) {
					case MINT_TYPE_I1: opcode = MINT_CONV_I1_I4; break;
					case MINT_TYPE_U1: opcode = MINT_CONV_U1_I4; break;
					case MINT_TYPE_I2: opcode = MINT_CONV_I2_I4; break;
					case MINT_TYPE_U2: opcode = MINT_CONV_U2_I4; break;
					default: g_assert_not_reached ();
				}

				td->cbb = bb;

				// This path should be rare enough not to bother with specific opcodes
				// Add implicit conversion and then return
				interp_clear_ins (ins);
				ins = interp_insert_ins (td, ins, opcode);
				interp_ins_set_dreg (ins, var);
				interp_ins_set_sreg (ins, var);

				ins = interp_insert_ins (td, ins, MINT_RET_LOCALLOC);
				interp_ins_set_dreg (ins, var);
				interp_ins_set_sreg (ins, var);
			}
			ins = ins->next;
		}
	}
}

static void
interp_squash_initlocals (TransformData *td)
{
	InterpInst *last_initlocal = NULL;
	int last_start = 0, last_end = 0;

	for (InterpInst *ins = td->entry_bb->first_ins; ins != NULL; ins = ins->next) {
		// Once we reach the real method code, we are finished with this pass
		if (ins->il_offset != -1)
			break;
		if (ins->opcode == MINT_INITLOCAL) {
			if (!last_initlocal) {
				last_initlocal = ins;
				last_start = get_var_offset (td, ins->dreg);
				last_end = last_start + (int)ins->data [0];
			} else {
				int new_start = get_var_offset (td, ins->dreg);
				// We allow a maximum of 64 bytes of redundant memset when squashing initlocals
				if (new_start >= last_end && new_start <= (last_end + 64)) {
					last_initlocal->opcode = MINT_INITLOCALS;
					last_initlocal->data [0] = GINT_TO_UINT16 (last_start);
					last_end = new_start + ins->data [0];
					last_initlocal->data [1] = GINT_TO_UINT16 (last_end - last_start);
					interp_clear_ins (ins);
				} else {
					last_initlocal = ins;
					last_start = get_var_offset (td, ins->dreg);
					last_end = last_start + ins->data [0];
				}
			}
		}
	}
}

static int
get_native_offset (TransformData *td, int il_offset)
{
	// We can't access offset_to_bb for header->code_size IL offset. Also, offset_to_bb
	// is not set for dead bblocks at method end.
	if (GINT_TO_UINT32(il_offset) < td->header->code_size && td->offset_to_bb [il_offset]) {
		InterpBasicBlock *bb = td->offset_to_bb [il_offset];
		g_assert (!bb->dead);
		return bb->native_offset;
	} else {
		return GPTRDIFF_TO_INT (td->new_code_end - td->new_code);
	}
}

static void
generate (MonoMethod *method, MonoMethodHeader *header, InterpMethod *rtm, MonoGenericContext *generic_context, MonoError *error)
{
	TransformData transform_data;
	TransformData *td;
	gboolean retry_compilation = FALSE;
	static gboolean verbose_method_inited;
	static char* verbose_method_name;

	if (!verbose_method_inited) {
		verbose_method_name = g_getenv ("MONO_VERBOSE_METHOD");
		verbose_method_inited = TRUE;
	}

retry:
	mono_interp_pgo_generate_start ();
	memset (&transform_data, 0, sizeof(transform_data));
	td = &transform_data;

	td->method = method;
	td->rtm = rtm;
	td->code_size = header->code_size;
	td->header = header;
	td->max_code_size = td->code_size;
	td->in_offsets = (int*)g_malloc0((header->code_size + 1) * sizeof(int));
	td->clause_indexes = (int*)g_malloc (header->code_size * sizeof (int));
	td->mempool = mono_mempool_new ();
	td->mem_manager = m_method_get_mem_manager (method);
	td->n_data_items = 0;
	td->max_data_items = 0;
	td->dummy_var = -1;
	td->data_items = NULL;
	td->data_hash = g_hash_table_new (NULL, NULL);
#ifdef ENABLE_EXPERIMENT_TIERED
	td->patchsite_hash = g_hash_table_new (NULL, NULL);
#endif
	td->gen_seq_points = !mini_debug_options.no_seq_points_compact_data || mini_debug_options.gen_sdb_seq_points;
	td->gen_sdb_seq_points = mini_debug_options.gen_sdb_seq_points;
	td->seq_points = g_ptr_array_new ();
	td->verbose_level = mono_interp_traceopt;
	td->prof_coverage = mono_profiler_coverage_instrumentation_enabled (method);
	if (retry_compilation) {
		// Optimizing the method can lead to deadce and better var offset allocation
		// reducing the likelihood of local space overflow.
		td->optimized = rtm->optimized = TRUE;
		td->disable_inlining = TRUE;
	} else {
		td->optimized = rtm->optimized;
		td->disable_inlining = !td->optimized;
	}
	rtm->data_items = td->data_items;

	if (td->prof_coverage)
		td->coverage_info = mono_profiler_coverage_alloc (method, header->code_size);

	if (verbose_method_name) {
		const char *name = verbose_method_name;

		if ((strchr (name, '.') > name) || strchr (name, ':')) {
			MonoMethodDesc *desc;

			desc = mono_method_desc_new (name, TRUE);
			if (mono_method_desc_full_match (desc, method)) {
				td->verbose_level = 4;
			}
			mono_method_desc_free (desc);
		} else {
			if (strcmp (method->name, name) == 0)
				td->verbose_level = 4;
		}
	}

	interp_method_compute_offsets (td, rtm, mono_method_signature_internal (method), header, error);
	goto_if_nok (error, exit);

	td->stack = (StackInfo*)g_malloc0 ((header->max_stack + 1) * sizeof (td->stack [0]));
	td->stack_capacity = header->max_stack + 1;
	td->sp = td->stack;
	td->max_stack_height = 0;
	td->line_numbers = g_array_new (FALSE, TRUE, sizeof (MonoDebugLineNumberEntry));
	td->current_il_offset = -1;

	generate_code (td, method, header, generic_context, error);
	goto_if_nok (error, exit);

	// Any newly created instructions will have undefined il_offset
	td->current_il_offset = -1;

	g_assert (td->inline_depth == 0);

	td->rtm->is_verbose = td->verbose_level > 0;

	if (td->has_localloc)
		interp_fix_localloc_ret (td);

	if (td->verbose_level) {
		g_print ("\nUnoptimized IR:\n");
		mono_interp_print_td_code (td);
	}

	if (td->optimized) {
		MONO_TIME_TRACK (mono_interp_stats.optimize_time, interp_optimize_code (td));
		interp_alloc_offsets (td);
		interp_squash_initlocals (td);
#if HOST_BROWSER
		if (mono_interp_opt & INTERP_OPT_JITERPRETER)
			jiterp_insert_entry_points (rtm, td);
#endif
	}

	generate_compacted_code (rtm, td);

	if (td->optimized) {
		// Offset allocator and compacted code generation use computed ref counts
		// from var values. We have to free this table later here.
		if (td->var_values != NULL) {
			g_free (td->var_values);
			td->var_values = NULL;
		}
	}

	if (td->total_locals_size >= G_MAXUINT16) {
		if (td->disable_inlining && td->optimized) {
			char *name = mono_method_get_full_name (method);
			char *msg = g_strdup_printf ("Unable to run method '%s': locals size too big.", name);
			g_free (name);
			mono_error_set_generic_error (error, "System", "InvalidProgramException", "%s", msg);
			g_free (msg);
			retry_compilation = FALSE;
			goto exit;
		} else {
			// We give the method another chance to compile with inlining disabled and optimization enabled
			if (td->verbose_level)
				g_print ("Local space overflow. Retrying compilation\n");
			retry_compilation = TRUE;
			goto exit;
		}
	} else {
		retry_compilation = FALSE;
	}

	if (td->verbose_level) {
		g_print ("Runtime method: %s %p\n", mono_method_full_name (method, TRUE), rtm);
		g_print ("Locals size %d\n", td->total_locals_size);
		g_print ("Calculated stack height: %d, stated height: %d\n", td->max_stack_height, header->max_stack);
		interp_dump_code (td->new_code, td->new_code_end, td->data_items);
	}

	/* Check if we use excessive stack space */
	if (td->max_stack_height > header->max_stack * 3u && header->max_stack > 16)
		g_warning ("Excessive stack space usage for method %s, %d/%d", method->name, td->max_stack_height, header->max_stack);

	guint32 code_len_u8, code_len_u16;
	code_len_u8 = GPTRDIFF_TO_UINT32 ((guint8 *) td->new_code_end - (guint8 *) td->new_code);
	code_len_u16 = GPTRDIFF_TO_UINT32 (td->new_code_end - td->new_code);

	rtm->clauses = (MonoExceptionClause*)imethod_alloc0 (td, header->num_clauses * sizeof (MonoExceptionClause));
	memcpy (rtm->clauses, header->clauses, header->num_clauses * sizeof(MonoExceptionClause));
	rtm->code = (gushort*)td->new_code;
	rtm->init_locals = header->init_locals;
	rtm->num_clauses = header->num_clauses;
	for (guint i = 0; i < header->num_clauses; i++) {
		MonoExceptionClause *c = rtm->clauses + i;
		int end_off = c->try_offset + c->try_len;
		c->try_offset = get_native_offset (td, c->try_offset);
		c->try_len = get_native_offset (td, end_off) - c->try_offset;
		g_assert ((c->try_offset + c->try_len) <= code_len_u16);
		end_off = c->handler_offset + c->handler_len;
		c->handler_offset = get_native_offset (td, c->handler_offset);
		c->handler_len = get_native_offset (td, end_off) - c->handler_offset;
		g_assert (c->handler_len >= 0 && (c->handler_offset + c->handler_len) <= code_len_u16);
		if (c->flags & MONO_EXCEPTION_CLAUSE_FILTER)
			c->data.filter_offset = get_native_offset (td, c->data.filter_offset);
	}
	// When optimized (using the var offset allocator), total_locals_size contains also the param area.
	// When unoptimized, the param area is stored in the same order, within the IL execution stack.
	g_assert (!td->optimized || !td->max_stack_size);
	rtm->alloca_size = td->total_locals_size + td->max_stack_size;
	g_assert ((rtm->alloca_size % MINT_STACK_ALIGNMENT) == 0);
	rtm->locals_size = td->param_area_offset;
	// FIXME: Can't allocate this using imethod_alloc0 as its registered with mono_interp_register_imethod_data_items ()
	//rtm->data_items = (gpointer*)imethod_alloc0 (td, td->n_data_items * sizeof (td->data_items [0]));
	rtm->data_items = (gpointer*)mono_mem_manager_alloc0 (td->mem_manager, td->n_data_items * sizeof (td->data_items [0]));
	memcpy (rtm->data_items, td->data_items, td->n_data_items * sizeof (td->data_items [0]));

	mono_interp_register_imethod_data_items (rtm->data_items, td->imethod_items);
	rtm->patchpoint_data = td->patchpoint_data;

	/* Save debug info */
	interp_save_debug_info (rtm, header, td, td->line_numbers);

	/* Create a MonoJitInfo for the interpreted method by creating the interpreter IR as the native code. */
	int jinfo_len;
	jinfo_len = mono_jit_info_size ((MonoJitInfoFlags)0, header->num_clauses, 0);
	MonoJitInfo *jinfo;
	jinfo = (MonoJitInfo *)imethod_alloc0 (td, jinfo_len);
	jinfo->is_interp = 1;
	rtm->jinfo = jinfo;
	mono_jit_info_init (jinfo, method, (guint8*)rtm->code, code_len_u8, (MonoJitInfoFlags)0, header->num_clauses, 0);
	for (guint32 i = 0; i < jinfo->num_clauses; ++i) {
		MonoJitExceptionInfo *ei = &jinfo->clauses [i];
		MonoExceptionClause *c = rtm->clauses + i;

		ei->flags = c->flags;
		ei->try_start = (guint8*)(rtm->code + c->try_offset);
		ei->try_end = (guint8*)(rtm->code + c->try_offset + c->try_len);
		ei->handler_start = (guint8*)(rtm->code + c->handler_offset);
		ei->exvar_offset = rtm->clause_data_offsets [i];
		if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			ei->data.filter = (guint8*)(rtm->code + c->data.filter_offset);
		} else if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
			ei->data.handler_end = (guint8*)(rtm->code + c->handler_offset + c->handler_len);
		} else {
			ei->data.catch_class = c->data.catch_class;
		}
	}

	save_seq_points (td, jinfo);
#ifdef ENABLE_EXPERIMENT_TIERED
	/* debugging aid, it makes `mono_pmip` work. */
	mono_jit_info_table_add (jinfo);
#endif

exit:
	g_free (td->in_offsets);
	g_free (td->clause_indexes);
	g_free (td->data_items);
	g_free (td->stack);
	g_free (td->vars);
	g_free (td->local_ref_count);
	g_hash_table_destroy (td->data_hash);
#ifdef ENABLE_EXPERIMENT_TIERED
	g_hash_table_destroy (td->patchsite_hash);
#endif
	g_ptr_array_free (td->seq_points, TRUE);
	if (td->line_numbers)
		g_array_free (td->line_numbers, TRUE);
	g_slist_free (td->imethod_items);
	mono_mempool_destroy (td->mempool);
	mono_interp_pgo_generate_end ();
	if (retry_compilation)
		goto retry;
}

gboolean
mono_test_interp_generate_code (TransformData *td, MonoMethod *method, MonoMethodHeader *header, MonoGenericContext *generic_context, MonoError *error)
{
	return generate_code (td, method, header, generic_context, error);
}

#ifdef ENABLE_EXPERIMENT_TIERED
static gboolean
tiered_patcher (MiniTieredPatchPointContext *ctx, gpointer patchsite)
{
	MonoMethod *m = ctx->target_method;

	if (!jit_call2_supported (m, mono_method_signature_internal (m)))
		return FALSE;

	/* TODO: Force compilation here. Currently the JIT will be invoked upon
	 *       first execution of `MINT_JIT_CALL2`. */
	InterpMethod *rmethod = mono_interp_get_imethod (cm);

	guint16 *ip = ((guint16 *) patchsite);
	*ip++ = MINT_JIT_CALL2;
	/* FIXME: this only works on 64bit */
	WRITE64 (ip, &rmethod);
	mono_memory_barrier ();

	return TRUE;
}
#endif


void
mono_interp_transform_init (void)
{
#ifdef ENABLE_EXPERIMENT_TIERED
	mini_tiered_register_callsite_patcher (tiered_patcher, TIERED_PATCH_KIND_INTERP);
#endif
}

void
mono_interp_transform_method (InterpMethod *imethod, ThreadContext *context, MonoError *error)
{
	MonoMethod *method = imethod->method;
	MonoMethodHeader *header = NULL;
	MonoVTable *method_class_vt;
	MonoGenericContext *generic_context = NULL;
	InterpMethod tmp_imethod;
	InterpMethod *real_imethod;

	error_init (error);

	mono_metadata_update_thread_expose_published ();

	if (mono_class_is_open_constructed_type (m_class_get_byval_arg (method->klass))) {
		mono_error_set_invalid_operation (error, "%s", "Could not execute the method because the containing type is not fully instantiated.");
		return;
	}

	// g_printerr ("TRANSFORM(0x%016lx): begin %s::%s\n", mono_thread_current (), method->klass->name, method->name);
	method_class_vt = mono_class_vtable_checked (imethod->method->klass, error);
	return_if_nok (error);

	if (!method_class_vt->initialized) {
		mono_runtime_class_init_full (method_class_vt, error);
		return_if_nok (error);
	}

	MONO_PROFILER_RAISE (jit_begin, (method));

	if (mono_method_signature_internal (method)->is_inflated)
		generic_context = mono_method_get_context (method);
	else {
		MonoGenericContainer *generic_container = mono_method_get_generic_container (method);
		if (generic_container)
			generic_context = &generic_container->context;
	}

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		MonoMethod *nm = NULL;
		if (imethod->transformed) {
			MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));
			return;
		}

		/* assumes all internal calls with an array this are built in... */
		if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL && (! mono_method_signature_internal (method)->hasthis || m_class_get_rank (method->klass) == 0)) {
			nm = mono_marshal_get_native_wrapper (method, FALSE, FALSE);
		} else {
			const char *name = method->name;
			if (m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class) {
				if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
					MonoJitICallInfo *mi = &mono_get_jit_icall_info ()->ves_icall_mono_delegate_ctor_interp;
					nm = mono_marshal_get_icall_wrapper (mi, TRUE);
				} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
					/*
					 * Usually handled during transformation of the caller, but
					 * when the caller is handled by another execution engine
					 * (for example fullAOT) we need to handle it here. That's
					 * known to be wrong in cases where the reference to
					 * `MonoDelegate` would be needed (FIXME).
					 */
					nm = mono_marshal_get_delegate_invoke (method, NULL);
				} else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
					nm = mono_marshal_get_delegate_begin_invoke (method);
				} else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
					nm = mono_marshal_get_delegate_end_invoke (method);
				}
			}
			if (nm == NULL)
				g_assert_not_reached ();
		}
		if (nm == NULL) {
			MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
			jit_mm_lock (jit_mm);
			imethod->alloca_size = sizeof (stackval); /* for tracing */
			mono_memory_barrier ();
			imethod->transformed = TRUE;
			mono_interp_stats.methods_transformed++;
			jit_mm_unlock (jit_mm);
			MONO_PROFILER_RAISE (jit_done, (method, NULL));
			return;
		}
		method = nm;
		header = interp_method_get_header (nm, error);
		return_if_nok (error);
	}

	int accessor_kind = -1;
	char *member_name = NULL;
	if (!header && mono_method_get_unsafe_accessor_attr_data (method, &accessor_kind, &member_name, error)) {
		method = mono_marshal_get_unsafe_accessor_wrapper (method, (MonoUnsafeAccessorKind)accessor_kind, member_name);
		g_assert (method);
	}

	if (!header) {
		header = mono_method_get_header_checked (method, error);
		return_if_nok (error);
	}

	/* Make modifications to a copy of imethod, copy them back inside the lock */
	real_imethod = imethod;
	memcpy (&tmp_imethod, imethod, sizeof (InterpMethod));
	imethod = &tmp_imethod;

	MONO_TIME_TRACK (mono_interp_stats.transform_time, generate (method, header, imethod, generic_context, error));

	mono_metadata_free_mh (header);

	return_if_nok (error);

	/* Copy changes back */
	imethod = real_imethod;

	MonoJitMemoryManager *jit_mm = get_default_jit_mm ();
	jit_mm_lock (jit_mm);
	if (!imethod->transformed) {
		// Ignore the first two fields which are unchanged. next_jit_code_hash shouldn't
		// be modified because it is racy with internal hash table insert.
		const int start_offset = 2 * sizeof (gpointer);
		memcpy ((char*)imethod + start_offset, (char*)&tmp_imethod + start_offset, sizeof (InterpMethod) - start_offset);
		mono_memory_barrier ();
		imethod->transformed = TRUE;
		mono_interp_stats.methods_transformed++;
		mono_atomic_fetch_add_i32 (&mono_jit_stats.methods_with_interp, 1);

		// FIXME Publishing of seq points seems to be racy with tiereing. We can have both tiered and untiered method
		// running at the same time. We could therefore get the optimized imethod seq points for the unoptimized method.
		gpointer seq_points = g_hash_table_lookup (jit_mm->seq_points, imethod->method);
		if (!seq_points || seq_points != imethod->jinfo->seq_points)
			g_hash_table_replace (jit_mm->seq_points, imethod->method, imethod->jinfo->seq_points);
	}
	jit_mm_unlock (jit_mm);

	if (mono_stats_method_desc && mono_method_desc_full_match (mono_stats_method_desc, imethod->method)) {
		g_printf ("Printing runtime stats at method: %s\n", mono_method_get_full_name (imethod->method));
		mono_runtime_print_stats ();
	}

	// FIXME: Add a different callback ?
	MONO_PROFILER_RAISE (jit_done, (method, imethod->jinfo));
}

#if HOST_BROWSER

InterpInst*
mono_jiterp_insert_ins (TransformData *td, InterpInst *prev_ins, int opcode)
{
	return interp_insert_ins (td, prev_ins, opcode);
}

#endif

#ifdef INTERP_ENABLE_SIMD
#include "transform-simd.c"
#endif
