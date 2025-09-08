//
// Not sure how we could automate this test, for now you have to look at the
// generated output (b.dll) and see that the signature for the method called
// (the one with no name :-) looks like this:
//
//  call       instance int32 (bool,
//             class System.String modreq([mscorlib]System.Runtime.CompilerServices.IsBoxed),
//             unsigned int8 modopt([mscorlib]System.Runtime.CompilerServices.IsConst),
//             int32 modopt([mscorlib]System.Runtime.CompilerServices.IsConst),
//             int64 modopt([mscorlib]System.Runtime.CompilerServices.IsConst))

    
using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

public class EmitHelloWorld{
    static void Main(string[] args)
    {
        AssemblyName an = new AssemblyName();
        an.Name = "HelloWorld";
        AssemblyBuilder ab = Thread.GetDomain().DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
        ModuleBuilder module = ab.DefineDynamicModule("b.dll");
        TypeBuilder tb = module.DefineType("type", TypeAttributes.Public | TypeAttributes.Class);
        MethodBuilder mb = tb.DefineMethod("test",
					   MethodAttributes.HideBySig | MethodAttributes.Static |
					   MethodAttributes.Public, typeof(void), null);
        ILGenerator ig = mb.GetILGenerator();

	//
	// This is the actual test:
	//   Generate a method signature that contains modopts and modreqs
	//   and call that.  It has no name or anything, not sure how this
	//   is actually used, but we at least generate the stuff
	//
	SignatureHelper sh = SignatureHelper.GetMethodSigHelper (module, CallingConventions.HasThis, typeof(int));
	sh.AddArgument (typeof (bool));
	Type [] req = new Type [] { typeof (System.Runtime.CompilerServices.IsBoxed) };
	sh.AddArgument (typeof (string), req, null);
	Type [] opt = new Type [] { typeof (System.Runtime.CompilerServices.IsConst) };
	sh.AddArgument (typeof (byte), null, opt);
	sh.AddArgument (typeof (int), null, opt);
	sh.AddArgument (typeof (long), null, opt);
	ig.Emit (OpCodes.Call, sh);

        ig.Emit(OpCodes.Ret);

        tb.CreateType();

	ab.Save ("b.dll");
     }
}