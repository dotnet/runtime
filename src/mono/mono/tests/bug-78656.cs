delegate void voi ();

class BasicValueTypesTest
{
	static int trycatch (voi f) {
		try {
			f ();
			return 1;
		}
		catch (System.OverflowException e) {
			System.Console.WriteLine (e);
			return 0;
		}
	}


	static void foo1 () {
		checked {
			long x = System.Int64.MinValue;
			long y =  x - 1L;
		}
	}

	static void foo2 () {
		checked {
			byte x = System.Byte.MaxValue;
			++x;
		}
	}

	public static int Main () {
		int result = 0;
		
		result += trycatch (foo1);
		result += trycatch (foo2);
		
		return result;
	}
}

