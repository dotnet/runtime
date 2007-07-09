// App.cs created with MonoDevelop
// User: lluis at 15:46 18/05/2007
//

using System;

namespace Application
{
	public class App
	{
		public static void Test ()
		{
			MyClass c = new MyClass ();
			c.Run ();
		}

		public static int Main ()
		{
			int numCaught = 0;

			for (int i = 0; i < 10; ++i) {
				try {
					Test ();
				} catch (Exception ex) {
					++numCaught;
				}
			}
			if (numCaught == 10)
				return 0;
			return 1;
		}
	}
	
	class MyClass: IMyInterface
	{
		public void Run ()
		{
		}
	}
}
