// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'recurse_calli'// as "recurse"
{

  //  .custom instance void ['mscorlib']System.Diagnostics.DebuggableAttribute::.ctor(bool,
  //                                                                                  bool) = ( 01 00 00 01 00 00 ) 
  
  
}
.assembly extern xunit.core {}
// MVID: {885F4995-E80A-480B-BB9B-30139BED90E1}
.namespace TestCase_recurse_calli_il
{
  .class public auto ansi Test extends ['mscorlib']System.Object
  {
    .method private hidebysig static int32 
            Fact1(int32& arg,
                  int32& result) il managed
    {
      // Code size       38 (0x26)
      .maxstack  3
      IL_0000:  ldarg.0
      IL_0001:  ldind.i4
      IL_0002:  ldc.i4.1
      IL_0003:  bgt.s      IL_0005
      
      			ldc.i4     0x12345
      			br        end_of_method
      
      IL_0005:  ldarg.1
      IL_0006:  dup
      IL_0007:  ldind.i4
      IL_0008:  ldarg.0
      IL_0009:  ldind.i4
      IL_000a:  mul
      IL_000b:  stind.i4
      IL_000c:  ldarg.0
      IL_000d:  dup
      IL_000e:  ldind.i4
      IL_000f:  ldc.i4.1
      IL_0010:  sub
      IL_0011:  stind.i4
      			
      			ldarg.0
      			ldarg.1
      			ldftn      int32 TestCase_recurse_calli_il.Test::Fact1(int32&, int32&)
      			calli      int32 (int32&, int32&)
      			
      end_of_method:
      			ret

    } // end of method 'Test::Fact1'

    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       72 (0x48)
      .maxstack  2
      .locals (int32 V_0,
               int32 V_1,
               int32 V_2)
      IL_0000:  ldc.i4.6
      IL_0001:  stloc.0
      IL_0002:  ldc.i4.1
      IL_0003:  stloc.1
      IL_0004:  ldloca.s   V_0
      IL_0006:  ldloca.s   V_1
      IL_0008:  call       int32 TestCase_recurse_calli_il.Test::Fact1(int32&,
                                                      int32&)
      IL_000d:  ldc.i4     0x12345
      IL_0012:  beq.s      IL_0022

      IL_0014:  ldstr      "FAILED"
      IL_0019:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_001e:  ldc.i4.1
      IL_001f:  stloc.2
      IL_0020:  br.s       IL_0046

      IL_0022:  ldloc.1
      IL_0023:  ldc.i4     0x2d0
      IL_0028:  beq.s      IL_0038

      IL_002a:  ldstr      "FAILED"
      IL_002f:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0034:  ldc.i4.2
      IL_0035:  stloc.2
      IL_0036:  br.s       IL_0046

      IL_0038:  ldstr      "PASSED"
      IL_003d:  call       void [System.Console]System.Console::WriteLine(class System.String)
      IL_0042:  ldc.i4    0x64
      IL_0043:  stloc.2
      IL_0044:  br.s       IL_0046

      IL_0046:  ldloc.2
      IL_0047:  ret
      IL_0048:  
    } // end of method 'Test::Main'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void ['mscorlib']System.Object::.ctor()
      IL_0006:  ret
      IL_0007:  
    } // end of method 'Test::.ctor'

  } // end of class 'Test'

} // end of namespace 'TestCase_recurse_calli_il'

//*********** DISASSEMBLY COMPLETE ***********************
