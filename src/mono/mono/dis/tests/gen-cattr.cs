/* Custom attributes for type parameters */
using System;

[AttributeUsage(AttributeTargets.GenericParameter)]
class GenParAttribute : Attribute {
}

class cons <[GenPar] A> {
	public void abc <[GenPar] M> () {
	}
}

class Test {
	public static void Main ()
	{
	}
}
