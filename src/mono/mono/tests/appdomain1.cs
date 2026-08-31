using System;
using System.Security.Policy;
using System.Runtime.Remoting;
using System.Threading;

class Container {

	class MBRTest : MarshalByRefObject
	{
		public int Int {
			get {
				return (int) AppDomain.CurrentDomain.GetData("test_integer");
			}
		}

		public string Str {
			get {
				return (string) AppDomain.CurrentDomain.GetData("test_string");
			}
		}

		public bool Bool {
			get {
				return (bool) AppDomain.CurrentDomain.GetData("test_bool");
			}
		}

		public int [] Arr {
			get {
				return (int []) AppDomain.CurrentDomain.GetData("test_array");
			}
		}
	}

	static int Main ()
	{
		Console.WriteLine ("Friendly name: " + AppDomain.CurrentDomain.FriendlyName);
			
		AppDomain newDomain = AppDomain.CreateDomain ("NewDomain");

		if (!RemotingServices.IsTransparentProxy(newDomain))
			return 1;

		// First test that this domain get's the right data from the other domain
		newDomain.SetData ("test_string", "a");

		object t = newDomain.GetData("test_string");
		if (t.GetType() != typeof(string))
			return 2;

		if ((string) newDomain.GetData ("test_string") != "a")
			return 3;

		newDomain.SetData ("test_integer", 1);
		if ((int) newDomain.GetData ("test_integer") != 1)
			return 4;

		newDomain.SetData ("test_bool", true);
		if ((bool)newDomain.GetData ("test_bool") != true)
			return 5;

		newDomain.SetData ("test_bool", false);
		if ((bool)newDomain.GetData ("test_bool") != false)
			return 6;

		int [] ta = { 1, 2, 3 };
		newDomain.SetData ("test_array", ta);

		int [] ca = (int [])newDomain.GetData ("test_array");
		
		if (ca [0] != 1 || ca [1] != 2 || ca [2] != 3)
			return 7;

		// Creata a MBR object to test that the other domain has the correct info
		MBRTest test = (MBRTest) newDomain.CreateInstanceAndUnwrap (typeof(MBRTest).Assembly.FullName, typeof(MBRTest).FullName);
		
		if (!RemotingServices.IsTransparentProxy(test))
			return 8;

		// Time to test that the newDomain also have the same info
		if (test.Int != 1)
			return 9;

		if (test.Str != "a")
			return 10;

		if (test.Bool != false)
			return 11;

		ca = test.Arr;
		
		if (ca [0] != 1 || ca [1] != 2 || ca [2] != 3)
			return 12;

		Console.WriteLine("test-ok");

		return 0;
	}
}
