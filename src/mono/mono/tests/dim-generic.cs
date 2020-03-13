using System;


interface IBaseThingy
{
	int Foo ();
}

interface INativeThingy<T> : IBaseThingy
{
	int IBaseThingy.Foo () {
		return 0;
        }
}

class NativeThingy : INativeThingy<string>
{
}

public class Test
{
	public static int test_0_dim_override()
	{
		var thingy = new NativeThingy ();
		var ithingy = (IBaseThingy)thingy;
		int i = ithingy.Foo ();
		return i;
    }

    public static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Test), args);
	}

}
