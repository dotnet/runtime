#include <stdio.h>
#include <string.h>

unsigned short*
test_lpwstr_marshal (unsigned short* chars, long length)
{
	int i = 0;
	unsigned short *res;

	res = malloc (2 * (length + 1));

	printf("test_lpwstr_marshal()\n");
	
	while ( i < length ) {
		printf("X|%u|\n", chars[i]);
		res [i] = chars[i++];
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
	printf ("Got values %d %d %d\n", u1.b, u1.a, u1.c);
	return u1.a + u1.b + u1.c;
}

int mono_return_int (int a) {
	printf ("Got value %d\n", a);
	return a;
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

int
mono_test_puts_static (char *s)
{
	printf ("TEST %s\n", s);
	return 1;
}

typedef int (*SimpleDelegate3) (int a, int b);

int
mono_invoke_delegate (SimpleDelegate3 delegate)
{
	int res;

	printf ("start invoke %p\n", delegate);

	res = delegate (2, 3);

	printf ("end invoke\n");

	return res;
}

int 
mono_test_marshal_char (short a1)
{
	if (a1 = 'a')
		return 0;
	
	return 1;
}

int 
mono_test_marshal_array (int *a1)
{
	int i, sum = 0;

	for (i = 0; i < 50; i++)
		sum += a1 [i];
	
	return sum;
}

typedef struct {
	int a;
	int b;
	int c;
	char *d;
} simplestruct;

simplestruct
mono_test_return_vtype ()
{
	simplestruct res;

	res.a = 0;
	res.b = 1;
	res.c = 0;
	res.d = "TEST";
	printf ("mono_test_return_vtype\n");
	return res;
}

void
mono_test_delegate_struct ()
{
	printf ("TEST\n");
}

typedef simplestruct (*ReturnVTypeDelegate) (simplestruct ss);

simplestruct
mono_test_return_vtype2 (ReturnVTypeDelegate func)
{
	simplestruct res;
	simplestruct res1;

	res.a = 1;
	res.b = 0;
	res.c = 1;
	res.d = "TEST";
	printf ("mono_test_return_vtype2\n");

	res1 = func (res);

	printf ("UA: %d\n", res1.a);
	printf ("UB: %d\n", res1.b);
	printf ("UC: %d\n", res1.c);
	printf ("UD: %s\n", res1.d);

	return res1;
}

typedef char* (*ReturnStringDelegate) (char *s);

char *
mono_test_return_string (ReturnStringDelegate func)
{
	char *res;

	printf ("mono_test_return_string\n");

	res = func ("TEST");

	printf ("got string: %s\n", res);
	return res;
}

typedef int (*RefVTypeDelegate) (int a, simplestruct *ss, int b);

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

typedef struct {
	int a;
	int (*func) (int);
} DelegateStruct;

int 
mono_test_marshal_delegate_struct (DelegateStruct ds)
{
	return ds.func (ds.a);
}

int 
mono_test_marshal_struct (simplestruct ss)
{
	if (ss.a == 0 && ss.b == 1 && ss.c == 0 &&
	    !strcmp (ss.d, "TEST"))
		return 0;

	return 1;
}


typedef int (*SimpleDelegate) (int a);

int
mono_test_marshal_delegate (SimpleDelegate delegate)
{
	return delegate (2);
}

typedef int (*SimpleDelegate2) (simplestruct ss);

int
mono_test_marshal_delegate2 (SimpleDelegate2 delegate)
{
	simplestruct ss;
	int res;

	ss.a = 0;
	ss.b = 1;
	ss.c = 0;
	ss.d = "TEST";

	printf ("Calling delegate from unmanaged code\n");
	res = delegate (ss);
	printf ("GOT %d\n", res);

	return res;
}

int 
mono_test_marshal_stringbuilder (char *s, int n)
{
	const char m[] = "This is my message.  Isn't it nice?";
	strncpy(s, m, n);
	return 0;
}

typedef struct {
} EmptyStruct;

int
mono_test_marshal_string_array (char **array)
{
	printf ("%p\n", array);
	return 0;
}

/* this does not work on Redhat gcc 2.96 */
int 
mono_test_empty_struct (int a, EmptyStruct es, int b)
{
	printf ("mono_test_empty_struct %d %d\n", a, b);

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
	int i;
       
	ret = g_malloc(sizeof(ByValStrStruct));
	memset(ret, 'a', sizeof(ByValStrStruct)-1);
	ret->a[sizeof(ByValStrStruct)-1] = 0;

	return ret;
}

int
mono_test_byvalstr_check (ByValStrStruct* data, char* correctString)
{
	int ret;

	ret = strcmp(data->a, correctString);
	printf ("T1: %s\n", data->a);
	printf ("T2: %s\n", correctString);

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

typedef int (*intcharFunc)(char*);

void 
callFunction (intcharFunc f)
{
	f ("ABC");
}

int
printInt (int* number)
{
	printf( "<%d>\n", *number );
	return *number + 1;
}


typedef struct {
        char* str;
        int i;
} SimpleObj;

int
class_marshal_test0 (SimpleObj *obj1)
{
	printf ("class_marshal_test0 %s %d\n", obj1->str, obj1->i);

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

	res->str = "ABC";
	res->i = 5;

	*obj1 = res;
}

int
class_marshal_test2 (SimpleObj **obj1)
{
	printf ("class_marshal_test2 %s %d\n", (*obj1)->str, (*obj1)->i);

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
string_marshal_test1 (char **str)
{
	*str = "TEST1";
}

int
string_marshal_test2 (char **str)
{
	printf ("string_marshal_test2 %s\n", *str);

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

char *
functionReturningString (void)
{
    return "ABC";
}

