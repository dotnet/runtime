/*
 * emit-x86.c: Support functions for emitting x86 code
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"
#include "codegen.h"
#include "debug.h"

static void
enter_method (MonoMethod *method, gpointer ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;

	printf ("ENTER: %s.%s::%s\n(", method->klass->name_space,
		method->klass->name, method->name);

	
	if (((int)ebp & 3) != 0) {
		g_error ("unaligned stack detected (%p)", ebp);
	}

	ebp += 8;

	if (ISSTRUCT (method->signature->ret)) {
		int size, align;
		
		g_assert (!method->signature->ret->byref);

		size = mono_type_stack_size (method->signature->ret, &align);

		printf ("VALUERET:%p, ", *((gpointer *)ebp));
		ebp += sizeof (gpointer);
	}

	if (method->signature->hasthis) {
		if (method->klass->valuetype) {
			printf ("value:%p, ", *((gpointer *)ebp));
		} else {
			o = *((MonoObject **)ebp);

			g_assert (o);

			class = o->vtable->klass;

			if (class == mono_defaults.string_class) {
				printf ("this:[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
			} else {
				printf ("this:%p[%s.%s], ", o, class->name_space, class->name);
			}
		}
		ebp += sizeof (gpointer);
	}

	for (i = 0; i < method->signature->param_count; ++i) {
		MonoType *type = method->signature->params [i];
		int size, align;
		size = mono_type_stack_size (type, &align);

		if (type->byref) {
			printf ("[BYREF:%p], ", *((gpointer *)ebp)); 
		} else switch (type->type) {
			
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			printf ("%d, ", *((int *)(ebp)));
			break;
		case MONO_TYPE_STRING: {
			MonoString *s = *((MonoString **)ebp);
			if (s) {
				g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
				printf ("[STRING:%p:%s], ", s, mono_string_to_utf8 (s));
			} else 
				printf ("[STRING:null], ");
			break;
		}
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT: {
			o = *((MonoObject **)ebp);
			if (o) {
				class = o->vtable->klass;
		    
				if (class == mono_defaults.string_class) {
					printf ("[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
				} else if (class == mono_defaults.int32_class) {
					printf ("[INT32:%p:%d], ", o, *(gint32 *)((gpointer)o + sizeof (MonoObject)));
				} else
					printf ("[%s.%s:%p], ", class->name_space, class->name, o);
			} else {
				printf ("%p, ", *((gpointer *)(ebp)));				
			}
			break;
		}
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			printf ("%p, ", *((gpointer *)(ebp)));
			break;
		case MONO_TYPE_I8:
			printf ("%lld, ", *((gint64 *)(ebp)));
			break;
		case MONO_TYPE_R4:
			printf ("%f, ", *((float *)(ebp)));
			break;
		case MONO_TYPE_R8:
			printf ("%f, ", *((double *)(ebp)));
			break;
		case MONO_TYPE_VALUETYPE: 
			printf ("[");
			for (j = 0; j < size; j++)
				printf ("%02x,", *((guint8*)ebp +j));
			printf ("], ");
			break;
		default:
			printf ("XX, ");
		}

		g_assert (align == 4);
		ebp += size + 3;
		ebp = (gpointer)((unsigned)ebp & ~(3));
	}

	printf (")\n");
}

static void
leave_method (MonoMethod *method, int edx, int eax, double test)
{
	gint64 l;

	printf ("LEAVE: %s.%s::%s ", method->klass->name_space,
		method->klass->name, method->name);

	switch (method->signature->ret->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_BOOLEAN:
		if (eax)
			printf ("TRUE:%d", eax);
		else 
			printf ("FALSE");
			
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		printf ("EAX=%d", eax);
		break;
	case MONO_TYPE_STRING: {
		MonoString *s = (MonoString *)eax;

		if (s) {
			g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
			printf ("[STRING:%p:%s]", s, mono_string_to_utf8 (s));
		} else 
			printf ("[STRING:null], ");
		break;
	}
	case MONO_TYPE_OBJECT: {
		MonoObject *o = (MonoObject *)eax;

		if (o) {
			if (o->vtable->klass == mono_defaults.boolean_class) {
				printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
			} else if  (o->vtable->klass == mono_defaults.int32_class) {
				printf ("[INT32:%p:%d]", o, *((gint32 *)((gpointer)o + sizeof (MonoObject))));	
			} else
				printf ("[%s.%s:%p]", o->vtable->klass->name_space, o->vtable->klass->name, o);
		} else
			printf ("[OBJECT:%p]", o);
	       
		break;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		printf ("EAX=%p", (gpointer)eax);
		break;
	case MONO_TYPE_I8:
		*((gint32 *)&l) = eax;
		*((gint32 *)&l + 1) = edx;
		printf ("EAX/EDX=%lld", l);
		break;
	case MONO_TYPE_R8:
		printf ("FP=%f\n", test);
		break;
	default:
		printf ("(unknown return type)");
	}

	printf ("\n");
}

/**
 * arch_emit_prologue:
 * @cfg: pointer to status information
 *
 * Emits the function prolog.
 */
static void
arch_emit_prologue (MonoFlowGraph *cfg)
{
	x86_push_reg (cfg->code, X86_EBP);
	x86_mov_reg_reg (cfg->code, X86_EBP, X86_ESP, 4);

	if (cfg->locals_size)
		x86_alu_reg_imm (cfg->code, X86_SUB, X86_ESP, cfg->locals_size);

	if (mono_regset_reg_used (cfg->rs, X86_EBX)) 
		x86_push_reg (cfg->code, X86_EBX);

	if (mono_regset_reg_used (cfg->rs, X86_EDI)) 
		x86_push_reg (cfg->code, X86_EDI);

	if (mono_regset_reg_used (cfg->rs, X86_ESI))
		x86_push_reg (cfg->code, X86_ESI);

	if (mono_jit_trace_calls) {
		x86_push_reg (cfg->code, X86_EBP);
		x86_push_imm (cfg->code, cfg->method);
		x86_mov_reg_imm (cfg->code, X86_EAX, enter_method);
		x86_call_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 8);
	}
}

/**
 * arch_emit_epilogue:
 * @cfg: pointer to status information
 *
 * Emits the function epilog.
 */
static void
arch_emit_epilogue (MonoFlowGraph *cfg)
{
	if (mono_jit_trace_calls) {
		x86_fld_reg (cfg->code, 0);
		x86_alu_reg_imm (cfg->code, X86_SUB, X86_ESP, 8);
		x86_fst_membase (cfg->code, X86_ESP, 0, TRUE, TRUE);
		x86_push_reg (cfg->code, X86_EAX);
		x86_push_reg (cfg->code, X86_EDX);
		x86_push_imm (cfg->code, cfg->method);
		x86_mov_reg_imm (cfg->code, X86_EAX, leave_method);
		x86_call_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 4);
		x86_pop_reg (cfg->code, X86_EDX);
		x86_pop_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 8);
	}

	if (mono_regset_reg_used (cfg->rs, X86_ESI))
		x86_pop_reg (cfg->code, X86_ESI);

	if (mono_regset_reg_used (cfg->rs, X86_EDI))
		x86_pop_reg (cfg->code, X86_EDI);

	if (mono_regset_reg_used (cfg->rs, X86_EBX))
		x86_pop_reg (cfg->code, X86_EBX);

	x86_leave (cfg->code);
	x86_ret (cfg->code);
}

/*
 * get_unbox_trampoline:
 * @m: method pointer
 * @addr: pointer to native code for @m
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
static gpointer
get_unbox_trampoline (MonoMethod *m, gpointer addr)
{
	guint8 *code, *start;
	int this_pos = 4;

	if (!m->signature->ret->byref && m->signature->ret->type == MONO_TYPE_VALUETYPE)
		this_pos = 8;
	    
	start = code = g_malloc (16);

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, addr);
	g_assert ((code - start) < 16);

	return start;
}

/**
 * x86_magic_trampoline:
 * @eax: saved x86 register 
 * @ecx: saved x86 register 
 * @edx: saved x86 register 
 * @esi: saved x86 register 
 * @edi: saved x86 register 
 * @ebx: saved x86 register
 * @code: pointer into caller code
 * @method: the method to translate
 *
 * This method is called by the trampoline functions for virtual
 * methods. It inspects the caller code to find the address of the
 * vtable slot, then calls the JIT compiler and writes the address
 * of the compiled method back to the vtable. All virtual methods 
 * are called with: x86_call_membase (inst, basereg, disp). We always
 * use 32 bit displacement to ensure that the length of the call 
 * instruction is 6 bytes. We need to get the value of the basereg 
 * and the constant displacement.
 */
static gpointer
x86_magic_trampoline (int eax, int ecx, int edx, int esi, int edi, 
		      int ebx, const guint8 *code, MonoMethod *m)
{
	guint8 reg;
	gint32 disp;
	gpointer o;
	gpointer addr;

	EnterCriticalSection (metadata_section);
	addr = arch_compile_method (m);
	LeaveCriticalSection (metadata_section);
	g_assert (addr);


	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 6;
	if ((code [1] != 0xe8) && (code [3] == 0xff) && ((code [4] & 0x18) == 0x10) && ((code [4] >> 6) == 1)) {
		reg = code [4] & 0x07;
		disp = (signed char)code [5];
	} else {
		if ((code [0] == 0xff) && ((code [1] & 0x18) == 0x10) && ((code [1] >> 6) == 2)) {
			reg = code [1] & 0x07;
			disp = *((gint32*)(code + 2));
		} else if ((code [1] == 0xe8)) {
			*((guint32*)(code + 2)) = (guint)addr - ((guint)code + 1) - 5; 
			return addr;
		} else {
			printf ("%x %x %x %x %x %x \n", code [0], code [1], code [2], code [3],
				code [4], code [5]);
			g_assert_not_reached ();
		}
	}

	switch (reg) {
	case X86_EAX:
		o = (gpointer)eax;
		break;
	case X86_EDX:
		o = (gpointer)edx;
		break;
	case X86_ECX:
		o = (gpointer)ecx;
		break;
	case X86_ESI:
		o = (gpointer)esi;
		break;
	case X86_EDI:
		o = (gpointer)edi;
		break;
	case X86_EBX:
		o = (gpointer)ebx;
		break;
	default:
		g_assert_not_reached ();
	}

	o += disp;

	if (m->klass->valuetype) {
		return *((gpointer *)o) = get_unbox_trampoline (m, addr);
	} else {
		return *((gpointer *)o) = addr;
	}
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * corresponding vtable entry (see x86_magic_trampoline).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
arch_create_jit_trampoline (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get ();
	guint8 *code, *buf;
	static guint8 *vc = NULL;
	GHashTable *jit_code_hash;

	/* icalls use method->addr */
	if (method->addr)
		return method->addr;

	/* previously created trampoline code */
	if (method->info)
		return method->info;

	/* check if we already have JITed code */
	if (mono_jit_share_code)
		jit_code_hash = mono_root_domain->jit_code_hash;
	else
		jit_code_hash = domain->jit_code_hash;

	if ((code = g_hash_table_lookup (jit_code_hash, method))) {
		mono_jit_stats.methods_lookups++;
		return code;
	}

	if (!vc) {
		vc = buf = g_malloc (256);

		/* save caller save regs because we need to do a call */ 
		x86_push_reg (buf, X86_EDX);
		x86_push_reg (buf, X86_EAX);
		x86_push_reg (buf, X86_ECX);

		/* save LMF begin */
		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_reg (buf, X86_EBP);

		/* save the IP (caller ip) */
		x86_push_membase (buf, X86_ESP, 32);

		/* save method info */
		x86_push_membase (buf, X86_ESP, 32);
		/* get the address of lmf for the current thread */
		x86_call_code (buf, arch_get_lmf_addr);
		/* push lmf */
		x86_push_reg (buf, X86_EAX); 
		/* push *lfm (previous_lmf) */
		x86_push_membase (buf, X86_EAX, 0);
		/* *(lmf) = ESP */
		x86_mov_membase_reg (buf, X86_EAX, 0, X86_ESP, 4);
		/* save LFM end */

		/* push the method info */
		x86_push_membase (buf, X86_ESP, 44);
		/* push the return address onto the stack */
		x86_push_membase (buf, X86_ESP, 52);

		/* save all register values */
		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_membase (buf, X86_ESP, 64); /* EDX */
		x86_push_membase (buf, X86_ESP, 64); /* ECX */
		x86_push_membase (buf, X86_ESP, 64); /* EAX */

		x86_call_code (buf, x86_magic_trampoline);
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 8*4);

		/* restore LMF start */
		/* ebx = previous_lmf */
		x86_pop_reg (buf, X86_EBX);
		/* edi = lmf */
		x86_pop_reg (buf, X86_EDI);
		/* *(lmf) = previous_lmf */
		x86_mov_membase_reg (buf, X86_EDI, 0, X86_EBX, 4);
		/* discard method info */
		x86_pop_reg (buf, X86_ESI);
		/* discard save IP */
		x86_pop_reg (buf, X86_ESI);
		/* restore caller saved regs */
		x86_pop_reg (buf, X86_EBP);
		x86_pop_reg (buf, X86_ESI);
		x86_pop_reg (buf, X86_EDI);
		x86_pop_reg (buf, X86_EBX);
		/* restore LMF end */

		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 16);

		/* call the compiled method */
		x86_jump_reg (buf, X86_EAX);

		g_assert ((buf - vc) <= 256);
	}

	code = buf = g_malloc (16);
	x86_push_imm (buf, method);
	x86_jump_code (buf, vc);
	g_assert ((buf - code) <= 16);

	/* store trampoline address */
	method->info = code;

	mono_jit_stats.method_trampolines++;

	return code;
}

static void
mono_label_cfg (MonoFlowGraph *cfg)
{
	int i, j;
	
	for (i = 0; i < cfg->block_count; i++) {
		GPtrArray *forest = cfg->bblocks [i].forest;
		int top;

		if (!cfg->bblocks [i].reached) /* unreachable code */
			continue;
		
		top = forest->len;

		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			MBState *mbstate;

			mbstate =  mono_burg_label (t1, cfg);
			if (!mbstate) {
				cfg->invalid = 1;
				if (mono_debug_handle)
					return;
				g_warning ("tree does not match");
				mono_print_ctree (t1); printf ("\n\n");

				mono_print_forest (forest);
				g_assert_not_reached ();
			}
		}
	}
}

static void
tree_preallocate_regs (MBTree *tree, int goal, MonoRegSet *rs) 
{
	switch (tree->op) {
	case MB_TERM_CALL_I4:
	case MB_TERM_CALL_I8:
	case MB_TERM_CALL_R8:
//	case MB_TERM_CALL_VOID :
		tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, tree->exclude_mask);
		tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, tree->exclude_mask);
		tree->reg3 = mono_regset_alloc_reg (rs, X86_ECX, tree->exclude_mask);
		return;
	default: break;
	}

	switch (goal) {
	case MB_NTERM_reg:
	case MB_NTERM_lreg: {
		switch (tree->op) {
		case MB_TERM_SHL:
		case MB_TERM_SHR:
		case MB_TERM_SHR_UN:
			tree->exclude_mask |= (1 << X86_ECX);
			tree->left->exclude_mask |= (1 << X86_ECX);
			break;
		case MB_TERM_MUL:
		case MB_TERM_MUL_OVF:
		case MB_TERM_MUL_OVF_UN:
		case MB_TERM_DIV:
		case MB_TERM_DIV_UN:
		case MB_TERM_REM:
		case MB_TERM_REM_UN:
			tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, tree->exclude_mask);
			tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, tree->exclude_mask);
			if (goal == MB_NTERM_reg) {
				tree->left->exclude_mask |= (1 << X86_EDX);
				tree->right->exclude_mask |= (1 << X86_EDX) | (1 << X86_EAX);
			}
			break;
		default:
			break;
		}
		break;
	}
	default:
		break;
	}
}

static void
tree_allocate_regs (MBTree *tree, int goal, MonoRegSet *rs) 
{
	MBTree *kids[10];
	int ern = mono_burg_rule (tree->state, goal);
	const guint16 *nts = mono_burg_nts [ern];
	int i;
	
	mono_burg_kids (tree, ern, kids);

	//printf ("RALLOC START %d %p %d\n",  tree->op, rs->free_mask, goal);

	if (nts [0] && kids [0] == tree) {
		/* chain rule */
		tree_allocate_regs (kids [0], nts [0], rs);
		return;
	}

	for (i = 0; nts [i]; i++)
		tree_preallocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++)
		tree_allocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++) {
		mono_regset_free_reg (rs, kids [i]->reg1);
		mono_regset_free_reg (rs, kids [i]->reg2);
		mono_regset_free_reg (rs, kids [i]->reg3);
	}

	switch (goal) {
	case MB_NTERM_reg:
		if (tree->reg1 < 0) { 
			tree->reg1 = mono_regset_alloc_reg (rs, -1, tree->exclude_mask);
			g_assert (tree->reg1 != -1);
		}
		break;

	case MB_NTERM_lreg:
		if (tree->reg1 < 0) { 
			tree->reg1 = mono_regset_alloc_reg (rs, -1, tree->exclude_mask);
			g_assert (tree->reg1 != -1);
		}
		if (tree->reg2 < 0) { 
			tree->reg2 = mono_regset_alloc_reg (rs, -1, tree->exclude_mask);
			g_assert (tree->reg2 != -1);
		}
		break;

	case MB_NTERM_freg:
		/* fixme: allocate floating point registers */
		break;
      
	case MB_NTERM_addr:
		if (tree->op == MB_TERM_ADD) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
			tree->reg2 = mono_regset_alloc_reg (rs, tree->right->reg1, tree->exclude_mask);
		}
		break;
		
	case MB_NTERM_base:
		if (tree->op == MB_TERM_ADD) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
		}
		break;
	       
	case MB_NTERM_index:
		if (tree->op == MB_TERM_SHL ||
		    tree->op == MB_TERM_MUL) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
		}
		break;
	       
	default:
		/* do nothing */
	}

	//printf ("RALLOC END %d %p\n",  tree->op, rs->free_mask);
	tree->emit = mono_burg_func [ern];
}

static void
arch_allocate_regs (MonoFlowGraph *cfg)
{
	int i, j;
	
	for (i = 0; i < cfg->block_count; i++) {
		GPtrArray *forest = cfg->bblocks [i].forest;
		int top;

		if (!cfg->bblocks [i].reached) /* unreachable code */
			continue;

		top = forest->len;

		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			//printf ("AREGSTART %d:%d %p\n", i, j, cfg->rs->free_mask);
			tree_allocate_regs (t1, 1, cfg->rs);
			//printf ("AREGENDT %d:%d %p\n", i, j, cfg->rs->free_mask);
			g_assert (cfg->rs->free_mask == 0xffffffff);
		}
	}
}

static void
tree_emit (int goal, MonoFlowGraph *cfg, MBTree *tree) 
{
	MBTree *kids[10];
	int i, ern = mono_burg_rule (tree->state, goal);
	const guint16 *nts = mono_burg_nts [ern];
	MBEmitFunc emit;
	int offset;

	mono_burg_kids (tree, ern, kids);

	for (i = 0; nts [i]; i++) 
		tree_emit (nts [i], cfg, kids [i]);

	tree->addr = offset = cfg->code - cfg->start;

	// we assume an instruction uses a maximum of 128 bytes
	if ((cfg->code_size - offset) <= 128) {
		int add = MIN (cfg->code_size, 128);
		cfg->code_size += add;
		mono_jit_stats.code_reallocs++;
		cfg->start = g_realloc (cfg->start, cfg->code_size);
		g_assert (cfg->start);
		cfg->code = cfg->start + offset;
	}

	if ((emit = mono_burg_func [ern]))
		emit (tree, cfg);

	g_assert ((cfg->code - cfg->start) < cfg->code_size);
}

static void
mono_emit_cfg (MonoFlowGraph *cfg)
{
	int i, j;

	for (i = 0; i < cfg->block_count; i++) {
		MonoBBlock *bb = &cfg->bblocks [i];
		GPtrArray *forest = bb->forest;
		int top;

		if (!bb->reached) /* unreachable code */
			continue;
		
		top = forest->len;

		bb->addr = cfg->code - cfg->start;
	  
		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			
			tree_emit (1, cfg, t1);
		}
	}
		
	cfg->epilog = cfg->code - cfg->start;
}

static void
mono_compute_branches (MonoFlowGraph *cfg)
{
	MonoJumpInfo *ji;
	guint8 *end;
	int i, j;

	end = cfg->code;

	for (j = 0; j < cfg->block_count; j++) {
		MonoBBlock *bb = &cfg->bblocks [j];
		GPtrArray *forest = bb->forest;
		int top;
		
		if (!bb->reached) /* unreachable code */
			continue;

		top = forest->len;
	
		for (i = 0; i < top; i++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);

			if (t1->op == MB_TERM_SWITCH) {
				MonoBBlock **jt = (MonoBBlock **)t1->data.p;
				guint32 *rt = (guint32 *)t1->data.p;
				
				int m = *((guint32 *)t1->data.p) + 1;
				int j;
				
				for (j = 1; j <= m; j++)
					rt [j] = (int)(jt [j]->addr + cfg->start);
				
				/* emit the switch instruction again to update addresses */
				cfg->code = cfg->start + t1->addr;
				((MBEmitFunc)t1->emit) (t1, cfg);
			}
		}
	}

	cfg->code = end;

	for (ji = cfg->jump_info; ji; ji = ji->next) {
		gpointer *ip = GUINT_TO_POINTER (GPOINTER_TO_UINT (ji->ip) + cfg->start);
		gpointer target;

		switch (ji->type) {
		case MONO_JUMP_INFO_BB:
			target = ji->data.bb->addr + cfg->start;
			*ip = target - GPOINTER_TO_UINT(ip) - 4;
			break;
		case MONO_JUMP_INFO_ABS:
			target = ji->data.target;
			*ip = target - GPOINTER_TO_UINT(ip) - 4;
			break;
		case MONO_JUMP_INFO_EPILOG:
			target = cfg->epilog + cfg->start;
			*ip = target - GPOINTER_TO_UINT(ip) - 4;
			break;
		case MONO_JUMP_INFO_IP:
			*ip = ip;
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void
mono_add_jump_info (MonoFlowGraph *cfg, gpointer ip, MonoJumpInfoType type, gpointer target)
{
	MonoJumpInfo *ji = mono_mempool_alloc (cfg->mp, sizeof (MonoJumpInfo));

	ji->type = type;
	ji->ip = GUINT_TO_POINTER (GPOINTER_TO_UINT (ip) - GPOINTER_TO_UINT (cfg->start));
	ji->data.target = target;
	ji->next = cfg->jump_info;

	cfg->jump_info = ji;
}

static int
match_debug_method (MonoMethod* method)
{
	GList *tmp = mono_debug_methods;

	for (; tmp; tmp = tmp->next) {
		if (mono_method_desc_full_match (tmp->data, method))
			return 1;
	}
	return 0;
}

static void
mono_delegate_ctor (MonoDelegate *this, MonoObject *target, 
		    gpointer addr)
{
	MonoDomain *domain = mono_domain_get ();
	MonoClass *class;
	MonoJitInfo *ji;

	g_assert (this);
	g_assert (addr);

	class = this->object.vtable->klass;

	if (!target && (ji = mono_jit_info_table_find (mono_jit_info_table, addr)))
		this->method_info = mono_method_get_object (domain, ji->method);
	
	this->target = target;
	this->method_ptr = addr;

}

/**
 * arch_compile_method:
 * @method: pointer to the method info
 *
 * JIT compilation of a single method. 
 *
 * Returns: a pointer to the newly created code.
 */
gpointer
arch_compile_method (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	MonoMemPool *mp;
	guint8 *addr;
	GHashTable *jit_code_hash;

	g_assert (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
	g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

	if (mono_jit_share_code)
		jit_code_hash = mono_root_domain->jit_code_hash;
	else
		jit_code_hash = domain->jit_code_hash;

	if ((addr = g_hash_table_lookup (jit_code_hash, method))) {
		mono_jit_stats.methods_lookups++;
		return addr;
	}

	mono_jit_stats.methods_compiled++;
	
	mp = mono_mempool_new ();

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("Start JIT compilation of %s.%s:%s\n", method->klass->name_space,
			method->klass->name, method->name);
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		const char *name = method->name;
		guint8 *code;
		gboolean delegate = FALSE;

		if (method->klass->parent == mono_defaults.multicastdelegate_class)
			delegate = TRUE;
				
		if (delegate && *name == '.' && (strcmp (name, ".ctor") == 0)) {
			addr = (gpointer)mono_delegate_ctor;
		} else if (delegate && *name == 'I' && (strcmp (name, "Invoke") == 0)) {
			/*
			 *	Invoke( args .. ) {
			 *		if ( prev )
			 *			prev.Invoke();
			 *		return this.<m_target>( args );
			 *	}
			 */
			MonoMethodSignature *csig = method->signature;
			guint8 *br[2], *pos[2];
			int i, arg_size, this_pos = 4;
			
			if (csig->ret->type == MONO_TYPE_VALUETYPE) {
				g_assert (!csig->ret->byref);
				this_pos = 8;
			}

			arg_size = 0;
			if (csig->param_count) {
				int align;
				
				for (i = 0; i < csig->param_count; ++i) {
					arg_size += mono_type_stack_size (csig->params [i], &align);
					g_assert (align == 4);
				}
			}

			addr = g_malloc (64 + arg_size);

			code = addr;
			/* load the this pointer */
			x86_mov_reg_membase (code, X86_EAX, X86_ESP, this_pos, 4);
			
			/* load prev */
			x86_mov_reg_membase (code, X86_EDX, X86_EAX, G_STRUCT_OFFSET (MonoMulticastDelegate, prev), 4);

			/* prev == 0 ? */
			x86_alu_reg_imm (code, X86_CMP, X86_EDX, 0);
			br[0] = code; x86_branch32 (code, X86_CC_EQ, 0, TRUE );
			pos[0] = code;

			x86_push_reg( code, X86_EAX );
			/* push args */
			for ( i = 0; i < (arg_size>>2); i++ )
				x86_push_membase( code, X86_ESP, (arg_size + this_pos + 4) );
			/* push next */
			x86_push_reg( code, X86_EDX );
			if (this_pos == 8)
				x86_push_membase (code, X86_ESP, (arg_size + 8));
			/* recurse */
			br[1] = code; x86_call_imm( code, 0 );
			pos[1] = code; x86_call_imm( br[1], addr - pos[1] );

			if (this_pos == 8)
				x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 8);
			else
				x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);
			x86_pop_reg( code, X86_EAX );
			
			/* prev == 0 */ 
			x86_branch32( br[0], X86_CC_EQ, code - pos[0], TRUE );
			
			/* load mtarget */
			x86_mov_reg_membase (code, X86_EDX, X86_EAX, G_STRUCT_OFFSET (MonoDelegate, target), 4); 
			/* mtarget == 0 ? */
			x86_alu_reg_imm (code, X86_CMP, X86_EDX, 0);
			br[0] = code; x86_branch32 (code, X86_CC_EQ, 0, TRUE);
			pos[0] = code;

			/* 
			 * virtual delegate methods: we have to
			 * replace the this pointer with the actual
			 * target
			 */
			x86_mov_membase_reg (code, X86_ESP, this_pos, X86_EDX, 4); 
			/* jump to method_ptr() */
			x86_jump_membase (code, X86_EAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));

			/* mtarget != 0 */ 
			x86_branch32( br[0], X86_CC_EQ, code - pos[0], TRUE);
			/* 
			 * static delegate methods: we have to remove
			 * the this pointer from the activation frame
			 * - I do this creating a new stack frame anx
			 * copy all arguments except the this pointer
			 */
			g_assert ((arg_size & 3) == 0);
			for (i = 0; i < (arg_size>>2); i++) {
				x86_push_membase (code, X86_ESP, (arg_size + this_pos));
			}

			if (this_pos == 8)
				x86_push_membase (code, X86_ESP, (arg_size + 4));
			
			x86_call_membase (code, X86_EAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
			if (arg_size) {
				if (this_pos == 8) 
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);
				else
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size);
			}

			x86_ret (code);
		
			g_assert ((code - (guint8*)addr) < (64 + arg_size));

			if (mono_jit_dump_asm) {
				char *id = g_strdup_printf ("%s.%s_%s", method->klass->name_space,
							    method->klass->name, method->name);
				mono_disassemble_code( addr, code - (guint8*)addr, id );
				g_free (id);
			}
		} else {
			if (mono_debug_handle) 
				return NULL;

			g_error ("Don't know how to exec runtime method %s.%s::%s", 
				 method->klass->name_space, method->klass->name, method->name);
		}
	
	} else {
		MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
		MonoFlowGraph *cfg;

		gulong code_size_ratio;

		ji = g_new0 (MonoJitInfo, 1);
		
		cfg = mono_cfg_new (method, mp);

		mono_analyze_flow (cfg);
		if (cfg->invalid) 
			return NULL;
		
		mono_analyze_stack (cfg);
		if (cfg->invalid) 
			return NULL;
		
		cfg->rs = mono_regset_new (X86_NREG);
		mono_regset_reserve_reg (cfg->rs, X86_ESP);
		mono_regset_reserve_reg (cfg->rs, X86_EBP);

		cfg->code_size = MAX (header->code_size * 5, 256);
		cfg->start = cfg->code = g_malloc (cfg->code_size);

		if (match_debug_method (method))
			x86_breakpoint (cfg->code);
		else if (mono_debug_handle)
			x86_nop (cfg->code);

		if (mono_jit_dump_forest) {
			int i;
			printf ("FOREST %s.%s:%s\n", method->klass->name_space,
				method->klass->name, method->name);
			for (i = 0; i < cfg->block_count; i++) {
				printf ("BLOCK %d:\n", i);
				mono_print_forest (cfg->bblocks [i].forest);
			}
		}
	
		mono_label_cfg (cfg);
		if (cfg->invalid) 
			return NULL;
		
		arch_allocate_regs (cfg);

		/* align to 8 byte boundary */
		cfg->locals_size += 7;
		cfg->locals_size &= ~7;

		arch_emit_prologue (cfg);
		mono_emit_cfg (cfg);
		arch_emit_epilogue (cfg);		

		addr = cfg->start;

		mono_jit_stats.allocated_code_size += cfg->code_size;

		code_size_ratio = cfg->code - cfg->start;
		if (code_size_ratio > mono_jit_stats.biggest_method_size) {
			mono_jit_stats.biggest_method_size = code_size_ratio;
			mono_jit_stats.biggest_method = method;
		}
		code_size_ratio = (code_size_ratio * 100) / header->code_size;
		if (code_size_ratio > mono_jit_stats.max_code_size_ratio) {
			mono_jit_stats.max_code_size_ratio = code_size_ratio;
			mono_jit_stats.max_ratio_method = method;
		}

		mono_compute_branches (cfg);
		
		if (mono_jit_dump_asm) {
			char *id = g_strdup_printf ("%s.%s_%s", method->klass->name_space,
						    method->klass->name, method->name);
			mono_disassemble_code (cfg->start, cfg->code - cfg->start, id);
			g_free (id);
		}
		if (mono_debug_handle)
			mono_debug_add_method (mono_debug_handle, cfg);

		ji->code_size = cfg->code - cfg->start;
		ji->used_regs = cfg->rs->used_mask;
		ji->method = method;
		ji->code_start = addr;
		mono_jit_info_table_add (mono_jit_info_table, ji);

		mono_jit_stats.native_code_size += ji->code_size;

		if (header->num_clauses) {
			int i, start_block, end_block;

			ji->num_clauses = header->num_clauses;
			ji->clauses = g_new0 (MonoJitExceptionInfo, header->num_clauses);

			for (i = 0; i < header->num_clauses; i++) {
				MonoExceptionClause *ec = &header->clauses [i];
				MonoJitExceptionInfo *ei = &ji->clauses [i];
			
				ei->flags = ec->flags;
				ei->token_or_filter = ec->token_or_filter;

				g_assert (cfg->bcinfo [ec->try_offset].is_block_start);
				start_block = cfg->bcinfo [ec->try_offset].block_id;
				end_block = cfg->bcinfo [ec->try_offset + ec->try_len].block_id;
				g_assert (cfg->bcinfo [ec->try_offset + ec->try_len].is_block_start);
				
				ei->try_start = cfg->start + cfg->bblocks [start_block].addr;
				ei->try_end = cfg->start + cfg->bblocks [end_block].addr;
				
				g_assert (cfg->bcinfo [ec->handler_offset].is_block_start);
				start_block = cfg->bcinfo [ec->handler_offset].block_id;
				ei->handler_start = cfg->start + cfg->bblocks [start_block].addr;	
				
				//printf ("TEST %x %x %x\n", ei->try_start, ei->try_end, ei->handler_start);
			}
		}
		
		mono_regset_free (cfg->rs);

		mono_cfg_free (cfg);

		mono_mempool_destroy (mp);

	}

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("END JIT compilation of %s.%s:%s %p %p\n", method->klass->name_space,
			method->klass->name, method->name, method, addr);
	}

	g_hash_table_insert (jit_code_hash, method, addr);

	return addr;
}

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
static gpointer
arch_get_restore_context ()
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	/* restore_contect (struct sigcontext *ctx) */
	/* we do not restore X86_EAX, X86_EDX */

	start = code = g_malloc (1024);
	
	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);

	/* get return address, stored in EDX */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, eip), 4);
	/* restore EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ebx), 4);
	/* restore EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, edi), 4);
	/* restore ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, esi), 4);
	/* restore ESP */
	x86_mov_reg_membase (code, X86_ESP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, esp), 4);
	/* restore EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ebp), 4);
	/* restore ECX. the exception object is passed here to the catch handler */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ecx), 4);

	/* jump to the saved IP */
	x86_jump_reg (code, X86_EDX);

	return start;
}

/*
 * arch_get_call_finally:
 *
 * Returns a pointer to a method which calls a finally handler.
 */
static gpointer
arch_get_call_finally ()
{
	static guint8 start [28];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	/* call_finally (struct sigcontext *ctx, unsigned long eip) */
	code = start;

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	/* load eip */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 12, 4);
	/* save EBP */
	x86_push_reg (code, X86_EBP);
	/* set new EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ebp), 4);
	/* call the handler */
	x86_call_reg (code, X86_ECX);
	/* restore EBP */
	x86_pop_reg (code, X86_EBP);
	/* restore saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);

	g_assert ((code - start) < 28);
	return start;
}

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj:
 */
void
arch_handle_exception (struct sigcontext *ctx, gpointer obj)
{
	MonoDomain *domain = mono_domain_get ();
	MonoJitInfo *ji;
	gpointer ip = (gpointer)ctx->eip;
	static void (*restore_context) (struct sigcontext *);
	static void (*call_finally) (struct sigcontext *, unsigned long);
       
	g_assert (ctx != NULL);
	g_assert (obj != NULL);

	ji = mono_jit_info_table_find (mono_jit_info_table, ip);

	if (!restore_context)
		restore_context = arch_get_restore_context ();
	
	if (!call_finally)
		call_finally = arch_get_call_finally ();

	if (ji) { /* we are inside managed code */
		MonoMethod *m = ji->method;
		int offset = 2;

		if (ji->num_clauses) {
			int i;

			g_assert (ji->clauses);
			
			for (i = 0; i < ji->num_clauses; i++) {
				MonoJitExceptionInfo *ei = &ji->clauses [i];

				if (ei->try_start <= ip && ip <= (ei->try_end)) { 
					/* catch block */
					if (ei->flags == 0 && mono_object_isinst (obj, 
					        mono_class_get (m->klass->image, ei->token_or_filter))) {
					
						ctx->eip = (unsigned long)ei->handler_start;
						ctx->ecx = (unsigned long)obj;
						restore_context (ctx);
						g_assert_not_reached ();
					}
				}
			}

			/* no handler found - we need to call all finally handlers */
			for (i = 0; i < ji->num_clauses; i++) {
				MonoJitExceptionInfo *ei = &ji->clauses [i];

				if (ei->try_start <= ip && ip < (ei->try_end) &&
				    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
					call_finally (ctx, (unsigned long)ei->handler_start);
				}
			}
		}

		if (mono_object_isinst (obj, mono_defaults.exception_class)) {
			char  *strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);
			char  *tmp;

			if (!strcmp (strace, "TODO: implement stack traces")){
				g_free (strace);
				strace = g_strdup ("");
			}

			tmp = g_strdup_printf ("%sin %s.%s:%s ()\n", strace, m->klass->name_space,  
					       m->klass->name, m->name);

			g_free (strace);

			((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);
			g_free (tmp);
		}

		/* continue unwinding */

		/* restore caller saved registers */
		if (ji->used_regs & X86_ESI_MASK) {
			ctx->esi = *((int *)ctx->ebp + offset);
			offset++;
		}
		if (ji->used_regs & X86_EDI_MASK) {
			ctx->edi = *((int *)ctx->ebp + offset);
			offset++;
		}
		if (ji->used_regs & X86_EBX_MASK) {
			ctx->ebx = *((int *)ctx->ebp + offset);
		}

		ctx->esp = ctx->ebp;
		ctx->eip = *((int *)ctx->ebp + 1);
		ctx->ebp = *((int *)ctx->ebp);
		
		if (ctx->ebp < (unsigned)mono_end_of_stack)
			arch_handle_exception (ctx, obj);
		else
			mono_jit_abort (obj);
	} else {
		gpointer *lmf_addr = TlsGetValue (lmf_thread_id);
		MonoLMF *lmf;
		MonoMethod *m;

		g_assert (lmf_addr);
		lmf = *((MonoLMF **)lmf_addr);

		if (!lmf)
			mono_jit_abort (obj);

		m = lmf->method;

		*lmf_addr = lmf->previous_lmf;

		ctx->esi = lmf->esi;
		ctx->edi = lmf->edi;
		ctx->ebx = lmf->ebx;
		ctx->ebp = lmf->ebp;
		ctx->eip = lmf->eip;
		ctx->esp = (unsigned long)lmf;

		if (mono_object_isinst (obj, mono_defaults.exception_class)) {
			char  *strace = mono_string_to_utf8 (((MonoException*)obj)->stack_trace);
			char  *tmp;

			if (!strcmp (strace, "TODO: implement stack traces"))
				strace = g_strdup ("");

			tmp = g_strdup_printf ("%sin (unmanaged) %s.%s:%s ()\n", strace, m->klass->name_space,  
					       m->klass->name, m->name);

			g_free (strace);

			((MonoException*)obj)->stack_trace = mono_string_new (domain, tmp);
			g_free (tmp);
		}

		if (ctx->eip < (unsigned)mono_end_of_stack)
			arch_handle_exception (ctx, obj);
		else
			mono_jit_abort (obj);
	}

	g_assert_not_reached ();
}

static void
throw_exception (unsigned long eax, unsigned long ecx, unsigned long edx, unsigned long ebx,
		 unsigned long esi, unsigned long edi, unsigned long ebp, MonoObject *exc,
		 unsigned long eip,  unsigned long esp)
{
	struct sigcontext ctx;

	ctx.esp = esp;
	ctx.eip = eip;
	ctx.ebp = ebp;
	ctx.edi = edi;
	ctx.esi = esi;
	ctx.ebx = ebx;
	ctx.edx = edx;
	ctx.ecx = ecx;
	ctx.eax = eax;
	
	arch_handle_exception (&ctx, exc);

	g_assert_not_reached ();
}

/**
 * arch_get_throw_exception:
 *
 * Returns a function pointer which can be used to raise 
 * exceptions. The returned function has the following 
 * signature: void (*func) (MonoException *exc); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, mono_get_exception_arithmetic ()); 
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer 
arch_get_throw_exception (void)
{
	static guint8 start [24];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	x86_push_reg (code, X86_ESP);
	x86_push_membase (code, X86_ESP, 4); /* IP */
	x86_push_membase (code, X86_ESP, 12); /* exception */
	x86_push_reg (code, X86_EBP);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDX);
	x86_push_reg (code, X86_ECX);
	x86_push_reg (code, X86_EAX);
	x86_call_code (code, throw_exception);
	/* we should never reach this breakpoint */
	x86_breakpoint (code);

	g_assert ((code - start) < 24);
	return start;
}

/**
 * arch_get_throw_exception_by_name:
 *
 * Returns a function pointer which can be used to raise 
 * corlib exceptions. The returned function has the following 
 * signature: void (*func) (char *exc_name); 
 * For example to raise an arithmetic exception you can use:
 *
 * x86_push_imm (code, "ArithmeticException"); 
 * x86_call_code (code, arch_get_throw_exception ()); 
 *
 */
gpointer 
arch_get_throw_exception_by_name ()
{
	static guint8 start [32];
	static int inited = 0;
	guint8 *code;

	if (inited)
		return start;

	inited = 1;
	code = start;

	/* fixme: we do not save EAX, EDX, ECD - unsure if we need that */

	x86_push_membase (code, X86_ESP, 4); /* exception name */
	x86_push_imm (code, "System");
	x86_push_imm (code, mono_defaults.exception_class->image);
	x86_call_code (code, mono_exception_from_name);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);
	/* save the newly create object (overwrite exception name)*/
	x86_mov_membase_reg (code, X86_ESP, 4, X86_EAX, 4);
	x86_jump_code (code, arch_get_throw_exception ());

	g_assert ((code - start) < 32);

	return start;
}	

/*
 * this returns a helper method to invoke a method with a user supplied
 * stack frame. The returned method has the following signature:
 * invoke_method_with_frame ((gpointer code, gpointer frame, int frame_size);
 */
static gpointer
get_invoke_method_with_frame ()
{
	static guint8 *start;
	guint8 *code;

	if (start)
		return start;

	start = code = g_malloc (64);

	/* Prolog */
	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 16, 4);
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, X86_EAX);

	x86_push_membase (code, X86_EBP, 16);
	x86_push_membase (code, X86_EBP, 12);
	x86_lea_membase (code, X86_EAX, X86_ESP, 2*4);
	x86_push_reg (code, X86_EAX);
	x86_call_code (code, memcpy);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);

	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	x86_call_reg (code, X86_EAX);

	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 16, 4);
	x86_alu_reg_reg (code, X86_ADD, X86_ESP, X86_ECX);

	/* Epilog */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);
	
	g_assert ((code - start) < 64);

	return start;
}

/**
 * arch_runtime_invoke:
 * @method: the method to invoke
 * @obj: this pointer
 * @params: array of parameter values.
 *
 * TODO: very ugly piece of code. we should replace that with a method-specific 
 * trampoline (as suggested by Paolo).
 */
MonoObject*
arch_runtime_invoke (MonoMethod *method, void *obj, void **params)
{
	static guint64 (*invoke_int64) (gpointer code, gpointer frame, int frame_size) = NULL;
	static double (*invoke_double) (gpointer code, gpointer frame, int frame_size) = NULL;
	MonoObject *retval;
	MonoMethodSignature *sig = method->signature;
	int i, tmp, type, sp = 0;
	void *ret;
	int frame_size = 0;
	gpointer *frame;
	gpointer code;

	/* allocate ret object. */
	if (sig->ret->type == MONO_TYPE_VOID) {
		retval = NULL;
		ret = NULL;
	} else {
		MonoClass *klass = mono_class_from_mono_type (sig->ret);
		if (klass->valuetype) {
			retval = mono_object_new (mono_domain_get (), klass);
			ret = ((char*)retval) + sizeof (MonoObject);
		} else {
			ret = &retval;
		}
	}
   
	if (ISSTRUCT (sig->ret))
		frame_size += sizeof (gpointer);
	
	if (sig->hasthis) 
		frame_size += sizeof (gpointer);

	for (i = 0; i < sig->param_count; ++i) {
		int align;
		frame_size += mono_type_stack_size (sig->params [i], &align);
	}

	frame = alloca (frame_size);

	if (ISSTRUCT (sig->ret))
		frame [sp++] = ret;

	if (sig->hasthis) 
		frame [sp++] = obj;
		

	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			frame [sp++] = params [i];
			continue;
		}
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_U1:
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
			tmp = *(MonoBoolean*)params [i];
			frame [sp++] = (gpointer)tmp;			
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_I2:
		case MONO_TYPE_CHAR:
			tmp = *(gint16*)params [i];
			frame [sp++] = (gpointer)tmp;			
			break;
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U4:
		case MONO_TYPE_I4:
			frame [sp++] = (gpointer)*(gint32*)params [i];
			break;
#if SIZEOF_VOID_P == 8
		case MONO_TYPE_U:
		case MONO_TYPE_I:
#endif
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			frame [sp++] = (gpointer)*(gint32*)params [i];
			frame [sp++] = (gpointer)*(((gint32*)params [i]) + 1);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				type = sig->params [i]->data.klass->enum_basetype->type;
				goto handle_enum;
			} else {
				g_warning ("generic valutype %s not handled in runtime invoke", sig->params [i]->data.klass->name);
			}
			break;
		case MONO_TYPE_STRING:
			frame [sp++] = params [i];
			break;
		default:
			g_error ("type 0x%x not handled in invoke", sig->params [i]->type);
		}
	}

	if (method->addr)
		code = method->addr;
	else
		code = arch_compile_method (method);

	if (!invoke_int64)
		invoke_int64 = (gpointer)invoke_double = get_invoke_method_with_frame ();

	type = sig->ret->type;
handle_enum_2:
	switch (type) {
	case MONO_TYPE_VOID:
		invoke_int64 (code, frame, frame_size);		
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
#if SIZEOF_VOID_P == 4
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_STRING:
		*((guint32 *)ret) = invoke_int64 (code, frame, frame_size);		
		break;
#if SIZEOF_VOID_P == 8
	case MONO_TYPE_U:
	case MONO_TYPE_I:
#endif
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		*((guint64 *)ret) = invoke_int64 (code, frame, frame_size);		
		break;
	case MONO_TYPE_R4:
		*((float *)ret) = invoke_double (code, frame, frame_size);		
		break;
	case MONO_TYPE_R8:
		*((double *)ret) = invoke_double (code, frame, frame_size);		
		break;
	case MONO_TYPE_VALUETYPE:
		if (sig->params [i]->data.klass->enumtype) {
			type = sig->params [i]->data.klass->enum_basetype->type;
			goto handle_enum_2;
		} else 
			invoke_int64 (code, frame, frame_size);		
		break;
	default:
		g_error ("type 0x%x not handled in invoke", sig->params [i]->type);
	}

	return retval;
}



