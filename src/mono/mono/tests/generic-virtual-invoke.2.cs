using System;
using System.Reflection;

public abstract class Base {
	public abstract object virt<T> ();
}

public class Derived : Base {
	public override object virt<T> () {
		return new T [3];
	}
}

public class main {
	public static int Main () {
		Base b = new Derived ();

		MethodInfo method = typeof (Base).GetMethod ("virt");
		Type [] args = { typeof (string) };

		method = method.MakeGenericMethod (args);

		if (method.Invoke (b, null).GetType () != typeof (string []))
			return 1;
		return 0;
	}
}
