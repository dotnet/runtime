#include "mini.h"
#include "mini-runtime.h"
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/loader-internals.h>
#include <mono/metadata/icall-internals.h>
#include <mono/metadata/seq-points-data.h>
#include <mono/mini/aot-runtime.h>
#include <mono/mini/seq-points.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/components.h>

#ifdef HOST_BROWSER
#ifndef DISABLE_THREADS
#include <mono/utils/mono-threads-wasm.h>
#endif
#endif

static int mono_wasm_debug_level = 0;
#ifndef DISABLE_JIT

#include "ir-emit.h"
#include "cpu-wasm.h"

 //FIXME figure out if we need to distingush between i,l,f,d types
typedef enum {
	ArgOnStack,
	ArgValuetypeAddrOnStack,
	ArgGsharedVTOnStack,
	ArgValuetypeAddrInIReg,
	ArgVtypeAsScalar,
	ArgInvalid,
} ArgStorage;

typedef struct {
	ArgStorage storage : 8;
	MonoType *type, *etype;
} ArgInfo;

struct CallInfo {
	int nargs;
	gboolean gsharedvt;

	ArgInfo ret;
	ArgInfo args [1];
};

// WASM ABI: https://github.com/WebAssembly/tool-conventions/blob/main/BasicCABI.md

static ArgStorage
get_storage (MonoType *type, MonoType **etype, gboolean is_return)
{
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_OBJECT:
		return ArgOnStack;

	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		return ArgOnStack;

	case MONO_TYPE_R4:
		return ArgOnStack;

	case MONO_TYPE_R8:
		return ArgOnStack;

	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (type))
			return ArgOnStack;

		if (mini_is_gsharedvt_variable_type (type))
			return ArgGsharedVTOnStack;
		/* fall through */
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF: {
		if (mini_wasm_is_scalar_vtype (type, etype))
			return ArgVtypeAsScalar;
		return is_return ? ArgValuetypeAddrInIReg : ArgValuetypeAddrOnStack;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (mini_is_gsharedvt_type (type));
		return ArgGsharedVTOnStack;
	case MONO_TYPE_VOID:
		g_assert (is_return);
		break;
	default:
		g_error ("Can't handle as return value 0x%x", type->type);
	}
	return ArgInvalid;
}

static CallInfo*
get_call_info (MonoMemPool *mp, MonoMethodSignature *sig)
{
	int n = sig->hasthis + sig->param_count;
	CallInfo *cinfo;

	if (mp)
		cinfo = (CallInfo *)mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = (CallInfo *)g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	cinfo->nargs = n;
	cinfo->gsharedvt = mini_is_gsharedvt_variable_signature (sig);

	/* return value */
	cinfo->ret.type = mini_get_underlying_type (sig->ret);
	cinfo->ret.storage = get_storage (cinfo->ret.type, &cinfo->ret.etype, TRUE);

	if (sig->hasthis)
		cinfo->args [0].storage = ArgOnStack;

	// not supported
	g_assert (sig->call_convention != MONO_CALL_VARARG);

	int i;
	for (i = 0; i < sig->param_count; ++i) {
		cinfo->args [i + sig->hasthis].type = mini_get_underlying_type (sig->params [i]);
		cinfo->args [i + sig->hasthis].storage = get_storage (cinfo->args [i + sig->hasthis].type, &cinfo->args [i + sig->hasthis].etype, FALSE);
	}

	return cinfo;
}

gboolean
mono_arch_have_fast_tls (void)
{
	return FALSE;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	g_error ("mono_arch_get_patch_offset");
	return 0;
}
gpointer
mono_arch_ip_from_context (void *sigctx)
{
	g_error ("mono_arch_ip_from_context");
}

gboolean
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return TRUE;
}

void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

gboolean
mono_arch_opcode_supported (int opcode)
{
	switch (opcode) {
	case OP_ATOMIC_ADD_I4:
	case OP_ATOMIC_ADD_I8:
	case OP_ATOMIC_EXCHANGE_I4:
	case OP_ATOMIC_EXCHANGE_I8:
	case OP_ATOMIC_CAS_I4:
	case OP_ATOMIC_CAS_I8:
	case OP_ATOMIC_LOAD_I1:
	case OP_ATOMIC_LOAD_I2:
	case OP_ATOMIC_LOAD_I4:
	case OP_ATOMIC_LOAD_I8:
	case OP_ATOMIC_LOAD_U1:
	case OP_ATOMIC_LOAD_U2:
	case OP_ATOMIC_LOAD_U4:
	case OP_ATOMIC_LOAD_U8:
	case OP_ATOMIC_LOAD_R4:
	case OP_ATOMIC_LOAD_R8:
	case OP_ATOMIC_STORE_I1:
	case OP_ATOMIC_STORE_I2:
	case OP_ATOMIC_STORE_I4:
	case OP_ATOMIC_STORE_I8:
	case OP_ATOMIC_STORE_U1:
	case OP_ATOMIC_STORE_U2:
	case OP_ATOMIC_STORE_U4:
	case OP_ATOMIC_STORE_U8:
	case OP_ATOMIC_STORE_R4:
	case OP_ATOMIC_STORE_R8:
		return TRUE;
	default:
		return FALSE;
	}
	return FALSE;
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	g_error ("mono_arch_output_basic_block");
}

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	return 0;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	g_error ("mono_arch_get_allocatable_int_vars");
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	g_error ("mono_arch_get_global_int_regs");
}

void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	g_error ("mono_arch_allocate_vars");
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = (CallInfo *)cfg->arch.cinfo;

	// if (cinfo->ret.storage == ArgValuetypeInReg)
	// 	cfg->ret_var_is_local = TRUE;

	mini_get_underlying_type (sig->ret);
	if (cinfo->ret.storage == ArgValuetypeAddrInIReg || cinfo->ret.storage == ArgGsharedVTOnStack) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_sdb_seq_points)
		g_error ("gen_sdb_seq_points not supported");

	if (cfg->method->save_lmf) {
		cfg->create_lmf_var = TRUE;
		cfg->lmf_ir = TRUE;
	}
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	g_error ("mono_arch_emit_call");
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	g_error ("mono_arch_emit_epilog");
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	g_error ("mono_arch_emit_exceptions");
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return NULL;
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	g_error ("mono_arch_emit_outarg_vt");
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	g_error ("mono_arch_emit_prolog");
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_get_underlying_type (mono_method_signature_internal (method)->ret);

	if (!m_type_is_byref (ret)) {
		if (ret->type == MONO_TYPE_R4) {
			MONO_EMIT_NEW_UNALU (cfg, OP_RMOVE, cfg->ret->dreg, val->dreg);
			return;
		} else if (ret->type == MONO_TYPE_R8) {
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
			return;
		} else if (ret->type == MONO_TYPE_I8 || ret->type == MONO_TYPE_U8) {
			MONO_EMIT_NEW_UNALU (cfg, OP_LMOVE, cfg->ret->dreg, val->dreg);
			return;
		}
	}
	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
}

LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	int i, n;
	CallInfo *cinfo;
	LLVMCallInfo *linfo;

	cinfo = get_call_info (cfg->mempool, sig);
	n = cinfo->nargs;

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	if (cinfo->ret.storage == ArgVtypeAsScalar) {
		linfo->ret.storage = LLVMArgWasmVtypeAsScalar;
		linfo->ret.etype = cinfo->ret.etype;
		linfo->ret.esize = mono_class_value_size (mono_class_from_mono_type_internal (cinfo->ret.type), NULL);
	} else if (mini_type_is_vtype (sig->ret)) {
		/* Vtype returned using a hidden argument */
		linfo->ret.storage = LLVMArgVtypeRetAddr;
		// linfo->vret_arg_index = cinfo->vret_arg_index;
	} else {
		if (sig->ret->type != MONO_TYPE_VOID)
			linfo->ret.storage = LLVMArgNormal;
	}

	for (i = 0; i < n; ++i) {
		ArgInfo *ainfo = &cinfo->args[i];

		switch (ainfo->storage) {
		case ArgOnStack:
			linfo->args [i].storage = LLVMArgNormal;
			break;
		case ArgValuetypeAddrOnStack:
			linfo->args [i].storage = LLVMArgVtypeByRef;
			break;
		case ArgGsharedVTOnStack:
			linfo->args [i].storage = LLVMArgGsharedvtVariable;
			break;
		case ArgVtypeAsScalar:
			linfo->args [i].storage = LLVMArgWasmVtypeAsScalar;
			linfo->args [i].type = ainfo->type;
			linfo->args [i].etype = ainfo->etype;
			linfo->args [i].esize = mono_class_value_size (mono_class_from_mono_type_internal (ainfo->type), NULL);
			break;
		case ArgValuetypeAddrInIReg:
			g_error ("this is only valid for sig->ret");
			break;
		}
	}

	return linfo;
}

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	return FALSE;
}

#endif // DISABLE_JIT

const char*
mono_arch_fregname (int reg)
{
	return "freg0";
}

const char*
mono_arch_regname (int reg)
{
	return "r0";
}

int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	g_error ("mono_arch_get_argument_info");
}

GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	g_error ("mono_arch_get_delegate_invoke_impls");
}

gpointer
mono_arch_get_gsharedvt_call_info (MonoMemoryManager *mem_manager, gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	g_error ("mono_arch_get_gsharedvt_call_info");
	return NULL;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	g_error ("mono_arch_get_delegate_invoke_impl");
}

#ifdef HOST_BROWSER

#include <emscripten.h>

//functions exported to be used by JS
G_BEGIN_DECLS
EMSCRIPTEN_KEEPALIVE void mono_wasm_execute_timer (void);

//JS functions imported that we use
extern void mono_wasm_schedule_timer (int shortestDueTimeMs);
G_END_DECLS

void mono_background_exec (void);

#endif // HOST_BROWSER

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	g_error ("mono_arch_get_this_arg_from_call");
}

gpointer
mono_arch_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method, int offset, gboolean load_imt_reg)
{
	g_error ("mono_arch_get_delegate_virtual_invoke_impl");
}


void
mono_arch_cpu_init (void)
{
	// printf ("mono_arch_cpu_init\n");
}

void
mono_arch_finish_init (void)
{
	// printf ("mono_arch_finish_init\n");
}

void
mono_arch_init (void)
{
	// printf ("mono_arch_init\n");
}

void
mono_arch_cleanup (void)
{
}

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_flush_register_windows (void)
{
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
	g_error ("mono_arch_find_static_call_vtable");
	return (MonoMethod*) regs [MONO_ARCH_IMT_REG];
}

MonoVTable*
mono_arch_find_static_call_vtable (host_mgreg_t *regs, guint8 *code)
{
	g_error ("mono_arch_find_static_call_vtable");
	return (MonoVTable*) regs [MONO_ARCH_RGCTX_REG];
}

GSList*
mono_arch_get_cie_program (void)
{
	GSList *l = NULL;

	return l;
}

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	g_error ("mono_arch_build_imt_trampoline");
}

guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	/* No arch specific passes yet */
	*exclude_mask = 0;
	return 0;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	g_error ("mono_arch_context_get_int_reg");
	return 0;
}

host_mgreg_t*
mono_arch_context_get_int_reg_address (MonoContext *ctx, int reg)
{
	g_error ("mono_arch_context_get_int_reg_address");
	return 0;
}

#if defined(HOST_BROWSER) || defined(HOST_WASI)

void
mono_runtime_install_handlers (void)
{
}

void
mono_init_native_crash_info (void)
{
	return;
}

#endif

#ifdef HOST_BROWSER

void
mono_runtime_setup_stat_profiler (void)
{
	g_error ("mono_runtime_setup_stat_profiler");
}

gboolean
MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal)
{
	g_error ("mono_chain_signal");

	return FALSE;
}

gboolean
mono_thread_state_init_from_handle (MonoThreadUnwindState *tctx, MonoThreadInfo *info, void *sigctx)
{
	g_error ("WASM systems don't support mono_thread_state_init_from_handle");
	return FALSE;
}

// this points to System.Threading.TimerQueue.TimerHandler C# method
static void *timer_handler;

EMSCRIPTEN_KEEPALIVE void
mono_wasm_execute_timer (void)
{
	// callback could be null if timer was never used by the application, but only by prevent_timer_throttling_tick()
	if (timer_handler==NULL) {
		return;
	}

	background_job_cb cb = timer_handler;
	MONO_ENTER_GC_UNSAFE;
	cb ();
	MONO_EXIT_GC_UNSAFE;
}

#ifdef DISABLE_THREADS
void
mono_wasm_main_thread_schedule_timer (void *timerHandler, int shortestDueTimeMs)
{
	// NOTE: here the `timerHandler` callback is [UnmanagedCallersOnly] which wraps it with MONO_ENTER_GC_UNSAFE/MONO_EXIT_GC_UNSAFE

	g_assert (timerHandler);
	timer_handler = timerHandler;
    mono_wasm_schedule_timer (shortestDueTimeMs);
}
#endif
#endif

void
mono_arch_register_icall (void)
{
#ifdef HOST_BROWSER
#ifdef DISABLE_THREADS
	mono_add_internal_call_internal ("System.Threading.TimerQueue::MainThreadScheduleTimer", mono_wasm_main_thread_schedule_timer);
	mono_add_internal_call_internal ("System.Threading.ThreadPool::MainThreadScheduleBackgroundJob", mono_main_thread_schedule_background_job);
#else
	mono_add_internal_call_internal ("System.Runtime.InteropServices.JavaScript.JSSynchronizationContext::TargetThreadScheduleBackgroundJob", mono_target_thread_schedule_background_job);
#endif /* DISABLE_THREADS */
#endif /* HOST_BROWSER */
}

void
mono_arch_patch_code_new (MonoCompile *cfg, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	g_error ("mono_arch_patch_code_new");
}

#ifdef HOST_BROWSER

G_BEGIN_DECLS

int inotify_init (void);
int inotify_rm_watch (int fd, int wd);
int inotify_add_watch (int fd, const char *pathname, uint32_t mask);
int sem_timedwait (sem_t *sem, const struct timespec *abs_timeout);

G_END_DECLS

G_BEGIN_DECLS

//llvm builtin's that we should not have used in the first place

#include <sys/types.h>
#include <pwd.h>
#include <uuid/uuid.h>

#ifndef __EMSCRIPTEN_PTHREADS__
int pthread_getschedparam (pthread_t thread, int *policy, struct sched_param *param)
{
	g_error ("pthread_getschedparam");
	return 0;
}
#endif

int
pthread_setschedparam(pthread_t thread, int policy, const struct sched_param *param)
{
	return 0;
}

int
sigsuspend(const sigset_t *sigmask)
{
	g_error ("sigsuspend");
	return 0;
}

int
inotify_init (void)
{
	g_error ("inotify_init");
}

int
inotify_rm_watch (int fd, int wd)
{
	g_error ("inotify_rm_watch");
	return 0;
}

int
inotify_add_watch (int fd, const char *pathname, uint32_t mask)
{
	g_error ("inotify_add_watch");
	return 0;
}

#ifndef __EMSCRIPTEN_PTHREADS__
int
sem_timedwait (sem_t *sem, const struct timespec *abs_timeout)
{
	g_error ("sem_timedwait");
	return 0;
}
#endif

ssize_t sendfile(int out_fd, int in_fd, off_t *offset, size_t count);

ssize_t sendfile(int out_fd, int in_fd, off_t *offset, size_t count)
{
	errno = ENOTSUP;
	return -1;
}

G_END_DECLS

/* Helper for runtime debugging */
void
mono_wasm_print_stack_trace (void)
{
	EM_ASM(
		   var err = new Error();
		   console.log ("Stacktrace: \n");
		   console.log (err.stack);
		   );
}

#endif // HOST_BROWSER

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	return NULL;
}

MONO_API void
mono_wasm_enable_debugging (int log_level)
{
	mono_wasm_debug_level = log_level;
}

int
mono_wasm_get_debug_level (void)
{
	return mono_wasm_debug_level;
}

/* Return whenever TYPE represents a vtype with only one scalar member */
gboolean
mini_wasm_is_scalar_vtype (MonoType *type, MonoType **etype)
{
	MonoClass *klass;
	MonoClassField *field;
	gpointer iter;

	if (etype)
		*etype = NULL;

	if (!MONO_TYPE_ISSTRUCT (type))
		return FALSE;
	klass = mono_class_from_mono_type_internal (type);
	mono_class_init_internal (klass);

	// A careful reading of the ABI spec suggests that an inlinearray does not count
	//  as having one scalar member (it either has N scalar members or one array member)
	if (m_class_is_inlinearray (klass))
		return FALSE;

	int size = mono_class_value_size (klass, NULL);
	if (size == 0 || size > 8)
		return FALSE;

	iter = NULL;
	int nfields = 0;
	field = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		nfields ++;
		if (nfields > 1)
			return FALSE;
		MonoType *t = mini_get_underlying_type (field->type);
		if (MONO_TYPE_ISSTRUCT (t)) {
			if (!mini_wasm_is_scalar_vtype (t, etype))
				return FALSE;
		} else if (!((MONO_TYPE_IS_PRIMITIVE (t) || MONO_TYPE_IS_REFERENCE (t) || MONO_TYPE_IS_POINTER (t)))) {
			return FALSE;
		} else {
			if (etype)
				*etype = t;
		}
	}

	if (etype) {
		if (!(*etype))
			*etype = mono_get_int32_type ();
	}

	return TRUE;
}
