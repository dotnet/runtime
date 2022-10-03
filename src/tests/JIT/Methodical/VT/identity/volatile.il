// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'volatile' // // as "bug"
{
}
.assembly extern xunit.core {}
// MVID: {A29CDE84-6701-43B3-B56A-DE8480CE7421}
.namespace JitTest_volatile_il
{
  .class value public auto ansi sealed VT extends [mscorlib]System.ValueType
  {
  	.field public int32 m_n
    .method public hidebysig static int32 Main() il managed
    {
		.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
		    01 00 00 00
		)
		.entrypoint
		.maxstack  8
		.locals (value class JitTest_volatile_il.VT src, value class JitTest_volatile_il.VT dst)
		ldloca src
		initobj JitTest_volatile_il.VT
		ldloc src
		ldloca src
		ldc.i4 1000
		stfld int32 JitTest_volatile_il.VT::m_n
		stloc dst
		ldloca dst
		ldfld int32 JitTest_volatile_il.VT::m_n
		dup
		call void [System.Console]System.Console::WriteLine(int32)
		ldc.i4 100
		add
		ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace JitTest_volatile_il

//*********** DISASSEMBLY COMPLETE ***********************
