using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

class Driver {
	static int Main () {
		var dyn = DefineDynamicAssembly (AppDomain.CurrentDomain);
		var core = TriggerLoadingSystemCore ();
		var asm = AppDomain.CurrentDomain.GetAssemblies ();
	
		if (asm [0] != typeof (object).Assembly) {
			Console.WriteLine ("first assembly must be mscorlib, but it was {0}", asm [0]);
			return 1;
		}

		if (asm [1] != typeof (Driver).Assembly) {
			Console.WriteLine ("second assembly must be test assembly, but it was {0}", asm [1]);
			return 2;
		}

		if (asm [2] != dyn) {
			Console.WriteLine ("third assembly must be SRE, but it was {0}", asm [2]);
			return 3;
		}

		if (asm [3] != core) {
			Console.WriteLine ("last assembly must be System.Core, but it was {0}", asm [3]);
			return 4;
		}

		return 0;
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
	static Assembly TriggerLoadingSystemCore ()
	{
		int[] x = new int[] { 1,2,3};
		x.Where (v => v > 1);
		return typeof (Enumerable).Assembly;
	}


	static Assembly DefineDynamicAssembly (AppDomain domain)
	{
		AssemblyName assemblyName = new AssemblyName ();
		assemblyName.Name = "MyDynamicAssembly";

		AssemblyBuilder assemblyBuilder = domain.DefineDynamicAssembly (assemblyName, AssemblyBuilderAccess.Run);
		ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule ("MyDynamicModule");
		TypeBuilder typeBuilder = moduleBuilder.DefineType ("MyDynamicType", TypeAttributes.Public);
		ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard, null);
		ILGenerator ilGenerator = constructorBuilder.GetILGenerator ();
		ilGenerator.EmitWriteLine ("MyDynamicType instantiated!");
		ilGenerator.Emit (OpCodes.Ret);
		typeBuilder.CreateType ();
		return assemblyBuilder;
	}
	
}
