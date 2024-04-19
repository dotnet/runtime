// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/*
if there are 3 classes A,B and C, and B inherits from A and C inherits from B, 
and classes A and B have a public virtual newslot method M(), calling C::M() 
on and instance of C calls the implenation of M() on A if class B is generic. 
The implementation of M() on B should be called. C# will not generate the
function call DerivedDerived::Method() since the method is not defined on this type
*/

.assembly extern mscorlib {}
.assembly 'dev10_715437' {}


.class public DerivedDerived extends class Derived`2<string,int32>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
	ldarg.0
	call instance void class Derived`2<string,int32>::.ctor()
	ret
  }
}
.class public Derived`2<T0, T1> extends Base
{
  .method public hidebysig newslot virtual instance string Method() cil managed { 
    ldstr "Derived::Method"
    ret
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
  	ldarg.0
	call instance void Base::.ctor()
	ret
  }
}
.class public Base
{
  .method public hidebysig newslot virtual instance string Method() cil managed { 
    ldstr "Base::Method"
    ret
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
  	ldarg.0
	call instance void [mscorlib]System.Object::.ctor()
	ret
  }
}




.class public auto ansi beforefieldinit Framework
{
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

		ldstr "Derived::Method"
		newobj instance void DerivedDerived::.ctor()
		callvirt instance string DerivedDerived::Method()
		call int32 Framework::CheckResult(string,string)
        ret
    }
}
