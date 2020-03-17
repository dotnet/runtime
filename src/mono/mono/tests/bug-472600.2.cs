using System;
using System.Collections.Generic;

public class EventInfoTestClass
{
	static EventInfoTestClass ()
	{
		string s = System.Environment.StackTrace;
	}
}

class Test
{
	static void Main ()
	{
		TestEventSubscription<object> ();
	}

	public static void TestEventSubscription<T> ()
	{
		new EventInfoTestClass ();
	}
}
