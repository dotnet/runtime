using System;
using System.Reflection;
using System.Reflection.Emit;


class Bla<T> {
	public T t;
}
public class Entry
{
    public static int Main()
    {
		Bla<int> d = new Bla<int>();	
		d.t = 99;
        Instance();

		AppDomain domain = AppDomain.CreateDomain ("test");
		try {
			domain.ExecuteAssembly ("Instance.exe");
		} catch (Exception e) {
			Console.WriteLine ("assembly has thrown "+e);
			return 1;
		}
        return 0;
    }

    public static void Instance()
    {
        AssemblyName name = new AssemblyName("Instance");
        AssemblyBuilder asmbuild = System.Threading.Thread.GetDomain().DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave);
        ModuleBuilder mod = asmbuild.DefineDynamicModule("Instance.exe");

        TypeBuilder G = mod.DefineType("G", TypeAttributes.Public);
        Type T = G.DefineGenericParameters("T")[0];
        Type GObj = G.MakeGenericType(new Type[] { typeof(object) });

         ConstructorBuilder Ctor = G.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
        {
            ILGenerator il = Ctor.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(object).GetConstructor(new Type[0]));
            il.Emit(OpCodes.Ret);
        }
   
		MethodBuilder Bar = G.DefineMethod("Bar", MethodAttributes.Public);
		{
            ILGenerator il = Bar.GetILGenerator();
			il.Emit(OpCodes.Ret);
		}

        MethodBuilder Foo = G.DefineMethod("Foo", MethodAttributes.Public | MethodAttributes.Static );
		{
            ILGenerator il = Foo.GetILGenerator();
		    il.Emit(OpCodes.Newobj, Ctor);
			il.Emit(OpCodes.Call, Bar);
			il.Emit(OpCodes.Ret);
		}

		TypeBuilder M = mod.DefineType("M", TypeAttributes.Public);


       MethodBuilder main = M.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static );
        {
            ILGenerator il = main.GetILGenerator();
            il.Emit(OpCodes.Call, TypeBuilder.GetMethod (GObj, Foo));
            il.Emit(OpCodes.Ret);
        }

		asmbuild.SetEntryPoint (main);
        G.CreateType();
		M.CreateType();

		asmbuild.Save("Instance.exe");

		
		Console.WriteLine("ok");
    }

}
