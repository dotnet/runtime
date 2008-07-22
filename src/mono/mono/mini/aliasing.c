/*
 * aliasing.c: Alias Analysis
 *
 * Author:
 *   Massimiliano Mantione (massi@ximian.com)
 *
 * (C) 2005 Novell, Inc.  http://www.novell.com
 */
#include <string.h>
#include <stdio.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool.h>
#include <mono/metadata/opcodes.h>

#include "aliasing.h"

#define MONO_APPLY_DEADCE_TO_SINGLE_METHOD 0
#define DEBUG_DEADCE 0

#define DEBUG_ALIAS_ANALYSIS 0

#define TRACE_ALIAS_ANALYSIS (info->cfg->verbose_level > 2)
#define DUMP_ALIAS_ANALYSIS (info->cfg->verbose_level > 4)
#define FOLLOW_ALIAS_ANALYSIS (info->cfg->verbose_level > 5)

static const char *mono_aliasing_value_names[] = {
	"ANY",
	"NO_ALIAS",
	"LOCAL",
	"LOCAL_FIELD"
};


static const char *mono_stack_type_names[] = {
	"STACK_INV",
	"STACK_I4",
	"STACK_I8",
	"STACK_PTR",
	"STACK_R8",
	"STACK_MP",
	"STACK_OBJ",
	"STACK_VTYPE",
	"STACK_MAX"
};

#define NO_VARIABLE_INDEX MONO_ALIASING_INVALID_VARIABLE_INDEX


#define OP_IS_OUTARG(op) (((op)==OP_OUTARG)||((op)==OP_OUTARG_REG)||((op)==OP_OUTARG_IMM)||((op)==OP_OUTARG_R4)||((op)==OP_OUTARG_R8)||((op)==OP_OUTARG_VT))
#define OP_IS_CALL(op) (((op)==CEE_CALLI)||((op)==OP_CALL)||((op)==OP_CALLVIRT)||(((op)>=OP_VOIDCALL)&&((op)<=OP_CALL_MEMBASE)))
#define OP_IS_STORE(op) (((op)==CEE_STIND_REF)||((op)==CEE_STIND_I1)||((op)==CEE_STIND_I2)||((op)==CEE_STIND_I4)||((op)==CEE_STIND_I8)||((op)==CEE_STIND_R4)||((op)==CEE_STIND_R8)||((op)==CEE_STIND_I))
#define OP_IS_LOAD(op) (((op)==CEE_LDIND_REF)||((op)==CEE_LDIND_I1)||((op)==CEE_LDIND_I2)||((op)==CEE_LDIND_I4)||((op)==CEE_LDIND_U1)||((op)==CEE_LDIND_U2)||((op)==CEE_LDIND_U4)||((op)==CEE_LDIND_I8)||((op)==CEE_LDIND_R4)||((op)==CEE_LDIND_R8)||((op)==CEE_LDIND_I))
#define OP_IS_CONST(op) (((op)==OP_ICONST)||((op)==OP_I8CONST)||((op)==OP_R4CONST)||((op)==OP_R8CONST)||((op)==OP_AOTCONST))
#define OP_IS_ICONV(op) (((op)==CEE_CONV_I4)||((op)==CEE_CONV_U4)||((op)==CEE_CONV_I8)||((op)==CEE_CONV_U8)||\
		((op)==CEE_CONV_OVF_I4_UN)||((op)==CEE_CONV_OVF_I8_UN)||((op)==CEE_CONV_OVF_U4_UN)||((op)==CEE_CONV_OVF_U8_UN)||\
		((op)==CEE_CONV_OVF_I4)||((op)==CEE_CONV_OVF_U4)||((op)==CEE_CONV_OVF_I8)||((op)==CEE_CONV_OVF_U8)||\
		((op)==OP_LCONV_TO_I8)||((op)==OP_LCONV_TO_OVF_I8)||((op)==OP_LCONV_TO_OVF_I8_UN)||\
		((op)==OP_LCONV_TO_U8)||((op)==OP_LCONV_TO_OVF_U8)||((op)==OP_LCONV_TO_OVF_U8_UN))
#define OP_IS_PCONV(op) (((op)==CEE_CONV_OVF_I_UN)||((op)==CEE_CONV_OVF_U_UN)||\
		((op)==CEE_CONV_I)||((op)==CEE_CONV_U)||\
		((op)==CEE_CONV_OVF_I)||((op)==CEE_CONV_OVF_U)||\
		((op)==OP_LCONV_TO_I)||((op)==OP_LCONV_TO_OVF_I)||((op)==OP_LCONV_TO_OVF_I_UN)||\
		((op)==OP_LCONV_TO_U)||((op)==OP_LCONV_TO_OVF_U)||((op)==OP_LCONV_TO_OVF_U_UN))
#define LOAD_OF_LOCAL_GIVES_POINTER(load,local) ((local->opcode == OP_LOCAL) && (((load)->type == STACK_MP) || ((load)->type == STACK_PTR) || ((local)->inst_vtype->type == MONO_TYPE_PTR)))


/*
 * A struct representing the context of the traversal of a MonoInst tree.
 * Used so that "update_aliasing_information_on_inst" can understand what
 * its caller was doing, and expecially where the current value is going
 * to be stored (if it is an alias, we must track it).
 */
typedef struct MonoAliasingInformationContext {
	MonoInst *inst;
	int current_subtree;
	MonoAliasValue subtree_aliases [2];
	
	struct MonoAliasingInformationContext *father;
} MonoAliasingInformationContext;


static void
print_alias_value (MonoAliasValue *alias_value) {
	printf ("[%s", mono_aliasing_value_names [alias_value->type]);
	if ((alias_value->type == MONO_ALIASING_TYPE_LOCAL) || (alias_value->type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
		printf (":%d]", alias_value->variable_index);
	} else {
		printf ("]");
	}
}

static void
print_ssaop_value (int ssaop_value) {
	printf ("[");
	if (ssaop_value & MONO_SSA_ADDRESS_TAKEN) printf ("I"); else printf (".");
	if (ssaop_value & MONO_SSA_LOAD) printf ("R"); else printf (".");
	if (ssaop_value & MONO_SSA_STORE) printf ("W"); else printf (".");
	printf ("]");
}

static void
print_aliasing_context (MonoAliasingInformationContext *context) {
	printf ("CONTEXT: left ");
	print_alias_value (&(context->subtree_aliases [0]));
	printf (", right ");
	print_alias_value (&(context->subtree_aliases [1]));
	if (context->father != NULL) {
		printf (", father ");
		print_alias_value (&(context->father->subtree_aliases [context->father->current_subtree]));
	}
	printf (", stack %s ", mono_stack_type_names [context->inst->type]);
	if (context->inst->ssa_op != MONO_SSA_NOP) {
		print_ssaop_value (context->inst->ssa_op);
		printf (" ");
	}
	printf ("in inst ");
	mono_print_tree_nl (context->inst);
}

static void
print_tree_node (MonoInst *tree) {
	if (!tree)
		return;
	
	printf (mono_inst_name (tree->opcode));
	
	if (OP_IS_OUTARG (tree->opcode)) {
		printf ("[OUT:%ld]", (long)tree->inst_c1);
	}
	
	switch (tree->opcode) {
	case OP_ICONST:
		printf ("[%d]", (int)tree->inst_c0);
		break;
	case OP_I8CONST:
		printf ("[%lld]", (long long)tree->inst_l);
		break;
	case OP_R8CONST:
		printf ("[%f]", *(double*)tree->inst_p0);
		break;
	case OP_R4CONST:
		printf ("[%f]", *(float*)tree->inst_p0);
		break;
	case OP_ARG:
	case OP_LOCAL:
		printf ("[%d]", (int)tree->inst_c0);
		break;
	case OP_REGOFFSET:
		if (tree->inst_offset < 0)
			printf ("[-0x%x(%s)]", (int)(-tree->inst_offset), mono_arch_regname (tree->inst_basereg));
		else
			printf ("[0x%x(%s)]", (int)(tree->inst_offset), mono_arch_regname (tree->inst_basereg));
		break;
	case OP_REGVAR:
		printf ("[%s]", mono_arch_regname (tree->dreg));
		break;
	case CEE_NEWARR:
		printf ("[%s]",  tree->inst_newa_class->name);
		break;
	case OP_CALL:
	case OP_CALLVIRT:
	case OP_FCALL:
	case OP_FCALLVIRT:
	case OP_LCALL:
	case OP_LCALLVIRT:
	case OP_VCALL:
	case OP_VCALLVIRT:
	case OP_VOIDCALL:
	case OP_VOIDCALLVIRT:
	case OP_TRAMPCALL_VTABLE:
	case OP_CALL_RGCTX:
	case OP_FCALL_RGCTX:
	case OP_VOIDCALL_RGCTX:
	case OP_LCALL_RGCTX:
	case OP_VCALL_RGCTX:
	case OP_CALL_REG_RGCTX:
	case OP_FCALL_REG_RGCTX:
	case OP_VOIDCALL_REG_RGCTX:
	case OP_LCALL_REG_RGCTX:
	case OP_VCALL_REG_RGCTX:
	case OP_CALLVIRT_IMT:
	case OP_VOIDCALLVIRT_IMT:
	case OP_FCALLVIRT_IMT:
	case OP_LCALLVIRT_IMT:
	case OP_VCALLVIRT_IMT: {
		MonoCallInst *call = (MonoCallInst*)tree;
		if (call->method)
			printf ("[%s]", call->method->name);
		else if (call->fptr) {
			MonoJitICallInfo *info = mono_find_jit_icall_by_addr (call->fptr);
			if (info)
				printf ("[%s]", info->name);
		}
		printf ("[ARGS:%d]", call->signature->param_count);
		break;
	}
	case OP_PHI: {
		int i;
		printf ("[%d (", (int)tree->inst_c0);
		for (i = 0; i < tree->inst_phi_args [0]; i++) {
			if (i)
				printf (", ");
			printf ("%d", tree->inst_phi_args [i + 1]);
		}
		printf (")]");
		break;
	}
	case OP_LOAD_MEMBASE:
	case OP_LOADI4_MEMBASE:
	case OP_LOADU4_MEMBASE:
	case OP_LOADU1_MEMBASE:
	case OP_LOADI1_MEMBASE:
	case OP_LOADU2_MEMBASE:
	case OP_LOADI2_MEMBASE:
		printf ("[%s] <- [%s + 0x%x]", mono_arch_regname (tree->dreg), mono_arch_regname (tree->inst_basereg), (int)tree->inst_offset);
		break;
	case OP_BR:
	case OP_CALL_HANDLER:
		printf ("[B%d]", tree->inst_target_bb->block_num);
		break;
	case CEE_BNE_UN:
	case CEE_BEQ:
	case CEE_BLT:
	case CEE_BLT_UN:
	case CEE_BGT:
	case CEE_BGT_UN:
	case CEE_BGE:
	case CEE_BGE_UN:
	case CEE_BLE:
	case CEE_BLE_UN:
		printf ("[B%dB%d]", tree->inst_true_bb->block_num, tree->inst_false_bb->block_num);
		break;
	case OP_DUMMY_USE:
		printf ("[%d]", (int)tree->inst_i0->inst_i0->inst_c0);
		break;
	case OP_DUMMY_STORE:
		printf ("[%d]", (int)tree->inst_i0->inst_c0);
		break;
	default:
		break;
	}
}

static void
print_variable_list (MonoLocalVariableList* variables) {
	printf ("{");
	while (variables != NULL) {
		printf ("%d", variables->variable_index);
		if (variables->next != NULL) {
			printf (",");
		}
		variables = variables->next;
	}
	printf ("}");
}

static void
print_used_aliases(MonoInst *inst, MonoLocalVariableList* affected_variables) {
	if (inst->ssa_op != MONO_SSA_NOP) {
		printf (" <");
		if (inst->ssa_op & MONO_SSA_ADDRESS_TAKEN) printf ("I");
		if (inst->ssa_op & MONO_SSA_LOAD) printf ("R");
		if (inst->ssa_op & MONO_SSA_STORE) printf ("W");
		if (inst->ssa_op != MONO_SSA_ADDRESS_TAKEN) {
			print_variable_list (affected_variables);
		} else {
			switch (inst->inst_i0->opcode) {
			case OP_LOCAL:
			case OP_ARG:
				printf ("{%ld}", (long)inst->inst_i0->inst_c0);
				break;
			case OP_RETARG:
				printf ("{RETARG}");
				break;
			default:
				printf ("{ANY}");
				break;
			}
		}
		printf (">");
	}
}

static void
print_tree_with_aliasing_information (MonoAliasingInformation *info, MonoInst *tree) {
	int arity;
	MonoLocalVariableList* affected_variables;

	if (!tree) {
		printf ("NULL-INST");
		return;
	}
	
	arity = mono_burg_arity [tree->opcode];
	
	print_tree_node (tree);
	
	if (OP_IS_CALL (tree->opcode) && arity) {
		printf (" THIS:");
	}
	
	if (arity) {
		printf (" (");
		print_tree_with_aliasing_information (info, tree->inst_i0);
		if (arity > 1) {
			printf (" ");
			print_tree_with_aliasing_information (info, tree->inst_i1);
		}
		printf (")");
	}
	
	affected_variables = mono_aliasing_get_affected_variables_for_inst_traversing_code (info, tree);
	print_used_aliases (tree, affected_variables);
}

static void
print_code_with_aliasing_information (MonoAliasingInformation *info) {
	char *name = mono_method_full_name (info->cfg->method, TRUE);
	int i;
	
	printf ("ALIASING DATA START (METHOD %s)\n", name);
	printf ("ALIASED VARIABLES: ");
	print_variable_list (info->uncontrollably_aliased_variables);
	printf ("\n");
	for (i = 0; i < info->cfg->num_bblocks; i++) {
		MonoAliasingInformationInBB *bb_info = &(info->bb [i]);
		MonoAliasUsageInformation *use;
		MonoInst *inst;
		
		printf ("CODE FOR BB %d\n", bb_info->bb->block_num);
		mono_aliasing_initialize_code_traversal (info, bb_info->bb);
		MONO_BB_FOR_EACH_INS (bb_info->bb, inst) {
			print_tree_with_aliasing_information (info, inst);
			printf ("\n");
		}
		
		printf ("USES FOR BB %d\n", bb_info->bb->block_num);
		for (use = bb_info->potential_alias_uses; use != NULL; use = use->next) {
			mono_print_tree (use->inst);
			print_used_aliases (use->inst, use->affected_variables);
			printf ("\n");
		}		
	}
	printf ("ALIASING DATA END (METHOD %s)\n", name);
	g_free (name);
}	


#define APPEND_USE(info,bb_info,use) do {\
		(use)->next = NULL;\
		if ((info)->next_interesting_inst != NULL) {\
			(info)->next_interesting_inst->next = (use);\
		} else {\
			(bb_info)->potential_alias_uses = (use);\
		}\
		(info)->next_interesting_inst = (use);\
	} while (0)
	
#define ADD_BAD_ALIAS(info,vi) do {\
		if (FOLLOW_ALIAS_ANALYSIS) {\
			printf ("ADDING BAD ALIAS FOR VARIABLE %d\n", vi);\
		}\
		if (! ((info)->variable_is_uncontrollably_aliased [(vi)])) {\
			MonoLocalVariableList *variable = mono_mempool_alloc ((info)->mempool, sizeof (MonoLocalVariableList));\
			variable->variable_index = (vi);\
			variable->next = (info)->uncontrollably_aliased_variables;\
			(info)->uncontrollably_aliased_variables = variable;\
			(info)->variable_is_uncontrollably_aliased [(vi)] = TRUE;\
		}\
	} while (0)

#define ADD_ARGUMGENT(info,inst,alias) do {\
		if ((info)->number_of_arguments == (info)->arguments_capacity) {\
			MonoInst **new_arguments = mono_mempool_alloc ((info)->mempool, sizeof (MonoInst*) * ((info)->arguments_capacity * 2));\
			MonoAliasValue *new_arguments_aliases = mono_mempool_alloc ((info)->mempool, sizeof (MonoAliasValue) * ((info)->arguments_capacity * 2));\
			memcpy (new_arguments, (info)->arguments, sizeof (MonoInst*) * ((info)->arguments_capacity));\
			memcpy (new_arguments_aliases, (info)->arguments_aliases, sizeof (MonoAliasValue) * ((info)->arguments_capacity));\
			(info)->arguments = new_arguments;\
			(info)->arguments_aliases = new_arguments_aliases;\
			(info)->arguments_capacity = (info)->arguments_capacity * 2;\
		}\
		(info)->arguments [(info)->number_of_arguments] = (inst);\
		(info)->arguments_aliases [(info)->number_of_arguments] = (alias);\
		(info)->number_of_arguments ++;\
	} while (0)

#define ADD_UNIQUE_VARIABLE(info,list,vi) do {\
		MonoLocalVariableList* target_element = (list);\
		while ((target_element != NULL) && (target_element->variable_index != (vi))) {\
			target_element = target_element->next;\
		}\
		if (target_element == NULL) {\
			target_element = mono_mempool_alloc ((info)->mempool, sizeof (MonoLocalVariableList));\
			target_element->variable_index = (vi);\
			target_element->next = (list);\
			(list) = target_element;\
		}\
	} while (0)

static void
update_aliasing_information_on_inst (MonoAliasingInformation *info, MonoAliasingInformationInBB *bb_info, MonoInst *inst, MonoAliasingInformationContext *father_context) {
	MonoAliasingInformationContext context;
	MonoAliasValue *father_alias;
	
	context.inst = inst;
	context.father = father_context;
	if (father_context != NULL) {
		father_alias = &(father_context->subtree_aliases [father_context->current_subtree]);
	} else {
		father_alias = NULL;
	}
	
	if (mono_burg_arity [inst->opcode]) {
		context.current_subtree = 0;
		context.subtree_aliases [0].type = MONO_ALIASING_TYPE_ANY;
		context.subtree_aliases [0].variable_index = NO_VARIABLE_INDEX;
		update_aliasing_information_on_inst (info, bb_info, inst->inst_i0, &context);
		
		if (mono_burg_arity [inst->opcode] > 1) {
			context.current_subtree = 1;
			context.subtree_aliases [1].type = MONO_ALIASING_TYPE_ANY;
			context.subtree_aliases [1].variable_index = NO_VARIABLE_INDEX;
			update_aliasing_information_on_inst (info, bb_info, inst->inst_i1, &context);
		} else {
			context.subtree_aliases [1].type = MONO_ALIASING_TYPE_NO_ALIAS;
		}
	} else {
		context.subtree_aliases [0].type = MONO_ALIASING_TYPE_NO_ALIAS;
		context.subtree_aliases [1].type = MONO_ALIASING_TYPE_NO_ALIAS;
	}
	
	if (OP_IS_CONST (inst->opcode)) {
		father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
	} else if (inst->ssa_op == MONO_SSA_ADDRESS_TAKEN) {
		MonoInst *local = inst->inst_i0;
		if ((local->opcode == OP_LOCAL) || (local->opcode == OP_ARG)) {
			gssize variable_index = local->inst_c0;
			father_alias->type = MONO_ALIASING_TYPE_LOCAL;
			father_alias->variable_index = variable_index;
		} else if (local->opcode == OP_RETARG) {
			father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
		} else {
			father_alias->type = MONO_ALIASING_TYPE_ANY;
		}
	} else if (inst->ssa_op == MONO_SSA_LOAD) {
		MonoInst *local = inst->inst_i0;
		
		if (LOAD_OF_LOCAL_GIVES_POINTER (inst,local)) {
			father_alias->type = MONO_ALIASING_TYPE_ANY;
		} else {
			father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
		}
	} else if (OP_IS_LOAD (inst->opcode) || (inst->opcode == CEE_LDOBJ)) {
		MonoInst *address = inst->inst_i0;
		MonoLocalVariableList *affected_variables = NULL;
		
		if ((address->opcode == OP_LOCAL) || (address->opcode == OP_ARG)) {
			gssize variable_index = address->inst_c0;
			MonoInst *local = info->cfg->varinfo [variable_index];
			
			affected_variables = &(info->variables [variable_index]);
			if (LOAD_OF_LOCAL_GIVES_POINTER (inst,local)) {
				father_alias->type = MONO_ALIASING_TYPE_ANY;
			} else {
				father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
			}
		} else if (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL) {
			gssize variable_index = context.subtree_aliases [0].variable_index;
			MonoInst *local = info->cfg->varinfo [variable_index];
			
			affected_variables = &(info->variables [variable_index]);;
			if (LOAD_OF_LOCAL_GIVES_POINTER (inst,local)) {
				father_alias->type = MONO_ALIASING_TYPE_ANY;
			} else {
				father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
			}
		} else if (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL_FIELD) {
			gssize variable_index = context.subtree_aliases [0].variable_index;
			
			affected_variables = &(info->variables [variable_index]);;
			if (inst->type == STACK_MP) {
				father_alias->type = MONO_ALIASING_TYPE_ANY;
			} else {
				father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
			}
		} else {
			if (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_ANY) {
				affected_variables = info->temporary_uncontrollably_aliased_variables;
			}
			if ((inst->type == STACK_MP) && (inst->inst_i0->opcode != OP_OBJADDR)) {
				father_alias->type = MONO_ALIASING_TYPE_ANY;
			} else {
				father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
			}
		}
		
		if (affected_variables != NULL) {
			MonoAliasUsageInformation *use = mono_mempool_alloc (info->mempool, sizeof (MonoAliasUsageInformation));
			
			inst->ssa_op = MONO_SSA_INDIRECT_LOAD;
			use->inst = inst;
			use->affected_variables = affected_variables;
			APPEND_USE (info,bb_info,use);
		}
	} else if (inst->ssa_op == MONO_SSA_STORE) {
		if (context.subtree_aliases [1].type == MONO_ALIASING_TYPE_LOCAL) {
			ADD_BAD_ALIAS (info, context.subtree_aliases [1].variable_index);
		}
	} else if (OP_IS_STORE (inst->opcode) || (inst->opcode == CEE_STOBJ)) {
		MonoInst *address = inst->inst_i0;
		MonoLocalVariableList *affected_variables = NULL;
		
		if (context.subtree_aliases [1].type == MONO_ALIASING_TYPE_LOCAL) {
			ADD_BAD_ALIAS (info, context.subtree_aliases [1].variable_index);
		}
		
		if ((address->opcode == OP_LOCAL) || (address->opcode == OP_ARG)) {
			gssize variable_index = address->inst_c0;
			
			affected_variables = &(info->variables [variable_index]);
		} else if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			gssize variable_index = context.subtree_aliases [0].variable_index;
			
			affected_variables = &(info->variables [variable_index]);;
		} else if (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_ANY) {
			affected_variables = info->temporary_uncontrollably_aliased_variables;
		}
		
		if (affected_variables != NULL) {
			MonoAliasUsageInformation *use = mono_mempool_alloc (info->mempool, sizeof (MonoAliasUsageInformation));
			
			inst->ssa_op = MONO_SSA_INDIRECT_STORE;
			use->inst = inst;
			use->affected_variables = affected_variables;
			APPEND_USE (info,bb_info,use);
		}
	} else if (OP_IS_OUTARG (inst->opcode)) {
		ADD_ARGUMGENT (info,inst,context.subtree_aliases [0]);
	} else if (OP_IS_CALL (inst->opcode)) {
		MonoCallInst *call = (MonoCallInst *) inst;
		MonoMethodSignature *sig = call->signature;
		gboolean call_has_untracked_pointer_argument = FALSE;
		MonoLocalVariableList *alias_arguments = NULL;
		int i;
		
		if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			ADD_UNIQUE_VARIABLE (info,alias_arguments,context.subtree_aliases [0].variable_index);
		} else if (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_ANY) {
			call_has_untracked_pointer_argument = TRUE;
		}
		
		if (FOLLOW_ALIAS_ANALYSIS) {
			printf ("CALL, scanning %d arguments\n", info->number_of_arguments);
		}
		for (i = 0; i < info->number_of_arguments; i++) {
			//FIXME
			//MonoInst *argument = info->arguments [i];
			MonoAliasValue arguments_alias = info->arguments_aliases [i];
			
			if ((arguments_alias.type == MONO_ALIASING_TYPE_LOCAL) || (arguments_alias.type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
				if (FOLLOW_ALIAS_ANALYSIS) {
					printf ("CALL, argument %d passes the address of local %d\n", i, arguments_alias.variable_index);
				}
				ADD_UNIQUE_VARIABLE (info,alias_arguments,arguments_alias.variable_index);
				//FIXME
				#if 0
				if (((arguments_alias.type == MONO_ALIASING_TYPE_LOCAL)) && (argument->inst_c1 > 0)) {
					int argument_index = argument->inst_c1 - 1;
					if (argument_index < sig->param_count) {
						if (! (sig->params [argument_index]->byref)) {
							ADD_BAD_ALIAS (info, arguments_alias.variable_index);
						}
					} else {
						printf ("*** ERROR: argument %d of %d: ", argument_index, sig->param_count);
						mono_print_tree_nl (argument);
					}
				}
				#endif
			} else if (arguments_alias.type == MONO_ALIASING_TYPE_ANY) {
				if (FOLLOW_ALIAS_ANALYSIS) {
					printf ("CALL, argument %d could pass the address of any local\n", i);
				}
				call_has_untracked_pointer_argument = TRUE;
			}
			//FIXME
			#if 0
			else if (argument->inst_c1 > 0) {
				int argument_index = argument->inst_c1 - 1;
				if (argument_index < sig->param_count) {
					if (sig->params [argument_index]->type == MONO_TYPE_PTR) {
						call_has_untracked_pointer_argument = TRUE;
					}
				} else {
					printf ("*** ERROR: argument %d of %d: ", argument_index, sig->param_count);
					mono_print_tree_nl (argument);
				}
			}
			#endif
		}
		
		if ((alias_arguments != NULL) || call_has_untracked_pointer_argument) {
			MonoAliasUsageInformation *use = mono_mempool_alloc (info->mempool, sizeof (MonoAliasUsageInformation));
			
			inst->ssa_op = MONO_SSA_INDIRECT_LOAD_STORE;
			use->inst = inst;
			use->affected_variables = alias_arguments;
			if (call_has_untracked_pointer_argument) {
				MonoLocalVariableList *untracked_element  = mono_mempool_alloc ((info)->mempool, sizeof (MonoLocalVariableList));
				untracked_element->variable_index = NO_VARIABLE_INDEX;
				untracked_element->next = use->affected_variables;
				use->affected_variables = untracked_element;
			}
			APPEND_USE (info,bb_info,use);
		}
		
		if ((sig->ret != NULL) && (father_alias != NULL)) {
			if (sig->ret->type != MONO_TYPE_PTR) {
				father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
			} else {
				father_alias->type = MONO_ALIASING_TYPE_ANY;
			}
		}
		
		info->number_of_arguments = 0;
	} else if ((inst->opcode == CEE_ADD) || (inst->opcode == OP_LADD)){
		if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			int variable_index = context.subtree_aliases [0].variable_index;
			//ADD_BAD_ALIAS (info, variable_index);
			father_alias->type = MONO_ALIASING_TYPE_LOCAL_FIELD;
			father_alias->variable_index = variable_index;
		} else if ((context.subtree_aliases [1].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [1].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			int variable_index = context.subtree_aliases [1].variable_index;
			//ADD_BAD_ALIAS (info, variable_index);
			father_alias->type = MONO_ALIASING_TYPE_LOCAL_FIELD;
			father_alias->variable_index = variable_index;
		} else if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_ANY) || (context.subtree_aliases [1].type == MONO_ALIASING_TYPE_ANY)) {
			father_alias->type = MONO_ALIASING_TYPE_ANY;
		} else {
			father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
		}
	} else if ((inst->opcode == OP_MEMCPY) || (inst->opcode == OP_MEMSET)) {
		if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			MonoAliasUsageInformation *use = mono_mempool_alloc (info->mempool, sizeof (MonoAliasUsageInformation));
			
			inst->ssa_op = MONO_SSA_INDIRECT_STORE;
			use->inst = inst;
			use->affected_variables = &(info->variables [context.subtree_aliases [0].variable_index]);
			APPEND_USE (info,bb_info,use);
		} else if (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_ANY) {
			MonoAliasUsageInformation *use = mono_mempool_alloc (info->mempool, sizeof (MonoAliasUsageInformation));
			
			inst->ssa_op = MONO_SSA_INDIRECT_STORE;
			use->inst = inst;
			use->affected_variables = info->temporary_uncontrollably_aliased_variables;
			APPEND_USE (info,bb_info,use);
		}
	} else if ((inst->opcode == OP_UNBOXCAST) || OP_IS_PCONV (inst->opcode) || OP_IS_ICONV (inst->opcode)) {
		father_alias->type = context.subtree_aliases [0].type;
		father_alias->variable_index = context.subtree_aliases [0].variable_index;
	} else if ((inst->opcode == CEE_LDELEMA) || (inst->opcode == OP_COMPARE) || (inst->opcode == OP_SWITCH)) {
		if (father_alias != NULL) {
			father_alias->type = MONO_ALIASING_TYPE_NO_ALIAS;
		}
	} else {
		MonoAliasType father_type = MONO_ALIASING_TYPE_NO_ALIAS;
		MonoLocalVariableList *affected_variables = NULL;
		
		if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [0].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			affected_variables = &(info->variables [context.subtree_aliases [0].variable_index]);
			ADD_BAD_ALIAS (info, context.subtree_aliases [0].variable_index);
		}
		if ((context.subtree_aliases [1].type == MONO_ALIASING_TYPE_LOCAL) || (context.subtree_aliases [1].type == MONO_ALIASING_TYPE_LOCAL_FIELD)) {
			if (affected_variables == NULL) {
				affected_variables = &(info->variables [context.subtree_aliases [1].variable_index]);
			} else if (affected_variables->variable_index != context.subtree_aliases [1].variable_index) {
				int previous_index = affected_variables->variable_index;
				affected_variables = NULL;
				ADD_UNIQUE_VARIABLE (info, affected_variables, previous_index);
				ADD_UNIQUE_VARIABLE (info, affected_variables, context.subtree_aliases [1].variable_index);
			}
			ADD_BAD_ALIAS (info, context.subtree_aliases [1].variable_index);
		}
		
		if (affected_variables != NULL) {
			MonoAliasUsageInformation *use = mono_mempool_alloc (info->mempool, sizeof (MonoAliasUsageInformation));
			
			inst->ssa_op = MONO_SSA_INDIRECT_LOAD_STORE;
			use->inst = inst;
			use->affected_variables = affected_variables;
			APPEND_USE (info, bb_info, use);
		}
		
		if (father_alias != NULL) { 
			if ((context.subtree_aliases [0].type == MONO_ALIASING_TYPE_ANY) || (context.subtree_aliases [1].type == MONO_ALIASING_TYPE_ANY)) {
				father_type = MONO_ALIASING_TYPE_ANY;
			}
			father_alias->type = father_type;
		}
	}
	
	if (FOLLOW_ALIAS_ANALYSIS) {
		print_aliasing_context (&context);
	}
}



/**
 * mono_build_aliasing_information:
 * @cfg: Control Flow Graph
 *
 * Builds the aliasing information in a cfg.
 * After this has run, all MonoInst.ssa_op fields will be properly
 * set (it will use the MONO_SSA_ADDRESS_TAKEN, MONO_SSA_LOAD and
 * MONO_SSA_STORE values as a starting point).
 */
MonoAliasingInformation*
mono_build_aliasing_information (MonoCompile *cfg) {
	MonoMemPool *pool = mono_mempool_new ();
	MonoAliasingInformation *info = mono_mempool_alloc (pool, sizeof (MonoAliasingInformation));
	int i;
#if (DEBUG_ALIAS_ANALYSIS)
	int verbose_level = cfg->verbose_level;
	cfg->verbose_level = 7;
#endif
	
	info->mempool = pool;
	info->cfg = cfg;
	info->bb = mono_mempool_alloc (pool, sizeof (MonoAliasingInformationInBB) * cfg->num_bblocks);
	info->uncontrollably_aliased_variables = NULL;
	info->next_interesting_inst = NULL;
	info->variables = mono_mempool_alloc (pool, sizeof (MonoLocalVariableList) * cfg->num_varinfo);
	info->variable_is_uncontrollably_aliased = mono_mempool_alloc (pool, sizeof (gboolean) * cfg->num_varinfo);
	for (i = 0; i < cfg->num_varinfo; i++) {
		info->variables [i].next = NULL;
		info->variables [i].variable_index = i;
		info->variable_is_uncontrollably_aliased [i] = FALSE;
	}
	info->temporary_uncontrollably_aliased_variables = mono_mempool_alloc (pool, sizeof (MonoLocalVariableList));
	info->temporary_uncontrollably_aliased_variables->next = NULL;
	info->temporary_uncontrollably_aliased_variables->variable_index = NO_VARIABLE_INDEX;
	info->arguments = mono_mempool_alloc (pool, sizeof (MonoInst*) * 16);
	info->arguments_aliases = mono_mempool_alloc (pool, sizeof (MonoAliasValue) * 16);
	info->arguments_capacity = 16;
	info->number_of_arguments = 0;
	
	for (i = 0; i < cfg->num_bblocks; i++) {
		MonoBasicBlock *bb = cfg->bblocks [i];
		MonoAliasingInformationInBB *bb_info = &(info->bb [i]);
		MonoInst *inst;
		
		if (FOLLOW_ALIAS_ANALYSIS) {
			printf ("TRAVERSING BB %d\n", bb->block_num);
		}
		
		bb_info->bb = bb;
		bb_info->potential_alias_uses = NULL;
		info->next_interesting_inst = NULL;
		
		MONO_BB_FOR_EACH_INS (bb, inst) {
			if (FOLLOW_ALIAS_ANALYSIS) {
				printf ("TRAVERSING INST: ");
				mono_print_tree_nl (inst);
			}
			update_aliasing_information_on_inst (info, bb_info, inst, NULL);
		}
		
		g_assert (info->number_of_arguments == 0);
	}
	
	//FIXME
	//#if 0
	for (i = 0; i < cfg->num_bblocks; i++) {
		MonoAliasingInformationInBB *bb_info = &(info->bb [i]);
		MonoAliasUsageInformation *use;
		
		for (use = bb_info->potential_alias_uses; use != NULL; use = use->next) {
			if ((use->affected_variables != NULL) && (use->affected_variables->variable_index == NO_VARIABLE_INDEX)) {
				if (use->affected_variables->next == NULL) {
					use->affected_variables = info->uncontrollably_aliased_variables;
				} else {
					MonoLocalVariableList *last = use->affected_variables;
					while (last->next != NULL) {
						while (last->next && info->variable_is_uncontrollably_aliased [last->next->variable_index]) {
							last->next = last->next->next;
						}
						if (last->next != NULL) {
							last = last->next;
						}
					}
					if (last->variable_index != NO_VARIABLE_INDEX) {
						use->affected_variables = use->affected_variables->next;
						last->next = info->uncontrollably_aliased_variables;
					} else {
						use->affected_variables = info->uncontrollably_aliased_variables;
					}
				}
			}
		}
	}
	//#endif
	
	if (DUMP_ALIAS_ANALYSIS) {
		print_code_with_aliasing_information (info);
	}
	
#if (DEBUG_ALIAS_ANALYSIS)
	cfg->verbose_level = verbose_level;
#endif
	
	return info;
}


void
mono_destroy_aliasing_information (MonoAliasingInformation *info) {
	mono_mempool_destroy (info->mempool);
}

void
mono_aliasing_initialize_code_traversal (MonoAliasingInformation *info, MonoBasicBlock *bb) {
	info->next_interesting_inst = info->bb [bb->dfn].potential_alias_uses;
}

MonoLocalVariableList*
mono_aliasing_get_affected_variables_for_inst_traversing_code (MonoAliasingInformation *info, MonoInst *inst) {
	if ((inst->ssa_op == MONO_SSA_LOAD) || (inst->ssa_op == MONO_SSA_STORE)) {
		return &(info->variables [inst->inst_i0->inst_c0]);
	} else if (info->next_interesting_inst != NULL) {
		if (inst == info->next_interesting_inst->inst) {
			MonoLocalVariableList *result = info->next_interesting_inst->affected_variables;
			info->next_interesting_inst = info->next_interesting_inst->next;
			return result;
		} else if (inst->ssa_op != MONO_SSA_NOP) {
			if (inst->ssa_op == MONO_SSA_ADDRESS_TAKEN) {
				return NULL;
			} else {
				printf ("ERROR: instruction not found '");
				//print_tree_with_aliasing_information (info, inst);
				mono_print_tree (inst);
				printf ("'\n");
				//g_assert_not_reached ();
				return NULL;
			}
		} else {
			return NULL;
		}
	} else {
		return NULL;
	}
}

#if 0
static MonoLocalVariableList*
mono_aliasing_get_affected_variables_for_inst_in_bb (MonoAliasingInformation *info, MonoInst *inst, MonoBasicBlock *bb) {
	MonoAliasUsageInformation *use;
	
	for (use = info->bb [bb->dfn].potential_alias_uses; use != NULL; use = use->next) {
		if (use->inst == inst) {
			return use->affected_variables;
		}
	}
	g_assert_not_reached ();
	return NULL;
}
#endif

MonoLocalVariableList*
mono_aliasing_get_affected_variables_for_inst (MonoAliasingInformation *info, MonoInst *inst) {
	int i;
	
	for (i = 0; i < info->cfg->num_bblocks; i++) {
		MonoAliasingInformationInBB *bb_info = &(info->bb [i]);
		MonoAliasUsageInformation *use;
		
		for (use = info->bb [bb_info->bb->dfn].potential_alias_uses; use != NULL; use = use->next) {
			if (use->inst == inst) {
				return use->affected_variables;
			}
		}
	}
	g_assert_not_reached ();
	return NULL;
}

#if (MONO_APPLY_DEADCE_TO_SINGLE_METHOD)
static char*
mono_deadce_method_name = NULL;
static gboolean check_deadce_method_name (MonoCompile *cfg) {
	gboolean result;
	if (mono_deadce_method_name == NULL) {
		mono_deadce_method_name = getenv ("MONO_DEADCE_METHOD_NAME");
	}
	if (mono_deadce_method_name != NULL) {
		char *method_name = mono_method_full_name (cfg->method, TRUE);
		if (strstr (method_name, mono_deadce_method_name) != NULL) {
			result = TRUE;
		} else {
			result = FALSE;
		}
		g_free (method_name);
	} else {
		result = TRUE;
	}
	return result;
}
#endif



#if (DEBUG_DEADCE)
#define LOG_DEADCE (info->cfg->verbose_level > 0)
#else
#define LOG_DEADCE (info->cfg->verbose_level > 4)
#endif

static gboolean
mono_aliasing_deadce_on_inst (MonoAliasingInformation *info, MonoInst **possibly_dead_assignments, MonoInst *inst) {
	int arity;
	gboolean has_side_effects;
	MonoLocalVariableList *affected_variables;
	
	arity = mono_burg_arity [inst->opcode];
	
	switch (inst->opcode) {
#define OPDEF(a1,a2,a3,a4,a5,a6,a7,a8,a9,a10) case a1:
#include "simple-cee-ops.h"
#undef OPDEF
#define MINI_OP(a1,a2) case a1:
#include "simple-mini-ops.h"
#undef MINI_OP
		has_side_effects = FALSE;
		break;
	default:
		has_side_effects = TRUE;
	}
	
	if (arity) {
		if (mono_aliasing_deadce_on_inst (info, possibly_dead_assignments, inst->inst_i0)) {
			has_side_effects = TRUE;
		}
		if (arity > 1) {
			if (mono_aliasing_deadce_on_inst (info, possibly_dead_assignments, inst->inst_i1)) {
				has_side_effects = TRUE;
			}
			
		}
	}
	
	affected_variables = mono_aliasing_get_affected_variables_for_inst_traversing_code (info, inst);
	
	if (affected_variables != NULL) {
		if (inst->ssa_op & MONO_SSA_LOAD) {
			MonoLocalVariableList *affected_variable;
			for (affected_variable = affected_variables; affected_variable != NULL; affected_variable = affected_variable->next) {
				if (LOG_DEADCE) {
					printf ("CLEARING slot %d at inst ", affected_variable->variable_index);
					mono_print_tree_nl (inst);
				}
				possibly_dead_assignments [affected_variable->variable_index] = NULL;
			}
		}
		if (inst->ssa_op & MONO_SSA_STORE) {
			MonoLocalVariableList *affected_variable;
			for (affected_variable = affected_variables; affected_variable != NULL; affected_variable = affected_variable->next) {
				if (possibly_dead_assignments [affected_variable->variable_index] != NULL) {
					if (LOG_DEADCE) {
						printf ("KILLING slot %d at inst ", affected_variable->variable_index);
						mono_print_tree_nl (inst);
					}
					possibly_dead_assignments [affected_variable->variable_index]->opcode = OP_NOP;
					possibly_dead_assignments [affected_variable->variable_index]->ssa_op = MONO_SSA_NOP;
					possibly_dead_assignments [affected_variable->variable_index] = NULL;
				}
			}
			
			//printf ("FAST DEADCE TOTAL LOCAL\n");
		}
		
	}
	
	if ((! has_side_effects) && (inst->ssa_op == MONO_SSA_STORE)) {
		if (LOG_DEADCE) {
			printf ("FILLING slot %d with inst ", (int)inst->inst_i0->inst_c0);
			mono_print_tree_nl (inst);
		}
		possibly_dead_assignments [inst->inst_i0->inst_c0] = inst;
	}
	
	return has_side_effects;
}


void
mono_aliasing_deadce (MonoAliasingInformation *info) {
	MonoCompile *cfg;
	MonoInst **possibly_dead_assignments;
	int i;
		
	cfg = info->cfg;
	
	possibly_dead_assignments = alloca (cfg->num_varinfo * sizeof (MonoInst*));
	
	if (LOG_DEADCE) {
		mono_print_code (cfg, "BEFORE DEADCE START");
	}
	
#if (MONO_APPLY_DEADCE_TO_SINGLE_METHOD)
	if (! check_deadce_method_name (cfg)) {
		if (LOG_DEADCE) {
			printf ("DEADCE disabled setting MONO_DEADCE_METHOD_NAME\n");
		}
		return;
	}
#endif
	
	for (i = 0; i < cfg->num_bblocks; i++) {
		MonoBasicBlock *bb;
		MonoInst *inst;
		int variable_index;
		
		bb = cfg->bblocks [i];
		memset (possibly_dead_assignments, 0, cfg->num_varinfo * sizeof (MonoInst*));
		mono_aliasing_initialize_code_traversal (info, bb);
		
		if (LOG_DEADCE) {
			printf ("Working on BB %d\n", bb->block_num);
		}
		
		MONO_BB_FOR_EACH_INS (bb, inst) {
			mono_aliasing_deadce_on_inst (info, possibly_dead_assignments, inst);
			if (inst->opcode == OP_JMP) {
				/* Keep arguments live! */
				for (variable_index = 0; variable_index < cfg->num_varinfo; variable_index++) {
					if (cfg->varinfo [variable_index]->opcode == OP_ARG) {
						if (LOG_DEADCE) {
							printf ("FINALLY CLEARING slot %d (JMP), inst was ", variable_index);
							mono_print_tree_nl (possibly_dead_assignments [variable_index]);
						}
						possibly_dead_assignments [variable_index] = NULL;
					}
				}
			}
		}
		
		for (variable_index = 0; variable_index < cfg->num_varinfo; variable_index++) {
			if ((possibly_dead_assignments [variable_index] != NULL) && (! mono_bitset_test (bb->live_out_set, variable_index))) {
				if (LOG_DEADCE) {
					printf ("FINALLY KILLING slot %d, inst was ", variable_index);
					mono_print_tree_nl (possibly_dead_assignments [variable_index]);
				}
				
				//printf ("FAST DEADCE DEAD LOCAL\n");
				
				possibly_dead_assignments [variable_index]->opcode = OP_NOP;
				possibly_dead_assignments [variable_index]->ssa_op = MONO_SSA_NOP;
			}
		}
	}
	
	if (LOG_DEADCE) {
		mono_print_code (cfg, "AFTER DEADCE");
	}
}
