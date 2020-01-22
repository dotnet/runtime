using System;

public delegate T Dele<T,K> (K k);

public class Generic<T> {
}

public interface Foo9<T> {
}
public class Foo <KEY> {

	public VAL Method<VAL> (Generic <KEY> k) {
		Dele<VAL,Generic <KEY>> t = new Dele<VAL,Generic <KEY>> (Test<VAL>);
			
		return t (k);
	}

	public static VAL Test<VAL> (Generic <KEY> k) {
		VAL v = default(VAL);
		return v;
	}
}

public class Driver {
	public static void Main () {
		new Foo<int>().Method<string>(new Generic<int>());
	}

	
}
