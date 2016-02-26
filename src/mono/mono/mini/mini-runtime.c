/*
 * mini-runtime.c: Runtime code for the JIT
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc.
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011-2015 Xamarin, Inc (http://www.xamarin.com)
 */

#include <config.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_SIGNAL_H
#include <signal.h>
#endif

#include <mono/utils/memcheck.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include "mono/metadata/profiler.h"
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/attach.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/monitor.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/checked-build.h>
#include <mono/io-layer/io-layer.h>

#include "mini.h"
#include "seq-points.h"
#include "tasklets.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"
#include "version.h"

#include "jit-icalls.h"

#include "mini-gc.h"
#include "mini-llvm.h"
#include "debugger-agent.h"

#ifdef MONO_ARCH_LLVM_SUPPORTED
#ifdef ENABLE_LLVM
#include "mini-llvm-cpp.h"
#endif
#endif

static guint32 default_opt = 0;
static gboolean default_opt_set = FALSE;

MonoNativeTlsKey mono_jit_tls_id;

#ifdef MONO_HAVE_FAST_TLS
MONO_FAST_TLS_DECLARE(mono_jit_tls);
#endif

gboolean mono_compile_aot = FALSE;
/* If this is set, no code is generated dynamically, everything is taken from AOT files */
gboolean mono_aot_only = FALSE;
/* Same as mono_aot_only, but only LLVM compiled code is used, no trampolines */
gboolean mono_llvm_only = FALSE;
MonoAotMode mono_aot_mode = MONO_AOT_MODE_NONE;

const char *mono_build_date;
gboolean mono_do_signal_chaining;
gboolean mono_do_crash_chaining;
int mini_verbose = 0;

/*
 * This flag controls whenever the runtime uses LLVM for JIT compilation, and whenever
 * it can load AOT code compiled by LLVM.
 */
gboolean mono_use_llvm = FALSE;

#define mono_jit_lock() mono_os_mutex_lock (&jit_mutex)
#define mono_jit_unlock() mono_os_mutex_unlock (&jit_mutex)
static mono_mutex_t jit_mutex;

static MonoCodeManager *global_codeman;

MonoDebugOptions debug_options;

#ifdef VALGRIND_JIT_REGISTER_MAP
int valgrind_register;
#endif

static GSList *tramp_infos;

static void register_icalls (void);

gboolean
mono_running_on_valgrind (void)
{
#ifndef HOST_WIN32
	if (RUNNING_ON_VALGRIND){
#ifdef VALGRIND_JIT_REGISTER_MAP
		valgrind_register = TRUE;
#endif
		return TRUE;
	} else
#endif
		return FALSE;
}

typedef struct {
	void *ip;
	MonoMethod *method;
} FindTrampUserData;

static void
find_tramp (gpointer key, gpointer value, gpointer user_data)
{
	FindTrampUserData *ud = (FindTrampUserData*)user_data;

	if (value == ud->ip)
		ud->method = (MonoMethod*)key;
}

/* debug function */
G_GNUC_UNUSED static char*
get_method_from_ip (void *ip)
{
	MonoJitInfo *ji;
	char *method;
	char *res;
	MonoDomain *domain = mono_domain_get ();
	MonoDebugSourceLocation *location;
	FindTrampUserData user_data;

	if (!domain)
		domain = mono_get_root_domain ();

	ji = mono_jit_info_table_find_internal (domain, (char *)ip, TRUE, TRUE);
	if (!ji) {
		user_data.ip = ip;
		user_data.method = NULL;
		mono_domain_lock (domain);
		g_hash_table_foreach (domain_jit_info (domain)->jit_trampoline_hash, find_tramp, &user_data);
		mono_domain_unlock (domain);
		if (user_data.method) {
			char *mname = mono_method_full_name (user_data.method, TRUE);
			res = g_strdup_printf ("<%p - JIT trampoline for %s>", ip, mname);
			g_free (mname);
			return res;
		}
		else
			return NULL;
	} else if (ji->is_trampoline) {
		res = g_strdup_printf ("<%p - %s trampoline>", ip, ((MonoTrampInfo*)ji->d.tramp_info)->name);
		return res;
	}

	method = mono_method_full_name (jinfo_get_method (ji), TRUE);
	/* FIXME: unused ? */
	location = mono_debug_lookup_source_location (jinfo_get_method (ji), (guint32)((guint8*)ip - (guint8*)ji->code_start), domain);

	res = g_strdup_printf (" %s + 0x%x (%p %p) [%p - %s]", method, (int)((char*)ip - (char*)ji->code_start), ji->code_start, (char*)ji->code_start + ji->code_size, domain, domain->friendly_name);

	mono_debug_free_source_location (location);
	g_free (method);

	return res;
}

/**
 * mono_pmip:
 * @ip: an instruction pointer address
 *
 * This method is used from a debugger to get the name of the
 * method at address @ip.   This routine is typically invoked from
 * a debugger like this:
 *
 * (gdb) print mono_pmip ($pc)
 *
 * Returns: the name of the method at address @ip.
 */
G_GNUC_UNUSED char *
mono_pmip (void *ip)
{
	return get_method_from_ip (ip);
}

/**
 * mono_print_method_from_ip
 * @ip: an instruction pointer address
 *
 * This method is used from a debugger to get the name of the
 * method at address @ip.
 *
 * This prints the name of the method at address @ip in the standard
 * output.  Unlike mono_pmip which returns a string, this routine
 * prints the value on the standard output.
 */
#ifdef __GNUC__
/* Prevent the linker from optimizing this away in embedding setups to help debugging */
 __attribute__((used))
#endif
void
mono_print_method_from_ip (void *ip)
{
	MonoJitInfo *ji;
	char *method;
	MonoDebugSourceLocation *source;
	MonoDomain *domain = mono_domain_get ();
	MonoDomain *target_domain = mono_domain_get ();
	FindTrampUserData user_data;
	MonoGenericSharingContext*gsctx;
	const char *shared_type;

	ji = mini_jit_info_table_find_ext (domain, (char *)ip, TRUE, &target_domain);
	if (ji && ji->is_trampoline) {
		MonoTrampInfo *tinfo = (MonoTrampInfo *)ji->d.tramp_info;

		printf ("IP %p is at offset 0x%x of trampoline '%s'.\n", ip, (int)((guint8*)ip - tinfo->code), tinfo->name);
		return;
	}

	if (!ji) {
		user_data.ip = ip;
		user_data.method = NULL;
		mono_domain_lock (domain);
		g_hash_table_foreach (domain_jit_info (domain)->jit_trampoline_hash, find_tramp, &user_data);
		mono_domain_unlock (domain);

		if (user_data.method) {
			char *mname = mono_method_full_name (user_data.method, TRUE);
			printf ("IP %p is a JIT trampoline for %s\n", ip, mname);
			g_free (mname);
			return;
		}

		g_print ("No method at %p\n", ip);
		fflush (stdout);
		return;
	}
	method = mono_method_full_name (jinfo_get_method (ji), TRUE);
	source = mono_debug_lookup_source_location (jinfo_get_method (ji), (guint32)((guint8*)ip - (guint8*)ji->code_start), target_domain);

	gsctx = mono_jit_info_get_generic_sharing_context (ji);
	shared_type = "";
	if (gsctx) {
		if (gsctx->is_gsharedvt)
			shared_type = "gsharedvt ";
		else
			shared_type = "gshared ";
	}

	g_print ("IP %p at offset 0x%x of %smethod %s (%p %p)[domain %p - %s]\n", ip, (int)((char*)ip - (char*)ji->code_start), shared_type, method, ji->code_start, (char*)ji->code_start + ji->code_size, target_domain, target_domain->friendly_name);

	if (source)
		g_print ("%s:%d\n", source->source_file, source->row);
	fflush (stdout);

	mono_debug_free_source_location (source);
	g_free (method);
}

/*
 * mono_method_same_domain:
 *
 * Determine whenever two compiled methods are in the same domain, thus
 * the address of the callee can be embedded in the caller.
 */
gboolean mono_method_same_domain (MonoJitInfo *caller, MonoJitInfo *callee)
{
	MonoMethod *cmethod;

	if (!caller || caller->is_trampoline || !callee || callee->is_trampoline)
		return FALSE;

	/*
	 * If the call was made from domain-neutral to domain-specific
	 * code, we can't patch the call site.
	 */
	if (caller->domain_neutral && !callee->domain_neutral)
		return FALSE;

	cmethod = jinfo_get_method (caller);
	if ((cmethod->klass == mono_defaults.appdomain_class) &&
		(strstr (cmethod->name, "InvokeInDomain"))) {
		 /* The InvokeInDomain methods change the current appdomain */
		return FALSE;
	}

	return TRUE;
}

/*
 * mono_global_codeman_reserve:
 *
 *  Allocate code memory from the global code manager.
 */
void *mono_global_codeman_reserve (int size)
{
	void *ptr;

	if (mono_aot_only)
		g_error ("Attempting to allocate from the global code manager while running with --aot-only.\n");

	if (!global_codeman) {
		/* This can happen during startup */
		global_codeman = mono_code_manager_new ();
		return mono_code_manager_reserve (global_codeman, size);
	}
	else {
		mono_jit_lock ();
		ptr = mono_code_manager_reserve (global_codeman, size);
		mono_jit_unlock ();
		return ptr;
	}
}

#if defined(__native_client_codegen__) && defined(__native_client__)
void
mono_nacl_gc()
{
#ifdef __native_client_gc__
	__nacl_suspend_thread_if_needed();
#endif
}

/* Given the temporary buffer (allocated by mono_global_codeman_reserve) into
 * which we are generating code, return a pointer to the destination in the
 * dynamic code segment into which the code will be copied when
 * mono_global_codeman_commit is called.
 * LOCKING: Acquires the jit lock.
 */
void*
nacl_global_codeman_get_dest (void *data)
{
	void *dest;
	mono_jit_lock ();
	dest = nacl_code_manager_get_code_dest (global_codeman, data);
	mono_jit_unlock ();
	return dest;
}

void
mono_global_codeman_commit (void *data, int size, int newsize)
{
	mono_jit_lock ();
	mono_code_manager_commit (global_codeman, data, size, newsize);
	mono_jit_unlock ();
}

/*
 * Convenience function which calls mono_global_codeman_commit to validate and
 * copy the code. The caller sets *buf_base and *buf_size to the start and size
 * of the buffer (allocated by mono_global_codeman_reserve), and *code_end to
 * the byte after the last instruction byte. On return, *buf_base will point to
 * the start of the copied in the code segment, and *code_end will point after
 * the end of the copied code.
 */
void
nacl_global_codeman_validate (guint8 **buf_base, int buf_size, guint8 **code_end)
{
	guint8 *tmp = nacl_global_codeman_get_dest (*buf_base);
	mono_global_codeman_commit (*buf_base, buf_size, *code_end - *buf_base);
	*code_end = tmp + (*code_end - *buf_base);
	*buf_base = tmp;
}
#else
/* no-op versions of Native Client functions */
void*
nacl_global_codeman_get_dest (void *data)
{
	return data;
}

void
mono_global_codeman_commit (void *data, int size, int newsize)
{
}

void
nacl_global_codeman_validate (guint8 **buf_base, int buf_size, guint8 **code_end)
{
}

#endif /* __native_client__ */

/**
 * mono_create_unwind_op:
 *
 *   Create an unwind op with the given parameters.
 */
MonoUnwindOp*
mono_create_unwind_op (int when, int tag, int reg, int val)
{
	MonoUnwindOp *op = g_new0 (MonoUnwindOp, 1);

	op->op = tag;
	op->reg = reg;
	op->val = val;
	op->when = when;

	return op;
}

MonoJumpInfoToken *
mono_jump_info_token_new2 (MonoMemPool *mp, MonoImage *image, guint32 token, MonoGenericContext *context)
{
	MonoJumpInfoToken *res = (MonoJumpInfoToken *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoToken));
	res->image = image;
	res->token = token;
	res->has_context = context != NULL;
	if (context)
		memcpy (&res->context, context, sizeof (MonoGenericContext));

	return res;
}

MonoJumpInfoToken *
mono_jump_info_token_new (MonoMemPool *mp, MonoImage *image, guint32 token)
{
	return mono_jump_info_token_new2 (mp, image, token, NULL);
}

/*
 * mono_tramp_info_create:
 *
 *   Create a MonoTrampInfo structure from the arguments. This function assumes ownership
 * of JI, and UNWIND_OPS.
 */
MonoTrampInfo*
mono_tramp_info_create (const char *name, guint8 *code, guint32 code_size, MonoJumpInfo *ji, GSList *unwind_ops)
{
	MonoTrampInfo *info = g_new0 (MonoTrampInfo, 1);

	info->name = g_strdup ((char*)name);
	info->code = code;
	info->code_size = code_size;
	info->ji = ji;
	info->unwind_ops = unwind_ops;

	return info;
}

void
mono_tramp_info_free (MonoTrampInfo *info)
{
	g_free (info->name);

	// FIXME: ji
	mono_free_unwind_info (info->unwind_ops);
	g_free (info);
}

static void
register_trampoline_jit_info (MonoDomain *domain, MonoTrampInfo *info)
{
	MonoJitInfo *ji;

	ji = (MonoJitInfo *)mono_domain_alloc0 (domain, mono_jit_info_size ((MonoJitInfoFlags)0, 0, 0));
	mono_jit_info_init (ji, NULL, info->code, info->code_size, (MonoJitInfoFlags)0, 0, 0);
	ji->d.tramp_info = info;
	ji->is_trampoline = TRUE;

	ji->unwind_info = mono_cache_unwind_info (info->uw_info, info->uw_info_len);

	mono_jit_info_table_add (domain, ji);
}

/*
 * mono_tramp_info_register:
 *
 * Remember INFO for use by xdebug, mono_print_method_from_ip (), jit maps, etc.
 * INFO can be NULL.
 * Frees INFO.
 */
void
mono_tramp_info_register (MonoTrampInfo *info, MonoDomain *domain)
{
	MonoTrampInfo *copy;

	if (!info)
		return;

	if (!domain)
		domain = mono_get_root_domain ();

	copy = g_new0 (MonoTrampInfo, 1);
	copy->code = info->code;
	copy->code_size = info->code_size;
	copy->name = g_strdup (info->name);

	if (info->unwind_ops) {
		copy->uw_info = mono_unwind_ops_encode (info->unwind_ops, &copy->uw_info_len);
	} else {
		/* Trampolines from aot have the unwind ops already encoded */
		copy->uw_info = info->uw_info;
		copy->uw_info_len = info->uw_info_len;
	}

	mono_jit_lock ();
	tramp_infos = g_slist_prepend (tramp_infos, copy);
	mono_jit_unlock ();

	mono_save_trampoline_xdebug_info (info);

	/* Only register trampolines that have unwind infos */
	if (mono_get_root_domain () && copy->uw_info)
		register_trampoline_jit_info (domain, copy);

	if (mono_jit_map_is_enabled ())
		mono_emit_jit_tramp (info->code, info->code_size, info->name);

	mono_tramp_info_free (info);
}

static void
mono_tramp_info_cleanup (void)
{
	GSList *l;

	for (l = tramp_infos; l; l = l->next) {
		MonoTrampInfo *info = (MonoTrampInfo *)l->data;

		mono_tramp_info_free (info);
	}
	g_slist_free (tramp_infos);
}

/* Register trampolines created before the root domain was created in the jit info tables */
static void
register_trampolines (MonoDomain *domain)
{
	GSList *l;

	for (l = tramp_infos; l; l = l->next) {
		MonoTrampInfo *info = (MonoTrampInfo *)l->data;

		register_trampoline_jit_info (domain, info);
	}
}

G_GNUC_UNUSED static void
break_count (void)
{
}

/*
 * Runtime debugging tool, use if (debug_count ()) <x> else <y> to do <x> the first COUNT times, then do <y> afterwards.
 * Set a breakpoint in break_count () to break the last time <x> is done.
 */
G_GNUC_UNUSED gboolean
mono_debug_count (void)
{
	static int count = 0;
	static gboolean inited;
	static const char *value;

	count ++;

	if (!inited) {
		value = g_getenv ("COUNT");
		inited = TRUE;
	}

	if (!value)
		return TRUE;

	if (count == atoi (value))
		break_count ();

	if (count > atoi (value))
		return FALSE;

	return TRUE;
}

gconstpointer
mono_icall_get_wrapper_full (MonoJitICallInfo* callinfo, gboolean do_compile)
{
	char *name;
	MonoMethod *wrapper;
	gconstpointer trampoline;
	MonoDomain *domain = mono_get_root_domain ();
	gboolean check_exc = TRUE;

	if (callinfo->wrapper)
		return callinfo->wrapper;

	if (callinfo->trampoline)
		return callinfo->trampoline;

	if (!strcmp (callinfo->name, "mono_thread_interruption_checkpoint"))
		/* This icall is used to check for exceptions, so don't check in the wrapper */
		check_exc = FALSE;

	name = g_strdup_printf ("__icall_wrapper_%s", callinfo->name);
	wrapper = mono_marshal_get_icall_wrapper (callinfo->sig, name, callinfo->func, check_exc);
	g_free (name);

	if (do_compile) {
		trampoline = mono_compile_method (wrapper);
	} else {
		MonoError error;

		trampoline = mono_create_jit_trampoline (domain, wrapper, &error);
		mono_error_assert_ok (&error);
		trampoline = mono_create_ftnptr (domain, (gpointer)trampoline);
	}

	mono_loader_lock ();
	if (!callinfo->trampoline) {
		mono_register_jit_icall_wrapper (callinfo, trampoline);
		callinfo->trampoline = trampoline;
	}
	mono_loader_unlock ();

	return callinfo->trampoline;
}

gconstpointer
mono_icall_get_wrapper (MonoJitICallInfo* callinfo)
{
	return mono_icall_get_wrapper_full (callinfo, FALSE);
}

static MonoJitDynamicMethodInfo*
mono_dynamic_code_hash_lookup (MonoDomain *domain, MonoMethod *method)
{
	MonoJitDynamicMethodInfo *res;

	if (domain_jit_info (domain)->dynamic_code_hash)
		res = (MonoJitDynamicMethodInfo *)g_hash_table_lookup (domain_jit_info (domain)->dynamic_code_hash, method);
	else
		res = NULL;
	return res;
}

static void
register_opcode_emulation (int opcode, const char *name, const char *sigstr, gpointer func, const char *symbol, gboolean no_throw)
{
#ifndef DISABLE_JIT
	mini_register_opcode_emulation (opcode, name, sigstr, func, symbol, no_throw);
#else
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	g_assert (!sig->hasthis);
	g_assert (sig->param_count < 3);

	/* Opcode emulation functions are assumed to don't call mono_raise_exception () */
	mono_register_jit_icall_full (func, name, sig, no_throw, TRUE, symbol);
#endif
}

/*
 * For JIT icalls implemented in C.
 * NAME should be the same as the name of the C function whose address is FUNC.
 * If @avoid_wrapper is TRUE, no wrapper is generated. This is for perf critical icalls which
 * can't throw exceptions.
 */
static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean avoid_wrapper)
{
	MonoMethodSignature *sig;

	if (sigstr)
		sig = mono_create_icall_signature (sigstr);
	else
		sig = NULL;

	mono_register_jit_icall_full (func, name, sig, avoid_wrapper, FALSE, avoid_wrapper ? name : NULL);
}

static void
register_icall_no_wrapper (gpointer func, const char *name, const char *sigstr)
{
	MonoMethodSignature *sig;

	if (sigstr)
		sig = mono_create_icall_signature (sigstr);
	else
		sig = NULL;

	mono_register_jit_icall_full (func, name, sig, TRUE, FALSE, name);
}

static void
register_icall_with_wrapper (gpointer func, const char *name, const char *sigstr)
{
	MonoMethodSignature *sig;

	if (sigstr)
		sig = mono_create_icall_signature (sigstr);
	else
		sig = NULL;

	mono_register_jit_icall_full (func, name, sig, FALSE, FALSE, NULL);
}

static void
register_dyn_icall (gpointer func, const char *name, const char *sigstr, gboolean save)
{
	MonoMethodSignature *sig;

	if (sigstr)
		sig = mono_create_icall_signature (sigstr);
	else
		sig = NULL;

	mono_register_jit_icall (func, name, sig, save);
}

#ifdef MONO_HAVE_FAST_TLS
MONO_FAST_TLS_DECLARE(mono_lmf_addr);
#ifdef MONO_ARCH_ENABLE_MONO_LMF_VAR
/*
 * When this is defined, the current lmf is stored in this tls variable instead of in
 * jit_tls->lmf.
 */
MONO_FAST_TLS_DECLARE(mono_lmf);
#endif
#endif

gint32
mono_get_jit_tls_offset (void)
{
	int offset;

#ifdef HOST_WIN32
	if (mono_jit_tls_id)
		offset = mono_jit_tls_id;
	else
		/* FIXME: Happens during startup */
		offset = -1;
#else
	MONO_THREAD_VAR_OFFSET (mono_jit_tls, offset);
#endif
	return offset;
}

gint32
mono_get_lmf_tls_offset (void)
{
#if defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	int offset;
	MONO_THREAD_VAR_OFFSET(mono_lmf,offset);
	return offset;
#else
	return -1;
#endif
}

gint32
mono_get_lmf_addr_tls_offset (void)
{
	int offset;
	MONO_THREAD_VAR_OFFSET(mono_lmf_addr,offset);
	return offset;
}

MonoLMF *
mono_get_lmf (void)
{
#if defined(MONO_HAVE_FAST_TLS) && defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	return (MonoLMF *)MONO_FAST_TLS_GET (mono_lmf);
#else
	MonoJitTlsData *jit_tls;

	if ((jit_tls = mono_native_tls_get_value (mono_jit_tls_id)))
		return jit_tls->lmf;
	/*
	 * We do not assert here because this function can be called from
	 * mini-gc.c on a thread that has not executed any managed code, yet
	 * (the thread object allocation can trigger a collection).
	 */
	return NULL;
#endif
}

MonoLMF **
mono_get_lmf_addr (void)
{
#ifdef MONO_HAVE_FAST_TLS
	return (MonoLMF **)MONO_FAST_TLS_GET (mono_lmf_addr);
#else
	MonoJitTlsData *jit_tls;

	jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	if (G_LIKELY (jit_tls))
		return &jit_tls->lmf;

	/*
	 * When resolving the call to mono_jit_thread_attach full-aot will look
	 * in the plt, which causes a call into the generic trampoline, which in turn
	 * tries to resolve the lmf_addr creating a cyclic dependency.  We cannot
	 * call mono_jit_thread_attach from the native-to-managed wrapper, without
	 * mono_get_lmf_addr, and mono_get_lmf_addr requires the thread to be attached.
	 */

	mono_thread_attach (mono_get_root_domain ());
	mono_thread_set_state (mono_thread_internal_current (), ThreadState_Background);

	if ((jit_tls = mono_native_tls_get_value (mono_jit_tls_id)))
		return &jit_tls->lmf;

	g_assert_not_reached ();
	return NULL;
#endif
}

void
mono_set_lmf (MonoLMF *lmf)
{
#if defined(MONO_HAVE_FAST_TLS) && defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	MONO_FAST_TLS_SET (mono_lmf, lmf);
#endif

	(*mono_get_lmf_addr ()) = lmf;
}

MonoJitTlsData*
mono_get_jit_tls (void)
{
	return (MonoJitTlsData *)mono_native_tls_get_value (mono_jit_tls_id);
}

static void
mono_set_jit_tls (MonoJitTlsData *jit_tls)
{
	MonoThreadInfo *info;

	mono_native_tls_set_value (mono_jit_tls_id, jit_tls);

#ifdef MONO_HAVE_FAST_TLS
	MONO_FAST_TLS_SET (mono_jit_tls, jit_tls);
#endif

	/* Save it into MonoThreadInfo so it can be accessed by mono_thread_state_init_from_handle () */
	info = mono_thread_info_current ();
	if (info)
		mono_thread_info_tls_set (info, TLS_KEY_JIT_TLS, jit_tls);
}

static void
mono_set_lmf_addr (gpointer lmf_addr)
{
	MonoThreadInfo *info;

#ifdef MONO_HAVE_FAST_TLS
	MONO_FAST_TLS_SET (mono_lmf_addr, lmf_addr);
#endif

	/* Save it into MonoThreadInfo so it can be accessed by mono_thread_state_init_from_handle () */
	info = mono_thread_info_current ();
	if (info)
		mono_thread_info_tls_set (info, TLS_KEY_LMF_ADDR, lmf_addr);
}

/*
 * mono_jit_thread_attach: called by native->managed wrappers
 *
 * In non-coop mode:
 *  - @dummy: is NULL
 *  - @return: the original domain which needs to be restored, or NULL.
 *
 * In coop mode:
 *  - @dummy: contains the original domain
 *  - @return: a cookie containing current MonoThreadInfo* if it was in BLOCKING mode, NULL otherwise
 */
gpointer
mono_jit_thread_attach (MonoDomain *domain, gpointer *dummy)
{
	MonoDomain *orig;

	if (!domain) {
		/* Happens when called from AOTed code which is only used in the root domain. */
		domain = mono_get_root_domain ();
	}

	g_assert (domain);

	if (!mono_threads_is_coop_enabled ()) {
		gboolean attached;

#ifdef MONO_HAVE_FAST_TLS
		attached = MONO_FAST_TLS_GET (mono_lmf_addr) != NULL;
#else
		attached = mono_native_tls_get_value (mono_jit_tls_id) != NULL;
#endif

		if (!attached) {
			mono_thread_attach (domain);

			// #678164
			mono_thread_set_state (mono_thread_internal_current (), ThreadState_Background);
		}

		orig = mono_domain_get ();
		if (orig != domain)
			mono_domain_set (domain, TRUE);

		return orig != domain ? orig : NULL;
	} else {
		MonoThreadInfo *info;

		info = mono_thread_info_current_unchecked ();
		if (!info || !mono_thread_info_is_live (info)) {
			/* thread state STARTING -> RUNNING */
			mono_thread_attach (domain);

			// #678164
			mono_thread_set_state (mono_thread_internal_current (), ThreadState_Background);

			*dummy = NULL;

			/* mono_threads_reset_blocking_start returns the current MonoThreadInfo
			 * if we were in BLOCKING mode */
			return mono_thread_info_current ();
		} else {
			orig = mono_domain_get ();

			/* orig might be null if we did an attach -> detach -> attach sequence */

			if (orig != domain)
				mono_domain_set (domain, TRUE);

			*dummy = orig;

			/* thread state (BLOCKING|RUNNING) -> RUNNING */
			return mono_threads_reset_blocking_start (dummy);
		}
	}
}

/*
 * mono_jit_thread_detach: called by native->managed wrappers
 *
 * In non-coop mode:
 *  - @cookie: the original domain which needs to be restored, or NULL.
 *  - @dummy: is NULL
 *
 * In coop mode:
 *  - @cookie: contains current MonoThreadInfo* if it was in BLOCKING mode, NULL otherwise
 *  - @dummy: contains the original domain
 */
void
mono_jit_thread_detach (gpointer cookie, gpointer *dummy)
{
	MonoDomain *domain, *orig;

	if (!mono_threads_is_coop_enabled ()) {
		orig = (MonoDomain*) cookie;

		if (orig)
			mono_domain_set (orig, TRUE);
	} else {
		orig = (MonoDomain*) *dummy;

		domain = mono_domain_get ();
		g_assert (domain);

		/* it won't do anything if cookie is NULL
		 * thread state RUNNING -> (RUNNING|BLOCKING) */
		mono_threads_reset_blocking_end (cookie, dummy);

		if (orig != domain) {
			if (!orig)
				mono_domain_unset ();
			else
				mono_domain_set (orig, TRUE);
		}
	}
}

/**
 * mono_thread_abort:
 * @obj: exception object
 *
 * abort the thread, print exception information and stack trace
 */
static void
mono_thread_abort (MonoObject *obj)
{
	/* MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id); */

	/* handle_remove should be eventually called for this thread, too
	g_free (jit_tls);*/

	if ((mono_runtime_unhandled_exception_policy_get () == MONO_UNHANDLED_POLICY_LEGACY) ||
			(obj->vtable->klass == mono_defaults.threadabortexception_class)) {
		mono_thread_exit ();
	} else {
		mono_invoke_unhandled_exception_hook (obj);
	}
}

static void*
setup_jit_tls_data (gpointer stack_start, gpointer abort_func)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	jit_tls = (MonoJitTlsData *)mono_native_tls_get_value (mono_jit_tls_id);
	if (jit_tls)
		return jit_tls;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	jit_tls->abort_func = (void (*)(MonoObject *))abort_func;
	jit_tls->end_of_stack = stack_start;

	mono_set_jit_tls (jit_tls);

	lmf = g_new0 (MonoLMF, 1);
	MONO_ARCH_INIT_TOP_LMF_ENTRY (lmf);

	jit_tls->first_lmf = lmf;

#if defined(MONO_HAVE_FAST_TLS) && defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	/* jit_tls->lmf is unused */
	MONO_FAST_TLS_SET (mono_lmf, lmf);
	mono_set_lmf_addr (MONO_FAST_TLS_ADDR (mono_lmf));
#else
	mono_set_lmf_addr (&jit_tls->lmf);

	jit_tls->lmf = lmf;
#endif

#ifdef MONO_ARCH_HAVE_TLS_INIT
	mono_arch_tls_init ();
#endif

	mono_setup_altstack (jit_tls);

	return jit_tls;
}

static void
free_jit_tls_data (MonoJitTlsData *jit_tls)
{
	mono_arch_free_jit_tls_data (jit_tls);
	mono_free_altstack (jit_tls);

	g_free (jit_tls->first_lmf);
	g_free (jit_tls);
}

static void
mono_thread_start_cb (intptr_t tid, gpointer stack_start, gpointer func)
{
	MonoThreadInfo *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort);
	thread = mono_thread_info_current_unchecked ();
	if (thread)
		thread->jit_data = jit_tls;

	mono_arch_cpu_init ();
}

void (*mono_thread_attach_aborted_cb ) (MonoObject *obj) = NULL;

static void
mono_thread_abort_dummy (MonoObject *obj)
{
  if (mono_thread_attach_aborted_cb)
    mono_thread_attach_aborted_cb (obj);
  else
    mono_thread_abort (obj);
}

static void
mono_thread_attach_cb (intptr_t tid, gpointer stack_start)
{
	MonoThreadInfo *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort_dummy);
	thread = mono_thread_info_current_unchecked ();
	if (thread)
		thread->jit_data = jit_tls;
	if (mono_profiler_get_events () & MONO_PROFILE_STATISTICAL)
		mono_runtime_setup_stat_profiler ();

	mono_arch_cpu_init ();
}

static void
mini_thread_cleanup (MonoNativeThreadId tid)
{
	MonoJitTlsData *jit_tls = NULL;
	MonoThreadInfo *info;

	info = mono_thread_info_current_unchecked ();

	/* We can't clean up tls information if we are on another thread, it will clean up the wrong stuff
	 * It would be nice to issue a warning when this happens outside of the shutdown sequence. but it's
	 * not a trivial thing.
	 *
	 * The current offender is mono_thread_manage which cleanup threads from the outside.
	 */
	if (info && mono_thread_info_get_tid (info) == tid) {
		jit_tls = (MonoJitTlsData *)info->jit_data;
		info->jit_data = NULL;

		mono_set_jit_tls (NULL);

		/* If we attach a thread but never call into managed land, we might never get an lmf.*/
		if (mono_get_lmf ()) {
			mono_set_lmf (NULL);
			mono_set_lmf_addr (NULL);
		}
	} else {
		info = mono_thread_info_lookup (tid);
		if (info) {
			jit_tls = (MonoJitTlsData *)info->jit_data;
			info->jit_data = NULL;
		}
		mono_hazard_pointer_clear (mono_hazard_pointer_get (), 1);
	}

	if (jit_tls)
		free_jit_tls_data (jit_tls);
}

int
mini_get_tls_offset (MonoTlsKey key)
{
	int offset;
	g_assert (MONO_ARCH_HAVE_TLS_GET);

	switch (key) {
	case TLS_KEY_THREAD:
		offset = mono_thread_get_tls_offset ();
		break;
	case TLS_KEY_JIT_TLS:
		offset = mono_get_jit_tls_offset ();
		break;
	case TLS_KEY_DOMAIN:
		offset = mono_domain_get_tls_offset ();
		break;
	case TLS_KEY_LMF:
		offset = mono_get_lmf_tls_offset ();
		break;
	case TLS_KEY_LMF_ADDR:
		offset = mono_get_lmf_addr_tls_offset ();
		break;
	default:
		offset = mono_tls_key_get_offset (key);
		g_assert (offset != -1);
		break;
	}

	return offset;
}

static gboolean
mini_tls_key_supported (MonoTlsKey key)
{
	if (!MONO_ARCH_HAVE_TLS_GET)
		return FALSE;

	return mini_get_tls_offset (key) != -1;
}

MonoJumpInfo *
mono_patch_info_list_prepend (MonoJumpInfo *list, int ip, MonoJumpInfoType type, gconstpointer target)
{
	MonoJumpInfo *ji = g_new0 (MonoJumpInfo, 1);

	ji->ip.i = ip;
	ji->type = type;
	ji->data.target = target;
	ji->next = list;

	return ji;
}

/**
 * mono_patch_info_dup_mp:
 *
 * Make a copy of PATCH_INFO, allocating memory from the mempool MP.
 */
MonoJumpInfo*
mono_patch_info_dup_mp (MonoMemPool *mp, MonoJumpInfo *patch_info)
{
	MonoJumpInfo *res = (MonoJumpInfo *)mono_mempool_alloc (mp, sizeof (MonoJumpInfo));
	memcpy (res, patch_info, sizeof (MonoJumpInfo));

	switch (patch_info->type) {
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_DECLSEC:
		res->data.token = (MonoJumpInfoToken *)mono_mempool_alloc (mp, sizeof (MonoJumpInfoToken));
		memcpy (res->data.token, patch_info->data.token, sizeof (MonoJumpInfoToken));
		break;
	case MONO_PATCH_INFO_SWITCH:
		res->data.table = (MonoJumpInfoBBTable *)mono_mempool_alloc (mp, sizeof (MonoJumpInfoBBTable));
		memcpy (res->data.table, patch_info->data.table, sizeof (MonoJumpInfoBBTable));
		res->data.table->table = (MonoBasicBlock **)mono_mempool_alloc (mp, sizeof (MonoBasicBlock*) * patch_info->data.table->table_size);
		memcpy (res->data.table->table, patch_info->data.table->table, sizeof (MonoBasicBlock*) * patch_info->data.table->table_size);
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_RGCTX_SLOT_INDEX:
		res->data.rgctx_entry = (MonoJumpInfoRgctxEntry *)mono_mempool_alloc (mp, sizeof (MonoJumpInfoRgctxEntry));
		memcpy (res->data.rgctx_entry, patch_info->data.rgctx_entry, sizeof (MonoJumpInfoRgctxEntry));
		res->data.rgctx_entry->data = mono_patch_info_dup_mp (mp, res->data.rgctx_entry->data);
		break;
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		res->data.del_tramp = (MonoDelegateClassMethodPair *)mono_mempool_alloc0 (mp, sizeof (MonoDelegateClassMethodPair));
		memcpy (res->data.del_tramp, patch_info->data.del_tramp, sizeof (MonoDelegateClassMethodPair));
		break;
	case MONO_PATCH_INFO_GSHAREDVT_CALL:
		res->data.gsharedvt = (MonoJumpInfoGSharedVtCall *)mono_mempool_alloc (mp, sizeof (MonoJumpInfoGSharedVtCall));
		memcpy (res->data.gsharedvt, patch_info->data.gsharedvt, sizeof (MonoJumpInfoGSharedVtCall));
		break;
	case MONO_PATCH_INFO_GSHAREDVT_METHOD: {
		MonoGSharedVtMethodInfo *info;
		MonoGSharedVtMethodInfo *oinfo;
		int i;

		oinfo = patch_info->data.gsharedvt_method;
		info = (MonoGSharedVtMethodInfo *)mono_mempool_alloc (mp, sizeof (MonoGSharedVtMethodInfo));
		res->data.gsharedvt_method = info;
		memcpy (info, oinfo, sizeof (MonoGSharedVtMethodInfo));
		info->entries = (MonoRuntimeGenericContextInfoTemplate *)mono_mempool_alloc (mp, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->count_entries);
		for (i = 0; i < oinfo->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *otemplate = &oinfo->entries [i];
			MonoRuntimeGenericContextInfoTemplate *template_ = &info->entries [i];

			memcpy (template_, otemplate, sizeof (MonoRuntimeGenericContextInfoTemplate));
		}
		//info->locals_types = mono_mempool_alloc0 (mp, info->nlocals * sizeof (MonoType*));
		//memcpy (info->locals_types, oinfo->locals_types, info->nlocals * sizeof (MonoType*));
		break;
	}
	case MONO_PATCH_INFO_VIRT_METHOD: {
		MonoJumpInfoVirtMethod *info;
		MonoJumpInfoVirtMethod *oinfo;

		oinfo = patch_info->data.virt_method;
		info = (MonoJumpInfoVirtMethod *)mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoVirtMethod));
		res->data.virt_method = info;
		memcpy (info, oinfo, sizeof (MonoJumpInfoVirtMethod));
		break;
	}
	default:
		break;
	}

	return res;
}

guint
mono_patch_info_hash (gconstpointer data)
{
	const MonoJumpInfo *ji = (MonoJumpInfo*)data;

	switch (ji->type) {
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_DECLSEC:
		return (ji->type << 8) | ji->data.token->token;
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		return (ji->type << 8) | ji->data.token->token | (ji->data.token->has_context ? (gsize)ji->data.token->context.class_inst : 0);
	case MONO_PATCH_INFO_INTERNAL_METHOD:
		return (ji->type << 8) | g_str_hash (ji->data.name);
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_ICALL_ADDR:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_SFLDA:
	case MONO_PATCH_INFO_SEQ_POINT_INFO:
	case MONO_PATCH_INFO_METHOD_RGCTX:
	case MONO_PATCH_INFO_SIGNATURE:
	case MONO_PATCH_INFO_TLS_OFFSET:
	case MONO_PATCH_INFO_METHOD_CODE_SLOT:
	case MONO_PATCH_INFO_AOT_JIT_INFO:
		return (ji->type << 8) | (gssize)ji->data.target;
	case MONO_PATCH_INFO_GSHAREDVT_CALL:
		return (ji->type << 8) | (gssize)ji->data.gsharedvt->method;
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_RGCTX_SLOT_INDEX: {
		MonoJumpInfoRgctxEntry *e = ji->data.rgctx_entry;

		return (ji->type << 8) | (gssize)e->method | (e->in_mrgctx) | e->info_type | mono_patch_info_hash (e->data);
	}
	case MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG:
	case MONO_PATCH_INFO_MSCORLIB_GOT_ADDR:
	case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR:
	case MONO_PATCH_INFO_GC_NURSERY_START:
	case MONO_PATCH_INFO_GC_NURSERY_BITS:
	case MONO_PATCH_INFO_JIT_TLS_ID:
	case MONO_PATCH_INFO_GOT_OFFSET:
	case MONO_PATCH_INFO_GC_SAFE_POINT_FLAG:
	case MONO_PATCH_INFO_AOT_MODULE:
		return (ji->type << 8);
	case MONO_PATCH_INFO_CASTCLASS_CACHE:
		return (ji->type << 8) | (ji->data.index);
	case MONO_PATCH_INFO_SWITCH:
		return (ji->type << 8) | ji->data.table->table_size;
	case MONO_PATCH_INFO_GSHAREDVT_METHOD:
		return (ji->type << 8) | (gssize)ji->data.gsharedvt_method->method;
	case MONO_PATCH_INFO_OBJC_SELECTOR_REF:
		/* Hash on the selector name */
		return g_str_hash (ji->data.target);
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		return (ji->type << 8) | (gsize)ji->data.del_tramp->klass | (gsize)ji->data.del_tramp->method | (gsize)ji->data.del_tramp->is_virtual;
	case MONO_PATCH_INFO_LDSTR_LIT:
		return g_str_hash (ji->data.target);
	case MONO_PATCH_INFO_VIRT_METHOD: {
		MonoJumpInfoVirtMethod *info = ji->data.virt_method;

		return (ji->type << 8) | (gssize)info->klass | (gssize)info->method;
	}
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
		return (ji->type << 8) | g_str_hash (ji->data.target);
	case MONO_PATCH_INFO_GSHAREDVT_IN_WRAPPER:
		return (ji->type << 8) | mono_signature_hash (ji->data.sig);
	default:
		printf ("info type: %d\n", ji->type);
		mono_print_ji (ji); printf ("\n");
		g_assert_not_reached ();
		return 0;
	}
}

/*
 * mono_patch_info_equal:
 *
 * This might fail to recognize equivalent patches, i.e. floats, so its only
 * usable in those cases where this is not a problem, i.e. sharing GOT slots
 * in AOT.
 */
gint
mono_patch_info_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoJumpInfo *ji1 = (MonoJumpInfo*)ka;
	const MonoJumpInfo *ji2 = (MonoJumpInfo*)kb;

	if (ji1->type != ji2->type)
		return 0;

	switch (ji1->type) {
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_DECLSEC:
		if ((ji1->data.token->image != ji2->data.token->image) ||
			(ji1->data.token->token != ji2->data.token->token) ||
			(ji1->data.token->has_context != ji2->data.token->has_context) ||
			(ji1->data.token->context.class_inst != ji2->data.token->context.class_inst) ||
			(ji1->data.token->context.method_inst != ji2->data.token->context.method_inst))
			return 0;
		break;
	case MONO_PATCH_INFO_INTERNAL_METHOD:
		return g_str_equal (ji1->data.name, ji2->data.name);
	case MONO_PATCH_INFO_RGCTX_FETCH:
	case MONO_PATCH_INFO_RGCTX_SLOT_INDEX: {
		MonoJumpInfoRgctxEntry *e1 = ji1->data.rgctx_entry;
		MonoJumpInfoRgctxEntry *e2 = ji2->data.rgctx_entry;

		return e1->method == e2->method && e1->in_mrgctx == e2->in_mrgctx && e1->info_type == e2->info_type && mono_patch_info_equal (e1->data, e2->data);
	}
	case MONO_PATCH_INFO_GSHAREDVT_CALL: {
		MonoJumpInfoGSharedVtCall *c1 = ji1->data.gsharedvt;
		MonoJumpInfoGSharedVtCall *c2 = ji2->data.gsharedvt;

		return c1->sig == c2->sig && c1->method == c2->method;
	}
	case MONO_PATCH_INFO_GSHAREDVT_METHOD:
		return ji1->data.gsharedvt_method->method == ji2->data.gsharedvt_method->method;
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		return ji1->data.del_tramp->klass == ji2->data.del_tramp->klass && ji1->data.del_tramp->method == ji2->data.del_tramp->method && ji1->data.del_tramp->is_virtual == ji2->data.del_tramp->is_virtual;
	case MONO_PATCH_INFO_CASTCLASS_CACHE:
		return ji1->data.index == ji2->data.index;
	case MONO_PATCH_INFO_VIRT_METHOD:
		return ji1->data.virt_method->klass == ji2->data.virt_method->klass && ji1->data.virt_method->method == ji2->data.virt_method->method;
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
		if (ji1->data.target == ji2->data.target)
			return 1;
		return strcmp (ji1->data.target, ji2->data.target) == 0 ? 1 : 0;
	case MONO_PATCH_INFO_GSHAREDVT_IN_WRAPPER:
		return mono_metadata_signature_equal (ji1->data.sig, ji2->data.sig) ? 1 : 0;
	default:
		if (ji1->data.target != ji2->data.target)
			return 0;
		break;
	}

	return 1;
}

gpointer
mono_resolve_patch_target (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors, MonoError *error)
{
	unsigned char *ip = patch_info->ip.i + code;
	gconstpointer target = NULL;

	mono_error_init (error);

	switch (patch_info->type) {
	case MONO_PATCH_INFO_BB:
		/*
		 * FIXME: This could be hit for methods without a prolog. Should use -1
		 * but too much code depends on a 0 initial value.
		 */
		//g_assert (patch_info->data.bb->native_offset);
		target = patch_info->data.bb->native_offset + code;
		break;
	case MONO_PATCH_INFO_ABS:
		target = patch_info->data.target;
		break;
	case MONO_PATCH_INFO_LABEL:
		target = patch_info->data.inst->inst_c0 + code;
		break;
	case MONO_PATCH_INFO_IP:
#if defined(__native_client__) && defined(__native_client_codegen__)
		/* Need to transform to the destination address, it's */
		/* emitted as an immediate in the code. */
		target = nacl_inverse_modify_patch_target(ip);
#else
		target = ip;
#endif
		break;
	case MONO_PATCH_INFO_METHOD_REL:
		target = code + patch_info->data.offset;
		break;
	case MONO_PATCH_INFO_INTERNAL_METHOD: {
		MonoJitICallInfo *mi = mono_find_jit_icall_by_name (patch_info->data.name);
		if (!mi) {
			g_warning ("unknown MONO_PATCH_INFO_INTERNAL_METHOD %s", patch_info->data.name);
			g_assert_not_reached ();
		}
		target = mono_icall_get_wrapper (mi);
		break;
	}
	case MONO_PATCH_INFO_JIT_ICALL_ADDR: {
		MonoJitICallInfo *mi = mono_find_jit_icall_by_name (patch_info->data.name);
		if (!mi) {
			g_warning ("unknown MONO_PATCH_INFO_JIT_ICALL_ADDR %s", patch_info->data.name);
			g_assert_not_reached ();
		}
		target = mi->func;
		break;
	}
	case MONO_PATCH_INFO_METHOD_JUMP:
		target = mono_create_jump_trampoline (domain, patch_info->data.method, FALSE, error);
		if (!mono_error_ok (error))
			return NULL;
#if defined(__native_client__) && defined(__native_client_codegen__)
# if defined(TARGET_AMD64)
		/* This target is an absolute address, not relative to the */
		/* current code being emitted on AMD64. */
		target = nacl_inverse_modify_patch_target(target);
# endif
#endif
		break;
	case MONO_PATCH_INFO_METHOD:
#if defined(__native_client_codegen__) && defined(USE_JUMP_TABLES)
		/*
		 * If we use jumptables, for recursive calls we cannot
		 * avoid trampoline, as we not yet know where we will
		 * be installed.
		 */
		target = mono_create_jit_trampoline (domain, patch_info->data.method, error);
		if (!mono_error_ok (error))
			return NULL;
#else
		if (patch_info->data.method == method) {
			target = code;
		} else {
			/* get the trampoline to the method from the domain */
			target = mono_create_jit_trampoline (domain, patch_info->data.method, error);
			if (!mono_error_ok (error))
				return NULL;
		}
#endif
		break;
	case MONO_PATCH_INFO_METHOD_CODE_SLOT: {
		gpointer code_slot;

		mono_domain_lock (domain);
		if (!domain_jit_info (domain)->method_code_hash)
			domain_jit_info (domain)->method_code_hash = g_hash_table_new (NULL, NULL);
		code_slot = g_hash_table_lookup (domain_jit_info (domain)->method_code_hash, patch_info->data.method);
		if (!code_slot) {
			code_slot = mono_domain_alloc0 (domain, sizeof (gpointer));
			g_hash_table_insert (domain_jit_info (domain)->method_code_hash, patch_info->data.method, code_slot);
		}
		mono_domain_unlock (domain);
		target = code_slot;
		break;
	}
	case MONO_PATCH_INFO_GC_SAFE_POINT_FLAG:
#if defined(__native_client_codegen__)
		target = (gpointer)&__nacl_thread_suspension_needed;
#else
		g_assert (mono_threads_is_coop_enabled ());
		target = (gpointer)&mono_polling_required;
#endif
		break;
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *jump_table;
		int i;
#if defined(__native_client__) && defined(__native_client_codegen__)
		/* This memory will leak, but we don't care if we're */
		/* not deleting JIT'd methods anyway                 */
		jump_table = g_malloc0 (sizeof(gpointer) * patch_info->data.table->table_size);
#else
		if (method && method->dynamic) {
			jump_table = (void **)mono_code_manager_reserve (mono_dynamic_code_hash_lookup (domain, method)->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
		} else {
			if (mono_aot_only) {
				jump_table = (void **)mono_domain_alloc (domain, sizeof (gpointer) * patch_info->data.table->table_size);
			} else {
				jump_table = (void **)mono_domain_code_reserve (domain, sizeof (gpointer) * patch_info->data.table->table_size);
			}
		}
#endif

		for (i = 0; i < patch_info->data.table->table_size; i++) {
#if defined(__native_client__) && defined(__native_client_codegen__)
			/* 'code' is relative to the current code blob, we */
			/* need to do this transform on it to make the     */
			/* pointers in this table absolute                 */
			jump_table [i] = nacl_inverse_modify_patch_target (code) + GPOINTER_TO_INT (patch_info->data.table->table [i]);
#else
			jump_table [i] = code + GPOINTER_TO_INT (patch_info->data.table->table [i]);
#endif
		}

#if defined(__native_client__) && defined(__native_client_codegen__)
		/* jump_table is in the data section, we need to transform */
		/* it here so when it gets modified in amd64_patch it will */
		/* then point back to the absolute data address            */
		target = nacl_inverse_modify_patch_target (jump_table);
#else
		target = jump_table;
#endif
		break;
	}
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_SIGNATURE:
	case MONO_PATCH_INFO_AOT_MODULE:
		target = patch_info->data.target;
		break;
	case MONO_PATCH_INFO_IID:
		mono_class_init (patch_info->data.klass);
		target = GINT_TO_POINTER ((int)patch_info->data.klass->interface_id);
		break;
	case MONO_PATCH_INFO_ADJUSTED_IID:
		mono_class_init (patch_info->data.klass);
		target = GINT_TO_POINTER ((int)(-((patch_info->data.klass->interface_id + 1) * SIZEOF_VOID_P)));
		break;
	case MONO_PATCH_INFO_VTABLE:
		target = mono_class_vtable (domain, patch_info->data.klass);
		g_assert (target);
		break;
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE: {
		MonoDelegateClassMethodPair *del_tramp = patch_info->data.del_tramp;

		if (del_tramp->is_virtual)
			target = mono_create_delegate_virtual_trampoline (domain, del_tramp->klass, del_tramp->method);
		else
			target = mono_create_delegate_trampoline_info (domain, del_tramp->klass, del_tramp->method);
		break;
	}
	case MONO_PATCH_INFO_SFLDA: {
		MonoVTable *vtable = mono_class_vtable (domain, patch_info->data.field->parent);

		if (mono_class_field_is_special_static (patch_info->data.field)) {
			gpointer addr = NULL;

			mono_domain_lock (domain);
			if (domain->special_static_fields)
				addr = g_hash_table_lookup (domain->special_static_fields, patch_info->data.field);
			mono_domain_unlock (domain);
			g_assert (addr);
			return addr;
		}

		g_assert (vtable);
		if (!vtable->initialized && !(vtable->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) && (method && mono_class_needs_cctor_run (vtable->klass, method)))
			/* Done by the generated code */
			;
		else {
			if (run_cctors) {
				if (!mono_runtime_class_init_full (vtable, error)) {
					return NULL;
				}
			}
		}
		target = (char*)mono_vtable_get_static_field_data (vtable) + patch_info->data.field->offset;
		break;
	}
	case MONO_PATCH_INFO_RVA: {
		guint32 field_index = mono_metadata_token_index (patch_info->data.token->token);
		guint32 rva;

		mono_metadata_field_info (patch_info->data.token->image, field_index - 1, NULL, &rva, NULL);
		target = mono_image_rva_map (patch_info->data.token->image, rva);
		break;
	}
	case MONO_PATCH_INFO_R4:
	case MONO_PATCH_INFO_R8:
		target = patch_info->data.target;
		break;
	case MONO_PATCH_INFO_EXC_NAME:
		target = patch_info->data.name;
		break;
	case MONO_PATCH_INFO_LDSTR:
		target =
			mono_ldstr (domain, patch_info->data.token->image,
						mono_metadata_token_index (patch_info->data.token->token));
		break;
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE: {
		gpointer handle;
		MonoClass *handle_class;

		handle = mono_ldtoken_checked (patch_info->data.token->image,
							   patch_info->data.token->token, &handle_class, patch_info->data.token->has_context ? &patch_info->data.token->context : NULL, error);
		if (!mono_error_ok (error))
			g_error ("Could not patch ldtoken due to %s", mono_error_get_message (error));
		mono_class_init (handle_class);
		mono_class_init (mono_class_from_mono_type ((MonoType *)handle));

		target = mono_type_get_object_checked (domain, (MonoType *)handle, error);
		if (!mono_error_ok (error))
			return NULL;
		break;
	}
	case MONO_PATCH_INFO_LDTOKEN: {
		gpointer handle;
		MonoClass *handle_class;

		handle = mono_ldtoken_checked (patch_info->data.token->image,
							   patch_info->data.token->token, &handle_class, patch_info->data.token->has_context ? &patch_info->data.token->context : NULL, error);
 		if (!mono_error_ok (error))
			g_error ("Could not patch ldtoken due to %s", mono_error_get_message (error));
		mono_class_init (handle_class);

		target = handle;
		break;
	}
	case MONO_PATCH_INFO_DECLSEC:
		target = (mono_metadata_blob_heap (patch_info->data.token->image, patch_info->data.token->token) + 2);
		break;
	case MONO_PATCH_INFO_ICALL_ADDR:
		/* run_cctors == 0 -> AOT */
		if (patch_info->data.method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
			const char *exc_class;
			const char *exc_arg;

			if (run_cctors) {
				target = mono_lookup_pinvoke_call (patch_info->data.method, &exc_class, &exc_arg);
				if (!target) {
					if (mono_aot_only) {
						mono_error_set_exception_instance (error, mono_exception_from_name_msg (mono_defaults.corlib, "System", exc_class, exc_arg));
						return NULL;
					}
					g_error ("Unable to resolve pinvoke method '%s' Re-run with MONO_LOG_LEVEL=debug for more information.\n", mono_method_full_name (patch_info->data.method, TRUE));
				}
			} else {
				target = NULL;
			}
		} else {
			target = mono_lookup_internal_call (patch_info->data.method);

			if (!target && run_cctors)
				g_error ("Unregistered icall '%s'\n", mono_method_full_name (patch_info->data.method, TRUE));
		}
		break;
	case MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG:
		target = mono_thread_interruption_request_flag ();
		break;
	case MONO_PATCH_INFO_METHOD_RGCTX: {
		MonoVTable *vtable = mono_class_vtable (domain, patch_info->data.method->klass);
		g_assert (vtable);

		target = mono_method_lookup_rgctx (vtable, mini_method_get_context (patch_info->data.method)->method_inst);
		break;
	}
	case MONO_PATCH_INFO_RGCTX_SLOT_INDEX: {
		int slot = mini_get_rgctx_entry_slot (patch_info->data.rgctx_entry);

		target = GINT_TO_POINTER (MONO_RGCTX_SLOT_INDEX (slot));
		break;
	}
	case MONO_PATCH_INFO_BB_OVF:
	case MONO_PATCH_INFO_EXC_OVF:
	case MONO_PATCH_INFO_GOT_OFFSET:
	case MONO_PATCH_INFO_NONE:
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH: {
		int slot = mini_get_rgctx_entry_slot (patch_info->data.rgctx_entry);

		target = mono_create_rgctx_lazy_fetch_trampoline (slot);
		break;
	}
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	case MONO_PATCH_INFO_SEQ_POINT_INFO:
		if (!run_cctors)
			/* AOT, not needed */
			target = NULL;
		else
			target = mono_arch_get_seq_point_info (domain, code);
		break;
#endif
	case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR: {
		int card_table_shift_bits;
		gpointer card_table_mask;

		target = mono_gc_get_card_table (&card_table_shift_bits, &card_table_mask);
		break;
	}
	case MONO_PATCH_INFO_GC_NURSERY_START: {
		int shift_bits;
		size_t size;

		target = mono_gc_get_nursery (&shift_bits, &size);
		break;
	}
	case MONO_PATCH_INFO_GC_NURSERY_BITS: {
		int shift_bits;
		size_t size;

		mono_gc_get_nursery (&shift_bits, &size);

		target = (gpointer)(mgreg_t)shift_bits;
		break;
	}
	case MONO_PATCH_INFO_CASTCLASS_CACHE: {
		target = mono_domain_alloc0 (domain, sizeof (gpointer));
		break;
	}
	case MONO_PATCH_INFO_JIT_TLS_ID: {
		target = (gpointer) (size_t) mono_jit_tls_id;
		break;
	}
	case MONO_PATCH_INFO_TLS_OFFSET: {
		int offset;

		offset = mini_get_tls_offset ((MonoTlsKey)GPOINTER_TO_INT (patch_info->data.target));
#ifdef MONO_ARCH_HAVE_TRANSLATE_TLS_OFFSET
		offset = mono_arch_translate_tls_offset (offset);
#endif
		target = GINT_TO_POINTER (offset);
		break;
	}
	case MONO_PATCH_INFO_OBJC_SELECTOR_REF: {
		target = NULL;
		break;
	}
	case MONO_PATCH_INFO_LDSTR_LIT: {
		int len;
		char *s;

		len = strlen ((const char *)patch_info->data.target);
		s = (char *)mono_domain_alloc0 (domain, len + 1);
		memcpy (s, patch_info->data.target, len);
		target = s;

		break;
	}
	case MONO_PATCH_INFO_GSHAREDVT_IN_WRAPPER:
		target = mini_get_gsharedvt_wrapper (TRUE, NULL, patch_info->data.sig, NULL, -1, FALSE);
		break;
	default:
		g_assert_not_reached ();
	}

	return (gpointer)target;
}

void
mini_init_gsctx (MonoDomain *domain, MonoMemPool *mp, MonoGenericContext *context, MonoGenericSharingContext *gsctx)
{
	MonoGenericInst *inst;
	int i;

	memset (gsctx, 0, sizeof (MonoGenericSharingContext));

	if (context && context->class_inst) {
		inst = context->class_inst;
		for (i = 0; i < inst->type_argc; ++i) {
			MonoType *type = inst->type_argv [i];

			if (mini_is_gsharedvt_gparam (type))
				gsctx->is_gsharedvt = TRUE;
		}
	}
	if (context && context->method_inst) {
		inst = context->method_inst;

		for (i = 0; i < inst->type_argc; ++i) {
			MonoType *type = inst->type_argv [i];

			if (mini_is_gsharedvt_gparam (type))
				gsctx->is_gsharedvt = TRUE;
		}
	}
}

/*
 * LOCKING: Acquires the jit code hash lock.
 */
MonoJitInfo*
mini_lookup_method (MonoDomain *domain, MonoMethod *method, MonoMethod *shared)
{
	MonoJitInfo *ji;
	static gboolean inited = FALSE;
	static int lookups = 0;
	static int failed_lookups = 0;

	mono_domain_jit_code_hash_lock (domain);
	ji = (MonoJitInfo *)mono_internal_hash_table_lookup (&domain->jit_code_hash, method);
	if (!ji && shared) {
		/* Try generic sharing */
		ji = (MonoJitInfo *)mono_internal_hash_table_lookup (&domain->jit_code_hash, shared);
		if (ji && !ji->has_generic_jit_info)
			ji = NULL;
		if (!inited) {
			mono_counters_register ("Shared generic lookups", MONO_COUNTER_INT|MONO_COUNTER_GENERICS, &lookups);
			mono_counters_register ("Failed shared generic lookups", MONO_COUNTER_INT|MONO_COUNTER_GENERICS, &failed_lookups);
			inited = TRUE;
		}

		++lookups;
		if (!ji)
			++failed_lookups;
	}
	mono_domain_jit_code_hash_unlock (domain);

	return ji;
}

static MonoJitInfo*
lookup_method (MonoDomain *domain, MonoMethod *method)
{
	MonoJitInfo *ji;
	MonoMethod *shared;

	ji = mini_lookup_method (domain, method, NULL);

	if (!ji) {
		if (!mono_method_is_generic_sharable (method, FALSE))
			return NULL;
		shared = mini_get_shared_method (method);
		ji = mini_lookup_method (domain, method, shared);
	}

	return ji;
}

MonoJitInfo *
mono_get_jit_info_from_method (MonoDomain *domain, MonoMethod *method)
{
	return lookup_method (domain, method);
}

#if ENABLE_JIT_MAP
static FILE* perf_map_file;

void
mono_enable_jit_map (void)
{
	if (!perf_map_file) {
		char name [64];
		g_snprintf (name, sizeof (name), "/tmp/perf-%d.map", getpid ());
		unlink (name);
		perf_map_file = fopen (name, "w");
	}
}

void
mono_emit_jit_tramp (void *start, int size, const char *desc)
{
	if (perf_map_file)
		fprintf (perf_map_file, "%llx %x %s\n", (long long unsigned int)(gsize)start, size, desc);
}

void
mono_emit_jit_map (MonoJitInfo *jinfo)
{
	if (perf_map_file) {
		char *name = mono_method_full_name (jinfo_get_method (jinfo), TRUE);
		mono_emit_jit_tramp (jinfo->code_start, jinfo->code_size, name);
		g_free (name);
	}
}

gboolean
mono_jit_map_is_enabled (void)
{
	return perf_map_file != NULL;
}

#endif

static void
no_gsharedvt_in_wrapper (void)
{
	g_assert_not_reached ();
}

static gpointer
mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt, MonoError *error)
{
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoJitInfo *info;
	gpointer code = NULL, p;
	MonoJitInfo *ji;
	MonoJitICallInfo *callinfo = NULL;
	WrapperInfo *winfo = NULL;

	mono_error_init (error);

	/*
	 * ICALL wrappers are handled specially, since there is only one copy of them
	 * shared by all appdomains.
	 */
	if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
		winfo = mono_marshal_get_wrapper_info (method);
	if (winfo && winfo->subtype == WRAPPER_SUBTYPE_ICALL_WRAPPER) {
		callinfo = mono_find_jit_icall_by_addr (winfo->d.icall.func);
		g_assert (callinfo);

		/* Must be domain neutral since there is only one copy */
		opt |= MONO_OPT_SHARED;
	} else {
		/* MONO_OPT_SHARED is no longer supported, we only use it for icall wrappers */
		opt &= ~MONO_OPT_SHARED;
	}

	if (opt & MONO_OPT_SHARED)
		target_domain = mono_get_root_domain ();
	else
		target_domain = domain;

	if (method->wrapper_type == MONO_WRAPPER_UNKNOWN) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (method);

		g_assert (info);
		if (info->subtype == WRAPPER_SUBTYPE_SYNCHRONIZED_INNER) {
			MonoGenericContext *ctx = NULL;
			if (method->is_inflated)
				ctx = mono_method_get_context (method);
			method = info->d.synchronized_inner.method;
			if (ctx) {
				method = mono_class_inflate_generic_method_checked (method, ctx, error);
				g_assert (mono_error_ok (error)); /* FIXME don't swallow the error */
			}
		}
	}

	info = lookup_method (target_domain, method);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if (! ((domain != target_domain) && !info->domain_neutral)) {
			MonoVTable *vtable;

			mono_jit_stats.methods_lookups++;
			vtable = mono_class_vtable (domain, method->klass);
			g_assert (vtable);
			if (!mono_runtime_class_init_full (vtable, error))
				return NULL;
			return mono_create_ftnptr (target_domain, info->code_start);
		}
	}

#ifdef MONO_USE_AOT_COMPILER
	if (opt & MONO_OPT_AOT) {
		MonoDomain *domain = mono_domain_get ();

		mono_class_init (method->klass);

		if ((code = mono_aot_get_method (domain, method))) {
			MonoVTable *vtable;

			/*
			 * In llvm-only mode, method might be a shared method, so we can't initialize its class.
			 * This is not a problem, since it will be initialized when the method is first
			 * called by init_method ().
			 */
			if (!mono_llvm_only) {
				vtable = mono_class_vtable (domain, method->klass);
				g_assert (vtable);
				if (!mono_runtime_class_init_full (vtable, error))
					return NULL;
			}
		}
	}
#endif

	if (!code && mono_llvm_only) {
		if (method->wrapper_type == MONO_WRAPPER_UNKNOWN) {
			WrapperInfo *info = mono_marshal_get_wrapper_info (method);

			if (info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG) {
				/*
				 * These wrappers are only created for signatures which are in the program, but
				 * sometimes we load methods too eagerly and have to create them even if they
				 * will never be called.
				 */
				return no_gsharedvt_in_wrapper;
			}
		}
	}

	if (!code)
		code = mono_jit_compile_method_inner (method, target_domain, opt, error);
	if (!mono_error_ok (error))
		return NULL;

	if (!code && mono_llvm_only) {
		printf ("AOT method not found in llvmonly mode: %s\n", mono_method_full_name (method, 1));
		g_assert_not_reached ();
	}

	if (!code)
		return NULL;

	if (method->wrapper_type == MONO_WRAPPER_WRITE_BARRIER || method->wrapper_type == MONO_WRAPPER_ALLOC) {
		MonoDomain *d;

		/*
		 * SGEN requires the JIT info for these methods to be registered, see is_ip_in_managed_allocator ().
		 */
		ji = mini_jit_info_table_find (mono_domain_get (), (char *)code, &d);
		g_assert (ji);
	}

	p = mono_create_ftnptr (target_domain, code);

	if (callinfo) {
		/*mono_register_jit_icall_wrapper takes the loader lock, so we take it on the outside. */
		mono_loader_lock ();
		mono_jit_lock ();
		if (!callinfo->wrapper) {
			callinfo->wrapper = p;
			mono_register_jit_icall_wrapper (callinfo, p);
		}
		mono_jit_unlock ();
		mono_loader_unlock ();
	}

	return p;
}

gpointer
mono_jit_compile_method (MonoMethod *method, MonoError *error)
{
	gpointer code;

	code = mono_jit_compile_method_with_opt (method, mono_get_optimizations_for_method (method, default_opt), error);
	return code;
}

#ifdef MONO_ARCH_HAVE_INVALIDATE_METHOD
static void
invalidated_delegate_trampoline (char *desc)
{
	g_error ("Unmanaged code called delegate of type %s which was already garbage collected.\n"
		 "See http://www.mono-project.com/Diagnostic:Delegate for an explanation and ways to fix this.",
		 desc);
}
#endif

/*
 * mono_jit_free_method:
 *
 *  Free all memory allocated by the JIT for METHOD.
 */
static void
mono_jit_free_method (MonoDomain *domain, MonoMethod *method)
{
	MonoJitDynamicMethodInfo *ji;
	gboolean destroy = TRUE;
	GHashTableIter iter;
	MonoJumpList *jlist;

	g_assert (method->dynamic);

	mono_domain_lock (domain);
	ji = mono_dynamic_code_hash_lookup (domain, method);
	mono_domain_unlock (domain);

	if (!ji)
		return;

	mono_debug_remove_method (method, domain);

	mono_domain_lock (domain);
	g_hash_table_remove (domain_jit_info (domain)->dynamic_code_hash, method);
	mono_domain_jit_code_hash_lock (domain);
	mono_internal_hash_table_remove (&domain->jit_code_hash, method);
	mono_domain_jit_code_hash_unlock (domain);
	g_hash_table_remove (domain_jit_info (domain)->jump_trampoline_hash, method);

	/* requires the domain lock - took above */
	mono_conc_hashtable_remove (domain_jit_info (domain)->runtime_invoke_hash, method);

	/* Remove jump targets in this method */
	g_hash_table_iter_init (&iter, domain_jit_info (domain)->jump_target_hash);
	while (g_hash_table_iter_next (&iter, NULL, (void**)&jlist)) {
		GSList *tmp, *remove;

		remove = NULL;
		for (tmp = jlist->list; tmp; tmp = tmp->next) {
			guint8 *ip = (guint8 *)tmp->data;

			if (ip >= (guint8*)ji->ji->code_start && ip < (guint8*)ji->ji->code_start + ji->ji->code_size)
				remove = g_slist_prepend (remove, tmp);
		}
		for (tmp = remove; tmp; tmp = tmp->next) {
			jlist->list = g_slist_delete_link ((GSList *)jlist->list, (GSList *)tmp->data);
		}
		g_slist_free (remove);
	}

	mono_domain_unlock (domain);

#ifdef MONO_ARCH_HAVE_INVALIDATE_METHOD
	if (debug_options.keep_delegates && method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		/*
		 * Instead of freeing the code, change it to call an error routine
		 * so people can fix their code.
		 */
		char *type = mono_type_full_name (&method->klass->byval_arg);
		char *type_and_method = g_strdup_printf ("%s.%s", type, method->name);

		g_free (type);
		mono_arch_invalidate_method (ji->ji, invalidated_delegate_trampoline, type_and_method);
		destroy = FALSE;
	}
#endif

	/*
	 * This needs to be done before freeing code_mp, since the code address is the
	 * key in the table, so if we free the code_mp first, another thread can grab the
	 * same code address and replace our entry in the table.
	 */
	mono_jit_info_table_remove (domain, ji->ji);

	if (destroy)
		mono_code_manager_destroy (ji->code_mp);
	g_free (ji);
}

gpointer
mono_jit_find_compiled_method_with_jit_info (MonoDomain *domain, MonoMethod *method, MonoJitInfo **ji)
{
	MonoDomain *target_domain;
	MonoJitInfo *info;

	if (default_opt & MONO_OPT_SHARED)
		target_domain = mono_get_root_domain ();
	else
		target_domain = domain;

	info = lookup_method (target_domain, method);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if (! ((domain != target_domain) && !info->domain_neutral)) {
			mono_jit_stats.methods_lookups++;
			if (ji)
				*ji = info;
			return info->code_start;
		}
	}

	if (ji)
		*ji = NULL;
	return NULL;
}

static guint32 bisect_opt = 0;
static GHashTable *bisect_methods_hash = NULL;

void
mono_set_bisect_methods (guint32 opt, const char *method_list_filename)
{
	FILE *file;
	char method_name [2048];

	bisect_opt = opt;
	bisect_methods_hash = g_hash_table_new (g_str_hash, g_str_equal);
	g_assert (bisect_methods_hash);

	file = fopen (method_list_filename, "r");
	g_assert (file);

	while (fgets (method_name, sizeof (method_name), file)) {
		size_t len = strlen (method_name);
		g_assert (len > 0);
		g_assert (method_name [len - 1] == '\n');
		method_name [len - 1] = 0;
		g_hash_table_insert (bisect_methods_hash, g_strdup (method_name), GINT_TO_POINTER (1));
	}
	g_assert (feof (file));
}

gboolean mono_do_single_method_regression = FALSE;
guint32 mono_single_method_regression_opt = 0;
MonoMethod *mono_current_single_method;
GSList *mono_single_method_list;
GHashTable *mono_single_method_hash;

guint32
mono_get_optimizations_for_method (MonoMethod *method, guint32 default_opt)
{
	g_assert (method);

	if (bisect_methods_hash) {
		char *name = mono_method_full_name (method, TRUE);
		void *res = g_hash_table_lookup (bisect_methods_hash, name);
		g_free (name);
		if (res)
			return default_opt | bisect_opt;
	}
	if (!mono_do_single_method_regression)
		return default_opt;
	if (!mono_current_single_method) {
		if (!mono_single_method_hash)
			mono_single_method_hash = g_hash_table_new (g_direct_hash, g_direct_equal);
		if (!g_hash_table_lookup (mono_single_method_hash, method)) {
			g_hash_table_insert (mono_single_method_hash, method, method);
			mono_single_method_list = g_slist_prepend (mono_single_method_list, method);
		}
		return default_opt;
	}
	if (method == mono_current_single_method)
		return mono_single_method_regression_opt;
	return default_opt;
}

gpointer
mono_jit_find_compiled_method (MonoDomain *domain, MonoMethod *method)
{
	return mono_jit_find_compiled_method_with_jit_info (domain, method, NULL);
}

typedef struct {
	MonoMethod *method;
	gpointer compiled_method;
	gpointer runtime_invoke;
	MonoVTable *vtable;
	MonoDynCallInfo *dyn_call_info;
	MonoClass *ret_box_class;
	MonoMethodSignature *sig;
	gboolean gsharedvt_invoke;
	gpointer *wrapper_arg;
} RuntimeInvokeInfo;

static RuntimeInvokeInfo*
create_runtime_invoke_info (MonoDomain *domain, MonoMethod *method, gpointer compiled_method, gboolean callee_gsharedvt, MonoError *error)
{
	MonoMethod *invoke;
	RuntimeInvokeInfo *info;

	info = g_new0 (RuntimeInvokeInfo, 1);
	info->compiled_method = compiled_method;
	info->sig = mono_method_signature (method);

	invoke = mono_marshal_get_runtime_invoke (method, FALSE);
	info->vtable = mono_class_vtable_full (domain, method->klass, error);
	if (!mono_error_ok (error))
		return NULL;
	g_assert (info->vtable);

	MonoMethodSignature *sig = mono_method_signature (method);
	MonoType *ret_type;

	/*
	 * We want to avoid AOTing 1000s of runtime-invoke wrappers when running
	 * in full-aot mode, so we use a slower, but more generic wrapper if
	 * possible, built on top of the OP_DYN_CALL opcode provided by the JIT.
	 */
#ifdef MONO_ARCH_DYN_CALL_SUPPORTED
	if (!mono_llvm_only && (mono_aot_only || debug_options.dyn_runtime_invoke)) {
		gboolean supported = TRUE;
		int i;

		if (method->string_ctor)
			sig = mono_marshal_get_string_ctor_signature (method);

		for (i = 0; i < sig->param_count; ++i) {
			MonoType *t = sig->params [i];

			if (t->byref && t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t)))
				supported = FALSE;
		}

		if (mono_class_is_contextbound (method->klass) || !info->compiled_method)
			supported = FALSE;

		if (supported)
			info->dyn_call_info = mono_arch_dyn_call_prepare (sig);
	}
#endif

	ret_type = sig->ret;
	switch (ret_type->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		info->ret_box_class = mono_class_from_mono_type (ret_type);
		break;
	case MONO_TYPE_PTR:
		info->ret_box_class = mono_defaults.int_class;
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
		break;
	case MONO_TYPE_GENERICINST:
		if (!MONO_TYPE_IS_REFERENCE (ret_type))
			info->ret_box_class = mono_class_from_mono_type (ret_type);
		break;
	case MONO_TYPE_VALUETYPE:
		info->ret_box_class = mono_class_from_mono_type (ret_type);
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	if (!info->dyn_call_info) {
		if (mono_llvm_only) {
#ifndef ENABLE_GSHAREDVT
			g_assert_not_reached ();
#endif
			info->gsharedvt_invoke = TRUE;
			if (!callee_gsharedvt) {
				/* Invoke a gsharedvt out wrapper instead */
				MonoMethod *wrapper = mini_get_gsharedvt_out_sig_wrapper (sig);
				MonoMethodSignature *wrapper_sig = mini_get_gsharedvt_out_sig_wrapper_signature (sig->hasthis, sig->ret->type != MONO_TYPE_VOID, sig->param_count);

				info->wrapper_arg = g_malloc0 (2 * sizeof (gpointer));
				info->wrapper_arg [0] = mini_add_method_wrappers_llvmonly (method, info->compiled_method, FALSE, FALSE, &(info->wrapper_arg [1]));

				/* Pass has_rgctx == TRUE since the wrapper has an extra arg */
				invoke = mono_marshal_get_runtime_invoke_for_sig (wrapper_sig);
				g_free (wrapper_sig);

				info->compiled_method = mono_jit_compile_method (wrapper, error);
				if (!mono_error_ok (error)) {
					g_free (info);
					return NULL;
				}
			} else {
				/* Gsharedvt methods can be invoked the same way */
				/* The out wrapper has the same signature as the compiled gsharedvt method */
				MonoMethodSignature *wrapper_sig = mini_get_gsharedvt_out_sig_wrapper_signature (sig->hasthis, sig->ret->type != MONO_TYPE_VOID, sig->param_count);

				info->wrapper_arg = mono_method_needs_static_rgctx_invoke (method, TRUE) ? mini_method_get_rgctx (method) : NULL;

				invoke = mono_marshal_get_runtime_invoke_for_sig (wrapper_sig);
				g_free (wrapper_sig);
			}
		}
		info->runtime_invoke = mono_jit_compile_method (invoke, error);
		if (!mono_error_ok (error)) {
			g_free (info);
			return NULL;
		}
	}

	return info;
}

static MonoObject*
mono_llvmonly_runtime_invoke (MonoMethod *method, RuntimeInvokeInfo *info, void *obj, void **params, MonoObject **exc, MonoError *error)
{
	MonoMethodSignature *sig = info->sig;
	MonoDomain *domain = mono_domain_get ();
	MonoObject *(*runtime_invoke) (MonoObject *this_obj, void **params, MonoObject **exc, void* compiled_method);
	gpointer *args;
	gpointer retval_ptr;
	guint8 retval [256];
	gpointer *param_refs;
	int i, pindex;

	mono_error_init (error);

	g_assert (info->gsharedvt_invoke);

	/*
	 * Instead of invoking the method directly, we invoke a gsharedvt out wrapper.
	 * The advantage of this is the gsharedvt out wrappers have a reduced set of
	 * signatures, so we only have to generate runtime invoke wrappers for these
	 * signatures.
	 * This code also handles invocation of gsharedvt methods directly, no
	 * out wrappers are used in that case.
	 */
	args = (void **)g_alloca ((sig->param_count + sig->hasthis + 2) * sizeof (gpointer));
	param_refs = (gpointer*)g_alloca ((sig->param_count + sig->hasthis + 2) * sizeof (gpointer));
	pindex = 0;
	/*
	 * The runtime invoke wrappers expects pointers to primitive types, so have to
	 * use indirections.
	 */
	if (sig->hasthis)
		args [pindex ++] = &obj;
	if (sig->ret->type != MONO_TYPE_VOID) {
		retval_ptr = (gpointer)&retval;
		args [pindex ++] = &retval_ptr;
	}
	for (i = 0; i < sig->param_count; ++i) {
		MonoType *t = sig->params [i];

		if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t))) {
			MonoClass *klass = mono_class_from_mono_type (t);
			guint8 *nullable_buf;
			int size;

			size = mono_class_value_size (klass, NULL);
			nullable_buf = g_alloca (size);
			g_assert (nullable_buf);

			/* The argument pointed to by params [i] is either a boxed vtype or null */
			mono_nullable_init (nullable_buf, (MonoObject*)params [i], klass);
			params [i] = nullable_buf;
		}

		if (!t->byref && (MONO_TYPE_IS_REFERENCE (t) || t->type == MONO_TYPE_PTR)) {
			param_refs [i] = params [i];
			params [i] = &(param_refs [i]);
		}
		args [pindex ++] = &params [i];
	}
	/* The gsharedvt out wrapper has an extra argument which contains the method to call */
	args [pindex ++] = &info->wrapper_arg;

	runtime_invoke = (MonoObject *(*)(MonoObject *, void **, MonoObject **, void *))info->runtime_invoke;

	runtime_invoke (NULL, args, exc, info->compiled_method);
	if (exc && *exc)
		mono_error_set_exception_instance (error, (MonoException*) *exc);

	if (sig->ret->type != MONO_TYPE_VOID && info->ret_box_class)
		return mono_value_box (domain, info->ret_box_class, retval);
	else
		return *(MonoObject**)retval;
}

/**
 * mono_jit_runtime_invoke:
 * @method: the method to invoke
 * @obj: this pointer
 * @params: array of parameter values.
 * @exc: Set to the exception raised in the managed method.  If NULL, error is thrown instead.
 *       If coop is enabled, this argument is ignored - all exceptoins are caught and propagated
 *       through @error
 * @error: error or caught exception object
 */
static MonoObject*
mono_jit_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc, MonoError *error)
{
	MonoMethod *invoke, *callee;
	MonoObject *(*runtime_invoke) (MonoObject *this_obj, void **params, MonoObject **exc, void* compiled_method);
	MonoDomain *domain = mono_domain_get ();
	MonoJitDomainInfo *domain_info;
	RuntimeInvokeInfo *info, *info2;
	MonoJitInfo *ji = NULL;
	gboolean callee_gsharedvt = FALSE;

	mono_error_init (error);

	if (obj == NULL && !(method->flags & METHOD_ATTRIBUTE_STATIC) && !method->string_ctor && (method->wrapper_type == 0)) {
		g_warning ("Ignoring invocation of an instance method on a NULL instance.\n");
		return NULL;
	}

	domain_info = domain_jit_info (domain);

	info = (RuntimeInvokeInfo *)mono_conc_hashtable_lookup (domain_info->runtime_invoke_hash, method);

	if (!info) {
		if (mono_security_core_clr_enabled ()) {
			/*
			 * This might be redundant since mono_class_vtable () already does this,
			 * but keep it just in case for moonlight.
			 */
			mono_class_setup_vtable (method->klass);
			if (mono_class_has_failure (method->klass)) {
				MonoException *fail_exc = mono_class_get_exception_for_failure (method->klass);
				if (exc)
					*exc = (MonoObject*)fail_exc;
				mono_error_set_exception_instance (error, fail_exc);
				return NULL;
			}
		}

		gpointer compiled_method;

		callee = method;
		if (method->klass->rank && (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
			(method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
			/*
			 * Array Get/Set/Address methods. The JIT implements them using inline code
			 * inside the runtime invoke wrappers, so no need to compile them.
			 */
			if (mono_aot_only) {
				/*
				 * Call a wrapper, since the runtime invoke wrapper was not generated.
				 */
				MonoMethod *wrapper;

				wrapper = mono_marshal_get_array_accessor_wrapper (method);
				invoke = mono_marshal_get_runtime_invoke (wrapper, FALSE);
				callee = wrapper;
			} else {
				callee = NULL;
			}
		}

		if (callee) {
			compiled_method = mono_jit_compile_method_with_opt (callee, mono_get_optimizations_for_method (callee, default_opt), error);
			if (!compiled_method) {
				g_assert (!mono_error_ok (error));
				return NULL;
			}

			if (mono_llvm_only) {
				ji = mini_jit_info_table_find (mono_domain_get (), (char *)mono_get_addr_from_ftnptr (compiled_method), NULL);
				callee_gsharedvt = mini_jit_info_is_gsharedvt (ji);
				if (callee_gsharedvt)
					callee_gsharedvt = mini_is_gsharedvt_variable_signature (mono_method_signature (jinfo_get_method (ji)));
			}

			if (!callee_gsharedvt)
				compiled_method = mini_add_method_trampoline (callee, compiled_method, mono_method_needs_static_rgctx_invoke (callee, TRUE), FALSE);
		} else {
			compiled_method = NULL;
		}

		info = create_runtime_invoke_info (domain, method, compiled_method, callee_gsharedvt, error);
		if (!mono_error_ok (error))
			return NULL;

		mono_domain_lock (domain);
		info2 = (RuntimeInvokeInfo *)mono_conc_hashtable_insert (domain_info->runtime_invoke_hash, method, info);
		mono_domain_unlock (domain);
		if (info2) {
			g_free (info);
			info = info2;
		}
	}

	/*
	 * We need this here because mono_marshal_get_runtime_invoke can place
	 * the helper method in System.Object and not the target class.
	 */
	if (!mono_runtime_class_init_full (info->vtable, error)) {
		if (exc)
			*exc = (MonoObject*) mono_error_convert_to_exception (error);
		return NULL;
	}

	/* If coop is enabled, and the caller didn't ask for the exception to be caught separately,
	   we always catch the exception and propagate it through the MonoError */
	gboolean catchExcInMonoError =
		(exc == NULL) && mono_threads_is_coop_enabled ();
	MonoObject *invoke_exc = NULL;
	if (catchExcInMonoError)
		exc = &invoke_exc;

	/* The wrappers expect this to be initialized to NULL */
	if (exc)
		*exc = NULL;

#ifdef MONO_ARCH_DYN_CALL_SUPPORTED
	if (info->dyn_call_info) {
		MonoMethodSignature *sig = mono_method_signature (method);
		gpointer *args;
		static RuntimeInvokeDynamicFunction dyn_runtime_invoke;
		int i, pindex;
		guint8 buf [512];
		guint8 retval [256];

		if (!dyn_runtime_invoke) {
			invoke = mono_marshal_get_runtime_invoke_dynamic ();
			dyn_runtime_invoke = (RuntimeInvokeDynamicFunction)mono_jit_compile_method (invoke, error);
			if (!mono_error_ok (error))
				return NULL;
		}

		/* Convert the arguments to the format expected by start_dyn_call () */
		args = (void **)g_alloca ((sig->param_count + sig->hasthis) * sizeof (gpointer));
		pindex = 0;
		if (sig->hasthis)
			args [pindex ++] = &obj;
		for (i = 0; i < sig->param_count; ++i) {
			MonoType *t = sig->params [i];

			if (t->byref) {
				args [pindex ++] = &params [i];
			} else if (MONO_TYPE_IS_REFERENCE (t) || t->type == MONO_TYPE_PTR) {
				args [pindex ++] = &params [i];
			} else {
				args [pindex ++] = params [i];
			}
		}

		//printf ("M: %s\n", mono_method_full_name (method, TRUE));

		mono_arch_start_dyn_call (info->dyn_call_info, (gpointer**)args, retval, buf, sizeof (buf));

		dyn_runtime_invoke (buf, exc, info->compiled_method);
		if (catchExcInMonoError && *exc != NULL)
			mono_error_set_exception_instance (error, (MonoException*) *exc);

		mono_arch_finish_dyn_call (info->dyn_call_info, buf);

		if (info->ret_box_class)
			return mono_value_box (domain, info->ret_box_class, retval);
		else
			return *(MonoObject**)retval;
	}
#endif

	if (mono_llvm_only)
		return mono_llvmonly_runtime_invoke (method, info, obj, params, exc, error);

	runtime_invoke = (MonoObject *(*)(MonoObject *, void **, MonoObject **, void *))info->runtime_invoke;

	MonoObject *result = runtime_invoke ((MonoObject *)obj, params, exc, info->compiled_method);
	if (catchExcInMonoError && *exc != NULL)
		mono_error_set_exception_instance (error, (MonoException*) *exc);
	return result;
}

typedef struct {
	MonoVTable *vtable;
	int slot;
} IMTThunkInfo;

typedef gpointer (*IMTThunkFunc) (gpointer *arg, MonoMethod *imt_method);

/*
 * mini_llvmonly_initial_imt_thunk:
 *
 *  This function is called the first time a call is made through an IMT thunk.
 * It should have the same signature as the mono_llvmonly_imt_thunk_... functions.
 */
static gpointer
mini_llvmonly_initial_imt_thunk (gpointer *arg, MonoMethod *imt_method)
{
	IMTThunkInfo *info = (IMTThunkInfo*)arg;
	gpointer *imt;
	gpointer *ftndesc;
	IMTThunkFunc func;

	mono_vtable_build_imt_slot (info->vtable, info->slot);

	imt = (gpointer*)info->vtable;
	imt -= MONO_IMT_SIZE;

	/* Return what the real IMT thunk returns */
	ftndesc = imt [info->slot];
	func = ftndesc [0];

	if (func == (IMTThunkFunc)mini_llvmonly_initial_imt_thunk)
		/* Happens when the imt slot contains only a generic virtual method */
		return NULL;
	return func ((gpointer *)ftndesc [1], imt_method);
}

/* This is called indirectly through an imt slot. */
static gpointer
mono_llvmonly_imt_thunk (gpointer *arg, MonoMethod *imt_method)
{
	int i = 0;

	/* arg points to an array created in mono_llvmonly_get_imt_thunk () */
	while (arg [i] && arg [i] != imt_method)
		i += 2;
	g_assert (arg [i]);

	return arg [i + 1];
}

/* Optimized versions of mono_llvmonly_imt_thunk () for different table sizes */
static gpointer
mono_llvmonly_imt_thunk_1 (gpointer *arg, MonoMethod *imt_method)
{
	//g_assert (arg [0] == imt_method);
	return arg [1];
}

static gpointer
mono_llvmonly_imt_thunk_2 (gpointer *arg, MonoMethod *imt_method)
{
	//g_assert (arg [0] == imt_method || arg [2] == imt_method);
	if (arg [0] == imt_method)
		return arg [1];
	else
		return arg [3];
}

static gpointer
mono_llvmonly_imt_thunk_3 (gpointer *arg, MonoMethod *imt_method)
{
	//g_assert (arg [0] == imt_method || arg [2] == imt_method || arg [4] == imt_method);
	if (arg [0] == imt_method)
		return arg [1];
	else if (arg [2] == imt_method)
		return arg [3];
	else
		return arg [5];
}

/*
 * A version of the imt thunk used for generic virtual/variant iface methods.
 * Unlikely a normal imt thunk, its possible that IMT_METHOD is not found
 * in the search table. The original JIT code had a 'fallback' trampoline it could
 * call, but we can't do that, so we just return NULL, and the compiled code
 * will handle it.
 */
static gpointer
mono_llvmonly_fallback_imt_thunk (gpointer *arg, MonoMethod *imt_method)
{
	int i = 0;

	while (arg [i] && arg [i] != imt_method)
		i += 2;
	if (!arg [i])
		return NULL;

	return arg [i + 1];
}

static gpointer
mono_llvmonly_get_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count, gpointer fail_tramp)
{
	gpointer *buf;
	gpointer *res;
	int i, index, real_count;
	gboolean virtual_generic = FALSE;

	/*
	 * Create an array which is passed to the imt thunk functions.
	 * The array contains MonoMethod-function descriptor pairs, terminated by a NULL entry.
	 */

	real_count = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (item->is_equals)
			real_count ++;
		if (item->has_target_code)
			virtual_generic = TRUE;
	}

	/*
	 * Initialize all vtable entries reachable from this imt slot, so the compiled
	 * code doesn't have to check it.
	 */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		int vt_slot;

		if (!item->is_equals || item->has_target_code)
			continue;
		vt_slot = item->value.vtable_slot;
		mono_init_vtable_slot (vtable, vt_slot);
	}

	/* Save the entries into an array */
	buf = (void **)mono_domain_alloc (domain, (real_count + 1) * 2 * sizeof (gpointer));
	index = 0;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];

		if (!item->is_equals)
			continue;

		g_assert (item->key);
		buf [(index * 2)] = item->key;
		if (item->has_target_code)
			buf [(index * 2) + 1] = item->value.target_code;
		else
			buf [(index * 2) + 1] = vtable->vtable [item->value.vtable_slot];
		index ++;
	}
	buf [(index * 2)] = NULL;
	buf [(index * 2) + 1] = fail_tramp;

	/*
	 * Return a function descriptor for a C function with 'buf' as its argument.
	 * It will by called by JITted code.
	 */
	res = (void **)mono_domain_alloc (domain, 2 * sizeof (gpointer));
	switch (real_count) {
	case 1:
		res [0] = mono_llvmonly_imt_thunk_1;
		break;
	case 2:
		res [0] = mono_llvmonly_imt_thunk_2;
		break;
	case 3:
		res [0] = mono_llvmonly_imt_thunk_3;
		break;
	default:
		res [0] = mono_llvmonly_imt_thunk;
		break;
	}
	if (virtual_generic || fail_tramp)
		res [0] = mono_llvmonly_fallback_imt_thunk;
	res [1] = buf;

	return res;
}

MONO_SIG_HANDLER_FUNC (, mono_sigfpe_signal_handler)
{
	MonoException *exc = NULL;
	MonoJitInfo *ji;
	MONO_SIG_HANDLER_INFO_TYPE *info = MONO_SIG_HANDLER_GET_INFO ();
	MONO_SIG_HANDLER_GET_CONTEXT;

	ji = mono_jit_info_table_find_internal (mono_domain_get (), (char *)mono_arch_ip_from_context (ctx), TRUE, TRUE);

#if defined(MONO_ARCH_HAVE_IS_INT_OVERFLOW)
	if (mono_arch_is_int_overflow (ctx, info))
		/*
		 * The spec says this throws ArithmeticException, but MS throws the derived
		 * OverflowException.
		 */
		exc = mono_get_exception_overflow ();
	else
		exc = mono_get_exception_divide_by_zero ();
#else
	exc = mono_get_exception_divide_by_zero ();
#endif

	if (!ji) {
		if (!mono_do_crash_chaining && mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;

		mono_handle_native_sigsegv (SIGSEGV, ctx, info);
		if (mono_do_crash_chaining) {
			mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
			return;
		}
	}

	mono_arch_handle_exception (ctx, exc);
}

MONO_SIG_HANDLER_FUNC (, mono_sigill_signal_handler)
{
	MonoException *exc;
	MONO_SIG_HANDLER_GET_CONTEXT;

	exc = mono_get_exception_execution_engine ("SIGILL");

	mono_arch_handle_exception (ctx, exc);
}

#if defined(MONO_ARCH_USE_SIGACTION) || defined(HOST_WIN32)
#define HAVE_SIG_INFO
#endif

MONO_SIG_HANDLER_FUNC (, mono_sigsegv_signal_handler)
{
	MonoJitInfo *ji;
	MonoJitTlsData *jit_tls = (MonoJitTlsData *)mono_native_tls_get_value (mono_jit_tls_id);
	gpointer fault_addr = NULL;
#ifdef HAVE_SIG_INFO
	MONO_SIG_HANDLER_INFO_TYPE *info = MONO_SIG_HANDLER_GET_INFO ();
#else
	void *info = NULL;
#endif
	MONO_SIG_HANDLER_GET_CONTEXT;

#if defined(MONO_ARCH_SOFT_DEBUG_SUPPORTED) && defined(HAVE_SIG_INFO)
	if (mono_arch_is_single_step_event (info, ctx)) {
		mono_debugger_agent_single_step_event (ctx);
		return;
	} else if (mono_arch_is_breakpoint_event (info, ctx)) {
		mono_debugger_agent_breakpoint_hit (ctx);
		return;
	}
#endif

#if defined(HAVE_SIG_INFO)
#if !defined(HOST_WIN32)
	fault_addr = info->si_addr;
	if (mono_aot_is_pagefault (info->si_addr)) {
		mono_aot_handle_pagefault (info->si_addr);
		return;
	}
#endif

	/* The thread might no be registered with the runtime */
	if (!mono_domain_get () || !jit_tls) {
		if (!mono_do_crash_chaining && mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;
		mono_handle_native_sigsegv (SIGSEGV, ctx, info);
		if (mono_do_crash_chaining) {
			mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
			return;
		}
	}
#endif

	ji = mono_jit_info_table_find_internal (mono_domain_get (), (char *)mono_arch_ip_from_context (ctx), TRUE, TRUE);

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	if (mono_handle_soft_stack_ovf (jit_tls, ji, ctx, info, (guint8*)info->si_addr))
		return;

#ifdef MONO_ARCH_HAVE_SIGCTX_TO_MONOCTX
	/* info->si_addr seems to be NULL on some kernels when handling stack overflows */
	fault_addr = info->si_addr;
	if (fault_addr == NULL) {
		MonoContext mctx;

		mono_sigctx_to_monoctx (ctx, &mctx);

		fault_addr = MONO_CONTEXT_GET_SP (&mctx);
	}
#endif

	if (jit_tls->stack_size &&
		ABS ((guint8*)fault_addr - ((guint8*)jit_tls->end_of_stack - jit_tls->stack_size)) < 8192 * sizeof (gpointer)) {
		/*
		 * The hard-guard page has been hit: there is not much we can do anymore
		 * Print a hopefully clear message and abort.
		 */
		mono_handle_hard_stack_ovf (jit_tls, ji, ctx, (guint8*)info->si_addr);
		g_assert_not_reached ();
	} else {
		/* The original handler might not like that it is executed on an altstack... */
		if (!ji && mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;

		mono_arch_handle_altstack_exception (ctx, info, info->si_addr, FALSE);
	}
#else

	if (!ji) {
		if (!mono_do_crash_chaining && mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;

		mono_handle_native_sigsegv (SIGSEGV, ctx, info);

		if (mono_do_crash_chaining) {
			mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
			return;
		}
	}

	mono_arch_handle_exception (ctx, NULL);
#endif
}

MONO_SIG_HANDLER_FUNC (, mono_sigint_signal_handler)
{
	MonoException *exc;
	MONO_SIG_HANDLER_GET_CONTEXT;

	exc = mono_get_exception_execution_engine ("Interrupted (SIGINT).");

	mono_arch_handle_exception (ctx, exc);
}

#ifndef DISABLE_REMOTING
/* mono_jit_create_remoting_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline which calls the remoting functions. This
 * is used in the vtable of transparent proxies.
 *
 * Returns: a pointer to the newly created code
 */
static gpointer
mono_jit_create_remoting_trampoline (MonoDomain *domain, MonoMethod *method, MonoRemotingTarget target)
{
	MonoMethod *nm;
	guint8 *addr = NULL;

	if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) && mono_method_signature (method)->generic_param_count) {
		return mono_create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING,
			domain, NULL);
	}

	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) ||
	    (mono_method_signature (method)->hasthis && (mono_class_is_marshalbyref (method->klass) || method->klass == mono_defaults.object_class))) {
		nm = mono_marshal_get_remoting_invoke_for_target (method, target);
		addr = (guint8 *)mono_compile_method (nm);
	} else
	{
		addr = (guint8 *)mono_compile_method (method);
	}
	return mono_get_addr_from_ftnptr (addr);
}
#endif

static void
no_imt_trampoline (void)
{
	g_assert_not_reached ();
}

static void
no_vcall_trampoline (void)
{
	g_assert_not_reached ();
}

static gpointer *vtable_trampolines;
static int vtable_trampolines_size;

gpointer
mini_get_vtable_trampoline (MonoVTable *vt, int slot_index)
{
	int index = slot_index + MONO_IMT_SIZE;

	if (mono_llvm_only) {
		if (slot_index < 0) {
			/* Initialize the IMT thunks to a 'trampoline' so the generated code doesn't have to initialize it */
			// FIXME: Memory management
			gpointer *ftndesc = g_malloc (2 * sizeof (gpointer));
			IMTThunkInfo *info = g_new0 (IMTThunkInfo, 1);
			info->vtable = vt;
			info->slot = index;
			ftndesc [0] = mini_llvmonly_initial_imt_thunk;
			ftndesc [1] = info;
			mono_memory_barrier ();
			return ftndesc;
		} else {
			return NULL;
		}
	}

	g_assert (slot_index >= - MONO_IMT_SIZE);
	if (!vtable_trampolines || slot_index + MONO_IMT_SIZE >= vtable_trampolines_size) {
		mono_jit_lock ();
		if (!vtable_trampolines || index >= vtable_trampolines_size) {
			int new_size;
			gpointer new_table;

			new_size = vtable_trampolines_size ? vtable_trampolines_size * 2 : 128;
			while (new_size <= index)
				new_size *= 2;
			new_table = g_new0 (gpointer, new_size);

			if (vtable_trampolines)
				memcpy (new_table, vtable_trampolines, vtable_trampolines_size * sizeof (gpointer));
			g_free (vtable_trampolines);
			mono_memory_barrier ();
			vtable_trampolines = (void **)new_table;
			vtable_trampolines_size = new_size;
		}
		mono_jit_unlock ();
	}

	if (!vtable_trampolines [index])
		vtable_trampolines [index] = mono_create_specific_trampoline (GUINT_TO_POINTER (slot_index), MONO_TRAMPOLINE_VCALL, mono_get_root_domain (), NULL);
	return vtable_trampolines [index];
}

static gpointer
mini_get_imt_trampoline (MonoVTable *vt, int slot_index)
{
	return mini_get_vtable_trampoline (vt, slot_index - MONO_IMT_SIZE);
}

static gboolean
mini_imt_entry_inited (MonoVTable *vt, int imt_slot_index)
{
	if (mono_llvm_only)
		return FALSE;

	gpointer *imt = (gpointer*)vt;
	imt -= MONO_IMT_SIZE;

	return (imt [imt_slot_index] != mini_get_imt_trampoline (vt, imt_slot_index));
}

static gboolean
is_callee_gsharedvt_variable (gpointer addr)
{
	MonoJitInfo *ji;
	gboolean callee_gsharedvt;

	ji = mini_jit_info_table_find (mono_domain_get (), (char *)mono_get_addr_from_ftnptr (addr), NULL);
	g_assert (ji);
	callee_gsharedvt = mini_jit_info_is_gsharedvt (ji);
	if (callee_gsharedvt)
		callee_gsharedvt = mini_is_gsharedvt_variable_signature (mono_method_signature (jinfo_get_method (ji)));
	return callee_gsharedvt;
}

gpointer
mini_get_delegate_arg (MonoMethod *method, gpointer method_ptr)
{
	gpointer arg = NULL;

	if (mono_method_needs_static_rgctx_invoke (method, FALSE))
		arg = mini_method_get_rgctx (method);

	/*
	 * Avoid adding gsharedvt in wrappers since they might not exist if
	 * this delegate is called through a gsharedvt delegate invoke wrapper.
	 * Instead, encode that the method is gsharedvt in del->extra_arg,
	 * the CEE_MONO_CALLI_EXTRA_ARG implementation in the JIT depends on this.
	 */
	if (method->is_inflated && is_callee_gsharedvt_variable (method_ptr)) {
		g_assert ((((mgreg_t)arg) & 1) == 0);
		arg = (gpointer)(((mgreg_t)arg) | 1);
	}
	return arg;
}

void
mini_init_delegate (MonoDelegate *del)
{
	if (mono_llvm_only)
		del->extra_arg = mini_get_delegate_arg (del->method, del->method_ptr);
}

gpointer
mono_get_delegate_virtual_invoke_impl (MonoMethodSignature *sig, MonoMethod *method)
{
	gboolean is_virtual_generic, is_interface, load_imt_reg;
	int offset, idx;

	static guint8 **cache = NULL;
	static int cache_size = 0;

	if (!method)
		return NULL;

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	is_virtual_generic = method->is_inflated && mono_method_get_declaring_generic_method (method)->is_generic;
	is_interface = method->klass->flags & TYPE_ATTRIBUTE_INTERFACE ? TRUE : FALSE;
	load_imt_reg = is_virtual_generic || is_interface;

	if (is_interface)
		offset = ((gint32)mono_method_get_imt_slot (method) - MONO_IMT_SIZE) * SIZEOF_VOID_P;
	else
		offset = G_STRUCT_OFFSET (MonoVTable, vtable) + ((mono_method_get_vtable_index (method)) * (SIZEOF_VOID_P));

	idx = (offset / SIZEOF_VOID_P + MONO_IMT_SIZE) * 2 + (load_imt_reg ? 1 : 0);
	g_assert (idx >= 0);

	/* Resize the cache to idx + 1 */
	if (cache_size < idx + 1) {
		mono_jit_lock ();
		if (cache_size < idx + 1) {
			guint8 **new_cache;
			int new_cache_size = idx + 1;

			new_cache = g_new0 (guint8*, new_cache_size);
			if (cache)
				memcpy (new_cache, cache, cache_size * sizeof (guint8*));
			g_free (cache);

			mono_memory_barrier ();
			cache = new_cache;
			cache_size = new_cache_size;
		}
		mono_jit_unlock ();
	}

	if (cache [idx])
		return cache [idx];

	/* FIXME Support more cases */
	if (mono_aot_only) {
		char tramp_name [256];
		const char *imt = load_imt_reg ? "_imt" : "";
		int ind = (load_imt_reg ? (-offset) : offset) / SIZEOF_VOID_P;

		sprintf (tramp_name, "delegate_virtual_invoke%s_%d", imt, ind);
		cache [idx] = (guint8 *)mono_aot_get_trampoline (tramp_name);
		g_assert (cache [idx]);
	} else {
		cache [idx] = (guint8 *)mono_arch_get_delegate_virtual_invoke_impl (sig, method, offset, load_imt_reg);
	}
	return cache [idx];
}

/**
 * mini_parse_debug_option:
 * @option: The option to parse.
 *
 * Parses debug options for the mono runtime. The options are the same as for
 * the MONO_DEBUG environment variable.
 *
 */
gboolean
mini_parse_debug_option (const char *option)
{
	if (!strcmp (option, "handle-sigint"))
		debug_options.handle_sigint = TRUE;
	else if (!strcmp (option, "keep-delegates"))
		debug_options.keep_delegates = TRUE;
	else if (!strcmp (option, "reverse-pinvoke-exceptions"))
		debug_options.reverse_pinvoke_exceptions = TRUE;
	else if (!strcmp (option, "collect-pagefault-stats"))
		debug_options.collect_pagefault_stats = TRUE;
	else if (!strcmp (option, "break-on-unverified"))
		debug_options.break_on_unverified = TRUE;
	else if (!strcmp (option, "no-gdb-backtrace"))
		debug_options.no_gdb_backtrace = TRUE;
	else if (!strcmp (option, "suspend-on-sigsegv"))
		debug_options.suspend_on_sigsegv = TRUE;
	else if (!strcmp (option, "suspend-on-exception"))
		debug_options.suspend_on_exception = TRUE;
	else if (!strcmp (option, "suspend-on-unhandled"))
		debug_options.suspend_on_unhandled = TRUE;
	else if (!strcmp (option, "dont-free-domains"))
		mono_dont_free_domains = TRUE;
	else if (!strcmp (option, "dyn-runtime-invoke"))
		debug_options.dyn_runtime_invoke = TRUE;
	else if (!strcmp (option, "gdb"))
		debug_options.gdb = TRUE;
	else if (!strcmp (option, "explicit-null-checks"))
		debug_options.explicit_null_checks = TRUE;
	else if (!strcmp (option, "gen-seq-points"))
		debug_options.gen_sdb_seq_points = TRUE;
	else if (!strcmp (option, "gen-compact-seq-points"))
		debug_options.gen_seq_points_compact_data = TRUE;
	else if (!strcmp (option, "single-imm-size"))
		debug_options.single_imm_size = TRUE;
	else if (!strcmp (option, "init-stacks"))
		debug_options.init_stacks = TRUE;
	else if (!strcmp (option, "casts"))
		debug_options.better_cast_details = TRUE;
	else if (!strcmp (option, "soft-breakpoints"))
		debug_options.soft_breakpoints = TRUE;
	else if (!strcmp (option, "check-pinvoke-callconv"))
		debug_options.check_pinvoke_callconv = TRUE;
	else if (!strcmp (option, "arm-use-fallback-tls"))
		debug_options.arm_use_fallback_tls = TRUE;
	else if (!strcmp (option, "debug-domain-unload"))
		mono_enable_debug_domain_unload (TRUE);
	else if (!strcmp (option, "partial-sharing"))
		mono_set_partial_sharing_supported (TRUE);
	else if (!strcmp (option, "align-small-structs"))
		mono_align_small_structs = TRUE;
	else if (!strcmp (option, "native-debugger-break"))
		debug_options.native_debugger_break = TRUE;
	else
		return FALSE;

	return TRUE;
}

static void
mini_parse_debug_options (void)
{
	const char *options = g_getenv ("MONO_DEBUG");
	gchar **args, **ptr;

	if (!options)
		return;

	args = g_strsplit (options, ",", -1);

	for (ptr = args; ptr && *ptr; ptr++) {
		const char *arg = *ptr;

		if (!mini_parse_debug_option (arg)) {
			fprintf (stderr, "Invalid option for the MONO_DEBUG env variable: %s\n", arg);
			fprintf (stderr, "Available options: 'handle-sigint', 'keep-delegates', 'reverse-pinvoke-exceptions', 'collect-pagefault-stats', 'break-on-unverified', 'no-gdb-backtrace', 'suspend-on-sigsegv', 'suspend-on-exception', 'suspend-on-unhandled', 'dont-free-domains', 'dyn-runtime-invoke', 'gdb', 'explicit-null-checks', 'gen-seq-points', 'gen-compact-seq-points', 'single-imm-size', 'init-stacks', 'casts', 'soft-breakpoints', 'check-pinvoke-callconv', 'arm-use-fallback-tls', 'debug-domain-unload', 'partial-sharing', 'align-small-structs', 'native-debugger-break'\n");
			exit (1);
		}
	}

	g_strfreev (args);
}

MonoDebugOptions *
mini_get_debug_options (void)
{
	return &debug_options;
}

static gpointer
mini_create_ftnptr (MonoDomain *domain, gpointer addr)
{
#if !defined(__ia64__) && (!defined(__ppc64__) && !defined(__powerpc64__) || _CALL_ELF == 2)
	return addr;
#else
	gpointer* desc = NULL;

	if ((desc = g_hash_table_lookup (domain->ftnptrs_hash, addr)))
		return desc;
#	ifdef __ia64__
	desc = mono_domain_code_reserve (domain, 2 * sizeof (gpointer));

	desc [0] = addr;
	desc [1] = NULL;
#	elif defined(__ppc64__) || defined(__powerpc64__)

	desc = mono_domain_alloc0 (domain, 3 * sizeof (gpointer));

	desc [0] = addr;
	desc [1] = NULL;
	desc [2] = NULL;
#	endif
	g_hash_table_insert (domain->ftnptrs_hash, addr, desc);
	return desc;
#endif
}

static gpointer
mini_get_addr_from_ftnptr (gpointer descr)
{
#if defined(__ia64__) || ((defined(__ppc64__) || defined(__powerpc64__)) && _CALL_ELF != 2)
	return *(gpointer*)descr;
#else
	return descr;
#endif
}

static void
register_jit_stats (void)
{
	mono_counters_register ("Compiled methods", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.methods_compiled);
	mono_counters_register ("Methods from AOT", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.methods_aot);
	mono_counters_register ("Methods JITted using mono JIT", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.methods_without_llvm);
	mono_counters_register ("Methods JITted using LLVM", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.methods_with_llvm);
	mono_counters_register ("JIT/method-to-IR (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_method_to_ir);
	mono_counters_register ("JIT/liveness_handle_exception_clauses (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_liveness_handle_exception_clauses);
	mono_counters_register ("JIT/handle_out_of_line_bblock (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_handle_out_of_line_bblock);
	mono_counters_register ("JIT/decompose_long_opts (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_decompose_long_opts);
	mono_counters_register ("JIT/local_cprop (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_cprop);
	mono_counters_register ("JIT/local_emulate_ops (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_emulate_ops);
	mono_counters_register ("JIT/optimize_branches (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_optimize_branches);
	mono_counters_register ("JIT/handle_global_vregs (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_handle_global_vregs);
	mono_counters_register ("JIT/local_deadce (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_deadce);
	mono_counters_register ("JIT/local_alias_analysis (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_alias_analysis);
	mono_counters_register ("JIT/if_conversion (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_if_conversion);
	mono_counters_register ("JIT/bb_ordering (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_bb_ordering);
	mono_counters_register ("JIT/compile_dominator_info (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_compile_dominator_info);
	mono_counters_register ("JIT/compute_natural_loops (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_compute_natural_loops);
	mono_counters_register ("JIT/insert_safepoints (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_insert_safepoints);
	mono_counters_register ("JIT/ssa_compute (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_ssa_compute);
	mono_counters_register ("JIT/ssa_cprop (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_ssa_cprop);
	mono_counters_register ("JIT/ssa_deadce(sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_ssa_deadce);
	mono_counters_register ("JIT/perform_abc_removal (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_perform_abc_removal);
	mono_counters_register ("JIT/ssa_remove (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_ssa_remove);
	mono_counters_register ("JIT/local_cprop2 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_cprop2);
	mono_counters_register ("JIT/handle_global_vregs2 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_handle_global_vregs2);
	mono_counters_register ("JIT/local_deadce2 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_deadce2);
	mono_counters_register ("JIT/optimize_branches2 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_optimize_branches2);
	mono_counters_register ("JIT/decompose_vtype_opts (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_decompose_vtype_opts);
	mono_counters_register ("JIT/decompose_array_access_opts (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_decompose_array_access_opts);
	mono_counters_register ("JIT/liveness_handle_exception_clauses2 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_liveness_handle_exception_clauses2);
	mono_counters_register ("JIT/analyze_liveness (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_analyze_liveness);
	mono_counters_register ("JIT/linear_scan (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_linear_scan);
	mono_counters_register ("JIT/arch_allocate_vars (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_arch_allocate_vars);
	mono_counters_register ("JIT/spill_global_vars (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_spill_global_vars);
	mono_counters_register ("JIT/jit_local_cprop3 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_cprop3);
	mono_counters_register ("JIT/jit_local_deadce3 (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_local_deadce3);
	mono_counters_register ("JIT/codegen (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_codegen);
	mono_counters_register ("JIT/create_jit_info (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_create_jit_info);
	mono_counters_register ("JIT/gc_create_gc_map (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_gc_create_gc_map);
	mono_counters_register ("JIT/save_seq_point_info (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_save_seq_point_info);
	mono_counters_register ("Total time spent JITting (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_time);
	mono_counters_register ("Basic blocks", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.basic_blocks);
	mono_counters_register ("Max basic blocks", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.max_basic_blocks);
	mono_counters_register ("Allocated vars", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocate_var);
	mono_counters_register ("Code reallocs", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.code_reallocs);
	mono_counters_register ("Allocated code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocated_code_size);
	mono_counters_register ("Allocated seq points size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocated_seq_points_size);
	mono_counters_register ("Inlineable methods", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.inlineable_methods);
	mono_counters_register ("Inlined methods", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.inlined_methods);
	mono_counters_register ("Regvars", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.regvars);
	mono_counters_register ("Locals stack size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.locals_stack_size);
	mono_counters_register ("Method cache lookups", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.methods_lookups);
	mono_counters_register ("Compiled CIL code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.cil_code_size);
	mono_counters_register ("Native code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.native_code_size);
	mono_counters_register ("Aliases found", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.alias_found);
	mono_counters_register ("Aliases eliminated", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.alias_removed);
	mono_counters_register ("Aliased loads eliminated", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.loads_eliminated);
	mono_counters_register ("Aliased stores eliminated", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.stores_eliminated);
}

static void runtime_invoke_info_free (gpointer value);

static gint
class_method_pair_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoClassMethodPair *apair = (const MonoClassMethodPair *)ka;
	const MonoClassMethodPair *bpair = (const MonoClassMethodPair *)kb;

	return apair->klass == bpair->klass && apair->method == bpair->method ? 1 : 0;
}

static guint
class_method_pair_hash (gconstpointer data)
{
	const MonoClassMethodPair *pair = (const MonoClassMethodPair *)data;

	return (gsize)pair->klass ^ (gsize)pair->method;
}

static void
mini_create_jit_domain_info (MonoDomain *domain)
{
	MonoJitDomainInfo *info = g_new0 (MonoJitDomainInfo, 1);

	info->jump_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jit_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->delegate_trampoline_hash = g_hash_table_new (class_method_pair_hash, class_method_pair_equal);
	info->llvm_vcall_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->runtime_invoke_hash = mono_conc_hashtable_new_full (mono_aligned_addr_hash, NULL, NULL, runtime_invoke_info_free);
	info->seq_points = g_hash_table_new_full (mono_aligned_addr_hash, NULL, NULL, mono_seq_point_info_free);
	info->arch_seq_points = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jump_target_hash = g_hash_table_new (NULL, NULL);

	domain->runtime_info = info;
}

static void
delete_jump_list (gpointer key, gpointer value, gpointer user_data)
{
	MonoJumpList *jlist = (MonoJumpList *)value;
	g_slist_free (jlist->list);
}

static void
delete_got_slot_list (gpointer key, gpointer value, gpointer user_data)
{
	GSList *list = (GSList *)value;
	g_slist_free (list);
}

static void
dynamic_method_info_free (gpointer key, gpointer value, gpointer user_data)
{
	MonoJitDynamicMethodInfo *di = (MonoJitDynamicMethodInfo *)value;
	mono_code_manager_destroy (di->code_mp);
	g_free (di);
}

static void
runtime_invoke_info_free (gpointer value)
{
	RuntimeInvokeInfo *info = (RuntimeInvokeInfo*)value;

#ifdef MONO_ARCH_DYN_CALL_SUPPORTED
	if (info->dyn_call_info)
		mono_arch_dyn_call_free (info->dyn_call_info);
#endif
	g_free (info);
}

static void
mini_free_jit_domain_info (MonoDomain *domain)
{
	MonoJitDomainInfo *info = domain_jit_info (domain);

	g_hash_table_foreach (info->jump_target_hash, delete_jump_list, NULL);
	g_hash_table_destroy (info->jump_target_hash);
	if (info->jump_target_got_slot_hash) {
		g_hash_table_foreach (info->jump_target_got_slot_hash, delete_got_slot_list, NULL);
		g_hash_table_destroy (info->jump_target_got_slot_hash);
	}
	if (info->dynamic_code_hash) {
		g_hash_table_foreach (info->dynamic_code_hash, dynamic_method_info_free, NULL);
		g_hash_table_destroy (info->dynamic_code_hash);
	}
	if (info->method_code_hash)
		g_hash_table_destroy (info->method_code_hash);
	g_hash_table_destroy (info->jump_trampoline_hash);
	g_hash_table_destroy (info->jit_trampoline_hash);
	g_hash_table_destroy (info->delegate_trampoline_hash);
	if (info->static_rgctx_trampoline_hash)
		g_hash_table_destroy (info->static_rgctx_trampoline_hash);
	g_hash_table_destroy (info->llvm_vcall_trampoline_hash);
	mono_conc_hashtable_destroy (info->runtime_invoke_hash);
	g_hash_table_destroy (info->seq_points);
	g_hash_table_destroy (info->arch_seq_points);
	if (info->agent_info)
		mono_debugger_agent_free_domain_info (domain);
	if (info->gsharedvt_arg_tramp_hash)
		g_hash_table_destroy (info->gsharedvt_arg_tramp_hash);
#ifdef ENABLE_LLVM
	mono_llvm_free_domain_info (domain);
#endif

	g_free (domain->runtime_info);
	domain->runtime_info = NULL;
}

#ifdef ENABLE_LLVM
static gboolean
llvm_init_inner (void)
{
	if (!mono_llvm_load (NULL))
		return FALSE;

	mono_llvm_init ();
	return TRUE;
}
#endif

/*
 * mini_llvm_init:
 *
 *   Load and initialize LLVM support.
 * Return TRUE on success.
 */
gboolean
mini_llvm_init (void)
{
#ifdef ENABLE_LLVM
	static gboolean llvm_inited;
	static gboolean init_result;

	mono_loader_lock_if_inited ();
	if (!llvm_inited) {
		init_result = llvm_init_inner ();
		llvm_inited = TRUE;
	}
	mono_loader_unlock_if_inited ();
	return init_result;
#else
	return FALSE;
#endif
}

MonoDomain *
mini_init (const char *filename, const char *runtime_version)
{
	MonoError error;
	MonoDomain *domain;
	MonoRuntimeCallbacks callbacks;
	MonoThreadInfoRuntimeCallbacks ticallbacks;

	MONO_VES_INIT_BEGIN ();

	CHECKED_MONO_INIT ();

#if defined(__linux__) && !defined(__native_client__)
	if (access ("/proc/self/maps", F_OK) != 0) {
		g_print ("Mono requires /proc to be mounted.\n");
		exit (1);
	}
#endif

	mono_os_mutex_init_recursive (&jit_mutex);

	mono_cross_helpers_run ();

	mini_jit_init ();

	/* Happens when using the embedding interface */
	if (!default_opt_set)
		default_opt = mono_parse_default_optimizations (NULL);

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED
	if (mono_aot_only)
		mono_set_generic_sharing_vt_supported (TRUE);
#else
	if (mono_llvm_only)
		mono_set_generic_sharing_vt_supported (TRUE);
#endif

#ifdef MONO_HAVE_FAST_TLS
	MONO_FAST_TLS_INIT (mono_jit_tls);
	MONO_FAST_TLS_INIT (mono_lmf_addr);
#ifdef MONO_ARCH_ENABLE_MONO_LMF_VAR
	MONO_FAST_TLS_INIT (mono_lmf);
#endif
#endif

	mono_runtime_set_has_tls_get (MONO_ARCH_HAVE_TLS_GET);

	if (!global_codeman)
		global_codeman = mono_code_manager_new ();

	memset (&callbacks, 0, sizeof (callbacks));
	callbacks.create_ftnptr = mini_create_ftnptr;
	callbacks.get_addr_from_ftnptr = mini_get_addr_from_ftnptr;
	callbacks.get_runtime_build_info = mono_get_runtime_build_info;
	callbacks.set_cast_details = mono_set_cast_details;
	callbacks.debug_log = mono_debugger_agent_debug_log;
	callbacks.debug_log_is_enabled = mono_debugger_agent_debug_log_is_enabled;
	callbacks.tls_key_supported = mini_tls_key_supported;
	callbacks.get_vtable_trampoline = mini_get_vtable_trampoline;
	callbacks.get_imt_trampoline = mini_get_imt_trampoline;
	callbacks.imt_entry_inited = mini_imt_entry_inited;
	callbacks.init_delegate = mini_init_delegate;
#define JIT_INVOKE_WORKS
#ifdef JIT_INVOKE_WORKS
	callbacks.runtime_invoke = mono_jit_runtime_invoke;
#endif
#define JIT_TRAMPOLINES_WORK
#ifdef JIT_TRAMPOLINES_WORK
	callbacks.compile_method = mono_jit_compile_method;
	callbacks.create_jump_trampoline = mono_create_jump_trampoline;
	callbacks.create_jit_trampoline = mono_create_jit_trampoline;
#endif

	mono_install_callbacks (&callbacks);

	memset (&ticallbacks, 0, sizeof (ticallbacks));
	ticallbacks.setup_async_callback = mono_setup_async_callback;
	ticallbacks.thread_state_init_from_sigctx = mono_thread_state_init_from_sigctx;
	ticallbacks.thread_state_init_from_handle = mono_thread_state_init_from_handle;
	ticallbacks.thread_state_init = mono_thread_state_init;

	mono_counters_init ();

	mono_threads_runtime_init (&ticallbacks);

	if (g_getenv ("MONO_DEBUG") != NULL)
		mini_parse_debug_options ();

	mono_code_manager_init ();

	mono_hwcap_init ();

	mono_arch_cpu_init ();

	mono_arch_init ();

	mono_unwind_init ();

#ifdef XDEBUG_ENABLED
	if (g_getenv ("MONO_XDEBUG")) {
		const char *xdebug_opts = g_getenv ("MONO_XDEBUG");
		mono_xdebug_init (xdebug_opts);
		/* So methods for multiple domains don't have the same address */
		mono_dont_free_domains = TRUE;
		mono_using_xdebug = TRUE;
	} else if (mini_get_debug_options ()->gdb) {
		mono_xdebug_init ((char*)"gdb");
		mono_dont_free_domains = TRUE;
		mono_using_xdebug = TRUE;
	}
#endif

#ifdef ENABLE_LLVM
	if (mono_use_llvm) {
		if (!mono_llvm_load (NULL)) {
			mono_use_llvm = FALSE;
			fprintf (stderr, "Mono Warning: llvm support could not be loaded.\n");
		}
	}
	if (mono_use_llvm)
		mono_llvm_init ();
#endif

	mono_trampolines_init ();

	mono_native_tls_alloc (&mono_jit_tls_id, NULL);

	if (default_opt & MONO_OPT_AOT)
		mono_aot_init ();

	mono_debugger_agent_init ();

#ifdef MONO_ARCH_GSHARED_SUPPORTED
	mono_set_generic_sharing_supported (TRUE);
#endif

#ifndef MONO_CROSS_COMPILE
	mono_runtime_install_handlers ();
#endif
	mono_threads_install_cleanup (mini_thread_cleanup);

#ifdef JIT_TRAMPOLINES_WORK
	mono_install_free_method (mono_jit_free_method);
#ifndef DISABLE_REMOTING
	mono_install_remoting_trampoline (mono_jit_create_remoting_trampoline);
#endif
	mono_install_delegate_trampoline (mono_create_delegate_trampoline);
	mono_install_create_domain_hook (mini_create_jit_domain_info);
	mono_install_free_domain_hook (mini_free_jit_domain_info);
#endif
	mono_install_get_cached_class_info (mono_aot_get_cached_class_info);
	mono_install_get_class_from_name (mono_aot_get_class_from_name);
	mono_install_jit_info_find_in_aot (mono_aot_find_jit_info);

	if (debug_options.collect_pagefault_stats)
		mono_aot_set_make_unreadable (TRUE);

	if (runtime_version)
		domain = mono_init_version (filename, runtime_version);
	else
		domain = mono_init_from_assembly (filename, filename);

	if (mono_aot_only) {
		/* This helps catch code allocation requests */
		mono_code_manager_set_read_only (domain->code_mp);
		mono_marshal_use_aot_wrappers (TRUE);
	}

	if (mono_llvm_only) {
		mono_install_imt_thunk_builder (mono_llvmonly_get_imt_thunk);
		mono_set_always_build_imt_thunks (TRUE);
	} else if (mono_aot_only) {
		mono_install_imt_thunk_builder (mono_aot_get_imt_thunk);
	} else {
		mono_install_imt_thunk_builder (mono_arch_build_imt_thunk);
	}

	/*Init arch tls information only after the metadata side is inited to make sure we see dynamic appdomain tls keys*/
	mono_arch_finish_init ();

	mono_icall_init ();

	/* This must come after mono_init () in the aot-only case */
	mono_exceptions_init ();

	/* This should come after mono_init () too */
	mini_gc_init ();

#ifndef DISABLE_JIT
	mono_create_helper_signatures ();
#endif

	register_jit_stats ();

#define JIT_CALLS_WORK
#ifdef JIT_CALLS_WORK
	/* Needs to be called here since register_jit_icall depends on it */
	mono_marshal_init ();

	mono_arch_register_lowlevel_calls ();

	register_icalls ();

	mono_generic_sharing_init ();
#endif

#ifdef MONO_ARCH_SIMD_INTRINSICS
	mono_simd_intrinsics_init ();
#endif

#if MONO_SUPPORT_TASKLETS
	mono_tasklets_init ();
#endif

	register_trampolines (domain);

	if (mono_compile_aot)
		/*
		 * Avoid running managed code when AOT compiling, since the platform
		 * might only support aot-only execution.
		 */
		mono_runtime_set_no_exec (TRUE);

#define JIT_RUNTIME_WORKS
#ifdef JIT_RUNTIME_WORKS
	mono_install_runtime_cleanup ((MonoDomainFunc)mini_cleanup);
	mono_runtime_init_checked (domain, mono_thread_start_cb, mono_thread_attach_cb, &error);
	mono_error_assert_ok (&error);
	mono_thread_attach (domain);
#endif

	mono_profiler_runtime_initialized ();

	MONO_VES_INIT_END ();

	return domain;
}

static void
register_icalls (void)
{
	mono_add_internal_call ("System.Diagnostics.StackFrame::get_frame_info",
				ves_icall_get_frame_info);
	mono_add_internal_call ("System.Diagnostics.StackTrace::get_trace",
				ves_icall_get_trace);
	mono_add_internal_call ("Mono.Runtime::mono_runtime_install_handlers",
				mono_runtime_install_handlers);

#if defined(PLATFORM_ANDROID) || defined(TARGET_ANDROID)
	mono_add_internal_call ("System.Diagnostics.Debugger::Mono_UnhandledException_internal",
				mono_debugger_agent_unhandled_exception);
#endif

	/*
	 * It's important that we pass `TRUE` as the last argument here, as
	 * it causes the JIT to omit a wrapper for these icalls. If the JIT
	 * *did* emit a wrapper, we'd be looking at infinite recursion since
	 * the wrapper would call the icall which would call the wrapper and
	 * so on.
	 */
	register_icall (mono_profiler_method_enter, "mono_profiler_method_enter", "void ptr", TRUE);
	register_icall (mono_profiler_method_leave, "mono_profiler_method_leave", "void ptr", TRUE);

	register_icall (mono_trace_enter_method, "mono_trace_enter_method", NULL, TRUE);
	register_icall (mono_trace_leave_method, "mono_trace_leave_method", NULL, TRUE);
	register_icall (mono_get_lmf_addr, "mono_get_lmf_addr", "ptr", TRUE);
	register_icall (mono_jit_thread_attach, "mono_jit_thread_attach", "ptr ptr ptr", TRUE);
	register_icall (mono_jit_thread_detach, "mono_jit_thread_detach", "void ptr ptr", TRUE);
	register_icall (mono_domain_get, "mono_domain_get", "ptr", TRUE);

	register_icall (mono_llvm_throw_exception, "mono_llvm_throw_exception", "void object", TRUE);
	register_icall (mono_llvm_rethrow_exception, "mono_llvm_rethrow_exception", "void object", TRUE);
	register_icall (mono_llvm_resume_exception, "mono_llvm_resume_exception", "void", TRUE);
	register_icall (mono_llvm_match_exception, "mono_llvm_match_exception", "int ptr int int", TRUE);
	register_icall (mono_llvm_clear_exception, "mono_llvm_clear_exception", NULL, TRUE);
	register_icall (mono_llvm_load_exception, "mono_llvm_load_exception", "object", TRUE);
	register_icall (mono_llvm_throw_corlib_exception, "mono_llvm_throw_corlib_exception", "void int", TRUE);
#if defined(ENABLE_LLVM) && !defined(MONO_LLVM_LOADED)
	register_icall (mono_llvm_set_unhandled_exception_handler, "mono_llvm_set_unhandled_exception_handler", NULL, TRUE);

	// FIXME: This is broken
	register_icall (mono_debug_personality, "mono_debug_personality", "int int int ptr ptr ptr", TRUE);
#endif

	register_dyn_icall (mono_get_throw_exception (), "mono_arch_throw_exception", "void object", TRUE);
	register_dyn_icall (mono_get_rethrow_exception (), "mono_arch_rethrow_exception", "void object", TRUE);
	register_dyn_icall (mono_get_throw_corlib_exception (), "mono_arch_throw_corlib_exception", "void ptr", TRUE);
	register_icall (mono_thread_get_undeniable_exception, "mono_thread_get_undeniable_exception", "object", FALSE);
	register_icall (mono_thread_interruption_checkpoint, "mono_thread_interruption_checkpoint", "object", FALSE);
	register_icall (mono_thread_force_interruption_checkpoint_noraise, "mono_thread_force_interruption_checkpoint_noraise", "object", FALSE);
#ifndef DISABLE_REMOTING
	register_icall (mono_load_remote_field_new, "mono_load_remote_field_new", "object object ptr ptr", FALSE);
	register_icall (mono_store_remote_field_new, "mono_store_remote_field_new", "void object ptr ptr object", FALSE);
#endif

#if defined(__native_client__) || defined(__native_client_codegen__)
	register_icall (mono_nacl_gc, "mono_nacl_gc", "void", FALSE);
#endif

	if (mono_threads_is_coop_enabled ())
		register_icall (mono_threads_state_poll, "mono_threads_state_poll", "void", FALSE);

#ifndef MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS
	register_opcode_emulation (OP_LMUL, "__emul_lmul", "long long long", mono_llmult, "mono_llmult", TRUE);
	register_opcode_emulation (OP_LDIV, "__emul_ldiv", "long long long", mono_lldiv, "mono_lldiv", FALSE);
	register_opcode_emulation (OP_LDIV_UN, "__emul_ldiv_un", "long long long", mono_lldiv_un, "mono_lldiv_un", FALSE);
	register_opcode_emulation (OP_LREM, "__emul_lrem", "long long long", mono_llrem, "mono_llrem", FALSE);
	register_opcode_emulation (OP_LREM_UN, "__emul_lrem_un", "long long long", mono_llrem_un, "mono_llrem_un", FALSE);
#endif
#if !defined(MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS) || defined(MONO_ARCH_EMULATE_LONG_MUL_OVF_OPTS)
	register_opcode_emulation (OP_LMUL_OVF_UN, "__emul_lmul_ovf_un", "long long long", mono_llmult_ovf_un, "mono_llmult_ovf_un", FALSE);
	register_opcode_emulation (OP_LMUL_OVF, "__emul_lmul_ovf", "long long long", mono_llmult_ovf, "mono_llmult_ovf", FALSE);
#endif

#ifndef MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
	register_opcode_emulation (OP_LSHL, "__emul_lshl", "long long int32", mono_lshl, "mono_lshl", TRUE);
	register_opcode_emulation (OP_LSHR, "__emul_lshr", "long long int32", mono_lshr, "mono_lshr", TRUE);
	register_opcode_emulation (OP_LSHR_UN, "__emul_lshr_un", "long long int32", mono_lshr_un, "mono_lshr_un", TRUE);
#endif

#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_EMULATE_DIV)
	register_opcode_emulation (OP_IDIV, "__emul_op_idiv", "int32 int32 int32", mono_idiv, "mono_idiv", FALSE);
	register_opcode_emulation (OP_IDIV_UN, "__emul_op_idiv_un", "int32 int32 int32", mono_idiv_un, "mono_idiv_un", FALSE);
	register_opcode_emulation (OP_IREM, "__emul_op_irem", "int32 int32 int32", mono_irem, "mono_irem", FALSE);
	register_opcode_emulation (OP_IREM_UN, "__emul_op_irem_un", "int32 int32 int32", mono_irem_un, "mono_irem_un", FALSE);
#endif

#ifdef MONO_ARCH_EMULATE_MUL_DIV
	register_opcode_emulation (OP_IMUL, "__emul_op_imul", "int32 int32 int32", mono_imul, "mono_imul", TRUE);
#endif

#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_EMULATE_MUL_OVF)
	register_opcode_emulation (OP_IMUL_OVF, "__emul_op_imul_ovf", "int32 int32 int32", mono_imul_ovf, "mono_imul_ovf", FALSE);
	register_opcode_emulation (OP_IMUL_OVF_UN, "__emul_op_imul_ovf_un", "int32 int32 int32", mono_imul_ovf_un, "mono_imul_ovf_un", FALSE);
#endif

#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_SOFT_FLOAT_FALLBACK)
	register_opcode_emulation (OP_FDIV, "__emul_fdiv", "double double double", mono_fdiv, "mono_fdiv", FALSE);
#endif

	register_opcode_emulation (OP_FCONV_TO_U8, "__emul_fconv_to_u8", "ulong double", mono_fconv_u8, "mono_fconv_u8", FALSE);
	register_opcode_emulation (OP_RCONV_TO_U8, "__emul_rconv_to_u8", "ulong float", mono_rconv_u8, "mono_rconv_u8", FALSE);
	register_opcode_emulation (OP_FCONV_TO_U4, "__emul_fconv_to_u4", "uint32 double", mono_fconv_u4, "mono_fconv_u4", FALSE);
	register_opcode_emulation (OP_FCONV_TO_OVF_I8, "__emul_fconv_to_ovf_i8", "long double", mono_fconv_ovf_i8, "mono_fconv_ovf_i8", FALSE);
	register_opcode_emulation (OP_FCONV_TO_OVF_U8, "__emul_fconv_to_ovf_u8", "ulong double", mono_fconv_ovf_u8, "mono_fconv_ovf_u8", FALSE);
	register_opcode_emulation (OP_RCONV_TO_OVF_I8, "__emul_rconv_to_ovf_i8", "long float", mono_rconv_ovf_i8, "mono_rconv_ovf_i8", FALSE);
	register_opcode_emulation (OP_RCONV_TO_OVF_U8, "__emul_rconv_to_ovf_u8", "ulong float", mono_rconv_ovf_u8, "mono_rconv_ovf_u8", FALSE);


#ifdef MONO_ARCH_EMULATE_FCONV_TO_I8
	register_opcode_emulation (OP_FCONV_TO_I8, "__emul_fconv_to_i8", "long double", mono_fconv_i8, "mono_fconv_i8", FALSE);
	register_opcode_emulation (OP_RCONV_TO_I8, "__emul_rconv_to_i8", "long float", mono_rconv_i8, "mono_rconv_i8", FALSE);
#endif

#ifdef MONO_ARCH_EMULATE_CONV_R8_UN
	register_opcode_emulation (OP_ICONV_TO_R_UN, "__emul_iconv_to_r_un", "double int32", mono_conv_to_r8_un, "mono_conv_to_r8_un", FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_LCONV_TO_R8
	register_opcode_emulation (OP_LCONV_TO_R8, "__emul_lconv_to_r8", "double long", mono_lconv_to_r8, "mono_lconv_to_r8", FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_LCONV_TO_R4
	register_opcode_emulation (OP_LCONV_TO_R4, "__emul_lconv_to_r4", "float long", mono_lconv_to_r4, "mono_lconv_to_r4", FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_LCONV_TO_R8_UN
	register_opcode_emulation (OP_LCONV_TO_R_UN, "__emul_lconv_to_r8_un", "double long", mono_lconv_to_r8_un, "mono_lconv_to_r8_un", FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_FREM
#if defined(__default_codegen__)
	register_opcode_emulation (OP_FREM, "__emul_frem", "double double double", fmod, "fmod", FALSE);
	register_opcode_emulation (OP_RREM, "__emul_rrem", "float float float", fmodf, "fmodf", FALSE);
#elif defined(__native_client_codegen__)
	register_opcode_emulation (OP_FREM, "__emul_frem", "double double double", mono_fmod, "mono_fmod", FALSE);
#endif
#endif

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	if (mono_arch_is_soft_float ()) {
		register_opcode_emulation (OP_FSUB, "__emul_fsub", "double double double", mono_fsub, "mono_fsub", FALSE);
		register_opcode_emulation (OP_FADD, "__emul_fadd", "double double double", mono_fadd, "mono_fadd", FALSE);
		register_opcode_emulation (OP_FMUL, "__emul_fmul", "double double double", mono_fmul, "mono_fmul", FALSE);
		register_opcode_emulation (OP_FNEG, "__emul_fneg", "double double", mono_fneg, "mono_fneg", FALSE);
		register_opcode_emulation (OP_ICONV_TO_R8, "__emul_iconv_to_r8", "double int32", mono_conv_to_r8, "mono_conv_to_r8", FALSE);
		register_opcode_emulation (OP_ICONV_TO_R4, "__emul_iconv_to_r4", "double int32", mono_conv_to_r4, "mono_conv_to_r4", FALSE);
		register_opcode_emulation (OP_FCONV_TO_R4, "__emul_fconv_to_r4", "double double", mono_fconv_r4, "mono_fconv_r4", FALSE);
		register_opcode_emulation (OP_FCONV_TO_I1, "__emul_fconv_to_i1", "int8 double", mono_fconv_i1, "mono_fconv_i1", FALSE);
		register_opcode_emulation (OP_FCONV_TO_I2, "__emul_fconv_to_i2", "int16 double", mono_fconv_i2, "mono_fconv_i2", FALSE);
		register_opcode_emulation (OP_FCONV_TO_I4, "__emul_fconv_to_i4", "int32 double", mono_fconv_i4, "mono_fconv_i4", FALSE);
		register_opcode_emulation (OP_FCONV_TO_U1, "__emul_fconv_to_u1", "uint8 double", mono_fconv_u1, "mono_fconv_u1", FALSE);
		register_opcode_emulation (OP_FCONV_TO_U2, "__emul_fconv_to_u2", "uint16 double", mono_fconv_u2, "mono_fconv_u2", FALSE);

#if SIZEOF_VOID_P == 4
		register_opcode_emulation (OP_FCONV_TO_I, "__emul_fconv_to_i", "int32 double", mono_fconv_i4, "mono_fconv_i4", FALSE);
#endif

		register_opcode_emulation (OP_FBEQ, "__emul_fcmp_eq", "uint32 double double", mono_fcmp_eq, "mono_fcmp_eq", FALSE);
		register_opcode_emulation (OP_FBLT, "__emul_fcmp_lt", "uint32 double double", mono_fcmp_lt, "mono_fcmp_lt", FALSE);
		register_opcode_emulation (OP_FBGT, "__emul_fcmp_gt", "uint32 double double", mono_fcmp_gt, "mono_fcmp_gt", FALSE);
		register_opcode_emulation (OP_FBLE, "__emul_fcmp_le", "uint32 double double", mono_fcmp_le, "mono_fcmp_le", FALSE);
		register_opcode_emulation (OP_FBGE, "__emul_fcmp_ge", "uint32 double double", mono_fcmp_ge, "mono_fcmp_ge", FALSE);
		register_opcode_emulation (OP_FBNE_UN, "__emul_fcmp_ne_un", "uint32 double double", mono_fcmp_ne_un, "mono_fcmp_ne_un", FALSE);
		register_opcode_emulation (OP_FBLT_UN, "__emul_fcmp_lt_un", "uint32 double double", mono_fcmp_lt_un, "mono_fcmp_lt_un", FALSE);
		register_opcode_emulation (OP_FBGT_UN, "__emul_fcmp_gt_un", "uint32 double double", mono_fcmp_gt_un, "mono_fcmp_gt_un", FALSE);
		register_opcode_emulation (OP_FBLE_UN, "__emul_fcmp_le_un", "uint32 double double", mono_fcmp_le_un, "mono_fcmp_le_un", FALSE);
		register_opcode_emulation (OP_FBGE_UN, "__emul_fcmp_ge_un", "uint32 double double", mono_fcmp_ge_un, "mono_fcmp_ge_un", FALSE);

		register_opcode_emulation (OP_FCEQ, "__emul_fcmp_ceq", "uint32 double double", mono_fceq, "mono_fceq", FALSE);
		register_opcode_emulation (OP_FCGT, "__emul_fcmp_cgt", "uint32 double double", mono_fcgt, "mono_fcgt", FALSE);
		register_opcode_emulation (OP_FCGT_UN, "__emul_fcmp_cgt_un", "uint32 double double", mono_fcgt_un, "mono_fcgt_un", FALSE);
		register_opcode_emulation (OP_FCLT, "__emul_fcmp_clt", "uint32 double double", mono_fclt, "mono_fclt", FALSE);
		register_opcode_emulation (OP_FCLT_UN, "__emul_fcmp_clt_un", "uint32 double double", mono_fclt_un, "mono_fclt_un", FALSE);

		register_icall (mono_fload_r4, "mono_fload_r4", "double ptr", FALSE);
		register_icall (mono_fstore_r4, "mono_fstore_r4", "void double ptr", FALSE);
		register_icall (mono_fload_r4_arg, "mono_fload_r4_arg", "uint32 double", FALSE);
		register_icall (mono_isfinite, "mono_isfinite", "uint32 double", FALSE);
	}
#endif
	register_icall (mono_ckfinite, "mono_ckfinite", "double double", FALSE);

#ifdef COMPRESSED_INTERFACE_BITMAP
	register_icall (mono_class_interface_match, "mono_class_interface_match", "uint32 ptr int32", TRUE);
#endif

#if SIZEOF_REGISTER == 4
	register_opcode_emulation (OP_FCONV_TO_U, "__emul_fconv_to_u", "uint32 double", mono_fconv_u4, "mono_fconv_u4", TRUE);
#else
	register_opcode_emulation (OP_FCONV_TO_U, "__emul_fconv_to_u", "ulong double", mono_fconv_u8, "mono_fconv_u8", TRUE);
#endif

	/* other jit icalls */
	register_icall (mono_delegate_ctor, "mono_delegate_ctor", "void object object ptr", FALSE);
	register_icall (mono_class_static_field_address , "mono_class_static_field_address",
				 "ptr ptr ptr", FALSE);
	register_icall (mono_ldtoken_wrapper, "mono_ldtoken_wrapper", "ptr ptr ptr ptr", FALSE);
	register_icall (mono_ldtoken_wrapper_generic_shared, "mono_ldtoken_wrapper_generic_shared",
		"ptr ptr ptr ptr", FALSE);
	register_icall (mono_get_special_static_data, "mono_get_special_static_data", "ptr int", FALSE);
	register_icall (mono_ldstr, "mono_ldstr", "object ptr ptr int32", FALSE);
	register_icall (mono_helper_stelem_ref_check, "mono_helper_stelem_ref_check", "void object object", FALSE);
	register_icall (ves_icall_object_new, "ves_icall_object_new", "object ptr ptr", FALSE);
	register_icall (ves_icall_object_new_specific, "ves_icall_object_new_specific", "object ptr", FALSE);
	register_icall (mono_array_new, "mono_array_new", "object ptr ptr int32", FALSE);
	register_icall (ves_icall_array_new_specific, "ves_icall_array_new_specific", "object ptr int32", FALSE);
	register_icall (ves_icall_runtime_class_init, "ves_icall_runtime_class_init", "void ptr", FALSE);
	register_icall (mono_ldftn, "mono_ldftn", "ptr ptr", FALSE);
	register_icall (mono_ldvirtfn, "mono_ldvirtfn", "ptr object ptr", FALSE);
	register_icall (mono_ldvirtfn_gshared, "mono_ldvirtfn_gshared", "ptr object ptr", FALSE);
	register_icall (mono_helper_compile_generic_method, "mono_helper_compile_generic_method", "ptr object ptr ptr", FALSE);
	register_icall (mono_helper_ldstr, "mono_helper_ldstr", "object ptr int", FALSE);
	register_icall (mono_helper_ldstr_mscorlib, "mono_helper_ldstr_mscorlib", "object int", FALSE);
	register_icall (mono_helper_newobj_mscorlib, "mono_helper_newobj_mscorlib", "object int", FALSE);
	register_icall (mono_value_copy, "mono_value_copy", "void ptr ptr ptr", FALSE);
	register_icall (mono_object_castclass_unbox, "mono_object_castclass_unbox", "object object ptr", FALSE);
	register_icall (mono_break, "mono_break", NULL, TRUE);
	register_icall (mono_create_corlib_exception_0, "mono_create_corlib_exception_0", "object int", TRUE);
	register_icall (mono_create_corlib_exception_1, "mono_create_corlib_exception_1", "object int object", TRUE);
	register_icall (mono_create_corlib_exception_2, "mono_create_corlib_exception_2", "object int object object", TRUE);
	register_icall (mono_array_new_1, "mono_array_new_1", "object ptr int", FALSE);
	register_icall (mono_array_new_2, "mono_array_new_2", "object ptr int int", FALSE);
	register_icall (mono_array_new_3, "mono_array_new_3", "object ptr int int int", FALSE);
	register_icall (mono_array_new_4, "mono_array_new_4", "object ptr int int int int", FALSE);
	register_icall (mono_get_native_calli_wrapper, "mono_get_native_calli_wrapper", "ptr ptr ptr ptr", FALSE);
	register_icall (mono_resume_unwind, "mono_resume_unwind", "void", TRUE);
	register_icall (mono_gsharedvt_constrained_call, "mono_gsharedvt_constrained_call", "object ptr ptr ptr ptr ptr", FALSE);
	register_icall (mono_gsharedvt_value_copy, "mono_gsharedvt_value_copy", "void ptr ptr ptr", TRUE);

	register_icall (mono_gc_wbarrier_value_copy_bitmap, "mono_gc_wbarrier_value_copy_bitmap", "void ptr ptr int int", FALSE);

	register_icall (mono_object_castclass_with_cache, "mono_object_castclass_with_cache", "object object ptr ptr", FALSE);
	register_icall (mono_object_isinst_with_cache, "mono_object_isinst_with_cache", "object object ptr ptr", FALSE);
	register_icall (mono_generic_class_init, "mono_generic_class_init", "void ptr", FALSE);
	register_icall (mono_fill_class_rgctx, "mono_class_fill_rgctx", "ptr ptr int", FALSE);
	register_icall (mono_fill_method_rgctx, "mono_method_fill_rgctx", "ptr ptr int", FALSE);

	register_icall (mono_debugger_agent_user_break, "mono_debugger_agent_user_break", "void", FALSE);

	register_icall (mono_aot_init_llvm_method, "mono_aot_init_llvm_method", "void ptr int", TRUE);
	register_icall (mono_aot_init_gshared_method_this, "mono_aot_init_gshared_method_this", "void ptr int object", TRUE);
	register_icall (mono_aot_init_gshared_method_mrgctx, "mono_aot_init_gshared_method_mrgctx", "void ptr int ptr", TRUE);
	register_icall (mono_aot_init_gshared_method_vtable, "mono_aot_init_gshared_method_vtable", "void ptr int ptr", TRUE);

	register_icall_no_wrapper (mono_resolve_iface_call_gsharedvt, "mono_resolve_iface_call_gsharedvt", "ptr object int ptr ptr");
	register_icall_no_wrapper (mono_resolve_vcall_gsharedvt, "mono_resolve_vcall_gsharedvt", "ptr object int ptr ptr");
	register_icall_no_wrapper (mono_resolve_generic_virtual_call, "mono_resolve_generic_virtual_call", "ptr ptr int ptr");
	register_icall_no_wrapper (mono_resolve_generic_virtual_iface_call, "mono_resolve_generic_virtual_iface_call", "ptr ptr int ptr");
	/* This needs a wrapper so it can have a preserveall cconv */
	register_icall (mono_init_vtable_slot, "mono_init_vtable_slot", "ptr ptr int", FALSE);
	register_icall (mono_llvmonly_init_delegate, "mono_llvmonly_init_delegate", "void object", TRUE);
	register_icall (mono_llvmonly_init_delegate_virtual, "mono_llvmonly_init_delegate_virtual", "void object object ptr", TRUE);
	register_icall (mono_get_assembly_object, "mono_get_assembly_object", "object ptr", TRUE);
	register_icall (mono_get_method_object, "mono_get_method_object", "object ptr", TRUE);

	register_icall_with_wrapper (mono_monitor_enter, "mono_monitor_enter", "void obj");
	register_icall_with_wrapper (mono_monitor_enter_v4, "mono_monitor_enter_v4", "void obj ptr");
	register_icall_no_wrapper (mono_monitor_enter_fast, "mono_monitor_enter_fast", "int obj");
	register_icall_no_wrapper (mono_monitor_enter_v4_fast, "mono_monitor_enter_v4_fast", "int obj ptr");

#ifdef TARGET_IOS
	register_icall (pthread_getspecific, "pthread_getspecific", "ptr ptr", TRUE);
#endif
}

MonoJitStats mono_jit_stats = {0};

static void
print_jit_stats (void)
{
	if (mono_jit_stats.enabled) {
		g_print ("Mono Jit statistics\n");
		g_print ("Max code size ratio:    %.2f (%s)\n", mono_jit_stats.max_code_size_ratio/100.0,
				 mono_jit_stats.max_ratio_method);
		g_print ("Biggest method:         %ld (%s)\n", mono_jit_stats.biggest_method_size,
				 mono_jit_stats.biggest_method);

		g_print ("Delegates created:      %ld\n", mono_stats.delegate_creations);
		g_print ("Initialized classes:    %ld\n", mono_stats.initialized_class_count);
		g_print ("Used classes:           %ld\n", mono_stats.used_class_count);
		g_print ("Generic vtables:        %ld\n", mono_stats.generic_vtable_count);
		g_print ("Methods:                %ld\n", mono_stats.method_count);
		g_print ("Static data size:       %ld\n", mono_stats.class_static_data_size);
		g_print ("VTable data size:       %ld\n", mono_stats.class_vtable_size);
		g_print ("Mscorlib mempool size:  %d\n", mono_mempool_get_allocated (mono_defaults.corlib->mempool));

		g_print ("\nInitialized classes:    %ld\n", mono_stats.generic_class_count);
		g_print ("Inflated types:         %ld\n", mono_stats.inflated_type_count);
		g_print ("Generics virtual invokes: %ld\n", mono_jit_stats.generic_virtual_invocations);

		g_print ("Sharable generic methods: %ld\n", mono_stats.generics_sharable_methods);
		g_print ("Unsharable generic methods: %ld\n", mono_stats.generics_unsharable_methods);
		g_print ("Shared generic methods: %ld\n", mono_stats.generics_shared_methods);
		g_print ("Shared vtype generic methods: %ld\n", mono_stats.gsharedvt_methods);

		g_print ("IMT tables size:        %ld\n", mono_stats.imt_tables_size);
		g_print ("IMT number of tables:   %ld\n", mono_stats.imt_number_of_tables);
		g_print ("IMT number of methods:  %ld\n", mono_stats.imt_number_of_methods);
		g_print ("IMT used slots:         %ld\n", mono_stats.imt_used_slots);
		g_print ("IMT colliding slots:    %ld\n", mono_stats.imt_slots_with_collisions);
		g_print ("IMT max collisions:     %ld\n", mono_stats.imt_max_collisions_in_slot);
		g_print ("IMT methods at max col: %ld\n", mono_stats.imt_method_count_when_max_collisions);
		g_print ("IMT thunks size:        %ld\n", mono_stats.imt_thunks_size);

		g_print ("JIT info table inserts: %ld\n", mono_stats.jit_info_table_insert_count);
		g_print ("JIT info table removes: %ld\n", mono_stats.jit_info_table_remove_count);
		g_print ("JIT info table lookups: %ld\n", mono_stats.jit_info_table_lookup_count);

		g_free (mono_jit_stats.max_ratio_method);
		mono_jit_stats.max_ratio_method = NULL;
		g_free (mono_jit_stats.biggest_method);
		mono_jit_stats.biggest_method = NULL;
	}
}

void
mini_cleanup (MonoDomain *domain)
{
	mono_runtime_shutdown_stat_profiler ();

#ifndef DISABLE_COM
	cominterop_release_all_rcws ();
#endif

#ifndef MONO_CROSS_COMPILE
	/*
	 * mono_domain_finalize () needs to be called early since it needs the
	 * execution engine still fully working (it may invoke managed finalizers).
	 */
	mono_domain_finalize (domain, 2000);
#endif

	/* This accesses metadata so needs to be called before runtime shutdown */
	print_jit_stats ();

	mono_profiler_shutdown ();

#ifndef MONO_CROSS_COMPILE
	mono_runtime_cleanup (domain);
#endif

	free_jit_tls_data ((MonoJitTlsData *)mono_native_tls_get_value (mono_jit_tls_id));

	mono_icall_cleanup ();

	mono_runtime_cleanup_handlers ();

#ifndef MONO_CROSS_COMPILE
	mono_domain_free (domain, TRUE);
#endif

#ifdef ENABLE_LLVM
	if (mono_use_llvm)
		mono_llvm_cleanup ();
#endif

	mono_aot_cleanup ();

	mono_trampolines_cleanup ();

	mono_unwind_cleanup ();

	mono_code_manager_destroy (global_codeman);
	g_free (vtable_trampolines);

	mini_jit_cleanup ();

	mono_tramp_info_cleanup ();

	mono_arch_cleanup ();

	mono_generic_sharing_cleanup ();

	mono_cleanup ();

	mono_trace_cleanup ();

	mono_counters_dump (MONO_COUNTER_SECTION_MASK | MONO_COUNTER_MONOTONIC, stdout);

	if (mono_inject_async_exc_method)
		mono_method_desc_free (mono_inject_async_exc_method);

	mono_native_tls_free (mono_jit_tls_id);

	mono_os_mutex_destroy (&jit_mutex);

	mono_code_manager_cleanup ();

#ifdef USE_JUMP_TABLES
	mono_jumptable_cleanup ();
#endif
}

void
mono_set_defaults (int verbose_level, guint32 opts)
{
	mini_verbose = verbose_level;
	mono_set_optimizations (opts);
}

void
mono_disable_optimizations (guint32 opts)
{
	default_opt &= ~opts;
}

void
mono_set_optimizations (guint32 opts)
{
	default_opt = opts;
	default_opt_set = TRUE;
#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED
	mono_set_generic_sharing_vt_supported (mono_aot_only || ((default_opt & MONO_OPT_GSHAREDVT) != 0));
#else
	if (mono_llvm_only)
		mono_set_generic_sharing_vt_supported (TRUE);
#endif
}

void
mono_set_verbose_level (guint32 level)
{
	mini_verbose = level;
}

/**
 * mono_get_runtime_build_info:
 *
 * Return the runtime version + build date in string format.
 * The returned string is owned by the caller. The returned string
 * format is "VERSION (FULL_VERSION BUILD_DATE)" and build date is optional.
 */
char*
mono_get_runtime_build_info (void)
{
	if (mono_build_date)
		return g_strdup_printf ("%s (%s %s)", VERSION, FULL_VERSION, mono_build_date);
	else
		return g_strdup_printf ("%s (%s)", VERSION, FULL_VERSION);
}

static void
mono_precompile_assembly (MonoAssembly *ass, void *user_data)
{
	GHashTable *assemblies = (GHashTable*)user_data;
	MonoImage *image = mono_assembly_get_image (ass);
	MonoMethod *method, *invoke;
	int i, count = 0;

	if (g_hash_table_lookup (assemblies, ass))
		return;

	g_hash_table_insert (assemblies, ass, ass);

	if (mini_verbose > 0)
		printf ("PRECOMPILE: %s.\n", mono_image_get_filename (image));

	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_METHOD); ++i) {
		MonoError error;

		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, &error);
		if (!method) {
			mono_error_cleanup (&error); /* FIXME don't swallow the error */
			continue;
		}
		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT)
			continue;
		if (method->is_generic || method->klass->generic_container)
			continue;

		count++;
		if (mini_verbose > 1) {
			char * desc = mono_method_full_name (method, TRUE);
			g_print ("Compiling %d %s\n", count, desc);
			g_free (desc);
		}
		mono_compile_method (method);
		if (strcmp (method->name, "Finalize") == 0) {
			invoke = mono_marshal_get_runtime_invoke (method, FALSE);
			mono_compile_method (invoke);
		}
#ifndef DISABLE_REMOTING
		if (mono_class_is_marshalbyref (method->klass) && mono_method_signature (method)->hasthis) {
			invoke = mono_marshal_get_remoting_invoke_with_check (method);
			mono_compile_method (invoke);
		}
#endif
	}

	/* Load and precompile referenced assemblies as well */
	for (i = 0; i < mono_image_get_table_rows (image, MONO_TABLE_ASSEMBLYREF); ++i) {
		mono_assembly_load_reference (image, i);
		if (image->references [i])
			mono_precompile_assembly (image->references [i], assemblies);
	}
}

void mono_precompile_assemblies ()
{
	GHashTable *assemblies = g_hash_table_new (NULL, NULL);

	mono_assembly_foreach ((GFunc)mono_precompile_assembly, assemblies);

	g_hash_table_destroy (assemblies);
}

/*
 * Used by LLVM.
 * Have to export this for AOT.
 */
void
mono_personality (void)
{
	/* Not used */
	g_assert_not_reached ();
}

#ifdef USE_JUMP_TABLES
#define DEFAULT_JUMPTABLE_CHUNK_ELEMENTS 128

typedef struct MonoJumpTableChunk {
	guint32 total;
	guint32 active;
	struct MonoJumpTableChunk *previous;
	/* gpointer entries[total]; */
} MonoJumpTableChunk;

static MonoJumpTableChunk* g_jumptable;
#define mono_jumptable_lock() mono_os_mutex_lock (&jumptable_mutex)
#define mono_jumptable_unlock() mono_os_mutex_unlock (&jumptable_mutex)
static mono_mutex_t jumptable_mutex;

static  MonoJumpTableChunk*
mono_create_jumptable_chunk (guint32 max_entries)
{
	guint32 size = sizeof (MonoJumpTableChunk) + max_entries * sizeof(gpointer);
	MonoJumpTableChunk *chunk = (MonoJumpTableChunk*) g_new0 (guchar, size);
	chunk->total = max_entries;
	return chunk;
}

void
mono_jumptable_init (void)
{
	if (g_jumptable == NULL) {
		mono_os_mutex_init_recursive (&jumptable_mutex);
		g_jumptable = mono_create_jumptable_chunk (DEFAULT_JUMPTABLE_CHUNK_ELEMENTS);
	}
}

gpointer*
mono_jumptable_add_entry (void)
{
	return mono_jumptable_add_entries (1);
}

gpointer*
mono_jumptable_add_entries (guint32 entries)
{
	guint32 index;
	gpointer *result;

	mono_jumptable_init ();
	mono_jumptable_lock ();
	index = g_jumptable->active;
	if (index + entries >= g_jumptable->total) {
		/*
		 * Grow jumptable, by adding one more chunk.
		 * We cannot realloc jumptable, as there could be pointers
		 * to existing jump table entries in the code, so instead
		 * we just add one more chunk.
		 */
		guint32 max_entries = entries;
		MonoJumpTableChunk *new_chunk;

		if (max_entries < DEFAULT_JUMPTABLE_CHUNK_ELEMENTS)
			max_entries = DEFAULT_JUMPTABLE_CHUNK_ELEMENTS;
		new_chunk = mono_create_jumptable_chunk (max_entries);
		/* Link old jumptable, so that we could free it up later. */
		new_chunk->previous = g_jumptable;
		g_jumptable = new_chunk;
		index = 0;
	}
	g_jumptable->active = index + entries;
	result = (gpointer*)((guchar*)g_jumptable + sizeof(MonoJumpTableChunk)) + index;
	mono_jumptable_unlock();

	return result;
}

void
mono_jumptable_cleanup (void)
{
	if (g_jumptable) {
		MonoJumpTableChunk *current = g_jumptable, *prev;
		while (current != NULL) {
			prev = current->previous;
			g_free (current);
			current = prev;
		}
		g_jumptable = NULL;
		mono_os_mutex_destroy (&jumptable_mutex);
	}
}

gpointer*
mono_jumptable_get_entry (guint8 *code_ptr)
{
	return mono_arch_jumptable_entry_from_code (code_ptr);
}
#endif
