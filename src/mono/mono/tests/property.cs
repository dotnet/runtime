using System;

public class TestProp {

	private int my_prop;

	public int MyProp {
		get {return my_prop;}
		set {my_prop = value;}
	}

	public TestProp (int v) {
		my_prop = v;
	}

	public static int Main() {
		TestProp p = new TestProp (2);
		if (p.MyProp != 2)
			return 1;
		p.MyProp = 54;
		if (p.MyProp != 54)
			return 2;
    	return 0;
	}
}
