#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <glib.h>
#include <gmodule.h>
#include <errno.h>
#include <time.h>
#include <math.h>
#include <setjmp.h>
#include <signal.h>
#include "../utils/mono-errno.h"
#include "../utils/mono-compiler.h"

#ifndef HOST_WIN32
#include <dlfcn.h>
#endif

#ifdef WIN32
#include <windows.h>
#include "initguid.h"
#else
#include <pthread.h>
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

#ifdef __GNUC__
#pragma GCC diagnostic ignored "-Wmissing-prototypes"
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

static void marshal_free (void *ptr)
{
#ifdef WIN32
	CoTaskMemFree (ptr);
#else
	g_free (ptr);
#endif
}

static void* marshal_alloc (gsize size)
{
#ifdef WIN32
	return CoTaskMemAlloc (size);
#else
	return g_malloc (size);
#endif
}

static void* marshal_alloc0 (gsize size)
{
#ifdef WIN32
	void* ptr = CoTaskMemAlloc (size);
	memset(ptr, 0, size);
	return ptr;
#else
	return g_malloc0 (size);
#endif
}

static char* marshal_strdup (const char *str)
{
#ifdef WIN32
	if (!str)
		return NULL;

	char *buf = (char *) CoTaskMemAlloc (strlen (str) + 1);
	return strcpy (buf, str);
#else
	return g_strdup (str);
#endif
}

static gunichar2* marshal_bstr_alloc(const gchar* str)
{
#ifdef WIN32
	gunichar2* temp = g_utf8_to_utf16 (str, -1, NULL, NULL, NULL);
	gunichar2* ret = SysAllocString (temp);
	g_free (temp);
	return ret;
#else
	gchar* ret = NULL;
	int slen = strlen (str);
	gunichar2* temp;
	/* allocate len + 1 utf16 characters plus 4 byte integer for length*/
	ret = (gchar *)g_malloc ((slen + 1) * sizeof(gunichar2) + sizeof(guint32));
	if (ret == NULL)
		return NULL;
	temp = g_utf8_to_utf16 (str, -1, NULL, NULL, NULL);
	memcpy (ret + sizeof(guint32), temp, slen * sizeof(gunichar2));
	* ((guint32 *) ret) = slen * sizeof(gunichar2);
	ret [4 + slen * sizeof(gunichar2)] = 0;
	ret [5 + slen * sizeof(gunichar2)] = 0;

	return (gunichar2*)(ret + 4);
#endif
}

#define marshal_new0(type,size)       ((type *) marshal_alloc0 (sizeof (type)* (size)))

LIBTEST_API int STDCALL
mono_cominterop_is_supported (void)
{
#if defined(TARGET_X86) || defined(TARGET_AMD64)
	return 1;
#endif
	return 0;
}

LIBTEST_API unsigned short* STDCALL
test_lpwstr_marshal (unsigned short* chars, int length)
{
	int i = 0;
	unsigned short *res;

	res = (unsigned short *)marshal_alloc (2 * (length + 1));

	// printf("test_lpwstr_marshal()\n");
	
	while ( i < length ) {
		// printf("X|%u|\n", chars[i]);
		res [i] = chars[i];
		i++;
	}

	res [i] = 0;

	return res;
}


LIBTEST_API void STDCALL
test_lpwstr_marshal_out (unsigned short** chars)
{
	int i = 0;
	const char abc[] = "ABC";
	glong len = strlen(abc);

	*chars = (unsigned short *)marshal_alloc (2 * (len + 1));
	
	while ( i < len ) {
		(*chars) [i] = abc[i];
		i++;
	}

	(*chars) [i] = 0;
}

typedef struct {
	int b;
	int a;
	int c;
} union_test_1_type;

LIBTEST_API int STDCALL  
mono_union_test_1 (union_test_1_type u1) {
	// printf ("Got values %d %d %d\n", u1.b, u1.a, u1.c);
	return u1.a + u1.b + u1.c;
}

LIBTEST_API int STDCALL  
mono_return_int (int a) {
	// printf ("Got value %d\n", a);
	return a;
}

LIBTEST_API float STDCALL  
mono_test_marshal_pass_return_float (float f) {
	return f + 1.0;
}

struct ss
{
	int i;
};

LIBTEST_API int STDCALL 
mono_return_int_ss (struct ss a) {
	// printf ("Got value %d\n", a.i);
	return a.i;
}

LIBTEST_API struct ss STDCALL
mono_return_ss (struct ss a) {
	// printf ("Got value %d\n", a.i);
	a.i++;
	return a;
}

struct sc1
{
	char c[1];
};

LIBTEST_API struct sc1 STDCALL
mono_return_sc1 (struct sc1 a) {
	// printf ("Got value %d\n", a.c[0]);
	a.c[0]++;
	return a;
}


struct sc3
{
	char c[3];
};

LIBTEST_API struct sc3 STDCALL 
mono_return_sc3 (struct sc3 a) {
	// printf ("Got values %d %d %d\n", a.c[0], a.c[1], a.c[2]);
	a.c[0]++;
	a.c[1] += 2;
	a.c[2] += 3;
	return a;
}

struct sc5
{
	char c[5];
};

LIBTEST_API struct sc5 STDCALL 
mono_return_sc5 (struct sc5 a) {
	// printf ("Got values %d %d %d %d %d\n", a.c[0], a.c[1], a.c[2], a.c[3], a.c[4]);
	a.c[0]++;
	a.c[1] += 2;
	a.c[2] += 3;
	a.c[3] += 4;
	a.c[4] += 5;
	return a;
}

union su
{
	int i1;
	int i2;
};

LIBTEST_API int STDCALL  
mono_return_int_su (union su a) {
	// printf ("Got value %d\n", a.i1);
	return a.i1;
}

struct FI {
	float f1;
	float f2;
	float f3;
};

struct NestedFloat {
	struct FI fi;
	float f4;
};

LIBTEST_API struct NestedFloat STDCALL
mono_return_nested_float (void)
{
	struct NestedFloat f;
	f.fi.f1 = 1.0;
	f.fi.f2 = 2.0;
	f.fi.f3 = 3.0;
	f.f4 = 4.0;
	return f;
}

struct Scalar4 {
	double val[4];
};

struct Rect {
	int x;
	int y;
	int width;
	int height;
};

LIBTEST_API char * STDCALL
mono_return_struct_4_double (void *ptr, struct Rect rect, struct Scalar4 sc4, int a, int b, int c)
{
	char *buffer = (char *)marshal_alloc (1024 * sizeof (char));
	sprintf (buffer, "sc4 = {%.1f, %.1f, %.1f, %.1f }, a=%x, b=%x, c=%x\n", (float) sc4.val [0], (float) sc4.val [1], (float) sc4.val [2], (float) sc4.val [3], a, b, c);
	return buffer;
}

LIBTEST_API int STDCALL  
mono_test_many_int_arguments (int a, int b, int c, int d, int e,
							  int f, int g, int h, int i, int j);
LIBTEST_API short STDCALL 
mono_test_many_short_arguments (short a, short b, short c, short d, short e,
								short f, short g, short h, short i, short j);
LIBTEST_API char STDCALL 
mono_test_many_char_arguments (char a, char b, char c, char d, char e,
							   char f, char g, char h, char i, char j);

LIBTEST_API int STDCALL 
mono_test_many_int_arguments (int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

LIBTEST_API short STDCALL 
mono_test_many_short_arguments (short a, short b, short c, short d, short e, short f, short g, short h, short i, short j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

LIBTEST_API char STDCALL 
mono_test_many_byte_arguments (char a, char b, char c, char d, char e, char f, char g, char h, char i, char j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

LIBTEST_API float STDCALL 
mono_test_many_float_arguments (float a, float b, float c, float d, float e, float f, float g, float h, float i, float j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

LIBTEST_API double STDCALL 
mono_test_many_double_arguments (double a, double b, double c, double d, double e, double f, double g, double h, double i, double j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

LIBTEST_API double STDCALL 
mono_test_split_double_arguments (double a, double b, float c, double d, double e)
{
	return a + b + c + d + e;
}

LIBTEST_API int STDCALL 
mono_test_puts_static (char *s)
{
	// printf ("TEST %s\n", s);
	return 1;
}

typedef int (STDCALL *SimpleDelegate3) (int a, int b);

LIBTEST_API int STDCALL 
mono_invoke_delegate (SimpleDelegate3 delegate)
{
	int res;

	// printf ("start invoke %p\n", delegate);

	res = delegate (2, 3);

	// printf ("end invoke\n");

	return res;
}

LIBTEST_API int STDCALL
mono_invoke_simple_delegate (SimpleDelegate d)
{
	return d (4);
}

LIBTEST_API int STDCALL  
mono_test_marshal_char (short a1)
{
	if (a1 == 'a')
		return 0;
	
	return 1;
}

LIBTEST_API void STDCALL
mono_test_marshal_char_array (gunichar2 *s)
{
	const char m[] = "abcdef";
	gunichar2* s2;
	glong len;

	s2 = g_utf8_to_utf16 (m, -1, NULL, &len, NULL);
	
	len = (len * 2) + 2;
	memcpy (s, s2, len);

	g_free (s2);
}

LIBTEST_API int STDCALL
mono_test_marshal_ansi_char_array (char *s)
{
	const char m[] = "abcdef";

	if (strncmp ("qwer", s, 4))
		return 1;

	memcpy (s, m, sizeof (m));
	return 0;
}

LIBTEST_API int STDCALL
mono_test_marshal_unicode_char_array (gunichar2 *s)
{
	const char m[] = "abcdef";
	const char expected[] = "qwer";
	gunichar2 *s1, *s2;
	glong len1, len2;

	s1 = g_utf8_to_utf16 (m, -1, NULL, &len1, NULL);
	s2 = g_utf8_to_utf16 (expected, -1, NULL, &len2, NULL);
	len1 = (len1 * 2);
	len2 = (len2 * 2);

	if (memcmp (s, s2, len2))
		return 1;

	memcpy (s, s1, len1);
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_empty_pinvoke (int i)
{
	return i;
}

LIBTEST_API int STDCALL  
mono_test_marshal_bool_byref (int a, int *b, int c)
{
    int res = *b;

	*b = 1;

	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bool_in_as_I1_U1 (char bTrue, char bFalse)
{
	if (!bTrue)
                return 1;
	if (bFalse)
                return 2;
        return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bool_out_as_I1_U1 (char* bTrue, char* bFalse)
{
        if (!bTrue || !bFalse)
		return 3;

	*bTrue = 1;
	*bFalse = 0;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bool_ref_as_I1_U1 (char* bTrue, char* bFalse)
{
	if (!bTrue || !bFalse)
                return 4;

	if (!(*bTrue))
                return 5;
        if (*bFalse)
                return 6;

	*bFalse = 1;
        *bTrue = 0;

	return 0;
}

LIBTEST_API int STDCALL  
mono_test_marshal_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++)
		sum += a1 [i];
	
	return sum;
}

LIBTEST_API int STDCALL  
mono_test_marshal_inout_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++) {
		sum += a1 [i];
		a1 [i] = 50 - a1 [i];
	}
	
	return sum;
}

LIBTEST_API int /* cdecl */
mono_test_marshal_inout_array_cdecl (int *a1)
{
	return mono_test_marshal_inout_array (a1);
}

LIBTEST_API int STDCALL  
mono_test_marshal_out_array (int *a1)
{
	int i;

	for (i = 0; i < 50; i++) {
		a1 [i] = i;
	}
	
	return 0;
}

LIBTEST_API int STDCALL
mono_test_marshal_out_byref_array_out_size_param (int **out_arr, int *out_len)
{
	int *arr;
	int i, len;

	len = 4;
	arr = (gint32 *)marshal_alloc (sizeof (gint32) * len);
	for (i = 0; i < len; ++i)
		arr [i] = i;
	*out_arr = arr;
	*out_len = len;

	return 0;
}

LIBTEST_API int STDCALL
mono_test_marshal_out_lparray_out_size_param (int *arr, int *out_len)
{
	int i, len;

	len = 4;
	for (i = 0; i < len; ++i)
		arr [i] = i;
	*out_len = len;

	return 0;
}

LIBTEST_API int STDCALL  
mono_test_marshal_inout_nonblittable_array (gunichar2 *a1)
{
	int i, sum = 0;

	for (i = 0; i < 10; i++) {
		a1 [i] = 'F';
	}
	
	return sum;
}

typedef struct {
	int a;
	int b;
	int c;
	const char *d;
	gunichar2 *d2;
} simplestruct;

typedef struct {
	double x;
	double y;
} point;

LIBTEST_API simplestruct STDCALL 
mono_test_return_vtype (int i)
{
	simplestruct res;
	static gunichar2 test2 [] = { 'T', 'E', 'S', 'T', '2', 0 };

	res.a = 0;
	res.b = 1;
	res.c = 0;
	res.d = "TEST";
	res.d2 = test2;

	return res;
}

LIBTEST_API void STDCALL
mono_test_delegate_struct (void)
{
	// printf ("TEST\n");
}

typedef char* (STDCALL *ReturnStringDelegate) (const char *s);

LIBTEST_API char * STDCALL 
mono_test_return_string (ReturnStringDelegate func)
{
	char *res;

	// printf ("mono_test_return_string\n");

	res = func ("TEST");
	marshal_free (res);

	// printf ("got string: %s\n", res);
	return marshal_strdup ("12345");
}

typedef int (STDCALL *RefVTypeDelegate) (int a, simplestruct *ss, int b);

LIBTEST_API int STDCALL 
mono_test_ref_vtype (int a, simplestruct *ss, int b, RefVTypeDelegate func)
{
	if (a == 1 && b == 2 && ss->a == 0 && ss->b == 1 && ss->c == 0 &&
	    !strcmp (ss->d, "TEST1")) {
		ss->a = 1;
		ss->b = 0;
		ss->c = 1;
		ss->d = "TEST2";

		return func (a, ss, b);
	}

	return 1;
}

typedef int (STDCALL *OutVTypeDelegate) (int a, simplestruct *ss, int b);

LIBTEST_API int STDCALL 
mono_test_marshal_out_struct (int a, simplestruct *ss, int b, OutVTypeDelegate func)
{
	/* Check that the input pointer is ignored */
	ss->d = (const char *)0x12345678;

	func (a, ss, b);

	if (ss->a && ss->b && ss->c && !strcmp (ss->d, "TEST3"))
		return 0;
	else
		return 1;
}

typedef int (STDCALL *InVTypeDelegate) (int a, simplestruct *ss, int b);

LIBTEST_API int STDCALL 
mono_test_marshal_in_struct (int a, simplestruct *ss, int b, InVTypeDelegate func)
{
	simplestruct ss2;
	int res;

	memcpy (&ss2, ss, sizeof (simplestruct));

	res = func (a, ss, b);
	if (res) {
		printf ("mono_test_marshal_in_struct () failed: %d\n", res);
		return 1;
	}

	/* Check that no modifications is made to the struct */
	if (ss2.a == ss->a && ss2.b == ss->b && ss2.c == ss->c && ss2.d == ss->d)
		return 0;
	else
		return 1;
}

typedef struct {
	int a;
	SimpleDelegate func, func2, func3;
} DelegateStruct;

LIBTEST_API DelegateStruct STDCALL 
mono_test_marshal_delegate_struct (DelegateStruct ds)
{
	DelegateStruct res;

	res.a = ds.func (ds.a) + ds.func2 (ds.a) + (ds.func3 == NULL ? 0 : 1);
	res.func = ds.func;
	res.func2 = ds.func2;
	res.func3 = NULL;

	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_byref_struct (simplestruct *ss, int a, int b, int c, char *d)
{
	gboolean res = (ss->a == a && ss->b == b && ss->c == c && strcmp (ss->d, d) == 0);

	marshal_free ((char*)ss->d);

	ss->a = !ss->a;
	ss->b = !ss->b;
	ss->c = !ss->c;
	ss->d = marshal_strdup ("DEF");

	return res ? 0 : 1;
}

typedef struct {
	int a;
	int b;
	int c;
	char *d;
	unsigned char e;
	double f;
	unsigned char g;
	guint64 h;
} simplestruct2;

LIBTEST_API int STDCALL 
mono_test_marshal_struct2 (simplestruct2 ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST") && 
	    ss.e == 99 && ss.f == 1.5 && ss.g == 42 && ss.h == (guint64)123)
		return 0;

	return 1;
}

/* on HP some of the struct should be on the stack and not in registers */
LIBTEST_API int STDCALL 
mono_test_marshal_struct2_2 (int i, int j, int k, simplestruct2 ss)
{
	if (i != 10 || j != 11 || k != 12)
		return 1;
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST") && 
	    ss.e == 99 && ss.f == 1.5 && ss.g == 42 && ss.h == (guint64)123)
		return 0;

	return 1;
}

LIBTEST_API int STDCALL  
mono_test_marshal_lpstruct (simplestruct *ss)
{
	if (ss->a == 0 && ss->b == 1 && ss->c == 0 &&
	    !strcmp (ss->d, "TEST"))
		return 0;

	return 1;
}

LIBTEST_API int STDCALL  
mono_test_marshal_lpstruct_blittable (point *p)
{
	if (p->x == 1.0 && p->y == 2.0)
		return 0;
	else
		return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_struct_array (simplestruct2 *ss)
{
	if (! (ss[0].a == 0 && ss[0].b == 1 && ss[0].c == 0 &&
		   !strcmp (ss[0].d, "TEST") && 
		   ss[0].e == 99 && ss[0].f == 1.5 && ss[0].g == 42 && ss[0].h == (guint64)123))
		return 1;

	if (! (ss[1].a == 0 && ss[1].b == 0 && ss[1].c == 0 &&
		   !strcmp (ss[1].d, "TEST2") && 
		   ss[1].e == 100 && ss[1].f == 2.5 && ss[1].g == 43 && ss[1].h == (guint64)124))
		return 1;

	return 0;
}

typedef struct long_align_struct {
	gint32 a;
	gint64 b;
	gint64 c;
} long_align_struct;

LIBTEST_API int STDCALL 
mono_test_marshal_long_align_struct_array (long_align_struct *ss)
{
	return ss[0].a + ss[0].b + ss[0].c + ss[1].a + ss[1].b + ss[1].c;
}

LIBTEST_API simplestruct2 * STDCALL 
mono_test_marshal_class (int i, int j, int k, simplestruct2 *ss, int l)
{
	simplestruct2 *res;

	if (!ss)
		return NULL;

	if (i != 10 || j != 11 || k != 12 || l != 14)
		return NULL;
	if (! (ss->a == 0 && ss->b == 1 && ss->c == 0 &&
		   !strcmp (ss->d, "TEST") && 
		   ss->e == 99 && ss->f == 1.5 && ss->g == 42 && ss->h == (guint64)123))
		return NULL;

	res = marshal_new0 (simplestruct2, 1);
	memcpy (res, ss, sizeof (simplestruct2));
	res->d = marshal_strdup ("TEST");
	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_byref_class (simplestruct2 **ssp)
{
	simplestruct2 *ss = *ssp;
	simplestruct2 *res;
	
	if (! (ss->a == 0 && ss->b == 1 && ss->c == 0 &&
		   !strcmp (ss->d, "TEST") && 
		   ss->e == 99 && ss->f == 1.5 && ss->g == 42 && ss->h == (guint64)123))
		return 1;

	res = marshal_new0 (simplestruct2, 1);
	memcpy (res, ss, sizeof (simplestruct2));
	res->d = marshal_strdup ("TEST-RES");

	*ssp = res;
	return 0;
}

MONO_DISABLE_WARNING (4172) // returning address of local

static void *
get_sp (void)
{
	int i;
	void *p;

	/* Yes, this is correct, we are only trying to determine the value of the stack here */
	p = &i;
	return p;
}

MONO_RESTORE_WARNING

LIBTEST_API int STDCALL 
reliable_delegate (int a)
{
	return a;
}

/*
 * Checks whether get_sp() works as expected. It doesn't work with gcc-2.95.3 on linux.
 */
static gboolean
is_get_sp_reliable (void)
{
	void *sp1, *sp2;

	reliable_delegate(1);
	sp1 = get_sp();
	reliable_delegate(1);
	sp2 = get_sp();
	return sp1 == sp2;
} 

LIBTEST_API int STDCALL 
mono_test_marshal_delegate (SimpleDelegate delegate)
{
	void *sp1, *sp2;

	/* Check that the delegate wrapper is stdcall */
	delegate (2);
	sp1 = get_sp ();
	delegate (2);
	sp2 = get_sp ();
	if (is_get_sp_reliable())
		g_assert (sp1 == sp2);

	return delegate (2);
}

static int STDCALL inc_cb (int i)
{
	return i + 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_out_delegate (SimpleDelegate *delegate)
{
	*delegate = inc_cb;

	return 0;
}

LIBTEST_API SimpleDelegate STDCALL 
mono_test_marshal_return_delegate (SimpleDelegate delegate)
{
	return delegate;
}

typedef int (STDCALL *DelegateByrefDelegate) (void *);

LIBTEST_API int STDCALL
mono_test_marshal_delegate_ref_delegate (DelegateByrefDelegate del)
{
	int (STDCALL *ptr) (int i);

	del (&ptr);

	return ptr (54);
}

static int STDCALL
return_plus_one (int i)
{
	return i + 1;
}

LIBTEST_API SimpleDelegate STDCALL 
mono_test_marshal_return_delegate_2 (void)
{
	return return_plus_one;
}

typedef simplestruct (STDCALL *SimpleDelegate2) (simplestruct ss);

static gboolean
is_utf16_equals (gunichar2 *s1, const char *s2)
{
	char *s;
	int res;

	s = g_utf16_to_utf8 (s1, -1, NULL, NULL, NULL);
	res = strcmp (s, s2);
	g_free (s);

	return res == 0;
}

LIBTEST_API int STDCALL
mono_test_marshal_struct (simplestruct ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST") && is_utf16_equals (ss.d2, "OK"))
		return 0;

	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_delegate2 (SimpleDelegate2 delegate)
{
	simplestruct ss, res;

	ss.a = 0;
	ss.b = 1;
	ss.c = 0;
	ss.d = "TEST";
	ss.d2 = g_utf8_to_utf16 ("TEST2", -1, NULL, NULL, NULL); 

	res = delegate (ss);
	if (! (res.a && !res.b && res.c && !strcmp (res.d, "TEST-RES") && is_utf16_equals (res.d2, "TEST2-RES")))
		return 1;

	return 0;
}

typedef simplestruct* (STDCALL *SimpleDelegate4) (simplestruct *ss);

LIBTEST_API int STDCALL 
mono_test_marshal_delegate4 (SimpleDelegate4 delegate)
{
	simplestruct ss;
	simplestruct *res;

	ss.a = 0;
	ss.b = 1;
	ss.c = 0;
	ss.d = "TEST";

	/* Check argument */
	res = delegate (&ss);
	if (!res)
		return 1;

	/* Check return value */
	if (! (!res->a && res->b && !res->c && !strcmp (res->d, "TEST")))
		return 2;

	/* Check NULL argument and NULL result */
	res = delegate (NULL);
	if (res)
		return 3;

	return 0;
}

typedef int (STDCALL *SimpleDelegate5) (simplestruct **ss);

LIBTEST_API int STDCALL 
mono_test_marshal_delegate5 (SimpleDelegate5 delegate)
{
	simplestruct ss;
	int res;
	simplestruct *ptr;

	ss.a = 0;
	ss.b = 1;
	ss.c = 0;
	ss.d = "TEST";

	ptr = &ss;

	res = delegate (&ptr);
	if (res != 0)
		return 1;

	if (!(ptr->a && !ptr->b && ptr->c && !strcmp (ptr->d, "RES")))
		return 2;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_delegate6 (SimpleDelegate5 delegate)
{
	delegate (NULL);
	return 0;
}

typedef int (STDCALL *SimpleDelegate7) (simplestruct **ss);

LIBTEST_API int STDCALL 
mono_test_marshal_delegate7 (SimpleDelegate7 delegate)
{
	int res;
	simplestruct *ptr;

	/* Check that the input pointer is ignored */
	ptr = (simplestruct *)0x12345678;

	res = delegate (&ptr);
	if (res != 0)
		return 1;

	if (!(ptr->a && !ptr->b && ptr->c && !strcmp (ptr->d, "RES")))
		return 2;

	return 0;
}

typedef int (STDCALL *InOutByvalClassDelegate) (simplestruct *ss);

LIBTEST_API int STDCALL 
mono_test_marshal_inout_byval_class_delegate (InOutByvalClassDelegate delegate)
{
	int res;
	simplestruct ss;

	ss.a = FALSE;
	ss.b = TRUE;
	ss.c = FALSE;
	ss.d = g_strdup_printf ("%s", "FOO");

	res = delegate (&ss);
	if (res != 0)
		return 1;

	if (!(ss.a && !ss.b && ss.c && !strcmp (ss.d, "RES")))
		return 2;

	return 0;
}

typedef int (STDCALL *SimpleDelegate8) (gunichar2 *s);

LIBTEST_API int STDCALL 
mono_test_marshal_delegate8 (SimpleDelegate8 delegate, gunichar2 *s)
{
	return delegate (s);
}

typedef int (STDCALL *return_int_fnt) (int i);
typedef int (STDCALL *SimpleDelegate9) (return_int_fnt d);

LIBTEST_API int STDCALL 
mono_test_marshal_delegate9 (SimpleDelegate9 delegate, gpointer ftn)
{
	return delegate ((return_int_fnt)ftn);
}

static int STDCALL 
return_self (int i)
{
	return i;
}

LIBTEST_API int STDCALL 
mono_test_marshal_delegate10 (SimpleDelegate9 delegate)
{
	return delegate (return_self);
}

typedef int (STDCALL *PrimitiveByrefDelegate) (int *i);

LIBTEST_API int STDCALL 
mono_test_marshal_primitive_byref_delegate (PrimitiveByrefDelegate delegate)
{
	int i = 1;

	int res = delegate (&i);
	if (res != 0)
		return res;

	if (i != 2)
		return 2;

	return 0;
}

typedef int (STDCALL *return_int_delegate) (int i);

typedef return_int_delegate (STDCALL *ReturnDelegateDelegate) (void);

LIBTEST_API int STDCALL 
mono_test_marshal_return_delegate_delegate (ReturnDelegateDelegate d)
{
	return (d ()) (55);
}

typedef int (STDCALL *VirtualDelegate) (int);

LIBTEST_API int STDCALL
mono_test_marshal_virtual_delegate (VirtualDelegate del)
{
	return del (42);
}

typedef char* (STDCALL *IcallDelegate) (const char *);
LIBTEST_API int STDCALL
mono_test_marshal_icall_delegate (IcallDelegate del)
{
	char *res = del ("ABC");
	return strcmp (res, "ABC") == 0 ? 0 : 1;
}

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";

	if (strcmp (s, "ABCD") != 0)
		return 1;
	memcpy(s, m, n);
	s [n] = '\0';
	return 0;
}

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder_append (char *s, int length)
{
	const char out_sentinel[] = "CSHARP_";
	const char out_len = strlen (out_sentinel);

	for (int i=0; i < length; i++) {
		s [i] = out_sentinel [i % out_len];
	}

	s [length] = '\0';


	return 0;
}

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder_default (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";

	memcpy(s, m, n);
	s [n] = '\0';
	return 0;
}

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder_unicode (gunichar2 *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";
	gunichar2* s2;
	glong len;

	s2 = g_utf8_to_utf16 (m, -1, NULL, &len, NULL);
	
	len = (len * 2) + 2;
	if (len > (n * 2))
		len = n * 2;
	memcpy (s, s2, len);

	g_free (s2);

	return 0;
}

LIBTEST_API void STDCALL
mono_test_marshal_stringbuilder_out (char **s)
{
	const char m[] = "This is my message.  Isn't it nice?";
	char *str;

	str = (char *)marshal_alloc (strlen (m) + 1);
	memcpy (str, m, strlen (m) + 1);
	
	*s = str;
}

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder_out_unicode (gunichar2 **s)
{
	const char m[] = "This is my message.  Isn't it nice?";
	gunichar2 *s2;
	glong len;

	s2 = g_utf8_to_utf16 (m, -1, NULL, &len, NULL);
	
	len = (len * 2) + 2;
	*s = (gunichar2 *)marshal_alloc (len);
	memcpy (*s, s2, len);

	g_free (s2);

	return 0;
}

LIBTEST_API int STDCALL
mono_test_marshal_stringbuilder_ref (char **s)
{
	const char m[] = "This is my message.  Isn't it nice?";
	char *str;

	if (strcmp (*s, "ABC"))
		return 1;

	str = (char *)marshal_alloc (strlen (m) + 1);
	memcpy (str, m, strlen (m) + 1);
	
	*s = str;
	return 0;
}

LIBTEST_API void STDCALL  
mono_test_marshal_stringbuilder_utf16_tolower (short *s, int n)
{
	for (int i = 0; i < n; i++)
		s[i] = tolower(s[i]);
}


#ifdef __GNUC__
#pragma GCC diagnostic push
#pragma GCC diagnostic ignored "-Wc++-compat"
#endif

/*
* Standard C and C++ doesn't allow empty structs, empty structs will always have a size of 1 byte.
* GCC have an extension to allow empty structs, https://gcc.gnu.org/onlinedocs/gcc/Empty-Structures.html.
* This cause a little dilemma since runtime build using none GCC compiler will not be compatible with
* GCC build C libraries and the other way around. On platforms where empty structs has size of 1 byte
* it must be represented in call and cannot be dropped. On Windows x64 structs will always be represented in the call
* meaning that an empty struct must have a representation in the callee in order to correctly follow the ABI used by the
* C/C++ standard and the runtime.
*/
typedef struct {
#if !defined(__GNUC__) || defined(TARGET_WIN32)
    char a;
#endif
} EmptyStruct;

#ifdef __GNUC__
#pragma GCC diagnostic pop
#endif

LIBTEST_API int STDCALL 
mono_test_marshal_empty_string_array (char **array)
{
	return (array == NULL) ? 0 : 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_string_array (char **array)
{
	if (strcmp (array [0], "ABC"))
		return 1;
	if (strcmp (array [1], "DEF"))
		return 2;

	if (array [2] != NULL)
		return 3;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_byref_string_array (char ***array)
{
	if (*array == NULL)
		return 0;

	if (strcmp ((*array) [0], "Alpha"))
		return 2;
	if (strcmp ((*array) [1], "Beta"))
		return 2;
	if (strcmp ((*array) [2], "Gamma"))
		return 2;

	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_stringbuilder_array (char **array)
{
	if (strcmp (array [0], "ABC"))
		return 1;
	if (strcmp (array [1], "DEF"))
		return 2;

	strcpy (array [0], "DEF");
	strcpy (array [1], "ABC");

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_unicode_string_array (gunichar2 **array, char **array2)
{
	GError *gerror = NULL;
	char *s;
	
	s = g_utf16_to_utf8 (array [0], -1, NULL, NULL, &gerror);
	if (strcmp (s, "ABC")) {
		g_free (s);
		return 1;
	}
	else
		g_free (s);

	s = g_utf16_to_utf8 (array [1], -1, NULL, NULL, &gerror);
	if (strcmp (s, "DEF")) {
		g_free (s);
		return 2;
	}
	else
		g_free (s);

	if (strcmp (array2 [0], "ABC"))
		return 3;

	if (strcmp (array2 [1], "DEF")) 
		return 4;

	return 0;
}

/* this does not work on Redhat gcc 2.96 */
LIBTEST_API int STDCALL  
mono_test_empty_struct (int a, EmptyStruct es, int b)
{
	// printf ("mono_test_empty_struct %d %d\n", a, b);

	// Intel icc on ia64 passes 'es' in 2 registers
#if defined(__ia64) && defined(__INTEL_COMPILER)
	return 0;
#else
	if (a == 1 && b == 2)
		return 0;
	return 1;
#endif
}

LIBTEST_API EmptyStruct STDCALL
mono_test_return_empty_struct (int a)
{
	EmptyStruct s;

	memset (&s, 0, sizeof (s));

#if !(defined(__i386__) && defined(__clang__))
	/* https://bugzilla.xamarin.com/show_bug.cgi?id=58901 */
	g_assert (a == 42);
#endif

	return s;
}

typedef struct {
       char a[100];
} ByValStrStruct;

LIBTEST_API ByValStrStruct * STDCALL 
mono_test_byvalstr_gen (void)
{
	ByValStrStruct *ret;
       
	ret = (ByValStrStruct *)malloc (sizeof (ByValStrStruct));
	memset(ret, 'a', sizeof(ByValStrStruct)-1);
	ret->a[sizeof(ByValStrStruct)-1] = 0;

	return ret;
}

LIBTEST_API int STDCALL 
mono_test_byvalstr_check (ByValStrStruct* data, char* correctString)
{
	int ret;

	ret = strcmp(data->a, correctString);
	// printf ("T1: %s\n", data->a);
	// printf ("T2: %s\n", correctString);

	/* we need g_free because the allocation was performed by mono_test_byvalstr_gen */
	g_free (data);
	return (ret != 0);
}

typedef struct {
	guint16 a[4];
	int  flag;
} ByValStrStruct_Unicode;

LIBTEST_API int STDCALL 
mono_test_byvalstr_check_unicode (ByValStrStruct_Unicode *ref, int test)
{
	if (ref->flag != 0x1234abcd){
		printf ("overwritten data");
		return 1;
	}
	    
	if (test == 1 || test == 3){
		if (ref->a [0] != '1' ||
		    ref->a [1] != '2'   ||
		    ref->a [2] != '3')
			return 1;
		return 0;
	}
	if (test == 2){
		if (ref->a [0] != '1' ||
		    ref->a [1] != '2')
			return 1;
		return 0;
	}
	return 10;
}

LIBTEST_API int STDCALL 
NameManglingAnsi (char *data)
{
	return data [0] + data [1] + data [2];
}

LIBTEST_API int STDCALL 
NameManglingAnsiA (char *data)
{
	g_assert_not_reached ();
}

LIBTEST_API int STDCALL 
NameManglingAnsiW (char *data)
{
	g_assert_not_reached ();
}

LIBTEST_API int STDCALL 
NameManglingAnsi2A (char *data)
{
	return data [0] + data [1] + data [2];
}

LIBTEST_API int STDCALL 
NameManglingAnsi2W (char *data)
{
	g_assert_not_reached ();
}

LIBTEST_API int STDCALL 
NameManglingUnicode (char *data)
{
	g_assert_not_reached ();
}

LIBTEST_API int STDCALL 
NameManglingUnicodeW (gunichar2 *data)
{
	return data [0] + data [1] + data [2];
}

LIBTEST_API int STDCALL 
NameManglingUnicode2 (gunichar2 *data)
{
	return data [0] + data [1] + data [2];
}

LIBTEST_API int STDCALL 
NameManglingAutoW (char *data)
{
#ifdef WIN32
	return (data [0] + data [1] + data [2]) == 131 ? 0 : 1;
#else
	g_assert_not_reached ();
#endif
}

LIBTEST_API int STDCALL 
NameManglingAuto (char *data)
{
#ifndef WIN32
	return (data [0] + data [1] + data [2]) == 198 ? 0 : 1;
#else
	g_assert_not_reached ();
#endif
}

typedef int (STDCALL *intcharFunc)(const char*);

LIBTEST_API void STDCALL 
callFunction (intcharFunc f)
{
	f ("ABC");
}

typedef struct {
        const char* str;
        int i;
} SimpleObj;

LIBTEST_API int STDCALL 
class_marshal_test0 (SimpleObj *obj1)
{
	// printf ("class_marshal_test0 %s %d\n", obj1->str, obj1->i);

	if (strcmp(obj1->str, "T1"))
		return -1;
	if (obj1->i != 4)
		return -2;

	return 0;
}

LIBTEST_API int STDCALL 
class_marshal_test4 (SimpleObj *obj1)
{
	if (obj1)
		return -1;

	return 0;
}

LIBTEST_API void STDCALL
class_marshal_test1 (SimpleObj **obj1)
{
	SimpleObj *res = (SimpleObj *)malloc (sizeof (SimpleObj));

	res->str = marshal_strdup ("ABC");
	res->i = 5;

	*obj1 = res;
}

LIBTEST_API int STDCALL 
class_marshal_test2 (SimpleObj **obj1)
{
	// printf ("class_marshal_test2 %s %d\n", (*obj1)->str, (*obj1)->i);

	if (strcmp((*obj1)->str, "ABC"))
		return -1;
	if ((*obj1)->i != 5)
		return -2;

	return 0;
}

LIBTEST_API int STDCALL 
string_marshal_test0 (char *str)
{
	if (strcmp (str, "TEST0"))
		return -1;

	return 0;
}

LIBTEST_API void STDCALL
string_marshal_test1 (const char **str)
{
	*str = marshal_strdup ("TEST1");
}

LIBTEST_API int STDCALL 
string_marshal_test2 (char **str)
{
	// printf ("string_marshal_test2 %s\n", *str);

	if (strcmp (*str, "TEST1"))
		return -1;

	*str = marshal_strdup ("TEST2");

	return 0;
}

LIBTEST_API int STDCALL 
string_marshal_test3 (char *str)
{
	if (str)
		return -1;

	return 0;
}

typedef struct {
	int a;
	int b;
} BlittableClass;

LIBTEST_API BlittableClass* STDCALL 
TestBlittableClass (BlittableClass *vl)
{
	BlittableClass *res;

	// printf ("TestBlittableClass %d %d\n", vl->a, vl->b);

	if (vl) {
		vl->a++;
		vl->b++;

		res = marshal_new0 (BlittableClass, 1);
		memcpy (res, vl, sizeof (BlittableClass));
	} else {
		res = marshal_new0 (BlittableClass, 1);
		res->a = 42;
		res->b = 43;
	}

	return res;
}

typedef struct OSVERSIONINFO_STRUCT
{ 
	int a; 
	int b; 
} OSVERSIONINFO_STRUCT;

LIBTEST_API int STDCALL  
MyGetVersionEx (OSVERSIONINFO_STRUCT *osvi)
{

	// printf ("GOT %d %d\n", osvi->a, osvi->b);

	osvi->a += 1;
	osvi->b += 1;

	return osvi->a + osvi->b;
}

LIBTEST_API int STDCALL  
BugGetVersionEx (int a, int b, int c, int d, int e, int f, int g, int h, OSVERSIONINFO_STRUCT *osvi)
{

	// printf ("GOT %d %d\n", osvi->a, osvi->b);

	osvi->a += 1;
	osvi->b += 1;

	return osvi->a + osvi->b;
}

LIBTEST_API int STDCALL 
mono_test_marshal_point (point pt)
{
	// printf("point %g %g\n", pt.x, pt.y);
	if (pt.x == 1.25 && pt.y == 3.5)
		return 0;

	return 1;
}

typedef struct {
	int x;
	double y;
} mixed_point;

LIBTEST_API int STDCALL 
mono_test_marshal_mixed_point (mixed_point pt)
{
	// printf("mixed point %d %g\n", pt.x, pt.y);
	if (pt.x == 5 && pt.y == 6.75)
		return 0;

	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_mixed_point_2 (mixed_point *pt)
{
	if (pt->x != 5 || pt->y != 6.75)
		return 1;

	pt->x = 10;
	pt->y = 12.35;

	return 0;
}

LIBTEST_API int STDCALL  
marshal_test_ref_bool(int i, char *b1, short *b2, int *b3)
{
    int res = 1;
    if (*b1 != 0 && *b1 != 1)
        return 1;
    if (*b2 != 0 && *b2 != -1) /* variant_bool */
        return 1;
    if (*b3 != 0 && *b3 != 1)
        return 1;
    if (i == ((*b1 << 2) | (-*b2 << 1) | *b3))
        res = 0;
    *b1 = !*b1;
    *b2 = ~*b2;
    *b3 = !*b3;
    return res;
}

struct BoolStruct
{
    int i;
    char b1;
    short b2; /* variant_bool */
    int b3;
};

LIBTEST_API int STDCALL  
marshal_test_bool_struct(struct BoolStruct *s)
{
    int res = 1;
    if (s->b1 != 0 && s->b1 != 1)
        return 1;
    if (s->b2 != 0 && s->b2 != -1)
        return 1;
    if (s->b3 != 0 && s->b3 != 1)
        return 1;
    if (s->i == ((s->b1 << 2) | (-s->b2 << 1) | s->b3))
        res = 0;
    s->b1 = !s->b1;
    s->b2 = ~s->b2;
    s->b3 = !s->b3;
    return res;
}

typedef struct {
	gint64 l;
} LongStruct2;

typedef struct {
	int i;
	LongStruct2 l;
} LongStruct;

LIBTEST_API int STDCALL
mono_test_marshal_long_struct (LongStruct *s)
{
	return s->i + s->l.l;
}

LIBTEST_API void STDCALL
mono_test_last_error (int err)
{
#ifdef WIN32
	SetLastError (err);

	/*
	* Make sure argument register used calling SetLastError
	* get's cleaned before returning back to caller. This is done to ensure
	* we don't get a undetected failure if error is preserved in register
	* on return since we read back value directly when doing p/invoke with SetLastError = true
	* into first argument register and then pass it to Mono function setting value in TLS.
	* If there is a codegen bug reading last error or the code has been incorrectly eliminated
	* this test could still succeed since expected error code could be left in argument register.
	* Below code just do something that shouldn't touch last error and won't be optimized away
	* but will change the argument registers to something different than err.
	*/
	char buffer[256] = { 0 };
	char value[] = "Dummy";
	strncpy (buffer, value, G_N_ELEMENTS (value) - 1);
#else
	mono_set_errno (err);
#endif
}

LIBTEST_API int STDCALL 
mono_test_asany (void *ptr, int what)
{
	switch (what) {
	case 1:
		return (*(int*)ptr == 5) ? 0 : 1;
	case 2:
		return strcmp ((const char*)ptr, "ABC") == 0 ? 0 : 1;
	case 3: {
		simplestruct2 ss = *(simplestruct2*)ptr;

		if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST") && 
	    ss.e == 99 && ss.f == 1.5 && ss.g == 42 && ss.h == (guint64)123)
			return 0;
		else
			return 1;
	}
	case 4: {
		GError *gerror = NULL;
		char *s;

		s = g_utf16_to_utf8 ((const gunichar2 *)ptr, -1, NULL, NULL, &gerror);

		if (!s)
			return 1;

		if (!strcmp (s, "ABC")) {
			g_free (s);
			return 0;
		}
		else {
			g_free (s);
			return 1;
		}
	}
	case 5: {
		return (*(intptr_t*)ptr == 5) ? 0 : 1;
	}
	default:
		g_assert_not_reached ();
	}

	return 1;
}

typedef struct
{
	int i;
	int j;
	int k;
	char *s;
} AsAnyStruct;

LIBTEST_API int STDCALL 
mono_test_marshal_asany_in (void* ptr)
{
	AsAnyStruct *asAny = (AsAnyStruct *)ptr;
	int res = asAny->i + asAny->j + asAny->k;

	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_asany_inout (void* ptr)
{
	AsAnyStruct *asAny = (AsAnyStruct *)ptr;
	int res = asAny->i + asAny->j + asAny->k;

	marshal_free (asAny->s);

	asAny->i = 10;
	asAny->j = 20;
	asAny->k = 30;
	asAny->s = 0;

	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_asany_out (void* ptr)
{
	AsAnyStruct *asAny = (AsAnyStruct *)ptr;
	int res = asAny->i + asAny->j + asAny->k;

	asAny->i = 10;
	asAny->j = 20;
	asAny->k = 30;
	asAny->s = 0;

	return res;
}

/*
 * AMD64 marshalling tests.
 */

typedef struct amd64_struct1 {
	int i;
	int j;
	int k;
	int l;
} amd64_struct1;

LIBTEST_API amd64_struct1 STDCALL 
mono_test_marshal_amd64_pass_return_struct1 (amd64_struct1 s)
{
	s.i ++;
	s.j ++;
	s.k ++;
	s.l ++;

	return s;
}

LIBTEST_API amd64_struct1 STDCALL 
mono_test_marshal_amd64_pass_return_struct1_many_args (amd64_struct1 s, int i1, int i2, int i3, int i4, int i5, int i6, int i7, int i8)
{
	s.i ++;
	s.j ++;
	s.k ++;
	s.l += 1 + i1 + i2 + i3 + i4 + i5 + i6 + i7 + i8;

	return s;
}

typedef struct amd64_struct2 {
	int i;
	int j;
} amd64_struct2;

LIBTEST_API amd64_struct2 STDCALL 
mono_test_marshal_amd64_pass_return_struct2 (amd64_struct2 s)
{
	s.i ++;
	s.j ++;

	return s;
}

typedef struct amd64_struct3 {
	int i;
} amd64_struct3;

LIBTEST_API amd64_struct3 STDCALL 
mono_test_marshal_amd64_pass_return_struct3 (amd64_struct3 s)
{
	s.i ++;

	return s;
}

typedef struct amd64_struct4 {
	double d1, d2;
} amd64_struct4;

LIBTEST_API amd64_struct4 STDCALL 
mono_test_marshal_amd64_pass_return_struct4 (amd64_struct4 s)
{
	s.d1 ++;
	s.d2 ++;

	return s;
}

/*
 * IA64 marshalling tests.
 */
typedef struct test_struct5 {
	float d1, d2;
} test_struct5;

LIBTEST_API test_struct5 STDCALL 
mono_test_marshal_ia64_pass_return_struct5 (double d1, double d2, test_struct5 s, int i, double d3, double d4)
{
	s.d1 += d1 + d2 + i;
	s.d2 += d3 + d4 + i;

	return s;
}

typedef struct test_struct6 {
	double d1, d2;
} test_struct6;

LIBTEST_API test_struct6 STDCALL 
mono_test_marshal_ia64_pass_return_struct6 (double d1, double d2, test_struct6 s, int i, double d3, double d4)
{
	s.d1 += d1 + d2 + i;
	s.d2 += d3 + d4;

	return s;
}

static guint32 custom_res [2];

LIBTEST_API void* STDCALL
mono_test_marshal_pass_return_custom (int i, guint32 *ptr, int j)
{
	/* ptr will be freed by CleanupNative, so make a copy */
	custom_res [0] = 0; /* not allocated by AllocHGlobal */
	custom_res [1] = ptr [1];

	return &custom_res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_pass_out_custom (int i, guint32 **ptr, int j)
{
	custom_res [0] = 0;
	custom_res [1] = i + j + 10;

	*ptr = custom_res;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_pass_inout_custom (int i, guint32 *ptr, int j)
{
	ptr [0] = 0;
	ptr [1] = i + ptr [1] + j;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_pass_out_byval_custom (int i, guint32 *ptr, int j)
{
	return ptr == NULL ? 0 : 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_pass_byref_custom (int i, guint32 **ptr, int j)
{
	(*ptr)[1] += i + j;

	return 0;
}

LIBTEST_API void* STDCALL
mono_test_marshal_pass_return_custom2 (int i, guint32 *ptr, int j)
{
	g_assert_not_reached ();

	return NULL;
}

LIBTEST_API void* STDCALL
mono_test_marshal_pass_return_custom_null (int i, guint32 *ptr, int j)
{
	g_assert (ptr == NULL);

	return NULL;
}

typedef void *(STDCALL *PassReturnPtrDelegate) (void *ptr);

LIBTEST_API int STDCALL 
mono_test_marshal_pass_return_custom_in_delegate (PassReturnPtrDelegate del)
{
	guint32 buf [2];
	guint32 res;
	guint32 *ptr;

	buf [0] = 0;
	buf [1] = 10;

	ptr = (guint32 *)del (&buf);

	res = ptr [1];

#ifdef WIN32
	/* FIXME: Freed with FreeHGlobal */
#else
	g_free (ptr);
#endif

	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_pass_return_custom_null_in_delegate (PassReturnPtrDelegate del)
{
	void *ptr = del (NULL);

	return (ptr == NULL) ? 15 : 0;
}

typedef void (STDCALL *CustomOutParamDelegate) (void **pptr);

LIBTEST_API int STDCALL 
mono_test_marshal_custom_out_param_delegate (CustomOutParamDelegate del)
{
	void* pptr = (void*)del;

	del (&pptr);

	if(pptr != NULL)
		return 1;

	return 0;
}

typedef int (STDCALL *ReturnEnumDelegate) (int e);

LIBTEST_API int STDCALL 
mono_test_marshal_return_enum_delegate (ReturnEnumDelegate func)
{
	return func (1);
}

typedef struct {
	int a, b, c;
	gint64 d;
} BlittableStruct;
	
typedef BlittableStruct (STDCALL *SimpleDelegate10) (BlittableStruct ss);

LIBTEST_API int STDCALL 
mono_test_marshal_blittable_struct_delegate (SimpleDelegate10 delegate)
{
	BlittableStruct ss, res;

	ss.a = 1;
	ss.b = 2;
	ss.c = 3;
	ss.d = 55;

	res = delegate (ss);
	if (! ((res.a == -1) && (res.b == -2) && (res.c == -3) && (res.d == -55)))
		return 1;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_stdcall_name_mangling (int a, int b, int c)
{
        return a + b + c;
}

LIBTEST_API int
mono_test_stdcall_mismatch_1 (int a, int b, int c)
{
        return a + b + c;
}

LIBTEST_API int STDCALL
mono_test_stdcall_mismatch_2 (int a, int b, int c)
{
        return a + b + c;
}

/*
 * PASSING AND RETURNING SMALL STRUCTURES FROM DELEGATES TESTS
 */

typedef struct {
	int i;
} SmallStruct1;
	
typedef SmallStruct1 (STDCALL *SmallStructDelegate1) (SmallStruct1 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate1 (SmallStructDelegate1 delegate)
{
	SmallStruct1 ss, res;

	ss.i = 1;

	res = delegate (ss);
	if (! (res.i == -1))
		return 1;

	return 0;
}

typedef struct {
	gint16 i, j;
} SmallStruct2;
	
typedef SmallStruct2 (STDCALL *SmallStructDelegate2) (SmallStruct2 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate2 (SmallStructDelegate2 delegate)
{
	SmallStruct2 ss, res;

	ss.i = 2;
	ss.j = 3;

	res = delegate (ss);
	if (! ((res.i == -2) && (res.j == -3)))
		return 1;

	return 0;
}

typedef struct {
	gint16 i;
	gint8 j;
} SmallStruct3;
	
typedef SmallStruct3 (STDCALL *SmallStructDelegate3) (SmallStruct3 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate3 (SmallStructDelegate3 delegate)
{
	SmallStruct3 ss, res;

	ss.i = 1;
	ss.j = 2;

	res = delegate (ss);
	if (! ((res.i == -1) && (res.j == -2)))
		return 1;

	return 0;
}

typedef struct {
	gint16 i;
} SmallStruct4;
	
typedef SmallStruct4 (STDCALL *SmallStructDelegate4) (SmallStruct4 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate4 (SmallStructDelegate4 delegate)
{
	SmallStruct4 ss, res;

	ss.i = 1;

	res = delegate (ss);
	if (! (res.i == -1))
		return 1;

	return 0;
}

typedef struct {
	gint64 i;
} SmallStruct5;
	
typedef SmallStruct5 (STDCALL *SmallStructDelegate5) (SmallStruct5 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate5 (SmallStructDelegate5 delegate)
{
	SmallStruct5 ss, res;

	ss.i = 5;

	res = delegate (ss);
	if (! (res.i == -5))
		return 1;

	return 0;
}

typedef struct {
	int i, j;
} SmallStruct6;
	
typedef SmallStruct6 (STDCALL *SmallStructDelegate6) (SmallStruct6 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate6 (SmallStructDelegate6 delegate)
{
	SmallStruct6 ss, res;

	ss.i = 1;
	ss.j = 2;

	res = delegate (ss);
	if (! ((res.i == -1) && (res.j == -2)))
		return 1;

	return 0;
}

typedef struct {
	int i;
	gint16 j;
} SmallStruct7;
	
typedef SmallStruct7 (STDCALL *SmallStructDelegate7) (SmallStruct7 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate7 (SmallStructDelegate7 delegate)
{
	SmallStruct7 ss, res;

	ss.i = 1;
	ss.j = 2;

	res = delegate (ss);
	if (! ((res.i == -1) && (res.j == -2)))
		return 1;

	return 0;
}

typedef struct {
	float i;
} SmallStruct8;
	
typedef SmallStruct8 (STDCALL *SmallStructDelegate8) (SmallStruct8 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate8 (SmallStructDelegate8 delegate)
{
	SmallStruct8 ss, res;

	ss.i = 1.0;

	res = delegate (ss);
	if (! ((res.i == -1.0)))
		return 1;

	return 0;
}

typedef struct {
	double i;
} SmallStruct9;
	
typedef SmallStruct9 (STDCALL *SmallStructDelegate9) (SmallStruct9 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate9 (SmallStructDelegate9 delegate)
{
	SmallStruct9 ss, res;

	ss.i = 1.0;

	res = delegate (ss);
	if (! ((res.i == -1.0)))
		return 1;

	return 0;
}

typedef struct {
	float i, j;
} SmallStruct10;
	
typedef SmallStruct10 (STDCALL *SmallStructDelegate10) (SmallStruct10 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate10 (SmallStructDelegate10 delegate)
{
	SmallStruct10 ss, res;

	ss.i = 1.0;
	ss.j = 2.0;

	res = delegate (ss);
	if (! ((res.i == -1.0) && (res.j == -2.0)))
		return 1;

	return 0;
}

typedef struct {
	float i;
	int j;
} SmallStruct11;
	
typedef SmallStruct11 (STDCALL *SmallStructDelegate11) (SmallStruct11 ss);

LIBTEST_API int STDCALL 
mono_test_marshal_small_struct_delegate11 (SmallStructDelegate11 delegate)
{
	SmallStruct11 ss, res;

	ss.i = 1.0;
	ss.j = 2;

	res = delegate (ss);
	if (! ((res.i == -1.0) && (res.j == -2)))
		return 1;

	return 0;
}

typedef int (STDCALL *ArrayDelegate) (int i, char *j, void *arr);

LIBTEST_API int STDCALL 
mono_test_marshal_array_delegate (void *arr, int len, ArrayDelegate del)
{
	return del (len, NULL, arr);
}

typedef int (STDCALL *ArrayDelegateLong) (gint64 i, char *j, void *arr);

LIBTEST_API int STDCALL 
mono_test_marshal_array_delegate_long (void *arr, gint64 len, ArrayDelegateLong del)
{
	return del (len, NULL, arr);
}

LIBTEST_API int STDCALL 
mono_test_marshal_out_array_delegate (int *arr, int len, ArrayDelegate del)
{
	del (len, NULL, arr);

	if ((arr [0] != 1) || (arr [1] != 2))
		return 1;
	else
		return 0;
}

typedef gunichar2* (STDCALL *UnicodeStringDelegate) (gunichar2 *message);

LIBTEST_API int STDCALL 
mono_test_marshal_return_unicode_string_delegate (UnicodeStringDelegate del)
{
	const char m[] = "abcdef";
	gunichar2 *s2, *res;
	glong len;

	s2 = g_utf8_to_utf16 (m, -1, NULL, &len, NULL);

	res = del (s2);

	marshal_free (res);

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_out_string_array_delegate (char **arr, int len, ArrayDelegate del)
{
	del (len, NULL, arr);

	if (!strcmp (arr [0], "ABC") && !strcmp (arr [1], "DEF"))
		return 0;
	else
		return 1;
}

typedef int (*CdeclDelegate) (int i, int j);

LIBTEST_API int STDCALL 
mono_test_marshal_cdecl_delegate (CdeclDelegate del)
{
	int i;

	for (i = 0; i < 1000; ++i)
		del (1, 2);

	return 0;
}

typedef char** (STDCALL *ReturnStringArrayDelegate) (int i);

LIBTEST_API int STDCALL 
mono_test_marshal_return_string_array_delegate (ReturnStringArrayDelegate d)
{
	char **arr = d (2);
	int res;

	if (arr == NULL)
		return 3;

	if (strcmp (arr [0], "ABC") || strcmp (arr [1], "DEF"))
		res = 1;
	else
		res = 0;

	marshal_free (arr);

	return res;
}

typedef int (STDCALL *ByrefStringDelegate) (char **s);

LIBTEST_API int STDCALL 
mono_test_marshal_byref_string_delegate (ByrefStringDelegate d)
{
	char *s = (char*)"ABC";
	int res;

	res = d (&s);
	if (res != 0)
		return res;

	if (!strcmp (s, "DEF"))
		res = 0;
	else
		res = 2;

	marshal_free (s);

	return res;
}

LIBTEST_API int STDCALL 
add_delegate (int i, int j)
{
	return i + j;
}

LIBTEST_API gpointer STDCALL 
mono_test_marshal_return_fnptr (void)
{
	return (gpointer)&add_delegate;
}

LIBTEST_API int STDCALL 
mono_xr (int code)
{
	printf ("codigo %x\n", code);
	return code + 1234;
}

typedef struct {
	int handle;
} HandleRef;

LIBTEST_API HandleRef STDCALL 
mono_xr_as_handle (int code)
{
	HandleRef ref;

	memset (&ref, 0, sizeof (ref));

	return ref;
}
 
typedef struct {
	int   a;
	void *handle1;
	void *handle2;
	int   b;
} HandleStructs;

LIBTEST_API int STDCALL 
mono_safe_handle_struct_ref (HandleStructs *x)
{
	printf ("Dingus Ref! \n");
	printf ("Values: %d %d %p %p\n", x->a, x->b, x->handle1, x->handle2);
	if (x->a != 1234)
		return 1;
	if (x->b != 8743)
		return 2;

	if (x->handle1 != (void*) 0x7080feed)
		return 3;

	if (x->handle2 != (void*) 0x1234abcd)
		return 4;

	return 0xf00d;
}

LIBTEST_API int STDCALL 
mono_safe_handle_struct (HandleStructs x)
{
	printf ("Dingus Standard! \n");
	printf ("Values: %d %d %p %p\n", x.a, x.b, x.handle1, x.handle2);
	if (x.a != 1234)
		return 1;
	if (x.b != 8743)
		return 2;

	if (x.handle1 != (void*) 0x7080feed)
		return 3;

	if (x.handle2 != (void*) 0x1234abcd)
		return 4;
	
	return 0xf00f;
}

typedef struct {
	void *a;
} TrivialHandle;

LIBTEST_API int STDCALL 
mono_safe_handle_struct_simple (TrivialHandle x)
{
	printf ("The value is %p\n", x.a);
	return ((int)(gsize)x.a) * 2;
}

LIBTEST_API int STDCALL 
mono_safe_handle_return (void)
{
	return 0x1000f00d;
}

LIBTEST_API void STDCALL
mono_safe_handle_ref (void **handle)
{
	if (*handle != 0){
		*handle = (void *) 0x800d;
		return;
	}

	*handle = (void *) 0xbad;
}

LIBTEST_API void* STDCALL
mono_safe_handle_ref_nomod (void **handle)
{
	return *handle;
}

LIBTEST_API double STDCALL
mono_test_marshal_date_time (double d, double *d2)
{
	*d2 = d;
	return d;
}

/*
 * COM INTEROP TESTS
 */

#ifndef WIN32

typedef struct
{
	guint32 a;
	guint16 b;
	guint16 c;
	guint8 d[8];
} GUID;

typedef const GUID *REFIID;

typedef struct IDispatch IDispatch;

typedef struct
{
	int (STDCALL *QueryInterface)(IDispatch *iface, REFIID iid, gpointer *out);
	int (STDCALL *AddRef)(IDispatch *iface);
	int (STDCALL *Release)(IDispatch *iface);
	int (STDCALL *GetTypeInfoCount)(IDispatch *iface, unsigned int *count);
	int (STDCALL *GetTypeInfo)(IDispatch *iface, unsigned int index, unsigned int lcid, gpointer *out);
	int (STDCALL *GetIDsOfNames)(IDispatch *iface, REFIID iid, gpointer names, unsigned int count, unsigned int lcid, gpointer ids);
	int (STDCALL *Invoke)(IDispatch *iface, unsigned int dispid, REFIID iid, unsigned int lcid, unsigned short flags, gpointer params, gpointer result, gpointer excepinfo, gpointer err_arg);
} IDispatchVtbl;

struct IDispatch
{
	const IDispatchVtbl *lpVtbl;
};

typedef struct {
	guint16 vt;
	guint16 wReserved1;
	guint16 wReserved2;
	guint16 wReserved3;
	union {
		gint64 llVal;
		gint32 lVal;
		guint8  bVal;
		gint16 iVal;
		float  fltVal;
		double dblVal;
		gint16 boolVal;
		gunichar2* bstrVal;
		gint8 cVal;
		guint16 uiVal;
		guint32 ulVal;
		guint64 ullVal;
		gpointer byref;
		struct {
			gpointer pvRecord;
			gpointer pRecInfo;
		};
	};
} VARIANT;

typedef enum {
	VARIANT_TRUE = -1,
	VARIANT_FALSE = 0
} VariantBool;

typedef enum {
	VT_EMPTY = 0,
	VT_NULL = 1,
	VT_I2 = 2,
	VT_I4 = 3,
	VT_R4 = 4,
	VT_R8 = 5,
	VT_CY = 6,
	VT_DATE = 7,
	VT_BSTR = 8,
	VT_DISPATCH = 9,
	VT_ERROR = 10,
	VT_BOOL = 11,
	VT_VARIANT = 12,
	VT_UNKNOWN = 13,
	VT_DECIMAL = 14,
	VT_I1 = 16,
	VT_UI1 = 17,
	VT_UI2 = 18,
	VT_UI4 = 19,
	VT_I8 = 20,
	VT_UI8 = 21,
	VT_INT = 22,
	VT_UINT = 23,
	VT_VOID = 24,
	VT_HRESULT = 25,
	VT_PTR = 26,
	VT_SAFEARRAY = 27,
	VT_CARRAY = 28,
	VT_USERDEFINED = 29,
	VT_LPSTR = 30,
	VT_LPWSTR = 31,
	VT_RECORD = 36,
	VT_FILETIME = 64,
	VT_BLOB = 65,
	VT_STREAM = 66,
	VT_STORAGE = 67,
	VT_STREAMED_OBJECT = 68,
	VT_STORED_OBJECT = 69,
	VT_BLOB_OBJECT = 70,
	VT_CF = 71,
	VT_CLSID = 72,
	VT_VECTOR = 4096,
	VT_ARRAY = 8192,
	VT_BYREF = 16384
} VarEnum;

void VariantInit(VARIANT* vt)
{
	vt->vt = VT_EMPTY;
}

#define S_OK 0

#endif

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_in(gunichar2* bstr)
{
	gint32 result = 0;
	gchar* bstr_utf8 = g_utf16_to_utf8 (bstr, -1, NULL, NULL, NULL);
	result = strcmp("mono_test_marshal_bstr_in", bstr_utf8);
	g_free(bstr_utf8);
	if (result == 0)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_out(gunichar2** bstr)
{
	*bstr = marshal_bstr_alloc ("mono_test_marshal_bstr_out");
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_in_null(gunichar2* bstr)
{
	if (!bstr)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_out_null(gunichar2** bstr)
{
	*bstr = NULL;
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_sbyte(VARIANT variant)
{
	if (variant.vt == VT_I1 && variant.cVal == 100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_byte(VARIANT variant)
{
	if (variant.vt == VT_UI1 && variant.bVal == 100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_short(VARIANT variant)
{
	if (variant.vt == VT_I2 && variant.iVal == 314)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_ushort(VARIANT variant)
{
	if (variant.vt == VT_UI2 && variant.uiVal == 314)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_int(VARIANT variant)
{
	if (variant.vt == VT_I4 && variant.lVal == 314)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_uint(VARIANT variant)
{
	if (variant.vt == VT_UI4 && variant.ulVal == 314)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_long(VARIANT variant)
{
	if (variant.vt == VT_I8 && variant.llVal == 314)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_ulong(VARIANT variant)
{
	if (variant.vt == VT_UI8 && variant.ullVal == 314)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_float(VARIANT variant)
{
	if (variant.vt == VT_R4 && (variant.fltVal - 3.14)/3.14 < .001)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_double(VARIANT variant)
{
	if (variant.vt == VT_R8 && (variant.dblVal - 3.14)/3.14 < .001)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_bstr(VARIANT variant)
{
	gint32 result = 0;
        gchar* bstr_utf8 = g_utf16_to_utf8 (variant.bstrVal, -1, NULL, NULL, NULL);
        result = strcmp("PI", bstr_utf8);
        g_free(bstr_utf8);

	if (variant.vt == VT_BSTR && !result)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_bool_true (VARIANT variant)
{
	if (variant.vt == VT_BOOL && variant.boolVal == VARIANT_TRUE)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_bool_false (VARIANT variant)
{
	if (variant.vt == VT_BOOL && variant.boolVal == VARIANT_FALSE)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_sbyte(VARIANT* variant)
{
	variant->vt = VT_I1;
	variant->cVal = 100;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_sbyte_byref(VARIANT* variant)
{
	variant->vt = VT_I1|VT_BYREF;
	variant->byref = marshal_alloc(1);
	*((gint8*)variant->byref) = 100;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_byte(VARIANT* variant)
{	
	variant->vt = VT_UI1;
	variant->bVal = 100;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_byte_byref(VARIANT* variant)
{	
	variant->vt = VT_UI1|VT_BYREF;
	variant->byref = marshal_alloc(1);
	*((gint8*)variant->byref) = 100;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_short(VARIANT* variant)
{
	variant->vt = VT_I2;
	variant->iVal = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_short_byref(VARIANT* variant)
{
	variant->vt = VT_I2|VT_BYREF;
	variant->byref = marshal_alloc(2);
	*((gint16*)variant->byref) = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_ushort(VARIANT* variant)
{
	variant->vt = VT_UI2;
	variant->uiVal = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_ushort_byref(VARIANT* variant)
{
	variant->vt = VT_UI2|VT_BYREF;
	variant->byref = marshal_alloc(2);
	*((guint16*)variant->byref) = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_int(VARIANT* variant)
{
	variant->vt = VT_I4;
	variant->lVal = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_int_byref(VARIANT* variant)
{
	variant->vt = VT_I4|VT_BYREF;
	variant->byref = marshal_alloc(4);
	*((gint32*)variant->byref) = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_uint(VARIANT* variant)
{
	variant->vt = VT_UI4;
	variant->ulVal = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_uint_byref(VARIANT* variant)
{
	variant->vt = VT_UI4|VT_BYREF;
	variant->byref = marshal_alloc(4);
	*((guint32*)variant->byref) = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_long(VARIANT* variant)
{
	variant->vt = VT_I8;
	variant->llVal = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_long_byref(VARIANT* variant)
{
	variant->vt = VT_I8|VT_BYREF;
	variant->byref = marshal_alloc(8);
	*((gint64*)variant->byref) = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_ulong(VARIANT* variant)
{
	variant->vt = VT_UI8;
	variant->ullVal = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_ulong_byref(VARIANT* variant)
{
	variant->vt = VT_UI8|VT_BYREF;
	variant->byref = marshal_alloc(8);
	*((guint64*)variant->byref) = 314;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_float(VARIANT* variant)
{
	variant->vt = VT_R4;
	variant->fltVal = 3.14;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_float_byref(VARIANT* variant)
{
	variant->vt = VT_R4|VT_BYREF;
	variant->byref = marshal_alloc(4);
	*((float*)variant->byref) = 3.14;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_double(VARIANT* variant)
{
	variant->vt = VT_R8;
	variant->dblVal = 3.14;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_double_byref(VARIANT* variant)
{
	variant->vt = VT_R8|VT_BYREF;
	variant->byref = marshal_alloc(8);
	*((double*)variant->byref) = 3.14;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bstr(VARIANT* variant)
{
	variant->vt = VT_BSTR;
	variant->bstrVal = marshal_bstr_alloc("PI");

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bstr_byref(VARIANT* variant)
{
	variant->vt = VT_BSTR|VT_BYREF;
	variant->byref = marshal_alloc(sizeof(gpointer));
	*((gunichar**)variant->byref) = (gunichar*)marshal_bstr_alloc("PI");

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_true (VARIANT* variant)
{
	variant->vt = VT_BOOL;
	variant->boolVal = VARIANT_TRUE;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_true_byref (VARIANT* variant)
{
	variant->vt = VT_BOOL|VT_BYREF;
	variant->byref = marshal_alloc(2);
	*((gint16*)variant->byref) = VARIANT_TRUE;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_false (VARIANT* variant)
{
	variant->vt = VT_BOOL;
	variant->boolVal = VARIANT_FALSE;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_false_byref (VARIANT* variant)
{
	variant->vt = VT_BOOL|VT_BYREF;
	variant->byref = marshal_alloc(2);
	*((gint16*)variant->byref) = VARIANT_FALSE;

	return 0;
}

typedef int (STDCALL *VarFunc) (int vt, VARIANT variant);
typedef int (STDCALL *VarRefFunc) (int vt, VARIANT* variant);

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_sbyte_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_I1;
	vt.cVal = -100;
	return func (VT_I1, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_byte_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_UI1;
	vt.bVal = 100;
	return func (VT_UI1, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_short_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_I2;
	vt.iVal = -100;
	return func (VT_I2, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_ushort_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_UI2;
	vt.uiVal = 100;
	return func (VT_UI2, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_int_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_I4;
	vt.lVal = -100;
	return func (VT_I4, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_uint_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_UI4;
	vt.ulVal = 100;
	return func (VT_UI4, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_long_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_I8;
	vt.llVal = -100;
	return func (VT_I8, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_ulong_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_UI8;
	vt.ullVal = 100;
	return func (VT_UI8, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_float_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_R4;
	vt.fltVal = 3.14;
	return func (VT_R4, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_double_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_R8;
	vt.dblVal = 3.14;
	return func (VT_R8, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_bstr_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_BSTR;
	vt.bstrVal = marshal_bstr_alloc("PI");
	return func (VT_BSTR, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_bool_true_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_BOOL;
	vt.boolVal = VARIANT_TRUE;
	return func (VT_BOOL, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_in_bool_false_unmanaged(VarFunc func)
{
	VARIANT vt;
	vt.vt = VT_BOOL;
	vt.boolVal = VARIANT_FALSE;
	return func (VT_BOOL, vt);
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_sbyte_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_I1, &vt);
	if (vt.vt == VT_I1 && vt.cVal == -100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_byte_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_UI1, &vt);
	if (vt.vt == VT_UI1 && vt.bVal == 100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_short_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_I2, &vt);
	if (vt.vt == VT_I2 && vt.iVal == -100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_ushort_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_UI2, &vt);
	if (vt.vt == VT_UI2 && vt.uiVal == 100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_int_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_I4, &vt);
	if (vt.vt == VT_I4 && vt.lVal == -100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_uint_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_UI4, &vt);
	if (vt.vt == VT_UI4 && vt.ulVal == 100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_long_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_I8, &vt);
	if (vt.vt == VT_I8 && vt.llVal == -100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_ulong_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_UI8, &vt);
	if (vt.vt == VT_UI8 && vt.ullVal == 100)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_float_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_R4, &vt);
	if (vt.vt == VT_R4 && fabs (vt.fltVal - 3.14f) < 1e-10)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_double_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_R8, &vt);
	if (vt.vt == VT_R8 && fabs (vt.dblVal - 3.14) < 1e-10)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bstr_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	gchar* bstr_utf8;
 	gint32 result = 0;


	VariantInit (&vt);
	func (VT_BSTR, &vt);
        bstr_utf8 = g_utf16_to_utf8 (vt.bstrVal, -1, NULL, NULL, NULL);
        result = strcmp("PI", bstr_utf8);
        g_free(bstr_utf8);
	if (vt.vt == VT_BSTR && !result)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_true_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_BOOL, &vt);
	if (vt.vt == VT_BOOL && vt.boolVal == VARIANT_TRUE)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_false_unmanaged(VarRefFunc func)
{
	VARIANT vt;
	VariantInit (&vt);
	func (VT_BOOL, &vt);
	if (vt.vt == VT_BOOL && vt.boolVal == VARIANT_TRUE)
		return 0;
	return 1;
}

typedef struct _StructWithVariant {
    VARIANT data;
} StructWithVariant;
typedef int (STDCALL *CheckStructWithVariantFunc) (StructWithVariant sv);

LIBTEST_API int STDCALL 
mono_test_marshal_struct_with_variant_in_unmanaged(CheckStructWithVariantFunc func)
{
    StructWithVariant sv;
    sv.data.vt = VT_I4;
    sv.data.lVal = -123;
    return func(sv);
}

LIBTEST_API int STDCALL
mono_test_marshal_struct_with_variant_out_unmanaged (StructWithVariant sv)
{
	if (sv.data.vt != VT_I4)
		return 1;
	if (sv.data.lVal != -123)
		return 2;
	return 0;
}

typedef struct _StructWithBstr {
    gunichar2* data;
} StructWithBstr;
typedef int (STDCALL *CheckStructWithBstrFunc) (StructWithBstr sb);

LIBTEST_API int STDCALL 
mono_test_marshal_struct_with_bstr_in_unmanaged(CheckStructWithBstrFunc func)
{
    StructWithBstr sb;
    sb.data = marshal_bstr_alloc("this is a test string");
    return func(sb);
}

LIBTEST_API int STDCALL
mono_test_marshal_struct_with_bstr_out_unmanaged (StructWithBstr sb)
{
	char *s = g_utf16_to_utf8 (sb.data, g_utf16_len (sb.data), NULL, NULL, NULL);
	gboolean same = !strcmp (s, "this is a test string");
	g_free (s);
	if (!same)
		return 1;
	return 0;
}

typedef struct MonoComObject MonoComObject;
typedef struct MonoDefItfObject MonoDefItfObject;

typedef struct
{
	int (STDCALL *QueryInterface)(MonoDefItfObject* pUnk, gpointer riid, gpointer* ppv);
	int (STDCALL *AddRef)(MonoDefItfObject* pUnk);
	int (STDCALL *Release)(MonoDefItfObject* pUnk);
	int (STDCALL *Method)(MonoDefItfObject* pUnk, int *value);
} MonoDefItf;

typedef struct
{
	int (STDCALL *QueryInterface)(MonoComObject* pUnk, gpointer riid, gpointer* ppv);
	int (STDCALL *AddRef)(MonoComObject* pUnk);
	int (STDCALL *Release)(MonoComObject* pUnk);
	int (STDCALL *get_ITest)(MonoComObject* pUnk, MonoComObject* *ppUnk);
	int (STDCALL *SByteIn)(MonoComObject* pUnk, char a);
	int (STDCALL *ByteIn)(MonoComObject* pUnk, unsigned char a);
	int (STDCALL *ShortIn)(MonoComObject* pUnk, short a);
	int (STDCALL *UShortIn)(MonoComObject* pUnk, unsigned short a);
	int (STDCALL *IntIn)(MonoComObject* pUnk, int a);
	int (STDCALL *UIntIn)(MonoComObject* pUnk, unsigned int a);
	int (STDCALL *LongIn)(MonoComObject* pUnk, gint64 a);
	int (STDCALL *ULongIn)(MonoComObject* pUnk, guint64 a);
	int (STDCALL *FloatIn)(MonoComObject* pUnk, float a);
	int (STDCALL *DoubleIn)(MonoComObject* pUnk, double a);
	int (STDCALL *ITestIn)(MonoComObject* pUnk, MonoComObject* pUnk2);
	int (STDCALL *ITestOut)(MonoComObject* pUnk, MonoComObject* *ppUnk);
	int (STDCALL *Return22NoICall)(MonoComObject* pUnk);
	int (STDCALL *IntOut)(MonoComObject* pUnk, int *a);
	int (STDCALL *ArrayIn)(MonoComObject* pUnk, void *array);
	int (STDCALL *ArrayIn2)(MonoComObject* pUnk, void *array);
	int (STDCALL *ArrayIn3)(MonoComObject* pUnk, void *array);
	int (STDCALL *ArrayOut)(MonoComObject* pUnk, guint32 *array, guint32 *result);
	int (STDCALL *GetDefInterface1)(MonoComObject* pUnk, MonoDefItfObject **iface);
	int (STDCALL *GetDefInterface2)(MonoComObject* pUnk, MonoDefItfObject **iface);
} MonoIUnknown;

struct MonoComObject
{
	MonoIUnknown* vtbl;
	int m_ref;
};

struct MonoDefItfObject
{
	MonoDefItf* vtbl;
};

static GUID IID_ITest = {0, 0, 0, {0,0,0,0,0,0,0,1}};
static GUID IID_IMonoUnknown = {0, 0, 0, {0xc0,0,0,0,0,0,0,0x46}};
static GUID IID_IMonoDispatch = {0x00020400, 0, 0, {0xc0,0,0,0,0,0,0,0x46}};
static GUID IID_INotImplemented = {0x12345678, 0, 0, {0x9a, 0xbc, 0xde, 0xf0, 0, 0, 0, 0}};

LIBTEST_API int STDCALL
MonoQueryInterface(MonoComObject* pUnk, gpointer riid, gpointer* ppv)
{

	*ppv = NULL;
	if (!memcmp(riid, &IID_IMonoUnknown, sizeof(GUID))) {
		*ppv = pUnk;
		return S_OK;
	}
	else if (!memcmp(riid, &IID_ITest, sizeof(GUID))) {
		*ppv = pUnk;
		return S_OK;
	}
	else if (!memcmp(riid, &IID_IMonoDispatch, sizeof(GUID))) {
		*ppv = pUnk;
		return S_OK;
	}
	return 0x80004002; //E_NOINTERFACE;
}

LIBTEST_API int STDCALL 
MonoAddRef(MonoComObject* pUnk)
{
	return ++(pUnk->m_ref);
}

LIBTEST_API int STDCALL 
MonoRelease(MonoComObject* pUnk)
{
	return --(pUnk->m_ref);
}

LIBTEST_API int STDCALL 
SByteIn(MonoComObject* pUnk, char a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
ByteIn(MonoComObject* pUnk, unsigned char a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
ShortIn(MonoComObject* pUnk, short a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
UShortIn(MonoComObject* pUnk, unsigned short a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
IntIn(MonoComObject* pUnk, int a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
UIntIn(MonoComObject* pUnk, unsigned int a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
LongIn(MonoComObject* pUnk, gint64 a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
ULongIn(MonoComObject* pUnk, guint64 a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
FloatIn(MonoComObject* pUnk, float a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
DoubleIn(MonoComObject* pUnk, double a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
ITestIn(MonoComObject* pUnk, MonoComObject *pUnk2)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
ITestOut(MonoComObject* pUnk, MonoComObject* *ppUnk)
{
	return S_OK;
}

LIBTEST_API int STDCALL
Return22NoICall(MonoComObject* pUnk)
{
	return 22;
}

LIBTEST_API int STDCALL
IntOut(MonoComObject* pUnk, int *a)
{
	return S_OK;
}

LIBTEST_API int STDCALL
ArrayIn(MonoComObject* pUnk, void *array)
{
	return S_OK;
}

LIBTEST_API int STDCALL
ArrayIn2(MonoComObject* pUnk, void *array)
{
	return S_OK;
}

LIBTEST_API int STDCALL
ArrayIn3(MonoComObject* pUnk, void *array)
{
	return S_OK;
}

LIBTEST_API int STDCALL
ArrayOut(MonoComObject* pUnk, guint32 *array, guint32 *result)
{
	return S_OK;
}

LIBTEST_API int STDCALL
GetDefInterface1(MonoComObject* pUnk, MonoDefItfObject **obj)
{
	return S_OK;
}

LIBTEST_API int STDCALL
GetDefInterface2(MonoComObject* pUnk, MonoDefItfObject **obj)
{
	return S_OK;
}

static void create_com_object (MonoComObject** pOut);

LIBTEST_API int STDCALL 
get_ITest(MonoComObject* pUnk, MonoComObject* *ppUnk)
{
	create_com_object (ppUnk);
	return S_OK;
}

static void create_com_object (MonoComObject** pOut)
{
	*pOut = marshal_new0 (MonoComObject, 1);
	(*pOut)->vtbl = marshal_new0 (MonoIUnknown, 1);

	(*pOut)->m_ref = 1;
	(*pOut)->vtbl->QueryInterface = MonoQueryInterface;
	(*pOut)->vtbl->AddRef = MonoAddRef;
	(*pOut)->vtbl->Release = MonoRelease;
	(*pOut)->vtbl->SByteIn = SByteIn;
	(*pOut)->vtbl->ByteIn = ByteIn;
	(*pOut)->vtbl->ShortIn = ShortIn;
	(*pOut)->vtbl->UShortIn = UShortIn;
	(*pOut)->vtbl->IntIn = IntIn;
	(*pOut)->vtbl->UIntIn = UIntIn;
	(*pOut)->vtbl->LongIn = LongIn;
	(*pOut)->vtbl->ULongIn = ULongIn;
	(*pOut)->vtbl->FloatIn = FloatIn;
	(*pOut)->vtbl->DoubleIn = DoubleIn;
	(*pOut)->vtbl->ITestIn = ITestIn;
	(*pOut)->vtbl->ITestOut = ITestOut;
	(*pOut)->vtbl->get_ITest = get_ITest;
	(*pOut)->vtbl->Return22NoICall = Return22NoICall;
	(*pOut)->vtbl->IntOut = IntOut;
	(*pOut)->vtbl->ArrayIn = ArrayIn;
	(*pOut)->vtbl->ArrayIn2 = ArrayIn2;
	(*pOut)->vtbl->ArrayIn3 = ArrayIn3;
	(*pOut)->vtbl->ArrayOut = ArrayOut;
	(*pOut)->vtbl->GetDefInterface1 = GetDefInterface1;
	(*pOut)->vtbl->GetDefInterface2 = GetDefInterface2;
}

static MonoComObject* same_object = NULL;

LIBTEST_API int STDCALL 
mono_test_marshal_com_object_create(MonoComObject* *pUnk)
{
	create_com_object (pUnk);

	if (!same_object)
		same_object = *pUnk;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_com_object_same(MonoComObject* *pUnk)
{
	*pUnk = same_object;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_com_object_destroy(MonoComObject *pUnk)
{
	int ref = --(pUnk->m_ref);
	g_free(pUnk->vtbl);
	g_free(pUnk);

	return ref;
}

LIBTEST_API int STDCALL 
mono_test_marshal_com_object_ref_count(MonoComObject *pUnk)
{
	return pUnk->m_ref;
}

LIBTEST_API int STDCALL 
mono_test_marshal_ccw_itest (MonoComObject *pUnk)
{
	int hr = 0;
	MonoComObject* pTest;

	if (!pUnk)
		return 1;

	hr = pUnk->vtbl->SByteIn (pUnk, -100);
	if (hr != 0)
		return 2;
	hr = pUnk->vtbl->ByteIn (pUnk, 100);
	if (hr != 0)
		return 3;
	hr = pUnk->vtbl->ShortIn (pUnk, -100);
	if (hr != 0)
		return 4;
	hr = pUnk->vtbl->UShortIn (pUnk, 100);
	if (hr != 0)
		return 5;
	hr = pUnk->vtbl->IntIn (pUnk, -100);
	if (hr != 0)
		return 6;
	hr = pUnk->vtbl->UIntIn (pUnk, 100);
	if (hr != 0)
		return 7;
	hr = pUnk->vtbl->LongIn (pUnk, -100);
	if (hr != 0)
		return 8;
	hr = pUnk->vtbl->ULongIn (pUnk, 100);
	if (hr != 0)
		return 9;
	hr = pUnk->vtbl->FloatIn (pUnk, 3.14f);
	if (hr != 0)
		return 10;
	hr = pUnk->vtbl->DoubleIn (pUnk, 3.14);
	if (hr != 0)
		return 11;
	hr = pUnk->vtbl->ITestIn (pUnk, pUnk);
	if (hr != 0)
		return 12;
	hr = pUnk->vtbl->ITestOut (pUnk, &pTest);
	if (hr != 0)
		return 13;

	return 0;
}

// Xamarin-47560
LIBTEST_API int STDCALL
mono_test_marshal_array_ccw_itest (int count, MonoComObject ** ppUnk)
{
	int hr = 0;

	if (!ppUnk)
		return 1;

	if (count < 1)
		return 2;

	if (!ppUnk[0])
		return 3;

	hr = ppUnk[0]->vtbl->SByteIn (ppUnk[0], -100);
	if (hr != 0)
		return 4;

	return 0;
}

LIBTEST_API int STDCALL
mono_test_marshal_retval_ccw_itest (MonoComObject *pUnk, int test_null)
{
	int hr = 0, i = 0;

	if (!pUnk)
		return 1;

	hr = pUnk->vtbl->IntOut (pUnk, &i);
	if (hr != 0)
		return 2;
	if (i != 33)
		return 3;
	if (test_null)
	{
		hr = pUnk->vtbl->IntOut (pUnk, NULL);
		if (hr != 0)
			return 4;
	}

	return 0;
}

LIBTEST_API int STDCALL
mono_test_default_interface_ccw (MonoComObject *pUnk)
{
	MonoDefItfObject *obj;
	int ret, value;

	ret = pUnk->vtbl->GetDefInterface1(pUnk, &obj);
	if (ret)
		return 1;
	value = 0;

	ret = obj->vtbl->Method(obj, &value);
	obj->vtbl->Release(obj);
	if (ret)
		return 2;
	if (value != 1)
		return 3;

	ret = pUnk->vtbl->GetDefInterface2(pUnk, &obj);
	if (ret)
		return 4;
	ret = obj->vtbl->Method(obj, &value);
	obj->vtbl->Release(obj);
	if (ret)
		return 5;
	if (value != 2)
		return 6;

	return 0;
}

/*
 * mono_method_get_unmanaged_thunk tests
 */

#if defined(__GNUC__) && ((defined(__i386__) && (defined(__linux__) || defined (__APPLE__)) || defined (__FreeBSD__) || defined(__OpenBSD__)) || (defined(__ppc__) && defined(__APPLE__)))
#define ALIGN(size) __attribute__ ((__aligned__(size)))
#else
#define ALIGN(size)
#endif


/* thunks.cs:TestStruct */
typedef struct _TestStruct {
	int A;
	double B;
} TestStruct;

/* Searches for mono symbols in all loaded modules */
static gpointer
lookup_mono_symbol (const char *symbol_name)
{
	gpointer symbol = NULL;
	GModule *mod = g_module_open (NULL, G_MODULE_BIND_LAZY);
	g_assert (mod != NULL);
	const gboolean success = g_module_symbol (mod, symbol_name, &symbol);
	g_assertf (success, "%s", symbol_name);
	return success ? symbol : NULL;
}

LIBTEST_API gpointer STDCALL
mono_test_marshal_lookup_symbol (const char *symbol_name)
{
#ifndef HOST_WIN32
	return dlsym (RTLD_DEFAULT, symbol_name);
#else
	// This isn't really proper, but it should work
	return lookup_mono_symbol (symbol_name);
#endif
}


// FIXME use runtime headers
#define MONO_BEGIN_EFRAME { void *__dummy; void *__region_cookie = mono_threads_enter_gc_unsafe_region ? mono_threads_enter_gc_unsafe_region (&__dummy) : NULL;
#define MONO_END_EFRAME if (mono_threads_exit_gc_unsafe_region) mono_threads_exit_gc_unsafe_region (__region_cookie, &__dummy); }

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

	// FIXME use runtime headers
	gpointer (*mono_method_get_unmanaged_thunk)(gpointer)
		= (gpointer (*)(gpointer))lookup_mono_symbol ("mono_method_get_unmanaged_thunk");

	// FIXME use runtime headers
	gpointer (*mono_string_new_wrapper)(const char *)
		= (gpointer (*)(const char *))lookup_mono_symbol ("mono_string_new_wrapper");

	// FIXME use runtime headers
	char *(*mono_string_to_utf8)(gpointer)
		= (char *(*)(gpointer))lookup_mono_symbol ("mono_string_to_utf8");

	// FIXME use runtime headers
	gpointer (*mono_object_unbox)(gpointer)
		= (gpointer (*)(gpointer))lookup_mono_symbol ("mono_object_unbox");

	// FIXME use runtime headers
	gpointer (*mono_threads_enter_gc_unsafe_region) (gpointer)
		= (gpointer (*)(gpointer))lookup_mono_symbol ("mono_threads_enter_gc_unsafe_region");

	// FIXME use runtime headers
	void (*mono_threads_exit_gc_unsafe_region) (gpointer, gpointer)
		= (void (*)(gpointer, gpointer))lookup_mono_symbol ("mono_threads_exit_gc_unsafe_region");

	

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
		int (STDCALL *F)(gpointer, guint8, gint16, gint32, gint64, float, double,
				 gpointer, gpointer*);
		gpointer obj;
		gpointer str = mono_string_new_wrapper ("Test6");
		int res;

		F = (int (STDCALL *)(gpointer, guint8, gint16, gint32, gint64, float, double, gpointer, gpointer *))test_method;
		obj = CreateObject (&ex);

		res = F (obj, 254, 32700, -245378, 6789600, 3.1415, 3.1415, str, &ex);
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
		gint64 (STDCALL *F)(gpointer*) = (gint64 (STDCALL *)(gpointer *))test_method;
		if (F (&ex) != G_MAXINT64) {
			ret = 4;
			goto done;
		}
		break;
	}

	case 8: {
		/* thunks.cs:Test.Test8 */
		void (STDCALL *F)(guint8*, gint16*, gint32*, gint64*, float*, double*,
				 gpointer*, gpointer*);

		guint8 a1;
		gint16 a2;
		gint32 a3;
		gint64 a4;
		float a5;
		double a6;
		gpointer a7;

		F = (void (STDCALL *)(guint8 *, gint16 *, gint32 *, gint64 *, float *, double *,
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
		void (STDCALL *F)(guint8*, gint16*, gint32*, gint64*, float*, double*,
			gpointer*, gpointer*);

		guint8 a1;
		gint16 a2;
		gint32 a3;
		gint64 a4;
		float a5;
		double a6;
		gpointer a7;

		F = (void (STDCALL *)(guint8 *, gint16 *, gint32 *, gint64 *, float *, double *,
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

typedef struct 
{
	char a;
} winx64_struct1;

LIBTEST_API int STDCALL  
mono_test_Winx64_struct1_in (winx64_struct1 var)
{
	if (var.a != 123)
		return 1;
	return 0;
}

typedef struct
{
	char a;
	char b;
} winx64_struct2;

LIBTEST_API int STDCALL  
mono_test_Winx64_struct2_in (winx64_struct2 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	return 0;
}


typedef struct
{
	char a;
	char b;
	short c;
} winx64_struct3;

LIBTEST_API int STDCALL  
mono_test_Winx64_struct3_in (winx64_struct3 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	if (var.c != 0x1234)
		return 3;
	return 0;
}

typedef struct
{
	char a;
	char b;
	short c;
	unsigned int d;
} winx64_struct4;

LIBTEST_API int STDCALL  
mono_test_Winx64_struct4_in (winx64_struct4 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	if (var.c != 0x1234)
		return 3;
	if (var.d != 0x87654321)
		return 4;
	return 0;
}

typedef struct
{
	char a;
	char b;
	char c;
} winx64_struct5;

LIBTEST_API int STDCALL  
mono_test_Winx64_struct5_in (winx64_struct5 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	if (var.c != 6)
		return 3;
	return 0;
}

typedef struct
{
	winx64_struct1 a;
	short b;
	char c;
} winx64_struct6;

LIBTEST_API int STDCALL  
mono_test_Winx64_struct6_in (winx64_struct6 var)
{
	if (var.a.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	if (var.c != 6)
		return 3;
	return 0;
}

LIBTEST_API int STDCALL  
mono_test_Winx64_structs_in1 (winx64_struct1 var1,
			 winx64_struct2 var2,
			 winx64_struct3 var3,
			 winx64_struct4 var4)
{
	if (var1.a != 123)
		return 1;
	
	if (var2.a != 4)
		return 2;
	if (var2.b != 5)
		return 3;
	
	if (var3.a != 4)
		return 4;
	if (var3.b != 5)
		return 2;
	if (var3.c != 0x1234)
		return 5;
	
	if (var4.a != 4)
		return 6;
	if (var4.b != 5)
		return 7;
	if (var4.c != 0x1234)
		return 8;
	if (var4.d != 0x87654321)
		return 9;
	return 0;
}

LIBTEST_API int STDCALL  
mono_test_Winx64_structs_in2 (winx64_struct1 var1,
			 winx64_struct1 var2,
			 winx64_struct1 var3,
			 winx64_struct1 var4,
			 winx64_struct1 var5)
{
	if (var1.a != 1)
		return 1;
	if (var2.a != 2)
		return 2;
	if (var3.a != 3)
		return 3;
	if (var4.a != 4)
		return 4;
	if (var5.a != 5)
		return 5;
	
	return 0;
}

LIBTEST_API int STDCALL  
mono_test_Winx64_structs_in3 (winx64_struct1 var1,
			 winx64_struct5 var2,
			 winx64_struct1 var3,
			 winx64_struct5 var4,
			 winx64_struct1 var5,
			 winx64_struct5 var6)
{
	if (var1.a != 1)
		return 1;
	
	if (var2.a != 2)
		return 2;
	if (var2.b != 3)
		return 2;
	if (var2.c != 4)
		return 4;
	
	if (var3.a != 5)
		return 5;
	
	if (var4.a != 6)
		return 6;
	if (var4.b != 7)
		return 7;
	if (var4.c != 8)
		return 8;
	
	if (var5.a != 9)
		return 9;

	if (var6.a != 10)
		return 10;
	if (var6.b != 11)
		return 11;
	if (var6.c != 12)
		return 12;
	
	return 0;
}

LIBTEST_API winx64_struct1 STDCALL  
mono_test_Winx64_struct1_ret (void)
{
	winx64_struct1 ret;
	ret.a = 123;
	return ret;
}

LIBTEST_API winx64_struct2 STDCALL  
mono_test_Winx64_struct2_ret (void)
{
	winx64_struct2 ret;
	ret.a = 4;
	ret.b = 5;
	return ret;
}

LIBTEST_API winx64_struct3 STDCALL  
mono_test_Winx64_struct3_ret (void)
{
	winx64_struct3 ret;
	ret.a = 4;
	ret.b = 5;
	ret.c = 0x1234;
	return ret;
}

LIBTEST_API winx64_struct4 STDCALL  
mono_test_Winx64_struct4_ret (void)
{
	winx64_struct4 ret;
	ret.a = 4;
	ret.b = 5;
	ret.c = 0x1234;
	ret.d = 0x87654321;
	return ret;
}

LIBTEST_API winx64_struct5 STDCALL  
mono_test_Winx64_struct5_ret (void)
{
	winx64_struct5 ret;
	ret.a = 4;
	ret.b = 5;
	ret.c = 6;
	return ret;
}

LIBTEST_API winx64_struct1 STDCALL  
mono_test_Winx64_struct1_ret_5_args (char a, char b, char c, char d, char e)
{
	winx64_struct1 ret;
	ret.a = a + b + c + d + e;
	return ret;
}

LIBTEST_API winx64_struct5 STDCALL
mono_test_Winx64_struct5_ret6_args (char a, char b, char c, char d, char e)
{
	winx64_struct5 ret;
	ret.a = a + b;
	ret.b = c + d;
	ret.c = e;
	return ret;
}

typedef struct
{
	float a;
	float b;
} winx64_floatStruct;

LIBTEST_API int STDCALL  
mono_test_Winx64_floatStruct (winx64_floatStruct a)
{
	if (a.a > 5.6 || a.a < 5.4)
		return 1;

	if (a.b > 9.6 || a.b < 9.4)
		return 2;
	
	return 0;
}

typedef struct
{
	double a;
} winx64_doubleStruct;

LIBTEST_API int STDCALL  
mono_test_Winx64_doubleStruct (winx64_doubleStruct a)
{
	if (a.a > 5.6 || a.a < 5.4)
		return 1;
	
	return 0;
}

typedef int (STDCALL *managed_struct1_delegate) (winx64_struct1 a);

LIBTEST_API int STDCALL 
mono_test_managed_Winx64_struct1_in(managed_struct1_delegate func)
{
	winx64_struct1 val;
	val.a = 5;
	return func (val);
}

typedef int (STDCALL *managed_struct5_delegate) (winx64_struct5 a);

LIBTEST_API int STDCALL 
mono_test_managed_Winx64_struct5_in(managed_struct5_delegate func)
{
	winx64_struct5 val;
	val.a = 5;
	val.b = 0x10;
	val.c = (char)0x99;
	return func (val);
}

typedef int (STDCALL *managed_struct1_struct5_delegate) (winx64_struct1 a, winx64_struct5 b,
							 winx64_struct1 c, winx64_struct5 d,
							 winx64_struct1 e, winx64_struct5 f);

LIBTEST_API int STDCALL 
mono_test_managed_Winx64_struct1_struct5_in(managed_struct1_struct5_delegate func)
{
	winx64_struct1 a, c, e;
	winx64_struct5 b, d, f;
	a.a = 1;
	b.a = 2; b.b = 3; b.c = 4;
	c.a = 5;
	d.a = 6; d.b = 7; d.c = 8;
	e.a = 9;
	f.a = 10; f.b = 11; f.c = 12;

	return func (a, b, c, d, e, f);
}

typedef winx64_struct1 (STDCALL *managed_struct1_ret_delegate) (void);

LIBTEST_API int STDCALL 
mono_test_Winx64_struct1_ret_managed (managed_struct1_ret_delegate func)
{
	winx64_struct1 ret;

	ret = func ();

	if (ret.a != 0x45)
		return 1;
	
	return 0;
}

typedef winx64_struct5 (STDCALL *managed_struct5_ret_delegate) (void);

LIBTEST_API int STDCALL 
mono_test_Winx64_struct5_ret_managed (managed_struct5_ret_delegate func)
{
	winx64_struct5 ret;

	ret = func ();

	if (ret.a != 0x12)
		return 1;
	if (ret.b != 0x34)
		return 2;
	if (ret.c != 0x56)
		return 3;
	
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bool_in (int arg, unsigned int expected, unsigned int bDefaultMarsh, unsigned int bBoolCustMarsh,
			   char bI1CustMarsh, unsigned char bU1CustMarsh, short bVBCustMarsh)
{
	switch (arg) {
	case 1:	
		if (bDefaultMarsh != expected)
			return 1;
		break;
	case 2:	
		if (bBoolCustMarsh != expected)
			return 2;
		break;
	case 3:	
		if (bI1CustMarsh != expected)
			return 3;
		break;
	case 4:	
		if (bU1CustMarsh != expected)
			return 4;
		break;
	case 5:	
		if (bVBCustMarsh != expected)
			return 5;
		break;
	default:
		return 999;		
	}
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bool_out (int arg, unsigned int testVal, unsigned int* bDefaultMarsh, unsigned int* bBoolCustMarsh,
			   char* bI1CustMarsh, unsigned char* bU1CustMarsh, unsigned short* bVBCustMarsh)
{
	switch (arg) {
	case 1:	
		if (!bDefaultMarsh)
			return 1;
		*bDefaultMarsh = testVal;
		break;	
	case 2:	
		if (!bBoolCustMarsh)
			return 2;
		*bBoolCustMarsh = testVal;
		break;	
	case 3:	
		if (!bI1CustMarsh)
			return 3;
		*bI1CustMarsh = (char)testVal;
		break;	
	case 4:	
		if (!bU1CustMarsh)
			return 4;
		*bU1CustMarsh = (unsigned char)testVal;
		break;	
	case 5:	
		if (!bVBCustMarsh)
			return 5;
		*bVBCustMarsh = (unsigned short)testVal;
		break;	
	default:
		return 999;
	}
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bool_ref (int arg, unsigned int expected, unsigned int testVal, unsigned int* bDefaultMarsh,
			    unsigned int* bBoolCustMarsh, char* bI1CustMarsh, unsigned char* bU1CustMarsh, 
			    unsigned short* bVBCustMarsh)
{
	switch (arg) {
	case 1:	
		if (!bDefaultMarsh)
			return 1;
		if (*bDefaultMarsh != expected)
			return 2;
		*bDefaultMarsh = testVal;
		break;
	case 2:	
		if (!bBoolCustMarsh)
			return 3;
		if (*bBoolCustMarsh != expected)
			return 4;
		*bBoolCustMarsh = testVal;
		break;
	case 3:	
		if (!bI1CustMarsh)
			return 5;
		if (*bI1CustMarsh != expected)
			return 6;
		*bI1CustMarsh = (char)testVal;
		break;
	case 4:	
		if (!bU1CustMarsh)
			return 7;
		if (*bU1CustMarsh != expected)
			return 8;
		*bU1CustMarsh = (unsigned char)testVal;
		break;
	case 5:	
		if (!bVBCustMarsh)
			return 9;
		if (*bVBCustMarsh != expected)
			return 10;
		*bVBCustMarsh = (unsigned short)testVal;
		break;
	default:
		return 999;		
	}
	return 0;
}


typedef int (STDCALL *MarshalBoolInDelegate) (int arg, unsigned int expected, unsigned int bDefaultMarsh,
	unsigned int bBoolCustMarsh, char bI1CustMarsh, unsigned char bU1CustMarsh, unsigned short bVBCustMarsh);

LIBTEST_API int STDCALL 
mono_test_managed_marshal_bool_in (int arg, unsigned int expected, unsigned int testVal, MarshalBoolInDelegate pfcn)
{
	if (!pfcn)
		return 0x9900;

	switch (arg) {
	case 1:
		return pfcn (arg, expected, testVal, 0, 0, 0, 0);
	case 2:
		return pfcn (arg, expected, 0, testVal,  0, 0, 0);
	case 3:
		return pfcn (arg, expected, 0, 0, testVal, 0, 0);
	case 4:
		return pfcn (arg, expected, 0, 0, 0, testVal, 0);
	case 5:
		return pfcn (arg, expected, 0, 0, 0, 0, testVal);
	default:
		return 0x9800;
	}

	return 0;
}

typedef int (STDCALL *MarshalBoolOutDelegate) (int arg, unsigned int expected, unsigned int* bDefaultMarsh,
	unsigned int* bBoolCustMarsh, char* bI1CustMarsh, unsigned char* bU1CustMarsh, unsigned short* bVBCustMarsh);

LIBTEST_API int STDCALL 
mono_test_managed_marshal_bool_out (int arg, unsigned int expected, unsigned int testVal, MarshalBoolOutDelegate pfcn)
{
	int ret;
	unsigned int lDefaultMarsh, lBoolCustMarsh;
	char lI1CustMarsh = 0;
	unsigned char lU1CustMarsh = 0;
	unsigned short lVBCustMarsh = 0;
	lDefaultMarsh = lBoolCustMarsh = 0;

	if (!pfcn)
		return 0x9900;

	switch (arg) {
	case 1: {
		unsigned int ltVal = 0;
		ret = pfcn (arg, testVal, &ltVal, &lBoolCustMarsh, &lI1CustMarsh, &lU1CustMarsh, &lVBCustMarsh);
		if (ret)
			return 0x0100 + ret;
		if (expected != ltVal)
			return 0x0200;
		break;
	}
	case 2: {
		unsigned int ltVal = 0;
		ret = pfcn (arg, testVal, &lDefaultMarsh, &ltVal, &lI1CustMarsh, &lU1CustMarsh, &lVBCustMarsh);
		if (ret)
			return 0x0300 + ret;
		if (expected != ltVal)
			return 0x0400;
		break;
	}
	case 3: {
		char ltVal = 0;
		ret = pfcn (arg, testVal, &lDefaultMarsh, &lBoolCustMarsh, &ltVal, &lU1CustMarsh, &lVBCustMarsh);
		if (ret)
			return 0x0500 + ret;
		if (expected != ltVal)
			return 0x0600;
		break;
	}
	case 4: {
		unsigned char ltVal = 0;
		ret = pfcn (arg, testVal, &lDefaultMarsh, &lBoolCustMarsh, &lI1CustMarsh, &ltVal, &lVBCustMarsh);
		if (ret)
			return 0x0700 + ret;
		if (expected != ltVal)
			return 0x0800;
		break;
	}
	case 5: {
		unsigned short ltVal = 0;
		ret = pfcn (arg, testVal, &lDefaultMarsh, &lBoolCustMarsh, &lI1CustMarsh, &lU1CustMarsh, &ltVal);
		if (ret)
			return 0x0900 + ret;
		if (expected != ltVal)
			return 0x1000;
		break;
	}
	default:
		return 0x9800;
	}

	return 0;
}

typedef int (STDCALL *MarshalBoolRefDelegate) (int arg, unsigned int expected, unsigned int testVal, unsigned int* bDefaultMarsh,
	unsigned int* bBoolCustMarsh, char* bI1CustMarsh, unsigned char* bU1CustMarsh, unsigned short* bVBCustMarsh);

LIBTEST_API int STDCALL 
mono_test_managed_marshal_bool_ref (int arg, unsigned int expected, unsigned int testVal, unsigned int outExpected,
				    unsigned int outTestVal, MarshalBoolRefDelegate pfcn)
{
	int ret;
	unsigned int lDefaultMarsh, lBoolCustMarsh;
	char lI1CustMarsh = 0;
	unsigned char lU1CustMarsh = 0;
	unsigned short lVBCustMarsh = 0;
	lDefaultMarsh = lBoolCustMarsh = 0;

	if (!pfcn)
		return 0x9900;

	switch (arg) {
	case 1:
	{
		unsigned int ltestVal = testVal;
		ret = pfcn (arg, expected, outTestVal, &ltestVal, &lBoolCustMarsh, &lI1CustMarsh, &lU1CustMarsh, &lVBCustMarsh);
		if (ret)
			return 0x0100 + ret;
		if (outExpected != ltestVal)
			return 0x0200;
		break;
	}
	case 2:
	{
		unsigned int ltestVal = testVal;
		ret = pfcn (arg, expected, outTestVal, &lDefaultMarsh, &ltestVal, &lI1CustMarsh, &lU1CustMarsh, &lVBCustMarsh);
		if (ret)
			return 0x0300 + ret;
		if (outExpected != ltestVal)
			return 0x0400;
		break;
	}
	case 3:
	{
		char ltestVal = testVal;
		ret = pfcn (arg, expected, outTestVal, &lDefaultMarsh, &lBoolCustMarsh, &ltestVal, &lU1CustMarsh, &lVBCustMarsh);
		if (ret)
			return 0x0500 + ret;
		if (outExpected != ltestVal)
			return 0x0600;
		break;
	}
	case 4:
	{
		unsigned char ltestVal = testVal;
		ret = pfcn (arg, expected, outTestVal, &lDefaultMarsh, &lBoolCustMarsh, &lI1CustMarsh, &ltestVal, &lVBCustMarsh);
		if (ret)
			return 0x0700 + ret;
		if (outExpected != ltestVal)
			return 0x0800;
		break;
	}
	case 5:
	{
		unsigned short ltestVal = testVal;
		ret = pfcn (arg, expected, outTestVal, &lDefaultMarsh, &lBoolCustMarsh, &lI1CustMarsh, &lU1CustMarsh, &ltestVal);
		if (ret)
			return 0x0900 + ret;
		if (outExpected != ltestVal)
			return 0x1000;
		break;
	}
	default:
		return 0x9800;
	}

	return 0;
}

#ifdef WIN32

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_out_1dim_vt_bstr_empty (SAFEARRAY** safearray)
{
	/* Create an empty one-dimensional array of variants */
	SAFEARRAY *pSA;
	SAFEARRAYBOUND dimensions [1];

	dimensions [0].lLbound = 0;
	dimensions [0].cElements = 0;

	pSA = SafeArrayCreate (VT_VARIANT, 1, dimensions);
	*safearray = pSA;
	return S_OK;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_out_1dim_vt_bstr (SAFEARRAY** safearray)
{
	/* Create a one-dimensional array of 10 variants filled with "0" to "9" */
	SAFEARRAY *pSA;
	SAFEARRAYBOUND dimensions [1];
	long i;
	gchar buffer [20];
	HRESULT hr = S_OK;
	long indices [1];

	dimensions [0].lLbound = 0;
	dimensions [0].cElements = 10;

	pSA= SafeArrayCreate (VT_VARIANT, 1, dimensions);
	for (i= dimensions [0].lLbound; i< (dimensions [0].cElements + dimensions [0].lLbound); i++) {
		VARIANT vOut;
		VariantInit (&vOut);
		vOut.vt = VT_BSTR;
		_ltoa (i,buffer,10);
		vOut.bstrVal= marshal_bstr_alloc (buffer);
		indices [0] = i;
		if ((hr = SafeArrayPutElement (pSA, indices, &vOut)) != S_OK) {
			VariantClear (&vOut);
			SafeArrayDestroy (pSA);
			return hr;
		}
		VariantClear (&vOut);
	}
	*safearray = pSA;
	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_out_2dim_vt_i4 (SAFEARRAY** safearray)
{
	/* Create a two-dimensional array of 4x3 variants filled with 11, 12, 13, etc. */
	SAFEARRAY *pSA;
	SAFEARRAYBOUND dimensions [2];
	long i, j;
	HRESULT hr = S_OK;
	long indices [2];

	dimensions [0].lLbound = 0;
	dimensions [0].cElements = 4;
	dimensions [1].lLbound = 0;
	dimensions [1].cElements = 3;

	pSA= SafeArrayCreate(VT_VARIANT, 2, dimensions);
	for (i= dimensions [0].lLbound; i< (dimensions [0].cElements + dimensions [0].lLbound); i++) {
		for (j= dimensions [1].lLbound; j< (dimensions [1].cElements + dimensions [1].lLbound); j++) {
			VARIANT vOut;
			VariantInit (&vOut);
			vOut.vt = VT_I4;
			vOut.lVal = (i+1)*10+(j+1);
			indices [0] = i;
			indices [1] = j;
			if ((hr = SafeArrayPutElement (pSA, indices, &vOut)) != S_OK) {
				VariantClear (&vOut);
				SafeArrayDestroy (pSA);
				return hr;
			}
			VariantClear (&vOut);  // does a deep destroy of source VARIANT	
		}
	}
	*safearray = pSA;
	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_out_4dim_vt_i4 (SAFEARRAY** safearray)
{
	/* Create a four-dimensional array of 10x3x6x7 variants filled with their indices */
	/* Also use non zero lower bounds                                                 */
	SAFEARRAY *pSA;
	SAFEARRAYBOUND dimensions [4];
	long i;
	HRESULT hr = S_OK;
	VARIANT *pData;

	dimensions [0].lLbound = 15;
	dimensions [0].cElements = 10;
	dimensions [1].lLbound = 20;
	dimensions [1].cElements = 3;
	dimensions [2].lLbound = 5;
	dimensions [2].cElements = 6;
	dimensions [3].lLbound = 12;
	dimensions [3].cElements = 7;

	pSA= SafeArrayCreate (VT_VARIANT, 4, dimensions);

	SafeArrayAccessData (pSA, (void **)&pData);

	for (i= 0; i< 10*3*6*7; i++) {
		VariantInit(&pData [i]);
		pData [i].vt = VT_I4;
		pData [i].lVal = i;
	}
	SafeArrayUnaccessData (pSA);
	*safearray = pSA;
	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_byval_1dim_empty (SAFEARRAY* safearray)
{
	/* Check that array is one dimensional and empty */

	UINT dim;
	long lbound, ubound;
	
	dim = SafeArrayGetDim (safearray);
	if (dim != 1)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound);
	SafeArrayGetUBound (safearray, 1, &ubound);

	if ((lbound > 0) || (ubound > 0))
		return 1;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_byval_1dim_vt_i4 (SAFEARRAY* safearray)
{
	/* Check that array is one dimensional containing integers from 1 to 10 */

	UINT dim;
	long lbound, ubound;
	VARIANT *pData;	
	long i;
	int result=0;

	dim = SafeArrayGetDim (safearray);
	if (dim != 1)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound);
	SafeArrayGetUBound (safearray, 1, &ubound);

	if ((lbound != 0) || (ubound != 9))
		return 1;

	SafeArrayAccessData (safearray, (void **)&pData);
	for (i= lbound; i <= ubound; i++) {
		if ((VariantChangeType (&pData [i], &pData [i], VARIANT_NOUSEROVERRIDE, VT_I4) != S_OK) || (pData [i].lVal != i + 1))
			result = 1;
	}
	SafeArrayUnaccessData (safearray);

	return result;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_byval_1dim_vt_mixed (SAFEARRAY* safearray)
{
	/* Check that array is one dimensional containing integers mixed with strings from 0 to 12 */

	UINT dim;
	long lbound, ubound;
	VARIANT *pData;	
	long i;
	long indices [1];
	VARIANT element;
	int result=0;

	VariantInit (&element);

	dim = SafeArrayGetDim (safearray);
	if (dim != 1)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound);
	SafeArrayGetUBound (safearray, 1, &ubound);
		
	if ((lbound != 0) || (ubound != 12))
		return 1;

	SafeArrayAccessData (safearray, (void **)&pData);
	for (i= lbound; i <= ubound; i++) {
		if ((i%2 == 0) && (pData [i].vt != VT_I4))
			result = 1;
		if ((i%2 == 1) && (pData [i].vt != VT_BSTR))
			result = 1;
		if ((VariantChangeType (&pData [i], &pData [i], VARIANT_NOUSEROVERRIDE, VT_I4) != S_OK) || (pData [i].lVal != i))
			result = 1;
	}
	SafeArrayUnaccessData (safearray);

	/* Change the first element of the array to verify that [in] parameters are not marshalled back to the managed side */

	indices [0] = 0;
	element.vt = VT_I4;
	element.lVal = 333;
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	return result;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_byval_2dim_vt_i4 (SAFEARRAY* safearray)
{
	/* Check that array is one dimensional containing integers mixed with strings from 0 to 12 */

	UINT dim;
	long lbound1, ubound1, lbound2, ubound2;
	long i, j, failed;
	long indices [2];
	VARIANT element;

	VariantInit (&element);

	dim = SafeArrayGetDim (safearray);
	if (dim != 2)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound1);
	SafeArrayGetUBound (safearray, 1, &ubound1);

	if ((lbound1 != 0) || (ubound1 != 1))
		return 1;

	SafeArrayGetLBound (safearray, 2, &lbound2);
	SafeArrayGetUBound (safearray, 2, &ubound2);

	if ((lbound2 != 0) || (ubound2 != 3)) {
		return 1;
	}

	for (i= lbound1; i <= ubound1; i++) {
		indices [0] = i;
		for (j= lbound2; j <= ubound2; j++) {
			indices [1] = j;
			if (SafeArrayGetElement (safearray, indices, &element) != S_OK)
				return 1;
			failed = ((element.vt != VT_I4) || (element.lVal != 10*(i+1)+(j+1)));
			VariantClear (&element);
			if (failed)
				return 1;
		}
	}

	/* Change the first element of the array to verify that [in] parameters are not marshalled back to the managed side */

	indices [0] = 0;
	indices [1] = 0;
	element.vt = VT_I4;
	element.lVal = 333;
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_byval_3dim_vt_bstr (SAFEARRAY* safearray)
{
	/* Check that array is one dimensional containing integers mixed with strings from 0 to 12 */

	UINT dim;
	long lbound1, ubound1, lbound2, ubound2, lbound3, ubound3;
	long i, j, k, failed;
	long indices [3];
	VARIANT element;

	VariantInit (&element);

	dim = SafeArrayGetDim (safearray);
	if (dim != 3)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound1);
	SafeArrayGetUBound (safearray, 1, &ubound1);

	if ((lbound1 != 0) || (ubound1 != 1))
		return 1;

	SafeArrayGetLBound (safearray, 2, &lbound2);
	SafeArrayGetUBound (safearray, 2, &ubound2);

	if ((lbound2 != 0) || (ubound2 != 1))
		return 1;

	SafeArrayGetLBound (safearray, 3, &lbound3);
	SafeArrayGetUBound (safearray, 3, &ubound3);

	if ((lbound3 != 0) || (ubound3 != 2))
		return 1;

	for (i= lbound1; i <= ubound1; i++) {
		indices [0] = i;
		for (j= lbound2; j <= ubound2; j++) {
			indices [1] = j;
		for (k= lbound3; k <= ubound3; k++) {
				indices [2] = k;
				if (SafeArrayGetElement (safearray, indices, &element) != S_OK)
					return 1;
				failed = ((element.vt != VT_BSTR) 
					|| (VariantChangeType (&element, &element, VARIANT_NOUSEROVERRIDE, VT_I4) != S_OK) 
					|| (element.lVal != 100*(i+1)+10*(j+1)+(k+1)));
				VariantClear (&element);
				if (failed)
					return 1;
			}
		}
	}

	/* Change the first element of the array to verify that [in] parameters are not marshalled back to the managed side */

	indices [0] = 0;
	indices [1] = 0;
	indices [2] = 0;
	element.vt = VT_BSTR;
	element.bstrVal = SysAllocString(L"Should not be copied");
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_byref_3dim_vt_bstr (SAFEARRAY** safearray)
{
	return mono_test_marshal_safearray_in_byval_3dim_vt_bstr (*safearray);
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_out_byref_1dim_empty (SAFEARRAY** safearray)
{
	/* Check that the input array is what is expected and change it so the caller can check */
	/* correct marshalling back to managed code                                             */

	UINT dim;
	long lbound, ubound;
	SAFEARRAYBOUND dimensions [1];
	long i;
	wchar_t buffer [20];
	HRESULT hr = S_OK;
	long indices [1];

	/* Check that in array is one dimensional and empty */

	dim = SafeArrayGetDim (*safearray);
	if (dim != 1) {
		return 1;
	}

	SafeArrayGetLBound (*safearray, 1, &lbound);
	SafeArrayGetUBound (*safearray, 1, &ubound);
		
	if ((lbound > 0) || (ubound > 0)) {
		return 1;
	}

	/* Re-dimension the array and return a one-dimensional array of 8 variants filled with "0" to "7" */

	dimensions [0].lLbound = 0;
	dimensions [0].cElements = 8;

	hr = SafeArrayRedim (*safearray, dimensions);
	if (hr != S_OK)
		return 1;

	for (i= dimensions [0].lLbound; i< (dimensions [0].lLbound + dimensions [0].cElements); i++) {
		VARIANT vOut;
		VariantInit (&vOut);
		vOut.vt = VT_BSTR;
		_ltow (i,buffer,10);
		vOut.bstrVal = SysAllocString (buffer);
		indices [0] = i;
		if ((hr = SafeArrayPutElement (*safearray, indices, &vOut)) != S_OK) {
			VariantClear (&vOut);
			SafeArrayDestroy (*safearray);
			return hr;
		}
		VariantClear (&vOut);
	}
	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_out_byref_3dim_vt_bstr (SAFEARRAY** safearray)
{
	/* Check that the input array is what is expected and change it so the caller can check */
	/* correct marshalling back to managed code                                             */

	UINT dim;
	long lbound1, ubound1, lbound2, ubound2, lbound3, ubound3;
	SAFEARRAYBOUND dimensions [1];
	long i, j, k, failed;
	wchar_t buffer [20];
	HRESULT hr = S_OK;
	long indices [3];
	VARIANT element;

	VariantInit (&element);

	/* Check that in array is three dimensional and contains the expected values */

	dim = SafeArrayGetDim (*safearray);
	if (dim != 3)
		return 1;

	SafeArrayGetLBound (*safearray, 1, &lbound1);
	SafeArrayGetUBound (*safearray, 1, &ubound1);

	if ((lbound1 != 0) || (ubound1 != 1))
		return 1;

	SafeArrayGetLBound (*safearray, 2, &lbound2);
	SafeArrayGetUBound (*safearray, 2, &ubound2);

	if ((lbound2 != 0) || (ubound2 != 1))
		return 1;

	SafeArrayGetLBound (*safearray, 3, &lbound3);
	SafeArrayGetUBound (*safearray, 3, &ubound3);

	if ((lbound3 != 0) || (ubound3 != 2))
		return 1;

	for (i= lbound1; i <= ubound1; i++) {
		indices [0] = i;
		for (j= lbound2; j <= ubound2; j++) {
			indices [1] = j;
			for (k= lbound3; k <= ubound3; k++) {
				indices [2] = k;
				if (SafeArrayGetElement (*safearray, indices, &element) != S_OK)
					return 1;
				failed = ((element.vt != VT_BSTR) 
					|| (VariantChangeType (&element, &element, VARIANT_NOUSEROVERRIDE, VT_I4) != S_OK) 
					|| (element.lVal != 100*(i+1)+10*(j+1)+(k+1)));
				VariantClear (&element);
				if (failed)
					return 1;
			}
		}
	}

	hr = SafeArrayDestroy (*safearray);
	if (hr != S_OK)
		return 1;

	/* Return a new one-dimensional array of 8 variants filled with "0" to "7" */

	dimensions [0].lLbound = 0;
	dimensions [0].cElements = 8;

	*safearray = SafeArrayCreate (VT_VARIANT, 1, dimensions);

	for (i= dimensions [0].lLbound; i< (dimensions [0].lLbound + dimensions [0].cElements); i++) {
		VARIANT vOut;
		VariantInit (&vOut);
		vOut.vt = VT_BSTR;
		_ltow (i,buffer,10);
		vOut.bstrVal = SysAllocString (buffer);
		indices [0] = i;
		if ((hr = SafeArrayPutElement (*safearray, indices, &vOut)) != S_OK) {
			VariantClear (&vOut);
			SafeArrayDestroy (*safearray);
			return hr;
		}
		VariantClear (&vOut);
	}
	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_out_byref_1dim_vt_i4 (SAFEARRAY** safearray)
{
	/* Check that the input array is what is expected and change it so the caller can check */
	/* correct marshalling back to managed code                                             */

	UINT dim;
	long lbound1, ubound1;
	long i, failed;
	HRESULT hr = S_OK;
	long indices [1];
	VARIANT element;
	
	VariantInit (&element);

	/* Check that in array is one dimensional and contains the expected value */

	dim = SafeArrayGetDim (*safearray);
	if (dim != 1)
		return 1;

	SafeArrayGetLBound (*safearray, 1, &lbound1);
	SafeArrayGetUBound (*safearray, 1, &ubound1);

	ubound1 = 1;
	if ((lbound1 != 0) || (ubound1 != 1))
		return 1;
	ubound1 = 0;

	for (i= lbound1; i <= ubound1; i++) {
		indices [0] = i;
		if (SafeArrayGetElement (*safearray, indices, &element) != S_OK)
			return 1;
		failed = (element.vt != VT_I4) || (element.lVal != i+1);
		VariantClear (&element);
		if (failed)
			return 1;
	}

	/* Change one of the elements of the array to verify that [out] parameter is marshalled back to the managed side */

	indices [0] = 0;
	element.vt = VT_I4;
	element.lVal = -1;
	SafeArrayPutElement (*safearray, indices, &element);
	VariantClear (&element);

	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_out_byval_1dim_vt_i4 (SAFEARRAY* safearray)
{
	/* Check that the input array is what is expected and change it so the caller can check */
	/* correct marshalling back to managed code                                             */

	UINT dim;
	long lbound1, ubound1;
	SAFEARRAYBOUND dimensions [1];
	long i, failed;
	HRESULT hr = S_OK;
	long indices [1];
	VARIANT element;

	VariantInit (&element);

	/* Check that in array is one dimensional and contains the expected value */

	dim = SafeArrayGetDim (safearray);
	if (dim != 1)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound1);
	SafeArrayGetUBound (safearray, 1, &ubound1);
		
	if ((lbound1 != 0) || (ubound1 != 0))
		return 1;

	for (i= lbound1; i <= ubound1; i++) {
		indices [0] = i;
		if (SafeArrayGetElement (safearray, indices, &element) != S_OK)
			return 1;
		failed = (element.vt != VT_I4) || (element.lVal != i+1);
		VariantClear (&element);
		if (failed)
			return 1;
	}

	/* Change the array to verify how [out] parameter is marshalled back to the managed side */

	/* Redimension the array */
	dimensions [0].lLbound = lbound1;
	dimensions [0].cElements = 2;
	hr = SafeArrayRedim(safearray, dimensions);

	indices [0] = 0;
	element.vt = VT_I4;
	element.lVal = 12345;
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	indices [0] = 1;
	element.vt = VT_I4;
	element.lVal = -12345;
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_in_out_byval_3dim_vt_bstr (SAFEARRAY* safearray)
{
	/* Check that the input array is what is expected and change it so the caller can check */
	/* correct marshalling back to managed code                                             */

	UINT dim;
	long lbound1, ubound1, lbound2, ubound2, lbound3, ubound3;
	long i, j, k, failed;
	HRESULT hr = S_OK;
	long indices [3];
	VARIANT element;

	VariantInit (&element);

	/* Check that in array is three dimensional and contains the expected values */

	dim = SafeArrayGetDim (safearray);
	if (dim != 3)
		return 1;

	SafeArrayGetLBound (safearray, 1, &lbound1);
	SafeArrayGetUBound (safearray, 1, &ubound1);

	if ((lbound1 != 0) || (ubound1 != 1))
		return 1;

	SafeArrayGetLBound (safearray, 2, &lbound2);
	SafeArrayGetUBound (safearray, 2, &ubound2);

	if ((lbound2 != 0) || (ubound2 != 1))
		return 1;

	SafeArrayGetLBound (safearray, 3, &lbound3);
	SafeArrayGetUBound (safearray, 3, &ubound3);

	if ((lbound3 != 0) || (ubound3 != 2))
		return 1;

	for (i= lbound1; i <= ubound1; i++) {
		indices [0] = i;
		for (j= lbound2; j <= ubound2; j++) {
			indices [1] = j;
			for (k= lbound3; k <= ubound3; k++) {
				indices [2] = k;
				if (SafeArrayGetElement (safearray, indices, &element) != S_OK)
					return 1;
				failed = ((element.vt != VT_BSTR) 
					|| (VariantChangeType (&element, &element, VARIANT_NOUSEROVERRIDE, VT_I4) != S_OK) 
					|| (element.lVal != 100*(i+1)+10*(j+1)+(k+1)));
				VariantClear (&element);
				if (failed)
					return 1;
			}
		}
	}

	/* Change the elements of the array to verify that [out] parameter is marshalled back to the managed side */

	indices [0] = 1;
	indices [1] = 1;
	indices [2] = 2;
	element.vt = VT_I4;
	element.lVal = 333;
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	indices [0] = 1;
	indices [1] = 1;
	indices [2] = 1;
	element.vt = VT_I4;
	element.lVal = 111;
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	indices [0] = 0;
	indices [1] = 1;
	indices [2] = 0;
	element.vt = VT_BSTR;
	element.bstrVal = marshal_bstr_alloc("ABCDEFG");
	SafeArrayPutElement (safearray, indices, &element);
	VariantClear (&element);

	return hr;
}

LIBTEST_API int STDCALL 
mono_test_marshal_safearray_mixed(
		SAFEARRAY  *safearray1,
		SAFEARRAY **safearray2,
		SAFEARRAY  *safearray3,
		SAFEARRAY **safearray4
		)
{
	HRESULT hr = S_OK;

	/* Initialize out parameters */
	*safearray2 = NULL;

	/* array1: Check that in array is one dimensional and contains the expected value */
	hr = mono_test_marshal_safearray_in_out_byval_1dim_vt_i4 (safearray1);

	/* array2: Fill in with some values to check on the managed side */
	if (hr == S_OK)
		hr = mono_test_marshal_safearray_out_1dim_vt_bstr (safearray2);

	/* array3: Check that in array is one dimensional and contains the expected value */
	if (hr == S_OK)
		hr = mono_test_marshal_safearray_in_byval_1dim_vt_mixed(safearray3);

	/* array4: Check input values and fill in with some values to check on the managed side */
	if (hr == S_OK)
		hr = mono_test_marshal_safearray_in_out_byref_3dim_vt_bstr(safearray4);

	return hr;
}

LIBTEST_API int STDCALL
mono_test_marshal_safearray_in_ccw(MonoComObject *pUnk)
{
	SAFEARRAY *array;
	VARIANT var;
	long index;
	int ret;

	array = SafeArrayCreateVector(VT_VARIANT, 0, 2);

	var.vt = VT_BSTR;
	var.bstrVal = marshal_bstr_alloc("Test");
	index = 0;
	SafeArrayPutElement(array, &index, &var);

	var.vt = VT_I4;
	var.intVal = 2345;
	index = 1;
	SafeArrayPutElement(array, &index, &var);

	ret = pUnk->vtbl->ArrayIn (pUnk, (void *)array);
	if (!ret)
		ret = pUnk->vtbl->ArrayIn2 (pUnk, (void *)array);
	if (!ret)
		ret = pUnk->vtbl->ArrayIn3 (pUnk, (void *)array);

	SafeArrayDestroy(array);

	return ret;
}

LIBTEST_API int STDCALL
mono_test_marshal_lparray_out_ccw(MonoComObject *pUnk)
{
	guint32 array, result;
	int ret;

	ret = pUnk->vtbl->ArrayOut (pUnk, &array, &result);
	if (ret)
		return ret;
	if (array != 55)
		return 1;
	if (result != 1)
		return 2;

	ret = pUnk->vtbl->ArrayOut (pUnk, NULL, &result);
	if (ret)
		return ret;
	if (result != 0)
		return 3;

	return 0;
}

#endif

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
	g_assert (res == 0);
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
	g_assert (res == 0);
	pthread_join (t, NULL);

	return call_managed_res;
#endif
}

typedef int (STDCALL *Callback) (void);

static Callback callback;

LIBTEST_API void STDCALL 
mono_test_marshal_set_callback (Callback cb)
{
	callback = cb;
}

LIBTEST_API int STDCALL 
mono_test_marshal_call_callback (void)
{
	return callback ();
}

LIBTEST_API int STDCALL
mono_test_marshal_lpstr (char *str)
{
	return strcmp ("ABC", str);
}

LIBTEST_API int STDCALL
mono_test_marshal_lpwstr (gunichar2 *str)
{
	char *s;
	int res;

	s = g_utf16_to_utf8 (str, -1, NULL, NULL, NULL);
	res = strcmp ("ABC", s);
	g_free (s);

	return res;
}

LIBTEST_API char* STDCALL
mono_test_marshal_return_lpstr (void)
{
	char *res = (char *)marshal_alloc (4);
	strcpy (res, "XYZ");
	return res;
}

LIBTEST_API gunichar2* STDCALL
mono_test_marshal_return_lpwstr (void)
{
	gunichar2 *res = (gunichar2 *)marshal_alloc (8);
	gunichar2* tmp = g_utf8_to_utf16 ("XYZ", -1, NULL, NULL, NULL);

	memcpy (res, tmp, 8);
	g_free (tmp);

	return res;
}

typedef
#if defined (HOST_WIN32) && defined (HOST_X86) && defined (__GNUC__)
// Workaround gcc ABI bug. It returns the struct in ST0 instead of edx:eax.
// Mono and Visual C++ agree.
union
#else
struct
#endif
{
	double d;
} SingleDoubleStruct;

LIBTEST_API SingleDoubleStruct STDCALL
mono_test_marshal_return_single_double_struct (void)
{
	SingleDoubleStruct res = {3.0};
	return res;
}

LIBTEST_API int STDCALL
mono_test_has_thiscall_globals (void)
{
// Visual C++ does not accept __thiscall on global functions, only
// member function and function pointers. Gcc accepts it also on global functions.
#if defined (HOST_X86) && defined (HOST_WIN32) && !defined (_MSC_VER)
	return 1;
#else
	return 0;
#endif
}

LIBTEST_API int STDCALL
mono_test_has_thiscall_pointers (void)
{
#if defined (HOST_X86) && defined (HOST_WIN32)
	return 1;
#else
	return 0;
#endif
}

LIBTEST_API int
#ifndef _MSC_VER
__thiscall
#endif
_mono_test_native_thiscall1 (int arg)
{
	return arg;
}

LIBTEST_API int
#ifndef _MSC_VER
__thiscall
#endif
_mono_test_native_thiscall2 (int arg, int arg2)
{
	return arg + (arg2^1);
}

LIBTEST_API int
#ifndef _MSC_VER
__thiscall
#endif
_mono_test_native_thiscall3 (int arg, int arg2, int arg3)
{
	return arg + (arg2^1) + (arg3^2);
}

typedef int (
#ifndef _MSC_VER
__thiscall
#endif
*ThiscallFunction)(int arg, int arg2);

LIBTEST_API ThiscallFunction STDCALL
mono_test_get_native_thiscall2 (void)
{
	return _mono_test_native_thiscall2;
}

LIBTEST_API int STDCALL
_mono_test_managed_thiscall1 (int (__thiscall*fn)(int), int arg)
{
	return fn(arg);
}

LIBTEST_API int STDCALL
_mono_test_managed_thiscall2 (int (__thiscall*fn)(int,int), int arg, int arg2)
{
	return fn(arg, arg2);
}

LIBTEST_API int STDCALL
_mono_test_managed_thiscall3 (int (__thiscall*fn)(int,int,int), int arg, int arg2, int arg3)
{
	return fn(arg, arg2, arg3);
}

typedef struct {
	char f1;
} sbyte1;

LIBTEST_API sbyte1 STDCALL
mono_return_sbyte1 (sbyte1 s1, int addend) {
	if (s1.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte1 s1.f1: got %d but expected %d\n", s1.f1, 1);
	}
	s1.f1+=addend; 
	return s1;
}

typedef struct {
	char f1,f2;
} sbyte2;

LIBTEST_API sbyte2 STDCALL
mono_return_sbyte2 (sbyte2 s2, int addend) {
	if (s2.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte2 s2.f1: got %d but expected %d\n", s2.f1, 1);
	}
	if (s2.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte2 s2.f2: got %d but expected %d\n", s2.f2, 2);
	}
	s2.f1+=addend; s2.f2+=addend; 
	return s2;
}

typedef struct {
	char f1,f2,f3;
} sbyte3;

LIBTEST_API sbyte3 STDCALL
mono_return_sbyte3 (sbyte3 s3, int addend) {
	if (s3.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte3 s3.f1: got %d but expected %d\n", s3.f1, 1);
	}
	if (s3.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte3 s3.f2: got %d but expected %d\n", s3.f2, 2);
	}
	if (s3.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte3 s3.f3: got %d but expected %d\n", s3.f3, 3);
	}
	s3.f1+=addend; s3.f2+=addend; s3.f3+=addend; 
	return s3;
}

typedef struct {
	char f1,f2,f3,f4;
} sbyte4;

LIBTEST_API sbyte4 STDCALL
mono_return_sbyte4 (sbyte4 s4, int addend) {
	if (s4.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte4 s4.f1: got %d but expected %d\n", s4.f1, 1);
	}
	if (s4.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte4 s4.f2: got %d but expected %d\n", s4.f2, 2);
	}
	if (s4.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte4 s4.f3: got %d but expected %d\n", s4.f3, 3);
	}
	if (s4.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte4 s4.f4: got %d but expected %d\n", s4.f4, 4);
	}
	s4.f1+=addend; s4.f2+=addend; s4.f3+=addend; s4.f4+=addend; 
	return s4;
}

typedef struct {
	char f1,f2,f3,f4,f5;
} sbyte5;

LIBTEST_API sbyte5 STDCALL
mono_return_sbyte5 (sbyte5 s5, int addend) {
	if (s5.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte5 s5.f1: got %d but expected %d\n", s5.f1, 1);
	}
	if (s5.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte5 s5.f2: got %d but expected %d\n", s5.f2, 2);
	}
	if (s5.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte5 s5.f3: got %d but expected %d\n", s5.f3, 3);
	}
	if (s5.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte5 s5.f4: got %d but expected %d\n", s5.f4, 4);
	}
	if (s5.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte5 s5.f5: got %d but expected %d\n", s5.f5, 5);
	}
	s5.f1+=addend; s5.f2+=addend; s5.f3+=addend; s5.f4+=addend; s5.f5+=addend; 
	return s5;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6;
} sbyte6;

LIBTEST_API sbyte6 STDCALL
mono_return_sbyte6 (sbyte6 s6, int addend) {
	if (s6.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte6 s6.f1: got %d but expected %d\n", s6.f1, 1);
	}
	if (s6.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte6 s6.f2: got %d but expected %d\n", s6.f2, 2);
	}
	if (s6.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte6 s6.f3: got %d but expected %d\n", s6.f3, 3);
	}
	if (s6.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte6 s6.f4: got %d but expected %d\n", s6.f4, 4);
	}
	if (s6.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte6 s6.f5: got %d but expected %d\n", s6.f5, 5);
	}
	if (s6.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte6 s6.f6: got %d but expected %d\n", s6.f6, 6);
	}
	s6.f1+=addend; s6.f2+=addend; s6.f3+=addend; s6.f4+=addend; s6.f5+=addend; s6.f6+=addend; 
	return s6;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7;
} sbyte7;

LIBTEST_API sbyte7 STDCALL
mono_return_sbyte7 (sbyte7 s7, int addend) {
	if (s7.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte7 s7.f1: got %d but expected %d\n", s7.f1, 1);
	}
	if (s7.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte7 s7.f2: got %d but expected %d\n", s7.f2, 2);
	}
	if (s7.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte7 s7.f3: got %d but expected %d\n", s7.f3, 3);
	}
	if (s7.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte7 s7.f4: got %d but expected %d\n", s7.f4, 4);
	}
	if (s7.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte7 s7.f5: got %d but expected %d\n", s7.f5, 5);
	}
	if (s7.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte7 s7.f6: got %d but expected %d\n", s7.f6, 6);
	}
	if (s7.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte7 s7.f7: got %d but expected %d\n", s7.f7, 7);
	}
	s7.f1+=addend; s7.f2+=addend; s7.f3+=addend; s7.f4+=addend; s7.f5+=addend; s7.f6+=addend; s7.f7+=addend; 
	return s7;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8;
} sbyte8;

LIBTEST_API sbyte8 STDCALL
mono_return_sbyte8 (sbyte8 s8, int addend) {
	if (s8.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte8 s8.f1: got %d but expected %d\n", s8.f1, 1);
	}
	if (s8.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte8 s8.f2: got %d but expected %d\n", s8.f2, 2);
	}
	if (s8.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte8 s8.f3: got %d but expected %d\n", s8.f3, 3);
	}
	if (s8.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte8 s8.f4: got %d but expected %d\n", s8.f4, 4);
	}
	if (s8.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte8 s8.f5: got %d but expected %d\n", s8.f5, 5);
	}
	if (s8.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte8 s8.f6: got %d but expected %d\n", s8.f6, 6);
	}
	if (s8.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte8 s8.f7: got %d but expected %d\n", s8.f7, 7);
	}
	if (s8.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte8 s8.f8: got %d but expected %d\n", s8.f8, 8);
	}
	s8.f1+=addend; s8.f2+=addend; s8.f3+=addend; s8.f4+=addend; s8.f5+=addend; s8.f6+=addend; s8.f7+=addend; s8.f8+=addend; 
	return s8;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9;
} sbyte9;

LIBTEST_API sbyte9 STDCALL
mono_return_sbyte9 (sbyte9 s9, int addend) {
	if (s9.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte9 s9.f1: got %d but expected %d\n", s9.f1, 1);
	}
	if (s9.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte9 s9.f2: got %d but expected %d\n", s9.f2, 2);
	}
	if (s9.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte9 s9.f3: got %d but expected %d\n", s9.f3, 3);
	}
	if (s9.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte9 s9.f4: got %d but expected %d\n", s9.f4, 4);
	}
	if (s9.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte9 s9.f5: got %d but expected %d\n", s9.f5, 5);
	}
	if (s9.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte9 s9.f6: got %d but expected %d\n", s9.f6, 6);
	}
	if (s9.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte9 s9.f7: got %d but expected %d\n", s9.f7, 7);
	}
	if (s9.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte9 s9.f8: got %d but expected %d\n", s9.f8, 8);
	}
	if (s9.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte9 s9.f9: got %d but expected %d\n", s9.f9, 9);
	}
	s9.f1+=addend; s9.f2+=addend; s9.f3+=addend; s9.f4+=addend; s9.f5+=addend; s9.f6+=addend; s9.f7+=addend; s9.f8+=addend; s9.f9+=addend; 
	return s9;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10;
} sbyte10;

LIBTEST_API sbyte10 STDCALL
mono_return_sbyte10 (sbyte10 s10, int addend) {
	if (s10.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte10 s10.f1: got %d but expected %d\n", s10.f1, 1);
	}
	if (s10.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte10 s10.f2: got %d but expected %d\n", s10.f2, 2);
	}
	if (s10.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte10 s10.f3: got %d but expected %d\n", s10.f3, 3);
	}
	if (s10.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte10 s10.f4: got %d but expected %d\n", s10.f4, 4);
	}
	if (s10.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte10 s10.f5: got %d but expected %d\n", s10.f5, 5);
	}
	if (s10.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte10 s10.f6: got %d but expected %d\n", s10.f6, 6);
	}
	if (s10.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte10 s10.f7: got %d but expected %d\n", s10.f7, 7);
	}
	if (s10.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte10 s10.f8: got %d but expected %d\n", s10.f8, 8);
	}
	if (s10.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte10 s10.f9: got %d but expected %d\n", s10.f9, 9);
	}
	if (s10.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte10 s10.f10: got %d but expected %d\n", s10.f10, 10);
	}
	s10.f1+=addend; s10.f2+=addend; s10.f3+=addend; s10.f4+=addend; s10.f5+=addend; s10.f6+=addend; s10.f7+=addend; s10.f8+=addend; s10.f9+=addend; s10.f10+=addend; 
	return s10;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11;
} sbyte11;

LIBTEST_API sbyte11 STDCALL
mono_return_sbyte11 (sbyte11 s11, int addend) {
	if (s11.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte11 s11.f1: got %d but expected %d\n", s11.f1, 1);
	}
	if (s11.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte11 s11.f2: got %d but expected %d\n", s11.f2, 2);
	}
	if (s11.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte11 s11.f3: got %d but expected %d\n", s11.f3, 3);
	}
	if (s11.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte11 s11.f4: got %d but expected %d\n", s11.f4, 4);
	}
	if (s11.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte11 s11.f5: got %d but expected %d\n", s11.f5, 5);
	}
	if (s11.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte11 s11.f6: got %d but expected %d\n", s11.f6, 6);
	}
	if (s11.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte11 s11.f7: got %d but expected %d\n", s11.f7, 7);
	}
	if (s11.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte11 s11.f8: got %d but expected %d\n", s11.f8, 8);
	}
	if (s11.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte11 s11.f9: got %d but expected %d\n", s11.f9, 9);
	}
	if (s11.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte11 s11.f10: got %d but expected %d\n", s11.f10, 10);
	}
	if (s11.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte11 s11.f11: got %d but expected %d\n", s11.f11, 11);
	}
	s11.f1+=addend; s11.f2+=addend; s11.f3+=addend; s11.f4+=addend; s11.f5+=addend; s11.f6+=addend; s11.f7+=addend; s11.f8+=addend; s11.f9+=addend; s11.f10+=addend; s11.f11+=addend; 
	return s11;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12;
} sbyte12;

LIBTEST_API sbyte12 STDCALL
mono_return_sbyte12 (sbyte12 s12, int addend) {
	if (s12.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte12 s12.f1: got %d but expected %d\n", s12.f1, 1);
	}
	if (s12.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte12 s12.f2: got %d but expected %d\n", s12.f2, 2);
	}
	if (s12.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte12 s12.f3: got %d but expected %d\n", s12.f3, 3);
	}
	if (s12.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte12 s12.f4: got %d but expected %d\n", s12.f4, 4);
	}
	if (s12.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte12 s12.f5: got %d but expected %d\n", s12.f5, 5);
	}
	if (s12.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte12 s12.f6: got %d but expected %d\n", s12.f6, 6);
	}
	if (s12.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte12 s12.f7: got %d but expected %d\n", s12.f7, 7);
	}
	if (s12.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte12 s12.f8: got %d but expected %d\n", s12.f8, 8);
	}
	if (s12.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte12 s12.f9: got %d but expected %d\n", s12.f9, 9);
	}
	if (s12.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte12 s12.f10: got %d but expected %d\n", s12.f10, 10);
	}
	if (s12.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte12 s12.f11: got %d but expected %d\n", s12.f11, 11);
	}
	if (s12.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte12 s12.f12: got %d but expected %d\n", s12.f12, 12);
	}
	s12.f1+=addend; s12.f2+=addend; s12.f3+=addend; s12.f4+=addend; s12.f5+=addend; s12.f6+=addend; s12.f7+=addend; s12.f8+=addend; s12.f9+=addend; s12.f10+=addend; s12.f11+=addend; s12.f12+=addend; 
	return s12;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13;
} sbyte13;

LIBTEST_API sbyte13 STDCALL
mono_return_sbyte13 (sbyte13 s13, int addend) {
	if (s13.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte13 s13.f1: got %d but expected %d\n", s13.f1, 1);
	}
	if (s13.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte13 s13.f2: got %d but expected %d\n", s13.f2, 2);
	}
	if (s13.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte13 s13.f3: got %d but expected %d\n", s13.f3, 3);
	}
	if (s13.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte13 s13.f4: got %d but expected %d\n", s13.f4, 4);
	}
	if (s13.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte13 s13.f5: got %d but expected %d\n", s13.f5, 5);
	}
	if (s13.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte13 s13.f6: got %d but expected %d\n", s13.f6, 6);
	}
	if (s13.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte13 s13.f7: got %d but expected %d\n", s13.f7, 7);
	}
	if (s13.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte13 s13.f8: got %d but expected %d\n", s13.f8, 8);
	}
	if (s13.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte13 s13.f9: got %d but expected %d\n", s13.f9, 9);
	}
	if (s13.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte13 s13.f10: got %d but expected %d\n", s13.f10, 10);
	}
	if (s13.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte13 s13.f11: got %d but expected %d\n", s13.f11, 11);
	}
	if (s13.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte13 s13.f12: got %d but expected %d\n", s13.f12, 12);
	}
	if (s13.f13 != 13) {
		fprintf(stderr, "mono_return_sbyte13 s13.f13: got %d but expected %d\n", s13.f13, 13);
	}
	s13.f1+=addend; s13.f2+=addend; s13.f3+=addend; s13.f4+=addend; s13.f5+=addend; s13.f6+=addend; s13.f7+=addend; s13.f8+=addend; s13.f9+=addend; s13.f10+=addend; s13.f11+=addend; s13.f12+=addend; s13.f13+=addend; 
	return s13;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14;
} sbyte14;

LIBTEST_API sbyte14 STDCALL
mono_return_sbyte14 (sbyte14 s14, int addend) {
	if (s14.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte14 s14.f1: got %d but expected %d\n", s14.f1, 1);
	}
	if (s14.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte14 s14.f2: got %d but expected %d\n", s14.f2, 2);
	}
	if (s14.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte14 s14.f3: got %d but expected %d\n", s14.f3, 3);
	}
	if (s14.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte14 s14.f4: got %d but expected %d\n", s14.f4, 4);
	}
	if (s14.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte14 s14.f5: got %d but expected %d\n", s14.f5, 5);
	}
	if (s14.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte14 s14.f6: got %d but expected %d\n", s14.f6, 6);
	}
	if (s14.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte14 s14.f7: got %d but expected %d\n", s14.f7, 7);
	}
	if (s14.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte14 s14.f8: got %d but expected %d\n", s14.f8, 8);
	}
	if (s14.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte14 s14.f9: got %d but expected %d\n", s14.f9, 9);
	}
	if (s14.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte14 s14.f10: got %d but expected %d\n", s14.f10, 10);
	}
	if (s14.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte14 s14.f11: got %d but expected %d\n", s14.f11, 11);
	}
	if (s14.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte14 s14.f12: got %d but expected %d\n", s14.f12, 12);
	}
	if (s14.f13 != 13) {
		fprintf(stderr, "mono_return_sbyte14 s14.f13: got %d but expected %d\n", s14.f13, 13);
	}
	if (s14.f14 != 14) {
		fprintf(stderr, "mono_return_sbyte14 s14.f14: got %d but expected %d\n", s14.f14, 14);
	}
	s14.f1+=addend; s14.f2+=addend; s14.f3+=addend; s14.f4+=addend; s14.f5+=addend; s14.f6+=addend; s14.f7+=addend; s14.f8+=addend; s14.f9+=addend; s14.f10+=addend; s14.f11+=addend; s14.f12+=addend; s14.f13+=addend; s14.f14+=addend; 
	return s14;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15;
} sbyte15;

LIBTEST_API sbyte15 STDCALL
mono_return_sbyte15 (sbyte15 s15, int addend) {
	if (s15.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte15 s15.f1: got %d but expected %d\n", s15.f1, 1);
	}
	if (s15.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte15 s15.f2: got %d but expected %d\n", s15.f2, 2);
	}
	if (s15.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte15 s15.f3: got %d but expected %d\n", s15.f3, 3);
	}
	if (s15.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte15 s15.f4: got %d but expected %d\n", s15.f4, 4);
	}
	if (s15.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte15 s15.f5: got %d but expected %d\n", s15.f5, 5);
	}
	if (s15.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte15 s15.f6: got %d but expected %d\n", s15.f6, 6);
	}
	if (s15.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte15 s15.f7: got %d but expected %d\n", s15.f7, 7);
	}
	if (s15.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte15 s15.f8: got %d but expected %d\n", s15.f8, 8);
	}
	if (s15.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte15 s15.f9: got %d but expected %d\n", s15.f9, 9);
	}
	if (s15.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte15 s15.f10: got %d but expected %d\n", s15.f10, 10);
	}
	if (s15.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte15 s15.f11: got %d but expected %d\n", s15.f11, 11);
	}
	if (s15.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte15 s15.f12: got %d but expected %d\n", s15.f12, 12);
	}
	if (s15.f13 != 13) {
		fprintf(stderr, "mono_return_sbyte15 s15.f13: got %d but expected %d\n", s15.f13, 13);
	}
	if (s15.f14 != 14) {
		fprintf(stderr, "mono_return_sbyte15 s15.f14: got %d but expected %d\n", s15.f14, 14);
	}
	if (s15.f15 != 15) {
		fprintf(stderr, "mono_return_sbyte15 s15.f15: got %d but expected %d\n", s15.f15, 15);
	}
	s15.f1+=addend; s15.f2+=addend; s15.f3+=addend; s15.f4+=addend; s15.f5+=addend; s15.f6+=addend; s15.f7+=addend; s15.f8+=addend; s15.f9+=addend; s15.f10+=addend; s15.f11+=addend; s15.f12+=addend; s15.f13+=addend; s15.f14+=addend; s15.f15+=addend; 
	return s15;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15,f16;
} sbyte16;

LIBTEST_API sbyte16 STDCALL
mono_return_sbyte16 (sbyte16 s16, int addend) {
	if (s16.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte16 s16.f1: got %d but expected %d\n", s16.f1, 1);
	}
	if (s16.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte16 s16.f2: got %d but expected %d\n", s16.f2, 2);
	}
	if (s16.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte16 s16.f3: got %d but expected %d\n", s16.f3, 3);
	}
	if (s16.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte16 s16.f4: got %d but expected %d\n", s16.f4, 4);
	}
	if (s16.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte16 s16.f5: got %d but expected %d\n", s16.f5, 5);
	}
	if (s16.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte16 s16.f6: got %d but expected %d\n", s16.f6, 6);
	}
	if (s16.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte16 s16.f7: got %d but expected %d\n", s16.f7, 7);
	}
	if (s16.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte16 s16.f8: got %d but expected %d\n", s16.f8, 8);
	}
	if (s16.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte16 s16.f9: got %d but expected %d\n", s16.f9, 9);
	}
	if (s16.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte16 s16.f10: got %d but expected %d\n", s16.f10, 10);
	}
	if (s16.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte16 s16.f11: got %d but expected %d\n", s16.f11, 11);
	}
	if (s16.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte16 s16.f12: got %d but expected %d\n", s16.f12, 12);
	}
	if (s16.f13 != 13) {
		fprintf(stderr, "mono_return_sbyte16 s16.f13: got %d but expected %d\n", s16.f13, 13);
	}
	if (s16.f14 != 14) {
		fprintf(stderr, "mono_return_sbyte16 s16.f14: got %d but expected %d\n", s16.f14, 14);
	}
	if (s16.f15 != 15) {
		fprintf(stderr, "mono_return_sbyte16 s16.f15: got %d but expected %d\n", s16.f15, 15);
	}
	if (s16.f16 != 16) {
		fprintf(stderr, "mono_return_sbyte16 s16.f16: got %d but expected %d\n", s16.f16, 16);
	}
	s16.f1+=addend; s16.f2+=addend; s16.f3+=addend; s16.f4+=addend; s16.f5+=addend; s16.f6+=addend; s16.f7+=addend; s16.f8+=addend; s16.f9+=addend; s16.f10+=addend; s16.f11+=addend; s16.f12+=addend; s16.f13+=addend; s16.f14+=addend; s16.f15+=addend; s16.f16+=addend; 
	return s16;
}

typedef struct {
	char f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15,f16,f17;
} sbyte17;

LIBTEST_API sbyte17 STDCALL
mono_return_sbyte17 (sbyte17 s17, int addend) {
	if (s17.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte17 s17.f1: got %d but expected %d\n", s17.f1, 1);
	}
	if (s17.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte17 s17.f2: got %d but expected %d\n", s17.f2, 2);
	}
	if (s17.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte17 s17.f3: got %d but expected %d\n", s17.f3, 3);
	}
	if (s17.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte17 s17.f4: got %d but expected %d\n", s17.f4, 4);
	}
	if (s17.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte17 s17.f5: got %d but expected %d\n", s17.f5, 5);
	}
	if (s17.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte17 s17.f6: got %d but expected %d\n", s17.f6, 6);
	}
	if (s17.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte17 s17.f7: got %d but expected %d\n", s17.f7, 7);
	}
	if (s17.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte17 s17.f8: got %d but expected %d\n", s17.f8, 8);
	}
	if (s17.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte17 s17.f9: got %d but expected %d\n", s17.f9, 9);
	}
	if (s17.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte17 s17.f10: got %d but expected %d\n", s17.f10, 10);
	}
	if (s17.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte17 s17.f11: got %d but expected %d\n", s17.f11, 11);
	}
	if (s17.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte17 s17.f12: got %d but expected %d\n", s17.f12, 12);
	}
	if (s17.f13 != 13) {
		fprintf(stderr, "mono_return_sbyte17 s17.f13: got %d but expected %d\n", s17.f13, 13);
	}
	if (s17.f14 != 14) {
		fprintf(stderr, "mono_return_sbyte17 s17.f14: got %d but expected %d\n", s17.f14, 14);
	}
	if (s17.f15 != 15) {
		fprintf(stderr, "mono_return_sbyte17 s17.f15: got %d but expected %d\n", s17.f15, 15);
	}
	if (s17.f16 != 16) {
		fprintf(stderr, "mono_return_sbyte17 s17.f16: got %d but expected %d\n", s17.f16, 16);
	}
	if (s17.f17 != 17) {
		fprintf(stderr, "mono_return_sbyte17 s17.f17: got %d but expected %d\n", s17.f17, 17);
	}
	s17.f1+=addend; s17.f2+=addend; s17.f3+=addend; s17.f4+=addend; s17.f5+=addend; s17.f6+=addend; s17.f7+=addend; s17.f8+=addend; s17.f9+=addend; s17.f10+=addend; s17.f11+=addend; s17.f12+=addend; s17.f13+=addend; s17.f14+=addend; s17.f15+=addend; s17.f16+=addend; s17.f17+=addend; 
	return s17;
}

typedef struct {
	struct {
		char f1;
	} nested1;
	char f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15;
	struct {
		char f16;
	} nested2;
} sbyte16_nested;

LIBTEST_API sbyte16_nested STDCALL
mono_return_sbyte16_nested (sbyte16_nested sn16, int addend) {
	if (sn16.nested1.f1 != 1) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.nested1.f1: got %d but expected %d\n", sn16.nested1.f1, 1);
	}
	if (sn16.f2 != 2) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f2: got %d but expected %d\n", sn16.f2, 2);
	}
	if (sn16.f3 != 3) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f3: got %d but expected %d\n", sn16.f3, 3);
	}
	if (sn16.f4 != 4) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f4: got %d but expected %d\n", sn16.f4, 4);
	}
	if (sn16.f5 != 5) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f5: got %d but expected %d\n", sn16.f5, 5);
	}
	if (sn16.f6 != 6) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f6: got %d but expected %d\n", sn16.f6, 6);
	}
	if (sn16.f7 != 7) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f7: got %d but expected %d\n", sn16.f7, 7);
	}
	if (sn16.f8 != 8) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f8: got %d but expected %d\n", sn16.f8, 8);
	}
	if (sn16.f9 != 9) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f9: got %d but expected %d\n", sn16.f9, 9);
	}
	if (sn16.f10 != 10) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f10: got %d but expected %d\n", sn16.f10, 10);
	}
	if (sn16.f11 != 11) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f11: got %d but expected %d\n", sn16.f11, 11);
	}
	if (sn16.f12 != 12) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f12: got %d but expected %d\n", sn16.f12, 12);
	}
	if (sn16.f13 != 13) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f13: got %d but expected %d\n", sn16.f13, 13);
	}
	if (sn16.f14 != 14) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f14: got %d but expected %d\n", sn16.f14, 14);
	}
	if (sn16.f15 != 15) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.f15: got %d but expected %d\n", sn16.f15, 15);
	}
	if (sn16.nested2.f16 != 16) {
		fprintf(stderr, "mono_return_sbyte16_nested sn16.nested2.f16: got %d but expected %d\n", sn16.nested2.f16, 16);
	}
	sn16.nested1.f1+=addend; sn16.f2+=addend; sn16.f3+=addend; sn16.f4+=addend; sn16.f5+=addend; sn16.f6+=addend; sn16.f7+=addend; sn16.f8+=addend; sn16.f9+=addend; sn16.f10+=addend; sn16.f11+=addend; sn16.f12+=addend; sn16.f13+=addend; sn16.f14+=addend; sn16.f15+=addend; sn16.nested2.f16+=addend; 
	return sn16;
}


typedef struct {
	short f1;
} short1;

LIBTEST_API short1 STDCALL
mono_return_short1 (short1 s1, int addend) {
	if (s1.f1 != 1) {
		fprintf(stderr, "mono_return_short1 s1.f1: got %d but expected %d\n", s1.f1, 1);
	}
	s1.f1+=addend; 
	return s1;
}

typedef struct {
	short f1,f2;
} short2;

LIBTEST_API short2 STDCALL
mono_return_short2 (short2 s2, int addend) {
	if (s2.f1 != 1) {
		fprintf(stderr, "mono_return_short2 s2.f1: got %d but expected %d\n", s2.f1, 1);
	}
	if (s2.f2 != 2) {
		fprintf(stderr, "mono_return_short2 s2.f2: got %d but expected %d\n", s2.f2, 2);
	}
	s2.f1+=addend; s2.f2+=addend; 
	return s2;
}

typedef struct {
	short f1,f2,f3;
} short3;

LIBTEST_API short3 STDCALL
mono_return_short3 (short3 s3, int addend) {
	if (s3.f1 != 1) {
		fprintf(stderr, "mono_return_short3 s3.f1: got %d but expected %d\n", s3.f1, 1);
	}
	if (s3.f2 != 2) {
		fprintf(stderr, "mono_return_short3 s3.f2: got %d but expected %d\n", s3.f2, 2);
	}
	if (s3.f3 != 3) {
		fprintf(stderr, "mono_return_short3 s3.f3: got %d but expected %d\n", s3.f3, 3);
	}
	s3.f1+=addend; s3.f2+=addend; s3.f3+=addend; 
	return s3;
}

typedef struct {
	short f1,f2,f3,f4;
} short4;

LIBTEST_API short4 STDCALL
mono_return_short4 (short4 s4, int addend) {
	if (s4.f1 != 1) {
		fprintf(stderr, "mono_return_short4 s4.f1: got %d but expected %d\n", s4.f1, 1);
	}
	if (s4.f2 != 2) {
		fprintf(stderr, "mono_return_short4 s4.f2: got %d but expected %d\n", s4.f2, 2);
	}
	if (s4.f3 != 3) {
		fprintf(stderr, "mono_return_short4 s4.f3: got %d but expected %d\n", s4.f3, 3);
	}
	if (s4.f4 != 4) {
		fprintf(stderr, "mono_return_short4 s4.f4: got %d but expected %d\n", s4.f4, 4);
	}
	s4.f1+=addend; s4.f2+=addend; s4.f3+=addend; s4.f4+=addend; 
	return s4;
}

typedef struct {
	short f1,f2,f3,f4,f5;
} short5;

LIBTEST_API short5 STDCALL
mono_return_short5 (short5 s5, int addend) {
	if (s5.f1 != 1) {
		fprintf(stderr, "mono_return_short5 s5.f1: got %d but expected %d\n", s5.f1, 1);
	}
	if (s5.f2 != 2) {
		fprintf(stderr, "mono_return_short5 s5.f2: got %d but expected %d\n", s5.f2, 2);
	}
	if (s5.f3 != 3) {
		fprintf(stderr, "mono_return_short5 s5.f3: got %d but expected %d\n", s5.f3, 3);
	}
	if (s5.f4 != 4) {
		fprintf(stderr, "mono_return_short5 s5.f4: got %d but expected %d\n", s5.f4, 4);
	}
	if (s5.f5 != 5) {
		fprintf(stderr, "mono_return_short5 s5.f5: got %d but expected %d\n", s5.f5, 5);
	}
	s5.f1+=addend; s5.f2+=addend; s5.f3+=addend; s5.f4+=addend; s5.f5+=addend; 
	return s5;
}

typedef struct {
	short f1,f2,f3,f4,f5,f6;
} short6;

LIBTEST_API short6 STDCALL
mono_return_short6 (short6 s6, int addend) {
	if (s6.f1 != 1) {
		fprintf(stderr, "mono_return_short6 s6.f1: got %d but expected %d\n", s6.f1, 1);
	}
	if (s6.f2 != 2) {
		fprintf(stderr, "mono_return_short6 s6.f2: got %d but expected %d\n", s6.f2, 2);
	}
	if (s6.f3 != 3) {
		fprintf(stderr, "mono_return_short6 s6.f3: got %d but expected %d\n", s6.f3, 3);
	}
	if (s6.f4 != 4) {
		fprintf(stderr, "mono_return_short6 s6.f4: got %d but expected %d\n", s6.f4, 4);
	}
	if (s6.f5 != 5) {
		fprintf(stderr, "mono_return_short6 s6.f5: got %d but expected %d\n", s6.f5, 5);
	}
	if (s6.f6 != 6) {
		fprintf(stderr, "mono_return_short6 s6.f6: got %d but expected %d\n", s6.f6, 6);
	}
	s6.f1+=addend; s6.f2+=addend; s6.f3+=addend; s6.f4+=addend; s6.f5+=addend; s6.f6+=addend; 
	return s6;
}

typedef struct {
	short f1,f2,f3,f4,f5,f6,f7;
} short7;

LIBTEST_API short7 STDCALL
mono_return_short7 (short7 s7, int addend) {
	if (s7.f1 != 1) {
		fprintf(stderr, "mono_return_short7 s7.f1: got %d but expected %d\n", s7.f1, 1);
	}
	if (s7.f2 != 2) {
		fprintf(stderr, "mono_return_short7 s7.f2: got %d but expected %d\n", s7.f2, 2);
	}
	if (s7.f3 != 3) {
		fprintf(stderr, "mono_return_short7 s7.f3: got %d but expected %d\n", s7.f3, 3);
	}
	if (s7.f4 != 4) {
		fprintf(stderr, "mono_return_short7 s7.f4: got %d but expected %d\n", s7.f4, 4);
	}
	if (s7.f5 != 5) {
		fprintf(stderr, "mono_return_short7 s7.f5: got %d but expected %d\n", s7.f5, 5);
	}
	if (s7.f6 != 6) {
		fprintf(stderr, "mono_return_short7 s7.f6: got %d but expected %d\n", s7.f6, 6);
	}
	if (s7.f7 != 7) {
		fprintf(stderr, "mono_return_short7 s7.f7: got %d but expected %d\n", s7.f7, 7);
	}
	s7.f1+=addend; s7.f2+=addend; s7.f3+=addend; s7.f4+=addend; s7.f5+=addend; s7.f6+=addend; s7.f7+=addend; 
	return s7;
}

typedef struct {
	short f1,f2,f3,f4,f5,f6,f7,f8;
} short8;

LIBTEST_API short8 STDCALL
mono_return_short8 (short8 s8, int addend) {
	if (s8.f1 != 1) {
		fprintf(stderr, "mono_return_short8 s8.f1: got %d but expected %d\n", s8.f1, 1);
	}
	if (s8.f2 != 2) {
		fprintf(stderr, "mono_return_short8 s8.f2: got %d but expected %d\n", s8.f2, 2);
	}
	if (s8.f3 != 3) {
		fprintf(stderr, "mono_return_short8 s8.f3: got %d but expected %d\n", s8.f3, 3);
	}
	if (s8.f4 != 4) {
		fprintf(stderr, "mono_return_short8 s8.f4: got %d but expected %d\n", s8.f4, 4);
	}
	if (s8.f5 != 5) {
		fprintf(stderr, "mono_return_short8 s8.f5: got %d but expected %d\n", s8.f5, 5);
	}
	if (s8.f6 != 6) {
		fprintf(stderr, "mono_return_short8 s8.f6: got %d but expected %d\n", s8.f6, 6);
	}
	if (s8.f7 != 7) {
		fprintf(stderr, "mono_return_short8 s8.f7: got %d but expected %d\n", s8.f7, 7);
	}
	if (s8.f8 != 8) {
		fprintf(stderr, "mono_return_short8 s8.f8: got %d but expected %d\n", s8.f8, 8);
	}
	s8.f1+=addend; s8.f2+=addend; s8.f3+=addend; s8.f4+=addend; s8.f5+=addend; s8.f6+=addend; s8.f7+=addend; s8.f8+=addend; 
	return s8;
}

typedef struct {
	short f1,f2,f3,f4,f5,f6,f7,f8,f9;
} short9;

LIBTEST_API short9 STDCALL
mono_return_short9 (short9 s9, int addend) {
	if (s9.f1 != 1) {
		fprintf(stderr, "mono_return_short9 s9.f1: got %d but expected %d\n", s9.f1, 1);
	}
	if (s9.f2 != 2) {
		fprintf(stderr, "mono_return_short9 s9.f2: got %d but expected %d\n", s9.f2, 2);
	}
	if (s9.f3 != 3) {
		fprintf(stderr, "mono_return_short9 s9.f3: got %d but expected %d\n", s9.f3, 3);
	}
	if (s9.f4 != 4) {
		fprintf(stderr, "mono_return_short9 s9.f4: got %d but expected %d\n", s9.f4, 4);
	}
	if (s9.f5 != 5) {
		fprintf(stderr, "mono_return_short9 s9.f5: got %d but expected %d\n", s9.f5, 5);
	}
	if (s9.f6 != 6) {
		fprintf(stderr, "mono_return_short9 s9.f6: got %d but expected %d\n", s9.f6, 6);
	}
	if (s9.f7 != 7) {
		fprintf(stderr, "mono_return_short9 s9.f7: got %d but expected %d\n", s9.f7, 7);
	}
	if (s9.f8 != 8) {
		fprintf(stderr, "mono_return_short9 s9.f8: got %d but expected %d\n", s9.f8, 8);
	}
	if (s9.f9 != 9) {
		fprintf(stderr, "mono_return_short9 s9.f9: got %d but expected %d\n", s9.f9, 9);
	}
	s9.f1+=addend; s9.f2+=addend; s9.f3+=addend; s9.f4+=addend; s9.f5+=addend; s9.f6+=addend; s9.f7+=addend; s9.f8+=addend; s9.f9+=addend; 
	return s9;
}

typedef struct {
	struct {
		short f1;
	} nested1;
	short f2,f3,f4,f5,f6,f7;
	struct {
		short f8;
	} nested2;
} short8_nested;

LIBTEST_API short8_nested STDCALL
mono_return_short8_nested (short8_nested sn8, int addend) {
	if (sn8.nested1.f1 != 1) {
		fprintf(stderr, "mono_return_short8_nested sn8.nested1.f1: got %d but expected %d\n", sn8.nested1.f1, 1);
	}
	if (sn8.f2 != 2) {
		fprintf(stderr, "mono_return_short8_nested sn8.f2: got %d but expected %d\n", sn8.f2, 2);
	}
	if (sn8.f3 != 3) {
		fprintf(stderr, "mono_return_short8_nested sn8.f3: got %d but expected %d\n", sn8.f3, 3);
	}
	if (sn8.f4 != 4) {
		fprintf(stderr, "mono_return_short8_nested sn8.f4: got %d but expected %d\n", sn8.f4, 4);
	}
	if (sn8.f5 != 5) {
		fprintf(stderr, "mono_return_short8_nested sn8.f5: got %d but expected %d\n", sn8.f5, 5);
	}
	if (sn8.f6 != 6) {
		fprintf(stderr, "mono_return_short8_nested sn8.f6: got %d but expected %d\n", sn8.f6, 6);
	}
	if (sn8.f7 != 7) {
		fprintf(stderr, "mono_return_short8_nested sn8.f7: got %d but expected %d\n", sn8.f7, 7);
	}
	if (sn8.nested2.f8 != 8) {
		fprintf(stderr, "mono_return_short8_nested sn8.nested2.f8: got %d but expected %d\n", sn8.nested2.f8, 8);
	}
	sn8.nested1.f1+=addend; sn8.f2+=addend; sn8.f3+=addend; sn8.f4+=addend; sn8.f5+=addend; sn8.f6+=addend; sn8.f7+=addend; sn8.nested2.f8+=addend; 
	return sn8;
}


typedef struct {
	int f1;
} int1;

LIBTEST_API int1 STDCALL
mono_return_int1 (int1 s1, int addend) {
	if (s1.f1 != 1) {
		fprintf(stderr, "mono_return_int1 s1.f1: got %d but expected %d\n", s1.f1, 1);
	}
	s1.f1+=addend; 
	return s1;
}

typedef struct {
	int f1,f2;
} int2;

LIBTEST_API int2 STDCALL
mono_return_int2 (int2 s2, int addend) {
	if (s2.f1 != 1) {
		fprintf(stderr, "mono_return_int2 s2.f1: got %d but expected %d\n", s2.f1, 1);
	}
	if (s2.f2 != 2) {
		fprintf(stderr, "mono_return_int2 s2.f2: got %d but expected %d\n", s2.f2, 2);
	}
	s2.f1+=addend; s2.f2+=addend; 
	return s2;
}

typedef struct {
	int f1,f2,f3;
} int3;

LIBTEST_API int3 STDCALL
mono_return_int3 (int3 s3, int addend) {
	if (s3.f1 != 1) {
		fprintf(stderr, "mono_return_int3 s3.f1: got %d but expected %d\n", s3.f1, 1);
	}
	if (s3.f2 != 2) {
		fprintf(stderr, "mono_return_int3 s3.f2: got %d but expected %d\n", s3.f2, 2);
	}
	if (s3.f3 != 3) {
		fprintf(stderr, "mono_return_int3 s3.f3: got %d but expected %d\n", s3.f3, 3);
	}
	s3.f1+=addend; s3.f2+=addend; s3.f3+=addend; 
	return s3;
}

typedef struct {
	int f1,f2,f3,f4;
} int4;

LIBTEST_API int4 STDCALL
mono_return_int4 (int4 s4, int addend) {
	if (s4.f1 != 1) {
		fprintf(stderr, "mono_return_int4 s4.f1: got %d but expected %d\n", s4.f1, 1);
	}
	if (s4.f2 != 2) {
		fprintf(stderr, "mono_return_int4 s4.f2: got %d but expected %d\n", s4.f2, 2);
	}
	if (s4.f3 != 3) {
		fprintf(stderr, "mono_return_int4 s4.f3: got %d but expected %d\n", s4.f3, 3);
	}
	if (s4.f4 != 4) {
		fprintf(stderr, "mono_return_int4 s4.f4: got %d but expected %d\n", s4.f4, 4);
	}
	s4.f1+=addend; s4.f2+=addend; s4.f3+=addend; s4.f4+=addend; 
	return s4;
}

typedef struct {
	int f1,f2,f3,f4,f5;
} int5;

LIBTEST_API int5 STDCALL
mono_return_int5 (int5 s5, int addend) {
	if (s5.f1 != 1) {
		fprintf(stderr, "mono_return_int5 s5.f1: got %d but expected %d\n", s5.f1, 1);
	}
	if (s5.f2 != 2) {
		fprintf(stderr, "mono_return_int5 s5.f2: got %d but expected %d\n", s5.f2, 2);
	}
	if (s5.f3 != 3) {
		fprintf(stderr, "mono_return_int5 s5.f3: got %d but expected %d\n", s5.f3, 3);
	}
	if (s5.f4 != 4) {
		fprintf(stderr, "mono_return_int5 s5.f4: got %d but expected %d\n", s5.f4, 4);
	}
	if (s5.f5 != 5) {
		fprintf(stderr, "mono_return_int5 s5.f5: got %d but expected %d\n", s5.f5, 5);
	}
	s5.f1+=addend; s5.f2+=addend; s5.f3+=addend; s5.f4+=addend; s5.f5+=addend; 
	return s5;
}

typedef struct {
	struct {
		int f1;
	} nested1;
	int f2,f3;
	struct {
		int f4;
	} nested2;
} int4_nested;

LIBTEST_API int4_nested STDCALL
mono_return_int4_nested (int4_nested sn4, int addend) {
	if (sn4.nested1.f1 != 1) {
		fprintf(stderr, "mono_return_int4_nested sn4.nested1.f1: got %d but expected %d\n", sn4.nested1.f1, 1);
	}
	if (sn4.f2 != 2) {
		fprintf(stderr, "mono_return_int4_nested sn4.f2: got %d but expected %d\n", sn4.f2, 2);
	}
	if (sn4.f3 != 3) {
		fprintf(stderr, "mono_return_int4_nested sn4.f3: got %d but expected %d\n", sn4.f3, 3);
	}
	if (sn4.nested2.f4 != 4) {
		fprintf(stderr, "mono_return_int4_nested sn4.nested2.f4: got %d but expected %d\n", sn4.nested2.f4, 4);
	}
	sn4.nested1.f1+=addend; sn4.f2+=addend; sn4.f3+=addend; sn4.nested2.f4+=addend; 
	return sn4;
}

typedef struct {
	float f1;
} float1;

LIBTEST_API float1 STDCALL
mono_return_float1 (float1 s1, int addend) {
	if (s1.f1 != 1) {
		fprintf(stderr, "mono_return_float1 s1.f1: got %f but expected %d\n", s1.f1, 1);
	}
	s1.f1+=addend; 
	return s1;
}

typedef struct {
	float f1,f2;
} float2;

LIBTEST_API float2 STDCALL
mono_return_float2 (float2 s2, int addend) {
	if (s2.f1 != 1) {
		fprintf(stderr, "mono_return_float2 s2.f1: got %f but expected %d\n", s2.f1, 1);
	}
	if (s2.f2 != 2) {
		fprintf(stderr, "mono_return_float2 s2.f2: got %f but expected %d\n", s2.f2, 2);
	}
	s2.f1+=addend; s2.f2+=addend; 
	return s2;
}

typedef struct {
	float f1,f2,f3;
} float3;

LIBTEST_API float3 STDCALL
mono_return_float3 (float3 s3, int addend) {
	if (s3.f1 != 1) {
		fprintf(stderr, "mono_return_float3 s3.f1: got %f but expected %d\n", s3.f1, 1);
	}
	if (s3.f2 != 2) {
		fprintf(stderr, "mono_return_float3 s3.f2: got %f but expected %d\n", s3.f2, 2);
	}
	if (s3.f3 != 3) {
		fprintf(stderr, "mono_return_float3 s3.f3: got %f but expected %d\n", s3.f3, 3);
	}
	s3.f1+=addend; s3.f2+=addend; s3.f3+=addend; 
	return s3;
}

typedef struct {
	float f1,f2,f3,f4;
} float4;

LIBTEST_API float4 STDCALL
mono_return_float4 (float4 s4, int addend) {
	if (s4.f1 != 1) {
		fprintf(stderr, "mono_return_float4 s4.f1: got %f but expected %d\n", s4.f1, 1);
	}
	if (s4.f2 != 2) {
		fprintf(stderr, "mono_return_float4 s4.f2: got %f but expected %d\n", s4.f2, 2);
	}
	if (s4.f3 != 3) {
		fprintf(stderr, "mono_return_float4 s4.f3: got %f but expected %d\n", s4.f3, 3);
	}
	if (s4.f4 != 4) {
		fprintf(stderr, "mono_return_float4 s4.f4: got %f but expected %d\n", s4.f4, 4);
	}
	s4.f1+=addend; s4.f2+=addend; s4.f3+=addend; s4.f4+=addend; 
	return s4;
}

typedef struct {
	float f1,f2,f3,f4,f5;
} float5;

LIBTEST_API float5 STDCALL
mono_return_float5 (float5 s5, int addend) {
	if (s5.f1 != 1) {
		fprintf(stderr, "mono_return_float5 s5.f1: got %f but expected %d\n", s5.f1, 1);
	}
	if (s5.f2 != 2) {
		fprintf(stderr, "mono_return_float5 s5.f2: got %f but expected %d\n", s5.f2, 2);
	}
	if (s5.f3 != 3) {
		fprintf(stderr, "mono_return_float5 s5.f3: got %f but expected %d\n", s5.f3, 3);
	}
	if (s5.f4 != 4) {
		fprintf(stderr, "mono_return_float5 s5.f4: got %f but expected %d\n", s5.f4, 4);
	}
	if (s5.f5 != 5) {
		fprintf(stderr, "mono_return_float5 s5.f5: got %f but expected %d\n", s5.f5, 5);
	}
	s5.f1+=addend; s5.f2+=addend; s5.f3+=addend; s5.f4+=addend; s5.f5+=addend; 
	return s5;
}

typedef struct {
	float f1,f2,f3,f4,f5,f6;
} float6;

LIBTEST_API float6 STDCALL
mono_return_float6 (float6 s6, int addend) {
	if (s6.f1 != 1) {
		fprintf(stderr, "mono_return_float6 s6.f1: got %f but expected %d\n", s6.f1, 1);
	}
	if (s6.f2 != 2) {
		fprintf(stderr, "mono_return_float6 s6.f2: got %f but expected %d\n", s6.f2, 2);
	}
	if (s6.f3 != 3) {
		fprintf(stderr, "mono_return_float6 s6.f3: got %f but expected %d\n", s6.f3, 3);
	}
	if (s6.f4 != 4) {
		fprintf(stderr, "mono_return_float6 s6.f4: got %f but expected %d\n", s6.f4, 4);
	}
	if (s6.f5 != 5) {
		fprintf(stderr, "mono_return_float6 s6.f5: got %f but expected %d\n", s6.f5, 5);
	}
	if (s6.f6 != 6) {
		fprintf(stderr, "mono_return_float6 s6.f6: got %f but expected %d\n", s6.f6, 6);
	}
	s6.f1+=addend; s6.f2+=addend; s6.f3+=addend; s6.f4+=addend; s6.f5+=addend; s6.f6+=addend; 
	return s6;
}

typedef struct {
	float f1,f2,f3,f4,f5,f6,f7;
} float7;

LIBTEST_API float7 STDCALL
mono_return_float7 (float7 s7, int addend) {
	if (s7.f1 != 1) {
		fprintf(stderr, "mono_return_float7 s7.f1: got %f but expected %d\n", s7.f1, 1);
	}
	if (s7.f2 != 2) {
		fprintf(stderr, "mono_return_float7 s7.f2: got %f but expected %d\n", s7.f2, 2);
	}
	if (s7.f3 != 3) {
		fprintf(stderr, "mono_return_float7 s7.f3: got %f but expected %d\n", s7.f3, 3);
	}
	if (s7.f4 != 4) {
		fprintf(stderr, "mono_return_float7 s7.f4: got %f but expected %d\n", s7.f4, 4);
	}
	if (s7.f5 != 5) {
		fprintf(stderr, "mono_return_float7 s7.f5: got %f but expected %d\n", s7.f5, 5);
	}
	if (s7.f6 != 6) {
		fprintf(stderr, "mono_return_float7 s7.f6: got %f but expected %d\n", s7.f6, 6);
	}
	if (s7.f7 != 7) {
		fprintf(stderr, "mono_return_float7 s7.f7: got %f but expected %d\n", s7.f7, 7);
	}
	s7.f1+=addend; s7.f2+=addend; s7.f3+=addend; s7.f4+=addend; s7.f5+=addend; s7.f6+=addend; s7.f7+=addend; 
	return s7;
}

typedef struct {
	float f1,f2,f3,f4,f5,f6,f7,f8;
} float8;

LIBTEST_API float8 STDCALL
mono_return_float8 (float8 s8, int addend) {
	if (s8.f1 != 1) {
		fprintf(stderr, "mono_return_float8 s8.f1: got %f but expected %d\n", s8.f1, 1);
	}
	if (s8.f2 != 2) {
		fprintf(stderr, "mono_return_float8 s8.f2: got %f but expected %d\n", s8.f2, 2);
	}
	if (s8.f3 != 3) {
		fprintf(stderr, "mono_return_float8 s8.f3: got %f but expected %d\n", s8.f3, 3);
	}
	if (s8.f4 != 4) {
		fprintf(stderr, "mono_return_float8 s8.f4: got %f but expected %d\n", s8.f4, 4);
	}
	if (s8.f5 != 5) {
		fprintf(stderr, "mono_return_float8 s8.f5: got %f but expected %d\n", s8.f5, 5);
	}
	if (s8.f6 != 6) {
		fprintf(stderr, "mono_return_float8 s8.f6: got %f but expected %d\n", s8.f6, 6);
	}
	if (s8.f7 != 7) {
		fprintf(stderr, "mono_return_float8 s8.f7: got %f but expected %d\n", s8.f7, 7);
	}
	if (s8.f8 != 8) {
		fprintf(stderr, "mono_return_float8 s8.f8: got %f but expected %d\n", s8.f8, 8);
	}
	s8.f1+=addend; s8.f2+=addend; s8.f3+=addend; s8.f4+=addend; s8.f5+=addend; s8.f6+=addend; s8.f7+=addend; s8.f8+=addend; 
	return s8;
}

typedef struct {
	float f1,f2,f3,f4,f5,f6,f7,f8,f9;
} float9;

LIBTEST_API float9 STDCALL
mono_return_float9 (float9 s9, int addend) {
	if (s9.f1 != 1) {
		fprintf(stderr, "mono_return_float9 s9.f1: got %f but expected %d\n", s9.f1, 1);
	}
	if (s9.f2 != 2) {
		fprintf(stderr, "mono_return_float9 s9.f2: got %f but expected %d\n", s9.f2, 2);
	}
	if (s9.f3 != 3) {
		fprintf(stderr, "mono_return_float9 s9.f3: got %f but expected %d\n", s9.f3, 3);
	}
	if (s9.f4 != 4) {
		fprintf(stderr, "mono_return_float9 s9.f4: got %f but expected %d\n", s9.f4, 4);
	}
	if (s9.f5 != 5) {
		fprintf(stderr, "mono_return_float9 s9.f5: got %f but expected %d\n", s9.f5, 5);
	}
	if (s9.f6 != 6) {
		fprintf(stderr, "mono_return_float9 s9.f6: got %f but expected %d\n", s9.f6, 6);
	}
	if (s9.f7 != 7) {
		fprintf(stderr, "mono_return_float9 s9.f7: got %f but expected %d\n", s9.f7, 7);
	}
	if (s9.f8 != 8) {
		fprintf(stderr, "mono_return_float9 s9.f8: got %f but expected %d\n", s9.f8, 8);
	}
	if (s9.f9 != 9) {
		fprintf(stderr, "mono_return_float9 s9.f9: got %f but expected %d\n", s9.f9, 9);
	}
	s9.f1+=addend; s9.f2+=addend; s9.f3+=addend; s9.f4+=addend; s9.f5+=addend; s9.f6+=addend; s9.f7+=addend; s9.f8+=addend; s9.f9+=addend; 
	return s9;
}

typedef struct {
	struct {
		float f1;
	} nested1;
	float f2,f3;
	struct {
		float f4;
	} nested2;
} float4_nested;

LIBTEST_API float4_nested STDCALL
mono_return_float4_nested (float4_nested sn4, int addend) {
	if (sn4.nested1.f1 != 1) {
		fprintf(stderr, "mono_return_float4_nested sn4.nested1.f1: got %f but expected %d\n", sn4.nested1.f1, 1);
	}
	if (sn4.f2 != 2) {
		fprintf(stderr, "mono_return_float4_nested sn4.f2: got %f but expected %d\n", sn4.f2, 2);
	}
	if (sn4.f3 != 3) {
		fprintf(stderr, "mono_return_float4_nested sn4.f3: got %f but expected %d\n", sn4.f3, 3);
	}
	if (sn4.nested2.f4 != 4) {
		fprintf(stderr, "mono_return_float4_nested sn4.nested2.f4: got %f but expected %d\n", sn4.nested2.f4, 4);
	}
	sn4.nested1.f1+=addend; sn4.f2+=addend; sn4.f3+=addend; sn4.nested2.f4+=addend; 
	return sn4;
}

typedef struct {
	double f1;
} double1;

LIBTEST_API double1 STDCALL
mono_return_double1 (double1 s1, int addend) {
	if (s1.f1 != 1) {
		fprintf(stderr, "mono_return_double1 s1.f1: got %f but expected %d\n", s1.f1, 1);
	}
	s1.f1+=addend; 
	return s1;
}

typedef struct {
	double f1,f2;
} double2;

LIBTEST_API double2 STDCALL
mono_return_double2 (double2 s2, int addend) {
	if (s2.f1 != 1) {
		fprintf(stderr, "mono_return_double2 s2.f1: got %f but expected %d\n", s2.f1, 1);
	}
	if (s2.f2 != 2) {
		fprintf(stderr, "mono_return_double2 s2.f2: got %f but expected %d\n", s2.f2, 2);
	}
	s2.f1+=addend; s2.f2+=addend; 
	return s2;
}

typedef struct {
	double f1,f2,f3;
} double3;

LIBTEST_API double3 STDCALL
mono_return_double3 (double3 s3, int addend) {
	if (s3.f1 != 1) {
		fprintf(stderr, "mono_return_double3 s3.f1: got %f but expected %d\n", s3.f1, 1);
	}
	if (s3.f2 != 2) {
		fprintf(stderr, "mono_return_double3 s3.f2: got %f but expected %d\n", s3.f2, 2);
	}
	if (s3.f3 != 3) {
		fprintf(stderr, "mono_return_double3 s3.f3: got %f but expected %d\n", s3.f3, 3);
	}
	s3.f1+=addend; s3.f2+=addend; s3.f3+=addend; 
	return s3;
}

typedef struct {
	double f1,f2,f3,f4;
} double4;

LIBTEST_API double4 STDCALL
mono_return_double4 (double4 s4, int addend) {
	if (s4.f1 != 1) {
		fprintf(stderr, "mono_return_double4 s4.f1: got %f but expected %d\n", s4.f1, 1);
	}
	if (s4.f2 != 2) {
		fprintf(stderr, "mono_return_double4 s4.f2: got %f but expected %d\n", s4.f2, 2);
	}
	if (s4.f3 != 3) {
		fprintf(stderr, "mono_return_double4 s4.f3: got %f but expected %d\n", s4.f3, 3);
	}
	if (s4.f4 != 4) {
		fprintf(stderr, "mono_return_double4 s4.f4: got %f but expected %d\n", s4.f4, 4);
	}
	s4.f1+=addend; s4.f2+=addend; s4.f3+=addend; s4.f4+=addend; 
	return s4;
}

typedef struct {
	double f1,f2,f3,f4,f5;
} double5;

LIBTEST_API double5 STDCALL
mono_return_double5 (double5 s5, int addend) {
	if (s5.f1 != 1) {
		fprintf(stderr, "mono_return_double5 s5.f1: got %f but expected %d\n", s5.f1, 1);
	}
	if (s5.f2 != 2) {
		fprintf(stderr, "mono_return_double5 s5.f2: got %f but expected %d\n", s5.f2, 2);
	}
	if (s5.f3 != 3) {
		fprintf(stderr, "mono_return_double5 s5.f3: got %f but expected %d\n", s5.f3, 3);
	}
	if (s5.f4 != 4) {
		fprintf(stderr, "mono_return_double5 s5.f4: got %f but expected %d\n", s5.f4, 4);
	}
	if (s5.f5 != 5) {
		fprintf(stderr, "mono_return_double5 s5.f5: got %f but expected %d\n", s5.f5, 5);
	}
	s5.f1+=addend; s5.f2+=addend; s5.f3+=addend; s5.f4+=addend; s5.f5+=addend; 
	return s5;
}

typedef struct {
	double f1,f2,f3,f4,f5,f6;
} double6;

LIBTEST_API double6 STDCALL
mono_return_double6 (double6 s6, int addend) {
	if (s6.f1 != 1) {
		fprintf(stderr, "mono_return_double6 s6.f1: got %f but expected %d\n", s6.f1, 1);
	}
	if (s6.f2 != 2) {
		fprintf(stderr, "mono_return_double6 s6.f2: got %f but expected %d\n", s6.f2, 2);
	}
	if (s6.f3 != 3) {
		fprintf(stderr, "mono_return_double6 s6.f3: got %f but expected %d\n", s6.f3, 3);
	}
	if (s6.f4 != 4) {
		fprintf(stderr, "mono_return_double6 s6.f4: got %f but expected %d\n", s6.f4, 4);
	}
	if (s6.f5 != 5) {
		fprintf(stderr, "mono_return_double6 s6.f5: got %f but expected %d\n", s6.f5, 5);
	}
	if (s6.f6 != 6) {
		fprintf(stderr, "mono_return_double6 s6.f6: got %f but expected %d\n", s6.f6, 6);
	}
	s6.f1+=addend; s6.f2+=addend; s6.f3+=addend; s6.f4+=addend; s6.f5+=addend; s6.f6+=addend; 
	return s6;
}

typedef struct {
	double f1,f2,f3,f4,f5,f6,f7;
} double7;

LIBTEST_API double7 STDCALL
mono_return_double7 (double7 s7, int addend) {
	if (s7.f1 != 1) {
		fprintf(stderr, "mono_return_double7 s7.f1: got %f but expected %d\n", s7.f1, 1);
	}
	if (s7.f2 != 2) {
		fprintf(stderr, "mono_return_double7 s7.f2: got %f but expected %d\n", s7.f2, 2);
	}
	if (s7.f3 != 3) {
		fprintf(stderr, "mono_return_double7 s7.f3: got %f but expected %d\n", s7.f3, 3);
	}
	if (s7.f4 != 4) {
		fprintf(stderr, "mono_return_double7 s7.f4: got %f but expected %d\n", s7.f4, 4);
	}
	if (s7.f5 != 5) {
		fprintf(stderr, "mono_return_double7 s7.f5: got %f but expected %d\n", s7.f5, 5);
	}
	if (s7.f6 != 6) {
		fprintf(stderr, "mono_return_double7 s7.f6: got %f but expected %d\n", s7.f6, 6);
	}
	if (s7.f7 != 7) {
		fprintf(stderr, "mono_return_double7 s7.f7: got %f but expected %d\n", s7.f7, 7);
	}
	s7.f1+=addend; s7.f2+=addend; s7.f3+=addend; s7.f4+=addend; s7.f5+=addend; s7.f6+=addend; s7.f7+=addend; 
	return s7;
}

typedef struct {
	double f1,f2,f3,f4,f5,f6,f7,f8;
} double8;

LIBTEST_API double8 STDCALL
mono_return_double8 (double8 s8, int addend) {
	if (s8.f1 != 1) {
		fprintf(stderr, "mono_return_double8 s8.f1: got %f but expected %d\n", s8.f1, 1);
	}
	if (s8.f2 != 2) {
		fprintf(stderr, "mono_return_double8 s8.f2: got %f but expected %d\n", s8.f2, 2);
	}
	if (s8.f3 != 3) {
		fprintf(stderr, "mono_return_double8 s8.f3: got %f but expected %d\n", s8.f3, 3);
	}
	if (s8.f4 != 4) {
		fprintf(stderr, "mono_return_double8 s8.f4: got %f but expected %d\n", s8.f4, 4);
	}
	if (s8.f5 != 5) {
		fprintf(stderr, "mono_return_double8 s8.f5: got %f but expected %d\n", s8.f5, 5);
	}
	if (s8.f6 != 6) {
		fprintf(stderr, "mono_return_double8 s8.f6: got %f but expected %d\n", s8.f6, 6);
	}
	if (s8.f7 != 7) {
		fprintf(stderr, "mono_return_double8 s8.f7: got %f but expected %d\n", s8.f7, 7);
	}
	if (s8.f8 != 8) {
		fprintf(stderr, "mono_return_double8 s8.f8: got %f but expected %d\n", s8.f8, 8);
	}
	s8.f1+=addend; s8.f2+=addend; s8.f3+=addend; s8.f4+=addend; s8.f5+=addend; s8.f6+=addend; s8.f7+=addend; s8.f8+=addend; 
	return s8;
}

typedef struct {
	double f1,f2,f3,f4,f5,f6,f7,f8,f9;
} double9;

LIBTEST_API double9 STDCALL
mono_return_double9 (double9 s9, int addend) {
	if (s9.f1 != 1) {
		fprintf(stderr, "mono_return_double9 s9.f1: got %f but expected %d\n", s9.f1, 1);
	}
	if (s9.f2 != 2) {
		fprintf(stderr, "mono_return_double9 s9.f2: got %f but expected %d\n", s9.f2, 2);
	}
	if (s9.f3 != 3) {
		fprintf(stderr, "mono_return_double9 s9.f3: got %f but expected %d\n", s9.f3, 3);
	}
	if (s9.f4 != 4) {
		fprintf(stderr, "mono_return_double9 s9.f4: got %f but expected %d\n", s9.f4, 4);
	}
	if (s9.f5 != 5) {
		fprintf(stderr, "mono_return_double9 s9.f5: got %f but expected %d\n", s9.f5, 5);
	}
	if (s9.f6 != 6) {
		fprintf(stderr, "mono_return_double9 s9.f6: got %f but expected %d\n", s9.f6, 6);
	}
	if (s9.f7 != 7) {
		fprintf(stderr, "mono_return_double9 s9.f7: got %f but expected %d\n", s9.f7, 7);
	}
	if (s9.f8 != 8) {
		fprintf(stderr, "mono_return_double9 s9.f8: got %f but expected %d\n", s9.f8, 8);
	}
	if (s9.f9 != 9) {
		fprintf(stderr, "mono_return_double9 s9.f9: got %f but expected %d\n", s9.f9, 9);
	}
	s9.f1+=addend; s9.f2+=addend; s9.f3+=addend; s9.f4+=addend; s9.f5+=addend; s9.f6+=addend; s9.f7+=addend; s9.f8+=addend; s9.f9+=addend; 
	return s9;
}

typedef struct {
	struct {
		double f1;
	} nested1;
	struct {
		double f2;
	} nested2;
} double2_nested;

LIBTEST_API double2_nested STDCALL
mono_return_double2_nested (double2_nested sn2, int addend) {
	if (sn2.nested1.f1 != 1) {
		fprintf(stderr, "mono_return_double2_nested sn2.nested1.f1: got %f but expected %d\n", sn2.nested1.f1, 1);
	}
	if (sn2.nested2.f2 != 2) {
		fprintf(stderr, "mono_return_double2_nested sn2.nested2.f2: got %f but expected %d\n", sn2.nested2.f2, 2);
	}
	sn2.nested1.f1+=addend; sn2.nested2.f2+=addend; 
	return sn2;
}



typedef struct {
	double f1[4];
} double_array4;

LIBTEST_API double_array4 STDCALL
mono_return_double_array4 (double_array4 sa4, int addend) {
	if (sa4.f1[0] != 1) {
		fprintf(stderr, "mono_return_double_array4 sa4.f1[0]: got %f but expected %d\n", sa4.f1[0], 1);
	}
	if (sa4.f1[1] != 2) {
		fprintf(stderr, "mono_return_double_array4 sa4.f1[1]: got %f but expected %d\n", sa4.f1[1], 2);
	}
	if (sa4.f1[2] != 3) {
		fprintf(stderr, "mono_return_double_array4 sa4.f1[2]: got %f but expected %d\n", sa4.f1[2], 3);
	}
	if (sa4.f1[3] != 4) {
		fprintf(stderr, "mono_return_double_array4 sa4.f1[3]: got %f but expected %d\n", sa4.f1[3], 4);
	}
	sa4.f1[0]+=addend; sa4.f1[1]+=addend; sa4.f1[2]+=addend; sa4.f1[3]+=addend; 
	return sa4;
}

typedef struct {
	int array [3];
} FixedArrayStruct;

LIBTEST_API int STDCALL
mono_test_marshal_fixed_array (FixedArrayStruct s)
{
	return s.array [0] + s.array [1] + s.array [2];
}

typedef struct {
	char array [16];
	char c;
} FixedBufferChar;

LIBTEST_API int STDCALL
mono_test_marshal_fixed_buffer_char (FixedBufferChar *s)
{
	if (!(s->array [0] == 'A' && s->array [1] == 'B' && s->array [2] == 'C' && s->c == 'D'))
		return 1;
	s->array [0] = 'E';
	s->array [1] = 'F';
	s->c = 'G';
	return 0;
}

typedef struct {
	short array [16];
	short c;
} FixedBufferUnicode;

LIBTEST_API int STDCALL
mono_test_marshal_fixed_buffer_unicode (FixedBufferUnicode *s)
{
	if (!(s->array [0] == 'A' && s->array [1] == 'B' && s->array [2] == 'C' && s->c == 'D'))
		return 1;
	s->array [0] = 'E';
	s->array [1] = 'F';
	s->c = 'G';
	return 0;
}

const int NSTRINGS = 6;
//test strings
const char  *utf8Strings[] = {  
                                "Managed",
                                 "Sîne klâwen durh die wolken sint geslagen" ,
                                 "काचं शक्नोम्यत्तुम् । नोपहिनस्ति माम्",
                                 "我能吞下玻璃而不伤身体",
                                 "ღმერთსი შემვედრე,შემვედრე, ნუთუ კვლა დამხსნას შემვედრე,სოფლისა შემვედრე, შემვედრე,შემვედრე,შემვედრე,შრომასა, ცეცხლს, წყალსა და მიწასა, ჰაერთა თანა მრომასა; მომცნეს ფრთენი და აღვფრინდე, მივჰხვდე მას ჩემსა ნდომასა, დღისით და ღამით ვჰხედვიდე მზისა ელვათა კრთომაასაშემვედრე,შემვედრე,",
                                 "Τη γλώσσα μου έδωσαν ελληνική",
"\0"
};

LIBTEST_API char *
build_return_string(const char* pReturn)
{
	char *ret = 0;
	if (pReturn == 0 || *pReturn == 0)
		return ret;

	size_t strLength = strlen(pReturn);
	ret = (char *)(marshal_alloc (sizeof(char)* (strLength + 1)));
	memcpy(ret, pReturn, strLength);
	ret [strLength] = '\0';
	return ret;
}

LIBTEST_API char *
StringParameterInOut(/*[In,Out]*/ char *s, int index)
{
	// return a copy
	return build_return_string(s);
}

LIBTEST_API void
StringParameterRefOut(/*out*/ char **s, int index)
{
	char *pszTextutf8 = (char*)utf8Strings[index];
	size_t strLength = strlen(pszTextutf8);
	*s = (char *)(marshal_alloc (sizeof(char)* (strLength + 1)));
	memcpy(*s, pszTextutf8, strLength);
	(*s)[strLength] = '\0';
}

LIBTEST_API void
StringParameterRef(/*ref*/ char **s, int index)
{
    char *pszTextutf8 = (char*)utf8Strings[index];
    size_t strLength = strlen(pszTextutf8);
    // do byte by byte validation of in string
    size_t szLen = strlen(*s);
    for (size_t i = 0; i < szLen; i++)
    {
        if ((*s)[i] != pszTextutf8[i])
        {
            printf("[in] managed string do not match native string\n");
	    abort ();
        }
    }

    if (*s)
    {
       marshal_free (*s);
    }
    // overwrite the orginal 
    *s = (char *)(marshal_alloc (sizeof(char)* (strLength + 1)));
    memcpy(*s, pszTextutf8, strLength);
    (*s)[strLength] = '\0';
}

LIBTEST_API void
StringBuilderParameterInOut(/*[In,Out] StringBuilder*/ char *s, int index)
{
    // if string.empty 
    if (s == 0 || *s == 0)
        return;

    char *pszTextutf8 = (char*)utf8Strings[index];

    // do byte by byte validation of in string
    size_t szLen = strlen(s);
    for (size_t i = 0; i < szLen; i++) 
    {
        if (s[i] != pszTextutf8[i])
        {
            printf("[in] managed string do not match native string\n");
	    abort ();
        }
    }  

    // modify the string inplace 
    size_t outLen = strlen(pszTextutf8);
    for (size_t i = 0; i < outLen; i++) {
        s[i] = pszTextutf8[i];
    }
    s[outLen] = '\0';
}

//out string builder
LIBTEST_API void
StringBuilderParameterOut(/*[Out] StringBuilder*/ char *s, int index)
{
    char *pszTextutf8 = (char*)utf8Strings[index];

    printf ("SBPO: Receiving %s\n", s);
    // modify the string inplace 
    size_t outLen = strlen(pszTextutf8);
    for (size_t i = 0; i < outLen; i++) {
        s[i] = pszTextutf8[i];
    }
    s[outLen] = '\0';
}

LIBTEST_API char *
StringParameterOut(/*[Out]*/ char *s, int index)
{
    // return a copy
    return build_return_string(s);
}

// Utf8 field
typedef struct FieldWithUtf8
{
    char *pFirst;
    int index;
}FieldWithUtf8;

//utf8 struct field
LIBTEST_API void
TestStructWithUtf8Field(struct FieldWithUtf8 fieldStruct)
{
    char *pszManagedutf8 = fieldStruct.pFirst;
    int stringIndex = fieldStruct.index;
    char *pszNative = 0;
    size_t outLen = 0;

    if (pszManagedutf8 == 0 || *pszManagedutf8 == 0)
        return;

    pszNative = (char*)utf8Strings[stringIndex];

    outLen = strlen(pszNative);
    // do byte by byte comparision
    for (size_t i = 0; i < outLen; i++) 
    {
        if (pszNative[i] != pszManagedutf8[i]) 
        {
            printf("Native and managed string do not match.\n");
	    abort ();
        }
    }
}

typedef void (* Callback2)(char *text, int index);

LIBTEST_API void
Utf8DelegateAsParameter(Callback2 managedCallback)
{
    for (int i = 0; i < NSTRINGS; ++i) 
    {
        char *pszNative = 0;
        pszNative = (char*)utf8Strings[i];
        managedCallback(pszNative, i);
    }
}


LIBTEST_API char*
StringBuilderParameterReturn(int index)
{
    char *pszTextutf8 = (char*)utf8Strings[index];
    size_t strLength = strlen(pszTextutf8);
    char * ret = (char *)(marshal_alloc (sizeof(char)* (strLength + 1)));
    memcpy(ret, pszTextutf8, strLength);
    ret[strLength] = '\0';

    return  ret;
}

LIBTEST_API int STDCALL
mono_test_marshal_pointer_array (int *arr[])
{
	int i;

	for (i = 0; i < 10; ++i) {
		if (*arr [i] != -1)
			return 1;
	}
	return 0;
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

typedef void (*VoidVoidCallback) (void);
typedef void (*MonoFtnPtrEHCallback) (guint32 gchandle);

typedef void *MonoDomain;
typedef void *MonoAssembly;
typedef void *MonoImage;
typedef void *MonoClass;
typedef void *MonoMethod;
typedef void *MonoThread;

typedef long long MonoObject;
typedef MonoObject MonoException;
typedef int32_t mono_bool;

static int sym_inited = 0;
static void (*sym_mono_install_ftnptr_eh_callback) (MonoFtnPtrEHCallback);
static MonoObject* (*sym_mono_gchandle_get_target) (guint32 gchandle);
static guint32 (*sym_mono_gchandle_new) (MonoObject *, mono_bool pinned);
static void (*sym_mono_gchandle_free) (guint32 gchandle);
static void (*sym_mono_raise_exception) (MonoException *ex);
static void (*sym_mono_domain_unload) (gpointer);
static void (*sym_mono_threads_exit_gc_safe_region_unbalanced) (gpointer, gpointer *);
static void (*null_function_ptr) (void);

static MonoDomain *(*sym_mono_get_root_domain) (void);

static MonoDomain *(*sym_mono_domain_get)(void);

static mono_bool (*sym_mono_domain_set)(MonoDomain *, mono_bool /*force */);

static MonoAssembly *(*sym_mono_domain_assembly_open) (MonoDomain *, const char*);

static MonoImage *(*sym_mono_assembly_get_image) (MonoAssembly *);

static MonoClass *(*sym_mono_class_from_name)(MonoImage *, const char *, const char *);

static MonoMethod *(*sym_mono_class_get_method_from_name)(MonoClass *, const char *, int /* arg_count */);

static MonoThread *(*sym_mono_thread_attach)(MonoDomain *);

static void (*sym_mono_thread_detach)(MonoThread *);

static MonoObject *(*sym_mono_runtime_invoke) (MonoMethod *, void*, void**, MonoObject**);


// SYM_LOOKUP(mono_runtime_invoke)
// expands to
//  sym_mono_runtime_invoke = g_cast (lookup_mono_symbol ("mono_runtime_invoke"));
//
// (the g_cast is necessary for C++ builds)
#define SYM_LOOKUP(name) do {			\
	sym_##name = g_cast (lookup_mono_symbol (#name));	\
	} while (0)

static void
mono_test_init_symbols (void)
{
	if (sym_inited)
		return;

	SYM_LOOKUP (mono_install_ftnptr_eh_callback);
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

	sym_inited = 1;
}

#ifndef TARGET_WASM

static jmp_buf test_jmp_buf;
static guint32 test_gchandle;

static void
mono_test_longjmp_callback (guint32 gchandle)
{
	test_gchandle = gchandle;
	longjmp (test_jmp_buf, 1);
}

LIBTEST_API void STDCALL
mono_test_setjmp_and_call (VoidVoidCallback managedCallback, intptr_t *out_handle)
{
	mono_test_init_symbols ();
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

static void (*mono_test_capture_throw_callback) (guint32 gchandle, guint32 *exception_out);

static void
mono_test_ftnptr_eh_callback (guint32 gchandle)
{
	guint32 exception_handle = 0;

	g_assert (gchandle != 0);
	MonoObject *exc = sym_mono_gchandle_get_target (gchandle);
	sym_mono_gchandle_free (gchandle);

	guint32 handle = sym_mono_gchandle_new (exc, FALSE);
	mono_test_capture_throw_callback (handle, &exception_handle);
	sym_mono_gchandle_free (handle);

	g_assert (exception_handle != 0);
	exc = sym_mono_gchandle_get_target (exception_handle);
	sym_mono_gchandle_free (exception_handle);

	sym_mono_raise_exception (exc);
	g_error ("mono_raise_exception should not return");
}

LIBTEST_API void STDCALL
mono_test_setup_ftnptr_eh_callback (VoidVoidCallback managed_entry, void (*capture_throw_callback) (guint32, guint32 *))
{
	mono_test_init_symbols ();
	mono_test_capture_throw_callback = capture_throw_callback;
	sym_mono_install_ftnptr_eh_callback (mono_test_ftnptr_eh_callback);
	managed_entry ();
}

LIBTEST_API void STDCALL
mono_test_cleanup_ftptr_eh_callback (void)
{
	mono_test_init_symbols ();
	sym_mono_install_ftnptr_eh_callback (NULL);
}

LIBTEST_API int STDCALL
mono_test_cominterop_ccw_queryinterface (MonoComObject *pUnk)
{
	void *pp;
	int hr = pUnk->vtbl->QueryInterface (pUnk, &IID_INotImplemented, &pp);

	// Return true if we can't get INotImplemented
	return pUnk == NULL && hr == S_OK;
}

typedef struct ccw_qi_shared_data {
	MonoComObject *pUnk;
	int i;
} ccw_qi_shared_data;

static void*
ccw_qi_foreign_thread (void *arg)
{
	ccw_qi_shared_data *shared = (ccw_qi_shared_data *)arg;
	void *pp;
	MonoComObject *pUnk = shared->pUnk;
	int hr = pUnk->vtbl->QueryInterface (pUnk, &IID_ITest, &pp);

	shared->i = (hr == S_OK) ? 0 : 43;
	return NULL;
}

LIBTEST_API int STDCALL
mono_test_cominterop_ccw_queryinterface_foreign_thread (MonoComObject *pUnk)
{
#ifdef WIN32
	return 0;
#else
	pthread_t t;
	ccw_qi_shared_data *shared = (ccw_qi_shared_data *)malloc (sizeof (ccw_qi_shared_data));
	if (!shared)
		abort ();
	shared->pUnk = pUnk;
	shared->i = 1;
	int res = pthread_create (&t, NULL, ccw_qi_foreign_thread, (void*)shared);
	g_assert (res == 0);
	pthread_join (t, NULL);
	int result = shared->i;
	free (shared);
	return result;
#endif
}

static void*
ccw_itest_foreign_thread (void *arg)
{
	ccw_qi_shared_data *shared = (ccw_qi_shared_data *)arg;
	MonoComObject *pUnk = shared->pUnk;
	int hr = pUnk->vtbl->SByteIn (pUnk, -100);
	shared->i = (hr == S_OK) ? 0 : 12;
	return NULL;
}

LIBTEST_API int STDCALL
mono_test_cominterop_ccw_itest_foreign_thread (MonoComObject *pUnk)
{
#ifdef WIN32
	return 0;
#else
	pthread_t t;
	ccw_qi_shared_data *shared = (ccw_qi_shared_data *)malloc (sizeof (ccw_qi_shared_data));
	if (!shared)
		abort ();
	shared->pUnk = pUnk;
	shared->i = 1;
	int res = pthread_create (&t, NULL, ccw_itest_foreign_thread, (void*)shared);
	g_assert (res == 0);
	pthread_join (t, NULL);
	int result = shared->i;
	free (shared);
	return result;
#endif
}


LIBTEST_API void STDCALL
mono_test_MerpCrashSnprintf (void)
{
	fprintf (stderr, "Before overwrite\n");

	char buff [1] = { '\0' };
	char overflow [1] = { 'a' }; // Not null-terminated
	g_snprintf (buff, sizeof(buff) * 10, "THISSHOULDOVERRUNTERRIBLY%s", overflow);
	g_snprintf ((char *) GINT_TO_POINTER(-1), sizeof(buff) * 10, "THISSHOULDOVERRUNTERRIBLY%s", overflow);
}

LIBTEST_API void STDCALL
mono_test_MerpCrashDladdr (void)
{
#ifndef HOST_WIN32
	dlopen (GINT_TO_POINTER(-1), -1);
#endif
}

LIBTEST_API void STDCALL
mono_test_MerpCrashMalloc (void)
{
	gpointer x = g_malloc (sizeof(gpointer));
	g_free (x);

	// Double free
	g_free (x);
}

LIBTEST_API void STDCALL
mono_test_MerpCrashNullFp (void)
{
	null_function_ptr ();
}

LIBTEST_API void STDCALL
mono_test_MerpCrashDomainUnload (void)
{
	mono_test_init_symbols ();
	sym_mono_domain_unload (GINT_TO_POINTER (-1));
}

LIBTEST_API void STDCALL
mono_test_MerpCrashUnbalancedGCSafe (void)
{
	mono_test_init_symbols ();
	gpointer foo = GINT_TO_POINTER (-1);
	gpointer bar = GINT_TO_POINTER (-2);
	sym_mono_threads_exit_gc_safe_region_unbalanced (foo, &bar);
}

LIBTEST_API void STDCALL
mono_test_MerpCrashUnhandledExceptionHook (void)
{
	g_assert_not_reached ();
}

LIBTEST_API void STDCALL
mono_test_MerpCrashSignalTerm (void)
{
	raise (SIGTERM);
}

// for the rest of the signal tests, we use SIGTERM as a fallback

LIBTEST_API void STDCALL
mono_test_MerpCrashSignalAbrt (void)
{
#if defined (SIGABRT)
	raise (SIGABRT);
#else
	raise (SIGTERM);
#endif
}

LIBTEST_API void STDCALL
mono_test_MerpCrashSignalFpe (void)
{
#if defined (SIGFPE)
	raise (SIGFPE);
#else
	raise (SIGTERM);
#endif
}

LIBTEST_API void STDCALL
mono_test_MerpCrashSignalBus (void)
{
#if defined (SIGBUS)
	raise (SIGBUS);
#else
	raise (SIGTERM);
#endif
}

LIBTEST_API void STDCALL
mono_test_MerpCrashSignalSegv (void)
{
#if defined (SIGSEGV)
	raise (SIGSEGV);
#else
	raise (SIGTERM);
#endif
}

LIBTEST_API void STDCALL
mono_test_MerpCrashSignalIll (void)
{
#if defined (SIGILL)
	raise (SIGILL);
#else
	raise (SIGTERM);
#endif
}

typedef struct _TestAutoDual _TestAutoDual;

typedef struct
{
	int (STDCALL *QueryInterface)(_TestAutoDual *iface, REFIID iid, gpointer *out);
	int (STDCALL *AddRef)(_TestAutoDual *iface);
	int (STDCALL *Release)(_TestAutoDual *iface);
	int (STDCALL *GetTypeInfoCount)(_TestAutoDual *iface, unsigned int *count);
	int (STDCALL *GetTypeInfo)(_TestAutoDual *iface, unsigned int index, unsigned int lcid, gpointer *out);
	int (STDCALL *GetIDsOfNames)(_TestAutoDual *iface, REFIID iid, gpointer names, unsigned int count, unsigned int lcid, gpointer ids);
	int (STDCALL *Invoke)(_TestAutoDual *iface, unsigned int dispid, REFIID iid, unsigned int lcid, unsigned short flags, gpointer params, gpointer result, gpointer excepinfo, gpointer err_arg);
	int (STDCALL *ToString)(_TestAutoDual *iface, gpointer string);
	int (STDCALL *Equals)(_TestAutoDual *iface, VARIANT other, short *retval);
	int (STDCALL *GetHashCode)(_TestAutoDual *iface, int *retval);
	int (STDCALL *GetType)(_TestAutoDual *iface, gpointer retval);
	int (STDCALL *parent_method_virtual)(_TestAutoDual *iface, int *retval);
	int (STDCALL *get_parent_property)(_TestAutoDual *iface, int *retval);
	int (STDCALL *parent_method_override)(_TestAutoDual *iface, int *retval);
	int (STDCALL *parent_iface_method)(_TestAutoDual *iface, int *retval);
	int (STDCALL *parent_method)(_TestAutoDual *iface, int *retval);
	int (STDCALL *child_method_virtual)(_TestAutoDual *iface, int *retval);
	int (STDCALL *iface1_method)(_TestAutoDual *iface, int *retval);
	int (STDCALL *iface1_parent_method)(_TestAutoDual *iface, int *retval);
	int (STDCALL *iface2_method)(_TestAutoDual *iface, int *retval);
	int (STDCALL *child_method)(_TestAutoDual *iface, int *retval);
} _TestAutoDualVtbl;

struct _TestAutoDual
{
	const _TestAutoDualVtbl *lpVtbl;
};

LIBTEST_API int STDCALL
mono_test_ccw_class_type_auto_dual (_TestAutoDual *iface)
{
	int hr, retval;

	hr = iface->lpVtbl->parent_method_virtual(iface, &retval);
	if (hr != 0)
		return 1;
	if (retval != 101)
		return 2;

	hr = iface->lpVtbl->get_parent_property(iface, &retval);
	if (hr != 0)
		return 3;
	if (retval != 102)
		return 4;

	hr = iface->lpVtbl->parent_method_override(iface, &retval);
	if (hr != 0)
		return 5;
	if (retval != 203)
		return 6;

	hr = iface->lpVtbl->parent_method(iface, &retval);
	if (hr != 0)
		return 7;
	if (retval != 104)
		return 8;

	hr = iface->lpVtbl->child_method_virtual(iface, &retval);
	if (hr != 0)
		return 11;
	if (retval != 106)
		return 12;

	hr = iface->lpVtbl->iface1_method(iface, &retval);
	if (hr != 0)
		return 13;
	if (retval != 107)
		return 14;

	hr = iface->lpVtbl->iface1_parent_method(iface, &retval);
	if (hr != 0)
		return 15;
	if (retval != 108)
		return 16;

	hr = iface->lpVtbl->iface2_method(iface, &retval);
	if (hr != 0)
		return 17;
	if (retval != 109)
		return 18;

	hr = iface->lpVtbl->child_method(iface, &retval);
	if (hr != 0)
		return 19;
	if (retval != 110)
		return 20;

	hr = iface->lpVtbl->parent_iface_method(iface, &retval);
	if (hr != 0)
		return 23;
	if (retval != 112)
		return 24;

	return 0;
}

static const GUID IID_IBanana = {0x12345678, 0, 0, {0, 0, 0, 0, 0, 0, 0, 2}};

typedef struct IBanana IBanana;

typedef struct
{
	int (STDCALL *QueryInterface)(IBanana *iface, REFIID iid, gpointer *out);
	int (STDCALL *AddRef)(IBanana *iface);
	int (STDCALL *Release)(IBanana *iface);
	int (STDCALL *GetTypeInfoCount)(IBanana *iface, unsigned int *count);
	int (STDCALL *GetTypeInfo)(IBanana *iface, unsigned int index, unsigned int lcid, gpointer *out);
	int (STDCALL *GetIDsOfNames)(IBanana *iface, REFIID iid, gpointer names, unsigned int count, unsigned int lcid, gpointer ids);
	int (STDCALL *Invoke)(IBanana *iface, unsigned int dispid, REFIID iid, unsigned int lcid, unsigned short flags, gpointer params, gpointer result, gpointer excepinfo, gpointer err_arg);
	int (STDCALL *iface1_method)(IBanana *iface, int *retval);
} IBananaVtbl;

struct IBanana
{
	const IBananaVtbl *lpVtbl;
};

LIBTEST_API int STDCALL
mono_test_ccw_class_type_none (IBanana *iface)
{
	int hr, retval;

	hr = iface->lpVtbl->iface1_method(iface, &retval);
	if (hr != 0)
		return 1;
	if (retval != 3)
		return 2;
	return 0;
}

LIBTEST_API int STDCALL
mono_test_ccw_class_type_auto_dispatch (IDispatch *disp)
{
	IBanana *banana;
	int hr, retval;

#ifdef __cplusplus
	hr = disp->QueryInterface (IID_IBanana, (void **)&banana);
#else
	hr = disp->lpVtbl->QueryInterface (disp, &IID_IBanana, (void **)&banana);
#endif
	if (hr != 0)
		return 1;
	hr = banana->lpVtbl->iface1_method(banana, &retval);
	if (hr != 0)
		return 2;
	if (retval != 3)
		return 3;
	banana->lpVtbl->Release(banana);

	return 0;
}

static guint8 static_arr[] = { 1, 2, 3, 4 };

LIBTEST_API guint8*
mono_test_marshal_return_array (void)
{
	return static_arr;
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
	struct invoke_names *names = (struct invoke_names*) malloc (sizeof (struct invoke_names));
	names->assm_name = strdup (assm_name);
	names->name_space = strdup (name_space);
	names->name = strdup (name);
	names->meth_name = strdup (meth_name);
	return names;
}

static void
destroy_invoke_names (struct invoke_names *n)
{
	free (n->assm_name);
	free (n->name_space);
	free (n->name);
	free (n->meth_name);
	free (n);
}

static void
test_invoke_by_name (struct invoke_names *names)
{
	mono_test_init_symbols ();

	MonoDomain *domain = sym_mono_domain_get ();
	MonoThread *thread = NULL;
	if (!domain) {
		thread = sym_mono_thread_attach (sym_mono_get_root_domain ());
	}
	domain = sym_mono_domain_get ();
	g_assert (domain);
	MonoAssembly *assm = sym_mono_domain_assembly_open (domain, names->assm_name);
	g_assert (assm);
	MonoImage *image = sym_mono_assembly_get_image (assm);
	MonoClass *klass = sym_mono_class_from_name (image, names->name_space, names->name);
	g_assert (klass);
	/* meth_name should be a static method that takes no arguments */
	MonoMethod *method = sym_mono_class_get_method_from_name (klass, names->meth_name, -1);
	g_assert (method);

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
                sleep (2);
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
		sleep (2);
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
		g_assert (res == 0);
		pthread_join (t, NULL);
		return 0;
	} else {
		pthread_t t;
		int res = pthread_create (&t, NULL, invoke_foreign_delegate, del);
		g_assert (res == 0);
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
	g_assert_not_reached ();
}
#endif

LIBTEST_API mono_bool STDCALL
mono_test_attach_invoke_block_foreign_thread (const char *assm_name, const char *name_space, const char *name, const char *meth_name, VoidVoidCallback del)
{
#ifndef HOST_WIN32
	struct names_and_mutex *nm = malloc (sizeof (struct names_and_mutex));
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
	g_assert (res == 0);
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

static const GUID IID_IDrupe = {0x9f001e6b, 0xa244, 0x3911, {0x88,0xdb, 0xbb,0x2b,0x6d,0x58,0x43,0xaa}};

#ifndef HOST_WIN32
typedef struct IUnknown IUnknown;

typedef struct
{
	int (STDCALL *QueryInterface)(IUnknown *iface, REFIID iid, gpointer *out);
	int (STDCALL *AddRef)(IUnknown *iface);
	int (STDCALL *Release)(IUnknown *iface);
} IUnknownVtbl;

struct IUnknown
{
	const IUnknownVtbl *lpVtbl;
};
#endif

LIBTEST_API int STDCALL
mono_test_ccw_query_interface (IUnknown *iface)
{
	IUnknown *drupe;
	int hr;

#ifdef __cplusplus
	hr = iface->QueryInterface (IID_IDrupe, (void **)&drupe);
#else
	hr = iface->lpVtbl->QueryInterface (iface, &IID_IDrupe, (void **)&drupe);
#endif
	if (hr != 0)
		return 1;
#ifdef __cplusplus
	drupe->Release();
#else
	drupe->lpVtbl->Release(drupe);
#endif

	return 0;
}

#ifdef __cplusplus
} // extern C
#endif
