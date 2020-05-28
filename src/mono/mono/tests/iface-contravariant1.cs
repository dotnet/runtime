using System;

/* Test of constrained.callvirt resolution for contravariant interfaces */


public interface I<in T>
{
		string F (T t);
}

public class C : I<object>
{
	public string F (object t)
	{
		return t.GetType().Name;
	}
}

/* U should be instantiated by a valuetype because we don't want the generic
 * sharing codepath */
public class G<T, TI, U>
	where TI : I<T>
{
	public G(TI i)
	{
		_i = i;
	}

	public string Do (T t)
	{
		// we want to get this in IL:
		//
		// constrained. !1
		// callvirt I`1<!T>::F(!0)
		//
		return _i.F (t);
	}
	
	private readonly TI _i;
}

public class Driver
{
	public static int Main ()
	{
		var c = new C();
		// instantiate with: T=string because we want to be
		// contravariant with object; U=int because we need a valuetype
		// to not end up in the generic sharing codepath.
		var h = new G<string, C, int>(c);
		var s = h.Do ("abc");
		var expected = typeof(string).Name;
		if (s == expected)
			return 0;
		else {
			Console.Error.WriteLine ("Got '{0}', expected '{1}'", s, expected);
			return 1;
		}
	}
}
