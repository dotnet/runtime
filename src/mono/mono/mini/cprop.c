/*
 * cprop.c: Constant propagation.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

/* dumb list-based implementation for now */

typedef struct _MiniACP MiniACP;

struct _MiniACP {
	MiniACP *next;
	short dreg;
	short sreg;
	int type;
};

static int
copy_value (MiniACP *acp, int reg, int type)
{
	MiniACP *tmp = acp;

	//g_print ("search reg %d '%c'\n", reg, type);
	while (tmp) {
	//	g_print ("considering dreg %d, sreg %d '%c'\n", tmp->dreg, tmp->sreg, tmp->type);
		if (tmp->type == type && tmp->dreg == reg) {
	//		g_print ("copy prop from %d to %d\n", tmp->sreg, tmp->dreg);
			return tmp->sreg;
		}
		tmp = tmp->next;
	}
	return reg;
}

static MiniACP*
remove_acp (MiniACP *acp, int reg, int type)
{
	MiniACP *tmp = acp;
	MiniACP *prev = NULL;

	while (tmp) {
		if (tmp->type == type && (tmp->dreg == reg || tmp->sreg == reg)) {
			if (prev)
				prev->next = tmp->next;
			else
				acp = tmp->next;
		} else {
			prev = tmp;
		}
		tmp = tmp->next;
	}
	return acp;
}

static MiniACP*
add_acp (MonoCompile *cfg, MiniACP *acp, int sreg, int dreg, int type)
{
	MiniACP *newacp = mono_mempool_alloc (cfg->mempool, sizeof (MiniACP));;
	newacp->type = type;
	newacp->sreg = sreg;
	newacp->dreg = dreg;

	newacp->next = acp;
	return newacp;
}

static void
local_copy_prop (MonoCompile *cfg, MonoInst *code)
{
	MiniACP *acp = NULL;
	const char *spec;
	MonoInst *ins = code;

	//g_print ("starting BB\n");

	while (ins) {
		spec = ins_get_spec (ins->opcode);
		//print_ins (0, ins);

		if (spec [MONO_INST_CLOB] != 's' && spec [MONO_INST_CLOB] != '1' && spec [MONO_INST_CLOB] != 'd' && spec [MONO_INST_CLOB] != 'a' && spec [MONO_INST_CLOB] != 'c') {
			if (spec [MONO_INST_SRC1] == 'f') {
				ins->sreg1 = copy_value (acp, ins->sreg1, 'f');
			} else if (spec [MONO_INST_SRC1]) {
				ins->sreg1 = copy_value (acp, ins->sreg1, 'i');
			}
		}

		if (spec [MONO_INST_CLOB] != 's') {
			if (spec [MONO_INST_SRC2] == 'f') {
				ins->sreg2 = copy_value (acp, ins->sreg2, 'f');
			} else if (spec [MONO_INST_SRC2]) {
				ins->sreg2 = copy_value (acp, ins->sreg2, 'i');
			}
		}

		if (mono_inst_get_src_registers (ins, NULL) > 2)
			NOT_IMPLEMENTED;

		/* invalidate pairs */
		if (spec [MONO_INST_DEST] == 'f') {
			acp = remove_acp (acp, ins->dreg, 'f');
		} else if (spec [MONO_INST_DEST]) {
			acp = remove_acp (acp, ins->dreg, 'i');
		}

		/* insert pairs */
		/*
		 * Later copy-propagate also immediate values and memory stores.
		 */
		if (ins->opcode == OP_MOVE && ins->sreg1 != ins->dreg) {
	//		g_print ("added acp of %d <- %d '%c'\n", ins->dreg, ins->sreg1, spec [MONO_INST_SRC1]);
			acp = add_acp (cfg, acp, ins->sreg1, ins->dreg, spec [MONO_INST_SRC1]);
		}

		if (spec [MONO_INST_CLOB] == 'c') {
			/* this is a call, invalidate all the pairs */
			acp = NULL;
		} else if ((ins->opcode) == OP_BR || (ins->opcode >= CEE_BEQ && ins->opcode <= CEE_BLT) ||
				(ins->opcode >= CEE_BNE_UN && ins->opcode <= CEE_BLT_UN)) {
			acp = NULL; /* invalidate all pairs */
			/* it's not enough to invalidate the pairs, because we don't always
			 * generate extended basic blocks (the BRANCH_LABEL stuff in the burg rules)
			 * This issue is going to reduce a lot the possibilities for optimization!
			 */
			return;
		}
		ins = ins->next;
	}
}

