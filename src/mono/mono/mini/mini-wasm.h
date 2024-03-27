#ifndef __MONO_MINI_WASM_H__
#define __MONO_MINI_WASM_H__

#include <mono/utils/mono-sigcontext.h>
#include <mono/utils/mono-context.h>

#define MONO_ARCH_CPU_SPEC mono_wasm_desc

#define MONO_MAX_IREGS 1
#define MONO_MAX_FREGS 1
#define MONO_MAX_XREGS 1

#define WASM_REG_0 0

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 0

#define MONO_ARCH_AOT_SUPPORTED 1
#define MONO_ARCH_LLVM_SUPPORTED 1
#define MONO_ARCH_GSHARED_SUPPORTED 1
#define MONO_ARCH_GSHAREDVT_SUPPORTED 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1
#define MONO_ARCH_NEED_DIV_CHECK 1
#define MONO_ARCH_NO_CODEMAN 1

#define MONO_ARCH_EMULATE_FREM 1
#define MONO_ARCH_EMULATE_FCONV_TO_U8 1
#define MONO_ARCH_EMULATE_FCONV_TO_U4 1
#define MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS 1
#define MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS 1
#define MONO_ARCH_FLOAT32_SUPPORTED 1

//mini-codegen stubs - this doesn't do anything
#define MONO_ARCH_CALLEE_REGS (1 << 0)
#define MONO_ARCH_CALLEE_FREGS (1 << 1)
#define MONO_ARCH_CALLEE_XREGS (1 << 2)
#define MONO_ARCH_CALLEE_SAVED_FREGS (1 << 3)
#define MONO_ARCH_CALLEE_SAVED_REGS (1 << 4)
#define MONO_ARCH_INST_FIXED_REG(desc) FALSE
#define MONO_ARCH_INST_IS_REGPAIR(desc) FALSE
#define MONO_ARCH_INST_REGPAIR_REG2(desc,hreg1) (-1)
#define MONO_ARCH_INST_SREG2_MASK(ins) 0

struct MonoLMF {
	/*
	 * If the second lowest bit is set to 1, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 */
	gpointer previous_lmf;
	gpointer lmf_addr;

	MonoMethod *method;
};

typedef struct {
	gpointer cinfo;
} MonoCompileArch;

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { } while (0)

#define MONO_CONTEXT_SET_LLVM_EXC_REG(ctx, exc) do { (ctx)->llvm_exc_reg = (gsize)exc; } while (0)

#define MONO_INIT_CONTEXT_FROM_FUNC(ctx,start_func) do {	\
	int ___tmp = 99;	\
	MONO_CONTEXT_SET_IP ((ctx), (start_func));	\
	MONO_CONTEXT_SET_BP ((ctx), (0));	\
	MONO_CONTEXT_SET_SP ((ctx), (&___tmp));	\
} while (0)


#define MONO_ARCH_VTABLE_REG WASM_REG_0
#define MONO_ARCH_IMT_REG WASM_REG_0
#define MONO_ARCH_RGCTX_REG WASM_REG_0

/* must be at a power of 2 and >= 8 */
#define MONO_ARCH_FRAME_ALIGNMENT 16

// Does the ABI have a volatile non-parameter register, so tailcall
// can pass context to generics or interfaces?
#define MONO_ARCH_HAVE_VOLATILE_NON_PARAM_REGISTER 0

#define MONO_ARCH_AOT_SUPPORTED 1
#define MONO_ARCH_LLVM_SUPPORTED 1
#define MONO_ARCH_GSHAREDVT_SUPPORTED 1
#define MONO_ARCH_HAVE_FULL_AOT_TRAMPOLINES 1

#define MONO_ARCH_SIMD_INTRINSICS 1

#define MONO_ARCH_INTERPRETER_SUPPORTED 1
#define MONO_ARCH_HAS_REGISTER_ICALL 1
#define MONO_ARCH_HAVE_SDB_TRAMPOLINES 1
#define MONO_ARCH_LLVM_TARGET_LAYOUT "e-m:e-p:32:32-i64:64-n32:64-S128"
#ifdef TARGET_WASI
#define MONO_ARCH_LLVM_TARGET_TRIPLE "wasm32-unknown-wasi"
#else
#define MONO_ARCH_LLVM_TARGET_TRIPLE "wasm32-unknown-emscripten"
#endif

// sdks/wasm/driver.c is C and uses this
G_EXTERN_C void mono_wasm_enable_debugging (int log_level);

#ifdef DISABLE_THREADS
void mono_wasm_main_thread_schedule_timer (void *timerHandler, int shortestDueTimeMs);
#endif // DISABLE_THREADS

void mono_wasm_print_stack_trace (void);

gboolean
mini_wasm_is_scalar_vtype (MonoType *type, MonoType **etype);

#endif /* __MONO_MINI_WASM_H__ */
