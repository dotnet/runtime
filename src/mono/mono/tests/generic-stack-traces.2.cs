using System;
using System.Diagnostics;
using System.Reflection;

public class Gen<T> {
    public static void staticCrash () {
	object o = null;
	o.GetType ();
    }

    public void callStaticCrash () {
	staticCrash ();
    }
}

class Foo<T1, T2> {

	public void Throw () {
		throw new Exception ();
	}

	public void Throw<T3> () {
		throw new Exception ();
	}
}

class Bar<T> : Foo<object, T> {
}

public class main {
    public static void callCallStaticCrash<T> () {
	Gen<T> gt = new Gen<T> ();
	gt.callStaticCrash ();
    }

    public static bool test (Exception exc, Type type) {
	StackTrace st = new StackTrace (exc);
	for (int i = 0; i < st.FrameCount; ++i) {
	    StackFrame sf = st.GetFrame (i);
	    MethodBase m = sf.GetMethod ();
		if (m == null)
			continue;
	    Type t = m.DeclaringType;
	    if (m.IsGenericMethod) {
		Type[] margs = m.GetGenericArguments ();
		//Console.WriteLine (m.Name);
		if (margs.Length != 1)
		    return false;
		if (margs [0] != type)
		    return false;
	    }
	    if (t.IsGenericType) {
		Type[] targs = t.GetGenericArguments ();
		//Console.WriteLine (t.FullName);
		if (targs.Length != 1)
		    return false;
		if (targs [0] != type)
		    return false;
	    }
	}
	return true;
    }

    public static int Main () {
	try {
	    callCallStaticCrash <int> ();
	} catch (Exception exc) {
	    if (!test (exc, typeof (int)))
		return 1;
	}
	try {
	    callCallStaticCrash <object> ();
	} catch (Exception exc) {
	    if (!test (exc, typeof (object)))
		return 1;
	}
	try {
	    callCallStaticCrash <string> ();
	} catch (Exception exc) {
	    if (!test (exc, typeof (string)))
		return 1;
	}
	try {
	    callCallStaticCrash <Gen<string>> ();
	} catch (Exception exc) {
	    if (!test (exc, typeof (Gen<string>)))
		return 1;
	}

	// Exception thrown in inherited method with different generic context
	// (#509406)
	try {
		new Bar <string> ().Throw ();
	} catch (Exception ex) {
		Console.WriteLine (new StackTrace (ex));
	}

	try {
		new Bar <string> ().Throw<Bar<string>> ();
	} catch (Exception ex) {
		Console.WriteLine (new StackTrace (ex));
	}

	return 0;
    }
}
