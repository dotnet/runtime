using System;
using System.Diagnostics;

[Generic]
[Generic (10)]
[DebuggerDisplay ("bla")]
public class Target 
{
	[DebuggerHidden]
	public void Foo() {}
}

[AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
public sealed class GenericAttribute : Attribute
{
	public GenericAttribute () {}
	public GenericAttribute (int x) {}
}

public class Class
{

	public static void Main ()
	{
	
	}
}
