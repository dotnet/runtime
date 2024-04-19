/**
 * \file
 */

#include <config.h>
#include <mono/utils/mono-compiler.h>

#ifndef DISABLE_JIT

#include "mini.h"
#include "ir-emit.h"
#include "mini-runtime.h"
#include "llvmonly-runtime.h"
#include "mini-llvm.h"
#include "jit-icalls.h"
#include "aot-compiler.h"
#include <mono/metadata/abi-details.h>
#include <mono/metadata/class-abi-details.h>
#include <mono/utils/mono-utils-debug.h>
#include "mono/metadata/icall-signatures.h"

static const gboolean debug_tailcall_break_compile = FALSE; // break in method_to_ir
static const gboolean debug_tailcall_break_run = FALSE;     // insert breakpoint in generated code

MonoJumpInfoTarget
mono_call_to_patch (MonoCallInst *call)
{
	MonoJumpInfoTarget patch;
	MonoJitICallId jit_icall_id;

	// This is similar to amd64 emit_call.

	if (call->inst.flags & MONO_INST_HAS_METHOD) {
		patch.type = MONO_PATCH_INFO_METHOD;
		patch.target = call->method;
	} else if ((jit_icall_id = call->jit_icall_id)) {
		patch.type = MONO_PATCH_INFO_JIT_ICALL_ID;
		patch.target = GUINT_TO_POINTER (jit_icall_id);
	} else {
		patch.type = MONO_PATCH_INFO_ABS;
		patch.target = call->fptr;
	}
	return patch;
}

void
mono_call_add_patch_info (MonoCompile *cfg, MonoCallInst *call, int ip)
{
	const MonoJumpInfoTarget patch = mono_call_to_patch (call);
	mono_add_patch_info (cfg, ip, patch.type, patch.target);
}

void
mini_test_tailcall (MonoCompile *cfg, gboolean tailcall)
{
	// A lot of tests say "tailcall" throughout their verbose output.
	// "tailcalllog" is more searchable.
	//
	// Do not change "tailcalllog" here without changing other places, e.g. tests that search for it.
	//
	g_assertf (tailcall || !mini_debug_options.test_tailcall_require, "tailcalllog fail from %s", cfg->method->name);
	mono_tailcall_print ("tailcalllog %s from %s\n", tailcall ? "success" : "fail", cfg->method->name);
}

void
mini_emit_tailcall_parameters (MonoCompile *cfg, MonoMethodSignature *sig)
{
	// OP_TAILCALL_PARAMETER helps compute the size of code, in order
	// to size branches around OP_TAILCALL_[REG,MEMBASE].
	//
	// The actual bytes are output from OP_TAILCALL_[REG,MEMBASE].
	// OP_TAILCALL_PARAMETER is an overestimate because typically
	// many parameters are in registers.

	const int n = sig->param_count + (sig->hasthis ? 1 : 0);
	for (int i = 0; i < n; ++i) {
		MonoInst *ins;
		MONO_INST_NEW (cfg, ins, OP_TAILCALL_PARAMETER);
		MONO_ADD_INS (cfg->cbb, ins);
	}

}

static int
ret_type_to_call_opcode (MonoCompile *cfg, MonoType *type, int calli, int virt)
{
handle_enum:
	type = mini_get_underlying_type (type);
	switch (type->type) {
	case MONO_TYPE_VOID:
		return calli? OP_VOIDCALL_REG: virt? OP_VOIDCALL_MEMBASE: OP_VOIDCALL;
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return calli? OP_CALL_REG: virt? OP_CALL_MEMBASE: OP_CALL;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return calli? OP_LCALL_REG: virt? OP_LCALL_MEMBASE: OP_LCALL;
	case MONO_TYPE_R4:
		if (cfg->r4fp)
			return calli? OP_RCALL_REG: virt? OP_RCALL_MEMBASE: OP_RCALL;
		else
			return calli? OP_FCALL_REG: virt? OP_FCALL_MEMBASE: OP_FCALL;
	case MONO_TYPE_R8:
		return calli? OP_FCALL_REG: virt? OP_FCALL_MEMBASE: OP_FCALL;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			goto handle_enum;
		} else {
			if (mini_class_is_simd (cfg, mono_class_from_mono_type_internal (type)))
				return calli? OP_XCALL_REG: virt? OP_XCALL_MEMBASE: OP_XCALL;
			else
				return calli? OP_VCALL_REG: virt? OP_VCALL_MEMBASE: OP_VCALL;
		}
	case MONO_TYPE_TYPEDBYREF:
		return calli? OP_VCALL_REG: virt? OP_VCALL_MEMBASE: OP_VCALL;
	case MONO_TYPE_GENERICINST: {
		if (mini_class_is_simd (cfg, mono_class_from_mono_type_internal (type)))
			return calli? OP_XCALL_REG: virt? OP_XCALL_MEMBASE: OP_XCALL;
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto handle_enum;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		/* gsharedvt */
		return calli? OP_VCALL_REG: virt? OP_VCALL_MEMBASE: OP_VCALL;
	default:
		g_error ("unknown type 0x%02x in ret_type_to_call_opcode", type->type);
	}
	return -1;
}

MonoCallInst *
mini_emit_call_args (MonoCompile *cfg, MonoMethodSignature *sig,
					 MonoInst **args, gboolean calli, gboolean virtual_, gboolean tailcall,
					 gboolean rgctx, gboolean unbox_trampoline, MonoMethod *target)
{
	MonoType *sig_ret;
	MonoCallInst *call;

	cfg->has_calls = TRUE;

	if (tailcall && cfg->llvm_only) {
		// FIXME tailcall should not be changed this late.
		// FIXME It really should not be changed due to llvm_only.
		// Accuracy is presently available MONO_IS_TAILCALL_OPCODE (call).
		tailcall = FALSE;
		mono_tailcall_print ("losing tailcall in %s due to llvm_only\n", cfg->method->name);
		mini_test_tailcall (cfg, FALSE);
	}

	if (tailcall && (debug_tailcall_break_compile || debug_tailcall_break_run)
		&& mono_is_usermode_native_debugger_present ()) {

		if (debug_tailcall_break_compile)
			G_BREAKPOINT ();

		if (tailcall && debug_tailcall_break_run) { // Can change tailcall in debugger.
			MonoInst *brk;
			MONO_INST_NEW (cfg, brk, OP_BREAK);
			MONO_ADD_INS (cfg->cbb, brk);
		}
	}

	if (tailcall) {
		mini_profiler_emit_tail_call (cfg, target);
		mini_emit_tailcall_parameters (cfg, sig);
		MONO_INST_NEW_CALL (cfg, call, calli ? OP_TAILCALL_REG : virtual_ ? OP_TAILCALL_MEMBASE : OP_TAILCALL);
	} else
		MONO_INST_NEW_CALL (cfg, call, ret_type_to_call_opcode (cfg, sig->ret, calli, virtual_));

	call->args = args;
	call->signature = sig;
	call->rgctx_reg = rgctx;
	sig_ret = mini_get_underlying_type (sig->ret);

	mini_type_to_eval_stack_type ((cfg), sig_ret, &call->inst);

	if (tailcall) {
		if (mini_type_is_vtype (sig_ret)) {
			call->vret_var = cfg->vret_addr;
			//g_assert_not_reached ();
		}
	} else if (mini_type_is_vtype (sig_ret)) {
		MonoInst *temp = mono_compile_create_var (cfg, sig_ret, OP_LOCAL);
		MonoInst *loada;

		temp->backend.is_pinvoke = sig->pinvoke && !sig->marshalling_disabled;

		/*
		 * We use a new opcode OP_OUTARG_VTRETADDR instead of LDADDR for emitting the
		 * address of return value to increase optimization opportunities.
		 * Before vtype decomposition, the dreg of the call ins itself represents the
		 * fact the call modifies the return value. After decomposition, the call will
		 * be transformed into one of the OP_VOIDCALL opcodes, and the VTRETADDR opcode
		 * will be transformed into an LDADDR.
		 */
		MONO_INST_NEW (cfg, loada, OP_OUTARG_VTRETADDR);
		loada->dreg = alloc_preg (cfg);
		loada->inst_p0 = temp;
		/* We reference the call too since call->dreg could change during optimization */
		loada->inst_p1 = call;
		MONO_ADD_INS (cfg->cbb, loada);

		call->inst.dreg = temp->dreg;

		call->vret_var = loada;
	} else if (!MONO_TYPE_IS_VOID (sig_ret))
		call->inst.dreg = alloc_dreg (cfg, (MonoStackType)call->inst.type);

#ifdef MONO_ARCH_SOFT_FLOAT_FALLBACK
	if (COMPILE_SOFT_FLOAT (cfg)) {
		/*
		 * If the call has a float argument, we would need to do an r8->r4 conversion using
		 * an icall, but that cannot be done during the call sequence since it would clobber
		 * the call registers + the stack. So we do it before emitting the call.
		 */
		for (int i = 0; i < sig->param_count + sig->hasthis; ++i) {
			MonoType *t;
			MonoInst *in = call->args [i];

			if (i >= sig->hasthis)
				t = sig->params [i - sig->hasthis];
			else
				t = mono_get_int_type ();
			t = mono_type_get_underlying_type (t);

			if (!m_type_is_byref (t) && t->type == MONO_TYPE_R4) {
				MonoInst *iargs [1];
				MonoInst *conv;

				iargs [0] = in;
				conv = mono_emit_jit_icall (cfg, mono_fload_r4_arg, iargs);

				/* The result will be in an int vreg */
				call->args [i] = conv;
			}
		}
	}
#endif

	call->need_unbox_trampoline = unbox_trampoline;

#ifdef ENABLE_LLVM
	if (COMPILE_LLVM (cfg))
		mono_llvm_emit_call (cfg, call);
	else
		mono_arch_emit_call (cfg, call);
#else
	mono_arch_emit_call (cfg, call);
#endif

	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;

	return call;
}

gboolean
mini_should_check_stack_pointer (MonoCompile *cfg)
{
	// This logic is shared by mini_emit_calli_full and is_supported_tailcall,
	// in order to compute tailcall_supported earlier. Alternatively it could be passed
	// out from mini_emit_calli_full -- if it has not been copied around
	// or decisions made based on it.

	WrapperInfo *info;

	return cfg->check_pinvoke_callconv &&
		cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE &&
		((info = mono_marshal_get_wrapper_info (cfg->method))) &&
		info->subtype == WRAPPER_SUBTYPE_PINVOKE;
}

static void
set_rgctx_arg (MonoCompile *cfg, MonoCallInst *call, int rgctx_reg, MonoInst *rgctx_arg)
{
	mono_call_inst_add_outarg_reg (cfg, call, rgctx_reg, MONO_ARCH_RGCTX_REG, FALSE);
	cfg->uses_rgctx_reg = TRUE;
	call->rgctx_reg = TRUE;
#ifdef ENABLE_LLVM
	call->rgctx_arg_reg = rgctx_reg;
#endif
}

/* Either METHOD or IMT_ARG needs to be set */
static void
emit_imt_argument (MonoCompile *cfg, MonoCallInst *call, MonoMethod *method, MonoInst *imt_arg)
{
	int method_reg;

	g_assert (method || imt_arg);

	if (COMPILE_LLVM (cfg)) {
		if (imt_arg) {
			method_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, method_reg, imt_arg->dreg);
		} else {
			MonoInst *ins = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_METHODCONST, method);
			method_reg = ins->dreg;
		}

#ifdef ENABLE_LLVM
		call->imt_arg_reg = method_reg;
#endif
		mono_call_inst_add_outarg_reg (cfg, call, method_reg, MONO_ARCH_IMT_REG, FALSE);
		return;
	}

	if (imt_arg) {
		method_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, method_reg, imt_arg->dreg);
	} else {
		MonoInst *ins = mini_emit_runtime_constant (cfg, MONO_PATCH_INFO_METHODCONST, method);
		method_reg = ins->dreg;
	}

	mono_call_inst_add_outarg_reg (cfg, call, method_reg, MONO_ARCH_IMT_REG, FALSE);
}

MonoInst*
mini_emit_calli_full (MonoCompile *cfg, MonoMethodSignature *sig, MonoInst **args, MonoInst *addr,
					  MonoInst *imt_arg, MonoInst *rgctx_arg, gboolean tailcall)
{
	MonoCallInst *call;
	MonoInst *ins;
	int rgctx_reg = -1;

	g_assert (!rgctx_arg || !imt_arg);

	if (rgctx_arg) {
		rgctx_reg = mono_alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, rgctx_reg, rgctx_arg->dreg);
	}

	const gboolean check_sp = mini_should_check_stack_pointer (cfg);

	// Checking stack pointer requires running code after a function call, prevents tailcall.
	// Caller needs to have decided that earlier.
	g_assert (!check_sp || !tailcall);

	if (check_sp) {
		if (!cfg->stack_imbalance_var)
			cfg->stack_imbalance_var = mono_compile_create_var (cfg, mono_get_int_type (), OP_LOCAL);

		MONO_INST_NEW (cfg, ins, OP_GET_SP);
		ins->dreg = cfg->stack_imbalance_var->dreg;
		MONO_ADD_INS (cfg->cbb, ins);
	}

	call = mini_emit_call_args (cfg, sig, args, TRUE, FALSE, tailcall, rgctx_arg ? TRUE : FALSE, FALSE, NULL);

	call->inst.sreg1 = addr->dreg;

	if (imt_arg)
		emit_imt_argument (cfg, call, NULL, imt_arg);

	MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

	if (check_sp) {
		int sp_reg;

		sp_reg = mono_alloc_preg (cfg);

		MONO_INST_NEW (cfg, ins, OP_GET_SP);
		ins->dreg = sp_reg;
		MONO_ADD_INS (cfg->cbb, ins);

		/* Restore the stack so we don't crash when throwing the exception */
		MONO_INST_NEW (cfg, ins, OP_SET_SP);
		ins->sreg1 = cfg->stack_imbalance_var->dreg;
		MONO_ADD_INS (cfg->cbb, ins);

		MONO_EMIT_NEW_BIALU (cfg, OP_COMPARE, -1, cfg->stack_imbalance_var->dreg, sp_reg);
		MONO_EMIT_NEW_COND_EXC (cfg, NE_UN, "ExecutionEngineException");
	}

	if (rgctx_arg)
		set_rgctx_arg (cfg, call, rgctx_reg, rgctx_arg);

	return (MonoInst*)call;
}

MonoInst*
mini_emit_calli (MonoCompile *cfg, MonoMethodSignature *sig, MonoInst **args, MonoInst *addr, MonoInst *imt_arg, MonoInst *rgctx_arg)
// Historical version without gboolean tailcall parameter.
{
	return mini_emit_calli_full (cfg, sig, args, addr, imt_arg, rgctx_arg, FALSE);
}

static int
callvirt_to_call (int opcode)
{
	switch (opcode) {
	case OP_TAILCALL_MEMBASE:
		return OP_TAILCALL;
	case OP_CALL_MEMBASE:
		return OP_CALL;
	case OP_VOIDCALL_MEMBASE:
		return OP_VOIDCALL;
	case OP_FCALL_MEMBASE:
		return OP_FCALL;
	case OP_RCALL_MEMBASE:
		return OP_RCALL;
	case OP_VCALL_MEMBASE:
		return OP_VCALL;
	case OP_XCALL_MEMBASE:
		return OP_XCALL;
	case OP_LCALL_MEMBASE:
		return OP_LCALL;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

static gboolean
can_enter_interp (MonoCompile *cfg, MonoMethod *method, gboolean virtual_)
{
	if (method->wrapper_type)
		return FALSE;

	if (m_class_get_image (method->klass) == m_class_get_image (cfg->method->klass)) {
		/* When using AOT profiling, the method might not be AOTed */
		if (cfg->compile_aot && mono_aot_can_enter_interp (method))
			return TRUE;
		/* Virtual calls from corlib can go outside corlib */
		if (!virtual_)
			return FALSE;
	}

	/* See needs_extra_arg () in mini-llvm.c */
	if (method->string_ctor)
		return FALSE;
	if (method->klass == mono_get_string_class () && (strstr (method->name, "memcpy") || strstr (method->name, "bzero")))
		return FALSE;

	/* Assume all calls outside the assembly can enter the interpreter */
	return TRUE;
}

MonoInst*
mini_emit_method_call_full (MonoCompile *cfg, MonoMethod *method, MonoMethodSignature *sig, gboolean tailcall,
							MonoInst **args, MonoInst *this_ins, MonoInst *imt_arg, MonoInst *rgctx_arg)
{
	gboolean virtual_ = this_ins != NULL;
	MonoCallInst *call;
	int rgctx_reg = 0;
	gboolean need_unbox_trampoline;

	if (!sig)
		sig = mono_method_signature_internal (method);

	if (rgctx_arg) {
		rgctx_reg = mono_alloc_preg (cfg);
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, rgctx_reg, rgctx_arg->dreg);
	}

	if (method->string_ctor) {
		/* Create the real signature */
		/* FIXME: Cache these */
		MonoMethodSignature *ctor_sig = mono_metadata_signature_dup_mempool (cfg->mempool, sig);
		ctor_sig->ret = m_class_get_byval_arg (mono_defaults.string_class);

		sig = ctor_sig;
	}

	mini_method_check_context_used (cfg, method);

	if (cfg->llvm_only && virtual_ && (method->flags & METHOD_ATTRIBUTE_VIRTUAL))
		return mini_emit_llvmonly_virtual_call (cfg, method, sig, 0, args);

	if (cfg->llvm_only && cfg->interp && !virtual_ && !tailcall && can_enter_interp (cfg, method, FALSE)) {
		MonoInst *ftndesc = mini_emit_get_rgctx_method (cfg, -1, method, MONO_RGCTX_INFO_METHOD_FTNDESC);

		/* Need wrappers for this signature to be able to enter interpreter */
		cfg->interp_in_signatures = g_slist_prepend_mempool (cfg->mempool, cfg->interp_in_signatures, sig);

		/* This call might need to enter the interpreter so make it indirect */
		return mini_emit_llvmonly_calli (cfg, sig, args, ftndesc);
	}

	need_unbox_trampoline = method->klass == mono_defaults.object_class || mono_class_is_interface (method->klass);

	call = mini_emit_call_args (cfg, sig, args, FALSE, virtual_, tailcall, rgctx_arg ? TRUE : FALSE, need_unbox_trampoline, method);
	call->method = method;
	call->inst.flags |= MONO_INST_HAS_METHOD;
	call->inst.inst_left = this_ins;

	// FIXME This has already been read in amd64 parameter construction.
	// Fixing it generates incorrect code. CEE_JMP needs attention.
	call->tailcall = tailcall;

	if (virtual_) {
		int vtable_reg, slot_reg, this_reg;
		int offset;

		this_reg = this_ins->dreg;

		if (!cfg->llvm_only && (m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class) && !strcmp (method->name, "Invoke")) {
			MonoInst *dummy_use;

			MONO_EMIT_NULL_CHECK (cfg, this_reg, FALSE);

			/* Make a call to delegate->invoke_impl */
			call->inst.inst_basereg = this_reg;
			call->inst.inst_offset = MONO_STRUCT_OFFSET (MonoDelegate, invoke_impl);
			MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

			/* We must emit a dummy use here because the delegate trampoline will
			replace the 'this' argument with the delegate target making this activation
			no longer a root for the delegate.
			This is an issue for delegates that target collectible code such as dynamic
			methods of GC'able assemblies.

			For a test case look into #667921.

			FIXME: a dummy use is not the best way to do it as the local register allocator
			will put it on a caller save register and spill it around the call.
			Ideally, we would either put it on a callee save register or only do the store part.
			 */
			EMIT_NEW_DUMMY_USE (cfg, dummy_use, args [0]);

			return (MonoInst*)call;
		}

		if ((!(method->flags & METHOD_ATTRIBUTE_VIRTUAL) ||
			 (MONO_METHOD_IS_FINAL (method)))) {
			/*
			 * the method is not virtual, we just need to ensure this is not null
			 * and then we can call the method directly.
			 */
			virtual_ = FALSE;
		} else if ((method->flags & METHOD_ATTRIBUTE_VIRTUAL) && MONO_METHOD_IS_FINAL (method)) {
			/*
			 * the method is virtual, but we can statically dispatch since either
			 * it's class or the method itself are sealed.
			 * But first we need to ensure it's not a null reference.
			 */
			virtual_ = FALSE;
		}

		if (!virtual_) {
			if (!method->string_ctor)
				MONO_EMIT_NEW_CHECK_THIS (cfg, this_reg);
		}

		if (!virtual_ && cfg->llvm_only && cfg->interp && !tailcall && can_enter_interp (cfg, method, FALSE)) {
			MonoInst *ftndesc = mini_emit_get_rgctx_method (cfg, -1, method, MONO_RGCTX_INFO_METHOD_FTNDESC);

			/* Need wrappers for this signature to be able to enter interpreter */
			cfg->interp_in_signatures = g_slist_prepend_mempool (cfg->mempool, cfg->interp_in_signatures, sig);

			/* This call might need to enter the interpreter so make it indirect */
			return mini_emit_llvmonly_calli (cfg, sig, args, ftndesc);
		} else if (!virtual_) {
			call->inst.opcode = GINT_TO_OPCODE (callvirt_to_call (call->inst.opcode));
		} else {
			vtable_reg = alloc_preg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_FAULT (cfg, vtable_reg, this_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
			if (mono_class_is_interface (method->klass)) {
				guint32 imt_slot = mono_method_get_imt_slot (method);
				emit_imt_argument (cfg, call, call->method, imt_arg);
				slot_reg = vtable_reg;
				offset = ((gint32)imt_slot - MONO_IMT_SIZE) * TARGET_SIZEOF_VOID_P;
			} else {
				slot_reg = vtable_reg;
				offset = MONO_STRUCT_OFFSET (MonoVTable, vtable) +
					((mono_method_get_vtable_index (method)) * (TARGET_SIZEOF_VOID_P));
				if (imt_arg) {
					g_assert (mono_method_signature_internal (method)->generic_param_count);
					emit_imt_argument (cfg, call, call->method, imt_arg);
				}
			}

			call->inst.sreg1 = slot_reg;
			call->inst.inst_offset = offset;
			call->is_virtual = TRUE;
		}
	}

	MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

	if (rgctx_arg)
		set_rgctx_arg (cfg, call, rgctx_reg, rgctx_arg);

	return (MonoInst*)call;
}

MonoInst*
mono_emit_method_call (MonoCompile *cfg, MonoMethod *method, MonoInst **args, MonoInst *this_ins)
{
	return mini_emit_method_call_full (cfg, method, mono_method_signature_internal (method), FALSE, args, this_ins, NULL, NULL);
}

static
MonoInst*
mono_emit_native_call (MonoCompile *cfg, gconstpointer func, MonoMethodSignature *sig,
					   MonoInst **args)
{
	MonoCallInst *call;

	g_assert (sig);

	call = mini_emit_call_args (cfg, sig, args, FALSE, FALSE, FALSE, FALSE, FALSE, NULL);
	call->fptr = func;

	MONO_ADD_INS (cfg->cbb, (MonoInst*)call);

	return (MonoInst*)call;
}

MonoInst*
mono_emit_jit_icall_id (MonoCompile *cfg, MonoJitICallId jit_icall_id, MonoInst **args)
{
	MonoJitICallInfo *info = mono_find_jit_icall_info (jit_icall_id);

	MonoCallInst *call = (MonoCallInst *)mono_emit_native_call (cfg, mono_icall_get_wrapper (info), info->sig, args);

	call->jit_icall_id = jit_icall_id;

	return (MonoInst*)call;
}

/*
 * mini_emit_abs_call:
 *
 *   Emit a call to the runtime function described by PATCH_TYPE and DATA.
 */
MonoInst*
mini_emit_abs_call (MonoCompile *cfg, MonoJumpInfoType patch_type, gconstpointer data,
					MonoMethodSignature *sig, MonoInst **args)
{
	MonoJumpInfo *ji = mono_patch_info_new (cfg->mempool, 0, patch_type, data);
	MonoInst *ins;

	/*
	 * We pass ji as the call address, the PATCH_INFO_ABS resolving code will
	 * handle it.
	 * FIXME: Is the abs_patches hashtable avoidable?
	 * Such as by putting the patch info in the call instruction?
	 */
	if (cfg->abs_patches == NULL)
		cfg->abs_patches = g_hash_table_new (NULL, NULL);
	g_hash_table_insert (cfg->abs_patches, ji, ji);
	ins = mono_emit_native_call (cfg, ji, sig, args);
	((MonoCallInst*)ins)->fptr_is_patch = TRUE;
	return ins;
}

MonoInst*
mini_emit_llvmonly_virtual_call (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, int context_used, MonoInst **sp)
{
	static MonoMethodSignature *helper_sig_llvmonly_imt_trampoline = NULL;
	MonoInst *icall_args [16];
	MonoInst *call_target, *ins, *vtable_ins;
	int this_reg, vtable_reg;
	gboolean is_iface = mono_class_is_interface (cmethod->klass);
	gboolean is_gsharedvt = cfg->gsharedvt && mini_is_gsharedvt_variable_signature (fsig);
	gboolean variant_iface = FALSE;
	guint32 slot;
	int offset;
	gboolean special_array_interface = m_class_is_array_special_interface (cmethod->klass);

	if (cfg->interp && can_enter_interp (cfg, cmethod, TRUE)) {
		/* Need wrappers for this signature to be able to enter interpreter */
		cfg->interp_in_signatures = g_slist_prepend_mempool (cfg->mempool, cfg->interp_in_signatures, fsig);

		if (m_class_is_delegate (cmethod->klass) && !strcmp (cmethod->name, "Invoke")) {
			/* To support dynamically generated code, add a signature for the actual method called by the delegate as well. */
			MonoMethodSignature *nothis_sig = mono_metadata_signature_dup_add_this (m_class_get_image (cmethod->klass), fsig, mono_get_object_class ());
			cfg->interp_in_signatures = g_slist_prepend_mempool (cfg->mempool, cfg->interp_in_signatures, nothis_sig);
		}
	}

	/*
	 * In llvm-only mode, vtables contain function descriptors instead of
	 * method addresses/trampolines.
	 */
	MONO_EMIT_NULL_CHECK (cfg, sp [0]->dreg, FALSE);

	if (is_iface)
		slot = mono_method_get_imt_slot (cmethod);
	else
		slot = mono_method_get_vtable_index (cmethod);

	this_reg = sp [0]->dreg;

	if (is_iface && mono_class_has_variant_generic_params (cmethod->klass))
		variant_iface = TRUE;

	if (!helper_sig_llvmonly_imt_trampoline) {
		MonoMethodSignature *tmp = mono_icall_sig_ptr_ptr_ptr;
		mono_memory_barrier ();
		helper_sig_llvmonly_imt_trampoline = tmp;
	}

	if (!cfg->gsharedvt && (m_class_get_parent (cmethod->klass) == mono_defaults.multicastdelegate_class) && !strcmp (cmethod->name, "Invoke")) {
		/* Delegate invokes */
		MONO_EMIT_NULL_CHECK (cfg, this_reg, FALSE);

		/* Make a call to delegate->invoke_impl */
		int invoke_reg = alloc_preg (cfg);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, invoke_reg, this_reg, MONO_STRUCT_OFFSET (MonoDelegate, invoke_impl));

		int addr_reg = alloc_preg (cfg);
		int arg_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, call_target, OP_LOAD_MEMBASE, addr_reg, invoke_reg, 0);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, arg_reg, invoke_reg, TARGET_SIZEOF_VOID_P);
		return mini_emit_extra_arg_calli (cfg, fsig, sp, arg_reg, call_target);
	}

	if (!is_gsharedvt && !fsig->generic_param_count && !is_iface) {
		/*
		 * The simplest case, a normal virtual call.
		 */
		int slot_reg = alloc_preg (cfg);
		int addr_reg = alloc_preg (cfg);
		int arg_reg = alloc_preg (cfg);
		MonoBasicBlock *non_null_bb;

		vtable_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, vtable_ins, OP_LOAD_MEMBASE, vtable_reg, this_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		offset = MONO_STRUCT_OFFSET (MonoVTable, vtable) + (slot * TARGET_SIZEOF_VOID_P);

		/* Load the vtable slot, which contains a function descriptor. */
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, slot_reg, vtable_reg, offset);

		NEW_BBLOCK (cfg, non_null_bb);

		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, slot_reg, 0);
		cfg->cbb->last_ins->flags |= MONO_INST_LIKELY;
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBNE_UN, non_null_bb);

		/* Slow path */
		// FIXME: Make the wrapper use the preserveall cconv
		// FIXME: Use one icall per slot for small slot numbers ?
		icall_args [0] = vtable_ins;
		EMIT_NEW_ICONST (cfg, icall_args [1], slot);
		/* Make the icall return the vtable slot value to save some code space */
		ins = mono_emit_jit_icall (cfg, mini_llvmonly_init_vtable_slot, icall_args);
		ins->dreg = slot_reg;
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, non_null_bb);

		/* Fastpath */
		MONO_START_BB (cfg, non_null_bb);
		/* Load the address + arg from the vtable slot */
		EMIT_NEW_LOAD_MEMBASE (cfg, call_target, OP_LOAD_MEMBASE, addr_reg, slot_reg, 0);
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, arg_reg, slot_reg, TARGET_SIZEOF_VOID_P);
		return mini_emit_extra_arg_calli (cfg, fsig, sp, arg_reg, call_target);
	}

	if (!is_gsharedvt && !fsig->generic_param_count && is_iface && !variant_iface && !special_array_interface) {
		/*
		 * A simple interface call
		 *
		 * We make a call through an imt slot to obtain the function descriptor we need to call.
		 * The imt slot contains a function descriptor for a runtime function + arg.
		 */
		int slot_reg = alloc_preg (cfg);
		int addr_reg = alloc_preg (cfg);
		int arg_reg = alloc_preg (cfg);
		MonoInst *thunk_addr_ins, *thunk_arg_ins, *ftndesc_ins;

		vtable_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, vtable_ins, OP_LOAD_MEMBASE, vtable_reg, this_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		offset = ((gint32)slot - MONO_IMT_SIZE) * TARGET_SIZEOF_VOID_P;

		/*
		 * The slot is already initialized when the vtable is created so there is no need
		 * to check it here.
		 */

		/* Load the imt slot, which contains a function descriptor. */
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, slot_reg, vtable_reg, offset);

		/* Load the address + arg of the imt thunk from the imt slot */
		EMIT_NEW_LOAD_MEMBASE (cfg, thunk_addr_ins, OP_LOAD_MEMBASE, addr_reg, slot_reg, 0);
		EMIT_NEW_LOAD_MEMBASE (cfg, thunk_arg_ins, OP_LOAD_MEMBASE, arg_reg, slot_reg, TARGET_SIZEOF_VOID_P);
		/*
		 * IMT thunks in llvm-only mode are C functions which take an info argument
		 * plus the imt method and return the ftndesc to call.
		 */
		icall_args [0] = thunk_arg_ins;
		icall_args [1] = mini_emit_get_rgctx_method (cfg, context_used,
												cmethod, MONO_RGCTX_INFO_METHOD);
		ftndesc_ins = mini_emit_calli (cfg, helper_sig_llvmonly_imt_trampoline, icall_args, thunk_addr_ins, NULL, NULL);
		return mini_emit_llvmonly_calli (cfg, fsig, sp, ftndesc_ins);
	}

	if (!is_gsharedvt && (fsig->generic_param_count || variant_iface || special_array_interface)) {
		/*
		 * This is similar to the interface case, the vtable slot points to an imt thunk which is
		 * dynamically extended as more instantiations are discovered.
		 * This handles generic virtual methods both on classes and interfaces.
		 */
		int slot_reg = alloc_preg (cfg);
		int addr_reg = alloc_preg (cfg);
		int arg_reg = alloc_preg (cfg);
		int ftndesc_reg = alloc_preg (cfg);
		MonoInst *thunk_addr_ins, *thunk_arg_ins, *ftndesc_ins;
		MonoBasicBlock *slowpath_bb, *end_bb;

		NEW_BBLOCK (cfg, slowpath_bb);
		NEW_BBLOCK (cfg, end_bb);

		vtable_reg = alloc_preg (cfg);
		EMIT_NEW_LOAD_MEMBASE (cfg, vtable_ins, OP_LOAD_MEMBASE, vtable_reg, this_reg, MONO_STRUCT_OFFSET (MonoObject, vtable));
		if (is_iface)
			offset = ((gint32)slot - MONO_IMT_SIZE) * TARGET_SIZEOF_VOID_P;
		else
			offset = MONO_STRUCT_OFFSET (MonoVTable, vtable) + (slot * TARGET_SIZEOF_VOID_P);

		/* Load the slot, which contains a function descriptor. */
		MONO_EMIT_NEW_LOAD_MEMBASE (cfg, slot_reg, vtable_reg, offset);

		/* These slots are not initialized, so fall back to the slow path until they are initialized */
		/* That happens when mono_method_add_generic_virtual_invocation () creates an IMT thunk */
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, slot_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, slowpath_bb);

		/* Fastpath */
		/* Same as with iface calls */
		EMIT_NEW_LOAD_MEMBASE (cfg, thunk_addr_ins, OP_LOAD_MEMBASE, addr_reg, slot_reg, 0);
		EMIT_NEW_LOAD_MEMBASE (cfg, thunk_arg_ins, OP_LOAD_MEMBASE, arg_reg, slot_reg, TARGET_SIZEOF_VOID_P);
		icall_args [0] = thunk_arg_ins;
		icall_args [1] = mini_emit_get_rgctx_method (cfg, context_used,
												cmethod, MONO_RGCTX_INFO_METHOD);
		ftndesc_ins = mini_emit_calli (cfg, helper_sig_llvmonly_imt_trampoline, icall_args, thunk_addr_ins, NULL, NULL);
		ftndesc_ins->dreg = ftndesc_reg;
		/*
		 * Unlike normal iface calls, these imt thunks can return NULL, i.e. when they are passed an instantiation
		 * they don't know about yet. Fall back to the slowpath in that case.
		 */
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_COMPARE_IMM, -1, ftndesc_reg, 0);
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_PBEQ, slowpath_bb);

		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

		/* Slowpath */
		MONO_START_BB (cfg, slowpath_bb);
		icall_args [0] = vtable_ins;
		EMIT_NEW_ICONST (cfg, icall_args [1], slot);
		icall_args [2] = mini_emit_get_rgctx_method (cfg, context_used,
													 cmethod, MONO_RGCTX_INFO_METHOD);
		if (is_iface)
			ftndesc_ins = mono_emit_jit_icall (cfg, mini_llvmonly_resolve_generic_virtual_iface_call, icall_args);
		else
			ftndesc_ins = mono_emit_jit_icall (cfg, mini_llvmonly_resolve_generic_virtual_call, icall_args);
		ftndesc_ins->dreg = ftndesc_reg;
		MONO_EMIT_NEW_BRANCH_BLOCK (cfg, OP_BR, end_bb);

		/* Common case */
		MONO_START_BB (cfg, end_bb);
		return mini_emit_llvmonly_calli (cfg, fsig, sp, ftndesc_ins);
	}

	if (is_gsharedvt && !(is_iface || fsig->generic_param_count || variant_iface || special_array_interface)) {
		MonoInst *ftndesc_ins;

		/* Normal virtual call using a gsharedvt calling conv */
		icall_args [0] = sp [0];
		EMIT_NEW_ICONST (cfg, icall_args [1], slot);

		ftndesc_ins = mono_emit_jit_icall (cfg, mini_llvmonly_resolve_vcall_gsharedvt_fast, icall_args);

		return mini_emit_llvmonly_calli (cfg, fsig, sp, ftndesc_ins);
	}

	/*
	 * Non-optimized cases
	 */
	icall_args [0] = sp [0];
	EMIT_NEW_ICONST (cfg, icall_args [1], slot);

	icall_args [2] = mini_emit_get_rgctx_method (cfg, context_used,
												 cmethod, MONO_RGCTX_INFO_METHOD);

	int arg_reg = alloc_preg (cfg);
	MONO_EMIT_NEW_PCONST (cfg, arg_reg, NULL);
	EMIT_NEW_VARLOADA_VREG (cfg, icall_args [3], arg_reg, mono_get_int_type ());

	g_assert (is_gsharedvt);
	if (is_iface)
		call_target = mono_emit_jit_icall (cfg, mini_llvmonly_resolve_iface_call_gsharedvt, icall_args);
	else
		call_target = mono_emit_jit_icall (cfg, mini_llvmonly_resolve_vcall_gsharedvt, icall_args);

	/*
	 * Pass the extra argument even if the callee doesn't receive it, most
	 * calling conventions allow this.
	 */
	return mini_emit_extra_arg_calli (cfg, fsig, sp, arg_reg, call_target);
}

static MonoMethodSignature*
sig_to_rgctx_sig (MonoMethodSignature *sig)
{
	// FIXME: memory allocation
	MonoMethodSignature *res;
	int i;

	res = (MonoMethodSignature *)g_malloc (MONO_SIZEOF_METHOD_SIGNATURE + (sig->param_count + 1) * sizeof (MonoType*));
	memcpy (res, sig, MONO_SIZEOF_METHOD_SIGNATURE);
	res->param_count = sig->param_count + 1;
	for (i = 0; i < sig->param_count; ++i)
		res->params [i] = sig->params [i];
	res->params [sig->param_count] = mono_class_get_byref_type (mono_defaults.int_class);
	return res;
}

/* Make an indirect call to FSIG passing an additional argument */
MonoInst*
mini_emit_extra_arg_calli (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **orig_args, int arg_reg, MonoInst *call_target)
{
	MonoMethodSignature *csig;
	MonoInst *args_buf [16];
	MonoInst **args;
	int i, pindex, tmp_reg;

	/* Make a call with an rgctx/extra arg */
	if (fsig->param_count + 2 < 16)
		args = args_buf;
	else
		args = (MonoInst **)mono_mempool_alloc0 (cfg->mempool, sizeof (MonoInst*) * (fsig->param_count + 2));
	pindex = 0;
	if (fsig->hasthis)
		args [pindex ++] = orig_args [0];
	for (i = 0; i < fsig->param_count; ++i)
		args [pindex ++] = orig_args [fsig->hasthis + i];
	tmp_reg = alloc_preg (cfg);
	EMIT_NEW_UNALU (cfg, args [pindex], OP_MOVE, tmp_reg, arg_reg);
	csig = sig_to_rgctx_sig (fsig);
	return mini_emit_calli (cfg, csig, args, call_target, NULL, NULL);
}

/* Emit an indirect call to the function descriptor ADDR */
MonoInst*
mini_emit_llvmonly_calli (MonoCompile *cfg, MonoMethodSignature *fsig, MonoInst **args, MonoInst *addr)
// FIXME no tailcall support
{
	int addr_reg, arg_reg;
	MonoInst *call_target;

	g_assert (cfg->llvm_only);

	/*
	 * addr points to a <addr, arg> pair, load both of them, and
	 * make a call to addr, passing arg as an extra arg.
	 */
	addr_reg = alloc_preg (cfg);
	EMIT_NEW_LOAD_MEMBASE (cfg, call_target, OP_LOAD_MEMBASE, addr_reg, addr->dreg, 0);
	arg_reg = alloc_preg (cfg);
	MONO_EMIT_NEW_LOAD_MEMBASE (cfg, arg_reg, addr->dreg, TARGET_SIZEOF_VOID_P);

	return mini_emit_extra_arg_calli (cfg, fsig, args, arg_reg, call_target);
}
#else
MONO_EMPTY_SOURCE_FILE (calls);
#endif
