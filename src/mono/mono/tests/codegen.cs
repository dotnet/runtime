using System;
using System.Reflection;
using System.Reflection.Emit;

class CGen {

	public static int Main() {
		AssemblyBuilder abuilder;
		ModuleBuilder mbuilder;
		TypeBuilder tbuilder;
		AssemblyName an;
		String name = "tcgen.exe";
		MethodBuilder method;
		TypeAttributes attrs = TypeAttributes.Public | TypeAttributes.Class;
		MethodAttributes mattrs = MethodAttributes.Public | MethodAttributes.Static;
		byte[] body = {0x2a}; // ret

		an = new AssemblyName ();
		an.Name = name;
		abuilder = AppDomain.CurrentDomain.DefineDynamicAssembly (an, AssemblyBuilderAccess.Save);

		mbuilder = abuilder.DefineDynamicModule (name, name);

		tbuilder = mbuilder.DefineType ("Test.CodeGen", attrs);
		Type result = typeof(int);
		Type[] param = new Type[] {};
		method = tbuilder.DefineMethod("Main", mattrs, result, param);
		method.CreateMethodBody (body, body.Length);
		Type t = tbuilder.CreateType ();
		abuilder.SetEntryPoint (method);
		abuilder.Save (name);
		return 0;
	}
}
