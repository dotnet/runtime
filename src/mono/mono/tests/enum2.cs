using System;

enum A {
	Hello,
	Bye
}

class X {

	static int Main ()
	{
		int num = 0;

		object x = A.Hello;
		object y = A.Bye;

		num++;
		if ((int)x != 0)
			return num;
		
		num++;
		if ((int)y != 1)
			return num;
	
		num++;
		if ("Hello" != A.Hello.ToString ())
			return num;

		num++;
		if (!x.Equals(x))
			return num;
		
		num++;
		if (x.Equals(y))
			return num;
		
		num++;
		if (x.Equals(0))
			return num;

		num++;
		Type et = x.GetType ();
		object z = Enum.ToObject (et, Int64.MaxValue);
		if ((int)z != -1)
			return num;
		
		num++;
		z = Enum.ToObject (et, 0);
		if (!x.Equals(z))
			return num;
		
		num++;
		z = Enum.ToObject (et, 1);
		if (!y.Equals(z))
			return num;

		num++;
		z = Enum.Parse (et, "Bye");
		if (!y.Equals(z))
			return num;
		
		num++;
		try {
			z = Enum.Parse (et, "bye");
		} catch {
			z = null;
		}
		if (z != null)
			return num;

		num++;
		z = Enum.Parse (et, "bye", true);
		if (!y.Equals(z))
			return num;
		
		return 0;
	}
}
	
