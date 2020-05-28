using System;
using System.Collections.Generic;

interface IFoo {}
class Foo : IFoo {}

class Driver
{
	static IEnumerable <Foo> Dele (bool b)
	{
		return null;
	}

	static void Main ()
	{
		Func<bool, IEnumerable<IFoo>> dele = Dele;
		dele (true);
	}
}