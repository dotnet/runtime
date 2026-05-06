using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Diagnostics;

public class A
{
	public static Exception Caught;

	public static void ThrowMe()
	{
		Exception e;
		try
		{
			throw new Exception("test");
		}
		catch (Exception e2)
		{
			e = e2;
		}

		var edi = ExceptionDispatchInfo.Capture(e);

		edi.Throw();
	}

	public static void Handler(Exception e)
	{
		Caught = e;
	}
}

public class Example
{
	public static int Main()
	{
		TT();
		string expected = A.Caught.StackTrace.ToString ();

		for (int i = 0; i < 1000; ++i) {
			Thread t = new Thread (delegate () {
					TT ();
				});
			t.Start ();
			t.Join ();
			GC.Collect ();
			GC.WaitForPendingFinalizers ();
			if (A.Caught.StackTrace != expected) {
				Console.WriteLine ("FAILED");
				return 1;
			}
		}
		return 0;
	}

	static void TT()
	{
		DynamicMethod multiplyHidden = new DynamicMethod(
			"",
			typeof(void), new[] { typeof(int) }, typeof(Example));

		ILGenerator ig = multiplyHidden.GetILGenerator();

		ig.BeginExceptionBlock();

		ig.Emit(OpCodes.Call, typeof(A).GetMethod("ThrowMe"));

		ig.BeginCatchBlock(typeof(Exception));

		ig.Emit(OpCodes.Call, typeof(A).GetMethod("Handler"));

		ig.EndExceptionBlock();

		ig.Emit(OpCodes.Ret);

		var invoke = (Action<int>)
			multiplyHidden.CreateDelegate(
				typeof(Action<int>)

			);

		invoke(1);
	}
}
