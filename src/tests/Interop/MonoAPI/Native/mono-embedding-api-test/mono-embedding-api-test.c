// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#include <assert.h>
#include <ctype.h>
#include <stdio.h>
#include <stddef.h>
#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <errno.h>
#include <time.h>
#include <math.h>
#include <setjmp.h>
#include <signal.h>
#ifndef WIN32
#include <unistd.h>
#endif

#ifndef HOST_WIN32
#include <dlfcn.h>
#endif

#ifdef WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif

#ifndef WIN32
#define S_OK 0x0
#endif

#ifdef __cplusplus
extern "C" {
#endif

#ifdef WIN32
#define STDCALL __stdcall
#else
#define STDCALL
#define __thiscall /* nothing */
#endif

#ifdef WIN32
extern __declspec(dllimport) void __stdcall CoTaskMemFree(void *ptr);
#endif

typedef int (STDCALL *SimpleDelegate) (int a);

#if defined(WIN32) && defined (_MSC_VER)
#define LIBTEST_API __declspec(dllexport)
#elif defined(__GNUC__)
#define LIBTEST_API  __attribute__ ((__visibility__ ("default")))
#else
#define LIBTEST_API
#endif

#define FALSE                0
#define TRUE                 1

typedef void *         gpointer;

static void
g_usleep (unsigned long micros)
{
#ifdef WIN32
	Sleep (micros/1000);
#else
	usleep (micros);
#endif
}

static gpointer
g_malloc (size_t x)
{
	return malloc (x);
}

static gpointer
g_malloc0 (size_t x)
{
	return calloc (1, x);
}

static void
g_free (void *ptr)
{
	free (ptr);
}

static void marshal_free (void *ptr)
{
#ifdef WIN32
	CoTaskMemFree (ptr);
#else
	g_free (ptr);
#endif
}

static void* marshal_alloc (size_t size)
{
#ifdef WIN32
	return CoTaskMemAlloc (size);
#else
	return g_malloc (size);
#endif
}

static char* marshal_strdup (const char *str)
{
#ifdef WIN32
	if (!str)
		return NULL;

	size_t n = strlen (str) + 1;
	char *buf = (char *) CoTaskMemAlloc (n);
	strncpy_s (buf, n, str, n - 1);
	buf[n] = 0;
	return buf;
#else
	return strdup (str);
#endif
}

static char *libtest_api_coreclr_name;

static uint8_t
mono_test_init_symbols (void);

LIBTEST_API uint8_t
libtest_initialize_runtime_symbols (const char *libcoreclr_name)
{
	libtest_api_coreclr_name = marshal_strdup (libcoreclr_name);
	return mono_test_init_symbols();
}
#ifdef WIN32
// Copied from eglib gmodule-win32.c
#if HAVE_API_SUPPORT_WIN32_ENUM_PROCESS_MODULES
static gpointer
w32_find_symbol (const char *symbol_name)
{
	HMODULE *modules;
	DWORD buffer_size = sizeof (HMODULE) * 1024;
	DWORD needed, i;

	modules = (HMODULE *) g_malloc (buffer_size);

	if (modules == NULL)
		return NULL;

	if (!EnumProcessModules (GetCurrentProcess (), modules,
				 buffer_size, &needed)) {
		g_free (modules);
		return NULL;
	}

	/* check whether the supplied buffer was too small, realloc, retry */
	if (needed > buffer_size) {
		g_free (modules);

		buffer_size = needed;
		modules = (HMODULE *) g_malloc (buffer_size);

		if (modules == NULL)
			return NULL;

		if (!EnumProcessModules (GetCurrentProcess (), modules,
					 buffer_size, &needed)) {
			g_free (modules);
			return NULL;
		}
	}

	for (i = 0; i < needed / sizeof (HANDLE); i++) {
		gpointer proc = (gpointer)(intptr_t)GetProcAddress (modules [i], symbol_name);
		if (proc != NULL) {
			g_free (modules);
			return proc;
		}
	}

	g_free (modules);
	return NULL;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_ENUM_PROCESS_MODULES
static gpointer
w32_find_symbol (const char *symbol_name)
{
	SetLastError (ERROR_NOT_SUPPORTED);
	return NULL;
}
#endif
#endif/*WIN32*/

#ifdef WIN32
static HMODULE libcoreclr_module;
#else
static void* libcoreclr_module;
#endif

/* Searches for mono symbols in all loaded modules */
static gpointer
lookup_mono_symbol (const char *symbol_name)
{
#ifndef HOST_WIN32
	if (!libcoreclr_module)
		libcoreclr_module = dlopen (libtest_api_coreclr_name, RTLD_LAZY);
	return dlsym (/*RTLD_DEFAULT*/ libcoreclr_module, symbol_name);
#else
	if (!libcoreclr_module)
		libcoreclr_module = GetModuleHandle (NULL);
	gpointer symbol = NULL;
	symbol = (gpointer)(intptr_t)GetProcAddress(libcoreclr_module, symbol_name);
	if (symbol)
		return symbol;
	return w32_find_symbol (symbol_name);
#endif
}

typedef void (*MonoFtnPtrEHCallback) (uint32_t gchandle);

static int sym_inited = 0;

/* MONO_API functions that aren't in public headers */
static void (*sym_mono_install_ftnptr_eh_callback) (MonoFtnPtrEHCallback);
static void (*sym_mono_threads_exit_gc_safe_region_unbalanced) (gpointer, gpointer *);

static void (*null_function_ptr) (void);

static void (*sym_mono_threads_exit_gc_unsafe_region) (void*, void*);

static void* (*sym_mono_threads_enter_gc_unsafe_region) (void** stackdata);

#include "api-types.h"

#define MONO_API_FUNCTION(ret,name,args) static ret (*sym_ ## name) args;
#include "api-functions.h"
#undef MONO_API_FUNCTION

// FIXME use runtime headers
#define MONO_BEGIN_EFRAME { void *__dummy; void *__region_cookie = mono_threads_enter_gc_unsafe_region ? mono_threads_enter_gc_unsafe_region (&__dummy) : NULL;
#define MONO_END_EFRAME if (mono_threads_exit_gc_unsafe_region) mono_threads_exit_gc_unsafe_region (__region_cookie, &__dummy); }


/*
 * mono_method_get_unmanaged_thunk tests
 */

/* thunks.cs:TestStruct */
typedef struct _TestStruct {
	int A;
	double B;
} TestStruct;

/**
 * test_method_thunk:
 *
 * @test_id: the test number
 * @test_method_handle: MonoMethod* of the C# test method
 * @create_object_method_handle: MonoMethod* of thunks.cs:Test.CreateObject
 */
LIBTEST_API int STDCALL
test_method_thunk (int test_id, gpointer test_method_handle, gpointer create_object_method_handle)
{
	int ret = 0;

	gpointer (*mono_method_get_unmanaged_thunk)(gpointer)
		= (gpointer (*)(gpointer))sym_mono_method_get_unmanaged_thunk;

	gpointer (*mono_string_new_wrapper)(const char *)
		= (gpointer (*)(const char *))sym_mono_string_new_wrapper;

	char *(*mono_string_to_utf8)(gpointer)
		= (char *(*)(gpointer))sym_mono_string_to_utf8;

	// FIXME use runtime headers
	gpointer (*mono_object_unbox)(gpointer)
		= (gpointer (*)(gpointer))sym_mono_object_unbox;

	// FIXME use runtime headers
	gpointer (*mono_threads_enter_gc_unsafe_region) (gpointer)
		= (gpointer (*)(gpointer))sym_mono_threads_enter_gc_unsafe_region;

	// FIXME use runtime headers
	void (*mono_threads_exit_gc_unsafe_region) (gpointer, gpointer)
		= (void (*)(gpointer, gpointer))sym_mono_threads_exit_gc_unsafe_region;



	gpointer test_method, ex = NULL;
	gpointer (STDCALL *CreateObject)(gpointer*);

	MONO_BEGIN_EFRAME;

	if (!mono_method_get_unmanaged_thunk) {
		ret = 1;
		goto done;
	}

	test_method =  mono_method_get_unmanaged_thunk (test_method_handle);
	if (!test_method) {
		ret = 2;
		goto done;
	}

	CreateObject = (gpointer (STDCALL *)(gpointer *))mono_method_get_unmanaged_thunk (create_object_method_handle);
	if (!CreateObject) {
		ret = 3;
		goto done;
	}


	switch (test_id) {

	case 0: {
		/* thunks.cs:Test.Test0 */
		void (STDCALL *F)(gpointer *) = (void (STDCALL *)(gpointer *))test_method;
		F (&ex);
		break;
	}

	case 1: {
		/* thunks.cs:Test.Test1 */
		int (STDCALL *F)(gpointer *) = (int (STDCALL *)(gpointer *))test_method;
		if (F (&ex) != 42) {
			ret = 4;
			goto done;
		}
		break;
	}

	case 2: {
		/* thunks.cs:Test.Test2 */
		gpointer (STDCALL *F)(gpointer, gpointer*) = (gpointer (STDCALL *)(gpointer, gpointer *))test_method;
		gpointer str = mono_string_new_wrapper ("foo");
		if (str != F (str, &ex)) {
			ret = 4;
			goto done;
		}
		break;
	}

	case 3: {
		/* thunks.cs:Test.Test3 */
		gpointer (STDCALL *F)(gpointer, gpointer, gpointer*);
		gpointer obj;
		gpointer str;

		F = (gpointer (STDCALL *)(gpointer, gpointer, gpointer *))test_method;
		obj = CreateObject (&ex);
		str = mono_string_new_wrapper ("bar");

		if (str != F (obj, str, &ex)) {
			ret = 4;
			goto done;
		}
		break;
	}

	case 4: {
		/* thunks.cs:Test.Test4 */
		int (STDCALL *F)(gpointer, gpointer, int, gpointer*);
		gpointer obj;
		gpointer str;

		F = (int (STDCALL *)(gpointer, gpointer, int, gpointer *))test_method;
		obj = CreateObject (&ex);
		str = mono_string_new_wrapper ("bar");

		if (42 != F (obj, str, 42, &ex)) {
			ret = 4;
			goto done;
		}

		break;
	}

	case 5: {
		/* thunks.cs:Test.Test5 */
		int (STDCALL *F)(gpointer, gpointer, int, gpointer*);
		gpointer obj;
		gpointer str;

		F = (int (STDCALL *)(gpointer, gpointer, int, gpointer *))test_method;
		obj = CreateObject (&ex);
		str = mono_string_new_wrapper ("bar");

		F (obj, str, 42, &ex);
		if (!ex) {
			ret = 4;
			goto done;
		}

		break;
	}

	case 6: {
		/* thunks.cs:Test.Test6 */
		int (STDCALL *F)(gpointer, uint8_t, int16_t, int32_t, int64_t, float, double,
				 gpointer, gpointer*);
		gpointer obj;
		gpointer str = mono_string_new_wrapper ("Test6");
		int res;

		F = (int (STDCALL *)(gpointer, uint8_t, int16_t, int32_t, int64_t, float, double, gpointer, gpointer *))test_method;
		obj = CreateObject (&ex);

		res = F (obj, 254, 32700, -245378, 6789600, 3.1415f, 3.1415, str, &ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (!res) {
			ret = 5;
			goto done;
		}

		break;
	}

	case 7: {
		/* thunks.cs:Test.Test7 */
		int64_t (STDCALL *F)(gpointer*) = (int64_t (STDCALL *)(gpointer *))test_method;
		if (F (&ex) != INT64_MAX) {
			ret = 4;
			goto done;
		}
		break;
	}

	case 8: {
		/* thunks.cs:Test.Test8 */
		void (STDCALL *F)(uint8_t*, int16_t*, int32_t*, int64_t*, float*, double*,
				 gpointer*, gpointer*);

		uint8_t a1;
		int16_t a2;
		int32_t a3;
		int64_t a4;
		float a5;
		double a6;
		gpointer a7;

		F = (void (STDCALL *)(uint8_t *, int16_t *, int32_t *, int64_t *, float *, double *,
			gpointer *, gpointer *))test_method;

		F (&a1, &a2, &a3, &a4, &a5, &a6, &a7, &ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (!(a1 == 254 &&
		      a2 == 32700 &&
		      a3 == -245378 &&
		      a4 == 6789600 &&
		      (fabs (a5 - 3.1415) < 0.001) &&
		      (fabs (a6 - 3.1415) < 0.001) &&
		      strcmp (mono_string_to_utf8 (a7), "Test8") == 0)){
				ret = 5;
				goto done;
			}

		break;
	}

	case 9: {
		/* thunks.cs:Test.Test9 */
		void (STDCALL *F)(uint8_t*, int16_t*, int32_t*, int64_t*, float*, double*,
			gpointer*, gpointer*);

		uint8_t a1;
		int16_t a2;
		int32_t a3;
		int64_t a4;
		float a5;
		double a6;
		gpointer a7;

		F = (void (STDCALL *)(uint8_t *, int16_t *, int32_t *, int64_t *, float *, double *,
			gpointer *, gpointer *))test_method;

		F (&a1, &a2, &a3, &a4, &a5, &a6, &a7, &ex);
		if (!ex) {
			ret = 4;
			goto done;
		}

		break;
	}

	case 10: {
		/* thunks.cs:Test.Test10 */
		void (STDCALL *F)(gpointer*, gpointer*);

		gpointer obj1, obj2;

		obj1 = obj2 = CreateObject (&ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		F = (void (STDCALL *)(gpointer *, gpointer *))test_method;

		F (&obj1, &ex);
		if (ex) {
			ret = 5;
			goto done;
		}

		if (obj1 == obj2) {
			ret = 6;
			goto done;
		}

		break;
	}

	case 100: {
		/* thunks.cs:TestStruct.Test0 */
		int (STDCALL *F)(gpointer*, gpointer*);

		gpointer obj;
		TestStruct *a1;
		int res;

		obj = CreateObject (&ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (!obj) {
			ret = 5;
			goto done;
		}

		a1 = (TestStruct *)mono_object_unbox (obj);
		if (!a1) {
			ret = 6;
			goto done;
		}

		a1->A = 42;
		a1->B = 3.1415;

		F = (int (STDCALL *)(gpointer *, gpointer *))test_method;

		res = F ((gpointer *)obj, &ex);
		if (ex) {
			ret = 7;
			goto done;
		}

		if (!res) {
			ret = 8;
			goto done;
		}

		/* check whether the call was really by value */
		if (a1->A != 42 || a1->B != 3.1415) {
			ret = 9;
			goto done;
		}

		break;
	}

	case 101: {
		/* thunks.cs:TestStruct.Test1 */
		void (STDCALL *F)(gpointer, gpointer*);

		TestStruct *a1;
		gpointer obj;

		obj = CreateObject (&ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (!obj) {
			ret = 5;
			goto done;
		}

		a1 = (TestStruct *)mono_object_unbox (obj);
		if (!a1) {
			ret = 6;
			goto done;
		}

		F = (void (STDCALL *)(gpointer, gpointer *))test_method;

		F (obj, &ex);
		if (ex) {
			ret = 7;
			goto done;
		}

		if (a1->A != 42) {
			ret = 8;
			goto done;
		}

		if (!(fabs (a1->B - 3.1415) < 0.001)) {
			ret = 9;
			goto done;
		}

		break;
	}

	case 102: {
		/* thunks.cs:TestStruct.Test2 */
		gpointer (STDCALL *F)(gpointer*);

		TestStruct *a1;
		gpointer obj;

		F = (gpointer (STDCALL *)(gpointer *))test_method;

		obj = F (&ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (!obj) {
			ret = 5;
			goto done;
		}

		a1 = (TestStruct *)mono_object_unbox (obj);

		if (a1->A != 42) {
			ret = 5;
			goto done;
		}

		if (!(fabs (a1->B - 3.1415) < 0.001)) {
			ret = 6;
			goto done;
		}

		break;
	}

	case 103: {
		/* thunks.cs:TestStruct.Test3 */
		void (STDCALL *F)(gpointer, gpointer*);

		TestStruct *a1;
		gpointer obj;

		obj = CreateObject (&ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (!obj) {
			ret = 5;
			goto done;
		}

		a1 = (TestStruct *)mono_object_unbox (obj);

		if (!a1) {
			ret = 6;
			goto done;
		}

		a1->A = 42;
		a1->B = 3.1415;

		F = (void (STDCALL *)(gpointer, gpointer *))test_method;

		F (obj, &ex);
		if (ex) {
			ret = 4;
			goto done;
		}

		if (a1->A != 1) {
			ret = 5;
			goto done;
		}

		if (a1->B != 17) {
			ret = 6;
			goto done;
		}

		break;
	}

	default:
		ret = 9;

	}
done:
	MONO_END_EFRAME;

	return ret;
}

static int call_managed_res;

static void
call_managed (gpointer arg)
{
	SimpleDelegate del = (SimpleDelegate)arg;

	call_managed_res = del (42);
}

LIBTEST_API int STDCALL
mono_test_marshal_thread_attach (SimpleDelegate del)
{
#ifdef WIN32
	return 43;
#else
	int res;
	pthread_t t;

	res = pthread_create (&t, NULL, (gpointer (*)(gpointer))call_managed, (gpointer)del);
	assert (res == 0);
	pthread_join (t, NULL);

	return call_managed_res;
#endif
}

typedef struct {
	char arr [4 * 1024];
} LargeStruct;

typedef int (STDCALL *LargeStructDelegate) (LargeStruct *s);

static void
call_managed_large_vt (gpointer arg)
{
	LargeStructDelegate del = (LargeStructDelegate)arg;
	LargeStruct s;

	call_managed_res = del (&s);
}

LIBTEST_API int STDCALL
mono_test_marshal_thread_attach_large_vt (SimpleDelegate del)
{
#ifdef WIN32
	return 43;
#else
	int res;
	pthread_t t;

	res = pthread_create (&t, NULL, (gpointer (*)(gpointer))call_managed_large_vt, (gpointer)del);
	assert (res == 0);
	pthread_join (t, NULL);

	return call_managed_res;
#endif
}

#ifndef WIN32

typedef void (*NativeToManagedExceptionRethrowFunc) (void);

void *mono_test_native_to_managed_exception_rethrow_thread (void *arg)
{
	NativeToManagedExceptionRethrowFunc func = (NativeToManagedExceptionRethrowFunc) arg;
	func ();
	return NULL;
}

LIBTEST_API void STDCALL
mono_test_native_to_managed_exception_rethrow (NativeToManagedExceptionRethrowFunc func)
{
	pthread_t t;
	pthread_create (&t, NULL, mono_test_native_to_managed_exception_rethrow_thread, (gpointer)func);
	pthread_join (t, NULL);
}
#endif

// SYM_LOOKUP(mono_runtime_invoke)
// expands to
//  sym_mono_runtime_invoke = lookup_mono_symbol ("mono_runtime_invoke");
#define SYM_LOOKUP(name) do {			\
	sym_##name = lookup_mono_symbol (#name);	\
	} while (0)

static uint8_t
mono_test_init_symbols (void)
{
	if (sym_inited)
		return 1;

	SYM_LOOKUP (mono_install_ftnptr_eh_callback);
	assert (sym_mono_install_ftnptr_eh_callback);
	SYM_LOOKUP (mono_gchandle_get_target);
	SYM_LOOKUP (mono_gchandle_new);
	SYM_LOOKUP (mono_gchandle_free);
	SYM_LOOKUP (mono_raise_exception);
	SYM_LOOKUP (mono_domain_unload);
	SYM_LOOKUP (mono_threads_exit_gc_safe_region_unbalanced);

	SYM_LOOKUP (mono_get_root_domain);
	SYM_LOOKUP (mono_domain_get);
	SYM_LOOKUP (mono_domain_set);
	SYM_LOOKUP (mono_domain_assembly_open);
	SYM_LOOKUP (mono_assembly_get_image);
	SYM_LOOKUP (mono_class_from_name);
	SYM_LOOKUP (mono_class_get_method_from_name);
	SYM_LOOKUP (mono_thread_attach);
	SYM_LOOKUP (mono_thread_detach);
	SYM_LOOKUP (mono_runtime_invoke);

	SYM_LOOKUP (mono_method_get_unmanaged_thunk);
	SYM_LOOKUP (mono_string_new_wrapper);
	SYM_LOOKUP (mono_string_to_utf8);
	SYM_LOOKUP (mono_object_unbox);

	SYM_LOOKUP (mono_threads_enter_gc_unsafe_region);
	SYM_LOOKUP (mono_threads_exit_gc_unsafe_region);

	sym_inited = 1;

	return ((void*)sym_mono_gchandle_new != NULL);

}

#ifndef TARGET_WASM

static jmp_buf test_jmp_buf;
static uint32_t test_gchandle;

static void
mono_test_longjmp_callback (uint32_t gchandle)
{
	test_gchandle = gchandle;
	longjmp (test_jmp_buf, 1);
}

typedef void (*VoidVoidCallback) (void);

LIBTEST_API void STDCALL
mono_test_setjmp_and_call (VoidVoidCallback managedCallback, intptr_t *out_handle)
{
	if (setjmp (test_jmp_buf) == 0) {
		*out_handle = 0;
		sym_mono_install_ftnptr_eh_callback (mono_test_longjmp_callback);
		managedCallback ();
		*out_handle = 0; /* Do not expect to return here */
	} else {
		sym_mono_install_ftnptr_eh_callback (NULL);
		*out_handle = test_gchandle;
	}
}

#endif

LIBTEST_API void STDCALL
mono_test_marshal_bstr (void *ptr)
{
}

static void (*mono_test_capture_throw_callback) (uint32_t gchandle, uint32_t *exception_out);

static void
mono_test_ftnptr_eh_callback (uint32_t gchandle)
{
	uint32_t exception_handle = 0;

	assert (gchandle != 0);
	MonoObject *exc = sym_mono_gchandle_get_target (gchandle);
	sym_mono_gchandle_free (gchandle);

	uint32_t handle = sym_mono_gchandle_new (exc, FALSE);
	mono_test_capture_throw_callback (handle, &exception_handle);
	sym_mono_gchandle_free (handle);

	assert (exception_handle != 0);
	exc = sym_mono_gchandle_get_target (exception_handle);
	sym_mono_gchandle_free (exception_handle);

	sym_mono_raise_exception ((MonoException*)exc);
	assert (((void)"mono_raise_exception should not return", 0));
}

LIBTEST_API void STDCALL
mono_test_setup_ftnptr_eh_callback (VoidVoidCallback managed_entry, void (*capture_throw_callback) (uint32_t, uint32_t *))
{
	mono_test_capture_throw_callback = capture_throw_callback;
	sym_mono_install_ftnptr_eh_callback (mono_test_ftnptr_eh_callback);
	managed_entry ();
}

LIBTEST_API void STDCALL
mono_test_cleanup_ftptr_eh_callback (void)
{
	sym_mono_install_ftnptr_eh_callback (NULL);
}

struct invoke_names {
	char *assm_name;
	char *name_space;
	char *name;
	char *meth_name;
};

static struct invoke_names *
make_invoke_names (const char *assm_name, const char *name_space, const char *name, const char *meth_name)
{
	struct invoke_names *names = (struct invoke_names*) marshal_alloc (sizeof (struct invoke_names));
	names->assm_name = marshal_strdup (assm_name);
	names->name_space = marshal_strdup (name_space);
	names->name = marshal_strdup (name);
	names->meth_name = marshal_strdup (meth_name);
	return names;
}

static void
destroy_invoke_names (struct invoke_names *n)
{
	marshal_free (n->assm_name);
	marshal_free (n->name_space);
	marshal_free (n->name);
	marshal_free (n->meth_name);
	marshal_free (n);
}

static void
test_invoke_by_name (struct invoke_names *names)
{
	MonoDomain *domain = sym_mono_domain_get ();
	MonoThread *thread = NULL;
	if (!domain) {
		thread = sym_mono_thread_attach (sym_mono_get_root_domain ());
	}
	domain = sym_mono_domain_get ();
	assert (domain);
	MonoAssembly *assm = sym_mono_domain_assembly_open (domain, names->assm_name);
	assert (assm);
	MonoImage *image = sym_mono_assembly_get_image (assm);
	MonoClass *klass = sym_mono_class_from_name (image, names->name_space, names->name);
	assert (klass);
	/* meth_name should be a static method that takes no arguments */
	MonoMethod *method = sym_mono_class_get_method_from_name (klass, names->meth_name, -1);
	assert (method);

	MonoObject *args[] = {NULL, };

	sym_mono_runtime_invoke (method, NULL, (void**)args, NULL);

	if (thread)
		sym_mono_thread_detach (thread);
}

#ifndef HOST_WIN32
static void*
invoke_foreign_thread (void* user_data)
{
	struct invoke_names *names = (struct invoke_names*)user_data;
        /*
         * Run a couple of times to check that attach/detach multiple
         * times from the same thread leaves it in a reasonable coop
         * thread state.
         */
        for (int i = 0; i < 5; ++i) {
                test_invoke_by_name (names);
                g_usleep (1000);
        }
	destroy_invoke_names (names);
	return NULL;
}

static void*
invoke_foreign_delegate (void *user_data)
{
	VoidVoidCallback del = (VoidVoidCallback)user_data;
	for (int i = 0; i < 5; ++i) {
		del ();
		g_usleep (1000);
	}
	return NULL;
}

#endif


LIBTEST_API mono_bool STDCALL
mono_test_attach_invoke_foreign_thread (const char *assm_name, const char *name_space, const char *name, const char *meth_name, VoidVoidCallback del)
{
#ifndef HOST_WIN32
	if (!del) {
		struct invoke_names *names = make_invoke_names (assm_name, name_space, name, meth_name);
		pthread_t t;
		int res = pthread_create (&t, NULL, invoke_foreign_thread, (void*)names);
		assert (res == 0);
		pthread_join (t, NULL);
		return 0;
	} else {
		pthread_t t;
		int res = pthread_create (&t, NULL, invoke_foreign_delegate, (void*)del);
		assert (res == 0);
		pthread_join (t, NULL);
		return 0;
	}
#else
	// TODO: Win32 version of this test
	return 1;
#endif
}

#ifndef HOST_WIN32
struct names_and_mutex {
	/* if del is NULL, use names, otherwise just call del */
	VoidVoidCallback del;
	struct invoke_names *names;
        /* mutex to coordinate test and foreign thread */
        pthread_mutex_t coord_mutex;
        pthread_cond_t coord_cond;
        /* mutex to block the foreign thread */
	pthread_mutex_t deadlock_mutex;
};

static void*
invoke_block_foreign_thread (void *user_data)
{
	// This thread calls into the runtime and then blocks. It should not
	// prevent the runtime from shutting down.
	struct names_and_mutex *nm = (struct names_and_mutex *)user_data;
	if (!nm->del) {
		test_invoke_by_name (nm->names);
	} else {
		nm->del ();
	}
        pthread_mutex_lock (&nm->coord_mutex);
        /* signal the test thread that we called the runtime */
        pthread_cond_signal (&nm->coord_cond);
        pthread_mutex_unlock (&nm->coord_mutex);

	pthread_mutex_lock (&nm->deadlock_mutex); // blocks forever
	fprintf(stderr, "did not expect to reach past deadlock\n");
	abort();
}
#endif

LIBTEST_API mono_bool STDCALL
mono_test_attach_invoke_block_foreign_thread (const char *assm_name, const char *name_space, const char *name, const char *meth_name, VoidVoidCallback del)
{
#ifndef HOST_WIN32
	struct names_and_mutex *nm = (struct names_and_mutex *)malloc (sizeof (struct names_and_mutex));
	nm->del = del;
	if (!del) {
		struct invoke_names *names = make_invoke_names (assm_name, name_space, name, meth_name);
		nm->names = names;
	} else {
		nm->names = NULL;
	}
	pthread_mutex_init (&nm->coord_mutex, NULL);
	pthread_cond_init (&nm->coord_cond, NULL);
	pthread_mutex_init (&nm->deadlock_mutex, NULL);

	pthread_mutex_lock (&nm->deadlock_mutex); // lock the mutex and never unlock it.
	pthread_t t;
	int res = pthread_create (&t, NULL, invoke_block_foreign_thread, (void*)nm);
	assert (res == 0);
	/* wait for the foreign thread to finish calling the runtime before
	 * detaching it and returning
	 */
	pthread_mutex_lock (&nm->coord_mutex);
	pthread_cond_wait (&nm->coord_cond, &nm->coord_mutex);
	pthread_mutex_unlock (&nm->coord_mutex);
	pthread_detach (t);
	return 0;
#else
	// TODO: Win32 version of this test
	return 1;
#endif
}

#ifdef __cplusplus
} // extern C
#endif
