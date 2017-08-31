//
// mini-llvm-cpp.cpp: C++ support classes for the mono LLVM integration
//
// (C) 2009-2011 Novell, Inc.
// Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//
// We need to override some stuff in LLVM, but this cannot be done using the C
// interface, so we have to use some C++ code here.
// The things which we override are:
// - the default JIT code manager used by LLVM doesn't allocate memory using
//   MAP_32BIT, we require it.
// - add some callbacks so we can obtain the size of methods and their exception
//   tables.
//

//
// Mono's internal header files are not C++ clean, so avoid including them if 
// possible
//

#include "config.h"

#include <stdint.h>

#include <llvm/Support/raw_ostream.h>
#include <llvm/IR/Function.h>
#include <llvm/IR/IRBuilder.h>
#include <llvm/IR/Module.h>
#include <llvm/IR/DIBuilder.h>
#include <llvm/IR/CallSite.h>

#include "mini-llvm-cpp.h"

using namespace llvm;

#if LLVM_API_VERSION > 100
// These are c++11 scoped enums in recent llvm versions
#define Acquire AtomicOrdering::Acquire
#define Release AtomicOrdering::Release
#define SequentiallyConsistent AtomicOrdering::SequentiallyConsistent
#endif

void
mono_llvm_dump_value (LLVMValueRef value)
{
	/* Same as LLVMDumpValue (), but print to stdout */
	fflush (stdout);
	outs () << (*unwrap<Value> (value));
}

/* Missing overload for building an alloca with an alignment */
LLVMValueRef
mono_llvm_build_alloca (LLVMBuilderRef builder, LLVMTypeRef Ty, 
						LLVMValueRef ArraySize,
						int alignment, const char *Name)
{
#if LLVM_API_VERSION >= 500
	return wrap (unwrap (builder)->Insert (new AllocaInst (unwrap (Ty), 0, unwrap (ArraySize), alignment), Name));
#else
	return wrap (unwrap (builder)->Insert (new AllocaInst (unwrap (Ty), unwrap (ArraySize), alignment), Name));
#endif
}

LLVMValueRef 
mono_llvm_build_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
					  const char *Name, gboolean is_volatile)
{
	LoadInst *ins = unwrap(builder)->CreateLoad(unwrap(PointerVal), is_volatile, Name);

	return wrap(ins);
}

LLVMValueRef
mono_llvm_build_atomic_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
							 const char *Name, gboolean is_volatile, int alignment, BarrierKind barrier)
{
	LoadInst *ins = unwrap(builder)->CreateLoad(unwrap(PointerVal), is_volatile, Name);

	ins->setAlignment (alignment);
	switch (barrier) {
	case LLVM_BARRIER_NONE:
		break;
	case LLVM_BARRIER_ACQ:
		ins->setOrdering(Acquire);
		break;
	case LLVM_BARRIER_SEQ:
		ins->setOrdering(SequentiallyConsistent);
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	return wrap(ins);
}

LLVMValueRef 
mono_llvm_build_aligned_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
							  const char *Name, gboolean is_volatile, int alignment)
{
	LoadInst *ins;

	ins = unwrap(builder)->CreateLoad(unwrap(PointerVal), is_volatile, Name);
	ins->setAlignment (alignment);

	return wrap(ins);
}

LLVMValueRef 
mono_llvm_build_store (LLVMBuilderRef builder, LLVMValueRef Val, LLVMValueRef PointerVal,
					  gboolean is_volatile, BarrierKind barrier)
{
	StoreInst *ins = unwrap(builder)->CreateStore(unwrap(Val), unwrap(PointerVal), is_volatile);

	switch (barrier) {
	case LLVM_BARRIER_NONE:
		break;
	case LLVM_BARRIER_REL:
		ins->setOrdering(Release);
		break;
	case LLVM_BARRIER_SEQ:
		ins->setOrdering(SequentiallyConsistent);
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	return wrap(ins);
}

LLVMValueRef 
mono_llvm_build_aligned_store (LLVMBuilderRef builder, LLVMValueRef Val, LLVMValueRef PointerVal,
							   gboolean is_volatile, int alignment)
{
	StoreInst *ins;

	ins = unwrap(builder)->CreateStore(unwrap(Val), unwrap(PointerVal), is_volatile);
	ins->setAlignment (alignment);

	return wrap (ins);
}

LLVMValueRef
mono_llvm_build_cmpxchg (LLVMBuilderRef builder, LLVMValueRef ptr, LLVMValueRef cmp, LLVMValueRef val)
{
	AtomicCmpXchgInst *ins;

	ins = unwrap(builder)->CreateAtomicCmpXchg (unwrap(ptr), unwrap (cmp), unwrap (val), SequentiallyConsistent, SequentiallyConsistent);
	return wrap (ins);
}

LLVMValueRef
mono_llvm_build_atomic_rmw (LLVMBuilderRef builder, AtomicRMWOp op, LLVMValueRef ptr, LLVMValueRef val)
{
	AtomicRMWInst::BinOp aop = AtomicRMWInst::Xchg;
	AtomicRMWInst *ins;

	switch (op) {
	case LLVM_ATOMICRMW_OP_XCHG:
		aop = AtomicRMWInst::Xchg;
		break;
	case LLVM_ATOMICRMW_OP_ADD:
		aop = AtomicRMWInst::Add;
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	ins = unwrap (builder)->CreateAtomicRMW (aop, unwrap (ptr), unwrap (val), SequentiallyConsistent);
	return wrap (ins);
}

LLVMValueRef
mono_llvm_build_fence (LLVMBuilderRef builder, BarrierKind kind)
{
	FenceInst *ins;
	AtomicOrdering ordering;

	g_assert (kind != LLVM_BARRIER_NONE);

	switch (kind) {
	case LLVM_BARRIER_ACQ:
		ordering = Acquire;
		break;
	case LLVM_BARRIER_REL:
		ordering = Release;
		break;
	case LLVM_BARRIER_SEQ:
		ordering = SequentiallyConsistent;
		break;
	default:
		g_assert_not_reached ();
		break;
	}

	ins = unwrap (builder)->CreateFence (ordering);
	return wrap (ins);
}

void
mono_llvm_set_must_tail (LLVMValueRef call_ins)
{
	CallInst *ins = (CallInst*)unwrap (call_ins);

	ins->setTailCallKind (CallInst::TailCallKind::TCK_MustTail);
}

void
mono_llvm_replace_uses_of (LLVMValueRef var, LLVMValueRef v)
{
	Value *V = ConstantExpr::getTruncOrBitCast (unwrap<Constant> (v), unwrap (var)->getType ());
	unwrap (var)->replaceAllUsesWith (V);
}

LLVMValueRef
mono_llvm_create_constant_data_array (const uint8_t *data, int len)
{
	return wrap(ConstantDataArray::get (*unwrap(LLVMGetGlobalContext ()), makeArrayRef(data, len)));
}

void
mono_llvm_set_is_constant (LLVMValueRef global_var)
{
	unwrap<GlobalVariable>(global_var)->setConstant (true);
}

void
mono_llvm_set_preserveall_cc (LLVMValueRef func)
{
	unwrap<Function>(func)->setCallingConv (CallingConv::PreserveAll);
}

void
mono_llvm_set_call_preserveall_cc (LLVMValueRef func)
{
	unwrap<CallInst>(func)->setCallingConv (CallingConv::PreserveAll);
}

void
mono_llvm_set_call_notail (LLVMValueRef func)
{
#if LLVM_API_VERSION > 100
	unwrap<CallInst>(func)->setTailCallKind (CallInst::TailCallKind::TCK_NoTail);
#endif
}

#if LLVM_API_VERSION > 500
static Attribute::AttrKind
convert_attr (AttrKind kind)
{
	switch (kind) {
	case LLVM_ATTR_NO_UNWIND:
		return Attribute::NoUnwind;
	case LLVM_ATTR_NO_INLINE:
		return Attribute::NoInline;
	case LLVM_ATTR_OPTIMIZE_FOR_SIZE:
		return Attribute::OptimizeForSize;
	case LLVM_ATTR_IN_REG:
		return Attribute::InReg;
	case LLVM_ATTR_STRUCT_RET:
		return Attribute::StructRet;
	case LLVM_ATTR_NO_ALIAS:
		return Attribute::NoAlias;
	case LLVM_ATTR_BY_VAL:
		return Attribute::ByVal;
	case LLVM_ATTR_UW_TABLE:
		return Attribute::UWTable;
	default:
		assert (0);
		return Attribute::NoUnwind;
	}
}
#else
static LLVMAttribute
convert_attr (AttrKind kind)
{
	switch (kind) {
	case LLVM_ATTR_NO_UNWIND:
		return LLVMNoUnwindAttribute;
	case LLVM_ATTR_NO_INLINE:
		return LLVMNoInlineAttribute;
	case LLVM_ATTR_OPTIMIZE_FOR_SIZE:
		return LLVMOptimizeForSizeAttribute;
	case LLVM_ATTR_IN_REG:
		return LLVMInRegAttribute;
	case LLVM_ATTR_STRUCT_RET:
		return LLVMStructRetAttribute;
	case LLVM_ATTR_NO_ALIAS:
		return LLVMNoAliasAttribute;
	case LLVM_ATTR_BY_VAL:
		return LLVMByValAttribute;
	case LLVM_ATTR_UW_TABLE:
		return LLVMUWTable;
	default:
		assert (0);
		return LLVMNoUnwindAttribute;
	}
}
#endif

void
mono_llvm_add_func_attr (LLVMValueRef func, AttrKind kind)
{
#if LLVM_API_VERSION > 100
	unwrap<Function> (func)->addAttribute (AttributeList::FunctionIndex, convert_attr (kind));
#else
	Function *Func = unwrap<Function>(func);
	const AttributeSet PAL = Func->getAttributes();
	AttrBuilder B(convert_attr (kind));
	const AttributeSet PALnew =
		PAL.addAttributes(Func->getContext(), AttributeSet::FunctionIndex,
						  AttributeSet::get(Func->getContext(),
											AttributeSet::FunctionIndex, B));
	Func->setAttributes(PALnew);
#endif
}

void
mono_llvm_add_param_attr (LLVMValueRef param, AttrKind kind)
{
#if LLVM_API_VERSION > 100
	Function *func = unwrap<Argument> (param)->getParent ();
	int n = unwrap<Argument> (param)->getArgNo ();
	func->addParamAttr (n, convert_attr (kind));
#else
	Argument *A = unwrap<Argument>(param);
	AttrBuilder B(convert_attr (kind));
	A->addAttr(AttributeSet::get(A->getContext(), A->getArgNo() + 1,  B));
#endif
}

void
mono_llvm_add_instr_attr (LLVMValueRef val, int index, AttrKind kind)
{
#if LLVM_API_VERSION > 100
	CallSite (unwrap<Instruction> (val)).addAttribute (index, convert_attr (kind));
#else
  CallSite Call = CallSite(unwrap<Instruction>(val));
  AttrBuilder B(convert_attr (kind));
  Call.setAttributes(
    Call.getAttributes().addAttributes(Call->getContext(), index,
                                       AttributeSet::get(Call->getContext(),
                                                         index, B)));
#endif
}

#if LLVM_API_VERSION > 100

void*
mono_llvm_create_di_builder (LLVMModuleRef module)
{
	return new DIBuilder (*unwrap(module));
}

void*
mono_llvm_di_create_compile_unit (void *di_builder, const char *cu_name, const char *dir, const char *producer)
{
	DIBuilder *builder = (DIBuilder*)di_builder;

#if LLVM_API_VERSION >= 500
	DIFile *di_file;

	di_file = builder->createFile (cu_name, dir);
	return builder->createCompileUnit (dwarf::DW_LANG_C99, di_file, producer, true, "", 0);
#else
	return builder->createCompileUnit (dwarf::DW_LANG_C99, cu_name, dir, producer, true, "", 0);
#endif
}

void*
mono_llvm_di_create_function (void *di_builder, void *cu, LLVMValueRef func, const char *name, const char *mangled_name, const char *dir, const char *file, int line)
{
	DIBuilder *builder = (DIBuilder*)di_builder;
	DIFile *di_file;
	DISubroutineType *type;
	DISubprogram *di_func;

	// FIXME: Share DIFile
	di_file = builder->createFile (file, dir);
	type = builder->createSubroutineType (builder->getOrCreateTypeArray (ArrayRef<Metadata*> ()));
	di_func = builder->createFunction (di_file, name, mangled_name, di_file, line, type, true, true, 0);

	unwrap<Function>(func)->setMetadata ("dbg", di_func);

	return di_func;
}

void*
mono_llvm_di_create_file (void *di_builder, const char *dir, const char *file)
{
	DIBuilder *builder = (DIBuilder*)di_builder;

	return builder->createFile (file, dir);
}

void*
mono_llvm_di_create_location (void *di_builder, void *scope, int row, int column)
{
	return DILocation::get (*unwrap(LLVMGetGlobalContext ()), row, column, (Metadata*)scope);
}

void
mono_llvm_di_set_location (LLVMBuilderRef builder, void *loc_md)
{
	unwrap(builder)->SetCurrentDebugLocation ((DILocation*)loc_md);
}

void
mono_llvm_di_builder_finalize (void *di_builder)
{
	DIBuilder *builder = (DIBuilder*)di_builder;

	builder->finalize ();
}

#endif /* #if LLVM_API_VERSION > 100 */
