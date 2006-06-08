using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;

public class Program {

	private const string filename = "library2b";

	static int GetTypeFalse (Assembly a)
	{
		string typename = "InheritanceDemand";
		Type t = a.GetType (typename, false);
		if (t == null) {
			Console.WriteLine ("*1* Get null for type '{0}' with security.", typename);
			return 1;
		} else {
			Console.WriteLine ("*0* Can get type '{0}' with security.", typename);
			return 0;
		}
	}

	static int GetTypeTrue (Assembly a)
	{
		string typename = "InheritanceDemand";
		Type t = a.GetType (typename, true);
		Console.WriteLine ("*0* Can get type '{0}' with security.", t);
		return 0;
	}

	static int GetTypes (Assembly a)
	{
		Type[] ts = a.GetTypes ();
		Console.WriteLine ("*0* Can get all types from assembly '{0}' loaded. {1} types present.", filename, ts.Length);
		return 0;
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
