/**
 * Emit memory access for the front-end.
 *
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include <mono/metadata/gc-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/utils/mono-memory-model.h>

#include "mini.h"
#include "mini-runtime.h"
#include "ir-emit.h"
#include "jit-icalls.h"

#define MAX_INLINE_COPIES 16
#define MAX_INLINE_COPY_SIZE 10000

void 
mini_emit_memset (MonoCompile *cfg, int destreg, int offset, int size, int val, int align)
{
	int val_reg;

	/*FIXME arbitrary hack to avoid unbound code expansion.*/
	g_assert (size < MAX_INLINE_COPY_SIZE);
	g_assert (val == 0);
	g_assert (align > 0);

	if ((size <= SIZEOF_REGISTER) && (size <= align)) {
		switch (size) {
		case 1:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI1_MEMBASE_IMM, destreg, offset, val);
			return;
		case 2:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI2_MEMBASE_IMM, destreg, offset, val);
			return;
		case 4:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI4_MEMBASE_IMM, destreg, offset, val);
			return;
#if SIZEOF_REGISTER == 8
		case 8:
			MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STOREI8_MEMBASE_IMM, destreg, offset, val);
			return;
#endif
		}
	}

	val_reg = alloc_preg (cfg);

	if (SIZEOF_REGISTER == 8)
		MONO_EMIT_NEW_I8CONST (cfg, val_reg, val);
	else
		MONO_EMIT_NEW_ICONST (cfg, val_reg, val);

	if (align < TARGET_SIZEOF_VOID_P) {
		if (align % 2 == 1)
			goto set_1;
		if (align % 4 == 2)
			goto set_2;
		if (TARGET_SIZEOF_VOID_P == 8 && align % 8 == 4)
			goto set_4;
	}

	//Unaligned offsets don't naturaly happen in the runtime, so it's ok to be conservative in how we copy
	//We assume that input src and dest are be aligned to `align` so offset just worsen it
	int offsets_mask;
	offsets_mask = offset & 0x7; //we only care about the misalignment part
	if (offsets_mask) {
		if (offsets_mask % 2 == 1)
			goto set_1;
		if (offsets_mask % 4 == 2)
			goto set_2;
		if (TARGET_SIZEOF_VOID_P == 8 && offsets_mask % 8 == 4)
			goto set_4;
	}

	if (SIZEOF_REGISTER == 8) {
		while (size >= 8) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, destreg, offset, val_reg);
			offset += 8;
			size -= 8;
		}
	}

set_4:
	while (size >= 4) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, val_reg);
		offset += 4;
		size -= 4;
	}


set_2:
	while (size >= 2) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, offset, val_reg);
		offset += 2;
		size -= 2;
	}

set_1:
	while (size >= 1) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, offset, val_reg);
		offset += 1;
		size -= 1;
	}
}

void 
mini_emit_memcpy (MonoCompile *cfg, int destreg, int doffset, int srcreg, int soffset, int size, int align)
{
	int cur_reg;

	/*FIXME arbitrary hack to avoid unbound code expansion.*/
	g_assert (size < MAX_INLINE_COPY_SIZE);
	g_assert (align > 0);

	if (align < TARGET_SIZEOF_VOID_P) {
		if (align == 4)
			goto copy_4;
		if (align == 2)
			goto copy_2;
		goto copy_1;
	}

	//Unaligned offsets don't naturaly happen in the runtime, so it's ok to be conservative in how we copy
	//We assume that input src and dest are be aligned to `align` so offset just worsen it
	int offsets_mask;
	offsets_mask = (doffset | soffset) & 0x7; //we only care about the misalignment part
	if (offsets_mask) {
		if (offsets_mask % 2 == 1)
			goto copy_1;
		if (offsets_mask % 4 == 2)
			goto copy_2;
		if (TARGET_SIZEOF_VOID_P == 8 && offsets_mask % 8 == 4)
			goto copy_4;
	}


	if (SIZEOF_REGISTER == 8) {
		while (size >= 8) {
			cur_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI8_MEMBASE, cur_reg, srcreg, soffset);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, destreg, doffset, cur_reg);
			doffset += 8;
			soffset += 8;
			size -= 8;
		}
	}

copy_4:
	while (size >= 4) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 4;
		soffset += 4;
		size -= 4;
	}

copy_2:
	while (size >= 2) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI2_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 2;
		soffset += 2;
		size -= 2;
	}

copy_1:
	while (size >= 1) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 1;
		soffset += 1;
		size -= 1;
	}
}

static void
mini_emit_memcpy_internal (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoInst *size_ins, int size, int align)
{
	/* FIXME: Optimize the case when src/dest is OP_LDADDR */

	/* We can't do copies at a smaller granule than the provided alignment */
	if (size_ins || (size / align > MAX_INLINE_COPIES) || !(cfg->opt & MONO_OPT_INTRINS)) {
		MonoInst *iargs [3];
		iargs [0] = dest;
		iargs [1] = src;

		if (!size_ins)
			EMIT_NEW_ICONST (cfg, size_ins, size);
		iargs [2] = size_ins;
		mono_emit_method_call (cfg, mini_get_memcpy_method (), iargs, NULL);
	} else {
		mini_emit_memcpy (cfg, dest->dreg, 0, src->dreg, 0, size, align);
	}
}

static void
mini_emit_memset_internal (MonoCompile *cfg, MonoInst *dest, MonoInst *value_ins, int value, MonoInst *size_ins, int size, int align)
{
	/* FIXME: Optimize the case when dest is OP_LDADDR */

	/* We can't do copies at a smaller granule than the provided alignment */
	if (value_ins || size_ins || value != 0 || (size / align > MAX_INLINE_COPIES) || !(cfg->opt & MONO_OPT_INTRINS)) {
		MonoInst *iargs [3];
		iargs [0] = dest;

		if (!value_ins)
			EMIT_NEW_ICONST (cfg, value_ins, value);
		iargs [1] = value_ins;

		if (!size_ins)
			EMIT_NEW_ICONST (cfg, size_ins, size);
		iargs [2] = size_ins;

		mono_emit_method_call (cfg, mini_get_memset_method (), iargs, NULL);
	} else {
		mini_emit_memset (cfg, dest->dreg, 0, size, value, align);
	}
}

static void
mini_emit_memcpy_const_size (MonoCompile *cfg, MonoInst *dest, MonoInst *src, int size, int align)
{
	mini_emit_memcpy_internal (cfg, dest, src, NULL, size, align);
}

static void
mini_emit_memset_const_size (MonoCompile *cfg, MonoInst *dest, int value, int size, int align)
{
	mini_emit_memset_internal (cfg, dest, NULL, value, NULL, size, align);
}


static void
create_write_barrier_bitmap (MonoCompile *cfg, MonoClass *klass, unsigned *wb_bitmap, int offset)
{
	MonoClassField *field;
	gpointer iter = NULL;

	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		int foffset;

		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		foffset = m_class_is_valuetype (klass) ? field->offset - MONO_ABI_SIZEOF (MonoObject): field->offset;
		if (mini_type_is_reference (mono_field_get_type_internal (field))) {
			g_assert ((foffset % TARGET_SIZEOF_VOID_P) == 0);
			*wb_bitmap |= 1 << ((offset + foffset) / TARGET_SIZEOF_VOID_P);
		} else {
			MonoClass *field_class = mono_class_from_mono_type_internal (field->type);
			if (cfg->gshared)
				field_class = mono_class_from_mono_type_internal (mini_get_underlying_type (m_class_get_byval_arg (field_class)));
			if (m_class_has_references (field_class))
				create_write_barrier_bitmap (cfg, field_class, wb_bitmap, offset + foffset);
		}
	}
}

static gboolean
mini_emit_wb_aware_memcpy (MonoCompile *cfg, MonoClass *klass, MonoInst *iargs[4], int size, int align)
{
	int dest_ptr_reg, tmp_reg, destreg, srcreg, offset;
	unsigned need_wb = 0;

	if (align == 0)
		align = 4;

	/*types with references can't have alignment smaller than sizeof(void*) */
	if (align < TARGET_SIZEOF_VOID_P)
		return FALSE;

	if (size > 5 * TARGET_SIZEOF_VOID_P)
		return FALSE;

	create_write_barrier_bitmap (cfg, klass, &need_wb, 0);

	destreg = iargs [0]->dreg;
	srcreg = iargs [1]->dreg;
	offset = 0;

	dest_ptr_reg = alloc_preg (cfg);
	tmp_reg = alloc_preg (cfg);

	/*tmp = dreg*/
	EMIT_NEW_UNALU (cfg, iargs [0], OP_MOVE, dest_ptr_reg, destreg);

	if ((need_wb & 0x1) && !mini_debug_options.weak_memory_model)
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_REL);

	while (size >= TARGET_SIZEOF_VOID_P) {
		MonoInst *load_inst;
		MONO_INST_NEW (cfg, load_inst, OP_LOAD_MEMBASE);
		load_inst->dreg = tmp_reg;
		load_inst->inst_basereg = srcreg;
		load_inst->inst_offset = offset;
		MONO_ADD_INS (cfg->cbb, load_inst);

		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREP_MEMBASE_REG, dest_ptr_reg, 0, tmp_reg);

		if (need_wb & 0x1)
			mini_emit_write_barrier (cfg, iargs [0], load_inst);

		offset += TARGET_SIZEOF_VOID_P;
		size -= TARGET_SIZEOF_VOID_P;
		need_wb >>= 1;

		/*tmp += sizeof (void*)*/
		if (size >= TARGET_SIZEOF_VOID_P) {
			NEW_BIALU_IMM (cfg, iargs [0], OP_PADD_IMM, dest_ptr_reg, dest_ptr_reg, TARGET_SIZEOF_VOID_P);
			MONO_ADD_INS (cfg->cbb, iargs [0]);
		}
	}

	/* Those cannot be references since size < sizeof (void*) */
	while (size >= 4) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, tmp_reg, srcreg, offset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, tmp_reg);
		offset += 4;
		size -= 4;
	}

	while (size >= 2) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI2_MEMBASE, tmp_reg, srcreg, offset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, offset, tmp_reg);
		offset += 2;
		size -= 2;
	}

	while (size >= 1) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, tmp_reg, srcreg, offset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, offset, tmp_reg);
		offset += 1;
		size -= 1;
	}

	return TRUE;
}

static void
mini_emit_memory_copy_internal (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoClass *klass, int explicit_align, gboolean native,
								gboolean stack_store)
{
	MonoInst *iargs [4];
	int size;
	guint32 align = 0;
	MonoInst *size_ins = NULL;
	MonoInst *memcpy_ins = NULL;

	g_assert (klass);
	/*
	Fun fact about @native. It's false that @klass will have no ref when @native is true.
	This happens in pinvoke2. What goes is that marshal.c uses CEE_MONO_LDOBJNATIVE and pass klass.
	The actual stuff being copied will have no refs, but @klass might.
	This means we can't assert !(klass->has_references && native).
	*/

	if (cfg->gshared)
		klass = mono_class_from_mono_type_internal (mini_get_underlying_type (m_class_get_byval_arg (klass)));

	/*
	 * This check breaks with spilled vars... need to handle it during verification anyway.
	 * g_assert (klass && klass == src->klass && klass == dest->klass);
	 */

	if (mini_is_gsharedvt_klass (klass)) {
		g_assert (!native);
		size_ins = mini_emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_VALUE_SIZE);
		memcpy_ins = mini_emit_get_gsharedvt_info_klass (cfg, klass, MONO_RGCTX_INFO_MEMCPY);
	}

	if (native)
		size = mono_class_native_size (klass, &align);
	else
		size = mono_class_value_size (klass, &align);

	if (!align)
		align = TARGET_SIZEOF_VOID_P;
	if (explicit_align)
		align = explicit_align;

	if (mini_type_is_reference (m_class_get_byval_arg (klass))) { // Refs *MUST* be naturally aligned
		MonoInst *store, *load;
		int dreg = alloc_ireg_ref (cfg);

		NEW_LOAD_MEMBASE (cfg, load, OP_LOAD_MEMBASE, dreg, src->dreg, 0);
		MONO_ADD_INS (cfg->cbb, load);

		if (!mini_debug_options.weak_memory_model)
			mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_REL);

		NEW_STORE_MEMBASE (cfg, store, OP_STORE_MEMBASE_REG, dest->dreg, 0, dreg);
		MONO_ADD_INS (cfg->cbb, store);

		mini_emit_write_barrier (cfg, dest, load);
		return;
	} else if (cfg->gen_write_barriers && (m_class_has_references (klass) || size_ins) &&
			   !native && !stack_store) { 	/* if native is true there should be no references in the struct */
		/* Avoid barriers when storing to the stack */
		if (!((dest->opcode == OP_ADD_IMM && dest->sreg1 == cfg->frame_reg) ||
			  (dest->opcode == OP_LDADDR))) {
			int context_used;

			iargs [0] = dest;
			iargs [1] = src;

			context_used = mini_class_check_context_used (cfg, klass);

			/* It's ok to intrinsify under gsharing since shared code types are layout stable. */
			if (!size_ins && (cfg->opt & MONO_OPT_INTRINS) && mini_emit_wb_aware_memcpy (cfg, klass, iargs, size, align)) {
			} else if (size_ins || align < TARGET_SIZEOF_VOID_P) {
				if (context_used) {
					iargs [2] = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
				}  else {
					iargs [2] = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_CLASS, klass);
					if (!cfg->compile_aot)
						mono_class_compute_gc_descriptor (klass);
				}
				if (size_ins)
					mono_emit_jit_icall (cfg, mono_gsharedvt_value_copy, iargs);
				else
					mono_emit_jit_icall (cfg, mono_value_copy_internal, iargs);
			} else {
				/* We don't unroll more than 5 stores to avoid code bloat. */
				/*This is harmless and simplify mono_gc_get_range_copy_func */
				size += (TARGET_SIZEOF_VOID_P - 1);
				size &= ~(TARGET_SIZEOF_VOID_P - 1);

				EMIT_NEW_ICONST (cfg, iargs [2], size);
				mono_emit_jit_icall (cfg, mono_gc_wbarrier_range_copy, iargs);
			}
			return;
		}
	}

	if (size_ins) {
		iargs [0] = dest;
		iargs [1] = src;
		iargs [2] = size_ins;
		mini_emit_calli (cfg, mono_method_signature_internal (mini_get_memcpy_method ()), iargs, memcpy_ins, NULL, NULL);
	} else {
		mini_emit_memcpy_const_size (cfg, dest, src, size, align);
	}
}

MonoInst*
mini_emit_memory_load (MonoCompile *cfg, MonoType *type, MonoInst *src, int offset, int ins_flag)
{
	MonoInst *ins;

	/* LLVM can handle unaligned loads and stores, so there's no reason to
	 * manually decompose an unaligned load here into a memcpy if we're
	 * using LLVM. */
	if ((ins_flag & MONO_INST_UNALIGNED) && !COMPILE_LLVM (cfg)) {
		MonoInst *addr, *tmp_var;
		int align;
		int size = mono_type_size (type, &align);

		if (offset) {
			MonoInst *add_offset;
			NEW_BIALU_IMM (cfg, add_offset, OP_PADD_IMM, alloc_preg (cfg), src->dreg, offset);
			MONO_ADD_INS (cfg->cbb, add_offset);
			src = add_offset;
		}

		tmp_var = mono_compile_create_var (cfg, type, OP_LOCAL);
		EMIT_NEW_VARLOADA (cfg, addr, tmp_var, tmp_var->inst_vtype);

		mini_emit_memcpy_const_size (cfg, addr, src, size, 1);
		EMIT_NEW_TEMPLOAD (cfg, ins, tmp_var->inst_c0);
	} else {
		EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, type, src->dreg, offset);
	}
	ins->flags |= ins_flag;

	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_ACQ);
	}

	return ins;
}


void
mini_emit_memory_store (MonoCompile *cfg, MonoType *type, MonoInst *dest, MonoInst *value, int ins_flag)
{
	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_REL);
	} else if (!mini_debug_options.weak_memory_model && mini_type_is_reference (type) && cfg->method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER)
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_REL);

	if (!(ins_flag & MONO_INST_NONULLCHECK))
		MONO_EMIT_NULL_CHECK (cfg, dest->dreg, FALSE);

	if ((ins_flag & MONO_INST_UNALIGNED) && !COMPILE_LLVM (cfg)) {
		MonoInst *addr, *mov, *tmp_var;

		tmp_var = mono_compile_create_var (cfg, type, OP_LOCAL);
		EMIT_NEW_TEMPSTORE (cfg, mov, tmp_var->inst_c0, value);
		EMIT_NEW_VARLOADA (cfg, addr, tmp_var, tmp_var->inst_vtype);
		mini_emit_memory_copy_internal (cfg, dest, addr, mono_class_from_mono_type_internal (type), 1, FALSE, (ins_flag & MONO_INST_STACK_STORE) != 0);
	} else {
		MonoInst *ins;

		/* FIXME: should check item at sp [1] is compatible with the type of the store. */
		EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, type, dest->dreg, 0, value->dreg);
		ins->flags |= ins_flag;
	}

	if (cfg->gen_write_barriers && cfg->method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER &&
		mini_type_is_reference (type) && !MONO_INS_IS_PCONST_NULL (value)) {
		/* insert call to write barrier */
		mini_emit_write_barrier (cfg, dest, value);
	}
}

void
mini_emit_memory_copy_bytes (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoInst *size, int ins_flag)
{
	int align = (ins_flag & MONO_INST_UNALIGNED) ? 1 : TARGET_SIZEOF_VOID_P;

	/*
	 * FIXME: It's unclear whether we should be emitting both the acquire
	 * and release barriers for cpblk. It is technically both a load and
	 * store operation, so it seems like that's the sensible thing to do.
	 *
	 * FIXME: We emit full barriers on both sides of the operation for
	 * simplicity. We should have a separate atomic memcpy method instead.
	 */
	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);
	}

	if ((cfg->opt & MONO_OPT_INTRINS) && (size->opcode == OP_ICONST)) {
		mini_emit_memcpy_const_size (cfg, dest, src, size->inst_c0, align);
	} else {
		mini_emit_memcpy_internal (cfg, dest, src, size, 0, align);
	}

	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);
	}
}

void
mini_emit_memory_init_bytes (MonoCompile *cfg, MonoInst *dest, MonoInst *value, MonoInst *size, int ins_flag)
{
	int align = (ins_flag & MONO_INST_UNALIGNED) ? 1 : TARGET_SIZEOF_VOID_P;

	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_REL);
	}

	//FIXME unrolled memset only supports zeroing
	if ((cfg->opt & MONO_OPT_INTRINS) && (size->opcode == OP_ICONST) && (value->opcode == OP_ICONST) && (value->inst_c0 == 0)) {
		mini_emit_memset_const_size (cfg, dest, value->inst_c0, size->inst_c0, align);
	} else {
		mini_emit_memset_internal (cfg, dest, value, 0, size, 0, align);
	}

}

/*
 * If @klass is a valuetype, emit code to copy a value with source address in @src and destination address in @dest.
 * If @klass is a ref type, copy a pointer instead.
 */

void
mini_emit_memory_copy (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoClass *klass, gboolean native, int ins_flag)
{
	int explicit_align = 0;
	if (ins_flag & MONO_INST_UNALIGNED)
		explicit_align = 1;

	/*
	 * FIXME: It's unclear whether we should be emitting both the acquire
	 * and release barriers for cpblk. It is technically both a load and
	 * store operation, so it seems like that's the sensible thing to do.
	 *
	 * FIXME: We emit full barriers on both sides of the operation for
	 * simplicity. We should have a separate atomic memcpy method instead.
	 */
	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);
	}

	mini_emit_memory_copy_internal (cfg, dest, src, klass, explicit_align, native, (ins_flag & MONO_INST_STACK_STORE) != 0);

	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile loads have acquire semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_SEQ);
	}
}
#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (memory_access);
#endif
