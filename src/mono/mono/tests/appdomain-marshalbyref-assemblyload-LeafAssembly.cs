
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Proxies;

namespace LeafAssembly {
	public class Leaf : MarshalByRefObject {
		static Leaf () {
			Console.WriteLine ("Static leaf constructor called in {0}", AppDomain.CurrentDomain);
		}
		public Leaf () {
			Console.WriteLine ("Created leaf in {0}", AppDomain.CurrentDomain);
		}
	}

	public class OtherLeafClass {
		/* We build this assembly twice: once into
		 * appdomain-marshalbyref-assemblyload1/ with PublicMethod()
		 * present, and once into appdomain-marshalbyref-assemblyload2/
		 * without it.  The regression test tries to trick Mono into
		 * loading from -assemblyload2/.
		 */
#if !UNDEFINE_OTHER_METHOD
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void PublicMethod () {

		}
#endif
	}
}
