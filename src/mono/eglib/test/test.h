#define test(name,func) do { char *r; printf ("  test: %s: ", name); fflush (stdout);r = func (); if (r){printf ("failure (%s)\n",r); free (r);} else printf ("OK\n");} while (0);


char *test_concat ();
char *test_strfreev ();
char *test_gstring ();
char *test_split ();
char *test_strreverse ();
char *hash_t1 (void);
char *hash_t2 (void);
char *test_slist_append ();
char *test_slist_concat ();
char *test_slist_find ();
char *test_slist_remove ();
char *test_slist_remove_link ();








