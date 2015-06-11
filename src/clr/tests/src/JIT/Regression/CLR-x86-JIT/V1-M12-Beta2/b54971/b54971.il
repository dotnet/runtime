// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib
{
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
		call void [mscorlib]System.Console::WriteLine(int32)
		ldc.i4 100
		add
		ret
    } // end of method VT::Main

  } // end of class VT

} // end of namespace JitTest

//*********** DISASSEMBLY COMPLETE ***********************
