/**
 * \file
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include "mini.h"
#include "ir-emit.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/class-abi-details.h>


#define is_complex_isinst(klass) (mono_class_is_interface (klass) || m_class_get_rank (klass) || mono_class_is_nullable (klass) || mono_class_is_marshalbyref (klass) || mono_class_is_sealed (klass) || m_class_get_byval_arg (klass)->type == MONO_TYPE_VAR || m_class_get_byval_arg (klass)->type == MONO_TYPE_MVAR)

static int
get_castclass_cache_idx (MonoCompile *cfg)
{
	/* Each CASTCLASS_CACHE patch needs a unique index which identifies the call site */
	cfg->castclass_cache_index ++;
	return (cfg->method_index << 16) | cfg->castclass_cache_index;
}

static void
emit_cached_check_args (MonoCompile *cfg, MonoInst *obj, MonoClass *klass, int context_used, MonoInst *args[3])
{
	args [0] = obj;

	if (context_used) {
		MonoInst *cache_ins;

		cache_ins = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_CAST_CACHE);

		/* klass - it's the second element of the cache entry*/
		EMIT_NEW_LOAD_MEMBASE (cfg, args [1], OP_LOAD_MEMBASE, alloc_preg (cfg), cache_ins->dreg, TARGET_SIZEOF_VOID_P);

		args [2] = cache_ins; /* cache */
	} else {
		int idx;

		EMIT_NEW_CLASSCONST (cfg, args [1], klass); /* klass */

		idx = get_castclass_cache_idx (cfg); /* inline cache*/
		args [2] = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_CASTCLASS_CACHE, GINT_TO_POINTER (idx));
	}
}

static MonoInst*
emit_isinst_with_cache (MonoCompile *cfg, MonoInst *obj, MonoClass *klass, int context_used)
{
	MonoInst *args [3];
	MonoMethod *mono_isinst = mono_marshal_get_isinst_with_cache ();

	emit_cached_check_args (cfg, obj, klass, context_used, args);
	return mono_emit_method_call (cfg, mono_isinst, args, NULL);
}

static MonoInst*
emit_castclass_with_cache_no_details (MonoCompile *cfg, MonoInst *obj, MonoClass *klass, int context_used)
{
	MonoInst *args [3];
	MonoMethod *mono_castclass = mono_marshal_get_castclass_with_cache ();
	MonoInst *res;

	emit_cached_check_args (cfg, obj, klass, context_used, args);

	res = mono_emit_method_call (cfg, mono_castclass, args, NULL);

	return res;
}

static MonoInst*
emit_castclass_with_cache (MonoCompile *cfg, MonoInst *obj, MonoClass *klass, int context_used)
{
	MonoInst *args [3];
	MonoMethod *mono_castclass = mono_marshal_get_castclass_with_cache ();
	MonoInst *res;

	emit_cached_check_args (cfg, obj, klass, context_used, args);

	mini_save_cast_details (cfg, klass, args [0]->dreg, TRUE);
	res = mono_emit_method_call (cfg, mono_castclass, args, NULL);
	mini_reset_cast_details (cfg);

	return res;
}

static void
mini_emit_class_check_inst (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoInst *klass_inst)
{
	if (klass_inst) {
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, klass_inst->dreg);
	} else {
		MonoInst *ins = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_CLASS, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, ins->dreg);
	}
	MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
}


static void
mini_emit_isninst_cast_inst (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoInst *klass_ins, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	int idepth_reg = alloc_preg (cfg);
	int stypes_reg = alloc_preg (cfg);
	int stype = alloc_preg (cfg);

	mono_class_setup_supertypes (klass);

	if (m_class_get_idepth (klass) > MONO_DEFAULT_SUPERTABLE_SIZE) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, idepth_reg, klass_reg, m_class_offsetof_idepth ());
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, idepth_reg, m_class_get_idepth (klass));
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBLT_UN, false_target);
	}
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stypes_reg, klass_reg, m_class_offsetof_supertypes ());
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stype, stypes_reg, ((m_class_get_idepth (klass) - 1) * TARGET_SIZEOF_VOID_P));
	if (klass_ins) {
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, stype, klass_ins->dreg);
	} else if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, stype, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, stype, (gsize)klass);
	}
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, true_target);
}


static void
mini_emit_interface_bitmap_check (MonoCompile *cfg, int intf_bit_reg, int base_reg, int offset, MonoClass *klass)
{
	int ibitmap_reg = alloc_preg (cfg);
#ifdef COMPRESSED_INTERFACE_BITMAP
	MonoInst *args [2];
	MonoInst *res, *ins;
	NEW_LOAD_MEMBASE (cfg, ins, OP_LOAD_MEMBASE, ibitmap_reg, base_reg, offset);
	MONO_ADD_INS (cfg->cbb, ins);
	args [0] = ins;
	args [1] = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_IID, klass);
	res = mono_emit_jit_icall (cfg, mono_class_interface_match, args);
	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, intf_bit_reg, res->dreg);
#else
	int ibitmap_byte_reg = alloc_preg (cfg);

	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, ibitmap_reg, base_reg, offset);

	if (cfg->compile_aot) {
		int iid_reg = alloc_preg (cfg);
		int shifted_iid_reg = alloc_preg (cfg);
		int ibitmap_byte_address_reg = alloc_preg (cfg);
		int masked_iid_reg = alloc_preg (cfg);
		int iid_one_bit_reg = alloc_preg (cfg);
		int iid_bit_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_AOTCONST (cfg, iid_reg, klass, MONO_PATCH_INFO_IID);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHR_IMM, shifted_iid_reg, iid_reg, 3);
		MONO_EMIT_NEW_BIALU (cfg, OP_PADD, ibitmap_byte_address_reg, ibitmap_reg, shifted_iid_reg);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, ibitmap_byte_reg, ibitmap_byte_address_reg, 0);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_IAND_IMM, masked_iid_reg, iid_reg, 7);
		MONO_EMIT_NEW_ICONST (cfg, iid_one_bit_reg, 1);
		MONO_EMIT_NEW_BIALU (cfg, OP_ISHL, iid_bit_reg, iid_one_bit_reg, masked_iid_reg);
		MONO_EMIT_NEW_BIALU (cfg, OP_IAND, intf_bit_reg, ibitmap_byte_reg, iid_bit_reg);
	} else {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, ibitmap_byte_reg, ibitmap_reg, m_class_get_interface_id (klass) >> 3);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, intf_bit_reg, ibitmap_byte_reg, ((target_mgreg_t)1) << (m_class_get_interface_id (klass) & 7));
	}
#endif
}

/* 
 * Emit code which loads into "intf_bit_reg" a nonzero value if the MonoClass
 * stored in "klass_reg" implements the interface "klass".
 */
static void
mini_emit_load_intf_bit_reg_class (MonoCompile *cfg, int intf_bit_reg, int klass_reg, MonoClass *klass)
{
	mini_emit_interface_bitmap_check (cfg, intf_bit_reg, klass_reg, m_class_offsetof_interface_bitmap (), klass);
}

/* 
 * Emit code which loads into "intf_bit_reg" a nonzero value if the MonoVTable
 * stored in "vtable_reg" implements the interface "klass".
 */
static void
mini_emit_load_intf_bit_reg_vtable (MonoCompile *cfg, int intf_bit_reg, int vtable_reg, MonoClass *klass)
{
	mini_emit_interface_bitmap_check (cfg, intf_bit_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, interface_bitmap), klass);
}

/* 
 * Emit code which checks whenever the interface id of @klass is smaller than
 * than the value given by max_iid_reg.
*/
static void
mini_emit_max_iid_check (MonoCompile *cfg, int max_iid_reg, MonoClass *klass,
						 MonoBasicBlock *false_target)
{
	if (cfg->compile_aot) {
		int iid_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_AOTCONST (cfg, iid_reg, klass, MONO_PATCH_INFO_IID);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, max_iid_reg, iid_reg);
	}
	else
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, max_iid_reg, m_class_get_interface_id (klass));
	if (false_target)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBLT_UN, false_target);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "InvalidCastException");
}

/* Same as above, but obtains max_iid from a vtable */
static void
mini_emit_max_iid_check_vtable (MonoCompile *cfg, int vtable_reg, MonoClass *klass,
								 MonoBasicBlock *false_target)
{
	int max_iid_reg = alloc_preg (cfg);
		
	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU4_MEMBASE, max_iid_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, max_interface_id));
	mini_emit_max_iid_check (cfg, max_iid_reg, klass, false_target);
}

/* Same as above, but obtains max_iid from a klass */
static void
mini_emit_max_iid_check_class (MonoCompile *cfg, int klass_reg, MonoClass *klass,
								 MonoBasicBlock *false_target)
{
	int max_iid_reg = alloc_preg (cfg);

	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU4_MEMBASE, max_iid_reg, klass_reg, m_class_offsetof_max_interface_id ());
	mini_emit_max_iid_check (cfg, max_iid_reg, klass, false_target);
}

static void
mini_emit_class_check_branch (MonoCompile *cfg, int klass_reg, MonoClass *klass, int branch_op, MonoBasicBlock *target)
{
	if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, (gsize)klass);
	}
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, branch_op, target);
}


static void
mini_emit_isninst_cast (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	mini_emit_isninst_cast_inst (cfg, klass_reg, klass, NULL, false_target, true_target);
}

static void
mini_emit_iface_cast (MonoCompile *cfg, int vtable_reg, MonoClass *klass, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	int intf_reg = alloc_preg (cfg);

	mini_emit_max_iid_check_vtable (cfg, vtable_reg, klass, false_target);
	mini_emit_load_intf_bit_reg_vtable (cfg, intf_reg, vtable_reg, klass);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, intf_reg, 0);
	if (true_target)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, true_target);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");		
}

/*
 * Variant of the above that takes a register to the class, not the vtable.
 */
static void
mini_emit_iface_class_cast (MonoCompile *cfg, int klass_reg, MonoClass *klass, MonoBasicBlock *false_target, MonoBasicBlock *true_target)
{
	int intf_bit_reg = alloc_preg (cfg);

	mini_emit_max_iid_check_class (cfg, klass_reg, klass, false_target);
	mini_emit_load_intf_bit_reg_class (cfg, intf_bit_reg, klass_reg, klass);
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, intf_bit_reg, 0);
	if (true_target)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, true_target);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");
}


static void
mini_emit_castclass (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoBasicBlock *object_is_null);
	
static void
mini_emit_castclass_inst (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoInst *klass_inst, MonoBasicBlock *object_is_null)
{
	if (m_class_get_rank (klass)) {
		int rank_reg = alloc_preg (cfg);
		int eclass_reg = alloc_preg (cfg);

		g_assert (!klass_inst);

		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, klass_reg, m_class_offsetof_rank ());
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, m_class_get_rank (klass));
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");

		//		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, m_class_offsetof_cast_class ());
		if (m_class_is_array_special_interface (m_class_get_cast_class (klass))) {
			MonoInst *src;

			MONO_INST_NEW (cfg, src, OP_LOCAL);
			src->dreg = obj_reg;
			emit_castclass_with_cache_no_details (cfg, src, klass, 0);
		} else if (m_class_get_cast_class (klass) == mono_defaults.object_class) {
			int parent_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, parent_reg, eclass_reg, m_class_offsetof_parent ());
			mini_emit_class_check_branch (cfg, parent_reg, m_class_get_parent (mono_defaults.enum_class), OP_PBNE_UN, object_is_null);
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (m_class_get_cast_class (klass) == m_class_get_parent (mono_defaults.enum_class)) {
			mini_emit_class_check_branch (cfg, eclass_reg, m_class_get_parent (mono_defaults.enum_class), OP_PBEQ, object_is_null);
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (m_class_get_cast_class (klass) == mono_defaults.enum_class) {
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (mono_class_is_interface (m_class_get_cast_class (klass))) {
			mini_emit_iface_class_cast (cfg, eclass_reg, m_class_get_cast_class (klass), NULL, NULL);
		} else {
			// Pass -1 as obj_reg to skip the check below for arrays of arrays
			mini_emit_castclass (cfg, -1, eclass_reg, m_class_get_cast_class (klass), object_is_null);
		}

		if ((m_class_get_rank (klass) == 1) && (m_class_get_byval_arg (klass)->type == MONO_TYPE_SZARRAY) && (obj_reg != -1)) {
			/* Check that the object is a vector too */
			int bounds_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, bounds_reg, obj_reg, MONO_STRUCT_OFFSET (MonoArray, bounds));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, bounds_reg, 0);
			MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
		}
	} else {
		int idepth_reg = alloc_preg (cfg);
		int stypes_reg = alloc_preg (cfg);
		int stype = alloc_preg (cfg);

		mono_class_setup_supertypes (klass);

		if (m_class_get_idepth (klass) > MONO_DEFAULT_SUPERTABLE_SIZE) {
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, idepth_reg, klass_reg, m_class_offsetof_idepth ());
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, idepth_reg, m_class_get_idepth (klass));
			MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "InvalidCastException");
		}
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stypes_reg, klass_reg, m_class_offsetof_supertypes ());
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stype, stypes_reg, ((m_class_get_idepth (klass) - 1) * TARGET_SIZEOF_VOID_P));
		mini_emit_class_check_inst (cfg, stype, klass, klass_inst);
	}
}

static void
mini_emit_castclass (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoBasicBlock *object_is_null)
{
	mini_emit_castclass_inst (cfg, obj_reg, klass_reg, klass, NULL, object_is_null);
}

static void
emit_special_array_iface_check (MonoCompile *cfg, MonoInst *src, MonoClass* klass, int vtable_reg, MonoBasicBlock *not_an_array, MonoBasicBlock *true_bb, int context_used)
{
	int rank_reg;

	if (!m_class_is_array_special_interface (klass))
		return;

	rank_reg = alloc_ireg (cfg);

	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, 1);
	if (not_an_array)
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, not_an_array);
	else
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");

	emit_castclass_with_cache_no_details (cfg, src, klass, context_used);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, true_bb);
}

/*
 * Returns NULL and set the cfg exception on error.
 */
static MonoInst*
handle_castclass (MonoCompile *cfg, MonoClass *klass, MonoInst *src, int context_used)
{
	MonoBasicBlock *is_null_bb;
	int obj_reg = src->dreg;
	MonoInst *klass_inst = NULL;

	if (MONO_INS_IS_PCONST_NULL (src))
		return src;

	if (context_used) {

		if (is_complex_isinst (klass))
			return emit_castclass_with_cache (cfg, src, klass, context_used);

		klass_inst = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
	}

	NEW_BBLOCK (cfg, is_null_bb);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, is_null_bb);

	mini_save_cast_details (cfg, klass, obj_reg, FALSE);

	if (mono_class_is_interface (klass)) {
		int tmp_reg = alloc_preg (cfg);
#ifndef DISABLE_REMOTING
		MonoBasicBlock *interface_fail_bb;
		MonoBasicBlock *array_fail_bb;
		int klass_reg = alloc_preg (cfg);

		NEW_BBLOCK (cfg, interface_fail_bb);
		NEW_BBLOCK (cfg, array_fail_bb);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, tmp_reg, klass, interface_fail_bb, is_null_bb);

		// iface bitmap check failed
		MONO_START_BB (cfg, interface_fail_bb);

		//Check if it's a rank zero array and emit fallback casting
		emit_special_array_iface_check (cfg, src, klass, tmp_reg, array_fail_bb, is_null_bb, context_used);

		// array check failed
		MONO_START_BB (cfg, array_fail_bb);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		mini_emit_class_check (cfg, klass_reg, mono_defaults.transparent_proxy_class);

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");

		MonoInst *args [1] = { src };
		MonoInst *proxy_test_inst = mono_emit_method_call (cfg, mono_marshal_get_proxy_cancast (klass), args, NULL);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, proxy_test_inst->dreg, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");

		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, is_null_bb);
#else
		MonoBasicBlock *interface_fail_bb = NULL;

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));

		if (m_class_is_array_special_interface (klass)) {
			NEW_BBLOCK (cfg, interface_fail_bb);
			mini_emit_iface_cast (cfg, tmp_reg, klass, interface_fail_bb, is_null_bb);
			// iface bitmap check failed
			MONO_START_BB (cfg, interface_fail_bb);

			//Check if it's a rank zero array and emit fallback casting
			emit_special_array_iface_check (cfg, src, klass, tmp_reg, NULL, is_null_bb, context_used);
		} else {
			mini_emit_iface_cast (cfg, tmp_reg, klass, NULL, NULL);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, is_null_bb);
		}
#endif
	} else if (mono_class_is_marshalbyref (klass)) {
#ifndef DISABLE_REMOTING
		MonoBasicBlock *no_proxy_bb, *fail_1_bb;
		int tmp_reg = alloc_preg (cfg);
		int klass_reg = alloc_preg (cfg);

		NEW_BBLOCK (cfg, no_proxy_bb);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, no_proxy_bb);

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, remote_class));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoRemoteClass, proxy_class));

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, no_proxy_bb);

		NEW_BBLOCK (cfg, fail_1_bb);

		mini_emit_isninst_cast (cfg, klass_reg, klass, fail_1_bb, is_null_bb);

		MONO_START_BB (cfg, fail_1_bb);

		MonoInst *args [1] = { src };
		MonoInst *proxy_test_inst = mono_emit_method_call (cfg, mono_marshal_get_proxy_cancast (klass), args, NULL);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, proxy_test_inst->dreg, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");

		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, is_null_bb);

		MONO_START_BB (cfg, no_proxy_bb);

		mini_emit_castclass_inst (cfg, obj_reg, klass_reg, klass, klass_inst, is_null_bb);
#else
		g_error ("Transparent proxy support is disabled while trying to JIT code that uses it");
#endif
	} else {
		int vtable_reg = alloc_preg (cfg);
		int klass_reg = alloc_preg (cfg);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));

		if (!m_class_get_rank (klass) && !cfg->compile_aot && !(cfg->opt & MONO_OPT_SHARED) && mono_class_is_sealed (klass)) {
			/* the remoting code is broken, access the class for now */
			if (0) { /*FIXME what exactly is broken? This change refers to r39380 from 2005 and mention some remoting fixes were due.*/
				MonoVTable *vt = mono_class_vtable_checked (cfg->domain, klass, cfg->error);
				if (!is_ok (cfg->error)) {
					mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
					return NULL;
				}
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, (gsize)vt);
			} else {
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, (gsize)klass);
			}
			MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
		} else {
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			mini_emit_castclass_inst (cfg, obj_reg, klass_reg, klass, klass_inst, is_null_bb);
		}
	}

	MONO_START_BB (cfg, is_null_bb);

	mini_reset_cast_details (cfg);

	return src;
}

/*
 * Returns NULL and set the cfg exception on error.
 */
static MonoInst*
handle_isinst (MonoCompile *cfg, MonoClass *klass, MonoInst *src, int context_used)
{
	MonoInst *ins;
	MonoBasicBlock *is_null_bb, *false_bb, *end_bb;
	int obj_reg = src->dreg;
	int vtable_reg = alloc_preg (cfg);
	int res_reg = alloc_ireg_ref (cfg);
	MonoInst *klass_inst = NULL;

	if (context_used) {
		if(is_complex_isinst (klass))
			return emit_isinst_with_cache (cfg, src, klass, context_used);

		klass_inst = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
	}

	NEW_BBLOCK (cfg, is_null_bb);
	NEW_BBLOCK (cfg, false_bb);
	NEW_BBLOCK (cfg, end_bb);

	/* Do the assignment at the beginning, so the other assignment can be if converted */
	EMIT_NEW_UNALU (cfg, ins, OP_MOVE, res_reg, obj_reg);
	ins->type = STACK_OBJ;
	ins->klass = klass;

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBEQ, is_null_bb);

	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));

	if (mono_class_is_interface (klass)) {
		MonoBasicBlock *interface_fail_bb;

		NEW_BBLOCK (cfg, interface_fail_bb);

		mini_emit_iface_cast (cfg, vtable_reg, klass, interface_fail_bb, is_null_bb);
		MONO_START_BB (cfg, interface_fail_bb);

		if (m_class_is_array_special_interface (klass)) {
			MonoBasicBlock *not_an_array;
			MonoInst *move;
			int rank_reg = alloc_ireg (cfg);

			NEW_BBLOCK (cfg, not_an_array);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, 1);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_IBNE_UN, not_an_array);

			MonoInst *res_inst = emit_isinst_with_cache (cfg, src, klass, context_used);
			EMIT_NEW_UNALU (cfg, move, OP_MOVE, res_reg, res_inst->dreg);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

			MONO_START_BB (cfg, not_an_array);
		}

#ifndef DISABLE_REMOTING
		int tmp_reg, klass_reg;
		MonoBasicBlock *call_proxy_isinst;

		NEW_BBLOCK (cfg, call_proxy_isinst);

		klass_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, false_bb);

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, false_bb);

		MONO_START_BB (cfg, call_proxy_isinst);

		MonoInst *args [1] = { src };
		MonoInst *proxy_test_inst = mono_emit_method_call (cfg, mono_marshal_get_proxy_cancast (klass), args, NULL);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, proxy_test_inst->dreg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, is_null_bb);
#else
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
#endif

	} else if (mono_class_is_marshalbyref (klass)) {

#ifndef DISABLE_REMOTING
		int tmp_reg, klass_reg;
		MonoBasicBlock *no_proxy_bb, *call_proxy_isinst;

		NEW_BBLOCK (cfg, no_proxy_bb);
		NEW_BBLOCK (cfg, call_proxy_isinst);

		klass_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, no_proxy_bb);

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, remote_class));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoRemoteClass, proxy_class));

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, false_bb);

		mini_emit_isninst_cast (cfg, klass_reg, klass, call_proxy_isinst, is_null_bb);

		MONO_START_BB (cfg, call_proxy_isinst);

		MonoInst *args [1] = { src };
		MonoInst *proxy_test_inst = mono_emit_method_call (cfg, mono_marshal_get_proxy_cancast (klass), args, NULL);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, proxy_test_inst->dreg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, is_null_bb);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);

		MONO_START_BB (cfg, no_proxy_bb);

		mini_emit_isninst_cast (cfg, klass_reg, klass, false_bb, is_null_bb);
#else
		g_error ("transparent proxy support is disabled while trying to JIT code that uses it");
#endif
	} else {
		int klass_reg = alloc_preg (cfg);

		if (m_class_get_rank (klass)) {
			int rank_reg = alloc_preg (cfg);
			int eclass_reg = alloc_preg (cfg);

			if ((m_class_get_rank (klass) == 1) && (m_class_get_byval_arg (klass)->type == MONO_TYPE_SZARRAY)) {
				/* Check that the object is a vector too */
				int bounds_reg = alloc_preg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, bounds_reg, obj_reg, MONO_STRUCT_OFFSET (MonoArray, bounds));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, bounds_reg, 0);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
			}

			g_assert (!context_used);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, m_class_get_rank (klass));
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, m_class_offsetof_cast_class ());
			if (m_class_is_array_special_interface (m_class_get_cast_class (klass))) {
				MonoInst *move, *res_inst;

				res_inst = emit_isinst_with_cache (cfg, src, klass, context_used);
				EMIT_NEW_UNALU (cfg, move, OP_MOVE, res_reg, res_inst->dreg);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
			} else if (m_class_get_cast_class (klass) == mono_defaults.object_class) {
				int parent_reg, class_kind_reg;
				MonoBasicBlock *pointer_check_bb;

				NEW_BBLOCK (cfg, pointer_check_bb);

				parent_reg = alloc_preg (cfg);
				class_kind_reg = alloc_preg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, parent_reg, eclass_reg, m_class_offsetof_parent ());
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, class_kind_reg, eclass_reg, m_class_offsetof_class_kind ());

				// Check if the parent class of the element is not System.ValueType
				mini_emit_class_check_branch (cfg, parent_reg, m_class_get_parent (mono_defaults.enum_class), OP_PBNE_UN, pointer_check_bb);
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);

				MONO_START_BB (cfg, pointer_check_bb);
				// Check if the parent class of the element is non-null, else manually check the type
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, parent_reg, NULL);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, is_null_bb);
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, class_kind_reg, MONO_CLASS_POINTER);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, false_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, is_null_bb);
			} else if (m_class_get_cast_class (klass) == m_class_get_parent (mono_defaults.enum_class)) {
				mini_emit_class_check_branch (cfg, eclass_reg, m_class_get_parent (mono_defaults.enum_class), OP_PBEQ, is_null_bb);
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);				
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (m_class_get_cast_class (klass) == mono_defaults.enum_class) {
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (mono_class_is_interface (m_class_get_cast_class (klass))) {
				mini_emit_iface_class_cast (cfg, eclass_reg, m_class_get_cast_class (klass), false_bb, is_null_bb);
			} else {
				/* the is_null_bb target simply copies the input register to the output */
				mini_emit_isninst_cast (cfg, eclass_reg, m_class_get_cast_class (klass), false_bb, is_null_bb);
			}
		} else if (mono_class_is_nullable (klass)) {
			g_assert (!context_used);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			/* the is_null_bb target simply copies the input register to the output */
			mini_emit_isninst_cast (cfg, klass_reg, m_class_get_cast_class (klass), false_bb, is_null_bb);
		} else {
			if (!cfg->compile_aot && !(cfg->opt & MONO_OPT_SHARED) && mono_class_is_sealed (klass)) {
				g_assert (!context_used);
				/* the remoting code is broken, access the class for now */
				if (0) {/*FIXME what exactly is broken? This change refers to r39380 from 2005 and mention some remoting fixes were due.*/
					MonoVTable *vt = mono_class_vtable_checked (cfg->domain, klass, cfg->error);
					if (!is_ok (cfg->error)) {
						mono_cfg_set_exception (cfg, MONO_EXCEPTION_MONO_ERROR);
						return NULL;
					}
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, (gsize)vt);
				} else {
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, (gsize)klass);
				}
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, is_null_bb);
			} else {
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
				/* the is_null_bb target simply copies the input register to the output */
				mini_emit_isninst_cast_inst (cfg, klass_reg, klass, klass_inst, false_bb, is_null_bb);
			}
		}
	}

	MONO_START_BB (cfg, false_bb);

	MONO_EMIT_NEW_PCONST (cfg, res_reg, NULL);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

	MONO_START_BB (cfg, is_null_bb);

	MONO_START_BB (cfg, end_bb);

	return ins;
}

static void
mono_decompose_typecheck (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
	MonoInst *ret, *move, *source;
	MonoClass *klass = ins->klass;
	int context_used = mini_class_check_context_used (cfg, klass);
	int is_isinst = ins->opcode == OP_ISINST;
	g_assert (is_isinst || ins->opcode == OP_CASTCLASS);
	source = get_vreg_to_inst (cfg, ins->sreg1);
	if (!source || source == (MonoInst *) -1)
		source = mono_compile_create_var_for_vreg (cfg, mono_get_object_type (), OP_LOCAL, ins->sreg1);
	g_assert (source && source != (MonoInst *) -1);

	MonoBasicBlock *first_bb;
	NEW_BBLOCK (cfg, first_bb);
	cfg->cbb = first_bb;

	if (mini_class_has_reference_variant_generic_argument (cfg, klass, context_used)) {
		if (is_isinst)
			ret = emit_isinst_with_cache (cfg, source, klass, context_used);
		else
			ret = emit_castclass_with_cache (cfg, source, klass, context_used);

	} else {
		if (is_isinst)
			ret = handle_isinst (cfg, klass, source, context_used);
		else
			ret = handle_castclass (cfg, klass, source, context_used);
	}
	EMIT_NEW_UNALU (cfg, move, OP_MOVE, ins->dreg, ret->dreg);

	g_assert (cfg->cbb->code || first_bb->code);
	MonoInst *prev = ins->prev;
	mono_replace_ins (cfg, bb, ins, &prev, first_bb, cfg->cbb);
}

void
mono_decompose_typechecks (MonoCompile *cfg)
{
	gboolean found_typetest = FALSE;
	for (MonoBasicBlock *bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			switch (ins->opcode) {
			case OP_ISINST:
			case OP_CASTCLASS:
				found_typetest = TRUE;
				mono_decompose_typecheck (cfg, bb, ins);
				break;
			}
		}
	}
	if ((cfg->verbose_level > 2) && found_typetest)
		mono_print_code (cfg, "AFTER DECOMPOSE TYPE_CHECKS");
	
}


//API used by method-to-ir.c
void
mini_emit_class_check (MonoCompile *cfg, int klass_reg, MonoClass *klass)
{
	mini_emit_class_check_inst (cfg, klass_reg, klass, NULL);
}

#else

MONO_EMPTY_SOURCE_FILE (type_checking);
#endif
