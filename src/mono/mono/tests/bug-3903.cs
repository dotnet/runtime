using System;
using System.Collections.Generic;
using System.Linq;

struct Foo
{
}


public class TestClass
{
    public static int Main ()
    {
        Foo[][] array = new Foo[][] { new Foo[0] };
		IEnumerable<object> aa1 = array;
		foreach (var x in aa1) Console.WriteLine (x);
		aa1.GetEnumerator ().ToString ();

		int[] array2 = new int[10];
		IEnumerable<uint> aa2 = (uint[])(object)array2;
		foreach (var x in aa2) Console.WriteLine (x);
		aa2.GetEnumerator ().ToString ();

        // The next line will crash
        List<object> list = array.Cast<object>().Select((arg) => arg).ToList();
		return 0;
    }
}
