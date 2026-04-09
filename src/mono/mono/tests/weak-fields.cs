using System;
using System.Threading;
using MonoTests.Helpers;

[AttributeUsage(AttributeTargets.Field)]
public sealed class Weak2Attribute : Attribute
{
}

public class Finalizable {
	public int a;

	~Finalizable () {
		Console.WriteLine ("Finalized. {0}", a);
	}
}

public class OneField {
	int x;
}
public class Tests
{
	static Finalizable retain;

	[Weak]
	public object Obj;
	[Weak2]
	public object Obj3;
	[Weak]
	public object Obj2;
	[Weak]
	public Finalizable Obj4;

	public static int Main (String[] args) {
		var t = new Tests ();
		FinalizerHelpers.PerformNoPinAction (delegate () {
				FinalizerHelpers.PerformNoPinAction (delegate () {
						t.Obj = new Finalizable ();
						t.Obj2 = new Finalizable ();
						t.Obj3 = new Finalizable ();
						t.Obj4 = retain = new Finalizable ();
						retain.a = 0x1029458;
					});
				GC.Collect (0);
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
				if (t.Obj != null)
					Environment.Exit (1);
				if (t.Obj2 != null)
					Environment.Exit (2);
				if (t.Obj3 == null)
					Environment.Exit (3);
				//overflow the nursery, make sure we fill it
				for (int i = 0; i < 1000 * 1000 * 10; ++i)
					new OneField ();

				if (retain.a != 0x1029458)
					Environment.Exit (4);

				retain = null;
			});
		GC.Collect ();
		GC.WaitForPendingFinalizers ();
		if (t.Obj4 != null)
			return 5;

		return 0;
	}
	
}
