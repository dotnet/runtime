// Load an interface from an invalid DLL and ensure the failure is clean.
// Notice this is very similar to bug-81673, except the interface is loaded
// through a transparent proxy instead of directly.

using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Remoting.Messaging;

namespace Application
{
	public class App
	{
		public static void Test ()
		{
	    	RemoteProxy remote2 = new RemoteProxy (typeof(App).Assembly.GetType("Application.Remote"));
	    	remote2.GetTransparentProxy ();
		}

		public static int Main ()
		{
			int numCaught = 0;

			for (int i = 0; i < 10; ++i) {
				try {
					Test ();
				} catch (Exception) {
					++numCaught;
				}
			}
			if (numCaught == 10)
				return 0;
			return 1;
		}
	}

	class Remote : MarshalByRefObject, IMyInterface {
		public void Run ()
		{
		}
	}

	class RemoteProxy : RealProxy {
		public RemoteProxy (Type t) : base (t) {

		}

		public override IMessage Invoke (IMessage request) {
			return null;
		}
	}
}
