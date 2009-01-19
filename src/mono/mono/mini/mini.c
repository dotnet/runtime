/*
 * mini.c: The new Mono code generator.
 *
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2002 Ximian, Inc.
 */

#include <config.h>
#include <signal.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#ifdef PLATFORM_MACOSX
#include <mach/mach.h>
#include <mach/mach_error.h>
#include <mach/exception.h>
#include <mach/task.h>
#include <pthread.h>
#endif

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/mono-endian.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/socket-io.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/io-layer/io-layer.h>
#include "mono/metadata/profiler.h"
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/security-manager.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/security-core-clr.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/attach.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/dtrace.h>

#include "mini.h"
#include <string.h>
#include <ctype.h>
#include "inssel.h"
#include "trace.h"
#include "version.h"

#include "jit-icalls.h"

#include "aliasing.h"

#include "debug-mini.h"

#define BRANCH_COST 100
#define INLINE_LENGTH_LIMIT 20
#define INLINE_FAILURE do {\
		if ((cfg->method != method) && (method->wrapper_type == MONO_WRAPPER_NONE))\
			goto inline_failure;\
	} while (0)
#define CHECK_CFG_EXCEPTION do {\
		if (cfg->exception_type != MONO_EXCEPTION_NONE)\
			goto exception_exit;\
	} while (0)
#define METHOD_ACCESS_FAILURE do {	\
		char *method_fname = mono_method_full_name (method, TRUE);	\
		char *cil_method_fname = mono_method_full_name (cil_method, TRUE);	\
		cfg->exception_type = MONO_EXCEPTION_METHOD_ACCESS;	\
		cfg->exception_message = g_strdup_printf ("Method `%s' is inaccessible from method `%s'\n", cil_method_fname, method_fname);	\
		g_free (method_fname);	\
		g_free (cil_method_fname);	\
		goto exception_exit;	\
	} while (0)
#define FIELD_ACCESS_FAILURE do {	\
		char *method_fname = mono_method_full_name (method, TRUE);	\
		char *field_fname = mono_field_full_name (field);	\
		cfg->exception_type = MONO_EXCEPTION_FIELD_ACCESS;	\
		cfg->exception_message = g_strdup_printf ("Field `%s' is inaccessible from method `%s'\n", field_fname, method_fname);	\
		g_free (method_fname);	\
		g_free (field_fname);	\
		goto exception_exit;	\
	} while (0)
#define GENERIC_SHARING_FAILURE(opcode) do {		\
		if (cfg->generic_sharing_context) {	\
            if (cfg->verbose_level > 1) \
			    printf ("sharing failed for method %s.%s.%s/%d opcode %s line %d\n", method->klass->name_space, method->klass->name, method->name, method->signature->param_count, mono_opcode_name ((opcode)), __LINE__); \
			cfg->exception_type = MONO_EXCEPTION_GENERIC_SHARING_FAILED;	\
			goto exception_exit;	\
		}			\
	} while (0)
#define GET_RGCTX(rgctx, context_used) do {						\
		MonoInst *this = NULL;					\
		g_assert (context_used);				\
		if (!(method->flags & METHOD_ATTRIBUTE_STATIC) &&	\
				!((context_used) & MONO_GENERIC_CONTEXT_USED_METHOD) &&	\
				!method->klass->valuetype)		\
			NEW_ARGLOAD (cfg, this, 0);			\
		(rgctx) = get_runtime_generic_context (cfg, method, (context_used), this, ip); \
	} while (0)


#define MONO_CHECK_THIS(ins) (mono_method_signature (cfg->method)->hasthis && (ins)->ssa_op == MONO_SSA_LOAD && (ins)->inst_left->inst_c0 == 0)

static void setup_stat_profiler (void);
gboolean  mono_arch_print_tree(MonoInst *tree, int arity);
static gpointer mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt);
static gpointer mono_jit_compile_method (MonoMethod *method);
inline static int mono_emit_jit_icall (MonoCompile *cfg, MonoBasicBlock *bblock, gconstpointer func, MonoInst **args, const guint8 *ip);

static void handle_stobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, MonoInst *src, 
			  const unsigned char *ip, MonoClass *klass, gboolean to_end, gboolean native, gboolean write_barrier);

static void dec_foreach (MonoInst *tree, MonoCompile *cfg);

int mono_method_to_ir2 (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
		   MonoInst *return_var, GList *dont_inline, MonoInst **inline_args, 
		   guint inline_offset, gboolean is_virtual_call);

static int mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
		   int locals_offset, MonoInst *return_var, GList *dont_inline, MonoInst **inline_args, 
		   guint inline_offset, gboolean is_virtual_call);

#ifdef MONO_ARCH_SOFT_FLOAT
static void
handle_store_float (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *ptr, MonoInst *val, const unsigned char *ip);
#endif

/* helper methods signature */
/* FIXME: Make these static again */
MonoMethodSignature *helper_sig_class_init_trampoline = NULL;
MonoMethodSignature *helper_sig_domain_get = NULL;
MonoMethodSignature *helper_sig_generic_class_init_trampoline = NULL;
MonoMethodSignature *helper_sig_rgctx_lazy_fetch_trampoline = NULL;
MonoMethodSignature *helper_sig_monitor_enter_exit_trampoline = NULL;

static guint32 default_opt = 0;
static gboolean default_opt_set = FALSE;

guint32 mono_jit_tls_id = -1;

#ifdef HAVE_KW_THREAD
static __thread gpointer mono_jit_tls MONO_TLS_FAST;
#endif

MonoTraceSpec *mono_jit_trace_calls = NULL;
gboolean mono_break_on_exc = FALSE;
gboolean mono_compile_aot = FALSE;
/* If this is set, no code is generated dynamically, everything is taken from AOT files */
gboolean mono_aot_only = FALSE;
/* Whenever to use IMT */
#ifdef MONO_ARCH_HAVE_IMT
gboolean mono_use_imt = TRUE;
#else
gboolean mono_use_imt = FALSE;
#endif
MonoMethodDesc *mono_inject_async_exc_method = NULL;
int mono_inject_async_exc_pos;
MonoMethodDesc *mono_break_at_bb_method = NULL;
int mono_break_at_bb_bb_num;
gboolean mono_do_x86_stack_align = TRUE;
const char *mono_build_date;

static int mini_verbose = 0;

#define mono_jit_lock() EnterCriticalSection (&jit_mutex)
#define mono_jit_unlock() LeaveCriticalSection (&jit_mutex)
static CRITICAL_SECTION jit_mutex;

static MonoCodeManager *global_codeman = NULL;

/* FIXME: Make this static again */
GHashTable *jit_icall_name_hash = NULL;

static MonoDebugOptions debug_options;

#ifdef VALGRIND_JIT_REGISTER_MAP
static int valgrind_register = 0;
#endif

/*
 * Table written to by the debugger with a 1-based index into the
 * mono_breakpoint_info table, which contains changes made to
 * the JIT instructions by the debugger.
 */
gssize
mono_breakpoint_info_index [MONO_BREAKPOINT_ARRAY_SIZE];

/* Whenever to check for pending exceptions in managed-to-native wrappers */
gboolean check_for_pending_exc = TRUE;

/* Whenever to disable passing/returning small valuetypes in registers for managed methods */
gboolean disable_vtypes_in_regs = FALSE;

gboolean mono_dont_free_global_codeman;

#ifdef DISABLE_JIT
/* Define this here, since many files reference it */
const guint8 mono_burg_arity [MBMAX_OPCODES] = {
};
#endif

gboolean
mono_running_on_valgrind (void)
{
#ifdef HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND){
#ifdef VALGRIND_JIT_REGISTER_MAP
			valgrind_register = TRUE;
#endif
			return TRUE;
		} else
			return FALSE;
#else
		return FALSE;
#endif
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
	
	ji = mono_jit_info_table_find (domain, ip);
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
	}
	method = mono_method_full_name (ji->method, TRUE);
	/* FIXME: unused ? */
	location = mono_debug_lookup_source_location (ji->method, (guint32)((guint8*)ip - (guint8*)ji->code_start), domain);

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
void
mono_print_method_from_ip (void *ip)
{
	MonoJitInfo *ji;
	char *method;
	MonoDebugSourceLocation *source;
	MonoDomain *domain = mono_domain_get ();
	FindTrampUserData user_data;
	
	ji = mono_jit_info_table_find (domain, ip);
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
		}
		else
			g_print ("No method at %p\n", ip);
		return;
	}
	method = mono_method_full_name (ji->method, TRUE);
	source = mono_debug_lookup_source_location (ji->method, (guint32)((guint8*)ip - (guint8*)ji->code_start), domain);

	g_print ("IP %p at offset 0x%x of method %s (%p %p)[domain %p - %s]\n", ip, (int)((char*)ip - (char*)ji->code_start), method, ji->code_start, (char*)ji->code_start + ji->code_size, domain, domain->friendly_name);

	if (source)
		g_print ("%s:%d\n", source->source_file, source->row);

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
	if (!caller || !callee)
		return FALSE;

	/*
	 * If the call was made from domain-neutral to domain-specific 
	 * code, we can't patch the call site.
	 */
	if (caller->domain_neutral && !callee->domain_neutral)
		return FALSE;

	if ((caller->method->klass == mono_defaults.appdomain_class) &&
		(strstr (caller->method->name, "InvokeInDomain"))) {
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

MonoJumpInfoToken *
mono_jump_info_token_new2 (MonoMemPool *mp, MonoImage *image, guint32 token, MonoGenericContext *context)
{
	MonoJumpInfoToken *res = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoToken));
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

#define MONO_INIT_VARINFO(vi,id) do { \
	(vi)->range.first_use.pos.bid = 0xffff; \
	(vi)->reg = -1; \
        (vi)->idx = (id); \
} while (0)

//#define UNVERIFIED do { G_BREAKPOINT (); goto unverified; } while (0)
#define UNVERIFIED do { if (debug_options.break_on_unverified) G_BREAKPOINT (); else goto unverified; } while (0)

/*
 * Basic blocks have two numeric identifiers:
 * dfn: Depth First Number
 * block_num: unique ID assigned at bblock creation
 */
#define NEW_BBLOCK(cfg) (mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock)))
#define ADD_BBLOCK(cfg,b) do {	\
		cfg->cil_offset_to_bb [(b)->cil_code - cfg->cil_start] = (b);	\
		(b)->block_num = cfg->num_bblocks++;	\
		(b)->real_offset = real_offset;	\
	} while (0)

#define GET_BBLOCK(cfg,tblock,ip) do {	\
		(tblock) = cfg->cil_offset_to_bb [(ip) - cfg->cil_start]; \
		if (!(tblock)) {	\
			if ((ip) >= end || (ip) < header->code) UNVERIFIED; \
			(tblock) = NEW_BBLOCK (cfg);	\
			(tblock)->cil_code = (ip);	\
			ADD_BBLOCK (cfg, (tblock));	\
		} \
	} while (0)

#define CHECK_BBLOCK(target,ip,tblock) do {	\
		if ((target) < (ip) && !(tblock)->code)	{	\
			bb_recheck = g_list_prepend (bb_recheck, (tblock));	\
			if (cfg->verbose_level > 2) g_print ("queued block %d for check at IL%04x from IL%04x\n", (tblock)->block_num, (int)((target) - header->code), (int)((ip) - header->code));	\
		}	\
	} while (0)

#define NEW_ICONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_ICONST;	\
		(dest)->inst_c0 = (val);	\
		(dest)->type = STACK_I4;	\
	} while (0)

#define NEW_PCONST(cfg,dest,val) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_PCONST);	\
		(dest)->inst_p0 = (val);	\
		(dest)->type = STACK_PTR;	\
	} while (0)


#ifdef MONO_ARCH_NEED_GOT_VAR

#define NEW_PATCH_INFO(cfg,dest,el1,el2) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_PATCH_INFO);	\
		(dest)->inst_left = (gpointer)(el1);	\
		(dest)->inst_right = (gpointer)(el2);	\
	} while (0)

#define NEW_AOTCONST(cfg,dest,patch_type,cons) do {			\
		MONO_INST_NEW ((cfg), (dest), OP_NOP); \
		(dest)->opcode = cfg->compile_aot ? OP_GOT_ENTRY : OP_PCONST; \
		if (cfg->compile_aot) {					\
			MonoInst *group, *got_var, *got_loc;		\
			got_loc = mono_get_got_var (cfg);		\
			NEW_TEMPLOAD ((cfg), got_var, got_loc->inst_c0); \
			NEW_PATCH_INFO ((cfg), group, cons, patch_type); \
			(dest)->inst_p0 = got_var;			\
			(dest)->inst_p1 = group;			\
		} else {						\
			(dest)->inst_p0 = (cons);			\
			(dest)->inst_i1 = (gpointer)(patch_type);	\
		}							\
		(dest)->type = STACK_PTR;				\
	} while (0)

#define NEW_AOTCONST_TOKEN(cfg,dest,patch_type,image,token,stack_type,stack_class) do { \
		MonoInst *group, *got_var, *got_loc;			\
		MONO_INST_NEW ((cfg), (dest), OP_GOT_ENTRY); \
		got_loc = mono_get_got_var (cfg);			\
		NEW_TEMPLOAD ((cfg), got_var, got_loc->inst_c0);	\
		NEW_PATCH_INFO ((cfg), group, NULL, patch_type);	\
		group->inst_p0 = mono_jump_info_token_new ((cfg)->mempool, (image), (token)); \
		(dest)->inst_p0 = got_var;				\
		(dest)->inst_p1 = group;				\
		(dest)->type = (stack_type);			\
        (dest)->klass = (stack_class);          \
	} while (0)

#else

#define NEW_AOTCONST(cfg,dest,patch_type,cons) do {    \
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->opcode = cfg->compile_aot ? OP_AOTCONST : OP_PCONST;	\
		(dest)->inst_p0 = (cons);	\
		(dest)->inst_i1 = (gpointer)(patch_type); \
		(dest)->type = STACK_PTR;	\
    } while (0)

#define NEW_AOTCONST_TOKEN(cfg,dest,patch_type,image,token,stack_type,stack_class) do { \
		MONO_INST_NEW ((cfg), (dest), OP_AOTCONST);	\
		(dest)->inst_p0 = mono_jump_info_token_new ((cfg)->mempool, (image), (token));	\
		(dest)->inst_p1 = (gpointer)(patch_type); \
		(dest)->type = (stack_type);	\
        (dest)->klass = (stack_class);          \
    } while (0)

#endif

#define NEW_CLASSCONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_CLASS, (val))

#define NEW_IMAGECONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_IMAGE, (val))

#define NEW_FIELDCONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_FIELD, (val))

#define NEW_METHODCONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_METHODCONST, (val))

#define NEW_VTABLECONST(cfg,dest,vtable) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_VTABLE, cfg->compile_aot ? (gpointer)((vtable)->klass) : (vtable))

#define NEW_SFLDACONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_SFLDA, (val))

#define NEW_LDSTRCONST(cfg,dest,image,token) NEW_AOTCONST_TOKEN ((cfg), (dest), MONO_PATCH_INFO_LDSTR, (image), (token), STACK_OBJ, mono_defaults.string_class)

#define NEW_TYPE_FROM_HANDLE_CONST(cfg,dest,image,token) NEW_AOTCONST_TOKEN ((cfg), (dest), MONO_PATCH_INFO_TYPE_FROM_HANDLE, (image), (token), STACK_OBJ, mono_defaults.monotype_class)

#define NEW_LDTOKENCONST(cfg,dest,image,token) NEW_AOTCONST_TOKEN ((cfg), (dest), MONO_PATCH_INFO_LDTOKEN, (image), (token), STACK_PTR, NULL)

#define NEW_DECLSECCONST(cfg,dest,image,entry) do { \
		if (cfg->compile_aot) { \
			NEW_AOTCONST_TOKEN (cfg, dest, MONO_PATCH_INFO_DECLSEC, image, (entry).index, STACK_OBJ, NULL); \
		} else { \
			NEW_PCONST (cfg, args [0], (entry).blob); \
		} \
	} while (0)

#define NEW_DOMAINCONST(cfg,dest) do { \
		if (cfg->opt & MONO_OPT_SHARED) { \
			/* avoid depending on undefined C behavior in sequence points */ \
			MonoInst* __domain_var = mono_get_domainvar (cfg); \
			NEW_TEMPLOAD (cfg, dest, __domain_var->inst_c0); \
		} else { \
			NEW_PCONST (cfg, dest, (cfg)->domain); \
		} \
	} while (0)

#define GET_VARINFO_INST(cfg,num) ((cfg)->varinfo [(num)]->inst)

#define NEW_ARGLOAD(cfg,dest,num) do {	\
                if (arg_array [(num)]->opcode == OP_ICONST) (dest) = arg_array [(num)]; else { \
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->ssa_op = MONO_SSA_LOAD;	\
		(dest)->inst_i0 = arg_array [(num)];	\
		(dest)->opcode = mini_type_to_ldind ((cfg), (dest)->inst_i0->inst_vtype); \
		type_to_eval_stack_type ((cfg), param_types [(num)], (dest));	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	}} while (0)

#define NEW_LOCLOAD(cfg,dest,num) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->ssa_op = MONO_SSA_LOAD;	\
		(dest)->inst_i0 = (cfg)->varinfo [locals_offset + (num)];	\
		(dest)->opcode = mini_type_to_ldind ((cfg), (dest)->inst_i0->inst_vtype); \
		type_to_eval_stack_type ((cfg), header->locals [(num)], (dest));	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_LOCLOADA(cfg,dest,num) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_LDADDR);	\
		(dest)->ssa_op = MONO_SSA_ADDRESS_TAKEN;	\
		(dest)->inst_i0 = (cfg)->varinfo [locals_offset + (num)];	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
        if (!MONO_TYPE_ISSTRUCT (header->locals [(num)])) \
           (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_RETLOADA(cfg,dest) do {	\
        if (cfg->vret_addr) { \
		    MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		    (dest)->ssa_op = MONO_SSA_LOAD;	\
		    (dest)->inst_i0 = cfg->vret_addr; \
		    (dest)->opcode = mini_type_to_ldind ((cfg), (dest)->inst_i0->inst_vtype); \
            (dest)->type = STACK_MP; \
		    (dest)->klass = (dest)->inst_i0->klass;	\
        } else { \
			MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		    (dest)->ssa_op = MONO_SSA_ADDRESS_TAKEN;	\
		    (dest)->inst_i0 = (cfg)->ret;	\
		    (dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		    (dest)->opcode = cfg->ret_var_is_local ? OP_LDADDR : CEE_LDIND_I;	\
		    (dest)->type = STACK_MP;	\
		    (dest)->klass = (dest)->inst_i0->klass;	\
            (cfg)->disable_ssa = TRUE; \
        } \
	} while (0)

#define NEW_ARGLOADA(cfg,dest,num) do {	\
                if (arg_array [(num)]->opcode == OP_ICONST) goto inline_failure; \
		MONO_INST_NEW ((cfg), (dest), OP_LDADDR);	\
		(dest)->ssa_op = MONO_SSA_ADDRESS_TAKEN;	\
		(dest)->inst_i0 = arg_array [(num)];	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
                (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_TEMPLOAD(cfg,dest,num) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->ssa_op = MONO_SSA_LOAD;	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->opcode = mini_type_to_ldind ((cfg), (dest)->inst_i0->inst_vtype); \
		type_to_eval_stack_type ((cfg), (dest)->inst_i0->inst_vtype, (dest));	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_TEMPLOADA(cfg,dest,num) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_LDADDR);	\
		(dest)->ssa_op = MONO_SSA_ADDRESS_TAKEN;	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
        if (!MONO_TYPE_ISSTRUCT (cfg->varinfo [(num)]->inst_vtype)) \
           (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_INDLOAD(cfg,dest,addr,vtype) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->inst_left = addr;	\
		(dest)->opcode = mini_type_to_ldind ((cfg), vtype);	\
		type_to_eval_stack_type ((cfg), vtype, (dest));	\
		/* FIXME: (dest)->klass = (dest)->inst_i0->klass;*/	\
	} while (0)

#define NEW_INDSTORE(cfg,dest,addr,value,vtype) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->inst_i0 = addr;	\
		(dest)->opcode = mini_type_to_stind ((cfg), vtype);	\
		(dest)->inst_i1 = (value);	\
		/* FIXME: (dest)->klass = (dest)->inst_i0->klass;*/	\
	} while (0)

#define NEW_TEMPSTORE(cfg,dest,num,inst) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->ssa_op = MONO_SSA_STORE;	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->opcode = mini_type_to_stind ((cfg), (dest)->inst_i0->inst_vtype); \
		(dest)->inst_i1 = (inst);	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_LOCSTORE(cfg,dest,num,inst) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->opcode = mini_type_to_stind ((cfg), header->locals [(num)]); \
		(dest)->ssa_op = MONO_SSA_STORE;	\
		(dest)->inst_i0 = (cfg)->varinfo [locals_offset + (num)];	\
		(dest)->inst_i1 = (inst);	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_ARGSTORE(cfg,dest,num,inst) do {	\
                if (arg_array [(num)]->opcode == OP_ICONST) goto inline_failure; \
		MONO_INST_NEW ((cfg), (dest), OP_NOP);	\
		(dest)->opcode = mini_type_to_stind ((cfg), param_types [(num)]); \
		(dest)->ssa_op = MONO_SSA_STORE;	\
		(dest)->inst_i0 = arg_array [(num)];	\
		(dest)->inst_i1 = (inst);	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_MEMCPY(cfg,dest,dst,src,memcpy_size,memcpy_align) do { \
		MONO_INST_NEW (cfg, dest, OP_MEMCPY); \
        (dest)->inst_left = (dst); \
		(dest)->inst_right = (src); \
        (dest)->backend.memcpy_args = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoMemcpyArgs)); \
		(dest)->backend.memcpy_args->size = (memcpy_size); \
		(dest)->backend.memcpy_args->align = (memcpy_align); \
    } while (0)

#define NEW_MEMSET(cfg,dest,dst,imm,memcpy_size,memcpy_align) do { \
		MONO_INST_NEW (cfg, dest, OP_MEMSET); \
        (dest)->inst_left = (dst); \
		(dest)->inst_imm = (imm); \
        (dest)->backend.memcpy_args = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoMemcpyArgs)); \
		(dest)->backend.memcpy_args->size = (memcpy_size); \
		(dest)->backend.memcpy_args->align = (memcpy_align); \
    } while (0)

#define NEW_DUMMY_USE(cfg,dest,load) do { \
		MONO_INST_NEW ((cfg), (dest), OP_DUMMY_USE);	\
		(dest)->inst_left = (load); \
    } while (0)

#define NEW_DUMMY_STORE(cfg,dest,num) do { \
		MONO_INST_NEW ((cfg), (dest), OP_DUMMY_STORE);	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define ADD_BINOP(op) do {	\
		MONO_INST_NEW (cfg, ins, (op));	\
		sp -= 2;	\
		ins->inst_i0 = sp [0];	\
		ins->inst_i1 = sp [1];	\
		*sp++ = ins;	\
		type_from_op (ins);	\
		CHECK_TYPE (ins);	\
		/* Have to insert a widening op */		 \
		/* FIXME: Need to add many more cases */ \
		if (ins->inst_i0->type == STACK_PTR && ins->inst_i1->type == STACK_I4) { \
			MonoInst *widen;  \
			MONO_INST_NEW (cfg, widen, CEE_CONV_I); \
            widen->inst_i0 = ins->inst_i1; \
			ins->inst_i1 = widen; \
		} \
	} while (0)

#define ADD_UNOP(op) do {	\
		MONO_INST_NEW (cfg, ins, (op));	\
		sp--;	\
		ins->inst_i0 = sp [0];	\
		*sp++ = ins;	\
		type_from_op (ins);	\
		CHECK_TYPE (ins);	\
	} while (0)

#define ADD_BINCOND(next_block) do {	\
		MonoInst *cmp;	\
		sp -= 2;		\
		MONO_INST_NEW(cfg, cmp, OP_COMPARE);	\
		cmp->inst_i0 = sp [0];	\
		cmp->inst_i1 = sp [1];	\
		cmp->cil_code = ins->cil_code;	\
		type_from_op (cmp);	\
		CHECK_TYPE (cmp);	\
		ins->inst_i0 = cmp;	\
		MONO_ADD_INS (bblock, ins);	\
		ins->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof(gpointer)*2);	\
		GET_BBLOCK (cfg, tblock, target);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_true_bb = tblock;	\
		CHECK_BBLOCK (target, ip, tblock);	\
		if ((next_block)) {	\
			link_bblock (cfg, bblock, (next_block));	\
			ins->inst_false_bb = (next_block);	\
			start_new_bblock = 1;	\
		} else {	\
			GET_BBLOCK (cfg, tblock, ip);		\
			link_bblock (cfg, bblock, tblock);	\
			ins->inst_false_bb = tblock;	\
			start_new_bblock = 2;	\
		}	\
	} while (0)

/* FIXME: handle float, long ... */
#define ADD_UNCOND(istrue) do {	\
		MonoInst *cmp;	\
		sp--;		\
		MONO_INST_NEW(cfg, cmp, OP_COMPARE);	\
		cmp->inst_i0 = sp [0];	\
                switch (cmp->inst_i0->type) { \
		case STACK_I8: \
			cmp->inst_i1 = zero_int64; break; \
		case STACK_R8: \
			cmp->inst_i1 = zero_r8; break; \
		case STACK_PTR: \
		case STACK_MP: \
			cmp->inst_i1 = zero_ptr; break;	\
		case STACK_OBJ: \
			cmp->inst_i1 = zero_obj; break;	\
		default: \
			cmp->inst_i1 = zero_int32;  \
		}  \
		cmp->cil_code = ins->cil_code;	\
		type_from_op (cmp);	\
		CHECK_TYPE (cmp);	\
		ins->inst_i0 = cmp;	\
		ins->opcode = (istrue)? CEE_BNE_UN: CEE_BEQ;	\
		MONO_ADD_INS (bblock, ins);	\
		ins->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof(gpointer)*2);	\
		GET_BBLOCK (cfg, tblock, target);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_true_bb = tblock;	\
		CHECK_BBLOCK (target, ip, tblock);	\
		GET_BBLOCK (cfg, tblock, ip);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_false_bb = tblock;	\
		start_new_bblock = 2;	\
	} while (0)

#define NEW_LDELEMA(cfg,dest,sp,k) do {	\
		MONO_INST_NEW ((cfg), (dest), CEE_LDELEMA);	\
		(dest)->inst_left = (sp) [0];	\
		(dest)->inst_right = (sp) [1];	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (k);	\
		(cfg)->flags |= MONO_CFG_HAS_LDELEMA; \
	} while (0)

#define NEW_GROUP(cfg,dest,el1,el2) do {	\
		MONO_INST_NEW ((cfg), (dest), OP_GROUP);	\
		(dest)->inst_left = (el1);	\
		(dest)->inst_right = (el2);	\
	} while (0)

#if 0
static gint
compare_bblock (gconstpointer a, gconstpointer b)
{
	const MonoBasicBlock *b1 = a;
	const MonoBasicBlock *b2 = b;

	return b2->cil_code - b1->cil_code;
}
#endif

/* *
 * link_bblock: Links two basic blocks
 *
 * links two basic blocks in the control flow graph, the 'from'
 * argument is the starting block and the 'to' argument is the block
 * the control flow ends to after 'from'.
 */
static void
link_bblock (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to)
{
	MonoBasicBlock **newa;
	int i, found;

#if 0
	if (from->cil_code) {
		if (to->cil_code)
			g_print ("edge from IL%04x to IL_%04x\n", from->cil_code - cfg->cil_code, to->cil_code - cfg->cil_code);
		else
			g_print ("edge from IL%04x to exit\n", from->cil_code - cfg->cil_code);
	} else {
		if (to->cil_code)
			g_print ("edge from entry to IL_%04x\n", to->cil_code - cfg->cil_code);
		else
			g_print ("edge from entry to exit\n");
	}
#endif
	found = FALSE;
	for (i = 0; i < from->out_count; ++i) {
		if (to == from->out_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (!found) {
		newa = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * (from->out_count + 1));
		for (i = 0; i < from->out_count; ++i) {
			newa [i] = from->out_bb [i];
		}
		newa [i] = to;
		from->out_count++;
		from->out_bb = newa;
	}

	found = FALSE;
	for (i = 0; i < to->in_count; ++i) {
		if (from == to->in_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (!found) {
		newa = mono_mempool_alloc (cfg->mempool, sizeof (gpointer) * (to->in_count + 1));
		for (i = 0; i < to->in_count; ++i) {
			newa [i] = to->in_bb [i];
		}
		newa [i] = from;
		to->in_count++;
		to->in_bb = newa;
	}
}

/**
 * mono_unlink_bblock:
 *
 *   Unlink two basic blocks.
 */
void
mono_unlink_bblock (MonoCompile *cfg, MonoBasicBlock *from, MonoBasicBlock* to)
{
	int i, pos;
	gboolean found;

	found = FALSE;
	for (i = 0; i < from->out_count; ++i) {
		if (to == from->out_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (found) {
		pos = 0;
		for (i = 0; i < from->out_count; ++i) {
			if (from->out_bb [i] != to)
				from->out_bb [pos ++] = from->out_bb [i];
		}
		g_assert (pos == from->out_count - 1);
		from->out_count--;
	}

	found = FALSE;
	for (i = 0; i < to->in_count; ++i) {
		if (from == to->in_bb [i]) {
			found = TRUE;
			break;
		}
	}
	if (found) {
		pos = 0;
		for (i = 0; i < to->in_count; ++i) {
			if (to->in_bb [i] != from)
				to->in_bb [pos ++] = to->in_bb [i];
		}
		g_assert (pos == to->in_count - 1);
		to->in_count--;
	}
}

/*
 * mono_bblocks_linked:
 *
 *   Return whenever BB1 and BB2 are linked in the CFG.
 */
gboolean
mono_bblocks_linked (MonoBasicBlock *bb1, MonoBasicBlock *bb2)
{
	int i;

	for (i = 0; i < bb1->out_count; ++i) {
		if (bb1->out_bb [i] == bb2)
			return TRUE;
	}

	return FALSE;
}

/**
 * mono_find_block_region:
 *
 *   We mark each basic block with a region ID. We use that to avoid BB
 *   optimizations when blocks are in different regions.
 *
 * Returns:
 *   A region token that encodes where this region is, and information
 *   about the clause owner for this block.
 *
 *   The region encodes the try/catch/filter clause that owns this block
 *   as well as the type.  -1 is a special value that represents a block
 *   that is in none of try/catch/filter.
 */
static int
mono_find_block_region (MonoCompile *cfg, int offset)
{
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = mono_method_get_header (method);
	MonoExceptionClause *clause;
	int i;

	/* first search for handlers and filters */
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if ((clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) && (offset >= clause->data.filter_offset) &&
		    (offset < (clause->handler_offset)))
			return ((i + 1) << 8) | MONO_REGION_FILTER | clause->flags;
			   
		if (MONO_OFFSET_IN_HANDLER (clause, offset)) {
			if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY)
				return ((i + 1) << 8) | MONO_REGION_FINALLY | clause->flags;
			else if (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT)
				return ((i + 1) << 8) | MONO_REGION_FAULT | clause->flags;
			else if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE)
				return ((i + 1) << 8) | MONO_REGION_CATCH | clause->flags;
		}
	}

	/* search the try blocks */
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return ((i + 1) << 8) | clause->flags;
	}

	return -1;
}

static GList*
mono_find_final_block (MonoCompile *cfg, unsigned char *ip, unsigned char *target, int type)
{
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = mono_method_get_header (method);
	MonoExceptionClause *clause;
	MonoBasicBlock *handler;
	int i;
	GList *res = NULL;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, (ip - header->code)) && 
		    (!MONO_OFFSET_IN_CLAUSE (clause, (target - header->code)))) {
			if (clause->flags == type) {
				handler = cfg->cil_offset_to_bb [clause->handler_offset];
				g_assert (handler);
				res = g_list_append (res, handler);
			}
		}
	}
	return res;
}

MonoInst *
mono_find_spvar_for_region (MonoCompile *cfg, int region)
{
	return g_hash_table_lookup (cfg->spvars, GINT_TO_POINTER (region));
}

static void
mono_create_spvar_for_region (MonoCompile *cfg, int region)
{
	MonoInst *var;

	var = g_hash_table_lookup (cfg->spvars, GINT_TO_POINTER (region));
	if (var)
		return;

	var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
	/* prevent it from being register allocated */
	var->flags |= MONO_INST_INDIRECT;

	g_hash_table_insert (cfg->spvars, GINT_TO_POINTER (region), var);
}

static MonoInst *
mono_find_exvar_for_offset (MonoCompile *cfg, int offset)
{
	return g_hash_table_lookup (cfg->exvars, GINT_TO_POINTER (offset));
}

static MonoInst*
mono_create_exvar_for_offset (MonoCompile *cfg, int offset)
{
	MonoInst *var;

	var = g_hash_table_lookup (cfg->exvars, GINT_TO_POINTER (offset));
	if (var)
		return var;

	var = mono_compile_create_var (cfg, &mono_defaults.object_class->byval_arg, OP_LOCAL);
	/* prevent it from being register allocated */
	var->flags |= MONO_INST_INDIRECT;

	g_hash_table_insert (cfg->exvars, GINT_TO_POINTER (offset), var);

	return var;
}

static void
df_visit (MonoBasicBlock *start, int *dfn, MonoBasicBlock **array)
{
	int i;

	array [*dfn] = start;
	/* g_print ("visit %d at %p (BB%ld)\n", *dfn, start->cil_code, start->block_num); */
	for (i = 0; i < start->out_count; ++i) {
		if (start->out_bb [i]->dfn)
			continue;
		(*dfn)++;
		start->out_bb [i]->dfn = *dfn;
		start->out_bb [i]->df_parent = start;
		array [*dfn] = start->out_bb [i];
		df_visit (start->out_bb [i], dfn, array);
	}
}

static MonoBasicBlock*
find_previous (MonoBasicBlock **bblocks, guint32 n_bblocks, MonoBasicBlock *start, const guchar *code)
{
	MonoBasicBlock *best = start;
	int i;

	for (i = 0; i < n_bblocks; ++i) {
		if (bblocks [i]) {
			MonoBasicBlock *bb = bblocks [i];

			if (bb->cil_code && bb->cil_code < code && bb->cil_code > best->cil_code)
				best = bb;
		}
	}

	return best;
}

static void
split_bblock (MonoCompile *cfg, MonoBasicBlock *first, MonoBasicBlock *second) {
	int i, j;
	MonoInst *inst;
	MonoBasicBlock *bb;

	if (second->code)
		return;
	
	/* 
	 * FIXME: take into account all the details:
	 * second may have been the target of more than one bblock
	 */
	second->out_count = first->out_count;
	second->out_bb = first->out_bb;

	for (i = 0; i < first->out_count; ++i) {
		bb = first->out_bb [i];
		for (j = 0; j < bb->in_count; ++j) {
			if (bb->in_bb [j] == first)
				bb->in_bb [j] = second;
		}
	}

	first->out_count = 0;
	first->out_bb = NULL;
	link_bblock (cfg, first, second);

	second->last_ins = first->last_ins;

	/*g_print ("start search at %p for %p\n", first->cil_code, second->cil_code);*/
	MONO_BB_FOR_EACH_INS (first, inst) {
		/*char *code = mono_disasm_code_one (NULL, cfg->method, inst->next->cil_code, NULL);
		g_print ("found %p: %s", inst->next->cil_code, code);
		g_free (code);*/
		if (inst->cil_code < second->cil_code && inst->next->cil_code >= second->cil_code) {
			second->code = inst->next;
			inst->next = NULL;
			first->last_ins = inst;
			second->next_bb = first->next_bb;
			first->next_bb = second;
			return;
		}
	}
	if (!second->code) {
		g_warning ("bblock split failed in %s::%s\n", cfg->method->klass->name, cfg->method->name);
		//G_BREAKPOINT ();
	}
}

guint32
mono_reverse_branch_op (guint32 opcode)
{
	static const int reverse_map [] = {
		CEE_BNE_UN, CEE_BLT, CEE_BLE, CEE_BGT, CEE_BGE,
		CEE_BEQ, CEE_BLT_UN, CEE_BLE_UN, CEE_BGT_UN, CEE_BGE_UN
	};
	static const int reverse_fmap [] = {
		OP_FBNE_UN, OP_FBLT, OP_FBLE, OP_FBGT, OP_FBGE,
		OP_FBEQ, OP_FBLT_UN, OP_FBLE_UN, OP_FBGT_UN, OP_FBGE_UN
	};
	static const int reverse_lmap [] = {
		OP_LBNE_UN, OP_LBLT, OP_LBLE, OP_LBGT, OP_LBGE,
		OP_LBEQ, OP_LBLT_UN, OP_LBLE_UN, OP_LBGT_UN, OP_LBGE_UN
	};
	static const int reverse_imap [] = {
		OP_IBNE_UN, OP_IBLT, OP_IBLE, OP_IBGT, OP_IBGE,
		OP_IBEQ, OP_IBLT_UN, OP_IBLE_UN, OP_IBGT_UN, OP_IBGE_UN
	};
				
	if (opcode >= CEE_BEQ && opcode <= CEE_BLT_UN) {
		opcode = reverse_map [opcode - CEE_BEQ];
	} else if (opcode >= OP_FBEQ && opcode <= OP_FBLT_UN) {
		opcode = reverse_fmap [opcode - OP_FBEQ];
	} else if (opcode >= OP_LBEQ && opcode <= OP_LBLT_UN) {
		opcode = reverse_lmap [opcode - OP_LBEQ];
	} else if (opcode >= OP_IBEQ && opcode <= OP_IBLT_UN) {
		opcode = reverse_imap [opcode - OP_IBEQ];
	} else
		g_assert_not_reached ();

	return opcode;
}

guint
mono_type_to_store_membase (MonoCompile *cfg, MonoType *type)
{
	if (type->byref)
		return OP_STORE_MEMBASE_REG;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return OP_STOREI1_MEMBASE_REG;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return OP_STOREI2_MEMBASE_REG;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return OP_STOREI4_MEMBASE_REG;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return OP_STORE_MEMBASE_REG;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return OP_STORE_MEMBASE_REG;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_STOREI8_MEMBASE_REG;
	case MONO_TYPE_R4:
		return OP_STORER4_MEMBASE_REG;
	case MONO_TYPE_R8:
		return OP_STORER8_MEMBASE_REG;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		}
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type (type)))
			return OP_STOREX_MEMBASE;
		return OP_STOREV_MEMBASE;
	case MONO_TYPE_TYPEDBYREF:
		return OP_STOREV_MEMBASE;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* FIXME: all the arguments must be references for now,
		 * later look inside cfg and see if the arg num is
		 * really a reference
		 */
		g_assert (cfg->generic_sharing_context);
		return OP_STORE_MEMBASE_REG;
	default:
		g_error ("unknown type 0x%02x in type_to_store_membase", type->type);
	}
	return -1;
}

guint
mono_type_to_load_membase (MonoCompile *cfg, MonoType *type)
{
	if (type->byref)
		return OP_LOAD_MEMBASE;

	switch (mono_type_get_underlying_type (type)->type) {
	case MONO_TYPE_I1:
		return OP_LOADI1_MEMBASE;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return OP_LOADU1_MEMBASE;
	case MONO_TYPE_I2:
		return OP_LOADI2_MEMBASE;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return OP_LOADU2_MEMBASE;
	case MONO_TYPE_I4:
		return OP_LOADI4_MEMBASE;
	case MONO_TYPE_U4:
		return OP_LOADU4_MEMBASE;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return OP_LOAD_MEMBASE;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return OP_LOAD_MEMBASE;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return OP_LOADI8_MEMBASE;
	case MONO_TYPE_R4:
		return OP_LOADR4_MEMBASE;
	case MONO_TYPE_R8:
		return OP_LOADR8_MEMBASE;
	case MONO_TYPE_VALUETYPE:
		if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type (type)))
			return OP_LOADX_MEMBASE;
	case MONO_TYPE_TYPEDBYREF:
		return OP_LOADV_MEMBASE;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (type))
			return OP_LOADV_MEMBASE;
		else
			return OP_LOAD_MEMBASE;
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* FIXME: all the arguments must be references for now,
		 * later look inside cfg and see if the arg num is
		 * really a reference
		 */
		g_assert (cfg->generic_sharing_context);
		return OP_LOAD_MEMBASE;
	default:
		g_error ("unknown type 0x%02x in type_to_load_membase", type->type);
	}
	return -1;
}

#ifdef MONO_ARCH_SOFT_FLOAT
static int
condbr_to_fp_br (int opcode)
{
	switch (opcode) {
	case CEE_BEQ: return OP_FBEQ;
	case CEE_BGE: return OP_FBGE;
	case CEE_BGT: return OP_FBGT;
	case CEE_BLE: return OP_FBLE;
	case CEE_BLT: return OP_FBLT;
	case CEE_BNE_UN: return OP_FBNE_UN;
	case CEE_BGE_UN: return OP_FBGE_UN;
	case CEE_BGT_UN: return OP_FBGT_UN;
	case CEE_BLE_UN: return OP_FBLE_UN;
	case CEE_BLT_UN: return OP_FBLT_UN;
	}
	g_assert_not_reached ();
	return 0;
}
#endif

/*
 * The following tables are used to quickly validate the IL code in type_from_op ().
 */
static const char
bin_num_table [STACK_MAX] [STACK_MAX] = {
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I4,  STACK_INV, STACK_PTR, STACK_INV, STACK_MP,  STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_I8,  STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_PTR, STACK_INV, STACK_PTR, STACK_INV, STACK_MP,  STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_R8,  STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_MP,  STACK_INV, STACK_MP,  STACK_INV, STACK_PTR, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV}
};

static const char 
neg_table [] = {
	STACK_INV, STACK_I4, STACK_I8, STACK_PTR, STACK_R8, STACK_INV, STACK_INV, STACK_INV
};

/* reduce the size of this table */
static const char
bin_int_table [STACK_MAX] [STACK_MAX] = {
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I4,  STACK_INV, STACK_PTR, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_I8,  STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_PTR, STACK_INV, STACK_PTR, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV}
};

static const char
bin_comp_table [STACK_MAX] [STACK_MAX] = {
/*	Inv i  L  p  F  &  O  vt */
	{0},
	{0, 1, 0, 1, 0, 0, 0, 0}, /* i, int32 */
	{0, 0, 1, 0, 0, 0, 0, 0}, /* L, int64 */
	{0, 1, 0, 1, 0, 2, 4, 0}, /* p, ptr */
	{0, 0, 0, 0, 1, 0, 0, 0}, /* F, R8 */
	{0, 0, 0, 2, 0, 1, 0, 0}, /* &, managed pointer */
	{0, 0, 0, 4, 0, 0, 3, 0}, /* O, reference */
	{0, 0, 0, 0, 0, 0, 0, 0}, /* vt value type */
};

/* reduce the size of this table */
static const char
shift_table [STACK_MAX] [STACK_MAX] = {
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I4,  STACK_INV, STACK_I4,  STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_I8,  STACK_INV, STACK_I8,  STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_PTR, STACK_INV, STACK_PTR, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV},
	{STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV, STACK_INV}
};

/*
 * Tables to map from the non-specific opcode to the matching
 * type-specific opcode.
 */
/* handles from CEE_ADD to CEE_SHR_UN (CEE_REM_UN for floats) */
static const guint16
binops_op_map [STACK_MAX] = {
	0, 0, OP_LADD-CEE_ADD, OP_PADD-CEE_ADD, OP_FADD-CEE_ADD, OP_PADD-CEE_ADD
};

/* handles from CEE_NEG to CEE_CONV_U8 */
static const guint16
unops_op_map [STACK_MAX] = {
	0, 0, OP_LNEG-CEE_NEG, OP_PNEG-CEE_NEG, OP_FNEG-CEE_NEG, OP_PNEG-CEE_NEG
};

/* handles from CEE_CONV_U2 to CEE_SUB_OVF_UN */
static const guint16
ovfops_op_map [STACK_MAX] = {
	0, 0, OP_LCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2, OP_FCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2
};

/* handles from CEE_CONV_OVF_I1_UN to CEE_CONV_OVF_U_UN */
static const guint16
ovf2ops_op_map [STACK_MAX] = {
	0, 0, OP_LCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_PCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_FCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_PCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN
};

/* handles from CEE_CONV_OVF_I1 to CEE_CONV_OVF_U8 */
static const guint16
ovf3ops_op_map [STACK_MAX] = {
	0, 0, OP_LCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_PCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_FCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_PCONV_TO_OVF_I1-CEE_CONV_OVF_I1
};

/* handles from CEE_CEQ to CEE_CLT_UN */
static const guint16
ceqops_op_map [STACK_MAX] = {
	0, 0, OP_LCEQ-OP_CEQ, OP_PCEQ-OP_CEQ, OP_FCEQ-OP_CEQ, OP_LCEQ-OP_CEQ
};

/*
 * Sets ins->type (the type on the eval stack) according to the
 * type of the opcode and the arguments to it.
 * Invalid IL code is marked by setting ins->type to the invalid value STACK_INV.
 *
 * FIXME: this function sets ins->type unconditionally in some cases, but
 * it should set it to invalid for some types (a conv.x on an object)
 */
static void
type_from_op (MonoInst *ins) {
	switch (ins->opcode) {
	/* binops */
	case CEE_ADD:
	case CEE_SUB:
	case CEE_MUL:
	case CEE_DIV:
	case CEE_REM:
		/* FIXME: check unverifiable args for STACK_MP */
		ins->type = bin_num_table [ins->inst_i0->type] [ins->inst_i1->type];
		ins->opcode += binops_op_map [ins->type];
		return;
	case CEE_DIV_UN:
	case CEE_REM_UN:
	case CEE_AND:
	case CEE_OR:
	case CEE_XOR:
		ins->type = bin_int_table [ins->inst_i0->type] [ins->inst_i1->type];
		ins->opcode += binops_op_map [ins->type];
		return;
	case CEE_SHL:
	case CEE_SHR:
	case CEE_SHR_UN:
		ins->type = shift_table [ins->inst_i0->type] [ins->inst_i1->type];
		ins->opcode += binops_op_map [ins->type];
		return;
	case OP_COMPARE:
	case OP_LCOMPARE:
		/* FIXME: handle some specifics with ins->next->type */
		ins->type = bin_comp_table [ins->inst_i0->type] [ins->inst_i1->type] ? STACK_I4: STACK_INV;
		if ((ins->inst_i0->type == STACK_I8) || ((sizeof (gpointer) == 8) && ((ins->inst_i0->type == STACK_PTR) || (ins->inst_i0->type == STACK_OBJ) || (ins->inst_i0->type == STACK_MP))))
			ins->opcode = OP_LCOMPARE;
		return;
	case OP_CEQ:
		ins->type = bin_comp_table [ins->inst_i0->type] [ins->inst_i1->type] ? STACK_I4: STACK_INV;
		ins->opcode += ceqops_op_map [ins->inst_i0->type];
		return;
		
	case OP_CGT:
	case OP_CGT_UN:
	case OP_CLT:
	case OP_CLT_UN:
		ins->type = (bin_comp_table [ins->inst_i0->type] [ins->inst_i1->type] & 1) ? STACK_I4: STACK_INV;
		ins->opcode += ceqops_op_map [ins->inst_i0->type];
		return;
	/* unops */
	case CEE_NEG:
		ins->type = neg_table [ins->inst_i0->type];
		ins->opcode += unops_op_map [ins->type];
		return;
	case CEE_NOT:
		if (ins->inst_i0->type >= STACK_I4 && ins->inst_i0->type <= STACK_PTR)
			ins->type = ins->inst_i0->type;
		else
			ins->type = STACK_INV;
		ins->opcode += unops_op_map [ins->type];
		return;
	case CEE_CONV_I1:
	case CEE_CONV_I2:
	case CEE_CONV_I4:
	case CEE_CONV_U4:
		ins->type = STACK_I4;
		ins->opcode += unops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_R_UN:
		ins->type = STACK_R8;
		switch (ins->inst_i0->type) {
		case STACK_I4:
		case STACK_PTR:
			break;
		case STACK_I8:
			ins->opcode = OP_LCONV_TO_R_UN; 
			break;
		}
		return;
	case CEE_CONV_OVF_I1:
	case CEE_CONV_OVF_U1:
	case CEE_CONV_OVF_I2:
	case CEE_CONV_OVF_U2:
	case CEE_CONV_OVF_I4:
	case CEE_CONV_OVF_U4:
		ins->type = STACK_I4;
		ins->opcode += ovf3ops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_OVF_I_UN:
	case CEE_CONV_OVF_U_UN:
		ins->type = STACK_PTR;
		ins->opcode += ovf2ops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_OVF_I1_UN:
	case CEE_CONV_OVF_I2_UN:
	case CEE_CONV_OVF_I4_UN:
	case CEE_CONV_OVF_U1_UN:
	case CEE_CONV_OVF_U2_UN:
	case CEE_CONV_OVF_U4_UN:
		ins->type = STACK_I4;
		ins->opcode += ovf2ops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_U:
		ins->type = STACK_PTR;
		switch (ins->inst_i0->type) {
		case STACK_I4:
			break;
		case STACK_PTR:
		case STACK_MP:
#if SIZEOF_VOID_P == 8
			ins->opcode = OP_LCONV_TO_U;
#endif
			break;
		case STACK_I8:
			ins->opcode = OP_LCONV_TO_U;
			break;
		case STACK_R8:
			ins->opcode = OP_FCONV_TO_U;
			break;
		}
		return;
	case CEE_CONV_I8:
	case CEE_CONV_U8:
		ins->type = STACK_I8;
		ins->opcode += unops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_OVF_I8:
	case CEE_CONV_OVF_U8:
		ins->type = STACK_I8;
		ins->opcode += ovf3ops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_OVF_U8_UN:
	case CEE_CONV_OVF_I8_UN:
		ins->type = STACK_I8;
		ins->opcode += ovf2ops_op_map [ins->inst_i0->type];
		return;
	case CEE_CONV_R4:
	case CEE_CONV_R8:
		ins->type = STACK_R8;
		ins->opcode += unops_op_map [ins->inst_i0->type];
		return;
	case OP_CKFINITE:
		ins->type = STACK_R8;		
		return;
	case CEE_CONV_U2:
	case CEE_CONV_U1:
		ins->type = STACK_I4;
		ins->opcode += ovfops_op_map [ins->inst_i0->type];
		break;
	case CEE_CONV_I:
	case CEE_CONV_OVF_I:
	case CEE_CONV_OVF_U:
		ins->type = STACK_PTR;
		ins->opcode += ovfops_op_map [ins->inst_i0->type];
		return;
	case CEE_ADD_OVF:
	case CEE_ADD_OVF_UN:
	case CEE_MUL_OVF:
	case CEE_MUL_OVF_UN:
	case CEE_SUB_OVF:
	case CEE_SUB_OVF_UN:
		ins->type = bin_num_table [ins->inst_i0->type] [ins->inst_i1->type];
		ins->opcode += ovfops_op_map [ins->inst_i0->type];
		if (ins->type == STACK_R8)
			ins->type = STACK_INV;
		return;
	default:
		g_error ("opcode 0x%04x not handled in type from op", ins->opcode);
		break;
	}
}

static const char 
ldind_type [] = {
	STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I8, STACK_PTR, STACK_R8, STACK_R8, STACK_OBJ
};

/* map ldelem.x to the matching ldind.x opcode */
static const guchar
ldelem_to_ldind [] = {
	CEE_LDIND_I1,
	CEE_LDIND_U1,
	CEE_LDIND_I2,
	CEE_LDIND_U2,
	CEE_LDIND_I4,
	CEE_LDIND_U4,
	CEE_LDIND_I8,
	CEE_LDIND_I,
	CEE_LDIND_R4,
	CEE_LDIND_R8,
	CEE_LDIND_REF
};

/* map stelem.x to the matching stind.x opcode */
static const guchar
stelem_to_stind [] = {
	CEE_STIND_I,
	CEE_STIND_I1,
	CEE_STIND_I2,
	CEE_STIND_I4,
	CEE_STIND_I8,
	CEE_STIND_R4,
	CEE_STIND_R8,
	CEE_STIND_REF
};


#ifdef MONO_ARCH_SOFT_FLOAT
static void
handle_store_float (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *ptr, MonoInst *val, const unsigned char *ip)
{
	MonoInst *iargs [2];
	iargs [0] = val;
	iargs [1] = ptr;

	mono_emit_jit_icall (cfg, bblock, mono_fstore_r4, iargs, ip);
}

static int
handle_load_float (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *ptr, const unsigned char *ip)
{
	MonoInst *iargs [1];
	iargs [0] = ptr;

	return mono_emit_jit_icall (cfg, bblock, mono_fload_r4, iargs, ip);
}

#define LDLOC_SOFT_FLOAT(cfg,ins,idx,ip) do {\
		if (header->locals [(idx)]->type == MONO_TYPE_R4 && !header->locals [(idx)]->byref) {	\
			int temp;	\
			NEW_LOCLOADA (cfg, (ins), (idx));	\
			temp = handle_load_float (cfg, bblock, (ins), (ip));	\
			NEW_TEMPLOAD (cfg, (ins), temp);	\
		}	\
	} while (0)
#define STLOC_SOFT_FLOAT(cfg,ins,idx,ip) do {\
		if (header->locals [(idx)]->type == MONO_TYPE_R4 && !header->locals [(idx)]->byref) {	\
			NEW_LOCLOADA (cfg, (ins), (idx));	\
			handle_store_float (cfg, bblock, (ins), *sp, (ip));	\
			MONO_INST_NEW (cfg, (ins), OP_NOP);	\
		}	\
	} while (0)
#define LDARG_SOFT_FLOAT(cfg,ins,idx,ip) do {\
		if (param_types [(idx)]->type == MONO_TYPE_R4 && !param_types [(idx)]->byref) {	\
			int temp;	\
			NEW_ARGLOADA (cfg, (ins), (idx));	\
			temp = handle_load_float (cfg, bblock, (ins), (ip));	\
			NEW_TEMPLOAD (cfg, (ins), temp);	\
		}	\
	} while (0)
#define STARG_SOFT_FLOAT(cfg,ins,idx,ip) do {\
		if (param_types [(idx)]->type == MONO_TYPE_R4 && !param_types [(idx)]->byref) {	\
			NEW_ARGLOADA (cfg, (ins), (idx));	\
			handle_store_float (cfg, bblock, (ins), *sp, (ip));	\
			MONO_INST_NEW (cfg, (ins), OP_NOP);	\
		}	\
	} while (0)

#define NEW_TEMPLOAD_SOFT_FLOAT(cfg,bblock,ins,num,ip) do {		\
	if ((ins)->opcode == CEE_LDIND_R4) {						\
	    int idx = (num);										\
	    int temp;											\
	    NEW_TEMPLOADA (cfg, (ins), (idx));							\
		temp = handle_load_float (cfg, (bblock), (ins), ip);		\
		NEW_TEMPLOAD (cfg, (ins), (temp));							\
	}																\
	} while (0)

#define NEW_TEMPSTORE_SOFT_FLOAT(cfg,bblock,ins,num,val,ip) do {		\
	if ((ins)->opcode == CEE_STIND_R4) {								\
	    int idx = (num);										\
		NEW_TEMPLOADA (cfg, (ins), (idx)); \
		handle_store_float ((cfg), (bblock), (ins), (val), (ip));	\
	} \
	} while (0)

#else

#define LDLOC_SOFT_FLOAT(cfg,ins,idx,ip)
#define STLOC_SOFT_FLOAT(cfg,ins,idx,ip)
#define LDARG_SOFT_FLOAT(cfg,ins,idx,ip)
#define STARG_SOFT_FLOAT(cfg,ins,idx,ip)
#define NEW_TEMPLOAD_SOFT_FLOAT(cfg,bblock,ins,num,ip)
#define NEW_TEMPSTORE_SOFT_FLOAT(cfg,bblock,ins,num,val,ip)
#endif

#if 0

static const char
param_table [STACK_MAX] [STACK_MAX] = {
	{0},
};

static int
check_values_to_signature (MonoInst *args, MonoType *this, MonoMethodSignature *sig)
{
	int i;

	if (sig->hasthis) {
		switch (args->type) {
		case STACK_I4:
		case STACK_I8:
		case STACK_R8:
		case STACK_VTYPE:
		case STACK_INV:
			return 0;
		}
		args++;
	}
	for (i = 0; i < sig->param_count; ++i) {
		switch (args [i].type) {
		case STACK_INV:
			return 0;
		case STACK_MP:
			if (!sig->params [i]->byref)
				return 0;
			continue;
		case STACK_OBJ:
			if (sig->params [i]->byref)
				return 0;
			switch (sig->params [i]->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_STRING:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_ARRAY:
				break;
			default:
				return 0;
			}
			continue;
		case STACK_R8:
			if (sig->params [i]->byref)
				return 0;
			if (sig->params [i]->type != MONO_TYPE_R4 && sig->params [i]->type != MONO_TYPE_R8)
				return 0;
			continue;
		case STACK_PTR:
		case STACK_I4:
		case STACK_I8:
		case STACK_VTYPE:
			break;
		}
		/*if (!param_table [args [i].type] [sig->params [i]->type])
			return 0;*/
	}
	return 1;
}
#endif

static guint
mini_type_to_ldind (MonoCompile* cfg, MonoType *type)
{
	if (cfg->generic_sharing_context && !type->byref) {
		/* FIXME: all the arguments must be references for now,
		 * later look inside cfg and see if the arg num is
		 * really a reference
		 */
		if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)
			return CEE_LDIND_REF;
	}
	return mono_type_to_ldind (type);
}

guint
mini_type_to_stind (MonoCompile* cfg, MonoType *type)
{
	if (cfg->generic_sharing_context && !type->byref) {
		/* FIXME: all the arguments must be references for now,
		 * later look inside cfg and see if the arg num is
		 * really a reference
		 */
		if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR)
			return CEE_STIND_REF;
	}
	return mono_type_to_stind (type);
}

int
mono_op_imm_to_op (int opcode)
{
	switch (opcode) {
	case OP_ADD_IMM:
#if SIZEOF_VOID_P == 4
		return OP_IADD;
#else
		return OP_LADD;
#endif
	case OP_IADD_IMM:
		return OP_IADD;
	case OP_LADD_IMM:
		return OP_LADD;
	case OP_ISUB_IMM:
		return OP_ISUB;
	case OP_LSUB_IMM:
		return OP_LSUB;
	case OP_IMUL_IMM:
		return OP_IMUL;
	case OP_AND_IMM:
#if SIZEOF_VOID_P == 4
		return OP_IAND;
#else
		return OP_LAND;
#endif
	case OP_IAND_IMM:
		return OP_IAND;
	case OP_LAND_IMM:
		return OP_LAND;
	case OP_IOR_IMM:
		return OP_IOR;
	case OP_LOR_IMM:
		return OP_LOR;
	case OP_IXOR_IMM:
		return OP_IXOR;
	case OP_LXOR_IMM:
		return OP_LXOR;
	case OP_ISHL_IMM:
		return OP_ISHL;
	case OP_LSHL_IMM:
		return OP_LSHL;
	case OP_ISHR_IMM:
		return OP_ISHR;
	case OP_LSHR_IMM:
		return OP_LSHR;
	case OP_ISHR_UN_IMM:
		return OP_ISHR_UN;
	case OP_LSHR_UN_IMM:
		return OP_LSHR_UN;
	case OP_IDIV_IMM:
		return OP_IDIV;
	case OP_IDIV_UN_IMM:
		return OP_IDIV_UN;
	case OP_IREM_UN_IMM:
		return OP_IREM_UN;
	case OP_IREM_IMM:
		return OP_IREM;
	case OP_DIV_IMM:
#if SIZEOF_VOID_P == 4
		return OP_IDIV;
#else
		return OP_LDIV;
#endif
	case OP_REM_IMM:
#if SIZEOF_VOID_P == 4
		return OP_IREM;
#else
		return OP_LREM;
#endif
	case OP_ADDCC_IMM:
		return OP_ADDCC;
	case OP_ADC_IMM:
		return OP_ADC;
	case OP_SUBCC_IMM:
		return OP_SUBCC;
	case OP_SBB_IMM:
		return OP_SBB;
	case OP_IADC_IMM:
		return OP_IADC;
	case OP_ISBB_IMM:
		return OP_ISBB;
	case OP_COMPARE_IMM:
		return OP_COMPARE;
	case OP_ICOMPARE_IMM:
		return OP_ICOMPARE;
	case OP_LOCALLOC_IMM:
		return OP_LOCALLOC;
	default:
		printf ("%s\n", mono_inst_name (opcode));
		g_assert_not_reached ();
		return -1;
	}
}

/*
 * mono_decompose_op_imm:
 *
 *   Replace the OP_.._IMM INS with its non IMM variant.
 */
void
mono_decompose_op_imm (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins)
{
	MonoInst *temp;

	MONO_INST_NEW (cfg, temp, OP_ICONST);
	temp->inst_c0 = ins->inst_imm;
	temp->dreg = cfg->globalra ? mono_alloc_ireg (cfg) : mono_regstate_next_int (cfg->rs);
	mono_bblock_insert_before_ins (bb, ins, temp);
	ins->opcode = mono_op_imm_to_op (ins->opcode);
	if (ins->opcode == OP_LOCALLOC)
		ins->sreg1 = temp->dreg;
	else
		ins->sreg2 = temp->dreg;

	bb->max_vreg = MAX (bb->max_vreg, cfg->globalra ? cfg->next_vreg : cfg->rs->next_vreg);
}

/*
 * When we need a pointer to the current domain many times in a method, we
 * call mono_domain_get() once and we store the result in a local variable.
 * This function returns the variable that represents the MonoDomain*.
 */
inline static MonoInst *
mono_get_domainvar (MonoCompile *cfg)
{
	if (!cfg->domainvar)
		cfg->domainvar = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
	return cfg->domainvar;
}

/*
 * The got_var contains the address of the Global Offset Table when AOT 
 * compiling.
 */
inline static MonoInst *
mono_get_got_var (MonoCompile *cfg)
{
#ifdef MONO_ARCH_NEED_GOT_VAR
	if (!cfg->compile_aot)
		return NULL;
	if (!cfg->got_var) {
		cfg->got_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
	}
	return cfg->got_var;
#else
	return NULL;
#endif
}

static MonoInst *
mono_get_vtable_var (MonoCompile *cfg)
{
	g_assert (cfg->generic_sharing_context);

	if (!cfg->rgctx_var) {
		cfg->rgctx_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		/* force the var to be stack allocated */
		cfg->rgctx_var->flags |= MONO_INST_INDIRECT;
	}

	return cfg->rgctx_var;
}

static void
set_vreg_to_inst (MonoCompile *cfg, int vreg, MonoInst *inst)
{
	if (vreg >= cfg->vreg_to_inst_len) {
		MonoInst **tmp = cfg->vreg_to_inst;
		int size = cfg->vreg_to_inst_len;

		while (vreg >= cfg->vreg_to_inst_len)
			cfg->vreg_to_inst_len = cfg->vreg_to_inst_len ? cfg->vreg_to_inst_len * 2 : 32;
		cfg->vreg_to_inst = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * cfg->vreg_to_inst_len);
		if (size)
			memcpy (cfg->vreg_to_inst, tmp, size * sizeof (MonoInst*));
	}
	cfg->vreg_to_inst [vreg] = inst;
}

#define mono_type_is_long(type) (!(type)->byref && ((mono_type_get_underlying_type (type)->type == MONO_TYPE_I8) || (mono_type_get_underlying_type (type)->type == MONO_TYPE_U8)))
#define mono_type_is_float(type) (!(type)->byref && (((type)->type == MONO_TYPE_R8) || ((type)->type == MONO_TYPE_R4)))

MonoInst*
mono_compile_create_var_for_vreg (MonoCompile *cfg, MonoType *type, int opcode, int vreg)
{
	MonoInst *inst;
	int num = cfg->num_varinfo;
	gboolean regpair;

	if ((num + 1) >= cfg->varinfo_count) {
		int orig_count = cfg->varinfo_count;
		cfg->varinfo_count = cfg->varinfo_count ? (cfg->varinfo_count * 2) : 64;
		cfg->varinfo = (MonoInst **)g_realloc (cfg->varinfo, sizeof (MonoInst*) * cfg->varinfo_count);
		cfg->vars = (MonoMethodVar *)g_realloc (cfg->vars, sizeof (MonoMethodVar) * cfg->varinfo_count);
		memset (&cfg->vars [orig_count], 0, (cfg->varinfo_count - orig_count) * sizeof (MonoMethodVar));
	}

	mono_jit_stats.allocate_var++;

	MONO_INST_NEW (cfg, inst, opcode);
	inst->inst_c0 = num;
	inst->inst_vtype = type;
	inst->klass = mono_class_from_mono_type (type);
	type_to_eval_stack_type (cfg, type, inst);
	/* if set to 1 the variable is native */
	inst->backend.is_pinvoke = 0;
	inst->dreg = vreg;

	cfg->varinfo [num] = inst;

	MONO_INIT_VARINFO (&cfg->vars [num], num);

	if (vreg != -1)
		set_vreg_to_inst (cfg, vreg, inst);

#if SIZEOF_VOID_P == 4
#ifdef MONO_ARCH_SOFT_FLOAT
	regpair = mono_type_is_long (type) || mono_type_is_float (type);
#else
	regpair = mono_type_is_long (type);
#endif
#else
	regpair = FALSE;
#endif

	if (regpair) {
		MonoInst *tree;

		/* 
		 * These two cannot be allocated using create_var_for_vreg since that would
		 * put it into the cfg->varinfo array, confusing many parts of the JIT.
		 */

		/* 
		 * Set flags to VOLATILE so SSA skips it.
		 */

		if (cfg->verbose_level >= 4) {
			printf ("  Create LVAR R%d (R%d, R%d)\n", inst->dreg, inst->dreg + 1, inst->dreg + 2);
		}

#ifdef MONO_ARCH_SOFT_FLOAT
		if (cfg->opt & MONO_OPT_SSA) {
			if (mono_type_is_float (type))
				inst->flags = MONO_INST_VOLATILE;
		}
#endif

		/* Allocate a dummy MonoInst for the first vreg */
		MONO_INST_NEW (cfg, tree, OP_LOCAL);
		tree->dreg = inst->dreg + 1;
		if (cfg->opt & MONO_OPT_SSA)
			tree->flags = MONO_INST_VOLATILE;
		tree->inst_c0 = num;
		tree->type = STACK_I4;
		tree->inst_vtype = &mono_defaults.int32_class->byval_arg;
		tree->klass = mono_class_from_mono_type (tree->inst_vtype);

		set_vreg_to_inst (cfg, inst->dreg + 1, tree);

		/* Allocate a dummy MonoInst for the second vreg */
		MONO_INST_NEW (cfg, tree, OP_LOCAL);
		tree->dreg = inst->dreg + 2;
		if (cfg->opt & MONO_OPT_SSA)
			tree->flags = MONO_INST_VOLATILE;
		tree->inst_c0 = num;
		tree->type = STACK_I4;
		tree->inst_vtype = &mono_defaults.int32_class->byval_arg;
		tree->klass = mono_class_from_mono_type (tree->inst_vtype);

		set_vreg_to_inst (cfg, inst->dreg + 2, tree);
	}

	cfg->num_varinfo++;
	if (cfg->verbose_level > 2)
		g_print ("created temp %d (R%d) of type %s\n", num, vreg, mono_type_get_name (type));
	return inst;
}

MonoInst*
mono_compile_create_var (MonoCompile *cfg, MonoType *type, int opcode)
{
	int dreg;

	if (mono_type_is_long (type))
		dreg = mono_alloc_dreg (cfg, STACK_I8);
#ifdef MONO_ARCH_SOFT_FLOAT
	else if (mono_type_is_float (type))
		dreg = mono_alloc_dreg (cfg, STACK_R8);
#endif
	else
		/* All the others are unified */
		dreg = mono_alloc_preg (cfg);

	return mono_compile_create_var_for_vreg (cfg, type, opcode, dreg);
}

/*
 * Transform a MonoInst into a load from the variable of index var_index.
 */
void
mono_compile_make_var_load (MonoCompile *cfg, MonoInst *dest, gssize var_index) {
	memset (dest, 0, sizeof (MonoInst));
	dest->ssa_op = MONO_SSA_LOAD;
	dest->inst_i0 = cfg->varinfo [var_index];
	dest->opcode = mini_type_to_ldind (cfg, dest->inst_i0->inst_vtype);
	type_to_eval_stack_type (cfg, dest->inst_i0->inst_vtype, dest);
	dest->klass = dest->inst_i0->klass;
}

/*
 * Create a MonoInst that is a load from the variable of index var_index.
 */
MonoInst*
mono_compile_create_var_load (MonoCompile *cfg, gssize var_index) {
	MonoInst *dest;
	NEW_TEMPLOAD (cfg,dest,var_index);
	return dest;
}

/*
 * Create a MonoInst that is a store of the given value into the variable of index var_index.
 */
MonoInst*
mono_compile_create_var_store (MonoCompile *cfg, gssize var_index, MonoInst *value) {
	MonoInst *dest;
	NEW_TEMPSTORE (cfg, dest, var_index, value);
	return dest;
}

static MonoType*
type_from_stack_type (MonoInst *ins) {
	switch (ins->type) {
	case STACK_I4: return &mono_defaults.int32_class->byval_arg;
	case STACK_I8: return &mono_defaults.int64_class->byval_arg;
	case STACK_PTR: return &mono_defaults.int_class->byval_arg;
	case STACK_R8: return &mono_defaults.double_class->byval_arg;
	case STACK_MP:
		/* 
		 * this if used to be commented without any specific reason, but
		 * it breaks #80235 when commented
		 */
		if (ins->klass)
			return &ins->klass->this_arg;
		else
			return &mono_defaults.object_class->this_arg;
	case STACK_OBJ:
		/* ins->klass may not be set for ldnull.
		 * Also, if we have a boxed valuetype, we want an object lass,
		 * not the valuetype class
		 */
		if (ins->klass && !ins->klass->valuetype)
			return &ins->klass->byval_arg;
		return &mono_defaults.object_class->byval_arg;
	case STACK_VTYPE: return &ins->klass->byval_arg;
	default:
		g_error ("stack type %d to montype not handled\n", ins->type);
	}
	return NULL;
}

MonoType*
mono_type_from_stack_type (MonoInst *ins) {
	return type_from_stack_type (ins);
}

static MonoClass*
array_access_to_klass (int opcode, MonoInst *array_obj)
{
	switch (opcode) {
	case CEE_LDELEM_U1:
		return mono_defaults.byte_class;
	case CEE_LDELEM_U2:
		return mono_defaults.uint16_class;
	case CEE_LDELEM_I:
	case CEE_STELEM_I:
		return mono_defaults.int_class;
	case CEE_LDELEM_I1:
	case CEE_STELEM_I1:
		return mono_defaults.sbyte_class;
	case CEE_LDELEM_I2:
	case CEE_STELEM_I2:
		return mono_defaults.int16_class;
	case CEE_LDELEM_I4:
	case CEE_STELEM_I4:
		return mono_defaults.int32_class;
	case CEE_LDELEM_U4:
		return mono_defaults.uint32_class;
	case CEE_LDELEM_I8:
	case CEE_STELEM_I8:
		return mono_defaults.int64_class;
	case CEE_LDELEM_R4:
	case CEE_STELEM_R4:
		return mono_defaults.single_class;
	case CEE_LDELEM_R8:
	case CEE_STELEM_R8:
		return mono_defaults.double_class;
	case CEE_LDELEM_REF:
	case CEE_STELEM_REF: {
		MonoClass *klass = array_obj->klass;
		/* FIXME: add assert */
		if (klass && klass->rank)
			return klass->element_class;
		return mono_defaults.object_class;
	}
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

/*
 * mono_add_ins_to_end:
 *
 *   Same as MONO_ADD_INS, but add INST before any branches at the end of BB.
 */
void
mono_add_ins_to_end (MonoBasicBlock *bb, MonoInst *inst)
{
	int opcode;

	if (!bb->code) {
		MONO_ADD_INS (bb, inst);
		return;
	}

	switch (bb->last_ins->opcode) {
	case OP_BR:
	case OP_BR_REG:
	case CEE_BEQ:
	case CEE_BGE:
	case CEE_BGT:
	case CEE_BLE:
	case CEE_BLT:
	case CEE_BNE_UN:
	case CEE_BGE_UN:
	case CEE_BGT_UN:
	case CEE_BLE_UN:
	case CEE_BLT_UN:
	case OP_SWITCH:
		mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
		break;
	default:
		if (MONO_IS_COND_BRANCH_OP (bb->last_ins)) {
			/* Need to insert the ins before the compare */
			if (bb->code == bb->last_ins) {
				mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
				return;
			}

			if (bb->code->next == bb->last_ins) {
				/* Only two instructions */
				opcode = bb->code->opcode;

				if ((opcode == OP_COMPARE) || (opcode == OP_COMPARE_IMM) || (opcode == OP_ICOMPARE) || (opcode == OP_ICOMPARE_IMM) || (opcode == OP_FCOMPARE) || (opcode == OP_LCOMPARE) || (opcode == OP_LCOMPARE_IMM)) {
					/* NEW IR */
					mono_bblock_insert_before_ins (bb, bb->code, inst);
				} else {
					mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
				}
			} else {
				opcode = bb->last_ins->prev->opcode;

				if ((opcode == OP_COMPARE) || (opcode == OP_COMPARE_IMM) || (opcode == OP_ICOMPARE) || (opcode == OP_ICOMPARE_IMM) || (opcode == OP_FCOMPARE) || (opcode == OP_LCOMPARE) || (opcode == OP_LCOMPARE_IMM)) {
					/* NEW IR */
					mono_bblock_insert_before_ins (bb, bb->last_ins->prev, inst);
				} else {
					mono_bblock_insert_before_ins (bb, bb->last_ins, inst);
				}					
			}
		}
		else
			MONO_ADD_INS (bb, inst);
		break;
	}
}

void
mono_add_varcopy_to_end (MonoCompile *cfg, MonoBasicBlock *bb, int src, int dest)
{
	MonoInst *inst, *load;

	NEW_TEMPLOAD (cfg, load, src);

	NEW_TEMPSTORE (cfg, inst, dest, load);
	/* FIXME: handle CEE_STIND_R4 */
	if (inst->opcode == CEE_STOBJ) {
		NEW_TEMPLOADA (cfg, inst, dest);
		handle_stobj (cfg, bb, inst, load, NULL, inst->klass, TRUE, FALSE, FALSE);
	} else {
		inst->cil_code = NULL;
		mono_add_ins_to_end (bb, inst);
	}
}

/*
 * This function is called to handle items that are left on the evaluation stack
 * at basic block boundaries. What happens is that we save the values to local variables
 * and we reload them later when first entering the target basic block (with the
 * handle_loaded_temps () function).
 * It is also used to handle items on the stack in store opcodes, since it is
 * possible that the variable to be stored into is already on the stack, in
 * which case its old value should be used.
 * A single joint point will use the same variables (stored in the array bb->out_stack or
 * bb->in_stack, if the basic block is before or after the joint point).
 * If the stack merge fails at a join point, cfg->unverifiable is set.
 */
static void
handle_stack_args (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **sp, int count)
{
	int i, bindex;
	MonoBasicBlock *outb;
	MonoInst *inst, **locals;
	gboolean found;

	if (!count)
		return;
	if (cfg->verbose_level > 3)
		g_print ("%d item(s) on exit from B%d\n", count, bb->block_num);

	if (!bb->out_scount) {
		bb->out_scount = count;
		//g_print ("bblock %d has out:", bb->block_num);
		found = FALSE;
		for (i = 0; i < bb->out_count; ++i) {
			outb = bb->out_bb [i];
			/* exception handlers are linked, but they should not be considered for stack args */
			if (outb->flags & BB_EXCEPTION_HANDLER)
				continue;
			//g_print (" %d", outb->block_num);
			if (outb->in_stack) {
				found = TRUE;
				bb->out_stack = outb->in_stack;
				break;
			}
		}
		//g_print ("\n");
		if (!found) {
			bb->out_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * count);
			for (i = 0; i < count; ++i) {
				/* 
				 * try to reuse temps already allocated for this purpouse, if they occupy the same
				 * stack slot and if they are of the same type.
				 * This won't cause conflicts since if 'local' is used to 
				 * store one of the values in the in_stack of a bblock, then
				 * the same variable will be used for the same outgoing stack 
				 * slot as well. 
				 * This doesn't work when inlining methods, since the bblocks
				 * in the inlined methods do not inherit their in_stack from
				 * the bblock they are inlined to. See bug #58863 for an
				 * example.
				 * This hack is disabled since it also prevents proper tracking of types.
				 */
#if 1
				bb->out_stack [i] = mono_compile_create_var (cfg, type_from_stack_type (sp [i]), OP_LOCAL);
#else
				if (cfg->inlined_method)
					bb->out_stack [i] = mono_compile_create_var (cfg, type_from_stack_type (sp [i]), OP_LOCAL);
				else
					bb->out_stack [i] = mono_compile_get_interface_var (cfg, i, sp [i]);
#endif
			}
		}
	}

	for (i = 0; i < bb->out_count; ++i) {
		outb = bb->out_bb [i];
		/* exception handlers are linked, but they should not be considered for stack args */
		if (outb->flags & BB_EXCEPTION_HANDLER)
			continue;
		if (outb->in_scount) {
			if (outb->in_scount != bb->out_scount) {
				cfg->unverifiable = TRUE;
				return;
			}
			continue; /* check they are the same locals */
		}
		outb->in_scount = count;
		outb->in_stack = bb->out_stack;
	}

	locals = bb->out_stack;
	for (i = 0; i < count; ++i) {
		/* add store ops at the end of the bb, before the branch */
		NEW_TEMPSTORE (cfg, inst, locals [i]->inst_c0, sp [i]);
		if (inst->opcode == CEE_STOBJ) {
			NEW_TEMPLOADA (cfg, inst, locals [i]->inst_c0);
			handle_stobj (cfg, bb, inst, sp [i], sp [i]->cil_code, inst->klass, TRUE, FALSE, FALSE);
		} else {
			inst->cil_code = sp [i]->cil_code;
			mono_add_ins_to_end (bb, inst);
		}
		if (cfg->verbose_level > 3)
			g_print ("storing %d to temp %d\n", i, (int)locals [i]->inst_c0);
	}

	/*
	 * It is possible that the out bblocks already have in_stack assigned, and
	 * the in_stacks differ. In this case, we will store to all the different 
	 * in_stacks.
	 */

	found = TRUE;
	bindex = 0;
	while (found) {
		/* Find a bblock which has a different in_stack */
		found = FALSE;
		while (bindex < bb->out_count) {
			outb = bb->out_bb [bindex];
			/* exception handlers are linked, but they should not be considered for stack args */
			if (outb->flags & BB_EXCEPTION_HANDLER) {
				bindex++;
				continue;
			}
			if (outb->in_stack != locals) {
				/* 
				 * Instead of storing sp [i] to locals [i], we need to store
				 * locals [i] to <new locals>[i], since the sp [i] tree can't
				 * be shared between trees.
				 */
				for (i = 0; i < count; ++i)
					mono_add_varcopy_to_end (cfg, bb, locals [i]->inst_c0, outb->in_stack [i]->inst_c0);
				locals = outb->in_stack;
				found = TRUE;
				break;
			}
			bindex ++;
		}
	}
}

static int
ret_type_to_call_opcode (MonoType *type, int calli, int virt, MonoGenericSharingContext *gsctx)
{
	if (type->byref)
		return calli? OP_CALL_REG: virt? OP_CALLVIRT: OP_CALL;

handle_enum:
	type = mini_get_basic_type_from_generic (gsctx, type);
	switch (type->type) {
	case MONO_TYPE_VOID:
		return calli? OP_VOIDCALL_REG: virt? OP_VOIDCALLVIRT: OP_VOIDCALL;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return calli? OP_CALL_REG: virt? OP_CALLVIRT: OP_CALL;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return calli? OP_CALL_REG: virt? OP_CALLVIRT: OP_CALL;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return calli? OP_CALL_REG: virt? OP_CALLVIRT: OP_CALL;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return calli? OP_LCALL_REG: virt? OP_LCALLVIRT: OP_LCALL;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return calli? OP_FCALL_REG: virt? OP_FCALLVIRT: OP_FCALL;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		} else
			return calli? OP_VCALL_REG: virt? OP_VCALLVIRT: OP_VCALL;
	case MONO_TYPE_TYPEDBYREF:
		return calli? OP_VCALL_REG: virt? OP_VCALLVIRT: OP_VCALL;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in ret_type_to_call_opcode", type->type);
	}
	return -1;
}

void
mono_create_jump_table (MonoCompile *cfg, MonoInst *label, MonoBasicBlock **bbs, int num_blocks)
{
	MonoJumpInfo *ji = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));
	MonoJumpInfoBBTable *table;

	table = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfoBBTable));
	table->table = bbs;
	table->table_size = num_blocks;
	
	ji->ip.label = label;
	ji->type = MONO_PATCH_INFO_SWITCH;
	ji->data.table = table;
	ji->next = cfg->patch_info;
	cfg->patch_info = ji;
}

static void
mono_save_token_info (MonoCompile *cfg, MonoImage *image, guint32 token, gpointer key)
{
	if (cfg->compile_aot) {
		MonoJumpInfoToken *jump_info_token = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfoToken));
		jump_info_token->image = image;
		jump_info_token->token = token;
		g_hash_table_insert (cfg->token_info_hash, key, jump_info_token);
	}
}

/*
 * When we add a tree of instructions, we need to ensure the instructions currently
 * on the stack are executed before (like, if we load a value from a local).
 * We ensure this by saving the currently loaded values to temps and rewriting the
 * instructions to load the values.
 * This is not done for opcodes that terminate a basic block (because it's handled already
 * by handle_stack_args ()) and for opcodes that can't change values, like POP.
 */
static void
handle_loaded_temps (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst **stack, MonoInst **sp)
{
	MonoInst *load, *store, *temp, *ins;

	while (stack < sp) {
		ins = *stack;
		/* handle also other constants */
		if ((ins->opcode != OP_ICONST) &&
		    /* temps never get written to again, so we can safely avoid duplicating them */
		    !(ins->ssa_op == MONO_SSA_LOAD && ins->inst_i0->opcode == OP_LOCAL && ins->inst_i0->flags & MONO_INST_IS_TEMP)) {
			temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
			temp->flags |= MONO_INST_IS_TEMP;
			NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
			store->cil_code = ins->cil_code;
			if (store->opcode == CEE_STOBJ) {
				NEW_TEMPLOADA (cfg, store, temp->inst_c0);
				handle_stobj (cfg, bblock, store, ins, ins->cil_code, temp->klass, FALSE, FALSE, FALSE);
			} else
				MONO_ADD_INS (bblock, store);
			NEW_TEMPLOAD (cfg, load, temp->inst_c0);
			load->cil_code = ins->cil_code;
			*stack = load;
		}
		stack++;
	}
}

/*
 * target_type_is_incompatible:
 * @cfg: MonoCompile context
 *
 * Check that the item @arg on the evaluation stack can be stored
 * in the target type (can be a local, or field, etc).
 * The cfg arg can be used to check if we need verification or just
 * validity checks.
 *
 * Returns: non-0 value if arg can't be stored on a target.
 */
static int
target_type_is_incompatible (MonoCompile *cfg, MonoType *target, MonoInst *arg)
{
	MonoType *simple_type;
	MonoClass *klass;

	if (target->byref) {
		/* FIXME: check that the pointed to types match */
		if (arg->type == STACK_MP)
			return arg->klass != mono_class_from_mono_type (target);
		if (arg->type == STACK_PTR)
			return 0;
		return 1;
	}
	simple_type = mono_type_get_underlying_type (target);
	switch (simple_type->type) {
	case MONO_TYPE_VOID:
		return 1;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		if (arg->type != STACK_I4 && arg->type != STACK_PTR)
			return 1;
		return 0;
	case MONO_TYPE_PTR:
		/* STACK_MP is needed when setting pinned locals */
		if (arg->type != STACK_I4 && arg->type != STACK_PTR && arg->type != STACK_MP)
			return 1;
		return 0;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_FNPTR:
		if (arg->type != STACK_I4 && arg->type != STACK_PTR)
			return 1;
		return 0;
	case MONO_TYPE_OBJECT:
		if (arg->type != STACK_OBJ)
			return 1;
		return 0;
	case MONO_TYPE_STRING:
		if (arg->type != STACK_OBJ)
			return 1;
		/* ldnull has arg->klass unset */
		/*if (arg->klass && arg->klass != mono_defaults.string_class) {
			G_BREAKPOINT ();
			return 1;
		}*/
		return 0;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		if (arg->type != STACK_OBJ)
			return 1;
		/* FIXME: check type compatibility */
		return 0;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		if (arg->type != STACK_I8)
			return 1;
		return 0;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		if (arg->type != STACK_R8)
			return 1;
		return 0;
	case MONO_TYPE_VALUETYPE:
		if (arg->type != STACK_VTYPE)
			return 1;
		klass = mono_class_from_mono_type (simple_type);
		if (klass != arg->klass)
			return 1;
		return 0;
	case MONO_TYPE_TYPEDBYREF:
		if (arg->type != STACK_VTYPE)
			return 1;
		klass = mono_class_from_mono_type (simple_type);
		if (klass != arg->klass)
			return 1;
		return 0;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (simple_type)) {
			klass = mono_class_from_mono_type (simple_type);
			if (klass->enumtype)
				return target_type_is_incompatible (cfg, klass->enum_basetype, arg);
			if (arg->type != STACK_VTYPE)
				return 1;
			if (klass != arg->klass)
				return 1;
			return 0;
		} else {
			if (arg->type != STACK_OBJ)
				return 1;
			/* FIXME: check type compatibility */
			return 0;
		}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* FIXME: all the arguments must be references for now,
		 * later look inside cfg and see if the arg num is
		 * really a reference
		 */
		g_assert (cfg->generic_sharing_context);
		if (arg->type != STACK_OBJ)
			return 1;
		return 0;
	default:
		g_error ("unknown type 0x%02x in target_type_is_incompatible", simple_type->type);
	}
	return 1;
}

/*
 * Prepare arguments for passing to a function call.
 * Return a non-zero value if the arguments can't be passed to the given
 * signature.
 * The type checks are not yet complete and some conversions may need
 * casts on 32 or 64 bit architectures.
 *
 * FIXME: implement this using target_type_is_incompatible ()
 */
static int
check_call_signature (MonoCompile *cfg, MonoMethodSignature *sig, MonoInst **args)
{
	MonoType *simple_type;
	int i;

	if (sig->hasthis) {
		if (args [0]->type != STACK_OBJ && args [0]->type != STACK_MP && args [0]->type != STACK_PTR)
			return 1;
		args++;
	}
	for (i = 0; i < sig->param_count; ++i) {
		if (sig->params [i]->byref) {
			if (args [i]->type != STACK_MP && args [i]->type != STACK_PTR)
				return 1;
			continue;
		}
		simple_type = sig->params [i];
		simple_type = mini_get_basic_type_from_generic (cfg->generic_sharing_context, simple_type);
handle_enum:
		switch (simple_type->type) {
		case MONO_TYPE_VOID:
			return 1;
			continue;
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			if (args [i]->type != STACK_I4 && args [i]->type != STACK_PTR)
				return 1;
			continue;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
			if (args [i]->type != STACK_I4 && args [i]->type != STACK_PTR && args [i]->type != STACK_MP && args [i]->type != STACK_OBJ)
				return 1;
			continue;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:    
			if (args [i]->type != STACK_OBJ)
				return 1;
			continue;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			if (args [i]->type != STACK_I8)
				return 1;
			continue;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (args [i]->type != STACK_R8)
				return 1;
			continue;
		case MONO_TYPE_VALUETYPE:
			if (simple_type->data.klass->enumtype) {
				simple_type = simple_type->data.klass->enum_basetype;
				goto handle_enum;
			}
			if (args [i]->type != STACK_VTYPE)
				return 1;
			continue;
		case MONO_TYPE_TYPEDBYREF:
			if (args [i]->type != STACK_VTYPE)
				return 1;
			continue;
		case MONO_TYPE_GENERICINST:
			simple_type = &simple_type->data.generic_class->container_class->byval_arg;
			goto handle_enum;

		default:
			g_error ("unknown type 0x%02x in check_call_signature",
				 simple_type->type);
		}
	}
	return 0;
}

inline static int
mono_spill_call (MonoCompile *cfg, MonoBasicBlock *bblock, MonoCallInst *call, MonoMethodSignature *sig, gboolean ret_object, 
		 const guint8 *ip, gboolean to_end)
{
	MonoInst *temp, *store, *ins = (MonoInst*)call;
	MonoType *ret = sig->ret;

	if (!MONO_TYPE_IS_VOID (ret) || ret_object) {
		if (ret_object) {
			call->inst.type = STACK_OBJ;
			call->inst.opcode = OP_CALL;
			temp = mono_compile_create_var (cfg, &mono_defaults.string_class->byval_arg, OP_LOCAL);
		} else {
			type_to_eval_stack_type (cfg, ret, ins);
			temp = mono_compile_create_var (cfg, ret, OP_LOCAL);
		}
		
		temp->flags |= MONO_INST_IS_TEMP;

		if (MONO_TYPE_ISSTRUCT (ret)) {
			MonoInst *loada, *dummy_store;

			/* 
			 * Emit a dummy store to the local holding the result so the
			 * liveness info remains correct.
			 */
			NEW_DUMMY_STORE (cfg, dummy_store, temp->inst_c0);
			if (to_end)
				mono_add_ins_to_end (bblock, dummy_store);
			else
				MONO_ADD_INS (bblock, dummy_store);

			/* we use this to allocate native sized structs */
			temp->backend.is_pinvoke = sig->pinvoke;

			NEW_TEMPLOADA (cfg, loada, temp->inst_c0);
			if (call->inst.opcode == OP_VCALL || call->inst.opcode == OP_VCALL_RGCTX)
				ins->inst_left = loada;
			else
				ins->inst_right = loada; /* a virtual or indirect call */

			if (to_end)
				mono_add_ins_to_end (bblock, ins);
			else
				MONO_ADD_INS (bblock, ins);
		} else {
			NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
			store->cil_code = ip;
			
#ifdef MONO_ARCH_SOFT_FLOAT
			if (store->opcode == CEE_STIND_R4) {
				/*FIXME implement proper support for to_end*/
				g_assert (!to_end);
				NEW_TEMPLOADA (cfg, store, temp->inst_c0);
				handle_store_float (cfg, bblock, store, ins, ip);
			} else
#endif
			if (to_end)
				mono_add_ins_to_end (bblock, store);
			else
				MONO_ADD_INS (bblock, store);
		}
		return temp->inst_c0;
	} else {
		if (to_end)
			mono_add_ins_to_end (bblock, ins);
		else
			MONO_ADD_INS (bblock, ins);
		return -1;
	}
}

inline static MonoCallInst *
mono_emit_call_args (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig, 
		     MonoInst **args, int calli, int virtual, const guint8 *ip, gboolean to_end)
{
	MonoCallInst *call;
	MonoInst *arg;

	MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (sig->ret, calli, virtual, cfg->generic_sharing_context));

#ifdef MONO_ARCH_SOFT_FLOAT
	/* we need to convert the r4 value to an int value */
	{
		int i;
		for (i = 0; i < sig->param_count; ++i) {
			if (!sig->params [i]->byref && sig->params [i]->type == MONO_TYPE_R4) {
				MonoInst *iargs [1];
				int temp;
				iargs [0] = args [i + sig->hasthis];

				temp = mono_emit_jit_icall (cfg, bblock, mono_fload_r4_arg, iargs, ip);
				NEW_TEMPLOAD (cfg, arg, temp);
				args [i + sig->hasthis] = arg;
			}
		}
	}
#endif

	call->inst.cil_code = ip;
	call->args = args;
	call->signature = sig;
	call = mono_arch_call_opcode (cfg, bblock, call, virtual);
	type_to_eval_stack_type (cfg, sig->ret, &call->inst);

	for (arg = call->out_args; arg;) {
		MonoInst *narg = arg->next;
		arg->next = NULL;
		if (!arg->cil_code)
			arg->cil_code = ip;
		if (to_end)
			mono_add_ins_to_end (bblock, arg);
		else
			MONO_ADD_INS (bblock, arg);
		arg = narg;
	}
	return call;
}

inline static MonoCallInst*
mono_emit_calli (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig, 
		 MonoInst **args, MonoInst *addr, const guint8 *ip)
{
	MonoCallInst *call = mono_emit_call_args (cfg, bblock, sig, args, TRUE, FALSE, ip, FALSE);

	call->inst.inst_i0 = addr;

	return call;
}

inline static MonoCallInst*
mono_emit_rgctx_calli (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig,
	MonoInst **args, MonoInst *addr, MonoInst *rgctx_arg, const guint8 *ip)
{
	MonoCallInst *call = mono_emit_calli (cfg, bblock, sig, args, addr, ip);

	if (rgctx_arg) {
		switch (call->inst.opcode) {
		case OP_CALL_REG: call->inst.opcode = OP_CALL_REG_RGCTX; break;
		case OP_VOIDCALL_REG: call->inst.opcode = OP_VOIDCALL_REG_RGCTX; break;
		case OP_FCALL_REG: call->inst.opcode = OP_FCALL_REG_RGCTX; break;
		case OP_LCALL_REG: call->inst.opcode = OP_LCALL_REG_RGCTX; break;
		case OP_VCALL_REG: {
			MonoInst *group;

			NEW_GROUP (cfg, group, call->inst.inst_left, NULL);
			call->inst.inst_left = group;
			call->inst.opcode = OP_VCALL_REG_RGCTX;
			break;
		}
		default: g_assert_not_reached ();
		}

		if (call->inst.opcode != OP_VCALL_REG_RGCTX) {
			g_assert (!call->inst.inst_right);
			call->inst.inst_right = rgctx_arg;
		} else {
			g_assert (!call->inst.inst_left->inst_right);
			call->inst.inst_left->inst_right = rgctx_arg;
		}
	}

	return call;
}

inline static int
mono_emit_calli_spilled (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig, 
						 MonoInst **args, MonoInst *addr, const guint8 *ip)
{
	MonoCallInst *call = mono_emit_calli (cfg, bblock, sig, args, addr, ip);

	return mono_spill_call (cfg, bblock, call, sig, FALSE, ip, FALSE);
}

static int
mono_emit_rgctx_calli_spilled (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig,
	MonoInst **args, MonoInst *addr, MonoInst *rgctx_arg, const guint8 *ip)
{
	MonoCallInst *call = mono_emit_rgctx_calli (cfg, bblock, sig, args, addr, rgctx_arg, ip);

	return mono_spill_call (cfg, bblock, call, sig, FALSE, ip, FALSE);
}

static MonoCallInst*
mono_emit_method_call_full (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method, MonoMethodSignature *sig,
		       MonoInst **args, const guint8 *ip, MonoInst *this, gboolean to_end)
{
	gboolean virtual = this != NULL;
	MonoCallInst *call;

	call = mono_emit_call_args (cfg, bblock, sig, args, FALSE, virtual, ip, to_end);

	if (this && sig->hasthis && 
	    (method->klass->marshalbyref || method->klass == mono_defaults.object_class) && 
	    !(method->flags & METHOD_ATTRIBUTE_VIRTUAL) && !MONO_CHECK_THIS (this)) {
		call->method = mono_marshal_get_remoting_invoke_with_check (method);
	} else {
		call->method = method;
	}
	call->inst.flags |= MONO_INST_HAS_METHOD;
	call->inst.inst_left = this;

	if (call->method->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
		/* Needed by the code generated in inssel.brg */
		mono_get_got_var (cfg);

	return call;
}

static MonoCallInst*
mono_emit_method_call (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method, MonoMethodSignature *sig,
		       MonoInst **args, const guint8 *ip, MonoInst *this)
{
	return mono_emit_method_call_full (cfg, bblock, method, sig, args, ip, this, FALSE);
}

inline static int
mono_emit_method_call_spilled (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method,  
		       MonoMethodSignature *signature, MonoInst **args, const guint8 *ip, MonoInst *this)
{
	MonoCallInst *call = mono_emit_method_call (cfg, bblock, method, signature, args, ip, this);

	return mono_spill_call (cfg, bblock, call, signature, method->string_ctor, ip, FALSE);
}

inline static int
mono_emit_method_call_spilled_full (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method,  
		       MonoMethodSignature *signature, MonoInst **args, const guint8 *ip, MonoInst *this,
		       gboolean ret_object, gboolean to_end)
{
	MonoCallInst *call = mono_emit_method_call_full (cfg, bblock, method, signature, args, ip, this, to_end);

	return mono_spill_call (cfg, bblock, call, signature, ret_object, ip, to_end);
}

inline static int
mono_emit_native_call (MonoCompile *cfg, MonoBasicBlock *bblock, gconstpointer func, MonoMethodSignature *sig,
		       MonoInst **args, const guint8 *ip, gboolean ret_object, gboolean to_end)
{
	MonoCallInst *call;

	g_assert (sig);

	call = mono_emit_call_args (cfg, bblock, sig, args, FALSE, FALSE, ip, to_end);
	call->fptr = func;

	return mono_spill_call (cfg, bblock, call, sig, ret_object, ip, to_end);
}

inline static int
mono_emit_jit_icall (MonoCompile *cfg, MonoBasicBlock *bblock, gconstpointer func, MonoInst **args, const guint8 *ip)
{
	MonoJitICallInfo *info = mono_find_jit_icall_by_addr (func);
	
	if (!info) {
		g_warning ("unregistered JIT ICall");
		g_assert_not_reached ();
	}

	return mono_emit_native_call (cfg, bblock, mono_icall_get_wrapper (info), info->sig, args, ip, FALSE, FALSE);
}

static MonoCallInst*
mono_emit_rgctx_method_call (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method, MonoMethodSignature *sig,
		MonoInst **args, MonoInst *rgctx_arg, MonoInst *imt_arg, const guint8 *ip, MonoInst *this)
{
	MonoCallInst *call = mono_emit_method_call_full (cfg, bblock, method, sig, args, ip, this, FALSE);

	g_assert (!(rgctx_arg && imt_arg));

	if (rgctx_arg) {
		switch (call->inst.opcode) {
		case OP_CALL: call->inst.opcode = OP_CALL_RGCTX; break;
		case OP_VOIDCALL: call->inst.opcode = OP_VOIDCALL_RGCTX; break;
		case OP_FCALL: call->inst.opcode = OP_FCALL_RGCTX; break;
		case OP_LCALL: call->inst.opcode = OP_LCALL_RGCTX; break;
		case OP_VCALL: call->inst.opcode = OP_VCALL_RGCTX; break;
		default: g_assert_not_reached ();
		}

		if (call->inst.opcode != OP_VCALL_RGCTX) {
			g_assert (!call->inst.inst_left);
			call->inst.inst_left = rgctx_arg;
		} else {
			g_assert (!call->inst.inst_right);
			call->inst.inst_right = rgctx_arg;
		}
	} else if (imt_arg) {
		switch (call->inst.opcode) {
		case OP_CALLVIRT: call->inst.opcode = OP_CALLVIRT_IMT; break;
		case OP_VOIDCALLVIRT: call->inst.opcode = OP_VOIDCALLVIRT_IMT; break;
		case OP_FCALLVIRT: call->inst.opcode = OP_FCALLVIRT_IMT; break;
		case OP_LCALLVIRT: call->inst.opcode = OP_LCALLVIRT_IMT; break;
		case OP_VCALLVIRT: {
			MonoInst *group;

			NEW_GROUP (cfg, group, call->inst.inst_left, NULL);
			call->inst.inst_left = group;
			call->inst.opcode = OP_VCALLVIRT_IMT;
			break;
		}
		default: g_assert_not_reached ();
		}

		if (call->inst.opcode != OP_VCALLVIRT_IMT) {
			g_assert (!call->inst.inst_right);
			call->inst.inst_right = imt_arg;
		} else {
			g_assert (!call->inst.inst_left->inst_right);
			call->inst.inst_left->inst_right = imt_arg;
		}
	}

	return call;
}

inline static int
mono_emit_rgctx_method_call_spilled (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method,  
		MonoMethodSignature *signature, MonoInst **args, MonoInst *rgctx_arg, MonoInst *imt_arg,
		const guint8 *ip, MonoInst *this)
{
	MonoCallInst *call = mono_emit_rgctx_method_call (cfg, bblock, method, signature, args, rgctx_arg, imt_arg, ip, this);

	return mono_spill_call (cfg, bblock, call, signature, method->string_ctor, ip, FALSE);
}

static void
mono_emulate_opcode (MonoCompile *cfg, MonoInst *tree, MonoInst **iargs, MonoJitICallInfo *info)
{
	MonoInst *ins, *temp = NULL, *store, *load, *begin;
	MonoInst *last_arg = NULL;
	int nargs;
	MonoCallInst *call;

	//g_print ("emulating: ");
	//mono_print_tree_nl (tree);
	MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (info->sig->ret, FALSE, FALSE, cfg->generic_sharing_context));
	ins = (MonoInst*)call;
	
	call->inst.cil_code = tree->cil_code;
	call->args = iargs;
	call->signature = info->sig;

	call = mono_arch_call_opcode (cfg, cfg->cbb, call, FALSE);

	if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
		temp = mono_compile_create_var (cfg, info->sig->ret, OP_LOCAL);
		temp->flags |= MONO_INST_IS_TEMP;
		NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
		/* FIXME: handle CEE_STIND_R4 */
		store->cil_code = tree->cil_code;
	} else {
		store = ins;
	}

	nargs = info->sig->param_count + info->sig->hasthis;

	for (last_arg = call->out_args; last_arg && last_arg->next; last_arg = last_arg->next) ;

	if (nargs)
		last_arg->next = store;

	if (nargs)
		begin = call->out_args;
	else
		begin = store;

	if (cfg->prev_ins) {
		/* 
		 * This assumes that that in a tree, emulate_opcode is called for a
		 * node before it is called for its children. dec_foreach needs to
		 * take this into account.
		 */
		store->next = cfg->prev_ins->next;
		cfg->prev_ins->next = begin;
	} else {
		store->next = cfg->cbb->code;
		cfg->cbb->code = begin;
	}

	call->fptr = mono_icall_get_wrapper (info);

	if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
		NEW_TEMPLOAD (cfg, load, temp->inst_c0);
		*tree = *load;
	}
}

/*
 * This entry point could be used later for arbitrary method
 * redirection.
 */
inline static int
mini_redirect_call (int *temp, MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method,  
		       MonoMethodSignature *signature, MonoInst **args, const guint8 *ip, MonoInst *this)
{

	if (method->klass == mono_defaults.string_class) {
		/* managed string allocation support */
		if (strcmp (method->name, "InternalAllocateStr") == 0) {
			MonoInst *iargs [2];
			MonoVTable *vtable = mono_class_vtable (cfg->domain, method->klass);
			MonoMethod *managed_alloc = mono_gc_get_managed_allocator (vtable, FALSE);
			if (!managed_alloc)
				return FALSE;
			NEW_VTABLECONST (cfg, iargs [0], vtable);
			iargs [1] = args [0];
			*temp = mono_emit_method_call_spilled (cfg, bblock, managed_alloc, mono_method_signature (managed_alloc), iargs, ip, this);
			return TRUE;
		}
	}
	return FALSE;
}

static MonoMethodSignature *
mono_get_array_new_va_signature (int arity)
{
	static GHashTable *sighash = NULL;
	MonoMethodSignature *res;
	int i;

	mono_jit_lock ();
	if (!sighash) {
		sighash = g_hash_table_new (NULL, NULL);
	}
	else if ((res = g_hash_table_lookup (sighash, GINT_TO_POINTER (arity)))) {
		mono_jit_unlock ();
		return res;
	}

	res = mono_metadata_signature_alloc (mono_defaults.corlib, arity + 1);

	res->pinvoke = 1;
#ifdef MONO_ARCH_VARARG_ICALLS
	/* Only set this only some archs since not all backends can handle varargs+pinvoke */
	res->call_convention = MONO_CALL_VARARG;
#endif

#ifdef PLATFORM_WIN32
	res->call_convention = MONO_CALL_C;
#endif

	res->params [0] = &mono_defaults.int_class->byval_arg;	
	for (i = 0; i < arity; i++)
		res->params [i + 1] = &mono_defaults.int_class->byval_arg;

	res->ret = &mono_defaults.object_class->byval_arg;

	g_hash_table_insert (sighash, GINT_TO_POINTER (arity), res);
	mono_jit_unlock ();

	return res;
}

MonoJitICallInfo *
mono_get_array_new_va_icall (int rank)
{
	MonoMethodSignature *esig;
	char icall_name [256];
	char *name;
	MonoJitICallInfo *info;

	/* Need to register the icall so it gets an icall wrapper */
	sprintf (icall_name, "ves_array_new_va_%d", rank);

	mono_jit_lock ();
	info = mono_find_jit_icall_by_name (icall_name);
	if (info == NULL) {
		esig = mono_get_array_new_va_signature (rank);
		name = g_strdup (icall_name);
		info = mono_register_jit_icall (mono_array_new_va, name, esig, FALSE);

		g_hash_table_insert (jit_icall_name_hash, name, name);
	}
	mono_jit_unlock ();

	return info;
}

static MonoMethod*
get_memcpy_method (void)
{
	static MonoMethod *memcpy_method = NULL;
	if (!memcpy_method) {
		memcpy_method = mono_class_get_method_from_name (mono_defaults.string_class, "memcpy", 3);
		if (!memcpy_method)
			g_error ("Old corlib found. Install a new one");
	}
	return memcpy_method;
}

static void
handle_stobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, MonoInst *src, const unsigned char *ip, MonoClass *klass, gboolean to_end, gboolean native, gboolean write_barrier) {
	MonoInst *iargs [3];
	int n;
	guint32 align = 0;
	MonoMethod *memcpy_method;

	g_assert (klass);
	/*
	 * This check breaks with spilled vars... need to handle it during verification anyway.
	 * g_assert (klass && klass == src->klass && klass == dest->klass);
	 */

	if (native)
		n = mono_class_native_size (klass, &align);
	else
		n = mono_class_value_size (klass, &align);

#if HAVE_WRITE_BARRIERS
	/* if native is true there should be no references in the struct */
	if (write_barrier && klass->has_references && !native) {
		iargs [0] = dest;
		iargs [1] = src;
		NEW_PCONST (cfg, iargs [2], klass);

		mono_emit_jit_icall (cfg, bblock, mono_value_copy, iargs, ip);
		return;
	}
#endif

	/* FIXME: add write barrier handling */
	if ((cfg->opt & MONO_OPT_INTRINS) && !to_end && n <= sizeof (gpointer) * 5) {
		MonoInst *inst;
		if (dest->opcode == OP_LDADDR) {
			/* Keep liveness info correct */
			NEW_DUMMY_STORE (cfg, inst, dest->inst_i0->inst_c0);
			MONO_ADD_INS (bblock, inst);
		}
		NEW_MEMCPY (cfg, inst, dest, src, n, align);
		MONO_ADD_INS (bblock, inst);
		return;
	}
	iargs [0] = dest;
	iargs [1] = src;
	NEW_ICONST (cfg, iargs [2], n);

	memcpy_method = get_memcpy_method ();
	mono_emit_method_call_spilled_full (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL, FALSE, to_end);
}

static MonoMethod*
get_memset_method (void)
{
	static MonoMethod *memset_method = NULL;
	if (!memset_method) {
		memset_method = mono_class_get_method_from_name (mono_defaults.string_class, "memset", 3);
		if (!memset_method)
			g_error ("Old corlib found. Install a new one");
	}
	return memset_method;
}

static void
handle_initobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, const guchar *ip, MonoClass *klass, MonoInst **stack_start, MonoInst **sp)
{
	MonoInst *iargs [3];
	MonoInst *ins, *zero_int32;
	int n;
	guint32 align;
	MonoMethod *memset_method;

	NEW_ICONST (cfg, zero_int32, 0);

	mono_class_init (klass);
	n = mono_class_value_size (klass, &align);
	MONO_INST_NEW (cfg, ins, 0);
	ins->cil_code = ip;
	ins->inst_left = dest;
	ins->inst_right = zero_int32;
	if (n == 1) {
		ins->opcode = CEE_STIND_I1;
		MONO_ADD_INS (bblock, ins);
	} else if ((n == 2) && (align >= 2)) {
		ins->opcode = CEE_STIND_I2;
		MONO_ADD_INS (bblock, ins);
	} else if ((n == 2) && (align >= 4)) {
		ins->opcode = CEE_STIND_I4;
		MONO_ADD_INS (bblock, ins);
	} else if (n <= sizeof (gpointer) * 5) {
		NEW_MEMSET (cfg, ins, dest, 0, n, align);
		MONO_ADD_INS (bblock, ins);
	} else {
		memset_method = get_memset_method ();
		handle_loaded_temps (cfg, bblock, stack_start, sp);
		iargs [0] = dest;
		NEW_ICONST (cfg, iargs [1], 0);
		NEW_ICONST (cfg, iargs [2], n);
		mono_emit_method_call_spilled (cfg, bblock, memset_method, memset_method->signature, iargs, ip, NULL);
	}
}

static int
handle_alloc (MonoCompile *cfg, MonoBasicBlock *bblock, MonoClass *klass, gboolean for_box, const guchar *ip)
{
	MonoInst *iargs [2];
	void *alloc_ftn;

	if (cfg->opt & MONO_OPT_SHARED) {
		NEW_DOMAINCONST (cfg, iargs [0]);
		NEW_CLASSCONST (cfg, iargs [1], klass);

		alloc_ftn = mono_object_new;
	} else if (cfg->compile_aot && bblock->out_of_line && klass->type_token && klass->image == mono_defaults.corlib) {
		/* This happens often in argument checking code, eg. throw new FooException... */
		/* Avoid relocations by calling a helper function specialized to mscorlib */
		NEW_ICONST (cfg, iargs [0], mono_metadata_token_index (klass->type_token));
		return mono_emit_jit_icall (cfg, bblock, mono_helper_newobj_mscorlib, iargs, ip);
	} else {
		MonoVTable *vtable = mono_class_vtable (cfg->domain, klass);
		MonoMethod *managed_alloc = mono_gc_get_managed_allocator (vtable, for_box);
		gboolean pass_lw;

		if (managed_alloc) {
			NEW_VTABLECONST (cfg, iargs [0], vtable);
			return mono_emit_method_call_spilled (cfg, bblock, managed_alloc, mono_method_signature (managed_alloc), iargs, ip, NULL);
		}
		alloc_ftn = mono_class_get_allocation_ftn (vtable, for_box, &pass_lw);
		if (pass_lw) {
			guint32 lw = vtable->klass->instance_size;
			lw = ((lw + (sizeof (gpointer) - 1)) & ~(sizeof (gpointer) - 1)) / sizeof (gpointer);
			NEW_ICONST (cfg, iargs [0], lw);
			NEW_VTABLECONST (cfg, iargs [1], vtable);
		}
		else
			NEW_VTABLECONST (cfg, iargs [0], vtable);
	}

	return mono_emit_jit_icall (cfg, bblock, alloc_ftn, iargs, ip);
}

static int
handle_alloc_from_inst (MonoCompile *cfg, MonoBasicBlock *bblock, MonoClass *klass, MonoInst *data_inst,
		gboolean for_box, const guchar *ip)
{
	MonoInst *iargs [2];
	MonoMethod *managed_alloc = NULL;
	void *alloc_ftn;
	/*
	  FIXME: we cannot get managed_alloc here because we can't get
	  the class's vtable (because it's not a closed class)

	MonoVTable *vtable = mono_class_vtable (cfg->domain, klass);
	MonoMethod *managed_alloc = mono_gc_get_managed_allocator (vtable, for_box);
	*/

	if (cfg->opt & MONO_OPT_SHARED) {
		NEW_DOMAINCONST (cfg, iargs [0]);
		iargs [1] = data_inst;
		alloc_ftn = mono_object_new;
	} else {
		g_assert (!cfg->compile_aot);

		if (managed_alloc) {
			iargs [0] = data_inst;
			return mono_emit_method_call_spilled (cfg, bblock, managed_alloc,
				mono_method_signature (managed_alloc), iargs, ip, NULL);
		}

		iargs [0] = data_inst;
		alloc_ftn = mono_object_new_specific;
	}

	return mono_emit_jit_icall (cfg, bblock, alloc_ftn, iargs, ip);
}

static MonoInst*
handle_box_copy (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *val, const guchar *ip, MonoClass *klass, int temp)
{
	MonoInst *dest, *vtoffset, *add, *vstore;

	NEW_TEMPLOAD (cfg, dest, temp);
	NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
	MONO_INST_NEW (cfg, add, OP_PADD);
	add->inst_left = dest;
	add->inst_right = vtoffset;
	add->cil_code = ip;
	add->klass = klass;
	MONO_INST_NEW (cfg, vstore, CEE_STIND_I);
	vstore->opcode = mini_type_to_stind (cfg, &klass->byval_arg);
	vstore->cil_code = ip;
	vstore->inst_left = add;
	vstore->inst_right = val;

#ifdef MONO_ARCH_SOFT_FLOAT
	if (vstore->opcode == CEE_STIND_R4) {
		handle_store_float (cfg, bblock, add, val, ip);
	} else
#endif
	if (vstore->opcode == CEE_STOBJ) {
		handle_stobj (cfg, bblock, add, val, ip, klass, FALSE, FALSE, TRUE);
	} else
		MONO_ADD_INS (bblock, vstore);

	NEW_TEMPLOAD (cfg, dest, temp);
	return dest;
}

static MonoInst *
handle_box (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *val, const guchar *ip, MonoClass *klass)
{
	MonoInst *dest;
	int temp;

	if (mono_class_is_nullable (klass)) {
		MonoMethod* method = mono_class_get_method_from_name (klass, "Box", 1);
		temp = mono_emit_method_call_spilled (cfg, bblock, method, mono_method_signature (method), &val, ip, NULL);
		NEW_TEMPLOAD (cfg, dest, temp);
		return dest;
	}

	temp = handle_alloc (cfg, bblock, klass, TRUE, ip);

	return handle_box_copy (cfg, bblock, val, ip, klass, temp);
}

static MonoInst *
handle_box_from_inst (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *val, const guchar *ip,
		MonoClass *klass, MonoInst *data_inst)
{
	int temp;

	g_assert (!mono_class_is_nullable (klass));

	temp = handle_alloc_from_inst (cfg, bblock, klass, data_inst, TRUE, ip);

	return handle_box_copy (cfg, bblock, val, ip, klass, temp);
}

static MonoInst*
handle_delegate_ctor (MonoCompile *cfg, MonoBasicBlock *bblock, MonoClass *klass, MonoInst *target, MonoMethod *method, unsigned char *ip)
{
	gpointer *trampoline;
	MonoInst *obj, *ins, *store, *offset_ins, *method_ins, *tramp_ins;
	int temp;

	temp = handle_alloc (cfg, bblock, klass, FALSE, ip);

	/* Inline the contents of mono_delegate_ctor */

	/* Set target field */
	/* Optimize away setting of NULL target */
	if (!(target->opcode == OP_PCONST && target->inst_p0 == 0)) {
		NEW_TEMPLOAD (cfg, obj, temp);
		NEW_ICONST (cfg, offset_ins, G_STRUCT_OFFSET (MonoDelegate, target));
		MONO_INST_NEW (cfg, ins, OP_PADD);
		ins->inst_left = obj;
		ins->inst_right = offset_ins;

		MONO_INST_NEW (cfg, store, CEE_STIND_REF);
		store->inst_left = ins;
		store->inst_right = target;
		mono_bblock_add_inst (bblock, store);
	}

	/* Set method field */
	NEW_TEMPLOAD (cfg, obj, temp);
	NEW_ICONST (cfg, offset_ins, G_STRUCT_OFFSET (MonoDelegate, method));
	MONO_INST_NEW (cfg, ins, OP_PADD);
	ins->inst_left = obj;
	ins->inst_right = offset_ins;

	NEW_METHODCONST (cfg, method_ins, method);

	MONO_INST_NEW (cfg, store, CEE_STIND_I);
	store->inst_left = ins;
	store->inst_right = method_ins;
	mono_bblock_add_inst (bblock, store);

	/* Set invoke_impl field */
	NEW_TEMPLOAD (cfg, obj, temp);
	NEW_ICONST (cfg, offset_ins, G_STRUCT_OFFSET (MonoDelegate, invoke_impl));
	MONO_INST_NEW (cfg, ins, OP_PADD);
	ins->inst_left = obj;
	ins->inst_right = offset_ins;

	trampoline = mono_create_delegate_trampoline (klass);
	NEW_AOTCONST (cfg, tramp_ins, MONO_PATCH_INFO_ABS, trampoline);

	MONO_INST_NEW (cfg, store, CEE_STIND_I);
	store->inst_left = ins;
	store->inst_right = tramp_ins;
	mono_bblock_add_inst (bblock, store);

	/* All the checks which are in mono_delegate_ctor () are done by the delegate trampoline */

	NEW_TEMPLOAD (cfg, obj, temp);

	return obj;
}

static int
handle_array_new (MonoCompile *cfg, MonoBasicBlock *bblock, int rank, MonoInst **sp, unsigned char *ip)
{
	MonoJitICallInfo *info;

	info = mono_get_array_new_va_icall (rank);

	cfg->flags |= MONO_CFG_HAS_VARARGS;

	/* FIXME: This uses info->sig, but it should use the signature of the wrapper */
	return mono_emit_native_call (cfg, bblock, mono_icall_get_wrapper (info), info->sig, sp, ip, TRUE, FALSE);
}

static void
mono_emit_load_got_addr (MonoCompile *cfg)
{
	MonoInst *load, *store, *dummy_use;
	MonoInst *get_got;

	if (!cfg->got_var || cfg->got_var_allocated)
		return;

	MONO_INST_NEW (cfg, get_got, OP_LOAD_GOTADDR);
	NEW_TEMPSTORE (cfg, store, cfg->got_var->inst_c0, get_got);

	/* Add it to the start of the first bblock */
	if (cfg->bb_entry->code) {
		store->next = cfg->bb_entry->code;
		cfg->bb_entry->code = store;
	}
	else
		MONO_ADD_INS (cfg->bb_entry, store);

	cfg->got_var_allocated = TRUE;

	/* 
	 * Add a dummy use to keep the got_var alive, since real uses might
	 * only be generated in the decompose or instruction selection phases.
	 * Add it to end_bblock, so the variable's lifetime covers the whole
	 * method.
	 */
	NEW_TEMPLOAD (cfg, load, cfg->got_var->inst_c0);
	NEW_DUMMY_USE (cfg, dummy_use, load);
	MONO_ADD_INS (cfg->bb_exit, dummy_use);
}

#define CODE_IS_STLOC(ip) (((ip) [0] >= CEE_STLOC_0 && (ip) [0] <= CEE_STLOC_3) || ((ip) [0] == CEE_STLOC_S))

gboolean
mini_class_is_system_array (MonoClass *klass)
{
	if (klass->parent == mono_defaults.array_class)
		return TRUE;
	else
		return FALSE;
}

static gboolean
mono_method_check_inlining (MonoCompile *cfg, MonoMethod *method)
{
	MonoMethodHeader *header = mono_method_get_header (method);
	MonoMethodSignature *signature = mono_method_signature (method);
	MonoVTable *vtable;
	int i;

	if (cfg->generic_sharing_context)
		return FALSE;

	if (method->inline_failure)
		return FALSE;

#ifdef MONO_ARCH_HAVE_LMF_OPS
	if (((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
		 (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) &&
	    !MONO_TYPE_ISSTRUCT (signature->ret) && !mini_class_is_system_array (method->klass))
		return TRUE;
#endif

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_NOINLINING) ||
	    (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) ||
	    (method->klass->marshalbyref) ||
	    !header || header->num_clauses ||
	    /* fixme: why cant we inline valuetype returns? */
	    MONO_TYPE_ISSTRUCT (signature->ret))
		return FALSE;

#ifdef MONO_ARCH_SOFT_FLOAT
	/* this complicates things, fix later */
	if (signature->ret->type == MONO_TYPE_R4)
		return FALSE;
#endif
	/* its not worth to inline methods with valuetype arguments?? */
	for (i = 0; i < signature->param_count; i++) {
		if (MONO_TYPE_ISSTRUCT (signature->params [i])) {
			return FALSE;
		}
#ifdef MONO_ARCH_SOFT_FLOAT
		/* this complicates things, fix later */
		if (!signature->params [i]->byref && signature->params [i]->type == MONO_TYPE_R4)
			return FALSE;
#endif
	}

	/* also consider num_locals? */
	/* Do the size check early to avoid creating vtables */
	if (getenv ("MONO_INLINELIMIT")) {
		if (header->code_size >= atoi (getenv ("MONO_INLINELIMIT"))) {
			return FALSE;
		}
	} else if (header->code_size >= INLINE_LENGTH_LIMIT)
		return FALSE;

	/*
	 * if we can initialize the class of the method right away, we do,
	 * otherwise we don't allow inlining if the class needs initialization,
	 * since it would mean inserting a call to mono_runtime_class_init()
	 * inside the inlined code
	 */
	if (!(cfg->opt & MONO_OPT_SHARED)) {
		if (method->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) {
			if (cfg->run_cctors && method->klass->has_cctor) {
				if (!method->klass->runtime_info)
					/* No vtable created yet */
					return FALSE;
				vtable = mono_class_vtable (cfg->domain, method->klass);
				if (!vtable)
					return FALSE;
				/* This makes so that inline cannot trigger */
				/* .cctors: too many apps depend on them */
				/* running with a specific order... */
				if (! vtable->initialized)
					return FALSE;
				mono_runtime_class_init (vtable);
			}
		} else if (mono_class_needs_cctor_run (method->klass, NULL)) {
			if (!method->klass->runtime_info)
				/* No vtable created yet */
				return FALSE;
			vtable = mono_class_vtable (cfg->domain, method->klass);
			if (!vtable)
				return FALSE;
			if (!vtable->initialized)
				return FALSE;
		}
	} else {
		/* 
		 * If we're compiling for shared code
		 * the cctor will need to be run at aot method load time, for example,
		 * or at the end of the compilation of the inlining method.
		 */
		if (mono_class_needs_cctor_run (method->klass, NULL) && !((method->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT)))
			return FALSE;
	}
	//if (!MONO_TYPE_IS_VOID (signature->ret)) return FALSE;

	/*
	 * CAS - do not inline methods with declarative security
	 * Note: this has to be before any possible return TRUE;
	 */
	if (mono_method_has_declsec (method))
		return FALSE;

	return TRUE;
}

static gboolean
mini_field_access_needs_cctor_run (MonoCompile *cfg, MonoMethod *method, MonoVTable *vtable)
{
	if (vtable->initialized && !cfg->compile_aot)
		return FALSE;

	if (vtable->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT)
		return FALSE;

	if (!mono_class_needs_cctor_run (vtable->klass, method))
		return FALSE;

	if (! (method->flags & METHOD_ATTRIBUTE_STATIC) && (vtable->klass == method->klass))
		/* The initialization is already done before the method is called */
		return FALSE;

	return TRUE;
}

static MonoInst*
mini_get_ldelema_ins (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *cmethod, MonoInst **sp, unsigned char *ip, gboolean is_set)
{
	int temp, rank;
	MonoInst *addr;
	MonoMethod *addr_method;
	int element_size;

	rank = mono_method_signature (cmethod)->param_count - (is_set? 1: 0);

	if (rank == 1) {
		MONO_INST_NEW (cfg, addr, CEE_LDELEMA);
		addr->inst_left = sp [0];
		addr->inst_right = sp [1];
		addr->type = STACK_MP;
		addr->klass = cmethod->klass->element_class;
		return addr;
	}

	if (rank == 2 && (cfg->opt & MONO_OPT_INTRINS)) {
#if defined(MONO_ARCH_EMULATE_MUL_DIV) && !defined(MONO_ARCH_NO_EMULATE_MUL)
		/* OP_LDELEMA2D depends on OP_LMUL */
#else
		MonoInst *indexes;
		NEW_GROUP (cfg, indexes, sp [1], sp [2]);
		MONO_INST_NEW (cfg, addr, OP_LDELEMA2D);
		addr->inst_left = sp [0];
		addr->inst_right = indexes;
		addr->type = STACK_MP;
		addr->klass = cmethod->klass->element_class;
		return addr;
#endif
	}

	element_size = mono_class_array_element_size (cmethod->klass->element_class);
	addr_method = mono_marshal_get_array_address (rank, element_size);
	temp = mono_emit_method_call_spilled (cfg, bblock, addr_method, addr_method->signature, sp, ip, NULL);
	NEW_TEMPLOAD (cfg, addr, temp);
	return addr;

}

static MonoJitICallInfo **emul_opcode_map = NULL;

MonoJitICallInfo *
mono_find_jit_opcode_emulation (int opcode)
{
	g_assert (opcode >= 0 && opcode <= OP_LAST);
	if  (emul_opcode_map)
		return emul_opcode_map [opcode];
	else
		return NULL;
}

static MonoInst*
mini_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	
	static MonoClass *runtime_helpers_class = NULL;
	if (! runtime_helpers_class)
		runtime_helpers_class = mono_class_from_name (mono_defaults.corlib,
			"System.Runtime.CompilerServices", "RuntimeHelpers");

	if (cmethod->klass == mono_defaults.string_class) {
		if (strcmp (cmethod->name, "get_Chars") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_GETCHR);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_STRLEN);
			ins->inst_i0 = args [0];
			return ins;
		} else if (strcmp (cmethod->name, "InternalSetChar") == 0) {
			MonoInst *get_addr;
 			MONO_INST_NEW (cfg, get_addr, OP_STR_CHAR_ADDR);
			get_addr->inst_i0 = args [0];
			get_addr->inst_i1 = args [1];
 			MONO_INST_NEW (cfg, ins, CEE_STIND_I2);
			ins->inst_i0 = get_addr;
			ins->inst_i1 = args [2];
			return ins;
		} else 
			return NULL;
	} else if (cmethod->klass == mono_defaults.object_class) {
		if (strcmp (cmethod->name, "GetType") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_GETTYPE);
			ins->inst_i0 = args [0];
			return ins;
		/* The OP_GETHASHCODE rule depends on OP_MUL */
#if !defined(MONO_ARCH_EMULATE_MUL_DIV) && !defined(HAVE_MOVING_COLLECTOR)
		} else if (strcmp (cmethod->name, "InternalGetHashCode") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_GETHASHCODE);
			ins->inst_i0 = args [0];
			return ins;
#endif
		} else if (strcmp (cmethod->name, ".ctor") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_NOP);
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == mono_defaults.array_class) {
 		if (cmethod->name [0] != 'g')
 			return NULL;

		if (strcmp (cmethod->name, "get_Rank") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_ARRAY_RANK);
			ins->inst_i0 = args [0];
			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0) {
 			MONO_INST_NEW (cfg, ins, CEE_LDLEN);
			ins->inst_i0 = args [0];
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == runtime_helpers_class) {
		if (strcmp (cmethod->name, "get_OffsetToStringData") == 0) {
			NEW_ICONST (cfg, ins, G_STRUCT_OFFSET (MonoString, chars));
			return ins;
		} else
			return NULL;
	} else if (cmethod->klass == mono_defaults.thread_class) {
		if (strcmp (cmethod->name, "get_CurrentThread") == 0 && (ins = mono_arch_get_thread_intrinsic (cfg)))
			return ins;
		if (strcmp (cmethod->name, "MemoryBarrier") == 0) {
			MONO_INST_NEW (cfg, ins, OP_MEMORY_BARRIER);
			return ins;
		}
		if (strcmp (cmethod->name, "SpinWait_nop") == 0) {
			MONO_INST_NEW (cfg, ins, OP_RELAXED_NOP);
			return ins;
		}
	} else if (mini_class_is_system_array (cmethod->klass) &&
			strcmp (cmethod->name, "GetGenericValueImpl") == 0) {
		MonoInst *sp [2];
		MonoInst *ldelem, *store, *load;
		MonoClass *eklass = mono_class_from_mono_type (fsig->params [1]);
		int n;
		n = mini_type_to_stind (cfg, &eklass->byval_arg);
		if (n == CEE_STOBJ)
			return NULL;
		sp [0] = args [0];
		sp [1] = args [1];
		NEW_LDELEMA (cfg, ldelem, sp, eklass);
		ldelem->flags |= MONO_INST_NORANGECHECK;
		MONO_INST_NEW (cfg, store, n);
		MONO_INST_NEW (cfg, load, mini_type_to_ldind (cfg, &eklass->byval_arg));
		type_to_eval_stack_type (cfg, &eklass->byval_arg, load);
		load->inst_left = ldelem;
		store->inst_left = args [2];
		store->inst_right = load;
		return store;
	} else if (cmethod->klass == mono_defaults.math_class) {
		/* 
		 * There is general branches code for Min/Max, but it does not work for 
		 * all inputs:
		 * http://everything2.com/?node_id=1051618
		 */
	} else if (cmethod->klass->image == mono_defaults.corlib &&
			   (strcmp (cmethod->klass->name_space, "System.Threading") == 0) &&
			   (strcmp (cmethod->klass->name, "Interlocked") == 0)) {
		ins = NULL;

#if SIZEOF_VOID_P == 8
		if (strcmp (cmethod->name, "Read") == 0 && (fsig->params [0]->type == MONO_TYPE_I8)) {
			/* 64 bit reads are already atomic */
			MONO_INST_NEW (cfg, ins, CEE_LDIND_I8);
			ins->inst_i0 = args [0];
		}
#endif

#ifdef MONO_ARCH_HAVE_ATOMIC_ADD
		if (strcmp (cmethod->name, "Increment") == 0) {
			MonoInst *ins_iconst;
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();

#if SIZEOF_VOID_P == 4
			if (opcode == OP_ATOMIC_ADD_NEW_I8)
				return NULL;
#endif

			MONO_INST_NEW (cfg, ins, opcode);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = 1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Decrement") == 0) {
			MonoInst *ins_iconst;
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();

#if SIZEOF_VOID_P == 4
			if (opcode == OP_ATOMIC_ADD_NEW_I8)
				return NULL;
#endif

			MONO_INST_NEW (cfg, ins, opcode);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = -1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Add") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();

#if SIZEOF_VOID_P == 4
			if (opcode == OP_ATOMIC_ADD_NEW_I8)
				return NULL;
#endif
			
			MONO_INST_NEW (cfg, ins, opcode);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif /* MONO_ARCH_HAVE_ATOMIC_ADD */

#ifdef MONO_ARCH_HAVE_ATOMIC_EXCHANGE
		if (strcmp (cmethod->name, "Exchange") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_EXCHANGE_I4;
#if SIZEOF_VOID_P == 8
			else if ((fsig->params [0]->type == MONO_TYPE_I8) ||
					 (fsig->params [0]->type == MONO_TYPE_I) ||
					 (fsig->params [0]->type == MONO_TYPE_OBJECT))
				opcode = OP_ATOMIC_EXCHANGE_I8;
#else
			else if ((fsig->params [0]->type == MONO_TYPE_I) ||
					 (fsig->params [0]->type == MONO_TYPE_OBJECT))
				opcode = OP_ATOMIC_EXCHANGE_I4;
#endif
			else
				return NULL;

#if SIZEOF_VOID_P == 4
			if (opcode == OP_ATOMIC_EXCHANGE_I8)
				return NULL;
#endif

			MONO_INST_NEW (cfg, ins, opcode);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif /* MONO_ARCH_HAVE_ATOMIC_EXCHANGE */

#ifdef MONO_ARCH_HAVE_ATOMIC_CAS_IMM
		/* 
		 * Can't implement CompareExchange methods this way since they have
		 * three arguments. We can implement one of the common cases, where the new
		 * value is a constant.
		 */
		if ((strcmp (cmethod->name, "CompareExchange") == 0)) {
			if (fsig->params [1]->type == MONO_TYPE_I4 && args [2]->opcode == OP_ICONST) {
				MONO_INST_NEW (cfg, ins, OP_ATOMIC_CAS_IMM_I4);
				ins->inst_i0 = args [0];
				ins->inst_i1 = args [1];
				ins->backend.data = GINT_TO_POINTER (args [2]->inst_c0);
			}
			/* The I8 case is hard to detect, since the arg might be a conv.i8 (iconst) tree */
		}
#endif /* MONO_ARCH_HAVE_ATOMIC_CAS_IMM */

		if (ins)
			return ins;
	} else if (cmethod->klass->image == mono_defaults.corlib) {
		if (cmethod->name [0] == 'B' && strcmp (cmethod->name, "Break") == 0
				&& strcmp (cmethod->klass->name, "Debugger") == 0) {
			MONO_INST_NEW (cfg, ins, OP_BREAK);
			return ins;
		}
		if (cmethod->name [0] == 'g' && strcmp (cmethod->name, "get_IsRunningOnWindows") == 0
				&& strcmp (cmethod->klass->name, "Environment") == 0) {
#ifdef PLATFORM_WIN32
	                NEW_ICONST (cfg, ins, 1);
#else
	                NEW_ICONST (cfg, ins, 0);
#endif
			return ins;
		}
	}

	return mono_arch_get_inst_for_method (cfg, cmethod, fsig, args);
}

static void
mono_save_args (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig, MonoInst **sp, MonoInst **args)
{
	MonoInst *store, *temp;
	int i;

	g_assert (!MONO_TYPE_ISSTRUCT (sig->ret));

	if (!sig->hasthis && sig->param_count == 0) 
		return;

	if (sig->hasthis) {
		if (sp [0]->opcode == OP_ICONST) {
			*args++ = sp [0];
		} else {
			temp = mono_compile_create_var (cfg, type_from_stack_type (*sp), OP_LOCAL);
			*args++ = temp;
			NEW_TEMPSTORE (cfg, store, temp->inst_c0, *sp);
			/* FIXME: handle CEE_STIND_R4 */
			store->cil_code = sp [0]->cil_code;
			MONO_ADD_INS (bblock, store);
		}
		sp++;
	}

	for (i = 0; i < sig->param_count; ++i) {
		if (sp [0]->opcode == OP_ICONST) {
			*args++ = sp [0];
		} else {
			temp = mono_compile_create_var (cfg, sig->params [i], OP_LOCAL);
			*args++ = temp;
			NEW_TEMPSTORE (cfg, store, temp->inst_c0, *sp);
			store->cil_code = sp [0]->cil_code;
			/* FIXME: handle CEE_STIND_R4 */
			if (store->opcode == CEE_STOBJ) {
				NEW_TEMPLOADA (cfg, store, temp->inst_c0);
				handle_stobj (cfg, bblock, store, *sp, sp [0]->cil_code, temp->klass, FALSE, FALSE, FALSE);
#ifdef MONO_ARCH_SOFT_FLOAT
			} else if (store->opcode == CEE_STIND_R4) {
				NEW_TEMPLOADA (cfg, store, temp->inst_c0);
				handle_store_float (cfg, bblock, store, *sp, sp [0]->cil_code);
#endif
			} else {
				MONO_ADD_INS (bblock, store);
			} 
		}
		sp++;
	}
}
#define MONO_INLINE_CALLED_LIMITED_METHODS 0
#define MONO_INLINE_CALLER_LIMITED_METHODS 0

#if (MONO_INLINE_CALLED_LIMITED_METHODS)
static char*
mono_inline_called_method_name_limit = NULL;
static gboolean check_inline_called_method_name_limit (MonoMethod *called_method) {
	char *called_method_name = mono_method_full_name (called_method, TRUE);
	int strncmp_result;
	
	if (mono_inline_called_method_name_limit == NULL) {
		char *limit_string = getenv ("MONO_INLINE_CALLED_METHOD_NAME_LIMIT");
		if (limit_string != NULL) {
			mono_inline_called_method_name_limit = limit_string;
		} else {
			mono_inline_called_method_name_limit = (char *) "";
		}
	}
	
	strncmp_result = strncmp (called_method_name, mono_inline_called_method_name_limit, strlen (mono_inline_called_method_name_limit));
	g_free (called_method_name);
	
	//return (strncmp_result <= 0);
	return (strncmp_result == 0);
}
#endif

#if (MONO_INLINE_CALLER_LIMITED_METHODS)
static char*
mono_inline_caller_method_name_limit = NULL;
static gboolean check_inline_caller_method_name_limit (MonoMethod *caller_method) {
	char *caller_method_name = mono_method_full_name (caller_method, TRUE);
	int strncmp_result;
	
	if (mono_inline_caller_method_name_limit == NULL) {
		char *limit_string = getenv ("MONO_INLINE_CALLER_METHOD_NAME_LIMIT");
		if (limit_string != NULL) {
			mono_inline_caller_method_name_limit = limit_string;
		} else {
			mono_inline_caller_method_name_limit = (char *) "";
		}
	}
	
	strncmp_result = strncmp (caller_method_name, mono_inline_caller_method_name_limit, strlen (mono_inline_caller_method_name_limit));
	g_free (caller_method_name);
	
	//return (strncmp_result <= 0);
	return (strncmp_result == 0);
}
#endif

static int
inline_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoBasicBlock *bblock, MonoInst **sp,
		guchar *ip, guint real_offset, GList *dont_inline, MonoBasicBlock **last_b, gboolean inline_allways)
{
	MonoInst *ins, *rvar = NULL;
	MonoMethodHeader *cheader;
	MonoBasicBlock *ebblock, *sbblock;
	int i, costs, new_locals_offset;
	MonoMethod *prev_inlined_method;
	MonoBasicBlock **prev_cil_offset_to_bb;
	unsigned char* prev_cil_start;
	guint32 prev_cil_offset_to_bb_len;

	g_assert (cfg->exception_type == MONO_EXCEPTION_NONE);

#if (MONO_INLINE_CALLED_LIMITED_METHODS)
	if ((! inline_allways) && ! check_inline_called_method_name_limit (cmethod))
		return 0;
#endif
#if (MONO_INLINE_CALLER_LIMITED_METHODS)
	if ((! inline_allways) && ! check_inline_caller_method_name_limit (cfg->method))
		return 0;
#endif

	if (bblock->out_of_line && !inline_allways)
		return 0;

	if (cfg->verbose_level > 2)
		g_print ("INLINE START %p %s -> %s\n", cmethod,  mono_method_full_name (cfg->method, TRUE), mono_method_full_name (cmethod, TRUE));

	if (!cmethod->inline_info) {
		mono_jit_stats.inlineable_methods++;
		cmethod->inline_info = 1;
	}
	/* allocate space to store the return value */
	if (!MONO_TYPE_IS_VOID (fsig->ret)) {
		rvar =  mono_compile_create_var (cfg, fsig->ret, OP_LOCAL);
	}

	/* allocate local variables */
	cheader = mono_method_get_header (cmethod);
	new_locals_offset = cfg->num_varinfo;
	for (i = 0; i < cheader->num_locals; ++i)
		mono_compile_create_var (cfg, cheader->locals [i], OP_LOCAL);

	/* allocate starte and end blocks */
	sbblock = NEW_BBLOCK (cfg);
	sbblock->block_num = cfg->num_bblocks++;
	sbblock->real_offset = real_offset;

	ebblock = NEW_BBLOCK (cfg);
	ebblock->block_num = cfg->num_bblocks++;
	ebblock->real_offset = real_offset;

	prev_inlined_method = cfg->inlined_method;
	cfg->inlined_method = cmethod;
	prev_cil_offset_to_bb = cfg->cil_offset_to_bb;
	prev_cil_offset_to_bb_len = cfg->cil_offset_to_bb_len;
	prev_cil_start = cfg->cil_start;

	costs = mono_method_to_ir (cfg, cmethod, sbblock, ebblock, new_locals_offset, rvar, dont_inline, sp, real_offset, *ip == CEE_CALLVIRT);

	cfg->inlined_method = prev_inlined_method;
	cfg->cil_offset_to_bb = prev_cil_offset_to_bb;
	cfg->cil_offset_to_bb_len = prev_cil_offset_to_bb_len;
	cfg->cil_start = prev_cil_start;

	if ((costs >= 0 && costs < 60) || inline_allways) {
		if (cfg->verbose_level > 2)
			g_print ("INLINE END %s -> %s\n", mono_method_full_name (cfg->method, TRUE), mono_method_full_name (cmethod, TRUE));
		
		mono_jit_stats.inlined_methods++;

		/* always add some code to avoid block split failures */
		MONO_INST_NEW (cfg, ins, OP_NOP);
		MONO_ADD_INS (bblock, ins);
		ins->cil_code = ip;

		bblock->next_bb = sbblock;
		link_bblock (cfg, bblock, sbblock);

		if (rvar) {
			NEW_TEMPLOAD (cfg, ins, rvar->inst_c0);
			NEW_TEMPLOAD_SOFT_FLOAT (cfg, ebblock, ins, rvar->inst_c0, ip);
			*sp++ = ins;
		}
		*last_b = ebblock;
		return costs + 1;
	} else {
		if (cfg->verbose_level > 2)
			g_print ("INLINE ABORTED %s\n", mono_method_full_name (cmethod, TRUE));
		cfg->exception_type = MONO_EXCEPTION_NONE;
		mono_loader_clear_error ();
		cmethod->inline_failure = TRUE;
	}
	return 0;
}

/*
 * Some of these comments may well be out-of-date.
 * Design decisions: we do a single pass over the IL code (and we do bblock 
 * splitting/merging in the few cases when it's required: a back jump to an IL
 * address that was not already seen as bblock starting point).
 * Code is validated as we go (full verification is still better left to metadata/verify.c).
 * Complex operations are decomposed in simpler ones right away. We need to let the 
 * arch-specific code peek and poke inside this process somehow (except when the 
 * optimizations can take advantage of the full semantic info of coarse opcodes).
 * All the opcodes of the form opcode.s are 'normalized' to opcode.
 * MonoInst->opcode initially is the IL opcode or some simplification of that 
 * (OP_LOAD, OP_STORE). The arch-specific code may rearrange it to an arch-specific 
 * opcode with value bigger than OP_LAST.
 * At this point the IR can be handed over to an interpreter, a dumb code generator
 * or to the optimizing code generator that will translate it to SSA form.
 *
 * Profiling directed optimizations.
 * We may compile by default with few or no optimizations and instrument the code
 * or the user may indicate what methods to optimize the most either in a config file
 * or through repeated runs where the compiler applies offline the optimizations to 
 * each method and then decides if it was worth it.
 *
 */

#define CHECK_TYPE(ins) if (!(ins)->type) UNVERIFIED
#define CHECK_STACK(num) if ((sp - stack_start) < (num)) UNVERIFIED
#define CHECK_STACK_OVF(num) if (((sp - stack_start) + (num)) > header->max_stack) UNVERIFIED
#define CHECK_ARG(num) if ((unsigned)(num) >= (unsigned)num_args) UNVERIFIED
#define CHECK_LOCAL(num) if ((unsigned)(num) >= (unsigned)header->num_locals) UNVERIFIED
#define CHECK_OPSIZE(size) if (ip + size > end) UNVERIFIED
#define CHECK_UNVERIFIABLE(cfg) if (cfg->unverifiable) UNVERIFIED
#define CHECK_TYPELOAD(klass) if (!(klass) || (klass)->exception_type) {cfg->exception_ptr = klass; goto load_error;}

/* offset from br.s -> br like opcodes */
#define BIG_BRANCH_OFFSET 13

static inline gboolean
ip_in_bb (MonoCompile *cfg, MonoBasicBlock *bb, const guint8* ip)
{
	MonoBasicBlock *b = cfg->cil_offset_to_bb [ip - cfg->cil_start];
	
	return b == NULL || b == bb;
}

static int
get_basic_blocks (MonoCompile *cfg, MonoMethodHeader* header, guint real_offset, unsigned char *start, unsigned char *end, unsigned char **pos)
{
	unsigned char *ip = start;
	unsigned char *target;
	int i;
	guint cli_addr;
	MonoBasicBlock *bblock;
	const MonoOpcode *opcode;

	while (ip < end) {
		cli_addr = ip - start;
		i = mono_opcode_value ((const guint8 **)&ip, end);
		if (i < 0)
			UNVERIFIED;
		opcode = &mono_opcodes [i];
		switch (opcode->argument) {
		case MonoInlineNone:
			ip++; 
			break;
		case MonoInlineString:
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineMethod:
		case MonoInlineTok:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
			target = start + cli_addr + 2 + (signed char)ip [1];
			GET_BBLOCK (cfg, bblock, target);
			ip += 2;
			if (ip < end)
				GET_BBLOCK (cfg, bblock, ip);
			break;
		case MonoInlineBrTarget:
			target = start + cli_addr + 5 + (gint32)read32 (ip + 1);
			GET_BBLOCK (cfg, bblock, target);
			ip += 5;
			if (ip < end)
				GET_BBLOCK (cfg, bblock, ip);
			break;
		case MonoInlineSwitch: {
			guint32 n = read32 (ip + 1);
			guint32 j;
			ip += 5;
			cli_addr += 5 + 4 * n;
			target = start + cli_addr;
			GET_BBLOCK (cfg, bblock, target);
			
			for (j = 0; j < n; ++j) {
				target = start + cli_addr + (gint32)read32 (ip);
				GET_BBLOCK (cfg, bblock, target);
				ip += 4;
			}
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}

		if (i == CEE_THROW) {
			unsigned char *bb_start = ip - 1;
			
			/* Find the start of the bblock containing the throw */
			bblock = NULL;
			while ((bb_start >= start) && !bblock) {
				bblock = cfg->cil_offset_to_bb [(bb_start) - start];
				bb_start --;
			}
			if (bblock)
				bblock->out_of_line = 1;
		}
	}
	return 0;
unverified:
	*pos = ip;
	return 1;
}

static MonoInst*
emit_tree (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *ins, const guint8* ip_next)
{
	MonoInst *store, *temp, *load;
	
	if (ip_in_bb (cfg, bblock, ip_next) &&
		(CODE_IS_STLOC (ip_next) || *ip_next == CEE_RET))
			return ins;
	
	temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
	temp->flags |= MONO_INST_IS_TEMP;
	NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
	/* FIXME: handle CEE_STIND_R4 */
	store->cil_code = ins->cil_code;
	MONO_ADD_INS (bblock, store);
	NEW_TEMPLOAD (cfg, load, temp->inst_c0);
	load->cil_code = ins->cil_code;
	return load;
}

static inline MonoMethod *
mini_get_method_allow_open (MonoMethod *m, guint32 token, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod *method;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return mono_method_get_wrapper_data (m, token);

	method = mono_get_method_full (m->klass->image, token, klass, context);

	return method;
}

static inline MonoMethod *
mini_get_method (MonoCompile *cfg, MonoMethod *m, guint32 token, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod *method = mini_get_method_allow_open (m, token, klass, context);

	if (method && cfg && !cfg->generic_sharing_context && mono_class_is_open_constructed_type (&method->klass->byval_arg))
		return NULL;

	return method;
}

static inline MonoClass*
mini_get_class (MonoMethod *method, guint32 token, MonoGenericContext *context)
{
	MonoClass *klass;

	if (method->wrapper_type != MONO_WRAPPER_NONE)
		klass = mono_method_get_wrapper_data (method, token);
	else
		klass = mono_class_get_full (method->klass->image, token, context);
	if (klass)
		mono_class_init (klass);
	return klass;
}

/*
 * Returns TRUE if the JIT should abort inlining because "callee"
 * is influenced by security attributes.
 */
static
gboolean check_linkdemand (MonoCompile *cfg, MonoMethod *caller, MonoMethod *callee, MonoBasicBlock *bblock, unsigned char *ip)
{
	guint32 result;
	
	if ((cfg->method != caller) && mono_method_has_declsec (callee)) {
		return TRUE;
	}
	
	result = mono_declsec_linkdemand (cfg->domain, caller, callee);
	if (result == MONO_JIT_SECURITY_OK)
		return FALSE;

	if (result == MONO_JIT_LINKDEMAND_ECMA) {
		/* Generate code to throw a SecurityException before the actual call/link */
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		MonoInst *args [2];

		NEW_ICONST (cfg, args [0], 4);
		NEW_METHODCONST (cfg, args [1], caller);
		mono_emit_method_call_spilled (cfg, bblock, secman->linkdemandsecurityexception, mono_method_signature (secman->linkdemandsecurityexception), args, ip, NULL);
	} else if (cfg->exception_type == MONO_EXCEPTION_NONE) {
		 /* don't hide previous results */
		cfg->exception_type = MONO_EXCEPTION_SECURITY_LINKDEMAND;
		cfg->exception_data = result;
		return TRUE;
	}
	
	return FALSE;
}

static MonoMethod*
method_access_exception (void)
{
	static MonoMethod *method = NULL;

	if (!method) {
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		method = mono_class_get_method_from_name (secman->securitymanager,
							  "MethodAccessException", 2);
	}
	g_assert (method);
	return method;
}

static void
emit_throw_method_access_exception (MonoCompile *cfg, MonoMethod *caller, MonoMethod *callee,
				    MonoBasicBlock *bblock, unsigned char *ip)
{
	MonoMethod *thrower = method_access_exception ();
	MonoInst *args [2];

	NEW_METHODCONST (cfg, args [0], caller);
	NEW_METHODCONST (cfg, args [1], callee);
	mono_emit_method_call_spilled (cfg, bblock, thrower,
		mono_method_signature (thrower), args, ip, NULL);
}

static MonoMethod*
verification_exception (void)
{
	static MonoMethod *method = NULL;

	if (!method) {
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		method = mono_class_get_method_from_name (secman->securitymanager,
							  "VerificationException", 0);
	}
	g_assert (method);
	return method;
}

static void
emit_throw_verification_exception (MonoCompile *cfg, MonoBasicBlock *bblock, unsigned char *ip)
{
	MonoMethod *thrower = verification_exception ();

	mono_emit_method_call_spilled (cfg, bblock, thrower,
		mono_method_signature (thrower),
		NULL, ip, NULL);
}

static void
ensure_method_is_allowed_to_call_method (MonoCompile *cfg, MonoMethod *caller, MonoMethod *callee,
					 MonoBasicBlock *bblock, unsigned char *ip)
{
	MonoSecurityCoreCLRLevel caller_level = mono_security_core_clr_method_level (caller, TRUE);
	MonoSecurityCoreCLRLevel callee_level = mono_security_core_clr_method_level (callee, TRUE);
	gboolean is_safe = TRUE;

	if (!(caller_level >= callee_level ||
			caller_level == MONO_SECURITY_CORE_CLR_SAFE_CRITICAL ||
			callee_level == MONO_SECURITY_CORE_CLR_SAFE_CRITICAL)) {
		is_safe = FALSE;
	}

	if (!is_safe)
		emit_throw_method_access_exception (cfg, caller, callee, bblock, ip);
}

static gboolean
method_is_safe (MonoMethod *method)
{
	/*
	if (strcmp (method->name, "unsafeMethod") == 0)
		return FALSE;
	*/
	return TRUE;
}

/*
 * Check that the IL instructions at ip are the array initialization
 * sequence and return the pointer to the data and the size.
 */
static const char*
initialize_array_data (MonoMethod *method, gboolean aot, unsigned char *ip, MonoInst *newarr, int *out_size, guint32 *out_field_token)
{
	/*
	 * newarr[System.Int32]
	 * dup
	 * ldtoken field valuetype ...
	 * call void class [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
	 */
	if (ip [0] == CEE_DUP && ip [1] == CEE_LDTOKEN && ip [5] == 0x4 && ip [6] == CEE_CALL) {
		MonoClass *klass = newarr->inst_newa_class;
		guint32 field_token = read32 (ip + 2);
		guint32 field_index = field_token & 0xffffff;
		guint32 token = read32 (ip + 7);
		guint32 rva;
		const char *data_ptr;
		int size = 0;
		MonoMethod *cmethod;
		MonoClass *dummy_class;
		MonoClassField *field = mono_field_from_token (method->klass->image, field_token, &dummy_class, NULL);
		int dummy_align;

		if (!field)
			return NULL;

		*out_field_token = field_token;

		if (newarr->inst_newa_len->opcode != OP_ICONST)
			return NULL;
		cmethod = mini_get_method (NULL, method, token, NULL, NULL);
		if (!cmethod)
			return NULL;
		if (strcmp (cmethod->name, "InitializeArray") || strcmp (cmethod->klass->name, "RuntimeHelpers") || cmethod->klass->image != mono_defaults.corlib)
			return NULL;
		switch (mono_type_get_underlying_type (&klass->byval_arg)->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			size = 1; break;
		/* we need to swap on big endian, so punt. Should we handle R4 and R8 as well? */
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			size = 2; break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
			size = 4; break;
		case MONO_TYPE_R8:
#ifdef ARM_FPU_FPA
			return NULL; /* stupid ARM FP swapped format */
#endif
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			size = 8; break;
#endif
		default:
			return NULL;
		}
		size *= newarr->inst_newa_len->inst_c0;
		if (size > mono_type_size (field->type, &dummy_align))
		    return NULL;
		*out_size = size;
		/*g_print ("optimized in %s: size: %d, numelems: %d\n", method->name, size, newarr->inst_newa_len->inst_c0);*/
		field_index = read32 (ip + 2) & 0xffffff;
		mono_metadata_field_info (method->klass->image, field_index - 1, NULL, &rva, NULL);
		data_ptr = mono_image_rva_map (method->klass->image, rva);
		/*g_print ("field: 0x%08x, rva: %d, rva_ptr: %p\n", read32 (ip + 2), rva, data_ptr);*/
		/* for aot code we do the lookup on load */
		if (aot && data_ptr)
			return GUINT_TO_POINTER (rva);
		return data_ptr;
	}
	return NULL;
}

static void
set_exception_type_from_invalid_il (MonoCompile *cfg, MonoMethod *method, unsigned char *ip)
{
	char *method_fname = mono_method_full_name (method, TRUE);
	char *method_code;

	if (mono_method_get_header (method)->code_size == 0)
		method_code = g_strdup ("method body is empty.");
	else
		method_code = mono_disasm_code_one (NULL, method, ip, NULL);
	cfg->exception_type = MONO_EXCEPTION_INVALID_PROGRAM;
	cfg->exception_message = g_strdup_printf ("Invalid IL code in %s: %s\n", method_fname, method_code);
	g_free (method_fname);
	g_free (method_code);
}

static void
set_exception_object (MonoCompile *cfg, MonoException *exception)
{
	cfg->exception_type = MONO_EXCEPTION_OBJECT_SUPPLIED;
	MONO_GC_REGISTER_ROOT (cfg->exception_ptr);
	cfg->exception_ptr = exception;
}

static MonoInst*
get_runtime_generic_context (MonoCompile *cfg, MonoMethod *method, int context_used, MonoInst *this, unsigned char *ip)
{
	g_assert (cfg->generic_sharing_context);

	if (method->klass->valuetype)
		g_assert (!this);

	if (context_used & MONO_GENERIC_CONTEXT_USED_METHOD) {
		MonoInst *mrgctx_loc, *mrgctx_var;

		g_assert (!this);
		g_assert (method->is_inflated && mono_method_get_context (method)->method_inst);

		mrgctx_loc = mono_get_vtable_var (cfg);
		NEW_TEMPLOAD (cfg, mrgctx_var, mrgctx_loc->inst_c0);

		return mrgctx_var;
	} else if ((method->flags & METHOD_ATTRIBUTE_STATIC) || method->klass->valuetype) {
		MonoInst *vtable_loc, *vtable_var;

		g_assert (!this);

		vtable_loc = mono_get_vtable_var (cfg);
		NEW_TEMPLOAD (cfg, vtable_var, vtable_loc->inst_c0);

		if (method->is_inflated && mono_method_get_context (method)->method_inst) {
			MonoInst *mrgctx_var = vtable_var;

			g_assert (G_STRUCT_OFFSET (MonoMethodRuntimeGenericContext, class_vtable) == 0);

			MONO_INST_NEW (cfg, vtable_var, CEE_LDIND_I);
			vtable_var->cil_code = ip;
			vtable_var->inst_left = mrgctx_var;
			vtable_var->type = STACK_PTR;
		}

		return vtable_var;
	} else {
		MonoInst *vtable;

		g_assert (this);

		MONO_INST_NEW (cfg, vtable, CEE_LDIND_I);
		vtable->inst_left = this;
		vtable->type = STACK_PTR;

		return vtable;
	}
}

static MonoInst*
get_runtime_generic_context_other_table_ptr (MonoCompile *cfg, MonoBasicBlock *bblock,
	MonoInst *rgc_ptr, guint32 slot, const unsigned char *ip)
{
	MonoMethodSignature *sig = helper_sig_rgctx_lazy_fetch_trampoline;
	guint8 *tramp = mono_create_rgctx_lazy_fetch_trampoline (slot);
	int temp;
	MonoInst *field;

	temp = mono_emit_native_call (cfg, bblock, tramp, sig, &rgc_ptr, ip, FALSE, FALSE);

	NEW_TEMPLOAD (cfg, field, temp);

	return field;
}

static MonoInst*
get_runtime_generic_context_ptr (MonoCompile *cfg, MonoMethod *method, int context_used, MonoBasicBlock *bblock,
	MonoClass *klass, MonoGenericContext *generic_context, MonoInst *rgctx, int rgctx_type, unsigned char *ip)
{
	guint32 slot = mono_method_lookup_or_register_other_info (method,
		context_used & MONO_GENERIC_CONTEXT_USED_METHOD, &klass->byval_arg, rgctx_type, generic_context);

	return get_runtime_generic_context_other_table_ptr (cfg, bblock, rgctx, slot, ip);
}

static MonoInst*
get_runtime_generic_context_method (MonoCompile *cfg, MonoMethod *method, int context_used, MonoBasicBlock *bblock,
	MonoMethod *cmethod, MonoGenericContext *generic_context, MonoInst *rgctx, int rgctx_type, const unsigned char *ip)
{
	guint32 slot = mono_method_lookup_or_register_other_info (method,
		context_used & MONO_GENERIC_CONTEXT_USED_METHOD, cmethod, rgctx_type, generic_context);

	return get_runtime_generic_context_other_table_ptr (cfg, bblock, rgctx, slot, ip);
}

static MonoInst*
get_runtime_generic_context_field (MonoCompile *cfg, MonoMethod *method, int context_used, MonoBasicBlock *bblock,
	MonoClassField *field, MonoGenericContext *generic_context, MonoInst *rgctx, int rgctx_type,
	const unsigned char *ip)
{
	guint32 slot = mono_method_lookup_or_register_other_info (method,
		context_used & MONO_GENERIC_CONTEXT_USED_METHOD, field, rgctx_type, generic_context);

	return get_runtime_generic_context_other_table_ptr (cfg, bblock, rgctx, slot, ip);
}

static MonoInst*
get_runtime_generic_context_method_rgctx (MonoCompile *cfg, MonoMethod *method, int context_used, MonoBasicBlock *bblock,
	MonoMethod *rgctx_method, MonoGenericContext *generic_context, MonoInst *rgctx, const unsigned char *ip)
{
	guint32 slot = mono_method_lookup_or_register_other_info (method,
		context_used & MONO_GENERIC_CONTEXT_USED_METHOD, rgctx_method,
		MONO_RGCTX_INFO_METHOD_RGCTX, generic_context);

	return get_runtime_generic_context_other_table_ptr (cfg, bblock, rgctx, slot, ip);
}

static gboolean
generic_class_is_reference_type (MonoCompile *cfg, MonoClass *klass)
{
	MonoType *type;

	if (cfg->generic_sharing_context)
		type = mini_get_basic_type_from_generic (cfg->generic_sharing_context, &klass->byval_arg);
	else
		type = &klass->byval_arg;
	return MONO_TYPE_IS_REFERENCE (type);
}

/**
 * Handles unbox of a Nullable<T>, returning a temp variable where the
 * result is stored.  If a rgctx is passed, then shared generic code
 * is generated.
 */
static int
handle_unbox_nullable (MonoCompile* cfg, MonoMethod *caller_method, int context_used, MonoBasicBlock* bblock,
	MonoInst* val, const guchar *ip, MonoClass* klass, MonoGenericContext *generic_context, MonoInst *rgctx)
{
	MonoMethod* method = mono_class_get_method_from_name (klass, "Unbox", 1);
	MonoMethodSignature *signature = mono_method_signature (method);

	if (rgctx) {
		/* FIXME: What if the class is shared?  We might not
		   have to get the address of the method from the
		   RGCTX. */
		MonoInst *addr = get_runtime_generic_context_method (cfg, caller_method, context_used, bblock, method,
			generic_context, rgctx, MONO_RGCTX_INFO_GENERIC_METHOD_CODE, ip);

		return mono_emit_rgctx_calli_spilled (cfg, bblock, signature, &val, addr, NULL, ip);
	} else {
		return mono_emit_method_call_spilled (cfg, bblock, method, signature, &val, ip, NULL);
	}
}

static MonoInst*
handle_box_nullable_from_inst (MonoCompile *cfg, MonoMethod *caller_method, int context_used, MonoBasicBlock *bblock,
	MonoInst *val, const guchar *ip, MonoClass *klass, MonoGenericContext *generic_context, MonoInst *rgctx)
{
	MonoMethod* method = mono_class_get_method_from_name (klass, "Box", 1);
	MonoInst *dest, *method_addr;
	int temp;

	g_assert (mono_class_is_nullable (klass));

	/* FIXME: What if the class is shared?  We might not have to
	   get the method address from the RGCTX. */
	method_addr = get_runtime_generic_context_method (cfg, caller_method, context_used, bblock, method,
			generic_context, rgctx, MONO_RGCTX_INFO_GENERIC_METHOD_CODE, ip);
	temp = mono_emit_rgctx_calli_spilled (cfg, bblock, mono_method_signature (method), &val,
			method_addr, NULL, ip);
	NEW_TEMPLOAD (cfg, dest, temp);
	return dest;
}

static int
emit_castclass (MonoClass *klass, guint32 token, int context_used, gboolean inst_is_castclass, MonoCompile *cfg,
		MonoMethod *method, MonoInst **arg_array, MonoType **param_types, GList *dont_inline,
		unsigned char *end, MonoMethodHeader *header, MonoGenericContext *generic_context,
		MonoBasicBlock **_bblock, unsigned char **_ip, MonoInst ***_sp, int *_inline_costs, guint *_real_offset)
{
	MonoBasicBlock *bblock = *_bblock;
	unsigned char *ip = *_ip;
	MonoInst **sp = *_sp;
	int inline_costs = *_inline_costs;
	guint real_offset = *_real_offset;
	int return_value = 0;

	if (context_used) {
		MonoInst *rgctx, *args [2];
		int temp;

		g_assert (!method->klass->valuetype);

		/* obj */
		args [0] = *sp;

		/* klass */
		GET_RGCTX (rgctx, context_used);
		args [1] = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
				generic_context, rgctx, MONO_RGCTX_INFO_KLASS, ip);

		temp = mono_emit_jit_icall (cfg, bblock, mono_object_castclass, args, ip);
		NEW_TEMPLOAD (cfg, *sp, temp);

		sp++;
		ip += 5;
		inline_costs += 2;
	} else if (klass->marshalbyref || klass->flags & TYPE_ATTRIBUTE_INTERFACE) {

		MonoMethod *mono_castclass;
		MonoInst *iargs [1];
		MonoBasicBlock *ebblock;
		int costs;
		int temp;

		mono_castclass = mono_marshal_get_castclass (klass);
		iargs [0] = sp [0];

		costs = inline_method (cfg, mono_castclass, mono_method_signature (mono_castclass), bblock,
				iargs, ip, real_offset, dont_inline, &ebblock, TRUE);

		g_assert (costs > 0);

		ip += 5;
		real_offset += 5;

		GET_BBLOCK (cfg, bblock, ip);
		ebblock->next_bb = bblock;
		link_bblock (cfg, ebblock, bblock);

		temp = iargs [0]->inst_i0->inst_c0;
		NEW_TEMPLOAD (cfg, *sp, temp);

		sp++;
		bblock = ebblock;
		inline_costs += costs;
	} else {
		MonoInst *ins;

		/* Needed by the code generated in inssel.brg */
		mono_get_got_var (cfg);

		MONO_INST_NEW (cfg, ins, CEE_CASTCLASS);
		ins->type = STACK_OBJ;
		ins->inst_left = *sp;
		ins->klass = klass;
		ins->inst_newa_class = klass;
		if (inst_is_castclass)
			ins->backend.record_cast_details = debug_options.better_cast_details;
		if (inst_is_castclass)
			*sp++ = emit_tree (cfg, bblock, ins, ip + 5);
		else
			*sp++ = ins;
		ip += 5;
	}

do_return:
	*_bblock = bblock;
	*_ip = ip;
	*_sp = sp;
	*_inline_costs = inline_costs;
	*_real_offset = real_offset;
	return return_value;
unverified:
	return_value = -1;
	goto do_return;
}

static int
emit_unbox (MonoClass *klass, guint32 token, int context_used,
		MonoCompile *cfg, MonoMethod *method, MonoInst **arg_array, MonoType **param_types, GList *dont_inline,
		unsigned char *end, MonoMethodHeader *header, MonoGenericContext *generic_context,
		MonoBasicBlock **_bblock, unsigned char **_ip, MonoInst ***_sp, int *_inline_costs, guint *_real_offset)
{
	MonoBasicBlock *bblock = *_bblock;
	unsigned char *ip = *_ip;
	MonoInst **sp = *_sp;
	int inline_costs = *_inline_costs;
	guint real_offset = *_real_offset;
	int return_value = 0;

	MonoInst *add, *vtoffset, *ins;

	/* Needed by the code generated in inssel.brg */
	mono_get_got_var (cfg);

	if (context_used) {
		MonoInst *rgctx, *element_class;

		/* This assertion is from the unboxcast insn */
		g_assert (klass->rank == 0);

		GET_RGCTX (rgctx, context_used);
		element_class = get_runtime_generic_context_ptr (cfg, method, context_used, bblock,
				klass->element_class, generic_context, rgctx, MONO_RGCTX_INFO_KLASS, ip);

		MONO_INST_NEW (cfg, ins, OP_UNBOXCAST_REG);
		ins->type = STACK_OBJ;
		ins->inst_left = *sp;
		ins->inst_right = element_class;
		ins->klass = klass;
		ins->cil_code = ip;
	} else {
		MONO_INST_NEW (cfg, ins, OP_UNBOXCAST);
		ins->type = STACK_OBJ;
		ins->inst_left = *sp;
		ins->klass = klass;
		ins->inst_newa_class = klass;
		ins->cil_code = ip;
	}

	MONO_INST_NEW (cfg, add, OP_PADD);
	NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
	add->inst_left = ins;
	add->inst_right = vtoffset;
	add->type = STACK_MP;
	add->klass = klass;
	*sp = add;

	*_bblock = bblock;
	*_ip = ip;
	*_sp = sp;
	*_inline_costs = inline_costs;
	*_real_offset = real_offset;
	return return_value;
}

gboolean
mini_assembly_can_skip_verification (MonoDomain *domain, MonoMethod *method)
{
	MonoAssembly *assembly = method->klass->image->assembly;
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;
	if (assembly->in_gac || assembly->image == mono_defaults.corlib)
		return FALSE;
	if (mono_security_get_mode () != MONO_SECURITY_MODE_NONE)
		return FALSE;
	return mono_assembly_has_skip_verification (assembly);
}

/*
 * mini_method_verify:
 * 
 * Verify the method using the new verfier.
 * 
 * Returns true if the method is invalid. 
 */
gboolean
mini_method_verify (MonoCompile *cfg, MonoMethod *method)
{
	GSList *tmp, *res;
	gboolean is_fulltrust;
	MonoLoaderError *error;

	if (method->verification_success)
		return FALSE;

	is_fulltrust = mono_verifier_is_method_full_trust (method);

	if (!mono_verifier_is_enabled_for_method (method))
		return FALSE;

	res = mono_method_verify_with_current_settings (method, cfg->skip_visibility);

	if ((error = mono_loader_get_last_error ())) {
		cfg->exception_type = error->exception_type;
		if (res)
			mono_free_verify_list (res);
		return TRUE;
	}

	if (res) { 
		for (tmp = res; tmp; tmp = tmp->next) {
			MonoVerifyInfoExtended *info = (MonoVerifyInfoExtended *)tmp->data;
			if (info->info.status == MONO_VERIFY_ERROR) {
				cfg->exception_type = info->exception_type;
				cfg->exception_message = g_strdup (info->info.message);
				mono_free_verify_list (res);
				return TRUE;
			}
			if (info->info.status == MONO_VERIFY_NOT_VERIFIABLE && !is_fulltrust) {
				cfg->exception_type = info->exception_type;
				cfg->exception_message = g_strdup (info->info.message);
				mono_free_verify_list (res);
				return TRUE;
			}
		}
		mono_free_verify_list (res);
	}
	method->verification_success = 1;
	return FALSE;
}

/*
 * mono_method_to_ir: translates IL into basic blocks containing trees
 */
static int
mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
		   int locals_offset, MonoInst *return_var, GList *dont_inline, MonoInst **inline_args, 
		   guint inline_offset, gboolean is_virtual_call)
{
	MonoInst *zero_int32, *zero_int64, *zero_ptr, *zero_obj, *zero_r8;
	MonoInst *ins, **sp, **stack_start;
	MonoBasicBlock *bblock, *tblock = NULL, *init_localsbb = NULL;
	MonoMethod *cmethod, *method_definition;
	MonoInst **arg_array;
	MonoMethodHeader *header;
	MonoImage *image;
	guint32 token, ins_flag;
	MonoClass *klass;
	MonoClass *constrained_call = NULL;
	unsigned char *ip, *end, *target, *err_pos;
	static double r8_0 = 0.0;
	MonoMethodSignature *sig;
	MonoGenericContext *generic_context = NULL;
	MonoGenericContainer *generic_container = NULL;
	MonoType **param_types;
	GList *bb_recheck = NULL, *tmp;
	int i, n, start_new_bblock, ialign;
	int num_calls = 0, inline_costs = 0;
	int breakpoint_id = 0;
	guint32 align;
	guint real_offset, num_args;
	MonoBoolean security, pinvoke;
	MonoSecurityManager* secman = NULL;
	MonoDeclSecurityActions actions;
	GSList *class_inits = NULL;
	gboolean dont_verify, dont_verify_stloc, readonly = FALSE;
	int context_used;

	/* serialization and xdomain stuff may need access to private fields and methods */
	dont_verify = method->klass->image->assembly->corlib_internal? TRUE: FALSE;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_XDOMAIN_DISPATCH;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE; /* bug #77896 */
	dont_verify |= method->wrapper_type == MONO_WRAPPER_COMINTEROP;
	dont_verify |= method->wrapper_type == MONO_WRAPPER_COMINTEROP_INVOKE;

	/* turn off visibility checks for smcs */
	dont_verify |= mono_security_get_mode () == MONO_SECURITY_MODE_SMCS_HACK;

	/* still some type unsafety issues in marshal wrappers... (unknown is PtrToStructure) */
	dont_verify_stloc = method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE;
	dont_verify_stloc |= method->wrapper_type == MONO_WRAPPER_UNKNOWN;
	dont_verify_stloc |= method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED;

	image = method->klass->image;
	header = mono_method_get_header (method);
	generic_container = mono_method_get_generic_container (method);
	sig = mono_method_signature (method);
	num_args = sig->hasthis + sig->param_count;
	ip = (unsigned char*)header->code;
	cfg->cil_start = ip;
	end = ip + header->code_size;
	mono_jit_stats.cil_code_size += header->code_size;

	method_definition = method;
	while (method_definition->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method_definition;
		method_definition = imethod->declaring;
	}

	/* SkipVerification is not allowed if core-clr is enabled */
	if (!dont_verify && mini_assembly_can_skip_verification (cfg->domain, method)) {
		dont_verify = TRUE;
		dont_verify_stloc = TRUE;
	}

	if (!dont_verify && mini_method_verify (cfg, method_definition))
		goto exception_exit;

	if (mono_debug_using_mono_debugger ())
		cfg->keep_cil_nops = TRUE;

	if (sig->is_inflated)
		generic_context = mono_method_get_context (method);
	else if (generic_container)
		generic_context = &generic_container->context;

	if (!cfg->generic_sharing_context)
		g_assert (!sig->has_type_parameters);

	if (sig->generic_param_count && method->wrapper_type == MONO_WRAPPER_NONE) {
		g_assert (method->is_inflated);
		g_assert (mono_method_get_context (method)->method_inst);
	}
	if (method->is_inflated && mono_method_get_context (method)->method_inst)
		g_assert (sig->generic_param_count);

	if (cfg->method == method)
		real_offset = 0;
	else
		real_offset = inline_offset;

	cfg->cil_offset_to_bb = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoBasicBlock*) * header->code_size);
	cfg->cil_offset_to_bb_len = header->code_size;

	if (cfg->verbose_level > 2)
		g_print ("method to IR %s\n", mono_method_full_name (method, TRUE));

	dont_inline = g_list_prepend (dont_inline, method);
	if (cfg->method == method) {

		if (cfg->prof_options & MONO_PROFILE_INS_COVERAGE)
			cfg->coverage_info = mono_profiler_coverage_alloc (cfg->method, header->code_size);

		/* ENTRY BLOCK */
		cfg->bb_entry = start_bblock = NEW_BBLOCK (cfg);
		start_bblock->cil_code = NULL;
		start_bblock->cil_length = 0;
		start_bblock->block_num = cfg->num_bblocks++;

		/* EXIT BLOCK */
		cfg->bb_exit = end_bblock = NEW_BBLOCK (cfg);
		end_bblock->cil_code = NULL;
		end_bblock->cil_length = 0;
		end_bblock->block_num = cfg->num_bblocks++;
		g_assert (cfg->num_bblocks == 2);

		arg_array = alloca (sizeof (MonoInst *) * num_args);
		for (i = num_args - 1; i >= 0; i--)
			arg_array [i] = cfg->varinfo [i];

		if (header->num_clauses) {
			cfg->spvars = g_hash_table_new (NULL, NULL);
			cfg->exvars = g_hash_table_new (NULL, NULL);
		}
		/* handle exception clauses */
		for (i = 0; i < header->num_clauses; ++i) {
			MonoBasicBlock *try_bb;
			MonoExceptionClause *clause = &header->clauses [i];

			GET_BBLOCK (cfg, try_bb, ip + clause->try_offset);
			try_bb->real_offset = clause->try_offset;
			GET_BBLOCK (cfg, tblock, ip + clause->handler_offset);
			tblock->real_offset = clause->handler_offset;
			tblock->flags |= BB_EXCEPTION_HANDLER;

			link_bblock (cfg, try_bb, tblock);

			if (*(ip + clause->handler_offset) == CEE_POP)
				tblock->flags |= BB_EXCEPTION_DEAD_OBJ;

			if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FAULT) {
				MONO_INST_NEW (cfg, ins, OP_START_HANDLER);
				MONO_ADD_INS (tblock, ins);

				/* todo: is a fault block unsafe to optimize? */
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FAULT)
					tblock->flags |= BB_EXCEPTION_UNSAFE;
			}


			/*g_print ("clause try IL_%04x to IL_%04x handler %d at IL_%04x to IL_%04x\n", clause->try_offset, clause->try_offset + clause->try_len, clause->flags, clause->handler_offset, clause->handler_offset + clause->handler_len);
			  while (p < end) {
			  g_print ("%s", mono_disasm_code_one (NULL, method, p, &p));
			  }*/
			/* catch and filter blocks get the exception object on the stack */
			if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				MonoInst *load, *dummy_use;

				/* mostly like handle_stack_args (), but just sets the input args */
				/* g_print ("handling clause at IL_%04x\n", clause->handler_offset); */
				tblock->in_scount = 1;
				tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
				tblock->in_stack [0] = mono_create_exvar_for_offset (cfg, clause->handler_offset);

				/* 
				 * Add a dummy use for the exvar so its liveness info will be
				 * correct.
				 */
				NEW_TEMPLOAD (cfg, load, tblock->in_stack [0]->inst_c0);
				NEW_DUMMY_USE (cfg, dummy_use, load);
				MONO_ADD_INS (tblock, dummy_use);
				
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					GET_BBLOCK (cfg, tblock, ip + clause->data.filter_offset);
					tblock->real_offset = clause->data.filter_offset;
					tblock->in_scount = 1;
					tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
					/* The filter block shares the exvar with the handler block */
					tblock->in_stack [0] = mono_create_exvar_for_offset (cfg, clause->handler_offset);
					MONO_INST_NEW (cfg, ins, OP_START_HANDLER);
					MONO_ADD_INS (tblock, ins);
				}
			}

			if (clause->flags != MONO_EXCEPTION_CLAUSE_FILTER &&
					clause->data.catch_class &&
					cfg->generic_sharing_context &&
					mono_class_check_context_used (clause->data.catch_class)) {
				if (mono_method_get_context (method)->method_inst)
					GENERIC_SHARING_FAILURE (CEE_NOP);

				/*
				 * In shared generic code with catch
				 * clauses containing type variables
				 * the exception handling code has to
				 * be able to get to the rgctx.
				 * Therefore we have to make sure that
				 * the vtable/mrgctx argument (for
				 * static or generic methods) or the
				 * "this" argument (for non-static
				 * methods) are live.
				 */
				if ((method->flags & METHOD_ATTRIBUTE_STATIC) ||
						mini_method_get_context (method)->method_inst ||
						method->klass->valuetype) {
					mono_get_vtable_var (cfg);
				} else {
					MonoInst *this, *dummy_use;
					MonoType *this_type;

					if (method->klass->valuetype)
						this_type = &method->klass->this_arg;
					else
						this_type = &method->klass->byval_arg;

					if (arg_array [0]->opcode == OP_ICONST) {
						this = arg_array [0];
					} else {
						this = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));
						this->ssa_op = MONO_SSA_LOAD;
						this->inst_i0 = arg_array [0];
						this->opcode = mini_type_to_ldind ((cfg), this->inst_i0->inst_vtype);
						type_to_eval_stack_type ((cfg), this_type, this);
						this->klass = this->inst_i0->klass;
					}

					NEW_DUMMY_USE (cfg, dummy_use, this);
					MONO_ADD_INS (tblock, dummy_use);
				}
			}
		}
	} else {
		arg_array = alloca (sizeof (MonoInst *) * num_args);
		mono_save_args (cfg, start_bblock, sig, inline_args, arg_array);
	}

	/* FIRST CODE BLOCK */
	bblock = NEW_BBLOCK (cfg);
	bblock->cil_code = ip;

	ADD_BBLOCK (cfg, bblock);

	if (cfg->method == method) {
		breakpoint_id = mono_debugger_method_has_breakpoint (method);
		if (breakpoint_id && (mono_debug_format != MONO_DEBUG_FORMAT_DEBUGGER)) {
			MONO_INST_NEW (cfg, ins, OP_BREAK);
			MONO_ADD_INS (bblock, ins);
		}
	}

	if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS)
		secman = mono_security_manager_get_methods ();

	security = (secman && mono_method_has_declsec (method));
	/* at this point having security doesn't mean we have any code to generate */
	if (security && (cfg->method == method)) {
		/* Only Demand, NonCasDemand and DemandChoice requires code generation.
		 * And we do not want to enter the next section (with allocation) if we
		 * have nothing to generate */
		security = mono_declsec_get_demands (method, &actions);
	}

	/* we must Demand SecurityPermission.Unmanaged before P/Invoking */
	pinvoke = (secman && (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE));
	if (pinvoke) {
		MonoMethod *wrapped = mono_marshal_method_from_wrapper (method);
		if (wrapped && (wrapped->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
			MonoCustomAttrInfo* custom = mono_custom_attrs_from_method (wrapped);

			/* unless the method or it's class has the [SuppressUnmanagedCodeSecurity] attribute */
			if (custom && mono_custom_attrs_has_attr (custom, secman->suppressunmanagedcodesecurity)) {
				pinvoke = FALSE;
			}
			if (custom)
				mono_custom_attrs_free (custom);

			if (pinvoke) {
				custom = mono_custom_attrs_from_class (wrapped->klass);
				if (custom && mono_custom_attrs_has_attr (custom, secman->suppressunmanagedcodesecurity)) {
					pinvoke = FALSE;
				}
				if (custom)
					mono_custom_attrs_free (custom);
			}
		} else {
			/* not a P/Invoke after all */
			pinvoke = FALSE;
		}
	}
	
	if ((header->init_locals || (cfg->method == method && (cfg->opt & MONO_OPT_SHARED))) || cfg->compile_aot || security || pinvoke) {
		/* we use a separate basic block for the initialization code */
		cfg->bb_init = init_localsbb = NEW_BBLOCK (cfg);
		init_localsbb->real_offset = real_offset;
		start_bblock->next_bb = init_localsbb;
		init_localsbb->next_bb = bblock;
		link_bblock (cfg, start_bblock, init_localsbb);
		link_bblock (cfg, init_localsbb, bblock);
		init_localsbb->block_num = cfg->num_bblocks++;
	} else {
		start_bblock->next_bb = bblock;
		link_bblock (cfg, start_bblock, bblock);
	}

	/* at this point we know, if security is TRUE, that some code needs to be generated */
	if (security && (cfg->method == method)) {
		MonoInst *args [2];

		mono_jit_stats.cas_demand_generation++;

		if (actions.demand.blob) {
			/* Add code for SecurityAction.Demand */
			NEW_DECLSECCONST (cfg, args[0], image, actions.demand);
			NEW_ICONST (cfg, args [1], actions.demand.size);
			/* Calls static void SecurityManager.InternalDemand (byte* permissions, int size); */
			mono_emit_method_call_spilled (cfg, init_localsbb, secman->demand, mono_method_signature (secman->demand), args, ip, NULL);
		}
		if (actions.noncasdemand.blob) {
			/* CLR 1.x uses a .noncasdemand (but 2.x doesn't) */
			/* For Mono we re-route non-CAS Demand to Demand (as the managed code must deal with it anyway) */
			NEW_DECLSECCONST (cfg, args[0], image, actions.noncasdemand);
			NEW_ICONST (cfg, args [1], actions.noncasdemand.size);
			/* Calls static void SecurityManager.InternalDemand (byte* permissions, int size); */
			mono_emit_method_call_spilled (cfg, init_localsbb, secman->demand, mono_method_signature (secman->demand), args, ip, NULL);
		}
		if (actions.demandchoice.blob) {
			/* New in 2.0, Demand must succeed for one of the permissions (i.e. not all) */
			NEW_DECLSECCONST (cfg, args[0], image, actions.demandchoice);
			NEW_ICONST (cfg, args [1], actions.demandchoice.size);
			/* Calls static void SecurityManager.InternalDemandChoice (byte* permissions, int size); */
			mono_emit_method_call_spilled (cfg, init_localsbb, secman->demandchoice, mono_method_signature (secman->demandchoice), args, ip, NULL);
		}
	}

	/* we must Demand SecurityPermission.Unmanaged before p/invoking */
	if (pinvoke) {
		mono_emit_method_call_spilled (cfg, init_localsbb, secman->demandunmanaged, mono_method_signature (secman->demandunmanaged), NULL, ip, NULL);
	}

	if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR) {
		if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
			MonoMethod *wrapped = mono_marshal_method_from_wrapper (method);
			if (wrapped && (wrapped->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
				if (!(method->klass && method->klass->image &&
						mono_security_core_clr_is_platform_image (method->klass->image))) {
					emit_throw_method_access_exception (cfg, method, wrapped, bblock, ip);
				}
			}
		}
		if (!method_is_safe (method))
			emit_throw_verification_exception (cfg, bblock, ip);
	}

	if (header->code_size == 0)
		UNVERIFIED;

	if (get_basic_blocks (cfg, header, real_offset, ip, end, &err_pos)) {
		ip = err_pos;
		UNVERIFIED;
	}

	if (cfg->method == method)
		mono_debug_init_method (cfg, bblock, breakpoint_id);

	param_types = mono_mempool_alloc (cfg->mempool, sizeof (MonoType*) * num_args);
	if (sig->hasthis)
		param_types [0] = method->klass->valuetype?&method->klass->this_arg:&method->klass->byval_arg;
	for (n = 0; n < sig->param_count; ++n)
		param_types [n + sig->hasthis] = sig->params [n];
	for (n = 0; n < header->num_locals; ++n) {
		if (header->locals [n]->type == MONO_TYPE_VOID && !header->locals [n]->byref)
			UNVERIFIED;
	}
	class_inits = NULL;

	/* do this somewhere outside - not here */
	NEW_ICONST (cfg, zero_int32, 0);
	NEW_ICONST (cfg, zero_int64, 0);
	zero_int64->type = STACK_I8;
	NEW_PCONST (cfg, zero_ptr, 0);
	NEW_PCONST (cfg, zero_obj, 0);
	zero_obj->type = STACK_OBJ;

	MONO_INST_NEW (cfg, zero_r8, OP_R8CONST);
	zero_r8->type = STACK_R8;
	zero_r8->inst_p0 = &r8_0;

	/* add a check for this != NULL to inlined methods */
	if (is_virtual_call) {
		MONO_INST_NEW (cfg, ins, OP_CHECK_THIS);
		NEW_ARGLOAD (cfg, ins->inst_left, 0);
		ins->cil_code = ip;
		MONO_ADD_INS (bblock, ins);
	}

	/* we use a spare stack slot in SWITCH and NEWOBJ and others */
	stack_start = sp = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * (header->max_stack + 1));

	ins_flag = 0;
	start_new_bblock = 0;
	while (ip < end) {

		if (cfg->method == method)
			real_offset = ip - header->code;
		else
			real_offset = inline_offset;
		cfg->ip = ip;

		context_used = 0;

		if (start_new_bblock) {
			bblock->cil_length = ip - bblock->cil_code;
			if (start_new_bblock == 2) {
				g_assert (ip == tblock->cil_code);
			} else {
				GET_BBLOCK (cfg, tblock, ip);
			}
			bblock->next_bb = tblock;
			bblock = tblock;
			start_new_bblock = 0;
			for (i = 0; i < bblock->in_scount; ++i) {
				if (cfg->verbose_level > 3)
					g_print ("loading %d from temp %d\n", i, (int)bblock->in_stack [i]->inst_c0);						
				NEW_TEMPLOAD (cfg, ins, bblock->in_stack [i]->inst_c0);
				*sp++ = ins;
			}
			g_slist_free (class_inits);
			class_inits = NULL;
		} else {
			if ((tblock = cfg->cil_offset_to_bb [ip - cfg->cil_start]) && (tblock != bblock)) {
				link_bblock (cfg, bblock, tblock);
				if (sp != stack_start) {
					handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
					sp = stack_start;
					CHECK_UNVERIFIABLE (cfg);
				}
				bblock->next_bb = tblock;
				bblock = tblock;
				for (i = 0; i < bblock->in_scount; ++i) {
					if (cfg->verbose_level > 3)
						g_print ("loading %d from temp %d\n", i, (int)bblock->in_stack [i]->inst_c0);						
					NEW_TEMPLOAD (cfg, ins, bblock->in_stack [i]->inst_c0);
					*sp++ = ins;
				}
				g_slist_free (class_inits);
				class_inits = NULL;
			}
		}

		bblock->real_offset = real_offset;

		if ((cfg->method == method) && cfg->coverage_info) {
			MonoInst *store, *one;
			guint32 cil_offset = ip - header->code;
			cfg->coverage_info->data [cil_offset].cil_code = ip;

			/* TODO: Use an increment here */
			NEW_ICONST (cfg, one, 1);
			one->cil_code = ip;

			NEW_PCONST (cfg, ins, &(cfg->coverage_info->data [cil_offset].count));
			ins->cil_code = ip;

			MONO_INST_NEW (cfg, store, CEE_STIND_I);
			store->inst_left = ins;
			store->inst_right = one;

			MONO_ADD_INS (bblock, store);
		}

		if (cfg->verbose_level > 3)
			g_print ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));

		switch (*ip) {
		case CEE_NOP:
			if (cfg->keep_cil_nops)
				MONO_INST_NEW (cfg, ins, OP_HARD_NOP);
			else
				MONO_INST_NEW (cfg, ins, OP_NOP);
			ip++;
			MONO_ADD_INS (bblock, ins);
			break;
		case CEE_BREAK:
			MONO_INST_NEW (cfg, ins, OP_BREAK);
			ip++;
			MONO_ADD_INS (bblock, ins);
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			CHECK_STACK_OVF (1);
			n = (*ip)-CEE_LDARG_0;
			CHECK_ARG (n);
			NEW_ARGLOAD (cfg, ins, n);
			LDARG_SOFT_FLOAT (cfg, ins, n, ip);
			ip++;
			*sp++ = ins;
			break;
		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3:
			CHECK_STACK_OVF (1);
			n = (*ip)-CEE_LDLOC_0;
			CHECK_LOCAL (n);
			NEW_LOCLOAD (cfg, ins, n);
			LDLOC_SOFT_FLOAT (cfg, ins, n, ip);
			ip++;
			*sp++ = ins;
			break;
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3:
			CHECK_STACK (1);
			n = (*ip)-CEE_STLOC_0;
			CHECK_LOCAL (n);
			--sp;
			handle_loaded_temps (cfg, bblock, stack_start, sp);
			NEW_LOCSTORE (cfg, ins, n, *sp);
			if (!dont_verify_stloc && target_type_is_incompatible (cfg, header->locals [n], *sp))
				UNVERIFIED;
			STLOC_SOFT_FLOAT (cfg, ins, n, ip);
			if (ins->opcode == CEE_STOBJ) {
				NEW_LOCLOADA (cfg, ins, n);
				handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE, FALSE);
			} else
				MONO_ADD_INS (bblock, ins);
			++ip;
			inline_costs += 1;
			break;
		case CEE_LDARG_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_ARG (ip [1]);
			NEW_ARGLOAD (cfg, ins, ip [1]);
			LDARG_SOFT_FLOAT (cfg, ins, ip [1], ip);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDARGA_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_ARG (ip [1]);
			NEW_ARGLOADA (cfg, ins, ip [1]);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_STARG_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (1);
			--sp;
			CHECK_ARG (ip [1]);
			NEW_ARGSTORE (cfg, ins, ip [1], *sp);
			handle_loaded_temps (cfg, bblock, stack_start, sp);
			if (!dont_verify_stloc && target_type_is_incompatible (cfg, param_types [ip [1]], *sp))
				UNVERIFIED;
			STARG_SOFT_FLOAT (cfg, ins, ip [1], ip);
			if (ins->opcode == CEE_STOBJ) {
				NEW_ARGLOADA (cfg, ins, ip [1]);
				handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE, FALSE);
			} else
				MONO_ADD_INS (bblock, ins);
			ip += 2;
			break;
		case CEE_LDLOC_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_LOCAL (ip [1]);
			NEW_LOCLOAD (cfg, ins, ip [1]);
			LDLOC_SOFT_FLOAT (cfg, ins, ip [1], ip);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDLOCA_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_LOCAL (ip [1]);
			NEW_LOCLOADA (cfg, ins, ip [1]);
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_STLOC_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (1);
			--sp;
			handle_loaded_temps (cfg, bblock, stack_start, sp);
			CHECK_LOCAL (ip [1]);
			NEW_LOCSTORE (cfg, ins, ip [1], *sp);
			if (!dont_verify_stloc && target_type_is_incompatible (cfg, header->locals [ip [1]], *sp))
				UNVERIFIED;
			STLOC_SOFT_FLOAT (cfg, ins, ip [1], ip);
			if (ins->opcode == CEE_STOBJ) {
				NEW_LOCLOADA (cfg, ins, ip [1]);
				handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE, FALSE);
			} else
				MONO_ADD_INS (bblock, ins);
			ip += 2;
			inline_costs += 1;
			break;
		case CEE_LDNULL:
			CHECK_STACK_OVF (1);
			NEW_PCONST (cfg, ins, NULL);
			ins->type = STACK_OBJ;
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_M1:
			CHECK_STACK_OVF (1);
			NEW_ICONST (cfg, ins, -1);
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			CHECK_STACK_OVF (1);
			NEW_ICONST (cfg, ins, (*ip) - CEE_LDC_I4_0);
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			++ip;
			NEW_ICONST (cfg, ins, *((signed char*)ip));
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4:
			CHECK_OPSIZE (5);
			CHECK_STACK_OVF (1);
			NEW_ICONST (cfg, ins, (gint32)read32 (ip + 1));
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_LDC_I8:
			CHECK_OPSIZE (9);
			CHECK_STACK_OVF (1);
			MONO_INST_NEW (cfg, ins, OP_I8CONST);
			ins->type = STACK_I8;
			++ip;
			ins->inst_l = (gint64)read64 (ip);
			ip += 8;
			*sp++ = ins;
			break;
		case CEE_LDC_R4: {
			float *f;
			/* we should really allocate this only late in the compilation process */
			mono_domain_lock (cfg->domain);
			f = mono_domain_alloc (cfg->domain, sizeof (float));
			mono_domain_unlock (cfg->domain);
			CHECK_OPSIZE (5);
			CHECK_STACK_OVF (1);
			MONO_INST_NEW (cfg, ins, OP_R4CONST);
			ins->type = STACK_R8;
			++ip;
			readr4 (ip, f);
			ins->inst_p0 = f;

			ip += 4;
			*sp++ = ins;			
			break;
		}
		case CEE_LDC_R8: {
			double *d;
			mono_domain_lock (cfg->domain);
			d = mono_domain_alloc (cfg->domain, sizeof (double));
			mono_domain_unlock (cfg->domain);
			CHECK_OPSIZE (9);
			CHECK_STACK_OVF (1);
			MONO_INST_NEW (cfg, ins, OP_R8CONST);
			ins->type = STACK_R8;
			++ip;
			readr8 (ip, d);
			ins->inst_p0 = d;

			ip += 8;
			*sp++ = ins;			
			break;
		}
		case CEE_DUP: {
			MonoInst *temp, *store;
			CHECK_STACK (1);
			CHECK_STACK_OVF (1);
			sp--;
			ins = *sp;
		
			/* 
			 * small optimization: if the loaded value was from a local already,
			 * just load it twice.
			 */
			if (ins->ssa_op == MONO_SSA_LOAD && 
			    (ins->inst_i0->opcode == OP_LOCAL || ins->inst_i0->opcode == OP_ARG)) {
				sp++;
				MONO_INST_NEW (cfg, temp, 0);
				*temp = *ins;
				*sp++ = temp;
			} else {
				temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
				temp->flags |= MONO_INST_IS_TEMP;
				NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
				/* FIXME: handle CEE_STIND_R4 */
				if (store->opcode == CEE_STOBJ) {
					NEW_TEMPLOADA (cfg, store, temp->inst_c0);
					handle_stobj (cfg, bblock, store, sp [0], sp [0]->cil_code, store->klass, TRUE, FALSE, FALSE);
				} else {
					MONO_ADD_INS (bblock, store);
				}
				NEW_TEMPLOAD (cfg, ins, temp->inst_c0);
				*sp++ = ins;
				NEW_TEMPLOAD (cfg, ins, temp->inst_c0);
				*sp++ = ins;
			}
			++ip;
			inline_costs += 2;
			break;
		}
		case CEE_POP:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, CEE_POP);
			MONO_ADD_INS (bblock, ins);
			ip++;
			--sp;
			ins->inst_i0 = *sp;
			break;
		case CEE_JMP:
			CHECK_OPSIZE (5);
			if (stack_start != sp)
				UNVERIFIED;
			MONO_INST_NEW (cfg, ins, OP_JMP);
			token = read32 (ip + 1);
			/* FIXME: check the signature matches */
			cmethod = mini_get_method (cfg, method, token, NULL, generic_context);

			if (!cmethod)
				goto load_error;

			if (cfg->generic_sharing_context && mono_method_check_context_used (cmethod))
				GENERIC_SHARING_FAILURE (CEE_JMP);

			if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS) {
				if (check_linkdemand (cfg, method, cmethod, bblock, ip))
					INLINE_FAILURE;
				CHECK_CFG_EXCEPTION;
			}

			ins->inst_p0 = cmethod;
			MONO_ADD_INS (bblock, ins);
			ip += 5;
			start_new_bblock = 1;
			break;
		case CEE_CALLI:
		case CEE_CALL:
		case CEE_CALLVIRT: {
			MonoInst *addr = NULL;
			MonoMethodSignature *fsig = NULL;
			int temp, array_rank = 0;
			int virtual = *ip == CEE_CALLVIRT;
			gboolean no_spill;
			gboolean pass_imt_from_rgctx = FALSE;
			MonoInst *imt_arg = NULL;
			gboolean pass_vtable = FALSE;
			gboolean pass_mrgctx = FALSE;
			MonoInst *vtable_arg = NULL;
			gboolean check_this = FALSE;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);

			if (*ip == CEE_CALLI) {
				cmethod = NULL;
				CHECK_STACK (1);
				--sp;
				addr = *sp;
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					fsig = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
				else
					fsig = mono_metadata_parse_signature (image, token);

				n = fsig->param_count + fsig->hasthis;
			} else {
				MonoMethod *cil_method;
				
				if (method->wrapper_type != MONO_WRAPPER_NONE) {
					cmethod =  (MonoMethod *)mono_method_get_wrapper_data (method, token);
					cil_method = cmethod;
				} else if (constrained_call) {
					cmethod = mono_get_method_constrained (image, token, constrained_call, generic_context, &cil_method);
					cil_method = cmethod;
				} else {
					cmethod = mini_get_method (cfg, method, token, NULL, generic_context);
					cil_method = cmethod;
				}

				if (!cmethod)
					goto load_error;
				if (!dont_verify && !cfg->skip_visibility) {
					MonoMethod *target_method = cil_method;
					if (method->is_inflated) {
						target_method = mini_get_method_allow_open (method, token, NULL, &(mono_method_get_generic_container (method_definition)->context));
					}
					if (!mono_method_can_access_method (method_definition, target_method) &&
						!mono_method_can_access_method (method, cil_method))
						METHOD_ACCESS_FAILURE;
				}

				if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR)
					ensure_method_is_allowed_to_call_method (cfg, method, cil_method, bblock, ip);

				if (!virtual && (cmethod->flags & METHOD_ATTRIBUTE_ABSTRACT))
					/* MS.NET seems to silently convert this to a callvirt */
					virtual = 1;

				if (!cmethod->klass->inited){
					if (!mono_class_init (cmethod->klass))
						goto load_error;
				}

				if (mono_method_signature (cmethod)->pinvoke) {
					MonoMethod *wrapper = mono_marshal_get_native_wrapper (cmethod, check_for_pending_exc, FALSE);
					fsig = mono_method_signature (wrapper);
				} else if (constrained_call) {
					fsig = mono_method_signature (cmethod);
				} else {
					fsig = mono_method_get_signature_full (cmethod, image, token, generic_context);
				}

				mono_save_token_info (cfg, image, token, cmethod);

				n = fsig->param_count + fsig->hasthis;

				if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS) {
					if (check_linkdemand (cfg, method, cmethod, bblock, ip))
						INLINE_FAILURE;
					CHECK_CFG_EXCEPTION;
				}

				if (cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL &&
				    mini_class_is_system_array (cmethod->klass)) {
					array_rank = cmethod->klass->rank;
				}

				if (cmethod->string_ctor)
					g_assert_not_reached ();

			}

			if (!cfg->generic_sharing_context && cmethod && cmethod->klass->generic_container)
				UNVERIFIED;

			if (!cfg->generic_sharing_context && cmethod)
				g_assert (!mono_method_check_context_used (cmethod));

			CHECK_STACK (n);

			//g_assert (!virtual || fsig->hasthis);

			sp -= n;

			if (constrained_call) {
				/*
				 * We have the `constrained.' prefix opcode.
				 */
				if (constrained_call->valuetype && !cmethod->klass->valuetype) {
					MonoInst *load;
					/*
					 * The type parameter is instantiated as a valuetype,
					 * but that type doesn't override the method we're
					 * calling, so we need to box `this'.
					 * sp [0] is a pointer to the data: we need the value
					 * in handle_box (), so load it here.
					 */
					MONO_INST_NEW (cfg, load, mini_type_to_ldind (cfg, &constrained_call->byval_arg));
					type_to_eval_stack_type (cfg, &constrained_call->byval_arg, load);
					load->inst_left = sp [0];
					sp [0] = handle_box (cfg, bblock, load, ip, constrained_call);
				} else if (!constrained_call->valuetype) {
					MonoInst *ins;

					/*
					 * The type parameter is instantiated as a reference
					 * type.  We have a managed pointer on the stack, so
					 * we need to dereference it here.
					 */

					MONO_INST_NEW (cfg, ins, CEE_LDIND_REF);
					ins->inst_i0 = sp [0];
					ins->type = STACK_OBJ;
					ins->klass = mono_class_from_mono_type (&constrained_call->byval_arg);
					sp [0] = ins;
				} else if (cmethod->klass->valuetype)
					virtual = 0;
				constrained_call = NULL;
			}

			if (*ip != CEE_CALLI && check_call_signature (cfg, fsig, sp))
				UNVERIFIED;

			if (cmethod && ((cmethod->flags & METHOD_ATTRIBUTE_STATIC) || cmethod->klass->valuetype) &&
					(cmethod->klass->generic_class || cmethod->klass->generic_container)) {
				gboolean sharing_enabled = mono_class_generic_sharing_enabled (cmethod->klass);
				MonoGenericContext *context = mini_class_get_context (cmethod->klass);
				gboolean context_sharable = mono_generic_context_is_sharable (context, TRUE);

				/*
				 * Pass vtable iff target method might
				 * be shared, which means that sharing
				 * is enabled for its class and its
				 * context is sharable (and it's not a
				 * generic method).
				 */
				if (sharing_enabled && context_sharable &&
					!(mini_method_get_context (cmethod) && mini_method_get_context (cmethod)->method_inst))
					pass_vtable = TRUE;
			}

			if (cmethod && mini_method_get_context (cmethod) &&
					mini_method_get_context (cmethod)->method_inst) {
				gboolean sharing_enabled = mono_class_generic_sharing_enabled (cmethod->klass);
				MonoGenericContext *context = mini_method_get_context (cmethod);
				gboolean context_sharable = mono_generic_context_is_sharable (context, TRUE);

				g_assert (!pass_vtable);

				if (sharing_enabled && context_sharable)
					pass_mrgctx = TRUE;
			}

			if (cfg->generic_sharing_context && cmethod) {
				MonoGenericContext *cmethod_context = mono_method_get_context (cmethod);

				context_used = mono_method_check_context_used (cmethod);

				if (context_used && (cmethod->klass->flags & TYPE_ATTRIBUTE_INTERFACE)) {
					/* Generic method interface
					   calls are resolved via a
					   helper function and don't
					   need an imt. */
					if (!cmethod_context || !cmethod_context->method_inst)
						pass_imt_from_rgctx = TRUE;
				}

				/*
				 * If a shared method calls another
				 * shared method then the caller must
				 * have a generic sharing context
				 * because the magic trampoline
				 * requires it.  FIXME: We shouldn't
				 * have to force the vtable/mrgctx
				 * variable here.  Instead there
				 * should be a flag in the cfg to
				 * request a generic sharing context.
				 */
				if (context_used && ((method->flags & METHOD_ATTRIBUTE_STATIC) || method->klass->valuetype))
					mono_get_vtable_var (cfg);
			}

			if (pass_vtable) {
				if (context_used) {
					MonoInst *rgctx;

					GET_RGCTX (rgctx, context_used);
					vtable_arg = get_runtime_generic_context_ptr (cfg, method, context_used,
						bblock, cmethod->klass, generic_context, rgctx, MONO_RGCTX_INFO_VTABLE, ip);
				} else {
					MonoVTable *vtable = mono_class_vtable (cfg->domain, cmethod->klass);
					
					CHECK_TYPELOAD (cmethod->klass);
					NEW_VTABLECONST (cfg, vtable_arg, vtable);
				}
			}

			if (pass_mrgctx) {
				g_assert (!vtable_arg);

				if (context_used) {
					MonoInst *rgctx;

					GET_RGCTX (rgctx, context_used);
					vtable_arg = get_runtime_generic_context_method_rgctx (cfg, method,
						context_used, bblock, cmethod, generic_context, rgctx, ip);
				} else {
					MonoMethodRuntimeGenericContext *mrgctx;

					mrgctx = mono_method_lookup_rgctx (mono_class_vtable (cfg->domain, cmethod->klass),
						mini_method_get_context (cmethod)->method_inst);

					cfg->disable_aot = TRUE;
					NEW_PCONST (cfg, vtable_arg, mrgctx);
				}

				if (!(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
						(cmethod->flags & METHOD_ATTRIBUTE_FINAL)) {
					if (virtual)
						check_this = TRUE;
					virtual = 0;
				}
			}

			if (pass_imt_from_rgctx) {
				MonoInst *rgctx;

				g_assert (!pass_vtable);
				g_assert (cmethod);

				GET_RGCTX (rgctx, context_used);
				imt_arg = get_runtime_generic_context_method (cfg, method, context_used, bblock, cmethod,
						generic_context, rgctx, MONO_RGCTX_INFO_METHOD, ip);
			}

			if (check_this) {
				MonoInst *check;

				MONO_INST_NEW (cfg, check, OP_CHECK_THIS_PASSTHROUGH);
				check->cil_code = ip;
				check->inst_left = sp [0];
				check->type = sp [0]->type;
				sp [0] = check;
			}

			if (cmethod && virtual && 
			    (cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) && 
		 	    !((cmethod->flags & METHOD_ATTRIBUTE_FINAL) && 
			      cmethod->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK) &&
			    mono_method_signature (cmethod)->generic_param_count) {
				MonoInst *this_temp, *this_arg_temp, *store;
				MonoInst *iargs [4];

				g_assert (mono_method_signature (cmethod)->is_inflated);
				/* Prevent inlining of methods that contain indirect calls */
				INLINE_FAILURE;

				this_temp = mono_compile_create_var (cfg, type_from_stack_type (sp [0]), OP_LOCAL);
				NEW_TEMPSTORE (cfg, store, this_temp->inst_c0, sp [0]);
				MONO_ADD_INS (bblock, store);

				/* FIXME: This should be a managed pointer */
				this_arg_temp = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);

				/* Because of the PCONST below */
				cfg->disable_aot = TRUE;
				NEW_TEMPLOAD (cfg, iargs [0], this_temp->inst_c0);
				if (context_used) {
					MonoInst *rgctx;

					GET_RGCTX (rgctx, context_used);
					iargs [1] = get_runtime_generic_context_method (cfg, method, context_used,
							bblock, cmethod,
							generic_context, rgctx, MONO_RGCTX_INFO_METHOD, ip);
					NEW_TEMPLOADA (cfg, iargs [2], this_arg_temp->inst_c0);
					temp = mono_emit_jit_icall (cfg, bblock,
						mono_helper_compile_generic_method, iargs, ip);
				} else {
					NEW_METHODCONST (cfg, iargs [1], cmethod);
					NEW_TEMPLOADA (cfg, iargs [2], this_arg_temp->inst_c0);
					temp = mono_emit_jit_icall (cfg, bblock, mono_helper_compile_generic_method,
						iargs, ip);
				}

				NEW_TEMPLOAD (cfg, addr, temp);
				NEW_TEMPLOAD (cfg, sp [0], this_arg_temp->inst_c0);

				if ((temp = mono_emit_calli_spilled (cfg, bblock, fsig, sp, addr, ip)) != -1) {
					NEW_TEMPLOAD (cfg, *sp, temp);
					sp++;
				}

				ip += 5;
				ins_flag = 0;
				break;
			}

			/* FIXME: runtime generic context pointer for jumps? */
			/* FIXME: handle this for generic sharing eventually */
			if ((ins_flag & MONO_INST_TAILCALL) && !cfg->generic_sharing_context && !vtable_arg && cmethod && (*ip == CEE_CALL) &&
				 (mono_metadata_signature_equal (mono_method_signature (method), mono_method_signature (cmethod)))) {
				int i;

				/* Prevent inlining of methods with tail calls (the call stack would be altered) */
				INLINE_FAILURE;
				/* FIXME: This assumes the two methods has the same number and type of arguments */
				/*
				 * We implement tail calls by storing the actual arguments into the 
				 * argument variables, then emitting a OP_JMP. Since the actual arguments
				 * can refer to the arg variables, we have to spill them.
				 */
				handle_loaded_temps (cfg, bblock, sp, sp + n);
				for (i = 0; i < n; ++i) {
					/* Prevent argument from being register allocated */
					arg_array [i]->flags |= MONO_INST_VOLATILE;

					/* Check if argument is the same */
					/* 
					 * FIXME: This loses liveness info, so it can only be done if the
					 * argument is not register allocated.
					 */
					NEW_ARGLOAD (cfg, ins, i);
					if ((ins->opcode == sp [i]->opcode) && (ins->inst_i0 == sp [i]->inst_i0))
						continue;

					NEW_ARGSTORE (cfg, ins, i, sp [i]);
					/* FIXME: handle CEE_STIND_R4 */
					if (ins->opcode == CEE_STOBJ) {
						NEW_ARGLOADA (cfg, ins, i);
						handle_stobj (cfg, bblock, ins, sp [i], sp [i]->cil_code, ins->klass, FALSE, FALSE, FALSE);
					}
					else
						MONO_ADD_INS (bblock, ins);
				}
				MONO_INST_NEW (cfg, ins, OP_JMP);
				ins->inst_p0 = cmethod;
				ins->inst_p1 = arg_array [0];
				MONO_ADD_INS (bblock, ins);
				link_bblock (cfg, bblock, end_bblock);			
				start_new_bblock = 1;
				/* skip CEE_RET as well */
				ip += 6;
				ins_flag = 0;
				break;
			}
			if (cmethod && (cfg->opt & MONO_OPT_INTRINS) && (ins = mini_get_inst_for_method (cfg, cmethod, fsig, sp))) {
				if (MONO_TYPE_IS_VOID (fsig->ret)) {
					MONO_ADD_INS (bblock, ins);
				} else {
					type_to_eval_stack_type (cfg, fsig->ret, ins);
					*sp = ins;
					sp++;
				}

				ip += 5;
				ins_flag = 0;
				break;
			}

			handle_loaded_temps (cfg, bblock, stack_start, sp);

			if ((cfg->opt & MONO_OPT_INLINE) && cmethod && //!check_this &&
			    (!virtual || !(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) || (cmethod->flags & METHOD_ATTRIBUTE_FINAL)) && 
			    mono_method_check_inlining (cfg, cmethod) &&
				 !g_list_find (dont_inline, cmethod)) {
				int costs;
				MonoBasicBlock *ebblock;
				gboolean allways = FALSE;

				if ((cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
					(cmethod->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
					/* Prevent inlining of methods that call wrappers */
					INLINE_FAILURE;
					cmethod = mono_marshal_get_native_wrapper (cmethod, check_for_pending_exc, FALSE);
					allways = TRUE;
				}

 				if ((costs = inline_method (cfg, cmethod, fsig, bblock, sp, ip, real_offset, dont_inline, &ebblock, allways))) {
					ip += 5;
					real_offset += 5;

					GET_BBLOCK (cfg, bblock, ip);
					ebblock->next_bb = bblock;
					link_bblock (cfg, ebblock, bblock);

 					if (!MONO_TYPE_IS_VOID (fsig->ret))
 						sp++;

					/* indicates start of a new block, and triggers a load of all 
					   stack arguments at bb boundarie */
					bblock = ebblock;

					inline_costs += costs;
					ins_flag = 0;
					break;
				}
			}
			
			inline_costs += 10 * num_calls++;

			/* tail recursion elimination */
			if ((cfg->opt & MONO_OPT_TAILC) && *ip == CEE_CALL && cmethod == method && ip [5] == CEE_RET &&
					!vtable_arg) {
				gboolean has_vtargs = FALSE;
				int i;
				
				/* Prevent inlining of methods with tail calls (the call stack would be altered) */
				INLINE_FAILURE;
				/* keep it simple */
				for (i =  fsig->param_count - 1; i >= 0; i--) {
					if (MONO_TYPE_ISSTRUCT (mono_method_signature (cmethod)->params [i])) 
						has_vtargs = TRUE;
				}

				if (!has_vtargs) {
					for (i = 0; i < n; ++i) {
						/* FIXME: handle CEE_STIND_R4 */
						NEW_ARGSTORE (cfg, ins, i, sp [i]);
						MONO_ADD_INS (bblock, ins);
					}
					MONO_INST_NEW (cfg, ins, OP_BR);
					MONO_ADD_INS (bblock, ins);
					tblock = start_bblock->out_bb [0];
					link_bblock (cfg, bblock, tblock);
					ins->inst_target_bb = tblock;
					start_new_bblock = 1;

					/* skip the CEE_RET, too */
					if (ip_in_bb (cfg, bblock, ip + 5))
						ip += 6;
					else
						ip += 5;
					ins_flag = 0;
					break;
				}
			}

			if (ip_in_bb (cfg, bblock, ip + 5) 
				&& (!MONO_TYPE_ISSTRUCT (fsig->ret))
				&& (!MONO_TYPE_IS_VOID (fsig->ret) || (cmethod && cmethod->string_ctor))
				&& (CODE_IS_STLOC (ip + 5) || ip [5] == CEE_POP || ip [5] == CEE_RET))
				/* No need to spill */
				no_spill = TRUE;
			else
				no_spill = FALSE;

			if (context_used && !imt_arg &&
					(!mono_method_is_generic_sharable_impl (cmethod, TRUE) ||
						!mono_class_generic_sharing_enabled (cmethod->klass)) &&
					(!virtual || cmethod->flags & METHOD_ATTRIBUTE_FINAL ||
						!(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL))) {
				MonoInst *rgctx;

				INLINE_FAILURE;

				g_assert (cfg->generic_sharing_context && cmethod);
				g_assert (!addr);

				/*
				 * We are compiling a call to
				 * non-shared generic code from shared
				 * code, which means that we have to
				 * look up the method in the rgctx and
				 * do an indirect call.
				 */
				GET_RGCTX (rgctx, context_used);
				addr = get_runtime_generic_context_method (cfg, method, context_used, bblock, cmethod,
						generic_context, rgctx, MONO_RGCTX_INFO_GENERIC_METHOD_CODE, ip);

			}

			if (addr) {
				g_assert (!imt_arg);

				if (*ip == CEE_CALL) {
					g_assert (context_used);
				} else if (*ip == CEE_CALLI) {
					g_assert (!vtable_arg);
				} else {
					g_assert (cmethod->flags & METHOD_ATTRIBUTE_FINAL ||
							!(cmethod->flags & METHOD_ATTRIBUTE_FINAL));
				}

				/* Prevent inlining of methods with indirect calls */
				INLINE_FAILURE;
				if (no_spill) {
					ins = (MonoInst*)mono_emit_rgctx_calli (cfg, bblock, fsig, sp, addr, vtable_arg, ip);
					*sp++ = ins;					
				} else {
					temp = mono_emit_rgctx_calli_spilled (cfg, bblock, fsig, sp, addr, vtable_arg, ip);
					if (temp != -1) {
						NEW_TEMPLOAD (cfg, *sp, temp);
						NEW_TEMPLOAD_SOFT_FLOAT (cfg, bblock, *sp, temp, ip);
						sp++;
					}
				}			
			} else if (array_rank) {
				MonoInst *addr;

				if (strcmp (cmethod->name, "Set") == 0) { /* array Set */ 
					if (sp [fsig->param_count]->type == STACK_OBJ) {
						MonoInst *iargs [2];
						MonoInst *array, *to_store, *store;

						handle_loaded_temps (cfg, bblock, stack_start, sp);
						
						array = mono_compile_create_var (cfg, type_from_stack_type (sp [0]), OP_LOCAL);
						NEW_TEMPSTORE (cfg, store, array->inst_c0, sp [0]);
						MONO_ADD_INS (bblock, store);
						NEW_TEMPLOAD (cfg, iargs [0], array->inst_c0);

						to_store = mono_compile_create_var (cfg, type_from_stack_type (sp [fsig->param_count]), OP_LOCAL);
						NEW_TEMPSTORE (cfg, store, to_store->inst_c0, sp [fsig->param_count]);
						/* FIXME: handle CEE_STIND_R4 */
						MONO_ADD_INS (bblock, store);
						NEW_TEMPLOAD (cfg, iargs [1], to_store->inst_c0);

						/*
						 * We first save the args for the call so that the args are copied to the stack
						 * and a new instruction tree for them is created. If we don't do this,
						 * the same MonoInst is added to two different trees and this is not 
						 * allowed by burg.
						 */
						mono_emit_jit_icall (cfg, bblock, mono_helper_stelem_ref_check, iargs, ip);

						NEW_TEMPLOAD (cfg, sp [0], array->inst_c0);
						NEW_TEMPLOAD (cfg, sp [fsig->param_count], to_store->inst_c0);
					}

					addr = mini_get_ldelema_ins (cfg, bblock, cmethod, sp, ip, TRUE);
					NEW_INDSTORE (cfg, ins, addr, sp [fsig->param_count], fsig->params [fsig->param_count - 1]);
					if (ins->opcode == CEE_STOBJ) {
						handle_stobj (cfg, bblock, addr, sp [fsig->param_count], ip, mono_class_from_mono_type (fsig->params [fsig->param_count-1]), FALSE, FALSE, TRUE);
#ifdef MONO_ARCH_SOFT_FLOAT
					} else if (ins->opcode == CEE_STIND_R4) {
						handle_store_float (cfg, bblock, addr, sp [fsig->param_count], ip);
#endif
					} else {
						MONO_ADD_INS (bblock, ins);
					}

				} else if (strcmp (cmethod->name, "Get") == 0) { /* array Get */
					addr = mini_get_ldelema_ins (cfg, bblock, cmethod, sp, ip, FALSE);
					NEW_INDLOAD (cfg, ins, addr, fsig->ret);
					if (ins->opcode == CEE_LDIND_R4) {
#ifdef MONO_ARCH_SOFT_FLOAT
						int temp;
						temp = handle_load_float (cfg, bblock, addr, ip);
						NEW_TEMPLOAD (cfg, *sp, temp);
						sp++;
#else
						*sp++ = ins;
#endif
					} else {
						*sp++ = ins;
					}
				} else if (strcmp (cmethod->name, "Address") == 0) { /* array Address */
					if (!cmethod->klass->element_class->valuetype && !readonly) {
						MonoInst* check;
						//* Needed by the code generated in inssel.brg * /
						mono_get_got_var (cfg);

						MONO_INST_NEW (cfg, check, OP_CHECK_ARRAY_TYPE);
						check->klass = cmethod->klass;
						check->inst_left = sp [0];
						check->type = STACK_OBJ;
						sp [0] = check;
					}

					readonly = FALSE;
					addr = mini_get_ldelema_ins (cfg, bblock, cmethod, sp, ip, FALSE);
					*sp++ = addr;
				} else {
					g_assert_not_reached ();
				}

			} else {
				/* Prevent inlining of methods which call other methods */
				INLINE_FAILURE;
				if (mini_redirect_call (&temp, cfg, bblock, cmethod, fsig, sp, ip, virtual ? sp [0] : NULL)) {
					if (temp != -1) {
						NEW_TEMPLOAD (cfg, *sp, temp);
						sp++;
					}
				} else if (no_spill) {
					ins = (MonoInst*)mono_emit_rgctx_method_call (cfg, bblock, cmethod, fsig, sp,
							vtable_arg, imt_arg, ip, virtual ? sp [0] : NULL);
					*sp++ = ins;
				} else {
					if ((temp = mono_emit_rgctx_method_call_spilled (cfg, bblock, cmethod, fsig, sp,
							vtable_arg, imt_arg, ip, virtual ? sp [0] : NULL)) != -1) {
						MonoInst *load;
						NEW_TEMPLOAD (cfg, load, temp);

#ifdef MONO_ARCH_SOFT_FLOAT
						if (load->opcode == CEE_LDIND_R4) {
							NEW_TEMPLOADA (cfg, load, temp);
							temp = handle_load_float (cfg, bblock, load, ip);
							NEW_TEMPLOAD (cfg, load, temp);
						}
#endif
						*sp++ = load;
					}
				}
			}

			ip += 5;
			ins_flag = 0;
			break;
		}
		case CEE_RET:
			if (cfg->method != method) {
				/* return from inlined method */
				if (return_var) {
					MonoInst *store;
					CHECK_STACK (1);
					--sp;
					//g_assert (returnvar != -1);
					NEW_TEMPSTORE (cfg, store, return_var->inst_c0, *sp);
					store->cil_code = sp [0]->cil_code;
					if (store->opcode == CEE_STOBJ) {
						g_assert_not_reached ();
						NEW_TEMPLOADA (cfg, store, return_var->inst_c0);
						/* FIXME: it is possible some optimization will pass the a heap pointer for the struct address, so we'll need the write barrier */
						handle_stobj (cfg, bblock, store, *sp, sp [0]->cil_code, return_var->klass, FALSE, FALSE, FALSE);
#ifdef MONO_ARCH_SOFT_FLOAT
					} else if (store->opcode == CEE_STIND_R4) {
						NEW_TEMPLOADA (cfg, store, return_var->inst_c0);
						handle_store_float (cfg, bblock, store, *sp, sp [0]->cil_code);
#endif
					} else
						MONO_ADD_INS (bblock, store);
				} 
			} else {
				if (cfg->ret) {
					g_assert (!return_var);
					CHECK_STACK (1);
					--sp;
					MONO_INST_NEW (cfg, ins, OP_NOP);
					ins->opcode = mini_type_to_stind (cfg, mono_method_signature (method)->ret);
					if (ins->opcode == CEE_STOBJ) {
						NEW_RETLOADA (cfg, ins);
						/* FIXME: it is possible some optimization will pass the a heap pointer for the struct address, so we'll need the write barrier */
						handle_stobj (cfg, bblock, ins, *sp, ip, cfg->ret->klass, FALSE, FALSE, FALSE);
					} else {
						ins->opcode = OP_SETRET;
						ins->inst_i0 = *sp;;
						ins->inst_i1 = NULL;
						MONO_ADD_INS (bblock, ins);
					}
				}
			}
			if (sp != stack_start)
				UNVERIFIED;
			MONO_INST_NEW (cfg, ins, OP_BR);
			ip++;
			ins->inst_target_bb = end_bblock;
			MONO_ADD_INS (bblock, ins);
			link_bblock (cfg, bblock, end_bblock);
			start_new_bblock = 1;
			break;
		case CEE_BR_S:
			CHECK_OPSIZE (2);
			MONO_INST_NEW (cfg, ins, OP_BR);
			ip++;
			MONO_ADD_INS (bblock, ins);
			target = ip + 1 + (signed char)(*ip);
			++ip;
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			CHECK_BBLOCK (target, ip, tblock);
			ins->inst_target_bb = tblock;
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			start_new_bblock = 1;
			inline_costs += BRANCH_COST;
			break;
		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (1);
			if (sp [-1]->type == STACK_VTYPE || sp [-1]->type == STACK_R8)
				UNVERIFIED;
			MONO_INST_NEW (cfg, ins, *ip + BIG_BRANCH_OFFSET);
			ip++;
			target = ip + 1 + *(signed char*)ip;
			ip++;
			ADD_UNCOND (ins->opcode == CEE_BRTRUE);
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			inline_costs += BRANCH_COST;
			break;
		case CEE_BEQ_S:
		case CEE_BGE_S:
		case CEE_BGT_S:
		case CEE_BLE_S:
		case CEE_BLT_S:
		case CEE_BNE_UN_S:
		case CEE_BGE_UN_S:
		case CEE_BGT_UN_S:
		case CEE_BLE_UN_S:
		case CEE_BLT_UN_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (2);
			MONO_INST_NEW (cfg, ins, *ip + BIG_BRANCH_OFFSET);
			ip++;
			target = ip + 1 + *(signed char*)ip;
			ip++;
#ifdef MONO_ARCH_SOFT_FLOAT
			if (sp [-1]->type == STACK_R8 || sp [-2]->type == STACK_R8) {
				ins->opcode = condbr_to_fp_br (ins->opcode);
				sp -= 2;
				ins->inst_left = sp [0];
				ins->inst_right = sp [1];
				ins->type = STACK_I4;
				*sp++ = emit_tree (cfg, bblock, ins, ins->cil_code);
				MONO_INST_NEW (cfg, ins, CEE_BRTRUE);
				ADD_UNCOND (TRUE);
			} else {
				ADD_BINCOND (NULL);
			}
#else
			ADD_BINCOND (NULL);
#endif
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			inline_costs += BRANCH_COST;
			break;
		case CEE_BR:
			CHECK_OPSIZE (5);
			MONO_INST_NEW (cfg, ins, OP_BR);
			ip++;
			MONO_ADD_INS (bblock, ins);
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			CHECK_BBLOCK (target, ip, tblock);
			ins->inst_target_bb = tblock;
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			start_new_bblock = 1;
			inline_costs += BRANCH_COST;
			break;
		case CEE_BRFALSE:
		case CEE_BRTRUE:
			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			if (sp [-1]->type == STACK_VTYPE || sp [-1]->type == STACK_R8)
				UNVERIFIED;
			MONO_INST_NEW (cfg, ins, *ip);
			ip++;
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
			ADD_UNCOND(ins->opcode == CEE_BRTRUE);
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			inline_costs += BRANCH_COST;
			break;
		case CEE_BEQ:
		case CEE_BGE:
		case CEE_BGT:
		case CEE_BLE:
		case CEE_BLT:
		case CEE_BNE_UN:
		case CEE_BGE_UN:
		case CEE_BGT_UN:
		case CEE_BLE_UN:
		case CEE_BLT_UN:
			CHECK_OPSIZE (5);
			CHECK_STACK (2);
			MONO_INST_NEW (cfg, ins, *ip);
			ip++;
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
#ifdef MONO_ARCH_SOFT_FLOAT
			if (sp [-1]->type == STACK_R8 || sp [-2]->type == STACK_R8) {
				ins->opcode = condbr_to_fp_br (ins->opcode);
				sp -= 2;
				ins->inst_left = sp [0];
				ins->inst_right = sp [1];
				ins->type = STACK_I4;
				*sp++ = emit_tree (cfg, bblock, ins, ins->cil_code);
				MONO_INST_NEW (cfg, ins, CEE_BRTRUE);
				ADD_UNCOND (TRUE);
			} else {
				ADD_BINCOND (NULL);
			}
#else
			ADD_BINCOND (NULL);
#endif
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			inline_costs += BRANCH_COST;
			break;
		case CEE_SWITCH:
			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			n = read32 (ip + 1);
			MONO_INST_NEW (cfg, ins, OP_SWITCH);
			--sp;
			ins->inst_left = *sp;
			if ((ins->inst_left->type != STACK_I4) && (ins->inst_left->type != STACK_PTR)) 
				UNVERIFIED;
			ip += 5;
			CHECK_OPSIZE (n * sizeof (guint32));
			target = ip + n * sizeof (guint32);
			MONO_ADD_INS (bblock, ins);
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			ins->klass = GUINT_TO_POINTER (n);
			ins->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (n + 1));
			ins->inst_many_bb [n] = tblock;

			for (i = 0; i < n; ++i) {
				GET_BBLOCK (cfg, tblock, target + (gint32)read32(ip));
				link_bblock (cfg, bblock, tblock);
				ins->inst_many_bb [i] = tblock;
				ip += 4;
			}
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
				CHECK_UNVERIFIABLE (cfg);
			}
			/* Needed by the code generated in inssel.brg */
			mono_get_got_var (cfg);
			inline_costs += (BRANCH_COST * 2);
			break;
		case CEE_LDIND_I1:
		case CEE_LDIND_U1:
		case CEE_LDIND_I2:
		case CEE_LDIND_U2:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
		case CEE_LDIND_I8:
		case CEE_LDIND_I:
		case CEE_LDIND_R4:
		case CEE_LDIND_R8:
		case CEE_LDIND_REF:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			ins->inst_i0 = *sp;
			*sp++ = ins;
			ins->type = ldind_type [*ip - CEE_LDIND_I1];
			ins->flags |= ins_flag;
			ins_flag = 0;
			if (ins->type == STACK_OBJ)
				ins->klass = mono_defaults.object_class;
#ifdef MONO_ARCH_SOFT_FLOAT
			if (*ip == CEE_LDIND_R4) {
				int temp;
				--sp;
				temp = handle_load_float (cfg, bblock, ins->inst_i0, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);
				sp++;
			}
#endif
			++ip;
			break;
		case CEE_STIND_REF:
		case CEE_STIND_I1:
		case CEE_STIND_I2:
		case CEE_STIND_I4:
		case CEE_STIND_I8:
		case CEE_STIND_R4:
		case CEE_STIND_R8:
			CHECK_STACK (2);
#ifdef MONO_ARCH_SOFT_FLOAT
			if (*ip == CEE_STIND_R4) {
				sp -= 2;
				handle_store_float (cfg, bblock, sp [0], sp [1], ip);
				ip++;
				break;
			}
#endif
#if HAVE_WRITE_BARRIERS
			if (*ip == CEE_STIND_REF && method->wrapper_type != MONO_WRAPPER_WRITE_BARRIER && !((sp [-1]->opcode == OP_PCONST) && (sp [-1]->inst_p0 == 0))) {
				/* insert call to write barrier */
				MonoMethod *write_barrier = mono_marshal_get_write_barrier ();
				sp -= 2;
				mono_emit_method_call_spilled (cfg, bblock, write_barrier, mono_method_signature (write_barrier), sp, ip, NULL);
				ip++;
				break;
			}
#endif
			MONO_INST_NEW (cfg, ins, *ip);
			ip++;
			sp -= 2;
			handle_loaded_temps (cfg, bblock, stack_start, sp);
			MONO_ADD_INS (bblock, ins);
			ins->inst_i0 = sp [0];
			ins->inst_i1 = sp [1];
			ins->flags |= ins_flag;
			ins_flag = 0;
			inline_costs += 1;
			break;
		case CEE_MUL:
			CHECK_STACK (2);
			ADD_BINOP (*ip);

#ifdef MONO_ARCH_NO_EMULATE_MUL_IMM
			/* FIXME: This breaks with ssapre (mono -O=ssapre loader.exe) */
			if ((ins->inst_right->opcode == OP_ICONST) && !(cfg->opt & MONO_OPT_SSAPRE)) {
				switch (ins->opcode) {
				case CEE_MUL:
					ins->opcode = OP_IMUL_IMM;
					ins->inst_imm = ins->inst_right->inst_c0;
					break;
				case OP_LMUL:
					ins->opcode = OP_LMUL_IMM;
					ins->inst_imm = ins->inst_right->inst_c0;
					break;
				default:
					g_assert_not_reached ();
				}
			}
#endif

			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
			}
			ip++;
			break;
		case CEE_ADD:
		case CEE_SUB:
		case CEE_DIV:
		case CEE_DIV_UN:
		case CEE_REM:
		case CEE_REM_UN:
		case CEE_AND:
		case CEE_OR:
		case CEE_XOR:
		case CEE_SHL:
		case CEE_SHR:
		case CEE_SHR_UN:
			CHECK_STACK (2);
			ADD_BINOP (*ip);
			/* special case that gives a nice speedup and happens to workaorund a ppc jit but (for the release)
			 * later apply the speedup to the left shift as well
			 * See BUG# 57957.
			 */
			if ((ins->opcode == OP_LSHR_UN) && (ins->type == STACK_I8) 
					&& (ins->inst_right->opcode == OP_ICONST) && (ins->inst_right->inst_c0 == 32)) {
				ins->opcode = OP_LSHR_UN_32;
				/*g_print ("applied long shr speedup to %s\n", cfg->method->name);*/
				ip++;
				break;
			}
			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
			}
			ip++;
			break;
		case CEE_NEG:
		case CEE_NOT:
		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_U4:
		case CEE_CONV_I8:
		case CEE_CONV_U8:
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
		case CEE_CONV_R_UN:
			CHECK_STACK (1);
			ADD_UNOP (*ip);

#ifdef MONO_ARCH_SOFT_FLOAT
			/*
			 * Its rather hard to emit the soft float code during the decompose
			 * pass, so avoid it in some specific cases.
			 */
			if (ins->opcode == OP_LCONV_TO_R4) {
				MonoInst *conv;

				ins->opcode = OP_LCONV_TO_R8;
				ins->type = STACK_R8;

				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);

				MONO_INST_NEW (cfg, conv, CEE_CONV_R4);
				conv->inst_left = sp [-1];
				conv->type = STACK_R8;
				sp [-1] = ins;

				ip++;
				break;
			}
#endif

			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
			}
			ip++;			
			break;
		case CEE_CONV_OVF_I4:
		case CEE_CONV_OVF_I1:
		case CEE_CONV_OVF_I2:
		case CEE_CONV_OVF_I:
		case CEE_CONV_OVF_U:
			CHECK_STACK (1);

			if (sp [-1]->type == STACK_R8) {
				ADD_UNOP (CEE_CONV_OVF_I8);
				ADD_UNOP (*ip);
			} else {
				ADD_UNOP (*ip);
			}

			ip++;
			break;
		case CEE_CONV_OVF_U1:
		case CEE_CONV_OVF_U2:
		case CEE_CONV_OVF_U4:
			CHECK_STACK (1);

			if (sp [-1]->type == STACK_R8) {
				ADD_UNOP (CEE_CONV_OVF_U8);
				ADD_UNOP (*ip);
			} else {
				ADD_UNOP (*ip);
			}

			ip++;
			break;
		case CEE_CONV_OVF_I1_UN:
		case CEE_CONV_OVF_I2_UN:
		case CEE_CONV_OVF_I4_UN:
		case CEE_CONV_OVF_I8_UN:
		case CEE_CONV_OVF_U1_UN:
		case CEE_CONV_OVF_U2_UN:
		case CEE_CONV_OVF_U4_UN:
		case CEE_CONV_OVF_U8_UN:
		case CEE_CONV_OVF_I_UN:
		case CEE_CONV_OVF_U_UN:
			CHECK_STACK (1);
			ADD_UNOP (*ip);
			ip++;
			break;
		case CEE_CPOBJ:
			CHECK_OPSIZE (5);
			CHECK_STACK (2);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			sp -= 2;
			if (generic_class_is_reference_type (cfg, klass)) {
				MonoInst *store, *load;
				MONO_INST_NEW (cfg, load, CEE_LDIND_REF);
				load->inst_i0 = sp [1];
				load->type = STACK_OBJ;
				load->klass = klass;
				load->flags |= ins_flag;
				MONO_INST_NEW (cfg, store, CEE_STIND_REF);
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				MONO_ADD_INS (bblock, store);
				store->inst_i0 = sp [0];
				store->inst_i1 = load;
				store->flags |= ins_flag;
			} else {
				guint32 align;

				n = mono_class_value_size (klass, &align);
				if ((cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
					MonoInst *copy;
					NEW_MEMCPY (cfg, copy, sp [0], sp [1], n, align);
					MONO_ADD_INS (bblock, copy);
				} else {
					MonoMethod *memcpy_method = get_memcpy_method ();
					MonoInst *iargs [3];
					iargs [0] = sp [0];
					iargs [1] = sp [1];
					NEW_ICONST (cfg, iargs [2], n);

					mono_emit_method_call_spilled (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL);
				}
			}
			ins_flag = 0;
			ip += 5;
			break;
		case CEE_LDOBJ: {
			MonoInst *iargs [3];
			int loc_index = -1;
			int stloc_len = 0;
			guint32 align;

			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			--sp;
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			if (generic_class_is_reference_type (cfg, klass)) {
				MONO_INST_NEW (cfg, ins, CEE_LDIND_REF);
				ins->inst_i0 = sp [0];
				ins->type = STACK_OBJ;
				ins->klass = klass;
				ins->flags |= ins_flag;
				ins_flag = 0;
				*sp++ = ins;
				ip += 5;
				break;
			}

			/* Optimize the common ldobj+stloc combination */
			switch (ip [5]) {
			case CEE_STLOC_S:
				loc_index = ip [6];
				stloc_len = 2;
				break;
			case CEE_STLOC_0:
			case CEE_STLOC_1:
			case CEE_STLOC_2:
			case CEE_STLOC_3:
				loc_index = ip [5] - CEE_STLOC_0;
				stloc_len = 1;
				break;
			default:
				break;
			}

			if ((loc_index != -1) && ip_in_bb (cfg, bblock, ip + 5)) {
				CHECK_LOCAL (loc_index);
				NEW_LOCSTORE (cfg, ins, loc_index, *sp);

				/* FIXME: handle CEE_STIND_R4 */
				if (ins->opcode == CEE_STOBJ) {
					handle_loaded_temps (cfg, bblock, stack_start, sp);
					g_assert (ins->opcode == CEE_STOBJ);
					NEW_LOCLOADA (cfg, ins, loc_index);
					handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE, FALSE);
					ip += 5;
					ip += stloc_len;
					break;
				}
			}

			n = mono_class_value_size (klass, &align);
			ins = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);
			NEW_TEMPLOADA (cfg, iargs [0], ins->inst_c0);
			if ((cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
				MonoInst *copy;
				NEW_MEMCPY (cfg, copy, iargs [0], *sp, n, align);
				MONO_ADD_INS (bblock, copy);
			} else {
				MonoMethod *memcpy_method = get_memcpy_method ();
				iargs [1] = *sp;
				NEW_ICONST (cfg, iargs [2], n);

				mono_emit_method_call_spilled (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL);
			}
			NEW_TEMPLOAD (cfg, *sp, ins->inst_c0);
			++sp;
			ip += 5;
			ins_flag = 0;
			inline_costs += 1;
			break;
		}
		case CEE_LDSTR:
			CHECK_STACK_OVF (1);
			CHECK_OPSIZE (5);
			n = read32 (ip + 1);

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				/* FIXME: moving GC */
				NEW_PCONST (cfg, ins, mono_method_get_wrapper_data (method, n));
				ins->type = STACK_OBJ;
				ins->klass = mono_defaults.string_class;
				*sp = ins;
			}
			else if (method->wrapper_type != MONO_WRAPPER_NONE) {
				int temp;
				MonoInst *iargs [1];

				NEW_PCONST (cfg, iargs [0], mono_method_get_wrapper_data (method, n));				
				temp = mono_emit_jit_icall (cfg, bblock, mono_string_new_wrapper, iargs, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);

			} else {

				if (cfg->opt & MONO_OPT_SHARED) {
					int temp;
					MonoInst *iargs [3];
					MonoInst* domain_var;
					
					if (cfg->compile_aot) {
						/* FIXME: bug when inlining methods from different assemblies (n is a token valid just in one). */
						cfg->ldstr_list = g_list_prepend (cfg->ldstr_list, GINT_TO_POINTER (n));
					}
					/* avoid depending on undefined C behavior in sequence points */
					domain_var = mono_get_domainvar (cfg);
					NEW_TEMPLOAD (cfg, iargs [0], domain_var->inst_c0);
					NEW_IMAGECONST (cfg, iargs [1], image);
					NEW_ICONST (cfg, iargs [2], mono_metadata_token_index (n));
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldstr, iargs, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
					mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
				} else {
					if (bblock->out_of_line) {
						MonoInst *iargs [2];
						int temp;

						if (cfg->method->klass->image == mono_defaults.corlib) {
							/* 
							 * Avoid relocations and save some code size by using a 
							 * version of helper_ldstr specialized to mscorlib.
							 */
							NEW_ICONST (cfg, iargs [0], mono_metadata_token_index (n));
							temp = mono_emit_jit_icall (cfg, bblock, mono_helper_ldstr_mscorlib, iargs, ip);
						} else {
							/* Avoid creating the string object */
							NEW_IMAGECONST (cfg, iargs [0], image);
							NEW_ICONST (cfg, iargs [1], mono_metadata_token_index (n));
							temp = mono_emit_jit_icall (cfg, bblock, mono_helper_ldstr, iargs, ip);
						}
						NEW_TEMPLOAD (cfg, *sp, temp);
					} 
					else
					if (cfg->compile_aot) {
						NEW_LDSTRCONST (cfg, ins, image, n);
						*sp = ins;
					} 
					else {
						NEW_PCONST (cfg, ins, NULL);
						ins->type = STACK_OBJ;
						ins->inst_p0 = mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
						ins->klass = mono_defaults.string_class;
						*sp = ins;
					}
				}
			}

			sp++;
			ip += 5;
			break;
		case CEE_NEWOBJ: {
			MonoInst *iargs [2];
			MonoMethodSignature *fsig;
			MonoInst this_ins;
			int temp;
			MonoInst *vtable_arg = NULL;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			cmethod = mini_get_method (cfg, method, token, NULL, generic_context);
			if (!cmethod)
				goto load_error;
			fsig = mono_method_get_signature (cmethod, image, token);

			mono_save_token_info (cfg, image, token, cmethod);

			if (!mono_class_init (cmethod->klass))
				goto load_error;

			if (cfg->generic_sharing_context)
				context_used = mono_method_check_context_used (cmethod);

			if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS) {
				if (check_linkdemand (cfg, method, cmethod, bblock, ip))
					INLINE_FAILURE;
				CHECK_CFG_EXCEPTION;
			} else if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR) {
				ensure_method_is_allowed_to_call_method (cfg, method, cmethod, bblock, ip);
			}

			if (cmethod->klass->valuetype && mono_class_generic_sharing_enabled (cmethod->klass) &&
					mono_method_is_generic_sharable_impl (cmethod, TRUE)) {
				if (cmethod->is_inflated && mono_method_get_context (cmethod)->method_inst) {
					if (context_used) {
						MonoInst *rgctx;

						GET_RGCTX (rgctx, context_used);
						vtable_arg = get_runtime_generic_context_method_rgctx (cfg, method,
							context_used, bblock, cmethod, generic_context, rgctx, ip);
					} else {
						MonoVTable *vtable = mono_class_vtable (cfg->domain, cmethod->klass);
						MonoMethodRuntimeGenericContext *mrgctx;

						mrgctx = mono_method_lookup_rgctx (vtable,
							mini_method_get_context (cmethod)->method_inst);

						NEW_PCONST (cfg, vtable_arg, mrgctx);
					}
				} else {
					if (context_used) {
						MonoInst *rgctx;

						GET_RGCTX (rgctx, context_used);
						vtable_arg = get_runtime_generic_context_ptr (cfg, method, context_used,
							bblock, cmethod->klass, generic_context,
							rgctx, MONO_RGCTX_INFO_VTABLE, ip);
					} else {
						MonoVTable *vtable = mono_class_vtable (cfg->domain, cmethod->klass);

						CHECK_TYPELOAD (cmethod->klass);
						NEW_VTABLECONST (cfg, vtable_arg, vtable);
					}
				}
			}

			n = fsig->param_count;
			CHECK_STACK (n);
 
			/* 
			 * Generate smaller code for the common newobj <exception> instruction in
			 * argument checking code.
			 */
			if (bblock->out_of_line && cmethod->klass->image == mono_defaults.corlib && n <= 2 && 
				((n < 1) || (!fsig->params [0]->byref && fsig->params [0]->type == MONO_TYPE_STRING)) && 
				((n < 2) || (!fsig->params [1]->byref && fsig->params [1]->type == MONO_TYPE_STRING))) {
				MonoInst *iargs [3];
				int temp;

				g_assert (!vtable_arg);

				sp -= n;

				NEW_ICONST (cfg, iargs [0], cmethod->klass->type_token);
				switch (n) {
				case 0:
					temp = mono_emit_jit_icall (cfg, bblock, mono_create_corlib_exception_0, iargs, ip);
					break;
				case 1:
					iargs [1] = sp [0];
					temp = mono_emit_jit_icall (cfg, bblock, mono_create_corlib_exception_1, iargs, ip);
					break;
				case 2:
					iargs [1] = sp [0];
					iargs [2] = sp [1];
					temp = mono_emit_jit_icall (cfg, bblock, mono_create_corlib_exception_2, iargs, ip);
					break;
				default:
					g_assert_not_reached ();
				}
				NEW_TEMPLOAD (cfg, ins, temp);
				*sp ++ = ins;

				ip += 5;
				inline_costs += 5;
				break;
			}

			/* move the args to allow room for 'this' in the first position */
			while (n--) {
				--sp;
				sp [1] = sp [0];
			}

			/* check_call_signature () requires sp[0] to be set */
			this_ins.type = STACK_OBJ;
			sp [0] = &this_ins;
			if (check_call_signature (cfg, fsig, sp))
				UNVERIFIED;

			handle_loaded_temps (cfg, bblock, stack_start, sp);

			if (mini_class_is_system_array (cmethod->klass)) {
				g_assert (!context_used);
				g_assert (!vtable_arg);

				NEW_METHODCONST (cfg, *sp, cmethod);

				if (fsig->param_count == 2)
					/* Avoid varargs in the common case */
					temp = mono_emit_jit_icall (cfg, bblock, mono_array_new_2, sp, ip);
				else
					temp = handle_array_new (cfg, bblock, fsig->param_count, sp, ip);
			} else if (cmethod->string_ctor) {
				g_assert (!context_used);
				g_assert (!vtable_arg);

				/* we simply pass a null pointer */
				NEW_PCONST (cfg, *sp, NULL); 
				/* now call the string ctor */
				temp = mono_emit_method_call_spilled (cfg, bblock, cmethod, fsig, sp, ip, NULL);
			} else {
				MonoInst* callvirt_this_arg = NULL;
				
				if (cmethod->klass->valuetype) {
					iargs [0] = mono_compile_create_var (cfg, &cmethod->klass->byval_arg, OP_LOCAL);
					temp = iargs [0]->inst_c0;

					NEW_TEMPLOADA (cfg, *sp, temp);

					handle_initobj (cfg, bblock, *sp, NULL, cmethod->klass, stack_start, sp);

					NEW_TEMPLOADA (cfg, *sp, temp);

					/* 
					 * The code generated by mini_emit_virtual_call () expects
					 * iargs [0] to be a boxed instance, but luckily the vcall
					 * will be transformed into a normal call there.
					 */
				} else if (context_used) {
					MonoInst *rgctx, *data;
					int rgctx_info;

					GET_RGCTX (rgctx, context_used);
					if (cfg->opt & MONO_OPT_SHARED)
						rgctx_info = MONO_RGCTX_INFO_KLASS;
					else
						rgctx_info = MONO_RGCTX_INFO_VTABLE;
					data = get_runtime_generic_context_ptr (cfg, method, context_used, bblock,
						cmethod->klass, generic_context, rgctx, rgctx_info, ip);

					temp = handle_alloc_from_inst (cfg, bblock, cmethod->klass, data, FALSE, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
				} else {
					MonoVTable *vtable = mono_class_vtable (cfg->domain, cmethod->klass);

					CHECK_TYPELOAD (cmethod->klass);
					if (mini_field_access_needs_cctor_run (cfg, method, vtable) && !(g_slist_find (class_inits, vtable))) {
						guint8 *tramp = mono_create_class_init_trampoline (vtable);
						mono_emit_native_call (cfg, bblock, tramp, 
											   helper_sig_class_init_trampoline,
											   NULL, ip, FALSE, FALSE);
						if (cfg->verbose_level > 2)
							g_print ("class %s.%s needs init call for ctor\n", cmethod->klass->name_space, cmethod->klass->name);
						class_inits = g_slist_prepend (class_inits, vtable);
					}
					temp = handle_alloc (cfg, bblock, cmethod->klass, FALSE, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
				}

				/* Avoid virtual calls to ctors if possible */
				if (cmethod->klass->marshalbyref)
					callvirt_this_arg = sp [0];
				
				if ((cfg->opt & MONO_OPT_INLINE) && cmethod && !context_used && !vtable_arg &&
				    mono_method_check_inlining (cfg, cmethod) &&
				    !mono_class_is_subclass_of (cmethod->klass, mono_defaults.exception_class, FALSE) &&
				    !g_list_find (dont_inline, cmethod)) {
					int costs;
					MonoBasicBlock *ebblock;
					if ((costs = inline_method (cfg, cmethod, fsig, bblock, sp, ip, real_offset, dont_inline, &ebblock, FALSE))) {

						ip += 5;
						real_offset += 5;
						
						GET_BBLOCK (cfg, bblock, ip);
						ebblock->next_bb = bblock;
						link_bblock (cfg, ebblock, bblock);

						NEW_TEMPLOAD (cfg, *sp, temp);
						sp++;

						/* indicates start of a new block, and triggers a load 
						   of all stack arguments at bb boundarie */
						bblock = ebblock;

						inline_costs += costs;
						break;
						
					} else {
						/* Prevent inlining of methods which call other methods */
						INLINE_FAILURE;
						mono_emit_method_call_spilled (cfg, bblock, cmethod, fsig, sp, ip, callvirt_this_arg);
					}
				} else if (context_used &&
						(!mono_method_is_generic_sharable_impl (cmethod, TRUE) ||
							!mono_class_generic_sharing_enabled (cmethod->klass))) {
					MonoInst *rgctx, *cmethod_addr;

					g_assert (!callvirt_this_arg);

					GET_RGCTX (rgctx, context_used);
					cmethod_addr = get_runtime_generic_context_method (cfg, method, context_used,
							bblock, cmethod,
							generic_context, rgctx, MONO_RGCTX_INFO_GENERIC_METHOD_CODE, ip);

					mono_emit_rgctx_calli_spilled (cfg, bblock, fsig, sp, cmethod_addr, vtable_arg, ip);
				} else {
					/* Prevent inlining of methods which call other methods */
					INLINE_FAILURE;
					/* now call the actual ctor */
					mono_emit_rgctx_method_call_spilled (cfg, bblock, cmethod, fsig, sp,
						vtable_arg, NULL, ip, callvirt_this_arg);
				}
			}

			NEW_TEMPLOAD (cfg, *sp, temp);
			sp++;
			
			ip += 5;
			inline_costs += 5;
			break;
		}
		case CEE_ISINST:
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			if (cfg->generic_sharing_context)
				context_used = mono_class_check_context_used (klass);

			/* Needed by the code generated in inssel.brg */
			if (!context_used)
				mono_get_got_var (cfg);

			if (context_used) {
				MonoInst *rgctx, *args [2];
				int temp;

				/* obj */
				args [0] = *sp;

				/* klass */
				GET_RGCTX (rgctx, context_used);
				args [1] = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
					generic_context, rgctx, MONO_RGCTX_INFO_KLASS, ip);

				temp = mono_emit_jit_icall (cfg, bblock, mono_object_isinst, args, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);

				sp++;
				ip += 5;
				inline_costs += 2;
			} else if (klass->marshalbyref || klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
			
				MonoMethod *mono_isinst;
				MonoInst *iargs [1];
				MonoBasicBlock *ebblock;
				int costs;
				int temp;
				
				mono_isinst = mono_marshal_get_isinst (klass); 
				iargs [0] = sp [0];
				
				costs = inline_method (cfg, mono_isinst, mono_method_signature (mono_isinst), bblock, 
						       iargs, ip, real_offset, dont_inline, &ebblock, TRUE);
			
				g_assert (costs > 0);
				
				ip += 5;
				real_offset += 5;
			
				GET_BBLOCK (cfg, bblock, ip);
				ebblock->next_bb = bblock;
				link_bblock (cfg, ebblock, bblock);

				temp = iargs [0]->inst_i0->inst_c0;
				NEW_TEMPLOAD (cfg, *sp, temp);
				
 				sp++;
				bblock = ebblock;
				inline_costs += costs;
			} else {
				MONO_INST_NEW (cfg, ins, *ip);
				ins->type = STACK_OBJ;
				ins->inst_left = *sp;
				ins->inst_newa_class = klass;
				ins->klass = klass;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 5);
				ip += 5;
			}
			break;
		case CEE_UNBOX_ANY: {
			MonoInst *iargs [3];
			guint32 align;

			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (cfg->generic_sharing_context)
				context_used = mono_class_check_context_used (klass);

			if (generic_class_is_reference_type (cfg, klass)) {
				switch (emit_castclass (klass, token, context_used, FALSE,
						cfg, method, arg_array, param_types, dont_inline, end, header,
						generic_context, &bblock, &ip, &sp, &inline_costs, &real_offset)) {
				case 0: break;
				case -1: goto unverified;
				case -2: goto exception_exit;
				default: g_assert_not_reached ();
				}
				break;
			}

			if (mono_class_is_nullable (klass)) {
				int v;
				MonoInst *rgctx = NULL;

				if (context_used)
					GET_RGCTX (rgctx, context_used);

				v = handle_unbox_nullable (cfg, method, context_used, bblock, *sp, ip, klass,
					generic_context, rgctx);
				NEW_TEMPLOAD (cfg, *sp, v);
				sp ++;
				ip += 5;
				break;
			}

			switch (emit_unbox (klass, token, context_used,
					cfg, method, arg_array, param_types, dont_inline, end, header,
					generic_context, &bblock, &ip, &sp, &inline_costs, &real_offset)) {
			case 0: break;
			case -1: goto unverified;
			case -2: goto exception_exit;
			default: g_assert_not_reached ();
			}
			ip += 5;
			/* LDOBJ impl */
			n = mono_class_value_size (klass, &align);
			ins = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);
			NEW_TEMPLOADA (cfg, iargs [0], ins->inst_c0);
			if ((cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
				MonoInst *copy;
				NEW_MEMCPY (cfg, copy, iargs [0], *sp, n, align);
				MONO_ADD_INS (bblock, copy);
			} else {
				MonoMethod *memcpy_method = get_memcpy_method ();
				iargs [1] = *sp;
				NEW_ICONST (cfg, iargs [2], n);

				mono_emit_method_call_spilled (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL);
			}
			NEW_TEMPLOAD (cfg, *sp, ins->inst_c0);
			++sp;
			inline_costs += 2;
			break;
		}
		case CEE_UNBOX:
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (cfg->generic_sharing_context)
				context_used = mono_class_check_context_used (klass);

			if (mono_class_is_nullable (klass)) {
				int v;
				MonoInst *rgctx = NULL;

				if (context_used)
					GET_RGCTX (rgctx, context_used);
				v = handle_unbox_nullable (cfg, method, context_used, bblock, *sp, ip, klass,
					generic_context, rgctx);
				NEW_TEMPLOAD (cfg, *sp, v);
				sp ++;
				ip += 5;
				break;
			}

			switch (emit_unbox (klass, token, context_used,
					cfg, method, arg_array, param_types, dont_inline, end, header,
					generic_context, &bblock, &ip, &sp, &inline_costs, &real_offset)) {
			case 0: break;
			case -1: goto unverified;
			case -2: goto exception_exit;
			default: g_assert_not_reached ();
			}

			sp++;
			ip += 5;
			inline_costs += 2;
			break;
		case CEE_CASTCLASS:
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			if (cfg->generic_sharing_context)
				context_used = mono_class_check_context_used (klass);

			switch (emit_castclass (klass, token, context_used, TRUE,
					cfg, method, arg_array, param_types, dont_inline, end, header,
					generic_context, &bblock, &ip, &sp, &inline_costs, &real_offset)) {
			case 0: break;
			case -1: goto unverified;
			case -2: goto exception_exit;
			default: g_assert_not_reached ();
			}
			break;
		case CEE_THROW:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, OP_THROW);
			--sp;
			ins->inst_left = *sp;
			ip++;
			bblock->out_of_line = TRUE;
			MONO_ADD_INS (bblock, ins);
			MONO_INST_NEW (cfg, ins, OP_NOT_REACHED);
			ins->cil_code = ip - 1;
			MONO_ADD_INS (bblock, ins);
			sp = stack_start;
			
			link_bblock (cfg, bblock, end_bblock);
			start_new_bblock = 1;
			break;
		case CEE_LDFLD:
		case CEE_LDFLDA:
		case CEE_STFLD: {
			MonoInst *offset_ins;
			MonoClassField *field;
			MonoBasicBlock *ebblock;
			int costs;
			guint foffset;

			if (*ip == CEE_STFLD) {
				CHECK_STACK (2);
				sp -= 2;
			} else {
				CHECK_STACK (1);
				--sp;
			}
			if (sp [0]->type == STACK_I4 || sp [0]->type == STACK_I8 || sp [0]->type == STACK_R8)
				UNVERIFIED;
			if (*ip != CEE_LDFLD && sp [0]->type == STACK_VTYPE)
				UNVERIFIED;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE) {
				field = mono_method_get_wrapper_data (method, token);
				klass = field->parent;
			} else {
				field = mono_field_from_token (image, token, &klass, generic_context);
			}
			if (!field)
				goto load_error;
			mono_class_init (klass);
			if (!dont_verify && !cfg->skip_visibility && !mono_method_can_access_field (method, field))
				FIELD_ACCESS_FAILURE;

			foffset = klass->valuetype? field->offset - sizeof (MonoObject): field->offset;
			/* FIXME: mark instructions for use in SSA */
			if (*ip == CEE_STFLD) {
				if (target_type_is_incompatible (cfg, field->type, sp [1]))
					UNVERIFIED;
				if ((klass->marshalbyref && !MONO_CHECK_THIS (sp [0])) || klass->contextbound || klass == mono_defaults.marshalbyrefobject_class) {
					MonoMethod *stfld_wrapper = mono_marshal_get_stfld_wrapper (field->type); 
					MonoInst *iargs [5];

					iargs [0] = sp [0];
					NEW_CLASSCONST (cfg, iargs [1], klass);
					NEW_FIELDCONST (cfg, iargs [2], field);
					NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : 
						    field->offset);
					iargs [4] = sp [1];

					if (cfg->opt & MONO_OPT_INLINE) {
						costs = inline_method (cfg, stfld_wrapper, mono_method_signature (stfld_wrapper), bblock, 
								iargs, ip, real_offset, dont_inline, &ebblock, TRUE);
						g_assert (costs > 0);
						      
						ip += 5;
						real_offset += 5;

						GET_BBLOCK (cfg, bblock, ip);
						ebblock->next_bb = bblock;
						link_bblock (cfg, ebblock, bblock);

						/* indicates start of a new block, and triggers a load 
						   of all stack arguments at bb boundarie */
						bblock = ebblock;

						inline_costs += costs;
						break;
					} else {
						mono_emit_method_call_spilled (cfg, bblock, stfld_wrapper, mono_method_signature (stfld_wrapper), iargs, ip, NULL);
					}
#if HAVE_WRITE_BARRIERS
				} else if (mini_type_to_stind (cfg, field->type) == CEE_STIND_REF && !(sp [1]->opcode == OP_PCONST && sp [1]->inst_c0 == 0)) {
					/* insert call to write barrier */
					MonoMethod *write_barrier = mono_marshal_get_write_barrier ();
					MonoInst *iargs [2];
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->inst_left = *sp;
					ins->inst_right = offset_ins;
					ins->type = STACK_MP;
					ins->klass = mono_defaults.object_class;
					iargs [0] = ins;
					iargs [1] = sp [1];
					mono_emit_method_call_spilled (cfg, bblock, write_barrier, mono_method_signature (write_barrier), iargs, ip, NULL);
#endif
#ifdef MONO_ARCH_SOFT_FLOAT
				} else if (mini_type_to_stind (cfg, field->type) == CEE_STIND_R4) {
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->inst_left = *sp;
					ins->inst_right = offset_ins;
					ins->type = STACK_MP;
					ins->klass = mono_defaults.object_class;
					handle_store_float (cfg, bblock, ins, sp [1], ip);
#endif
				} else {
					MonoInst *store;
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->inst_left = *sp;
					ins->inst_right = offset_ins;
					ins->type = STACK_MP;

					MONO_INST_NEW (cfg, store, mini_type_to_stind (cfg, field->type));
					store->inst_left = ins;
					store->inst_right = sp [1];
					handle_loaded_temps (cfg, bblock, stack_start, sp);
					store->flags |= ins_flag;
					ins_flag = 0;
					if (store->opcode == CEE_STOBJ) {
						handle_stobj (cfg, bblock, ins, sp [1], ip, 
							      mono_class_from_mono_type (field->type), FALSE, FALSE, TRUE);
					} else
						MONO_ADD_INS (bblock, store);
				}
			} else {
				if ((klass->marshalbyref && !MONO_CHECK_THIS (sp [0])) || klass->contextbound || klass == mono_defaults.marshalbyrefobject_class) {
					MonoMethod *wrapper = (*ip == CEE_LDFLDA) ? mono_marshal_get_ldflda_wrapper (field->type) : mono_marshal_get_ldfld_wrapper (field->type); 
					MonoInst *iargs [4];
					int temp;
					
					iargs [0] = sp [0];
					NEW_CLASSCONST (cfg, iargs [1], klass);
					NEW_FIELDCONST (cfg, iargs [2], field);
					NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset);
					if ((cfg->opt & MONO_OPT_INLINE) && !MONO_TYPE_ISSTRUCT (mono_method_signature (wrapper)->ret)) {
						costs = inline_method (cfg, wrapper, mono_method_signature (wrapper), bblock, 
								iargs, ip, real_offset, dont_inline, &ebblock, TRUE);
						g_assert (costs > 0);
						      
						ip += 5;
						real_offset += 5;

						GET_BBLOCK (cfg, bblock, ip);
						ebblock->next_bb = bblock;
						link_bblock (cfg, ebblock, bblock);

						temp = iargs [0]->inst_i0->inst_c0;

						NEW_TEMPLOAD (cfg, *sp, temp);
						sp++;

						/* indicates start of a new block, and triggers a load of
						   all stack arguments at bb boundarie */
						bblock = ebblock;
						
						inline_costs += costs;
						break;
					} else {
						temp = mono_emit_method_call_spilled (cfg, bblock, wrapper, mono_method_signature (wrapper), iargs, ip, NULL);
						NEW_TEMPLOAD (cfg, *sp, temp);
						NEW_TEMPLOAD_SOFT_FLOAT (cfg, bblock, *sp, temp, ip);
						sp++;
					}
				} else {
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->inst_left = *sp;
					ins->inst_right = offset_ins;
					ins->type = STACK_MP;

					if (*ip == CEE_LDFLDA) {
						ins->klass = mono_class_from_mono_type (field->type);
						*sp++ = ins;
					} else {
						MonoInst *load;
						MONO_INST_NEW (cfg, load, mini_type_to_ldind (cfg, field->type));
						type_to_eval_stack_type (cfg, field->type, load);
						load->inst_left = ins;
						load->flags |= ins_flag;
						ins_flag = 0;
#ifdef MONO_ARCH_SOFT_FLOAT
						if (mini_type_to_ldind (cfg, field->type) == CEE_LDIND_R4) {
							int temp;
							temp = handle_load_float (cfg, bblock, ins, ip);
							NEW_TEMPLOAD (cfg, *sp, temp);
							sp++;
						} else
#endif
						*sp++ = load;
					}
				}
			}
			ip += 5;
			break;
		}
		case CEE_LDSFLD:
		case CEE_LDSFLDA:
		case CEE_STSFLD: {
			MonoClassField *field;
			gboolean is_special_static;
			gpointer addr = NULL;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE) {
				field = mono_method_get_wrapper_data (method, token);
				klass = field->parent;
			}
			else
				field = mono_field_from_token (image, token, &klass, generic_context);
			if (!field)
				goto load_error;
			mono_class_init (klass);
			if (!dont_verify && !cfg->skip_visibility && !mono_method_can_access_field (method, field))
				FIELD_ACCESS_FAILURE;

			/*
			 * We can only support shared generic static
			 * field access on architectures where the
			 * trampoline code has been extended to handle
			 * the generic class init.
			 */
#ifndef MONO_ARCH_VTABLE_REG
			GENERIC_SHARING_FAILURE (*ip);
#endif

			if (cfg->generic_sharing_context)
				context_used = mono_class_check_context_used (klass);

			g_assert (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL));

			if ((*ip) == CEE_STSFLD)
				handle_loaded_temps (cfg, bblock, stack_start, sp);

			is_special_static = mono_class_field_is_special_static (field);

			if ((cfg->opt & MONO_OPT_SHARED) ||
					(cfg->compile_aot && is_special_static) ||
					(context_used && is_special_static)) {
				int temp;
				MonoInst *iargs [2];

				g_assert (field->parent);
				if ((cfg->opt & MONO_OPT_SHARED) || cfg->compile_aot) {
					MonoInst *domain_var;
					/* avoid depending on undefined C behavior in sequence points */
					domain_var = mono_get_domainvar (cfg);
					NEW_TEMPLOAD (cfg, iargs [0], domain_var->inst_c0);
				} else {
					NEW_DOMAINCONST (cfg, iargs [0]);
				}
				if (context_used) {
					MonoInst *rgctx;

					GET_RGCTX (rgctx, context_used);
					iargs [1] = get_runtime_generic_context_field (cfg, method, context_used,
							bblock, field,
							generic_context, rgctx, MONO_RGCTX_INFO_CLASS_FIELD, ip);
				} else {
					NEW_FIELDCONST (cfg, iargs [1], field);
				}
				temp = mono_emit_jit_icall (cfg, bblock, mono_class_static_field_address, iargs, ip);
				NEW_TEMPLOAD (cfg, ins, temp);
			} else if (context_used) {
				MonoInst *rgctx, *static_data;

				/*
				g_print ("sharing static field access in %s.%s.%s - depth %d offset %d\n",
					method->klass->name_space, method->klass->name, method->name,
					depth, field->offset);
				*/

				if (mono_class_needs_cctor_run (klass, method)) {
					MonoMethodSignature *sig = helper_sig_generic_class_init_trampoline;
					MonoCallInst *call;
					MonoInst *vtable, *rgctx;

					GET_RGCTX (rgctx, context_used);
					vtable = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
							generic_context, rgctx, MONO_RGCTX_INFO_VTABLE, ip);

					call = mono_emit_call_args (cfg, bblock, sig, NULL, FALSE, FALSE, ip, FALSE);
					call->inst.opcode = OP_TRAMPCALL_VTABLE;
					call->fptr = mono_create_generic_class_init_trampoline ();

					call->inst.inst_left = vtable;

					mono_spill_call (cfg, bblock, call, sig, FALSE, ip, FALSE);
				}

				/*
				 * The pointer we're computing here is
				 *
				 *   super_info.static_data + field->offset
				 */
				GET_RGCTX (rgctx, context_used);
				static_data = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
					generic_context, rgctx, MONO_RGCTX_INFO_STATIC_DATA, ip);

				if (field->offset == 0) {
					ins = static_data;
				} else {
					MonoInst *field_offset;

					NEW_ICONST (cfg, field_offset, field->offset);

					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->inst_left = static_data;
					ins->inst_right = field_offset;
					ins->type = STACK_PTR;
					ins->klass = klass;
				}
			} else {
				MonoVTable *vtable;

				vtable = mono_class_vtable (cfg->domain, klass);
				CHECK_TYPELOAD (klass);
				if (!is_special_static) {
					if (mini_field_access_needs_cctor_run (cfg, method, vtable) && !(g_slist_find (class_inits, vtable))) {
						guint8 *tramp = mono_create_class_init_trampoline (vtable);
						mono_emit_native_call (cfg, bblock, tramp, 
											   helper_sig_class_init_trampoline,
											   NULL, ip, FALSE, FALSE);
						if (cfg->verbose_level > 2)
							g_print ("class %s.%s needs init call for %s\n", klass->name_space, klass->name, mono_field_get_name (field));
						class_inits = g_slist_prepend (class_inits, vtable);
					} else {
						if (cfg->run_cctors) {
							MonoException *ex;
							/* This makes so that inline cannot trigger */
							/* .cctors: too many apps depend on them */
							/* running with a specific order... */
							if (! vtable->initialized)
								INLINE_FAILURE;
							ex = mono_runtime_class_init_full (vtable, FALSE);
							if (ex) {
								set_exception_object (cfg, ex);
								goto exception_exit;
							}					
						}
					}
					addr = (char*)vtable->data + field->offset;

					if (cfg->compile_aot)
						NEW_SFLDACONST (cfg, ins, field);
					else
						NEW_PCONST (cfg, ins, addr);
				} else {
					int temp;
					MonoInst *iargs [1];

					/* The special_static_fields
					 * field is init'd in
					 * mono_class_vtable, so it
					 * needs to be called here.
					 */
					if (!(cfg->opt & MONO_OPT_SHARED)) {
						mono_class_vtable (cfg->domain, klass);
						CHECK_TYPELOAD (klass);
					}
					mono_domain_lock (cfg->domain);
					if (cfg->domain->special_static_fields)
						addr = g_hash_table_lookup (cfg->domain->special_static_fields, field);
					mono_domain_unlock (cfg->domain);

					/* 
					 * insert call to mono_threads_get_static_data (GPOINTER_TO_UINT (addr)) 
					 * This could be later optimized to do just a couple of
					 * memory dereferences with constant offsets.
					 */
					NEW_ICONST (cfg, iargs [0], GPOINTER_TO_UINT (addr));
					temp = mono_emit_jit_icall (cfg, bblock, mono_get_special_static_data, iargs, ip);
					NEW_TEMPLOAD (cfg, ins, temp);
				}
			}

			/* FIXME: mark instructions for use in SSA */
			if (*ip == CEE_LDSFLDA) {
				ins->klass = mono_class_from_mono_type (field->type);
				*sp++ = ins;
			} else if (*ip == CEE_STSFLD) {
				MonoInst *store;
				CHECK_STACK (1);
				sp--;
				MONO_INST_NEW (cfg, store, mini_type_to_stind (cfg, field->type));
				store->inst_left = ins;
				store->inst_right = sp [0];
				store->flags |= ins_flag;
				ins_flag = 0;

#ifdef MONO_ARCH_SOFT_FLOAT
				if (store->opcode == CEE_STIND_R4)
					handle_store_float (cfg, bblock, ins, sp [0], ip);
				else
#endif
				if (store->opcode == CEE_STOBJ) {
					handle_stobj (cfg, bblock, ins, sp [0], ip, mono_class_from_mono_type (field->type), FALSE, FALSE, FALSE);
				} else
					MONO_ADD_INS (bblock, store);
			} else {
				gboolean is_const = FALSE;
				MonoVTable *vtable = NULL;

				if (!context_used)
					vtable = mono_class_vtable (cfg->domain, klass);

				CHECK_TYPELOAD (klass);
				if (!context_used && !((cfg->opt & MONO_OPT_SHARED) || cfg->compile_aot) && 
				    vtable->initialized && (field->type->attrs & FIELD_ATTRIBUTE_INIT_ONLY)) {
					gpointer addr = (char*)vtable->data + field->offset;
					int ro_type = field->type->type;
					if (ro_type == MONO_TYPE_VALUETYPE && field->type->data.klass->enumtype) {
						ro_type = field->type->data.klass->enum_basetype->type;
					}
					/* g_print ("RO-FIELD %s.%s:%s\n", klass->name_space, klass->name, mono_field_get_name (field));*/
					is_const = TRUE;
					switch (ro_type) {
					case MONO_TYPE_BOOLEAN:
					case MONO_TYPE_U1:
						NEW_ICONST (cfg, *sp, *((guint8 *)addr));
						sp++;
						break;
					case MONO_TYPE_I1:
						NEW_ICONST (cfg, *sp, *((gint8 *)addr));
						sp++;
						break;						
					case MONO_TYPE_CHAR:
					case MONO_TYPE_U2:
						NEW_ICONST (cfg, *sp, *((guint16 *)addr));
						sp++;
						break;
					case MONO_TYPE_I2:
						NEW_ICONST (cfg, *sp, *((gint16 *)addr));
						sp++;
						break;
						break;
					case MONO_TYPE_I4:
						NEW_ICONST (cfg, *sp, *((gint32 *)addr));
						sp++;
						break;						
					case MONO_TYPE_U4:
						NEW_ICONST (cfg, *sp, *((guint32 *)addr));
						sp++;
						break;
#ifndef HAVE_MOVING_COLLECTOR
					case MONO_TYPE_I:
					case MONO_TYPE_U:
					case MONO_TYPE_STRING:
					case MONO_TYPE_OBJECT:
					case MONO_TYPE_CLASS:
					case MONO_TYPE_SZARRAY:
					case MONO_TYPE_PTR:
					case MONO_TYPE_FNPTR:
					case MONO_TYPE_ARRAY:
						NEW_PCONST (cfg, *sp, *((gpointer *)addr));
						type_to_eval_stack_type (cfg, field->type, *sp);
						sp++;
						break;
#endif
					case MONO_TYPE_I8:
					case MONO_TYPE_U8:
						MONO_INST_NEW (cfg, *sp, OP_I8CONST);
						sp [0]->type = STACK_I8;
						sp [0]->inst_l = *((gint64 *)addr);
						sp++;
						break;
					case MONO_TYPE_R4:
					case MONO_TYPE_R8:
					case MONO_TYPE_VALUETYPE:
					default:
						is_const = FALSE;
						break;
					}
				}

				if (!is_const) {
					MonoInst *load;
					CHECK_STACK_OVF (1);
					MONO_INST_NEW (cfg, load, mini_type_to_ldind (cfg, field->type));
					type_to_eval_stack_type (cfg, field->type, load);
					load->inst_left = ins;
					load->flags |= ins_flag;
#ifdef MONO_ARCH_SOFT_FLOAT
					if (load->opcode == CEE_LDIND_R4) {
						int temp;
						temp = handle_load_float (cfg, bblock, ins, ip);
						NEW_TEMPLOAD (cfg, load, temp);
					}
#endif
					*sp++ = load;
					ins_flag = 0;
				}
			}
			ip += 5;
			break;
		}
		case CEE_STOBJ:
			CHECK_STACK (2);
			sp -= 2;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			n = mini_type_to_stind (cfg, &klass->byval_arg);
			/* FIXME: handle CEE_STIND_R4 */
			if (n == CEE_STOBJ) {
				handle_stobj (cfg, bblock, sp [0], sp [1], ip, klass, FALSE, FALSE, TRUE);
			} else {
				/* FIXME: should check item at sp [1] is compatible with the type of the store. */
				MonoInst *store;
				MONO_INST_NEW (cfg, store, n);
				store->inst_left = sp [0];
				store->inst_right = sp [1];
				store->flags |= ins_flag;
				MONO_ADD_INS (bblock, store);
			}
			ins_flag = 0;
			ip += 5;
			inline_costs += 1;
			break;
		case CEE_BOX: {
			MonoInst *val;

			CHECK_STACK (1);
			--sp;
			val = *sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (cfg->generic_sharing_context)
				context_used =  mono_class_check_context_used (klass);

			if (generic_class_is_reference_type (cfg, klass)) {
				*sp++ = val;
				ip += 5;
				break;
			}
			if (klass == mono_defaults.void_class)
				UNVERIFIED;
			if (target_type_is_incompatible (cfg, &klass->byval_arg, *sp))
				UNVERIFIED;
			/* frequent check in generic code: box (struct), brtrue */
			if (!mono_class_is_nullable (klass) &&
			    ip + 5 < end && ip_in_bb (cfg, bblock, ip + 5) && (ip [5] == CEE_BRTRUE || ip [5] == CEE_BRTRUE_S)) {
				/*g_print ("box-brtrue opt at 0x%04x in %s\n", real_offset, method->name);*/
				MONO_INST_NEW (cfg, ins, CEE_POP);
				MONO_ADD_INS (bblock, ins);
				ins->inst_i0 = *sp;
				ip += 5;
				cfg->ip = ip;
				MONO_INST_NEW (cfg, ins, OP_BR);
				MONO_ADD_INS (bblock, ins);
				if (*ip == CEE_BRTRUE_S) {
					CHECK_OPSIZE (2);
					ip++;
					target = ip + 1 + (signed char)(*ip);
					ip++;
				} else {
					CHECK_OPSIZE (5);
					ip++;
					target = ip + 4 + (gint)(read32 (ip));
					ip += 4;
				}
				GET_BBLOCK (cfg, tblock, target);
				link_bblock (cfg, bblock, tblock);
				CHECK_BBLOCK (target, ip, tblock);
				ins->inst_target_bb = tblock;
				GET_BBLOCK (cfg, tblock, ip);
				link_bblock (cfg, bblock, tblock);
				if (sp != stack_start) {
					handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
					sp = stack_start;
					CHECK_UNVERIFIABLE (cfg);
				}
				start_new_bblock = 1;
				break;
			}
			if (context_used) {
				MonoInst *rgctx;

				if (mono_class_is_nullable (klass)) {
					GET_RGCTX (rgctx, context_used);
					*sp++ = handle_box_nullable_from_inst (cfg, method, context_used, bblock, val,
							ip, klass, generic_context, rgctx);
				} else {
					MonoInst *data;
					int rgctx_info;

					GET_RGCTX (rgctx, context_used);
					if (cfg->opt & MONO_OPT_SHARED)
						rgctx_info = MONO_RGCTX_INFO_KLASS;
					else
						rgctx_info = MONO_RGCTX_INFO_VTABLE;
					data = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
							generic_context, rgctx, rgctx_info, ip);

					*sp++ = handle_box_from_inst (cfg, bblock, val, ip, klass, data);
  				}
			} else {
				*sp++ = handle_box (cfg, bblock, val, ip, klass);
			}
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_NEWARR:
			CHECK_STACK (1);
			--sp;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);

			/* allocate the domainvar - becaus this is used in decompose_foreach */
			if (cfg->opt & MONO_OPT_SHARED) {
				mono_get_domainvar (cfg);
				/* LAME-IR: Mark it as used since otherwise it will be optimized away */
				cfg->domainvar->flags |= MONO_INST_VOLATILE;
			}

			/* Ditto */
			mono_get_got_var (cfg);

			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);

			if (cfg->generic_sharing_context)
				context_used = mono_class_check_context_used (klass);

			if (context_used) {
				MonoInst *rgctx, *args [3];
				int temp;

				/* domain */
				/* FIXME: what about domain-neutral code? */
				NEW_DOMAINCONST (cfg, args [0]);

				/* klass */
				GET_RGCTX (rgctx, context_used);
				args [1] = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
					generic_context, rgctx, MONO_RGCTX_INFO_KLASS, ip);

				/* array len */
				args [2] = *sp;

				temp = mono_emit_jit_icall (cfg, bblock, mono_array_new, args, ip);
				NEW_TEMPLOAD (cfg, ins, temp);
			} else {
				MONO_INST_NEW (cfg, ins, *ip);
				ins->inst_newa_class = klass;
				ins->inst_newa_len = *sp;
				ins->type = STACK_OBJ;
				ins->klass = mono_array_class_get (klass, 1);
			}

			ip += 5;
			*sp++ = ins;
			/* 
			 * we store the object so calls to create the array are not interleaved
			 * with the arguments of other calls.
			 */
			if (1) {
				MonoInst *store, *temp, *load;
				const char *data_ptr;
				int data_size = 0;
				guint32 field_token;

				--sp;
				temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
				NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
				store->cil_code = ins->cil_code;
				MONO_ADD_INS (bblock, store);
				/* 
				 * we inline/optimize the initialization sequence if possible.
				 * we should also allocate the array as not cleared, since we spend as much time clearing to 0 as initializing
				 * for small sizes open code the memcpy
				 * ensure the rva field is big enough
				 */
				if ((cfg->opt & MONO_OPT_INTRINS) && ip + 6 < end && ip_in_bb (cfg, bblock, ip + 6) && (data_ptr = initialize_array_data (method, cfg->compile_aot, ip, ins, &data_size, &field_token))) {
					MonoMethod *memcpy_method = get_memcpy_method ();
					MonoInst *data_offset, *add;
					MonoInst *iargs [3];
					NEW_ICONST (cfg, iargs [2], data_size);
					NEW_TEMPLOAD (cfg, load, temp->inst_c0);
					load->cil_code = ins->cil_code;
					NEW_ICONST (cfg, data_offset, G_STRUCT_OFFSET (MonoArray, vector));
					MONO_INST_NEW (cfg, add, OP_PADD);
					add->inst_left = load;
					add->inst_right = data_offset;
					iargs [0] = add;
					if (cfg->compile_aot) {
						NEW_AOTCONST_TOKEN (cfg, iargs [1], MONO_PATCH_INFO_RVA, method->klass->image, GPOINTER_TO_UINT(field_token), STACK_PTR, NULL);
					} else {
						NEW_PCONST (cfg, iargs [1], (char*)data_ptr);
					}
					mono_emit_method_call_spilled (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL);
					ip += 11;
				}
				NEW_TEMPLOAD (cfg, load, temp->inst_c0);
				load->cil_code = ins->cil_code;
				*sp++ = load;
			}
			inline_costs += 1;
			break;
		case CEE_LDLEN:
			CHECK_STACK (1);
			--sp;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
			MONO_INST_NEW (cfg, ins, *ip);
			ip++;
			ins->inst_left = *sp;
			ins->type = STACK_PTR;
			*sp++ = ins;
			break;
		case CEE_LDELEMA:
			CHECK_STACK (2);
			sp -= 2;
			CHECK_OPSIZE (5);
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;

			klass = mini_get_class (method, read32 (ip + 1), generic_context);
			CHECK_TYPELOAD (klass);
			/* we need to make sure that this array is exactly the type it needs
			 * to be for correctness. the wrappers are lax with their usage
			 * so we need to ignore them here
			 */
			if (!klass->valuetype && method->wrapper_type == MONO_WRAPPER_NONE && !readonly) {
				MonoInst* check;

				/* Needed by the code generated in inssel.brg */
				mono_get_got_var (cfg);

				MONO_INST_NEW (cfg, check, OP_CHECK_ARRAY_TYPE);
				check->klass = mono_array_class_get (klass, 1);
				check->inst_left = sp [0];
				check->type = STACK_OBJ;
				sp [0] = check;
			}
			
			readonly = FALSE;
			mono_class_init (klass);
			NEW_LDELEMA (cfg, ins, sp, klass);
			*sp++ = ins;
			ip += 5;
			break;
		case CEE_LDELEM_ANY: {
			MonoInst *load;
			CHECK_STACK (2);
			sp -= 2;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			mono_class_init (klass);
			NEW_LDELEMA (cfg, load, sp, klass);
			MONO_INST_NEW (cfg, ins, mini_type_to_ldind (cfg, &klass->byval_arg));
			ins->inst_left = load;
			*sp++ = ins;
			type_to_eval_stack_type (cfg, &klass->byval_arg, ins);
			ip += 5;
			break;
		}
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
		case CEE_LDELEM_REF: {
			MonoInst *load;
			/*
			 * translate to:
			 * ldind.x (ldelema (array, index))
			 * ldelema does the bounds check
			 */
			CHECK_STACK (2);
			sp -= 2;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
			klass = array_access_to_klass (*ip, sp [0]);
			NEW_LDELEMA (cfg, load, sp, klass);
#ifdef MONO_ARCH_SOFT_FLOAT
			if (*ip == CEE_LDELEM_R4) {
				int temp;
				temp = handle_load_float (cfg, bblock, load, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);
				sp++;
				++ip;
				break;
			}
#endif
			MONO_INST_NEW (cfg, ins, ldelem_to_ldind [*ip - CEE_LDELEM_I1]);
			ins->inst_left = load;
			*sp++ = ins;
			ins->type = ldind_type [ins->opcode - CEE_LDIND_I1];
			ins->klass = klass;
			++ip;
			break;
		}
		case CEE_STELEM_I:
		case CEE_STELEM_I1:
		case CEE_STELEM_I2:
		case CEE_STELEM_I4:
		case CEE_STELEM_I8:
		case CEE_STELEM_R4:
		case CEE_STELEM_R8: {
			MonoInst *load;
			/*
			 * translate to:
			 * stind.x (ldelema (array, index), val)
			 * ldelema does the bounds check
			 */
			CHECK_STACK (3);
			sp -= 3;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
			klass = array_access_to_klass (*ip, sp [0]);
			NEW_LDELEMA (cfg, load, sp, klass);
#ifdef MONO_ARCH_SOFT_FLOAT
			if (*ip == CEE_STELEM_R4) {
				handle_store_float (cfg, bblock, load, sp [2], ip);
				ip++;
				break;
			}
#endif
			MONO_INST_NEW (cfg, ins, stelem_to_stind [*ip - CEE_STELEM_I]);
			ins->inst_left = load;
			ins->inst_right = sp [2];
			++ip;
			handle_loaded_temps (cfg, bblock, stack_start, sp);
			MONO_ADD_INS (bblock, ins);
			inline_costs += 1;
			break;
		}
		case CEE_STELEM_ANY: {
			MonoInst *load;
			/*
			 * translate to:
			 * stind.x (ldelema (array, index), val)
			 * ldelema does the bounds check
			 */
			CHECK_STACK (3);
			sp -= 3;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mini_get_class (method, token, generic_context);
			CHECK_TYPELOAD (klass);
			mono_class_init (klass);
			if (generic_class_is_reference_type (cfg, klass)) {
				/* storing a NULL doesn't need any of the complex checks in stelemref */
				if (sp [2]->opcode == OP_PCONST && sp [2]->inst_p0 == NULL) {
					MonoInst *load;
					NEW_LDELEMA (cfg, load, sp, mono_defaults.object_class);
					MONO_INST_NEW (cfg, ins, CEE_STIND_REF);
					ins->inst_left = load;
					ins->inst_right = sp [2];
					MONO_ADD_INS (bblock, ins);
				} else {
					MonoMethod* helper = mono_marshal_get_stelemref ();
					MonoInst *iargs [3];
					handle_loaded_temps (cfg, bblock, stack_start, sp);

					iargs [2] = sp [2];
					iargs [1] = sp [1];
					iargs [0] = sp [0];

					mono_emit_method_call_spilled (cfg, bblock, helper, mono_method_signature (helper), iargs, ip, NULL);
				}
			} else {
				NEW_LDELEMA (cfg, load, sp, klass);

				n = mini_type_to_stind (cfg, &klass->byval_arg);
				/* FIXME: CEE_STIND_R4 */
				if (n == CEE_STOBJ)
					handle_stobj (cfg, bblock, load, sp [2], ip, klass, FALSE, FALSE, TRUE);
				else {
					MONO_INST_NEW (cfg, ins, n);
					ins->inst_left = load;
					ins->inst_right = sp [2];
					handle_loaded_temps (cfg, bblock, stack_start, sp);
					MONO_ADD_INS (bblock, ins);
				}
			}
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_STELEM_REF: {
			MonoInst *iargs [3];
			MonoMethod* helper = mono_marshal_get_stelemref ();

			CHECK_STACK (3);
			sp -= 3;
			if (sp [0]->type != STACK_OBJ)
				UNVERIFIED;
			if (sp [2]->type != STACK_OBJ)
				UNVERIFIED;

			handle_loaded_temps (cfg, bblock, stack_start, sp);

			/* storing a NULL doesn't need any of the complex checks in stelemref */
			if (sp [2]->opcode == OP_PCONST && sp [2]->inst_p0 == NULL) {
				MonoInst *load;
				NEW_LDELEMA (cfg, load, sp, mono_defaults.object_class);
				MONO_INST_NEW (cfg, ins, stelem_to_stind [*ip - CEE_STELEM_I]);
				ins->inst_left = load;
				ins->inst_right = sp [2];
				MONO_ADD_INS (bblock, ins);
			} else {
				iargs [2] = sp [2];
				iargs [1] = sp [1];
				iargs [0] = sp [0];
			
				mono_emit_method_call_spilled (cfg, bblock, helper, mono_method_signature (helper), iargs, ip, NULL);
				inline_costs += 1;
			}

			++ip;
			break;
		}
		case CEE_CKFINITE: {
			MonoInst *store, *temp;
			CHECK_STACK (1);

			/* this instr. can throw exceptions as side effect,
			 * so we cant eliminate dead code which contains CKFINITE opdodes.
			 * Spilling to memory makes sure that we always perform
			 * this check */

			
			MONO_INST_NEW (cfg, ins, OP_CKFINITE);
			ins->inst_left = sp [-1];
			temp = mono_compile_create_var (cfg, &mono_defaults.double_class->byval_arg, OP_LOCAL);

			NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
			MONO_ADD_INS (bblock, store);

			NEW_TEMPLOAD (cfg, sp [-1], temp->inst_c0);
		       
			++ip;
			break;
		}
		case CEE_REFANYVAL:
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mono_class_get_full (image, token, generic_context);
			CHECK_TYPELOAD (klass);
			mono_class_init (klass);

			/* Needed by the code generated in inssel.brg */
			mono_get_got_var (cfg);

			if (cfg->generic_sharing_context) {
				context_used = mono_class_check_context_used (klass);
				if (context_used && cfg->compile_aot)
					GENERIC_SHARING_FAILURE (*ip);
			}

			if (context_used) {
				MonoInst *rgctx;

				MONO_INST_NEW (cfg, ins, OP_REFANYVAL_REG);
				ins->type = STACK_MP;
				ins->inst_left = *sp;
				ins->klass = klass;

				GET_RGCTX (rgctx, context_used);
				ins->inst_right = get_runtime_generic_context_ptr (cfg, method, context_used,
						bblock, klass, generic_context, rgctx, MONO_RGCTX_INFO_KLASS, ip);
			} else {
				MONO_INST_NEW (cfg, ins, *ip);
				ins->type = STACK_MP;
				ins->inst_left = *sp;
				ins->klass = klass;
				ins->inst_newa_class = klass;
			}
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_MKREFANY: {
			MonoInst *loc;

			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mono_class_get_full (image, token, generic_context);
			CHECK_TYPELOAD (klass);
			mono_class_init (klass);

			if (cfg->generic_sharing_context) {
				context_used = mono_class_check_context_used (klass);
				if (context_used && cfg->compile_aot)
					GENERIC_SHARING_FAILURE (CEE_MKREFANY);
			}

			loc = mono_compile_create_var (cfg, &mono_defaults.typed_reference_class->byval_arg, OP_LOCAL);
			if (context_used) {
				MonoInst *rgctx, *klass_type, *klass_klass, *loc_load;

				GET_RGCTX (rgctx, context_used);
				klass_klass = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
						generic_context, rgctx, MONO_RGCTX_INFO_KLASS, ip);
				GET_RGCTX (rgctx, context_used);
				klass_type = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, klass,
						generic_context, rgctx, MONO_RGCTX_INFO_TYPE, ip);

				NEW_TEMPLOADA (cfg, loc_load, loc->inst_c0);

				MONO_INST_NEW (cfg, ins, OP_MKREFANY_REGS);
				NEW_GROUP (cfg, ins->inst_left, klass_type, klass_klass);
				NEW_GROUP (cfg, ins->inst_right, *sp, loc_load);
			} else {
				MonoInst *klassconst;

				NEW_PCONST (cfg, klassconst, klass);

				MONO_INST_NEW (cfg, ins, *ip);
				NEW_TEMPLOADA (cfg, ins->inst_right, loc->inst_c0);
				NEW_GROUP (cfg, ins->inst_left, *sp, klassconst);
			}

			MONO_ADD_INS (bblock, ins);

			NEW_TEMPLOAD (cfg, *sp, loc->inst_c0);
			++sp;
			ip += 5;
			break;
		}
		case CEE_LDTOKEN: {
			gpointer handle;
			MonoClass *handle_class;

			CHECK_STACK_OVF (1);

			CHECK_OPSIZE (5);
			n = read32 (ip + 1);

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD ||
					method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED) {
				handle = mono_method_get_wrapper_data (method, n);
				handle_class = mono_method_get_wrapper_data (method, n + 1);
				if (handle_class == mono_defaults.typehandle_class)
					handle = &((MonoClass*)handle)->byval_arg;
			}
			else {
				handle = mono_ldtoken (image, n, &handle_class, generic_context);
			}
			if (!handle)
				goto load_error;
			mono_class_init (handle_class);

			if (cfg->generic_sharing_context) {
				if (handle_class == mono_defaults.typehandle_class) {
					/* If we get a MONO_TYPE_CLASS
					   then we need to provide the
					   open type, not an
					   instantiation of it. */
					if (mono_type_get_type (handle) == MONO_TYPE_CLASS)
						context_used = 0;
					else
						context_used = mono_class_check_context_used (mono_class_from_mono_type (handle));
				} else if (handle_class == mono_defaults.fieldhandle_class)
					context_used = mono_class_check_context_used (((MonoClassField*)handle)->parent);
				else if (handle_class == mono_defaults.methodhandle_class)
					context_used = mono_method_check_context_used (handle);
				else
					g_assert_not_reached ();
			}

			if ((cfg->opt & MONO_OPT_SHARED) &&
					method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD &&
					method->wrapper_type != MONO_WRAPPER_SYNCHRONIZED) {
				int temp;
				MonoInst *res, *store, *addr, *vtvar, *iargs [3];
				int method_context_used;

				if (cfg->generic_sharing_context)
					method_context_used = mono_method_check_context_used (method);
				else
					method_context_used = 0;

				vtvar = mono_compile_create_var (cfg, &handle_class->byval_arg, OP_LOCAL); 

				NEW_IMAGECONST (cfg, iargs [0], image);
				NEW_ICONST (cfg, iargs [1], n);
				if (method_context_used) {
					MonoInst *rgctx;

					GET_RGCTX (rgctx, method_context_used);
					iargs [2] = get_runtime_generic_context_method (cfg, method, method_context_used,
							bblock, method,
							generic_context, rgctx, MONO_RGCTX_INFO_METHOD, ip);
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldtoken_wrapper_generic_shared,
							iargs, ip);
				} else {
					NEW_PCONST (cfg, iargs [2], generic_context);
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldtoken_wrapper, iargs, ip);
				}
				NEW_TEMPLOAD (cfg, res, temp);
				NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
				NEW_INDSTORE (cfg, store, addr, res, &mono_defaults.int_class->byval_arg);
				MONO_ADD_INS (bblock, store);
				NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
			} else {
				if ((ip + 10 < end) && ip_in_bb (cfg, bblock, ip + 5) &&
					handle_class == mono_defaults.typehandle_class &&
					((ip [5] == CEE_CALL) || (ip [5] == CEE_CALLVIRT)) && 
					(cmethod = mini_get_method (cfg, method, read32 (ip + 6), NULL, generic_context)) &&
					(cmethod->klass == mono_defaults.monotype_class->parent) &&
					(strcmp (cmethod->name, "GetTypeFromHandle") == 0)) {
					MonoClass *tclass = mono_class_from_mono_type (handle);
					mono_class_init (tclass);
					if (context_used) {
						MonoInst *rgctx;

						g_assert (!cfg->compile_aot);

						GET_RGCTX (rgctx, context_used);
						ins = get_runtime_generic_context_ptr (cfg, method, context_used, bblock, tclass,
							generic_context, rgctx, MONO_RGCTX_INFO_REFLECTION_TYPE, ip);
					} else if (cfg->compile_aot) {
						/*
						 * FIXME: We would have to include the context into the
						 * aot constant too (tests/generic-array-type.2.exe).
						 */
						if (generic_context)
							cfg->disable_aot = TRUE;
						NEW_TYPE_FROM_HANDLE_CONST (cfg, ins, image, n);
					} else {
						NEW_PCONST (cfg, ins, mono_type_get_object (cfg->domain, handle));
					}
					ins->type = STACK_OBJ;
					ins->klass = cmethod->klass;
					ip += 5;
				} else {
					MonoInst *store, *addr, *vtvar;

					if (context_used) {
							MonoInst *rgctx;

						g_assert (!cfg->compile_aot);

						GET_RGCTX (rgctx, context_used);
						if (handle_class == mono_defaults.typehandle_class) {
							ins = get_runtime_generic_context_ptr (cfg, method,
									context_used, bblock,
									mono_class_from_mono_type (handle), generic_context,
									rgctx, MONO_RGCTX_INFO_TYPE, ip);
						} else if (handle_class == mono_defaults.methodhandle_class) {
							ins = get_runtime_generic_context_method (cfg, method,
									context_used, bblock, handle, generic_context,
									rgctx, MONO_RGCTX_INFO_METHOD, ip);
						} else if (handle_class == mono_defaults.fieldhandle_class) {
							ins = get_runtime_generic_context_field (cfg, method,
									context_used, bblock, handle, generic_context,
									rgctx, MONO_RGCTX_INFO_CLASS_FIELD, ip);
						} else {
							g_assert_not_reached ();
						}
					}
					else if (cfg->compile_aot) {
						NEW_LDTOKENCONST (cfg, ins, image, n);
					} else {
						NEW_PCONST (cfg, ins, handle);
					}
					vtvar = mono_compile_create_var (cfg, &handle_class->byval_arg, OP_LOCAL);
					NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
					NEW_INDSTORE (cfg, store, addr, ins, &mono_defaults.int_class->byval_arg);
					MONO_ADD_INS (bblock, store);
					NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
				}
			}

			*sp++ = ins;
			ip += 5;
			break;
		}
		case CEE_CONV_U2:
		case CEE_CONV_U1:
		case CEE_CONV_I:
			CHECK_STACK (1);
			ADD_UNOP (*ip);
			ip++;
			break;
		case CEE_ADD_OVF:
		case CEE_ADD_OVF_UN:
		case CEE_MUL_OVF:
		case CEE_MUL_OVF_UN:
		case CEE_SUB_OVF:
		case CEE_SUB_OVF_UN:
			CHECK_STACK (2);
			ADD_BINOP (*ip);
			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
			}
			ip++;
			break;
		case CEE_ENDFINALLY:
			MONO_INST_NEW (cfg, ins, OP_ENDFINALLY);
			MONO_ADD_INS (bblock, ins);
			ip++;
			start_new_bblock = 1;

			/*
			 * Control will leave the method so empty the stack, otherwise
			 * the next basic block will start with a nonempty stack.
			 */
			while (sp != stack_start) {
				MONO_INST_NEW (cfg, ins, CEE_POP);
				sp--;
				ins->inst_i0 = *sp;
				MONO_ADD_INS (bblock, ins);
			}
			break;
		case CEE_LEAVE:
		case CEE_LEAVE_S: {
			GList *handlers;

			if (*ip == CEE_LEAVE) {
				CHECK_OPSIZE (5);
				target = ip + 5 + (gint32)read32(ip + 1);
			} else {
				CHECK_OPSIZE (2);
				target = ip + 2 + (signed char)(ip [1]);
			}

			/* empty the stack */
			while (sp != stack_start) {
				MONO_INST_NEW (cfg, ins, CEE_POP);
				sp--;
				ins->inst_i0 = *sp;
				MONO_ADD_INS (bblock, ins);
			}

			/* 
			 * If this leave statement is in a catch block, check for a
			 * pending exception, and rethrow it if necessary.
			 */
			for (i = 0; i < header->num_clauses; ++i) {
				MonoExceptionClause *clause = &header->clauses [i];

				/* 
				 * Use <= in the final comparison to handle clauses with multiple
				 * leave statements, like in bug #78024.
				 * The ordering of the exception clauses guarantees that we find the
				 * innermost clause.
				 */
				if (MONO_OFFSET_IN_HANDLER (clause, ip - header->code) && (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) && (ip - header->code + ((*ip == CEE_LEAVE) ? 5 : 2)) <= (clause->handler_offset + clause->handler_len)) {
					int temp;
					MonoInst *load;

					NEW_TEMPLOAD (cfg, load, mono_find_exvar_for_offset (cfg, clause->handler_offset)->inst_c0);

					temp = mono_emit_jit_icall (cfg, bblock, mono_thread_get_undeniable_exception, NULL, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
				
					MONO_INST_NEW (cfg, ins, OP_THROW_OR_NULL);
					ins->inst_left = *sp;
					ins->inst_right = load;
					MONO_ADD_INS (bblock, ins);
				}
			}

			if ((handlers = mono_find_final_block (cfg, ip, target, MONO_EXCEPTION_CLAUSE_FINALLY))) {
				GList *tmp;
				for (tmp = handlers; tmp; tmp = tmp->next) {
					tblock = tmp->data;
					link_bblock (cfg, bblock, tblock);
					MONO_INST_NEW (cfg, ins, OP_CALL_HANDLER);
					ins->inst_target_bb = tblock;
					MONO_ADD_INS (bblock, ins);
				}
				g_list_free (handlers);
			} 

			MONO_INST_NEW (cfg, ins, OP_BR);
			MONO_ADD_INS (bblock, ins);
			GET_BBLOCK (cfg, tblock, target);
			link_bblock (cfg, bblock, tblock);
			CHECK_BBLOCK (target, ip, tblock);
			ins->inst_target_bb = tblock;
			start_new_bblock = 1;

			if (*ip == CEE_LEAVE)
				ip += 5;
			else
				ip += 2;

			break;
		}
		case CEE_STIND_I:
			CHECK_STACK (2);
			MONO_INST_NEW (cfg, ins, *ip);
			sp -= 2;
			handle_loaded_temps (cfg, bblock, stack_start, sp);
			MONO_ADD_INS (bblock, ins);
			ip++;
			ins->inst_i0 = sp [0];
			ins->inst_i1 = sp [1];
			inline_costs += 1;
			break;
		case CEE_CONV_U:
			CHECK_STACK (1);
			ADD_UNOP (*ip);
			ip++;
			break;
		/* trampoline mono specific opcodes */
		case MONO_CUSTOM_PREFIX: {

			g_assert (method->wrapper_type != MONO_WRAPPER_NONE);

			CHECK_OPSIZE (2);
			switch (ip [1]) {

			case CEE_MONO_ICALL: {
				int temp;
				gpointer func;
				MonoJitICallInfo *info;

				token = read32 (ip + 2);
				func = mono_method_get_wrapper_data (method, token);
				info = mono_find_jit_icall_by_addr (func);
				if (info == NULL){
					g_error ("An attempt has been made to perform an icall to address %p, "
						 "but the address has not been registered as an icall\n", info);
					g_assert_not_reached ();
				}

				CHECK_STACK (info->sig->param_count);
				sp -= info->sig->param_count;

				temp = mono_emit_jit_icall (cfg, bblock, info->func, sp, ip);
				if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
					NEW_TEMPLOAD (cfg, *sp, temp);
					sp++;
				}

				ip += 6;
				inline_costs += 10 * num_calls++;

				break;
			}
			case CEE_MONO_LDPTR: {
				gpointer ptr;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);

				ptr = mono_method_get_wrapper_data (method, token);
				if (cfg->compile_aot && (cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE || cfg->method->wrapper_type == MONO_WRAPPER_RUNTIME_INVOKE)) {
					MonoMethod *wrapped = mono_marshal_method_from_wrapper (cfg->method);

					if (wrapped && ptr != NULL && mono_lookup_internal_call (wrapped) == ptr) {
						NEW_AOTCONST (cfg, ins, MONO_PATCH_INFO_ICALL_ADDR, wrapped);
						*sp++ = ins;
						ip += 6;
						break;
					}

					if ((method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && (strstr (method->name, "__icall_wrapper_") == method->name)) {
						MonoJitICallInfo *callinfo;
						const char *icall_name;

						icall_name = method->name + strlen ("__icall_wrapper_");
						g_assert (icall_name);
						callinfo = mono_find_jit_icall_by_name (icall_name);
						g_assert (callinfo);

						if (ptr == callinfo->func) {
							/* Will be transformed into an AOTCONST later */
							NEW_PCONST (cfg, ins, ptr);
							*sp++ = ins;
							ip += 6;
							break;
						}
					}
				}
				/* FIXME: Generalize this */
				if (cfg->compile_aot && ptr == mono_thread_interruption_request_flag ()) {
					NEW_AOTCONST (cfg, ins, MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG, NULL);
					*sp++ = ins;
					ip += 6;
					break;
				}
				NEW_PCONST (cfg, ins, ptr);
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				/* Can't embed random pointers into AOT code */
				cfg->disable_aot = 1;
				break;
			}
			case CEE_MONO_ICALL_ADDR: {
				MonoMethod *cmethod;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);

				cmethod = mono_method_get_wrapper_data (method, token);

				g_assert (cfg->compile_aot);

				NEW_AOTCONST (cfg, ins, MONO_PATCH_INFO_ICALL_ADDR, cmethod);
				*sp++ = ins;
				ip += 6;
				break;
			}
			case CEE_MONO_VTADDR:
				CHECK_STACK (1);
				--sp;
				MONO_INST_NEW (cfg, ins, OP_VTADDR);
				ins->type = STACK_MP;
				ins->inst_left = *sp;
				*sp++ = ins;
				ip += 2;
				break;
			case CEE_MONO_NEWOBJ: {
				MonoInst *iargs [2];
				int temp;
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				mono_class_init (klass);
				NEW_DOMAINCONST (cfg, iargs [0]);
				NEW_CLASSCONST (cfg, iargs [1], klass);
				temp = mono_emit_jit_icall (cfg, bblock, mono_object_new, iargs, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);
				sp++;
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_MONO_OBJADDR:
				CHECK_STACK (1);
				--sp;
				MONO_INST_NEW (cfg, ins, OP_OBJADDR);
				ins->type = STACK_MP;
				ins->inst_left = *sp;
				*sp++ = ins;
				ip += 2;
				break;
			case CEE_MONO_LDNATIVEOBJ:
				CHECK_STACK (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				klass = mono_method_get_wrapper_data (method, token);
				g_assert (klass->valuetype);
				mono_class_init (klass);
				NEW_INDLOAD (cfg, ins, sp [-1], &klass->byval_arg);
				sp [-1] = ins;
				ip += 6;
				break;
			case CEE_MONO_RETOBJ:
				g_assert (cfg->ret);
				g_assert (mono_method_signature (method)->pinvoke); 
				CHECK_STACK (1);
				--sp;
				
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);    
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);

				NEW_RETLOADA (cfg, ins);
				handle_stobj (cfg, bblock, ins, *sp, ip, klass, FALSE, TRUE, FALSE);
				
				if (sp != stack_start)
					UNVERIFIED;
				
				MONO_INST_NEW (cfg, ins, OP_BR);
				ins->inst_target_bb = end_bblock;
				MONO_ADD_INS (bblock, ins);
				link_bblock (cfg, bblock, end_bblock);
				start_new_bblock = 1;
				ip += 6;
				break;
			case CEE_MONO_CISINST:
			case CEE_MONO_CCASTCLASS: {
				int token;
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				/* Needed by the code generated in inssel.brg */
				mono_get_got_var (cfg);

#ifdef __i386__
				/* 
				 * The code generated for CCASTCLASS has too much register pressure
				 * (obj+vtable+ibitmap_byte_reg+iid_reg), leading to the usual
				 * branches-inside-bblocks problem.
				 */
				cfg->disable_aot = TRUE;
#endif
		
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				MONO_INST_NEW (cfg, ins, (ip [1] == CEE_MONO_CISINST) ? OP_CISINST : OP_CCASTCLASS);
				ins->type = STACK_I4;
				ins->inst_left = *sp;
				ins->inst_newa_class = klass;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 6);
				ip += 6;
				break;
			}
			case CEE_MONO_SAVE_LMF:
			case CEE_MONO_RESTORE_LMF:
#ifdef MONO_ARCH_HAVE_LMF_OPS
				MONO_INST_NEW (cfg, ins, (ip [1] == CEE_MONO_SAVE_LMF) ? OP_SAVE_LMF : OP_RESTORE_LMF);
				MONO_ADD_INS (bblock, ins);
				cfg->need_lmf_area = TRUE;
#endif
				ip += 2;
				break;
			case CEE_MONO_CLASSCONST:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				NEW_CLASSCONST (cfg, ins, mono_method_get_wrapper_data (method, token));
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			case CEE_MONO_NOT_TAKEN:
				bblock->out_of_line = TRUE;
				ip += 2;
				break;
			case CEE_MONO_TLS:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				MONO_INST_NEW (cfg, ins, OP_TLS_GET);
				ins->inst_offset = (gint32)read32 (ip + 2);
				ins->type = STACK_PTR;
				*sp++ = ins;
				ip += 6;
				break;
			default:
				g_error ("opcode 0x%02x 0x%02x not handled", MONO_CUSTOM_PREFIX, ip [1]);
				break;
			}
			break;
		}
		case CEE_PREFIX1: {
			CHECK_OPSIZE (2);
			switch (ip [1]) {
			case CEE_ARGLIST: {
				/* somewhat similar to LDTOKEN */
				MonoInst *addr, *vtvar;
				CHECK_STACK_OVF (1);
				vtvar = mono_compile_create_var (cfg, &mono_defaults.argumenthandle_class->byval_arg, OP_LOCAL); 

				NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
				MONO_INST_NEW (cfg, ins, OP_ARGLIST);
				ins->inst_left = addr;
				MONO_ADD_INS (bblock, ins);
				NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
				*sp++ = ins;
				ip += 2;
				break;
			}
			case CEE_CEQ:
			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN: {
				MonoInst *cmp;
				CHECK_STACK (2);
				/*
				 * The following transforms:
				 *    CEE_CEQ    into OP_CEQ
				 *    CEE_CGT    into OP_CGT
				 *    CEE_CGT_UN into OP_CGT_UN
				 *    CEE_CLT    into OP_CLT
				 *    CEE_CLT_UN into OP_CLT_UN
				 */
				MONO_INST_NEW (cfg, cmp, (OP_CEQ - CEE_CEQ) + ip [1]);
				
				MONO_INST_NEW (cfg, ins, cmp->opcode);
				sp -= 2;
				cmp->inst_i0 = sp [0];
				cmp->inst_i1 = sp [1];
				type_from_op (cmp);
				CHECK_TYPE (cmp);
				ins->type = STACK_I4;
				ins->inst_i0 = cmp;
#if MONO_ARCH_SOFT_FLOAT
				if (sp [0]->type == STACK_R8) {
					cmp->type = STACK_I4;
					*sp++ = emit_tree (cfg, bblock, cmp, ip + 2);
					ip += 2;
					break;
				}
#endif
				if ((sp [0]->type == STACK_I8) || ((sizeof (gpointer) == 8) && ((sp [0]->type == STACK_PTR) || (sp [0]->type == STACK_OBJ) || (sp [0]->type == STACK_MP))))
					cmp->opcode = OP_LCOMPARE;
				else
					cmp->opcode = OP_COMPARE;
				*sp++ = ins;
				/* spill it to reduce the expression complexity
				 * and workaround bug 54209 
				 */
				if (cmp->inst_left->type == STACK_I8) {
					--sp;
					*sp++ = emit_tree (cfg, bblock, ins, ip + 2);
				}
				ip += 2;
				break;
			}
			case CEE_LDFTN: {
				MonoInst *argconst;
				MonoMethod *cil_method, *ctor_method;
				int temp;
				gboolean needs_static_rgctx_invoke;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				n = read32 (ip + 2);
				cmethod = mini_get_method (cfg, method, n, NULL, generic_context);
				if (!cmethod)
					goto load_error;
				mono_class_init (cmethod->klass);

				if (cfg->generic_sharing_context)
					context_used = mono_method_check_context_used (cmethod);

				needs_static_rgctx_invoke = mono_method_needs_static_rgctx_invoke (cmethod, TRUE);

				cil_method = cmethod;
				if (!dont_verify && !cfg->skip_visibility && !mono_method_can_access_method (method, cmethod))
					METHOD_ACCESS_FAILURE;
				if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS) {
					if (check_linkdemand (cfg, method, cmethod, bblock, ip))
						INLINE_FAILURE;
					CHECK_CFG_EXCEPTION;
				} else if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR) {
					ensure_method_is_allowed_to_call_method (cfg, method, cmethod, bblock, ip);
				}

				/* 
				 * Optimize the common case of ldftn+delegate creation
				 */
#if defined(MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE) && !defined(HAVE_WRITE_BARRIERS)
				/* FIXME: SGEN support */
				/* FIXME: handle shared static generic methods */
				/* FIXME: handle this in shared code */
		 		if (!needs_static_rgctx_invoke && !context_used && (sp > stack_start) && (ip + 6 + 5 < end) && ip_in_bb (cfg, bblock, ip + 6) && (ip [6] == CEE_NEWOBJ) && (ctor_method = mini_get_method (cfg, method, read32 (ip + 7), NULL, generic_context)) && (ctor_method->klass->parent == mono_defaults.multicastdelegate_class)) {
					MonoInst *target_ins;

					ip += 6;
					if (cfg->verbose_level > 3)
						g_print ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));
					target_ins = sp [-1];
					sp --;
					*sp = handle_delegate_ctor (cfg, bblock, ctor_method->klass, target_ins, cmethod, ip);
					ip += 5;					
					sp ++;
					break;
				}
#endif

				handle_loaded_temps (cfg, bblock, stack_start, sp);

				if (context_used) {
					MonoInst *rgctx;

					if (needs_static_rgctx_invoke)
						cmethod = mono_marshal_get_static_rgctx_invoke (cmethod);

					GET_RGCTX (rgctx, context_used);
					argconst = get_runtime_generic_context_method (cfg, method, context_used,
							bblock, cmethod,
							generic_context, rgctx, MONO_RGCTX_INFO_METHOD, ip);
				} else if (needs_static_rgctx_invoke) {
					NEW_METHODCONST (cfg, argconst, mono_marshal_get_static_rgctx_invoke (cmethod));
				} else {
					NEW_METHODCONST (cfg, argconst, cmethod);
				}
				temp = mono_emit_jit_icall (cfg, bblock, mono_ldftn, &argconst, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);
				sp ++;
				
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_LDVIRTFTN: {
				MonoInst *args [2];
				int temp;

				CHECK_STACK (1);
				CHECK_OPSIZE (6);
				n = read32 (ip + 2);
				cmethod = mini_get_method (cfg, method, n, NULL, generic_context);
				if (!cmethod)
					goto load_error;
				mono_class_init (cmethod->klass);

				if (cfg->generic_sharing_context)
					context_used = mono_method_check_context_used (cmethod);

				if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS) {
					if (check_linkdemand (cfg, method, cmethod, bblock, ip))
						INLINE_FAILURE;
					CHECK_CFG_EXCEPTION;
				} else if (mono_security_get_mode () == MONO_SECURITY_MODE_CORE_CLR) {
					ensure_method_is_allowed_to_call_method (cfg, method, cmethod, bblock, ip);
				}

				handle_loaded_temps (cfg, bblock, stack_start, sp);

				--sp;
				args [0] = *sp;
				if (context_used) {
					MonoInst *rgctx;

					GET_RGCTX (rgctx, context_used);
					args [1] = get_runtime_generic_context_method (cfg, method, context_used,
							bblock, cmethod,
							generic_context, rgctx, MONO_RGCTX_INFO_METHOD, ip);
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldvirtfn_gshared, args, ip);
				} else {
					NEW_METHODCONST (cfg, args [1], cmethod);
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldvirtfn, args, ip);
				}
				NEW_TEMPLOAD (cfg, *sp, temp);
				sp ++;

				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_LDARG:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				NEW_ARGLOAD (cfg, ins, n);
				LDARG_SOFT_FLOAT (cfg, ins, n, ip);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDARGA:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				NEW_ARGLOADA (cfg, ins, n);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_STARG:
				CHECK_STACK (1);
				--sp;
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				NEW_ARGSTORE (cfg, ins, n, *sp);
				if (!dont_verify_stloc && target_type_is_incompatible (cfg, param_types [n], *sp))
					UNVERIFIED;
				STARG_SOFT_FLOAT (cfg, ins, n, ip);
				if (ins->opcode == CEE_STOBJ) {
					NEW_ARGLOADA (cfg, ins, n);
					handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE, FALSE);
				} else
					MONO_ADD_INS (bblock, ins);
				ip += 4;
				break;
			case CEE_LDLOC:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);
				NEW_LOCLOAD (cfg, ins, n);
				LDLOC_SOFT_FLOAT (cfg, ins, n, ip);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDLOCA:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);
				NEW_LOCLOADA (cfg, ins, n);
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_STLOC:
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				NEW_LOCSTORE (cfg, ins, n, *sp);
				if (!dont_verify_stloc && target_type_is_incompatible (cfg, header->locals [n], *sp))
					UNVERIFIED;
				STLOC_SOFT_FLOAT (cfg, ins, n, ip);
				if (ins->opcode == CEE_STOBJ) {
					NEW_LOCLOADA (cfg, ins, n);
					handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE, FALSE);
				} else
					MONO_ADD_INS (bblock, ins);
				ip += 4;
				inline_costs += 1;
				break;
			case CEE_LOCALLOC:
				CHECK_STACK (1);
				--sp;
				if (sp != stack_start) 
					UNVERIFIED;
				if (cfg->method != method) 
					/* 
					 * Inlining this into a loop in a parent could lead to 
					 * stack overflows which is different behavior than the
					 * non-inlined case, thus disable inlining in this case.
					 */
					goto inline_failure;
				MONO_INST_NEW (cfg, ins, OP_LOCALLOC);
				ins->inst_left = *sp;
				ins->type = STACK_PTR;

				cfg->flags |= MONO_CFG_HAS_ALLOCA;
				if (header->init_locals)
					ins->flags |= MONO_INST_INIT;

				*sp++ = ins;
				ip += 2;
				/* FIXME: set init flag if locals init is set in this method */
				break;
			case CEE_ENDFILTER: {
				MonoExceptionClause *clause, *nearest;
				int cc, nearest_num;

				CHECK_STACK (1);
				--sp;
				if ((sp != stack_start) || (sp [0]->type != STACK_I4)) 
					UNVERIFIED;
				MONO_INST_NEW (cfg, ins, OP_ENDFILTER);
				ins->inst_left = *sp;
				MONO_ADD_INS (bblock, ins);
				start_new_bblock = 1;
				ip += 2;

				nearest = NULL;
				nearest_num = 0;
				for (cc = 0; cc < header->num_clauses; ++cc) {
					clause = &header->clauses [cc];
					if ((clause->flags & MONO_EXCEPTION_CLAUSE_FILTER) &&
						((ip - header->code) > clause->data.filter_offset && (ip - header->code) <= clause->handler_offset) &&
					    (!nearest || (clause->data.filter_offset < nearest->data.filter_offset))) {
						nearest = clause;
						nearest_num = cc;
					}
				}
				g_assert (nearest);
				if ((ip - header->code) != nearest->handler_offset)
					UNVERIFIED;

				break;
			}
			case CEE_UNALIGNED_:
				ins_flag |= MONO_INST_UNALIGNED;
				/* FIXME: record alignment? we can assume 1 for now */
				CHECK_OPSIZE (3);
				ip += 3;
				break;
			case CEE_VOLATILE_:
				ins_flag |= MONO_INST_VOLATILE;
				ip += 2;
				break;
			case CEE_TAIL_:
				ins_flag   |= MONO_INST_TAILCALL;
				cfg->flags |= MONO_CFG_HAS_TAIL;
				/* Can't inline tail calls at this time */
				inline_costs += 100000;
				ip += 2;
				break;
			case CEE_INITOBJ:
				CHECK_STACK (1);
				--sp;
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				klass = mini_get_class (method, token, generic_context);
				CHECK_TYPELOAD (klass);

				if (generic_class_is_reference_type (cfg, klass)) {
					MonoInst *store, *load;
					NEW_PCONST (cfg, load, NULL);
					load->type = STACK_OBJ;
					load->klass = klass;
					MONO_INST_NEW (cfg, store, CEE_STIND_REF);
					handle_loaded_temps (cfg, bblock, stack_start, sp);
					MONO_ADD_INS (bblock, store);
					store->inst_i0 = sp [0];
					store->inst_i1 = load;
				} else {
					handle_initobj (cfg, bblock, *sp, NULL, klass, stack_start, sp);
				}
				ip += 6;
				inline_costs += 1;
				break;
			case CEE_CONSTRAINED_:
				/* FIXME: implement */
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				constrained_call = mono_class_get_full (image, token, generic_context);
				CHECK_TYPELOAD (constrained_call);
				ip += 6;
				break;
			case CEE_CPBLK:
			case CEE_INITBLK: {
				MonoInst *iargs [3];
				CHECK_STACK (3);
				sp -= 3;
				if ((cfg->opt & MONO_OPT_INTRINS) && (ip [1] == CEE_CPBLK) && (sp [2]->opcode == OP_ICONST) && ((n = sp [2]->inst_c0) <= sizeof (gpointer) * 5)) {
					MonoInst *copy;
					NEW_MEMCPY (cfg, copy, sp [0], sp [1], n, 0);
					MONO_ADD_INS (bblock, copy);
					ip += 2;
					break;
				}
				iargs [0] = sp [0];
				iargs [1] = sp [1];
				iargs [2] = sp [2];
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				if (ip [1] == CEE_CPBLK) {
					MonoMethod *memcpy_method = get_memcpy_method ();
					mono_emit_method_call_spilled (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL);
				} else {
					MonoMethod *memset_method = get_memset_method ();
					mono_emit_method_call_spilled (cfg, bblock, memset_method, memset_method->signature, iargs, ip, NULL);
				}
				ip += 2;
				inline_costs += 1;
				break;
			}
			case CEE_NO_:
				CHECK_OPSIZE (3);
				if (ip [2] & 0x1)
					ins_flag |= MONO_INST_NOTYPECHECK;
				if (ip [2] & 0x2)
					ins_flag |= MONO_INST_NORANGECHECK;
				/* we ignore the no-nullcheck for now since we
				 * really do it explicitly only when doing callvirt->call
				 */
				ip += 3;
				break;
			case CEE_RETHROW: {
				MonoInst *load;
				int handler_offset = -1;

				for (i = 0; i < header->num_clauses; ++i) {
					MonoExceptionClause *clause = &header->clauses [i];
					if (MONO_OFFSET_IN_HANDLER (clause, ip - header->code) && !(clause->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
						handler_offset = clause->handler_offset;
						break;
					}
				}

				bblock->flags |= BB_EXCEPTION_UNSAFE;

				g_assert (handler_offset != -1);

				NEW_TEMPLOAD (cfg, load, mono_find_exvar_for_offset (cfg, handler_offset)->inst_c0);
				MONO_INST_NEW (cfg, ins, OP_RETHROW);
				ins->inst_left = load;
				MONO_ADD_INS (bblock, ins);
				sp = stack_start;
				link_bblock (cfg, bblock, end_bblock);
				start_new_bblock = 1;
				ip += 2;
				break;
			}
			case CEE_SIZEOF:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				/* FIXXME: handle generics. */
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
					MonoType *type = mono_type_create_from_typespec (image, token);
					token = mono_type_size (type, &ialign);
				} else {
					MonoClass *klass = mono_class_get_full (image, token, generic_context);
					CHECK_TYPELOAD (klass);
					mono_class_init (klass);
					token = mono_class_value_size (klass, &align);
				}
				NEW_ICONST (cfg, ins, token);
				*sp++= ins;
				ip += 6;
				break;
			case CEE_REFANYTYPE:
				CHECK_STACK (1);
				MONO_INST_NEW (cfg, ins, OP_REFANYTYPE);
				--sp;
				ins->type = STACK_MP;
				ins->inst_left = *sp;
				ins->type = STACK_VTYPE;
				ins->klass = mono_defaults.typehandle_class;
				ip += 2;
				*sp++ = ins;
				break;
			case CEE_READONLY_:
				readonly = TRUE;
				ip += 2;
				break;
			default:
				g_error ("opcode 0xfe 0x%02x not handled", ip [1]);
			}
			break;
		}
		default:
			g_error ("opcode 0x%02x not handled", *ip);
		}
	}
	if (start_new_bblock != 1)
		UNVERIFIED;

	bblock->cil_length = ip - bblock->cil_code;
	bblock->next_bb = end_bblock;

	if (cfg->method == method && cfg->domainvar) {
		MonoInst *store;
		MonoInst *get_domain;
		
		if (! (get_domain = mono_arch_get_domain_intrinsic (cfg))) {
			MonoCallInst *call;
			
			MONO_INST_NEW_CALL (cfg, call, OP_CALL);
			call->signature = helper_sig_domain_get;
			call->inst.type = STACK_PTR;
			call->fptr = mono_domain_get;
			get_domain = (MonoInst*)call;
		}
		
		NEW_TEMPSTORE (cfg, store, cfg->domainvar->inst_c0, get_domain);
		MONO_ADD_INS (init_localsbb, store);
	}

	if (cfg->method == method && cfg->got_var)
		mono_emit_load_got_addr (cfg);

	if (header->init_locals) {
		MonoInst *store;
		cfg->ip = header->code;
		for (i = 0; i < header->num_locals; ++i) {
			MonoType *ptype = header->locals [i];
			int t = ptype->type;
			if (t == MONO_TYPE_VALUETYPE && ptype->data.klass->enumtype)
				t = ptype->data.klass->enum_basetype->type;
			if (ptype->byref) {
				NEW_PCONST (cfg, ins, NULL);
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
			} else if (t >= MONO_TYPE_BOOLEAN && t <= MONO_TYPE_U4) {
				NEW_ICONST (cfg, ins, 0);
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
			} else if (t == MONO_TYPE_I8 || t == MONO_TYPE_U8) {
				MONO_INST_NEW (cfg, ins, OP_I8CONST);
				ins->type = STACK_I8;
				ins->inst_l = 0;
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
			} else if (t == MONO_TYPE_R4 || t == MONO_TYPE_R8) {
#ifdef MONO_ARCH_SOFT_FLOAT
				/* FIXME: handle init of R4 */
#else
				MONO_INST_NEW (cfg, ins, OP_R8CONST);
				ins->type = STACK_R8;
				ins->inst_p0 = (void*)&r8_0;
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
#endif
			} else if ((t == MONO_TYPE_VALUETYPE) || (t == MONO_TYPE_TYPEDBYREF) ||
				   ((t == MONO_TYPE_GENERICINST) && mono_type_generic_inst_is_valuetype (ptype))) {
				NEW_LOCLOADA (cfg, ins, i);
				handle_initobj (cfg, init_localsbb, ins, NULL, mono_class_from_mono_type (ptype), NULL, NULL);
			} else {
				NEW_PCONST (cfg, ins, NULL);
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
			}
		}
	}

	cfg->ip = NULL;

	/* resolve backward branches in the middle of an existing basic block */
	for (tmp = bb_recheck; tmp; tmp = tmp->next) {
		bblock = tmp->data;
		/*g_print ("need recheck in %s at IL_%04x\n", method->name, bblock->cil_code - header->code);*/
		tblock = find_previous (cfg->cil_offset_to_bb, header->code_size, start_bblock, bblock->cil_code);
		if (tblock != start_bblock) {
			int l;
			split_bblock (cfg, tblock, bblock);
			l = bblock->cil_code - header->code;
			bblock->cil_length = tblock->cil_length - l;
			tblock->cil_length = l;
		} else {
			g_print ("recheck failed.\n");
		}
	}

	if (cfg->method == method) {
		MonoBasicBlock *bb;
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb->region = mono_find_block_region (cfg, bb->real_offset);
			if (cfg->spvars)
				mono_create_spvar_for_region (cfg, bb->region);
			if (cfg->verbose_level > 2)
				g_print ("REGION BB%d IL_%04x ID_%08X\n", bb->block_num, bb->real_offset, bb->region);
		}
	}

	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);

	if (inline_costs < 0) {
		char *mname;

		/* Method is too large */
		mname = mono_method_full_name (method, TRUE);
		cfg->exception_type = MONO_EXCEPTION_INVALID_PROGRAM;
		cfg->exception_message = g_strdup_printf ("Method %s is too complex.", mname);
		g_free (mname);
		return -1;
	}

	return inline_costs;

 exception_exit:
	g_assert (cfg->exception_type != MONO_EXCEPTION_NONE);
	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);
	return -1;

 inline_failure:
	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);
	return -1;

 load_error:
	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);
	cfg->exception_type = MONO_EXCEPTION_TYPE_LOAD;
	return -1;

 unverified:
	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);
	set_exception_type_from_invalid_il (cfg, method, ip);
	return -1;
}

void
mono_print_tree (MonoInst *tree) {
	int arity;

	if (!tree)
		return;

	arity = mono_burg_arity [tree->opcode];

	printf (" %s%s", arity?"(":"",  mono_inst_name (tree->opcode));

	switch (tree->opcode) {
	case OP_ICONST:
		printf ("[%d]", (int)tree->inst_c0);
		break;
	case OP_I8CONST:
		printf ("[%lld]", (long long)tree->inst_l);
		break;
	case OP_R8CONST:
		printf ("[%f]", *(double*)tree->inst_p0);
		break;
	case OP_R4CONST:
		printf ("[%f]", *(float*)tree->inst_p0);
		break;
	case OP_ARG:
	case OP_LOCAL:
		printf ("[%d]", (int)tree->inst_c0);
		break;
	case OP_REGOFFSET:
		if (tree->inst_offset < 0)
			printf ("[-0x%x(%s)]", (int)(-tree->inst_offset), mono_arch_regname (tree->inst_basereg));
		else
			printf ("[0x%x(%s)]", (int)(tree->inst_offset), mono_arch_regname (tree->inst_basereg));
		break;
	case OP_REGVAR:
		printf ("[%s]", mono_arch_regname (tree->dreg));
		break;
	case CEE_NEWARR:
		printf ("[%s]",  tree->inst_newa_class->name);
		mono_print_tree (tree->inst_newa_len);
		break;
	case OP_CALL:
	case OP_CALLVIRT:
	case OP_FCALL:
	case OP_FCALLVIRT:
	case OP_LCALL:
	case OP_LCALLVIRT:
	case OP_VCALL:
	case OP_VCALLVIRT:
	case OP_VOIDCALL:
	case OP_VOIDCALLVIRT:
	case OP_TRAMPCALL_VTABLE: {
		MonoCallInst *call = (MonoCallInst*)tree;
		if (call->method)
			printf ("[%s]", call->method->name);
		else if (call->fptr) {
			MonoJitICallInfo *info = mono_find_jit_icall_by_addr (call->fptr);
			if (info)
				printf ("[%s]", info->name);
		}
		break;
	}
	case OP_PHI: {
		int i;
		printf ("[%d (", (int)tree->inst_c0);
		for (i = 0; i < tree->inst_phi_args [0]; i++) {
			if (i)
				printf (", ");
			printf ("%d", tree->inst_phi_args [i + 1]);
		}
		printf (")]");
		break;
	}
	case OP_RENAME:
	case OP_RETARG:
	case OP_NOP:
	case OP_JMP:
	case OP_BREAK:
		break;
	case OP_LOAD_MEMBASE:
	case OP_LOADI4_MEMBASE:
	case OP_LOADU4_MEMBASE:
	case OP_LOADU1_MEMBASE:
	case OP_LOADI1_MEMBASE:
	case OP_LOADU2_MEMBASE:
	case OP_LOADI2_MEMBASE:
		printf ("[%s] <- [%s + 0x%x]", mono_arch_regname (tree->dreg), mono_arch_regname (tree->inst_basereg), (int)tree->inst_offset);
		break;
	case OP_BR:
	case OP_CALL_HANDLER:
		printf ("[B%d]", tree->inst_target_bb->block_num);
		break;
	case OP_SWITCH:
	case CEE_ISINST:
	case CEE_CASTCLASS:
	case OP_OUTARG:
	case OP_CALL_REG:
	case OP_FCALL_REG:
	case OP_LCALL_REG:
	case OP_VCALL_REG:
	case OP_VOIDCALL_REG:
		mono_print_tree (tree->inst_left);
		break;
	case CEE_BNE_UN:
	case CEE_BEQ:
	case CEE_BLT:
	case CEE_BLT_UN:
	case CEE_BGT:
	case CEE_BGT_UN:
	case CEE_BGE:
	case CEE_BGE_UN:
	case CEE_BLE:
	case CEE_BLE_UN:
		printf ("[B%dB%d]", tree->inst_true_bb->block_num, tree->inst_false_bb->block_num);
		mono_print_tree (tree->inst_left);
		break;
	default:
		if (!mono_arch_print_tree(tree, arity)) {
			if (arity) {
				mono_print_tree (tree->inst_left);
				if (arity > 1)
					mono_print_tree (tree->inst_right);
			}
		}
		break;
	}

	if (arity)
		printf (")");
}

void
mono_print_tree_nl (MonoInst *tree)
{
	mono_print_tree (tree);
	printf ("\n");
}

static void
create_helper_signature (void)
{
	helper_sig_domain_get = mono_create_icall_signature ("ptr");
	helper_sig_class_init_trampoline = mono_create_icall_signature ("void");
	helper_sig_generic_class_init_trampoline = mono_create_icall_signature ("void");
	helper_sig_rgctx_lazy_fetch_trampoline = mono_create_icall_signature ("ptr ptr");
	helper_sig_monitor_enter_exit_trampoline = mono_create_icall_signature ("void");
}

static gconstpointer
mono_icall_get_wrapper_full (MonoJitICallInfo* callinfo, gboolean do_compile)
{
	char *name;
	MonoMethod *wrapper;
	gconstpointer trampoline;
	MonoDomain *domain = mono_get_root_domain ();
	
	if (callinfo->wrapper) {
		return callinfo->wrapper;
	}

	if (callinfo->trampoline)
		return callinfo->trampoline;

	/* 
	 * We use the lock on the root domain instead of the JIT lock to protect 
	 * callinfo->trampoline, since we do a lot of stuff inside the critical section.
	 */
	mono_domain_lock (domain);

	if (callinfo->trampoline) {
		mono_domain_unlock (domain);
		return callinfo->trampoline;
	}

	name = g_strdup_printf ("__icall_wrapper_%s", callinfo->name);
	wrapper = mono_marshal_get_icall_wrapper (callinfo->sig, name, callinfo->func, check_for_pending_exc);
	g_free (name);

	if (do_compile)
		trampoline = mono_compile_method (wrapper);
	else
		trampoline = mono_create_ftnptr (domain, mono_create_jit_trampoline_in_domain (domain, wrapper));
	mono_register_jit_icall_wrapper (callinfo, trampoline);

	callinfo->trampoline = trampoline;

	mono_domain_unlock (domain);
	
	return callinfo->trampoline;
}

gconstpointer
mono_icall_get_wrapper (MonoJitICallInfo* callinfo)
{
	return mono_icall_get_wrapper_full (callinfo, FALSE);
}

static void
mono_dynamic_code_hash_insert (MonoDomain *domain, MonoMethod *method, MonoJitDynamicMethodInfo *ji)
{
	if (!domain_jit_info (domain)->dynamic_code_hash)
		domain_jit_info (domain)->dynamic_code_hash = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (domain_jit_info (domain)->dynamic_code_hash, method, ji);
}

static MonoJitDynamicMethodInfo*
mono_dynamic_code_hash_lookup (MonoDomain *domain, MonoMethod *method)
{
	MonoJitDynamicMethodInfo *res;

	if (domain_jit_info (domain)->dynamic_code_hash)
		res = g_hash_table_lookup (domain_jit_info (domain)->dynamic_code_hash, method);
	else
		res = NULL;
	return res;
}

typedef struct {
	MonoClass *vtype;
	GList *active, *inactive;
	GSList *slots;
} StackSlotInfo;

static gint 
compare_by_interval_start_pos_func (gconstpointer a, gconstpointer b)
{
	MonoMethodVar *v1 = (MonoMethodVar*)a;
	MonoMethodVar *v2 = (MonoMethodVar*)b;

	if (v1 == v2)
		return 0;
	else if (v1->interval->range && v2->interval->range)
		return v1->interval->range->from - v2->interval->range->from;
	else if (v1->interval->range)
		return -1;
	else
		return 1;
}

#ifndef DISABLE_JIT

#if 0
#define LSCAN_DEBUG(a) do { a; } while (0)
#else
#define LSCAN_DEBUG(a)
#endif

static gint32*
mono_allocate_stack_slots_full2 (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
{
	int i, slot, offset, size;
	guint32 align;
	MonoMethodVar *vmv;
	MonoInst *inst;
	gint32 *offsets;
	GList *vars = NULL, *l, *unhandled;
	StackSlotInfo *scalar_stack_slots, *vtype_stack_slots, *slot_info;
	MonoType *t;
	int nvtypes;

	LSCAN_DEBUG (printf ("Allocate Stack Slots 2 for %s:\n", mono_method_full_name (cfg->method, TRUE)));

	scalar_stack_slots = mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * MONO_TYPE_PINNED);
	vtype_stack_slots = NULL;
	nvtypes = 0;

	offsets = mono_mempool_alloc (cfg->mempool, sizeof (gint32) * cfg->num_varinfo);
	for (i = 0; i < cfg->num_varinfo; ++i)
		offsets [i] = -1;

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		inst = cfg->varinfo [i];
		vmv = MONO_VARINFO (cfg, i);

		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR || inst->opcode == OP_REGOFFSET)
			continue;

		vars = g_list_prepend (vars, vmv);
	}

	vars = g_list_sort (g_list_copy (vars), compare_by_interval_start_pos_func);

	/* Sanity check */
	/*
	i = 0;
	for (unhandled = vars; unhandled; unhandled = unhandled->next) {
		MonoMethodVar *current = unhandled->data;

		if (current->interval->range) {
			g_assert (current->interval->range->from >= i);
			i = current->interval->range->from;
		}
	}
	*/

	offset = 0;
	*stack_align = 0;
	for (unhandled = vars; unhandled; unhandled = unhandled->next) {
		MonoMethodVar *current = unhandled->data;

		vmv = current;
		inst = cfg->varinfo [vmv->idx];

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type (inst->inst_vtype), &align);
		}
		else {
			int ialign;

			size = mono_type_size (inst->inst_vtype, &ialign);
			align = ialign;
		}

		t = mono_type_get_underlying_type (inst->inst_vtype);
		switch (t->type) {
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (t)) {
				slot_info = &scalar_stack_slots [t->type];
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
			if (!vtype_stack_slots)
				vtype_stack_slots = mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * 256);
			for (i = 0; i < nvtypes; ++i)
				if (t->data.klass == vtype_stack_slots [i].vtype)
					break;
			if (i < nvtypes)
				slot_info = &vtype_stack_slots [i];
			else {
				g_assert (nvtypes < 256);
				vtype_stack_slots [nvtypes].vtype = t->data.klass;
				slot_info = &vtype_stack_slots [nvtypes];
				nvtypes ++;
			}
			break;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_I4:
#else
		case MONO_TYPE_I8:
			/* Share non-float stack slots of the same size */
			slot_info = &scalar_stack_slots [MONO_TYPE_CLASS];
			break;
#endif
		default:
			slot_info = &scalar_stack_slots [t->type];
		}

		slot = 0xffffff;
		if (cfg->comp_done & MONO_COMP_LIVENESS) {
			int pos;
			gboolean changed;

			//printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos);

			if (!current->interval->range) {
				if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
					pos = ~0;
				else {
					/* Dead */
					inst->flags |= MONO_INST_IS_DEAD;
					continue;
				}
			}
			else
				pos = current->interval->range->from;

			LSCAN_DEBUG (printf ("process R%d ", inst->dreg));
			if (current->interval->range)
				LSCAN_DEBUG (mono_linterval_print (current->interval));
			LSCAN_DEBUG (printf ("\n"));

			/* Check for intervals in active which expired or inactive */
			changed = TRUE;
			/* FIXME: Optimize this */
			while (changed) {
				changed = FALSE;
				for (l = slot_info->active; l != NULL; l = l->next) {
					MonoMethodVar *v = (MonoMethodVar*)l->data;

					if (v->interval->last_range->to < pos) {
						slot_info->active = g_list_delete_link (slot_info->active, l);
						slot_info->slots = g_slist_prepend_mempool (cfg->mempool, slot_info->slots, GINT_TO_POINTER (offsets [v->idx]));
						LSCAN_DEBUG (printf ("Interval R%d has expired, adding 0x%x to slots\n", cfg->varinfo [v->idx]->dreg, offsets [v->idx]));
						changed = TRUE;
						break;
					}
					else if (!mono_linterval_covers (v->interval, pos)) {
						slot_info->inactive = g_list_append (slot_info->inactive, v);
						slot_info->active = g_list_delete_link (slot_info->active, l);
						LSCAN_DEBUG (printf ("Interval R%d became inactive\n", cfg->varinfo [v->idx]->dreg));
						changed = TRUE;
						break;
					}
				}
			}

			/* Check for intervals in inactive which expired or active */
			changed = TRUE;
			/* FIXME: Optimize this */
			while (changed) {
				changed = FALSE;
				for (l = slot_info->inactive; l != NULL; l = l->next) {
					MonoMethodVar *v = (MonoMethodVar*)l->data;

					if (v->interval->last_range->to < pos) {
						slot_info->inactive = g_list_delete_link (slot_info->inactive, l);
						// FIXME: Enabling this seems to cause impossible to debug crashes
						//slot_info->slots = g_slist_prepend_mempool (cfg->mempool, slot_info->slots, GINT_TO_POINTER (offsets [v->idx]));
						LSCAN_DEBUG (printf ("Interval R%d has expired, adding 0x%x to slots\n", cfg->varinfo [v->idx]->dreg, offsets [v->idx]));
						changed = TRUE;
						break;
					}
					else if (mono_linterval_covers (v->interval, pos)) {
						slot_info->active = g_list_append (slot_info->active, v);
						slot_info->inactive = g_list_delete_link (slot_info->inactive, l);
						LSCAN_DEBUG (printf ("\tInterval R%d became active\n", cfg->varinfo [v->idx]->dreg));
						changed = TRUE;
						break;
					}
				}
			}

			/* 
			 * This also handles the case when the variable is used in an
			 * exception region, as liveness info is not computed there.
			 */
			/* 
			 * FIXME: All valuetypes are marked as INDIRECT because of LDADDR
			 * opcodes.
			 */
			if (! (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				if (slot_info->slots) {
					slot = GPOINTER_TO_INT (slot_info->slots->data);

					slot_info->slots = slot_info->slots->next;
				}

				/* FIXME: We might want to consider the inactive intervals as well if slot_info->slots is empty */

				slot_info->active = mono_varlist_insert_sorted (cfg, slot_info->active, vmv, TRUE);
			}
		}

#if 0
		{
			static int count = 0;
			count ++;

			if (count == atoi (getenv ("COUNT3")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (getenv ("COUNT3")))
				slot = 0xffffff;
			else {
				mono_print_tree_nl (inst);
				}
		}
#endif

		LSCAN_DEBUG (printf ("R%d %s -> 0x%x\n", inst->dreg, mono_type_full_name (t), slot));

		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (gpointer) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (MONO_TYPE_ISSTRUCT (t))
				align = MAX (sizeof (gpointer), mono_class_min_align (mono_class_from_mono_type (t)));

			if (backward) {
				offset += size;
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
			}
			else {
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
				offset += size;
			}

			if (*stack_align == 0)
				*stack_align = align;
		}

		offsets [vmv->idx] = slot;
	}
	g_list_free (vars);
	for (i = 0; i < MONO_TYPE_PINNED; ++i) {
		if (scalar_stack_slots [i].active)
			g_list_free (scalar_stack_slots [i].active);
	}
	for (i = 0; i < nvtypes; ++i) {
		if (vtype_stack_slots [i].active)
			g_list_free (vtype_stack_slots [i].active);
	}

	mono_jit_stats.locals_stack_size += offset;

	*stack_size = offset;
	return offsets;
}

/*
 *  mono_allocate_stack_slots_full:
 *
 *  Allocate stack slots for all non register allocated variables using a
 * linear scan algorithm.
 * Returns: an array of stack offsets.
 * STACK_SIZE is set to the amount of stack space needed.
 * STACK_ALIGN is set to the alignment needed by the locals area.
 */
gint32*
mono_allocate_stack_slots_full (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
{
	int i, slot, offset, size;
	guint32 align;
	MonoMethodVar *vmv;
	MonoInst *inst;
	gint32 *offsets;
	GList *vars = NULL, *l;
	StackSlotInfo *scalar_stack_slots, *vtype_stack_slots, *slot_info;
	MonoType *t;
	int nvtypes;

	if ((cfg->num_varinfo > 0) && MONO_VARINFO (cfg, 0)->interval)
		return mono_allocate_stack_slots_full2 (cfg, backward, stack_size, stack_align);

	scalar_stack_slots = mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * MONO_TYPE_PINNED);
	vtype_stack_slots = NULL;
	nvtypes = 0;

	offsets = mono_mempool_alloc (cfg->mempool, sizeof (gint32) * cfg->num_varinfo);
	for (i = 0; i < cfg->num_varinfo; ++i)
		offsets [i] = -1;

	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		inst = cfg->varinfo [i];
		vmv = MONO_VARINFO (cfg, i);

		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR || inst->opcode == OP_REGOFFSET)
			continue;

		vars = g_list_prepend (vars, vmv);
	}

	vars = mono_varlist_sort (cfg, vars, 0);
	offset = 0;
	*stack_align = sizeof (gpointer);
	for (l = vars; l; l = l->next) {
		vmv = l->data;
		inst = cfg->varinfo [vmv->idx];

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type (inst->inst_vtype), &align);
		} else {
			int ialign;

			size = mono_type_size (inst->inst_vtype, &ialign);
			align = ialign;
		}

		t = mono_type_get_underlying_type (inst->inst_vtype);
		if (t->byref) {
			slot_info = &scalar_stack_slots [MONO_TYPE_I];
		} else {
			switch (t->type) {
			case MONO_TYPE_GENERICINST:
				if (!mono_type_generic_inst_is_valuetype (t)) {
					slot_info = &scalar_stack_slots [t->type];
					break;
				}
				/* Fall through */
			case MONO_TYPE_VALUETYPE:
				if (!vtype_stack_slots)
					vtype_stack_slots = mono_mempool_alloc0 (cfg->mempool, sizeof (StackSlotInfo) * 256);
				for (i = 0; i < nvtypes; ++i)
					if (t->data.klass == vtype_stack_slots [i].vtype)
						break;
				if (i < nvtypes)
					slot_info = &vtype_stack_slots [i];
				else {
					g_assert (nvtypes < 256);
					vtype_stack_slots [nvtypes].vtype = t->data.klass;
					slot_info = &vtype_stack_slots [nvtypes];
					nvtypes ++;
				}
				break;
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_STRING:
			case MONO_TYPE_PTR:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#if SIZEOF_VOID_P == 4
			case MONO_TYPE_I4:
#else
			case MONO_TYPE_I8:
#endif
				/* Share non-float stack slots of the same size */
				slot_info = &scalar_stack_slots [MONO_TYPE_CLASS];
				break;
			default:
				slot_info = &scalar_stack_slots [t->type];
			}
		}

		slot = 0xffffff;
		if (cfg->comp_done & MONO_COMP_LIVENESS) {
			//printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos);
			
			/* expire old intervals in active */
			while (slot_info->active) {
				MonoMethodVar *amv = (MonoMethodVar *)slot_info->active->data;

				if (amv->range.last_use.abs_pos > vmv->range.first_use.abs_pos)
					break;

				//printf ("EXPIR  %2d %08x %08x C%d R%d\n", amv->idx, amv->range.first_use.abs_pos, amv->range.last_use.abs_pos, amv->spill_costs, amv->reg);

				slot_info->active = g_list_delete_link (slot_info->active, slot_info->active);
				slot_info->slots = g_slist_prepend_mempool (cfg->mempool, slot_info->slots, GINT_TO_POINTER (offsets [amv->idx]));
			}

			/* 
			 * This also handles the case when the variable is used in an
			 * exception region, as liveness info is not computed there.
			 */
			/* 
			 * FIXME: All valuetypes are marked as INDIRECT because of LDADDR
			 * opcodes.
			 */
			if (! (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))) {
				if (slot_info->slots) {
					slot = GPOINTER_TO_INT (slot_info->slots->data);

					slot_info->slots = slot_info->slots->next;
				}

				slot_info->active = mono_varlist_insert_sorted (cfg, slot_info->active, vmv, TRUE);
			}
		}

		{
			static int count = 0;
			count ++;

			/*
			if (count == atoi (getenv ("COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (getenv ("COUNT")))
				slot = 0xffffff;
			else {
				mono_print_tree_nl (inst);
				}
			*/
		}

		if (cfg->disable_reuse_stack_slots)
			slot = 0xffffff;

		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (gpointer) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (MONO_TYPE_ISSTRUCT (t)) {
				align = MAX (sizeof (gpointer), mono_class_min_align (mono_class_from_mono_type (t)));
				/* 
				 * Align the size too so the code generated for passing vtypes in
				 * registers doesn't overwrite random locals.
				 */
				size = (size + (align - 1)) & ~(align -1);
			}

			if (backward) {
				offset += size;
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
			}
			else {
				offset += align - 1;
				offset &= ~(align - 1);
				slot = offset;
				offset += size;
			}

			*stack_align = MAX (*stack_align, align);
		}

		offsets [vmv->idx] = slot;
	}
	g_list_free (vars);
	for (i = 0; i < MONO_TYPE_PINNED; ++i) {
		if (scalar_stack_slots [i].active)
			g_list_free (scalar_stack_slots [i].active);
	}
	for (i = 0; i < nvtypes; ++i) {
		if (vtype_stack_slots [i].active)
			g_list_free (vtype_stack_slots [i].active);
	}

	mono_jit_stats.locals_stack_size += offset;

	*stack_size = offset;
	return offsets;
}

gint32*
mono_allocate_stack_slots (MonoCompile *m, guint32 *stack_size, guint32 *stack_align)
{
	return mono_allocate_stack_slots_full (m, TRUE, stack_size, stack_align);
}

#else

gint32*
mono_allocate_stack_slots_full (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* DISABLE_JIT */

void
mono_register_opcode_emulation (int opcode, const char *name, const char *sigstr, gpointer func, gboolean no_throw)
{
	MonoJitICallInfo *info;
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	if (!emul_opcode_map)
		emul_opcode_map = g_new0 (MonoJitICallInfo*, OP_LAST + 1);

	g_assert (!sig->hasthis);
	g_assert (sig->param_count < 3);

	info = mono_register_jit_icall (func, name, sig, no_throw);

	emul_opcode_map [opcode] = info;
}

static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean save)
{
	MonoMethodSignature *sig;

	if (sigstr)
		sig = mono_create_icall_signature (sigstr);
	else
		sig = NULL;

	mono_register_jit_icall (func, name, sig, save);
}

static void
decompose_foreach (MonoInst *tree, gpointer data) 
{
	static MonoJitICallInfo *newarr_info = NULL;
	static MonoJitICallInfo *newarr_specific_info = NULL;
	MonoJitICallInfo *info;
	int i;

	switch (tree->opcode) {
	case CEE_NEWARR: {
		MonoCompile *cfg = data;
		MonoInst *iargs [3];

		if (!newarr_info) {
			newarr_info = mono_find_jit_icall_by_addr (mono_array_new);
			g_assert (newarr_info);
			newarr_specific_info = mono_find_jit_icall_by_addr (mono_array_new_specific);
			g_assert (newarr_specific_info);
		}

		if (cfg->opt & MONO_OPT_SHARED) {
			NEW_DOMAINCONST (cfg, iargs [0]);
			NEW_CLASSCONST (cfg, iargs [1], tree->inst_newa_class);
			iargs [2] = tree->inst_newa_len;

			info = newarr_info;
		}
		else {
			MonoVTable *vtable = mono_class_vtable (cfg->domain, mono_array_class_get (tree->inst_newa_class, 1));

			g_assert (vtable);
			NEW_VTABLECONST (cfg, iargs [0], vtable);
			iargs [1] = tree->inst_newa_len;

			info = newarr_specific_info;
		}

		mono_emulate_opcode (cfg, tree, iargs, info);

		/* Need to decompose arguments after the the opcode is decomposed */
		for (i = 0; i < info->sig->param_count; ++i)
			dec_foreach (iargs [i], cfg);
		break;
	}
#ifdef MONO_ARCH_SOFT_FLOAT
	case OP_FBEQ:
	case OP_FBGE:
	case OP_FBGT:
	case OP_FBLE:
	case OP_FBLT:
	case OP_FBNE_UN:
	case OP_FBGE_UN:
	case OP_FBGT_UN:
	case OP_FBLE_UN:
	case OP_FBLT_UN: {
		if ((info = mono_find_jit_opcode_emulation (tree->opcode))) {
			MonoCompile *cfg = data;
			MonoInst *iargs [2];
		
			iargs [0] = tree->inst_i0;
			iargs [1] = tree->inst_i1;
		
			mono_emulate_opcode (cfg, tree, iargs, info);

			dec_foreach (iargs [0], cfg);
			dec_foreach (iargs [1], cfg);
			break;
		} else {
			g_assert_not_reached ();
		}
		break;
	}
	case OP_FCEQ:
	case OP_FCGT:
	case OP_FCGT_UN:
	case OP_FCLT:
	case OP_FCLT_UN: {
		if ((info = mono_find_jit_opcode_emulation (tree->opcode))) {
			MonoCompile *cfg = data;
			MonoInst *iargs [2];

			/* the args are in the compare opcode ... */
			iargs [0] = tree->inst_i0;
			iargs [1] = tree->inst_i1;
		
			mono_emulate_opcode (cfg, tree, iargs, info);

			dec_foreach (iargs [0], cfg);
			dec_foreach (iargs [1], cfg);
			break;
		} else {
			g_assert_not_reached ();
		}
		break;
	}
#endif

	default:
		break;
	}
}

void
mono_inst_foreach (MonoInst *tree, MonoInstFunc func, gpointer data) {

	switch (mono_burg_arity [tree->opcode]) {
	case 0: break;
	case 1: 
		mono_inst_foreach (tree->inst_left, func, data);
		break;
	case 2: 
		mono_inst_foreach (tree->inst_left, func, data);
		mono_inst_foreach (tree->inst_right, func, data);
		break;
	default:
		g_assert_not_reached ();
	}
	func (tree, data);
}

G_GNUC_UNUSED
static void
mono_print_bb_code (MonoBasicBlock *bb)
{
	MonoInst *c;

	MONO_BB_FOR_EACH_INS (bb, c) {
		mono_print_tree (c);
		g_print ("\n");
	}
}

static void
print_dfn (MonoCompile *cfg) {
	int i, j;
	char *code;
	MonoBasicBlock *bb;
	MonoInst *c;

	g_print ("IR code for method %s\n", mono_method_full_name (cfg->method, TRUE));

	for (i = 0; i < cfg->num_bblocks; ++i) {
		bb = cfg->bblocks [i];
		/*if (bb->cil_code) {
			char* code1, *code2;
			code1 = mono_disasm_code_one (NULL, cfg->method, bb->cil_code, NULL);
			if (bb->last_ins->cil_code)
				code2 = mono_disasm_code_one (NULL, cfg->method, bb->last_ins->cil_code, NULL);
			else
				code2 = g_strdup ("");

			code1 [strlen (code1) - 1] = 0;
			code = g_strdup_printf ("%s -> %s", code1, code2);
			g_free (code1);
			g_free (code2);
		} else*/
			code = g_strdup ("\n");
		g_print ("\nBB%d (%d) (len: %d): %s", bb->block_num, i, bb->cil_length, code);
		MONO_BB_FOR_EACH_INS (bb, c) {
			if (cfg->new_ir) {
				mono_print_ins_index (-1, c);
			} else {
				mono_print_tree (c);
				g_print ("\n");
			}
		}

		g_print ("\tprev:");
		for (j = 0; j < bb->in_count; ++j) {
			g_print (" BB%d", bb->in_bb [j]->block_num);
		}
		g_print ("\t\tsucc:");
		for (j = 0; j < bb->out_count; ++j) {
			g_print (" BB%d", bb->out_bb [j]->block_num);
		}
		g_print ("\n\tidom: BB%d\n", bb->idom? bb->idom->block_num: -1);

		if (bb->idom)
			g_assert (mono_bitset_test_fast (bb->dominators, bb->idom->dfn));

		if (bb->dominators)
			mono_blockset_print (cfg, bb->dominators, "\tdominators", bb->idom? bb->idom->dfn: -1);
		if (bb->dfrontier)
			mono_blockset_print (cfg, bb->dfrontier, "\tdfrontier", -1);
		g_free (code);
	}

	g_print ("\n");
}

void
mono_bblock_add_inst (MonoBasicBlock *bb, MonoInst *inst)
{
	MONO_ADD_INS (bb, inst);
}

void
mono_bblock_insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert)
{
	if (ins == NULL) {
		ins = bb->code;
		bb->code = ins_to_insert;

		/* Link with next */
		ins_to_insert->next = ins;
		if (ins)
			ins->prev = ins_to_insert;

		if (bb->last_ins == NULL)
			bb->last_ins = ins_to_insert;
	} else {
		/* Link with next */
		ins_to_insert->next = ins->next;
		if (ins->next)
			ins->next->prev = ins_to_insert;

		/* Link with previous */
		ins->next = ins_to_insert;
		ins_to_insert->prev = ins;

		if (bb->last_ins == ins)
			bb->last_ins = ins_to_insert;
	}
}

void
mono_bblock_insert_before_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *ins_to_insert)
{
	if (ins == NULL) {
		NOT_IMPLEMENTED;
		ins = bb->code;
		bb->code = ins_to_insert;
		ins_to_insert->next = ins;
		if (bb->last_ins == NULL)
			bb->last_ins = ins_to_insert;
	} else {
		/* Link with previous */
		if (ins->prev)
			ins->prev->next = ins_to_insert;
		ins_to_insert->prev = ins->prev;

		/* Link with next */
		ins->prev = ins_to_insert;
		ins_to_insert->next = ins;

		if (bb->code == ins)
			bb->code = ins_to_insert;
	}
}

/*
 * mono_verify_bblock:
 *
 *   Verify that the next and prev pointers are consistent inside the instructions in BB.
 */
void
mono_verify_bblock (MonoBasicBlock *bb)
{
	MonoInst *ins, *prev;

	prev = NULL;
	for (ins = bb->code; ins; ins = ins->next) {
		g_assert (ins->prev == prev);
		prev = ins;
	}
	if (bb->last_ins)
		g_assert (!bb->last_ins->next);
}

/*
 * mono_verify_cfg:
 *
 *   Perform consistency checks on the JIT data structures and the IR
 */
void
mono_verify_cfg (MonoCompile *cfg)
{
	MonoBasicBlock *bb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		mono_verify_bblock (bb);
}

void
mono_destroy_compile (MonoCompile *cfg)
{
	//mono_mempool_stats (cfg->mempool);
	mono_free_loop_info (cfg);
	if (cfg->rs)
		mono_regstate_free (cfg->rs);
	if (cfg->spvars)
		g_hash_table_destroy (cfg->spvars);
	if (cfg->exvars)
		g_hash_table_destroy (cfg->exvars);
	mono_mempool_destroy (cfg->mempool);
	g_list_free (cfg->ldstr_list);
	g_hash_table_destroy (cfg->token_info_hash);
	if (cfg->abs_patches)
		g_hash_table_destroy (cfg->abs_patches);

	g_free (cfg->varinfo);
	g_free (cfg->vars);
	g_free (cfg->exception_message);
	g_free (cfg);
}

#ifdef HAVE_KW_THREAD
static __thread gpointer mono_lmf_addr MONO_TLS_FAST;
#ifdef MONO_ARCH_ENABLE_MONO_LMF_VAR
/* 
 * When this is defined, the current lmf is stored in this tls variable instead of in 
 * jit_tls->lmf.
 */
static __thread gpointer mono_lmf MONO_TLS_FAST;
#endif
#endif

guint32
mono_get_jit_tls_key (void)
{
	return mono_jit_tls_id;
}

gint32
mono_get_jit_tls_offset (void)
{
#ifdef HAVE_KW_THREAD
	int offset;
	MONO_THREAD_VAR_OFFSET (mono_jit_tls, offset);
	return offset;
#else
	return -1;
#endif
}

gint32
mono_get_lmf_tls_offset (void)
{
#if defined(HAVE_KW_THREAD) && defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
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
#if defined(HAVE_KW_THREAD) && defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	return mono_lmf;
#else
	MonoJitTlsData *jit_tls;

	if ((jit_tls = TlsGetValue (mono_jit_tls_id)))
		return jit_tls->lmf;

	g_assert_not_reached ();
	return NULL;
#endif
}

MonoLMF **
mono_get_lmf_addr (void)
{
#ifdef HAVE_KW_THREAD
	return mono_lmf_addr;
#else
	MonoJitTlsData *jit_tls;

	if ((jit_tls = TlsGetValue (mono_jit_tls_id)))
		return &jit_tls->lmf;

	g_assert_not_reached ();
	return NULL;
#endif
}

/* Called by native->managed wrappers */
void
mono_jit_thread_attach (MonoDomain *domain)
{
#ifdef HAVE_KW_THREAD
	if (!mono_lmf_addr) {
		mono_thread_attach (domain);
	}
#else
	if (!TlsGetValue (mono_jit_tls_id))
		mono_thread_attach (domain);
#endif
	if (mono_domain_get () != domain)
		mono_domain_set (domain, TRUE);
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
	/* MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id); */
	
	/* handle_remove should be eventually called for this thread, too
	g_free (jit_tls);*/

	if ((mono_runtime_unhandled_exception_policy_get () == MONO_UNHANDLED_POLICY_LEGACY) ||
			(obj->vtable->klass == mono_defaults.threadabortexception_class)) {
		mono_thread_exit ();
	} else {
		exit (mono_environment_exitcode_get ());
	}
}

static void*
setup_jit_tls_data (gpointer stack_start, gpointer abort_func)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	jit_tls = TlsGetValue (mono_jit_tls_id);
	if (jit_tls)
		return jit_tls;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	TlsSetValue (mono_jit_tls_id, jit_tls);

#ifdef HAVE_KW_THREAD
	mono_jit_tls = jit_tls;
#endif

	jit_tls->abort_func = abort_func;
	jit_tls->end_of_stack = stack_start;

	lmf = g_new0 (MonoLMF, 1);
#ifdef MONO_ARCH_INIT_TOP_LMF_ENTRY
	MONO_ARCH_INIT_TOP_LMF_ENTRY (lmf);
#else
	lmf->ebp = -1;
#endif

	jit_tls->first_lmf = lmf;

#if defined(HAVE_KW_THREAD) && defined(MONO_ARCH_ENABLE_MONO_LMF_VAR)
	/* jit_tls->lmf is unused */
	mono_lmf = lmf;
	mono_lmf_addr = &mono_lmf;
#else
#if defined(HAVE_KW_THREAD)
	mono_lmf_addr = &jit_tls->lmf;	
#endif

	jit_tls->lmf = lmf;
#endif

	mono_arch_setup_jit_tls_data (jit_tls);
	mono_setup_altstack (jit_tls);

	return jit_tls;
}

static void
mono_thread_start_cb (gsize tid, gpointer stack_start, gpointer func)
{
	MonoThread *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort);
	thread = mono_thread_current ();
	mono_debugger_thread_created (tid, thread, jit_tls);
	if (thread)
		thread->jit_data = jit_tls;
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
mono_thread_attach_cb (gsize tid, gpointer stack_start)
{
	MonoThread *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort_dummy);
	thread = mono_thread_current ();
	mono_debugger_thread_created (tid, thread, (MonoJitTlsData *) jit_tls);
	if (thread)
		thread->jit_data = jit_tls;
	if (mono_profiler_get_events () & MONO_PROFILE_STATISTICAL)
		setup_stat_profiler ();
}

static void
mini_thread_cleanup (MonoThread *thread)
{
	MonoJitTlsData *jit_tls = thread->jit_data;

	if (jit_tls) {
		mono_debugger_thread_cleanup (jit_tls);
		mono_arch_free_jit_tls_data (jit_tls);

		mono_free_altstack (jit_tls);
		g_free (jit_tls->first_lmf);
		g_free (jit_tls);
		thread->jit_data = NULL;
		if (thread == mono_thread_current ())
			TlsSetValue (mono_jit_tls_id, NULL);
	}
}

static MonoInst*
mono_create_tls_get (MonoCompile *cfg, int offset)
{
#ifdef MONO_ARCH_HAVE_TLS_GET
	MonoInst* ins;
	
	if (offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->dreg = cfg->new_ir ? mono_alloc_preg (cfg) : mono_regstate_next_int (cfg->rs);
	ins->inst_offset = offset;
	return ins;
#else
	return NULL;
#endif
}

MonoInst*
mono_get_jit_tls_intrinsic (MonoCompile *cfg)
{
	return mono_create_tls_get (cfg, mono_get_jit_tls_offset ());
}

void
mono_add_patch_info (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target)
{
	MonoJumpInfo *ji = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));

	ji->ip.i = ip;
	ji->type = type;
	ji->data.target = target;
	ji->next = cfg->patch_info;

	cfg->patch_info = ji;
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

void
mono_remove_patch_info (MonoCompile *cfg, int ip)
{
	MonoJumpInfo **ji = &cfg->patch_info;

	while (*ji) {
		if ((*ji)->ip.i == ip)
			*ji = (*ji)->next;
		else
			ji = &((*ji)->next);
	}
}

/**
 * mono_patch_info_dup_mp:
 *
 * Make a copy of PATCH_INFO, allocating memory from the mempool MP.
 */
MonoJumpInfo*
mono_patch_info_dup_mp (MonoMemPool *mp, MonoJumpInfo *patch_info)
{
	MonoJumpInfo *res = mono_mempool_alloc (mp, sizeof (MonoJumpInfo));
	memcpy (res, patch_info, sizeof (MonoJumpInfo));

	switch (patch_info->type) {
	case MONO_PATCH_INFO_RVA:
	case MONO_PATCH_INFO_LDSTR:
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_DECLSEC:
		res->data.token = mono_mempool_alloc (mp, sizeof (MonoJumpInfoToken));
		memcpy (res->data.token, patch_info->data.token, sizeof (MonoJumpInfoToken));
		break;
	case MONO_PATCH_INFO_SWITCH:
		res->data.table = mono_mempool_alloc (mp, sizeof (MonoJumpInfoBBTable));
		memcpy (res->data.table, patch_info->data.table, sizeof (MonoJumpInfoBBTable));
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH:
		res->data.rgctx_entry = mono_mempool_alloc (mp, sizeof (MonoJumpInfoRgctxEntry));
		memcpy (res->data.rgctx_entry, patch_info->data.rgctx_entry, sizeof (MonoJumpInfoRgctxEntry));
		res->data.rgctx_entry->data = mono_patch_info_dup_mp (mp, res->data.rgctx_entry->data);
		break;
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
	case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
	case MONO_PATCH_INFO_LDTOKEN:
	case MONO_PATCH_INFO_DECLSEC:
		return (ji->type << 8) | ji->data.token->token;
	case MONO_PATCH_INFO_VTABLE:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IID:
	case MONO_PATCH_INFO_ADJUSTED_IID:
	case MONO_PATCH_INFO_CLASS_INIT:
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_INTERNAL_METHOD:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_SFLDA:
		return (ji->type << 8) | (gssize)ji->data.target;
	default:
		return (ji->type << 8);
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
	default:
		if (ji1->data.target != ji2->data.target)
			return 0;
		break;
	}

	return 1;
}

gpointer
mono_resolve_patch_target (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors)
{
	unsigned char *ip = patch_info->ip.i + code;
	gconstpointer target = NULL;

	switch (patch_info->type) {
	case MONO_PATCH_INFO_BB:
		g_assert (patch_info->data.bb->native_offset);
		target = patch_info->data.bb->native_offset + code;
		break;
	case MONO_PATCH_INFO_ABS:
		target = patch_info->data.target;
		break;
	case MONO_PATCH_INFO_LABEL:
		target = patch_info->data.inst->inst_c0 + code;
		break;
	case MONO_PATCH_INFO_IP:
		target = ip;
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
	case MONO_PATCH_INFO_METHOD_JUMP:
		target = mono_create_jump_trampoline (domain, patch_info->data.method, FALSE);
		break;
	case MONO_PATCH_INFO_METHOD:
		if (patch_info->data.method == method) {
			target = code;
		} else {
			/* get the trampoline to the method from the domain */
			if (method && method->wrapper_type == MONO_WRAPPER_STATIC_RGCTX_INVOKE) {
				target = mono_create_jit_trampoline_in_domain (mono_domain_get (),
					patch_info->data.method);
			} else {
				target = mono_create_jit_trampoline (patch_info->data.method);
			}
		}
		break;
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *jump_table;
		int i;

		if (method && method->dynamic) {
			jump_table = mono_code_manager_reserve (mono_dynamic_code_hash_lookup (domain, method)->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
		} else {
			mono_domain_lock (domain);
			if (mono_aot_only)
				jump_table = mono_domain_alloc (domain, sizeof (gpointer) * patch_info->data.table->table_size);
			else
				jump_table = mono_code_manager_reserve (domain->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
			mono_domain_unlock (domain);
		}

		for (i = 0; i < patch_info->data.table->table_size; i++)
			jump_table [i] = code + GPOINTER_TO_INT (patch_info->data.table->table [i]);
		target = jump_table;
		break;
	}
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_CLASS:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_FIELD:
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
	case MONO_PATCH_INFO_CLASS_INIT: {
		MonoVTable *vtable = mono_class_vtable (domain, patch_info->data.klass);

		g_assert (vtable);
		target = mono_create_class_init_trampoline (vtable);
		break;
	}
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		target = mono_create_delegate_trampoline (patch_info->data.klass);
		break;
	case MONO_PATCH_INFO_SFLDA: {
		MonoVTable *vtable = mono_class_vtable (domain, patch_info->data.field->parent);

		g_assert (vtable);
		if (!vtable->initialized && !(vtable->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) && (method && mono_class_needs_cctor_run (vtable->klass, method)))
			/* Done by the generated code */
			;
		else {
			if (run_cctors)
				mono_runtime_class_init (vtable);
		}
		target = (char*)vtable->data + patch_info->data.field->offset;
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

		handle = mono_ldtoken (patch_info->data.token->image, 
							   patch_info->data.token->token, &handle_class, patch_info->data.token->has_context ? &patch_info->data.token->context : NULL);
		mono_class_init (handle_class);
		mono_class_init (mono_class_from_mono_type (handle));

		target =
			mono_type_get_object (domain, handle);
		break;
	}
	case MONO_PATCH_INFO_LDTOKEN: {
		gpointer handle;
		MonoClass *handle_class;
		
		handle = mono_ldtoken (patch_info->data.token->image,
				       patch_info->data.token->token, &handle_class, NULL);
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
			if (run_cctors) {
				target = mono_lookup_pinvoke_call (patch_info->data.method, NULL, NULL);
				if (!target)
					g_error ("Unable to resolve pinvoke method '%s' Re-run with MONO_LOG_LEVEL=debug for more information.\n", mono_method_full_name (patch_info->data.method, TRUE));
			} else {
				target = NULL;
			}
		} else {
			target = mono_lookup_internal_call (patch_info->data.method);

			if (!target && run_cctors)
				g_error ("Unregistered icall '%s'\n", mono_method_full_name (patch_info->data.method, TRUE));
		}
		break;
	case MONO_PATCH_INFO_JIT_ICALL_ADDR: {
		MonoJitICallInfo *mi = mono_find_jit_icall_by_name (patch_info->data.name);
		if (!mi) {
			g_warning ("unknown MONO_PATCH_INFO_JIT_ICALL_ADDR %s", patch_info->data.name);
			g_assert_not_reached ();
		}
		target = mi->func;
		break;
	}
	case MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG:
		target = mono_thread_interruption_request_flag ();
		break;
	case MONO_PATCH_INFO_METHOD_RGCTX:
		target = mono_method_lookup_rgctx (mono_class_vtable (domain, patch_info->data.method->klass), mini_method_get_context (patch_info->data.method)->method_inst);
		break;
	case MONO_PATCH_INFO_BB_OVF:
	case MONO_PATCH_INFO_EXC_OVF:
	case MONO_PATCH_INFO_GOT_OFFSET:
	case MONO_PATCH_INFO_NONE:
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH: {
		MonoJumpInfoRgctxEntry *entry = patch_info->data.rgctx_entry;
		guint32 slot = -1;

		switch (entry->data->type) {
		case MONO_PATCH_INFO_CLASS:
			slot = mono_method_lookup_or_register_other_info (entry->method, entry->in_mrgctx, &entry->data->data.klass->byval_arg, entry->info_type, mono_method_get_context (entry->method));
			break;
		case MONO_PATCH_INFO_METHOD:
		case MONO_PATCH_INFO_METHODCONST:
			slot = mono_method_lookup_or_register_other_info (entry->method, entry->in_mrgctx, entry->data->data.method, entry->info_type, mono_method_get_context (entry->method));
			break;
		case MONO_PATCH_INFO_FIELD:
			slot = mono_method_lookup_or_register_other_info (entry->method, entry->in_mrgctx, entry->data->data.field, entry->info_type, mono_method_get_context (entry->method));
			break;
		default:
			g_assert_not_reached ();
			break;
		}

		target = mono_create_rgctx_lazy_fetch_trampoline (slot);
		break;
	}
	case MONO_PATCH_INFO_GENERIC_CLASS_INIT:
		target = mono_create_generic_class_init_trampoline ();
		break;
	case MONO_PATCH_INFO_MONITOR_ENTER:
		target = mono_create_monitor_enter_trampoline ();
		break;
	case MONO_PATCH_INFO_MONITOR_EXIT:
		target = mono_create_monitor_exit_trampoline ();
		break;
	default:
		g_assert_not_reached ();
	}

	return (gpointer)target;
}

static void
dec_foreach (MonoInst *tree, MonoCompile *cfg) {
	MonoJitICallInfo *info;

	decompose_foreach (tree, cfg);

	switch (mono_burg_arity [tree->opcode]) {
	case 0: break;
	case 1: 
		dec_foreach (tree->inst_left, cfg);

		if ((info = mono_find_jit_opcode_emulation (tree->opcode))) {
			MonoInst *iargs [2];
		
			iargs [0] = tree->inst_left;

			mono_emulate_opcode (cfg, tree, iargs, info);
			return;
		}

		break;
	case 2:
#ifdef MONO_ARCH_BIGMUL_INTRINS
	       	if (tree->opcode == OP_LMUL
				&& (cfg->opt & MONO_OPT_INTRINS)
				&& (tree->inst_left->opcode == CEE_CONV_I8 
					|| tree->inst_left->opcode == CEE_CONV_U8)
				&& tree->inst_left->inst_left->type == STACK_I4
				&& (tree->inst_right->opcode == CEE_CONV_I8 
					|| tree->inst_right->opcode == CEE_CONV_U8)
				&& tree->inst_right->inst_left->type == STACK_I4
				&& tree->inst_left->opcode == tree->inst_right->opcode) {
			tree->opcode = (tree->inst_left->opcode == CEE_CONV_I8 ? OP_BIGMUL: OP_BIGMUL_UN);
			tree->inst_left = tree->inst_left->inst_left;
			tree->inst_right = tree->inst_right->inst_left;
			dec_foreach (tree, cfg);
		} else 
#endif
			if ((info = mono_find_jit_opcode_emulation (tree->opcode))) {
			MonoInst *iargs [2];
		
			iargs [0] = tree->inst_i0;
			iargs [1] = tree->inst_i1;
		
			mono_emulate_opcode (cfg, tree, iargs, info);

			dec_foreach (iargs [0], cfg);
			dec_foreach (iargs [1], cfg);
			return;
		} else {
			dec_foreach (tree->inst_left, cfg);
			dec_foreach (tree->inst_right, cfg);
		}
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
decompose_pass (MonoCompile *cfg) {
	MonoBasicBlock *bb;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *tree;
		cfg->cbb = bb;
		cfg->prev_ins = NULL;
		MONO_BB_FOR_EACH_INS (cfg->cbb, tree) {
			dec_foreach (tree, cfg);
			cfg->prev_ins = tree;
		}
	}
}

static void
mono_compile_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i;

	header = mono_method_get_header (cfg->method);

	sig = mono_method_signature (cfg->method);
	
	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		if (cfg->new_ir) {
			cfg->ret = mono_compile_create_var (cfg, sig->ret, OP_ARG);
			/* Inhibit optimizations */
			cfg->ret->flags |= MONO_INST_VOLATILE;
		} else {
			cfg->ret = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
			cfg->ret->opcode = OP_RETARG;
			cfg->ret->inst_vtype = sig->ret;
			cfg->ret->klass = mono_class_from_mono_type (sig->ret);
		}
	}
	if (cfg->verbose_level > 2)
		g_print ("creating vars\n");

	cfg->args = mono_mempool_alloc0 (cfg->mempool, (sig->param_count + sig->hasthis) * sizeof (MonoInst*));

	if (sig->hasthis)
		cfg->args [0] = mono_compile_create_var (cfg, &cfg->method->klass->this_arg, OP_ARG);

	for (i = 0; i < sig->param_count; ++i) {
		cfg->args [i + sig->hasthis] = mono_compile_create_var (cfg, sig->params [i], OP_ARG);
		if (sig->params [i]->byref) {
			if (!cfg->new_ir) cfg->disable_ssa = TRUE;
		}
	}

	if (cfg->new_ir && cfg->verbose_level > 2) {
		if (cfg->ret) {
			printf ("\treturn : ");
			mono_print_ins (cfg->ret);
		}

		if (sig->hasthis) {
			printf ("\tthis: ");
			mono_print_ins (cfg->args [0]);
		}

		for (i = 0; i < sig->param_count; ++i) {
			printf ("\targ [%d]: ", i);
			mono_print_ins (cfg->args [i + sig->hasthis]);
		}
	}

	cfg->locals_start = cfg->num_varinfo;
	cfg->locals = mono_mempool_alloc0 (cfg->mempool, header->num_locals * sizeof (MonoInst*));

	if (cfg->verbose_level > 2)
		g_print ("creating locals\n");

	for (i = 0; i < header->num_locals; ++i)
		cfg->locals [i] = mono_compile_create_var (cfg, header->locals [i], OP_LOCAL);

	if (cfg->verbose_level > 2)
		g_print ("locals done\n");

	mono_arch_create_vars (cfg);
}

void
mono_print_code (MonoCompile *cfg, const char* msg)
{
	MonoBasicBlock *bb;
	
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *tree = bb->code;	

		if (cfg->new_ir) {
			mono_print_bb (bb, msg);
		} else {
			if (!tree)
				continue;
			
			g_print ("%s CODE BLOCK %d (nesting %d):\n", msg, bb->block_num, bb->nesting);

			MONO_BB_FOR_EACH_INS (bb, tree) {
				mono_print_tree (tree);
				g_print ("\n");
			}
		}
	}
}

#ifndef DISABLE_JIT

extern const char * const mono_burg_rule_string [];

static void
emit_state (MonoCompile *cfg, MBState *state, int goal)
{
	MBState *kids [10];
	int ern = mono_burg_rule (state, goal);
	const guint16 *nts = mono_burg_nts_data + mono_burg_nts [ern];

	//g_print ("rule: %s\n", mono_burg_rule_string [ern]);
	switch (goal) {
	case MB_NTERM_reg:
		//if (state->reg2)
		//	state->reg1 = state->reg2; /* chain rule */
		//else
#ifdef MONO_ARCH_ENABLE_EMIT_STATE_OPT
		if (!state->reg1)
#endif
			state->reg1 = mono_regstate_next_int (cfg->rs);
		//g_print ("alloc symbolic R%d (reg2: R%d) in block %d\n", state->reg1, state->reg2, cfg->cbb->block_num);
		break;
	case MB_NTERM_lreg:
		state->reg1 = mono_regstate_next_int (cfg->rs);
		state->reg2 = mono_regstate_next_int (cfg->rs);
		break;
	case MB_NTERM_freg:
#ifdef MONO_ARCH_SOFT_FLOAT
		state->reg1 = mono_regstate_next_int (cfg->rs);
		state->reg2 = mono_regstate_next_int (cfg->rs);
#else
		state->reg1 = mono_regstate_next_float (cfg->rs);
#endif
		break;
	default:
#ifdef MONO_ARCH_ENABLE_EMIT_STATE_OPT
		/*
		 * Enabling this might cause bugs to surface in the local register
		 * allocators on some architectures like x86.
		 */
		if ((state->tree->ssa_op == MONO_SSA_STORE) && (state->left->tree->opcode == OP_REGVAR)) {
			/* Do not optimize away reg-reg moves */
			if (! ((state->right->tree->ssa_op == MONO_SSA_LOAD) && (state->right->left->tree->opcode == OP_REGVAR))) {
				state->right->reg1 = state->left->tree->dreg;
			}
		}
#endif

		/* do nothing */
		break;
	}
	if (nts [0]) {
		mono_burg_kids (state, ern, kids);

		emit_state (cfg, kids [0], nts [0]);
		if (nts [1]) {
			emit_state (cfg, kids [1], nts [1]);
			if (nts [2]) {
				emit_state (cfg, kids [2], nts [2]);
				if (nts [3]) {
					g_assert (!nts [4]);
					emit_state (cfg, kids [3], nts [3]);
				}
			}
		}
	}

//	g_print ("emit: %s (%p)\n", mono_burg_rule_string [ern], state);
	mono_burg_emit (ern, state, state->tree, cfg);
}

#define DEBUG_SELECTION

static void 
mini_select_instructions (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	
	cfg->state_pool = mono_mempool_new ();
	cfg->rs = mono_regstate_new ();

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (bb->last_ins && MONO_IS_COND_BRANCH_OP (bb->last_ins) &&
		    bb->last_ins->inst_false_bb && bb->next_bb != bb->last_ins->inst_false_bb) {

			/* we are careful when inverting, since bugs like #59580
			 * could show up when dealing with NaNs.
			 */
			if (MONO_IS_COND_BRANCH_NOFP(bb->last_ins) && bb->next_bb == bb->last_ins->inst_true_bb) {
				MonoBasicBlock *tmp =  bb->last_ins->inst_true_bb;
				bb->last_ins->inst_true_bb = bb->last_ins->inst_false_bb;
				bb->last_ins->inst_false_bb = tmp;

				bb->last_ins->opcode = mono_reverse_branch_op (bb->last_ins->opcode);
			} else {			
				MonoInst *ins;

				MONO_INST_NEW (cfg, ins, OP_BR);
				ins->inst_target_bb = bb->last_ins->inst_false_bb;
				MONO_ADD_INS (bb, ins);
			}
		}
	}

#ifdef DEBUG_SELECTION
	if (cfg->verbose_level >= 4) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *tree;
			g_print ("DUMP BLOCK %d:\n", bb->block_num);
			MONO_BB_FOR_EACH_INS (bb, tree) {
				mono_print_tree (tree);
				g_print ("\n");
			}
		}
	}
#endif

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *tree = bb->code, *next;	
		MBState *mbstate;

		if (!tree)
			continue;
		bb->code = NULL;
		bb->last_ins = NULL;
		
		cfg->cbb = bb;
		mono_regstate_reset (cfg->rs);

#ifdef DEBUG_SELECTION
		if (cfg->verbose_level >= 3)
			g_print ("LABEL BLOCK %d:\n", bb->block_num);
#endif
		for (; tree; tree = next) {
			next = tree->next;
#ifdef DEBUG_SELECTION
			if (cfg->verbose_level >= 3) {
				mono_print_tree (tree);
				g_print ("\n");
			}
#endif

			cfg->ip = tree->cil_code;
			if (!(mbstate = mono_burg_label (tree, cfg))) {
				g_warning ("unable to label tree %p", tree);
				mono_print_tree (tree);
				g_print ("\n");				
				g_assert_not_reached ();
			}
			emit_state (cfg, mbstate, MB_NTERM_stmt);
		}
		bb->max_vreg = cfg->rs->next_vreg;

		if (bb->last_ins)
			bb->last_ins->next = NULL;

		mono_mempool_empty (cfg->state_pool); 
	}
	mono_mempool_destroy (cfg->state_pool); 

	cfg->ip = NULL;
}

/*
 * mono_normalize_opcodes:
 *
 *   Replace CEE_ and OP_ opcodes with the corresponding OP_I or OP_L opcodes.
 */

static gint16 *remap_table;

#if SIZEOF_VOID_P == 8
#define REMAP_OPCODE(opcode) OP_L ## opcode
#else
#define REMAP_OPCODE(opcode) OP_I ## opcode
#endif

static G_GNUC_UNUSED void
mono_normalize_opcodes (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;

	if (!remap_table) {
		remap_table = g_new0 (gint16, OP_LAST);

#if SIZEOF_VOID_P == 8
		remap_table [CEE_CONV_U8] = OP_ZEXT_I4;
		remap_table [CEE_CONV_U] = OP_ZEXT_I4;
		remap_table [CEE_CONV_I8] = OP_SEXT_I4;
		remap_table [CEE_CONV_I] = OP_SEXT_I4;
		remap_table [CEE_CONV_OVF_U4] = OP_LCONV_TO_OVF_U4;
		remap_table [CEE_CONV_OVF_I4_UN] = OP_LCONV_TO_OVF_I4_UN;
#else
#endif
		remap_table [CEE_CONV_R4] = OP_ICONV_TO_R4;
		remap_table [CEE_CONV_R8] = OP_ICONV_TO_R8;
		remap_table [CEE_CONV_I4] = OP_MOVE;
		remap_table [CEE_CONV_U4] = OP_MOVE;
		remap_table [CEE_CONV_I1] = REMAP_OPCODE (CONV_TO_I1);
		remap_table [CEE_CONV_I2] = REMAP_OPCODE (CONV_TO_I2);
		remap_table [CEE_CONV_U1] = REMAP_OPCODE (CONV_TO_U1);
		remap_table [CEE_CONV_U2] = REMAP_OPCODE (CONV_TO_U2);
		remap_table [CEE_CONV_R_UN] = REMAP_OPCODE (CONV_TO_R_UN);
		remap_table [CEE_ADD] = REMAP_OPCODE (ADD);
		remap_table [CEE_SUB] = REMAP_OPCODE (SUB);
		remap_table [CEE_MUL] = REMAP_OPCODE (MUL);
		remap_table [CEE_DIV] = REMAP_OPCODE (DIV);
		remap_table [CEE_REM] = REMAP_OPCODE (REM);
		remap_table [CEE_DIV_UN] = REMAP_OPCODE (DIV_UN);
		remap_table [CEE_REM_UN] = REMAP_OPCODE (REM_UN);
		remap_table [CEE_AND] = REMAP_OPCODE (AND);
		remap_table [CEE_OR] = REMAP_OPCODE (OR);
		remap_table [CEE_XOR] = REMAP_OPCODE (XOR);
		remap_table [CEE_SHL] = REMAP_OPCODE (SHL);
		remap_table [CEE_SHR] = REMAP_OPCODE (SHR);
		remap_table [CEE_SHR_UN] = REMAP_OPCODE (SHR_UN);
		remap_table [CEE_NOT] = REMAP_OPCODE (NOT);
		remap_table [CEE_NEG] = REMAP_OPCODE (NEG);
		remap_table [CEE_CALL] = OP_CALL;
		remap_table [CEE_BEQ] = REMAP_OPCODE (BEQ);
		remap_table [CEE_BNE_UN] = REMAP_OPCODE (BNE_UN);
		remap_table [CEE_BLT] = REMAP_OPCODE (BLT);
		remap_table [CEE_BLT_UN] = REMAP_OPCODE (BLT_UN);
		remap_table [CEE_BGT] = REMAP_OPCODE (BGT);
		remap_table [CEE_BGT_UN] = REMAP_OPCODE (BGT_UN);
		remap_table [CEE_BGE] = REMAP_OPCODE (BGE);
		remap_table [CEE_BGE_UN] = REMAP_OPCODE (BGE_UN);
		remap_table [CEE_BLE] = REMAP_OPCODE (BLE);
		remap_table [CEE_BLE_UN] = REMAP_OPCODE (BLE_UN);
		remap_table [CEE_ADD_OVF] = REMAP_OPCODE (ADD_OVF);
		remap_table [CEE_ADD_OVF_UN] = REMAP_OPCODE (ADD_OVF_UN);
		remap_table [CEE_SUB_OVF] = REMAP_OPCODE (SUB_OVF);
		remap_table [CEE_SUB_OVF_UN] = REMAP_OPCODE (SUB_OVF_UN);
		remap_table [CEE_MUL_OVF] = REMAP_OPCODE (MUL_OVF);
		remap_table [CEE_MUL_OVF_UN] = REMAP_OPCODE (MUL_OVF_UN);
	}

	MONO_BB_FOR_EACH_INS (bb, ins) {
		int remapped = remap_table [ins->opcode];
		if (remapped)
			ins->opcode = remapped;
	}
}

void
mono_codegen (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	MonoBasicBlock *bb;
	int i, max_epilog_size;
	guint8 *code;

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		cfg->spill_count = 0;
		/* we reuse dfn here */
		/* bb->dfn = bb_count++; */
#ifdef MONO_ARCH_ENABLE_NORMALIZE_OPCODES
		if (!cfg->new_ir)
			mono_normalize_opcodes (cfg, bb);
#endif

		mono_arch_lowering_pass (cfg, bb);

		if (cfg->opt & MONO_OPT_PEEPHOLE)
			mono_arch_peephole_pass_1 (cfg, bb);

		if (!cfg->globalra)
			mono_local_regalloc (cfg, bb);

		if (cfg->opt & MONO_OPT_PEEPHOLE)
			mono_arch_peephole_pass_2 (cfg, bb);
	}

	if (cfg->prof_options & MONO_PROFILE_COVERAGE)
		cfg->coverage_info = mono_profiler_coverage_alloc (cfg->method, cfg->num_bblocks);

	code = mono_arch_emit_prolog (cfg);

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		code = mono_arch_instrument_prolog (cfg, mono_profiler_method_enter, code, FALSE);

	cfg->code_len = code - cfg->native_code;
	cfg->prolog_end = cfg->code_len;

	mono_debug_open_method (cfg);

	/* emit code all basic blocks */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		bb->native_offset = cfg->code_len;
		//if ((bb == cfg->bb_entry) || !(bb->region == -1 && !bb->dfn))
			mono_arch_output_basic_block (cfg, bb);

		if (bb == cfg->bb_exit) {
			cfg->epilog_begin = cfg->code_len;

			if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE) {
				code = cfg->native_code + cfg->code_len;
				code = mono_arch_instrument_epilog (cfg, mono_profiler_method_leave, code, FALSE);
				cfg->code_len = code - cfg->native_code;
				g_assert (cfg->code_len < cfg->code_size);
			}

			mono_arch_emit_epilog (cfg);
		}
	}

	mono_arch_emit_exceptions (cfg);

	max_epilog_size = 0;

	code = cfg->native_code + cfg->code_len;

	/* we always allocate code in cfg->domain->code_mp to increase locality */
	cfg->code_size = cfg->code_len + max_epilog_size;
	/* fixme: align to MONO_ARCH_CODE_ALIGNMENT */

	if (cfg->method->dynamic) {
		guint unwindlen = 0;
#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
		unwindlen = mono_arch_unwindinfo_get_size (cfg->arch.unwindinfo);
#endif
		/* Allocate the code into a separate memory pool so it can be freed */
		cfg->dynamic_info = g_new0 (MonoJitDynamicMethodInfo, 1);
		cfg->dynamic_info->code_mp = mono_code_manager_new_dynamic ();
		mono_domain_lock (cfg->domain);
		mono_dynamic_code_hash_insert (cfg->domain, cfg->method, cfg->dynamic_info);
		mono_domain_unlock (cfg->domain);

		code = mono_code_manager_reserve (cfg->dynamic_info->code_mp, cfg->code_size + unwindlen);
	} else {
		guint unwindlen = 0;
#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
		unwindlen = mono_arch_unwindinfo_get_size (cfg->arch.unwindinfo);
#endif
		mono_domain_lock (cfg->domain);
		code = mono_code_manager_reserve (cfg->domain->code_mp, cfg->code_size + unwindlen);
		mono_domain_unlock (cfg->domain);
	}

	memcpy (code, cfg->native_code, cfg->code_len);
	g_free (cfg->native_code);
	cfg->native_code = code;
	code = cfg->native_code + cfg->code_len;
  
	/* g_assert (((int)cfg->native_code & (MONO_ARCH_CODE_ALIGNMENT - 1)) == 0); */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_ABS: {
			MonoJitICallInfo *info = mono_find_jit_icall_by_addr (patch_info->data.target);

			/*
			 * Change patches of type MONO_PATCH_INFO_ABS into patches describing the 
			 * absolute address.
			 */
			if (info) {
				//printf ("TEST %s %p\n", info->name, patch_info->data.target);
				// FIXME: CLEAN UP THIS MESS.
				if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && 
					strstr (cfg->method->name, info->name)) {
					/*
					 * This is an icall wrapper, and this is a call to the
					 * wrapped function.
					 */
					if (cfg->compile_aot) {
						patch_info->type = MONO_PATCH_INFO_JIT_ICALL_ADDR;
						patch_info->data.name = info->name;
					}
				} else {
					/* for these array methods we currently register the same function pointer
					 * since it's a vararg function. But this means that mono_find_jit_icall_by_addr ()
					 * will return the incorrect one depending on the order they are registered.
					 * See tests/test-arr.cs
					 */
					if (strstr (info->name, "ves_array_new_va_") == NULL && strstr (info->name, "ves_array_element_address_") == NULL) {
						patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
						patch_info->data.name = info->name;
					}
				}
			}
			
			if (patch_info->type == MONO_PATCH_INFO_ABS && !cfg->new_ir) {
				MonoVTable *vtable = mono_find_class_init_trampoline_by_addr (patch_info->data.target);
				if (vtable) {
					patch_info->type = MONO_PATCH_INFO_CLASS_INIT;
					patch_info->data.klass = vtable->klass;
				}
			}

			if (patch_info->type == MONO_PATCH_INFO_ABS) {
				MonoClass *klass = mono_find_delegate_trampoline_by_addr (patch_info->data.target);
				if (klass) {
					patch_info->type = MONO_PATCH_INFO_DELEGATE_TRAMPOLINE;
					patch_info->data.klass = klass;
				}
			}

			if (patch_info->type == MONO_PATCH_INFO_ABS) {
				if (cfg->abs_patches) {
					MonoJumpInfo *abs_ji = g_hash_table_lookup (cfg->abs_patches, patch_info->data.target);
					if (abs_ji) {
						patch_info->type = abs_ji->type;
						patch_info->data.target = abs_ji->data.target;
					}
				}
			}

			break;
		}
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table;
			if (cfg->method->dynamic) {
				table = mono_code_manager_reserve (cfg->dynamic_info->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
			} else {
				mono_domain_lock (cfg->domain);
				table = mono_code_manager_reserve (cfg->domain->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
				mono_domain_unlock (cfg->domain);
			}

			if (!cfg->compile_aot && !cfg->new_ir)
				/* In the aot case, the patch already points to the correct location */
				patch_info->ip.i = patch_info->ip.label->inst_c0;
			for (i = 0; i < patch_info->data.table->table_size; i++) {
				/* Might be NULL if the switch is eliminated */
				if (patch_info->data.table->table [i]) {
					g_assert (patch_info->data.table->table [i]->native_offset);
					table [i] = GINT_TO_POINTER (patch_info->data.table->table [i]->native_offset);
				} else {
					table [i] = NULL;
				}
			}
			patch_info->data.table->table = (MonoBasicBlock**)table;
			break;
		}
		case MONO_PATCH_INFO_METHOD_JUMP: {
			GSList *list;
			MonoDomain *domain = cfg->domain;
			unsigned char *ip = cfg->native_code + patch_info->ip.i;

			mono_domain_lock (domain);
			if (!domain_jit_info (domain)->jump_target_hash)
				domain_jit_info (domain)->jump_target_hash = g_hash_table_new (NULL, NULL);
			list = g_hash_table_lookup (domain_jit_info (domain)->jump_target_hash, patch_info->data.method);
			list = g_slist_prepend (list, ip);
			g_hash_table_insert (domain_jit_info (domain)->jump_target_hash, patch_info->data.method, list);
			mono_domain_unlock (domain);
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}

#ifdef VALGRIND_JIT_REGISTER_MAP
if (valgrind_register){
		char* nm = mono_method_full_name (cfg->method, TRUE);
		VALGRIND_JIT_REGISTER_MAP (nm, cfg->native_code, cfg->native_code + cfg->code_len);
		g_free (nm);
	}
#endif
 
	if (cfg->verbose_level > 0) {
		char* nm = mono_method_full_name (cfg->method, TRUE);
		g_print ("Method %s emitted at %p to %p (code length %d) [%s]\n", 
				 nm, 
				 cfg->native_code, cfg->native_code + cfg->code_len, cfg->code_len, cfg->domain->friendly_name);
		g_free (nm);
	}

	{
		gboolean is_generic = FALSE;

		if (cfg->method->is_inflated || mono_method_get_generic_container (cfg->method) ||
				cfg->method->klass->generic_container || cfg->method->klass->generic_class) {
			is_generic = TRUE;
		}

		if (cfg->generic_sharing_context)
			g_assert (is_generic);
	}

#ifdef MONO_ARCH_HAVE_SAVE_UNWIND_INFO
	mono_arch_save_unwind_info (cfg);
#endif
	
	mono_arch_patch_code (cfg->method, cfg->domain, cfg->native_code, cfg->patch_info, cfg->run_cctors);

	if (cfg->method->dynamic) {
		mono_code_manager_commit (cfg->dynamic_info->code_mp, cfg->native_code, cfg->code_size, cfg->code_len);
	} else {
		mono_domain_lock (cfg->domain);
		mono_code_manager_commit (cfg->domain->code_mp, cfg->native_code, cfg->code_size, cfg->code_len);
		mono_domain_unlock (cfg->domain);
	}
	
	mono_arch_flush_icache (cfg->native_code, cfg->code_len);

	mono_debug_close_method (cfg);
#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
	mono_arch_unwindinfo_install_unwind_info (&cfg->arch.unwindinfo, cfg->native_code, cfg->code_len);
#endif
}

static MonoGenericInst*
get_object_generic_inst (int type_argc)
{
	MonoType **type_argv;
	int i;

	type_argv = alloca (sizeof (MonoType*) * type_argc);

	for (i = 0; i < type_argc; ++i)
		type_argv [i] = &mono_defaults.object_class->byval_arg;

	return mono_metadata_get_generic_inst (type_argc, type_argv);
}

/*
 * mini_method_compile:
 * @method: the method to compile
 * @opts: the optimization flags to use
 * @domain: the domain where the method will be compiled in
 * @run_cctors: whether we should run type ctors if possible
 * @compile_aot: whether this is an AOT compilation
 * @parts: debug flag
 *
 * Returns: a MonoCompile* pointer. Caller must check the exception_type
 * field in the returned struct to see if compilation succeded.
 */
MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, gboolean run_cctors, gboolean compile_aot, int parts)
{
	MonoMethodHeader *header;
	guint8 *ip;
	MonoCompile *cfg;
	MonoJitInfo *jinfo;
	int dfn, i, code_size_ratio;
	gboolean deadce_has_run = FALSE;
	gboolean try_generic_shared;
	MonoMethod *method_to_compile, *method_to_register;
	int generic_info_size;

	mono_jit_stats.methods_compiled++;
	if (mono_profiler_get_events () & MONO_PROFILE_JIT_COMPILATION)
		mono_profiler_method_jit (method);
	if (MONO_PROBE_METHOD_COMPILE_BEGIN_ENABLED ())
		MONO_PROBE_METHOD_COMPILE_BEGIN (method);
 
	if (compile_aot)
		/* We are passed the original generic method definition */
		try_generic_shared = mono_class_generic_sharing_enabled (method->klass) &&
			(opts & MONO_OPT_GSHARED) && (method->is_generic || method->klass->generic_container);
	else
		try_generic_shared = mono_class_generic_sharing_enabled (method->klass) &&
			(opts & MONO_OPT_GSHARED) && mono_method_is_generic_sharable_impl (method, FALSE);

	if (opts & MONO_OPT_GSHARED) {
		if (try_generic_shared)
			mono_stats.generics_sharable_methods++;
		else if (mono_method_is_generic_impl (method))
			mono_stats.generics_unsharable_methods++;
	}

 restart_compile:
	if (try_generic_shared) {
		MonoMethod *declaring_method;
		MonoGenericContext *shared_context;

		if (compile_aot) {
			declaring_method = method;
		} else {
			declaring_method = mono_method_get_declaring_generic_method (method);
			if (method->klass->generic_class)
				g_assert (method->klass->generic_class->container_class == declaring_method->klass);
			else
				g_assert (method->klass == declaring_method->klass);
		}

		if (declaring_method->is_generic)
			shared_context = &(mono_method_get_generic_container (declaring_method)->context);
		else
			shared_context = &declaring_method->klass->generic_container->context;

		method_to_compile = mono_class_inflate_generic_method (declaring_method, shared_context);
		g_assert (method_to_compile);
	} else {
		method_to_compile = method;
	}

	cfg = g_new0 (MonoCompile, 1);
	cfg->method = method_to_compile;
	cfg->mempool = mono_mempool_new ();
	cfg->opt = opts;
	cfg->prof_options = mono_profiler_get_events ();
	cfg->run_cctors = run_cctors;
	cfg->domain = domain;
	cfg->verbose_level = mini_verbose;
	cfg->compile_aot = compile_aot;
	cfg->skip_visibility = method->skip_visibility;
	cfg->orig_method = method;
	if (try_generic_shared)
		cfg->generic_sharing_context = (MonoGenericSharingContext*)&cfg->generic_sharing_context;
	cfg->token_info_hash = g_hash_table_new (NULL, NULL);

	if (cfg->compile_aot && !try_generic_shared && (method->is_generic || method->klass->generic_container)) {
		cfg->exception_type = MONO_EXCEPTION_GENERIC_SHARING_FAILED;
		return cfg;
	}

	/* The debugger has no liveness information, so avoid sharing registers/stack slots */
	if (mono_debug_using_mono_debugger () || debug_options.mdb_optimizations) {
		cfg->disable_reuse_registers = TRUE;
		cfg->disable_reuse_stack_slots = TRUE;
		/* 
		 * This decreases the change the debugger will read registers/stack slots which are
		 * not yet initialized.
		 */
		cfg->disable_initlocals_opt = TRUE;

		/* Temporarily disable this when running in the debugger until we have support
		 * for this in the debugger. */
		cfg->disable_omit_fp = TRUE;

		/* The debugger needs all locals to be on the stack or in a global register */
		cfg->disable_vreg_to_lvreg = TRUE;

		// cfg->opt |= MONO_OPT_SHARED;
		cfg->opt &= ~MONO_OPT_DEADCE;
		cfg->opt &= ~MONO_OPT_INLINE;
		cfg->opt &= ~MONO_OPT_COPYPROP;
		cfg->opt &= ~MONO_OPT_CONSPROP;
		cfg->opt &= ~MONO_OPT_GSHARED;
	}

	header = mono_method_get_header (method_to_compile);
	if (!header) {
		cfg->exception_type = MONO_EXCEPTION_INVALID_PROGRAM;
		cfg->exception_message = g_strdup_printf ("Missing or incorrect header for method %s", cfg->method->name);
		if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		if (cfg->prof_options & MONO_PROFILE_JIT_COMPILATION)
			mono_profiler_method_end_jit (method, NULL, MONO_PROFILE_FAILED);
		return cfg;
	}

	if (getenv ("MONO_VERBOSE_METHOD")) {
		if (strcmp (cfg->method->name, getenv ("MONO_VERBOSE_METHOD")) == 0)
			cfg->verbose_level = 4;
	}

	ip = (guint8 *)header->code;

	cfg->intvars = mono_mempool_alloc0 (cfg->mempool, sizeof (guint16) * STACK_MAX * header->max_stack);

	if (cfg->verbose_level > 2) {
		if (cfg->generic_sharing_context)
			g_print ("converting shared method %s\n", mono_method_full_name (method, TRUE));
		else
			g_print ("converting method %s\n", mono_method_full_name (method, TRUE));
	}

	if (cfg->opt & (MONO_OPT_ABCREM | MONO_OPT_SSAPRE))
		cfg->opt |= MONO_OPT_SSA;

	{
		static int count = 0;

		count ++;

		if (getenv ("MONO_COUNT")) {
			if (count == atoi (getenv ("MONO_COUNT"))) {
				printf ("LAST: %s\n", mono_method_full_name (method, TRUE));
				//cfg->verbose_level = 5;
			}
			if (count <= atoi (getenv ("MONO_COUNT")))
				cfg->new_ir = TRUE;

			/*
			 * Passing/returning vtypes in registers in managed methods is an ABI change 
			 * from the old JIT.
			 */
			disable_vtypes_in_regs = TRUE;
		}
		else
			cfg->new_ir = TRUE;
	}

	/* 
	if ((cfg->method->klass->image != mono_defaults.corlib) || (strstr (cfg->method->klass->name, "StackOverflowException") && strstr (cfg->method->name, ".ctor")) || (strstr (cfg->method->klass->name, "OutOfMemoryException") && strstr (cfg->method->name, ".ctor")))
		cfg->globalra = TRUE;
	*/

	//cfg->globalra = TRUE;

	//if (!strcmp (cfg->method->klass->name, "Tests") && !cfg->method->wrapper_type)
	//	cfg->globalra = TRUE;

	{
		static int count = 0;
		count ++;

		if (getenv ("COUNT2")) {
			if (count == atoi (getenv ("COUNT2")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (getenv ("COUNT2")))
				cfg->globalra = FALSE;
		}
	}

	if (header->clauses)
		cfg->globalra = FALSE;

	if (cfg->method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED)
		/* The code in the prolog clobbers caller saved registers */
		cfg->globalra = FALSE;

	// FIXME: Disable globalra in case of tracing/profiling

	if (cfg->method->save_lmf)
		/* The LMF saving code might clobber caller saved registers */
		cfg->globalra = FALSE;

	// FIXME:
	if (!strcmp (cfg->method->name, "CompareInternal"))
		cfg->globalra = FALSE;

	/*
	if (strstr (cfg->method->name, "LoadData"))
		cfg->new_ir = FALSE;
	*/

	if (cfg->new_ir) {
		cfg->rs = mono_regstate_new ();
		cfg->next_vreg = cfg->rs->next_vreg;
	}

	/* FIXME: Fix SSA to handle branches inside bblocks */
	if (cfg->opt & MONO_OPT_SSA)
		cfg->enable_extended_bblocks = FALSE;

	/*
	 * FIXME: This confuses liveness analysis because variables which are assigned after
	 * a branch inside a bblock become part of the kill set, even though the assignment
	 * might not get executed. This causes the optimize_initlocals pass to delete some
	 * assignments which are needed.
	 * Also, the mono_if_conversion pass needs to be modified to recognize the code
	 * created by this.
	 */
	//cfg->enable_extended_bblocks = TRUE;

	/*
	 * create MonoInst* which represents arguments and local variables
	 */
	mono_compile_create_vars (cfg);

	if (cfg->new_ir) {
		/* SSAPRE is not supported on linear IR */
		cfg->opt &= ~MONO_OPT_SSAPRE;

		i = mono_method_to_ir2 (cfg, method_to_compile, NULL, NULL, NULL, NULL, NULL, 0, FALSE);
	}
	else {
		i = mono_method_to_ir (cfg, method_to_compile, NULL, NULL, cfg->locals_start, NULL, NULL, NULL, 0, FALSE);
	}

	if (i < 0) {
		if (try_generic_shared && cfg->exception_type == MONO_EXCEPTION_GENERIC_SHARING_FAILED) {
			if (compile_aot) {
				if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
					MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
				return cfg;
			}
			mono_destroy_compile (cfg);
			try_generic_shared = FALSE;
			goto restart_compile;
		}
		g_assert (cfg->exception_type != MONO_EXCEPTION_GENERIC_SHARING_FAILED);

		if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		if (cfg->prof_options & MONO_PROFILE_JIT_COMPILATION)
			mono_profiler_method_end_jit (method, NULL, MONO_PROFILE_FAILED);
		/* cfg contains the details of the failure, so let the caller cleanup */
		return cfg;
	}

	mono_jit_stats.basic_blocks += cfg->num_bblocks;
	mono_jit_stats.max_basic_blocks = MAX (cfg->num_bblocks, mono_jit_stats.max_basic_blocks);

	/*g_print ("numblocks = %d\n", cfg->num_bblocks);*/

	if (cfg->new_ir) {
		mono_decompose_long_opts (cfg);

		/* Should be done before branch opts */
		if (cfg->opt & (MONO_OPT_CONSPROP | MONO_OPT_COPYPROP))
			mono_local_cprop2 (cfg);
	}

	if (cfg->opt & MONO_OPT_BRANCH)
		mono_optimize_branches (cfg);

	if (cfg->new_ir) {
		/* This must be done _before_ global reg alloc and _after_ decompose */
		mono_handle_global_vregs (cfg);
	if (cfg->opt & MONO_OPT_DEADCE)
		mono_local_deadce (cfg);
		mono_if_conversion (cfg);
	}

	if ((cfg->opt & MONO_OPT_SSAPRE) || cfg->globalra)
		mono_remove_critical_edges (cfg);

	/* Depth-first ordering on basic blocks */
	cfg->bblocks = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (cfg->num_bblocks + 1));

	dfn = 0;
	df_visit (cfg->bb_entry, &dfn, cfg->bblocks);
	if (cfg->num_bblocks != dfn + 1) {
		MonoBasicBlock *bb;

		cfg->num_bblocks = dfn + 1;

		/* remove unreachable code, because the code in them may be 
		 * inconsistent  (access to dead variables for example) */
		for (bb = cfg->bb_entry; bb;) {
			MonoBasicBlock *bbn = bb->next_bb;

			/* 
			 * FIXME: Can't use the second case in methods with clauses, since the 
			 * bblocks inside the clauses are not processed during dfn computation.
			 */
			if (((header->clauses && (bbn && bbn->region == -1 && bbn->in_count == 0)) ||
				 (!header->clauses && (bbn && bbn->region == -1 && !bbn->dfn))) &&
				bbn != cfg->bb_exit) {
				if (cfg->verbose_level > 1)
					g_print ("found unreachable code in BB%d\n", bbn->block_num);
				/* There may exist unreachable branches to this bb */
				bb->next_bb = bbn->next_bb;
				mono_nullify_basic_block (bbn);			
			} else {
				bb = bb->next_bb;
			}
		}
	}

	if (((cfg->num_varinfo > 2000) || (cfg->num_bblocks > 1000)) && !cfg->compile_aot) {
		/* 
		 * we disable some optimizations if there are too many variables
		 * because JIT time may become too expensive. The actual number needs 
		 * to be tweaked and eventually the non-linear algorithms should be fixed.
		 */
		cfg->opt &= ~ (MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP);
		cfg->disable_ssa = TRUE;
	}

	if (cfg->opt & MONO_OPT_LOOP) {
		mono_compile_dominator_info (cfg, MONO_COMP_DOM | MONO_COMP_IDOM);
		mono_compute_natural_loops (cfg);
	}

	/* after method_to_ir */
	if (parts == 1) {
		if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

//#define DEBUGSSA "logic_run"
#define DEBUGSSA_CLASS "Tests"
#ifdef DEBUGSSA

	if (!header->num_clauses && !cfg->disable_ssa) {
		mono_local_cprop (cfg);

#ifndef DISABLE_SSA
		if (cfg->new_ir)
			mono_ssa_compute2 (cfg);
		else
			mono_ssa_compute (cfg);
#endif
	}
#else 
	if (cfg->opt & MONO_OPT_SSA) {
		if (!(cfg->comp_done & MONO_COMP_SSA) && !header->num_clauses && !cfg->disable_ssa) {
#ifndef DISABLE_SSA
			if (!cfg->new_ir)
				mono_local_cprop (cfg);
			if (cfg->new_ir)
				mono_ssa_compute2 (cfg);
			else
				mono_ssa_compute (cfg);
#endif

			if (cfg->verbose_level >= 2) {
				print_dfn (cfg);
			}
		}
	}
#endif

	/* after SSA translation */
	if (parts == 2) {
		if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	if ((cfg->opt & MONO_OPT_CONSPROP) || (cfg->opt & MONO_OPT_COPYPROP)) {
		if (cfg->comp_done & MONO_COMP_SSA) {
#ifndef DISABLE_SSA
			if (cfg->new_ir)
				mono_ssa_cprop2 (cfg);
			else
				mono_ssa_cprop (cfg);
#endif
		} else {
			if (!cfg->new_ir)
				mono_local_cprop (cfg);
		}
	}

#ifndef DISABLE_SSA
	if (cfg->comp_done & MONO_COMP_SSA) {			
		//mono_ssa_strength_reduction (cfg);

		if (cfg->opt & MONO_OPT_SSAPRE) {
			mono_perform_ssapre (cfg);
			//mono_local_cprop (cfg);
		}

		if (cfg->opt & MONO_OPT_DEADCE) {
			if (cfg->new_ir)
				mono_ssa_deadce2 (cfg);
			else
				mono_ssa_deadce (cfg);
			deadce_has_run = TRUE;
		}

		if (cfg->new_ir) {
			if ((cfg->flags & (MONO_CFG_HAS_LDELEMA|MONO_CFG_HAS_CHECK_THIS)) && (cfg->opt & MONO_OPT_ABCREM))
				mono_perform_abc_removal2 (cfg);
		} else {
			if ((cfg->flags & MONO_CFG_HAS_LDELEMA) && (cfg->opt & MONO_OPT_ABCREM))
				mono_perform_abc_removal (cfg);
		}

		if (cfg->new_ir) {
			mono_ssa_remove2 (cfg);
			mono_local_cprop2 (cfg);
			mono_handle_global_vregs (cfg);
			if (cfg->opt & MONO_OPT_DEADCE)
				mono_local_deadce (cfg);
		}
		else
			mono_ssa_remove (cfg);

		if (cfg->opt & MONO_OPT_BRANCH) {
			MonoBasicBlock *bb;

			mono_optimize_branches (cfg);

			/* Have to recompute cfg->bblocks and bb->dfn */
			if (cfg->globalra) {
				mono_remove_critical_edges (cfg);

				for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
					bb->dfn = 0;

				/* Depth-first ordering on basic blocks */
				cfg->bblocks = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (cfg->num_bblocks + 1));

				dfn = 0;
				df_visit (cfg->bb_entry, &dfn, cfg->bblocks);
				cfg->num_bblocks = dfn + 1;
			}
		}
	}
#endif

	/* after SSA removal */
	if (parts == 3) {
		if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	if (cfg->new_ir) {
#ifdef MONO_ARCH_SOFT_FLOAT
		mono_decompose_soft_float (cfg);
#endif
		mono_decompose_vtype_opts (cfg);
		if (cfg->flags & MONO_CFG_HAS_ARRAY_ACCESS)
			mono_decompose_array_access_opts (cfg);
	}

	if (!cfg->new_ir) {
		if (cfg->verbose_level > 4)
			mono_print_code (cfg, "BEFORE DECOMPOSE");

		decompose_pass (cfg);
	}

	if (cfg->got_var) {
		GList *regs;

		g_assert (cfg->got_var_allocated);

		/* 
		 * Allways allocate the GOT var to a register, because keeping it
		 * in memory will increase the number of live temporaries in some
		 * code created by inssel.brg, leading to the well known spills+
		 * branches problem. Testcase: mcs crash in 
		 * System.MonoCustomAttrs:GetCustomAttributes.
		 */
		regs = mono_arch_get_global_int_regs (cfg);
		g_assert (regs);
		cfg->got_var->opcode = OP_REGVAR;
		cfg->got_var->dreg = GPOINTER_TO_INT (regs->data);
		cfg->used_int_regs |= 1LL << cfg->got_var->dreg;
		
		g_list_free (regs);
	}

	/* todo: remove code when we have verified that the liveness for try/catch blocks
	 * works perfectly 
	 */
	/* 
	 * Currently, this can't be commented out since exception blocks are not
	 * processed during liveness analysis.
	 */
	mono_liveness_handle_exception_clauses (cfg);

	if (cfg->globalra) {
		MonoBasicBlock *bb;

		/* Have to do this before regalloc since it can create vregs */
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			mono_arch_lowering_pass (cfg, bb);

		mono_global_regalloc (cfg);
	}

	if ((cfg->opt & MONO_OPT_LINEARS) && !cfg->globalra) {
		GList *vars, *regs;
		
		/* For now, compute aliasing info only if needed for deadce... */
		if (!cfg->new_ir && (cfg->opt & MONO_OPT_DEADCE) && (! deadce_has_run) && (header->num_clauses == 0)) {
			cfg->aliasing_info = mono_build_aliasing_information (cfg);
		}

		/* fixme: maybe we can avoid to compute livenesss here if already computed ? */
		cfg->comp_done &= ~MONO_COMP_LIVENESS;
		if (!(cfg->comp_done & MONO_COMP_LIVENESS))
			mono_analyze_liveness (cfg);

		if (cfg->aliasing_info != NULL) {
			mono_aliasing_deadce (cfg->aliasing_info);
			deadce_has_run = TRUE;
		}
		
		if ((vars = mono_arch_get_allocatable_int_vars (cfg))) {
			regs = mono_arch_get_global_int_regs (cfg);
			if (cfg->got_var)
				regs = g_list_delete_link (regs, regs);
			mono_linear_scan (cfg, vars, regs, &cfg->used_int_regs);
		}
		
		if (cfg->aliasing_info != NULL) {
			mono_destroy_aliasing_information (cfg->aliasing_info);
			cfg->aliasing_info = NULL;
		}
	}

	//mono_print_code (cfg);

    //print_dfn (cfg);
	
	/* variables are allocated after decompose, since decompose could create temps */
	if (!cfg->globalra)
		mono_arch_allocate_vars (cfg);

	if (!cfg->new_ir && cfg->opt & MONO_OPT_CFOLD)
		mono_constant_fold (cfg);

	if (cfg->new_ir) {
		MonoBasicBlock *bb;
		gboolean need_local_opts;

		if (!cfg->globalra) {
			mono_spill_global_vars (cfg, &need_local_opts);

			if (need_local_opts || cfg->compile_aot) {
				/* To optimize code created by spill_global_vars */
				mono_local_cprop2 (cfg);
				if (cfg->opt & MONO_OPT_DEADCE)
					mono_local_deadce (cfg);
			}
		}

		/* Add branches between non-consecutive bblocks */
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			if (bb->last_ins && MONO_IS_COND_BRANCH_OP (bb->last_ins) &&
				bb->last_ins->inst_false_bb && bb->next_bb != bb->last_ins->inst_false_bb) {
				/* we are careful when inverting, since bugs like #59580
				 * could show up when dealing with NaNs.
				 */
				if (MONO_IS_COND_BRANCH_NOFP(bb->last_ins) && bb->next_bb == bb->last_ins->inst_true_bb) {
					MonoBasicBlock *tmp =  bb->last_ins->inst_true_bb;
					bb->last_ins->inst_true_bb = bb->last_ins->inst_false_bb;
					bb->last_ins->inst_false_bb = tmp;

					bb->last_ins->opcode = mono_reverse_branch_op (bb->last_ins->opcode);
				} else {			
					MonoInst *inst = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
					inst->opcode = OP_BR;
					inst->inst_target_bb = bb->last_ins->inst_false_bb;
					mono_bblock_add_inst (bb, inst);
				}
			}
		}

		if (cfg->verbose_level >= 4 && !cfg->globalra) {
			for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
				MonoInst *tree = bb->code;	
				g_print ("DUMP BLOCK %d:\n", bb->block_num);
				if (!tree)
					continue;
				for (; tree; tree = tree->next) {
					mono_print_ins_index (-1, tree);
				}
			}
		}

		/* FIXME: */
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb->max_vreg = cfg->next_vreg;
		}
	}
	else
		mini_select_instructions (cfg);

	mono_codegen (cfg);
	if (cfg->verbose_level >= 2) {
		char *id =  mono_method_full_name (cfg->method, FALSE);
		mono_disassemble_code (cfg, cfg->native_code, cfg->code_len, id + 3);
		g_free (id);
	}

	if (cfg->generic_sharing_context)
		generic_info_size = sizeof (MonoGenericJitInfo);
	else
		generic_info_size = 0;

	if (cfg->method->dynamic) {
		jinfo = g_malloc0 (sizeof (MonoJitInfo) + (header->num_clauses * sizeof (MonoJitExceptionInfo)) +
				generic_info_size);
	} else {
		/* we access cfg->domain->mp */
		mono_domain_lock (cfg->domain);
		jinfo = mono_domain_alloc0 (cfg->domain, sizeof (MonoJitInfo) +
				(header->num_clauses * sizeof (MonoJitExceptionInfo)) +
				generic_info_size);
		mono_domain_unlock (cfg->domain);
	}

	if (cfg->generic_sharing_context) {
		MonoGenericContext object_context;

		g_assert (!method_to_compile->klass->generic_class);
		if (method_to_compile->klass->generic_container) {
			int type_argc = method_to_compile->klass->generic_container->type_argc;

			object_context.class_inst = get_object_generic_inst (type_argc);
		} else {
			object_context.class_inst = NULL;
		}

		if (mini_method_get_context (method_to_compile)->method_inst) {
			int type_argc = mini_method_get_context (method_to_compile)->method_inst->type_argc;

			object_context.method_inst = get_object_generic_inst (type_argc);
		} else {
			object_context.method_inst = NULL;
		}

		g_assert (object_context.class_inst || object_context.method_inst);

		method_to_register = mono_class_inflate_generic_method (method_to_compile, &object_context);
	} else {
		g_assert (method == method_to_compile);
		method_to_register = method;
	}

	jinfo->method = method_to_register;
	jinfo->code_start = cfg->native_code;
	jinfo->code_size = cfg->code_len;
	jinfo->used_regs = cfg->used_int_regs;
	jinfo->domain_neutral = (cfg->opt & MONO_OPT_SHARED) != 0;
	jinfo->cas_inited = FALSE; /* initialization delayed at the first stalk walk using this method */
	jinfo->num_clauses = header->num_clauses;

	if (cfg->generic_sharing_context) {
		MonoInst *inst;
		MonoGenericJitInfo *gi;

		jinfo->has_generic_jit_info = 1;

		gi = mono_jit_info_get_generic_jit_info (jinfo);
		g_assert (gi);

		gi->generic_sharing_context = cfg->generic_sharing_context;

		/*
		 * Non-generic static methods only get a "this" info
		 * if they use the rgctx variable (which they are
		 * forced to if they have any open catch clauses).
		 */
		if (cfg->rgctx_var ||
				(!(method_to_compile->flags & METHOD_ATTRIBUTE_STATIC) &&
				!mini_method_get_context (method_to_compile)->method_inst &&
				!method_to_compile->klass->valuetype)) {
			gi->has_this = 1;

			if ((method_to_compile->flags & METHOD_ATTRIBUTE_STATIC) ||
					mini_method_get_context (method_to_compile)->method_inst ||
					method_to_compile->klass->valuetype) {
				inst = cfg->rgctx_var;
				g_assert (inst->opcode == OP_REGOFFSET);
			} else {
				inst = cfg->args [0];
			}

			if (inst->opcode == OP_REGVAR) {
				gi->this_in_reg = 1;
				gi->this_reg = inst->dreg;

				//g_print ("this in reg %d\n", inst->dreg);
			} else {
				g_assert (inst->opcode == OP_REGOFFSET);
#ifdef __i386__
				g_assert (inst->inst_basereg == X86_EBP);
#elif defined(__x86_64__)
				g_assert (inst->inst_basereg == X86_EBP || inst->inst_basereg == X86_ESP);
#endif
				g_assert (inst->inst_offset >= G_MININT32 && inst->inst_offset <= G_MAXINT32);

				gi->this_in_reg = 0;
				gi->this_reg = inst->inst_basereg;
				gi->this_offset = inst->inst_offset;

				//g_print ("this at offset %d from reg %d\n", gi->this_offset, gi->this_reg);
			}
		} else {
			gi->has_this = 0;
		}
	}

	if (header->num_clauses) {
		int i;

		for (i = 0; i < header->num_clauses; i++) {
			MonoExceptionClause *ec = &header->clauses [i];
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			MonoBasicBlock *tblock;
			MonoInst *exvar;

			ei->flags = ec->flags;

			exvar = mono_find_exvar_for_offset (cfg, ec->handler_offset);
			ei->exvar_offset = exvar ? exvar->inst_offset : 0;

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				tblock = cfg->cil_offset_to_bb [ec->data.filter_offset];
				g_assert (tblock);
				ei->data.filter = cfg->native_code + tblock->native_offset;
			} else {
				ei->data.catch_class = ec->data.catch_class;
			}

			tblock = cfg->cil_offset_to_bb [ec->try_offset];
			g_assert (tblock);
			ei->try_start = cfg->native_code + tblock->native_offset;
			g_assert (tblock->native_offset);
			tblock = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			g_assert (tblock);
			ei->try_end = cfg->native_code + tblock->native_offset;
			g_assert (tblock->native_offset);
			tblock = cfg->cil_offset_to_bb [ec->handler_offset];
			g_assert (tblock);
			ei->handler_start = cfg->native_code + tblock->native_offset;
		}
	}

	cfg->jit_info = jinfo;
#if defined(__arm__)
	mono_arch_fixup_jinfo (cfg);
#endif

	if (!cfg->compile_aot) {
		mono_domain_lock (cfg->domain);
		mono_jit_info_table_add (cfg->domain, jinfo);

		if (cfg->method->dynamic)
			mono_dynamic_code_hash_lookup (cfg->domain, cfg->method)->ji = jinfo;
		mono_domain_unlock (cfg->domain);
	}

	/* collect statistics */
	mono_perfcounters->jit_methods++;
	mono_perfcounters->jit_bytes += header->code_size;
	mono_jit_stats.allocated_code_size += cfg->code_len;
	code_size_ratio = cfg->code_len;
	if (code_size_ratio > mono_jit_stats.biggest_method_size && mono_jit_stats.enabled) {
		mono_jit_stats.biggest_method_size = code_size_ratio;
		g_free (mono_jit_stats.biggest_method);
		mono_jit_stats.biggest_method = g_strdup_printf ("%s::%s)", method->klass->name, method->name);
	}
	code_size_ratio = (code_size_ratio * 100) / mono_method_get_header (method)->code_size;
	if (code_size_ratio > mono_jit_stats.max_code_size_ratio && mono_jit_stats.enabled) {
		mono_jit_stats.max_code_size_ratio = code_size_ratio;
		g_free (mono_jit_stats.max_ratio_method);
		mono_jit_stats.max_ratio_method = g_strdup_printf ("%s::%s)", method->klass->name, method->name);
	}
	mono_jit_stats.native_code_size += cfg->code_len;

	if (MONO_PROBE_METHOD_COMPILE_END_ENABLED ())
		MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
	if (cfg->prof_options & MONO_PROFILE_JIT_COMPILATION)
		mono_profiler_method_end_jit (method, jinfo, MONO_PROFILE_OK);

	return cfg;
}

#else

MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, gboolean run_cctors, gboolean compile_aot, int parts)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* DISABLE_JIT */

static MonoJitInfo*
lookup_generic_method (MonoDomain *domain, MonoMethod *method)
{
	MonoMethod *open_method;

	if (!mono_method_is_generic_sharable_impl (method, FALSE))
		return NULL;

	open_method = mono_method_get_declaring_generic_method (method);

	return mono_domain_lookup_shared_generic (domain, open_method);
}

/*
 * LOCKING: Assumes domain->jit_code_hash_lock is held.
 */
static MonoJitInfo*
lookup_method_inner (MonoDomain *domain, MonoMethod *method)
{
	MonoJitInfo *ji = mono_internal_hash_table_lookup (&domain->jit_code_hash, method);

	if (ji)
		return ji;

	return lookup_generic_method (domain, method);
}

static MonoJitInfo*
lookup_method (MonoDomain *domain, MonoMethod *method)
{
	MonoJitInfo *info;

	mono_domain_jit_code_hash_lock (domain);
	info = lookup_method_inner (domain, method);
	mono_domain_jit_code_hash_unlock (domain);

	return info;
}

static gpointer
mono_jit_compile_method_inner (MonoMethod *method, MonoDomain *target_domain, int opt)
{
	MonoCompile *cfg;
	gpointer code = NULL;
	MonoJitInfo *info;
	MonoVTable *vtable;

#ifdef MONO_USE_AOT_COMPILER
	if ((opt & MONO_OPT_AOT) && !(mono_profiler_get_events () & MONO_PROFILE_JIT_COMPILATION)) {
		MonoDomain *domain = mono_domain_get ();

		mono_class_init (method->klass);

		if ((code = mono_aot_get_method (domain, method))) {
			vtable = mono_class_vtable (domain, method->klass);
			g_assert (vtable);
			mono_runtime_class_init (vtable);
			return code;
		}
	}
#endif

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
		MonoMethod *nm;
		MonoMethodPInvoke* piinfo = (MonoMethodPInvoke *) method;

		if (!piinfo->addr) {
			if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
				piinfo->addr = mono_lookup_internal_call (method);
			else if (method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)
#ifdef PLATFORM_WIN32
				g_warning ("Method '%s' in assembly '%s' contains native code that cannot be executed by Mono in modules loaded from byte arrays. The assembly was probably created using C++/CLI.\n", mono_method_full_name (method, TRUE), method->klass->image->name);
#else
				g_warning ("Method '%s' in assembly '%s' contains native code that cannot be executed by Mono on this platform. The assembly was probably created using C++/CLI.\n", mono_method_full_name (method, TRUE), method->klass->image->name);
#endif
			else
				mono_lookup_pinvoke_call (method, NULL, NULL);
		}
		nm = mono_marshal_get_native_wrapper (method, check_for_pending_exc, FALSE);
		return mono_get_addr_from_ftnptr (mono_compile_method (nm));

		//if (mono_debug_format != MONO_DEBUG_FORMAT_NONE) 
		//mono_debug_add_wrapper (method, nm);
	} else if ((method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		const char *name = method->name;
		MonoMethod *nm;

		if (method->klass->parent == mono_defaults.multicastdelegate_class) {
			if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
				MonoJitICallInfo *mi = mono_find_jit_icall_by_name ("mono_delegate_ctor");
				g_assert (mi);
				/*
				 * We need to make sure this wrapper
				 * is compiled because it might end up
				 * in an (M)RGCTX if generic sharing
				 * is enabled, and would be called
				 * indirectly.  If it were a
				 * trampoline we'd try to patch that
				 * indirect call, which is not
				 * possible.
				 */
				return mono_get_addr_from_ftnptr ((gpointer)mono_icall_get_wrapper_full (mi, TRUE));
			} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
				return mono_create_delegate_trampoline (method->klass);
#else
				nm = mono_marshal_get_delegate_invoke (method, NULL);
				return mono_get_addr_from_ftnptr (mono_compile_method (nm));
#endif
			} else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
				nm = mono_marshal_get_delegate_begin_invoke (method);
				return mono_get_addr_from_ftnptr (mono_compile_method (nm));
			} else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
				nm = mono_marshal_get_delegate_end_invoke (method);
				return mono_get_addr_from_ftnptr (mono_compile_method (nm));
			}
		}
		return NULL;
	}

	if (mono_aot_only)
		g_error ("Attempting to JIT compile method '%s' while running with --aot-only.\n", mono_method_full_name (method, TRUE));

	cfg = mini_method_compile (method, opt, target_domain, TRUE, FALSE, 0);

	switch (cfg->exception_type) {
	case MONO_EXCEPTION_NONE:
		break;
	case MONO_EXCEPTION_TYPE_LOAD:
	case MONO_EXCEPTION_MISSING_FIELD:
	case MONO_EXCEPTION_MISSING_METHOD:
	case MONO_EXCEPTION_FILE_NOT_FOUND:
	case MONO_EXCEPTION_BAD_IMAGE: {
		/* Throw a type load exception if needed */
		MonoLoaderError *error = mono_loader_get_last_error ();
		MonoException *ex;

		if (error) {
			ex = mono_loader_error_prepare_exception (error);
		} else {
			if (cfg->exception_ptr) {
				ex = mono_class_get_exception_for_failure (cfg->exception_ptr);
			} else {
				if (cfg->exception_type == MONO_EXCEPTION_MISSING_FIELD)
					ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingFieldException", cfg->exception_message);
				else if (cfg->exception_type == MONO_EXCEPTION_MISSING_METHOD)
					ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MissingMethodException", cfg->exception_message);
				else if (cfg->exception_type == MONO_EXCEPTION_TYPE_LOAD)
					ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "TypeLoadException", cfg->exception_message);
				else if (cfg->exception_type == MONO_EXCEPTION_FILE_NOT_FOUND)
					ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "FileNotFoundException", cfg->exception_message);
				else if (cfg->exception_type == MONO_EXCEPTION_BAD_IMAGE)
					ex = mono_get_exception_bad_image_format (cfg->exception_message);
				else
					g_assert_not_reached ();
			}
		}
		mono_destroy_compile (cfg);
		mono_raise_exception (ex);
		break;
	}
	case MONO_EXCEPTION_INVALID_PROGRAM: {
		MonoException *ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "InvalidProgramException", cfg->exception_message);
		mono_destroy_compile (cfg);
		mono_raise_exception (ex);
		break;
	}
	case MONO_EXCEPTION_UNVERIFIABLE_IL: {
		MonoException *ex = mono_exception_from_name_msg (mono_defaults.corlib, "System.Security", "VerificationException", cfg->exception_message);
		mono_destroy_compile (cfg);
		mono_raise_exception (ex);
		break;
	}
	case MONO_EXCEPTION_METHOD_ACCESS: {
		MonoException *ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MethodAccessException", cfg->exception_message);
		mono_destroy_compile (cfg);
		mono_raise_exception (ex);
		break;
	}
	case MONO_EXCEPTION_FIELD_ACCESS: {
		MonoException *ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "FieldAccessException", cfg->exception_message);
		mono_destroy_compile (cfg);
		mono_raise_exception (ex);
		break;
	}
	/* this can only be set if the security manager is active */
	case MONO_EXCEPTION_SECURITY_LINKDEMAND: {
		MonoSecurityManager* secman = mono_security_manager_get_methods ();
		MonoObject *exc = NULL;
		gpointer args [2];

		args [0] = &cfg->exception_data;
		args [1] = &method;
		mono_runtime_invoke (secman->linkdemandsecurityexception, NULL, args, &exc);

		mono_destroy_compile (cfg);
		cfg = NULL;

		mono_raise_exception ((MonoException*)exc);
	}
	case MONO_EXCEPTION_OBJECT_SUPPLIED: {
		MonoException *exp = cfg->exception_ptr;
		MONO_GC_UNREGISTER_ROOT (cfg->exception_ptr);
		mono_destroy_compile (cfg);
		mono_raise_exception (exp);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	mono_domain_lock (target_domain);

	/* Check if some other thread already did the job. In this case, we can
       discard the code this thread generated. */

	mono_domain_jit_code_hash_lock (target_domain);

	info = lookup_method_inner (target_domain, method);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if ((target_domain == mono_domain_get ()) || info->domain_neutral) {
			code = info->code_start;
//			printf("Discarding code for method %s\n", method->name);
		}
	}
	
	if (code == NULL) {
		mono_internal_hash_table_insert (&target_domain->jit_code_hash, cfg->jit_info->method, cfg->jit_info);
		mono_domain_jit_code_hash_unlock (target_domain);
		code = cfg->native_code;

		if (cfg->generic_sharing_context && mono_method_is_generic_sharable_impl (method, FALSE)) {
			/* g_print ("inserting method %s.%s.%s\n", method->klass->name_space, method->klass->name, method->name); */
			mono_domain_register_shared_generic (target_domain, 
				mono_method_get_declaring_generic_method (method), cfg->jit_info);
			mono_stats.generics_shared_methods++;
		}
	} else {
		mono_domain_jit_code_hash_unlock (target_domain);
	}

	mono_destroy_compile (cfg);

	if (domain_jit_info (target_domain)->jump_target_hash) {
		MonoJumpInfo patch_info;
		GSList *list, *tmp;
		list = g_hash_table_lookup (domain_jit_info (target_domain)->jump_target_hash, method);
		if (list) {
			patch_info.next = NULL;
			patch_info.ip.i = 0;
			patch_info.type = MONO_PATCH_INFO_METHOD_JUMP;
			patch_info.data.method = method;
			g_hash_table_remove (domain_jit_info (target_domain)->jump_target_hash, method);
		}
		for (tmp = list; tmp; tmp = tmp->next)
			mono_arch_patch_code (NULL, target_domain, tmp->data, &patch_info, TRUE);
		g_slist_free (list);
	}

	mono_domain_unlock (target_domain);

	vtable = mono_class_vtable (target_domain, method->klass);
	if (!vtable) {
		MonoException *exc;
		exc = mono_class_get_exception_for_failure (method->klass);
		g_assert (exc);
		mono_raise_exception (exc);
	}
	mono_runtime_class_init (vtable);
	return code;
}

static gpointer
mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt)
{
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoJitInfo *info;
	gpointer p;
	MonoJitICallInfo *callinfo = NULL;

	/*
	 * ICALL wrappers are handled specially, since there is only one copy of them
	 * shared by all appdomains.
	 */
	if ((method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && (strstr (method->name, "__icall_wrapper_") == method->name)) {
		const char *icall_name;

		icall_name = method->name + strlen ("__icall_wrapper_");
		g_assert (icall_name);
		callinfo = mono_find_jit_icall_by_name (icall_name);
		g_assert (callinfo);

		/* Must be domain neutral since there is only one copy */
		opt |= MONO_OPT_SHARED;
	}

	if (opt & MONO_OPT_SHARED)
		target_domain = mono_get_root_domain ();
	else 
		target_domain = domain;

	info = lookup_method (target_domain, method);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if (! ((domain != target_domain) && !info->domain_neutral)) {
			MonoVTable *vtable;

			mono_jit_stats.methods_lookups++;
			vtable = mono_class_vtable (domain, method->klass);
			mono_runtime_class_init (vtable);
			return mono_create_ftnptr (target_domain, info->code_start);
		}
	}

	p = mono_create_ftnptr (target_domain, mono_jit_compile_method_inner (method, target_domain, opt));

	if (callinfo) {
		mono_jit_lock ();
		if (!callinfo->wrapper) {
			callinfo->wrapper = p;
			mono_register_jit_icall_wrapper (callinfo, p);
			mono_debug_add_icall_wrapper (method, callinfo);
		}
		mono_jit_unlock ();
	}

	return p;
}

static gpointer
mono_jit_compile_method (MonoMethod *method)
{
	return mono_jit_compile_method_with_opt (method, default_opt);
}

#ifdef MONO_ARCH_HAVE_INVALIDATE_METHOD
static void
invalidated_delegate_trampoline (char *desc)
{
	g_error ("Unmanaged code called delegate of type %s which was already garbage collected.\n"
		 "See http://www.go-mono.com/delegate.html for an explanation and ways to fix this.",
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

	g_assert (method->dynamic);

	mono_domain_lock (domain);
	ji = mono_dynamic_code_hash_lookup (domain, method);
	mono_domain_unlock (domain);

	if (!ji)
		return;
	mono_domain_lock (domain);
	g_hash_table_remove (domain_jit_info (domain)->dynamic_code_hash, method);
	mono_internal_hash_table_remove (&domain->jit_code_hash, method);
	g_hash_table_remove (domain_jit_info (domain)->jump_trampoline_hash, method);
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
mono_jit_find_compiled_method (MonoDomain *domain, MonoMethod *method)
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
			return info->code_start;
		}
	}

	return NULL;
}

/**
 * mono_jit_runtime_invoke:
 * @method: the method to invoke
 * @obj: this pointer
 * @params: array of parameter values.
 * @exc: used to catch exceptions objects
 */
static MonoObject*
mono_jit_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	MonoMethod *to_compile;
	MonoMethod *invoke;
	MonoObject *(*runtime_invoke) (MonoObject *this, void **params, MonoObject **exc, void* compiled_method);
	void* compiled_method;
	MonoVTable *vtable;

	if (obj == NULL && !(method->flags & METHOD_ATTRIBUTE_STATIC) && !method->string_ctor && (method->wrapper_type == 0)) {
		g_warning ("Ignoring invocation of an instance method on a NULL instance.\n");
		return NULL;
	}

	if (mono_method_needs_static_rgctx_invoke (method, FALSE))
		to_compile = mono_marshal_get_static_rgctx_invoke (method);
	else
		to_compile = method;

	invoke = mono_marshal_get_runtime_invoke (method);
	runtime_invoke = mono_jit_compile_method (invoke);
	
	/* We need this here becuase mono_marshal_get_runtime_invoke can be place 
	 * the helper method in System.Object and not the target class
	 */
	vtable = mono_class_vtable (mono_domain_get (), method->klass);
	g_assert (vtable);
	mono_runtime_class_init (vtable);

	if (method->klass->rank && (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
		(method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
		/* 
		 * Array Get/Set/Address methods. The JIT implements them using inline code 
		 * inside the runtime invoke wrappers, so no need to compile them.
		 */
		compiled_method = NULL;
	} else {
		compiled_method = mono_jit_compile_method (to_compile);
	}
	return runtime_invoke (obj, params, exc, compiled_method);
}

#ifdef MONO_GET_CONTEXT
#define GET_CONTEXT MONO_GET_CONTEXT
#endif

#ifndef GET_CONTEXT
#ifdef PLATFORM_WIN32
#define GET_CONTEXT \
	struct sigcontext *ctx = (struct sigcontext*)_dummy;
#else
#ifdef MONO_ARCH_USE_SIGACTION
#define GET_CONTEXT \
    void *ctx = context;
#elif defined(__sparc__)
#define GET_CONTEXT \
    void *ctx = sigctx;
#else
#define GET_CONTEXT \
	void **_p = (void **)&_dummy; \
	struct sigcontext *ctx = (struct sigcontext *)++_p;
#endif
#endif
#endif

#ifdef MONO_ARCH_USE_SIGACTION
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, siginfo_t *info, void *context)
#elif defined(__sparc__)
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, void *sigctx)
#else
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy)
#endif

static void
SIG_HANDLER_SIGNATURE (sigfpe_signal_handler)
{
	MonoException *exc = NULL;
#ifndef MONO_ARCH_USE_SIGACTION
	void *info = NULL;
#endif
	GET_CONTEXT;

#if defined(MONO_ARCH_HAVE_IS_INT_OVERFLOW)
	if (mono_arch_is_int_overflow (ctx, info))
		exc = mono_get_exception_arithmetic ();
	else
		exc = mono_get_exception_divide_by_zero ();
#else
	exc = mono_get_exception_divide_by_zero ();
#endif
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
SIG_HANDLER_SIGNATURE (sigill_signal_handler)
{
	MonoException *exc;
	GET_CONTEXT;

	exc = mono_get_exception_execution_engine ("SIGILL");
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
SIG_HANDLER_SIGNATURE (sigsegv_signal_handler)
{
#ifndef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	MonoException *exc = NULL;
#endif
	MonoJitInfo *ji;
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);

	GET_CONTEXT;

#ifdef MONO_ARCH_USE_SIGACTION
	if (debug_options.collect_pagefault_stats) {
		if (mono_aot_is_pagefault (info->si_addr)) {
			mono_aot_handle_pagefault (info->si_addr);
			return;
		}
	}
#endif

	/* The thread might no be registered with the runtime */
	if (!mono_domain_get () || !jit_tls)
		mono_handle_native_sigsegv (SIGSEGV, ctx);

	ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context (ctx));

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	if (mono_handle_soft_stack_ovf (jit_tls, ji, ctx, (guint8*)info->si_addr))
		return;

	/* The hard-guard page has been hit: there is not much we can do anymore
	 * Print a hopefully clear message and abort.
	 */
	if (jit_tls->stack_size && 
			ABS ((guint8*)info->si_addr - ((guint8*)jit_tls->end_of_stack - jit_tls->stack_size)) < 32768) {
		const char *method;
		/* we don't do much now, but we can warn the user with a useful message */
		fprintf (stderr, "Stack overflow: IP: %p, fault addr: %p\n", mono_arch_ip_from_context (ctx), (gpointer)info->si_addr);
		if (ji && ji->method)
			method = mono_method_full_name (ji->method, TRUE);
		else
			method = "Unmanaged";
		fprintf (stderr, "At %s\n", method);
		_exit (1);
	} else {
		mono_arch_handle_altstack_exception (ctx, info->si_addr, FALSE);
	}
#else

	if (!ji) {
		mono_handle_native_sigsegv (SIGSEGV, ctx);
	}
			
	mono_arch_handle_exception (ctx, exc, FALSE);
#endif
}

#ifndef PLATFORM_WIN32

static void
SIG_HANDLER_SIGNATURE (sigabrt_signal_handler)
{
	MonoJitInfo *ji;
	GET_CONTEXT;

	ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context(ctx));
	if (!ji) {
		mono_handle_native_sigsegv (SIGABRT, ctx);
	}
}

static void
SIG_HANDLER_SIGNATURE (sigusr1_signal_handler)
{
	gboolean running_managed;
	MonoException *exc;
	MonoThread *thread = mono_thread_current ();
	MonoDomain *domain = mono_domain_get ();
	void *ji;
	
	GET_CONTEXT;

	if (!thread || !domain)
		/* The thread might not have started up yet */
		/* FIXME: Specify the synchronization with start_wrapper () in threads.c */
		return;

	if (thread->thread_dump_requested) {
		thread->thread_dump_requested = FALSE;

		mono_print_thread_dump (ctx);
	}

	/*
	 * FIXME:
	 * This is an async signal, so the code below must not call anything which
	 * is not async safe. That includes the pthread locking functions. If we
	 * know that we interrupted managed code, then locking is safe.
	 */
	ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context(ctx));
	running_managed = ji != NULL;
	
	exc = mono_thread_request_interruption (running_managed); 
	if (!exc) return;

	mono_arch_handle_exception (ctx, exc, FALSE);
}

#if defined(__i386__) || defined(__x86_64__)
#define FULL_STAT_PROFILER_BACKTRACE 1
#define CURRENT_FRAME_GET_BASE_POINTER(f) (* (gpointer*)(f))
#define CURRENT_FRAME_GET_RETURN_ADDRESS(f) (* (((gpointer*)(f)) + 1))
#if MONO_ARCH_STACK_GROWS_UP
#define IS_BEFORE_ON_STACK <
#define IS_AFTER_ON_STACK >
#else
#define IS_BEFORE_ON_STACK >
#define IS_AFTER_ON_STACK <
#endif
#else
#define FULL_STAT_PROFILER_BACKTRACE 0
#endif

#if defined(__ia64__) || defined(__sparc__) || defined(sparc) || defined(__s390__) || defined(s390)

static void
SIG_HANDLER_SIGNATURE (sigprof_signal_handler)
{
	NOT_IMPLEMENTED;
}

#else

static void
SIG_HANDLER_SIGNATURE (sigprof_signal_handler)
{
	int call_chain_depth = mono_profiler_stat_get_call_chain_depth ();
	GET_CONTEXT;
	
	if (call_chain_depth == 0) {
		mono_profiler_stat_hit (mono_arch_ip_from_context (ctx), ctx);
	} else {
		MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
		int current_frame_index = 1;
		MonoContext mono_context;
#if FULL_STAT_PROFILER_BACKTRACE
		guchar *current_frame;
		guchar *stack_bottom;
		guchar *stack_top;
#else
		MonoDomain *domain;
#endif
		guchar *ips [call_chain_depth + 1];

		mono_arch_sigctx_to_monoctx (ctx, &mono_context);
		ips [0] = MONO_CONTEXT_GET_IP (&mono_context);
		
		if (jit_tls != NULL) {
#if FULL_STAT_PROFILER_BACKTRACE
			stack_bottom = jit_tls->end_of_stack;
			stack_top = MONO_CONTEXT_GET_SP (&mono_context);
			current_frame = MONO_CONTEXT_GET_BP (&mono_context);
			
			while ((current_frame_index <= call_chain_depth) &&
					(stack_bottom IS_BEFORE_ON_STACK (guchar*) current_frame) &&
					((guchar*) current_frame IS_BEFORE_ON_STACK stack_top)) {
				ips [current_frame_index] = CURRENT_FRAME_GET_RETURN_ADDRESS (current_frame);
				current_frame_index ++;
				stack_top = current_frame;
				current_frame = CURRENT_FRAME_GET_BASE_POINTER (current_frame);
			}
#else
			domain = mono_domain_get ();
			if (domain != NULL) {
				MonoLMF *lmf = NULL;
				MonoJitInfo *ji;
				MonoJitInfo res;
				MonoContext new_mono_context;
				int native_offset;
				ji = mono_find_jit_info (domain, jit_tls, &res, NULL, &mono_context,
						&new_mono_context, NULL, &lmf, &native_offset, NULL);
				while ((ji != NULL) && (current_frame_index <= call_chain_depth)) {
					ips [current_frame_index] = MONO_CONTEXT_GET_IP (&new_mono_context);
					current_frame_index ++;
					mono_context = new_mono_context;
					ji = mono_find_jit_info (domain, jit_tls, &res, NULL, &mono_context,
							&new_mono_context, NULL, &lmf, &native_offset, NULL);
				}
			}
#endif
		}
		
		
		mono_profiler_stat_call_chain (current_frame_index, & ips [0], ctx);
	}
}

#endif

static void
SIG_HANDLER_SIGNATURE (sigquit_signal_handler)
{
	gboolean res;

	GET_CONTEXT;

	/* We use this signal to start the attach agent too */
	res = mono_attach_start ();
	if (res)
		return;

	printf ("Full thread dump:\n");

	mono_threads_request_thread_dump ();

	/*
	 * print_thread_dump () skips the current thread, since sending a signal
	 * to it would invoke the signal handler below the sigquit signal handler,
	 * and signal handlers don't create an lmf, so the stack walk could not
	 * be performed.
	 */
	mono_print_thread_dump (ctx);
}

static void
SIG_HANDLER_SIGNATURE (sigusr2_signal_handler)
{
	gboolean enabled = mono_trace_is_enabled ();

	mono_trace_enable (!enabled);
}

#endif

static void
SIG_HANDLER_SIGNATURE (sigint_signal_handler)
{
	MonoException *exc;
	GET_CONTEXT;

	exc = mono_get_exception_execution_engine ("Interrupted (SIGINT).");
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

#ifdef PLATFORM_MACOSX

/*
 * This code disables the CrashReporter of MacOS X by installing
 * a dummy Mach exception handler.
 */

/*
 * http://darwinsource.opendarwin.org/10.4.3/xnu-792.6.22/osfmk/man/exc_server.html
 */
extern
boolean_t
exc_server (mach_msg_header_t *request_msg,
	    mach_msg_header_t *reply_msg);

/*
 * The exception message
 */
typedef struct {
	mach_msg_base_t msg;  /* common mach message header */
	char payload [1024];  /* opaque */
} mach_exception_msg_t;

/* The exception port */
static mach_port_t mach_exception_port = VM_MAP_NULL;

/*
 * Implicitly called by exc_server. Must be public.
 *
 * http://darwinsource.opendarwin.org/10.4.3/xnu-792.6.22/osfmk/man/catch_exception_raise.html
 */
kern_return_t
catch_exception_raise (
	mach_port_t exception_port,
	mach_port_t thread,
	mach_port_t task,
	exception_type_t exception,
	exception_data_t code,
	mach_msg_type_number_t code_count)
{
	/* consume the exception */
	return KERN_FAILURE;
}

/*
 * Exception thread handler.
 */
static
void *
mach_exception_thread (void *arg)
{
	for (;;) {
		mach_exception_msg_t request;
		mach_exception_msg_t reply;
		mach_msg_return_t result;

		/* receive from "mach_exception_port" */
		result = mach_msg (&request.msg.header,
				   MACH_RCV_MSG | MACH_RCV_LARGE,
				   0,
				   sizeof (request),
				   mach_exception_port,
				   MACH_MSG_TIMEOUT_NONE,
				   MACH_PORT_NULL);

		g_assert (result == MACH_MSG_SUCCESS);

		/* dispatch to catch_exception_raise () */
		exc_server (&request.msg.header, &reply.msg.header);

		/* send back to sender */
		result = mach_msg (&reply.msg.header,
				   MACH_SEND_MSG,
				   reply.msg.header.msgh_size,
				   0,
				   MACH_PORT_NULL,
				   MACH_MSG_TIMEOUT_NONE,
				   MACH_PORT_NULL);

		g_assert (result == MACH_MSG_SUCCESS);
	}
	return NULL;
}

static void
macosx_register_exception_handler ()
{
	mach_port_t task;
	pthread_attr_t attr;
	pthread_t thread;

	if (mach_exception_port != VM_MAP_NULL)
		return;

	task = mach_task_self ();

	/* create the "mach_exception_port" with send & receive rights */
	g_assert (mach_port_allocate (task, MACH_PORT_RIGHT_RECEIVE,
				      &mach_exception_port) == KERN_SUCCESS);
	g_assert (mach_port_insert_right (task, mach_exception_port, mach_exception_port,
					  MACH_MSG_TYPE_MAKE_SEND) == KERN_SUCCESS);

	/* create the exception handler thread */
	g_assert (!pthread_attr_init (&attr));
	g_assert (!pthread_attr_setdetachstate (&attr, PTHREAD_CREATE_DETACHED));
	g_assert (!pthread_create (&thread, &attr, mach_exception_thread, NULL));
	pthread_attr_destroy (&attr);

	/*
	 * register "mach_exception_port" as a receiver for the
	 * EXC_BAD_ACCESS exception
	 *
	 * http://darwinsource.opendarwin.org/10.4.3/xnu-792.6.22/osfmk/man/task_set_exception_ports.html
	 */
	g_assert (task_set_exception_ports (task, EXC_MASK_BAD_ACCESS,
					    mach_exception_port,
					    EXCEPTION_DEFAULT,
					    MACHINE_THREAD_STATE) == KERN_SUCCESS);
}
#endif

#ifndef PLATFORM_WIN32
static void
add_signal_handler (int signo, gpointer handler)
{
	struct sigaction sa;

#ifdef MONO_ARCH_USE_SIGACTION
	sa.sa_sigaction = handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO;
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	if (signo == SIGSEGV)
		sa.sa_flags |= SA_ONSTACK;
#endif
#else
	sa.sa_handler = handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
#endif
	g_assert (sigaction (signo, &sa, NULL) != -1);
}

static void
remove_signal_handler (int signo)
{
	struct sigaction sa;

	sa.sa_handler = SIG_DFL;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;

	g_assert (sigaction (signo, &sa, NULL) != -1);
}
#endif

static void
mono_runtime_install_handlers (void)
{
#ifdef PLATFORM_WIN32
	win32_seh_init();
	win32_seh_set_handler(SIGFPE, sigfpe_signal_handler);
	win32_seh_set_handler(SIGILL, sigill_signal_handler);
	win32_seh_set_handler(SIGSEGV, sigsegv_signal_handler);
	if (debug_options.handle_sigint)
		win32_seh_set_handler(SIGINT, sigint_signal_handler);

#else /* !PLATFORM_WIN32 */

	sigset_t signal_set;

#if defined(PLATFORM_MACOSX) && !defined(__arm__)
	macosx_register_exception_handler ();
#endif

	if (debug_options.handle_sigint)
		add_signal_handler (SIGINT, sigint_signal_handler);

	add_signal_handler (SIGFPE, sigfpe_signal_handler);
	add_signal_handler (SIGQUIT, sigquit_signal_handler);
	add_signal_handler (SIGILL, sigill_signal_handler);
	add_signal_handler (SIGBUS, sigsegv_signal_handler);
	if (mono_jit_trace_calls != NULL)
		add_signal_handler (SIGUSR2, sigusr2_signal_handler);

	add_signal_handler (mono_thread_get_abort_signal (), sigusr1_signal_handler);
	/* it seems to have become a common bug for some programs that run as parents
	 * of many processes to block signal delivery for real time signals.
	 * We try to detect and work around their breakage here.
	 */
	sigemptyset (&signal_set);
	sigaddset (&signal_set, mono_thread_get_abort_signal ());
	sigprocmask (SIG_UNBLOCK, &signal_set, NULL);

	signal (SIGPIPE, SIG_IGN);

	add_signal_handler (SIGABRT, sigabrt_signal_handler);

	/* catch SIGSEGV */
	add_signal_handler (SIGSEGV, sigsegv_signal_handler);
#endif /* PLATFORM_WIN32 */
}

static void
mono_runtime_cleanup_handlers (void)
{
#ifdef PLATFORM_WIN32
	win32_seh_cleanup();
#else
	if (debug_options.handle_sigint)
		remove_signal_handler (SIGINT);

	remove_signal_handler (SIGFPE);
	remove_signal_handler (SIGQUIT);
	remove_signal_handler (SIGILL);
	remove_signal_handler (SIGBUS);
	if (mono_jit_trace_calls != NULL)
		remove_signal_handler (SIGUSR2);

	remove_signal_handler (mono_thread_get_abort_signal ());

	remove_signal_handler (SIGABRT);

	remove_signal_handler (SIGSEGV);
#endif /* PLATFORM_WIN32 */
}


#ifdef HAVE_LINUX_RTC_H
#include <linux/rtc.h>
#include <sys/ioctl.h>
#include <fcntl.h>
static int rtc_fd = -1;

static int
enable_rtc_timer (gboolean enable)
{
	int flags;
	flags = fcntl (rtc_fd, F_GETFL);
	if (flags < 0) {
		perror ("getflags");
		return 0;
	}
	if (enable)
		flags |= FASYNC;
	else
		flags &= ~FASYNC;
	if (fcntl (rtc_fd, F_SETFL, flags) == -1) {
		perror ("setflags");
		return 0;
	}
	return 1;
}
#endif

#ifdef PLATFORM_WIN32
static HANDLE win32_main_thread;
static MMRESULT win32_timer;

static void CALLBACK
win32_time_proc (UINT uID, UINT uMsg, DWORD dwUser, DWORD dw1, DWORD dw2)
{
	CONTEXT context;

	context.ContextFlags = CONTEXT_CONTROL;
	if (GetThreadContext (win32_main_thread, &context)) {
#ifdef _WIN64
		mono_profiler_stat_hit ((guchar *) context.Rip, &context);
#else
		mono_profiler_stat_hit ((guchar *) context.Eip, &context);
#endif
	}
}
#endif

static void
setup_stat_profiler (void)
{
#ifdef ITIMER_PROF
	struct itimerval itval;
	static int inited = 0;
#ifdef HAVE_LINUX_RTC_H
	const char *rtc_freq;
	if (!inited && (rtc_freq = g_getenv ("MONO_RTC"))) {
		int freq = 0;
		inited = 1;
		if (*rtc_freq)
			freq = atoi (rtc_freq);
		if (!freq)
			freq = 1024;
		rtc_fd = open ("/dev/rtc", O_RDONLY);
		if (rtc_fd == -1) {
			perror ("open /dev/rtc");
			return;
		}
		add_signal_handler (SIGPROF, sigprof_signal_handler);
		if (ioctl (rtc_fd, RTC_IRQP_SET, freq) == -1) {
			perror ("set rtc freq");
			return;
		}
		if (ioctl (rtc_fd, RTC_PIE_ON, 0) == -1) {
			perror ("start rtc");
			return;
		}
		if (fcntl (rtc_fd, F_SETSIG, SIGPROF) == -1) {
			perror ("setsig");
			return;
		}
		if (fcntl (rtc_fd, F_SETOWN, getpid ()) == -1) {
			perror ("setown");
			return;
		}
		enable_rtc_timer (TRUE);
		return;
	}
	if (rtc_fd >= 0)
		return;
#endif

	itval.it_interval.tv_usec = 999;
	itval.it_interval.tv_sec = 0;
	itval.it_value = itval.it_interval;
	setitimer (ITIMER_PROF, &itval, NULL);
	if (inited)
		return;
	inited = 1;
	add_signal_handler (SIGPROF, sigprof_signal_handler);
#elif defined (PLATFORM_WIN32)
	static int inited = 0;
	TIMECAPS timecaps;

	if (inited)
		return;

	inited = 1;
	if (timeGetDevCaps (&timecaps, sizeof (timecaps)) != TIMERR_NOERROR)
		return;

	if ((win32_main_thread = OpenThread (READ_CONTROL | THREAD_GET_CONTEXT, FALSE, GetCurrentThreadId ())) == NULL)
		return;

	if (timeBeginPeriod (1) != TIMERR_NOERROR)
		return;

	if ((win32_timer = timeSetEvent (1, 0, win32_time_proc, 0, TIME_PERIODIC)) == 0) {
		timeEndPeriod (1);
		return;
	}
#endif
}

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
		return mono_arch_create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC_VIRTUAL_REMOTING,
			domain, NULL);
	}

	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || 
	    (mono_method_signature (method)->hasthis && (method->klass->marshalbyref || method->klass == mono_defaults.object_class))) {
		nm = mono_marshal_get_remoting_invoke_for_target (method, target);
		addr = mono_compile_method (nm);
	} else {
		addr = mono_compile_method (method);
	}
	return mono_get_addr_from_ftnptr (addr);
}

#ifdef MONO_ARCH_HAVE_IMT
static gpointer
mini_get_imt_trampoline (void)
{
	static gpointer tramp = NULL;
	if (!tramp)
		tramp = mono_create_specific_trampoline (MONO_FAKE_IMT_METHOD, MONO_TRAMPOLINE_JIT, mono_get_root_domain (), NULL);
	return tramp;
}
#endif

#ifdef MONO_ARCH_COMMON_VTABLE_TRAMPOLINE
gpointer
mini_get_vtable_trampoline (void)
{
	static gpointer tramp = NULL;
	if (!tramp)
		tramp = mono_create_specific_trampoline (MONO_FAKE_VTABLE_METHOD, MONO_TRAMPOLINE_JIT, mono_get_root_domain (), NULL);
	return tramp;
}
#endif

static void
mini_parse_debug_options (void)
{
	char *options = getenv ("MONO_DEBUG");
	gchar **args, **ptr;
	
	if (!options)
		return;

	args = g_strsplit (options, ",", -1);

	for (ptr = args; ptr && *ptr; ptr++) {
		const char *arg = *ptr;

		if (!strcmp (arg, "handle-sigint"))
			debug_options.handle_sigint = TRUE;
		else if (!strcmp (arg, "keep-delegates"))
			debug_options.keep_delegates = TRUE;
		else if (!strcmp (arg, "collect-pagefault-stats"))
			debug_options.collect_pagefault_stats = TRUE;
		else if (!strcmp (arg, "break-on-unverified"))
			debug_options.break_on_unverified = TRUE;
		else if (!strcmp (arg, "no-gdb-backtrace"))
			debug_options.no_gdb_backtrace = TRUE;
		else if (!strcmp (arg, "dont-free-domains"))
			mono_dont_free_domains = TRUE;
		else {
			fprintf (stderr, "Invalid option for the MONO_DEBUG env variable: %s\n", arg);
			fprintf (stderr, "Available options: 'handle-sigint', 'keep-delegates', 'collect-pagefault-stats', 'break-on-unverified', 'no-gdb-backtrace', 'dont-free-domains'\n");
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
 
static void
mini_create_jit_domain_info (MonoDomain *domain)
{
	MonoJitDomainInfo *info = g_new0 (MonoJitDomainInfo, 1);

	info->class_init_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jump_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jit_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->delegate_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	domain->runtime_info = info;
}

static void
delete_jump_list (gpointer key, gpointer value, gpointer user_data)
{
	g_slist_free (value);
}

static void
dynamic_method_info_free (gpointer key, gpointer value, gpointer user_data)
{
	MonoJitDynamicMethodInfo *di = value;
	mono_code_manager_destroy (di->code_mp);
	g_free (di);
}

static void
mini_free_jit_domain_info (MonoDomain *domain)
{
	MonoJitDomainInfo *info = domain_jit_info (domain);

	if (info->jump_target_hash) {
		g_hash_table_foreach (info->jump_target_hash, delete_jump_list, NULL);
		g_hash_table_destroy (info->jump_target_hash);
	}
	if (info->jump_target_got_slot_hash) {
		g_hash_table_foreach (info->jump_target_got_slot_hash, delete_jump_list, NULL);
		g_hash_table_destroy (info->jump_target_got_slot_hash);
	}
	if (info->dynamic_code_hash) {
		g_hash_table_foreach (info->dynamic_code_hash, dynamic_method_info_free, NULL);
		g_hash_table_destroy (info->dynamic_code_hash);
	}
	if (info->method_code_hash)
		g_hash_table_destroy (info->method_code_hash);
	g_hash_table_destroy (info->class_init_trampoline_hash);
	g_hash_table_destroy (info->jump_trampoline_hash);
	g_hash_table_destroy (info->jit_trampoline_hash);
	g_hash_table_destroy (info->delegate_trampoline_hash);

	g_free (domain->runtime_info);
	domain->runtime_info = NULL;
}

MonoDomain *
mini_init (const char *filename, const char *runtime_version)
{
	MonoDomain *domain;

	MONO_PROBE_VES_INIT_BEGIN ();

#ifdef __linux__
	if (access ("/proc/self/maps", F_OK) != 0) {
		g_print ("Mono requires /proc to be mounted.\n");
		exit (1);
	}
#endif

	/* Happens when using the embedding interface */
	if (!default_opt_set)
		default_opt = mono_parse_default_optimizations (NULL);

	InitializeCriticalSection (&jit_mutex);

	if (!global_codeman)
		global_codeman = mono_code_manager_new ();
	jit_icall_name_hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);

	mono_arch_cpu_init ();

	mono_arch_init ();

	mono_trampolines_init ();

	if (!g_thread_supported ())
		g_thread_init (NULL);

	if (getenv ("MONO_DEBUG") != NULL)
		mini_parse_debug_options ();

	mono_gc_base_init ();

	mono_jit_tls_id = TlsAlloc ();
	setup_jit_tls_data ((gpointer)-1, mono_thread_abort);

#ifndef DISABLE_JIT
	mono_burg_init ();
#endif

	if (default_opt & MONO_OPT_AOT)
		mono_aot_init ();

	mono_runtime_install_handlers ();
	mono_threads_install_cleanup (mini_thread_cleanup);

#ifdef MONO_ARCH_HAVE_NOTIFY_PENDING_EXC
	// This is experimental code so provide an env var to switch it off
	if (getenv ("MONO_DISABLE_PENDING_EXCEPTIONS")) {
		printf ("MONO_DISABLE_PENDING_EXCEPTIONS env var set.\n");
	} else {
		check_for_pending_exc = FALSE;
		mono_threads_install_notify_pending_exc (mono_arch_notify_pending_exc);
	}
#endif

#define JIT_TRAMPOLINES_WORK
#ifdef JIT_TRAMPOLINES_WORK
	mono_install_compile_method (mono_jit_compile_method);
	mono_install_free_method (mono_jit_free_method);
	mono_install_trampoline (mono_create_jit_trampoline);
	mono_install_jump_trampoline (mono_create_jump_trampoline);
	mono_install_remoting_trampoline (mono_jit_create_remoting_trampoline);
	mono_install_delegate_trampoline (mono_create_delegate_trampoline);
	mono_install_create_domain_hook (mini_create_jit_domain_info);
	mono_install_free_domain_hook (mini_free_jit_domain_info);
#endif
#define JIT_INVOKE_WORKS
#ifdef JIT_INVOKE_WORKS
	mono_install_runtime_invoke (mono_jit_runtime_invoke);
#endif
	mono_install_stack_walk (mono_jit_walk_stack);
	mono_install_get_cached_class_info (mono_aot_get_cached_class_info);
	mono_install_get_class_from_name (mono_aot_get_class_from_name);
 	mono_install_jit_info_find_in_aot (mono_aot_find_jit_info);

	if (debug_options.collect_pagefault_stats) {
		mono_aot_set_make_unreadable (TRUE);
	}

	if (runtime_version)
		domain = mono_init_version (filename, runtime_version);
	else
		domain = mono_init_from_assembly (filename, filename);

	if (mono_aot_only) {
		/* The IMT tables are very dynamic thus they are hard to AOT */
		mono_use_imt = FALSE;
		/* This helps catch code allocation requests */
		mono_code_manager_set_read_only (domain->code_mp);
	}

#ifdef MONO_ARCH_HAVE_IMT
	if (mono_use_imt) {
		mono_install_imt_thunk_builder (mono_arch_build_imt_thunk);
		mono_install_imt_trampoline (mini_get_imt_trampoline ());
#if MONO_ARCH_COMMON_VTABLE_TRAMPOLINE
		mono_install_vtable_trampoline (mini_get_vtable_trampoline ());
#endif
	}
#endif

	/* This must come after mono_init () in the aot-only case */
	mono_exceptions_init ();
	mono_install_handler (mono_get_throw_exception ());

	mono_icall_init ();

	mono_add_internal_call ("System.Diagnostics.StackFrame::get_frame_info", 
				ves_icall_get_frame_info);
	mono_add_internal_call ("System.Diagnostics.StackTrace::get_trace", 
				ves_icall_get_trace);
	mono_add_internal_call ("System.Exception::get_trace", 
				ves_icall_System_Exception_get_trace);
	mono_add_internal_call ("System.Security.SecurityFrame::_GetSecurityFrame",
				ves_icall_System_Security_SecurityFrame_GetSecurityFrame);
	mono_add_internal_call ("System.Security.SecurityFrame::_GetSecurityStack",
				ves_icall_System_Security_SecurityFrame_GetSecurityStack);
	mono_add_internal_call ("Mono.Runtime::mono_runtime_install_handlers", 
				mono_runtime_install_handlers);


	create_helper_signature ();

#define JIT_CALLS_WORK
#ifdef JIT_CALLS_WORK
	/* Needs to be called here since register_jit_icall depends on it */
	mono_marshal_init ();

	mono_arch_register_lowlevel_calls ();
	register_icall (mono_profiler_method_enter, "mono_profiler_method_enter", NULL, TRUE);
	register_icall (mono_profiler_method_leave, "mono_profiler_method_leave", NULL, TRUE);
	register_icall (mono_trace_enter_method, "mono_trace_enter_method", NULL, TRUE);
	register_icall (mono_trace_leave_method, "mono_trace_leave_method", NULL, TRUE);
	register_icall (mono_get_lmf_addr, "mono_get_lmf_addr", "ptr", TRUE);
	register_icall (mono_jit_thread_attach, "mono_jit_thread_attach", "void", TRUE);
	register_icall (mono_domain_get, "mono_domain_get", "ptr", TRUE);

	register_icall (mono_get_throw_exception (), "mono_arch_throw_exception", "void object", TRUE);
	register_icall (mono_get_rethrow_exception (), "mono_arch_rethrow_exception", "void object", TRUE);
	register_icall (mono_get_throw_exception_by_name (), "mono_arch_throw_exception_by_name", "void ptr", TRUE); 
#if MONO_ARCH_HAVE_THROW_CORLIB_EXCEPTION
	register_icall (mono_get_throw_corlib_exception (), "mono_arch_throw_corlib_exception", 
				 "void ptr", TRUE);
#endif
	register_icall (mono_thread_get_undeniable_exception, "mono_thread_get_undeniable_exception", "object", FALSE);
	register_icall (mono_thread_interruption_checkpoint, "mono_thread_interruption_checkpoint", "void", FALSE);
	register_icall (mono_thread_force_interruption_checkpoint, "mono_thread_force_interruption_checkpoint", "void", FALSE);
	register_icall (mono_load_remote_field_new, "mono_load_remote_field_new", "object object ptr ptr", FALSE);
	register_icall (mono_store_remote_field_new, "mono_store_remote_field_new", "void object ptr ptr object", FALSE);

	/* 
	 * NOTE, NOTE, NOTE, NOTE:
	 * when adding emulation for some opcodes, remember to also add a dummy
	 * rule to the burg files, because we need the arity information to be correct.
	 */
#ifndef MONO_ARCH_NO_EMULATE_LONG_MUL_OPTS
	mono_register_opcode_emulation (OP_LMUL, "__emul_lmul", "long long long", mono_llmult, TRUE);
	mono_register_opcode_emulation (OP_LDIV, "__emul_ldiv", "long long long", mono_lldiv, FALSE);
	mono_register_opcode_emulation (OP_LDIV_UN, "__emul_ldiv_un", "long long long", mono_lldiv_un, FALSE);
	mono_register_opcode_emulation (OP_LREM, "__emul_lrem", "long long long", mono_llrem, FALSE);
	mono_register_opcode_emulation (OP_LREM_UN, "__emul_lrem_un", "long long long", mono_llrem_un, FALSE);
	mono_register_opcode_emulation (OP_LMUL_OVF_UN, "__emul_lmul_ovf_un", "long long long", mono_llmult_ovf_un, FALSE);
	mono_register_opcode_emulation (OP_LMUL_OVF, "__emul_lmul_ovf", "long long long", mono_llmult_ovf, FALSE);
#endif

#ifndef MONO_ARCH_NO_EMULATE_LONG_SHIFT_OPS
	mono_register_opcode_emulation (OP_LSHL, "__emul_lshl", "long long int32", mono_lshl, TRUE);
	mono_register_opcode_emulation (OP_LSHR, "__emul_lshr", "long long int32", mono_lshr, TRUE);
	mono_register_opcode_emulation (OP_LSHR_UN, "__emul_lshr_un", "long long int32", mono_lshr_un, TRUE);
#endif

#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_EMULATE_DIV)
	mono_register_opcode_emulation (CEE_DIV, "__emul_idiv", "int32 int32 int32", mono_idiv, FALSE);
	mono_register_opcode_emulation (CEE_DIV_UN, "__emul_idiv_un", "int32 int32 int32", mono_idiv_un, FALSE);
	mono_register_opcode_emulation (CEE_REM, "__emul_irem", "int32 int32 int32", mono_irem, FALSE);
	mono_register_opcode_emulation (CEE_REM_UN, "__emul_irem_un", "int32 int32 int32", mono_irem_un, FALSE);
	mono_register_opcode_emulation (OP_IDIV, "__emul_op_idiv", "int32 int32 int32", mono_idiv, FALSE);
	mono_register_opcode_emulation (OP_IDIV_UN, "__emul_op_idiv_un", "int32 int32 int32", mono_idiv_un, FALSE);
	mono_register_opcode_emulation (OP_IREM, "__emul_op_irem", "int32 int32 int32", mono_irem, FALSE);
	mono_register_opcode_emulation (OP_IREM_UN, "__emul_op_irem_un", "int32 int32 int32", mono_irem_un, FALSE);
#endif

#ifdef MONO_ARCH_EMULATE_MUL_DIV
	mono_register_opcode_emulation (CEE_MUL_OVF, "__emul_imul_ovf", "int32 int32 int32", mono_imul_ovf, FALSE);
	mono_register_opcode_emulation (CEE_MUL_OVF_UN, "__emul_imul_ovf_un", "int32 int32 int32", mono_imul_ovf_un, FALSE);
	mono_register_opcode_emulation (CEE_MUL, "__emul_imul", "int32 int32 int32", mono_imul, TRUE);
	mono_register_opcode_emulation (OP_IMUL, "__emul_op_imul", "int32 int32 int32", mono_imul, TRUE);
	mono_register_opcode_emulation (OP_IMUL_OVF, "__emul_op_imul_ovf", "int32 int32 int32", mono_imul_ovf, FALSE);
	mono_register_opcode_emulation (OP_IMUL_OVF_UN, "__emul_op_imul_ovf_un", "int32 int32 int32", mono_imul_ovf_un, FALSE);
#endif
#if defined(MONO_ARCH_EMULATE_MUL_DIV) || defined(MONO_ARCH_SOFT_FLOAT)
	mono_register_opcode_emulation (OP_FDIV, "__emul_fdiv", "double double double", mono_fdiv, FALSE);
#endif

	mono_register_opcode_emulation (OP_FCONV_TO_U8, "__emul_fconv_to_u8", "ulong double", mono_fconv_u8, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_U4, "__emul_fconv_to_u4", "uint32 double", mono_fconv_u4, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_OVF_I8, "__emul_fconv_to_ovf_i8", "long double", mono_fconv_ovf_i8, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_OVF_U8, "__emul_fconv_to_ovf_u8", "ulong double", mono_fconv_ovf_u8, FALSE);

#ifdef MONO_ARCH_EMULATE_FCONV_TO_I8
	mono_register_opcode_emulation (OP_FCONV_TO_I8, "__emul_fconv_to_i8", "long double", mono_fconv_i8, FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_CONV_R8_UN
	mono_register_opcode_emulation (CEE_CONV_R_UN, "__emul_conv_r_un", "double int32", mono_conv_to_r8_un, FALSE);
	mono_register_opcode_emulation (OP_ICONV_TO_R_UN, "__emul_iconv_to_r_un", "double int32", mono_conv_to_r8_un, FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_LCONV_TO_R8
	mono_register_opcode_emulation (OP_LCONV_TO_R8, "__emul_lconv_to_r8", "double long", mono_lconv_to_r8, FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_LCONV_TO_R4
	mono_register_opcode_emulation (OP_LCONV_TO_R4, "__emul_lconv_to_r4", "float long", mono_lconv_to_r4, FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_LCONV_TO_R8_UN
	mono_register_opcode_emulation (OP_LCONV_TO_R_UN, "__emul_lconv_to_r8_un", "double long", mono_lconv_to_r8_un, FALSE);
#endif
#ifdef MONO_ARCH_EMULATE_FREM
	mono_register_opcode_emulation (OP_FREM, "__emul_frem", "double double double", fmod, FALSE);
#endif

#ifdef MONO_ARCH_SOFT_FLOAT
	mono_register_opcode_emulation (OP_FSUB, "__emul_fsub", "double double double", mono_fsub, FALSE);
	mono_register_opcode_emulation (OP_FADD, "__emul_fadd", "double double double", mono_fadd, FALSE);
	mono_register_opcode_emulation (OP_FMUL, "__emul_fmul", "double double double", mono_fmul, FALSE);
	mono_register_opcode_emulation (OP_FNEG, "__emul_fneg", "double double", mono_fneg, FALSE);
	mono_register_opcode_emulation (CEE_CONV_R8, "__emul_conv_r8", "double int32", mono_conv_to_r8, FALSE);
	mono_register_opcode_emulation (OP_ICONV_TO_R8, "__emul_iconv_to_r8", "double int32", mono_conv_to_r8, FALSE);
	mono_register_opcode_emulation (CEE_CONV_R4, "__emul_conv_r4", "double int32", mono_conv_to_r4, FALSE);
	mono_register_opcode_emulation (OP_ICONV_TO_R4, "__emul_iconv_to_r4", "double int32", mono_conv_to_r4, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_R4, "__emul_fconv_to_r4", "double double", mono_fconv_r4, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_I1, "__emul_fconv_to_i1", "int8 double", mono_fconv_i1, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_I2, "__emul_fconv_to_i2", "int16 double", mono_fconv_i2, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_I4, "__emul_fconv_to_i4", "int32 double", mono_fconv_i4, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_U1, "__emul_fconv_to_u1", "uint8 double", mono_fconv_u1, FALSE);
	mono_register_opcode_emulation (OP_FCONV_TO_U2, "__emul_fconv_to_u2", "uint16 double", mono_fconv_u2, FALSE);

	mono_register_opcode_emulation (OP_FBEQ, "__emul_fcmp_eq", "uint32 double double", mono_fcmp_eq, FALSE);
	mono_register_opcode_emulation (OP_FBLT, "__emul_fcmp_lt", "uint32 double double", mono_fcmp_lt, FALSE);
	mono_register_opcode_emulation (OP_FBGT, "__emul_fcmp_gt", "uint32 double double", mono_fcmp_gt, FALSE);
	mono_register_opcode_emulation (OP_FBLE, "__emul_fcmp_le", "uint32 double double", mono_fcmp_le, FALSE);
	mono_register_opcode_emulation (OP_FBGE, "__emul_fcmp_ge", "uint32 double double", mono_fcmp_ge, FALSE);
	mono_register_opcode_emulation (OP_FBNE_UN, "__emul_fcmp_ne_un", "uint32 double double", mono_fcmp_ne_un, FALSE);
	mono_register_opcode_emulation (OP_FBLT_UN, "__emul_fcmp_lt_un", "uint32 double double", mono_fcmp_lt_un, FALSE);
	mono_register_opcode_emulation (OP_FBGT_UN, "__emul_fcmp_gt_un", "uint32 double double", mono_fcmp_gt_un, FALSE);
	mono_register_opcode_emulation (OP_FBLE_UN, "__emul_fcmp_le_un", "uint32 double double", mono_fcmp_le_un, FALSE);
	mono_register_opcode_emulation (OP_FBGE_UN, "__emul_fcmp_ge_un", "uint32 double double", mono_fcmp_ge_un, FALSE);

	mono_register_opcode_emulation (OP_FCEQ, "__emul_fcmp_ceq", "uint32 double double", mono_fceq, FALSE);
	mono_register_opcode_emulation (OP_FCGT, "__emul_fcmp_cgt", "uint32 double double", mono_fcgt, FALSE);
	mono_register_opcode_emulation (OP_FCGT_UN, "__emul_fcmp_cgt_un", "uint32 double double", mono_fcgt_un, FALSE);
	mono_register_opcode_emulation (OP_FCLT, "__emul_fcmp_clt", "uint32 double double", mono_fclt, FALSE);
	mono_register_opcode_emulation (OP_FCLT_UN, "__emul_fcmp_clt_un", "uint32 double double", mono_fclt_un, FALSE);

	register_icall (mono_fload_r4, "mono_fload_r4", "double ptr", FALSE);
	register_icall (mono_fstore_r4, "mono_fstore_r4", "void double ptr", FALSE);
	register_icall (mono_fload_r4_arg, "mono_fload_r4_arg", "uint32 double", FALSE);
#endif

#if SIZEOF_VOID_P == 4
	mono_register_opcode_emulation (OP_FCONV_TO_U, "__emul_fconv_to_u", "uint32 double", mono_fconv_u4, TRUE);
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
	register_icall (mono_helper_stelem_ref_check, "helper_stelem_ref_check", "void object object", FALSE);
	register_icall (mono_object_new, "mono_object_new", "object ptr ptr", FALSE);
	register_icall (mono_object_new_specific, "mono_object_new_specific", "object ptr", FALSE);
	register_icall (mono_array_new, "mono_array_new", "object ptr ptr int32", FALSE);
	register_icall (mono_array_new_specific, "mono_array_new_specific", "object ptr int32", FALSE);
	register_icall (mono_runtime_class_init, "mono_runtime_class_init", "void ptr", FALSE);
	register_icall (mono_ldftn, "mono_ldftn", "ptr ptr", FALSE);
	register_icall (mono_ldvirtfn, "mono_ldvirtfn", "ptr object ptr", FALSE);
	register_icall (mono_ldvirtfn_gshared, "mono_ldvirtfn_gshared", "ptr object ptr", FALSE);
	register_icall (mono_helper_compile_generic_method, "compile_generic_method", "ptr object ptr ptr", FALSE);
	register_icall (mono_helper_ldstr, "helper_ldstr", "object ptr int", FALSE);
	register_icall (mono_helper_ldstr_mscorlib, "helper_ldstr_mscorlib", "object int", FALSE);
	register_icall (mono_helper_newobj_mscorlib, "helper_newobj_mscorlib", "object int", FALSE);
	register_icall (mono_value_copy, "mono_value_copy", "void ptr ptr ptr", FALSE);
	register_icall (mono_object_castclass, "mono_object_castclass", "object object ptr", FALSE);
	register_icall (mono_break, "mono_break", NULL, TRUE);
	register_icall (mono_create_corlib_exception_0, "mono_create_corlib_exception_0", "object int", TRUE);
	register_icall (mono_create_corlib_exception_1, "mono_create_corlib_exception_1", "object int object", TRUE);
	register_icall (mono_create_corlib_exception_2, "mono_create_corlib_exception_2", "object int object object", TRUE);
	register_icall (mono_array_new_1, "mono_array_new_1", "object ptr int", FALSE);
	register_icall (mono_array_new_2, "mono_array_new_2", "object ptr int int", FALSE);
#endif

	mono_generic_sharing_init ();

#ifdef MONO_ARCH_SIMD_INTRINSICS
	mono_simd_intrinsics_init ();
#endif

	if (mono_compile_aot)
		/* 
		 * Avoid running managed code when AOT compiling, since the platform
		 * might only support aot-only execution.
		 */
		mono_runtime_set_no_exec (TRUE);

#define JIT_RUNTIME_WORKS
#ifdef JIT_RUNTIME_WORKS
	mono_install_runtime_cleanup ((MonoDomainFunc)mini_cleanup);
	mono_runtime_init (domain, mono_thread_start_cb, mono_thread_attach_cb);
	mono_thread_attach (domain);
#endif

	mono_profiler_runtime_initialized ();
	
	MONO_PROBE_VES_INIT_END ();
	
	return domain;
}

MonoJitStats mono_jit_stats = {0};

static void 
print_jit_stats (void)
{
	if (mono_jit_stats.enabled) {
		g_print ("Mono Jit statistics\n");
		g_print ("Compiled methods:       %ld\n", mono_jit_stats.methods_compiled);
		g_print ("Methods from AOT:       %ld\n", mono_jit_stats.methods_aot);
		g_print ("Methods cache lookup:   %ld\n", mono_jit_stats.methods_lookups);
		g_print ("Method trampolines:     %ld\n", mono_jit_stats.method_trampolines);
		g_print ("Basic blocks:           %ld\n", mono_jit_stats.basic_blocks);
		g_print ("Max basic blocks:       %ld\n", mono_jit_stats.max_basic_blocks);
		g_print ("Allocated vars:         %ld\n", mono_jit_stats.allocate_var);
		g_print ("Compiled CIL code size: %ld\n", mono_jit_stats.cil_code_size);
		g_print ("Native code size:       %ld\n", mono_jit_stats.native_code_size);
		g_print ("Max code size ratio:    %.2f (%s)\n", mono_jit_stats.max_code_size_ratio/100.0,
				 mono_jit_stats.max_ratio_method);
		g_print ("Biggest method:         %ld (%s)\n", mono_jit_stats.biggest_method_size,
				 mono_jit_stats.biggest_method);
		g_print ("Code reallocs:          %ld\n", mono_jit_stats.code_reallocs);
		g_print ("Allocated code size:    %ld\n", mono_jit_stats.allocated_code_size);
		g_print ("Inlineable methods:     %ld\n", mono_jit_stats.inlineable_methods);
		g_print ("Inlined methods:        %ld\n", mono_jit_stats.inlined_methods);
		g_print ("Regvars:                %ld\n", mono_jit_stats.regvars);
		g_print ("Locals stack size:      %ld\n", mono_jit_stats.locals_stack_size);

		g_print ("\nCreated object count:   %ld\n", mono_stats.new_object_count);
		g_print ("Delegates created:      %ld\n", mono_stats.delegate_creations);
		g_print ("Initialized classes:    %ld\n", mono_stats.initialized_class_count);
		g_print ("Used classes:           %ld\n", mono_stats.used_class_count);
		g_print ("Generic vtables:        %ld\n", mono_stats.generic_vtable_count);
		g_print ("Methods:                %ld\n", mono_stats.method_count);
		g_print ("Static data size:       %ld\n", mono_stats.class_static_data_size);
		g_print ("VTable data size:       %ld\n", mono_stats.class_vtable_size);
		g_print ("Mscorlib mempool size:  %d\n", mono_mempool_get_allocated (mono_defaults.corlib->mempool));

		g_print ("\nGeneric instances:      %ld\n", mono_stats.generic_instance_count);
		g_print ("Initialized classes:    %ld\n", mono_stats.generic_class_count);
		g_print ("Inflated methods:       %ld / %ld\n", mono_stats.inflated_method_count_2,
			 mono_stats.inflated_method_count);
		g_print ("Inflated types:         %ld\n", mono_stats.inflated_type_count);
		g_print ("Generics metadata size: %ld\n", mono_stats.generics_metadata_size);
		g_print ("Generics virtual invokes: %ld\n", mono_jit_stats.generic_virtual_invocations);

		g_print ("Sharable generic methods: %ld\n", mono_stats.generics_sharable_methods);
		g_print ("Unsharable generic methods: %ld\n", mono_stats.generics_unsharable_methods);
		g_print ("Shared generic methods: %ld\n", mono_stats.generics_shared_methods);

		g_print ("Dynamic code allocs:    %ld\n", mono_stats.dynamic_code_alloc_count);
		g_print ("Dynamic code bytes:     %ld\n", mono_stats.dynamic_code_bytes_count);
		g_print ("Dynamic code frees:     %ld\n", mono_stats.dynamic_code_frees_count);

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

		g_print ("Hazardous pointers:     %ld\n", mono_stats.hazardous_pointer_count);
#ifdef HAVE_SGEN_GC
		g_print ("Minor GC collections:   %ld\n", mono_stats.minor_gc_count);
		g_print ("Major GC collections:   %ld\n", mono_stats.major_gc_count);
		g_print ("Minor GC time in msecs: %lf\n", (double)mono_stats.minor_gc_time_usecs / 1000.0);
		g_print ("Major GC time in msecs: %lf\n", (double)mono_stats.major_gc_time_usecs / 1000.0);
#endif
		if (mono_security_get_mode () == MONO_SECURITY_MODE_CAS) {
			g_print ("\nDecl security check   : %ld\n", mono_jit_stats.cas_declsec_check);
			g_print ("LinkDemand (user)     : %ld\n", mono_jit_stats.cas_linkdemand);
			g_print ("LinkDemand (icall)    : %ld\n", mono_jit_stats.cas_linkdemand_icall);
			g_print ("LinkDemand (pinvoke)  : %ld\n", mono_jit_stats.cas_linkdemand_pinvoke);
			g_print ("LinkDemand (aptc)     : %ld\n", mono_jit_stats.cas_linkdemand_aptc);
			g_print ("Demand (code gen)     : %ld\n", mono_jit_stats.cas_demand_generation);
		}
		if (debug_options.collect_pagefault_stats) {
			g_print ("AOT pagefaults        : %d\n", mono_aot_get_n_pagefaults ());
		}

		g_free (mono_jit_stats.max_ratio_method);
		mono_jit_stats.max_ratio_method = NULL;
		g_free (mono_jit_stats.biggest_method);
		mono_jit_stats.biggest_method = NULL;
	}
}

void
mini_cleanup (MonoDomain *domain)
{
#ifdef HAVE_LINUX_RTC_H
	if (rtc_fd >= 0)
		enable_rtc_timer (FALSE);
#endif

	/* 
	 * mono_runtime_cleanup() and mono_domain_finalize () need to
	 * be called early since they need the execution engine still
	 * fully working (mono_domain_finalize may invoke managed finalizers
	 * and mono_runtime_cleanup will wait for other threads to finish).
	 */
	mono_domain_finalize (domain, 2000);

	/* This accesses metadata so needs to be called before runtime shutdown */
	print_jit_stats ();

	mono_runtime_cleanup (domain);

	mono_profiler_shutdown ();

	mono_icall_cleanup ();

	mono_runtime_cleanup_handlers ();

	mono_domain_free (domain, TRUE);

	mono_debugger_cleanup ();

	mono_trampolines_cleanup ();

	if (!mono_dont_free_global_codeman)
		mono_code_manager_destroy (global_codeman);
	g_hash_table_destroy (jit_icall_name_hash);
	g_free (emul_opcode_map);

	mono_arch_cleanup ();

	mono_cleanup ();

	mono_trace_cleanup ();

	mono_counters_dump (-1, stdout);

	if (mono_inject_async_exc_method)
		mono_method_desc_free (mono_inject_async_exc_method);

	TlsFree(mono_jit_tls_id);

	DeleteCriticalSection (&jit_mutex);

	DeleteCriticalSection (&mono_delegate_section);
}

void
mono_set_defaults (int verbose_level, guint32 opts)
{
	mini_verbose = verbose_level;
	default_opt = opts;
	default_opt_set = TRUE;
}

/*
 * mono_get_runtime_build_info:
 *
 *   Return the runtime version + build date in string format.
 * The returned string is owned by the caller.
 */
char*
mono_get_runtime_build_info (void)
{
	if (mono_build_date)
		return g_strdup_printf ("%s %s", FULL_VERSION, mono_build_date);
	else
		return g_strdup_printf ("%s", FULL_VERSION);
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
		method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
		if (method->flags & METHOD_ATTRIBUTE_ABSTRACT)
			continue;

		count++;
		if (mini_verbose > 1) {
			char * desc = mono_method_full_name (method, TRUE);
			g_print ("Compiling %d %s\n", count, desc);
			g_free (desc);
		}
		mono_compile_method (method);
		if (strcmp (method->name, "Finalize") == 0) {
			invoke = mono_marshal_get_runtime_invoke (method);
			mono_compile_method (invoke);
		}
		if (method->klass->marshalbyref && mono_method_signature (method)->hasthis) {
			invoke = mono_marshal_get_remoting_invoke_with_check (method);
			mono_compile_method (invoke);
		}
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
