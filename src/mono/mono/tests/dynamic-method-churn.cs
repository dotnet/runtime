using System;
using System.Reflection;
using System.Reflection.Emit;

class Program
{
	//See GH #9176
	static int Main (string[] args) {
		for (int i = 0; i < 20000; ++i) {
			DynamicMethod dm = new DynamicMethod(
				$"dm_{i}",
				null,
				new Type[0],
				typeof(Program).Module);

			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Ret);

			Action a = (Action) dm.CreateDelegate(typeof(Action));
			a();
			var m = a.Method;
			m.Invoke (null, null);
			if ((i % 50) == 0) {
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			}
		}
		return 0;
	}
}
