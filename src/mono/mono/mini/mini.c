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
#include <unistd.h>
#include <math.h>
#include <sys/time.h>

#ifdef sun    // Solaris x86
#include <sys/types.h>
#include <sys/ucontext.h>
#endif

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
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
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/security-manager.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/os/gc_wrapper.h>

#include "mini.h"
#include <string.h>
#include <ctype.h>
#include "inssel.h"
#include "trace.h"

#include "jit-icalls.c"

/* 
 * this is used to determine when some branch optimizations are possible: we exclude FP compares
 * because they have weird semantics with NaNs.
 */
#define MONO_IS_COND_BRANCH_OP(ins) (((ins)->opcode >= CEE_BEQ && (ins)->opcode <= CEE_BLT_UN) || ((ins)->opcode >= OP_LBEQ && (ins)->opcode <= OP_LBLT_UN) || ((ins)->opcode >= OP_FBEQ && (ins)->opcode <= OP_FBLT_UN) || ((ins)->opcode >= OP_IBEQ && (ins)->opcode <= OP_IBLT_UN))
#define MONO_IS_COND_BRANCH_NOFP(ins) (MONO_IS_COND_BRANCH_OP(ins) && (ins)->inst_left->inst_left->type != STACK_R8)

#define MONO_CHECK_THIS(ins) (mono_method_signature (cfg->method)->hasthis && (ins)->ssa_op == MONO_SSA_LOAD && (ins)->inst_left->inst_c0 == 0)

static void setup_stat_profiler (void);
gboolean  mono_arch_print_tree(MonoInst *tree, int arity);
static gpointer mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt);
static gpointer mono_jit_compile_method (MonoMethod *method);
static gpointer mono_jit_find_compiled_method (MonoDomain *domain, MonoMethod *method);

static void handle_stobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, MonoInst *src, 
			  const unsigned char *ip, MonoClass *klass, gboolean to_end, gboolean native);

static void dec_foreach (MonoInst *tree, MonoCompile *cfg);

static int mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
		   int locals_offset, MonoInst *return_var, GList *dont_inline, MonoInst **inline_args, 
		   guint inline_offset, gboolean is_virtual_call);

extern guint8 mono_burg_arity [];
/* helper methods signature */
static MonoMethodSignature *helper_sig_class_init_trampoline = NULL;
static MonoMethodSignature *helper_sig_domain_get = NULL;

static guint32 default_opt = 0;

guint32 mono_jit_tls_id = -1;
MonoTraceSpec *mono_jit_trace_calls = NULL;
gboolean mono_break_on_exc = FALSE;
#ifndef DISABLE_AOT
gboolean mono_compile_aot = FALSE;
#endif
gboolean mono_use_security_manager = FALSE;

static int mini_verbose = 0;

static CRITICAL_SECTION jit_mutex;

static GHashTable *class_init_hash_addr = NULL;

static MonoCodeManager *global_codeman = NULL;

static GHashTable *jit_icall_name_hash = NULL;

static MonoDebugOptions debug_options;

/*
 * Address of the trampoline code.  This is used by the debugger to check
 * whether a method is a trampoline.
 */
guint8* mono_trampoline_code [MONO_TRAMPOLINE_NUM];

gboolean
mono_running_on_valgrind (void)
{
#ifdef HAVE_VALGRIND_MEMCHECK_H
		if (RUNNING_ON_VALGRIND)
			return TRUE;
		else
			return FALSE;
#else
		return FALSE;
#endif
}

/*
 * mono_create_ftnptr:
 *
 *   Given a function address, create a function descriptor for it.
 * This is only needed on IA64.
 */
gpointer
mono_create_ftnptr (MonoDomain *domain, gpointer addr)
{
#ifdef __ia64__
	gpointer *desc;

	mono_domain_lock (domain);
	desc = mono_code_manager_reserve (domain->code_mp, 2 * sizeof (gpointer));
	mono_domain_unlock (domain);

	desc [0] = addr;
	desc [1] = NULL;

	return desc;
#else
	return addr;
#endif
}

/* debug function */
G_GNUC_UNUSED static char*
get_method_from_ip (void *ip)
{
	MonoJitInfo *ji;
	char *method;
	char *source;
	char *res;
	MonoDomain *domain = mono_domain_get ();
	
	ji = mono_jit_info_table_find (domain, ip);
	if (!ji) {
		return NULL;
	}
	method = mono_method_full_name (ji->method, TRUE);
	source = mono_debug_source_location_from_address (ji->method, (guint32)((guint8*)ip - (guint8*)ji->code_start), NULL, domain);

	res = g_strdup_printf (" %s + 0x%x (%p %p) [%p - %s]", method, (int)((char*)ip - (char*)ji->code_start), ji->code_start, (char*)ji->code_start + ji->code_size, domain, domain->friendly_name);

	g_free (source);
	g_free (method);

	return res;
}

/* debug function */
G_GNUC_UNUSED static void
print_method_from_ip (void *ip)
{
	MonoJitInfo *ji;
	char *method;
	char *source;
	MonoDomain *domain = mono_domain_get ();
	
	ji = mono_jit_info_table_find (domain, ip);
	if (!ji) {
		g_print ("No method at %p\n", ip);
		return;
	}
	method = mono_method_full_name (ji->method, TRUE);
	source = mono_debug_source_location_from_address (ji->method, (guint32)((guint8*)ip - (guint8*)ji->code_start), NULL, domain);

	g_print ("IP %p at offset 0x%x of method %s (%p %p)[domain %p - %s]\n", ip, (int)((char*)ip - (char*)ji->code_start), method, ji->code_start, (char*)ji->code_start + ji->code_size, domain, domain->friendly_name);

	if (source)
		g_print ("%s\n", source);

	g_free (source);
	g_free (method);
}

G_GNUC_UNUSED void
mono_print_method_from_ip (void *ip)
{
	print_method_from_ip (ip);
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

	if (!global_codeman) {
		/* This can happen during startup */
		global_codeman = mono_code_manager_new ();
		return mono_code_manager_reserve (global_codeman, size);
	}
	else {
		EnterCriticalSection (&jit_mutex);
		ptr = mono_code_manager_reserve (global_codeman, size);
		LeaveCriticalSection (&jit_mutex);
		return ptr;
	}
}

MonoJumpInfoToken *
mono_jump_info_token_new (MonoMemPool *mp, MonoImage *image, guint32 token)
{
	MonoJumpInfoToken *res = mono_mempool_alloc0 (mp, sizeof (MonoJumpInfoToken));
	res->image = image;
	res->token = token;

	return res;
}

#define MONO_INIT_VARINFO(vi,id) do { \
	(vi)->range.first_use.pos.bid = 0xffff; \
	(vi)->reg = -1; \
        (vi)->idx = (id); \
} while (0)

/*
 * Basic blocks have two numeric identifiers:
 * dfn: Depth First Number
 * block_num: unique ID assigned at bblock creation
 */
#define NEW_BBLOCK(cfg) (mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock)))
#define ADD_BBLOCK(cfg,bbhash,b) do {	\
		g_hash_table_insert (bbhash, (b)->cil_code, (b));	\
		(b)->block_num = cfg->num_bblocks++;	\
		(b)->real_offset = real_offset;	\
	} while (0)

#define GET_BBLOCK(cfg,bbhash,tblock,ip) do {	\
		(tblock) = g_hash_table_lookup (bbhash, (ip));	\
		if (!(tblock)) {	\
			if ((ip) >= end || (ip) < header->code) goto unverified; \
			(tblock) = NEW_BBLOCK (cfg);	\
			(tblock)->cil_code = (ip);	\
			ADD_BBLOCK (cfg, (bbhash), (tblock));	\
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
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_PCONST;	\
		(dest)->inst_p0 = (val);	\
		(dest)->type = STACK_PTR;	\
	} while (0)


#ifdef MONO_ARCH_NEED_GOT_VAR

#define NEW_PATCH_INFO(cfg,dest,el1,el2) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_PATCH_INFO;	\
		(dest)->inst_left = (gpointer)(el1);	\
		(dest)->inst_right = (gpointer)(el2);	\
	} while (0)

#define NEW_AOTCONST(cfg,dest,patch_type,cons) do {			\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst)); \
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

#define NEW_AOTCONST_TOKEN(cfg,dest,patch_type,image,token,stack_type) do { \
		MonoInst *group, *got_var, *got_loc;			\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst)); \
		(dest)->opcode = OP_GOT_ENTRY;				\
		got_loc = mono_get_got_var (cfg);			\
		NEW_TEMPLOAD ((cfg), got_var, got_loc->inst_c0);	\
		NEW_PATCH_INFO ((cfg), group, NULL, patch_type);	\
		group->inst_p0 = mono_jump_info_token_new ((cfg)->mempool, (image), (token)); \
		(dest)->inst_p0 = got_var;				\
		(dest)->inst_p1 = group;				\
		(dest)->type = (stack_type);				\
	} while (0)

#else

#define NEW_AOTCONST(cfg,dest,patch_type,cons) do {    \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = cfg->compile_aot ? OP_AOTCONST : OP_PCONST;	\
		(dest)->inst_p0 = (cons);	\
		(dest)->inst_i1 = (gpointer)(patch_type); \
		(dest)->type = STACK_PTR;	\
    } while (0)

#define NEW_AOTCONST_TOKEN(cfg,dest,patch_type,image,token,stack_type) do {    \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_AOTCONST;	\
		(dest)->inst_p0 = mono_jump_info_token_new ((cfg)->mempool, (image), (token));	\
		(dest)->inst_p1 = (gpointer)(patch_type); \
		(dest)->type = (stack_type);	\
    } while (0)

#endif

#define NEW_CLASSCONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_CLASS, (val))

#define NEW_IMAGECONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_IMAGE, (val))

#define NEW_FIELDCONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_FIELD, (val))

#define NEW_METHODCONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_METHODCONST, (val))

#define NEW_VTABLECONST(cfg,dest,vtable) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_VTABLE, cfg->compile_aot ? (gpointer)((vtable)->klass) : (vtable))

#define NEW_SFLDACONST(cfg,dest,val) NEW_AOTCONST ((cfg), (dest), MONO_PATCH_INFO_SFLDA, (val))

#define NEW_LDSTRCONST(cfg,dest,image,token) NEW_AOTCONST_TOKEN ((cfg), (dest), MONO_PATCH_INFO_LDSTR, (image), (token), STACK_OBJ)

#define NEW_TYPE_FROM_HANDLE_CONST(cfg,dest,image,token) NEW_AOTCONST_TOKEN ((cfg), (dest), MONO_PATCH_INFO_TYPE_FROM_HANDLE, (image), (token), STACK_OBJ)

#define NEW_LDTOKENCONST(cfg,dest,image,token) NEW_AOTCONST_TOKEN ((cfg), (dest), MONO_PATCH_INFO_LDTOKEN, (image), (token), STACK_PTR)

#define NEW_DECLSECCONST(cfg,dest,image,entry) do { \
		if (cfg->compile_aot) { \
			NEW_AOTCONST_TOKEN (cfg, dest, MONO_PATCH_INFO_DECLSEC, image, (entry).index, STACK_OBJ); \
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
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_LOAD;	\
		(dest)->inst_i0 = arg_array [(num)];	\
		(dest)->opcode = mono_type_to_ldind ((dest)->inst_i0->inst_vtype);	\
		type_to_eval_stack_type (param_types [(num)], (dest));	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	}} while (0)

#define NEW_LOCLOAD(cfg,dest,num) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_LOAD;	\
		(dest)->inst_i0 = (cfg)->varinfo [locals_offset + (num)];	\
		(dest)->opcode = mono_type_to_ldind ((dest)->inst_i0->inst_vtype);	\
		type_to_eval_stack_type (header->locals [(num)], (dest));	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_LOCLOADA(cfg,dest,num) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_MAYBE_LOAD;	\
		(dest)->inst_i0 = (cfg)->varinfo [locals_offset + (num)];	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->opcode = OP_LDADDR;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
        if (!MONO_TYPE_ISSTRUCT (header->locals [(num)])) \
           (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_RETLOADA(cfg,dest) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_MAYBE_LOAD;	\
		(dest)->inst_i0 = (cfg)->ret;	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->opcode = cfg->ret_var_is_local ? OP_LDADDR : CEE_LDIND_I;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
                (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_ARGLOADA(cfg,dest,num) do {	\
                if (arg_array [(num)]->opcode == OP_ICONST) goto inline_failure; \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_MAYBE_LOAD;	\
		(dest)->inst_i0 = arg_array [(num)];	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->opcode = OP_LDADDR;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
                (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_TEMPLOAD(cfg,dest,num) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_LOAD;	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->opcode = mono_type_to_ldind ((dest)->inst_i0->inst_vtype);	\
		type_to_eval_stack_type ((dest)->inst_i0->inst_vtype, (dest));	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_TEMPLOADA(cfg,dest,num) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_MAYBE_LOAD;	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->opcode = OP_LDADDR;	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (dest)->inst_i0->klass;	\
        if (!MONO_TYPE_ISSTRUCT (cfg->varinfo [(num)]->inst_vtype)) \
           (cfg)->disable_ssa = TRUE; \
	} while (0)


#define NEW_INDLOAD(cfg,dest,addr,vtype) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->inst_left = addr;	\
		(dest)->opcode = mono_type_to_ldind (vtype);	\
		type_to_eval_stack_type (vtype, (dest));	\
		/* FIXME: (dest)->klass = (dest)->inst_i0->klass;*/	\
	} while (0)

#define NEW_INDSTORE(cfg,dest,addr,value,vtype) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->inst_i0 = addr;	\
		(dest)->opcode = mono_type_to_stind (vtype);	\
		(dest)->inst_i1 = (value);	\
		/* FIXME: (dest)->klass = (dest)->inst_i0->klass;*/	\
	} while (0)

#define NEW_TEMPSTORE(cfg,dest,num,inst) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_STORE;	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->opcode = mono_type_to_stind ((dest)->inst_i0->inst_vtype);	\
		(dest)->inst_i1 = (inst);	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_LOCSTORE(cfg,dest,num,inst) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = mono_type_to_stind (header->locals [(num)]);	\
		(dest)->ssa_op = MONO_SSA_STORE;	\
		(dest)->inst_i0 = (cfg)->varinfo [locals_offset + (num)];	\
		(dest)->inst_i1 = (inst);	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_ARGSTORE(cfg,dest,num,inst) do {	\
                if (arg_array [(num)]->opcode == OP_ICONST) goto inline_failure; \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = mono_type_to_stind (param_types [(num)]);	\
		(dest)->ssa_op = MONO_SSA_STORE;	\
		(dest)->inst_i0 = arg_array [(num)];	\
		(dest)->inst_i1 = (inst);	\
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define NEW_DUMMY_USE(cfg,dest,load) do { \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_DUMMY_USE; \
		(dest)->inst_left = (load); \
    } while (0)

#define NEW_DUMMY_STORE(cfg,dest,num) do { \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->inst_i0 = (cfg)->varinfo [(num)];	\
		(dest)->opcode = OP_DUMMY_STORE; \
		(dest)->klass = (dest)->inst_i0->klass;	\
	} while (0)

#define ADD_BINOP(op) do {	\
		MONO_INST_NEW (cfg, ins, (op));	\
		ins->cil_code = ip;	\
		sp -= 2;	\
		ins->inst_i0 = sp [0];	\
		ins->inst_i1 = sp [1];	\
		*sp++ = ins;	\
		type_from_op (ins);	\
		CHECK_TYPE (ins);	\
	} while (0)

#define ADD_UNOP(op) do {	\
		MONO_INST_NEW (cfg, ins, (op));	\
		ins->cil_code = ip;	\
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
		GET_BBLOCK (cfg, bbhash, tblock, target);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_true_bb = tblock;	\
		CHECK_BBLOCK (target, ip, tblock);	\
		if ((next_block)) {	\
			link_bblock (cfg, bblock, (next_block));	\
			ins->inst_false_bb = (next_block);	\
			start_new_bblock = 1;	\
		} else {	\
			GET_BBLOCK (cfg, bbhash, tblock, ip);		\
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
		GET_BBLOCK (cfg, bbhash, tblock, target);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_true_bb = tblock;	\
		CHECK_BBLOCK (target, ip, tblock);	\
		GET_BBLOCK (cfg, bbhash, tblock, ip);		\
		link_bblock (cfg, bblock, tblock);	\
		ins->inst_false_bb = tblock;	\
		start_new_bblock = 2;	\
	} while (0)

#define NEW_LDELEMA(cfg,dest,sp,k) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = CEE_LDELEMA;	\
		(dest)->inst_left = (sp) [0];	\
		(dest)->inst_right = (sp) [1];	\
		(dest)->type = STACK_MP;	\
		(dest)->klass = (k);	\
		(cfg)->flags |= MONO_CFG_HAS_LDELEMA; \
	} while (0)

#define NEW_GROUP(cfg,dest,el1,el2) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_GROUP;	\
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
			if (clause->flags & MONO_EXCEPTION_CLAUSE_FINALLY)
				return ((i + 1) << 8) | MONO_REGION_FINALLY | clause->flags;
			else
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
				handler = g_hash_table_lookup (cfg->bb_hash, header->code + clause->handler_offset);
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
	/*g_print ("visit %d at %p\n", *dfn, start->cil_code);*/
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

typedef struct {
	const guchar *code;
	MonoBasicBlock *best;
} PrevStruct;

static void
previous_foreach (gconstpointer key, gpointer val, gpointer data)
{
	PrevStruct *p = data;
	MonoBasicBlock *bb = val;
	//printf ("FIDPREV %d %p  %p %p %p %p %d %d %d\n", bb->block_num, p->code, bb, p->best, bb->cil_code, p->best->cil_code,
	//bb->method == p->best->method, bb->cil_code < p->code, bb->cil_code > p->best->cil_code);

	if (bb->cil_code && bb->cil_code < p->code && bb->cil_code > p->best->cil_code)
		p->best = bb;
}

static MonoBasicBlock*
find_previous (GHashTable *bb_hash, MonoBasicBlock *start, const guchar *code) {
	PrevStruct p;

	p.code = code;
	p.best = start;

	g_hash_table_foreach (bb_hash, (GHFunc)previous_foreach, &p);
	return p.best;
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
	for (inst = first->code; inst && inst->next; inst = inst->next) {
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

static guint32
reverse_branch_op (guint32 opcode)
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
mono_type_to_ldind (MonoType *type)
{
	if (type->byref)
		return CEE_LDIND_I;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
		return CEE_LDIND_I1;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return CEE_LDIND_U1;
	case MONO_TYPE_I2:
		return CEE_LDIND_I2;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return CEE_LDIND_U2;
	case MONO_TYPE_I4:
		return CEE_LDIND_I4;
	case MONO_TYPE_U4:
		return CEE_LDIND_U4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return CEE_LDIND_I;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return CEE_LDIND_REF;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return CEE_LDIND_I8;
	case MONO_TYPE_R4:
		return CEE_LDIND_R4;
	case MONO_TYPE_R8:
		return CEE_LDIND_R8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		}
		return CEE_LDOBJ;
	case MONO_TYPE_TYPEDBYREF:
		return CEE_LDOBJ;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in type_to_ldind", type->type);
	}
	return -1;
}

guint
mono_type_to_stind (MonoType *type)
{
	if (type->byref)
		return CEE_STIND_I;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return CEE_STIND_I1;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return CEE_STIND_I2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return CEE_STIND_I4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return CEE_STIND_I;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return CEE_STIND_REF;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return CEE_STIND_I8;
	case MONO_TYPE_R4:
		return CEE_STIND_R4;
	case MONO_TYPE_R8:
		return CEE_STIND_R8;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		}
		return CEE_STOBJ;
	case MONO_TYPE_TYPEDBYREF:
		return CEE_STOBJ;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in type_to_stind", type->type);
	}
	return -1;
}

/*
 * Returns the type used in the eval stack when @type is loaded.
 * FIXME: return a MonoType/MonoClass for the byref and VALUETYPE cases.
 */
static void
type_to_eval_stack_type (MonoType *type, MonoInst *inst)
{
	MonoClass *klass;

	if (type->byref) {
		inst->type = STACK_MP;
		return;
	}

	klass = mono_class_from_mono_type (type);

handle_enum:
	switch (type->type) {
	case MONO_TYPE_VOID:
		inst->type = STACK_INV;
		return;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		inst->type = STACK_I4;
		return;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		inst->type = STACK_PTR;
		return;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		inst->type = STACK_OBJ;
		return;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		inst->type = STACK_I8;
		return;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		inst->type = STACK_R8;
		return;
	case MONO_TYPE_VALUETYPE:
		if (type->data.klass->enumtype) {
			type = type->data.klass->enum_basetype;
			goto handle_enum;
		} else {
			inst->klass = klass;
			inst->type = STACK_VTYPE;
			return;
		}
	case MONO_TYPE_TYPEDBYREF:
		inst->klass = mono_defaults.typed_reference_class;
		inst->type = STACK_VTYPE;
		return;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in eval stack type", type->type);
	}
}

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
	{0},
	{0, 1, 0, 1, 0, 0, 4, 0},
	{0, 0, 1, 0, 0, 0, 0, 0},
	{0, 1, 0, 1, 0, 2, 4, 0},
	{0, 0, 0, 0, 1, 0, 0, 0},
	{0, 0, 0, 2, 0, 1, 0, 0},
	{0, 4, 0, 4, 0, 0, 3, 0},
	{0, 0, 0, 0, 0, 0, 0, 0},
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
	0, 0, OP_LCEQ-CEE_CEQ, OP_PCEQ-CEE_CEQ, OP_FCEQ-CEE_CEQ, OP_LCEQ-CEE_CEQ
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
	case OP_CGT:
	case OP_CGT_UN:
	case OP_CLT:
	case OP_CLT_UN:
		ins->type = bin_comp_table [ins->inst_i0->type] [ins->inst_i1->type] ? STACK_I4: STACK_INV;
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
		case STACK_PTR:
		case STACK_MP:
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
	case CEE_CKFINITE:
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
		return;
	default:
		g_error ("opcode 0x%04x not handled in type from op", ins->opcode);
		break;
	}
}

static const char 
ldind_type [] = {
	STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I4, STACK_I8, STACK_MP, STACK_R8, STACK_R8, STACK_OBJ
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

#if 0

static const char
param_table [STACK_MAX] [STACK_MAX] = {
	{0},
};

static int
check_values_to_signature (MonoInst *args, MonoType *this, MonoMethodSignature *sig) {
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

MonoInst*
mono_compile_create_var (MonoCompile *cfg, MonoType *type, int opcode)
{
	MonoInst *inst;
	int num = cfg->num_varinfo;

	if ((num + 1) >= cfg->varinfo_count) {
		cfg->varinfo_count = (cfg->varinfo_count + 2) * 2;
		cfg->varinfo = (MonoInst **)g_realloc (cfg->varinfo, sizeof (MonoInst*) * cfg->varinfo_count);
		cfg->vars = (MonoMethodVar **)g_realloc (cfg->vars, sizeof (MonoMethodVar*) * cfg->varinfo_count);      
	}

	/*g_print ("created temp %d of type 0x%x\n", num, type->type);*/
	mono_jit_stats.allocate_var++;

	MONO_INST_NEW (cfg, inst, opcode);
	inst->inst_c0 = num;
	inst->inst_vtype = type;
	inst->klass = mono_class_from_mono_type (type);
	/* if set to 1 the variable is native */
	inst->unused = 0;

	cfg->varinfo [num] = inst;

	cfg->vars [num] = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoMethodVar));
	MONO_INIT_VARINFO (cfg->vars [num], num);

	cfg->num_varinfo++;
	//g_print ("created temp %d of type %s\n", num, mono_type_get_name (type));
	return inst;
}

/*
 * Transform a MonoInst into a load from the variable of index var_index.
 */
void
mono_compile_make_var_load (MonoCompile *cfg, MonoInst *dest, gssize var_index) {
	memset (dest, 0, sizeof (MonoInst));
	dest->ssa_op = MONO_SSA_LOAD;
	dest->inst_i0 = cfg->varinfo [var_index];
	dest->opcode = mono_type_to_ldind (dest->inst_i0->inst_vtype);
	type_to_eval_stack_type (dest->inst_i0->inst_vtype, dest);
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
	case STACK_MP: return &mono_defaults.int_class->byval_arg;
	case STACK_OBJ: return &mono_defaults.object_class->byval_arg;
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
array_access_to_klass (int opcode)
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
	case CEE_STELEM_REF:
		return mono_defaults.object_class;
	default:
		g_assert_not_reached ();
	}
	return NULL;
}

void
mono_add_ins_to_end (MonoBasicBlock *bb, MonoInst *inst)
{
	MonoInst *prev;
	if (!bb->code) {
		MONO_ADD_INS (bb, inst);
		return;
	}
	switch (bb->last_ins->opcode) {
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
	case CEE_BR:
	case CEE_SWITCH:
		prev = bb->code;
		while (prev->next && prev->next != bb->last_ins)
			prev = prev->next;
		if (prev == bb->code) {
			if (bb->last_ins == bb->code) {
				inst->next = bb->code;
				bb->code = inst;
			} else {
				inst->next = prev->next;
				prev->next = inst;
			}
		} else {
			inst->next = bb->last_ins;
			prev->next = inst;
		}
		break;
	//	g_warning ("handle conditional jump in add_ins_to_end ()\n");
	default:
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
	if (inst->opcode == CEE_STOBJ) {
		NEW_TEMPLOADA (cfg, inst, dest);
		handle_stobj (cfg, bb, inst, load, NULL, inst->klass, TRUE, FALSE);
	} else {
		inst->cil_code = NULL;
		mono_add_ins_to_end (bb, inst);
	}
}

/*
 * We try to share variables when possible
 */
static MonoInst *
mono_compile_get_interface_var (MonoCompile *cfg, int slot, MonoInst *ins)
{
	MonoInst *res;
	int pos, vnum;

	/* inlining can result in deeper stacks */ 
	if (slot >= mono_method_get_header (cfg->method)->max_stack)
		return mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);

	pos = ins->type - 1 + slot * STACK_MAX;

	switch (ins->type) {
	case STACK_I4:
	case STACK_I8:
	case STACK_R8:
	case STACK_PTR:
	case STACK_MP:
	case STACK_OBJ:
		if ((vnum = cfg->intvars [pos]))
			return cfg->varinfo [vnum];
		res = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
		cfg->intvars [pos] = res->inst_c0;
		break;
	default:
		res = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
	}
	return res;
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
 */
static int
handle_stack_args (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **sp, int count) {
	int i, bindex;
	MonoBasicBlock *outb;
	MonoInst *inst, **locals;
	gboolean found;

	if (!count)
		return 0;
	if (cfg->verbose_level > 3)
		g_print ("%d item(s) on exit from B%d\n", count, bb->block_num);
	if (!bb->out_scount) {
		bb->out_scount = count;
		//g_print ("bblock %d has out:", bb->block_num);
		found = FALSE;
		for (i = 0; i < bb->out_count; ++i) {
			outb = bb->out_bb [i];
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
				 */
				if (cfg->inlined_method)
					bb->out_stack [i] = mono_compile_create_var (cfg, type_from_stack_type (sp [i]), OP_LOCAL);
				else
					bb->out_stack [i] = mono_compile_get_interface_var (cfg, i, sp [i]);
			}
		}
	}

	for (i = 0; i < bb->out_count; ++i) {
		outb = bb->out_bb [i];
		if (outb->in_scount)
			continue; /* check they are the same locals */
		outb->in_scount = count;
		outb->in_stack = bb->out_stack;
	}

	locals = bb->out_stack;
	for (i = 0; i < count; ++i) {
		/* add store ops at the end of the bb, before the branch */
		NEW_TEMPSTORE (cfg, inst, locals [i]->inst_c0, sp [i]);
		if (inst->opcode == CEE_STOBJ) {
			NEW_TEMPLOADA (cfg, inst, locals [i]->inst_c0);
			handle_stobj (cfg, bb, inst, sp [i], sp [i]->cil_code, inst->klass, TRUE, FALSE);
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
	
	return 0;
}

static int
ret_type_to_call_opcode (MonoType *type, int calli, int virt)
{
	if (type->byref)
		return calli? OP_CALL_REG: virt? CEE_CALLVIRT: CEE_CALL;

handle_enum:
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
		return calli? OP_CALL_REG: virt? CEE_CALLVIRT: CEE_CALL;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return calli? OP_CALL_REG: virt? CEE_CALLVIRT: CEE_CALL;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return calli? OP_CALL_REG: virt? CEE_CALLVIRT: CEE_CALL;
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
				handle_stobj (cfg, bblock, store, ins, ins->cil_code, temp->klass, FALSE, FALSE);
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
 * Prepare arguments for passing to a function call.
 * Return a non-zero value if the arguments can't be passed to the given
 * signature.
 * The type checks are not yet complete and some conversions may need
 * casts on 32 or 64 bit architectures.
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
			call->inst.opcode = CEE_CALL;
			temp = mono_compile_create_var (cfg, &mono_defaults.string_class->byval_arg, OP_LOCAL);
		} else {
			type_to_eval_stack_type (ret, ins);
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
			temp->unused = sig->pinvoke;

			NEW_TEMPLOADA (cfg, loada, temp->inst_c0);
			if (call->inst.opcode == OP_VCALL)
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

	MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (sig->ret, calli, virtual));
	
	call->inst.cil_code = ip;
	call->args = args;
	call->signature = sig;
	call = mono_arch_call_opcode (cfg, bblock, call, virtual);
	type_to_eval_stack_type (sig->ret, &call->inst);
	
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

inline static int
mono_emit_calli (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethodSignature *sig, 
		 MonoInst **args, MonoInst *addr, const guint8 *ip)
{
	MonoCallInst *call = mono_emit_call_args (cfg, bblock, sig, args, TRUE, FALSE, ip, FALSE);

	call->inst.inst_i0 = addr;

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

	if (!virtual)
		mono_get_got_var (cfg);
	else if (call->method->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
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

	mono_get_got_var (cfg);

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

	mono_get_got_var (cfg);
	return mono_emit_native_call (cfg, bblock, mono_icall_get_wrapper (info), info->sig, args, ip, FALSE, FALSE);
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
	MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (info->sig->ret, FALSE, FALSE));
	ins = (MonoInst*)call;
	
	call->inst.cil_code = tree->cil_code;
	call->args = iargs;
	call->signature = info->sig;

	call = mono_arch_call_opcode (cfg, cfg->cbb, call, FALSE);

	mono_get_got_var (cfg);

	if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
		temp = mono_compile_create_var (cfg, info->sig->ret, OP_LOCAL);
		temp->flags |= MONO_INST_IS_TEMP;
		NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
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

static MonoMethodSignature *
mono_get_element_address_signature (int arity)
{
	static GHashTable *sighash = NULL;
	MonoMethodSignature *res;
	int i;

	EnterCriticalSection (&jit_mutex);
	if (!sighash) {
		sighash = g_hash_table_new (NULL, NULL);
	}
	else if ((res = g_hash_table_lookup (sighash, GINT_TO_POINTER (arity)))) {
		LeaveCriticalSection (&jit_mutex);
		return res;
	}

	res = mono_metadata_signature_alloc (mono_defaults.corlib, arity + 1);

	res->pinvoke = 1;
#ifdef MONO_ARCH_VARARG_ICALLS
	/* Only set this only some archs since not all backends can handle varargs+pinvoke */
	res->call_convention = MONO_CALL_VARARG;
#endif
	res->params [0] = &mono_defaults.array_class->byval_arg; 
	
	for (i = 1; i <= arity; i++)
		res->params [i] = &mono_defaults.int_class->byval_arg;

	res->ret = &mono_defaults.int_class->byval_arg;

	g_hash_table_insert (sighash, GINT_TO_POINTER (arity), res);
	LeaveCriticalSection (&jit_mutex);

	return res;
}

static MonoMethodSignature *
mono_get_array_new_va_signature (int arity)
{
	static GHashTable *sighash = NULL;
	MonoMethodSignature *res;
	int i;

	EnterCriticalSection (&jit_mutex);
	if (!sighash) {
		sighash = g_hash_table_new (NULL, NULL);
	}
	else if ((res = g_hash_table_lookup (sighash, GINT_TO_POINTER (arity)))) {
		LeaveCriticalSection (&jit_mutex);
		return res;
	}

	res = mono_metadata_signature_alloc (mono_defaults.corlib, arity + 1);

	res->pinvoke = 1;
#ifdef MONO_ARCH_VARARG_ICALLS
	/* Only set this only some archs since not all backends can handle varargs+pinvoke */
	res->call_convention = MONO_CALL_VARARG;
#endif

	res->params [0] = &mono_defaults.int_class->byval_arg;	
	for (i = 0; i < arity; i++)
		res->params [i + 1] = &mono_defaults.int_class->byval_arg;

	res->ret = &mono_defaults.int_class->byval_arg;

	g_hash_table_insert (sighash, GINT_TO_POINTER (arity), res);
	LeaveCriticalSection (&jit_mutex);

	return res;
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
handle_stobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, MonoInst *src, const unsigned char *ip, MonoClass *klass, gboolean to_end, gboolean native) {
	MonoInst *iargs [3];
	int n;
	int align = 0;
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

	if ((cfg->opt & MONO_OPT_INTRINS) && !to_end && n <= sizeof (gpointer) * 5) {
		MonoInst *inst;
		if (dest->opcode == OP_LDADDR) {
			/* Keep liveness info correct */
			NEW_DUMMY_STORE (cfg, inst, dest->inst_i0->inst_c0);
			MONO_ADD_INS (bblock, inst);
		}
		MONO_INST_NEW (cfg, inst, OP_MEMCPY);
		inst->inst_left = dest;
		inst->inst_right = src;
		inst->cil_code = ip;
		inst->unused = n;
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
	MonoMethod *memset_method;

	NEW_ICONST (cfg, zero_int32, 0);

	mono_class_init (klass);
	n = mono_class_value_size (klass, NULL);
	MONO_INST_NEW (cfg, ins, 0);
	ins->cil_code = ip;
	ins->inst_left = dest;
	ins->inst_right = zero_int32;
	switch (n) {
	case 1:
		ins->opcode = CEE_STIND_I1;
		MONO_ADD_INS (bblock, ins);
		break;
	case 2:
		ins->opcode = CEE_STIND_I2;
		MONO_ADD_INS (bblock, ins);
		break;
	case 4:
		ins->opcode = CEE_STIND_I4;
		MONO_ADD_INS (bblock, ins);
		break;
	default:
		if (n <= sizeof (gpointer) * 5) {
			ins->opcode = OP_MEMSET;
			ins->inst_imm = 0;
			ins->unused = n;
			MONO_ADD_INS (bblock, ins);
			break;
		}
		memset_method = get_memset_method ();
		handle_loaded_temps (cfg, bblock, stack_start, sp);
		iargs [0] = dest;
		NEW_ICONST (cfg, iargs [1], 0);
		NEW_ICONST (cfg, iargs [2], n);
		mono_emit_method_call_spilled (cfg, bblock, memset_method, memset_method->signature, iargs, ip, NULL);
		break;
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
	} else {
		MonoVTable *vtable = mono_class_vtable (cfg->domain, klass);
		gboolean pass_lw;
		
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
	
static MonoInst *
handle_box (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *val, const guchar *ip, MonoClass *klass)
{
	MonoInst *dest, *vtoffset, *add, *vstore;
	int temp;

	temp = handle_alloc (cfg, bblock, klass, TRUE, ip);
	NEW_TEMPLOAD (cfg, dest, temp);
	NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
	MONO_INST_NEW (cfg, add, OP_PADD);
	add->inst_left = dest;
	add->inst_right = vtoffset;
	add->cil_code = ip;
	add->klass = klass;
	MONO_INST_NEW (cfg, vstore, CEE_STIND_I);
	vstore->opcode = mono_type_to_stind (&klass->byval_arg);
	vstore->cil_code = ip;
	vstore->inst_left = add;
	vstore->inst_right = val;

	if (vstore->opcode == CEE_STOBJ) {
		handle_stobj (cfg, bblock, add, val, ip, klass, FALSE, FALSE);
	} else
		MONO_ADD_INS (bblock, vstore);

	NEW_TEMPLOAD (cfg, dest, temp);
	return dest;
}

static int
handle_array_new (MonoCompile *cfg, MonoBasicBlock *bblock, int rank, MonoInst **sp, unsigned char *ip)
{
	MonoMethodSignature *esig;
	char icall_name [256];
	char *name;
	MonoJitICallInfo *info;

	/* Need to register the icall so it gets an icall wrapper */
	sprintf (icall_name, "ves_array_new_va_%d", rank);

	info = mono_find_jit_icall_by_name (icall_name);
	if (info == NULL) {
		esig = mono_get_array_new_va_signature (rank);
		name = g_strdup (icall_name);
		info = mono_register_jit_icall (mono_array_new_va, name, esig, FALSE);

		EnterCriticalSection (&jit_mutex);
		g_hash_table_insert (jit_icall_name_hash, name, name);
		LeaveCriticalSection (&jit_mutex);
	}

	cfg->flags |= MONO_CFG_HAS_VARARGS;

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

static gboolean
mini_class_is_system_array (MonoClass *klass)
{
	if (klass->parent == mono_defaults.array_class)
		return TRUE;
	else if (mono_defaults.generic_array_class && klass->parent && klass->parent->generic_class)
		return klass->parent->generic_class->container_class == mono_defaults.generic_array_class;
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

	/* its not worth to inline methods with valuetype arguments?? */
	for (i = 0; i < signature->param_count; i++) {
		if (MONO_TYPE_ISSTRUCT (signature->params [i])) {
			return FALSE;
		}
	}

	/*
	 * if we can initialize the class of the method right away, we do,
	 * otherwise we don't allow inlining if the class needs initialization,
	 * since it would mean inserting a call to mono_runtime_class_init()
	 * inside the inlined code
	 */
	if (!(cfg->opt & MONO_OPT_SHARED)) {
		vtable = mono_class_vtable (cfg->domain, method->klass);
		if (method->klass->flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT) {
			if (cfg->run_cctors)
				mono_runtime_class_init (vtable);
		}
		else if (!vtable->initialized && mono_class_needs_cctor_run (method->klass, NULL))
			return FALSE;
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

	/* also consider num_locals? */
	if (getenv ("MONO_INLINELIMIT")) {
		if (header->code_size < atoi (getenv ("MONO_INLINELIMIT"))) {
			return TRUE;
		}
	} else if (header->code_size < 20)
		return TRUE;

	return FALSE;
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
	MonoMethodSignature *esig;
	char icall_name [256];
	char *name;
	MonoJitICallInfo *info;

	rank = mono_method_signature (cmethod)->param_count - (is_set? 1: 0);

	if (rank == 2 && (cfg->opt & MONO_OPT_INTRINS)) {
#ifdef MONO_ARCH_EMULATE_MUL_DIV
		/* OP_LDELEMA2D depends on OP_LMUL */
#else
		MonoInst *indexes;
		NEW_GROUP (cfg, indexes, sp [1], sp [2]);
		MONO_INST_NEW (cfg, addr, OP_LDELEMA2D);
		addr->inst_left = sp [0];
		addr->inst_right = indexes;
		addr->cil_code = ip;
		addr->type = STACK_MP;
		addr->klass = cmethod->klass;
		return addr;
#endif
	}

	/* Need to register the icall so it gets an icall wrapper */
	sprintf (icall_name, "ves_array_element_address_%d", rank);

	info = mono_find_jit_icall_by_name (icall_name);
	if (info == NULL) {
		esig = mono_get_element_address_signature (rank);
		name = g_strdup (icall_name);
		info = mono_register_jit_icall (ves_array_element_address, name, esig, FALSE);

		EnterCriticalSection (&jit_mutex);
		g_hash_table_insert (jit_icall_name_hash, name, name);
		LeaveCriticalSection (&jit_mutex);
	}

	temp = mono_emit_native_call (cfg, bblock, mono_icall_get_wrapper (info), info->sig, sp, ip, FALSE, FALSE);
	cfg->flags |= MONO_CFG_HAS_VARARGS;

	NEW_TEMPLOAD (cfg, addr, temp);
	return addr;
}

static MonoJitICallInfo **emul_opcode_map = NULL;

static inline MonoJitICallInfo *
mono_find_jit_opcode_emulation (int opcode)
{
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
 		if (cmethod->name [0] != 'g')
 			return NULL;
 
		if (strcmp (cmethod->name, "get_Chars") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_GETCHR);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
			return ins;
		} else if (strcmp (cmethod->name, "get_Length") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_STRLEN);
			ins->inst_i0 = args [0];
			return ins;
		} else 
			return NULL;
	} else if (cmethod->klass == mono_defaults.object_class) {
		if (strcmp (cmethod->name, "GetType") == 0) {
 			MONO_INST_NEW (cfg, ins, OP_GETTYPE);
			ins->inst_i0 = args [0];
			return ins;
		} else if (strcmp (cmethod->name, "InternalGetHashCode") == 0) {
#ifdef MONO_ARCH_EMULATE_MUL_DIV
		/* The OP_GETHASHCODE rule depends on OP_MUL */
#else
 			MONO_INST_NEW (cfg, ins, OP_GETHASHCODE);
			ins->inst_i0 = args [0];
			return ins;
#endif
		} else if (strcmp (cmethod->name, ".ctor") == 0) {
 			MONO_INST_NEW (cfg, ins, CEE_NOP);
			return ins;
		} else
			return NULL;
	} else if (mini_class_is_system_array (cmethod->klass)) {
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
		return NULL;
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
			if (store->opcode == CEE_STOBJ) {
				NEW_TEMPLOADA (cfg, store, temp->inst_c0);
				handle_stobj (cfg, bblock, store, *sp, sp [0]->cil_code, temp->klass, FALSE, FALSE);
			} else {
				MONO_ADD_INS (bblock, store);
			} 
		}
		sp++;
	}
}

static int
inline_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoBasicBlock *bblock, MonoInst **sp,
		guchar *ip, guint real_offset, GList *dont_inline, MonoBasicBlock **last_b, gboolean inline_allways)
{
	MonoInst *ins, *rvar = NULL;
	MonoMethodHeader *cheader;
	MonoBasicBlock *ebblock, *sbblock;
	int i, costs, new_locals_offset;
	MonoMethod *prev_inlined_method;

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

	costs = mono_method_to_ir (cfg, cmethod, sbblock, ebblock, new_locals_offset, rvar, dont_inline, sp, real_offset, *ip == CEE_CALLVIRT);

	cfg->inlined_method = prev_inlined_method;

	if ((costs >= 0 && costs < 60) || inline_allways) {
		if (cfg->verbose_level > 2)
			g_print ("INLINE END %s -> %s\n", mono_method_full_name (cfg->method, TRUE), mono_method_full_name (cmethod, TRUE));
		
		mono_jit_stats.inlined_methods++;

		/* always add some code to avoid block split failures */
		MONO_INST_NEW (cfg, ins, CEE_NOP);
		MONO_ADD_INS (bblock, ins);
		ins->cil_code = ip;

		bblock->next_bb = sbblock;
		link_bblock (cfg, bblock, sbblock);

		if (rvar) {
			NEW_TEMPLOAD (cfg, ins, rvar->inst_c0);
			*sp++ = ins;
		}
		*last_b = ebblock;
		return costs + 1;
	} else {
		if (cfg->verbose_level > 2)
			g_print ("INLINE ABORTED %s\n", mono_method_full_name (cmethod, TRUE));
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
 * TODO:
 * * consider using an array instead of an hash table (bb_hash)
 */

#define CHECK_TYPE(ins) if (!(ins)->type) goto unverified
#define CHECK_STACK(num) if ((sp - stack_start) < (num)) goto unverified
#define CHECK_STACK_OVF(num) if (((sp - stack_start) + (num)) > header->max_stack) goto unverified
#define CHECK_ARG(num) if ((unsigned)(num) >= (unsigned)num_args) goto unverified
#define CHECK_LOCAL(num) if ((unsigned)(num) >= (unsigned)header->num_locals) goto unverified
#define CHECK_OPSIZE(size) if (ip + size > end) goto unverified


/* offset from br.s -> br like opcodes */
#define BIG_BRANCH_OFFSET 13

static gboolean
ip_in_bb (MonoCompile *cfg, MonoBasicBlock *bb, const guint8* ip)
{
	MonoBasicBlock *b = g_hash_table_lookup (cfg->bb_hash, ip);
	
	return b == NULL || b == bb;
}

static int
get_basic_blocks (MonoCompile *cfg, GHashTable *bbhash, MonoMethodHeader* header, guint real_offset, unsigned char *start, unsigned char *end, unsigned char **pos)
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
			goto unverified;
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
			GET_BBLOCK (cfg, bbhash, bblock, target);
			ip += 2;
			if (ip < end)
				GET_BBLOCK (cfg, bbhash, bblock, ip);
			break;
		case MonoInlineBrTarget:
			target = start + cli_addr + 5 + (gint32)read32 (ip + 1);
			GET_BBLOCK (cfg, bbhash, bblock, target);
			ip += 5;
			if (ip < end)
				GET_BBLOCK (cfg, bbhash, bblock, ip);
			break;
		case MonoInlineSwitch: {
			guint32 n = read32 (ip + 1);
			guint32 j;
			ip += 5;
			cli_addr += 5 + 4 * n;
			target = start + cli_addr;
			GET_BBLOCK (cfg, bbhash, bblock, target);
			
			for (j = 0; j < n; ++j) {
				target = start + cli_addr + (gint32)read32 (ip);
				GET_BBLOCK (cfg, bbhash, bblock, target);
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
			while ((bb_start > start) && !bblock) {
				bblock = g_hash_table_lookup (bbhash, (bb_start));
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
		(CODE_IS_STLOC (ip_next) || *ip_next == CEE_BRTRUE || *ip_next == CEE_BRFALSE ||
		*ip_next == CEE_BRTRUE_S || *ip_next == CEE_BRFALSE_S || *ip_next == CEE_RET))
			return ins;
	
	temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
	temp->flags |= MONO_INST_IS_TEMP;
	NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
	store->cil_code = ins->cil_code;
	MONO_ADD_INS (bblock, store);
	NEW_TEMPLOAD (cfg, load, temp->inst_c0);
	load->cil_code = ins->cil_code;
	return load;
}

static inline MonoMethod *
mini_get_method (MonoMethod *m, guint32 token, MonoClass *klass, MonoGenericContext *context)
{
	MonoMethod *method;

	if (m->wrapper_type != MONO_WRAPPER_NONE)
		return mono_method_get_wrapper_data (m, token);

	method = mono_get_method_full (m->klass->image, token, klass, context);

	if (method->is_inflated)
		method = mono_get_inflated_method (method);

	return method;
}

static
void check_linkdemand (MonoCompile *cfg, MonoMethod *caller, MonoMethod *callee, MonoBasicBlock *bblock, unsigned char *ip)
{
	guint32 result = mono_declsec_linkdemand (cfg->domain, caller, callee);
	if (result == MONO_JIT_SECURITY_OK)
		return;

	if (result == MONO_JIT_LINKDEMAND_ECMA) {
		/* Generate code to throw a SecurityException before the actual call/link */
		MonoAssembly *assembly = mono_image_get_assembly (caller->klass->image);
		MonoReflectionAssembly *refass = (MonoReflectionAssembly*) mono_assembly_get_object (cfg->domain, assembly);
		MonoReflectionMethod *refmet = mono_method_get_object (cfg->domain, caller, NULL);
		MonoSecurityManager *secman = mono_security_manager_get_methods ();
		MonoInst *args [3];

		NEW_ICONST (cfg, args [0], 4);
		NEW_PCONST (cfg, args [1], refass);
		NEW_PCONST (cfg, args [2], refmet);
		mono_emit_method_call_spilled (cfg, bblock, secman->linkdemandsecurityexception, mono_method_signature (secman->linkdemandsecurityexception), args, ip, NULL);
	} else if (cfg->exception_type == MONO_EXCEPTION_NONE) {
		 /* don't hide previous results */
		cfg->exception_type = MONO_EXCEPTION_SECURITY_LINKDEMAND;
		cfg->exception_data = result;
	}
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
	GHashTable *bbhash;
	MonoMethod *cmethod;
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
	int i, n, start_new_bblock, align;
	int num_calls = 0, inline_costs = 0;
	int breakpoint_id = 0;
	guint real_offset, num_args;
	MonoBoolean security, pinvoke;
	MonoSecurityManager* secman = NULL;
	MonoDeclSecurityActions actions;
	GSList *class_inits = NULL;

	image = method->klass->image;
	header = mono_method_get_header (method);
	generic_container = ((MonoMethodNormal *)method)->generic_container;
	sig = mono_method_signature (method);
	num_args = sig->hasthis + sig->param_count;
	ip = (unsigned char*)header->code;
	end = ip + header->code_size;
	mono_jit_stats.cil_code_size += header->code_size;

	if (sig->is_inflated)
		generic_context = ((MonoMethodInflated *) method)->context;
	else if (generic_container)
		generic_context = &generic_container->context;

	g_assert (!sig->has_type_parameters);

	if (cfg->method == method) {
		real_offset = 0;
		bbhash = cfg->bb_hash;
	} else {
		real_offset = inline_offset;
		bbhash = g_hash_table_new (g_direct_hash, NULL);
	}

	if (cfg->verbose_level > 2)
		g_print ("method to IR %s\n", mono_method_full_name (method, TRUE));

	dont_inline = g_list_prepend (dont_inline, method);
	if (cfg->method == method) {

		if (cfg->method->save_lmf)
			/* Needed by the prolog code */
			mono_get_got_var (cfg);

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
			//unsigned char *p = ip;
			MonoExceptionClause *clause = &header->clauses [i];
			GET_BBLOCK (cfg, bbhash, tblock, ip + clause->try_offset);
			tblock->real_offset = clause->try_offset;
			GET_BBLOCK (cfg, bbhash, tblock, ip + clause->handler_offset);
			tblock->real_offset = clause->handler_offset;

			if (clause->flags == MONO_EXCEPTION_CLAUSE_FINALLY ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				MONO_INST_NEW (cfg, ins, OP_START_HANDLER);
				MONO_ADD_INS (tblock, ins);
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
					GET_BBLOCK (cfg, bbhash, tblock, ip + clause->data.filter_offset);
					tblock->real_offset = clause->data.filter_offset;
					tblock->in_scount = 1;
					tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
					/* The filter block shares the exvar with the handler block */
					tblock->in_stack [0] = mono_create_exvar_for_offset (cfg, clause->handler_offset);
					MONO_INST_NEW (cfg, ins, OP_START_HANDLER);
					MONO_ADD_INS (tblock, ins);
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

	ADD_BBLOCK (cfg, bbhash, bblock);

	if (cfg->method == method) {
		breakpoint_id = mono_debugger_method_has_breakpoint (method);
		if (breakpoint_id && (mono_debug_format != MONO_DEBUG_FORMAT_DEBUGGER)) {
			MONO_INST_NEW (cfg, ins, CEE_BREAK);
			MONO_ADD_INS (bblock, ins);
		}
	}

	if (mono_use_security_manager)
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

			if (pinvoke) {
				custom = mono_custom_attrs_from_class (wrapped->klass);
				if (custom && mono_custom_attrs_has_attr (custom, secman->suppressunmanagedcodesecurity)) {
					pinvoke = FALSE;
				}
			}
		} else {
			/* not a P/Invoke after all */
			pinvoke = FALSE;
		}
	}
	
	if ((header->init_locals || (cfg->method == method && (cfg->opt & MONO_OPT_SHARED))) || mono_compile_aot || security || pinvoke) {
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

	if (get_basic_blocks (cfg, bbhash, header, real_offset, ip, end, &err_pos)) {
		ip = err_pos;
		goto unverified;
	}

	if (cfg->method == method)
		mono_debug_init_method (cfg, bblock, breakpoint_id);

	param_types = mono_mempool_alloc (cfg->mempool, sizeof (MonoType*) * num_args);
	if (sig->hasthis)
		param_types [0] = method->klass->valuetype?&method->klass->this_arg:&method->klass->byval_arg;
	for (n = 0; n < sig->param_count; ++n)
		param_types [n + sig->hasthis] = sig->params [n];
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

		if (start_new_bblock) {
			bblock->cil_length = ip - bblock->cil_code;
			if (start_new_bblock == 2) {
				g_assert (ip == tblock->cil_code);
			} else {
				GET_BBLOCK (cfg, bbhash, tblock, ip);
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
			if ((tblock = g_hash_table_lookup (bbhash, ip)) && (tblock != bblock)) {
				link_bblock (cfg, bblock, tblock);
				if (sp != stack_start) {
					handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
					sp = stack_start;
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
			store->cil_code = ip;
			store->inst_left = ins;
			store->inst_right = one;

			MONO_ADD_INS (bblock, store);
		}

		if (cfg->verbose_level > 3)
			g_print ("converting (in B%d: stack: %d) %s", bblock->block_num, (int)(sp - stack_start), mono_disasm_code_one (NULL, method, ip, NULL));

		switch (*ip) {
		case CEE_NOP:
		case CEE_BREAK:
			MONO_INST_NEW (cfg, ins, *ip);
			ins->cil_code = ip++;
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
			ins->cil_code = ip++;
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
			ins->cil_code = ip++;
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
			ins->cil_code = ip;
			if (ins->opcode == CEE_STOBJ) {
				NEW_LOCLOADA (cfg, ins, n);
				handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
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
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDARGA_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_ARG (ip [1]);
			NEW_ARGLOADA (cfg, ins, ip [1]);
			ins->cil_code = ip;
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
			ins->cil_code = ip;
			if (ins->opcode == CEE_STOBJ) {
				NEW_ARGLOADA (cfg, ins, ip [1]);
				handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
			} else
				MONO_ADD_INS (bblock, ins);
			ip += 2;
			break;
		case CEE_LDLOC_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_LOCAL (ip [1]);
			NEW_LOCLOAD (cfg, ins, ip [1]);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDLOCA_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			CHECK_LOCAL (ip [1]);
			NEW_LOCLOADA (cfg, ins, ip [1]);
			ins->cil_code = ip;
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
			ins->cil_code = ip;
			if (ins->opcode == CEE_STOBJ) {
				NEW_LOCLOADA (cfg, ins, ip [1]);
				handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
			} else
				MONO_ADD_INS (bblock, ins);
			ip += 2;
			inline_costs += 1;
			break;
		case CEE_LDNULL:
			CHECK_STACK_OVF (1);
			NEW_PCONST (cfg, ins, NULL);
			ins->cil_code = ip;
			ins->type = STACK_OBJ;
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_M1:
			CHECK_STACK_OVF (1);
			NEW_ICONST (cfg, ins, -1);
			ins->cil_code = ip;
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
			ins->cil_code = ip;
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4_S:
			CHECK_OPSIZE (2);
			CHECK_STACK_OVF (1);
			++ip;
			NEW_ICONST (cfg, ins, *((signed char*)ip));
			ins->cil_code = ip;
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4:
			CHECK_OPSIZE (5);
			CHECK_STACK_OVF (1);
			NEW_ICONST (cfg, ins, (gint32)read32 (ip + 1));
			ins->cil_code = ip;
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_LDC_I8:
			CHECK_OPSIZE (9);
			CHECK_STACK_OVF (1);
			MONO_INST_NEW (cfg, ins, OP_I8CONST);
			ins->cil_code = ip;
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
			f = mono_mempool_alloc (cfg->domain->mp, sizeof (float));
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
			d = mono_mempool_alloc (cfg->domain->mp, sizeof (double));
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
				temp->cil_code = ip;
				*sp++ = temp;
			} else {
				temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
				temp->flags |= MONO_INST_IS_TEMP;
				temp->cil_code = ip;
				NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
				store->cil_code = ip;
				if (store->opcode == CEE_STOBJ) {
					NEW_TEMPLOADA (cfg, store, temp->inst_c0);
					handle_stobj (cfg, bblock, store, sp [0], sp [0]->cil_code, store->klass, TRUE, FALSE);
				} else {
					MONO_ADD_INS (bblock, store);
				}
				NEW_TEMPLOAD (cfg, ins, temp->inst_c0);
				*sp++ = ins;
				ins->cil_code = ip;
				NEW_TEMPLOAD (cfg, ins, temp->inst_c0);
				*sp++ = ins;
				ins->cil_code = ip;
			}
			++ip;
			inline_costs += 2;
			break;
		}
		case CEE_POP:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, CEE_POP);
			MONO_ADD_INS (bblock, ins);
			ins->cil_code = ip++;
			--sp;
			ins->inst_i0 = *sp;
			break;
		case CEE_JMP:
			CHECK_OPSIZE (5);
			if (stack_start != sp)
				goto unverified;
			MONO_INST_NEW (cfg, ins, CEE_JMP);
			token = read32 (ip + 1);
			/* FIXME: check the signature matches */
			cmethod = mini_get_method (method, token, NULL, generic_context);

			if (mono_use_security_manager) {
				check_linkdemand (cfg, method, cmethod, bblock, ip);
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
				if (method->wrapper_type != MONO_WRAPPER_NONE) {
					cmethod =  (MonoMethod *)mono_method_get_wrapper_data (method, token);
				} else if (constrained_call) {
					cmethod = mono_get_method_constrained (image, token, constrained_call, generic_context);
					cmethod = mono_get_inflated_method (cmethod);
				} else {
					cmethod = mini_get_method (method, token, NULL, generic_context);
				}

				g_assert (cmethod);

				if (!virtual && (cmethod->flags & METHOD_ATTRIBUTE_ABSTRACT))
					/* MS.NET seems to silently convert this to a callvirt */
					virtual = 1;

				if (!cmethod->klass->inited)
					mono_class_init (cmethod->klass);

				if (mono_method_signature (cmethod)->pinvoke) {
					MonoMethod *wrapper = mono_marshal_get_native_wrapper (cmethod);
					fsig = mono_method_signature (wrapper);
				} else if (constrained_call) {
					fsig = mono_method_signature (cmethod);
				} else {
					fsig = mono_method_get_signature_full (cmethod, image, token, generic_context);
				}

				n = fsig->param_count + fsig->hasthis;

				if (mono_use_security_manager) {
					check_linkdemand (cfg, method, cmethod, bblock, ip);
				}

				if (cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL &&
				    mini_class_is_system_array (cmethod->klass)) {
					array_rank = cmethod->klass->rank;
				}

				if (cmethod->string_ctor)
					g_assert_not_reached ();

			}

			if (!virtual) {
				mono_get_got_var (cfg);
			} else {
				/* code in inssel.brg might transform a virtual call to a normal call */
				if (!(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) || 
					((cmethod->flags & METHOD_ATTRIBUTE_FINAL) && 
					 cmethod->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK))
					mono_get_got_var (cfg);
			}

			if (cmethod && cmethod->klass->generic_container) {
				G_BREAKPOINT ();
				goto unverified;
			}

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
					MONO_INST_NEW (cfg, load, mono_type_to_ldind (&constrained_call->byval_arg));
					type_to_eval_stack_type (&constrained_call->byval_arg, load);
					load->cil_code = ip;
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
					ins->cil_code = ip;
					ins->inst_i0 = sp [0];
					ins->type = STACK_OBJ;
					sp [0] = ins;
				} else if (cmethod->klass->valuetype)
					virtual = 0;
				constrained_call = NULL;
			}

			if (*ip != CEE_CALLI && check_call_signature (cfg, fsig, sp)) {
				G_BREAKPOINT ();
				goto unverified;
			}

			if (cmethod && virtual && 
			    (cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) && 
		 	    !((cmethod->flags & METHOD_ATTRIBUTE_FINAL) && 
			      cmethod->wrapper_type != MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK) &&
			    mono_method_signature (cmethod)->generic_param_count) {
				MonoInst *this_temp, *store;
				MonoInst *iargs [3];

				g_assert (mono_method_signature (cmethod)->is_inflated);

				this_temp = mono_compile_create_var (cfg, type_from_stack_type (sp [0]), OP_LOCAL);
				this_temp->cil_code = ip;
				NEW_TEMPSTORE (cfg, store, this_temp->inst_c0, sp [0]);

				store->cil_code = ip;
				MONO_ADD_INS (bblock, store);

				NEW_TEMPLOAD (cfg, iargs [0], this_temp->inst_c0);
				NEW_PCONST (cfg, iargs [1], cmethod);
				NEW_PCONST (cfg, iargs [2], ((MonoMethodInflated *) cmethod)->context);
				temp = mono_emit_jit_icall (cfg, bblock, helper_compile_generic_method, iargs, ip);

				NEW_TEMPLOAD (cfg, addr, temp);
				NEW_TEMPLOAD (cfg, sp [0], this_temp->inst_c0);

				if ((temp = mono_emit_calli (cfg, bblock, fsig, sp, addr, ip)) != -1) {
					NEW_TEMPLOAD (cfg, *sp, temp);
					sp++;
				}

				ip += 5;
				break;
			}

			if ((ins_flag & MONO_INST_TAILCALL) && cmethod && (*ip == CEE_CALL) &&
				 (mono_metadata_signature_equal (mono_method_signature (method), mono_method_signature (cmethod)))) {
				int i;
				/* FIXME: This assumes the two methods has the same number and type of arguments */
				for (i = 0; i < n; ++i) {
					/* Check if argument is the same */
					NEW_ARGLOAD (cfg, ins, i);
					if ((ins->opcode == sp [i]->opcode) && (ins->inst_i0 == sp [i]->inst_i0))
						continue;

					/* Prevent argument from being register allocated */
					arg_array [i]->flags |= MONO_INST_VOLATILE;
					NEW_ARGSTORE (cfg, ins, i, sp [i]);
					ins->cil_code = ip;
					if (ins->opcode == CEE_STOBJ) {
						NEW_ARGLOADA (cfg, ins, i);
						handle_stobj (cfg, bblock, ins, sp [i], sp [i]->cil_code, ins->klass, FALSE, FALSE);
					}
					else
						MONO_ADD_INS (bblock, ins);
				}
				MONO_INST_NEW (cfg, ins, CEE_JMP);
				ins->cil_code = ip;
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
				ins->cil_code = ip;

				if (MONO_TYPE_IS_VOID (fsig->ret)) {
					MONO_ADD_INS (bblock, ins);
				} else {
					type_to_eval_stack_type (fsig->ret, ins);
					*sp = ins;
					sp++;
				}

				ip += 5;
				break;
			}

			handle_loaded_temps (cfg, bblock, stack_start, sp);

			if ((cfg->opt & MONO_OPT_INLINE) && cmethod &&
			    (!virtual || !(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) || (cmethod->flags & METHOD_ATTRIBUTE_FINAL)) && 
			    mono_method_check_inlining (cfg, cmethod) &&
				 !g_list_find (dont_inline, cmethod)) {
				int costs;
				MonoBasicBlock *ebblock;
				gboolean allways = FALSE;

				if ((cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
					(cmethod->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
					cmethod = mono_marshal_get_native_wrapper (cmethod);
					allways = TRUE;
				}

 				if ((costs = inline_method (cfg, cmethod, fsig, bblock, sp, ip, real_offset, dont_inline, &ebblock, allways))) {
					ip += 5;
					real_offset += 5;

					GET_BBLOCK (cfg, bbhash, bblock, ip);
					ebblock->next_bb = bblock;
					link_bblock (cfg, ebblock, bblock);

 					if (!MONO_TYPE_IS_VOID (fsig->ret))
 						sp++;

					/* indicates start of a new block, and triggers a load of all 
					   stack arguments at bb boundarie */
					bblock = ebblock;

					inline_costs += costs;
					break;
				}
			}
			
			inline_costs += 10 * num_calls++;

			/* tail recursion elimination */
			if ((cfg->opt & MONO_OPT_TAILC) && *ip == CEE_CALL && cmethod == method && ip [5] == CEE_RET) {
				gboolean has_vtargs = FALSE;
				int i;
				
				/* keep it simple */
				for (i =  fsig->param_count - 1; i >= 0; i--) {
					if (MONO_TYPE_ISSTRUCT (mono_method_signature (cmethod)->params [i])) 
						has_vtargs = TRUE;
				}

				if (!has_vtargs) {
					for (i = 0; i < n; ++i) {
						NEW_ARGSTORE (cfg, ins, i, sp [i]);
						ins->cil_code = ip;
						MONO_ADD_INS (bblock, ins);
					}
					MONO_INST_NEW (cfg, ins, CEE_BR);
					ins->cil_code = ip;
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

					break;
				}
			}

			if (*ip == CEE_CALLI) {

				if ((temp = mono_emit_calli (cfg, bblock, fsig, sp, addr, ip)) != -1) {
					NEW_TEMPLOAD (cfg, *sp, temp);
					sp++;
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
						store->cil_code = ip;
						MONO_ADD_INS (bblock, store);
						NEW_TEMPLOAD (cfg, iargs [0], array->inst_c0);

						to_store = mono_compile_create_var (cfg, type_from_stack_type (sp [fsig->param_count]), OP_LOCAL);
						NEW_TEMPSTORE (cfg, store, to_store->inst_c0, sp [fsig->param_count]);
						store->cil_code = ip;
						MONO_ADD_INS (bblock, store);
						NEW_TEMPLOAD (cfg, iargs [1], to_store->inst_c0);

						/*
						 * We first save the args for the call so that the args are copied to the stack
						 * and a new instruction tree for them is created. If we don't do this,
						 * the same MonoInst is added to two different trees and this is not 
						 * allowed by burg.
						 */
						mono_emit_jit_icall (cfg, bblock, helper_stelem_ref_check, iargs, ip);

						NEW_TEMPLOAD (cfg, sp [0], array->inst_c0);
						NEW_TEMPLOAD (cfg, sp [fsig->param_count], to_store->inst_c0);
					}

					addr = mini_get_ldelema_ins (cfg, bblock, cmethod, sp, ip, TRUE);
					NEW_INDSTORE (cfg, ins, addr, sp [fsig->param_count], fsig->params [fsig->param_count - 1]);
					ins->cil_code = ip;
					if (ins->opcode == CEE_STOBJ) {
						handle_stobj (cfg, bblock, addr, sp [fsig->param_count], ip, mono_class_from_mono_type (fsig->params [fsig->param_count-1]), FALSE, FALSE);
					} else {
						MONO_ADD_INS (bblock, ins);
					}

				} else if (strcmp (cmethod->name, "Get") == 0) { /* array Get */
					addr = mini_get_ldelema_ins (cfg, bblock, cmethod, sp, ip, FALSE);
					NEW_INDLOAD (cfg, ins, addr, fsig->ret);
					ins->cil_code = ip;

					*sp++ = ins;
				} else if (strcmp (cmethod->name, "Address") == 0) { /* array Address */
					addr = mini_get_ldelema_ins (cfg, bblock, cmethod, sp, ip, FALSE);
					*sp++ = addr;
				} else {
					g_assert_not_reached ();
				}

			} else {
				if (ip_in_bb (cfg, bblock, ip + 5) 
				    && (!MONO_TYPE_ISSTRUCT (fsig->ret))
				    && (!MONO_TYPE_IS_VOID (fsig->ret) || cmethod->string_ctor)
				    && (CODE_IS_STLOC (ip + 5) || ip [5] == CEE_POP || ip [5] == CEE_BRTRUE || ip [5] == CEE_BRFALSE ||
					ip [5] == CEE_BRTRUE_S || ip [5] == CEE_BRFALSE_S || ip [5] == CEE_RET)) {
					/* no need to spill */
					ins = (MonoInst*)mono_emit_method_call (cfg, bblock, cmethod, fsig, sp, ip, virtual ? sp [0] : NULL);
					*sp++ = ins;
				} else {
					if ((temp = mono_emit_method_call_spilled (cfg, bblock, cmethod, fsig, sp, ip, virtual ? sp [0] : NULL)) != -1) {
						NEW_TEMPLOAD (cfg, *sp, temp);
						sp++;
					}
				}
			}

			ip += 5;
			break;
		}
		case CEE_RET:
			if (cfg->method != method) {
				/* return from inlined methode */
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
						handle_stobj (cfg, bblock, store, *sp, sp [0]->cil_code, return_var->klass, FALSE, FALSE);
					} else
						MONO_ADD_INS (bblock, store);
				} 
			} else {
				if (cfg->ret) {
					g_assert (!return_var);
					CHECK_STACK (1);
					--sp;
					MONO_INST_NEW (cfg, ins, CEE_NOP);
					ins->opcode = mono_type_to_stind (mono_method_signature (method)->ret);
					if (ins->opcode == CEE_STOBJ) {
						NEW_RETLOADA (cfg, ins);
						handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
					} else {
						ins->opcode = OP_SETRET;
						ins->cil_code = ip;
						ins->inst_i0 = *sp;;
						ins->inst_i1 = NULL;
						MONO_ADD_INS (bblock, ins);
					}
				}
			}
			if (sp != stack_start)
				goto unverified;
			MONO_INST_NEW (cfg, ins, CEE_BR);
			ins->cil_code = ip++;
			ins->inst_target_bb = end_bblock;
			MONO_ADD_INS (bblock, ins);
			link_bblock (cfg, bblock, end_bblock);
			start_new_bblock = 1;
			break;
		case CEE_BR_S:
			CHECK_OPSIZE (2);
			MONO_INST_NEW (cfg, ins, CEE_BR);
			ins->cil_code = ip++;
			MONO_ADD_INS (bblock, ins);
			target = ip + 1 + (signed char)(*ip);
			++ip;
			GET_BBLOCK (cfg, bbhash, tblock, target);
			link_bblock (cfg, bblock, tblock);
			CHECK_BBLOCK (target, ip, tblock);
			ins->inst_target_bb = tblock;
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			start_new_bblock = 1;
			inline_costs += 10;
			break;
		case CEE_BRFALSE_S:
		case CEE_BRTRUE_S:
			CHECK_OPSIZE (2);
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip + BIG_BRANCH_OFFSET);
			ins->cil_code = ip++;
			target = ip + 1 + *(signed char*)ip;
			ip++;
			ADD_UNCOND (ins->opcode == CEE_BRTRUE);
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			inline_costs += 10;
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
			ins->cil_code = ip++;
			target = ip + 1 + *(signed char*)ip;
			ip++;
			ADD_BINCOND (NULL);
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			inline_costs += 10;
			break;
		case CEE_BR:
			CHECK_OPSIZE (5);
			MONO_INST_NEW (cfg, ins, CEE_BR);
			ins->cil_code = ip++;
			MONO_ADD_INS (bblock, ins);
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
			GET_BBLOCK (cfg, bbhash, tblock, target);
			link_bblock (cfg, bblock, tblock);
			CHECK_BBLOCK (target, ip, tblock);
			ins->inst_target_bb = tblock;
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			start_new_bblock = 1;
			inline_costs += 10;
			break;
		case CEE_BRFALSE:
		case CEE_BRTRUE:
			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			ins->cil_code = ip++;
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
			ADD_UNCOND(ins->opcode == CEE_BRTRUE);
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			inline_costs += 10;
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
			ins->cil_code = ip++;
			target = ip + 4 + (gint32)read32(ip);
			ip += 4;
			ADD_BINCOND(NULL);
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			inline_costs += 10;
			break;
		case CEE_SWITCH:
			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			n = read32 (ip + 1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			ins->inst_left = *sp;
			if ((ins->inst_left->type != STACK_I4) && (ins->inst_left->type != STACK_PTR)) 
				goto unverified;
			ins->cil_code = ip;
			ip += 5;
			CHECK_OPSIZE (n * sizeof (guint32));
			target = ip + n * sizeof (guint32);
			MONO_ADD_INS (bblock, ins);
			GET_BBLOCK (cfg, bbhash, tblock, target);
			link_bblock (cfg, bblock, tblock);
			ins->klass = GUINT_TO_POINTER (n);
			ins->inst_many_bb = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (n + 1));
			ins->inst_many_bb [n] = tblock;

			for (i = 0; i < n; ++i) {
				GET_BBLOCK (cfg, bbhash, tblock, target + (gint32)read32(ip));
				link_bblock (cfg, bblock, tblock);
				ins->inst_many_bb [i] = tblock;
				ip += 4;
			}
			if (sp != stack_start) {
				handle_stack_args (cfg, bblock, stack_start, sp - stack_start);
				sp = stack_start;
			}
			/* Needed by the code generated in inssel.brg */
			mono_get_got_var (cfg);
			inline_costs += 20;
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
			ins->cil_code = ip;
			--sp;
			ins->inst_i0 = *sp;
			*sp++ = ins;
			ins->type = ldind_type [*ip - CEE_LDIND_I1];
			ins->flags |= ins_flag;
			ins_flag = 0;
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
			MONO_INST_NEW (cfg, ins, *ip);
			ins->cil_code = ip++;
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
				default:
					g_assert_not_reached ();
				}
			}
#endif

			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
				mono_get_got_var (cfg);
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
				ins->opcode = OP_LONG_SHRUN_32;
				/*g_print ("applied long shr speedup to %s\n", cfg->method->name);*/
				ip++;
				break;
			}
			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
				mono_get_got_var (cfg);
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
			if (mono_find_jit_opcode_emulation (ins->opcode)) {
				--sp;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 1);
				mono_get_got_var (cfg);
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
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);

			mono_class_init (klass);
			sp -= 2;
			if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
				MonoInst *store, *load;
				MONO_INST_NEW (cfg, load, CEE_LDIND_REF);
				load->cil_code = ip;
				load->inst_i0 = sp [1];
				load->type = STACK_OBJ;
				load->flags |= ins_flag;
				MONO_INST_NEW (cfg, store, CEE_STIND_REF);
				store->cil_code = ip;
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				MONO_ADD_INS (bblock, store);
				store->inst_i0 = sp [0];
				store->inst_i1 = load;
				store->flags |= ins_flag;
			} else {
				n = mono_class_value_size (klass, NULL);
				if ((cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
					MonoInst *copy;
					MONO_INST_NEW (cfg, copy, OP_MEMCPY);
					copy->inst_left = sp [0];
					copy->inst_right = sp [1];
					copy->cil_code = ip;
					copy->unused = n;
					MONO_ADD_INS (bblock, copy);
				} else {
					MonoMethod *memcpy_method = get_memcpy_method ();
					MonoInst *iargs [3];
					iargs [0] = sp [0];
					iargs [1] = sp [1];
					NEW_ICONST (cfg, iargs [2], n);
					iargs [2]->cil_code = ip;

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
			CHECK_OPSIZE (5);
			CHECK_STACK (1);
			--sp;
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);

			mono_class_init (klass);
			if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
				MONO_INST_NEW (cfg, ins, CEE_LDIND_REF);
				ins->cil_code = ip;
				ins->inst_i0 = sp [0];
				ins->type = STACK_OBJ;
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

				if (ins->opcode == CEE_STOBJ) {
					handle_loaded_temps (cfg, bblock, stack_start, sp);
					ins->cil_code = ip;
					g_assert (ins->opcode == CEE_STOBJ);
					NEW_LOCLOADA (cfg, ins, loc_index);
					handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
					ip += 5;
					ip += stloc_len;
					break;
				}
			}

			n = mono_class_value_size (klass, NULL);
			ins = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);
			NEW_TEMPLOADA (cfg, iargs [0], ins->inst_c0);
			if ((cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
				MonoInst *copy;
				MONO_INST_NEW (cfg, copy, OP_MEMCPY);
				copy->inst_left = iargs [0];
				copy->inst_right = *sp;
				copy->cil_code = ip;
				copy->unused = n;
				MONO_ADD_INS (bblock, copy);
			} else {
				MonoMethod *memcpy_method = get_memcpy_method ();
				iargs [1] = *sp;
				NEW_ICONST (cfg, iargs [2], n);
				iargs [2]->cil_code = ip;

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
				NEW_PCONST (cfg, ins, mono_method_get_wrapper_data (method, n));
				ins->cil_code = ip;
				ins->type = STACK_OBJ;
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

						/* Avoid creating the string object */
						NEW_IMAGECONST (cfg, iargs [0], image);
						NEW_ICONST (cfg, iargs [1], mono_metadata_token_index (n));
						temp = mono_emit_jit_icall (cfg, bblock, helper_ldstr, iargs, ip);
						NEW_TEMPLOAD (cfg, *sp, temp);
					} 
					else
					if (cfg->compile_aot) {
						NEW_LDSTRCONST (cfg, ins, image, n);
						*sp = ins;
					} 
					else {
						NEW_PCONST (cfg, ins, NULL);
						ins->cil_code = ip;
						ins->type = STACK_OBJ;
						ins->inst_p0 = mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
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
			int temp;
			
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			cmethod = mini_get_method (method, token, NULL, generic_context);
			fsig = mono_method_get_signature (cmethod, image, token);

			mono_class_init (cmethod->klass);

			if (mono_use_security_manager) {
				check_linkdemand (cfg, method, cmethod, bblock, ip);
			}

			n = fsig->param_count;
			CHECK_STACK (n);

			/* move the args to allow room for 'this' in the first position */
			while (n--) {
				--sp;
				sp [1] = sp [0];
			}

			handle_loaded_temps (cfg, bblock, stack_start, sp);

			if (mini_class_is_system_array (cmethod->klass)) {
				NEW_METHODCONST (cfg, *sp, cmethod);
				temp = handle_array_new (cfg, bblock, fsig->param_count, sp, ip);
			} else if (cmethod->string_ctor) {
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
					 * will be transformed into a normal call there. The AOT
					 * case needs an already allocate got_var.
					 */
					mono_get_got_var (cfg);
				} else {
					temp = handle_alloc (cfg, bblock, cmethod->klass, FALSE, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
				}

				/* Avoid virtual calls to ctors if possible */
				if (cmethod->klass->marshalbyref)
					callvirt_this_arg = sp [0];
				
				if ((cfg->opt & MONO_OPT_INLINE) && cmethod &&
				    mono_method_check_inlining (cfg, cmethod) &&
				    !mono_class_is_subclass_of (cmethod->klass, mono_defaults.exception_class, FALSE) &&
				    !g_list_find (dont_inline, cmethod)) {
					int costs;
					MonoBasicBlock *ebblock;
					if ((costs = inline_method (cfg, cmethod, fsig, bblock, sp, ip, real_offset, dont_inline, &ebblock, FALSE))) {

						ip += 5;
						real_offset += 5;
						
						GET_BBLOCK (cfg, bbhash, bblock, ip);
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
						mono_emit_method_call_spilled (cfg, bblock, cmethod, fsig, sp, ip, callvirt_this_arg);
					}
				} else {
					/* now call the actual ctor */
					mono_emit_method_call_spilled (cfg, bblock, cmethod, fsig, sp, ip, callvirt_this_arg);
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
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);

			/* Needed by the code generated in inssel.brg */
			mono_get_got_var (cfg);

			if (klass->marshalbyref || klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
			
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
			
				GET_BBLOCK (cfg, bbhash, bblock, ip);
				ebblock->next_bb = bblock;
				link_bblock (cfg, ebblock, bblock);

				temp = iargs [0]->inst_i0->inst_c0;
				NEW_TEMPLOAD (cfg, *sp, temp);
				
 				sp++;
				bblock = ebblock;
				inline_costs += costs;

			}
			else {
				MONO_INST_NEW (cfg, ins, *ip);
				ins->type = STACK_OBJ;
				ins->inst_left = *sp;
				ins->inst_newa_class = klass;
				ins->cil_code = ip;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 5);
				ip += 5;
			}
			break;
		case CEE_UNBOX_ANY: {
			MonoInst *add, *vtoffset;
			MonoInst *iargs [3];

			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else 
				klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);

			if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
				/* CASTCLASS */
				if (klass->marshalbyref || klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
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
				
					GET_BBLOCK (cfg, bbhash, bblock, ip);
					ebblock->next_bb = bblock;
					link_bblock (cfg, ebblock, bblock);
	
					temp = iargs [0]->inst_i0->inst_c0;
					NEW_TEMPLOAD (cfg, *sp, temp);
					
					sp++;
					bblock = ebblock;
					inline_costs += costs;				
				}
				else {
					MONO_INST_NEW (cfg, ins, CEE_CASTCLASS);
					ins->type = STACK_OBJ;
					ins->inst_left = *sp;
					ins->klass = klass;
					ins->inst_newa_class = klass;
					ins->cil_code = ip;
					*sp++ = ins;
					ip += 5;
				}
				break;
			}

			MONO_INST_NEW (cfg, ins, OP_UNBOXCAST);
			ins->type = STACK_OBJ;
			ins->inst_left = *sp;
			ins->klass = klass;
			ins->inst_newa_class = klass;
			ins->cil_code = ip;

			MONO_INST_NEW (cfg, add, OP_PADD);
			NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
			add->inst_left = ins;
			add->inst_right = vtoffset;
			add->type = STACK_MP;
			*sp = add;
			ip += 5;
			/* LDOBJ impl */
			n = mono_class_value_size (klass, NULL);
			ins = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);
			NEW_TEMPLOADA (cfg, iargs [0], ins->inst_c0);
			if ((cfg->opt & MONO_OPT_INTRINS) && n <= sizeof (gpointer) * 5) {
				MonoInst *copy;
				MONO_INST_NEW (cfg, copy, OP_MEMCPY);
				copy->inst_left = iargs [0];
				copy->inst_right = *sp;
				copy->cil_code = ip;
				copy->unused = n;
				MONO_ADD_INS (bblock, copy);
			} else {
				MonoMethod *memcpy_method = get_memcpy_method ();
				iargs [1] = *sp;
				NEW_ICONST (cfg, iargs [2], n);
				iargs [2]->cil_code = ip;

				mono_emit_method_call_spilled (cfg, bblock, memcpy_method, memcpy_method->signature, iargs, ip, NULL);
			}
			NEW_TEMPLOAD (cfg, *sp, ins->inst_c0);
			++sp;
			inline_costs += 2;
			break;
		}
		case CEE_UNBOX: {
			MonoInst *add, *vtoffset;

			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else 
				klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);

			/* Needed by the code generated in inssel.brg */
			mono_get_got_var (cfg);

			MONO_INST_NEW (cfg, ins, OP_UNBOXCAST);
			ins->type = STACK_OBJ;
			ins->inst_left = *sp;
			ins->klass = klass;
			ins->inst_newa_class = klass;
			ins->cil_code = ip;

			MONO_INST_NEW (cfg, add, OP_PADD);
			NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
			add->inst_left = ins;
			add->inst_right = vtoffset;
			add->type = STACK_MP;
			*sp++ = add;
			ip += 5;
			inline_costs += 2;
			break;
		}
		case CEE_CASTCLASS:
			CHECK_STACK (1);
			--sp;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);

			/* Needed by the code generated in inssel.brg */
			mono_get_got_var (cfg);
		
			if (klass->marshalbyref || klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
				
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
			
				GET_BBLOCK (cfg, bbhash, bblock, ip);
				ebblock->next_bb = bblock;
				link_bblock (cfg, ebblock, bblock);

				temp = iargs [0]->inst_i0->inst_c0;
				NEW_TEMPLOAD (cfg, *sp, temp);
				
 				sp++;
				bblock = ebblock;
				inline_costs += costs;
			}
			else {
				MONO_INST_NEW (cfg, ins, *ip);
				ins->type = STACK_OBJ;
				ins->inst_left = *sp;
				ins->klass = klass;
				ins->inst_newa_class = klass;
				ins->cil_code = ip;
				*sp++ = emit_tree (cfg, bblock, ins, ip + 5);
				ip += 5;
			}
			break;
		case CEE_THROW:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			ins->inst_left = *sp;
			ins->cil_code = ip++;
			bblock->out_of_line = TRUE;
			MONO_ADD_INS (bblock, ins);
			MONO_INST_NEW (cfg, ins, OP_NOT_REACHED);
			ins->cil_code = ip - 1;
			MONO_ADD_INS (bblock, ins);
			sp = stack_start;
			link_bblock (cfg, bblock, end_bblock);
			start_new_bblock = 1;
			mono_get_got_var (cfg);
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
			// FIXME: enable this test later.
			//if (sp [0]->type != STACK_OBJ && sp [0]->type != STACK_MP)
			//	goto unverified;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass, generic_context);
			mono_class_init (klass);

			foffset = klass->valuetype? field->offset - sizeof (MonoObject): field->offset;
			/* FIXME: mark instructions for use in SSA */
			if (*ip == CEE_STFLD) {
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

						GET_BBLOCK (cfg, bbhash, bblock, ip);
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
				} else {
					MonoInst *store;
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->cil_code = ip;
					ins->inst_left = *sp;
					ins->inst_right = offset_ins;
					ins->type = STACK_MP;

					MONO_INST_NEW (cfg, store, mono_type_to_stind (field->type));
					store->cil_code = ip;
					store->inst_left = ins;
					store->inst_right = sp [1];
					handle_loaded_temps (cfg, bblock, stack_start, sp);
					store->flags |= ins_flag;
					ins_flag = 0;
					if (store->opcode == CEE_STOBJ) {
						handle_stobj (cfg, bblock, ins, sp [1], ip, 
							      mono_class_from_mono_type (field->type), FALSE, FALSE);
					} else
						MONO_ADD_INS (bblock, store);
				}
			} else {
				if ((klass->marshalbyref && !MONO_CHECK_THIS (sp [0])) || klass->contextbound || klass == mono_defaults.marshalbyrefobject_class) {
					MonoMethod *ldfld_wrapper = mono_marshal_get_ldfld_wrapper (field->type); 
					MonoInst *iargs [4];
					int temp;
					
					iargs [0] = sp [0];
					NEW_CLASSCONST (cfg, iargs [1], klass);
					NEW_FIELDCONST (cfg, iargs [2], field);
					NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset);
					if ((cfg->opt & MONO_OPT_INLINE) && !MONO_TYPE_ISSTRUCT (mono_method_signature (ldfld_wrapper)->ret)) {
						costs = inline_method (cfg, ldfld_wrapper, mono_method_signature (ldfld_wrapper), bblock, 
								       iargs, ip, real_offset, dont_inline, &ebblock, TRUE);
						g_assert (costs > 0);
						      
						ip += 5;
						real_offset += 5;

						GET_BBLOCK (cfg, bbhash, bblock, ip);
						ebblock->next_bb = bblock;
						link_bblock (cfg, ebblock, bblock);

						temp = iargs [0]->inst_i0->inst_c0;

						if (*ip == CEE_LDFLDA) {
							/* not sure howto handle this */
							NEW_TEMPLOADA (cfg, *sp, temp);
						} else {
							NEW_TEMPLOAD (cfg, *sp, temp);
						}
						sp++;

						/* indicates start of a new block, and triggers a load of
						   all stack arguments at bb boundarie */
						bblock = ebblock;
						
						inline_costs += costs;
						break;
					} else {
						temp = mono_emit_method_call_spilled (cfg, bblock, ldfld_wrapper, mono_method_signature (ldfld_wrapper), iargs, ip, NULL);
						if (*ip == CEE_LDFLDA) {
							/* not sure howto handle this */
							NEW_TEMPLOADA (cfg, *sp, temp);
						} else {
							NEW_TEMPLOAD (cfg, *sp, temp);
						}
						sp++;
					}
				} else {
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, OP_PADD);
					ins->cil_code = ip;
					ins->inst_left = *sp;
					ins->inst_right = offset_ins;
					ins->type = STACK_MP;

					if (*ip == CEE_LDFLDA) {
						*sp++ = ins;
					} else {
						MonoInst *load;
						MONO_INST_NEW (cfg, load, mono_type_to_ldind (field->type));
						type_to_eval_stack_type (field->type, load);
						load->cil_code = ip;
						load->inst_left = ins;
						load->flags |= ins_flag;
						ins_flag = 0;
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
			gpointer addr = NULL;

			CHECK_OPSIZE (5);
			token = read32 (ip + 1);

			field = mono_field_from_token (image, token, &klass, generic_context);
			mono_class_init (klass);

			g_assert (!(field->type->attrs & FIELD_ATTRIBUTE_LITERAL));

			if ((*ip) == CEE_STSFLD)
				handle_loaded_temps (cfg, bblock, stack_start, sp);

			/* The special_static_fields field is init'd in mono_class_vtable, so it needs
			 * to be called here.
			 */
			if (!(cfg->opt & MONO_OPT_SHARED))
				mono_class_vtable (cfg->domain, klass);
			mono_domain_lock (cfg->domain);
			if (cfg->domain->special_static_fields)
				addr = g_hash_table_lookup (cfg->domain->special_static_fields, field);
			mono_domain_unlock (cfg->domain);

			if ((cfg->opt & MONO_OPT_SHARED) || (cfg->compile_aot && addr)) {
				int temp;
				MonoInst *iargs [2];
				MonoInst *domain_var;
				
				g_assert (field->parent);
				/* avoid depending on undefined C behavior in sequence points */
				domain_var = mono_get_domainvar (cfg);
				NEW_TEMPLOAD (cfg, iargs [0], domain_var->inst_c0);
				NEW_FIELDCONST (cfg, iargs [1], field);
				temp = mono_emit_jit_icall (cfg, bblock, mono_class_static_field_address, iargs, ip);
				NEW_TEMPLOAD (cfg, ins, temp);
			} else {
				MonoVTable *vtable;
				vtable = mono_class_vtable (cfg->domain, klass);
				if (!addr) {
					if (mini_field_access_needs_cctor_run (cfg, method, vtable) && !(g_slist_find (class_inits, vtable))) {
						guint8 *tramp = mono_create_class_init_trampoline (vtable);
						mono_emit_native_call (cfg, bblock, tramp, 
											   helper_sig_class_init_trampoline,
											   NULL, ip, FALSE, FALSE);
						if (cfg->verbose_level > 2)
							g_print ("class %s.%s needs init call for %s\n", klass->name_space, klass->name, field->name);
						class_inits = g_slist_prepend (class_inits, vtable);
					} else {
						if (cfg->run_cctors)
							mono_runtime_class_init (vtable);
					}
					addr = (char*)vtable->data + field->offset;

					if (cfg->compile_aot)
						NEW_SFLDACONST (cfg, ins, field);
					else
						NEW_PCONST (cfg, ins, addr);
					ins->cil_code = ip;
				} else {
					/* 
					 * insert call to mono_threads_get_static_data (GPOINTER_TO_UINT (addr)) 
					 * This could be later optimized to do just a couple of
					 * memory dereferences with constant offsets.
					 */
					int temp;
					MonoInst *iargs [1];
					NEW_ICONST (cfg, iargs [0], GPOINTER_TO_UINT (addr));
					temp = mono_emit_jit_icall (cfg, bblock, mono_get_special_static_data, iargs, ip);
					NEW_TEMPLOAD (cfg, ins, temp);
				}
			}

			/* FIXME: mark instructions for use in SSA */
			if (*ip == CEE_LDSFLDA) {
				*sp++ = ins;
			} else if (*ip == CEE_STSFLD) {
				MonoInst *store;
				CHECK_STACK (1);
				sp--;
				MONO_INST_NEW (cfg, store, mono_type_to_stind (field->type));
				store->cil_code = ip;
				store->inst_left = ins;
				store->inst_right = sp [0];
				store->flags |= ins_flag;
				ins_flag = 0;

				if (store->opcode == CEE_STOBJ) {
					handle_stobj (cfg, bblock, ins, sp [0], ip, mono_class_from_mono_type (field->type), FALSE, FALSE);
				} else
					MONO_ADD_INS (bblock, store);
			} else {
				gboolean is_const = FALSE;
				MonoVTable *vtable = mono_class_vtable (cfg->domain, klass);
				if (!((cfg->opt & MONO_OPT_SHARED) || cfg->compile_aot) && 
				    vtable->initialized && (field->type->attrs & FIELD_ATTRIBUTE_INIT_ONLY)) {
					gpointer addr = (char*)vtable->data + field->offset;
					int ro_type = field->type->type;
					if (ro_type == MONO_TYPE_VALUETYPE && field->type->data.klass->enumtype) {
						ro_type = field->type->data.klass->enum_basetype->type;
					}
					/* g_print ("RO-FIELD %s.%s:%s\n", klass->name_space, klass->name, field->name);*/
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
						type_to_eval_stack_type (field->type, *sp);
						sp++;
						break;
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
					MONO_INST_NEW (cfg, load, mono_type_to_ldind (field->type));
					type_to_eval_stack_type (field->type, load);
					load->cil_code = ip;
					load->inst_left = ins;
					*sp++ = load;
					load->flags |= ins_flag;
					ins_flag = 0;
					/* fixme: dont see the problem why this does not work */
					//cfg->disable_aot = TRUE;
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
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);
			n = mono_type_to_stind (&klass->byval_arg);
			if (n == CEE_STOBJ) {
				handle_stobj (cfg, bblock, sp [0], sp [1], ip, klass, FALSE, FALSE);
			} else {
				/* FIXME: should check item at sp [1] is compatible with the type of the store. */
				MonoInst *store;
				MONO_INST_NEW (cfg, store, n);
				store->cil_code = ip;
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
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);

			if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
				*sp++ = val;
				ip += 5;
				break;
			}
			*sp++ = handle_box (cfg, bblock, val, ip, klass);
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_NEWARR:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			ins->cil_code = ip;
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

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get_full (image, token, generic_context);

			mono_class_init (klass);
			ins->inst_newa_class = klass;
			ins->inst_newa_len = *sp;
			ins->type = STACK_OBJ;
			ip += 5;
			*sp++ = ins;
			/* 
			 * we store the object so calls to create the array are not interleaved
			 * with the arguments of other calls.
			 */
			if (1) {
				MonoInst *store, *temp, *load;
				--sp;
				temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
				NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
				store->cil_code = ins->cil_code;
				MONO_ADD_INS (bblock, store);
				NEW_TEMPLOAD (cfg, load, temp->inst_c0);
				load->cil_code = ins->cil_code;
				*sp++ = load;
			}
			inline_costs += 1;
			break;
		case CEE_LDLEN:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			ins->cil_code = ip++;
			--sp;
			ins->inst_left = *sp;
			ins->type = STACK_PTR;
			*sp++ = ins;
			break;
		case CEE_LDELEMA:
			CHECK_STACK (2);
			sp -= 2;
			CHECK_OPSIZE (5);

			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass*)mono_method_get_wrapper_data (method, read32 (ip + 1));
			else
				klass = mono_class_get_full (image, read32 (ip + 1), generic_context);
			
			/* we need to make sure that this array is exactly the type it needs
			 * to be for correctness. the wrappers are lax with their usage
			 * so we need to ignore them here
			 */
			if (!klass->valuetype && method->wrapper_type == MONO_WRAPPER_NONE) {
				MonoInst* check;
				MONO_INST_NEW (cfg, check, OP_CHECK_ARRAY_TYPE);
				check->cil_code = ip;
				check->klass = klass;
				check->inst_left = sp [0];
				check->type = STACK_OBJ;
				sp [0] = check;
			}
			
			mono_class_init (klass);
			NEW_LDELEMA (cfg, ins, sp, klass);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 5;
			break;
		case CEE_LDELEM_ANY: {
			MonoInst *load;
			CHECK_STACK (2);
			sp -= 2;
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);
			NEW_LDELEMA (cfg, load, sp, klass);
			load->cil_code = ip;
			MONO_INST_NEW (cfg, ins, mono_type_to_ldind (&klass->byval_arg));
			ins->cil_code = ip;
			ins->inst_left = load;
			*sp++ = ins;
			type_to_eval_stack_type (&klass->byval_arg, ins);
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
			klass = array_access_to_klass (*ip);
			NEW_LDELEMA (cfg, load, sp, klass);
			load->cil_code = ip;
			MONO_INST_NEW (cfg, ins, ldelem_to_ldind [*ip - CEE_LDELEM_I1]);
			ins->cil_code = ip;
			ins->inst_left = load;
			*sp++ = ins;
			ins->type = ldind_type [ins->opcode - CEE_LDIND_I1];
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
			klass = array_access_to_klass (*ip);
			NEW_LDELEMA (cfg, load, sp, klass);
			load->cil_code = ip;
			MONO_INST_NEW (cfg, ins, stelem_to_stind [*ip - CEE_STELEM_I]);
			ins->cil_code = ip;
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
			CHECK_OPSIZE (5);
			token = read32 (ip + 1);
			klass = mono_class_get_full (image, token, generic_context);
			mono_class_init (klass);
			if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
				MonoMethod* helper = mono_marshal_get_stelemref ();
				MonoInst *iargs [3];
				handle_loaded_temps (cfg, bblock, stack_start, sp);

				iargs [2] = sp [2];
				iargs [1] = sp [1];
				iargs [0] = sp [0];
				
				mono_emit_method_call_spilled (cfg, bblock, helper, mono_method_signature (helper), iargs, ip, NULL);
			} else {
				NEW_LDELEMA (cfg, load, sp, klass);
				load->cil_code = ip;

				n = mono_type_to_stind (&klass->byval_arg);
				if (n == CEE_STOBJ)
					handle_stobj (cfg, bblock, load, sp [2], ip, klass, FALSE, FALSE);
				else {
					MONO_INST_NEW (cfg, ins, n);
					ins->cil_code = ip;
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

			handle_loaded_temps (cfg, bblock, stack_start, sp);

			iargs [2] = sp [2];
			iargs [1] = sp [1];
			iargs [0] = sp [0];
			
			mono_emit_method_call_spilled (cfg, bblock, helper, mono_method_signature (helper), iargs, ip, NULL);

			/*
			MonoInst *group;
			NEW_GROUP (cfg, group, sp [0], sp [1]);
			MONO_INST_NEW (cfg, ins, CEE_STELEM_REF);
			ins->cil_code = ip;
			ins->inst_left = group;
			ins->inst_right = sp [2];
			MONO_ADD_INS (bblock, ins);
			*/

			++ip;
			inline_costs += 1;
			break;
		}
		case CEE_CKFINITE: {
			MonoInst *store, *temp;
			CHECK_STACK (1);

			/* this instr. can throw exceptions as side effect,
			 * so we cant eliminate dead code which contains CKFINITE opdodes.
			 * Spilling to memory makes sure that we always perform
			 * this check */

			
			MONO_INST_NEW (cfg, ins, CEE_CKFINITE);
			ins->cil_code = ip;
			ins->inst_left = sp [-1];
			temp = mono_compile_create_var (cfg, &mono_defaults.double_class->byval_arg, OP_LOCAL);

			NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
			store->cil_code = ip;
			MONO_ADD_INS (bblock, store);

			NEW_TEMPLOAD (cfg, sp [-1], temp->inst_c0);
		       
			++ip;
			break;
		}
		case CEE_REFANYVAL:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			CHECK_OPSIZE (5);
			klass = mono_class_get_full (image, read32 (ip + 1), generic_context);
			mono_class_init (klass);
			ins->type = STACK_MP;
			ins->inst_left = *sp;
			ins->klass = klass;
			ins->inst_newa_class = klass;
			ins->cil_code = ip;
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_MKREFANY: {
			MonoInst *loc, *klassconst;

			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			CHECK_OPSIZE (5);
			klass = mono_class_get_full (image, read32 (ip + 1), generic_context);
			mono_class_init (klass);
			ins->cil_code = ip;

			loc = mono_compile_create_var (cfg, &mono_defaults.typed_reference_class->byval_arg, OP_LOCAL);
			NEW_TEMPLOADA (cfg, ins->inst_right, loc->inst_c0);

			NEW_PCONST (cfg, klassconst, klass);
			NEW_GROUP (cfg, ins->inst_left, *sp, klassconst);
			
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

			if (method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD) {
				handle = mono_method_get_wrapper_data (method, n);
				handle_class = mono_method_get_wrapper_data (method, n + 1);
			}
			else {
				handle = mono_ldtoken (image, n, &handle_class, generic_context);
			}
			mono_class_init (handle_class);

			if (cfg->opt & MONO_OPT_SHARED) {
				int temp;
				MonoInst *res, *store, *addr, *vtvar, *iargs [3];

				vtvar = mono_compile_create_var (cfg, &handle_class->byval_arg, OP_LOCAL); 

				NEW_IMAGECONST (cfg, iargs [0], image);
				NEW_ICONST (cfg, iargs [1], n);
				NEW_PCONST (cfg, iargs [2], generic_context);
				temp = mono_emit_jit_icall (cfg, bblock, mono_ldtoken_wrapper, iargs, ip);
				NEW_TEMPLOAD (cfg, res, temp);
				NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
				NEW_INDSTORE (cfg, store, addr, res, &mono_defaults.int_class->byval_arg);
				MONO_ADD_INS (bblock, store);
				NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
			} else {
				if ((ip [5] == CEE_CALL) && (cmethod = mini_get_method (method, read32 (ip + 6), NULL, generic_context)) &&
						(cmethod->klass == mono_defaults.monotype_class->parent) &&
						(strcmp (cmethod->name, "GetTypeFromHandle") == 0) && ip_in_bb (cfg, bblock, ip + 5)) {
					MonoClass *tclass = mono_class_from_mono_type (handle);
					mono_class_init (tclass);
					if (cfg->compile_aot)
						NEW_TYPE_FROM_HANDLE_CONST (cfg, ins, image, n);
					else
						NEW_PCONST (cfg, ins, mono_type_get_object (cfg->domain, handle));
					ins->type = STACK_OBJ;
					ins->klass = cmethod->klass;
					ip += 5;
				} else {
					MonoInst *store, *addr, *vtvar;

					if (cfg->compile_aot)
						NEW_LDTOKENCONST (cfg, ins, image, n);
					else
						NEW_PCONST (cfg, ins, handle);
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
				mono_get_got_var (cfg);
			}
			ip++;
			break;
		case CEE_ENDFINALLY:
			MONO_INST_NEW (cfg, ins, *ip);
			MONO_ADD_INS (bblock, ins);
			ins->cil_code = ip++;
			start_new_bblock = 1;

			/*
			 * Control will leave the method so empty the stack, otherwise
			 * the next basic block will start with a nonempty stack.
			 */
			while (sp != stack_start) {
				MONO_INST_NEW (cfg, ins, CEE_POP);
				ins->cil_code = ip;
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
				ins->cil_code = ip;
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

				if (MONO_OFFSET_IN_HANDLER (clause, ip - header->code) && (clause->flags == MONO_EXCEPTION_CLAUSE_NONE) && (ip - header->code + ((*ip == CEE_LEAVE) ? 5 : 2)) == (clause->handler_offset + clause->handler_len)) {
					int temp;
					MonoInst *load;

					NEW_TEMPLOAD (cfg, load, mono_find_exvar_for_offset (cfg, clause->handler_offset)->inst_c0);
					load->cil_code = ip;

					temp = mono_emit_jit_icall (cfg, bblock, mono_thread_get_pending_exception, NULL, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
				
					MONO_INST_NEW (cfg, ins, OP_THROW_OR_NULL);
					ins->inst_left = *sp;
					ins->inst_right = load;
					ins->cil_code = ip;
					MONO_ADD_INS (bblock, ins);
				}
			}

			/* fixme: call fault handler ? */

			if ((handlers = mono_find_final_block (cfg, ip, target, MONO_EXCEPTION_CLAUSE_FINALLY))) {
				GList *tmp;
				for (tmp = handlers; tmp; tmp = tmp->next) {
					tblock = tmp->data;
					link_bblock (cfg, bblock, tblock);
					MONO_INST_NEW (cfg, ins, OP_CALL_HANDLER);
					ins->cil_code = ip;
					ins->inst_target_bb = tblock;
					MONO_ADD_INS (bblock, ins);
				}
				g_list_free (handlers);
			} 

			MONO_INST_NEW (cfg, ins, CEE_BR);
			ins->cil_code = ip;
			MONO_ADD_INS (bblock, ins);
			GET_BBLOCK (cfg, bbhash, tblock, target);
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
			ins->cil_code = ip++;
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
				g_assert (info);

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
			case CEE_MONO_LDPTR:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				NEW_PCONST (cfg, ins, mono_method_get_wrapper_data (method, token));
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				/* Can't embed random pointers into AOT code */
				cfg->disable_aot = 1;
				break;
			case CEE_MONO_VTADDR:
				CHECK_STACK (1);
				--sp;
				MONO_INST_NEW (cfg, ins, OP_VTADDR);
				ins->cil_code = ip;
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
				ins->cil_code = ip;
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
				handle_stobj (cfg, bblock, ins, *sp, ip, klass, FALSE, TRUE);
				
				if (sp != stack_start)
					goto unverified;
				
				MONO_INST_NEW (cfg, ins, CEE_BR);
				ins->cil_code = ip;
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
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
				MONO_INST_NEW (cfg, ins, (ip [1] == CEE_MONO_CISINST) ? OP_CISINST : OP_CCASTCLASS);
				ins->type = STACK_I4;
				ins->inst_left = *sp;
				ins->inst_newa_class = klass;
				ins->cil_code = ip;
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
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
				break;
			case CEE_MONO_NOT_TAKEN:
				bblock->out_of_line = TRUE;
				ip += 2;
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
				addr->cil_code = ip;
				MONO_INST_NEW (cfg, ins, OP_ARGLIST);
				ins->cil_code = ip;
				ins->inst_left = addr;
				MONO_ADD_INS (bblock, ins);
				NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
				ins->cil_code = ip;
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
				MONO_INST_NEW (cfg, cmp, 256 + ip [1]);
				
				MONO_INST_NEW (cfg, ins, cmp->opcode);
				sp -= 2;
				cmp->inst_i0 = sp [0];
				cmp->inst_i1 = sp [1];
				cmp->cil_code = ip;
				type_from_op (cmp);
				CHECK_TYPE (cmp);
				if ((sp [0]->type == STACK_I8) || ((sizeof (gpointer) == 8) && ((sp [0]->type == STACK_PTR) || (sp [0]->type == STACK_OBJ) || (sp [0]->type == STACK_MP))))
					cmp->opcode = OP_LCOMPARE;
				else
					cmp->opcode = OP_COMPARE;
				ins->cil_code = ip;
				ins->type = STACK_I4;
				ins->inst_i0 = cmp;
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
				int temp;

				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				n = read32 (ip + 2);
				cmethod = mini_get_method (method, n, NULL, generic_context);

				mono_class_init (cmethod->klass);

				if (mono_use_security_manager) {
					check_linkdemand (cfg, method, cmethod, bblock, ip);
				}

				handle_loaded_temps (cfg, bblock, stack_start, sp);

				NEW_METHODCONST (cfg, argconst, cmethod);
				if (method->wrapper_type != MONO_WRAPPER_SYNCHRONIZED)
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldftn, &argconst, ip);
				else
					temp = mono_emit_jit_icall (cfg, bblock, mono_ldftn_nosync, &argconst, ip);
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
				cmethod = mini_get_method (method, n, NULL, generic_context);

				mono_class_init (cmethod->klass);

				if (mono_use_security_manager) {
					check_linkdemand (cfg, method, cmethod, bblock, ip);
				}

				handle_loaded_temps (cfg, bblock, stack_start, sp);

				--sp;
				args [0] = *sp;
				NEW_METHODCONST (cfg, args [1], cmethod);
				temp = mono_emit_jit_icall (cfg, bblock, mono_ldvirtfn, args, ip);
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
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDARGA:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_ARG (n);
				NEW_ARGLOADA (cfg, ins, n);
				ins->cil_code = ip;
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
				ins->cil_code = ip;
				if (ins->opcode == CEE_STOBJ) {
					NEW_ARGLOADA (cfg, ins, n);
					handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
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
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDLOCA:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (4);
				n = read16 (ip + 2);
				CHECK_LOCAL (n);
				NEW_LOCLOADA (cfg, ins, n);
				ins->cil_code = ip;
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
				ins->cil_code = ip;
				if (ins->opcode == CEE_STOBJ) {
					NEW_LOCLOADA (cfg, ins, n);
					handle_stobj (cfg, bblock, ins, *sp, ip, ins->klass, FALSE, FALSE);
				} else
					MONO_ADD_INS (bblock, ins);
				ip += 4;
				inline_costs += 1;
				break;
			case CEE_LOCALLOC:
				CHECK_STACK (1);
				--sp;
				if (sp != stack_start) 
					goto unverified;
				if (cfg->method != method) 
					/* 
					 * Inlining this into a loop in a parent could lead to 
					 * stack overflows which is different behavior than the
					 * non-inlined case, thus disable inlining in this case.
					 */
					goto inline_failure;
				MONO_INST_NEW (cfg, ins, OP_LOCALLOC);
				ins->inst_left = *sp;
				ins->cil_code = ip;
				ins->type = STACK_MP;

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
					goto unverified;
				MONO_INST_NEW (cfg, ins, OP_ENDFILTER);
				ins->inst_left = *sp;
				ins->cil_code = ip;
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
					goto unverified;

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
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					klass = mono_method_get_wrapper_data (method, token);
				else
					klass = mono_class_get_full (image, token, generic_context);
				if (MONO_TYPE_IS_REFERENCE (&klass->byval_arg)) {
					MonoInst *store, *load;
					NEW_PCONST (cfg, load, NULL);
					load->cil_code = ip;
					load->type = STACK_OBJ;
					MONO_INST_NEW (cfg, store, CEE_STIND_REF);
					store->cil_code = ip;
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
				ip += 6;
				break;
			case CEE_CPBLK:
			case CEE_INITBLK: {
				MonoInst *iargs [3];
				CHECK_STACK (3);
				sp -= 3;
				if ((cfg->opt & MONO_OPT_INTRINS) && (ip [1] == CEE_CPBLK) && (sp [2]->opcode == OP_ICONST) && ((n = sp [2]->inst_c0) <= sizeof (gpointer) * 5)) {
					MonoInst *copy;
					MONO_INST_NEW (cfg, copy, OP_MEMCPY);
					copy->inst_left = sp [0];
					copy->inst_right = sp [1];
					copy->cil_code = ip;
					copy->unused = n;
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
					if (MONO_OFFSET_IN_HANDLER (clause, ip - header->code) && !(clause->flags & MONO_EXCEPTION_CLAUSE_FINALLY))
						handler_offset = clause->handler_offset;
				}

				g_assert (handler_offset != -1);

				NEW_TEMPLOAD (cfg, load, mono_find_exvar_for_offset (cfg, handler_offset)->inst_c0);
				load->cil_code = ip;
				MONO_INST_NEW (cfg, ins, OP_RETHROW);
				ins->inst_left = load;
				ins->cil_code = ip;
				MONO_ADD_INS (bblock, ins);
				sp = stack_start;
				link_bblock (cfg, bblock, end_bblock);
				start_new_bblock = 1;
				ip += 2;
				mono_get_got_var (cfg);
				break;
			}
			case CEE_SIZEOF:
				CHECK_STACK_OVF (1);
				CHECK_OPSIZE (6);
				token = read32 (ip + 2);
				/* FIXXME: handle generics. */
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
					MonoType *type = mono_type_create_from_typespec (image, token);
					token = mono_type_size (type, &align);
				} else {
					MonoClass *szclass = mono_class_get_full (image, token, generic_context);
					mono_class_init (szclass);
					token = mono_class_value_size (szclass, &align);
				}
				NEW_ICONST (cfg, ins, token);
				ins->cil_code = ip;
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
				ins->cil_code = ip;
				ip += 2;
				*sp++ = ins;
				break;
			case CEE_READONLY_:
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
		goto unverified;

	bblock->cil_length = ip - bblock->cil_code;
	bblock->next_bb = end_bblock;

	if (cfg->method == method && cfg->domainvar) {
		MonoInst *store;
		MonoInst *get_domain;
		
		if (! (get_domain = mono_arch_get_domain_intrinsic (cfg))) {
			MonoCallInst *call;
			
			MONO_INST_NEW_CALL (cfg, call, CEE_CALL);
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
				MONO_INST_NEW (cfg, ins, OP_R8CONST);
				ins->type = STACK_R8;
				ins->inst_p0 = (void*)&r8_0;
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
			} else if ((t == MONO_TYPE_VALUETYPE) || (t == MONO_TYPE_TYPEDBYREF) ||
				   ((t == MONO_TYPE_GENERICINST) && mono_metadata_generic_class_is_valuetype (ptype->data.generic_class))) {
				NEW_LOCLOADA (cfg, ins, i);
				handle_initobj (cfg, init_localsbb, ins, NULL, mono_class_from_mono_type (ptype), NULL, NULL);
			} else {
				NEW_PCONST (cfg, ins, NULL);
				NEW_LOCSTORE (cfg, store, i, ins);
				MONO_ADD_INS (init_localsbb, store);
			}
		}
	}

	/* resolve backward branches in the middle of an existing basic block */
	for (tmp = bb_recheck; tmp; tmp = tmp->next) {
		bblock = tmp->data;
		/*g_print ("need recheck in %s at IL_%04x\n", method->name, bblock->cil_code - header->code);*/
		tblock = find_previous (bbhash, start_bblock, bblock->cil_code);
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
	} else {
		g_hash_table_destroy (bbhash);
	}

	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);
	return inline_costs;

 inline_failure:
	if (cfg->method != method) 
		g_hash_table_destroy (bbhash);
	g_slist_free (class_inits);
	dont_inline = g_list_remove (dont_inline, method);
	return -1;

 unverified:
	if (cfg->method != method) 
		g_hash_table_destroy (bbhash);
	g_slist_free (class_inits);
	g_error ("Invalid IL code at IL%04x in %s: %s\n", (int)(ip - header->code), 
		 mono_method_full_name (method, TRUE), mono_disasm_code_one (NULL, method, ip, NULL));
	dont_inline = g_list_remove (dont_inline, method);
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
	case CEE_CALL:
	case CEE_CALLVIRT:
	case OP_FCALL:
	case OP_FCALLVIRT:
	case OP_LCALL:
	case OP_LCALLVIRT:
	case OP_VCALL:
	case OP_VCALLVIRT:
	case OP_VOIDCALL:
	case OP_VOIDCALLVIRT: {
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
	case CEE_NOP:
	case CEE_JMP:
	case CEE_BREAK:
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
	case CEE_BR:
	case OP_CALL_HANDLER:
		printf ("[B%d]", tree->inst_target_bb->block_num);
		break;
	case CEE_SWITCH:
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
}

gconstpointer
mono_icall_get_wrapper (MonoJitICallInfo* callinfo)
{
	char *name;
	MonoMethod *wrapper;
	gconstpointer code;
	
	if (callinfo->wrapper)
		return callinfo->wrapper;
	
	name = g_strdup_printf ("__icall_wrapper_%s", callinfo->name);
	wrapper = mono_marshal_get_icall_wrapper (callinfo->sig, name, callinfo->func);
	/* Must be domain neutral since there is only one copy */
	code = mono_jit_compile_method_with_opt (wrapper, default_opt | MONO_OPT_SHARED);

	if (!callinfo->wrapper) {
		callinfo->wrapper = code;
		mono_register_jit_icall_wrapper (callinfo, code);
		mono_debug_add_icall_wrapper (wrapper, callinfo);
	}

	g_free (name);
	return callinfo->wrapper;
}

static void
mono_init_trampolines (void)
{
	mono_trampoline_code [MONO_TRAMPOLINE_GENERIC] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_GENERIC);
	mono_trampoline_code [MONO_TRAMPOLINE_JUMP] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_JUMP);
	mono_trampoline_code [MONO_TRAMPOLINE_CLASS_INIT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_CLASS_INIT);
#ifdef MONO_ARCH_HAVE_PIC_AOT
	mono_trampoline_code [MONO_TRAMPOLINE_AOT] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_AOT);
#endif
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	mono_trampoline_code [MONO_TRAMPOLINE_DELEGATE] = mono_arch_create_trampoline_code (MONO_TRAMPOLINE_DELEGATE);
#endif
}

guint8 *
mono_get_trampoline_code (MonoTrampolineType tramp_type)
{
	return mono_trampoline_code [tramp_type];
}

gpointer
mono_create_class_init_trampoline (MonoVTable *vtable)
{
	gpointer code, ptr;

	/* previously created trampoline code */
	mono_domain_lock (vtable->domain);
	ptr = 
		g_hash_table_lookup (vtable->domain->class_init_trampoline_hash,
								  vtable);
	mono_domain_unlock (vtable->domain);
	if (ptr)
		return ptr;

#ifdef MONO_ARCH_HAVE_CREATE_SPECIFIC_TRAMPOLINE
	code = mono_arch_create_specific_trampoline (vtable, MONO_TRAMPOLINE_CLASS_INIT, vtable->domain, NULL);
#else
	code = mono_arch_create_class_init_trampoline (vtable);
#endif

	ptr = mono_create_ftnptr (vtable->domain, code);

	/* store trampoline address */
	mono_domain_lock (vtable->domain);
	g_hash_table_insert (vtable->domain->class_init_trampoline_hash,
							  vtable, ptr);
	mono_domain_unlock (vtable->domain);

	EnterCriticalSection (&jit_mutex);
	if (!class_init_hash_addr)
		class_init_hash_addr = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (class_init_hash_addr, ptr, vtable);
	LeaveCriticalSection (&jit_mutex);

	return ptr;
}

gpointer
mono_create_jump_trampoline (MonoDomain *domain, MonoMethod *method, 
							 gboolean add_sync_wrapper)
{
	MonoJitInfo *ji;
	gpointer code;
#ifdef MONO_ARCH_HAVE_CREATE_SPECIFIC_TRAMPOLINE
	guint32 code_size;
#endif

	if (add_sync_wrapper && method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_create_jump_trampoline (domain, mono_marshal_get_synchronized_wrapper (method), FALSE);

	code = mono_jit_find_compiled_method (domain, method);
	if (code)
		return code;

	mono_domain_lock (domain);
	code = g_hash_table_lookup (domain->jump_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (code)
		return code;

#ifdef MONO_ARCH_HAVE_CREATE_SPECIFIC_TRAMPOLINE
	code = mono_arch_create_specific_trampoline (method, MONO_TRAMPOLINE_JUMP, mono_domain_get (), &code_size);

	ji = g_new0 (MonoJitInfo, 1);
	ji->code_start = code;
	ji->code_size = code_size;
	ji->method = method;
#else
	ji = mono_arch_create_jump_trampoline (method);
#endif

	/*
	 * mono_delegate_ctor needs to find the method metadata from the 
	 * trampoline address, so we save it here.
	 */

	mono_jit_info_table_add (domain, ji);

	mono_domain_lock (domain);
	g_hash_table_insert (domain->jump_trampoline_hash, method, ji->code_start);
	mono_domain_unlock (domain);

	return ji->code_start;
}

gpointer
mono_create_jit_trampoline (MonoMethod *method)
{
	MonoDomain *domain = mono_domain_get ();
	gpointer tramp;

	mono_domain_lock (domain);
	tramp = g_hash_table_lookup (domain->jit_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (tramp)
		return tramp;

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
		return mono_create_jit_trampoline (mono_marshal_get_synchronized_wrapper (method));

#ifdef MONO_ARCH_HAVE_CREATE_SPECIFIC_TRAMPOLINE
	tramp =  mono_arch_create_specific_trampoline (method, MONO_TRAMPOLINE_GENERIC, mono_domain_get (), NULL);
#else
	tramp = mono_arch_create_jit_trampoline (method);
#endif
	
	mono_domain_lock (domain);
	g_hash_table_insert (domain->jit_trampoline_hash, method, tramp);
	mono_domain_unlock (domain);

	mono_jit_stats.method_trampolines++;

	return tramp;
}	

#ifdef MONO_ARCH_HAVE_CREATE_TRAMPOLINE_FROM_TOKEN
gpointer
mono_create_jit_trampoline_from_token (MonoImage *image, guint32 token)
{
	gpointer tramp;

	MonoDomain *domain = mono_domain_get ();
	guint8 *buf, *start;

	mono_domain_lock (domain);
	buf = start = mono_code_manager_reserve (domain->code_mp, 2 * sizeof (gpointer));
	mono_domain_unlock (domain);

	*(gpointer*)(gpointer)buf = image;
	buf += sizeof (gpointer);
	*(guint32*)(gpointer)buf = token;

	tramp = mono_arch_create_specific_trampoline (start, MONO_TRAMPOLINE_AOT, domain, NULL);

	mono_jit_stats.method_trampolines++;

	return tramp;
}	
#endif

gpointer
mono_create_delegate_trampoline (MonoMethod *method, gpointer addr)
{
#ifdef MONO_ARCH_HAVE_CREATE_DELEGATE_TRAMPOLINE
	gpointer code, ptr;
	guint32 code_size;
	MonoDomain *domain = mono_domain_get ();

#ifndef __ia64__
	code = mono_jit_find_compiled_method (domain, method);
	if (code)
		return code;
#else
	/* 
	 * FIXME: We should return a function descriptor here but it is not stored
	 * anywhere so it would be leaked.
	 */
#endif

	mono_domain_lock (domain);
	ptr = g_hash_table_lookup (domain->delegate_trampoline_hash, method);
	mono_domain_unlock (domain);
	if (ptr)
		return ptr;

	code = mono_arch_create_specific_trampoline (method, MONO_TRAMPOLINE_DELEGATE, domain, &code_size);

	ptr = mono_create_ftnptr (domain, code);

	/* store trampoline address */
	mono_domain_lock (domain);
	g_hash_table_insert (domain->delegate_trampoline_hash,
							  method, ptr);
	mono_domain_unlock (domain);

	return ptr;
#else
	return addr;
#endif
}

MonoVTable*
mono_find_class_init_trampoline_by_addr (gconstpointer addr)
{
	MonoVTable *res;

	EnterCriticalSection (&jit_mutex);
	if (class_init_hash_addr)
		res = g_hash_table_lookup (class_init_hash_addr, addr);
	else
		res = NULL;
	LeaveCriticalSection (&jit_mutex);
	return res;
}

static void
mono_dynamic_code_hash_insert (MonoDomain *domain, MonoMethod *method, MonoJitDynamicMethodInfo *ji)
{
	if (!domain->dynamic_code_hash)
		domain->dynamic_code_hash = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (domain->dynamic_code_hash, method, ji);
}

static MonoJitDynamicMethodInfo*
mono_dynamic_code_hash_lookup (MonoDomain *domain, MonoMethod *method)
{
	MonoJitDynamicMethodInfo *res;

	if (domain->dynamic_code_hash)
		res = g_hash_table_lookup (domain->dynamic_code_hash, method);
	else
		res = NULL;
	return res;
}

typedef struct {
	MonoClass *vtype;
	GList *active;
	GList *slots;
} StackSlotInfo;

/*
 * mono_allocate_stack_slots:
 *
 *  Allocate stack slots for all non register allocated variables using a
 * linear scan algorithm.
 * Returns: an array of stack offsets which the caller should free.
 * STACK_SIZE is set to the amount of stack space needed.
 * STACK_ALIGN is set to the alignment needed by the locals area.
 */
gint32*
mono_allocate_stack_slots (MonoCompile *m, guint32 *stack_size, guint32 *stack_align)
{
	int i, slot, offset, size, align;
	MonoMethodVar *vmv;
	MonoInst *inst;
	gint32 *offsets;
	GList *vars = NULL, *l;
	StackSlotInfo *scalar_stack_slots, *vtype_stack_slots, *slot_info;
	MonoType *t;
	int nvtypes;

	scalar_stack_slots = g_new0 (StackSlotInfo, MONO_TYPE_PINNED);
	vtype_stack_slots = g_new0 (StackSlotInfo, 256);
	nvtypes = 0;

	offsets = g_new (gint32, m->num_varinfo);
	for (i = 0; i < m->num_varinfo; ++i)
		offsets [i] = -1;

	for (i = m->locals_start; i < m->num_varinfo; i++) {
		inst = m->varinfo [i];
		vmv = MONO_VARINFO (m, i);

		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR || inst->opcode == OP_REGOFFSET)
			continue;

		vars = g_list_prepend (vars, vmv);
	}

	vars = mono_varlist_sort (m, vars, 0);
	offset = 0;
	*stack_align = 0;
	for (l = vars; l; l = l->next) {
		vmv = l->data;
		inst = m->varinfo [vmv->idx];

		/* inst->unused indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->unused && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
			size = mono_class_native_size (inst->inst_vtype->data.klass, &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		t = mono_type_get_underlying_type (inst->inst_vtype);
		switch (t->type) {
		case MONO_TYPE_VALUETYPE:
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
		default:
			slot_info = &scalar_stack_slots [t->type];
		}

		slot = 0xffffff;
		if (m->comp_done & MONO_COMP_LIVENESS) {
			//printf ("START  %2d %08x %08x\n",  vmv->idx, vmv->range.first_use.abs_pos, vmv->range.last_use.abs_pos);
			
			/* expire old intervals in active */
			while (slot_info->active) {
				MonoMethodVar *amv = (MonoMethodVar *)slot_info->active->data;

				if (amv->range.last_use.abs_pos > vmv->range.first_use.abs_pos)
					break;

				//printf ("EXPIR  %2d %08x %08x C%d R%d\n", amv->idx, amv->range.first_use.abs_pos, amv->range.last_use.abs_pos, amv->spill_costs, amv->reg);

				slot_info->active = g_list_delete_link (slot_info->active, slot_info->active);
				slot_info->slots = g_list_prepend (slot_info->slots, GINT_TO_POINTER (offsets [amv->idx]));
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

					slot_info->slots = g_list_delete_link (slot_info->slots, slot_info->slots);
				}

				slot_info->active = mono_varlist_insert_sorted (m, slot_info->active, vmv, TRUE);
			}
		}

		{
			static int count = 0;
			count ++;

			/*
			if (count == atoi (getenv ("COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (m->method, TRUE));
			if (count > atoi (getenv ("COUNT")))
				slot = 0xffffff;
			else {
				mono_print_tree_nl (inst);
				}
			*/
		}
		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (gpointer) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (t->type == MONO_TYPE_VALUETYPE)
				align = sizeof (gpointer);

			offset += size;
			offset += align - 1;
			offset &= ~(align - 1);
			slot = offset;

			if (*stack_align == 0)
				*stack_align = align;
		}

		offsets [vmv->idx] = slot;
	}
	g_list_free (vars);
	for (i = 0; i < MONO_TYPE_PINNED; ++i) {
		g_list_free (scalar_stack_slots [i].active);
		g_list_free (scalar_stack_slots [i].slots);
	}
	for (i = 0; i < nvtypes; ++i) {
		g_list_free (vtype_stack_slots [i].active);
		g_list_free (vtype_stack_slots [i].slots);
	}
	g_free (scalar_stack_slots);
	g_free (vtype_stack_slots);

	*stack_size = offset;
	return offsets;
}

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
mono_print_bb_code (MonoBasicBlock *bb) {
	if (bb->code) {
		MonoInst *c = bb->code;
		while (c) {
			mono_print_tree (c);
			g_print ("\n");
			c = c->next;
		}
	}
}

static void
print_dfn (MonoCompile *cfg) {
	int i, j;
	char *code;
	MonoBasicBlock *bb;

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
		g_print ("\nBB%d DFN%d (len: %d): %s", bb->block_num, i, bb->cil_length, code);
		if (bb->code) {
			MonoInst *c = bb->code;
			while (c) {
				mono_print_tree (c);
				g_print ("\n");
				c = c->next;
			}
		} else {

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
	inst->next = NULL;
	if (bb->last_ins) {
		g_assert (bb->code);
		bb->last_ins->next = inst;
		bb->last_ins = inst;
	} else {
		bb->last_ins = bb->code = inst;
	}
}

void
mono_destroy_compile (MonoCompile *cfg)
{
	//mono_mempool_stats (cfg->mempool);
	g_hash_table_destroy (cfg->bb_hash);
	mono_free_loop_info (cfg);
	if (cfg->rs)
		mono_regstate_free (cfg->rs);
	if (cfg->spvars)
		g_hash_table_destroy (cfg->spvars);
	if (cfg->exvars)
		g_hash_table_destroy (cfg->exvars);
	mono_mempool_destroy (cfg->mempool);
	g_list_free (cfg->ldstr_list);

	g_free (cfg->varinfo);
	g_free (cfg->vars);
	g_free (cfg);
}

#ifdef HAVE_KW_THREAD
static __thread gpointer mono_lmf_addr MONO_TLS_FAST;
#endif

guint32
mono_get_jit_tls_key (void)
{
	return mono_jit_tls_id;
}

gint32
mono_get_lmf_tls_offset (void)
{
	int offset;
	MONO_THREAD_VAR_OFFSET(mono_lmf_addr,offset);
	return offset;
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

	mono_thread_exit ();
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

	jit_tls->abort_func = abort_func;
	jit_tls->end_of_stack = stack_start;

	lmf = g_new0 (MonoLMF, 1);
	lmf->ebp = -1;

	jit_tls->lmf = jit_tls->first_lmf = lmf;

#ifdef HAVE_KW_THREAD
	mono_lmf_addr = &jit_tls->lmf;
#endif

	mono_arch_setup_jit_tls_data (jit_tls);

	return jit_tls;
}

static void
mono_thread_start_cb (gsize tid, gpointer stack_start, gpointer func)
{
	MonoThread *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort);
	thread = mono_thread_current ();
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
		mono_arch_free_jit_tls_data (jit_tls);
		g_free (jit_tls->first_lmf);
		g_free (jit_tls);
		thread->jit_data = NULL;
	}
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

gpointer
mono_resolve_patch_target (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *patch_info, gboolean run_cctors)
{
	unsigned char *ip = patch_info->ip.i + code;
	gconstpointer target = NULL;

	switch (patch_info->type) {
	case MONO_PATCH_INFO_BB:
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
	case MONO_PATCH_INFO_METHOD_JUMP: {
		GSList *list;

		/* get the trampoline to the method from the domain */
		target = mono_create_jump_trampoline (domain, patch_info->data.method, TRUE);
		if (!domain->jump_target_hash)
			domain->jump_target_hash = g_hash_table_new (NULL, NULL);
		list = g_hash_table_lookup (domain->jump_target_hash, patch_info->data.method);
		list = g_slist_prepend (list, ip);
		g_hash_table_insert (domain->jump_target_hash, patch_info->data.method, list);
		break;
	}
	case MONO_PATCH_INFO_METHOD:
		if (patch_info->data.method == method) {
			target = code;
		} else
			/* get the trampoline to the method from the domain */
			target = mono_create_jit_trampoline (patch_info->data.method);
		break;
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *jump_table;
		int i;

		if (method && method->dynamic) {
			jump_table = mono_code_manager_reserve (mono_dynamic_code_hash_lookup (domain, method)->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
		} else {
			mono_domain_lock (domain);
			jump_table = mono_code_manager_reserve (domain->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
			mono_domain_unlock (domain);
		}

		for (i = 0; i < patch_info->data.table->table_size; i++) {
			jump_table [i] = code + GPOINTER_TO_INT (patch_info->data.table->table [i]);
		}
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
	case MONO_PATCH_INFO_VTABLE:
		target = mono_class_vtable (domain, patch_info->data.klass);
		break;
	case MONO_PATCH_INFO_CLASS_INIT:
		target = mono_create_class_init_trampoline (mono_class_vtable (domain, patch_info->data.klass));
		break;
	case MONO_PATCH_INFO_SFLDA: {
		MonoVTable *vtable = mono_class_vtable (domain, patch_info->data.field->parent);
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
				       patch_info->data.token->token, &handle_class, NULL);
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
	case MONO_PATCH_INFO_BB_OVF:
	case MONO_PATCH_INFO_EXC_OVF:
	case MONO_PATCH_INFO_GOT_OFFSET:
	case MONO_PATCH_INFO_NONE:
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
		for (tree = cfg->cbb->code; tree; tree = tree->next) {
			dec_foreach (tree, cfg);
			cfg->prev_ins = tree;
		}
	}
}

static void
nullify_basic_block (MonoBasicBlock *bb) 
{
	bb->in_count = 0;
	bb->out_count = 0;
	bb->in_bb = NULL;
	bb->out_bb = NULL;
	bb->next_bb = NULL;
	bb->code = bb->last_ins = NULL;
	bb->cil_code = NULL;
}

static void 
replace_out_block (MonoBasicBlock *bb, MonoBasicBlock *orig,  MonoBasicBlock *repl)
{
	int i;

	for (i = 0; i < bb->out_count; i++) {
		MonoBasicBlock *ob = bb->out_bb [i];
		if (ob == orig) {
			if (!repl) {
				if (bb->out_count > 1) {
					bb->out_bb [i] = bb->out_bb [bb->out_count - 1];
				}
				bb->out_count--;
			} else {
				bb->out_bb [i] = repl;
			}
		}
	}
}

static void 
replace_in_block (MonoBasicBlock *bb, MonoBasicBlock *orig, MonoBasicBlock *repl)
{
	int i;

	for (i = 0; i < bb->in_count; i++) {
		MonoBasicBlock *ib = bb->in_bb [i];
		if (ib == orig) {
			if (!repl) {
				if (bb->in_count > 1) {
					bb->in_bb [i] = bb->in_bb [bb->in_count - 1];
				}
				bb->in_count--;
			} else {
				bb->in_bb [i] = repl;
			}
		}
	}
}

static void 
replace_or_add_in_block (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *orig, MonoBasicBlock *repl)
{
	gboolean found = FALSE;
	int i;

	for (i = 0; i < bb->in_count; i++) {
		MonoBasicBlock *ib = bb->in_bb [i];
		if (ib == orig) {
			if (!repl) {
				if (bb->in_count > 1) {
					bb->in_bb [i] = bb->in_bb [bb->in_count - 1];
				}
				bb->in_count--;
			} else {
				bb->in_bb [i] = repl;
			}
			found = TRUE;
		}
	}
	
	if (! found) {
		MonoBasicBlock **new_in_bb = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (bb->in_count + 1));
		for (i = 0; i < bb->in_count; i++) {
			new_in_bb [i] = bb->in_bb [i];
		}
		new_in_bb [i] = repl;
		bb->in_count++;
		bb->in_bb = new_in_bb;
	}
}


static void
replace_out_block_in_code (MonoBasicBlock *bb, MonoBasicBlock *orig, MonoBasicBlock *repl) {
	MonoInst *inst;
	
	for (inst = bb->code; inst != NULL; inst = inst->next) {
		if (inst->opcode == OP_CALL_HANDLER) {
			if (inst->inst_target_bb == orig) {
				inst->inst_target_bb = repl;
			}
		}
	}
	if (bb->last_ins != NULL) {
		switch (bb->last_ins->opcode) {
		case CEE_BR:
			if (bb->last_ins->inst_target_bb == orig) {
				bb->last_ins->inst_target_bb = repl;
			}
			break;
		case CEE_SWITCH: {
			int i;
			int n = GPOINTER_TO_INT (bb->last_ins->klass);
			for (i = 0; i < n; i++ ) {
				if (bb->last_ins->inst_many_bb [i] == orig) {
					bb->last_ins->inst_many_bb [i] = repl;
				}
			}
			break;
		}
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
			if (bb->last_ins->inst_true_bb == orig) {
				bb->last_ins->inst_true_bb = repl;
			}
			if (bb->last_ins->inst_false_bb == orig) {
				bb->last_ins->inst_false_bb = repl;
			}
			break;
		default:
			break;
		}
	}
}

static void 
replace_basic_block (MonoBasicBlock *bb, MonoBasicBlock *orig,  MonoBasicBlock *repl)
{
	int i, j;

	for (i = 0; i < bb->out_count; i++) {
		MonoBasicBlock *ob = bb->out_bb [i];
		for (j = 0; j < ob->in_count; j++) {
			if (ob->in_bb [j] == orig) {
				ob->in_bb [j] = repl;
			}
		}
	}

}

/**
  * Check if a bb is useless (is just made of NOPs and ends with an
  * unconditional branch, or nothing).
  * If it is so, unlink it from the CFG and nullify it, and return TRUE.
  * Otherwise, return FALSE;
  */
static gboolean
remove_block_if_useless (MonoCompile *cfg, MonoBasicBlock *bb, MonoBasicBlock *previous_bb) {
	MonoBasicBlock *target_bb = NULL;
	MonoInst *inst;
	
	/* Do not touch handlers */
	if (bb->region != -1) return FALSE;
	
	for (inst = bb->code; inst != NULL; inst = inst->next) {
		switch (inst->opcode) {
		case CEE_NOP:
			break;
		case CEE_BR:
			target_bb = inst->inst_target_bb;
			break;
		default:
			return FALSE;
		}
	}
	
	if (target_bb == NULL) {
		if ((bb->out_count == 1) && (bb->out_bb [0] == bb->next_bb)) {
			target_bb = bb->next_bb;
		} else {
			/* Do not touch empty BBs that do not "fall through" to their next BB (like the exit BB) */
			return FALSE;
		}
	}
	
	/* Do not touch BBs following a switch (they are the "default" branch) */
	if ((previous_bb->last_ins != NULL) && (previous_bb->last_ins->opcode == CEE_SWITCH)) {
		return FALSE;
	}
	
	/* Do not touch BBs following the entry BB and jumping to something that is not */
	/* thiry "next" bb (the entry BB cannot contain the branch) */
	if ((previous_bb == cfg->bb_entry) && (bb->next_bb != target_bb)) {
		return FALSE;
	}
	
	/* Check that there is a target BB, and that bb is not an empty loop (Bug 75061) */
	if ((target_bb != NULL) && (target_bb != bb)) {
		int i;

		if (cfg->verbose_level > 1) {
			printf ("remove_block_if_useless %s, removed BB%d\n", mono_method_full_name (cfg->method, TRUE), bb->block_num);
		}
		
		for (i = 0; i < bb->in_count; i++) {
			MonoBasicBlock *in_bb = bb->in_bb [i];
			replace_out_block (in_bb, bb, target_bb);
			replace_out_block_in_code (in_bb, bb, target_bb);
			if (bb->in_count == 1) {
				replace_in_block (target_bb, bb, in_bb);
			} else {
				replace_or_add_in_block (cfg, target_bb, bb, in_bb);
			}
		}
		
		if ((previous_bb != cfg->bb_entry) &&
				(previous_bb->region == bb->region) &&
				((previous_bb->last_ins == NULL) ||
				((previous_bb->last_ins->opcode != CEE_BR) &&
				(! (MONO_IS_COND_BRANCH_OP (previous_bb->last_ins))) &&
				(previous_bb->last_ins->opcode != CEE_SWITCH)))) {
			for (i = 0; i < previous_bb->out_count; i++) {
				if (previous_bb->out_bb [i] == target_bb) {
					MonoInst *jump;
					MONO_INST_NEW (cfg, jump, CEE_BR);
					MONO_ADD_INS (previous_bb, jump);
					jump->cil_code = previous_bb->cil_code;
					jump->inst_target_bb = target_bb;
					break;
				}
			}
		}
		
		previous_bb->next_bb = bb->next_bb;
		nullify_basic_block (bb);
		
		return TRUE;
	} else {
		return FALSE;
	}
}

static void
merge_basic_blocks (MonoBasicBlock *bb, MonoBasicBlock *bbn) 
{
	bb->out_count = bbn->out_count;
	bb->out_bb = bbn->out_bb;

	replace_basic_block (bb, bbn, bb);

	if (bb->last_ins) {
		if (bbn->code) {
			bb->last_ins->next = bbn->code;
			bb->last_ins = bbn->last_ins;
		}
	} else {
		bb->code = bbn->code;
		bb->last_ins = bbn->last_ins;
	}
	bb->next_bb = bbn->next_bb;
	nullify_basic_block (bbn);
}

static void
move_basic_block_to_end (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoBasicBlock *bbn, *next;

	next = bb->next_bb;

	/* Find the previous */
	for (bbn = cfg->bb_entry; bbn->next_bb && bbn->next_bb != bb; bbn = bbn->next_bb)
		;
	if (bbn->next_bb) {
		bbn->next_bb = bb->next_bb;
	}

	/* Find the last */
	for (bbn = cfg->bb_entry; bbn->next_bb; bbn = bbn->next_bb)
		;
	bbn->next_bb = bb;
	bb->next_bb = NULL;

	/* Add a branch */
	if (next && (!bb->last_ins || (bb->last_ins->opcode != OP_NOT_REACHED))) {
		MonoInst *ins;

		MONO_INST_NEW (cfg, ins, CEE_BR);
		MONO_ADD_INS (bb, ins);
		link_bblock (cfg, bb, next);
		ins->inst_target_bb = next;
	}		
}

/*
 * Optimizes the branches on the Control Flow Graph
 *
 */
static void
optimize_branches (MonoCompile *cfg)
{
	int i, changed = FALSE;
	MonoBasicBlock *bb, *bbn;
	guint32 niterations;

	/*
	 * Some crazy loops could cause the code below to go into an infinite
	 * loop, see bug #53003 for an example. To prevent this, we put an upper
	 * bound on the number of iterations.
	 */
	if (cfg->num_bblocks > 1000)
		niterations = cfg->num_bblocks * 2;
	else
		niterations = 1000;
	do {
		MonoBasicBlock *previous_bb;
		changed = FALSE;
		niterations --;

		/* we skip the entry block (exit is handled specially instead ) */
		for (previous_bb = cfg->bb_entry, bb = cfg->bb_entry->next_bb; bb; previous_bb = bb, bb = bb->next_bb) {

			/* dont touch code inside exception clauses */
			if (bb->region != -1)
				continue;

			if (remove_block_if_useless (cfg, bb, previous_bb)) {
				changed = TRUE;
				continue;
			}

			if ((bbn = bb->next_bb) && bbn->in_count == 0 && bb->region == bbn->region) {
				if (cfg->verbose_level > 2)
					g_print ("nullify block triggered %d\n", bbn->block_num);

				bb->next_bb = bbn->next_bb;

				for (i = 0; i < bbn->out_count; i++)
					replace_in_block (bbn->out_bb [i], bbn, NULL);

				nullify_basic_block (bbn);			
				changed = TRUE;
			}

			if (bb->out_count == 1) {
				bbn = bb->out_bb [0];

				/* conditional branches where true and false targets are the same can be also replaced with CEE_BR */
				if (bb->last_ins && MONO_IS_COND_BRANCH_OP (bb->last_ins)) {
					MonoInst *pop;
					MONO_INST_NEW (cfg, pop, CEE_POP);
					pop->inst_left = bb->last_ins->inst_left->inst_left;
					mono_add_ins_to_end (bb, pop);
					MONO_INST_NEW (cfg, pop, CEE_POP);
					pop->inst_left = bb->last_ins->inst_left->inst_right;
					mono_add_ins_to_end (bb, pop);
					bb->last_ins->opcode = CEE_BR;
					bb->last_ins->inst_target_bb = bb->last_ins->inst_true_bb;
					changed = TRUE;
					if (cfg->verbose_level > 2)
						g_print ("cond branch removal triggered in %d %d\n", bb->block_num, bb->out_count);
				}

				if (bb->region == bbn->region && bb->next_bb == bbn) {
					/* the block are in sequence anyway ... */

					/* branches to the following block can be removed */
					if (bb->last_ins && bb->last_ins->opcode == CEE_BR) {
						bb->last_ins->opcode = CEE_NOP;
						changed = TRUE;
						if (cfg->verbose_level > 2)
							g_print ("br removal triggered %d -> %d\n", bb->block_num, bbn->block_num);
					}

					if (bbn->in_count == 1) {

						if (bbn != cfg->bb_exit) {
							if (cfg->verbose_level > 2)
								g_print ("block merge triggered %d -> %d\n", bb->block_num, bbn->block_num);
							merge_basic_blocks (bb, bbn);
							changed = TRUE;
						}

						//mono_print_bb_code (bb);
					}
				}				
			}
		}
	} while (changed && (niterations > 0));

	niterations = 1000;
	do {
		changed = FALSE;
		niterations --;

		/* we skip the entry block (exit is handled specially instead ) */
		for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {

			/* dont touch code inside exception clauses */
			if (bb->region != -1)
				continue;

			if ((bbn = bb->next_bb) && bbn->in_count == 0 && bb->region == bbn->region) {
				if (cfg->verbose_level > 2) {
					g_print ("nullify block triggered %d\n", bbn->block_num);
				}
				bb->next_bb = bbn->next_bb;

				for (i = 0; i < bbn->out_count; i++)
					replace_in_block (bbn->out_bb [i], bbn, NULL);

				nullify_basic_block (bbn);			
				changed = TRUE;
				break;
			}


			if (bb->out_count == 1) {
				bbn = bb->out_bb [0];

				if (bb->last_ins && bb->last_ins->opcode == CEE_BR) {
					bbn = bb->last_ins->inst_target_bb;
					if (bb->region == bbn->region && bbn->code && bbn->code->opcode == CEE_BR &&
					    bbn->code->inst_target_bb->region == bb->region) {
						
						if (cfg->verbose_level > 2)
							g_print ("in %s branch to branch triggered %d -> %d -> %d\n", cfg->method->name, 
								 bb->block_num, bbn->block_num, bbn->code->inst_target_bb->block_num);

						replace_in_block (bbn, bb, NULL);
						replace_out_block (bb, bbn, bbn->code->inst_target_bb);
						link_bblock (cfg, bb, bbn->code->inst_target_bb);
						bb->last_ins->inst_target_bb = bbn->code->inst_target_bb;
						changed = TRUE;
						break;
					}
				}
			} else if (bb->out_count == 2) {
				if (bb->last_ins && MONO_IS_COND_BRANCH_NOFP (bb->last_ins)) {
					int branch_result = mono_eval_cond_branch (bb->last_ins);
					MonoBasicBlock *taken_branch_target = NULL, *untaken_branch_target = NULL;
					if (branch_result == BRANCH_TAKEN) {
						taken_branch_target = bb->last_ins->inst_true_bb;
						untaken_branch_target = bb->last_ins->inst_false_bb;
					} else if (branch_result == BRANCH_NOT_TAKEN) {
						taken_branch_target = bb->last_ins->inst_false_bb;
						untaken_branch_target = bb->last_ins->inst_true_bb;
					}
					if (taken_branch_target) {
						/* if mono_eval_cond_branch () is ever taken to handle 
						 * non-constant values to compare, issue a pop here.
						 */
						bb->last_ins->opcode = CEE_BR;
						bb->last_ins->inst_target_bb = taken_branch_target;
						replace_out_block (bb, untaken_branch_target, NULL);
						replace_in_block (untaken_branch_target, bb, NULL);
						changed = TRUE;
						break;
					}
					bbn = bb->last_ins->inst_true_bb;
					if (bb->region == bbn->region && bbn->code && bbn->code->opcode == CEE_BR &&
					    bbn->code->inst_target_bb->region == bb->region) {
						if (cfg->verbose_level > 2)		
							g_print ("cbranch1 to branch triggered %d -> (%d) %d (0x%02x)\n", 
								 bb->block_num, bbn->block_num, bbn->code->inst_target_bb->block_num, 
								 bbn->code->opcode);

						bb->last_ins->inst_true_bb = bbn->code->inst_target_bb;

						replace_in_block (bbn, bb, NULL);
						if (!bbn->in_count)
							replace_in_block (bbn->code->inst_target_bb, bbn, bb);
						replace_out_block (bb, bbn, bbn->code->inst_target_bb);

						link_bblock (cfg, bb, bbn->code->inst_target_bb);

						changed = TRUE;
						break;
					}

					bbn = bb->last_ins->inst_false_bb;
					if (bb->region == bbn->region && bbn->code && bbn->code->opcode == CEE_BR &&
					    bbn->code->inst_target_bb->region == bb->region) {
						if (cfg->verbose_level > 2)
							g_print ("cbranch2 to branch triggered %d -> (%d) %d (0x%02x)\n", 
								 bb->block_num, bbn->block_num, bbn->code->inst_target_bb->block_num, 
								 bbn->code->opcode);

						bb->last_ins->inst_false_bb = bbn->code->inst_target_bb;

						replace_in_block (bbn, bb, NULL);
						if (!bbn->in_count)
							replace_in_block (bbn->code->inst_target_bb, bbn, bb);
						replace_out_block (bb, bbn, bbn->code->inst_target_bb);

						link_bblock (cfg, bb, bbn->code->inst_target_bb);

						changed = TRUE;
						break;
					}
				}

				if (bb->last_ins && MONO_IS_COND_BRANCH_NOFP (bb->last_ins)) {
					if (bb->last_ins->inst_false_bb->out_of_line && (bb->region == bb->last_ins->inst_false_bb->region)) {
						/* Reverse the branch */
						bb->last_ins->opcode = reverse_branch_op (bb->last_ins->opcode);
						bbn = bb->last_ins->inst_false_bb;
						bb->last_ins->inst_false_bb = bb->last_ins->inst_true_bb;
						bb->last_ins->inst_true_bb = bbn;

						move_basic_block_to_end (cfg, bb->last_ins->inst_true_bb);
						if (cfg->verbose_level > 2)
							g_print ("cbranch to throw block triggered %d.\n", 
									 bb->block_num);
					}
				}
			}
		}
	} while (changed && (niterations > 0));

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
		cfg->ret = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
		cfg->ret->opcode = OP_RETARG;
		cfg->ret->inst_vtype = sig->ret;
		cfg->ret->klass = mono_class_from_mono_type (sig->ret);
	}
	if (cfg->verbose_level > 2)
		g_print ("creating vars\n");

	if (sig->hasthis)
		mono_compile_create_var (cfg, &cfg->method->klass->this_arg, OP_ARG);

	for (i = 0; i < sig->param_count; ++i) {
		mono_compile_create_var (cfg, sig->params [i], OP_ARG);
		if (sig->params [i]->byref) {
			cfg->disable_ssa = TRUE;
		}
	}

	cfg->locals_start = cfg->num_varinfo;

	if (cfg->verbose_level > 2)
		g_print ("creating locals\n");
	for (i = 0; i < header->num_locals; ++i)
		mono_compile_create_var (cfg, header->locals [i], OP_LOCAL);
	if (cfg->verbose_level > 2)
		g_print ("locals done\n");

#ifdef MONO_ARCH_HAVE_CREATE_VARS
	mono_arch_create_vars (cfg);
#endif
}

void
mono_print_code (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *tree = bb->code;	

		if (!tree)
			continue;
		
		g_print ("CODE BLOCK %d (nesting %d):\n", bb->block_num, bb->nesting);

		for (; tree; tree = tree->next) {
			mono_print_tree (tree);
			g_print ("\n");
		}

		if (bb->last_ins)
			bb->last_ins->next = NULL;
	}
}

extern const char * const mono_burg_rule_string [];

static void
emit_state (MonoCompile *cfg, MBState *state, int goal)
{
	MBState *kids [10];
	int ern = mono_burg_rule (state, goal);
	const guint16 *nts = mono_burg_nts [ern];
	MBEmitFunc emit;

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
		state->reg1 = mono_regstate_next_float (cfg->rs);
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
				g_assert (!nts [3]);
				emit_state (cfg, kids [2], nts [2]);
			}
		}
	}

//	g_print ("emit: %s (%p)\n", mono_burg_rule_string [ern], state);
	if ((emit = mono_burg_func [ern]))
		emit (state, state->tree, cfg);	
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
		    bb->next_bb != bb->last_ins->inst_false_bb) {

			/* we are careful when inverting, since bugs like #59580
			 * could show up when dealing with NaNs.
			 */
			if (MONO_IS_COND_BRANCH_NOFP(bb->last_ins) && bb->next_bb == bb->last_ins->inst_true_bb) {
				MonoBasicBlock *tmp =  bb->last_ins->inst_true_bb;
				bb->last_ins->inst_true_bb = bb->last_ins->inst_false_bb;
				bb->last_ins->inst_false_bb = tmp;

				bb->last_ins->opcode = reverse_branch_op (bb->last_ins->opcode);
			} else {			
				MonoInst *inst = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
				inst->opcode = CEE_BR;
				inst->inst_target_bb = bb->last_ins->inst_false_bb;
				mono_bblock_add_inst (bb, inst);
			}
		}
	}

#ifdef DEBUG_SELECTION
	if (cfg->verbose_level >= 4) {
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *tree = bb->code;	
		g_print ("DUMP BLOCK %d:\n", bb->block_num);
		if (!tree)
			continue;
		for (; tree; tree = tree->next) {
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

			if (!(mbstate = mono_burg_label (tree, cfg))) {
				g_warning ("unable to label tree %p", tree);
				mono_print_tree (tree);
				g_print ("\n");				
				g_assert_not_reached ();
			}
			emit_state (cfg, mbstate, MB_NTERM_stmt);
		}
		bb->max_ireg = cfg->rs->next_vireg;
		bb->max_freg = cfg->rs->next_vfreg;

		if (bb->last_ins)
			bb->last_ins->next = NULL;

		mono_mempool_empty (cfg->state_pool); 
	}
	mono_mempool_destroy (cfg->state_pool); 
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
		mono_arch_local_regalloc (cfg, bb);
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
		mono_arch_output_basic_block (cfg, bb);

		if (bb == cfg->bb_exit) {
			cfg->epilog_begin = cfg->code_len;

			if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE) {
				code = cfg->native_code + cfg->code_len;
				code = mono_arch_instrument_epilog (cfg, mono_profiler_method_leave, code, FALSE);
				cfg->code_len = code - cfg->native_code;
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
		/* Allocate the code into a separate memory pool so it can be freed */
		cfg->dynamic_info = g_new0 (MonoJitDynamicMethodInfo, 1);
		cfg->dynamic_info->code_mp = mono_code_manager_new_dynamic ();
		mono_domain_lock (cfg->domain);
		mono_dynamic_code_hash_insert (cfg->domain, cfg->method, cfg->dynamic_info);
		mono_domain_unlock (cfg->domain);

		code = mono_code_manager_reserve (cfg->dynamic_info->code_mp, cfg->code_size);
	} else {
		mono_domain_lock (cfg->domain);
		code = mono_code_manager_reserve (cfg->domain->code_mp, cfg->code_size);
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
			if (info) {
				//printf ("TEST %s %p\n", info->name, patch_info->data.target);
				if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && 
					strstr (cfg->method->name, info->name))
					/*
					 * This is an icall wrapper, and this is a call to the
					 * wrapped function.
					 */
					;
				else {
					patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
					patch_info->data.name = info->name;
				}
			}
			else {
				MonoVTable *vtable = mono_find_class_init_trampoline_by_addr (patch_info->data.target);
				if (vtable) {
					patch_info->type = MONO_PATCH_INFO_CLASS_INIT;
					patch_info->data.klass = vtable->klass;
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

			if (!cfg->compile_aot)
				/* In the aot case, the patch already points to the correct location */
				patch_info->ip.i = patch_info->ip.label->inst_c0;
			for (i = 0; i < patch_info->data.table->table_size; i++) {
				table [i] = GINT_TO_POINTER (patch_info->data.table->table [i]->native_offset);
			}
			patch_info->data.table->table = (MonoBasicBlock**)table;
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}
       
	if (cfg->verbose_level > 0) {
		char* nm = mono_method_full_name (cfg->method, TRUE);
		g_print ("Method %s emitted at %p to %p [%s]\n", 
				 nm, 
				 cfg->native_code, cfg->native_code + cfg->code_len, cfg->domain->friendly_name);
		g_free (nm);
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
}

static void
mono_cprop_copy_values (MonoCompile *cfg, MonoInst *tree, MonoInst **acp)
{
	MonoInst *cp;
	int arity;

	if (tree->ssa_op == MONO_SSA_LOAD && (tree->inst_i0->opcode == OP_LOCAL || tree->inst_i0->opcode == OP_ARG) && 
	    (cp = acp [tree->inst_i0->inst_c0]) && !tree->inst_i0->flags) {

		if (cp->opcode == OP_ICONST) {
			if (cfg->opt & MONO_OPT_CONSPROP) {
				//{ static int c = 0; printf ("CCOPY %d %d %s\n", c++, cp->inst_c0, mono_method_full_name (cfg->method, TRUE)); }
				*tree = *cp;
			}
		} else {
			if (tree->inst_i0->inst_vtype->type == cp->inst_vtype->type) {
				if (cfg->opt & MONO_OPT_COPYPROP) {
					//{ static int c = 0; printf ("VCOPY %d\n", ++c); }
					tree->inst_i0 = cp;
				} 
			}
		} 
	} else {
		arity = mono_burg_arity [tree->opcode];

		if (arity) {
			mono_cprop_copy_values (cfg, tree->inst_i0, acp);
			if (cfg->opt & MONO_OPT_CFOLD)
				mono_constant_fold_inst (tree, NULL); 
			/* The opcode may have changed */
			if (mono_burg_arity [tree->opcode] > 1) {
				mono_cprop_copy_values (cfg, tree->inst_i1, acp);
				if (cfg->opt & MONO_OPT_CFOLD)
					mono_constant_fold_inst (tree, NULL); 
			}
			mono_constant_fold_inst (tree, NULL); 
		}
	}
}

static void
mono_cprop_invalidate_values (MonoInst *tree, MonoInst **acp, int acp_size)
{
	int arity;

	switch (tree->opcode) {
	case CEE_STIND_I:
	case CEE_STIND_I1:
	case CEE_STIND_I2:
	case CEE_STIND_I4:
	case CEE_STIND_REF:
	case CEE_STIND_I8:
	case CEE_STIND_R4:
	case CEE_STIND_R8:
	case CEE_STOBJ:
		if (tree->ssa_op == MONO_SSA_NOP) {
			memset (acp, 0, sizeof (MonoInst *) * acp_size);
			return;
		}

		break;
	case CEE_CALL:
	case OP_CALL_REG:
	case CEE_CALLVIRT:
	case OP_LCALL_REG:
	case OP_LCALLVIRT:
	case OP_LCALL:
	case OP_FCALL_REG:
	case OP_FCALLVIRT:
	case OP_FCALL:
	case OP_VCALL_REG:
	case OP_VCALLVIRT:
	case OP_VCALL:
	case OP_VOIDCALL_REG:
	case OP_VOIDCALLVIRT:
	case OP_VOIDCALL: {
		MonoCallInst *call = (MonoCallInst *)tree;
		MonoMethodSignature *sig = call->signature;
		int i, byref = FALSE;

		for (i = 0; i < sig->param_count; i++) {
			if (sig->params [i]->byref) {
				byref = TRUE;
				break;
			}
		}

		if (byref)
			memset (acp, 0, sizeof (MonoInst *) * acp_size);

		return;
	}
	default:
		break;
	}

	arity = mono_burg_arity [tree->opcode];

	switch (arity) {
	case 0:
		break;
	case 1:
		mono_cprop_invalidate_values (tree->inst_i0, acp, acp_size);
		break;
	case 2:
		mono_cprop_invalidate_values (tree->inst_i0, acp, acp_size);
		mono_cprop_invalidate_values (tree->inst_i1, acp, acp_size);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
mono_local_cprop_bb (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **acp, int acp_size)
{
	MonoInst *tree = bb->code;	
	int i;

	if (!tree)
		return;

	for (; tree; tree = tree->next) {

		mono_cprop_copy_values (cfg, tree, acp);

		mono_cprop_invalidate_values (tree, acp, acp_size);

		if (tree->ssa_op == MONO_SSA_STORE  && 
		    (tree->inst_i0->opcode == OP_LOCAL || tree->inst_i0->opcode == OP_ARG)) {
			MonoInst *i1 = tree->inst_i1;

			acp [tree->inst_i0->inst_c0] = NULL;

			for (i = 0; i < acp_size; i++) {
				if (acp [i] && acp [i]->opcode != OP_ICONST && 
				    acp [i]->inst_c0 == tree->inst_i0->inst_c0) {
					acp [i] = NULL;
				}
			}

			if (i1->opcode == OP_ICONST) {
				acp [tree->inst_i0->inst_c0] = i1;
				//printf ("DEF1 BB%d %d\n", bb->block_num,tree->inst_i0->inst_c0);
			}
			if (i1->ssa_op == MONO_SSA_LOAD && 
			    (i1->inst_i0->opcode == OP_LOCAL || i1->inst_i0->opcode == OP_ARG) &&
			    (i1->inst_i0->inst_c0 != tree->inst_i0->inst_c0)) {
				acp [tree->inst_i0->inst_c0] = i1->inst_i0;
				//printf ("DEF2 BB%d %d %d\n", bb->block_num,tree->inst_i0->inst_c0,i1->inst_i0->inst_c0);
			}
		}

		/*
		  if (tree->opcode == CEE_BEQ) {
		  g_assert (tree->inst_i0->opcode == OP_COMPARE);
		  if (tree->inst_i0->inst_i0->opcode == OP_ICONST &&
		  tree->inst_i0->inst_i1->opcode == OP_ICONST) {
		  
		  tree->opcode = CEE_BR;
		  if (tree->inst_i0->inst_i0->opcode == tree->inst_i0->inst_i1->opcode) {
		  tree->inst_target_bb = tree->inst_true_bb;
		  } else {
		  tree->inst_target_bb = tree->inst_false_bb;
		  }
		  }
		  }
		*/
	}
}

static void
mono_local_cprop (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	MonoInst **acp;

	acp = alloca (sizeof (MonoInst *) * cfg->num_varinfo);

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		memset (acp, 0, sizeof (MonoInst *) * cfg->num_varinfo);
		mono_local_cprop_bb (cfg, bb, acp, cfg->num_varinfo);
	}
}

static void
remove_critical_edges (MonoCompile *cfg) {
	MonoBasicBlock *bb;
	MonoBasicBlock *previous_bb;
	
	if (cfg->verbose_level > 3) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			int i;
			printf ("remove_critical_edges %s, BEFORE BB%d (in:", mono_method_full_name (cfg->method, TRUE), bb->block_num);
			for (i = 0; i < bb->in_count; i++) {
				printf (" %d", bb->in_bb [i]->block_num);
			}
			printf (") (out:");
			for (i = 0; i < bb->out_count; i++) {
				printf (" %d", bb->out_bb [i]->block_num);
			}
			printf (")");
			if (bb->last_ins != NULL) {
				printf (" ");
				mono_print_tree (bb->last_ins);
			}
			printf ("\n");
		}
	}
	
	for (previous_bb = cfg->bb_entry, bb = previous_bb->next_bb; bb != NULL; previous_bb = previous_bb->next_bb, bb = bb->next_bb) {
		if (bb->in_count > 1) {
			int in_bb_index;
			for (in_bb_index = 0; in_bb_index < bb->in_count; in_bb_index++) {
				MonoBasicBlock *in_bb = bb->in_bb [in_bb_index];
				if (in_bb->out_count > 1) {
					MonoBasicBlock *new_bb = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
					new_bb->block_num = cfg->num_bblocks++;
//					new_bb->real_offset = bb->real_offset;
					new_bb->region = bb->region;
					
					/* Do not alter the CFG while altering the BB list */
					if (previous_bb->region == bb->region) {
						if (previous_bb != cfg->bb_entry) {
							/* If previous_bb "followed through" to bb, */
							/* keep it linked with a CEE_BR */
							if ((previous_bb->last_ins == NULL) ||
									((previous_bb->last_ins->opcode != CEE_BR) &&
									(! (MONO_IS_COND_BRANCH_OP (previous_bb->last_ins))) &&
									(previous_bb->last_ins->opcode != CEE_SWITCH))) {
								int i;
								/* Make sure previous_bb really falls through bb */
								for (i = 0; i < previous_bb->out_count; i++) {
									if (previous_bb->out_bb [i] == bb) {
										MonoInst *jump;
										MONO_INST_NEW (cfg, jump, CEE_BR);
										MONO_ADD_INS (previous_bb, jump);
										jump->cil_code = previous_bb->cil_code;
										jump->inst_target_bb = bb;
										break;
									}
								}
							}
						} else {
							/* We cannot add any inst to the entry BB, so we must */
							/* put a new BB in the middle to hold the CEE_BR */
							MonoInst *jump;
							MonoBasicBlock *new_bb_after_entry = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoBasicBlock));
							new_bb_after_entry->block_num = cfg->num_bblocks++;
//							new_bb_after_entry->real_offset = bb->real_offset;
							new_bb_after_entry->region = bb->region;
							
							MONO_INST_NEW (cfg, jump, CEE_BR);
							MONO_ADD_INS (new_bb_after_entry, jump);
							jump->cil_code = bb->cil_code;
							jump->inst_target_bb = bb;
							
							previous_bb->next_bb = new_bb_after_entry;
							previous_bb = new_bb_after_entry;
							
							if (cfg->verbose_level > 2) {
								printf ("remove_critical_edges %s, added helper BB%d jumping to BB%d\n", mono_method_full_name (cfg->method, TRUE), new_bb_after_entry->block_num, bb->block_num);
							}
						}
					}
					
					/* Insert new_bb in the BB list */
					previous_bb->next_bb = new_bb;
					new_bb->next_bb = bb;
					previous_bb = new_bb;
					
					/* Setup in_bb and out_bb */
					new_bb->in_bb = mono_mempool_alloc ((cfg)->mempool, sizeof (MonoBasicBlock*));
					new_bb->in_bb [0] = in_bb;
					new_bb->in_count = 1;
					new_bb->out_bb = mono_mempool_alloc ((cfg)->mempool, sizeof (MonoBasicBlock*));
					new_bb->out_bb [0] = bb;
					new_bb->out_count = 1;
					
					/* Relink in_bb and bb to (from) new_bb */
					replace_out_block (in_bb, bb, new_bb);
					replace_out_block_in_code (in_bb, bb, new_bb);
					replace_in_block (bb, in_bb, new_bb);
					
					if (cfg->verbose_level > 2) {
						printf ("remove_critical_edges %s, removed critical edge from BB%d to BB%d (added BB%d)\n", mono_method_full_name (cfg->method, TRUE), in_bb->block_num, bb->block_num, new_bb->block_num);
					}
				}
			}
		}
	}
	
	if (cfg->verbose_level > 3) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			int i;
			printf ("remove_critical_edges %s, AFTER BB%d (in:", mono_method_full_name (cfg->method, TRUE), bb->block_num);
			for (i = 0; i < bb->in_count; i++) {
				printf (" %d", bb->in_bb [i]->block_num);
			}
			printf (") (out:");
			for (i = 0; i < bb->out_count; i++) {
				printf (" %d", bb->out_bb [i]->block_num);
			}
			printf (")");
			if (bb->last_ins != NULL) {
				printf (" ");
				mono_print_tree (bb->last_ins);
			}
			printf ("\n");
		}
	}
}

MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, gboolean run_cctors, gboolean compile_aot, int parts)
{
	MonoMethodHeader *header = mono_method_get_header (method);
	guint8 *ip = (guint8 *)header->code;
	MonoCompile *cfg;
	MonoJitInfo *jinfo;
	int dfn = 0, i, code_size_ratio;

	mono_jit_stats.methods_compiled++;
	if (mono_profiler_get_events () & MONO_PROFILE_JIT_COMPILATION)
		mono_profiler_method_jit (method);

	cfg = g_new0 (MonoCompile, 1);
	cfg->method = method;
	cfg->mempool = mono_mempool_new ();
	cfg->opt = opts;
	cfg->prof_options = mono_profiler_get_events ();
	cfg->run_cctors = run_cctors;
	cfg->bb_hash = g_hash_table_new (NULL, NULL);
	cfg->domain = domain;
	cfg->verbose_level = mini_verbose;
	cfg->compile_aot = compile_aot;
	cfg->intvars = mono_mempool_alloc0 (cfg->mempool, sizeof (guint16) * STACK_MAX * 
					    mono_method_get_header (method)->max_stack);

	if (cfg->verbose_level > 2)
		g_print ("converting method %s\n", mono_method_full_name (method, TRUE));

	/*
	 * create MonoInst* which represents arguments and local variables
	 */
	mono_compile_create_vars (cfg);

	if ((i = mono_method_to_ir (cfg, method, NULL, NULL, cfg->locals_start, NULL, NULL, NULL, 0, FALSE)) < 0) {
		if (cfg->prof_options & MONO_PROFILE_JIT_COMPILATION)
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
		mono_destroy_compile (cfg);
		return NULL;
	}

	mono_jit_stats.basic_blocks += cfg->num_bblocks;
	mono_jit_stats.max_basic_blocks = MAX (cfg->num_bblocks, mono_jit_stats.max_basic_blocks);

	if ((cfg->num_varinfo > 2000) && !mono_compile_aot) {
		/* 
		 * we disable some optimizations if there are too many variables
		 * because JIT time may become too expensive. The actual number needs 
		 * to be tweaked and eventually the non-linear algorithms should be fixed.
		 */
		cfg->opt &= ~ (MONO_OPT_LINEARS | MONO_OPT_COPYPROP | MONO_OPT_CONSPROP);
		cfg->disable_ssa = TRUE;
	}

	/*g_print ("numblocks = %d\n", cfg->num_bblocks);*/

	if (cfg->opt & MONO_OPT_BRANCH)
		optimize_branches (cfg);

	if (cfg->opt & MONO_OPT_SSAPRE) {
		remove_critical_edges (cfg);
	}

	/* Depth-first ordering on basic blocks */
	cfg->bblocks = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (cfg->num_bblocks + 1));

	df_visit (cfg->bb_entry, &dfn, cfg->bblocks);
	if (cfg->num_bblocks != dfn + 1) {
		MonoBasicBlock *bb;

		cfg->num_bblocks = dfn + 1;

		if (!header->clauses) {
			/* remove unreachable code, because the code in them may be 
			 * inconsistent  (access to dead variables for example) */
			for (bb = cfg->bb_entry; bb;) {
				MonoBasicBlock *bbn = bb->next_bb;

				if (bbn && bbn->region == -1 && !bbn->dfn) {
					if (cfg->verbose_level > 1)
						g_print ("found unreachabel code in BB%d\n", bbn->block_num);
					bb->next_bb = bbn->next_bb;
					nullify_basic_block (bbn);			
				} else {
					bb = bb->next_bb;
				}
			}
		}
	}

	if (cfg->opt & MONO_OPT_LOOP) {
		mono_compile_dominator_info (cfg, MONO_COMP_DOM | MONO_COMP_IDOM);
		mono_compute_natural_loops (cfg);
	}

	/* after method_to_ir */
	if (parts == 1)
		return cfg;

//#define DEBUGSSA "logic_run"
#define DEBUGSSA_CLASS "Tests"
#ifdef DEBUGSSA

	if (!header->num_clauses && !cfg->disable_ssa) {
		mono_local_cprop (cfg);
		mono_ssa_compute (cfg);
	}
#else 

	/* fixme: add all optimizations which requires SSA */
	if (cfg->opt & (MONO_OPT_DEADCE | MONO_OPT_ABCREM | MONO_OPT_SSAPRE)) {
		if (!(cfg->comp_done & MONO_COMP_SSA) && !header->num_clauses && !cfg->disable_ssa) {
			mono_local_cprop (cfg);
			mono_ssa_compute (cfg);

			if (cfg->verbose_level >= 2) {
				print_dfn (cfg);
			}
		}
	}
#endif

	/* after SSA translation */
	if (parts == 2)
		return cfg;

	if ((cfg->opt & MONO_OPT_CONSPROP) || (cfg->opt & MONO_OPT_COPYPROP)) {
		if (cfg->comp_done & MONO_COMP_SSA) {
			mono_ssa_cprop (cfg);
		} else {
			mono_local_cprop (cfg);
		}
	}

	if (cfg->comp_done & MONO_COMP_SSA) {			
		mono_ssa_deadce (cfg);

		//mono_ssa_strength_reduction (cfg);

		if ((cfg->flags & MONO_CFG_HAS_LDELEMA) && (cfg->opt & MONO_OPT_ABCREM))
			mono_perform_abc_removal (cfg);
		
		if (cfg->opt & MONO_OPT_SSAPRE)
			mono_perform_ssapre (cfg);
		
		mono_ssa_remove (cfg);

		if (cfg->opt & MONO_OPT_BRANCH)
			optimize_branches (cfg);
	}

	/* after SSA removal */
	if (parts == 3)
		return cfg;

	decompose_pass (cfg);

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

	if (cfg->opt & MONO_OPT_LINEARS) {
		GList *vars, *regs;

		/* fixme: maybe we can avoid to compute livenesss here if already computed ? */
		cfg->comp_done &= ~MONO_COMP_LIVENESS;
		if (!(cfg->comp_done & MONO_COMP_LIVENESS))
			mono_analyze_liveness (cfg);

		if ((vars = mono_arch_get_allocatable_int_vars (cfg))) {
			regs = mono_arch_get_global_int_regs (cfg);
			if (cfg->got_var)
				regs = g_list_delete_link (regs, regs);
			mono_linear_scan (cfg, vars, regs, &cfg->used_int_regs);
		}
	}

	//mono_print_code (cfg);

    //print_dfn (cfg);
	
	/* variables are allocated after decompose, since decompose could create temps */
	mono_arch_allocate_vars (cfg);

	if (cfg->opt & MONO_OPT_CFOLD)
		mono_constant_fold (cfg);

	mini_select_instructions (cfg);

	mono_codegen (cfg);
	if (cfg->verbose_level >= 2) {
		char *id =  mono_method_full_name (cfg->method, FALSE);
		mono_disassemble_code (cfg, cfg->native_code, cfg->code_len, id + 3);
		g_free (id);
	}
	
	if (cfg->method->dynamic) {
		jinfo = g_malloc0 (sizeof (MonoJitInfo) + (header->num_clauses * sizeof (MonoJitExceptionInfo)));
	} else {
		/* we access cfg->domain->mp */
		mono_domain_lock (cfg->domain);
		jinfo = mono_mempool_alloc0 (cfg->domain->mp, sizeof (MonoJitInfo) + (header->num_clauses * sizeof (MonoJitExceptionInfo)));
		mono_domain_unlock (cfg->domain);
	}

	jinfo->method = method;
	jinfo->code_start = cfg->native_code;
	jinfo->code_size = cfg->code_len;
	jinfo->used_regs = cfg->used_int_regs;
	jinfo->domain_neutral = (cfg->opt & MONO_OPT_SHARED) != 0;
	jinfo->cas_inited = FALSE; /* initialization delayed at the first stalk walk using this method */

	if (header->num_clauses) {
		int i;

		jinfo->num_clauses = header->num_clauses;

		for (i = 0; i < header->num_clauses; i++) {
			MonoExceptionClause *ec = &header->clauses [i];
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			MonoBasicBlock *tblock;
			MonoInst *exvar;

			ei->flags = ec->flags;

			exvar = mono_find_exvar_for_offset (cfg, ec->handler_offset);
			ei->exvar_offset = exvar ? exvar->inst_offset : 0;

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->data.filter_offset);
				g_assert (tblock);
				ei->data.filter = cfg->native_code + tblock->native_offset;
			} else {
				ei->data.catch_class = ec->data.catch_class;
			}

			tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->try_offset);
			g_assert (tblock);
			ei->try_start = cfg->native_code + tblock->native_offset;
			g_assert (tblock->native_offset);
			tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->try_offset + ec->try_len);
			g_assert (tblock);
			ei->try_end = cfg->native_code + tblock->native_offset;
			g_assert (tblock->native_offset);
			tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->handler_offset);
			g_assert (tblock);
			ei->handler_start = cfg->native_code + tblock->native_offset;
		}
	}

	cfg->jit_info = jinfo;

	mono_domain_lock (cfg->domain);
	mono_jit_info_table_add (cfg->domain, jinfo);

	if (cfg->method->dynamic)
		mono_dynamic_code_hash_lookup (cfg->domain, cfg->method)->ji = jinfo;
	mono_domain_unlock (cfg->domain);

	/* collect statistics */
	mono_jit_stats.allocated_code_size += cfg->code_len;
	code_size_ratio = cfg->code_len;
	if (code_size_ratio > mono_jit_stats.biggest_method_size) {
			mono_jit_stats.biggest_method_size = code_size_ratio;
			mono_jit_stats.biggest_method = method;
	}
	code_size_ratio = (code_size_ratio * 100) / mono_method_get_header (method)->code_size;
	if (code_size_ratio > mono_jit_stats.max_code_size_ratio) {
		mono_jit_stats.max_code_size_ratio = code_size_ratio;
		mono_jit_stats.max_ratio_method = method;
	}
	mono_jit_stats.native_code_size += cfg->code_len;

	if (cfg->prof_options & MONO_PROFILE_JIT_COMPILATION)
		mono_profiler_method_end_jit (method, MONO_PROFILE_OK);

	/* this can only be set if the security manager is active */
	if (cfg->exception_type == MONO_EXCEPTION_SECURITY_LINKDEMAND) {
		MonoAssembly *assembly = mono_image_get_assembly (method->klass->image);
		MonoReflectionAssembly *refass = (MonoReflectionAssembly*) mono_assembly_get_object (domain, assembly);
		MonoReflectionMethod *refmet = mono_method_get_object (domain, method, NULL);
		MonoSecurityManager* secman = mono_security_manager_get_methods ();
		MonoObject *exc = NULL;
		gpointer args [3];

		args [0] = &cfg->exception_data;
		args [1] = refass;
		args [2] = refmet;
		mono_runtime_invoke (secman->linkdemandsecurityexception, NULL, args, &exc);

		mono_destroy_compile (cfg);
		cfg = NULL;

		mono_raise_exception ((MonoException*)exc);
	}

	return cfg;
}

static gpointer
mono_jit_compile_method_inner (MonoMethod *method, MonoDomain *target_domain)
{
	MonoCompile *cfg;
	GHashTable *jit_code_hash;
	gpointer code = NULL;
	guint32 opt;
	MonoJitInfo *info;

	opt = default_opt;

	jit_code_hash = target_domain->jit_code_hash;

	method = mono_get_inflated_method (method);

#ifdef MONO_USE_AOT_COMPILER
	if (!mono_compile_aot && (opt & MONO_OPT_AOT)) {
		MonoJitInfo *info;
		MonoDomain *domain = mono_domain_get ();

		mono_domain_lock (domain);

		mono_class_init (method->klass);
		if ((info = mono_aot_get_method (domain, method))) {
			g_hash_table_insert (domain->jit_code_hash, method, info);
			mono_domain_unlock (domain);
			mono_runtime_class_init (mono_class_vtable (domain, method->klass));
			return info->code_start;
		}

		mono_domain_unlock (domain);
	}
#endif

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
		MonoMethod *nm;
		MonoMethodPInvoke* piinfo = (MonoMethodPInvoke *) method;

		if (method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)
			g_error ("Method '%s' in assembly '%s' contains native code and mono can't run it. The assembly was probably created by Managed C++.\n", mono_method_full_name (method, TRUE), method->klass->image->name);

		if (!piinfo->addr) {
			if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
				piinfo->addr = mono_lookup_internal_call (method);
			else
				if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)
					mono_lookup_pinvoke_call (method, NULL, NULL);
		}
			nm = mono_marshal_get_native_wrapper (method);
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
				return mono_get_addr_from_ftnptr ((gpointer)mono_icall_get_wrapper (mi));
			} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
			        nm = mono_marshal_get_delegate_invoke (method);
					return mono_get_addr_from_ftnptr (mono_compile_method (nm));
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

	cfg = mini_method_compile (method, opt, target_domain, TRUE, FALSE, 0);

	mono_domain_lock (target_domain);

	/* Check if some other thread already did the job. In this case, we can
       discard the code this thread generated. */

	if ((info = g_hash_table_lookup (target_domain->jit_code_hash, method))) {
		/* We can't use a domain specific method in another domain */
		if ((target_domain == mono_domain_get ()) || info->domain_neutral) {
			code = info->code_start;
//			printf("Discarding code for method %s\n", method->name);
		}
	}
	
	if (code == NULL) {
		g_hash_table_insert (jit_code_hash, method, cfg->jit_info);
		code = cfg->native_code;
	}

	mono_destroy_compile (cfg);

	if (target_domain->jump_target_hash) {
		MonoJumpInfo patch_info;
		GSList *list, *tmp;
		list = g_hash_table_lookup (target_domain->jump_target_hash, method);
		if (list) {
			patch_info.next = NULL;
			patch_info.ip.i = 0;
			patch_info.type = MONO_PATCH_INFO_METHOD_JUMP;
			patch_info.data.method = method;
			g_hash_table_remove (target_domain->jump_target_hash, method);
		}
		for (tmp = list; tmp; tmp = tmp->next)
			mono_arch_patch_code (NULL, target_domain, tmp->data, &patch_info, TRUE);
		g_slist_free (list);
	}

	mono_domain_unlock (target_domain);

	mono_runtime_class_init (mono_class_vtable (target_domain, method->klass));
	return code;
}

static gpointer
mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt)
{
	/* FIXME: later copy the code from mono */
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoJitInfo *info;
	gpointer p;

	if (opt & MONO_OPT_SHARED)
		target_domain = mono_get_root_domain ();
	else 
		target_domain = domain;

	mono_domain_lock (target_domain);

	if ((info = g_hash_table_lookup (target_domain->jit_code_hash, method))) {
		/* We can't use a domain specific method in another domain */
		if (! ((domain != target_domain) && !info->domain_neutral)) {
			mono_domain_unlock (target_domain);
			mono_jit_stats.methods_lookups++;
			mono_runtime_class_init (mono_class_vtable (domain, method->klass));
			return mono_create_ftnptr (domain, info->code_start);
		}
	}

	mono_domain_unlock (target_domain);
	p = mono_jit_compile_method_inner (method, target_domain);
	return mono_create_ftnptr (domain, p);
}

static gpointer
mono_jit_compile_method (MonoMethod *method)
{
	return mono_jit_compile_method_with_opt (method, default_opt);
}

static void
invalidated_delegate_trampoline (char *desc)
{
	g_error ("Unmanaged code called delegate of type %s which was already garbage collected.\n"
		 "See http://www.go-mono.com/delegate.html for an explanation and ways to fix this.",
		 desc);
}

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
	g_hash_table_remove (domain->dynamic_code_hash, method);
	g_hash_table_remove (domain->jit_code_hash, method);
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
	 * key in the table, so if we the code_mp first, another thread can grab the
	 * same code address and replace our entry in the table.
	 */
	mono_jit_info_table_remove (domain, ji->ji);

	if (destroy)
		mono_code_manager_destroy (ji->code_mp);
	g_free (ji->ji);
	g_free (ji);
}

static gpointer
mono_jit_find_compiled_method (MonoDomain *domain, MonoMethod *method)
{
	MonoDomain *target_domain;
	MonoJitInfo *info;

	if (default_opt & MONO_OPT_SHARED)
		target_domain = mono_get_root_domain ();
	else 
		target_domain = domain;

	mono_domain_lock (target_domain);

	if ((info = g_hash_table_lookup (target_domain->jit_code_hash, method))) {
		/* We can't use a domain specific method in another domain */
		if (! ((domain != target_domain) && !info->domain_neutral)) {
			mono_domain_unlock (target_domain);
			mono_jit_stats.methods_lookups++;
			return info->code_start;
		}
	}

	mono_domain_unlock (target_domain);

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
	MonoMethod *invoke;
	MonoObject *(*runtime_invoke) (MonoObject *this, void **params, MonoObject **exc, void* compiled_method);
	void* compiled_method;

	if (obj == NULL && !(method->flags & METHOD_ATTRIBUTE_STATIC) && !method->string_ctor && (method->wrapper_type == 0)) {
		g_warning ("Ignoring invocation of an instance method on a NULL instance.\n");
		return NULL;
	}

	method = mono_get_inflated_method (method);
	invoke = mono_marshal_get_runtime_invoke (method);
	runtime_invoke = mono_jit_compile_method (invoke);
	
	/* We need this here becuase mono_marshal_get_runtime_invoke can be place 
	 * the helper method in System.Object and not the target class
	 */
	mono_runtime_class_init (mono_class_vtable (mono_domain_get (), method->klass));

	compiled_method = mono_jit_compile_method (method);
	return runtime_invoke (obj, params, exc, compiled_method);
}

#ifdef PLATFORM_WIN32
#define GET_CONTEXT \
	struct sigcontext *ctx = (struct sigcontext*)_dummy;
#else
#ifdef __sparc
#define GET_CONTEXT \
    void *ctx = context;
#elif defined(sun)    // Solaris x86
#define GET_CONTEXT \
    ucontext_t *uctx = context; \
    struct sigcontext *ctx = (struct sigcontext *)&(uctx->uc_mcontext);
#elif defined(__ppc__) || defined (__powerpc__) || defined (__s390__) || defined (MONO_ARCH_USE_SIGACTION)
#define GET_CONTEXT \
    void *ctx = context;
#else
#define GET_CONTEXT \
	void **_p = (void **)&_dummy; \
	struct sigcontext *ctx = (struct sigcontext *)++_p;
#endif
#endif

#ifdef MONO_ARCH_USE_SIGACTION
#define SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, siginfo_t *info, void *context)
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
	GET_CONTEXT
	exc = mono_get_exception_execution_engine ("SIGILL");
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

#ifndef MONO_ARCH_USE_SIGACTION
#error "Can't use sigaltstack without sigaction"
#endif

#endif

static void
SIG_HANDLER_SIGNATURE (sigsegv_signal_handler)
{
	MonoException *exc = NULL;
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
#endif
	GET_CONTEXT

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	/* Can't allocate memory using Boehm GC on altstack */
	if (jit_tls->stack_size && 
		((guint8*)info->si_addr >= (guint8*)jit_tls->end_of_stack - jit_tls->stack_size) &&
		((guint8*)info->si_addr < (guint8*)jit_tls->end_of_stack))
		exc = mono_domain_get ()->stack_overflow_ex;
	else
		exc = mono_domain_get ()->null_reference_ex;
#endif

	if (debug_options.abort_on_sigsegv) {
		MonoJitInfo *ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context(ctx));
		if (!ji) {
			fprintf (stderr, "Got SIGSEGV while in unmanaged code, and the 'abort-on-sigsegv' MONO_DEBUG option is set. Aborting...\n");
			/* Segfault in unmanaged code */
			abort ();
		}
	}
			
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
SIG_HANDLER_SIGNATURE (sigusr1_signal_handler)
{
	gboolean running_managed;
	MonoException *exc;
	void *ji;
	
	GET_CONTEXT;

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

static void
SIG_HANDLER_SIGNATURE (sigprof_signal_handler)
{
	GET_CONTEXT;

	mono_profiler_stat_hit (mono_arch_ip_from_context (ctx), ctx);
}

static void
SIG_HANDLER_SIGNATURE (sigquit_signal_handler)
{
       MonoException *exc;
       GET_CONTEXT

       exc = mono_get_exception_execution_engine ("Interrupted (SIGQUIT).");
       
       mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
SIG_HANDLER_SIGNATURE (sigint_signal_handler)
{
	MonoException *exc;
	GET_CONTEXT

	exc = mono_get_exception_execution_engine ("Interrupted (SIGINT).");
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
SIG_HANDLER_SIGNATURE (sigusr2_signal_handler)
{
	gboolean enabled = mono_trace_is_enabled ();

	mono_trace_enable (!enabled);
}

#ifndef PLATFORM_WIN32
static void
add_signal_handler (int signo, gpointer handler)
{
	struct sigaction sa;

#ifdef MONO_ARCH_USE_SIGACTION
	sa.sa_sigaction = handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO;
#else
	sa.sa_handler = handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
#endif
	g_assert (sigaction (signo, &sa, NULL) != -1);
}
#endif

static void
mono_runtime_install_handlers (void)
{
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	struct sigaction sa;
#endif

#ifdef PLATFORM_WIN32
	win32_seh_init();
	win32_seh_set_handler(SIGFPE, sigfpe_signal_handler);
	win32_seh_set_handler(SIGILL, sigill_signal_handler);
	win32_seh_set_handler(SIGSEGV, sigsegv_signal_handler);
	if (debug_options.handle_sigint)
		win32_seh_set_handler(SIGINT, sigint_signal_handler);

#else /* !PLATFORM_WIN32 */

	/* libpthreads has its own implementation of sigaction(),
	 * but it seems to work well with our current exception
	 * handlers. If not we must call syscall directly instead 
	 * of sigaction */

	if (debug_options.handle_sigint)
		add_signal_handler (SIGINT, sigint_signal_handler);

	add_signal_handler (SIGFPE, sigfpe_signal_handler);
	add_signal_handler (SIGQUIT, sigquit_signal_handler);
	add_signal_handler (SIGILL, sigill_signal_handler);
	add_signal_handler (SIGBUS, sigsegv_signal_handler);
	if (mono_jit_trace_calls != NULL)
		add_signal_handler (SIGUSR2, sigusr2_signal_handler);

	add_signal_handler (mono_thread_get_abort_signal (), sigusr1_signal_handler);

	/* catch SIGSEGV */
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	sa.sa_sigaction = sigsegv_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = SA_SIGINFO | SA_ONSTACK;
	g_assert (sigaction (SIGSEGV, &sa, NULL) != -1);
#else
	add_signal_handler (SIGSEGV, sigsegv_signal_handler);
#endif

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
mono_jit_create_remoting_trampoline (MonoMethod *method, MonoRemotingTarget target)
{
	MonoMethod *nm;
	guint8 *addr = NULL;

	if ((method->flags & METHOD_ATTRIBUTE_ABSTRACT) || 
	    (mono_method_signature (method)->hasthis && (method->klass->marshalbyref || method->klass == mono_defaults.object_class))) {
		nm = mono_marshal_get_remoting_invoke_for_target (method, target);
		addr = mono_compile_method (nm);
	} else {
		addr = mono_compile_method (method);
	}
	return mono_get_addr_from_ftnptr (addr);
}

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
		else if (!strcmp (arg, "abort-on-sigsegv"))
			debug_options.abort_on_sigsegv = TRUE;
		else {
			fprintf (stderr, "Invalid option for the MONO_DEBUG env variable: %s\n", arg);
			fprintf (stderr, "Available options: 'handle-sigint', 'keep-delegates', 'abort-on-sigsegv'\n");
			exit (1);
		}
	}
}

MonoDomain *
mini_init (const char *filename)
{
	MonoDomain *domain;

	/* Happens when using the embedding interface */
	if (default_opt == 0)
		default_opt = mono_parse_default_optimizations (NULL);

	InitializeCriticalSection (&jit_mutex);

	global_codeman = mono_code_manager_new ();
	jit_icall_name_hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);

	mono_arch_cpu_init ();

	mono_init_trampolines ();

	if (!g_thread_supported ())
		g_thread_init (NULL);

	if (getenv ("MONO_DEBUG") != NULL)
		mini_parse_debug_options ();

	if (mono_running_on_valgrind ()) {
		gsize stack_bottom = (gsize)&domain;
		stack_bottom += 4095;
		stack_bottom &= ~4095;
		GC_stackbottom = (char*)stack_bottom;
	}
	MONO_GC_PRE_INIT ();

	mono_jit_tls_id = TlsAlloc ();
	setup_jit_tls_data ((gpointer)-1, mono_thread_abort);

	mono_burg_init ();

	if (default_opt & MONO_OPT_AOT)
		mono_aot_init ();

	mono_runtime_install_handlers ();
	mono_threads_install_cleanup (mini_thread_cleanup);

#define JIT_TRAMPOLINES_WORK
#ifdef JIT_TRAMPOLINES_WORK
	mono_install_compile_method (mono_jit_compile_method);
	mono_install_free_method (mono_jit_free_method);
	mono_install_trampoline (mono_create_jit_trampoline);
	mono_install_remoting_trampoline (mono_jit_create_remoting_trampoline);
	mono_install_delegate_trampoline (mono_create_delegate_trampoline);
#endif
#define JIT_INVOKE_WORKS
#ifdef JIT_INVOKE_WORKS
	mono_install_runtime_invoke (mono_jit_runtime_invoke);
	mono_install_handler (mono_arch_get_throw_exception ());
#endif
	mono_install_stack_walk (mono_jit_walk_stack);
	mono_install_init_vtable (mono_aot_init_vtable);
	mono_install_get_cached_class_info (mono_aot_get_cached_class_info);
 	mono_install_jit_info_find_in_aot (mono_aot_find_jit_info);

	domain = mono_init_from_assembly (filename, filename);
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

	register_icall (mono_arch_get_throw_exception (), "mono_arch_throw_exception", "void object", TRUE);
	register_icall (mono_arch_get_rethrow_exception (), "mono_arch_rethrow_exception", "void object", TRUE);
	register_icall (mono_arch_get_throw_exception_by_name (), "mono_arch_throw_exception_by_name", "void ptr", TRUE); 
#if MONO_ARCH_HAVE_THROW_CORLIB_EXCEPTION
	register_icall (mono_arch_get_throw_corlib_exception (), "mono_arch_throw_corlib_exception", 
				 "void ptr", TRUE);
#endif
	register_icall (mono_thread_get_pending_exception, "mono_thread_get_pending_exception", "object", FALSE);
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
	mono_register_opcode_emulation (CEE_DIV, "__emul_idiv", "int32 int32 int32", mono_idiv, TRUE);
	mono_register_opcode_emulation (CEE_DIV_UN, "__emul_idiv_un", "int32 int32 int32", mono_idiv_un, TRUE);
	mono_register_opcode_emulation (CEE_REM, "__emul_irem", "int32 int32 int32", mono_irem, TRUE);
	mono_register_opcode_emulation (CEE_REM_UN, "__emul_irem_un", "int32 int32 int32", mono_irem_un, TRUE);
#endif

#ifdef MONO_ARCH_EMULATE_MUL_DIV
	mono_register_opcode_emulation (CEE_MUL_OVF, "__emul_imul_ovf", "int32 int32 int32", mono_imul_ovf, TRUE);
	mono_register_opcode_emulation (CEE_MUL_OVF_UN, "__emul_imul_ovf_un", "int32 int32 int32", mono_imul_ovf_un, TRUE);
	mono_register_opcode_emulation (CEE_MUL, "__emul_imul", "int32 int32 int32", mono_imul, TRUE);
	mono_register_opcode_emulation (OP_FDIV, "__emul_fdiv", "double double double", mono_fdiv, TRUE);
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

#if SIZEOF_VOID_P == 4
	mono_register_opcode_emulation (OP_FCONV_TO_U, "__emul_fconv_to_u", "uint32 double", mono_fconv_u4, TRUE);
#endif

	/* other jit icalls */
	register_icall (mono_delegate_ctor, "mono_delegate_ctor", "void object object ptr", FALSE);
	register_icall (mono_class_static_field_address , "mono_class_static_field_address", 
				 "ptr ptr ptr", FALSE);
	register_icall (mono_ldtoken_wrapper, "mono_ldtoken_wrapper", "ptr ptr ptr ptr", FALSE);
	register_icall (mono_get_special_static_data, "mono_get_special_static_data", "ptr int", FALSE);
	register_icall (mono_ldstr, "mono_ldstr", "object ptr ptr int32", FALSE);
	register_icall (helper_stelem_ref, "helper_stelem_ref", "void ptr int32 object", FALSE);
	register_icall (helper_stelem_ref_check, "helper_stelem_ref_check", "void object object", FALSE);
	register_icall (mono_object_new, "mono_object_new", "object ptr ptr", FALSE);
	register_icall (mono_object_new_specific, "mono_object_new_specific", "object ptr", FALSE);
	register_icall (mono_array_new, "mono_array_new", "object ptr ptr int32", FALSE);
	register_icall (mono_array_new_specific, "mono_array_new_specific", "object ptr int32", FALSE);
	register_icall (mono_runtime_class_init, "mono_runtime_class_init", "void ptr", FALSE);
	register_icall (mono_ldftn, "mono_ldftn", "ptr ptr", FALSE);
	register_icall (mono_ldftn_nosync, "mono_ldftn_nosync", "ptr ptr", FALSE);
	register_icall (mono_ldvirtfn, "mono_ldvirtfn", "ptr object ptr", FALSE);
	register_icall (helper_compile_generic_method, "compile_generic_method", "ptr object ptr ptr", FALSE);
	register_icall (helper_ldstr, "helper_ldstr", "object ptr int", FALSE);
#endif

#define JIT_RUNTIME_WORKS
#ifdef JIT_RUNTIME_WORKS
	mono_install_runtime_cleanup ((MonoDomainFunc)mini_cleanup);
	mono_runtime_init (domain, mono_thread_start_cb, mono_thread_attach_cb);
#endif

	mono_thread_attach (domain);
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
		g_print ("Analyze stack repeat:   %ld\n", mono_jit_stats.analyze_stack_repeat);
		g_print ("Compiled CIL code size: %ld\n", mono_jit_stats.cil_code_size);
		g_print ("Native code size:       %ld\n", mono_jit_stats.native_code_size);
		g_print ("Max code size ratio:    %.2f (%s::%s)\n", mono_jit_stats.max_code_size_ratio/100.0,
				mono_jit_stats.max_ratio_method->klass->name, mono_jit_stats.max_ratio_method->name);
		g_print ("Biggest method:         %ld (%s::%s)\n", mono_jit_stats.biggest_method_size,
				mono_jit_stats.biggest_method->klass->name, mono_jit_stats.biggest_method->name);
		g_print ("Code reallocs:          %ld\n", mono_jit_stats.code_reallocs);
		g_print ("Allocated code size:    %ld\n", mono_jit_stats.allocated_code_size);
		g_print ("Inlineable methods:     %ld\n", mono_jit_stats.inlineable_methods);
		g_print ("Inlined methods:        %ld\n", mono_jit_stats.inlined_methods);
		
		g_print ("\nCreated object count:   %ld\n", mono_stats.new_object_count);
		g_print ("Initialized classes:    %ld\n", mono_stats.initialized_class_count);
		g_print ("Used classes:           %ld\n", mono_stats.used_class_count);
		g_print ("Static data size:       %ld\n", mono_stats.class_static_data_size);
		g_print ("VTable data size:       %ld\n", mono_stats.class_vtable_size);

		g_print ("\nGeneric instances:      %ld\n", mono_stats.generic_instance_count);
		g_print ("Initialized classes:    %ld\n", mono_stats.generic_class_count);
		g_print ("Inflated methods:       %ld / %ld\n", mono_stats.inflated_method_count_2,
			 mono_stats.inflated_method_count);
		g_print ("Inflated types:         %ld\n", mono_stats.inflated_type_count);
		g_print ("Generics metadata size: %ld\n", mono_stats.generics_metadata_size);

		if (mono_use_security_manager) {
			g_print ("\nDecl security check   : %ld\n", mono_jit_stats.cas_declsec_check);
			g_print ("LinkDemand (user)     : %ld\n", mono_jit_stats.cas_linkdemand);
			g_print ("LinkDemand (icall)    : %ld\n", mono_jit_stats.cas_linkdemand_icall);
			g_print ("LinkDemand (pinvoke)  : %ld\n", mono_jit_stats.cas_linkdemand_pinvoke);
			g_print ("LinkDemand (aptc)     : %ld\n", mono_jit_stats.cas_linkdemand_aptc);
			g_print ("Demand (code gen)     : %ld\n", mono_jit_stats.cas_demand_generation);
		}
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

	mono_runtime_cleanup (domain);

	mono_profiler_shutdown ();

	mono_debug_cleanup ();

	mono_icall_cleanup ();

#ifdef PLATFORM_WIN32
	win32_seh_cleanup();
#endif

	mono_domain_free (domain, TRUE);

	mono_code_manager_destroy (global_codeman);
	g_hash_table_destroy (jit_icall_name_hash);
	if (class_init_hash_addr)
		g_hash_table_destroy (class_init_hash_addr);

	print_jit_stats ();
}

void
mono_set_defaults (int verbose_level, guint32 opts)
{
	mini_verbose = verbose_level;
	default_opt = opts;
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
