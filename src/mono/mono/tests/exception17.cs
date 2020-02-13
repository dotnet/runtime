using System;

public class main {
    public static Exception exc;

    public static void finaller () {
	try {
	    throw exc;
	} finally {
	    throw exc;
	}
    }

    public static void catcher1 () {
	try {
	    finaller ();
	} catch (Exception) {
	}
    }

    public static void catcher2 () {
	try {
	    try {
		throw exc;
	    } finally {
		catcher1 ();
		throw exc;
	    }
	} catch (Exception) {
	}
    }

    public static int Main () {
	exc = new Exception ();

	catcher1 ();
	catcher2 ();

	return 0;
    }
}
