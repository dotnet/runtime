/*
 * Handle the differences between the llvm backend beeing embedded
 * or loaded at runtime.
 */
#ifdef MONO_LLVM_LOADED

int mono_llvm_load (const char* bpath) MONO_INTERNAL;

#ifdef MONO_LLVM_IN_MINI
typedef void (*MonoLLVMVoidFunc)(void);
typedef void (*MonoLLVMCFGFunc)(MonoCompile *cfg);
typedef void (*MonoLLVMEmitCallFunc)(MonoCompile *cfg, MonoCallInst *call);
typedef void (*MonoLLVMCreateAotFunc)(const char *got_symbol);
typedef void (*MonoLLVMEmitAotFunc)(const char *filename, int got_size);

static MonoLLVMVoidFunc mono_llvm_init_fptr;
static MonoLLVMVoidFunc mono_llvm_cleanup_fptr;
static MonoLLVMCFGFunc mono_llvm_emit_method_fptr;
static MonoLLVMEmitCallFunc mono_llvm_emit_call_fptr;
static MonoLLVMCreateAotFunc mono_llvm_create_aot_module_fptr;
static MonoLLVMEmitAotFunc mono_llvm_emit_aot_module_fptr;
static MonoLLVMCFGFunc mono_llvm_check_method_supported_fptr;

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
mono_llvm_create_aot_module (const char *got_symbol)
{
	g_assert (mono_llvm_create_aot_module_fptr);
	mono_llvm_create_aot_module_fptr (got_symbol);
}

void
mono_llvm_emit_aot_module (const char *filename, int got_size)
{
	g_assert (mono_llvm_emit_aot_module_fptr);
	mono_llvm_emit_aot_module_fptr (filename, got_size);
}

void
mono_llvm_check_method_supported (MonoCompile *cfg)
{
	mono_llvm_check_method_supported_fptr (cfg);
}

static MonoDl*
try_llvm_load (char *dir, char **err)
{
	gpointer iter;
	MonoDl *llvm_lib;
	char *path;
	iter = NULL;
	*err = NULL;
	while ((path = mono_dl_build_path (dir, "mono-llvm", &iter))) {
		g_free (*err);
		llvm_lib = mono_dl_open (path, MONO_DL_LAZY, err);
		g_free (path);
		if (llvm_lib)
			return llvm_lib;
	}
	return NULL;
}

int
mono_llvm_load (const char* bpath)
{
	MonoDl *llvm_lib = NULL;
	char *err;
	char buf [4096];
	int binl;
	binl = readlink ("/proc/self/exe", buf, sizeof (buf)-1);
	if (binl != -1) {
		char *base;
		char *name;
		buf [binl] = 0;
		base = g_path_get_dirname (buf);
		name = g_strdup_printf ("%s/.libs", base);
		g_free (base);
		err = NULL;
		llvm_lib = try_llvm_load (name, &err);
		g_free (name);
	}
	if (!llvm_lib) {
		llvm_lib = try_llvm_load (NULL, &err);
		if (!llvm_lib) {
			g_warning ("llvm load failed: %s\n", err);
			g_free (err);
			return FALSE;
		}
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
	return TRUE;
symbol_error:
	g_warning ("llvm symbol load failed: %s\n", err);
	g_free (err);
	return FALSE;
}

#endif

#else
#define mono_llvm_load(bpath) TRUE
#endif /* MONO_LLVM_LOADED */

