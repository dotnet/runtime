using System;

public class Library {

	private int _pouet;
	[NotLinked] private int _hey;

	public Library ()
	{
		_pouet = 1;
	}

	[NotLinked] public Library (int pouet)
	{
		_pouet = pouet;
	}

	public int Hello ()
	{
		Console.WriteLine ("Hello");
		return _pouet;
	}

	[NotLinked] public void Hey (int hey)
	{
		_hey = hey;
		Console.WriteLine (_hey);
	}
}


[NotLinked] public class Toy {
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
