using System;

public delegate T Dele<T,K> (K k);

public class Foo <KEY> {

	public VAL Method<VAL> (KEY k) {
		Dele<VAL,KEY> t = new Dele<VAL,KEY> (Test<VAL>);
			
		return t (k);
	}

	public static VAL Test<VAL> (KEY k) {
		VAL v = default(VAL);
		return v;
	}
}

public class Driver {
	public static void Main () {
		new Foo<int>().Method<string>(99);
	}

	
}
