using System;
using System.Reflection;
using System.Reflection.Emit;

class Program
{
	static int Main (string[] args) {
		Func<long?,long?> del_global = null;
		bool caught_ex;

		for (int i = 1; i < 100; ++i) {
			// Random method whose delegate invoke is not optimized away by jit
			//
			//    .method public static hidebysig
			//           default valuetype [mscorlib]System.Nullable`1<int64> StaticMethodToBeClosedOverNull (object o, valuetype [mscorlib]System.Nullable`1<int64> bar)  cil managed
			//    {
			//       IL_0000:  newobj instance void class [mscorlib]System.Exception::'.ctor'()
			//       IL_0005:  throw
			//    }
			//
			//    public static long? StaticMethodToBeClosedOverNull (object o, long? bar)
			//    {
			//         throw new Exception ();
			//    }
			DynamicMethod dm = new DynamicMethod(
				$"dm_{i}",
				typeof (Nullable<long>),
				new Type[2] { typeof (object), typeof (Nullable<long>) },
				typeof(Program).Module);

			ConstructorInfo ctorInfo = typeof (Exception).GetConstructor(Type.EmptyTypes);

			ILGenerator il = dm.GetILGenerator();
			il.Emit(OpCodes.Newobj, ctorInfo);
			il.Emit(OpCodes.Throw);

			var del = (Func<long?,long?>)dm.CreateDelegate (typeof (Func<long?,long?>));
			caught_ex = false;
			try {
				del (5);
			} catch (Exception) {
				caught_ex = true;
			}
			if (!caught_ex)
				Environment.Exit (1);
			// Make sure the finalizer thread has work to do, so it will also free dynamic methods
			new Program ();
			if (i % 50 == 0) {
				del_global = del;
				GC.Collect ();
				GC.WaitForPendingFinalizers ();
			}
		}

		GC.Collect ();
		// The delegate invoke wrapper of del_global was created for another dynamic method (that should have
		// been freed) than the one associated with this delegate. Does the delegate invocation/EH still work ?
		caught_ex = false;
		try {
			del_global (5);
		} catch (Exception) {
			caught_ex = true;
		}
		if (!caught_ex)
			Environment.Exit (2);
		return 0;
	}

	~Program ()
	{

	}
}
