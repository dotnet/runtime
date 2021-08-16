using System;
using Repro;

public class Extended : Base {
	protected override int Test () { return 0; }
}


public class Driver {
	public static int Main () {
		return new Generic<Extended>().Run(new Extended());
	}
}
