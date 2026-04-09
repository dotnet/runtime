using System;

public class main {
    static int AddABunchOfShorts (__arglist)
    {
	int result = 0;

	System.ArgIterator iter = new System.ArgIterator (__arglist);
	int argCount = iter.GetRemainingCount();

	for (int i = 0; i < argCount; i++) {
	    System.TypedReference typedRef = iter.GetNextArg();
	    result += (short)TypedReference.ToObject( typedRef );
	}

	return result;
    }

    static int AddABunchOfBytes (__arglist)
    {
	int result = 0;

	System.ArgIterator iter = new System.ArgIterator (__arglist);
	int argCount = iter.GetRemainingCount();

	for (int i = 0; i < argCount; i++) {
	    System.TypedReference typedRef = iter.GetNextArg();
	    result += (byte)TypedReference.ToObject( typedRef );
	}

	return result;
    }

    public static int Main () {
	if (AddABunchOfShorts (__arglist ((short)1, (short)2, (short)3)) != 6)
	    return 1;

	if (AddABunchOfBytes (__arglist ((byte)4, (byte)5, (byte)6)) != 15)
	    return 2;

	return 0;
    }
}
