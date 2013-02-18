/*
 * graph.c: Helper routines to graph various internal states of the code generator
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */

#include <config.h>

#ifndef DISABLE_JIT

#include <string.h>
#include <mono/metadata/debug-helpers.h>

#include "mini.h"

static char *
convert_name (const char *str)
{
	int i, j, len = strlen (str);
	char *res = g_malloc (len * 2);

	j = 0;
	for (i = 0; i < len; i++) {
		char c = str [i];

		switch (c) {
		case '.':
			res [j++] = '_';
			break;
		default:
			res [j++] = c;
		}
	}

	res [j] = 0;

	return res;
}

static void
dtree_emit_one_loop_level (MonoCompile *cfg, FILE *fp, MonoBasicBlock *h)
{
	MonoBasicBlock *bb;
	int i, level = 0;

	if (h) {
		level = h->nesting;
		fprintf (fp, "subgraph cluster_%d {\n", h->block_num);
		fprintf (fp, "label=\"loop_%d\"\n", h->block_num);
	} 

	for (i = 1; i < cfg->num_bblocks; ++i) {
		bb = cfg->bblocks [i];

		if (!h || (g_list_find (h->loop_blocks, bb) && bb != h)) {
			if (bb->nesting == level) {
				fprintf (fp, "BB%d -> BB%d;\n", bb->idom->block_num, bb->block_num);
			}
		
			if (bb->nesting == (level + 1) && bb->loop_blocks) {
				fprintf (fp, "BB%d -> BB%d;\n", bb->idom->block_num, bb->block_num);
				dtree_emit_one_loop_level (cfg, fp, bb);
			}
		}
	}
	
	if (h) {
		fprintf (fp, "}\n");
	}
}

static void
cfg_emit_one_loop_level (MonoCompile *cfg, FILE *fp, MonoBasicBlock *h)
{
	MonoBasicBlock *bb;
	int j, level = 0;

	if (h) {
		level = h->nesting;
		fprintf (fp, "subgraph cluster_%d {\n", h->block_num);
		fprintf (fp, "label=\"loop_%d\"\n", h->block_num);
	} 

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		if (bb->region != -1) {
			switch (bb->region & (MONO_REGION_FINALLY|MONO_REGION_CATCH|MONO_REGION_FAULT|MONO_REGION_FILTER)) {
			case MONO_REGION_CATCH:
				fprintf (fp, "BB%d [color=blue];\n", bb->block_num);;
				break;
			case MONO_REGION_FINALLY:
				fprintf (fp, "BB%d [color=green];\n", bb->block_num);;
				break;
			case MONO_REGION_FAULT:
			case MONO_REGION_FILTER:
				fprintf (fp, "BB%d [color=yellow];\n", bb->block_num);;
				break;
			default:
				break;
			}
		}

		if (!h || (g_list_find (h->loop_blocks, bb) && bb != h)) {

			if (bb->nesting == level) {
				for (j = 0; j < bb->in_count; j++) 
					fprintf (fp, "BB%d -> BB%d;\n", bb->in_bb [j]->block_num, bb->block_num);
			}
		
			if (bb->nesting == (level + 1) && bb->loop_blocks) {
				for (j = 0; j < bb->in_count; j++) 
					fprintf (fp, "BB%d -> BB%d;\n", bb->in_bb [j]->block_num, bb->block_num);
				cfg_emit_one_loop_level (cfg, fp, bb);
			}
		}
	}
	
	if (h) {
		fprintf (fp, "}\n");
	}
}

static void
mono_draw_dtree (MonoCompile *cfg, FILE *fp)
{
	g_assert ((cfg->comp_done & MONO_COMP_IDOM));

	fprintf (fp, "digraph %s {\n", convert_name (cfg->method->name));
	fprintf (fp, "node [fontsize=12.0]\nedge [len=1,color=red]\n");
	fprintf (fp, "label=\"Dominator tree for %s\";\n", mono_method_full_name (cfg->method, TRUE));

	fprintf (fp, "BB0 [shape=doublecircle];\n");
	fprintf (fp, "BB1 [color=red];\n");

	dtree_emit_one_loop_level (cfg, fp, NULL);

	fprintf (fp, "}\n");
}

static void
mono_draw_cfg (MonoCompile *cfg, FILE *fp)
{
	fprintf (fp, "digraph %s {\n", convert_name (cfg->method->name));
	fprintf (fp, "node [fontsize=12.0]\nedge [len=1,color=red]\n");
	fprintf (fp, "label=\"CFG for %s\";\n", mono_method_full_name (cfg->method, TRUE));

	fprintf (fp, "BB0 [shape=doublecircle];\n");
	fprintf (fp, "BB1 [color=red];\n");

	cfg_emit_one_loop_level (cfg, fp, NULL);

	fprintf (fp, "}\n");
}

#if 0

static void
mono_print_label (FILE *fp, MonoInst *tree) {
	int arity;

	if (!tree)
		return;

	arity = mono_burg_arity [tree->opcode];

	fprintf (fp, "\\ %s%s", arity? "(": "",  mono_inst_name (tree->opcode));

	switch (tree->opcode) {
	case OP_ICONST:
		fprintf (fp, "[%ld]", (long)tree->inst_c0);
		break;
	case OP_I8CONST:
		fprintf (fp, "[%lld]", (long long)tree->inst_l);
		break;
	case OP_R8CONST:
		fprintf (fp, "[%f]", *(double*)tree->inst_p0);
		break;
	case OP_R4CONST:
		fprintf (fp, "[%f]", *(float*)tree->inst_p0);
		break;
	case OP_ARG:
	case OP_LOCAL:
		fprintf (fp, "[%d]", (int)tree->inst_c0);
		break;
	case OP_REGOFFSET:
		fprintf (fp, "[0x%x(%s)]", (int)tree->inst_offset, mono_arch_regname (tree->inst_basereg));
		break;
	case OP_REGVAR:
		fprintf (fp, "[%s]", mono_arch_regname (tree->dreg));
		break;
	case CEE_NEWARR:
		fprintf (fp, "[%s]",  tree->inst_newa_class->name);
		mono_print_label (fp, tree->inst_newa_len);
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
	case OP_VOIDCALLVIRT: {
		MonoCallInst *call = (MonoCallInst*)tree;
		if (call->method) {
			if (mono_method_signature (call->method)->hasthis && tree->inst_left) {
				mono_print_label (fp, tree->inst_left);
			}
			fprintf (fp, "[%s]", call->method->name);
		}
		break;
	}
	case OP_PHI: {
		int i;
		fprintf (fp, "[%d\\ (", (int)tree->inst_c0);
		for (i = 0; i < tree->inst_phi_args [0]; i++) {
			if (i)
				fprintf (fp, ",\\ ");
			fprintf (fp, "%d", tree->inst_phi_args [i + 1]);
		}
		fprintf (fp, ")]");
		break;
	}
	case OP_NOP:
	case OP_JMP:
	case OP_BREAK:
		break;
	case OP_BR:
		fprintf (fp, "[B%d]", tree->inst_target_bb->block_num);
		break;
	case OP_SWITCH:
	case CEE_ISINST:
	case CEE_CASTCLASS:
	case OP_CALL_REG:
	case OP_FCALL_REG:
	case OP_LCALL_REG:
	case OP_VCALL_REG:
	case OP_VOIDCALL_REG:
		mono_print_label (fp, tree->inst_left);
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
		fprintf (fp, "[B%dB%d]", tree->inst_true_bb->block_num, tree->inst_false_bb->block_num);
		mono_print_label (fp, tree->inst_left);
		break;
	default:
		if (arity) {
			mono_print_label (fp, tree->inst_left);
			if (arity > 1)
				mono_print_label (fp, tree->inst_right);
		}
		break;
	}

	if (arity)
		fprintf (fp, ")");
}

#endif

static void
mono_draw_code_cfg (MonoCompile *cfg, FILE *fp)
{
	MonoBasicBlock *bb;
		
	fprintf (fp, "digraph %s {\n", convert_name (cfg->method->name));
	fprintf (fp, "node [fontsize=12.0]\nedge [len=1,color=red]\n");
	fprintf (fp, "label=\"CFG for %s\";\n", mono_method_full_name (cfg->method, TRUE));

	fprintf (fp, "BB0 [shape=doublecircle];\n");
	fprintf (fp, "BB1 [color=red];\n");

	for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {
		MonoInst *inst;
		const char *color;

		if (bb == cfg->bb_exit)
			continue;

		if ((cfg->comp_done & MONO_COMP_REACHABILITY) && (bb->flags & BB_REACHABLE))
			color = "color=red,";
		else
			color = "";

		fprintf (fp, "BB%d [%sshape=record,labeljust=l,label=\"{BB%d|", bb->block_num, color, bb->block_num);
			
		MONO_BB_FOR_EACH_INS (bb, inst) {
			//mono_print_label (fp, inst);
			fprintf (fp, "\\n");			
		}

		fprintf (fp, "}\"];\n");			
	}

	cfg_emit_one_loop_level (cfg, fp, NULL);

	fprintf (fp, "}\n");
}

void
mono_draw_graph (MonoCompile *cfg, MonoGraphOptions draw_options)
{
	char *com;
	const char *fn;
	FILE *fp;

	fn = "/tmp/minidtree.graph";
	fp = fopen (fn, "w+");
	g_assert (fp);

	switch (draw_options) {
	case MONO_GRAPH_DTREE:
		mono_draw_dtree (cfg, fp);
		break;
	case MONO_GRAPH_CFG:
		mono_draw_cfg (cfg, fp);
		break;
	case MONO_GRAPH_CFG_CODE:
	case MONO_GRAPH_CFG_OPTCODE:
	case MONO_GRAPH_CFG_SSA:
		mono_draw_code_cfg (cfg, fp);
		break;
	}

	fclose (fp);

	//com = g_strdup_printf ("dot %s -Tpng -o %s.png; eog %s.png", fn, fn, fn);
	com = g_strdup_printf ("dot %s -Tps -o %s.ps;gv %s.ps", fn, fn, fn);
	system (com);
	g_free (com);
}

#endif /* DISABLE_JIT */

