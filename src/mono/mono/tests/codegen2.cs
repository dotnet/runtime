using System;
using System.Reflection;
using System.Reflection.Emit;

class CGen {

	public static int Main() {
		AssemblyBuilder abuilder;
		ModuleBuilder mbuilder;
		TypeBuilder tbuilder, tb2;
		FieldBuilder fbuilder;
		PropertyBuilder pbuilder;
		ILGenerator ilg;
		AssemblyName an;
		String name = "tcgen.exe";
		MethodBuilder get_method, entryp;
		TypeAttributes attrs = TypeAttributes.Public | TypeAttributes.Class;
		MethodAttributes mattrs = MethodAttributes.Public | MethodAttributes.Static;

		an = new AssemblyName ();
		an.Name = name;
		abuilder = AppDomain.CurrentDomain.DefineDynamicAssembly (an, AssemblyBuilderAccess.Save);

		mbuilder = abuilder.DefineDynamicModule (name, name);

		tbuilder = mbuilder.DefineType ("Test.CodeGen", attrs);
		Type result = typeof(int);
		Type[] param = new Type[] {typeof (String[])};
		entryp = tbuilder.DefineMethod("Main", mattrs, result, param);
		ilg = entryp.GetILGenerator (128);
		ilg.DeclareLocal (typeof(int));
		Label fail = ilg.DefineLabel ();
		ilg.Emit (OpCodes.Ldc_I4_2);
		ilg.Emit (OpCodes.Dup);
		ilg.Emit (OpCodes.Add);
		ilg.Emit (OpCodes.Stloc_0);
		ilg.Emit (OpCodes.Ldc_I4_4);
		ilg.Emit (OpCodes.Ldloc_0);
		ilg.Emit (OpCodes.Sub);
		ilg.Emit (OpCodes.Brfalse, fail);
			ilg.Emit (OpCodes.Ldc_I4_1);
			ilg.Emit (OpCodes.Ret);
		ilg.MarkLabel (fail);
		ilg.Emit (OpCodes.Ldc_I4_0);
		ilg.Emit (OpCodes.Ret);
		

		fbuilder = tbuilder.DefineField ("int_field", typeof(int), FieldAttributes.Private);
		pbuilder = tbuilder.DefineProperty ("FieldI", PropertyAttributes.None, typeof(int), null);
		get_method = tbuilder.DefineMethod("get_FieldI", MethodAttributes.Public, result, null);
		ilg = get_method.GetILGenerator (128);
		ilg.Emit (OpCodes.Ldloc_0);
		ilg.Emit (OpCodes.Ldloc_0);
		ilg.Emit (OpCodes.Ceq);
		ilg.Emit (OpCodes.Ret);
		pbuilder.SetGetMethod (get_method);

		Type t = tbuilder.CreateType ();
		abuilder.SetEntryPoint (entryp);
		abuilder.Save (name);
		return 0;
	}
}
