#include <stdio.h>

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

	ss.a = 0;
	ss.b = 1;
	ss.c = 0;
	ss.d = "TEST";

	return delegate (ss);
}

