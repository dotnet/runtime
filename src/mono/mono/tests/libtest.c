#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <glib.h>
#include <errno.h>
#include <time.h>

#ifdef WIN32
#define STDCALL __stdcall
#else
#define STDCALL
#endif

#ifdef WIN32
extern __declspec(dllimport) __stdcall void CoTaskMemFree(void *ptr);
#endif

typedef int (STDCALL *SimpleDelegate) (int a);

static void marshal_free (void *ptr)
{
#ifdef WIN32
	CoTaskMemFree (ptr);
#else
	g_free (ptr);
#endif
}

STDCALL unsigned short*
test_lpwstr_marshal (unsigned short* chars, long length)
{
	int i = 0;
	unsigned short *res;

	res = malloc (2 * (length + 1));

	// printf("test_lpwstr_marshal()\n");
	
	while ( i < length ) {
		// printf("X|%u|\n", chars[i]);
		res [i] = chars[i];
		i++;
	}

	res [i] = 0;

	return res;
}

typedef struct {
	int b;
	int a;
	int c;
} union_test_1_type;

STDCALL int 
mono_union_test_1 (union_test_1_type u1) {
	// printf ("Got values %d %d %d\n", u1.b, u1.a, u1.c);
	return u1.a + u1.b + u1.c;
}

STDCALL int 
mono_return_int (int a) {
	// printf ("Got value %d\n", a);
	return a;
}

struct ss
{
	int i;
};

STDCALL int 
mono_return_int_ss (struct ss a) {
	// printf ("Got value %d\n", a.i);
	return a.i;
}

STDCALL struct ss 
mono_return_ss (struct ss a) {
	// printf ("Got value %d\n", a.i);
	a.i++;
	return a;
}

struct sc1
{
	char c[1];
};

STDCALL struct sc1 
mono_return_sc1 (struct sc1 a) {
	// printf ("Got value %d\n", a.c[0]);
	a.c[0]++;
	return a;
}


struct sc3
{
	char c[3];
};

STDCALL struct sc3 
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

STDCALL struct sc5 
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

STDCALL int 
mono_return_int_su (union su a) {
	// printf ("Got value %d\n", a.i1);
	return a.i1;
}

STDCALL int 
mono_test_many_int_arguments (int a, int b, int c, int d, int e,
							  int f, int g, int h, int i, int j);
STDCALL short 
mono_test_many_short_arguments (short a, short b, short c, short d, short e,
								short f, short g, short h, short i, short j);
STDCALL char 
mono_test_many_char_arguments (char a, char b, char c, char d, char e,
							   char f, char g, char h, char i, char j);

STDCALL int
mono_test_many_int_arguments (int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

STDCALL short
mono_test_many_short_arguments (short a, short b, short c, short d, short e, short f, short g, short h, short i, short j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

STDCALL char
mono_test_many_byte_arguments (char a, char b, char c, char d, char e, char f, char g, char h, char i, char j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

STDCALL float
mono_test_many_float_arguments (float a, float b, float c, float d, float e, float f, float g, float h, float i, float j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

STDCALL double
mono_test_many_double_arguments (double a, double b, double c, double d, double e, double f, double g, double h, double i, double j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

STDCALL double
mono_test_split_double_arguments (double a, double b, float c, double d, double e)
{
	return a + b + c + d + e;
}

STDCALL int
mono_test_puts_static (char *s)
{
	// printf ("TEST %s\n", s);
	return 1;
}

typedef int (STDCALL *SimpleDelegate3) (int a, int b);

STDCALL int
mono_invoke_delegate (SimpleDelegate3 delegate)
{
	int res;

	// printf ("start invoke %p\n", delegate);

	res = delegate (2, 3);

	// printf ("end invoke\n");

	return res;
}

STDCALL int 
mono_test_marshal_char (short a1)
{
	if (a1 == 'a')
		return 0;
	
	return 1;
}

STDCALL void
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

STDCALL int
mono_test_empty_pinvoke (int i)
{
	return i;
}

STDCALL int 
mono_test_marshal_bool_byref (int a, int *b, int c)
{
    int res = *b;

	*b = 1;

	return res;
}

STDCALL int 
mono_test_marshal_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++)
		sum += a1 [i];
	
	return sum;
}

STDCALL int 
mono_test_marshal_inout_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++) {
		sum += a1 [i];
		a1 [i] = 50 - a1 [i];
	}
	
	return sum;
}

STDCALL int 
mono_test_marshal_out_array (int *a1)
{
	int i;

	for (i = 0; i < 50; i++) {
		a1 [i] = i;
	}
	
	return 0;
}

STDCALL int 
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

STDCALL simplestruct
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

STDCALL void
mono_test_delegate_struct (void)
{
	// printf ("TEST\n");
}

typedef char* (STDCALL *ReturnStringDelegate) (const char *s);

STDCALL char *
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

STDCALL int
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

STDCALL int
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

STDCALL DelegateStruct
mono_test_marshal_delegate_struct (DelegateStruct ds)
{
	DelegateStruct res;

	res.a = ds.func (ds.a) + ds.func2 (ds.a);
	res.func = ds.func;
	res.func2 = ds.func2;

	return res;
}

STDCALL int 
mono_test_marshal_struct (simplestruct ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST"))
		return 0;

	return 1;
}

STDCALL int
mono_test_marshal_byref_struct (simplestruct *ss, int a, int b, int c, char *d)
{
	gboolean res = (ss->a == a && ss->b == b && ss->c == c && strcmp (ss->d, d) == 0);

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

STDCALL int
mono_test_marshal_struct2 (simplestruct2 ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST") && 
	    ss.e == 99 && ss.f == 1.5 && ss.g == 42 && ss.h == (guint64)123)
		return 0;

	return 1;
}

/* on HP some of the struct should be on the stack and not in registers */
STDCALL int
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

STDCALL int
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

STDCALL int
mono_test_marshal_long_align_struct_array (long_align_struct *ss)
{
	return ss[0].a + ss[0].b + ss[0].c + ss[1].a + ss[1].b + ss[1].c;
}

STDCALL simplestruct2 *
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

STDCALL int
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

	p = &i;
	return p;
}

STDCALL int
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

STDCALL int
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

STDCALL SimpleDelegate
mono_test_marshal_return_delegate (SimpleDelegate delegate)
{
	return delegate;
}

static STDCALL int
return_plus_one (int i)
{
	return i + 1;
}

STDCALL SimpleDelegate
mono_test_marshal_return_delegate_2 ()
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
mono_test_marshal_delegate6 (SimpleDelegate5 delegate)
{
	int res;

	res = delegate (NULL);

	return 0;
}

typedef int (STDCALL *SimpleDelegate7) (simplestruct **ss);

STDCALL int
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

typedef int (STDCALL *SimpleDelegate8) (gunichar2 *s);

STDCALL int
mono_test_marshal_delegate8 (SimpleDelegate8 delegate, gunichar2 *s)
{
	return delegate (s);
}

typedef int (STDCALL *return_int_fnt) (int i);
typedef int (STDCALL *SimpleDelegate9) (return_int_fnt d);

STDCALL int
mono_test_marshal_delegate9 (SimpleDelegate9 delegate, gpointer ftn)
{
	return delegate (ftn);
}

STDCALL static int
return_self (int i)
{
	return i;
}

STDCALL int
mono_test_marshal_delegate10 (SimpleDelegate9 delegate)
{
	return delegate (return_self);
}

typedef int (STDCALL *PrimitiveByrefDelegate) (int *i);

STDCALL int
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

STDCALL int
mono_test_marshal_return_delegate_delegate (ReturnDelegateDelegate d)
{
	return (d ()) (55);
}

STDCALL int 
mono_test_marshal_stringbuilder (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";

	if (strcmp (s, "ABCD") != 0)
		return 1;
	strncpy(s, m, n);
	return 0;
}

STDCALL int 
mono_test_marshal_stringbuilder_unicode (gunichar2 *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";
	gunichar2* s2;
	glong len;

	s2 = g_utf8_to_utf16 (m, -1, NULL, &len, NULL);
	
	len = (len * 2) + 2;
	if (len > n)
		len = n;
	memcpy (s, s2, len);

	g_free (s2);

	return 0;
}

typedef struct {
#ifndef __GNUC__
    char a;
#endif
} EmptyStruct;

STDCALL int
mono_test_marshal_empty_string_array (char **array)
{
	return (array == NULL) ? 0 : 1;
}

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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
STDCALL int 
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

STDCALL ByValStrStruct *
mono_test_byvalstr_gen (void)
{
	ByValStrStruct *ret;
       
	ret = malloc(sizeof(ByValStrStruct));
	memset(ret, 'a', sizeof(ByValStrStruct)-1);
	ret->a[sizeof(ByValStrStruct)-1] = 0;

	return ret;
}

STDCALL int
mono_test_byvalstr_check (ByValStrStruct* data, char* correctString)
{
	int ret;

	ret = strcmp(data->a, correctString);
	// printf ("T1: %s\n", data->a);
	// printf ("T2: %s\n", correctString);

	marshal_free (data);
	return (ret != 0);
}

STDCALL int
NameManglingAnsi (char *data)
{
	return data [0] + data [1] + data [2];
}

STDCALL int
NameManglingAnsiA (char *data)
{
	g_assert_not_reached ();
}

STDCALL int
NameManglingAnsiW (char *data)
{
	g_assert_not_reached ();
}

STDCALL int
NameManglingAnsi2A (char *data)
{
	return data [0] + data [1] + data [2];
}

STDCALL int
NameManglingAnsi2W (char *data)
{
	g_assert_not_reached ();
}

STDCALL int
NameManglingUnicode (char *data)
{
	g_assert_not_reached ();
}

STDCALL int
NameManglingUnicodeW (gunichar2 *data)
{
	return data [0] + data [1] + data [2];
}

STDCALL int
NameManglingUnicode2 (gunichar2 *data)
{
	return data [0] + data [1] + data [2];
}

STDCALL int
NameManglingAutoW (char *data)
{
#ifdef WIN32
	return (data [0] + data [1] + data [2]) == 131 ? 0 : 1;
#else
	g_assert_not_reached ();
#endif
}

STDCALL int
NameManglingAuto (char *data)
{
#ifndef WIN32
	return (data [0] + data [1] + data [2]) == 198 ? 0 : 1;
#else
	g_assert_not_reached ();
#endif
}

typedef int (STDCALL *intcharFunc)(const char*);

STDCALL void 
callFunction (intcharFunc f)
{
	f ("ABC");
}

typedef struct {
        const char* str;
        int i;
} SimpleObj;

STDCALL int
class_marshal_test0 (SimpleObj *obj1)
{
	// printf ("class_marshal_test0 %s %d\n", obj1->str, obj1->i);

	if (strcmp(obj1->str, "T1"))
		return -1;
	if (obj1->i != 4)
		return -2;

	return 0;
}

STDCALL int
class_marshal_test4 (SimpleObj *obj1)
{
	if (obj1)
		return -1;

	return 0;
}

STDCALL void
class_marshal_test1 (SimpleObj **obj1)
{
	SimpleObj *res = malloc (sizeof (SimpleObj));

	res->str = g_strdup ("ABC");
	res->i = 5;

	*obj1 = res;
}

STDCALL int
class_marshal_test2 (SimpleObj **obj1)
{
	// printf ("class_marshal_test2 %s %d\n", (*obj1)->str, (*obj1)->i);

	if (strcmp((*obj1)->str, "ABC"))
		return -1;
	if ((*obj1)->i != 5)
		return -2;

	return 0;
}

STDCALL int
string_marshal_test0 (char *str)
{
	if (strcmp (str, "TEST0"))
		return -1;

	return 0;
}

STDCALL void
string_marshal_test1 (const char **str)
{
	*str = "TEST1";
}

STDCALL int
string_marshal_test2 (char **str)
{
	// printf ("string_marshal_test2 %s\n", *str);

	if (strcmp (*str, "TEST1"))
		return -1;

	return 0;
}

STDCALL int
string_marshal_test3 (char *str)
{
	if (str)
		return -1;

	return 0;
}

typedef struct {
	int a;
	int b;
} VectorList;

STDCALL VectorList* 
TestVectorList (VectorList *vl)
{
	VectorList *res;

	// printf ("TestVectorList %d %d\n", vl->a, vl->b);

	vl->a++;
	vl->b++;

	res = g_new0 (VectorList, 1);
	memcpy (res, vl, sizeof (VectorList));

	return res;
}

typedef struct _OSVERSIONINFO
{ 
	int a; 
	int b; 
} OSVERSIONINFO; 

STDCALL int 
GetVersionEx (OSVERSIONINFO *osvi)
{

	// printf ("GOT %d %d\n", osvi->a, osvi->b);

	osvi->a += 1;
	osvi->b += 1;

	return osvi->a + osvi->b;
}

STDCALL int 
BugGetVersionEx (int a, int b, int c, int d, int e, int f, int g, int h, OSVERSIONINFO *osvi)
{

	// printf ("GOT %d %d\n", osvi->a, osvi->b);

	osvi->a += 1;
	osvi->b += 1;

	return osvi->a + osvi->b;
}

typedef struct {
	double x;
	double y;
} point;

STDCALL int
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

STDCALL int
mono_test_marshal_mixed_point (mixed_point pt)
{
	// printf("mixed point %d %g\n", pt.x, pt.y);
	if (pt.x == 5 && pt.y == 6.75)
		return 0;

	return 1;
}

STDCALL int
mono_test_marshal_mixed_point_2 (mixed_point *pt)
{
	if (pt->x != 5 || pt->y != 6.75)
		return 1;

	pt->x = 10;
	pt->y = 12.35;

	return 0;
}

STDCALL int 
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

STDCALL int 
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

#ifdef WIN32
extern __declspec(dllimport) __stdcall void SetLastError(int x);
#endif

STDCALL void
mono_test_last_error (int err)
{
#ifdef WIN32
	SetLastError (err);
#else
	errno = err;
#endif
}

STDCALL int
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

/*
 * AMD64 marshalling tests.
 */

typedef struct amd64_struct1 {
	int i;
	int j;
	int k;
	int l;
} amd64_struct1;

STDCALL amd64_struct1
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

STDCALL amd64_struct2
mono_test_marshal_amd64_pass_return_struct2 (amd64_struct2 s)
{
	s.i ++;
	s.j ++;

	return s;
}

typedef struct amd64_struct3 {
	int i;
} amd64_struct3;

STDCALL amd64_struct3
mono_test_marshal_amd64_pass_return_struct3 (amd64_struct3 s)
{
	s.i ++;

	return s;
}

typedef struct amd64_struct4 {
	double d1, d2;
} amd64_struct4;

STDCALL amd64_struct4
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

STDCALL test_struct5
mono_test_marshal_ia64_pass_return_struct5 (double d1, double d2, test_struct5 s, double d3, double d4)
{
	s.d1 += d1 + d2;
	s.d2 += d3 + d4;

	return s;
}

typedef struct test_struct6 {
	double d1, d2;
} test_struct6;

STDCALL test_struct6
mono_test_marshal_ia64_pass_return_struct6 (double d1, double d2, test_struct6 s, double d3, double d4)
{
	s.d1 += d1 + d2;
	s.d2 += d3 + d4;

	return s;
}

static guint32 custom_res [2];

STDCALL void*
mono_test_marshal_pass_return_custom (int i, guint32 *ptr, int j)
{
	/* ptr will be freed by CleanupNative, so make a copy */
	custom_res [0] = 0; /* not allocated by AllocHGlobal */
	custom_res [1] = ptr [1];

	return &custom_res;
}

STDCALL int
mono_test_marshal_pass_out_custom (int i, guint32 **ptr, int j)
{
	custom_res [0] = 0;
	custom_res [1] = i + j + 10;

	*ptr = custom_res;

	return 0;
}

STDCALL int
mono_test_marshal_pass_byref_custom (int i, guint32 **ptr, int j)
{
	(*ptr)[1] += i + j;

	return 0;
}

STDCALL void*
mono_test_marshal_pass_return_custom2 (int i, guint32 *ptr, int j)
{
	g_assert_not_reached ();

	return NULL;
}

STDCALL void*
mono_test_marshal_pass_return_custom_null (int i, guint32 *ptr, int j)
{
	g_assert (ptr == NULL);

	return NULL;
}

typedef void *(STDCALL *PassReturnPtrDelegate) (void *ptr);

STDCALL int
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

STDCALL int
mono_test_marshal_pass_return_custom_null_in_delegate (PassReturnPtrDelegate del)
{
	void *ptr = del (NULL);

	return (ptr == NULL) ? 15 : 0;
}

typedef int (STDCALL *ReturnEnumDelegate) (int e);

STDCALL int
mono_test_marshal_return_enum_delegate (ReturnEnumDelegate func)
{
	return func (1);
}

typedef struct {
	int a, b, c;
	gint64 d;
} BlittableStruct;
	
typedef BlittableStruct (STDCALL *SimpleDelegate10) (BlittableStruct ss);

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
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

STDCALL int
mono_test_marshal_array_delegate (void *arr, int len, ArrayDelegate del)
{
	return del (len, NULL, arr);
}

STDCALL int
mono_test_marshal_out_array_delegate (int *arr, int len, ArrayDelegate del)
{
	del (len, NULL, arr);

	if ((arr [0] != 1) || (arr [1] != 2))
		return 1;
	else
		return 0;
}

STDCALL int
mono_test_marshal_out_string_array_delegate (char **arr, int len, ArrayDelegate del)
{
	del (len, NULL, arr);

	if (!strcmp (arr [0], "ABC") && !strcmp (arr [1], "DEF"))
		return 0;
	else
		return 1;
}

typedef int (*CdeclDelegate) (int i, int j);

STDCALL int
mono_test_marshal_cdecl_delegate (CdeclDelegate del)
{
	int i;

	for (i = 0; i < 1000; ++i)
		del (1, 2);

	return 0;
}

typedef char** (*ReturnStringArrayDelegate) (int i);

STDCALL int
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
