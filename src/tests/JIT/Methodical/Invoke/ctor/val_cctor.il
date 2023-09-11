// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {1C70E5EE-C023-4741-BFC0-A85E3927FB65}
.namespace JitTest_val_cctor_il
{
  .class value public auto ansi sealed TestStruct
         extends ['mscorlib']System.ValueType
  {
    .field private int64 m_testParam
    .field private static int64 m_sum
    .method private hidebysig specialname rtspecialname 
            instance void  .ctor(unsigned int64 testParam) il managed
    {
      // Code size       46 (0x2e)
      .maxstack  3
      .locals (value class JitTest_val_cctor_il.TestStruct V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldarg.1
      IL_0002:  stfld      int64 JitTest_val_cctor_il.TestStruct::m_testParam
      IL_0007:  ldsfld     int64 JitTest_val_cctor_il.TestStruct::m_sum
      IL_000c:  ldarg.0
      IL_000d:  ldfld      int64 JitTest_val_cctor_il.TestStruct::m_testParam
      IL_0012:  add
      IL_0013:  stsfld     int64 JitTest_val_cctor_il.TestStruct::m_sum
      IL_0018:  ldsfld     int64 JitTest_val_cctor_il.TestStruct::m_sum
      IL_001d:  ldc.i4.s   100
      IL_001f:  conv.i8
      IL_0020:  bge.s      IL_002d

      IL_0022:  ldloca.s   V_0
      IL_0024:  ldarg.1
      IL_0025:  ldc.i4.1
      IL_0026:  conv.i8
      IL_0027:  add
      IL_0028:  call       instance void JitTest_val_cctor_il.TestStruct::.ctor(unsigned int64)
      IL_002d:  ret
    } // end of method 'TestStruct::.ctor'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       66 (0x42)
      .maxstack  2
      .locals (value class JitTest_val_cctor_il.TestStruct V_0,
               int32 V_1)
      .try
      {
//        IL_0000:  ldloca.s   V_0
//        IL_0002:  ldc.i4.0
//        IL_0003:  conv.i8
//        IL_0004:  call       instance void JitTest_val_cctor_il.TestStruct::.ctor(unsigned int64)
        IL_0009:  ldsfld     int64 JitTest_val_cctor_il.TestStruct::m_sum
        IL_000e:  ldc.i4.s   105
        IL_0010:  conv.i8
        IL_0011:  beq.s      IL_0021

        IL_0013:  ldstr      "Failed"
        IL_0018:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_001d:  ldc.i4.1
        IL_001e:  stloc.1
        IL_001f:  leave.s    IL_0040

        IL_0021:  leave.s    IL_0032

      }  // end .try
      catch ['mscorlib']System.Object 
      {
        IL_0023:  pop
        IL_0024:  ldstr      "Failed w/ exception"
        IL_0029:  call       void [System.Console]System.Console::WriteLine(class System.String)
        IL_002e:  ldc.i4.2
        IL_002f:  stloc.1
        IL_0030:  leave.s    IL_0040

      }  // end handler
      IL_0032:  ldstr      "Passed"
      IL_0037:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_003c:  ldc.i4    0x64
      IL_003d:  stloc.1
      IL_003e:  br.s       IL_0040

      IL_0040:  ldloc.1
      IL_0041:  ret
    } // end of method 'TestStruct::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       8 (0x8)
      .maxstack  8
      IL_0000:  ldc.i4.0
      IL_0001:  conv.i8
      IL_0002:  stsfld     int64 JitTest_val_cctor_il.TestStruct::m_sum
      
        		ldc.i4.0
        		conv.i8
				newobj 	instance void JitTest_val_cctor_il.TestStruct::.ctor(unsigned int64)
      			pop
      			
      IL_0007:  ret
    } // end of method 'TestStruct::.cctor'

  } // end of class 'TestStruct'

} // end of namespace 'JitTest_val_cctor_il'

//*********** DISASSEMBLY COMPLETE ***********************
