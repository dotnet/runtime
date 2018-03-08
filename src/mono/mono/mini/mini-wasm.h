#ifndef __MONO_MINI_WASM_H__
#define __MONO_MINI_WASM_H__

#include <mono/utils/mono-sigcontext.h>
#include <mono/utils/mono-context.h>

#define MONO_ARCH_CPU_SPEC mono_wasm_desc

#define MONO_MAX_IREGS 1
#define MONO_MAX_FREGS 1

#define WASM_REG_0 0


struct MonoLMF {
	/* 
	 * If the second lowest bit is set to 1, then this is a MonoLMFExt structure, and
	 * the other fields are not valid.
	 */
	gpointer previous_lmf;
	gpointer lmf_addr;

	/* This is set to signal this is the top lmf entry */
	gboolean top_entry;
};

typedef struct {
	int dummy;
} MonoCompileArch;

#define MONO_ARCH_INIT_TOP_LMF_ENTRY(lmf) do { (lmf)->top_entry = TRUE; } while (0)

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

#define MONO_ARCH_INTERPRETER_SUPPORTED 1
#define MONO_ARCH_HAS_REGISTER_ICALL 1
#define MONO_ARCH_HAVE_PATCH_CODE_NEW 1

void mono_wasm_debugger_init (void);

void mono_wasm_enable_debugging (void);
void mono_wasm_breakpoint_hit (void);
void mono_wasm_set_timeout (int timeout, int id);

#endif /* __MONO_MINI_WASM_H__ */  
