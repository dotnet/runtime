using System;

class Test<T>  where T :Exception {

	public void test_catch () {
		try {
			throw new RankException ();
		} catch (T) {
		}
	}
}

public class Program
{
	delegate void Action();
	static void ExpectedException<T>(Action action) where T: Exception
	{
		try { 
			action();
			Console.WriteLine("Expected Exception: " + typeof(T).FullName);
		} catch (T) { }
	}

	public static void Main()
	{
		ExpectedException<DivideByZeroException>(delegate() { throw new DivideByZeroException(); });
		Test<RankException> t = new Test<RankException> ();
		t.test_catch ();
	}
}
