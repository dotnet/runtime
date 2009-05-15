/*
 * mini-hppa.h: HPPA backend for the Mono code generator
 *
 * Copyright (c) 2007 Randolph Chung
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */

#ifndef __MONO_MINI_HPPA_H__
#define __MONO_MINI_HPPA_H__

#include <mono/arch/hppa/hppa-codegen.h>

#include <glib.h>

#define MONO_ARCH_CPU_SPEC hppa_desc

/* HPPA's stack grows towards higher addresses */
#define MONO_ARCH_STACK_GROWS_UP

#define MONO_MAX_IREGS 32
#define MONO_MAX_FREGS 32

/* hppa_r20 - hppa_r22 are scratch registers
 * hppa_r23 - hppa_r26 are the incoming argument registers
 * hppa_r28, hppa_29 are the return value registers */
#define MONO_ARCH_CALLEE_REGS ((0x3f << hppa_r20) | (1 << hppa_r28) | (1 << hppa_r29))

/* hppa_r3 - hppa_r19, hppa_27, hppa_30 */
#define MONO_ARCH_CALLEE_SAVED_REGS ((0x1ffff << hppa_r3) | (1 << hppa_r27) | (1 << hppa_r30))

/* hppa_fr4 - hppa_fr7 are incoming argument registers
 * hppa_fr8 - hppa_fr11 are scratch registers 
 * hppa_fr22 - hppa_fr31 are scratch registers
 * we reserve hppa_fr31 for code generation */
#define MONO_ARCH_CALLEE_FREGS ((0xff << hppa_fr4) | (0x1ff << hppa_fr22))

/* hppa_fr12 - hppa_fr21 */
#define MONO_ARCH_CALLEE_SAVED_FREGS (0x3ff << hppa_fr12)

#define MONO_ARCH_USE_FPSTACK FALSE
#define MONO_ARCH_FPSTACK_SIZE 0
#define MONO_ARCH_INST_FIXED_REG(desc) ((desc == 'o') ? hppa_r0 : (desc == 'a') ? hppa_r28 : (desc == 'L') ? hppa_r29 : -1)
#define MONO_ARCH_INST_SREG2_MASK(ins) (0)

#define MONO_ARCH_INST_IS_REGPAIR(desc) ((desc == 'l') || (desc == 'L'))
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) ((desc == 'L') ? hppa_r28 : (desc == 'l') ? ((hreg1)+1) : -1)

#define MONO_ARCH_FRAME_ALIGNMENT 64
#define MONO_ARCH_CODE_ALIGNMENT 32
#define HPPA_STACK_LMF_OFFSET	8

/* r3-r19, r27 */
#define MONO_SAVED_GREGS 18
#define MONO_SAVED_GREGS_MASK	((0x1ffff << hppa_r3) | (1 << hppa_r27))
/* fr12-fr21 */
#define MONO_SAVED_FREGS 10
#define MONO_SAVED_FREGS_MASK	(0x003ff000)
#define HPPA_IS_SAVED_GREG(i)	((1 << (i)) & MONO_SAVED_GREGS_MASK)
#define HPPA_IS_SAVED_FREG(i)	((1 << (i)) & MONO_SAVED_FREGS_MASK)

struct MonoLMF {
	gpointer    previous_lmf;
	gpointer    lmf_addr;
	MonoMethod *method;
	gpointer    eip; /* pc */
	gpointer    ebp; /* sp */
	gulong regs [MONO_SAVED_GREGS];
	double fregs [MONO_SAVED_FREGS];
};

typedef struct MonoContext {
	gulong pc;
	gulong sp;
	gulong regs [MONO_SAVED_GREGS];
	double fregs [MONO_SAVED_FREGS];
} MonoContext;

typedef struct MonoCompileArch {
	gint32 lmf_offset;
	gint32 localloc_offset;
} MonoCompileArch;

#define MONO_CONTEXT_SET_IP(ctx,_ip) do { (ctx)->pc = (int)(_ip); } while (0) 
#define MONO_CONTEXT_SET_BP(ctx,_bp) do { (ctx)->sp = (int)(_bp); } while (0) 
#define MONO_CONTEXT_SET_SP(ctx,_sp) do { (ctx)->sp = (int)(_sp); } while (0)

#define MONO_CONTEXT_GET_IP(ctx) ((gpointer)((ctx)->pc))
#define MONO_CONTEXT_GET_BP(ctx) ((gpointer)((ctx)->sp))
#define MONO_CONTEXT_GET_SP(ctx) ((gpointer)((ctx)->sp))

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
	unsigned long sp;					\
	asm volatile ("copy %%sp, %0\n" : "=r"(sp));		\
	MONO_CONTEXT_SET_IP ((ctx), (start_func));		\
	MONO_CONTEXT_SET_BP ((ctx), sp);			\
	} while (0)

#define MONO_ARCH_USE_SIGACTION 1

#define MONO_ARCH_EMULATE_FCONV_TO_I8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R8   1
#define MONO_ARCH_EMULATE_LCONV_TO_R4   1
#define MONO_ARCH_EMULATE_CONV_R8_UN    1
#define MONO_ARCH_EMULATE_LCONV_TO_R8_UN 1
#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_EMULATE_DIV 1
#define MONO_ARCH_NEED_DIV_CHECK 1

/* 
 * HPPA does not have an addresable "cflags", so all our compare and branch
 * instructions are combined
 */

#define	MONO_EMIT_NEW_HPPA_COND_EXC(cfg,cond,sr1,sr2,name) do {	\
	        MonoInst *inst; \
		inst = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		inst->opcode = cond;  \
	        inst->inst_p1 = (char*)name; \
		inst->sreg1 = sr1; \
		inst->sreg2 = sr2; \
	        mono_bblock_add_inst ((cfg)->cbb, inst); \
	} while (0)

#define MONO_EMIT_NEW_COMPARE_EXC(cfg, cmp_op, sreg1, sreg2, exc) \
	MONO_EMIT_NEW_HPPA_COND_EXC (cfg, OP_HPPA_COND_EXC_##cmp_op, sreg1, sreg2, exc)

#define MONO_EMIT_NEW_COMPARE_IMM_EXC(cfg, cmp_op, sreg1, imm, exc) do { \
		guint32 cmp_reg; \
		if (!(imm)) { \
			cmp_reg = hppa_r0; \
		} \
		else { \
			cmp_reg = hppa_r1; \
			MONO_EMIT_NEW_ICONST (cfg, cmp_reg, (imm)); \
		} \
		MONO_EMIT_NEW_COMPARE_EXC (cfg, cmp_op, sreg1, cmp_reg, exc); \
	} while (0)

#define MONO_EMIT_NEW_ICOMPARE_IMM_EXC(cfg, cmp_op, sreg1, imm, exc) do { \
		MONO_EMIT_NEW_COMPARE_IMM_EXC(cfg, cmp_op, sreg1, imm, exc); \
	} while (0)

typedef struct {
	gint8 reg;
	gint8 size;
	gint8 pass_in_reg:1;
	int vtsize;
	int offset;
} MonoHPPAArgInfo;


void hppa_patch (guint32 *code, const gpointer target);

#endif /* __MONO_MINI_HPPA_H__ */  
