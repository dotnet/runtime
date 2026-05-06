using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.ComponentModel;

//TODO add to a typeref
//TODO is Bla<D> a memberref?
//TODO to an entry in security table
//TODO add to a standalonesig
//TODO add to a moduleref
//TODO add to a typespec
//TODO add to a assemblyref
//TODO add to a file
//TODO add to a exportedtype
//TODO add to a manifesresource
//TODO add to an interfaceimpl


[assembly: Generic (14)]
[module: Generic (80)]


[AttributeUsage(AttributeTargets.All)]
public sealed class GenericAttribute : Attribute
{
	public GenericAttribute () {}
	public GenericAttribute (int x) {}
}

public interface IFace {}


public class Foo : IFace
{}

public class Foo<T> 
{
	[Generic (70)]
	public void Bla<D> () {}

}


public delegate int Del();

[Generic (30)]
public class Class
{
	[Generic (20)]
	int field;

	int Foo ([Generic (50)] int d) { return d; }

	[Generic (100)]
	public int Bla { get; set; }

	[Generic (110)]
	public event Del Zzz;

	[Generic (10)]
	public static void Main ()
	{
	
	}
}
