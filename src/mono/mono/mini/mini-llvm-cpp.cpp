//
// mini-llvm-cpp.cpp: C++ support classes for the mono LLVM integration
//
// (C) 2009 Novell, Inc.
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

#include <stdint.h>

#include <llvm/Support/raw_ostream.h>
#include <llvm/PassManager.h>
#include <llvm/ExecutionEngine/ExecutionEngine.h>
#include <llvm/ExecutionEngine/JITMemoryManager.h>
#include <llvm/ExecutionEngine/JITEventListener.h>
#include <llvm/Target/TargetOptions.h>
#include <llvm/Target/TargetData.h>
#include <llvm/Target/TargetRegisterInfo.h>
#include <llvm/Analysis/Verifier.h>
#include <llvm/Transforms/Scalar.h>
#include <llvm/Support/CommandLine.h>
#include "llvm/Support/PassNameParser.h"
#include "llvm/Support/PrettyStackTrace.h"
#include <llvm/CodeGen/Passes.h>
#include <llvm/CodeGen/MachineFunctionPass.h>
#include <llvm/CodeGen/MachineFunction.h>
#include <llvm/CodeGen/MachineFrameInfo.h>
#include <llvm/CodeGen/MonoMachineFunctionInfo.h>
//#include <llvm/LinkAllPasses.h>

#include "llvm-c/Core.h"
#include "llvm-c/ExecutionEngine.h"

#include "mini-llvm-cpp.h"

extern "C" void LLVMInitializeX86TargetInfo();

using namespace llvm;

class MonoJITMemoryManager : public JITMemoryManager
{
private:
	JITMemoryManager *mm;

public:
	/* Callbacks installed by mono */
	AllocCodeMemoryCb *alloc_cb;

	MonoJITMemoryManager ();
	~MonoJITMemoryManager ();

	void setMemoryWritable (void);

	void setMemoryExecutable (void);

	void AllocateGOT();

    unsigned char *getGOTBase() const {
		return mm->getGOTBase ();
    }

#if LLVM_MAJOR_VERSION == 2 && LLVM_MINOR_VERSION < 7
    void *getDlsymTable() const {
		return mm->getDlsymTable ();
    }

	void SetDlsymTable(void *ptr);
#endif

	void setPoisonMemory(bool) {
	}

	unsigned char *startFunctionBody(const Function *F, 
									 uintptr_t &ActualSize);
  
	unsigned char *allocateStub(const GlobalValue* F, unsigned StubSize,
								 unsigned Alignment);
  
	void endFunctionBody(const Function *F, unsigned char *FunctionStart,
						 unsigned char *FunctionEnd);

	unsigned char *allocateSpace(intptr_t Size, unsigned Alignment);

	uint8_t *allocateGlobal(uintptr_t Size, unsigned Alignment);
  
	void deallocateMemForFunction(const Function *F);
  
	unsigned char*startExceptionTable(const Function* F,
									  uintptr_t &ActualSize);
  
	void endExceptionTable(const Function *F, unsigned char *TableStart,
						   unsigned char *TableEnd, 
						   unsigned char* FrameRegister);

#if LLVM_MAJOR_VERSION == 2 && LLVM_MINOR_VERSION >= 7
	virtual void deallocateFunctionBody(void*) {
	}

	virtual void deallocateExceptionTable(void*) {
	}
#endif
};

MonoJITMemoryManager::MonoJITMemoryManager ()
{
	SizeRequired = true;
	mm = JITMemoryManager::CreateDefaultMemManager ();
}

MonoJITMemoryManager::~MonoJITMemoryManager ()
{
}

void
MonoJITMemoryManager::setMemoryWritable (void)
{
}

void
MonoJITMemoryManager::setMemoryExecutable (void)
{
}

void
MonoJITMemoryManager::AllocateGOT()
{
	mm->AllocateGOT ();
}

#if LLVM_MAJOR_VERSION == 2 && LLVM_MINOR_VERSION < 7  
void
MonoJITMemoryManager::SetDlsymTable(void *ptr)
{
	mm->SetDlsymTable (ptr);
}
#endif

unsigned char *
MonoJITMemoryManager::startFunctionBody(const Function *F, 
					uintptr_t &ActualSize)
{
	return alloc_cb (wrap (F), ActualSize);
}
  
unsigned char *
MonoJITMemoryManager::allocateStub(const GlobalValue* F, unsigned StubSize,
			   unsigned Alignment)
{
	return alloc_cb (wrap (F), StubSize);
}
  
void
MonoJITMemoryManager::endFunctionBody(const Function *F, unsigned char *FunctionStart,
				  unsigned char *FunctionEnd)
{
}

unsigned char *
MonoJITMemoryManager::allocateSpace(intptr_t Size, unsigned Alignment)
{
	return new unsigned char [Size];
}

uint8_t *
MonoJITMemoryManager::allocateGlobal(uintptr_t Size, unsigned Alignment)
{
	return new unsigned char [Size];
}

void
MonoJITMemoryManager::deallocateMemForFunction(const Function *F)
{
}
  
unsigned char*
MonoJITMemoryManager::startExceptionTable(const Function* F,
					  uintptr_t &ActualSize)
{
	return alloc_cb (wrap (F), ActualSize);
}
  
void
MonoJITMemoryManager::endExceptionTable(const Function *F, unsigned char *TableStart,
					unsigned char *TableEnd, 
					unsigned char* FrameRegister)
{
}

static MonoJITMemoryManager *mono_mm;

static FunctionPassManager *fpm;

void
mono_llvm_optimize_method (LLVMValueRef method)
{
	verifyFunction (*(unwrap<Function> (method)));
	fpm->run (*unwrap<Function> (method));
}

void
mono_llvm_dump_value (LLVMValueRef value)
{
	/* Same as LLVMDumpValue (), but print to stdout */
	outs () << (*unwrap<Value> (value));
}

/* Missing overload for building an alloca with an alignment */
LLVMValueRef
mono_llvm_build_alloca (LLVMBuilderRef builder, LLVMTypeRef Ty, 
						LLVMValueRef ArraySize,
						int alignment, const char *Name)
{
	return wrap (unwrap (builder)->Insert (new AllocaInst (unwrap (Ty), unwrap (ArraySize), alignment), Name));
}

LLVMValueRef 
mono_llvm_build_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
					  const char *Name, gboolean is_volatile)
{
	return wrap(unwrap(builder)->CreateLoad(unwrap(PointerVal), is_volatile, Name));
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
					  gboolean is_volatile)
{
	return wrap(unwrap(builder)->CreateStore(unwrap(Val), unwrap(PointerVal), is_volatile));
}

void
mono_llvm_replace_uses_of (LLVMValueRef var, LLVMValueRef v)
{
	Value *V = ConstantExpr::getTruncOrBitCast (unwrap<Constant> (v), unwrap (var)->getType ());
	unwrap (var)->replaceAllUsesWith (V);
}

static cl::list<const PassInfo*, bool, PassNameParser>
PassList(cl::desc("Optimizations available:"));

class MonoJITEventListener : public JITEventListener {

public:
	FunctionEmittedCb *emitted_cb;

	MonoJITEventListener (FunctionEmittedCb *cb) {
		emitted_cb = cb;
	}

	virtual void NotifyFunctionEmitted(const Function &F,
									   void *Code, size_t Size,
									   const EmittedFunctionDetails &Details) {
		/*
		 * X86TargetMachine::setCodeModelForJIT() sets the code model to Large on amd64,
		 * which means the JIT will generate calls of the form
		 * mov reg, <imm>
		 * call *reg
		 * Our trampoline code can't patch this. Passing CodeModel::Small to createJIT
		 * doesn't seem to work, we need Default. A discussion is here:
		 * http://lists.cs.uiuc.edu/pipermail/llvmdev/2009-December/027999.html
		 * There seems to no way to get the TargeMachine used by an EE either, so we
		 * install a profiler hook and reset the code model here.
		 * This should be inside an ifdef, but we can't include our config.h either,
		 * since its definitions conflict with LLVM's config.h.
		 *
		 */
		//#if defined(TARGET_X86) || defined(TARGET_AMD64)
#ifndef LLVM_MONO_BRANCH
		/* The LLVM mono branch contains a workaround, so this is not needed */
		if (Details.MF->getTarget ().getCodeModel () == CodeModel::Large) {
			Details.MF->getTarget ().setCodeModel (CodeModel::Default);
		}
#endif
		//#endif

		emitted_cb (wrap (&F), Code, (char*)Code + Size);
	}
};

LLVMExecutionEngineRef
mono_llvm_create_ee (LLVMModuleProviderRef MP, AllocCodeMemoryCb *alloc_cb, FunctionEmittedCb *emitted_cb, ExceptionTableCb *exception_cb)
{
  std::string Error;

  LLVMInitializeX86Target ();
  LLVMInitializeX86TargetInfo ();

  llvm::cl::ParseEnvironmentOptions("mono", "MONO_LLVM", "", false);

  mono_mm = new MonoJITMemoryManager ();
  mono_mm->alloc_cb = alloc_cb;

#if LLVM_MAJOR_VERSION == 2 && LLVM_MINOR_VERSION < 8
   DwarfExceptionHandling = true;
#else
   JITExceptionHandling = true;
#endif
  // PrettyStackTrace installs signal handlers which trip up libgc
  DisablePrettyStackTrace = true;

  ExecutionEngine *EE = ExecutionEngine::createJIT (unwrap (MP), &Error, mono_mm, CodeGenOpt::Default);
  if (!EE) {
	  errs () << "Unable to create LLVM ExecutionEngine: " << Error << "\n";
	  g_assert_not_reached ();
  }
  EE->InstallExceptionTableRegister (exception_cb);
  EE->RegisterJITEventListener (new MonoJITEventListener (emitted_cb));

  fpm = new FunctionPassManager (unwrap (MP));

  fpm->add(new TargetData(*EE->getTargetData()));
  /* Add a random set of passes */
  /* Make this run-time configurable */
  fpm->add(createInstructionCombiningPass());
  fpm->add(createReassociatePass());
  fpm->add(createGVNPass());
  fpm->add(createCFGSimplificationPass());

  /* Add passes specified by the env variable */
  /* FIXME: This can only add passes which are linked in, thus are already used */
  for (unsigned i = 0; i < PassList.size(); ++i) {
      const PassInfo *PassInf = PassList[i];
      Pass *P = 0;

      if (PassInf->getNormalCtor())
		  P = PassInf->getNormalCtor()();
	  fpm->add (P);
  }

  return wrap(EE);
}

void
mono_llvm_dispose_ee (LLVMExecutionEngineRef ee)
{
	delete unwrap (ee);

	delete fpm;
}
