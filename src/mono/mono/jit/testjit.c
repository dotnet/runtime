/*
 * testjit.c: The mono JIT compiler.
 *
 * Author:
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"
#include "regset.h"
#include "codegen.h"

/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,
static char *opcode_names [] = {
#include "mono/cil/opcode.def"	
};
#undef OPDEF

#define SET_VARINFO(vi,t,k,o)  do { vi.type=t; vi.kind=k; vi.offset=o; } while (0)

#define MAKE_CJUMP(name)                                                      \
case CEE_##name:                                                              \
case CEE_##name##_S: {                                                        \
        gint32 target;                                                        \
	int near_jump = *ip == CEE_##name##_S;                                \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
	if (near_jump)                                                        \
		target = cli_addr + 2 + (signed char) *ip;                    \
	else                                                                  \
		target = cli_addr + 5 + (gint32) read32 (ip);                 \
	g_assert (target >= 0 && target <= header->code_size);                \
	g_assert (bcinfo [target].is_block_start);                            \
	tbb = &cfg->bblocks [bcinfo [target].block_id];                       \
	MARK_REACHED (tbb);                                                   \
	t1->data.p = tbb;                                                     \
	ADD_TREE (t1);                                                        \
	ip += near_jump ? 1: 4;		                                      \
	break;                                                                \
}

#define MAKE_BI_ALU(name)                                                     \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
        g_assert (sp [0]->svt == sp [1]->svt);                                \
	PUSH_TREE (t1, sp [0]->svt);                                          \
	break;                                                                \
}

#define MAKE_LDIND(name, op, svt)                                             \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp--;                                                                 \
	t1 = mono_ctree_new (mp, op, *sp, NULL);                              \
	PUSH_TREE (t1, svt);                                                  \
	break;                                                                \
}
	
#define MAKE_LDELEM(name, op, svt, s)                                         \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
        t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t1->data.i = s;                                                       \
        t1 = mono_ctree_new (mp, MB_TERM_MUL, sp [1], t1);                    \
        t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t2->data.i = G_STRUCT_OFFSET (MonoArray, vector);                     \
        t2 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t2);                    \
	t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);                        \
	t1 = mono_ctree_new (mp, op, t1, NULL);                               \
	PUSH_TREE (t1, svt);                                                  \
	break;                                                                \
}
	
#define MAKE_STIND(name, op)                                                  \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, op, sp [0], sp [1]);                         \
	ADD_TREE (t1);                                                        \
	break;                                                                \
}

/*	
#define MAKE_STELEM(name, op, s)                                              \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 3;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_LDELEMA, sp [0], sp [1]);            \
	t1->data.i = s;                                                       \
	t1 = mono_ctree_new (mp, op, t1, sp [2]);                             \
	ADD_TREE (t1);                                                        \
	break;                                                                \
}
*/

#define MAKE_STELEM(name, op, s)                                              \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 3;                                                              \
        t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t1->data.i = s;                                                       \
        t1 = mono_ctree_new (mp, MB_TERM_MUL, sp [1], t1);                    \
        t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);                      \
        t2->data.i = G_STRUCT_OFFSET (MonoArray, vector);                     \
        t2 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t2);                    \
	t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);                        \
	t1 = mono_ctree_new (mp, op, t1, sp [2]);                             \
	ADD_TREE (t1);                                                        \
	break;                                                                \
}
	
/* Whether to dump the assembly code after genreating it */
gboolean mono_jit_dump_asm = FALSE;

/* Whether to dump the forest */
gboolean mono_jit_dump_forest = FALSE;

/* Whether to print function call traces */
gboolean mono_jit_trace_calls = FALSE;

/* 
 * We sometimes need static data, for example the forest generator need it to
 * store constants or class data.
 */
inline static gpointer
mono_alloc_static (int size)
{
	return g_malloc (size);
} 
inline static gpointer
mono_alloc_static0 (int size)
{
	return g_malloc0 (size);
} 


typedef void (*MonoCCtor) (void);

/**
 * mono_jit_init_class:
 * @klass: the class to initialise
 *
 * Initialise the class @klass by calling the class
 * constructor.
 */
static void
mono_jit_init_class (MonoClass *klass)
{
	MonoCCtor cctor;
	MonoMethod *method;
	int i;

	if (!klass->metadata_inited)
		mono_class_metadata_init (klass);

	if (klass->inited)
		return;

	if (klass->parent && !klass->parent->inited)
		mono_jit_init_class (klass->parent);
	
	klass->inited = 1;

	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
		    (strcmp (".cctor", method->name) == 0)) {
	
			cctor = arch_compile_method (method);
			cctor ();
			return;
		}
	}
	/* No class constructor found */
}

static int
map_store_svt_type (int svt)
{
	switch (svt) {
	case VAL_I32:
	case VAL_POINTER:
		return MB_TERM_STIND_I4;
	case VAL_I64:
		return MB_TERM_STIND_I8;
	case VAL_DOUBLE:
		return MB_TERM_STIND_R8;
	default:
		g_assert_not_reached ();
	}

	return 0;
}

static int
map_stvalue_type (MonoClass *class)
{
	int size;

	g_assert (class->valuetype);

	if (!class->inited)
		mono_jit_init_class (class);

	size =  class->instance_size - sizeof (MonoObject);

	switch (size) {
	case 4:
		return MB_TERM_STIND_I4;
	case 2:
		return MB_TERM_STIND_I2;
	case 1:
		return MB_TERM_STIND_I1;
	}
	return MB_TERM_STIND_OBJ;
}

/**
 * map_stind_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding store opcode 
 * for the code generator.
 */
static int
map_stind_type (MonoType *type)
{
	if (type->byref) 
		return MB_TERM_STIND_I4;

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return MB_TERM_STIND_I1;	
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return MB_TERM_STIND_I2;	
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return MB_TERM_STIND_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_STIND_I8;
	case MONO_TYPE_R4:
		return MB_TERM_STIND_R4;
	case MONO_TYPE_R8:
		return MB_TERM_STIND_R8;
	case MONO_TYPE_VALUETYPE: 
		return map_stvalue_type (type->data.klass);
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

/**
 * map_ldind_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding load opcode 
 * for the code generator.
 */
static int
map_ldind_type (MonoType *type, MonoValueType *svt)
{
	if (type->byref) {
		*svt = VAL_POINTER;
		return MB_TERM_LDIND_I4;
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
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
		*svt = VAL_I32;
		return MB_TERM_LDIND_I4;
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
		return MB_TERM_LDIND_U4;
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
	case MONO_TYPE_VALUETYPE: {
		int size =  type->data.klass->instance_size - sizeof (MonoObject);

		switch (size) {
		case 4:
			*svt = VAL_I32;
			return MB_TERM_LDIND_U4;
		case 2:
			*svt = VAL_I32;
			return MB_TERM_LDIND_U2;
		case 1:
			*svt = VAL_I32;
			return MB_TERM_LDIND_U1;
		default:
			*svt = VAL_UNKNOWN;
			return MB_TERM_LDIND_OBJ;
		}
	}
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

/**
 * map_call_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding call opcode 
 * for the code generator.
 */
static int
map_call_type (MonoType *type, MonoValueType *svt)
{
	switch (type->type) {
	case MONO_TYPE_VOID:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*svt = VAL_I32;
		return MB_TERM_CALL_I4;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
		*svt = VAL_POINTER;
		return MB_TERM_CALL_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
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

/*
 * prints the tree to stdout
 */
void
mono_print_ctree (MBTree *tree)
{
	int arity;

	if (!tree)
		return;

	arity = (tree->left != NULL) + (tree->right != NULL);

	if (arity)
		printf (" (%s", mono_burg_term_string [tree->op]);
	else 
		printf (" %s", mono_burg_term_string [tree->op]);

	g_assert (!(tree->right && !tree->left));

	mono_print_ctree (tree->left);
	mono_print_ctree (tree->right);

	if (arity)
		printf (")");
}

/*
 * prints the whole forest to stdout
 */
void
mono_print_forest (GPtrArray *forest)
{
	const int top = forest->len;
	int i;

	for (i = 0; i < top; i++) {
		MBTree *t = (MBTree *) g_ptr_array_index (forest, i);
		printf ("       ");
		mono_print_ctree (t);
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
mono_disassemble_code (guint8 *code, int size)
{
	int i;
	FILE *ofd;

	if (!(ofd = fopen ("/tmp/test.s", "w")))
		g_assert_not_reached ();

	for (i = 0; i < size; ++i) 
		fprintf (ofd, ".byte %d\n", (unsigned int) code [i]);

	fclose (ofd);

	system ("as /tmp/test.s -o /tmp/test.o;objdump -d /tmp/test.o"); 
}

static int
arch_allocate_var (MonoFlowGraph *cfg, int size, int align, MonoValueKind kind, MonoValueType type)
{
	MonoVarInfo vi;

	switch (kind) {
	case MONO_TEMPVAR:
	case MONO_LOCALVAR: {
		cfg->locals_size += align - 1;
		cfg->locals_size &= ~(align - 1);
		cfg->locals_size += size;

		SET_VARINFO (vi, type, kind, - cfg->locals_size);
		g_array_append_val (cfg->varinfo, vi);
		break;
	}
	case MONO_ARGVAR: {
		SET_VARINFO (vi, type, kind, cfg->args_size + 8);
		g_array_append_val (cfg->varinfo, vi);

		cfg->args_size += align - 1;
		cfg->args_size &= ~(align - 1);
		cfg->args_size += size;
		break;
	}
	default:
		g_assert_not_reached ();
	}

	return cfg->varinfo->len - 1;
}

inline static void
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

static int
mono_allocate_intvar (MonoFlowGraph *cfg, int slot, MonoValueType type)
{
	int size, align, vnum;

	g_assert (type != VAL_UNKNOWN);

	if ((vnum = cfg->intvars[slot][type - 1]))
		return vnum;

	mono_get_val_sizes (type, &size, &align);

	return cfg->intvars[slot][type - 1] = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, type);
}

/**
 * ctree_create_load:
 * @mp: pointer to a memory pool
 * @addr_type: address type (MB_TERM_ADDR_L or MB_TERM_ADDR_G)
 * @type: the type of the value
 * @addr: the address of the value
 *
 * Creates a tree to load the value at address @addr.
 */
inline static MBTree *
ctree_create_load (MonoMemPool *mp, int addr_type, MonoType *type, gpointer addr, MonoValueType *svt)
{
	int ldind = map_ldind_type (type, svt);
	MBTree *t;

	t = mono_ctree_new_leaf (mp, addr_type);
	t->data.p = addr;
	t = mono_ctree_new (mp, ldind, t, NULL);

	return t;
}

/**
 * ctree_create_store:
 * @mp: pointer to a memory pool
 * @addr_type: address type (MB_TERM_ADDR_L or MB_TERM_ADDR_G)
 * @s: the value (tree) to store
 * @type: the type of the value
 * @addr: the address of the value
 *
 * Creates a tree to store the value @s at address @addr.
 */
inline static MBTree *
ctree_create_store (MonoMemPool *mp, int addr_type, MBTree *s, MonoType *type, gpointer addr)
{
	int stind = map_stind_type (type);
	MBTree *t;

	t = mono_ctree_new_leaf (mp, addr_type);
	t->data.p = addr;
	t = mono_ctree_new (mp, stind, t, s);

	return t;
}

inline static MBTree *
ctree_dup_address (MonoMemPool *mp, MBTree *s)
{
	MBTree *t;

	switch (s->op) {

	case MB_TERM_ADDR_L:
	case MB_TERM_ADDR_G:
		t = mono_ctree_new_leaf (mp, s->op);
		t->data.i = s->data.i;
		t->svt = VAL_POINTER;
		return t;
	default:
		g_warning ("unknown tree opcode %d", s->op);
		g_assert_not_reached ();
	}

	return NULL;
}

/**
 * Create a duplicate of the value of a tree. This is
 * easy for trees starting with LDIND/STIND, since the
 * duplicate is simple a LDIND tree with the same address.
 * For other trees we have to split the tree into one tree
 * storing the value to a new temporary variable, and 
 * another tree which loads that value back. We can then
 * duplicate the second tree.
 */
static MBTree *
ctree_create_dup (MonoMemPool *mp, MBTree *s)
{
	MBTree *t;
	
	switch (s->op) {
	case MB_TERM_STIND_I1:
	case MB_TERM_LDIND_I1:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I1, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_STIND_I2:
	case MB_TERM_LDIND_I2:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I2, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_STIND_I4:
	case MB_TERM_LDIND_I4:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I4, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_LDIND_U4:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_U4, t, NULL);
		t->svt = VAL_I32;
		break;
	case MB_TERM_STIND_I8:
	case MB_TERM_LDIND_I8:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_I8, t, NULL);
		t->svt = VAL_I64;
		break;
	case MB_TERM_STIND_R4:
	case MB_TERM_LDIND_R4:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_R4, t, NULL);
		t->svt = VAL_DOUBLE;
		break;
	case MB_TERM_STIND_R8:
	case MB_TERM_LDIND_R8:
		t = ctree_dup_address (mp, s->left);
		t = mono_ctree_new (mp, MB_TERM_LDIND_R8, t, NULL);
		t->svt = VAL_DOUBLE;
		break;
	default:
		g_warning ("unknown op \"%s\"", mono_burg_term_string [s->op]);
		g_assert_not_reached ();
	}

	return t;
}

static MBTree *
mono_store_tree (MonoFlowGraph *cfg, int slot, MBTree *s, MBTree **dup)
{
	MonoMemPool *mp = cfg->mp;
	MBTree *t;
	int vnum;

	switch (s->op) {
	case MB_TERM_STIND_I1:
	case MB_TERM_LDIND_I1:
	case MB_TERM_STIND_I2:
	case MB_TERM_LDIND_I2:
	case MB_TERM_STIND_I4:
	case MB_TERM_LDIND_I4:
	case MB_TERM_STIND_I8:
	case MB_TERM_LDIND_I8:
	case MB_TERM_STIND_R4:
	case MB_TERM_LDIND_R4:
	case MB_TERM_STIND_R8:
	case MB_TERM_LDIND_R8: {
		if (dup)
			*dup = ctree_create_dup (mp, s);
		return NULL;
	}			
	default: {
			g_assert (s->svt != VAL_UNKNOWN);

			if (slot >= 0) 
				vnum = mono_allocate_intvar (cfg, slot, s->svt);
			else {
				int size, align;
				mono_get_val_sizes (s->svt, &size, &align);
				vnum = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, s->svt);
			}

			t = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t->data.i = vnum;
		       
			t = mono_ctree_new (mp, map_store_svt_type (s->svt), t, s);
			t->svt = s->svt;
		}
	}

	if (dup) 
		mono_store_tree (cfg, -1, t, dup);

	return t;
}

MonoFlowGraph *
mono_cfg_new (MonoMethod *method, MonoMemPool *mp)
{
	MonoVarInfo vi;
	MonoFlowGraph *cfg;

	cfg = mono_mempool_alloc0 (mp, sizeof (MonoFlowGraph));

	cfg->method = method;
	cfg->mp = mp;

	cfg->varinfo = g_array_new (FALSE, TRUE, sizeof (MonoVarInfo));
	
	SET_VARINFO (vi, 0, 0, 0);
	g_array_append_val (cfg->varinfo, vi); /* add invalid value at position 0 */

	cfg->intvars = mono_mempool_alloc0 (mp, sizeof (guint16) * VAL_DOUBLE * 
					    ((MonoMethodNormal *)method)->header->max_stack);
	return cfg;
}

void
mono_cfg_free (MonoFlowGraph *cfg)
{
	int i;

	for (i = 0; i < cfg->block_count; i++) {
		g_ptr_array_free (cfg->bblocks [i].forest, TRUE);
	}

	g_array_free (cfg->varinfo, TRUE);
}

#define CREATE_BLOCK(t) {if (!bcinfo [t].is_block_start) {block_count++;bcinfo [t].is_block_start = 1;}}

void
mono_analyze_flow (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMemPool *mp = cfg->mp;
	register const unsigned char *ip, *end;
	MonoMethodHeader *header;
	MonoBytecodeInfo *bcinfo;
	MonoBBlock *bblocks, *bb;
	gboolean block_end;
	int i, block_count;

	header = ((MonoMethodNormal *)method)->header;

	bcinfo = mono_mempool_alloc0 (mp, header->code_size * sizeof (MonoBytecodeInfo));
	bcinfo [0].is_block_start = 1;
	block_count = 1;
	block_end = FALSE;

	ip = header->code;
	end = ip + header->code_size;

	/* fixme: add block boundaries for exceptions */

	while (ip < end) {
		guint32 cli_addr = ip - header->code;

		//printf ("IL%04x OPCODE %s\n", cli_addr, opcode_names [*ip]);
		
		if (block_end) {
			CREATE_BLOCK (cli_addr);
			block_end = FALSE;
		}

		switch (*ip) {

		case CEE_THROW:
			ip++;
			block_end = 1;
			break;
		case CEE_NOP: 
		case CEE_BREAK:
		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
		case CEE_LDNULL:
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: 
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
		case CEE_NEG:
		case CEE_NOT:
		case CEE_DUP:
		case CEE_POP:
		case CEE_ADD:
		case CEE_SUB:
		case CEE_AND:
		case CEE_OR:
		case CEE_XOR:
		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
		case CEE_MUL:
		case CEE_DIV:
		case CEE_DIV_UN:
		case CEE_REM:
		case CEE_REM_UN:
		case CEE_LDIND_I1:
		case CEE_LDIND_U1:
		case CEE_LDIND_I2:
		case CEE_LDIND_U2:
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_REF:
		case CEE_LDIND_U4:
		case CEE_LDIND_I8:
		case CEE_LDIND_R4:
		case CEE_LDIND_R8:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
		case CEE_STIND_REF:
		case CEE_STELEM_I:
		case CEE_STELEM_I1:
		case CEE_STELEM_I2:
		case CEE_STELEM_I4:
		case CEE_STELEM_I8:
		case CEE_STELEM_R4:
		case CEE_STELEM_R8:
		case CEE_STELEM_REF:
		case CEE_LDLEN:
		case CEE_LDELEM_I1:
		case CEE_LDELEM_U1:
		case CEE_LDELEM_I2:
		case CEE_LDELEM_U2:
		case CEE_LDELEM_I4:
		case CEE_LDELEM_U4:
		case CEE_LDELEM_I8:
		case CEE_LDELEM_I:
		case CEE_LDELEM_R4:
		case CEE_LDELEM_R8:
		case CEE_LDELEM_REF:
		case CEE_CONV_I1:
		case CEE_CONV_U1:
		case CEE_CONV_I2:
		case CEE_CONV_U2:
		case CEE_CONV_I:
		case CEE_CONV_U:
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case CEE_CONV_I8:
		case CEE_CONV_U8:
		case CEE_CONV_R4:
		case CEE_CONV_R8:
			ip++;
			break;
		case CEE_RET:
			ip++;
			block_end = 1;
			break;
		case CEE_BOX:
		case CEE_UNBOX:
		case CEE_LDOBJ:
		case CEE_LDSTR:
		case CEE_LDSFLD:
		case CEE_LDSFLDA:
		case CEE_LDFLD:
		case CEE_LDFLDA:
		case CEE_STSFLD: 
		case CEE_STFLD:
		case CEE_NEWOBJ:
		case CEE_NEWARR:
		case CEE_LDTOKEN:
		case CEE_CALL:
		case CEE_CALLVIRT:
		case CEE_ISINST:
		case CEE_CASTCLASS:
		case CEE_LDC_I4:
		case CEE_LDC_R4:
			ip += 5;
			break;
		case CEE_BR:
		case CEE_LEAVE:
		case CEE_BRTRUE:
		case CEE_BRFALSE:
		case CEE_BGT:
		case CEE_BGT_UN:
		case CEE_BLT:
		case CEE_BLT_UN:
		case CEE_BNE_UN:
		case CEE_BEQ:
		case CEE_BGE:
		case CEE_BGE_UN:
		case CEE_BLE:
		case CEE_BLE_UN: {
			gint32 offset;
			ip++;
			offset = read32 (ip);
			ip += 4;
			CREATE_BLOCK (cli_addr + 5 + offset);
			block_end = 1;
			break;
		}
		case CEE_LDC_I8:
		case CEE_LDC_R8:
			ip += 9;
			break;
		case CEE_LDC_I4_S:
		case CEE_LDLOC_S:
		case CEE_LDLOCA_S:
		case CEE_STLOC_S:
		case CEE_LDARG_S: 
			ip += 2;
			break;
		case CEE_BR_S:
		case CEE_LEAVE_S:
		case CEE_BRTRUE_S:
		case CEE_BRFALSE_S:
		case CEE_BGT_S:
		case CEE_BGT_UN_S:
		case CEE_BLT_S:
		case CEE_BLT_UN_S:
		case CEE_BNE_UN_S:
		case CEE_BEQ_S:
		case CEE_BGE_S:
		case CEE_BGE_UN_S:
		case CEE_BLE_S:
		case CEE_BLE_UN_S: {
			gint32 offset;
			ip++;
			offset = (signed char)*ip;
			ip++;
			CREATE_BLOCK (cli_addr + 2 + offset);
			block_end = 1;
			break;
		}
		case CEE_SWITCH: {
			gint32 i, st, target, n;
			++ip;
			n = read32 (ip);
			ip += 4;
			st = cli_addr + 5 + 4 * n;
			CREATE_BLOCK (st);

			for (i = 0; i < n; i++) {
				target = read32 (ip) + st;
				ip += 4;
				CREATE_BLOCK (st + target);			
			}
			break;
		}
		case 0xFE: {
			++ip;			
			switch (*ip) {
				
			case CEE_CEQ:
				ip++;
				break;
			case CEE_LDARG:
			case CEE_INITOBJ:
				ip +=5;
				break;
			default:
				g_error ("Unimplemented opcode at IL_%04x "
					 "0xFE %02x", ip - header->code, *ip);
			}
			break;
		}

		default:
			g_warning ("unknown instruction `%s' at IL_%04X", 
				   opcode_names [*ip], ip - header->code);
			g_assert_not_reached ();
		}
	}


	g_assert (block_count);

	bb = bblocks  = mono_mempool_alloc0 (mp, sizeof (MonoBBlock) * block_count);

	block_count = 0;
	bblocks [0].reached = 1;

	for (i = 0; i < header->code_size; i++) {
		if (bcinfo [i].is_block_start) {
			bb->cli_addr = i;
			if (block_count)
				bb [-1].length = i - bb [-1].cli_addr; 
			bcinfo [i].block_id = block_count;
			bb++;
			block_count++;
		}
	}
	bb [-1].length = header->code_size - bb [-1].cli_addr; 

	cfg->bcinfo = bcinfo;
	cfg->bblocks = bblocks;
	cfg->block_count = block_count;
}

static MBTree **
mono_copy_stack (MBTree **sp, int depth, MonoMemPool *mp)
{
	int i;
	MBTree **copy;

	copy = mono_mempool_alloc (mp, depth * sizeof (MBTree *));
	
	sp -= depth;

	for (i = 0; i < depth; i++) {
		switch (sp [i]->op) {
		case MB_TERM_LDIND_I1:
		case MB_TERM_LDIND_U1:
		case MB_TERM_LDIND_I2:
		case MB_TERM_LDIND_U2:
		case MB_TERM_LDIND_I4:
		case MB_TERM_LDIND_U4:
		case MB_TERM_LDIND_I8:
		case MB_TERM_LDIND_R4:
		case MB_TERM_LDIND_R8:
			break;
		default:
			g_warning ("cant handle type %s (%d)\n", mono_burg_term_string [sp [i]->op], i);
				// fixme: store the value somewhere
			g_assert_not_reached ();
		}
	}

	g_assert_not_reached ();
	return NULL;
}

/**
 * ves_array_element_address:
 * @this: a pointer to the array object
 *
 * Returns: the address of an array element.
 */
static gpointer 
ves_array_element_address (MonoArray *this, ...)
{
	MonoClass *class;
	va_list ap;
	int i, ind, esize;
	gpointer ea;

	g_assert (this != NULL);

	va_start(ap, this);

	class = this->obj.klass;

	ind = va_arg(ap, int) - this->bounds [0].lower_bound;
	for (i = 1; i < class->rank; i++) {
		ind = ind*this->bounds [i].length + va_arg(ap, int) -
			this->bounds [i].lower_bound;;
	}

	esize = mono_array_element_size (class);
	ea = (gpointer*)((char*)this->vector + (ind * esize));
	//printf ("AADDRESS %p %p %d\n", this, ea, ind);

	va_end(ap);

	return ea;
}

static MonoArray *
mono_array_new_va (MonoMethod *cm, ...)
{
	va_list ap;
	guint32 *lengths;
	guint32 *lower_bounds;
	int pcount = cm->signature->param_count;
	int rank = cm->klass->rank;
	int i, d;

	va_start (ap, cm);

	lengths = alloca (sizeof (guint32) * pcount);
	for (i = 0; i < pcount; ++i)
		lengths [i] = d = va_arg(ap, int);

	if (rank == pcount) {
		/* Only lengths provided. */
		lower_bounds = NULL;
	} else {
		g_assert (pcount == (rank * 2));
		/* lower bounds are first. */
		lower_bounds = lengths;
		lengths += rank;
	}
	va_end(ap);

	return mono_array_new_full (cm->klass, lengths, lower_bounds);
}

#define ADD_TREE(t)     do { g_ptr_array_add (forest, (t)); } while (0)
#define PUSH_TREE(t,k)  do { *sp = t; sp++; t->svt = k; } while (0)

#define LOCAL_POS(n)    (1 + n)
#define LOCAL_TYPE(n)   ((header)->locals [(n)])

#define ARG_POS(n)      (1 + header->num_locals + n)
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])

#define MARK_REACHED(bb) do { if (!bb->reached) { bb->reached = 1; }} while (0)

/**
 * mono_create_cfg:
 * @method: the method to analyse
 * @mp: a memory pool
 * @locals_size: to return the size of local vars
 *
 * This is the architecture independent part of JIT compilation.
 * It creates a forest of trees which can then be fed into the
 * architecture dependent code generation.
 *
 * The algorithm is from Andi Krall, the same is used in CACAO
 */
void
mono_analyze_stack (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMemPool *mp = cfg->mp;
	MonoBytecodeInfo *bcinfo = cfg->bcinfo;
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	MonoValueType svt;
	MBTree **sp, **stack, **arg_sp, *t1, *t2, *t3;
	register const unsigned char *ip, *end;
	GPtrArray *forest;
	int i, j, depth, repeat_count;
	gboolean repeat, superblock_end;
	MonoBBlock *bb, *tbb;

	header = ((MonoMethodNormal *)method)->header;
	signature = method->signature;
	image = method->klass->image; 

	sp = stack = alloca (sizeof (MBTree *) * (header->max_stack + 1));
	
	if (header->num_locals) {
		int size, align;

		for (i = 0; i < header->num_locals; ++i) {
			size = mono_type_size (header->locals [i], &align);
			arch_allocate_var (cfg, size, align, MONO_LOCALVAR, VAL_UNKNOWN);
		}
	}
	
	if (signature->params_size) {
		int align, size;
		int has_this = signature->hasthis;

		if (has_this) {
			size = align = sizeof (gpointer);
			arch_allocate_var (cfg, size, align, MONO_ARGVAR, VAL_POINTER);
		}

		for (i = 0; i < signature->param_count; ++i) {
			size = mono_type_size (signature->params [i], &align);
			if (size < 4) {
				size = 4; 
				align = 4;
			}
			arch_allocate_var (cfg, size, align, MONO_ARGVAR, VAL_UNKNOWN);
		}
	}

	repeat_count = 0;

	do {
		repeat = FALSE;
		superblock_end = TRUE;
		sp = stack;

		//printf ("START\n");
		for (i = 0; i < cfg->block_count; i++) {
			bb = &cfg->bblocks [i];

			//printf ("BBS %d %d %d %d\n", i, bb->reached, bb->finished, superblock_end);
			
			if (!bb->reached && !superblock_end) {

				if ((depth = sp - stack)) {

					g_assert_not_reached ();
					//bb->instack = mono_copy_stack;
					bb->indepth = depth;

					for (j = 0; j < depth; j++) {
						bb->instack [j] = ctree_create_dup (mp, sp [j]);
					}
				}

				MARK_REACHED (bb);
			} 
			
			if (bb->reached) {

				if (!bb->finished) {

					sp = stack;

					for (j = 0; j < bb->indepth; j++) {
						sp [j] = bb->instack [j];
					}
					sp += bb->indepth;

					bb->finished = 1;
				
					ip = header->code + bb->cli_addr;
					end = ip + bb->length;

					bb->forest = forest = g_ptr_array_new ();
				
					superblock_end = FALSE;

        while (ip < end) {
		guint32 cli_addr = ip - header->code;
					
		//printf ("%d IL%04x OPCODE %s %d %d %d\n", i, cli_addr, opcode_names [*ip], 
		//forest->len, superblock_end, sp - stack);

		switch (*ip) {
			case CEE_THROW: {
			--sp;
			ip++;
			
			// fixme: 

			t1 = mono_ctree_new_leaf (mp, MB_TERM_NOP);
			ADD_TREE (t1);		
			superblock_end = TRUE;
			break;
		}
		case CEE_BOX: {
			MonoClass *c;
			guint32 token;
			
			--sp;
			++ip;
			token = read32 (ip);
			ip += 4;
			
			c = mono_class_get (image, token);
			
			t1 = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ);
			t1->data.p = c;
			t1->svt = VAL_POINTER;

			t1 = mono_store_tree (cfg, -1, t1, &t3);
			g_assert (t1);
			ADD_TREE (t1);

			t1 = ctree_create_dup (mp, t3);
			t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t2->data.i = sizeof (MonoObject);
			t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);

			t1 = mono_ctree_new (mp, map_stvalue_type (c), t1, *sp);
			ADD_TREE (t1);

			PUSH_TREE (t3, VAL_POINTER);

			break;
		}
		case CEE_UNBOX: {
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			// fixme: add type check

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = sizeof (MonoObject);
			t1 = mono_ctree_new (mp, MB_TERM_ADD, *sp, t1);

			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_LDLEN: {
			ip++;
			sp--;
			
			t1 = mono_ctree_new (mp, MB_TERM_LDLEN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}

		case CEE_LDOBJ: {
			guint32 token;
			MonoClass *c;
			int size;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			c = mono_class_get (image, token);
			g_assert (c->valuetype);

			size = c->instance_size - sizeof (MonoObject);
			switch (size) {
			case 4:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_U4, *sp, NULL);
				svt = VAL_I32;
				break;
			case 2:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_U2, *sp, NULL);
				svt = VAL_I32;
				break;
			case 1:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_U1, *sp, NULL);
				svt = VAL_I32;
				break;
			default:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_OBJ, *sp, NULL);
				svt = VAL_UNKNOWN;
				break;
			}
			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDSTR: {
			MonoObject *o;
			guint32 index;

			++ip;
			index = mono_metadata_token_index (read32 (ip));
			ip += 4;

			o = (MonoObject *) mono_ldstr (image, index);
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.p = o;

			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_LDSFLD:
		case CEE_LDSFLDA: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDSFLDA;
			gpointer addr;

			++ip;
			token = read32 (ip);
			ip += 4;
			
			/* need to handle fieldrefs */
			klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 
			        mono_metadata_typedef_from_field (image, token & 0xffffff));

			if (!klass->inited)
				mono_jit_init_class (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			addr = MONO_CLASS_STATIC_FIELDS_BASE (klass) + field->offset;

			if (load_addr) {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = addr;
				svt = VAL_POINTER;
			} else {
				t1 = ctree_create_load (mp, MB_TERM_ADDR_G, field->type, addr, &svt);
			}

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDFLD:
		case CEE_LDFLDA: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			int load_addr = *ip == CEE_LDFLDA;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			/* need to handle fieldrefs */
			klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 
			        mono_metadata_typedef_from_field (image, token & 0xffffff));

			if (!klass->inited)
				mono_jit_init_class (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			
			if (klass->valuetype)
				t1->data.i = field->offset - sizeof (MonoObject);
			else 
				t1->data.i = field->offset;

			t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);

			if (!load_addr)
				t1 = mono_ctree_new (mp, map_ldind_type (field->type, &svt), t1, NULL);
			else
				svt = VAL_POINTER;

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_STSFLD: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;
			gpointer addr;

			++ip;
			token = read32 (ip);
			ip += 4;
			--sp;

			/* need to handle fieldrefs */
			klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 
			        mono_metadata_typedef_from_field (image, token & 0xffffff));

			if (!klass->inited)
				mono_jit_init_class (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			addr = MONO_CLASS_STATIC_FIELDS_BASE (klass) + field->offset;
			t1 = ctree_create_store (mp, MB_TERM_ADDR_G, *sp, field->type, addr);

			ADD_TREE (t1);
			break;
		}
		case CEE_STFLD: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			/* need to handle fieldrefs */
			klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 
			        mono_metadata_typedef_from_field (image, token & 0xffffff));

			if (!klass->inited)
				mono_jit_init_class (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			if (klass->valuetype)
				t1->data.i = field->offset - sizeof (MonoObject);
			else 
				t1->data.i = field->offset;

			printf ("VALUETYPE %d %d %d\n", klass->valuetype, field->offset, t1->data.i);

			t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);
			t1 = mono_ctree_new (mp, map_stind_type (field->type), t1, sp [1]);

			ADD_TREE (t1);
			break;
		}
		case CEE_NOP: { 
			++ip;
			break;
		}
		case CEE_BREAK: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BREAK);
			ADD_TREE (t1);
			break;
		} 
		case CEE_SWITCH: {
			guint32 i, n;
			MonoBBlock **jt;
			gint32 st, target;

			++ip;
			n = read32 (ip);
			ip += 4;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_SWITCH, *sp, NULL);
			jt = t1->data.p = mono_alloc_static (sizeof (gpointer) * (n + 2));
			st = cli_addr + 5 + 4 * n;
			
			// hack: we store n at position 0
			jt [0] = (MonoBBlock *)n;

			for (i = 1; i <= (n + 1); i++) {
				if (i > n)
					target = st;
				else {
					target = read32 (ip) + st;
					ip += 4;
				}
				g_assert (target >= 0 && target <= header->code_size);
				g_assert (bcinfo [target].is_block_start);
				tbb = &cfg->bblocks [bcinfo [target].block_id];
				MARK_REACHED (tbb);
				jt [i] = tbb; 
			}

			ADD_TREE (t1);
			break;
		}
		case CEE_LDTOKEN: {
			gpointer handle;
			MonoClass *handle_class;

			++ip;
			handle = mono_ldtoken (image, read32 (ip), &handle_class);
			ip += 4;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.p = handle;
			PUSH_TREE (t1, VAL_POINTER);

			break;
		}
		case CEE_NEWARR: {
			MonoClass *class;
			guint32 token;

			ip++;
			--sp;
			token = read32 (ip);
			class = mono_class_get (image, token);
			ip += 4;

			t1 = mono_ctree_new (mp, MB_TERM_NEWARR, *sp, NULL);
			t1->data.p = class;
			PUSH_TREE (t1, VAL_POINTER);

			break;
		}
		case CEE_NEWOBJ: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			MBTree *this = NULL;
			guint32 token;
			int i, align, size, args_size = 0;
			int newarr = FALSE;

			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			g_assert (cm);
			g_assert (!strcmp (cm->name, ".ctor"));
			
			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			g_assert (csig->hasthis);
			
			arg_sp = sp -= csig->param_count;

			if (cm->klass->parent == mono_defaults.array_class) {

				newarr = TRUE;
				this = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				this->data.p = cm;

			} else {
				
				this = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ);
				this->data.p = cm->klass;
				this->svt = VAL_POINTER;

				t1 = mono_store_tree (cfg, -1, this, &this);
				g_assert (t1);
				ADD_TREE (t1);

			}

			for (i = csig->param_count - 1; i >= 0; i--) {
				t1 = mono_ctree_new (mp, MB_TERM_ARG, arg_sp [i], NULL);	
				ADD_TREE (t1);
				size = mono_type_size (cm->signature->params [i], &align);
				args_size += (size + 3) & ~3;
			}

			t1 = mono_ctree_new (mp, MB_TERM_ARG, this, NULL);	
			ADD_TREE (t1);
			args_size += sizeof (gpointer);

			if (newarr) {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = mono_array_new_va;

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, t2, NULL);
				t1->data.i = args_size;
				t1->svt = VAL_I32;

			} else {
				
				if (!cm->addr)
					cm->addr = arch_create_simple_jit_trampoline (cm);

				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t2->data.p = (char *)cm + G_STRUCT_OFFSET (MonoMethod, addr);
				t2 = mono_ctree_new (mp, MB_TERM_LDIND_I4, t2, NULL);
			}

			t1 = mono_ctree_new (mp, map_call_type (csig->ret, &svt), t2, NULL);
			t1->data.i = args_size;
			t1->svt = svt;

			if (newarr) {
				
				t1 = mono_store_tree (cfg, -1, t1, &t2);
				g_assert (t1);
				ADD_TREE (t1);
				PUSH_TREE (t2, t2->svt);

			} else {

				ADD_TREE (t1);			
				t1 = ctree_create_dup (mp, this);		
				PUSH_TREE (t1, t1->svt);
			}
			break;
		}
		case CEE_CALL: 
		case CEE_CALLVIRT: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			MBTree *this = NULL;
			guint32 token;
			int i, align, size, args_size = 0;
			int virtual = *ip == CEE_CALLVIRT;
			gboolean array_set = FALSE;
			gboolean array_get = FALSE;
			int nargs;

			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			g_assert (cm);
			
			if ((cm->flags & METHOD_ATTRIBUTE_FINAL) ||
			    !(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
				virtual = 0;

			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			g_assert (!virtual || csig->hasthis);

			nargs = csig->param_count;
			arg_sp = sp -= nargs;
			
			if ((cm->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
			    (cm->klass->parent == mono_defaults.array_class)) {
				if (!strcmp (cm->name, "Set")) { 
					array_set = TRUE;
					nargs--;
				} else if (!strcmp (cm->name, "Get")) 
					array_get = TRUE;
			}

			for (i = nargs - 1; i >= 0; i--) {
				t1 = mono_ctree_new (mp, MB_TERM_ARG, arg_sp [i], NULL);	
				ADD_TREE (t1);
				size = mono_type_size (cm->signature->params [i], &align);
				args_size += (size + 3) & ~3;
			}

			if (csig->hasthis) {
				this = *(--sp);
				t1 = mono_ctree_new (mp, MB_TERM_ARG, this, NULL);	
				ADD_TREE (t1);
				args_size += sizeof (gpointer);
			}

			if (array_get) {
				int size, align, vnum;
				
				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = ves_array_element_address;

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, t2, NULL);
				t1->data.i = args_size;

				t1 = mono_ctree_new (mp, map_ldind_type (csig->ret, &svt), t1, NULL);
				t1->svt = svt;		

				mono_get_val_sizes (t1->svt, &size, &align);
				vnum = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, svt);

				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t2->data.i = vnum;
				t1 = mono_ctree_new (mp, map_store_svt_type (svt), t2, t1);
				t1->svt = svt;

				ADD_TREE (t1);
				t1 = ctree_create_dup (mp, t1);
				PUSH_TREE (t1, t1->svt);

			} else if (array_set) {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = ves_array_element_address;

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, t2, NULL);
				t1->data.i = args_size;

				t1 = mono_ctree_new (mp, map_stind_type (csig->params [nargs]), t1, arg_sp [nargs]);
				ADD_TREE (t1);
			
			} else {

				if (virtual) {
				
					t2 = ctree_create_dup (mp, this);
			       
					if (!cm->klass->metadata_inited)
						mono_class_metadata_init (cm->klass);

					if (cm->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
						t2 = mono_ctree_new (mp, MB_TERM_INTF_ADDR, t2, NULL);
					else 
						t2 = mono_ctree_new (mp, MB_TERM_VFUNC_ADDR, t2, NULL);
	 
					t2->data.m = cm;

				} else {
				
					if (!cm->addr)
						cm->addr = arch_create_simple_jit_trampoline (cm);
				
					t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
					t2->data.p = (char *)cm + G_STRUCT_OFFSET (MonoMethod, addr);
					t2 = mono_ctree_new (mp, MB_TERM_LDIND_I4, t2, NULL);
				}

				t1 = mono_ctree_new (mp, map_call_type (csig->ret, &svt), t2, NULL);
				t1->data.i = args_size;
				t1->svt = svt;

				if (csig->ret->type != MONO_TYPE_VOID) {

					t1 = mono_store_tree (cfg, -1, t1, &t2);
					g_assert (t1);
					ADD_TREE (t1);
					PUSH_TREE (t2, t2->svt);
					
				} else
					ADD_TREE (t1);
   
			}

			break;
		}
		case CEE_ISINST:
		case CEE_CASTCLASS: {
			guint32 token;
			++ip;
			token = read32 (ip);
			
			/* fixme: do something */

			ip += 4;
			break;
		}
		case CEE_LDC_I4_S: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = *(gint8 *)ip;
			++ip;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDC_I4: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = read32 (ip);
			ip += 4;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8: {
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = (*ip) - CEE_LDC_I4_0;
			++ip;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDNULL: {
			//fixme: don't know if this is portable ?
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = 0;
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_LDC_I8: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I8);
			t1->data.l = read64 (ip);
			ip += 8;
			PUSH_TREE (t1, VAL_I64);		
			break;
		}
		case CEE_LDC_R4: {
			float *f = mono_alloc_static (sizeof (float));
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_R4);
			readr4 (ip, f);
			t1->data.p = f;
			ip += 4;
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		}
		case CEE_LDC_R8: { 
			double *d = mono_alloc_static (sizeof (double));
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_R8);
			readr8 (ip, d);
			t1->data.p = d;
			ip += 8;
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		}
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int n = (*ip) - CEE_LDLOC_0;
			++ip;

			t1 = ctree_create_load (mp, MB_TERM_ADDR_L, LOCAL_TYPE (n), 
						(gpointer)LOCAL_POS (n), &svt);

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDLOC_S: {
			++ip;
			
			t1 = ctree_create_load (mp, MB_TERM_ADDR_L, LOCAL_TYPE (*ip), 
						(gpointer)LOCAL_POS (*ip), &svt);
			++ip;

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDLOCA_S: {
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.p = (gpointer)LOCAL_POS (*ip);
			++ip;
			PUSH_TREE (t1, VAL_POINTER);			
			break;
		}
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: {
			int n = (*ip) - CEE_STLOC_0;
			++ip;
			--sp;

			t1 = ctree_create_store (mp, MB_TERM_ADDR_L, *sp, LOCAL_TYPE (n), 
						 (gpointer)LOCAL_POS (n));

			ADD_TREE (t1);			
			break;
		}
		case CEE_STLOC_S: {
			++ip;
			--sp;

			t1 = ctree_create_store (mp, MB_TERM_ADDR_L, *sp, LOCAL_TYPE (*ip), 
						 (gpointer)LOCAL_POS (*ip));
			++ip;

			ADD_TREE (t1);			
			break;
		}

		MAKE_BI_ALU (ADD)
		MAKE_BI_ALU (SUB)
		MAKE_BI_ALU (AND)
		MAKE_BI_ALU (OR)
		MAKE_BI_ALU (XOR)
		MAKE_BI_ALU (SHL)
		MAKE_BI_ALU (SHR)
		MAKE_BI_ALU (SHR_UN)
		MAKE_BI_ALU (MUL)
		MAKE_BI_ALU (DIV)
		MAKE_BI_ALU (DIV_UN)
		MAKE_BI_ALU (REM)
		MAKE_BI_ALU (REM_UN)

		MAKE_LDIND (LDIND_I1,  MB_TERM_LDIND_I1, VAL_I32)
		MAKE_LDIND (LDIND_U1,  MB_TERM_LDIND_U1, VAL_I32)
		MAKE_LDIND (LDIND_I2,  MB_TERM_LDIND_I2, VAL_I32)
		MAKE_LDIND (LDIND_U2,  MB_TERM_LDIND_U2, VAL_I32)
		MAKE_LDIND (LDIND_I,   MB_TERM_LDIND_I4, VAL_I32)
		MAKE_LDIND (LDIND_I4,  MB_TERM_LDIND_I4, VAL_I32)
		MAKE_LDIND (LDIND_REF, MB_TERM_LDIND_U4, VAL_I32)
		MAKE_LDIND (LDIND_U4,  MB_TERM_LDIND_U4, VAL_I32)
		MAKE_LDIND (LDIND_I8,  MB_TERM_LDIND_I8, VAL_I64)
		MAKE_LDIND (LDIND_R4,  MB_TERM_LDIND_R4, VAL_DOUBLE)
		MAKE_LDIND (LDIND_R8,  MB_TERM_LDIND_R8, VAL_DOUBLE)

		MAKE_STIND (STIND_I1,  MB_TERM_STIND_I1)
		MAKE_STIND (STIND_I2,  MB_TERM_STIND_I2)
		MAKE_STIND (STIND_I,   MB_TERM_STIND_I4)
		MAKE_STIND (STIND_I4,  MB_TERM_STIND_I4)
		MAKE_STIND (STIND_I8,  MB_TERM_STIND_I8)
		MAKE_STIND (STIND_R4,  MB_TERM_STIND_R4)
		MAKE_STIND (STIND_R8,  MB_TERM_STIND_R8)
		MAKE_STIND (STIND_REF, MB_TERM_STIND_I4)

		MAKE_LDELEM (LDELEM_I1,  MB_TERM_LDIND_I1, VAL_I32, 1)
		MAKE_LDELEM (LDELEM_U1,  MB_TERM_LDIND_U1, VAL_I32, 1)
		MAKE_LDELEM (LDELEM_I2,  MB_TERM_LDIND_I2, VAL_I32, 2)
		MAKE_LDELEM (LDELEM_U2,  MB_TERM_LDIND_U2, VAL_I32, 2)
		MAKE_LDELEM (LDELEM_I,   MB_TERM_LDIND_I4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_I4,  MB_TERM_LDIND_I4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_REF, MB_TERM_LDIND_U4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_U4,  MB_TERM_LDIND_U4, VAL_I32, 4)
		MAKE_LDELEM (LDELEM_I8,  MB_TERM_LDIND_I8, VAL_I64, 8)
		MAKE_LDELEM (LDELEM_R4,  MB_TERM_LDIND_R4, VAL_DOUBLE, 4)
		MAKE_LDELEM (LDELEM_R8,  MB_TERM_LDIND_R8, VAL_DOUBLE, 8)

		MAKE_STELEM (STELEM_I1,  MB_TERM_STIND_I1, 1)
		MAKE_STELEM (STELEM_I2,  MB_TERM_STIND_I2, 2)
		MAKE_STELEM (STELEM_I4,  MB_TERM_STIND_I4, 4)
		MAKE_STELEM (STELEM_I,   MB_TERM_STIND_I4, 4)
		MAKE_STELEM (STELEM_REF, MB_TERM_STIND_I4, 4)
		MAKE_STELEM (STELEM_I8,  MB_TERM_STIND_I8, 8)
		MAKE_STELEM (STELEM_R4,  MB_TERM_STIND_R4, 4)
		MAKE_STELEM (STELEM_R8,  MB_TERM_STIND_R8, 8)

		case CEE_NEG: {
			ip++;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_NEG, sp [0], NULL);
			PUSH_TREE (t1, sp [0]->svt);		
			break;
		}
		case CEE_NOT: {
			ip++;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_NOT, sp [0], NULL);
			PUSH_TREE (t1, sp [0]->svt);
			break;
		}
	        case CEE_BR_S: {
			gint32 target;

			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			target = cli_addr + 2 + (signed char) *ip;

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			MARK_REACHED (tbb);
			t1->data.p = tbb;

			ADD_TREE (t1);
			++ip;
			superblock_end = TRUE;
			break;
		}
		case CEE_BR: {
			gint32 target;

			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			target = cli_addr + 5 + (gint32) read32(ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			MARK_REACHED (tbb);
			t1->data.p = tbb;

			ADD_TREE (t1);
			ip += 4;
			superblock_end = TRUE;
			break;
		}

		MAKE_CJUMP(BGT)
		MAKE_CJUMP(BGT_UN)
		MAKE_CJUMP(BLT)
		MAKE_CJUMP(BLT_UN)
		MAKE_CJUMP(BNE_UN)
		MAKE_CJUMP(BEQ)
		MAKE_CJUMP(BGE)
		MAKE_CJUMP(BGE_UN)
		MAKE_CJUMP(BLE)
		MAKE_CJUMP(BLE_UN)

		case CEE_BRTRUE:
		case CEE_BRTRUE_S: {
			gint32 target;
			int near_jump = *ip == CEE_BRTRUE_S;
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_BRTRUE, sp [0], NULL);

			if (near_jump)
				target = cli_addr + 2 + (signed char) *ip;
			else 
				target = cli_addr + 5 + (gint32) read32 (ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			MARK_REACHED (tbb);
			t1->data.p = tbb;

			ip += near_jump ? 1: 4;
			ADD_TREE (t1);
			break;
		}
		case CEE_BRFALSE:
		case CEE_BRFALSE_S: {
			gint32 target;
			int near_jump = *ip == CEE_BRFALSE_S;
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_BRFALSE, sp [0], NULL);

			if (near_jump)
				target = cli_addr + 2 + (signed char) *ip;
			else 
				target = cli_addr + 5 + (gint32) read32 (ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			MARK_REACHED (tbb);
			t1->data.p = tbb;

			ip += near_jump ? 1: 4;
			ADD_TREE (t1);

			if ((tbb->indepth = sp - stack)) {
				
				printf ("DEPTH %d\n", tbb->indepth);
				g_assert_not_reached ();
			}


			break;
		}
		case CEE_RET: {
			ip++;

			if (signature->ret->type != MONO_TYPE_VOID) {
				--sp;
				t1 = mono_ctree_new (mp, MB_TERM_RETV, *sp, NULL);
			} else {
				t1 = mono_ctree_new (mp, MB_TERM_RET, NULL, NULL);
			}

			t1->last_instr = (ip == end);

			ADD_TREE (t1);

			if (sp > stack) {
				g_warning ("more values on stack at IL_%04x: %d",  ip - header->code, sp - stack);
				mono_print_ctree (sp [-1]);
				printf ("\n");
			}
			superblock_end = TRUE;
			break;
		}
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int n = (*ip) - CEE_LDARG_0;
			++ip;

			t1 = ctree_create_load (mp, MB_TERM_ADDR_L, ARG_TYPE (n), 
						(gpointer)ARG_POS (n), &svt);
			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDARG_S: {
			++ip;

			t1 = ctree_create_load (mp, MB_TERM_ADDR_L, ARG_TYPE (*ip), 
						(gpointer)ARG_POS (*ip), &svt);
			PUSH_TREE (t1, svt);
			++ip;
			break;
		}
		case CEE_DUP: {
			++ip; 
			sp--;
			/* fixme: IMO we can use the temp. variable associated
			 * with the current slot instead of -1 
			 */
			if (t2 = mono_store_tree (cfg, -1, *sp, &t1))
				ADD_TREE (t2);

			PUSH_TREE (t1, t1->svt);
			t1 = ctree_create_dup (mp, t1);		
			PUSH_TREE (t1, t1->svt);

			break;
		}
		case CEE_POP: {
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_POP, *sp, NULL);
			ADD_TREE (t1);

			break;
		}
		case CEE_CONV_U1: 
		case CEE_CONV_I1: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		}
		case CEE_CONV_U2: 
		case CEE_CONV_I2: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		}
		case CEE_CONV_I: 
		case CEE_CONV_U: 
		case CEE_CONV_U4: 
		case CEE_CONV_I4: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		}
		case CEE_CONV_I8: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I8, *sp, NULL);
			PUSH_TREE (t1, VAL_I64);		
			break;
		}
		case CEE_CONV_R8: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_R8, *sp, NULL);
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		}
		case 0xFE: {
			++ip;			
			switch (*ip) {
				
			MAKE_BI_ALU (CEQ)

			case CEE_INITOBJ: {
				MonoClass *class;
				guint32 token;
				
				++ip;
				token = read32 (ip);
				class = mono_class_get (image, token);
				ip += 4;
				sp--;
				
				t1 = mono_ctree_new (mp, MB_TERM_INITOBJ, *sp, NULL);
				t1->data.i = mono_class_value_size (class, NULL);
				ADD_TREE (t1);

				break;
			}
			case CEE_LDARG: {
				guint32 n;
				++ip;
				n = read32 (ip);
				ip += 4;

				t1 = ctree_create_load (mp, MB_TERM_ADDR_L, ARG_TYPE (n), 
							(gpointer)ARG_POS (n), &svt);
				PUSH_TREE (t1, svt);
				break;
			}
			default:
				g_error ("Unimplemented opcode at IL_%04x "
					 "0xFE %02x", ip - header->code, *ip);
			}
			break;
		}	
		default:
			g_warning ("unknown instruction `%s' at IL_%04X", 
				   opcode_names [*ip], ip - header->code);
			mono_print_forest (forest);
			g_assert_not_reached ();
		}
	}		

        if ((depth = sp - stack)) {
		g_assert_not_reached ();
	}

	                        } else 
					superblock_end = TRUE;

			} else {
				superblock_end = TRUE;
				printf ("unreached block %d\n", i);
				repeat = TRUE;
				g_assert (repeat_count < 10);
			}
				//printf ("BBE %d %d %d %d\n", i, bb->reached, bb->finished, superblock_end);
		}

		repeat_count++;
		//printf ("REPEAT %d\n", repeat);


	} while (repeat);

	//printf ("FINISHED\n");
}

/* this function is never called */
static void 
ves_array_set (MonoArray *this, ...)
{
	g_assert_not_reached ();
}

/* this function is never called */
static void 
ves_array_get (MonoArray *this, ...)
{
	g_assert_not_reached ();
}
	
/**
 * mono_jit_assembly:
 * @assembly: reference to an assembly
 *
 * JIT compilation of all methods in the assembly. Prints debugging
 * information on stdout.
 */
static void
mono_jit_assembly (MonoAssembly *assembly)
{
	MonoImage *image = assembly->image;
	MonoMethod *method;
	MonoTableInfo *t = &image->tables [MONO_TABLE_METHOD];
	int i;

	for (i = 0; i < t->rows; i++) {

		method = mono_get_method (image, 
					  (MONO_TABLE_METHOD << 24) | (i + 1), 
					  NULL);

		printf ("\nMethod: %s\n\n", method->name);

		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT)
			printf ("ABSTARCT\n");
		else
			arch_compile_method (method);

	}

}

typedef int (*MonoMainIntVoid) ();

/**
 * mono_jit_exec:
 * @assembly: reference to an assembly
 * @argc: argument count
 * @argv: argument vector
 *
 * Start execution of a program.
 */
static int 
mono_jit_exec (MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	MonoMethod *method;
	int res = 0;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);

	if (method->signature->param_count) {
		g_warning ("Main () with arguments not yet supported");
		exit (1);
	} else {
		MonoMainIntVoid mfunc;

		mfunc = arch_compile_method (method);
		res = mfunc ();
	}
	
	return res;
}

static void
usage (char *name)
{
	fprintf (stderr,
		 "%s %s, the Mono ECMA CLI JIT Compiler, (C) 2001 Ximian, Inc.\n\n"
		 "Usage is: %s [options] executable args...\n", name,  VERSION, name);
	fprintf (stderr,
		 "Valid Options are:\n"
		 "-d            debug the jit, show disassembler output.\n"
		 "--dump-asm    dumps the assembly code generated\n"
		 "--dump-forest dumps the reconstructed forest\n"
		 "--trace-calls printf function call trace\n"
		 "--help        print this help message\n");
	exit (1);
}

int 
main (int argc, char *argv [])
{
	MonoAssembly *assembly;
	int retval = 0, i;
	char *file;
	gboolean testjit = FALSE;

	if (argc < 2)
		usage (argv [0]);

	for (i = 1; i < argc && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--help") == 0) {
			usage (argv [0]);
		} else if (strcmp (argv [i], "-d") == 0) {
			testjit = TRUE;
			mono_jit_dump_asm = TRUE;
			mono_jit_dump_forest = TRUE;
		} else if (strcmp (argv [i], "--dump-asm") == 0)
			mono_jit_dump_asm = TRUE;
		else if (strcmp (argv [i], "--dump-forest") == 0)
			mono_jit_dump_forest = TRUE;
		else if (strcmp (argv [i], "--trace-calls") == 0)
			mono_jit_trace_calls = TRUE;
		else
			usage (argv [0]);
	}
	
	file = argv [i];

	if (!file)
		usage (argv [0]);

	mono_init ();
	mono_init_icall ();
	mono_add_internal_call ("__array_Set", ves_array_set);
	mono_add_internal_call ("__array_Get", ves_array_get);

	assembly = mono_assembly_open (file, NULL, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}

	if (testjit) {
		mono_jit_assembly (assembly);
	} else {
		retval = mono_jit_exec (assembly, argc, argv);
		printf ("RESULT: %d\n", retval);
	}

	mono_assembly_close (assembly);

	return retval;
}



