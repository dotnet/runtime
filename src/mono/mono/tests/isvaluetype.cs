using System;

namespace test {

	public class Test {
		public static int  Main (string[] args) {
			int verbose = 0;
			if (args.Length > 0 && args[0] == "-v")
				verbose++;

			if (verbose != 0)
				Console.WriteLine ("ValueType is valuetype: "+ typeof(System.ValueType).IsValueType.ToString());
			if (typeof(System.ValueType).IsValueType)
				return 1;
			if (verbose != 0)
				Console.WriteLine ("Enum is valuetype: "+ typeof(System.Enum).IsValueType.ToString());
			if (typeof(System.Enum).IsValueType)
				return 2;
			if (verbose != 0)
				Console.WriteLine ("TypeAttributes is valuetype: "+ typeof(System.Reflection.TypeAttributes).IsValueType.ToString());
			if (!typeof(System.Reflection.TypeAttributes).IsValueType)
				return 3;
			if (verbose != 0)
				Console.WriteLine ("TypeAttributes is enum: "+ typeof(System.Reflection.TypeAttributes).IsEnum.ToString());
			if (!typeof(System.Reflection.TypeAttributes).IsEnum)
				return 4;
			if (verbose != 0)
				Console.WriteLine ("Enum is enum: "+ typeof(System.Enum).IsEnum.ToString());
			if (typeof(System.Enum).IsEnum)
				return 5;
			if (verbose != 0)
				Console.WriteLine ("Int32 is valuetype: "+ typeof(System.Int32).IsValueType.ToString());
			if (!typeof(System.Int32).IsValueType)
				return 6;
			return 0;
		}
	}
}
