using System;
using System.IO;
using System.Security.Policy;
using System.Threading;
using System.Runtime.Serialization;

class Container {

	[Serializable]
	public struct c2 : ISerializable {
		public int a;
		public string s1;

		private c2 (SerializationInfo info, StreamingContext context) {
			a = info.GetInt32("a");
			s1 = info.GetString("s1");
		}

		public void GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("a", a);
			if (s1 != null)
				info.AddValue ("s1", s1);
			else
				info.AddValue ("s1", "(null)");
		}
	}
	
	[Serializable]
	public class c1 {
		public int a = 1;
		public int b = 2;
		public string s1 = "TEST1";
		[NonSerialized] public string s2 = "TEST2";
		public c2 e1;
	}
	
	static int Main ()
	{
		Console.WriteLine ("Friendly name: " + AppDomain.CurrentDomain.FriendlyName);

		AppDomainSetup setup = new AppDomainSetup ();
		setup.ApplicationBase = Directory.GetCurrentDirectory ();
			
		AppDomain newDomain = AppDomain.CreateDomain ("NewDomain", null, setup);

		c1 a1 = new c1 ();
		a1.e1.a = 3;
		a1.e1.s1 = "SS";
        
		newDomain.SetData ("TEST", a1);
	        
		c1 r1 = (c1)newDomain.GetData ("TEST");
	        
		if (r1.a != 1 || r1.b !=2)
				return 1;
			
		if (r1.s1 != "TEST1")
			return 2;
		
		if (r1.s2 != null)
			return 3;

		if (r1.e1.a != 3)
			return 4;

		if (r1.e1.s1 != "SS")
			return 5;
			
		Console.WriteLine("test-ok");

		return 0;
	}
}
