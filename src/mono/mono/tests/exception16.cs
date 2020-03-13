using System;

public class TestTryFinally 
{
	static int result = 0;
	
	public static void TrivialMain() 
	{
		int i = 123;
		string s = "Some string";
		object o = s;

		try {
			// Illegal conversion; o contains a string not an int
			i = (int) o;   
		}
		finally {
			Console.WriteLine("i = {0}", i);
			result = i;
		}
	}

	public static int Main() 
	{
		try {
			try {
				TrivialMain();
			}
			finally {
				Console.WriteLine("cleaning up");
			}
		}
		catch(Exception) {
			Console.WriteLine("catch expected exception");
			result += 1;
		}

		if (result != 124)
			return 1;

		return 0;
	}
}

