using System;

class Class1
{
	static int AddABunchOfInts (__arglist)
	{
		int result = 0;

		System.ArgIterator iter = new System.ArgIterator (__arglist);
		int argCount = iter.GetRemainingCount();

		for (int i = 0; i < argCount; i++) {
			System.TypedReference typedRef = iter.GetNextArg();
			result += (int)TypedReference.ToObject( typedRef );
		}
		
		return result;
	}

	static int Main (string[] args)
	{
		int result = AddABunchOfInts ( __arglist ( 2, 3, 4 ));
		Console.WriteLine ("Answer: {0}", result);

		if (result != 9)
			return 1;

		return 0;
	}
}
