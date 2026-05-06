using System;
using System.Reflection;
using System.Reflection.Emit;


class Driver {
	public void AvoidInlining()
	{
	}

	public int Foo()
	{
		AvoidInlining();
		return -99;
	}

	public static int Main()
	{

		DynamicMethod method_builder = new DynamicMethod ("WriteHello" , typeof (int), new Type[] {typeof (Driver)}, typeof (Driver));
		ILGenerator ilg = method_builder.GetILGenerator ();

		ilg.Emit (OpCodes.Ldarg_0);
		ilg.Emit (OpCodes.Call, typeof (Driver).GetMethod ("Foo"));
		ilg.Emit (OpCodes.Ret);

		int res = (int) method_builder.Invoke (null, new object[] {new Driver()});
		return res == -99 ? 0 : 1;
	}
}
