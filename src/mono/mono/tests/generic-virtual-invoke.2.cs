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

public class ClassA {}

public class main {
	public static int Main () {
		Base b = new Derived ();

		MethodInfo method = typeof (Base).GetMethod ("virt");
		Type [] arg_types = { typeof (object), typeof (string), typeof (ClassA),
				      typeof (Base), typeof (Derived) };
		Type [] array_types = { typeof (object []), typeof (string []), typeof (ClassA []),
					typeof (Base []), typeof (Derived []) };

		for (int j = 0; j < 100; ++j)
			for (int i = 0; i < arg_types.Length; ++i)
			{
				Type [] args = { arg_types [i] };
				MethodInfo inflated = method.MakeGenericMethod (args);

				if (inflated.Invoke (b, null).GetType () != array_types [i])
					return 1;
			}

		return 0;
	}
}
