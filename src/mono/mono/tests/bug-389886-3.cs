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
		static Type[] args;
		static ConstructorBuilder delegate_ctor;
		static MethodBuilder invoke_mb;
		static string ASSEMBLY_NAME = "MonoTests.System.Reflection.Emit.TypeBuilderTest";

		static void SetUp ()
		{
			AssemblyName assemblyName = new AssemblyName ();
			assemblyName.Name = ASSEMBLY_NAME;

			assembly =
				Thread.GetDomain ().DefineDynamicAssembly (
					assemblyName, AssemblyBuilderAccess.RunAndSave, ".");

			module = assembly.DefineDynamicModule ("module1", "bla.exe");
		}

		static TypeBuilder DefineDelegate () {
		   TypeBuilder typeBuilder = module.DefineType( "MyDelegate", 
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed, 
				typeof (object) );
			args = typeBuilder.DefineGenericParameters ("TIn", "TOut");

			delegate_ctor = typeBuilder.DefineConstructor( 
				MethodAttributes.Public | MethodAttributes.HideBySig | 
				MethodAttributes.RTSpecialName | MethodAttributes.SpecialName, 
				CallingConventions.Standard, 
				new Type[] { typeof(Object), typeof (IntPtr) } ); 

			delegate_ctor.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed ); 

			invoke_mb = typeBuilder.DefineMethod( 
				"Invoke", 
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, 
				args [1], 
				new Type[] { args [0] } ); 

			invoke_mb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed ); 

			MethodBuilder mb = typeBuilder.DefineMethod( 
				"BeginInvoke", 
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, 
				typeof (IAsyncResult), 
				new Type[] { args [0], typeof (AsyncCallback), typeof (object) } ); 

			mb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed ); 

			mb = typeBuilder.DefineMethod( 
				"EndInvoke", 
				MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig, 
				args [1], 
				new Type[] {  typeof (IAsyncResult) } ); 

			mb.SetImplementationFlags( MethodImplAttributes.Runtime | MethodImplAttributes.Managed ); 

			return typeBuilder;
		}

		static int Main()
		{
			SetUp ();

			TypeBuilder tb = DefineDelegate ();
			TypeBuilder main = module.DefineType ("Main", TypeAttributes.Public);
			/* >>>move this to after the SetParent call and things will work<<< */
			Type inst = tb.MakeGenericType (new Type[] {typeof (double), typeof(int)});

			tb.SetParent (typeof( System.MulticastDelegate));

			ConstructorBuilder ctor = main.DefineDefaultConstructor (MethodAttributes.Public);

			MethodBuilder foo_mb = main.DefineMethod("Foo",  MethodAttributes.Public, typeof (int), new Type[] { typeof (double) } ); 
			ILGenerator ig = foo_mb.GetILGenerator ();
			ig.Emit (OpCodes.Ldc_I4_0);
			ig.Emit (OpCodes.Ret);


			MethodBuilder main_mb = main.DefineMethod ("Main", MethodAttributes.Public | MethodAttributes.Static, typeof (int), Type.EmptyTypes);
			ig = main_mb.GetILGenerator ();

			ig.Emit (OpCodes.Newobj, ctor);
			ig.Emit (OpCodes.Ldftn, foo_mb);
			ig.Emit (OpCodes.Newobj, TypeBuilder.GetConstructor (inst, delegate_ctor));
			ig.Emit (OpCodes.Ldc_R8, 2.2);
			ig.Emit (OpCodes.Callvirt, TypeBuilder.GetMethod (inst, invoke_mb));

			ig.Emit (OpCodes.Ret);

			tb.CreateType ();

			Type t = main.CreateType ();

			MethodInfo method = t.GetMethod ("Main");
			method.Invoke (null, null);

			return 0;
		}
	}
}
