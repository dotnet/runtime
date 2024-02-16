// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/*
We basically have an interface I<T,U>, class Base that implements I<string,int> and class Derived 
that inherits from Base, and implements I<int,int>. I<T,U> has a method string Method(). 

Creating this delegate throws the assert: 
	-> Delegate.CreateDelegate(typeof(myClosedDel), new Derived(), typeof(I<string,int>.GetMethod("Method"));

where MyClosedDel is: 
	-> public delegate string MyClosedDel ();


*/
.assembly extern mscorlib {}
.assembly 'dev10_720779' {}



.class public Derived extends Base implements class IBaseInterface2`2<int32,int32>
{
	.method public hidebysig virtual instance string Method() cil managed { 
		ldstr "Derived::Method"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void Base::.ctor()
		ret
	}
}
.class public Base implements class IBaseInterface2`2<string,int32>
{
	.method public hidebysig virtual instance string Method() cil managed { 
		ldstr "Base::Method"
		ret
	}
	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed { 
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
.class interface public abstract IBaseInterface2`2<T0,T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method() cil managed { 
	}
}




.class public sealed myClosedDel extends [mscorlib]System.MulticastDelegate {
  .method public hidebysig specialname rtspecialname instance void .ctor(object 'object', native int 'method') runtime managed {}
  .method public hidebysig newslot virtual instance class [mscorlib]System.IAsyncResult BeginInvoke(class [mscorlib]System.AsyncCallback callback, object 'object') runtime managed {}
  .method public hidebysig newslot virtual instance string Invoke() runtime managed {}
  .method public hidebysig newslot virtual instance string EndInvoke(class [mscorlib]System.IAsyncResult result) runtime managed {}
}
.class public auto ansi beforefieldinit Framework {
	.method public hidebysig static string DelCall0(object inst, class [mscorlib]System.Type t, string mName) cil managed {
	
		//CS:
	    //Delegate d = Delegate.CreateDelegate(typeof(myClosedDel), inst, t.GetMethod(mName));
        //return (string)d.DynamicInvoke(null);

		.maxstack  10
		ldtoken    myClosedDel
		call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		ldarg.0
		ldarg.1
		ldarg.2
		callvirt   instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
		call       class [mscorlib]System.Delegate [mscorlib]System.Delegate::CreateDelegate(class [mscorlib]System.Type,object,class [mscorlib]System.Reflection.MethodInfo)
		ldnull
		callvirt   instance object [mscorlib]System.Delegate::DynamicInvoke(object[])
		castclass  [mscorlib]System.String
		ret
	}
	.method private hidebysig static int32  CheckResult(string expected, string actual) cil managed {
	  .maxstack  8
	    ldstr      "EXPECTED : "
	    ldarg.0
	    call       string [mscorlib]System.String::Concat(string,string)
	    call       void [System.Console]System.Console::WriteLine(string)
	    ldstr      "ACTUAL   : "
	    ldarg.1
	    call       string [mscorlib]System.String::Concat(string,string)
	    call       void [System.Console]System.Console::WriteLine(string)
	    ldarg.0
	    ldarg.1
	    call       bool [mscorlib]System.String::op_Inequality(string,string)
	    brfalse.s  PASSING
		
		ldstr "FAIL"
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s   99
		br END
	  
	  PASSING:  
		ldstr "PASS"
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s   100
		
	  END:
        ret
	  
	} // end of method Test::CheckResult
    .method public hidebysig static int32  Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack  10
        .locals init (object V_0)
		

		//EXPECTED RETURN: "Derived::Method"
		ldstr "Derived::Method"
		newobj instance void class Derived::.ctor()
		ldtoken class IBaseInterface2`2<string,int32>
		call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
		ldstr "Method"
		call string Framework::DelCall0(object,class [mscorlib]System.Type,string)
		call int32 Framework::CheckResult(string,string)

		
        ret
    }
}
