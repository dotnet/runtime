
using System;
using System.Runtime.CompilerServices;

namespace MidAssembly {
	[Serializable]
	public class MidClass : MarshalByRefObject {
		public MidClass () {
			Console.WriteLine ("Created mid in {0}", AppDomain.CurrentDomain);
		}

		public void MidMethod (object leaf) {
			Console.WriteLine ("Called MidMethod in {0}", AppDomain.CurrentDomain);
			var ad = AppDomain.CurrentDomain;
			Console.WriteLine ("Domain {0} has loaded:", ad);
			foreach (var assm in ad.GetAssemblies ()) {
				Console.WriteLine (" - {0}", assm);
				Console.WriteLine ("     with location: {0}", assm.Location);
			}
		}


		public void ForceLoadFrom (string assmPath)
		{
			Console.WriteLine ($" loading from {assmPath} into {AppDomain.CurrentDomain}");
			System.Reflection.Assembly.LoadFrom (assmPath);
		}

		public void DoSomeAction () {
			LeafAssembly.OtherLeafClass.PublicMethod ();
		}
	}
}
