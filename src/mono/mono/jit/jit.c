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
char *opcode_names [] = {
#include "mono/cil/opcode.def"	
};
#undef OPDEF

#define MAKE_CJUMP(name)                                                      \
case CEE_##name:                                                              \
case CEE_##name##_S: {                                                        \
	int near_jump = *ip == CEE_##name##_S;                                \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
	if (near_jump)                                                        \
		t1->data.i = cli_addr + 2 + (signed char) *ip;                \
	else                                                                  \
		t1->data.i = cli_addr + 5 + (gint32) read32 (ip);             \
	t1->cli_addr = sp [0]->cli_addr;                                      \
	ADD_TREE (t1);                                                        \
	ip += near_jump ? 1: 4;		                                      \
	break;                                                                \
}

#define MAKE_BI_ALU(name)                                                     \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, MB_TERM_##name, sp [0], sp [1]);             \
	t1->cli_addr = sp [0]->cli_addr;                                      \
	PUSH_TREE (t1);                                                       \
	break;                                                                \
}

#define MAKE_LDIND(name, op)                                                  \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp--;                                                                 \
	t1 = mono_ctree_new (mp, op, *sp, NULL);                              \
	t1->cli_addr = sp [0]->cli_addr;                                      \
	PUSH_TREE (t1);                                                       \
	break;                                                                \
}
	
#define MAKE_STIND(name, op)                                                  \
case CEE_##name: {                                                            \
	++ip;                                                                 \
	sp -= 2;                                                              \
	t1 = mono_ctree_new (mp, op, sp [0], sp [1]);                         \
	t1->cli_addr = sp [0]->cli_addr;                                      \
	ADD_TREE (t1);                                                        \
	break;                                                                \
}
	
/* Whether to dump the assembly code after genreating it */
gboolean mono_jit_dump_asm = FALSE;

/* Whether to dump the forest */
gboolean mono_jit_dump_forest = FALSE;

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
		return MB_TERM_STIND_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_STIND_I8;
	case MONO_TYPE_R4:
		return MB_TERM_STIND_R4;
	case MONO_TYPE_R8:
		return MB_TERM_STIND_R8;
	case MONO_TYPE_VALUETYPE: {
		int size =  type->data.klass->instance_size - sizeof (MonoObject);

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
map_ldind_type (MonoType *type)
{
	if (type->byref)
		return MB_TERM_LDIND_I4;
       
	switch (type->type) {
	case MONO_TYPE_I1:
		return MB_TERM_LDIND_I1;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return MB_TERM_LDIND_U1;
	case MONO_TYPE_I2:
		return MB_TERM_LDIND_I2;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return MB_TERM_LDIND_U2;
	case MONO_TYPE_I:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
		return MB_TERM_LDIND_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_LDIND_I8;
	case MONO_TYPE_R4:
		return MB_TERM_LDIND_R4;
	case MONO_TYPE_R8:
		return MB_TERM_LDIND_R8;
	case MONO_TYPE_VALUETYPE: {
		int size =  type->data.klass->instance_size - sizeof (MonoObject);

		switch (size) {
		case 4:
			return MB_TERM_LDIND_I4;
		case 2:
			return MB_TERM_LDIND_U2;
		case 1:
			return MB_TERM_LDIND_U1;
		}
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
map_call_type (MonoType *type)
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
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
		return MB_TERM_CALL_I4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return MB_TERM_CALL_I8;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
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
		if (t->jump_target && t->cli_addr >= 0)
			printf ("IL%04x:", t->cli_addr);
		else
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

/**
 * ctree_create_load:
 * @mp: pointer to a memory pool
 * @addr_type: address type (MB_TERM_ADDR_L, MB_TERM_ADDR_A or MB_TERM_ADDR_G)
 * @type: the type of the value
 * @addr: the address of the value
 *
 * Creates a tree to load the value at address @addr.
 */
inline static MBTree *
ctree_create_load (MonoMemPool *mp, int addr_type, MonoType *type, gpointer addr)
{
	int ldind = map_ldind_type (type);
	MBTree *t;

	t = mono_ctree_new_leaf (mp, addr_type);
	t->data.p = addr;
	t = mono_ctree_new (mp, ldind, t, NULL);

	return t;
}

/**
 * ctree_create_store:
 * @mp: pointer to a memory pool
 * @addr_type: address type (MB_TERM_ADDR_L, MB_TERM_ADDR_A or MB_TERM_ADDR_G)
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

static MBTree *
ctree_create_newobj (MonoMemPool *mp, MonoClass *klass)
{
	MBTree *t1, *t2;
	static gpointer newobj_func = mono_object_new;

	t1 = mono_ctree_new_leaf (mp, MB_TERM_ARG_END);
	t2 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
	t2->data.p = klass;
	t1 = mono_ctree_new (mp, MB_TERM_ARG, t1, t2);	

	t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
	t2->data.p = &newobj_func;
	t1 = mono_ctree_new (mp, MB_TERM_CALL_I4, t1, t2);
	t1->size = sizeof (gpointer);

	return t1;
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
	case MB_TERM_ADDR_L:
	case MB_TERM_ADDR_A:
	case MB_TERM_ADDR_G:
		t = mono_ctree_new_leaf (mp, s->op);
		t->data.i = s->data.i;
		return t;
	case MB_TERM_STIND_I1:
	case MB_TERM_LDIND_I1:
		t = ctree_create_dup (mp, s->left);
		return mono_ctree_new (mp, MB_TERM_LDIND_I1, t, NULL);
	case MB_TERM_STIND_I2:
	case MB_TERM_LDIND_I2:
		t = ctree_create_dup (mp, s->left);
		return mono_ctree_new (mp, MB_TERM_LDIND_I2, t, NULL);
	case MB_TERM_STIND_I4:
	case MB_TERM_LDIND_I4:
		t = ctree_create_dup (mp, s->left);
		return mono_ctree_new (mp, MB_TERM_LDIND_I4, t, NULL);
	case MB_TERM_STIND_I8:
	case MB_TERM_LDIND_I8:
		t = ctree_create_dup (mp, s->left);
		return mono_ctree_new (mp, MB_TERM_LDIND_I8, t, NULL);
	case MB_TERM_STIND_R4:
	case MB_TERM_LDIND_R4:
		t = ctree_create_dup (mp, s->left);
		return mono_ctree_new (mp, MB_TERM_LDIND_R4, t, NULL);
	case MB_TERM_STIND_R8:
	case MB_TERM_LDIND_R8:
		t = ctree_create_dup (mp, s->left);
		return mono_ctree_new (mp, MB_TERM_LDIND_R8, t, NULL);
	default:
		g_warning ("unknown op \"%s\"", mono_burg_term_string [s->op]);
		g_assert_not_reached ();
	}

	g_assert_not_reached ();
	return NULL;
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
	
			cctor = arch_create_jit_trampoline (method);
			cctor ();
			return;
		}
	}
	/* No class constructor found */
}

#define ADD_TREE(t)     do { g_ptr_array_add (forest, (t)); } while (0)
#define PUSH_TREE(t)    do { *sp = t; sp++; } while (0)

#define LOCAL_POS(n)    (locals_offsets [(n)])
#define LOCAL_TYPE(n)   ((header)->locals [(n)])

#define ARG_POS(n)      (args_offsets [(n)])
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])

/**
 * mono_create_forest:
 * @method: the method to analyse
 * @mp: a memory pool
 * @locals_size: to return the size of local vars
 *
 * This is the architecture independent part of JIT compilation.
 * It creates a forest of trees which can then be fed into the
 * architecture dependent code generation.
 *
 * We should extend this to mark basic blocks. We can then also try
 * to make various optimisations at this level.
 */
GPtrArray *
mono_create_forest (MonoMethod *method, MonoMemPool *mp, guint *locals_size)
{
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	MBTree **sp, **stack, *t1, *t2;
	register const unsigned char *ip, *end;
	guint *locals_offsets;
	guint *args_offsets;
	GPtrArray *forest;
	int local_offset = 0;

	header = ((MonoMethodNormal *)method)->header;
	signature = method->signature;
	image = method->klass->image;
	
	ip = header->code;
	end = ip + header->code_size;

	sp = stack = alloca (sizeof (MBTree *) * (header->max_stack + 1));
	
	if (header->num_locals) {
		int i, size, align;
		locals_offsets = alloca (sizeof (gint) * header->num_locals);

		for (i = 0; i < header->num_locals; ++i) {
			size = mono_type_size (header->locals [i], &align);
			local_offset += align - 1;
			local_offset &= ~(align - 1);
			local_offset += size;
			locals_offsets [i] = - local_offset;
		}
	}
	
	if (signature->params_size) {
		int i, align, size, offset = 0;
		int has_this = signature->hasthis;

		args_offsets = alloca (sizeof (gint) * (signature->param_count + has_this));
		if (has_this) {
			args_offsets [0] = 0;
			offset += sizeof (gpointer);
		}

		for (i = 0; i < signature->param_count; ++i) {
			args_offsets [i + has_this] = offset;
			size = mono_type_size (signature->params [i], &align);
			if (size < 4) {
				size = 4; 
				align = 4;
			}
			offset += align - 1;
			offset &= ~(align - 1);
			offset += size;
		}
	}

	forest = g_ptr_array_new ();

	while (ip < end) {
		guint32 cli_addr = ip - header->code;

		printf ("IL%04x OPCODE %s %d\n", cli_addr, opcode_names [*ip], forest->len);

		switch (*ip) {

		case CEE_THROW: {
			--sp;
			ip++;
			
			// fixme: 

			t1 = mono_ctree_new_leaf (mp, MB_TERM_NOP);
			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);		
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

			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
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
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_I4, *sp, NULL);
				break;
			case 2:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_U2, *sp, NULL);
				break;
			case 1:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_U1, *sp, NULL);
				break;
			default:
				t1 = mono_ctree_new (mp, MB_TERM_LDIND_OBJ, *sp, NULL);
				break;
			}
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
			break;
		}
		case CEE_LDSTR: {
			MonoObject *o;
			guint32 index;

			++ip;
			index = mono_metadata_token_index (read32 (ip));
			ip += 4;

			o = mono_ldstr (image, index);
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.p = o;

			PUSH_TREE (t1);
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
			} else {
				t1 = ctree_create_load (mp, MB_TERM_ADDR_G, field->type, addr);
			}

			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
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
				t1 = mono_ctree_new (mp, map_ldind_type (field->type), t1, NULL);
	
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
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

			t1->cli_addr = sp [0]->cli_addr;
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

			t1 = mono_ctree_new (mp, MB_TERM_ADD, sp [0], t1);
			t1 = mono_ctree_new (mp, map_stind_type (field->type), t1, sp [1]);

			t1->cli_addr = sp [0]->cli_addr;
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
			t1->cli_addr = cli_addr;
			ADD_TREE (t1);
			break;
		} 
		case CEE_SWITCH: {
			guint32 i, n, *jt;
			gint32 st;

			++ip;
			n = read32 (ip);
			ip += 4;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_SWITCH, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			jt = t1->data.p = mono_alloc_static (4 * (n + 2));
			st = jt [n + 1] = cli_addr + 5 + 4 * n;
			
			jt [0] = n;
			for (i = 1; i <= n; i++) {
				jt [i] = read32 (ip) + st; 
				ip += 4;
			}

			ADD_TREE (t1);
			break;
		}
		case CEE_NEWOBJ:
		case CEE_CALL: 
		case CEE_CALLVIRT: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			MBTree *nobj, *this = NULL;
			guint32 token;
			int i, nargs, align, size, args_size = 0;
			gint32 fa;
			int virtual = *ip == CEE_CALLVIRT;
			int newobj = *ip == CEE_NEWOBJ;

			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			g_assert (cm);

			if (!cm->addr)
				cm->addr = arch_create_jit_trampoline (cm);
			
			if ((cm->flags & METHOD_ATTRIBUTE_FINAL) ||
			    !(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
				virtual = 0;

#warning disabled virtual calls
			virtual = 0;

			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);

			if (newobj) {
				for (i = 0; i < csig->param_count; i++)
					sp [-i] = sp [-i - 1];
				local_offset += sizeof (gpointer);
				nobj = ctree_create_newobj (mp, cm->klass);
				nobj = ctree_create_store (mp, MB_TERM_ADDR_L, nobj, 
							   &cm->klass->this_arg, 
							   (gpointer)-local_offset);
				nobj->cli_addr = cli_addr;
				ADD_TREE (nobj);
				sp [-i] =  ctree_create_dup (mp, nobj);
				sp++;
			} 
			

			nargs = csig->param_count;
			if (csig->hasthis || virtual || newobj) {
				nargs++;
				sp = sp - nargs;
				this =  *sp;
			} else {
				sp = sp - nargs;
			}

			if (virtual) {
				t2 = ctree_create_dup (mp, this);

				if (!cm->klass->metadata_inited)
					mono_class_metadata_init (cm->klass);

				if (cm->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
					t2 = mono_ctree_new (mp, MB_TERM_INTF_ADDR, t1, NULL);
					t2->data.i = cm->klass->interface_id << 2;
					printf ("SLOT %s %d %d\n", cm->name, cm->klass->metadata_inited, cm->slot);
					t2->size = cm->slot << 2;
				} else {
					t2 = mono_ctree_new (mp, MB_TERM_VFUNC_ADDR, t1, NULL);
					t2->data.i = cm->slot << 2;
				}
			} else {
				t2 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_G);
				t2->data.p = (char *)cm + G_STRUCT_OFFSET (MonoMethod, addr);
			}

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ARG_END);

			if (nargs) {

				fa = sp [0]->cli_addr;
				printf ("FA %04x\n", fa);

#ifdef ARCH_ARGS_RIGHT_TO_LEFT
				for (i = nargs - 1; i >= 0; i--) {
#else
				for (i = 0; i < nargs; i++) {
#endif
					t1 = mono_ctree_new (mp, MB_TERM_ARG, t1, sp [i]);	
					
					if (!i && this)
						size = mono_type_size (&cm->klass->this_arg, &align);
					else
						size = mono_type_size (cm->signature->params [i - (this != NULL)], &align);

					// fixme: does this really work ?
					args_size += (size + 3) & ~3;
				}
			} else
				fa = cli_addr;

			t1 = mono_ctree_new (mp, map_call_type (csig->ret), t1, t2);
			t1->size = args_size;
			
			if (csig->ret->type != MONO_TYPE_VOID) {
				size = mono_type_size (csig->ret, &align);
				local_offset += align - 1;
				local_offset &= ~(align - 1);
				local_offset += size;

				t2 = ctree_create_store (mp, MB_TERM_ADDR_L, t1, csig->ret, 
							 (gpointer)-local_offset);
				t2->cli_addr = fa;
				ADD_TREE (t2);

				t1 = ctree_create_dup (mp, t2);
				PUSH_TREE (t1);
			} else {
				if (newobj) {
					ADD_TREE (t1);			
					t1 = ctree_create_dup (mp, nobj);		
					PUSH_TREE (t1);
				} else {
					t1->cli_addr = fa;
					ADD_TREE (t1);
				}
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
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;
		}
		case CEE_LDC_I4: { 
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = read32 (ip);
			ip += 4;
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
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
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;
		}
		case CEE_LDNULL: {
			//fixme: don't know if this is portable ?
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I4);
			t1->data.i = 0;
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;
		}
		case CEE_LDC_I8: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_I8);
			t1->data.l = read64 (ip);
			ip += 8;
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_LDC_R4: {
			float *f = mono_alloc_static (sizeof (float));
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_R4);
			readr4 (ip, f);
			t1->data.p = f;
			t1->cli_addr = cli_addr;
			ip += 4;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_LDC_R8: { 
			double *d = mono_alloc_static (sizeof (double));
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_CONST_R8);
			readr8 (ip, d);
			t1->data.p = d;
			t1->cli_addr = cli_addr;
			ip += 8;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int n = (*ip) - CEE_LDLOC_0;
			++ip;

			t1 = ctree_create_load (mp, MB_TERM_ADDR_L, LOCAL_TYPE (n), 
						(gpointer)LOCAL_POS (n));
			t1->cli_addr = cli_addr;

			PUSH_TREE (t1);
			break;
		}
		case CEE_LDLOC_S: {
			++ip;
			
			t1 = ctree_create_load (mp, MB_TERM_ADDR_L, LOCAL_TYPE (*ip), 
						(gpointer)LOCAL_POS (*ip));
			t1->cli_addr = cli_addr;
			++ip;

			PUSH_TREE (t1);
			break;
		}
		case CEE_LDLOCA_S: {
			++ip;

			t1 = mono_ctree_new_leaf (mp, MB_TERM_ADDR_L);
			t1->data.p = (gpointer)LOCAL_POS (*ip);
			t1->cli_addr = cli_addr;
			++ip;
			PUSH_TREE (t1);			
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
			t1->cli_addr = sp [0]->cli_addr;

			ADD_TREE (t1);			
			break;
		}
		case CEE_STLOC_S: {
			++ip;
			--sp;

			t1 = ctree_create_store (mp, MB_TERM_ADDR_L, *sp, LOCAL_TYPE (*ip), 
						 (gpointer)LOCAL_POS (*ip));
			t1->cli_addr = sp [0]->cli_addr;
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

		MAKE_LDIND (LDIND_I1,  MB_TERM_LDIND_I1)
		MAKE_LDIND (LDIND_U1,  MB_TERM_LDIND_U1)
		MAKE_LDIND (LDIND_I2,  MB_TERM_LDIND_I2)
		MAKE_LDIND (LDIND_U2,  MB_TERM_LDIND_U2)
		MAKE_LDIND (LDIND_I,   MB_TERM_LDIND_I4)
		MAKE_LDIND (LDIND_I4,  MB_TERM_LDIND_I4)
		MAKE_LDIND (LDIND_REF, MB_TERM_LDIND_U4)
		MAKE_LDIND (LDIND_U4,  MB_TERM_LDIND_U4)
		MAKE_LDIND (LDIND_I8,  MB_TERM_LDIND_I8)
		MAKE_LDIND (LDIND_R4,  MB_TERM_LDIND_R4)
		MAKE_LDIND (LDIND_R8,  MB_TERM_LDIND_R8)

		MAKE_STIND (STIND_I1,  MB_TERM_STIND_I1)
		MAKE_STIND (STIND_I2,  MB_TERM_STIND_I2)
		MAKE_STIND (STIND_I,   MB_TERM_STIND_I4)
		MAKE_STIND (STIND_I4,  MB_TERM_STIND_I4)
		MAKE_STIND (STIND_I8,  MB_TERM_STIND_I8)
		MAKE_STIND (STIND_R4,  MB_TERM_STIND_R4)
		MAKE_STIND (STIND_R8,  MB_TERM_STIND_R8)
		MAKE_STIND (STIND_REF, MB_TERM_STIND_I4)

		case CEE_NEG: {
			ip++;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_NEG, sp [0], NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_NOT: {
			ip++;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_NOT, sp [0], NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
			break;
		}
	        case CEE_BR_S: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			t1->data.i = cli_addr + 2 + (signed char) *ip;
			t1->cli_addr = cli_addr;
			ADD_TREE (t1);
			++ip;
			break;
		}
		case CEE_BR: {
			++ip;
			t1 = mono_ctree_new_leaf (mp, MB_TERM_BR);
			t1->data.i = cli_addr + 5 + (gint32) read32(ip);
			t1->cli_addr = cli_addr;
			ADD_TREE (t1);
			ip += 4;
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
			int near_jump = *ip == CEE_BRTRUE_S;
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_BRTRUE, sp [0], NULL);

			if (near_jump)
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			ip += near_jump ? 1: 4;
			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);
			break;
		}
		case CEE_BRFALSE:
		case CEE_BRFALSE_S: {
			int near_jump = *ip == CEE_BRFALSE_S;
			++ip;
			--sp;

			t1 = mono_ctree_new (mp, MB_TERM_BRFALSE, sp [0], NULL);

			if (near_jump)
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			ip += near_jump ? 1: 4;
			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);
			break;
		}
		case CEE_RET: {
			ip++;

			if (signature->ret->type != MONO_TYPE_VOID) {
				--sp;
				t1 = mono_ctree_new (mp, MB_TERM_RETV, *sp, NULL);
				t1->cli_addr = sp [0]->cli_addr;
			} else {
				t1 = mono_ctree_new (mp, MB_TERM_RET, NULL, NULL);
				t1->cli_addr = cli_addr;
			}

			t1->last_instr = (ip == end);

			ADD_TREE (t1);

			if (sp > stack) {
				g_warning ("more values on stack at IL_%04x: %d",  ip - header->code, sp - stack);
				mono_print_ctree (sp [-1]);
				printf ("\n");
			}
			break;
		}
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int n = (*ip) - CEE_LDARG_0;
			++ip;

			t1 = ctree_create_load (mp, MB_TERM_ADDR_A, ARG_TYPE (n), 
						(gpointer)ARG_POS (n));
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;
		}
		case CEE_LDARG_S: {
			++ip;

			t1 = ctree_create_load (mp, MB_TERM_ADDR_A, ARG_TYPE (*ip), 
						(gpointer)ARG_POS (*ip));
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			++ip;
			break;
		}
		case CEE_DUP: {
			++ip; 
			
			t1 = ctree_create_dup (mp, sp [-1]);
			PUSH_TREE (t1);

			break;
		}
		case CEE_POP: {
			++ip;
			--sp;
			/*
			 * all side effects are already on the forest,
			 * so we can simply ignore this tree
			 */
			// we use the mompool g_free (*sp);
			break;
		}
		case CEE_CONV_U1: 
		case CEE_CONV_I1: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I1, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_CONV_U2: 
		case CEE_CONV_I2: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I2, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_CONV_I: 
		case CEE_CONV_I4: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I4, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_CONV_I8: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_I8, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case CEE_CONV_R8: {
			++ip;
			sp--;
			t1 = mono_ctree_new (mp, MB_TERM_CONV_R8, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
		}
		case 0xFE:
			++ip;			
			switch (*ip) {
				
			MAKE_BI_ALU (CEQ)

			case CEE_LDARG: {
				guint32 n;
				++ip;
				n = read32 (ip);
				ip += 4;

				t1 = ctree_create_load (mp, MB_TERM_ADDR_A, ARG_TYPE (n), 
							(gpointer)ARG_POS (n));
				t1->cli_addr = cli_addr;
				PUSH_TREE (t1);
				break;
			}
			default:
				g_error ("Unimplemented opcode at IL_%04x "
					 "0xFE %02x", ip - header->code, *ip);
			}
			break;
		default:
			g_warning ("unknown instruction `%s' at IL_%04X", 
				   opcode_names [*ip], ip - header->code);
			mono_print_forest (forest);
			g_assert_not_reached ();
		}
	}

	*locals_size = local_offset;
	return forest;
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

		mfunc = arch_create_jit_trampoline (method);
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
		else
			usage (argv [0]);
	}
	
	file = argv [i];

	if (!file)
		usage (argv [0]);

	mono_init ();
	mono_init_icall ();

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



