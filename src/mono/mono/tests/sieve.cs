/* -*- mode: c -*-
 * $Id$
 * http://www.bagley.org/~doug/shootout/
 */

class Test {
static public int Main() {
    //int NUM = ((argc == 2) ? atoi(argv[1]) : 1);
    int NUM = 300;
    byte[] flags = new byte[8192 + 1];
    int i, k;
    int count = 0;

    while (NUM-- != 0) {
	count = 0; 
	for (i=2; i <= 8192; i++) {
	    flags[i] = 1;
	}
	for (i=2; i <= 8192; i++) {
	    if (flags[i] != 0) {
		// remove all multiples of prime: i
		for (k=i+i; k <= 8192; k+=i) {
		    flags[k] = 0;
		}
		count++;
	    }
	}
    }
    System.Console.WriteLine("Count: {0}\n", count);
    return(0);
}
}
