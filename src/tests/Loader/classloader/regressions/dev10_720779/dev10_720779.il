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
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
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
		call instance void [System.Runtime]System.Object::.ctor()
		ret
	}
}
.class interface public abstract IBaseInterface2`2<T0,T1> 
{
	.method public hidebysig newslot abstract virtual instance string Method() cil managed { 
	}
}




.class public sealed myClosedDel extends [System.Runtime]System.MulticastDelegate {
  .method public hidebysig specialname rtspecialname instance void .ctor(object 'object', native int 'method') runtime managed {}
  .method public hidebysig newslot virtual instance class [System.Runtime]System.IAsyncResult BeginInvoke(class [System.Runtime]System.AsyncCallback callback, object 'object') runtime managed {}
  .method public hidebysig newslot virtual instance string Invoke() runtime managed {}
  .method public hidebysig newslot virtual instance string EndInvoke(class [System.Runtime]System.IAsyncResult result) runtime managed {}
}
.class public auto ansi beforefieldinit Framework {
	.method public hidebysig static string DelCall0(object inst, class [System.Runtime]System.Type t, string mName) cil managed {

		.param [2]
		.custom instance void [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute::.ctor(valuetype [System.Runtime]System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes) = ( 01 00 08 00 00 00 00 00 ) 
	
		//CS:
	    //Delegate d = Delegate.CreateDelegate(typeof(myClosedDel), inst, t.GetMethod(mName));
        //return (string)d.DynamicInvoke(null);

		.maxstack  10
		ldtoken    myClosedDel
		call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
		ldarg.0
		ldarg.1
		ldarg.2
		callvirt   instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string)
		call       class [System.Runtime]System.Delegate [System.Runtime]System.Delegate::CreateDelegate(class [System.Runtime]System.Type,object,class [System.Runtime]System.Reflection.MethodInfo)
		ldnull
		callvirt   instance object [System.Runtime]System.Delegate::DynamicInvoke(object[])
		castclass  [System.Runtime]System.String
		ret
	}
	.method private hidebysig static int32  CheckResult(string expected, string actual) cil managed {
	  .maxstack  8
	    ldstr      "EXPECTED : "
	    ldarg.0
	    call       string [System.Runtime]System.String::Concat(string,string)
	    call       void [System.Console]System.Console::WriteLine(string)
	    ldstr      "ACTUAL   : "
	    ldarg.1
	    call       string [System.Runtime]System.String::Concat(string,string)
	    call       void [System.Console]System.Console::WriteLine(string)
	    ldarg.0
	    ldarg.1
	    call       bool [System.Runtime]System.String::op_Inequality(string,string)
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
		call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
		ldstr "Method"
		call string Framework::DelCall0(object,class [System.Runtime]System.Type,string)
		call int32 Framework::CheckResult(string,string)

		
        ret
    }
}
