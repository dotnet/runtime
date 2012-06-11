/*
 * mini-amd64.c: AMD64 backend for the Mono code generator
 *
 * Based on mini-x86.c.
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Patrik Torstensson
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2003 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */
#include "mini.h"
#include <string.h>
#include <math.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internal.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-tls.h>

#include "trace.h"
#include "ir-emit.h"
#include "mini-amd64.h"
#include "cpu-amd64.h"
#include "debugger-agent.h"
#include "mini-gc.h"

static gint lmf_tls_offset = -1;
static gint lmf_addr_tls_offset = -1;
static gint appdomain_tls_offset = -1;

#ifdef MONO_XEN_OPT
static gboolean optimize_for_xen = TRUE;
#else
#define optimize_for_xen 0
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define IS_IMM32(val) ((((guint64)val) >> 32) == 0)

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

#ifdef HOST_WIN32
/* Under windows, the calling convention is never stdcall */
#define CALLCONV_IS_STDCALL(call_conv) (FALSE)
#else
#define CALLCONV_IS_STDCALL(call_conv) ((call_conv) == MONO_CALL_STDCALL)
#endif

/* This mutex protects architecture specific caches */
#define mono_mini_arch_lock() EnterCriticalSection (&mini_arch_mutex)
#define mono_mini_arch_unlock() LeaveCriticalSection (&mini_arch_mutex)
static CRITICAL_SECTION mini_arch_mutex;

MonoBreakpointInfo
mono_breakpoint_info [MONO_BREAKPOINT_ARRAY_SIZE];

/* Structure used by the sequence points in AOTed code */
typedef struct {
	gpointer ss_trigger_page;
	gpointer bp_trigger_page;
	gpointer bp_addrs [MONO_ZERO_LEN_ARRAY];
} SeqPointInfo;

/*
 * The code generated for sequence points reads from this location, which is
 * made read-only when single stepping is enabled.
 */
static gpointer ss_trigger_page;

/* Enabled breakpoints read from this trigger page */
static gpointer bp_trigger_page;

/* The size of the breakpoint sequence */
static int breakpoint_size;

/* The size of the breakpoint instruction causing the actual fault */
static int breakpoint_fault_size;

/* The size of the single step instruction causing the actual fault */
static int single_step_fault_size;

#ifdef HOST_WIN32
/* On Win64 always reserve first 32 bytes for first four arguments */
#define ARGS_OFFSET 48
#else
#define ARGS_OFFSET 16
#endif
#define GP_SCRATCH_REG AMD64_R11

/*
 * AMD64 register usage:
 * - callee saved registers are used for global register allocation
 * - %r11 is used for materializing 64 bit constants in opcodes
 * - the rest is used for local allocation
 */

/*
 * Floating point comparison results:
 *                  ZF PF CF
 * A > B            0  0  0
 * A < B            0  0  1
 * A = B            1  0  0
 * A > B            0  0  0
 * UNORDERED        1  1  1
 */

const char*
mono_arch_regname (int reg)
{
	switch (reg) {
	case AMD64_RAX: return "%rax";
	case AMD64_RBX: return "%rbx";
	case AMD64_RCX: return "%rcx";
	case AMD64_RDX: return "%rdx";
	case AMD64_RSP: return "%rsp";	
	case AMD64_RBP: return "%rbp";
	case AMD64_RDI: return "%rdi";
	case AMD64_RSI: return "%rsi";
	case AMD64_R8: return "%r8";
	case AMD64_R9: return "%r9";
	case AMD64_R10: return "%r10";
	case AMD64_R11: return "%r11";
	case AMD64_R12: return "%r12";
	case AMD64_R13: return "%r13";
	case AMD64_R14: return "%r14";
	case AMD64_R15: return "%r15";
	}
	return "unknown";
}

static const char * packed_xmmregs [] = {
	"p:xmm0", "p:xmm1", "p:xmm2", "p:xmm3", "p:xmm4", "p:xmm5", "p:xmm6", "p:xmm7", "p:xmm8",
	"p:xmm9", "p:xmm10", "p:xmm11", "p:xmm12", "p:xmm13", "p:xmm14", "p:xmm15"
};

static const char * single_xmmregs [] = {
	"s:xmm0", "s:xmm1", "s:xmm2", "s:xmm3", "s:xmm4", "s:xmm5", "s:xmm6", "s:xmm7", "s:xmm8",
	"s:xmm9", "s:xmm10", "s:xmm11", "s:xmm12", "s:xmm13", "s:xmm14", "s:xmm15"
};

const char*
mono_arch_fregname (int reg)
{
	if (reg < AMD64_XMM_NREG)
		return single_xmmregs [reg];
	else
		return "unknown";
}

const char *
mono_arch_xregname (int reg)
{
	if (reg < AMD64_XMM_NREG)
		return packed_xmmregs [reg];
	else
		return "unknown";
}

G_GNUC_UNUSED static void
break_count (void)
{
}

G_GNUC_UNUSED static gboolean
debug_count (void)
{
	static int count = 0;
	count ++;

	if (!getenv ("COUNT"))
		return TRUE;

	if (count == atoi (getenv ("COUNT"))) {
		break_count ();
	}

	if (count > atoi (getenv ("COUNT"))) {
		return FALSE;
	}

	return TRUE;
}

static gboolean
debug_omit_fp (void)
{
#if 0
	return debug_count ();
#else
	return TRUE;
#endif
}

static inline gboolean
amd64_is_near_call (guint8 *code)
{
	/* Skip REX */
	if ((code [0] >= 0x40) && (code [0] <= 0x4f))
		code += 1;

	return code [0] == 0xe8;
}

#ifdef __native_client_codegen__

/* Keep track of instruction "depth", that is, the level of sub-instruction */
/* for any given instruction.  For instance, amd64_call_reg resolves to     */
/* amd64_call_reg_internal, which uses amd64_alu_* macros, etc.             */
/* We only want to force bundle alignment for the top level instruction,    */
/* so NaCl pseudo-instructions can be implemented with sub instructions.    */
static MonoNativeTlsKey nacl_instruction_depth;

static MonoNativeTlsKey nacl_rex_tag;
static MonoNativeTlsKey nacl_legacy_prefix_tag;

void
amd64_nacl_clear_legacy_prefix_tag ()
{
	mono_native_tls_set_value (nacl_legacy_prefix_tag, NULL);
}

void
amd64_nacl_tag_legacy_prefix (guint8* code)
{
	if (mono_native_tls_get_value (nacl_legacy_prefix_tag) == NULL)
		mono_native_tls_set_value (nacl_legacy_prefix_tag, code);
}

void
amd64_nacl_tag_rex (guint8* code)
{
	mono_native_tls_set_value (nacl_rex_tag, code);
}

guint8*
amd64_nacl_get_legacy_prefix_tag ()
{
	return (guint8*)mono_native_tls_get_value (nacl_legacy_prefix_tag);
}

guint8*
amd64_nacl_get_rex_tag ()
{
	return (guint8*)mono_native_tls_get_value (nacl_rex_tag);
}

/* Increment the instruction "depth" described above */
void
amd64_nacl_instruction_pre ()
{
	intptr_t depth = (intptr_t) mono_native_tls_get_value (nacl_instruction_depth);
	depth++;
	mono_native_tls_set_value (nacl_instruction_depth, (gpointer)depth);
}

/* amd64_nacl_instruction_post: Decrement instruction "depth", force bundle */
/* alignment if depth == 0 (top level instruction)                          */
/* IN: start, end    pointers to instruction beginning and end              */
/* OUT: start, end   pointers to beginning and end after possible alignment */
/* GLOBALS: nacl_instruction_depth     defined above                        */
void
amd64_nacl_instruction_post (guint8 **start, guint8 **end)
{
	intptr_t depth = (intptr_t) mono_native_tls_get_value (nacl_instruction_depth);
	depth--;
	mono_native_tls_set_value (nacl_instruction_depth, (void*)depth);

	g_assert ( depth >= 0 );
	if (depth == 0) {
  		uintptr_t space_in_block;
		uintptr_t instlen;
		guint8 *prefix = amd64_nacl_get_legacy_prefix_tag ();
		/* if legacy prefix is present, and if it was emitted before */
		/* the start of the instruction sequence, adjust the start   */
		if (prefix != NULL && prefix < *start) {
			g_assert (*start - prefix <= 3);/* only 3 are allowed */
			*start = prefix;
		}
		space_in_block = kNaClAlignment - ((uintptr_t)(*start) & kNaClAlignmentMask);
		instlen = (uintptr_t)(*end - *start);
		/* Only check for instructions which are less than        */
		/* kNaClAlignment. The only instructions that should ever */
		/* be that long are call sequences, which are already     */
		/* padded out to align the return to the next bundle.     */
		if (instlen > space_in_block && instlen < kNaClAlignment) {
			const size_t MAX_NACL_INST_LENGTH = kNaClAlignment;
  			guint8 copy_of_instruction[MAX_NACL_INST_LENGTH];
  			const size_t length = (size_t)((*end)-(*start));
  			g_assert (length < MAX_NACL_INST_LENGTH);
			
  			memcpy (copy_of_instruction, *start, length);
			*start = mono_arch_nacl_pad (*start, space_in_block);
			memcpy (*start, copy_of_instruction, length);
			*end = *start + length;
		}
		amd64_nacl_clear_legacy_prefix_tag ();
		amd64_nacl_tag_rex (NULL);
	}
}

/* amd64_nacl_membase_handler: ensure all access to memory of the form      */
/*   OFFSET(%rXX) is sandboxed.  For allowable base registers %rip, %rbp,   */
/*   %rsp, and %r15, emit the membase as usual.  For all other registers,   */
/*   make sure the upper 32-bits are cleared, and use that register in the  */
/*   index field of a new address of this form: OFFSET(%r15,%eXX,1)         */
/* IN:      code                                                            */
/*             pointer to current instruction stream (in the                */
/*             middle of an instruction, after opcode is emitted)           */
/*          basereg/offset/dreg                                             */
/*             operands of normal membase address                           */
/* OUT:     code                                                            */
/*             pointer to the end of the membase/memindex emit              */
/* GLOBALS: nacl_rex_tag                                                    */
/*             position in instruction stream that rex prefix was emitted   */
/*          nacl_legacy_prefix_tag                                          */
/*             (possibly NULL) position in instruction of legacy x86 prefix */
void
amd64_nacl_membase_handler (guint8** code, gint8 basereg, gint32 offset, gint8 dreg)
{
	gint8 true_basereg = basereg;

	/* Cache these values, they might change  */
 	/* as new instructions are emitted below. */
	guint8* rex_tag = amd64_nacl_get_rex_tag ();
	guint8* legacy_prefix_tag = amd64_nacl_get_legacy_prefix_tag ();

	/* 'basereg' is given masked to 0x7 at this point, so check */
	/* the rex prefix to see if this is an extended register.   */
	if ((rex_tag != NULL) && IS_REX(*rex_tag) && (*rex_tag & AMD64_REX_B)) {
		true_basereg |= 0x8;
	}

#define X86_LEA_OPCODE (0x8D)

	if (!amd64_is_valid_nacl_base (true_basereg) && (*(*code-1) != X86_LEA_OPCODE)) {
		guint8* old_instruction_start;
		
		/* This will hold the 'mov %eXX, %eXX' that clears the upper */
		/* 32-bits of the old base register (new index register)     */
		guint8 buf[32];
		guint8* buf_ptr = buf;
		size_t insert_len;

		g_assert (rex_tag != NULL);

		if (IS_REX(*rex_tag)) {
			/* The old rex.B should be the new rex.X */
			if (*rex_tag & AMD64_REX_B) {
				*rex_tag |= AMD64_REX_X;
			}
			/* Since our new base is %r15 set rex.B */
			*rex_tag |= AMD64_REX_B;
		} else {
			/* Shift the instruction by one byte  */
			/* so we can insert a rex prefix      */
			memmove (rex_tag + 1, rex_tag, (size_t)(*code - rex_tag));
			*code += 1;
			/* New rex prefix only needs rex.B for %r15 base */
			*rex_tag = AMD64_REX(AMD64_REX_B);
		}

		if (legacy_prefix_tag) {
			old_instruction_start = legacy_prefix_tag;
		} else {
			old_instruction_start = rex_tag;
		}
		
		/* Clears the upper 32-bits of the previous base register */
		amd64_mov_reg_reg_size (buf_ptr, true_basereg, true_basereg, 4);
		insert_len = buf_ptr - buf;
		
		/* Move the old instruction forward to make */
		/* room for 'mov' stored in 'buf_ptr'       */
		memmove (old_instruction_start + insert_len, old_instruction_start, (size_t)(*code - old_instruction_start));
		*code += insert_len;
		memcpy (old_instruction_start, buf, insert_len);

		/* Sandboxed replacement for the normal membase_emit */
		x86_memindex_emit (*code, dreg, AMD64_R15, offset, basereg, 0);
		
	} else {
		/* Normal default behavior, emit membase memory location */
		x86_membase_emit_body (*code, dreg, basereg, offset);
	}
}


static inline unsigned char*
amd64_skip_nops (unsigned char* code)
{
	guint8 in_nop;
	do {
		in_nop = 0;
		if (   code[0] == 0x90) {
			in_nop = 1;
			code += 1;
		}
		if (   code[0] == 0x66 && code[1] == 0x90) {
			in_nop = 1;
			code += 2;
		}
		if (code[0] == 0x0f && code[1] == 0x1f
		 && code[2] == 0x00) {
			in_nop = 1;
			code += 3;
		}
		if (code[0] == 0x0f && code[1] == 0x1f
		 && code[2] == 0x40 && code[3] == 0x00) {
			in_nop = 1;
			code += 4;
		}
		if (code[0] == 0x0f && code[1] == 0x1f
		 && code[2] == 0x44 && code[3] == 0x00
		 && code[4] == 0x00) {
			in_nop = 1;
			code += 5;
		}
		if (code[0] == 0x66 && code[1] == 0x0f
		 && code[2] == 0x1f && code[3] == 0x44
		 && code[4] == 0x00 && code[5] == 0x00) {
			in_nop = 1;
			code += 6;
		}
		if (code[0] == 0x0f && code[1] == 0x1f
		 && code[2] == 0x80 && code[3] == 0x00
		 && code[4] == 0x00 && code[5] == 0x00
		 && code[6] == 0x00) {
			in_nop = 1;
			code += 7;
		}
		if (code[0] == 0x0f && code[1] == 0x1f
		 && code[2] == 0x84 && code[3] == 0x00
		 && code[4] == 0x00 && code[5] == 0x00
		 && code[6] == 0x00 && code[7] == 0x00) {
			in_nop = 1;
			code += 8;
		}
	} while ( in_nop );
	return code;
}

guint8*
mono_arch_nacl_skip_nops (guint8* code)
{
  return amd64_skip_nops(code);
}

#endif /*__native_client_codegen__*/

static inline void 
amd64_patch (unsigned char* code, gpointer target)
{
	guint8 rex = 0;

#ifdef __native_client_codegen__
	code = amd64_skip_nops (code);
#endif
#if defined(__native_client_codegen__) && defined(__native_client__)
	if (nacl_is_code_address (code)) {
		/* For tail calls, code is patched after being installed */
		/* but not through the normal "patch callsite" method.   */
		unsigned char buf[kNaClAlignment];
		unsigned char *aligned_code = (uintptr_t)code & ~kNaClAlignmentMask;
		int ret;
		memcpy (buf, aligned_code, kNaClAlignment);
		/* Patch a temp buffer of bundle size, */
		/* then install to actual location.    */
		amd64_patch (buf + ((uintptr_t)code - (uintptr_t)aligned_code), target);
		ret = nacl_dyncode_modify (aligned_code, buf, kNaClAlignment);
		g_assert (ret == 0);
		return;
	}
	target = nacl_modify_patch_target (target);
#endif

	/* Skip REX */
	if ((code [0] >= 0x40) && (code [0] <= 0x4f)) {
		rex = code [0];
		code += 1;
	}

	if ((code [0] & 0xf8) == 0xb8) {
		/* amd64_set_reg_template */
		*(guint64*)(code + 1) = (guint64)target;
	}
	else if ((code [0] == 0x8b) && rex && x86_modrm_mod (code [1]) == 0 && x86_modrm_rm (code [1]) == 5) {
		/* mov 0(%rip), %dreg */
		*(guint32*)(code + 2) = (guint32)(guint64)target - 7;
	}
	else if ((code [0] == 0xff) && (code [1] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		*(guint32*)(code + 2) = ((guint32)(guint64)target) - 7;
	}
	else if (code [0] == 0xe8) {
		/* call <DISP> */
		gint64 disp = (guint8*)target - (guint8*)code;
		g_assert (amd64_is_imm32 (disp));
		x86_patch (code, (unsigned char*)target);
	}
	else
		x86_patch (code, (unsigned char*)target);
}

void 
mono_amd64_patch (unsigned char* code, gpointer target)
{
	amd64_patch (code, target);
}

typedef enum {
	ArgInIReg,
	ArgInFloatSSEReg,
	ArgInDoubleSSEReg,
	ArgOnStack,
	ArgValuetypeInReg,
	ArgValuetypeAddrInIReg,
	ArgNone /* only in pair_storage */
} ArgStorage;

typedef struct {
	gint16 offset;
	gint8  reg;
	ArgStorage storage;

	/* Only if storage == ArgValuetypeInReg */
	ArgStorage pair_storage [2];
	gint8 pair_regs [2];
	int nregs;
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	guint32 freg_usage;
	gboolean need_stack_align;
	gboolean vtype_retaddr;
	/* The index of the vret arg in the argument list */
	int vret_arg_index;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a) if (cfg->verbose_level > 1) a

#ifdef HOST_WIN32
#define PARAM_REGS 4

static AMD64_Reg_No param_regs [] = { AMD64_RCX, AMD64_RDX, AMD64_R8, AMD64_R9 };

static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };
#else
#define PARAM_REGS 6
 
static AMD64_Reg_No param_regs [] = { AMD64_RDI, AMD64_RSI, AMD64_RDX, AMD64_RCX, AMD64_R8, AMD64_R9 };

 static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };
#endif

static void inline
add_general (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo)
{
    ainfo->offset = *stack_size;

    if (*gr >= PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		/* Since the same stack slot size is used for all arg */
		/*  types, it needs to be big enough to hold them all */
		(*stack_size) += sizeof(mgreg_t);
    }
    else {
		ainfo->storage = ArgInIReg;
		ainfo->reg = param_regs [*gr];
		(*gr) ++;
    }
}

#ifdef HOST_WIN32
#define FLOAT_PARAM_REGS 4
#else
#define FLOAT_PARAM_REGS 8
#endif

static void inline
add_float (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo, gboolean is_double)
{
    ainfo->offset = *stack_size;

    if (*gr >= FLOAT_PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		/* Since the same stack slot size is used for both float */
		/*  types, it needs to be big enough to hold them both */
		(*stack_size) += sizeof(mgreg_t);
    }
    else {
		/* A double register */
		if (is_double)
			ainfo->storage = ArgInDoubleSSEReg;
		else
			ainfo->storage = ArgInFloatSSEReg;
		ainfo->reg = *gr;
		(*gr) += 1;
    }
}

typedef enum ArgumentClass {
	ARG_CLASS_NO_CLASS,
	ARG_CLASS_MEMORY,
	ARG_CLASS_INTEGER,
	ARG_CLASS_SSE
} ArgumentClass;

static ArgumentClass
merge_argument_class_from_type (MonoType *type, ArgumentClass class1)
{
	ArgumentClass class2 = ARG_CLASS_NO_CLASS;
	MonoType *ptype;

	ptype = mini_type_get_underlying_type (NULL, type);
	switch (ptype->type) {
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
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		class2 = ARG_CLASS_INTEGER;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
#ifdef HOST_WIN32
		class2 = ARG_CLASS_INTEGER;
#else
		class2 = ARG_CLASS_SSE;
#endif
		break;

	case MONO_TYPE_TYPEDBYREF:
		g_assert_not_reached ();

	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ptype)) {
			class2 = ARG_CLASS_INTEGER;
			break;
		}
		/* fall through */
	case MONO_TYPE_VALUETYPE: {
		MonoMarshalType *info = mono_marshal_load_type_info (ptype->data.klass);
		int i;

		for (i = 0; i < info->num_fields; ++i) {
			class2 = class1;
			class2 = merge_argument_class_from_type (info->fields [i].field->type, class2);
		}
		break;
	}
	default:
		g_assert_not_reached ();
	}

	/* Merge */
	if (class1 == class2)
		;
	else if (class1 == ARG_CLASS_NO_CLASS)
		class1 = class2;
	else if ((class1 == ARG_CLASS_MEMORY) || (class2 == ARG_CLASS_MEMORY))
		class1 = ARG_CLASS_MEMORY;
	else if ((class1 == ARG_CLASS_INTEGER) || (class2 == ARG_CLASS_INTEGER))
		class1 = ARG_CLASS_INTEGER;
	else
		class1 = ARG_CLASS_SSE;

	return class1;
}
#ifdef __native_client_codegen__
const guint kNaClAlignment = kNaClAlignmentAMD64;
const guint kNaClAlignmentMask = kNaClAlignmentMaskAMD64;

/* Default alignment for Native Client is 32-byte. */
gint8 nacl_align_byte = -32; /* signed version of 0xe0 */

/* mono_arch_nacl_pad: Add pad bytes of alignment instructions at code,  */
/* Check that alignment doesn't cross an alignment boundary.             */
guint8*
mono_arch_nacl_pad(guint8 *code, int pad)
{
	const int kMaxPadding = 8; /* see amd64-codegen.h:amd64_padding_size() */

	if (pad == 0) return code;
	/* assertion: alignment cannot cross a block boundary */
	g_assert (((uintptr_t)code & (~kNaClAlignmentMask)) ==
	         (((uintptr_t)code + pad - 1) & (~kNaClAlignmentMask)));
	while (pad >= kMaxPadding) {
		amd64_padding (code, kMaxPadding);
		pad -= kMaxPadding;
	}
	if (pad != 0) amd64_padding (code, pad);
	return code;
}
#endif

static void
add_valuetype (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, ArgInfo *ainfo, MonoType *type,
			   gboolean is_return,
			   guint32 *gr, guint32 *fr, guint32 *stack_size)
{
	guint32 size, quad, nquads, i;
	/* Keep track of the size used in each quad so we can */
	/* use the right size when copying args/return vars.  */
	guint32 quadsize [2] = {8, 8};
	ArgumentClass args [2];
	MonoMarshalType *info = NULL;
	MonoClass *klass;
	MonoGenericSharingContext tmp_gsctx;
	gboolean pass_on_stack = FALSE;
	
	/* 
	 * The gsctx currently contains no data, it is only used for checking whenever
	 * open types are allowed, some callers like mono_arch_get_argument_info ()
	 * don't pass it to us, so work around that.
	 */
	if (!gsctx)
		gsctx = &tmp_gsctx;

	klass = mono_class_from_mono_type (type);
	size = mini_type_stack_size_full (gsctx, &klass->byval_arg, NULL, sig->pinvoke);
#ifndef HOST_WIN32
	if (!sig->pinvoke && !disable_vtypes_in_regs && ((is_return && (size == 8)) || (!is_return && (size <= 16)))) {
		/* We pass and return vtypes of size 8 in a register */
	} else if (!sig->pinvoke || (size == 0) || (size > 16)) {
		pass_on_stack = TRUE;
	}
#else
	if (!sig->pinvoke) {
		pass_on_stack = TRUE;
	}
#endif

	/* If this struct can't be split up naturally into 8-byte */
	/* chunks (registers), pass it on the stack.              */
	if (sig->pinvoke && !pass_on_stack) {
		guint32 align;
		guint32 field_size;

		info = mono_marshal_load_type_info (klass);
		g_assert(info);
		for (i = 0; i < info->num_fields; ++i) {
			field_size = mono_marshal_type_size (info->fields [i].field->type, 
							   info->fields [i].mspec, 
							   &align, TRUE, klass->unicode);
			if ((info->fields [i].offset < 8) && (info->fields [i].offset + field_size) > 8) {
				pass_on_stack = TRUE;
				break;
			}
		}
	}

	if (pass_on_stack) {
		/* Allways pass in memory */
		ainfo->offset = *stack_size;
		*stack_size += ALIGN_TO (size, 8);
		ainfo->storage = ArgOnStack;

		return;
	}

	/* FIXME: Handle structs smaller than 8 bytes */
	//if ((size % 8) != 0)
	//	NOT_IMPLEMENTED;

	if (size > 8)
		nquads = 2;
	else
		nquads = 1;

	if (!sig->pinvoke) {
		/* Always pass in 1 or 2 integer registers */
		args [0] = ARG_CLASS_INTEGER;
		args [1] = ARG_CLASS_INTEGER;
		/* Only the simplest cases are supported */
		if (is_return && nquads != 1) {
			args [0] = ARG_CLASS_MEMORY;
			args [1] = ARG_CLASS_MEMORY;
		}
	} else {
		/*
		 * Implement the algorithm from section 3.2.3 of the X86_64 ABI.
		 * The X87 and SSEUP stuff is left out since there are no such types in
		 * the CLR.
		 */
		info = mono_marshal_load_type_info (klass);
		g_assert (info);

#ifndef HOST_WIN32
		if (info->native_size > 16) {
			ainfo->offset = *stack_size;
			*stack_size += ALIGN_TO (info->native_size, 8);
			ainfo->storage = ArgOnStack;

			return;
		}
#else
		switch (info->native_size) {
		case 1: case 2: case 4: case 8:
			break;
		default:
			if (is_return) {
				ainfo->storage = ArgOnStack;
				ainfo->offset = *stack_size;
				*stack_size += ALIGN_TO (info->native_size, 8);
			}
			else {
				ainfo->storage = ArgValuetypeAddrInIReg;

				if (*gr < PARAM_REGS) {
					ainfo->pair_storage [0] = ArgInIReg;
					ainfo->pair_regs [0] = param_regs [*gr];
					(*gr) ++;
				}
				else {
					ainfo->pair_storage [0] = ArgOnStack;
					ainfo->offset = *stack_size;
					*stack_size += 8;
				}
			}

			return;
		}
#endif

		args [0] = ARG_CLASS_NO_CLASS;
		args [1] = ARG_CLASS_NO_CLASS;
		for (quad = 0; quad < nquads; ++quad) {
			int size;
			guint32 align;
			ArgumentClass class1;
		
			if (info->num_fields == 0)
				class1 = ARG_CLASS_MEMORY;
			else
				class1 = ARG_CLASS_NO_CLASS;
			for (i = 0; i < info->num_fields; ++i) {
				size = mono_marshal_type_size (info->fields [i].field->type, 
											   info->fields [i].mspec, 
											   &align, TRUE, klass->unicode);
				if ((info->fields [i].offset < 8) && (info->fields [i].offset + size) > 8) {
					/* Unaligned field */
					NOT_IMPLEMENTED;
				}

				/* Skip fields in other quad */
				if ((quad == 0) && (info->fields [i].offset >= 8))
					continue;
				if ((quad == 1) && (info->fields [i].offset < 8))
					continue;

				/* How far into this quad this data extends.*/
				/* (8 is size of quad) */
				quadsize [quad] = info->fields [i].offset + size - (quad * 8);

				class1 = merge_argument_class_from_type (info->fields [i].field->type, class1);
			}
			g_assert (class1 != ARG_CLASS_NO_CLASS);
			args [quad] = class1;
		}
	}

	/* Post merger cleanup */
	if ((args [0] == ARG_CLASS_MEMORY) || (args [1] == ARG_CLASS_MEMORY))
		args [0] = args [1] = ARG_CLASS_MEMORY;

	/* Allocate registers */
	{
		int orig_gr = *gr;
		int orig_fr = *fr;

		ainfo->storage = ArgValuetypeInReg;
		ainfo->pair_storage [0] = ainfo->pair_storage [1] = ArgNone;
		ainfo->nregs = nquads;
		for (quad = 0; quad < nquads; ++quad) {
			switch (args [quad]) {
			case ARG_CLASS_INTEGER:
				if (*gr >= PARAM_REGS)
					args [quad] = ARG_CLASS_MEMORY;
				else {
					ainfo->pair_storage [quad] = ArgInIReg;
					if (is_return)
						ainfo->pair_regs [quad] = return_regs [*gr];
					else
						ainfo->pair_regs [quad] = param_regs [*gr];
					(*gr) ++;
				}
				break;
			case ARG_CLASS_SSE:
				if (*fr >= FLOAT_PARAM_REGS)
					args [quad] = ARG_CLASS_MEMORY;
				else {
					if (quadsize[quad] <= 4)
						ainfo->pair_storage [quad] = ArgInFloatSSEReg;
					else ainfo->pair_storage [quad] = ArgInDoubleSSEReg;
					ainfo->pair_regs [quad] = *fr;
					(*fr) ++;
				}
				break;
			case ARG_CLASS_MEMORY:
				break;
			default:
				g_assert_not_reached ();
			}
		}

		if ((args [0] == ARG_CLASS_MEMORY) || (args [1] == ARG_CLASS_MEMORY)) {
			/* Revert possible register assignments */
			*gr = orig_gr;
			*fr = orig_fr;

			ainfo->offset = *stack_size;
			if (sig->pinvoke)
				*stack_size += ALIGN_TO (info->native_size, 8);
			else
				*stack_size += nquads * sizeof(mgreg_t);
			ainfo->storage = ArgOnStack;
		}
	}
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * For AMD64, see the "System V ABI, x86-64 Architecture Processor Supplement 
 * Draft Version 0.23" document for more information.
 */
static CallInfo*
get_call_info (MonoGenericSharingContext *gsctx, MonoMemPool *mp, MonoMethodSignature *sig)
{
	guint32 i, gr, fr, pstart;
	MonoType *ret_type;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;
	gboolean is_pinvoke = sig->pinvoke;

	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	cinfo->nargs = n;

	gr = 0;
	fr = 0;

	/* return value */
	{
		ret_type = mini_type_get_underlying_type (gsctx, sig->ret);
		switch (ret_type->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = AMD64_RAX;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = AMD64_RAX;
			break;
		case MONO_TYPE_R4:
			cinfo->ret.storage = ArgInFloatSSEReg;
			cinfo->ret.reg = AMD64_XMM0;
			break;
		case MONO_TYPE_R8:
			cinfo->ret.storage = ArgInDoubleSSEReg;
			cinfo->ret.reg = AMD64_XMM0;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ret_type)) {
				cinfo->ret.storage = ArgInIReg;
				cinfo->ret.reg = AMD64_RAX;
				break;
			}
			/* fall through */
		case MONO_TYPE_VALUETYPE: {
			guint32 tmp_gr = 0, tmp_fr = 0, tmp_stacksize = 0;

			add_valuetype (gsctx, sig, &cinfo->ret, sig->ret, TRUE, &tmp_gr, &tmp_fr, &tmp_stacksize);
			if (cinfo->ret.storage == ArgOnStack) {
				cinfo->vtype_retaddr = TRUE;
				/* The caller passes the address where the value is stored */
			}
			break;
		}
		case MONO_TYPE_TYPEDBYREF:
			/* Same as a valuetype with size 24 */
			cinfo->vtype_retaddr = TRUE;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	pstart = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (cinfo->vtype_retaddr && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_type_get_underlying_type (gsctx, sig->params [0]))))) {
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0);
		} else {
			add_general (&gr, &stack_size, &cinfo->args [sig->hasthis + 0]);
			pstart = 1;
		}
		add_general (&gr, &stack_size, &cinfo->ret);
		cinfo->vret_arg_index = 1;
	} else {
		/* this */
		if (sig->hasthis)
			add_general (&gr, &stack_size, cinfo->args + 0);

		if (cinfo->vtype_retaddr)
			add_general (&gr, &stack_size, &cinfo->ret);
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == 0)) {
		gr = PARAM_REGS;
		fr = FLOAT_PARAM_REGS;
		
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie);
	}

	for (i = pstart; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];
		MonoType *ptype;

#ifdef HOST_WIN32
		/* The float param registers and other param registers must be the same index on Windows x64.*/
		if (gr > fr)
			fr = gr;
		else if (fr > gr)
			gr = fr;
#endif

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* We allways pass the sig cookie on the stack for simplicity */
			/* 
			 * Prevent implicit arguments + the sig cookie from being passed 
			 * in registers.
			 */
			gr = PARAM_REGS;
			fr = FLOAT_PARAM_REGS;

			/* Emit the signature cookie just before the implicit arguments */
			add_general (&gr, &stack_size, &cinfo->sig_cookie);
		}

		ptype = mini_type_get_underlying_type (gsctx, sig->params [i]);
		switch (ptype->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ptype)) {
				add_general (&gr, &stack_size, ainfo);
				break;
			}
			/* fall through */
		case MONO_TYPE_VALUETYPE:
			add_valuetype (gsctx, sig, ainfo, sig->params [i], FALSE, &gr, &fr, &stack_size);
			break;
		case MONO_TYPE_TYPEDBYREF:
#ifdef HOST_WIN32
			add_valuetype (gsctx, sig, ainfo, sig->params [i], FALSE, &gr, &fr, &stack_size);
#else
			stack_size += sizeof (MonoTypedRef);
			ainfo->storage = ArgOnStack;
#endif
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_R4:
			add_float (&fr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_R8:
			add_float (&fr, &stack_size, ainfo, TRUE);
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n > 0) && (sig->sentinelpos == sig->param_count)) {
		gr = PARAM_REGS;
		fr = FLOAT_PARAM_REGS;
		
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie);
	}

#ifdef HOST_WIN32
	// There always is 32 bytes reserved on the stack when calling on Winx64
	stack_size += 0x20;
#endif

#ifndef MONO_AMD64_NO_PUSHES
	if (stack_size & 0x8) {
		/* The AMD64 ABI requires each stack frame to be 16 byte aligned */
		cinfo->need_stack_align = TRUE;
		stack_size += 8;
	}
#endif

	cinfo->stack_usage = stack_size;
	cinfo->reg_usage = gr;
	cinfo->freg_usage = fr;
	return cinfo;
}

/*
 * mono_arch_get_argument_info:
 * @csig:  a method signature
 * @param_count: the number of parameters to consider
 * @arg_info: an array to store the result infos
 *
 * Gathers information on parameters such as size, alignment and
 * padding. arg_info should be large enought to hold param_count + 1 entries. 
 *
 * Returns the size of the argument area on the stack.
 */
int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	int k;
	CallInfo *cinfo = get_call_info (NULL, NULL, csig);
	guint32 args_size = cinfo->stack_usage;

	/* The arguments are saved to a stack area in mono_arch_instrument_prolog */
	if (csig->hasthis) {
		arg_info [0].offset = 0;
	}

	for (k = 0; k < param_count; k++) {
		arg_info [k + 1].offset = ((k + csig->hasthis) * 8);
		/* FIXME: */
		arg_info [k + 1].size = 0;
	}

	g_free (cinfo);

	return args_size;
}

gboolean
mono_amd64_tail_call_supported (MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig)
{
	CallInfo *c1, *c2;
	gboolean res;

	c1 = get_call_info (NULL, NULL, caller_sig);
	c2 = get_call_info (NULL, NULL, callee_sig);
	res = c1->stack_usage >= c2->stack_usage;
	if (callee_sig->ret && MONO_TYPE_ISSTRUCT (callee_sig->ret) && c2->ret.storage != ArgValuetypeInReg)
		/* An address on the callee's stack is passed as the first argument */
		res = FALSE;

	g_free (c1);
	g_free (c2);

	return res;
}

static int 
cpuid (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx)
{
#if defined(MONO_CROSS_COMPILE)
	return 0;
#else
#ifndef _MSC_VER
	__asm__ __volatile__ ("cpuid"
		: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
		: "a" (id));
#else
	int info[4];
	__cpuid(info, id);
	*p_eax = info[0];
	*p_ebx = info[1];
	*p_ecx = info[2];
	*p_edx = info[3];
#endif
	return 1;
#endif
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
#ifndef _MSC_VER
	guint16 fpcw;

	/* spec compliance requires running with double precision */
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
	fpcw &= ~X86_FPCW_PRECC_MASK;
	fpcw |= X86_FPCW_PREC_DOUBLE;
	__asm__  __volatile__ ("fldcw %0\n": : "m" (fpcw));
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
#else
	/* TODO: This is crashing on Win64 right now.
	* _control87 (_PC_53, MCW_PC);
	*/
#endif
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
	int flags;

	InitializeCriticalSection (&mini_arch_mutex);
#if defined(__native_client_codegen__)
	mono_native_tls_alloc (&nacl_instruction_depth, NULL);
	mono_native_tls_set_value (nacl_instruction_depth, (gpointer)0);
	mono_native_tls_alloc (&nacl_rex_tag, NULL);
	mono_native_tls_alloc (&nacl_legacy_prefix_tag, NULL);
#endif

#ifdef MONO_ARCH_NOMAP32BIT
	flags = MONO_MMAP_READ;
	/* amd64_mov_reg_imm () + amd64_mov_reg_membase () */
	breakpoint_size = 13;
	breakpoint_fault_size = 3;
#else
	flags = MONO_MMAP_READ|MONO_MMAP_32BIT;
	/* amd64_mov_reg_mem () */
	breakpoint_size = 8;
	breakpoint_fault_size = 8;
#endif

	/* amd64_alu_membase_imm_size (code, X86_CMP, AMD64_R11, 0, 0, 4); */
	single_step_fault_size = 4;

	ss_trigger_page = mono_valloc (NULL, mono_pagesize (), flags);
	bp_trigger_page = mono_valloc (NULL, mono_pagesize (), flags);
	mono_mprotect (bp_trigger_page, mono_pagesize (), 0);

	mono_aot_register_jit_icall ("mono_amd64_throw_exception", mono_amd64_throw_exception);
	mono_aot_register_jit_icall ("mono_amd64_throw_corlib_exception", mono_amd64_throw_corlib_exception);
	mono_aot_register_jit_icall ("mono_amd64_get_original_ip", mono_amd64_get_original_ip);
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
	DeleteCriticalSection (&mini_arch_mutex);
#if defined(__native_client_codegen__)
	mono_native_tls_free (nacl_instruction_depth);
	mono_native_tls_free (nacl_rex_tag);
	mono_native_tls_free (nacl_legacy_prefix_tag);
#endif
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	int eax, ebx, ecx, edx;
	guint32 opts = 0;

	*exclude_mask = 0;
	/* Feature Flags function, flags returned in EDX. */
	if (cpuid (1, &eax, &ebx, &ecx, &edx)) {
		if (edx & (1 << 15)) {
			opts |= MONO_OPT_CMOV;
			if (edx & 1)
				opts |= MONO_OPT_FCMOV;
			else
				*exclude_mask |= MONO_OPT_FCMOV;
		} else
			*exclude_mask |= MONO_OPT_CMOV;
	}

	return opts;
}

/*
 * This function test for all SSE functions supported.
 *
 * Returns a bitmask corresponding to all supported versions.
 * 
 */
guint32
mono_arch_cpu_enumerate_simd_versions (void)
{
	int eax, ebx, ecx, edx;
	guint32 sse_opts = 0;

	if (cpuid (1, &eax, &ebx, &ecx, &edx)) {
		if (edx & (1 << 25))
			sse_opts |= SIMD_VERSION_SSE1;
		if (edx & (1 << 26))
			sse_opts |= SIMD_VERSION_SSE2;
		if (ecx & (1 << 0))
			sse_opts |= SIMD_VERSION_SSE3;
		if (ecx & (1 << 9))
			sse_opts |= SIMD_VERSION_SSSE3;
		if (ecx & (1 << 19))
			sse_opts |= SIMD_VERSION_SSE41;
		if (ecx & (1 << 20))
			sse_opts |= SIMD_VERSION_SSE42;
	}

	/* Yes, all this needs to be done to check for sse4a.
	   See: "Amd: CPUID Specification"
	 */
	if (cpuid (0x80000000, &eax, &ebx, &ecx, &edx)) {
		/* eax greater or equal than 0x80000001, ebx = 'htuA', ecx = DMAc', edx = 'itne'*/
		if ((((unsigned int) eax) >= 0x80000001) && (ebx == 0x68747541) && (ecx == 0x444D4163) && (edx == 0x69746E65)) {
			cpuid (0x80000001, &eax, &ebx, &ecx, &edx);
			if (ecx & (1 << 6))
				sse_opts |= SIMD_VERSION_SSE4a;
		}
	}

	return sse_opts;	
}

#ifndef DISABLE_JIT

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		if (mono_is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

/**
 * mono_arch_compute_omit_fp:
 *
 *   Determine whenever the frame pointer can be eliminated.
 */
static void
mono_arch_compute_omit_fp (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i, locals_size;
	CallInfo *cinfo;

	if (cfg->arch.omit_fp_computed)
		return;

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	/*
	 * FIXME: Remove some of the restrictions.
	 */
	cfg->arch.omit_fp = TRUE;
	cfg->arch.omit_fp_computed = TRUE;

#ifdef __native_client_codegen__
	/* NaCl modules may not change the value of RBP, so it cannot be */
	/* used as a normal register, but it can be used as a frame pointer*/
	cfg->disable_omit_fp = TRUE;
	cfg->arch.omit_fp = FALSE;
#endif

	if (cfg->disable_omit_fp)
		cfg->arch.omit_fp = FALSE;

	if (!debug_omit_fp ())
		cfg->arch.omit_fp = FALSE;
	/*
	if (cfg->method->save_lmf)
		cfg->arch.omit_fp = FALSE;
	*/
	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		cfg->arch.omit_fp = FALSE;
	if (header->num_clauses)
		cfg->arch.omit_fp = FALSE;
	if (cfg->param_area)
		cfg->arch.omit_fp = FALSE;
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
		cfg->arch.omit_fp = FALSE;
	if ((mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)) ||
		(cfg->prof_options & MONO_PROFILE_ENTER_LEAVE))
		cfg->arch.omit_fp = FALSE;
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];

		if (ainfo->storage == ArgOnStack) {
			/* 
			 * The stack offset can only be determined when the frame
			 * size is known.
			 */
			cfg->arch.omit_fp = FALSE;
		}
	}

	locals_size = 0;
	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		int ialign;

		locals_size += mono_type_size (ins->inst_vtype, &ialign);
	}
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	mono_arch_compute_omit_fp (cfg);

	if (cfg->globalra) {
		if (cfg->arch.omit_fp)
			regs = g_list_prepend (regs, (gpointer)AMD64_RBP);
 
		regs = g_list_prepend (regs, (gpointer)AMD64_RBX);
		regs = g_list_prepend (regs, (gpointer)AMD64_R12);
		regs = g_list_prepend (regs, (gpointer)AMD64_R13);
		regs = g_list_prepend (regs, (gpointer)AMD64_R14);
#ifndef __native_client_codegen__
		regs = g_list_prepend (regs, (gpointer)AMD64_R15);
#endif
 
		regs = g_list_prepend (regs, (gpointer)AMD64_R10);
		regs = g_list_prepend (regs, (gpointer)AMD64_R9);
		regs = g_list_prepend (regs, (gpointer)AMD64_R8);
		regs = g_list_prepend (regs, (gpointer)AMD64_RDI);
		regs = g_list_prepend (regs, (gpointer)AMD64_RSI);
		regs = g_list_prepend (regs, (gpointer)AMD64_RDX);
		regs = g_list_prepend (regs, (gpointer)AMD64_RCX);
		regs = g_list_prepend (regs, (gpointer)AMD64_RAX);
	} else {
		if (cfg->arch.omit_fp)
			regs = g_list_prepend (regs, (gpointer)AMD64_RBP);

		/* We use the callee saved registers for global allocation */
		regs = g_list_prepend (regs, (gpointer)AMD64_RBX);
		regs = g_list_prepend (regs, (gpointer)AMD64_R12);
		regs = g_list_prepend (regs, (gpointer)AMD64_R13);
		regs = g_list_prepend (regs, (gpointer)AMD64_R14);
#ifndef __native_client_codegen__
		regs = g_list_prepend (regs, (gpointer)AMD64_R15);
#endif
#ifdef HOST_WIN32
		regs = g_list_prepend (regs, (gpointer)AMD64_RDI);
		regs = g_list_prepend (regs, (gpointer)AMD64_RSI);
#endif
	}

	return regs;
}
 
GList*
mono_arch_get_global_fp_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i;

	/* All XMM registers */
	for (i = 0; i < 16; ++i)
		regs = g_list_prepend (regs, GINT_TO_POINTER (i));

	return regs;
}

GList*
mono_arch_get_iregs_clobbered_by_call (MonoCallInst *call)
{
	static GList *r = NULL;

	if (r == NULL) {
		GList *regs = NULL;

		regs = g_list_prepend (regs, (gpointer)AMD64_RBP);
		regs = g_list_prepend (regs, (gpointer)AMD64_RBX);
		regs = g_list_prepend (regs, (gpointer)AMD64_R12);
		regs = g_list_prepend (regs, (gpointer)AMD64_R13);
		regs = g_list_prepend (regs, (gpointer)AMD64_R14);
#ifndef __native_client_codegen__
		regs = g_list_prepend (regs, (gpointer)AMD64_R15);
#endif

		regs = g_list_prepend (regs, (gpointer)AMD64_R10);
		regs = g_list_prepend (regs, (gpointer)AMD64_R9);
		regs = g_list_prepend (regs, (gpointer)AMD64_R8);
		regs = g_list_prepend (regs, (gpointer)AMD64_RDI);
		regs = g_list_prepend (regs, (gpointer)AMD64_RSI);
		regs = g_list_prepend (regs, (gpointer)AMD64_RDX);
		regs = g_list_prepend (regs, (gpointer)AMD64_RCX);
		regs = g_list_prepend (regs, (gpointer)AMD64_RAX);

		InterlockedCompareExchangePointer ((gpointer*)&r, regs, NULL);
	}

	return r;
}

GList*
mono_arch_get_fregs_clobbered_by_call (MonoCallInst *call)
{
	int i;
	static GList *r = NULL;

	if (r == NULL) {
		GList *regs = NULL;

		for (i = 0; i < AMD64_XMM_NREG; ++i)
			regs = g_list_prepend (regs, GINT_TO_POINTER (MONO_MAX_IREGS + i));

		InterlockedCompareExchangePointer ((gpointer*)&r, regs, NULL);
	}

	return r;
}

/*
 * mono_arch_regalloc_cost:
 *
 *  Return the cost, in number of memory references, of the action of 
 * allocating the variable VMV into a register during global register
 * allocation.
 */
guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	MonoInst *ins = cfg->varinfo [vmv->idx];

	if (cfg->method->save_lmf)
		/* The register is already saved */
		/* substract 1 for the invisible store in the prolog */
		return (ins->opcode == OP_ARG) ? 0 : 1;
	else
		/* push+pop */
		return (ins->opcode == OP_ARG) ? 1 : 2;
}

/*
 * mono_arch_fill_argument_info:
 *
 *   Populate cfg->args, cfg->ret and cfg->vret_addr with information about the arguments
 * of the method.
 */
void
mono_arch_fill_argument_info (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *ins;
	int i;
	CallInfo *cinfo;

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	cinfo = cfg->arch.cinfo;

	/*
	 * Contrary to mono_arch_allocate_vars (), the information should describe
	 * where the arguments are at the beginning of the method, not where they can be 
	 * accessed during the execution of the method. The later makes no sense for the 
	 * global register allocator, since a variable can be in more than one location.
	 */
	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
		case ArgInFloatSSEReg:
		case ArgInDoubleSSEReg:
			if ((MONO_TYPE_ISSTRUCT (sig->ret) && !mono_class_from_mono_type (sig->ret)->enumtype) || (sig->ret->type == MONO_TYPE_TYPEDBYREF)) {
				cfg->vret_addr->opcode = OP_REGVAR;
				cfg->vret_addr->inst_c0 = cinfo->ret.reg;
			}
			else {
				cfg->ret->opcode = OP_REGVAR;
				cfg->ret->inst_c0 = cinfo->ret.reg;
			}
			break;
		case ArgValuetypeInReg:
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = -1;
			cfg->ret->inst_offset = -1;
			break;
		default:
			g_assert_not_reached ();
		}
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];
		MonoType *arg_type;

		ins = cfg->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		switch (ainfo->storage) {
		case ArgInIReg:
		case ArgInFloatSSEReg:
		case ArgInDoubleSSEReg:
			ins->opcode = OP_REGVAR;
			ins->inst_c0 = ainfo->reg;
			break;
		case ArgOnStack:
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = -1;
			ins->inst_offset = -1;
			break;
		case ArgValuetypeInReg:
			/* Dummy */
			ins->opcode = OP_NOP;
			break;
		default:
			g_assert_not_reached ();
		}
	}
}
 
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *ins;
	int i, offset;
	guint32 locals_stack_size, locals_stack_align;
	gint32 *offsets;
	CallInfo *cinfo;

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	cinfo = cfg->arch.cinfo;

	mono_arch_compute_omit_fp (cfg);

	/*
	 * We use the ABI calling conventions for managed code as well.
	 * Exception: valuetypes are only sometimes passed or returned in registers.
	 */

	/*
	 * The stack looks like this:
	 * <incoming arguments passed on the stack>
	 * <return value>
	 * <lmf/caller saved registers>
	 * <locals>
	 * <spill area>
	 * <localloc area>  -> grows dynamically
	 * <params area>
	 */

	if (cfg->arch.omit_fp) {
		cfg->flags |= MONO_CFG_HAS_SPILLUP;
		cfg->frame_reg = AMD64_RSP;
		offset = 0;
	} else {
		/* Locals are allocated backwards from %fp */
		cfg->frame_reg = AMD64_RBP;
		offset = 0;
	}

	if (cfg->method->save_lmf) {
		/* The LMF var is allocated normally */
	} else {
		if (cfg->arch.omit_fp)
			cfg->arch.reg_save_area_offset = offset;
		/* Reserve space for caller saved registers */
		for (i = 0; i < AMD64_NREG; ++i)
			if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
				offset += sizeof(mgreg_t);
			}
	}

	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
		case ArgInFloatSSEReg:
		case ArgInDoubleSSEReg:
			if ((MONO_TYPE_ISSTRUCT (sig->ret) && !mono_class_from_mono_type (sig->ret)->enumtype) || (sig->ret->type == MONO_TYPE_TYPEDBYREF)) {
				if (cfg->globalra) {
					cfg->vret_addr->opcode = OP_REGVAR;
					cfg->vret_addr->inst_c0 = cinfo->ret.reg;
				} else {
					/* The register is volatile */
					cfg->vret_addr->opcode = OP_REGOFFSET;
					cfg->vret_addr->inst_basereg = cfg->frame_reg;
					if (cfg->arch.omit_fp) {
						cfg->vret_addr->inst_offset = offset;
						offset += 8;
					} else {
						offset += 8;
						cfg->vret_addr->inst_offset = -offset;
					}
					if (G_UNLIKELY (cfg->verbose_level > 1)) {
						printf ("vret_addr =");
						mono_print_ins (cfg->vret_addr);
					}
				}
			}
			else {
				cfg->ret->opcode = OP_REGVAR;
				cfg->ret->inst_c0 = cinfo->ret.reg;
			}
			break;
		case ArgValuetypeInReg:
			/* Allocate a local to hold the result, the epilog will copy it to the correct place */
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = cfg->frame_reg;
			if (cfg->arch.omit_fp) {
				cfg->ret->inst_offset = offset;
				offset += cinfo->ret.pair_storage [1] == ArgNone ? 8 : 16;
			} else {
				offset += cinfo->ret.pair_storage [1] == ArgNone ? 8 : 16;
				cfg->ret->inst_offset = - offset;
			}
			break;
		default:
			g_assert_not_reached ();
		}
		if (!cfg->globalra)
			cfg->ret->dreg = cfg->ret->inst_c0;
	}

	/* Allocate locals */
	if (!cfg->globalra) {
		offsets = mono_allocate_stack_slots (cfg, cfg->arch.omit_fp ? FALSE: TRUE, &locals_stack_size, &locals_stack_align);
		if (locals_stack_size > MONO_ARCH_MAX_FRAME_SIZE) {
			char *mname = mono_method_full_name (cfg->method, TRUE);
			cfg->exception_type = MONO_EXCEPTION_INVALID_PROGRAM;
			cfg->exception_message = g_strdup_printf ("Method %s stack is too big.", mname);
			g_free (mname);
			return;
		}
		
		if (locals_stack_align) {
			offset += (locals_stack_align - 1);
			offset &= ~(locals_stack_align - 1);
		}
		if (cfg->arch.omit_fp) {
			cfg->locals_min_stack_offset = offset;
			cfg->locals_max_stack_offset = offset + locals_stack_size;
		} else {
			cfg->locals_min_stack_offset = - (offset + locals_stack_size);
			cfg->locals_max_stack_offset = - offset;
		}
		
		for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
			if (offsets [i] != -1) {
				MonoInst *ins = cfg->varinfo [i];
				ins->opcode = OP_REGOFFSET;
				ins->inst_basereg = cfg->frame_reg;
				if (cfg->arch.omit_fp)
					ins->inst_offset = (offset + offsets [i]);
				else
					ins->inst_offset = - (offset + offsets [i]);
				//printf ("allocated local %d to ", i); mono_print_tree_nl (ins);
			}
		}
		offset += locals_stack_size;
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG)) {
		g_assert (!cfg->arch.omit_fp);
		g_assert (cinfo->sig_cookie.storage == ArgOnStack);
		cfg->sig_cookie = cinfo->sig_cookie.offset + ARGS_OFFSET;
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ins = cfg->args [i];
		if (ins->opcode != OP_REGVAR) {
			ArgInfo *ainfo = &cinfo->args [i];
			gboolean inreg = TRUE;
			MonoType *arg_type;

			if (sig->hasthis && (i == 0))
				arg_type = &mono_defaults.object_class->byval_arg;
			else
				arg_type = sig->params [i - sig->hasthis];

			if (cfg->globalra) {
				/* The new allocator needs info about the original locations of the arguments */
				switch (ainfo->storage) {
				case ArgInIReg:
				case ArgInFloatSSEReg:
				case ArgInDoubleSSEReg:
					ins->opcode = OP_REGVAR;
					ins->inst_c0 = ainfo->reg;
					break;
				case ArgOnStack:
					g_assert (!cfg->arch.omit_fp);
					ins->opcode = OP_REGOFFSET;
					ins->inst_basereg = cfg->frame_reg;
					ins->inst_offset = ainfo->offset + ARGS_OFFSET;
					break;
				case ArgValuetypeInReg:
					ins->opcode = OP_REGOFFSET;
					ins->inst_basereg = cfg->frame_reg;
					/* These arguments are saved to the stack in the prolog */
					offset = ALIGN_TO (offset, sizeof(mgreg_t));
					if (cfg->arch.omit_fp) {
						ins->inst_offset = offset;
						offset += (ainfo->storage == ArgValuetypeInReg) ? ainfo->nregs * sizeof (mgreg_t) : sizeof (mgreg_t);
					} else {
						offset += (ainfo->storage == ArgValuetypeInReg) ? ainfo->nregs * sizeof (mgreg_t) : sizeof (mgreg_t);
						ins->inst_offset = - offset;
					}
					break;
				default:
					g_assert_not_reached ();
				}

				continue;
			}

			/* FIXME: Allocate volatile arguments to registers */
			if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
				inreg = FALSE;

			/* 
			 * Under AMD64, all registers used to pass arguments to functions
			 * are volatile across calls.
			 * FIXME: Optimize this.
			 */
			if ((ainfo->storage == ArgInIReg) || (ainfo->storage == ArgInFloatSSEReg) || (ainfo->storage == ArgInDoubleSSEReg) || (ainfo->storage == ArgValuetypeInReg))
				inreg = FALSE;

			ins->opcode = OP_REGOFFSET;

			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInFloatSSEReg:
			case ArgInDoubleSSEReg:
				if (inreg) {
					ins->opcode = OP_REGVAR;
					ins->dreg = ainfo->reg;
				}
				break;
			case ArgOnStack:
				g_assert (!cfg->arch.omit_fp);
				ins->opcode = OP_REGOFFSET;
				ins->inst_basereg = cfg->frame_reg;
				ins->inst_offset = ainfo->offset + ARGS_OFFSET;
				break;
			case ArgValuetypeInReg:
				break;
			case ArgValuetypeAddrInIReg: {
				MonoInst *indir;
				g_assert (!cfg->arch.omit_fp);
				
				MONO_INST_NEW (cfg, indir, 0);
				indir->opcode = OP_REGOFFSET;
				if (ainfo->pair_storage [0] == ArgInIReg) {
					indir->inst_basereg = cfg->frame_reg;
					offset = ALIGN_TO (offset, sizeof (gpointer));
					offset += (sizeof (gpointer));
					indir->inst_offset = - offset;
				}
				else {
					indir->inst_basereg = cfg->frame_reg;
					indir->inst_offset = ainfo->offset + ARGS_OFFSET;
				}
				
				ins->opcode = OP_VTARG_ADDR;
				ins->inst_left = indir;
				
				break;
			}
			default:
				NOT_IMPLEMENTED;
			}

			if (!inreg && (ainfo->storage != ArgOnStack) && (ainfo->storage != ArgValuetypeAddrInIReg)) {
				ins->opcode = OP_REGOFFSET;
				ins->inst_basereg = cfg->frame_reg;
				/* These arguments are saved to the stack in the prolog */
				offset = ALIGN_TO (offset, sizeof(mgreg_t));
				if (cfg->arch.omit_fp) {
					ins->inst_offset = offset;
					offset += (ainfo->storage == ArgValuetypeInReg) ? ainfo->nregs * sizeof (mgreg_t) : sizeof (mgreg_t);
					// Arguments are yet supported by the stack map creation code
					//cfg->locals_max_stack_offset = MAX (cfg->locals_max_stack_offset, offset);
				} else {
					offset += (ainfo->storage == ArgValuetypeInReg) ? ainfo->nregs * sizeof (mgreg_t) : sizeof (mgreg_t);
					ins->inst_offset = - offset;
					//cfg->locals_min_stack_offset = MIN (cfg->locals_min_stack_offset, offset);
				}
			}
		}
	}

	cfg->stack_offset = offset;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	if (cinfo->ret.storage == ArgValuetypeInReg)
		cfg->ret_var_is_local = TRUE;

	if ((cinfo->ret.storage != ArgValuetypeInReg) && MONO_TYPE_ISSTRUCT (sig->ret)) {
		cfg->vret_addr = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_seq_points) {
		MonoInst *ins;

		if (cfg->compile_aot) {
			MonoInst *ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_info_var = ins;
		}

	    ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		ins->flags |= MONO_INST_VOLATILE;
		cfg->arch.ss_trigger_page_var = ins;
	}

#ifdef MONO_AMD64_NO_PUSHES
	/*
	 * When this is set, we pass arguments on the stack by moves, and by allocating 
	 * a bigger stack frame, instead of pushes.
	 * Pushes complicate exception handling because the arguments on the stack have
	 * to be popped each time a frame is unwound. They also make fp elimination
	 * impossible.
	 * FIXME: This doesn't work inside filter/finally clauses, since those execute
	 * on a new frame which doesn't include a param area.
	 */
	cfg->arch.no_pushes = TRUE;
#endif

	if (cfg->method->save_lmf) {
		MonoInst *lmf_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		lmf_var->flags |= MONO_INST_VOLATILE;
		lmf_var->flags |= MONO_INST_LMF;
		cfg->arch.lmf_var = lmf_var;
	}

#ifndef MONO_AMD64_NO_PUSHES
	cfg->arch_eh_jit_info = 1;
#endif
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *tree)
{
	MonoInst *ins;

	switch (storage) {
	case ArgInIReg:
		MONO_INST_NEW (cfg, ins, OP_MOVE);
		ins->dreg = mono_alloc_ireg_copy (cfg, tree->dreg);
		ins->sreg1 = tree->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, FALSE);
		break;
	case ArgInFloatSSEReg:
		MONO_INST_NEW (cfg, ins, OP_AMD64_SET_XMMREG_R4);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = tree->dreg;
		MONO_ADD_INS (cfg->cbb, ins);

		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);
		break;
	case ArgInDoubleSSEReg:
		MONO_INST_NEW (cfg, ins, OP_FMOVE);
		ins->dreg = mono_alloc_freg (cfg);
		ins->sreg1 = tree->dreg;
		MONO_ADD_INS (cfg->cbb, ins);

		mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, reg, TRUE);

		break;
	default:
		g_assert_not_reached ();
	}
}

static int
arg_storage_to_load_membase (ArgStorage storage)
{
	switch (storage) {
	case ArgInIReg:
#if defined(__mono_ilp32__)
		return OP_LOADI8_MEMBASE;
#else
		return OP_LOAD_MEMBASE;
#endif
	case ArgInDoubleSSEReg:
		return OP_LOADR8_MEMBASE;
	case ArgInFloatSSEReg:
		return OP_LOADR4_MEMBASE;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoInst *arg;
	MonoMethodSignature *tmp_sig;
	int sig_reg;

	if (call->tail_call)
		NOT_IMPLEMENTED;

	g_assert (cinfo->sig_cookie.storage == ArgOnStack);
			
	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is 
	 * passed first and all the arguments which were before it are
	 * passed on the stack after the signature. So compensate by 
	 * passing a different signature.
	 */
	tmp_sig = mono_metadata_signature_dup_full (cfg->method->klass->image, call->signature);
	tmp_sig->param_count -= call->signature->sentinelpos;
	tmp_sig->sentinelpos = 0;
	memcpy (tmp_sig->params, call->signature->params + call->signature->sentinelpos, tmp_sig->param_count * sizeof (MonoType*));

	sig_reg = mono_alloc_ireg (cfg);
	MONO_EMIT_NEW_SIGNATURECONST (cfg, sig_reg, tmp_sig);

	if (cfg->arch.no_pushes) {
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, AMD64_RSP, cinfo->sig_cookie.offset, sig_reg);
	} else {
		MONO_INST_NEW (cfg, arg, OP_X86_PUSH);
		arg->sreg1 = sig_reg;
		MONO_ADD_INS (cfg->cbb, arg);
	}
}

static inline LLVMArgStorage
arg_storage_to_llvm_arg_storage (MonoCompile *cfg, ArgStorage storage)
{
	switch (storage) {
	case ArgInIReg:
		return LLVMArgInIReg;
	case ArgNone:
		return LLVMArgNone;
	default:
		g_assert_not_reached ();
		return LLVMArgNone;
	}
}

#ifdef ENABLE_LLVM
LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	int j;
	LLVMCallInfo *linfo;
	MonoType *t;

	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	/*
	 * LLVM always uses the native ABI while we use our own ABI, the
	 * only difference is the handling of vtypes:
	 * - we only pass/receive them in registers in some cases, and only 
	 *   in 1 or 2 integer registers.
	 */
	if (cinfo->ret.storage == ArgValuetypeInReg) {
		if (sig->pinvoke) {
			cfg->exception_message = g_strdup ("pinvoke + vtypes");
			cfg->disable_llvm = TRUE;
			return linfo;
		}

		linfo->ret.storage = LLVMArgVtypeInReg;
		for (j = 0; j < 2; ++j)
			linfo->ret.pair_storage [j] = arg_storage_to_llvm_arg_storage (cfg, cinfo->ret.pair_storage [j]);
	}

	if (MONO_TYPE_ISSTRUCT (sig->ret) && cinfo->ret.storage == ArgInIReg) {
		/* Vtype returned using a hidden argument */
		linfo->ret.storage = LLVMArgVtypeRetAddr;
		linfo->vret_arg_index = cinfo->vret_arg_index;
	}

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = &mono_defaults.int_class->byval_arg;

		linfo->args [i].storage = LLVMArgNone;

		switch (ainfo->storage) {
		case ArgInIReg:
			linfo->args [i].storage = LLVMArgInIReg;
			break;
		case ArgInDoubleSSEReg:
		case ArgInFloatSSEReg:
			linfo->args [i].storage = LLVMArgInFPReg;
			break;
		case ArgOnStack:
			if (MONO_TYPE_ISSTRUCT (t)) {
				linfo->args [i].storage = LLVMArgVtypeByVal;
			} else {
				linfo->args [i].storage = LLVMArgInIReg;
				if (!t->byref) {
					if (t->type == MONO_TYPE_R4)
						linfo->args [i].storage = LLVMArgInFPReg;
					else if (t->type == MONO_TYPE_R8)
						linfo->args [i].storage = LLVMArgInFPReg;
				}
			}
			break;
		case ArgValuetypeInReg:
			if (sig->pinvoke) {
				cfg->exception_message = g_strdup ("pinvoke + vtypes");
				cfg->disable_llvm = TRUE;
				return linfo;
			}

			linfo->args [i].storage = LLVMArgVtypeInReg;
			for (j = 0; j < 2; ++j)
				linfo->args [i].pair_storage [j] = arg_storage_to_llvm_arg_storage (cfg, ainfo->pair_storage [j]);
			break;
		default:
			cfg->exception_message = g_strdup ("ainfo->storage");
			cfg->disable_llvm = TRUE;
			break;
		}
	}

	return linfo;
}
#endif

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n, stack_size;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	stack_size = 0;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);

	if (COMPILE_LLVM (cfg)) {
		/* We shouldn't be called in the llvm case */
		cfg->disable_llvm = TRUE;
		return;
	}

	if (cinfo->need_stack_align) {
		if (!cfg->arch.no_pushes)
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_ESP, 8);
	}

	/* 
	 * Emit all arguments which are passed on the stack to prevent register
	 * allocation problems.
	 */
	if (cfg->arch.no_pushes) {
		for (i = 0; i < n; ++i) {
			MonoType *t;
			ainfo = cinfo->args + i;

			in = call->args [i];

			if (sig->hasthis && i == 0)
				t = &mono_defaults.object_class->byval_arg;
			else
				t = sig->params [i - sig->hasthis];

			if (ainfo->storage == ArgOnStack && !MONO_TYPE_ISSTRUCT (t) && !call->tail_call) {
				if (!t->byref) {
					if (t->type == MONO_TYPE_R4)
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, AMD64_RSP, ainfo->offset, in->dreg);
					else if (t->type == MONO_TYPE_R8)
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, AMD64_RSP, ainfo->offset, in->dreg);
					else
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, AMD64_RSP, ainfo->offset, in->dreg);
				} else {
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, AMD64_RSP, ainfo->offset, in->dreg);
				}
				if (cfg->compute_gc_maps) {
					MonoInst *def;

					EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF (cfg, def, ainfo->offset, t);
				}
			}
		}
	}

	/*
	 * Emit all parameters passed in registers in non-reverse order for better readability
	 * and to help the optimization in emit_prolog ().
	 */
	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		in = call->args [i];

		if (ainfo->storage == ArgInIReg)
			add_outarg_reg (cfg, call, ainfo->storage, ainfo->reg, in);
	}

	for (i = n - 1; i >= 0; --i) {
		ainfo = cinfo->args + i;

		in = call->args [i];

		switch (ainfo->storage) {
		case ArgInIReg:
			/* Already done */
			break;
		case ArgInFloatSSEReg:
		case ArgInDoubleSSEReg:
			add_outarg_reg (cfg, call, ainfo->storage, ainfo->reg, in);
			break;
		case ArgOnStack:
		case ArgValuetypeInReg:
		case ArgValuetypeAddrInIReg:
			if (ainfo->storage == ArgOnStack && call->tail_call) {
				MonoInst *call_inst = (MonoInst*)call;
				cfg->args [i]->flags |= MONO_INST_VOLATILE;
				EMIT_NEW_ARGSTORE (cfg, call_inst, i, in);
			} else if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(sig->params [i - sig->hasthis]))) {
				guint32 align;
				guint32 size;

				if (sig->params [i - sig->hasthis]->type == MONO_TYPE_TYPEDBYREF) {
					size = sizeof (MonoTypedRef);
					align = sizeof (gpointer);
				}
				else {
					if (sig->pinvoke)
						size = mono_type_native_stack_size (&in->klass->byval_arg, &align);
					else {
						/* 
						 * Other backends use mono_type_stack_size (), but that
						 * aligns the size to 8, which is larger than the size of
						 * the source, leading to reads of invalid memory if the
						 * source is at the end of address space.
						 */
						size = mono_class_value_size (in->klass, &align);
					}
				}
				g_assert (in->klass);

				if (ainfo->storage == ArgOnStack && size >= 10000) {
					/* Avoid asserts in emit_memcpy () */
					cfg->exception_type = MONO_EXCEPTION_INVALID_PROGRAM;
					cfg->exception_message = g_strdup_printf ("Passing an argument of size '%d'.", size);
					/* Continue normally */
				}

				if (size > 0) {
					MONO_INST_NEW (cfg, arg, OP_OUTARG_VT);
					arg->sreg1 = in->dreg;
					arg->klass = in->klass;
					arg->backend.size = size;
					arg->inst_p0 = call;
					arg->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
					memcpy (arg->inst_p1, ainfo, sizeof (ArgInfo));

					MONO_ADD_INS (cfg->cbb, arg);
				}
			} else {
				if (cfg->arch.no_pushes) {
					/* Already done */
				} else {
					MONO_INST_NEW (cfg, arg, OP_X86_PUSH);
					arg->sreg1 = in->dreg;
					if (!sig->params [i - sig->hasthis]->byref) {
						if (sig->params [i - sig->hasthis]->type == MONO_TYPE_R4) {
							MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_ESP, 8);
							arg->opcode = OP_STORER4_MEMBASE_REG;
							arg->inst_destbasereg = X86_ESP;
							arg->inst_offset = 0;
						} else if (sig->params [i - sig->hasthis]->type == MONO_TYPE_R8) {
							MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_ESP, 8);
							arg->opcode = OP_STORER8_MEMBASE_REG;
							arg->inst_destbasereg = X86_ESP;
							arg->inst_offset = 0;
						}
					}
					MONO_ADD_INS (cfg->cbb, arg);
				}
			}
			break;
		default:
			g_assert_not_reached ();
		}

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos))
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret)) {
		MonoInst *vtarg;

		if (cinfo->ret.storage == ArgValuetypeInReg) {
			if (cinfo->ret.pair_storage [0] == ArgInIReg && cinfo->ret.pair_storage [1] == ArgNone) {
				/*
				 * Tell the JIT to use a more efficient calling convention: call using
				 * OP_CALL, compute the result location after the call, and save the 
				 * result there.
				 */
				call->vret_in_reg = TRUE;
				/* 
				 * Nullify the instruction computing the vret addr to enable 
				 * future optimizations.
				 */
				if (call->vret_var)
					NULLIFY_INS (call->vret_var);
			} else {
				if (call->tail_call)
					NOT_IMPLEMENTED;
				/*
				 * The valuetype is in RAX:RDX after the call, need to be copied to
				 * the stack. Push the address here, so the call instruction can
				 * access it.
				 */
				if (!cfg->arch.vret_addr_loc) {
					cfg->arch.vret_addr_loc = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
					/* Prevent it from being register allocated or optimized away */
					((MonoInst*)cfg->arch.vret_addr_loc)->flags |= MONO_INST_VOLATILE;
				}

				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, ((MonoInst*)cfg->arch.vret_addr_loc)->dreg, call->vret_var->dreg);
			}
		}
		else {
			MONO_INST_NEW (cfg, vtarg, OP_MOVE);
			vtarg->sreg1 = call->vret_var->dreg;
			vtarg->dreg = mono_alloc_preg (cfg);
			MONO_ADD_INS (cfg->cbb, vtarg);

			mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		}
	}

#ifdef HOST_WIN32
	if (call->inst.opcode != OP_JMP && OP_TAILCALL != call->inst.opcode) {
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_ESP, 0x20);
	}
#endif

	if (cfg->method->save_lmf) {
		MONO_INST_NEW (cfg, arg, OP_AMD64_SAVE_SP_TO_LMF);
		MONO_ADD_INS (cfg->cbb, arg);
	}

	call->stack_usage = cinfo->stack_usage;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoInst *arg;
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int size = ins->backend.size;

	if (ainfo->storage == ArgValuetypeInReg) {
		MonoInst *load;
		int part;

		for (part = 0; part < 2; ++part) {
			if (ainfo->pair_storage [part] == ArgNone)
				continue;

			MONO_INST_NEW (cfg, load, arg_storage_to_load_membase (ainfo->pair_storage [part]));
			load->inst_basereg = src->dreg;
			load->inst_offset = part * sizeof(mgreg_t);

			switch (ainfo->pair_storage [part]) {
			case ArgInIReg:
				load->dreg = mono_alloc_ireg (cfg);
				break;
			case ArgInDoubleSSEReg:
			case ArgInFloatSSEReg:
				load->dreg = mono_alloc_freg (cfg);
				break;
			default:
				g_assert_not_reached ();
			}
			MONO_ADD_INS (cfg->cbb, load);

			add_outarg_reg (cfg, call, ainfo->pair_storage [part], ainfo->pair_regs [part], load);
		}
	} else if (ainfo->storage == ArgValuetypeAddrInIReg) {
		MonoInst *vtaddr, *load;
		vtaddr = mono_compile_create_var (cfg, &ins->klass->byval_arg, OP_LOCAL);
		
		g_assert (!cfg->arch.no_pushes);

		MONO_INST_NEW (cfg, load, OP_LDADDR);
		load->inst_p0 = vtaddr;
		vtaddr->flags |= MONO_INST_INDIRECT;
		load->type = STACK_MP;
		load->klass = vtaddr->klass;
		load->dreg = mono_alloc_ireg (cfg);
		MONO_ADD_INS (cfg->cbb, load);
		mini_emit_memcpy (cfg, load->dreg, 0, src->dreg, 0, size, 4);

		if (ainfo->pair_storage [0] == ArgInIReg) {
			MONO_INST_NEW (cfg, arg, OP_X86_LEA_MEMBASE);
			arg->dreg = mono_alloc_ireg (cfg);
			arg->sreg1 = load->dreg;
			arg->inst_imm = 0;
			MONO_ADD_INS (cfg->cbb, arg);
			mono_call_inst_add_outarg_reg (cfg, call, arg->dreg, ainfo->pair_regs [0], FALSE);
		} else {
			MONO_INST_NEW (cfg, arg, OP_X86_PUSH);
			arg->sreg1 = load->dreg;
			MONO_ADD_INS (cfg->cbb, arg);
		}
	} else {
		if (size == 8) {
			if (cfg->arch.no_pushes) {
				int dreg = mono_alloc_ireg (cfg);

				MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, 0);
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, AMD64_RSP, ainfo->offset, dreg);
			} else {
				/* Can't use this for < 8 since it does an 8 byte memory load */
				MONO_INST_NEW (cfg, arg, OP_X86_PUSH_MEMBASE);
				arg->inst_basereg = src->dreg;
				arg->inst_offset = 0;
				MONO_ADD_INS (cfg->cbb, arg);
			}
		} else if (size <= 40) {
			if (cfg->arch.no_pushes) {
				mini_emit_memcpy (cfg, AMD64_RSP, ainfo->offset, src->dreg, 0, size, 4);
			} else {
				MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_ESP, ALIGN_TO (size, 8));
				mini_emit_memcpy (cfg, X86_ESP, 0, src->dreg, 0, size, 4);
			}
		} else {
			if (cfg->arch.no_pushes) {
				// FIXME: Code growth
				mini_emit_memcpy (cfg, AMD64_RSP, ainfo->offset, src->dreg, 0, size, 4);
			} else {
				MONO_INST_NEW (cfg, arg, OP_X86_PUSH_OBJ);
				arg->inst_basereg = src->dreg;
				arg->inst_offset = 0;
				arg->inst_imm = size;
				MONO_ADD_INS (cfg->cbb, arg);
			}
		}

		if (cfg->compute_gc_maps) {
			MonoInst *def;
			EMIT_NEW_GC_PARAM_SLOT_LIVENESS_DEF (cfg, def, ainfo->offset, &ins->klass->byval_arg);
		}
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_type_get_underlying_type (NULL, mono_method_signature (method)->ret);

	if (ret->type == MONO_TYPE_R4) {
		if (COMPILE_LLVM (cfg))
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		else
			MONO_EMIT_NEW_UNALU (cfg, OP_AMD64_SET_XMMREG_R4, cfg->ret->dreg, val->dreg);
		return;
	} else if (ret->type == MONO_TYPE_R8) {
		MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		return;
	}
			
	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

#endif /* DISABLE_JIT */

#define EMIT_COND_BRANCH(ins,cond,sign) \
        if (ins->inst_true_bb->native_offset) { \
	        x86_branch (code, cond, cfg->native_code + ins->inst_true_bb->native_offset, sign); \
        } else { \
	        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	        if ((cfg->opt & MONO_OPT_BRANCH) && \
            x86_is_imm8 (ins->inst_true_bb->max_offset - offset)) \
		        x86_branch8 (code, cond, 0, sign); \
                else \
	                x86_branch32 (code, cond, 0, sign); \
}

typedef struct {
	MonoMethodSignature *sig;
	CallInfo *cinfo;
} ArchDynCallInfo;

typedef struct {
	mgreg_t regs [PARAM_REGS];
	mgreg_t res;
	guint8 *ret;
} DynCallArgs;

static gboolean
dyn_call_supported (MonoMethodSignature *sig, CallInfo *cinfo)
{
	int i;

#ifdef HOST_WIN32
	return FALSE;
#endif

	switch (cinfo->ret.storage) {
	case ArgNone:
	case ArgInIReg:
		break;
	case ArgValuetypeInReg: {
		ArgInfo *ainfo = &cinfo->ret;

		if (ainfo->pair_storage [0] != ArgNone && ainfo->pair_storage [0] != ArgInIReg)
			return FALSE;
		if (ainfo->pair_storage [1] != ArgNone && ainfo->pair_storage [1] != ArgInIReg)
			return FALSE;
		break;
	}
	default:
		return FALSE;
	}

	for (i = 0; i < cinfo->nargs; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];
		switch (ainfo->storage) {
		case ArgInIReg:
			break;
		case ArgValuetypeInReg:
			if (ainfo->pair_storage [0] != ArgNone && ainfo->pair_storage [0] != ArgInIReg)
				return FALSE;
			if (ainfo->pair_storage [1] != ArgNone && ainfo->pair_storage [1] != ArgInIReg)
				return FALSE;
			break;
		default:
			return FALSE;
		}
	}

	return TRUE;
}

/*
 * mono_arch_dyn_call_prepare:
 *
 *   Return a pointer to an arch-specific structure which contains information 
 * needed by mono_arch_get_dyn_call_args (). Return NULL if OP_DYN_CALL is not
 * supported for SIG.
 * This function is equivalent to ffi_prep_cif in libffi.
 */
MonoDynCallInfo*
mono_arch_dyn_call_prepare (MonoMethodSignature *sig)
{
	ArchDynCallInfo *info;
	CallInfo *cinfo;

	cinfo = get_call_info (NULL, NULL, sig);

	if (!dyn_call_supported (sig, cinfo)) {
		g_free (cinfo);
		return NULL;
	}

	info = g_new0 (ArchDynCallInfo, 1);
	// FIXME: Preprocess the info to speed up get_dyn_call_args ().
	info->sig = sig;
	info->cinfo = cinfo;
	
	return (MonoDynCallInfo*)info;
}

/*
 * mono_arch_dyn_call_free:
 *
 *   Free a MonoDynCallInfo structure.
 */
void
mono_arch_dyn_call_free (MonoDynCallInfo *info)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;

	g_free (ainfo->cinfo);
	g_free (ainfo);
}

#if !defined(__native_client__)
#define PTR_TO_GREG(ptr) (mgreg_t)(ptr)
#define GREG_TO_PTR(greg) (gpointer)(greg)
#else
/* Correctly handle casts to/from 32-bit pointers without compiler warnings */
#define PTR_TO_GREG(ptr) (mgreg_t)(uintptr_t)(ptr)
#define GREG_TO_PTR(greg) (gpointer)(guint32)(greg)
#endif

/*
 * mono_arch_get_start_dyn_call:
 *
 *   Convert the arguments ARGS to a format which can be passed to OP_DYN_CALL, and
 * store the result into BUF.
 * ARGS should be an array of pointers pointing to the arguments.
 * RET should point to a memory buffer large enought to hold the result of the
 * call.
 * This function should be as fast as possible, any work which does not depend
 * on the actual values of the arguments should be done in 
 * mono_arch_dyn_call_prepare ().
 * start_dyn_call + OP_DYN_CALL + finish_dyn_call is equivalent to ffi_call in
 * libffi.
 */
void
mono_arch_start_dyn_call (MonoDynCallInfo *info, gpointer **args, guint8 *ret, guint8 *buf, int buf_len)
{
	ArchDynCallInfo *dinfo = (ArchDynCallInfo*)info;
	DynCallArgs *p = (DynCallArgs*)buf;
	int arg_index, greg, i, pindex;
	MonoMethodSignature *sig = dinfo->sig;

	g_assert (buf_len >= sizeof (DynCallArgs));

	p->res = 0;
	p->ret = ret;

	arg_index = 0;
	greg = 0;
	pindex = 0;

	if (sig->hasthis || dinfo->cinfo->vret_arg_index == 1) {
		p->regs [greg ++] = PTR_TO_GREG(*(args [arg_index ++]));
		if (!sig->hasthis)
			pindex = 1;
	}

	if (dinfo->cinfo->vtype_retaddr)
		p->regs [greg ++] = PTR_TO_GREG(ret);

	for (i = pindex; i < sig->param_count; i++) {
		MonoType *t = mono_type_get_underlying_type (sig->params [i]);
		gpointer *arg = args [arg_index ++];

		if (t->byref) {
			p->regs [greg ++] = PTR_TO_GREG(*(arg));
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#if !defined(__mono_ilp32__)
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
#endif
			g_assert (dinfo->cinfo->args [i + sig->hasthis].reg == param_regs [greg]);
			p->regs [greg ++] = PTR_TO_GREG(*(arg));
			break;
#if defined(__mono_ilp32__)
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			g_assert (dinfo->cinfo->args [i + sig->hasthis].reg == param_regs [greg]);
			p->regs [greg ++] = *(guint64*)(arg);
			break;
#endif
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
			p->regs [greg ++] = *(guint8*)(arg);
			break;
		case MONO_TYPE_I1:
			p->regs [greg ++] = *(gint8*)(arg);
			break;
		case MONO_TYPE_I2:
			p->regs [greg ++] = *(gint16*)(arg);
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			p->regs [greg ++] = *(guint16*)(arg);
			break;
		case MONO_TYPE_I4:
			p->regs [greg ++] = *(gint32*)(arg);
			break;
		case MONO_TYPE_U4:
			p->regs [greg ++] = *(guint32*)(arg);
			break;
		case MONO_TYPE_GENERICINST:
		    if (MONO_TYPE_IS_REFERENCE (t)) {
				p->regs [greg ++] = PTR_TO_GREG(*(arg));
				break;
			} else {
				/* Fall through */
			}
		case MONO_TYPE_VALUETYPE: {
			ArgInfo *ainfo = &dinfo->cinfo->args [i + sig->hasthis];

			g_assert (ainfo->storage == ArgValuetypeInReg);
			if (ainfo->pair_storage [0] != ArgNone) {
				g_assert (ainfo->pair_storage [0] == ArgInIReg);
				p->regs [greg ++] = ((mgreg_t*)(arg))[0];
			}
			if (ainfo->pair_storage [1] != ArgNone) {
				g_assert (ainfo->pair_storage [1] == ArgInIReg);
				p->regs [greg ++] = ((mgreg_t*)(arg))[1];
			}
			break;
		}
		default:
			g_assert_not_reached ();
		}
	}

	g_assert (greg <= PARAM_REGS);
}

/*
 * mono_arch_finish_dyn_call:
 *
 *   Store the result of a dyn call into the return value buffer passed to
 * start_dyn_call ().
 * This function should be as fast as possible, any work which does not depend
 * on the actual values of the arguments should be done in 
 * mono_arch_dyn_call_prepare ().
 */
void
mono_arch_finish_dyn_call (MonoDynCallInfo *info, guint8 *buf)
{
	ArchDynCallInfo *dinfo = (ArchDynCallInfo*)info;
	MonoMethodSignature *sig = dinfo->sig;
	guint8 *ret = ((DynCallArgs*)buf)->ret;
	mgreg_t res = ((DynCallArgs*)buf)->res;

	switch (mono_type_get_underlying_type (sig->ret)->type) {
	case MONO_TYPE_VOID:
		*(gpointer*)ret = NULL;
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		*(gpointer*)ret = GREG_TO_PTR(res);
		break;
	case MONO_TYPE_I1:
		*(gint8*)ret = res;
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		*(guint8*)ret = res;
		break;
	case MONO_TYPE_I2:
		*(gint16*)ret = res;
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		*(guint16*)ret = res;
		break;
	case MONO_TYPE_I4:
		*(gint32*)ret = res;
		break;
	case MONO_TYPE_U4:
		*(guint32*)ret = res;
		break;
	case MONO_TYPE_I8:
		*(gint64*)ret = res;
		break;
	case MONO_TYPE_U8:
		*(guint64*)ret = res;
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_IS_REFERENCE (sig->ret)) {
			*(gpointer*)ret = GREG_TO_PTR(res);
			break;
		} else {
			/* Fall through */
		}
	case MONO_TYPE_VALUETYPE:
		if (dinfo->cinfo->vtype_retaddr) {
			/* Nothing to do */
		} else {
			ArgInfo *ainfo = &dinfo->cinfo->ret;

			g_assert (ainfo->storage == ArgValuetypeInReg);

			if (ainfo->pair_storage [0] != ArgNone) {
				g_assert (ainfo->pair_storage [0] == ArgInIReg);
				((mgreg_t*)ret)[0] = res;
			}

			g_assert (ainfo->pair_storage [1] == ArgNone);
		}
		break;
	default:
		g_assert_not_reached ();
	}
}

/* emit an exception if condition is fail */
#define EMIT_COND_SYSTEM_EXCEPTION(cond,signed,exc_name)            \
        do {                                                        \
		MonoInst *tins = mono_branch_optimize_exception_target (cfg, bb, exc_name); \
		if (tins == NULL) {										\
			mono_add_patch_info (cfg, code - cfg->native_code,   \
					MONO_PATCH_INFO_EXC, exc_name);  \
			x86_branch32 (code, cond, 0, signed);               \
		} else {	\
			EMIT_COND_BRANCH (tins, cond, signed);	\
		}			\
	} while (0); 

#define EMIT_FPCOMPARE(code) do { \
	amd64_fcompp (code); \
	amd64_fnstsw (code); \
} while (0); 

#define EMIT_SSE2_FPFUNC(code, op, dreg, sreg1) do { \
    amd64_movsd_membase_reg (code, AMD64_RSP, -8, (sreg1)); \
	amd64_fld_membase (code, AMD64_RSP, -8, TRUE); \
	amd64_ ##op (code); \
	amd64_fst_membase (code, AMD64_RSP, -8, TRUE, TRUE); \
	amd64_movsd_reg_membase (code, (dreg), AMD64_RSP, -8); \
} while (0);

static guint8*
emit_call_body (MonoCompile *cfg, guint8 *code, guint32 patch_type, gconstpointer data)
{
	gboolean no_patch = FALSE;

	/* 
	 * FIXME: Add support for thunks
	 */
	{
		gboolean near_call = FALSE;

		/*
		 * Indirect calls are expensive so try to make a near call if possible.
		 * The caller memory is allocated by the code manager so it is 
		 * guaranteed to be at a 32 bit offset.
		 */

		if (patch_type != MONO_PATCH_INFO_ABS) {
			/* The target is in memory allocated using the code manager */
			near_call = TRUE;

			if ((patch_type == MONO_PATCH_INFO_METHOD) || (patch_type == MONO_PATCH_INFO_METHOD_JUMP)) {
				if (((MonoMethod*)data)->klass->image->aot_module)
					/* The callee might be an AOT method */
					near_call = FALSE;
				if (((MonoMethod*)data)->dynamic)
					/* The target is in malloc-ed memory */
					near_call = FALSE;
			}

			if (patch_type == MONO_PATCH_INFO_INTERNAL_METHOD) {
				/* 
				 * The call might go directly to a native function without
				 * the wrapper.
				 */
				MonoJitICallInfo *mi = mono_find_jit_icall_by_name (data);
				if (mi) {
					gconstpointer target = mono_icall_get_wrapper (mi);
					if ((((guint64)target) >> 32) != 0)
						near_call = FALSE;
				}
			}
		}
		else {
			if (cfg->abs_patches && g_hash_table_lookup (cfg->abs_patches, data)) {
				/* 
				 * This is not really an optimization, but required because the
				 * generic class init trampolines use R11 to pass the vtable.
				 */
				near_call = TRUE;
			} else {
				MonoJitICallInfo *info = mono_find_jit_icall_by_addr (data);
				if (info) {
					if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && 
						strstr (cfg->method->name, info->name)) {
						/* A call to the wrapped function */
						if ((((guint64)data) >> 32) == 0)
							near_call = TRUE;
						no_patch = TRUE;
					}
					else if (info->func == info->wrapper) {
						/* No wrapper */
						if ((((guint64)info->func) >> 32) == 0)
							near_call = TRUE;
					}
					else {
						/* See the comment in mono_codegen () */
						if ((info->name [0] != 'v') || (strstr (info->name, "ves_array_new_va_") == NULL && strstr (info->name, "ves_array_element_address_") == NULL))
							near_call = TRUE;
					}
				}
				else if ((((guint64)data) >> 32) == 0) {
					near_call = TRUE;
					no_patch = TRUE;
				}
			}
		}

		if (cfg->method->dynamic)
			/* These methods are allocated using malloc */
			near_call = FALSE;

#ifdef MONO_ARCH_NOMAP32BIT
		near_call = FALSE;
#endif

		/* The 64bit XEN kernel does not honour the MAP_32BIT flag. (#522894) */
		if (optimize_for_xen)
			near_call = FALSE;

		if (cfg->compile_aot) {
			near_call = TRUE;
			no_patch = TRUE;
		}

		if (near_call) {
			/* 
			 * Align the call displacement to an address divisible by 4 so it does
			 * not span cache lines. This is required for code patching to work on SMP
			 * systems.
			 */
			if (!no_patch && ((guint32)(code + 1 - cfg->native_code) % 4) != 0) {
				guint32 pad_size = 4 - ((guint32)(code + 1 - cfg->native_code) % 4);
				amd64_padding (code, pad_size);
			}
			mono_add_patch_info (cfg, code - cfg->native_code, patch_type, data);
			amd64_call_code (code, 0);
		}
		else {
			mono_add_patch_info (cfg, code - cfg->native_code, patch_type, data);
			amd64_set_reg_template (code, GP_SCRATCH_REG);
			amd64_call_reg (code, GP_SCRATCH_REG);
		}
	}

	return code;
}

static inline guint8*
emit_call (MonoCompile *cfg, guint8 *code, guint32 patch_type, gconstpointer data, gboolean win64_adjust_stack)
{
#ifdef HOST_WIN32
	if (win64_adjust_stack)
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 32);
#endif
	code = emit_call_body (cfg, code, patch_type, data);
#ifdef HOST_WIN32
	if (win64_adjust_stack)
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 32);
#endif	
	
	return code;
}

static inline int
store_membase_imm_to_store_membase_reg (int opcode)
{
	switch (opcode) {
	case OP_STORE_MEMBASE_IMM:
		return OP_STORE_MEMBASE_REG;
	case OP_STOREI4_MEMBASE_IMM:
		return OP_STOREI4_MEMBASE_REG;
	case OP_STOREI8_MEMBASE_IMM:
		return OP_STOREI8_MEMBASE_REG;
	}

	return -1;
}

#ifndef DISABLE_JIT

#define INST_IGNORES_CFLAGS(opcode) (!(((opcode) == OP_ADC) || ((opcode) == OP_ADC_IMM) || ((opcode) == OP_IADC) || ((opcode) == OP_IADC_IMM) || ((opcode) == OP_SBB) || ((opcode) == OP_SBB_IMM) || ((opcode) == OP_ISBB) || ((opcode) == OP_ISBB_IMM)))

/*
 * mono_arch_peephole_pass_1:
 *
 *   Perform peephole opts which should/can be performed before local regalloc
 */
void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		MonoInst *last_ins = ins->prev;

		switch (ins->opcode) {
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
			if ((ins->sreg1 < MONO_MAX_IREGS) && (ins->dreg >= MONO_MAX_IREGS) && (ins->inst_imm > 0)) {
				/* 
				 * X86_LEA is like ADD, but doesn't have the
				 * sreg1==dreg restriction. inst_imm > 0 is needed since LEA sign-extends 
				 * its operand to 64 bit.
				 */
				ins->opcode = OP_X86_LEA_MEMBASE;
				ins->inst_basereg = ins->sreg1;
			}
			break;
		case OP_LXOR:
		case OP_IXOR:
			if ((ins->sreg1 == ins->sreg2) && (ins->sreg1 == ins->dreg)) {
				MonoInst *ins2;

				/* 
				 * Replace STORE_MEMBASE_IMM 0 with STORE_MEMBASE_REG since 
				 * the latter has length 2-3 instead of 6 (reverse constant
				 * propagation). These instruction sequences are very common
				 * in the initlocals bblock.
				 */
				for (ins2 = ins->next; ins2; ins2 = ins2->next) {
					if (((ins2->opcode == OP_STORE_MEMBASE_IMM) || (ins2->opcode == OP_STOREI4_MEMBASE_IMM) || (ins2->opcode == OP_STOREI8_MEMBASE_IMM) || (ins2->opcode == OP_STORE_MEMBASE_IMM)) && (ins2->inst_imm == 0)) {
						ins2->opcode = store_membase_imm_to_store_membase_reg (ins2->opcode);
						ins2->sreg1 = ins->dreg;
					} else if ((ins2->opcode == OP_STOREI1_MEMBASE_IMM) || (ins2->opcode == OP_STOREI2_MEMBASE_IMM) || (ins2->opcode == OP_STOREI8_MEMBASE_REG) || (ins2->opcode == OP_STORE_MEMBASE_REG)) {
						/* Continue */
					} else if (((ins2->opcode == OP_ICONST) || (ins2->opcode == OP_I8CONST)) && (ins2->dreg == ins->dreg) && (ins2->inst_c0 == 0)) {
						NULLIFY_INS (ins2);
						/* Continue */
					} else {
						break;
					}
				}
			}
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			/* OP_COMPARE_IMM (reg, 0) 
			 * --> 
			 * OP_AMD64_TEST_NULL (reg) 
			 */
			if (!ins->inst_imm)
				ins->opcode = OP_AMD64_TEST_NULL;
			break;
		case OP_ICOMPARE_IMM:
			if (!ins->inst_imm)
				ins->opcode = OP_X86_TEST_NULL;
			break;
		case OP_AMD64_ICOMPARE_MEMBASE_IMM:
			/* 
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_X86_COMPARE_MEMBASE_IMM offset(basereg), imm
			 * -->
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_COMPARE_IMM reg, imm
			 *
			 * Note: if imm = 0 then OP_COMPARE_IMM replaced with OP_X86_TEST_NULL
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
					ins->opcode = OP_ICOMPARE_IMM;
					ins->sreg1 = last_ins->sreg1;

					/* check if we can remove cmp reg,0 with test null */
					if (!ins->inst_imm)
						ins->opcode = OP_X86_TEST_NULL;
				}

			break;
		}

		mono_peephole_ins (bb, ins);
	}
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_ICONST:
		case OP_I8CONST: {
			/* reg = 0 -> XOR (reg, reg) */
			/* XOR sets cflags on x86, so we cant do it always */
			if (ins->inst_c0 == 0 && (!ins->next || (ins->next && INST_IGNORES_CFLAGS (ins->next->opcode)))) {
				ins->opcode = OP_LXOR;
				ins->sreg1 = ins->dreg;
				ins->sreg2 = ins->dreg;
				/* Fall through */
			} else {
				break;
			}
		}
		case OP_LXOR:
			/*
			 * Use IXOR to avoid a rex prefix if possible. The cpu will sign extend the 
			 * 0 result into 64 bits.
			 */
			if ((ins->sreg1 == ins->sreg2) && (ins->sreg1 == ins->dreg)) {
				ins->opcode = OP_IXOR;
			}
			/* Fall through */
		case OP_IXOR:
			if ((ins->sreg1 == ins->sreg2) && (ins->sreg1 == ins->dreg)) {
				MonoInst *ins2;

				/* 
				 * Replace STORE_MEMBASE_IMM 0 with STORE_MEMBASE_REG since 
				 * the latter has length 2-3 instead of 6 (reverse constant
				 * propagation). These instruction sequences are very common
				 * in the initlocals bblock.
				 */
				for (ins2 = ins->next; ins2; ins2 = ins2->next) {
					if (((ins2->opcode == OP_STORE_MEMBASE_IMM) || (ins2->opcode == OP_STOREI4_MEMBASE_IMM) || (ins2->opcode == OP_STOREI8_MEMBASE_IMM) || (ins2->opcode == OP_STORE_MEMBASE_IMM)) && (ins2->inst_imm == 0)) {
						ins2->opcode = store_membase_imm_to_store_membase_reg (ins2->opcode);
						ins2->sreg1 = ins->dreg;
					} else if ((ins2->opcode == OP_STOREI1_MEMBASE_IMM) || (ins2->opcode == OP_STOREI2_MEMBASE_IMM) || (ins2->opcode == OP_STOREI4_MEMBASE_REG) || (ins2->opcode == OP_STOREI8_MEMBASE_REG) || (ins2->opcode == OP_STORE_MEMBASE_REG) || (ins2->opcode == OP_LIVERANGE_START) || (ins2->opcode == OP_GC_LIVENESS_DEF) || (ins2->opcode == OP_GC_LIVENESS_USE)) {
						/* Continue */
					} else if (((ins2->opcode == OP_ICONST) || (ins2->opcode == OP_I8CONST)) && (ins2->dreg == ins->dreg) && (ins2->inst_c0 == 0)) {
						NULLIFY_INS (ins2);
						/* Continue */
					} else {
						break;
					}
				}
			}
			break;
		case OP_IADD_IMM:
			if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_INC_REG;
			break;
		case OP_ISUB_IMM:
			if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_DEC_REG;
			break;
		}

		mono_peephole_ins (bb, ins);
	}
}

#define NEW_INS(cfg,ins,dest,op) do {	\
		MONO_INST_NEW ((cfg), (dest), (op)); \
        (dest)->cil_code = (ins)->cil_code; \
        mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

/*
 * mono_arch_lowering_pass:
 *
 *  Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *temp;

	/*
	 * FIXME: Need to add more instructions, but the current machine 
	 * description can't model some parts of the composite instructions like
	 * cdq.
	 */
	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_DIV_IMM:
		case OP_REM_IMM:
		case OP_IDIV_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_UN_IMM:
			mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_IREM_IMM:
			/* Keep the opcode if we can implement it efficiently */
			if (!((ins->inst_imm > 0) && (mono_is_power_of_two (ins->inst_imm) != -1)))
				mono_decompose_op_imm (cfg, bb, ins);
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			if (!amd64_is_imm32 (ins->inst_imm)) {
				NEW_INS (cfg, ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->opcode = OP_COMPARE;
				ins->sreg2 = temp->dreg;
			}
			break;
#ifndef __mono_ilp32__
		case OP_LOAD_MEMBASE:
#endif
		case OP_LOADI8_MEMBASE:
#ifndef __native_client_codegen__
		/*  Don't generate memindex opcodes (to simplify */
		/*  read sandboxing) */
			if (!amd64_is_imm32 (ins->inst_offset)) {
				NEW_INS (cfg, ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->opcode = OP_AMD64_LOADI8_MEMINDEX;
				ins->inst_indexreg = temp->dreg;
			}
#endif
			break;
#ifndef __mono_ilp32__
		case OP_STORE_MEMBASE_IMM:
#endif
		case OP_STOREI8_MEMBASE_IMM:
			if (!amd64_is_imm32 (ins->inst_imm)) {
				NEW_INS (cfg, ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->opcode = OP_STOREI8_MEMBASE_REG;
				ins->sreg1 = temp->dreg;
			}
			break;
#ifdef MONO_ARCH_SIMD_INTRINSICS
		case OP_EXPAND_I1: {
				int temp_reg1 = mono_alloc_ireg (cfg);
				int temp_reg2 = mono_alloc_ireg (cfg);
				int original_reg = ins->sreg1;

				NEW_INS (cfg, ins, temp, OP_ICONV_TO_U1);
				temp->sreg1 = original_reg;
				temp->dreg = temp_reg1;

				NEW_INS (cfg, ins, temp, OP_SHL_IMM);
				temp->sreg1 = temp_reg1;
				temp->dreg = temp_reg2;
				temp->inst_imm = 8;

				NEW_INS (cfg, ins, temp, OP_LOR);
				temp->sreg1 = temp->dreg = temp_reg2;
				temp->sreg2 = temp_reg1;

 				ins->opcode = OP_EXPAND_I2;
				ins->sreg1 = temp_reg2;
			}
			break;
#endif
		default:
			break;
		}
	}

	bb->max_vreg = cfg->next_vreg;
}

static const int 
branch_cc_table [] = {
	X86_CC_EQ, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_NE, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_O, X86_CC_NO, X86_CC_C, X86_CC_NC
};

/* Maps CMP_... constants to X86_CC_... constants */
static const int
cc_table [] = {
	X86_CC_EQ, X86_CC_NE, X86_CC_LE, X86_CC_GE, X86_CC_LT, X86_CC_GT,
	X86_CC_LE, X86_CC_GE, X86_CC_LT, X86_CC_GT
};

static const int
cc_signed_table [] = {
	TRUE, TRUE, TRUE, TRUE, TRUE, TRUE,
	FALSE, FALSE, FALSE, FALSE
};

/*#include "cprop.c"*/

static unsigned char*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	amd64_sse_cvttsd2si_reg_reg (code, dreg, sreg);

	if (size == 1)
		amd64_widen_reg (code, dreg, dreg, is_signed, FALSE);
	else if (size == 2)
		amd64_widen_reg (code, dreg, dreg, is_signed, TRUE);
	return code;
}

static unsigned char*
mono_emit_stack_alloc (MonoCompile *cfg, guchar *code, MonoInst* tree)
{
	int sreg = tree->sreg1;
	int need_touch = FALSE;

#if defined(HOST_WIN32) || defined(MONO_ARCH_SIGSEGV_ON_ALTSTACK)
	if (!tree->flags & MONO_INST_INIT)
		need_touch = TRUE;
#endif

	if (need_touch) {
		guint8* br[5];

		/*
		 * Under Windows:
		 * If requested stack size is larger than one page,
		 * perform stack-touch operation
		 */
		/*
		 * Generate stack probe code.
		 * Under Windows, it is necessary to allocate one page at a time,
		 * "touching" stack after each successful sub-allocation. This is
		 * because of the way stack growth is implemented - there is a
		 * guard page before the lowest stack page that is currently commited.
		 * Stack normally grows sequentially so OS traps access to the
		 * guard page and commits more pages when needed.
		 */
		amd64_test_reg_imm (code, sreg, ~0xFFF);
		br[0] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);

		br[2] = code; /* loop */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 0x1000);
		amd64_test_membase_reg (code, AMD64_RSP, 0, AMD64_RSP);
		amd64_alu_reg_imm (code, X86_SUB, sreg, 0x1000);
		amd64_alu_reg_imm (code, X86_CMP, sreg, 0x1000);
		br[3] = code; x86_branch8 (code, X86_CC_AE, 0, FALSE);
		amd64_patch (br[3], br[2]);
		amd64_test_reg_reg (code, sreg, sreg);
		br[4] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, sreg);

		br[1] = code; x86_jump8 (code, 0);

		amd64_patch (br[0], code);
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, sreg);
		amd64_patch (br[1], code);
		amd64_patch (br[4], code);
	}
	else
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, tree->sreg1);

	if (tree->flags & MONO_INST_INIT) {
		int offset = 0;
		if (tree->dreg != AMD64_RAX && sreg != AMD64_RAX) {
			amd64_push_reg (code, AMD64_RAX);
			offset += 8;
		}
		if (tree->dreg != AMD64_RCX && sreg != AMD64_RCX) {
			amd64_push_reg (code, AMD64_RCX);
			offset += 8;
		}
		if (tree->dreg != AMD64_RDI && sreg != AMD64_RDI) {
			amd64_push_reg (code, AMD64_RDI);
			offset += 8;
		}
		
		amd64_shift_reg_imm (code, X86_SHR, sreg, 3);
		if (sreg != AMD64_RCX)
			amd64_mov_reg_reg (code, AMD64_RCX, sreg, 8);
		amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
				
		amd64_lea_membase (code, AMD64_RDI, AMD64_RSP, offset);
		if (cfg->param_area && cfg->arch.no_pushes)
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RDI, cfg->param_area);
		amd64_cld (code);
#if defined(__default_codegen__)
		amd64_prefix (code, X86_REP_PREFIX);
		amd64_stosl (code);
#elif defined(__native_client_codegen__)
		/* NaCl stos pseudo-instruction */
		amd64_codegen_pre(code);
		/* First, clear the upper 32 bits of RDI (mov %edi, %edi)  */
		amd64_mov_reg_reg (code, AMD64_RDI, AMD64_RDI, 4);
		/* Add %r15 to %rdi using lea, condition flags unaffected. */
		amd64_lea_memindex_size (code, AMD64_RDI, AMD64_R15, 0, AMD64_RDI, 0, 8);
		amd64_prefix (code, X86_REP_PREFIX);
		amd64_stosl (code);
		amd64_codegen_post(code);
#endif /* __native_client_codegen__ */
		
		if (tree->dreg != AMD64_RDI && sreg != AMD64_RDI)
			amd64_pop_reg (code, AMD64_RDI);
		if (tree->dreg != AMD64_RCX && sreg != AMD64_RCX)
			amd64_pop_reg (code, AMD64_RCX);
		if (tree->dreg != AMD64_RAX && sreg != AMD64_RAX)
			amd64_pop_reg (code, AMD64_RAX);
	}
	return code;
}

static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	CallInfo *cinfo;
	guint32 quad;

	/* Move return value to the target register */
	/* FIXME: do this in the local reg allocator */
	switch (ins->opcode) {
	case OP_CALL:
	case OP_CALL_REG:
	case OP_CALL_MEMBASE:
	case OP_LCALL:
	case OP_LCALL_REG:
	case OP_LCALL_MEMBASE:
		g_assert (ins->dreg == AMD64_RAX);
		break;
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4) {
			amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, AMD64_XMM0);
		}
		else {
			if (ins->dreg != AMD64_XMM0)
				amd64_sse_movsd_reg_reg (code, ins->dreg, AMD64_XMM0);
		}
		break;
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
	case OP_VCALL2:
	case OP_VCALL2_REG:
	case OP_VCALL2_MEMBASE:
		cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, ((MonoCallInst*)ins)->signature);
		if (cinfo->ret.storage == ArgValuetypeInReg) {
			MonoInst *loc = cfg->arch.vret_addr_loc;

			/* Load the destination address */
			g_assert (loc->opcode == OP_REGOFFSET);
			amd64_mov_reg_membase (code, AMD64_RCX, loc->inst_basereg, loc->inst_offset, sizeof(gpointer));

			for (quad = 0; quad < 2; quad ++) {
				switch (cinfo->ret.pair_storage [quad]) {
				case ArgInIReg:
					amd64_mov_membase_reg (code, AMD64_RCX, (quad * sizeof(mgreg_t)), cinfo->ret.pair_regs [quad], sizeof(mgreg_t));
					break;
				case ArgInFloatSSEReg:
					amd64_movss_membase_reg (code, AMD64_RCX, (quad * 8), cinfo->ret.pair_regs [quad]);
					break;
				case ArgInDoubleSSEReg:
					amd64_movsd_membase_reg (code, AMD64_RCX, (quad * 8), cinfo->ret.pair_regs [quad]);
					break;
				case ArgNone:
					break;
				default:
					NOT_IMPLEMENTED;
				}
			}
		}
		break;
	}

	return code;
}

#endif /* DISABLE_JIT */

#ifdef __APPLE__
static int tls_gs_offset;
#endif

gboolean
mono_amd64_have_tls_get (void)
{
#ifdef __APPLE__
	static gboolean have_tls_get = FALSE;
	static gboolean inited = FALSE;

	if (inited)
		return have_tls_get;

	guint8 *ins = (guint8*)pthread_getspecific;

	/*
	 * We're looking for these two instructions:
	 *
	 * mov    %gs:[offset](,%rdi,8),%rax
	 * retq
	 */
	have_tls_get = ins [0] == 0x65 &&
		       ins [1] == 0x48 &&
		       ins [2] == 0x8b &&
		       ins [3] == 0x04 &&
		       ins [4] == 0xfd &&
		       ins [6] == 0x00 &&
		       ins [7] == 0x00 &&
		       ins [8] == 0x00 &&
		       ins [9] == 0xc3;

	inited = TRUE;

	tls_gs_offset = ins[5];

	return have_tls_get;
#else
	return TRUE;
#endif
}

/*
 * mono_amd64_emit_tls_get:
 * @code: buffer to store code to
 * @dreg: hard register where to place the result
 * @tls_offset: offset info
 *
 * mono_amd64_emit_tls_get emits in @code the native code that puts in
 * the dreg register the item in the thread local storage identified
 * by tls_offset.
 *
 * Returns: a pointer to the end of the stored code
 */
guint8*
mono_amd64_emit_tls_get (guint8* code, int dreg, int tls_offset)
{
#ifdef HOST_WIN32
	g_assert (tls_offset < 64);
	x86_prefix (code, X86_GS_PREFIX);
	amd64_mov_reg_mem (code, dreg, (tls_offset * 8) + 0x1480, 8);
#elif defined(__APPLE__)
	x86_prefix (code, X86_GS_PREFIX);
	amd64_mov_reg_mem (code, dreg, tls_gs_offset + (tls_offset * 8), 8);
#else
	if (optimize_for_xen) {
		x86_prefix (code, X86_FS_PREFIX);
		amd64_mov_reg_mem (code, dreg, 0, 8);
		amd64_mov_reg_membase (code, dreg, dreg, tls_offset, 8);
	} else {
		x86_prefix (code, X86_FS_PREFIX);
		amd64_mov_reg_mem (code, dreg, tls_offset, 8);
	}
#endif
	return code;
}

/*
 * emit_setup_lmf:
 *
 *   Emit code to initialize an LMF structure at LMF_OFFSET.
 */
static guint8*
emit_setup_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset, int cfa_offset)
{
	int i;

	/* 
	 * The ip field is not set, the exception handling code will obtain it from the stack location pointed to by the sp field.
	 */
	/* 
	 * sp is saved right before calls but we need to save it here too so
	 * async stack walks would work.
	 */
	amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsp), AMD64_RSP, 8);
	/* Skip method (only needed for trampoline LMF frames) */
	/* Save callee saved regs */
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		int offset;

		switch (i) {
		case AMD64_RBX: offset = G_STRUCT_OFFSET (MonoLMF, rbx); break;
		case AMD64_RBP: offset = G_STRUCT_OFFSET (MonoLMF, rbp); break;
		case AMD64_R12: offset = G_STRUCT_OFFSET (MonoLMF, r12); break;
		case AMD64_R13: offset = G_STRUCT_OFFSET (MonoLMF, r13); break;
		case AMD64_R14: offset = G_STRUCT_OFFSET (MonoLMF, r14); break;
#ifndef __native_client_codegen__
		case AMD64_R15: offset = G_STRUCT_OFFSET (MonoLMF, r15); break;
#endif
#ifdef HOST_WIN32
		case AMD64_RDI: offset = G_STRUCT_OFFSET (MonoLMF, rdi); break;
		case AMD64_RSI: offset = G_STRUCT_OFFSET (MonoLMF, rsi); break;
#endif
		default:
			offset = -1;
			break;
		}

		if (offset != -1) {
			amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + offset, i, 8);
			if ((cfg->arch.omit_fp || (i != AMD64_RBP)) && cfa_offset != -1)
				mono_emit_unwind_op_offset (cfg, code, i, - (cfa_offset - (lmf_offset + offset)));
		}
	}

	/* These can't contain refs */
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rip), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsp), SLOT_NOREF);

	/* These are handled automatically by the stack marking code */
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbx), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbp), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r12), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r13), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r14), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r15), SLOT_NOREF);
#ifdef HOST_WIN32
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rdi), SLOT_NOREF);
	mini_gc_set_slot_type_from_fp (cfg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsi), SLOT_NOREF);
#endif

	return code;
}

/*
 * emit_save_lmf:
 *
 *   Emit code to push an LMF structure on the LMF stack.
 */
static guint8*
emit_save_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset, gboolean *args_clobbered)
{
	if ((lmf_tls_offset != -1) && !optimize_for_xen) {
		/*
		 * Optimized version which uses the mono_lmf TLS variable instead of 
		 * indirection through the mono_lmf_addr TLS variable.
		 */
		/* %rax = previous_lmf */
		x86_prefix (code, X86_FS_PREFIX);
		amd64_mov_reg_mem (code, AMD64_RAX, lmf_tls_offset, 8);

		/* Save previous_lmf */
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_RAX, 8);
		/* Set new lmf */
		if (lmf_offset == 0) {
			x86_prefix (code, X86_FS_PREFIX);
			amd64_mov_mem_reg (code, lmf_tls_offset, cfg->frame_reg, 8);
		} else {
			amd64_lea_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset);
			x86_prefix (code, X86_FS_PREFIX);
			amd64_mov_mem_reg (code, lmf_tls_offset, AMD64_R11, 8);
		}
	} else {
		if (lmf_addr_tls_offset != -1) {
			/* Load lmf quicky using the FS register */
			code = mono_amd64_emit_tls_get (code, AMD64_RAX, lmf_addr_tls_offset);
#ifdef HOST_WIN32
			/* The TLS key actually contains a pointer to the MonoJitTlsData structure */
			/* FIXME: Add a separate key for LMF to avoid this */
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RAX, G_STRUCT_OFFSET (MonoJitTlsData, lmf));
#endif
		}
		else {
			/* 
			 * The call might clobber argument registers, but they are already
			 * saved to the stack/global regs.
			 */
			if (args_clobbered)
				*args_clobbered = TRUE;
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
							  (gpointer)"mono_get_lmf_addr", TRUE);		
		}

		/* Save lmf_addr */
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), AMD64_RAX, sizeof(gpointer));
		/* Save previous_lmf */
		amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, sizeof(gpointer));
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, sizeof(gpointer));
		/* Set new lmf */
		amd64_lea_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset);
		amd64_mov_membase_reg (code, AMD64_RAX, 0, AMD64_R11, sizeof(gpointer));
	}

	return code;
}

/*
 * emit_save_lmf:
 *
 *   Emit code to pop an LMF structure from the LMF stack.
 */
static guint8*
emit_restore_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset)
{
	if ((lmf_tls_offset != -1) && !optimize_for_xen) {
		/*
		 * Optimized version which uses the mono_lmf TLS variable instead of indirection
		 * through the mono_lmf_addr TLS variable.
		 */
		/* reg = previous_lmf */
		amd64_mov_reg_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof(gpointer));
		x86_prefix (code, X86_FS_PREFIX);
		amd64_mov_mem_reg (code, lmf_tls_offset, AMD64_R11, 8);
	} else {
		/* Restore previous lmf */
		amd64_mov_reg_membase (code, AMD64_RCX, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), sizeof(gpointer));
		amd64_mov_reg_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), sizeof(gpointer));
		amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, sizeof(gpointer));
	}

	return code;
}

#define REAL_PRINT_REG(text,reg) \
mono_assert (reg >= 0); \
amd64_push_reg (code, AMD64_RAX); \
amd64_push_reg (code, AMD64_RDX); \
amd64_push_reg (code, AMD64_RCX); \
amd64_push_reg (code, reg); \
amd64_push_imm (code, reg); \
amd64_push_imm (code, text " %d %p\n"); \
amd64_mov_reg_imm (code, AMD64_RAX, printf); \
amd64_call_reg (code, AMD64_RAX); \
amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 3*4); \
amd64_pop_reg (code, AMD64_RCX); \
amd64_pop_reg (code, AMD64_RDX); \
amd64_pop_reg (code, AMD64_RAX);

/* benchmark and set based on cpu */
#define LOOP_ALIGNMENT 8
#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

#ifndef DISABLE_JIT

#if defined(__native_client__) || defined(__native_client_codegen__)
void mono_nacl_gc()
{
#ifdef __native_client_gc__
	__nacl_suspend_thread_if_needed();
#endif
}
#endif

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint offset;
	guint8 *code = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	guint last_offset = 0;
	int max_len;

	/* Fix max_offset estimate for each successor bb */
	if (cfg->opt & MONO_OPT_BRANCH) {
		int current_offset = cfg->code_len;
		MonoBasicBlock *current_bb;
		for (current_bb = bb; current_bb != NULL; current_bb = current_bb->next_bb) {
			current_bb->max_offset = current_offset;
			current_offset += current_bb->max_length;
		}
	}

	if (cfg->opt & MONO_OPT_LOOP) {
		int pad, align = LOOP_ALIGNMENT;
		/* set alignment depending on cpu */
		if (bb_is_loop_start (bb) && (pad = (cfg->code_len & (align - 1)))) {
			pad = align - pad;
			/*g_print ("adding %d pad at %x to loop in %s\n", pad, cfg->code_len, cfg->method->name);*/
			amd64_padding (code, pad);
			cfg->code_len += pad;
			bb->native_offset = cfg->code_len;
		}
	}

#if defined(__native_client_codegen__)
	/* For Native Client, all indirect call/jump targets must be */
	/* 32-byte aligned.  Exception handler blocks are jumped to  */
	/* indirectly as well.                                       */
	gboolean bb_needs_alignment = (bb->flags & BB_INDIRECT_JUMP_TARGET) ||
				      (bb->flags & BB_EXCEPTION_HANDLER);

	if ( bb_needs_alignment && ((cfg->code_len & kNaClAlignmentMask) != 0)) {
		int pad = kNaClAlignment - (cfg->code_len & kNaClAlignmentMask);
		if (pad != kNaClAlignment) code = mono_arch_nacl_pad(code, pad);
		cfg->code_len += pad;
		bb->native_offset = cfg->code_len;
	}
#endif  /*__native_client_codegen__*/

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		MonoProfileCoverageInfo *cov = cfg->coverage_info;
		g_assert (!cfg->compile_aot);

		cov->data [bb->dfn].cil_code = bb->cil_code;
		amd64_mov_reg_imm (code, AMD64_R11, (guint64)&cov->data [bb->dfn].count);
		/* this is not thread save, but good enough */
		amd64_inc_membase (code, AMD64_R11, 0);
	}

	offset = code - cfg->native_code;

	mono_debug_open_block (cfg, bb, offset);

    if (mono_break_at_bb_method && mono_method_desc_full_match (mono_break_at_bb_method, cfg->method) && bb->block_num == mono_break_at_bb_bb_num)
		x86_breakpoint (code);

	MONO_BB_FOR_EACH_INS (bb, ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];

#define EXTRA_CODE_SPACE (NACL_SIZE (16, 16 + kNaClAlignment))

		if (G_UNLIKELY (offset > (cfg->code_size - max_len - EXTRA_CODE_SPACE))) {
			cfg->code_size *= 2;
			cfg->native_code = mono_realloc_native_code(cfg);
			code = cfg->native_code + offset;
			cfg->stat_code_reallocs++;
		}

		if (cfg->debug_info)
			mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_BIGMUL:
			amd64_mul_reg (code, ins->sreg2, TRUE);
			break;
		case OP_BIGMUL_UN:
			amd64_mul_reg (code, ins->sreg2, FALSE);
			break;
		case OP_X86_SETEQ_MEMBASE:
			amd64_set_membase (code, X86_CC_EQ, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 1);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 2);
			break;
		case OP_STOREI4_MEMBASE_IMM:
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_STOREI1_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 1);
			break;
		case OP_STOREI2_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 2);
			break;
		/* In AMD64 NaCl, pointers are 4 bytes, */
		/*  so STORE_* != STOREI8_*. Likewise below. */
		case OP_STORE_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, sizeof(gpointer));
			break;
		case OP_STOREI8_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 8);
			break;
		case OP_STOREI4_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 4);
			break;
		case OP_STORE_MEMBASE_IMM:
#ifndef __native_client_codegen__
			/* In NaCl, this could be a PCONST type, which could */
			/* mean a pointer type was copied directly into the  */
			/* lower 32-bits of inst_imm, so for InvalidPtr==-1  */
			/* the value would be 0x00000000FFFFFFFF which is    */
			/* not proper for an imm32 unless you cast it.       */
			g_assert (amd64_is_imm32 (ins->inst_imm));
#endif
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, (gint32)ins->inst_imm, sizeof(gpointer));
			break;
		case OP_STOREI8_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_LOAD_MEM:
#ifdef __mono_ilp32__
			/* In ILP32, pointers are 4 bytes, so separate these */
			/* cases, use literal 8 below where we really want 8 */
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_mov_reg_membase (code, ins->dreg, ins->dreg, 0, sizeof(gpointer));
			break;
#endif
		case OP_LOADI8_MEM:
			// FIXME: Decompose this earlier
			if (amd64_is_imm32 (ins->inst_imm))
				amd64_mov_reg_mem (code, ins->dreg, ins->inst_imm, 8);
			else {
				amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
				amd64_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 8);
			}
			break;
		case OP_LOADI4_MEM:
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_movsxd_reg_membase (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_LOADU4_MEM:
			// FIXME: Decompose this earlier
			if (amd64_is_imm32 (ins->inst_imm))
				amd64_mov_reg_mem (code, ins->dreg, ins->inst_imm, 4);
			else {
				amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
				amd64_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 4);
			}
			break;
		case OP_LOADU1_MEM:
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_widen_membase (code, ins->dreg, ins->dreg, 0, FALSE, FALSE);
			break;
		case OP_LOADU2_MEM:
			/* For NaCl, pointers are 4 bytes, so separate these */
			/* cases, use literal 8 below where we really want 8 */
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_widen_membase (code, ins->dreg, ins->dreg, 0, FALSE, TRUE);
			break;
		case OP_LOAD_MEMBASE:
			g_assert (amd64_is_imm32 (ins->inst_offset));
			amd64_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, sizeof(gpointer));
			break;
		case OP_LOADI8_MEMBASE:
			/* Use literal 8 instead of sizeof pointer or */
			/* register, we really want 8 for this opcode */
			g_assert (amd64_is_imm32 (ins->inst_offset));
			amd64_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, 8);
			break;
		case OP_LOADI4_MEMBASE:
			amd64_movsxd_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADU4_MEMBASE:
			amd64_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_LOADU1_MEMBASE:
			/* The cpu zero extends the result into 64 bits */
			amd64_widen_membase_size (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, FALSE, 4);
			break;
		case OP_LOADI1_MEMBASE:
			amd64_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, FALSE);
			break;
		case OP_LOADU2_MEMBASE:
			/* The cpu zero extends the result into 64 bits */
			amd64_widen_membase_size (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, TRUE, 4);
			break;
		case OP_LOADI2_MEMBASE:
			amd64_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, TRUE);
			break;
		case OP_AMD64_LOADI8_MEMINDEX:
			amd64_mov_reg_memindex_size (code, ins->dreg, ins->inst_basereg, 0, ins->inst_indexreg, 0, 8);
			break;
		case OP_LCONV_TO_I1:
		case OP_ICONV_TO_I1:
		case OP_SEXT_I1:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, TRUE, FALSE);
			break;
		case OP_LCONV_TO_I2:
		case OP_ICONV_TO_I2:
		case OP_SEXT_I2:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, TRUE, TRUE);
			break;
		case OP_LCONV_TO_U1:
		case OP_ICONV_TO_U1:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, FALSE, FALSE);
			break;
		case OP_LCONV_TO_U2:
		case OP_ICONV_TO_U2:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, FALSE, TRUE);
			break;
		case OP_ZEXT_I4:
			/* Clean out the upper word */
			amd64_mov_reg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_SEXT_I4:
			amd64_movsxd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_COMPARE:
		case OP_LCOMPARE:
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_CMP, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_COMPARE_REG_MEMBASE:
			amd64_alu_reg_membase (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_TEST_NULL:
			amd64_test_reg_reg_size (code, ins->sreg1, ins->sreg1, 4);
			break;
		case OP_AMD64_TEST_NULL:
			amd64_test_reg_reg (code, ins->sreg1, ins->sreg1);
			break;

		case OP_X86_ADD_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_ADD, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_SUB_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_SUB, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_AND_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_AND, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_OR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_OR, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_XOR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_XOR, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;

		case OP_X86_ADD_MEMBASE_IMM:
			/* FIXME: Make a 64 version too */
			amd64_alu_membase_imm_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_SUB_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_AND_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_OR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_XOR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_ADD_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_SUB_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_AND_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_OR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_XOR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_INC_MEMBASE:
			amd64_inc_membase_size (code, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_X86_INC_REG:
			amd64_inc_reg_size (code, ins->dreg, 4);
			break;
		case OP_X86_DEC_MEMBASE:
			amd64_dec_membase_size (code, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_X86_DEC_REG:
			amd64_dec_reg_size (code, ins->dreg, 4);
			break;
		case OP_X86_MUL_REG_MEMBASE:
		case OP_X86_MUL_MEMBASE_REG:
			amd64_imul_reg_membase_size (code, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_AMD64_ICOMPARE_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_AMD64_ICOMPARE_MEMBASE_IMM:
			amd64_alu_membase_imm_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_AMD64_COMPARE_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_COMPARE_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_X86_COMPARE_MEMBASE8_IMM:
			amd64_alu_membase8_imm_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_AMD64_ICOMPARE_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_AMD64_COMPARE_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;

		case OP_AMD64_ADD_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_ADD, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_SUB_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_SUB, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_AND_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_AND, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_OR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_OR, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_XOR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_XOR, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;

		case OP_AMD64_ADD_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_SUB_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_AND_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_OR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_XOR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;

		case OP_AMD64_ADD_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_SUB_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_AND_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_OR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_XOR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;

		case OP_BREAK:
			amd64_breakpoint (code);
			break;
		case OP_RELAXED_NOP:
			x86_prefix (code, X86_REP_PREFIX);
			x86_nop (code);
			break;
		case OP_HARD_NOP:
			x86_nop (code);
			break;
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_STORE:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_SEQ_POINT: {
			int i;

			/* 
			 * Read from the single stepping trigger page. This will cause a
			 * SIGSEGV when single stepping is enabled.
			 * We do this _before_ the breakpoint, so single stepping after
			 * a breakpoint is hit will step to the next IL offset.
			 */
			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				MonoInst *var = cfg->arch.ss_trigger_page_var;

				amd64_mov_reg_membase (code, AMD64_R11, var->inst_basereg, var->inst_offset, 8);
				amd64_alu_membase_imm_size (code, X86_CMP, AMD64_R11, 0, 0, 4);
			}

			/* 
			 * This is the address which is saved in seq points, 
			 */
			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			if (cfg->compile_aot) {
				guint32 offset = code - cfg->native_code;
				guint32 val;
				MonoInst *info_var = cfg->arch.seq_point_info_var;

				/* Load info var */
				amd64_mov_reg_membase (code, AMD64_R11, info_var->inst_basereg, info_var->inst_offset, 8);
				val = ((offset) * sizeof (guint8*)) + G_STRUCT_OFFSET (SeqPointInfo, bp_addrs);
				/* Load the info->bp_addrs [offset], which is either a valid address or the address of a trigger page */
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, val, 8);
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, 8);
			} else {
				/* 
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				for (i = 0; i < breakpoint_size; ++i)
					x86_nop (code);
			}
			/*
			 * Add an additional nop so skipping the bp doesn't cause the ip to point
			 * to another IL offset.
			 */
			x86_nop (code);
			break;
		}
		case OP_ADDCC:
		case OP_LADD:
			amd64_alu_reg_reg (code, X86_ADD, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
			amd64_alu_reg_reg (code, X86_ADC, ins->sreg1, ins->sreg2);
			break;
		case OP_ADD_IMM:
		case OP_LADD_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_ADD, ins->dreg, ins->inst_imm);
			break;
		case OP_ADC_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_ADC, ins->dreg, ins->inst_imm);
			break;
		case OP_SUBCC:
		case OP_LSUB:
			amd64_alu_reg_reg (code, X86_SUB, ins->sreg1, ins->sreg2);
			break;
		case OP_SBB:
			amd64_alu_reg_reg (code, X86_SBB, ins->sreg1, ins->sreg2);
			break;
		case OP_SUB_IMM:
		case OP_LSUB_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_SUB, ins->dreg, ins->inst_imm);
			break;
		case OP_SBB_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_SBB, ins->dreg, ins->inst_imm);
			break;
		case OP_LAND:
			amd64_alu_reg_reg (code, X86_AND, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_LAND_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_imm);
			break;
		case OP_LMUL:
			amd64_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MUL_IMM:
		case OP_LMUL_IMM:
		case OP_IMUL_IMM: {
			guint32 size = (ins->opcode == OP_IMUL_IMM) ? 4 : 8;
			
			switch (ins->inst_imm) {
			case 2:
				/* MOV r1, r2 */
				/* ADD r1, r1 */
				if (ins->dreg != ins->sreg1)
					amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, size);
				amd64_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 3:
				/* LEA r1, [r2 + r2*2] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				break;
			case 5:
				/* LEA r1, [r2 + r2*4] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				break;
			case 6:
				/* LEA r1, [r2 + r2*2] */
				/* ADD r1, r1          */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				amd64_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 9:
				/* LEA r1, [r2 + r2*8] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 3);
				break;
			case 10:
				/* LEA r1, [r2 + r2*4] */
				/* ADD r1, r1          */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				amd64_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 12:
				/* LEA r1, [r2 + r2*2] */
				/* SHL r1, 2           */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				amd64_shift_reg_imm (code, X86_SHL, ins->dreg, 2);
				break;
			case 25:
				/* LEA r1, [r2 + r2*4] */
				/* LEA r1, [r1 + r1*4] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				amd64_lea_memindex (code, ins->dreg, ins->dreg, 0, ins->dreg, 2);
				break;
			case 100:
				/* LEA r1, [r2 + r2*4] */
				/* SHL r1, 2           */
				/* LEA r1, [r1 + r1*4] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				amd64_shift_reg_imm (code, X86_SHL, ins->dreg, 2);
				amd64_lea_memindex (code, ins->dreg, ins->dreg, 0, ins->dreg, 2);
				break;
			default:
				amd64_imul_reg_reg_imm_size (code, ins->dreg, ins->sreg1, ins->inst_imm, size);
				break;
			}
			break;
		}
		case OP_LDIV:
		case OP_LREM:
			/* Regalloc magic makes the div/rem cases the same */
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_cdq (code);
				amd64_div_membase (code, AMD64_RSP, -8, TRUE);
			} else {
				amd64_cdq (code);
				amd64_div_reg (code, ins->sreg2, TRUE);
			}
			break;
		case OP_LDIV_UN:
		case OP_LREM_UN:
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_membase (code, AMD64_RSP, -8, FALSE);
			} else {
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_reg (code, ins->sreg2, FALSE);
			}
			break;
		case OP_IDIV:
		case OP_IREM:
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_cdq_size (code, 4);
				amd64_div_membase_size (code, AMD64_RSP, -8, TRUE, 4);
			} else {
				amd64_cdq_size (code, 4);
				amd64_div_reg_size (code, ins->sreg2, TRUE, 4);
			}
			break;
		case OP_IDIV_UN:
		case OP_IREM_UN:
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_membase_size (code, AMD64_RSP, -8, FALSE, 4);
			} else {
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_reg_size (code, ins->sreg2, FALSE, 4);
			}
			break;
		case OP_IREM_IMM: {
			int power = mono_is_power_of_two (ins->inst_imm);

			g_assert (ins->sreg1 == X86_EAX);
			g_assert (ins->dreg == X86_EAX);
			g_assert (power >= 0);

			if (power == 0) {
				amd64_mov_reg_imm (code, ins->dreg, 0);
				break;
			}

			/* Based on gcc code */

			/* Add compensation for negative dividents */
			amd64_mov_reg_reg_size (code, AMD64_RDX, AMD64_RAX, 4);
			if (power > 1)
				amd64_shift_reg_imm_size (code, X86_SAR, AMD64_RDX, 31, 4);
			amd64_shift_reg_imm_size (code, X86_SHR, AMD64_RDX, 32 - power, 4);
			amd64_alu_reg_reg_size (code, X86_ADD, AMD64_RAX, AMD64_RDX, 4);
			/* Compute remainder */
			amd64_alu_reg_imm_size (code, X86_AND, AMD64_RAX, (1 << power) - 1, 4);
			/* Remove compensation */
			amd64_alu_reg_reg_size (code, X86_SUB, AMD64_RAX, AMD64_RDX, 4);
			break;
		}
		case OP_LMUL_OVF:
			amd64_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		case OP_LOR:
			amd64_alu_reg_reg (code, X86_OR, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_LOR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_OR, ins->sreg1, ins->inst_imm);
			break;
		case OP_LXOR:
			amd64_alu_reg_reg (code, X86_XOR, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_LXOR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_XOR, ins->sreg1, ins->inst_imm);
			break;
		case OP_LSHL:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg (code, X86_SHL, ins->dreg);
			break;
		case OP_LSHR:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg (code, X86_SAR, ins->dreg);
			break;
		case OP_SHR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm_size (code, X86_SAR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LSHR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm (code, X86_SAR, ins->dreg, ins->inst_imm);
			break;
		case OP_SHR_UN_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm_size (code, X86_SHR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LSHR_UN_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_imm);
			break;
		case OP_LSHR_UN:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg (code, X86_SHR, ins->dreg);
			break;
		case OP_SHL_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm_size (code, X86_SHL, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LSHL_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm (code, X86_SHL, ins->dreg, ins->inst_imm);
			break;

		case OP_IADDCC:
		case OP_IADD:
			amd64_alu_reg_reg_size (code, X86_ADD, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IADC:
			amd64_alu_reg_reg_size (code, X86_ADC, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IADD_IMM:
			amd64_alu_reg_imm_size (code, X86_ADD, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_IADC_IMM:
			amd64_alu_reg_imm_size (code, X86_ADC, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISUBCC:
		case OP_ISUB:
			amd64_alu_reg_reg_size (code, X86_SUB, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_ISBB:
			amd64_alu_reg_reg_size (code, X86_SBB, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_ISUB_IMM:
			amd64_alu_reg_imm_size (code, X86_SUB, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISBB_IMM:
			amd64_alu_reg_imm_size (code, X86_SBB, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_IAND:
			amd64_alu_reg_reg_size (code, X86_AND, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IAND_IMM:
			amd64_alu_reg_imm_size (code, X86_AND, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_IOR:
			amd64_alu_reg_reg_size (code, X86_OR, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IOR_IMM:
			amd64_alu_reg_imm_size (code, X86_OR, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_IXOR:
			amd64_alu_reg_reg_size (code, X86_XOR, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IXOR_IMM:
			amd64_alu_reg_imm_size (code, X86_XOR, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_INEG:
			amd64_neg_reg_size (code, ins->sreg1, 4);
			break;
		case OP_INOT:
			amd64_not_reg_size (code, ins->sreg1, 4);
			break;
		case OP_ISHL:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg_size (code, X86_SHL, ins->dreg, 4);
			break;
		case OP_ISHR:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg_size (code, X86_SAR, ins->dreg, 4);
			break;
		case OP_ISHR_IMM:
			amd64_shift_reg_imm_size (code, X86_SAR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISHR_UN_IMM:
			amd64_shift_reg_imm_size (code, X86_SHR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISHR_UN:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg_size (code, X86_SHR, ins->dreg, 4);
			break;
		case OP_ISHL_IMM:
			amd64_shift_reg_imm_size (code, X86_SHL, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_IMUL:
			amd64_imul_reg_reg_size (code, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IMUL_OVF:
			amd64_imul_reg_reg_size (code, ins->sreg1, ins->sreg2, 4);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		case OP_IMUL_OVF_UN:
		case OP_LMUL_OVF_UN: {
			/* the mul operation and the exception check should most likely be split */
			int non_eax_reg, saved_eax = FALSE, saved_edx = FALSE;
			int size = (ins->opcode == OP_IMUL_OVF_UN) ? 4 : 8;
			/*g_assert (ins->sreg2 == X86_EAX);
			g_assert (ins->dreg == X86_EAX);*/
			if (ins->sreg2 == X86_EAX) {
				non_eax_reg = ins->sreg1;
			} else if (ins->sreg1 == X86_EAX) {
				non_eax_reg = ins->sreg2;
			} else {
				/* no need to save since we're going to store to it anyway */
				if (ins->dreg != X86_EAX) {
					saved_eax = TRUE;
					amd64_push_reg (code, X86_EAX);
				}
				amd64_mov_reg_reg (code, X86_EAX, ins->sreg1, size);
				non_eax_reg = ins->sreg2;
			}
			if (ins->dreg == X86_EDX) {
				if (!saved_eax) {
					saved_eax = TRUE;
					amd64_push_reg (code, X86_EAX);
				}
			} else {
				saved_edx = TRUE;
				amd64_push_reg (code, X86_EDX);
			}
			amd64_mul_reg_size (code, non_eax_reg, FALSE, size);
			/* save before the check since pop and mov don't change the flags */
			if (ins->dreg != X86_EAX)
				amd64_mov_reg_reg (code, ins->dreg, X86_EAX, size);
			if (saved_edx)
				amd64_pop_reg (code, X86_EDX);
			if (saved_eax)
				amd64_pop_reg (code, X86_EAX);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		}
		case OP_ICOMPARE:
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_ICOMPARE_IMM:
			amd64_alu_reg_imm_size (code, X86_CMP, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_IBEQ:
		case OP_IBLT:
		case OP_IBGT:
		case OP_IBGE:
		case OP_IBLE:
		case OP_LBEQ:
		case OP_LBLT:
		case OP_LBGT:
		case OP_LBGE:
		case OP_LBLE:
		case OP_IBNE_UN:
		case OP_IBLT_UN:
		case OP_IBGT_UN:
		case OP_IBGE_UN:
		case OP_IBLE_UN:
		case OP_LBNE_UN:
		case OP_LBLT_UN:
		case OP_LBGT_UN:
		case OP_LBGE_UN:
		case OP_LBLE_UN:
			EMIT_COND_BRANCH (ins, cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)]);
			break;

		case OP_CMOV_IEQ:
		case OP_CMOV_IGE:
		case OP_CMOV_IGT:
		case OP_CMOV_ILE:
		case OP_CMOV_ILT:
		case OP_CMOV_INE_UN:
		case OP_CMOV_IGE_UN:
		case OP_CMOV_IGT_UN:
		case OP_CMOV_ILE_UN:
		case OP_CMOV_ILT_UN:
		case OP_CMOV_LEQ:
		case OP_CMOV_LGE:
		case OP_CMOV_LGT:
		case OP_CMOV_LLE:
		case OP_CMOV_LLT:
		case OP_CMOV_LNE_UN:
		case OP_CMOV_LGE_UN:
		case OP_CMOV_LGT_UN:
		case OP_CMOV_LLE_UN:
		case OP_CMOV_LLT_UN:
			g_assert (ins->dreg == ins->sreg1);
			/* This needs to operate on 64 bit values */
			amd64_cmov_reg (code, cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)], ins->dreg, ins->sreg2);
			break;

		case OP_LNOT:
			amd64_not_reg (code, ins->sreg1);
			break;
		case OP_LNEG:
			amd64_neg_reg (code, ins->sreg1);
			break;

		case OP_ICONST:
		case OP_I8CONST:
			if ((((guint64)ins->inst_c0) >> 32) == 0)
				amd64_mov_reg_imm_size (code, ins->dreg, ins->inst_c0, 4);
			else
				amd64_mov_reg_imm_size (code, ins->dreg, ins->inst_c0, 8);
			break;
		case OP_AOTCONST:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			amd64_mov_reg_membase (code, ins->dreg, AMD64_RIP, 0, sizeof(gpointer));
			break;
		case OP_JUMP_TABLE:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			amd64_mov_reg_imm_size (code, ins->dreg, 0, 8);
			break;
		case OP_MOVE:
			amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, sizeof(mgreg_t));
			break;
		case OP_AMD64_SET_XMMREG_R4: {
			amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_AMD64_SET_XMMREG_R8: {
			if (ins->dreg != ins->sreg1)
				amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_TAILCALL: {
			MonoCallInst *call = (MonoCallInst*)ins;
			int pos = 0, i;

			/* FIXME: no tracing support... */
			if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
				code = mono_arch_instrument_epilog_full (cfg, mono_profiler_method_leave, code, FALSE, TRUE);

			g_assert (!cfg->method->save_lmf);

			if (cfg->arch.omit_fp) {
				guint32 save_offset = 0;
				/* Pop callee-saved registers */
				for (i = 0; i < AMD64_NREG; ++i)
					if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
						amd64_mov_reg_membase (code, i, AMD64_RSP, save_offset, 8);
						save_offset += 8;
					}
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, cfg->arch.stack_alloc_size);

				// FIXME:
				if (call->stack_usage)
					NOT_IMPLEMENTED;
			}
			else {
				for (i = 0; i < AMD64_NREG; ++i)
					if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i)))
						pos -= sizeof(mgreg_t);

				/* Restore callee-saved registers */
				for (i = AMD64_NREG - 1; i > 0; --i) {
					if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
						amd64_mov_reg_membase (code, i, AMD64_RBP, pos, sizeof(mgreg_t));
						pos += sizeof(mgreg_t);
					}
				}

				/* Copy arguments on the stack to our argument area */
				for (i = 0; i < call->stack_usage; i += sizeof(mgreg_t)) {
					amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, i, sizeof(mgreg_t));
					amd64_mov_membase_reg (code, AMD64_RBP, 16 + i, AMD64_RAX, sizeof(mgreg_t));
				}
			
				if (pos)
					amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, pos);

				amd64_leave (code);
			}

			offset = code - cfg->native_code;
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			if (cfg->compile_aot)
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
			else
				amd64_set_reg_template (code, AMD64_R11);
			amd64_jump_reg (code, AMD64_R11);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			amd64_alu_membase_imm_size (code, X86_CMP, ins->sreg1, 0, 0, 4);
			break;
		case OP_ARGLIST: {
			amd64_lea_membase (code, AMD64_R11, cfg->frame_reg, cfg->sig_cookie);
			amd64_mov_membase_reg (code, ins->sreg1, 0, AMD64_R11, sizeof(gpointer));
			break;
		}
		case OP_CALL:
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
			call = (MonoCallInst*)ins;
			/*
			 * The AMD64 ABI forces callers to know about varargs.
			 */
			if ((call->signature->call_convention == MONO_CALL_VARARG) && (call->signature->pinvoke))
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
			else if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && (cfg->method->klass->image != mono_defaults.corlib)) {
				/* 
				 * Since the unmanaged calling convention doesn't contain a 
				 * 'vararg' entry, we have to treat every pinvoke call as a
				 * potential vararg call.
				 */
				guint32 nregs, i;
				nregs = 0;
				for (i = 0; i < AMD64_XMM_NREG; ++i)
					if (call->used_fregs & (1 << i))
						nregs ++;
				if (!nregs)
					amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
				else
					amd64_mov_reg_imm (code, AMD64_RAX, nregs);
			}

			if (ins->flags & MONO_INST_HAS_METHOD)
				code = emit_call (cfg, code, MONO_PATCH_INFO_METHOD, call->method, FALSE);
			else
				code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, call->fptr, FALSE);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention) && !cfg->arch.no_pushes)
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, call->stack_usage);
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			call = (MonoCallInst*)ins;

			if (AMD64_IS_ARGUMENT_REG (ins->sreg1)) {
				amd64_mov_reg_reg (code, AMD64_R11, ins->sreg1, 8);
				ins->sreg1 = AMD64_R11;
			}

			/*
			 * The AMD64 ABI forces callers to know about varargs.
			 */
			if ((call->signature->call_convention == MONO_CALL_VARARG) && (call->signature->pinvoke)) {
				if (ins->sreg1 == AMD64_RAX) {
					amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, 8);
					ins->sreg1 = AMD64_R11;
				}
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
			} else if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && (cfg->method->klass->image != mono_defaults.corlib)) {
				/* 
				 * Since the unmanaged calling convention doesn't contain a 
				 * 'vararg' entry, we have to treat every pinvoke call as a
				 * potential vararg call.
				 */
				guint32 nregs, i;
				nregs = 0;
				for (i = 0; i < AMD64_XMM_NREG; ++i)
					if (call->used_fregs & (1 << i))
						nregs ++;
				if (ins->sreg1 == AMD64_RAX) {
					amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, 8);
					ins->sreg1 = AMD64_R11;
				}
				if (!nregs)
					amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
				else
					amd64_mov_reg_imm (code, AMD64_RAX, nregs);
			}

			amd64_call_reg (code, ins->sreg1);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention) && !cfg->arch.no_pushes)
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, call->stack_usage);
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			call = (MonoCallInst*)ins;

			amd64_call_membase (code, ins->sreg1, ins->inst_offset);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention) && !cfg->arch.no_pushes)
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, call->stack_usage);
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_DYN_CALL: {
			int i;
			MonoInst *var = cfg->dyn_call_var;

			g_assert (var->opcode == OP_REGOFFSET);

			/* r11 = args buffer filled by mono_arch_get_dyn_call_args () */
			amd64_mov_reg_reg (code, AMD64_R11, ins->sreg1, 8);
			/* r10 = ftn */
			amd64_mov_reg_reg (code, AMD64_R10, ins->sreg2, 8);

			/* Save args buffer */
			amd64_mov_membase_reg (code, var->inst_basereg, var->inst_offset, AMD64_R11, 8);

			/* Set argument registers */
			for (i = 0; i < PARAM_REGS; ++i)
				amd64_mov_reg_membase (code, param_regs [i], AMD64_R11, i * sizeof(mgreg_t), sizeof(mgreg_t));
			
			/* Make the call */
			amd64_call_reg (code, AMD64_R10);

			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;

			/* Save result */
			amd64_mov_reg_membase (code, AMD64_R11, var->inst_basereg, var->inst_offset, 8);
			amd64_mov_membase_reg (code, AMD64_R11, G_STRUCT_OFFSET (DynCallArgs, res), AMD64_RAX, 8);
			break;
		}
		case OP_AMD64_SAVE_SP_TO_LMF: {
			MonoInst *lmf_var = cfg->arch.lmf_var;
			amd64_mov_membase_reg (code, cfg->frame_reg, lmf_var->inst_offset + G_STRUCT_OFFSET (MonoLMF, rsp), AMD64_RSP, 8);
			break;
		}
		case OP_X86_PUSH:
			g_assert (!cfg->arch.no_pushes);
			amd64_push_reg (code, ins->sreg1);
			break;
		case OP_X86_PUSH_IMM:
			g_assert (!cfg->arch.no_pushes);
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_push_imm (code, ins->inst_imm);
			break;
		case OP_X86_PUSH_MEMBASE:
			g_assert (!cfg->arch.no_pushes);
			amd64_push_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_PUSH_OBJ: {
			int size = ALIGN_TO (ins->inst_imm, 8);

			g_assert (!cfg->arch.no_pushes);

			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, size);
			amd64_push_reg (code, AMD64_RDI);
			amd64_push_reg (code, AMD64_RSI);
			amd64_push_reg (code, AMD64_RCX);
			if (ins->inst_offset)
				amd64_lea_membase (code, AMD64_RSI, ins->inst_basereg, ins->inst_offset);
			else
				amd64_mov_reg_reg (code, AMD64_RSI, ins->inst_basereg, 8);
			amd64_lea_membase (code, AMD64_RDI, AMD64_RSP, (3 * 8));
			amd64_mov_reg_imm (code, AMD64_RCX, (size >> 3));
			amd64_cld (code);
			amd64_prefix (code, X86_REP_PREFIX);
			amd64_movsd (code);
			amd64_pop_reg (code, AMD64_RCX);
			amd64_pop_reg (code, AMD64_RSI);
			amd64_pop_reg (code, AMD64_RDI);
			break;
		}
		case OP_X86_LEA:
			amd64_lea_memindex (code, ins->dreg, ins->sreg1, ins->inst_imm, ins->sreg2, ins->backend.shift_amount);
			break;
		case OP_X86_LEA_MEMBASE:
			amd64_lea_membase (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_XCHG:
			amd64_xchg_reg_reg (code, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_LOCALLOC:
			/* keep alignment */
			amd64_alu_reg_imm (code, X86_ADD, ins->sreg1, MONO_ARCH_FRAME_ALIGNMENT - 1);
			amd64_alu_reg_imm (code, X86_AND, ins->sreg1, ~(MONO_ARCH_FRAME_ALIGNMENT - 1));
			code = mono_emit_stack_alloc (cfg, code, ins);
			amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);
			if (cfg->param_area && cfg->arch.no_pushes)
				amd64_alu_reg_imm (code, X86_ADD, ins->dreg, cfg->param_area);
			break;
		case OP_LOCALLOC_IMM: {
			guint32 size = ins->inst_imm;
			size = (size + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

			if (ins->flags & MONO_INST_INIT) {
				if (size < 64) {
					int i;

					amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, size);
					amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);

					for (i = 0; i < size; i += 8)
						amd64_mov_membase_reg (code, AMD64_RSP, i, ins->dreg, 8);
					amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);					
				} else {
					amd64_mov_reg_imm (code, ins->dreg, size);
					ins->sreg1 = ins->dreg;

					code = mono_emit_stack_alloc (cfg, code, ins);
					amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);
				}
			} else {
				amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, size);
				amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);
			}
			if (cfg->param_area && cfg->arch.no_pushes)
				amd64_alu_reg_imm (code, X86_ADD, ins->dreg, cfg->param_area);
			break;
		}
		case OP_THROW: {
			amd64_mov_reg_reg (code, AMD64_ARG_REG1, ins->sreg1, 8);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception", FALSE);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_RETHROW: {
			amd64_mov_reg_reg (code, AMD64_ARG_REG1, ins->sreg1, 8);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception", FALSE);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		}
		case OP_CALL_HANDLER: 
			/* Align stack */
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			amd64_call_imm (code, 0);
			mono_cfg_add_try_hole (cfg, ins->inst_eh_block, code, bb);
			/* Restore stack alignment */
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
			break;
		case OP_START_HANDLER: {
			/* Even though we're saving RSP, use sizeof */
			/* gpointer because spvar is of type IntPtr */
			/* see: mono_create_spvar_for_region */
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			amd64_mov_membase_reg (code, spvar->inst_basereg, spvar->inst_offset, AMD64_RSP, sizeof(gpointer));

			if ((MONO_BBLOCK_IS_IN_REGION (bb, MONO_REGION_FINALLY) ||
				 MONO_BBLOCK_IS_IN_REGION (bb, MONO_REGION_FINALLY)) &&
				cfg->param_area && cfg->arch.no_pushes) {
				amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT));
			}
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			amd64_mov_reg_membase (code, AMD64_RSP, spvar->inst_basereg, spvar->inst_offset, sizeof(gpointer));
			amd64_ret (code);
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			amd64_mov_reg_membase (code, AMD64_RSP, spvar->inst_basereg, spvar->inst_offset, sizeof(gpointer));
			/* The local allocator will put the result into RAX */
			amd64_ret (code);
			break;
		}

		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case OP_BR:
			//g_print ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
			//if ((ins->inst_target_bb == bb->next_bb) && ins == bb->last_ins)
			//break;
				if (ins->inst_target_bb->native_offset) {
					amd64_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
				} else {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
					if ((cfg->opt & MONO_OPT_BRANCH) &&
					    x86_is_imm8 (ins->inst_target_bb->max_offset - offset))
						x86_jump8 (code, 0);
					else 
						x86_jump32 (code, 0);
			}
			break;
		case OP_BR_REG:
			amd64_jump_reg (code, ins->sreg1);
			break;
		case OP_CEQ:
		case OP_LCEQ:
		case OP_ICEQ:
		case OP_CLT:
		case OP_LCLT:
		case OP_ICLT:
		case OP_CGT:
		case OP_ICGT:
		case OP_LCGT:
		case OP_CLT_UN:
		case OP_LCLT_UN:
		case OP_ICLT_UN:
		case OP_CGT_UN:
		case OP_LCGT_UN:
		case OP_ICGT_UN:
			amd64_set_reg (code, cc_table [mono_opcode_to_cond (ins->opcode)], ins->dreg, cc_signed_table [mono_opcode_to_cond (ins->opcode)]);
			amd64_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
		case OP_COND_EXC_IEQ:
		case OP_COND_EXC_INE_UN:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_ILT_UN:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_IGT_UN:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_IGE_UN:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_ILE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)], ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
		case OP_COND_EXC_NO:
		case OP_COND_EXC_C:
		case OP_COND_EXC_NC:
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_EQ], 
						    (ins->opcode < OP_COND_EXC_NE_UN), ins->inst_p1);
			break;
		case OP_COND_EXC_IOV:
		case OP_COND_EXC_INO:
		case OP_COND_EXC_IC:
		case OP_COND_EXC_INC:
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_IEQ], 
						    (ins->opcode < OP_COND_EXC_INE_UN), ins->inst_p1);
			break;

		/* floating point opcodes */
		case OP_R8CONST: {
			double d = *(double *)ins->inst_p0;

			if ((d == 0.0) && (mono_signbit (d) == 0)) {
				amd64_sse_xorpd_reg_reg (code, ins->dreg, ins->dreg);
			}
			else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, ins->inst_p0);
				amd64_sse_movsd_reg_membase (code, ins->dreg, AMD64_RIP, 0);
			}
			break;
		}
		case OP_R4CONST: {
			float f = *(float *)ins->inst_p0;

			if ((f == 0.0) && (mono_signbit (f) == 0)) {
				amd64_sse_xorpd_reg_reg (code, ins->dreg, ins->dreg);
			}
			else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R4, ins->inst_p0);
				amd64_sse_movss_reg_membase (code, ins->dreg, AMD64_RIP, 0);
				amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, ins->dreg);
			}
			break;
		}
		case OP_STORER8_MEMBASE_REG:
			amd64_sse_movsd_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1);
			break;
		case OP_LOADR8_MEMBASE:
			amd64_sse_movsd_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_STORER4_MEMBASE_REG:
			/* This requires a double->single conversion */
			amd64_sse_cvtsd2ss_reg_reg (code, AMD64_XMM15, ins->sreg1);
			amd64_sse_movss_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, AMD64_XMM15);
			break;
		case OP_LOADR4_MEMBASE:
			amd64_sse_movss_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, ins->dreg);
			break;
		case OP_ICONV_TO_R4: /* FIXME: change precision */
		case OP_ICONV_TO_R8:
			amd64_sse_cvtsi2sd_reg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_LCONV_TO_R4: /* FIXME: change precision */
		case OP_LCONV_TO_R8:
			amd64_sse_cvtsi2sd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_FCONV_TO_R4:
			/* FIXME: nothing to do ?? */
			break;
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			break;
		case OP_FCONV_TO_U4:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);			
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_FCONV_TO_I8:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 8, TRUE);
			break;
		case OP_LCONV_TO_R_UN: { 
			guint8 *br [2];

			/* Based on gcc code */
			amd64_test_reg_reg (code, ins->sreg1, ins->sreg1);
			br [0] = code; x86_branch8 (code, X86_CC_S, 0, TRUE);

			/* Positive case */
			amd64_sse_cvtsi2sd_reg_reg (code, ins->dreg, ins->sreg1);
			br [1] = code; x86_jump8 (code, 0);
			amd64_patch (br [0], code);

			/* Negative case */
			/* Save to the red zone */
			amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RAX, 8);
			amd64_mov_membase_reg (code, AMD64_RSP, -16, AMD64_RCX, 8);
			amd64_mov_reg_reg (code, AMD64_RCX, ins->sreg1, 8);
			amd64_mov_reg_reg (code, AMD64_RAX, ins->sreg1, 8);
			amd64_alu_reg_imm (code, X86_AND, AMD64_RCX, 1);
			amd64_shift_reg_imm (code, X86_SHR, AMD64_RAX, 1);
			amd64_alu_reg_imm (code, X86_OR, AMD64_RAX, AMD64_RCX);
			amd64_sse_cvtsi2sd_reg_reg (code, ins->dreg, AMD64_RAX);
			amd64_sse_addsd_reg_reg (code, ins->dreg, ins->dreg);
			/* Restore */
			amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RSP, -16, 8);
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, -8, 8);
			amd64_patch (br [1], code);
			break;
		}
		case OP_LCONV_TO_OVF_U4:
			amd64_alu_reg_imm (code, X86_CMP, ins->sreg1, 0);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_LT, TRUE, "OverflowException");
			amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, 8);
			break;
		case OP_LCONV_TO_OVF_I4_UN:
			amd64_alu_reg_imm (code, X86_CMP, ins->sreg1, 0x7fffffff);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_GT, FALSE, "OverflowException");
			amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, 8);
			break;
		case OP_FMOVE:
			if (ins->dreg != ins->sreg1)
				amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_FADD:
			amd64_sse_addsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;
		case OP_FSUB:
			amd64_sse_subsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;		
		case OP_FMUL:
			amd64_sse_mulsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;		
		case OP_FDIV:
			amd64_sse_divsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;		
		case OP_FNEG: {
			static double r8_0 = -0.0;

			g_assert (ins->sreg1 == ins->dreg);
					
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, &r8_0);
			amd64_sse_xorpd_reg_membase (code, ins->dreg, AMD64_RIP, 0);
			break;
		}
		case OP_SIN:
			EMIT_SSE2_FPFUNC (code, fsin, ins->dreg, ins->sreg1);
			break;		
		case OP_COS:
			EMIT_SSE2_FPFUNC (code, fcos, ins->dreg, ins->sreg1);
			break;		
		case OP_ABS: {
			static guint64 d = 0x7fffffffffffffffUL;

			g_assert (ins->sreg1 == ins->dreg);
					
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, &d);
			amd64_sse_andpd_reg_membase (code, ins->dreg, AMD64_RIP, 0);
			break;		
		}
		case OP_SQRT:
			EMIT_SSE2_FPFUNC (code, fsqrt, ins->dreg, ins->sreg1);
			break;
		case OP_IMIN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			amd64_cmov_reg_size (code, X86_CC_GT, TRUE, ins->dreg, ins->sreg2, 4);
			break;
		case OP_IMIN_UN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			amd64_cmov_reg_size (code, X86_CC_GT, FALSE, ins->dreg, ins->sreg2, 4);
			break;
		case OP_IMAX:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			amd64_cmov_reg_size (code, X86_CC_LT, TRUE, ins->dreg, ins->sreg2, 4);
			break;
		case OP_IMAX_UN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			amd64_cmov_reg_size (code, X86_CC_LT, FALSE, ins->dreg, ins->sreg2, 4);
			break;
		case OP_LMIN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			amd64_cmov_reg (code, X86_CC_GT, TRUE, ins->dreg, ins->sreg2);
			break;
		case OP_LMIN_UN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			amd64_cmov_reg (code, X86_CC_GT, FALSE, ins->dreg, ins->sreg2);
			break;
		case OP_LMAX:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			amd64_cmov_reg (code, X86_CC_LT, TRUE, ins->dreg, ins->sreg2);
			break;
		case OP_LMAX_UN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			amd64_cmov_reg (code, X86_CC_LT, FALSE, ins->dreg, ins->sreg2);
			break;	
		case OP_X86_FPOP:
			break;		
		case OP_FCOMPARE:
			/* 
			 * The two arguments are swapped because the fbranch instructions
			 * depend on this for the non-sse case to work.
			 */
			amd64_sse_comisd_reg_reg (code, ins->sreg2, ins->sreg1);
			break;
		case OP_FCEQ: {
			/* zeroing the register at the start results in 
			 * shorter and faster code (we can also remove the widening op)
			 */
			guchar *unordered_check;
			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_reg (code, ins->sreg1, ins->sreg2);
			unordered_check = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			amd64_set_reg (code, X86_CC_EQ, ins->dreg, FALSE);
			amd64_patch (unordered_check, code);
			break;
		}
		case OP_FCLT:
		case OP_FCLT_UN:
			/* zeroing the register at the start results in 
			 * shorter and faster code (we can also remove the widening op)
			 */
			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_reg (code, ins->sreg2, ins->sreg1);
			if (ins->opcode == OP_FCLT_UN) {
				guchar *unordered_check = code;
				guchar *jump_to_end;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				amd64_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
				jump_to_end = code;
				x86_jump8 (code, 0);
				amd64_patch (unordered_check, code);
				amd64_inc_reg (code, ins->dreg);
				amd64_patch (jump_to_end, code);
			} else {
				amd64_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
			}
			break;
		case OP_FCGT:
		case OP_FCGT_UN: {
			/* zeroing the register at the start results in 
			 * shorter and faster code (we can also remove the widening op)
			 */
			guchar *unordered_check;
			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_reg (code, ins->sreg2, ins->sreg1);
			if (ins->opcode == OP_FCGT) {
				unordered_check = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				amd64_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
				amd64_patch (unordered_check, code);
			} else {
				amd64_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
			}
			break;
		}
		case OP_FCLT_MEMBASE:
		case OP_FCGT_MEMBASE:
		case OP_FCLT_UN_MEMBASE:
		case OP_FCGT_UN_MEMBASE:
		case OP_FCEQ_MEMBASE: {
			guchar *unordered_check, *jump_to_end;
			int x86_cond;

			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_membase (code, ins->sreg1, ins->sreg2, ins->inst_offset);

			switch (ins->opcode) {
			case OP_FCEQ_MEMBASE:
				x86_cond = X86_CC_EQ;
				break;
			case OP_FCLT_MEMBASE:
			case OP_FCLT_UN_MEMBASE:
				x86_cond = X86_CC_LT;
				break;
			case OP_FCGT_MEMBASE:
			case OP_FCGT_UN_MEMBASE:
				x86_cond = X86_CC_GT;
				break;
			default:
				g_assert_not_reached ();
			}

			unordered_check = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			amd64_set_reg (code, x86_cond, ins->dreg, FALSE);

			switch (ins->opcode) {
			case OP_FCEQ_MEMBASE:
			case OP_FCLT_MEMBASE:
			case OP_FCGT_MEMBASE:
				amd64_patch (unordered_check, code);
				break;
			case OP_FCLT_UN_MEMBASE:
			case OP_FCGT_UN_MEMBASE:
				jump_to_end = code;
				x86_jump8 (code, 0);
				amd64_patch (unordered_check, code);
				amd64_inc_reg (code, ins->dreg);
				amd64_patch (jump_to_end, code);
				break;
			default:
				break;
			}
			break;
		}
		case OP_FBEQ: {
			guchar *jump = code;
			x86_branch8 (code, X86_CC_P, 0, TRUE);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			amd64_patch (jump, code);
			break;
		}
		case OP_FBNE_UN:
			/* Branch if C013 != 100 */
			/* branch if !ZF or (PF|CF) */
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_B, FALSE);
			break;
		case OP_FBLT:
			EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
			if (ins->opcode == OP_FBGT) {
				guchar *br1;

				/* skip branch if C1=1 */
				br1 = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				/* branch if (C0 | C3) = 1 */
				EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
				amd64_patch (br1, code);
				break;
			} else {
				EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
			}
			break;
		case OP_FBGE: {
			/* Branch if C013 == 100 or 001 */
			guchar *br1;

			/* skip branch if C1=1 */
			br1 = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			/* branch if (C0 | C3) = 1 */
			EMIT_COND_BRANCH (ins, X86_CC_BE, FALSE);
			amd64_patch (br1, code);
			break;
		}
		case OP_FBGE_UN:
			/* Branch if C013 == 000 */
			EMIT_COND_BRANCH (ins, X86_CC_LE, FALSE);
			break;
		case OP_FBLE: {
			/* Branch if C013=000 or 100 */
			guchar *br1;

			/* skip branch if C1=1 */
			br1 = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			/* branch if C0=0 */
			EMIT_COND_BRANCH (ins, X86_CC_NB, FALSE);
			amd64_patch (br1, code);
			break;
		}
		case OP_FBLE_UN:
			/* Branch if C013 != 001 */
			EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_GE, FALSE);
			break;
		case OP_CKFINITE:
			/* Transfer value to the fp stack */
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 16);
			amd64_movsd_membase_reg (code, AMD64_RSP, 0, ins->sreg1);
			amd64_fld_membase (code, AMD64_RSP, 0, TRUE);

			amd64_push_reg (code, AMD64_RAX);
			amd64_fxam (code);
			amd64_fnstsw (code);
			amd64_alu_reg_imm (code, X86_AND, AMD64_RAX, 0x4100);
			amd64_alu_reg_imm (code, X86_CMP, AMD64_RAX, X86_FP_C0);
			amd64_pop_reg (code, AMD64_RAX);
			amd64_fstp (code, 0);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_EQ, FALSE, "ArithmeticException");
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 16);
			break;
		case OP_TLS_GET: {
			code = mono_amd64_emit_tls_get (code, ins->dreg, ins->inst_offset);
			break;
		}
		case OP_MEMORY_BARRIER: {
			switch (ins->backend.memory_barrier_kind) {
			case StoreLoadBarrier:
			case FullBarrier:
				/* http://blogs.sun.com/dave/resource/NHM-Pipeline-Blog-V2.txt */
				x86_prefix (code, X86_LOCK_PREFIX);
				amd64_alu_membase_imm (code, X86_ADD, AMD64_RSP, 0, 0);
				break;
			}
			break;
		}
		case OP_ATOMIC_ADD_I4:
		case OP_ATOMIC_ADD_I8: {
			int dreg = ins->dreg;
			guint32 size = (ins->opcode == OP_ATOMIC_ADD_I4) ? 4 : 8;

			if (dreg == ins->inst_basereg)
				dreg = AMD64_R11;
			
			if (dreg != ins->sreg2)
				amd64_mov_reg_reg (code, ins->dreg, ins->sreg2, size);

			x86_prefix (code, X86_LOCK_PREFIX);
			amd64_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, dreg, size);

			if (dreg != ins->dreg)
				amd64_mov_reg_reg (code, ins->dreg, dreg, size);

			break;
		}
		case OP_ATOMIC_ADD_NEW_I4:
		case OP_ATOMIC_ADD_NEW_I8: {
			int dreg = ins->dreg;
			guint32 size = (ins->opcode == OP_ATOMIC_ADD_NEW_I4) ? 4 : 8;

			if ((dreg == ins->sreg2) || (dreg == ins->inst_basereg))
				dreg = AMD64_R11;

			amd64_mov_reg_reg (code, dreg, ins->sreg2, size);
			amd64_prefix (code, X86_LOCK_PREFIX);
			amd64_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, dreg, size);
			/* dreg contains the old value, add with sreg2 value */
			amd64_alu_reg_reg_size (code, X86_ADD, dreg, ins->sreg2, size);
			
			if (ins->dreg != dreg)
				amd64_mov_reg_reg (code, ins->dreg, dreg, size);

			break;
		}
		case OP_ATOMIC_EXCHANGE_I4:
		case OP_ATOMIC_EXCHANGE_I8: {
			guchar *br[2];
			int sreg2 = ins->sreg2;
			int breg = ins->inst_basereg;
			guint32 size;
			gboolean need_push = FALSE, rdx_pushed = FALSE;

			if (ins->opcode == OP_ATOMIC_EXCHANGE_I8)
				size = 8;
			else
				size = 4;

			/* 
			 * See http://msdn.microsoft.com/en-us/magazine/cc302329.aspx for
			 * an explanation of how this works.
			 */

			/* cmpxchg uses eax as comperand, need to make sure we can use it
			 * hack to overcome limits in x86 reg allocator 
			 * (req: dreg == eax and sreg2 != eax and breg != eax) 
			 */
			g_assert (ins->dreg == AMD64_RAX);

			if (breg == AMD64_RAX && ins->sreg2 == AMD64_RAX)
				/* Highly unlikely, but possible */
				need_push = TRUE;

			/* The pushes invalidate rsp */
			if ((breg == AMD64_RAX) || need_push) {
				amd64_mov_reg_reg (code, AMD64_R11, breg, 8);
				breg = AMD64_R11;
			}

			/* We need the EAX reg for the comparand */
			if (ins->sreg2 == AMD64_RAX) {
				if (breg != AMD64_R11) {
					amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, 8);
					sreg2 = AMD64_R11;
				} else {
					g_assert (need_push);
					amd64_push_reg (code, AMD64_RDX);
					amd64_mov_reg_reg (code, AMD64_RDX, AMD64_RAX, size);
					sreg2 = AMD64_RDX;
					rdx_pushed = TRUE;
				}
			}

			amd64_mov_reg_membase (code, AMD64_RAX, breg, ins->inst_offset, size);

			br [0] = code; amd64_prefix (code, X86_LOCK_PREFIX);
			amd64_cmpxchg_membase_reg_size (code, breg, ins->inst_offset, sreg2, size);
			br [1] = code; amd64_branch8 (code, X86_CC_NE, -1, FALSE);
			amd64_patch (br [1], br [0]);

			if (rdx_pushed)
				amd64_pop_reg (code, AMD64_RDX);

			break;
		}
		case OP_ATOMIC_CAS_I4:
		case OP_ATOMIC_CAS_I8: {
			guint32 size;

			if (ins->opcode == OP_ATOMIC_CAS_I8)
				size = 8;
			else
				size = 4;

			/* 
			 * See http://msdn.microsoft.com/en-us/magazine/cc302329.aspx for
			 * an explanation of how this works.
			 */
			g_assert (ins->sreg3 == AMD64_RAX);
			g_assert (ins->sreg1 != AMD64_RAX);
			g_assert (ins->sreg1 != ins->sreg2);

			amd64_prefix (code, X86_LOCK_PREFIX);
			amd64_cmpxchg_membase_reg_size (code, ins->sreg1, ins->inst_offset, ins->sreg2, size);

			if (ins->dreg != AMD64_RAX)
				amd64_mov_reg_reg (code, ins->dreg, AMD64_RAX, size);
			break;
		}
		case OP_CARD_TABLE_WBARRIER: {
			int ptr = ins->sreg1;
			int value = ins->sreg2;
			guchar *br;
			int nursery_shift, card_table_shift;
			gpointer card_table_mask;
			size_t nursery_size;

			gpointer card_table = mono_gc_get_card_table (&card_table_shift, &card_table_mask);
			guint64 nursery_start = (guint64)mono_gc_get_nursery (&nursery_shift, &nursery_size);
			guint64 shifted_nursery_start = nursery_start >> nursery_shift;

			/*If either point to the stack we can simply avoid the WB. This happens due to
			 * optimizations revealing a stack store that was not visible when op_cardtable was emited.
			 */
			if (ins->sreg1 == AMD64_RSP || ins->sreg2 == AMD64_RSP)
				continue;

			/*
			 * We need one register we can clobber, we choose EDX and make sreg1
			 * fixed EAX to work around limitations in the local register allocator.
			 * sreg2 might get allocated to EDX, but that is not a problem since
			 * we use it before clobbering EDX.
			 */
			g_assert (ins->sreg1 == AMD64_RAX);

			/*
			 * This is the code we produce:
			 *
			 *   edx = value
			 *   edx >>= nursery_shift
			 *   cmp edx, (nursery_start >> nursery_shift)
			 *   jne done
			 *   edx = ptr
			 *   edx >>= card_table_shift
			 *   edx += cardtable
			 *   [edx] = 1
			 * done:
			 */

			if (value != AMD64_RDX)
				amd64_mov_reg_reg (code, AMD64_RDX, value, 8);
			amd64_shift_reg_imm (code, X86_SHR, AMD64_RDX, nursery_shift);
			if (shifted_nursery_start >> 31) {
				/*
				 * The value we need to compare against is 64 bits, so we need
				 * another spare register.  We use RBX, which we save and
				 * restore.
				 */
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RBX, 8);
				amd64_mov_reg_imm (code, AMD64_RBX, shifted_nursery_start);
				amd64_alu_reg_reg (code, X86_CMP, AMD64_RDX, AMD64_RBX);
				amd64_mov_reg_membase (code, AMD64_RBX, AMD64_RSP, -8, 8);
			} else {
				amd64_alu_reg_imm (code, X86_CMP, AMD64_RDX, shifted_nursery_start);
			}
			br = code; x86_branch8 (code, X86_CC_NE, -1, FALSE);
			amd64_mov_reg_reg (code, AMD64_RDX, ptr, 8);
			amd64_shift_reg_imm (code, X86_SHR, AMD64_RDX, card_table_shift);
			if (card_table_mask)
				amd64_alu_reg_imm (code, X86_AND, AMD64_RDX, (guint32)(guint64)card_table_mask);

			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_GC_CARD_TABLE_ADDR, card_table);
			amd64_alu_reg_membase (code, X86_ADD, AMD64_RDX, AMD64_RIP, 0);

			amd64_mov_membase_imm (code, AMD64_RDX, 0, 1, 1);
			x86_patch (br, code);
			break;
		}
#ifdef MONO_ARCH_SIMD_INTRINSICS
		/* TODO: Some of these IR opcodes are marked as no clobber when they indeed do. */
		case OP_ADDPS:
			amd64_sse_addps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DIVPS:
			amd64_sse_divps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MULPS:
			amd64_sse_mulps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBPS:
			amd64_sse_subps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MAXPS:
			amd64_sse_maxps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MINPS:
			amd64_sse_minps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPPS:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 7);
			amd64_sse_cmpps_reg_reg_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_ANDPS:
			amd64_sse_andps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ANDNPS:
			amd64_sse_andnps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ORPS:
			amd64_sse_orps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_XORPS:
			amd64_sse_xorps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SQRTPS:
			amd64_sse_sqrtps_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_RSQRTPS:
			amd64_sse_rsqrtps_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_RCPPS:
			amd64_sse_rcpps_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_ADDSUBPS:
			amd64_sse_addsubps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HADDPS:
			amd64_sse_haddps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HSUBPS:
			amd64_sse_hsubps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DUPPS_HIGH:
			amd64_sse_movshdup_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_DUPPS_LOW:
			amd64_sse_movsldup_reg_reg (code, ins->dreg, ins->sreg1);
			break;

		case OP_PSHUFLEW_HIGH:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			amd64_sse_pshufhw_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_PSHUFLEW_LOW:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			amd64_sse_pshuflw_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_PSHUFLED:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_SHUFPS:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0xFF);
			amd64_sse_shufps_reg_reg_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_SHUFPD:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 0x3);
			amd64_sse_shufpd_reg_reg_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;

		case OP_ADDPD:
			amd64_sse_addpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DIVPD:
			amd64_sse_divpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MULPD:
			amd64_sse_mulpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBPD:
			amd64_sse_subpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MAXPD:
			amd64_sse_maxpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MINPD:
			amd64_sse_minpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPPD:
			g_assert (ins->inst_c0 >= 0 && ins->inst_c0 <= 7);
			amd64_sse_cmppd_reg_reg_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_ANDPD:
			amd64_sse_andpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ANDNPD:
			amd64_sse_andnpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_ORPD:
			amd64_sse_orpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_XORPD:
			amd64_sse_xorpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_SQRTPD:
			amd64_sse_sqrtpd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_ADDSUBPD:
			amd64_sse_addsubpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HADDPD:
			amd64_sse_haddpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_HSUBPD:
			amd64_sse_hsubpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_DUPPD:
			amd64_sse_movddup_reg_reg (code, ins->dreg, ins->sreg1);
			break;

		case OP_EXTRACT_MASK:
			amd64_sse_pmovmskb_reg_reg (code, ins->dreg, ins->sreg1);
			break;

		case OP_PAND:
			amd64_sse_pand_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_POR:
			amd64_sse_por_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PXOR:
			amd64_sse_pxor_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB:
			amd64_sse_paddb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW:
			amd64_sse_paddw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDD:
			amd64_sse_paddd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDQ:
			amd64_sse_paddq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PSUBB:
			amd64_sse_psubb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW:
			amd64_sse_psubw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBD:
			amd64_sse_psubd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBQ:
			amd64_sse_psubq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PMAXB_UN:
			amd64_sse_pmaxub_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXW_UN:
			amd64_sse_pmaxuw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXD_UN:
			amd64_sse_pmaxud_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		
		case OP_PMAXB:
			amd64_sse_pmaxsb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXW:
			amd64_sse_pmaxsw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMAXD:
			amd64_sse_pmaxsd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PAVGB_UN:
			amd64_sse_pavgb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PAVGW_UN:
			amd64_sse_pavgw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PMINB_UN:
			amd64_sse_pminub_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMINW_UN:
			amd64_sse_pminuw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMIND_UN:
			amd64_sse_pminud_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PMINB:
			amd64_sse_pminsb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMINW:
			amd64_sse_pminsw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMIND:
			amd64_sse_pminsd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PCMPEQB:
			amd64_sse_pcmpeqb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQW:
			amd64_sse_pcmpeqw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQD:
			amd64_sse_pcmpeqd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPEQQ:
			amd64_sse_pcmpeqq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PCMPGTB:
			amd64_sse_pcmpgtb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTW:
			amd64_sse_pcmpgtw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTD:
			amd64_sse_pcmpgtd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PCMPGTQ:
			amd64_sse_pcmpgtq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PSUM_ABS_DIFF:
			amd64_sse_psadbw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_UNPACK_LOWB:
			amd64_sse_punpcklbw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWW:
			amd64_sse_punpcklwd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWD:
			amd64_sse_punpckldq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWQ:
			amd64_sse_punpcklqdq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWPS:
			amd64_sse_unpcklps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_LOWPD:
			amd64_sse_unpcklpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_UNPACK_HIGHB:
			amd64_sse_punpckhbw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHW:
			amd64_sse_punpckhwd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHD:
			amd64_sse_punpckhdq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHQ:
			amd64_sse_punpckhqdq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHPS:
			amd64_sse_unpckhps_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_UNPACK_HIGHPD:
			amd64_sse_unpckhpd_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PACKW:
			amd64_sse_packsswb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKD:
			amd64_sse_packssdw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKW_UN:
			amd64_sse_packuswb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PACKD_UN:
			amd64_sse_packusdw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB_SAT_UN:
			amd64_sse_paddusb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBB_SAT_UN:
			amd64_sse_psubusb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW_SAT_UN:
			amd64_sse_paddusw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW_SAT_UN:
			amd64_sse_psubusw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PADDB_SAT:
			amd64_sse_paddsb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBB_SAT:
			amd64_sse_psubsb_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PADDW_SAT:
			amd64_sse_paddsw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PSUBW_SAT:
			amd64_sse_psubsw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
			
		case OP_PMULW:
			amd64_sse_pmullw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULD:
			amd64_sse_pmulld_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULQ:
			amd64_sse_pmuludq_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULW_HIGH_UN:
			amd64_sse_pmulhuw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_PMULW_HIGH:
			amd64_sse_pmulhw_reg_reg (code, ins->sreg1, ins->sreg2);
			break;

		case OP_PSHRW:
			amd64_sse_psrlw_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRW_REG:
			amd64_sse_psrlw_reg_reg (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSARW:
			amd64_sse_psraw_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARW_REG:
			amd64_sse_psraw_reg_reg (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLW:
			amd64_sse_psllw_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLW_REG:
			amd64_sse_psllw_reg_reg (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHRD:
			amd64_sse_psrld_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRD_REG:
			amd64_sse_psrld_reg_reg (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSARD:
			amd64_sse_psrad_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARD_REG:
			amd64_sse_psrad_reg_reg (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHLD:
			amd64_sse_pslld_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLD_REG:
			amd64_sse_pslld_reg_reg (code, ins->dreg, ins->sreg2);
			break;

		case OP_PSHRQ:
			amd64_sse_psrlq_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHRQ_REG:
			amd64_sse_psrlq_reg_reg (code, ins->dreg, ins->sreg2);
			break;
		
		/*TODO: This is appart of the sse spec but not added
		case OP_PSARQ:
			amd64_sse_psraq_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSARQ_REG:
			amd64_sse_psraq_reg_reg (code, ins->dreg, ins->sreg2);
			break;	
		*/
	
		case OP_PSHLQ:
			amd64_sse_psllq_reg_imm (code, ins->dreg, ins->inst_imm);
			break;
		case OP_PSHLQ_REG:
			amd64_sse_psllq_reg_reg (code, ins->dreg, ins->sreg2);
			break;	
		case OP_CVTDQ2PD:
			amd64_sse_cvtdq2pd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTDQ2PS:
			amd64_sse_cvtdq2ps_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPD2DQ:
			amd64_sse_cvtpd2dq_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPD2PS:
			amd64_sse_cvtpd2ps_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPS2DQ:
			amd64_sse_cvtps2dq_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTPS2PD:
			amd64_sse_cvtps2pd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTTPD2DQ:
			amd64_sse_cvttpd2dq_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_CVTTPS2DQ:
			amd64_sse_cvttps2dq_reg_reg (code, ins->dreg, ins->sreg1);
			break;

		case OP_ICONV_TO_X:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_EXTRACT_I4:
			amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_EXTRACT_I8:
			if (ins->inst_c0) {
				amd64_movhlps_reg_reg (code, AMD64_XMM15, ins->sreg1);
				amd64_movd_reg_xreg_size (code, ins->dreg, AMD64_XMM15, 8);
			} else {
				amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 8);
			}
			break;
		case OP_EXTRACT_I1:
		case OP_EXTRACT_U1:
			amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			if (ins->inst_c0)
				amd64_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_c0 * 8);
			amd64_widen_reg (code, ins->dreg, ins->dreg, ins->opcode == OP_EXTRACT_I1, FALSE);
			break;
		case OP_EXTRACT_I2:
		case OP_EXTRACT_U2:
			/*amd64_movd_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			if (ins->inst_c0)
				amd64_shift_reg_imm_size (code, X86_SHR, ins->dreg, 16, 4);*/
			amd64_sse_pextrw_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			amd64_widen_reg_size (code, ins->dreg, ins->dreg, ins->opcode == OP_EXTRACT_I2, TRUE, 4);
			break;
		case OP_EXTRACT_R8:
			if (ins->inst_c0)
				amd64_movhlps_reg_reg (code, ins->dreg, ins->sreg1);
			else
				amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_INSERT_I2:
			amd64_sse_pinsrw_reg_reg_imm (code, ins->sreg1, ins->sreg2, ins->inst_c0);
			break;
		case OP_EXTRACTX_U2:
			amd64_sse_pextrw_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_c0);
			break;
		case OP_INSERTX_U1_SLOW:
			/*sreg1 is the extracted ireg (scratch)
			/sreg2 is the to be inserted ireg (scratch)
			/dreg is the xreg to receive the value*/

			/*clear the bits from the extracted word*/
			amd64_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_c0 & 1 ? 0x00FF : 0xFF00);
			/*shift the value to insert if needed*/
			if (ins->inst_c0 & 1)
				amd64_shift_reg_imm_size (code, X86_SHL, ins->sreg2, 8, 4);
			/*join them together*/
			amd64_alu_reg_reg (code, X86_OR, ins->sreg1, ins->sreg2);
			amd64_sse_pinsrw_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_c0 / 2);
			break;
		case OP_INSERTX_I4_SLOW:
			amd64_sse_pinsrw_reg_reg_imm (code, ins->dreg, ins->sreg2, ins->inst_c0 * 2);
			amd64_shift_reg_imm (code, X86_SHR, ins->sreg2, 16);
			amd64_sse_pinsrw_reg_reg_imm (code, ins->dreg, ins->sreg2, ins->inst_c0 * 2 + 1);
			break;
		case OP_INSERTX_I8_SLOW:
			amd64_movd_xreg_reg_size(code, AMD64_XMM15, ins->sreg2, 8);
			if (ins->inst_c0)
				amd64_movlhps_reg_reg (code, ins->dreg, AMD64_XMM15);
			else
				amd64_sse_movsd_reg_reg (code, ins->dreg, AMD64_XMM15);
			break;

		case OP_INSERTX_R4_SLOW:
			switch (ins->inst_c0) {
			case 0:
				amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->sreg2);
				break;
			case 1:
				amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(1, 0, 2, 3));
				amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->sreg2);
				amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(1, 0, 2, 3));
				break;
			case 2:
				amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(2, 1, 0, 3));
				amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->sreg2);
				amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(2, 1, 0, 3));
				break;
			case 3:
				amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(3, 1, 2, 0));
				amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->sreg2);
				amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, mono_simd_shuffle_mask(3, 1, 2, 0));
				break;
			}
			break;
		case OP_INSERTX_R8_SLOW:
			if (ins->inst_c0)
				amd64_movlhps_reg_reg (code, ins->dreg, ins->sreg2);
			else
				amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;
		case OP_STOREX_MEMBASE_REG:
		case OP_STOREX_MEMBASE:
			amd64_sse_movups_membase_reg (code, ins->dreg, ins->inst_offset, ins->sreg1);
			break;
		case OP_LOADX_MEMBASE:
			amd64_sse_movups_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_LOADX_ALIGNED_MEMBASE:
			amd64_sse_movaps_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_STOREX_ALIGNED_MEMBASE_REG:
			amd64_sse_movaps_membase_reg (code, ins->dreg, ins->inst_offset, ins->sreg1);
			break;
		case OP_STOREX_NTA_MEMBASE_REG:
			amd64_sse_movntps_reg_membase (code, ins->dreg, ins->sreg1, ins->inst_offset);
			break;
		case OP_PREFETCH_MEMBASE:
			amd64_sse_prefetch_reg_membase (code, ins->backend.arg_info, ins->sreg1, ins->inst_offset);
			break;

		case OP_XMOVE:
			/*FIXME the peephole pass should have killed this*/
			if (ins->dreg != ins->sreg1)
				amd64_sse_movaps_reg_reg (code, ins->dreg, ins->sreg1);
			break;		
		case OP_XZERO:
			amd64_sse_pxor_reg_reg (code, ins->dreg, ins->dreg);
			break;
		case OP_ICONV_TO_R8_RAW:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 4);
			amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, ins->dreg);
			break;

		case OP_FCONV_TO_R8_X:
			amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			break;

		case OP_XCONV_R8_TO_I4:
			amd64_sse_cvttsd2si_reg_xreg_size (code, ins->dreg, ins->sreg1, 4);
			switch (ins->backend.source_opcode) {
			case OP_FCONV_TO_I1:
				amd64_widen_reg (code, ins->dreg, ins->dreg, TRUE, FALSE);
				break;
			case OP_FCONV_TO_U1:
				amd64_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
				break;
			case OP_FCONV_TO_I2:
				amd64_widen_reg (code, ins->dreg, ins->dreg, TRUE, TRUE);
				break;
			case OP_FCONV_TO_U2:
				amd64_widen_reg (code, ins->dreg, ins->dreg, FALSE, TRUE);
				break;
			}			
			break;

		case OP_EXPAND_I2:
			amd64_sse_pinsrw_reg_reg_imm (code, ins->dreg, ins->sreg1, 0);
			amd64_sse_pinsrw_reg_reg_imm (code, ins->dreg, ins->sreg1, 1);
			amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_I4:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 4);
			amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_I8:
			amd64_movd_xreg_reg_size (code, ins->dreg, ins->sreg1, 8);
			amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, 0x44);
			break;
		case OP_EXPAND_R4:
			amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->dreg);
			amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_EXPAND_R8:
			amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			amd64_sse_pshufd_reg_reg_imm (code, ins->dreg, ins->dreg, 0x44);
			break;
#endif
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = code - cfg->native_code;
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = code - cfg->native_code;
			break;
		}
		case OP_NACL_GC_SAFE_POINT: {
#if defined(__native_client_codegen__)
			code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)mono_nacl_gc, TRUE);
#endif
			break;
		}
		case OP_GC_LIVENESS_DEF:
		case OP_GC_LIVENESS_USE:
		case OP_GC_PARAM_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		case OP_GC_SPILL_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			bb->spill_slot_defs = g_slist_prepend_mempool (cfg->mempool, bb->spill_slot_defs, ins);
			break;
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((code - cfg->native_code - offset) > max_len) {
#if !defined(__native_client_codegen__)
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
#endif
		}
	       
		last_ins = ins;
		last_offset = offset;
	}

	cfg->code_len = code - cfg->native_code;
}

#endif /* DISABLE_JIT */

void
mono_arch_register_lowlevel_calls (void)
{
	/* The signature doesn't matter */
	mono_register_jit_icall (mono_amd64_throw_exception, "mono_amd64_throw_exception", mono_create_icall_signature ("void"), TRUE);
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;
	gboolean compile_aot = !run_cctors;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		if (compile_aot) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_BB:
			case MONO_PATCH_INFO_LABEL:
				break;
			default:
				/* No need to patch these */
				continue;
			}
		}

		switch (patch_info->type) {
		case MONO_PATCH_INFO_NONE:
			continue;
		case MONO_PATCH_INFO_METHOD_REL:
		case MONO_PATCH_INFO_R8:
		case MONO_PATCH_INFO_R4:
			g_assert_not_reached ();
			continue;
		case MONO_PATCH_INFO_BB:
			break;
		default:
			break;
		}

		/* 
		 * Debug code to help track down problems where the target of a near call is
		 * is not valid.
		 */
		if (amd64_is_near_call (ip)) {
			gint64 disp = (guint8*)target - (guint8*)ip;

			if (!amd64_is_imm32 (disp)) {
				printf ("TYPE: %d\n", patch_info->type);
				switch (patch_info->type) {
				case MONO_PATCH_INFO_INTERNAL_METHOD:
					printf ("V: %s\n", patch_info->data.name);
					break;
				case MONO_PATCH_INFO_METHOD_JUMP:
				case MONO_PATCH_INFO_METHOD:
					printf ("V: %s\n", patch_info->data.method->name);
					break;
				default:
					break;
				}
			}
		}

		amd64_patch (ip, (gpointer)target);
	}
}

#ifndef DISABLE_JIT

static int
get_max_epilog_size (MonoCompile *cfg)
{
	int max_epilog_size = 16;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 256;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	max_epilog_size += (AMD64_NREG * 2);

	return max_epilog_size;
}

/*
 * This macro is used for testing whenever the unwinder works correctly at every point
 * where an async exception can happen.
 */
/* This will generate a SIGSEGV at the given point in the code */
#define async_exc_point(code) do { \
    if (mono_inject_async_exc_method && mono_method_desc_full_match (mono_inject_async_exc_method, cfg->method)) { \
         if (cfg->arch.async_point_count == mono_inject_async_exc_pos) \
             amd64_mov_reg_mem (code, AMD64_RAX, 0, 4); \
         cfg->arch.async_point_count ++; \
    } \
} while (0)

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *ins;
	int alloc_size, pos, i, cfa_offset, quad, max_epilog_size;
	guint8 *code;
	CallInfo *cinfo;
	MonoInst *lmf_var = cfg->arch.lmf_var;
	gboolean args_clobbered = FALSE;
	gboolean trace = FALSE;
#ifdef __native_client_codegen__
	guint alignment_check;
#endif

	cfg->code_size =  MAX (cfg->header->code_size * 4, 10240);

#if defined(__default_codegen__)
	code = cfg->native_code = g_malloc (cfg->code_size);
#elif defined(__native_client_codegen__)
	/* native_code_alloc is not 32-byte aligned, native_code is. */
	cfg->native_code_alloc = g_malloc (cfg->code_size + kNaClAlignment);

	/* Align native_code to next nearest kNaclAlignment byte. */
	cfg->native_code = (uintptr_t)cfg->native_code_alloc + kNaClAlignment;
	cfg->native_code = (uintptr_t)cfg->native_code & ~kNaClAlignmentMask;

	code = cfg->native_code;

	alignment_check = (guint)cfg->native_code & kNaClAlignmentMask;
	g_assert (alignment_check == 0);
#endif

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		trace = TRUE;

	/* Amount of stack space allocated by register saving code */
	pos = 0;

	/* Offset between RSP and the CFA */
	cfa_offset = 0;

	/* 
	 * The prolog consists of the following parts:
	 * FP present:
	 * - push rbp, mov rbp, rsp
	 * - save callee saved regs using pushes
	 * - allocate frame
	 * - save rgctx if needed
	 * - save lmf if needed
	 * FP not present:
	 * - allocate frame
	 * - save rgctx if needed
	 * - save lmf if needed
	 * - save callee saved regs using moves
	 */

	// CFA = sp + 8
	cfa_offset = 8;
	mono_emit_unwind_op_def_cfa (cfg, code, AMD64_RSP, 8);
	// IP saved at CFA - 8
	mono_emit_unwind_op_offset (cfg, code, AMD64_RIP, -cfa_offset);
	async_exc_point (code);
	mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset, SLOT_NOREF);

	if (!cfg->arch.omit_fp) {
		amd64_push_reg (code, AMD64_RBP);
		cfa_offset += 8;
		mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
		mono_emit_unwind_op_offset (cfg, code, AMD64_RBP, - cfa_offset);
		async_exc_point (code);
#ifdef HOST_WIN32
		mono_arch_unwindinfo_add_push_nonvol (&cfg->arch.unwindinfo, cfg->native_code, code, AMD64_RBP);
#endif
		/* These are handled automatically by the stack marking code */
		mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset, SLOT_NOREF);
		
		amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof(mgreg_t));
		mono_emit_unwind_op_def_cfa_reg (cfg, code, AMD64_RBP);
		async_exc_point (code);
#ifdef HOST_WIN32
		mono_arch_unwindinfo_add_set_fpreg (&cfg->arch.unwindinfo, cfg->native_code, code, AMD64_RBP);
#endif
	}

	/* Save callee saved registers */
	if (!cfg->arch.omit_fp && !method->save_lmf) {
		int offset = cfa_offset;

		for (i = 0; i < AMD64_NREG; ++i)
			if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
				amd64_push_reg (code, i);
				pos += 8; /* AMD64 push inst is always 8 bytes, no way to change it */
				offset += 8;
				mono_emit_unwind_op_offset (cfg, code, i, - offset);
				async_exc_point (code);

				/* These are handled automatically by the stack marking code */
				mini_gc_set_slot_type_from_cfa (cfg, - offset, SLOT_NOREF);
			}
	}

	/* The param area is always at offset 0 from sp */
	/* This needs to be allocated here, since it has to come after the spill area */
	if (cfg->arch.no_pushes && cfg->param_area) {
		if (cfg->arch.omit_fp)
			// FIXME:
			g_assert_not_reached ();
		cfg->stack_offset += ALIGN_TO (cfg->param_area, sizeof(mgreg_t));
	}

	if (cfg->arch.omit_fp) {
		/* 
		 * On enter, the stack is misaligned by the pushing of the return
		 * address. It is either made aligned by the pushing of %rbp, or by
		 * this.
		 */
		alloc_size = ALIGN_TO (cfg->stack_offset, 8);
		if ((alloc_size % 16) == 0) {
			alloc_size += 8;
			/* Mark the padding slot as NOREF */
			mini_gc_set_slot_type_from_cfa (cfg, -cfa_offset - sizeof (mgreg_t), SLOT_NOREF);
		}
	} else {
		alloc_size = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);
		if (cfg->stack_offset != alloc_size) {
			/* Mark the padding slot as NOREF */
			mini_gc_set_slot_type_from_fp (cfg, -alloc_size + cfg->param_area, SLOT_NOREF);
		}
		cfg->arch.sp_fp_offset = alloc_size;
		alloc_size -= pos;
	}

	cfg->arch.stack_alloc_size = alloc_size;

	/* Allocate stack frame */
	if (alloc_size) {
		/* See mono_emit_stack_alloc */
#if defined(HOST_WIN32) || defined(MONO_ARCH_SIGSEGV_ON_ALTSTACK)
		guint32 remaining_size = alloc_size;
		/*FIXME handle unbounded code expansion, we should use a loop in case of more than X interactions*/
		guint32 required_code_size = ((remaining_size / 0x1000) + 1) * 10; /*10 is the max size of amd64_alu_reg_imm + amd64_test_membase_reg*/
		guint32 offset = code - cfg->native_code;
		if (G_UNLIKELY (required_code_size >= (cfg->code_size - offset))) {
			while (required_code_size >= (cfg->code_size - offset))
				cfg->code_size *= 2;
			cfg->native_code = mono_realloc_native_code (cfg);
			code = cfg->native_code + offset;
			cfg->stat_code_reallocs++;
		}

		while (remaining_size >= 0x1000) {
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 0x1000);
 			if (cfg->arch.omit_fp) {
				cfa_offset += 0x1000;
 				mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
			}
			async_exc_point (code);
#ifdef HOST_WIN32
			if (cfg->arch.omit_fp) 
				mono_arch_unwindinfo_add_alloc_stack (&cfg->arch.unwindinfo, cfg->native_code, code, 0x1000);
#endif

			amd64_test_membase_reg (code, AMD64_RSP, 0, AMD64_RSP);
			remaining_size -= 0x1000;
		}
		if (remaining_size) {
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, remaining_size);
 			if (cfg->arch.omit_fp) {
				cfa_offset += remaining_size;
 				mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
				async_exc_point (code);
			}
#ifdef HOST_WIN32
			if (cfg->arch.omit_fp) 
				mono_arch_unwindinfo_add_alloc_stack (&cfg->arch.unwindinfo, cfg->native_code, code, remaining_size);
#endif
		}
#else
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, alloc_size);
		if (cfg->arch.omit_fp) {
			cfa_offset += alloc_size;
			mono_emit_unwind_op_def_cfa_offset (cfg, code, cfa_offset);
			async_exc_point (code);
		}
#endif
	}

	/* Stack alignment check */
#if 0
	{
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_RSP, 8);
		amd64_alu_reg_imm (code, X86_AND, AMD64_RAX, 0xf);
		amd64_alu_reg_imm (code, X86_CMP, AMD64_RAX, 0);
		x86_branch8 (code, X86_CC_EQ, 2, FALSE);
		amd64_breakpoint (code);
	}
#endif

#ifndef TARGET_WIN32
	if (mini_get_debug_options ()->init_stacks) {
		/* Fill the stack frame with a dummy value to force deterministic behavior */
	
		/* Save registers to the red zone */
		amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDI, 8);
		amd64_mov_membase_reg (code, AMD64_RSP, -16, AMD64_RCX, 8);

		amd64_mov_reg_imm (code, AMD64_RAX, 0x2a2a2a2a2a2a2a2a);
		amd64_mov_reg_imm (code, AMD64_RCX, alloc_size / 8);
		amd64_mov_reg_reg (code, AMD64_RDI, AMD64_RSP, 8);

		amd64_cld (code);
#if defined(__default_codegen__)
		amd64_prefix (code, X86_REP_PREFIX);
		amd64_stosl (code);
#elif defined(__native_client_codegen__)
		/* NaCl stos pseudo-instruction */
		amd64_codegen_pre (code);
		/* First, clear the upper 32 bits of RDI (mov %edi, %edi)  */
		amd64_mov_reg_reg (code, AMD64_RDI, AMD64_RDI, 4);
		/* Add %r15 to %rdi using lea, condition flags unaffected. */
		amd64_lea_memindex_size (code, AMD64_RDI, AMD64_R15, 0, AMD64_RDI, 0, 8);
		amd64_prefix (code, X86_REP_PREFIX);
		amd64_stosl (code);
		amd64_codegen_post (code);
#endif /* __native_client_codegen__ */

		amd64_mov_reg_membase (code, AMD64_RDI, AMD64_RSP, -8, 8);
		amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RSP, -16, 8);
	}
#endif	

	/* Save LMF */
	if (method->save_lmf) {
		code = emit_setup_lmf (cfg, code, lmf_var->inst_offset, cfa_offset);
	}

	/* Save callee saved registers */
	if (cfg->arch.omit_fp && !method->save_lmf) {
		gint32 save_area_offset = cfg->arch.reg_save_area_offset;

		/* Save caller saved registers after sp is adjusted */
		/* The registers are saved at the bottom of the frame */
		/* FIXME: Optimize this so the regs are saved at the end of the frame in increasing order */
		for (i = 0; i < AMD64_NREG; ++i)
			if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
				amd64_mov_membase_reg (code, AMD64_RSP, save_area_offset, i, 8);
				mono_emit_unwind_op_offset (cfg, code, i, - (cfa_offset - save_area_offset));

				/* These are handled automatically by the stack marking code */
				mini_gc_set_slot_type_from_cfa (cfg, - (cfa_offset - save_area_offset), SLOT_NOREF);

				save_area_offset += 8;
				async_exc_point (code);
			}
	}

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		g_assert (cfg->rgctx_var->opcode == OP_REGOFFSET &&
				(cfg->rgctx_var->inst_basereg == AMD64_RBP || cfg->rgctx_var->inst_basereg == AMD64_RSP));

		amd64_mov_membase_reg (code, cfg->rgctx_var->inst_basereg, cfg->rgctx_var->inst_offset, MONO_ARCH_RGCTX_REG, sizeof(gpointer));

		mono_add_var_location (cfg, cfg->rgctx_var, TRUE, MONO_ARCH_RGCTX_REG, 0, 0, code - cfg->native_code);
		mono_add_var_location (cfg, cfg->rgctx_var, FALSE, cfg->rgctx_var->inst_basereg, cfg->rgctx_var->inst_offset, code - cfg->native_code, 0);
	}

	/* compute max_length in order to use short forward jumps */
	max_epilog_size = get_max_epilog_size (cfg);
	if (cfg->opt & MONO_OPT_BRANCH) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *ins;
			int max_length = 0;

			if (cfg->prof_options & MONO_PROFILE_COVERAGE)
				max_length += 6;
			/* max alignment for loops */
			if ((cfg->opt & MONO_OPT_LOOP) && bb_is_loop_start (bb))
				max_length += LOOP_ALIGNMENT;
#ifdef __native_client_codegen__
			/* max alignment for native client */
			max_length += kNaClAlignment;
#endif

			MONO_BB_FOR_EACH_INS (bb, ins) {
#ifdef __native_client_codegen__
				{
					int space_in_block = kNaClAlignment -
						((max_length + cfg->code_len) & kNaClAlignmentMask);
					int max_len = ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
					if (space_in_block < max_len && max_len < kNaClAlignment) {
						max_length += space_in_block;
					}
				}
#endif  /*__native_client_codegen__*/
				max_length += ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
			}

			/* Take prolog and epilog instrumentation into account */
			if (bb == cfg->bb_entry || bb == cfg->bb_exit)
				max_length += max_epilog_size;
			
			bb->max_length = max_length;
		}
	}

	sig = mono_method_signature (method);
	pos = 0;

	cinfo = cfg->arch.cinfo;

	if (sig->ret->type != MONO_TYPE_VOID) {
		/* Save volatile arguments to the stack */
		if (cfg->vret_addr && (cfg->vret_addr->opcode != OP_REGVAR))
			amd64_mov_membase_reg (code, cfg->vret_addr->inst_basereg, cfg->vret_addr->inst_offset, cinfo->ret.reg, 8);
	}

	/* Keep this in sync with emit_load_volatile_arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;
		MonoType *arg_type;

		ins = cfg->args [i];

		if ((ins->flags & MONO_INST_IS_DEAD) && !trace)
			/* Unused arguments */
			continue;

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		stack_offset = ainfo->offset + ARGS_OFFSET;

		if (cfg->globalra) {
			/* All the other moves are done by the register allocator */
			switch (ainfo->storage) {
 			case ArgInFloatSSEReg:
				amd64_sse_cvtss2sd_reg_reg (code, ainfo->reg, ainfo->reg);
				break;
			case ArgValuetypeInReg:
				for (quad = 0; quad < 2; quad ++) {
					switch (ainfo->pair_storage [quad]) {
					case ArgInIReg:
						amd64_mov_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof(mgreg_t)), ainfo->pair_regs [quad], sizeof(mgreg_t));
						break;
					case ArgInFloatSSEReg:
						amd64_movss_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof(mgreg_t)), ainfo->pair_regs [quad]);
						break;
					case ArgInDoubleSSEReg:
						amd64_movsd_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof(mgreg_t)), ainfo->pair_regs [quad]);
						break;
					case ArgNone:
						break;
					default:
						g_assert_not_reached ();
					}
				}
				break;
			default:
				break;
			}

			continue;
		}

		/* Save volatile arguments to the stack */
		if (ins->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg: {
				guint32 size = 8;

				/* FIXME: I1 etc */
				/*
				if (stack_offset & 0x1)
					size = 1;
				else if (stack_offset & 0x2)
					size = 2;
				else if (stack_offset & 0x4)
					size = 4;
				else
					size = 8;
				*/
				amd64_mov_membase_reg (code, ins->inst_basereg, ins->inst_offset, ainfo->reg, size);

				/*
				 * Save the original location of 'this',
				 * get_generic_info_from_stack_frame () needs this to properly look up
				 * the argument value during the handling of async exceptions.
				 */
				if (ins == cfg->args [0]) {
					mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
					mono_add_var_location (cfg, ins, FALSE, ins->inst_basereg, ins->inst_offset, code - cfg->native_code, 0);
				}
				break;
			}
			case ArgInFloatSSEReg:
				amd64_movss_membase_reg (code, ins->inst_basereg, ins->inst_offset, ainfo->reg);
				break;
			case ArgInDoubleSSEReg:
				amd64_movsd_membase_reg (code, ins->inst_basereg, ins->inst_offset, ainfo->reg);
				break;
			case ArgValuetypeInReg:
				for (quad = 0; quad < 2; quad ++) {
					switch (ainfo->pair_storage [quad]) {
					case ArgInIReg:
						amd64_mov_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof(mgreg_t)), ainfo->pair_regs [quad], sizeof(mgreg_t));
						break;
					case ArgInFloatSSEReg:
						amd64_movss_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof(mgreg_t)), ainfo->pair_regs [quad]);
						break;
					case ArgInDoubleSSEReg:
						amd64_movsd_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof(mgreg_t)), ainfo->pair_regs [quad]);
						break;
					case ArgNone:
						break;
					default:
						g_assert_not_reached ();
					}
				}
				break;
			case ArgValuetypeAddrInIReg:
				if (ainfo->pair_storage [0] == ArgInIReg)
					amd64_mov_membase_reg (code, ins->inst_left->inst_basereg, ins->inst_left->inst_offset, ainfo->pair_regs [0],  sizeof (gpointer));
				break;
			default:
				break;
			}
		} else {
			/* Argument allocated to (non-volatile) register */
			switch (ainfo->storage) {
			case ArgInIReg:
				amd64_mov_reg_reg (code, ins->dreg, ainfo->reg, 8);
				break;
			case ArgOnStack:
				amd64_mov_reg_membase (code, ins->dreg, AMD64_RBP, ARGS_OFFSET + ainfo->offset, 8);
				break;
			default:
				g_assert_not_reached ();
			}

			if (ins == cfg->args [0]) {
				mono_add_var_location (cfg, ins, TRUE, ainfo->reg, 0, 0, code - cfg->native_code);
				mono_add_var_location (cfg, ins, TRUE, ins->dreg, 0, code - cfg->native_code, 0);
			}
		}
	}

	if (method->save_lmf) {
		code = emit_save_lmf (cfg, code, lmf_var->inst_offset, &args_clobbered);
	}

	if (trace) {
		args_clobbered = TRUE;
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);
	}

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		args_clobbered = TRUE;

	/*
	 * Optimize the common case of the first bblock making a call with the same
	 * arguments as the method. This works because the arguments are still in their
	 * original argument registers.
	 * FIXME: Generalize this
	 */
	if (!args_clobbered) {
		MonoBasicBlock *first_bb = cfg->bb_entry;
		MonoInst *next;

		next = mono_bb_first_ins (first_bb);
		if (!next && first_bb->next_bb) {
			first_bb = first_bb->next_bb;
			next = mono_bb_first_ins (first_bb);
		}

		if (first_bb->in_count > 1)
			next = NULL;

		for (i = 0; next && i < sig->param_count + sig->hasthis; ++i) {
			ArgInfo *ainfo = cinfo->args + i;
			gboolean match = FALSE;
			
			ins = cfg->args [i];
			if (ins->opcode != OP_REGVAR) {
				switch (ainfo->storage) {
				case ArgInIReg: {
					if (((next->opcode == OP_LOAD_MEMBASE) || (next->opcode == OP_LOADI4_MEMBASE)) && next->inst_basereg == ins->inst_basereg && next->inst_offset == ins->inst_offset) {
						if (next->dreg == ainfo->reg) {
							NULLIFY_INS (next);
							match = TRUE;
						} else {
							next->opcode = OP_MOVE;
							next->sreg1 = ainfo->reg;
							/* Only continue if the instruction doesn't change argument regs */
							if (next->dreg == ainfo->reg || next->dreg == AMD64_RAX)
								match = TRUE;
						}
					}
					break;
				}
				default:
					break;
				}
			} else {
				/* Argument allocated to (non-volatile) register */
				switch (ainfo->storage) {
				case ArgInIReg:
					if (next->opcode == OP_MOVE && next->sreg1 == ins->dreg && next->dreg == ainfo->reg) {
						NULLIFY_INS (next);
						match = TRUE;
					}
					break;
				default:
					break;
				}
			}

			if (match) {
				next = next->next;
				//next = mono_inst_list_next (&next->node, &first_bb->ins_list);
				if (!next)
					break;
			}
		}
	}

	if (cfg->gen_seq_points) {
		MonoInst *info_var = cfg->arch.seq_point_info_var;

		/* Initialize seq_point_info_var */
		if (cfg->compile_aot) {
			/* Initialize the variable from a GOT slot */
			/* Same as OP_AOTCONST */
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_SEQ_POINT_INFO, cfg->method);
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, sizeof(gpointer));
			g_assert (info_var->opcode == OP_REGOFFSET);
			amd64_mov_membase_reg (code, info_var->inst_basereg, info_var->inst_offset, AMD64_R11, 8);
		}

		/* Initialize ss_trigger_page_var */
		ins = cfg->arch.ss_trigger_page_var;

		g_assert (ins->opcode == OP_REGOFFSET);

		if (cfg->compile_aot) {
			amd64_mov_reg_membase (code, AMD64_R11, info_var->inst_basereg, info_var->inst_offset, 8);
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, G_STRUCT_OFFSET (SeqPointInfo, ss_trigger_page), 8);
		} else {
			amd64_mov_reg_imm (code, AMD64_R11, (guint64)ss_trigger_page);
		}
		amd64_mov_membase_reg (code, ins->inst_basereg, ins->inst_offset, AMD64_R11, 8);
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int quad, pos, i;
	guint8 *code;
	int max_epilog_size;
	CallInfo *cinfo;
	gint32 lmf_offset = cfg->arch.lmf_var ? ((MonoInst*)cfg->arch.lmf_var)->inst_offset : -1;
	
	max_epilog_size = get_max_epilog_size (cfg);

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = mono_realloc_native_code (cfg);
		cfg->stat_code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);

	/* the code restoring the registers must be kept in sync with OP_JMP */
	pos = 0;
	
	if (method->save_lmf) {
		/* check if we need to restore protection of the stack after a stack overflow */
		if (mono_get_jit_tls_offset () != -1) {
			guint8 *patch;
			code = mono_amd64_emit_tls_get (code, AMD64_RCX, mono_get_jit_tls_offset ());
			/* we load the value in a separate instruction: this mechanism may be
			 * used later as a safer way to do thread interruption
			 */
			amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RCX, G_STRUCT_OFFSET (MonoJitTlsData, restore_stack_prot), 8);
			x86_alu_reg_imm (code, X86_CMP, X86_ECX, 0);
			patch = code;
			x86_branch8 (code, X86_CC_Z, 0, FALSE);
			/* note that the call trampoline will preserve eax/edx */
			x86_call_reg (code, X86_ECX);
			x86_patch (patch, code);
		} else {
			/* FIXME: maybe save the jit tls in the prolog */
		}

		code = emit_restore_lmf (cfg, code, lmf_offset);

		/* Restore caller saved regs */
		if (cfg->used_int_regs & (1 << AMD64_RBP)) {
			amd64_mov_reg_membase (code, AMD64_RBP, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbp), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_RBX)) {
			amd64_mov_reg_membase (code, AMD64_RBX, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbx), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R12)) {
			amd64_mov_reg_membase (code, AMD64_R12, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r12), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R13)) {
			amd64_mov_reg_membase (code, AMD64_R13, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r13), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R14)) {
			amd64_mov_reg_membase (code, AMD64_R14, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r14), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R15)) {
#if defined(__default_codegen__)
			amd64_mov_reg_membase (code, AMD64_R15, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r15), 8);
#elif defined(__native_client_codegen__)
			g_assert_not_reached();
#endif
		}
#ifdef HOST_WIN32
		if (cfg->used_int_regs & (1 << AMD64_RDI)) {
			amd64_mov_reg_membase (code, AMD64_RDI, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rdi), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_RSI)) {
			amd64_mov_reg_membase (code, AMD64_RSI, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsi), 8);
		}
#endif
	} else {

		if (cfg->arch.omit_fp) {
			gint32 save_area_offset = cfg->arch.reg_save_area_offset;

			for (i = 0; i < AMD64_NREG; ++i)
				if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
					amd64_mov_reg_membase (code, i, AMD64_RSP, save_area_offset, 8);
					save_area_offset += 8;
				}
		}
		else {
			for (i = 0; i < AMD64_NREG; ++i)
				if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i)))
					pos -= sizeof(mgreg_t);

			if (pos) {
				if (pos == - sizeof(mgreg_t)) {
					/* Only one register, so avoid lea */
					for (i = AMD64_NREG - 1; i > 0; --i)
						if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
							amd64_mov_reg_membase (code, i, AMD64_RBP, pos, 8);
						}
				}
				else {
					amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, pos);

					/* Pop registers in reverse order */
					for (i = AMD64_NREG - 1; i > 0; --i)
						if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
							amd64_pop_reg (code, i);
						}
				}
			}
		}
	}

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	if (cinfo->ret.storage == ArgValuetypeInReg) {
		ArgInfo *ainfo = &cinfo->ret;
		MonoInst *inst = cfg->ret;

		for (quad = 0; quad < 2; quad ++) {
			switch (ainfo->pair_storage [quad]) {
			case ArgInIReg:
				amd64_mov_reg_membase (code, ainfo->pair_regs [quad], inst->inst_basereg, inst->inst_offset + (quad * sizeof(mgreg_t)), sizeof(mgreg_t));
				break;
			case ArgInFloatSSEReg:
				amd64_movss_reg_membase (code, ainfo->pair_regs [quad], inst->inst_basereg, inst->inst_offset + (quad * sizeof(mgreg_t)));
				break;
			case ArgInDoubleSSEReg:
				amd64_movsd_reg_membase (code, ainfo->pair_regs [quad], inst->inst_basereg, inst->inst_offset + (quad * sizeof(mgreg_t)));
				break;
			case ArgNone:
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	if (cfg->arch.omit_fp) {
		if (cfg->arch.stack_alloc_size)
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, cfg->arch.stack_alloc_size);
	} else {
		amd64_leave (code);
	}
	async_exc_point (code);
	amd64_ret (code);

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int nthrows, i;
	guint8 *code;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];
	guint32 code_size = 0;

	/* Compute needed space */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			code_size += 40;
		if (patch_info->type == MONO_PATCH_INFO_R8)
			code_size += 8 + 15; /* sizeof (double) + alignment */
		if (patch_info->type == MONO_PATCH_INFO_R4)
			code_size += 4 + 15; /* sizeof (float) + alignment */
		if (patch_info->type == MONO_PATCH_INFO_GC_CARD_TABLE_ADDR)
			code_size += 8 + 7; /*sizeof (void*) + alignment */
	}

#ifdef __native_client_codegen__
	/* Give us extra room on Native Client.  This could be   */
	/* more carefully calculated, but bundle alignment makes */
	/* it much trickier, so *2 like other places is good.    */
	code_size *= 2;
#endif

	while (cfg->code_len + code_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = mono_realloc_native_code (cfg);
		cfg->stat_code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	/* add code to raise exceptions */
	nthrows = 0;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			guint8 *buf, *buf2;
			guint32 throw_ip;

			amd64_patch (patch_info->ip.i + cfg->native_code, code);

			exc_class = mono_class_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			g_assert (exc_class);
			throw_ip = patch_info->ip.i;

			//x86_breakpoint (code);
			/* Find a throw sequence for the same exception class */
			for (i = 0; i < nthrows; ++i)
				if (exc_classes [i] == exc_class)
					break;
			if (i < nthrows) {
				amd64_mov_reg_imm (code, AMD64_ARG_REG2, (exc_throw_end [i] - cfg->native_code) - throw_ip);
				x86_jump_code (code, exc_throw_start [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
			}
			else {
				buf = code;
				amd64_mov_reg_imm_size (code, AMD64_ARG_REG2, 0xf0f0f0f0, 4);
				buf2 = code;

				if (nthrows < 16) {
					exc_classes [nthrows] = exc_class;
					exc_throw_start [nthrows] = code;
				}
				amd64_mov_reg_imm (code, AMD64_ARG_REG1, exc_class->type_token - MONO_TOKEN_TYPE_DEF);

				patch_info->type = MONO_PATCH_INFO_NONE;

				code = emit_call_body (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, "mono_arch_throw_corlib_exception");

				amd64_mov_reg_imm (buf, AMD64_ARG_REG2, (code - cfg->native_code) - throw_ip);
				while (buf < buf2)
					x86_nop (buf);

				if (nthrows < 16) {
					exc_throw_end [nthrows] = code;
					nthrows ++;
				}
			}
			break;
		}
		default:
			/* do nothing */
			break;
		}
		g_assert(code < cfg->native_code + cfg->code_size);
	}

	/* Handle relocations with RIP relative addressing */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		gboolean remove = FALSE;
		guint8 *orig_code = code;

		switch (patch_info->type) {
		case MONO_PATCH_INFO_R8:
		case MONO_PATCH_INFO_R4: {
			guint8 *pos, *patch_pos;
			guint32 target_pos;

			/* The SSE opcodes require a 16 byte alignment */
#if defined(__default_codegen__)
			code = (guint8*)ALIGN_TO (code, 16);
#elif defined(__native_client_codegen__)
			{
				/* Pad this out with HLT instructions  */
				/* or we can get garbage bytes emitted */
				/* which will fail validation          */
				guint8 *aligned_code;
				/* extra align to make room for  */
				/* mov/push below 		       */
				int extra_align = patch_info->type == MONO_PATCH_INFO_R8 ? 2 : 1;
				aligned_code = (guint8*)ALIGN_TO (code + extra_align, 16);
				/* The technique of hiding data in an  */
				/* instruction has a problem here: we  */
				/* need the data aligned to a 16-byte  */
				/* boundary but the instruction cannot */
				/* cross the bundle boundary. so only  */
				/* odd multiples of 16 can be used     */
				if ((intptr_t)aligned_code % kNaClAlignment == 0) {
					aligned_code += 16;
				}
				while (code < aligned_code) {
					*(code++) = 0xf4; /* hlt */
				}
			}	
#endif

			pos = cfg->native_code + patch_info->ip.i;
			if (IS_REX (pos [1])) {
				patch_pos = pos + 5;
				target_pos = code - pos - 9;
			}
			else {
				patch_pos = pos + 4;
				target_pos = code - pos - 8;
			}

			if (patch_info->type == MONO_PATCH_INFO_R8) {
#ifdef __native_client_codegen__
				/* Hide 64-bit data in a         */
				/* "mov imm64, r11" instruction. */
				/* write it before the start of  */
				/* the data*/
				*(code-2) = 0x49; /* prefix      */
				*(code-1) = 0xbb; /* mov X, %r11 */
#endif
				*(double*)code = *(double*)patch_info->data.target;
				code += sizeof (double);
			} else {
#ifdef __native_client_codegen__
				/* Hide 32-bit data in a        */
				/* "push imm32" instruction.    */
				*(code-1) = 0x68; /* push */
#endif
				*(float*)code = *(float*)patch_info->data.target;
				code += sizeof (float);
			}

			*(guint32*)(patch_pos) = target_pos;

			remove = TRUE;
			break;
		}
		case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR: {
			guint8 *pos;

			if (cfg->compile_aot)
				continue;

			/*loading is faster against aligned addresses.*/
			code = (guint8*)ALIGN_TO (code, 8);
			memset (orig_code, 0, code - orig_code);

			pos = cfg->native_code + patch_info->ip.i;

			/*alu_op [rex] modr/m imm32 - 7 or 8 bytes */
			if (IS_REX (pos [1]))
				*(guint32*)(pos + 4) = (guint8*)code - pos - 8;
			else
				*(guint32*)(pos + 3) = (guint8*)code - pos - 7;

			*(gpointer*)code = (gpointer)patch_info->data.target;
			code += sizeof (gpointer);

			remove = TRUE;
			break;
		}
		default:
			break;
		}

		if (remove) {
			if (patch_info == cfg->patch_info)
				cfg->patch_info = patch_info->next;
			else {
				MonoJumpInfo *tmp;

				for (tmp = cfg->patch_info; tmp->next != patch_info; tmp = tmp->next)
					;
				tmp->next = patch_info->next;
			}
		}
		g_assert (code < cfg->native_code + cfg->code_size);
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

#endif /* DISABLE_JIT */

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
	CallInfo *cinfo = NULL;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int i, n, stack_area = 0;

	/* Keep this in sync with mono_arch_get_argument_info */

	if (enable_arguments) {
		/* Allocate a new area on the stack and save arguments there */
		sig = mono_method_signature (cfg->method);

		cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);

		n = sig->param_count + sig->hasthis;

		stack_area = ALIGN_TO (n * 8, 16);

		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, stack_area);

		for (i = 0; i < n; ++i) {
			inst = cfg->args [i];

			if (inst->opcode == OP_REGVAR)
				amd64_mov_membase_reg (code, AMD64_RSP, (i * 8), inst->dreg, 8);
			else {
				amd64_mov_reg_membase (code, AMD64_R11, inst->inst_basereg, inst->inst_offset, 8);
				amd64_mov_membase_reg (code, AMD64_RSP, (i * 8), AMD64_R11, 8);
			}
		}
	}

	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_METHODCONST, cfg->method);
	amd64_set_reg_template (code, AMD64_ARG_REG1);
	amd64_mov_reg_reg (code, AMD64_ARG_REG2, AMD64_RSP, 8);
	code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func, TRUE);

	if (enable_arguments)
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, stack_area);

	return code;
}

enum {
	SAVE_NONE,
	SAVE_STRUCT,
	SAVE_EAX,
	SAVE_EAX_EDX,
	SAVE_XMM
};

void*
mono_arch_instrument_epilog_full (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers)
{
	guchar *code = p;
	int save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;
	MonoType *ret_type = mini_type_get_underlying_type (NULL, mono_method_signature (method)->ret);
	int i;
	
	switch (ret_type->type) {
	case MONO_TYPE_VOID:
		/* special case string .ctor icall */
		if (strcmp (".ctor", method->name) && method->klass == mono_defaults.string_class)
			save_mode = SAVE_EAX;
		else
			save_mode = SAVE_NONE;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		save_mode = SAVE_EAX;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		save_mode = SAVE_XMM;
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ret_type)) {
			save_mode = SAVE_EAX;
			break;
		}
		/* Fall through */
	case MONO_TYPE_VALUETYPE:
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_EAX;
		break;
	}

	/* Save the result and copy it into the proper argument register */
	switch (save_mode) {
	case SAVE_EAX:
		amd64_push_reg (code, AMD64_RAX);
		/* Align stack */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
		if (enable_arguments)
			amd64_mov_reg_reg (code, AMD64_ARG_REG2, AMD64_RAX, 8);
		break;
	case SAVE_STRUCT:
		/* FIXME: */
		if (enable_arguments)
			amd64_mov_reg_imm (code, AMD64_ARG_REG2, 0);
		break;
	case SAVE_XMM:
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
		amd64_movsd_membase_reg (code, AMD64_RSP, 0, AMD64_XMM0);
		/* Align stack */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
		/* 
		 * The result is already in the proper argument register so no copying
		 * needed.
		 */
		break;
	case SAVE_NONE:
		break;
	default:
		g_assert_not_reached ();
	}

	/* Set %al since this is a varargs call */
	if (save_mode == SAVE_XMM)
		amd64_mov_reg_imm (code, AMD64_RAX, 1);
	else
		amd64_mov_reg_imm (code, AMD64_RAX, 0);

	if (preserve_argument_registers) {
		for (i = 0; i < PARAM_REGS; ++i)
			amd64_push_reg (code, param_regs [i]);
	}

	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_METHODCONST, method);
	amd64_set_reg_template (code, AMD64_ARG_REG1);
	code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func, TRUE);

	if (preserve_argument_registers) {
		for (i = PARAM_REGS - 1; i >= 0; --i)
			amd64_pop_reg (code, param_regs [i]);
	}

	/* Restore result */
	switch (save_mode) {
	case SAVE_EAX:
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
		amd64_pop_reg (code, AMD64_RAX);
		break;
	case SAVE_STRUCT:
		/* FIXME: */
		break;
	case SAVE_XMM:
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
		amd64_movsd_reg_membase (code, AMD64_XMM0, AMD64_RSP, 0);
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
		break;
	case SAVE_NONE:
		break;
	default:
		g_assert_not_reached ();
	}

	return code;
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	/* Not needed */
}

void
mono_arch_flush_register_windows (void)
{
}

gboolean 
mono_arch_is_inst_imm (gint64 imm)
{
	return amd64_is_imm32 (imm);
}

/*
 * Determine whenever the trap whose info is in SIGINFO is caused by
 * integer overflow.
 */
gboolean
mono_arch_is_int_overflow (void *sigctx, void *info)
{
	MonoContext ctx;
	guint8* rip;
	int reg;
	gint64 value;

	mono_arch_sigctx_to_monoctx (sigctx, &ctx);

	rip = (guint8*)ctx.rip;

	if (IS_REX (rip [0])) {
		reg = amd64_rex_b (rip [0]);
		rip ++;
	}
	else
		reg = 0;

	if ((rip [0] == 0xf7) && (x86_modrm_mod (rip [1]) == 0x3) && (x86_modrm_reg (rip [1]) == 0x7)) {
		/* idiv REG */
		reg += x86_modrm_rm (rip [1]);

		switch (reg) {
		case AMD64_RAX:
			value = ctx.rax;
			break;
		case AMD64_RBX:
			value = ctx.rbx;
			break;
		case AMD64_RCX:
			value = ctx.rcx;
			break;
		case AMD64_RDX:
			value = ctx.rdx;
			break;
		case AMD64_RBP:
			value = ctx.rbp;
			break;
		case AMD64_RSP:
			value = ctx.rsp;
			break;
		case AMD64_RSI:
			value = ctx.rsi;
			break;
		case AMD64_RDI:
			value = ctx.rdi;
			break;
		case AMD64_R12:
			value = ctx.r12;
			break;
		case AMD64_R13:
			value = ctx.r13;
			break;
		case AMD64_R14:
			value = ctx.r14;
			break;
		case AMD64_R15:
			value = ctx.r15;
			break;
		default:
			g_assert_not_reached ();
			reg = -1;
		}			

		if (value == -1)
			return TRUE;
	}

	return FALSE;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 3;
}

/**
 * mono_breakpoint_clean_code:
 *
 * Copy @size bytes from @code - @offset to the buffer @buf. If the debugger inserted software
 * breakpoints in the original code, they are removed in the copy.
 *
 * Returns TRUE if no sw breakpoint was present.
 */
gboolean
mono_breakpoint_clean_code (guint8 *method_start, guint8 *code, int offset, guint8 *buf, int size)
{
	int i;
	gboolean can_write = TRUE;
	/*
	 * If method_start is non-NULL we need to perform bound checks, since we access memory
	 * at code - offset we could go before the start of the method and end up in a different
	 * page of memory that is not mapped or read incorrect data anyway. We zero-fill the bytes
	 * instead.
	 */
	if (!method_start || code - offset >= method_start) {
		memcpy (buf, code - offset, size);
	} else {
		int diff = code - method_start;
		memset (buf, 0, size);
		memcpy (buf + offset - diff, method_start, diff + size - offset);
	}
	code -= offset;
	for (i = 0; i < MONO_BREAKPOINT_ARRAY_SIZE; ++i) {
		int idx = mono_breakpoint_info_index [i];
		guint8 *ptr;
		if (idx < 1)
			continue;
		ptr = mono_breakpoint_info [idx].address;
		if (ptr >= code && ptr < code + size) {
			guint8 saved_byte = mono_breakpoint_info [idx].saved_byte;
			can_write = FALSE;
			/*g_print ("patching %p with 0x%02x (was: 0x%02x)\n", ptr, saved_byte, buf [ptr - code]);*/
			buf [ptr - code] = saved_byte;
		}
	}
	return can_write;
}

#if defined(__native_client_codegen__)
/* For membase calls, we want the base register. for Native Client,  */
/* all indirect calls have the following sequence with the given sizes: */
/* mov %eXX,%eXX				[2-3]	*/
/* mov disp(%r15,%rXX,scale),%r11d		[4-8]	*/
/* and $0xffffffffffffffe0,%r11d		[4]	*/
/* add %r15,%r11				[3]	*/
/* callq *%r11					[3]	*/


/* Determine if code points to a NaCl call-through-register sequence, */
/* (i.e., the last 3 instructions listed above) */
int
is_nacl_call_reg_sequence(guint8* code)
{
	const char *sequence = "\x41\x83\xe3\xe0" /* and */
			       "\x4d\x03\xdf"     /* add */
			       "\x41\xff\xd3";   /* call */
	return memcmp(code, sequence, 10) == 0;
}

/* Determine if code points to the first opcode of the mov membase component */
/* of an indirect call sequence (i.e. the first 2 instructions listed above) */
/* (there could be a REX prefix before the opcode but it is ignored) */
static int
is_nacl_indirect_call_membase_sequence(guint8* code)
{
	       /* Check for mov opcode, reg-reg addressing mode (mod = 3), */
	return code[0] == 0x8b && amd64_modrm_mod(code[1]) == 3 &&
	       /* and that src reg = dest reg */
	       amd64_modrm_reg(code[1]) == amd64_modrm_rm(code[1]) &&
	       /* Check that next inst is mov, uses SIB byte (rm = 4), */
	       IS_REX(code[2]) &&
	       code[3] == 0x8b && amd64_modrm_rm(code[4]) == 4 &&
	       /* and has dst of r11 and base of r15 */
	       (amd64_modrm_reg(code[4]) + amd64_rex_r(code[2])) == AMD64_R11 &&
	       (amd64_sib_base(code[5]) + amd64_rex_b(code[2])) == AMD64_R15;
}
#endif /* __native_client_codegen__ */

int
mono_arch_get_this_arg_reg (guint8 *code)
{
	return AMD64_ARG_REG1;
}

gpointer
mono_arch_get_this_arg_from_call (mgreg_t *regs, guint8 *code)
{
	return (gpointer)regs [mono_arch_get_this_arg_reg (code)];
}

#define MAX_ARCH_DELEGATE_PARAMS 10

static gpointer
get_delegate_invoke_impl (gboolean has_target, guint32 param_count, guint32 *code_len)
{
	guint8 *code, *start;
	int i;

	if (has_target) {
		start = code = mono_global_codeman_reserve (64);

		/* Replace the this argument with the target */
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
		amd64_mov_reg_membase (code, AMD64_ARG_REG1, AMD64_RAX, G_STRUCT_OFFSET (MonoDelegate, target), 8);
		amd64_jump_membase (code, AMD64_RAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));

		g_assert ((code - start) < 64);
	} else {
		start = code = mono_global_codeman_reserve (64);

		if (param_count == 0) {
			amd64_jump_membase (code, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		} else {
			/* We have to shift the arguments left */
			amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
			for (i = 0; i < param_count; ++i) {
#ifdef HOST_WIN32
				if (i < 3)
					amd64_mov_reg_reg (code, param_regs [i], param_regs [i + 1], 8);
				else
					amd64_mov_reg_membase (code, param_regs [i], AMD64_RSP, 0x28, 8);
#else
				amd64_mov_reg_reg (code, param_regs [i], param_regs [i + 1], 8);
#endif
			}

			amd64_jump_membase (code, AMD64_RAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		}
		g_assert ((code - start) < 64);
	}

	nacl_global_codeman_validate(&start, 64, &code);

	mono_debug_add_delegate_trampoline (start, code - start);

	if (code_len)
		*code_len = code - start;


	if (mono_jit_map_is_enabled ()) {
		char *buff;
		if (has_target)
			buff = (char*)"delegate_invoke_has_target";
		else
			buff = g_strdup_printf ("delegate_invoke_no_target_%d", param_count);
		mono_emit_jit_tramp (start, code - start, buff);
		if (!has_target)
			g_free (buff);
	}

	return start;
}

/*
 * mono_arch_get_delegate_invoke_impls:
 *
 *   Return a list of MonoTrampInfo structures for the delegate invoke impl
 * trampolines.
 */
GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	guint8 *code;
	guint32 code_len;
	int i;

	code = get_delegate_invoke_impl (TRUE, 0, &code_len);
	res = g_slist_prepend (res, mono_tramp_info_create (g_strdup ("delegate_invoke_impl_has_target"), code, code_len, NULL, NULL));

	for (i = 0; i < MAX_ARCH_DELEGATE_PARAMS; ++i) {
		code = get_delegate_invoke_impl (FALSE, i, &code_len);
		res = g_slist_prepend (res, mono_tramp_info_create (g_strdup_printf ("delegate_invoke_impl_target_%d", i), code, code_len, NULL, NULL));
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;
	int i;

	if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
		return NULL;

	/* FIXME: Support more cases */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;

		if (cached)
			return cached;

		if (mono_aot_only)
			start = mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		else
			start = get_delegate_invoke_impl (TRUE, 0, NULL);

		mono_memory_barrier ();

		cached = start;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;
		if (sig->param_count > 4)
			return NULL;

		code = cache [sig->param_count];
		if (code)
			return code;

		if (mono_aot_only) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			start = get_delegate_invoke_impl (FALSE, sig->param_count, NULL);
		}

		mono_memory_barrier ();

		cache [sig->param_count] = start;
	}

	return start;
}
void
mono_arch_finish_init (void)
{
#ifdef HOST_WIN32
	/* 
	 * We need to init this multiple times, since when we are first called, the key might not
	 * be initialized yet.
	 */
	appdomain_tls_offset = mono_domain_get_tls_key ();
	lmf_tls_offset = mono_get_jit_tls_key ();
	lmf_addr_tls_offset = mono_get_jit_tls_key ();

	/* Only 64 tls entries can be accessed using inline code */
	if (appdomain_tls_offset >= 64)
		appdomain_tls_offset = -1;
	if (lmf_tls_offset >= 64)
		lmf_tls_offset = -1;
	if (lmf_addr_tls_offset >= 64)
		lmf_addr_tls_offset = -1;
#else
#ifdef MONO_XEN_OPT
	optimize_for_xen = access ("/proc/xen", F_OK) == 0;
#endif
	appdomain_tls_offset = mono_domain_get_tls_offset ();
 	lmf_tls_offset = mono_get_lmf_tls_offset ();
	lmf_addr_tls_offset = mono_get_lmf_addr_tls_offset ();
#endif
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

#ifdef MONO_ARCH_HAVE_IMT

#if defined(__default_codegen__)
#define CMP_SIZE (6 + 1)
#define CMP_REG_REG_SIZE (4 + 1)
#define BR_SMALL_SIZE 2
#define BR_LARGE_SIZE 6
#define MOV_REG_IMM_SIZE 10
#define MOV_REG_IMM_32BIT_SIZE 6
#define JUMP_REG_SIZE (2 + 1)
#elif defined(__native_client_codegen__)
/* NaCl N-byte instructions can be padded up to N-1 bytes */
#define CMP_SIZE ((6 + 1) * 2 - 1)
#define CMP_REG_REG_SIZE ((4 + 1) * 2 - 1)
#define BR_SMALL_SIZE (2 * 2 - 1)
#define BR_LARGE_SIZE (6 * 2 - 1)
#define MOV_REG_IMM_SIZE (10 * 2 - 1)
#define MOV_REG_IMM_32BIT_SIZE (6 * 2 - 1)
/* Jump reg for NaCl adds a mask (+4) and add (+3) */
#define JUMP_REG_SIZE ((2 + 1 + 4 + 3) * 2 - 1)
/* Jump membase's size is large and unpredictable    */
/* in native client, just pad it out a whole bundle. */
#define JUMP_MEMBASE_SIZE (kNaClAlignment)
#endif

static int
imt_branch_distance (MonoIMTCheckItem **imt_entries, int start, int target)
{
	int i, distance = 0;
	for (i = start; i < target; ++i)
		distance += imt_entries [i]->chunk_size;
	return distance;
}

/*
 * LOCKING: called with the domain lock held
 */
gpointer
mono_arch_build_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count,
	gpointer fail_tramp)
{
	int i;
	int size = 0;
	guint8 *code, *start;
	gboolean vtable_is_32bit = ((gsize)(vtable) == (gsize)(int)(gsize)(vtable));

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done) {
					if (amd64_is_imm32 (item->key))
						item->chunk_size += CMP_SIZE;
					else
						item->chunk_size += MOV_REG_IMM_SIZE + CMP_REG_REG_SIZE;
				}
				if (item->has_target_code) {
					item->chunk_size += MOV_REG_IMM_SIZE;
				} else {
					if (vtable_is_32bit)
						item->chunk_size += MOV_REG_IMM_32BIT_SIZE;
					else
						item->chunk_size += MOV_REG_IMM_SIZE;
#ifdef __native_client_codegen__
					item->chunk_size += JUMP_MEMBASE_SIZE;
#endif
				}
				item->chunk_size += BR_SMALL_SIZE + JUMP_REG_SIZE;
			} else {
				if (fail_tramp) {
					item->chunk_size += MOV_REG_IMM_SIZE * 3 + CMP_REG_REG_SIZE +
						BR_SMALL_SIZE + JUMP_REG_SIZE * 2;
				} else {
					if (vtable_is_32bit)
						item->chunk_size += MOV_REG_IMM_32BIT_SIZE;
					else
						item->chunk_size += MOV_REG_IMM_SIZE;
					item->chunk_size += JUMP_REG_SIZE;
					/* with assert below:
					 * item->chunk_size += CMP_SIZE + BR_SMALL_SIZE + 1;
					 */
#ifdef __native_client_codegen__
					item->chunk_size += JUMP_MEMBASE_SIZE;
#endif
				}
			}
		} else {
			if (amd64_is_imm32 (item->key))
				item->chunk_size += CMP_SIZE;
			else
				item->chunk_size += MOV_REG_IMM_SIZE + CMP_REG_REG_SIZE;
			item->chunk_size += BR_LARGE_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}
#if defined(__native_client__) && defined(__native_client_codegen__)
	/* In Native Client, we don't re-use thunks, allocate from the */
	/* normal code manager paths. */
	code = mono_domain_code_reserve (domain, size);
#else
	if (fail_tramp)
		code = mono_method_alloc_generic_virtual_thunk (domain, size);
	else
		code = mono_domain_code_reserve (domain, size);
#endif
	start = code;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = code;
		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case) {
					if (amd64_is_imm32 (item->key))
						amd64_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gssize)item->key);
					else {
						amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, item->key);
						amd64_alu_reg_reg (code, X86_CMP, MONO_ARCH_IMT_REG, MONO_ARCH_IMT_SCRATCH_REG);
					}
				}
				item->jmp_code = code;
				amd64_branch8 (code, X86_CC_NE, 0, FALSE);
				if (item->has_target_code) {
					amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, item->value.target_code);
					amd64_jump_reg (code, MONO_ARCH_IMT_SCRATCH_REG);
				} else {
					amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, & (vtable->vtable [item->value.vtable_slot]));
					amd64_jump_membase (code, MONO_ARCH_IMT_SCRATCH_REG, 0);
				}

				if (fail_case) {
					amd64_patch (item->jmp_code, code);
					amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, fail_tramp);
					amd64_jump_reg (code, MONO_ARCH_IMT_SCRATCH_REG);
					item->jmp_code = NULL;
				}
			} else {
				/* enable the commented code to assert on wrong method */
#if 0
				if (amd64_is_imm32 (item->key))
					amd64_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gssize)item->key);
				else {
					amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, item->key);
					amd64_alu_reg_reg (code, X86_CMP, MONO_ARCH_IMT_REG, MONO_ARCH_IMT_SCRATCH_REG);
				}
				item->jmp_code = code;
				amd64_branch8 (code, X86_CC_NE, 0, FALSE);
				/* See the comment below about R10 */
				amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, & (vtable->vtable [item->value.vtable_slot]));
				amd64_jump_membase (code, MONO_ARCH_IMT_SCRATCH_REG, 0);
				amd64_patch (item->jmp_code, code);
				amd64_breakpoint (code);
				item->jmp_code = NULL;
#else
				/* We're using R10 (MONO_ARCH_IMT_SCRATCH_REG) here because R11 (MONO_ARCH_IMT_REG)
				   needs to be preserved.  R10 needs
				   to be preserved for calls which
				   require a runtime generic context,
				   but interface calls don't. */
				amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, & (vtable->vtable [item->value.vtable_slot]));
				amd64_jump_membase (code, MONO_ARCH_IMT_SCRATCH_REG, 0);
#endif
			}
		} else {
			if (amd64_is_imm32 (item->key))
				amd64_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gssize)item->key);
			else {
				amd64_mov_reg_imm (code, MONO_ARCH_IMT_SCRATCH_REG, item->key);
				amd64_alu_reg_reg (code, X86_CMP, MONO_ARCH_IMT_REG, MONO_ARCH_IMT_SCRATCH_REG);
			}
			item->jmp_code = code;
			if (x86_is_imm8 (imt_branch_distance (imt_entries, i, item->check_target_idx)))
				x86_branch8 (code, X86_CC_GE, 0, FALSE);
			else
				x86_branch32 (code, X86_CC_GE, 0, FALSE);
		}
		g_assert (code - item->code_target <= item->chunk_size);
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				amd64_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
			}
		}
	}

	if (!fail_tramp)
		mono_stats.imt_thunks_size += code - start;
	g_assert (code - start <= size);

	nacl_domain_code_validate(domain, &start, size, &code);

	return start;
}

MonoMethod*
mono_arch_find_imt_method (mgreg_t *regs, guint8 *code)
{
	return (MonoMethod*)regs [MONO_ARCH_IMT_REG];
}
#endif

MonoVTable*
mono_arch_find_static_call_vtable (mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*) regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	mono_add_unwind_op_def_cfa (l, (guint8*)NULL, (guint8*)NULL, AMD64_RSP, 8);
	mono_add_unwind_op_offset (l, (guint8*)NULL, (guint8*)NULL, AMD64_RIP, -8);

	return l;
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	int opcode = 0;

	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sin") == 0) {
			opcode = OP_SIN;
		} else if (strcmp (cmethod->name, "Cos") == 0) {
			opcode = OP_COS;
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			opcode = OP_SQRT;
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			opcode = OP_ABS;
		}
		
		if (opcode) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = STACK_R8;
			ins->dreg = mono_alloc_freg (cfg);
			ins->sreg1 = args [0]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

		opcode = 0;
		if (cfg->opt & MONO_OPT_CMOV) {
			if (strcmp (cmethod->name, "Min") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMIN;
				if (fsig->params [0]->type == MONO_TYPE_U4)
					opcode = OP_IMIN_UN;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMIN;
				else if (fsig->params [0]->type == MONO_TYPE_U8)
					opcode = OP_LMIN_UN;
			} else if (strcmp (cmethod->name, "Max") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMAX;
				if (fsig->params [0]->type == MONO_TYPE_U4)
					opcode = OP_IMAX_UN;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMAX;
				else if (fsig->params [0]->type == MONO_TYPE_U8)
					opcode = OP_LMAX_UN;
			}
		}
		
		if (opcode) {
			MONO_INST_NEW (cfg, ins, opcode);
			ins->type = fsig->params [0]->type == MONO_TYPE_I4 ? STACK_I4 : STACK_I8;
			ins->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = args [0]->dreg;
			ins->sreg2 = args [1]->dreg;
			MONO_ADD_INS (cfg->cbb, ins);
		}

#if 0
		/* OP_FREM is not IEEE compatible */
		else if (strcmp (cmethod->name, "IEEERemainder") == 0) {
			MONO_INST_NEW (cfg, ins, OP_FREM);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif
	}

	/* 
	 * Can't implement CompareExchange methods this way since they have
	 * three arguments.
	 */

	return ins;
}

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	return 0;
}

MonoInst* mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
	MonoInst* ins;
	
	if (appdomain_tls_offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = appdomain_tls_offset;
	return ins;
}

#define _CTX_REG(ctx,fld,i) ((&ctx->fld)[i])

mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	switch (reg) {
	case AMD64_RCX: return ctx->rcx;
	case AMD64_RDX: return ctx->rdx;
	case AMD64_RBX: return ctx->rbx;
	case AMD64_RBP: return ctx->rbp;
	case AMD64_RSP: return ctx->rsp;
	default:
		if (reg < 8)
			return _CTX_REG (ctx, rax, reg);
		else if (reg >= 12)
			return _CTX_REG (ctx, r12, reg - 12);
		else
			g_assert_not_reached ();
	}
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, mgreg_t val)
{
	switch (reg) {
	case AMD64_RCX:
		ctx->rcx = val;
		break;
	case AMD64_RDX: 
		ctx->rdx = val;
		break;
	case AMD64_RBX:
		ctx->rbx = val;
		break;
	case AMD64_RBP:
		ctx->rbp = val;
		break;
	case AMD64_RSP:
		ctx->rsp = val;
		break;
	default:
		if (reg < 8)
			_CTX_REG (ctx, rax, reg) = val;
		else if (reg >= 12)
			_CTX_REG (ctx, r12, reg - 12) = val;
		else
			g_assert_not_reached ();
	}
}

/*MONO_ARCH_HAVE_HANDLER_BLOCK_GUARD*/
gpointer
mono_arch_install_handler_block_guard (MonoJitInfo *ji, MonoJitExceptionInfo *clause, MonoContext *ctx, gpointer new_value)
{
	int offset;
	gpointer *sp, old_value;
	char *bp;
	const unsigned char *handler;

	/*Decode the first instruction to figure out where did we store the spvar*/
	/*Our jit MUST generate the following:
	 mov    %rsp, ?(%rbp)

	 Which is encoded as: REX.W 0x89 mod_rm
	 mod_rm (rsp, rbp, imm) which can be: (imm will never be zero)
		mod (reg + imm8):  01 reg(rsp): 100 rm(rbp): 101 -> 01100101 (0x65)
		mod (reg + imm32): 10 reg(rsp): 100 rm(rbp): 101 -> 10100101 (0xA5)

	FIXME can we generate frameless methods on this case?

	*/
	handler = clause->handler_start;

	/*REX.W*/
	if (*handler != 0x48)
		return NULL;
	++handler;

	/*mov r, r/m */
	if (*handler != 0x89)
		return NULL;
	++handler;

	if (*handler == 0x65)
		offset = *(signed char*)(handler + 1);
	else if (*handler == 0xA5)
		offset = *(int*)(handler + 1);
	else
		return NULL;

	/*Load the spvar*/
	bp = MONO_CONTEXT_GET_BP (ctx);
	sp = *(gpointer*)(bp + offset);

	old_value = *sp;
	if (old_value < ji->code_start || (char*)old_value > ((char*)ji->code_start + ji->code_size))
		return old_value;

	*sp = new_value;

	return old_value;
}

/*
 * mono_arch_emit_load_aotconst:
 *
 *   Emit code to load the contents of the GOT slot identified by TRAMP_TYPE and
 * TARGET from the mscorlib GOT in full-aot code.
 * On AMD64, the result is placed into R11.
 */
guint8*
mono_arch_emit_load_aotconst (guint8 *start, guint8 *code, MonoJumpInfo **ji, int tramp_type, gconstpointer target)
{
	*ji = mono_patch_info_list_prepend (*ji, code - start, tramp_type, target);
	amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);

	return code;
}

/*
 * mono_arch_get_trampolines:
 *
 *   Return a list of MonoTrampInfo structures describing arch specific trampolines
 * for AOT.
 */
GSList *
mono_arch_get_trampolines (gboolean aot)
{
	return mono_amd64_get_exception_trampolines (aot);
}

/* Soft Debug support */
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED

/*
 * mono_arch_set_breakpoint:
 *
 *   Set a breakpoint at the native code corresponding to JI at NATIVE_OFFSET.
 * The location should contain code emitted by OP_SEQ_POINT.
 */
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	guint8 *orig_code = code;

	if (ji->from_aot) {
		guint32 native_offset = ip - (guint8*)ji->code_start;
		SeqPointInfo *info = mono_arch_get_seq_point_info (mono_domain_get (), ji->code_start);

		g_assert (info->bp_addrs [native_offset] == 0);
		info->bp_addrs [native_offset] = bp_trigger_page;
	} else {
		/* 
		 * In production, we will use int3 (has to fix the size in the md 
		 * file). But that could confuse gdb, so during development, we emit a SIGSEGV
		 * instead.
		 */
		g_assert (code [0] == 0x90);
		if (breakpoint_size == 8) {
			amd64_mov_reg_mem (code, AMD64_R11, (guint64)bp_trigger_page, 4);
		} else {
			amd64_mov_reg_imm_size (code, AMD64_R11, (guint64)bp_trigger_page, 8);
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, 4);
		}

		g_assert (code - orig_code == breakpoint_size);
	}
}

/*
 * mono_arch_clear_breakpoint:
 *
 *   Clear the breakpoint at IP.
 */
void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	int i;

	if (ji->from_aot) {
		guint32 native_offset = ip - (guint8*)ji->code_start;
		SeqPointInfo *info = mono_arch_get_seq_point_info (mono_domain_get (), ji->code_start);

		g_assert (info->bp_addrs [native_offset] == 0);
		info->bp_addrs [native_offset] = info;
	} else {
		for (i = 0; i < breakpoint_size; ++i)
			x86_nop (code);
	}
}

gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
#ifdef HOST_WIN32
	EXCEPTION_RECORD* einfo = (EXCEPTION_RECORD*)info;
	return FALSE;
#else
	siginfo_t* sinfo = (siginfo_t*) info;
	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= bp_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)bp_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
#endif
}

/*
 * mono_arch_skip_breakpoint:
 *
 *   Modify CTX so the ip is placed after the breakpoint instruction, so when
 * we resume, the instruction is not executed again.
 */
void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	if (ji->from_aot) {
		/* amd64_mov_reg_membase (code, AMD64_R11, AMD64_R11, 0, 8) */
		MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 3);
	} else {
		MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + breakpoint_fault_size);
	}
}
	
/*
 * mono_arch_start_single_stepping:
 *
 *   Start single stepping.
 */
void
mono_arch_start_single_stepping (void)
{
	mono_mprotect (ss_trigger_page, mono_pagesize (), 0);
}
	
/*
 * mono_arch_stop_single_stepping:
 *
 *   Stop single stepping.
 */
void
mono_arch_stop_single_stepping (void)
{
	mono_mprotect (ss_trigger_page, mono_pagesize (), MONO_MMAP_READ);
}

/*
 * mono_arch_is_single_step_event:
 *
 *   Return whenever the machine state in SIGCTX corresponds to a single
 * step event.
 */
gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
#ifdef HOST_WIN32
	EXCEPTION_RECORD* einfo = (EXCEPTION_RECORD*)info;
	return FALSE;
#else
	siginfo_t* sinfo = (siginfo_t*) info;
	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= ss_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)ss_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
#endif
}

/*
 * mono_arch_skip_single_step:
 *
 *   Modify CTX so the ip is placed after the single step trigger instruction,
 * we resume, the instruction is not executed again.
 */
void
mono_arch_skip_single_step (MonoContext *ctx)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + single_step_fault_size);
}

/*
 * mono_arch_create_seq_point_info:
 *
 *   Return a pointer to a data structure which is used by the sequence
 * point implementation in AOTed code.
 */
gpointer
mono_arch_get_seq_point_info (MonoDomain *domain, guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;
	int i;

	// FIXME: Add a free function

	mono_domain_lock (domain);
	info = g_hash_table_lookup (domain_jit_info (domain)->arch_seq_points,
								code);
	mono_domain_unlock (domain);

	if (!info) {
		ji = mono_jit_info_table_find (domain, (char*)code);
		g_assert (ji);

		// FIXME: Optimize the size
		info = g_malloc0 (sizeof (SeqPointInfo) + (ji->code_size * sizeof (gpointer)));

		info->ss_trigger_page = ss_trigger_page;
		info->bp_trigger_page = bp_trigger_page;
		/* Initialize to a valid address */
		for (i = 0; i < ji->code_size; ++i)
			info->bp_addrs [i] = info;

		mono_domain_lock (domain);
		g_hash_table_insert (domain_jit_info (domain)->arch_seq_points,
							 code, info);
		mono_domain_unlock (domain);
	}

	return info;
}

#endif
