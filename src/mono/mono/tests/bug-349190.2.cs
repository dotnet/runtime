using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.IO;
using System.Collections.Generic;


public class Driver
{
		public static int Main () {
			if (!TestOneAssembly ())
				return 1;
			if (!TestTwoAssemblies ())
				return 2;
			return 0;
		}

        public static bool TestTwoAssemblies ()
        {
			AssemblyBuilder assembly2 = Thread.GetDomain ().DefineDynamicAssembly (new AssemblyName ("res2"), AssemblyBuilderAccess.RunAndSave, Path.GetTempPath ());
			ModuleBuilder module2 = assembly2.DefineDynamicModule ("res2.dll");

			TypeBuilder tb2 = module2.DefineType ("ExternalType", TypeAttributes.Public | TypeAttributes.Abstract);

			MethodBuilder m_2 = tb2.DefineMethod ("m_2", MethodAttributes.Public | MethodAttributes.Static);
			Type[] gparams_m_2 = m_2.DefineGenericParameters ("T");
			m_2.SetReturnType (gparams_m_2[0]);
			m_2.SetParameters (gparams_m_2[0]);
			ILGenerator il = m_2.GetILGenerator ();
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ret);


			AssemblyBuilder assembly = Thread.GetDomain ().DefineDynamicAssembly (new AssemblyName ("res"), AssemblyBuilderAccess.RunAndSave, Path.GetTempPath ());
			ModuleBuilder module = assembly.DefineDynamicModule ("res.exe");

			TypeBuilder tb = module.DefineType ("Mono.Rocks.IEnumerable", TypeAttributes.Public | TypeAttributes.Abstract);

			MethodBuilder mb = tb.DefineMethod ("NaturalSort", MethodAttributes.Public | MethodAttributes.Static);
			Type[] gparams = mb.DefineGenericParameters ("T");
			mb.SetReturnType (typeof (IEnumerable<>).MakeGenericType (gparams));
			mb.SetParameters (typeof (IEnumerable<>).MakeGenericType (gparams));

			il = mb.GetILGenerator ();
			il.Emit (OpCodes.Ldftn, m_2);
			il.Emit (OpCodes.Pop);

			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ret);

			TypeBuilder driver = module.DefineType ("Driver", TypeAttributes.Public);
			MethodBuilder main = tb.DefineMethod ("Main", MethodAttributes.Public | MethodAttributes.Static);
			il = main.GetILGenerator ();
			il.Emit (OpCodes.Ldnull);
			il.Emit (OpCodes.Call, mb.MakeGenericMethod (typeof(int)));
			il.Emit (OpCodes.Pop);
			il.Emit (OpCodes.Ret);

			assembly.SetEntryPoint (main);

			Type t = tb.CreateType ();
			tb2.CreateType ();
			driver.CreateType ();


 			assembly2.Save ("res2.dll");
			assembly.Save ("res.exe");

			IEnumerable<int> en = new int[] { 1,2,3 };
			bool res = en == t.GetMethod ("NaturalSort").MakeGenericMethod (typeof (int)).Invoke (null, new object[] { en });

			Thread.GetDomain ().ExecuteAssembly(Path.GetTempPath () + Path.DirectorySeparatorChar +"res.exe");
			return res;
       }



        public static bool TestOneAssembly()
        {
			AssemblyBuilder assembly = Thread.GetDomain ().DefineDynamicAssembly (new AssemblyName ("ALAL"), AssemblyBuilderAccess.RunAndSave, Path.GetTempPath ());
			ModuleBuilder module = assembly.DefineDynamicModule ("res1.exe");

			TypeBuilder tb = module.DefineType ("Mono.Rocks.IEnumerable", TypeAttributes.Public | TypeAttributes.Abstract);

			MethodBuilder m_2 = tb.DefineMethod ("m_2", MethodAttributes.Private | MethodAttributes.Static);
			Type[] gparams_m_2 = m_2.DefineGenericParameters ("T");
			m_2.SetReturnType (gparams_m_2[0]);
			m_2.SetParameters (gparams_m_2[0]);

			MethodBuilder mb = tb.DefineMethod ("NaturalSort", MethodAttributes.Public | MethodAttributes.Static);
			Type[] gparams = mb.DefineGenericParameters ("T");
			mb.SetReturnType (typeof (IEnumerable<>).MakeGenericType (gparams));
			mb.SetParameters (typeof (IEnumerable<>).MakeGenericType (gparams));

			ILGenerator il = mb.GetILGenerator ();

			il.Emit (OpCodes.Ldftn, m_2);
			il.Emit (OpCodes.Pop);

			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ret);

			il = m_2.GetILGenerator ();
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Ret);

			TypeBuilder driver = module.DefineType ("Driver", TypeAttributes.Public);
			MethodBuilder main = tb.DefineMethod ("Main", MethodAttributes.Public | MethodAttributes.Static);
			il = main.GetILGenerator ();
			il.Emit (OpCodes.Ldnull);
			il.Emit (OpCodes.Call, mb.MakeGenericMethod (typeof(int)));
			il.Emit (OpCodes.Pop);
			il.Emit (OpCodes.Ret);

			assembly.SetEntryPoint (main);


			Type t = tb.CreateType ();
			driver.CreateType ();

			IEnumerable<int> en = new int[] { 1,2,3 };
			bool res = en == t.GetMethod ("NaturalSort").MakeGenericMethod (typeof (int)).Invoke (null, new object[] {en });
			assembly.Save ("res1.exe");
 			res &= en == t.GetMethod ("NaturalSort").MakeGenericMethod (typeof (int)).Invoke (null, new object[] {en });

			Thread.GetDomain ().ExecuteAssembly(Path.GetTempPath () + Path.DirectorySeparatorChar +"res1.exe");
 			return res;
       }
}
