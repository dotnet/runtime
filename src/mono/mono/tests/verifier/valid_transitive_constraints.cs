using System;

public interface ICacheable {}

public class Foo<T, U> where T : U, new() where U : ICacheable, new()
{
	public object Test () {
		return new Bar<T> ();
	}

}

public class Bar<T> where T : ICacheable {}

public class Test : ICacheable {}

public class Program
{
    static void Main()
    {
		var x = new Foo<Test, Test> ();
		x.Test ();
    }
}
