using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace GenericSize
{
    class GenericSize 
    {
    static int Iterations = 10000;
		static AssemblyBuilder assembly;
		static ModuleBuilder module;
		static string ASSEMBLY_NAME = "MonoTests.System.Reflection.Emit.TypeBuilderTest";

		static void SetUp ()
		{
			AssemblyName assemblyName = new AssemblyName ();
			assemblyName.Name = ASSEMBLY_NAME;

			assembly =
				Thread.GetDomain ().DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Run);

			module = assembly.DefineDynamicModule ("module1");
		}

		static int Main()
		{
			SetUp ();

			TypeBuilder tb = module.DefineType ("Test", TypeAttributes.Public);
			tb.DefineGenericParameters ("T");
			var tb_ctor = tb.DefineDefaultConstructor (MethodAttributes.Public);

			var tb2 = module.DefineType ("Test2", TypeAttributes.Public);
			var g0 = tb2.DefineGenericParameters ("T");

			var mb = tb2.DefineMethod ("Foo", MethodAttributes.Public | MethodAttributes.Static, typeof (object), new Type [0]);

			var il = mb.GetILGenerator();
			il.Emit(OpCodes.Newobj, TypeBuilder.GetConstructor (tb.MakeGenericType (g0), tb_ctor));
			il.Emit(OpCodes.Ret);

			var t1 = tb.CreateType ();
			var t2 = tb2.CreateType ();

			var ginst = t2.MakeGenericType (typeof (string));
			var method = ginst.GetMethod ("Foo", BindingFlags.Public | BindingFlags.Static);

			var lst = new List<Object>();

			for (int i = 0; i < GenericSize.Iterations; i++) {
				lst.Add (method.Invoke (null, null));
				if (i % 15 == 0)
					GC.Collect();
			}

			return 0;
		}
	}
}
