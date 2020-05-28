using System;

public class ValueType1
{
	static int Main ()
	{
		Blah a = new Blah ("abc", 1);
		Blah b = new Blah ("ab" + 'c', 1);
		long start, end;
		start = Environment.TickCount;

		start = Environment.TickCount;
		for (int i = 0; i < 1000000; i++)
			a.GetHashCode ();
		end = Environment.TickCount;
		Console.WriteLine ("struct common GetHashCode(): {0}", end-start);

		start = Environment.TickCount;
		for (int i = 0; i < 1000000; i++)
			a.Equals (b);
		end = Environment.TickCount;
		Console.WriteLine ("struct common Equals(): {0}", end-start);

		Blah2 a2 = new Blah2 ("abc", 1);
		Blah2 b2 = new Blah2 ("abc", 1);
		start = Environment.TickCount;
		for (int i = 0; i < 1000000; i++)
			a2.GetHashCode ();
		end = Environment.TickCount;
		Console.WriteLine ("struct specific GetHashCode(): {0}", end-start);

		start = Environment.TickCount;
		for (int i = 0; i < 1000000; i++)
			a2.Equals (b2);
		end = Environment.TickCount;
		Console.WriteLine ("struct specific Equals(): {0}", end-start);

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

	struct Blah2
	{ 
		public string s;
		public int i;

		public Blah2 (string s, int k)
		{
			this.s = s;
			i = k;
		}

		public override int GetHashCode () {
			return i ^ s.GetHashCode ();
		}
		public override bool Equals (object obj) {
			if (obj == null || !(obj is Blah2))
				return false;
			Blah2 b = (Blah2)obj;
			return b.s == this.s && b.i == this.i;
		}
	}
}

