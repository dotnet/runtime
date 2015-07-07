/*
 * Handle the differences between the llvm backend beeing embedded
 * or loaded at runtime.
 */

#include "mini.h"

#ifdef MONO_LLVM_LOADED

#ifdef __MACH__
#include <mach-o/dyld.h>
#endif

typedef void (*MonoLLVMVoidFunc)(void);
typedef void (*MonoLLVMCFGFunc)(MonoCompile *cfg);
typedef void (*MonoLLVMEmitCallFunc)(MonoCompile *cfg, MonoCallInst *call);
typedef void (*MonoLLVMCreateAotFunc)(MonoAssembly *assembly, const char *global_prefix, gboolean emit_dwarf, gboolean static_link);
typedef void (*MonoLLVMEmitAotFunc)(const char *filename, const char *cu_name);
typedef void (*MonoLLVMEmitAotInfoFunc)(MonoAotFileInfo *info, gboolean has_jitted_code);
typedef void (*MonoLLVMEmitAotDataFunc)(const char *symbol, guint8 *data, int data_len);
typedef void (*MonoLLVMFreeDomainFunc)(MonoDomain *domain);

static MonoLLVMVoidFunc mono_llvm_init_fptr;
static MonoLLVMVoidFunc mono_llvm_cleanup_fptr;
static MonoLLVMCFGFunc mono_llvm_emit_method_fptr;
static MonoLLVMEmitCallFunc mono_llvm_emit_call_fptr;
static MonoLLVMCreateAotFunc mono_llvm_create_aot_module_fptr;
static MonoLLVMEmitAotFunc mono_llvm_emit_aot_module_fptr;
static MonoLLVMCFGFunc mono_llvm_check_method_supported_fptr;
static MonoLLVMEmitAotInfoFunc mono_llvm_emit_aot_file_info_fptr;
static MonoLLVMEmitAotDataFunc mono_llvm_emit_aot_data_fptr;
static MonoLLVMFreeDomainFunc mono_llvm_free_domain_info_fptr;

void
mono_llvm_init (void)
{
	mono_llvm_init_fptr ();
}

void
mono_llvm_cleanup (void)
{
	mono_llvm_cleanup_fptr ();
}

void
mono_llvm_emit_method (MonoCompile *cfg)
{
	mono_llvm_emit_method_fptr (cfg);
}

void
mono_llvm_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	mono_llvm_emit_call_fptr (cfg, call);
}

void
mono_llvm_create_aot_module (MonoAssembly *assembly, const char *global_prefix, gboolean emit_dwarf, gboolean static_link)
{
	g_assert (mono_llvm_create_aot_module_fptr);
	mono_llvm_create_aot_module_fptr (assembly, global_prefix, emit_dwarf, static_link);
}

void
mono_llvm_emit_aot_module (const char *filename, const char *cu_name)
{
	g_assert (mono_llvm_emit_aot_module_fptr);
	mono_llvm_emit_aot_module_fptr (filename, cu_name);
}

void
mono_llvm_check_method_supported (MonoCompile *cfg)
{
	mono_llvm_check_method_supported_fptr (cfg);
}

void
mono_llvm_free_domain_info (MonoDomain *domain)
{
	if (mono_llvm_free_domain_info_fptr)
		mono_llvm_free_domain_info_fptr (domain);
}

void
mono_llvm_emit_aot_file_info (MonoAotFileInfo *info, gboolean has_jitted_code)
{
	if (mono_llvm_emit_aot_file_info_fptr)
		mono_llvm_emit_aot_file_info_fptr (info, has_jitted_code);
}

void
mono_llvm_emit_aot_data (const char *symbol, guint8 *data, int data_len)
{
	if (mono_llvm_emit_aot_data_fptr)
		mono_llvm_emit_aot_data_fptr (symbol, data, data_len);
}

int
mono_llvm_load (const char* bpath)
{
	char *err = NULL;
	MonoDl *llvm_lib = mono_dl_open_runtime_lib ("mono-llvm", MONO_DL_LAZY, &err);

	if (!llvm_lib) {
		g_warning ("llvm load failed: %s\n", err);
		g_free (err);
		return FALSE;
	}

	err = mono_dl_symbol (llvm_lib, "mono_llvm_init", (void**)&mono_llvm_init_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_cleanup", (void**)&mono_llvm_cleanup_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_emit_method", (void**)&mono_llvm_emit_method_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_emit_call", (void**)&mono_llvm_emit_call_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_create_aot_module", (void**)&mono_llvm_create_aot_module_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_emit_aot_module", (void**)&mono_llvm_emit_aot_module_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_check_method_supported", (void**)&mono_llvm_check_method_supported_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_free_domain_info", (void**)&mono_llvm_free_domain_info_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_emit_aot_file_info", (void**)&mono_llvm_emit_aot_file_info_fptr);
	if (err) goto symbol_error;
	err = mono_dl_symbol (llvm_lib, "mono_llvm_emit_aot_data", (void**)&mono_llvm_emit_aot_data_fptr);
	if (err) goto symbol_error;
	return TRUE;
symbol_error:
	g_warning ("llvm symbol load failed: %s\n", err);
	g_free (err);
	return FALSE;
}

#else

int
mono_llvm_load (const char* bpath)
{
	return TRUE;
}

#endif /* MONO_LLVM_LOADED */

