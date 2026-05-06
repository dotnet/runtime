using System;
using System.Reflection;

public class HostExpression {}
public class OMetaStream<T> {}
public class OMetaList<T> {}

public class OMetaParser {
	public virtual bool NameFirst (OMetaStream<char> inputStream, out object result) {
		result = null;
		Console.WriteLine ("ok");
		return true;
	}

}

public class Driver {
	static int Main () {
		var f = new OMetaParser ();
		var m = typeof (OMetaParser).GetMethod ("NameFirst");

		var arg0 = new OMetaStream<char> ();
		var arg1 = new OMetaList<HostExpression> ();

		bool res = (bool)m.Invoke (f, new object[] { arg0, arg1 });
		return res ? 0 : 1;
	}
}
