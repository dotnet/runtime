#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <glib.h>
#include <gmodule.h>
#include <errno.h>
#include <time.h>
#include <math.h>

#ifdef WIN32
#include <windows.h>
#include "initguid.h"
#else
#include <pthread.h>
#endif

#ifdef WIN32
#define STDCALL __stdcall
#else
#define STDCALL
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
	int len;
	char *buf;

	if (!str)
		return NULL;

	len = strlen (str);
	buf = (char *) CoTaskMemAlloc (len + 1);
	return strcpy (buf, str);
#else
	return g_strdup (str);
#endif
}

static gunichar2* marshal_bstr_alloc(const gchar* str)
{
#ifdef WIN32
	gunichar2* ret = NULL;
	gunichar2* temp = NULL;
	temp = g_utf8_to_utf16 (str, -1, NULL, NULL, NULL);
	ret = SysAllocString (temp);
	g_free (temp);
	return ret;
#else
	gchar* ret = NULL;
	int slen = strlen (str);
	gunichar2* temp;
	/* allocate len + 1 utf16 characters plus 4 byte integer for length*/
	ret = g_malloc ((slen + 1) * sizeof(gunichar2) + sizeof(guint32));
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
test_lpwstr_marshal (unsigned short* chars, long length)
{
	int i = 0;
	unsigned short *res;

	res = marshal_alloc (2 * (length + 1));

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

	*chars = marshal_alloc (2 * (len + 1));
	
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
	ss->d = (gpointer)0x12345678;

	func (a, ss, b);

	if (ss->a && ss->b && ss->c && !strcmp (ss->d, "TEST3"))
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
mono_test_marshal_struct (simplestruct ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST"))
		return 0;

	return 1;
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

static void *
get_sp (void)
{
	int i;
	void *p;

	/* Yes, this is correct, we are only trying to determine the value of the stack here */
	p = &i;
	return p;
}

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
	int res;

	res = delegate (NULL);

	return 0;
}

typedef int (STDCALL *SimpleDelegate7) (simplestruct **ss);

LIBTEST_API int STDCALL 
mono_test_marshal_delegate7 (SimpleDelegate7 delegate)
{
	int res;
	simplestruct *ptr;

	/* Check that the input pointer is ignored */
	ptr = (gpointer)0x12345678;

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
	return delegate (ftn);
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

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";

	if (strcmp (s, "ABCD") != 0)
		return 1;
	strncpy(s, m, n);
	s [n] = '\0';
	return 0;
}

LIBTEST_API int STDCALL  
mono_test_marshal_stringbuilder_default (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";

	strncpy(s, m, n);
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

	str = marshal_alloc (strlen (m) + 1);
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
	*s = marshal_alloc (len);
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

	str = marshal_alloc (strlen (m) + 1);
	memcpy (str, m, strlen (m) + 1);
	
	*s = str;
	return 0;
}

typedef struct {
#ifndef __GNUC__
    char a;
#endif
} EmptyStruct;

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
	GError *error = NULL;
	char *s;
	
	s = g_utf16_to_utf8 (array [0], -1, NULL, NULL, &error);
	if (strcmp (s, "ABC")) {
		g_free (s);
		return 1;
	}
	else
		g_free (s);

	s = g_utf16_to_utf8 (array [1], -1, NULL, NULL, &error);
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

typedef struct {
       char a[100];
} ByValStrStruct;

LIBTEST_API ByValStrStruct * STDCALL 
mono_test_byvalstr_gen (void)
{
	ByValStrStruct *ret;
       
	ret = malloc(sizeof(ByValStrStruct));
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
	SimpleObj *res = malloc (sizeof (SimpleObj));

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
#else
	errno = err;
#endif
}

LIBTEST_API int STDCALL 
mono_test_asany (void *ptr, int what)
{
	switch (what) {
	case 1:
		return (*(int*)ptr == 5) ? 0 : 1;
	case 2:
		return strcmp (ptr, "ABC") == 0 ? 0 : 1;
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
		GError *error = NULL;
		char *s;

		s = g_utf16_to_utf8 (ptr, -1, NULL, NULL, &error);

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
	AsAnyStruct* asAny = ptr;
	int res = asAny->i + asAny->j + asAny->k;

	return res;
}

LIBTEST_API int STDCALL 
mono_test_marshal_asany_inout (void* ptr)
{
	AsAnyStruct* asAny = ptr;
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
	AsAnyStruct* asAny = ptr;
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

	ptr = del (&buf);

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
	void* pptr = del;

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
	return &add_delegate;
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
		*handle = (void *) 0xbad;
		return;
	}

	*handle = (void *) 0x800d;
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

typedef struct
{
	guint32 a;
	guint16 b;
	guint16 c;
	guint8 d[8];
} GUID;

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
mono_test_marshal_variant_out_byte(VARIANT* variant)
{	
	variant->vt = VT_UI1;
	variant->bVal = 100;

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
mono_test_marshal_variant_out_ushort(VARIANT* variant)
{
	variant->vt = VT_UI2;
	variant->uiVal = 314;

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
mono_test_marshal_variant_out_uint(VARIANT* variant)
{
	variant->vt = VT_UI4;
	variant->ulVal = 314;

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
mono_test_marshal_variant_out_ulong(VARIANT* variant)
{
	variant->vt = VT_UI8;
	variant->ullVal = 314;

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
mono_test_marshal_variant_out_double(VARIANT* variant)
{
	variant->vt = VT_R8;
	variant->dblVal = 3.14;

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
mono_test_marshal_variant_out_bool_true (VARIANT* variant)
{
	variant->vt = VT_BOOL;
	variant->boolVal = VARIANT_TRUE;

	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_variant_out_bool_false (VARIANT* variant)
{
	variant->vt = VT_BOOL;
	variant->boolVal = VARIANT_FALSE;

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

typedef struct MonoComObject MonoComObject;

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
} MonoIUnknown;

struct MonoComObject
{
	MonoIUnknown* vtbl;
	int m_ref;
};

static GUID IID_ITest = {0, 0, 0, {0,0,0,0,0,0,0,1}};
static GUID IID_IMonoUnknown = {0, 0, 0, {0xc0,0,0,0,0,0,0,0x46}};
static GUID IID_IMonoDispatch = {0x00020400, 0, 0, {0xc0,0,0,0,0,0,0,0x46}};

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
get_ITest(MonoComObject* pUnk, MonoComObject* *ppUnk)
{
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

/*
 * mono_method_get_unmanaged_thunk tests
 */

#if defined(__GNUC__) && ((defined(__i386__) && (defined(__linux__) || defined (__APPLE__)) || defined (__FreeBSD__) || defined(__OpenBSD__)) || (defined(__ppc__) && defined(__APPLE__)))
#define ALIGN(size) __attribute__ ((aligned(size)))
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
	gpointer symbol;
	if (g_module_symbol (g_module_open (NULL, G_MODULE_BIND_LAZY), symbol_name, &symbol))
		return symbol;
	else
		return NULL;
}

LIBTEST_API gpointer STDCALL
mono_test_marshal_lookup_symbol (const char *symbol_name)
{
	return lookup_mono_symbol (symbol_name);
}

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
	gpointer (*mono_method_get_unmanaged_thunk)(gpointer)
		= lookup_mono_symbol ("mono_method_get_unmanaged_thunk");

	gpointer (*mono_string_new_wrapper)(const char *)
		= lookup_mono_symbol ("mono_string_new_wrapper");

	char* (*mono_string_to_utf8)(gpointer)
		= lookup_mono_symbol ("mono_string_to_utf8");

	gpointer (*mono_object_unbox)(gpointer)
		= lookup_mono_symbol ("mono_object_unbox");

	gpointer test_method, ex = NULL;
	gpointer (STDCALL *CreateObject)(gpointer*);


	if (!mono_method_get_unmanaged_thunk)
		return 1;

	test_method =  mono_method_get_unmanaged_thunk (test_method_handle);
	if (!test_method)
		return 2;

	CreateObject = mono_method_get_unmanaged_thunk (create_object_method_handle);
	if (!CreateObject)
		return 3;


	switch (test_id) {

	case 0: {
		/* thunks.cs:Test.Test0 */
		void (STDCALL *F)(gpointer*) = test_method;
		F (&ex);
		break;
	}

	case 1: {
		/* thunks.cs:Test.Test1 */
		int (STDCALL *F)(gpointer*) = test_method;
		if (F (&ex) != 42)
			return 4;
		break;
	}

	case 2: {
		/* thunks.cs:Test.Test2 */
		gpointer (STDCALL *F)(gpointer, gpointer*) = test_method;
		gpointer str = mono_string_new_wrapper ("foo");
		if (str != F (str, &ex))
			return 4;
		break;
	}

	case 3: {
		/* thunks.cs:Test.Test3 */
		gpointer (STDCALL *F)(gpointer, gpointer, gpointer*);
		gpointer obj;
		gpointer str;

		F = test_method;
		obj = CreateObject (&ex);
		str = mono_string_new_wrapper ("bar");

		if (str != F (obj, str, &ex))
			return 4;
		break;
	}

	case 4: {
		/* thunks.cs:Test.Test4 */
		int (STDCALL *F)(gpointer, gpointer, int, gpointer*);
		gpointer obj;
		gpointer str;

		F = test_method;
		obj = CreateObject (&ex);
		str = mono_string_new_wrapper ("bar");

		if (42 != F (obj, str, 42, &ex))
			return 4;

		break;
	}

	case 5: {
		/* thunks.cs:Test.Test5 */
		int (STDCALL *F)(gpointer, gpointer, int, gpointer*);
		gpointer obj;
		gpointer str;

		F = test_method;
		obj = CreateObject (&ex);
		str = mono_string_new_wrapper ("bar");

		F (obj, str, 42, &ex);
		if (!ex)
		    return 4;

		break;
	}

	case 6: {
		/* thunks.cs:Test.Test6 */
		int (STDCALL *F)(gpointer, guint8, gint16, gint32, gint64, float, double,
				 gpointer, gpointer*);
		gpointer obj;
		gpointer str = mono_string_new_wrapper ("Test6");
		int res;

		F = test_method;
		obj = CreateObject (&ex);

		res = F (obj, 254, 32700, -245378, 6789600, 3.1415, 3.1415, str, &ex);
		if (ex)
			return 4;

		if (!res)
			return 5;

		break;
	}

	case 7: {
		/* thunks.cs:Test.Test7 */
		gint64 (STDCALL *F)(gpointer*) = test_method;
		if (F (&ex) != G_MAXINT64)
			return 4;
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

		F = test_method;

		F (&a1, &a2, &a3, &a4, &a5, &a6, &a7, &ex);
		if (ex)
			return 4;

		if (!(a1 == 254 &&
		      a2 == 32700 &&
		      a3 == -245378 &&
		      a4 == 6789600 &&
		      (fabs (a5 - 3.1415) < 0.001) &&
		      (fabs (a6 - 3.1415) < 0.001) &&
		      strcmp (mono_string_to_utf8 (a7), "Test8") == 0))
			return 5;

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

		F = test_method;

		F (&a1, &a2, &a3, &a4, &a5, &a6, &a7, &ex);
		if (!ex)
			return 4;

		break;
	}

	case 10: {
		/* thunks.cs:Test.Test10 */
		void (STDCALL *F)(gpointer*, gpointer*);

		gpointer obj1, obj2;

		obj1 = obj2 = CreateObject (&ex);
		if (ex)
			return 4;

		F = test_method;

		F (&obj1, &ex);
		if (ex)
			return 5;

		if (obj1 == obj2)
			return 6;

		break;
	}

	case 100: {
		/* thunks.cs:TestStruct.Test0 */
		int (STDCALL *F)(gpointer*, gpointer*);

		gpointer obj;
		TestStruct *a1;
		int res;

		obj = CreateObject (&ex);
		if (ex)
			return 4;

		if (!obj)
			return 5;

		a1 = mono_object_unbox (obj);
		if (!a1)
			return 6;

		a1->A = 42;
		a1->B = 3.1415;

		F = test_method;

		res = F (obj, &ex);
		if (ex)
			return 7;

		if (!res)
			return 8;

		/* check whether the call was really by value */
		if (a1->A != 42 || a1->B != 3.1415)
			return 9;

		break;
	}

	case 101: {
		/* thunks.cs:TestStruct.Test1 */
		void (STDCALL *F)(gpointer, gpointer*);

		TestStruct *a1;
		gpointer obj;

		obj = CreateObject (&ex);
		if (ex)
			return 4;

		if (!obj)
			return 5;

		a1 = mono_object_unbox (obj);
		if (!a1)
			return 6;

		F = test_method;

		F (obj, &ex);
		if (ex)
			return 7;

		if (a1->A != 42)
			return 8;

		if (!fabs (a1->B - 3.1415) < 0.001)
			return 9;

		break;
	}

	case 102: {
		/* thunks.cs:TestStruct.Test2 */
		gpointer (STDCALL *F)(gpointer*);

		TestStruct *a1;
		gpointer obj;

		F = test_method;

		obj = F (&ex);
		if (ex)
			return 4;

		if (!obj)
			return 5;

		a1 = mono_object_unbox (obj);

		if (a1->A != 42)
			return 5;

		if (!fabs (a1->B - 3.1415) < 0.001)
			return 6;

		break;
	}

	case 103: {
		/* thunks.cs:TestStruct.Test3 */
		void (STDCALL *F)(gpointer, gpointer*);

		TestStruct *a1;
		gpointer obj;

		obj = CreateObject (&ex);
		if (ex)
			return 4;

		if (!obj)
			return 5;
		
		a1 = mono_object_unbox (obj);

		if (!a1)
			return 6;

		a1->A = 42;
		a1->B = 3.1415;

		F = test_method;

		F (obj, &ex);
		if (ex)
			return 4;

		if (a1->A != 1)
			return 5;

		if (a1->B != 17)
			return 6;

		break;
	}

	default:
		return 9;

	}

	return 0;
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
	val.c = 0x99;
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

	pSA= SafeArrayCreate (VT_VARIANT, 1, dimensions);
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

#endif

static int call_managed_res;

static void
call_managed (gpointer arg)
{
	SimpleDelegate del = arg;

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

	res = pthread_create (&t, NULL, (gpointer)call_managed, del);
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
	char *res = marshal_alloc (4);
	strcpy (res, "XYZ");
	return res;
}


LIBTEST_API gunichar2* STDCALL
mono_test_marshal_return_lpwstr (void)
{
	gunichar2 *res = marshal_alloc (8);
	gunichar2* tmp = g_utf8_to_utf16 ("XYZ", -1, NULL, NULL, NULL);

	memcpy (res, tmp, 8);
	g_free (tmp);

	return res;
}
