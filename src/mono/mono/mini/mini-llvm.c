/*
 * mini-llvm.c: llvm "Backend" for the mono JIT
 *
 * (C) 2009 Novell, Inc.
 */

#include "mini.h"
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mempool-internals.h>

#include "llvm-c/Core.h"
#include "llvm-c/ExecutionEngine.h"

#include "mini-llvm-cpp.h"

typedef struct {
	MonoMemPool *mempool;

	LLVMValueRef got_var;

	LLVMValueRef throw_corlib_exception;

	/* Maps method names to the corresponding LLVMValueRef */
	GHashTable *emitted_method_decls;

	MonoCompile *cfg;
	LLVMValueRef lmethod;
	LLVMBasicBlockRef *bblocks, *end_bblocks;
	int sindex, default_index, ex_index;
	LLVMBuilderRef builder;
	LLVMValueRef *values, *addresses;
} EmitContext;

typedef struct {
	MonoBasicBlock *bb;
	MonoInst *phi;
	int index;
} PhiNode;

/*
 * Instruction metadata
 * This is the same as ins_info, but LREG != IREG.
 */
#ifdef MINI_OP
#undef MINI_OP
#endif
#ifdef MINI_OP3
#undef MINI_OP3
#endif
#define MINI_OP(a,b,dest,src1,src2) dest, src1, src2, ' ',
#define MINI_OP3(a,b,dest,src1,src2,src3) dest, src1, src2, src3,
#define NONE ' '
#define IREG 'i'
#define FREG 'f'
#define VREG 'v'
#define XREG 'x'
#define LREG 'l'
/* keep in sync with the enum in mini.h */
const char
llvm_ins_info[] = {
#include "mini-ops.h"
};
#undef MINI_OP
#undef MINI_OP3

#define LLVM_INS_INFO(opcode) (&llvm_ins_info [((opcode) - OP_START - 1) * 4])

#define LLVM_FAILURE(ctx, reason) do { \
	(ctx)->cfg->exception_message = g_strdup (reason); \
	(ctx)->cfg->disable_llvm = TRUE; \
	goto FAILURE; \
} while (0)

#define CHECK_FAILURE(ctx) do { \
    if ((ctx)->cfg->disable_llvm) \
		goto FAILURE; \
} while (0)

static LLVMIntPredicate cond_to_llvm_cond [] = {
	LLVMIntEQ,
	LLVMIntNE,
	LLVMIntSLE,
	LLVMIntSGE,
	LLVMIntSLT,
	LLVMIntSGT,
	LLVMIntULE,
	LLVMIntUGE,
	LLVMIntULT,
	LLVMIntUGT,
};

static LLVMRealPredicate fpcond_to_llvm_cond [] = {
	LLVMRealOEQ,
	LLVMRealUNE,
	LLVMRealOLE,
	LLVMRealOGE,
	LLVMRealOLT,
	LLVMRealOGT,
	LLVMRealULE,
	LLVMRealUGE,
	LLVMRealULT,
	LLVMRealUGT,
};

static LLVMModuleRef module;
static LLVMExecutionEngineRef ee;
static GHashTable *llvm_types;
static guint32 current_cfg_tls_id;

static void mono_llvm_init (void);

static LLVMTypeRef
IntPtrType (void)
{
	return sizeof (gpointer) == 8 ? LLVMInt64Type () : LLVMInt32Type ();
}

static LLVMTypeRef
type_to_llvm_type (EmitContext *ctx, MonoType *t)
{
	if (t->byref)
		return IntPtrType ();
	switch (t->type) {
	case MONO_TYPE_VOID:
		return LLVMVoidType ();
	case MONO_TYPE_I1:
		return LLVMInt8Type ();
	case MONO_TYPE_I2:
		return LLVMInt16Type ();
	case MONO_TYPE_I4:
		return LLVMInt32Type ();
	case MONO_TYPE_U1:
		return LLVMInt8Type ();
	case MONO_TYPE_U2:
		return LLVMInt16Type ();
	case MONO_TYPE_U4:
		return LLVMInt32Type ();
	case MONO_TYPE_BOOLEAN:
		return LLVMInt8Type ();
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return LLVMInt64Type ();
	case MONO_TYPE_CHAR:
		return LLVMInt16Type ();
	case MONO_TYPE_R4:
		return LLVMFloatType ();
	case MONO_TYPE_R8:
		return LLVMDoubleType ();
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		return IntPtrType ();
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
		return LLVMPointerType (IntPtrType (), 0);
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* Because of generic sharing */
		return IntPtrType ();
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			return IntPtrType ();
		/* Fall through */
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass;
		LLVMTypeRef ltype;

		klass = mono_class_from_mono_type (t);

		if (klass->enumtype)
			return LLVMInt32Type ();
		ltype = g_hash_table_lookup (llvm_types, klass);
		if (!ltype) {
			ltype = LLVMArrayType (LLVMInt8Type (), mono_class_value_size (klass, NULL));
			g_hash_table_insert (llvm_types, klass, ltype);
		}
		return ltype;
	}

	default:
		ctx->cfg->exception_message = g_strdup_printf ("type %s", mono_type_full_name (t));
		ctx->cfg->disable_llvm = TRUE;
		return NULL;
	}
}

static LLVMTypeRef
type_to_llvm_arg_type (EmitContext *ctx, MonoType *t)
{
	LLVMTypeRef ptype = type_to_llvm_type (ctx, t);
	
	if (ptype == LLVMInt8Type () || ptype == LLVMInt16Type ()) {
		/* 
		 * LLVM generates code which only sets the lower bits, while JITted
		 * code expects all the bits to be set.
		 */
		ptype = LLVMInt32Type ();
	}

	return ptype;
}

static G_GNUC_UNUSED LLVMTypeRef
llvm_type_to_stack_type (LLVMTypeRef type)
{
	if (type == NULL)
		return NULL;
	if (type == LLVMInt8Type ())
		return LLVMInt32Type ();
	else if (type == LLVMInt16Type ())
		return LLVMInt32Type ();
	else if (type == LLVMFloatType ())
		return LLVMDoubleType ();
	else
		return type;
}

static LLVMTypeRef
regtype_to_llvm_type (char c)
{
	switch (c) {
	case 'i':
		return LLVMInt32Type ();
	case 'l':
		return LLVMInt64Type ();
	case 'f':
		return LLVMDoubleType ();
	default:
		return NULL;
	}
}

static LLVMTypeRef
conv_to_llvm_type (int opcode)
{
	switch (opcode) {
	case OP_ICONV_TO_I1:
	case OP_LCONV_TO_I1:
		return LLVMInt8Type ();
	case OP_ICONV_TO_U1:
	case OP_LCONV_TO_U1:
		return LLVMInt8Type ();
	case OP_ICONV_TO_I2:
	case OP_LCONV_TO_I2:
		return LLVMInt16Type ();
	case OP_ICONV_TO_U2:
	case OP_LCONV_TO_U2:
		return LLVMInt16Type ();
	case OP_ICONV_TO_I4:
	case OP_LCONV_TO_I4:
		return LLVMInt32Type ();
	case OP_ICONV_TO_U4:
	case OP_LCONV_TO_U4:
		return LLVMInt32Type ();
	case OP_ICONV_TO_I8:
		return LLVMInt64Type ();
	case OP_ICONV_TO_R4:
		return LLVMFloatType ();
	case OP_ICONV_TO_R8:
		return LLVMDoubleType ();
	case OP_ICONV_TO_U8:
		return LLVMInt64Type ();
	case OP_FCONV_TO_I4:
		return LLVMInt32Type ();
	case OP_FCONV_TO_I8:
		return LLVMInt64Type ();
	case OP_FCONV_TO_I1:
	case OP_FCONV_TO_U1:
		return LLVMInt8Type ();
	case OP_FCONV_TO_I2:
	case OP_FCONV_TO_U2:
		return LLVMInt16Type ();
	case OP_FCONV_TO_I:
	case OP_FCONV_TO_U:
		return sizeof (gpointer) == 8 ? LLVMInt64Type () : LLVMInt32Type ();
	default:
		printf ("%s\n", mono_inst_name (opcode));
		g_assert_not_reached ();
		return NULL;
	}
}		

static LLVMTypeRef
load_store_to_llvm_type (int opcode, int *size, gboolean *sext, gboolean *zext)
{
	*sext = FALSE;
	*zext = FALSE;

	switch (opcode) {
	case OP_LOADI1_MEMBASE:
	case OP_STOREI1_MEMBASE_REG:
	case OP_STOREI1_MEMBASE_IMM:
		*size = 1;
		*sext = TRUE;
		return LLVMInt8Type ();
	case OP_LOADU1_MEMBASE:
		*size = 1;
		*zext = TRUE;
		return LLVMInt8Type ();
	case OP_LOADI2_MEMBASE:
	case OP_STOREI2_MEMBASE_REG:
	case OP_STOREI2_MEMBASE_IMM:
		*size = 2;
		*sext = TRUE;
		return LLVMInt16Type ();
	case OP_LOADU2_MEMBASE:
		*size = 2;
		*zext = TRUE;
		return LLVMInt16Type ();
	case OP_LOADI4_MEMBASE:
	case OP_LOADU4_MEMBASE:
	case OP_STOREI4_MEMBASE_REG:
	case OP_STOREI4_MEMBASE_IMM:
		*size = 4;
		return LLVMInt32Type ();
	case OP_LOADI8_MEMBASE:
	case OP_LOADI8_MEM:
	case OP_STOREI8_MEMBASE_REG:
	case OP_STOREI8_MEMBASE_IMM:
		*size = 8;
		return LLVMInt64Type ();
	case OP_LOADR4_MEMBASE:
	case OP_STORER4_MEMBASE_REG:
		*size = 4;
		return LLVMFloatType ();
	case OP_LOADR8_MEMBASE:
	case OP_STORER8_MEMBASE_REG:
		*size = 8;
		return LLVMDoubleType ();
	case OP_LOAD_MEMBASE:
	case OP_LOAD_MEM:
	case OP_STORE_MEMBASE_REG:
	case OP_STORE_MEMBASE_IMM:
		*size = sizeof (gpointer);
		return IntPtrType ();
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

static const char*
ovf_op_to_intrins (int opcode)
{
	switch (opcode) {
	case OP_IADD_OVF:
		return "llvm.sadd.with.overflow.i32";
	case OP_IADD_OVF_UN:
		return "llvm.uadd.with.overflow.i32";
	case OP_ISUB_OVF:
		return "llvm.ssub.with.overflow.i32";
	case OP_ISUB_OVF_UN:
		return "llvm.usub.with.overflow.i32";
	case OP_IMUL_OVF:
		return "llvm.smul.with.overflow.i32";
	case OP_IMUL_OVF_UN:
		return "llvm.umul.with.overflow.i32";
	case OP_LADD_OVF:
		return "llvm.sadd.with.overflow.i64";
	case OP_LADD_OVF_UN:
		return "llvm.uadd.with.overflow.i64";
	case OP_LSUB_OVF:
		return "llvm.ssub.with.overflow.i64";
	case OP_LSUB_OVF_UN:
		return "llvm.usub.with.overflow.i64";
	case OP_LMUL_OVF:
		return "llvm.smul.with.overflow.i64";
	case OP_LMUL_OVF_UN:
		return "llvm.umul.with.overflow.i64";
	default:
		g_assert_not_reached ();
		return NULL;
	}
}

static LLVMBasicBlockRef
get_bb (EmitContext *ctx, MonoBasicBlock *bb)
{
	char bb_name [128];

	if (ctx->bblocks [bb->block_num] == NULL) {
		sprintf (bb_name, "BB%d", bb->block_num);

		ctx->bblocks [bb->block_num] = LLVMAppendBasicBlock (ctx->lmethod, bb_name);
		ctx->end_bblocks [bb->block_num] = ctx->bblocks [bb->block_num];
	}

	return ctx->bblocks [bb->block_num];
}

/* Return the last LLVM bblock corresponding to BB */
static LLVMBasicBlockRef
get_end_bb (EmitContext *ctx, MonoBasicBlock *bb)
{
	get_bb (ctx, bb);
	return ctx->end_bblocks [bb->block_num];
}

static const char*
get_tempname (EmitContext *ctx)
{
	// FIXME:
	static char temp_name [128];

	sprintf (temp_name, "s%d", ctx->sindex ++);
	
	return temp_name;
}

static gpointer
resolve_patch (MonoCompile *cfg, MonoJumpInfoType type, gconstpointer target)
{
	MonoJumpInfo ji;

	memset (&ji, 0, sizeof (ji));
	ji.type = type;
	ji.data.target = target;

	return mono_resolve_patch_target (cfg->method, cfg->domain, NULL, &ji, FALSE);
}

static LLVMValueRef
convert (EmitContext *ctx, LLVMValueRef v, LLVMTypeRef dtype)
{
	LLVMTypeRef stype = LLVMTypeOf (v);

	if (stype != dtype) {
		/* Extend */
		if (dtype == LLVMInt64Type () && (stype == LLVMInt32Type () || stype == LLVMInt16Type () || stype == LLVMInt8Type ()))
			return LLVMBuildSExt (ctx->builder, v, dtype, get_tempname (ctx));
		else if (dtype == LLVMInt32Type () && (stype == LLVMInt16Type () || stype == LLVMInt8Type ()))
			return LLVMBuildSExt (ctx->builder, v, dtype, get_tempname (ctx));
		else if (dtype == LLVMInt16Type () && (stype == LLVMInt8Type ()))
			return LLVMBuildSExt (ctx->builder, v, dtype, get_tempname (ctx));
		else if (dtype == LLVMDoubleType () && stype == LLVMFloatType ())
			return LLVMBuildFPExt (ctx->builder, v, dtype, get_tempname (ctx));

		/* Trunc */
		if (stype == LLVMInt64Type () && (dtype == LLVMInt32Type () || dtype == LLVMInt16Type () || dtype == LLVMInt8Type ()))
			return LLVMBuildTrunc (ctx->builder, v, dtype, get_tempname (ctx));
		if (stype == LLVMInt32Type () && (dtype == LLVMInt16Type () || dtype == LLVMInt8Type ()))
			return LLVMBuildTrunc (ctx->builder, v, dtype, get_tempname (ctx));
		if (stype == LLVMDoubleType () && dtype == LLVMFloatType ())
			return LLVMBuildFPTrunc (ctx->builder, v, dtype, get_tempname (ctx));

		if (LLVMGetTypeKind (stype) == LLVMPointerTypeKind && LLVMGetTypeKind (dtype) == LLVMPointerTypeKind)
			return LLVMBuildBitCast (ctx->builder, v, dtype, get_tempname (ctx));
		if (LLVMGetTypeKind (dtype) == LLVMPointerTypeKind)
			return LLVMBuildIntToPtr (ctx->builder, v, dtype, get_tempname (ctx));
		if (LLVMGetTypeKind (stype) == LLVMPointerTypeKind)
			return LLVMBuildPtrToInt (ctx->builder, v, dtype, get_tempname (ctx));

		LLVMDumpValue (v);
		LLVMDumpValue (LLVMConstNull (dtype));
		g_assert_not_reached ();
		return NULL;
	} else {
		return v;
	}
}

/* Emit stores for volatile variables */
static void
emit_volatile_store (EmitContext *ctx, int vreg)
{
	MonoInst *var = get_vreg_to_inst (ctx->cfg, vreg);

	if (var && var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) {
		LLVMBuildStore (ctx->builder, convert (ctx, ctx->values [vreg], type_to_llvm_type (ctx, var->inst_vtype)), ctx->addresses [vreg]);
	}
}

static LLVMTypeRef
sig_to_llvm_sig (EmitContext *ctx, MonoMethodSignature *sig, gboolean vretaddr)
{
	LLVMTypeRef ret_type;
	LLVMTypeRef *param_types = NULL;
	LLVMTypeRef res;
	int i, pindex;

	ret_type = type_to_llvm_type (ctx, sig->ret);
	CHECK_FAILURE (ctx);

	if (MONO_TYPE_ISSTRUCT (sig->ret) && !vretaddr)
		LLVM_FAILURE (ctx, "vtype ret");

	param_types = g_new0 (LLVMTypeRef, sig->param_count + sig->hasthis + vretaddr);
	pindex = 0;
	if (vretaddr) {
		ret_type = LLVMVoidType ();
		param_types [pindex ++] = IntPtrType ();
	}
	if (sig->hasthis)
		param_types [pindex ++] = IntPtrType ();
	for (i = 0; i < sig->param_count; ++i) {
		if (MONO_TYPE_ISSTRUCT (sig->params [i]))
			LLVM_FAILURE (ctx, "vtype param");
		param_types [pindex ++] = type_to_llvm_arg_type (ctx, sig->params [i]);
	}
	CHECK_FAILURE (ctx);

	res = LLVMFunctionType (ret_type, param_types, sig->param_count + sig->hasthis + vretaddr, FALSE);
	g_free (param_types);

	return res;

 FAILURE:
	g_free (param_types);

	return NULL;
}

static G_GNUC_UNUSED LLVMTypeRef 
LLVMFunctionType1(LLVMTypeRef ReturnType,
				  LLVMTypeRef ParamType1,
				  int IsVarArg)
{
	LLVMTypeRef param_types [1];

	param_types [0] = ParamType1;

	return LLVMFunctionType (ReturnType, param_types, 1, IsVarArg);
}

static LLVMTypeRef 
LLVMFunctionType2(LLVMTypeRef ReturnType,
				  LLVMTypeRef ParamType1,
				  LLVMTypeRef ParamType2,
				  int IsVarArg)
{
	LLVMTypeRef param_types [2];

	param_types [0] = ParamType1;
	param_types [1] = ParamType2;

	return LLVMFunctionType (ReturnType, param_types, 2, IsVarArg);
}

static LLVMTypeRef 
LLVMFunctionType3(LLVMTypeRef ReturnType,
				  LLVMTypeRef ParamType1,
				  LLVMTypeRef ParamType2,
				  LLVMTypeRef ParamType3,
				  int IsVarArg)
{
	LLVMTypeRef param_types [3];

	param_types [0] = ParamType1;
	param_types [1] = ParamType2;
	param_types [2] = ParamType3;

	return LLVMFunctionType (ReturnType, param_types, 3, IsVarArg);
}

static void
emit_cond_throw_pos (EmitContext *ctx)
{
}

static void
emit_cond_system_exception (EmitContext *ctx, MonoBasicBlock *bb, const char *exc_type, LLVMValueRef cmp)
{
	char bb_name [128];
	LLVMBasicBlockRef ex_bb, noex_bb;
	LLVMBuilderRef builder;
	MonoClass *exc_class;
	static LLVMValueRef throw;

	sprintf (bb_name, "EX_BB%d", ctx->ex_index);
	ex_bb = LLVMAppendBasicBlock (ctx->lmethod, bb_name);

	sprintf (bb_name, "NOEX_BB%d", ctx->ex_index);
	noex_bb = LLVMAppendBasicBlock (ctx->lmethod, bb_name);

	LLVMBuildCondBr (ctx->builder, cmp, ex_bb, noex_bb);

	ctx->builder = LLVMCreateBuilder ();
	LLVMPositionBuilderAtEnd (ctx->builder, noex_bb);

	ctx->end_bblocks [bb->block_num] = noex_bb;

	exc_class = mono_class_from_name (mono_defaults.corlib, "System", exc_type);
	g_assert (exc_class);

	/* Emit exception throwing code */
	builder = LLVMCreateBuilder ();
	LLVMPositionBuilderAtEnd (builder, ex_bb);

	// FIXME:
	if (!throw) {
		throw = LLVMAddFunction (module, "throw", LLVMFunctionType (LLVMVoidType (), NULL, 0, FALSE));	

		LLVMAddGlobalMapping (ee, throw, &abort);
	}

	LLVMBuildCall (builder, throw, NULL, 0, "");

	LLVMBuildUnreachable (builder);

	ctx->ex_index ++;
}

void
mono_llvm_emit_method (MonoCompile *cfg)
{
	EmitContext *ctx;
	MonoMethodSignature *sig;
	MonoBasicBlock *bb;
	LLVMTypeRef method_type;
	LLVMValueRef method = NULL;
	char *method_name;
	LLVMValueRef *values, *addresses;
	LLVMTypeRef *vreg_types;
	int i, max_block_num;
	GHashTable *phi_nodes;
	gboolean last = FALSE;
	GPtrArray *phi_values;

	/* The code below might acquire the loader lock, so use it for global locking */
	mono_loader_lock ();

	if (!ee)
		mono_llvm_init ();

	/* Used to communicate with the callbacks */
	TlsSetValue (current_cfg_tls_id, cfg);

	ctx = g_new0 (EmitContext, 1);
	ctx->cfg = cfg;
	ctx->mempool = cfg->mempool;

	values = g_new0 (LLVMValueRef, cfg->next_vreg);
	addresses = g_new0 (LLVMValueRef, cfg->next_vreg);
	vreg_types = g_new0 (LLVMTypeRef, cfg->next_vreg);
	phi_nodes = g_hash_table_new (NULL, NULL);
	phi_values = g_ptr_array_new ();

	ctx->values = values;
	ctx->addresses = addresses;

#if 1
	{
		static int count = 0;
		count ++;

		if (getenv ("LLVM_COUNT")) {
			if (count == atoi (getenv ("LLVM_COUNT"))) {
				printf ("LAST: %s\n", mono_method_full_name (cfg->method, TRUE));
				last = TRUE;
			}
			if (count > atoi (getenv ("LLVM_COUNT")))
				LLVM_FAILURE (ctx, "");
		}
	}
#endif

	sig = mono_method_signature (cfg->method);

	method_type = sig_to_llvm_sig (ctx, sig, cfg->vret_addr != NULL);
	CHECK_FAILURE (ctx);

	method_name = mono_method_full_name (cfg->method, TRUE);
	method = LLVMAddFunction (module, method_name, method_type);
	ctx->lmethod = method;
	g_free (method_name);

	if (cfg->method->save_lmf)
		LLVM_FAILURE (ctx, "lmf");

	if (cfg->vret_addr) {
		values [cfg->vret_addr->dreg] = LLVMGetParam (method, 0);
		for (i = 0; i < sig->param_count + sig->hasthis; ++i)
			values [cfg->args [i]->dreg] = LLVMGetParam (method, i + 1);
	} else {
		for (i = 0; i < sig->param_count + sig->hasthis; ++i)
			values [cfg->args [i]->dreg] = LLVMGetParam (method, i);
	}

	max_block_num = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb)
		max_block_num = MAX (max_block_num, bb->block_num);
	ctx->bblocks = g_new0 (LLVMBasicBlockRef, max_block_num + 1);
	ctx->end_bblocks = g_new0 (LLVMBasicBlockRef, max_block_num + 1);

	/* Add branches between non-consecutive bblocks */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		if (bb->last_ins && MONO_IS_COND_BRANCH_OP (bb->last_ins) &&
			bb->next_bb != bb->last_ins->inst_false_bb) {
			
			MonoInst *inst = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst));
			inst->opcode = OP_BR;
			inst->inst_target_bb = bb->last_ins->inst_false_bb;
			mono_bblock_add_inst (bb, inst);
		}
	}

	/* 
	 * Make a first pass over the code in dfn order to obtain the exact type of all 
	 * vregs, and to create PHI nodes.
	 * This is needed because we only use iregs/fregs, while llvm uses many types,
	 * and requires precise types. Also, our IR is not precise in many places, like
	 * using i32 arguments to opcodes expecting an i64 etc. 
	 */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		LLVMBuilderRef builder;
		char *dname;
		char dname_buf[128];

		builder = LLVMCreateBuilder ();

		for (ins = bb->code; ins; ins = ins->next) {
			switch (ins->opcode) {
			case OP_PHI:
			case OP_FPHI: {
				LLVMTypeRef phi_type = llvm_type_to_stack_type (type_to_llvm_type (ctx, &ins->klass->byval_arg));

				CHECK_FAILURE (ctx);

				/* 
				 * Have to precreate these, as they can be referenced by
				 * earlier instructions.
				 */
				sprintf (dname_buf, "t%d", ins->dreg);
				dname = dname_buf;
				values [ins->dreg] = LLVMBuildPhi (builder, phi_type, dname);

				g_ptr_array_add (phi_values, values [ins->dreg]);

				/* 
				 * Set the expected type of the incoming arguments since these have
				 * to have the same type.
				 */
				for (i = 0; i < ins->inst_phi_args [0]; i++) {
					int sreg1 = ins->inst_phi_args [i + 1];
					
					if (sreg1 != -1)
						vreg_types [sreg1] = phi_type;
				}
				break;
				}
			default:
				break;
			}
		}
	}

	/*
	 * Second pass: generate code.
	 */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins;
		LLVMBasicBlockRef cbb;
		LLVMBuilderRef builder;
		gboolean has_terminator;
		LLVMValueRef v;
		LLVMValueRef lhs, rhs;

		if (!(bb == cfg->bb_entry || bb->in_count > 0))
			continue;

		cbb = get_bb (ctx, bb);
		builder = LLVMCreateBuilder ();
		ctx->builder = builder;
		LLVMPositionBuilderAtEnd (builder, cbb);

		if (bb == cfg->bb_entry) {
			/*
			 * Handle indirect/volatile variables by allocating memory for them
			 * using 'alloca', and storing their address in a temporary.
			 */
			for (i = 0; i < cfg->num_varinfo; ++i) {
				MonoInst *var = cfg->varinfo [i];
				LLVMTypeRef vtype;

				if (var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || MONO_TYPE_ISSTRUCT (var->inst_vtype)) {
					vtype = type_to_llvm_type (ctx, var->inst_vtype);
					CHECK_FAILURE (ctx);
					addresses [var->dreg] = LLVMBuildAlloca (builder, vtype, get_tempname (ctx));
				}
			}

			/* Convert arguments */
			for (i = 0; i < sig->param_count; ++i)
				values [cfg->args [i + sig->hasthis]->dreg] = convert (ctx, values [cfg->args [i + sig->hasthis]->dreg], llvm_type_to_stack_type (type_to_llvm_type (ctx, sig->params [i])));

			for (i = 0; i < sig->param_count; ++i)
				emit_volatile_store (ctx, cfg->args [i + sig->hasthis]->dreg);
		}

		has_terminator = FALSE;
		for (ins = bb->code; ins; ins = ins->next) {
			const char *spec = LLVM_INS_INFO (ins->opcode);
			char *dname;
			char dname_buf [128];

			if (has_terminator)
				/* There could be instructions after a terminator, skip them */
				break;

			if (spec [MONO_INST_DEST] != ' ' && !MONO_IS_STORE_MEMBASE (ins)) {
				sprintf (dname_buf, "t%d", ins->dreg);
				dname = dname_buf;
			}

			if (spec [MONO_INST_SRC1] != ' ' && spec [MONO_INST_SRC1] != 'v') {
				MonoInst *var = get_vreg_to_inst (cfg, ins->sreg1);

				if (var && var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) {
					lhs = LLVMBuildLoad (builder, addresses [ins->sreg1], get_tempname (ctx));
				} else {
					/* It is ok for SETRET to have an uninitialized argument */
					if (!values [ins->sreg1] && ins->opcode != OP_SETRET)
						LLVM_FAILURE (ctx, "sreg1");
					lhs = values [ins->sreg1];
				}
			} else {
				lhs = NULL;
			}

			if (spec [MONO_INST_SRC2] != ' ' && spec [MONO_INST_SRC2] != ' ') {
				MonoInst *var = get_vreg_to_inst (cfg, ins->sreg2);
				if (var && var->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT)) {
					rhs = LLVMBuildLoad (builder, addresses [ins->sreg2], get_tempname (ctx));
				} else {
					if (!values [ins->sreg2])
						LLVM_FAILURE (ctx, "sreg2");
					rhs = values [ins->sreg2];
				}
			} else {
				rhs = NULL;
			}

			//mono_print_ins (ins);
			switch (ins->opcode) {
			case OP_NOP:
			case OP_NOT_NULL:
			case OP_LIVERANGE_START:
			case OP_LIVERANGE_END:
				break;
			case OP_ICONST:
				values [ins->dreg] = LLVMConstInt (LLVMInt32Type (), ins->inst_c0, FALSE);
				break;
			case OP_I8CONST:
				values [ins->dreg] = LLVMConstInt (LLVMInt64Type (), (gint64)ins->inst_c0, FALSE);
				break;
			case OP_R8CONST:
				values [ins->dreg] = LLVMConstReal (LLVMDoubleType (), *(double*)ins->inst_p0);
				break;
			case OP_R4CONST:
				values [ins->dreg] = LLVMConstFPExt (LLVMConstReal (LLVMFloatType (), *(float*)ins->inst_p0), LLVMDoubleType ());
				break;
			case OP_BR:
				LLVMBuildBr (builder, get_bb (ctx, ins->inst_target_bb));
				has_terminator = TRUE;
				break;
			case OP_SWITCH: {
				int i;
				LLVMValueRef v;
				char *bb_name;
				LLVMBasicBlockRef new_bb;
				LLVMBuilderRef new_builder;

				// The default branch is already handled
				// FIXME: Handle it here

				/* Start new bblock */
				bb_name = g_strdup_printf ("SWITCH_DEFAULT_BB%d", ctx->default_index ++);
				new_bb = LLVMAppendBasicBlock (ctx->lmethod, bb_name);

				v = LLVMBuildSwitch (builder, lhs, new_bb, GPOINTER_TO_UINT (ins->klass));
				for (i = 0; i < GPOINTER_TO_UINT (ins->klass); ++i) {
					MonoBasicBlock *target_bb = ins->inst_many_bb [i];

					LLVMAddCase (v, LLVMConstInt (LLVMInt32Type (), i, FALSE), get_bb (ctx, target_bb));
				}

				new_builder = LLVMCreateBuilder ();
				LLVMPositionBuilderAtEnd (new_builder, new_bb);
				LLVMBuildUnreachable (new_builder);

				has_terminator = TRUE;
				g_assert (!ins->next);
				
				break;
			}

			case OP_SETRET:
				if (!lhs) {
					/* 
					 * The method did not set its return value, probably because it
					 * ends with a throw.
					 */
					if (cfg->vret_addr)
						LLVMBuildRetVoid (builder);
					else
						LLVMBuildRet (builder, LLVMConstNull (type_to_llvm_type (ctx, sig->ret)));
				} else {
					LLVMBuildRet (builder, convert (ctx, lhs, type_to_llvm_type (ctx, sig->ret)));
				}
				has_terminator = TRUE;
				break;
			case OP_ICOMPARE:
			case OP_FCOMPARE:
			case OP_LCOMPARE:
			case OP_COMPARE:
			case OP_ICOMPARE_IMM:
			case OP_LCOMPARE_IMM:
			case OP_COMPARE_IMM:
#ifdef __x86_64__
			case OP_AMD64_ICOMPARE_MEMBASE_REG:
			case OP_AMD64_ICOMPARE_MEMBASE_IMM:
#endif
			{
				CompRelation rel;
				LLVMValueRef cmp;

				if (ins->next->opcode == OP_NOP)
					break;

				rel = mono_opcode_to_cond (ins->next->opcode);

				/* Used for implementing bound checks */
#ifdef __x86_64__
				if ((ins->opcode == OP_AMD64_ICOMPARE_MEMBASE_REG) || (ins->opcode == OP_AMD64_ICOMPARE_MEMBASE_IMM)) {
					int size = 4;
					LLVMValueRef index;
					LLVMTypeRef t;

					t = LLVMInt32Type ();

					g_assert (ins->inst_offset % size == 0);
					index = LLVMConstInt (LLVMInt32Type (), ins->inst_offset / size, FALSE);				

					lhs = LLVMBuildLoad (builder, LLVMBuildGEP (builder, convert (ctx, values [ins->inst_basereg], LLVMPointerType (t, 0)), &index, 1, get_tempname (ctx)), get_tempname (ctx));
				}
				if (ins->opcode == OP_AMD64_ICOMPARE_MEMBASE_IMM) {
					lhs = convert (ctx, lhs, LLVMInt32Type ());
					rhs = LLVMConstInt (LLVMInt32Type (), ins->inst_imm, FALSE);
				}
				if (ins->opcode == OP_AMD64_ICOMPARE_MEMBASE_REG)
					rhs = convert (ctx, rhs, LLVMInt32Type ());
#endif

				if (ins->opcode == OP_ICOMPARE_IMM) {
					lhs = convert (ctx, lhs, LLVMInt32Type ());
					rhs = LLVMConstInt (LLVMInt32Type (), ins->inst_imm, FALSE);
				}
				if (ins->opcode == OP_LCOMPARE_IMM)
					rhs = LLVMConstInt (LLVMInt64Type (), ins->inst_imm, FALSE);
				if (ins->opcode == OP_LCOMPARE)
					rhs = convert (ctx, rhs, LLVMInt64Type ());
				if (ins->opcode == OP_ICOMPARE) {
					lhs = convert (ctx, lhs, LLVMInt32Type ());
					rhs = convert (ctx, rhs, LLVMInt32Type ());
				}

				if (lhs && rhs) {
					if (LLVMGetTypeKind (LLVMTypeOf (lhs)) == LLVMPointerTypeKind)
						rhs = convert (ctx, rhs, LLVMTypeOf (lhs));
					else if (LLVMGetTypeKind (LLVMTypeOf (rhs)) == LLVMPointerTypeKind)
						lhs = convert (ctx, lhs, LLVMTypeOf (rhs));
				}

				/* We use COMPARE+SETcc/Bcc, llvm uses SETcc+br cond */
				if (ins->opcode == OP_FCOMPARE)
					cmp = LLVMBuildFCmp (builder, fpcond_to_llvm_cond [rel], convert (ctx, lhs, LLVMDoubleType ()), convert (ctx, rhs, LLVMDoubleType ()), get_tempname (ctx));
				else if (ins->opcode == OP_COMPARE_IMM)
					cmp = LLVMBuildICmp (builder, cond_to_llvm_cond [rel], convert (ctx, lhs, IntPtrType ()), LLVMConstInt (IntPtrType (), ins->inst_imm, FALSE), get_tempname (ctx));
				else if (ins->opcode == OP_COMPARE)
					cmp = LLVMBuildICmp (builder, cond_to_llvm_cond [rel], convert (ctx, lhs, IntPtrType ()), convert (ctx, rhs, IntPtrType ()), get_tempname (ctx));
				else
					cmp = LLVMBuildICmp (builder, cond_to_llvm_cond [rel], lhs, rhs, get_tempname (ctx));

				if (MONO_IS_COND_BRANCH_OP (ins->next)) {
					LLVMBuildCondBr (builder, cmp, get_bb (ctx, ins->next->inst_true_bb), get_bb (ctx, ins->next->inst_false_bb));
					has_terminator = TRUE;
				} else if (MONO_IS_SETCC (ins->next)) {
					dname = g_strdup_printf ("t%d", ins->next->dreg);
					values [ins->next->dreg] = LLVMBuildZExt (builder, cmp, LLVMInt32Type (), dname);
				} else if (MONO_IS_COND_EXC (ins->next)) {
					//emit_cond_throw_pos (ctx);
					emit_cond_system_exception (ctx, bb, ins->next->inst_p1, cmp);
					builder = ctx->builder;
				} else {
					LLVM_FAILURE (ctx, "next");
				}

				ins = ins->next;
				break;
			}
			case OP_FCEQ:
			case OP_FCLT:
			case OP_FCLT_UN:
			case OP_FCGT:
			case OP_FCGT_UN: {
				CompRelation rel;
				LLVMValueRef cmp;

				rel = mono_opcode_to_cond (ins->opcode);

				cmp = LLVMBuildFCmp (builder, fpcond_to_llvm_cond [rel], lhs, rhs, get_tempname (ctx));
				values [ins->dreg] = LLVMBuildZExt (builder, cmp, LLVMInt32Type (), dname);
				break;
			}
			case OP_PHI:
			case OP_FPHI: {
				int i;

				/* Created earlier, insert it now */
				LLVMInsertIntoBuilder (builder, values [ins->dreg]);

				/* Check that all input bblocks really branch to us */
				for (i = 0; i < bb->in_count; ++i) {
					if (bb->in_bb [i]->last_ins && bb->in_bb [i]->last_ins->opcode == OP_NOT_REACHED)
						ins->inst_phi_args [i + 1] = -1;
				}

				// FIXME: If a SWITCH statement branches to the same bblock in more
				// than once case, the PHI should reference the bblock multiple times
				for (i = 0; i < bb->in_count; ++i)
					if (bb->in_bb [i]->last_ins && bb->in_bb [i]->last_ins->opcode == OP_SWITCH) {
						LLVM_FAILURE (ctx, "switch + phi");
						break;
					}

				for (i = 0; i < ins->inst_phi_args [0]; i++) {
					int sreg1 = ins->inst_phi_args [i + 1];
					LLVMBasicBlockRef in_bb;

					if (sreg1 == -1)
						continue;

					/* Add incoming values which are already defined */
					if (FALSE && values [sreg1]) {
						in_bb = get_end_bb (ctx, bb->in_bb [i]);

						g_assert (LLVMTypeOf (values [sreg1]) == LLVMTypeOf (values [ins->dreg]));
						LLVMAddIncoming (values [ins->dreg], &values [sreg1], &in_bb, 1);
					} else {
						/* Remember for later */
						//LLVM_FAILURE (ctx, "phi incoming value");
						GSList *node_list = g_hash_table_lookup (phi_nodes, GUINT_TO_POINTER (bb->in_bb [i]));
						PhiNode *node = mono_mempool_alloc0 (ctx->mempool, sizeof (PhiNode));
						node->bb = bb;
						node->phi = ins;
						node->index = i;
						node_list = g_slist_prepend_mempool (ctx->mempool, node_list, node);
						g_hash_table_insert (phi_nodes, GUINT_TO_POINTER (bb->in_bb [i]), node_list);
					}
				}
				break;
			}
			case OP_MOVE:
			case OP_FMOVE:
				g_assert (lhs);
				values [ins->dreg] = lhs;
				break;
			case OP_IADD:
			case OP_ISUB:
			case OP_IAND:
			case OP_IMUL:
			case OP_IDIV:
			case OP_IDIV_UN:
			case OP_IREM:
			case OP_IREM_UN:
			case OP_IOR:
			case OP_IXOR:
			case OP_ISHL:
			case OP_ISHR:
			case OP_ISHR_UN:
			case OP_FADD:
			case OP_FSUB:
			case OP_FMUL:
			case OP_FDIV:
			case OP_LADD:
			case OP_LSUB:
			case OP_LMUL:
			case OP_LDIV:
			case OP_LDIV_UN:
			case OP_LREM:
			case OP_LREM_UN:
			case OP_LAND:
			case OP_LOR:
			case OP_LXOR:
			case OP_LSHL:
			case OP_LSHR:
			case OP_LSHR_UN:
				lhs = convert (ctx, lhs, regtype_to_llvm_type (spec [MONO_INST_DEST]));
				rhs = convert (ctx, rhs, regtype_to_llvm_type (spec [MONO_INST_DEST]));

				switch (ins->opcode) {
				case OP_IADD:
				case OP_FADD:
				case OP_LADD:
					values [ins->dreg] = LLVMBuildAdd (builder, lhs, rhs, dname);
					break;
				case OP_ISUB:
				case OP_FSUB:
				case OP_LSUB:
					values [ins->dreg] = LLVMBuildSub (builder, lhs, rhs, dname);
					break;
				case OP_IMUL:
				case OP_FMUL:
				case OP_LMUL:
					values [ins->dreg] = LLVMBuildMul (builder, lhs, rhs, dname);
					break;
				case OP_IREM:
				case OP_LREM:
					values [ins->dreg] = LLVMBuildSRem (builder, lhs, rhs, dname);
					break;
				case OP_IREM_UN:
				case OP_LREM_UN:
					values [ins->dreg] = LLVMBuildURem (builder, lhs, rhs, dname);
					break;
				case OP_IDIV:
				case OP_LDIV:
					values [ins->dreg] = LLVMBuildSDiv (builder, lhs, rhs, dname);
					break;
				case OP_IDIV_UN:
				case OP_LDIV_UN:
					values [ins->dreg] = LLVMBuildUDiv (builder, lhs, rhs, dname);
					break;
				case OP_FDIV:
					values [ins->dreg] = LLVMBuildFDiv (builder, lhs, rhs, dname);
					break;
				case OP_IAND:
				case OP_LAND:
					values [ins->dreg] = LLVMBuildAnd (builder, lhs, rhs, dname);
					break;
				case OP_IOR:
				case OP_LOR:
					values [ins->dreg] = LLVMBuildOr (builder, lhs, rhs, dname);
					break;
				case OP_IXOR:
				case OP_LXOR:
					values [ins->dreg] = LLVMBuildXor (builder, lhs, rhs, dname);
					break;
				case OP_ISHL:
				case OP_LSHL:
					values [ins->dreg] = LLVMBuildShl (builder, lhs, rhs, dname);
					break;
				case OP_ISHR:
				case OP_LSHR:
					values [ins->dreg] = LLVMBuildAShr (builder, lhs, rhs, dname);
					break;
				case OP_ISHR_UN:
				case OP_LSHR_UN:
					values [ins->dreg] = LLVMBuildLShr (builder, lhs, rhs, dname);
					break;
				default:
					g_assert_not_reached ();
				}
				break;
			case OP_IADD_IMM:
			case OP_ISUB_IMM:
			case OP_IMUL_IMM:
			case OP_IREM_IMM:
			case OP_IDIV_IMM:
			case OP_IDIV_UN_IMM:
			case OP_IAND_IMM:
			case OP_IOR_IMM:
			case OP_IXOR_IMM:
			case OP_ISHL_IMM:
			case OP_ISHR_IMM:
			case OP_ISHR_UN_IMM:
			case OP_LADD_IMM:
			case OP_LSUB_IMM:
			case OP_LREM_IMM:
			case OP_LAND_IMM:
			case OP_LOR_IMM:
			case OP_LXOR_IMM:
			case OP_LSHL_IMM:
			case OP_LSHR_IMM:
			case OP_LSHR_UN_IMM:
			case OP_ADD_IMM:
			case OP_AND_IMM:
			case OP_MUL_IMM:
			case OP_SHL_IMM: {
				LLVMValueRef imm;

				if (spec [MONO_INST_SRC1] == 'l')
					imm = LLVMConstInt (LLVMInt64Type (), ins->inst_imm, FALSE);
				else
					imm = LLVMConstInt (LLVMInt32Type (), ins->inst_imm, FALSE);
				if (LLVMGetTypeKind (LLVMTypeOf (lhs)) == LLVMPointerTypeKind)
					lhs = convert (ctx, lhs, IntPtrType ());
				imm = convert (ctx, imm, LLVMTypeOf (lhs));
				switch (ins->opcode) {
				case OP_IADD_IMM:
				case OP_LADD_IMM:
				case OP_ADD_IMM:
					values [ins->dreg] = LLVMBuildAdd (builder, lhs, imm, dname);
					break;
				case OP_ISUB_IMM:
				case OP_LSUB_IMM:
					values [ins->dreg] = LLVMBuildSub (builder, lhs, imm, dname);
					break;
				case OP_IMUL_IMM:
				case OP_MUL_IMM:
					values [ins->dreg] = LLVMBuildMul (builder, lhs, imm, dname);
					break;
				case OP_IDIV_IMM:
				case OP_LDIV_IMM:
					values [ins->dreg] = LLVMBuildSDiv (builder, lhs, imm, dname);
					break;
				case OP_IDIV_UN_IMM:
				case OP_LDIV_UN_IMM:
					values [ins->dreg] = LLVMBuildUDiv (builder, lhs, imm, dname);
					break;
				case OP_IREM_IMM:
				case OP_LREM_IMM:
					values [ins->dreg] = LLVMBuildSRem (builder, lhs, imm, dname);
					break;
				case OP_IAND_IMM:
				case OP_LAND_IMM:
				case OP_AND_IMM:
					values [ins->dreg] = LLVMBuildAnd (builder, lhs, imm, dname);
					break;
				case OP_IOR_IMM:
				case OP_LOR_IMM:
					values [ins->dreg] = LLVMBuildOr (builder, lhs, imm, dname);
					break;
				case OP_IXOR_IMM:
				case OP_LXOR_IMM:
					values [ins->dreg] = LLVMBuildXor (builder, lhs, imm, dname);
					break;
				case OP_ISHL_IMM:
				case OP_LSHL_IMM:
				case OP_SHL_IMM:
					values [ins->dreg] = LLVMBuildShl (builder, lhs, imm, dname);
					break;
				case OP_ISHR_IMM:
				case OP_LSHR_IMM:
					values [ins->dreg] = LLVMBuildAShr (builder, lhs, imm, dname);
					break;
				case OP_ISHR_UN_IMM:
				case OP_LSHR_UN_IMM:
					values [ins->dreg] = LLVMBuildLShr (builder, lhs, imm, dname);
					break;
				default:
					g_assert_not_reached ();
				}
				break;
			}
			case OP_INEG:
				values [ins->dreg] = LLVMBuildSub (builder, LLVMConstInt (LLVMInt32Type (), 0, FALSE), convert (ctx, lhs, LLVMInt32Type ()), dname);
				break;
			case OP_LNEG:
				values [ins->dreg] = LLVMBuildSub (builder, LLVMConstInt (LLVMInt64Type (), 0, FALSE), lhs, dname);
				break;
			case OP_FNEG:
				values [ins->dreg] = LLVMBuildSub (builder, LLVMConstReal (LLVMDoubleType (), 0.0), lhs, dname);
				break;
			case OP_INOT: {
				guint32 v = 0xffffffff;
				values [ins->dreg] = LLVMBuildXor (builder, LLVMConstInt (LLVMInt32Type (), v, FALSE), lhs, dname);
				break;
			}
			case OP_LNOT: {
				guint64 v = 0xffffffffffffffff;
				values [ins->dreg] = LLVMBuildXor (builder, LLVMConstInt (LLVMInt64Type (), v, FALSE), lhs, dname);
				break;
			}
			case OP_X86_LEA: {
				LLVMValueRef v1, v2;

				v1 = LLVMBuildMul (builder, convert (ctx, rhs, IntPtrType ()), LLVMConstInt (IntPtrType (), (1 << ins->backend.shift_amount), FALSE), get_tempname (ctx));
				v2 = LLVMBuildAdd (builder, convert (ctx, lhs, IntPtrType ()), v1, get_tempname (ctx));
				values [ins->dreg] = LLVMBuildAdd (builder, v2, LLVMConstInt (IntPtrType (), ins->inst_imm, FALSE), dname);
				break;
			}

			case OP_ICONV_TO_I1:
			case OP_ICONV_TO_I2:
			case OP_ICONV_TO_I4:
			case OP_ICONV_TO_U1:
			case OP_ICONV_TO_U2:
			case OP_ICONV_TO_U4:
			case OP_LCONV_TO_I1:
			case OP_LCONV_TO_I2:
			case OP_LCONV_TO_U1:
			case OP_LCONV_TO_U2:
			case OP_LCONV_TO_U4: {
				gboolean sign;

				sign = (ins->opcode == OP_ICONV_TO_I1) || (ins->opcode == OP_ICONV_TO_I2) || (ins->opcode == OP_ICONV_TO_I4) || (ins->opcode == OP_LCONV_TO_I1) || (ins->opcode == OP_LCONV_TO_I2);

				/* Have to do two casts since our vregs have type int */
				v = LLVMBuildTrunc (builder, lhs, conv_to_llvm_type (ins->opcode), get_tempname (ctx));
				if (sign)
					values [ins->dreg] = LLVMBuildSExt (builder, v, LLVMInt32Type (), dname);
				else
					values [ins->dreg] = LLVMBuildZExt (builder, v, LLVMInt32Type (), dname);
				break;
			}
			case OP_FCONV_TO_I4:
				values [ins->dreg] = LLVMBuildFPToSI (builder, lhs, LLVMInt32Type (), dname);
				break;
			case OP_FCONV_TO_I1:
				values [ins->dreg] = LLVMBuildSExt (builder, LLVMBuildFPToSI (builder, lhs, LLVMInt8Type (), dname), LLVMInt32Type (), get_tempname (ctx));
				break;
			case OP_FCONV_TO_U1:
				values [ins->dreg] = LLVMBuildZExt (builder, LLVMBuildFPToUI (builder, lhs, LLVMInt8Type (), dname), LLVMInt32Type (), get_tempname (ctx));
				break;
			case OP_FCONV_TO_I2:
				values [ins->dreg] = LLVMBuildSExt (builder, LLVMBuildFPToSI (builder, lhs, LLVMInt16Type (), dname), LLVMInt32Type (), get_tempname (ctx));
				break;
			case OP_FCONV_TO_U2:
				values [ins->dreg] = LLVMBuildZExt (builder, LLVMBuildFPToUI (builder, lhs, LLVMInt16Type (), dname), LLVMInt32Type (), get_tempname (ctx));
				break;
			case OP_FCONV_TO_I8:
				values [ins->dreg] = LLVMBuildFPToSI (builder, lhs, LLVMInt64Type (), dname);
				break;
			case OP_FCONV_TO_I:
				values [ins->dreg] = LLVMBuildFPToSI (builder, lhs, IntPtrType (), dname);
				break;
			case OP_ICONV_TO_R8:
			case OP_LCONV_TO_R8:
				values [ins->dreg] = LLVMBuildSIToFP (builder, lhs, LLVMDoubleType (), dname);
				break;
			case OP_LCONV_TO_R_UN:
				values [ins->dreg] = LLVMBuildUIToFP (builder, lhs, LLVMDoubleType (), dname);
				break;
			case OP_ICONV_TO_R4:
			case OP_LCONV_TO_R4:
				v = LLVMBuildSIToFP (builder, lhs, LLVMFloatType (), get_tempname (ctx));
				values [ins->dreg] = LLVMBuildFPExt (builder, v, LLVMDoubleType (), dname);
				break;
			case OP_FCONV_TO_R4:
				v = LLVMBuildFPTrunc (builder, lhs, LLVMFloatType (), get_tempname (ctx));
				values [ins->dreg] = LLVMBuildFPExt (builder, v, LLVMDoubleType (), dname);
				break;
			case OP_SEXT_I4:
				values [ins->dreg] = LLVMBuildSExt (builder, lhs, LLVMInt64Type (), dname);
				break;
			case OP_ZEXT_I4:
				values [ins->dreg] = LLVMBuildZExt (builder, lhs, LLVMInt64Type (), dname);
				break;
			case OP_TRUNC_I4:
				values [ins->dreg] = LLVMBuildTrunc (builder, lhs, LLVMInt32Type (), dname);
				break;
			case OP_LOCALLOC_IMM: {
				LLVMValueRef v;

				guint32 size = ins->inst_imm;
				size = (size + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

				v = mono_llvm_build_alloca (builder, LLVMInt8Type (), LLVMConstInt (LLVMInt32Type (), size, FALSE), MONO_ARCH_FRAME_ALIGNMENT, get_tempname (ctx));

				if (ins->flags & MONO_INST_INIT) {
					LLVMValueRef args [4];

					args [0] = v;
					args [1] = LLVMConstInt (LLVMInt8Type (), 0, FALSE);
					args [2] = LLVMConstInt (LLVMInt32Type (), size, FALSE);
					args [3] = LLVMConstInt (LLVMInt32Type (), MONO_ARCH_FRAME_ALIGNMENT, FALSE);
					LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.memset.i32"), args, 4, "");
				}

				values [ins->dreg] = v;
				break;
			}
			case OP_LOADI1_MEMBASE:
			case OP_LOADU1_MEMBASE:
			case OP_LOADI2_MEMBASE:
			case OP_LOADU2_MEMBASE:
			case OP_LOADI4_MEMBASE:
			case OP_LOADU4_MEMBASE:
			case OP_LOADI8_MEMBASE:
			case OP_LOADR4_MEMBASE:
			case OP_LOADR8_MEMBASE:
			case OP_LOAD_MEMBASE:
			case OP_LOADI8_MEM:
			case OP_LOAD_MEM: {
				int size = 8;
				LLVMValueRef index;
				LLVMTypeRef t;
				gboolean sext = FALSE, zext = FALSE;

				t = load_store_to_llvm_type (ins->opcode, &size, &sext, &zext);

				if (sext || zext)
					dname = (char*)get_tempname (ctx);

				g_assert (ins->inst_offset % size == 0);
				if ((ins->opcode == OP_LOADI8_MEM) || (ins->opcode == OP_LOAD_MEM)) {
					values [ins->dreg] = LLVMBuildLoad (builder, convert (ctx, LLVMConstInt (IntPtrType (), ins->inst_imm, FALSE), LLVMPointerType (t, 0)), dname);
				} else if (ins->inst_offset == 0) {
					values [ins->dreg] = LLVMBuildLoad (builder, convert (ctx, values [ins->inst_basereg], LLVMPointerType (t, 0)), dname);
				} else {
					index = LLVMConstInt (LLVMInt32Type (), ins->inst_offset / size, FALSE);				
					values [ins->dreg] = LLVMBuildLoad (builder, LLVMBuildGEP (builder, convert (ctx, values [ins->inst_basereg], LLVMPointerType (t, 0)), &index, 1, get_tempname (ctx)), dname);
				}
				if (sext)
					values [ins->dreg] = LLVMBuildSExt (builder, values [ins->dreg], LLVMInt32Type (), dname);
				else if (zext)
					values [ins->dreg] = LLVMBuildZExt (builder, values [ins->dreg], LLVMInt32Type (), dname);
				else if (ins->opcode == OP_LOADR4_MEMBASE)
					values [ins->dreg] = LLVMBuildFPExt (builder, values [ins->dreg], LLVMDoubleType (), dname);
				break;
			}
				
			case OP_STOREI1_MEMBASE_REG:
			case OP_STOREI2_MEMBASE_REG:
			case OP_STOREI4_MEMBASE_REG:
			case OP_STOREI8_MEMBASE_REG:
			case OP_STORER4_MEMBASE_REG:
			case OP_STORER8_MEMBASE_REG:
			case OP_STORE_MEMBASE_REG: {
				int size = 8;
				LLVMValueRef index;
				LLVMTypeRef t;
				gboolean sext = FALSE, zext = FALSE;

				t = load_store_to_llvm_type (ins->opcode, &size, &sext, &zext);

				g_assert (ins->inst_offset % size == 0);
				index = LLVMConstInt (LLVMInt32Type (), ins->inst_offset / size, FALSE);				
				LLVMBuildStore (builder, convert (ctx, values [ins->sreg1], t), LLVMBuildGEP (builder, convert (ctx, values [ins->inst_destbasereg], LLVMPointerType (t, 0)), &index, 1, get_tempname (ctx)));
				break;
			}

			case OP_STOREI1_MEMBASE_IMM:
			case OP_STOREI2_MEMBASE_IMM:
			case OP_STOREI4_MEMBASE_IMM:
			case OP_STOREI8_MEMBASE_IMM:
			case OP_STORE_MEMBASE_IMM: {
				int size = 8;
				LLVMValueRef index;
				LLVMTypeRef t;
				gboolean sext = FALSE, zext = FALSE;

				t = load_store_to_llvm_type (ins->opcode, &size, &sext, &zext);

				g_assert (ins->inst_offset % size == 0);
				index = LLVMConstInt (LLVMInt32Type (), ins->inst_offset / size, FALSE);				
				LLVMBuildStore (builder, convert (ctx, LLVMConstInt (LLVMInt32Type (), ins->inst_imm, FALSE), t), LLVMBuildGEP (builder, convert (ctx, values [ins->inst_destbasereg], LLVMPointerType (t, 0)), &index, 1, get_tempname (ctx)));
				break;
			}

			case OP_CHECK_THIS:
				LLVMBuildLoad (builder, convert (ctx, values [ins->sreg1], LLVMPointerType (IntPtrType (), 0)), get_tempname (ctx));
				break;
			case OP_OUTARG_VTRETADDR:
				break;
			case OP_VOIDCALL:
			case OP_CALL:
			case OP_LCALL:
			case OP_FCALL:
			case OP_VCALL:
			case OP_VOIDCALL_MEMBASE:
			case OP_CALL_MEMBASE:
			case OP_LCALL_MEMBASE:
			case OP_FCALL_MEMBASE:
			case OP_VCALL_MEMBASE: {
				MonoCallInst *call = (MonoCallInst*)ins;
				MonoMethodSignature *sig = call->signature;
				LLVMValueRef callee;
				LLVMValueRef *args;
				GSList *l;
				int i, pindex;
				gboolean vretaddr;
				LLVMTypeRef llvm_sig;
				gpointer target;

				vretaddr = call->vret_var != NULL;

				llvm_sig = sig_to_llvm_sig (ctx, sig, vretaddr);
				CHECK_FAILURE (ctx);

				if (call->rgctx_reg) {
					LLVM_FAILURE (ctx, "rgctx reg");
				}

				/* FIXME: Avoid creating duplicate methods */

				if (ins->flags & MONO_INST_HAS_METHOD) {
					callee = LLVMAddFunction (module, "", llvm_sig);

					target = 
						mono_create_jit_trampoline_in_domain (mono_domain_get (),
															  call->method);
					LLVMAddGlobalMapping (ee, callee, target);
				} else {
					MonoJitICallInfo *info = mono_find_jit_icall_by_addr (call->fptr);

					callee = LLVMAddFunction (module, "", llvm_sig);

					if (info) {
						MonoJumpInfo ji;

						memset (&ji, 0, sizeof (ji));
						ji.type = MONO_PATCH_INFO_JIT_ICALL_ADDR;
						ji.data.target = info->name;

						target = mono_resolve_patch_target (cfg->method, cfg->domain, NULL, &ji, FALSE);
						LLVMAddGlobalMapping (ee, callee, target);
					} else {
						target = NULL;
						if (cfg->abs_patches) {
							MonoJumpInfo *abs_ji = g_hash_table_lookup (cfg->abs_patches, call->fptr);
							if (abs_ji) {
								target = mono_resolve_patch_target (cfg->method, cfg->domain, NULL, abs_ji, FALSE);
								LLVMAddGlobalMapping (ee, callee, target);
							}
						}
						if (!target)
							LLVMAddGlobalMapping (ee, callee, (gpointer)call->fptr);
					}
				}

				if (ins->opcode == OP_VOIDCALL_MEMBASE || ins->opcode == OP_CALL_MEMBASE || ins->opcode == OP_VCALL_MEMBASE || ins->opcode == OP_LCALL_MEMBASE || ins->opcode == OP_FCALL_MEMBASE) {
					int size = sizeof (gpointer);
					LLVMValueRef index;

					g_assert (ins->inst_offset % size == 0);
					index = LLVMConstInt (LLVMInt32Type (), ins->inst_offset / size, FALSE);				

					callee = convert (ctx, LLVMBuildLoad (builder, LLVMBuildGEP (builder, convert (ctx, values [ins->inst_basereg], LLVMPointerType (IntPtrType (), 0)), &index, 1, get_tempname (ctx)), get_tempname (ctx)), LLVMPointerType (llvm_sig, 0));

					// FIXME: mono_arch_get_vcall_slot () can't decode the code
					// generated by LLVM
					//LLVM_FAILURE (ctx, "virtual call");

					if (call->method && call->method->klass->flags & TYPE_ATTRIBUTE_INTERFACE)
						/* No support for passing the IMT argument */
						LLVM_FAILURE (ctx, "imt");
				} else {
					if (ins->flags & MONO_INST_HAS_METHOD) {
					}
				}

				/* Collect and convert arguments */
				args = alloca (sizeof (LLVMValueRef) * (sig->param_count + sig->hasthis + vretaddr));
				l = call->out_ireg_args;
				pindex = 0;
				if (vretaddr) {
					MonoInst *var = call->vret_var->inst_p0;

					g_assert (addresses [var->dreg]);
					args [pindex ++] = LLVMBuildPtrToInt (builder, addresses [var->dreg], IntPtrType (), get_tempname (ctx));
				}

				for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
					guint32 regpair;
					int reg;

					regpair = (guint32)(gssize)(l->data);
					reg = regpair & 0xffffff;
					args [pindex] = values [reg];
					if (!values [reg])
						/* Vtypes */
						LLVM_FAILURE (ctx, "vtype arg");
					g_assert (args [pindex]);
					if (i == 0 && sig->hasthis)
						args [pindex] = convert (ctx, args [pindex], IntPtrType ());
					else
						args [pindex] = convert (ctx, args [pindex], type_to_llvm_arg_type (ctx, sig->params [i - sig->hasthis]));

					pindex ++;
					l = l->next;
				}

				// FIXME: Align call sites

				if (sig->ret->type != MONO_TYPE_VOID && !vretaddr)
					/* FIXME: Convert res */
					values [ins->dreg] = convert (ctx, LLVMBuildCall (builder, callee, args, pindex, dname), llvm_type_to_stack_type (type_to_llvm_type (ctx, sig->ret)));
				else
					LLVMBuildCall (builder, callee, args, pindex, "");
				break;
			}
			case OP_GOT_ENTRY: {
				LLVMValueRef indexes [2];

				indexes [0] = LLVMConstInt (LLVMInt32Type (), 0, FALSE);
				indexes [1] = LLVMConstInt (LLVMInt32Type (), (gssize)ins->inst_p0, FALSE);
				values [ins->dreg] = LLVMBuildLoad (builder, LLVMBuildGEP (builder, ctx->got_var, indexes, 2, get_tempname (ctx)), dname);
				break;
			}
			case OP_THROW: {
				MonoMethodSignature *throw_sig;
				LLVMValueRef callee;

				throw_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
				throw_sig->ret = &mono_defaults.void_class->byval_arg;
				throw_sig->params [0] = &mono_defaults.object_class->byval_arg;
				// FIXME: Prevent duplicates
				callee = LLVMAddFunction (module, "mono_arch_throw_exception", sig_to_llvm_sig (ctx, throw_sig, FALSE));
				LLVMAddGlobalMapping (ee, callee, resolve_patch (cfg, MONO_PATCH_INFO_INTERNAL_METHOD, "mono_arch_throw_exception"));
				LLVMBuildCall (builder, callee, &values [ins->sreg1], 1, "");
				break;
			}
			case OP_NOT_REACHED:
				LLVMBuildUnreachable (builder);
				has_terminator = TRUE;
				/* Might have instructions after this */
				while (ins->next) {
					MonoInst *next = ins->next;
					MONO_DELETE_INS (bb, next);
				}				
				break;
			case OP_LDADDR: {
				MonoInst *var = ins->inst_p0;

				values [ins->dreg] = addresses [var->dreg];
				break;
			}
			case OP_SIN: {
				LLVMValueRef args [1];

				args [0] = lhs;
				values [ins->dreg] = LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.sin.f64"), args, 1, dname);
				break;
			}
			case OP_COS: {
				LLVMValueRef args [1];

				args [0] = lhs;
				values [ins->dreg] = LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.cos.f64"), args, 1, dname);
				break;
			}
				/* test_0_sqrt_nan fails with LLVM */
				/*
			case OP_SQRT: {
				LLVMValueRef args [1];

				args [0] = lhs;
				values [ins->dreg] = LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.sqrt.f64"), args, 1, dname);
				break;
			}
				*/

			case OP_IMIN:
			case OP_LMIN: {
				LLVMValueRef v = LLVMBuildICmp (builder, LLVMIntSLE, lhs, rhs, get_tempname (ctx));
				values [ins->dreg] = LLVMBuildSelect (builder, v, lhs, rhs, dname);
				break;
			}
			case OP_IMAX:
			case OP_LMAX: {
				LLVMValueRef v = LLVMBuildICmp (builder, LLVMIntSGE, lhs, rhs, get_tempname (ctx));
				values [ins->dreg] = LLVMBuildSelect (builder, v, lhs, rhs, dname);
				break;
			}
			case OP_IMIN_UN:
			case OP_LMIN_UN: {
				LLVMValueRef v = LLVMBuildICmp (builder, LLVMIntULE, lhs, rhs, get_tempname (ctx));
				values [ins->dreg] = LLVMBuildSelect (builder, v, lhs, rhs, dname);
				break;
			}
			case OP_IMAX_UN:
			case OP_LMAX_UN: {
				LLVMValueRef v = LLVMBuildICmp (builder, LLVMIntUGE, lhs, rhs, get_tempname (ctx));
				values [ins->dreg] = LLVMBuildSelect (builder, v, lhs, rhs, dname);
				break;
			}
			case OP_ATOMIC_EXCHANGE_I4: {
				LLVMValueRef args [2];

				g_assert (ins->inst_offset == 0);

				args [0] = convert (ctx, lhs, LLVMPointerType (LLVMInt32Type (), 0));
				args [1] = rhs;
				values [ins->dreg] = LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.atomic.swap.i32.p0i32"), args, 2, dname);
				break;
			}
			case OP_ATOMIC_EXCHANGE_I8: {
				LLVMValueRef args [2];

				g_assert (ins->inst_offset == 0);

				args [0] = convert (ctx, lhs, LLVMPointerType (LLVMInt64Type (), 0));
				args [1] = rhs;
				values [ins->dreg] = LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.atomic.swap.i64.p0i64"), args, 2, dname);
				break;
			}
			case OP_ATOMIC_ADD_NEW_I4: {
				LLVMValueRef args [2];

				g_assert (ins->inst_offset == 0);

				args [0] = convert (ctx, lhs, LLVMPointerType (LLVMInt32Type (), 0));
				args [1] = rhs;
				values [ins->dreg] = LLVMBuildAdd (builder, LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.atomic.load.add.i32.p0i32"), args, 2, get_tempname (ctx)), args [1], dname);
				break;
			}
			case OP_ATOMIC_ADD_NEW_I8: {
				LLVMValueRef args [2];

				g_assert (ins->inst_offset == 0);

				args [0] = convert (ctx, lhs, LLVMPointerType (LLVMInt64Type (), 0));
				args [1] = convert (ctx, rhs, LLVMInt64Type ());
				values [ins->dreg] = LLVMBuildAdd (builder, LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.atomic.load.add.i64.p0i64"), args, 2, get_tempname (ctx)), args [1], dname);
				break;
			}
#if 0
			case OP_ATOMIC_CAS_IMM_I4: {
				LLVMValueRef args [3];

				args [0] = convert (ctx, lhs, LLVMPointerType (LLVMInt32Type (), 0));
				/* comparand */
				args [1] = LLVMConstInt (LLVMInt32Type (), GPOINTER_TO_INT (ins->backend.data), FALSE);
				/* new value */
				args [2] = rhs;
				values [ins->dreg] = LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.atomic.cmp.swap.i32.p0i32"), args, 3, dname);
				break;
			}
#endif
			case OP_MEMORY_BARRIER: {
				LLVMValueRef args [5];

				for (i = 0; i < 5; ++i)
					args [i] = LLVMConstInt (LLVMInt1Type (), TRUE, TRUE);

				LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.memory.barrier"), args, 5, "");
				break;
			}

			/*
			 * Overflow opcodes.
			 */
			// FIXME: LLVM can't handle mul_ovf_un yet
			case OP_IADD_OVF:
			case OP_IADD_OVF_UN:
			case OP_ISUB_OVF:
			case OP_ISUB_OVF_UN:
			case OP_IMUL_OVF:
				//case OP_IMUL_OVF_UN:
			case OP_LADD_OVF:
			case OP_LSUB_OVF:
			case OP_LSUB_OVF_UN:
			case OP_LMUL_OVF: {
				//case OP_LMUL_OVF_UN: {
				LLVMValueRef args [2], val, ovf, func;

				emit_cond_throw_pos (ctx);

				args [0] = lhs;
				args [1] = rhs;
				func = LLVMGetNamedFunction (module, ovf_op_to_intrins (ins->opcode));
				g_assert (func);
				val = LLVMBuildCall (builder, func, args, 2, "");
				values [ins->dreg] = LLVMBuildExtractValue (builder, val, 0, dname);
				ovf = LLVMBuildExtractValue (builder, val, 1, get_tempname (ctx));
				emit_cond_system_exception (ctx, bb, "OverflowException", ovf);
				builder = ctx->builder;
				break;
			}

			/* 
			 * Valuetypes.
			 *   We currently model them using arrays. Promotion to local vregs is 
			 * disabled for them in mono_handle_global_vregs () in the LLVM case, 
			 * so we always have an entry in cfg->varinfo for them.
			 * FIXME: Is this needed ?
			 */
			case OP_VZERO: {
				MonoClass *klass = ins->klass;
				LLVMValueRef args [4];

				if (!klass) {
					// FIXME:
					LLVM_FAILURE (ctx, "!klass");
					break;
				}

				args [0] = LLVMBuildBitCast (builder, addresses [ins->dreg], LLVMPointerType (LLVMInt8Type (), 0), get_tempname (ctx));
				args [1] = LLVMConstInt (LLVMInt8Type (), 0, FALSE);
				args [2] = LLVMConstInt (LLVMInt32Type (), mono_class_value_size (klass, NULL), FALSE);
				// FIXME: Alignment
				args [3] = LLVMConstInt (LLVMInt32Type (), 0, FALSE);
				LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.memset.i32"), args, 4, "");
				break;
			}

			case OP_STOREV_MEMBASE:
			case OP_LOADV_MEMBASE:
			case OP_VMOVE: {
				MonoClass *klass = ins->klass;
				LLVMValueRef src, dst, args [4];

				if (!klass) {
					// FIXME:
					LLVM_FAILURE (ctx, "!klass");
					break;
				}

				switch (ins->opcode) {
				case OP_STOREV_MEMBASE:
					src = LLVMBuildBitCast (builder, addresses [ins->sreg1], LLVMPointerType (LLVMInt8Type (), 0), get_tempname (ctx));
					dst = convert (ctx, LLVMBuildAdd (builder, values [ins->inst_destbasereg], LLVMConstInt (IntPtrType (), ins->inst_offset, FALSE), get_tempname (ctx)), LLVMPointerType (LLVMInt8Type (), 0));
					break;
				case OP_LOADV_MEMBASE:
					if (!addresses [ins->dreg])
						addresses [ins->dreg] = LLVMBuildAlloca (builder, type_to_llvm_type (ctx, &klass->byval_arg), get_tempname (ctx));
					src = convert (ctx, LLVMBuildAdd (builder, values [ins->inst_basereg], LLVMConstInt (IntPtrType (), ins->inst_offset, FALSE), get_tempname (ctx)), LLVMPointerType (LLVMInt8Type (), 0));
					dst = LLVMBuildBitCast (builder, addresses [ins->dreg], LLVMPointerType (LLVMInt8Type (), 0), get_tempname (ctx));
					break;
				case OP_VMOVE:
					if (!addresses [ins->dreg])
						addresses [ins->dreg] = LLVMBuildAlloca (builder, type_to_llvm_type (ctx, &klass->byval_arg), get_tempname (ctx));
					src = LLVMBuildBitCast (builder, addresses [ins->sreg1], LLVMPointerType (LLVMInt8Type (), 0), get_tempname (ctx));
					dst = LLVMBuildBitCast (builder, addresses [ins->dreg], LLVMPointerType (LLVMInt8Type (), 0), get_tempname (ctx));
					break;
				default:
					g_assert_not_reached ();
				}

				args [0] = dst;
				args [1] = src;
				args [2] = LLVMConstInt (LLVMInt32Type (), mono_class_value_size (klass, NULL), FALSE);
				args [3] = LLVMConstInt (LLVMInt32Type (), 0, FALSE);
				// FIXME: Alignment
				args [3] = LLVMConstInt (LLVMInt32Type (), 0, FALSE);
				LLVMBuildCall (builder, LLVMGetNamedFunction (module, "llvm.memcpy.i32"), args, 4, "");
				break;
			}

			default: {
				char *reason = g_strdup_printf ("opcode %s", mono_inst_name (ins->opcode));
				LLVM_FAILURE (ctx, reason);
				break;
			}
			}

			/* Convert the value to the type required by phi nodes */
			if (spec [MONO_INST_DEST] != ' ' && !MONO_IS_STORE_MEMBASE (ins) && vreg_types [ins->dreg])
				values [ins->dreg] = convert (ctx, values [ins->dreg], vreg_types [ins->dreg]);

			/* Add stores for volatile variables */
			if (spec [MONO_INST_DEST] != ' ' && spec [MONO_INST_DEST] != 'v' && !MONO_IS_STORE_MEMBASE (ins))
				emit_volatile_store (ctx, ins->dreg);
		}

		if (!has_terminator && bb->next_bb && (bb == cfg->bb_entry || bb->in_count > 0))
			LLVMBuildBr (builder, get_bb (ctx, bb->next_bb));

		if (bb == cfg->bb_exit && sig->ret->type == MONO_TYPE_VOID)
			LLVMBuildRetVoid (builder);
	}

	/* Add incoming phi values */
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		GSList *ins_list = g_hash_table_lookup (phi_nodes, GUINT_TO_POINTER (bb));

		while (ins_list) {
			PhiNode *node = ins_list->data;
			MonoInst *phi = node->phi;
			int sreg1 = phi->inst_phi_args [node->index + 1];
			LLVMBasicBlockRef in_bb;

			in_bb = get_end_bb (ctx, node->bb->in_bb [node->index]);

			g_assert (LLVMTypeOf (values [sreg1]) == LLVMTypeOf (values [phi->dreg]));
			LLVMAddIncoming (values [phi->dreg], &values [sreg1], &in_bb, 1);

			ins_list = ins_list->next;
		}
	}

	if (last)
		LLVMDumpValue (method);

	mono_llvm_optimize_method (method);

	if (cfg->verbose_level > 1)
		LLVMDumpValue (method);

	cfg->native_code = LLVMGetPointerToGlobal (ee, method);

	/* Set by emit_cb */
	g_assert (cfg->code_len);

	goto CLEANUP;

 FAILURE:

	if (method) {
		/* Need to add unused phi nodes as they can be referenced by other values */
		LLVMBasicBlockRef phi_bb = LLVMAppendBasicBlock (method, "PHI_BB");
		LLVMBuilderRef builder;

		builder = LLVMCreateBuilder ();
		LLVMPositionBuilderAtEnd (builder, phi_bb);

		for (i = 0; i < phi_values->len; ++i) {
			LLVMValueRef v = g_ptr_array_index (phi_values, i);
			if (LLVMGetInstructionParent (v) == NULL)
				LLVMInsertIntoBuilder (builder, v);
		}
		
		LLVMDeleteFunction (method);
	}

 CLEANUP:
	g_free (values);
	g_free (addresses);
	g_free (vreg_types);
	g_hash_table_destroy (phi_nodes);
	g_ptr_array_free (phi_values, TRUE);
	g_free (ctx->bblocks);
	g_free (ctx->end_bblocks);

	g_free (ctx);

	TlsSetValue (current_cfg_tls_id, NULL);

	mono_loader_unlock ();
}

static unsigned char*
alloc_cb (LLVMValueRef function, int size)
{
	MonoCompile *cfg;

	cfg = TlsGetValue (current_cfg_tls_id);

	if (cfg) {
		// FIXME: dynamic
		return mono_domain_code_reserve (cfg->domain, size);
	} else {
		return mono_domain_code_reserve (mono_domain_get (), size);
	}
}

static void
emitted_cb (LLVMValueRef function, void *start, void *end)
{
	MonoCompile *cfg;

	cfg = TlsGetValue (current_cfg_tls_id);
	g_assert (cfg);
	cfg->code_len = (guint8*)end - (guint8*)start;
}

static void
exception_cb (void *data)
{
	MonoCompile *cfg;

	cfg = TlsGetValue (current_cfg_tls_id);
	g_assert (cfg);

	/*
	 * data points to a DWARF FDE structure, convert it to our unwind format and
	 * save it.
	 * An alternative would be to save it directly, and modify our unwinder to work
	 * with it.
	 */
	cfg->encoded_unwind_ops = mono_unwind_get_ops_from_fde ((guint8*)data, &cfg->encoded_unwind_ops_len);
}

static void
mono_llvm_init (void)
{
	current_cfg_tls_id = TlsAlloc ();

	module = LLVMModuleCreateWithName ("mono");

	ee = mono_llvm_create_ee (LLVMCreateModuleProviderForExistingModule (module), alloc_cb, emitted_cb, exception_cb);

	llvm_types = g_hash_table_new (NULL, NULL);

	/* Emit declarations of instrinsics */
	{
		LLVMTypeRef memset_params [] = { LLVMPointerType (LLVMInt8Type (), 0), LLVMInt8Type (), LLVMInt32Type (), LLVMInt32Type () };

		LLVMAddFunction (module, "llvm.memset.i32", LLVMFunctionType (LLVMVoidType (), memset_params, 4, FALSE));
	}

	{
		LLVMTypeRef memcpy_params [] = { LLVMPointerType (LLVMInt8Type (), 0), LLVMPointerType (LLVMInt8Type (), 0), LLVMInt32Type (), LLVMInt32Type () };

		LLVMAddFunction (module, "llvm.memcpy.i32", LLVMFunctionType (LLVMVoidType (), memcpy_params, 4, FALSE));
	}

	{
		LLVMTypeRef params [] = { LLVMDoubleType () };

		LLVMAddFunction (module, "llvm.sin.f64", LLVMFunctionType (LLVMDoubleType (), params, 1, FALSE));
		LLVMAddFunction (module, "llvm.cos.f64", LLVMFunctionType (LLVMDoubleType (), params, 1, FALSE));
		LLVMAddFunction (module, "llvm.sqrt.f64", LLVMFunctionType (LLVMDoubleType (), params, 1, FALSE));
	}

	{
		LLVMTypeRef membar_params [] = { LLVMInt1Type (), LLVMInt1Type (), LLVMInt1Type (), LLVMInt1Type (), LLVMInt1Type () };

		LLVMAddFunction (module, "llvm.atomic.swap.i32.p0i32", LLVMFunctionType2 (LLVMInt32Type (), LLVMPointerType (LLVMInt32Type (), 0), LLVMInt32Type (), FALSE));
		LLVMAddFunction (module, "llvm.atomic.swap.i64.p0i64", LLVMFunctionType2 (LLVMInt64Type (), LLVMPointerType (LLVMInt64Type (), 0), LLVMInt64Type (), FALSE));
		LLVMAddFunction (module, "llvm.atomic.load.add.i32.p0i32", LLVMFunctionType2 (LLVMInt32Type (), LLVMPointerType (LLVMInt32Type (), 0), LLVMInt32Type (), FALSE));
		LLVMAddFunction (module, "llvm.atomic.load.add.i64.p0i64", LLVMFunctionType2 (LLVMInt64Type (), LLVMPointerType (LLVMInt64Type (), 0), LLVMInt64Type (), FALSE));
		LLVMAddFunction (module, "llvm.atomic.cmp.swap.i32.p0i32", LLVMFunctionType3 (LLVMInt32Type (), LLVMPointerType (LLVMInt32Type (), 0), LLVMInt32Type (), LLVMInt32Type (), FALSE));
		LLVMAddFunction (module, "llvm.memory.barrier", LLVMFunctionType (LLVMVoidType (), membar_params, 5, FALSE));
	}

	{
		LLVMTypeRef ovf_res_i32 [] = { LLVMInt32Type (), LLVMInt1Type () };
		LLVMTypeRef ovf_params_i32 [] = { LLVMInt32Type (), LLVMInt32Type () };

		LLVMAddFunction (module, "llvm.sadd.with.overflow.i32", LLVMFunctionType (LLVMStructType (ovf_res_i32, 2, FALSE), ovf_params_i32, 2, FALSE));
		LLVMAddFunction (module, "llvm.uadd.with.overflow.i32", LLVMFunctionType (LLVMStructType (ovf_res_i32, 2, FALSE), ovf_params_i32, 2, FALSE));
		LLVMAddFunction (module, "llvm.ssub.with.overflow.i32", LLVMFunctionType (LLVMStructType (ovf_res_i32, 2, FALSE), ovf_params_i32, 2, FALSE));
		LLVMAddFunction (module, "llvm.usub.with.overflow.i32", LLVMFunctionType (LLVMStructType (ovf_res_i32, 2, FALSE), ovf_params_i32, 2, FALSE));
		LLVMAddFunction (module, "llvm.smul.with.overflow.i32", LLVMFunctionType (LLVMStructType (ovf_res_i32, 2, FALSE), ovf_params_i32, 2, FALSE));
		LLVMAddFunction (module, "llvm.umul.with.overflow.i32", LLVMFunctionType (LLVMStructType (ovf_res_i32, 2, FALSE), ovf_params_i32, 2, FALSE));
	}

	{
		LLVMTypeRef ovf_res_i64 [] = { LLVMInt64Type (), LLVMInt1Type () };
		LLVMTypeRef ovf_params_i64 [] = { LLVMInt64Type (), LLVMInt64Type () };

		LLVMAddFunction (module, "llvm.sadd.with.overflow.i64", LLVMFunctionType (LLVMStructType (ovf_res_i64, 2, FALSE), ovf_params_i64, 2, FALSE));
		LLVMAddFunction (module, "llvm.uadd.with.overflow.i64", LLVMFunctionType (LLVMStructType (ovf_res_i64, 2, FALSE), ovf_params_i64, 2, FALSE));
		LLVMAddFunction (module, "llvm.ssub.with.overflow.i64", LLVMFunctionType (LLVMStructType (ovf_res_i64, 2, FALSE), ovf_params_i64, 2, FALSE));
		LLVMAddFunction (module, "llvm.usub.with.overflow.i64", LLVMFunctionType (LLVMStructType (ovf_res_i64, 2, FALSE), ovf_params_i64, 2, FALSE));
		LLVMAddFunction (module, "llvm.smul.with.overflow.i64", LLVMFunctionType (LLVMStructType (ovf_res_i64, 2, FALSE), ovf_params_i64, 2, FALSE));
		LLVMAddFunction (module, "llvm.umul.with.overflow.i64", LLVMFunctionType (LLVMStructType (ovf_res_i64, 2, FALSE), ovf_params_i64, 2, FALSE));
	}
}

/*
  DESIGN:
  - Emit LLVM IR from the mono IR using the LLVM C API.
  - The original arch specific code remains, so we can fall back to it if we run
    into something we can't handle.
  FIXME:
  - llvm's PrettyStackTrace class seems to register a signal handler which screws
    up our GC. Also, it calls sigaction () a _lot_ of times instead of just once.
*/

/*  
  A partial list of issues:
  - Handling of opcodes which can throw exceptions.

      In the mono JIT, these are implemented using code like this:
	  method:
      <compare>
	  throw_pos:
	  b<cond> ex_label
	  <rest of code>
      ex_label:
	  push throw_pos - method
	  call <exception trampoline>

	  The problematic part is push throw_pos - method, which cannot be represented
      in the LLVM IR, since it does not support label values.
	  -> this can be implemented in AOT mode using inline asm + labels, but cannot
	  be implemented in JIT mode ?
	  -> a possible but slower implementation would use the normal exception 
      throwing code but it would need to control the placement of the throw code
      (it needs to be exactly after the compare+branch).
	  -> perhaps add a PC offset intrinsics ?

  - efficient implementation of .ovf opcodes.

	  These are currently implemented as:
	  <ins which sets the condition codes>
	  b<cond> ex_label

	  Some overflow opcodes are now supported by LLVM SVN.

  - exception handling, unwinding.
    - SSA is disabled for methods with exception handlers    
	- How to obtain unwind info for LLVM compiled methods ?
	  -> this is now solved by converting the unwind info generated by LLVM
	     into our format.
	- LLVM uses the c++ exception handling framework, while we use our home grown
      code, and couldn't use the c++ one:
      - its not supported under VC++, other exotic platforms.
	  - it might be impossible to support filter clauses with it.

  - trampolines.
  
    The trampolines need a predictable call sequence, since they need to disasm
    the calling code to obtain register numbers / offsets.

    LLVM currently generates this code in non-JIT mode:
	   mov    -0x98(%rax),%eax
	   callq  *%rax
    Here, the vtable pointer is lost. 
    -> solution: use one vtable trampoline per class.

  - passing/receiving the IMT pointer/RGCTX.
    -> solution: pass them as normal arguments ?

  - argument passing.
  
	  LLVM does not allow the specification of argument registers etc. This means
      that all calls are made according to the platform ABI.

  - ldaddr.

    Supported though alloca, we need to emit the load/store code.

  - types.

    The mono JIT uses pointer sized iregs/double fregs, while LLVM uses precisely
    typed registers, so we have to keep track of the precise LLVM type of each vreg.
    This is made easier because the IR is already in SSA form.
    An additional problem is that our IR is not consistent with types, i.e. i32/ia64 
	types are frequently used incorrectly.
*/

/* FIXME: Normalize some aspects of the mono IR to allow easier translation, like:
 *   - each bblock should end with a branch
 *   - setting the return value, making cfg->ret non-volatile
 * - merge some changes back to HEAD, to reduce the differences.
 * - avoid some transformations in the JIT which make it harder for us to generate
 *   code.
 * - fix memory leaks.
 * - use pointer types to help optimizations.
 */
