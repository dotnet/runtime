//
// jit-llvm.cpp: Support code for using LLVM as a JIT backend
//
// (C) 2009-2011 Novell, Inc.
// Copyright 2011-2015 Xamarin, Inc (http://www.xamarin.com)
//
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

#include "config.h"

#include <llvm-c/Core.h>
#include <llvm-c/ExecutionEngine.h>

#include "mini-llvm-cpp.h"
#include "mini-runtime.h"
#include "llvm-jit.h"

#if defined(MONO_ARCH_LLVM_JIT_SUPPORTED) && !defined(MONO_CROSS_COMPILE)

#include <llvm/ADT/SmallVector.h>
#include <llvm/Support/raw_ostream.h>
#include <llvm/Support/Host.h>
#include <llvm/Support/Memory.h>
#include <llvm/Support/TargetSelect.h>
#include <llvm/IR/Mangler.h>
#include "llvm/IR/LegacyPassManager.h"
#include "llvm/IR/LegacyPassNameParser.h"
#include <llvm/ExecutionEngine/ExecutionEngine.h>
#include "llvm/ExecutionEngine/Orc/CompileUtils.h"
#include "llvm/ExecutionEngine/Orc/IRCompileLayer.h"
#include "llvm/ExecutionEngine/Orc/LambdaResolver.h"
#include "llvm/ExecutionEngine/RTDyldMemoryManager.h"
#include "llvm/ExecutionEngine/Orc/RTDyldObjectLinkingLayer.h"
#include "llvm/ExecutionEngine/JITSymbol.h"
#include "llvm/Transforms/Scalar.h"

#include "llvm/CodeGen/BuiltinGCs.h"

#if LLVM_API_VERSION >= 1100
#include "llvm/InitializePasses.h"
#endif

#include <cstdlib>

#include <mono/utils/mono-dl.h>

using namespace llvm;
using namespace llvm::orc;

extern cl::opt<bool> EnableMonoEH;
extern cl::opt<std::string> MonoEHFrameSymbol;

void
mono_llvm_set_unhandled_exception_handler (void)
{
}

// noop function that merely ensures that certain symbols are not eliminated
// from the resulting binary.
static void
link_gc () {
	llvm::linkAllBuiltinGCs();
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

static MonoNativeTlsKey current_cfg_tls_id;

static unsigned char *
alloc_code (LLVMValueRef function, int size)
{
	auto cfg = (MonoCompile *)mono_native_tls_get_value (current_cfg_tls_id);
	g_assert (cfg);
	return (unsigned char *)mono_mem_manager_code_reserve (cfg->mem_manager, size);
}

class MonoLLVMMemoryManager : public RTDyldMemoryManager
{
public:
	~MonoLLVMMemoryManager() override;

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
private:
	SmallVector<sys::MemoryBlock, 16> PendingCodeMem;
};

MonoLLVMMemoryManager::~MonoLLVMMemoryManager()
{
}

uint8_t *
MonoLLVMMemoryManager::allocateDataSection(uintptr_t Size,
										  unsigned Alignment,
										  unsigned SectionID,
										  StringRef SectionName,
										  bool IsReadOnly)
{
	uint8_t *res;

	// FIXME: Use a mempool
	if (Alignment == 0)
                Alignment = 16;
	res = (uint8_t*)malloc (Size + Alignment);
	res = (uint8_t*)ALIGN_PTR_TO(res, Alignment);
	assert (res);
	g_assert (GPOINTER_TO_UINT (res) % Alignment == 0);
	memset (res, 0, Size);
	return res;
}

uint8_t *
MonoLLVMMemoryManager::allocateCodeSection(uintptr_t Size,
										  unsigned Alignment,
										  unsigned SectionID,
										  StringRef SectionName)
{
	uint8_t *mem = alloc_code (NULL, Size);
	PendingCodeMem.push_back (sys::MemoryBlock ((void *)mem, Size));
	return mem;
}

bool
MonoLLVMMemoryManager::finalizeMemory(std::string *ErrMsg)
{
	for (sys::MemoryBlock &Block : PendingCodeMem) {
		sys::Memory::InvalidateInstructionCache (Block.base (), Block.allocatedSize ());
	}
	PendingCodeMem.clear ();
	return false;
}

#if defined(TARGET_AMD64) || defined(TARGET_X86)
#define NO_CALL_FRAME_OPT " -no-x86-call-frame-opt"
#else
#define NO_CALL_FRAME_OPT ""
#endif

// The OptimizationList is automatically populated with registered Passes by the
// PassNameParser.
//
static cl::list<const PassInfo*, bool, PassNameParser>
PassList(cl::desc("Optimizations available:"));

static void
init_function_pass_manager (legacy::FunctionPassManager &fpm)
{
	auto reg = PassRegistry::getPassRegistry ();
	for (size_t i = 0; i < PassList.size(); i++) {
		Pass *pass = PassList[i]->getNormalCtor()();
		if (pass->getPassKind () == llvm::PT_Function || pass->getPassKind () == llvm::PT_Loop) {
			fpm.add (pass);
		} else {
			auto info = reg->getPassInfo (pass->getPassID());
			auto name = info->getPassArgument ();
			printf("Opt pass is ignored: %.*s\n", (int) name.size(), name.data());
		}
	}
	// -place-safepoints pass is mandatory
	fpm.add (createPlaceSafepointsPass ());

	fpm.doInitialization();
}

#if LLVM_API_VERSION >= 1100
using symbol_t = const llvm::StringRef;
static inline std::string
to_str (symbol_t s)
{
	return s.str ();
}
#else
using symbol_t = const std::string &;
static inline const std::string &
to_str (symbol_t s)
{
	return s;
}
#endif

struct MonoLLVMJIT {
	std::shared_ptr<MonoLLVMMemoryManager> mmgr;
	ExecutionSession execution_session;
	std::map<VModuleKey, std::shared_ptr<SymbolResolver>> resolvers;
	TargetMachine *target_machine;
	LegacyRTDyldObjectLinkingLayer object_layer;
	LegacyIRCompileLayer<decltype(object_layer), SimpleCompiler> compile_layer;
	DataLayout data_layout;
	legacy::FunctionPassManager fpm;

	MonoLLVMJIT (TargetMachine *tm, Module *pgo_module)
		: mmgr (std::make_shared<MonoLLVMMemoryManager>())
		, target_machine (tm)
		, object_layer (
			AcknowledgeORCv1Deprecation, execution_session,
			[this] (VModuleKey k) {
				return LegacyRTDyldObjectLinkingLayer::Resources{
					mmgr, resolvers[k] };
			})
		, compile_layer (
			AcknowledgeORCv1Deprecation, object_layer,
			SimpleCompiler{*target_machine})
		, data_layout (target_machine->createDataLayout())
		, fpm (pgo_module)
	{
		compile_layer.setNotifyCompiled ([] (VModuleKey, std::unique_ptr<Module> module) {
			module.release ();
		});
		init_function_pass_manager (fpm);
	}

	VModuleKey
	add_module (std::unique_ptr<Module> m)
	{
		auto k = execution_session.allocateVModule();
		auto lookup_name = [this] (symbol_t nameref) {
			const auto &namestr = to_str (nameref);
			auto jit_sym = compile_layer.findSymbol (namestr, false);
			if (jit_sym) {
				return jit_sym;
			}
			JITSymbolFlags flags{};
			if (namestr == "___bzero") {
				return JITSymbol{(uint64_t)(gssize)(void*)bzero, flags};
			}
			ERROR_DECL (error);
			auto namebuf = namestr.c_str ();
			auto current = mono_dl_open (NULL, 0, error);
			mono_error_cleanup (error);
			g_assert (current);
			auto name = namebuf[0] == '_' ? namebuf + 1 : namebuf;
			void *sym = nullptr;
			error_init_reuse (error);
			sym = mono_dl_symbol (current, name, error);
			if (!sym) {
				outs () << "R: " << namestr << " " << mono_error_get_message_without_fields (error) << "\n";
			}
			mono_error_cleanup (error);
			assert (sym);
			return JITSymbol{(uint64_t)(gssize)sym, flags};
		};
		auto on_error = [] (Error err) {
			outs () << "R2: " << err << "\n";
			assert (0);
		};
		auto resolver = createLegacyLookupResolver (execution_session,
			lookup_name, on_error);
		resolvers[k] = std::move (resolver);
		auto err = compile_layer.addModule (k, std::move(m));
		if (err) {
			outs () << "addModule error: " << err << "\n";
			assert (0);
		}
		return k;
	}

	std::string
	mangle (llvm::StringRef name)
	{
		std::string ret;
		raw_string_ostream out{ret};
		Mangler::getNameWithPrefix (out, name, data_layout);
		return ret;
	}

	std::string
	mangle (const GlobalValue *gv)
	{
		std::string ret;
		raw_string_ostream out{ret};
		Mangler{}.getNameWithPrefix (out, gv, false);
		return ret;
	}

	void
	optimize (Function *func)
	{
		auto module = func->getParent ();
		module->setDataLayout (data_layout);
		fpm.run (*func);
	}

	gpointer
	compile (
		Function *func, int nvars, LLVMValueRef *callee_vars,
		gpointer *callee_addrs, gpointer *eh_frame)
	{
		auto module = func->getParent ();
		module->setDataLayout (data_layout);
		// The lifetime of this module is managed by Mono, not LLVM, so
		// the `unique_ptr` created here will be released in the
		// NotifyCompiled callback.
		auto k = add_module (std::unique_ptr<Module>(module));
		auto bodysym = compile_layer.findSymbolIn (k, mangle (func), false);
		auto bodyaddr = bodysym.getAddress ();
		if (!bodyaddr)
			g_assert_not_reached();
		for (int i = 0; i < nvars; ++i) {
			auto var = unwrap<GlobalVariable> (callee_vars[i]);
			auto sym = compile_layer.findSymbolIn (k, mangle (var->getName ()), true);
			auto addr = sym.getAddress ();
			g_assert ((bool)addr);
			callee_addrs[i] = (gpointer)addr.get ();
		}
		auto ehsym = compile_layer.findSymbolIn (k, "mono_eh_frame", false);
		auto ehaddr = ehsym.getAddress ();
		g_assert ((bool)ehaddr);
		*eh_frame = (gpointer)ehaddr.get ();
		return (gpointer)bodyaddr.get ();
	}
};

static MonoLLVMJIT *
make_mono_llvm_jit (TargetMachine *target_machine, llvm::Module *pgo_module)
{
	return new MonoLLVMJIT{target_machine, pgo_module};
}

static llvm::Module *dummy_pgo_module = nullptr;
static MonoLLVMJIT *jit;

static void
init_passes_and_options ()
{
	PassRegistry &registry = *PassRegistry::getPassRegistry();
	initializeCore(registry);
	initializeScalarOpts(registry);
	initializeInstCombine(registry);
	initializeTarget(registry);
	initializeLoopIdiomRecognizeLegacyPassPass(registry);

	// FIXME: find optimal mono specific order of passes
	// see https://llvm.org/docs/Frontend/PerformanceTips.html#pass-ordering
	// the following order is based on a stripped version of "OPT -O2"
	const char *default_opts = " -simplifycfg -sroa -lower-expect -instcombine -sroa -jump-threading -loop-rotate -licm -simplifycfg -lcssa -loop-idiom -indvars -loop-deletion -gvn -memcpyopt -sccp -bdce -instcombine -dse -simplifycfg -enable-implicit-null-checks -sroa -instcombine" NO_CALL_FRAME_OPT;
	const char *opts = g_getenv ("MONO_LLVM_OPT");
	if (opts == NULL)
		opts = default_opts;
	else if (opts[0] == '+') // Append passes to the default order if starts with '+', overwrite otherwise
		opts = g_strdup_printf ("%s %s", default_opts, opts + 1);
	else if (opts[0] != ' ') // pass order has to start with a leading whitespace
		opts = g_strdup_printf (" %s", opts);

	char **args = g_strsplit (opts, " ", -1);
	llvm::cl::ParseCommandLineOptions (g_strv_length (args), args, "");
	g_strfreev (args);
}

void
mono_llvm_jit_init ()
{
	if (jit != nullptr) return;

	link_gc ();

	mono_native_tls_alloc (&current_cfg_tls_id, NULL);

	InitializeNativeTarget ();
	InitializeNativeTargetAsmPrinter();

	EnableMonoEH = true;
	MonoEHFrameSymbol = "mono_eh_frame";
	EngineBuilder EB;

	if (mono_use_fast_math) {
		TargetOptions opts;
		opts.NoInfsFPMath = true;
		opts.NoNaNsFPMath = true;
		opts.NoSignedZerosFPMath = true;
		opts.NoTrappingFPMath = true;
		opts.UnsafeFPMath = true;
		opts.AllowFPOpFusion = FPOpFusion::Fast;
		EB.setTargetOptions (opts);
	}

	EB.setOptLevel (CodeGenOpt::Aggressive);
	EB.setMCPU (sys::getHostCPUName ());

#ifdef TARGET_AMD64
	EB.setMArch ("x86-64");
#elif TARGET_X86
	EB.setMArch ("x86");
#elif TARGET_ARM64
	EB.setMArch ("aarch64");
#elif TARGET_ARM
	EB.setMArch ("arm");
#else
	g_assert_not_reached ();
#endif

	llvm::StringMap<bool> cpu_features;
	// Why 76? LLVM 9 supports 76 different x86 feature strings. This
	// requires around 1216 bytes of data in the local activation record.
	// It'd be possible to stream entries to setMAttrs using
	// llvm::map_range and llvm::make_filter_range, but llvm::map_range
	// isn't available in LLVM 6, and it's not worth writing a small
	// single-purpose one here.
	llvm::SmallVector<llvm::StringRef, 76> supported_features;
	if (llvm::sys::getHostCPUFeatures (cpu_features)) {
		for (const auto &feature : cpu_features) {
			if (feature.second)
				supported_features.push_back (feature.first ());
		}
		EB.setMAttrs (supported_features);
	}

	auto TM = EB.selectTarget ();
	assert (TM);
	dummy_pgo_module = unwrap (LLVMModuleCreateWithName("dummy-pgo-module"));
	init_passes_and_options ();
	jit = make_mono_llvm_jit (TM, dummy_pgo_module);
}

MonoEERef
mono_llvm_create_ee (LLVMExecutionEngineRef *ee)
{
	return NULL;
}

void
mono_llvm_optimize_method (LLVMValueRef method)
{
	jit->optimize (unwrap<Function> (method));
}

/*
 * mono_llvm_compile_method:
 *
 *   Compile METHOD to native code. Compute the addresses of the variables in CALLEE_VARS and store them into
 * CALLEE_ADDRS. Return the EH frame address in EH_FRAME.
 */
gpointer
mono_llvm_compile_method (MonoEERef mono_ee, MonoCompile *cfg, LLVMValueRef method, int nvars, LLVMValueRef *callee_vars, gpointer *callee_addrs, gpointer *eh_frame)
{
	mono_native_tls_set_value (current_cfg_tls_id, cfg);
	auto ret = jit->compile (unwrap<Function> (method), nvars, callee_vars, callee_addrs, eh_frame);
	mono_native_tls_set_value (current_cfg_tls_id, nullptr);
	return ret;
}

void
mono_llvm_dispose_ee (MonoEERef *eeref)
{
}

#else /* MONO_CROSS_COMPILE */

void
mono_llvm_set_unhandled_exception_handler (void)
{
}

void
mono_llvm_jit_init ()
{
}

MonoEERef
mono_llvm_create_ee (LLVMExecutionEngineRef *ee)
{
	g_error ("LLVM JIT not supported on this platform.");
	return NULL;
}

gpointer
mono_llvm_compile_method (MonoEERef mono_ee, MonoCompile *cfg, LLVMValueRef method, int nvars, LLVMValueRef *callee_vars, gpointer *callee_addrs, gpointer *eh_frame)
{
	g_assert_not_reached ();
	return NULL;
}

void
mono_llvm_dispose_ee (MonoEERef *eeref)
{
	g_assert_not_reached ();
}

#endif /* !MONO_CROSS_COMPILE */
