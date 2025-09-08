using System;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

/* Regression test for https://github.com/mono/mono/issues/7944 */
public class MyClass
{
	public string Prop1 { get; set; }
	public string Prop2 { get; set; }

	public void DoRepro()
	{
		var prop1Setter = this.GetType ().GetProperty (nameof (Prop1)).GetSetMethod ();
		var prop2Setter = this.GetType ().GetProperty (nameof (Prop2)).GetSetMethod ();
		var prop1Delegate = (Action <MyClass, string>) prop1Setter.CreateDelegate(typeof (Action <MyClass, string>));
		var prop2Delegate = (Action <MyClass, string>) prop2Setter.CreateDelegate(typeof (Action <MyClass, string>));
		prop1Delegate (this, "prop1Value");
		prop2Delegate (this, "prop2Value");

		// Console.WriteLine ($"prop1: {Prop1}");
		// Console.WriteLine ($"prop2: {Prop2}");
	}

	public static int Main (string []args) {
		var o = new MyClass ();
		o.DoRepro ();

		if (o.Prop1 != "prop1Value")
			return 1;

		if (o.Prop2 != "prop2Value")
			return 2;

		return 0;
	}
}

