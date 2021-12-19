// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b32374' { }
.assembly extern xunit.core {}
.namespace Test
{
  .class auto ansi BB extends [mscorlib]System.Object
  {
    .field public static class Test.BB[] m_axStatic1
    .field public class [mscorlib]System.Object[] m_axField1
    .field public unsigned int32[] m_auField3
    .method public hidebysig static unsigned int32[] 
            Static1(unsigned int32 param2) il managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  ldsfld     class Test.BB[] Test.BB::m_axStatic1
      IL_0005:  ldc.i4.2
      IL_0006:  ldelem.ref
      IL_0007:  ldfld      unsigned int32[] Test.BB::m_auField3
      IL_000c:  ret
    } // end of method 'BB::Static1'

    .method public hidebysig static vararg class [mscorlib]System.Object[] 
            Static2(unsigned int32[] param2,
                    unsigned int32& param3) il managed
    {
      // Code size       11 (0xb)
      .maxstack  8
      IL_0000:  newobj     instance void Test.BB::.ctor()
      IL_0005:  ldfld      class [mscorlib]System.Object[] Test.BB::m_axField1
      IL_000a:  ret
    } // end of method 'BB::Static2'

    .method public hidebysig static float64[] 
            AA_Static2(unsigned int32& param1,
                       unsigned int32[] param4) il managed
    {
      // Code size       2 (0x2)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  ret
    } // end of method 'BB::AA_Static2'

    .method private hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      // Code size       97 (0x61)
      .maxstack  6
      .locals (class Test.BB V_0,
               int32 V_1)
      .try
      {
        IL_0000:  newobj     instance void Test.BB::.ctor()
        IL_0005:  stloc.0
        IL_0006:  ldnull
        IL_0007:  ldloc.0
        IL_0008:  ldfld      unsigned int32[] Test.BB::m_auField3
        IL_000d:  ldc.i4.2
        IL_000e:  ldelema    [mscorlib]System.UInt32
        IL_0013:  ldc.i4.0
        IL_0014:  call       unsigned int32[] Test.BB::Static1(unsigned int32)
        IL_0019:  ldc.i4.2
        IL_001a:  ldelema    [mscorlib]System.UInt32
        IL_001f:  ldc.i4.0
        IL_0020:  call       unsigned int32[] Test.BB::Static1(unsigned int32)
        IL_0025:  ldsfld     class Test.BB[] Test.BB::m_axStatic1
        IL_002a:  ldc.i4.2
        IL_002b:  ldelem.ref
        IL_002c:  ldfld      unsigned int32[] Test.BB::m_auField3
        IL_0031:  ldc.i4.2
        IL_0032:  ldelema    [mscorlib]System.UInt32
        IL_0037:  call       vararg class [mscorlib]System.Object[] Test.BB::Static2(unsigned int32[],
                                                                           unsigned int32&)
        IL_003c:  ldc.i4.2
        IL_003d:  ldelem.ref
        IL_003e:  unbox      [mscorlib]System.UInt32
        IL_0043:  ldind.u4
        IL_0044:  call       unsigned int32[] Test.BB::Static1(unsigned int32)
        IL_0049:  call       float64[] Test.BB::AA_Static2(unsigned int32&,
                                                           unsigned int32[])
        IL_004e:  ldc.i4.2
        IL_004f:  ldelem.r8
        IL_0050:  call       vararg class [mscorlib]System.Object[] Test.BB::Static2(unsigned int32[],
                                                                           unsigned int32&,
                                                                           ...,
                                                                           float64)
        IL_0055:  pop
        IL_0056:  leave.s    IL_005d

      }  // end .try
      catch [mscorlib]System.NullReferenceException 
      {
        IL_0058:  pop
        IL_0059:  ldc.i4.0
        IL_005a:  stloc.1
        IL_005b:  leave.s    IL_005f

      }  // end handler
      IL_005d:  ldc.i4.1
      IL_005e:  ret

      IL_005f:  ldloc.1
		ldc.i4 100
		add
      IL_0060:  ret
    } // end of method 'BB::Main'

    .method public hidebysig specialname rtspecialname static 
            void .cctor() il managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldnull
      IL_0001:  stsfld     class Test.BB[] Test.BB::m_axStatic1
      IL_0006:  ret
    } // end of method 'BB::.cctor'

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldnull
      IL_0002:  stfld      class [mscorlib]System.Object[] Test.BB::m_axField1
      IL_0007:  ldarg.0
      IL_0008:  ldnull
      IL_0009:  stfld      unsigned int32[] Test.BB::m_auField3
      IL_000e:  ldarg.0
      IL_000f:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0014:  ret
    } // end of method 'BB::.ctor'

  } // end of class 'BB'

} // end of namespace 'Test'

//*********** DISASSEMBLY COMPLETE ***********************
