using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace TestApp
{
	public class A<T> {
		public T fld;
	}

    class Program
    {
		static AssemblyBuilder assembly;
		static ModuleBuilder module;
		static string ASSEMBLY_NAME = "MonoTests.System.Reflection.Emit.TypeBuilderTest";

		static void SetUp ()
		{
			AssemblyName assemblyName = new AssemblyName ();
			assemblyName.Name = ASSEMBLY_NAME;

			assembly =
				Thread.GetDomain ().DefineDynamicAssembly (
					assemblyName, AssemblyBuilderAccess.RunAndSave, ".");

			module = assembly.DefineDynamicModule ("module1", "Module1.dll");
		}

		static int Main()
		{
			Type gtd = typeof (A<>);
			Type oi = gtd.MakeGenericType (gtd.GetGenericArguments ());

			if (oi != gtd) {
				Console.WriteLine ("fully open instantiation of static type not the same of the generic type definition");
				return 1;
			}

			SetUp ();
			TypeBuilder tb = module.DefineType ("Nullable`1", TypeAttributes.Public);
			Type[] args = tb.DefineGenericParameters ("T");
			Type type = tb.MakeGenericType (args);

			if (type == tb) {
				Console.WriteLine ("fully open instantiation of TypeBuilder is the same of the TypeBuilder");
				return 2;
			}
		
			Type res = tb.CreateType ();
			Type oires = res.MakeGenericType (res.GetGenericArguments ());

			if (res != oires) {
				Console.WriteLine ("fully open instantiation not the same of the generic type definition for the TypeBuilder created type");
				return 3;
			}

			try {
				oires.GetConstructors ();
			} catch (Exception e) {
				Console.WriteLine ("fully open instantiation of the TypeBuilder created type must have GetConstructors working {0}", e);
				return 5;
			}

			return 0;
		}
	}
}
