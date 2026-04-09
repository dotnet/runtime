// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.





.assembly extern mscorlib
{
}
.assembly ASSEMBLY_NAME
{
}
.assembly extern xunit.core {}
.namespace JitTest_nested_etc_il
{
  .class value private auto ansi sealed Struct1
         extends [mscorlib]System.ValueType
  {
    .class value auto ansi sealed nested public Struct1$Struct2
           extends [mscorlib]System.ValueType
    {
      .field private int32 m_i2
      .field private int64 m_l2
      .method public hidebysig instance void
              Verify() il managed
      {
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldfld      int32 JitTest_nested_etc_il.Struct1/Struct1$Struct2::m_i2
        IL_0006:  brtrue.s   IL_0012

        IL_0008:  ldarg.0
        IL_0009:  ldfld      int64 JitTest_nested_etc_il.Struct1/Struct1$Struct2::m_l2
        IL_000e:  ldc.i4.0
        IL_000f:  conv.i8
        IL_0010:  beq.s      IL_0018

        IL_0012:  newobj     instance void [mscorlib]System.Exception::.ctor()
        IL_0017:  throw

        IL_0018:  ret
      } // end of method Struct1$Struct2::Verify

    } // end of class Struct1$Struct2

    .field private int32 m_i1
    .field private int64 m_l1
    .field public value class JitTest_nested_etc_il.Struct1/Struct1$Struct2 m_str2
    .method public hidebysig instance void
            Verify() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      int32 JitTest_nested_etc_il.Struct1::m_i1
      IL_0006:  brtrue.s   IL_0012

      IL_0008:  ldarg.0
      IL_0009:  ldfld      int64 JitTest_nested_etc_il.Struct1::m_l1
      IL_000e:  ldc.i4.0
      IL_000f:  conv.i8
      IL_0010:  beq.s      IL_0018

      IL_0012:  newobj     instance void [mscorlib]System.Exception::.ctor()
      IL_0017:  throw

      IL_0018:  ldarg.0
      IL_0019:  ldflda     value class JitTest_nested_etc_il.Struct1/Struct1$Struct2 JitTest_nested_etc_il.Struct1::m_str2
      IL_001e:  call       instance void JitTest_nested_etc_il.Struct1/Struct1$Struct2::Verify()
      IL_0023:  ret
    } // end of method Struct1::Verify

  } // end of class Struct1

  .class public auto ansi Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  1
      .locals (value class JitTest_nested_etc_il.Struct1 V_0,
               typedref V_1,
               value class JitTest_nested_etc_il.Struct1/Struct1$Struct2 V_2)
      IL_0000:  ldloca.s   V_0
      IL_0002:  initobj    JitTest_nested_etc_il.Struct1
      IL_0008:  ldloca.s   V_0
      IL_000a:  mkrefany   JitTest_nested_etc_il.Struct1
      IL_0011:  refanyval  JitTest_nested_etc_il.Struct1
      IL_0016:  ldobj      JitTest_nested_etc_il.Struct1
      IL_001b:  stloc.0
      IL_001c:  ldloca.s   V_0
      IL_001e:  call       instance void JitTest_nested_etc_il.Struct1::Verify()
      IL_0023:  ldloca.s   V_0
      IL_0025:  ldflda     value class JitTest_nested_etc_il.Struct1/Struct1$Struct2 JitTest_nested_etc_il.Struct1::m_str2
      IL_002a:  mkrefany   JitTest_nested_etc_il.Struct1/Struct1$Struct2
      IL_0031:  refanyval  JitTest_nested_etc_il.Struct1/Struct1$Struct2
      IL_0036:  ldobj      JitTest_nested_etc_il.Struct1/Struct1$Struct2
      IL_003b:  stloc.2
      IL_003c:  ldloca.s   V_2
      IL_003e:  call       instance void JitTest_nested_etc_il.Struct1/Struct1$Struct2::Verify()
      			ldc.i4 100
      IL_0043:  ret
    } // end of method Test::Main

    .method public hidebysig specialname rtspecialname 
            instance void .ctor() il managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    } // end of method Test::.ctor

  } // end of class Test

} // end of namespace JitTest_nested_etc_il

