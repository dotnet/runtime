/*
 * helpers.c: architecture independent helper functions
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#if HAVE_BOEHM_GC
#include <gc/gc.h>
#endif
#include <glib.h>

#include "codegen.h"
#include "helpers.h"

int
mono_map_store_svt_type (int svt)
{
	switch (svt) {
	case VAL_I32:
		return MB_TERM_STIND_I4;
	case VAL_POINTER:
		return MB_TERM_STIND_REF;
	case VAL_I64:
		return MB_TERM_STIND_I8;
	case VAL_DOUBLE:
		return MB_TERM_STIND_R8;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

void
mono_get_val_sizes (MonoValueType type, int *size, int *align) 
{ 
	switch (type) {
	case VAL_I32:
		*size = *align = sizeof (gint32);
		break;
	case VAL_I64:
		*size = *align = sizeof (gint64);
		break;
	case VAL_POINTER:
		*size = *align = sizeof (gpointer);
		break;
	case VAL_DOUBLE:
		*size = *align = sizeof (double);
		break;
	default:
		g_assert_not_reached ();
	}
}

/**
 * mono_map_stind_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding store opcode 
 * for the code generator.
 */
int
mono_map_stind_type (MonoType *type)
{
	if (type->byref) 
		return MB_TERM_STIND_REF;

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return MB_TERM_STIND_I1;	
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return MB_TERM_STIND_I2;	
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return MB_TERM_STIND_I4;	
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return MB_TERM_STIND_REF;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		return MB_TERM_STIND_I8;
	case MONO_TYPE_R4:
		return MB_TERM_STIND_R4;
	case MONO_TYPE_R8:
		return MB_TERM_STIND_R8;
	case MONO_TYPE_VALUETYPE: 
		if (type->data.klass->enumtype)
			return mono_map_stind_type (type->data.klass->enum_basetype);
		else
			return MB_TERM_STIND_OBJ;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

/**
 * mono_map_remote_stind_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding remote store opcode 
 * for the code generator.
 */
int
mono_map_remote_stind_type (MonoType *type)
{
	if (type->byref) {
		return MB_TERM_REMOTE_STIND_REF;
	}

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return MB_TERM_REMOTE_STIND_I1;	
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return MB_TERM_REMOTE_STIND_I2;	
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return MB_TERM_REMOTE_STIND_I4;	
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return MB_TERM_REMOTE_STIND_REF;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_REMOTE_STIND_I8;
	case MONO_TYPE_R4:
		return MB_TERM_REMOTE_STIND_R4;
	case MONO_TYPE_R8:
		return MB_TERM_REMOTE_STIND_R8;
	case MONO_TYPE_VALUETYPE: 
		if (type->data.klass->enumtype)
			return mono_map_remote_stind_type (type->data.klass->enum_basetype);
		else
			return MB_TERM_REMOTE_STIND_OBJ;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

int
mono_map_starg_type (MonoType *type)
{
	if (type->byref) 
		return MB_TERM_STIND_REF;

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return MB_TERM_STIND_I4;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return MB_TERM_STIND_REF;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_STIND_I8;
	case MONO_TYPE_R4:
		return MB_TERM_STIND_R4;
	case MONO_TYPE_R8:
		return MB_TERM_STIND_R8;
	case MONO_TYPE_VALUETYPE: 
		if (type->data.klass->enumtype)
			return mono_map_starg_type (type->data.klass->enum_basetype);
		else
			return MB_TERM_STIND_OBJ;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

int
mono_map_arg_type (MonoType *type)
{
	if (type->byref) 
		return MB_TERM_ARG_I4;

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return MB_TERM_ARG_I4;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
	case MONO_TYPE_STRING:
#if SIZEOF_VOID_P == 8
		return MB_TERM_ARG_I8;
#else
		return MB_TERM_ARG_I4;
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		return MB_TERM_ARG_I8;
	case MONO_TYPE_R4:
		return MB_TERM_ARG_R4;
	case MONO_TYPE_R8:
		return MB_TERM_ARG_R8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype)
			return mono_map_arg_type (type->data.klass->enum_basetype);
		else
			return MB_TERM_ARG_OBJ;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

/**
 * mono_map_ldind_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding load opcode 
 * for the code generator.
 */
int
mono_map_ldind_type (MonoType *type, MonoValueType *svt)
{
	if (type->byref) {
		*svt = VAL_POINTER;
		return MB_TERM_LDIND_REF;
	}

	switch (type->type) {
	case MONO_TYPE_I1:
		*svt = VAL_I32;
		return MB_TERM_LDIND_I1;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		*svt = VAL_I32;
		return MB_TERM_LDIND_U1;
	case MONO_TYPE_I2:
		*svt = VAL_I32;
		return MB_TERM_LDIND_I2;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		*svt = VAL_I32;
		return MB_TERM_LDIND_U2;
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_I4:
		*svt = VAL_I32;
		return MB_TERM_LDIND_I4;
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_U4:
		*svt = VAL_I32;
		return MB_TERM_LDIND_U4;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		*svt = VAL_POINTER;
		return MB_TERM_LDIND_REF;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*svt = VAL_I64;
		return MB_TERM_LDIND_I8;
	case MONO_TYPE_R4:
		*svt = VAL_DOUBLE;
		return MB_TERM_LDIND_R4;
	case MONO_TYPE_R8:
		*svt = VAL_DOUBLE;
		return MB_TERM_LDIND_R8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return mono_map_ldind_type (type->data.klass->enum_basetype, svt);
		} else {
			*svt = VAL_UNKNOWN;
			return MB_TERM_LDIND_OBJ;
		}
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

int
mono_map_ldarg_type (MonoType *type, MonoValueType *svt)
{
	if (type->byref) {
		*svt = VAL_POINTER;
		return MB_TERM_LDIND_REF;
	}

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*svt = VAL_I32;
		return MB_TERM_LDIND_U4;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		*svt = VAL_POINTER;
		return MB_TERM_LDIND_REF;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		*svt = VAL_I64;
		return MB_TERM_LDIND_I8;
	case MONO_TYPE_R4:
		*svt = VAL_DOUBLE;
		return MB_TERM_LDIND_R4;
	case MONO_TYPE_R8:
		*svt = VAL_DOUBLE;
		return MB_TERM_LDIND_R8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return mono_map_ldarg_type (type->data.klass->enum_basetype, svt);
		} else {
			*svt = VAL_UNKNOWN;
			return MB_TERM_LDIND_OBJ;
		}
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

/**
 * mono_map_call_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding call opcode 
 * for the code generator.
 */
int
mono_map_call_type (MonoType *type, MonoValueType *svt)
{
	if (type->byref) 
		return MB_TERM_CALL_I4;

	switch (type->type) {
	case MONO_TYPE_VOID:
		*svt = VAL_UNKNOWN;
		return MB_TERM_CALL_VOID;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*svt = VAL_I32;
		return MB_TERM_CALL_I4;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return mono_map_call_type (type->data.klass->enum_basetype, svt);
		} else {
			*svt = VAL_I32;
			return MB_TERM_CALL_VOID;
		}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY: 
		*svt = VAL_POINTER;
#if SIZEOF_VOID_P == 8
		return MB_TERM_CALL_I8;
#else
		return MB_TERM_CALL_I4;
#endif
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_I:
	case MONO_TYPE_U:
#endif
		*svt = VAL_I64;
		return MB_TERM_CALL_I8;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		*svt = VAL_DOUBLE;
		return MB_TERM_CALL_R8;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

MBTree *
mono_ctree_new (MonoMemPool *mp, int op, MBTree *left, MBTree *right)
{
	MBTree *t = mono_mempool_alloc0 (mp, sizeof (MBTree));

	t->op = op;
	t->left = left;
	t->right = right;
	t->reg1 = -1;
	t->reg2 = -1;
	t->reg3 = -1;
	t->svt = VAL_UNKNOWN;
	t->cli_addr = -1;
	return t;
}

MBTree *
mono_ctree_new_leaf (MonoMemPool *mp, int op)
{
	return mono_ctree_new (mp, op, NULL, NULL);
}

MBTree *
mono_ctree_new_icon4 (MonoMemPool *mp, gint32 data)
{
	MBTree *t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
	t1->data.i = data;
	return t1;
}

/*
 * prints the tree to stdout
 */
void
mono_print_ctree (MonoFlowGraph *cfg, MBTree *tree)
{
	int arity;

	if (!tree)
		return;

	arity = (tree->left != NULL) + (tree->right != NULL);

	if (arity)
		printf (" (%s", mono_burg_term_string [tree->op]);
	else 
		printf (" %s", mono_burg_term_string [tree->op]);

	switch (tree->op) {
	case MB_TERM_CONST_I4:
		printf ("[%d]", tree->data.i);
		break;
	case MB_TERM_ADDR_L:
		if (VARINFO (cfg, tree->data.i).reg >= 0)
			printf ("[%s|%d]", arch_get_reg_name (VARINFO (cfg, tree->data.i).reg), 
				tree->data.i);
		else
			printf ("[%d]", tree->data.i);
		break;
	}

	g_assert (!(tree->right && !tree->left));

	mono_print_ctree (cfg, tree->left);
	mono_print_ctree (cfg, tree->right);

	if (arity)
		printf (")");
}

/*
 * prints the whole forest to stdout
 */
void
mono_print_forest (MonoFlowGraph *cfg, GPtrArray *forest)
{
	const int top = forest->len;
	int i;

	for (i = 0; i < top; i++) {
		MBTree *t = (MBTree *) g_ptr_array_index (forest, i);
		printf ("       ");
		mono_print_ctree (cfg, t);
		printf ("\n");
	}

}

/**
 * mono_disassemble_code:
 * @code: a pointer to the code
 * @size: the code size in bytes
 *
 * Disassemble to code to stdout.
 */
void
mono_disassemble_code (guint8 *code, int size, char *id)
{
	int i;
	FILE *ofd;

	if (!(ofd = fopen ("/tmp/test.s", "w")))
		g_assert_not_reached ();

	fprintf (ofd, "%s:\n", id);

	for (i = 0; i < size; ++i) 
		fprintf (ofd, ".byte %d\n", (unsigned int) code [i]);

	fclose (ofd);

	system ("as /tmp/test.s -o /tmp/test.o;objdump -d /tmp/test.o"); 
}

