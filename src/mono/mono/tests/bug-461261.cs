using System;
using System.Collections.Generic;

public interface ICode
{
	string Code { get; }
}

static class Program
{
	public static void Main ()
	{
		IEnumerable<object> array = new ICode[10];
		IEnumerator<object> x = array.GetEnumerator ();

		x.MoveNext ();
		object o = x.Current;
	}
}
