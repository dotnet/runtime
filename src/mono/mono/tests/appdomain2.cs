using System;
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
			Console.WriteLine ("SetObjectData called: " + info.AssemblyName + "," +
					   info.FullTypeName + " " + s1 + ", " + a);
		}

		public void GetObjectData (SerializationInfo info, StreamingContext context) {
			Console.WriteLine ("GetObjectData called: " + info.AssemblyName + "," +
					   info.FullTypeName + " " + s1 + ", " + a);
			info.AddValue ("a", a);
			if (s1 != null)
				info.AddValue ("s1", s1);
			else
				info.AddValue ("s1", "(null)");
		}
	}
	
	[Serializable]
	public class c1 {
		public c1 () {
			e1.a = 3;
			e1.s1 = "SS";
		}
		public int a = 1;
		public int b = 2;
		public string s1 = "TEST1";
		[NonSerialized] public string s2 = "TEST2";
		public c2 [] sa = new c2 [2];
		public c2 e1;
	}
	
	static int Main ()
	{
		AppDomainSetup setup = new AppDomainSetup ();
		setup.ApplicationBase = ".";

		Console.WriteLine (AppDomain.CurrentDomain.FriendlyName);
			
		AppDomain newDomain = AppDomain.CreateDomain ("NewDomain", new Evidence (), setup);

		c1 a1 = new c1 ();
		
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
			return 4;
		
		if (r1.sa [0].s1 != "(null)")
			return 5;
		
		if (r1.sa [0].a != 0)
			return 6;

		return 0;
	}
}
