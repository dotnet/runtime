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
#include <unistd.h>

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
#include "debug.h"

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

#define SET_VARINFO(vi,t,k,o,s)  do { vi.type=t; vi.kind=k; vi.offset=o; vi.size=s; } while (0)

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
	create_outstack (cfg, bb, stack, sp - stack);                         \
	mark_reached (cfg, tbb, bb->outstack, bb->outdepth);                  \
	t1->data.p = tbb;                                                     \
	ADD_TREE (t1, cli_addr);                                                        \
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

#define MAKE_CMP(name)                                                        \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
        g_assert (sp [0]->svt == sp [1]->svt);                                \
	PUSH_TREE (t1, VAL_I32);                                              \
	break;                                                                \
}

#define MAKE_SPILLED_BI_ALU(name)                                             \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
        g_assert (sp [0]->svt == sp [1]->svt);                                \
        t1->svt = sp [0]->svt;                                                \
        t1 = mono_store_tree (cfg, -1, t1, &t2);                              \
        g_assert (t1);                                                        \
        ADD_TREE (t1, cli_addr);                                                        \
	PUSH_TREE (t2, t2->svt);                                              \
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
	ADD_TREE (t1, cli_addr);                                                        \
	break;                                                                \
}

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
	ADD_TREE (t1, cli_addr);                                                        \
	break;                                                                \
}
	
/* Whether to dump the assembly code after genreating it */
gboolean mono_jit_dump_asm = FALSE;

/* Whether to dump the forest */
gboolean mono_jit_dump_forest = FALSE;

/* Whether to print function call traces */
gboolean mono_jit_trace_calls = FALSE;

MonoDebugHandle *mono_debug_handle = NULL;
GList *mono_debug_methods = NULL;

gpointer mono_end_of_stack = NULL;

MonoJitInfoTable *mono_jit_info_table = NULL;

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

MonoJitInfoTable *
mono_jit_info_table_new ()
{
	return g_array_new (FALSE, FALSE, sizeof (gpointer));
}

int
mono_jit_info_table_index (MonoJitInfoTable *table, gpointer addr)
{
	int left = 0, right = table->len;

	while (left < right) {
		int pos = (left + right) / 2;
		MonoJitInfo *ji = g_array_index (table, gpointer, pos);
		gpointer start = ji->code_start;
		gpointer end = start + ji->code_size;

		if (addr < start)
			right = pos;
		else if (addr >= end) 
			left = pos + 1;
		else
			return pos;
	}

	return left;
}

MonoJitInfo *
mono_jit_info_table_find (MonoJitInfoTable *table, gpointer addr)
{
	int left = 0, right = table->len;

	while (left < right) {
		int pos = (left + right) / 2;
		MonoJitInfo *ji = g_array_index (table, gpointer, pos);
		gpointer start = ji->code_start;
		gpointer end = start + ji->code_size;

		if (addr < start)
			right = pos;
		else if (addr >= end) 
			left = pos + 1;
		else
			return ji;
	}

	return NULL;
}

void
mono_jit_info_table_add (MonoJitInfoTable *table, MonoJitInfo *ji)
{
	gpointer start = ji->code_start;
	int pos = mono_jit_info_table_index (table, start);

	//printf ("TESTADD %d %p\n", pos, ji->code_start);
	g_array_insert_val (table, pos, ji);
}

/**
 * runtime_class_init:
 * @klass: the class to initialise
 *
 * Initialise the class @klass by calling the class constructor.
 */
static void
runtime_class_init (MonoClass *klass)
{
	MonoCCtor cctor;
	MonoMethod *method;
	int i;

	if (mono_debug_handle)
		mono_debug_add_type (mono_debug_handle, klass);
	
	for (i = 0; i < klass->method.count; ++i) {
		method = klass->methods [i];
		if ((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && 
		    (strcmp (".cctor", method->name) == 0)) {
	
			cctor = arch_compile_method (method);
			if (!cctor && mono_debug_handle)
				return;
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
		if (type->data.klass->enumtype)
			return map_stind_type (type->data.klass->enum_basetype);
		else
			return MB_TERM_STIND_OBJ;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

static int
map_starg_type (MonoType *type)
{
	if (type->byref) 
		return MB_TERM_STIND_I4;

	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
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
		if (type->data.klass->enumtype)
			return map_starg_type (type->data.klass->enum_basetype);
		else
			return MB_TERM_STIND_OBJ;
	default:
		g_warning ("unknown type %02x", type->type);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return -1;
}

static int
map_arg_type (MonoType *type, gboolean pinvoke)
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
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return MB_TERM_ARG_I4;
	case MONO_TYPE_STRING:
		if (pinvoke)
			return MB_TERM_ARG_STRING;
		return MB_TERM_ARG_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_ARG_I8;
	case MONO_TYPE_R4:
		return MB_TERM_ARG_R4;
	case MONO_TYPE_R8:
		return MB_TERM_ARG_R8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype)
			return map_arg_type (type->data.klass->enum_basetype, pinvoke);
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
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return map_ldind_type (type->data.klass->enum_basetype, svt);
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

static int
map_ldarg_type (MonoType *type, MonoValueType *svt)
{
	if (type->byref) {
		*svt = VAL_POINTER;
		return MB_TERM_LDIND_I4;
	}

	switch (type->type) {
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
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return map_ldarg_type (type->data.klass->enum_basetype, svt);
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
 * map_call_type:
 * @type: the type to map
 *
 * Translates the MonoType @type into the corresponding call opcode 
 * for the code generator.
 */
static int
map_call_type (MonoType *type, MonoValueType *svt)
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
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*svt = VAL_I32;
		return MB_TERM_CALL_I4;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			return map_call_type (type->data.klass->enum_basetype, svt);
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

	switch (tree->op) {
	case MB_TERM_ADDR_L:
		printf ("[%d]", tree->data.i);
		break;
	}

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

		SET_VARINFO (vi, type, kind, - cfg->locals_size, size);
		g_array_append_val (cfg->varinfo, vi);
		break;
	}
	case MONO_ARGVAR: {
		int arg_start = 8 + cfg->has_vtarg*4;

		SET_VARINFO (vi, type, kind, cfg->args_size + arg_start, size);
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
	int size, align, vnum, pos;
	
	g_assert (type != VAL_UNKNOWN);

	/* take care if you modify MonoValueType */
	g_assert (VAL_DOUBLE == 4);

	/* fixme: machine dependant */ 
	if (type == VAL_POINTER)
		type = VAL_I32; /* VAL_I32 and VAL_POINTER share the same slot */

	pos = type - 1 + slot * VAL_DOUBLE;

	if ((vnum = cfg->intvars [pos])) 		
		return vnum;
	mono_get_val_sizes (type, &size, &align);

	cfg->intvars[pos] = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, type);

	return cfg->intvars[pos];
}

static int
mono_allocate_excvar (MonoFlowGraph *cfg)
{
	if (cfg->excvar)
		return cfg->excvar;

	cfg->excvar = arch_allocate_var (cfg, 4, 4, MONO_TEMPVAR, VAL_POINTER);

	return cfg->excvar;
}


/**
 * ctree_create_load:
 * @cfg: pointer to the control flow graph
 * @type: the type of the value to load
 * @addr: the address of the value
 *
 * Creates a tree to load the value at address @addr.
 */
inline static MBTree *
ctree_create_load (MonoFlowGraph *cfg, MonoType *type, MBTree *addr, MonoValueType *svt, gboolean arg)
{
	MonoMemPool *mp = cfg->mp;
	int ldind;
	MBTree *t;

	if (arg)
		ldind = map_ldarg_type (type, svt);
	else
		ldind = map_ldind_type (type, svt);

	t = mono_ctree_new (mp, ldind, addr, NULL);

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
ctree_create_store (MonoFlowGraph *cfg, MonoType *type, MBTree *addr, 
		    MBTree *s, gboolean arg)
{
	MonoMemPool *mp = cfg->mp;
	int stind; 
	MBTree *t;
	
	if (arg)
		stind = map_starg_type (type);
	else
		stind = map_stind_type (type);

	t = mono_ctree_new (mp, stind, addr, s);

	if (!type->byref && type->type == MONO_TYPE_VALUETYPE)	
		t->data.i = mono_class_value_size (type->data.klass, NULL);
	
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
	int vnum = 0;

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
		if (slot >= 0) {
			vnum = mono_allocate_intvar (cfg, slot, s->svt);

			if (s->left->op == MB_TERM_ADDR_L && s->left->data.i == vnum) {
				if (dup)
					*dup = ctree_create_dup (mp, s);
				return NULL;
			}
			// fall through
		} else {
			if (dup)
				*dup = ctree_create_dup (mp, s);
			return NULL;
		}
	}			
	default: {
			g_assert (s->svt != VAL_UNKNOWN);

			if (slot >= 0) {
				vnum = mono_allocate_intvar (cfg, slot, s->svt);
			} else {
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

	g_assert (((MonoMethodNormal *)method)->header);

	cfg = mono_mempool_alloc0 (mp, sizeof (MonoFlowGraph));

	cfg->method = method;
	cfg->mp = mp;

	cfg->varinfo = g_array_new (FALSE, TRUE, sizeof (MonoVarInfo));
	
	SET_VARINFO (vi, 0, 0, 0, 0);
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


static void
runtime_object_init (MonoObject *obj)
{
	MonoClass *klass = obj->klass;
	MonoMethod *method = NULL;
	void (*ctor) (gpointer this);
	int i;

	for (i = 0; i < klass->method.count; ++i) {
		if (!strcmp (".ctor", klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == 0) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method);

	ctor = arch_compile_method (method);
	ctor (obj);
}

static MonoBBlock *
mono_find_final_block (MonoFlowGraph *cfg, guint32 ip, int type)
{
	MonoMethod *method = cfg->method;
	MonoBytecodeInfo *bcinfo = cfg->bcinfo;
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	MonoExceptionClause *clause;
	int i;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_HANDLER (clause, ip))
			return NULL;

		if (MONO_OFFSET_IN_CLAUSE (clause, ip)) {
			if (clause->flags & type) {
				g_assert (bcinfo [clause->handler_offset].is_block_start);
				return &cfg->bblocks [bcinfo [clause->handler_offset].block_id];
			} else
				return NULL;
		}
	}
	return NULL;
}

#define CREATE_BLOCK(t) {if (!bcinfo [t].is_block_start) {block_count++;bcinfo [t].is_block_start = 1; }}

void
mono_analyze_flow (MonoFlowGraph *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMemPool *mp = cfg->mp;
	register const unsigned char *ip, *end;
	MonoMethodHeader *header;
	MonoBytecodeInfo *bcinfo;
	MonoExceptionClause *clause;
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
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		CREATE_BLOCK (clause->try_offset);
		CREATE_BLOCK (clause->handler_offset);
	}

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
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_U4_UN:
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U8_UN:
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_U4:
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
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
		case CEE_ENDFINALLY:
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
		case CEE_STOBJ:
		case CEE_LDELEMA:
		case CEE_NEWOBJ:
		case CEE_CPOBJ:
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
		case CEE_LDARGA_S: 
		case CEE_STARG_S:
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
				CREATE_BLOCK (target);			
			}
			break;
		}
		case 0xFE: {
			++ip;			
			switch (*ip) {
				
			case CEE_CEQ:
			case CEE_CLT:
				ip++;
				break;
			case CEE_LDARG:
			case CEE_INITOBJ:
			case CEE_LDFTN:
				ip +=5;
				break;
			case CEE_ENDFILTER:
			case CEE_RETHROW:
				ip++;
				block_end = 1;
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
			//g_assert_not_reached ();
			cfg->invalid = 1;
			return;
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

#define ADD_TREE(t,a)   do { t->cli_addr = a; g_ptr_array_add (forest, (t)); } while (0)
#define PUSH_TREE(t,k)  do { int tt = k; *sp = t; t->svt = tt; sp++; } while (0)

#define LOCAL_POS(n)    (1 + n)
#define LOCAL_TYPE(n)   ((header)->locals [(n)])

#define ARG_POS(n)      (firstarg + n)
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])

static void
create_outstack (MonoFlowGraph *cfg, MonoBBlock *bb, MBTree **stack, int depth)
{
	MonoMemPool *mp = cfg->mp;
	MBTree **c = stack, *t1, *t2;
	GPtrArray *forest = bb->forest;
	int i;

	g_assert (bb->reached);

	if (depth <= 0)
		return;

	if (bb->outstack) {
		g_assert (bb->outdepth == depth);
		return;
	}

	bb->outdepth = depth;
	bb->outstack = mono_mempool_alloc (mp, depth * sizeof (MBTree *));
	
	for (i = 0; i < depth; i++) {
		if ((t1 = mono_store_tree (cfg, i, c [i], &t2)))
			ADD_TREE (t1, -1);
		bb->outstack [i] = t2;
	}
}

static void
mark_reached (MonoFlowGraph *cfg, MonoBBlock *target, MBTree **stack, int depth)
{
	MonoMemPool *mp = cfg->mp;
	int i;

	if (target->reached)
		return;

	target->reached = 1;

	if (depth == 0)
		return;

	g_assert (stack);

	if (target->instack) {
		g_assert (target->indepth == depth);
		return;
	}

	target->indepth = depth;
	target->instack = mono_mempool_alloc (mp, depth * sizeof (MBTree *));
	
	for (i = 0; i < depth; i++) {
		target->instack [i] = ctree_create_dup (mp, stack [i]);
	}
	
	
}

#define MARK_REACHED(bb) do { if (!bb->reached) { bb->reached = 1; }} while (0)

/**
 * mono_create_cfg:
 * @cfg: control flow graph
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
	int varnum = 0, firstarg = 0, retvtarg = 0;
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
			varnum = arch_allocate_var (cfg, size, align, MONO_LOCALVAR, VAL_UNKNOWN);
			if (i == 0)
				cfg->locals_start_index = varnum;
		}
	}

	if (signature->ret->type == MONO_TYPE_VALUETYPE) {
		int size, align;

		// fixme: maybe we must add this check to the above if statement
		g_assert (!signature->ret->byref);

		cfg->has_vtarg = 1;

		size = mono_type_size (signature->ret, &align);
		
		retvtarg = varnum = arch_allocate_var (cfg, size, align, MONO_LOCALVAR, VAL_UNKNOWN);
		
		//printf ("VALUETYPE METHOD %s.%s::%s %d\n", method->klass->name_space, 
		//method->klass->name, method->name, size);
	}
	
	cfg->args_start_index = firstarg = varnum + 1;
 
	if (signature->hasthis) {
		arch_allocate_var (cfg, sizeof (gpointer), sizeof (gpointer), MONO_ARGVAR, VAL_POINTER);
	}

	if (signature->param_count) {
		int align, size;

		for (i = 0; i < signature->param_count; ++i) {
			size = mono_type_size (signature->params [i], &align);
			if (size < 4) {
				size = 4; 
				align = 4;
			}
			arch_allocate_var (cfg, size, align, MONO_ARGVAR, VAL_UNKNOWN);
		}
	}

	for (i = 0; i < header->num_clauses; ++i) {
		MonoExceptionClause *clause = &header->clauses [i];		
		tbb = &cfg->bblocks [bcinfo [clause->handler_offset].block_id];
		if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) {
			tbb->instack = mono_mempool_alloc (mp, sizeof (MBTree *));
			tbb->indepth = 1;
			tbb->instack [0] = t1 = mono_ctree_new_leaf (mp, MB_TERM_EXCEPTION);
			t1->data.i = mono_allocate_excvar (cfg);
			t1->svt = VAL_POINTER;
			tbb->reached = 1;
		} else if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
			mark_reached (cfg, tbb, NULL, 0);
		} else {
			g_warning ("implement me");
			g_assert_not_reached ();
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
			
			//printf ("BBS %d %05x %05x %d %d %d %s\n", i, bb->cli_addr, bb->cli_addr + bb->length, bb->reached, bb->finished, superblock_end, method->name);
			
			if (!bb->reached && !superblock_end) {
				MonoBBlock *sbb = &cfg->bblocks [i - 1];

				g_assert (sbb->outdepth == (sp - stack));

				mark_reached (cfg, bb, sbb->outstack, sbb->outdepth);
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
			
			t1 = mono_ctree_new (mp, MB_TERM_THROW, *sp, NULL);
			ADD_TREE (t1, cli_addr);		
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
			ADD_TREE (t1, cli_addr);

			t1 = ctree_create_dup (mp, t3);
			t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t2->data.i = sizeof (MonoObject);
			t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);

			t1 = ctree_create_store (cfg, &c->byval_arg, t1, *sp, FALSE);
			ADD_TREE (t1, cli_addr);

			PUSH_TREE (t3, VAL_POINTER);

			break;
		}
		case CEE_UNBOX: {
			MonoClass *class;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			class = mono_class_get (image, token);
			t1 = mono_ctree_new (mp, MB_TERM_UNBOX, *sp, NULL);
			t1->data.klass = class;

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

			++ip;
			token = read32 (ip);
			ip += 4;
			sp--;

			c = mono_class_get (image, token);
			g_assert (c->valuetype);

			t1 = ctree_create_load (cfg, &c->byval_arg, *sp, &svt, FALSE);
			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_STOBJ: {
			guint32 token;
			MonoClass *c;
			int size;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			c = mono_class_get (image, token);
			g_assert (c->valuetype);

			size = mono_class_value_size (c, NULL);
			
			t1 = mono_ctree_new (mp, MB_TERM_STIND_OBJ, sp [0], sp [1]);
			t1->data.i = size;
			ADD_TREE (t1, cli_addr);
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

			mono_class_init (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			addr = MONO_CLASS_STATIC_FIELDS_BASE (klass) + field->offset;

			if (load_addr) {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = addr;
				svt = VAL_POINTER;
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = addr;
				t1 = ctree_create_load (cfg, field->type, t1, &svt, FALSE);
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

			mono_class_init (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			
			if (klass->valuetype)
				t1->data.i = field->offset - sizeof (MonoObject);
			else 
				t1->data.i = field->offset;

			t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);

			if (!load_addr)
				t1 = ctree_create_load (cfg, field->type, t1, &svt, FALSE);
			else
				svt = VAL_POINTER;

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_STSFLD: {
			MonoClass *klass;
			MonoClassField *field;
			guint32 token;

			++ip;
			token = read32 (ip);
			ip += 4;
			--sp;

			/* need to handle fieldrefs */
			klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 
			        mono_metadata_typedef_from_field (image, token & 0xffffff));

			mono_class_init (klass);
	
			field = mono_class_get_field (klass, token);
			g_assert (field);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
			t1->data.p = MONO_CLASS_STATIC_FIELDS_BASE (klass) + field->offset;
			t1 = ctree_create_store (cfg, field->type, t1, *sp, FALSE);

			ADD_TREE (t1, cli_addr);
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

			mono_class_init (klass);

			field = mono_class_get_field (klass, token);
			g_assert (field);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			if (klass->valuetype)
				t1->data.i = field->offset - sizeof (MonoObject);
			else 
				t1->data.i = field->offset;

			//printf ("VALUETYPE %d %d %d\n", klass->valuetype, field->offset, t1->data.i);

			t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);
			t1 = ctree_create_store (cfg, field->type, t1, sp [1], FALSE);

			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_LDELEMA: {
			MonoClass *class;
			guint32 esize, token;

			++ip;
			token = read32 (ip);
			ip += 4;
			sp -= 2;

			class = mono_class_get (image, token);

			mono_class_init (class);

			esize = mono_class_instance_size (class);
			if (class->valuetype)
				esize -= sizeof (MonoObject);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = esize;
			t1 = mono_ctree_new (mp, MB_TERM_MUL, sp [1], t1);
			t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t2->data.i = G_STRUCT_OFFSET (MonoArray, vector);
			t2 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t2);
			t1 = mono_ctree_new (mp, MB_TERM_ADD, t1, t2);

			PUSH_TREE (t1, VAL_POINTER);
			break;
		}
		case CEE_NOP: { 
			++ip;
			break;
		}
		case CEE_BREAK: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BREAK);
			ADD_TREE (t1, cli_addr);
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

			create_outstack (cfg, bb, stack, sp - stack);

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
				mark_reached (cfg, tbb, stack, sp - stack);
				jt [i] = tbb; 
			}

			ADD_TREE (t1, cli_addr);
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
		case CEE_CPOBJ: {
			MonoClass *class;
			guint32 token;

			++ip;
			token = read32 (ip);
			class = mono_class_get (image, token);
			ip += 4;
			sp -= 2;

			t1 = mono_ctree_new (mp, MB_TERM_CPOBJ, sp [0], sp [1]);
			ADD_TREE (t1, cli_addr);
			
			break;
		}
		case CEE_NEWOBJ: {
			MonoMethodSignature *csig;
			MethodCallInfo *ci;
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
			
			ci =  mono_mempool_alloc0 (mp, sizeof (MethodCallInfo));
			ci->m = cm;

			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			g_assert (csig->hasthis);
			
			arg_sp = sp -= csig->param_count;

			if (!cm->klass->inited)
				mono_class_init (cm->klass);

			if (cm->klass->parent == mono_defaults.array_class) {
				newarr = TRUE;
				this = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				this->data.m = cm;
			} else {				
				if (cm->klass->valuetype) {
					this = mono_ctree_new_leaf (mp, MB_TERM_NEWSTRUCT);
					this->data.i =  mono_class_value_size (cm->klass, NULL);
				} else {
					this = mono_ctree_new_leaf (mp, MB_TERM_NEWOBJ);
					this->data.klass = cm->klass;
				}

				this->svt = VAL_POINTER;

				t1 = mono_store_tree (cfg, -1, this, &this);
				g_assert (t1);
				ADD_TREE (t1, cli_addr);
			}

			for (i = csig->param_count - 1; i >= 0; i--) {
				MonoType *type = cm->signature->params [i];
				t1 = mono_ctree_new (mp, map_arg_type (type, FALSE), arg_sp [i], NULL);	
				size = mono_type_size (type, &align);
				t1->data.i = size;
				ADD_TREE (t1, cli_addr);
				args_size += (size + 3) & ~3;
			}

			args_size += sizeof (gpointer); /* this argument */		
			ci->args_size = args_size;

			if (newarr) {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = mono_array_new_va;

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, this, t2);
				t1->data.p = ci;
				t1->svt = VAL_POINTER;

				t1 = mono_store_tree (cfg, -1, t1, &t2);
				g_assert (t1);
				ADD_TREE (t1, cli_addr);
				PUSH_TREE (t2, t2->svt);

			} else {
				
				if (!cm->addr)
					cm->addr = arch_create_simple_jit_trampoline (cm);

				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t2->data.p = (char *)cm + G_STRUCT_OFFSET (MonoMethod, addr);
				t2 = mono_ctree_new (mp, MB_TERM_LDIND_I4, t2, NULL);

				t1 = mono_ctree_new (mp, map_call_type (csig->ret, &svt), this, t2);
				t1->data.p = ci;
				t1->svt = svt;

				ADD_TREE (t1, cli_addr); 
				t1 = ctree_create_dup (mp, this);	
				PUSH_TREE (t1, t1->svt);

			}
			break;
		}
		case CEE_CALL: 
		case CEE_CALLVIRT: {
			MonoMethodSignature *csig;
			MethodCallInfo *ci;
			MonoMethod *cm;
			MBTree *this = NULL;
			guint32 token;
			int i, align, size, args_size = 0;
			int virtual = *ip == CEE_CALLVIRT;
			gboolean array_set = FALSE;
			gboolean array_get = FALSE;
			gboolean pinvoke = FALSE;
			int nargs, vtype_num = 0;

			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			g_assert (cm);

			ci =  mono_mempool_alloc0 (mp, sizeof (MethodCallInfo));
			ci->m = cm;

			if ((cm->flags &  METHOD_ATTRIBUTE_PINVOKE_IMPL) &&
			    !(cm->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL))
				pinvoke = TRUE;

			if ((cm->flags & METHOD_ATTRIBUTE_FINAL) ||
			    !(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
				virtual = 0;

			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			g_assert (!virtual || csig->hasthis);

			/* fixme: we need to unbox the this pointer for value types */
			g_assert (!virtual || !cm->klass->valuetype);

			nargs = csig->param_count;
			arg_sp = sp -= nargs;
			
			if (cm->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
				if (cm->klass->parent == mono_defaults.array_class) {
					if (!strcmp (cm->name, "Set")) { 
						array_set = TRUE;
						nargs--;
					} else if (!strcmp (cm->name, "Get")) 
						array_get = TRUE;
				}
			}

			for (i = nargs - 1; i >= 0; i--) {
				MonoType *type = cm->signature->params [i];
				t1 = mono_ctree_new (mp, map_arg_type (type, pinvoke), arg_sp [i], NULL);
				size = mono_type_size (type, &align);
				t1->data.i = size;
				ADD_TREE (t1, cli_addr);
				args_size += (size + 3) & ~3;

				// fixme: align value type arguments  to 8 byte boundary on the stack
			}

			if (csig->hasthis) {
				this = *(--sp);				
				args_size += sizeof (gpointer);
			} else
				this = mono_ctree_new_leaf (mp, MB_TERM_NOP);

			if (csig->ret->type == MONO_TYPE_VALUETYPE) {
				int size, align;
				size = mono_type_size (csig->ret, &align);
				vtype_num = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, VAL_UNKNOWN);
			}

			ci->args_size = args_size;
			ci->vtype_num = vtype_num;

			if (array_get) {
				int size, align, vnum;
				
				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = ves_array_element_address;

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, this, t2);
				t1->data.p = ci;

				t1 = mono_ctree_new (mp, map_ldind_type (csig->ret, &svt), t1, NULL);
				t1->svt = svt;		

				mono_get_val_sizes (t1->svt, &size, &align);
				vnum = arch_allocate_var (cfg, size, align, MONO_TEMPVAR, svt);

				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t2->data.i = vnum;
				t1 = mono_ctree_new (mp, map_store_svt_type (svt), t2, t1);
				t1->svt = svt;

				ADD_TREE (t1, cli_addr);
				t1 = ctree_create_dup (mp, t1);
				PUSH_TREE (t1, t1->svt);

			} else if (array_set) {

				t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
				t2->data.p = ves_array_element_address;

				t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, this, t2);
				t1->data.p = ci;

				t1 = ctree_create_store (cfg, csig->params [nargs], t1, arg_sp [nargs], FALSE);
				ADD_TREE (t1, cli_addr);
			
			} else {

				if (virtual) {
					mono_class_init (cm->klass);

					if (cm->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
						t2 = mono_ctree_new_leaf (mp, MB_TERM_INTF_ADDR);
					else 
						t2 = mono_ctree_new_leaf (mp, MB_TERM_VFUNC_ADDR);
	 
					t2->data.m = cm;

				} else {
				
					if (!cm->addr)
						cm->addr = arch_create_simple_jit_trampoline (cm);
				
					t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
					t2->data.p = (char *)cm + G_STRUCT_OFFSET (MonoMethod, addr);
					t2 = mono_ctree_new (mp, MB_TERM_LDIND_I4, t2, NULL);
				}

				t1 = mono_ctree_new (mp, map_call_type (csig->ret, &svt), this, t2);
				t1->data.p = ci;
				t1->svt = svt;

				if (csig->ret->type != MONO_TYPE_VOID) {

					if (vtype_num) {
						ADD_TREE (t1, cli_addr);
						t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
						t1->data.i = vtype_num;
						PUSH_TREE (t1, VAL_POINTER); 
					} else {
						t1 = mono_store_tree (cfg, -1, t1, &t2);
						g_assert (t1);
						ADD_TREE (t1, cli_addr);
						PUSH_TREE (t2, t2->svt);
					}
				} else
					ADD_TREE (t1, cli_addr);
   
			}

			break;
		}
		case CEE_ISINST: {
			MonoClass *c;
			guint32 token;
			++ip;
			token = read32 (ip);
			--sp;

			c = mono_class_get (image, token);

			t1 = mono_ctree_new (mp, MB_TERM_ISINST, *sp, NULL);
			t1->data.klass = c;
			
			PUSH_TREE (t1, VAL_I32);

			ip += 4;
			break;
		}
		case CEE_CASTCLASS: {
			MonoClass *c;
			guint32 token;
			++ip;
			token = read32 (ip);
			--sp;

			c = mono_class_get (image, token);

			t1 = mono_ctree_new (mp, MB_TERM_CASTCLASS, *sp, NULL);
			t1->data.klass = c;
			
			PUSH_TREE (t1, VAL_I32);

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

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (n);
			t1 = ctree_create_load (cfg, LOCAL_TYPE (n), t1, &svt, FALSE);

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDLOC_S: {
			++ip;
			
			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (*ip);
			t1 = ctree_create_load (cfg, LOCAL_TYPE (*ip), t1, &svt, FALSE);
			++ip;

			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDLOCA_S: {
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (*ip);
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

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (n);
			t1 = ctree_create_store (cfg, LOCAL_TYPE (n), t1, *sp, FALSE);

			ADD_TREE (t1, cli_addr);			
			break;
		}
		case CEE_STLOC_S: {
			++ip;
			--sp;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = LOCAL_POS (*ip);
			t1 = ctree_create_store (cfg, LOCAL_TYPE (*ip), t1, *sp, FALSE);

			++ip;
			ADD_TREE (t1, cli_addr);			
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
		MAKE_SPILLED_BI_ALU (DIV)
		MAKE_SPILLED_BI_ALU (DIV_UN)
		MAKE_SPILLED_BI_ALU (REM)
		MAKE_SPILLED_BI_ALU (REM_UN)

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
		case CEE_BR: 
	        case CEE_BR_S: {
			gint32 target;
			int br_s = (*ip == CEE_BR_S);

			++ip;
			if (br_s)
				target = cli_addr + 2 + (signed char) *ip;
			else
				target = cli_addr + 5 + (gint32) read32(ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			create_outstack (cfg, bb, stack, sp - stack);
			mark_reached (cfg, tbb, bb->outstack, bb->outdepth);

			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			t1->data.p = tbb;
			ADD_TREE (t1, cli_addr);

			if (br_s)
				++ip;
			else
				ip += 4;

			superblock_end = TRUE;
			break;
		}
	        case CEE_LEAVE:
	        case CEE_LEAVE_S: {
			gint32 target;
			MonoBBlock *hb;
			int leave_s = (*ip == CEE_LEAVE_S);

			++ip;
			if (leave_s)
				target = cli_addr + 2 + (signed char) *ip;
			else
				target = cli_addr + 5 + (gint32) read32(ip);

			g_assert (target >= 0 && target <= header->code_size);
			g_assert (bcinfo [target].is_block_start);
			tbb = &cfg->bblocks [bcinfo [target].block_id];
			g_assert ((sp - stack) == 0);
			mark_reached (cfg, tbb, NULL, 0);

			/* fixme: fault handler */

			if ((hb = mono_find_final_block (cfg, cli_addr, MONO_EXCEPTION_CLAUSE_FINALLY))) {
				mark_reached (cfg, hb, NULL, 0);
				t1 = mono_ctree_new_leaf (mp, MB_TERM_HANDLER);
				t1->data.p = hb;
				ADD_TREE (t1, cli_addr);
			}

			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			t1->data.p = tbb;
			ADD_TREE (t1, cli_addr);
			
			if (leave_s)
				++ip;
			else
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
			create_outstack (cfg, bb, stack, sp - stack);
			mark_reached (cfg, tbb, bb->outstack, bb->outdepth);
  
			t1->data.p = tbb;
			ip += near_jump ? 1: 4;
			ADD_TREE (t1, cli_addr);
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
			create_outstack (cfg, bb, stack, sp - stack);
			mark_reached (cfg, tbb, bb->outstack, bb->outdepth);
		    
			t1->data.p = tbb;
			ip += near_jump ? 1: 4;
			ADD_TREE (t1, cli_addr);
			break;
		}
		case CEE_RET: {
			MonoType *ret = signature->ret;

			ip++;

			if (ret->type != MONO_TYPE_VOID) {
				--sp;
				if (!ret->byref && ret->type == MONO_TYPE_VALUETYPE) {
					int align;
					t1 = mono_ctree_new (mp, MB_TERM_RET_OBJ, *sp, NULL);
					t1->data.i = mono_class_value_size (ret->data.klass, &align);
				} else {
					t1 = mono_ctree_new (mp, MB_TERM_RET, *sp, NULL);
				}
			} else {
				t1 = mono_ctree_new_leaf (mp, MB_TERM_RET_VOID);
			}

			t1->last_instr = (ip == end);

			ADD_TREE (t1, cli_addr);

			if (sp > stack) {
				g_warning ("more values on stack at IL_%04x: %d",  ip - header->code, sp - stack);
				mono_print_ctree (sp [-1]);
				printf ("\n");
			}
			superblock_end = TRUE;
			break;
		}
		case CEE_ENDFINALLY: {
			ip++;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ENDFINALLY);
			ADD_TREE (t1, cli_addr);
			t1->last_instr = FALSE;

			g_assert (sp == stack);
			superblock_end = TRUE;
			break;
		}
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int n = (*ip) - CEE_LDARG_0;
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = ARG_POS (n);
			t1 = ctree_create_load (cfg, ARG_TYPE (n), t1, &svt, TRUE);
			PUSH_TREE (t1, svt);
			break;
		}
		case CEE_LDARG_S: {
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = ARG_POS (*ip);
			t1 = ctree_create_load (cfg, ARG_TYPE (*ip), t1, &svt, TRUE);
			PUSH_TREE (t1, svt);
			++ip;
			break;
		}
		case CEE_LDARGA_S: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = ARG_POS (*ip);
			PUSH_TREE (t1, VAL_POINTER);
			++ip;
			break;
		}
		case CEE_STARG_S: {
			++ip;
			--sp;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = ARG_POS (*ip);
			t1 = ctree_create_store (cfg, ARG_TYPE (*ip), t1, *sp, TRUE);
			++ip;
			ADD_TREE (t1, cli_addr);			
			break;
		}
		case CEE_DUP: {
			int vnum;

			++ip; 
			sp--;

			vnum = mono_allocate_intvar (cfg, sp - stack, sp [0]->svt);
			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.i = vnum;
		       
			t2 = mono_ctree_new (mp, map_store_svt_type (sp [0]->svt), t1, sp [0]);
			t2->svt = sp [0]->svt;
			ADD_TREE (t2, cli_addr);

			t1 = ctree_create_dup (mp, t2);		
			PUSH_TREE (t1, t1->svt);
			t1 = ctree_create_dup (mp, t1);		
			PUSH_TREE (t1, t1->svt);

			break;
		}
		case CEE_POP: {
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_POP, *sp, NULL);
			ADD_TREE (t1, cli_addr);

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
		case CEE_CONV_U8: {
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
		case CEE_CONV_R4: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_R4, *sp, NULL);
			PUSH_TREE (t1, VAL_DOUBLE);		
			break;
		}
		case CEE_CONV_OVF_I4: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_I4_UN: /* on 32-bits the test is the same */
		case CEE_CONV_OVF_U4: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		}
		case CEE_CONV_OVF_I1: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_I1_UN: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I1_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U1: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U1, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_I2: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U2: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U2, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_I2_UN: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I2_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_U8: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_U8, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);
			break;
		}
		case CEE_CONV_OVF_U4_UN: {
			// fixme: raise exceptions ?
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I4, *sp, NULL);
			PUSH_TREE (t1, VAL_I32);		
			break;
		}
		case CEE_CONV_OVF_I8_UN: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_OVF_I8_UN, *sp, NULL);
			PUSH_TREE (t1, VAL_I64);
			break;
		}
		case 0xFE: {
			++ip;			
			switch (*ip) {
				
			MAKE_CMP (CEQ)
			MAKE_CMP (CLT)

			case CEE_RETHROW: {
				++ip;
				t1 = mono_ctree_new_leaf (mp, MB_TERM_RETHROW);
				t1->data.i = mono_allocate_excvar (cfg);
				ADD_TREE (t1, cli_addr);
				break;
			}
			case CEE_LDFTN: {
				MonoMethod *cm;
				guint32 token;
				++ip;
				token = read32 (ip);
				ip += 4;

				cm = mono_get_method (image, token, NULL);
				g_assert (cm);
				
				if (!cm->addr)
					cm->addr = arch_create_simple_jit_trampoline (cm);

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t1->data.p = (char *)cm + G_STRUCT_OFFSET (MonoMethod, addr);
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_I4, t1, NULL);
				PUSH_TREE (t1, VAL_POINTER);
				break;
			}
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
				ADD_TREE (t1, cli_addr);

				break;
			}
			case CEE_LDARG: {
				guint32 n;
				++ip;
				n = read32 (ip);
				ip += 4;

				t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
				t1->data.i = ARG_POS (n);
				t1 = ctree_create_load (cfg, ARG_TYPE (n), t1, &svt, TRUE);
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
			if (mono_debug_handle) {
				cfg->invalid = 1;
				return;
			}
			mono_print_forest (forest);
			g_assert_not_reached ();
		}
	}		

        if ((depth = sp - stack)) {
		//printf ("DEPTH %d %d\n",  depth, sp [0]->op);
		//mono_print_forest (forest);
		create_outstack (cfg, bb, stack, sp - stack);
	}

	                        } else 
					superblock_end = TRUE;

			} else {
				superblock_end = TRUE;
				//printf ("unreached block %d\n", i);
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
typedef int (*MonoMainIntArgs) (MonoArray* args);

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
	MonoMainIntVoid mfunc;
	int res = 0;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);

	mfunc = arch_compile_method (method);
	mono_end_of_stack = &res; /* a pointer to a local variable is always < BP */

	if (method->signature->param_count) {
		MonoArray *arr = (MonoArray*)mono_array_new (mono_defaults.string_class, argc);
		int i;
		for (i = 0; i < argc; ++i)
			mono_array_set (arr, gpointer, i, mono_string_new (argv [i]));
		res = ((MonoMainIntArgs)mfunc) (arr);
	} else {
		res = mfunc ();
	}

	if (method->signature->ret->type == MONO_TYPE_VOID)
		res = 0;
	
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
		 "--stabs       write stabs debug information\n"
		 "--debug name  insert a breakpoint at the start of method name\n"
		 "--help        print this help message\n");
	exit (1);
}

static void
sigfpe_signal_handler (int _dummy)
{
	MonoObject *exc;
	void **_p = (void **)&_dummy;
	struct sigcontext *ctx = (struct sigcontext *)++_p;

	exc = get_exception_divide_by_zero ();
	
	arch_handle_exception (ctx, exc);

	g_error ("we should never reach this code");
}

static void
sigsegv_signal_handler (int _dummy)
{
	MonoObject *exc;
	void **_p = (void **)&_dummy;
	struct sigcontext *ctx = (struct sigcontext *)++_p;

	exc = get_exception_execution_engine ();
	
	arch_handle_exception (ctx, exc);

	g_error ("we should never reach this code");
}

/**
 * mono_jit_abort:
 * @obj: exception object
 *
 * abort the program, print exception information and stack trace
 */
void
mono_jit_abort (MonoObject *obj)
{
	char *message = "";
	char *trace = NULL;
	MonoString *str; ;

	g_assert (obj);

	if (mono_object_isinst (obj, mono_defaults.exception_class)) {
		if ((str = ((MonoException *)obj)->message))
			message = mono_string_to_utf8 (str);
		if ((str = ((MonoException *)obj)->stack_trace))
			trace = mono_string_to_utf8 (str);
	}				
	
	g_warning ("unhandled exception %s.%s: \"%s\"", obj->klass->name_space, 
		   obj->klass->name, message);
       
	if (trace) {
		g_printerr (trace);
		g_printerr ("\n");
	}

	exit (1);
}

int 
main (int argc, char *argv [])
{
	struct sigaction sa;
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
		else if (strcmp (argv [i], "--debug") == 0) {
			mono_debug_methods = g_list_append (mono_debug_methods, argv [++i]);
		} else if (strcmp (argv [i], "--stabs") == 0) {
			mono_debug_handle = mono_debug_open_file ("");
		} else
			usage (argv [0]);
	}
	
	file = argv [i];

	if (!file)
		usage (argv [0]);


	/* catch SIGFPE */
	sa.sa_handler = sigfpe_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	g_assert (syscall (SYS_sigaction, SIGFPE, &sa, NULL) != -1);

	/* catch SIGSEGV */
	sa.sa_handler = sigsegv_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGSEGV, &sa, NULL) != -1);

	mono_init_icall ();
	mono_add_internal_call ("__array_Set", ves_array_set);
	mono_add_internal_call ("__array_Get", ves_array_get);

	mono_jit_info_table = mono_jit_info_table_new ();

	mono_install_runtime_class_init (runtime_class_init);
	mono_install_runtime_object_init (runtime_object_init);
	mono_init ();

	/*
	 * This doesn't seem to work... :-(
	if (mono_debug_handle)
		mono_install_trampoline (arch_compile_method);
	else*/
		mono_install_trampoline (arch_create_jit_trampoline);

	assembly = mono_assembly_open (file, NULL, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}

	if (testjit) {
		mono_jit_assembly (assembly);
	} else {
		/*
		 * skip the program name from the args.
		 */
		++i;
		retval = mono_jit_exec (assembly, argc - i, argv + i);
		printf ("RESULT: %d\n", retval);
	}

	mono_assembly_close (assembly);

	return retval;
}



