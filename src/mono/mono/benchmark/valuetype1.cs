using System;

public class ValueType1
{
	static int Main ()
	{
		Blah a = new Blah ("abc", 1);

		for (int i = 0; i < 1000000; i++)
			a.GetHashCode ();

		return 0;
	}

	struct Blah
	{ 
		public string s;
		public int i;

		public Blah (string s, int k)
		{
			this.s = s;
			i = k;
		}
	}
}

