/**
 * Emit memory access for the front-end.
 *
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include <mono/utils/mono-memory-model.h>

#include "mini.h"
#include "ir-emit.h"

#define MAX_INLINE_COPIES 10

void 
mini_emit_memset (MonoCompile *cfg, int destreg, int offset, int size, int val, int align)
{
	int val_reg;

	g_assert (val == 0);

	if (align == 0)
		align = 4;

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

	if (align < 4) {
		/* This could be optimized further if neccesary */
		while (size >= 1) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, offset, val_reg);
			offset += 1;
			size -= 1;
		}
		return;
	}	

	if (!cfg->backend->no_unaligned_access && SIZEOF_REGISTER == 8) {
		if (offset % 8) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, val_reg);
			offset += 4;
			size -= 4;
		}
		while (size >= 8) {
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, destreg, offset, val_reg);
			offset += 8;
			size -= 8;
		}
	}	

	while (size >= 4) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, offset, val_reg);
		offset += 4;
		size -= 4;
	}
	while (size >= 2) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, offset, val_reg);
		offset += 2;
		size -= 2;
	}
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

	if (align == 0)
		align = 4;

	/*FIXME arbitrary hack to avoid unbound code expansion.*/
	g_assert (size < 10000);

	if (align < 4) {
		/* This could be optimized further if neccesary */
		while (size >= 1) {
			cur_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, cur_reg, srcreg, soffset);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI1_MEMBASE_REG, destreg, doffset, cur_reg);
			doffset += 1;
			soffset += 1;
			size -= 1;
		}
	}

	if (!cfg->backend->no_unaligned_access && SIZEOF_REGISTER == 8) {
		while (size >= 8) {
			cur_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI8_MEMBASE, cur_reg, srcreg, soffset);
			MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, destreg, doffset, cur_reg);
			doffset += 8;
			soffset += 8;
			size -= 8;
		}
	}	

	while (size >= 4) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI4_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 4;
		soffset += 4;
		size -= 4;
	}
	while (size >= 2) {
		cur_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI2_MEMBASE, cur_reg, srcreg, soffset);
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI2_MEMBASE_REG, destreg, doffset, cur_reg);
		doffset += 2;
		soffset += 2;
		size -= 2;
	}
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
	if (size_ins || ((size / align > MAX_INLINE_COPIES) && !(cfg->opt & MONO_OPT_INTRINS))) {
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
	if (value_ins || size_ins || value != 0 || ((size / align > MAX_INLINE_COPIES) && !(cfg->opt & MONO_OPT_INTRINS))) {
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

MonoInst*
mini_emit_memory_load (MonoCompile *cfg, MonoType *type, MonoInst *src, int offset, int ins_flag)
{
	MonoInst *ins;

	EMIT_NEW_LOAD_MEMBASE_TYPE (cfg, ins, type, src->dreg, offset);
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
	MonoInst *ins;

	if (ins_flag & MONO_INST_VOLATILE) {
		/* Volatile stores have release semantics, see 12.6.7 in Ecma 335 */
		mini_emit_memory_barrier (cfg, MONO_MEMORY_BARRIER_REL);
	}
	/* FIXME: should check item at sp [1] is compatible with the type of the store. */

	EMIT_NEW_STORE_MEMBASE_TYPE (cfg, ins, type, dest->dreg, 0, value->dreg);
	ins->flags |= ins_flag;
	if (cfg->gen_write_barriers && cfg->method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER &&
		mini_type_is_reference (type) && !MONO_INS_IS_PCONST_NULL (value)) {
		/* insert call to write barrier */
		mini_emit_write_barrier (cfg, dest, value);
	}
}

void
mini_emit_memory_copy_bytes (MonoCompile *cfg, MonoInst *dest, MonoInst *src, MonoInst *size, int ins_flag)
{
	int align = SIZEOF_VOID_P;

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
		if (cfg->verbose_level > 3)
			printf ("EMITING REGULAR COPY\n");
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
	int align = SIZEOF_VOID_P;

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

#endif
