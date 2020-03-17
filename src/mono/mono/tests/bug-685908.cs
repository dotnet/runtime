
using System;
using System.Linq;
using System.Linq.Expressions;

class Program
{
	static int Main ()
	{
		if (Test<S> () == 5)
			return 0;
		return 1;
	}

	static int Test<T> () where T : I
	{
		Expression<Func<T, int>> e = l => l.SetValue () + l.Value;
		var arg = default (T);
		return (int) (e.Compile () (arg));
	}
}

interface I
{
	int Value { get; }
	int SetValue ();
}

struct S : I
{
	int value;

	public int Value {
		get {
			return value;
		}
	}

	public int SetValue ()
	{
		value = 5;
		return 0;
	}
}
