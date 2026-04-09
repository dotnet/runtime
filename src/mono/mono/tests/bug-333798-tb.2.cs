using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class Gen<T> {
        public static Gen<T>[] newSelfArr () {
                return null;
        }
}

public class Driver {
        public static void Test () {
                Gen<int>.newSelfArr ();
        }
}

public class GenericsTests
{
	static AssemblyBuilder assembly;
	static ModuleBuilder module;

	static void SetUp ()
	{
		AssemblyName assemblyName = new AssemblyName ();
		assemblyName.Name = "TestAssembly";
		assembly = 
			Thread.GetDomain ().DefineDynamicAssembly (
				assemblyName, AssemblyBuilderAccess.RunAndSave, ".");
		module = assembly.DefineDynamicModule ("module1", "TestModuleSS.dll");
    }

	public static int Main () {
		SetUp ();
		TypeBuilder tb = module.DefineType ("Gen", TypeAttributes.Public);
		Type[] args = tb.DefineGenericParameters ("T");
		Type oi = tb.MakeGenericType (args);

		MethodBuilder mb = tb.DefineMethod ("Test", MethodAttributes.Public | MethodAttributes.Static, oi.MakeArrayType (), new Type [0]);
	
		ILGenerator il = mb.GetILGenerator();
		il.Emit (OpCodes.Ldnull);
		il.Emit (OpCodes.Ret);
		tb.CreateType ();

		TypeBuilder main = module.DefineType ("Driver", TypeAttributes.Public);
		MethodBuilder mb2 = main.DefineMethod ("Test", MethodAttributes.Public | MethodAttributes.Static);

		il = mb2.GetILGenerator();
		il.Emit (OpCodes.Call, TypeBuilder.GetMethod (tb.MakeGenericType (typeof (int)), mb));
		il.Emit (OpCodes.Pop);
		il.Emit (OpCodes.Ret);
		Type tt = main.CreateType ();

		tt.GetMethod ("Test").Invoke (null, null);
		//typeof (Driver).GetMethod ("Test").Invoke (null, null);
		return 0;
	}
}
