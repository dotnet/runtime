using System;

public static class Driver
{
	delegate void SimpleDelegate ();

	static int error = 0;

	class VirtualDelegate0
	{
		public virtual void OnEvent ()
		{
			Console.WriteLine ("VirtualDelegate0.OnEvent (error!)");
			error = 1;
		}
	}

	class VirtualDelegate1 : VirtualDelegate0
	{
		public override void OnEvent ()
		{
			Console.WriteLine ("VirtualDelegate1.OnEvent");
		}
	}

	class NonVirtualDelegate
	{
		public void OnEvent ()
		{
			Console.WriteLine ("NonVirtualDelegate.OnEvent");
		}
	}

	static bool check (SimpleDelegate d)
	{
		error = 0;
		d ();
		return error == 0;
	}

	public static int Main ()
	{
		SimpleDelegate dv = new SimpleDelegate (new VirtualDelegate1 ().OnEvent);
		SimpleDelegate dnv = new SimpleDelegate (new NonVirtualDelegate ().OnEvent);

		if (!check (dv + dv))
			return 1;
		if (!check (dnv + dv))
			return 2;
		if (!check (dv + dnv))
			return 3;
		if (!check (dnv + dnv))
			return 4;

		return 0;
	}
}
