using System;

public interface ICommon {
	int DoIt ();
}

public class Base : ICommon {
	int ICommon.DoIt () { return 1; }
	public virtual int DoIt () { return 2; }
}

public class Derived : Base, ICommon {
	int ICommon.DoIt () { return 3; }
	public new virtual int DoIt () { return 4; }
}

public class ReallyDerived : Derived {
	public override int DoIt () { return 5; }
}

public class Test {

	static int Main () {
		ReallyDerived r1 = new ReallyDerived ();
		Derived       r2 = r1;
		Base          r3 = r1;
		ICommon       r4 = r1;
		Object        r5 = r1;

		if (r1.DoIt() != 5)
			return 1;

		//		Console.WriteLine ("TEST {0}", ((ICommon)r1).DoIt ());

		if (((ICommon)r1).DoIt() != 3)
			return 2;

		if (r2.DoIt() != 5)
			return 3;
		
		if (r3.DoIt() != 2)
			return 4;
		
		if (r4.DoIt() != 3)
			return 5;
		
		return 0;
	}
}
