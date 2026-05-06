using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace TestApp
{
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
			SetUp ();

			TypeBuilder iface = module.DefineType ("IFace", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
			iface.DefineGenericParameters ("T");

			TypeBuilder parent = module.DefineType ("Parent", TypeAttributes.Public);
	
			TypeBuilder child = module.DefineType ("Child", TypeAttributes.Public, parent);

			TypeBuilder main = module.DefineType ("Main", TypeAttributes.Public);

			child.AddInterfaceImplementation (iface.MakeGenericType (new Type [] { typeof(int) }));

			iface.DefineMethod ("Foo", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual, typeof(void), Type.EmptyTypes);

			ConstructorBuilder parent_constructor = parent.DefineDefaultConstructor (MethodAttributes.Public);

			ConstructorBuilder ctor = child.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
			ILGenerator ig = ctor.GetILGenerator ();
			ig.Emit (OpCodes.Ldarg_0);
			ig.Emit (OpCodes.Call, parent_constructor);
			ig.Emit (OpCodes.Ret);

			MethodBuilder foo_mb = child.DefineMethod ("Foo", MethodAttributes.Public | MethodAttributes.Virtual, typeof (void), Type.EmptyTypes);
			foo_mb.GetILGenerator ().Emit (OpCodes.Ret);


			MethodBuilder main_mb = main.DefineMethod ("Main", MethodAttributes.Public | MethodAttributes.Static, typeof (void), Type.EmptyTypes);
			ig = main_mb.GetILGenerator ();
			ig.Emit (OpCodes.Newobj, ctor);
			ig.Emit (OpCodes.Callvirt, foo_mb);
			ig.Emit (OpCodes.Ret);

			iface.CreateType ();

			parent.CreateType ();

			child.CreateType ();

			Type t = main.CreateType ();

			MethodInfo method = t.GetMethod ("Main");
			method.Invoke (null, null);

			/*Type gtd = typeof (A<>);
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
				type.GetConstructors ();
			} catch (Exception e) {
				Console.WriteLine ("fully open instantiation of TypeBuilder must have GetConstructors working {0}", e);
				return 4;
			}


			try {
				oires.GetConstructors ();
			} catch (Exception e) {
				Console.WriteLine ("fully open instantiation of the TypeBuilder created type must have GetConstructors working {0}", e);
				return 5;
			}*/

			return 0;
		}
	}
}
