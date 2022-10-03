// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib
{
}
.assembly 'indcall'
{

  
}
.assembly extern xunit.core {}
.namespace JitTest_indcall_refany_il
{
  .class public auto ansi Test extends ['mscorlib']System.Object
  {
    .method private hidebysig instance int32 DoStuff(method instance int32 *(refany)) il managed
    {
			.locals (int32 N)
			.maxstack  8
			ldarg.0
			ldc.i4.7
			stloc N
			ldloca N
			mkrefany [mscorlib]System.Int32
			ldarg.1
			calli		instance int32(refany)
			ret
    } // end of method 'Test::DoStuff'
    .method private hidebysig instance int32 DummyMethod(refany) il managed
    {
			.maxstack  8
			ldarg.1
			refanytype
			call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
			ldtoken     [mscorlib]System.Int32
			call class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle )
			ceq
			brtrue OK
			newobj		instance void [mscorlib]System.Exception::.ctor()
			throw
		OK:
			ldarg.1
			refanyval  [mscorlib]System.Int32
			ldind.i4
			ldc.i4.7
			xor
			ldc.i4 100
			add
			ret
    } // end of method 'Test::DoStuff'

    .method public hidebysig static int32 Main() il managed
    {
			.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
			    01 00 00 00
			)
			.entrypoint
			.maxstack  6
			.locals (int32 V_0, int32)
			newobj		instance void JitTest_indcall_refany_il.Test::.ctor()
			ldftn 		instance int32 JitTest_indcall_refany_il.Test::DummyMethod(refany)
			ldftn		instance int32 JitTest_indcall_refany_il.Test::DoStuff(method instance int32 *(refany))
			tail. calli		instance int32 (method instance int32 *(refany))
			ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_indcall_refany_il'
