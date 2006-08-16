#define test(name,func) do { char *r; printf ("  test: %s: ", name); fflush (stdout);r = func (); if (r)printf ("failure (%s)\n",r); else printf ("OK\n");} while (0);
