using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

public class Tests
{
	public static int Main (String[] args) {
		AssemblyName assemblyName = new AssemblyName ();
		assemblyName.Name = "foo";

		AssemblyBuilder assembly =
			Thread.GetDomain ().DefineDynamicAssembly (
													   assemblyName, AssemblyBuilderAccess.RunAndSave);

		ModuleBuilder module = assembly.DefineDynamicModule ("foo.dll");

		TypeBuilder if_tb = module.DefineType ("IF`1", TypeAttributes.Public|TypeAttributes.Abstract|TypeAttributes.Interface);

		GenericTypeParameterBuilder [] typeParams = if_tb.DefineGenericParameters ("T");

		MethodBuilder if_mb = if_tb.DefineMethod ("foo", MethodAttributes.Public|MethodAttributes.Abstract|MethodAttributes.Virtual, typeParams [0], Type.EmptyTypes);
		MethodBuilder if_mb2 = if_tb.DefineMethod ("foo2", MethodAttributes.Public|MethodAttributes.Abstract|MethodAttributes.Virtual, typeParams [0], Type.EmptyTypes);


		TypeBuilder tb = module.DefineType ("Foo`1", TypeAttributes.Public, typeof (object));
		GenericTypeParameterBuilder [] tbTypeParams = tb.DefineGenericParameters ("T");


		Type inst = if_tb.MakeGenericType (tbTypeParams [0]);

		tb.AddInterfaceImplementation (inst);

		var mb0 = tb.DefineMethod ("foo", MethodAttributes.Public|MethodAttributes.Virtual, typeParams [0], Type.EmptyTypes);
		mb0.GetILGenerator ().Emit (OpCodes.Ret);

		var mb = tb.DefineMethod ("__foo", MethodAttributes.Public|MethodAttributes.Virtual, typeParams [0],Type.EmptyTypes);
		var gen = mb.GetILGenerator ();
		var local = gen.DeclareLocal (typeParams [0], false);
		gen.Emit (OpCodes.Ldloca, local);
		gen.Emit (OpCodes.Initobj, typeParams [0]);
		gen.Emit (OpCodes.Ldloc, local);
		gen.Emit (OpCodes.Ret);

		tb.DefineMethodOverride (mb, TypeBuilder.GetMethod (inst, if_mb));
		tb.DefineMethodOverride (mb, TypeBuilder.GetMethod (inst, if_mb2));

		var k = if_tb.CreateType ();

		Type t = tb.CreateType ();
		object obj = Activator.CreateInstance (t.MakeGenericType (new Type [] { typeof (string)}));
		var info = k.MakeGenericType (new Type [] { typeof (string) }).GetMethod ("foo");
		var res = info.Invoke (obj, null);

		return res == null ? 0 : 1;
	}
}

