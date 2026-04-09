using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

/*public delegate void FooDelegate ();*/

public class EmitTest
{
	static ConstructorBuilder ctor;
	static MethodBuilder targetMethod;
	static MethodBuilder testEvents;
	static Type fooOpenInst;
	static Type[] genericArgs;

	static void EmitCtor (TypeBuilder genericFoo) {
		ConstructorBuilder mb = genericFoo.DefineConstructor (MethodAttributes.Public, CallingConventions.Standard, null);
		ILGenerator il = mb.GetILGenerator ();
		for (int i = 0; i < 20; ++i)
				il.Emit (OpCodes.Nop);

		il.Emit (OpCodes.Ldarg_0);
		il.Emit (OpCodes.Call, typeof (object).GetConstructors()[0]);
		il.Emit (OpCodes.Ret);

		ctor = mb;
	}

	static void EmitTestEvents (TypeBuilder genericFoo) {
		MethodBuilder mb = genericFoo.DefineMethod ("TestEvents", MethodAttributes.Public, typeof (void), null);
		ILGenerator il = mb.GetILGenerator ();
		for (int i = 0; i < 20; ++i)
				il.Emit (OpCodes.Nop);

		il.Emit (OpCodes.Ldarg_0);
		il.Emit (OpCodes.Ldnull);
		il.Emit (OpCodes.Callvirt, targetMethod);

		il.Emit (OpCodes.Ret);

		testEvents = mb;
	}


	static void EmitTargetMethod (TypeBuilder genericFoo) {
		MethodBuilder mb = genericFoo.DefineMethod ("TargetMethod", MethodAttributes.Public, typeof (void), new Type[] {typeof (object) });
		ILGenerator il = mb.GetILGenerator ();

		for (int i = 0; i < 20; ++i)
				il.Emit (OpCodes.Nop);

		il.Emit (OpCodes.Ldtoken, genericArgs [0]);
		il.Emit (OpCodes.Call, typeof (Type).GetMethod ("GetTypeFromHandle"));
		il.Emit (OpCodes.Call, typeof (Console).GetMethod ("WriteLine", new Type[] { typeof (object) }));
		il.Emit (OpCodes.Ret);

		targetMethod = mb;
	}

	public static int Main () {
		AssemblyName assemblyName = new AssemblyName();
		assemblyName.Name = "customMod";
		assemblyName.Version = new Version (1, 2, 3, 4);

		AssemblyBuilder assembly 
			= Thread.GetDomain().DefineDynamicAssembly(
				  assemblyName, AssemblyBuilderAccess.RunAndSave);

		ModuleBuilder module = assembly.DefineDynamicModule("res.exe", "res.exe");

		TypeBuilder genericFoo = module.DefineType ("GenericFoo", TypeAttributes.Public, typeof (object));
		genericArgs = genericFoo.DefineGenericParameters ("T");
		fooOpenInst = genericFoo.MakeGenericType (genericArgs);

		EmitCtor (genericFoo);
		EmitTargetMethod (genericFoo);
		EmitTestEvents (genericFoo);

		TypeBuilder moduletype = module.DefineType ("ModuleType", TypeAttributes.Public, typeof (object));
		MethodBuilder main = moduletype.DefineMethod ("Main", MethodAttributes.Public | MethodAttributes.Static, typeof (void), null);
		ILGenerator il = main.GetILGenerator ();

		Type strInst = genericFoo.MakeGenericType (typeof (string));
		il.Emit (OpCodes.Newobj, TypeBuilder.GetConstructor (strInst, ctor));
		il.Emit (OpCodes.Callvirt, TypeBuilder.GetMethod (strInst, testEvents));
		il.Emit (OpCodes.Ret);

		genericFoo.CreateType ();
		Type res = moduletype.CreateType ();
	
		res.GetMethod ("Main").Invoke (null, null);
		return 0;
	}

}

