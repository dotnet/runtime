// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly bug 
{
}
.namespace JitTest
{
  .class value private auto ansi sealed VT extends [mscorlib]System.ValueType
  {
  	.field public int32 m_n
    .method private hidebysig static int32 Main() il managed
    {
		.entrypoint
		.maxstack  8
		.locals (value class JitTest.VT src, value class JitTest.VT dst)
		ldloca src
		initobj JitTest.VT
		ldloc src
		ldloca src
		ldc.i4 1000
		stfld int32 JitTest.VT::m_n
		stloc dst
		ldloca dst
		ldfld int32 JitTest.VT::m_n
		dup
		call void [System.Console]System.Console::WriteLine(int32)
		ldc.i4 100
		add
		ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
