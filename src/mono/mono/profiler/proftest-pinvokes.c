#include <config.h>

#include <stdio.h>

#ifdef __cplusplus
extern "C" {
#endif


#ifdef WIN32
#define STDCALL __stdcall
#else
#define STDCALL
#endif

#if defined(WIN32) && defined (_MSC_VER)
#define LIBTEST_API __declspec(dllexport)
#elif defined(__GNUC__)
#define LIBTEST_API  __attribute__ ((__visibility__ ("default")))
#else
#define LIBTEST_API
#endif

typedef void (STDCALL *fn_ptr) (void);

LIBTEST_API void STDCALL
test_reverse_pinvoke (fn_ptr p);

#ifdef __cplusplus
}
#endif



void STDCALL
test_reverse_pinvoke (fn_ptr p)
{
	printf ("testfunc called\n");
	p ();
}
