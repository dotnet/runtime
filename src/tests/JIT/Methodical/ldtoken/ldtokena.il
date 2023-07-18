// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldtokena.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ASSEMBLY_NAME
{
  
  
}
.assembly extern xunit.core {}
.namespace JitTest_ldtokena_il
{
  .class public auto ansi beforefieldinit TestClass
         extends [mscorlib]System.Object
  {
    .field family class JitTest_ldtokena_il.TestClass m_fld
    .field public static class JitTest_ldtokena_il.TestClass s_fld
    .method public hidebysig static class JitTest_ldtokena_il.TestClass 
            StaticMethod(class JitTest_ldtokena_il.TestClass inst) cil managed
    {
      .maxstack  1
      .locals (class JitTest_ldtokena_il.TestClass V_0)
      IL_0000:  newobj     instance void JitTest_ldtokena_il.TestClass::.ctor()
      IL_0005:  stloc.0
      IL_0006:  br.s       IL_0008
      IL_0008:  ldloc.0
      IL_0009:  ret
    }
    .method private hidebysig instance class JitTest_ldtokena_il.TestClass 
            InstanceMethod(class JitTest_ldtokena_il.TestClass inst) cil managed
    {
      .maxstack  1
      .locals (class JitTest_ldtokena_il.TestClass V_0)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0004
      IL_0004:  ldloc.0
      IL_0005:  ret
    }
    .method assembly hidebysig newslot virtual 
            instance class JitTest_ldtokena_il.TestClass 
            VirtualMethod(class JitTest_ldtokena_il.TestClass inst) cil managed
    {
      .maxstack  1
      .locals (class JitTest_ldtokena_il.TestClass V_0)
      IL_0000:  ldarg.0
      IL_0001:  stloc.0
      IL_0002:  br.s       IL_0004
      IL_0004:  ldloc.0
      IL_0005:  ret
    }
    .method public hidebysig static int32 Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8
      .locals (int32 V_0,
      		valuetype [mscorlib]System.RuntimeFieldHandle,
      		valuetype [mscorlib]System.RuntimeMethodHandle,
      		valuetype [mscorlib]System.RuntimeTypeHandle)
      ldc.i4.0
      stloc.0

      ldtoken field class JitTest_ldtokena_il.TestClass JitTest_ldtokena_il.TestClass::m_fld
      stloc.1

      ldloca 0
      ldind.i4
      brfalse br1
      ldloca 1
      br mg1
br1:
      ldloca 1
mg1:
      ldobj [mscorlib]System.RuntimeFieldHandle
      pop

      ldc.i4 100
      ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    }
  }
}
