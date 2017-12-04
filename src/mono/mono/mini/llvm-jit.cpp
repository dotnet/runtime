//
// jit-llvm.cpp: Support code for using LLVM as a JIT backend
//
// (C) 2009-2011 Novell, Inc.
// Copyright 2011-2015 Xamarin, Inc (http://www.xamarin.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Mono's internal header files are not C++ clean, so avoid including them if 
// possible
//

#include "config.h"

#include <llvm-c/Core.h>
#include <llvm-c/ExecutionEngine.h>

#include "mini-llvm-cpp.h"
#include "llvm-jit.h"

#if !defined(MONO_CROSS_COMPILE) && LLVM_API_VERSION > 100

/*
 * LLVM 3.9 uses the OrcJIT APIs
 */

#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/Host.h>
#include <llvm/Support/TargetSelect.h>
#include <llvm/IR/Mangler.h>
#include <llvm/ExecutionEngine/ExecutionEngine.h>
#include "llvm/ExecutionEngine/Orc/CompileUtils.h"
#include "llvm/ExecutionEngine/Orc/IRCompileLayer.h"
#include "llvm/ExecutionEngine/Orc/LambdaResolver.h"
#if LLVM_API_VERSION >= 500
#include "llvm/ExecutionEngine/RTDyldMemoryManager.h"
#include "llvm/ExecutionEngine/Orc/RTDyldObjectLinkingLayer.h"
#include "llvm/ExecutionEngine/JITSymbol.h"
#else
#include "llvm/ExecutionEngine/Orc/ObjectLinkingLayer.h"
#endif

#include <cstdlib>

extern "C" {
#include <mono/utils/mono-dl.h>
}

using namespace llvm;
using namespace llvm::orc;

extern cl::opt<bool> EnableMonoEH;
extern cl::opt<std::string> MonoEHFrameSymbol;

void
mono_llvm_set_unhandled_exception_handler (void)
{
}

template <typename T>
static std::vector<T> singletonSet(T t) {
  std::vector<T> Vec;
  Vec.push_back(std::move(t));
  return Vec;
}

#ifdef __MINGW32__

#include <stddef.h>
extern void *memset(void *, int, size_t);
void bzero (void *to, size_t count) { memset (to, 0, count); }

#endif

static AllocCodeMemoryCb *alloc_code_mem_cb;

class MonoJitMemoryManager : public RTDyldMemoryManager
{
public:
	~MonoJitMemoryManager() override;

	uint8_t *allocateDataSection(uintptr_t Size,
								 unsigned Alignment,
								 unsigned SectionID,
								 StringRef SectionName,
								 bool IsReadOnly) override;

	uint8_t *allocateCodeSection(uintptr_t Size,
								 unsigned Alignment,
								 unsigned SectionID,
								 StringRef SectionName) override;

	bool finalizeMemory(std::string *ErrMsg = nullptr) override;
};

MonoJitMemoryManager::~MonoJitMemoryManager()
{
}

uint8_t *
MonoJitMemoryManager::allocateDataSection(uintptr_t Size,
										  unsigned Alignment,
										  unsigned SectionID,
										  StringRef SectionName,
										  bool IsReadOnly) {
	uint8_t *res = (uint8_t*)malloc (Size);
	assert (res);
	memset (res, 0, Size);
	return res;
}

uint8_t *
MonoJitMemoryManager::allocateCodeSection(uintptr_t Size,
										  unsigned Alignment,
										  unsigned SectionID,
										  StringRef SectionName)
{
	return alloc_code_mem_cb (NULL, Size);
}

bool
MonoJitMemoryManager::finalizeMemory(std::string *ErrMsg)
{
	return false;
}

class MonoLLVMJIT {
public:
	/* We use our own trampoline infrastructure instead of the Orc one */
#if LLVM_API_VERSION >= 500
	typedef RTDyldObjectLinkingLayer ObjLayerT;
	typedef IRCompileLayer<ObjLayerT, SimpleCompiler> CompileLayerT;
	typedef CompileLayerT::ModuleHandleT ModuleHandleT;
#else
	typedef ObjectLinkingLayer<> ObjLayerT;
	typedef IRCompileLayer<ObjLayerT> CompileLayerT;
	typedef CompileLayerT::ModuleSetHandleT ModuleHandleT;
#endif

	MonoLLVMJIT (TargetMachine *TM, MonoJitMemoryManager *mm)
#if LLVM_API_VERSION >= 500
		: TM(TM), ObjectLayer([=] { return std::shared_ptr<RuntimeDyld::MemoryManager> (mm); }),
#else
		: TM(TM),
#endif
		  CompileLayer (ObjectLayer, SimpleCompiler (*TM)),
		  modules() {
	}

#if LLVM_API_VERSION >= 500
	ModuleHandleT addModule(Function *F, std::shared_ptr<Module> M) {
#else
	ModuleHandleT addModule(Function *F, Module *M) {
#endif
		auto Resolver = createLambdaResolver(
                      [&](const std::string &Name) {
						  const char *name = Name.c_str ();
#if LLVM_API_VERSION >= 500
						  JITSymbolFlags flags = JITSymbolFlags ();
#else
						  JITSymbolFlags flags = (JITSymbolFlags)0;
#endif
						  if (!strcmp (name, "___bzero")) {
#if LLVM_API_VERSION >= 500
							  return JITSymbol((uint64_t)(gssize)(void*)bzero, flags);
#else
							  return RuntimeDyld::SymbolInfo((uint64_t)(gssize)(void*)bzero, flags);
#endif
						  }

						  MonoDl *current;
						  char *err;
						  void *symbol;
						  current = mono_dl_open (NULL, 0, NULL);
						  g_assert (current);
						  if (name [0] == '_')
							  err = mono_dl_symbol (current, name + 1, &symbol);
						  else
							  err = mono_dl_symbol (current, name, &symbol);
						  mono_dl_close (current);
						  if (!symbol)
							  outs () << "R: " << Name << "\n";
						  assert (symbol);
#if LLVM_API_VERSION >= 500
						  return JITSymbol((uint64_t)(gssize)symbol, flags);
#else
						  return RuntimeDyld::SymbolInfo((uint64_t)(gssize)symbol, flags);
#endif
                      },
                      [](const std::string &S) {
						  outs () << "R2: " << S << "\n";
						  assert (0);
						  return nullptr;
					  } );

#if LLVM_API_VERSION >= 500
		ModuleHandleT m = CompileLayer.addModule(M,
												 std::move(Resolver)).get ();
		return m;
#else
		return CompileLayer.addModuleSet(singletonSet(M),
										  make_unique<MonoJitMemoryManager>(),
										  std::move(Resolver));
#endif
	}

	std::string mangle(const std::string &Name) {
		std::string MangledName;
		{
			raw_string_ostream MangledNameStream(MangledName);
			Mangler::getNameWithPrefix(MangledNameStream, Name,
									   TM->createDataLayout());
		}
		return MangledName;
	}

	std::string mangle(const GlobalValue *GV) {
		std::string MangledName;
		{
			Mangler Mang;

			raw_string_ostream MangledNameStream(MangledName);
			Mang.getNameWithPrefix(MangledNameStream, GV, false);
		}
		return MangledName;
	}

	gpointer compile (Function *F, int nvars, LLVMValueRef *callee_vars, gpointer *callee_addrs, gpointer *eh_frame) {
		F->getParent ()->setDataLayout (TM->createDataLayout ());
#if LLVM_API_VERSION >= 500
		// Orc uses a shared_ptr to refer to modules so we have to save them ourselves to keep a ref
		std::shared_ptr<Module> m (F->getParent ());
		modules.push_back (m);
		auto ModuleHandle = addModule (F, m);
#else
		auto ModuleHandle = addModule (F, F->getParent ());
#endif
		auto BodySym = CompileLayer.findSymbolIn(ModuleHandle, mangle (F), false);
		auto BodyAddr = BodySym.getAddress();
		assert (BodyAddr);

		for (int i = 0; i < nvars; ++i) {
			GlobalVariable *var = unwrap<GlobalVariable>(callee_vars [i]);

			auto sym = CompileLayer.findSymbolIn (ModuleHandle, mangle (var->getName ()), true);
			auto addr = sym.getAddress ();
			g_assert (addr);
#if LLVM_API_VERSION >= 500
			callee_addrs [i] = (gpointer)addr.get ();
#else
			callee_addrs [i] = (gpointer)addr;
#endif
		}

		auto ehsym = CompileLayer.findSymbolIn(ModuleHandle, "mono_eh_frame", false);
		auto ehaddr = ehsym.getAddress ();
		g_assert (ehaddr);
#if LLVM_API_VERSION >= 500
		*eh_frame = (gpointer)ehaddr.get ();
		return (gpointer)BodyAddr.get ();
#else
		*eh_frame = (gpointer)ehaddr;
		return (gpointer)BodyAddr;
#endif
	}

private:
	TargetMachine *TM;
	ObjLayerT ObjectLayer;
	CompileLayerT CompileLayer;
	std::vector<std::shared_ptr<Module>> modules;
};

static MonoLLVMJIT *jit;
static MonoJitMemoryManager *mono_mm;

MonoEERef
mono_llvm_create_ee (LLVMModuleProviderRef MP, AllocCodeMemoryCb *alloc_cb, FunctionEmittedCb *emitted_cb, ExceptionTableCb *exception_cb, DlSymCb *dlsym_cb, LLVMExecutionEngineRef *ee)
{
	alloc_code_mem_cb = alloc_cb;

	InitializeNativeTarget ();
	InitializeNativeTargetAsmPrinter();

	EnableMonoEH = true;
	MonoEHFrameSymbol = "mono_eh_frame";

	EngineBuilder EB;
#if defined(TARGET_AMD64) || defined(TARGET_X86)
	std::vector<std::string> attrs;
	// FIXME: Autodetect this
	attrs.push_back("sse3");
	attrs.push_back("sse4.1");
	EB.setMAttrs (attrs);
#endif
	auto TM = EB.selectTarget ();
	assert (TM);

	mono_mm = new MonoJitMemoryManager ();
	jit = new MonoLLVMJIT (TM, mono_mm);

	return NULL;
}

/*
 * mono_llvm_compile_method:
 *
 *   Compile METHOD to native code. Compute the addresses of the variables in CALLEE_VARS and store them into
 * CALLEE_ADDRS. Return the EH frame address in EH_FRAME.
 */
gpointer
mono_llvm_compile_method (MonoEERef mono_ee, LLVMValueRef method, int nvars, LLVMValueRef *callee_vars, gpointer *callee_addrs, gpointer *eh_frame)
{
	return jit->compile (unwrap<Function> (method), nvars, callee_vars, callee_addrs, eh_frame);
}

void
mono_llvm_dispose_ee (MonoEERef *eeref)
{
}

void
LLVMAddGlobalMapping(LLVMExecutionEngineRef EE, LLVMValueRef Global,
					 void* Addr)
{
	g_assert_not_reached ();
}

void*
LLVMGetPointerToGlobal(LLVMExecutionEngineRef EE, LLVMValueRef Global)
{
	g_assert_not_reached ();
	return NULL;
}

#elif !defined(MONO_CROSS_COMPILE) && LLVM_API_VERSION < 100

#include <stdint.h>

#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/Host.h>
#include <llvm/PassManager.h>
#include <llvm/ExecutionEngine/ExecutionEngine.h>
#include <llvm/ExecutionEngine/JITMemoryManager.h>
#include <llvm/ExecutionEngine/JITEventListener.h>
#include <llvm/Target/TargetOptions.h>
#include <llvm/Target/TargetRegisterInfo.h>
#include <llvm/IR/Verifier.h>
#include <llvm/Analysis/Passes.h>
#include <llvm/Transforms/Scalar.h>
#include <llvm/Support/CommandLine.h>
#include <llvm/IR/LegacyPassNameParser.h>
#include <llvm/Support/PrettyStackTrace.h>
#include <llvm/CodeGen/Passes.h>
#include <llvm/CodeGen/MachineFunctionPass.h>
#include <llvm/CodeGen/MachineFunction.h>
#include <llvm/CodeGen/MachineFrameInfo.h>
#include <llvm/IR/Function.h>
#include <llvm/IR/IRBuilder.h>
#include <llvm/IR/Module.h>

using namespace llvm;

static void (*unhandled_exception)() = default_mono_llvm_unhandled_exception;

void
mono_llvm_set_unhandled_exception_handler (void)
{
	std::set_terminate (unhandled_exception);
}

class MonoJITMemoryManager : public JITMemoryManager
{
private:
	JITMemoryManager *mm;

public:
	/* Callbacks installed by mono */
	AllocCodeMemoryCb *alloc_cb;
	DlSymCb *dlsym_cb;
	ExceptionTableCb *exception_cb;

	MonoJITMemoryManager ();
	~MonoJITMemoryManager ();

	void setMemoryWritable (void);

	void setMemoryExecutable (void);

	void AllocateGOT();

    unsigned char *getGOTBase() const {
		return mm->getGOTBase ();
    }

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

	virtual void deallocateFunctionBody(void*) {
	}

	virtual void deallocateExceptionTable(void*) {
	}

	virtual uint8_t *allocateCodeSection(uintptr_t Size, unsigned Alignment, unsigned SectionID,
										 StringRef SectionName) {
		// FIXME:
		assert(0);
		return NULL;
	}

	virtual uint8_t *allocateDataSection(uintptr_t Size, unsigned Alignment, unsigned SectionID,
										 StringRef SectionName, bool IsReadOnly) {
		// FIXME:
		assert(0);
		return NULL;
	}

	virtual bool applyPermissions(std::string*) {
		// FIXME:
		assert(0);
		return false;
	}

	virtual bool finalizeMemory(std::string *ErrMsg = 0) {
		// FIXME:
		assert(0);
		return false;
	}

	virtual void* getPointerToNamedFunction(const std::string &Name, bool AbortOnFailure) {
		void *res;
		char *err;

		err = dlsym_cb (Name.c_str (), &res);
		if (err) {
			outs () << "Unable to resolve: " << Name << ": " << err << "\n";
			assert(0);
			return NULL;
		}
		return res;
	}
};

MonoJITMemoryManager::MonoJITMemoryManager ()
{
	mm = JITMemoryManager::CreateDefaultMemManager ();
}

MonoJITMemoryManager::~MonoJITMemoryManager ()
{
	delete mm;
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

unsigned char *
MonoJITMemoryManager::startFunctionBody(const Function *F, 
					uintptr_t &ActualSize)
{
	// FIXME: This leaks memory
	if (ActualSize == 0)
		ActualSize = 128;
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
	return startFunctionBody(F, ActualSize);
}
  
void
MonoJITMemoryManager::endExceptionTable(const Function *F, unsigned char *TableStart,
					unsigned char *TableEnd, 
					unsigned char* FrameRegister)
{
	exception_cb (FrameRegister);
}

class MonoJITEventListener : public JITEventListener {

public:
	FunctionEmittedCb *emitted_cb;

	MonoJITEventListener (FunctionEmittedCb *cb) {
		emitted_cb = cb;
	}

	virtual void NotifyFunctionEmitted(const Function &F,
									   void *Code, size_t Size,
									   const EmittedFunctionDetails &Details) {
		emitted_cb (wrap (&F), Code, (char*)Code + Size);
	}
};

class MonoEE {
public:
	ExecutionEngine *EE;
	MonoJITMemoryManager *mm;
	MonoJITEventListener *listener;
	FunctionPassManager *fpm;
};

void
mono_llvm_optimize_method (MonoEERef eeref, LLVMValueRef method)
{
	MonoEE *mono_ee = (MonoEE*)eeref;

	/*
	 * The verifier does some checks on the whole module, leading to quadratic behavior.
	 */
	//verifyFunction (*(unwrap<Function> (method)));
	mono_ee->fpm->run (*unwrap<Function> (method));
}

static cl::list<const PassInfo*, bool, PassNameParser>
PassList(cl::desc("Optimizations available:"));

static void
force_pass_linking (void)
{
	// Make sure the rest is linked in, but never executed
	char *foo = g_getenv ("FOO");
	gboolean ret = (foo != (char*)-1);
	g_free (foo);

	if (ret) 
		return;

	// This is a subset of the passes in LinkAllPasses.h
	// The utility passes and the interprocedural passes are commented out

      (void) llvm::createAAEvalPass();
      (void) llvm::createAggressiveDCEPass();
      (void) llvm::createAliasAnalysisCounterPass();
      (void) llvm::createAliasDebugger();
	  /*
      (void) llvm::createArgumentPromotionPass();
      (void) llvm::createStructRetPromotionPass();
	  */
      (void) llvm::createBasicAliasAnalysisPass();
      (void) llvm::createLibCallAliasAnalysisPass(0);
      (void) llvm::createScalarEvolutionAliasAnalysisPass();
      //(void) llvm::createBlockPlacementPass();
      (void) llvm::createBreakCriticalEdgesPass();
      (void) llvm::createCFGSimplificationPass();
	  /*
      (void) llvm::createConstantMergePass();
      (void) llvm::createConstantPropagationPass();
	  */
	  /*
      (void) llvm::createDeadArgEliminationPass();
	  */
      (void) llvm::createDeadCodeEliminationPass();
      (void) llvm::createDeadInstEliminationPass();
      (void) llvm::createDeadStoreEliminationPass();
	  /*
      (void) llvm::createDeadTypeEliminationPass();
      (void) llvm::createDomOnlyPrinterPass();
      (void) llvm::createDomPrinterPass();
      (void) llvm::createDomOnlyViewerPass();
      (void) llvm::createDomViewerPass();
      (void) llvm::createEdgeProfilerPass();
      (void) llvm::createOptimalEdgeProfilerPass();
      (void) llvm::createFunctionInliningPass();
      (void) llvm::createAlwaysInlinerPass();
      (void) llvm::createGlobalDCEPass();
      (void) llvm::createGlobalOptimizerPass();
      (void) llvm::createGlobalsModRefPass();
      (void) llvm::createIPConstantPropagationPass();
      (void) llvm::createIPSCCPPass();
	  */
      (void) llvm::createIndVarSimplifyPass();
      (void) llvm::createInstructionCombiningPass();
	  /*
      (void) llvm::createInternalizePass(false);
	  */
      (void) llvm::createLCSSAPass();
      (void) llvm::createLICMPass();
      (void) llvm::createLazyValueInfoPass();
      //(void) llvm::createLoopDependenceAnalysisPass();
	  /*
      (void) llvm::createLoopExtractorPass();
	  */
      (void) llvm::createLoopSimplifyPass();
      (void) llvm::createLoopStrengthReducePass();
      (void) llvm::createLoopUnrollPass();
      (void) llvm::createLoopUnswitchPass();
      (void) llvm::createLoopRotatePass();
      (void) llvm::createLowerInvokePass();
	  /*
      (void) llvm::createLowerSetJmpPass();
	  */
      (void) llvm::createLowerSwitchPass();
      (void) llvm::createNoAAPass();
	  /*
      (void) llvm::createNoProfileInfoPass();
      (void) llvm::createProfileEstimatorPass();
      (void) llvm::createProfileVerifierPass();
      (void) llvm::createProfileLoaderPass();
	  */
      (void) llvm::createPromoteMemoryToRegisterPass();
      (void) llvm::createDemoteRegisterToMemoryPass();
	  /*
      (void) llvm::createPruneEHPass();
      (void) llvm::createPostDomOnlyPrinterPass();
      (void) llvm::createPostDomPrinterPass();
      (void) llvm::createPostDomOnlyViewerPass();
      (void) llvm::createPostDomViewerPass();
	  */
      (void) llvm::createReassociatePass();
      (void) llvm::createSCCPPass();
      (void) llvm::createScalarReplAggregatesPass();
      //(void) llvm::createSimplifyLibCallsPass();
	  /*
      (void) llvm::createSingleLoopExtractorPass();
      (void) llvm::createStripSymbolsPass();
      (void) llvm::createStripNonDebugSymbolsPass();
      (void) llvm::createStripDeadDebugInfoPass();
      (void) llvm::createStripDeadPrototypesPass();
      (void) llvm::createTailCallEliminationPass();
      (void) llvm::createTailDuplicationPass();
      (void) llvm::createJumpThreadingPass();
	  */
	  /*
      (void) llvm::createUnifyFunctionExitNodesPass();
	  */
      (void) llvm::createInstCountPass();
      (void) llvm::createCodeGenPreparePass();
      (void) llvm::createGVNPass();
      (void) llvm::createMemCpyOptPass();
      (void) llvm::createLoopDeletionPass();
	  /*
      (void) llvm::createPostDomTree();
      (void) llvm::createPostDomFrontier();
      (void) llvm::createInstructionNamerPass();
      (void) llvm::createPartialSpecializationPass();
      (void) llvm::createFunctionAttrsPass();
      (void) llvm::createMergeFunctionsPass();
      (void) llvm::createPrintModulePass(0);
      (void) llvm::createPrintFunctionPass("", 0);
      (void) llvm::createDbgInfoPrinterPass();
      (void) llvm::createModuleDebugInfoPrinterPass();
      (void) llvm::createPartialInliningPass();
      (void) llvm::createGEPSplitterPass();
      (void) llvm::createLintPass();
	  */
      (void) llvm::createSinkingPass();
}

static gboolean inited;

static void
init_llvm (void)
{
	if (inited)
		return;

  force_pass_linking ();

#ifdef TARGET_ARM
  LLVMInitializeARMTarget ();
  LLVMInitializeARMTargetInfo ();
  LLVMInitializeARMTargetMC ();
#elif defined(TARGET_X86) || defined(TARGET_AMD64)
  LLVMInitializeX86Target ();
  LLVMInitializeX86TargetInfo ();
  LLVMInitializeX86TargetMC ();
#elif defined(TARGET_POWERPC)
  LLVMInitializePowerPCTarget ();
  LLVMInitializePowerPCTargetInfo ();
  LLVMInitializePowerPCTargetMC ();
#else
  #error Unsupported mono-llvm target
#endif

  PassRegistry &Registry = *PassRegistry::getPassRegistry();
  initializeCore(Registry);
  initializeScalarOpts(Registry);
  initializeAnalysis(Registry);
  initializeIPA(Registry);
  initializeTransformUtils(Registry);
  initializeInstCombine(Registry);
  initializeTarget(Registry);

  llvm::cl::ParseEnvironmentOptions("mono", "MONO_LLVM", "");

  inited = true;
}

MonoEERef
mono_llvm_create_ee (LLVMModuleProviderRef MP, AllocCodeMemoryCb *alloc_cb, FunctionEmittedCb *emitted_cb, ExceptionTableCb *exception_cb, DlSymCb *dlsym_cb, LLVMExecutionEngineRef *ee)
{
  std::string Error;
  MonoEE *mono_ee;

  init_llvm ();

  mono_ee = new MonoEE ();

  MonoJITMemoryManager *mono_mm = new MonoJITMemoryManager ();
  mono_mm->alloc_cb = alloc_cb;
  mono_mm->dlsym_cb = dlsym_cb;
  mono_mm->exception_cb = exception_cb;
  mono_ee->mm = mono_mm;

  /*
   * The Default code model doesn't seem to work on amd64,
   * test_0_fields_with_big_offsets (among others) crashes, because LLVM tries to call
   * memset using a normal pcrel code which is in 32bit memory, while memset isn't.
   */

  TargetOptions opts;
  opts.JITExceptionHandling = 1;

  StringRef cpu_name = sys::getHostCPUName ();

  // EngineBuilder no longer has a copy assignment operator (?)
  std::unique_ptr<Module> Owner(unwrap(MP));
  EngineBuilder b (std::move(Owner));
  ExecutionEngine *EE = b.setJITMemoryManager (mono_mm).setTargetOptions (opts).setAllocateGVsWithCode (true).setMCPU (cpu_name).create ();

  g_assert (EE);
  mono_ee->EE = EE;

  MonoJITEventListener *listener = new MonoJITEventListener (emitted_cb);
  EE->RegisterJITEventListener (listener);
  mono_ee->listener = listener;

  FunctionPassManager *fpm = new FunctionPassManager (unwrap (MP));
  mono_ee->fpm = fpm;

  fpm->add(new DataLayoutPass(*EE->getDataLayout()));

  if (PassList.size() > 0) {
	  /* Use the passes specified by the env variable */
	  /* Only the passes in force_pass_linking () can be used */
	  for (unsigned i = 0; i < PassList.size(); ++i) {
		  const PassInfo *PassInf = PassList[i];
		  Pass *P = 0;

		  if (PassInf->getNormalCtor())
			  P = PassInf->getNormalCtor()();
		  fpm->add (P);
	  }
  } else {
	  /* Use the same passes used by 'opt' by default, without the ipo passes */
	  const char *opts = "-simplifycfg -domtree -domfrontier -scalarrepl -instcombine -simplifycfg -domtree -domfrontier -scalarrepl -instcombine -simplifycfg -instcombine -simplifycfg -reassociate -domtree -loops -loop-simplify -domfrontier -loop-simplify -lcssa -loop-rotate -licm -lcssa -loop-unswitch -instcombine -scalar-evolution -loop-simplify -lcssa -iv-users -indvars -loop-deletion -loop-simplify -lcssa -loop-unroll -instcombine -memdep -gvn -memdep -memcpyopt -sccp -instcombine -domtree -memdep -dse -adce -gvn -simplifycfg";
	  char **args;
	  int i;

	  args = g_strsplit (opts, " ", 1000);
	  for (i = 0; args [i]; i++)
		  ;
	  llvm::cl::ParseCommandLineOptions (i, args, "");
	  g_strfreev (args);

	  for (unsigned i = 0; i < PassList.size(); ++i) {
		  const PassInfo *PassInf = PassList[i];
		  Pass *P = 0;

		  if (PassInf->getNormalCtor())
			  P = PassInf->getNormalCtor()();
		  g_assert (P->getPassKind () == llvm::PT_Function || P->getPassKind () == llvm::PT_Loop);
		  fpm->add (P);
	  }

	  /*
	  fpm->add(createInstructionCombiningPass());
	  fpm->add(createReassociatePass());
	  fpm->add(createGVNPass());
	  fpm->add(createCFGSimplificationPass());
	  */
  }

  *ee = wrap (EE);

  return mono_ee;
}

void
mono_llvm_dispose_ee (MonoEERef *eeref)
{
	MonoEE *mono_ee = (MonoEE*)eeref;

	delete mono_ee->EE;
	delete mono_ee->fpm;
	//delete mono_ee->mm;
	delete mono_ee->listener;
	delete mono_ee;
}

#else /* MONO_CROSS_COMPILE */

void
mono_llvm_set_unhandled_exception_handler (void)
{
}

MonoEERef
mono_llvm_create_ee (LLVMModuleProviderRef MP, AllocCodeMemoryCb *alloc_cb, FunctionEmittedCb *emitted_cb, ExceptionTableCb *exception_cb, DlSymCb *dlsym_cb, LLVMExecutionEngineRef *ee)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_llvm_optimize_method (MonoEERef eeref, LLVMValueRef method)
{
	g_assert_not_reached ();
}

gpointer
mono_llvm_compile_method (MonoEERef mono_ee, LLVMValueRef method, int nvars, LLVMValueRef *callee_vars, gpointer *callee_addrs, gpointer *eh_frame)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_llvm_dispose_ee (MonoEERef *eeref)
{
	g_assert_not_reached ();
}

/* Not linked in */
void
LLVMAddGlobalMapping(LLVMExecutionEngineRef EE, LLVMValueRef Global,
					 void* Addr)
{
	g_assert_not_reached ();
}

void*
LLVMGetPointerToGlobal(LLVMExecutionEngineRef EE, LLVMValueRef Global)
{
	g_assert_not_reached ();
	return NULL;
}

#endif /* !MONO_CROSS_COMPILE */
