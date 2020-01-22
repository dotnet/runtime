// Test for a MemberRef with a TypeSpec parent

class g<T>
{
	public void foo <A> (A _a)
	{
	}
}

class test {
	public static void Main ()
	{
		g<int> _g = new g<int> ();
		_g.foo ("abc");
	}
}
