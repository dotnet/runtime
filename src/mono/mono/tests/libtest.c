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

typedef int (STDCALL *SimpleDelegate) (int a);

unsigned short*
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

int mono_union_test_1 (union_test_1_type u1) {
	// printf ("Got values %d %d %d\n", u1.b, u1.a, u1.c);
	return u1.a + u1.b + u1.c;
}

int mono_return_int (int a) {
	// printf ("Got value %d\n", a);
	return a;
}

struct ss
{
	int i;
};

int mono_return_int_ss (struct ss a) {
	// printf ("Got value %d\n", a.i);
	return a.i;
}

struct ss mono_return_ss (struct ss a) {
	// printf ("Got value %d\n", a.i);
	a.i++;
	return a;
}

struct sc1
{
	char c[1];
};

struct sc1 mono_return_sc1 (struct sc1 a) {
	// printf ("Got value %d\n", a.c[0]);
	a.c[0]++;
	return a;
}


struct sc3
{
	char c[3];
};

struct sc3 mono_return_sc3 (struct sc3 a) {
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

struct sc5 mono_return_sc5 (struct sc5 a) {
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

int mono_return_int_su (union su a) {
	// printf ("Got value %d\n", a.i1);
	return a.i1;
}

int mono_test_many_int_arguments (int a, int b, int c, int d, int e,
				  int f, int g, int h, int i, int j);
short mono_test_many_short_arguments (short a, short b, short c, short d, short e,
				      short f, short g, short h, short i, short j);
char mono_test_many_char_arguments (char a, char b, char c, char d, char e,
				    char f, char g, char h, char i, char j);

int
mono_test_many_int_arguments (int a, int b, int c, int d, int e, int f, int g, int h, int i, int j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

short
mono_test_many_short_arguments (short a, short b, short c, short d, short e, short f, short g, short h, short i, short j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

char
mono_test_many_byte_arguments (char a, char b, char c, char d, char e, char f, char g, char h, char i, char j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

float
mono_test_many_float_arguments (float a, float b, float c, float d, float e, float f, float g, float h, float i, float j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

double
mono_test_many_double_arguments (double a, double b, double c, double d, double e, double f, double g, double h, double i, double j)
{
	return a + b + c + d + e + f + g + h + i + j;
}

double
mono_test_split_double_arguments (double a, double b, float c, double d, double e)
{
	return a + b + c + d + e;
}

int
mono_test_puts_static (char *s)
{
	// printf ("TEST %s\n", s);
	return 1;
}

typedef int (STDCALL *SimpleDelegate3) (int a, int b);

int
mono_invoke_delegate (SimpleDelegate3 delegate)
{
	int res;

	// printf ("start invoke %p\n", delegate);

	res = delegate (2, 3);

	// printf ("end invoke\n");

	return res;
}

int 
mono_test_marshal_char (short a1)
{
	if (a1 == 'a')
		return 0;
	
	return 1;
}

void
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

int
mono_test_empty_pinvoke (int i)
{
	return i;
}

int 
mono_test_marshal_bool_byref (int a, int *b, int c)
{
    int res = *b;

	*b = 1;

	return res;
}

int 
mono_test_marshal_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++)
		sum += a1 [i];
	
	return sum;
}

int 
mono_test_marshal_inout_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++) {
		sum += a1 [i];
		a1 [i] = 50 - a1 [i];
	}
	
	return sum;
}

int 
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
} simplestruct;

simplestruct
mono_test_return_vtype (int i)
{
	simplestruct res;

	res.a = 0;
	res.b = 1;
	res.c = 0;
	res.d = "TEST";

	return res;
}

void
mono_test_delegate_struct (void)
{
	// printf ("TEST\n");
}

typedef char* (STDCALL *ReturnStringDelegate) (const char *s);

char *
mono_test_return_string (ReturnStringDelegate func)
{
	char *res;

	// printf ("mono_test_return_string\n");

	res = func ("TEST");

	// printf ("got string: %s\n", res);
	return res;
}

typedef int (STDCALL *RefVTypeDelegate) (int a, simplestruct *ss, int b);

int
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

int
mono_test_marshal_out_struct (int a, simplestruct *ss, int b, OutVTypeDelegate func)
{
	int res;

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

int 
mono_test_marshal_delegate_struct (DelegateStruct ds)
{
	return ds.func (ds.a) + ds.func2 (ds.a);
}

int 
mono_test_marshal_struct (simplestruct ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST"))
		return 0;

	return 1;
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

int
mono_test_marshal_struct2 (simplestruct2 ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST") && 
	    ss.e == 99 && ss.f == 1.5 && ss.g == 42 && ss.h == (guint64)123)
		return 0;

	return 1;
}

/* on HP some of the struct should be on the stack and not in registers */
int
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

int
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

int
mono_test_marshal_long_align_struct_array (long_align_struct *ss)
{
	return ss[0].a + ss[0].b + ss[0].c + ss[1].a + ss[1].b + ss[1].c;
}

simplestruct2 *
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

int
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

int
mono_test_marshal_delegate (SimpleDelegate delegate)
{
	void *sp1, *sp2;

	/* Check that the delegate wrapper is stdcall */
	delegate (2);
	sp1 = get_sp ();
	delegate (2);
	sp2 = get_sp ();
	g_assert (sp1 == sp2);

	return delegate (2);
}

typedef simplestruct (STDCALL *SimpleDelegate2) (simplestruct ss);

int
mono_test_marshal_delegate2 (SimpleDelegate2 delegate)
{
	simplestruct ss, res;

	ss.a = 0;
	ss.b = 1;
	ss.c = 0;
	ss.d = "TEST";

	res = delegate (ss);
	if (! (res.a && !res.b && res.c && !strcmp (res.d, "TEST-RES")))
		return 1;

	return 0;
}

typedef simplestruct* (STDCALL *SimpleDelegate4) (simplestruct *ss);

int
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

int
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

int
mono_test_marshal_delegate6 (SimpleDelegate5 delegate)
{
	int res;

	res = delegate (NULL);

	return 0;
}

typedef int (STDCALL *SimpleDelegate7) (simplestruct **ss);

int
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

int
mono_test_marshal_delegate8 (SimpleDelegate8 delegate, gunichar2 *s)
{
	return delegate (s);
}

typedef int (STDCALL *return_int_fnt) (int i);
typedef int (STDCALL *SimpleDelegate9) (return_int_fnt *d);

int
mono_test_marshal_delegate9 (SimpleDelegate9 delegate, gpointer ftn)
{
	return delegate (ftn);
}

int
return_self (int i)
{
	return i;
}

int
mono_test_marshal_delegate10 (SimpleDelegate9 delegate)
{
	return delegate (return_self);
}

typedef int (STDCALL *PrimitiveByrefDelegate) (int *i);

int
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

typedef return_int_delegate (STDCALL *ReturnDelegateDelegate) ();

int
mono_test_marshal_return_delegate_delegate (ReturnDelegateDelegate d)
{
	return (d ()) (55);
}


int 
mono_test_marshal_stringbuilder (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";

	if (strcmp (s, "ABCD") != 0)
		return 1;
	strncpy(s, m, n);
	return 0;
}

int 
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

int
mono_test_marshal_empty_string_array (char **array)
{
	return (array == NULL) ? 0 : 1;
}

int
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

int
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

int
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

int
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
int 
mono_test_empty_struct (int a, EmptyStruct es, int b)
{
	// printf ("mono_test_empty_struct %d %d\n", a, b);

	if (a == 1 && b == 2)
		return 0;
	return 1;
}

typedef struct {
       char a[100];
} ByValStrStruct;

ByValStrStruct *
mono_test_byvalstr_gen (void)
{
	ByValStrStruct *ret;
       
	ret = malloc(sizeof(ByValStrStruct));
	memset(ret, 'a', sizeof(ByValStrStruct)-1);
	ret->a[sizeof(ByValStrStruct)-1] = 0;

	return ret;
}

int
mono_test_byvalstr_check (ByValStrStruct* data, char* correctString)
{
	int ret;

	ret = strcmp(data->a, correctString);
	// printf ("T1: %s\n", data->a);
	// printf ("T2: %s\n", correctString);

	g_free(data);
	return (ret != 0);
}

int 
HexDump(char *data)
{
	int i, res = 0;
	char *p;

	printf ("HEXDUMP DEFAULT VERSION\n");

	p = data;
	for (i=0; i < 8; ++i)
	{
		res += *p;
		printf("%0x ", (int) *(p++));
	}
	putchar('\n');

	return res;
}

int 
HexDumpA(char *data)
{
	int i, res = 0;
	char *p;

	printf ("HEXDUMP ANSI VERSION\n");

	p = data;
	for (i=0; i < 8; ++i)
	{
		res += *p;
		printf("%0x ", (int) *(p++));
	}
	putchar('\n');

	return res + 100000;
}

int 
HexDump1W(char *data)
{
	int i, res = 0;
	char *p;

	printf ("HEXDUMP UNICODE VERSION\n");

	p = data;
	for (i=0; i < 8; ++i)
	{
		res += *p;
		printf("%0x ", (int) *(p++));
	}
	putchar('\n');

	return res + 1000000;
}

typedef int (STDCALL *intcharFunc)(const char*);

void 
callFunction (intcharFunc f)
{
	f ("ABC");
}

typedef struct {
        const char* str;
        int i;
} SimpleObj;

int
class_marshal_test0 (SimpleObj *obj1)
{
	// printf ("class_marshal_test0 %s %d\n", obj1->str, obj1->i);

	if (strcmp(obj1->str, "T1"))
		return -1;
	if (obj1->i != 4)
		return -2;

	return 0;
}

int
class_marshal_test4 (SimpleObj *obj1)
{
	if (obj1)
		return -1;

	return 0;
}

void
class_marshal_test1 (SimpleObj **obj1)
{
	SimpleObj *res = malloc (sizeof (SimpleObj));

	res->str = g_strdup ("ABC");
	res->i = 5;

	*obj1 = res;
}

int
class_marshal_test2 (SimpleObj **obj1)
{
	// printf ("class_marshal_test2 %s %d\n", (*obj1)->str, (*obj1)->i);

	if (strcmp((*obj1)->str, "ABC"))
		return -1;
	if ((*obj1)->i != 5)
		return -2;

	return 0;
}

int
string_marshal_test0 (char *str)
{
	if (strcmp (str, "TEST0"))
		return -1;

	return 0;
}

void
string_marshal_test1 (const char **str)
{
	*str = "TEST1";
}

int
string_marshal_test2 (char **str)
{
	// printf ("string_marshal_test2 %s\n", *str);

	if (strcmp (*str, "TEST1"))
		return -1;

	return 0;
}

int
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


VectorList* TestVectorList (VectorList *vl)
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

int 
GetVersionEx (OSVERSIONINFO *osvi)
{

	// printf ("GOT %d %d\n", osvi->a, osvi->b);

	osvi->a += 1;
	osvi->b += 1;

	return osvi->a + osvi->b;
}

int 
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

int
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

int
mono_test_marshal_mixed_point (mixed_point pt)
{
	// printf("mixed point %d %g\n", pt.x, pt.y);
	if (pt.x == 5 && pt.y == 6.75)
		return 0;

	return 1;
}

int
mono_test_marshal_mixed_point_2 (mixed_point *pt)
{
	if (pt->x != 5 || pt->y != 6.75)
		return 1;

	pt->x = 10;
	pt->y = 12.35;

	return 0;
}

int 
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

int 
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

void
mono_test_last_error (int err)
{
#ifdef WIN32
	SetLastError (err);
#else
	errno = err;
#endif
}

int
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

amd64_struct1
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

amd64_struct2
mono_test_marshal_amd64_pass_return_struct2 (amd64_struct2 s)
{
	s.i ++;
	s.j ++;

	return s;
}

typedef struct amd64_struct3 {
	int i;
} amd64_struct3;

amd64_struct3
mono_test_marshal_amd64_pass_return_struct3 (amd64_struct3 s)
{
	s.i ++;

	return s;
}

typedef struct amd64_struct4 {
	double d1, d2;
} amd64_struct4;

amd64_struct4
mono_test_marshal_amd64_pass_return_struct4 (amd64_struct4 s)
{
	s.d1 ++;
	s.d2 ++;

	return s;
}

static guint32 custom_res [2];

void*
mono_test_marshal_pass_return_custom (int i, guint32 *ptr, int j)
{
	/* ptr will be freed by CleanupNative, so make a copy */
	custom_res [0] = 0; /* not allocated by AllocHGlobal */
	custom_res [1] = ptr [1];

	return &custom_res;
}

typedef void *(STDCALL *PassReturnPtrDelegate) (void *ptr);

int
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


