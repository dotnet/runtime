using System;

public class ValueType2
{
	static int Main ()
	{
		Blah a = new Blah ("abc", 1);
		Blah b = new Blah (string.Format ("ab{0}", 'c'), 1);

		for (int i = 0; i < 1000000; i++)
			a.Equals (b);

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

