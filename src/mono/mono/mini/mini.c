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

#include "mini.h"
#include <string.h>
#include <ctype.h>
#include "inssel.h"
#include "debug.h"

#include "jit-icalls.c"

#define MONO_CHECK_THIS(ins) (cfg->method->signature->hasthis && (ins)->ssa_op == MONO_SSA_LOAD && (ins)->inst_left->inst_c0 == 0)

static gpointer mono_jit_compile_method (MonoMethod *method);

static void handle_stobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, MonoInst *src, 
			  const unsigned char *ip, MonoClass *klass, gboolean to_end, gboolean native);

extern guint8 mono_burg_arity [];
/* helper methods signature */
static MonoMethodSignature *helper_sig_long_long_long = NULL;
static MonoMethodSignature *helper_sig_long_long_int = NULL;
static MonoMethodSignature *helper_sig_newarr = NULL;
static MonoMethodSignature *helper_sig_ldstr = NULL;
static MonoMethodSignature *helper_sig_domain_get = NULL;
static MonoMethodSignature *helper_sig_object_new = NULL;
static MonoMethodSignature *helper_sig_compile = NULL;
static MonoMethodSignature *helper_sig_compile_virt = NULL;
static MonoMethodSignature *helper_sig_obj_ptr = NULL;
static MonoMethodSignature *helper_sig_ptr_void = NULL;
static MonoMethodSignature *helper_sig_void_ptr = NULL;
static MonoMethodSignature *helper_sig_void_obj = NULL;
static MonoMethodSignature *helper_sig_void_ptr_ptr = NULL;
static MonoMethodSignature *helper_sig_void_ptr_ptr_ptr = NULL;
static MonoMethodSignature *helper_sig_ptr_ptr_ptr = NULL;
static MonoMethodSignature *helper_sig_ptr_obj = NULL;
static MonoMethodSignature *helper_sig_initobj = NULL;
static MonoMethodSignature *helper_sig_memcpy = NULL;
static MonoMethodSignature *helper_sig_memset = NULL;
static MonoMethodSignature *helper_sig_ulong_double = NULL;
static MonoMethodSignature *helper_sig_long_double = NULL;
static MonoMethodSignature *helper_sig_uint_double = NULL;
static MonoMethodSignature *helper_sig_int_double = NULL;

static guint32 default_opt = MONO_OPT_PEEPHOLE;

guint32 mono_jit_tls_id = 0;
gboolean mono_jit_trace_calls = FALSE;
gboolean mono_break_on_exc = FALSE;
gboolean mono_compile_aot = FALSE;
gboolean mono_trace_coverage = FALSE;
gboolean mono_jit_profile = FALSE;
MonoDebugFormat mono_debug_format = MONO_DEBUG_FORMAT_NONE;

CRITICAL_SECTION *metadata_section = NULL;

static int mini_verbose = 0;

#ifdef MONO_USE_EXC_TABLES
static gboolean
mono_type_blittable (MonoType *type)
{
	if (type->byref)
		return FALSE;

	switch (type->type){
	case MONO_TYPE_VOID:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_OBJECT:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		return type->data.klass->blittable;
		break;
	default:
		break;
	}

	return FALSE;
}

gboolean
mono_method_blittable (MonoMethod *method)
{
	MonoMethodSignature *sig;
	int i;

	if (!method->addr)
		return FALSE;

	if (!mono_arch_has_unwind_info (method->addr)) {
		return FALSE;
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL)
		return TRUE;

	sig = method->signature;

	if (!mono_type_blittable (sig->ret))
		return FALSE;

	for (i = 0; i < sig->param_count; i++)
		if (!mono_type_blittable (sig->params [i]))
			return FALSE;

	return TRUE;
}
#endif

/* debug function */
static void
print_method_from_ip (void *ip)
{
	MonoJitInfo *ji;
	char *method;
	
	ji = mono_jit_info_table_find (mono_domain_get (), ip);
	if (!ji) {
		g_print ("No method at %p\n", ip);
		return;
	}
	method = mono_method_full_name (ji->method, TRUE);
	g_print ("IP at offset 0x%x of method %s (%p %p)\n", (char*)ip - (char*)ji->code_start, method, ji->code_start, (char*)ji->code_start + ji->code_size);
	g_free (method);

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
		}	\
        	(tblock)->real_offset = real_offset; \
	} while (0)

#define CHECK_BBLOCK(target,ip,tblock) do {	\
		if ((target) < (ip) && !(tblock)->code)	{	\
			bb_recheck = g_list_prepend (bb_recheck, (tblock));	\
			if (cfg->verbose_level > 2) g_print ("queued block %d for check at IL%04x from IL%04x\n", (tblock)->block_num, (target) - header->code, (ip) - header->code);	\
		}	\
	} while (0)

#define NEW_ICONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_ICONST;	\
		(dest)->inst_c0 = (val);	\
		(dest)->type = STACK_I4;	\
	} while (0)

/* FIXME: have a different definition of NEW_PCONST for 64 bit systems */
#define NEW_PCONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_ICONST;	\
		(dest)->inst_p0 = (val);	\
		(dest)->type = STACK_PTR;	\
	} while (0)

#define NEW_CLASSCONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = mono_compile_aot ? OP_AOTCONST : OP_ICONST;	\
		(dest)->inst_p0 = (val);	\
		(dest)->inst_i1 = (gpointer)MONO_PATCH_INFO_CLASS; \
		(dest)->type = STACK_PTR;	\
	} while (0)

#define NEW_IMAGECONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = mono_compile_aot ? OP_AOTCONST : OP_ICONST;	\
		(dest)->inst_p0 = (val);	\
		(dest)->inst_i1 = (gpointer)MONO_PATCH_INFO_IMAGE; \
		(dest)->type = STACK_PTR;	\
	} while (0)

#define NEW_FIELDCONST(cfg,dest,field) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = mono_compile_aot ? OP_AOTCONST : OP_ICONST;	\
		(dest)->inst_p0 = (field);	\
		(dest)->inst_i1 = (gpointer)MONO_PATCH_INFO_FIELD; \
		(dest)->type = STACK_PTR;	\
	} while (0)

#define NEW_METHODCONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = mono_compile_aot ? OP_AOTCONST : OP_ICONST;	\
		(dest)->inst_p0 = (val);	\
		(dest)->inst_i1 = (gpointer)MONO_PATCH_INFO_METHODCONST; \
		(dest)->type = STACK_PTR;	\
	} while (0)

#define NEW_DOMAINCONST(cfg,dest) do { \
               if ((cfg->opt & MONO_OPT_SAHRED) || mono_compile_aot) { \
                       NEW_TEMPLOAD (cfg, dest, mono_get_domainvar (cfg)->inst_c0); \
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
                (cfg)->disable_ssa = TRUE; \
	} while (0)

#define NEW_RETLOADA(cfg,dest) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->ssa_op = MONO_SSA_MAYBE_LOAD;	\
		(dest)->inst_i0 = (cfg)->ret;	\
		(dest)->inst_i0->flags |= MONO_INST_INDIRECT;	\
		(dest)->opcode = CEE_LDIND_I;	\
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
		MONO_INST_NEW(cfg, cmp, OP_COMPARE);	\
		sp -= 2;		\
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
		MONO_INST_NEW(cfg, cmp, OP_COMPARE);	\
		sp--;		\
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
	} while (0)

static GHashTable *coverage_hash = NULL;

MonoCoverageInfo *
mono_allocate_coverage_info (MonoMethod *method, int size)
{
	MonoCoverageInfo *res;

	if (!coverage_hash)
		coverage_hash = g_hash_table_new (NULL, NULL);

	res = g_malloc0 (sizeof (MonoCoverageInfo) + sizeof (int) * size * 2);

	res->entries = size;

	g_hash_table_insert (coverage_hash, method, res);

	return res;
}

MonoCoverageInfo *
mono_get_coverage_info (MonoMethod *method)
{
	if (!coverage_hash)
		return NULL;

	return g_hash_table_lookup (coverage_hash, method);
}
		
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

/*
 * We mark each basic block with a region ID. We use that to avoid BB
 * optimizations when blocks are in different regions. 
 */
static int
mono_find_block_region (MonoCompile *cfg, int offset, int *filter_lengths)
{
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	MonoExceptionClause *clause;
	int i;

	/* first search for handlers and filters */
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if ((clause->flags & MONO_EXCEPTION_CLAUSE_FILTER) && (offset >= clause->token_or_filter) &&
		    (offset < (clause->token_or_filter + filter_lengths [i])))
			return (i << 8) | 128 | clause->flags;
			   
		if (MONO_OFFSET_IN_HANDLER (clause, offset)) {
			return (i << 8) | 64 | clause->flags;
		}
	}

	/* search the try blocks */
	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, offset))
			return (i << 8) | clause->flags;
	}

	return -1;
}

static MonoBasicBlock *
mono_find_final_block (MonoCompile *cfg, unsigned char *ip, unsigned char *target, int type)
{
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	MonoExceptionClause *clause;
	MonoBasicBlock *handler;
	int i;

	for (i = 0; i < header->num_clauses; ++i) {
		clause = &header->clauses [i];
		if (MONO_OFFSET_IN_CLAUSE (clause, (ip - header->code)) && 
		    (!MONO_OFFSET_IN_CLAUSE (clause, (target - header->code)))) {
			if (clause->flags & type) {
				handler = g_hash_table_lookup (cfg->bb_hash, header->code + clause->handler_offset);
				g_assert (handler);
				return handler;
			}
		}
	}
	return NULL;
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

guint
mono_type_to_ldind (MonoType *type)
{
	int t = type->type;

	if (type->byref)
		return CEE_LDIND_I;

handle_enum:
	switch (t) {
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
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		return CEE_LDOBJ;
	default:
		g_error ("unknown type 0x%02x in type_to_ldind", type->type);
	}
	return -1;
}

guint
mono_type_to_stind (MonoType *type)
{
	int t = type->type;

	if (type->byref)
		return CEE_STIND_I;

handle_enum:
	switch (t) {
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
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		return CEE_STOBJ;
		/* fail right now */
	default:
		g_error ("unknown type %02x in type_to_stind", type->type);
	}
	return -1;
}

/*
 * Returns the type used in the eval stack when @type is loaded.
 * FIXME: return a MonoType/MonoClass for the byref and VALUETYPE cases.
 */
static void
type_to_eval_stack_type (MonoType *type, MonoInst *inst) {
	int t = type->type;

	if (type->byref) {
		inst->type = STACK_MP;
		return;
	}

handle_enum:
	switch (t) {
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
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else {
			inst->klass = type->data.klass;
			inst->type = STACK_VTYPE;
			return;
		}
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
	{0, 1, 0, 1, 0, 2, 0, 0},
	{0, 0, 0, 0, 1, 0, 0, 0},
	{0, 0, 0, 2, 0, 1, 0, 0},
	{0, 4, 0, 0, 0, 0, 3, 0},
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
	0, 0, OP_LADD-CEE_ADD, OP_PADD-CEE_ADD, OP_FADD-CEE_ADD, 0
};

/* handles from CEE_NEG to CEE_CONV_U8 */
static const guint16
unops_op_map [STACK_MAX] = {
	0, 0, OP_LNEG-CEE_NEG, OP_PNEG-CEE_NEG, OP_FNEG-CEE_NEG, 0
};

/* handles from CEE_CONV_U2 to CEE_SUB_OVF_UN */
static const guint16
ovfops_op_map [STACK_MAX] = {
	0, 0, OP_LCONV_TO_U2-CEE_CONV_U2, OP_PCONV_TO_U2-CEE_CONV_U2, OP_FCONV_TO_U2-CEE_CONV_U2, 0
};

/* handles from CEE_CONV_OVF_I1_UN to CEE_CONV_OVF_U_UN */
static const guint16
ovf2ops_op_map [STACK_MAX] = {
	0, 0, OP_LCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_PCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, OP_FCONV_TO_OVF_I1_UN-CEE_CONV_OVF_I1_UN, 0
};

/* handles from CEE_CONV_OVF_I1 to CEE_CONV_OVF_U8 */
static const guint16
ovf3ops_op_map [STACK_MAX] = {
	0, 0, OP_LCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_PCONV_TO_OVF_I1-CEE_CONV_OVF_I1, OP_FCONV_TO_OVF_I1-CEE_CONV_OVF_I1, 0
};

/* handles from CEE_CEQ to CEE_CLT_UN */
static const guint16
ceqops_op_map [STACK_MAX] = {
	0, 0, OP_LCEQ-CEE_CEQ, OP_PCEQ-CEE_CEQ, OP_FCEQ-CEE_CEQ, 0
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
		/* FIXME: handle some specifics with ins->next->type */
		ins->type = bin_comp_table [ins->inst_i0->type] [ins->inst_i1->type] ? STACK_I4: STACK_INV;
		return;
	case 256+CEE_CEQ:
	case 256+CEE_CGT:
	case 256+CEE_CGT_UN:
	case 256+CEE_CLT:
	case 256+CEE_CLT_UN:
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

static void
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
 * This function is called to handle items that are left on the evaluation stack
 * at basic block boundaries. What happens is that we save the values to local variables
 * and we reload them later when first entering the target basic block (with the
 * handle_loaded_temps () function).
 * A single joint point will use the same variables (stored in the array bb->out_stack or
 * bb->in_stack, if the basic block is before or after the joint point).
 */
static int
handle_stack_args (MonoCompile *cfg, MonoBasicBlock *bb, MonoInst **sp, int count) {
	int i;
	MonoBasicBlock *outb;
	MonoInst *inst, **locals;

	if (!count)
		return 0;
	if (cfg->verbose_level > 3)
		g_print ("%d item(s) on exit from B%d\n", count, bb->block_num);
	if (!bb->out_scount) {
		int found = 0;
		bb->out_scount = count;
		//g_print ("bblock %d has out:", bb->block_num);
		for (i = 0; i < bb->out_count; ++i) {
			outb = bb->out_bb [i];
			//g_print (" %d", outb->block_num);
			if (outb->in_stack) {
				found = 1;
				bb->out_stack = outb->in_stack;
				break;
			}
		}
		//g_print ("\n");
		if (!found) {
			bb->out_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*) * count);
			for (i = 0; i < count; ++i) {
				/* 
				 * dietmar suggests that we can reuse temps already allocated 
				 * for this purpouse, if they occupy the same stack slot and if 
				 * they are of the same type.
				 */
				bb->out_stack [i] = mono_compile_create_var (cfg, type_from_stack_type (sp [i]), OP_LOCAL);
			}
		}
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
			g_print ("storing %d to temp %d\n", i, locals [i]->inst_c0);
	}
	
	for (i = 0; i < bb->out_count; ++i) {
		outb = bb->out_bb [i];
		if (outb->in_scount)
			continue; /* check they are the same locals */
		outb->in_scount = count;
		outb->in_stack = locals;
	}
	return 0;
}

static int
ret_type_to_call_opcode (MonoType *type, int calli, int virt)
{
	int t = type->type;

	if (type->byref)
		return calli? OP_CALL_REG: virt? CEE_CALLVIRT: CEE_CALL;

handle_enum:
	switch (t) {
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
			t = type->data.klass->enum_basetype->type;
			goto handle_enum;
		} else
			return calli? OP_VCALL_REG: virt? OP_VCALLVIRT: OP_VCALL;
	default:
		g_error ("unknown type %02x in ret_type_to_call_opcode", type->type);
	}
	return -1;
}

void
mono_create_jump_table (MonoCompile *cfg, MonoInst *label, MonoBasicBlock **bbs, int num_blocks)
{
	MonoJumpInfo *ji = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));
	
	ji->ip.label = label;
	ji->type = MONO_PATCH_INFO_SWITCH;
	ji->data.table = bbs;
	ji->next = cfg->patch_info;
	ji->table_size = num_blocks;
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
		if (ins->opcode != OP_ICONST) {
			temp = mono_compile_create_var (cfg, type_from_stack_type (ins), OP_LOCAL);
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

		if (MONO_TYPE_ISSTRUCT (ret)) {
			MonoInst *loada;

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
	int i;

	MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (sig->ret, calli, virtual));
	
	call->inst.cil_code = ip;
	call->args = args;
	call->signature = sig;
	call = mono_arch_call_opcode (cfg, bblock, call, virtual);

	for (i = 0; i < (sig->param_count + sig->hasthis); ++i) {
		if (call->args [i]) {
			if (to_end)
				mono_add_ins_to_end (bblock, call->args [i]);
			else
				MONO_ADD_INS (bblock, call->args [i]);
		}
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
mono_emit_method_call (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method, MonoMethodSignature *sig,
		       MonoInst **args, const guint8 *ip, MonoInst *this)
{
	gboolean virtual = this != NULL;
	MonoCallInst *call;

	call = mono_emit_call_args (cfg, bblock, sig, args, FALSE, virtual, ip, FALSE);

	if (this && sig->hasthis && 
	    (method->klass->marshalbyref || method->klass == mono_defaults.object_class) && 
	    !(method->flags & METHOD_ATTRIBUTE_VIRTUAL) && !MONO_CHECK_THIS (this)) {
		call->method = mono_marshal_get_remoting_invoke_with_check (method);
	} else {
		call->method = method;
	}
	call->inst.flags |= MONO_INST_HAS_METHOD;
	call->inst.inst_left = this;

	return call;
}

inline static int
mono_emit_method_call_spilled (MonoCompile *cfg, MonoBasicBlock *bblock, MonoMethod *method,  
		       MonoInst **args, const guint8 *ip, MonoInst *this)
{
	MonoCallInst *call = mono_emit_method_call (cfg, bblock, method, method->signature, args, ip, this);

	return mono_spill_call (cfg, bblock, call, method->signature, method->string_ctor, ip, FALSE);
}

inline static int
mono_emit_native_call (MonoCompile *cfg, MonoBasicBlock *bblock, gconstpointer func, MonoMethodSignature *sig,
		       MonoInst **args, const guint8 *ip, gboolean to_end)
{
	MonoCallInst *call;

	g_assert (sig);

	call = mono_emit_call_args (cfg, bblock, sig, args, FALSE, FALSE, ip, to_end);
	call->fptr = func;
	return mono_spill_call (cfg, bblock, call, sig, func == mono_array_new_va, ip, to_end);
}

inline static int
mono_emit_jit_icall (MonoCompile *cfg, MonoBasicBlock *bblock, gconstpointer func, MonoInst **args, const guint8 *ip)
{
	MonoJitICallInfo *info = mono_find_jit_icall_by_addr (func);
	
	if (!info) {
		g_warning ("unregistered JIT ICall");
		g_assert_not_reached ();
	}

	return mono_emit_native_call (cfg, bblock, info->wrapper, info->sig, args, ip, FALSE);
}

static void
mono_emulate_opcode (MonoCompile *cfg, MonoInst *tree, MonoInst **iargs, MonoJitICallInfo *info)
{
	MonoInst *ins, *temp = NULL, *store, *load;
	int i, nargs;
	MonoCallInst *call;

	/*g_print ("emulating: ");
	mono_print_tree (tree);
	g_print ("\n");*/
	MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (info->sig->ret, FALSE, FALSE));
	ins = (MonoInst*)call;
	
	call->inst.cil_code = tree->cil_code;
	call->args = iargs;
	call->signature = info->sig;

	call = mono_arch_call_opcode (cfg, cfg->cbb, call, FALSE);

	if (!MONO_TYPE_IS_VOID (info->sig->ret)) {
		temp = mono_compile_create_var (cfg, info->sig->ret, OP_LOCAL);
		NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
		store->cil_code = tree->cil_code;
	} else {
		store = ins;
	}

	nargs = info->sig->param_count + info->sig->hasthis;

	for (i = 1; i < nargs; i++) {
		call->args [i - 1]->next = call->args [i];
	}

	if (nargs)
		call->args [nargs - 1]->next = store;

	if (cfg->prev_ins) {
		store->next = cfg->prev_ins->next;
		if (nargs)
			cfg->prev_ins->next =  call->args [0];
		else
			cfg->prev_ins->next = store;
	} else {
		store->next = cfg->cbb->code;
		if (nargs)		
			cfg->cbb->code = call->args [0];
		else
			cfg->cbb->code = store;
	}

	
	call->fptr = info->wrapper;

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

	if (!sighash)
		sighash = g_hash_table_new (NULL, NULL);


	if ((res = g_hash_table_lookup (sighash, (gpointer)arity)))
		return res;

	res = mono_metadata_signature_alloc (mono_defaults.corlib, arity + 1);

	res->params [0] = &mono_defaults.array_class->byval_arg; 
	
	for (i = 1; i <= arity; i++)
		res->params [i] = &mono_defaults.int_class->byval_arg;

	res->ret = &mono_defaults.int_class->byval_arg;

	g_hash_table_insert (sighash, (gpointer)arity, res);

	return res;
}

static void
handle_stobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, MonoInst *src, const unsigned char *ip, MonoClass *klass, gboolean to_end, gboolean native) {
	MonoInst *iargs [3];
	int n;

	g_assert (klass);
	/*
	 * This check breaks with spilled vars... need to handle it during verification anyway.
	 * g_assert (klass && klass == src->klass && klass == dest->klass);
	 */

	if (native)
		n = mono_class_native_size (klass, NULL);
	else
		n = mono_class_value_size (klass, NULL);

	iargs [0] = dest;
	iargs [1] = src;
	NEW_ICONST (cfg, iargs [2], n);

	mono_emit_native_call (cfg, bblock, helper_memcpy, helper_sig_memcpy, iargs, ip, to_end);
}

static void
handle_initobj (MonoCompile *cfg, MonoBasicBlock *bblock, MonoInst *dest, const guchar *ip, MonoClass *klass, MonoInst **stack_start, MonoInst **sp)
{
	MonoInst *iargs [2];
	MonoInst *ins, *zero_int32;
	int n;

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
		handle_loaded_temps (cfg, bblock, stack_start, sp);
		NEW_ICONST (cfg, ins, n);
		iargs [0] = dest;
		iargs [1] = ins;
		mono_emit_jit_icall (cfg, bblock, helper_initobj, iargs, ip);
		break;
	}
}

#define CODE_IS_STLOC(ip) (((ip) [0] >= CEE_STLOC_0 && (ip) [0] <= CEE_STLOC_3) || ((ip) [0] == CEE_STLOC_S))

static gboolean
mono_method_check_inlining (MonoMethod *method)
{
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	MonoMethodSignature *signature = method->signature;
	int i;

	/* fixme: we should inline wrappers */
	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return FALSE;

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

	//if (!MONO_TYPE_IS_VOID (signature->ret)) return FALSE;

	/* also consider num_locals? */
	if (header->code_size < 20)
		return TRUE;

	return FALSE;
}

static MonoInst*
mini_get_opcode_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	int pc, op;
	MonoInst *ins;

	if (cmethod->klass == mono_defaults.string_class) {
		if (cmethod->name [0] != 'g' || strcmp (cmethod->name, "get_Chars"))
			return NULL;
		op = OP_GETCHR;
	} else if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sin") == 0)
			op = OP_SIN;
		else if (strcmp (cmethod->name, "Cos") == 0)
			op = OP_COS;
		else if (strcmp (cmethod->name, "Tan") == 0)
			op = OP_TAN;
		else if (strcmp (cmethod->name, "Atan") == 0)
			op = OP_ATAN;
		else if (strcmp (cmethod->name, "Sqrt") == 0)
			op = OP_SQRT;
		else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8)
			op = OP_ABS;
		else
			return NULL;
	} else {
		return NULL;
	}
	pc = fsig->param_count + fsig->hasthis;
	MONO_INST_NEW (cfg, ins, op);

	if (pc > 0) {
		ins->inst_i0 = args [0];
		if (pc > 1)
			ins->inst_i1 = args [1];
	}

	return ins;
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
			temp = mono_compile_create_var (cfg, type_from_stack_type (*sp), OP_LOCAL);
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

/* offset from br.s -> br like opcodes */
#define BIG_BRANCH_OFFSET 13

/*
 * mono_method_to_ir: translates IL into basic blocks containing trees
 */
static int
mono_method_to_ir (MonoCompile *cfg, MonoMethod *method, MonoBasicBlock *start_bblock, MonoBasicBlock *end_bblock, 
		   int locals_offset, MonoInst *return_var, GList *dont_inline, MonoInst **inline_args, 
		   guint inline_offset)
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
	unsigned char *ip, *end, *target;
	static double r8_0 = 0.0;
	MonoMethodSignature *sig;
	MonoType **param_types;
	GList *bb_recheck = NULL, *tmp;
	int i, n, start_new_bblock, align;
	int num_calls = 0, inline_costs = 0;
	int *filter_lengths = NULL;
	guint real_offset;

	image = method->klass->image;
	header = ((MonoMethodNormal *)method)->header;
	sig = method->signature;
	ip = (unsigned char*)header->code;
	end = ip + header->code_size;
	mono_jit_stats.cil_code_size += header->code_size;

	if (cfg->method == method) {
		real_offset = 0;
		bbhash = cfg->bb_hash;
	} else {
		real_offset = inline_offset;
		bbhash = g_hash_table_new (g_direct_hash, NULL);
	}

	if (cfg->method == method) {

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

		arg_array = alloca (sizeof (MonoInst *) * (sig->hasthis + sig->param_count));
		for (i = sig->hasthis + sig->param_count - 1; i >= 0; i--)
			arg_array [i] = cfg->varinfo [i];

		if (mono_compile_aot) 
			cfg->opt |= MONO_OPT_SAHRED;

		if (header->num_clauses) {
			int size = sizeof (int) * header->num_clauses;
			filter_lengths = alloca (size);
			memset (filter_lengths, 0, size);
		}
		/* handle exception clauses */
		for (i = 0; i < header->num_clauses; ++i) {
			//unsigned char *p = ip;
			MonoExceptionClause *clause = &header->clauses [i];
			GET_BBLOCK (cfg, bbhash, tblock, ip + clause->try_offset);
			tblock->real_offset = clause->try_offset;
			GET_BBLOCK (cfg, bbhash, tblock, ip + clause->handler_offset);
			tblock->real_offset = clause->handler_offset;
			/*g_print ("clause try IL_%04x to IL_%04x handler %d at IL_%04x to IL_%04x\n", clause->try_offset, clause->try_offset + clause->try_len, clause->flags, clause->handler_offset, clause->handler_offset + clause->handler_len);
			  while (p < end) {
			  g_print ("%s", mono_disasm_code_one (NULL, method, p, &p));
			  }*/
			/* catch and filter blocks get the exception object on the stack */
			if (clause->flags == MONO_EXCEPTION_CLAUSE_NONE ||
			    clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				/* mostly like handle_stack_args (), but just sets the input args */
				/* g_print ("handling clause at IL_%04x\n", clause->handler_offset); */
				if (!cfg->exvar) {
					cfg->exvar = mono_compile_create_var (cfg, &mono_defaults.object_class->byval_arg, OP_LOCAL);
					/* prevent it from being register allocated */
					cfg->exvar->flags |= MONO_INST_INDIRECT;
				}
				tblock->in_scount = 1;
				tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
				tblock->in_stack [0] = cfg->exvar;
				if (clause->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
					GET_BBLOCK (cfg, bbhash, tblock, ip + clause->token_or_filter);
					tblock->real_offset = clause->token_or_filter;
					tblock->in_scount = 1;
					tblock->in_stack = mono_mempool_alloc (cfg->mempool, sizeof (MonoInst*));
					tblock->in_stack [0] = cfg->exvar;
				}
			}
		}

	} else {
		arg_array = alloca (sizeof (MonoInst *) * (sig->hasthis + sig->param_count));
		mono_save_args (cfg, start_bblock, sig, inline_args, arg_array);
	}

	/* FIRST CODE BLOCK */
	bblock = NEW_BBLOCK (cfg);
	bblock->cil_code = ip;

	ADD_BBLOCK (cfg, bbhash, bblock);

	if (cfg->method == method) {
		if (mono_method_has_breakpoint (method, FALSE)) {
			MONO_INST_NEW (cfg, ins, CEE_BREAK);
			MONO_ADD_INS (bblock, ins);
		}
	}
	
	if ((header->init_locals || (cfg->method == method && (cfg->opt & MONO_OPT_SAHRED)))) {
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

	mono_debug_init_method (cfg, bblock);

	param_types = mono_mempool_alloc (cfg->mempool, sizeof (MonoType*) * (sig->hasthis + sig->param_count));
	if (sig->hasthis)
		param_types [0] = method->klass->valuetype?&method->klass->this_arg:&method->klass->byval_arg;
	for (n = 0; n < sig->param_count; ++n)
		param_types [n + sig->hasthis] = sig->params [n];

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
	if (cfg->method != method && sig->hasthis) {
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
				NEW_TEMPLOAD (cfg, ins, bblock->in_stack [i]->inst_c0);
				*sp++ = ins;
			}
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
					NEW_TEMPLOAD (cfg, ins, bblock->in_stack [i]->inst_c0);
					*sp++ = ins;
				}
			}
		}

		if (cfg->verbose_level > 3)
			g_print ("converting (in B%d: stack: %d) %s", bblock->block_num, sp-stack_start, mono_disasm_code_one (NULL, method, ip, NULL));

		switch (*ip) {
		case CEE_NOP:
			++ip;
			break;
		case CEE_BREAK:
			MONO_INST_NEW (cfg, ins, CEE_BREAK);
			ins->cil_code = ip++;
			MONO_ADD_INS (bblock, ins);
			break;
		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3:
			CHECK_STACK_OVF (1);
			n = (*ip)-CEE_LDARG_0;
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
			CHECK_STACK_OVF (1);
			NEW_ARGLOAD (cfg, ins, ip [1]);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDARGA_S:
			CHECK_STACK_OVF (1);
			NEW_ARGLOADA (cfg, ins, ip [1]);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_STARG_S:
			CHECK_STACK (1);
			--sp;
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
			CHECK_STACK_OVF (1);
			NEW_LOCLOAD (cfg, ins, ip [1]);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_LDLOCA_S:
			CHECK_STACK_OVF (1);
			NEW_LOCLOADA (cfg, ins, ip [1]);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 2;
			break;
		case CEE_STLOC_S:
			CHECK_STACK (1);
			--sp;
			handle_loaded_temps (cfg, bblock, stack_start, sp);
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
			CHECK_STACK_OVF (1);
			++ip;
			NEW_ICONST (cfg, ins, *((signed char*)ip));
			ins->cil_code = ip;
			++ip;
			*sp++ = ins;
			break;
		case CEE_LDC_I4:
			CHECK_STACK_OVF (1);
			NEW_ICONST (cfg, ins, (gint32)read32 (ip + 1));
			ins->cil_code = ip;
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_LDC_I8:
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
			float *f = g_malloc (sizeof (float));
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
			double *d = g_malloc (sizeof (double));
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
				temp->cil_code = ip;
				NEW_TEMPSTORE (cfg, store, temp->inst_c0, ins);
				store->cil_code = ip;
				MONO_ADD_INS (bblock, store);
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
			if (stack_start != sp)
				goto unverified;
			MONO_INST_NEW (cfg, ins, CEE_JMP);
			token = read32 (ip + 1);
			/* FIXME: check the signature matches */
			cmethod = mono_get_method (image, token, NULL);
			/*
			 * The current magic trampoline can't handle this
			 * apparently, so we compile the method right away.
			 * Later, we may need to fix the trampoline or use a different one.
			 */
			ins->inst_p0 = mono_compile_method (cmethod);
			MONO_ADD_INS (bblock, ins);
			ip += 5;
			start_new_bblock = 1;
			break;
		case CEE_CALLI:
		case CEE_CALL:
		case CEE_CALLVIRT: {
			MonoInst *addr = NULL;
			MonoMethodSignature *fsig = NULL;
			MonoMethodHeader *cheader;
			int temp, array_rank = 0;
			int virtual = *ip == CEE_CALLVIRT;

			token = read32 (ip + 1);

			if (*ip == CEE_CALLI) {
				cmethod = NULL;
				cheader = NULL;
				CHECK_STACK (1);
				--sp;
				addr = *sp;
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					fsig = (MonoMethodSignature *)mono_method_get_wrapper_data (method, token);
				else
					fsig = mono_metadata_parse_signature (image, token);

				n = fsig->param_count + fsig->hasthis;

			} else {
				cmethod = mono_get_method (image, token, NULL);
				cheader = ((MonoMethodNormal *)cmethod)->header;

				if (!cmethod->klass->inited)
					mono_class_init (cmethod->klass);

				if (cmethod->signature->pinvoke) {
#ifdef MONO_USE_EXC_TABLES
					if (mono_method_blittable (cmethod)) {
						fsig = cmethod->signature;
					} else {
#endif
						MonoMethod *wrapper = mono_marshal_get_native_wrapper (cmethod);
						fsig = wrapper->signature;
#ifdef MONO_USE_EXC_TABLES
					}
#endif
				} else {
					fsig = cmethod->signature;
				}

				n = fsig->param_count + fsig->hasthis;

				if (cmethod->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL &&
				    cmethod->klass->parent == mono_defaults.array_class) {
					array_rank = cmethod->klass->rank;
				}

				if (cmethod->string_ctor)
					g_assert_not_reached ();

			}

			CHECK_STACK (n);

			//g_assert (!virtual || fsig->hasthis);

			sp -= n;

			if (cmethod && (cfg->opt & MONO_OPT_INTRINS) && (ins = mini_get_opcode_for_method (cfg, cmethod, fsig, sp))) {
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

			if ((cfg->opt & MONO_OPT_INLINE) && 
			    (!virtual || !(cmethod->flags & METHOD_ATTRIBUTE_VIRTUAL) || (cmethod->flags & METHOD_ATTRIBUTE_FINAL)) && 
			    cmethod && cheader && mono_method_check_inlining (cmethod) &&
			    method != cmethod && !g_list_find (dont_inline, cmethod)) {
				MonoInst *rvar = NULL;
				MonoBasicBlock *ebblock, *sbblock;
				int costs, new_locals_offset;
				
				if (cfg->verbose_level > 2)
					g_print ("INLINE START %p %s\n", cmethod,  mono_method_full_name (cmethod, TRUE));

				if (!cmethod->inline_info) {
					mono_jit_stats.inlineable_methods++;
					cmethod->inline_info = 1;
				}
				/* allocate space to store the return value */
				if (!MONO_TYPE_IS_VOID (fsig->ret)) 
					rvar =  mono_compile_create_var (cfg, fsig->ret, OP_LOCAL);

				/* allocate local variables */
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
				
				dont_inline = g_list_prepend (dont_inline, method);
				costs = mono_method_to_ir (cfg, cmethod, sbblock, ebblock, new_locals_offset, rvar, dont_inline, sp, real_offset);
				dont_inline = g_list_remove (dont_inline, method);
				
				if (costs >= 0 && costs < 60) {

					mono_jit_stats.inlined_methods++;

					/* always add some code to avoid block split failures */
					MONO_INST_NEW (cfg, ins, CEE_NOP);
					MONO_ADD_INS (bblock, ins);
					ins->cil_code = ip;

					ip += 5;
					real_offset += 5;

					bblock->next_bb = sbblock;
					link_bblock (cfg, bblock, sbblock);

					GET_BBLOCK (cfg, bbhash, bblock, ip);
					ebblock->next_bb = bblock;
					link_bblock (cfg, ebblock, bblock);

					if (rvar) {
						NEW_TEMPLOAD (cfg, ins, rvar->inst_c0);
						*sp++ = ins;
					}
					if (sp != stack_start) {
						handle_stack_args (cfg, ebblock, stack_start, sp - stack_start);
						sp = stack_start;
					}
					start_new_bblock = 1;
					if (cfg->verbose_level > 2)
						g_print ("INLINE END %s\n", mono_method_full_name (cmethod, TRUE));
					
					// { static int c = 0; printf ("ICOUNT %d %d %s\n", c++, costs, mono_method_full_name (cmethod, TRUE)); }

					inline_costs += costs;
					break;
				} else {

					if (cfg->verbose_level > 2)
						g_print ("INLINE ABORTED %s\n", mono_method_full_name (cmethod, TRUE));

				}
			}
			
			inline_costs += 10 * num_calls++;
			handle_loaded_temps (cfg, bblock, stack_start, sp);

			/* tail recursion elimination */
			if ((cfg->opt & MONO_OPT_TAILC) && *ip == CEE_CALL && cmethod == cfg->method && ip [5] == CEE_RET) {
				gboolean has_vtargs = FALSE;
				int i;
				
				/* keep it simple */
				for (i =  fsig->param_count - 1; i >= 0; i--) {
					if (MONO_TYPE_ISSTRUCT (cmethod->signature->params [i])) 
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
					ip += 5;
					
					if (!MONO_TYPE_IS_VOID (fsig->ret)) {
						/* just create a dummy - the value is never used */
						ins = mono_compile_create_var (cfg, fsig->ret, OP_LOCAL);
						NEW_TEMPLOAD (cfg, *sp, ins->inst_c0);
						sp++;
					}

					break;
				}
			}

			if (*ip == CEE_CALLI) {

				if ((temp = mono_emit_calli (cfg, bblock, fsig, sp, addr, ip)) != -1) {
					NEW_TEMPLOAD (cfg, *sp, temp);
					sp++;
				}
	      				
			} else if (array_rank) {
				MonoMethodSignature *esig;
				MonoInst *addr;

				if (strcmp (cmethod->name, "Set") == 0) { /* array Set */ 
					esig = mono_get_element_address_signature (fsig->param_count - 1);
					
					temp = mono_emit_native_call (cfg, bblock, ves_array_element_address, esig, sp, ip, FALSE);
					NEW_TEMPLOAD (cfg, addr, temp);
					NEW_INDSTORE (cfg, ins, addr, sp [fsig->param_count], fsig->params [fsig->param_count - 1]);
					ins->cil_code = ip;
					if (ins->opcode == CEE_STOBJ) {
						handle_stobj (cfg, bblock, addr, sp [fsig->param_count], ip, mono_class_from_mono_type (fsig->params [fsig->param_count-1]), FALSE, FALSE);
					} else {
						MONO_ADD_INS (bblock, ins);
					}

				} else if (strcmp (cmethod->name, "Get") == 0) { /* array Get */
					esig = mono_get_element_address_signature (fsig->param_count);

					temp = mono_emit_native_call (cfg, bblock, ves_array_element_address, esig, sp, ip, FALSE);
					NEW_TEMPLOAD (cfg, addr, temp);
					NEW_INDLOAD (cfg, ins, addr, fsig->ret);
					ins->cil_code = ip;

					*sp++ = ins;
				} else if (strcmp (cmethod->name, "Address") == 0) { /* array Address */
					/* implement me */
					esig = mono_get_element_address_signature (fsig->param_count);

					temp = mono_emit_native_call (cfg, bblock, ves_array_element_address, esig, sp, ip, FALSE);
					NEW_TEMPLOAD (cfg, *sp, temp);
					sp++;
				} else {
					g_assert_not_reached ();
				}

			} else {
				if (0 && CODE_IS_STLOC (ip + 5) && (!MONO_TYPE_ISSTRUCT (fsig->ret)) && (!MONO_TYPE_IS_VOID (fsig->ret) || cmethod->string_ctor)) {
					/* no need to spill */
					ins = (MonoInst*)mono_emit_method_call (cfg, bblock, cmethod, fsig, sp, ip, virtual ? sp [0] : NULL);
					*sp++ = ins;
				} else {
					if ((temp = mono_emit_method_call_spilled (cfg, bblock, cmethod, sp, ip, virtual ? sp [0] : NULL)) != -1) {
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
					ins->opcode = mono_type_to_stind (method->signature->ret);
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
			CHECK_STACK (1);
			n = read32 (ip + 1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			ins->inst_left = *sp;
			if (ins->inst_left->type != STACK_I4) goto unverified;
			ins->cil_code = ip;
			ip += 5;
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
			/* FIXME: handle stack args */
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
		case CEE_ADD:
		case CEE_SUB:
		case CEE_MUL:
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
			ip++;
			break;
		case CEE_NEG:
		case CEE_NOT:
		case CEE_CONV_I1:
		case CEE_CONV_I2:
		case CEE_CONV_I4:
		case CEE_CONV_I8:
		case CEE_CONV_R4:
		case CEE_CONV_R8:
		case CEE_CONV_U4:
		case CEE_CONV_U8:
		case CEE_CONV_OVF_I8:
		case CEE_CONV_OVF_U8:
		case CEE_CONV_R_UN:
			CHECK_STACK (1);
			ADD_UNOP (*ip);
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
			g_error ("opcode 0x%02x not handled", *ip);
			break;
		case CEE_LDOBJ: {
			MonoInst *iargs [3];
			CHECK_STACK (1);
			--sp;
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get (image, token);
			mono_class_init (klass);
			n = mono_class_value_size (klass, NULL);
			ins = mono_compile_create_var (cfg, &klass->byval_arg, OP_LOCAL);
			NEW_TEMPLOADA (cfg, iargs [0], ins->inst_c0);
			iargs [1] = *sp;
			NEW_ICONST (cfg, iargs [2], n);
			mono_emit_jit_icall (cfg, bblock, helper_memcpy, iargs, ip);
			NEW_TEMPLOAD (cfg, *sp, ins->inst_c0);
			++sp;
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_LDSTR:
			CHECK_STACK_OVF (1);
			n = read32 (ip + 1);

			if (mono_compile_aot) {
				cfg->ldstr_list = g_list_prepend (cfg->ldstr_list, (gpointer)n);
			}

			if ((cfg->opt & MONO_OPT_SAHRED) || mono_compile_aot) {
				int temp;
				MonoInst *iargs [3];
				NEW_TEMPLOAD (cfg, iargs [0], mono_get_domainvar (cfg)->inst_c0);
				NEW_IMAGECONST (cfg, iargs [1], image);
				NEW_ICONST (cfg, iargs [2], mono_metadata_token_index (n));
				temp = mono_emit_jit_icall (cfg, bblock, mono_ldstr, iargs, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);
				mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
			} else {
				NEW_PCONST (cfg, ins, NULL);
				ins->cil_code = ip;
				ins->type = STACK_OBJ;
				ins->inst_p0 = mono_ldstr (cfg->domain, image, mono_metadata_token_index (n));
				*sp = ins;
			}
			sp++;
			ip += 5;
			break;
		case CEE_NEWOBJ: {
			MonoInst *iargs [2];
			int temp;

			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE) {
				cmethod = mono_method_get_wrapper_data (method, token);
			} else
				cmethod = mono_get_method (image, token, NULL);

			mono_class_init (cmethod->klass);

			n = cmethod->signature->param_count;
			CHECK_STACK (n);

			/* move the args to allow room for 'this' in the first position */
			while (n--) {
				--sp;
				sp [1] = sp [0];
			}

			handle_loaded_temps (cfg, bblock, stack_start, sp);
			

			if (cmethod->klass->parent == mono_defaults.array_class) {
				NEW_METHODCONST (cfg, *sp, cmethod);
				temp = mono_emit_native_call (cfg, bblock, mono_array_new_va, cmethod->signature, sp, ip, FALSE);

			} else if (cmethod->string_ctor) {
				/* we simply pass a null pointer */
				NEW_PCONST (cfg, *sp, NULL); 
				/* now call the string ctor */
				temp = mono_emit_method_call_spilled (cfg, bblock, cmethod, sp, ip, NULL);
			} else {
				if (cmethod->klass->valuetype) {
					iargs [0] = mono_compile_create_var (cfg, &cmethod->klass->byval_arg, OP_LOCAL);
					temp = iargs [0]->inst_c0;
					NEW_TEMPLOADA (cfg, *sp, temp);
				} else {
					NEW_DOMAINCONST (cfg, iargs [0]);
					NEW_CLASSCONST (cfg, iargs [1], cmethod->klass);

					temp = mono_emit_jit_icall (cfg, bblock, mono_object_new, iargs, ip);
					NEW_TEMPLOAD (cfg, *sp, temp);
				}

				/* now call the actual ctor */
				mono_emit_method_call_spilled (cfg, bblock, cmethod, sp, ip, NULL);
			}

			NEW_TEMPLOAD (cfg, *sp, temp);
			sp++;
			
			ip += 5;
			inline_costs += 5;
			break;
		}
		case CEE_ISINST:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			klass = mono_class_get (image, read32 (ip + 1));
			mono_class_init (klass);
			ins->type = STACK_OBJ;
			ins->inst_left = *sp;
			ins->inst_newa_class = klass;
			ins->cil_code = ip;
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_UNBOX: {
			MonoInst *add, *vtoffset;
			/* FIXME: need to check class: move to inssel.brg? */
			CHECK_STACK (1);
			--sp;
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else 
				klass = mono_class_get (image, token);
			mono_class_init (klass);
			MONO_INST_NEW (cfg, add, CEE_ADD);
			NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
			add->inst_left = *sp;
			add->inst_right = vtoffset;
			add->type = STACK_MP;
			*sp++ = add;
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_CASTCLASS:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			klass = mono_class_get (image, read32 (ip + 1));
			mono_class_init (klass);
			ins->type = STACK_OBJ;
			ins->inst_left = *sp;
			ins->klass = klass;
			ins->inst_newa_class = klass;
			ins->cil_code = ip;
			ip += 5;
			*sp++ = ins;
			break;
		case CEE_THROW:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			--sp;
			ins->inst_left = *sp;
			ins->cil_code = ip++;
			MONO_ADD_INS (bblock, ins);
			sp = stack_start;
			start_new_bblock = 1;
			break;
		case CEE_LDFLD:
		case CEE_LDFLDA:
		case CEE_STFLD: {
			MonoInst *offset_ins;
			MonoClassField *field;
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
			token = read32 (ip + 1);
			field = mono_field_from_token (image, token, &klass);
			mono_class_init (klass);
			foffset = klass->valuetype? field->offset - sizeof (MonoObject): field->offset;
			/* FIXME: mark instructions for use in SSA */
			if (*ip == CEE_STFLD) {
				if (klass->marshalbyref && !MONO_CHECK_THIS (sp [0])) {
					/* fixme: we need to inline that call somehow */
					MonoMethod *stfld_wrapper = mono_marshal_get_stfld_wrapper (field->type); 
					MonoInst *iargs [5];
					iargs [0] = sp [0];
					NEW_CLASSCONST (cfg, iargs [1], klass);
					NEW_FIELDCONST (cfg, iargs [2], field);
					NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset);
					iargs [4] = sp [1];
					mono_emit_method_call_spilled (cfg, bblock, stfld_wrapper, iargs, ip, NULL);
				} else {
					MonoInst *store;
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, CEE_ADD);
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
				if (klass->marshalbyref && !MONO_CHECK_THIS (sp [0])) {
					/* fixme: we need to inline that call somehow */
					MonoMethod *ldfld_wrapper = mono_marshal_get_ldfld_wrapper (field->type); 
					MonoInst *iargs [4];
					int temp;
					iargs [0] = sp [0];
					NEW_CLASSCONST (cfg, iargs [1], klass);
					NEW_FIELDCONST (cfg, iargs [2], field);
					NEW_ICONST (cfg, iargs [3], klass->valuetype ? field->offset - sizeof (MonoObject) : field->offset);
					temp = mono_emit_method_call_spilled (cfg, bblock, ldfld_wrapper, iargs, ip, NULL);
					if (*ip == CEE_LDFLDA) {
						/* not sure howto handle this */
						NEW_TEMPLOADA (cfg, *sp, temp);
					} else {
						NEW_TEMPLOAD (cfg, *sp, temp);
					}
					sp++;
				} else {
					NEW_ICONST (cfg, offset_ins, foffset);
					MONO_INST_NEW (cfg, ins, CEE_ADD);
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
			MonoVTable *vtable;

			token = read32 (ip + 1);

			field = mono_field_from_token (image, token, &klass);
			mono_class_init (klass);

			handle_loaded_temps (cfg, bblock, stack_start, sp);
				
			if (((cfg->opt & MONO_OPT_SAHRED) || mono_compile_aot)) {
				int temp;
				MonoInst *iargs [2];
				g_assert (field->parent);
				NEW_TEMPLOAD (cfg, iargs [0], mono_get_domainvar (cfg)->inst_c0);
				NEW_FIELDCONST (cfg, iargs [1], field);
				temp = mono_emit_jit_icall (cfg, bblock, mono_class_static_field_address, iargs, ip);
				NEW_TEMPLOAD (cfg, ins, temp);
			} else {
				vtable = mono_class_vtable (cfg->domain, klass);
				NEW_PCONST (cfg, ins, (char*)vtable->data + field->offset);
				ins->cil_code = ip;
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
			ip += 5;
			break;
		}
		case CEE_STOBJ:
			CHECK_STACK (2);
			sp -= 2;
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get (image, token);
			mono_class_init (klass);
			handle_stobj (cfg, bblock, sp [0], sp [1], ip, klass, FALSE, FALSE);
			ip += 5;
			inline_costs += 1;
			break;
		case CEE_BOX: {
			MonoInst *iargs [2];
			MonoInst *load, *vtoffset, *add, *val, *vstore;
			int temp;
			CHECK_STACK (1);
			--sp;
			val = *sp;
			token = read32 (ip + 1);
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get (image, token);
			mono_class_init (klass);

			/* much like NEWOBJ */
			NEW_DOMAINCONST (cfg, iargs [0]);
			NEW_CLASSCONST (cfg, iargs [1], klass);
			
			temp = mono_emit_jit_icall (cfg, bblock, mono_object_new, iargs, ip);
			NEW_TEMPLOAD (cfg, load, temp);
			NEW_ICONST (cfg, vtoffset, sizeof (MonoObject));
			MONO_INST_NEW (cfg, add, CEE_ADD);
			add->inst_left = load;
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

			NEW_TEMPLOAD (cfg, load, temp);
			*sp++ = load;
			ip += 5;
			inline_costs += 1;
			break;
		}
		case CEE_NEWARR:
			CHECK_STACK (1);
			MONO_INST_NEW (cfg, ins, *ip);
			ins->cil_code = ip;
			--sp;

			token = read32 (ip + 1);

			/* allocate the domainvar - becaus this is used in decompose_foreach */
			if ((cfg->opt & MONO_OPT_SAHRED) || mono_compile_aot)
				mono_get_domainvar (cfg);
			
			if (method->wrapper_type != MONO_WRAPPER_NONE)
				klass = (MonoClass *)mono_method_get_wrapper_data (method, token);
			else
				klass = mono_class_get (image, token);

			mono_class_init (klass);
			ins->inst_newa_class = klass;
			ins->inst_newa_len = *sp;
			ins->type = STACK_OBJ;
			ip += 5;
			*sp++ = ins;
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
			klass = mono_class_get (image, read32 (ip + 1));
			mono_class_init (klass);
			NEW_LDELEMA (cfg, ins, sp, klass);
			ins->cil_code = ip;
			*sp++ = ins;
			ip += 5;
			break;
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
		case CEE_STELEM_R8:
		case CEE_STELEM_REF: {
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
			/* FIXME: add the implicit STELEM_REF castclass */
			inline_costs += 1;
			cfg->disable_ssa = TRUE;
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
		case CEE_MKREFANY:
			g_error ("opcode 0x%02x not handled", *ip);
			break;
		case CEE_LDTOKEN: {
			gpointer handle;
			MonoClass *handle_class;

			CHECK_STACK_OVF (1);

			n = read32 (ip + 1);

			handle = mono_ldtoken (image, n, &handle_class);
			mono_class_init (handle_class);

			if (((cfg->opt & MONO_OPT_SAHRED) || mono_compile_aot)) {
				int temp;
				MonoInst *res, *store, *addr, *vtvar, *iargs [2];

				vtvar = mono_compile_create_var (cfg, &handle_class->byval_arg, OP_LOCAL); 

				NEW_IMAGECONST (cfg, iargs [0], image);
				NEW_ICONST (cfg, iargs [1], n);
				temp = mono_emit_jit_icall (cfg, bblock, mono_ldtoken_wrapper, iargs, ip);
				NEW_TEMPLOAD (cfg, res, temp);
				NEW_TEMPLOADA (cfg, addr, vtvar->inst_c0);
				NEW_INDSTORE (cfg, store, addr, res, &mono_defaults.int_class->byval_arg);
				MONO_ADD_INS (bblock, store);
				NEW_TEMPLOAD (cfg, ins, vtvar->inst_c0);
			} else {
				if ((ip [5] == CEE_CALL) && (cmethod = mono_get_method (image, read32 (ip + 6), NULL)) &&
						(cmethod->klass == mono_defaults.monotype_class->parent) &&
						(strcmp (cmethod->name, "GetTypeFromHandle") == 0)) {
					MonoClass *tclass = mono_class_from_mono_type (handle);
					mono_class_init (tclass);
					NEW_PCONST (cfg, ins, mono_type_get_object (cfg->domain, handle));
					ins->type = STACK_OBJ;
					ins->klass = cmethod->klass;
					ip += 5;
				} else {
					NEW_PCONST (cfg, ins, handle);
					ins->type = STACK_VTYPE;
					ins->klass = handle_class;
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
			ip++;
			break;
		case CEE_ENDFINALLY:
			/* FIXME: check stack state */
			MONO_INST_NEW (cfg, ins, *ip);
			MONO_ADD_INS (bblock, ins);
			ins->cil_code = ip++;
			start_new_bblock = 1;
			break;
		case CEE_LEAVE:
		case CEE_LEAVE_S:
			if (*ip == CEE_LEAVE) {
				target = ip + 5 + (gint32)read32(ip + 1);
			} else {
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

			/* fixme: call fault handler ? */

			if ((tblock = mono_find_final_block (cfg, ip, target, MONO_EXCEPTION_CLAUSE_FINALLY))) {
				link_bblock (cfg, bblock, tblock);
				MONO_INST_NEW (cfg, ins, OP_HANDLER);
				ins->cil_code = ip;
				ins->inst_target_bb = tblock;
				MONO_ADD_INS (bblock, ins);
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

			switch (ip [1]) {

			case CEE_MONO_FUNC1: {
				int temp;
				gpointer func = NULL;
				CHECK_STACK (1);
				sp--;

				switch (ip [2]) {
				case MONO_MARSHAL_CONV_STR_LPWSTR:
					func = mono_string_to_utf16;
					break;
				case MONO_MARSHAL_CONV_LPWSTR_STR:
					func = mono_string_from_utf16;
					break;
				case MONO_MARSHAL_CONV_LPSTR_STR:
					func = mono_string_new_wrapper;
					break;
				case MONO_MARSHAL_CONV_STR_LPTSTR:
				case MONO_MARSHAL_CONV_STR_LPSTR:
					func = mono_string_to_utf8;
					break;
				case MONO_MARSHAL_CONV_STR_BSTR:
					func = mono_string_to_bstr;
					break;
				case MONO_MARSHAL_CONV_STR_TBSTR:
				case MONO_MARSHAL_CONV_STR_ANSIBSTR:
					func = mono_string_to_ansibstr;
					break;
				case MONO_MARSHAL_CONV_SB_LPSTR:
					func = mono_string_builder_to_utf8;
					break;
				case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
					func = mono_array_to_savearray;
					break;
				case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
					func = mono_array_to_lparray;
					break;
				case MONO_MARSHAL_CONV_DEL_FTN:
					func = mono_delegate_to_ftnptr;
					break;
				case MONO_MARSHAL_CONV_STRARRAY_STRLPARRAY:
					func = mono_marshal_string_array;
					break;
				default:
					g_warning ("unknown conversion %d\n", ip [2]);
					g_assert_not_reached ();
				}

				temp = mono_emit_jit_icall (cfg, bblock, func, sp, ip);
				NEW_TEMPLOAD (cfg, *sp, temp);
				sp++;

				ip += 3;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_MONO_PROC2: {
				gpointer func = NULL;
				CHECK_STACK (2);
				sp -= 2;

				switch (ip [2]) {
				case MONO_MARSHAL_CONV_LPSTR_SB:
					func = mono_string_utf8_to_builder;
					break;
				case MONO_MARSHAL_FREE_ARRAY:
					func = mono_marshal_free_array;
					break;
				default:
					g_assert_not_reached ();
				}

				mono_emit_jit_icall (cfg, bblock, func, sp, ip);
				ip += 3;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_MONO_PROC3: {
				gpointer func = NULL;
				CHECK_STACK (3);
				sp -= 3;

				switch (ip [2]) {
				case MONO_MARSHAL_CONV_STR_BYVALSTR:
					func = mono_string_to_byvalstr;
					break;
				case MONO_MARSHAL_CONV_STR_BYVALWSTR:
					func = mono_string_to_byvalwstr;
					break;
				default:
					g_assert_not_reached ();
				}

				mono_emit_jit_icall (cfg, bblock, func, sp, ip);
				ip += 3;
				inline_costs += 10 * num_calls++;
				break;
			}
			case CEE_MONO_FREE:
				CHECK_STACK (1);
				sp -= 1;
				mono_emit_jit_icall (cfg, bblock, g_free, sp, ip);
				ip += 2;
				inline_costs += 10 * num_calls++;
				break;
			case CEE_MONO_LDPTR:
				CHECK_STACK_OVF (1);
				token = read32 (ip + 2);
				NEW_PCONST (cfg, ins, mono_method_get_wrapper_data (method, token));
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 6;
				inline_costs += 10 * num_calls++;
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
				g_assert (method->signature->pinvoke); 
				CHECK_STACK (1);
				--sp;
				
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
			default:
				g_error ("opcode 0x%02x 0x%02x not handled", MONO_CUSTOM_PREFIX, ip [1]);
				break;
			}
			break;
		}
		case CEE_PREFIX1: {
			switch (ip [1]) {
			case CEE_ARGLIST:
				g_error ("opcode 0xfe 0x%02x not handled", ip [1]);
				break;
			case CEE_CEQ:
			case CEE_CGT:
			case CEE_CGT_UN:
			case CEE_CLT:
			case CEE_CLT_UN: {
				MonoInst *cmp;
				CHECK_STACK (2);
				MONO_INST_NEW (cfg, cmp, 256 + ip [1]);
				MONO_INST_NEW (cfg, ins, cmp->opcode);
				sp -= 2;
				cmp->inst_i0 = sp [0];
				cmp->inst_i1 = sp [1];
				cmp->cil_code = ip;
				type_from_op (cmp);
				CHECK_TYPE (cmp);
				cmp->opcode = OP_COMPARE;
				ins->cil_code = ip;
				ins->type = STACK_I4;
				ins->inst_i0 = cmp;
				*sp++ = ins;
				ip += 2;
				break;
			}
			case CEE_LDFTN: {
				MonoInst *argconst;
				int temp;

				CHECK_STACK_OVF (1);
				n = read32 (ip + 2);
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					cmethod = mono_method_get_wrapper_data (method, n);
				else {
					cmethod = mono_get_method (image, n, NULL);

					/*
					 * We can't do this in mono_ldftn, since it is used in
					 * the synchronized wrapper, leading to an infinite loop.
					 */
					if (cmethod->iflags & METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED)
						cmethod = mono_marshal_get_synchronized_wrapper (cmethod);
				}

				mono_class_init (cmethod->klass);
				handle_loaded_temps (cfg, bblock, stack_start, sp);

				NEW_METHODCONST (cfg, argconst, cmethod);
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
				n = read32 (ip + 2);
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					cmethod = mono_method_get_wrapper_data (method, n);
				else
					cmethod = mono_get_method (image, n, NULL);

				mono_class_init (cmethod->klass);
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
				NEW_ARGLOAD (cfg, ins, read16 (ip + 2));
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDARGA:
				CHECK_STACK_OVF (1);
				NEW_ARGLOADA (cfg, ins, read16 (ip + 2));
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_STARG:
				CHECK_STACK (1);
				--sp;
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				n = read16 (ip + 2);
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
				NEW_LOCLOAD (cfg, ins, read16 (ip + 2));
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_LDLOCA:
				CHECK_STACK_OVF (1);
				NEW_LOCLOADA (cfg, ins, read16 (ip + 2));
				ins->cil_code = ip;
				*sp++ = ins;
				ip += 4;
				break;
			case CEE_STLOC:
				CHECK_STACK (1);
				--sp;
				n = read16 (ip + 2);
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
				MONO_INST_NEW (cfg, ins, 256 + ip [1]);
				ins->inst_left = *sp;
				ins->cil_code = ip;

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
				for (cc = 0; cc < header->num_clauses; ++cc) {
					clause = &header->clauses [cc];
					if ((clause->flags & MONO_EXCEPTION_CLAUSE_FILTER) &&
					    (!nearest || (clause->token_or_filter > nearest->token_or_filter))) {
						nearest = clause;
						nearest_num = cc;
					}
				}
				g_assert (nearest);
				filter_lengths [nearest_num] = (ip - header->code) -  nearest->token_or_filter;

				break;
			}
			case CEE_UNALIGNED_:
				ins_flag |= MONO_INST_UNALIGNED;
				ip += 3;
				break;
			case CEE_VOLATILE_:
				ins_flag |= MONO_INST_VOLATILE;
				ip += 2;
				break;
			case CEE_TAIL_:
				ins_flag |= MONO_INST_TAILCALL;
				ip += 2;
				break;
			case CEE_INITOBJ:
				CHECK_STACK (1);
				--sp;
				token = read32 (ip + 2);
				if (method->wrapper_type != MONO_WRAPPER_NONE)
					klass = mono_method_get_wrapper_data (method, token);
				else
					klass = mono_class_get (image, token);
				handle_initobj (cfg, bblock, *sp, NULL, klass, stack_start, sp);
				ip += 6;
				inline_costs += 1;
				break;
			case CEE_CPBLK:
			case CEE_INITBLK: {
				MonoInst *iargs [3];
				CHECK_STACK (3);
				sp -= 3;
				iargs [0] = sp [0];
				iargs [1] = sp [1];
				iargs [2] = sp [2];
				handle_loaded_temps (cfg, bblock, stack_start, sp);
				if (ip [1] == CEE_CPBLK) {
					mono_emit_jit_icall (cfg, bblock, helper_memcpy, iargs, ip);
				} else {
					mono_emit_jit_icall (cfg, bblock, helper_memset, iargs, ip);
				}
				ip += 2;
				inline_costs += 1;
				break;
			}
			case CEE_RETHROW: {
				MonoInst *load;
				/* FIXME: check we are in a catch handler */
				NEW_TEMPLOAD (cfg, load, cfg->exvar->inst_c0);
				load->cil_code = ip;
				MONO_INST_NEW (cfg, ins, CEE_THROW);
				ins->inst_left = load;
				ins->cil_code = ip;
				MONO_ADD_INS (bblock, ins);
				sp = stack_start;
				start_new_bblock = 1;
				ip += 2;
				break;
			}
			case CEE_SIZEOF:
				CHECK_STACK_OVF (1);
				token = read32 (ip + 2);
				if (mono_metadata_token_table (token) == MONO_TABLE_TYPESPEC) {
					MonoType *type = mono_type_create_from_typespec (image, token);
					token = mono_type_size (type, &align);
					mono_metadata_free_type (type);
				} else {
					MonoClass *szclass = mono_class_get (image, token);
					mono_class_init (szclass);
					token = mono_class_value_size (szclass, &align);
				}
				NEW_ICONST (cfg, ins, token);
				ins->cil_code = ip;
				*sp++= ins;
				ip += 6;
				break;
			case CEE_REFANYTYPE:
				g_error ("opcode 0xfe 0x%02x not handled", ip [1]);
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
	link_bblock (cfg, bblock, end_bblock);

	if (cfg->method == method && cfg->domainvar) {
		MonoCallInst *call;
		MonoInst *store;

		MONO_INST_NEW_CALL (cfg, call, CEE_CALL);
		call->signature = helper_sig_domain_get;
		call->inst.type = STACK_PTR;
		call->fptr = mono_domain_get;
		NEW_TEMPSTORE (cfg, store, cfg->domainvar->inst_c0, (MonoInst*)call);
		
		MONO_ADD_INS (init_localsbb, store);
	}

	if (header->init_locals) {
		MonoInst *store;
		for (i = 0; i < header->num_locals; ++i) {
			int t = header->locals [i]->type;
			if (t == MONO_TYPE_VALUETYPE && header->locals [i]->data.klass->enumtype)
				t = header->locals [i]->data.klass->enum_basetype->type;
			/* FIXME: use initobj for valuetypes, handle pointers, long, float. */
			if (t >= MONO_TYPE_BOOLEAN && t <= MONO_TYPE_U4) {
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
			} else if (t == MONO_TYPE_VALUETYPE) {
				NEW_LOCLOADA (cfg, ins, i);
				handle_initobj (cfg, init_localsbb, ins, NULL, mono_class_from_mono_type (header->locals [i]), NULL, NULL);
				break;
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

	/* we compute regions here, because the length of filter clauses is not known in advance.
	* It is computed in the CEE_ENDFILTER case in the above switch statement*/
	if (cfg->method == method) {
		MonoBasicBlock *bb;
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb->region = mono_find_block_region (cfg, bb->real_offset, filter_lengths);
			if (cfg->verbose_level > 2)
				g_print ("REGION BB%d IL_%04x ID_%08X\n", bb->block_num, bb->real_offset, bb->region);
		}
	} else {
		g_hash_table_destroy (bbhash);
	}

	return inline_costs;

 inline_failure:
	if (cfg->method != method) 
		g_hash_table_destroy (bbhash);
	return -1;

 unverified:
	if (cfg->method != method) 
		g_hash_table_destroy (bbhash);
	g_error ("Invalid IL code at IL%04x in %s: %s\n", ip - header->code, 
		 mono_method_full_name (method, TRUE), mono_disasm_code_one (NULL, method, ip, NULL));
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
		printf ("[%d]", tree->inst_c0);
		break;
	case OP_I8CONST:
		printf ("[%lld]", tree->inst_l);
		break;
	case OP_R8CONST:
		printf ("[%f]", *(double*)tree->inst_p0);
		break;
	case OP_R4CONST:
		printf ("[%f]", *(float*)tree->inst_p0);
		break;
	case OP_ARG:
	case OP_LOCAL:
		printf ("[%d]", tree->inst_c0);
		break;
	case OP_REGOFFSET:
		printf ("[0x%x(%s)]", tree->inst_offset, mono_arch_regname (tree->inst_basereg));
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
		break;
	}
	case OP_PHI: {
		int i;
		printf ("[%d (", tree->inst_c0);
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
	case CEE_BR:
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
		if (arity) {
			mono_print_tree (tree->inst_left);
			if (arity > 1)
				mono_print_tree (tree->inst_right);
		}
		break;
	}

	if (arity)
		printf (")");
}

static void
create_helper_signature (void)
{
	/* FIXME: set call conv */
	/* MonoArray * mono_array_new (MonoDomain *domain, MonoClass *klass, gint32 len) */
	helper_sig_newarr = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	helper_sig_newarr->params [0] = helper_sig_newarr->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_newarr->ret = &mono_defaults.object_class->byval_arg;
	helper_sig_newarr->params [2] = &mono_defaults.int32_class->byval_arg;
	helper_sig_newarr->pinvoke = 1;

	/* MonoObject * mono_object_new (MonoDomain *domain, MonoClass *klass) */
	helper_sig_object_new = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_object_new->params [0] = helper_sig_object_new->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_object_new->ret = &mono_defaults.object_class->byval_arg;
	helper_sig_object_new->pinvoke = 1;

	/* void* mono_method_compile (MonoMethod*) */
	helper_sig_compile = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_compile->params [0] = helper_sig_compile->ret = &mono_defaults.int_class->byval_arg;
	helper_sig_compile->pinvoke = 1;

	/* void* mono_ldvirtfn (MonoObject *, MonoMethod*) */
	helper_sig_compile_virt = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_compile_virt->params [0] = &mono_defaults.object_class->byval_arg;
	helper_sig_compile_virt->params [1] = helper_sig_compile_virt->ret = &mono_defaults.int_class->byval_arg;
	helper_sig_compile_virt->pinvoke = 1;

	/* MonoString* mono_ldstr (MonoDomain *domain, MonoImage *image, guint32 str_index) */
	helper_sig_ldstr = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	helper_sig_ldstr->params [0] = helper_sig_ldstr->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_ldstr->params [2] = &mono_defaults.int32_class->byval_arg;
	helper_sig_ldstr->ret = &mono_defaults.object_class->byval_arg;
	helper_sig_ldstr->pinvoke = 1;

	/* MonoDomain *mono_domain_get (void) */
	helper_sig_domain_get = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	helper_sig_domain_get->ret = &mono_defaults.int_class->byval_arg;
	helper_sig_domain_get->pinvoke = 1;

	/* long amethod (long, long) */
	helper_sig_long_long_long = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_long_long_long->params [0] = helper_sig_long_long_long->params [1] = 
		&mono_defaults.int64_class->byval_arg;
	helper_sig_long_long_long->ret = &mono_defaults.int64_class->byval_arg;
	helper_sig_long_long_long->pinvoke = 1;

	/* object  amethod (intptr) */
	helper_sig_obj_ptr = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_obj_ptr->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_obj_ptr->ret = &mono_defaults.object_class->byval_arg;
	helper_sig_obj_ptr->pinvoke = 1;

	/* void amethod (intptr) */
	helper_sig_void_ptr = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_void_ptr->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_void_ptr->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_void_ptr->pinvoke = 1;

	/* void amethod (MonoObject *obj) */
	helper_sig_void_obj = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_void_obj->params [0] = &mono_defaults.object_class->byval_arg;
	helper_sig_void_obj->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_void_obj->pinvoke = 1;

	/* intptr amethod (void) */
	helper_sig_ptr_void = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	helper_sig_ptr_void->ret = &mono_defaults.int_class->byval_arg;
	helper_sig_ptr_void->pinvoke = 1;

	/* void  amethod (intptr, intptr) */
	helper_sig_void_ptr_ptr = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_void_ptr_ptr->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_void_ptr_ptr->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_void_ptr_ptr->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_void_ptr_ptr->pinvoke = 1;

	/* void  amethod (intptr, intptr, intptr) */
	helper_sig_void_ptr_ptr_ptr = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	helper_sig_void_ptr_ptr_ptr->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_void_ptr_ptr_ptr->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_void_ptr_ptr_ptr->params [2] = &mono_defaults.int_class->byval_arg;
	helper_sig_void_ptr_ptr_ptr->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_void_ptr_ptr_ptr->pinvoke = 1;

	/* intptr  amethod (intptr, intptr) */
	helper_sig_ptr_ptr_ptr = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_ptr_ptr_ptr->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_ptr_ptr_ptr->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_ptr_ptr_ptr->ret = &mono_defaults.int_class->byval_arg;
	helper_sig_ptr_ptr_ptr->pinvoke = 1;

	/* IntPtr  amethod (object) */
	helper_sig_ptr_obj = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_ptr_obj->params [0] = &mono_defaults.object_class->byval_arg;
	helper_sig_ptr_obj->ret = &mono_defaults.int_class->byval_arg;
	helper_sig_ptr_obj->pinvoke = 1;

	/* long amethod (long, guint32) */
	helper_sig_long_long_int = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_long_long_int->params [0] = &mono_defaults.int64_class->byval_arg;
	helper_sig_long_long_int->params [1] = &mono_defaults.int32_class->byval_arg;
	helper_sig_long_long_int->ret = &mono_defaults.int64_class->byval_arg;
	helper_sig_long_long_int->pinvoke = 1;

	/* ulong amethod (double) */
	helper_sig_ulong_double = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_ulong_double->params [0] = &mono_defaults.double_class->byval_arg;
	helper_sig_ulong_double->ret = &mono_defaults.uint64_class->byval_arg;
	helper_sig_ulong_double->pinvoke = 1;

	/* long amethod (double) */
	helper_sig_long_double = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_long_double->params [0] = &mono_defaults.double_class->byval_arg;
	helper_sig_long_double->ret = &mono_defaults.int64_class->byval_arg;
	helper_sig_long_double->pinvoke = 1;

	/* uint amethod (double) */
	helper_sig_uint_double = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_uint_double->params [0] = &mono_defaults.double_class->byval_arg;
	helper_sig_uint_double->ret = &mono_defaults.uint32_class->byval_arg;
	helper_sig_uint_double->pinvoke = 1;

	/* int amethod (double) */
	helper_sig_int_double = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
	helper_sig_int_double->params [0] = &mono_defaults.double_class->byval_arg;
	helper_sig_int_double->ret = &mono_defaults.int32_class->byval_arg;
	helper_sig_int_double->pinvoke = 1;

	/* void  initobj (intptr, int size) */
	helper_sig_initobj = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
	helper_sig_initobj->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_initobj->params [1] = &mono_defaults.int32_class->byval_arg;
	helper_sig_initobj->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_initobj->pinvoke = 1;

	/* void  memcpy (intptr, intptr, int size) */
	helper_sig_memcpy = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	helper_sig_memcpy->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_memcpy->params [1] = &mono_defaults.int_class->byval_arg;
	helper_sig_memcpy->params [2] = &mono_defaults.int32_class->byval_arg;
	helper_sig_memcpy->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_memcpy->pinvoke = 1;

	/* void  memset (intptr, int val, int size) */
	helper_sig_memset = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	helper_sig_memset->params [0] = &mono_defaults.int_class->byval_arg;
	helper_sig_memset->params [1] = &mono_defaults.int32_class->byval_arg;
	helper_sig_memset->params [2] = &mono_defaults.int32_class->byval_arg;
	helper_sig_memset->ret = &mono_defaults.void_class->byval_arg;
	helper_sig_memset->pinvoke = 1;
}

static GHashTable *jit_icall_hash_name = NULL;
static GHashTable *jit_icall_hash_addr = NULL;

MonoJitICallInfo *
mono_find_jit_icall_by_name (const char *name)
{
	g_assert (jit_icall_hash_name);

	//printf ("lookup addr %s %p\n", name, g_hash_table_lookup (jit_icall_hash_name, name));
	return g_hash_table_lookup (jit_icall_hash_name, name);
}

MonoJitICallInfo *
mono_find_jit_icall_by_addr (gconstpointer addr)
{
	g_assert (jit_icall_hash_addr);

	return g_hash_table_lookup (jit_icall_hash_addr, (gpointer)addr);
}

MonoJitICallInfo *
mono_register_jit_icall (gconstpointer func, const char *name, MonoMethodSignature *sig, gboolean is_save)
{
	MonoJitICallInfo *info;
	MonoMethod *wrapper;
	char *n;
	
	g_assert (func);
	g_assert (name);

	if (!jit_icall_hash_name) {
		jit_icall_hash_name = g_hash_table_new (g_str_hash, g_str_equal);
		jit_icall_hash_addr = g_hash_table_new (NULL, NULL);
	}

	if (g_hash_table_lookup (jit_icall_hash_name, name)) {
		g_warning ("jit icall already defined \"%s\"\n", name);
		g_assert_not_reached ();
	}

	info = g_new (MonoJitICallInfo, 1);
	
	info->name = g_strdup (name);
	info->func = func;
	info->sig = sig;
		
	if (is_save
#ifdef MONO_USE_EXC_TABLES
	    || mono_arch_has_unwind_info (func)
#endif
	    ) {
		info->wrapper = func;
	} else {
		g_assert (sig);
		n = g_strdup_printf ("__icall_wrapper_%s", name);	
		wrapper = mono_marshal_get_icall_wrapper (sig, n, func);
		info->wrapper = mono_jit_compile_method (wrapper);
		g_free (n);
	}

	g_hash_table_insert (jit_icall_hash_name, info->name, info);
	g_hash_table_insert (jit_icall_hash_addr, (gpointer)func, info);
	if (func != info->wrapper)
		g_hash_table_insert (jit_icall_hash_addr, (gpointer)info->wrapper, info);

	return info;
}

static GHashTable *emul_opcode_hash = NULL;

static MonoJitICallInfo *
mono_find_jit_opcode_emulation (int opcode)
{
	if  (emul_opcode_hash)
		return g_hash_table_lookup (emul_opcode_hash, (gpointer)opcode);
	else
		return NULL;
}

void
mono_register_opcode_emulation (int opcode, MonoMethodSignature *sig, gpointer func)
{
	MonoJitICallInfo *info;
	char *name;

	if (!emul_opcode_hash)
		emul_opcode_hash = g_hash_table_new (NULL, NULL);

	g_assert (!sig->hasthis);
	g_assert (sig->param_count < 3);

	name = g_strdup_printf ("__emulate_%s",  mono_inst_name (opcode));

	info = mono_register_jit_icall (func, name, sig, FALSE);

	g_free (name);

	g_hash_table_insert (emul_opcode_hash, (gpointer)opcode, info);
}

static void
decompose_foreach (MonoInst *tree, gpointer data) 
{
	static MonoJitICallInfo *newarr_info = NULL;

	switch (tree->opcode) {
	case CEE_NEWARR: {
		MonoCompile *cfg = data;
		MonoInst *iargs [3];

		NEW_DOMAINCONST (cfg, iargs [0]);
		NEW_CLASSCONST (cfg, iargs [1], tree->inst_newa_class);
		iargs [2] = tree->inst_newa_len;

		if (!newarr_info) {
			newarr_info =  mono_find_jit_icall_by_addr (mono_array_new);
			g_assert (newarr_info);
		}

		mono_emulate_opcode (cfg, tree, iargs, newarr_info);
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

#if 0
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
#endif

static void
print_dfn (MonoCompile *cfg) {
	int i, j;
	char *code;
	MonoBasicBlock *bb;

	g_print ("IR code for method %s\n", mono_method_full_name (cfg->method, TRUE));

	for (i = 0; i < cfg->num_bblocks; ++i) {
		bb = cfg->bblocks [i];
		if (bb->cil_code) {
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
		} else
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

/*
 * returns the offset used by spillvar. It allocates a new
 * spill variable if necessary. 
 */
int
mono_spillvar_offset (MonoCompile *cfg, int spillvar)
{
	MonoSpillInfo **si, *info;
	int i = 0;

	si = &cfg->spill_info; 
	
	while (i <= spillvar) {

		if (!*si) {
			*si = info = mono_mempool_alloc (cfg->mempool, sizeof (MonoSpillInfo));
			info->next = NULL;
			cfg->stack_offset -= sizeof (gpointer);
			info->offset = cfg->stack_offset;
		}

		if (i == spillvar)
			return (*si)->offset;

		i++;
		si = &(*si)->next;
	}

	g_assert_not_reached ();
	return 0;
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
	if (cfg->rs)
		mono_regstate_free (cfg->rs);
	mono_mempool_destroy (cfg->mempool);
	g_list_free (cfg->ldstr_list);

	g_free (cfg->varinfo);
	g_free (cfg->vars);
	g_free (cfg);
}

gpointer 
mono_get_lmf_addr (void)
{
	MonoJitTlsData *jit_tls;	

	if ((jit_tls = TlsGetValue (mono_jit_tls_id)))
		return &jit_tls->lmf;

	g_assert_not_reached ();
	return NULL;
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
	MonoJitTlsData *jit_tls = TlsGetValue (mono_jit_tls_id);
	
	g_free (jit_tls);

	ExitThread (-1);
}

static void
mono_thread_start_cb (guint32 tid, gpointer stack_start, gpointer func)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	TlsSetValue (mono_jit_tls_id, jit_tls);

	jit_tls->abort_func = mono_thread_abort;
	jit_tls->end_of_stack = stack_start;

	lmf = g_new0 (MonoLMF, 1);
	lmf->ebp = -1;

	jit_tls->lmf = lmf;
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
mono_thread_attach_cb (guint32 tid, gpointer stack_start)
{
	MonoJitTlsData *jit_tls;
	MonoLMF *lmf;

	jit_tls = g_new0 (MonoJitTlsData, 1);

	TlsSetValue (mono_jit_tls_id, jit_tls);

	jit_tls->abort_func = mono_thread_abort_dummy;
	jit_tls->end_of_stack = stack_start;

	lmf = g_new0 (MonoLMF, 1);
	lmf->ebp = -1;

	jit_tls->lmf = lmf;
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

static void
dec_foreach (MonoInst *tree, MonoCompile *cfg) {
	MonoJitICallInfo *info;

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
	decompose_foreach (tree, cfg);
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
}

static void 
replace_basic_block (MonoBasicBlock *bb, MonoBasicBlock *orig,  MonoBasicBlock *repl)
{
	int i, j;

	for (i = 0; i < bb->out_count; i++) {
		MonoBasicBlock *ob = bb->out_bb [i];
		for (j = 0; j < ob->in_count; j++) {
			if (ob->in_bb [j] == orig)
				ob->in_bb [j] = repl;
		}
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
optimize_branches (MonoCompile *cfg) {
	int changed = FALSE;
	MonoBasicBlock *bb, *bbn;

	do {
		changed = FALSE;

		/* we skip the entry block (exit is handled specially instead ) */
		for (bb = cfg->bb_entry->next_bb; bb; bb = bb->next_bb) {

			if (bb->out_count == 1) {
				bbn = bb->out_bb [0];

				if (bb->region == bbn->region && bb->next_bb == bbn) {
				/* the block are in sequence anyway ... */

					/* 
					 * miguel: I do not understand what the test below does, could we
					 * use a macro, or a comment here?  opcode > CEE_BEQ && <= BLT_UN
					 *
					 * It could also test for bb->last_in only once, and the value
					 * could be cached (last_ins->opcode)
					 */
					if (bb->last_ins && (bb->last_ins->opcode == CEE_BR || (
						(bb->last_ins && bb->last_ins->opcode >= CEE_BEQ && bb->last_ins->opcode <= CEE_BLT_UN)))) {
						bb->last_ins->opcode = CEE_NOP;
						changed = TRUE;
						if (cfg->verbose_level > 2)
							g_print ("br removal triggered %d -> %d\n", bb->block_num, bbn->block_num);
					}
					/* fixme: this causes problems with inlining */
					if (bbn->in_count == 1) {

						if (bbn != cfg->bb_exit) {
							if (cfg->verbose_level > 2)
								g_print ("block merge triggered %d -> %d\n", bb->block_num, bbn->block_num);
							merge_basic_blocks (bb, bbn);
							changed = TRUE;
						}

						//mono_print_bb_code (bb);
					}
				} else {
					if (bb->last_ins && bb->last_ins->opcode == CEE_BR) {
						bbn = bb->last_ins->inst_target_bb;
						if (bb->region == bbn->region && bbn->code && bbn->code->opcode == CEE_BR) {
							/*
							if (cfg->verbose_level > 2)
								g_print ("in %s branch to branch triggered %d -> %d\n", cfg->method->name, bb->block_num, bbn->block_num);
							bb->out_bb [0] = bb->last_ins->inst_target_bb = bbn->code->inst_target_bb;
							changed = TRUE;*/
						}
					}
				}
			} else if (bb->out_count == 2) {
				/* fixme: this does not correctly unlink the blocks, so we get serious problems in idom code */
				if (0 && bb->last_ins && bb->last_ins->opcode >= CEE_BEQ && bb->last_ins->opcode <= CEE_BLT_UN) {
					bbn = bb->last_ins->inst_true_bb;
					if (bb->region == bbn->region && bbn->code && bbn->code->opcode == CEE_BR) {
						if (cfg->verbose_level > 2)
							g_print ("cbranch to branch triggered %d -> %d (0x%02x)\n", bb->block_num, 
								 bbn->block_num, bbn->code->opcode);
						 
						if (bb->out_bb [0] == bbn) {
							bb->out_bb [0] = bbn->code->inst_target_bb;
						} else if (bb->out_bb [1] == bbn) {
							bb->out_bb [1] = bbn->code->inst_target_bb;
						}
						bb->last_ins->inst_true_bb = bbn->code->inst_target_bb;
						changed = TRUE;
					}
				}
			}
		}
	} while (changed);
}

static void
mono_compile_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i;

	header = ((MonoMethodNormal *)cfg->method)->header;

	sig = cfg->method->signature;
	
	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		cfg->ret = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
		cfg->ret->opcode = OP_RETARG;
		cfg->ret->inst_vtype = sig->ret;
		cfg->ret->klass = mono_class_from_mono_type (sig->ret);
	}

	if (sig->hasthis)
		mono_compile_create_var (cfg, &cfg->method->klass->this_arg, OP_ARG);

	for (i = 0; i < sig->param_count; ++i)
		mono_compile_create_var (cfg, sig->params [i], OP_ARG);

	cfg->locals_start = cfg->num_varinfo;

	for (i = 0; i < header->num_locals; ++i)
		mono_compile_create_var (cfg, header->locals [i], OP_LOCAL);
}

#if 0
static void
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
#endif

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
				g_warning ("unabled to label tree %p", tree);
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

	if (mono_trace_coverage)
		mono_allocate_coverage_info (cfg->method, cfg->num_bblocks);

	code = mono_arch_emit_prolog (cfg);

	if (mono_jit_profile)
		code = mono_arch_instrument_prolog (cfg, mono_profiler_method_enter, code, FALSE);

	cfg->code_len = code - cfg->native_code;
	cfg->prolog_end = cfg->code_len;

	mono_debug_open_method (cfg);
	     
	/* emit code all basic blocks */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		bb->native_offset = cfg->code_len;
		mono_arch_output_basic_block (cfg, bb);
	}
	cfg->bb_exit->native_offset = cfg->code_len;

	code = cfg->native_code + cfg->code_len;

	max_epilog_size = mono_arch_max_epilog_size (cfg);

	/* we always allocate code in cfg->domain->code_mp to increase locality */
	cfg->code_size = cfg->code_len + max_epilog_size;
	/* fixme: align to MONO_ARCH_CODE_ALIGNMENT */
	code = mono_mempool_alloc (cfg->domain->code_mp, cfg->code_size);
	memcpy (code, cfg->native_code, cfg->code_len);
	g_free (cfg->native_code);
	cfg->native_code = code;
	code = cfg->native_code + cfg->code_len;
  
	/* g_assert (((int)cfg->native_code & (MONO_ARCH_CODE_ALIGNMENT - 1)) == 0); */

	cfg->epilog_begin = cfg->code_len;

	if (mono_jit_profile)
		code = mono_arch_instrument_epilog (cfg, mono_profiler_method_leave, code, FALSE);

	cfg->code_len = code - cfg->native_code;

	mono_arch_emit_epilog (cfg);

	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_ABS: {
			MonoJitICallInfo *info = mono_find_jit_icall_by_addr (patch_info->data.target);
			if (info) {
				//printf ("TEST %s %p\n", info->name, patch_info->data.target);
				patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
				patch_info->data.name = info->name;
			}
			break;
		}
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table = g_new (gpointer, patch_info->table_size);
			patch_info->ip.i = patch_info->ip.label->inst_c0;
			for (i = 0; i < patch_info->table_size; i++) {
				table [i] = (gpointer)patch_info->data.table [i]->native_offset;
			}
			patch_info->data.target = table;
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}
       
	if (cfg->verbose_level > 1)
		g_print ("Method %s::%s emmitted at %p to %p\n", cfg->method->klass->name, 
			 cfg->method->name, cfg->native_code, cfg->native_code + cfg->code_len);

	mono_arch_patch_code (cfg->method, cfg->domain, cfg->native_code, cfg->patch_info);

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
			if (arity > 1) {
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

MonoCompile*
mini_method_compile (MonoMethod *method, guint32 opts, MonoDomain *domain, int parts)
{
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	guint8 *ip = (guint8 *)header->code;
	MonoCompile *cfg;
	MonoJitInfo *jinfo;
	int dfn = 0, i, code_size_ratio;

	mono_jit_stats.methods_compiled++;

	cfg = g_new0 (MonoCompile, 1);
	cfg->method = method;
	cfg->mempool = mono_mempool_new ();
	cfg->opt = opts;
	cfg->bb_hash = g_hash_table_new (g_direct_hash, NULL);
	cfg->domain = domain;
	cfg->verbose_level = mini_verbose;

	/*
	 * create MonoInst* which represents arguments and local variables
	 */
	mono_compile_create_vars (cfg);

	if (cfg->verbose_level > 2)
		g_print ("converting method %s\n", mono_method_full_name (method, TRUE));

	if ((i = mono_method_to_ir (cfg, method, NULL, NULL, cfg->locals_start, NULL, NULL, NULL, 0)) < 0) {
		mono_destroy_compile (cfg);
		return NULL;
	}

	mono_jit_stats.basic_blocks += cfg->num_bblocks;
	mono_jit_stats.max_basic_blocks = MAX (cfg->num_bblocks, mono_jit_stats.max_basic_blocks);

	/*g_print ("numblocks = %d\n", cfg->num_bblocks);*/

	/* Depth-first ordering on basic blocks */
	cfg->bblocks = mono_mempool_alloc (cfg->mempool, sizeof (MonoBasicBlock*) * (cfg->num_bblocks + 1));

	if (cfg->opt & MONO_OPT_BRANCH)
		optimize_branches (cfg);

	df_visit (cfg->bb_entry, &dfn, cfg->bblocks);
	if (cfg->num_bblocks != dfn + 1) {
		if (cfg->verbose_level > 1)
			g_print ("unreachable code?\n");
		cfg->num_bblocks = dfn + 1;
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
	if (cfg->opt & (MONO_OPT_DEADCE)) {
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

	if ((cfg->opt & MONO_OPT_CONSPROP) ||  (cfg->opt & MONO_OPT_COPYPROP)) {
		if (cfg->comp_done & MONO_COMP_SSA) {
			mono_ssa_cprop (cfg);
		} else {
			mono_local_cprop (cfg);
		}
	}

	if (cfg->comp_done & MONO_COMP_SSA) {			
		mono_ssa_deadce (cfg);

		//mono_ssa_strength_reduction (cfg);

		mono_ssa_remove (cfg);

		if (cfg->opt & MONO_OPT_BRANCH)
			optimize_branches (cfg);
	}

	/* after SSA removal */
	if (parts == 3)
		return cfg;
	
	decompose_pass (cfg);

	if (cfg->opt & MONO_OPT_LINEARS) {
		GList *vars, *regs;

		/* fixme: maybe we can avoid to compute livenesss here if already computed ? */
		cfg->comp_done &= ~MONO_COMP_LIVENESS;
		if (!(cfg->comp_done & MONO_COMP_LIVENESS))
			mono_analyze_liveness (cfg);
		
		if ((vars = mono_arch_get_allocatable_int_vars (cfg))) {
			regs = mono_arch_get_global_int_regs (cfg);
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
		mono_disassemble_code (cfg->native_code, cfg->code_len, id + 3);
		g_free (id);
	}
	
	jinfo = mono_mempool_alloc0 (cfg->domain->mp, sizeof (MonoJitInfo));

	jinfo = g_new0 (MonoJitInfo, 1);
	jinfo->method = method;
	jinfo->code_start = cfg->native_code;
	jinfo->code_size = cfg->code_len;
	jinfo->used_regs = cfg->used_int_regs;

	if (header->num_clauses) {
		int i;

		jinfo->exvar_offset = cfg->exvar? cfg->exvar->inst_offset: 0;
		jinfo->num_clauses = header->num_clauses;
		jinfo->clauses = mono_mempool_alloc0 (cfg->domain->mp, 
		        sizeof (MonoJitExceptionInfo) * header->num_clauses);

		for (i = 0; i < header->num_clauses; i++) {
			MonoExceptionClause *ec = &header->clauses [i];
			MonoJitExceptionInfo *ei = &jinfo->clauses [i];
			MonoBasicBlock *tblock;

			ei->flags = ec->flags;

			if (ei->flags == MONO_EXCEPTION_CLAUSE_FILTER) {
				tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->token_or_filter);
				g_assert (tblock);
				ei->data.filter = cfg->native_code + tblock->native_offset;
			} else {
				ei->data.token = ec->token_or_filter;
			}

			tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->try_offset);
			g_assert (tblock);
			ei->try_start = cfg->native_code + tblock->native_offset;
			tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->try_offset + ec->try_len);
			g_assert (tblock);
			ei->try_end = cfg->native_code + tblock->native_offset;
			tblock = g_hash_table_lookup (cfg->bb_hash, ip + ec->handler_offset);
			g_assert (tblock);
			ei->handler_start = cfg->native_code + tblock->native_offset;

		}
	}

	mono_jit_info_table_add (cfg->domain, jinfo);

	/* collect statistics */
	mono_jit_stats.allocated_code_size += cfg->code_len;
	code_size_ratio = cfg->code_len;
	if (code_size_ratio > mono_jit_stats.biggest_method_size) {
			mono_jit_stats.biggest_method_size = code_size_ratio;
			mono_jit_stats.biggest_method = method;
	}
	code_size_ratio = (code_size_ratio * 100) / ((MonoMethodNormal *)method)->header->code_size;
	if (code_size_ratio > mono_jit_stats.max_code_size_ratio) {
		mono_jit_stats.max_code_size_ratio = code_size_ratio;
		mono_jit_stats.max_ratio_method = method;
	}
	mono_jit_stats.native_code_size += cfg->code_len;

	return cfg;
}

static gpointer
mono_jit_compile_method (MonoMethod *method)
{
	/* FIXME: later copy the code from mono */
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoCompile *cfg;
	GHashTable *jit_code_hash;
	gpointer code;

	if (default_opt & MONO_OPT_SAHRED)
		target_domain = mono_root_domain;
	else 
		target_domain = domain;

	jit_code_hash = target_domain->jit_code_hash;

	if ((code = g_hash_table_lookup (jit_code_hash, method))) {
		mono_jit_stats.methods_lookups++;
		return code;
	}

#ifdef MONO_USE_AOT_COMPILER
	if (!mono_compile_aot) {
		mono_class_init (method->klass);
		if ((code = mono_aot_get_method (method))) {
			g_hash_table_insert (jit_code_hash, method, code);
			return code;
		}
	}
#endif

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
		if (!method->info) {
			MonoMethod *nm;

			if (!method->addr && (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
				mono_lookup_pinvoke_call (method);
#ifdef MONO_USE_EXC_TABLES
			if (mono_method_blittable (method)) {
				method->info = method->addr;
			} else {
#endif
				nm = mono_marshal_get_native_wrapper (method);
				method->info = mono_compile_method (nm);

				//if (mono_debug_format != MONO_DEBUG_FORMAT_NONE) 
				//mono_debug_add_wrapper (method, nm);
#ifdef MONO_USE_EXC_TABLES
			}
#endif
		}
		return method->info;
	} else if ((method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		const char *name = method->name;
		MonoMethod *nm;

		if (method->klass->parent == mono_defaults.multicastdelegate_class) {
			if (*name == '.' && (strcmp (name, ".ctor") == 0)) {
				/* FIXME: uhm, we need a wrapper to handle exceptions? */
				return (gpointer)mono_delegate_ctor;
			} else if (*name == 'I' && (strcmp (name, "Invoke") == 0)) {
			        nm = mono_marshal_get_delegate_invoke (method);
				return mono_jit_compile_method (nm);
			} else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
				nm = mono_marshal_get_delegate_begin_invoke (method);
				return mono_jit_compile_method (nm);
			} else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
				nm = mono_marshal_get_delegate_end_invoke (method);
				return mono_jit_compile_method (nm);
			}
		}
		return NULL;
	}

	cfg = mini_method_compile (method, default_opt, target_domain, 0);
	code = cfg->native_code;
	mono_destroy_compile (cfg);

	g_hash_table_insert (jit_code_hash, method, code);

	/* make sure runtime_init is called */
	mono_class_vtable (target_domain, method->klass);

	return code;
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
	MonoObject *(*runtime_invoke) (MonoObject *this, void **params, MonoObject **exc);
	invoke = mono_marshal_get_runtime_invoke (method);
	runtime_invoke = mono_jit_compile_method (invoke);      
	return runtime_invoke (obj, params, exc);
}

#ifdef PLATFORM_WIN32
#define GET_CONTEXT \
	struct sigcontext *ctx = (struct sigcontext*)_dummy;
#else
#define GET_CONTEXT \
	void **_p = (void **)&_dummy; \
	struct sigcontext *ctx = (struct sigcontext *)++_p;
#endif

static void
sigfpe_signal_handler (int _dummy)
{
	MonoException *exc;
	GET_CONTEXT

	exc = mono_get_exception_divide_by_zero ();
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
sigill_signal_handler (int _dummy)
{
	MonoException *exc;
	GET_CONTEXT
	exc = mono_get_exception_execution_engine ("SIGILL");
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
sigsegv_signal_handler (int _dummy)
{
	MonoException *exc;
	GET_CONTEXT

	exc = mono_get_exception_null_reference ();
	
	mono_arch_handle_exception (ctx, exc, FALSE);
}

static void
sigusr1_signal_handler (int _dummy)
{
	MonoThread *thread;
	GET_CONTEXT
	
	thread = mono_thread_current ();
        
	g_assert (thread->abort_exc);

	mono_arch_handle_exception (ctx, thread->abort_exc, FALSE);
}

static void
mono_runtime_install_handlers (void)
{
#ifndef PLATFORM_WIN32
	struct sigaction sa;
#endif

#ifdef PLATFORM_WIN32
	win32_seh_init();
	win32_seh_set_handler(SIGFPE, sigfpe_signal_handler);
	win32_seh_set_handler(SIGILL, sigill_signal_handler);
	win32_seh_set_handler(SIGSEGV, sigsegv_signal_handler);
#else /* !PLATFORM_WIN32 */

	/* libpthreads has its own implementation of sigaction(),
	 * but it seems to work well with our current exception
	 * handlers. If not we must call syscall directly instead 
	 * of sigaction */
	
	/* catch SIGFPE */
	sa.sa_handler = sigfpe_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGFPE, &sa, NULL) != -1);
	g_assert (sigaction (SIGFPE, &sa, NULL) != -1);

	/* catch SIGILL */
	sa.sa_handler = sigill_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGILL, &sa, NULL) != -1);
	g_assert (sigaction (SIGILL, &sa, NULL) != -1);

	/* catch thread abort signal */
	sa.sa_handler = sigusr1_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGILL, &sa, NULL) != -1);
	g_assert (sigaction (mono_thread_get_abort_signal (), &sa, NULL) != -1);

#if 1
	/* catch SIGSEGV */
	sa.sa_handler = sigsegv_signal_handler;
	sigemptyset (&sa.sa_mask);
	sa.sa_flags = 0;
	//g_assert (syscall (SYS_sigaction, SIGSEGV, &sa, NULL) != -1);
	g_assert (sigaction (SIGSEGV, &sa, NULL) != -1);
#endif
#endif /* PLATFORM_WIN32 */
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
mono_jit_create_remoting_trampoline (MonoMethod *method)
{
	MonoMethod *nm;
	guint8 *addr = NULL;

	if (method->signature->hasthis && (method->klass->marshalbyref || method->klass == mono_defaults.object_class)) {
		nm = mono_marshal_get_remoting_invoke (method);
		addr = mono_compile_method (nm);
	} else {
		addr = mono_compile_method (method);
	}
	return addr;
}

static CRITICAL_SECTION ms;

MonoDomain *
mini_init (const char *filename)
{
	MonoDomain *domain;
	
	metadata_section = &ms;
	InitializeCriticalSection (metadata_section);

	mono_jit_tls_id = TlsAlloc ();
	mono_thread_start_cb (GetCurrentThreadId (), (gpointer)-1, NULL);

	mono_burg_init ();

	mono_runtime_install_handlers ();

	mono_install_compile_method (mono_jit_compile_method);
	mono_install_trampoline (mono_arch_create_jit_trampoline);
	mono_install_remoting_trampoline (mono_jit_create_remoting_trampoline);
	mono_install_runtime_invoke (mono_jit_runtime_invoke);
	mono_install_handler (mono_arch_get_throw_exception ());
	mono_install_stack_walk (mono_jit_walk_stack);
	mono_install_get_config_dir ();

	domain = mono_init (filename);
	mono_init_icall ();

	mono_add_internal_call ("System.Diagnostics.StackFrame::get_frame_info", 
				ves_icall_get_frame_info);
	mono_add_internal_call ("System.Diagnostics.StackTrace::get_trace", 
				ves_icall_get_trace);
	mono_add_internal_call ("Mono.Runtime::mono_runtime_install_handlers", 
				mono_runtime_install_handlers);


	create_helper_signature ();

	mono_arch_register_lowlevel_calls ();
	mono_register_jit_icall (mono_profiler_method_enter, "mono_profiler_method_enter", NULL, TRUE);
	mono_register_jit_icall (mono_profiler_method_leave, "mono_profiler_method_leave", NULL, TRUE);

	mono_register_jit_icall (mono_get_lmf_addr, "mono_get_lmf_addr", helper_sig_ptr_void, TRUE);
	mono_register_jit_icall (mono_domain_get, "mono_domain_get", helper_sig_domain_get, TRUE);

	/* fixme: we cant hanlde vararg methods this way, because the signature is not constant */
	//mono_register_jit_icall (ves_array_element_address, "ves_array_element_address", NULL);
	//mono_register_jit_icall (mono_array_new_va, "mono_array_new_va", NULL);

	mono_register_jit_icall (mono_arch_get_throw_exception (), "mono_arch_throw_exception", helper_sig_void_obj, TRUE);
	mono_register_jit_icall (mono_arch_get_throw_exception_by_name (), "mono_arch_throw_exception_by_name", 
				 helper_sig_void_ptr, TRUE);

	/* 
	 * NOTE, NOTE, NOTE, NOTE:
	 * when adding emulation for some opcodes, remember to also add a dummy
	 * rule to the burg files, because we need the arity information to be correct.
	 */
	mono_register_opcode_emulation (OP_LMUL, helper_sig_long_long_long, mono_llmult);
	mono_register_opcode_emulation (OP_LMUL_OVF_UN, helper_sig_long_long_long, mono_llmult_ovf_un);
	mono_register_opcode_emulation (OP_LMUL_OVF, helper_sig_long_long_long, mono_llmult_ovf);
	mono_register_opcode_emulation (OP_LDIV, helper_sig_long_long_long, mono_lldiv);
	mono_register_opcode_emulation (OP_LDIV_UN, helper_sig_long_long_long, mono_lldiv_un);
	mono_register_opcode_emulation (OP_LREM, helper_sig_long_long_long, mono_llrem);
	mono_register_opcode_emulation (OP_LREM_UN, helper_sig_long_long_long, mono_llrem_un);

	mono_register_opcode_emulation (OP_LSHL, helper_sig_long_long_int, mono_lshl);
	mono_register_opcode_emulation (OP_LSHR, helper_sig_long_long_int, mono_lshr);
	mono_register_opcode_emulation (OP_LSHR_UN, helper_sig_long_long_int, mono_lshr_un);

	mono_register_opcode_emulation (OP_FCONV_TO_U8, helper_sig_ulong_double, mono_fconv_u8);
	mono_register_opcode_emulation (OP_FCONV_TO_U4, helper_sig_uint_double, mono_fconv_u4);
	mono_register_opcode_emulation (OP_FCONV_TO_OVF_I8, helper_sig_long_double, mono_fconv_ovf_i8);
	mono_register_opcode_emulation (OP_FCONV_TO_OVF_U8, helper_sig_ulong_double, mono_fconv_ovf_u8);

#if SIZEOF_VOID_P == 4
	mono_register_opcode_emulation (OP_FCONV_TO_U, helper_sig_uint_double, mono_fconv_u4);
#else
#warning "fixme: add opcode emulation"
#endif

	/* other jit icalls */
	mono_register_jit_icall (mono_class_static_field_address , "mono_class_static_field_address", 
				 helper_sig_ptr_ptr_ptr, FALSE);
	mono_register_jit_icall (mono_ldtoken_wrapper, "mono_ldtoken_wrapper", helper_sig_ptr_ptr_ptr, FALSE);
	mono_register_jit_icall (mono_ldstr, "mono_ldstr", helper_sig_ldstr, FALSE);
	mono_register_jit_icall (helper_memcpy, "helper_memcpy", helper_sig_memcpy, FALSE);
	mono_register_jit_icall (helper_memset, "helper_memset", helper_sig_memset, FALSE);
	mono_register_jit_icall (helper_initobj, "helper_initobj", helper_sig_initobj, FALSE);
	mono_register_jit_icall (mono_object_new, "mono_object_new", helper_sig_object_new, FALSE);
	mono_register_jit_icall (mono_array_new, "mono_array_new", helper_sig_newarr, FALSE);
	mono_register_jit_icall (mono_string_to_utf16, "mono_string_to_utf16", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_string_from_utf16, "mono_string_from_utf16", helper_sig_obj_ptr, FALSE);
	mono_register_jit_icall (mono_string_new_wrapper, "mono_string_new_wrapper", helper_sig_obj_ptr, FALSE);
	mono_register_jit_icall (mono_string_to_utf8, "mono_string_to_utf8", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_string_to_bstr, "mono_string_to_bstr", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_string_to_ansibstr, "mono_string_to_ansibstr", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_string_builder_to_utf8, "mono_string_builder_to_utf8", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_array_to_savearray, "mono_array_to_savearray", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_array_to_lparray, "mono_array_to_lparray", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_delegate_to_ftnptr, "mono_delegate_to_ftnptr", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_marshal_string_array, "mono_marshal_string_array", helper_sig_ptr_obj, FALSE);
	mono_register_jit_icall (mono_string_utf8_to_builder, "mono_string_utf8_to_builder", helper_sig_void_ptr_ptr, FALSE);
	mono_register_jit_icall (mono_marshal_free_array, "mono_marshal_free_array", helper_sig_void_ptr_ptr, FALSE);
	mono_register_jit_icall (mono_string_to_byvalstr, "mono_string_to_byvalstr", helper_sig_void_ptr_ptr_ptr, FALSE);
	mono_register_jit_icall (mono_string_to_byvalwstr, "mono_string_to_byvalwstr", helper_sig_void_ptr_ptr_ptr, FALSE);
	mono_register_jit_icall (g_free, "g_free", helper_sig_void_ptr, FALSE);
	mono_register_jit_icall (mono_ldftn, "mono_ldftn", helper_sig_compile, FALSE);
	mono_register_jit_icall (mono_ldvirtfn, "mono_ldvirtfn", helper_sig_compile_virt, FALSE);

	mono_runtime_init (domain, mono_thread_start_cb,
			   mono_thread_attach_cb);

	//mono_thread_attach (domain);
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
	}
}

void
mini_cleanup (MonoDomain *domain)
{
	/* 
	 * mono_runtime_cleanup() needs to be called early since
	 * it needs the execution engine still fully working (it will
	 * wait for other threads to finish).
	 */
	mono_runtime_cleanup (domain);

	mono_domain_finalize (domain);

	mono_profiler_shutdown ();

	mono_debug_cleanup ();
#ifdef PLATFORM_WIN32
	win32_seh_cleanup();
#endif

	mono_domain_unload (domain, TRUE);

	print_jit_stats ();
	DeleteCriticalSection (metadata_section);
}

void
mini_set_defaults (int verbose_level, guint32 opts)
{
	mini_verbose = verbose_level;
	default_opt = opts;
}

