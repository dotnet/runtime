// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'fr8'// as "fr4"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {B1A03CB5-0762-4E9F-BA39-E5C10C9A69AE}
.namespace JitTest_fr8_implicit_il
{
  .class public auto ansi Test
         extends ['mscorlib']System.Object
  {
    .method private hidebysig instance void 
            MulTest(float64 arg1,
                    float64 arg2,
                    float64 result) il managed
    {
      // Code size       43 (0x2b)
      .maxstack  9
      			ldarg.1
      			ldarg.2
      			mul
      			dup
      			ldarg.3
      			ldc.r8     0.05
      			add
      			bge.s      throwex

      			ldarg.3
      			ldc.r8     0.05
      			sub
      			dup
      			bgt.un.s   throwex
      			pop
      			ret

		throwex:
				pop
      			newobj     instance void ['mscorlib']System.Exception::.ctor()
      			throw
    } // end of method 'Test::MulTest'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       58 (0x3a)
      .maxstack  4
      .locals (int32 V_0)
      .try
      {
        IL_0000:  newobj     instance void JitTest_fr8_implicit_il.Test::.ctor()
        IL_0005:  ldc.r8     0.12345679
        IL_000a:  ldc.r8     123456789.0
        IL_000f:  ldc.r8     15241579.4
        IL_0014:  call       instance void JitTest_fr8_implicit_il.Test::MulTest(float64, float64, float64)
        IL_0019:  leave.s    IL_002a
      }  // end .try
      catch ['mscorlib']System.Object 
      {
        IL_001b:  pop
        IL_001c:  ldstr      "Failed w/ exception"
        IL_0021:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_0026:  ldc.i4.1
        IL_0027:  stloc.0
        IL_0028:  leave.s    IL_0038

      }  // end handler
      IL_002a:  ldstr      "Passed"
      IL_002f:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0034:  ldc.i4   0x64
      IL_0035:  stloc.0
      IL_0036:  br.s       IL_0038

      IL_0038:  ldloc.0
      IL_0039:  ret
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'JitTest_fr8_implicit_il'

//*********** DISASSEMBLY COMPLETE ***********************
