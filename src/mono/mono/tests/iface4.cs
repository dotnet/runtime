using System;

public interface IVehicle {
	int Start ();
	int Stop ();
	int Turn ();
}

public class Base : IVehicle {
	int IVehicle.Start () { return 1; }
	public int Stop () { return 2; }
	public virtual int Turn () { return 3; }
}

public class Derived1 : Base {
	// replaces Base.Turn + IVehice.Turn
	public override int Turn () { return 4; }
}

public class Derived2 : Base, IVehicle {
	// legal - we redeclared IVehicle support
	public new int Stop () { return 6; }
	// legal - we redeclared IVehicle support
	int IVehicle.Start () { return 5; }
	// replaces IVehicle.Turn 
	int IVehicle.Turn () { return 7; }
	// replaces Base.Turn 
	public override int Turn () { return 8; }
}

public class Test {

	static int Main () {
		Derived1 d1 = new Derived1 ();
		Derived2 d2 = new Derived2 ();
		Base b1 = d1;
		Base b2 = d2;

		if (d1.Turn () != 4)
			return 1;
		
		if (((IVehicle)d1).Turn () != 4)
			return 2;

		if (((IVehicle)d2).Turn () != 7)
			return 3;

		if (b2.Turn () != 8)
			return 4;
		
		if (((IVehicle)b2).Turn () != 7)
			return 5;
		
		//Console.WriteLine ("TEST {0}", ((IVehicle)b2).Turn ());	

		return 0;
	}
}
