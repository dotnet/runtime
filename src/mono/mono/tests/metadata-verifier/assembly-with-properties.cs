using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.ComponentModel;

public delegate void Del ();

public class Class0
{
	const int foo = 10;
	public int PropA { get; set; }
	public int PropB { set { int x = value; } }
	public int PropC { get { return 0; } }
}

public class Class1
{
	public int Prop0 { get; set; }
	public int Prop1 { get; set; }
	public int Prop2 { get; set; }
}

public class Class2
{
	public int Prop0 { get; set; }
	public int Prop1 { get; set; }
	public int Prop2 { get; set; }

	public int this [string key] {
	    get { return 0; }
	    set { }
	}

}

public class Class
{
	public static void Main ()
	{
	
	}
}
