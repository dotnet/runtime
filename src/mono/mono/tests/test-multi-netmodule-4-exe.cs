// Compiler options: -r:test-multi-netmodule-2-dll1.dll

using System;
using System.Reflection;

public class M4 {
	public static int Main () {
		M2 m2 = new M2();

		// Expecting failure
		try {
			var DLL = Assembly.LoadFile(@"test-multi-netmodule-3-dll2.dll");
	        var m3Type = DLL.GetType("M3");
	        var m3 = Activator.CreateInstance(m3Type);
	        var m3m1Field = m3Type.GetField("m1");

    		Console.WriteLine("M3    assembly:" + m3Type.Assembly);
			Console.WriteLine("M3.M1 assembly:" + m3m1Field.DeclaringType.Assembly);
        } catch (System.TypeLoadException) {
        	return 0;
        }

		Console.WriteLine("M2    assembly:" + typeof (M2).Assembly);
		Console.WriteLine("M2.M1 assembly:" + m2.m1.GetType().Assembly);

		return 1;
	}
}
