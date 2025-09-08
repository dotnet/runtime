using System;
using System.Reflection;

class B {
	public virtual int vmethod () {
		return 0;
	}
}

class T : B {

	public override int vmethod () {
		return 1;
	}
	static int stuff (int a) {
		return 0;
	}
	static int stuff (char a) {
		return 1;
	}
	static int Main () {
		Type t = typeof (T);
		Type b = typeof (B);
		T obj = new T ();
		Type[] char_types = new Type[1] {typeof(char)};
		Type[] int_types = new Type[1] {typeof(int)};
		object[] int_args = new object[1] {1};
		object[] char_args = new object[1] {(char)1};
		MethodBase m1, m2;
		bool ok = false;
		try {
			m1 = t.GetMethod ("stuff", BindingFlags.Static|BindingFlags.NonPublic);
		} catch (AmbiguousMatchException) {
			ok = true;
		}
		if (!ok)
			return 1;

		m1 = t.GetMethod ("stuff", BindingFlags.Static|BindingFlags.NonPublic,
			null, char_types, null);
		Console.WriteLine ("m1: {0}", m1);
		if (m1 == null)
			return 2;

		object m1res = m1.Invoke (null, char_args);
		Console.WriteLine ("m1 invoke: {0}", m1res);
		if ((int)m1res != 1)
			return 3;
		
		ok = false;
		try {
			m1res = m1.Invoke (null, int_args);
		} catch (ArgumentException) {
			ok = true;
		}
		if (!ok)
			return 4;
		
		m2 = b.GetMethod ("vmethod");
		Console.WriteLine ("m2: {0}, declaring: {1}, reflected: {2}", m2, m2.DeclaringType, m2.ReflectedType);
		object m2res = m2.Invoke (obj, null);
		if ((int)m1res != 1)
			return 5;

		return 0;
	}
}

