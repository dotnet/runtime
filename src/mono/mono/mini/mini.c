/*
 * mini.c: The new Mono code generator.
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc.
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */

#define MONO_LLVM_IN_MINI 1
#include <config.h>
#include <signal.h>
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
#include <mono/io-layer/io-layer.h>
#include "mono/metadata/profiler.h"
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/attach.h>
#include <mono/metadata/runtime.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/mono-logger-internal.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-tls.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/dtrace.h>
#include <mono/utils/mono-signal-handler.h>
#include <mono/utils/mono-threads.h>

#include "mini.h"
#include "mini-llvm.h"
#include "tasklets.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"
#include "version.h"
#include "ir-emit.h"

#include "jit-icalls.h"

#include "mini-gc.h"
#include "debugger-agent.h"

static gpointer mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt, MonoException **ex);


static guint32 default_opt = 0;
static gboolean default_opt_set = FALSE;

MonoNativeTlsKey mono_jit_tls_id;

#ifdef MONO_HAVE_FAST_TLS
MONO_FAST_TLS_DECLARE(mono_jit_tls);
#endif

MonoTraceSpec *mono_jit_trace_calls;
gboolean mono_compile_aot = FALSE;
/* If this is set, no code is generated dynamically, everything is taken from AOT files */
gboolean mono_aot_only = FALSE;
/* Whenever to use IMT */
gboolean mono_use_imt = TRUE;
MonoMethodDesc *mono_inject_async_exc_method;
int mono_inject_async_exc_pos;
MonoMethodDesc *mono_break_at_bb_method;
int mono_break_at_bb_bb_num;
gboolean mono_do_x86_stack_align = TRUE;
const char *mono_build_date;
gboolean mono_do_signal_chaining;
gboolean mono_do_crash_chaining;
static gboolean	mono_using_xdebug;
static int mini_verbose = 0;

/*
 * This flag controls whenever the runtime uses LLVM for JIT compilation, and whenever
 * it can load AOT code compiled by LLVM.
 */
gboolean mono_use_llvm = FALSE;

#define mono_jit_lock() mono_mutex_lock (&jit_mutex)
#define mono_jit_unlock() mono_mutex_unlock (&jit_mutex)
static mono_mutex_t jit_mutex;

static MonoCodeManager *global_codeman;

static GHashTable *jit_icall_name_hash;

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

static GSList *tramp_infos;

static void register_icalls (void);

gpointer
mono_realloc_native_code (MonoCompile *cfg)
{
#if defined(__default_codegen__)
	return g_realloc (cfg->native_code, cfg->code_size);
#elif defined(__native_client_codegen__)
	guint old_padding;
	gpointer native_code;
	guint alignment_check;

	/* Save the old alignment offset so we can re-align after the realloc. */
	old_padding = (guint)(cfg->native_code - cfg->native_code_alloc);
	cfg->code_size = NACL_BUNDLE_ALIGN_UP (cfg->code_size);

	cfg->native_code_alloc = g_realloc ( cfg->native_code_alloc,
										 cfg->code_size + kNaClAlignment );

	/* Align native_code to next nearest kNaClAlignment byte. */
	native_code = (guint)cfg->native_code_alloc + kNaClAlignment;
	native_code = (guint)native_code & ~kNaClAlignmentMask;

	/* Shift the data to be 32-byte aligned again. */
	memmove (native_code, cfg->native_code_alloc + old_padding, cfg->code_size);

	alignment_check = (guint)native_code & kNaClAlignmentMask;
	g_assert (alignment_check == 0);
	return native_code;
#else
	g_assert_not_reached ();
	return cfg->native_code;
#endif
}

#ifdef __native_client_codegen__

/* Prevent instructions from straddling a 32-byte alignment boundary.   */
/* Instructions longer than 32 bytes must be aligned internally.        */
/* IN: pcode, instlen                                                   */
/* OUT: pcode                                                           */
void mono_nacl_align_inst(guint8 **pcode, int instlen) {
  int space_in_block;

  space_in_block = kNaClAlignment - ((uintptr_t)(*pcode) & kNaClAlignmentMask);

  if (G_UNLIKELY (instlen >= kNaClAlignment)) {
    g_assert_not_reached();
  } else if (instlen > space_in_block) {
    *pcode = mono_arch_nacl_pad(*pcode, space_in_block);
  }
}

/* Move emitted call sequence to the end of a kNaClAlignment-byte block.  */
/* IN: start    pointer to start of call sequence                         */
/* IN: pcode    pointer to end of call sequence (current "IP")            */
/* OUT: start   pointer to the start of the call sequence after padding   */
/* OUT: pcode   pointer to the end of the call sequence after padding     */
void mono_nacl_align_call(guint8 **start, guint8 **pcode) {
  const size_t MAX_NACL_CALL_LENGTH = kNaClAlignment;
  guint8 copy_of_call[MAX_NACL_CALL_LENGTH];
  guint8 *temp;

  const size_t length = (size_t)((*pcode)-(*start));
  g_assert(length < MAX_NACL_CALL_LENGTH);

  memcpy(copy_of_call, *start, length);
  temp = mono_nacl_pad_call(*start, (guint8)length);
  memcpy(temp, copy_of_call, length);
  (*start) = temp;
  (*pcode) = temp + length;
}

/* mono_nacl_pad_call(): Insert padding for Native Client call instructions */
/*    code     pointer to buffer for emitting code                          */
/*    ilength  length of call instruction                                   */
guint8 *mono_nacl_pad_call(guint8 *code, guint8 ilength) {
  int freeSpaceInBlock = kNaClAlignment - ((uintptr_t)code & kNaClAlignmentMask);
  int padding = freeSpaceInBlock - ilength;

  if (padding < 0) {
    /* There isn't enough space in this block for the instruction. */
    /* Fill this block and start a new one.                        */
    code = mono_arch_nacl_pad(code, freeSpaceInBlock);
    freeSpaceInBlock = kNaClAlignment;
    padding = freeSpaceInBlock - ilength;
  }
  g_assert(ilength > 0);
  g_assert(padding >= 0);
  g_assert(padding < kNaClAlignment);
  if (0 == padding) return code;
  return mono_arch_nacl_pad(code, padding);
}

guint8 *mono_nacl_align(guint8 *code) {
  int padding = kNaClAlignment - ((uintptr_t)code & kNaClAlignmentMask);
  if (padding != kNaClAlignment) code = mono_arch_nacl_pad(code, padding);
  return code;
}

void mono_nacl_fix_patches(const guint8 *code, MonoJumpInfo *ji)
{
#ifndef USE_JUMP_TABLES
  MonoJumpInfo *patch_info;
  for (patch_info = ji; patch_info; patch_info = patch_info->next) {
    unsigned char *ip = patch_info->ip.i + code;
    ip = mono_arch_nacl_skip_nops(ip);
    patch_info->ip.i = ip - code;
  }
#endif
}
#endif  /* __native_client_codegen__ */

gboolean
mono_running_on_valgrind (void)
{
	if (RUNNING_ON_VALGRIND){
#ifdef VALGRIND_JIT_REGISTER_MAP
		valgrind_register = TRUE;
#endif
		return TRUE;
	} else
		return FALSE;
}

typedef struct {
	MonoExceptionClause *clause;
	MonoBasicBlock *basic_block;
	int start_offset;
} TryBlockHole;

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
	GSList *l;
	
	ji = mini_jit_info_table_find (domain, ip, &target_domain);
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
		for (l = tramp_infos; l; l = l->next) {
			MonoTrampInfo *tinfo = l->data;

			if ((guint8*)ip >= tinfo->code && (guint8*)ip <= tinfo->code + tinfo->code_size) {
				printf ("IP %p is at offset 0x%x of trampoline '%s'.\n", ip, (int)((guint8*)ip - tinfo->code), tinfo->name);
				return;
			}
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
		if (gsctx->var_is_vt || gsctx->mvar_is_vt)
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

	if (!caller || !callee)
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

/**
 * mono_emit_unwind_op:
 *
 *   Add an unwind op with the given parameters for the list of unwind ops stored in
 * cfg->unwind_ops.
 */
void
mono_emit_unwind_op (MonoCompile *cfg, int when, int tag, int reg, int val)
{
	MonoUnwindOp *op = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoUnwindOp));

	op->op = tag;
	op->reg = reg;
	op->val = val;
	op->when = when;
	
	cfg->unwind_ops = g_slist_append_mempool (cfg->mempool, cfg->unwind_ops, op);
	if (cfg->verbose_level > 1) {
		switch (tag) {
		case DW_CFA_def_cfa:
			printf ("CFA: [%x] def_cfa: %s+0x%x\n", when, mono_arch_regname (reg), val);
			break;
		case DW_CFA_def_cfa_register:
			printf ("CFA: [%x] def_cfa_reg: %s\n", when, mono_arch_regname (reg));
			break;
		case DW_CFA_def_cfa_offset:
			printf ("CFA: [%x] def_cfa_offset: 0x%x\n", when, val);
			break;
		case DW_CFA_offset:
			printf ("CFA: [%x] offset: %s at cfa-0x%x\n", when, mono_arch_regname (reg), -val);
			break;
		}
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
	GSList *l;

	g_free (info->name);

	// FIXME: ji
	for (l = info->unwind_ops; l; l = l->next)
		g_free (l->data);
	g_slist_free (info->unwind_ops);
	g_free (info);
}

/*
 * mono_tramp_info_register:
 *
 * Remember INFO for use by xdebug, mono_print_method_from_ip (), jit maps, etc.
 * INFO can be NULL.
 * Frees INFO.
 */
void
mono_tramp_info_register (MonoTrampInfo *info)
{
	MonoTrampInfo *copy;

	if (!info)
		return;

	copy = g_new0 (MonoTrampInfo, 1);
	copy->code = info->code;
	copy->code_size = info->code_size;
	copy->name = g_strdup (info->name);

	mono_jit_lock ();
	tramp_infos = g_slist_prepend (tramp_infos, copy);
	mono_jit_unlock ();

	mono_save_trampoline_xdebug_info (info);

	if (mono_jit_map_is_enabled ())
		mono_emit_jit_tramp (info->code, info->code_size, info->name);

	mono_tramp_info_free (info);
}

static void
mono_tramp_info_cleanup (void)
{
	GSList *l;

	for (l = tramp_infos; l; l = l->next) {
		MonoTrampInfo *info = l->data;

		mono_tramp_info_free (info);
	}
	g_slist_free (tramp_infos);
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

#define MONO_INIT_VARINFO(vi,id) do { \
	(vi)->range.first_use.pos.bid = 0xffff; \
	(vi)->reg = -1; \
        (vi)->idx = (id); \
} while (0)

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

static int
mono_find_block_region_notry (MonoCompile *cfg, int offset)
{
	MonoMethodHeader *header = cfg->header;
	MonoExceptionClause *clause;
	int i;

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
			else
				return ((i + 1) << 8) | MONO_REGION_CATCH | clause->flags;
		}
	}

	return -1;
}

/*
 * mono_get_block_region_notry:
 *
 *   Return the region corresponding to REGION, ignoring try clauses nested inside
 * finally clauses.
 */
int
mono_get_block_region_notry (MonoCompile *cfg, int region)
{
	if ((region & (0xf << 4)) == MONO_REGION_TRY) {
		MonoMethodHeader *header = cfg->header;
		
		/*
		 * This can happen if a try clause is nested inside a finally clause.
		 */
		int clause_index = (region >> 8) - 1;
		g_assert (clause_index >= 0 && clause_index < header->num_clauses);
		
		region = mono_find_block_region_notry (cfg, header->clauses [clause_index].try_offset);
	}

	return region;
}

MonoInst *
mono_find_spvar_for_region (MonoCompile *cfg, int region)
{
	region = mono_get_block_region_notry (cfg, region);

	return g_hash_table_lookup (cfg->spvars, GINT_TO_POINTER (region));
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

	type = mini_replace_type (type);

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
			type = mono_class_enum_basetype (type->data.klass);
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
		if (mini_type_var_is_vt (cfg, type))
			return OP_STOREV_MEMBASE;
		else
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

	type = mini_replace_type (type);

	switch (type->type) {
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
		g_assert (cfg->generic_sharing_context);
		if (mini_type_var_is_vt (cfg, type))
			return OP_LOADV_MEMBASE;
		else
			return OP_LOAD_MEMBASE;
	default:
		g_error ("unknown type 0x%02x in type_to_load_membase", type->type);
	}
	return -1;
}

static guint
mini_type_to_ldind (MonoCompile* cfg, MonoType *type)
{
	if (cfg->generic_sharing_context && !type->byref) {
		if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) {
			if (mini_type_var_is_vt (cfg, type))
				return CEE_LDOBJ;
			else
				return CEE_LDIND_REF;
		}
	}
	return mono_type_to_ldind (type);
}

#ifndef DISABLE_JIT

guint
mini_type_to_stind (MonoCompile* cfg, MonoType *type)
{
	type = mini_replace_type (type);

	if (cfg->generic_sharing_context && !type->byref) {
		if (type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) {
			if (mini_type_var_is_vt (cfg, type))
				return CEE_STOBJ;
			else
				return CEE_STIND_REF;
		}
	}
	return mono_type_to_stind (type);
}

int
mono_op_imm_to_op (int opcode)
{
	switch (opcode) {
	case OP_ADD_IMM:
#if SIZEOF_REGISTER == 4
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
#if SIZEOF_REGISTER == 4
		return OP_IAND;
#else
		return OP_LAND;
#endif
	case OP_OR_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IOR;
#else
		return OP_LOR;
#endif
	case OP_XOR_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IXOR;
#else
		return OP_LXOR;
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
	case OP_LREM_IMM:
		return OP_LREM;
	case OP_DIV_IMM:
#if SIZEOF_REGISTER == 4
		return OP_IDIV;
#else
		return OP_LDIV;
#endif
	case OP_REM_IMM:
#if SIZEOF_REGISTER == 4
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
	temp->dreg = mono_alloc_ireg (cfg);
	mono_bblock_insert_before_ins (bb, ins, temp);
	ins->opcode = mono_op_imm_to_op (ins->opcode);
	if (ins->opcode == OP_LOCALLOC)
		ins->sreg1 = temp->dreg;
	else
		ins->sreg2 = temp->dreg;

	bb->max_vreg = MAX (bb->max_vreg, cfg->next_vreg);
}

#endif

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

#ifdef DISABLE_JIT

MonoInst*
mono_compile_create_var (MonoCompile *cfg, MonoType *type, int opcode)
{
	return NULL;
}

#else

MonoInst*
mono_compile_create_var_for_vreg (MonoCompile *cfg, MonoType *type, int opcode, int vreg)
{
	MonoInst *inst;
	int num = cfg->num_varinfo;
	gboolean regpair;

	type = mini_replace_type (type);

	if ((num + 1) >= cfg->varinfo_count) {
		int orig_count = cfg->varinfo_count;
		cfg->varinfo_count = cfg->varinfo_count ? (cfg->varinfo_count * 2) : 32;
		cfg->varinfo = (MonoInst **)g_realloc (cfg->varinfo, sizeof (MonoInst*) * cfg->varinfo_count);
		cfg->vars = (MonoMethodVar *)g_realloc (cfg->vars, sizeof (MonoMethodVar) * cfg->varinfo_count);
		memset (&cfg->vars [orig_count], 0, (cfg->varinfo_count - orig_count) * sizeof (MonoMethodVar));
	}

	cfg->stat_allocate_var++;

	MONO_INST_NEW (cfg, inst, opcode);
	inst->inst_c0 = num;
	inst->inst_vtype = type;
	inst->klass = mono_class_from_mono_type (type);
	type_to_eval_stack_type (cfg, type, inst);
	/* if set to 1 the variable is native */
	inst->backend.is_pinvoke = 0;
	inst->dreg = vreg;

	if (inst->klass->exception_type)
		mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);

	if (cfg->compute_gc_maps) {
		if (type->byref) {
			mono_mark_vreg_as_mp (cfg, vreg);
		} else {
			MonoType *t = mini_replace_type (type);
			if ((MONO_TYPE_ISSTRUCT (t) && inst->klass->has_references) || mini_type_is_reference (cfg, t)) {
				inst->flags |= MONO_INST_GC_TRACK;
				mono_mark_vreg_as_ref (cfg, vreg);
			}
		}
	}
	
	cfg->varinfo [num] = inst;

	MONO_INIT_VARINFO (&cfg->vars [num], num);
	MONO_VARINFO (cfg, num)->vreg = vreg;

	if (vreg != -1)
		set_vreg_to_inst (cfg, vreg, inst);

#if SIZEOF_REGISTER == 4
	if (mono_arch_is_soft_float ()) {
		regpair = mono_type_is_long (type) || mono_type_is_float (type);
	} else {
		regpair = mono_type_is_long (type);
	}
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

		if (mono_arch_is_soft_float () && cfg->opt & MONO_OPT_SSA) {
			if (mono_type_is_float (type))
				inst->flags = MONO_INST_VOLATILE;
		}

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
	type = mini_replace_type (type);

	if (mono_type_is_long (type))
		dreg = mono_alloc_dreg (cfg, STACK_I8);
	else if (mono_arch_is_soft_float () && mono_type_is_float (type))
		dreg = mono_alloc_dreg (cfg, STACK_R8);
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
	dest->inst_i0 = cfg->varinfo [var_index];
	dest->opcode = mini_type_to_ldind (cfg, dest->inst_i0->inst_vtype);
	type_to_eval_stack_type (cfg, dest->inst_i0->inst_vtype, dest);
	dest->klass = dest->inst_i0->klass;
}

#endif

void
mono_mark_vreg_as_ref (MonoCompile *cfg, int vreg)
{
	if (vreg >= cfg->vreg_is_ref_len) {
		gboolean *tmp = cfg->vreg_is_ref;
		int size = cfg->vreg_is_ref_len;

		while (vreg >= cfg->vreg_is_ref_len)
			cfg->vreg_is_ref_len = cfg->vreg_is_ref_len ? cfg->vreg_is_ref_len * 2 : 32;
		cfg->vreg_is_ref = mono_mempool_alloc0 (cfg->mempool, sizeof (gboolean) * cfg->vreg_is_ref_len);
		if (size)
			memcpy (cfg->vreg_is_ref, tmp, size * sizeof (gboolean));
	}
	cfg->vreg_is_ref [vreg] = TRUE;
}	

void
mono_mark_vreg_as_mp (MonoCompile *cfg, int vreg)
{
	if (vreg >= cfg->vreg_is_mp_len) {
		gboolean *tmp = cfg->vreg_is_mp;
		int size = cfg->vreg_is_mp_len;

		while (vreg >= cfg->vreg_is_mp_len)
			cfg->vreg_is_mp_len = cfg->vreg_is_mp_len ? cfg->vreg_is_mp_len * 2 : 32;
		cfg->vreg_is_mp = mono_mempool_alloc0 (cfg->mempool, sizeof (gboolean) * cfg->vreg_is_mp_len);
		if (size)
			memcpy (cfg->vreg_is_mp, tmp, size * sizeof (gboolean));
	}
	cfg->vreg_is_mp [vreg] = TRUE;
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

static MonoMethodSignature *
mono_get_array_new_va_signature (int arity)
{
	static GHashTable *sighash;
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
	if (ARCH_VARARG_ICALLS)
		/* Only set this only some archs since not all backends can handle varargs+pinvoke */
		res->call_convention = MONO_CALL_VARARG;

#ifdef TARGET_WIN32
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

gboolean
mini_class_is_system_array (MonoClass *klass)
{
	if (klass->parent == mono_defaults.array_class)
		return TRUE;
	else
		return FALSE;
}

gboolean
mini_assembly_can_skip_verification (MonoDomain *domain, MonoMethod *method)
{
	MonoAssembly *assembly = method->klass->image->assembly;
	if (method->wrapper_type != MONO_WRAPPER_NONE && method->wrapper_type != MONO_WRAPPER_DYNAMIC_METHOD)
		return FALSE;
	if (assembly->in_gac || assembly->image == mono_defaults.corlib)
		return FALSE;
	if (mono_security_enabled ())
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
static gboolean
mini_method_verify (MonoCompile *cfg, MonoMethod *method, gboolean fail_compile)
{
	GSList *tmp, *res;
	gboolean is_fulltrust;
	MonoLoaderError *error;

	if (method->verification_success)
		return FALSE;

	if (!mono_verifier_is_enabled_for_method (method))
		return FALSE;

	/*skip verification implies the assembly must be */
	is_fulltrust = mono_verifier_is_method_full_trust (method) ||  mini_assembly_can_skip_verification (cfg->domain, method);

	res = mono_method_verify_with_current_settings (method, cfg->skip_visibility, is_fulltrust);

	if ((error = mono_loader_get_last_error ())) {
		if (fail_compile)
			cfg->exception_type = error->exception_type;
		else
			mono_loader_clear_error ();
		if (res)
			mono_free_verify_list (res);
		return TRUE;
	}

	if (res) { 
		for (tmp = res; tmp; tmp = tmp->next) {
			MonoVerifyInfoExtended *info = (MonoVerifyInfoExtended *)tmp->data;
			if (info->info.status == MONO_VERIFY_ERROR) {
				if (fail_compile) {
				char *method_name = mono_method_full_name (method, TRUE);
					cfg->exception_type = info->exception_type;
					cfg->exception_message = g_strdup_printf ("Error verifying %s: %s", method_name, info->info.message);
					g_free (method_name);
				}
				mono_free_verify_list (res);
				return TRUE;
			}
			if (info->info.status == MONO_VERIFY_NOT_VERIFIABLE && (!is_fulltrust || info->exception_type == MONO_EXCEPTION_METHOD_ACCESS || info->exception_type == MONO_EXCEPTION_FIELD_ACCESS)) {
				if (fail_compile) {
					char *method_name = mono_method_full_name (method, TRUE);
					cfg->exception_type = info->exception_type;
					cfg->exception_message = g_strdup_printf ("Error verifying %s: %s", method_name, info->info.message);
					g_free (method_name);
				}
				mono_free_verify_list (res);
				return TRUE;
			}
		}
		mono_free_verify_list (res);
	}
	method->verification_success = 1;
	return FALSE;
}

/*Returns true if something went wrong*/
gboolean
mono_compile_is_broken (MonoCompile *cfg, MonoMethod *method, gboolean fail_compile)
{
	MonoMethod *method_definition = method;
	gboolean dont_verify = method->klass->image->assembly->corlib_internal;

	while (method_definition->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *) method_definition;
		method_definition = imethod->declaring;
	}

	return !dont_verify && mini_method_verify (cfg, method_definition, fail_compile);
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
	mono_loader_lock (); /*FIXME mono_compile_method requires the loader lock, by large.*/
	mono_domain_lock (domain);

	if (callinfo->trampoline) {
		mono_domain_unlock (domain);
		mono_loader_unlock ();
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
	mono_loader_unlock ();
	
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
mono_allocate_stack_slots2 (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
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
	gboolean reuse_slot;

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

		t = mono_type_get_underlying_type (inst->inst_vtype);
		if (cfg->gsharedvt && mini_is_gsharedvt_variable_type (cfg, t))
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (t) && t->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type (t), &align);
		}
		else {
			int ialign;

			size = mini_type_stack_size (NULL, t, &ialign);
			align = ialign;

			if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type (t)))
				align = 16;
		}

		reuse_slot = TRUE;
		if (cfg->disable_reuse_stack_slots)
			reuse_slot = FALSE;

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
			if (cfg->disable_reuse_ref_stack_slots)
				reuse_slot = FALSE;
			break;

		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
#if SIZEOF_VOID_P == 4
		case MONO_TYPE_I4:
#else
		case MONO_TYPE_I8:
#endif
			if (cfg->disable_ref_noref_stack_slot_share) {
				slot_info = &scalar_stack_slots [MONO_TYPE_I];
				break;
			}
			/* Fall through */

		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
			/* Share non-float stack slots of the same size */
			slot_info = &scalar_stack_slots [MONO_TYPE_CLASS];
			if (cfg->disable_reuse_ref_stack_slots)
				reuse_slot = FALSE;
			break;

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

			if (count == atoi (g_getenv ("COUNT3")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (g_getenv ("COUNT3")))
				slot = 0xffffff;
			else {
				mono_print_ins (inst);
				}
		}
#endif

		LSCAN_DEBUG (printf ("R%d %s -> 0x%x\n", inst->dreg, mono_type_full_name (t), slot));

		if (inst->flags & MONO_INST_LMF) {
			size = sizeof (MonoLMF);
			align = sizeof (mgreg_t);
			reuse_slot = FALSE;
		}

		if (!reuse_slot)
			slot = 0xffffff;

		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (gpointer) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (MONO_TYPE_ISSTRUCT (t)) {
				align = MAX (align, sizeof (gpointer));
				align = MAX (align, mono_class_min_align (mono_class_from_mono_type (t)));
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

	cfg->stat_locals_stack_size += offset;

	*stack_size = offset;
	return offsets;
}

/*
 *  mono_allocate_stack_slots:
 *
 *  Allocate stack slots for all non register allocated variables using a
 * linear scan algorithm.
 * Returns: an array of stack offsets.
 * STACK_SIZE is set to the amount of stack space needed.
 * STACK_ALIGN is set to the alignment needed by the locals area.
 */
gint32*
mono_allocate_stack_slots (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
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
	gboolean reuse_slot;

	if ((cfg->num_varinfo > 0) && MONO_VARINFO (cfg, 0)->interval)
		return mono_allocate_stack_slots2 (cfg, backward, stack_size, stack_align);

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
	*stack_align = sizeof(mgreg_t);
	for (l = vars; l; l = l->next) {
		vmv = l->data;
		inst = cfg->varinfo [vmv->idx];

		t = mono_type_get_underlying_type (inst->inst_vtype);
		if (cfg->gsharedvt && mini_is_gsharedvt_variable_type (cfg, t))
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structures */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (t) && t->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type (t), &align);
		} else {
			int ialign;

			size = mini_type_stack_size (NULL, t, &ialign);
			align = ialign;

			if (mono_class_from_mono_type (t)->exception_type)
				mono_cfg_set_exception (cfg, MONO_EXCEPTION_TYPE_LOAD);

			if (MONO_CLASS_IS_SIMD (cfg, mono_class_from_mono_type (t)))
				align = 16;
		}

		reuse_slot = TRUE;
		if (cfg->disable_reuse_stack_slots)
			reuse_slot = FALSE;

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
				if (cfg->disable_reuse_ref_stack_slots)
					reuse_slot = FALSE;
				break;

			case MONO_TYPE_PTR:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
#if SIZEOF_VOID_P == 4
			case MONO_TYPE_I4:
#else
			case MONO_TYPE_I8:
#endif
				if (cfg->disable_ref_noref_stack_slot_share) {
					slot_info = &scalar_stack_slots [MONO_TYPE_I];
					break;
				}
				/* Fall through */

			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_STRING:
				/* Share non-float stack slots of the same size */
				slot_info = &scalar_stack_slots [MONO_TYPE_CLASS];
				if (cfg->disable_reuse_ref_stack_slots)
					reuse_slot = FALSE;
				break;
			case MONO_TYPE_VAR:
			case MONO_TYPE_MVAR:
				slot_info = &scalar_stack_slots [t->type];
				break;
			default:
				slot_info = &scalar_stack_slots [t->type];
				break;
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
			if (count == atoi (g_getenv ("COUNT")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (g_getenv ("COUNT")))
				slot = 0xffffff;
			else {
				mono_print_ins (inst);
				}
			*/
		}

		if (inst->flags & MONO_INST_LMF) {
			/*
			 * This variable represents a MonoLMF structure, which has no corresponding
			 * CLR type, so hard-code its size/alignment.
			 */
			size = sizeof (MonoLMF);
			align = sizeof (mgreg_t);
			reuse_slot = FALSE;
		}

		if (!reuse_slot)
			slot = 0xffffff;

		if (slot == 0xffffff) {
			/*
			 * Allways allocate valuetypes to sizeof (gpointer) to allow more
			 * efficient copying (and to work around the fact that OP_MEMCPY
			 * and OP_MEMSET ignores alignment).
			 */
			if (MONO_TYPE_ISSTRUCT (t)) {
				align = MAX (align, sizeof (gpointer));
				align = MAX (align, mono_class_min_align (mono_class_from_mono_type (t)));
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

	cfg->stat_locals_stack_size += offset;

	*stack_size = offset;
	return offsets;
}

#else

gint32*
mono_allocate_stack_slots (MonoCompile *cfg, gboolean backward, guint32 *stack_size, guint32 *stack_align)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* DISABLE_JIT */

#define EMUL_HIT_SHIFT 3
#define EMUL_HIT_MASK ((1 << EMUL_HIT_SHIFT) - 1)
/* small hit bitmap cache */
static mono_byte emul_opcode_hit_cache [(OP_LAST>>EMUL_HIT_SHIFT) + 1] = {0};
static short emul_opcode_num = 0;
static short emul_opcode_alloced = 0;
static short *emul_opcode_opcodes;
static MonoJitICallInfo **emul_opcode_map;

MonoJitICallInfo *
mono_find_jit_opcode_emulation (int opcode)
{
	g_assert (opcode >= 0 && opcode <= OP_LAST);
	if (emul_opcode_hit_cache [opcode >> (EMUL_HIT_SHIFT + 3)] & (1 << (opcode & EMUL_HIT_MASK))) {
		int i;
		for (i = 0; i < emul_opcode_num; ++i) {
			if (emul_opcode_opcodes [i] == opcode)
				return emul_opcode_map [i];
		}
	}
	return NULL;
}

static void
register_opcode_emulation (int opcode, const char *name, const char *sigstr, gpointer func, const char *symbol, gboolean no_throw)
{
	MonoJitICallInfo *info;
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	g_assert (!sig->hasthis);
	g_assert (sig->param_count < 3);

	info = mono_register_jit_icall_full (func, name, sig, no_throw, symbol);

	if (emul_opcode_num >= emul_opcode_alloced) {
		int incr = emul_opcode_alloced? emul_opcode_alloced/2: 16;
		emul_opcode_alloced += incr;
		emul_opcode_map = g_realloc (emul_opcode_map, sizeof (emul_opcode_map [0]) * emul_opcode_alloced);
		emul_opcode_opcodes = g_realloc (emul_opcode_opcodes, sizeof (emul_opcode_opcodes [0]) * emul_opcode_alloced);
	}
	emul_opcode_map [emul_opcode_num] = info;
	emul_opcode_opcodes [emul_opcode_num] = opcode;
	emul_opcode_num++;
	emul_opcode_hit_cache [opcode >> (EMUL_HIT_SHIFT + 3)] |= (1 << (opcode & EMUL_HIT_MASK));
}

/*
 * For JIT icalls implemented in C.
 * NAME should be the same as the name of the C function whose address is FUNC.
 * If SAVE is TRUE, no wrapper is generated. This is for perf critical icalls which
 * can't throw exceptions.
 */
static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean save)
{
	MonoMethodSignature *sig;

	if (sigstr)
		sig = mono_create_icall_signature (sigstr);
	else
		sig = NULL;

	mono_register_jit_icall_full (func, name, sig, save, save ? name : NULL);
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

static void
print_dfn (MonoCompile *cfg) {
	int i, j;
	char *code;
	MonoBasicBlock *bb;
	MonoInst *c;

	{
		char *method_name = mono_method_full_name (cfg->method, TRUE);
		g_print ("IR code for method %s\n", method_name);
		g_free (method_name);
	}

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
			mono_print_ins_index (-1, c);
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
		ins = bb->code;
		if (ins)
			ins->prev = ins_to_insert;
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
	GSList *l;

	if (cfg->header)
		mono_metadata_free_mh (cfg->header);
	//mono_mempool_stats (cfg->mempool);
	mono_free_loop_info (cfg);
	if (cfg->rs)
		mono_regstate_free (cfg->rs);
	if (cfg->spvars)
		g_hash_table_destroy (cfg->spvars);
	if (cfg->exvars)
		g_hash_table_destroy (cfg->exvars);
	for (l = cfg->headers_to_free; l; l = l->next)
		mono_metadata_free_mh (l->data);
	g_list_free (cfg->ldstr_list);
	g_hash_table_destroy (cfg->token_info_hash);
	if (cfg->abs_patches)
		g_hash_table_destroy (cfg->abs_patches);
	mono_mempool_destroy (cfg->mempool);

	mono_debug_free_method (cfg);

	g_free (cfg->varinfo);
	g_free (cfg->vars);
	g_free (cfg->exception_message);
	g_free (cfg);
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
	return MONO_FAST_TLS_GET (mono_lmf);
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
	return MONO_FAST_TLS_GET (mono_lmf_addr);
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

	mono_jit_thread_attach (NULL);
	
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
	return mono_native_tls_get_value (mono_jit_tls_id);
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
 * mono_jit_thread_attach:
 *
 * Called by native->managed wrappers. Returns the original domain which needs to be
 * restored, or NULL.
 */
MonoDomain*
mono_jit_thread_attach (MonoDomain *domain)
{
	MonoDomain *orig;

	if (!domain)
		/* 
		 * Happens when called from AOTed code which is only used in the root
		 * domain.
		 */
		domain = mono_get_root_domain ();

#ifdef MONO_HAVE_FAST_TLS
	if (!MONO_FAST_TLS_GET (mono_lmf_addr)) {
		mono_thread_attach (domain);
		// #678164
		mono_thread_set_state (mono_thread_internal_current (), ThreadState_Background);
	}
#else
	if (!mono_native_tls_get_value (mono_jit_tls_id)) {
		mono_thread_attach (domain);
		mono_thread_set_state (mono_thread_internal_current (), ThreadState_Background);
	}
#endif
	orig = mono_domain_get ();
	if (orig != domain)
 		mono_domain_set (domain, TRUE);

	return orig != domain ? orig : NULL;
}	

/* Called by native->managed wrappers */
void
mono_jit_set_domain (MonoDomain *domain)
{
	if (domain)
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

	jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	if (jit_tls)
		return jit_tls;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	jit_tls->abort_func = abort_func;
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
	MonoInternalThread *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort);
	thread = mono_thread_internal_current ();
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
	MonoInternalThread *thread;
	void *jit_tls = setup_jit_tls_data (stack_start, mono_thread_abort_dummy);
	thread = mono_thread_internal_current ();
	if (thread)
		thread->jit_data = jit_tls;
	if (mono_profiler_get_events () & MONO_PROFILE_STATISTICAL)
		mono_runtime_setup_stat_profiler ();

	mono_arch_cpu_init ();
}

static void
mini_thread_cleanup (MonoInternalThread *thread)
{
	MonoJitTlsData *jit_tls = thread->jit_data;

	if (jit_tls) {
		/* We can't clean up tls information if we are on another thread, it will clean up the wrong stuff
		 * It would be nice to issue a warning when this happens outside of the shutdown sequence. but it's
		 * not a trivial thing.
		 *
		 * The current offender is mono_thread_manage which cleanup threads from the outside.
		 */
		if (thread == mono_thread_internal_current ())
			mono_set_jit_tls (NULL);

		/* If we attach a thread but never call into managed land, we might never get an lmf.*/
		if (mono_get_lmf ()) {
			mono_set_lmf (NULL);
			mono_set_lmf_addr (NULL);
		}

		free_jit_tls_data (jit_tls);

		thread->jit_data = NULL;
	}
}

int
mini_get_tls_offset (MonoTlsKey key)
{
	int offset;

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

#ifndef DISABLE_JIT

static MonoInst*
mono_create_tls_get_offset (MonoCompile *cfg, int offset)
{
	MonoInst* ins;

	if (!MONO_ARCH_HAVE_TLS_GET)
		return NULL;

	if (offset == -1)
		return NULL;

	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->dreg = mono_alloc_preg (cfg);
	ins->inst_offset = offset;
	return ins;
}

gboolean
mini_tls_get_supported (MonoCompile *cfg, MonoTlsKey key)
{
	if (!MONO_ARCH_HAVE_TLS_GET)
		return FALSE;

	if (cfg->compile_aot)
		return ARCH_HAVE_TLS_GET_REG;
	else
		return mini_get_tls_offset (key) != -1;
}

MonoInst*
mono_create_tls_get (MonoCompile *cfg, MonoTlsKey key)
{
	/*
	 * TLS offsets might be different at AOT time, so load them from a GOT slot and
	 * use a different opcode.
	 */
	if (cfg->compile_aot) {
		if (MONO_ARCH_HAVE_TLS_GET && ARCH_HAVE_TLS_GET_REG) {
			MonoInst *ins, *c;

			EMIT_NEW_TLS_OFFSETCONST (cfg, c, key);
			MONO_INST_NEW (cfg, ins, OP_TLS_GET_REG);
			ins->dreg = mono_alloc_preg (cfg);
			ins->sreg1 = c->dreg;
			return ins;
		} else {
			return NULL;
		}
	}

	return mono_create_tls_get_offset (cfg, mini_get_tls_offset (key));
}

MonoInst*
mono_get_jit_tls_intrinsic (MonoCompile *cfg)
{
	return mono_create_tls_get (cfg, TLS_KEY_JIT_TLS);
}

MonoInst*
mono_get_domain_intrinsic (MonoCompile* cfg)
{
	return mono_create_tls_get (cfg, TLS_KEY_DOMAIN);
}

MonoInst*
mono_get_thread_intrinsic (MonoCompile* cfg)
{
	return mono_create_tls_get (cfg, TLS_KEY_THREAD);
}

MonoInst*
mono_get_lmf_intrinsic (MonoCompile* cfg)
{
	return mono_create_tls_get (cfg, TLS_KEY_LMF);
}

MonoInst*
mono_get_lmf_addr_intrinsic (MonoCompile* cfg)
{
	return mono_create_tls_get (cfg, TLS_KEY_LMF_ADDR);
}

#endif /* !DISABLE_JIT */


static gboolean
mini_tls_key_supported (MonoTlsKey key)
{
	if (!MONO_ARCH_HAVE_TLS_GET)
		return FALSE;

	return mini_get_tls_offset (key) != -1;
}

void
mono_add_patch_info (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target)
{
	MonoJumpInfo *ji = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfo));

	ji->ip.i = ip;
	ji->type = type;
	ji->data.target = target;
	ji->next = cfg->patch_info;

	cfg->patch_info = ji;
}

void
mono_add_patch_info_rel (MonoCompile *cfg, int ip, MonoJumpInfoType type, gconstpointer target, int relocation)
{
	MonoJumpInfo *ji = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoJumpInfo));

	ji->ip.i = ip;
	ji->type = type;
	ji->relocation = relocation;
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
		res->data.table->table = mono_mempool_alloc (mp, sizeof (MonoBasicBlock*) * patch_info->data.table->table_size);
		memcpy (res->data.table->table, patch_info->data.table->table, sizeof (MonoBasicBlock*) * patch_info->data.table->table_size);
		break;
	case MONO_PATCH_INFO_RGCTX_FETCH:
		res->data.rgctx_entry = mono_mempool_alloc (mp, sizeof (MonoJumpInfoRgctxEntry));
		memcpy (res->data.rgctx_entry, patch_info->data.rgctx_entry, sizeof (MonoJumpInfoRgctxEntry));
		res->data.rgctx_entry->data = mono_patch_info_dup_mp (mp, res->data.rgctx_entry->data);
		break;
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE:
		res->data.del_tramp = mono_mempool_alloc0 (mp, sizeof (MonoDelegateClassMethodPair));
		memcpy (res->data.del_tramp, patch_info->data.del_tramp, sizeof (MonoDelegateClassMethodPair));
		break;
	case MONO_PATCH_INFO_GSHAREDVT_CALL:
		res->data.gsharedvt = mono_mempool_alloc (mp, sizeof (MonoJumpInfoGSharedVtCall));
		memcpy (res->data.gsharedvt, patch_info->data.gsharedvt, sizeof (MonoJumpInfoGSharedVtCall));
		break;
	case MONO_PATCH_INFO_GSHAREDVT_METHOD: {
		MonoGSharedVtMethodInfo *info;
		MonoGSharedVtMethodInfo *oinfo;
		int i;

		oinfo = patch_info->data.gsharedvt_method;
		info = mono_mempool_alloc (mp, sizeof (MonoGSharedVtMethodInfo));
		res->data.gsharedvt_method = info;
		memcpy (info, oinfo, sizeof (MonoGSharedVtMethodInfo));
		info->entries = mono_mempool_alloc (mp, sizeof (MonoRuntimeGenericContextInfoTemplate) * info->count_entries);
		for (i = 0; i < oinfo->num_entries; ++i) {
			MonoRuntimeGenericContextInfoTemplate *otemplate = &oinfo->entries [i];
			MonoRuntimeGenericContextInfoTemplate *template = &info->entries [i];

			memcpy (template, otemplate, sizeof (MonoRuntimeGenericContextInfoTemplate));
		}
		//info->locals_types = mono_mempool_alloc0 (mp, info->nlocals * sizeof (MonoType*));
		//memcpy (info->locals_types, oinfo->locals_types, info->nlocals * sizeof (MonoType*));
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
	case MONO_PATCH_INFO_CLASS_INIT:
	case MONO_PATCH_INFO_GENERIC_CLASS_INIT:
	case MONO_PATCH_INFO_METHODCONST:
	case MONO_PATCH_INFO_METHOD:
	case MONO_PATCH_INFO_METHOD_JUMP:
	case MONO_PATCH_INFO_IMAGE:
	case MONO_PATCH_INFO_JIT_ICALL_ADDR:
	case MONO_PATCH_INFO_ICALL_ADDR:
	case MONO_PATCH_INFO_FIELD:
	case MONO_PATCH_INFO_SFLDA:
	case MONO_PATCH_INFO_SEQ_POINT_INFO:
	case MONO_PATCH_INFO_METHOD_RGCTX:
	case MONO_PATCH_INFO_SIGNATURE:
	case MONO_PATCH_INFO_TLS_OFFSET:
	case MONO_PATCH_INFO_METHOD_CODE_SLOT:
		return (ji->type << 8) | (gssize)ji->data.target;
	case MONO_PATCH_INFO_GSHAREDVT_CALL:
		return (ji->type << 8) | (gssize)ji->data.gsharedvt->method;
	case MONO_PATCH_INFO_RGCTX_FETCH: {
		MonoJumpInfoRgctxEntry *e = ji->data.rgctx_entry;

		return (ji->type << 8) | (gssize)e->method | (e->in_mrgctx) | e->info_type | mono_patch_info_hash (e->data);
	}
	case MONO_PATCH_INFO_INTERRUPTION_REQUEST_FLAG:
	case MONO_PATCH_INFO_MSCORLIB_GOT_ADDR:
	case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR:
	case MONO_PATCH_INFO_JIT_TLS_ID:
	case MONO_PATCH_INFO_MONITOR_ENTER:
	case MONO_PATCH_INFO_MONITOR_EXIT:
	case MONO_PATCH_INFO_GOT_OFFSET:
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
		return (ji->type << 8) | (gsize)ji->data.del_tramp->klass | (gsize)ji->data.del_tramp->method | (gsize)ji->data.del_tramp->virtual;
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
	case MONO_PATCH_INFO_RGCTX_FETCH: {
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
		return ji1->data.del_tramp->klass == ji2->data.del_tramp->klass && ji1->data.del_tramp->method == ji2->data.del_tramp->method && ji1->data.del_tramp->virtual == ji2->data.del_tramp->virtual;
	case MONO_PATCH_INFO_CASTCLASS_CACHE:
		return ji1->data.index == ji2->data.index;
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
		target = mono_create_jump_trampoline (domain, patch_info->data.method, FALSE);
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
		target = mono_create_jit_trampoline_in_domain (domain, patch_info->data.method);
#else
		if (patch_info->data.method == method) {
			target = code;
		} else {
			/* get the trampoline to the method from the domain */
			target = mono_create_jit_trampoline_in_domain (domain, patch_info->data.method);
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
	case MONO_PATCH_INFO_SWITCH: {
		gpointer *jump_table;
		int i;
#if defined(__native_client__) && defined(__native_client_codegen__)
		/* This memory will leak, but we don't care if we're */
		/* not deleting JIT'd methods anyway                 */
		jump_table = g_malloc0 (sizeof(gpointer) * patch_info->data.table->table_size);
#else
		if (method && method->dynamic) {
			jump_table = mono_code_manager_reserve (mono_dynamic_code_hash_lookup (domain, method)->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
		} else {
			if (mono_aot_only) {
				jump_table = mono_domain_alloc (domain, sizeof (gpointer) * patch_info->data.table->table_size);
			} else {
				jump_table = mono_domain_code_reserve (domain, sizeof (gpointer) * patch_info->data.table->table_size);
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
	case MONO_PATCH_INFO_DELEGATE_TRAMPOLINE: {
		MonoDelegateClassMethodPair *del_tramp = patch_info->data.del_tramp;

		if (del_tramp->virtual)
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
			if (run_cctors)
				mono_runtime_class_init (vtable);
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
							   patch_info->data.token->token, &handle_class, patch_info->data.token->has_context ? &patch_info->data.token->context : NULL);
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
					if (mono_aot_only)
						mono_raise_exception (mono_exception_from_name_msg (mono_defaults.corlib, "System", exc_class, exc_arg));
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
			slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, &entry->data->data.klass->byval_arg, entry->info_type, mono_method_get_context (entry->method));
			break;
		case MONO_PATCH_INFO_METHOD:
		case MONO_PATCH_INFO_METHODCONST:
			slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, entry->data->data.method, entry->info_type, mono_method_get_context (entry->method));
			break;
		case MONO_PATCH_INFO_FIELD:
			slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, entry->data->data.field, entry->info_type, mono_method_get_context (entry->method));
			break;
		case MONO_PATCH_INFO_SIGNATURE:
			slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, entry->data->data.sig, entry->info_type, mono_method_get_context (entry->method));
			break;
		case MONO_PATCH_INFO_GSHAREDVT_CALL: {
			MonoJumpInfoGSharedVtCall *call_info = g_malloc0 (sizeof (MonoJumpInfoGSharedVtCall)); //mono_domain_alloc0 (domain, sizeof (MonoJumpInfoGSharedVtCall));

			memcpy (call_info, entry->data->data.gsharedvt, sizeof (MonoJumpInfoGSharedVtCall));
			slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, call_info, entry->info_type, mono_method_get_context (entry->method));
			break;
		}
		case MONO_PATCH_INFO_GSHAREDVT_METHOD: {
			MonoGSharedVtMethodInfo *info;
			MonoGSharedVtMethodInfo *oinfo = entry->data->data.gsharedvt_method;
			int i;

			/* Make a copy into the domain mempool */
			info = g_malloc0 (sizeof (MonoGSharedVtMethodInfo)); //mono_domain_alloc0 (domain, sizeof (MonoGSharedVtMethodInfo));
			info->method = oinfo->method;
			info->num_entries = oinfo->num_entries;
			info->entries = g_malloc0 (sizeof (MonoRuntimeGenericContextInfoTemplate) * info->num_entries);
			for (i = 0; i < oinfo->num_entries; ++i) {
				MonoRuntimeGenericContextInfoTemplate *otemplate = &oinfo->entries [i];
				MonoRuntimeGenericContextInfoTemplate *template = &info->entries [i];

				memcpy (template, otemplate, sizeof (MonoRuntimeGenericContextInfoTemplate));
			}
			slot = mono_method_lookup_or_register_info (entry->method, entry->in_mrgctx, info, entry->info_type, mono_method_get_context (entry->method));
			break;
		}
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
#ifdef MONO_ARCH_SOFT_DEBUG_SUPPORTED
	case MONO_PATCH_INFO_SEQ_POINT_INFO:
		if (!run_cctors)
			/* AOT, not needed */
			target = NULL;
		else
			target = mono_arch_get_seq_point_info (domain, code);
		break;
#endif
	case MONO_PATCH_INFO_LLVM_IMT_TRAMPOLINE:
#ifdef MONO_ARCH_LLVM_SUPPORTED
		g_assert (mono_use_llvm);
		target = mono_create_llvm_imt_trampoline (domain, patch_info->data.imt_tramp->method, patch_info->data.imt_tramp->vt_offset);
#else
		g_assert_not_reached ();
#endif
		break;
	case MONO_PATCH_INFO_GC_CARD_TABLE_ADDR: {
		int card_table_shift_bits;
		gpointer card_table_mask;

		target = mono_gc_get_card_table (&card_table_shift_bits, &card_table_mask);
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

		offset = mini_get_tls_offset (GPOINTER_TO_INT (patch_info->data.target));
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
	default:
		g_assert_not_reached ();
	}

	return (gpointer)target;
}

void
mono_add_seq_point (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst *ins, int native_offset)
{
	ins->inst_offset = native_offset;
	g_ptr_array_add (cfg->seq_points, ins);
	if (bb) {
		bb->seq_points = g_slist_prepend_mempool (cfg->mempool, bb->seq_points, ins);
		bb->last_seq_point = ins;
	}
}

void
mono_add_var_location (MonoCompile *cfg, MonoInst *var, gboolean is_reg, int reg, int offset, int from, int to)
{
	MonoDwarfLocListEntry *entry = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoDwarfLocListEntry));

	if (is_reg)
		g_assert (offset == 0);

	entry->is_reg = is_reg;
	entry->reg = reg;
	entry->offset = offset;
	entry->from = from;
	entry->to = to;

	if (var == cfg->args [0])
		cfg->this_loclist = g_slist_append_mempool (cfg->mempool, cfg->this_loclist, entry);
	else if (var == cfg->rgctx_var)
		cfg->rgctx_loclist = g_slist_append_mempool (cfg->mempool, cfg->rgctx_loclist, entry);
}

#ifndef DISABLE_JIT

static void
mono_compile_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i;

	header = cfg->header;

	sig = mono_method_signature (cfg->method);
	
	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		cfg->ret = mono_compile_create_var (cfg, sig->ret, OP_ARG);
		/* Inhibit optimizations */
		cfg->ret->flags |= MONO_INST_VOLATILE;
	}
	if (cfg->verbose_level > 2)
		g_print ("creating vars\n");

	cfg->args = mono_mempool_alloc0 (cfg->mempool, (sig->param_count + sig->hasthis) * sizeof (MonoInst*));

	if (sig->hasthis)
		cfg->args [0] = mono_compile_create_var (cfg, &cfg->method->klass->this_arg, OP_ARG);

	for (i = 0; i < sig->param_count; ++i) {
		cfg->args [i + sig->hasthis] = mono_compile_create_var (cfg, sig->params [i], OP_ARG);
	}

	if (cfg->verbose_level > 2) {
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

	if (cfg->method->save_lmf && cfg->create_lmf_var) {
		MonoInst *lmf_var = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		lmf_var->flags |= MONO_INST_VOLATILE;
		lmf_var->flags |= MONO_INST_LMF;
		cfg->lmf_var = lmf_var;
	}
}

void
mono_print_code (MonoCompile *cfg, const char* msg)
{
	MonoBasicBlock *bb;
	
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		mono_print_bb (bb, msg);
}

static void
mono_postprocess_patches (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int i;

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
#if defined(__native_client__) && defined(__native_client_codegen__)
			/* This memory will leak.  */
			/* TODO: can we free this when  */
			/* making the final jump table? */
			table = g_malloc0 (sizeof(gpointer) * patch_info->data.table->table_size);
#else
			if (cfg->method->dynamic) {
				table = mono_code_manager_reserve (cfg->dynamic_info->code_mp, sizeof (gpointer) * patch_info->data.table->table_size);
			} else {
				table = mono_domain_code_reserve (cfg->domain, sizeof (gpointer) * patch_info->data.table->table_size);
			}
#endif

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
			MonoJumpList *jlist;
			MonoDomain *domain = cfg->domain;
			unsigned char *ip = cfg->native_code + patch_info->ip.i;
#if defined(__native_client__) && defined(__native_client_codegen__)
			/* When this jump target gets evaluated, the method */
			/* will be installed in the dynamic code section,   */
			/* not at the location of cfg->native_code.         */
			ip = nacl_inverse_modify_patch_target (cfg->native_code) + patch_info->ip.i;
#endif

			mono_domain_lock (domain);
			jlist = g_hash_table_lookup (domain_jit_info (domain)->jump_target_hash, patch_info->data.method);
			if (!jlist) {
				jlist = mono_domain_alloc0 (domain, sizeof (MonoJumpList));
				g_hash_table_insert (domain_jit_info (domain)->jump_target_hash, patch_info->data.method, jlist);
			}
			jlist->list = g_slist_prepend (jlist->list, ip);
			mono_domain_unlock (domain);
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}
}

static void
collect_pred_seq_points (MonoBasicBlock *bb, MonoInst *ins, GSList **next, int depth)
{
	int i;
	MonoBasicBlock *in_bb;
	GSList *l;

	for (i = 0; i < bb->in_count; ++i) {
		in_bb = bb->in_bb [i];

		if (in_bb->last_seq_point) {
			int src_index = in_bb->last_seq_point->backend.size;
			int dst_index = ins->backend.size;

			/* bb->in_bb might contain duplicates */
			for (l = next [src_index]; l; l = l->next)
				if (GPOINTER_TO_UINT (l->data) == dst_index)
					break;
			if (!l)
				next [src_index] = g_slist_append (next [src_index], GUINT_TO_POINTER (dst_index));
		} else {
			/* Have to look at its predecessors */
			if (depth < 5)
				collect_pred_seq_points (in_bb, ins, next, depth + 1);
		}
	}
}

static void
mono_save_seq_point_info (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	GSList *bb_seq_points, *l;
	MonoInst *last;
	MonoDomain *domain = cfg->domain;
	int i;
	MonoSeqPointInfo *info;
	GSList **next;

	if (!cfg->seq_points)
		return;

	info = g_malloc0 (sizeof (MonoSeqPointInfo) + (cfg->seq_points->len * sizeof (SeqPoint)));
	info->len = cfg->seq_points->len;
	for (i = 0; i < cfg->seq_points->len; ++i) {
		SeqPoint *sp = &info->seq_points [i];
		MonoInst *ins = g_ptr_array_index (cfg->seq_points, i);

		sp->il_offset = ins->inst_imm;
		sp->native_offset = ins->inst_offset;
		if (ins->flags & MONO_INST_NONEMPTY_STACK)
			sp->flags |= MONO_SEQ_POINT_FLAG_NONEMPTY_STACK;

		/* Used below */
		ins->backend.size = i;
	}

	/*
	 * For each sequence point, compute the list of sequence points immediately
	 * following it, this is needed to implement 'step over' in the debugger agent.
	 */ 
	next = g_new0 (GSList*, cfg->seq_points->len);
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		bb_seq_points = g_slist_reverse (bb->seq_points);
		last = NULL;
		for (l = bb_seq_points; l; l = l->next) {
			MonoInst *ins = l->data;

			if (ins->inst_imm == METHOD_ENTRY_IL_OFFSET || ins->inst_imm == METHOD_EXIT_IL_OFFSET)
				/* Used to implement method entry/exit events */
				continue;
			if (ins->inst_offset == SEQ_POINT_NATIVE_OFFSET_DEAD_CODE)
				continue;

			if (last != NULL) {
				/* Link with the previous seq point in the same bb */
				next [last->backend.size] = g_slist_append (next [last->backend.size], GUINT_TO_POINTER (ins->backend.size));
			} else {
				/* Link with the last bb in the previous bblocks */
				collect_pred_seq_points (bb, ins, next, 0);
			}

			last = ins;
		}

		if (bb->last_ins && bb->last_ins->opcode == OP_ENDFINALLY && bb->seq_points) {
			MonoBasicBlock *bb2;
			MonoInst *endfinally_seq_point = NULL;

			/*
			 * The ENDFINALLY branches are not represented in the cfg, so link it with all seq points starting bbs.
			 */
			l = g_slist_last (bb->seq_points);
			if (l) {
				endfinally_seq_point = l->data;

				for (bb2 = cfg->bb_entry; bb2; bb2 = bb2->next_bb) {
					GSList *l = g_slist_last (bb2->seq_points);

					if (l) {
						MonoInst *ins = l->data;

						if (!(ins->inst_imm == METHOD_ENTRY_IL_OFFSET || ins->inst_imm == METHOD_EXIT_IL_OFFSET) && ins != endfinally_seq_point)
							next [endfinally_seq_point->backend.size] = g_slist_append (next [endfinally_seq_point->backend.size], GUINT_TO_POINTER (ins->backend.size));
					}
				}
			}
		}
	}

	if (cfg->verbose_level > 2) {
		printf ("\nSEQ POINT MAP: \n");
	}

	for (i = 0; i < cfg->seq_points->len; ++i) {
		SeqPoint *sp = &info->seq_points [i];
		GSList *l;
		int j, next_index;

		sp->next_len = g_slist_length (next [i]);
		sp->next = g_new (int, sp->next_len);
		j = 0;
		if (cfg->verbose_level > 2 && next [i]) {
			printf ("\tIL0x%x ->", sp->il_offset);
			for (l = next [i]; l; l = l->next) {
				next_index = GPOINTER_TO_UINT (l->data);
				printf (" IL0x%x", info->seq_points [next_index].il_offset);
			}
			printf ("\n");
		}
		for (l = next [i]; l; l = l->next) {
			next_index = GPOINTER_TO_UINT (l->data);
			sp->next [j ++] = next_index;
		}
		g_slist_free (next [i]);
	}
	g_free (next);

	cfg->seq_point_info = info;

	// FIXME: dynamic methods
	if (!cfg->compile_aot) {
		mono_domain_lock (domain);
		// FIXME: How can the lookup succeed ?
		if (!g_hash_table_lookup (domain_jit_info (domain)->seq_points, cfg->method_to_register))
			g_hash_table_insert (domain_jit_info (domain)->seq_points, cfg->method_to_register, info);
		mono_domain_unlock (domain);
	}

	g_ptr_array_free (cfg->seq_points, TRUE);
	cfg->seq_points = NULL;
}

void
mono_codegen (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	int max_epilog_size;
	guint8 *code;
	MonoDomain *code_domain;

	if (mono_using_xdebug)
		/*
		 * Recent gdb versions have trouble processing symbol files containing
		 * overlapping address ranges, so allocate all code from the code manager
		 * of the root domain. (#666152).
		 */
		code_domain = mono_get_root_domain ();
	else
		code_domain = cfg->domain;

#if defined(__native_client_codegen__) && defined(__native_client__)
	void *code_dest;

	/* This keeps patch targets from being transformed during
	 * ordinary method compilation, for local branches and jumps.
	 */
	nacl_allow_target_modification (FALSE);
#endif

	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		cfg->spill_count = 0;
		/* we reuse dfn here */
		/* bb->dfn = bb_count++; */

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

	cfg->code_len = code - cfg->native_code;
	cfg->prolog_end = cfg->code_len;

	mono_debug_open_method (cfg);

	/* emit code all basic blocks */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		bb->native_offset = cfg->code_len;
		bb->real_native_offset = cfg->code_len;
		//if ((bb == cfg->bb_entry) || !(bb->region == -1 && !bb->dfn))
			mono_arch_output_basic_block (cfg, bb);
		bb->native_length = cfg->code_len - bb->native_offset;

		if (bb == cfg->bb_exit) {
			cfg->epilog_begin = cfg->code_len;
			mono_arch_emit_epilog (cfg);
			cfg->epilog_end = cfg->code_len;
		}
	}

#ifdef __native_client_codegen__
	mono_nacl_fix_patches (cfg->native_code, cfg->patch_info);
#endif
	mono_arch_emit_exceptions (cfg);

	max_epilog_size = 0;

	/* we always allocate code in cfg->domain->code_mp to increase locality */
	cfg->code_size = cfg->code_len + max_epilog_size;
#ifdef __native_client_codegen__
	cfg->code_size = NACL_BUNDLE_ALIGN_UP (cfg->code_size);
#endif
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

		if (mono_using_xdebug)
			/* See the comment for cfg->code_domain */
			code = mono_domain_code_reserve (code_domain, cfg->code_size + unwindlen);
		else
			code = mono_code_manager_reserve (cfg->dynamic_info->code_mp, cfg->code_size + unwindlen);
	} else {
		guint unwindlen = 0;
#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
		unwindlen = mono_arch_unwindinfo_get_size (cfg->arch.unwindinfo);
#endif
		code = mono_domain_code_reserve (code_domain, cfg->code_size + unwindlen);
	}
#if defined(__native_client_codegen__) && defined(__native_client__)
	nacl_allow_target_modification (TRUE);
#endif

	g_assert (code);
	memcpy (code, cfg->native_code, cfg->code_len);
#if defined(__default_codegen__)
	g_free (cfg->native_code);
#elif defined(__native_client_codegen__)
	if (cfg->native_code_alloc) {
		g_free (cfg->native_code_alloc);
		cfg->native_code_alloc = 0;
	}
	else if (cfg->native_code) {
		g_free (cfg->native_code);
	}
#endif /* __native_client_codegen__ */
	cfg->native_code = code;
	code = cfg->native_code + cfg->code_len;
  
	/* g_assert (((int)cfg->native_code & (MONO_ARCH_CODE_ALIGNMENT - 1)) == 0); */
	mono_postprocess_patches (cfg);

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

#if defined(__native_client_codegen__) && defined(__native_client__)
	if (!cfg->compile_aot) {
		if (cfg->method->dynamic) {
			code_dest = nacl_code_manager_get_code_dest(cfg->dynamic_info->code_mp, cfg->native_code);
		} else {
			code_dest = nacl_domain_get_code_dest(cfg->domain, cfg->native_code);
		}
	}
#endif

#if defined(__native_client_codegen__)
	mono_nacl_fix_patches (cfg->native_code, cfg->patch_info);
#endif

	mono_arch_patch_code (cfg->method, cfg->domain, cfg->native_code, cfg->patch_info, cfg->dynamic_info ? cfg->dynamic_info->code_mp : NULL, cfg->run_cctors);

	if (cfg->method->dynamic) {
		if (mono_using_xdebug)
			mono_domain_code_commit (code_domain, cfg->native_code, cfg->code_size, cfg->code_len);
		else
			mono_code_manager_commit (cfg->dynamic_info->code_mp, cfg->native_code, cfg->code_size, cfg->code_len);
	} else {
		mono_domain_code_commit (code_domain, cfg->native_code, cfg->code_size, cfg->code_len);
	}
#if defined(__native_client_codegen__) && defined(__native_client__)
	cfg->native_code = code_dest;
#endif
	mono_profiler_code_buffer_new (cfg->native_code, cfg->code_len, MONO_PROFILER_CODE_BUFFER_METHOD, cfg->method);
	
	mono_arch_flush_icache (cfg->native_code, cfg->code_len);

	mono_debug_close_method (cfg);

#ifdef MONO_ARCH_HAVE_UNWIND_TABLE
	mono_arch_unwindinfo_install_unwind_info (&cfg->arch.unwindinfo, cfg->native_code, cfg->code_len);
#endif
}

static void
compute_reachable (MonoBasicBlock *bb)
{
	int i;

	if (!(bb->flags & BB_VISITED)) {
		bb->flags |= BB_VISITED;
		for (i = 0; i < bb->out_count; ++i)
			compute_reachable (bb->out_bb [i]);
	}
}

static void
mono_handle_out_of_line_bblock (MonoCompile *cfg)
{
	MonoBasicBlock *bb;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (bb->next_bb && bb->next_bb->out_of_line && bb->last_ins && !MONO_IS_BRANCH_OP (bb->last_ins)) {
			MonoInst *ins;
			MONO_INST_NEW (cfg, ins, OP_BR);
			MONO_ADD_INS (bb, ins);
			ins->inst_target_bb = bb->next_bb;
		}
	}
}

#endif /* #ifndef DISABLE_JIT */

static MonoJitInfo*
create_jit_info_for_trampoline (MonoMethod *wrapper, MonoTrampInfo *info)
{
	MonoDomain *domain = mono_get_root_domain ();
	MonoJitInfo *jinfo;
	guint8 *uw_info;
	guint32 info_len;

	if (info->uw_info) {
		uw_info = info->uw_info;
		info_len = info->uw_info_len;
	} else {
		uw_info = mono_unwind_ops_encode (info->unwind_ops, &info_len);
	}

	jinfo = mono_domain_alloc0 (domain, MONO_SIZEOF_JIT_INFO);
	jinfo->d.method = wrapper;
	jinfo->code_start = info->code;
	jinfo->code_size = info->code_size;
	jinfo->unwind_info = mono_cache_unwind_info (uw_info, info_len);

	if (!info->uw_info)
		g_free (uw_info);

	return jinfo;
}

#ifndef DISABLE_JIT

static MonoJitInfo*
create_jit_info (MonoCompile *cfg, MonoMethod *method_to_compile)
{
	GSList *tmp;
	MonoMethodHeader *header;
	MonoJitInfo *jinfo;
	MonoJitInfoFlags flags = JIT_INFO_NONE;
	int num_clauses, num_holes = 0;
	guint32 stack_size = 0;

	g_assert (method_to_compile == cfg->method);
	header = cfg->header;

	if (cfg->generic_sharing_context)
		flags |= JIT_INFO_HAS_GENERIC_JIT_INFO;

	if (cfg->arch_eh_jit_info) {
		MonoJitArgumentInfo *arg_info;
		MonoMethodSignature *sig = mono_method_signature (cfg->method_to_register);

		/*
		 * This cannot be computed during stack walking, as
		 * mono_arch_get_argument_info () is not signal safe.
		 */
		arg_info = g_newa (MonoJitArgumentInfo, sig->param_count + 1);
		stack_size = mono_arch_get_argument_info (cfg->generic_sharing_context, sig, sig->param_count, arg_info);

		if (stack_size)
			flags |= JIT_INFO_HAS_ARCH_EH_INFO;
	}

	if (cfg->has_unwind_info_for_epilog && !(flags & JIT_INFO_HAS_ARCH_EH_INFO))
		flags |= JIT_INFO_HAS_ARCH_EH_INFO;
		
	if (cfg->try_block_holes) {
		for (tmp = cfg->try_block_holes; tmp; tmp = tmp->next) {
			TryBlockHole *hole = tmp->data;
			MonoExceptionClause *ec = hole->clause;
			int hole_end = hole->basic_block->native_offset + hole->basic_block->native_length;
			MonoBasicBlock *clause_last_bb = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			g_assert (clause_last_bb);

			/* Holes at the end of a try region can be represented by simply reducing the size of the block itself.*/
			if (clause_last_bb->native_offset != hole_end)
				++num_holes;
		}
		if (num_holes)
			flags |= JIT_INFO_HAS_TRY_BLOCK_HOLES;
		if (G_UNLIKELY (cfg->verbose_level >= 4))
			printf ("Number of try block holes %d\n", num_holes);
	}

	if (mono_security_method_has_declsec (cfg->method_to_register))
		flags |= JIT_INFO_HAS_ARCH_EH_INFO;

	if (COMPILE_LLVM (cfg))
		num_clauses = cfg->llvm_ex_info_len;
	else
		num_clauses = header->num_clauses;

	if (cfg->method->dynamic)
		jinfo = g_malloc0 (mono_jit_info_size (flags, num_clauses, num_holes));
	else
		jinfo = mono_domain_alloc0 (cfg->domain, mono_jit_info_size (flags, num_clauses, num_holes));
	mono_jit_info_init (jinfo, cfg->method_to_register, cfg->native_code, cfg->code_len, flags, num_clauses, num_holes);
	jinfo->domain_neutral = (cfg->opt & MONO_OPT_SHARED) != 0;

	if (COMPILE_LLVM (cfg))
		jinfo->from_llvm = TRUE;

	if (cfg->generic_sharing_context) {
		MonoInst *inst;
		MonoGenericJitInfo *gi;
		GSList *loclist = NULL;

		gi = mono_jit_info_get_generic_jit_info (jinfo);
		g_assert (gi);

		if (cfg->method->dynamic)
			gi->generic_sharing_context = g_new0 (MonoGenericSharingContext, 1);
		else
			gi->generic_sharing_context = mono_domain_alloc0 (cfg->domain, sizeof (MonoGenericSharingContext));
		mini_init_gsctx (cfg->method->dynamic ? NULL : cfg->domain, NULL, cfg->gsctx_context, gi->generic_sharing_context);

		if ((method_to_compile->flags & METHOD_ATTRIBUTE_STATIC) ||
				mini_method_get_context (method_to_compile)->method_inst ||
				method_to_compile->klass->valuetype) {
			g_assert (cfg->rgctx_var);
		}

		gi->has_this = 1;

		if ((method_to_compile->flags & METHOD_ATTRIBUTE_STATIC) ||
				mini_method_get_context (method_to_compile)->method_inst ||
				method_to_compile->klass->valuetype) {
			inst = cfg->rgctx_var;
			if (!COMPILE_LLVM (cfg))
				g_assert (inst->opcode == OP_REGOFFSET);
			loclist = cfg->rgctx_loclist;
		} else {
			inst = cfg->args [0];
			loclist = cfg->this_loclist;
		}

		if (loclist) {
			/* Needed to handle async exceptions */
			GSList *l;
			int i;

			gi->nlocs = g_slist_length (loclist);
			if (cfg->method->dynamic)
				gi->locations = g_malloc0 (gi->nlocs * sizeof (MonoDwarfLocListEntry));
			else
				gi->locations = mono_domain_alloc0 (cfg->domain, gi->nlocs * sizeof (MonoDwarfLocListEntry));
			i = 0;
			for (l = loclist; l; l = l->next) {
				memcpy (&(gi->locations [i]), l->data, sizeof (MonoDwarfLocListEntry));
				i ++;
			}
		}

		if (COMPILE_LLVM (cfg)) {
			g_assert (cfg->llvm_this_reg != -1);
			gi->this_in_reg = 0;
			gi->this_reg = cfg->llvm_this_reg;
			gi->this_offset = cfg->llvm_this_offset;
		} else if (inst->opcode == OP_REGVAR) {
			gi->this_in_reg = 1;
			gi->this_reg = inst->dreg;
		} else {
			g_assert (inst->opcode == OP_REGOFFSET);
#ifdef TARGET_X86
			g_assert (inst->inst_basereg == X86_EBP);
#elif defined(TARGET_AMD64)
			g_assert (inst->inst_basereg == X86_EBP || inst->inst_basereg == X86_ESP);
#endif
			g_assert (inst->inst_offset >= G_MININT32 && inst->inst_offset <= G_MAXINT32);

			gi->this_in_reg = 0;
			gi->this_reg = inst->inst_basereg;
			gi->this_offset = inst->inst_offset;
		}
	}

	if (num_holes) {
		MonoTryBlockHoleTableJitInfo *table;
		int i;

		table = mono_jit_info_get_try_block_hole_table_info (jinfo);
		table->num_holes = (guint16)num_holes;
		i = 0;
		for (tmp = cfg->try_block_holes; tmp; tmp = tmp->next) {
			guint32 start_bb_offset;
			MonoTryBlockHoleJitInfo *hole;
			TryBlockHole *hole_data = tmp->data;
			MonoExceptionClause *ec = hole_data->clause;
			int hole_end = hole_data->basic_block->native_offset + hole_data->basic_block->native_length;
			MonoBasicBlock *clause_last_bb = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			g_assert (clause_last_bb);

			/* Holes at the end of a try region can be represented by simply reducing the size of the block itself.*/
			if (clause_last_bb->native_offset == hole_end)
				continue;

			start_bb_offset = hole_data->start_offset - hole_data->basic_block->native_offset;
			hole = &table->holes [i++];
			hole->clause = hole_data->clause - &header->clauses [0];
			hole->offset = (guint32)hole_data->start_offset;
			hole->length = (guint16)(hole_data->basic_block->native_length - start_bb_offset);

			if (G_UNLIKELY (cfg->verbose_level >= 4))
				printf ("\tTry block hole at eh clause %d offset %x length %x\n", hole->clause, hole->offset, hole->length);
		}
		g_assert (i == num_holes);
	}

	if (jinfo->has_arch_eh_info) {
		MonoArchEHJitInfo *info;

		info = mono_jit_info_get_arch_eh_info (jinfo);

		info->stack_size = stack_size;
	}

	if (COMPILE_LLVM (cfg)) {
		if (num_clauses)
			memcpy (&jinfo->clauses [0], &cfg->llvm_ex_info [0], num_clauses * sizeof (MonoJitExceptionInfo));
	} else if (header->num_clauses) {
		int i;

		for (i = 0; i < header->num_clauses; i++) {
			MonoExceptionClause *ec = &header->clauses [i];
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			MonoBasicBlock *tblock;
			MonoInst *exvar, *spvar;

			ei->flags = ec->flags;

			/*
			 * The spvars are needed by mono_arch_install_handler_block_guard ().
			 */
			if (ei->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
				int region;

				region = ((i + 1) << 8) | MONO_REGION_FINALLY | ec->flags;
				spvar = mono_find_spvar_for_region (cfg, region);
				g_assert (spvar);
				ei->exvar_offset = spvar->inst_offset;
			} else {
				exvar = mono_find_exvar_for_offset (cfg, ec->handler_offset);
				ei->exvar_offset = exvar ? exvar->inst_offset : 0;
			}

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				tblock = cfg->cil_offset_to_bb [ec->data.filter_offset];
				g_assert (tblock);
				ei->data.filter = cfg->native_code + tblock->native_offset;
			} else {
				ei->data.catch_class = ec->data.catch_class;
			}

			tblock = cfg->cil_offset_to_bb [ec->try_offset];
			g_assert (tblock);
			g_assert (tblock->native_offset);
			ei->try_start = cfg->native_code + tblock->native_offset;
			if (tblock->extend_try_block) {
				/*
				 * Extend the try block backwards to include parts of the previous call
				 * instruction.
				 */
				ei->try_start = (guint8*)ei->try_start - MONO_ARCH_MONITOR_ENTER_ADJUSTMENT;
			}
			tblock = cfg->cil_offset_to_bb [ec->try_offset + ec->try_len];
			g_assert (tblock);
			if (!tblock->native_offset) {
				int j, end;
				for (j = ec->try_offset + ec->try_len, end = ec->try_offset; j >= end; --j) {
					MonoBasicBlock *bb = cfg->cil_offset_to_bb [j];
					if (bb && bb->native_offset) {
						tblock = bb;
						break;
					}
				}
			}
			ei->try_end = cfg->native_code + tblock->native_offset;
			g_assert (tblock->native_offset);
			tblock = cfg->cil_offset_to_bb [ec->handler_offset];
			g_assert (tblock);
			ei->handler_start = cfg->native_code + tblock->native_offset;

			for (tmp = cfg->try_block_holes; tmp; tmp = tmp->next) {
				TryBlockHole *hole = tmp->data;
				gpointer hole_end = cfg->native_code + (hole->basic_block->native_offset + hole->basic_block->native_length);
				if (hole->clause == ec && hole_end == ei->try_end) {
					if (G_UNLIKELY (cfg->verbose_level >= 4))
						printf ("\tShortening try block %d from %x to %x\n", i, (int)((guint8*)ei->try_end - cfg->native_code), hole->start_offset);

					ei->try_end = cfg->native_code + hole->start_offset;
					break;
				}
			}

			if (ec->flags == MONO_EXCEPTION_CLAUSE_FINALLY) {
				int end_offset;
				if (ec->handler_offset + ec->handler_len < header->code_size) {
					tblock = cfg->cil_offset_to_bb [ec->handler_offset + ec->handler_len];
					if (tblock->native_offset) {
						end_offset = tblock->native_offset;
					} else {
						int j, end;

						for (j = ec->handler_offset + ec->handler_len, end = ec->handler_offset; j >= end; --j) {
							MonoBasicBlock *bb = cfg->cil_offset_to_bb [j];
							if (bb && bb->native_offset) {
								tblock = bb;
								break;
							}
						}
						end_offset = tblock->native_offset +  tblock->native_length;
					}
				} else {
					end_offset = cfg->epilog_begin;
				}
				ei->data.handler_end = cfg->native_code + end_offset;
			}
		}
	}

	if (G_UNLIKELY (cfg->verbose_level >= 4)) {
		int i;
		for (i = 0; i < jinfo->num_clauses; i++) {
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			int start = (guint8*)ei->try_start - cfg->native_code;
			int end = (guint8*)ei->try_end - cfg->native_code;
			int handler = (guint8*)ei->handler_start - cfg->native_code;
			int handler_end = (guint8*)ei->data.handler_end - cfg->native_code;

			printf ("JitInfo EH clause %d flags %x try %x-%x handler %x-%x\n", i, ei->flags, start, end, handler, handler_end);
		}
	}

	if (cfg->encoded_unwind_ops) {
		/* Generated by LLVM */
		jinfo->unwind_info = mono_cache_unwind_info (cfg->encoded_unwind_ops, cfg->encoded_unwind_ops_len);
		g_free (cfg->encoded_unwind_ops);
	} else if (cfg->unwind_ops) {
		guint32 info_len;
		guint8 *unwind_info = mono_unwind_ops_encode (cfg->unwind_ops, &info_len);
		guint32 unwind_desc;

		unwind_desc = mono_cache_unwind_info (unwind_info, info_len);

		if (cfg->has_unwind_info_for_epilog) {
			MonoArchEHJitInfo *info;

			info = mono_jit_info_get_arch_eh_info (jinfo);
			g_assert (info);
			info->epilog_size = cfg->code_len - cfg->epilog_begin;
		}
		jinfo->unwind_info = unwind_desc;
		g_free (unwind_info);
	} else {
		jinfo->unwind_info = cfg->used_int_regs;
	}

	return jinfo;
}
#endif

static MonoType*
get_gsharedvt_type (MonoType *t)
{
	MonoGenericParam *par = t->data.generic_param;
	MonoGenericParam *copy;
	MonoType *res;
	MonoImage *image = NULL;

	/* 
	 * Create an anonymous gparam with a different serial so normal gshared and gsharedvt methods have
	 * a different instantiation.
	 */
	g_assert (mono_generic_param_info (par));
	if (par->owner) {
		image = par->owner->image;

		mono_image_lock (image);
		if (!image->gsharedvt_types)
			image->gsharedvt_types = g_hash_table_new (NULL, NULL);
		res = g_hash_table_lookup (image->gsharedvt_types, par);
		mono_image_unlock (image);
		if (res)
			return res;
		copy = mono_image_alloc0 (image, sizeof (MonoGenericParamFull));
		memcpy (copy, par, sizeof (MonoGenericParamFull));
	} else {
		copy = g_memdup (par, sizeof (MonoGenericParamFull));
	}
	copy->owner = NULL;
	// FIXME:
	copy->image = mono_defaults.corlib;
	copy->serial = 1;
	res = mono_metadata_type_dup (NULL, t);
	res->data.generic_param = copy;

	if (par->owner) {
		mono_image_lock (image);
		/* Duplicates are ok */
		g_hash_table_insert (image->gsharedvt_types, par, res);
		mono_image_unlock (image);
	}

	return res;
}

static gboolean
is_gsharedvt_type (MonoType *t)
{
	return (t->type == MONO_TYPE_VAR || t->type == MONO_TYPE_MVAR) && t->data.generic_param->serial == 1;
}

/* Return whenever METHOD is a gsharedvt method */
static gboolean
is_gsharedvt_method (MonoMethod *method)
{
	MonoGenericContext *context;
	MonoGenericInst *inst;
	int i;

	if (!method->is_inflated)
		return FALSE;
	context = mono_method_get_context (method);
	inst = context->class_inst;
	if (inst) {
		for (i = 0; i < inst->type_argc; ++i)
			if (is_gsharedvt_type (inst->type_argv [i]))
				return TRUE;
	}
	inst = context->method_inst;
	if (inst) {
		for (i = 0; i < inst->type_argc; ++i)
			if (is_gsharedvt_type (inst->type_argv [i]))
				return TRUE;
	}
	return FALSE;
}

static gboolean
is_open_method (MonoMethod *method)
{
	MonoGenericContext *context;

	if (!method->is_inflated)
		return FALSE;
	context = mono_method_get_context (method);
	if (context->class_inst && context->class_inst->is_open)
		return TRUE;
	if (context->method_inst && context->method_inst->is_open)
		return TRUE;
	return FALSE;
}

static MonoGenericInst*
get_shared_inst (MonoGenericInst *inst, MonoGenericInst *shared_inst, MonoGenericContainer *container, gboolean all_vt, gboolean gsharedvt)
{
	MonoGenericInst *res;
	MonoType **type_argv;
	int i;

	type_argv = g_new0 (MonoType*, inst->type_argc);
	for (i = 0; i < inst->type_argc; ++i) {
		if (!all_vt && (MONO_TYPE_IS_REFERENCE (inst->type_argv [i]) || inst->type_argv [i]->type == MONO_TYPE_VAR || inst->type_argv [i]->type == MONO_TYPE_MVAR)) {
			type_argv [i] = shared_inst->type_argv [i];
		} else if (all_vt) {
			type_argv [i] = get_gsharedvt_type (shared_inst->type_argv [i]);
		} else if (gsharedvt) {
			type_argv [i] = get_gsharedvt_type (shared_inst->type_argv [i]);
		} else {
			type_argv [i] = inst->type_argv [i];
		}
	}

	res = mono_metadata_get_generic_inst (inst->type_argc, type_argv);
	g_free (type_argv);
	return res;
}

/*
 * mini_get_shared_method_full:
 *
 *   Return the method which is actually compiled/registered when doing generic sharing.
 * If ALL_VT is true, return the shared method belonging to an all-vtype instantiation.
 * If IS_GSHAREDVT is true, treat METHOD as a gsharedvt method even if it fails some constraints.
 * METHOD can be a non-inflated generic method.
 */
MonoMethod*
mini_get_shared_method_full (MonoMethod *method, gboolean all_vt, gboolean is_gsharedvt)
{
	MonoGenericContext shared_context;
	MonoMethod *declaring_method, *res;
	gboolean partial = FALSE;
	gboolean gsharedvt = FALSE;
	MonoGenericContainer *class_container, *method_container = NULL;

	if (method->is_generic || (method->klass->generic_container && !method->is_inflated)) {
		declaring_method = method;
	} else {
		declaring_method = mono_method_get_declaring_generic_method (method);
	}

	if (declaring_method->is_generic)
		shared_context = mono_method_get_generic_container (declaring_method)->context;
	else
		shared_context = declaring_method->klass->generic_container->context;

	/* Handle gsharedvt/partial sharing */
	if ((method != declaring_method && method->is_inflated && !mono_method_is_generic_sharable_full (method, FALSE, FALSE, TRUE)) ||
		is_gsharedvt || mini_is_gsharedvt_sharable_method (method)) {
		MonoGenericContext *context = mono_method_get_context (method);
		MonoGenericInst *inst;

		partial = mono_method_is_generic_sharable_full (method, FALSE, TRUE, FALSE);

		gsharedvt = is_gsharedvt || (!partial && mini_is_gsharedvt_sharable_method (method));

		class_container = declaring_method->klass->generic_container;
		method_container = mono_method_get_generic_container (declaring_method);

		/* 
		 * Create the shared context by replacing the ref type arguments with
		 * type parameters, and keeping the rest.
		 */
		if (context)
			inst = context->class_inst;
		else
			inst = shared_context.class_inst;
		if (inst)
			shared_context.class_inst = get_shared_inst (inst, shared_context.class_inst, class_container, all_vt, gsharedvt);

		if (context)
			inst = context->method_inst;
		else
			inst = shared_context.method_inst;
		if (inst)
			shared_context.method_inst = get_shared_inst (inst, shared_context.method_inst, method_container, all_vt, gsharedvt);

		partial = TRUE;
	}

    res = mono_class_inflate_generic_method (declaring_method, &shared_context);
	if (!partial) {
		/* The result should be an inflated method whose parent is not inflated */
		g_assert (!res->klass->is_inflated);
	}
	return res;
}

MonoMethod*
mini_get_shared_method (MonoMethod *method)
{
	return mini_get_shared_method_full (method, FALSE, FALSE);
}

void
mini_init_gsctx (MonoDomain *domain, MonoMemPool *mp, MonoGenericContext *context, MonoGenericSharingContext *gsctx)
{
	MonoGenericInst *inst;
	int i;

	memset (gsctx, 0, sizeof (MonoGenericSharingContext));

	if (context && context->class_inst) {
		inst = context->class_inst;
		if (domain)
			gsctx->var_is_vt = mono_domain_alloc0 (domain, sizeof (gboolean) * inst->type_argc);
		else if (mp)
			gsctx->var_is_vt = mono_mempool_alloc0 (mp, sizeof (gboolean) * inst->type_argc);
		else
			gsctx->var_is_vt = g_new0 (gboolean, inst->type_argc);

		for (i = 0; i < inst->type_argc; ++i) {
			MonoType *type = inst->type_argv [i];

			if ((type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) && type->data.generic_param->serial == 1)
				gsctx->var_is_vt [i] = TRUE;
		}
	}
	if (context && context->method_inst) {
		inst = context->method_inst;
		if (domain)
			gsctx->mvar_is_vt = mono_domain_alloc0 (domain, sizeof (gboolean) * inst->type_argc);
		else if (mp)
			gsctx->mvar_is_vt = mono_mempool_alloc0 (mp, sizeof (gboolean) * inst->type_argc);
		else
			gsctx->mvar_is_vt = g_new0 (gboolean, inst->type_argc);

		for (i = 0; i < inst->type_argc; ++i) {
			MonoType *type = inst->type_argv [i];

			if ((type->type == MONO_TYPE_VAR || type->type == MONO_TYPE_MVAR) && type->data.generic_param->serial == 1)
				gsctx->mvar_is_vt [i] = TRUE;
		}
	}
}

#ifndef DISABLE_JIT
/*
 * mini_method_compile:
 * @method: the method to compile
 * @opts: the optimization flags to use
 * @domain: the domain where the method will be compiled in
 * @flags: compilation flags
 * @parts: debug flag
 *
 * Returns: a MonoCompile* pointer. Caller must check the exception_type
 * field in the returned struct to see if compilation succeded.
 */
MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, JitFlags flags, int parts)
{
	MonoMethodHeader *header;
	MonoMethodSignature *sig;
	MonoError err;
	guint8 *ip;
	MonoCompile *cfg;
	int dfn, i, code_size_ratio;
#ifndef DISABLE_SSA
	gboolean deadce_has_run = FALSE;
#endif
	gboolean try_generic_shared, try_llvm = FALSE;
	MonoMethod *method_to_compile, *method_to_register;
	gboolean method_is_gshared = FALSE;
	gboolean run_cctors = (flags & JIT_FLAG_RUN_CCTORS) ? 1 : 0;
	gboolean compile_aot = (flags & JIT_FLAG_AOT) ? 1 : 0;
	gboolean full_aot = (flags & JIT_FLAG_FULL_AOT) ? 1 : 0;
#ifdef ENABLE_LLVM
	gboolean llvm = (flags & JIT_FLAG_LLVM) ? 1 : 0;
#endif
	static gboolean verbose_method_inited;
	static const char *verbose_method_name;

	InterlockedIncrement (&mono_jit_stats.methods_compiled);
	if (mono_profiler_get_events () & MONO_PROFILE_JIT_COMPILATION)
		mono_profiler_method_jit (method);
	if (MONO_METHOD_COMPILE_BEGIN_ENABLED ())
		MONO_PROBE_METHOD_COMPILE_BEGIN (method);

	if (compile_aot)
		/* 
		 * We might get passed the original generic method definition or
		 * instances with type parameters.
		 * FIXME: Remove the method->klass->generic_class limitation.
		 */
		try_generic_shared = mono_class_generic_sharing_enabled (method->klass) &&
			(opts & MONO_OPT_GSHARED) && ((method->is_generic || method->klass->generic_container) || (!method->klass->generic_class && mono_method_is_generic_sharable_full (method, TRUE, FALSE, FALSE)));
	else
		try_generic_shared = mono_class_generic_sharing_enabled (method->klass) &&
			(opts & MONO_OPT_GSHARED) && mono_method_is_generic_sharable (method, FALSE);

	if (opts & MONO_OPT_GSHARED) {
		if (try_generic_shared)
			mono_stats.generics_sharable_methods++;
		else if (mono_method_is_generic_impl (method))
			mono_stats.generics_unsharable_methods++;
	}

	if (mini_is_gsharedvt_sharable_method (method)) {
		if (!mono_debug_count ())
			try_generic_shared = FALSE;
		if (compile_aot)
			try_generic_shared = FALSE;
	}

	if (is_gsharedvt_method (method) || (compile_aot && is_open_method (method))) {
		/* We are AOTing a gshared method directly */
		method_is_gshared = TRUE;
		g_assert (compile_aot);
		try_generic_shared = TRUE;
	}

#ifdef ENABLE_LLVM
	try_llvm = mono_use_llvm || llvm;
#endif

 restart_compile:
	if (method_is_gshared) {
		method_to_compile = method;
	} else {
		if (try_generic_shared) {
			method_to_compile = mini_get_shared_method (method);
			g_assert (method_to_compile);
		} else {
			method_to_compile = method;
		}
	}

	cfg = g_new0 (MonoCompile, 1);
	cfg->method = method_to_compile;
	cfg->header = mono_method_get_header (cfg->method);
	cfg->mempool = mono_mempool_new ();
	cfg->opt = opts;
	cfg->prof_options = mono_profiler_get_events ();
	cfg->run_cctors = run_cctors;
	cfg->domain = domain;
	cfg->verbose_level = mini_verbose;
	cfg->compile_aot = compile_aot;
	cfg->full_aot = full_aot;
	cfg->skip_visibility = method->skip_visibility;
	cfg->orig_method = method;
	cfg->gen_seq_points = debug_options.gen_seq_points;
	cfg->explicit_null_checks = debug_options.explicit_null_checks;
	cfg->soft_breakpoints = debug_options.soft_breakpoints;
	cfg->check_pinvoke_callconv = debug_options.check_pinvoke_callconv;
	if (try_generic_shared)
		cfg->generic_sharing_context = (MonoGenericSharingContext*)&cfg->gsctx;
	cfg->compile_llvm = try_llvm;
	cfg->token_info_hash = g_hash_table_new (NULL, NULL);

	if (cfg->gen_seq_points)
		cfg->seq_points = g_ptr_array_new ();
	mono_error_init (&cfg->error);

	if (cfg->compile_aot && !try_generic_shared && (method->is_generic || method->klass->generic_container || method_is_gshared)) {
		cfg->exception_type = MONO_EXCEPTION_GENERIC_SHARING_FAILED;
		return cfg;
	}

	if (cfg->generic_sharing_context && (mini_is_gsharedvt_sharable_method (method) || method_is_gshared)) {
		MonoMethodInflated *inflated;
		MonoGenericContext *context;

		if (method_is_gshared) {
			g_assert (method->is_inflated);
			inflated = (MonoMethodInflated*)method;
			context = &inflated->context;

			/* We are compiling a gsharedvt method directly */
			g_assert (compile_aot);
		} else {
			g_assert (method_to_compile->is_inflated);
			inflated = (MonoMethodInflated*)method_to_compile;
			context = &inflated->context;
		}

		mini_init_gsctx (NULL, cfg->mempool, context, &cfg->gsctx);
		cfg->gsctx_context = context;

		cfg->gsharedvt = TRUE;
		// FIXME:
		cfg->disable_llvm = TRUE;
	}

	if (cfg->generic_sharing_context) {
		method_to_register = method_to_compile;
		cfg->gshared = TRUE;
	} else {
		g_assert (method == method_to_compile);
		method_to_register = method;
	}
	cfg->method_to_register = method_to_register;

	mono_error_init (&err);
	sig = mono_method_signature_checked (cfg->method, &err);	
	if (!sig) {
		cfg->exception_type = MONO_EXCEPTION_TYPE_LOAD;
		cfg->exception_message = g_strdup (mono_error_get_message (&err));
		mono_error_cleanup (&err);
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		return cfg;
	}

	header = cfg->header;
	if (!header) {
		MonoLoaderError *error;

		if ((error = mono_loader_get_last_error ())) {
			cfg->exception_type = error->exception_type;
		} else {
			cfg->exception_type = MONO_EXCEPTION_INVALID_PROGRAM;
			cfg->exception_message = g_strdup_printf ("Missing or incorrect header for method %s", cfg->method->name);
		}
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		return cfg;
	}

#ifdef ENABLE_LLVM
	{
		static gboolean inited;

		if (!inited)
			inited = TRUE;

		/* 
		 * Check for methods which cannot be compiled by LLVM early, to avoid
		 * the extra compilation pass.
		 */
		if (COMPILE_LLVM (cfg)) {
			mono_llvm_check_method_supported (cfg);
			if (cfg->disable_llvm) {
				if (cfg->verbose_level >= 1) {
					//nm = mono_method_full_name (cfg->method, TRUE);
					printf ("LLVM failed for '%s': %s\n", method->name, cfg->exception_message);
					//g_free (nm);
				}
				mono_destroy_compile (cfg);
				try_llvm = FALSE;
				goto restart_compile;
			}
		}
	}
#endif

	/* The debugger has no liveness information, so avoid sharing registers/stack slots */
	if (debug_options.mdb_optimizations) {
		cfg->disable_reuse_registers = TRUE;
		cfg->disable_reuse_stack_slots = TRUE;
		/* 
		 * This decreases the change the debugger will read registers/stack slots which are
		 * not yet initialized.
		 */
		cfg->disable_initlocals_opt = TRUE;

		cfg->extend_live_ranges = TRUE;

		/* Temporarily disable this when running in the debugger until we have support
		 * for this in the debugger. */
		/* This is no longer needed with sdb */
		//cfg->disable_omit_fp = TRUE;

		/* The debugger needs all locals to be on the stack or in a global register */
		cfg->disable_vreg_to_lvreg = TRUE;

		/* Don't remove unused variables when running inside the debugger since the user
		 * may still want to view them. */
		cfg->disable_deadce_vars = TRUE;

		// cfg->opt |= MONO_OPT_SHARED;
		cfg->opt &= ~MONO_OPT_DEADCE;
		cfg->opt &= ~MONO_OPT_INLINE;
		cfg->opt &= ~MONO_OPT_COPYPROP;
		cfg->opt &= ~MONO_OPT_CONSPROP;
		/* This is no longer needed with sdb */
		//cfg->opt &= ~MONO_OPT_GSHARED;

		/* This is needed for the soft debugger, which doesn't like code after the epilog */
		cfg->disable_out_of_line_bblocks = TRUE;
	}

	if (mono_using_xdebug) {
		/* 
		 * Make each variable use its own register/stack slot and extend 
		 * their liveness to cover the whole method, making them displayable
		 * in gdb even after they are dead.
		 */
		cfg->disable_reuse_registers = TRUE;
		cfg->disable_reuse_stack_slots = TRUE;
		cfg->extend_live_ranges = TRUE;
		cfg->compute_precise_live_ranges = TRUE;
	}

	mini_gc_init_cfg (cfg);

	if (COMPILE_LLVM (cfg)) {
		cfg->opt |= MONO_OPT_ABCREM;
	}

	if (!verbose_method_inited) {
		verbose_method_name = g_getenv ("MONO_VERBOSE_METHOD");
		verbose_method_inited = TRUE;
	}
	if (verbose_method_name) {
		const char *name = verbose_method_name;

		if ((strchr (name, '.') > name) || strchr (name, ':')) {
			MonoMethodDesc *desc;
			
			desc = mono_method_desc_new (name, TRUE);
			if (mono_method_desc_full_match (desc, cfg->method)) {
				cfg->verbose_level = 4;
			}
			mono_method_desc_free (desc);
		} else {
			if (strcmp (cfg->method->name, name) == 0)
				cfg->verbose_level = 4;
		}
	}

	ip = (guint8 *)header->code;

	cfg->intvars = mono_mempool_alloc0 (cfg->mempool, sizeof (guint16) * STACK_MAX * header->max_stack);

	if (cfg->verbose_level > 0) {
		char *method_name;

		method_name = mono_method_full_name (method, TRUE);
		g_print ("converting %s%s%smethod %s\n", COMPILE_LLVM (cfg) ? "llvm " : "", cfg->gsharedvt ? "gsharedvt " : "", (cfg->generic_sharing_context && !cfg->gsharedvt) ? "gshared " : "", method_name);
		/*
		if (COMPILE_LLVM (cfg))
			g_print ("converting llvm method %s\n", method_name = mono_method_full_name (method, TRUE));
		else if (cfg->gsharedvt)
			g_print ("converting gsharedvt method %s\n", method_name = mono_method_full_name (method_to_compile, TRUE));
		else if (cfg->generic_sharing_context)
			g_print ("converting shared method %s\n", method_name = mono_method_full_name (method_to_compile, TRUE));
		else
			g_print ("converting method %s\n", method_name = mono_method_full_name (method, TRUE));
		*/
		g_free (method_name);
	}

	if (cfg->opt & (MONO_OPT_ABCREM | MONO_OPT_SSAPRE))
		cfg->opt |= MONO_OPT_SSA;

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

		/*
		if (g_getenv ("COUNT2")) {
			cfg->globalra = TRUE;
			if (count == atoi (g_getenv ("COUNT2")))
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
			if (count > atoi (g_getenv ("COUNT2")))
				cfg->globalra = FALSE;
		}
		*/
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

	if (header->code_size > 5000)
		// FIXME:
		/* Too large bblocks could overflow the ins positions */
		cfg->globalra = FALSE;

	cfg->rs = mono_regstate_new ();
	if (cfg->globalra)
		cfg->rs->next_vreg = MONO_MAX_IREGS + MONO_MAX_FREGS;
	cfg->next_vreg = cfg->rs->next_vreg;

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

	/*We must verify the method before doing any IR generation as mono_compile_create_vars can assert.*/
	if (mono_compile_is_broken (cfg, cfg->method, TRUE)) {
		if (mini_get_debug_options ()->break_on_unverified)
			G_BREAKPOINT ();
		return cfg;
	}

	/*
	 * create MonoInst* which represents arguments and local variables
	 */
	mono_compile_create_vars (cfg);

	/* SSAPRE is not supported on linear IR */
	cfg->opt &= ~MONO_OPT_SSAPRE;

	i = mono_method_to_ir (cfg, method_to_compile, NULL, NULL, NULL, NULL, 0, FALSE);

	if (i < 0) {
		if (try_generic_shared && cfg->exception_type == MONO_EXCEPTION_GENERIC_SHARING_FAILED) {
			if (compile_aot) {
				if (MONO_METHOD_COMPILE_END_ENABLED ())
					MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
				return cfg;
			}
			mono_destroy_compile (cfg);
			try_generic_shared = FALSE;
			goto restart_compile;
		}
		g_assert (cfg->exception_type != MONO_EXCEPTION_GENERIC_SHARING_FAILED);

		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, FALSE);
		/* cfg contains the details of the failure, so let the caller cleanup */
		return cfg;
	}

	cfg->stat_basic_blocks += cfg->num_bblocks;

	if (COMPILE_LLVM (cfg)) {
		MonoInst *ins;

		/* The IR has to be in SSA form for LLVM */
		cfg->opt |= MONO_OPT_SSA;

		// FIXME:
		if (cfg->ret) {
			// Allow SSA on the result value
			cfg->ret->flags &= ~MONO_INST_VOLATILE;

			// Add an explicit return instruction referencing the return value
			MONO_INST_NEW (cfg, ins, OP_SETRET);
			ins->sreg1 = cfg->ret->dreg;

			MONO_ADD_INS (cfg->bb_exit, ins);
		}

		cfg->opt &= ~MONO_OPT_LINEARS;

		/* FIXME: */
		cfg->opt &= ~MONO_OPT_BRANCH;
	}

	/* todo: remove code when we have verified that the liveness for try/catch blocks
	 * works perfectly 
	 */
	/* 
	 * Currently, this can't be commented out since exception blocks are not
	 * processed during liveness analysis.
	 * It is also needed, because otherwise the local optimization passes would
	 * delete assignments in cases like this:
	 * r1 <- 1
	 * <something which throws>
	 * r1 <- 2
	 * This also allows SSA to be run on methods containing exception clauses, since
	 * SSA will ignore variables marked VOLATILE.
	 */
	mono_liveness_handle_exception_clauses (cfg);

	mono_handle_out_of_line_bblock (cfg);

	/*g_print ("numblocks = %d\n", cfg->num_bblocks);*/

	if (!COMPILE_LLVM (cfg))
		mono_decompose_long_opts (cfg);

	/* Should be done before branch opts */
	if (cfg->opt & (MONO_OPT_CONSPROP | MONO_OPT_COPYPROP))
		mono_local_cprop (cfg);

	if (cfg->opt & MONO_OPT_BRANCH)
		mono_optimize_branches (cfg);

	/* This must be done _before_ global reg alloc and _after_ decompose */
	mono_handle_global_vregs (cfg);
	if (cfg->opt & MONO_OPT_DEADCE)
		mono_local_deadce (cfg);
	if (cfg->opt & MONO_OPT_ALIAS_ANALYSIS)
		mono_local_alias_analysis (cfg);
	/* Disable this for LLVM to make the IR easier to handle */
	if (!COMPILE_LLVM (cfg))
		mono_if_conversion (cfg);

	if ((cfg->opt & MONO_OPT_SSAPRE) || cfg->globalra)
		mono_remove_critical_edges (cfg);

	/* Depth-first ordering on basic blocks */
	cfg->bblocks = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (cfg->num_bblocks + 1));

	cfg->max_block_num = cfg->num_bblocks;

	dfn = 0;
	df_visit (cfg->bb_entry, &dfn, cfg->bblocks);
	if (cfg->num_bblocks != dfn + 1) {
		MonoBasicBlock *bb;

		cfg->num_bblocks = dfn + 1;

		/* remove unreachable code, because the code in them may be 
		 * inconsistent  (access to dead variables for example) */
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			bb->flags &= ~BB_VISITED;
		compute_reachable (cfg->bb_entry);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			if (bb->flags & BB_EXCEPTION_HANDLER)
				compute_reachable (bb);
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			if (!(bb->flags & BB_VISITED)) {
				if (cfg->verbose_level > 1)
					g_print ("found unreachable code in BB%d\n", bb->block_num);
				bb->code = bb->last_ins = NULL;
				while (bb->out_count)
					mono_unlink_bblock (cfg, bb, bb->out_bb [0]);
			}
		}
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
			bb->flags &= ~BB_VISITED;
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
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	/*
	  if (header->num_clauses)
	  cfg->disable_ssa = TRUE;
	*/

//#define DEBUGSSA "logic_run"
//#define DEBUGSSA_CLASS "Tests"
#ifdef DEBUGSSA

	if (!cfg->disable_ssa) {
		mono_local_cprop (cfg);

#ifndef DISABLE_SSA
		mono_ssa_compute (cfg);
#endif
	}
#else 
	if (cfg->opt & MONO_OPT_SSA) {
		if (!(cfg->comp_done & MONO_COMP_SSA) && !cfg->disable_ssa) {
#ifndef DISABLE_SSA
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
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

	if ((cfg->opt & MONO_OPT_CONSPROP) || (cfg->opt & MONO_OPT_COPYPROP)) {
		if (cfg->comp_done & MONO_COMP_SSA && !COMPILE_LLVM (cfg)) {
#ifndef DISABLE_SSA
			mono_ssa_cprop (cfg);
#endif
		}
	}

#ifndef DISABLE_SSA
	if (cfg->comp_done & MONO_COMP_SSA && !COMPILE_LLVM (cfg)) {
		//mono_ssa_strength_reduction (cfg);

		if (cfg->opt & MONO_OPT_SSAPRE) {
			mono_perform_ssapre (cfg);
			//mono_local_cprop (cfg);
		}

		if (cfg->opt & MONO_OPT_DEADCE) {
			mono_ssa_deadce (cfg);
			deadce_has_run = TRUE;
		}

		if ((cfg->flags & (MONO_CFG_HAS_LDELEMA|MONO_CFG_HAS_CHECK_THIS)) && (cfg->opt & MONO_OPT_ABCREM))
			mono_perform_abc_removal (cfg);

		mono_ssa_remove (cfg);
		mono_local_cprop (cfg);
		mono_handle_global_vregs (cfg);
		if (cfg->opt & MONO_OPT_DEADCE)
			mono_local_deadce (cfg);

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

	if (cfg->comp_done & MONO_COMP_SSA && COMPILE_LLVM (cfg)) {
		mono_ssa_loop_invariant_code_motion (cfg);
		/* This removes MONO_INST_FAULT flags too so perform it unconditionally */
		if (cfg->opt & MONO_OPT_ABCREM)
			mono_perform_abc_removal (cfg);
	}

	/* after SSA removal */
	if (parts == 3) {
		if (MONO_METHOD_COMPILE_END_ENABLED ())
			MONO_PROBE_METHOD_COMPILE_END (method, TRUE);
		return cfg;
	}

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	if (COMPILE_SOFT_FLOAT (cfg))
		mono_decompose_soft_float (cfg);
#endif
	if (COMPILE_LLVM (cfg))
		mono_decompose_vtype_opts_llvm (cfg);
	else
		mono_decompose_vtype_opts (cfg);
	if (cfg->flags & MONO_CFG_HAS_ARRAY_ACCESS)
		mono_decompose_array_access_opts (cfg);

	if (cfg->got_var) {
#ifndef MONO_ARCH_GOT_REG
		GList *regs;
#endif
		int got_reg;

		g_assert (cfg->got_var_allocated);

		/* 
		 * Allways allocate the GOT var to a register, because keeping it
		 * in memory will increase the number of live temporaries in some
		 * code created by inssel.brg, leading to the well known spills+
		 * branches problem. Testcase: mcs crash in 
		 * System.MonoCustomAttrs:GetCustomAttributes.
		 */
#ifdef MONO_ARCH_GOT_REG
		got_reg = MONO_ARCH_GOT_REG;
#else
		regs = mono_arch_get_global_int_regs (cfg);
		g_assert (regs);
		got_reg = GPOINTER_TO_INT (regs->data);
		g_list_free (regs);
#endif
		cfg->got_var->opcode = OP_REGVAR;
		cfg->got_var->dreg = got_reg;
		cfg->used_int_regs |= 1LL << cfg->got_var->dreg;
	}

	/*
	 * Have to call this again to process variables added since the first call.
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
		GList *vars, *regs, *l;
		
		/* fixme: maybe we can avoid to compute livenesss here if already computed ? */
		cfg->comp_done &= ~MONO_COMP_LIVENESS;
		if (!(cfg->comp_done & MONO_COMP_LIVENESS))
			mono_analyze_liveness (cfg);

		if ((vars = mono_arch_get_allocatable_int_vars (cfg))) {
			regs = mono_arch_get_global_int_regs (cfg);
			/* Remove the reg reserved for holding the GOT address */
			if (cfg->got_var) {
				for (l = regs; l; l = l->next) {
					if (GPOINTER_TO_UINT (l->data) == cfg->got_var->dreg) {
						regs = g_list_delete_link (regs, l);
						break;
					}
				}
			}
			mono_linear_scan (cfg, vars, regs, &cfg->used_int_regs);
		}
	}

	//mono_print_code (cfg, "");

    //print_dfn (cfg);
	
	/* variables are allocated after decompose, since decompose could create temps */
	if (!cfg->globalra && !COMPILE_LLVM (cfg)) {
		mono_arch_allocate_vars (cfg);
		if (cfg->exception_type)
			return cfg;
	}

	{
		MonoBasicBlock *bb;
		gboolean need_local_opts;

		if (!cfg->globalra && !COMPILE_LLVM (cfg)) {
			mono_spill_global_vars (cfg, &need_local_opts);

			if (need_local_opts || cfg->compile_aot) {
				/* To optimize code created by spill_global_vars */
				mono_local_cprop (cfg);
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

	if (COMPILE_LLVM (cfg)) {
#ifdef ENABLE_LLVM
		char *nm;

		/* The IR has to be in SSA form for LLVM */
		if (!(cfg->comp_done & MONO_COMP_SSA)) {
			cfg->exception_message = g_strdup ("SSA disabled.");
			cfg->disable_llvm = TRUE;
		}

		if (cfg->flags & MONO_CFG_HAS_ARRAY_ACCESS)
			mono_decompose_array_access_opts (cfg);

		if (!cfg->disable_llvm)
			mono_llvm_emit_method (cfg);
		if (cfg->disable_llvm) {
			if (cfg->verbose_level >= 1) {
				//nm = mono_method_full_name (cfg->method, TRUE);
				printf ("LLVM failed for '%s': %s\n", method->name, cfg->exception_message);
				//g_free (nm);
			}
			mono_destroy_compile (cfg);
			try_llvm = FALSE;
			goto restart_compile;
		}

		if (cfg->verbose_level > 0 && !cfg->compile_aot) {
			nm = mono_method_full_name (cfg->method, TRUE);
			g_print ("LLVM Method %s emitted at %p to %p (code length %d) [%s]\n", 
					 nm, 
					 cfg->native_code, cfg->native_code + cfg->code_len, cfg->code_len, cfg->domain->friendly_name);
			g_free (nm);
		}
#endif
	} else {
		mono_codegen (cfg);
	}

	if (COMPILE_LLVM (cfg))
		InterlockedIncrement (&mono_jit_stats.methods_with_llvm);
	else
		InterlockedIncrement (&mono_jit_stats.methods_without_llvm);

	cfg->jit_info = create_jit_info (cfg, method_to_compile);

#ifdef MONO_ARCH_HAVE_LIVERANGE_OPS
	if (cfg->extend_live_ranges) {
		/* Extend live ranges to cover the whole method */
		for (i = 0; i < cfg->num_varinfo; ++i)
			MONO_VARINFO (cfg, i)->live_range_end = cfg->code_len;
	}
#endif

	if (!cfg->compile_aot)
		mono_save_xdebug_info (cfg);

	mini_gc_create_gc_map (cfg);
 
	mono_save_seq_point_info (cfg);

	if (cfg->verbose_level >= 2) {
		char *id =  mono_method_full_name (cfg->method, FALSE);
		mono_disassemble_code (cfg, cfg->native_code, cfg->code_len, id + 3);
		g_free (id);
	}

	if (!cfg->compile_aot) {
		mono_domain_lock (cfg->domain);
		mono_jit_info_table_add (cfg->domain, cfg->jit_info);

		if (cfg->method->dynamic)
			mono_dynamic_code_hash_lookup (cfg->domain, cfg->method)->ji = cfg->jit_info;
		mono_domain_unlock (cfg->domain);
	}

#if 0
	if (cfg->gsharedvt)
		printf ("GSHAREDVT: %s\n", mono_method_full_name (cfg->method, TRUE));
#endif

	/* collect statistics */
#ifndef DISABLE_PERFCOUNTERS
	mono_perfcounters->jit_methods++;
	mono_perfcounters->jit_bytes += header->code_size;
#endif
	mono_jit_stats.allocated_code_size += cfg->code_len;
	code_size_ratio = cfg->code_len;
	if (code_size_ratio > mono_jit_stats.biggest_method_size && mono_jit_stats.enabled) {
		mono_jit_stats.biggest_method_size = code_size_ratio;
		g_free (mono_jit_stats.biggest_method);
		mono_jit_stats.biggest_method = g_strdup_printf ("%s::%s)", method->klass->name, method->name);
	}
	code_size_ratio = (code_size_ratio * 100) / header->code_size;
	if (code_size_ratio > mono_jit_stats.max_code_size_ratio && mono_jit_stats.enabled) {
		mono_jit_stats.max_code_size_ratio = code_size_ratio;
		g_free (mono_jit_stats.max_ratio_method);
		mono_jit_stats.max_ratio_method = g_strdup_printf ("%s::%s)", method->klass->name, method->name);
	}
	mono_jit_stats.native_code_size += cfg->code_len;

	if (MONO_METHOD_COMPILE_END_ENABLED ())
		MONO_PROBE_METHOD_COMPILE_END (method, TRUE);

	return cfg;
}

#else

MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, JitFlags flags, int parts)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* DISABLE_JIT */

/*
 * LOCKING: Acquires the jit code hash lock.
 */
static MonoJitInfo*
lookup_method_inner (MonoDomain *domain, MonoMethod *method, MonoMethod *shared)
{
	MonoJitInfo *ji;
	static gboolean inited = FALSE;
	static int lookups = 0;
	static int failed_lookups = 0;

	mono_domain_jit_code_hash_lock (domain);
	ji = mono_internal_hash_table_lookup (&domain->jit_code_hash, method);
	if (!ji && shared) {
		/* Try generic sharing */
		ji = mono_internal_hash_table_lookup (&domain->jit_code_hash, shared);
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

	ji = lookup_method_inner (domain, method, NULL);

	if (!ji) {
		if (!mono_method_is_generic_sharable (method, FALSE))
			return NULL;
		shared = mini_get_shared_method (method);
		ji = lookup_method_inner (domain, method, shared);
	}

	return ji;
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

static gpointer
mono_jit_compile_method_inner (MonoMethod *method, MonoDomain *target_domain, int opt, MonoException **jit_ex)
{
	MonoCompile *cfg;
	gpointer code = NULL;
	MonoJitInfo *jinfo, *info;
	MonoVTable *vtable;
	MonoException *ex = NULL;
	guint32 prof_options;
	GTimer *jit_timer;
	MonoMethod *prof_method, *shared;

#ifdef MONO_USE_AOT_COMPILER
	if (opt & MONO_OPT_AOT) {
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
#ifdef HOST_WIN32
				g_warning ("Method '%s' in assembly '%s' contains native code that cannot be executed by Mono in modules loaded from byte arrays. The assembly was probably created using C++/CLI.\n", mono_method_full_name (method, TRUE), method->klass->image->name);
#else
				g_warning ("Method '%s' in assembly '%s' contains native code that cannot be executed by Mono on this platform. The assembly was probably created using C++/CLI.\n", mono_method_full_name (method, TRUE), method->klass->image->name);
#endif
			else
				mono_lookup_pinvoke_call (method, NULL, NULL);
		}
		nm = mono_marshal_get_native_wrapper (method, check_for_pending_exc, mono_aot_only);
		code = mono_get_addr_from_ftnptr (mono_compile_method (nm));
		jinfo = mono_jit_info_table_find (target_domain, code);
		if (!jinfo)
			jinfo = mono_jit_info_table_find (mono_domain_get (), code);
		if (jinfo)
			mono_profiler_method_end_jit (method, jinfo, MONO_PROFILE_OK);
		return code;
	} else if ((method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		const char *name = method->name;
		char *full_name, *msg;
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
				return mono_create_delegate_trampoline (target_domain, method->klass);
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

		full_name = mono_method_full_name (method, TRUE);
		msg = g_strdup_printf ("Unrecognizable runtime implemented method '%s'", full_name);
		*jit_ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "InvalidProgramException", msg);
		g_free (full_name);
		g_free (msg);
		return NULL;
	}

	if (method->wrapper_type == MONO_WRAPPER_UNKNOWN) {
		WrapperInfo *info = mono_marshal_get_wrapper_info (method);

		if (info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN || info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_OUT) {
			static MonoTrampInfo *in_tinfo, *out_tinfo;
			MonoTrampInfo *tinfo;
			MonoJitInfo *jinfo;
			gboolean is_in = info->subtype == WRAPPER_SUBTYPE_GSHAREDVT_IN;

			if (is_in && in_tinfo)
				return in_tinfo->code;
			else if (!is_in && out_tinfo)
				return out_tinfo->code;

			/*
			 * This is a special wrapper whose body is implemented in assembly, like a trampoline. We use a wrapper so EH
			 * works.
			 * FIXME: The caller signature doesn't match the callee, which might cause problems on some platforms
			 */
			if (mono_aot_only)
				mono_aot_get_trampoline_full (is_in ? "gsharedvt_trampoline" : "gsharedvt_out_trampoline", &tinfo);
			else
				mono_arch_get_gsharedvt_trampoline (&tinfo, FALSE);
			jinfo = create_jit_info_for_trampoline (method, tinfo);
			mono_jit_info_table_add (mono_get_root_domain (), jinfo);
			if (is_in)
				in_tinfo = tinfo;
			else
				out_tinfo = tinfo;
			return tinfo->code;
		}
	}

	if (mono_aot_only) {
		char *fullname = mono_method_full_name (method, TRUE);
		char *msg = g_strdup_printf ("Attempting to JIT compile method '%s' while running with --aot-only. See http://docs.xamarin.com/ios/about/limitations for more information.\n", fullname);

		*jit_ex = mono_get_exception_execution_engine (msg);
		g_free (fullname);
		g_free (msg);
		
		return NULL;
	}

	jit_timer = g_timer_new ();

	cfg = mini_method_compile (method, opt, target_domain, JIT_FLAG_RUN_CCTORS, 0);
	prof_method = cfg->method;

	g_timer_stop (jit_timer);
	mono_jit_stats.jit_time += g_timer_elapsed (jit_timer, NULL);
	g_timer_destroy (jit_timer);

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
		break;
	}
	case MONO_EXCEPTION_INVALID_PROGRAM:
		ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "InvalidProgramException", cfg->exception_message);
		break;
	case MONO_EXCEPTION_UNVERIFIABLE_IL:
		ex = mono_exception_from_name_msg (mono_defaults.corlib, "System.Security", "VerificationException", cfg->exception_message);
		break;
	case MONO_EXCEPTION_METHOD_ACCESS:
		ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "MethodAccessException", cfg->exception_message);
		break;
	case MONO_EXCEPTION_FIELD_ACCESS:
		ex = mono_exception_from_name_msg (mono_defaults.corlib, "System", "FieldAccessException", cfg->exception_message);
		break;
#ifndef DISABLE_SECURITY
	/* this can only be set if the security manager is active */
	case MONO_EXCEPTION_SECURITY_LINKDEMAND: {
		MonoSecurityManager* secman = mono_security_manager_get_methods ();
		MonoObject *exc = NULL;
		gpointer args [2];

		args [0] = &cfg->exception_data;
		args [1] = &method;
		mono_runtime_invoke (secman->linkdemandsecurityexception, NULL, args, &exc);

		ex = (MonoException*)exc;
		break;
	}
#endif
	case MONO_EXCEPTION_OBJECT_SUPPLIED: {
		MonoException *exp = cfg->exception_ptr;
		MONO_GC_UNREGISTER_ROOT (cfg->exception_ptr);

		ex = exp;
		break;
	}
	case MONO_EXCEPTION_OUT_OF_MEMORY:
		ex = mono_domain_get ()->out_of_memory_ex;
		break;
	case MONO_EXCEPTION_MONO_ERROR:
		g_assert (!mono_error_ok (&cfg->error));
		ex = mono_error_convert_to_exception (&cfg->error);
		break;
	default:
		g_assert_not_reached ();
	}

	if (ex) {
		if (cfg->prof_options & MONO_PROFILE_JIT_COMPILATION)
			mono_profiler_method_end_jit (method, NULL, MONO_PROFILE_FAILED);

		mono_destroy_compile (cfg);
		*jit_ex = ex;

		return NULL;
	}

	if (mono_method_is_generic_sharable (method, FALSE))
		shared = mini_get_shared_method (method);
	else
		shared = NULL;

	mono_domain_lock (target_domain);

	/* Check if some other thread already did the job. In this case, we can
       discard the code this thread generated. */

	info = lookup_method_inner (target_domain, method, shared);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if ((target_domain == mono_domain_get ()) || info->domain_neutral) {
			code = info->code_start;
//			printf("Discarding code for method %s\n", method->name);
		}
	}
	if (code == NULL) {
		/* The lookup + insert is atomic since this is done inside the domain lock */
		mono_domain_jit_code_hash_lock (target_domain);
		mono_internal_hash_table_insert (&target_domain->jit_code_hash, cfg->jit_info->d.method, cfg->jit_info);
		mono_domain_jit_code_hash_unlock (target_domain);

		code = cfg->native_code;

		if (cfg->generic_sharing_context && mono_method_is_generic_sharable (method, FALSE))
			mono_stats.generics_shared_methods++;
		if (cfg->gsharedvt)
			mono_stats.gsharedvt_methods++;
	}

	jinfo = cfg->jit_info;

	prof_options = cfg->prof_options;

	/*
	 * Update global stats while holding a lock, instead of doing many
	 * InterlockedIncrement operations during JITting.
	 */
	mono_jit_stats.allocate_var += cfg->stat_allocate_var;
	mono_jit_stats.locals_stack_size += cfg->stat_locals_stack_size;
	mono_jit_stats.basic_blocks += cfg->stat_basic_blocks;
	mono_jit_stats.max_basic_blocks = MAX (cfg->stat_basic_blocks, mono_jit_stats.max_basic_blocks);
	mono_jit_stats.cil_code_size += cfg->stat_cil_code_size;
	mono_jit_stats.regvars += cfg->stat_n_regvars;
	mono_jit_stats.inlineable_methods += cfg->stat_inlineable_methods;
	mono_jit_stats.inlined_methods += cfg->stat_inlined_methods;
	mono_jit_stats.cas_demand_generation += cfg->stat_cas_demand_generation;
	mono_jit_stats.code_reallocs += cfg->stat_code_reallocs;

	mono_destroy_compile (cfg);

#ifndef DISABLE_JIT
	if (domain_jit_info (target_domain)->jump_target_hash) {
		MonoJumpInfo patch_info;
		MonoJumpList *jlist;
		GSList *tmp;
		jlist = g_hash_table_lookup (domain_jit_info (target_domain)->jump_target_hash, method);
		if (jlist) {
			patch_info.next = NULL;
			patch_info.ip.i = 0;
			patch_info.type = MONO_PATCH_INFO_METHOD_JUMP;
			patch_info.data.method = method;
			g_hash_table_remove (domain_jit_info (target_domain)->jump_target_hash, method);

#if defined(__native_client_codegen__) && defined(__native_client__)
			/* These patches are applied after a method has been installed, no target munging is needed. */
			nacl_allow_target_modification (FALSE);
#endif
			for (tmp = jlist->list; tmp; tmp = tmp->next)
				mono_arch_patch_code (NULL, target_domain, tmp->data, &patch_info, NULL, TRUE);
#if defined(__native_client_codegen__) && defined(__native_client__)
			nacl_allow_target_modification (TRUE);
#endif
		}
	}

	mono_emit_jit_map (jinfo);
#endif
	mono_domain_unlock (target_domain);

	vtable = mono_class_vtable (target_domain, method->klass);
	if (!vtable) {
		ex = mono_class_get_exception_for_failure (method->klass);
		g_assert (ex);
		*jit_ex = ex;
		return NULL;
	}

	if (prof_options & MONO_PROFILE_JIT_COMPILATION) {
		if (method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) {
			if (mono_marshal_method_from_wrapper (method)) {
				/* Native func wrappers have no method */
				/* The profiler doesn't know about wrappers, so pass the original icall method */
				mono_profiler_method_end_jit (mono_marshal_method_from_wrapper (method), jinfo, MONO_PROFILE_OK);
			}
		}
		mono_profiler_method_end_jit (method, jinfo, MONO_PROFILE_OK);
		if (prof_method != method) {
			mono_profiler_method_end_jit (prof_method, jinfo, MONO_PROFILE_OK);
		}
	}

	ex = mono_runtime_class_init_full (vtable, FALSE);
	if (ex) {
		*jit_ex = ex;
		return NULL;
	}
	return code;
}

static gpointer
mono_jit_compile_method_with_opt (MonoMethod *method, guint32 opt, MonoException **ex)
{
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoJitInfo *info;
	gpointer code, p;
	MonoJitInfo *ji;
	MonoJitICallInfo *callinfo = NULL;
	WrapperInfo *winfo = NULL;

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
	}

	if (method->dynamic)
		opt &= ~MONO_OPT_SHARED;

	/* These methods can become invalid when a domain is unloaded */
	if (method->klass->image != mono_get_corlib () || method->is_inflated)
		opt &= ~MONO_OPT_SHARED;

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
			if (ctx)
				method = mono_class_inflate_generic_method (method, ctx);
		}
	}

	info = lookup_method (target_domain, method);
	if (info) {
		/* We can't use a domain specific method in another domain */
		if (! ((domain != target_domain) && !info->domain_neutral)) {
			MonoVTable *vtable;
			MonoException *tmpEx;

			mono_jit_stats.methods_lookups++;
			vtable = mono_class_vtable (domain, method->klass);
			g_assert (vtable);
			tmpEx = mono_runtime_class_init_full (vtable, ex == NULL);
			if (tmpEx) {
				*ex = tmpEx;
				return NULL;
			}
			return mono_create_ftnptr (target_domain, info->code_start);
		}
	}

	code = mono_jit_compile_method_inner (method, target_domain, opt, ex);
	if (!code)
		return NULL;

	if (method->wrapper_type == MONO_WRAPPER_WRITE_BARRIER || method->wrapper_type == MONO_WRAPPER_ALLOC) {
		MonoDomain *d;

		/*
		 * SGEN requires the JIT info for these methods to be registered, see is_ip_in_managed_allocator ().
		 */
		ji = mini_jit_info_table_find (mono_domain_get (), code, &d);
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
mono_jit_compile_method (MonoMethod *method)
{
	MonoException *ex = NULL;
	gpointer code;

	code = mono_jit_compile_method_with_opt (method, mono_get_optimizations_for_method (method, default_opt), &ex);
	if (!code) {
		g_assert (ex);
		mono_raise_exception (ex);
	}

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
	mono_conc_hashtable_remove (domain_jit_info (domain)->runtime_invoke_hash, method);

	/* Remove jump targets in this method */
	g_hash_table_iter_init (&iter, domain_jit_info (domain)->jump_target_hash);
	while (g_hash_table_iter_next (&iter, NULL, (void**)&jlist)) {
		GSList *tmp, *remove;

		remove = NULL;
		for (tmp = jlist->list; tmp; tmp = tmp->next) {
			guint8 *ip = tmp->data;

			if (ip >= (guint8*)ji->ji->code_start && ip < (guint8*)ji->ji->code_start + ji->ji->code_size)
				remove = g_slist_prepend (remove, tmp);
		}
		for (tmp = remove; tmp; tmp = tmp->next) {
			jlist->list = g_slist_delete_link (jlist->list, tmp->data);
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

gboolean mono_do_single_method_regression = FALSE;
guint32 mono_single_method_regression_opt = 0;
MonoMethod *mono_current_single_method;
GSList *mono_single_method_list;
GHashTable *mono_single_method_hash;

guint32
mono_get_optimizations_for_method (MonoMethod *method, guint32 default_opt)
{
	g_assert (method);

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
} RuntimeInvokeInfo;

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
	MonoMethod *invoke, *callee;
	MonoObject *(*runtime_invoke) (MonoObject *this, void **params, MonoObject **exc, void* compiled_method);
	MonoDomain *domain = mono_domain_get ();
	MonoJitDomainInfo *domain_info;
	RuntimeInvokeInfo *info, *info2;
	
	if (obj == NULL && !(method->flags & METHOD_ATTRIBUTE_STATIC) && !method->string_ctor && (method->wrapper_type == 0)) {
		g_warning ("Ignoring invocation of an instance method on a NULL instance.\n");
		return NULL;
	}

	domain_info = domain_jit_info (domain);

	info = mono_conc_hashtable_lookup (domain_info->runtime_invoke_hash, method);

	if (!info) {
		if (mono_security_core_clr_enabled ()) {
			/* 
			 * This might be redundant since mono_class_vtable () already does this,
			 * but keep it just in case for moonlight.
			 */
			mono_class_setup_vtable (method->klass);
			if (method->klass->exception_type != MONO_EXCEPTION_NONE) {
				if (exc)
					*exc = (MonoObject*)mono_class_get_exception_for_failure (method->klass);
				else
					mono_raise_exception (mono_class_get_exception_for_failure (method->klass));
				return NULL;
			}
		}

		info = g_new0 (RuntimeInvokeInfo, 1);

		invoke = mono_marshal_get_runtime_invoke (method, FALSE);
		info->vtable = mono_class_vtable_full (domain, method->klass, TRUE);
		g_assert (info->vtable);

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
			MonoException *jit_ex = NULL;

			info->compiled_method = mono_jit_compile_method_with_opt (callee, mono_get_optimizations_for_method (callee, default_opt), &jit_ex);
			if (!info->compiled_method) {
				g_free (info);
				g_assert (jit_ex);
				if (exc) {
					*exc = (MonoObject*)jit_ex;
					return NULL;
				} else {
					mono_raise_exception (jit_ex);
				}
			}

			info->compiled_method = mini_add_method_trampoline (NULL, callee, info->compiled_method, mono_method_needs_static_rgctx_invoke (callee, FALSE), FALSE);
		} else {
			info->compiled_method = NULL;
		}

		/*
		 * We want to avoid AOTing 1000s of runtime-invoke wrappers when running
		 * in full-aot mode, so we use a slower, but more generic wrapper if
		 * possible, built on top of the OP_DYN_CALL opcode provided by the JIT.
		 */
#ifdef MONO_ARCH_DYN_CALL_SUPPORTED
		if (mono_aot_only || debug_options.dyn_runtime_invoke) {
			MonoType *ret_type;
			MonoMethodSignature *sig = mono_method_signature (method);
			gboolean supported = TRUE;
			int i;

			if (method->string_ctor)
				sig = mono_marshal_get_string_ctor_signature (method);

			for (i = 0; i < sig->param_count; ++i) {
				MonoType *t = sig->params [i];

				if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t)))
					supported = FALSE;
			}

			if (mono_class_is_contextbound (method->klass) || !info->compiled_method)
				supported = FALSE;

			if (supported)
				info->dyn_call_info = mono_arch_dyn_call_prepare (sig);

			ret_type = sig->ret;
			if (info->dyn_call_info) {
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
			}
		}
#endif

		if (!info->dyn_call_info)
			info->runtime_invoke = mono_jit_compile_method (invoke);

		info2 = mono_conc_hashtable_insert (domain_info->runtime_invoke_hash, method, info);
		if (info2) {
			g_free (info);
			info = info2;
		}
	}

	runtime_invoke = info->runtime_invoke;

	/*
	 * We need this here because mono_marshal_get_runtime_invoke can place 
	 * the helper method in System.Object and not the target class.
	 */
	if (exc) {
		*exc = (MonoObject*)mono_runtime_class_init_full (info->vtable, FALSE);
		if (*exc)
			return NULL;
	} else {
		mono_runtime_class_init (info->vtable);
	}

	/* The wrappers expect this to be initialized to NULL */
	if (exc)
		*exc = NULL;

#ifdef MONO_ARCH_DYN_CALL_SUPPORTED
	if (info->dyn_call_info) {
		MonoMethodSignature *sig = mono_method_signature (method);
		gpointer *args;
		static RuntimeInvokeDynamicFunction dyn_runtime_invoke;
		int i, pindex;
		guint8 buf [128];
		guint8 retval [128];

		if (!dyn_runtime_invoke) {
			invoke = mono_marshal_get_runtime_invoke_dynamic ();
			dyn_runtime_invoke = mono_jit_compile_method (invoke);
		}

		/* Convert the arguments to the format expected by start_dyn_call () */
		args = g_alloca ((sig->param_count + sig->hasthis) * sizeof (gpointer));
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

		mono_arch_finish_dyn_call (info->dyn_call_info, buf);

		if (info->ret_box_class)
			return mono_value_box (domain, info->ret_box_class, retval);
		else
			return *(MonoObject**)retval;
	}
#endif

	return runtime_invoke (obj, params, exc, info->compiled_method);
}

MONO_SIG_HANDLER_FUNC (, mono_sigfpe_signal_handler)
{
	MonoException *exc = NULL;
	MonoJitInfo *ji;
	void *info = MONO_SIG_HANDLER_GET_INFO ();
	MONO_SIG_HANDLER_GET_CONTEXT;

	ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context (ctx));

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

		mono_handle_native_sigsegv (SIGSEGV, ctx);
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
	MonoJitTlsData *jit_tls = mono_native_tls_get_value (mono_jit_tls_id);
	gpointer fault_addr = NULL;
#ifdef HAVE_SIG_INFO
	MONO_SIG_HANDLER_INFO_TYPE *info = MONO_SIG_HANDLER_GET_INFO ();
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

#if !defined(HOST_WIN32) && defined(HAVE_SIG_INFO)
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
		mono_handle_native_sigsegv (SIGSEGV, ctx);
		if (mono_do_crash_chaining) {
			mono_chain_signal (MONO_SIG_HANDLER_PARAMS);
			return;
		}
	}

	ji = mono_jit_info_table_find (mono_domain_get (), mono_arch_ip_from_context (ctx));

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	if (mono_handle_soft_stack_ovf (jit_tls, ji, ctx, (guint8*)info->si_addr))
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

		mono_arch_handle_altstack_exception (ctx, info->si_addr, FALSE);
	}
#else

	if (!ji) {
		if (!mono_do_crash_chaining && mono_chain_signal (MONO_SIG_HANDLER_PARAMS))
			return;

		mono_handle_native_sigsegv (SIGSEGV, ctx);

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
		addr = mono_compile_method (nm);
	} else
	{
		addr = mono_compile_method (method);
	}
	return mono_get_addr_from_ftnptr (addr);
}
#endif

static gpointer *vtable_trampolines;
static int vtable_trampolines_size;

gpointer
mini_get_vtable_trampoline (int slot_index)
{
	int index = slot_index + MONO_IMT_SIZE;

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
			vtable_trampolines = new_table;
			vtable_trampolines_size = new_size;
		}
		mono_jit_unlock ();
	}

	if (!vtable_trampolines [index])
		vtable_trampolines [index] = mono_create_specific_trampoline (GUINT_TO_POINTER (slot_index), MONO_TRAMPOLINE_VCALL, mono_get_root_domain (), NULL);
	return vtable_trampolines [index];
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

	/* FIXME Support more cases */
	if (mono_aot_only)
		return NULL;

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	is_virtual_generic = method->is_inflated && mono_method_get_declaring_generic_method (method)->is_generic;
	is_interface = method->klass->flags & TYPE_ATTRIBUTE_INTERFACE ? TRUE : FALSE;
	load_imt_reg = is_virtual_generic || is_interface;

	if (is_interface && !is_virtual_generic)
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

	return cache [idx] = mono_arch_get_delegate_virtual_invoke_impl (sig, method, offset, load_imt_reg);
}

static gpointer
mini_get_imt_trampoline (int slot_index)
{
	return mini_get_vtable_trampoline (slot_index - MONO_IMT_SIZE);
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

		if (!strcmp (arg, "handle-sigint"))
			debug_options.handle_sigint = TRUE;
		else if (!strcmp (arg, "keep-delegates"))
			debug_options.keep_delegates = TRUE;
		else if (!strcmp (arg, "reverse-pinvoke-exceptions"))
			debug_options.reverse_pinvoke_exceptions = TRUE;
		else if (!strcmp (arg, "collect-pagefault-stats"))
			debug_options.collect_pagefault_stats = TRUE;
		else if (!strcmp (arg, "break-on-unverified"))
			debug_options.break_on_unverified = TRUE;
		else if (!strcmp (arg, "no-gdb-backtrace"))
			debug_options.no_gdb_backtrace = TRUE;
		else if (!strcmp (arg, "suspend-on-sigsegv"))
			debug_options.suspend_on_sigsegv = TRUE;
		else if (!strcmp (arg, "suspend-on-exception"))
			debug_options.suspend_on_exception = TRUE;
		else if (!strcmp (arg, "suspend-on-unhandled"))
			debug_options.suspend_on_unhandled = TRUE;
		else if (!strcmp (arg, "dont-free-domains"))
			mono_dont_free_domains = TRUE;
		else if (!strcmp (arg, "dyn-runtime-invoke"))
			debug_options.dyn_runtime_invoke = TRUE;
		else if (!strcmp (arg, "gdb"))
			debug_options.gdb = TRUE;
		else if (!strcmp (arg, "explicit-null-checks"))
			debug_options.explicit_null_checks = TRUE;
		else if (!strcmp (arg, "gen-seq-points"))
			debug_options.gen_seq_points = TRUE;
		else if (!strcmp (arg, "init-stacks"))
			debug_options.init_stacks = TRUE;
		else if (!strcmp (arg, "casts"))
			debug_options.better_cast_details = TRUE;
		else if (!strcmp (arg, "soft-breakpoints"))
			debug_options.soft_breakpoints = TRUE;
		else if (!strcmp (arg, "check-pinvoke-callconv"))
			debug_options.check_pinvoke_callconv = TRUE;
		else if (!strcmp (arg, "debug-domain-unload"))
			mono_enable_debug_domain_unload (TRUE);
		else {
			fprintf (stderr, "Invalid option for the MONO_DEBUG env variable: %s\n", arg);
			fprintf (stderr, "Available options: 'handle-sigint', 'keep-delegates', 'reverse-pinvoke-exceptions', 'collect-pagefault-stats', 'break-on-unverified', 'no-gdb-backtrace', 'dont-free-domains', 'suspend-on-sigsegv', 'suspend-on-exception', 'suspend-on-unhandled', 'dyn-runtime-invoke', 'gdb', 'explicit-null-checks', 'init-stacks', 'check-pinvoke-callconv', 'debug-domain-unload'\n");
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
#if !defined(__ia64__) && !defined(__ppc64__) && !defined(__powerpc64__)
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
#if defined(__ia64__) || defined(__ppc64__) || defined(__powerpc64__)
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
	mono_counters_register ("Total time spent JITting (sec)", MONO_COUNTER_JIT | MONO_COUNTER_DOUBLE, &mono_jit_stats.jit_time);
	mono_counters_register ("Basic blocks", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.basic_blocks);
	mono_counters_register ("Max basic blocks", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.max_basic_blocks);
	mono_counters_register ("Allocated vars", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocate_var);
	mono_counters_register ("Code reallocs", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.code_reallocs);
	mono_counters_register ("Allocated code size", MONO_COUNTER_JIT | MONO_COUNTER_INT, &mono_jit_stats.allocated_code_size);
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
static void seq_point_info_free (gpointer value);

static gint
class_method_pair_equal (gconstpointer ka, gconstpointer kb)
{
	const MonoClassMethodPair *apair = ka;
	const MonoClassMethodPair *bpair = kb;

	return apair->klass == bpair->klass && apair->method == bpair->method ? 1 : 0;
}

static guint
class_method_pair_hash (gconstpointer data)
{
	const MonoClassMethodPair *pair = data;

	return (gsize)pair->klass ^ (gsize)pair->method;
}

static void
mini_create_jit_domain_info (MonoDomain *domain)
{
	MonoJitDomainInfo *info = g_new0 (MonoJitDomainInfo, 1);

	info->class_init_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jump_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jit_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->delegate_trampoline_hash = g_hash_table_new (class_method_pair_hash, class_method_pair_equal);
	info->llvm_vcall_trampoline_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->runtime_invoke_hash = mono_conc_hashtable_new_full (&domain->lock, mono_aligned_addr_hash, NULL, NULL, runtime_invoke_info_free);
	info->seq_points = g_hash_table_new_full (mono_aligned_addr_hash, NULL, NULL, seq_point_info_free);
	info->arch_seq_points = g_hash_table_new (mono_aligned_addr_hash, NULL);
	info->jump_target_hash = g_hash_table_new (NULL, NULL);

	domain->runtime_info = info;
}

static void
delete_jump_list (gpointer key, gpointer value, gpointer user_data)
{
	MonoJumpList *jlist = value;
	g_slist_free (jlist->list);
}

static void
delete_got_slot_list (gpointer key, gpointer value, gpointer user_data)
{
	GSList *list = value;
	g_slist_free (list);
}

static void
dynamic_method_info_free (gpointer key, gpointer value, gpointer user_data)
{
	MonoJitDynamicMethodInfo *di = value;
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

static void seq_point_info_free (gpointer value)
{
	int i = 0;
	MonoSeqPointInfo* info = (MonoSeqPointInfo*)value;
	
	for (i = 0; i < info->len; ++i) {
		SeqPoint *sp = &info->seq_points [i];
		g_free (sp->next);
	}

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
	g_hash_table_destroy (info->class_init_trampoline_hash);
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
	MonoDomain *domain;
	MonoRuntimeCallbacks callbacks;
	MonoThreadInfoRuntimeCallbacks ticallbacks;

	MONO_VES_INIT_BEGIN ();

#if defined(__linux__) && !defined(__native_client__)
	if (access ("/proc/self/maps", F_OK) != 0) {
		g_print ("Mono requires /proc to be mounted.\n");
		exit (1);
	}
#endif

	mono_mutex_init_recursive (&jit_mutex);

	mono_cross_helpers_run ();

	/* Happens when using the embedding interface */
	if (!default_opt_set)
		default_opt = mono_parse_default_optimizations (NULL);

#ifdef MONO_ARCH_GSHAREDVT_SUPPORTED
	if (mono_aot_only)
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

	if (mono_use_imt) {
		callbacks.get_vtable_trampoline = mini_get_vtable_trampoline;
		callbacks.get_imt_trampoline = mini_get_imt_trampoline;
	}

	mono_install_callbacks (&callbacks);

	memset (&ticallbacks, 0, sizeof (ticallbacks));
	ticallbacks.setup_async_callback = mono_setup_async_callback;
	ticallbacks.thread_state_init_from_sigctx = mono_thread_state_init_from_sigctx;
	ticallbacks.thread_state_init_from_handle = mono_thread_state_init_from_handle;

	mono_threads_runtime_init (&ticallbacks);

	if (g_getenv ("MONO_DEBUG") != NULL)
		mini_parse_debug_options ();

	mono_code_manager_init ();

	mono_hwcap_init ();

	mono_arch_cpu_init ();

	mono_arch_init ();

	mono_unwind_init ();

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

#ifdef MONO_ARCH_HAVE_NOTIFY_PENDING_EXC
	check_for_pending_exc = FALSE;
	mono_threads_install_notify_pending_exc ((MonoThreadNotifyPendingExcFunc)mono_arch_notify_pending_exc);
#endif

#define JIT_TRAMPOLINES_WORK
#ifdef JIT_TRAMPOLINES_WORK
	mono_install_compile_method (mono_jit_compile_method);
	mono_install_free_method (mono_jit_free_method);
	mono_install_trampoline (mono_create_jit_trampoline);
	mono_install_jump_trampoline (mono_create_jump_trampoline);
#ifndef DISABLE_REMOTING
	mono_install_remoting_trampoline (mono_jit_create_remoting_trampoline);
#endif
	mono_install_delegate_trampoline (mono_create_delegate_trampoline);
	mono_install_create_domain_hook (mini_create_jit_domain_info);
	mono_install_free_domain_hook (mini_free_jit_domain_info);
#endif
#define JIT_INVOKE_WORKS
#ifdef JIT_INVOKE_WORKS
	mono_install_runtime_invoke (mono_jit_runtime_invoke);
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

	if (mono_use_imt) {
		if (mono_aot_only)
			mono_install_imt_thunk_builder (mono_aot_get_imt_thunk);
		else
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

	jit_icall_name_hash = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);

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
	mono_add_internal_call ("System.Exception::get_trace", 
				ves_icall_System_Exception_get_trace);
	mono_add_internal_call ("System.Security.SecurityFrame::_GetSecurityFrame",
				ves_icall_System_Security_SecurityFrame_GetSecurityFrame);
	mono_add_internal_call ("System.Security.SecurityFrame::_GetSecurityStack",
				ves_icall_System_Security_SecurityFrame_GetSecurityStack);
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
	register_icall (mono_jit_thread_attach, "mono_jit_thread_attach", "ptr ptr", TRUE);
	register_icall (mono_jit_set_domain, "mono_jit_set_domain", "void ptr", TRUE);
	register_icall (mono_domain_get, "mono_domain_get", "ptr", TRUE);

	register_dyn_icall (mono_get_throw_exception (), "mono_arch_throw_exception", "void object", TRUE);
	register_dyn_icall (mono_get_rethrow_exception (), "mono_arch_rethrow_exception", "void object", TRUE);
	register_dyn_icall (mono_get_throw_corlib_exception (), "mono_arch_throw_corlib_exception", "void ptr", TRUE);
	register_icall (mono_thread_get_undeniable_exception, "mono_thread_get_undeniable_exception", "object", FALSE);
	register_icall (mono_thread_interruption_checkpoint, "mono_thread_interruption_checkpoint", "void", FALSE);
	register_icall (mono_thread_force_interruption_checkpoint, "mono_thread_force_interruption_checkpoint", "void", FALSE);
#ifndef DISABLE_REMOTING
	register_icall (mono_load_remote_field_new, "mono_load_remote_field_new", "object object ptr ptr", FALSE);
	register_icall (mono_store_remote_field_new, "mono_store_remote_field_new", "void object ptr ptr object", FALSE);
#endif

#if defined(__native_client__) || defined(__native_client_codegen__)
	register_icall (mono_nacl_gc, "mono_nacl_gc", "void", TRUE);
#endif
	/* 
	 * NOTE, NOTE, NOTE, NOTE:
	 * when adding emulation for some opcodes, remember to also add a dummy
	 * rule to the burg files, because we need the arity information to be correct.
	 */
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
	if (ARCH_EMULATE_MUL_DIV || mono_arch_is_soft_float ()) {
		register_opcode_emulation (OP_FDIV, "__emul_fdiv", "double double double", mono_fdiv, "mono_fdiv", FALSE);
	}
#endif

	register_opcode_emulation (OP_FCONV_TO_U8, "__emul_fconv_to_u8", "ulong double", mono_fconv_u8, "mono_fconv_u8", FALSE);
	register_opcode_emulation (OP_FCONV_TO_U4, "__emul_fconv_to_u4", "uint32 double", mono_fconv_u4, "mono_fconv_u4", FALSE);
	register_opcode_emulation (OP_FCONV_TO_OVF_I8, "__emul_fconv_to_ovf_i8", "long double", mono_fconv_ovf_i8, "mono_fconv_ovf_i8", FALSE);
	register_opcode_emulation (OP_FCONV_TO_OVF_U8, "__emul_fconv_to_ovf_u8", "ulong double", mono_fconv_ovf_u8, "mono_fconv_ovf_u8", FALSE);

#ifdef MONO_ARCH_EMULATE_FCONV_TO_I8
	register_opcode_emulation (OP_FCONV_TO_I8, "__emul_fconv_to_i8", "long double", mono_fconv_i8, "mono_fconv_i8", FALSE);
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
	register_icall (mono_object_new, "mono_object_new", "object ptr ptr", FALSE);
	register_icall (mono_object_new_specific, "mono_object_new_specific", "object ptr", FALSE);
	register_icall (mono_array_new, "mono_array_new", "object ptr ptr int32", FALSE);
	register_icall (mono_array_new_specific, "mono_array_new_specific", "object ptr int32", FALSE);
	register_icall (mono_runtime_class_init, "mono_runtime_class_init", "void ptr", FALSE);
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

	register_icall (mono_debugger_agent_user_break, "mono_debugger_agent_user_break", "void", FALSE);

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

		if (mono_security_cas_enabled ()) {
			g_print ("\nDecl security check   : %ld\n", mono_jit_stats.cas_declsec_check);
			g_print ("LinkDemand (user)     : %ld\n", mono_jit_stats.cas_linkdemand);
			g_print ("LinkDemand (icall)    : %ld\n", mono_jit_stats.cas_linkdemand_icall);
			g_print ("LinkDemand (pinvoke)  : %ld\n", mono_jit_stats.cas_linkdemand_pinvoke);
			g_print ("LinkDemand (aptc)     : %ld\n", mono_jit_stats.cas_linkdemand_aptc);
			g_print ("Demand (code gen)     : %ld\n", mono_jit_stats.cas_demand_generation);
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

	free_jit_tls_data (mono_native_tls_get_value (mono_jit_tls_id));

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
	g_hash_table_destroy (jit_icall_name_hash);
	g_free (emul_opcode_map);
	g_free (emul_opcode_opcodes);
	g_free (vtable_trampolines);

	mono_tramp_info_cleanup ();

	mono_arch_cleanup ();

	mono_generic_sharing_cleanup ();

	mono_cleanup ();

	mono_trace_cleanup ();

	mono_counters_dump (MONO_COUNTER_SECTION_MASK | MONO_COUNTER_MONOTONIC, stdout);

	if (mono_inject_async_exc_method)
		mono_method_desc_free (mono_inject_async_exc_method);

	mono_native_tls_free (mono_jit_tls_id);

	mono_mutex_destroy (&jit_mutex);

	mono_mutex_destroy (&mono_delegate_section);

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
		method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
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

#ifndef DISABLE_JIT

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments) {
	return mono_arch_instrument_epilog_full (cfg, func, p, enable_arguments, FALSE);
}

void
mono_cfg_add_try_hole (MonoCompile *cfg, MonoExceptionClause *clause, guint8 *start, MonoBasicBlock *bb)
{
	TryBlockHole *hole = mono_mempool_alloc (cfg->mempool, sizeof (TryBlockHole));
	hole->clause = clause;
	hole->start_offset = start - cfg->native_code;
	hole->basic_block = bb;

	cfg->try_block_holes = g_slist_append_mempool (cfg->mempool, cfg->try_block_holes, hole);
}

void
mono_cfg_set_exception (MonoCompile *cfg, int type)
{
	cfg->exception_type = type;
}

#endif

/* Dummy versions of some arch specific functions to avoid ifdefs at call sites */

#ifndef MONO_ARCH_GSHAREDVT_SUPPORTED

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	return FALSE;
}

gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, MonoGenericSharingContext *gsctx, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_arg_trampoline (MonoDomain *domain, gpointer arg, gpointer addr)
{
	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_arch_get_gsharedvt_trampoline (MonoTrampInfo **info, gboolean aot)
{
	g_assert_not_reached ();
	return NULL;
}

#endif

#if defined(MONO_ARCH_GSHAREDVT_SUPPORTED) && !defined(ENABLE_GSHAREDVT)

gboolean
mono_arch_gsharedvt_sig_supported (MonoMethodSignature *sig)
{
	return FALSE;
}

gpointer
mono_arch_get_gsharedvt_call_info (gpointer addr, MonoMethodSignature *normal_sig, MonoMethodSignature *gsharedvt_sig, MonoGenericSharingContext *gsctx, gboolean gsharedvt_in, gint32 vcall_offset, gboolean calli)
{
	NOT_IMPLEMENTED;
	return NULL;
}

#endif

#ifdef USE_JUMP_TABLES
#define DEFAULT_JUMPTABLE_CHUNK_ELEMENTS 128

typedef struct MonoJumpTableChunk {
	guint32 total;
	guint32 active;
	struct MonoJumpTableChunk *previous;
	/* gpointer entries[total]; */
} MonoJumpTableChunk;

static MonoJumpTableChunk* g_jumptable;
#define mono_jumptable_lock() mono_mutex_lock (&jumptable_mutex)
#define mono_jumptable_unlock() mono_mutex_unlock (&jumptable_mutex)
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
		mono_mutex_init_recursive (&jumptable_mutex);
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
		mono_mutex_destroy (&jumptable_mutex);
	}
}

gpointer*
mono_jumptable_get_entry (guint8 *code_ptr)
{
	return mono_arch_jumptable_entry_from_code (code_ptr);
}
#endif

/*
 * mini_replace_type:
 *
 * Replace the type used in the metadata stream with what the JIT will actually use during compilation.
*/
MonoType*
mini_replace_type (MonoType *type)
{
	type = mono_type_get_underlying_type (type);
	return mini_native_type_replace_type (type);
}
