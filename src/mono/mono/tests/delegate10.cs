using System;
using System.Reflection;

public class Tests
{
	public struct Test {
		public string MyProp {get; set;}
	}

	delegate string GetterDelegate (ref Test arg);

	public static int Main (String[] args) {
		var m = typeof (Tests.Test).GetProperty ("MyProp").GetMethod;

		var d = (GetterDelegate)m.CreateDelegate (typeof (GetterDelegate));

		var s = new Test () { MyProp = "A" };
		if (d (ref s) == "A")
			return 0;
		else
			return 1;
	}
}
