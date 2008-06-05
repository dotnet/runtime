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
	return g_strdup ("12345");
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
	SimpleDelegate func, func2;
} DelegateStruct;

LIBTEST_API DelegateStruct STDCALL 
mono_test_marshal_delegate_struct (DelegateStruct ds)
{
	DelegateStruct res;

	res.a = ds.func (ds.a) + ds.func2 (ds.a);
	res.func = ds.func;
	res.func2 = ds.func2;

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
	ss->d = g_strdup ("DEF");

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

	res = g_new0 (simplestruct2, 1);
	memcpy (res, ss, sizeof (simplestruct2));
	res->d = g_strdup ("TEST");
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

	res = g_new0 (simplestruct2, 1);
	memcpy (res, ss, sizeof (simplestruct2));
	res->d = g_strdup ("TEST-RES");

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

	marshal_free (data);
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

	res->str = g_strdup ("ABC");
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
	*str = g_strdup ("TEST1");
}

LIBTEST_API int STDCALL 
string_marshal_test2 (char **str)
{
	// printf ("string_marshal_test2 %s\n", *str);

	if (strcmp (*str, "TEST1"))
		return -1;

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

		res = g_new0 (BlittableClass, 1);
		memcpy (res, vl, sizeof (BlittableClass));
	} else {
		res = g_new0 (BlittableClass, 1);
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
mono_test_marshal_ia64_pass_return_struct5 (double d1, double d2, test_struct5 s, double d3, double d4)
{
	s.d1 += d1 + d2;
	s.d2 += d3 + d4;

	return s;
}

typedef struct test_struct6 {
	double d1, d2;
} test_struct6;

LIBTEST_API test_struct6 STDCALL 
mono_test_marshal_ia64_pass_return_struct6 (double d1, double d2, test_struct6 s, double d3, double d4)
{
	s.d1 += d1 + d2;
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

typedef char** (*ReturnStringArrayDelegate) (int i);

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
/*
 * COM INTEROP TESTS
 */

#ifdef WIN32

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_in(BSTR bstr)
{
	if (!wcscmp(bstr, L"mono_test_marshal_bstr_in"))
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_out(BSTR* bstr)
{
	*bstr = SysAllocString(L"mono_test_marshal_bstr_out");
	return 0;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_in_null(BSTR bstr)
{
	if (!bstr)
		return 0;
	return 1;
}

LIBTEST_API int STDCALL 
mono_test_marshal_bstr_out_null(BSTR* bstr)
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
	if (variant.vt == VT_BSTR && !wcscmp(variant.bstrVal, L"PI"))
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
	variant->bstrVal = SysAllocString(L"PI");

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
	vt.bstrVal = SysAllocString(L"PI");
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
	VariantInit (&vt);
	func (VT_BSTR, &vt);
	if (vt.vt == VT_BSTR && !wcscmp(vt.bstrVal, L"PI"))
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
	int (STDCALL *LongIn)(MonoComObject* pUnk, LONGLONG a);
	int (STDCALL *ULongIn)(MonoComObject* pUnk, ULONGLONG a);
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

DEFINE_GUID(IID_ITest, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);
DEFINE_GUID(IID_IMonoUnknown, 0, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);
DEFINE_GUID(IID_IMonoDispatch, 0x00020400, 0, 0, 0xC0, 0, 0, 0, 0, 0, 0, 0x46);

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
	return E_NOINTERFACE;
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
LongIn(MonoComObject* pUnk, LONGLONG a)
{
	return S_OK;
}

LIBTEST_API int STDCALL 
ULongIn(MonoComObject* pUnk, ULONGLONG a)
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
	*pOut = g_new0 (MonoComObject, 1);
	(*pOut)->vtbl = g_new0 (MonoIUnknown, 1);

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


#endif //NOT_YET


/*
 * mono_method_get_unmanaged_thunk tests
 */

#if defined(__GNUC__) && defined(__i386__) && (defined(__linux__) || defined (__APPLE__))
#define ALIGN(size) __attribute__ ((aligned(size)))
#else
#define ALIGN(size)
#endif


/* thunks.cs:TestStruct */
typedef struct _TestStruct {
	int A;
	double B ALIGN(8);  /* align according to  mono's struct layout */
} TestStruct;

/* Searches for mono symbols in all loaded modules */
static gpointer
lookup_mono_symbol (char *symbol_name)
{
	gpointer symbol;
	if (g_module_symbol (g_module_open (NULL, G_MODULE_BIND_LAZY), symbol_name, &symbol))
		return symbol;
	else
		return NULL;
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

	gpointer (*mono_string_new_wrapper)(char *)
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

struct winx64_struct1
{
	char a;
};

LIBTEST_API int STDCALL  
test_Winx64_struct1_in (struct winx64_struct1 var)
{
	if (var.a != 123)
		return 1;
	return 0;
}

struct winx64_struct2
{
	char a;
	char b;
};

LIBTEST_API int STDCALL  
test_Winx64_struct2_in (struct winx64_struct2 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	return 0;
}


struct winx64_struct3
{
	char a;
	char b;
	short c;
};

LIBTEST_API int STDCALL  
test_Winx64_struct3_in (struct winx64_struct3 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	if (var.c != 0x1234)
		return 3;
	return 0;
}

struct winx64_struct4
{
	char a;
	char b;
	short c;
	unsigned int d;
};

LIBTEST_API int STDCALL  
test_Winx64_struct4_in (struct winx64_struct4 var)
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

struct winx64_struct5
{
	char a;
	char b;
	char c;
};

LIBTEST_API int STDCALL  
test_Winx64_struct5_in (struct winx64_struct5 var)
{
	if (var.a != 4)
		return 1;
	if (var.b != 5)
		return 2;
	if (var.c != 6)
		return 3;
	return 0;
}

LIBTEST_API int STDCALL  
test_Winx64_structs_in1 (struct winx64_struct1 var1,
			 struct winx64_struct2 var2,
			 struct winx64_struct3 var3,
			 struct winx64_struct4 var4)
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
test_Winx64_structs_in2 (struct winx64_struct1 var1,
			 struct winx64_struct1 var2,
			 struct winx64_struct1 var3,
			 struct winx64_struct1 var4,
			 struct winx64_struct1 var5)
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

LIBTEST_API struct winx64_struct1 STDCALL  
test_Winx64_struct1_ret ()
{
	struct winx64_struct1 ret;
	ret.a = 123;
	return ret;
}

LIBTEST_API struct winx64_struct2 STDCALL  
test_Winx64_struct2_ret ()
{
	struct winx64_struct2 ret;
	ret.a = 4;
	ret.b = 5;
	return ret;
}

LIBTEST_API struct winx64_struct3 STDCALL  
test_Winx64_struct3_ret ()
{
	struct winx64_struct3 ret;
	ret.a = 4;
	ret.b = 5;
	ret.c = 0x1234;
	return ret;
}

LIBTEST_API struct winx64_struct4 STDCALL  
test_Winx64_struct4_ret ()
{
	struct winx64_struct4 ret;
	ret.a = 4;
	ret.b = 5;
	ret.c = 0x1234;
	ret.d = 0x87654321;
	return ret;
}

struct winx64_floatStruct
{
	float a;
	float b;
};

LIBTEST_API int STDCALL  
test_Winx64_floatStruct (struct winx64_floatStruct a)
{
	if (a.a > 5.6 || a.a < 5.4)
		return 1;

	if (a.b > 9.6 || a.b < 9.4)
		return 2;
	
	return 0;
}

struct winx64_doubleStruct
{
	double a;
};

LIBTEST_API int STDCALL  
test_Winx64_doubleStruct (struct winx64_doubleStruct a)
{
	if (a.a > 5.6 || a.a < 5.4)
		return 1;
	
	return 0;
}

