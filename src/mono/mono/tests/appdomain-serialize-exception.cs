using System;
using System.Reflection;
using System.Runtime.Serialization;

public class UnserializableException : Exception
{
}

public class TestOutput : MarshalByRefObject
{
	public void ThrowUnserializable ()
	{
		Console.WriteLine("Throwing Unserializable exception in AppDomain \"{0}\"", AppDomain.CurrentDomain.FriendlyName);
		throw new UnserializableException ();
	}
}

public class Example
{
	public static int Main ()
	{
		string original_domain = AppDomain.CurrentDomain.FriendlyName;

		AppDomain ad = AppDomain.CreateDomain("subdomain");
		try {
			TestOutput remoteOutput = (TestOutput) ad.CreateInstanceAndUnwrap(
				typeof (TestOutput).Assembly.FullName,
				"TestOutput");
			remoteOutput.ThrowUnserializable ();
		} catch (SerializationException) {
			Console.WriteLine ("Caught serialization exception");
		} catch (Exception) {
			Console.WriteLine ("Caught other exception");
			Environment.Exit (1);
		} finally {
			Console.WriteLine ("Finally in domain {0}", AppDomain.CurrentDomain.FriendlyName);
			if (original_domain != AppDomain.CurrentDomain.FriendlyName)
				Environment.Exit (2);
			AppDomain.Unload (ad);
		}

		Console.WriteLine ("All OK");
		return 0;
	}
}
