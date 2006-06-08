using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

public class Program {

	private const string filename = "library2a";

	static int GetTypeFalse (Assembly a)
	{
		string typename = "InheritanceDemand";
		Type t = a.GetType (typename, false);
		if (t == null) {
			Console.WriteLine ("*0* Get null for type '{0}' with security.", typename);
			return 0;
		} else {
			Console.WriteLine ("*1* Can get type '{0}' with security (false).", typename);
			return 1;
		}
	}

	static int GetTypeTrue (Assembly a)
	{
		try {
			string typename = "InheritanceDemand";
			Type t = a.GetType (typename, true);
			Console.WriteLine ("*1* Can get type '{0}' with security (true).", t);
			return 1;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*0* Expected SecurityException\n{0}", se);
			return 0;
		}
	}

	static int GetTypes (Assembly a)
	{
		try {
			Type[] ts = a.GetTypes ();
			Console.WriteLine ("*1* Can get all types from assembly '{0}' loaded. {1} types present.", filename, ts.Length);
			return 1;
		}
		catch (ReflectionTypeLoadException rtle) {
			Console.WriteLine ("*0* Expected ReflectionTypeLoadException\n{0}", rtle);
			Console.WriteLine ("Types ({0}):", rtle.Types.Length);
			for (int i=0; i < rtle.Types.Length; i++) {
				Console.WriteLine ("\t{0}", rtle.Types [i]);
			}
			Console.WriteLine ("LoaderExceptions ({0}):", rtle.LoaderExceptions.Length);
			for (int i=0; i < rtle.LoaderExceptions.Length; i++) {
				Console.WriteLine ("\t{0}", rtle.LoaderExceptions [i]);
			}
			return 0;
		}
	}

	static int Main ()
	{
		try {
			Assembly a = Assembly.Load (filename);
			if (a == null) {
				Console.WriteLine ("*2* Couldn't load assembly '{0}'.", filename);
				return 2;
			}

			string typename = "NoSecurity";
			Type t = a.GetType (typename);
			if (t == null) {
				Console.WriteLine ("*3* Cannot get type '{0}' without security.", typename);
				return 3;
			}

			int err = GetTypeFalse (a);
			if (err != 0)
				return err;

			err = GetTypeTrue (a);
			if (err != 0)
				return err;

			err = GetTypes (a);
			return err;
		}
		catch (ReflectionTypeLoadException rtle) {
			Console.WriteLine ("*4* Expected ReflectionTypeLoadException\n{0}", rtle);
			return 4;
		}
		catch (SecurityException se) {
			Console.WriteLine ("*5* Unexpected SecurityException\n{0}", se);
			return 5;
		}
		catch (Exception e) {
			Console.WriteLine ("*6* Unexpected Exception\n{0}", e);
			return 6;
		}
	}
}
