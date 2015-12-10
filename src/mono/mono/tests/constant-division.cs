using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Test {
	public class T {
		private delegate TReturn OneParameter<TReturn, TParameter0> (TParameter0 p0);

		private static Type[] SDivisionArgs = {typeof(int)};
		private static Type[] UDivisionArgs = {typeof(uint)};

		public static int Main (string[] args) {
			if (TestSDivision (2000, 10) != 0)
				return -1;

			if (TestUDivision (2000, 10) != 0)
				return -1;

			return 0;
		}

		private static int TestSDivision (int divisions, int invokes)
		{
			int i, j;
			Random rand = new Random ();
			for (i = 0; i < divisions; i++) {
				int divisor = rand.Next (Int32.MinValue, Int32.MaxValue);

				if (divisor == 0 || divisor == -1)
					continue;

				DynamicMethod SDivision = new DynamicMethod(
					String.Format ("SDivision{0}", i),
					typeof(int),
					SDivisionArgs,
					typeof(T).Module);

				ILGenerator il = SDivision.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldc_I4, divisor);
				il.Emit(OpCodes.Div);
				il.Emit(OpCodes.Ret);

				OneParameter<int, int> invokeSDivision =
					(OneParameter<int, int>)
					SDivision.CreateDelegate(typeof(OneParameter<int, int>));

				for (j = 0; j < invokes; j++) {
					int dividend = rand.Next (Int32.MinValue, Int32.MaxValue);
					int result, expected;

					result = invokeSDivision (dividend);
					expected = dividend / divisor;

					if (result != expected) {
						Console.WriteLine("{0} / {1} = {2} != {3})", dividend, divisor, expected, result);
						return -1;
					}
				}
			}

			return 0;
		}

		private static int TestUDivision (int divisions, int invokes)
		{
			int i, j;
			Random rand = new Random ();
			for (i = 0; i < divisions; i++) {
				uint divisor = (uint)rand.Next (Int32.MinValue, Int32.MaxValue);

				if (divisor == 0)
					continue;

				DynamicMethod UDivision = new DynamicMethod(
					String.Format ("UDivision{0}", i),
					typeof(uint),
					UDivisionArgs,
					typeof(T).Module);

				ILGenerator il = UDivision.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldc_I4, divisor);
				il.Emit(OpCodes.Div_Un);
				il.Emit(OpCodes.Ret);

				OneParameter<uint, uint> invokeUDivision =
					(OneParameter<uint, uint>)
					UDivision.CreateDelegate(typeof(OneParameter<uint, uint>));

				for (j = 0; j < invokes; j++) {
					uint dividend = (uint)rand.Next (Int32.MinValue, Int32.MaxValue);
					uint result, expected;

					result = invokeUDivision (dividend);
					expected = dividend / divisor;

					if (result != expected) {
						Console.WriteLine("{0} / {1} = {2} != {3})", dividend, divisor, expected, result);
						return -1;
					}
				}
			}
			return 0;
		}
	}
}
