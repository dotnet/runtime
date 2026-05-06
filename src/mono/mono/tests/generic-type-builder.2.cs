/* This test case is taken verbatim from the corlib test suite.  The
 * reason we need it here, too, is that the corlib tests don't run
 * with generic code sharing enabled for all code.  Once that is
 * enabled by default in Mono, this test should be removed from the
 * runtime test suite.
 */

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;

public class main {
	private static AssemblyBuilder assembly;

	private static ModuleBuilder module;

	static string ASSEMBLY_NAME = "MonoTests.System.Reflection.Emit.TypeBuilderTest";

	protected static void SetUp ()
	{
		AssemblyName assemblyName = new AssemblyName ();
		assemblyName.Name = ASSEMBLY_NAME;

		assembly =
			Thread.GetDomain ().DefineDynamicAssembly (
				assemblyName, AssemblyBuilderAccess.RunAndSave, Path.GetTempPath ());

		module = assembly.DefineDynamicModule ("module1");
	}

	public static int GetField ()
	{
		TypeBuilder tb = module.DefineType ("bla", TypeAttributes.Public);
		GenericTypeParameterBuilder [] typeParams = tb.DefineGenericParameters ("T");

		ConstructorBuilder cb = tb.DefineDefaultConstructor (MethodAttributes.Public);

		FieldBuilder fb1 = tb.DefineField ("field1", typeParams [0], FieldAttributes.Public);

		Type t = tb.MakeGenericType (typeof (int));

		// Chect that calling MakeArrayType () does not initialize the class
		// (bug #351172)
		t.MakeArrayType ();

		// Check that the instantiation of a type builder contains live data
		TypeBuilder.GetField (t, fb1);
		FieldBuilder fb2 = tb.DefineField ("field2", typeParams [0], FieldAttributes.Public);
		FieldInfo fi2 = TypeBuilder.GetField (t, fb1);

		MethodBuilder mb = tb.DefineMethod ("get_int", MethodAttributes.Public|MethodAttributes.Static, typeof (int), Type.EmptyTypes);
		ILGenerator ilgen = mb.GetILGenerator ();
		ilgen.Emit (OpCodes.Newobj, TypeBuilder.GetConstructor (t, cb));
		ilgen.Emit (OpCodes.Dup);
		ilgen.Emit (OpCodes.Ldc_I4, 42);
		ilgen.Emit (OpCodes.Stfld, fi2);
		ilgen.Emit (OpCodes.Ldfld, fi2);
		ilgen.Emit (OpCodes.Ret);

		// Check GetField on a type instantiated with type parameters
		Type t3 = tb.MakeGenericType (typeParams [0]);
		FieldBuilder fb3 = tb.DefineField ("field3", typeParams [0], FieldAttributes.Public);
		FieldInfo fi3 = TypeBuilder.GetField (t3, fb3);

		MethodBuilder mb3 = tb.DefineMethod ("get_T", MethodAttributes.Public|MethodAttributes.Static, typeParams [0], Type.EmptyTypes);
		ILGenerator ilgen3 = mb3.GetILGenerator ();
		ilgen3.Emit (OpCodes.Newobj, TypeBuilder.GetConstructor (t3, cb));
		ilgen3.Emit (OpCodes.Ldfld, fi3);
		ilgen3.Emit (OpCodes.Ret);

		Type created = tb.CreateType ();

		Type inst = created.MakeGenericType (typeof (object));

		if ((int)(inst.GetMethod ("get_int").Invoke (null, null)) != 42)
			return 1;

		if (inst.GetMethod ("get_T").Invoke (null, null) != null)
			return 1;

		return 0;
	}

	public static int Main () {
		SetUp ();
		return GetField ();
	}
}
