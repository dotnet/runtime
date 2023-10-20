// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}
/*BUG: Assert: Precondition failure: FAILED: !ContainsGenericVariables() (vm\method.cpp, Line: 1781)

The bug related to populating MethodDesc slots in generic dictionaries at runtime.

  o The dictionary entry "signature" in these cases describes the compile-time <Type>::<Method>
    pair that targets this MethodDesc.

  o The problem was specific to cases where the implementation of <Type>::<Method> was not defined
    directly on <Type> but was instead inherited from some generic base class in <Type>'s base
    class chain.
*/

.assembly extern mscorlib {}
.assembly 'dev10_710121' {}

.class public DerivedDerived
		extends class Derived`2<string,string>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}
.class public Derived`2<T0, T1> 
		extends class Base`2<int32,!T1>
{
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}
.class public Base`2<T0, T1> 
{
  .method public hidebysig newslot virtual instance string Method<M0>() cil managed 
  { 
    ldstr "Base::Method<"
    ldtoken !!M0
    call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    call string [mscorlib]System.String::Concat(object,object)
    ldstr ">"
    call string [mscorlib]System.String::Concat(object,object)
    ret
  }
  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ret
  }
}



.class public auto ansi beforefieldinit Framework
{
	.method private hidebysig static void  CheckResult(string expected, string actual) cil managed {
	  .maxstack  8
	  IL_0000:  ldstr      "EXPECTED : "
	  IL_0005:  ldarg.0
	  IL_0006:  call       string [mscorlib]System.String::Concat(string,string)
	  IL_000b:  call       void [System.Console]System.Console::WriteLine(string)
	  IL_0010:  ldstr      "ACTUAL   : "
	  IL_0015:  ldarg.1
	  IL_0016:  call       string [mscorlib]System.String::Concat(string,string)
	  IL_001b:  call       void [System.Console]System.Console::WriteLine(string)
	  IL_0020:  ldarg.0
	  IL_0021:  ldarg.1
	  IL_0022:  call       bool [mscorlib]System.String::op_Inequality(string,string)
	  IL_0027:  brfalse.s  IL_0034
	  IL_0029:  ldstr      "CheckResult FAILED!"
	  IL_002e:  newobj     instance void [mscorlib]System.Exception::.ctor(string)
	  IL_0033:  throw
	  IL_0034:  ret
	} // end of method Test::CheckResult
	
	.method static string ConstCaller<T0,T1,(class Derived`2<!!T0,!!T1>)W>(!!W 'inst') cil managed {
		.maxstack 33
		
		ldarga.s     0
		constrained. !!W
		callvirt     instance string class Derived`2<!!T0,!!T1>::Method<object>()
		
		ret
	}
    .method public hidebysig static int32  Main() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        .maxstack  3

		ldstr "Base::Method<System.Object>"
		newobj instance void DerivedDerived::.ctor()
		callvirt     instance string class Derived`2<string,string>::Method<object>()
		call void Framework::CheckResult(string,string)

		ldstr "Base::Method<System.Object>"
		newobj instance void DerivedDerived::.ctor()
		call string Framework::ConstCaller<string,string,class DerivedDerived>(!!2)
		call void Framework::CheckResult(string,string)

		ldstr "PASS"
		call       void [System.Console]System.Console::WriteLine(string)
		ldc.i4.s   100
        ret
    }
}
