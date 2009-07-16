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

#include <llvm/PassManager.h>
#include <llvm/ExecutionEngine/ExecutionEngine.h>
#include <llvm/ExecutionEngine/JITMemoryManager.h>
#include <llvm/Target/TargetOptions.h>
#include <llvm/Target/TargetData.h>
#include <llvm/Analysis/Verifier.h>
#include <llvm/Transforms/Scalar.h>
#include <llvm/Support/CommandLine.h>
#include "llvm/Support/PassNameParser.h"
#include "llvm/Support/PrettyStackTrace.h"
#include <llvm/CodeGen/Passes.h>
#include <llvm/CodeGen/MachineFunctionPass.h>
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
	FunctionEmittedCb *emitted_cb;

	MonoJITMemoryManager ();
	~MonoJITMemoryManager ();

	void setMemoryWritable (void);

	void setMemoryExecutable (void);

	void AllocateGOT();

    unsigned char *getGOTBase() const {
		return mm->getGOTBase ();
    }
    
    void *getDlsymTable() const {
		return mm->getDlsymTable ();
    }

	void setPoisonMemory(bool) {
	}
      
	void SetDlsymTable(void *ptr);
  
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
  
void
MonoJITMemoryManager::SetDlsymTable(void *ptr)
{
	mm->SetDlsymTable (ptr);
}
  
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
	emitted_cb (wrap (F), FunctionStart, FunctionEnd);
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

static LLVMContext ctx;

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
	cout << (*unwrap<Value> (value));
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
mono_llvm_build_volatile_load (LLVMBuilderRef builder, LLVMValueRef PointerVal,
							   const char *Name)
{
	return wrap(unwrap(builder)->CreateLoad(unwrap(PointerVal), true, Name));
}

static cl::list<const PassInfo*, bool, PassNameParser>
PassList(cl::desc("Optimizations available:"));

LLVMExecutionEngineRef
mono_llvm_create_ee (LLVMModuleProviderRef MP, AllocCodeMemoryCb *alloc_cb, FunctionEmittedCb *emitted_cb, ExceptionTableCb *exception_cb)
{
  std::string Error;

  LLVMInitializeX86Target ();
  LLVMInitializeX86TargetInfo ();

  llvm::cl::ParseEnvironmentOptions("mono", "MONO_LLVM", "", false);

  mono_mm = new MonoJITMemoryManager ();
  mono_mm->alloc_cb = alloc_cb;
  mono_mm->emitted_cb = emitted_cb;

  ExceptionHandling = true;
  // PrettyStackTrace installs signal handlers which trip up libgc
  DisablePrettyStackTrace = true;

  ExecutionEngine *EE = ExecutionEngine::createJIT (unwrap (MP), &Error, mono_mm, CodeGenOpt::Default);
  if (!EE) {
	  cerr << "Unable to create LLVM ExecutionEngine: " << Error << "\n";
	  g_assert_not_reached ();
  }
  EE->InstallExceptionTableRegister (exception_cb);

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
	  if (dynamic_cast<MachineFunctionPass*>(P) != 0) {
		  cerr << PassInf->getPassName () << " is a machine function pass.\n";
	  } else {
		  fpm->add (P);
	  }
  }

  return wrap(EE);
}

void
mono_llvm_dispose_ee (LLVMExecutionEngineRef ee)
{
	delete unwrap (ee);

	delete fpm;
}
