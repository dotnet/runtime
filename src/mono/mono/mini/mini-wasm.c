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

//XXX This is dirty, extend ee.h to support extracting info from MonoInterpFrameHandle
#include <mono/mini/interp/interp-internals.h>

#ifndef DISABLE_JIT

#include "ir-emit.h"
#include "cpu-wasm.h"

 //FIXME figure out if we need to distingush between i,l,f,d types
typedef enum {
	ArgOnStack,
	ArgValuetypeAddrOnStack,
	ArgGsharedVTOnStack,
	ArgValuetypeAddrInIReg,
	ArgInvalid,
} ArgStorage;

typedef struct {
	ArgStorage storage : 8;
} ArgInfo;

struct CallInfo {
	int nargs;
	gboolean gsharedvt;

	ArgInfo ret;
	ArgInfo args [1];
};

static ArgStorage
get_storage (MonoType *type, gboolean is_return)
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

		if (mini_is_gsharedvt_type (type)) {
			return ArgGsharedVTOnStack;
		}
		/* fall through */
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF: {
		return is_return ? ArgValuetypeAddrInIReg : ArgValuetypeAddrOnStack;
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_assert (mini_is_gsharedvt_type (type));
		return ArgGsharedVTOnStack;
		break;
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
	cinfo->ret.storage = get_storage (mini_get_underlying_type (sig->ret), TRUE);

	if (sig->hasthis)
		cinfo->args [0].storage = ArgOnStack;

	// not supported
	g_assert (sig->call_convention != MONO_CALL_VARARG);

	int i;
	for (i = 0; i < sig->param_count; ++i)
		cinfo->args [i + sig->hasthis].storage = get_storage (mini_get_underlying_type (sig->params [i]), FALSE);

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
	MonoType *sig_ret;

	sig = mono_method_signature_internal (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->mempool, sig);
	cinfo = (CallInfo *)cfg->arch.cinfo;

	// if (cinfo->ret.storage == ArgValuetypeInReg)
	// 	cfg->ret_var_is_local = TRUE;

	sig_ret = mini_get_underlying_type (sig->ret);
	if (cinfo->ret.storage == ArgValuetypeAddrInIReg || cinfo->ret.storage == ArgGsharedVTOnStack) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_sdb_seq_points)
		g_error ("gen_sdb_seq_points not supported");

	if (cfg->method->save_lmf)
		cfg->create_lmf_var = TRUE;

	if (cfg->method->save_lmf)
		cfg->lmf_ir = TRUE;
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

	if (!ret->byref) {
		if (ret->type == MONO_TYPE_R4) {
			MONO_EMIT_NEW_UNALU (cfg, cfg->r4fp ? OP_RMOVE : OP_FMOVE, cfg->ret->dreg, val->dreg);
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

	if (mini_type_is_vtype (sig->ret)) {
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
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	g_error ("mono_arch_get_gsharedvt_call_info");
	return NULL;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	g_error ("mono_arch_get_delegate_invoke_impl");
}

#ifdef HOST_WASM

#include <emscripten.h>

//functions exported to be used by JS
G_BEGIN_DECLS
EMSCRIPTEN_KEEPALIVE void mono_set_timeout_exec (int id);

//JS functions imported that we use
extern void mono_set_timeout (int t, int d);
G_END_DECLS

#endif // HOST_WASM

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

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
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

gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	g_error ("mono_arch_build_imt_trampoline");
}

guint32
mono_arch_cpu_enumerate_simd_versions (void)
{
	return 0;
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

#ifdef HOST_WASM

void
mono_runtime_setup_stat_profiler (void)
{
	g_error ("mono_runtime_setup_stat_profiler");
}


void
mono_runtime_shutdown_stat_profiler (void)
{
	g_error ("mono_runtime_shutdown_stat_profiler");
}


gboolean
MONO_SIG_HANDLER_SIGNATURE (mono_chain_signal)
{
	g_error ("mono_chain_signal");
	
	return FALSE;
}

void
mono_runtime_install_handlers (void)
{
}

void
mono_runtime_cleanup_handlers (void)
{
}

void
mono_init_native_crash_info (void)
{
	return;
}

void
mono_cleanup_native_crash_info (void)
{
	return;
}

gboolean
mono_thread_state_init_from_handle (MonoThreadUnwindState *tctx, MonoThreadInfo *info, void *sigctx)
{
	g_error ("WASM systems don't support mono_thread_state_init_from_handle");
	return FALSE;
}

EMSCRIPTEN_KEEPALIVE void
mono_set_timeout_exec (int id)
{
	ERROR_DECL (error);
	MonoClass *klass = mono_class_load_from_name (mono_defaults.corlib, "System.Threading", "WasmRuntime");
	g_assert (klass);

	MonoMethod *method = mono_class_get_method_from_name_checked (klass, "TimeoutCallback", -1, 0, error);
	mono_error_assert_ok (error);
	g_assert (method);

	gpointer params[1] = { &id };
	MonoObject *exc = NULL;

	mono_runtime_try_invoke (method, NULL, params, &exc, error);

	//YES we swallow exceptions cuz there's nothing much we can do from here.
	//FIXME Maybe call the unhandled exception function?
	if (!is_ok (error)) {
		printf ("timeout callback failed due to %s\n", mono_error_get_message (error));
		mono_error_cleanup (error);
	}

	if (exc) {
		char *type_name = mono_type_get_full_name (mono_object_class (exc));
		printf ("timeout callback threw a %s\n", type_name);
		g_free (type_name);
	}
}

#endif

void
mono_wasm_set_timeout (int timeout, int id)
{
#ifdef HOST_WASM
	mono_set_timeout (timeout, id);
#endif
}

void
mono_arch_register_icall (void)
{
	mono_add_internal_call_internal ("System.Threading.WasmRuntime::SetTimeout", mono_wasm_set_timeout);
}

void
mono_arch_patch_code_new (MonoCompile *cfg, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gpointer target)
{
	g_error ("mono_arch_patch_code_new");
}

#ifdef HOST_WASM

/*
The following functions don't belong here, but are due to laziness.
*/
gboolean mono_w32file_get_file_system_type (const gunichar2 *path, gunichar2 *fsbuffer, gint fsbuffersize);

G_BEGIN_DECLS

void * getgrnam (const char *name);
void * getgrgid (gid_t gid);
int inotify_init (void);
int inotify_rm_watch (int fd, int wd);
int inotify_add_watch (int fd, const char *pathname, uint32_t mask);
int sem_timedwait (sem_t *sem, const struct timespec *abs_timeout);

G_END_DECLS

//w32file-wasm.c
gboolean
mono_w32file_get_file_system_type (const gunichar2 *path, gunichar2 *fsbuffer, gint fsbuffersize)
{
	glong len;
	gboolean status = FALSE;

	gunichar2 *ret = g_utf8_to_utf16 ("memfs", -1, NULL, &len, NULL);
	if (ret != NULL && len < fsbuffersize) {
		memcpy (fsbuffer, ret, len * sizeof (gunichar2));
		fsbuffer [len] = 0;
		status = TRUE;
	}
	if (ret != NULL)
		g_free (ret);

	return status;
}

G_BEGIN_DECLS

//llvm builtin's that we should not have used in the first place

#include <sys/types.h>
#include <pwd.h>
#include <uuid/uuid.h>

//libc / libpthread missing bits from musl or shit we didn't detect :facepalm:
int pthread_getschedparam (pthread_t thread, int *policy, struct sched_param *param)
{
	g_error ("pthread_getschedparam");
	return 0;
}

int
pthread_setschedparam(pthread_t thread, int policy, const struct sched_param *param)
{
	return 0;
}


int
pthread_attr_getstacksize (const pthread_attr_t *attr, size_t *stacksize)
{
	return 65536; //wasm page size
}

int
pthread_sigmask (int how, const sigset_t *set, sigset_t *oset)
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
getdtablesize (void)
{
	return 256; //random constant that is the fd limit
}

void *
getgrnam (const char *name)
{
	return NULL;
}

void *
getgrgid (gid_t gid)
{
	return NULL;
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

int
sem_timedwait (sem_t *sem, const struct timespec *abs_timeout)
{
	g_error ("sem_timedwait");
	return 0;
	
}

ssize_t sendfile(int out_fd, int in_fd, off_t *offset, size_t count);

ssize_t sendfile(int out_fd, int in_fd, off_t *offset, size_t count)
{
	g_error ("sendfile");
	return 0;
}

int
getpwnam_r (const char *name, struct passwd *pwd, char *buffer, size_t bufsize,
			struct passwd **result)
{
	g_error ("getpwnam_r");
	return 0;
}

int
getpwuid_r (uid_t uid, struct passwd *pwd, char *buffer, size_t bufsize,
			struct passwd **result)
{
	g_error ("getpwuid_r");
	return 0;
}

G_END_DECLS

#endif // HOST_WASM

gpointer
mono_arch_load_function (MonoJitICallId jit_icall_id)
{
	return NULL;
}
