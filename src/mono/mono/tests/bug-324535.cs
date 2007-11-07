using System;

class Test
{
    static int Main ()
    {
	bool exc = false;
	try {
	    Generator.fieldOverflow();
	} catch (ArgumentException e) {
	    exc = true;
	    //Console.WriteLine(e.ToString());
	}
	if (!exc) return 1;

	exc = false;
	try {
	    Generator.referenceArray();
	} catch (ArgumentException e) {
	    exc = true;
	    //Console.WriteLine(e.ToString());
	}
	if (!exc) return 1;

	exc = false;
	try {
	    Generator.nonRVAField();
	} catch (ArgumentException e) {
	    exc = true;
	    //Console.WriteLine(e.ToString());
	}
	if (!exc) return 1;

	return 0;
    }
}
