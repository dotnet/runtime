using System;
using System.Reflection;
using System.Collections.Generic;


class Test
{
    static U[] Foo<T, U> (T[] arg) where T : class, U
    {
        return arg;
    }

    public static IEnumerable<U> Foo2<T, U> (IEnumerable<T> arg) where T : class, U
    {
        return arg;
    }

    static IEnumerable<U[]> Foo3<T, U> (IEnumerable<T[]> arg) where T : class, U
    {
        return arg;
    }

    static int Main ()
    {
		var m = typeof (Test).GetMethod ("Foo2");
		var gp = m.GetGenericArguments ();
		var t = gp[0];
		var u = gp[1];
		Console.WriteLine (t);
		Console.WriteLine (u);
		Console.WriteLine (t.IsAssignableFrom (u));
		Console.WriteLine (u.IsAssignableFrom (t));
        return 0;
    }
}
