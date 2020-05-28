using System;

public class Test {

	public enum MyEnum {
		ZERO,
		ONE
	}

	public static int Main() {
		MyEnum en = MyEnum.ONE;
		IComparable ic;
		object o = en;
		
		ic = (IComparable)o;
		
		ic = (object)en as IComparable;
		if (ic == null)
			return 1;
		
		return 0;
	}
}
