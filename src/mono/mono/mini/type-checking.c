#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include <mini.h>
#include <ir-emit.h>
#include <mono/metadata/abi-details.h>


//XXX maybe move to mini.h / mini.c?

static int
mini_class_check_context_used (MonoCompile *cfg, MonoClass *klass)
{
	if (cfg->gshared)
		return mono_class_check_context_used (klass);
	else
		return 0;
}


#define is_complex_isinst(klass) (mono_class_is_interface (klass) || klass->rank || mono_class_is_nullable (klass) || mono_class_is_marshalbyref (klass) || mono_class_is_sealed (klass) || klass->byval_arg.type == MONO_TYPE_VAR || klass->byval_arg.type == MONO_TYPE_MVAR)

static MonoInst*
emit_isinst_with_cache (MonoCompile *cfg, MonoClass *klass, MonoInst **args)
{
	MonoMethod *mono_isinst = mono_marshal_get_isinst_with_cache ();
	return mono_emit_method_call (cfg, mono_isinst, args, NULL);
}

static int
get_castclass_cache_idx (MonoCompile *cfg)
{
	/* Each CASTCLASS_CACHE patch needs a unique index which identifies the call site */
	cfg->castclass_cache_index ++;
	return (cfg->method_index << 16) | cfg->castclass_cache_index;
}

static MonoInst*
emit_isinst_with_cache_nonshared (MonoCompile *cfg, MonoInst *obj, MonoClass *klass)
{
	MonoInst *args [3];
	int idx;

	args [0] = obj; /* obj */
	EMIT_NEW_CLASSCONST (cfg, args [1], klass); /* klass */

	idx = get_castclass_cache_idx (cfg); /* inline cache*/
	args [2] = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_CASTCLASS_CACHE, GINT_TO_POINTER (idx));

	return emit_isinst_with_cache (cfg, klass, args);
}

static MonoInst*
emit_castclass_with_cache (MonoCompile *cfg, MonoClass *klass, MonoInst **args)
{
	MonoMethod *mono_castclass = mono_marshal_get_castclass_with_cache ();
	MonoInst *res;

	mini_save_cast_details (cfg, klass, args [0]->dreg, TRUE);
	res = mono_emit_method_call (cfg, mono_castclass, args, NULL);
	mini_reset_cast_details (cfg);

	return res;
}

static inline void
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

	if (klass->idepth > MONO_DEFAULT_SUPERTABLE_SIZE) {
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, idepth_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, idepth));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, idepth_reg, klass->idepth);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBLT_UN, false_target);
	}
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stypes_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, supertypes));
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stype, stypes_reg, ((klass->idepth - 1) * SIZEOF_VOID_P));
	if (klass_ins) {
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, stype, klass_ins->dreg);
	} else if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, stype, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, stype, klass);
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
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADI1_MEMBASE, ibitmap_byte_reg, ibitmap_reg, klass->interface_id >> 3);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_AND_IMM, intf_bit_reg, ibitmap_byte_reg, 1 << (klass->interface_id & 7));
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
	mini_emit_interface_bitmap_check (cfg, intf_bit_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, interface_bitmap), klass);
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
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, max_iid_reg, klass->interface_id);
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

	MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU4_MEMBASE, max_iid_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, max_interface_id));
	mini_emit_max_iid_check (cfg, max_iid_reg, klass, false_target);
}

static inline void
mini_emit_class_check_branch (MonoCompile *cfg, int klass_reg, MonoClass *klass, int branch_op, MonoBasicBlock *target)
{
	if (cfg->compile_aot) {
		int const_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_CLASSCONST (cfg, const_reg, klass);
		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, klass_reg, const_reg);
	} else {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
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
	if (klass->rank) {
		int rank_reg = alloc_preg (cfg);
		int eclass_reg = alloc_preg (cfg);

		g_assert (!klass_inst);
		MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, rank));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, klass->rank);
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "InvalidCastException");
		//		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, cast_class));
		if (klass->cast_class == mono_defaults.object_class) {
			int parent_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, parent_reg, eclass_reg, MONO_STRUCT_OFFSET (MonoClass, parent));
			mini_emit_class_check_branch (cfg, parent_reg, mono_defaults.enum_class->parent, OP_PBNE_UN, object_is_null);
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (klass->cast_class == mono_defaults.enum_class->parent) {
			mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class->parent, OP_PBEQ, object_is_null);
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (klass->cast_class == mono_defaults.enum_class) {
			mini_emit_class_check (cfg, eclass_reg, mono_defaults.enum_class);
		} else if (mono_class_is_interface (klass->cast_class)) {
			mini_emit_iface_class_cast (cfg, eclass_reg, klass->cast_class, NULL, NULL);
		} else {
			// Pass -1 as obj_reg to skip the check below for arrays of arrays
			mini_emit_castclass (cfg, -1, eclass_reg, klass->cast_class, object_is_null);
		}

		if ((klass->rank == 1) && (klass->byval_arg.type == MONO_TYPE_SZARRAY) && (obj_reg != -1)) {
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

		if (klass->idepth > MONO_DEFAULT_SUPERTABLE_SIZE) {
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, idepth_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, idepth));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, idepth_reg, klass->idepth);
			MONO_EMIT_NEW_COND_EXC (cfg, LT_UN, "InvalidCastException");
		}
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stypes_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, supertypes));
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, stype, stypes_reg, ((klass->idepth - 1) * SIZEOF_VOID_P));
		mini_emit_class_check_inst (cfg, stype, klass, klass_inst);
	}
}

static void
mini_emit_castclass (MonoCompile *cfg, int obj_reg, int klass_reg, MonoClass *klass, MonoBasicBlock *object_is_null)
{
	mini_emit_castclass_inst (cfg, obj_reg, klass_reg, klass, NULL, object_is_null);
}


/*
 * Returns NULL and set the cfg exception on error.
 */
static MonoInst*
handle_castclass (MonoCompile *cfg, MonoClass *klass, MonoInst *src, int context_used)
{
	MonoBasicBlock *is_null_bb;
	int obj_reg = src->dreg;
	int vtable_reg = alloc_preg (cfg);
	MonoInst *klass_inst = NULL;

	if (MONO_INS_IS_PCONST_NULL (src))
		return src;

	if (context_used) {
		MonoInst *args [3];

		if (mini_class_has_reference_variant_generic_argument (cfg, klass, context_used) || is_complex_isinst (klass)) {
			MonoInst *cache_ins;

			cache_ins = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_CAST_CACHE);

			/* obj */
			args [0] = src;

			/* klass - it's the second element of the cache entry*/
			EMIT_NEW_LOAD_MEMBASE (cfg, args [1], OP_LOAD_MEMBASE, alloc_preg (cfg), cache_ins->dreg, sizeof (gpointer));

			/* cache */
			args [2] = cache_ins;

			return emit_castclass_with_cache (cfg, klass, args);
		}

		klass_inst = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_KLASS);
	}

	NEW_BBLOCK (cfg, is_null_bb);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, is_null_bb);

	mini_save_cast_details (cfg, klass, obj_reg, FALSE);

	if (mono_class_is_interface (klass)) {
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, vtable_reg, klass, NULL, NULL);
	} else {
		int klass_reg = alloc_preg (cfg);

		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, vtable_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));

		if (!klass->rank && !cfg->compile_aot && !(cfg->opt & MONO_OPT_SHARED) && mono_class_is_sealed (klass)) {
			/* the remoting code is broken, access the class for now */
			if (0) { /*FIXME what exactly is broken? This change refers to r39380 from 2005 and mention some remoting fixes were due.*/
				MonoVTable *vt = mono_class_vtable (cfg->domain, klass);
				if (!vt) {
					mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);
					cfg->exception_ptr = klass;
					return NULL;
				}
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, vt);
			} else {
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
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
		MonoInst *args [3];

		if(mini_class_has_reference_variant_generic_argument (cfg, klass, context_used) || is_complex_isinst (klass)) {
			MonoInst *cache_ins = mini_emit_get_rgctx_klass (cfg, context_used, klass, MONO_RGCTX_INFO_CAST_CACHE);

			args [0] = src; /* obj */

			/* klass - it's the second element of the cache entry*/
			EMIT_NEW_LOAD_MEMBASE (cfg, args [1], OP_LOAD_MEMBASE, alloc_preg (cfg), cache_ins->dreg, sizeof (gpointer));

			args [2] = cache_ins; /* cache */
			return emit_isinst_with_cache (cfg, klass, args);
		}

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
		g_assert (!context_used);
		/* the is_null_bb target simply copies the input register to the output */
		mini_emit_iface_cast (cfg, vtable_reg, klass, false_bb, is_null_bb);
	} else {
		int klass_reg = alloc_preg (cfg);

		if (klass->rank) {
			int rank_reg = alloc_preg (cfg);
			int eclass_reg = alloc_preg (cfg);

			g_assert (!context_used);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, rank_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, rank));
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, rank_reg, klass->rank);
			MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, eclass_reg, klass_reg, MONO_STRUCT_OFFSET (MonoClass, cast_class));
			if (klass->cast_class == mono_defaults.object_class) {
				int parent_reg = alloc_preg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, parent_reg, eclass_reg, MONO_STRUCT_OFFSET (MonoClass, parent));
				mini_emit_class_check_branch (cfg, parent_reg, mono_defaults.enum_class->parent, OP_PBNE_UN, is_null_bb);
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (klass->cast_class == mono_defaults.enum_class->parent) {
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class->parent, OP_PBEQ, is_null_bb);
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);				
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (klass->cast_class == mono_defaults.enum_class) {
				mini_emit_class_check_branch (cfg, eclass_reg, mono_defaults.enum_class, OP_PBEQ, is_null_bb);
				MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false_bb);
			} else if (mono_class_is_interface (klass->cast_class)) {
				mini_emit_iface_class_cast (cfg, eclass_reg, klass->cast_class, false_bb, is_null_bb);
			} else {
				if ((klass->rank == 1) && (klass->byval_arg.type == MONO_TYPE_SZARRAY)) {
					/* Check that the object is a vector too */
					int bounds_reg = alloc_preg (cfg);
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, bounds_reg, obj_reg, MONO_STRUCT_OFFSET (MonoArray, bounds));
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, bounds_reg, 0);
					MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false_bb);
				}

				/* the is_null_bb target simply copies the input register to the output */
				mini_emit_isninst_cast (cfg, eclass_reg, klass->cast_class, false_bb, is_null_bb);
			}
		} else if (mono_class_is_nullable (klass)) {
			g_assert (!context_used);
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
			/* the is_null_bb target simply copies the input register to the output */
			mini_emit_isninst_cast (cfg, klass_reg, klass->cast_class, false_bb, is_null_bb);
		} else {
			if (!cfg->compile_aot && !(cfg->opt & MONO_OPT_SHARED) && mono_class_is_sealed (klass)) {
				g_assert (!context_used);
				/* the remoting code is broken, access the class for now */
				if (0) {/*FIXME what exactly is broken? This change refers to r39380 from 2005 and mention some remoting fixes were due.*/
					MonoVTable *vt = mono_class_vtable (cfg->domain, klass);
					if (!vt) {
						mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);
						cfg->exception_ptr = klass;
						return NULL;
					}
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, vtable_reg, vt);
				} else {
					MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, vtable_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
					MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, klass_reg, klass);
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

	MONO_EMIT_NEW_PCONST (cfg, res_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

	MONO_START_BB (cfg, is_null_bb);

	MONO_START_BB (cfg, end_bb);

	return ins;
}

static MonoInst*
emit_castclass_with_cache_nonshared (MonoCompile *cfg, MonoInst *obj, MonoClass *klass)
{
	MonoInst *args [3];
	int idx;

	/* obj */
	args [0] = obj;

	/* klass */
	EMIT_NEW_CLASSCONST (cfg, args [1], klass);

	/* inline cache*/
	idx = get_castclass_cache_idx (cfg);
	args [2] = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_CASTCLASS_CACHE, GINT_TO_POINTER (idx));

	/*The wrapper doesn't inline well so the bloat of inlining doesn't pay off.*/
	return emit_castclass_with_cache (cfg, klass, args);
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
		source = mono_compile_create_var_for_vreg (cfg, &mono_defaults.object_class->byval_arg, OP_LOCAL, ins->sreg1);
	g_assert (source && source != (MonoInst *) -1);

	MonoBasicBlock *first_bb;
	NEW_BBLOCK (cfg, first_bb);
	cfg->cbb = first_bb;

	if (!context_used && (mini_class_has_reference_variant_generic_argument (cfg, klass, context_used) || klass->is_array_special_interface)) {
		if (is_isinst)
			ret = emit_isinst_with_cache_nonshared (cfg, source, klass);
		else
			ret = emit_castclass_with_cache_nonshared (cfg, source, klass);
	} else if (!context_used && (mono_class_is_marshalbyref (klass) || mono_class_is_interface (klass))) {
		MonoInst *iargs [1];
		int costs;

		iargs [0] = source;
		if (is_isinst) {
			MonoMethod *wrapper = mono_marshal_get_isinst (klass);
			costs = mini_inline_method (cfg, wrapper, mono_method_signature (wrapper), iargs, 0, 0, TRUE);
		} else {
			MonoMethod *wrapper = mono_marshal_get_castclass (klass);
			mini_save_cast_details (cfg, klass, source->dreg, TRUE);
			costs = mini_inline_method (cfg, wrapper, mono_method_signature (wrapper), iargs, 0, 0, TRUE);
			mini_reset_cast_details (cfg);
		}
		g_assert (costs > 0);
		ret = iargs [0];
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
	for (MonoBasicBlock *bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		MONO_BB_FOR_EACH_INS (bb, ins) {
			switch (ins->opcode) {
			case OP_ISINST:
			case OP_CASTCLASS:
				mono_decompose_typecheck (cfg, bb, ins);
				break;
			}
		}
	}
}

//Those two functions will go away as we get rid of CEE_MONO_CISINST and CEE_MONO_CCASTCLASS.
MonoInst*
mini_emit_cisinst (MonoCompile *cfg, MonoClass *klass, MonoInst *src)
{
	/* This opcode takes as input an object reference and a class, and returns:
	0) if the object is an instance of the class,
	1) if the object is not instance of the class,
	2) if the object is a proxy whose type cannot be determined */

	MonoInst *ins;
#ifndef DISABLE_REMOTING
	MonoBasicBlock *true_bb, *false_bb, *false2_bb, *end_bb, *no_proxy_bb, *interface_fail_bb;
#else
	MonoBasicBlock *true_bb, *false_bb, *end_bb;
#endif
	int obj_reg = src->dreg;
	int dreg = alloc_ireg (cfg);
	int tmp_reg;
#ifndef DISABLE_REMOTING
	int klass_reg = alloc_preg (cfg);
#endif

	NEW_BBLOCK (cfg, true_bb);
	NEW_BBLOCK (cfg, false_bb);
	NEW_BBLOCK (cfg, end_bb);
#ifndef DISABLE_REMOTING
	NEW_BBLOCK (cfg, false2_bb);
	NEW_BBLOCK (cfg, no_proxy_bb);
#endif

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, false_bb);

	if (mono_class_is_interface (klass)) {
#ifndef DISABLE_REMOTING
		NEW_BBLOCK (cfg, interface_fail_bb);
#endif

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
#ifndef DISABLE_REMOTING
		mini_emit_iface_cast (cfg, tmp_reg, klass, interface_fail_bb, true_bb);
		MONO_START_BB (cfg, interface_fail_bb);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));
		
		mini_emit_class_check_branch (cfg, klass_reg, mono_defaults.transparent_proxy_class, OP_PBNE_UN, false_bb);

		tmp_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, false2_bb);		
#else
		mini_emit_iface_cast (cfg, tmp_reg, klass, false_bb, true_bb);
#endif
	} else {
#ifndef DISABLE_REMOTING
		tmp_reg = alloc_preg (cfg);
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
		
		mini_emit_isninst_cast (cfg, klass_reg, klass, false2_bb, true_bb);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, false2_bb);

		MONO_START_BB (cfg, no_proxy_bb);

		mini_emit_isninst_cast (cfg, klass_reg, klass, false_bb, true_bb);
#else
		g_error ("transparent proxy support is disabled while trying to JIT code that uses it");
#endif
	}

	MONO_START_BB (cfg, false_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 1);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

#ifndef DISABLE_REMOTING
	MONO_START_BB (cfg, false2_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 2);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
#endif

	MONO_START_BB (cfg, true_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 0);

	MONO_START_BB (cfg, end_bb);

	/* FIXME: */
	MONO_INST_NEW (cfg, ins, OP_ICONST);
	ins->dreg = dreg;
	ins->type = STACK_I4;

	return ins;
}

MonoInst*
mini_emit_ccastclass (MonoCompile *cfg, MonoClass *klass, MonoInst *src)
{
	/* This opcode takes as input an object reference and a class, and returns:
	0) if the object is an instance of the class,
	1) if the object is a proxy whose type cannot be determined
	an InvalidCastException exception is thrown otherwhise*/
	
	MonoInst *ins;
#ifndef DISABLE_REMOTING
	MonoBasicBlock *end_bb, *ok_result_bb, *no_proxy_bb, *interface_fail_bb, *fail_1_bb;
#else
	MonoBasicBlock *ok_result_bb;
#endif
	int obj_reg = src->dreg;
	int dreg = alloc_ireg (cfg);
	int tmp_reg = alloc_preg (cfg);

#ifndef DISABLE_REMOTING
	int klass_reg = alloc_preg (cfg);
	NEW_BBLOCK (cfg, end_bb);
#endif

	NEW_BBLOCK (cfg, ok_result_bb);

	MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, obj_reg, 0);
	MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, ok_result_bb);

	mini_save_cast_details (cfg, klass, obj_reg, FALSE);

	if (mono_class_is_interface (klass)) {
#ifndef DISABLE_REMOTING
		NEW_BBLOCK (cfg, interface_fail_bb);
	
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, tmp_reg, klass, interface_fail_bb, ok_result_bb);
		MONO_START_BB (cfg, interface_fail_bb);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, klass_reg, tmp_reg, MONO_STRUCT_OFFSET (MonoVTable, klass));

		mini_emit_class_check (cfg, klass_reg, mono_defaults.transparent_proxy_class);

		tmp_reg = alloc_preg (cfg);		
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoTransparentProxy, custom_type_info));
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, tmp_reg, 0);
		MONO_EMIT_NEW_COND_EXC (cfg, EQ, "InvalidCastException");
		
		MONO_EMIT_NEW_ICONST (cfg, dreg, 1);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);
#else
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, tmp_reg, obj_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mini_emit_iface_cast (cfg, tmp_reg, klass, NULL, NULL);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, ok_result_bb);
#endif
	} else {
#ifndef DISABLE_REMOTING
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
		
		mini_emit_isninst_cast (cfg, klass_reg, klass, fail_1_bb, ok_result_bb);

		MONO_START_BB (cfg, fail_1_bb);

		MONO_EMIT_NEW_ICONST (cfg, dreg, 1);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

		MONO_START_BB (cfg, no_proxy_bb);

		mini_emit_castclass (cfg, obj_reg, klass_reg, klass, ok_result_bb);
#else
		g_error ("Transparent proxy support is disabled while trying to JIT code that uses it");
#endif
	}

	MONO_START_BB (cfg, ok_result_bb);

	MONO_EMIT_NEW_ICONST (cfg, dreg, 0);

#ifndef DISABLE_REMOTING
	MONO_START_BB (cfg, end_bb);
#endif

	/* FIXME: */
	MONO_INST_NEW (cfg, ins, OP_ICONST);
	ins->dreg = dreg;
	ins->type = STACK_I4;

	return ins;
}

//API used by method-to-ir.c
void
mini_emit_class_check (MonoCompile *cfg, int klass_reg, MonoClass *klass)
{
	mini_emit_class_check_inst (cfg, klass_reg, klass, NULL);
}

#endif