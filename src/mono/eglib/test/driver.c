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
	test ("s-slist-append", test_slist_append);
	test ("s-slist-concat", test_slist_concat);
	test ("s-slist-find", test_slist_find);
	test ("s-slist-remove", test_slist_remove);
	test ("s-slist-remove-link", test_slist_remove_link);
	test ("s-slist-insert-sorted", test_slist_insert_sorted);
}
