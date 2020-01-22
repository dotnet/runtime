using System;
using System.Reflection;

/*
public delegate object ArrDel (int i);

public class Gen<T> {
    public object newArr (int i) {
	return new T [i];
    }

    public ArrDel newDel () {
	return new ArrDel (this.newArr);
    }
}

public delegate object DelObj (object g, int i);
public delegate object DelStr (object g, int i);
*/

public class main {
    public static int work () {
	Gen<string> gs = new Gen<string> ();
	Gen<object> go = new Gen<object> ();

	MethodInfo miObj = typeof (Gen<object>).GetMethod ("newArr", BindingFlags.Public | BindingFlags.Instance);
	MethodInfo miStr = typeof (Gen<string>).GetMethod ("newArr", BindingFlags.Public | BindingFlags.Instance);

	if (miObj == miStr) {
	    Console.WriteLine ("methods equal");
	    return 1;
	}

	ObjArrDel oad = go.newObjDel ();
	StrArrDel sad = gs.newStrDel ();
	StrArrDel sad2 = (StrArrDel)Delegate.CreateDelegate (typeof (StrArrDel), null, miStr);

	if (oad.Method != miObj) {
	    Console.WriteLine ("wrong object method");
	    if (oad.Method == miStr)
		Console.WriteLine ("object method is string");
	    return 1;
	}

	if (sad.Method != miStr) {
	    Console.WriteLine ("wrong string method");
	    if (sad.Method == miObj)
		Console.WriteLine ("string method is object");
	    else
		return 1;
	} else {
	    Console.WriteLine ("right string method");
	}

	if (sad2.Method != miStr) {
	    Console.WriteLine ("wrong string2 method");
	    if (sad2.Method == miObj)
		Console.WriteLine ("string2 method is object");
	    return 1;
	}

	Console.WriteLine ("calling object del");
	if (oad (go, 3).GetType () != typeof (object [])) {
	    Console.WriteLine ("not object array");
	    return 1;
	}

	Console.WriteLine ("calling string del");
	if (sad (gs, 3).GetType () != typeof (string [])) {
	    Console.WriteLine ("not string array");
	    return 1;
	}

	Console.WriteLine ("calling string del2");
	if (sad2 (gs, 3).GetType () != typeof (string [])) {
	    Console.WriteLine ("not string2 array");
	    return 1;
	}

	try {
	    StrArrDel sad3 = (StrArrDel)Delegate.CreateDelegate (typeof (StrArrDel), null, miObj);
	    Console.WriteLine ("object method for string delegate");
	    return 1;
	} catch (ArgumentException) {
	}

	/*
	DelObj delObj = (DelObj)Delegate.CreateDelegate (typeof (DelObj), null, miObj);

	if (delObj (go, 3).GetType () != typeof (object []))
	    return 1;

	DelStr delStr = (DelStr)Delegate.CreateDelegate (typeof (DelStr), null, miStr);

	if (delStr (gs, 3).GetType () != typeof (string []))
	    return 1;
	*/

	/*
	ArrDel ad = go.newDel ();
	if (ad (3).GetType () != typeof (object []))
	    return 1;

	ad = gs.newDel ();
	if (ad (3).GetType () != typeof (string []))
	    return 1;
	*/

	Console.WriteLine ("done");

	return 0;
    }

    public static int Main () {
	Gen<string> gs = new Gen<string> ();
	Gen<object> go = new Gen<object> ();

	gs.newArr (3);
	go.newArr (3);

	return work ();
    }
}
