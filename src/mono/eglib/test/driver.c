#include <stdio.h>

#include "test.h"

int main ()
{
	printf ("hashtable\n");
	test ("hash-1", hash_t1);
	test ("s-freev", test_strfreev);
	test ("s-concat", test_concat);
	test ("s-split", test_split);
	test ("s-gstring", test_gstring);
	test ("s-gstrreverse", test_strreverse);
}
