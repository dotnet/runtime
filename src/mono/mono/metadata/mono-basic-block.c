/**
 * \file
 * Routines for parsing basic blocks from the IL stream
 *
 * Authors:
 *   Rodrigo Kumpera (rkumpera@novell.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>

#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/mono-basic-block.h>
#include <mono/metadata/opcodes.h>

#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-compiler.h>

#define CHECK_ADD4_OVERFLOW_UN(a, b) ((guint32)(0xFFFFFFFFU) - (guint32)(b) < (guint32)(a))
#define CHECK_ADD8_OVERFLOW_UN(a, b) ((guint64)(0xFFFFFFFFFFFFFFFFUL) - (guint64)(b) < (guint64)(a))

#if SIZEOF_VOID_P == 4
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD4_OVERFLOW_UN(a, b)
#else
#define CHECK_ADDP_OVERFLOW_UN(a,b) CHECK_ADD8_OVERFLOW_UN(a, b)
#endif

#define ADDP_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADDP_OVERFLOW_UN (a, b))
#define ADD_IS_GREATER_OR_OVF(a, b, c) (((a) + (b) > (c)) || CHECK_ADD4_OVERFLOW_UN (a, b))

#define DEBUG_BB 0

enum {
	RED,
	BLACK
};

#if DEBUG_BB

static void
dump_bb_list (MonoSimpleBasicBlock *bb, MonoSimpleBasicBlock **root, const char *msg)
{
	printf ("------- %s --- root is %x ---\n", msg, (*root)->start);
	while (bb) {
		GSList *tmp;
		printf ("BB start %x end %x left ", bb->start, bb->end);
		if (bb->left)
			printf ("%x", bb->left->start);
		else
			printf ("NIL");

		printf (" right ");
		if (bb->right)
			printf ("%x", bb->right->start);
		else
			printf ("NIL");

		printf (" parent ");
		if (bb->parent)
			printf ("%x", bb->parent->start);
		else
			printf ("NIL");

		printf(" color %s out [", bb->colour == RED ? "red" : "black");

		for (tmp = bb->out_bb; tmp; tmp = tmp->next) {
			MonoSimpleBasicBlock *to = tmp->data;
			printf ("%x ", to->start);
		}
		printf ("] %s\n", bb->dead ? "dead" : "alive");
		bb = bb->next;
	}
}

#endif

static void
bb_unlink (MonoSimpleBasicBlock *from, MonoSimpleBasicBlock *to)
{
	if (from->out_bb)
		from->out_bb = g_slist_remove (from->out_bb, to);
}

static void
bb_link (MonoSimpleBasicBlock *from, MonoSimpleBasicBlock *to)
{
	if (g_slist_find (from->out_bb, to))
		return;
	from->out_bb = g_slist_prepend (from->out_bb, to);
}


static MonoSimpleBasicBlock*
bb_grandparent (MonoSimpleBasicBlock *bb)
{
	return bb && bb->parent ? bb->parent->parent : NULL;
}

static MonoSimpleBasicBlock*
bb_uncle (MonoSimpleBasicBlock *bb)
{
	MonoSimpleBasicBlock *gp = bb_grandparent (bb);
	if (gp == NULL)
		return NULL;
	if (bb->parent == gp->left)
		return gp->right;
	return gp->left;
}

static void
change_node (MonoSimpleBasicBlock *from, MonoSimpleBasicBlock *to, MonoSimpleBasicBlock **root)
{
	MonoSimpleBasicBlock *parent = from->parent;
	if (parent) {
		if (parent->left == from)
			parent->left = to;
		else
			parent->right = to;
	} else {
		*root = to;
	}
	to->parent = parent;
}

static void
rotate_right (MonoSimpleBasicBlock *parent, MonoSimpleBasicBlock **root)
{
	MonoSimpleBasicBlock *bb = parent->left;
	if (bb->right) {
		parent->left = bb->right;
		parent->left->parent = parent;
	} else
		parent->left = NULL;
	bb->right = parent;
	change_node (parent, bb, root);
	parent->parent = bb;
}

static void
rotate_left (MonoSimpleBasicBlock *bb, MonoSimpleBasicBlock **root)
{
	MonoSimpleBasicBlock *other = bb->right;
	if (other->left) {
		bb->right = other->left;
		bb->right->parent = bb;
	} else
		bb->right = NULL;
	other->left = bb;
	change_node (bb, other, root);
	bb->parent = other;
}

/* School book implementation of a RB tree with insert then fix (which requires a parent pointer)
 * TODO implement Sedgewick's version that doesn't require parent pointers
 */
static void
bb_insert (MonoSimpleBasicBlock *first, MonoSimpleBasicBlock *bb, MonoSimpleBasicBlock **root)
{
	MonoSimpleBasicBlock *parent, *uncle, *grandparent;
	int bb_start = bb->start;

	parent = *root;
	do {
		if (bb_start < parent->start) {
			if (parent->left == NULL) {
				parent->left = bb;
				break;
			}
			parent = parent->left;
		} else {
			if (parent->right == NULL) {
				parent->right = bb;
				break;
			}
			parent = parent->right;
		}
	} while (parent);
	g_assert (parent);
	bb->parent = parent;

	bb->colour = RED;

	do {
		if (bb->parent == NULL) {
			bb->colour = BLACK;
			break;
		}

		if (bb->parent->colour == BLACK)
			break;

		uncle = bb_uncle (bb);
		if (uncle && uncle->colour == RED) {
			grandparent = bb_grandparent (bb);

			bb->parent->colour = BLACK;
			uncle->colour = BLACK;
			grandparent->colour = RED;
			bb = grandparent;
			continue;
		}

		grandparent = bb_grandparent (bb);
		if ((bb == bb->parent->right) && (bb->parent == grandparent->left)) {
			rotate_left (bb->parent, root);
			bb = bb->left;
		} else if ((bb == bb->parent->left) && (bb->parent == grandparent->right)) {
			rotate_right (bb->parent, root);
			bb = bb->right;
		}

		grandparent = bb_grandparent (bb);
		bb->parent->colour = BLACK;
		grandparent->colour = RED;
		if ((bb == bb->parent->left) && (bb->parent == grandparent->left))
			rotate_right (grandparent, root);
		else
			rotate_left (grandparent, root);
		break;
	} while (TRUE);
}

static gboolean
bb_idx_is_contained (MonoSimpleBasicBlock *bb, int target)
{
	return bb->start <= target && target < bb->end;
}

/*
 * Split the basic blocks from @first at @target.
 * @hint is a guess of a very close to the target basic block. It is probed before the RB tree as it's often possible
 * to provide a near to exact guess (next block splits, switch branch targets, etc)
 *
 */
static MonoSimpleBasicBlock*
bb_split (MonoSimpleBasicBlock *first, MonoSimpleBasicBlock *hint, MonoSimpleBasicBlock **root, guint target, gboolean link_blocks, MonoMethod *method, MonoError *error)
{
	MonoSimpleBasicBlock *res, *bb = first;

	error_init (error);

	if (bb_idx_is_contained (hint, target)) {
		first = hint;
	} else if (hint->next && bb_idx_is_contained (hint->next, target)) {
		first = hint->next;
	} else {
		first = *root;
		do {
			if (bb_idx_is_contained (first, target))
				break;
			if (first->start > target)
				first = first->left;
			else
				first = first->right;
		} while (first);
	}

	if (first == NULL) {
		mono_error_set_not_verifiable (error, method, "Invalid instruction target %x", target);
		return NULL;
	}

	if (first->start == target)
		return first;

	res = g_new0 (MonoSimpleBasicBlock, 1);
	res->start = target;
	res->end = first->end;
	res->next = first->next;
	res->out_bb = first->out_bb;
	res->dead = TRUE;

	first->end = res->start;
	first->next = res;
	first->out_bb = NULL;

	if (link_blocks)
		bb_link (first, res);
	bb_insert (bb, res, root);

	return res;
}

static void
bb_liveness (MonoSimpleBasicBlock *bb)
{
	GPtrArray* mark_stack = g_ptr_array_new ();
	GSList *tmp;

	/*All function entry points (prologue, EH handler/filter) are already marked*/
	while (bb) {
		if (!bb->dead)
			g_ptr_array_add (mark_stack, bb);
		bb = bb->next;
	}

	while (mark_stack->len > 0) {
		MonoSimpleBasicBlock *block = (MonoSimpleBasicBlock *)g_ptr_array_remove_index_fast (mark_stack, mark_stack->len - 1);
		block->dead = FALSE;

		for (tmp = block->out_bb; tmp; tmp = tmp->next) {
			MonoSimpleBasicBlock *to = (MonoSimpleBasicBlock *)tmp->data;
			if (to->dead)
				g_ptr_array_add (mark_stack, to);
		}
	}

	g_ptr_array_free (mark_stack, TRUE);
}

/*This doesn't returns endfilter because it's not useful to split at its boundary.
Endfilter must be the last instruction of a filter clause and MS enforces this, so we can't have
dead code after it.
*/
static gboolean
mono_opcode_has_static_branch (int opcode)
{
	switch (opcode) {
	case MONO_CEE_RET:
	case MONO_CEE_THROW:
	case MONO_CEE_RETHROW:
	case MONO_CEE_ENDFINALLY:
	case MONO_CEE_MONO_RETHROW:
		return TRUE;
	}
	return FALSE;
}


static void
bb_formation_il_pass (const unsigned char *start, const unsigned char *end, MonoSimpleBasicBlock *bb, MonoSimpleBasicBlock **root, MonoMethod *method, MonoError *error)
{
	unsigned const char *ip = start;
	MonoOpcodeEnum value;
	int size;
	guint cli_addr, offset;
	MonoSimpleBasicBlock *branch, *next, *current;
	const MonoOpcode *opcode;

	error_init (error);

	current = bb;

	while (ip < end) {
		cli_addr = ip - start;
		size = mono_opcode_value_and_size (&ip, end, &value);
		if (size < 0) {
			mono_error_set_not_verifiable (error, method, "Invalid instruction %x", *ip);
			return;
		}

		while (current && cli_addr >= current->end)
			current = current->next;
		g_assert (current);

		opcode = &mono_opcodes [value];
		switch (opcode->argument) {
		case MonoInlineNone:
			ip++;
			if (!mono_opcode_has_static_branch (value) || ip >= end)
				break;
			if (!(next = bb_split (bb, current, root, ip - start, FALSE, method, error)))
				return;

			bb_unlink (current, next);
			current = next;
			break;
		case MonoInlineString:
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;

		case MonoInlineMethod:
			ip += 5;
			if (value != MONO_CEE_JMP || ip >= end)
				break;
			if (!(next = bb_split (bb, current, root, ip - start, FALSE, method, error)))
				return;

			bb_unlink (current, next);
			current = next;

			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		case MonoShortInlineBrTarget:
		case MonoInlineBrTarget:
			if (opcode->argument == MonoShortInlineBrTarget) {
				offset = cli_addr + 2 + (signed char)ip [1];
				ip += 2;
			} else {
				offset = cli_addr + 5 + (gint32)read32 (ip + 1);
				ip += 5;
			}
			
			branch = bb_split (bb, current, root, offset, TRUE, method, error);
			if (!branch)
				return;

			/*If we splitted the current BB*/
			if (offset < cli_addr && branch->start > current->start)
				current = branch;
			if (ip < end) {
				next = bb_split (bb, current, root, ip - start, opcode->flow_type != MONO_FLOW_BRANCH, method, error);
				if (!next)
					return;
			} else {
				next = NULL;
			}

			bb_link (current, branch);
			if (next && opcode->flow_type == MONO_FLOW_BRANCH && next != branch) {
				bb_unlink (current, next);
				current = next;
			}
			break;
		case MonoInlineSwitch: {
			MonoSimpleBasicBlock *tmp;
			guint32 j, n = read32 (ip + 1);

			ip += 5;
			offset = cli_addr + 5 + 4 * n;
			if (!(next = bb_split (bb, current, root, offset, TRUE, method, error)))
				return;

			bb_link (current, next);
			tmp = next;			

			for (j = 0; j < n; ++j) {
				if (ip >= end) {
					mono_error_set_not_verifiable (error, method, "Invalid switch instruction %x", cli_addr);
					return;
				}
				if (!(next = bb_split (bb, next, root, offset + (gint32)read32 (ip), TRUE, method, error)))
					return;
				bb_link (current, next);
				ip += 4;
			}
			current = tmp;
			break;
		}
		default:
			mono_error_set_not_verifiable (error, method, "Invalid instruction %x", *ip);
			return;
		}
	}
	if (ip != end)
		mono_error_set_not_verifiable (error, method, "Invalid last instruction");
}

static void
bb_formation_eh_pass (MonoMethodHeader *header, MonoSimpleBasicBlock *bb, MonoSimpleBasicBlock **root, MonoMethod *method, MonoError *error)
{
	int i;
	int end = header->code_size;

	error_init (error);

	/*We must split at all points to verify for targets in the middle of an instruction*/
	for (i = 0; i < header->num_clauses; ++i) {
		MonoExceptionClause *clause = header->clauses + i;
		MonoSimpleBasicBlock *try_block, *handler;

		if (!(try_block = bb_split (bb, bb, root, clause->try_offset, TRUE, method, error)))
			return;

		handler = bb_split (bb, try_block, root, clause->handler_offset, FALSE, method, error);
		if (!handler)
			return;
		handler->dead = FALSE;

		if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
			MonoSimpleBasicBlock *filter = bb_split (bb, try_block, root, clause->data.filter_offset, FALSE, method, error);
			if (!filter)
				return;
			filter->dead = FALSE;
		}

		if (clause->try_offset + clause->try_len < end && !bb_split (bb, try_block, root, clause->try_offset + clause->try_len, FALSE, method, error))
			return;

		if (clause->handler_offset + clause->handler_len < end && !bb_split (bb, handler, root, clause->handler_offset + clause->handler_len, FALSE, method, error))
			return;
	}
}

/*
 * mono_basic_block_free:
 *
 * Release the memory associated with the list of basis blocks @bb.
*/
void
mono_basic_block_free (MonoSimpleBasicBlock *bb)
{
	while (bb) {
		MonoSimpleBasicBlock *next = bb->next;
		if (bb->out_bb)
			g_slist_free (bb->out_bb);
		g_free (bb);
		bb = next;
	}
}

/*
 * mono_basic_block_split:
 *
 * Return the list of basic blocks of method. Return NULL on failure and set @error.
*/
MonoSimpleBasicBlock*
mono_basic_block_split (MonoMethod *method, MonoError *error, MonoMethodHeader *header)
{
	MonoSimpleBasicBlock *bb, *root;
	const unsigned char *start, *end;

	error_init (error);

	start = header->code;
	end = start + header->code_size;

	bb = g_new0 (MonoSimpleBasicBlock, 1);
	bb->start = 0;
	bb->end = end - start;
	bb->colour = BLACK;
	bb->dead = FALSE;

	root = bb;
	bb_formation_il_pass (start, end, bb, &root, method, error);
	if (!is_ok (error))
		goto fail;
	
	bb_formation_eh_pass (header, bb, &root, method, error);
	if (!is_ok (error))
		goto fail;

	bb_liveness (bb);

#if DEBUG_BB
	dump_bb_list (bb, &root, g_strdup_printf("AFTER LIVENESS %s", mono_method_full_name (method, TRUE)));
#endif

	return bb;

fail:
	mono_basic_block_free (bb);
	return NULL;
}

/*
 * mono_opcode_value_and_size:
 *
 * Returns the size of the opcode starting at *@ip, or -1 on error.
 * Value is the opcode number. 
*/
int
mono_opcode_value_and_size (const unsigned char **ip, const unsigned char *end, MonoOpcodeEnum *value)
{
	const unsigned char *start = *ip, *p;
	int i = *value = mono_opcode_value (ip, end);
	int size = 0; 
	if (i < 0 || i >= MONO_CEE_LAST)
		return -1;
	p = *ip;

	switch (mono_opcodes [i].argument) {
	case MonoInlineNone:
		size = 1;
		break;
	case MonoInlineString:
	case MonoInlineType:
	case MonoInlineField:
	case MonoInlineMethod:
	case MonoInlineTok:
	case MonoInlineSig:
	case MonoShortInlineR:
	case MonoInlineI:
	case MonoInlineBrTarget:
		size = 5;
		break;
	case MonoInlineVar:
		size = 3;
		break;
	case MonoShortInlineVar:
	case MonoShortInlineI:
	case MonoShortInlineBrTarget:
		size = 2;
		break;
	case MonoInlineR:
	case MonoInlineI8:
		size = 9;
		break;
	case MonoInlineSwitch: {
		guint32 entries;
		if (ADDP_IS_GREATER_OR_OVF (p, 5, end))
			return -1;
		entries = read32 (p + 1);
		if (entries >= (0xFFFFFFFFU / 4))
			return -1;
		size = 5 + 4 * entries;
		break;
	}
	default:
		g_error ("Invalid opcode %d argument %d max opcode %d\n", i, mono_opcodes [i].argument, MONO_CEE_LAST);
	}

	if (ADDP_IS_GREATER_OR_OVF (p, size, end))
		return -1;

	return (p - start) + size;
}

/*
 * mono_opcode_size:
 *
 * Returns the size of the opcode starting at @ip, or -1 on error.
*/
int
mono_opcode_size (const unsigned char *ip, const unsigned char *end)
{
	MonoOpcodeEnum tmp;
	return mono_opcode_value_and_size (&ip, end, &tmp);
}

