/**
 * \file
 * Implement simple alias analysis for local variables.
 *
 * Author:
 *   Rodrigo Kumpera (kumpera@gmail.com)
 *
 * (C) 2013 Xamarin
 */

#include <config.h>
#include <stdio.h>

#include "mini.h"
#include "ir-emit.h"
#include "glib.h"
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

static gboolean
is_int_stack_size (int type)
{
#if SIZEOF_VOID_P == 4
	return type == STACK_I4 || type == STACK_MP;
#else
	return type == STACK_I4;
#endif
}

static gboolean
is_long_stack_size (int type)
{
#if SIZEOF_VOID_P == 8
	return type == STACK_I8 || type == STACK_MP;
#else
	return type == STACK_I8;
#endif
}


static gboolean
lower_load (MonoCompile *cfg, MonoInst *load, MonoInst *ldaddr)
{
	MonoInst *var = (MonoInst *)ldaddr->inst_p0;
	MonoType *type = &var->klass->byval_arg;
	int replaced_op = mono_type_to_load_membase (cfg, type);

	if (load->opcode == OP_LOADV_MEMBASE && load->klass != var->klass) {
		if (cfg->verbose_level > 2)
			printf ("Incompatible load_vtype classes %s x %s\n", load->klass->name, var->klass->name);
		return FALSE;
	}

	if (replaced_op != load->opcode) {
		if (cfg->verbose_level > 2) 
			printf ("Incompatible load type: expected %s but got %s\n", 
				mono_inst_name (replaced_op),
				mono_inst_name (load->opcode));
		return FALSE;
	} else {
		if (cfg->verbose_level > 2) { printf ("mem2reg replacing: "); mono_print_ins (load); }
	}

	load->opcode = mono_type_to_regmove (cfg, type);
	type_to_eval_stack_type (cfg, type, load);
	load->sreg1 = var->dreg;
	mono_atomic_inc_i32 (&mono_jit_stats.loads_eliminated);
	return TRUE;
}

static gboolean
lower_store (MonoCompile *cfg, MonoInst *store, MonoInst *ldaddr)
{
	MonoInst *var = (MonoInst *)ldaddr->inst_p0;
	MonoType *type = &var->klass->byval_arg;
	int replaced_op = mono_type_to_store_membase (cfg, type);

	if (store->opcode == OP_STOREV_MEMBASE && store->klass != var->klass) {
		if (cfg->verbose_level > 2)
			printf ("Incompatible store_vtype classes %s x %s\n", store->klass->name, store->klass->name);
		return FALSE;
	}


	if (replaced_op != store->opcode) {
		if (cfg->verbose_level > 2) 
			printf ("Incompatible store_reg type: expected %s but got %s\n", 
				mono_inst_name (replaced_op),
				mono_inst_name (store->opcode));
		return FALSE;
	} else {
		if (cfg->verbose_level > 2) { printf ("mem2reg replacing: "); mono_print_ins (store); }
	}

	store->opcode = mono_type_to_regmove (cfg, type);
	type_to_eval_stack_type (cfg, type, store);
	store->dreg = var->dreg;
	mono_atomic_inc_i32 (&mono_jit_stats.stores_eliminated);
	return TRUE;
}

static gboolean
lower_store_imm (MonoCompile *cfg, MonoInst *store, MonoInst *ldaddr)
{
	MonoInst *var = (MonoInst *)ldaddr->inst_p0;
	MonoType *type = &var->klass->byval_arg;
	int store_op = mono_type_to_store_membase (cfg, type);
	if (store_op == OP_STOREV_MEMBASE || store_op == OP_STOREX_MEMBASE)
		return FALSE;

	switch (store->opcode) {
#if SIZEOF_VOID_P == 4
	case OP_STORE_MEMBASE_IMM:
#endif
	case OP_STOREI4_MEMBASE_IMM:
		if (!is_int_stack_size (var->type)) {
			if (cfg->verbose_level > 2) printf ("Incompatible variable of size != 4\n");
			return FALSE;
		}
		if (cfg->verbose_level > 2) { printf ("mem2reg replacing: "); mono_print_ins (store); }
		store->opcode = OP_ICONST;
		store->type = STACK_I4;
		store->dreg = var->dreg;
		store->inst_c0 = store->inst_imm;
		break;

#if SIZEOF_VOID_P == 8
	case OP_STORE_MEMBASE_IMM:
#endif    
	case OP_STOREI8_MEMBASE_IMM:
	 	if (!is_long_stack_size (var->type)) {
			if (cfg->verbose_level > 2) printf ("Incompatible variable of size != 8\n");
			return FALSE;
		}
		if (cfg->verbose_level > 2) { printf ("mem2reg replacing: "); mono_print_ins (store); }
		store->opcode = OP_I8CONST;
		store->type = STACK_I8;
		store->dreg = var->dreg;
		store->inst_l = store->inst_imm;
		break;
	default:
		return FALSE;
	}
	mono_atomic_inc_i32 (&mono_jit_stats.stores_eliminated);
	return TRUE;
}

static gboolean
lower_memory_access (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoInst *ins, *tmp;
	gboolean needs_dce = FALSE;
	GHashTable *addr_loads = g_hash_table_new (NULL, NULL);
	//FIXME optimize
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		g_hash_table_remove_all (addr_loads);

		for (ins = bb->code; ins; ins = ins->next) {
handle_instruction:
			switch (ins->opcode) {
			case OP_LDADDR: {
				MonoInst *var = (MonoInst*)ins->inst_p0;
				if (var->flags & MONO_INST_VOLATILE) {
					if (cfg->verbose_level > 2) { printf ("Found address to volatile var, can't take it: "); mono_print_ins (ins); }
				} else {
					g_hash_table_insert (addr_loads, GINT_TO_POINTER (ins->dreg), ins);
					if (cfg->verbose_level > 2) { printf ("New address: "); mono_print_ins (ins); }
				}
				break;
			}

			case OP_MOVE:
				tmp = (MonoInst*)g_hash_table_lookup (addr_loads, GINT_TO_POINTER (ins->sreg1));
				/*
				Forward propagate known aliases
				ldaddr R10 <- R8
				mov R11 <- R10
				*/
				if (tmp) {
					g_hash_table_insert (addr_loads, GINT_TO_POINTER (ins->dreg), tmp);
					if (cfg->verbose_level > 2) { printf ("New alias: "); mono_print_ins (ins); }
				} else {
					/*
					Source value is not a know address, kill the variable.
					*/
					if (g_hash_table_remove (addr_loads, GINT_TO_POINTER (ins->dreg))) {
						if (cfg->verbose_level > 2) { printf ("Killed alias: "); mono_print_ins (ins); }
					}
				}
				break;

			case OP_LOADV_MEMBASE:
			case OP_LOAD_MEMBASE:
			case OP_LOADU1_MEMBASE:
			case OP_LOADI2_MEMBASE:
			case OP_LOADU2_MEMBASE:
			case OP_LOADI4_MEMBASE:
			case OP_LOADU4_MEMBASE:
			case OP_LOADI1_MEMBASE:
			case OP_LOADI8_MEMBASE:
#ifndef MONO_ARCH_SOFT_FLOAT_FALLBACK
			case OP_LOADR4_MEMBASE:
#endif
			case OP_LOADR8_MEMBASE:
				if (ins->inst_offset != 0)
					continue;
				tmp = (MonoInst *)g_hash_table_lookup (addr_loads, GINT_TO_POINTER (ins->sreg1));
				if (tmp) {
					if (cfg->verbose_level > 2) { printf ("Found candidate load:"); mono_print_ins (ins); }
					if (lower_load (cfg, ins, tmp)) {
						needs_dce = TRUE;
						/* Try to propagate known aliases if an OP_MOVE was inserted */
						goto handle_instruction;
					}
				}
				break;

			case OP_STORE_MEMBASE_REG:
			case OP_STOREI1_MEMBASE_REG:
			case OP_STOREI2_MEMBASE_REG:
			case OP_STOREI4_MEMBASE_REG:
			case OP_STOREI8_MEMBASE_REG:
#ifndef MONO_ARCH_SOFT_FLOAT_FALLBACK
			case OP_STORER4_MEMBASE_REG:
#endif
			case OP_STORER8_MEMBASE_REG:
			case OP_STOREV_MEMBASE:
				if (ins->inst_offset != 0)
					continue;
				tmp = (MonoInst *)g_hash_table_lookup (addr_loads, GINT_TO_POINTER (ins->dreg));
				if (tmp) {
					if (cfg->verbose_level > 2) { printf ("Found candidate store:"); mono_print_ins (ins); }
					if (lower_store (cfg, ins, tmp)) {
						needs_dce = TRUE;
						/* Try to propagate known aliases if an OP_MOVE was inserted */
						goto handle_instruction;
					}
				}
				break;
			//FIXME missing storei1_membase_imm and storei2_membase_imm
			case OP_STORE_MEMBASE_IMM:
			case OP_STOREI4_MEMBASE_IMM:
			case OP_STOREI8_MEMBASE_IMM:
				if (ins->inst_offset != 0)
					continue;
				tmp = (MonoInst *)g_hash_table_lookup (addr_loads, GINT_TO_POINTER (ins->dreg));
				if (tmp) {
					if (cfg->verbose_level > 2) { printf ("Found candidate store-imm:"); mono_print_ins (ins); }
					needs_dce |= lower_store_imm (cfg, ins, tmp);
				}
				break;
			case OP_CHECK_THIS:
			case OP_NOT_NULL:
				tmp = (MonoInst *)g_hash_table_lookup (addr_loads, GINT_TO_POINTER (ins->sreg1));
				if (tmp) {
					if (cfg->verbose_level > 2) { printf ("Found null check over local: "); mono_print_ins (ins); }
					NULLIFY_INS (ins);
					needs_dce = TRUE;
				}
				break;
			}
		}
	}
	g_hash_table_destroy (addr_loads);
	return needs_dce;
}

static gboolean
recompute_aliased_variables (MonoCompile *cfg, int *restored_vars)
{
	int i;
	MonoBasicBlock *bb;
	MonoInst *ins;
	int kills = 0;
	int adds = 0;
	*restored_vars = 0;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *var = cfg->varinfo [i];
		if (var->flags & MONO_INST_INDIRECT) {
			if (cfg->verbose_level > 2) {
				printf ("Killing :"); mono_print_ins (var);
			}
			++kills;
		}
		var->flags &= ~MONO_INST_INDIRECT;
	}

	if (!kills)
		return FALSE;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		for (ins = bb->code; ins; ins = ins->next) {
			if (ins->opcode == OP_LDADDR) {
				MonoInst *var;

				if (cfg->verbose_level > 2) { printf ("Found op :"); mono_print_ins (ins); }

				var = (MonoInst*)ins->inst_p0;
				if (!(var->flags & MONO_INST_INDIRECT)) {
					if (cfg->verbose_level > 1) { printf ("Restoring :"); mono_print_ins (var); }
					++adds;
				}
				var->flags |= MONO_INST_INDIRECT;
			}
		}
	}
	*restored_vars = adds;

	mono_atomic_fetch_add_i32 (&mono_jit_stats.alias_found, kills);
	mono_atomic_fetch_add_i32 (&mono_jit_stats.alias_removed, kills - adds);
	if (kills > adds) {
		if (cfg->verbose_level > 2) {
			printf ("Method: %s\n", mono_method_full_name (cfg->method, 1));
			printf ("Kills %d Adds %d\n", kills, adds);
		}
		return TRUE;
	}
	return FALSE;
}

/*
FIXME:
	Don't DCE on the whole CFG, only the BBs that have changed.

TODO:
	SRVT of small types can fix cases of mismatch for fields of a different type than the component.
	Handle aliasing of byrefs in call conventions.
*/
void
mono_local_alias_analysis (MonoCompile *cfg)
{
	int i, restored_vars = 1;
	if (!cfg->has_indirection)
		return;

	if (cfg->verbose_level > 2)
		mono_print_code (cfg, "BEFORE ALIAS_ANALYSIS");

	/*
	Remove indirection and memory access of known variables.
	*/
	if (!lower_memory_access (cfg))
		goto done;

	/*
	By replacing indirect access with direct operations, some LDADDR ops become dead. Kill them.
	*/
	if (cfg->opt & MONO_OPT_DEADCE)
		mono_local_deadce (cfg);

	/*
	Some variables no longer need to be flagged as indirect, find them.
	Since indirect vars are converted into global vregs, each pass eliminates only one level of indirection.
	Most cases only need one pass and some 2.
	*/
	for (i = 0; i < 3 && restored_vars > 0 && recompute_aliased_variables (cfg, &restored_vars); ++i) {
		/*
		A lot of simplification just took place, we recompute local variables and do DCE to
		really profit from the previous gains
		*/
		mono_handle_global_vregs (cfg);
		if (cfg->opt & MONO_OPT_DEADCE)
			mono_local_deadce (cfg);
	}

done:
	if (cfg->verbose_level > 2)
		mono_print_code (cfg, "AFTER ALIAS_ANALYSIS");
}

#else /* !DISABLE_JIT */

MONO_EMPTY_SOURCE_FILE (alias_analysis);

#endif /* !DISABLE_JIT */
